using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ChangePasswordViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private string _email = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private int _passwordStrength;
    private string _passwordStrengthText = string.Empty;
    private bool _passwordsMatch;
    private bool _showPasswordMatch;
    private string _passwordMatchText = string.Empty;
    private bool _isLoading;
    private bool _showNewPassword;
    private bool _showConfirmPassword;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                UpdatePasswordStrength();
                CheckPasswordMatch();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                CheckPasswordMatch();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int PasswordStrength
    {
        get => _passwordStrength;
        set => SetProperty(ref _passwordStrength, value);
    }

    public string PasswordStrengthText
    {
        get => _passwordStrengthText;
        set => SetProperty(ref _passwordStrengthText, value);
    }

    public bool PasswordsMatch
    {
        get => _passwordsMatch;
        set => SetProperty(ref _passwordsMatch, value);
    }

    public bool ShowPasswordMatch
    {
        get => _showPasswordMatch;
        set => SetProperty(ref _showPasswordMatch, value);
    }

    public string PasswordMatchText
    {
        get => _passwordMatchText;
        set => SetProperty(ref _passwordMatchText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool ShowNewPassword
    {
        get => _showNewPassword;
        set => SetProperty(ref _showNewPassword, value);
    }

    public bool ShowConfirmPassword
    {
        get => _showConfirmPassword;
        set => SetProperty(ref _showConfirmPassword, value);
    }

    public ICommand ChangePasswordCommand { get; }
    public ICommand BackCommand { get; }

    public event Action? OnChangePasswordSuccess;
    public event Action? OnBack;

    public ChangePasswordViewModel()
    {
        try
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }
        catch
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }

        ChangePasswordCommand = new AsyncRelayCommand(ExecuteChangePasswordAsync, CanExecuteChangePassword);
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());
    }

    public void Reset()
    {
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = string.Empty;
        Email = string.Empty;
        ShowNewPassword = false;
        ShowConfirmPassword = false;
    }

    private void UpdatePasswordStrength()
    {
        if (string.IsNullOrEmpty(NewPassword))
        {
            PasswordStrength = 0;
            PasswordStrengthText = string.Empty;
            return;
        }

        int strength = 0;
        
        if (NewPassword.Length >= 8) strength++;
        if (NewPassword.Any(char.IsUpper)) strength++;
        if (NewPassword.Any(char.IsDigit)) strength++;
        if (NewPassword.Any(c => !char.IsLetterOrDigit(c))) strength++;

        PasswordStrength = strength;
        PasswordStrengthText = strength switch
        {
            1 => "Yếu",
            2 => "Trung bình",
            3 => "Mạnh",
            4 => "Rất mạnh",
            _ => string.Empty
        };
    }

    private void CheckPasswordMatch()
    {
        // Ch? hi?n th? khi c? hai tru?ng d?u có n?i dung
        ShowPasswordMatch = !string.IsNullOrEmpty(NewPassword) && !string.IsNullOrEmpty(ConfirmPassword);

        if (ShowPasswordMatch)
        {
            PasswordsMatch = NewPassword == ConfirmPassword;
            PasswordMatchText = PasswordsMatch ? "Mật khẩu khớp" : "Mật khẩu không khớp";
        }
    }

    private bool CanExecuteChangePassword(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword) &&
               PasswordsMatch &&
               PasswordStrength >= 2 &&
               !IsLoading;
    }

    private async Task ExecuteChangePasswordAsync(object? parameter)
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (string.IsNullOrEmpty(Email))
            {
                ErrorMessage = "Email không hợp lệ!";
                return;
            }

            bool success = await _databaseService.ChangePasswordAsync(Email, NewPassword);
            
            if (success)
            {
                OnChangePasswordSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Không thể đổi mật khẩu. Vui lòng thử lại!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

