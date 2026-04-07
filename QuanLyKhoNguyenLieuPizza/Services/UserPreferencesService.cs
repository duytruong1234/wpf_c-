using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Dịch vụ lưu trữ tùy chọn người dùng (ghi nhớ đăng nhập)
/// Lưu vào file JSON trong thư mục AppData/Local
/// </summary>
public class UserPreferencesService
{
    private static UserPreferencesService? _instance;
    private readonly string _preferencesFilePath;
    
    // Entropy bổ sung cho mã hóa DPAPI
    private static readonly byte[] AdditionalEntropy = 
        Encoding.UTF8.GetBytes("QuanLyKhoNguyenLieuPizza_v1");

    public static UserPreferencesService Instance => _instance ??= new UserPreferencesService();

    private UserPreferencesService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyKhoNguyenLieuPizza");
        
        Directory.CreateDirectory(appDataPath);
        _preferencesFilePath = Path.Combine(appDataPath, "user_preferences.json");
    }

    /// <summary>
    /// Lưu thông tin đăng nhập
    /// </summary>
    public void SaveLoginCredentials(string username, string password)
    {
        try
        {
            var preferences = LoadPreferencesFile();
            preferences.RememberMe = true;
            preferences.SavedUsername = username;
            preferences.EncryptedPassword = EncryptPassword(password);
            preferences.LastSavedDate = DateTime.Now;
            SavePreferencesFile(preferences);
            
            System.Diagnostics.Debug.WriteLine($"Đã lưu thông tin đăng nhập cho: {username}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu thông tin đăng nhập: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc thông tin đăng nhập đã lưu
    /// </summary>
    public (bool rememberMe, string username, string password) LoadLoginCredentials()
    {
        try
        {
            var preferences = LoadPreferencesFile();
            
            if (preferences.RememberMe && !string.IsNullOrEmpty(preferences.SavedUsername))
            {
                var password = DecryptPassword(preferences.EncryptedPassword);
                return (true, preferences.SavedUsername, password);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc thông tin đăng nhập: {ex.Message}");
        }
        
        return (false, string.Empty, string.Empty);
    }

    /// <summary>
    /// Xóa thông tin đăng nhập đã lưu
    /// </summary>
    public void ClearLoginCredentials()
    {
        try
        {
            var preferences = LoadPreferencesFile();
            preferences.RememberMe = false;
            preferences.SavedUsername = string.Empty;
            preferences.EncryptedPassword = string.Empty;
            preferences.LastSavedDate = null;
            SavePreferencesFile(preferences);
            
            System.Diagnostics.Debug.WriteLine("Đã xóa thông tin đăng nhập đã lưu");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi xóa thông tin đăng nhập: {ex.Message}");
        }
    }

    /// <summary>
    /// Mã hóa mật khẩu bằng Windows DPAPI (chỉ giải mã được trên cùng máy + user)
    /// </summary>
    private string EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        try
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var encryptedBytes = ProtectedData.Protect(
                passwordBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi mã hóa mật khẩu: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Giải mã mật khẩu
    /// </summary>
    private string DecryptPassword(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedPassword);
            var passwordBytes = ProtectedData.Unprotect(
                encryptedBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(passwordBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi giải mã mật khẩu: {ex.Message}");
            return string.Empty;
        }
    }

    private UserPreferences LoadPreferencesFile()
    {
        try
        {
            if (File.Exists(_preferencesFilePath))
            {
                var json = File.ReadAllText(_preferencesFilePath);
                return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi đọc file preferences: {ex.Message}");
        }
        
        return new UserPreferences();
    }

    private void SavePreferencesFile(UserPreferences preferences)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(preferences, options);
        File.WriteAllText(_preferencesFilePath, json);
    }

    /// <summary>
    /// Model lưu trữ tùy chọn người dùng
    /// </summary>
    private class UserPreferences
    {
        public bool RememberMe { get; set; }
        public string SavedUsername { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public DateTime? LastSavedDate { get; set; }
    }
}
