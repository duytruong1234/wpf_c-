using System.Text;
using System.Globalization;
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
        // Load from configuration instead of hardcoding
        _connectionString = ConfigurationService.Instance.GetConnectionString();
    }

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqlConnection GetConnection() => new SqlConnection(_connectionString);

    #region Authentication
    public async Task<TaiKhoan?> AuthenticateAsync(string username, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Authentication Started ===");
            System.Diagnostics.Debug.WriteLine($"Username: {username}");
            System.Diagnostics.Debug.WriteLine($"Connection String: {_connectionString}");
            
            using var conn = GetConnection();
            
            System.Diagnostics.Debug.WriteLine("Attempting to open connection...");
            await conn.OpenAsync();
            System.Diagnostics.Debug.WriteLine("Connection opened successfully!");
            
            var sql = @"SELECT tk.TaiKhoanID, tk.NhanVienID, tk.Username, tk.Password, tk.TrangThai,
                              nv.NhanVienID, nv.HoTen, nv.HinhAnh, nv.NgaySinh, nv.DiaChi, nv.SDT, nv.Email, nv.ChucVuID, nv.TrangThai,
                              cv.TenChucVu
                       FROM TaiKhoan tk
                       LEFT JOIN NhanVien nv ON tk.NhanVienID = nv.NhanVienID
                       LEFT JOIN ChucVu cv ON nv.ChucVuID = cv.ChucVuID
                       WHERE tk.Username = @Username AND tk.Password = @Password AND tk.TrangThai = 1";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            
            System.Diagnostics.Debug.WriteLine("Executing query...");
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                System.Diagnostics.Debug.WriteLine("User found in database!");
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
                
                System.Diagnostics.Debug.WriteLine($"Login successful for user: {taiKhoan.Username}");
                return taiKhoan;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No user found with provided credentials");
                
                // Check if user exists at all
                var checkUserSql = "SELECT COUNT(*) FROM TaiKhoan WHERE Username = @Username";
                using var checkCmd = new SqlCommand(checkUserSql, conn);
                // Connection is still open from previous reader, need to close reader first
                reader.Close();
                checkCmd.Parameters.AddWithValue("@Username", username);
                var userCount = (int)await checkCmd.ExecuteScalarAsync();
                
                System.Diagnostics.Debug.WriteLine($"Users with username '{username}': {userCount}");
                
                if (userCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine("User exists but password or TrangThai is incorrect");
                }
            }
        }
        catch (SqlException sqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"=== SQL ERROR ===");
            System.Diagnostics.Debug.WriteLine($"Error Number: {sqlEx.Number}");
            System.Diagnostics.Debug.WriteLine($"Error Message: {sqlEx.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {sqlEx.StackTrace}");
            throw new Exception($"Lỗi kết nối database: {sqlEx.Message}", sqlEx);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== GENERAL ERROR ===");
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw new Exception($"Lỗi xác thực: {ex.Message}", ex);
        }
        
        return null;
    }
    #endregion

    #region User Management
    public async Task<List<ChucVu>> GetChucVusAsync()
    {
        var result = new List<ChucVu>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT ChucVuID, TenChucVu FROM ChucVu", conn);
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
            System.Diagnostics.Debug.WriteLine($"Error getting ChucVus: {ex.Message}");
        }
        return result;
    }

    public async Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Relaxed verification: Find user by Email first (unique), then verify other info in memory
            // This handles issues with SQL case sensitivity, spacing, date formats, etc.
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

                // Verify Name (Case insensitive, ignore ALL spaces, remove accents)
                string Normalize(string s)
                {
                    if (string.IsNullOrEmpty(s)) return string.Empty;
                    
                    // Remove accents
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
                    
                    // Remove spaces and lowercase
                    return withoutAccents.Replace(" ", "").ToLower();
                }
                
                if (Normalize(dbHoTen) != Normalize(hoTen))
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: Name mismatch. DB='{dbHoTen}', Input='{hoTen}'");
                    System.Diagnostics.Debug.WriteLine($"Normalized: DB='{Normalize(dbHoTen)}', Input='{Normalize(hoTen)}'");
                    return null;
                }

                // Verify Date of Birth (Date part only)
                if (dbNgaySinh?.Date != ngaySinh.Date)
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: DoB mismatch. DB='{dbNgaySinh:d}', Input='{ngaySinh:d}'");
                    return null;
                }

                // Verify Role
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
        return null; // Not found
    }

    public async Task<bool> ChangePasswordAsync(string email, string newPassword)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Update password for the user linked to this email
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

    #region Loai Nguyen Lieu
    public async Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync()
    {
        var result = new List<LoaiNguyenLieu>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand("SELECT LoaiNLID, TenLoai FROM LoaiNguyenLieu", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new LoaiNguyenLieu
                {
                    LoaiNLID = reader.GetInt32(0),
                    TenLoai = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading LoaiNguyenLieu: {ex.Message}");
            // Don't throw - return empty list for graceful degradation
        }
        
        return result;
    }

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

    #region Nha Cung Cap
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
    #endregion

    #region Nguyen Lieu
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

    #region Ton Kho
    public async Task<List<TonKho>> GetTonKhosAsync()
    {
        var result = new List<TonKho>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT tk.TonKhoID, tk.NguyenLieuID, tk.SoLuongTon, tk.NgayCapNhat,
                              nl.TenNguyenLieu, nl.HinhAnh, dv.TenDonVi
                       FROM TonKho tk
                       INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
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
                        DonViTinh = reader.IsDBNull(6) ? null : new DonViTinh { TenDonVi = reader.GetString(6) }
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

    #region Don Vi Tinh
    public async Task<List<DonViTinh>> GetDonViTinhsAsync()
    {
        var result = new List<DonViTinh>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand("SELECT DonViID, TenDonVi FROM DonViTinh", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(new DonViTinh
                {
                    DonViID = reader.GetInt32(0),
                    TenDonVi = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading DonViTinh: {ex.Message}");
        }
        
        return result;
    }
    #endregion

    #region Quy Doi Don Vi
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

    #region Additional Methods
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

    public async Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Soft delete by setting TrangThai = 0
            var sql = "UPDATE NguyenLieu SET TrangThai = 0 WHERE NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting NguyenLieu: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"UPDATE TonKho SET SoLuongTon = @SoLuongTon, NgayCapNhat = GETDATE() 
                       WHERE NguyenLieuID = @NguyenLieuID";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SoLuongTon", soLuong);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating TonKho: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Statistics for Dashboard
    public async Task<int> GetTotalTonKhoCountAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "SELECT COUNT(*) FROM TonKho WHERE SoLuongTon > 0";
            using var cmd = new SqlCommand(sql, conn);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting TonKho count: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetLowStockCountAsync(decimal threshold = 20)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = "SELECT COUNT(*) FROM TonKho WHERE SoLuongTon > 0 AND SoLuongTon < @Threshold";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Threshold", threshold);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting low stock count: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetNearExpiryCountAsync(int days = 7)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Assuming there's a HanSuDung column or similar in NguyenLieu/TonKho
            // Adjust this query based on your actual schema
            var sql = @"SELECT COUNT(*) FROM TonKho tk
                       INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                       WHERE tk.SoLuongTon > 0 
                       AND nl.HanSuDung IS NOT NULL 
                       AND nl.HanSuDung <= DATEADD(DAY, @Days, GETDATE())
                       AND nl.HanSuDung > GETDATE()";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", days);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting near expiry count: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetExpiredCountAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Adjust based on your actual schema
            var sql = @"SELECT COUNT(*) FROM TonKho tk
                       INNER JOIN NguyenLieu nl ON tk.NguyenLieuID = nl.NguyenLieuID
                       WHERE tk.SoLuongTon > 0 
                       AND nl.HanSuDung IS NOT NULL 
                       AND nl.HanSuDung <= GETDATE()";
            using var cmd = new SqlCommand(sql, conn);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting expired count: {ex.Message}");
            return 0;
        }
    }
    #endregion

    #region Phieu Nhap
    public async Task<List<PhieuNhap>> GetPhieuNhapsAsync(
        string? searchText = null,
        int? nhanVienId = null,
        int? nhaCungCapId = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null)
    {
        var result = new List<PhieuNhap>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT pn.PhieuNhapID, pn.MaPhieuNhap, pn.NhanVienNhapID, pn.NhaCungCapID,
                              pn.NgayNhap, pn.TongTien, pn.TrangThai,
                              nv.HoTen as TenNhanVien, ncc.TenNCC
                       FROM PhieuNhap pn
                       LEFT JOIN NhanVien nv ON pn.NhanVienNhapID = nv.NhanVienID
                       LEFT JOIN NhaCungCap ncc ON pn.NhaCungCapID = ncc.NhaCungCapID
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
                    TrangThai = reader.GetByte(6)
                };
                
                if (!reader.IsDBNull(7))
                {
                    phieuNhap.NhanVienNhap = new NhanVien { HoTen = reader.GetString(7) };
                }
                
                if (!reader.IsDBNull(8))
                {
                    phieuNhap.NhaCungCap = new NhaCungCap { TenNCC = reader.GetString(8) };
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
                              nv.HoTen as TenNhanVien, ncc.TenNCC, ncc.DiaChi
                       FROM PhieuNhap pn
                       LEFT JOIN NhanVien nv ON pn.NhanVienNhapID = nv.NhanVienID
                       LEFT JOIN NhaCungCap ncc ON pn.NhaCungCapID = ncc.NhaCungCapID
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
                    TrangThai = reader.GetByte(6)
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
                              dv.TenDonVi, nlncc.GiaNhap
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
                    nl.NguyenLieuNhaCungCaps = new List<NguyenLieuNhaCungCap>
                    {
                        new NguyenLieuNhaCungCap { GiaNhap = reader.GetDecimal(8) }
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
                // Insert new PhieuNhap
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
                // Update existing PhieuNhap
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
                
                // Delete existing chi tiets
                var deleteSql = "DELETE FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
                await deleteCmd.ExecuteNonQueryAsync();
            }
            
            // Insert chi tiets
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
                
                // Update TonKho
                await UpdateTonKhoOnNhapAsync(conn, transaction, ct.NguyenLieuID!.Value, ct.SoLuong * ct.HeSo);
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

    private async Task UpdateTonKhoOnNhapAsync(SqlConnection conn, SqlTransaction transaction, int nguyenLieuId, decimal soLuong)
    {
        // Check if TonKho exists
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
            // Get chi tiets to reverse TonKho
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
            
            // Reverse TonKho
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
            
            // Delete chi tiets
            var deleteCtSql = "DELETE FROM CT_PhieuNhap WHERE PhieuNhapID = @PhieuNhapID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@PhieuNhapID", phieuNhapId);
            await deleteCtCmd.ExecuteNonQueryAsync();
            
            // Delete PhieuNhap
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

    #region Phieu Xuat
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
                // Insert new PhieuXuat
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
                // Update existing PhieuXuat
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
                
                // Delete existing chi tiets
                var deleteSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
                using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                deleteCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
                await deleteCmd.ExecuteNonQueryAsync();
            }
            
            // Insert chi tiets
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
            // Get chi tiets to update TonKho
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
            
            // Update TonKho (subtract)
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
            
            // Update PhieuXuat status
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
            // Delete chi tiets
            var deleteCtSql = "DELETE FROM CT_PhieuXuat WHERE PhieuXuatID = @PhieuXuatID";
            using var deleteCtCmd = new SqlCommand(deleteCtSql, conn, transaction);
            deleteCtCmd.Parameters.AddWithValue("@PhieuXuatID", phieuXuatId);
            await deleteCtCmd.ExecuteNonQueryAsync();
            
            // Delete PhieuXuat
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

    #region Nha Cung Cap Management
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
            
            // Check if NhaCungCap is being used
            var checkSql = @"SELECT COUNT(*) FROM NguyenLieuNhaCungCap WHERE NhaCungCapID = @NhaCungCapID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@NhaCungCapID", nhaCungCapId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (count > 0)
            {
                // Soft delete - set TrangThai = false
                return await UpdateNhaCungCapTrangThaiAsync(nhaCungCapId, false);
            }
            
            // Hard delete if not being used
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
    #endregion

    #region Chuc Vu Management
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
            
            // Check if any NhanVien is using this ChucVu
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

    #region Nhan Vien Management
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
            // Delete TaiKhoan first
            var deleteAccountSql = "DELETE FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var deleteAccountCmd = new SqlCommand(deleteAccountSql, conn, transaction);
            deleteAccountCmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            await deleteAccountCmd.ExecuteNonQueryAsync();
            
            // Then delete NhanVien
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

    public async Task<bool> CreateTaiKhoanForNhanVienAsync(int nhanVienId, string username, string password)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Check if account already exists
            var checkSql = "SELECT COUNT(*) FROM TaiKhoan WHERE NhanVienID = @NhanVienID";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@NhanVienID", nhanVienId);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (count > 0)
            {
                return false; // Account already exists
            }
            
            var sql = @"INSERT INTO TaiKhoan (NhanVienID, Username, Password, TrangThai)
                       VALUES (@NhanVienID, @Username, @Password, 1)";
            
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
                               ISNULL(hh.TinhTrang, 1) AS TinhTrang
                        FROM HangHoa hh
                        INNER JOIN GiaTheo_Size gts ON hh.MaHangHoa = gts.MaHangHoa
                        LEFT JOIN DoanhMuc_Size ds ON gts.SizeID = ds.SizeID";
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
                    TrangThai = !reader.IsDBNull(6) && Convert.ToBoolean(reader.GetValue(6))
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
        // Not used with HangHoa-based schema (no Pizza table)
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

            // Check if HangHoa already exists
            using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM HangHoa WHERE MaHangHoa = @Ma", conn);
            checkCmd.Parameters.AddWithValue("@Ma", pizza.MaPizza);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (exists)
            {
                // UPDATE HangHoa
                using var cmdHH = new SqlCommand(
                    @"UPDATE HangHoa SET TenHangHoa=@Ten, HinhAnh=@Anh, TinhTrang=@TT WHERE MaHangHoa=@Ma", conn);
                cmdHH.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                cmdHH.Parameters.AddWithValue("@Ten", pizza.TenPizza);
                cmdHH.Parameters.AddWithValue("@Anh", (object?)pizza.HinhAnh ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@TT", pizza.TrangThai);
                await cmdHH.ExecuteNonQueryAsync();
            }
            else
            {
                // INSERT HangHoa
                using var cmdHH = new SqlCommand(
                    @"INSERT INTO HangHoa (MaHangHoa, TenHangHoa, HinhAnh, TinhTrang) VALUES (@Ma, @Ten, @Anh, @TT)", conn);
                cmdHH.Parameters.AddWithValue("@Ma", pizza.MaPizza);
                cmdHH.Parameters.AddWithValue("@Ten", pizza.TenPizza);
                cmdHH.Parameters.AddWithValue("@Anh", (object?)pizza.HinhAnh ?? DBNull.Value);
                cmdHH.Parameters.AddWithValue("@TT", pizza.TrangThai);
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
        // Not used with HangHoa-based schema
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
    #endregion

    #region CongThuc (Recipe)
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
        // Not used with HangHoa-based schema
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

    #region DonHang (Order)
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
                                nv.HoTen
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
                    PhuongThucTT = "Tiền mặt",
                    TrangThai = 2, // Hoàn thành
                    GhiChu = "Bán hàng tại quầy"
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

        // Sort combined results by date descending
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
                               p.TenPizza, p.KichThuoc
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
                        KichThuoc = reader.IsDBNull(7) ? "M" : reader.GetString(7)
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
                // Insert DonHang
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

                // Insert CT_DonHang
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

                // Deduct ingredients from TonKho based on CongThuc
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
    #endregion

    #region Sales Statistics
    public async Task<decimal> GetDoanhThuAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT ISNULL(SUM(ThanhToan), 0) FROM (
                           SELECT ThanhToan FROM DonHang WHERE TrangThai = 2 AND NgayTao >= @FromDate AND NgayTao <= @ToDate
                           UNION ALL
                           SELECT ISNULL(TongTien, 0) AS ThanhToan FROM PhieuBanHang 
                           WHERE NgayBan >= @FromDate AND NgayBan <= @ToDate
                           AND MaPhieuBan NOT IN (SELECT MaDonHang FROM DonHang WHERE MaDonHang IS NOT NULL)
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
            var sql = @"SELECT COUNT(*) FROM (
                           SELECT DonHangID FROM DonHang WHERE TrangThai = 2 AND NgayTao >= @FromDate AND NgayTao <= @ToDate
                           UNION ALL
                           SELECT 0 FROM PhieuBanHang 
                           WHERE NgayBan >= @FromDate AND NgayBan <= @ToDate
                           AND MaPhieuBan NOT IN (SELECT MaDonHang FROM DonHang WHERE MaDonHang IS NOT NULL)
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
            using var conn = GetConnection();
            await conn.OpenAsync();
            // Lợi nhuận = Doanh thu bán hàng - Chi phí nhập nguyên liệu trong kỳ
            var sql = @"SELECT 
                            ISNULL((SELECT SUM(ThanhToan) FROM DonHang WHERE TrangThai = 2 AND NgayTao >= @FromDate AND NgayTao <= @ToDate), 0)
                          - ISNULL((SELECT SUM(TongTien) FROM PhieuNhap WHERE TrangThai = 2 AND NgayNhap >= @FromDate AND NgayNhap <= @ToDate), 0)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
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
            // Chi phí = tổng tiền nhập kho nguyên liệu thực tế trong kỳ
            var sql = @"SELECT ISNULL(SUM(TongTien), 0) FROM PhieuNhap 
                        WHERE TrangThai = 2 AND NgayNhap >= @FromDate AND NgayNhap <= @ToDate";
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
            var sql = @"SELECT TOP (@Top) p.TenPizza, p.KichThuoc, SUM(ctdh.SoLuong) AS SoLuongBan, SUM(ctdh.ThanhTien) AS DoanhThu
                       FROM CT_DonHang ctdh
                       INNER JOIN DonHang dh ON ctdh.DonHangID = dh.DonHangID
                       INNER JOIN Pizza p ON ctdh.PizzaID = p.PizzaID
                       WHERE dh.TrangThai = 2 AND dh.NgayTao >= @FromDate AND dh.NgayTao <= @ToDate
                       GROUP BY p.TenPizza, p.KichThuoc
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
                donHangs.Add(new DonHang
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
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting RecentDonHangs: {ex.Message}");
        }
        return donHangs;
    }
    #endregion

    #region HangHoa (Product)
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

    #region PhieuBanHang (Sales Receipt)
    public async Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new List<PhieuBanHang>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT pb.MaPhieuBan, pb.NhanVienBanID, pb.NgayBan, pb.TongTien,
                              nv.HoTen, pb.GhiChu
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
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5)
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
                              nv.HoTen, pb.GhiChu
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
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5)
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

    public async Task<string> SavePhieuBanHangAsync(PhieuBanHang phieuBan, List<CT_PhieuBan> chiTiets)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();
        try
        {
            // Insert PhieuBanHang
            var sql = @"INSERT INTO PhieuBanHang (MaPhieuBan, NhanVienBanID, NgayBan, TongTien, GhiChu)
                       VALUES (@MaPhieuBan, @NhanVienBanID, @NgayBan, @TongTien, @GhiChu)";
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@MaPhieuBan", phieuBan.MaPhieuBan);
            cmd.Parameters.AddWithValue("@NhanVienBanID", phieuBan.NhanVienBanID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NgayBan", phieuBan.NgayBan ?? (object)DateTime.Now);
            cmd.Parameters.AddWithValue("@TongTien", phieuBan.TongTien ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@GhiChu", phieuBan.GhiChu ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

            // Insert CT_PhieuBan
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

            // Deduct ingredients from TonKho based on CongThuc_Pizza
            foreach (var ct in chiTiets)
            {
                if (ct.MaHangHoa == null || ct.SizeID == null) continue;
                var congThucs = await GetCongThucPizzaInternalAsync(conn, transaction, ct.MaHangHoa, ct.SizeID);
                foreach (var recipe in congThucs)
                {
                    var deductAmount = (decimal)(recipe.SoLuong ?? 0) * (ct.SoLuong ?? 0);
                    if (deductAmount <= 0) continue;
                    var deductSql = @"UPDATE TonKho SET SoLuongTon = SoLuongTon - @SoLuong, NgayCapNhat = GETDATE()
                                     WHERE NguyenLieuID = @NguyenLieuID AND SoLuongTon >= @SoLuong";
                    using var deductCmd = new SqlCommand(deductSql, conn, transaction);
                    deductCmd.Parameters.AddWithValue("@SoLuong", deductAmount);
                    deductCmd.Parameters.AddWithValue("@NguyenLieuID", recipe.NguyenLieuID);
                    await deductCmd.ExecuteNonQueryAsync();
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
    #endregion

    #region Sales Statistics (PhieuBanHang)
    public async Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "SELECT ISNULL(SUM(TongTien), 0) FROM PhieuBanHang WHERE NgayBan >= @FromDate AND NgayBan <= @ToDate";
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
            var sql = "SELECT COUNT(*) FROM PhieuBanHang WHERE NgayBan >= @FromDate AND NgayBan <= @ToDate";
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
