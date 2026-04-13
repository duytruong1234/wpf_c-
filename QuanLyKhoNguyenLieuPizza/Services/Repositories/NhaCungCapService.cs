using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class NhaCungCapService : DatabaseContext
{
    public NhaCungCapService(string connectionString) : base(connectionString) { }

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
            
            // Kiá»ƒm tra náº¿u NhÃ CungCáº¥p Ä‘ang Ä‘Æ°á»£c sá»­ dá»¥ng
            var checkSql = @"SELECT COUNT(*) FROM NguyenLieuNhaCungCap WHERE NhaCungCapID = @NhaCungCapID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (count > 0)
            {
                // XÃ³a má»m - Ä‘áº·t Tráº¡ngThÃ¡i = false
                return await UpdateNhaCungCapTrangThaiAsync(nhaCungCapId, false);
            }
            
            // XÃ³a cá»©ng náº¿u khÃ´ng Ä‘Æ°á»£c sá»­ dá»¥ng
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
            
            var sql = @"SELECT COUNT(*) FROM NhaCungCap";
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
}
