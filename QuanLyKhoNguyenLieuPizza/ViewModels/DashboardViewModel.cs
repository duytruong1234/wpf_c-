using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

// Item hiển thị trong popup chi tiết trạng thái
public class StockDetailItem : BaseViewModel
{
    public string TenNguyenLieu { get; set; } = string.Empty;
    public decimal SoLuongTon { get; set; }
    public string DonVi { get; set; } = string.Empty;
    public DateTime? HanSuDung { get; set; }
    public bool HasExpiry => HanSuDung.HasValue;
    public string HanSuDungText => HanSuDung?.ToString("dd/MM/yyyy") ?? "—";
}

public class TopPizzaItem : BaseViewModel
{
    public string TenPizza { get; set; } = string.Empty;
    public string KichThuoc { get; set; } = "M";
    public int SoLuongBan { get; set; }
    public decimal DoanhThu { get; set; }
    public int Rank { get; set; }
}

// Item đối soát tiêu hao nguyên liệu
public class DoiSoatItem : BaseViewModel
{
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public decimal LuongXuatCheBien { get; set; }
    public decimal LuongTieuHaoBanHang { get; set; }
    public decimal ChenhLech => LuongXuatCheBien - LuongTieuHaoBanHang;
    public double TyLeChenhLech => LuongXuatCheBien > 0
        ? (double)((LuongXuatCheBien - LuongTieuHaoBanHang) / LuongXuatCheBien * 100m)
        : 0;
    public string TrangThaiChenhLech => ChenhLech == 0 ? "Cân bằng"
        : ChenhLech > 0 ? "Nhập nhiều hơn" : "Xuất nhiều hơn";
    public string MauChenhLech => Math.Abs(TyLeChenhLech) < 5 ? "#10B981"
        : Math.Abs(TyLeChenhLech) < 10 ? "#F59E0B" : "#EF4444";
}

