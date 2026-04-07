using Microsoft.Data.SqlClient;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.Utilities;

/// <summary>
/// Kiểm tra kết nối đến SQL Server
/// </summary>
public static class DatabaseConnectionTester
{
    public static async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            var connectionString = ConfigurationService.Instance.GetConnectionString();
            
            System.Diagnostics.Debug.WriteLine($"Testing connection with: {connectionString}");
            
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            
            // Kiểm tra truy vấn đơn giản
            using var cmd = new SqlCommand("SELECT @@VERSION", conn);
            var version = await cmd.ExecuteScalarAsync();
            
            return (true, $"Connection successful! SQL Server version: {version?.ToString()?.Split('\n')[0]}");
        }
        catch (SqlException sqlEx)
        {
            var errorMsg = sqlEx.Number switch
            {
                -1 => "Không thể kết nối đến SQL Server. Kiểm tra xem SQL Server có đang chạy không.",
                -2 => "Hết thời gian kết nối. SQL Server có thể đã tắt hoặc không thể truy cập.",
                18456 => "Đăng nhập thất bại. Kiểm tra tên người dùng và mật khẩu.",
                4060 => "Cơ sở dữ liệu không tồn tại. Tạo cơ sở dữ liệu trước.",
                _ => $"SQL Error {sqlEx.Number}: {sqlEx.Message}"
            };
            
            System.Diagnostics.Debug.WriteLine($"SQL Error: {errorMsg}");
            return (false, errorMsg);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            return (false, $"Error: {ex.Message}");
        }
    }
}

