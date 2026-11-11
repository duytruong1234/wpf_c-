using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Kiểm tra và tự động tạo Database từ file .sql khi khách chạy app lần đầu.
/// Nếu SQL Server chưa được cài đặt, hướng dẫn người dùng tải và cài đặt.
/// </summary>
public static class DatabaseInitializer
{
    private const string SQL_SERVER_DOWNLOAD_URL =
        "https://go.microsoft.com/fwlink/p/?linkid=2216019";

    /// <summary>
    /// Kiểm tra nếu DB chưa tồn tại thì tự động tạo từ file script .sql
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync()
    {
        var config = ConfigurationService.Instance;
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Debug.WriteLine("DatabaseInitializer: Connection string rỗng, bỏ qua.");
            return;
        }

        // Parse tên database từ connection string
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrEmpty(databaseName))
        {
            Debug.WriteLine("DatabaseInitializer: Không tìm thấy tên database trong connection string.");
            return;
        }

        // Kết nối tới master để kiểm tra DB có tồn tại hay không
        builder.InitialCatalog = "master";
        builder.ConnectTimeout = 5; // Timeout nhanh để phát hiện SQL Server chưa cài
        var masterConnectionString = builder.ConnectionString;

        try
        {
            using var conn = new SqlConnection(masterConnectionString);
            await conn.OpenAsync();

            // SQL Server đang chạy — Kiểm tra DB đã tồn tại chưa
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName", conn);
            checkCmd.Parameters.AddWithValue("@dbName", databaseName);

            var exists = (int)(await checkCmd.ExecuteScalarAsync())! > 0;

            if (exists)
            {
                Debug.WriteLine($"DatabaseInitializer: Database '{databaseName}' đã tồn tại.");
                return;
            }

            // DB chưa tồn tại → Tạo từ file .sql
            Debug.WriteLine($"DatabaseInitializer: Database '{databaseName}' chưa tồn tại. Đang tạo...");
            await CreateDatabaseFromScript(conn, builder, databaseName);
        }
        catch (SqlException ex) when (ex.Number == -1 || ex.Number == 2 || ex.Number == -2)
        {
            // Không kết nối được SQL Server → Hướng dẫn cài đặt
            HandleSqlServerNotFound(ex);
        }
        catch (Exception ex) {
            Debug.WriteLine($"DatabaseInitializer: Lỗi: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Lỗi khi khởi tạo cơ sở dữ liệu:\n{ex.Message}",
                "Lỗi",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xử lý khi không tìm thấy SQL Server — hướng dẫn tải và cài đặt
    /// </summary>
    private static void HandleSqlServerNotFound(SqlException ex)
    {
        Debug.WriteLine($"DatabaseInitializer: SQL Server không khả dụng - {ex.Message}");

        var result = System.Windows.MessageBox.Show(
            "Không tìm thấy SQL Server trên máy tính!\n\n" +
            "Ứng dụng cần SQL Server để lưu trữ dữ liệu.\n" +
            "Vui lòng cài đặt SQL Server Express (miễn phí) trước khi sử dụng.\n\n" +
            "Hướng dẫn:\n" +
            "1. Nhấn [Yes] để mở trang tải SQL Server Express\n" +
            "2. Tải và cài đặt SQL Server Express\n" +
            "3. Khởi động lại ứng dụng\n\n" +
            "Nhấn [No] để tiếp tục mà không có cơ sở dữ liệu.\n" +
            "(Ứng dụng sẽ không hoạt động đầy đủ)",
            "Cần cài đặt SQL Server",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                // Mở trình duyệt tải SQL Server Express
                Process.Start(new ProcessStartInfo
                {
                    FileName = SQL_SERVER_DOWNLOAD_URL,
                    UseShellExecute = true
                });

                System.Windows.MessageBox.Show(
                    "Trình duyệt đã được mở để tải SQL Server Express.\n\n" +
                    "Sau khi cài đặt xong SQL Server:\n" +
                    "1. Đảm bảo SQL Server service đang chạy\n" +
                    "2. Khởi động lại ứng dụng\n" +
                    "3. Database sẽ được tạo tự động",
                    "Hướng dẫn cài đặt",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch
            {
                System.Windows.MessageBox.Show(
                    "Không thể mở trình duyệt tự động.\n\n" +
                    "Vui lòng tải SQL Server Express tại:\n" +
                    SQL_SERVER_DOWNLOAD_URL,
                    "Tải SQL Server",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
    }

    /// <summary>
    /// Tạo database từ file script .sql
    /// </summary>
    private static async Task CreateDatabaseFromScript(
        SqlConnection conn, SqlConnectionStringBuilder builder, string databaseName)
    {
        var sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Resources", "Database", "QLKNL_PIZZA.sql");

        if (!File.Exists(sqlFilePath))
        {
            Debug.WriteLine($"DatabaseInitializer: Không tìm thấy file script: {sqlFilePath}");

            System.Windows.MessageBox.Show(
                $"Không tìm thấy file khởi tạo cơ sở dữ liệu:\n{sqlFilePath}\n\n" +
                "Vui lòng đặt file QLKNL_PIZZA.sql vào thư mục Resources\\Database\\ cùng với file .exe",
                "Lỗi Cơ sở dữ liệu",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        // Đọc file SQL (hỗ trợ cả UTF-8 và UTF-16)
        string sqlScript;
        var rawBytes = await File.ReadAllBytesAsync(sqlFilePath);

        // Kiểm tra BOM UTF-16 LE
        if (rawBytes.Length >= 2 && rawBytes[0] == 0xFF && rawBytes[1] == 0xFE)
        {
            sqlScript = System.Text.Encoding.Unicode.GetString(rawBytes);
        }
        else
        {
            sqlScript = System.Text.Encoding.UTF8.GetString(rawBytes);
        }

        // Xử lý script: bỏ phần CREATE DATABASE vì file path khác nhau mỗi máy
        // Thay vào đó ta tạo DB đơn giản rồi chạy phần còn lại
        sqlScript = RemoveCreateDatabaseSection(sqlScript, databaseName);

        // Tạo database đơn giản (không chỉ định đường dẫn cứng)
        var createDbCmd = new SqlCommand(
            $"CREATE DATABASE [{databaseName}]", conn);
        await createDbCmd.ExecuteNonQueryAsync();

        Debug.WriteLine($"DatabaseInitializer: Đã tạo Database '{databaseName}'.");

        // Đóng kết nối master, mở kết nối mới tới DB vừa tạo
        conn.Close();

        // Chờ SQL Server kích hoạt DB
        await Task.Delay(1000);

        // Kết nối tới DB mới tạo và chạy phần còn lại của script
        builder.InitialCatalog = databaseName;
        builder.ConnectTimeout = 30; // Timeout dài hơn cho import dữ liệu
        using var dbConn = new SqlConnection(builder.ConnectionString);
        await dbConn.OpenAsync();

        // Tách script theo lệnh GO
        var batches = SplitByGo(sqlScript);
        int successCount = 0;
        int errorCount = 0;

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Bỏ qua các lệnh ALTER DATABASE, USE [master], sp_fulltext_database
            if (trimmed.StartsWith("ALTER DATABASE", StringComparison.OrdinalIgnoreCase)) continue;
            if (trimmed.StartsWith("USE [", StringComparison.OrdinalIgnoreCase)) continue;
            if (trimmed.Contains("sp_fulltext_database", StringComparison.OrdinalIgnoreCase)) continue;
            if (trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)) continue;
            if (trimmed.StartsWith("IF (1 = FULLTEXTSERVICEPROPERTY", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                using var cmd = new SqlCommand(trimmed, dbConn);
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync();
                successCount++;
            }
            catch (SqlException ex)
            {
                errorCount++;
                // Ghi log nhưng tiếp tục (có thể một số lệnh không tương thích)
                Debug.WriteLine($"DatabaseInitializer: Lỗi SQL batch: {ex.Message}");
                Debug.WriteLine($"  Batch: {trimmed[..Math.Min(200, trimmed.Length)]}...");
            }
        }

        Debug.WriteLine($"DatabaseInitializer: Tạo database '{databaseName}' thành công! ({successCount} thành công, {errorCount} lỗi)");

        System.Windows.MessageBox.Show(
            $"Đã tạo cơ sở dữ liệu '{databaseName}' thành công!\n\n" +
            $"• {successCount} lệnh SQL đã thực thi\n" +
            "• Dữ liệu khởi tạo đã được nạp từ SQL script\n\n" +
            "Ứng dụng sẽ tiếp tục khởi động.",
            "Khởi tạo Database thành công",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// Xóa phần CREATE DATABASE khỏi script (vì đường dẫn file cứng không phù hợp trên máy khác)
    /// </summary>
    private static string RemoveCreateDatabaseSection(string script, string dbName)
    {
        // Xóa từ "CREATE DATABASE" đến "GO" đầu tiên sau nó
        var pattern = @"CREATE\s+DATABASE\s+\[" + Regex.Escape(dbName) + @"\].*?^GO\s*$";
        script = Regex.Replace(script, pattern,
            "", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Xóa USE [master]
        script = Regex.Replace(script, @"^USE\s+\[master\]\s*$", "",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return script;
    }

    /// <summary>
    /// Tách script SQL theo lệnh GO (chuẩn SQL Server batch separator)
    /// </summary>
    private static List<string> SplitByGo(string script)
    {
        // Tách theo dòng chỉ chứa "GO" (không phân biệt hoa thường)
        var batches = Regex.Split(script, @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return batches.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
    }
}
