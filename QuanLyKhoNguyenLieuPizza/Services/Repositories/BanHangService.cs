using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class BanHangService : DatabaseContext
{
    public BanHangService(string connectionString) : base(connectionString) { }

    public async Task<List<DonHang>> GetDonHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new List<DonHang>();
        var existingMaDonHangs = new HashSet<string>();

        // 1) Try loading from DonHang table (may not exist)
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            bool hasDonHang = false;
            using (var chkCmd = new SqlCommand("SELECT OBJECT_ID('dbo.DonHang', 'U')", conn))
            {
                hasDonHang = await chkCmd.ExecuteScalarAsync() is not (null or DBNull);
            }
            if (hasDonHang)
            {
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
        }
        catch { }
        // 2) Always load from PhieuBanHang (the active data source)
        try
        {
            using var conn2 = GetConnection();
            await conn2.OpenAsync();
            var sql2 = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, ISNULL(pb.TongTien, 0),
                                nv.HoTen, pb.GhiChu, pb.PhuongThucTT, ISNULL(pb.GiamGia, 0), ISNULL(pb.ThanhToan, ISNULL(pb.TongTien, 0))
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
                    GiamGia = reader2.GetDecimal(7),
                    ThanhToan = reader2.GetDecimal(8),
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
        catch { }

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
        catch {
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
        catch { }
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

                // Trừ nguyên liệu từ TồnKho dựa trên CôngThức (có quy đổi đơn vị)
                var unitContextCache = new Dictionary<int, IngredientUnitContext>();
                var unitNameCache = new Dictionary<int, string>();
                foreach (var ct in chiTiets)
                {
                    var congThucs = await GetCongThucsInternalAsync(conn, transaction, ct.PizzaID);
                    foreach (var recipe in congThucs)
                    {
                        var convertedAmount = await ConvertAmountToStockUnitAsync(
                            conn, transaction,
                            recipe.NguyenLieuID,
                            recipe.SoLuong * ct.SoLuong,
                            recipe.DonViID,
                            unitContextCache,
                            unitNameCache);
                        if (convertedAmount <= 0) continue;

                        var deductSql = @"UPDATE TonKho SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                         WHERE NguyenLieuID = @NguyenLieuID AND SoLuongTon >= @SoLuong";
                        using var deductCmd = new SqlCommand(deductSql, conn, transaction);
                        deductCmd.Parameters.AddWithValue("@SoLuong", convertedAmount);
                        deductCmd.Parameters.AddWithValue("@NguyenLieuID", recipe.NguyenLieuID);
                        var rows = await deductCmd.ExecuteNonQueryAsync();
                        if (rows == 0)
                        {
                            // Lấy tên nguyên liệu để hiển thị thông báo rõ ràng
                            var nlNameSql = "SELECT TenNguyenLieu FROM NguyenLieu WHERE NguyenLieuID = @NguyenLieuID";
                            using var nlNameCmd = new SqlCommand(nlNameSql, conn, transaction);
                            nlNameCmd.Parameters.AddWithValue("@NguyenLieuID", recipe.NguyenLieuID);
                            var tenNL = await nlNameCmd.ExecuteScalarAsync() as string ?? $"ID {recipe.NguyenLieuID}";
                            throw new Exception($"Nguyên liệu '{tenNL}' không đủ tồn kho để chế biến.");
                        }

                        await DeductSoLuongConTheoLoAsync(
                            conn, transaction, recipe.NguyenLieuID, convertedAmount,
                            unitContextCache, unitNameCache);
                        await UpsertDuLieuTieuHaoAsync(conn, transaction, recipe.NguyenLieuID, donHang.NgayTao, convertedAmount);
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
        catch {
            return 0;
        }
    }

    private async Task<List<CongThuc>> GetCongThucsInternalAsync(SqlConnection conn, SqlTransaction transaction, int pizzaId)
    {
        var result = new List<CongThuc>();
        var sql = @"SELECT CongThucID, PizzaID, NguyenLieuID, SoLuong, DonViID FROM CongThuc WHERE PizzaID = @PizzaID";
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
        catch {
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
        catch {
            return false;
        }
    }
    public async Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new List<PhieuBanHang>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, pb.TongTien,
                              nv.HoTen, pb.GhiChu, pb.PhuongThucTT, pb.GiamGia, pb.ThanhToan
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
                    PhuongThucTT = reader.IsDBNull(6) ? null : reader.GetString(6),
                    GiamGia = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    ThanhToan = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
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
        catch { }
        return result;
    }

    public async Task<PhieuBanHang?> GetPhieuBanHangByIdAsync(string maPhieuBan)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, pb.TongTien,
                              nv.HoTen, pb.GhiChu, pb.PhuongThucTT, pb.GiamGia, pb.ThanhToan
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
                    PhuongThucTT = reader.IsDBNull(6) ? null : reader.GetString(6),
                    GiamGia = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    ThanhToan = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
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
        catch { }
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
        catch { }
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
        catch {
            return $"PB{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
    public async Task<bool> UpdatePhieuBanHangAsync(PhieuBanHang pb)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"UPDATE PhieuBanHang
                        SET PhuongThucTT = @PhuongThucTT,
                            GhiChu = @GhiChu
                        WHERE MaPhieuBan = @MaPhieuBan";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhuongThucTT", pb.PhuongThucTT ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@GhiChu", pb.GhiChu ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MaPhieuBan", pb.MaPhieuBan);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch {
            return false;
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
        catch {
            return false;
        }
    }

    /// <summary>
    /// Xóa phiếu bán hàng và hoàn trả nguyên liệu vào tồn kho.
    /// </summary>
    public async Task<bool> DeletePhieuBanHangWithRestoreAsync(string maPhieuBan)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                // 1) Đọc chi tiết phiếu bán để tính nguyên liệu cần hoàn
                DateTime ngayBanRestore = DateTime.Now;
                using (var ngayCmd = new SqlCommand("SELECT NgayBan FROM PhieuBanHang WHERE MaPhieuBan = @MaPhieu", conn, transaction))
                {
                    ngayCmd.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                    var nbObj = await ngayCmd.ExecuteScalarAsync();
                    if (nbObj != null && nbObj != DBNull.Value)
                        ngayBanRestore = Convert.ToDateTime(nbObj);
                }
                var chiTietsRestore = new List<CT_PhieuBan>();
                var ctReadSql = @"SELECT MaHangHoa, SizeID, MaDeBanh, SoLuong FROM CT_PhieuBan WHERE MaPhieuBan = @MaPhieu";
                using (var ctCmd = new SqlCommand(ctReadSql, conn, transaction))
                {
                    ctCmd.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                    using var reader = await ctCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        chiTietsRestore.Add(new CT_PhieuBan
                        {
                            MaHangHoa = reader.IsDBNull(0) ? null : reader.GetString(0),
                            SizeID = reader.IsDBNull(1) ? null : reader.GetString(1),
                            MaDeBanh = reader.IsDBNull(2) ? null : reader.GetString(2),
                            SoLuong = reader.IsDBNull(3) ? null : reader.GetInt32(3)
                        });
                    }
                }

                // 2) Tính tổng nguyên liệu cần hoàn trả
                var restoreMap = new Dictionary<int, decimal>();
                var unitCtxCache = new Dictionary<int, IngredientUnitContext>();
                var unitNmCache = new Dictionary<int, string>();

                foreach (var ct in chiTietsRestore)
                {
                    if (ct.MaHangHoa == null || ct.SizeID == null) continue;
                    var soLuongMua = ct.SoLuong ?? 0;
                    if (soLuongMua <= 0) continue;

                    var ingredientMap = new Dictionary<int, decimal>();

                    // CongThuc_Pizza (nhân bánh)
                    var congThucs = await GetCongThucPizzaInternalAsync(conn, transaction, ct.MaHangHoa, ct.SizeID);
                    foreach (var recipe in congThucs)
                    {
                        var amount = await ConvertAmountToStockUnitAsync(
                            conn, transaction, recipe.NguyenLieuID,
                            (decimal)(recipe.SoLuong ?? 0), recipe.DonViID,
                            unitCtxCache, unitNmCache);
                        if (amount <= 0) continue;
                        if (ingredientMap.ContainsKey(recipe.NguyenLieuID))
                            ingredientMap[recipe.NguyenLieuID] += amount;
                        else
                            ingredientMap[recipe.NguyenLieuID] = amount;
                    }

                    // QuyDinh_Bot (bột mì)
                    if (!string.IsNullOrEmpty(ct.MaDeBanh))
                    {
                        var botSqlR = @"SELECT qb.TrongLuongBot, qb.DonViID
                                        FROM QuyDinh_Bot qb
                                        INNER JOIN DoanhMuc_De dd ON qb.LoaiCotBanh = dd.LoaiCotBanh
                                        WHERE qb.SizeID = @SizeID AND dd.MaDeBanh = @MaDeBanh";
                        using var botCmdR = CreateCommand(botSqlR, conn, transaction);
                        botCmdR.Parameters.AddWithValue("@SizeID", ct.SizeID);
                        botCmdR.Parameters.AddWithValue("@MaDeBanh", ct.MaDeBanh);
                        using var botReaderR = await botCmdR.ExecuteReaderAsync();
                        if (await botReaderR.ReadAsync())
                        {
                            var trongLuongBot = botReaderR.IsDBNull(0) ? 0m : Convert.ToDecimal(botReaderR.GetDouble(0));
                            int? donViBotId = botReaderR.IsDBNull(1) ? null : botReaderR.GetInt32(1);
                            botReaderR.Close();
                            if (trongLuongBot > 0)
                            {
                                var findBotSqlR = "SELECT TOP 1 NguyenLieuID FROM NguyenLieu WHERE TenNguyenLieu LIKE N'%Bột mì%' OR TenNguyenLieu LIKE N'%Bot mi%'";
                                using var findBotCmdR = CreateCommand(findBotSqlR, conn, transaction);
                                var botNlId = await findBotCmdR.ExecuteScalarAsync();
                                if (botNlId != null && botNlId != DBNull.Value)
                                {
                                    var nlId = Convert.ToInt32(botNlId);
                                    var normalizedBotAmount = await ConvertAmountToStockUnitAsync(
                                        conn, transaction, nlId, trongLuongBot, donViBotId,
                                        unitCtxCache, unitNmCache);
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

                    // QuyDinh_Vien (viền)
                    if (!string.IsNullOrEmpty(ct.MaDeBanh))
                    {
                        var vienSqlR = @"SELECT NguyenLieuID, SoLuongVien, DonViID FROM QuyDinh_Vien
                                         WHERE MaDeBanh = @MaDeBanh AND SizeID = @SizeID";
                        var vienItems = new List<(int NguyenLieuId, decimal SoLuong, int? DonViId)>();
                        using var vienCmdR = CreateCommand(vienSqlR, conn, transaction);
                        vienCmdR.Parameters.AddWithValue("@MaDeBanh", ct.MaDeBanh);
                        vienCmdR.Parameters.AddWithValue("@SizeID", ct.SizeID);
                        using (var vienReaderR = await vienCmdR.ExecuteReaderAsync())
                        {
                            while (await vienReaderR.ReadAsync())
                            {
                                vienItems.Add((
                                    vienReaderR.GetInt32(0),
                                    vienReaderR.IsDBNull(1) ? 0m : Convert.ToDecimal(vienReaderR.GetDouble(1)),
                                    vienReaderR.IsDBNull(2) ? null : vienReaderR.GetInt32(2)));
                            }
                        }
                        foreach (var vienItem in vienItems)
                        {
                            var soLuongVien = await ConvertAmountToStockUnitAsync(
                                conn, transaction, vienItem.NguyenLieuId, vienItem.SoLuong, vienItem.DonViId,
                                unitCtxCache, unitNmCache);
                            if (soLuongVien <= 0) continue;
                            if (ingredientMap.ContainsKey(vienItem.NguyenLieuId))
                                ingredientMap[vienItem.NguyenLieuId] += soLuongVien;
                            else
                                ingredientMap[vienItem.NguyenLieuId] = soLuongVien;
                        }
                    }

                    foreach (var (nguyenLieuId, amountPerPizza) in ingredientMap)
                    {
                        var totalAmount = amountPerPizza * soLuongMua;
                        if (totalAmount <= 0) continue;
                        if (restoreMap.ContainsKey(nguyenLieuId))
                            restoreMap[nguyenLieuId] += totalAmount;
                        else
                            restoreMap[nguyenLieuId] = totalAmount;
                    }
                }

                // 3) Hoàn trả nguyên liệu vào tồn kho
                foreach (var kvp in restoreMap)
                {
                    var restoreSql = @"UPDATE TonKho
                                       SET SoLuongTon = SoLuongTon + @SoLuong, NgayCapNhat = GETDATE()
                                       WHERE NguyenLieuID = @NguyenLieuID";
                    using var restoreCmd = new SqlCommand(restoreSql, conn, transaction);
                    restoreCmd.Parameters.AddWithValue("@SoLuong", kvp.Value);
                    restoreCmd.Parameters.AddWithValue("@NguyenLieuID", kvp.Key);
                    await restoreCmd.ExecuteNonQueryAsync();
                    await UpsertDuLieuTieuHaoAsync(conn, transaction, kvp.Key, ngayBanRestore, -kvp.Value);
                }

                // 4) Xóa chi tiết và phiếu bán hàng
                using var delCtR = new SqlCommand("DELETE FROM CT_PhieuBan WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                delCtR.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                await delCtR.ExecuteNonQueryAsync();

                using var delPbR = new SqlCommand("DELETE FROM PhieuBanHang WHERE MaPhieuBan = @MaPhieu", conn, transaction);
                delPbR.Parameters.AddWithValue("@MaPhieu", maPhieuBan);
                await delPbR.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch {
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
                    var details = new List<string>();
                    var hasFlourShortage = false;

                    foreach (var item in insufficient)
                    {
                        var unitContext = await GetIngredientUnitContextAsync(conn, transaction, item.Id, unitContextCache);
                        var unitSuffix = string.IsNullOrWhiteSpace(unitContext.StockUnitName)
                            ? string.Empty
                            : $" {unitContext.StockUnitName}";

                        details.Add($"{item.Name} (cần {item.Required:N2}{unitSuffix}, còn {item.Available:N2}{unitSuffix})");

                        if (item.Name.Contains("bột mì", StringComparison.CurrentCultureIgnoreCase) ||
                            item.Name.Contains("bot mi", StringComparison.CurrentCultureIgnoreCase))
                        {
                            hasFlourShortage = true;
                        }
                    }

                    var message = "Không đủ nguyên liệu: " + string.Join(", ", details);
                    if (hasFlourShortage)
                    {
                        message += ". Lưu ý: Bột mì được tính thêm từ quy định bột theo size/đế bánh, không chỉ từ công thức topping.";
                    }

                    throw new Exception(message);
                }
            }

            // 3) Lưu PhiếuBánHàng
            var sql = @"INSERT INTO PhieuBanHang (MaPhieuBan, NhanVienBanID, NgayBan, TongTien, PhuongThucTT, GhiChu, GiamGia, ThanhToan)
                       VALUES (@MaPhieuBan, @NhanVienBanID, @NgayBan, @TongTien, @PhuongThucTT, @GhiChu, @GiamGia, @ThanhToan)";
            using (var cmd = new SqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@MaPhieuBan", phieuBan.MaPhieuBan);
                cmd.Parameters.AddWithValue("@NhanVienBanID", phieuBan.NhanVienBanID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayBan", phieuBan.NgayBan ?? (object)DateTime.Now);
                cmd.Parameters.AddWithValue("@TongTien", phieuBan.TongTien ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PhuongThucTT", phieuBan.PhuongThucTT ?? "Tiền mặt");
                cmd.Parameters.AddWithValue("@GhiChu", phieuBan.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GiamGia", phieuBan.GiamGia ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ThanhToan", phieuBan.ThanhToan ?? (object)DBNull.Value);
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
                    // Lấy tên nguyên liệu để hiển thị thông báo rõ ràng
                    var nlNameSql = "SELECT TenNguyenLieu FROM NguyenLieu WHERE NguyenLieuID = @NguyenLieuID";
                    using var nlNameCmd = new SqlCommand(nlNameSql, conn, transaction);
                    nlNameCmd.Parameters.AddWithValue("@NguyenLieuID", kvp.Key);
                    var tenNL = await nlNameCmd.ExecuteScalarAsync() as string ?? $"ID {kvp.Key}";
                    throw new Exception($"Nguyên liệu '{tenNL}' không đủ tồn kho để chế biến.");
                }

                await DeductSoLuongConTheoLoAsync(
                    conn, transaction, kvp.Key, kvp.Value,
                    unitContextCache, unitNameCache);
                await UpsertDuLieuTieuHaoAsync(conn, transaction, kvp.Key, phieuBan.NgayBan ?? DateTime.Now, kvp.Value);
            }

            transaction.Commit();
            return phieuBan.MaPhieuBan;
        }
        catch {
            transaction.Rollback();
            throw;
        }
    }

    private async Task DeductSoLuongConTheoLoAsync(
        SqlConnection conn,
        SqlTransaction transaction,
        int nguyenLieuId,
        decimal soLuongTruTheoDonViTon,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        const decimal tolerance = 0.0001m;
        var remaining = soLuongTruTheoDonViTon;

        var batches = new List<(int ChiTietID, decimal SoLuongCon, int? DonViID)>();
        var sql = @"SELECT ct.ChiTietID, ct.SoLuongCon, ct.DonViID
                    FROM CT_PhieuNhap ct WITH (UPDLOCK, ROWLOCK)
                    INNER JOIN PhieuNhap pn ON ct.PhieuNhapID = pn.PhieuNhapID
                    WHERE ct.NguyenLieuID = @NguyenLieuID
                      AND pn.TrangThai = 2
                      AND ct.SoLuongCon > 0
                    ORDER BY CASE WHEN ct.HSD IS NULL THEN 1 ELSE 0 END,
                             ct.HSD ASC, pn.NgayNhap ASC, ct.ChiTietID ASC";

        using (var cmd = new SqlCommand(sql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                batches.Add((
                    reader.GetInt32(0),
                    reader.GetDecimal(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2)));
            }
        }

        foreach (var batch in batches)
        {
            if (remaining <= tolerance)
                break;

            var batchStockAmount = await ConvertAmountToStockUnitAsync(
                conn, transaction, nguyenLieuId, batch.SoLuongCon, batch.DonViID,
                contextCache, unitNameCache);

            if (batchStockAmount <= tolerance)
                continue;

            var takeStockAmount = remaining <= batchStockAmount ? remaining : batchStockAmount;
            var takeBatchAmount = await ConvertStockAmountToUnitAsync(
                conn, transaction, nguyenLieuId, takeStockAmount, batch.DonViID,
                contextCache, unitNameCache);

            if (takeBatchAmount > batch.SoLuongCon || batchStockAmount - takeStockAmount <= tolerance)
                takeBatchAmount = batch.SoLuongCon;

            var updateSql = @"UPDATE CT_PhieuNhap
                              SET SoLuongCon = CASE
                                  WHEN SoLuongCon - @SoLuongTru <= @Tolerance THEN 0
                                  ELSE SoLuongCon - @SoLuongTru
                              END
                              WHERE ChiTietID = @ChiTietID";
            using var updateCmd = new SqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@SoLuongTru", takeBatchAmount);
            updateCmd.Parameters.AddWithValue("@Tolerance", tolerance);
            updateCmd.Parameters.AddWithValue("@ChiTietID", batch.ChiTietID);
            await updateCmd.ExecuteNonQueryAsync();

            remaining -= takeStockAmount;
        }

        if (remaining > tolerance)
            throw new InvalidOperationException("Không đủ SoLuongCon theo lô để trừ tồn bán hàng.");
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
        var sql = @"SELECT MaHangHoa, SizeID, NguyenLieuID, SoLuong, DonViID FROM CongThuc_Pizza WHERE MaHangHoa = @MaHangHoa AND SizeID = @SizeID";
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

    private async Task UpsertDuLieuTieuHaoAsync(SqlConnection conn, SqlTransaction transaction, int nguyenLieuId, DateTime ngayGiaoDich, decimal deltaSoLuong)
    {
        try
        {
            var kyTieuHao = new DateTime(ngayGiaoDich.Year, ngayGiaoDich.Month, 1);
            var checkSql = @"SELECT TieuHaoID FROM DuLieuTieuHao WHERE NguyenLieuID = @NguyenLieuID AND KyTieuHao = @KyTieuHao";
            using var checkCmd = new SqlCommand(checkSql, conn, transaction);
            checkCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            checkCmd.Parameters.AddWithValue("@KyTieuHao", kyTieuHao);
            var existingId = await checkCmd.ExecuteScalarAsync();

            if (existingId != null && existingId != DBNull.Value)
            {
                var updateSql = @"UPDATE DuLieuTieuHao
                                  SET SoLuongTieuHao = CASE WHEN SoLuongTieuHao + @Delta < 0 THEN 0 ELSE SoLuongTieuHao + @Delta END
                                  WHERE TieuHaoID = @TieuHaoID";
                using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@Delta", (double)deltaSoLuong);
                updateCmd.Parameters.AddWithValue("@TieuHaoID", Convert.ToInt32(existingId));
                await updateCmd.ExecuteNonQueryAsync();
            }
            else if (deltaSoLuong > 0)
            {
                var insertSql = @"INSERT INTO DuLieuTieuHao (NguyenLieuID, DonViID, KyTieuHao, SoLuongTieuHao)
                                  SELECT @NguyenLieuID, ISNULL(DonViID, 1), @KyTieuHao, @SoLuong
                                  FROM NguyenLieu WHERE NguyenLieuID = @NguyenLieuID";
                using var insertCmd = new SqlCommand(insertSql, conn, transaction);
                insertCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
                insertCmd.Parameters.AddWithValue("@KyTieuHao", kyTieuHao);
                insertCmd.Parameters.AddWithValue("@SoLuong", (double)deltaSoLuong);
                await insertCmd.ExecuteNonQueryAsync();
            }

            // Làm mới dự báo của kỳ kế tiếp khi lượng tiêu hao thực tế trong kỳ thay đổi
            const string invalidateSql = @"
                DELETE FROM DuBao WHERE NguyenLieuID = @NguyenLieuID AND KyDuBao = DATEADD(MONTH, 1, @KyTieuHao);
                DELETE FROM DanhGiaDuBao WHERE NguyenLieuID = @NguyenLieuID AND DenKyTest = @KyTieuHao;";
            using var invCmd = new SqlCommand(invalidateSql, conn, transaction);
            invCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            invCmd.Parameters.AddWithValue("@KyTieuHao", kyTieuHao);
            await invCmd.ExecuteNonQueryAsync();
        }
        catch
        {
        }
    }
}
