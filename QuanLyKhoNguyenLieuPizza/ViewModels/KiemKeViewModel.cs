using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Views;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

/// <summary>
/// Dòng preview sinh chứng từ tự động khi duyệt (hiển thị trong dialog xác nhận duyệt)
/// </summary>
public class ApprovePreviewItem
{
    public string Description { get; set; } = string.Empty;
    public string HighlightText { get; set; } = string.Empty;
    public string HighlightColor { get; set; } = "#1E293B";
}

public class PageItem
{
    public int Number { get; set; }
    public bool IsActive { get; set; }
    public ICommand? GoToPageCommand { get; set; }
}

public class KiemKeViewModel : BaseViewModel
{
    private readonly DatabaseService _dbService;

    // DANH SÁCH PHIẾU ═══════════════
    private ObservableCollection<PhieuKiemKe> _phieuKiemKes = new();
    public ObservableCollection<PhieuKiemKe> PhieuKiemKes
    {
        get => _phieuKiemKes;
        set => SetProperty(ref _phieuKiemKes, value);
    }

    private PhieuKiemKe? _selectedPhieuKiemKe;
    public PhieuKiemKe? SelectedPhieuKiemKe
    {
        get => _selectedPhieuKiemKe;
        set => SetProperty(ref _selectedPhieuKiemKe, value);
    }

