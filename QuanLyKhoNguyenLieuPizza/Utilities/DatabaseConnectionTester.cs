using Microsoft.Data.SqlClient;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.Utilities;

/// <summary>
/// Test connection to SQL Server
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
            
            // Test simple query
            using var cmd = new SqlCommand("SELECT @@VERSION", conn);
            var version = await cmd.ExecuteScalarAsync();
            
            return (true, $"Connection successful! SQL Server version: {version?.ToString()?.Split('\n')[0]}");
        }
        catch (SqlException sqlEx)
        {
            var errorMsg = sqlEx.Number switch
            {
                -1 => "Cannot connect to SQL Server. Check if SQL Server is running.",
                -2 => "Connection timeout. SQL Server may be down or unreachable.",
                18456 => "Login failed. Check username and password.",
                4060 => "Database does not exist. Create the database first.",
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
