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
                // ThÃªm phiáº¿u xuáº¥t má»›i
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
                // Cáº­p nháº­t phiáº¿u xuáº¥t hiá»‡n cÃ³
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
                
                // XÃ³a chi tiáº¿t hiá»‡n cÃ³
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
            // Láº¥y chi tiáº¿t Ä‘á»ƒ cáº­p nháº­t Tá»“nKho
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
            
            // Cáº­p nháº­t Tá»“nKho (trá»«)
            foreach (var ct in chiTiets)
            {
                var soLuongTru = ct.SoLuong * ct.HeSo;
                
                var checkSql = "SELECT ISNULL(SoLuongTon, 0) FROM TonKho WHERE NguyenLieuID = @NguyenLieuID";
                using var checkCmd = new SqlCommand(checkSql, conn, transaction);
                checkCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                var currentTon = Convert.ToDecimal(await checkCmd.ExecuteScalarAsync() ?? 0);
                
                if (currentTon < soLuongTru)
                {
                    throw new Exception("Sá»‘ lÆ°á»£ng tá»“n kho khÃ´ng Ä‘á»§ Ä‘á»ƒ trá»«. (Tá»“n kho khÃ´ng Ä‘Æ°á»£c phÃ©p nhá» hÆ¡n 0)");
                }

                var updateSql = @"UPDATE TonKho 
                                 SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                 WHERE NguyenLieuID = @NguyenLieuID";
                using var updateCmd = new SqlCommand(updateSql, conn, transaction);
                updateCmd.Parameters.AddWithValue("@SoLuong", soLuongTru);
                updateCmd.Parameters.AddWithValue("@NguyenLieuID", ct.NguyenLieuID);
                await updateCmd.ExecuteNonQueryAsync();
            }
            
            // Cáº­p nháº­t tráº¡ng thÃ¡i Phiáº¿uXuáº¥t
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
}
