using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class PhieuNhapViewModel : BaseViewModel
{
    // ⚡ Sử dụng interface thay vì concrete class — dùng chung singleton từ ServiceLocator
    private readonly IDatabaseService _databaseService;

    #region Thuộc tính
    // Thuộc tính theo vai trò
    private bool _isNhanVienKho;
    public bool IsNhanVienKho
    {
        get => _isNhanVienKho;
        set => SetProperty(ref _isNhanVienKho, value);
    }

    public bool IsQuanLy => !IsNhanVienKho;

    private string _currentUserName = string.Empty;
    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    private ObservableCollection<PhieuNhap> _phieuNhaps = new();
    public ObservableCollection<PhieuNhap> PhieuNhaps
    {
        get => _phieuNhaps;
        set
        {
            if (SetProperty(ref _phieuNhaps, value))
            {
                OnPropertyChanged(nameof(CountChoDuyet));
                OnPropertyChanged(nameof(CountDaDuyet));
            }
        }
    }

    public int CountChoDuyet => PhieuNhaps?.Count(p => p.TrangThai == 1) ?? 0;
    public int CountDaDuyet => PhieuNhaps?.Count(p => p.TrangThai == 2) ?? 0;

    private PhieuNhap? _selectedPhieuNhap;
    public PhieuNhap? SelectedPhieuNhap
    {
        get => _selectedPhieuNhap;
        set
        {
            if (SetProperty(ref _selectedPhieuNhap, value))
            {
                OnPropertyChanged(nameof(CanEditDelete));
                OnPropertyChanged(nameof(CanApprove));
            }
        }
    }

    private ObservableCollection<CT_PhieuNhap> _chiTietPhieuNhaps = new();
    public ObservableCollection<CT_PhieuNhap> ChiTietPhieuNhaps
    {
        get => _chiTietPhieuNhaps;
        set => SetProperty(ref _chiTietPhieuNhaps, value);
    }

    private ObservableCollection<NhaCungCap> _nhaCungCaps = new();
    public ObservableCollection<NhaCungCap> NhaCungCaps
    {
        get => _nhaCungCaps;
        set => SetProperty(ref _nhaCungCaps, value);
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

    // Thuộc tính lọc
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value) && !IsBatchUpdating)
            {
                // ⚡ Debounce: chờ 300ms sau lần gõ cuối mới gọi DB
                // "pizza" = 1 DB call thay vì 5 ("p","pi","piz","pizz","pizza")
                _ = DebounceAsync(LoadPhieuNhapsAsync);
            }
        }
    }

    private NhanVien? _selectedNhanVienFilter;
    public NhanVien? SelectedNhanVienFilter
    {
        get => _selectedNhanVienFilter;
        set
        {
            if (SetProperty(ref _selectedNhanVienFilter, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private NhaCungCap? _selectedNhaCungCapFilter;
    public NhaCungCap? SelectedNhaCungCapFilter
    {
        get => _selectedNhaCungCapFilter;
        set
        {
            if (SetProperty(ref _selectedNhaCungCapFilter, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private DateTime? _tuNgay;
    public DateTime? TuNgay
    {
        get => _tuNgay;
        set
        {
            if (SetProperty(ref _tuNgay, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private DateTime? _denNgay;
    public DateTime? DenNgay
    {
        get => _denNgay;
        set
        {
            if (SetProperty(ref _denNgay, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
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

    // Lọc trạng thái
    private bool _filterChoDuyet = true;
    public bool FilterChoDuyet
    {
        get => _filterChoDuyet;
        set
        {
            if (SetProperty(ref _filterChoDuyet, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private bool _filterDaDuyet = true;
    public bool FilterDaDuyet
    {
        get => _filterDaDuyet;
        set
        {
            if (SetProperty(ref _filterDaDuyet, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private bool _filterDaHuy;
    public bool FilterDaHuy
    {
        get => _filterDaHuy;
        set
        {
            if (SetProperty(ref _filterDaHuy, value) && !IsBatchUpdating)
            {
                _ = LoadPhieuNhapsAsync();
            }
        }
    }

    private decimal _tongTien;
    public decimal TongTien
    {
        get => _tongTien;
        set => SetProperty(ref _tongTien, value);
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

    // Thuộc tính hộp thoại
    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    private bool _isDetailDialogOpen;
    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    private bool _isCreateMode;
    public bool IsCreateMode
    {
        get => _isCreateMode;
        set => SetProperty(ref _isCreateMode, value);
    }

    // Thuộc tính form tạo/sửa
    private NhaCungCap? _selectedNhaCungCapForm;
    public NhaCungCap? SelectedNhaCungCapForm
    {
        get => _selectedNhaCungCapForm;
        set
        {
            if (SetProperty(ref _selectedNhaCungCapForm, value))
            {
                _ = LoadNguyenLieusByNhaCungCapAsync();
            }
        }
    }

    private ObservableCollection<CT_PhieuNhap> _chiTietForm = new();
    public ObservableCollection<CT_PhieuNhap> ChiTietForm
    {
        get => _chiTietForm;
        set => SetProperty(ref _chiTietForm, value);
    }

    private decimal _tongTienForm;
    public decimal TongTienForm
    {
        get => _tongTienForm;
        set => SetProperty(ref _tongTienForm, value);
    }

    private ObservableCollection<NguyenLieu> _nguyenLieusOfNCC = new();
    public ObservableCollection<NguyenLieu> NguyenLieusOfNCC
    {
        get => _nguyenLieusOfNCC;
        set => SetProperty(ref _nguyenLieusOfNCC, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // Chỉ sửa/xóa phiếu Chờ duyệt và trong ngày
    public bool CanEditDelete => SelectedPhieuNhap != null && 
                                  SelectedPhieuNhap.TrangThai == 1 &&
                                  SelectedPhieuNhap.NgayNhap.Date == DateTime.Today;

    // Quản lý mới được duyệt phiếu Chờ duyệt
    public bool CanApprove => SelectedPhieuNhap != null && SelectedPhieuNhap.TrangThai == 1 && IsQuanLy;

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
    public ICommand CreatePhieuNhapCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand EditPhieuNhapCommand { get; }
    public ICommand DeletePhieuNhapCommand { get; }
    public ICommand ApprovePhieuNhapCommand { get; }
    public ICommand CancelPhieuNhapCommand { get; }
    public ICommand SavePhieuNhapCommand { get; }
    public ICommand CancelDialogCommand { get; }
    public ICommand CloseDetailDialogCommand { get; }
    public ICommand AddNguyenLieuCommand { get; }
    public ICommand RemoveChiTietCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand PrintPhieuNhapCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    #endregion

    public PhieuNhapViewModel()
    {
        // ⚡ Dùng ServiceLocator thay vì new DatabaseService() — dùng chung singleton
        try
        {
            _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }

        // Phát hiện người dùng hiện tại là nhân viên kho
        var currentUser = CurrentUserSession.Instance.CurrentUser;
        var chucVuId = currentUser?.NhanVien?.ChucVuID ?? 0;
        IsNhanVienKho = chucVuId == 4; // 4: Nhân viên kho
        CurrentUserName = currentUser?.NhanVien?.HoTen ?? "Nhân viên";

        // ⚡ Dùng AsyncRelayCommand cho các lệnh async — an toàn hơn async void
        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        CreatePhieuNhapCommand = new RelayCommand(_ => OpenCreateDialog());
        ViewDetailCommand = new AsyncRelayCommand(async p => await ViewDetailAsync(p));
        EditPhieuNhapCommand = new AsyncRelayCommand(async p => await EditPhieuNhapAsync(p));
        DeletePhieuNhapCommand = new AsyncRelayCommand(async p => await DeletePhieuNhapAsync(p));
        ApprovePhieuNhapCommand = new AsyncRelayCommand(async _ => await ApprovePhieuNhapAsync());
        CancelPhieuNhapCommand = new AsyncRelayCommand(async _ => await CancelPhieuNhapAsync());
        SavePhieuNhapCommand = new AsyncRelayCommand(async _ => await SavePhieuNhapAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        CloseDetailDialogCommand = new RelayCommand(_ => IsDetailDialogOpen = false);
        AddNguyenLieuCommand = new RelayCommand(p => AddNguyenLieuToForm(p));
        RemoveChiTietCommand = new RelayCommand(p => RemoveChiTietFromForm(p));
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        PrintPhieuNhapCommand = new AsyncRelayCommand(async p => await PrintPhieuNhapAsync(p));
        IncreaseQuantityCommand = new RelayCommand(p => IncreaseQuantity(p));
        DecreaseQuantityCommand = new RelayCommand(p => DecreaseQuantity(p));

        // ⚡ SafeInitializeAsync thay vì fire-and-forget
        SafeInitializeAsync(LoadDataAsync);
    }

    private void IncreaseQuantity(object? parameter)
    {
        if (parameter is CT_PhieuNhap chiTiet)
        {
            chiTiet.SoLuong++;
            CalculateTongTienForm();
        }
    }

    private void DecreaseQuantity(object? parameter)
    {
        if (parameter is CT_PhieuNhap chiTiet && chiTiet.SoLuong > 1)
        {
            chiTiet.SoLuong--;
            CalculateTongTienForm();
        }
    }

    #region Phương thức
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var nhaCungCaps = await _databaseService.GetNhaCungCapsAsync();
            var allNhaCungCaps = new List<NhaCungCap>
            {
                new NhaCungCap { NhaCungCapID = 0, TenNCC = "Tất cả" }
            };
            allNhaCungCaps.AddRange(nhaCungCaps);
            NhaCungCaps = new ObservableCollection<NhaCungCap>(allNhaCungCaps);

            var nhanViens = await _databaseService.GetNhanViensAsync();
            NhanViens = new ObservableCollection<NhanVien>(nhanViens);

            var donViTinhs = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs = new ObservableCollection<DonViTinh>(donViTinhs);

            await LoadPhieuNhapsAsync();
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

    private async Task LoadPhieuNhapsAsync()
    {
        try
        {
            var trangThaiFilter = new List<byte>();
            if (FilterChoDuyet) trangThaiFilter.Add(1);
            if (FilterDaDuyet) trangThaiFilter.Add(2);
            if (FilterDaHuy) trangThaiFilter.Add(3);

            var phieuNhaps = await _databaseService.GetPhieuNhapsAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                SelectedNhanVienFilter?.NhanVienID,
                SelectedNhaCungCapFilter?.NhaCungCapID,
                TuNgay,
                DenNgay,
                trangThaiFilter.Any() ? trangThaiFilter : null);

            PhieuNhaps = new ObservableCollection<PhieuNhap>(phieuNhaps);

            // Đếm số phiếu theo trạng thái
            SoPhieuChoDuyet = phieuNhaps.Count(p => p.TrangThai == 1);
            SoPhieuDaDuyet = phieuNhaps.Count(p => p.TrangThai == 2);
            SoPhieuDaHuy = phieuNhaps.Count(p => p.TrangThai == 3);

            TongTien = await _databaseService.GetTotalTongTienPhieuNhapAsync(
                SelectedNhanVienFilter?.NhanVienID,
                SelectedNhaCungCapFilter?.NhaCungCapID,
                TuNgay,
                DenNgay);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PhieuNhaps: {ex.Message}");
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
        // ⚡ Batch guard: thay đổi 7 property mà chỉ reload DB 1 lần cuối
        // Trước đây: 8 DB calls (mỗi setter gọi LoadPhieuNhapsAsync)
        // Bây giờ: 1 DB call duy nhất
        IsBatchUpdating = true;
        try
        {
            SearchText = string.Empty;
            SelectedNhanVienFilter = null;
            SelectedNhaCungCapFilter = null;
            SelectedThoiGian = string.Empty;
            TuNgay = null;
            DenNgay = null;
            FilterChoDuyet = true;
            FilterDaDuyet = true;
            FilterDaHuy = false;
        }
        finally
        {
            IsBatchUpdating = false;
        }
        
        // Gọi reload 1 lần duy nhất
        _ = LoadPhieuNhapsAsync();
    }

    private void OpenCreateDialog()
    {
        IsCreateMode = true;
        ChiTietForm = new ObservableCollection<CT_PhieuNhap>();
        TongTienForm = 0;
        NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>();
        // Auto-select "Tất cả" to show all materials by default
        SelectedNhaCungCapForm = NhaCungCaps.FirstOrDefault(n => n.NhaCungCapID == 0);
        IsDialogOpen = true;
    }

    private async Task LoadNguyenLieusByNhaCungCapAsync()
    {
        if (SelectedNhaCungCapForm == null)
        {
            NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>();
            return;
        }

        try
        {
            List<NguyenLieu> nguyenLieus;
            if (SelectedNhaCungCapForm.NhaCungCapID == 0)
            {
                // "Tất cả" selected - load all NguyenLieu with prices
                nguyenLieus = await _databaseService.GetAllNguyenLieusWithPriceAsync();
            }
            else
            {
                nguyenLieus = await _databaseService.GetNguyenLieusByNhaCungCapAsync(SelectedNhaCungCapForm.NhaCungCapID);
            }
            NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>(nguyenLieus);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieus: {ex.Message}");
        }
    }

    private void AddNguyenLieuToForm(object? parameter)
    {
        if (parameter is NguyenLieu nguyenLieu)
        {
            // Kiểm tra đã tồn tại chưa
            if (ChiTietForm.Any(ct => ct.NguyenLieuID == nguyenLieu.NguyenLieuID))
            {
                return;
            }

            var giaNhap = nguyenLieu.NguyenLieuNhaCungCaps?.FirstOrDefault()?.GiaNhap ?? 0;

            var chiTiet = new CT_PhieuNhap
            {
                NguyenLieuID = nguyenLieu.NguyenLieuID,
                NguyenLieu = nguyenLieu,
                SoLuong = 1,
                DonViID = nguyenLieu.DonViID,
                DonViTinh = nguyenLieu.DonViTinh,
                HeSo = 1,
                DonGia = giaNhap,
                ThanhTien = giaNhap,
                HSD = DateTime.Today.AddMonths(6)
            };

            ChiTietForm.Add(chiTiet);
            CalculateTongTienForm();
        }
    }

    private void RemoveChiTietFromForm(object? parameter)
    {
        if (parameter is CT_PhieuNhap chiTiet)
        {
            ChiTietForm.Remove(chiTiet);
            CalculateTongTienForm();
        }
    }

    public void UpdateChiTietThanhTien(CT_PhieuNhap chiTiet)
    {
        chiTiet.ThanhTien = chiTiet.SoLuong * chiTiet.DonGia;
        CalculateTongTienForm();
    }

    private void CalculateTongTienForm()
    {
        TongTienForm = ChiTietForm.Sum(ct => ct.ThanhTien ?? 0);
    }

    private async Task ViewDetailAsync(object? parameter)
    {
        int phieuNhapId = 0;
        
        if (parameter is PhieuNhap phieuNhap)
        {
            phieuNhapId = phieuNhap.PhieuNhapID;
        }
        else if (parameter is int id)
        {
            phieuNhapId = id;
        }

        if (phieuNhapId == 0) return;

        try
        {
            SelectedPhieuNhap = await _databaseService.GetPhieuNhapByIdAsync(phieuNhapId);
            var chiTiets = await _databaseService.GetChiTietPhieuNhapAsync(phieuNhapId);
            ChiTietPhieuNhaps = new ObservableCollection<CT_PhieuNhap>(chiTiets);
            OnPropertyChanged(nameof(CanApprove));
            OnPropertyChanged(nameof(CanEditDelete));
            IsDetailDialogOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading detail: {ex.Message}");
        }
    }

    private async Task EditPhieuNhapAsync(object? parameter)
    {
        if (SelectedPhieuNhap == null || !CanEditDelete) return;

        IsCreateMode = false;
        
        // Tải dữ liệu để chỉnh sửa
        SelectedNhaCungCapForm = NhaCungCaps.FirstOrDefault(n => n.NhaCungCapID == SelectedPhieuNhap.NhaCungCapID);
        
        await LoadNguyenLieusByNhaCungCapAsync();

        var chiTiets = await _databaseService.GetChiTietPhieuNhapAsync(SelectedPhieuNhap.PhieuNhapID);
        ChiTietForm = new ObservableCollection<CT_PhieuNhap>(chiTiets);
        
        CalculateTongTienForm();
        IsDetailDialogOpen = false;
        IsDialogOpen = true;
    }

    private async Task DeletePhieuNhapAsync(object? parameter)
    {
        if (SelectedPhieuNhap == null || !CanEditDelete) return;

        try
        {
            var result = await _databaseService.DeletePhieuNhapAsync(SelectedPhieuNhap.PhieuNhapID);
            if (result)
            {
                await LoadPhieuNhapsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuNhap = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting PhieuNhap: {ex.Message}");
        }
    }

    private async Task ApprovePhieuNhapAsync()
    {
        if (SelectedPhieuNhap == null || !CanApprove) return;

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;
            if (currentUser?.NhanVienID == null) return;

            var result = await _databaseService.ApprovePhieuNhapAsync(
                SelectedPhieuNhap.PhieuNhapID, 
                currentUser.NhanVienID.Value);
                
            if (result)
            {
                await LoadPhieuNhapsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuNhap = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error approving PhieuNhap: {ex.Message}");
        }
    }

    private async Task CancelPhieuNhapAsync()
    {
        if (SelectedPhieuNhap == null || SelectedPhieuNhap.TrangThai != 1) return;

        try
        {
            var result = await _databaseService.CancelPhieuNhapAsync(SelectedPhieuNhap.PhieuNhapID);
            if (result)
            {
                await LoadPhieuNhapsAsync();
                IsDetailDialogOpen = false;
                SelectedPhieuNhap = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error canceling PhieuNhap: {ex.Message}");
        }
    }

    private async Task SavePhieuNhapAsync()
    {
        if (SelectedNhaCungCapForm == null || !ChiTietForm.Any())
        {
            return;
        }

        try
        {
            var currentUser = CurrentUserSession.Instance.CurrentUser;
            
            var phieuNhap = new PhieuNhap
            {
                NhanVienNhapID = currentUser?.NhanVienID,
                NhaCungCapID = SelectedNhaCungCapForm.NhaCungCapID,
                NgayNhap = DateTime.Now,
                TongTien = TongTienForm,
                TrangThai = 1 // Chờ duyệt
            };

            if (IsCreateMode)
            {
                phieuNhap.MaPhieuNhap = await _databaseService.GenerateMaPhieuNhapAsync();
            }
            else
            {
                phieuNhap.PhieuNhapID = SelectedPhieuNhap!.PhieuNhapID;
                phieuNhap.MaPhieuNhap = SelectedPhieuNhap.MaPhieuNhap;
            }

            await _databaseService.SavePhieuNhapAsync(phieuNhap, ChiTietForm.ToList());
            
            CloseDialog();
            await LoadPhieuNhapsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving PhieuNhap: {ex.Message}");
        }
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        SelectedNhaCungCapForm = null;
        ChiTietForm.Clear();
        TongTienForm = 0;
    }

    public string GetTrangThaiText(byte trangThai)
    {
        return trangThai switch
        {
            1 => "Chờ duyệt",
            2 => "Đã duyệt",
            3 => "Đã hủy",
            _ => "Không xác định"
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

    private async Task PrintPhieuNhapAsync(object? parameter)
    {
        PhieuNhap? phieuNhap = null;
        
        if (parameter is PhieuNhap pn)
        {
            phieuNhap = pn;
        }
        else if (SelectedPhieuNhap != null)
        {
            phieuNhap = SelectedPhieuNhap;
        }

        if (phieuNhap == null) return;

        try
        {
            // Tải chi tiết nếu chưa có
            var chiTiets = await _databaseService.GetChiTietPhieuNhapAsync(phieuNhap.PhieuNhapID);
            
            // Gọi PrintService
            PrintService.PrintPhieuNhap(phieuNhap, chiTiets);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error printing PhieuNhap: {ex.Message}");
        }
    }
    #endregion
}


