using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class SaoLuuViewModel : BaseViewModel
{
    private string _statusMessage = string.Empty;
    private string _lastBackupInfo = string.Empty;
    private bool _isBackupInProgress;
    private bool _isRestoreInProgress;
    private double _progressValue;
    private string? _lastBackupPath;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LastBackupInfo
    {
        get => _lastBackupInfo;
        set => SetProperty(ref _lastBackupInfo, value);
    }

    public bool IsBackupInProgress
    {
        get => _isBackupInProgress;
        set
        {
            SetProperty(ref _isBackupInProgress, value);
            OnPropertyChanged(nameof(IsNotProcessing));
        }
    }

    public bool IsRestoreInProgress
    {
        get => _isRestoreInProgress;
        set
        {
            SetProperty(ref _isRestoreInProgress, value);
            OnPropertyChanged(nameof(IsNotProcessing));
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public bool IsNotProcessing => !IsBackupInProgress && !IsRestoreInProgress;

    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand OpenBackupFolderCommand { get; }

    public SaoLuuViewModel()
    {
        BackupCommand = new AsyncRelayCommand(ExecuteBackupAsync);
        RestoreCommand = new AsyncRelayCommand(ExecuteRestoreAsync);
        OpenBackupFolderCommand = new RelayCommand(_ => OpenDefaultBackupFolder());

        LoadLastBackupInfo();
        LoadSavedBackupPath();
    }

    private string GetConnectionString()
    {
        return ConfigurationService.Instance.GetConnectionString();
    }

    private string GetDefaultBackupFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PizzaInn_Backup");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return folder;
    }

    private string GetBackupPathFile()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PizzaInn", "last_backup.txt");
    }

    private void LoadSavedBackupPath()
    {
        try
        {
            var pathFile = GetBackupPathFile();
            if (File.Exists(pathFile))
            {
                _lastBackupPath = File.ReadAllText(pathFile).Trim();
                if (File.Exists(_lastBackupPath))
                {
                    UpdateBackupInfoFromFile(_lastBackupPath);
                }
            }
        }
        catch { /* ignore */ }
    }

    private void SaveLastBackupPath(string path)
    {
        try
        {
            _lastBackupPath = path;
            var dir = Path.GetDirectoryName(GetBackupPathFile())!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(GetBackupPathFile(), path);
        }
        catch { /* ignore */ }
    }

    private void LoadLastBackupInfo()
    {
        try
        {
            var folder = GetDefaultBackupFolder();
            var files = Directory.GetFiles(folder, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (files.Any())
            {
                UpdateBackupInfoFromFile(files.First().FullName);
            }
            else
            {
                LastBackupInfo = "Chưa có bản sao lưu nào.\nHãy tạo bản sao lưu đầu tiên để bảo vệ dữ liệu!";
            }
        }
        catch (Exception ex)
        {
            LastBackupInfo = $"Không thể đọc thông tin sao lưu: {ex.Message}";
        }
    }

    private void UpdateBackupInfoFromFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
                LastBackupInfo = $"Bản sao lưu gần nhất: {fileInfo.Name}\n" +
                                 $"Thời gian: {fileInfo.LastWriteTime:dd/MM/yyyy HH:mm:ss}\n" +
                                 $"Kích thước: {sizeInMb:F2} MB\n" +
                                 $"Vị trí: {fileInfo.DirectoryName}";
            }
        }
        catch { /* ignore */ }
    }

    private async Task ExecuteBackupAsync()
    {
        try
        {
            // Hỏi người dùng chọn nơi lưu
            var dialog = new SaveFileDialog
            {
                Title = "Chọn nơi lưu bản sao lưu database",
                Filter = "Tệp sao lưu CSDL (*.bak)|*.bak",
                FileName = $"PizzaInn_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
                InitialDirectory = GetDefaultBackupFolder()
            };

            if (dialog.ShowDialog() != true)
                return;

            IsBackupInProgress = true;
            StatusMessage = "Đang sao lưu database...";
            ProgressValue = 0;

            var backupPath = dialog.FileName;

            await Task.Run(() =>
            {
                var connectionString = GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Thực hiện backup
                var sql = $@"
                    BACKUP DATABASE [QuanLyKhoNguyenLieuPizza] 
                    TO DISK = N'{backupPath}' 
                    WITH FORMAT, INIT, 
                    NAME = N'PizzaInn Backup - {DateTime.Now:dd/MM/yyyy HH:mm}',
                    COMPRESSION,
                    STATS = 10";

                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 300; // 5 phút timeout
                command.ExecuteNonQuery();
            });

            ProgressValue = 100;
            StatusMessage = $"Sao lưu thành công!\nĐường dẫn: {backupPath}";
            SaveLastBackupPath(backupPath);
            UpdateBackupInfoFromFile(backupPath);

            MessageBox.Show(
                $"Sao lưu database thành công!\n\nFile: {backupPath}\nThời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                "Sao lưu thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi sao lưu: {ex.Message}";
            MessageBox.Show(
                $"Sao lưu thất bại!\n\nLỗi: {ex.Message}",
                "Lỗi sao lưu",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBackupInProgress = false;
        }
    }

    private async Task ExecuteRestoreAsync()
    {
        try
        {
            // Hỏi người dùng chọn file backup
            var dialog = new OpenFileDialog
            {
                Title = "Chọn file sao lưu để phục hồi",
                Filter = "Tệp sao lưu CSDL (*.bak)|*.bak",
                InitialDirectory = GetDefaultBackupFolder()
            };

            if (dialog.ShowDialog() != true)
                return;

            // Cảnh báo trước khi restore
            var confirm = MessageBox.Show(
                "⚠️ CẢNH BÁO: Phục hồi database sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại!\n\n" +
                "Tất cả hóa đơn, nguyên liệu, nhân viên... hiện có sẽ bị thay thế bằng dữ liệu trong file sao lưu.\n\n" +
                "Bạn có chắc chắn muốn tiếp tục?",
                "Xác nhận phục hồi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            // Xác nhận lần 2
            var confirm2 = MessageBox.Show(
                "Xác nhận lần cuối:\n\n" +
                $"File phục hồi: {Path.GetFileName(dialog.FileName)}\n\n" +
                "Nhấn [Yes] để bắt đầu phục hồi.",
                "Xác nhận lần cuối",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm2 != MessageBoxResult.Yes)
                return;

            IsRestoreInProgress = true;
            StatusMessage = "Đang phục hồi database... Vui lòng không tắt ứng dụng!";
            ProgressValue = 0;

            var backupPath = dialog.FileName;

            await Task.Run(() =>
            {
                // Kết nối vào master database để thực hiện restore
                var connectionString = GetConnectionString();
                var builder = new SqlConnectionStringBuilder(connectionString);
                builder.InitialCatalog = "master";
                
                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();

                // Đặt database về single user mode
                try
                {
                    using var cmdSingle = new SqlCommand(
                        "ALTER DATABASE [QuanLyKhoNguyenLieuPizza] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                        connection);
                    cmdSingle.CommandTimeout = 60;
                    cmdSingle.ExecuteNonQuery();
                }
                catch
                {
                    // Database có thể chưa tồn tại, bỏ qua lỗi
                }

                // Thực hiện restore
                var sql = $@"
                    RESTORE DATABASE [QuanLyKhoNguyenLieuPizza] 
                    FROM DISK = N'{backupPath}' 
                    WITH REPLACE, STATS = 10";

                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 600; // 10 phút timeout
                command.ExecuteNonQuery();

                // Đặt lại multi user mode
                using var cmdMulti = new SqlCommand(
                    "ALTER DATABASE [QuanLyKhoNguyenLieuPizza] SET MULTI_USER;",
                    connection);
                cmdMulti.CommandTimeout = 60;
                cmdMulti.ExecuteNonQuery();
            });

            ProgressValue = 100;
            StatusMessage = $"Phục hồi database thành công từ: {Path.GetFileName(backupPath)}";

            MessageBox.Show(
                $"Phục hồi database thành công!\n\nFile: {Path.GetFileName(backupPath)}\n\n" +
                "Khuyến nghị: Khởi động lại ứng dụng để đảm bảo dữ liệu được cập nhật đầy đủ.",
                "Phục hồi thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi phục hồi: {ex.Message}";
            MessageBox.Show(
                $"Phục hồi thất bại!\n\nLỗi: {ex.Message}\n\n" +
                "Hãy thử lại hoặc liên hệ hỗ trợ kỹ thuật.",
                "Lỗi phục hồi",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsRestoreInProgress = false;
        }
    }

    private void OpenDefaultBackupFolder()
    {
        try
        {
            // Mở thư mục chứa file backup gần nhất, nếu không có thì mở thư mục mặc định
            var folder = GetDefaultBackupFolder();
            if (!string.IsNullOrEmpty(_lastBackupPath) && File.Exists(_lastBackupPath))
            {
                folder = Path.GetDirectoryName(_lastBackupPath)!;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
