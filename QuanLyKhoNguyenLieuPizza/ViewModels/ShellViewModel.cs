using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ShellViewModel : BaseViewModel
{
    private object? _currentContent;
    private string _selectedMenuItem = string.Empty;
    private string _tenNguoiDung = "Ten nguoi dung";
    private string? _chucVuNguoiDung;
    private string? _avatarNguoiDung;
    private bool _isProfileOpen;

    // Thuộc tính hiển thị theo vai trò
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
    private bool _canViewSaoLuu;
    private bool _isSidebarVisible = true;

    // ⚡ LAZY LOADING: ViewModel chỉ được tạo khi user chuyển đến trang đó
    // Trước đây tạo 11 ViewModel cùng lúc → ~50 DB connections lúc khởi động
    // Bây giờ chỉ tạo 1 ViewModel ban đầu → khởi động nhanh gấp 5-10 lần
    private DashboardViewModel? _dashboardViewModel;
    private TonKhoViewModel? _tonKhoViewModel;
    private NguyenLieuViewModel? _nguyenLieuViewModel;
    private PhieuNhapViewModel? _phieuNhapViewModel;
    private PhieuXuatViewModel? _phieuXuatViewModel;
    private NhaCungCapViewModel? _nhaCungCapViewModel;
    private NhanVienViewModel? _nhanVienViewModel;
    private BanHangViewModel? _banHangViewModel;
    private DonHangViewModel? _donHangViewModel;
    private PizzaViewModel? _pizzaViewModel;
    private QuyDinhViewModel? _quyDinhViewModel;
    private ProfileViewModel? _profileViewModel;
    private SaoLuuViewModel? _saoLuuViewModel;

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

    // Thuộc tính hiển thị theo vai trò cho các mục menu
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

    public bool CanViewSaoLuu
    {
        get => _canViewSaoLuu;
        set => SetProperty(ref _canViewSaoLuu, value);
    }

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => SetProperty(ref _isSidebarVisible, value);
    }

    // Computed properties: ẩn tiêu đề nhóm khi không có mục nào trong nhóm
    public bool CanViewTongQuanSection => CanViewBaoCaoThongKe;
    public bool CanViewQuanLyKhoSection => CanViewTonKho || CanViewPhieuNhap || CanViewPhieuXuat || CanViewNguyenLieu || CanViewPizza;
    public bool CanViewHeThongSection => CanViewNhaCungCap || CanViewNhanVien || CanViewSaoLuu;
    public bool CanViewBanHangSection => CanViewBanHang || CanViewDonHang;

    public bool IsProfileOpen
    {
        get => _isProfileOpen;
        set => SetProperty(ref _isProfileOpen, value);
    }

    public ProfileViewModel ProfileViewModel => _profileViewModel ??= CreateProfileViewModel();

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ShowProfileCommand { get; }

    public event Action? OnLogout;
    public event Action? OnChangePassword;

    public ShellViewModel()
    {
        // ⚡ KHÔNG tạo ViewModel ở đây nữa — chỉ tạo khi cần (lazy loading)

        NavigateCommand = new RelayCommand(ExecuteNavigate);
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        ShowProfileCommand = new RelayCommand(_ => ShowProfile());

        // Tải thông tin người dùng và thiết lập quyền
        RefreshUserInfo();
        SetupRoleBasedPermissions();

        // Bắt đầu với màn hình phù hợp theo vai trò — chỉ tạo 1 ViewModel ban đầu
        NavigateToDefaultView();
    }

    /// <summary>
    /// Tạo ProfileViewModel với thiết lập sự kiện. Chỉ gọi 1 lần (lazy).
    /// </summary>
    private ProfileViewModel CreateProfileViewModel()
    {
        var vm = new ProfileViewModel();
        vm.OnClose += () =>
        {
            IsProfileOpen = false;
            RefreshUserInfo();
        };
        vm.OnLogout += () =>
        {
            IsProfileOpen = false;
            OnLogout?.Invoke();
        };
        vm.OnChangePassword += () =>
        {
            IsProfileOpen = false;
            OnChangePassword?.Invoke();
        };
        return vm;
    }

    private void SetupRoleBasedPermissions()
    {
        var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
        var chucVuId = nhanVien?.ChucVuID ?? 0;
        var chucVuTen = nhanVien?.ChucVu?.TenChucVu?.Trim() ?? "";
        
        // Giá trị cơ sở dữ liệu:
        // ChucVuID 2: "Quản lý"
        // ChucVuID 3: "Nhân viên bếp"  
        // ChucVuID 4: "Nhân viên kho"
        // ChucVuID 5: "Nhân viên bán hàng"

        // Kiểm tra theo ChucVuID trước (chính xác nhất), sau đó theo tên làm dự phòng
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

        // Thiết lập quyền dựa trên vai trò - Quản lý có ưu tiên cao nhất
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
            CanViewSaoLuu = true;
            IsSidebarVisible = true;
        }
        else if (isNhanVienKho)
        {
            // Nhân viên kho: quản lý phiếu nhập + xuất
            CanViewBaoCaoThongKe = false;
            CanViewTonKho = false;
            CanViewPhieuNhap = true;
            CanViewPhieuXuat = true;
            CanViewNguyenLieu = false;
            CanViewNhaCungCap = false;
            CanViewNhanVien = false;
            CanViewBanHang = false;
            CanViewDonHang = false;
            CanViewPizza = false;
            IsSidebarVisible = false;
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
            IsSidebarVisible = false;
        }
        else if (isNhanVienBanHang)
        {
            // Nhân viên bán hàng: bán hàng + xuất kho
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
            IsSidebarVisible = false;
        }
        else
        {
            // Mặc định: xem tồn kho + xuất kho
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

        // Thông báo cập nhật hiển thị tiêu đề nhóm
        OnPropertyChanged(nameof(CanViewTongQuanSection));
        OnPropertyChanged(nameof(CanViewQuanLyKhoSection));
        OnPropertyChanged(nameof(CanViewHeThongSection));
        OnPropertyChanged(nameof(CanViewBanHangSection));
    }

    private void NavigateToDefaultView()
    {
        // Điều hướng đến màn hình đầu tiên có sẵn dựa trên quyền
        // Gọi trực tiếp NavigateToContent để đảm bảo ViewModel được tạo
        if (CanViewBaoCaoThongKe)
        {
            _selectedMenuItem = "BaoCaoThongKe";
            OnPropertyChanged(nameof(SelectedMenuItem));
            NavigateToContent("BaoCaoThongKe");
        }
        else if (CanViewPhieuNhap)
        {
            _selectedMenuItem = "PhieuNhap";
            OnPropertyChanged(nameof(SelectedMenuItem));
            NavigateToContent("PhieuNhap");
        }
        else if (CanViewPhieuXuat)
        {
            _selectedMenuItem = "PhieuXuat";
            OnPropertyChanged(nameof(SelectedMenuItem));
            NavigateToContent("PhieuXuat");
        }
        else if (CanViewBanHang)
        {
            _selectedMenuItem = "BanHang";
            OnPropertyChanged(nameof(SelectedMenuItem));
            NavigateToContent("BanHang");
        }
        else if (CanViewTonKho)
        {
            _selectedMenuItem = "TonKho";
            OnPropertyChanged(nameof(SelectedMenuItem));
            NavigateToContent("TonKho");
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
        ProfileViewModel.RefreshData();
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
        // ⚡ LAZY: ViewModel chỉ được tạo lần đầu khi user navigate đến
        // Lần tiếp theo sẽ dùng lại instance đã tạo (??= operator)
        CurrentContent = menuItem switch
        {
            "BaoCaoThongKe" => _dashboardViewModel ??= new DashboardViewModel(),
            "TonKho" => CreateTonKhoViewModel(),
            "NguyenLieu" => _nguyenLieuViewModel ??= new NguyenLieuViewModel(),
            "PhieuNhap" => _phieuNhapViewModel ??= new PhieuNhapViewModel(),
            "PhieuXuat" => _phieuXuatViewModel ??= new PhieuXuatViewModel(),
            "NhaCungCap" => _nhaCungCapViewModel ??= new NhaCungCapViewModel(),
            "NhanVien" => _nhanVienViewModel ??= new NhanVienViewModel(),
            "BanHang" => _banHangViewModel ??= new BanHangViewModel(),
            "DonHang" => CreateDonHangViewModel(),
            "Pizza" => _pizzaViewModel ??= new PizzaViewModel(),
            "QuyDinh" => _quyDinhViewModel ??= new QuyDinhViewModel(),
            "SaoLuu" => _saoLuuViewModel ??= new SaoLuuViewModel(),
            _ => _dashboardViewModel ??= new DashboardViewModel()
        };
    }

    private TonKhoViewModel CreateTonKhoViewModel()
    {
        if (_tonKhoViewModel == null)
        {
            _tonKhoViewModel = new TonKhoViewModel();
            _tonKhoViewModel.OnNavigateToPhieuNhap += () => SelectedMenuItem = "PhieuNhap";
            _tonKhoViewModel.OnNavigateToPhieuXuat += () => SelectedMenuItem = "PhieuXuat";
        }
        return _tonKhoViewModel;
    }

    private DonHangViewModel CreateDonHangViewModel()
    {
        if (_donHangViewModel == null)
        {
            _donHangViewModel = new DonHangViewModel();
            _donHangViewModel.OnNavigateToBanHang += () => SelectedMenuItem = "BanHang";
        }
        return _donHangViewModel;
    }
}

