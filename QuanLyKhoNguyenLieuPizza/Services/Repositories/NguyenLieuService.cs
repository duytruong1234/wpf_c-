using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class NguyenLieuService : DatabaseContext
{
    public NguyenLieuService(string connectionString) : base(connectionString) { }

    public async Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync() =>
        await ExecuteQueryListAsync("SELECT LoaiNLID, TenLoai FROM LoaiNguyenLieu",
            r => new LoaiNguyenLieu { LoaiNLID = r.GetInt32(0), TenLoai = r.GetString(1) });

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
    public async Task<List<DonViTinh>> GetDonViTinhsAsync() =>
        await ExecuteQueryListAsync("SELECT DonViID, TenDonVi FROM DonViTinh",
            r => new DonViTinh { DonViID = r.GetInt32(0), TenDonVi = r.GetString(1) });
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

    public async Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId) =>
        await ExecuteCommandAsync("UPDATE NguyenLieu SET TrangThai = 0 WHERE NguyenLieuID = @NguyenLieuID",
            new SqlParameter("@NguyenLieuID", nguyenLieuId));

    public async Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong) =>
        await ExecuteCommandAsync(
            @"UPDATE TonKho SET SoLuongTon = @SoLuongTon, NgayCapNhat = GETDATE() WHERE NguyenLieuID = @NguyenLieuID",
            new SqlParameter("@SoLuongTon", soLuong),
            new SqlParameter("@NguyenLieuID", nguyenLieuId));
}
