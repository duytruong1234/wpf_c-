using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

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
    private readonly IDatabaseService _databaseService;

    private string _selectedMenuItem = "BaoCaoThongKe";
    private string _tenNguoiDung = string.Empty;
    private NguyenLieu? _selectedNguyenLieu;
    private DateTime? _ngayBatDau;
    private DateTime? _ngayKetThuc;
    private bool _isLoading;

    // Thống kê kho hàng
    private int _soLuongTonKho;
    private int _soLuongTonKhoThap;
    private int _soLuongSapHetHan;
    private int _soLuongHetHan;
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
        set => SetProperty(ref _ngayBatDau, value);
    }

    public DateTime? NgayKetThuc
    {
        get => _ngayKetThuc;
        set => SetProperty(ref _ngayKetThuc, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
    public int TotalNguyenLieu => Math.Max(SoLuongTonKho, 10);

    public ObservableCollection<NguyenLieu> NguyenLieus { get; } = [];
    public ObservableCollection<TopPizzaItem> TopPizzas { get; } = [];
    public ObservableCollection<DonHang> RecentDonHangs { get; } = [];

    // Lệnh
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

    // Sự kiện cập nhật biểu đồ
    public event Action? OnDataLoaded;
    public event Action? OnLogout;

    public DashboardViewModel()
    {
        try
        {
            _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }
        
        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        // ⚡ AsyncRelayCommand thay vì async void
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());

        // Tải tên người dùng hiện tại
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        TenNguoiDung = currentUser?.NhanVien?.HoTen ?? currentUser?.Username ?? "Người dùng";

        // ⚡ SafeInitializeAsync thay vì fire-and-forget
        SafeInitializeAsync(LoadDataAsync);
    }

    public DashboardViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        
        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());

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
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // Đợt 1: Chạy tất cả truy vấn độc lập song song
            var tonKhoTask = _databaseService.GetTotalTonKhoCountAsync();
            var lowStockTask = _databaseService.GetLowStockCountAsync(20);
            var nearExpiryTask = _databaseService.GetNearExpiryCountAsync(7);
            var expiredTask = _databaseService.GetExpiredCountAsync();
            var doanhThuTodayTask = _databaseService.GetDoanhThuAsync(today, today);
            var tongDonTodayTask = _databaseService.GetTotalDonHangCountAsync(today, today);
            var doanhThuMonthTask = _databaseService.GetDoanhThuAsync(firstDayOfMonth, today);
            var tongDonMonthTask = _databaseService.GetTotalDonHangCountAsync(firstDayOfMonth, today);
            var loiNhuanTask = _databaseService.GetTotalLoiNhuanAsync(firstDayOfMonth, today);
            var chiPhiTask = _databaseService.GetChiPhiNguyenLieuAsync(firstDayOfMonth, today);
            var topPizzasTask = _databaseService.GetTopPizzasAsync(firstDayOfMonth, today, 5);
            var recentOrdersTask = _databaseService.GetRecentDonHangsAsync(8);

            // Doanh thu 7 ngày gần nhất
            var dailyRevenueTasks = new List<Task<decimal>>();
            var dailyLabels = new List<string>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                dailyRevenueTasks.Add(_databaseService.GetDoanhThuAsync(date, date));
                dailyLabels.Add(date.ToString("dd/MM"));
            }

            await Task.WhenAll(
                tonKhoTask, lowStockTask, nearExpiryTask, expiredTask,
                doanhThuTodayTask, tongDonTodayTask, doanhThuMonthTask, tongDonMonthTask,
                loiNhuanTask, chiPhiTask, topPizzasTask, recentOrdersTask);

            await Task.WhenAll(dailyRevenueTasks);

            // Gán kết quả
            SoLuongTonKho = tonKhoTask.Result;
            SoLuongTonKhoThap = lowStockTask.Result;
            SoLuongSapHetHan = nearExpiryTask.Result;
            SoLuongHetHan = expiredTask.Result;
            DoanhThuHomNay = doanhThuTodayTask.Result;
            TongDonHomNay = tongDonTodayTask.Result;
            DoanhThuThang = doanhThuMonthTask.Result;
            TongDonThang = tongDonMonthTask.Result;
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
}