    // Bộ lọc
    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                if (!IsBatchUpdating) _ = LoadPhieuKiemKesAsync();
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
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                if (!IsBatchUpdating) _ = LoadPhieuKiemKesAsync();
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
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                if (!IsBatchUpdating) _ = LoadPhieuKiemKesAsync();
            }
        }
    }

    private string? _selectedTrangThaiFilter;
    public string? SelectedTrangThaiFilter
    {
        get => _selectedTrangThaiFilter;
        set
        {
            if (SetProperty(ref _selectedTrangThaiFilter, value))
            {
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                if (!IsBatchUpdating) _ = LoadPhieuKiemKesAsync();
            }
        }
    }

    public List<string> TrangThaiOptions { get; } = new()
    {
        "Tất cả", "Bản Nháp", "Chờ Duyệt", "Đã Duyệt", "Đã Hủy"
    };

    // STATE HIỂN THỊ ═══════════════
    // 3 chế độ chính: Danh sách | Form lập phiếu | Chi tiết (view only)
    private bool _isFormVisible;
    public bool IsFormVisible
    {
        get => _isFormVisible;
        set => SetProperty(ref _isFormVisible, value);
    }

    private bool _isDetailVisible;
    public bool IsDetailVisible
    {
        get => _isDetailVisible;
        set => SetProperty(ref _isDetailVisible, value);
    }

    private bool _isReviewMode;
    public bool IsReviewMode
    {
        get => _isReviewMode;
        set => SetProperty(ref _isReviewMode, value);
    }

    private bool _isViewOnly;
    public bool IsViewOnly
    {
        get => _isViewOnly;
        set => SetProperty(ref _isViewOnly, value);
    }

    private string _formTitle = "LẬP PHIẾU KIỂM KÊ KHO";
    public string FormTitle
    {
        get => _formTitle;
        set => SetProperty(ref _formTitle, value);
    }

    private string _formBadge = "Bản Nhập Mới";
    public string FormBadge
    {
        get => _formBadge;
        set => SetProperty(ref _formBadge, value);
    }

    // PHIẾU DATA ═══════════════
    private PhieuKiemKe _currentPhieu = new();
    public PhieuKiemKe CurrentPhieu
    {
        get => _currentPhieu;
        set => SetProperty(ref _currentPhieu, value);
    }

    private string? _maPhieu;
    public string? MaPhieu
    {
        get => _maPhieu;
        set => SetProperty(ref _maPhieu, value);
    }

    private string? _tenNhanVienKiem;
    public string? TenNhanVienKiem
    {
        get => _tenNhanVienKiem;
        set => SetProperty(ref _tenNhanVienKiem, value);
    }

    private string? _ngayKiemDisplay;
    public string? NgayKiemDisplay
    {
        get => _ngayKiemDisplay;
        set => SetProperty(ref _ngayKiemDisplay, value);
    }

    private string? _tenNhanVienDuyet;
    public string? TenNhanVienDuyet
    {
        get => _tenNhanVienDuyet;
        set => SetProperty(ref _tenNhanVienDuyet, value);
    }

    private string? _ngayDuyetDisplay;
    public string? NgayDuyetDisplay
    {
        get => _ngayDuyetDisplay;
        set => SetProperty(ref _ngayDuyetDisplay, value);
    }

    private string? _lyDoKiemKe;
    public string? LyDoKiemKe
    {
        get => _lyDoKiemKe;
        set => SetProperty(ref _lyDoKiemKe, value);
    }

    private string? _loaiKiemKe;
    public string? LoaiKiemKe
    {
        get => _loaiKiemKe;
        set => SetProperty(ref _loaiKiemKe, value);
    }

    public List<string> LoaiKiemKeOptions { get; } = new()
    {
        "Kiểm kê định kỳ", "Kiểm kê đột xuất", "Kiểm kê chuyển giao"
    };

    private string? _ghiChuDuyet;
    public string? GhiChuDuyet
    {
        get => _ghiChuDuyet;
        set => SetProperty(ref _ghiChuDuyet, value);
    }

    // TRẠNG THÁI PHIẾU CHI TIẾT ═══════════════
    private bool _isDaDuyet;
    public bool IsDaDuyet
    {
        get => _isDaDuyet;
        set => SetProperty(ref _isDaDuyet, value);
    }

    private bool _isDaHuy;
    public bool IsDaHuy
    {
        get => _isDaHuy;
        set => SetProperty(ref _isDaHuy, value);
    }

    // DIALOG XÁC NHẬN TỪ CHỐI ═══════════════
    private bool _isRejectDialogOpen;
    public bool IsRejectDialogOpen
    {
        get => _isRejectDialogOpen;
        set => SetProperty(ref _isRejectDialogOpen, value);
    }

    private string? _lyDoTuChoi;
    public string? LyDoTuChoi
    {
        get => _lyDoTuChoi;
        set => SetProperty(ref _lyDoTuChoi, value);
    }

    // DIALOG XÁC NHẬN YÊU CẦU ĐẾM LẠI ═══════════════
    private bool _isRecountDialogOpen;
    public bool IsRecountDialogOpen
    {
        get => _isRecountDialogOpen;
        set => SetProperty(ref _isRecountDialogOpen, value);
    }

    private string? _lyDoRecount;
    public string? LyDoRecount
    {
        get => _lyDoRecount;
        set => SetProperty(ref _lyDoRecount, value);
    }

    // DIALOG XÁC NHẬN DUYỆT ═══════════════
    private bool _isApproveDialogOpen;
    public bool IsApproveDialogOpen
    {
        get => _isApproveDialogOpen;
        set => SetProperty(ref _isApproveDialogOpen, value);
    }

    private ObservableCollection<ApprovePreviewItem> _approvePreviewItems = new();
    public ObservableCollection<ApprovePreviewItem> ApprovePreviewItems
    {
        get => _approvePreviewItems;
        set => SetProperty(ref _approvePreviewItems, value);
    }

    // CHI TIẾT FORM ═══════════════
    private ObservableCollection<CT_PhieuKiemKe> _chiTietKiemKeForm = new();
    public ObservableCollection<CT_PhieuKiemKe> ChiTietKiemKeForm
    {
        get => _chiTietKiemKeForm;
        set => SetProperty(ref _chiTietKiemKeForm, value);
    }

    private ObservableCollection<NguyenLieu> _nguyenLieus = new();
    public ObservableCollection<NguyenLieu> NguyenLieus
    {
        get => _nguyenLieus;
        set => SetProperty(ref _nguyenLieus, value);
    }

    private NguyenLieu? _selectedNguyenLieu;
    public NguyenLieu? SelectedNguyenLieu
    {
        get => _selectedNguyenLieu;
        set => SetProperty(ref _selectedNguyenLieu, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isQuanLy;
    public bool IsQuanLy
    {
        get => _isQuanLy;
        set => SetProperty(ref _isQuanLy, value);
    }

    // PHÂN TRANG ═══════════════
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                _ = LoadPhieuKiemKesAsync();
            }
        }
    }

    private int _totalPages = 1;
    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
    }

    private int _pageSize = 8;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                CurrentPage = 1;
                _ = LoadPhieuKiemKesAsync();
            }
        }
    }

    private int _totalRecords;
    public int TotalRecords
    {
        get => _totalRecords;
        set => SetProperty(ref _totalRecords, value);
    }

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    private ObservableCollection<PageItem> _pageNumbers = new();
    public ObservableCollection<PageItem> PageNumbers
    {
        get => _pageNumbers;
        set => SetProperty(ref _pageNumbers, value);
    }

    // COMMANDS ═══════════════
    public ICommand CreatePhieuCommand { get; }
    public ICommand SaveDraftCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand AddNguyenLieuCommand { get; }
    public ICommand RemoveNguyenLieuCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand CloseFormCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand AddEmptyChiTietCommand { get; }
    // Dialog commands
    public ICommand OpenRejectDialogCommand { get; }
    public ICommand CloseRejectDialogCommand { get; }
    public ICommand ConfirmRejectCommand { get; }
    public ICommand OpenApproveDialogCommand { get; }
    public ICommand CloseApproveDialogCommand { get; }
    public ICommand ConfirmApproveCommand { get; }
    public ICommand OpenRecountDialogCommand { get; }
    public ICommand CloseRecountDialogCommand { get; }
    public ICommand ConfirmRecountCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand PrintCommand { get; }

    public KiemKeViewModel()
    {
        _dbService = App.Services.GetRequiredService<DatabaseService>();

        CreatePhieuCommand = new AsyncRelayCommand(CreatePhieuAsync);
        SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        AddNguyenLieuCommand = new AsyncRelayCommand(AddNguyenLieuAsync);
        AddEmptyChiTietCommand = new RelayCommand(_ => AddEmptyChiTiet());
        RemoveNguyenLieuCommand = new RelayCommand(RemoveNguyenLieu);
        ViewDetailCommand = new AsyncRelayCommand(ViewDetailAsync);
        EditCommand = new AsyncRelayCommand(EditAsync);
        CloseFormCommand = new RelayCommand(_ => CloseForm());
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);

        // Dialog commands
        OpenRejectDialogCommand = new RelayCommand(_ => OpenRejectDialog());
        CloseRejectDialogCommand = new RelayCommand(_ => IsRejectDialogOpen = false);
        ConfirmRejectCommand = new AsyncRelayCommand(ConfirmRejectAsync);
        OpenApproveDialogCommand = new RelayCommand(_ => OpenApproveDialog());
        CloseApproveDialogCommand = new RelayCommand(_ => IsApproveDialogOpen = false);
        ConfirmApproveCommand = new AsyncRelayCommand(ConfirmApproveAsync);
        OpenRecountDialogCommand = new RelayCommand(_ => OpenRecountDialog());
        CloseRecountDialogCommand = new RelayCommand(_ => IsRecountDialogOpen = false);
        ConfirmRecountCommand = new AsyncRelayCommand(ConfirmRecountAsync);
        BackToListCommand = new RelayCommand(_ => BackToList());
        PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; }, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; }, _ => CurrentPage < TotalPages);
        ClearFilterCommand = new AsyncRelayCommand(async _ => await ClearFilterAsync());
        PrintCommand = new AsyncRelayCommand(async p => await PrintPhieuKiemKeAsync(p));

        // Xác định vai trò
        var chucVu = CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVu?.TenChucVu?.Trim().ToLower() ?? "";
        IsQuanLy = chucVu.Contains("quản lý") || chucVu.Contains("quan ly");

        _ = LoadPhieuKiemKesAsync();
    }

    // LOAD DANH SÁCH ═══════════════
    private async Task LoadPhieuKiemKesAsync()
    {
        try
        {
            IsLoading = true;
            var trangThai = _selectedTrangThaiFilter == "Tất cả" ? null : _selectedTrangThaiFilter;
            var list = await _dbService.GetPhieuKiemKesAsync(_searchText, _tuNgay, _denNgay, trangThai);

            // Tính toán phân trang
            TotalRecords = list.Count;
            TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
            if (TotalPages < 1) TotalPages = 1;

            if (CurrentPage > TotalPages)
            {
                _currentPage = TotalPages;
                OnPropertyChanged(nameof(CurrentPage));
            }

            var pagedList = list.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            PhieuKiemKes = new ObservableCollection<PhieuKiemKe>(pagedList);

            UpdatePageNumbers();
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearFilterAsync()
    {
        IsBatchUpdating = true;
        try
        {
            SearchText = null;
            TuNgay = null;
            DenNgay = null;
            SelectedTrangThaiFilter = "Tất cả";
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
        }
        finally
        {
            IsBatchUpdating = false;
        }
        await LoadPhieuKiemKesAsync();
    }

    private void UpdatePageNumbers()
    {
        var pages = new List<PageItem>();
        for (int i = 1; i <= TotalPages; i++)
        {
            var pageNum = i;
            pages.Add(new PageItem
            {
                Number = pageNum,
                IsActive = pageNum == CurrentPage,
                GoToPageCommand = new RelayCommand(_ => CurrentPage = pageNum)
            });
        }
        PageNumbers = new ObservableCollection<PageItem>(pages);
    }

    // TẠO PHIẾU MỚI ═══════════════
    private async Task CreatePhieuAsync(object? _)
    {
        try
        {
            IsLoading = true;
            FormTitle = "LẬP PHIẾU KIỂM KÊ KHO";
            FormBadge = "Mới lập (Nháp)";
            IsReviewMode = false;
            IsViewOnly = false;
            IsDetailVisible = false;

            CurrentPhieu = new PhieuKiemKe
            {
                TrangThai = "Bản Nháp",
                NgayKiem = DateTime.Now,
                NhanVienKiemID = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID
            };

            MaPhieu = await _dbService.GenerateMaPhieuKiemAsync();
            TenNhanVienKiem = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HoTen ?? "Nhân viên";
            LyDoKiemKe = null;
            LoaiKiemKe = null;
            GhiChuDuyet = null;

            ChiTietKiemKeForm.Clear();
            AddEmptyChiTiet();

            var nguyenLieus = await _dbService.GetNguyenLieusForKiemKeAsync();
            NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);

            IsFormVisible = true;
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi tạo phiếu: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // THÊM NGUYÊN LIỆU VÀO FORM ═══════════════
    private async Task AddNguyenLieuAsync(object? _)
    {
        if (SelectedNguyenLieu == null) return;

        var exists = ChiTietKiemKeForm.Any(x => x.NguyenLieuID == SelectedNguyenLieu.NguyenLieuID);
        if (exists)
        {
            PizzaMessageBox.Show("Nguyên liệu này đã có trong danh sách kiểm kê.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);
            return;
        }

        var donViKhoId = SelectedNguyenLieu.TonKho?.DonViID ?? SelectedNguyenLieu.DonViID;

        var ct = new CT_PhieuKiemKe
        {
            NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
            NguyenLieu = new NguyenLieu
            {
                NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
                MaNguyenLieu = SelectedNguyenLieu.MaNguyenLieu,
                TenNguyenLieu = SelectedNguyenLieu.TenNguyenLieu
            },
            TonHeThong = SelectedNguyenLieu.TonKho?.SoLuongTon ?? 0,
            DonViKhoID = donViKhoId,
            DonViKho = SelectedNguyenLieu.DonViTinh,
            TonThucTe = 0,
            HeSoKiem = 1m
        };

        try
        {
            var quyDois = await _dbService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);
            ct.DonViKiemOptions = new ObservableCollection<QuyDoiDonVi>(quyDois);

            var chuanUnit = quyDois.FirstOrDefault(q => q.LaDonViChuan) ?? quyDois.FirstOrDefault();
            if (chuanUnit != null)
            {
                ct.HeSoChuan = chuanUnit.HeSo > 0 ? chuanUnit.HeSo : 1m;
                ct.SelectedDonViKiem = chuanUnit;
                ct.DonViKiemID = chuanUnit.DonViID;
                ct.DonViKiem = chuanUnit.DonViTinh;
            }
            else
            {
                ct.DonViKiemID = donViKhoId;
                ct.DonViKiem = SelectedNguyenLieu.DonViTinh;
            }
        }
        catch {
            ct.DonViKiemID = donViKhoId;
            ct.DonViKiem = SelectedNguyenLieu.DonViTinh;
        }

        ct.PropertyChanged += ChiTiet_PropertyChanged;
        ChiTietKiemKeForm.Add(ct);
        SelectedNguyenLieu = null;
    }

    private void AddEmptyChiTiet()
    {
        var ct = new CT_PhieuKiemKe
        {
            TonThucTe = 0,
            HeSoKiem = 1m
        };
        ct.PropertyChanged += ChiTiet_PropertyChanged;
        ChiTietKiemKeForm.Add(ct);
    }

    private async void ChiTiet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CT_PhieuKiemKe ct && e.PropertyName == nameof(CT_PhieuKiemKe.NguyenLieu))
        {
            if (ct.NguyenLieu != null)
            {
                var nguyenLieu = ct.NguyenLieu;
                ct.NguyenLieuID = nguyenLieu.NguyenLieuID;
                ct.TonHeThong = nguyenLieu.TonKho?.SoLuongTon ?? 0;

                var donViKhoId = nguyenLieu.TonKho?.DonViID ?? nguyenLieu.DonViID;
                ct.DonViKhoID = donViKhoId;
                ct.DonViKho = nguyenLieu.DonViTinh;

                try
                {
                    var quyDois = await _dbService.GetQuyDoiDonVisAsync(nguyenLieu.NguyenLieuID);
                    ct.DonViKiemOptions = new ObservableCollection<QuyDoiDonVi>(quyDois);

                    var chuanUnit = quyDois.FirstOrDefault(q => q.LaDonViChuan) ?? quyDois.FirstOrDefault();
                    if (chuanUnit != null)
                    {
                        ct.HeSoChuan = chuanUnit.HeSo > 0 ? chuanUnit.HeSo : 1m;
                        ct.SelectedDonViKiem = chuanUnit;
                        ct.DonViKiemID = chuanUnit.DonViID;
                        ct.DonViKiem = chuanUnit.DonViTinh;
                    }
                    else
                    {
                        ct.DonViKiemID = donViKhoId;
                        ct.DonViKiem = nguyenLieu.DonViTinh;
                    }
                }
                catch {
                    ct.DonViKiemID = donViKhoId;
                    ct.DonViKiem = nguyenLieu.DonViTinh;
                }
            }
        }
    }

    private void RemoveNguyenLieu(object? parameter)
    {
        if (parameter is CT_PhieuKiemKe ct && !IsViewOnly)
        {
            ct.PropertyChanged -= ChiTiet_PropertyChanged;
            ChiTietKiemKeForm.Remove(ct);
        }
    }

    // LƯU BẢN NHÁP ═══════════════
    private async Task SaveDraftAsync(object? _)
    {
        if (!ValidateForm()) return;

        try
        {
            IsLoading = true;

            CurrentPhieu.MaPhieuKiem = MaPhieu;
            CurrentPhieu.LyDoKiemKe = LyDoKiemKe;
            CurrentPhieu.LoaiKiemKe = LoaiKiemKe;
            CurrentPhieu.TrangThai = "Bản Nháp";

            var id = await _dbService.SavePhieuKiemKeAsync(CurrentPhieu, ChiTietKiemKeForm.ToList());
            CurrentPhieu.KiemKeID = id;

            PizzaMessageBox.Show("Đã lưu bản nháp thành công!", "Thành công",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);

            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi lưu bản nháp: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // GỬI DUYỆT ═══════════════
    private async Task SubmitAsync(object? _)
    {
        if (!ValidateForm()) return;

        if (ChiTietKiemKeForm.Count == 0)
        {
            PizzaMessageBox.Show("Vui lòng thêm ít nhất 1 nguyên liệu.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Warning);
            return;
        }

        var result = PizzaMessageBox.Show(
            "Bạn có chắc chắn muốn gửi phiếu kiểm kê để quản lý duyệt?\n\n" +
            "Sau khi gửi, bạn sẽ không thể chỉnh sửa phiếu này.",
            "Xác nhận gửi duyệt",
            PizzaMessageBoxButtons.YesNo, PizzaMessageBoxImage.Question);

        if (result != true) return;

        try
        {
            IsLoading = true;

            CurrentPhieu.MaPhieuKiem = MaPhieu;
            CurrentPhieu.LyDoKiemKe = LyDoKiemKe;
            CurrentPhieu.LoaiKiemKe = LoaiKiemKe;
            CurrentPhieu.TrangThai = "Bản Nháp";

            // Mở popup ký tên xác nhận (ChuKyKiem)
            var signaturePopup = new Views.SignaturePopup(MaPhieu ?? "KK", TenNhanVienKiem ?? "Nhân viên");
            signaturePopup.Owner = System.Windows.Application.Current.MainWindow;
            if (signaturePopup.ShowDialog() == true)
            {
                CurrentPhieu.ChuKyKiem = signaturePopup.SignatureBase64;
            }
            else
            {
                IsLoading = false;
                return; // Hủy nếu không ký
            }

            var id = await _dbService.SavePhieuKiemKeAsync(CurrentPhieu, ChiTietKiemKeForm.ToList());
            CurrentPhieu.KiemKeID = id;

            await _dbService.SubmitPhieuKiemKeAsync(id, CurrentPhieu.ChuKyKiem);

            PizzaMessageBox.Show("Đã gửi phiếu kiểm kê cho quản lý duyệt!", "Thành công",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);

            CloseForm();
            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi gửi duyệt: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // XEM CHI TIẾT (FULL PAGE) ═══════════════
    private async Task ViewDetailAsync(object? parameter)
    {
        var phieu = parameter as PhieuKiemKe ?? SelectedPhieuKiemKe;
        if (phieu == null) return;

        try
        {
            IsLoading = true;

            // Luôn mở trang chi tiết full-page
            await LoadPhieuData(phieu);

            // Thiết lập trạng thái hiển thị
            FormTitle = "CHI TIẾT PHIẾU KIỂM KÊ";
            FormBadge = phieu.TrangThai ?? "N/A";
            IsDaDuyet = phieu.TrangThai == "Đã Duyệt";
            IsDaHuy = phieu.TrangThai == "Đã Hủy";
            IsReviewMode = IsQuanLy && phieu.TrangThai == "Chờ Duyệt";
            IsViewOnly = true;

            // Hiển thị tên người duyệt / ngày duyệt
            TenNhanVienDuyet = phieu.NhanVienDuyet?.HoTen;
            NgayDuyetDisplay = phieu.NgayDuyet?.ToString("dd/MM/yyyy HH:mm");
            NgayKiemDisplay = phieu.NgayKiem?.ToString("dd/MM/yyyy HH:mm");

            IsFormVisible = false;
            IsDetailVisible = true;
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi xem chi tiết: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditAsync(object? parameter)
    {
        var phieu = parameter as PhieuKiemKe ?? SelectedPhieuKiemKe;
        if (phieu == null || phieu.TrangThai != "Bản Nháp") return;

        try
        {
            IsLoading = true;
            FormTitle = "LẬP PHIẾU KIỂM KÊ KHO";
            FormBadge = "Tiếp tục đếm";
            IsReviewMode = false;
            IsViewOnly = false;
            IsDetailVisible = false;

            var nguyenLieus = await _dbService.GetNguyenLieusForKiemKeAsync();
            NguyenLieus = new ObservableCollection<NguyenLieu>(nguyenLieus);

            await LoadPhieuData(phieu);

            IsFormVisible = true;
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi chỉnh sửa: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    private void OpenRejectDialog()
    {
        LyDoTuChoi = null;
        IsRejectDialogOpen = true;
    }

    private async Task ConfirmRejectAsync(object? _)
    {
        if (CurrentPhieu == null || CurrentPhieu.KiemKeID == 0) return;

        if (string.IsNullOrWhiteSpace(LyDoTuChoi))
        {
            PizzaMessageBox.Show("Vui lòng nhập lý do từ chối.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Warning);
            return;
        }

        try
        {
            IsLoading = true;
            IsRejectDialogOpen = false;

            var nhanVienDuyetId = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID ?? 0;
            await _dbService.RejectPhieuKiemKeAsync(
                CurrentPhieu.KiemKeID, nhanVienDuyetId, LyDoTuChoi);

            PizzaMessageBox.Show("Đã từ chối và hủy phiếu kiểm kê.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);

            BackToList();
            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi từ chối: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // DIALOG YÊU CẦU ĐẾM LẠI ═══════════════
    private void OpenRecountDialog()
    {
        LyDoRecount = null;
        IsRecountDialogOpen = true;
    }

    private async Task ConfirmRecountAsync(object? _)
    {
        if (CurrentPhieu == null || CurrentPhieu.KiemKeID == 0) return;

        if (string.IsNullOrWhiteSpace(LyDoRecount))
        {
            PizzaMessageBox.Show("Vui lòng nhập lý do yêu cầu đếm lại.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Warning);
            return;
        }

        try
        {
            IsLoading = true;
            IsRecountDialogOpen = false;

            var nhanVienDuyetId = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID ?? 0;
            await _dbService.RequestRecountPhieuKiemKeAsync(
                CurrentPhieu.KiemKeID, nhanVienDuyetId, LyDoRecount);

            PizzaMessageBox.Show("Đã yêu cầu đếm lại phiếu kiểm kê.", "Thông báo",
                PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);

            BackToList();
            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi yêu cầu đếm lại: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // DIALOG DUYỆT ═══════════════
    private void OpenApproveDialog()
    {
        // Build preview items
        var items = new ObservableCollection<ApprovePreviewItem>();

        foreach (var ct in ChiTietKiemKeForm)
        {
            var tenNL = ct.NguyenLieu?.TenNguyenLieu ?? "N/A";
            var donVi = ct.TenDonViKho;
            var cl = ct.ChenhLech;

            if (cl > 0)
            {
                items.Add(new ApprovePreviewItem
                {
                    Description = $"Sinh 01 Phiếu Nhập Bù Kho cho ",
                    HighlightText = $"+ {cl:N2} {donVi} {tenNL}.",
                    HighlightColor = "#059669"
                });
            }
            else if (cl < 0)
            {
                items.Add(new ApprovePreviewItem
                {
                    Description = $"Sinh 01 Phiếu Xuất Hủy Tồn cho ",
                    HighlightText = $"- {Math.Abs(cl):N2} {donVi} {tenNL}.",
                    HighlightColor = "#DC2626"
                });
            }
            else
            {
                items.Add(new ApprovePreviewItem
                {
                    Description = $"{tenNL} khớp số lượng ",
                    HighlightText = "(Không phát sinh chứng từ).",
                    HighlightColor = "#6B7280"
                });
            }
        }

        ApprovePreviewItems = items;
        IsApproveDialogOpen = true;
    }

    private async Task ConfirmApproveAsync(object? _)
    {
        if (CurrentPhieu == null || CurrentPhieu.KiemKeID == 0) return;

        try
        {
            IsLoading = true;
            IsApproveDialogOpen = false;

            // Mở popup ký tên xác nhận cho quản lý duyệt
            var tenNguoiDuyet = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HoTen ?? "Quản lý";
            var signaturePopup = new Views.SignaturePopup(CurrentPhieu.MaPhieuKiem ?? "KK", tenNguoiDuyet);
            signaturePopup.Owner = System.Windows.Application.Current.MainWindow;
            string? chuKyDuyet = null;
            if (signaturePopup.ShowDialog() == true)
            {
                chuKyDuyet = signaturePopup.SignatureBase64;
            }
            else
            {
                IsLoading = false;
                return; // Hủy duyệt nếu không ký
            }

            var nhanVienDuyetId = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID ?? 0;
            await _dbService.ApprovePhieuKiemKeAsync(
                CurrentPhieu.KiemKeID, nhanVienDuyetId, chuKyDuyet, GhiChuDuyet);

            PizzaMessageBox.Show(
                "Đã duyệt phiếu kiểm kê thành công!\nPhiếu nhập/xuất điều chỉnh đã được tạo tự động.",
                "Thành công", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Info);

            BackToList();
            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi duyệt phiếu: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // XÓA PHIẾU (BẢN NHÁP) ═══════════════
    private async Task DeleteAsync(object? parameter)
    {
        var phieu = parameter as PhieuKiemKe ?? SelectedPhieuKiemKe;
        if (phieu == null || phieu.TrangThai != "Bản Nháp") return;

        var result = PizzaMessageBox.Show(
            $"Bạn có chắc chắn xóa phiếu {phieu.MaPhieuKiem}?",
            "Xác nhận xóa",
            PizzaMessageBoxButtons.YesNo, PizzaMessageBoxImage.Warning);

        if (result != true) return;

        try
        {
            IsLoading = true;
            await _dbService.DeletePhieuKiemKeAsync(phieu.KiemKeID);
            await LoadPhieuKiemKesAsync();
        }
        catch (Exception ex) {
            PizzaMessageBox.Show($"Lỗi xóa phiếu: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // HELPERS ═══════════════
    private async Task LoadPhieuData(PhieuKiemKe phieu)
    {
        CurrentPhieu = phieu;
        MaPhieu = phieu.MaPhieuKiem;
        TenNhanVienKiem = phieu.NhanVienKiem?.HoTen ?? "N/A";
        LyDoKiemKe = phieu.LyDoKiemKe;
        LoaiKiemKe = phieu.LoaiKiemKe;
        GhiChuDuyet = phieu.GhiChuDuyet;

        var chiTiets = await _dbService.GetChiTietPhieuKiemKeAsync(phieu.KiemKeID);

        foreach (var ct in chiTiets)
        {
            if (ct.NguyenLieuID.HasValue)
            {
                if (NguyenLieus != null && NguyenLieus.Count > 0)
                {
                    var matched = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == ct.NguyenLieuID.Value);
                    if (matched != null)
                    {
                        ct.NguyenLieu = matched;
                    }
                }

                try
                {
                    var quyDois = await _dbService.GetQuyDoiDonVisAsync(ct.NguyenLieuID.Value);
                    ct.DonViKiemOptions = new ObservableCollection<QuyDoiDonVi>(quyDois);

                    // Set HeSoChuan từ đơn vị chuẩn
                    var chuanUnit = quyDois.FirstOrDefault(q => q.LaDonViChuan) ?? quyDois.FirstOrDefault();
                    if (chuanUnit != null)
                    {
                        ct.HeSoChuan = chuanUnit.HeSo > 0 ? chuanUnit.HeSo : 1m;
                    }

                    if (ct.DonViKiemID.HasValue)
                    {
                        var selectedUnit = quyDois.FirstOrDefault(q => q.DonViID == ct.DonViKiemID);
                        if (selectedUnit != null)
                            ct.SelectedDonViKiem = selectedUnit;
                    }
                }
                catch { /* ignore */ }
            }
        }

        ChiTietKiemKeForm = new ObservableCollection<CT_PhieuKiemKe>(chiTiets);
    }

    private void CloseForm()
    {
        IsFormVisible = false;
        IsDetailVisible = false;
        IsReviewMode = false;
        IsViewOnly = false;
        IsRejectDialogOpen = false;
        IsApproveDialogOpen = false;
        IsRecountDialogOpen = false;
    }

    private void BackToList()
    {
        CloseForm();
    }

    private async Task RefreshAsync(object? _)
    {
        await LoadPhieuKiemKesAsync();
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(MaPhieu))
        {
            PizzaMessageBox.Show("Mã phiếu không được để trống.", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private async Task PrintPhieuKiemKeAsync(object? parameter)
    {
        PhieuKiemKe? phieuKiem = null;

        if (parameter is PhieuKiemKe pk)
        {
            phieuKiem = pk;
        }
        else if (CurrentPhieu != null && CurrentPhieu.KiemKeID != 0)
        {
            phieuKiem = CurrentPhieu;
        }

        if (phieuKiem == null) return;

        try
        {
            IsLoading = true;
            // Tải chi tiết đầy đủ của phiếu kiểm kê
            var chiTiets = await _dbService.GetChiTietPhieuKiemKeAsync(phieuKiem.KiemKeID);

            // Map thông tin nguyên liệu và đơn vị để in
            foreach (var ct in chiTiets)
            {
                if (ct.NguyenLieuID.HasValue && (ct.NguyenLieu == null || string.IsNullOrEmpty(ct.NguyenLieu.TenNguyenLieu)))
                {
                    var matched = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == ct.NguyenLieuID.Value);
                    if (matched != null) ct.NguyenLieu = matched;
                }
            }

            // Gọi PrintService
            PrintService.PrintPhieuKiemKe(phieuKiem, chiTiets);
        }
        catch (Exception ex)
        {
            PizzaMessageBox.Show($"Lỗi khi tải dữ liệu in: {ex.Message}", "Lỗi", PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
