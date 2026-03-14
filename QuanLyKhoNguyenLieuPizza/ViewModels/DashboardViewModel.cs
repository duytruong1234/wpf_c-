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

    // Warehouse Statistics
    private int _soLuongTonKho;
    private int _soLuongTonKhoThap;
    private int _soLuongSapHetHan;
    private int _soLuongHetHan;
    private int _soLuongNhap;
    private int _soLuongXuat;
    private int _tonKho;

    // Sales Statistics
    private decimal _doanhThuHomNay;
    private decimal _doanhThuThang;
    private int _tongDonHomNay;
    private int _tongDonThang;
    private decimal _loiNhuanThang;
    private decimal _chiPhiNguyenLieuThang;

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

    // Sales properties
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

    // Total nguyen lieu for chart calculations (default to 10 if 0)
    public int TotalNguyenLieu => Math.Max(SoLuongTonKho, 10);

    public ObservableCollection<NguyenLieu> NguyenLieus { get; } = [];
    public ObservableCollection<TopPizzaItem> TopPizzas { get; } = [];
    public ObservableCollection<DonHang> RecentDonHangs { get; } = [];

    // Commands
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshCommand { get; }

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
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

        // Load current user name
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        TenNguoiDung = currentUser?.NhanVien?.HoTen ?? currentUser?.Username ?? "Ng??i dùng";

        _ = LoadDataAsync();
    }

    public DashboardViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        
        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

        _ = LoadDataAsync();
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
            // Load warehouse statistics
            SoLuongTonKho = await _databaseService.GetTotalTonKhoCountAsync();
            SoLuongTonKhoThap = await _databaseService.GetLowStockCountAsync(20);
            SoLuongSapHetHan = await _databaseService.GetNearExpiryCountAsync(7);
            SoLuongHetHan = await _databaseService.GetExpiredCountAsync();

            // Load sales statistics
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            DoanhThuHomNay = await _databaseService.GetDoanhThuAsync(today, today);
            TongDonHomNay = await _databaseService.GetTotalDonHangCountAsync(today, today);
            DoanhThuThang = await _databaseService.GetDoanhThuAsync(firstDayOfMonth, today);
            TongDonThang = await _databaseService.GetTotalDonHangCountAsync(firstDayOfMonth, today);
            LoiNhuanThang = await _databaseService.GetTotalLoiNhuanAsync(firstDayOfMonth, today);
            ChiPhiNguyenLieuThang = await _databaseService.GetChiPhiNguyenLieuAsync(firstDayOfMonth, today);

            // Load top pizzas this month
            var topPizzas = await _databaseService.GetTopPizzasAsync(firstDayOfMonth, today, 5);
            TopPizzas.Clear();
            int rank = 1;
            foreach (var (tenPizza, kichThuoc, soLuongBan, doanhThu) in topPizzas)
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

            // Load recent orders
            var recentOrders = await _databaseService.GetRecentDonHangsAsync(8);
            RecentDonHangs.Clear();
            foreach (var order in recentOrders)
            {
                RecentDonHangs.Add(order);
            }
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
