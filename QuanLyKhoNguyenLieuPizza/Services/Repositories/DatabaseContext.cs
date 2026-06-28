using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

/// <summary>
/// Lớp cơ sở chứa các phương thức truy vấn dùng chung cho tất cả service.
/// </summary>
public abstract class DatabaseContext
{
    protected readonly string _connectionString;

    protected DatabaseContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected SqlConnection GetConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Tự động migration: đảm bảo schema phù hợp khi khởi động app.
    /// - Rename ChuKySo → ChuKySoDuyet (PhieuNhap, PhieuXuat)
    /// - Thêm cột mới: ChuKyNhap/ChuKyXuat, LoaiPhieu, KiemKeID, HanCanhBao
    /// - Tạo bảng PhieuKiemKe, CT_PhieuKiemKe
    /// </summary>
    public async Task EnsureChuKySoColumnsAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Phase 1: Thêm các cột phụ trợ còn thiếu
            var addColumnMigrations = new[]
            {
                ("PhieuXuat", "GhiChu", "NVARCHAR(500)"),
                ("PhieuNhap", "GhiChu", "NVARCHAR(500)")
            };

            foreach (var (table, column, dataType) in addColumnMigrations)
            {
                var checkSql = $@"IF NOT EXISTS (SELECT 1 FROM sys.columns
                                  WHERE object_id = OBJECT_ID('{table}') AND name = '{column}')
                                  ALTER TABLE [{table}] ADD [{column}] {dataType} NULL";
                using var cmd = new SqlCommand(checkSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Phase 2: Chuyển ChuKySo cũ sang ChuKySoDuyet, không để lại cột cũ gây nhầm lẫn
            var renameMigrations = new[]
            {
                ("PhieuNhap", "ChuKySo", "ChuKySoDuyet"),
                ("PhieuXuat", "ChuKySo", "ChuKySoDuyet")
            };

            foreach (var (table, oldCol, newCol) in renameMigrations)
            {
                var renameSql = $@"
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{oldCol}')
                       AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{newCol}')
                    BEGIN
                        EXEC sp_rename '{table}.{oldCol}', '{newCol}', 'COLUMN'
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{oldCol}')
                       AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('{table}') AND name = '{newCol}')
                    BEGIN
                        UPDATE [{table}]
                        SET [{newCol}] = [{oldCol}]
                        WHERE [{newCol}] IS NULL AND [{oldCol}] IS NOT NULL

                        ALTER TABLE [{table}] DROP COLUMN [{oldCol}]
                    END";
                using var cmd = new SqlCommand(renameSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Phase 3: Thêm các cột mới cho kiểm kê
            var newColumnMigrations = new[]
            {
                ("PhieuNhap", "ChuKySoDuyet", "NVARCHAR(MAX)"),
                ("PhieuNhap", "ChuKyNhap", "NVARCHAR(MAX)"),
                ("PhieuNhap", "LoaiPhieu", "NVARCHAR(100)"),
                ("PhieuNhap", "KiemKeID", "INT"),
                ("PhieuXuat", "ChuKySoDuyet", "NVARCHAR(MAX)"),
                ("PhieuXuat", "ChuKyXuat", "NVARCHAR(MAX)"),
                ("PhieuXuat", "LoaiPhieu", "NVARCHAR(100)"),
                ("PhieuXuat", "KiemKeID", "INT"),
                ("NguyenLieu", "HanCanhBao", "INT")
            };

            foreach (var (table, column, dataType) in newColumnMigrations)
            {
                var checkSql = $@"IF NOT EXISTS (SELECT 1 FROM sys.columns
                                  WHERE object_id = OBJECT_ID('{table}') AND name = '{column}')
                                  ALTER TABLE [{table}] ADD [{column}] {dataType} NULL";
                using var cmd = new SqlCommand(checkSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Phase 4: Tạo bảng PhieuKiemKe
            var soLuongConMigrationSql = @"
                IF OBJECT_ID('CT_PhieuNhap') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CT_PhieuNhap') AND name = 'SoLuongCon')
                BEGIN
                    ALTER TABLE CT_PhieuNhap
                    ADD SoLuongCon DECIMAL(18,4) NOT NULL
                        CONSTRAINT DF_CT_PhieuNhap_SoLuongCon DEFAULT (0)

                    UPDATE CT_PhieuNhap
                    SET SoLuongCon = SoLuong
                END";
            using (var cmd = new SqlCommand(soLuongConMigrationSql, conn))
                await cmd.ExecuteNonQueryAsync();

            var createPhieuKiemKeSql = @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PhieuKiemKe')
                CREATE TABLE PhieuKiemKe
                (
                    KiemKeID INT IDENTITY(1,1) PRIMARY KEY,
                    MaPhieuKiem NVARCHAR(50),
                    NhanVienKiemID INT,
                    NhanVienDuyetID INT,
                    NgayKiem DATETIME,
                    NgayDuyet DATETIME,
                    GhiChuDuyet NVARCHAR(500),
                    LyDoKiemKe NVARCHAR(255),
                    LoaiKiemKe NVARCHAR(50),
                    TrangThai NVARCHAR(50),
                    ChuKyKiem NVARCHAR(MAX),
                    ChuKyDuyet NVARCHAR(MAX),
                    CONSTRAINT FK_PhieuKiemKe_NhanVienKiem FOREIGN KEY (NhanVienKiemID) REFERENCES NhanVien(NhanVienID),
                    CONSTRAINT FK_PhieuKiemKe_NhanVienDuyet FOREIGN KEY (NhanVienDuyetID) REFERENCES NhanVien(NhanVienID)
                )";
            using (var cmd = new SqlCommand(createPhieuKiemKeSql, conn))
                await cmd.ExecuteNonQueryAsync();

            // Phase 5: Tạo bảng CT_PhieuKiemKe
            var createCTPhieuKiemKeSql = @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CT_PhieuKiemKe')
                CREATE TABLE CT_PhieuKiemKe
                (
                    ChiTietKiemID INT IDENTITY(1,1) PRIMARY KEY,
                    KiemKeID INT NOT NULL,
                    NguyenLieuID INT NOT NULL,
                    TonHeThong DECIMAL(18,4) DEFAULT 0,
                    DonViKhoID INT,
                    TonThucTe DECIMAL(18,4) DEFAULT 0,
                    DonViKiemID INT,
                    HeSoKiem DECIMAL(18,4) DEFAULT 1,
                    ChenhLech DECIMAL(18,4) DEFAULT 0,
                    GhiChuNhanVien NVARCHAR(500),
                    PhanHoiDuyet NVARCHAR(500),
                    ChiTietPhieuNhapID INT,
                    LoaiDieuChinh NVARCHAR(10),
                    PhieuDieuChinhID UNIQUEIDENTIFIER,
                    CONSTRAINT FK_CT_PhieuKiemKe_PhieuKiemKe FOREIGN KEY (KiemKeID) REFERENCES PhieuKiemKe(KiemKeID),
                    CONSTRAINT FK_CT_PhieuKiemKe_NguyenLieu FOREIGN KEY (NguyenLieuID) REFERENCES NguyenLieu(NguyenLieuID),
                    CONSTRAINT FK_CT_PhieuKiemKe_DonViKho FOREIGN KEY (DonViKhoID) REFERENCES DonViTinh(DonViID),
                    CONSTRAINT FK_CT_PhieuKiemKe_DonViKiem FOREIGN KEY (DonViKiemID) REFERENCES DonViTinh(DonViID),
                    CONSTRAINT FK_CT_PhieuKiemKe_CT_PhieuNhap FOREIGN KEY (ChiTietPhieuNhapID) REFERENCES CT_PhieuNhap(ChiTietID),
                    CONSTRAINT CK_CT_PhieuKiemKe_LoaiDieuChinh CHECK (LoaiDieuChinh IN (N'Nhập', N'Xuất') OR LoaiDieuChinh IS NULL)
                )";
            using (var cmd = new SqlCommand(createCTPhieuKiemKeSql, conn))
                await cmd.ExecuteNonQueryAsync();

            var ctKiemKeExtraColumnsSql = @"
                IF OBJECT_ID('CT_PhieuKiemKe') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CT_PhieuKiemKe') AND name = 'ChiTietPhieuNhapID')
                        ALTER TABLE CT_PhieuKiemKe ADD ChiTietPhieuNhapID INT NULL

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CT_PhieuKiemKe') AND name = 'LoaiDieuChinh')
                        ALTER TABLE CT_PhieuKiemKe ADD LoaiDieuChinh NVARCHAR(10) NULL

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CT_PhieuKiemKe') AND name = 'PhieuDieuChinhID')
                        ALTER TABLE CT_PhieuKiemKe ADD PhieuDieuChinhID UNIQUEIDENTIFIER NULL

                    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CT_PhieuKiemKe_LoaiDieuChinh')
                        ALTER TABLE CT_PhieuKiemKe
                        ADD CONSTRAINT CK_CT_PhieuKiemKe_LoaiDieuChinh
                        CHECK (LoaiDieuChinh IN (N'Nhập', N'Xuất') OR LoaiDieuChinh IS NULL)
                END";
            using (var cmd = new SqlCommand(ctKiemKeExtraColumnsSql, conn))
                await cmd.ExecuteNonQueryAsync();

        }
        catch { }
    }

    protected sealed class IngredientUnitContext
    {
        public int IngredientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? StockUnitId { get; init; }
        public string? StockUnitName { get; init; }
        public Dictionary<int, decimal> UnitFactors { get; } = new();
    }

    protected static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction? transaction = null)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (transaction != null)
            cmd.Transaction = transaction;
        return cmd;
    }

    protected static string NormalizeUnitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    protected static bool TryGetWeightFactorToKilogram(string? unitName, out decimal factor)
    {
        factor = NormalizeUnitToken(unitName) switch
        {
            "g" => 0.001m,
            "kg" => 1m,
            _ => 0m
        };

        return factor > 0;
    }

    protected static bool TryGetVolumeFactorToLiter(string? unitName, out decimal factor)
    {
        factor = NormalizeUnitToken(unitName) switch
        {
            "ml" => 0.001m,
            "l" => 1m,
            "lit" => 1m,
            _ => 0m
        };

        return factor > 0;
    }

    protected static bool IsPackageLikeUnit(string? unitName)
    {
        return NormalizeUnitToken(unitName) switch
        {
            "bao" => true,
            "thung" => true,
            "hop" => true,
            "goi" => true,
            "cai" => true,
            "can" => true,
            _ => false
        };
    }

    protected async Task<string?> GetUnitNameAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int? unitId,
        Dictionary<int, string> unitNameCache)
    {
        if (!unitId.HasValue)
            return null;

        if (unitNameCache.TryGetValue(unitId.Value, out var cached))
            return cached;

        const string sql = "SELECT TenDonVi FROM DonViTinh WHERE DonViID = @DonViID";
        using var cmd = CreateCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("@DonViID", unitId.Value);
        var result = await cmd.ExecuteScalarAsync();
        var name = result?.ToString();

        if (!string.IsNullOrWhiteSpace(name))
            unitNameCache[unitId.Value] = name;

        return name;
    }

    protected async Task<IngredientUnitContext> GetIngredientUnitContextAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int ingredientId,
        Dictionary<int, IngredientUnitContext> contextCache)
    {
        if (contextCache.TryGetValue(ingredientId, out var cached))
            return cached;

        const string ingredientSql = @"SELECT nl.NguyenLieuID, nl.TenNguyenLieu, nl.DonViID, dv.TenDonVi
                                       FROM NguyenLieu nl
                                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                                       WHERE nl.NguyenLieuID = @NguyenLieuID";

        int? fallbackUnitId = null;
        string? fallbackUnitName = null;
        string ingredientName = $"ID {ingredientId}";

        using (var cmd = CreateCommand(ingredientSql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", ingredientId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                ingredientName = reader.IsDBNull(1) ? $"ID {ingredientId}" : reader.GetString(1);
                fallbackUnitId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                fallbackUnitName = reader.IsDBNull(3) ? null : reader.GetString(3);
            }
        }

        // Đọc tất cả quy đổi đơn vị VÀ tìm đơn vị chuẩn (LaDonViChuan=1) từ bảng QuyDoiDonVi
        const string factorSql = @"SELECT qd.DonViID, qd.HeSo, qd.LaDonViChuan, dv.TenDonVi
                                   FROM QuyDoiDonVi qd
                                   LEFT JOIN DonViTinh dv ON qd.DonViID = dv.DonViID
                                   WHERE qd.NguyenLieuID = @NguyenLieuID";

        int? quyDoiStockUnitId = null;
        string? quyDoiStockUnitName = null;
        var unitFactors = new Dictionary<int, decimal>();

        using (var cmd = CreateCommand(factorSql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", ingredientId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0))
                    continue;

                var unitId = reader.GetInt32(0);
                var factor = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                var isDonViChuan = !reader.IsDBNull(2) && reader.GetBoolean(2);
                var tenDonVi = reader.IsDBNull(3) ? null : reader.GetString(3);

                if (factor > 0)
                    unitFactors[unitId] = factor;

                if (isDonViChuan)
                {
                    quyDoiStockUnitId = unitId;
                    quyDoiStockUnitName = tenDonVi;
                }
            }
        }

        // Ưu tiên đơn vị chuẩn từ QuyDoiDonVi, fallback về NguyenLieu.DonViID
        var finalStockUnitId = quyDoiStockUnitId ?? fallbackUnitId;
        var finalStockUnitName = quyDoiStockUnitName ?? fallbackUnitName;

        var context = new IngredientUnitContext
        {
            IngredientId = ingredientId,
            Name = ingredientName,
            StockUnitId = finalStockUnitId,
            StockUnitName = finalStockUnitName
        };

        foreach (var kvp in unitFactors)
            context.UnitFactors[kvp.Key] = kvp.Value;

        if (finalStockUnitId is int stockUnitId && !context.UnitFactors.ContainsKey(stockUnitId))
            context.UnitFactors[stockUnitId] = 1m;

        contextCache[ingredientId] = context;
        return context;
    }

    private static decimal GetReliableFactor(int unitId, string? unitName, Dictionary<int, decimal> dbFactors)
    {
        // CHỈ lấy hệ số từ bảng QuyDoiDonVi — KHÔNG tự suy diễn
        // Đảm bảo tính toàn vẹn: Nhập, Xuất, Bán hàng đều tuân theo cùng 1 quy tắc
        if (dbFactors.TryGetValue(unitId, out var factor) && factor > 0)
            return factor;

        // Nếu DB không có → trả về 0 (không quy đổi được)
        // Buộc người dùng phải cấu hình QuyDoiDonVi trước khi sử dụng
        return 0m;
    }

    protected static bool TryConvertByConfiguredFactors(
        IngredientUnitContext context,
        int sourceUnitId,
        string? sourceUnitName,
        decimal amount,
        out decimal convertedAmount)
    {
        convertedAmount = 0m;

        if (context.StockUnitId is not int stockUnitId)
            return false;

        var sourceFactor = GetReliableFactor(sourceUnitId, sourceUnitName, context.UnitFactors);
        if (sourceFactor <= 0) return false;

        var stockFactor = GetReliableFactor(stockUnitId, context.StockUnitName, context.UnitFactors);
        if (stockFactor <= 0) return false;

        convertedAmount = amount * sourceFactor / stockFactor;
        return true;
    }

    protected async Task<decimal> ConvertAmountToStockUnitAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int ingredientId,
        decimal amount,
        int? sourceUnitId,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        if (amount <= 0)
            return 0m;

        var context = await GetIngredientUnitContextAsync(conn, transaction, ingredientId, contextCache);

        // ĐẢM BẢO TOÀN VẸN: Phải có đơn vị lưu tồn kho trong QuyDoiDonVi
        if (context.StockUnitId == null)
            throw new InvalidOperationException(
                $"Nguyên liệu '{context.Name}' chưa cấu hình đơn vị lưu tồn kho trong QuyDoiDonVi. " +
                $"Vui lòng vào Tồn Kho → chọn nguyên liệu → thêm đơn vị và tích chọn ĐV Lưu Tồn Kho.");

        // ĐẢM BẢO TOÀN VẸN: Công thức/cấu hình phải chỉ định đơn vị
        if (sourceUnitId == null)
            throw new InvalidOperationException(
                $"Nguyên liệu '{context.Name}' chưa được gán đơn vị trong công thức/cấu hình. " +
                $"Vui lòng kiểm tra lại công thức món ăn hoặc cấu hình làm bánh.");

        // Nếu không trùng khớp đơn vị, thử quy đổi dựa trên hệ số đã cấu hình
        if (sourceUnitId != context.StockUnitId)
        {
            var sourceUnitName = await GetUnitNameAsync(conn, transaction, sourceUnitId, unitNameCache);

            if (TryConvertByConfiguredFactors(context, sourceUnitId.Value, sourceUnitName, amount, out var convertedAmount))
            {
                return convertedAmount;
            }

            throw new InvalidOperationException(
                $"Nguyên liệu '{context.Name}': Đơn vị trong công thức/cấu hình là '{sourceUnitName ?? $"ID {sourceUnitId.Value}"}' " +
                $"nhưng đơn vị lưu tồn kho là '{context.StockUnitName}'. Không tìm thấy tỷ lệ quy đổi hợp lệ. " +
                $"Vui lòng vào phần Quy Đổi Đơn Vị để thiết lập tỷ lệ quy đổi.");
        }

        // Cùng đơn vị → trừ trực tiếp
        return amount;
    }

    protected async Task<decimal> ConvertStockAmountToUnitAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int ingredientId,
        decimal stockAmount,
        int? targetUnitId,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        if (stockAmount <= 0)
            return 0m;

        var context = await GetIngredientUnitContextAsync(conn, transaction, ingredientId, contextCache);

        if (context.StockUnitId == null)
            throw new InvalidOperationException($"Nguyen lieu '{context.Name}' chua cau hinh don vi luu ton kho.");

        if (targetUnitId == null)
            throw new InvalidOperationException($"Nguyen lieu '{context.Name}' chua co don vi lo hang.");

        if (targetUnitId == context.StockUnitId)
            return stockAmount;

        var targetUnitName = await GetUnitNameAsync(conn, transaction, targetUnitId, unitNameCache);
        var stockFactor = GetReliableFactor(context.StockUnitId.Value, context.StockUnitName, context.UnitFactors);
        var targetFactor = GetReliableFactor(targetUnitId.Value, targetUnitName, context.UnitFactors);

        if (stockFactor <= 0 || targetFactor <= 0)
        {
            throw new InvalidOperationException(
                $"Nguyen lieu '{context.Name}': khong tim thay ty le quy doi tu '{context.StockUnitName}' sang '{targetUnitName ?? $"ID {targetUnitId.Value}"}'.");
        }

        return stockAmount * stockFactor / targetFactor;
    }

    /// <summary>
    /// Thực thi truy vấn và ánh xạ kết quả thành danh sách.
    /// </summary>
    protected async Task<List<T>> ExecuteQueryListAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var result = new List<T>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(mapper(reader));
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Thực thi truy vấn trả về một giá trị đơn.
    /// </summary>
    protected async Task<T> ExecuteScalarValueAsync<T>(string sql, T defaultValue, params SqlParameter[] parameters)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                return (T)Convert.ChangeType(result, typeof(T));
        }
        catch { }
        return defaultValue;
    }

    /// <summary>
    /// Thực thi lệnh không trả về dữ liệu (INSERT/UPDATE/DELETE).
    /// </summary>
    protected async Task<bool> ExecuteCommandAsync(string sql, params SqlParameter[] parameters)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// Thực thi truy vấn trả về một đối tượng hoặc null.
    /// </summary>
    protected async Task<T?> ExecuteQuerySingleAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters) where T : class
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }
        }
        catch { }
        return null;
    }
}
