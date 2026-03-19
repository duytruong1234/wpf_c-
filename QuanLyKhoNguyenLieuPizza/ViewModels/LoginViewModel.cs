using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Utilities;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
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
            _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }
        
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
        ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
        
        // Kiểm tra kết nối khi khởi động
        _ = TestConnectionAsync();
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
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin đăng nhập!";
                return;
            }

            var taiKhoan = await _databaseService.AuthenticateAsync(Username, Password);
            
            if (taiKhoan != null)
            {
                CurrentUserSession.Instance.SetUser(taiKhoan);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
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
        Password = string.Empty;
        ErrorMessage = string.Empty;
    }
}


