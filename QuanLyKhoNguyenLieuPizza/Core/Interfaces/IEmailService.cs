namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

public interface IEmailService
{
    Task<(bool Success, string Message)> SendOTPAsync(string email, string otpCode);
    Task<(bool Success, string Message)> SendEmailAsync(string toEmail, string subject, string body);
    string GenerateOTP(int length = 6);
}

