using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class KiemKeService : DatabaseContext
{
    public KiemKeService(string connectionString) : base(connectionString) { }

    private async Task EnsureKiemKeSchemaAsync(SqlConnection conn, SqlTransaction? transaction = null)
    {
        var sql = @"
            IF OBJECT_ID('dbo.CT_PhieuNhap', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.CT_PhieuNhap', 'SoLuongCon') IS NULL
            BEGIN
                ALTER TABLE dbo.CT_PhieuNhap
                ADD SoLuongCon DECIMAL(18,4) NOT NULL DEFAULT (0);

                EXEC(N'UPDATE dbo.CT_PhieuNhap SET SoLuongCon = SoLuong');
            END

            IF OBJECT_ID('dbo.CT_PhieuKiemKe', 'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('dbo.CT_PhieuKiemKe', 'ChiTietPhieuNhapID') IS NULL
                    ALTER TABLE dbo.CT_PhieuKiemKe ADD ChiTietPhieuNhapID INT NULL;

                IF COL_LENGTH('dbo.CT_PhieuKiemKe', 'LoaiDieuChinh') IS NULL
                    ALTER TABLE dbo.CT_PhieuKiemKe ADD LoaiDieuChinh NVARCHAR(10) NULL;

                IF COL_LENGTH('dbo.CT_PhieuKiemKe', 'PhieuDieuChinhID') IS NULL
                    ALTER TABLE dbo.CT_PhieuKiemKe ADD PhieuDieuChinhID UNIQUEIDENTIFIER NULL;
            END";

        using var cmd = new SqlCommand(sql, conn, transaction);
        await cmd.ExecuteNonQueryAsync();
    }

    // DANH SÁCH PHIẾU KIỂM KÊ ═══════════════

    public async Task<List<PhieuKiemKe>> GetPhieuKiemKesAsync(
        string? searchText = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null,
        string? trangThai = null)
    {
        var result = new List<PhieuKiemKe>();

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT pk.KiemKeID, pk.MaPhieuKiem, pk.NhanVienKiemID, pk.NhanVienDuyetID,
                              pk.NgayKiem, pk.NgayDuyet, pk.GhiChuDuyet, pk.LyDoKiemKe,
                              pk.LoaiKiemKe, pk.TrangThai, pk.ChuKyKiem, pk.ChuKyDuyet,
                              nvk.HoTen as TenNVKiem, nvd.HoTen as TenNVDuyet
                       FROM PhieuKiemKe pk
                       LEFT JOIN NhanVien nvk ON pk.NhanVienKiemID = nvk.NhanVienID
                       LEFT JOIN NhanVien nvd ON pk.NhanVienDuyetID = nvd.NhanVienID
                       WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(searchText))
                sql += " AND (pk.MaPhieuKiem LIKE @SearchText OR nvk.HoTen LIKE @SearchText)";
            if (tuNgay.HasValue)
                sql += " AND pk.NgayKiem >= @TuNgay";
            if (denNgay.HasValue)
                sql += " AND pk.NgayKiem <= @DenNgay";
            if (!string.IsNullOrWhiteSpace(trangThai))
                sql += " AND pk.TrangThai = @TrangThai";

            sql += " ORDER BY pk.NgayKiem DESC";

            using var cmd = new SqlCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(searchText))
                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
            if (tuNgay.HasValue)
                cmd.Parameters.AddWithValue("@TuNgay", tuNgay.Value.Date);
            if (denNgay.HasValue)
                cmd.Parameters.AddWithValue("@DenNgay", denNgay.Value.Date.AddDays(1).AddSeconds(-1));
            if (!string.IsNullOrWhiteSpace(trangThai))
                cmd.Parameters.AddWithValue("@TrangThai", trangThai);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var pk = new PhieuKiemKe
                {
                    KiemKeID = reader.GetInt32(0),
                    MaPhieuKiem = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NhanVienKiemID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    NhanVienDuyetID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    NgayKiem = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    NgayDuyet = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    GhiChuDuyet = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LyDoKiemKe = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LoaiKiemKe = reader.IsDBNull(8) ? null : reader.GetString(8),
                    TrangThai = reader.IsDBNull(9) ? "Bản Nháp" : reader.GetString(9),
                    ChuKyKiem = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ChuKyDuyet = reader.IsDBNull(11) ? null : reader.GetString(11)
                };

                if (!reader.IsDBNull(12))
                    pk.NhanVienKiem = new NhanVien { HoTen = reader.GetString(12) };
                if (!reader.IsDBNull(13))
                    pk.NhanVienDuyet = new NhanVien { HoTen = reader.GetString(13) };

                result.Add(pk);
            }
        }
        catch { }

        return result;
    }

    // CHI TIẾT PHIẾU KIỂM KÊ ═══════════════

    public async Task<List<CT_PhieuKiemKe>> GetChiTietPhieuKiemKeAsync(int kiemKeId)
    {
        var result = new List<CT_PhieuKiemKe>();

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            await EnsureKiemKeSchemaAsync(conn);

            var sql = @"SELECT ct.ChiTietKiemID, ct.KiemKeID, ct.NguyenLieuID,
                              ct.TonHeThong, ct.DonViKhoID, ct.TonThucTe, ct.DonViKiemID,
                              ct.HeSoKiem, ct.ChenhLech, ct.GhiChuNhanVien, ct.PhanHoiDuyet,
                              ct.ChiTietPhieuNhapID, ctpn.HSD,
                              ctpn.PhieuNhapID, pn.MaPhieuNhap, ctpn.HSD,
                              nl.MaNguyenLieu, nl.TenNguyenLieu,
                              dvk.TenDonVi as TenDVKho, dvkm.TenDonVi as TenDVKiem
                       FROM CT_PhieuKiemKe ct
                       LEFT JOIN CT_PhieuNhap ctpn ON ct.ChiTietPhieuNhapID = ctpn.ChiTietID
                       LEFT JOIN PhieuNhap pn ON ctpn.PhieuNhapID = pn.PhieuNhapID
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dvk ON ct.DonViKhoID = dvk.DonViID
                       LEFT JOIN DonViTinh dvkm ON ct.DonViKiemID = dvkm.DonViID
                       WHERE ct.KiemKeID = @KiemKeID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var ct = new CT_PhieuKiemKe
                {
                    ChiTietKiemID = reader.GetInt32(0),
                    KiemKeID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    NguyenLieuID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    TonHeThong = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    DonViKhoID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    TonThucTe = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    DonViKiemID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    HeSoKiem = reader.IsDBNull(7) ? 1m : reader.GetDecimal(7),
                    ChenhLech = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                    GhiChuNhanVien = reader.IsDBNull(9) ? null : reader.GetString(9),
                    PhanHoiDuyet = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ChiTietPhieuNhapID = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    NgayHetHanNhap = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                    PhieuNhapID = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    MaPhieuNhap = reader.IsDBNull(14) ? null : reader.GetString(14),
                    HSD = reader.IsDBNull(15)
                        ? (reader.IsDBNull(12) ? null : reader.GetDateTime(12))
                        : reader.GetDateTime(15)
                };

                ct.NguyenLieu = new NguyenLieu
                {
                    MaNguyenLieu = reader.IsDBNull(16) ? null : reader.GetString(16),
                    TenNguyenLieu = reader.IsDBNull(17) ? string.Empty : reader.GetString(17)
                };

                if (!reader.IsDBNull(18))
                    ct.DonViKho = new DonViTinh { TenDonVi = reader.GetString(18) };
                if (!reader.IsDBNull(19))
                    ct.DonViKiem = new DonViTinh { TenDonVi = reader.GetString(19) };

                result.Add(ct);
            }
        }
        catch { }

        return result;
    }

    // SINH MÃ PHIẾU ═══════════════

    public async Task<string> GenerateMaPhieuKiemAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT TOP 1 MaPhieuKiem FROM PhieuKiemKe
                         WHERE MaPhieuKiem LIKE 'PK%'
                         ORDER BY KiemKeID DESC";

            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                var lastCode = result.ToString()!;
                if (int.TryParse(lastCode.Substring(2), out int lastNumber))
                {
                    return $"PK{(lastNumber + 1):D6}";
                }
            }

            return "PK000001";
        }
        catch {
            return $"PK{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    // LƯU PHIẾU KIỂM KÊ ═══════════════

    public async Task<int> SavePhieuKiemKeAsync(PhieuKiemKe phieuKiemKe, List<CT_PhieuKiemKe> chiTiets)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        await EnsureKiemKeSchemaAsync(conn);
        using var transaction = conn.BeginTransaction();

        try
        {
            int kiemKeId;

            if (phieuKiemKe.KiemKeID == 0)
            {
                var sql = @"INSERT INTO PhieuKiemKe (MaPhieuKiem, NhanVienKiemID, NgayKiem, LyDoKiemKe, LoaiKiemKe, TrangThai)
                           VALUES (@MaPhieuKiem, @NhanVienKiemID, @NgayKiem, @LyDoKiemKe, @LoaiKiemKe, @TrangThai);
                           SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaPhieuKiem", phieuKiemKe.MaPhieuKiem ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienKiemID", phieuKiemKe.NhanVienKiemID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayKiem", phieuKiemKe.NgayKiem ?? DateTime.Now);
                cmd.Parameters.AddWithValue("@LyDoKiemKe", phieuKiemKe.LyDoKiemKe ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LoaiKiemKe", phieuKiemKe.LoaiKiemKe ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuKiemKe.TrangThai);

                var result = await cmd.ExecuteScalarAsync();
                kiemKeId = Convert.ToInt32(result);
            }
            else
            {
                kiemKeId = phieuKiemKe.KiemKeID;

                var sql = @"UPDATE PhieuKiemKe
                           SET LyDoKiemKe = @LyDoKiemKe, LoaiKiemKe = @LoaiKiemKe, TrangThai = @TrangThai
                           WHERE KiemKeID = @KiemKeID";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                cmd.Parameters.AddWithValue("@LyDoKiemKe", phieuKiemKe.LyDoKiemKe ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LoaiKiemKe", phieuKiemKe.LoaiKiemKe ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuKiemKe.TrangThai);

                await cmd.ExecuteNonQueryAsync();

                // Xóa chi tiết cũ
                var deleteSql = "DELETE FROM CT_PhieuKiemKe WHERE KiemKeID = @KiemKeID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // Thêm chi tiết
            foreach (var ct in chiTiets)
            {
                var ctSql = @"INSERT INTO CT_PhieuKiemKe
                             (KiemKeID, NguyenLieuID, TonHeThong, DonViKhoID, TonThucTe, DonViKiemID, HeSoKiem, ChenhLech,
                              GhiChuNhanVien, ChiTietPhieuNhapID, LoaiDieuChinh)
                             VALUES (@KiemKeID, @NguyenLieuID, @TonHeThong, @DonViKhoID, @TonThucTe, @DonViKiemID,
                                     @HeSoKiem, @ChenhLech, @GhiChuNhanVien, @ChiTietPhieuNhapID, @LoaiDieuChinh)";

                using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                var loaiDieuChinh = ct.ChenhLech > 0 ? "Nhập" : ct.ChenhLech < 0 ? "Xuất" : null;
                ctCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                ctCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@TonHeThong", ct.TonHeThong);
                ctCmd.Parameters.AddWithValue("@DonViKhoID", ct.DonViKhoID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@TonThucTe", ct.TonThucTe);
                ctCmd.Parameters.AddWithValue("@DonViKiemID", ct.DonViKiemID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@HeSoKiem", ct.HeSoKiem);
                ctCmd.Parameters.AddWithValue("@ChenhLech", ct.ChenhLech);
                ctCmd.Parameters.AddWithValue("@GhiChuNhanVien", ct.GhiChuNhanVien ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@ChiTietPhieuNhapID", ct.ChiTietPhieuNhapID ?? (object)DBNull.Value);
                ctCmd.Parameters.AddWithValue("@LoaiDieuChinh", loaiDieuChinh ?? (object)DBNull.Value);

                await ctCmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            return kiemKeId;
        }
        catch {
            transaction.Rollback();
            throw;
        }
    }

    // GỬI DUYỆT ═══════════════

    public async Task<bool> SubmitPhieuKiemKeAsync(int kiemKeId, string? chuKyKiem = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE PhieuKiemKe
                       SET TrangThai = N'Chờ Duyệt', ChuKyKiem = @ChuKyKiem
                       WHERE KiemKeID = @KiemKeID AND TrangThai = N'Bản Nháp'";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            cmd.Parameters.AddWithValue("@ChuKyKiem", (object?)chuKyKiem ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch {
            return false;
        }
    }

    // DUYỆT PHIẾU + AUTO-GEN NHẬP/XUẤT ═══════════════

    public async Task<bool> ApprovePhieuKiemKeAsync(int kiemKeId, int nhanVienDuyetId, string? chuKyDuyet = null, string? ghiChuDuyet = null)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        await EnsureKiemKeSchemaAsync(conn);
        using var transaction = conn.BeginTransaction();

        try
        {
            // 1. Cập nhật trạng thái phiếu kiểm kê → Đã Duyệt
            var updateSql = @"UPDATE PhieuKiemKe
                             SET TrangThai = N'Đã Duyệt', NhanVienDuyetID = @NhanVienDuyetID,
                                 NgayDuyet = GETDATE(), ChuKyDuyet = @ChuKyDuyet, GhiChuDuyet = @GhiChuDuyet
                             WHERE KiemKeID = @KiemKeID AND TrangThai = N'Chờ Duyệt'";

            using var updateCmd = new SqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            updateCmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            updateCmd.Parameters.AddWithValue("@ChuKyDuyet", (object?)chuKyDuyet ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@GhiChuDuyet", (object?)ghiChuDuyet ?? DBNull.Value);

            var rows = await updateCmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                transaction.Rollback();
                return false;
            }

            // Lấy MaPhieuKiem để làm ghi chú cho phiếu nhập/xuất tự sinh
            var getMaPhieuSql = "SELECT MaPhieuKiem FROM PhieuKiemKe WHERE KiemKeID = @KiemKeID";
            string maPhieuKiem = "";
            using (var getMaPhieuCmd = new SqlCommand(getMaPhieuSql, conn, transaction))
            {
                getMaPhieuCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                var val = await getMaPhieuCmd.ExecuteScalarAsync();
                if (val != null && val != DBNull.Value)
                {
                    maPhieuKiem = val.ToString()!;
                }
            }

            // 2. Lấy chi tiết phiếu kiểm kê
            var chiTiets = new List<(int NguyenLieuID, decimal ChenhLech, int? DonViKhoID, int? ChiTietPhieuNhapID, DateTime? HSD)>();

            var getSql = @"SELECT ct.NguyenLieuID, ct.ChenhLech, ct.DonViKhoID, ct.ChiTietPhieuNhapID, ctpn.HSD
                           FROM CT_PhieuKiemKe ct
                           LEFT JOIN CT_PhieuNhap ctpn ON ct.ChiTietPhieuNhapID = ctpn.ChiTietID
                           WHERE ct.KiemKeID = @KiemKeID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);

            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetDateTime(4)));
            }
            reader.Close();

            // 3. Phân loại chênh lệch: tăng → phiếu nhập, giảm → phiếu xuất
            var tang = chiTiets.Where(x => x.ChenhLech > 0).ToList();
            var giam = chiTiets.Where(x => x.ChenhLech < 0).ToList();

            // 4. Tạo phiếu nhập cho chênh lệch tăng
            if (tang.Count > 0)
            {
                var maPNSql = @"SELECT TOP 1 MaPhieuNhap FROM PhieuNhap WHERE MaPhieuNhap LIKE 'PN%' ORDER BY PhieuNhapID DESC";
                using var maPNCmd = new SqlCommand(maPNSql, conn, transaction);
                var lastPN = await maPNCmd.ExecuteScalarAsync();
                string maPN = "PN000001";
                if (lastPN != null && lastPN != DBNull.Value)
                {
                    var lastCode = lastPN.ToString()!;
                    if (int.TryParse(lastCode.Substring(2), out int lastNum))
                        maPN = $"PN{(lastNum + 1):D6}";
                }

                var insertPNSql = @"INSERT INTO PhieuNhap (MaPhieuNhap, NhanVienNhapID, NgayNhap, TongTien, TrangThai, LoaiPhieu, KiemKeID, NhanVienDuyetID, NgayDuyet, GhiChu)
                                   VALUES (@MaPN, NULL, GETDATE(), 0, 2, N'Nhập điều chỉnh kiểm kê', @KiemKeID, @NVDuyetID, GETDATE(), @GhiChu);
                                   SELECT SCOPE_IDENTITY();";
                using var insertPNCmd = new SqlCommand(insertPNSql, conn, transaction);
                insertPNCmd.Parameters.AddWithValue("@MaPN", maPN);
                insertPNCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                insertPNCmd.Parameters.AddWithValue("@NVDuyetID", nhanVienDuyetId);
                insertPNCmd.Parameters.AddWithValue("@GhiChu", $"Nhập phần thừa phát sinh từ đợt kiểm kê {maPhieuKiem}");
                var phieuNhapId = Convert.ToInt32(await insertPNCmd.ExecuteScalarAsync());

                foreach (var ct in tang)
                {
                    var ctSql = @"INSERT INTO CT_PhieuNhap (PhieuNhapID, NguyenLieuID, SoLuong, DonViID, HeSo, DonGia, ThanhTien, HSD, SoLuongCon)
                                 VALUES (@PNID, @NLID, @SoLuong, @DVID, 1, 0, 0, @HSD, @SoLuong)";
                    using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                    ctCmd.Parameters.AddWithValue("@PNID", phieuNhapId);
                    ctCmd.Parameters.AddWithValue("@NLID", ct.NguyenLieuID);
                    ctCmd.Parameters.AddWithValue("@SoLuong", ct.ChenhLech);
                    ctCmd.Parameters.AddWithValue("@DVID", ct.DonViKhoID ?? (object)DBNull.Value);
                    ctCmd.Parameters.AddWithValue("@HSD", ct.HSD ?? (object)DBNull.Value);
                    await ctCmd.ExecuteNonQueryAsync();

                    // Cập nhật tồn kho: + chênh lệch
                    await AdjustTonKhoAsync(conn, transaction, ct.NguyenLieuID, ct.ChenhLech, ct.DonViKhoID);
                }
            }

            // 5. Tạo phiếu xuất cho chênh lệch giảm
            if (giam.Count > 0)
            {
                var maPXSql = @"SELECT TOP 1 MaPhieuXuat FROM PhieuXuat WHERE MaPhieuXuat LIKE 'PX%' ORDER BY PhieuXuatID DESC";
                using var maPXCmd = new SqlCommand(maPXSql, conn, transaction);
                var lastPX = await maPXCmd.ExecuteScalarAsync();
                string maPX = "PX000001";
                if (lastPX != null && lastPX != DBNull.Value)
                {
                    var lastCode = lastPX.ToString()!;
                    if (int.TryParse(lastCode.Substring(2), out int lastNum))
                        maPX = $"PX{(lastNum + 1):D6}";
                }

                var insertPXSql = @"INSERT INTO PhieuXuat (MaPhieuXuat, NhanVienYeuID, NgayYeuCau, TrangThai, LoaiPhieu, KiemKeID, NhanVienDuyetID, NgayDuyet, GhiChu)
                                   VALUES (@MaPX, NULL, GETDATE(), 2, N'Xuất điều chỉnh hao hụt', @KiemKeID, @NVDuyetID, GETDATE(), @GhiChu);
                                   SELECT SCOPE_IDENTITY();";
                using var insertPXCmd = new SqlCommand(insertPXSql, conn, transaction);
                insertPXCmd.Parameters.AddWithValue("@MaPX", maPX);
                insertPXCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
                insertPXCmd.Parameters.AddWithValue("@NVDuyetID", nhanVienDuyetId);
                insertPXCmd.Parameters.AddWithValue("@GhiChu", $"Xuất trừ phần hao hụt thất thoát từ đợt kiểm kê {maPhieuKiem}");
                var phieuXuatId = Convert.ToInt32(await insertPXCmd.ExecuteScalarAsync());

                var contextCache = new Dictionary<int, IngredientUnitContext>();
                var unitNameCache = new Dictionary<int, string>();

                foreach (var ct in giam)
                {
                    var soLuongXuat = Math.Abs(ct.ChenhLech);
                    var ctSql = @"INSERT INTO CT_PhieuXuat (PhieuXuatID, NguyenLieuID, SoLuong, DonViID, HeSo)
                                 VALUES (@PXID, @NLID, @SoLuong, @DVID, 1)";
                    using var ctCmd = new SqlCommand(ctSql, conn, transaction);
                    ctCmd.Parameters.AddWithValue("@PXID", phieuXuatId);
                    ctCmd.Parameters.AddWithValue("@NLID", ct.NguyenLieuID);
                    ctCmd.Parameters.AddWithValue("@SoLuong", soLuongXuat);
                    ctCmd.Parameters.AddWithValue("@DVID", ct.DonViKhoID ?? (object)DBNull.Value);
                    await ctCmd.ExecuteNonQueryAsync();

                    await DeductLoKiemKeAsync(
                        conn, transaction, ct.NguyenLieuID, soLuongXuat,
                        ct.ChiTietPhieuNhapID, ct.HSD,
                        contextCache, unitNameCache);

                    // Cập nhật tồn kho: - chênh lệch (đã âm nên ChenhLech < 0)
                    await AdjustTonKhoAsync(conn, transaction, ct.NguyenLieuID, ct.ChenhLech, ct.DonViKhoID);
                }
            }

            transaction.Commit();
            return true;
        }
        catch {
            transaction.Rollback();
            throw;
        }
    }

    // HỦY PHIẾU ═══════════════

    public async Task<bool> RejectPhieuKiemKeAsync(int kiemKeId, int nhanVienDuyetId, string? ghiChuDuyet = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE PhieuKiemKe
                       SET TrangThai = N'Đã Hủy', NhanVienDuyetID = @NhanVienDuyetID,
                           NgayDuyet = GETDATE(), GhiChuDuyet = @GhiChuDuyet
                       WHERE KiemKeID = @KiemKeID AND TrangThai = N'Chờ Duyệt'";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            cmd.Parameters.AddWithValue("@GhiChuDuyet", (object?)ghiChuDuyet ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch {
            return false;
        }
    }

    // LẤY NGUYÊN LIỆU + TỒN KHO CHO KIỂM KÊ ═══════════════

    public async Task<List<NguyenLieu>> GetNguyenLieusForKiemKeAsync()
    {
        var result = new List<NguyenLieu>();

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu,
                              nl.DonViID, nl.TrangThai,
                              COALESCE(dvTk.TenDonVi, dv.TenDonVi) AS TenDonVi,
                              ISNULL(tk.SoLuongTon, 0) as SoLuongTon,
                              COALESCE(tk.DonViID, nl.DonViID) as DonViKhoID
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                       LEFT JOIN DonViTinh dvTk ON tk.DonViID = dvTk.DonViID
                       WHERE nl.TrangThai = 1
                       ORDER BY nl.TenNguyenLieu";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var nl = new NguyenLieu
                {
                    NguyenLieuID = reader.GetInt32(0),
                    MaNguyenLieu = reader.IsDBNull(1) ? null : reader.GetString(1),
                    TenNguyenLieu = reader.GetString(2),
                    DonViID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    TrangThai = reader.GetBoolean(4)
                };

                if (!reader.IsDBNull(5))
                    nl.DonViTinh = new DonViTinh { TenDonVi = reader.GetString(5) };

                nl.TonKho = new TonKho
                {
                    SoLuongTon = reader.GetDecimal(6),
                    DonViID = reader.IsDBNull(7) ? null : reader.GetInt32(7)
                };

                result.Add(nl);
            }
        }
        catch { }

        return result;
    }

    // YÊU CẦU ĐẾM LẠI ═══════════════

    public async Task<bool> RequestRecountPhieuKiemKeAsync(int kiemKeId, int nhanVienDuyetId, string? ghiChuDuyet = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE PhieuKiemKe
                       SET TrangThai = N'Bản Nháp', NhanVienDuyetID = @NhanVienDuyetID,
                           NgayDuyet = GETDATE(), GhiChuDuyet = @GhiChuDuyet
                       WHERE KiemKeID = @KiemKeID AND TrangThai = N'Chờ Duyệt'";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            cmd.Parameters.AddWithValue("@GhiChuDuyet", (object?)ghiChuDuyet ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch {
            return false;
        }
    }

    // XÓA PHIẾU KIỂM KÊ ═══════════════

    public async Task<bool> DeletePhieuKiemKeAsync(int kiemKeId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            var deleteCtSql = "DELETE FROM CT_PhieuKiemKe WHERE KiemKeID = @KiemKeID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            await deleteCtCmd.ExecuteNonQueryAsync();

            var deleteSql = "DELETE FROM PhieuKiemKe WHERE KiemKeID = @KiemKeID AND TrangThai = N'Bản Nháp'";
            using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@KiemKeID", kiemKeId);
            var rows = await deleteCmd.ExecuteNonQueryAsync();

            transaction.Commit();
            return rows > 0;
        }
        catch {
            transaction.Rollback();
            return false;
        }
    }

    // HELPER: ĐIỀU CHỈNH TỒN KHO ═══════════════

    private async Task DeductLoKiemKeAsync(
        SqlConnection conn,
        SqlTransaction transaction,
        int nguyenLieuId,
        decimal soLuongTruTheoDonViTon,
        int? chiTietPhieuNhapId,
        DateTime? hsd,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        const decimal tolerance = 0.0001m;
        if (soLuongTruTheoDonViTon <= tolerance)
            return;

        var batches = new List<(int ChiTietID, decimal SoLuongCon, int? DonViID, DateTime? HSD, string MaPhieuNhap)>();
        var sql = chiTietPhieuNhapId.HasValue
            ? @"SELECT ctpn.ChiTietID, ctpn.SoLuongCon, ctpn.DonViID, ctpn.HSD, ISNULL(pn.MaPhieuNhap, '')
                FROM CT_PhieuNhap ctpn WITH (UPDLOCK, ROWLOCK)
                LEFT JOIN PhieuNhap pn ON ctpn.PhieuNhapID = pn.PhieuNhapID
                WHERE ctpn.ChiTietID = @ChiTietPhieuNhapID
                  AND ctpn.NguyenLieuID = @NguyenLieuID
                  AND ctpn.SoLuongCon > 0"
            : @"SELECT ctpn.ChiTietID, ctpn.SoLuongCon, ctpn.DonViID, ctpn.HSD, ISNULL(pn.MaPhieuNhap, '')
                FROM CT_PhieuNhap ctpn WITH (UPDLOCK, ROWLOCK)
                LEFT JOIN PhieuNhap pn ON ctpn.PhieuNhapID = pn.PhieuNhapID
                WHERE ctpn.NguyenLieuID = @NguyenLieuID
                  AND ctpn.SoLuongCon > 0
                  AND ((@HSD IS NULL AND ctpn.HSD IS NULL) OR ctpn.HSD = @HSD)
                ORDER BY ctpn.ChiTietID ASC";

        using (var cmd = new SqlCommand(sql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            if (chiTietPhieuNhapId.HasValue)
            {
                cmd.Parameters.AddWithValue("@ChiTietPhieuNhapID", chiTietPhieuNhapId.Value);
            }
            else
            {
                var hsdParam = cmd.Parameters.Add("@HSD", SqlDbType.Date);
                hsdParam.Value = hsd?.Date ?? (object)DBNull.Value;
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                batches.Add((
                    reader.GetInt32(0),
                    reader.GetDecimal(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    reader.GetString(4)));
            }
        }

        if (batches.Count == 0)
        {
            var hsdText = hsd?.ToString("dd/MM/yyyy") ?? "không có HSD";
            throw new InvalidOperationException($"Không tìm thấy lô nhập còn tồn đúng HSD {hsdText} để trừ hao hụt kiểm kê.");
        }

        var remaining = soLuongTruTheoDonViTon;
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
        {
            var hsdText = hsd?.ToString("dd/MM/yyyy") ?? "không có HSD";
            throw new InvalidOperationException($"Số lượng còn của lô/HSD {hsdText} không đủ để trừ hao hụt kiểm kê.");
        }
    }

    private async Task AdjustTonKhoAsync(SqlConnection conn, SqlTransaction transaction, int nguyenLieuId, decimal chenhLech, int? donViKhoId)
    {
        // Kiểm tra tồn kho đã tồn tại chưa
        var checkSql = "SELECT COUNT(*) FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
        checkCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (count > 0)
        {
            var updateSql = @"UPDATE TonKho
                             SET SoLuongTon = SoLuongTon + @ChenhLech, NgayCapNhat = GETDATE()
                             WHERE NguyenLieuID = @NguyenLieuID";
            using var updateCmd = new SqlCommand(updateSql, conn, transaction);
            updateCmd.Parameters.AddWithValue("@ChenhLech", chenhLech);
            updateCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            await updateCmd.ExecuteNonQueryAsync();
        }
        else if (chenhLech > 0)
        {
            var insertSql = @"INSERT INTO TonKho (NguyenLieuID, SoLuongTon, NgayCapNhat, DonViID)
                             VALUES (@NguyenLieuID, @SoLuong, GETDATE(), @DonViID)";
            using var insertCmd = new SqlCommand(insertSql, conn, transaction);
            insertCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            insertCmd.Parameters.AddWithValue("@SoLuong", chenhLech);
            insertCmd.Parameters.AddWithValue("@DonViID", donViKhoId ?? (object)DBNull.Value);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<LoKiemKeOption>> GetLoHangsForKiemKeAsync(int nguyenLieuId)
    {
        var result = new List<LoKiemKeOption>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            await EnsureKiemKeSchemaAsync(conn);
            var sql = @"
                SELECT 
                    ctpn.ChiTietID, ctpn.PhieuNhapID, pn.MaPhieuNhap, pn.NgayNhap,
                    ctpn.NguyenLieuID, nl.TenNguyenLieu, 
                    COALESCE(qdChuan.DonViID, ctpn.DonViID) AS DonViID,
                    COALESCE(dvChuan.TenDonVi, dv.TenDonVi) AS TenDonVi,
                    COALESCE(qdChuan.HeSo, ctpn.HeSo) AS HeSo,
                    (ctpn.SoLuongCon * COALESCE(qdNhap.HeSo, ctpn.HeSo) / COALESCE(qdChuan.HeSo, 1.0)) AS SoLuongCon,
                    ctpn.HSD
                FROM CT_PhieuNhap ctpn
                JOIN PhieuNhap pn ON ctpn.PhieuNhapID = pn.PhieuNhapID
                JOIN NguyenLieu nl ON ctpn.NguyenLieuID = nl.NguyenLieuID
                LEFT JOIN DonViTinh dv ON ctpn.DonViID = dv.DonViID
                LEFT JOIN QuyDoiDonVi qdChuan ON ctpn.NguyenLieuID = qdChuan.NguyenLieuID AND qdChuan.LaDonViChuan = 1
                LEFT JOIN DonViTinh dvChuan ON qdChuan.DonViID = dvChuan.DonViID
                LEFT JOIN QuyDoiDonVi qdNhap ON ctpn.NguyenLieuID = qdNhap.NguyenLieuID AND ctpn.DonViID = qdNhap.DonViID
                WHERE ctpn.NguyenLieuID = @NguyenLieuID
                  AND pn.TrangThai = 2
                  AND ctpn.SoLuongCon > 0
                ORDER BY ctpn.HSD ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LoKiemKeOption
                {
                    ChiTietPhieuNhapID = reader.GetInt32(0),
                    PhieuNhapID = reader.GetInt32(1),
                    MaPhieuNhap = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    NgayNhap = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                    NguyenLieuID = reader.GetInt32(4),
                    TenNguyenLieu = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    DonViID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    TenDonVi = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    HeSo = reader.IsDBNull(8) ? 1m : reader.GetDecimal(8),
                    SoLuongCon = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9),
                    HSD = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
                });
            }
        }
        catch (Exception ex)
        { 
            System.Windows.MessageBox.Show(ex.ToString(), "DB Error");
        }
        return result;
    }
}
