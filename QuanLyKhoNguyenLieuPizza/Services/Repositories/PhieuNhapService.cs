using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class PhieuNhapService : DatabaseContext
{
    public PhieuNhapService(string connectionString) : base(connectionString) { }

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
                              pn.NhanVienDuyetID, pn.NgayDuyet, nvd.HoTen as TenNhanVienDuyet,
                              pn.GhiChu, pn.ChuKySoDuyet, pn.LoaiPhieu, pn.KiemKeID, pn.ChuKyNhap
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
                    NgayDuyet = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    GhiChu = reader.IsDBNull(12) ? null : reader.GetString(12),
                    ChuKySoDuyet = reader.IsDBNull(13) ? null : reader.GetString(13),
                    LoaiPhieu = reader.IsDBNull(14) ? null : reader.GetString(14),
                    KiemKeID = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    ChuKyNhap = reader.IsDBNull(16) ? null : reader.GetString(16)
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
        catch { }

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
                              pn.NhanVienDuyetID, pn.NgayDuyet, nvd.HoTen as TenNhanVienDuyet,
                              pn.GhiChu, pn.ChuKySoDuyet, pn.LoaiPhieu, pn.KiemKeID, pn.ChuKyNhap
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
                    NgayDuyet = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    GhiChu = reader.IsDBNull(13) ? null : reader.GetString(13),
                    ChuKySoDuyet = reader.IsDBNull(14) ? null : reader.GetString(14),
                    LoaiPhieu = reader.IsDBNull(15) ? null : reader.GetString(15),
                    KiemKeID = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                    ChuKyNhap = reader.IsDBNull(17) ? null : reader.GetString(17)
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
        catch { }

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
                              nl.MaNguyenLieu, nl.TenNguyenLieu, dv.TenDonVi,
                              dvChuan.TenDonVi AS TenDonViChuan, qdChuan.HeSo AS HeSoDonViChuan
                       FROM CT_PhieuNhap ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       LEFT JOIN QuyDoiDonVi qdChuan ON ct.NguyenLieuID = qdChuan.NguyenLieuID AND qdChuan.HeSo = 1
                       LEFT JOIN DonViTinh dvChuan ON qdChuan.DonViID = dvChuan.DonViID
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

                // Đơn vị chuẩn
                chiTiet.TenDonViChuan = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
                chiTiet.HeSoDonViChuan = reader.IsDBNull(13) ? 1m : reader.GetDecimal(13);

                result.Add(chiTiet);
            }
        }
        catch { }

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
        catch { }

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
        catch { }

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
        catch {
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
                var sql = @"INSERT INTO PhieuNhap (MaPhieuNhap, NhanVienNhapID, NhaCungCapID, NgayNhap, TongTien, TrangThai, LoaiPhieu, KiemKeID, ChuKyNhap)
                           VALUES (@MaPhieuNhap, @NhanVienNhapID, @NhaCungCapID, @NgayNhap, @TongTien, @TrangThai, @LoaiPhieu, @KiemKeID, @ChuKyNhap);
                           SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaPhieuNhap", phieuNhap.MaPhieuNhap ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienNhapID", phieuNhap.NhanVienNhapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhaCungCapID", phieuNhap.NhaCungCapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayNhap", phieuNhap.NgayNhap);
                cmd.Parameters.AddWithValue("@TongTien", phieuNhap.TongTien);
                cmd.Parameters.AddWithValue("@TrangThai", phieuNhap.TrangThai);
                cmd.Parameters.AddWithValue("@LoaiPhieu", phieuNhap.LoaiPhieu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KiemKeID", phieuNhap.KiemKeID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ChuKyNhap", phieuNhap.ChuKyNhap ?? (object)DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                phieuNhapId = Convert.ToInt32(result);
            }
            else
            {
                // Cập nhật phiếu nhập hiện có
                phieuNhapId = phieuNhap.PhieuNhapID;

                var sql = @"UPDATE PhieuNhap
                           SET NhanVienNhapID = @NhanVienNhapID, NhaCungCapID = @NhaCungCapID,
                               NgayNhap = @NgayNhap, TongTien = @TongTien, TrangThai = @TrangThai,
                               LoaiPhieu = @LoaiPhieu, KiemKeID = @KiemKeID, ChuKyNhap = @ChuKyNhap
                           WHERE PhieuNhapID = @PhieuNhapID";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                cmd.Parameters.AddWithValue("@NhanVienNhapID", phieuNhap.NhanVienNhapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhaCungCapID", phieuNhap.NhaCungCapID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayNhap", phieuNhap.NgayNhap);
                cmd.Parameters.AddWithValue("@TongTien", phieuNhap.TongTien);
                cmd.Parameters.AddWithValue("@TrangThai", phieuNhap.TrangThai);
                cmd.Parameters.AddWithValue("@LoaiPhieu", phieuNhap.LoaiPhieu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KiemKeID", phieuNhap.KiemKeID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ChuKyNhap", phieuNhap.ChuKyNhap ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                // Xóa chi tiết hiện có
                var deleteSql = "DELETE FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // ThÃªm chi tiáº¿t
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

                // KHÃ”NG cáº­p nháº­t Tá»“nKho á»Ÿ Ä‘Ã¢y - chá»‰ cáº­p nháº­t khi duyá»‡t phiáº¿u
            }

            transaction.Commit();
            return phieuNhapId;
        }
        catch {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ApprovePhieuNhapAsync(int phieuNhapId, int nhanVienDuyetId, string? chuKySo = null)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Debug: Kiểm tra TrangThai thực sự trong DB trước khi UPDATE
            var checkSql = "SELECT TrangThai FROM PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var checkCmd = new SqlCommand(checkSql, conn, transaction);
            checkCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            var currentStatus = await checkCmd.ExecuteScalarAsync();

            // Cập nhật trạng thái phiếu nhập + chữ ký số
            var sql = @"UPDATE PhieuNhap
                       SET TrangThai = 2, NhanVienDuyetID = @NhanVienDuyetID, NgayDuyet = GETDATE(),
                           ChuKySoDuyet = @ChuKySo
                       WHERE PhieuNhapID = @PhieuNhapID AND TrangThai = 1";

            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            cmd.Parameters.AddWithValue("@ChuKySo", (object?)chuKySo ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                transaction.Rollback();
                return false;
            }

            // Láº¥y chi tiáº¿t phiáº¿u nháº­p Ä‘á»ƒ cáº­p nháº­t Tá»“nKho
            var getSql = "SELECT NguyenLieuID, SoLuong, DonViID FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);

            var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, int? DonViID)>();
            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((reader.GetInt32(0), reader.GetDecimal(1), reader.IsDBNull(2) ? null : reader.GetInt32(2)));
            }
            reader.Close();

            // Cáº­p nháº­t Tá»“nKho
            var contextCache = new Dictionary<int, IngredientUnitContext>();
            var unitNameCache = new Dictionary<int, string>();

            foreach (var ct in chiTiets)
            {
                var soLuongCong = await ConvertAmountToStockUnitAsync(
                    conn, transaction, ct.NguyenLieuID, ct.SoLuong, ct.DonViID,
                    contextCache, unitNameCache);

                await UpdateTonKhoOnNhapAsync(conn, transaction, ct.NguyenLieuID, soLuongCong);
            }

            transaction.Commit();
            return true;
        }
        catch {
            transaction.Rollback();
            throw; // Ném lỗi lên ViewModel để hiện cho user
        }
    }

    public async Task<bool> CancelPhieuNhapAsync(int phieuNhapId, int nguoiHuyId, string? lyDoHuy = null, string? chuKyHuy = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE PhieuNhap
                        SET TrangThai = 3, NhanVienDuyetID = @NguoiHuyId, NgayDuyet = GETDATE(), GhiChu = @GhiChu, ChuKySoDuyet = @ChuKyHuy
                        WHERE PhieuNhapID = @PhieuNhapID AND TrangThai = 1";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            cmd.Parameters.AddWithValue("@NguoiHuyId", nguoiHuyId);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)lyDoHuy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChuKyHuy", (object?)chuKyHuy ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch {
            return false;
        }
    }

    private async Task UpdateTonKhoOnNhapAsync(SqlConnection conn, SqlTransaction transaction, int nguyenLieuId, decimal soLuong)
    {
        // Lấy DonViID chuẩn: ưu tiên QuyDoiDonVi (LaDonViChuan=1), fallback về NguyenLieu.DonViID
        int? donViChuanId = null;
        var dvSql = @"SELECT TOP 1 COALESCE(qd.DonViID, nl.DonViID)
                      FROM NguyenLieu nl
                      LEFT JOIN QuyDoiDonVi qd ON nl.NguyenLieuID = qd.NguyenLieuID AND qd.LaDonViChuan = 1
                      WHERE nl.NguyenLieuID = @NguyenLieuID";
        using (var dvCmd = new SqlCommand(dvSql, conn, transaction))
        {
            dvCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            var dvResult = await dvCmd.ExecuteScalarAsync();
            if (dvResult != null && dvResult != DBNull.Value)
                donViChuanId = Convert.ToInt32(dvResult);
        }

        // Kiểm tra nếu TồnKho đã tồn tại
        var checkSql = "SELECT COUNT(*) FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
        checkCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (count > 0)
        {
            var updateSql = @"UPDATE TonKho
                             SET SoLuongTon = SoLuongTon + @SoLuong, NgayCapNhat = GETDATE(),
                                 DonViID = COALESCE(DonViID, @DonViID)
                             WHERE NguyenLieuID = @NguyenLieuID";
            using var updateCmd = new SqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@SoLuong", soLuong);
            updateCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            updateCmd.Parameters.AddWithValue("@DonViID", donViChuanId ?? (object)DBNull.Value);
            await updateCmd.ExecuteNonQueryAsync();
        }
        else
        {
            var insertSql = @"INSERT INTO TonKho (NguyenLieuID, SoLuongTon, NgayCapNhat, DonViID)
                             VALUES (@NguyenLieuID, @SoLuong, GETDATE(), @DonViID)";
            using var insertCmd = new SqlCommand(insertSql, conn, transaction);
            insertCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            insertCmd.Parameters.AddWithValue("@SoLuong", soLuong);
            insertCmd.Parameters.AddWithValue("@DonViID", donViChuanId ?? (object)DBNull.Value);
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
            // Kiểm tra TrangThai của PhieuNhap để biết có cần hoàn tác TồnKho hay không
            var statusSql = "SELECT TrangThai FROM PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            byte trangThai = 0;
            using (var statusCmd = new SqlCommand(statusSql, conn, transaction))
            {
                statusCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                var statusResult = await statusCmd.ExecuteScalarAsync();
                if (statusResult != null && statusResult != DBNull.Value)
                {
                    trangThai = Convert.ToByte(statusResult);
                }
            }

            // Chỉ hoàn tác tồn kho nếu phiếu đã được duyệt (TrangThai == 2)
            if (trangThai == 2)
            {
                // Lấy chi tiết để hoàn tác TồnKho
                var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, int? DonViID)>();

                var getSql = "SELECT NguyenLieuID, SoLuong, DonViID FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
                using var getCmd = new SqlCommand(getSql, conn, transaction);
                getCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);

                using var reader = await getCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    chiTiets.Add((reader.GetInt32(0), reader.GetDecimal(1), reader.IsDBNull(2) ? null : reader.GetInt32(2)));
                }
                reader.Close();

                // Hoàn tác Tồn kho
                var contextCache = new Dictionary<int, IngredientUnitContext>();
                var unitNameCache = new Dictionary<int, string>();

                foreach (var ct in chiTiets)
                {
                    var soLuongHoanTac = await ConvertAmountToStockUnitAsync(
                        conn, transaction, ct.NguyenLieuID, ct.SoLuong, ct.DonViID,
                        contextCache, unitNameCache);

                    // Kiểm tra tồn kho trước khi hoàn tác (để tránh tồn kho bị âm)
                    var checkTonSql = "SELECT SoLuongTon FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
                    decimal soLuongTonHienTai = 0;
                    using (var checkTonCmd = new SqlCommand(checkTonSql, conn, transaction))
                    {
                        checkTonCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                        var tonResult = await checkTonCmd.ExecuteScalarAsync();
                        if (tonResult != null && tonResult != DBNull.Value)
                        {
                            soLuongTonHienTai = Convert.ToDecimal(tonResult);
                        }
                    }

                    if (soLuongTonHienTai < soLuongHoanTac)
                    {
                        // Lấy tên nguyên liệu để thông báo lỗi rõ ràng
                        var getTenSql = "SELECT TenNguyenLieu FROM NguyenLieu WHERE NguyenLieuID = @NguyenLieuID";
                        string tenNL = "Nguyên liệu";
                        using (var getTenCmd = new SqlCommand(getTenSql, conn, transaction))
                        {
                            getTenCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                            var tenResult = await getTenCmd.ExecuteScalarAsync();
                            if (tenResult != null && tenResult != DBNull.Value)
                            {
                                tenNL = tenResult.ToString() ?? "Nguyên liệu";
                            }
                        }
                        throw new InvalidOperationException($"Không thể hoàn tác tồn kho cho nguyên liệu '{tenNL}' vì số lượng tồn hiện tại ({soLuongTonHienTai:N2}) nhỏ hơn số lượng cần hoàn tác ({soLuongHoanTac:N2}).");
                    }

                    var updateSql = @"UPDATE TonKho
                                     SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                     WHERE NguyenLieuID = @NguyenLieuID";
                    using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@SoLuong", soLuongHoanTac);
                    updateCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                    await updateCmd.ExecuteNonQueryAsync();
                }
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
        catch
        {
            transaction.Rollback();
            throw;
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
        catch { }

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
        catch {
            return 0;
        }
    }
}
