using System.Windows.Input;
using System.Windows.Threading;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Commands;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class OTPViewModel : BaseViewModel
{
    private readonly EmailService _emailService;
    private string _otpCode = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private string _countdownText = "05:00";
    private bool _canResend;
    private bool _isLoading;
    private int _remainingSeconds = 300;
    private readonly DispatcherTimer _timer;
    private bool _isSending;

    private string _email = string.Empty;
    private string _currentOTP = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string OTPCode
    {
        get => _otpCode;
        set => SetProperty(ref _otpCode, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    public string CountdownText
    {
        get => _countdownText;
        set => SetProperty(ref _countdownText, value);
    }

    public bool CanResend
    {
        get => _canResend;
        set => SetProperty(ref _canResend, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand VerifyOTPCommand { get; }
    public ICommand ResendOTPCommand { get; }
    public ICommand CloseCommand { get; }

    public event Action? OnVerifySuccess;
    public event Action? OnClose;

    public OTPViewModel()
    {
        _emailService = new EmailService();
        
        VerifyOTPCommand = new RelayCommand(ExecuteVerifyOTP, CanExecuteVerifyOTP);
        ResendOTPCommand = new AsyncRelayCommand(async () => await ExecuteResendOTPAsync(), () => CanResend && !IsLoading);
        CloseCommand = new RelayCommand(_ => OnClose?.Invoke());

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
    }

    /// <summary>
    /// Kh?i t?o v� g?i OTP d?n email
    /// </summary>
    public async Task InitializeAndSendOTPAsync(string email)
    {
        // Ngan g?i nhi?u l?n c�ng l�c
        if (_isSending)
        {
            System.Diagnostics.Debug.WriteLine("=== BLOCKED: Already sending OTP ===");
            return;
        }

        _isSending = true;
        try
        {
            Email = email;
            await SendOTPAsync();
            StartCountdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeAndSendOTPAsync Error: {ex}");
            ErrorMessage = $"L?i kh?i t?o OTP: {ex.Message}";
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task SendOTPAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            // T?o m� OTP m?i
            _currentOTP = _emailService.GenerateOTP(6);
            
            System.Diagnostics.Debug.WriteLine($"=== Sending OTP ===");
            System.Diagnostics.Debug.WriteLine($"Email: {Email}");
            System.Diagnostics.Debug.WriteLine($"OTP: {_currentOTP}");

            // G?i OTP qua email
            var (success, message) = await _emailService.SendOTPAsync(Email, _currentOTP);

            if (success)
            {
                SuccessMessage = message;
            }
            else
            {
                ErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"L?i g?i OTP: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SendOTP Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartCountdown()
    {
        _remainingSeconds = 300; // 5 ph�t
        CanResend = false;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            CanResend = true;
            CountdownText = "00:00";
        }
        else
        {
            var minutes = _remainingSeconds / 60;
            var seconds = _remainingSeconds % 60;
            CountdownText = $"{minutes:D2}:{seconds:D2}";
        }
    }

    private bool CanExecuteVerifyOTP(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(OTPCode) && 
               OTPCode.Length == 6 && 
               !IsLoading;
    }

    private void ExecuteVerifyOTP(object? parameter)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsLoading = true;

        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Verifying OTP ===");
            System.Diagnostics.Debug.WriteLine($"Email: {Email}");
            System.Diagnostics.Debug.WriteLine($"Input OTP: {OTPCode}");
            System.Diagnostics.Debug.WriteLine($"Expected OTP (local): {_currentOTP}");

            // Verify OTP using EmailService (which has the actual OTP storage)
            bool isValid = _emailService.VerifyOTP(Email, OTPCode);
            
            System.Diagnostics.Debug.WriteLine($"VerifyOTP result: {isValid}");

            if (isValid)
            {
                SuccessMessage = "X�c th?c OTP th�nh c�ng!";
                _timer.Stop();

                // Invoke success on UI thread to avoid cross-thread issues in subscribers
                try
                {
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    System.Diagnostics.Debug.WriteLine("Invoking OnVerifySuccess (will use Dispatcher.BeginInvoke if available)");
                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                OnVerifySuccess?.Invoke();
                            }
                            catch (Exception innerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"OnVerifySuccess subscriber threw: {innerEx}");
                            }
                        }));
                    }
                    else
                    {
                        try
                        {
                            OnVerifySuccess?.Invoke();
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"OnVerifySuccess subscriber threw: {innerEx}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OnVerifySuccess invoke scheduling error: {ex}");
                }
                finally
                {
                    // clear local OTP input
                    OTPCode = string.Empty;
                }
            }
            else
            {
                ErrorMessage = "M� OTP kh�ng ch�nh x�c ho?c d� h?t h?n!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Verify OTP Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteResendOTPAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email kh�ng h?p l?!";
            return;
        }

        await SendOTPAsync();
        
        if (string.IsNullOrEmpty(ErrorMessage))
        {
            StartCountdown();
        }
    }
}
