using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ShellViewModel : BaseViewModel
{
    private object? _currentContent;
    private string _selectedMenuItem = "BaoCaoThongKe";
    private string _tenNguoiDung = "Ten nguoi dung";
    private string? _chucVuNguoiDung;
    private string? _avatarNguoiDung;
    private bool _isProfileOpen;

    // Role-based visibility properties
    private bool _canViewBaoCaoThongKe;
    private bool _canViewTonKho;
    private bool _canViewPhieuNhap;
    private bool _canViewPhieuXuat;
    private bool _canViewNguyenLieu;
    private bool _canViewNhaCungCap;
    private bool _canViewNhanVien;
    private bool _canViewBanHang;
    private bool _canViewDonHang;
    private bool _canViewPizza;

    private readonly DashboardViewModel _dashboardViewModel;
    private readonly TonKhoViewModel _tonKhoViewModel;
    private readonly NguyenLieuViewModel _nguyenLieuViewModel;
    private readonly PhieuNhapViewModel _phieuNhapViewModel;
    private readonly PhieuXuatViewModel _phieuXuatViewModel;
    private readonly NhaCungCapViewModel _nhaCungCapViewModel;
    private readonly NhanVienViewModel _nhanVienViewModel;
    private readonly BanHangViewModel _banHangViewModel;
    private readonly DonHangViewModel _donHangViewModel;
    private readonly PizzaViewModel _pizzaViewModel;
    private readonly ProfileViewModel _profileViewModel;

    public object? CurrentContent
    {
        get => _currentContent;
        set => SetProperty(ref _currentContent, value);
    }

    public string SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value))
            {
                NavigateToContent(value);
            }
        }
    }

    public string TenNguoiDung
    {
        get => _tenNguoiDung;
        set => SetProperty(ref _tenNguoiDung, value);
    }

    public string? ChucVuNguoiDung
    {
        get => _chucVuNguoiDung;
        set => SetProperty(ref _chucVuNguoiDung, value);
    }

    public string? AvatarNguoiDung
    {
        get => _avatarNguoiDung;
        set => SetProperty(ref _avatarNguoiDung, value);
    }

    public bool HasAvatar => !string.IsNullOrEmpty(AvatarNguoiDung);

    // Role-based visibility properties for menu items
    public bool CanViewBaoCaoThongKe
    {
        get => _canViewBaoCaoThongKe;
        set => SetProperty(ref _canViewBaoCaoThongKe, value);
    }

    public bool CanViewTonKho
    {
        get => _canViewTonKho;
        set => SetProperty(ref _canViewTonKho, value);
    }

    public bool CanViewPhieuNhap
    {
        get => _canViewPhieuNhap;
        set => SetProperty(ref _canViewPhieuNhap, value);
    }

    public bool CanViewPhieuXuat
    {
        get => _canViewPhieuXuat;
        set => SetProperty(ref _canViewPhieuXuat, value);
    }

    public bool CanViewNguyenLieu
    {
        get => _canViewNguyenLieu;
        set => SetProperty(ref _canViewNguyenLieu, value);
    }

    public bool CanViewNhaCungCap
    {
        get => _canViewNhaCungCap;
        set => SetProperty(ref _canViewNhaCungCap, value);
    }

    public bool CanViewNhanVien
    {
        get => _canViewNhanVien;
        set => SetProperty(ref _canViewNhanVien, value);
    }

    public bool CanViewBanHang
    {
        get => _canViewBanHang;
        set => SetProperty(ref _canViewBanHang, value);
    }

    public bool CanViewDonHang
    {
        get => _canViewDonHang;
        set => SetProperty(ref _canViewDonHang, value);
    }

    public bool CanViewPizza
    {
        get => _canViewPizza;
        set => SetProperty(ref _canViewPizza, value);
    }

    public bool IsProfileOpen
    {
        get => _isProfileOpen;
        set => SetProperty(ref _isProfileOpen, value);
    }

    public ProfileViewModel ProfileViewModel => _profileViewModel;

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ShowProfileCommand { get; }

    public event Action? OnLogout;
    public event Action? OnChangePassword;

    public ShellViewModel()
    {
        _dashboardViewModel = new DashboardViewModel();
        _tonKhoViewModel = new TonKhoViewModel();
        _nguyenLieuViewModel = new NguyenLieuViewModel();
        _phieuNhapViewModel = new PhieuNhapViewModel();
        _phieuXuatViewModel = new PhieuXuatViewModel();
        _nhaCungCapViewModel = new NhaCungCapViewModel();
        _nhanVienViewModel = new NhanVienViewModel();
        _banHangViewModel = new BanHangViewModel();
        _donHangViewModel = new DonHangViewModel();
        _pizzaViewModel = new PizzaViewModel();
        _profileViewModel = new ProfileViewModel();

        // Setup profile events
        _profileViewModel.OnClose += () => 
        {
            IsProfileOpen = false;
            RefreshUserInfo();
        };
        _profileViewModel.OnLogout += () => 
        {
            IsProfileOpen = false;
            OnLogout?.Invoke();
        };
        _profileViewModel.OnChangePassword += () =>
        {
            IsProfileOpen = false;
            OnChangePassword?.Invoke();
        };

        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        ShowProfileCommand = new RelayCommand(_ => ShowProfile());

        // Load user info and setup permissions
        RefreshUserInfo();
        SetupRoleBasedPermissions();

        // Start with appropriate view based on role
        NavigateToDefaultView();
    }

    private void SetupRoleBasedPermissions()
    {
        var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
        var chucVuId = nhanVien?.ChucVuID ?? 0;
        var chucVuTen = nhanVien?.ChucVu?.TenChucVu?.Trim() ?? "";
        
        // Database values:
        // ChucVuID 2: "Quản lý"
        // ChucVuID 3: "Nhân viên bếp"  
        // ChucVuID 4: "Nhân viên kho"
        // ChucVuID 5: "Nhân viên bán hàng"

        // Check by ChucVuID first (most reliable), then by name as fallback
        bool isQuanLy = chucVuId == 2 || 
                        chucVuTen.Contains("Quản lý") ||
                        chucVuTen.Contains("quản lý") ||
                        chucVuTen.ToLower().Contains("quan ly");

        bool isNhanVienBep = chucVuId == 3 ||
                             (!isQuanLy && (chucVuTen.Contains("bếp") || 
                                            chucVuTen.ToLower().Contains("bep")));

        bool isNhanVienKho = chucVuId == 4 ||
                             (!isQuanLy && !isNhanVienBep && chucVuTen.ToLower().Contains("kho"));

        bool isNhanVienBanHang = chucVuId == 5 ||
                                 (!isQuanLy && !isNhanVienBep && !isNhanVienKho && 
                                  (chucVuTen.ToLower().Contains("bán hàng") || chucVuTen.ToLower().Contains("ban hang")));

        // Set permissions based on role - Quản lý has highest priority
        if (isQuanLy)
        {
            // Quản lý: giám sát tất cả, KHÔNG bán hàng trực tiếp (xem doanh thu trong Dashboard)
            CanViewBaoCaoThongKe = true;
            CanViewTonKho = true;
            CanViewPhieuNhap = true;
            CanViewPhieuXuat = true;
            CanViewNguyenLieu = true;
            CanViewNhaCungCap = true;
            CanViewNhanVien = true;
            CanViewBanHang = false;
            CanViewDonHang = true;
            CanViewPizza = true;
        }
        else if (isNhanVienKho)
        {
            // Nhân viên kho: chỉ quản lý phiếu nhập
            CanViewBaoCaoThongKe = false;
            CanViewTonKho = false;
            CanViewPhieuNhap = true;
            CanViewPhieuXuat = false;
            CanViewNguyenLieu = false;
            CanViewNhaCungCap = false;
            CanViewNhanVien = false;
            CanViewBanHang = false;
            CanViewDonHang = false;
            CanViewPizza = false;
        }
        else if (isNhanVienBep)
        {
            // Nhân viên bếp: chỉ quản lý phiếu xuất
            CanViewBaoCaoThongKe = false;
            CanViewTonKho = false;
            CanViewPhieuNhap = false;
            CanViewPhieuXuat = true;
            CanViewNguyenLieu = false;
            CanViewNhaCungCap = false;
            CanViewNhanVien = false;
            CanViewBanHang = false;
            CanViewDonHang = false;
            CanViewPizza = false;
        }
        else if (isNhanVienBanHang)
        {
            // Nhân viên bán hàng: chỉ bán hàng
            CanViewBaoCaoThongKe = false;
            CanViewTonKho = false;
            CanViewPhieuNhap = false;
            CanViewPhieuXuat = false;
            CanViewNguyenLieu = false;
            CanViewNhaCungCap = false;
            CanViewNhanVien = false;
            CanViewBanHang = true;
            CanViewDonHang = false;
            CanViewPizza = false;
        }
        else
        {
            // Mặc định: chỉ xem tồn kho
            CanViewBaoCaoThongKe = false;
            CanViewTonKho = true;
            CanViewPhieuNhap = false;
            CanViewPhieuXuat = false;
            CanViewNguyenLieu = false;
            CanViewNhaCungCap = false;
            CanViewNhanVien = false;
            CanViewBanHang = false;
            CanViewDonHang = false;
            CanViewPizza = false;
        }
    }

    private void NavigateToDefaultView()
    {
        // Navigate to the first available view based on permissions
        if (CanViewBaoCaoThongKe)
        {
            SelectedMenuItem = "BaoCaoThongKe";
            CurrentContent = _dashboardViewModel;
        }
        else if (CanViewPhieuNhap)
        {
            SelectedMenuItem = "PhieuNhap";
            CurrentContent = _phieuNhapViewModel;
        }
        else if (CanViewPhieuXuat)
        {
            SelectedMenuItem = "PhieuXuat";
            CurrentContent = _phieuXuatViewModel;
        }
        else if (CanViewBanHang)
        {
            SelectedMenuItem = "BanHang";
            CurrentContent = _banHangViewModel;
        }
        else if (CanViewTonKho)
        {
            SelectedMenuItem = "TonKho";
            CurrentContent = _tonKhoViewModel;
        }
    }

    private void RefreshUserInfo()
    {
        TenNguoiDung = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HoTen ?? "Người dùng";
        ChucVuNguoiDung = CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVu?.TenChucVu ?? "Nhân viên";
        AvatarNguoiDung = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        OnPropertyChanged(nameof(HasAvatar));
    }

    private void ShowProfile()
    {
        _profileViewModel.RefreshData();
        IsProfileOpen = true;
    }

    private void ExecuteNavigate(object? parameter)
    {
        if (parameter is string menuItem)
        {
            SelectedMenuItem = menuItem;
        }
    }

    private void NavigateToContent(string menuItem)
    {
        CurrentContent = menuItem switch
        {
            "BaoCaoThongKe" => _dashboardViewModel,
            "TonKho" => _tonKhoViewModel,
            "NguyenLieu" => _nguyenLieuViewModel,
            "PhieuNhap" => _phieuNhapViewModel,
            "PhieuXuat" => _phieuXuatViewModel,
            "NhaCungCap" => _nhaCungCapViewModel,
            "NhanVien" => _nhanVienViewModel,
            "BanHang" => _banHangViewModel,
            "DonHang" => _donHangViewModel,
            "Pizza" => _pizzaViewModel,
            _ => _dashboardViewModel
        };
    }
}
