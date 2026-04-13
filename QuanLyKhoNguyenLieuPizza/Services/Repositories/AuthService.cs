using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class AuthService : DatabaseContext
{
    public AuthService(string connectionString) : base(connectionString) { }

    public async Task<TaiKhoan?> AuthenticateAsync(string username, string password)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT tk.TaiKhoanID, tk.NhanVienID, tk.Username, tk.Password, tk.TrangThai,
                              nv.NhanVienID, nv.HoTen, nv.HinhAnh, nv.NgaySinh, nv.DiaChi, nv.SDT, nv.Email, nv.ChucVuID, nv.TrangThai,
                              cv.TenChucVu
                       FROM TaiKhoan tk
                       LEFT JOIN NhanVien nv ON tk.NhanVienID = nv.NhanVienID
                       LEFT JOIN ChucVu cv ON nv.ChucVuID = cv.ChucVuID
                       WHERE tk.Username = @Username AND tk.Password = @Password";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var tkTrangThai = reader.GetBoolean(4);
                if (!tkTrangThai)
                {
                    throw new Exception("TÃ i khoáº£n nÃ y Ä‘Ã£ bá»‹ khÃ³a!");
                }

                if (!reader.IsDBNull(13))
                {
                    var nvTrangThai = reader.GetBoolean(13);
                    if (!nvTrangThai)
                    {
                        throw new Exception("TÃ i khoáº£n cá»§a nhÃ¢n viÃªn Ä‘Ã£ nghá»‰ viá»‡c khÃ´ng Ä‘Æ°á»£c phÃ©p truy cáº­p vÃ o há»‡ thá»‘ng!");
                    }
                }

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
                
                return taiKhoan;
            }
        }
        catch (SqlException sqlEx)
        {
            throw new Exception($"Lá»—i káº¿t ná»‘i database: {sqlEx.Message}", sqlEx);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("TÃ i khoáº£n")) throw;
            throw new Exception($"Lá»—i xÃ¡c thá»±c: {ex.Message}", ex);
        }
        
        return null;
    }

    public async Task<List<ChucVu>> GetChucVusAsync() =>
        await ExecuteQueryListAsync("SELECT ChucVuID, TenChucVu FROM ChucVu",
            r => new ChucVu { ChucVuID = r.GetInt32(0), TenChucVu = r.GetString(1) });

    public async Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // XÃ¡c thá»±c ná»›i lá»ng: TÃ¬m ngÆ°á»i dÃ¹ng theo Email trÆ°á»›c (Ä‘á»™c nháº¥t), sau Ä‘Ã³ xÃ¡c minh thÃ´ng tin khÃ¡c trong bá»™ nhá»›
            // Äiá»u nÃ y xá»­ lÃ½ cÃ¡c váº¥n Ä‘á» vá» phÃ¢n biá»‡t chá»¯ hoa/thÆ°á»ng, khoáº£ng tráº¯ng, Ä‘á»‹nh dáº¡ng ngÃ y, v.v.
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

                // XÃ¡c thá»±c tÃªn (KhÃ´ng phÃ¢n biá»‡t hoa/thÆ°á»ng, bá» Táº¤T Cáº¢ khoáº£ng tráº¯ng, bá» dáº¥u)
                string Normalize(string s)
                {
                    if (string.IsNullOrEmpty(s)) return string.Empty;
                    
                    // Bá» dáº¥u
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
                    
                    // Bá» khoáº£ng tráº¯ng vÃ  chuyá»ƒn chá»¯ thÆ°á»ng
                    return withoutAccents.Replace(" ", "").ToLower();
                }
                
                if (Normalize(dbHoTen) != Normalize(hoTen))
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: Name mismatch. DB='{dbHoTen}', Input='{hoTen}'");
                    System.Diagnostics.Debug.WriteLine($"Normalized: DB='{Normalize(dbHoTen)}', Input='{Normalize(hoTen)}'");
                    return null;
                }

                // XÃ¡c thá»±c ngÃ y sinh (Chá»‰ pháº§n ngÃ y)
                if (dbNgaySinh?.Date != ngaySinh.Date)
                {
                    System.Diagnostics.Debug.WriteLine($"Verify Failed: DoB mismatch. DB='{dbNgaySinh:d}', Input='{ngaySinh:d}'");
                    return null;
                }

                // XÃ¡c thá»±c chá»©c vá»¥
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
        return null; // KhÃ´ng tÃ¬m tháº¥y
    }

    public async Task<bool> ChangePasswordAsync(string email, string newPassword)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Cáº­p nháº­t máº­t kháº©u cho ngÆ°á»i dÃ¹ng liÃªn káº¿t vá»›i email nÃ y
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
}
