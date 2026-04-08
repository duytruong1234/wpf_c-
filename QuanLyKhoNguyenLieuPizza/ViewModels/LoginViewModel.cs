using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Utilities;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly UserPreferencesService _preferencesService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _rememberMe;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand ForgotPasswordCommand { get; }

    public event Action? OnLoginSuccess;
    public event Action? OnForgotPassword;

    public LoginViewModel()
    {
        try
        {
            _databaseService = new DatabaseService();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }
        
        _preferencesService = UserPreferencesService.Instance;
        
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
        ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
        
        // T?i thông tin dang nh?p dã luu (n?u có)
        LoadSavedCredentials();
        
        // Ki?m tra k?t n?i khi kh?i d?ng
        _ = TestConnectionAsync();
    }

    /// <summary>
    /// T?i thông tin dang nh?p dã luu t? l?n tru?c
    /// </summary>
    private void LoadSavedCredentials()
    {
        try
        {
            var (rememberMe, savedUsername, savedPassword) = _preferencesService.LoadLoginCredentials();
            
            if (rememberMe && !string.IsNullOrEmpty(savedUsername))
            {
                Username = savedUsername;
                Password = savedPassword;
                RememberMe = true;
                
                System.Diagnostics.Debug.WriteLine($"Ðã t?i thông tin dang nh?p dã luu cho: {savedUsername}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"L?i khi t?i thông tin dang nh?p: {ex.Message}");
        }
    }

    private async Task TestConnectionAsync()
    {
        var (success, _) = await DatabaseConnectionTester.TestConnectionAsync();
        if (!success)
            System.Diagnostics.Debug.WriteLine("Database connection test failed");
    }

    private bool CanExecuteLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(Password) && 
               !IsLoading;
    }

    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nh?p d?y d? thông tin dang nh?p!";
                return;
            }

            var taiKhoan = await _databaseService.AuthenticateAsync(Username, Password);
            
            if (taiKhoan != null)
            {
                // X? lý ghi nh? dang nh?p
                if (RememberMe)
                {
                    _preferencesService.SaveLoginCredentials(Username, Password);
                }
                else
                {
                    _preferencesService.ClearLoginCredentials();
                }
                
                CurrentUserSession.Instance.SetUser(taiKhoan);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Tên dang nh?p ho?c m?t kh?u không dúng!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"L?i: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ExecuteForgotPassword(object? parameter)
    {
        OnForgotPassword?.Invoke();
    }

    public void Reset()
    {
        // Ch? xóa password và error n?u KHÔNG ghi nh? dang nh?p
        if (!RememberMe)
        {
            Username = string.Empty;
            Password = string.Empty;
        }
        ErrorMessage = string.Empty;
    }
}
