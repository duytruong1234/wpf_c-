using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class NhanVienService : DatabaseContext
{
    public NhanVienService(string connectionString) : base(connectionString) { }

    public async Task<List<ChucVu>> GetAllChucVusAsync()
    {
        var result = new List<ChucVu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ChucVuID, TenChucVu FROM ChucVu ORDER BY TenChucVu";
            
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
            
            // Kiá»ƒm tra náº¿u cÃ³ NhÃ¢nViÃªn nÃ o Ä‘ang sá»­ dá»¥ng Chá»©cVá»¥ nÃ y
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
            // XÃ³a TÃ iKhoáº£n trÆ°á»›c
            var deleteAccountSql = "DELETE FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var deleteAccountCmd = new SqlCommand(deleteAccountSql, conn, transaction);
            deleteAccountCmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            await deleteAccountCmd.ExecuteNonQueryAsync();
            
            // Sau Ä‘Ã³ xÃ³a NhÃ¢nViÃªn
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
            var sql = @"SELECT TaiKhoanID, NhanVienID, Username, Password, TrangThai FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
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
            
            var sql = @"SELECT COUNT(*) FROM NhanVien";
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
}
