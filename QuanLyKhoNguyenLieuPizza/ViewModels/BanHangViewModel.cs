using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class CartItem : BaseViewModel
{
    private int _soLuong = 1;

    public HangHoa HangHoa { get; set; } = null!;
    public DoanhMuc_Size Size { get; set; } = null!;
    public DoanhMuc_De? DeBanh { get; set; }
    public decimal GiaBan { get; set; }
    public decimal GiaThem { get; set; }
    public int SoLuong
    {
        get => _soLuong;
        set
        {
            if (SetProperty(ref _soLuong, value))
                OnPropertyChanged(nameof(ThanhTien));
        }
    }
    public decimal DonGia => GiaBan + GiaThem;
    public decimal ThanhTien => DonGia * SoLuong;

    public string CartKey => $"{HangHoa.MaHangHoa}_{Size.SizeID}_{DeBanh?.MaDeBanh ?? "NONE"}";
}

public class BanHangViewModel : BaseViewModel
{
    private readonly IDatabaseService _db;

    // Trạng thái hiển thị
    private bool _isMainView = true;
    private bool _isOrderHistoryView;
    private bool _isOrderDetailOpen;
    private bool _isLoading;
    private bool _isSizeDeDialogOpen;
    private bool _isDeletePhieuBanDialogOpen;

    // Danh sách sản phẩm & giỏ hàng
    private ObservableCollection<HangHoa> _hangHoas = new();
    private List<HangHoa> _cachedActiveHangHoas = new();
    private ObservableCollection<CartItem> _cartItems = new();
    private ObservableCollection<PhieuBanHang> _phieuBanHangs = new();

    // Lựa chọn Size & Đế
    private ObservableCollection<DoanhMuc_Size> _sizes = new();
    private ObservableCollection<DoanhMuc_De> _deBanhs = new();
    private ObservableCollection<GiaTheo_Size> _giaTheoSizes = new();
    private ObservableCollection<GiaTheo_De> _giaTheoDes = new();
    private DoanhMuc_Size? _selectedSize;
    private DoanhMuc_De? _selectedDe;
    private HangHoa? _selectedHangHoa;

    // Thống kê
    private decimal _doanhThuHomNay;
    private int _tongDonHomNay;

    // Chi tiết đơn hàng
    private PhieuBanHang? _selectedPhieuBan;
    private ObservableCollection<CT_PhieuBan> _selectedChiTiets = new();
    private PhieuBanHang? _deletingPhieuBan;

    // Lọc
    private DateTime _filterFromDate = DateTime.Today;
    private DateTime _filterToDate = DateTime.Today;
    private string _searchText = string.Empty;
    private string _ghiChu = string.Empty;
    private string _selectedPhuongThucTT = "Tiền mặt";

    // Thanh toán chi tiết
    private decimal? _giamGia;
    private decimal? _tienKhachDua;

