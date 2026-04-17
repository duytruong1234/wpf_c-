using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Kiểm tra và tự động tạo Database từ file .sql khi khách chạy app lần đầu.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Kiểm tra nếu DB chưa tồn tại thì tự động tạo từ file script .sql
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync()
    {
        var config = ConfigurationService.Instance;
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Connection string rỗng, bỏ qua.");
            return;
        }

        // Parse tên database từ connection string
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrEmpty(databaseName))
        {
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Không tìm thấy tên database trong connection string.");
            return;
        }

        // Kết nối tới master để kiểm tra DB có tồn tại hay không
        builder.InitialCatalog = "master";
        var masterConnectionString = builder.ConnectionString;

        try
        {
            using var conn = new SqlConnection(masterConnectionString);
            await conn.OpenAsync();

            // Kiểm tra DB đã tồn tại chưa
            var checkCmd = new SqlCommand(
                $"SELECT COUNT(*) FROM sys.databases WHERE name = @dbName", conn);
            checkCmd.Parameters.AddWithValue("@dbName", databaseName);

            var exists = (int)(await checkCmd.ExecuteScalarAsync())! > 0;

            if (exists)
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Database '{databaseName}' đã tồn tại.");
                return;
            }

            // DB chưa tồn tại → Chạy script .sql để tạo
            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Database '{databaseName}' chưa tồn tại. Đang tạo...");

            var sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Database", "QuanLyKhoNguyenLieuPizza.sql");

            if (!File.Exists(sqlFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Không tìm thấy file script: {sqlFilePath}");

                // Thông báo cho người dùng
                System.Windows.MessageBox.Show(
                    $"Không tìm thấy file khởi tạo cơ sở dữ liệu:\n{sqlFilePath}\n\n" +
                    "Vui lòng đặt file QuanLyKhoNguyenLieuPizza.sql vào thư mục Resources\\Database\\ cùng với file .exe",
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

            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Đã tạo Database '{databaseName}'.");

            // Đóng kết nối master, mở kết nối mới tới DB vừa tạo
            conn.Close();

            // Chờ SQL Server kích hoạt DB
            await Task.Delay(1000);

            // Kết nối tới DB mới tạo và chạy phần còn lại của script
            builder.InitialCatalog = databaseName;
            using var dbConn = new SqlConnection(builder.ConnectionString);
            await dbConn.OpenAsync();

            // Tách script theo lệnh GO
            var batches = SplitByGo(sqlScript);

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Bỏ qua các lệnh ALTER DATABASE, USE [master], sp_fulltext_database
                if (trimmed.StartsWith("ALTER DATABASE", StringComparison.OrdinalIgnoreCase)) continue;
                if (trimmed.StartsWith("USE [master]", StringComparison.OrdinalIgnoreCase)) continue;
                if (trimmed.Contains("sp_fulltext_database", StringComparison.OrdinalIgnoreCase)) continue;
                if (trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)) continue;
                if (trimmed.StartsWith("IF (1 = FULLTEXTSERVICEPROPERTY", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    using var cmd = new SqlCommand(trimmed, dbConn);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    // Ghi log nhưng tiếp tục (có thể một số lệnh không tương thích)
                    System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Lỗi SQL batch: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Batch: {trimmed.Substring(0, Math.Min(200, trimmed.Length))}...");
                }
            }

            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Tạo database '{databaseName}' thành công!");

            System.Windows.MessageBox.Show(
                $"Đã tạo cơ sở dữ liệu '{databaseName}' thành công!\n\nỨng dụng sẽ tiếp tục khởi động.",
                "Khởi tạo Database",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (SqlException ex) when (ex.Number == -1 || ex.Number == 2)
        {
            // Không kết nối được SQL Server
            System.Windows.MessageBox.Show(
                "Không thể kết nối đến SQL Server!\n\n" +
                "Vui lòng đảm bảo:\n" +
                "• SQL Server đã được cài đặt và đang chạy\n" +
                "• Tên Server trong appsettings.json đúng\n\n" +
                $"Chi tiết: {ex.Message}",
                "Lỗi Kết nối",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Lỗi: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Lỗi khi khởi tạo cơ sở dữ liệu:\n{ex.Message}",
                "Lỗi",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
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
