using System;
using System.IO;

namespace QuanLyKhoNguyenLieuPizza.Helpers;

/// <summary>
/// Helper quản lý lưu trữ hình ảnh.
/// Hình ảnh được lưu trong AppData/Local/QuanLyKhoPizza/Images để tránh
/// vấn đề quyền ghi khi cài vào Program Files, và đảm bảo ảnh tồn tại
/// khi publish/cài đặt trên máy khác.
/// </summary>
public static class ImageStorageHelper
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QuanLyKhoPizza");

    /// <summary>
    /// Thư mục gốc lưu trữ hình ảnh trong AppData
    /// </summary>
    public static string ImagesRootFolder => Path.Combine(AppDataFolder, "Images");

    /// <summary>
    /// Thư mục lưu ảnh nhân viên
    /// </summary>
    public static string NhanVienImagesFolder => Path.Combine(ImagesRootFolder, "NhanVien");

    /// <summary>
    /// Đảm bảo tất cả thư mục ảnh đã được tạo
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(ImagesRootFolder);
        Directory.CreateDirectory(NhanVienImagesFolder);
    }

    /// <summary>
    /// Copy file ảnh vào AppData và trả về relative path để lưu vào DB.
    /// Relative path sẽ có dạng: /Images/filename.jpg hoặc /Images/NhanVien/filename.jpg
    /// </summary>
    /// <param name="sourceFilePath">Đường dẫn tuyệt đối file nguồn</param>
    /// <param name="subfolder">Thư mục con (VD: "NhanVien"), null nếu lưu trực tiếp vào Images</param>
    /// <returns>Đường dẫn tương đối để lưu vào DB</returns>
    public static string CopyImageToStorage(string sourceFilePath, string? subfolder = null)
    {
        EnsureDirectories();

        var fileName = Path.GetFileName(sourceFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var uniqueFileName = $"{nameWithoutExt}_{timestamp}{extension}";

        string destFolder;
        string relativePath;

        if (!string.IsNullOrEmpty(subfolder))
        {
            destFolder = Path.Combine(ImagesRootFolder, subfolder);
            Directory.CreateDirectory(destFolder);
            relativePath = $"/Images/{subfolder}/{uniqueFileName}";
        }
        else
        {
            destFolder = ImagesRootFolder;
            relativePath = $"/Images/{uniqueFileName}";
        }

        var destPath = Path.Combine(destFolder, uniqueFileName);
        File.Copy(sourceFilePath, destPath, true);

        return relativePath;
    }

    /// <summary>
    /// Phân giải đường dẫn tương đối từ DB thành đường dẫn tuyệt đối.
    /// Tìm ảnh ở nhiều vị trí: AppData, BaseDirectory (compat cũ).
    /// </summary>
    /// <param name="relativePath">Đường dẫn tương đối từ DB</param>
    /// <returns>Đường dẫn tuyệt đối nếu tìm thấy, null nếu không</returns>
    public static string? ResolveImagePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        // 1. Tìm trong AppData (cách lưu mới: /Images/...)
        var appDataPath = Path.Combine(AppDataFolder, cleanPath);
        if (File.Exists(appDataPath))
            return appDataPath;

        // 2. Tìm trong BaseDirectory (compat cũ: /Resources/Images/...)
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var baseAbsolutePath = Path.Combine(basePath, cleanPath);
        if (File.Exists(baseAbsolutePath))
            return baseAbsolutePath;

        // 3. Tìm dạng đường dẫn tuyệt đối
        if (File.Exists(relativePath))
            return relativePath;

        // 4. Thử tìm với prefix Resources (compat: cũ lưu /Resources/Images/..., mới lưu /Images/...)
        if (cleanPath.StartsWith("Resources" + Path.DirectorySeparatorChar))
        {
            // Thử tìm trong AppData với đường dẫn bỏ "Resources/"
            var withoutResources = cleanPath.Substring("Resources\\".Length);
            var altPath = Path.Combine(AppDataFolder, withoutResources);
            if (File.Exists(altPath))
                return altPath;
        }

        return null;
    }

    /// <summary>
    /// Migrate hình ảnh cũ từ BaseDirectory sang AppData (chạy 1 lần khi khởi động app)
    /// </summary>
    public static void MigrateImagesFromBaseDirectory()
    {
        try
        {
            EnsureDirectories();

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var oldImagesPath = Path.Combine(basePath, "Resources", "Images");

            if (!Directory.Exists(oldImagesPath))
                return;

            // Copy tất cả file ảnh từ thư mục cũ sang AppData
            CopyDirectoryRecursive(oldImagesPath, ImagesRootFolder);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error migrating images: {ex.Message}");
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);

            // Chỉ copy nếu chưa tồn tại
            if (!File.Exists(destFile))
            {
                try
                {
                    File.Copy(file, destFile, false);
                }
                catch { /* Bỏ qua lỗi trùng file */ }
            }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectoryRecursive(dir, Path.Combine(destDir, dirName));
        }
    }
}
