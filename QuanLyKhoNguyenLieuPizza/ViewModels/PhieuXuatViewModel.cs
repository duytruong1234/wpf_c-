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
        set => SetProperty(ref _isCreateMode, value);
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
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

            SoPhieuChoDuyet = phieuXuats.Count(p => p.TrangThai == 1);
            SoPhieuDaDuyet = phieuXuats.Count(p => p.TrangThai == 2);
            SoPhieuDaHuy = phieuXuats.Count(p => p.TrangThai == 3);
            SoPhieuHomNay = phieuXuats.Count(p => p.NgayYeuCau.Date == DateTime.Today);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PhieuXuats: {ex.Message}");
        }
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
        FilterChoDuyet = true;
        FilterDaDuyet = true;
        FilterDaHuy = true;
    }

    private async void OpenCreateDialog()
    {
        IsCreateMode = true;
        ChiTietForm = new ObservableCollection<CT_PhieuXuat>();
        
        // Tải lại nguyên liệu với tồn kho mới nhất
        var nguyenLieus = await _databaseService.GetNguyenLieusWithTonKhoAsync();
        NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);
        
        IsDialogOpen = true;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading QuyDoiDonVi: {ex.Message}");
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

            ChiTietForm.Add(chiTiet);
        }
    }

    private void RemoveChiTietFromForm(object? parameter)
    {
        if (parameter is CT_PhieuXuat chiTiet)
        {
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading detail: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting PhieuXuat: {ex.Message}");
        }
    }

    private async Task ApprovePhieuXuatAsync()
    {
        if (SelectedPhieuXuat == null || !CanApprove) return;

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;
            if (currentUser?.NhanVienID == null) return;

            var result = await _databaseService.ApprovePhieuXuatAsync(
                SelectedPhieuXuat.PhieuXuatID, 
                currentUser.NhanVienID.Value);
                
            if (result)
            {
                await LoadPhieuXuatsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuXuat = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error approving PhieuXuat: {ex.Message}");
            System.Windows.MessageBox.Show(ex.Message, "Lỗi khi duyệt phiếu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task CancelPhieuXuatAsync()
    {
        if (SelectedPhieuXuat == null || SelectedPhieuXuat.TrangThai != 1) return;

        try
        {
            var result = await _databaseService.CancelPhieuXuatAsync(SelectedPhieuXuat.PhieuXuatID);
            if (result)
            {
                await LoadPhieuXuatsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuXuat = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling PhieuXuat: {ex.Message}");
        }
    }

    private async Task SavePhieuXuatAsync()
    {
        if (!ChiTietForm.Any())
        {
            return;
        }

        // Đã gỡ bỏ: Kiểm tra ràng buộc bắt buộc phải là Đơn vị chuẩn
        // Hệ thống hiện tại đã hỗ trợ tự động quy đổi khi duyệt phiếu xuất thông qua hàm ConvertAmountToStockUnitAsync

        // Ràng buộc kiểm tra tồn kho: Số lượng xuất không được lớn hơn tồn kho
        // Tồn kho lưu theo đơn vị chuẩn → cần quy đổi số lượng xuất về cùng đơn vị chuẩn
        var chiTietGroups = ChiTietForm.GroupBy(x => x.NguyenLieuID);
        foreach (var group in chiTietGroups)
        {
            var firstItem = group.First();
            
            // Tìm kiếm đối tượng NguyenLieu có chứa Tồn Kho mới nhất 
            var nl = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == firstItem.NguyenLieuID) ?? firstItem.NguyenLieu;
            var tonKhoHienTai = nl?.TonKho?.SoLuongTon ?? 0;

            // Tìm hệ số của đơn vị chuẩn (đơn vị lưu tồn kho)
            decimal donViChuanHeSo = 1m;
            if (firstItem.DonViOptions != null && firstItem.DonViOptions.Any())
            {
                // Đơn vị chuẩn = đơn vị có DonViID trùng với đơn vị gốc của nguyên liệu
                var donViGoc = firstItem.DonViOptions.FirstOrDefault(o => o.DonViID == (firstItem.NguyenLieu?.DonViID ?? 0));
                if (donViGoc != null && donViGoc.HeSo > 0)
                    donViChuanHeSo = donViGoc.HeSo;
            }

            // Quy đổi tổng số lượng xuất về đơn vị chuẩn
            var tongSoLuongChuanHoa = group.Sum(x => x.SoLuong * x.HeSo / donViChuanHeSo);
            
            if (tongSoLuongChuanHoa > tonKhoHienTai)
            {
                System.Windows.MessageBox.Show(
                    $"Số lượng xuất của '{nl?.TenNguyenLieu}' ({tongSoLuongChuanHoa:N2} {nl?.DonViTinh?.TenDonVi}) đang vượt mức tồn kho hiện tại ({tonKhoHienTai:N2} {nl?.DonViTinh?.TenDonVi}).\nVui lòng kiểm tra và điều chỉnh lại!", 
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
                TrangThai = 1 // Cho duyet
            };

            if (IsCreateMode)
            {
                phieuXuat.MaPhieuXuat = await _databaseService.GenerateMaPhieuXuatAsync();
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving PhieuXuat: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error printing PhieuXuat: {ex.Message}");
        }
    }
    #endregion
}


