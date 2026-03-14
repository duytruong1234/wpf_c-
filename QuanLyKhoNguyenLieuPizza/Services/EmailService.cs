using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class EmailService : IEmailService
{
    // SMTP Configuration
    private readonly string _smtpHost = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _senderEmail = "truongnguyen1714@gmail.com"; 
    private readonly string _senderPassword = "iphnvwmsugwdybck"; 
    private readonly string _senderDisplayName = "Pizza Warehouse System";
    private readonly bool _enableSsl = true;

    private static readonly Dictionary<string, (string OTP, DateTime Expiry)> _otpStorage = new();

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
        
        System.Diagnostics.Debug.WriteLine($"=== OTP Stored ===");
        System.Diagnostics.Debug.WriteLine($"Email (original): {email}");
        System.Diagnostics.Debug.WriteLine($"Key (lowercase): {key}");
        System.Diagnostics.Debug.WriteLine($"OTP Code: {otpCode}");
        System.Diagnostics.Debug.WriteLine($"Expiry: {DateTime.Now.AddMinutes(5)}");
        System.Diagnostics.Debug.WriteLine($"Total OTPs in storage: {_otpStorage.Count}");

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
                System.Diagnostics.Debug.WriteLine("=== EMAIL SERVICE WARNING ===");
                System.Diagnostics.Debug.WriteLine("Email sender credentials not configured!");
                System.Diagnostics.Debug.WriteLine($"Would send OTP to: {toEmail}");
                
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
            
            System.Diagnostics.Debug.WriteLine($"G\u1eedi email th\u00e0nh c\u00f4ng \u0111\u1ebfn: {toEmail}");
            return (true, "M\u00e3 OTP \u0111\u00e3 \u0111\u01b0\u1ee3c g\u1eedi \u0111\u1ebfn email c\u1ee7a b\u1ea1n!");
        }
        catch (SmtpException smtpEx)
        {
            System.Diagnostics.Debug.WriteLine($"L\u1ed7i SMTP: {smtpEx.Message}");
            System.Diagnostics.Debug.WriteLine($"M\u00e3 tr\u1ea1ng th\u00e1i: {smtpEx.StatusCode}");
            return (false, $"L\u1ed7i g\u1eedi email: {smtpEx.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"L\u1ed7i email: {ex.Message}");
            return (false, $"Kh\u00f4ng th\u1ec3 g\u1eedi email: {ex.Message}");
        }
    }

    public bool VerifyOTP(string email, string otpCode)
    {
        var key = email.ToLower();
        
        System.Diagnostics.Debug.WriteLine($"=== VerifyOTP Called ===");
        System.Diagnostics.Debug.WriteLine($"Email (original): {email}");
        System.Diagnostics.Debug.WriteLine($"Key (lowercase): {key}");
        System.Diagnostics.Debug.WriteLine($"Input OTP: {otpCode}");
        System.Diagnostics.Debug.WriteLine($"Total OTPs in storage: {_otpStorage.Count}");
        
        if (!_otpStorage.TryGetValue(key, out var stored))
        {
            System.Diagnostics.Debug.WriteLine($"FAILED: No OTP found for key '{key}'");
            System.Diagnostics.Debug.WriteLine("Available keys:");
            foreach (var k in _otpStorage.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"  - '{k}'");
            }
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"Found OTP: {stored.OTP}");
        System.Diagnostics.Debug.WriteLine($"Expiry: {stored.Expiry}");
        System.Diagnostics.Debug.WriteLine($"Current Time: {DateTime.Now}");

        if (DateTime.Now > stored.Expiry)
        {
            System.Diagnostics.Debug.WriteLine("FAILED: OTP expired");
            _otpStorage.Remove(key);
            return false;
        }

        if (stored.OTP == otpCode)
        {
            System.Diagnostics.Debug.WriteLine("SUCCESS: OTP matched!");
            _otpStorage.Remove(key);
            return true;
        }

        System.Diagnostics.Debug.WriteLine($"FAILED: OTP mismatch. Expected '{stored.OTP}', got '{otpCode}'");
        return false;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

