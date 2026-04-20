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
    
    public bool IsLowStock => MucDoTonKho == "Thấp";
    
    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class QuyDoiDonViItemViewModel : BaseViewModel
{
    private bool _laDonViChuan;
    private string _tenDonVi = string.Empty;
    private decimal _heSo;
    
    public int QuyDoiID { get; set; }
    public int? DonViID { get; set; }
    
    public string TenDonVi
    {
        get => _tenDonVi;
        set => SetProperty(ref _tenDonVi, value);
    }
    
    public decimal HeSo
    {
        get => _heSo;
        set => SetProperty(ref _heSo, value / 1.000000000000000000000000000000000m);
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
    
    // Cho popup thêm quy đổi
    private string _donViNhap = string.Empty;
    private DonViTinh? _selectedDonViXuat;
    private string _heSoNhap = string.Empty;
    
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

    public event Action? OnBack;
    public event Action? OnNavigateToPhieuNhap;
    public event Action? OnNavigateToPhieuXuat;

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
        
        // ⚡ Thay fire-and-forget bằng SafeInitializeAsync — bắt lỗi thay vì nuốt chìm
        SafeInitializeAsync(LoadDataAsync);
    }

    public TonKhoViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeCommands();
        
        SafeInitializeAsync(LoadDataAsync);
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
    }

    private void ApplyFilters()
    {
        // ⚡ Tính toán filter ngoài UI thread, chỉ swap collection trên UI
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
        
        // ⚡ Materialize danh sách trước rồi mới swap trên UI
        var result = filtered.ToList();
        
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // ⚡ Thay vì Clear + Add từng cái (N lần UI vẽ lại),
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
        
        System.Diagnostics.Debug.WriteLine($"ApplyFilters - Filter: {SelectedFilter}, Total: {TonKhoItems.Count}, Filtered: {result.Count}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving edit: {ex.Message}");
        }
    }

    private string GetMucDoTonKho(decimal soLuong)
    {
        if (soLuong <= 0) return "Hết hàng";
        return soLuong < 20 ? "Thấp" : (soLuong < 50 ? "Trung bình" : "Cao");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            
            // ⚡ Tải dữ liệu ngoài UI thread
            var loaiNLs = await _databaseService.GetLoaiNguyenLieusAsync();
            var donVis = await _databaseService.GetDonViTinhsAsync();

            // ⚡ Cập nhật UI bằng BeginInvoke (không block) thay vì Invoke (block)
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadDataAsync: {ex.Message}");
        }
        finally
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task LoadTonKhoAsync()
    {
        var tonKhos = await _databaseService.GetTonKhosAsync();
        
        // ⚡ Chuẩn bị dữ liệu ngoài UI thread
        var items = tonKhos.Select(tk => new TonKhoItemViewModel
        {
            NguyenLieuID = tk.NguyenLieuID ?? 0,
            MaNguyenLieu = tk.NguyenLieu?.MaNguyenLieu ?? "",
            TenNguyenLieu = tk.NguyenLieu?.TenNguyenLieu ?? "",
            HinhAnh = tk.NguyenLieu?.HinhAnh,
            SoLuongTon = tk.SoLuongTon,
            DonViTinh = tk.NguyenLieu?.DonViTinh?.TenDonVi ?? "",
            MucDoTonKho = GetMucDoTonKho(tk.SoLuongTon),
            EditCommand = OpenEditPopupCommand,
            DeleteCommand = DeleteItemCommand
        }).ToList();

        // ⚡ Chỉ cập nhật UI 1 lần với tất cả dữ liệu
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in FilterNguyenLieuAsync: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"=== Loading QuyDoi for NguyenLieuID: {SelectedNguyenLieu.NguyenLieuID} ===");
            var quyDois = await _databaseService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);
            System.Diagnostics.Debug.WriteLine($"=== Found {quyDois.Count} QuyDoi records ===");
            
            if (quyDois.Count > 0)
            {
                foreach (var qd in quyDois)
                {
                    System.Diagnostics.Debug.WriteLine($"QuyDoi: {qd.DonViTinh?.TenDonVi}, HeSo: {qd.HeSo}");
                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = qd.QuyDoiID,
                        DonViID = qd.DonViID,
                        TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                        HeSo = qd.HeSo,
                        LaDonViChuan = qd.LaDonViChuan,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
            }
            else
            {
                // Không tìm thấy đơn vị quy đổi - thêm đơn vị gốc từ nguyên liệu
                System.Diagnostics.Debug.WriteLine($"=== No QuyDoi found, adding base unit: {SelectedNguyenLieu.DonViTinh} ===");
                
                // Tìm DonViID cho đơn vị hiện tại
                var donVi = DonViTinhs.FirstOrDefault(d => d.TenDonVi == SelectedNguyenLieu.DonViTinh);
                if (donVi != null)
                {
                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = 0,
                        DonViID = donVi.DonViID,
                        TenDonVi = donVi.TenDonVi,
                        HeSo = 1.0m,
                        LaDonViChuan = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
                else
                {
                    // Nếu không tìm thấy đơn vị, thêm nguyên trạng
                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = 0,
                        DonViID = null,
                        TenDonVi = SelectedNguyenLieu.DonViTinh,
                        HeSo = 1.0m,
                        LaDonViChuan = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand,
                        SetDonViChuanCommand = SetDonViChuanCommand
                    });
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"=== Total QuyDoiDonVis in collection: {QuyDoiDonVis.Count} ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading QuyDoiDonVi: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task ExecuteSaveAsync()
    {
        if (SelectedNguyenLieu == null) return;

        // Validate: phải có ít nhất 1 đơn vị chuẩn
        if (QuyDoiDonVis.Any() && !QuyDoiDonVis.Any(r => r.LaDonViChuan))
        {
            System.Windows.MessageBox.Show(
                "Phải có ít nhất 1 đơn vị được chọn làm ĐV Chuẩn (đơn vị lưu tồn kho)!",
                "Thiếu đơn vị chuẩn",
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
                if (qd.HeSo <= 0)
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
                    HeSo = qd.HeSo,
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
                // ⚡ BUG FIX: Lấy hệ số cũ từ giao diện (QuyDoiDonVis) thay vì từ DB (oldDonViChuan)
                // Vì người dùng có thể đang sửa cả hệ số cũ trong cùng 1 lần lưu!
                var uiOldDonVi = QuyDoiDonVis.FirstOrDefault(qd => qd.DonViID == oldDonViChuan?.DonViID);
                decimal oldHeSo = uiOldDonVi?.HeSo ?? oldDonViChuan?.HeSo ?? 1m;
                decimal newHeSo = newDonViChuan.HeSo;
                
                if (oldHeSo > 0 && newHeSo > 0)
                {
                    decimal GetWeight(string unit) => unit switch { "g" => 0.001m, "kg" => 1m, "mg" => 0.000001m, _ => 0m };
                    decimal GetVolume(string unit) => unit switch { "ml" => 0.001m, "l" => 1m, "lit" => 1m, _ => 0m };
                    
                    decimal GetReliableFactor(string? unitName, decimal dbFactor)
                    {
                        var lowerUnit = unitName?.ToLower() ?? "";
                        var w = GetWeight(lowerUnit);
                        if (w > 0) return w;
                        var v = GetVolume(lowerUnit);
                        if (v > 0) return v;
                        return dbFactor;
                    }

                    var oldName = oldDonViChuan?.DonViTinh?.TenDonVi;
                    var newName = newDonViChuan.TenDonVi;

                    var reliableOldHeSo = GetReliableFactor(oldName, oldHeSo);
                    var reliableNewHeSo = GetReliableFactor(newName, newHeSo);

                    if (reliableOldHeSo > 0 && reliableNewHeSo > 0)
                    {
                        decimal conversionFactor = reliableOldHeSo / reliableNewHeSo;
                        decimal newSoLuongTon = SelectedNguyenLieu.SoLuongTon * conversionFactor;

                        await _databaseService.UpdateTonKhoAsync(SelectedNguyenLieu.NguyenLieuID, newSoLuongTon);
                        
                        // ⚡ BUG FIX: Cập nhật lại số lượng tồn trên UI để người dùng thấy ngay kết quả
                        SelectedNguyenLieu.SoLuongTon = newSoLuongTon;
                        SelectedNguyenLieu.MucDoTonKho = GetMucDoTonKho(newSoLuongTon);
                    }
                }
                
                // Cập nhật DonViID trong bảng NguyenLieu
                var nguyenLieu = await _databaseService.GetNguyenLieuByIdAsync(SelectedNguyenLieu.NguyenLieuID);
                if (nguyenLieu != null)
                {
                    nguyenLieu.DonViID = newDonViChuan.DonViID;
                    await _databaseService.SaveNguyenLieuAsync(nguyenLieu);
                    SelectedNguyenLieu.DonViTinh = newDonViChuan.TenDonVi;
                }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
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

        // Validate: đơn vị gốc và đơn vị quy đổi không được giống nhau
        var donViGoc = DonViTinhs.FirstOrDefault(d => d.TenDonVi == SelectedNguyenLieu.DonViTinh);
        if (donViGoc != null && donViGoc.DonViID == SelectedDonViXuat.DonViID)
        {
            System.Windows.MessageBox.Show(
                "Đơn vị quy đổi không được trùng với đơn tính gốc của nguyên liệu!",
                "Lỗi đơn vị trùng",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Validate: kiểm tra trùng lặp trong danh sách hiện tại
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving new QuyDoi: {ex.Message}");
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
            // Mở hộp thoại chỉnh sửa hoặc điều hướng đến trang chỉnh sửa
            // Hiện tại, chỉ hiển thị thông báo debug
            System.Diagnostics.Debug.WriteLine($"Edit item: {item.TenNguyenLieu} (ID: {item.NguyenLieuID})");
            
            // TODO: Triển khai chức năng chỉnh sửa
            // Có thể mở popup để chỉnh sửa chi tiết nguyên liệu
            // Ví dụ:
            // - Mở popup để chỉnh sửa tên nguyên liệu, số lượng, đơn vị, v.v.
            // - Hoặc điều hướng đến trang chỉnh sửa riêng
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting quy doi: {ex.Message}");
            }
        }
    }
}

