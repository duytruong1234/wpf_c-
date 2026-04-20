using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class PizzaService : DatabaseContext
{
    public PizzaService(string connectionString) : base(connectionString) { }

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
        // KhÃ´ng sá»­ dá»¥ng vá»›i schema dá»±a trÃªn HÃ ngHÃ³a (khÃ´ng cÃ³ báº£ng Pizza)
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

            // Kiá»ƒm tra náº¿u HÃ ngHÃ³a Ä‘Ã£ tá»“n táº¡i
            using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM HangHoa WHERE MaHangHoa = @Ma", conn);
            checkCmd.Parameters.AddWithValue("@Ma", pizza.MaPizza);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (exists)
            {
                // Cáº­p nháº­t HÃ ng hÃ³a
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
        // KhÃ´ng sá»­ dá»¥ng vá»›i schema dá»±a trÃªn HÃ ngHÃ³a
        return false;
    }

    public async Task<bool> DeletePizzaByMaAsync(string maHangHoa)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
                BEGIN TRY
                    BEGIN TRAN;
                    DELETE FROM CongThuc_Pizza WHERE MaHangHoa = @Ma;
                    DELETE FROM GiaTheo_Size WHERE MaHangHoa = @Ma;
                    DELETE FROM HangHoa WHERE MaHangHoa = @Ma;
                    COMMIT TRAN;
                END TRY
                BEGIN CATCH
                    ROLLBACK TRAN;
                    -- Lá»—i 547 lÃ  vi pháº¡m khoÃ¡ ngoáº¡i (vd: Ä‘Ã£ tá»«ng bÃ¡n trong hoÃ¡ Ä‘Æ¡n)
                    -- TrÆ°á»ng há»£p nÃ y ta fallback vá» Soft Delete (Ngá»«ng bÃ¡n)
                    IF ERROR_NUMBER() = 547
                    BEGIN
                        UPDATE HangHoa SET TinhTrang = 0 WHERE MaHangHoa = @Ma;
                    END
                    ELSE
                    BEGIN
                        THROW;
                    END
                END CATCH", conn);
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
        // KhÃ´ng sá»­ dá»¥ng vá»›i schema dá»±a trÃªn HÃ ngHÃ³a
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
                       WHERE hh.TinhTrang = 1
                         AND EXISTS (SELECT 1 FROM CongThuc_Pizza ctp WHERE ctp.MaHangHoa = hh.MaHangHoa)";
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
                       req.SizeID,
                       req.MaDeBanh,
                       req.NguyenLieuID,
                       req.SoLuong,
                       req.DonViID,
                       req.TenNguyenLieu,
                       req.SoLuongTon
                FROM (
                    -- Nguyên liệu nhân bánh
                    SELECT gs.MaHangHoa,
                           gs.SizeID,
                           gtd.MaDeBanh,
                           ctp.NguyenLieuID,
                           CAST(ctp.SoLuong AS decimal(18,4)) AS SoLuong,
                           ctp.DonViID,
                           nl.TenNguyenLieu,
                           ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                    FROM GiaTheo_Size gs
                    INNER JOIN GiaTheo_De gtd ON gs.SizeID = gtd.SizeID
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
                           gs.SizeID,
                           gtd.MaDeBanh,
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
                        WHERE (nl.TenNguyenLieu LIKE N'%Bột mì%'
                           OR nl.TenNguyenLieu LIKE N'%Bot mi%')
                    ) botNl
                    LEFT JOIN TonKho tk
                        ON botNl.NguyenLieuID = tk.NguyenLieuID
                    WHERE gs.MaHangHoa IN ({inClause})

                    UNION ALL

                    -- Nguyên liệu viền theo size + đế
                    SELECT gs.MaHangHoa,
                           gs.SizeID,
                           gtd.MaDeBanh,
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

            var rows = new List<(string MaHangHoa, string SizeID, string MaDeBanh, int NguyenLieuId, decimal Required, int? DonViId, string TenNguyenLieu, decimal SoLuongTon)>();

            using var cmd = CreateCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows.Add((
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetInt32(3),
                        reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                        reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)));
                }
            }

            var unitContextCache = new Dictionary<int, IngredientUnitContext>();
            var unitNameCache = new Dictionary<int, string>();
            var missingMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            var comboReqs = new Dictionary<string, Dictionary<int, decimal>>();
            var nguyenLieuInfo = new Dictionary<int, (string Name, decimal Stock)>();
            var comboMaHangHoa = new Dictionary<string, string>(); // To trace "MaHangHoa|SizeID|MaDeBanh" -> MaHangHoa

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TenNguyenLieu))
                    continue;

                try
                {
                    var normalizedRequired = await ConvertAmountToStockUnitAsync(
                        conn, null, row.NguyenLieuId, row.Required, row.DonViId, unitContextCache, unitNameCache);

                    var key = $"{row.MaHangHoa}|{row.SizeID}|{row.MaDeBanh}";
                    comboMaHangHoa[key] = row.MaHangHoa;

                    if (!comboReqs.TryGetValue(key, out var reqList))
                    {
                        reqList = new Dictionary<int, decimal>();
                        comboReqs[key] = reqList;
                    }

                    if (!reqList.ContainsKey(row.NguyenLieuId))
                        reqList[row.NguyenLieuId] = 0;
                    
                    reqList[row.NguyenLieuId] += normalizedRequired;
                    nguyenLieuInfo[row.NguyenLieuId] = (row.TenNguyenLieu, row.SoLuongTon);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skip stock pre-check for '{row.TenNguyenLieu}': {ex.Message}");
                }
            }

            // Check each combination against stock
            foreach (var kvp in comboReqs)
            {
                var maHangHoa = comboMaHangHoa[kvp.Key];
                foreach (var req in kvp.Value)
                {
                    var nlId = req.Key;
                    var totalRequired = req.Value;
                    var info = nguyenLieuInfo[nlId];

                    if (totalRequired > info.Stock)
                    {
                        if (!missingMap.TryGetValue(maHangHoa, out var missingIngredients))
                        {
                            missingIngredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            missingMap[maHangHoa] = missingIngredients;
                        }

                        missingIngredients.Add(info.Name);
                    }
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

    public async Task<List<QuyDinh_Bot>> GetQuyDinhBotsAsync()
    {
        var result = new List<QuyDinh_Bot>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT qb.SizeID, qb.LoaiCotBanh, qb.TrongLuongBot, qb.DonViID,
                               ds.TenSize, dv.TenDonVi
                        FROM QuyDinh_Bot qb
                        LEFT JOIN DoanhMuc_Size ds ON qb.SizeID = ds.SizeID
                        LEFT JOIN DonViTinh dv ON qb.DonViID = dv.DonViID";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new QuyDinh_Bot
                {
                    SizeID = reader.GetString(0),
                    LoaiCotBanh = reader.GetString(1),
                    TrongLuongBot = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                    DonViID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    DoanhMucSize = reader.IsDBNull(4) ? null : new DoanhMuc_Size { SizeID = reader.GetString(0), TenSize = reader.GetString(4) },
                    DonViTinh = reader.IsDBNull(5) ? null : new DonViTinh { DonViID = reader.GetInt32(3), TenDonVi = reader.GetString(5) }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting QuyDinh_Bot: {ex.Message}");
        }
        return result;
    }

    public async Task<bool> SaveQuyDinhBotAsync(QuyDinh_Bot item)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"MERGE QuyDinh_Bot AS target
                        USING (SELECT @SizeID AS SizeID, @LoaiCotBanh AS LoaiCotBanh) AS source
                        ON target.SizeID = source.SizeID AND target.LoaiCotBanh = source.LoaiCotBanh
                        WHEN MATCHED THEN
                            UPDATE SET TrongLuongBot = @TrongLuong, DonViID = @DonViID
                        WHEN NOT MATCHED THEN
                            INSERT (SizeID, LoaiCotBanh, TrongLuongBot, DonViID)
                            VALUES (@SizeID, @LoaiCotBanh, @TrongLuong, @DonViID);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SizeID", item.SizeID);
            cmd.Parameters.AddWithValue("@LoaiCotBanh", item.LoaiCotBanh);
            cmd.Parameters.AddWithValue("@TrongLuong", item.TrongLuongBot ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViID", item.DonViID ?? (object)DBNull.Value);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QuyDinh_Bot: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteQuyDinhBotAsync(string sizeId, string loaiCotBanh)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM QuyDinh_Bot WHERE SizeID = @SizeID AND LoaiCotBanh = @LoaiCotBanh", conn);
            cmd.Parameters.AddWithValue("@SizeID", sizeId);
            cmd.Parameters.AddWithValue("@LoaiCotBanh", loaiCotBanh);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QuyDinh_Bot: {ex.Message}");
            return false;
        }
    }

    public async Task<List<QuyDinh_Vien>> GetQuyDinhViensAsync()
    {
        var result = new List<QuyDinh_Vien>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT qv.MaDeBanh, qv.SizeID, qv.NguyenLieuID, qv.SoLuongVien, qv.DonViID,
                               dd.TenDeBanh, ds.TenSize, nl.TenNguyenLieu, dv.TenDonVi
                        FROM QuyDinh_Vien qv
                        LEFT JOIN DoanhMuc_De dd ON qv.MaDeBanh = dd.MaDeBanh
                        LEFT JOIN DoanhMuc_Size ds ON qv.SizeID = ds.SizeID
                        LEFT JOIN NguyenLieu nl ON qv.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON qv.DonViID = dv.DonViID";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new QuyDinh_Vien
                {
                    MaDeBanh = reader.GetString(0),
                    SizeID = reader.GetString(1),
                    NguyenLieuID = reader.GetInt32(2),
                    SoLuongVien = reader.IsDBNull(3) ? null : Convert.ToDouble(reader.GetValue(3)),
                    DonViID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DoanhMucDe = reader.IsDBNull(5) ? null : new DoanhMuc_De { MaDeBanh = reader.GetString(0), TenDeBanh = reader.GetString(5) },
                    DoanhMucSize = reader.IsDBNull(6) ? null : new DoanhMuc_Size { SizeID = reader.GetString(1), TenSize = reader.GetString(6) },
                    NguyenLieu = reader.IsDBNull(7) ? null : new NguyenLieu { NguyenLieuID = reader.GetInt32(2), TenNguyenLieu = reader.GetString(7) },
                    DonViTinh = reader.IsDBNull(8) ? null : new DonViTinh { DonViID = reader.GetInt32(4), TenDonVi = reader.GetString(8) }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting QuyDinh_Vien: {ex.Message}");
        }
        return result;
    }

    public async Task<bool> SaveQuyDinhVienAsync(QuyDinh_Vien item)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"MERGE QuyDinh_Vien AS target
                        USING (SELECT @MaDeBanh AS MaDeBanh, @SizeID AS SizeID, @NguyenLieuID AS NguyenLieuID) AS source
                        ON target.MaDeBanh = source.MaDeBanh AND target.SizeID = source.SizeID AND target.NguyenLieuID = source.NguyenLieuID
                        WHEN MATCHED THEN
                            UPDATE SET SoLuongVien = @SoLuong, DonViID = @DonViID
                        WHEN NOT MATCHED THEN
                            INSERT (MaDeBanh, SizeID, NguyenLieuID, SoLuongVien, DonViID)
                            VALUES (@MaDeBanh, @SizeID, @NguyenLieuID, @SoLuong, @DonViID);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaDeBanh", item.MaDeBanh);
            cmd.Parameters.AddWithValue("@SizeID", item.SizeID);
            cmd.Parameters.AddWithValue("@NguyenLieuID", item.NguyenLieuID);
            cmd.Parameters.AddWithValue("@SoLuong", item.SoLuongVien ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DonViID", item.DonViID ?? (object)DBNull.Value);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QuyDinh_Vien: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteQuyDinhVienAsync(string maDeBanh, string sizeId, int nguyenLieuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("DELETE FROM QuyDinh_Vien WHERE MaDeBanh = @MaDeBanh AND SizeID = @SizeID AND NguyenLieuID = @NguyenLieuID", conn);
            cmd.Parameters.AddWithValue("@MaDeBanh", maDeBanh);
            cmd.Parameters.AddWithValue("@SizeID", sizeId);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QuyDinh_Vien: {ex.Message}");
            return false;
        }
    }
}
