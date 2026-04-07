using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class MainViewModel : BaseViewModel
{
    private object? _currentView;
    private readonly LoginViewModel _loginViewModel;
    private readonly VerifyInfoViewModel _verifyInfoViewModel;
    private readonly OTPViewModel _otpViewModel;
    private readonly ChangePasswordViewModel _changePasswordViewModel;
    private ShellViewModel? _shellViewModel;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public MainViewModel()
    {
        _loginViewModel = new LoginViewModel();
        _verifyInfoViewModel = new VerifyInfoViewModel();
        _otpViewModel = new OTPViewModel();
        _changePasswordViewModel = new ChangePasswordViewModel();

        // Thiết lập các sự kiện điều hướng
        _loginViewModel.OnLoginSuccess += OnLoginSuccess;
        _loginViewModel.OnForgotPassword += async () =>
        {
            CurrentView = _verifyInfoViewModel;
            await _verifyInfoViewModel.ReloadChucVusAsync();
        };

        _verifyInfoViewModel.OnVerifySuccess += OnVerifyInfoSuccess;
        _verifyInfoViewModel.OnBack += () => CurrentView = _loginViewModel;

        _otpViewModel.OnVerifySuccess += () => 
        {
            _changePasswordViewModel.Reset();
            _changePasswordViewModel.Email = _otpViewModel.Email;
            CurrentView = _changePasswordViewModel;
        };
        _otpViewModel.OnClose += () => CurrentView = _loginViewModel;

        _changePasswordViewModel.OnChangePasswordSuccess += () => CurrentView = _loginViewModel;
        _changePasswordViewModel.OnBack += () => CurrentView = _otpViewModel;

        // Bắt đầu với màn hình đăng nhập
        CurrentView = _loginViewModel;
    }

    private async void OnVerifyInfoSuccess(string email)
    {
        try
        {
            App.LogToFile($"OnVerifyInfoSuccess START - email: {email}");
            System.Diagnostics.Debug.WriteLine($"=== OnVerifyInfoSuccess called with email: {email} ===");

            // Chuyển sang view OTP trước để user thấy loading
            App.LogToFile("Setting CurrentView = _otpViewModel");
            CurrentView = _otpViewModel;
            App.LogToFile("CurrentView set OK");

            // Khởi tạo và gửi OTP đến email
            App.LogToFile("Calling InitializeAndSendOTPAsync");
            await _otpViewModel.InitializeAndSendOTPAsync(email);

            App.LogToFile("OnVerifyInfoSuccess DONE");
            System.Diagnostics.Debug.WriteLine("=== OTP sent successfully ===");
        }
        catch (Exception ex)
        {
            App.LogToFile($"OnVerifyInfoSuccess ERROR: {ex}");
            System.Diagnostics.Debug.WriteLine($"=== OnVerifyInfoSuccess ERROR: {ex} ===");
            System.Windows.MessageBox.Show(
                $"Lỗi khi gửi OTP: {ex.Message}",
                "Lỗi",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            CurrentView = _verifyInfoViewModel;
        }
    }

    private void OnLoginSuccess()
    {
        // Tạo ShellViewModel SAU khi đăng nhập thành công, khi CurrentUserSession đã được điền
        _shellViewModel = new ShellViewModel();
        _shellViewModel.OnLogout += OnLogout;
        
        CurrentView = _shellViewModel;
    }

    private void OnLogout()
    {
        // Xóa shell view model khi đăng xuất
        if (_shellViewModel != null)
        {
            _shellViewModel.OnLogout -= OnLogout;
            _shellViewModel = null;
        }

        _loginViewModel.Reset();
        CurrentUserSession.Instance.Logout();
        CurrentView = _loginViewModel;
    }
}


