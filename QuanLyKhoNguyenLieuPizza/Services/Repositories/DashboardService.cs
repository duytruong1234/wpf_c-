using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class DashboardService : DatabaseContext
{
    public DashboardService(string connectionString) : base(connectionString) { }

    public async Task<int> GetTotalNguyenLieuCountAsync() =>
        await ExecuteScalarValueAsync("SELECT COUNT(*) FROM NguyenLieu WHERE TrangThai = 1", 0);

    public async Task<int> GetTotalTonKhoCountAsync() =>
        await ExecuteScalarValueAsync("SELECT COUNT(*) FROM TonKho tk INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID WHERE tk.SoLuongTon > 0 AND nl.TrangThai = 1", 0);

    public async Task<int> GetLowStockCountAsync(decimal threshold = 20) =>
        await ExecuteScalarValueAsync(
            "SELECT COUNT(*) FROM TonKho tk INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID WHERE tk.SoLuongTon > 0 AND tk.SoLuongTon < @Threshold AND nl.TrangThai = 1", 0,
            new SqlParameter("@Threshold", threshold));

    public async Task<int> GetNearExpiryCountAsync(int days = 7) =>
        await ExecuteScalarValueAsync(
            @"SELECT COUNT(*) FROM TonKho tk
              INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
              OUTER APPLY (
                  SELECT TOP 1 ctp.HSD
                  FROM CT_PhieuNhap ctp
                  INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                  WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                  ORDER BY pn.NgayNhap DESC
              ) phieu
              WHERE tk.SoLuongTon > 0 
              AND nl.TrangThai = 1
              AND phieu.HSD IS NOT NULL 
              AND phieu.HSD <= DATEADD(DAY, @Days, GETDATE())
              AND phieu.HSD > GETDATE()", 0,
            new SqlParameter("@Days", days));

    public async Task<int> GetExpiredCountAsync() =>
        await ExecuteScalarValueAsync(
            @"SELECT COUNT(*) FROM TonKho tk
              INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
              OUTER APPLY (
                  SELECT TOP 1 ctp.HSD
                  FROM CT_PhieuNhap ctp
                  INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                  WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                  ORDER BY pn.NgayNhap DESC
              ) phieu
              WHERE tk.SoLuongTon > 0 
              AND nl.TrangThai = 1
              AND phieu.HSD IS NOT NULL 
              AND phieu.HSD <= GETDATE()", 0);

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetLowStockItemsAsync(decimal threshold = 20)
    {
        var result = new List<(string, decimal, string, DateTime?)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ISNULL(nl.TenNguyenLieu, N'Không tên'), ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, ''), phieu.HSD
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        OUTER APPLY (
                            SELECT TOP 1 ctp.HSD
                            FROM CT_PhieuNhap ctp
                            INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                            ORDER BY pn.NgayNhap DESC
                        ) phieu
                        WHERE tk.SoLuongTon > 0 AND tk.SoLuongTon < @Threshold
                        AND nl.TrangThai = 1
                        ORDER BY tk.SoLuongTon ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Threshold", threshold);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader[0]?.ToString() ?? "", Convert.ToDecimal(reader[1]), reader[2]?.ToString() ?? "", reader.IsDBNull(3) ? null : Convert.ToDateTime(reader[3])));
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
            var sql = @"SELECT ISNULL(nl.TenNguyenLieu, N'Không tên'), ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, ''), phieu.HSD
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        OUTER APPLY (
                            SELECT TOP 1 ctp.HSD
                            FROM CT_PhieuNhap ctp
                            INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                            ORDER BY pn.NgayNhap DESC
                        ) phieu
                        WHERE tk.SoLuongTon > 0 
                        AND nl.TrangThai = 1
                        AND phieu.HSD IS NOT NULL 
                        AND phieu.HSD <= DATEADD(DAY, @Days, GETDATE())
                        AND phieu.HSD > GETDATE()
                        ORDER BY phieu.HSD ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", days);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader[0]?.ToString() ?? "", Convert.ToDecimal(reader[1]), reader[2]?.ToString() ?? "", reader.IsDBNull(3) ? null : Convert.ToDateTime(reader[3])));
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
            var sql = @"SELECT ISNULL(nl.TenNguyenLieu, N'Không tên'), ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, ''), phieu.HSD
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        OUTER APPLY (
                            SELECT TOP 1 ctp.HSD
                            FROM CT_PhieuNhap ctp
                            INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                            ORDER BY pn.NgayNhap DESC
                        ) phieu
                        WHERE tk.SoLuongTon > 0 
                        AND nl.TrangThai = 1
                        AND phieu.HSD IS NOT NULL 
                        AND phieu.HSD <= GETDATE()
                        ORDER BY phieu.HSD ASC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader[0]?.ToString() ?? "", Convert.ToDecimal(reader[1]), reader[2]?.ToString() ?? "", reader.IsDBNull(3) ? null : Convert.ToDateTime(reader[3])));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetExpiredItems: {ex.Message}"); }
        return result;
    }

    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNormalStockItemsAsync(decimal lowThreshold = 20)
    {
        var result = new List<(string, decimal, string, DateTime?)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ISNULL(nl.TenNguyenLieu, N'Không tên'), ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, ''), phieu.HSD
                        FROM TonKho tk
                        INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        OUTER APPLY (
                            SELECT TOP 1 ctp.HSD
                            FROM CT_PhieuNhap ctp
                            INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                            ORDER BY pn.NgayNhap DESC
                        ) phieu
                        WHERE tk.SoLuongTon >= @Threshold
                        AND nl.TrangThai = 1
                        ORDER BY nl.TenNguyenLieu ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Threshold", lowThreshold);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader[0]?.ToString() ?? "", Convert.ToDecimal(reader[1]), reader[2]?.ToString() ?? "", reader.IsDBNull(3) ? null : Convert.ToDateTime(reader[3])));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetNormalStockItems: {ex.Message}"); }
        return result;
    }
    public async Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetOutOfStockItemsAsync()
    {
        var result = new List<(string, decimal, string, DateTime?)>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ISNULL(nl.TenNguyenLieu, N'Không tên'), ISNULL(tk.SoLuongTon, 0), ISNULL(dv.TenDonVi, ''), phieu.HSD
                        FROM NguyenLieu nl
                        LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                        LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                        OUTER APPLY (
                            SELECT TOP 1 ctp.HSD
                            FROM CT_PhieuNhap ctp
                            INNER JOIN PhieuNhap pn ON ctp.PhieuNhapID = pn.PhieuNhapID
                            WHERE ctp.NguyenLieuID = nl.NguyenLieuID AND pn.TrangThai = 2 AND ctp.HSD IS NOT NULL
                            ORDER BY pn.NgayNhap DESC
                        ) phieu
                        WHERE (tk.TonKhoID IS NULL OR tk.SoLuongTon <= 0) AND nl.TrangThai = 1
                        ORDER BY nl.TenNguyenLieu ASC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader[0]?.ToString() ?? "", Convert.ToDecimal(reader[1]), reader[2]?.ToString() ?? "", reader.IsDBNull(3) ? null : Convert.ToDateTime(reader[3])));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error GetOutOfStockItems: {ex.Message}"); }
        return result;
    }
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

        // Sáº¯p xáº¿p theo ngÃ y má»›i nháº¥t vÃ  láº¥y top
        donHangs.Sort((a, b) => b.NgayTao.CompareTo(a.NgayTao));
        return donHangs.Take(top).ToList();
    }
    public async Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate)
    {
        decimal total = 0;
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Doanh thu tá»« PhieuBanHang (POS)
            try
            {
                var sql1 = @"SELECT ISNULL(SUM(TongTien), 0) FROM PhieuBanHang 
                             WHERE NgayBan >= @FromDate AND NgayBan < @ToDate";
                using var cmd1 = new SqlCommand(sql1, conn);
                cmd1.Parameters.AddWithValue("@FromDate", fromDate.Date);
                cmd1.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
                var r1 = await cmd1.ExecuteScalarAsync();
                if (r1 != null && r1 != DBNull.Value) total += Convert.ToDecimal(r1);
            }
            catch (Exception ex1)
            {
                System.Diagnostics.Debug.WriteLine($"Error PhieuBanHang revenue: {ex1.Message}");
            }

            // Doanh thu tá»« DonHang (náº¿u báº£ng tá»“n táº¡i)
            try
            {
                var sql2 = @"SELECT ISNULL(SUM(ThanhToan), 0) FROM DonHang 
                             WHERE NgayTao >= @FromDate AND NgayTao < @ToDate AND TrangThai = 2";
                using var cmd2 = new SqlCommand(sql2, conn);
                cmd2.Parameters.AddWithValue("@FromDate", fromDate.Date);
                cmd2.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
                var r2 = await cmd2.ExecuteScalarAsync();
                if (r2 != null && r2 != DBNull.Value) total += Convert.ToDecimal(r2);
            }
            catch { /* Báº£ng DonHang cÃ³ thá»ƒ khÃ´ng tá»“n táº¡i */ }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting DoanhThuBanHang: {ex.Message}");
        }
        return total;
    }

    public async Task<int> GetTotalPhieuBanCountAsync(DateTime fromDate, DateTime toDate)
    {
        int total = 0;
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Äáº¿m tá»« PhieuBanHang (POS)
            try
            {
                var sql1 = @"SELECT COUNT(*) FROM PhieuBanHang 
                             WHERE NgayBan >= @FromDate AND NgayBan < @ToDate";
                using var cmd1 = new SqlCommand(sql1, conn);
                cmd1.Parameters.AddWithValue("@FromDate", fromDate.Date);
                cmd1.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
                total += Convert.ToInt32(await cmd1.ExecuteScalarAsync());
            }
            catch (Exception ex1)
            {
                System.Diagnostics.Debug.WriteLine($"Error PhieuBanHang count: {ex1.Message}");
            }

            // Äáº¿m tá»« DonHang (náº¿u báº£ng tá»“n táº¡i)
            try
            {
                var sql2 = @"SELECT COUNT(*) FROM DonHang 
                             WHERE NgayTao >= @FromDate AND NgayTao < @ToDate AND TrangThai = 2";
                using var cmd2 = new SqlCommand(sql2, conn);
                cmd2.Parameters.AddWithValue("@FromDate", fromDate.Date);
                cmd2.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
                total += Convert.ToInt32(await cmd2.ExecuteScalarAsync());
            }
            catch { /* Báº£ng DonHang cÃ³ thá»ƒ khÃ´ng tá»“n táº¡i */ }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PhieuBan count: {ex.Message}");
        }
        return total;
    }
}