public class DashboardViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly ExpiryNotificationService? _expiryNotificationService;

    private string _selectedMenuItem = "BaoCaoThongKe";
    private string _tenNguoiDung = string.Empty;
    private NguyenLieu? _selectedNguyenLieu;
    private DateTime? _ngayBatDau;
    private DateTime? _ngayKetThuc;
    private bool _isLoading;
    private DateTime _ngayDoiSoat = DateTime.Today;

    // Thống kê kho hàng
    private int _tongSoNguyenLieu;
    private int _soLuongTonKho;
    private int _soLuongTonKhoThap;
    private int _soLuongSapHetHan;
    private int _soLuongHetHan;
    private int _soLuongHetHang;
    private int _soLuongNhap;
    private int _soLuongXuat;
    private int _tonKho;

    // Thống kê bán hàng
    private decimal _doanhThuHomNay;
    private decimal _doanhThuThang;
    private int _tongDonHomNay;
    private int _tongDonThang;
    private decimal _loiNhuanThang;
    private decimal _chiPhiNguyenLieuThang;

    // Đối soát tiêu hao
    private decimal _tongXuatCheBien;
    private decimal _tongTieuHaoBanHang;
    private decimal _tongChenhLech;

    // Popup trạng thái nguyên liệu
    private bool _isStatusPopupOpen;
    private string _statusPopupTitle = string.Empty;
    private string _statusPopupColor = "#5B6AFF";

    // Email cảnh báo HSD
    private bool _isSendingEmail;
    private string _emailStatusMessage = string.Empty;
    private bool _canSendExpiryEmail;

    // Dữ liệu biểu đồ
    private double[] _dailyRevenueValues = [];
    private string[] _dailyRevenueLabels = [];
    private List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)> _topPizzaData = [];

    public string SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetProperty(ref _selectedMenuItem, value);
    }

    public string TenNguoiDung
    {
        get => _tenNguoiDung;
        set => SetProperty(ref _tenNguoiDung, value);
    }

    public NguyenLieu? SelectedNguyenLieu
    {
        get => _selectedNguyenLieu;
        set => SetProperty(ref _selectedNguyenLieu, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public int TongSoNguyenLieu
    {
        get => _tongSoNguyenLieu;
        set => SetProperty(ref _tongSoNguyenLieu, value);
    }

    public int SoLuongTonKho
    {
        get => _soLuongTonKho;
        set => SetProperty(ref _soLuongTonKho, value);
    }

    public int SoLuongTonKhoThap
    {
        get => _soLuongTonKhoThap;
        set => SetProperty(ref _soLuongTonKhoThap, value);
    }

    public int SoLuongSapHetHan
    {
        get => _soLuongSapHetHan;
        set => SetProperty(ref _soLuongSapHetHan, value);
    }

    public int SoLuongHetHan
    {
        get => _soLuongHetHan;
        set => SetProperty(ref _soLuongHetHan, value);
    }

    public int SoLuongHetHang
    {
        get => _soLuongHetHang;
        set => SetProperty(ref _soLuongHetHang, value);
    }

    public int SoLuongNhap
    {
        get => _soLuongNhap;
        set => SetProperty(ref _soLuongNhap, value);
    }

    public int SoLuongXuat
    {
        get => _soLuongXuat;
        set => SetProperty(ref _soLuongXuat, value);
    }

    public int TonKho
    {
        get => _tonKho;
        set => SetProperty(ref _tonKho, value);
    }

    // Thuộc tính bán hàng
    public decimal DoanhThuHomNay
    {
        get => _doanhThuHomNay;
        set => SetProperty(ref _doanhThuHomNay, value);
    }

    public decimal DoanhThuThang
    {
        get => _doanhThuThang;
        set => SetProperty(ref _doanhThuThang, value);
    }

    public int TongDonHomNay
    {
        get => _tongDonHomNay;
        set => SetProperty(ref _tongDonHomNay, value);
    }

    public int TongDonThang
    {
        get => _tongDonThang;
        set => SetProperty(ref _tongDonThang, value);
    }

    public decimal LoiNhuanThang
    {
        get => _loiNhuanThang;
        set => SetProperty(ref _loiNhuanThang, value);
    }

    public decimal ChiPhiNguyenLieuThang
    {
        get => _chiPhiNguyenLieuThang;
        set => SetProperty(ref _chiPhiNguyenLieuThang, value);
    }

    // Thuộc tính dữ liệu biểu đồ
    public double[] DailyRevenueValues
    {
        get => _dailyRevenueValues;
        set => SetProperty(ref _dailyRevenueValues, value);
    }

    public string[] DailyRevenueLabels
    {
        get => _dailyRevenueLabels;
        set => SetProperty(ref _dailyRevenueLabels, value);
    }

    public List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)> TopPizzaData
    {
        get => _topPizzaData;
        set => SetProperty(ref _topPizzaData, value);
    }

    // Tổng nguyên liệu cho tính toán biểu đồ (mặc định là 10 nếu bằng 0)
    public int TotalNguyenLieu => Math.Max(TongSoNguyenLieu, 10);

    public ObservableCollection<NguyenLieu> NguyenLieus { get; } = [];
    public ObservableCollection<TopPizzaItem> TopPizzas { get; } = [];
    public ObservableCollection<DonHang> RecentDonHangs { get; } = [];
    public ObservableCollection<DoiSoatItem> DoiSoatItems { get; } = [];

    public decimal TongXuatCheBien
    {
        get => _tongXuatCheBien;
        set => SetProperty(ref _tongXuatCheBien, value);
    }

    public decimal TongTieuHaoBanHang
    {
        get => _tongTieuHaoBanHang;
        set => SetProperty(ref _tongTieuHaoBanHang, value);
    }

    public decimal TongChenhLech
    {
        get => _tongChenhLech;
        set => SetProperty(ref _tongChenhLech, value);
    }
    public ObservableCollection<StockDetailItem> StatusDetailItems { get; } = [];

    public bool IsStatusPopupOpen
    {
        get => _isStatusPopupOpen;
        set => SetProperty(ref _isStatusPopupOpen, value);
    }

    public string StatusPopupTitle
    {
        get => _statusPopupTitle;
        set => SetProperty(ref _statusPopupTitle, value);
    }

    public string StatusPopupColor
    {
        get => _statusPopupColor;
        set => SetProperty(ref _statusPopupColor, value);
    }

    // Email cảnh báo HSD properties
    public bool IsSendingEmail
    {
        get => _isSendingEmail;
        set => SetProperty(ref _isSendingEmail, value);
    }

    public string EmailStatusMessage
    {
        get => _emailStatusMessage;
        set => SetProperty(ref _emailStatusMessage, value);
    }

    public bool CanSendExpiryEmail
    {
        get => _canSendExpiryEmail;
        set => SetProperty(ref _canSendExpiryEmail, value);
    }

    // Ngày đối soát
    public DateTime NgayDoiSoat
    {
        get => _ngayDoiSoat;
        set
        {
            if (SetProperty(ref _ngayDoiSoat, value))
            {
                SafeInitializeAsync(LoadDoiSoatDataAsync);
            }
        }
    }

    // Lệnh
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowStatusDetailCommand { get; }
    public ICommand CloseStatusPopupCommand { get; }
    public ICommand SendExpiryEmailCommand { get; }

    // Sự kiện cập nhật biểu đồ
    public event Action? OnDataLoaded;
    public event Action? OnLogout;

    public DashboardViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        ShowStatusDetailCommand = new AsyncRelayCommand(async p => await LoadStatusDetailAsync(p));
        CloseStatusPopupCommand = new RelayCommand(_ => IsStatusPopupOpen = false);
        SendExpiryEmailCommand = new AsyncRelayCommand(async _ => await SendExpiryEmailAsync());

        // Khởi tạo ExpiryNotificationService
        try
        {
            var emailService = App.Services.GetService<IEmailService>();
            if (emailService != null)
                _expiryNotificationService = new ExpiryNotificationService(_databaseService, emailService);
        }
        catch { /* EmailService chưa được đăng ký */ }

        // Chỉ quản lý mới được gửi email cảnh báo
        var chucVu = CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVu?.TenChucVu?.ToLower() ?? "";
        CanSendExpiryEmail = chucVu.Contains("quản lý") || chucVu.Contains("quan ly");

        // Tải tên người dùng hiện tại
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        TenNguoiDung = currentUser?.NhanVien?.HoTen ?? currentUser?.Username ?? "Người dùng";

        // Khởi tạo ngày mặc định
        _ngayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ngayKetThuc = DateTime.Today;

        // SafeInitializeAsync thay vì fire-and-forget
        SafeInitializeAsync(LoadDataAsync);
    }

    public DashboardViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        ShowStatusDetailCommand = new AsyncRelayCommand(async p => await LoadStatusDetailAsync(p));
        CloseStatusPopupCommand = new RelayCommand(_ => IsStatusPopupOpen = false);
        SendExpiryEmailCommand = new AsyncRelayCommand(async _ => await SendExpiryEmailAsync());

        // Khởi tạo ngày mặc định
        _ngayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ngayKetThuc = DateTime.Today;

        SafeInitializeAsync(LoadDataAsync);
    }

    private void ExecuteNavigate(object? parameter)
    {
        if (parameter is string menuItem)
        {
            SelectedMenuItem = menuItem;
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
            var today = DateTime.Today;
            var fromDate = _ngayBatDau ?? new DateTime(today.Year, today.Month, 1);
            var toDate = _ngayKetThuc ?? today;
            var toDateQuery = toDate.Date.AddDays(1).AddTicks(-1);

            // Đợt 1: Chạy tất cả truy vấn độc lập song song
            var tongNguyenLieuTask = _databaseService.GetTotalNguyenLieuCountAsync();
            var tonKhoTask = _databaseService.GetTotalTonKhoCountAsync();
            var lowStockTask = _databaseService.GetLowStockCountAsync(20);
            var nearExpiryTask = _databaseService.GetNearExpiryCountAsync(7);
            var expiredTask = _databaseService.GetExpiredCountAsync();
            var doanhThuTodayTask = _databaseService.GetDoanhThuBanHangAsync(today, today.Date.AddDays(1).AddTicks(-1));
            var tongDonTodayTask = _databaseService.GetTotalPhieuBanCountAsync(today, today.Date.AddDays(1).AddTicks(-1));
            // Doanh thu theo lọc
            var doanhThuMonthTask = _databaseService.GetDoanhThuBanHangAsync(fromDate, toDateQuery);
            var tongDonMonthTask = _databaseService.GetTotalPhieuBanCountAsync(fromDate, toDateQuery);
            var loiNhuanTask = _databaseService.GetTotalLoiNhuanAsync(fromDate, toDateQuery);
            var chiPhiTask = _databaseService.GetChiPhiNguyenLieuAsync(fromDate, toDateQuery);
            var topPizzasTask = _databaseService.GetTopPizzasAsync(fromDate, toDateQuery, 5);
            var recentOrdersTask = _databaseService.GetRecentDonHangsAsync(8);

            // Doanh thu 7 ngày gần nhất
            var dailyRevenueTasks = new List<Task<decimal>>();
            var dailyLabels = new List<string>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                dailyRevenueTasks.Add(_databaseService.GetDoanhThuBanHangAsync(date, date));
                dailyLabels.Add(date.ToString("dd/MM"));
            }

            await Task.WhenAll(
                tongNguyenLieuTask, tonKhoTask, lowStockTask, nearExpiryTask, expiredTask,
                doanhThuTodayTask, tongDonTodayTask, doanhThuMonthTask, tongDonMonthTask,
                loiNhuanTask, chiPhiTask, topPizzasTask, recentOrdersTask);

            await Task.WhenAll(dailyRevenueTasks);

            // Gán kết quả
            TongSoNguyenLieu = tongNguyenLieuTask.Result;
            SoLuongTonKho = tonKhoTask.Result;
            SoLuongTonKhoThap = lowStockTask.Result;
            SoLuongSapHetHan = nearExpiryTask.Result;
            SoLuongHetHan = expiredTask.Result;

            // Tính số lượng hết hàng
            SoLuongHetHang = Math.Max(0, TongSoNguyenLieu - SoLuongTonKho);

            // Doanh thu theo bộ lọc
            var monthRevenue = doanhThuMonthTask.Result;
            var monthOrders = tongDonMonthTask.Result;
            DoanhThuHomNay = doanhThuTodayTask.Result;
            TongDonHomNay = tongDonTodayTask.Result;
            DoanhThuThang = monthRevenue;
            TongDonThang = monthOrders;
            LoiNhuanThang = loiNhuanTask.Result;
            ChiPhiNguyenLieuThang = chiPhiTask.Result;

            // Lưu dữ liệu biểu đồ
            DailyRevenueValues = dailyRevenueTasks.Select(t => (double)(t.Result / 1000m)).ToArray();
            DailyRevenueLabels = dailyLabels.ToArray();

            // Cập nhật các danh sách
            TopPizzas.Clear();
            int rank = 1;
            foreach (var (tenPizza, kichThuoc, soLuongBan, doanhThu) in topPizzasTask.Result)
            {
                TopPizzas.Add(new TopPizzaItem
                {
                    TenPizza = tenPizza,
                    KichThuoc = kichThuoc,
                    SoLuongBan = soLuongBan,
                    DoanhThu = doanhThu,
                    Rank = rank++
                });
            }

            TopPizzaData = topPizzasTask.Result;

            RecentDonHangs.Clear();
            foreach (var order in recentOrdersTask.Result)
            {
                RecentDonHangs.Add(order);
            }

            // === Đối soát tiêu hao NL ===
            await LoadDoiSoatDataAsync();

            // Thông báo cập nhật biểu đồ
            OnDataLoaded?.Invoke();
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Tải dữ liệu đối soát tiêu hao theo ngày đã chọn (NgayDoiSoat)
    /// </summary>
    private async Task LoadDoiSoatDataAsync()
    {
        try
        {
            var doiSoatData = await _databaseService.GetDoiSoatTieuHaoAsync(_ngayDoiSoat);
            DoiSoatItems.Clear();
            decimal sumXuat = 0, sumTieuHao = 0;
            foreach (var (nlId, tenNL, donVi, xuatCB, tieuHao) in doiSoatData)
            {
                DoiSoatItems.Add(new DoiSoatItem
                {
                    TenNguyenLieu = tenNL,
                    DonVi = donVi,
                    LuongXuatCheBien = xuatCB,
                    LuongTieuHaoBanHang = tieuHao
                });
                sumXuat += xuatCB;
                sumTieuHao += tieuHao;
            }
            TongXuatCheBien = sumXuat;
            TongTieuHaoBanHang = sumTieuHao;
            TongChenhLech = sumXuat - sumTieuHao;
        }
        catch { }
    }

    private async Task LoadStatusDetailAsync(object? parameter)
    {
        if (parameter is not string statusType) return;

        StatusDetailItems.Clear();

        switch (statusType)
        {
            case "BinhThuong":
                StatusPopupTitle = "Nguyên liệu bình thường";
                StatusPopupColor = "#5B6AFF";
                var normalItems = await _databaseService.GetNormalStockItemsAsync(20);
                foreach (var item in normalItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi, HanSuDung = item.HanSuDung });
                break;

            case "TonThap":
                StatusPopupTitle = "Nguyên liệu tồn kho thấp";
                StatusPopupColor = "#F59E0B";
                var lowItems = await _databaseService.GetLowStockItemsAsync(20);
                foreach (var item in lowItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi, HanSuDung = item.HanSuDung });
                break;

            case "SapHetHan":
                StatusPopupTitle = "Nguyên liệu sắp hết hạn";
                StatusPopupColor = "#06B6D4";
                var nearExpiryItems = await _databaseService.GetNearExpiryItemsAsync(7);
                foreach (var item in nearExpiryItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi, HanSuDung = item.HanSuDung });
                break;

            case "HetHan":
                StatusPopupTitle = "Nguyên liệu đã hết hạn";
                StatusPopupColor = "#EF4444";
                var expiredItems = await _databaseService.GetExpiredItemsAsync();
                foreach (var item in expiredItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi, HanSuDung = item.HanSuDung });
                break;

            case "HetHang":
                StatusPopupTitle = "Nguyên liệu đã hết hàng";
                StatusPopupColor = "#64748B";
                var outOfStockItems = await _databaseService.GetOutOfStockItemsAsync();
                foreach (var item in outOfStockItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi, HanSuDung = item.HanSuDung });
                break;
        }

        IsStatusPopupOpen = true;
    }

    /// <summary>
    /// Gửi email cảnh báo nguyên liệu sắp/đã hết hạn đến email quản lý hiện tại
    /// </summary>
    private async Task SendExpiryEmailAsync()
    {
        if (_expiryNotificationService == null || IsSendingEmail) return;

        try
        {
            IsSendingEmail = true;
            EmailStatusMessage = "Đang gửi email...";

            // Lấy email quản lý hiện tại
            var currentEmail = CurrentUserSession.Instance.CurrentUser?.NhanVien?.Email;
            if (string.IsNullOrWhiteSpace(currentEmail))
            {
                EmailStatusMessage = "Tài khoản chưa có email!";
                return;
            }

            var result = await _expiryNotificationService.CheckAndNotifyAsync(currentEmail, days: 7, forceResend: true);

            if (result.Success)
            {
                EmailStatusMessage = result.ItemCount > 0
                    ? $"Đã gửi email ({result.ItemCount} nguyên liệu)"
                    : "Không có NL cần cảnh báo";

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(result.ItemCount > 0
                        ? $"Gửi thành công email báo cáo nguyên liệu sắp hết hạn đến {currentEmail}.\nTổng cộng: {result.ItemCount} nguyên liệu."
                        : "Không có nguyên liệu nào sắp hết hạn hoặc quá hạn cần báo cáo.",
                        "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
            }
            else
            {
                EmailStatusMessage = $"Lỗi: {result.Message}";
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Có lỗi xảy ra khi gửi email:\n{result.Message}",
                        "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }
        catch (Exception ex) {
            EmailStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsSendingEmail = false;
            // Tự động ẩn message sau 5 giây
            _ = Task.Delay(5000).ContinueWith(_ =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    EmailStatusMessage = string.Empty);
            });
        }
    }
}
