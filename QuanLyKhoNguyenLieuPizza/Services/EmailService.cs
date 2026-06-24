using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class EmailService : IEmailService
{
    // Cấu hình SMTP
    private readonly string _smtpHost = GetSetting("SMTP_HOST", "SmtpHost", "smtp.gmail.com");
    private readonly int _smtpPort = GetIntSetting("SMTP_PORT", "SmtpPort", 587);
    private readonly string _senderEmail = GetSetting("SMTP_SENDER_EMAIL", "SmtpSenderEmail", "your-email@example.com");
    private readonly string _senderPassword = GetSetting("SMTP_SENDER_PASSWORD", "SmtpSenderPassword", "your-app-password");
    private readonly string _senderDisplayName = GetSetting("SMTP_SENDER_NAME", "SmtpSenderName", "Pizza Warehouse System");
    private readonly bool _enableSsl = GetBoolSetting("SMTP_ENABLE_SSL", "SmtpEnableSsl", true);

    private static readonly Dictionary<string, (string OTP, DateTime Expiry)> _otpStorage = new();

    private static string GetSetting(string environmentKey, string appSettingKey, string fallback)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentKey);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        var appSettingValue = ConfigurationService.Instance.GetAppSetting(appSettingKey);
        return string.IsNullOrWhiteSpace(appSettingValue) ? fallback : appSettingValue;
    }

    private static int GetIntSetting(string environmentKey, string appSettingKey, int fallback)
    {
        return int.TryParse(GetSetting(environmentKey, appSettingKey, string.Empty), out var value)
            ? value
            : fallback;
    }

    private static bool GetBoolSetting(string environmentKey, string appSettingKey, bool fallback)
    {
        return bool.TryParse(GetSetting(environmentKey, appSettingKey, string.Empty), out var value)
            ? value
            : fallback;
    }

    public string GenerateOTP(int length = 6)
    {
        const string chars = "0123456789";
        var random = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        random.GetBytes(bytes);

        var otp = new char[length];
        for (int i = 0; i < length; i++)
        {
            otp[i] = chars[bytes[i] % chars.Length];
        }

        return new string(otp);
    }

    public async Task<(bool Success, string Message)> SendOTPAsync(string email, string otpCode)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email kh\u00f4ng \u0111\u01b0\u1ee3c \u0111\u1ec3 tr\u1ed1ng!");
        }

        if (!IsValidEmail(email))
        {
            return (false, "\u0110\u1ecbnh d\u1ea1ng email kh\u00f4ng h\u1ee3p l\u1ec7!");
        }

        var key = email.ToLower();
        _otpStorage[key] = (otpCode, DateTime.Now.AddMinutes(5));

        var subject = "M\u00e3 OTP X\u00e1c Th\u1ef1c - Pizza Warehouse System";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #E85D04, #F48C06); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .otp-code {{ font-size: 36px; font-weight: bold; color: #E85D04; letter-spacing: 8px; padding: 20px 30px; background: #FFF3E0; border-radius: 12px; display: inline-block; margin: 20px 0; }}
        .message {{ color: #666; font-size: 14px; line-height: 1.6; }}
        .warning {{ color: #E85D04; font-size: 12px; margin-top: 20px; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; color: #999; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Pizza Warehouse System</h1>
        </div>
        <div class='content'>
            <p class='message'>Xin ch&#224;o,</p>
            <p class='message'>B&#7841;n &#273;&#227; y&#234;u c&#7847;u &#273;&#7863;t l&#7841;i m&#7853;t kh&#7849;u. S&#7917; d&#7909;ng m&#227; OTP b&#234;n d&#432;&#7899;i &#273;&#7875; x&#225;c th&#7921;c:</p>
            <div class='otp-code'>{otpCode}</div>
            <p class='message'>M&#227; OTP c&#243; hi&#7879;u l&#7921;c trong <strong>5 ph&#250;t</strong>.</p>
            <p class='warning'>N&#7871;u b&#7841;n kh&#244;ng y&#234;u c&#7847;u &#273;&#7863;t l&#7841;i m&#7853;t kh&#7849;u, vui l&#242;ng b&#7887; qua email n&#224;y.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Pizza Warehouse System. All rights reserved.</p>
            <p>&#272;&#226;y l&#224; email t&#7921; &#273;&#7897;ng, vui l&#242;ng kh&#244;ng tr&#7843; l&#7901;i.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(email, subject, body);
    }

    public async Task<(bool Success, string Message)> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            if (_senderEmail.Contains("your-email") || _senderPassword.Contains("your-app-password"))
            {

                return (true, $"[DEV MODE] M\u00e3 OTP \u0111\u00e3 \u0111\u01b0\u1ee3c g\u1eedi \u0111\u1ebfn {toEmail}");
            }

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_senderEmail, _senderPassword),
                EnableSsl = _enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);

            return (true, "M\u00e3 OTP \u0111\u00e3 \u0111\u01b0\u1ee3c g\u1eedi \u0111\u1ebfn email c\u1ee7a b\u1ea1n!");
        }
        catch (SmtpException smtpEx)
        {
            return (false, $"L\u1ed7i g\u1eedi email: {smtpEx.Message}");
        }
        catch (Exception ex) {
            return (false, $"Kh\u00f4ng th\u1ec3 g\u1eedi email: {ex.Message}");
        }
    }

    public bool VerifyOTP(string email, string otpCode)
    {
        var key = email.ToLower();

        if (!_otpStorage.TryGetValue(key, out var stored))
        {
            foreach (var k in _otpStorage.Keys)
            {
            }
            return false;
        }

        if (DateTime.Now > stored.Expiry)
        {
            _otpStorage.Remove(key);
            return false;
        }

        if (stored.OTP == otpCode)
        {
            _otpStorage.Remove(key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gửi email thông báo nguyên liệu sắp hết hạn / đã hết hạn
    /// </summary>
    public async Task<(bool Success, string Message)> SendExpiryNotificationAsync(
        string toEmail,
        List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)> nearExpiryItems,
        List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)> expiredItems)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !IsValidEmail(toEmail))
            return (false, "Email không hợp lệ!");

        int totalItems = nearExpiryItems.Count + expiredItems.Count;
        if (totalItems == 0)
            return (true, "Không có nguyên liệu sắp hết hạn hoặc đã hết hạn.");

        var subject = $"Cảnh báo HSD Nguyên Liệu - {DateTime.Now:dd/MM/yyyy} - Pizza Warehouse";

        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string FormatQuantity(decimal value) => value % 1 == 0 ? value.ToString("N0") : value.ToString("N2");

        static string BuildItemCard(
            string tenNguyenLieu,
            decimal soLuongTon,
            string donVi,
            DateTime? hanSuDung,
            string statusText,
            string statusBackground,
            string statusColor)
        {
            return $@"
                <div class='item-card' style='background:#2A2A3E;border:1px solid #374151;border-radius:14px;padding:14px 14px 12px;margin:0 0 12px;'>
                    <div style='color:#F9FAFB;font-size:15px;font-weight:700;line-height:1.35;margin-bottom:12px;'>{Encode(tenNguyenLieu)}</div>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                        <tr>
                            <td class='item-cell' width='50%' style='padding:0 8px 10px 0;vertical-align:top;'>
                                <div style='color:#9CA3AF;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:.3px;'>Tồn theo HSD</div>
                                <div style='color:#FFFFFF;font-size:16px;font-weight:800;margin-top:3px;'>{FormatQuantity(soLuongTon)} <span style='color:#CBD5E1;font-size:13px;font-weight:600;'>{Encode(donVi)}</span></div>
                            </td>
                            <td class='item-cell' width='50%' style='padding:0 0 10px 8px;vertical-align:top;'>
                                <div style='color:#9CA3AF;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:.3px;'>Hạn sử dụng</div>
                                <div style='color:#FFFFFF;font-size:16px;font-weight:800;margin-top:3px;'>{hanSuDung:dd/MM/yyyy}</div>
                            </td>
                        </tr>
                        <tr>
                            <td colspan='2' style='padding-top:2px;'>
                                <span style='display:inline-block;background:{statusBackground};color:{statusColor};padding:6px 12px;border-radius:999px;font-weight:800;font-size:13px;line-height:1.2;'>{Encode(statusText)}</span>
                            </td>
                        </tr>
                    </table>
                </div>";
        }

        var nearExpiryCards = "";
        foreach (var item in nearExpiryItems)
        {
            var daysLeft = item.HanSuDung.HasValue ? (item.HanSuDung.Value.Date - DateTime.Today).Days : 0;
            var urgencyColor = daysLeft <= 2 ? "#FF6B6B" : "#FFB347";
            nearExpiryCards += BuildItemCard(
                item.TenNguyenLieu,
                item.SoLuongTon,
                item.DonVi,
                item.HanSuDung,
                $"Còn {daysLeft} ngày",
                urgencyColor,
                "#1E1E2E");
        }

        var expiredCards = "";
        foreach (var item in expiredItems)
        {
            var daysExpired = item.HanSuDung.HasValue ? (DateTime.Today - item.HanSuDung.Value.Date).Days : 0;
            expiredCards += BuildItemCard(
                item.TenNguyenLieu,
                item.SoLuongTon,
                item.DonVi,
                item.HanSuDung,
                $"Hết hạn {daysExpired} ngày",
                "#FF4444",
                "#FFFFFF");
        }

        var nearExpirySection = nearExpiryItems.Count > 0 ? $@"
            <div style='margin-bottom:24px;'>
                <h2 class='section-title' style='color:#FFB347;font-size:18px;line-height:1.25;margin:0 0 12px;'>Nguyên liệu SẮP HẾT HẠN ({nearExpiryItems.Count} lô/HSD)</h2>
                {nearExpiryCards}
            </div>" : "";

        var expiredSection = expiredItems.Count > 0 ? $@"
            <div style='margin-bottom:24px;'>
                <h2 class='section-title' style='color:#FF4444;font-size:18px;line-height:1.25;margin:0 0 12px;'>Nguyên liệu ĐÃ HẾT HẠN ({expiredItems.Count} lô/HSD)</h2>
                {expiredCards}
            </div>" : "";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        @media only screen and (max-width: 520px) {{
            .container {{ width:100% !important;border-radius:0 !important;border-left:0 !important;border-right:0 !important; }}
            .header {{ padding:24px 18px !important; }}
            .title {{ font-size:21px !important;line-height:1.25 !important; }}
            .content {{ padding:18px 14px !important; }}
            .summary-wrap {{ padding:8px 14px !important; }}
            .summary-cell {{ display:block !important;width:100% !important;border-left:0 !important;border-top:1px solid #374151 !important;padding:12px 0 !important; }}
            .summary-cell:first-child {{ border-top:0 !important; }}
            .item-card {{ padding:14px !important;border-radius:12px !important; }}
            .item-cell {{ display:block !important;width:100% !important;padding:0 0 10px 0 !important; }}
            .section-title {{ font-size:17px !important; }}
            .footer {{ padding:16px 18px !important; }}
        }}
    </style>
