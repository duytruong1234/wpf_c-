using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

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

public class DashboardViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private string _selectedMenuItem = "BaoCaoThongKe";
    private string _tenNguoiDung = string.Empty;
    private NguyenLieu? _selectedNguyenLieu;
    private DateTime? _ngayBatDau;
    private DateTime? _ngayKetThuc;
    private bool _isLoading;

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

    // Popup trạng thái nguyên liệu
    private bool _isStatusPopupOpen;
    private string _statusPopupTitle = string.Empty;
    private string _statusPopupColor = "#5B6AFF";

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

    public DateTime? NgayBatDau
    {
        get => _ngayBatDau;
        set 
        {
            if (SetProperty(ref _ngayBatDau, value))
            {
                SafeInitializeAsync(LoadDataAsync);
            }
        }
    }

    public DateTime? NgayKetThuc
    {
        get => _ngayKetThuc;
        set 
        {
            if (SetProperty(ref _ngayKetThuc, value))
            {
                SafeInitializeAsync(LoadDataAsync);
            }
        }
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

    // Lệnh
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowStatusDetailCommand { get; }
    public ICommand CloseStatusPopupCommand { get; }

    // Sự kiện cập nhật biểu đồ
    public event Action? OnDataLoaded;
    public event Action? OnLogout;

    public DashboardViewModel()
    {
        try
        {
            _databaseService = new DatabaseService();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }
        
        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        ShowStatusDetailCommand = new AsyncRelayCommand(async p => await LoadStatusDetailAsync(p));
        CloseStatusPopupCommand = new RelayCommand(_ => IsStatusPopupOpen = false);

        // Tải tên người dùng hiện tại
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        TenNguoiDung = currentUser?.NhanVien?.HoTen ?? currentUser?.Username ?? "Người dùng";

        // Khởi tạo ngày mặc định
        _ngayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ngayKetThuc = DateTime.Today;

        // ⚡ SafeInitializeAsync thay vì fire-and-forget
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
            var fromDate = NgayBatDau ?? new DateTime(today.Year, today.Month, 1);
            var toDate = NgayKetThuc ?? today;
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

            // Thông báo cập nhật biểu đồ
            OnDataLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
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
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi });
                break;

            case "TonThap":
                StatusPopupTitle = "Nguyên liệu tồn kho thấp";
                StatusPopupColor = "#F59E0B";
                var lowItems = await _databaseService.GetLowStockItemsAsync(20);
                foreach (var item in lowItems)
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi });
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
                    StatusDetailItems.Add(new StockDetailItem { TenNguyenLieu = item.TenNguyenLieu, SoLuongTon = item.SoLuongTon, DonVi = item.DonVi });
                break;
        }

        IsStatusPopupOpen = true;
    }
}
