using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Dịch vụ điều phối kiểm tra và gửi email cảnh báo nguyên liệu sắp hết hạn / đã hết hạn.
/// Đảm bảo chỉ gửi tối đa 1 email/ngày để tránh spam.
/// </summary>
public class ExpiryNotificationService
{
    private readonly DatabaseService _databaseService;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Lưu ngày gửi email cuối cùng (tránh gửi nhiều lần trong ngày)
    /// </summary>
    private static DateTime? _lastSentDate;

    public ExpiryNotificationService(DatabaseService databaseService, IEmailService emailService)
    {
        _databaseService = databaseService;
        _emailService = emailService;
    }

    /// <summary>
    /// Kiểm tra xem đã gửi email thông báo hôm nay chưa
    /// </summary>
    public bool HasSentToday()
    {
        return _lastSentDate.HasValue && _lastSentDate.Value.Date == DateTime.Today;
    }

    /// <summary>
    /// Kiểm tra nguyên liệu sắp/đã hết hạn và gửi email cảnh báo.
    /// Trả về (Success, Message, ItemCount).
    /// </summary>
    /// <param name="toEmail">Email người nhận (quản lý)</param>
    /// <param name="days">Số ngày cảnh báo trước HSD (mặc định 7)</param>
    /// <param name="forceResend">Bỏ qua kiểm tra đã gửi hôm nay</param>
    public async Task<(bool Success, string Message, int ItemCount)> CheckAndNotifyAsync(
        string toEmail, int days = 7, bool forceResend = false)
    {
        try
        {
            // Kiểm tra đã gửi hôm nay chưa (nếu không forceResend)
            if (!forceResend && HasSentToday())
            {
                return (true, "Email cảnh báo HSD đã được gửi hôm nay.", 0);
            }

            // Lấy dữ liệu nguyên liệu sắp hết hạn và đã hết hạn
            var nearExpiryItems = await _databaseService.GetNearExpiryItemsAsync(days);
            var expiredItems = await _databaseService.GetExpiredItemsAsync();

            int totalItems = nearExpiryItems.Count + expiredItems.Count;

            if (totalItems == 0)
            {
                return (true, "Không có nguyên liệu nào sắp hết hạn hoặc đã hết hạn.", 0);
            }

            // Lấy danh sách email của nhân viên thuộc bộ phận kho, bếp và quản lý
            var emailList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(toEmail))
            {
                foreach (var em in toEmail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    emailList.Add(em.Trim());
                }
            }

            try
            {
                var employees = await _databaseService.GetAllNhanViensFullAsync(trangThai: true);
                var rolesToNotify = new[] { "kho", "bếp", "bep", "quản lý", "quan ly" };
                foreach (var emp in employees)
                {
                    if (emp.ChucVu != null && !string.IsNullOrWhiteSpace(emp.Email))
                    {
                        var roleName = emp.ChucVu.TenChucVu.ToLower().Trim();
                        if (rolesToNotify.Any(r => roleName.Contains(r)))
                        {
                            emailList.Add(emp.Email.Trim());
                        }
                    }
                }
            }
            catch { }

            string combinedEmails = string.Join(",", emailList);

            if (string.IsNullOrWhiteSpace(combinedEmails))
            {
                return (false, "Không tìm thấy email hợp lệ để gửi cảnh báo.", 0);
            }

            // Gửi email
            var result = await _emailService.SendExpiryNotificationAsync(
                combinedEmails, nearExpiryItems, expiredItems);

            if (result.Success)
            {
                _lastSentDate = DateTime.Today;
                return (true,
                    $"Đã gửi email cảnh báo đến {combinedEmails}.\n" +
                    $"• {nearExpiryItems.Count} nguyên liệu sắp hết hạn\n" +
                    $"• {expiredItems.Count} nguyên liệu đã hết hạn",
                    totalItems);
            }
            else
            {
                return (false, $"Lỗi gửi email: {result.Message}", 0);
            }
        }
        catch (Exception ex) {
            return (false, $"Lỗi: {ex.Message}", 0);
        }
    }
}
