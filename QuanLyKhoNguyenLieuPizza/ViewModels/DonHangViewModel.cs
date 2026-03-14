using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class DonHangViewModel : BaseViewModel
{
    private readonly IDatabaseService _db;

    private ObservableCollection<DonHang> _donHangs = [];
    private ObservableCollection<CT_DonHang> _chiTietDonHangs = [];
    private DonHang? _selectedDonHang;
    private string _searchText = string.Empty;
    private DateTime? _tuNgay;
    private DateTime? _denNgay;
    private bool _isLoading;
    private bool _isDetailOpen;
    private int _tongDonHang;
    private int _donHoanThanh;
    private decimal _tongDoanhThu;

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

    public int TongDonHang
    {
        get => _tongDonHang;
        set => SetProperty(ref _tongDonHang, value);
    }

    public int DonHoanThanh
    {
        get => _donHoanThanh;
        set => SetProperty(ref _donHoanThanh, value);
    }

    public decimal TongDoanhThu
    {
        get => _tongDoanhThu;
        set => SetProperty(ref _tongDoanhThu, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand CloseDetailCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public DonHangViewModel()
    {
        _db = ServiceLocator.Instance.GetService<IDatabaseService>();

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        ViewDetailCommand = new RelayCommand(ExecuteViewDetail);
        CloseDetailCommand = new RelayCommand(_ => IsDetailOpen = false);
        ClearFilterCommand = new RelayCommand(ExecuteClearFilter);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var allOrders = await _db.GetDonHangsAsync(TuNgay, DenNgay);

            // Compute stats from full date-filtered set before applying search filter
            TongDonHang = allOrders.Count;
            DonHoanThanh = allOrders.Count(d => d.TrangThai == 2);
            TongDoanhThu = allOrders.Sum(d => d.ThanhToan);

            // Filter by search text
            var filtered = allOrders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(d =>
                    (d.MaDonHang?.ToLower().Contains(search) == true) ||
                    (d.NhanVien?.HoTen?.ToLower().Contains(search) == true) ||
                    (d.GhiChu?.ToLower().Contains(search) == true)
                );
            }

            DonHangs = new ObservableCollection<DonHang>(filtered);
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
                // PhieuBanHang-sourced record: load CT_PhieuBan and map to CT_DonHang
                var ctPhieuBans = await _db.GetChiTietPhieuBanAsync(donHang.MaDonHang);
                var mapped = ctPhieuBans.Select(ct => new CT_DonHang
                {
                    SoLuong = ct.SoLuong ?? 0,
                    DonGia = (ct.SoLuong ?? 0) > 0 ? (ct.ThanhTien ?? 0) / (ct.SoLuong ?? 1) : 0,
                    ThanhTien = ct.ThanhTien ?? 0,
                    Pizza = new Pizza
                    {
                        TenPizza = ct.HangHoa?.TenHangHoa ?? ct.MaHangHoa ?? "",
                        KichThuoc = ct.DoanhMucSize?.TenSize ?? ct.SizeID ?? ""
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

    private void ExecuteClearFilter(object? parameter)
    {
        _searchText = string.Empty;
        _tuNgay = null;
        _denNgay = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(TuNgay));
        OnPropertyChanged(nameof(DenNgay));
        _ = LoadDataAsync();
    }
}
