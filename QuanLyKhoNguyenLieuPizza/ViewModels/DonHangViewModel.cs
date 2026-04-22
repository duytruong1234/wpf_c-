using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class DonHangViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    private ObservableCollection<DonHang> _donHangs = [];
    private ObservableCollection<CT_DonHang> _chiTietDonHangs = [];
    private ObservableCollection<NhanVien> _nhanViens = [];
    private DonHang? _selectedDonHang;
    private NhanVien? _selectedNhanVien;
    private string _searchText = string.Empty;
    private DateTime? _tuNgay;
    private DateTime? _denNgay;
    private bool _isLoading;
    private bool _isDetailOpen;
    private bool _isDeleteDialogOpen;
    private DonHang? _deletingDonHang;
    private int _tongDonHang;
    private decimal _tongDoanhThu;
    private string _topNhanVienName = "—";
    private decimal _topNhanVienDoanhThu;

    public ObservableCollection<DonHang> DonHangs
    {
        get => _donHangs;
        set => SetProperty(ref _donHangs, value);
    }

    public ObservableCollection<CT_DonHang> ChiTietDonHangs
    {
        get => _chiTietDonHangs;
        set => SetProperty(ref _chiTietDonHangs, value);
    }

    public ObservableCollection<NhanVien> NhanViens
    {
        get => _nhanViens;
        set => SetProperty(ref _nhanViens, value);
    }

    public NhanVien? SelectedNhanVien
    {
        get => _selectedNhanVien;
        set
        {
            if (SetProperty(ref _selectedNhanVien, value))
                _ = LoadDataAsync();
        }
    }

    public DonHang? SelectedDonHang
    {
        get => _selectedDonHang;
        set
        {
            if (SetProperty(ref _selectedDonHang, value) && value != null)
            {
                _ = LoadChiTietAsync(value);
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime? TuNgay
    {
        get => _tuNgay;
        set
        {
            if (SetProperty(ref _tuNgay, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime? DenNgay
    {
        get => _denNgay;
        set
        {
            if (SetProperty(ref _denNgay, value))
                _ = LoadDataAsync();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsDetailOpen
    {
        get => _isDetailOpen;
        set => SetProperty(ref _isDetailOpen, value);
    }

    public bool IsDeleteDialogOpen
    {
        get => _isDeleteDialogOpen;
        set => SetProperty(ref _isDeleteDialogOpen, value);
    }

    public DonHang? DeletingDonHang
    {
        get => _deletingDonHang;
        set => SetProperty(ref _deletingDonHang, value);
    }

    public int TongDonHang
    {
        get => _tongDonHang;
        set => SetProperty(ref _tongDonHang, value);
    }

    public decimal TongDoanhThu
    {
        get => _tongDoanhThu;
        set => SetProperty(ref _tongDoanhThu, value);
    }

    public string TopNhanVienName
    {
        get => _topNhanVienName;
        set => SetProperty(ref _topNhanVienName, value);
    }

    public decimal TopNhanVienDoanhThu
    {
        get => _topNhanVienDoanhThu;
        set => SetProperty(ref _topNhanVienDoanhThu, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand CloseDetailCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand PrintHoaDonCommand { get; }
    public ICommand NavigateToBanHangCommand { get; }
    public ICommand DeleteDonHangCommand { get; }
    public ICommand ConfirmDeleteDonHangCommand { get; }
    public ICommand CloseDeleteDialogCommand { get; }

    public event Action? OnNavigateToBanHang;

    public DonHangViewModel()
    {
        _db = App.Services.GetRequiredService<DatabaseService>();

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        ViewDetailCommand = new RelayCommand(ExecuteViewDetail);
        CloseDetailCommand = new RelayCommand(_ => IsDetailOpen = false);
        ClearFilterCommand = new RelayCommand(ExecuteClearFilter);
        PrintHoaDonCommand = new RelayCommand(_ => ExecutePrintHoaDon());
        NavigateToBanHangCommand = new RelayCommand(_ => OnNavigateToBanHang?.Invoke());
        DeleteDonHangCommand = new RelayCommand(ExecuteDeleteDonHang);
        ConfirmDeleteDonHangCommand = new AsyncRelayCommand(async _ => await ConfirmDeleteDonHangAsync());
        CloseDeleteDialogCommand = new RelayCommand(_ => IsDeleteDialogOpen = false);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var allOrders = await _db.GetDonHangsAsync(TuNgay, DenNgay);

            // Lọc theo nhân viên
            var filtered = allOrders.AsEnumerable();
            if (SelectedNhanVien != null)
            {
                filtered = filtered.Where(d => d.NhanVienID == SelectedNhanVien.NhanVienID);
            }

            // Lọc theo văn bản tìm kiếm
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(d =>
                    (d.MaDonHang?.ToLower().Contains(search) == true) ||
                    (d.NhanVien?.HoTen?.ToLower().Contains(search) == true) ||
                    (d.GhiChu?.ToLower().Contains(search) == true)
                );
            }

            var filteredList = filtered.ToList();

            // Tính thống kê từ danh sách đã lọc
            TongDonHang = filteredList.Count;
            TongDoanhThu = filteredList.Sum(d => d.ThanhToan);

            // Tính nhân viên bán chạy nhất (từ tất cả đơn hàng, không bị ảnh hưởng bởi filter NV)
            var topNV = allOrders
                .Where(d => d.NhanVien != null)
                .GroupBy(d => new { d.NhanVienID, d.NhanVien!.HoTen })
                .Select(g => new { g.Key.HoTen, DoanhThu = g.Sum(d => d.ThanhToan) })
                .OrderByDescending(x => x.DoanhThu)
                .FirstOrDefault();
            TopNhanVienName = topNV?.HoTen ?? "—";
            TopNhanVienDoanhThu = topNV?.DoanhThu ?? 0;

            // Cập nhật danh sách NV lọc từ đơn hàng thực tế (không chỉ NV bán hàng)
            var nvFromOrders = allOrders
                .Where(d => d.NhanVien != null)
                .Select(d => d.NhanVien!)
                .GroupBy(nv => nv.NhanVienID)
                .Select(g => g.First())
                .OrderBy(nv => nv.HoTen)
                .ToList();
            NhanViens = new ObservableCollection<NhanVien>(nvFromOrders);

            DonHangs = new ObservableCollection<DonHang>(filteredList);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading DonHangs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadChiTietAsync(DonHang donHang)
    {
        try
        {
            if (donHang.DonHangID > 0)
            {
                var chiTiets = await _db.GetDonHangChiTietsAsync(donHang.DonHangID);
                ChiTietDonHangs = new ObservableCollection<CT_DonHang>(chiTiets);
            }
            else if (!string.IsNullOrEmpty(donHang.MaDonHang))
            {
                // Bản ghi từ PhieuBanHang: tải CT_PhieuBan và chuyển đổi sang CT_DonHang
                var ctPhieuBans = await _db.GetChiTietPhieuBanAsync(donHang.MaDonHang);
                var mapped = ctPhieuBans.Select(ct => new CT_DonHang
                {
                    SoLuong = ct.SoLuong ?? 0,
                    DonGia = (ct.SoLuong ?? 0) > 0 ? (ct.ThanhTien ?? 0) / (ct.SoLuong ?? 1) : 0,
                    ThanhTien = ct.ThanhTien ?? 0,
                    Pizza = new Pizza
                    {
                        TenPizza = ct.HangHoa?.TenHangHoa ?? ct.MaHangHoa ?? "",
                        KichThuoc = ct.DoanhMucSize?.TenSize ?? ct.SizeID ?? "",
                        HinhAnh = ct.HangHoa?.HinhAnh
                    }
                }).ToList();
                ChiTietDonHangs = new ObservableCollection<CT_DonHang>(mapped);
            }
            else
            {
                ChiTietDonHangs = [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading ChiTiet: {ex.Message}");
        }
    }

    private void ExecuteViewDetail(object? parameter)
    {
        if (parameter is DonHang dh)
        {
            SelectedDonHang = dh;
            IsDetailOpen = true;
        }
    }

    private void ExecuteDeleteDonHang(object? parameter)
    {
        if (parameter is DonHang dh)
        {
            DeletingDonHang = dh;
            IsDeleteDialogOpen = true;
        }
    }

    private async Task ConfirmDeleteDonHangAsync()
    {
        if (DeletingDonHang == null) return;

        try
        {
            bool success = await _db.DeleteDonHangAsync(DeletingDonHang);
            if (success)
            {
                IsDeleteDialogOpen = false;
                DeletingDonHang = null;
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show("Không thể xóa đơn hàng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa đơn hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteClearFilter(object? parameter)
    {
        _searchText = string.Empty;
        _tuNgay = null;
        _denNgay = null;
        _selectedNhanVien = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(TuNgay));
        OnPropertyChanged(nameof(DenNgay));
        OnPropertyChanged(nameof(SelectedNhanVien));
        _ = LoadDataAsync();
    }

    private void ExecutePrintHoaDon()
    {
        if (SelectedDonHang == null || !ChiTietDonHangs.Any()) return;
        PrintService.PrintHoaDonBanHang(SelectedDonHang, ChiTietDonHangs);
    }
}
