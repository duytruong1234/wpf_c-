using System.IO;
using System.Text.Json;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Service to read configuration from appsettings.json
/// </summary>
public class ConfigurationService
{
    private static ConfigurationService? _instance;
    private readonly Dictionary<string, string> _connectionStrings = new();
    private readonly Dictionary<string, string> _appSettings = new();

    public static ConfigurationService Instance => _instance ??= new ConfigurationService();

    private ConfigurationService()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (!File.Exists(configPath))
            {
                // Try development path
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<JsonElement>(json);

                // Load ConnectionStrings
                if (config.TryGetProperty("ConnectionStrings", out var connStrings))
                {
                    foreach (var prop in connStrings.EnumerateObject())
                    {
                        _connectionStrings[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }

                // Load AppSettings
                if (config.TryGetProperty("AppSettings", out var appSettings))
                {
                    foreach (var prop in appSettings.EnumerateObject())
                    {
                        _appSettings[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("appsettings.json not found, using default values");
                // Set default connection string
                _connectionStrings["DefaultConnection"] = "Server=localhost;Database=QuanLyKhoNguyenLieu;Trusted_Connection=True;TrustServerCertificate=True;";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }

    public string GetConnectionString(string name = "DefaultConnection")
    {
        return _connectionStrings.TryGetValue(name, out var value) ? value : string.Empty;
    }

    public string GetAppSetting(string key)
    {
        return _appSettings.TryGetValue(key, out var value) ? value : string.Empty;
    }
}