</head>
<body style='margin:0;padding:0;background:#111827;font-family:Segoe UI,Arial,sans-serif;-webkit-text-size-adjust:100%;'>
    <div class='container' style='max-width:640px;width:100%;margin:0 auto;background:#1E1E2E;border-radius:14px;overflow:hidden;border:1px solid #374151;'>
        <!-- Header -->
        <div class='header' style='background:linear-gradient(135deg,#E85D04,#F48C06);padding:28px 28px;text-align:center;'>
            <h1 class='title' style='margin:0;color:white;font-size:23px;line-height:1.2;'>Pizza Warehouse System</h1>
            <p style='margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:14px;'>Báo cáo Hạn Sử Dụng Nguyên Liệu</p>
        </div>

        <!-- Summary -->
        <div class='content' style='padding:22px 24px;'>
            <div class='summary-wrap' style='background:#2A2A3E;border:1px solid #374151;border-radius:14px;padding:12px 16px;margin-bottom:22px;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                    <tr>
                        <td class='summary-cell' width='34%' style='text-align:center;padding:4px 10px;vertical-align:middle;'>
                            <div style='color:#9CA3AF;font-size:12px;font-weight:600;'>Ngày kiểm tra</div>
                            <div style='color:white;font-size:16px;font-weight:800;margin-top:5px;line-height:1.35;'>{DateTime.Now:dd/MM/yyyy}<br>{DateTime.Now:HH:mm}</div>
                        </td>
                        <td class='summary-cell' width='33%' style='text-align:center;padding:4px 10px;vertical-align:middle;border-left:1px solid #374151;'>
                            <div style='color:#9CA3AF;font-size:12px;font-weight:600;'>Sắp hết hạn</div>
                            <div style='color:#FFB347;font-size:26px;font-weight:900;margin-top:4px;'>{nearExpiryItems.Count}</div>
                        </td>
                        <td class='summary-cell' width='33%' style='text-align:center;padding:4px 10px;vertical-align:middle;border-left:1px solid #374151;'>
                            <div style='color:#9CA3AF;font-size:12px;font-weight:600;'>Đã hết hạn</div>
                            <div style='color:#FF4444;font-size:26px;font-weight:900;margin-top:4px;'>{expiredItems.Count}</div>
                        </td>
                    </tr>
                </table>
            </div>

            {expiredSection}
            {nearExpirySection}
        </div>

        <!-- Footer -->
        <div class='footer' style='background:#111827;padding:18px 24px;text-align:center;border-top:1px solid #374151;'>
            <p style='margin:0;color:#8B95A7;font-size:12px;line-height:1.5;'>
                Đây là email tự động từ hệ thống quản lý kho nguyên liệu Pizza.<br/>
                Vui lòng kiểm tra và xử lý kịp thời các nguyên liệu trên.
            </p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var parts = email.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var addr = new MailAddress(trimmed);
                if (addr.Address != trimmed)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
