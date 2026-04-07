using System.Text;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Tải từ cấu hình thay vì mã cứng
        _connectionString = ConfigurationService.Instance.GetConnectionString();
    }

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqlConnection GetConnection() => new SqlConnection(_connectionString);

    private sealed class IngredientUnitContext
    {
        public int IngredientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? StockUnitId { get; init; }
        public string? StockUnitName { get; init; }
        public Dictionary<int, decimal> UnitFactors { get; } = new();
    }

    private static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction? transaction = null)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (transaction != null)
            cmd.Transaction = transaction;
        return cmd;
    }

    private static string NormalizeUnitToken(string? value)
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

    private static bool TryGetWeightFactorToKilogram(string? unitName, out decimal factor)
    {
        factor = NormalizeUnitToken(unitName) switch
        {
            "g" => 0.001m,
            "kg" => 1m,
            _ => 0m
        };

        return factor > 0;
    }

    private static bool TryGetVolumeFactorToLiter(string? unitName, out decimal factor)
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

    private static bool IsPackageLikeUnit(string? unitName)
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

    private async Task<string?> GetUnitNameAsync(
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

    private async Task<IngredientUnitContext> GetIngredientUnitContextAsync(
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

        IngredientUnitContext? context = null;
        using (var cmd = CreateCommand(ingredientSql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", ingredientId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                context = new IngredientUnitContext
                {
                    IngredientId = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? $"ID {ingredientId}" : reader.GetString(1),
                    StockUnitId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    StockUnitName = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
        }

        context ??= new IngredientUnitContext
        {
            IngredientId = ingredientId,
            Name = $"ID {ingredientId}"
        };

        const string factorSql = @"SELECT DonViID, HeSo
                                   FROM QuyDoiDonVi
                                   WHERE NguyenLieuID = @NguyenLieuID";

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
                if (factor > 0)
                    context.UnitFactors[unitId] = factor;
            }
        }

        if (context.StockUnitId is int stockUnitId && !context.UnitFactors.ContainsKey(stockUnitId))
            context.UnitFactors[stockUnitId] = 1m;

        contextCache[ingredientId] = context;
        return context;
    }

    private static bool TryConvertByConfiguredFactors(
        IngredientUnitContext context,
        int sourceUnitId,
        decimal amount,
        out decimal convertedAmount)
    {
        convertedAmount = 0m;

        if (context.StockUnitId is not int stockUnitId)
            return false;

        if (!context.UnitFactors.TryGetValue(sourceUnitId, out var sourceFactor) || sourceFactor <= 0)
            return false;

        if (!context.UnitFactors.TryGetValue(stockUnitId, out var stockFactor) || stockFactor <= 0)
            return false;

        convertedAmount = amount * sourceFactor / stockFactor;
        return true;
    }

    private static bool TryConvertByMetricUnits(
        string? sourceUnitName,
        string? stockUnitName,
        decimal amount,
        out decimal convertedAmount)
    {
        convertedAmount = 0m;

        if (TryGetWeightFactorToKilogram(sourceUnitName, out var sourceWeight) &&
            TryGetWeightFactorToKilogram(stockUnitName, out var stockWeight))
        {
            convertedAmount = amount * sourceWeight / stockWeight;
            return true;
        }

        if (TryGetVolumeFactorToLiter(sourceUnitName, out var sourceVolume) &&
            TryGetVolumeFactorToLiter(stockUnitName, out var stockVolume))
        {
            convertedAmount = amount * sourceVolume / stockVolume;
            return true;
        }

        return false;
    }

    private static bool TryConvertByPackageFallback(
        IngredientUnitContext context,
        string? sourceUnitName,
        decimal amount,
        out decimal convertedAmount)
    {
        convertedAmount = 0m;

        if (context.StockUnitId is not int stockUnitId)
            return false;

        if (!context.UnitFactors.TryGetValue(stockUnitId, out var stockFactor) || stockFactor <= 0)
            return false;

        if (!IsPackageLikeUnit(context.StockUnitName) && context.UnitFactors.Count == 0)
            return false;

        // Legacy data stores a few packaged ingredients by box/carton while recipes use grams/ml.
        // When no explicit mapping exists, treat 1 standard package as 1kg/1L and scale by QuyDoiDonVi if present.
        if (TryGetWeightFactorToKilogram(sourceUnitName, out var sourceWeight))
        {
            convertedAmount = amount * sourceWeight / stockFactor;
            return true;
        }

        if (TryGetVolumeFactorToLiter(sourceUnitName, out var sourceVolume))
        {
            convertedAmount = amount * sourceVolume / stockFactor;
            return true;
        }

        return false;
    }

    private async Task<decimal> ConvertAmountToStockUnitAsync(
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

        if (sourceUnitId == null || context.StockUnitId == null || sourceUnitId == context.StockUnitId)
            return amount;

        if (TryConvertByConfiguredFactors(context, sourceUnitId.Value, amount, out var convertedAmount))
            return convertedAmount;

        var sourceUnitName = await GetUnitNameAsync(conn, transaction, sourceUnitId, unitNameCache);
        if (TryConvertByMetricUnits(sourceUnitName, context.StockUnitName, amount, out convertedAmount))
            return convertedAmount;

        if (TryConvertByPackageFallback(context, sourceUnitName, amount, out convertedAmount))
            return convertedAmount;

        throw new InvalidOperationException(
            $"Chưa cấu hình quy đổi đơn vị cho nguyên liệu '{context.Name}' ({sourceUnitName ?? $"ID {sourceUnitId.Value}"} -> {context.StockUnitName ?? "đơn vị tồn kho"}).");
    }

    #region Phương thức hỗ trợ
    /// <summary>
    /// Thực thi truy vấn và ánh xạ kết quả thành danh sách. Xử lý kết nối, try-catch và ghi log.
    /// </summary>
    private async Task<List<T>> ExecuteQueryListAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Query Error: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Thực thi truy vấn trả về một giá trị đơn. Xử lý kết nối, try-catch và ghi log.
    /// </summary>
    private async Task<T> ExecuteScalarValueAsync<T>(string sql, T defaultValue, params SqlParameter[] parameters)
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Scalar Error: {ex.Message}");
        }
        return defaultValue;
    }

    /// <summary>
    /// Thực thi lệnh không trả về dữ liệu (INSERT/UPDATE/DELETE). Xử lý kết nối, try-catch và ghi log.
    /// </summary>
    private async Task<bool> ExecuteCommandAsync(string sql, params SqlParameter[] parameters)
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Command Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thực thi truy vấn trả về một đối tượng hoặc null. Xử lý kết nối, try-catch và ghi log.
    /// </summary>
    private async Task<T?> ExecuteQuerySingleAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters) where T : class
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Query Error: {ex.Message}");
        }
        return null;
    }
    #endregion

    #region Xác thực
    public async Task<TaiKhoan?> AuthenticateAsync(string username, string password)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT tk.TaiKhoanID, tk.NhanVienID, tk.Username, tk.Password, tk.TrangThai,
                              nv.NhanVienID, nv.HoTen, nv.HinhAnh, nv.NgaySinh, nv.DiaChi, nv.SDT, nv.Email, nv.ChucVuID, nv.TrangThai,
                              cv.TenChucVu
                       FROM TaiKhoan tk
                       LEFT JOIN NhanVien nv ON tk.NhanVienID = nv.NhanVienID
                       LEFT JOIN ChucVu cv ON nv.ChucVuID = cv.ChucVuID
                       WHERE tk.Username = @Username AND tk.Password = @Password AND tk.TrangThai = 1 AND (nv.TrangThai IS NULL OR nv.TrangThai = 1)";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var taiKhoan = new TaiKhoan
                {
                    TaiKhoanID = reader.GetInt32(0),
                    NhanVienID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Username = reader.GetString(2),
                    Password = reader.GetString(3),
                    TrangThai = reader.GetBoolean(4)
                };
                
                if (!reader.IsDBNull(5))
                {
                    taiKhoan.NhanVien = new NhanVien
                    {
                        NhanVienID = reader.GetInt32(5),
                        HoTen = reader.GetString(6),
                        HinhAnh = reader.IsDBNull(7) ? null : reader.GetString(7),
                        NgaySinh = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                        DiaChi = reader.IsDBNull(9) ? null : reader.GetString(9),
                        SDT = reader.IsDBNull(10) ? null : reader.GetString(10),
                        Email = reader.IsDBNull(11) ? null : reader.GetString(11),
                        ChucVuID = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                        TrangThai = reader.GetBoolean(13)
                    };
                    
                    if (!reader.IsDBNull(14))
                    {
                        taiKhoan.NhanVien.ChucVu = new ChucVu
                        {
                            TenChucVu = reader.GetString(14)
                        };
                    }
                }
                
                return taiKhoan;
            }
        }
        catch (SqlException sqlEx)
        {
            throw new Exception($"Lỗi kết nối database: {sqlEx.Message}", sqlEx);
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi xác thực: {ex.Message}", ex);
        }
        
        return null;
    }
    #endregion

    #region Quản lý người dùng
    public async Task<List<ChucVu>> GetChucVusAsync() =>
        await ExecuteQueryListAsync("SELECT ChucVuID, TenChucVu FROM ChucVu",
            r => new ChucVu { ChucVuID = r.GetInt32(0), TenChucVu = r.GetString(1) });

    public async Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Xác thực nới lỏng: Tìm người dùng theo Email trước (độc nhất), sau đó xác minh thông tin khác trong bộ nhớ
            // Điều này xử lý các vấn đề về phân biệt chữ hoa/thường, khoảng trắng, định dạng ngày, v.v.
            var sql = @"SELECT NhanVienID, HoTen, Email, ChucVuID, NgaySinh
                        FROM NhanVien 
                        WHERE Email = @Email AND TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email ?? string.Empty);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var dbNhanVienID = reader.GetInt32(0);
                var dbHoTen = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var dbEmail = reader.GetString(2); 
                var dbChucVuID = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                var dbNgaySinh = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);

                // Xác thực tên (Không phân biệt hoa/thường, bỏ TẤT CẢ khoảng trắng, bỏ dấu)
                string Normalize(string s)
                {
                    if (string.IsNullOrEmpty(s)) return string.Empty;
                    
                    // Bỏ dấu
                    var normalized = s.Normalize(NormalizationForm.FormD);
                    var builder = new StringBuilder();
                    foreach (var c in normalized)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        {
                            builder.Append(c);
                        }
                    }
                    var withoutAccents = builder.ToString().Normalize(NormalizationForm.FormC);
                    
                    // Bỏ khoảng trắng và chuyển chữ thường
                    return withoutAccents.Replace(" ", "").ToLower();
                }
                
                if (Normalize(dbHoTen) != Normalize(hoTen))
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: Name mismatch. DB='{dbHoTen}', Input='{hoTen}'");
                    System.Diagnostics.Debug.WriteLine($"Normalized: DB='{Normalize(dbHoTen)}', Input='{Normalize(hoTen)}'");
                    return null;
                }

                // Xác thực ngày sinh (Chỉ phần ngày)
                if (dbNgaySinh?.Date != ngaySinh.Date)
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: DoB mismatch. DB='{dbNgaySinh:d}', Input='{ngaySinh:d}'");
                    return null;
                }

                // Xác thực chức vụ
                if (dbChucVuID != chucVuId)
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: Role mismatch. DB='{dbChucVuID}', Input='{chucVuId}'");
                    return null;
                }

                return new NhanVien
                {
                    NhanVienID = dbNhanVienID,
                    HoTen = dbHoTen,
                    Email = dbEmail,
                    ChucVuID = dbChucVuID,
                    NgaySinh = dbNgaySinh
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verifying user info: {ex.Message}");
            throw;
        }
        return null; // Không tìm thấy
    }

    public async Task<bool> ChangePasswordAsync(string email, string newPassword)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Cập nhật mật khẩu cho người dùng liên kết với email này
            var sql = @"UPDATE TaiKhoan 
                        SET Password = @Password 
                        FROM TaiKhoan tk
                        INNER JOIN NhanVien nv ON tk.NhanVienID = nv.NhanVienID
                        WHERE nv.Email = @Email";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Password", newPassword);
            cmd.Parameters.AddWithValue("@Email", email);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Loại Nguyên Liệu
    public async Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync() =>
        await ExecuteQueryListAsync("SELECT LoaiNLID, TenLoai FROM LoaiNguyenLieu",
            r => new LoaiNguyenLieu { LoaiNLID = r.GetInt32(0), TenLoai = r.GetString(1) });

    public async Task<bool> SaveLoaiNguyenLieuAsync(LoaiNguyenLieu loaiNguyenLieu)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (loaiNguyenLieu.LoaiNLID == 0)
            {
                sql = @"INSERT INTO LoaiNguyenLieu (TenLoai) VALUES (@TenLoai);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE LoaiNguyenLieu SET TenLoai = @TenLoai WHERE LoaiNLID = @LoaiNLID";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenLoai", loaiNguyenLieu.TenLoai);
            
            if (loaiNguyenLieu.LoaiNLID > 0)
            {
                cmd.Parameters.AddWithValue("@LoaiNLID", loaiNguyenLieu.LoaiNLID);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    loaiNguyenLieu.LoaiNLID = Convert.ToInt32(result);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving LoaiNguyenLieu: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteLoaiNguyenLieuAsync(int loaiNLID)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "DELETE FROM LoaiNguyenLieu WHERE LoaiNLID = @LoaiNLID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LoaiNLID", loaiNLID);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting LoaiNguyenLieu: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Nhà Cung Cấp
    public async Task<List<NhaCungCap>> GetNhaCungCapsAsync()
    {
        var result = new List<NhaCungCap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand("SELECT NhaCungCapID, TenNCC, DiaChi, SDT, Email, TrangThai FROM NhaCungCap WHERE TrangThai = 1", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new NhaCungCap
                {
                    NhaCungCapID = reader.GetInt32(0),
                    TenNCC = reader.GetString(1),
                    DiaChi = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SDT = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TrangThai = reader.GetBoolean(5)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhaCungCap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<NhaCungCap>> GetNhaCungCapsByNguyenLieuAsync(int nguyenLieuId)
    {
        var result = new List<NhaCungCap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ncc.NhaCungCapID, ncc.TenNCC, ncc.DiaChi, ncc.SDT, ncc.Email, ncc.TrangThai
                       FROM NhaCungCap ncc
                       INNER JOIN NguyenLieuNhaCungCap nlncc ON ncc.NhaCungCapID = nlncc.NhaCungCapID
                       WHERE nlncc.NguyenLieuID = @NguyenLieuID AND ncc.TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new NhaCungCap
                {
                    NhaCungCapID = reader.GetInt32(0),
                    TenNCC = reader.GetString(1),
                    DiaChi = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SDT = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TrangThai = reader.GetBoolean(5)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhaCungCap by NguyenLieu: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<NguyenLieuNhaCungCap>> GetNguyenLieuNhaCungCapsAsync(int nguyenLieuId)
    {
        var result = new List<NguyenLieuNhaCungCap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nlncc.NguyenLieuID, nlncc.NhaCungCapID, nlncc.GiaNhap, nlncc.NgayCapNhat,
                              ncc.TenNCC
                       FROM NguyenLieuNhaCungCap nlncc
                       INNER JOIN NhaCungCap ncc ON nlncc.NhaCungCapID = ncc.NhaCungCapID
                       WHERE nlncc.NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new NguyenLieuNhaCungCap
                {
                    NguyenLieuID = reader.GetInt32(0),
                    NhaCungCapID = reader.GetInt32(1),
                    GiaNhap = reader.GetDecimal(2),
                    NgayCapNhat = reader.GetDateTime(3),
                    NhaCungCap = new NhaCungCap { TenNCC = reader.GetString(4) }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieuNhaCungCap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<bool> UpsertNguyenLieuNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId, decimal giaNhap)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Delete existing relationships for this nguyenlieu first, then insert new one
            var deleteSql = "DELETE FROM NguyenLieuNhaCungCap WHERE NguyenLieuID = @NguyenLieuID";
            using var deleteCmd = new SqlCommand(deleteSql, conn);
            deleteCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            var insertSql = @"INSERT INTO NguyenLieuNhaCungCap (NguyenLieuID, NhaCungCapID, GiaNhap, NgayCapNhat)
                             VALUES (@NguyenLieuID, @NhaCungCapID, @GiaNhap, GETDATE())";
            using var insertCmd = new SqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            insertCmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            insertCmd.Parameters.AddWithValue("@GiaNhap", giaNhap);
            await insertCmd.ExecuteNonQueryAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error upserting NguyenLieuNhaCungCap: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AddNguyenLieuToNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId, decimal giaNhap)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"MERGE NguyenLieuNhaCungCap AS target
                        USING (SELECT @NguyenLieuID AS NguyenLieuID, @NhaCungCapID AS NhaCungCapID) AS source
                        ON target.NguyenLieuID = source.NguyenLieuID AND target.NhaCungCapID = source.NhaCungCapID
                        WHEN MATCHED THEN
                            UPDATE SET GiaNhap = @GiaNhap, NgayCapNhat = GETDATE()
                        WHEN NOT MATCHED THEN
                            INSERT (NguyenLieuID, NhaCungCapID, GiaNhap, NgayCapNhat)
                            VALUES (@NguyenLieuID, @NhaCungCapID, @GiaNhap, GETDATE());";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            cmd.Parameters.AddWithValue("@GiaNhap", giaNhap);
            await cmd.ExecuteNonQueryAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding NguyenLieu to NhaCungCap: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Nguyên Liệu
    public async Task<List<NguyenLieu>> GetNguyenLieusAsync(int? loaiNLID = null)
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh, 
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi, lnl.TenLoai
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN LoaiNguyenLieu lnl ON nl.LoaiNLID = lnl.LoaiNLID
                       WHERE nl.TrangThai = 1";
            
            if (loaiNLID.HasValue && loaiNLID.Value > 0)
            {
                sql += " AND nl.LoaiNLID = @LoaiNLID";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            if (loaiNLID.HasValue && loaiNLID.Value > 0)
            {
                cmd.Parameters.AddWithValue("@LoaiNLID", loaiNLID.Value);
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    nl.LoaiNguyenLieu = new LoaiNguyenLieu { TenLoai = reader.GetString(8) };
                }
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieu: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<NguyenLieu>> GetAllNguyenLieusWithDetailsAsync()
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh, 
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi, lnl.TenLoai,
                              tk.SoLuongTon,
                              (SELECT TOP 1 ncc.TenNCC FROM NguyenLieuNhaCungCap nlncc 
                               INNER JOIN NhaCungCap ncc ON nlncc.NhaCungCapID = ncc.NhaCungCapID 
                               WHERE nlncc.NguyenLieuID = nl.NguyenLieuID) as TenNCC,
                              (SELECT TOP 1 nlncc.GiaNhap FROM NguyenLieuNhaCungCap nlncc 
                               WHERE nlncc.NguyenLieuID = nl.NguyenLieuID) as GiaNhap
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN LoaiNguyenLieu lnl ON nl.LoaiNLID = lnl.LoaiNLID
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    nl.LoaiNguyenLieu = new LoaiNguyenLieu { TenLoai = reader.GetString(8) };
                }
                
                if (!reader.IsDBNull(9))
                {
                    nl.TonKho = new TonKho { SoLuongTon = reader.GetDecimal(9) };
                }
                
                if (!reader.IsDBNull(10))
                {
                    nl.NguyenLieuNhaCungCaps = new List<NguyenLieuNhaCungCap>
                    {
                        new NguyenLieuNhaCungCap 
                        { 
                            NhaCungCap = new NhaCungCap { TenNCC = reader.GetString(10) },
                            GiaNhap = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                        }
                    };
                }
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieu with details: {ex.Message}");
        }
        
        return result;
    }
    #endregion

    #region Tồn Kho
    public async Task<List<TonKho>> GetTonKhosAsync()
    {
        var result = new List<TonKho>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ISNULL(tk.TonKhoID, 0), nl.NguyenLieuID, 
                              ISNULL(tk.SoLuongTon, 0), ISNULL(tk.NgayCapNhat, GETDATE()),
                              nl.TenNguyenLieu, nl.HinhAnh, dv.TenDonVi, nl.MaNguyenLieu
                       FROM NguyenLieu nl
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       WHERE nl.TrangThai = 1
                       ORDER BY nl.NguyenLieuID ASC";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var tk = new TonKho
                {
                    TonKhoID = reader.GetInt32(0),
                    NguyenLieuID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    SoLuongTon = reader.GetDecimal(2),
                    NgayCapNhat = reader.GetDateTime(3),
                    NguyenLieu = new NguyenLieu
                    {
                        TenNguyenLieu = reader.GetString(4),
                        HinhAnh = reader.IsDBNull(5) ? null : reader.GetString(5),
                        DonViTinh = reader.IsDBNull(6) ? null : new DonViTinh { TenDonVi = reader.GetString(6) },
                        MaNguyenLieu = reader.IsDBNull(7) ? null : reader.GetString(7)
                    }
                };
                
                result.Add(tk);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading TonKho: {ex.Message}");
        }
        
        return result;
    }
    #endregion

    #region Đơn Vị Tính
    public async Task<List<DonViTinh>> GetDonViTinhsAsync() =>
        await ExecuteQueryListAsync("SELECT DonViID, TenDonVi FROM DonViTinh",
            r => new DonViTinh { DonViID = r.GetInt32(0), TenDonVi = r.GetString(1) });
    #endregion

    #region Quy Đổi Đơn Vị
    public async Task<List<QuyDoiDonVi>> GetQuyDoiDonVisAsync(int nguyenLieuID)
    {
        var result = new List<QuyDoiDonVi>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT qd.QuyDoiID, qd.NguyenLieuID, qd.DonViID, qd.HeSo, qd.LaDonViChuan, dv.TenDonVi
                       FROM QuyDoiDonVi qd
                       INNER JOIN DonViTinh dv ON qd.DonViID = dv.DonViID
                       WHERE qd.NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuID);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new QuyDoiDonVi
                {
                    QuyDoiID = reader.GetInt32(0),
                    NguyenLieuID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    DonViID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    HeSo = reader.GetDecimal(3),
                    LaDonViChuan = reader.GetBoolean(4),
                    DonViTinh = new DonViTinh { TenDonVi = reader.GetString(5) }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading QuyDoiDonVi: {ex.Message}");
        }
        
        return result;
    }

    public async Task<bool> SaveQuyDoiDonViAsync(QuyDoiDonVi quyDoi)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (quyDoi.QuyDoiID == 0)
            {
                sql = @"INSERT INTO QuyDoiDonVi (NguyenLieuID, DonViID, HeSo, LaDonViChuan)
                       VALUES (@NguyenLieuID, @DonViID, @HeSo, @LaDonViChuan)";
            }
            else
            {
                sql = @"UPDATE QuyDoiDonVi 
                       SET DonViID = @DonViID, HeSo = @HeSo, LaDonViChuan = @LaDonViChuan
                       WHERE QuyDoiID = @QuyDoiID";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", quyDoi.NguyenLieuID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViID", quyDoi.DonViID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@HeSo", quyDoi.HeSo);
            cmd.Parameters.AddWithValue("@LaDonViChuan", quyDoi.LaDonViChuan);
            
            if (quyDoi.QuyDoiID > 0)
            {
                cmd.Parameters.AddWithValue("@QuyDoiID", quyDoi.QuyDoiID);
            }
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QuyDoiDonVi: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteQuyDoiDonViAsync(int quyDoiId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "DELETE FROM QuyDoiDonVi WHERE QuyDoiID = @QuyDoiID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@QuyDoiID", quyDoiId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QuyDoiDonVi: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Phương thức bổ sung
    public async Task<NguyenLieu?> GetNguyenLieuByIdAsync(int id)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh, 
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi, lnl.TenLoai
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN LoaiNguyenLieu lnl ON nl.LoaiNLID = lnl.LoaiNLID
                       WHERE nl.NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", id);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    nl.LoaiNguyenLieu = new LoaiNguyenLieu { TenLoai = reader.GetString(8) };
                }
                
                return nl;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting NguyenLieu: {ex.Message}");
        }
        
        return null;
    }

    public async Task<TonKho?> GetTonKhoByNguyenLieuIdAsync(int nguyenLieuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT tk.TonKhoID, tk.NguyenLieuID, tk.SoLuongTon, tk.NgayCapNhat,
                              nl.TenNguyenLieu, dv.TenDonVi
                       FROM TonKho tk
                       INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       WHERE tk.NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new TonKho
                {
                    TonKhoID = reader.GetInt32(0),
                    NguyenLieuID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    SoLuongTon = reader.GetDecimal(2),
                    NgayCapNhat = reader.GetDateTime(3),
                    NguyenLieu = new NguyenLieu
                    {
                        TenNguyenLieu = reader.GetString(4),
                        DonViTinh = reader.IsDBNull(5) ? null : new DonViTinh { TenDonVi = reader.GetString(5) }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting TonKho: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> SaveNguyenLieuAsync(NguyenLieu nguyenLieu)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (nguyenLieu.NguyenLieuID == 0)
            {
                sql = @"INSERT INTO NguyenLieu (MaNguyenLieu, TenNguyenLieu, HinhAnh, LoaiNLID, DonViID, TrangThai)
                       VALUES (@MaNguyenLieu, @TenNguyenLieu, @HinhAnh, @LoaiNLID, @DonViID, @TrangThai);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE NguyenLieu 
                       SET MaNguyenLieu = @MaNguyenLieu, TenNguyenLieu = @TenNguyenLieu, 
                           HinhAnh = @HinhAnh, LoaiNLID = @LoaiNLID, DonViID = @DonViID, TrangThai = @TrangThai
                       WHERE NguyenLieuID = @NguyenLieuID";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaNguyenLieu", nguyenLieu.MaNguyenLieu ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TenNguyenLieu", nguyenLieu.TenNguyenLieu);
            cmd.Parameters.AddWithValue("@HinhAnh", nguyenLieu.HinhAnh ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LoaiNLID", nguyenLieu.LoaiNLID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViID", nguyenLieu.DonViID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrangThai", nguyenLieu.TrangThai);
            
            if (nguyenLieu.NguyenLieuID > 0)
            {
                cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieu.NguyenLieuID);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    nguyenLieu.NguyenLieuID = Convert.ToInt32(result);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NguyenLieu: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId) =>
        await ExecuteCommandAsync("UPDATE NguyenLieu SET TrangThai = 0 WHERE NguyenLieuID = @NguyenLieuID",
            new SqlParameter("@NguyenLieuID", nguyenLieuId));

    public async Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong) =>
        await ExecuteCommandAsync(
            @"UPDATE TonKho SET SoLuongTon = @SoLuongTon, NgayCapNhat = GETDATE() WHERE NguyenLieuID = @NguyenLieuID",
            new SqlParameter("@SoLuongTon", soLuong),
            new SqlParameter("@NguyenLieuID", nguyenLieuId));
    #endregion

    #region Thống kê cho Dashboard
    public async Task<int> GetTotalNguyenLieuCountAsync() =>
        await ExecuteScalarValueAsync("SELECT COUNT(*) FROM NguyenLieu WHERE TrangThai = 1", 0);

    public async Task<int> GetTotalTonKhoCountAsync() =>
        await ExecuteScalarValueAsync("SELECT COUNT(*) FROM TonKho WHERE SoLuongTon > 0", 0);

    public async Task<int> GetLowStockCountAsync(decimal threshold = 20) =>
        await ExecuteScalarValueAsync(
            "SELECT COUNT(*) FROM TonKho WHERE SoLuongTon > 0 AND SoLuongTon < @Threshold", 0,
            new SqlParameter("@Threshold", threshold));

    public async Task<int> GetNearExpiryCountAsync(int days = 7) =>
        await ExecuteScalarValueAsync(
            @"SELECT COUNT(*) FROM TonKho tk
              INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
              WHERE tk.SoLuongTon > 0 
              AND nl.HanSuDung IS NOT NULL 
              AND nl.HanSuDung <= DATEADD(DAY, @Days, GETDATE())
              AND nl.HanSuDung > GETDATE()", 0,
            new SqlParameter("@Days", days));

    public async Task<int> GetExpiredCountAsync() =>
        await ExecuteScalarValueAsync(
            @"SELECT COUNT(*) FROM TonKho tk
              INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
              WHERE tk.SoLuongTon > 0 
              AND nl.HanSuDung IS NOT NULL 
              AND nl.HanSuDung <= GETDATE()", 0);

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi)>> GetLowStockItemsAsync(decimal threshold = 20)
    {
        var result = new List<(string, decimal, string)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.TenNguyenLieu, tk.SoLuongTon, ISNULL(dv.TenDonVi, '')
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        WHERE tk.SoLuongTon > 0 AND tk.SoLuongTon < @Threshold
                        ORDER BY tk.SoLuongTon ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Threshold", threshold);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetLowStockItems: {ex.Message}"); }
        return result;
    }

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNearExpiryItemsAsync(int days = 7)
    {
        var result = new List<(string, decimal, string, DateTime?)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.TenNguyenLieu, tk.SoLuongTon, ISNULL(dv.TenDonVi, ''), nl.HanSuDung
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        WHERE tk.SoLuongTon > 0 
                        AND nl.HanSuDung IS NOT NULL 
                        AND nl.HanSuDung <= DATEADD(DAY, @Days, GETDATE())
                        AND nl.HanSuDung > GETDATE()
                        ORDER BY nl.HanSuDung ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", days);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetDateTime(3)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetNearExpiryItems: {ex.Message}"); }
        return result;
    }

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetExpiredItemsAsync()
    {
        var result = new List<(string, decimal, string, DateTime?)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.TenNguyenLieu, tk.SoLuongTon, ISNULL(dv.TenDonVi, ''), nl.HanSuDung
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        WHERE tk.SoLuongTon > 0 
                        AND nl.HanSuDung IS NOT NULL 
                        AND nl.HanSuDung <= GETDATE()
                        ORDER BY nl.HanSuDung ASC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetDateTime(3)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetExpiredItems: {ex.Message}"); }
        return result;
    }

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi)>> GetNormalStockItemsAsync(decimal lowThreshold = 20)
    {
        var result = new List<(string, decimal, string)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.TenNguyenLieu, tk.SoLuongTon, ISNULL(dv.TenDonVi, '')
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        WHERE tk.SoLuongTon >= @Threshold
                        AND (nl.HanSuDung IS NULL OR nl.HanSuDung > GETDATE())
                        ORDER BY nl.TenNguyenLieu ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Threshold", lowThreshold);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetNormalStockItems: {ex.Message}"); }
        return result;
    }
    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi)>> GetOutOfStockItemsAsync()
    {
        var result = new List<(string, decimal, string)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.TenNguyenLieu, ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, '')
                        FROM NguyenLieu nl
                        LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        WHERE (tk.TonKhoID IS NULL OR tk.SoLuongTon <= 0) AND nl.TrangThai = 1
                        ORDER BY nl.TenNguyenLieu ASC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetOutOfStockItems: {ex.Message}"); }
        return result;
    }
    #endregion

    #region Phiếu Nhập
    public async Task<List<PhieuNhap>> GetPhieuNhapsAsync(
        string? searchText = null,
        int? nhanVienId = null,
        int? nhaCungCapId = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null,
        List<byte>? trangThaiFilter = null)
    {
        var result = new List<PhieuNhap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT pn.PhieuNhapID, pn.MaPhieuNhap, pn.NhanVienNhapID, pn.NhaCungCapID,
                              pn.NgayNhap, pn.TongTien, pn.TrangThai,
                              nv.HoTen as TenNhanVien, ncc.TenNCC,
                              pn.NhanVienDuyetID, pn.NgayDuyet, nvd.HoTen as TenNhanVienDuyet
                       FROM PhieuNhap pn
                       LEFT JOIN NhanVien nv ON pn.NhanVienNhapID = nv.NhanVienID
                       LEFT JOIN NhaCungCap ncc ON pn.NhaCungCapID = ncc.NhaCungCapID
                       LEFT JOIN NhanVien nvd ON pn.NhanVienDuyetID = nvd.NhanVienID
                       WHERE 1=1";
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (pn.MaPhieuNhap LIKE @SearchText OR nv.HoTen LIKE @SearchText OR ncc.TenNCC LIKE @SearchText)";
            }
            
            if (nhanVienId.HasValue)
            {
                sql += " AND pn.NhanVienNhapID = @NhanVienID";
            }
            
            if (nhaCungCapId.HasValue)
            {
                sql += " AND pn.NhaCungCapID = @NhaCungCapID";
            }
            
            if (tuNgay.HasValue)
            {
                sql += " AND pn.NgayNhap >= @TuNgay";
            }
            
            if (denNgay.HasValue)
            {
                sql += " AND pn.NgayNhap <= @DenNgay";
            }
            
            if (trangThaiFilter != null && trangThaiFilter.Any())
            {
                sql += " AND pn.TrangThai IN (" + string.Join(",", trangThaiFilter) + ")";
            }
            
            sql += " ORDER BY pn.NgayNhap DESC";
            
            using var cmd = new SqlCommand(sql, conn);
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
            }
            
            if (nhanVienId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId.Value);
            }
            
            if (nhaCungCapId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId.Value);
            }
            
            if (tuNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@TuNgay", tuNgay.Value.Date);
            }
            
            if (denNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@DenNgay", denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var phieuNhap = new PhieuNhap
                {
                    PhieuNhapID = reader.GetInt32(0),
                    MaPhieuNhap = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienNhapID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NhaCungCapID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    NgayNhap = reader.GetDateTime(4),
                    TongTien = reader.GetDecimal(5),
                    TrangThai = reader.GetByte(6),
                    NhanVienDuyetID = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    NgayDuyet = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
                };
                
                if (!reader.IsDBNull(7))
                {
                    phieuNhap.NhanVienNhap = new NhanVien { HoTen = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    phieuNhap.NhaCungCap = new NhaCungCap { TenNCC = reader.GetString(8) };
                }
                
                if (!reader.IsDBNull(11))
                {
                    phieuNhap.NhanVienDuyet = new NhanVien { HoTen = reader.GetString(11) };
                }
                
                result.Add(phieuNhap);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PhieuNhap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<PhieuNhap?> GetPhieuNhapByIdAsync(int phieuNhapId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT pn.PhieuNhapID, pn.MaPhieuNhap, pn.NhanVienNhapID, pn.NhaCungCapID,
                              pn.NgayNhap, pn.TongTien, pn.TrangThai,
                              nv.HoTen as TenNhanVien, ncc.TenNCC, ncc.DiaChi,
                              pn.NhanVienDuyetID, pn.NgayDuyet, nvd.HoTen as TenNhanVienDuyet
                       FROM PhieuNhap pn
                       LEFT JOIN NhanVien nv ON pn.NhanVienNhapID = nv.NhanVienID
                       LEFT JOIN NhaCungCap ncc ON pn.NhaCungCapID = ncc.NhaCungCapID
                       LEFT JOIN NhanVien nvd ON pn.NhanVienDuyetID = nvd.NhanVienID
                       WHERE pn.PhieuNhapID = @PhieuNhapID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var phieuNhap = new PhieuNhap
                {
                    PhieuNhapID = reader.GetInt32(0),
                    MaPhieuNhap = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienNhapID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NhaCungCapID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    NgayNhap = reader.GetDateTime(4),
                    TongTien = reader.GetDecimal(5),
                    TrangThai = reader.GetByte(6),
                    NhanVienDuyetID = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    NgayDuyet = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                };
                
                if (!reader.IsDBNull(7))
                {
                    phieuNhap.NhanVienNhap = new NhanVien { HoTen = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    phieuNhap.NhaCungCap = new NhaCungCap 
                    { 
                        TenNCC = reader.GetString(8),
                        DiaChi = reader.IsDBNull(9) ? null : reader.GetString(9)
                    };
                }
                
                if (!reader.IsDBNull(12))
                {
                    phieuNhap.NhanVienDuyet = new NhanVien { HoTen = reader.GetString(12) };
                }
                
                return phieuNhap;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuNhap: {ex.Message}");
        }
        
        return null;
    }

    public async Task<List<CT_PhieuNhap>> GetChiTietPhieuNhapAsync(int phieuNhapId)
    {
        var result = new List<CT_PhieuNhap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ct.ChiTietID, ct.PhieuNhapID, ct.NguyenLieuID, ct.SoLuong,
                              ct.DonViID, ct.HeSo, ct.DonGia, ct.ThanhTien, ct.HSD,
                              nl.MaNguyenLieu, nl.TenNguyenLieu, dv.TenDonVi
                       FROM CT_PhieuNhap ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       WHERE ct.PhieuNhapID = @PhieuNhapID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var chiTiet = new CT_PhieuNhap
                {
                    ChiTietID = reader.GetInt32(0),
                    PhieuNhapID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    NguyenLieuID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    SoLuong = reader.GetDecimal(3),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    HeSo = reader.GetDecimal(5),
                    DonGia = reader.GetDecimal(6),
                    ThanhTien = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    HSD = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
                };
                
                chiTiet.NguyenLieu = new NguyenLieu
                {
                    MaNguyenLieu = reader.IsDBNull(9) ? null : reader.GetString(9),
                    TenNguyenLieu = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                };
                
                if (!reader.IsDBNull(11))
                {
                    chiTiet.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(11) };
                }
                
                result.Add(chiTiet);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading CT_PhieuNhap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<NguyenLieu>> GetNguyenLieusByNhaCungCapAsync(int nhaCungCapId)
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh,
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi, nlncc.NhaCungCapID, ncc.TenNCC, nlncc.GiaNhap
                       FROM NguyenLieu nl
                       INNER JOIN NguyenLieuNhaCungCap nlncc ON nl.NguyenLieuID = nlncc.NguyenLieuID
                       INNER JOIN NhaCungCap ncc ON nlncc.NhaCungCapID = ncc.NhaCungCapID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       WHERE nlncc.NhaCungCapID = @NhaCungCapID AND nl.TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(10))
                {
                    nl.NguyenLieuNhaCungCaps = new List<NguyenLieuNhaCungCap>
                    {
                        new NguyenLieuNhaCungCap
                        {
                            NhaCungCapID = reader.GetInt32(8),
                            GiaNhap = reader.GetDecimal(10),
                            NhaCungCap = new NhaCungCap
                            {
                                NhaCungCapID = reader.GetInt32(8),
                                TenNCC = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                            }
                        }
                    };
                }
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieu by NhaCungCap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<NguyenLieu>> GetAllNguyenLieusWithPriceAsync()
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh,
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi,
                              pref.NhaCungCapID, pref.TenNCC, pref.GiaNhap
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       OUTER APPLY (
                           SELECT TOP 1 nlncc.NhaCungCapID, ncc.TenNCC, nlncc.GiaNhap
                           FROM NguyenLieuNhaCungCap nlncc
                           LEFT JOIN NhaCungCap ncc ON nlncc.NhaCungCapID = ncc.NhaCungCapID
                           WHERE nlncc.NguyenLieuID = nl.NguyenLieuID
                           ORDER BY nlncc.NgayCapNhat DESC, nlncc.NhaCungCapID
                       ) pref
                       WHERE nl.TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(10))
                {
                    nl.NguyenLieuNhaCungCaps = new List<NguyenLieuNhaCungCap>
                    {
                        new NguyenLieuNhaCungCap
                        {
                            NhaCungCapID = reader.GetInt32(8),
                            GiaNhap = reader.GetDecimal(10),
                            NhaCungCap = new NhaCungCap
                            {
                                NhaCungCapID = reader.GetInt32(8),
                                TenNCC = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                            }
                        }
                    };
                }
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading all NguyenLieu with price: {ex.Message}");
        }
        
        return result;
    }

    public async Task<string> GenerateMaPhieuNhapAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT TOP 1 MaPhieuNhap FROM PhieuNhap 
                       WHERE MaPhieuNhap LIKE 'PN%' 
                       ORDER BY PhieuNhapID DESC";
            
            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                var lastCode = result.ToString()!;
                if (int.TryParse(lastCode.Substring(2), out int lastNumber))
                {
                    return $"PN{(lastNumber + 1):D6}";
                }
            }
            
            return "PN000001";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating MaPhieuNhap: {ex.Message}");
            return $"PN{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    public async Task<int> SavePhieuNhapAsync(PhieuNhap phieuNhap, List<CT_PhieuNhap> chiTiets)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            int phieuNhapId;
            
            if (phieuNhap.PhieuNhapID == 0)
            {
                // Thêm phiếu nhập mới
                var sql = @"INSERT INTO PhieuNhap (MaPhieuNhap, NhanVienNhapID, NhaCungCapID, NgayNhap, TongTien, TrangThai)
                           VALUES (@MaPhieuNhap, @NhanVienNhapID, @NhaCungCapID, @NgayNhap, @TongTien, @TrangThai);
                           SELECT SCOPE_IDENTITY();";
                
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaPhieuNhap", phieuNhap.MaPhieuNhap ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienNhapID", phieuNhap.NhanVienNhapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhaCungCapID", phieuNhap.NhaCungCapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayNhap", phieuNhap.NgayNhap);
                cmd.Parameters.AddWithValue("@TongTien", phieuNhap.TongTien);
                cmd.Parameters.AddWithValue("@TrangThai", phieuNhap.TrangThai);
                
                var result = await cmd.ExecuteScalarAsync();
                phieuNhapId = Convert.ToInt32(result);
            }
            else
            {
                // Cập nhật phiếu nhập hiện có
                phieuNhapId = phieuNhap.PhieuNhapID;
                
                var sql = @"UPDATE PhieuNhap 
                           SET NhanVienNhapID = @NhanVienNhapID, NhaCungCapID = @NhaCungCapID,
                               NgayNhap = @NgayNhap, TongTien = @TongTien, TrangThai = @TrangThai
                           WHERE PhieuNhapID = @PhieuNhapID";
                
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                cmd.Parameters.AddWithValue("@NhanVienNhapID", phieuNhap.NhanVienNhapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhaCungCapID", phieuNhap.NhaCungCapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayNhap", phieuNhap.NgayNhap);
                cmd.Parameters.AddWithValue("@TongTien", phieuNhap.TongTien);
                cmd.Parameters.AddWithValue("@TrangThai", phieuNhap.TrangThai);
                
                await cmd.ExecuteNonQueryAsync();
                
                // Xóa chi tiết hiện có
                var deleteSql = "DELETE FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                await deleteCmd.ExecuteNonQueryAsync();
            }
            
            // Thêm chi tiết
            foreach (var ct in chiTiets)
            {
                var ctSql = @"INSERT INTO CT_PhieuNhap (PhieuNhapID, NguyenLieuID, SoLuong, DonViID, HeSo, DonGia, ThanhTien, HSD)
                             VALUES (@PhieuNhapID, @NguyenLieuID, @SoLuong, @DonViID, @HeSo, @DonGia, @ThanhTien, @HSD)";
                
                using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                ctCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                ctCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
                ctCmd.Parameters.AddWithValue("@DonViID", ct.DonViID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@HeSo", ct.HeSo);
                ctCmd.Parameters.AddWithValue("@DonGia", ct.DonGia);
                ctCmd.Parameters.AddWithValue("@ThanhTien", ct.ThanhTien ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@HSD", ct.HSD ?? (object)DBNull.Value);
                
                await ctCmd.ExecuteNonQueryAsync();
                
                // KHÔNG cập nhật TồnKho ở đây - chỉ cập nhật khi duyệt phiếu
            }
            
            transaction.Commit();
            return phieuNhapId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error saving PhieuNhap: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ApprovePhieuNhapAsync(int phieuNhapId, int nhanVienDuyetId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            // Cập nhật trạng thái phiếu nhập
            var sql = @"UPDATE PhieuNhap 
                       SET TrangThai = 2, NhanVienDuyetID = @NhanVienDuyetID, NgayDuyet = GETDATE()
                       WHERE PhieuNhapID = @PhieuNhapID AND TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                transaction.Rollback();
                return false;
            }
            
            // Lấy chi tiết phiếu nhập để cập nhật TồnKho
            var getSql = "SELECT NguyenLieuID, SoLuong, HeSo FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            
            var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, decimal HeSo)>();
            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((reader.GetInt32(0), reader.GetDecimal(1), reader.GetDecimal(2)));
            }
            reader.Close();
            
            // Cập nhật TồnKho
            foreach (var ct in chiTiets)
            {
                await UpdateTonKhoOnNhapAsync(conn, transaction, ct.NguyenLieuID, ct.SoLuong * ct.HeSo);
            }
            
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error approving PhieuNhap: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelPhieuNhapAsync(int phieuNhapId, int nguoiHuyId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"UPDATE PhieuNhap 
                        SET TrangThai = 3, NhanVienDuyetID = @NguoiHuyId, NgayDuyet = GETDATE()
                        WHERE PhieuNhapID = @PhieuNhapID AND TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            cmd.Parameters.AddWithValue("@NguoiHuyId", nguoiHuyId);
            
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling PhieuNhap: {ex.Message}");
            return false;
        }
    }

    private async Task UpdateTonKhoOnNhapAsync(SqlConnection conn, SqlTransaction transaction, int nguyenLieuId, decimal soLuong)
    {
        // Kiểm tra nếu TồnKho đã tồn tại
        var checkSql = "SELECT COUNT(*) FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
        checkCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        
        if (count > 0)
        {
            var updateSql = @"UPDATE TonKho 
                             SET SoLuongTon = SoLuongTon + @SoLuong, NgayCapNhat = GETDATE()
                             WHERE NguyenLieuID = @NguyenLieuID";
            using var updateCmd = new SqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@SoLuong", soLuong);
            updateCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            await updateCmd.ExecuteNonQueryAsync();
        }
        else
        {
            var insertSql = @"INSERT INTO TonKho (NguyenLieuID, SoLuongTon, NgayCapNhat)
                             VALUES (@NguyenLieuID, @SoLuong, GETDATE())";
            using var insertCmd = new SqlCommand(insertSql, conn, transaction);
            insertCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            insertCmd.Parameters.AddWithValue("@SoLuong", soLuong);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<bool> DeletePhieuNhapAsync(int phieuNhapId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            // Lấy chi tiết để hoàn tác TồnKho
            var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, decimal HeSo)>();
            
            var getSql = "SELECT NguyenLieuID, SoLuong, HeSo FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            
            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((reader.GetInt32(0), reader.GetDecimal(1), reader.GetDecimal(2)));
            }
            reader.Close();
            
            // Hoàn tác Tồn kho
            foreach (var ct in chiTiets)
            {
                var updateSql = @"UPDATE TonKho 
                                 SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                 WHERE NguyenLieuID = @NguyenLieuID";
                using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong * ct.HeSo);
                updateCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                await updateCmd.ExecuteNonQueryAsync();
            }
            
            // Xóa chi tiết
            var deleteCtSql = "DELETE FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            await deleteCtCmd.ExecuteNonQueryAsync();
            
            // Xóa PhiếuNhập
            var deleteSql = "DELETE FROM PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error deleting PhieuNhap: {ex.Message}");
            return false;
        }
    }

    public async Task<List<NhanVien>> GetNhanViensAsync()
    {
        var result = new List<NhanVien>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT NhanVienID, HoTen, NgaySinh, DiaChi, SDT, Email, ChucVuID, TrangThai, HinhAnh
                       FROM NhanVien WHERE TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new NhanVien
                {
                    NhanVienID = reader.GetInt32(0),
                    HoTen = reader.GetString(1),
                    NgaySinh = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    DiaChi = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SDT = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ChucVuID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    TrangThai = reader.GetBoolean(7),
                    HinhAnh = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhanVien: {ex.Message}");
        }
        
        return result;
    }

    public async Task<decimal> GetTotalTongTienPhieuNhapAsync(
        int? nhanVienId = null,
        int? nhaCungCapId = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ISNULL(SUM(TongTien), 0) FROM PhieuNhap WHERE 1=1";
            
            if (nhanVienId.HasValue)
            {
                sql += " AND NhanVienNhapID = @NhanVienID";
            }
            
            if (nhaCungCapId.HasValue)
            {
                sql += " AND NhaCungCapID = @NhaCungCapID";
            }
            
            if (tuNgay.HasValue)
            {
                sql += " AND NgayNhap >= @TuNgay";
            }
            
            if (denNgay.HasValue)
            {
                sql += " AND NgayNhap <= @DenNgay";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            
            if (nhanVienId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId.Value);
            }
            
            if (nhaCungCapId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId.Value);
            }
            
            if (tuNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@TuNgay", tuNgay.Value.Date);
            }
            
            if (denNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@DenNgay", denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            }
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting total TongTien: {ex.Message}");
            return 0;
        }
    }
    #endregion

    #region Phiếu Xuất
    public async Task<List<PhieuXuat>> GetPhieuXuatsAsync(
        string? searchText = null,
        int? nhanVienYeuCauId = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null,
        List<byte>? trangThaiFilter = null)
    {
        var result = new List<PhieuXuat>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT px.PhieuXuatID, px.MaPhieuXuat, px.NhanVienYeuID, px.NhanVienDuyetID,
                              px.NgayYeuCau, px.NgayDuyet, px.TrangThai,
                              nvy.HoTen as TenNVYeuCau, nvd.HoTen as TenNVDuyet
                       FROM PhieuXuat px
                       LEFT JOIN NhanVien nvy ON px.NhanVienYeuID = nvy.NhanVienID
                       LEFT JOIN NhanVien nvd ON px.NhanVienDuyetID = nvd.NhanVienID
                       WHERE 1=1";
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (px.MaPhieuXuat LIKE @SearchText OR nvy.HoTen LIKE @SearchText)";
            }
            
            if (nhanVienYeuCauId.HasValue)
            {
                sql += " AND px.NhanVienYeuID = @NhanVienYeuID";
            }
            
            if (tuNgay.HasValue)
            {
                sql += " AND px.NgayYeuCau >= @TuNgay";
            }
            
            if (denNgay.HasValue)
            {
                sql += " AND px.NgayYeuCau <= @DenNgay";
            }
            
            if (trangThaiFilter != null && trangThaiFilter.Any())
            {
                sql += $" AND px.TrangThai IN ({string.Join(",", trangThaiFilter)})";
            }
            
            sql += " ORDER BY px.NgayYeuCau DESC";
            
            using var cmd = new SqlCommand(sql, conn);
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
            }
            
            if (nhanVienYeuCauId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NhanVienYeuID", nhanVienYeuCauId.Value);
            }
            
            if (tuNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@TuNgay", tuNgay.Value.Date);
            }
            
            if (denNgay.HasValue)
            {
                cmd.Parameters.AddWithValue("@DenNgay", denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var phieuXuat = new PhieuXuat
                {
                    PhieuXuatID = reader.GetInt32(0),
                    MaPhieuXuat = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienYeuID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NhanVienDuyetID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    NgayYeuCau = reader.GetDateTime(4),
                    NgayDuyet = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    TrangThai = reader.GetByte(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    phieuXuat.NhanVienYeuCau = new NhanVien { HoTen = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    phieuXuat.NhanVienDuyet = new NhanVien { HoTen = reader.GetString(8) };
                }
                
                result.Add(phieuXuat);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PhieuXuat: {ex.Message}");
        }
        
        return result;
    }

    public async Task<PhieuXuat?> GetPhieuXuatByIdAsync(int phieuXuatId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT px.PhieuXuatID, px.MaPhieuXuat, px.NhanVienYeuID, px.NhanVienDuyetID,
                              px.NgayYeuCau, px.NgayDuyet, px.TrangThai,
                              nvy.HoTen as TenNVYeuCau, nvd.HoTen as TenNVDuyet
                       FROM PhieuXuat px
                       LEFT JOIN NhanVien nvy ON px.NhanVienYeuID = nvy.NhanVienID
                       LEFT JOIN NhanVien nvd ON px.NhanVienDuyetID = nvd.NhanVienID
                       WHERE px.PhieuXuatID = @PhieuXuatID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var phieuXuat = new PhieuXuat
                {
                    PhieuXuatID = reader.GetInt32(0),
                    MaPhieuXuat = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienYeuID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NhanVienDuyetID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    NgayYeuCau = reader.GetDateTime(4),
                    NgayDuyet = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    TrangThai = reader.GetByte(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    phieuXuat.NhanVienYeuCau = new NhanVien { HoTen = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    phieuXuat.NhanVienDuyet = new NhanVien { HoTen = reader.GetString(8) };
                }
                
                return phieuXuat;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuXuat: {ex.Message}");
        }
        
        return null;
    }

    public async Task<List<CT_PhieuXuat>> GetChiTietPhieuXuatAsync(int phieuXuatId)
    {
        var result = new List<CT_PhieuXuat>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ct.ChiTietID, ct.PhieuXuatID, ct.NguyenLieuID, ct.SoLuong,
                              ct.DonViID, ct.HeSo,
                              nl.MaNguyenLieu, nl.TenNguyenLieu, dv.TenDonVi
                       FROM CT_PhieuXuat ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       WHERE ct.PhieuXuatID = @PhieuXuatID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var chiTiet = new CT_PhieuXuat
                {
                    ChiTietID = reader.GetInt32(0),
                    PhieuXuatID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    NguyenLieuID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    SoLuong = reader.GetDecimal(3),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    HeSo = reader.GetDecimal(5)
                };
                
                chiTiet.NguyenLieu = new NguyenLieu
                {
                    MaNguyenLieu = reader.IsDBNull(6) ? null : reader.GetString(6),
                    TenNguyenLieu = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                };
                
                if (!reader.IsDBNull(8))
                {
                    chiTiet.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(8) };
                }
                
                result.Add(chiTiet);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading CT_PhieuXuat: {ex.Message}");
        }
        
        return result;
    }

    public async Task<string> GenerateMaPhieuXuatAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT TOP 1 MaPhieuXuat FROM PhieuXuat 
                       WHERE MaPhieuXuat LIKE 'PX%' 
                       ORDER BY PhieuXuatID DESC";
            
            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            
            if (result != null && result != DBNull.Value)
            {
                var lastCode = result.ToString()!;
                if (int.TryParse(lastCode.Substring(2), out int lastNumber))
                {
                    return $"PX{(lastNumber + 1):D6}";
                }
            }
            
            return "PX000001";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating MaPhieuXuat: {ex.Message}");
            return $"PX{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    public async Task<int> SavePhieuXuatAsync(PhieuXuat phieuXuat, List<CT_PhieuXuat> chiTiets)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            int phieuXuatId;
            
            if (phieuXuat.PhieuXuatID == 0)
            {
                // Thêm phiếu xuất mới
                var sql = @"INSERT INTO PhieuXuat (MaPhieuXuat, NhanVienYeuID, NhanVienDuyetID, NgayYeuCau, NgayDuyet, TrangThai)
                           VALUES (@MaPhieuXuat, @NhanVienYeuID, @NhanVienDuyetID, @NgayYeuCau, @NgayDuyet, @TrangThai);
                           SELECT SCOPE_IDENTITY();";
                
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaPhieuXuat", phieuXuat.MaPhieuXuat ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienYeuID", phieuXuat.NhanVienYeuID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienDuyetID", phieuXuat.NhanVienDuyetID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayYeuCau", phieuXuat.NgayYeuCau);
                cmd.Parameters.AddWithValue("@NgayDuyet", phieuXuat.NgayDuyet ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuXuat.TrangThai);
                
                var result = await cmd.ExecuteScalarAsync();
                phieuXuatId = Convert.ToInt32(result);
            }
            else
            {
                // Cập nhật phiếu xuất hiện có
                phieuXuatId = phieuXuat.PhieuXuatID;
                
                var sql = @"UPDATE PhieuXuat 
                           SET NhanVienYeuID = @NhanVienYeuID, NhanVienDuyetID = @NhanVienDuyetID,
                               NgayYeuCau = @NgayYeuCau, NgayDuyet = @NgayDuyet, TrangThai = @TrangThai
                           WHERE PhieuXuatID = @PhieuXuatID";
                
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                cmd.Parameters.AddWithValue("@NhanVienYeuID", phieuXuat.NhanVienYeuID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienDuyetID", phieuXuat.NhanVienDuyetID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayYeuCau", phieuXuat.NgayYeuCau);
                cmd.Parameters.AddWithValue("@NgayDuyet", phieuXuat.NgayDuyet ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuXuat.TrangThai);
                
                await cmd.ExecuteNonQueryAsync();
                
                // Xóa chi tiết hiện có
                var deleteSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                await deleteCmd.ExecuteNonQueryAsync();
            }
            
            // Thêm chi tiết
            foreach (var ct in chiTiets)
            {
                var ctSql = @"INSERT INTO CT_PhieuXuat (PhieuXuatID, NguyenLieuID, SoLuong, DonViID, HeSo)
                             VALUES (@PhieuXuatID, @NguyenLieuID, @SoLuong, @DonViID, @HeSo)";
                
                using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                ctCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                ctCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
                ctCmd.Parameters.AddWithValue("@DonViID", ct.DonViID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@HeSo", ct.HeSo);
                
                await ctCmd.ExecuteNonQueryAsync();
            }
            
            transaction.Commit();
            return phieuXuatId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error saving PhieuXuat: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ApprovePhieuXuatAsync(int phieuXuatId, int nhanVienDuyetId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            // Lấy chi tiết để cập nhật TồnKho
            var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, decimal HeSo)>();
            
            var getSql = "SELECT NguyenLieuID, SoLuong, HeSo FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            
            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((reader.GetInt32(0), reader.GetDecimal(1), reader.GetDecimal(2)));
            }
            reader.Close();
            
            // Cập nhật TồnKho (trừ)
            foreach (var ct in chiTiets)
            {
                var updateSql = @"UPDATE TonKho 
                                 SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                 WHERE NguyenLieuID = @NguyenLieuID";
                using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong * ct.HeSo);
                updateCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                await updateCmd.ExecuteNonQueryAsync();
            }
            
            // Cập nhật trạng thái PhiếuXuất
            var sql = @"UPDATE PhieuXuat 
                       SET TrangThai = 2, NhanVienDuyetID = @NhanVienDuyetID, NgayDuyet = GETDATE()
                       WHERE PhieuXuatID = @PhieuXuatID";
            
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            
            await cmd.ExecuteNonQueryAsync();
            
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error approving PhieuXuat: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelPhieuXuatAsync(int phieuXuatId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "UPDATE PhieuXuat SET TrangThai = 3 WHERE PhieuXuatID = @PhieuXuatID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling PhieuXuat: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePhieuXuatAsync(int phieuXuatId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            // Xóa chi tiết
            var deleteCtSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            await deleteCtCmd.ExecuteNonQueryAsync();
            
            // Xóa PhiếuXuất
            var deleteSql = "DELETE FROM PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error deleting PhieuXuat: {ex.Message}");
            return false;
        }
    }

    public async Task<List<NguyenLieu>> GetNguyenLieusWithTonKhoAsync()
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh,
                              nl.LoaiNLID, nl.DonViID, nl.TrangThai,
                              dv.TenDonVi, ISNULL(tk.SoLuongTon, 0) as SoLuongTon
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                       WHERE nl.TrangThai = 1 AND ISNULL(tk.SoLuongTon, 0) > 0";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LoaiNLID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DonViID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TrangThai = reader.GetBoolean(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(7) };
                }
                
                nl.TonKho = new TonKho { SoLuongTon = reader.GetDecimal(8) };
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieu with TonKho: {ex.Message}");
        }
        
        return result;
    }
    #endregion

    #region Quản lý Nhà Cung Cấp
    public async Task<List<NhaCungCap>> GetAllNhaCungCapsAsync(string? searchText = null, bool? trangThai = null)
    {
        var result = new List<NhaCungCap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ncc.NhaCungCapID, ncc.TenNCC, ncc.DiaChi, ncc.SDT, ncc.Email, ncc.TrangThai,
                              (SELECT COUNT(*) FROM NguyenLieuNhaCungCap WHERE NhaCungCapID = ncc.NhaCungCapID) as SoNguyenLieu
                       FROM NhaCungCap ncc
                       WHERE 1=1";
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (ncc.TenNCC LIKE @SearchText OR ncc.DiaChi LIKE @SearchText OR ncc.SDT LIKE @SearchText OR ncc.Email LIKE @SearchText)";
            }
            
            if (trangThai.HasValue)
            {
                sql += " AND ncc.TrangThai = @TrangThai";
            }
            
            sql += " ORDER BY ncc.TenNCC";
            
            using var cmd = new SqlCommand(sql, conn);
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
            }
            
            if (trangThai.HasValue)
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai.Value);
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new NhaCungCap
                {
                    NhaCungCapID = reader.GetInt32(0),
                    TenNCC = reader.GetString(1),
                    DiaChi = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SDT = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TrangThai = reader.GetBoolean(5)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhaCungCap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<NhaCungCap?> GetNhaCungCapByIdAsync(int nhaCungCapId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT NhaCungCapID, TenNCC, DiaChi, SDT, Email, TrangThai
                       FROM NhaCungCap WHERE NhaCungCapID = @NhaCungCapID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new NhaCungCap
                {
                    NhaCungCapID = reader.GetInt32(0),
                    TenNCC = reader.GetString(1),
                    DiaChi = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SDT = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TrangThai = reader.GetBoolean(5)
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting NhaCungCap: {ex.Message}");
        }
        
        return null;
    }

    public async Task<int> SaveNhaCungCapAsync(NhaCungCap nhaCungCap)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (nhaCungCap.NhaCungCapID == 0)
            {
                sql = @"INSERT INTO NhaCungCap (TenNCC, DiaChi, SDT, Email, TrangThai)
                       VALUES (@TenNCC, @DiaChi, @SDT, @Email, @TrangThai);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE NhaCungCap 
                       SET TenNCC = @TenNCC, DiaChi = @DiaChi, SDT = @SDT, Email = @Email, TrangThai = @TrangThai
                       WHERE NhaCungCapID = @NhaCungCapID;
                       SELECT @NhaCungCapID;";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenNCC", nhaCungCap.TenNCC);
            cmd.Parameters.AddWithValue("@DiaChi", nhaCungCap.DiaChi ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SDT", nhaCungCap.SDT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", nhaCungCap.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrangThai", nhaCungCap.TrangThai);
            
            if (nhaCungCap.NhaCungCapID > 0)
            {
                cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCap.NhaCungCapID);
            }
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NhaCungCap: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateNhaCungCapTrangThaiAsync(int nhaCungCapId, bool trangThai)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "UPDATE NhaCungCap SET TrangThai = @TrangThai WHERE NhaCungCapID = @NhaCungCapID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TrangThai", trangThai);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating NhaCungCap TrangThai: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteNhaCungCapAsync(int nhaCungCapId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Kiểm tra nếu NhàCungCấp đang được sử dụng
            var checkSql = @"SELECT COUNT(*) FROM NguyenLieuNhaCungCap WHERE NhaCungCapID = @NhaCungCapID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (count > 0)
            {
                // Xóa mềm - đặt TrạngThái = false
                return await UpdateNhaCungCapTrangThaiAsync(nhaCungCapId, false);
            }
            
            // Xóa cứng nếu không được sử dụng
            var sql = "DELETE FROM NhaCungCap WHERE NhaCungCapID = @NhaCungCapID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting NhaCungCap: {ex.Message}");
            return false;
        }
    }

    public async Task<List<NguyenLieu>> GetNguyenLieusByNhaCungCapIdAsync(int nhaCungCapId)
    {
        var result = new List<NguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, nl.HinhAnh,
                              nl.DonViID, dv.TenDonVi, nlncc.GiaNhap
                       FROM NguyenLieu nl
                       INNER JOIN NguyenLieuNhaCungCap nlncc ON nl.NguyenLieuID = nlncc.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       WHERE nlncc.NhaCungCapID = @NhaCungCapID AND nl.TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    HinhAnh = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                };
                
                if (!reader.IsDBNull(5))
                {
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(5) };
                }
                
                if (!reader.IsDBNull(6))
                {
                    nl.NguyenLieuNhaCungCaps = new List<NguyenLieuNhaCungCap>
                    {
                        new NguyenLieuNhaCungCap { GiaNhap = reader.GetDecimal(6) }
                    };
                }
                
                result.Add(nl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieu by NhaCungCap: {ex.Message}");
        }
        
        return result;
    }

    public async Task<int> GetNhaCungCapCountAsync(bool? trangThai = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "SELECT COUNT(*) FROM NhaCungCap";
            if (trangThai.HasValue)
            {
                sql += " WHERE TrangThai = @TrangThai";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            if (trangThai.HasValue)
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai.Value);
            }
            
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error counting NhaCungCap: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> DeleteNguyenLieuFromNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "DELETE FROM NguyenLieuNhaCungCap WHERE NguyenLieuID = @NguyenLieuID AND NhaCungCapID = @NhaCungCapID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            cmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting NguyenLieu from NhaCungCap: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Quản lý Chức Vụ
    public async Task<List<ChucVu>> GetAllChucVusAsync()
    {
        var result = new List<ChucVu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "SELECT ChucVuID, TenChucVu FROM ChucVu ORDER BY TenChucVu";
            
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new ChucVu
                {
                    ChucVuID = reader.GetInt32(0),
                    TenChucVu = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading ChucVu: {ex.Message}");
        }
        
        return result;
    }

    public async Task<int> SaveChucVuAsync(ChucVu chucVu)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (chucVu.ChucVuID == 0)
            {
                sql = @"INSERT INTO ChucVu (TenChucVu) VALUES (@TenChucVu);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE ChucVu SET TenChucVu = @TenChucVu WHERE ChucVuID = @ChucVuID;
                       SELECT @ChucVuID;";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenChucVu", chucVu.TenChucVu);
            
            if (chucVu.ChucVuID > 0)
            {
                cmd.Parameters.AddWithValue("@ChucVuID", chucVu.ChucVuID);
            }
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving ChucVu: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteChucVuAsync(int chucVuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Kiểm tra nếu có NhânViên nào đang sử dụng ChứcVụ này
            var checkSql = "SELECT COUNT(*) FROM NhanVien WHERE ChucVuID = @ChucVuID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@ChucVuID", chucVuId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (count > 0)
            {
                return false; // Cannot delete, being used
            }
            
            var sql = "DELETE FROM ChucVu WHERE ChucVuID = @ChucVuID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ChucVuID", chucVuId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting ChucVu: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Quản lý Nhân Viên
    public async Task<List<NhanVien>> GetAllNhanViensFullAsync(string? searchText = null, bool? trangThai = null, List<int>? chucVuIds = null)
    {
        var result = new List<NhanVien>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nv.NhanVienID, nv.HoTen, nv.NgaySinh, nv.DiaChi, nv.SDT, nv.Email,
                              nv.ChucVuID, nv.TrangThai, nv.HinhAnh,
                              cv.TenChucVu, tk.Username
                       FROM NhanVien nv
                       LEFT JOIN ChucVu cv ON nv.ChucVuID = cv.ChucVuID
                       LEFT JOIN TaiKhoan tk ON nv.NhanVienID = tk.NhanVienID
                       WHERE 1=1";
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (nv.HoTen LIKE @SearchText OR nv.SDT LIKE @SearchText OR nv.Email LIKE @SearchText)";
            }
            
            if (trangThai.HasValue)
            {
                sql += " AND nv.TrangThai = @TrangThai";
            }
            
            if (chucVuIds != null && chucVuIds.Any())
            {
                sql += $" AND nv.ChucVuID IN ({string.Join(",", chucVuIds)})";
            }
            
            sql += " ORDER BY nv.HoTen";
            
            using var cmd = new SqlCommand(sql, conn);
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
            }
            
            if (trangThai.HasValue)
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai.Value);
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nhanVien = new NhanVien
                {
                    NhanVienID = reader.GetInt32(0),
                    HoTen = reader.GetString(1),
                    NgaySinh = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    DiaChi = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SDT = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ChucVuID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    TrangThai = reader.GetBoolean(7),
                    HinhAnh = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                
                if (!reader.IsDBNull(9))
                {
                    nhanVien.ChucVu = new ChucVu { TenChucVu = reader.GetString(9) };
                }
                
                if (!reader.IsDBNull(10))
                {
                    nhanVien.TaiKhoan = new TaiKhoan { Username = reader.GetString(10) };
                }
                
                result.Add(nhanVien);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhanVien: {ex.Message}");
        }
        
        return result;
    }

    public async Task<NhanVien?> GetNhanVienByIdAsync(int nhanVienId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT nv.NhanVienID, nv.HoTen, nv.NgaySinh, nv.DiaChi, nv.SDT, nv.Email,
                              nv.ChucVuID, nv.TrangThai, nv.HinhAnh, cv.TenChucVu
                       FROM NhanVien nv
                       LEFT JOIN ChucVu cv ON nv.ChucVuID = cv.ChucVuID
                       WHERE nv.NhanVienID = @NhanVienID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var nhanVien = new NhanVien
                {
                    NhanVienID = reader.GetInt32(0),
                    HoTen = reader.GetString(1),
                    NgaySinh = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    DiaChi = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SDT = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ChucVuID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    TrangThai = reader.GetBoolean(7),
                    HinhAnh = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                
                if (!reader.IsDBNull(9))
                {
                    nhanVien.ChucVu = new ChucVu { TenChucVu = reader.GetString(9) };
                }
                
                return nhanVien;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting NhanVien: {ex.Message}");
        }
        
        return null;
    }

    public async Task<int> SaveNhanVienAsync(NhanVien nhanVien)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            string sql;
            if (nhanVien.NhanVienID == 0)
            {
                sql = @"INSERT INTO NhanVien (HoTen, NgaySinh, DiaChi, SDT, Email, ChucVuID, TrangThai, HinhAnh)
                       VALUES (@HoTen, @NgaySinh, @DiaChi, @SDT, @Email, @ChucVuID, @TrangThai, @HinhAnh);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE NhanVien 
                       SET HoTen = @HoTen, NgaySinh = @NgaySinh, DiaChi = @DiaChi, 
                           SDT = @SDT, Email = @Email, ChucVuID = @ChucVuID, 
                           TrangThai = @TrangThai, HinhAnh = @HinhAnh
                       WHERE NhanVienID = @NhanVienID;
                       SELECT @NhanVienID;";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HoTen", nhanVien.HoTen);
            cmd.Parameters.AddWithValue("@NgaySinh", nhanVien.NgaySinh ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DiaChi", nhanVien.DiaChi ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SDT", nhanVien.SDT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", nhanVien.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ChucVuID", nhanVien.ChucVuID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrangThai", nhanVien.TrangThai);
            cmd.Parameters.AddWithValue("@HinhAnh", nhanVien.HinhAnh ?? (object)DBNull.Value);
            
            if (nhanVien.NhanVienID > 0)
            {
                cmd.Parameters.AddWithValue("@NhanVienID", nhanVien.NhanVienID);
            }
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NhanVien: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateNhanVienTrangThaiAsync(int nhanVienId, bool trangThai)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "UPDATE NhanVien SET TrangThai = @TrangThai WHERE NhanVienID = @NhanVienID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TrangThai", trangThai);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating NhanVien TrangThai: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteNhanVienAsync(int nhanVienId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        
        try
        {
            // Xóa TàiKhoản trước
            var deleteAccountSql = "DELETE FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var deleteAccountCmd = new SqlCommand(deleteAccountSql, conn, transaction);
            deleteAccountCmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            await deleteAccountCmd.ExecuteNonQueryAsync();
            
            // Sau đó xóa NhânViên
            var sql = "DELETE FROM NhanVien WHERE NhanVienID = @NhanVienID";
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            
            await cmd.ExecuteNonQueryAsync();
            
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error deleting NhanVien: {ex.Message}");
            return false;
        }
    }

    public async Task<TaiKhoan?> GetTaiKhoanByNhanVienIDAsync(int nhanVienId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "SELECT TaiKhoanID, NhanVienID, Username, Password, TrangThai FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TaiKhoan
                {
                    TaiKhoanID = reader.GetInt32(0),
                    NhanVienID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Username = reader.GetString(2),
                    Password = reader.GetString(3),
                    TrangThai = reader.GetBoolean(4)
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting TaiKhoan: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CreateTaiKhoanForNhanVienAsync(int nhanVienId, string username, string password)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var checkSql = "SELECT COUNT(*) FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            string sql;
            if (count > 0)
            {
                sql = @"UPDATE TaiKhoan SET Username = @Username, Password = @Password WHERE NhanVienID = @NhanVienID";
            }
            else
            {
                sql = @"INSERT INTO TaiKhoan (NhanVienID, Username, Password, TrangThai)
                         VALUES (@NhanVienID, @Username, @Password, 1)";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating TaiKhoan: {ex.Message}");
            return false;
        }
    }

    public async Task<int> GetNhanVienCountAsync(bool? trangThai = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "SELECT COUNT(*) FROM NhanVien";
            if (trangThai.HasValue)
            {
                sql += " WHERE TrangThai = @TrangThai";
            }
            
            using var cmd = new SqlCommand(sql, conn);
            if (trangThai.HasValue)
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai.Value);
            }
            
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error counting NhanVien: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> UpdateNhanVienAvatarAsync(int nhanVienId, string hinhAnh)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "UPDATE NhanVien SET HinhAnh = @HinhAnh WHERE NhanVienID = @NhanVienID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HinhAnh", hinhAnh);
            cmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating NhanVien avatar: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Pizza
    public async Task<List<LoaiHangHoa>> GetLoaiHangHoasAsync()
    {
        var result = new List<LoaiHangHoa>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT LoaiHangHoaID, TenLoaiHangHoa FROM LoaiHangHoa", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LoaiHangHoa
                {
                    LoaiHangHoaID = reader.GetString(0),
                    TenLoaiHangHoa = reader.IsDBNull(1) ? null : reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading LoaiHangHoa: {ex.Message}");
        }
        return result;
    }

    public async Task<List<Pizza>> GetPizzasAsync()
    {
        var result = new List<Pizza>();

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT hh.MaHangHoa, ISNULL(hh.TenHangHoa, '') AS TenPizza,
                               hh.HinhAnh, gts.SizeID,
                               ISNULL(ds.TenSize, gts.SizeID) AS KichThuoc,
                               ISNULL(gts.GiaBan, 0) AS GiaBan,
                               ISNULL(hh.TinhTrang, 1) AS TinhTrang,
                               hh.LoaiHangHoaID, lhh.TenLoaiHangHoa,
                               hh.DonViID, dvt.TenDonVi
                        FROM HangHoa hh
                        LEFT JOIN GiaTheo_Size gts ON hh.MaHangHoa = gts.MaHangHoa
                        LEFT JOIN DoanhMuc_Size ds ON gts.SizeID = ds.SizeID
                        LEFT JOIN LoaiHangHoa lhh ON hh.LoaiHangHoaID = lhh.LoaiHangHoaID
                        LEFT JOIN DonViTinh dvt ON hh.DonViID = dvt.DonViID";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Pizza
                {
                    PizzaID = 0,
                    MaPizza = reader.GetString(0),
                    TenPizza = reader.GetString(1),
                    HinhAnh = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SizeID = reader.IsDBNull(3) ? null : reader.GetString(3),
                    KichThuoc = reader.IsDBNull(4) ? "M" : reader.GetString(4),
                    GiaBan = reader.GetDecimal(5),
                    TrangThai = !reader.IsDBNull(6) && Convert.ToBoolean(reader.GetValue(6)),
                    LoaiHangHoaID = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LoaiMonAn = reader.IsDBNull(8) ? null : reader.GetString(8),
                    DonViID = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    TenDonVi = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting Pizzas from HangHoa: {ex.Message}");
        }

        return result;
    }

    public async Task<Pizza?> GetPizzaByIdAsync(int pizzaId)
    {
        // Không sử dụng với schema dựa trên HàngHóa (không có bảng Pizza)
        return null;
    }

    public async Task<bool> SavePizzaAsync(Pizza pizza)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            if (string.IsNullOrEmpty(pizza.MaPizza))
                return false;

            // Kiểm tra nếu HàngHóa đã tồn tại
            using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM HangHoa WHERE MaHangHoa = @Ma", conn);
            checkCmd.Parameters.AddWithValue("@Ma", pizza.MaPizza);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (exists)
            {
                // Cập nhật Hàng hóa
                using var cmdHH = new SqlCommand(
                    @"UPDATE HangHoa SET TenHangHoa=@Ten, HinhAnh=@Anh, TinhTrang=@TT, LoaiHangHoaID=@Loai, DonViID=@DVT WHERE MaHangHoa=@Ma", conn);
                cmdHH.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                cmdHH.Parameters.AddWithValue("@Ten", pizza.TenPizza);
                cmdHH.Parameters.AddWithValue("@Anh", (object?)pizza.HinhAnh ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@TT", pizza.TrangThai);
                cmdHH.Parameters.AddWithValue("@Loai", (object?)pizza.LoaiHangHoaID ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@DVT", (object?)pizza.DonViID ?? DBNull.Value);
                await cmdHH.ExecuteNonQueryAsync();
            }
            else
            {
                // INSERT HangHoa
                using var cmdHH = new SqlCommand(
                    @"INSERT INTO HangHoa (MaHangHoa, TenHangHoa, HinhAnh, TinhTrang, LoaiHangHoaID, DonViID) VALUES (@Ma, @Ten, @Anh, @TT, @Loai, @DVT)", conn);
                cmdHH.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                cmdHH.Parameters.AddWithValue("@Ten", pizza.TenPizza);
                cmdHH.Parameters.AddWithValue("@Anh", (object?)pizza.HinhAnh ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@TT", pizza.TrangThai);
                cmdHH.Parameters.AddWithValue("@Loai", (object?)pizza.LoaiHangHoaID ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@DVT", (object?)pizza.DonViID ?? DBNull.Value);
                await cmdHH.ExecuteNonQueryAsync();
            }

            // Resolve SizeID from KichThuoc if not set
            var sizeId = pizza.SizeID;
            if (string.IsNullOrEmpty(sizeId))
            {
                using var sizeCmd = new SqlCommand(
                    "SELECT TOP 1 SizeID FROM DoanhMuc_Size WHERE TenSize = @K OR SizeID = @K", conn);
                sizeCmd.Parameters.AddWithValue("@K", pizza.KichThuoc);
                var sizeResult = await sizeCmd.ExecuteScalarAsync();
                sizeId = sizeResult?.ToString();
            }

            if (!string.IsNullOrEmpty(sizeId))
            {
                // UPSERT GiaTheo_Size
                using var checkSize = new SqlCommand(
                    "SELECT COUNT(1) FROM GiaTheo_Size WHERE MaHangHoa=@Ma AND SizeID=@S", conn);
                checkSize.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                checkSize.Parameters.AddWithValue("@S", sizeId);
                var sizeExists = Convert.ToInt32(await checkSize.ExecuteScalarAsync()) > 0;

                if (sizeExists)
                {
                    using var cmdGia = new SqlCommand(
                        "UPDATE GiaTheo_Size SET GiaBan=@Gia WHERE MaHangHoa=@Ma AND SizeID=@S", conn);
                    cmdGia.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                    cmdGia.Parameters.AddWithValue("@S", sizeId);
                    cmdGia.Parameters.AddWithValue("@Gia", pizza.GiaBan);
                    await cmdGia.ExecuteNonQueryAsync();
                }
                else
                {
                    using var cmdGia = new SqlCommand(
                        "INSERT INTO GiaTheo_Size (MaHangHoa, SizeID, GiaBan) VALUES (@Ma, @S, @Gia)", conn);
                    cmdGia.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                    cmdGia.Parameters.AddWithValue("@S", sizeId);
                    cmdGia.Parameters.AddWithValue("@Gia", pizza.GiaBan);
                    await cmdGia.ExecuteNonQueryAsync();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving Pizza: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePizzaAsync(int pizzaId)
    {
        // Không sử dụng với schema dựa trên HàngHóa
        return false;
    }

    public async Task<bool> DeletePizzaByMaAsync(string maHangHoa)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("UPDATE HangHoa SET TinhTrang = 0 WHERE MaHangHoa = @Ma", conn);
            cmd.Parameters.AddWithValue("@Ma", maHangHoa);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting Pizza: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> TogglePizzaTrangThaiAsync(string maHangHoa, bool newStatus)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("UPDATE HangHoa SET TinhTrang = @TT WHERE MaHangHoa = @Ma", conn);
            cmd.Parameters.AddWithValue("@TT", newStatus);
            cmd.Parameters.AddWithValue("@Ma", maHangHoa);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling Pizza status: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Công Thức
    public async Task<List<CongThuc>> GetCongThucsAsync(int pizzaId)
    {
        var result = new List<CongThuc>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ct.CongThucID, ct.PizzaID, ct.NguyenLieuID, ct.SoLuong, ct.DonViID,
                              nl.TenNguyenLieu, dv.TenDonVi
                       FROM CongThuc ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       WHERE ct.PizzaID = @PizzaID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PizzaID", pizzaId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ct = new CongThuc
                {
                    CongThucID = reader.GetInt32(0),
                    PizzaID = reader.GetInt32(1),
                    NguyenLieuID = reader.GetInt32(2),
                    SoLuong = reader.GetDecimal(3),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                };
                ct.NguyenLieu = new NguyenLieu
                {
                    NguyenLieuID = ct.NguyenLieuID,
                    TenNguyenLieu = reader.IsDBNull(5) ? "" : reader.GetString(5)
                };
                if (!reader.IsDBNull(6))
                {
                    ct.DonViTinh = new DonViTinh
                    {
                        DonViID = ct.DonViID ?? 0,
                        TenDonVi = reader.GetString(6)
                    };
                }
                result.Add(ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting CongThucs: {ex.Message}");
        }
        return result;
    }

    public async Task<bool> SaveCongThucAsync(CongThuc congThuc)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            string sql;
            if (congThuc.CongThucID == 0)
            {
                sql = @"INSERT INTO CongThuc (PizzaID, NguyenLieuID, SoLuong, DonViID) 
                       VALUES (@PizzaID, @NguyenLieuID, @SoLuong, @DonViID);
                       SELECT SCOPE_IDENTITY();";
            }
            else
            {
                sql = @"UPDATE CongThuc SET NguyenLieuID=@NguyenLieuID, SoLuong=@SoLuong, DonViID=@DonViID 
                       WHERE CongThucID=@CongThucID";
            }
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PizzaID", congThuc.PizzaID);
            cmd.Parameters.AddWithValue("@NguyenLieuID", congThuc.NguyenLieuID);
            cmd.Parameters.AddWithValue("@SoLuong", congThuc.SoLuong);
            cmd.Parameters.AddWithValue("@DonViID", (object?)congThuc.DonViID ?? DBNull.Value);

            if (congThuc.CongThucID > 0)
            {
                cmd.Parameters.AddWithValue("@CongThucID", congThuc.CongThucID);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    congThuc.CongThucID = Convert.ToInt32(result);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving CongThuc: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCongThucAsync(int congThucId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM CongThuc WHERE CongThucID = @CongThucID", conn);
            cmd.Parameters.AddWithValue("@CongThucID", congThucId);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting CongThuc: {ex.Message}");
            return false;
        }
    }

    public async Task<decimal> CalculateGiaVonAsync(int pizzaId)
    {
        // Không sử dụng với schema dựa trên HàngHóa
        return 0;
    }

    public async Task<decimal> CalculateGiaVonByMaAsync(string maHangHoa, string sizeId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ISNULL(SUM(ct.SoLuong * ISNULL(
                           (SELECT TOP 1 ctn.DonGia FROM CT_PhieuNhap ctn 
                            INNER JOIN PhieuNhap pn ON ctn.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctn.NguyenLieuID = ct.NguyenLieuID AND pn.TrangThai = 2
                            ORDER BY pn.NgayNhap DESC), 
                           ISNULL((SELECT TOP 1 nnc.GiaNhap FROM NguyenLieuNhaCungCap nnc 
                                   WHERE nnc.NguyenLieuID = ct.NguyenLieuID 
                                   ORDER BY nnc.NgayCapNhat DESC), 0)
                       )), 0) AS GiaVon
                       FROM CongThuc_Pizza ct WHERE ct.MaHangHoa = @Ma AND ct.SizeID = @S";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Ma", maHangHoa);
            cmd.Parameters.AddWithValue("@S", sizeId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculating GiaVon: {ex.Message}");
            return 0;
        }
    }
    #endregion

    #region Đơn Hàng
    public async Task<List<DonHang>> GetDonHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new List<DonHang>();
        var existingMaDonHangs = new HashSet<string>();

        // 1) Try loading from DonHang table (may not exist)
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT dh.DonHangID, dh.MaDonHang, dh.NhanVienID, dh.NgayTao, dh.TongTien, 
                              dh.GiamGia, dh.ThanhToan, dh.PhuongThucTT, dh.TrangThai, dh.GhiChu,
                              nv.HoTen
                       FROM DonHang dh
                       LEFT JOIN NhanVien nv ON dh.NhanVienID = nv.NhanVienID
                       WHERE 1=1";
            if (fromDate.HasValue)
                sql += " AND dh.NgayTao >= @FromDate";
            if (toDate.HasValue)
                sql += " AND dh.NgayTao <= @ToDate";
            sql += " ORDER BY dh.NgayTao DESC";

            using var cmd = new SqlCommand(sql, conn);
            if (fromDate.HasValue)
                cmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
            if (toDate.HasValue)
                cmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dh = new DonHang
                {
                    DonHangID = reader.GetInt32(0),
                    MaDonHang = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NgayTao = reader.GetDateTime(3),
                    TongTien = reader.GetDecimal(4),
                    GiamGia = reader.GetDecimal(5),
                    ThanhToan = reader.GetDecimal(6),
                    PhuongThucTT = reader.IsDBNull(7) ? "Tiền mặt" : reader.GetString(7),
                    TrangThai = reader.GetByte(8),
                    GhiChu = reader.IsDBNull(9) ? null : reader.GetString(9)
                };
                if (!reader.IsDBNull(10))
                {
                    dh.NhanVien = new NhanVien
                    {
                        NhanVienID = dh.NhanVienID ?? 0,
                        HoTen = reader.GetString(10)
                    };
                }
                result.Add(dh);
                if (dh.MaDonHang != null)
                    existingMaDonHangs.Add(dh.MaDonHang);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DonHangs from DonHang table: {ex.Message}");
        }

        // 2) Always load from PhieuBanHang (the active data source)
        try
        {
            using var conn2 = GetConnection();
            await conn2.OpenAsync();
            var sql2 = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, ISNULL(pb.TongTien, 0),
                                nv.HoTen, pb.GhiChu, pb.PhuongThucTT
                         FROM PhieuBanHang pb
                         LEFT JOIN NhanVien nv ON pb.NhanVienBanID = nv.NhanVienID
                         WHERE 1=1";
            if (fromDate.HasValue)
                sql2 += " AND pb.NgayBan >= @FromDate";
            if (toDate.HasValue)
                sql2 += " AND pb.NgayBan <= @ToDate";
            sql2 += " ORDER BY pb.NgayBan DESC";

            using var cmd2 = new SqlCommand(sql2, conn2);
            if (fromDate.HasValue)
                cmd2.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
            if (toDate.HasValue)
                cmd2.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1));

            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var maPhieu = reader2.IsDBNull(0) ? null : reader2.GetString(0);
                if (maPhieu != null && existingMaDonHangs.Contains(maPhieu))
                    continue; // skip duplicates already loaded from DonHang table

                var dh = new DonHang
                {
                    DonHangID = 0,
                    MaDonHang = maPhieu,
                    NhanVienID = reader2.IsDBNull(1) ? null : reader2.GetInt32(1),
                    NgayTao = reader2.IsDBNull(2) ? DateTime.Now : reader2.GetDateTime(2),
                    TongTien = reader2.GetDecimal(3),
                    GiamGia = 0,
                    ThanhToan = reader2.GetDecimal(3),
                    PhuongThucTT = reader2.IsDBNull(6) ? "Tiền mặt" : reader2.GetString(6),
                    TrangThai = 2, // Hoàn thành
                    GhiChu = reader2.IsDBNull(5) ? "Bán hàng tại quầy" : reader2.GetString(5)
                };
                if (!reader2.IsDBNull(4))
                {
                    dh.NhanVien = new NhanVien
                    {
                        NhanVienID = dh.NhanVienID ?? 0,
                        HoTen = reader2.GetString(4)
                    };
                }
                result.Add(dh);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DonHangs from PhieuBanHang: {ex.Message}");
        }

        // Sắp xếp kết quả kết hợp theo ngày giảm dần
        result.Sort((a, b) => b.NgayTao.CompareTo(a.NgayTao));
        return result;
    }

    public async Task<DonHang?> GetDonHangByIdAsync(int donHangId)
    {
        try
        {
            var donHangs = await GetDonHangsAsync();
            return donHangs.FirstOrDefault(d => d.DonHangID == donHangId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DonHang by ID: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CT_DonHang>> GetDonHangChiTietsAsync(int donHangId)
    {
        var result = new List<CT_DonHang>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ct.ChiTietID, ct.DonHangID, ct.PizzaID, ct.SoLuong, ct.DonGia, ct.ThanhTien,
                               p.TenPizza, p.KichThuoc, p.HinhAnh
                        FROM CT_DonHang ct
                        LEFT JOIN Pizza p ON ct.PizzaID = p.PizzaID
                        WHERE ct.DonHangID = @DonHangID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DonHangID", donHangId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ct = new CT_DonHang
                {
                    ChiTietID = reader.GetInt32(0),
                    DonHangID = reader.GetInt32(1),
                    PizzaID = reader.GetInt32(2),
                    SoLuong = reader.GetInt32(3),
                    DonGia = reader.GetDecimal(4),
                    ThanhTien = reader.GetDecimal(5)
                };
                if (!reader.IsDBNull(6))
                {
                    ct.Pizza = new Pizza
                    {
                        PizzaID = ct.PizzaID,
                        TenPizza = reader.GetString(6),
                        KichThuoc = reader.IsDBNull(7) ? "M" : reader.GetString(7),
                        HinhAnh = reader.IsDBNull(8) ? null : reader.GetString(8)
                    };
                }
                result.Add(ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DonHang ChiTiets: {ex.Message}");
        }
        return result;
    }

    public async Task<int> SaveDonHangAsync(DonHang donHang, List<CT_DonHang> chiTiets)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Thêm ĐơnHàng
                var sql = @"INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
                           VALUES (@MaDonHang, @NhanVienID, @NgayTao, @TongTien, @GiamGia, @ThanhToan, @PhuongThucTT, @TrangThai, @GhiChu);
                           SELECT SCOPE_IDENTITY();";
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaDonHang", (object?)donHang.MaDonHang ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienID", (object?)donHang.NhanVienID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayTao", donHang.NgayTao);
                cmd.Parameters.AddWithValue("@TongTien", donHang.TongTien);
                cmd.Parameters.AddWithValue("@GiamGia", donHang.GiamGia);
                cmd.Parameters.AddWithValue("@ThanhToan", donHang.ThanhToan);
                cmd.Parameters.AddWithValue("@PhuongThucTT", donHang.PhuongThucTT);
                cmd.Parameters.AddWithValue("@TrangThai", donHang.TrangThai);
                cmd.Parameters.AddWithValue("@GhiChu", (object?)donHang.GhiChu ?? DBNull.Value);

                var donHangId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                donHang.DonHangID = donHangId;

                // Thêm CT_ĐơnHàng
                foreach (var ct in chiTiets)
                {
                    var ctSql = @"INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien)
                                 VALUES (@DonHangID, @PizzaID, @SoLuong, @DonGia, @ThanhTien)";
                    using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                    ctCmd.Parameters.AddWithValue("@DonHangID", donHangId);
                    ctCmd.Parameters.AddWithValue("@PizzaID", ct.PizzaID);
                    ctCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
                    ctCmd.Parameters.AddWithValue("@DonGia", ct.DonGia);
                    ctCmd.Parameters.AddWithValue("@ThanhTien", ct.ThanhTien);
                    await ctCmd.ExecuteNonQueryAsync();
                }

                // Trừ nguyên liệu từ TồnKho dựa trên CôngThức
                foreach (var ct in chiTiets)
                {
                    var congThucs = await GetCongThucsInternalAsync(conn, transaction, ct.PizzaID);
                    foreach (var recipe in congThucs)
                    {
                        var deductSql = @"UPDATE TonKho SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                         WHERE NguyenLieuID = @NguyenLieuID AND SoLuongTon >= @SoLuong";
                        using var deductCmd = new SqlCommand(deductSql, conn, transaction);
                        deductCmd.Parameters.AddWithValue("@SoLuong", recipe.SoLuong * ct.SoLuong);
                        deductCmd.Parameters.AddWithValue("@NguyenLieuID", recipe.NguyenLieuID);
                        await deductCmd.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return donHangId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving DonHang: {ex.Message}");
            return 0;
        }
    }

    private async Task<List<CongThuc>> GetCongThucsInternalAsync(SqlConnection conn, SqlTransaction transaction, int pizzaId)
    {
        var result = new List<CongThuc>();
        var sql = "SELECT CongThucID, PizzaID, NguyenLieuID, SoLuong, DonViID FROM CongThuc WHERE PizzaID = @PizzaID";
        using var cmd = new SqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("@PizzaID", pizzaId);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new CongThuc
            {
                CongThucID = reader.GetInt32(0),
                PizzaID = reader.GetInt32(1),
                NguyenLieuID = reader.GetInt32(2),
                SoLuong = reader.GetDecimal(3),
                DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            });
        }
        return result;
    }

    public async Task<bool> UpdateDonHangStatusAsync(int donHangId, byte trangThai)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("UPDATE DonHang SET TrangThai = @TrangThai WHERE DonHangID = @DonHangID", conn);
            cmd.Parameters.AddWithValue("@TrangThai", trangThai);
            cmd.Parameters.AddWithValue("@DonHangID", donHangId);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating DonHang status: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteDonHangAsync(DonHang donHang)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (donHang.DonHangID > 0)
                {
                    // Xóa từ bảng DonHang (xóa CT_DonHang trước)
                    using var delCt = new SqlCommand("DELETE FROM CT_DonHang WHERE DonHangID = @DonHangID", conn, transaction);
                    delCt.Parameters.AddWithValue("@DonHangID", donHang.DonHangID);
                    await delCt.ExecuteNonQueryAsync();

                    using var delDh = new SqlCommand("DELETE FROM DonHang WHERE DonHangID = @DonHangID", conn, transaction);
                    delDh.Parameters.AddWithValue("@DonHangID", donHang.DonHangID);
                    await delDh.ExecuteNonQueryAsync();
                }
                else if (!string.IsNullOrEmpty(donHang.MaDonHang))
                {
                    // Xóa từ bảng PhieuBanHang (xóa CT trước)
                    using var delCt = new SqlCommand("DELETE FROM CT_PhieuBan WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                    delCt.Parameters.AddWithValue("@MaPhieu", donHang.MaDonHang);
                    await delCt.ExecuteNonQueryAsync();

                    using var delPb = new SqlCommand("DELETE FROM PhieuBanHang WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                    delPb.Parameters.AddWithValue("@MaPhieu", donHang.MaDonHang);
                    await delPb.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting DonHang: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Thống kê bán hàng
    public async Task<decimal> GetDoanhThuAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            // Doanh thu chính từ PhieuBanHang (nguồn dữ liệu bán hàng chính)
            // + DonHang records chưa có trong PhieuBanHang
            var sql = @"SELECT ISNULL(SUM(ThanhToan), 0) FROM (
                           SELECT ISNULL(TongTien, 0) AS ThanhToan FROM PhieuBanHang 
                           WHERE NgayBan >= @FromDate AND NgayBan < @ToDate
                           UNION ALL
                           SELECT ThanhToan FROM DonHang 
                           WHERE TrangThai = 2 AND NgayTao >= @FromDate AND NgayTao < @ToDate
                           AND (MaDonHang IS NULL OR MaDonHang NOT IN (SELECT MaPhieuBan FROM PhieuBanHang WHERE MaPhieuBan IS NOT NULL))
                       ) AS combined";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DoanhThu: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetTotalDonHangCountAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            // Đếm đơn hàng chính từ PhieuBanHang + DonHang chưa có trong PhieuBanHang
            var sql = @"SELECT COUNT(*) FROM (
                           SELECT MaPhieuBan FROM PhieuBanHang 
                           WHERE NgayBan >= @FromDate AND NgayBan < @ToDate
                           UNION ALL
                           SELECT MaDonHang FROM DonHang 
                           WHERE TrangThai = 2 AND NgayTao >= @FromDate AND NgayTao < @ToDate
                           AND (MaDonHang IS NULL OR MaDonHang NOT IN (SELECT MaPhieuBan FROM PhieuBanHang WHERE MaPhieuBan IS NOT NULL))
                       ) AS combined";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DonHang count: {ex.Message}");
            return 0;
        }
    }

    public async Task<decimal> GetTotalLoiNhuanAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            // Lợi nhuận = Doanh thu - Chi phí NL tiêu hao
            var doanhThu = await GetDoanhThuAsync(fromDate, toDate);
            var chiPhi = await GetChiPhiNguyenLieuAsync(fromDate, toDate);
            return doanhThu - chiPhi;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting LoiNhuan: {ex.Message}");
            return 0;
        }
    }

    public async Task<decimal> GetChiPhiNguyenLieuAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            // Chi phí NL tiêu hao = SUM(lượng NL tiêu hao per pizza × đơn giá nhập gần nhất × số lượng mua)
            // Bao gồm: CongThuc_Pizza (nhân) + QuyDinh_Bot (bột) + QuyDinh_Vien (viền)
            var sql = @"
                ;WITH SalesInPeriod AS (
                    SELECT ct.MaHangHoa, ct.SizeID, ct.MaDeBanh, ct.SoLuong
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan <= @ToDate
                ),
                -- Bước 1: Nguyên liệu từ CongThuc_Pizza (nhân bánh)
                NhanCost AS (
                    SELECT s.MaHangHoa, s.SizeID, s.MaDeBanh, s.SoLuong AS SoLuongMua,
                           ctp.NguyenLieuID, CAST(ctp.SoLuong AS decimal(18,4)) AS SoLuongNL
                    FROM SalesInPeriod s
                    INNER JOIN CongThuc_Pizza ctp ON s.MaHangHoa = ctp.MaHangHoa AND s.SizeID = ctp.SizeID
                ),
                -- Bước 2: Nguyên liệu bột mì từ QuyDinh_Bot
                BotCost AS (
                    SELECT s.MaHangHoa, s.SizeID, s.MaDeBanh, s.SoLuong AS SoLuongMua,
                           nl.NguyenLieuID, CAST(qb.TrongLuongBot AS decimal(18,4)) AS SoLuongNL
                    FROM SalesInPeriod s
                    INNER JOIN DoanhMuc_De dd ON s.MaDeBanh = dd.MaDeBanh
                    INNER JOIN QuyDinh_Bot qb ON s.SizeID = qb.SizeID AND dd.LoaiCotBanh = qb.LoaiCotBanh
                    CROSS APPLY (SELECT TOP 1 NguyenLieuID FROM NguyenLieu WHERE TenNguyenLieu LIKE N'%Bột mì%') nl
                    WHERE s.MaDeBanh IS NOT NULL
                ),
                -- Bước 3: Nguyên liệu viền từ QuyDinh_Vien
                VienCost AS (
                    SELECT s.MaHangHoa, s.SizeID, s.MaDeBanh, s.SoLuong AS SoLuongMua,
                           qv.NguyenLieuID, CAST(qv.SoLuongVien AS decimal(18,4)) AS SoLuongNL
                    FROM SalesInPeriod s
                    INNER JOIN QuyDinh_Vien qv ON s.MaDeBanh = qv.MaDeBanh AND s.SizeID = qv.SizeID
                    WHERE s.MaDeBanh IS NOT NULL
                ),
                -- Gom tất cả
                AllCosts AS (
                    SELECT NguyenLieuID, SoLuongMua, SoLuongNL FROM NhanCost
                    UNION ALL
                    SELECT NguyenLieuID, SoLuongMua, SoLuongNL FROM BotCost
                    UNION ALL
                    SELECT NguyenLieuID, SoLuongMua, SoLuongNL FROM VienCost
                ),
                -- Lấy đơn giá nhập gần nhất cho mỗi nguyên liệu
                LatestPrice AS (
                    SELECT ctn.NguyenLieuID, ctn.DonGia
                    FROM CT_PhieuNhap ctn
                    INNER JOIN (
                        SELECT NguyenLieuID, MAX(ChiTietID) AS MaxID
                        FROM CT_PhieuNhap
                        GROUP BY NguyenLieuID
                    ) latest ON ctn.NguyenLieuID = latest.NguyenLieuID AND ctn.ChiTietID = latest.MaxID
                )
                SELECT ISNULL(SUM(ac.SoLuongNL * ac.SoLuongMua * ISNULL(lp.DonGia, 0)), 0)
                FROM AllCosts ac
                LEFT JOIN LatestPrice lp ON ac.NguyenLieuID = lp.NguyenLieuID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting ChiPhiNguyenLieu: {ex.Message}");
            return 0;
        }
    }

    public async Task<List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)>> GetTopPizzasAsync(DateTime fromDate, DateTime toDate, int top = 5)
    {
        var result = new List<(string, string, int, decimal)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            // Lấy từ CT_PhieuBan (nguồn dữ liệu bán hàng chính)
            var sql = @"SELECT TOP (@Top) 
                           ISNULL(hh.TenHangHoa, ct.MaHangHoa) AS TenPizza, 
                           ISNULL(ds.TenSize, ct.SizeID) AS KichThuoc, 
                           SUM(ISNULL(ct.SoLuong, 0)) AS SoLuongBan, 
                           SUM(ISNULL(ct.ThanhTien, 0)) AS DoanhThu
                       FROM CT_PhieuBan ct
                       INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                       LEFT JOIN HangHoa hh ON ct.MaHangHoa = hh.MaHangHoa
                       LEFT JOIN DoanhMuc_Size ds ON ct.SizeID = ds.SizeID
                       WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                       GROUP BY ISNULL(hh.TenHangHoa, ct.MaHangHoa), ISNULL(ds.TenSize, ct.SizeID)
                       ORDER BY SoLuongBan DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Top", top);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDecimal(3)
                ));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting TopPizzas: {ex.Message}");
        }
        return result;
    }

    public async Task<List<DonHang>> GetRecentDonHangsAsync(int top = 10)
    {
        var donHangs = new List<DonHang>();
        var existingMaDonHangs = new HashSet<string>();

        // 1) Lấy từ bảng DonHang
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT TOP (@Top) dh.DonHangID, dh.MaDonHang, dh.NhanVienID, dh.NgayTao, 
                               dh.TongTien, dh.GiamGia, dh.ThanhToan, dh.PhuongThucTT, dh.TrangThai, dh.GhiChu,
                               nv.HoTen
                       FROM DonHang dh
                       LEFT JOIN NhanVien nv ON dh.NhanVienID = nv.NhanVienID
                       ORDER BY dh.NgayTao DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Top", top);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dh = new DonHang
                {
                    DonHangID = reader.GetInt32(0),
                    MaDonHang = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NgayTao = reader.GetDateTime(3),
                    TongTien = reader.GetDecimal(4),
                    GiamGia = reader.GetDecimal(5),
                    ThanhToan = reader.GetDecimal(6),
                    PhuongThucTT = reader.IsDBNull(7) ? "Tiền mặt" : reader.GetString(7),
                    TrangThai = reader.GetByte(8),
                    GhiChu = reader.IsDBNull(9) ? null : reader.GetString(9),
                    NhanVien = reader.IsDBNull(10) ? null : new NhanVien { HoTen = reader.GetString(10) }
                };
                donHangs.Add(dh);
                if (dh.MaDonHang != null)
                    existingMaDonHangs.Add(dh.MaDonHang);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting RecentDonHangs from DonHang: {ex.Message}");
        }

        // 2) Lấy từ bảng PhieuBanHang (nguồn dữ liệu bán hàng chính)
        try
        {
            using var conn2 = GetConnection();
            await conn2.OpenAsync();
            var sql2 = @"SELECT TOP (@Top) pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, 
                                ISNULL(pb.TongTien, 0), nv.HoTen, pb.GhiChu, pb.PhuongThucTT
                        FROM PhieuBanHang pb
                        LEFT JOIN NhanVien nv ON pb.NhanVienBanID = nv.NhanVienID
                        ORDER BY pb.NgayBan DESC";
            using var cmd2 = new SqlCommand(sql2, conn2);
            cmd2.Parameters.AddWithValue("@Top", top);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var maPhieu = reader2.IsDBNull(0) ? null : reader2.GetString(0);
                if (maPhieu != null && existingMaDonHangs.Contains(maPhieu))
                    continue; // skip duplicates

                donHangs.Add(new DonHang
                {
                    DonHangID = 0,
                    MaDonHang = maPhieu,
                    NhanVienID = reader2.IsDBNull(1) ? null : reader2.GetInt32(1),
                    NgayTao = reader2.IsDBNull(2) ? DateTime.Now : reader2.GetDateTime(2),
                    TongTien = reader2.GetDecimal(3),
                    GiamGia = 0,
                    ThanhToan = reader2.GetDecimal(3),
                    PhuongThucTT = reader2.IsDBNull(6) ? "Tiền mặt" : reader2.GetString(6),
                    TrangThai = 2, // Hoàn thành
                    GhiChu = reader2.IsDBNull(5) ? "Bán hàng tại quầy" : reader2.GetString(5),
                    NhanVien = reader2.IsDBNull(4) ? null : new NhanVien 
                    { 
                        NhanVienID = reader2.IsDBNull(1) ? 0 : reader2.GetInt32(1),
                        HoTen = reader2.GetString(4) 
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting RecentDonHangs from PhieuBanHang: {ex.Message}");
        }

        // Sắp xếp theo ngày mới nhất và lấy top
        donHangs.Sort((a, b) => b.NgayTao.CompareTo(a.NgayTao));
        return donHangs.Take(top).ToList();
    }
    #endregion

    #region Hàng Hóa
    public async Task<List<HangHoa>> GetHangHoasAsync()
    {
        var result = new List<HangHoa>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT hh.MaHangHoa, hh.TenHangHoa, hh.HinhAnh, hh.DonViID, hh.LoaiHangHoaID, hh.TinhTrang,
                              dv.TenDonVi, lhh.TenLoaiHangHoa
                       FROM HangHoa hh
                       LEFT JOIN DonViTinh dv ON hh.DonViID = dv.DonViID
                       LEFT JOIN LoaiHangHoa lhh ON hh.LoaiHangHoaID = lhh.LoaiHangHoaID
                       WHERE hh.TinhTrang = 1";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var hh = new HangHoa
                {
                    MaHangHoa = reader.GetString(0),
                    TenHangHoa = reader.IsDBNull(1) ? null : reader.GetString(1),
                    HinhAnh = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DonViID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    LoaiHangHoaID = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TinhTrang = reader.IsDBNull(5) ? null : reader.GetBoolean(5)
                };
                if (!reader.IsDBNull(6))
                    hh.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(6) };
                if (!reader.IsDBNull(7))
                    hh.LoaiHangHoa = new LoaiHangHoa { TenLoaiHangHoa = reader.GetString(7) };
                result.Add(hh);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting HangHoas: {ex.Message}");
        }
        return result;
    }

    public async Task<HangHoa?> GetHangHoaByIdAsync(string maHangHoa)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT hh.MaHangHoa, hh.TenHangHoa, hh.HinhAnh, hh.DonViID, hh.LoaiHangHoaID, hh.TinhTrang,
                              dv.TenDonVi, lhh.TenLoaiHangHoa
                       FROM HangHoa hh
                       LEFT JOIN DonViTinh dv ON hh.DonViID = dv.DonViID
                       LEFT JOIN LoaiHangHoa lhh ON hh.LoaiHangHoaID = lhh.LoaiHangHoaID
                       WHERE hh.MaHangHoa = @MaHangHoa";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHangHoa", maHangHoa);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var hh = new HangHoa
                {
                    MaHangHoa = reader.GetString(0),
                    TenHangHoa = reader.IsDBNull(1) ? null : reader.GetString(1),
                    HinhAnh = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DonViID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    LoaiHangHoaID = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TinhTrang = reader.IsDBNull(5) ? null : reader.GetBoolean(5)
                };
                if (!reader.IsDBNull(6))
                    hh.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(6) };
                if (!reader.IsDBNull(7))
                    hh.LoaiHangHoa = new LoaiHangHoa { TenLoaiHangHoa = reader.GetString(7) };
                return hh;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting HangHoa by ID: {ex.Message}");
        }
        return null;
    }

    public async Task<Dictionary<string, List<string>>> GetOutOfStockIngredientsByHangHoaAsync(IEnumerable<string> maHangHoas)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var maList = maHangHoas?
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (maList.Count == 0)
            return result;

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var parameters = maList.Select((m, i) => new SqlParameter($"@Ma{i}", m)).ToArray();
            var inClause = string.Join(",", parameters.Select(p => p.ParameterName));

            var sql = $@"
                SELECT req.MaHangHoa,
                       req.NguyenLieuID,
                       req.SoLuong,
                       req.DonViID,
                       req.TenNguyenLieu,
                       req.SoLuongTon
                FROM (
                    -- Nguyên liệu nhân bánh
                    SELECT gs.MaHangHoa,
                           ctp.NguyenLieuID,
                           CAST(ctp.SoLuong AS decimal(18,4)) AS SoLuong,
                           ctp.DonViID,
                           nl.TenNguyenLieu,
                           ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                    FROM GiaTheo_Size gs
                    INNER JOIN CongThuc_Pizza ctp
                        ON gs.MaHangHoa = ctp.MaHangHoa
                       AND gs.SizeID = ctp.SizeID
                    INNER JOIN NguyenLieu nl
                        ON ctp.NguyenLieuID = nl.NguyenLieuID
                    LEFT JOIN TonKho tk
                        ON ctp.NguyenLieuID = tk.NguyenLieuID
                    WHERE gs.MaHangHoa IN ({inClause})

                    UNION ALL

                    -- Nguyên liệu bột theo size + đế
                    SELECT gs.MaHangHoa,
                           botNl.NguyenLieuID,
                           CAST(qb.TrongLuongBot AS decimal(18,4)) AS SoLuong,
                           qb.DonViID,
                           botNl.TenNguyenLieu,
                           ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                    FROM GiaTheo_Size gs
                    INNER JOIN GiaTheo_De gtd
                        ON gs.SizeID = gtd.SizeID
                    INNER JOIN DoanhMuc_De dd
                        ON gtd.MaDeBanh = dd.MaDeBanh
                    INNER JOIN QuyDinh_Bot qb
                        ON gs.SizeID = qb.SizeID
                       AND dd.LoaiCotBanh = qb.LoaiCotBanh
                    CROSS APPLY (
                        SELECT TOP 1 nl.NguyenLieuID, nl.TenNguyenLieu
                        FROM NguyenLieu nl
                        WHERE nl.TenNguyenLieu LIKE N'%Bột mì%'
                           OR nl.TenNguyenLieu LIKE N'%Bot mi%'
                    ) botNl
                    LEFT JOIN TonKho tk
                        ON botNl.NguyenLieuID = tk.NguyenLieuID
                    WHERE gs.MaHangHoa IN ({inClause})

                    UNION ALL

                    -- Nguyên liệu viền theo size + đế
                    SELECT gs.MaHangHoa,
                           qv.NguyenLieuID,
                           CAST(qv.SoLuongVien AS decimal(18,4)) AS SoLuong,
                           qv.DonViID,
                           nl.TenNguyenLieu,
                           ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                    FROM GiaTheo_Size gs
                    INNER JOIN GiaTheo_De gtd
                        ON gs.SizeID = gtd.SizeID
                    INNER JOIN QuyDinh_Vien qv
                        ON gtd.MaDeBanh = qv.MaDeBanh
                       AND gs.SizeID = qv.SizeID
                    INNER JOIN NguyenLieu nl
                        ON qv.NguyenLieuID = nl.NguyenLieuID
                    LEFT JOIN TonKho tk
                        ON qv.NguyenLieuID = tk.NguyenLieuID
                    WHERE gs.MaHangHoa IN ({inClause})
                ) req";

            var rows = new List<(string MaHangHoa, int NguyenLieuId, decimal Required, int? DonViId, string TenNguyenLieu, decimal SoLuongTon)>();

            using var cmd = CreateCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows.Add((
                        reader.GetString(0),
                        reader.GetInt32(1),
                        reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                        reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        reader.IsDBNull(5) ? 0m : reader.GetDecimal(5)));
                }
            }

            var unitContextCache = new Dictionary<int, IngredientUnitContext>();
            var unitNameCache = new Dictionary<int, string>();
            var missingMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TenNguyenLieu))
                    continue;

                try
                {
                    var normalizedRequired = await ConvertAmountToStockUnitAsync(
                        conn,
                        null,
                        row.NguyenLieuId,
                        row.Required,
                        row.DonViId,
                        unitContextCache,
                        unitNameCache);

                    if (normalizedRequired > row.SoLuongTon)
                    {
                        if (!missingMap.TryGetValue(row.MaHangHoa, out var missingIngredients))
                        {
                            missingIngredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            missingMap[row.MaHangHoa] = missingIngredients;
                        }

                        missingIngredients.Add(row.TenNguyenLieu);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skip stock pre-check for '{row.TenNguyenLieu}': {ex.Message}");
                }
            }

            foreach (var entry in missingMap)
            {
                result[entry.Key] = entry.Value
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting out-of-stock ingredients: {ex.Message}");
        }

        return result;
    }
    #endregion

    #region DoanhMuc_Size & DoanhMuc_De
    public async Task<List<DoanhMuc_Size>> GetDoanhMucSizesAsync()
    {
        var result = new List<DoanhMuc_Size>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT SizeID, TenSize FROM DoanhMuc_Size", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DoanhMuc_Size
                {
                    SizeID = reader.GetString(0),
                    TenSize = reader.IsDBNull(1) ? null : reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DoanhMuc_Size: {ex.Message}");
        }
        return result;
    }

    public async Task<List<DoanhMuc_De>> GetDoanhMucDesAsync()
    {
        var result = new List<DoanhMuc_De>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT MaDeBanh, TenDeBanh, LoaiCotBanh FROM DoanhMuc_De", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DoanhMuc_De
                {
                    MaDeBanh = reader.GetString(0),
                    TenDeBanh = reader.IsDBNull(1) ? null : reader.GetString(1),
                    LoaiCotBanh = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DoanhMuc_De: {ex.Message}");
        }
        return result;
    }
    #endregion

    #region GiaTheo_Size & GiaTheo_De
    public async Task<List<GiaTheo_Size>> GetGiaTheoSizeByHangHoaAsync(string maHangHoa)
    {
        var result = new List<GiaTheo_Size>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT gts.MaHangHoa, gts.SizeID, gts.GiaBan, ds.TenSize
                       FROM GiaTheo_Size gts
                       LEFT JOIN DoanhMuc_Size ds ON gts.SizeID = ds.SizeID
                       WHERE gts.MaHangHoa = @MaHangHoa";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHangHoa", maHangHoa);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new GiaTheo_Size
                {
                    MaHangHoa = reader.GetString(0),
                    SizeID = reader.GetString(1),
                    GiaBan = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    DoanhMucSize = reader.IsDBNull(3) ? null : new DoanhMuc_Size
                    {
                        SizeID = reader.GetString(1),
                        TenSize = reader.GetString(3)
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GiaTheo_Size: {ex.Message}");
        }
        return result;
    }

    public async Task<List<GiaTheo_De>> GetGiaTheoDeAsync(string sizeId)
    {
        var result = new List<GiaTheo_De>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT gtd.SizeID, gtd.MaDeBanh, gtd.GiaThem, dd.TenDeBanh
                       FROM GiaTheo_De gtd
                       LEFT JOIN DoanhMuc_De dd ON gtd.MaDeBanh = dd.MaDeBanh
                       WHERE gtd.SizeID = @SizeID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SizeID", sizeId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new GiaTheo_De
                {
                    SizeID = reader.GetString(0),
                    MaDeBanh = reader.GetString(1),
                    GiaThem = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    DoanhMucDe = reader.IsDBNull(3) ? null : new DoanhMuc_De
                    {
                        MaDeBanh = reader.GetString(1),
                        TenDeBanh = reader.GetString(3)
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GiaTheo_De: {ex.Message}");
        }
        return result;
    }
    #endregion

    #region Phiếu Bán Hàng
    public async Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new List<PhieuBanHang>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, pb.TongTien,
                              nv.HoTen, pb.GhiChu, pb.PhuongThucTT
                       FROM PhieuBanHang pb
                       LEFT JOIN NhanVien nv ON pb.NhanVienBanID = nv.NhanVienID
                       WHERE 1=1";
            if (fromDate.HasValue)
                sql += " AND pb.NgayBan >= @FromDate";
            if (toDate.HasValue)
                sql += " AND pb.NgayBan <= @ToDate";
            sql += " ORDER BY pb.NgayBan DESC";

            using var cmd = new SqlCommand(sql, conn);
            if (fromDate.HasValue)
                cmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
            if (toDate.HasValue)
                cmd.Parameters.AddWithValue("@ToDate", toDate.Value.Date.AddDays(1));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var pb = new PhieuBanHang
                {
                    MaPhieuBan = reader.GetString(0),
                    NhanVienBanID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    NgayBan = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    TongTien = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5),
                    PhuongThucTT = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
                if (!reader.IsDBNull(4))
                {
                    pb.NhanVienBan = new NhanVien
                    {
                        NhanVienID = pb.NhanVienBanID ?? 0,
                        HoTen = reader.GetString(4)
                    };
                }
                result.Add(pb);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuBanHangs: {ex.Message}");
        }
        return result;
    }

    public async Task<PhieuBanHang?> GetPhieuBanHangByIdAsync(string maPhieuBan)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, pb.TongTien,
                              nv.HoTen, pb.GhiChu, pb.PhuongThucTT
                       FROM PhieuBanHang pb
                       LEFT JOIN NhanVien nv ON pb.NhanVienBanID = nv.NhanVienID
                       WHERE pb.MaPhieuBan = @MaPhieuBan";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaPhieuBan", maPhieuBan);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var pb = new PhieuBanHang
                {
                    MaPhieuBan = reader.GetString(0),
                    NhanVienBanID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    NgayBan = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    TongTien = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5),
                    PhuongThucTT = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
                if (!reader.IsDBNull(4))
                {
                    pb.NhanVienBan = new NhanVien
                    {
                        NhanVienID = pb.NhanVienBanID ?? 0,
                        HoTen = reader.GetString(4)
                    };
                }
                return pb;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuBanHang by ID: {ex.Message}");
        }
        return null;
    }

    public async Task<List<CT_PhieuBan>> GetChiTietPhieuBanAsync(string maPhieuBan)
    {
        var result = new List<CT_PhieuBan>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ct.ChiTietBanID, ct.MaPhieuBan, ct.MaHangHoa, ct.SizeID, ct.MaDeBanh, ct.SoLuong, ct.ThanhTien,
                              hh.TenHangHoa, ds.TenSize, dd.TenDeBanh, hh.HinhAnh
                       FROM CT_PhieuBan ct
                       LEFT JOIN HangHoa hh ON ct.MaHangHoa = hh.MaHangHoa
                       LEFT JOIN DoanhMuc_Size ds ON ct.SizeID = ds.SizeID
                       LEFT JOIN DoanhMuc_De dd ON ct.MaDeBanh = dd.MaDeBanh
                       WHERE ct.MaPhieuBan = @MaPhieuBan";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaPhieuBan", maPhieuBan);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ct = new CT_PhieuBan
                {
                    ChiTietBanID = reader.GetInt32(0),
                    MaPhieuBan = reader.IsDBNull(1) ? null : reader.GetString(1),
                    MaHangHoa = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SizeID = reader.IsDBNull(3) ? null : reader.GetString(3),
                    MaDeBanh = reader.IsDBNull(4) ? null : reader.GetString(4),
                    SoLuong = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ThanhTien = reader.IsDBNull(6) ? null : reader.GetDecimal(6)
                };
                if (!reader.IsDBNull(7))
                    ct.HangHoa = new HangHoa { MaHangHoa = ct.MaHangHoa!, TenHangHoa = reader.GetString(7), HinhAnh = reader.IsDBNull(10) ? null : reader.GetString(10) };
                if (!reader.IsDBNull(8))
                    ct.DoanhMucSize = new DoanhMuc_Size { SizeID = ct.SizeID!, TenSize = reader.GetString(8) };
                if (!reader.IsDBNull(9))
                    ct.DoanhMucDe = new DoanhMuc_De { MaDeBanh = ct.MaDeBanh!, TenDeBanh = reader.GetString(9) };
                result.Add(ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting CT_PhieuBan: {ex.Message}");
        }
        return result;
    }

    public async Task<string> GenerateMaPhieuBanAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT TOP 1 MaPhieuBan FROM PhieuBanHang 
                       WHERE MaPhieuBan LIKE 'PB%' 
                       ORDER BY MaPhieuBan DESC";
            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                var lastCode = result.ToString()!;
                if (int.TryParse(lastCode.Substring(2), out int lastNumber))
                {
                    return $"PB{(lastNumber + 1):D6}";
                }
            }
            return "PB000001";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating MaPhieuBan: {ex.Message}");
            return $"PB{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    public async Task<bool> DeletePhieuBanHangAsync(string maPhieuBan)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                using var delCt = new SqlCommand("DELETE FROM CT_PhieuBan WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                delCt.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                await delCt.ExecuteNonQueryAsync();

                using var delPb = new SqlCommand("DELETE FROM PhieuBanHang WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                delPb.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                await delPb.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting PhieuBanHang: {ex.Message}");
            return false;
        }
    }

    public async Task<string> SavePhieuBanHangAsync(PhieuBanHang phieuBan, List<CT_PhieuBan> chiTiets)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        try
        {
            // 1) Tính tổng nguyên liệu cần trừ cho toàn bộ đơn
            var requiredMap = new Dictionary<int, decimal>();
            var unitContextCache = new Dictionary<int, IngredientUnitContext>();
            var unitNameCache = new Dictionary<int, string>();

            void AddRequired(int nguyenLieuId, decimal amount)
            {
                if (amount <= 0) return;
                if (requiredMap.ContainsKey(nguyenLieuId))
                    requiredMap[nguyenLieuId] += amount;
                else
                    requiredMap[nguyenLieuId] = amount;
            }

            foreach (var ct in chiTiets)
            {
                if (ct.MaHangHoa == null || ct.SizeID == null) continue;
                var soLuongMua = ct.SoLuong ?? 0;
                if (soLuongMua <= 0) continue;

                var ingredientMap = new Dictionary<int, decimal>();

                // Bước 1: CongThuc_Pizza (nhân bánh)
                var congThucs = await GetCongThucPizzaInternalAsync(conn, transaction, ct.MaHangHoa, ct.SizeID);
                foreach (var recipe in congThucs)
                {
                    var amount = await ConvertAmountToStockUnitAsync(
                        conn,
                        transaction,
                        recipe.NguyenLieuID,
                        (decimal)(recipe.SoLuong ?? 0),
                        recipe.DonViID,
                        unitContextCache,
                        unitNameCache);
                    if (amount <= 0) continue;
                    if (ingredientMap.ContainsKey(recipe.NguyenLieuID))
                        ingredientMap[recipe.NguyenLieuID] += amount;
                    else
                        ingredientMap[recipe.NguyenLieuID] = amount;
                }

                // Bước 2: QuyDinh_Bot (bột mì) - lookup bằng SizeID + LoaiCotBanh từ DoanhMuc_De
                if (!string.IsNullOrEmpty(ct.MaDeBanh))
                {
                    var botSql = @"SELECT qb.TrongLuongBot, qb.DonViID
                                   FROM QuyDinh_Bot qb
                                   INNER JOIN DoanhMuc_De dd ON qb.LoaiCotBanh = dd.LoaiCotBanh
                                   WHERE qb.SizeID = @SizeID AND dd.MaDeBanh = @MaDeBanh";
                    using var botCmd = CreateCommand(botSql, conn, transaction);
                    botCmd.Parameters.AddWithValue("@SizeID", ct.SizeID);
                    botCmd.Parameters.AddWithValue("@MaDeBanh", ct.MaDeBanh);
                    using var botReader = await botCmd.ExecuteReaderAsync();
                    if (await botReader.ReadAsync())
                    {
                        var trongLuongBot = botReader.IsDBNull(0) ? 0m : Convert.ToDecimal(botReader.GetDouble(0));
                        int? donViBotId = botReader.IsDBNull(1) ? null : botReader.GetInt32(1);
                        botReader.Close();
                        if (trongLuongBot > 0)
                        {
                            // Tìm NguyenLieuID của bột mì (tên chứa "Bột" hoặc "bột mì")
                            var findBotSql = "SELECT TOP 1 NguyenLieuID FROM NguyenLieu WHERE TenNguyenLieu LIKE N'%Bột mì%' OR TenNguyenLieu LIKE N'%Bot mi%'";
                            using var findBotCmd = CreateCommand(findBotSql, conn, transaction);
                            var botNlId = await findBotCmd.ExecuteScalarAsync();
                            if (botNlId != null && botNlId != DBNull.Value)
                            {
                                var nlId = Convert.ToInt32(botNlId);
                                var normalizedBotAmount = await ConvertAmountToStockUnitAsync(
                                    conn,
                                    transaction,
                                    nlId,
                                    trongLuongBot,
                                    donViBotId,
                                    unitContextCache,
                                    unitNameCache);

                                if (normalizedBotAmount > 0)
                                {
                                    if (ingredientMap.ContainsKey(nlId))
                                        ingredientMap[nlId] += normalizedBotAmount;
                                    else
                                        ingredientMap[nlId] = normalizedBotAmount;
                                }
                            }
                        }
                    }
                }

                // Bước 3: QuyDinh_Vien (viền) - lookup bằng MaDeBanh + SizeID
                if (!string.IsNullOrEmpty(ct.MaDeBanh))
                {
                    var vienSql = @"SELECT NguyenLieuID, SoLuongVien, DonViID FROM QuyDinh_Vien 
                                    WHERE MaDeBanh = @MaDeBanh AND SizeID = @SizeID";
                    var vienItems = new List<(int NguyenLieuId, decimal SoLuong, int? DonViId)>();
                    using var vienCmd = CreateCommand(vienSql, conn, transaction);
                    vienCmd.Parameters.AddWithValue("@MaDeBanh", ct.MaDeBanh);
                    vienCmd.Parameters.AddWithValue("@SizeID", ct.SizeID);
                    using (var vienReader = await vienCmd.ExecuteReaderAsync())
                    {
                        while (await vienReader.ReadAsync())
                        {
                            vienItems.Add((
                                vienReader.GetInt32(0),
                                vienReader.IsDBNull(1) ? 0m : Convert.ToDecimal(vienReader.GetDouble(1)),
                                vienReader.IsDBNull(2) ? null : vienReader.GetInt32(2)));
                        }
                    }

                    foreach (var vienItem in vienItems)
                    {
                        var soLuongVien = await ConvertAmountToStockUnitAsync(
                            conn,
                            transaction,
                            vienItem.NguyenLieuId,
                            vienItem.SoLuong,
                            vienItem.DonViId,
                            unitContextCache,
                            unitNameCache);
                        if (soLuongVien <= 0) continue;
                        if (ingredientMap.ContainsKey(vienItem.NguyenLieuId))
                            ingredientMap[vienItem.NguyenLieuId] += soLuongVien;
                        else
                            ingredientMap[vienItem.NguyenLieuId] = soLuongVien;
                    }
                }

                // Gom tổng nguyên liệu theo số lượng mua
                foreach (var (nguyenLieuId, amountPerPizza) in ingredientMap)
                {
                    var totalAmount = amountPerPizza * soLuongMua;
                    AddRequired(nguyenLieuId, totalAmount);
                }
            }

            // 2) Kiểm tra tồn kho trước khi lưu phiếu
            if (requiredMap.Count > 0)
            {
                var insufficient = await GetInsufficientIngredientsAsync(conn, transaction, requiredMap);
                if (insufficient.Count > 0)
                {
                    var message = "Không đủ nguyên liệu: " +
                                  string.Join(", ", insufficient.Select(i => $"{i.Name} (còn {i.Available:N2})"));
                    throw new Exception(message);
                }
            }

            // 3) Lưu PhiếuBánHàng
            var sql = @"INSERT INTO PhieuBanHang (MaPhieuBan, NhanVienBanID, NgayBan, TongTien, PhuongThucTT, GhiChu)
                       VALUES (@MaPhieuBan, @NhanVienBanID, @NgayBan, @TongTien, @PhuongThucTT, @GhiChu)";
            using (var cmd = new SqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@MaPhieuBan", phieuBan.MaPhieuBan);
                cmd.Parameters.AddWithValue("@NhanVienBanID", phieuBan.NhanVienBanID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayBan", phieuBan.NgayBan ?? (object)DateTime.Now);
                cmd.Parameters.AddWithValue("@TongTien", phieuBan.TongTien ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PhuongThucTT", phieuBan.PhuongThucTT ?? "Tiền mặt");
                cmd.Parameters.AddWithValue("@GhiChu", phieuBan.GhiChu ?? (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            // 4) Lưu CT_PhiếuBán
            foreach (var ct in chiTiets)
            {
                var ctSql = @"INSERT INTO CT_PhieuBan (MaPhieuBan, MaHangHoa, SizeID, MaDeBanh, SoLuong, ThanhTien)
                             VALUES (@MaPhieuBan, @MaHangHoa, @SizeID, @MaDeBanh, @SoLuong, @ThanhTien)";
                using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                ctCmd.Parameters.AddWithValue("@MaPhieuBan", phieuBan.MaPhieuBan);
                ctCmd.Parameters.AddWithValue("@MaHangHoa", ct.MaHangHoa ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@SizeID", ct.SizeID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@MaDeBanh", ct.MaDeBanh ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@ThanhTien", ct.ThanhTien ?? (object)DBNull.Value);
                await ctCmd.ExecuteNonQueryAsync();
            }

            // 5) Trừ tồn kho theo tổng nguyên liệu
            foreach (var kvp in requiredMap)
            {
                var deductSql = @"UPDATE TonKho 
                                 SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                 WHERE NguyenLieuID = @NguyenLieuID AND SoLuongTon >= @SoLuong";
                using var deductCmd = new SqlCommand(deductSql, conn, transaction);
                deductCmd.Parameters.AddWithValue("@SoLuong", kvp.Value);
                deductCmd.Parameters.AddWithValue("@NguyenLieuID", kvp.Key);
                var rows = await deductCmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new Exception($"Nguyên liệu không đủ tồn kho (ID {kvp.Key})");
                }
            }

            transaction.Commit();
            return phieuBan.MaPhieuBan;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"Error saving PhieuBanHang: {ex.Message}");
            throw;
        }
    }

    private async Task<List<(int Id, string Name, decimal Required, decimal Available)>> GetInsufficientIngredientsAsync(
        SqlConnection conn,
        SqlTransaction transaction,
        Dictionary<int, decimal> requiredMap)
    {
        var result = new List<(int Id, string Name, decimal Required, decimal Available)>();
        var ids = requiredMap.Keys.ToList();
        if (ids.Count == 0)
            return result;

        var parameters = ids.Select((id, i) => new SqlParameter($"@Id{i}", id)).ToArray();
        var inClause = string.Join(",", parameters.Select(p => p.ParameterName));

        var sql = $@"SELECT nl.NguyenLieuID, nl.TenNguyenLieu, ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                     FROM NguyenLieu nl
                     LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                     WHERE nl.NguyenLieuID IN ({inClause})";

        var availableMap = new Dictionary<int, decimal>();
        var nameMap = new Dictionary<int, string>();

        using (var cmd = new SqlCommand(sql, conn, transaction))
        {
            cmd.Parameters.AddRange(parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var name = reader.IsDBNull(1) ? $"ID {id}" : reader.GetString(1);
                var available = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                availableMap[id] = available;
                nameMap[id] = name;
            }
        }

        foreach (var kvp in requiredMap)
        {
            var available = availableMap.TryGetValue(kvp.Key, out var a) ? a : 0m;
            if (available < kvp.Value)
            {
                var name = nameMap.TryGetValue(kvp.Key, out var n) ? n : $"ID {kvp.Key}";
                result.Add((kvp.Key, name, kvp.Value, available));
            }
        }

        return result;
    }

    private async Task<List<CongThuc_Pizza>> GetCongThucPizzaInternalAsync(SqlConnection conn, SqlTransaction transaction, string maHangHoa, string sizeId)
    {
        var result = new List<CongThuc_Pizza>();
        var sql = "SELECT MaHangHoa, SizeID, NguyenLieuID, SoLuong, DonViID FROM CongThuc_Pizza WHERE MaHangHoa = @MaHangHoa AND SizeID = @SizeID";
        using var cmd = new SqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("@MaHangHoa", maHangHoa);
        cmd.Parameters.AddWithValue("@SizeID", sizeId);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new CongThuc_Pizza
            {
                MaHangHoa = reader.GetString(0),
                SizeID = reader.GetString(1),
                NguyenLieuID = reader.GetInt32(2),
                SoLuong = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            });
        }
        return result;
    }
    #endregion

    #region CongThuc_Pizza
    public async Task<List<CongThuc_Pizza>> GetCongThucPizzaAsync(string maHangHoa, string sizeId)
    {
        var result = new List<CongThuc_Pizza>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ct.MaHangHoa, ct.SizeID, ct.NguyenLieuID, ct.SoLuong, ct.DonViID,
                              nl.TenNguyenLieu, dv.TenDonVi
                       FROM CongThuc_Pizza ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       WHERE ct.MaHangHoa = @MaHangHoa AND ct.SizeID = @SizeID";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHangHoa", maHangHoa);
            cmd.Parameters.AddWithValue("@SizeID", sizeId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ct = new CongThuc_Pizza
                {
                    MaHangHoa = reader.GetString(0),
                    SizeID = reader.GetString(1),
                    NguyenLieuID = reader.GetInt32(2),
                    SoLuong = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                };
                if (!reader.IsDBNull(5))
                    ct.NguyenLieu = new NguyenLieu { NguyenLieuID = ct.NguyenLieuID, TenNguyenLieu = reader.GetString(5) };
                if (!reader.IsDBNull(6))
                    ct.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(6) };
                result.Add(ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting CongThuc_Pizza: {ex.Message}");
        }
        return result;
    }

    public async Task<bool> SaveCongThucPizzaAsync(CongThuc_Pizza congThuc)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"MERGE CongThuc_Pizza AS target
                        USING (SELECT @MaHangHoa AS MaHangHoa, @SizeID AS SizeID, @NguyenLieuID AS NguyenLieuID) AS source
                        ON target.MaHangHoa = source.MaHangHoa AND target.SizeID = source.SizeID AND target.NguyenLieuID = source.NguyenLieuID
                        WHEN MATCHED THEN
                            UPDATE SET SoLuong = @SoLuong, DonViID = @DonViID
                        WHEN NOT MATCHED THEN
                            INSERT (MaHangHoa, SizeID, NguyenLieuID, SoLuong, DonViID)
                            VALUES (@MaHangHoa, @SizeID, @NguyenLieuID, @SoLuong, @DonViID);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHangHoa", congThuc.MaHangHoa);
            cmd.Parameters.AddWithValue("@SizeID", congThuc.SizeID);
            cmd.Parameters.AddWithValue("@NguyenLieuID", congThuc.NguyenLieuID);
            cmd.Parameters.AddWithValue("@SoLuong", (object?)congThuc.SoLuong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViID", (object?)congThuc.DonViID ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving CongThuc_Pizza: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCongThucPizzaAsync(string maHangHoa, string sizeId, int nguyenLieuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "DELETE FROM CongThuc_Pizza WHERE MaHangHoa = @MaHangHoa AND SizeID = @SizeID AND NguyenLieuID = @NguyenLieuID", conn);
            cmd.Parameters.AddWithValue("@MaHangHoa", maHangHoa);
            cmd.Parameters.AddWithValue("@SizeID", sizeId);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting CongThuc_Pizza: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Thống kê bán hàng (PhiếuBánHàng)
    public async Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Tính doanh thu từ cả bảng DonHang (trạng thái Hoàn thành = 2) và PhieuBanHang
            var sql = @"
                SELECT ISNULL(SUM(DoanhThu), 0) FROM (
                    SELECT ThanhToan AS DoanhThu 
                    FROM DonHang 
                    WHERE NgayTao >= @FromDate AND NgayTao < @ToDate AND TrangThai = 2
                    UNION ALL
                    SELECT TongTien AS DoanhThu 
                    FROM PhieuBanHang 
                    WHERE NgayBan >= @FromDate AND NgayBan < @ToDate
                ) AS CombinedRevenue";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DoanhThuBanHang: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetTotalPhieuBanCountAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Đếm tổng số đơn từ cả bảng DonHang (trạng thái Hoàn thành = 2) và PhieuBanHang
            var sql = @"
                SELECT COUNT(*) FROM (
                    SELECT DonHangID 
                    FROM DonHang 
                    WHERE NgayTao >= @FromDate AND NgayTao < @ToDate AND TrangThai = 2
                    UNION ALL
                    SELECT ROW_NUMBER() OVER (ORDER BY NgayBan) 
                    FROM PhieuBanHang 
                    WHERE NgayBan >= @FromDate AND NgayBan < @ToDate
                ) AS CombinedOrders";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuBan count: {ex.Message}");
            return 0;
        }
    }
    #endregion
}


