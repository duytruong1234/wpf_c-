using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class TonKhoService : DatabaseContext
{
    public TonKhoService(string connectionString) : base(connectionString) { }

    public async Task<List<TonKho>> GetTonKhosAsync()
    {
        var result = new List<TonKho>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            var sql = @"SELECT ISNULL(tk.TonKhoID, 0), nl.NguyenLieuID, 
                              ISNULL(tk.SoLuongTon, 0), ISNULL(tk.NgayCapNhat, GETDATE()),
                              nl.TenNguyenLieu, nl.HinhAnh, dv.TenDonVi, nl.MaNguyenLieu
                       FROM NguyenLieu nl
                       LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                       WHERE nl.TrangThai = 1
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
                        DonViTinh = reader.IsDBNull(6) ? null : new DonViTinh { TenDonVi = reader.GetString(6) },
                        MaNguyenLieu = reader.IsDBNull(7) ? null : reader.GetString(7)
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
}
