namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

public interface IEmailService
{
    Task<(bool Success, string Message)> SendOTPAsync(string email, string otpCode);
    Task<(bool Success, string Message)> SendEmailAsync(string toEmail, string subject, string body);
    Task<(bool Success, string Message)> SendExpiryNotificationAsync(
        string toEmail,
        List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)> nearExpiryItems,
        List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)> expiredItems);
    string GenerateOTP(int length = 6);
}