    #region Thuộc tính
    public bool IsMainView { get => _isMainView; set { SetProperty(ref _isMainView, value); } }
    public bool IsOrderHistoryView { get => _isOrderHistoryView; set { SetProperty(ref _isOrderHistoryView, value); } }
    public bool IsOrderDetailOpen { get => _isOrderDetailOpen; set { SetProperty(ref _isOrderDetailOpen, value); } }
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); } }
    public bool IsSizeDeDialogOpen { get => _isSizeDeDialogOpen; set { SetProperty(ref _isSizeDeDialogOpen, value); } }
    public bool IsDeletePhieuBanDialogOpen { get => _isDeletePhieuBanDialogOpen; set { SetProperty(ref _isDeletePhieuBanDialogOpen, value); } }

    public ObservableCollection<HangHoa> HangHoas { get => _hangHoas; set { SetProperty(ref _hangHoas, value); } }
    public ObservableCollection<CartItem> CartItems { get => _cartItems; set { SetProperty(ref _cartItems, value); } }
    public ObservableCollection<PhieuBanHang> PhieuBanHangs { get => _phieuBanHangs; set { SetProperty(ref _phieuBanHangs, value); } }

    public ObservableCollection<DoanhMuc_Size> Sizes { get => _sizes; set { SetProperty(ref _sizes, value); } }
    public ObservableCollection<DoanhMuc_De> DeBanhs { get => _deBanhs; set { SetProperty(ref _deBanhs, value); } }
    public ObservableCollection<GiaTheo_Size> GiaTheoSizes { get => _giaTheoSizes; set { SetProperty(ref _giaTheoSizes, value); } }
    public ObservableCollection<GiaTheo_De> GiaTheoDes { get => _giaTheoDes; set { SetProperty(ref _giaTheoDes, value); } }

    public DoanhMuc_Size? SelectedSize
    {
        get => _selectedSize;
        set
        {
            if (SetProperty(ref _selectedSize, value))
            {
                if (value != null)
                    _ = LoadGiaTheoDeAsync(value.SizeID);
                OnPropertyChanged(nameof(PreviewGiaBanSize));
                OnPropertyChanged(nameof(PreviewGiaThem));
                OnPropertyChanged(nameof(PreviewGia));
            }
        }
    }
    public DoanhMuc_De? SelectedDe
    {
        get => _selectedDe;
        set
        {
            if (SetProperty(ref _selectedDe, value))
            {
                OnPropertyChanged(nameof(PreviewGiaThem));
                OnPropertyChanged(nameof(PreviewGia));
            }
        }
    }
    public HangHoa? SelectedHangHoa { get => _selectedHangHoa; set { SetProperty(ref _selectedHangHoa, value); } }

    public decimal PreviewGiaBanSize
    {
        get
        {
            if (SelectedSize == null) return 0;
            var giaSize = GiaTheoSizes.FirstOrDefault(g => g.SizeID == SelectedSize.SizeID);
            return giaSize?.GiaBan ?? 0;
        }
    }
    public decimal PreviewGiaThem
    {
        get
        {
            if (SelectedDe == null || SelectedSize == null) return 0;
            var giaDe = GiaTheoDes.FirstOrDefault(g => g.MaDeBanh == SelectedDe.MaDeBanh);
            return giaDe?.GiaThem ?? 0;
        }
    }
    public decimal PreviewGia => PreviewGiaBanSize + PreviewGiaThem;

    public decimal TongTienGioHang => CartItems.Sum(c => c.ThanhTien);
    public int TongSoMon => CartItems.Sum(c => c.SoLuong);

    public decimal? GiamGia
    {
        get => _giamGia;
        set
        {
            if (SetProperty(ref _giamGia, value))
                RefreshPaymentTotals();
        }
    }
    public decimal KhachCanTra => Math.Max(0, TongTienGioHang - (GiamGia ?? 0));
    public decimal? TienKhachDua
    {
        get => _tienKhachDua;
        set
        {
            if (SetProperty(ref _tienKhachDua, value))
                OnPropertyChanged(nameof(TienThua));
        }
    }
    public decimal TienThua => Math.Max(0, (TienKhachDua ?? 0) - KhachCanTra);

    public decimal DoanhThuHomNay { get => _doanhThuHomNay; set { SetProperty(ref _doanhThuHomNay, value); } }
    public int TongDonHomNay { get => _tongDonHomNay; set { SetProperty(ref _tongDonHomNay, value); } }

    public PhieuBanHang? SelectedPhieuBan { get => _selectedPhieuBan; set { SetProperty(ref _selectedPhieuBan, value); } }
    public ObservableCollection<CT_PhieuBan> SelectedChiTiets { get => _selectedChiTiets; set { SetProperty(ref _selectedChiTiets, value); } }
    public PhieuBanHang? DeletingPhieuBan { get => _deletingPhieuBan; set { SetProperty(ref _deletingPhieuBan, value); } }

    public DateTime FilterFromDate { get => _filterFromDate; set { if (SetProperty(ref _filterFromDate, value)) _ = LoadPhieuBanHangsAsync(); } }
    public DateTime FilterToDate { get => _filterToDate; set { if (SetProperty(ref _filterToDate, value)) _ = LoadPhieuBanHangsAsync(); } }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterHangHoas();
        }
    }
    public string GhiChu { get => _ghiChu; set { SetProperty(ref _ghiChu, value); } }

    public List<string> DanhSachPhuongThucTT { get; } = new() { "Tiền mặt", "Chuyển khoản" };
    public string SelectedPhuongThucTT
    {
        get => _selectedPhuongThucTT;
        set
        {
            if (SetProperty(ref _selectedPhuongThucTT, value))
            {
                OnPropertyChanged(nameof(IsChuyenKhoan));
                OnPropertyChanged(nameof(QrCodeUrl));
            }
        }
    }

    // VietQR Config (đổi sang tài khoản thật khi deploy)
    private const string BANK_ID = "MB";
    private const string BANK_ACCOUNT = "0123456789";
    private const string BANK_NAME = "MB Bank";
    private const string ACCOUNT_NAME = "PIZZINN";

    public bool IsChuyenKhoan => SelectedPhuongThucTT == "Chuyển khoản";
    public string QrCodeUrl => $"https://img.vietqr.io/image/{BANK_ID}-{BANK_ACCOUNT}-compact2.png?amount={KhachCanTra:0}&addInfo=Thanh+toan+Pizza";
    #endregion

    #region Lệnh
    // Điều hướng
    public ICommand ShowPOSCommand { get; }
    public ICommand ShowOrderHistoryCommand { get; }
    public ICommand BackToMainCommand { get; }

    // Bán hàng
    public ICommand SelectHangHoaCommand { get; }
    public ICommand ConfirmAddToCartCommand { get; }
    public ICommand CancelSizeDeDialogCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand PrintTamTinhCommand { get; }

    // Đơn hàng
    public ICommand ViewOrderDetailCommand { get; }
    public ICommand CloseOrderDetailCommand { get; }
    public ICommand DeletePhieuBanCommand { get; }
    public ICommand CloseDeletePhieuBanDialogCommand { get; }
    public ICommand ConfirmDeletePhieuBanCommand { get; }
    #endregion

    public BanHangViewModel()
    {
        _db = ServiceLocator.Instance.GetService<IDatabaseService>();

        // Lệnh điều hướng
        ShowPOSCommand = new RelayCommand(_ => NavigateTo("POS"));
        ShowOrderHistoryCommand = new AsyncRelayCommand(async _ => { NavigateTo("History"); await LoadPhieuBanHangsAsync(); });
        BackToMainCommand = new RelayCommand(_ => NavigateTo("POS"));

        // Lệnh bán hàng
        SelectHangHoaCommand = new AsyncRelayCommand(ExecuteSelectHangHoaAsync);
        ConfirmAddToCartCommand = new RelayCommand(ExecuteConfirmAddToCart);
        CancelSizeDeDialogCommand = new RelayCommand(_ => IsSizeDeDialogOpen = false);
        RemoveFromCartCommand = new RelayCommand(ExecuteRemoveFromCart);
        IncreaseQuantityCommand = new RelayCommand(ExecuteIncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand(ExecuteDecreaseQuantity);
        CheckoutCommand = new AsyncRelayCommand(ExecuteCheckoutAsync);
        ClearCartCommand = new RelayCommand(_ => { CartItems.Clear(); RefreshCartTotals(); });
        PrintTamTinhCommand = new RelayCommand(_ => ExecutePrintTamTinh());

        // Đơn hàng
        ViewOrderDetailCommand = new AsyncRelayCommand(ExecuteViewOrderDetailAsync);
        CloseOrderDetailCommand = new RelayCommand(_ => IsOrderDetailOpen = false);
        DeletePhieuBanCommand = new RelayCommand(ExecuteDeletePhieuBan);
        CloseDeletePhieuBanDialogCommand = new RelayCommand(_ => { IsDeletePhieuBanDialogOpen = false; DeletingPhieuBan = null; });
        ConfirmDeletePhieuBanCommand = new AsyncRelayCommand(ExecuteConfirmDeletePhieuBanAsync);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await LoadHangHoasAsync();
            await LoadStatsAsync();
            await LoadSupportDataAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Điều hướng
    private void NavigateTo(string view)
    {
        IsMainView = view == "POS";
        IsOrderHistoryView = view == "History";
    }
    #endregion

    #region Tải dữ liệu
    private async Task LoadHangHoasAsync()
    {
        var hangHoas = await _db.GetHangHoasAsync();
        _cachedActiveHangHoas = hangHoas;

        await RefreshStockStatusAsync();
        FilterHangHoas();
    }

    private async Task RefreshStockStatusAsync()
    {
        if (_cachedActiveHangHoas.Count == 0)
            return;

        var missingMap = await _db.GetOutOfStockIngredientsByHangHoaAsync(
            _cachedActiveHangHoas.Select(h => h.MaHangHoa));

        foreach (var hangHoa in _cachedActiveHangHoas)
        {
            if (missingMap.TryGetValue(hangHoa.MaHangHoa, out var missing) && missing.Count > 0)
            {
                hangHoa.IsOutOfStock = true;
                hangHoa.OutOfStockNote = $"Thiếu: {string.Join(", ", missing)}";
            }
            else
            {
                hangHoa.IsOutOfStock = false;
                hangHoa.OutOfStockNote = string.Empty;
            }
        }
    }

    private async Task LoadStatsAsync()
    {
        var today = DateTime.Today;
        DoanhThuHomNay = await _db.GetDoanhThuBanHangAsync(today, today);
        TongDonHomNay = await _db.GetTotalPhieuBanCountAsync(today, today);
    }

    private async Task LoadPhieuBanHangsAsync()
    {
        var phieuBans = await _db.GetPhieuBanHangsAsync(FilterFromDate, FilterToDate);
        
        var currentNhanVienId = CurrentUserSession.Instance.CurrentUser?.NhanVienID;
        var chucVuId = CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVuID;
        
        // Quản lý (ChucVuID = 2) xem được tất cả, còn lại chỉ xem đơn hàng của mình
        if (chucVuId != 2)
        {
            phieuBans = phieuBans.Where(p => p.NhanVienBanID == currentNhanVienId).ToList();
        }
        
        PhieuBanHangs = new ObservableCollection<PhieuBanHang>(phieuBans);
    }

    private async Task LoadSupportDataAsync()
    {
        var sizes = await _db.GetDoanhMucSizesAsync();
        Sizes = new ObservableCollection<DoanhMuc_Size>(sizes);
        var des = await _db.GetDoanhMucDesAsync();
        DeBanhs = new ObservableCollection<DoanhMuc_De>(des);
    }

    private async Task LoadGiaTheoDeAsync(string sizeId)
    {
        var giaTheoDes = await _db.GetGiaTheoDeAsync(sizeId);
        GiaTheoDes = new ObservableCollection<GiaTheo_De>(giaTheoDes);
        OnPropertyChanged(nameof(PreviewGiaThem));
        OnPropertyChanged(nameof(PreviewGia));
    }

    private void FilterHangHoas()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            HangHoas = new ObservableCollection<HangHoa>(_cachedActiveHangHoas);
            return;
        }
        var search = SearchText.ToLower();
        var filtered = _cachedActiveHangHoas.Where(h =>
            (h.TenHangHoa?.ToLower().Contains(search) ?? false) ||
            h.MaHangHoa.ToLower().Contains(search)).ToList();
        HangHoas = new ObservableCollection<HangHoa>(filtered);
    }
    #endregion

    #region Thao tác giỏ hàng
    private async Task ExecuteSelectHangHoaAsync(object? parameter)
    {
        if (parameter is not HangHoa hangHoa) return;
        if (hangHoa.IsOutOfStock)
        {
            var note = string.IsNullOrWhiteSpace(hangHoa.OutOfStockNote) ? "Pizza này đang hết nguyên liệu." : hangHoa.OutOfStockNote;
            MessageBox.Show(note, "Hết hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SelectedHangHoa = hangHoa;

        // Tải giá cho sản phẩm này
        var giaTheoSizes = await _db.GetGiaTheoSizeByHangHoaAsync(hangHoa.MaHangHoa);
        GiaTheoSizes = new ObservableCollection<GiaTheo_Size>(giaTheoSizes);

        // Đặt lại lựa chọn
        SelectedSize = Sizes.FirstOrDefault();
        SelectedDe = null;

        IsSizeDeDialogOpen = true;
    }

    private void ExecuteConfirmAddToCart(object? parameter)
    {
        if (SelectedHangHoa == null || SelectedSize == null) return;

        // Tìm giá cho size đã chọn
        var giaSize = GiaTheoSizes.FirstOrDefault(g => g.SizeID == SelectedSize.SizeID);
        decimal giaBan = giaSize?.GiaBan ?? 0;

        // Tìm giá bổ sung cho đế bánh đã chọn
        decimal giaThem = 0;
        if (SelectedDe != null)
        {
            var giaDe = GiaTheoDes.FirstOrDefault(g => g.MaDeBanh == SelectedDe.MaDeBanh);
            giaThem = giaDe?.GiaThem ?? 0;
        }

        var cartKey = $"{SelectedHangHoa.MaHangHoa}_{SelectedSize.SizeID}_{SelectedDe?.MaDeBanh ?? "NONE"}";
        var existing = CartItems.FirstOrDefault(c => c.CartKey == cartKey);
        if (existing != null)
        {
            existing.SoLuong++;
        }
        else
        {
            CartItems.Add(new CartItem
            {
                HangHoa = SelectedHangHoa,
                Size = SelectedSize,
                DeBanh = SelectedDe,
                GiaBan = giaBan,
                GiaThem = giaThem,
                SoLuong = 1
            });
        }

        IsSizeDeDialogOpen = false;
        RefreshCartTotals();
    }

    private void ExecuteRemoveFromCart(object? parameter)
    {
        if (parameter is not CartItem item) return;
        CartItems.Remove(item);
        RefreshCartTotals();
    }

    private void ExecuteIncreaseQuantity(object? parameter)
    {
        if (parameter is not CartItem item) return;
        item.SoLuong++;
        RefreshCartTotals();
    }

    private void ExecuteDecreaseQuantity(object? parameter)
    {
        if (parameter is not CartItem item) return;
        if (item.SoLuong > 1)
            item.SoLuong--;
        else
            CartItems.Remove(item);
        RefreshCartTotals();
    }

    private void RefreshCartTotals()
    {
        OnPropertyChanged(nameof(TongTienGioHang));
        OnPropertyChanged(nameof(TongSoMon));
        RefreshPaymentTotals();
    }

    private void RefreshPaymentTotals()
    {
        OnPropertyChanged(nameof(KhachCanTra));
        OnPropertyChanged(nameof(TienThua));
        OnPropertyChanged(nameof(QrCodeUrl));
    }

    private void ExecutePrintTamTinh()
    {
        if (!CartItems.Any())
        {
            MessageBox.Show("Giỏ hàng trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Tạo phiếu tạm tính (chưa lưu DB) - convert sang DonHang/CT_DonHang để dùng chung print
        var tempDonHang = new DonHang
        {
            MaDonHang = "TẠM TÍNH",
            NgayTao = DateTime.Now,
            TongTien = TongTienGioHang,
            GiamGia = GiamGia ?? 0,
            ThanhToan = KhachCanTra,
            TienKhachDua = TienKhachDua,
            GhiChu = GhiChu,
            PhuongThucTT = SelectedPhuongThucTT,
            NhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien
        };
        var chiTiets = CartItems.Select(c => new CT_DonHang
        {
            SoLuong = c.SoLuong,
            DonGia = c.DonGia,
            ThanhTien = c.ThanhTien,
            Pizza = new Pizza
            {
                TenPizza = c.HangHoa.TenHangHoa ?? "",
                KichThuoc = c.Size.TenSize ?? ""
            }
        });
        PrintService.PrintHoaDonBanHang(tempDonHang, chiTiets);
    }

    private async Task ExecuteCheckoutAsync(object? parameter)
    {
        if (!CartItems.Any())
        {
            MessageBox.Show("Giỏ hàng trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Xác nhận thanh toán đơn hàng?\nTổng tiền: {TongTienGioHang:N0} d",
            "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var nhanVienId = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID;
            var maPhieuBan = await _db.GenerateMaPhieuBanAsync();

            var phieuBan = new PhieuBanHang
            {
                MaPhieuBan = maPhieuBan,
                NhanVienBanID = nhanVienId,
                NgayBan = DateTime.Now,
                TongTien = TongTienGioHang,
                GhiChu = string.IsNullOrWhiteSpace(GhiChu) ? null : GhiChu.Trim(),
                PhuongThucTT = SelectedPhuongThucTT
            };

            var chiTiets = CartItems.Select(c => new CT_PhieuBan
            {
                MaHangHoa = c.HangHoa.MaHangHoa,
                SizeID = c.Size.SizeID,
                MaDeBanh = c.DeBanh?.MaDeBanh,
                SoLuong = c.SoLuong,
                ThanhTien = c.ThanhTien
            }).ToList();

            var savedMa = await _db.SavePhieuBanHangAsync(phieuBan, chiTiets);
            if (!string.IsNullOrEmpty(savedMa))
            {
                MessageBox.Show($"Thanh toán thành công!\nMã phiếu: {savedMa}\nTổng tiền: {TongTienGioHang:N0} d",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                CartItems.Clear();
                GhiChu = string.Empty;
                SelectedPhuongThucTT = "Tiền mặt";
                GiamGia = null;
                TienKhachDua = null;
                RefreshCartTotals();
                await LoadStatsAsync();
                await RefreshStockStatusAsync();
                FilterHangHoas();
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra khi tạo phiếu bán!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            await RefreshStockStatusAsync();
            FilterHangHoas();
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion

    #region Đơn hàng
    private async Task ExecuteViewOrderDetailAsync(object? parameter)
    {
        if (parameter is not PhieuBanHang pb) return;
        SelectedPhieuBan = pb;
        var chiTiets = await _db.GetChiTietPhieuBanAsync(pb.MaPhieuBan);
        SelectedChiTiets = new ObservableCollection<CT_PhieuBan>(chiTiets);
        IsOrderDetailOpen = true;
    }

    private void ExecuteDeletePhieuBan(object? parameter)
    {
        if (parameter is not PhieuBanHang pb) return;
        DeletingPhieuBan = pb;
        IsDeletePhieuBanDialogOpen = true;
    }

    private async Task ExecuteConfirmDeletePhieuBanAsync(object? parameter)
    {
        if (DeletingPhieuBan == null) return;
        
        IsLoading = true;
        try
        {
            var success = await _db.DeletePhieuBanHangAsync(DeletingPhieuBan.MaPhieuBan);
            if (success)
            {
                PhieuBanHangs.Remove(DeletingPhieuBan);
                
                // Cập nhật lại thống kê nếu hóa đơn bị xóa thuộc ngày hôm nay
                if (DeletingPhieuBan.NgayBan?.Date == DateTime.Today)
                {
                    await LoadStatsAsync();
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Có lỗi xảy ra khi xóa đơn hàng!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
            IsDeletePhieuBanDialogOpen = false;
            DeletingPhieuBan = null;
        }
    }
    #endregion
}





