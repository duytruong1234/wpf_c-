using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class PhieuXuatService : DatabaseContext
{
    public PhieuXuatService(string connectionString) : base(connectionString) { }

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
                              nvy.HoTen as TenNVYeuCau, nvd.HoTen as TenNVDuyet,
                              px.LoaiPhieu, px.KiemKeID, px.GhiChu, px.ChuKyXuat, px.ChuKySoDuyet
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
                    TrangThai = reader.GetByte(6),
                    LoaiPhieu = reader.IsDBNull(9) ? null : reader.GetString(9),
                    KiemKeID = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    GhiChu = reader.IsDBNull(11) ? null : reader.GetString(11),
                    ChuKyXuat = reader.IsDBNull(12) ? null : reader.GetString(12),
                    ChuKySoDuyet = reader.IsDBNull(13) ? null : reader.GetString(13)
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
        catch { }

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
                              nvy.HoTen as TenNVYeuCau, nvd.HoTen as TenNVDuyet,
                              px.LoaiPhieu, px.KiemKeID, px.GhiChu, px.ChuKyXuat, px.ChuKySoDuyet
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
                    TrangThai = reader.GetByte(6),
                    LoaiPhieu = reader.IsDBNull(9) ? null : reader.GetString(9),
                    KiemKeID = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    GhiChu = reader.IsDBNull(11) ? null : reader.GetString(11),
                    ChuKyXuat = reader.IsDBNull(12) ? null : reader.GetString(12),
                    ChuKySoDuyet = reader.IsDBNull(13) ? null : reader.GetString(13)
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
        catch { }

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
                              nl.MaNguyenLieu, nl.TenNguyenLieu, dv.TenDonVi,
                              dvChuan.TenDonVi AS TenDonViChuan, qdChuan.HeSo AS HeSoDonViChuan,
                              qdXuat.HeSo AS HeSoXuat
                       FROM CT_PhieuXuat ct
                       LEFT JOIN NguyenLieu nl ON ct.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON ct.DonViID = dv.DonViID
                       LEFT JOIN QuyDoiDonVi qdChuan ON ct.NguyenLieuID = qdChuan.NguyenLieuID AND qdChuan.LaDonViChuan = 1
                       LEFT JOIN DonViTinh dvChuan ON qdChuan.DonViID = dvChuan.DonViID
                       LEFT JOIN QuyDoiDonVi qdXuat ON ct.NguyenLieuID = qdXuat.NguyenLieuID AND ct.DonViID = qdXuat.DonViID
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

                // Đơn vị chuẩn
                chiTiet.TenDonViChuan = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
                chiTiet.HeSoDonViChuan = reader.IsDBNull(10) ? 1m : reader.GetDecimal(10);

                // Lấy HeSo đúng từ QuyDoiDonVi cho đơn vị xuất (dùng để tính số lượng lưu kho)
                if (!reader.IsDBNull(11))
                    chiTiet.HeSo = reader.GetDecimal(11);

                result.Add(chiTiet);
            }
        }
        catch { }

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
        catch {
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
                var sql = @"INSERT INTO PhieuXuat (MaPhieuXuat, NhanVienYeuID, NhanVienDuyetID, NgayYeuCau, NgayDuyet, TrangThai, LoaiPhieu, KiemKeID, GhiChu, ChuKyXuat)
                           VALUES (@MaPhieuXuat, @NhanVienYeuID, @NhanVienDuyetID, @NgayYeuCau, @NgayDuyet, @TrangThai, @LoaiPhieu, @KiemKeID, @GhiChu, @ChuKyXuat);
                           SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@MaPhieuXuat", phieuXuat.MaPhieuXuat ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienYeuID", phieuXuat.NhanVienYeuID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienDuyetID", phieuXuat.NhanVienDuyetID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayYeuCau", phieuXuat.NgayYeuCau);
                cmd.Parameters.AddWithValue("@NgayDuyet", phieuXuat.NgayDuyet ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuXuat.TrangThai);
                cmd.Parameters.AddWithValue("@LoaiPhieu", phieuXuat.LoaiPhieu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KiemKeID", phieuXuat.KiemKeID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", phieuXuat.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ChuKyXuat", phieuXuat.ChuKyXuat ?? (object)DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                phieuXuatId = Convert.ToInt32(result);
            }
            else
            {
                // Cập nhật phiếu xuất hiện có
                phieuXuatId = phieuXuat.PhieuXuatID;

                var sql = @"UPDATE PhieuXuat
                           SET NhanVienYeuID = @NhanVienYeuID, NhanVienDuyetID = @NhanVienDuyetID,
                               NgayYeuCau = @NgayYeuCau, NgayDuyet = @NgayDuyet, TrangThai = @TrangThai,
                               LoaiPhieu = @LoaiPhieu, KiemKeID = @KiemKeID, GhiChu = @GhiChu, ChuKyXuat = @ChuKyXuat
                           WHERE PhieuXuatID = @PhieuXuatID";

                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                cmd.Parameters.AddWithValue("@NhanVienYeuID", phieuXuat.NhanVienYeuID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NhanVienDuyetID", phieuXuat.NhanVienDuyetID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NgayYeuCau", phieuXuat.NgayYeuCau);
                cmd.Parameters.AddWithValue("@NgayDuyet", phieuXuat.NgayDuyet ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", phieuXuat.TrangThai);
                cmd.Parameters.AddWithValue("@LoaiPhieu", phieuXuat.LoaiPhieu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@KiemKeID", phieuXuat.KiemKeID ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", phieuXuat.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ChuKyXuat", phieuXuat.ChuKyXuat ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                // Xóa chi tiết hiện có
                var deleteSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // ThÃªm chi tiáº¿t
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
        catch {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ApprovePhieuXuatAsync(int phieuXuatId, int nhanVienDuyetId, string? chuKySo = null)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Lay chi tiet de cap nhat TonKho
            var chiTiets = new List<(int NguyenLieuID, decimal SoLuong, int? DonViID)>();

            var getSql = "SELECT NguyenLieuID, SoLuong, DonViID FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var getCmd = new SqlCommand(getSql, conn, transaction);
            getCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);

            using var reader = await getCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chiTiets.Add((
                    reader.GetInt32(0),
                    reader.GetDecimal(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2)));
            }
            reader.Close();

            // Cap nhat TonKho (tru)
            var contextCache = new Dictionary<int, IngredientUnitContext>();
            var unitNameCache = new Dictionary<int, string>();

            foreach (var ct in chiTiets)
            {
                // Quy doi so luong xuat ve don vi chuan (don vi luu ton kho)
                var soLuongTru = await ConvertAmountToStockUnitAsync(
                    conn, transaction, ct.NguyenLieuID, ct.SoLuong, ct.DonViID,
                    contextCache, unitNameCache);

                var checkSql = "SELECT ISNULL(SoLuongTon, 0) FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
                using var checkCmd = new SqlCommand(checkSql, conn, transaction);
                checkCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                var currentTon = Convert.ToDecimal(await checkCmd.ExecuteScalarAsync() ?? 0);

                if (currentTon < soLuongTru)
                {
                    // Lấy tên nguyên liệu để thông báo rõ ràng hơn
                    var nameSql = "SELECT TenNguyenLieu FROM NguyenLieu WHERE NguyenLieuID = @NguyenLieuID";
                    using var nameCmd = new SqlCommand(nameSql, conn, transaction);
                    nameCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                    var tenNL = await nameCmd.ExecuteScalarAsync() as string ?? $"ID {ct.NguyenLieuID}";

                    throw new Exception($"Tồn kho không đủ để xuất nguyên liệu '{tenNL}'. (Tồn: {currentTon:N2}, Cần xuất: {soLuongTru:N2})");
                }

                await DeductLoHangTheoHsdAsync(
                    conn, transaction, ct.NguyenLieuID, soLuongTru,
                    contextCache, unitNameCache);

                var updateSql = @"UPDATE TonKho
                                 SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                 WHERE NguyenLieuID = @NguyenLieuID";
                using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@SoLuong", soLuongTru);
                updateCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                await updateCmd.ExecuteNonQueryAsync();
            }

            // Cap nhat trang thai PhieuXuat + chữ ký số
            var sql = @"UPDATE PhieuXuat
                       SET TrangThai = 2, NhanVienDuyetID = @NhanVienDuyetID, NgayDuyet = GETDATE(),
                           ChuKySoDuyet = @ChuKySo
                       WHERE PhieuXuatID = @PhieuXuatID";

            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            cmd.Parameters.AddWithValue("@ChuKySo", (object?)chuKySo ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            transaction.Commit();
            return true;
        }
        catch {
            transaction.Rollback();
            throw;
        }
    }

    private async Task DeductLoHangTheoHsdAsync(
        SqlConnection conn,
        SqlTransaction transaction,
        int nguyenLieuId,
        decimal soLuongTruTheoDonViTon,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        const decimal tolerance = 0.0001m;
        var remaining = soLuongTruTheoDonViTon;

        var batches = new List<(int ChiTietID, decimal SoLuongCon, int? DonViID, DateTime? HSD, string MaPhieuNhap)>();
        var sql = @"SELECT ct.ChiTietID, ct.SoLuongCon, ct.DonViID, ct.HSD, ISNULL(pn.MaPhieuNhap, '')
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
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    reader.GetString(4)));
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
            throw new InvalidOperationException("Khong du SoLuongCon theo lo de xuat kho.");
    }

    public async Task<bool> CancelPhieuXuatAsync(int phieuXuatId, int nhanVienDuyetId, string? lyDoHuy = null, string? chuKyHuy = null)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE PhieuXuat
                        SET TrangThai = 3, GhiChu = @LyDoHuy, ChuKySoDuyet = @ChuKyHuy,
                            NhanVienDuyetID = @NhanVienDuyetID, NgayDuyet = GETDATE()
                        WHERE PhieuXuatID = @PhieuXuatID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            cmd.Parameters.AddWithValue("@NhanVienDuyetID", nhanVienDuyetId);
            cmd.Parameters.AddWithValue("@LyDoHuy", (object?)lyDoHuy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChuKyHuy", (object?)chuKyHuy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch {
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
            // XÃ³a chi tiáº¿t
            var deleteCtSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            await deleteCtCmd.ExecuteNonQueryAsync();

            // XÃ³a Phiáº¿uXuáº¥t
            var deleteSql = "DELETE FROM PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            await deleteCmd.ExecuteNonQueryAsync();

            transaction.Commit();
            return true;
        }
        catch {
            transaction.Rollback();
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
                              COALESCE(dvTk.TenDonVi, dv.TenDonVi) AS TenDonVi,
                              ISNULL(tk.SoLuongTon, 0) as SoLuongTon
                       FROM NguyenLieu nl
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                       LEFT JOIN DonViTinh dvTk ON tk.DonViID = dvTk.DonViID
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
        catch { }

        return result;
    }
}
