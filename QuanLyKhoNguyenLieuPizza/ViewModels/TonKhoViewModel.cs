using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class TonKhoItemViewModel : BaseViewModel
{
    public int NguyenLieuID { get; set; }
    public string MaNguyenLieu { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public decimal SoLuongTon { get; set; }
    public string DonViTinh { get; set; } = string.Empty;
    public string MucDoTonKho { get; set; } = string.Empty;

    public bool IsLowStock => MucDoTonKho == "Thấp" || MucDoTonKho == "Hết hàng";

    private bool _hasExpirySoon;
    public bool HasExpirySoon
    {
        get => _hasExpirySoon;
        set => SetProperty(ref _hasExpirySoon, value);
    }

    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    public ICommand? ViewExpiryDetailCommand { get; set; }
}

public class QuyDoiDonViItemViewModel : BaseViewModel
{
    private bool _laDonViChuan;
    private string _tenDonVi = string.Empty;
    private string _heSoText = "1";

    public int QuyDoiID { get; set; }
    public int? DonViID { get; set; }

    public string TenDonVi
    {
        get => _tenDonVi;
        set => SetProperty(ref _tenDonVi, value);
    }

    public string HeSoText
    {
        get => _heSoText;
        set => SetProperty(ref _heSoText, value);
    }

    public bool LaDonViChuan
    {
        get => _laDonViChuan;
        set
        {
            if (SetProperty(ref _laDonViChuan, value) && value)
            {
                // Khi được tích, gọi command để bỏ tích các đơn vị khác
                SetDonViChuanCommand?.Execute(this);
            }
        }
    }

    /// <summary>
    /// Đặt LaDonViChuan mà không kích hoạt command (tránh vòng lặp vô hạn)
    /// </summary>
    public void SetDonViChuanSilent(bool value)
    {
        if (_laDonViChuan != value)
        {
            _laDonViChuan = value;
            OnPropertyChanged(nameof(LaDonViChuan));
        }
    }

    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    public ICommand? SetDonViChuanCommand { get; set; }

    private bool _isBaseUnit;
    public bool IsBaseUnit
    {
        get => _isBaseUnit;
        set => SetProperty(ref _isBaseUnit, value);
    }
}

// Item lô hàng nhập kho với HSD
public class LoHangItem : BaseViewModel
{
    public int PhieuNhapID { get; set; }
    public string MaPhieuNhap { get; set; } = string.Empty;
    public DateTime NgayNhap { get; set; }
    public int NguyenLieuID { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public decimal SoLuongNhap { get; set; }
    public DateTime? HSD { get; set; }
    public int? HanCanhBao { get; set; }
    public string NgayNhapText => NgayNhap.ToString("dd/MM/yyyy");
    public string HSDText => HSD?.ToString("dd/MM/yyyy") ?? "—";
    public bool HasHSD => HSD.HasValue;

    public int SoNgayConLai => HSD.HasValue ? (int)(HSD.Value.Date - DateTime.Today).TotalDays : int.MaxValue;

    public string TrangThaiHSD
    {
        get
        {
            if (!HSD.HasValue) return "Không có HSD";
            if (SoNgayConLai < 0) return "Đã hết hạn";
            if (SoNgayConLai == 0) return "Hết hạn hôm nay";
            if (SoNgayConLai <= (HanCanhBao ?? 7)) return $"Còn {SoNgayConLai} ngày";
            return "Còn hạn";
        }
    }

    public string MauTrangThai
    {
        get
        {
            if (!HSD.HasValue) return "#94A3B8";
            if (SoNgayConLai < 0) return "#EF4444";
            if (SoNgayConLai <= (HanCanhBao ?? 7)) return "#F59E0B";
            return "#10B981";
        }
    }

    public bool IsExpired => HSD.HasValue && SoNgayConLai < 0;
    public bool IsNearExpiry => HSD.HasValue && SoNgayConLai >= 0 && SoNgayConLai <= (HanCanhBao ?? 7);
}

public class TonKhoViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private LoaiNguyenLieu? _selectedLoaiNguyenLieu;
    private TonKhoItemViewModel? _selectedNguyenLieu;
    private bool _isQuyDoiPopupOpen;
    private bool _isAddQuyDoiPopupOpen;
    private bool _isEditPopupOpen;
    private bool _isEditing;
    private int _soNguyenLieuTonKho;
    private bool _isLoading;
    private string _searchText = string.Empty;
    private string _selectedFilter = "TatCa";
    private bool _canViewHeSoQuyDoi = true;
    private bool _canViewThemTonKho = true;
    private bool _canViewXuatKho = true;

    // Lô hàng & cảnh báo HSD
    private bool _isLoHangPopupOpen;
    private string _selectedLoHangTab = "CanhBao";
    private int _soLoHangCanhBao;

    // Cho popup thêm quy đổi
    private string _donViNhap = string.Empty;
    private DonViTinh? _selectedDonViXuat;
    private string _heSoNhap = string.Empty;

    // Cho popup quản lý đơn vị tính
    private bool _isDonViTinhPopupOpen;
    private string _newDonViTinhName = string.Empty;
    private DonViTinh? _editingDonViTinh;
    private bool _isEditingDonViTinh;

    // Cho popup chỉnh sửa
    private string _editTenNguyenLieu = string.Empty;
    private decimal _editSoLuongTon;
    private DonViTinh? _editDonViTinh;
    private LoaiNguyenLieu? _editLoaiNguyenLieu;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool IsEditPopupOpen
    {
        get => _isEditPopupOpen;
        set => SetProperty(ref _isEditPopupOpen, value);
    }

    public string EditTenNguyenLieu
    {
        get => _editTenNguyenLieu;
        set => SetProperty(ref _editTenNguyenLieu, value);
    }

    public decimal EditSoLuongTon
    {
        get => _editSoLuongTon;
        set => SetProperty(ref _editSoLuongTon, value);
    }

    public DonViTinh? EditDonViTinh
    {
        get => _editDonViTinh;
        set => SetProperty(ref _editDonViTinh, value);
    }

    public LoaiNguyenLieu? EditLoaiNguyenLieu
    {
        get => _editLoaiNguyenLieu;
        set => SetProperty(ref _editLoaiNguyenLieu, value);
    }

    public LoaiNguyenLieu? SelectedLoaiNguyenLieu
    {
        get => _selectedLoaiNguyenLieu;
        set
        {
            if (SetProperty(ref _selectedLoaiNguyenLieu, value))
            {
                _ = FilterNguyenLieuAsync();
            }
        }
    }

    public TonKhoItemViewModel? SelectedNguyenLieu
    {
        get => _selectedNguyenLieu;
        set
        {
            if (SetProperty(ref _selectedNguyenLieu, value))
            {
                _ = LoadQuyDoiDonViAsync();

                // Đặt đơn vị nhập
                if (value != null)
                {
                    DonViNhap = value.DonViTinh;
                }
            }
        }
    }

    public bool IsQuyDoiPopupOpen
    {
        get => _isQuyDoiPopupOpen;
        set => SetProperty(ref _isQuyDoiPopupOpen, value);
    }

    public bool IsAddQuyDoiPopupOpen
    {
        get => _isAddQuyDoiPopupOpen;
        set => SetProperty(ref _isAddQuyDoiPopupOpen, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public int SoNguyenLieuTonKho
    {
        get => _soNguyenLieuTonKho;
        set => SetProperty(ref _soNguyenLieuTonKho, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string DonViNhap
    {
        get => _donViNhap;
        set => SetProperty(ref _donViNhap, value);
    }

    public DonViTinh? SelectedDonViXuat
    {
        get => _selectedDonViXuat;
        set => SetProperty(ref _selectedDonViXuat, value);
    }

    public string HeSoNhap
    {
        get => _heSoNhap;
        set => SetProperty(ref _heSoNhap, value);
    }

    public ObservableCollection<LoaiNguyenLieu> LoaiNguyenLieus { get; } = new();
    public ObservableCollection<TonKhoItemViewModel> TonKhoItems { get; } = new();
    public ObservableCollection<TonKhoItemViewModel> FilteredTonKhoItems { get; } = new();
    public ObservableCollection<TonKhoItemViewModel> FilteredNguyenLieus { get; } = new();
    public ObservableCollection<QuyDoiDonViItemViewModel> QuyDoiDonVis { get; } = new();
    public ObservableCollection<DonViTinh> DonViTinhs { get; } = new();
    public ObservableCollection<LoHangItem> AllLoHangItems { get; } = new();
    public ObservableCollection<LoHangItem> LoHangCanhBaoItems { get; } = new();

    public bool IsLoHangPopupOpen
    {
        get => _isLoHangPopupOpen;
        set => SetProperty(ref _isLoHangPopupOpen, value);
    }

    public string SelectedLoHangTab
    {
        get => _selectedLoHangTab;
        set => SetProperty(ref _selectedLoHangTab, value);
    }

    public int SoLoHangCanhBao
    {
        get => _soLoHangCanhBao;
        set => SetProperty(ref _soLoHangCanhBao, value);
    }

    // Lệnh
    public ICommand OpenQuyDoiPopupCommand { get; private set; } = null!;
    public ICommand CloseQuyDoiPopupCommand { get; private set; } = null!;
    public ICommand SelectNguyenLieuCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand AddQuyDoiCommand { get; private set; } = null!;
    public ICommand OpenAddQuyDoiPopupCommand { get; private set; } = null!;
    public ICommand CloseAddQuyDoiPopupCommand { get; private set; } = null!;
    public ICommand SaveNewQuyDoiCommand { get; private set; } = null!;
    public ICommand BackCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand EditItemCommand { get; private set; } = null!;
    public ICommand DeleteItemCommand { get; private set; } = null!;
    public ICommand FilterTatCaCommand { get; private set; } = null!;
    public ICommand FilterTonThapCommand { get; private set; } = null!;
    public ICommand FilterTonCaoCommand { get; private set; } = null!;
    public ICommand OpenEditPopupCommand { get; private set; } = null!;
    public ICommand CloseEditPopupCommand { get; private set; } = null!;
    public ICommand SaveEditCommand { get; private set; } = null!;
    public ICommand EditQuyDoiCommand { get; private set; } = null!;
    public ICommand DeleteQuyDoiCommand { get; private set; } = null!;
    public ICommand SetDonViChuanCommand { get; private set; } = null!;
    public ICommand GoToPhieuNhapCommand { get; private set; } = null!;
    public ICommand GoToPhieuXuatCommand { get; private set; } = null!;
    public ICommand OpenLoHangPopupCommand { get; private set; } = null!;
    public ICommand CloseLoHangPopupCommand { get; private set; } = null!;
    public ICommand SwitchLoHangTabCommand { get; private set; } = null!;
    public ICommand ViewPhieuNhapCommand { get; private set; } = null!;
    public ICommand ViewExpiryDetailForItemCommand { get; private set; } = null!;
    public ICommand CloseNguyenLieuLoHangPopupCommand { get; private set; } = null!;

    // Popup quản lý đơn vị tính
    public ICommand OpenDonViTinhPopupCommand { get; private set; } = null!;
    public ICommand CloseDonViTinhPopupCommand { get; private set; } = null!;
    public ICommand SaveDonViTinhCommand { get; private set; } = null!;
    public ICommand EditDonViTinhCommand { get; private set; } = null!;
    public ICommand DeleteDonViTinhCommand { get; private set; } = null!;
    public ICommand CancelEditDonViTinhCommand { get; private set; } = null!;

    public bool IsDonViTinhPopupOpen
    {
        get => _isDonViTinhPopupOpen;
        set => SetProperty(ref _isDonViTinhPopupOpen, value);
    }

    public string NewDonViTinhName
    {
        get => _newDonViTinhName;
        set => SetProperty(ref _newDonViTinhName, value);
    }

    public DonViTinh? EditingDonViTinh
    {
        get => _editingDonViTinh;
        set => SetProperty(ref _editingDonViTinh, value);
    }

    public bool IsEditingDonViTinh
    {
        get => _isEditingDonViTinh;
        set => SetProperty(ref _isEditingDonViTinh, value);
    }

    // Popup lô hàng theo nguyên liệu
    private bool _isNguyenLieuLoHangPopupOpen;
    public bool IsNguyenLieuLoHangPopupOpen
    {
        get => _isNguyenLieuLoHangPopupOpen;
        set => SetProperty(ref _isNguyenLieuLoHangPopupOpen, value);
    }

    private string _selectedNguyenLieuName = string.Empty;
    public string SelectedNguyenLieuName
    {
        get => _selectedNguyenLieuName;
        set => SetProperty(ref _selectedNguyenLieuName, value);
    }

    public ObservableCollection<LoHangItem> SelectedNguyenLieuLoHang { get; } = new();

    /// <summary>
    /// Ẩn nút "Hệ số quy đổi" với nhân viên kho và nhân viên bếp
    /// </summary>
    public bool CanViewHeSoQuyDoi
    {
        get => _canViewHeSoQuyDoi;
        set => SetProperty(ref _canViewHeSoQuyDoi, value);
    }

    /// <summary>
    /// Ẩn nút "Thêm tồn kho" (→ Phiếu Nhập) với nhân viên bếp
    /// </summary>
    public bool CanViewThemTonKho
    {
        get => _canViewThemTonKho;
        set => SetProperty(ref _canViewThemTonKho, value);
    }

    /// <summary>
    /// Ẩn nút "Xuất kho" (→ Phiếu Xuất) với nhân viên kho
    /// </summary>
    public bool CanViewXuatKho
    {
        get => _canViewXuatKho;
        set => SetProperty(ref _canViewXuatKho, value);
    }

    public event Action? OnBack;
    public event Action? OnNavigateToPhieuNhap;
    public event Action? OnNavigateToPhieuXuat;
    public event Action<int>? OnNavigateToPhieuNhapDetail;

    public TonKhoViewModel()
    {
        try
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }
        catch
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }

        InitializeCommands();
        SetupRolePermissions();

        // Thay fire-and-forget bằng SafeInitializeAsync — bắt lỗi thay vì nuốt chìm
        SafeInitializeAsync(LoadDataAsync);
    }

    public TonKhoViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeCommands();
        SetupRolePermissions();

        SafeInitializeAsync(LoadDataAsync);
    }

    /// <summary>
    /// Thiết lập quyền theo vai trò: nhân viên kho + nhân viên bếp không thấy Hệ số quy đổi
    /// </summary>
    private void SetupRolePermissions()
    {
        var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
        var chucVuTen = (nhanVien?.ChucVu?.TenChucVu?.Trim() ?? "").ToLower();

        // Kiểm tra theo tên chức vụ (không dùng ChucVuID vì ID tự động tăng)
        bool isQuanLy = chucVuTen.Contains("quản lý") || chucVuTen.Contains("quan ly");
        bool isNhanVienBep = !isQuanLy && (chucVuTen.Contains("bếp") || chucVuTen.Contains("bep"));
        bool isNhanVienKho = !isQuanLy && !isNhanVienBep && chucVuTen.Contains("kho");

        // Nhân viên kho + nhân viên bếp → ẩn hệ số quy đổi
        CanViewHeSoQuyDoi = !(isNhanVienKho || isNhanVienBep);

        // Nhân viên bếp: ẩn nút "Thêm tồn kho" (phiếu nhập)
        CanViewThemTonKho = !isNhanVienBep;

        // Nhân viên kho: ẩn nút "Xuất kho" (phiếu xuất)
        CanViewXuatKho = !isNhanVienKho;
    }

    private void InitializeCommands()
    {
        OpenQuyDoiPopupCommand = new RelayCommand(_ => IsQuyDoiPopupOpen = true);
        CloseQuyDoiPopupCommand = new RelayCommand(_ =>
        {
            IsQuyDoiPopupOpen = false;
            IsEditing = false;
            SelectedNguyenLieu = null;
        });
        SelectNguyenLieuCommand = new RelayCommand(ExecuteSelectNguyenLieu);
        EditCommand = new RelayCommand(_ => IsEditing = true);
        SaveCommand = new AsyncRelayCommand(async _ => await ExecuteSaveAsync());
        AddQuyDoiCommand = new RelayCommand(_ =>
        {
            IsAddQuyDoiPopupOpen = true;
            HeSoNhap = string.Empty;
            SelectedDonViXuat = null;
        });
        OpenAddQuyDoiPopupCommand = new RelayCommand(_ => IsAddQuyDoiPopupOpen = true);
        CloseAddQuyDoiPopupCommand = new RelayCommand(_ => IsAddQuyDoiPopupOpen = false);
        SaveNewQuyDoiCommand = new AsyncRelayCommand(async _ => await ExecuteSaveNewQuyDoiAsync());
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        EditItemCommand = new RelayCommand(ExecuteEditItem);
        DeleteItemCommand = new AsyncRelayCommand(async param => await ExecuteDeleteItemAsync(param));

        // Lệnh lọc
        FilterTatCaCommand = new RelayCommand(_ => SelectedFilter = "TatCa");
        FilterTonThapCommand = new RelayCommand(_ => SelectedFilter = "TonThap");
        FilterTonCaoCommand = new RelayCommand(_ => SelectedFilter = "TonCao");

        // Lệnh popup chỉnh sửa
        OpenEditPopupCommand = new RelayCommand(ExecuteOpenEditPopup);
        CloseEditPopupCommand = new RelayCommand(_ => IsEditPopupOpen = false);
        SaveEditCommand = new AsyncRelayCommand(async _ => await ExecuteSaveEditAsync());

        // Lệnh sửa/xóa quy đổi
        EditQuyDoiCommand = new RelayCommand(ExecuteEditQuyDoi);
        DeleteQuyDoiCommand = new AsyncRelayCommand(async param => await ExecuteDeleteQuyDoiAsync(param));
        SetDonViChuanCommand = new RelayCommand(ExecuteSetDonViChuan);

        // Lệnh chuyển trang
        GoToPhieuNhapCommand = new RelayCommand(_ => OnNavigateToPhieuNhap?.Invoke());
        GoToPhieuXuatCommand = new RelayCommand(_ => OnNavigateToPhieuXuat?.Invoke());

        // Lô hàng & cảnh báo HSD
        OpenLoHangPopupCommand = new AsyncRelayCommand(async _ =>
        {
            await LoadLoHangDataAsync();
            SelectedLoHangTab = "CanhBao";
            IsLoHangPopupOpen = true;
        });
        CloseLoHangPopupCommand = new RelayCommand(_ => IsLoHangPopupOpen = false);
        SwitchLoHangTabCommand = new RelayCommand(p =>
        {
            if (p is string tab) SelectedLoHangTab = tab;
        });
        ViewPhieuNhapCommand = new RelayCommand(p =>
        {
            if (p is int phieuNhapId)
            {
                OnNavigateToPhieuNhapDetail?.Invoke(phieuNhapId);
            }
        });
        ViewExpiryDetailForItemCommand = new AsyncRelayCommand(async p =>
        {
            if (p is TonKhoItemViewModel item)
            {
                SelectedNguyenLieuName = item.TenNguyenLieu;
                SelectedNguyenLieuLoHang.Clear();

                try
                {
                    var allBatches = await _databaseService.GetTonKhoTheoLoHangAsync();
                    var filteredBatches = allBatches
                        .Where(lo => lo.NguyenLieuID == item.NguyenLieuID)
                        .OrderBy(lo => lo.HSD ?? DateTime.MaxValue)
                        .ThenBy(lo => lo.NgayNhap)
                        .ToList();

                    foreach (var (phieuNhapID, maPN, ngayNhap, nlId, tenNL, donVi, soLuong, hsd, hanCanhBao) in filteredBatches)
                    {
                        SelectedNguyenLieuLoHang.Add(new LoHangItem
                        {
                            PhieuNhapID = phieuNhapID,
                            MaPhieuNhap = maPN,
                            NgayNhap = ngayNhap,
                            NguyenLieuID = nlId,
                            TenNguyenLieu = tenNL,
                            DonVi = donVi,
                            SoLuongNhap = soLuong,
                            HSD = hsd,
                            HanCanhBao = hanCanhBao
                        });
                    }
                }
                catch { }

                IsNguyenLieuLoHangPopupOpen = true;
            }
        });
        CloseNguyenLieuLoHangPopupCommand = new RelayCommand(_ => IsNguyenLieuLoHangPopupOpen = false);

        // Popup quản lý đơn vị tính
        OpenDonViTinhPopupCommand = new RelayCommand(_ =>
        {
            NewDonViTinhName = string.Empty;
            EditingDonViTinh = null;
            IsEditingDonViTinh = false;
            IsDonViTinhPopupOpen = true;
        });
        CloseDonViTinhPopupCommand = new RelayCommand(_ =>
        {
            IsDonViTinhPopupOpen = false;
            IsEditingDonViTinh = false;
            EditingDonViTinh = null;
            NewDonViTinhName = string.Empty;
        });
        SaveDonViTinhCommand = new AsyncRelayCommand(async _ => await ExecuteSaveDonViTinhAsync());
        EditDonViTinhCommand = new RelayCommand(p =>
        {
            if (p is DonViTinh dv)
            {
                EditingDonViTinh = dv;
                NewDonViTinhName = dv.TenDonVi;
                IsEditingDonViTinh = true;
            }
        });
        DeleteDonViTinhCommand = new AsyncRelayCommand(async p =>
        {
            if (p is DonViTinh dv)
                await ExecuteDeleteDonViTinhAsync(dv);
        });
        CancelEditDonViTinhCommand = new RelayCommand(_ =>
        {
            IsEditingDonViTinh = false;
            EditingDonViTinh = null;
            NewDonViTinhName = string.Empty;
        });
    }

    private void ApplyFilters()
    {
        // Tính toán filter ngoài UI thread, chỉ swap collection trên UI
        var filtered = TonKhoItems.AsEnumerable();

        // Áp dụng bộ lọc tìm kiếm
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(x =>
                x.TenNguyenLieu.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                x.MaNguyenLieu.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Áp dụng bộ lọc trạng thái
        if (SelectedFilter == "TonThap")
        {
            filtered = filtered.Where(x => x.IsLowStock || x.MucDoTonKho.Contains("Thấp", StringComparison.OrdinalIgnoreCase));
        }
        else if (SelectedFilter == "TonCao")
        {
            filtered = filtered.Where(x => !x.IsLowStock && x.MucDoTonKho.Contains("Cao", StringComparison.OrdinalIgnoreCase));
        }

        // Materialize danh sách trước rồi mới swap trên UI
        var result = filtered.ToList();

        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // Thay vì Clear + Add từng cái (N lần UI vẽ lại),
            // dùng ReplaceAll chỉ vẽ lại 1 lần
            if (FilteredTonKhoItems is RangeObservableCollection<TonKhoItemViewModel> rangeCollection)
            {
                rangeCollection.ReplaceAll(result);
            }
            else
            {
                FilteredTonKhoItems.Clear();
                foreach (var item in result)
                    FilteredTonKhoItems.Add(item);
            }
        });

    }

    private void ExecuteOpenEditPopup(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            SelectedNguyenLieu = item;
            EditTenNguyenLieu = item.TenNguyenLieu;
            EditSoLuongTon = item.SoLuongTon;
            EditDonViTinh = DonViTinhs.FirstOrDefault(d => d.TenDonVi == item.DonViTinh);
            IsEditPopupOpen = true;
        }
    }

    private async Task ExecuteSaveEditAsync()
    {
        if (SelectedNguyenLieu == null) return;

        if (EditSoLuongTon < 0)
        {
            System.Windows.MessageBox.Show(
                "Số lượng tồn kho không được là số âm!",
                "Cảnh báo",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Cập nhật tồn kho
            var success = await _databaseService.UpdateTonKhoAsync(SelectedNguyenLieu.NguyenLieuID, EditSoLuongTon);

            if (success)
            {
                // Cập nhật giao diện
                SelectedNguyenLieu.SoLuongTon = EditSoLuongTon;
                SelectedNguyenLieu.MucDoTonKho = GetMucDoTonKho(EditSoLuongTon);

                IsEditPopupOpen = false;
                await LoadTonKhoAsync();
            }
        }
        catch { }
    }

    private string GetMucDoTonKho(decimal soLuong, decimal heSoChuan = 1m)
    {
        if (soLuong <= 0) return "Hết hàng";
        // Quy đổi về đơn vị gốc để so sánh ngưỡng nhất quán
        var soLuongGoc = soLuong * heSoChuan;
        return soLuongGoc < 20 ? "Thấp" : (soLuongGoc < 50 ? "Trung bình" : "Cao");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            // Tải dữ liệu ngoài UI thread
            var loaiNLs = await _databaseService.GetLoaiNguyenLieusAsync();
            var donVis = await _databaseService.GetDonViTinhsAsync();

            // Cập nhật UI bằng BeginInvoke (không block) thay vì Invoke (block)
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LoaiNguyenLieus.Clear();
                LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 0, TenLoai = "Tất cả" });
                foreach (var loai in loaiNLs)
                    LoaiNguyenLieus.Add(loai);

                if (LoaiNguyenLieus.Count > 0)
                    SelectedLoaiNguyenLieu = LoaiNguyenLieus[0];

                DonViTinhs.Clear();
                foreach (var dv in donVis)
                    DonViTinhs.Add(dv);
            });

            // Tải dữ liệu tồn kho
            await LoadTonKhoAsync();

            // Tải nguyên liệu cho popup
            await FilterNguyenLieuAsync();

            // Tải số lượng lô hàng cảnh báo HSD
            try
            {
                var canhBaoData = await _databaseService.GetLoHangCanhBaoHSDAsync();
                SoLoHangCanhBao = canhBaoData.Count;

                // Tính HasExpirySoon cho từng nguyên liệu
                var nguyenLieuIdsWithExpiry = canhBaoData
                    .Select(lo => lo.NguyenLieuID)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToHashSet();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in TonKhoItems)
                    {
                        item.HasExpirySoon = nguyenLieuIdsWithExpiry.Contains(item.NguyenLieuID);
                    }
                });
            }
            catch { SoLoHangCanhBao = 0; }
        }
        catch { }
        finally
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task LoadTonKhoAsync()
    {
        var tonKhos = await _databaseService.GetTonKhosAsync();

        // Chuẩn bị dữ liệu ngoài UI thread
        var items = tonKhos.Select(tk => new TonKhoItemViewModel
        {
            NguyenLieuID = tk.NguyenLieuID ?? 0,
            MaNguyenLieu = tk.NguyenLieu?.MaNguyenLieu ?? "",
            TenNguyenLieu = tk.NguyenLieu?.TenNguyenLieu ?? "",
            HinhAnh = tk.NguyenLieu?.HinhAnh,
            SoLuongTon = tk.SoLuongTon,
            DonViTinh = tk.NguyenLieu?.DonViTinh?.TenDonVi ?? "",
            MucDoTonKho = GetMucDoTonKho(tk.SoLuongTon, tk.HeSoChuan),
            EditCommand = OpenEditPopupCommand,
            DeleteCommand = DeleteItemCommand,
            ViewExpiryDetailCommand = ViewExpiryDetailForItemCommand
        }).ToList();

        // Chỉ cập nhật UI 1 lần với tất cả dữ liệu
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            TonKhoItems.Clear();
            foreach (var item in items)
                TonKhoItems.Add(item);

            SoNguyenLieuTonKho = TonKhoItems.Count;
            ApplyFilters();
        });
    }

    private async Task FilterNguyenLieuAsync()
    {
        try
        {
            int? loaiID = SelectedLoaiNguyenLieu?.LoaiNLID;
            if (loaiID == 0) loaiID = null;

            var nguyenLieus = await _databaseService.GetNguyenLieusAsync(loaiID);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredNguyenLieus.Clear();

                foreach (var nl in nguyenLieus)
                {
                    var tonKhoItem = TonKhoItems.FirstOrDefault(t => t.NguyenLieuID == nl.NguyenLieuID);

                    FilteredNguyenLieus.Add(new TonKhoItemViewModel
                    {
                        NguyenLieuID = nl.NguyenLieuID,
                        MaNguyenLieu = nl.MaNguyenLieu ?? "",
                        TenNguyenLieu = nl.TenNguyenLieu,
                        HinhAnh = nl.HinhAnh,
                        SoLuongTon = tonKhoItem?.SoLuongTon ?? 0,
                        DonViTinh = nl.DonViTinh?.TenDonVi ?? "",
                        MucDoTonKho = tonKhoItem?.MucDoTonKho ?? "Chưa có",
                        EditCommand = OpenEditPopupCommand,
                        DeleteCommand = DeleteItemCommand
                    });
                }
            });
        }
        catch {
            System.Windows.Application.Current.Dispatcher.Invoke(() => FilteredNguyenLieus.Clear());
        }
    }

    private void ExecuteSelectNguyenLieu(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            SelectedNguyenLieu = item;
        }
    }

    private async Task LoadQuyDoiDonViAsync()
    {
        QuyDoiDonVis.Clear();

        if (SelectedNguyenLieu == null) return;

        try
        {
            var quyDois = await _databaseService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);

            // Tìm đơn vị gốc của nguyên liệu
            var donViGoc = DonViTinhs.FirstOrDefault(d => d.TenDonVi == SelectedNguyenLieu.DonViTinh);
            bool baseUnitAdded = false;

            if (quyDois.Count > 0)
            {
                // Thêm đơn vị gốc lên đầu tiên nếu có trong danh sách
                var baseQuyDoi = donViGoc != null
                    ? quyDois.FirstOrDefault(qd => qd.DonViID == donViGoc.DonViID)
                    : null;

                if (baseQuyDoi != null)
                {
                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = baseQuyDoi.QuyDoiID,
                        DonViID = baseQuyDoi.DonViID,
                        TenDonVi = baseQuyDoi.DonViTinh?.TenDonVi ?? "",
                        HeSoText = baseQuyDoi.HeSo.ToString("0.####"),
                        LaDonViChuan = baseQuyDoi.LaDonViChuan,
                        IsBaseUnit = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                    baseUnitAdded = true;
                }

                foreach (var qd in quyDois)
                {
                    // Bỏ qua đơn vị gốc vì đã thêm ở trên
                    if (baseQuyDoi != null && qd.QuyDoiID == baseQuyDoi.QuyDoiID)
                        continue;

                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = qd.QuyDoiID,
                        DonViID = qd.DonViID,
                        TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                        HeSoText = qd.HeSo.ToString("0.####"),
                        LaDonViChuan = qd.LaDonViChuan,
                        IsBaseUnit = false,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
            }

            // Nếu chưa thêm đơn vị gốc, thêm mặc định lên đầu
            if (!baseUnitAdded)
            {

                if (donViGoc != null)
                {
                    QuyDoiDonVis.Insert(0, new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = 0,
                        DonViID = donViGoc.DonViID,
                        TenDonVi = donViGoc.TenDonVi,
                        HeSoText = "1",
                        LaDonViChuan = true,
                        IsBaseUnit = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
                else
                {
                    QuyDoiDonVis.Insert(0, new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = 0,
                        DonViID = null,
                        TenDonVi = SelectedNguyenLieu.DonViTinh,
                        HeSoText = "1",
                        LaDonViChuan = true,
                        IsBaseUnit = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
            }

        }
        catch { }
    }

    private async Task ExecuteSaveAsync()
    {
        if (SelectedNguyenLieu == null) return;

        // Validate: phải có ít nhất 1 đơn vị chuẩn
        if (QuyDoiDonVis.Any() && !QuyDoiDonVis.Any(r => r.LaDonViChuan))
        {
            System.Windows.MessageBox.Show(
                "Phải có ít nhất 1 đơn vị được chọn làm đơn vị lưu tồn kho!",
                "Thiếu đơn vị lưu tồn kho",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Lấy trạng thái cũ từ DB TRƯỚC KHI lưu để phát hiện thay đổi đơn vị chuẩn
            var oldQuyDois = await _databaseService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);
            var oldDonViChuan = oldQuyDois.FirstOrDefault(qd => qd.LaDonViChuan);

            foreach (var qd in QuyDoiDonVis)
            {
                var cleanText = qd.HeSoText?.Replace(",", ".") ?? "";
                if (!decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedHeSo) || parsedHeSo <= 0)
                {
                    System.Windows.MessageBox.Show(
                        $"Hệ số của đơn vị '{qd.TenDonVi}' không hợp lệ! Phải là số dương.",
                        "Lỗi hệ số",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var quyDoi = new QuyDoiDonVi
                {
                    QuyDoiID = qd.QuyDoiID,
                    NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
                    DonViID = qd.DonViID,
                    HeSo = parsedHeSo,
                    LaDonViChuan = qd.LaDonViChuan
                };

                await _databaseService.SaveQuyDoiDonViAsync(quyDoi);
            }

            // So sánh đơn vị chuẩn CŨ (từ DB) với đơn vị chuẩn MỚI (từ UI)
            var newDonViChuan = QuyDoiDonVis.FirstOrDefault(qd => qd.LaDonViChuan);
            if (newDonViChuan?.DonViID != null &&
                (oldDonViChuan == null || oldDonViChuan.DonViID != newDonViChuan.DonViID))
            {
                // Quy đổi số lượng tồn kho theo đơn vị chuẩn mới
                // BUG FIX: Lấy hệ số cũ từ giao diện (QuyDoiDonVis) thay vì từ DB (oldDonViChuan)
                // Vì người dùng có thể đang sửa cả hệ số cũ trong cùng 1 lần lưu!
                var uiOldDonVi = QuyDoiDonVis.FirstOrDefault(qd => qd.DonViID == oldDonViChuan?.DonViID);
                decimal oldHeSo = 1m;
                if (uiOldDonVi != null)
                {
                    var cleanOld = uiOldDonVi.HeSoText?.Replace(",", ".") ?? "";
                    if (decimal.TryParse(cleanOld, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedOldHeSo))
                    {
                        oldHeSo = parsedOldHeSo;
                    }
                }
                else if (oldDonViChuan != null)
                {
                    oldHeSo = oldDonViChuan.HeSo;
                }

                decimal newHeSo = 1m;
                var cleanNew = newDonViChuan.HeSoText?.Replace(",", ".") ?? "";
                if (decimal.TryParse(cleanNew, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedNewHeSo))
                {
                    newHeSo = parsedNewHeSo;
                }

                if (oldHeSo > 0 && newHeSo > 0)
                {
                    decimal conversionFactor = oldHeSo / newHeSo;
                    decimal newSoLuongTon = SelectedNguyenLieu.SoLuongTon * conversionFactor;

                    // Cập nhật cả số lượng VÀ đơn vị trong bảng TonKho
                    await _databaseService.UpdateTonKhoDonViAsync(
                        SelectedNguyenLieu.NguyenLieuID, newSoLuongTon, newDonViChuan.DonViID ?? 0);

                    // BUG FIX: Cập nhật lại số lượng tồn + đơn vị trên UI
                    SelectedNguyenLieu.SoLuongTon = newSoLuongTon;
                    SelectedNguyenLieu.MucDoTonKho = GetMucDoTonKho(newSoLuongTon);
                    SelectedNguyenLieu.DonViTinh = newDonViChuan.TenDonVi;
                }

                // Lưu ý: KHÔNG thay đổi DonViID trong bảng NguyenLieu
                // Đơn vị gốc của nguyên liệu luôn giữ nguyên, chỉ đổi đơn vị chuẩn + TonKho.DonViID
            }

            System.Windows.MessageBox.Show(
                "Đã lưu hệ số thành công!",
                "Thành công",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            IsEditing = false;

            // Reload lại để cập nhật QuyDoiID mới và tránh trùng lặp
            await LoadQuyDoiDonViAsync();

            // Reload bảng tồn kho để hiển thị đơn vị mới
            await LoadTonKhoAsync();
        }
        catch (Exception ex) {
            System.Windows.MessageBox.Show(
                $"Lỗi khi lưu: {ex.Message}",
                "Lỗi",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ExecuteSaveNewQuyDoiAsync()
    {
        // Validate: phải chọn nguyên liệu
        if (SelectedNguyenLieu == null)
        {
            System.Windows.MessageBox.Show(
                "Vui lòng chọn nguyên liệu trước!",
                "Thiếu thông tin",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Validate: phải chọn đơn vị quy đổi
        if (SelectedDonViXuat == null)
        {
            System.Windows.MessageBox.Show(
                "Vui lòng chọn đơn vị quy đổi!",
                "Thiếu thông tin",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Validate: kiểm tra trùng lặp trong danh sách hiện tại (bao gồm cả đơn vị gốc nếu đã có)
        if (QuyDoiDonVis.Any(qd => qd.DonViID == SelectedDonViXuat.DonViID))
        {
            System.Windows.MessageBox.Show(
                $"Đơn vị '{SelectedDonViXuat.TenDonVi}' đã tồn tại trong bảng quy đổi!",
                "Trùng lặp",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Validate: hệ số phải là số dương
        var heSoText = HeSoNhap?.Replace(",", ".") ?? "";
        if (string.IsNullOrWhiteSpace(HeSoNhap))
        {
            System.Windows.MessageBox.Show(
                "Vui lòng nhập hệ số quy đổi!",
                "Thiếu thông tin",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(heSoText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal heSo) || heSo <= 0)
        {
            System.Windows.MessageBox.Show(
                "Hệ số phải là một số dương hợp lệ!",
                "Lỗi hệ số",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var quyDoi = new QuyDoiDonVi
            {
                QuyDoiID = 0,
                NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
                DonViID = SelectedDonViXuat.DonViID,
                HeSo = heSo,
                LaDonViChuan = false
            };

            var success = await _databaseService.SaveQuyDoiDonViAsync(quyDoi);

            if (success)
            {
                IsAddQuyDoiPopupOpen = false;
                HeSoNhap = string.Empty;
                SelectedDonViXuat = null;

                // Reload lại danh sách từ DB để tránh trùng lặp
                await LoadQuyDoiDonViAsync();

                System.Windows.MessageBox.Show(
                    "Đã thêm hệ số quy đổi thành công!",
                    "Thành công",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex) {
            System.Windows.MessageBox.Show(
                $"Lỗi khi thêm quy đổi: {ex.Message}",
                "Lỗi",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ExecuteEditItem(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            ExecuteOpenEditPopup(item);
        }
    }

    private async Task ExecuteDeleteItemAsync(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            var confirmed = await ShowDeleteConfirmation(
                item.TenNguyenLieu,
                "Xóa nguyên liệu",
                $"Bạn có chắc chắn muốn xóa nguyên liệu \"{item.TenNguyenLieu}\"?\nTất cả dữ liệu liên quan sẽ bị xóa theo.");
            if (!confirmed) return;

            try
            {
                var success = await _databaseService.DeleteNguyenLieuAsync(item.NguyenLieuID);

                if (success)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TonKhoItems.Remove(item);
                        FilteredNguyenLieus.Remove(item);
                        SoNguyenLieuTonKho = TonKhoItems.Count;
                    });
                }
            }
            catch { }
        }
    }

    private void ExecuteEditQuyDoi(object? parameter)
    {
        if (parameter is QuyDoiDonViItemViewModel quyDoi)
        {
            // Bật chế độ chỉnh sửa cho mục cụ thể này
            IsEditing = true;
        }
    }

    private void ExecuteSetDonViChuan(object? parameter)
    {
        if (parameter is QuyDoiDonViItemViewModel selectedItem)
        {
            // Bỏ tích tất cả các đơn vị khác, chỉ giữ đơn vị được chọn
            foreach (var item in QuyDoiDonVis)
            {
                if (item != selectedItem && item.LaDonViChuan)
                {
                    item.SetDonViChuanSilent(false);
                }
            }
        }
    }

    private async Task ExecuteDeleteQuyDoiAsync(object? parameter)
    {
        if (parameter is QuyDoiDonViItemViewModel quyDoi)
        {
            // Không cho phép xóa đơn vị gốc
            if (quyDoi.IsBaseUnit)
            {
                System.Windows.MessageBox.Show(
                    "Không thể xóa đơn vị gốc của nguyên liệu!",
                    "Không được phép",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var confirmed = await ShowDeleteConfirmation(
                quyDoi.TenDonVi,
                "Xóa hệ số quy đổi",
                $"Bạn có chắc chắn muốn xóa hệ số quy đổi \"{quyDoi.TenDonVi}\"?\nHành động này không thể hoàn tác.");
            if (!confirmed) return;

            try
            {
                if (quyDoi.QuyDoiID > 0)
                {
                    var success = await _databaseService.DeleteQuyDoiDonViAsync(quyDoi.QuyDoiID);

                    if (success)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            QuyDoiDonVis.Remove(quyDoi);
                        });
                    }
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        QuyDoiDonVis.Remove(quyDoi);
                    });
                }
            }
            catch { }
        }
    }

    private async Task LoadLoHangDataAsync()
    {
        try
        {
            var allBatches = await _databaseService.GetTonKhoTheoLoHangAsync();
            var warningBatches = await _databaseService.GetLoHangCanhBaoHSDAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AllLoHangItems.Clear();
                foreach (var (phieuNhapID, maPN, ngayNhap, nlId, tenNL, donVi, soLuong, hsd, hanCB) in allBatches)
                {
                    AllLoHangItems.Add(new LoHangItem
                    {
                        PhieuNhapID = phieuNhapID,
                        MaPhieuNhap = maPN,
                        NgayNhap = ngayNhap,
                        NguyenLieuID = nlId,
                        TenNguyenLieu = tenNL,
                        DonVi = donVi,
                        SoLuongNhap = soLuong,
                        HSD = hsd,
                        HanCanhBao = hanCB
                    });
                }

                LoHangCanhBaoItems.Clear();
                foreach (var (phieuNhapID, maPN, ngayNhap, nlId, tenNL, donVi, soLuong, hsd, soNgayConLai) in warningBatches)
                {
                    LoHangCanhBaoItems.Add(new LoHangItem
                    {
                        PhieuNhapID = phieuNhapID,
                        MaPhieuNhap = maPN,
                        NgayNhap = ngayNhap,
                        NguyenLieuID = nlId,
                        TenNguyenLieu = tenNL,
                        DonVi = donVi,
                        SoLuongNhap = soLuong,
                        HSD = hsd
                    });
                }

                SoLoHangCanhBao = LoHangCanhBaoItems.Count;
            });
        }
        catch { }
    }

    private async Task ExecuteSaveDonViTinhAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDonViTinhName))
        {
            System.Windows.MessageBox.Show("Vui lòng nhập tên đơn vị tính!", "Thiếu thông tin",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Kiểm tra trùng tên
        var existing = DonViTinhs.FirstOrDefault(d =>
            d.TenDonVi.Equals(NewDonViTinhName.Trim(), StringComparison.OrdinalIgnoreCase)
            && d.DonViID != (EditingDonViTinh?.DonViID ?? 0));
        if (existing != null)
        {
            System.Windows.MessageBox.Show($"Đơn vị tính '{NewDonViTinhName.Trim()}' đã tồn tại!", "Trùng lặp",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var donVi = new DonViTinh
            {
                DonViID = EditingDonViTinh?.DonViID ?? 0,
                TenDonVi = NewDonViTinhName.Trim()
            };

            var success = await _databaseService.SaveDonViTinhAsync(donVi);
            if (success)
            {
                // Reload danh sách đơn vị tính
                var donVis = await _databaseService.GetDonViTinhsAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    DonViTinhs.Clear();
                    foreach (var dv in donVis)
                        DonViTinhs.Add(dv);
                });

                NewDonViTinhName = string.Empty;
                EditingDonViTinh = null;
                IsEditingDonViTinh = false;

                System.Windows.MessageBox.Show(
                    IsEditingDonViTinh ? "Đã cập nhật đơn vị tính!" : "Đã thêm đơn vị tính mới!",
                    "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task ExecuteDeleteDonViTinhAsync(DonViTinh dv)
    {
        var result = System.Windows.MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa đơn vị tính \"{dv.TenDonVi}\"?\nHành động này không thể hoàn tác.",
            "Xóa đơn vị tính",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var success = await _databaseService.DeleteDonViTinhAsync(dv.DonViID);
            if (success)
            {
                var donVis = await _databaseService.GetDonViTinhsAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    DonViTinhs.Clear();
                    foreach (var d in donVis)
                        DonViTinhs.Add(d);
                });

                System.Windows.MessageBox.Show("Đã xóa đơn vị tính!", "Thành công",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Không thể xóa: {ex.Message}\nĐơn vị tính này có thể đang được sử dụng.",
                "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
