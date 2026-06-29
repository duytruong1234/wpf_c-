using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class PhieuXuatViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    #region Thuộc tính
    // Thuộc tính theo vai trò
    private bool _isNhanVien;
    public bool IsNhanVien
    {
        get => _isNhanVien;
        set => SetProperty(ref _isNhanVien, value);
    }

    private bool _isNhanVienBep;
    public bool IsNhanVienBep
    {
        get => _isNhanVienBep;
        set => SetProperty(ref _isNhanVienBep, value);
    }

    public bool IsQuanLy => !IsNhanVien;

    private string _currentUserName = string.Empty;
    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    private ObservableCollection<PhieuXuat> _phieuXuats = new();
    public ObservableCollection<PhieuXuat> PhieuXuats
    {
        get => _phieuXuats;
        set
        {
            if (SetProperty(ref _phieuXuats, value))
            {
                OnPropertyChanged(nameof(CountChoDuyet));
                OnPropertyChanged(nameof(CountDaDuyet));
            }
        }
    }

    public int CountChoDuyet => PhieuXuats?.Count(p => p.TrangThai == 1) ?? 0;
    public int CountDaDuyet => PhieuXuats?.Count(p => p.TrangThai == 2) ?? 0;

    private PhieuXuat? _selectedPhieuXuat;
    public PhieuXuat? SelectedPhieuXuat
    {
        get => _selectedPhieuXuat;
        set
        {
            if (SetProperty(ref _selectedPhieuXuat, value))
            {
                OnPropertyChanged(nameof(CanEditDelete));
                OnPropertyChanged(nameof(CanApprove));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(IsCancelVisible));
                OnPropertyChanged(nameof(IsEditDeleteVisible));
                OnPropertyChanged(nameof(CanDelete));
                OnPropertyChanged(nameof(IsDeleteVisible));
                OnPropertyChanged(nameof(IsLoaiPhieuEnabled));
            }
        }
    }

    private ObservableCollection<CT_PhieuXuat> _chiTietPhieuXuats = new();
    public ObservableCollection<CT_PhieuXuat> ChiTietPhieuXuats
    {
        get => _chiTietPhieuXuats;
        set => SetProperty(ref _chiTietPhieuXuats, value);
    }

    private ObservableCollection<NhanVien> _nhanViens = new();
    public ObservableCollection<NhanVien> NhanViens
    {
        get => _nhanViens;
        set => SetProperty(ref _nhanViens, value);
    }

    private ObservableCollection<NguyenLieu> _nguyenLieus = new();
    public ObservableCollection<NguyenLieu> NguyenLieus
    {
        get => _nguyenLieus;
        set => SetProperty(ref _nguyenLieus, value);
    }

    private ObservableCollection<DonViTinh> _donViTinhs = new();
    public ObservableCollection<DonViTinh> DonViTinhs
    {
        get => _donViTinhs;
        set => SetProperty(ref _donViTinhs, value);
    }

    private int _soPhieuChoDuyet;
    public int SoPhieuChoDuyet
    {
        get => _soPhieuChoDuyet;
        set => SetProperty(ref _soPhieuChoDuyet, value);
    }

    private int _soPhieuDaDuyet;
    public int SoPhieuDaDuyet
    {
        get => _soPhieuDaDuyet;
        set => SetProperty(ref _soPhieuDaDuyet, value);
    }

    private int _soPhieuDaHuy;
    public int SoPhieuDaHuy
    {
        get => _soPhieuDaHuy;
        set => SetProperty(ref _soPhieuDaHuy, value);
    }

    private int _soPhieuHomNay;
    public int SoPhieuHomNay
    {
        get => _soPhieuHomNay;
        set => SetProperty(ref _soPhieuHomNay, value);
    }

    // Thuộc tính lọc
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private NhanVien? _selectedNhanVienFilter;
    public NhanVien? SelectedNhanVienFilter
    {
        get => _selectedNhanVienFilter;
        set
        {
            if (SetProperty(ref _selectedNhanVienFilter, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private DateTime? _tuNgay;
    public DateTime? TuNgay
    {
        get => _tuNgay;
        set
        {
            if (SetProperty(ref _tuNgay, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private DateTime? _denNgay;
    public DateTime? DenNgay
    {
        get => _denNgay;
        set
        {
            if (SetProperty(ref _denNgay, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private string _selectedThoiGian = string.Empty;
    public string SelectedThoiGian
    {
        get => _selectedThoiGian;
        set
        {
            if (SetProperty(ref _selectedThoiGian, value))
            {
                ApplyTimeFilter(value);
            }
        }
    }

    // Trang thai filter
    public ObservableCollection<string> TrangThaiOptions { get; } = new ObservableCollection<string>
    {
        "Tất cả", "Chờ duyệt", "Đã duyệt", "Đã hủy"
    };

    private string _selectedTrangThaiFilter = "Tất cả";
    public string SelectedTrangThaiFilter
    {
        get => _selectedTrangThaiFilter;
        set
        {
            if (SetProperty(ref _selectedTrangThaiFilter, value))
            {
                if (value == "Tất cả")
                {
                    FilterChoDuyet = true;
                    FilterDaDuyet = true;
                    FilterDaHuy = true;
                }
                else if (value == "Chờ duyệt")
                {
                    FilterChoDuyet = true;
                    FilterDaDuyet = false;
                    FilterDaHuy = false;
                }
                else if (value == "Đã duyệt")
                {
                    FilterChoDuyet = false;
                    FilterDaDuyet = true;
                    FilterDaHuy = false;
                }
                else if (value == "Đã hủy")
                {
                    FilterChoDuyet = false;
                    FilterDaDuyet = false;
                    FilterDaHuy = true;
                }
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private bool _filterChoDuyet = true;
    public bool FilterChoDuyet
    {
        get => _filterChoDuyet;
        set
        {
            if (SetProperty(ref _filterChoDuyet, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private bool _filterDaDuyet = true;
    public bool FilterDaDuyet
    {
        get => _filterDaDuyet;
        set
        {
            if (SetProperty(ref _filterDaDuyet, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    private bool _filterDaHuy = true;
    public bool FilterDaHuy
    {
        get => _filterDaHuy;
        set
        {
            if (SetProperty(ref _filterDaHuy, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    // Lọc loại phiếu
    public ObservableCollection<string> FilterLoaiPhieuOptions { get; } = new ObservableCollection<string>
    {
        "Tất cả", "Xuất kho chế biến", "Xuất hủy hàng", "Xuất trả nhà cung cấp", "Xuất điều chỉnh hao hụt"
    };

    private string _selectedFilterLoaiPhieu = "Tất cả";
    public string SelectedFilterLoaiPhieu
    {
        get => _selectedFilterLoaiPhieu;
        set
        {
            if (SetProperty(ref _selectedFilterLoaiPhieu, value))
            {
                _ = LoadPhieuXuatsAsync();
            }
        }
    }

    // Thuộc tính hộp thoại
    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set
        {
            if (SetProperty(ref _isDialogOpen, value))
                OnPropertyChanged(nameof(AnyDialogOpen));
        }
    }

    private bool _isDetailDialogOpen;
    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set
        {
            if (SetProperty(ref _isDetailDialogOpen, value))
                OnPropertyChanged(nameof(AnyDialogOpen));
        }
    }

    public override bool AnyDialogOpen => IsDialogOpen || IsDetailDialogOpen;

    private bool _isCreateMode;
    public bool IsCreateMode
    {
        get => _isCreateMode;
        set
        {
            if (SetProperty(ref _isCreateMode, value))
            {
                OnPropertyChanged(nameof(IsLoaiPhieuEnabled));
            }
        }
    }

    // CHẾ ĐỘ HỦY NGUYÊN LIỆU ═══
    private bool _isHuyMode;
    public bool IsHuyMode
    {
        get => _isHuyMode;
        set
        {
            if (SetProperty(ref _isHuyMode, value))
            {
                OnPropertyChanged(nameof(IsLoaiPhieuEnabled));
            }
        }
    }

    public bool IsNotHuyMode => !IsHuyMode;

    private string? _lyDoHuy;
    public string? LyDoHuy
    {
        get => _lyDoHuy;
        set => SetProperty(ref _lyDoHuy, value);
    }

    private string _formLoaiPhieu = "Xuất kho chế biến";
    public string FormLoaiPhieu
    {
        get => _formLoaiPhieu;
        set => SetProperty(ref _formLoaiPhieu, value);
    }

    public ObservableCollection<string> LoaiPhieuOptions { get; } = new()
    {
        "Xuất kho chế biến", "Xuất hủy hàng", "Xuất trả nhà cung cấp", "Xuất điều chỉnh hao hụt"
    };

    public bool IsLoaiPhieuEnabled => !IsHuyMode && (IsCreateMode || (SelectedPhieuXuat != null && SelectedPhieuXuat.KiemKeID == null));

    private string _formDialogTitle = "LẬP PHIẾU XUẤT KHO";
    public string FormDialogTitle
    {
        get => _formDialogTitle;
        set => SetProperty(ref _formDialogTitle, value);
    }

    private string _formDialogBadge = "Mới lập (Nhập)";
    public string FormDialogBadge
    {
        get => _formDialogBadge;
        set => SetProperty(ref _formDialogBadge, value);
    }

    private string? _formGhiChu;
    public string? FormGhiChu
    {
        get => _formGhiChu;
        set => SetProperty(ref _formGhiChu, value);
    }

    // Thuộc tính form
    private ObservableCollection<CT_PhieuXuat> _chiTietForm = new();
    public ObservableCollection<CT_PhieuXuat> ChiTietForm
    {
        get => _chiTietForm;
        set => SetProperty(ref _chiTietForm, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool CanEditDelete => SelectedPhieuXuat != null &&
                                  SelectedPhieuXuat.TrangThai == 1 &&
                                  (DateTime.Now - SelectedPhieuXuat.NgayYeuCau).TotalHours <= 24;

    public bool CanApprove => SelectedPhieuXuat != null && SelectedPhieuXuat.TrangThai == 1 && IsQuanLy;

    public bool CanCancel => CanApprove || CanEditDelete;

    public bool IsCancelVisible => CanCancel;

    public bool IsEditDeleteVisible => CanEditDelete;

    // Xóa phiếu: chỉ cho phép xóa phiếu đã hủy
    public bool CanDelete => SelectedPhieuXuat != null && SelectedPhieuXuat.TrangThai == 3;

    // Hiển thị nút Xóa
    public bool IsDeleteVisible => CanDelete;

    public List<string> ThoiGianOptions { get; } = new()
    {
        "Hom nay",
        "Hom qua",
        "Tuan nay",
        "Thang nay",
        "Tat ca"
    };
    #endregion

    #region Lệnh
    public ICommand LoadDataCommand { get; }
    public ICommand CreatePhieuXuatCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand EditPhieuXuatCommand { get; }
    public ICommand DeletePhieuXuatCommand { get; }
    public ICommand ApprovePhieuXuatCommand { get; }
    public ICommand CancelPhieuXuatCommand { get; }
    public ICommand SavePhieuXuatCommand { get; }
    public ICommand CancelDialogCommand { get; }
    public ICommand CloseDetailDialogCommand { get; }
    public ICommand AddNguyenLieuCommand { get; }
    public ICommand RemoveChiTietCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand PrintPhieuXuatCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand AddEmptyChiTietCommand { get; }
    #endregion

    public PhieuXuatViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        // Nhận biết người dùng hiện tại là nhân viên (theo tên chức vụ, không dùng ID vì tự động tăng)
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        var chucVuTen = (currentUser?.NhanVien?.ChucVu?.TenChucVu?.Trim() ?? "").ToLower();
        bool isQuanLy = chucVuTen.Contains("quản lý") || chucVuTen.Contains("quan ly");
        bool isNhanVienBep = !isQuanLy && (chucVuTen.Contains("bếp") || chucVuTen.Contains("bep"));
        bool isNhanVienKho = !isQuanLy && !isNhanVienBep && chucVuTen.Contains("kho");
        IsNhanVien = isNhanVienBep || isNhanVienKho;
        IsNhanVienBep = isNhanVienBep;
        CurrentUserName = currentUser?.NhanVien?.HoTen ?? "Nhân viên";

        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        CreatePhieuXuatCommand = new RelayCommand(_ => OpenCreateDialog());
        ViewDetailCommand = new AsyncRelayCommand(async p => await ViewDetailAsync(p));
        EditPhieuXuatCommand = new AsyncRelayCommand(async p => await EditPhieuXuatAsync(p));
        DeletePhieuXuatCommand = new AsyncRelayCommand(async p => await DeletePhieuXuatAsync(p));
        ApprovePhieuXuatCommand = new AsyncRelayCommand(async _ => await ApprovePhieuXuatAsync());
        CancelPhieuXuatCommand = new AsyncRelayCommand(async _ => await CancelPhieuXuatAsync());
        SavePhieuXuatCommand = new AsyncRelayCommand(async _ => await SavePhieuXuatAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        CloseDetailDialogCommand = new RelayCommand(_ => IsDetailDialogOpen = false);
        AddNguyenLieuCommand = new RelayCommand(p => AddNguyenLieuToForm(p));
        RemoveChiTietCommand = new RelayCommand(p => RemoveChiTietFromForm(p));
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        PrintPhieuXuatCommand = new AsyncRelayCommand(async p => await PrintPhieuXuatAsync(p));
        IncreaseQuantityCommand = new RelayCommand(p => IncreaseQuantity(p));
        DecreaseQuantityCommand = new RelayCommand(p => DecreaseQuantity(p));
        AddEmptyChiTietCommand = new RelayCommand(_ => AddEmptyChiTiet());

        // Tải dữ liệu khi khởi tạo
        SafeInitializeAsync(() => LoadDataAsync(true));
    }

    private void IncreaseQuantity(object? parameter)
    {
        if (parameter is CT_PhieuXuat chiTiet)
        {
            chiTiet.SoLuong++;
        }
    }

    private void DecreaseQuantity(object? parameter)
    {
        if (parameter is CT_PhieuXuat chiTiet && chiTiet.SoLuong > 1)
        {
            chiTiet.SoLuong--;
        }
    }

    #region Phương thức
    private async Task LoadDataAsync(bool isInitialLoad = false)
    {
        IsLoading = true;
        try
        {
            var nhanViens = await _databaseService.GetNhanViensAsync();
            NhanViens = new ObservableCollection<NhanVien>(nhanViens);

            var donViTinhs = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs = new ObservableCollection<DonViTinh>(donViTinhs);

            var nguyenLieus = await _databaseService.GetNguyenLieusWithTonKhoAsync();
            NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);

            await LoadPhieuXuatsAsync();

            if (isInitialLoad && IsNhanVien)
            {
                OpenCreateDialog();
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPhieuXuatsAsync()
    {
        try
        {
            var trangThaiFilter = new List<byte>();
            if (FilterChoDuyet) trangThaiFilter.Add(1);
            if (FilterDaDuyet) trangThaiFilter.Add(2);
            if (FilterDaHuy) trangThaiFilter.Add(3);

            var phieuXuats = await _databaseService.GetPhieuXuatsAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                SelectedNhanVienFilter?.NhanVienID,
                TuNgay,
                DenNgay,
                trangThaiFilter.Any() ? trangThaiFilter : null);

            // Nhân viên chỉ thấy phiếu của mình, quản lý thấy tất cả
            if (IsNhanVien)
            {
                var currentUser = CurrentUserSession.Instance.CurrentUser;
                if (currentUser != null && currentUser.NhanVienID.HasValue)
                {
                    phieuXuats = phieuXuats.Where(p => p.NhanVienYeuID == currentUser.NhanVienID.Value).ToList();
                }
            }

            PhieuXuats = new ObservableCollection<PhieuXuat>(phieuXuats);

            // Filter theo loại phiếu (client-side)
            if (!string.IsNullOrEmpty(SelectedFilterLoaiPhieu) && SelectedFilterLoaiPhieu != "Tất cả")
            {
                phieuXuats = phieuXuats.Where(p => p.LoaiPhieu == SelectedFilterLoaiPhieu).ToList();
                PhieuXuats = new ObservableCollection<PhieuXuat>(phieuXuats);
            }

            SoPhieuChoDuyet = phieuXuats.Count(p => p.TrangThai == 1);
            SoPhieuDaDuyet = phieuXuats.Count(p => p.TrangThai == 2);
            SoPhieuDaHuy = phieuXuats.Count(p => p.TrangThai == 3);
            SoPhieuHomNay = phieuXuats.Count(p => p.NgayYeuCau.Date == DateTime.Today);
        }
        catch { }
    }

    private void ApplyTimeFilter(string thoiGian)
    {
        var today = DateTime.Today;
        switch (thoiGian)
        {
            case "Hom nay":
                TuNgay = today;
                DenNgay = today;
                break;
            case "Hom qua":
                TuNgay = today.AddDays(-1);
                DenNgay = today.AddDays(-1);
                break;
            case "Tuan nay":
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
                TuNgay = startOfWeek;
                DenNgay = today;
                break;
            case "Thang nay":
                TuNgay = new DateTime(today.Year, today.Month, 1);
                DenNgay = today;
                break;
            case "Tat ca":
                TuNgay = null;
                DenNgay = null;
                break;
        }
    }

    private void ClearFilter()
    {
        SearchText = string.Empty;
        SelectedNhanVienFilter = null;
        SelectedThoiGian = string.Empty;
        TuNgay = null;
        DenNgay = null;
        SelectedTrangThaiFilter = "Tất cả";
        SelectedFilterLoaiPhieu = "Tất cả";
        FilterChoDuyet = true;
        FilterDaDuyet = true;
        FilterDaHuy = true;
    }

    private async void OpenCreateDialog()
    {
        IsCreateMode = true;
        IsHuyMode = false;
        FormLoaiPhieu = "Xuất kho chế biến";
        OnPropertyChanged(nameof(IsNotHuyMode));
        FormDialogTitle = "LẬP PHIẾU XUẤT KHO";
        FormDialogBadge = "Mới lập (Nhập)";
        LyDoHuy = null;
        FormGhiChu = null;
        ChiTietForm = new ObservableCollection<CT_PhieuXuat>();

        // Tải lại nguyên liệu với tồn kho mới nhất
        var nguyenLieus = await _databaseService.GetNguyenLieusWithTonKhoAsync();
        NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);

        IsDialogOpen = true;
    }


    private void AddEmptyChiTiet()
    {
        var chiTiet = new CT_PhieuXuat
        {
            SoLuong = 1
        };
        chiTiet.PropertyChanged += ChiTiet_PropertyChanged;
        ChiTietForm.Add(chiTiet);
    }

    private async void ChiTiet_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is CT_PhieuXuat chiTiet && e.PropertyName == nameof(CT_PhieuXuat.NguyenLieu))
        {
            if (chiTiet.NguyenLieu != null)
            {
                var nguyenLieu = chiTiet.NguyenLieu;
                chiTiet.NguyenLieuID = nguyenLieu.NguyenLieuID;

                // Load danh sách đơn vị quy đổi
                var donViOptions = new System.Collections.ObjectModel.ObservableCollection<DonViOption>();
                var defaultOption = new DonViOption
                {
                    DonViID = nguyenLieu.DonViID ?? 0,
                    TenDonVi = nguyenLieu.DonViTinh?.TenDonVi ?? "",
                    HeSo = 1
                };

                try
                {
                    var quyDois = await _databaseService.GetQuyDoiDonVisAsync(nguyenLieu.NguyenLieuID);
                    var donViXuat = quyDois.FirstOrDefault(q => q.LaDonViChuan);
                    if (donViXuat != null)
                    {
                        defaultOption = new DonViOption
                        {
                            DonViID = donViXuat.DonViID ?? 0,
                            TenDonVi = donViXuat.DonViTinh?.TenDonVi ?? "",
                            HeSo = donViXuat.HeSo
                        };
                    }
                    donViOptions.Add(defaultOption);
                    foreach (var qd in quyDois)
                    {
                        if (qd.DonViID == defaultOption.DonViID) continue;
                        donViOptions.Add(new DonViOption
                        {
                            DonViID = qd.DonViID ?? 0,
                            TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                            HeSo = qd.HeSo
                        });
                    }
                }
                catch {
                    if (!donViOptions.Any()) donViOptions.Add(defaultOption);
                }

                chiTiet.DonViOptions = donViOptions;
                chiTiet.SelectedDonVi = defaultOption;
            }
        }
    }

    private async void AddNguyenLieuToForm(object? parameter)
    {
        if (parameter is NguyenLieu nguyenLieu)
        {
            // Kiểm tra nếu đã tồn tại
            if (ChiTietForm.Any(ct => ct.NguyenLieuID == nguyenLieu.NguyenLieuID))
            {
                return;
            }

            // Load danh sách đơn vị quy đổi
            var donViOptions = new System.Collections.ObjectModel.ObservableCollection<DonViOption>();

            var defaultOption = new DonViOption
            {
                DonViID = nguyenLieu.DonViID ?? 0,
                TenDonVi = nguyenLieu.DonViTinh?.TenDonVi ?? "",
                HeSo = 1
            };

            try
            {
                var quyDois = await _databaseService.GetQuyDoiDonVisAsync(nguyenLieu.NguyenLieuID);
                var donViXuat = quyDois.FirstOrDefault(q => q.LaDonViChuan);

                if (donViXuat != null)
                {
                    defaultOption = new DonViOption
                    {
                        DonViID = donViXuat.DonViID ?? 0,
                        TenDonVi = donViXuat.DonViTinh?.TenDonVi ?? "",
                        HeSo = donViXuat.HeSo
                    };
                }

                donViOptions.Add(defaultOption);

                foreach (var qd in quyDois)
                {
                    if (qd.DonViID == defaultOption.DonViID) continue;
                    donViOptions.Add(new DonViOption
                    {
                        DonViID = qd.DonViID ?? 0,
                        TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                        HeSo = qd.HeSo
                    });
                }
            }
            catch {
                if (!donViOptions.Any()) donViOptions.Add(defaultOption);
            }

            var chiTiet = new CT_PhieuXuat
            {
                NguyenLieuID = nguyenLieu.NguyenLieuID,
                NguyenLieu = nguyenLieu,
                SoLuong = 1,
                DonViID = defaultOption.DonViID,
                DonViTinh = new DonViTinh { DonViID = defaultOption.DonViID, TenDonVi = defaultOption.TenDonVi },
                HeSo = defaultOption.HeSo,
                DonViOptions = donViOptions
            };
            chiTiet.SelectedDonVi = defaultOption;

            chiTiet.PropertyChanged += ChiTiet_PropertyChanged;
            ChiTietForm.Add(chiTiet);
        }
    }

    private void RemoveChiTietFromForm(object? parameter)
    {
        if (parameter is CT_PhieuXuat chiTiet)
        {
            chiTiet.PropertyChanged -= ChiTiet_PropertyChanged;
            ChiTietForm.Remove(chiTiet);
        }
    }

    private async Task ViewDetailAsync(object? parameter)
    {
        int phieuXuatId = 0;

        if (parameter is PhieuXuat phieuXuat)
        {
            phieuXuatId = phieuXuat.PhieuXuatID;
        }
        else if (parameter is int id)
        {
            phieuXuatId = id;
        }

        if (phieuXuatId == 0) return;

        try
        {
            SelectedPhieuXuat = await _databaseService.GetPhieuXuatByIdAsync(phieuXuatId);
            var chiTiets = await _databaseService.GetChiTietPhieuXuatAsync(phieuXuatId);
            ChiTietPhieuXuats = new ObservableCollection<CT_PhieuXuat>(chiTiets);
            IsDetailDialogOpen = true;
        }
        catch { }
    }

    private async Task EditPhieuXuatAsync(object? parameter)
    {
        if (SelectedPhieuXuat == null || !CanEditDelete) return;

        IsCreateMode = false;

        // Tải lại nguyên liệu trước
        var nguyenLieus = await _databaseService.GetNguyenLieusWithTonKhoAsync();
        NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);

        var chiTiets = await _databaseService.GetChiTietPhieuXuatAsync(SelectedPhieuXuat.PhieuXuatID);

        // Tái tạo lại tham chiếu NguyenLieu và DonViOptions cho form
        foreach (var ct in chiTiets)
        {
            var matchingNl = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == ct.NguyenLieuID);
            if (matchingNl != null)
            {
                ct.NguyenLieu = matchingNl;

                var donViOptions = new System.Collections.ObjectModel.ObservableCollection<DonViOption>();
                var defaultOption = new DonViOption
                {
                    DonViID = matchingNl.DonViID ?? 0,
                    TenDonVi = matchingNl.DonViTinh?.TenDonVi ?? "",
                    HeSo = 1
                };

                try
                {
                    var quyDois = await _databaseService.GetQuyDoiDonVisAsync(matchingNl.NguyenLieuID);
                    var donViXuat = quyDois.FirstOrDefault(q => q.LaDonViChuan);
                    if (donViXuat != null)
                    {
                        defaultOption = new DonViOption
                        {
                            DonViID = donViXuat.DonViID ?? 0,
                            TenDonVi = donViXuat.DonViTinh?.TenDonVi ?? "",
                            HeSo = donViXuat.HeSo
                        };
                    }

                    donViOptions.Add(defaultOption);
                    foreach (var qd in quyDois)
                    {
                        if (qd.DonViID == defaultOption.DonViID) continue;
                        donViOptions.Add(new DonViOption
                        {
                            DonViID = qd.DonViID ?? 0,
                            TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                            HeSo = qd.HeSo
                        });
                    }
                }
                catch
                {
                    if (!donViOptions.Any()) donViOptions.Add(defaultOption);
                }

                ct.DonViOptions = donViOptions;
                var currentDonViOption = donViOptions.FirstOrDefault(d => d.DonViID == ct.DonViID);
                if (currentDonViOption != null)
                {
                    ct.SelectedDonVi = currentDonViOption;
                }
                else
                {
                    ct.SelectedDonVi = defaultOption;
                    ct.DonViID = defaultOption.DonViID;
                    ct.HeSo = defaultOption.HeSo;
                }
            }
        }

        ChiTietForm = new ObservableCollection<CT_PhieuXuat>(chiTiets);
        FormLoaiPhieu = SelectedPhieuXuat.LoaiPhieu ?? "Xuất kho chế biến";
        FormGhiChu = SelectedPhieuXuat.GhiChu;

        IsDetailDialogOpen = false;
        IsDialogOpen = true;
    }

    private async Task DeletePhieuXuatAsync(object? parameter)
    {
        if (SelectedPhieuXuat == null || !CanDelete) return;

        var confirmed = await ShowDeleteConfirmation(
            SelectedPhieuXuat.MaPhieuXuat ?? "",
            "Xóa phiếu xuất",
            $"Bạn có chắc chắn muốn xóa phiếu xuất \"{SelectedPhieuXuat.MaPhieuXuat}\"?\nHành động này không thể hoàn tác.");
        if (!confirmed) return;

        try
        {
            var result = await _databaseService.DeletePhieuXuatAsync(SelectedPhieuXuat.PhieuXuatID);
            if (result)
            {
                await LoadPhieuXuatsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuXuat = null;
            }
        }
        catch { }
    }

    private async Task ApprovePhieuXuatAsync()
    {
        if (SelectedPhieuXuat == null || !CanApprove) return;

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;
            if (currentUser?.NhanVienID == null) return;

            // === PRE-CHECK: Kiểm tra tồn kho trước khi duyệt ===
            var chiTiets = await _databaseService.GetChiTietPhieuXuatAsync(SelectedPhieuXuat.PhieuXuatID);
            if (chiTiets.Any())
            {
                // Dùng GetTonKhosAsync để lấy TẤT CẢ nguyên liệu (kể cả tồn = 0)
                var tonKhos = await _databaseService.GetTonKhosAsync();
                var insufficientItems = new List<string>();

                var chiTietGroups = chiTiets.GroupBy(x => x.NguyenLieuID);
                foreach (var group in chiTietGroups)
                {
                    var nlId = group.Key ?? 0;
                    var tk = tonKhos.FirstOrDefault(t => t.NguyenLieuID == nlId);
                    var tonKhoHienTai = tk?.SoLuongTon ?? 0;
                    var tenNL = tk?.NguyenLieu?.TenNguyenLieu ?? group.First().NguyenLieu?.TenNguyenLieu ?? $"ID {nlId}";
                    var donVi = tk?.NguyenLieu?.DonViTinh?.TenDonVi ?? "";

                    // Lấy hệ số của đơn vị chuẩn để quy đổi
                    decimal donViChuanHeSo = 1m;
                    try
                    {
                        var quyDois = await _databaseService.GetQuyDoiDonVisAsync(nlId);
                        var donViChuan = quyDois.FirstOrDefault(q => q.LaDonViChuan);
                        if (donViChuan != null && donViChuan.HeSo > 0)
                        {
                            donViChuanHeSo = donViChuan.HeSo;
                        }
                    }
                    catch { }

                    // Tính tổng số lượng xuất (quy đổi về đơn vị chuẩn)
                    decimal tongSoLuong = group.Sum(x => x.SoLuong * x.HeSo / donViChuanHeSo);

                    if (tongSoLuong > tonKhoHienTai)
                    {
                        insufficientItems.Add($"• {tenNL}: cần {tongSoLuong:N2} {donVi}, tồn kho chỉ còn {tonKhoHienTai:N2} {donVi}");
                    }
                }

                if (insufficientItems.Any())
                {
                    var message = "Không thể duyệt phiếu xuất vì tồn kho không đủ:\n\n"
                        + string.Join("\n", insufficientItems)
                        + "\n\nVui lòng kiểm tra và điều chỉnh số lượng tồn kho trước khi duyệt.";

                    System.Windows.MessageBox.Show(
                        message,
                        "Không đủ tồn kho",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
            }

            // Mở popup chữ ký số để quản lý ký duyệt
            var nguoiDuyet = currentUser.NhanVien?.HoTen ?? "Quản lý";
            var maPhieu = SelectedPhieuXuat.MaPhieuXuat ?? $"PX-{SelectedPhieuXuat.PhieuXuatID}";

            var signaturePopup = new Views.SignaturePopup(maPhieu, nguoiDuyet);
            var dialogResult = signaturePopup.ShowDialog();

            if (dialogResult != true || string.IsNullOrEmpty(signaturePopup.SignatureBase64))
                return; // Hủy duyệt nếu không ký

            var result = await _databaseService.ApprovePhieuXuatAsync(
                SelectedPhieuXuat.PhieuXuatID,
                currentUser.NhanVienID.Value,
                signaturePopup.SignatureBase64);

            if (result)
            {
                await LoadPhieuXuatsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuXuat = null;
            }
        }
        catch (Exception ex) {
            System.Windows.MessageBox.Show(
                $"Lỗi khi duyệt phiếu xuất:\n\n{ex.Message}",
                "Lỗi khi duyệt phiếu",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task CancelPhieuXuatAsync()
    {
        if (SelectedPhieuXuat == null || SelectedPhieuXuat.TrangThai != 1) return;

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;
            if (currentUser?.NhanVienID == null) return;

            // Yêu cầu nhập lý do hủy
            var inputDialog = new System.Windows.Window
            {
                Title = "Lý do hủy phiếu xuất",
                Width = 420, Height = 200,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize
            };
            var sp = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(20) };
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Vui lòng nhập lý do hủy phiếu:",
                FontSize = 14, Margin = new System.Windows.Thickness(0, 0, 0, 10)
            });
            var tbInput = new System.Windows.Controls.TextBox
            {
                FontSize = 13, Padding = new System.Windows.Thickness(8, 6, 8, 6),
                AcceptsReturn = false
            };
            sp.Children.Add(tbInput);
            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(0, 16, 0, 0)
            };
            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Xác nhận hủy", Width = 120, Height = 32,
                Margin = new System.Windows.Thickness(0, 0, 8, 0), IsDefault = true
            };
            btnOk.Click += (s, e) => { inputDialog.DialogResult = true; };
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Quay lại", Width = 80, Height = 32, IsCancel = true
            };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            sp.Children.Add(btnPanel);
            inputDialog.Content = sp;

            if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(tbInput.Text)) return;
            var lyDoHuy = tbInput.Text.Trim();

            // Yêu cầu chữ ký xác nhận hủy
            var nguoiHuy = currentUser.NhanVien?.HoTen ?? "Nhân viên";
            var maPhieu = SelectedPhieuXuat.MaPhieuXuat ?? $"PX-{SelectedPhieuXuat.PhieuXuatID}";
            var signaturePopup = new Views.SignaturePopup(maPhieu, nguoiHuy);
            signaturePopup.Owner = System.Windows.Application.Current.MainWindow;

            if (signaturePopup.ShowDialog() != true || string.IsNullOrEmpty(signaturePopup.SignatureBase64))
                return;

            var result = await _databaseService.CancelPhieuXuatAsync(
                SelectedPhieuXuat.PhieuXuatID, currentUser.NhanVienID.Value, lyDoHuy, signaturePopup.SignatureBase64);
            if (result)
            {
                await LoadPhieuXuatsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuXuat = null;
            }
        }
        catch { }
    }

    private async Task SavePhieuXuatAsync()
    {
        if (!ChiTietForm.Any())
        {
            return;
        }

        if (ChiTietForm.Any(ct => ct.SoLuong <= 0))
        {
            System.Windows.MessageBox.Show("Số lượng nguyên liệu xuất phải lớn hơn 0!", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Validation cho phiếu hủy: bắt buộc nhập lý do
        if (IsHuyMode && string.IsNullOrWhiteSpace(LyDoHuy))
        {
            System.Windows.MessageBox.Show(
                "Vui lòng nhập lý do hủy hàng.",
                "Thiếu thông tin", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Đã gỡ bỏ: Kiểm tra ràng buộc bắt buộc phải là Đơn vị chuẩn
        // Hệ thống hiện tại đã hỗ trợ tự động quy đổi khi duyệt phiếu xuất thông qua hàm ConvertAmountToStockUnitAsync

        // Ràng buộc kiểm tra tồn kho: Số lượng xuất không được lớn hơn tồn kho
        // Tồn kho lưu theo đơn vị chuẩn (LaDonViChuan) → cần quy đổi số lượng xuất về cùng đơn vị chuẩn
        var chiTietGroups = ChiTietForm.GroupBy(x => x.NguyenLieuID);
        foreach (var group in chiTietGroups)
        {
            var firstItem = group.First();

            // Tìm kiếm đối tượng NguyenLieu có chứa Tồn Kho mới nhất
            var nl = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == firstItem.NguyenLieuID) ?? firstItem.NguyenLieu;
            var tonKhoHienTai = nl?.TonKho?.SoLuongTon ?? 0;

            // Tìm hệ số của đơn vị chuẩn (đơn vị lưu tồn kho = đơn vị mặc định = phần tử đầu tiên trong DonViOptions)
            decimal donViChuanHeSo = 1m;
            if (firstItem.DonViOptions != null && firstItem.DonViOptions.Any())
            {
                // Đơn vị chuẩn = đơn vị đầu tiên (là đơn vị LaDonViChuan, được set ở AddNguyenLieuToForm)
                var donViChuan = firstItem.DonViOptions.FirstOrDefault();
                if (donViChuan != null && donViChuan.HeSo > 0)
                    donViChuanHeSo = donViChuan.HeSo;
            }

            // Quy đổi tổng số lượng xuất về đơn vị chuẩn
            var tongSoLuongChuanHoa = group.Sum(x => x.SoLuong * x.HeSo / donViChuanHeSo);

            // Lấy tên đơn vị chuẩn để hiển thị
            var tenDonViChuan = firstItem.DonViOptions?.FirstOrDefault()?.TenDonVi ?? nl?.DonViTinh?.TenDonVi ?? "";

            if (tongSoLuongChuanHoa > tonKhoHienTai)
            {
                System.Windows.MessageBox.Show(
                    $"Số lượng xuất của '{nl?.TenNguyenLieu}' ({tongSoLuongChuanHoa:N2} {tenDonViChuan}) đang vượt mức tồn kho hiện tại ({tonKhoHienTai:N2} {tenDonViChuan}).\nVui lòng kiểm tra và điều chỉnh lại!",
                    "Cảnh báo tồn kho",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;

            var phieuXuat = new PhieuXuat
            {
                NhanVienYeuID = currentUser?.NhanVienID,
                NgayYeuCau = DateTime.Now,
                TrangThai = 1, // Cho duyet
                LoaiPhieu = FormLoaiPhieu,
                KiemKeID = IsCreateMode ? null : SelectedPhieuXuat?.KiemKeID,
                GhiChu = BuildGhiChu()
            };

            string maPhieuForSign;
            if (IsCreateMode)
            {
                if (IsHuyMode)
                {
                    var maXuat = await _databaseService.GenerateMaPhieuXuatAsync();
                    maPhieuForSign = maXuat?.Replace("PX-", "XH-") ?? "XH";
                }
                else
                {
                    maPhieuForSign = await _databaseService.GenerateMaPhieuXuatAsync();
                }
            }
            else
            {
                maPhieuForSign = SelectedPhieuXuat?.MaPhieuXuat ?? "PX";
            }

            // Mở popup ký tên xác nhận (ChuKyXuat) cho nhân viên gửi phiếu
            var nguoiXuat = currentUser?.NhanVien?.HoTen ?? "Nhân viên";
            var signaturePopup = new Views.SignaturePopup(maPhieuForSign, nguoiXuat);
            signaturePopup.Owner = System.Windows.Application.Current.MainWindow;
            if (signaturePopup.ShowDialog() == true)
            {
                phieuXuat.ChuKyXuat = signaturePopup.SignatureBase64;
            }
            else
            {
                return; // Hủy nếu không ký
            }

            if (IsCreateMode)
            {
                phieuXuat.MaPhieuXuat = maPhieuForSign;
            }
            else
            {
                phieuXuat.PhieuXuatID = SelectedPhieuXuat!.PhieuXuatID;
                phieuXuat.MaPhieuXuat = SelectedPhieuXuat.MaPhieuXuat;
                phieuXuat.NgayYeuCau = SelectedPhieuXuat.NgayYeuCau;
            }

            await _databaseService.SavePhieuXuatAsync(phieuXuat, ChiTietForm.ToList());

            CloseDialog();
            await LoadPhieuXuatsAsync();
        }
        catch { }
    }

    private string? BuildGhiChu()
    {
        var parts = new List<string>();
        if (IsHuyMode && !string.IsNullOrWhiteSpace(LyDoHuy))
            parts.Add($"Lý do hủy: {LyDoHuy.Trim()}");
        if (!string.IsNullOrWhiteSpace(FormGhiChu))
            parts.Add(FormGhiChu.Trim());
        return parts.Any() ? string.Join(" | ", parts) : null;
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        ChiTietForm.Clear();
    }

    public string GetTrangThaiText(byte trangThai)
    {
        return trangThai switch
        {
            1 => "Chờ duyệt",
            2 => "Đã xuất kho",
            3 => "Đã hủy",
            _ => "Khong xac dinh"
        };
    }

    public string GetTrangThaiColor(byte trangThai)
    {
        return trangThai switch
        {
            1 => "#FEF3CD", // Cảnh báo - Chờ duyệt
            2 => "#D1FAE5", // Thành công - Đã duyệt
            3 => "#FEE2E2", // Nguy hiểm - Đã hủy
            _ => "#F1F5F9"
        };
    }

    public string GetTrangThaiForeground(byte trangThai)
    {
        return trangThai switch
        {
            1 => "#92400E",
            2 => "#059669",
            3 => "#DC2626",
            _ => "#64748B"
        };
    }

    private async Task PrintPhieuXuatAsync(object? parameter)
    {
        PhieuXuat? phieuXuat = null;

        if (parameter is PhieuXuat px)
        {
            phieuXuat = px;
        }
        else if (SelectedPhieuXuat != null)
        {
            phieuXuat = SelectedPhieuXuat;
        }

        if (phieuXuat == null) return;

        try
        {
            // Load chi tiết nếu chưa có
            var chiTiets = await _databaseService.GetChiTietPhieuXuatAsync(phieuXuat.PhieuXuatID);

            // Gọi PrintService
            PrintService.PrintPhieuXuat(phieuXuat, chiTiets);
        }
        catch { }
    }
    #endregion
}
