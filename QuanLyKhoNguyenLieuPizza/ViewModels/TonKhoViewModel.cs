using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class TonKhoItemViewModel : BaseViewModel
{
    public int NguyenLieuID { get; set; }
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
        set => SetProperty(ref _heSo, value);
    }
    
    public bool LaDonViChuan
    {
        get => _laDonViChuan;
        set => SetProperty(ref _laDonViChuan, value);
    }
    
    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class TonKhoViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    
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
    
    // For Add QuyDoi popup
    private string _donViNhap = string.Empty;
    private DonViTinh? _selectedDonViXuat;
    private string _heSoNhap = string.Empty;
    
    // For Edit popup
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
                
                // Set don vi nhap
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

    // Commands
    public ICommand OpenQuyDoiPopupCommand { get; private set; }
    public ICommand CloseQuyDoiPopupCommand { get; private set; }
    public ICommand SelectNguyenLieuCommand { get; private set; }
    public ICommand EditCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand AddQuyDoiCommand { get; private set; }
    public ICommand OpenAddQuyDoiPopupCommand { get; private set; }
    public ICommand CloseAddQuyDoiPopupCommand { get; private set; }
    public ICommand SaveNewQuyDoiCommand { get; private set; }
    public ICommand BackCommand { get; private set; }
    public ICommand RefreshCommand { get; private set; }
    public ICommand EditItemCommand { get; private set; }
    public ICommand DeleteItemCommand { get; private set; }
    public ICommand FilterTatCaCommand { get; private set; }
    public ICommand FilterTonThapCommand { get; private set; }
    public ICommand FilterTonCaoCommand { get; private set; }
    public ICommand OpenEditPopupCommand { get; private set; }
    public ICommand CloseEditPopupCommand { get; private set; }
    public ICommand SaveEditCommand { get; private set; }
    public ICommand EditQuyDoiCommand { get; private set; }
    public ICommand DeleteQuyDoiCommand { get; private set; }

    public event Action? OnBack;

    public TonKhoViewModel()
    {
        try
        {
            _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }
        
        InitializeCommands();
        
        _ = LoadDataAsync();
    }

    public TonKhoViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeCommands();
        
        _ = LoadDataAsync();
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
        SaveCommand = new RelayCommand(async _ => await ExecuteSaveAsync());
        AddQuyDoiCommand = new RelayCommand(_ => 
        {
            IsAddQuyDoiPopupOpen = true;
            HeSoNhap = string.Empty;
            SelectedDonViXuat = null;
        });
        OpenAddQuyDoiPopupCommand = new RelayCommand(_ => IsAddQuyDoiPopupOpen = true);
        CloseAddQuyDoiPopupCommand = new RelayCommand(_ => IsAddQuyDoiPopupOpen = false);
        SaveNewQuyDoiCommand = new RelayCommand(async _ => await ExecuteSaveNewQuyDoiAsync());
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        EditItemCommand = new RelayCommand(ExecuteEditItem);
        DeleteItemCommand = new RelayCommand(async param => await ExecuteDeleteItemAsync(param));
        
        // Filter commands
        FilterTatCaCommand = new RelayCommand(_ => SelectedFilter = "TatCa");
        FilterTonThapCommand = new RelayCommand(_ => SelectedFilter = "TonThap");
        FilterTonCaoCommand = new RelayCommand(_ => SelectedFilter = "TonCao");
        
        // Edit popup commands
        OpenEditPopupCommand = new RelayCommand(ExecuteOpenEditPopup);
        CloseEditPopupCommand = new RelayCommand(_ => IsEditPopupOpen = false);
        SaveEditCommand = new RelayCommand(async _ => await ExecuteSaveEditAsync());
        
        // QuyDoi Edit/Delete commands
        EditQuyDoiCommand = new RelayCommand(ExecuteEditQuyDoi);
        DeleteQuyDoiCommand = new RelayCommand(async param => await ExecuteDeleteQuyDoiAsync(param));
    }

    private void ApplyFilters()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            FilteredTonKhoItems.Clear();
            
            var filtered = TonKhoItems.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(x => x.TenNguyenLieu.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            
            // Apply status filter
            if (SelectedFilter == "TonThap")
            {
                filtered = filtered.Where(x => x.IsLowStock || x.MucDoTonKho.Contains("Thấp", StringComparison.OrdinalIgnoreCase));
            }
            else if (SelectedFilter == "TonCao")
            {
                filtered = filtered.Where(x => !x.IsLowStock && x.MucDoTonKho.Contains("Cao", StringComparison.OrdinalIgnoreCase));
            }
            // If SelectedFilter is "TatCa" or anything else, show all (no additional filtering)
            
            foreach (var item in filtered)
            {
                FilteredTonKhoItems.Add(item);
            }
            
            System.Diagnostics.Debug.WriteLine($"ApplyFilters - Filter: {SelectedFilter}, Total: {TonKhoItems.Count}, Filtered: {FilteredTonKhoItems.Count}");
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

        try
        {
            // Update TonKho
            var success = await _databaseService.UpdateTonKhoAsync(SelectedNguyenLieu.NguyenLieuID, EditSoLuongTon);
            
            if (success)
            {
                // Update UI
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
        return soLuong < 20 ? "Thấp" : (soLuong < 50 ? "Trung bình" : "Cao");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            
            // Load Loai Nguyen Lieu
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LoaiNguyenLieus.Clear();
                LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 0, TenLoai = "Tất cả" });
            });
            
            var loaiNLs = await _databaseService.GetLoaiNguyenLieusAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var loai in loaiNLs)
                {
                    LoaiNguyenLieus.Add(loai);
                }
                
                // Set default selection to "Tất cả"
                if (LoaiNguyenLieus.Count > 0)
                {
                    SelectedLoaiNguyenLieu = LoaiNguyenLieus[0];
                }
            });

            // Load Don Vi Tinh
            var donVis = await _databaseService.GetDonViTinhsAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                DonViTinhs.Clear();
                foreach (var dv in donVis)
                {
                    DonViTinhs.Add(dv);
                }
            });

            // Load Ton Kho data
            await LoadTonKhoAsync();
            
            // Load Nguyen Lieu for popup
            await FilterNguyenLieuAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadDataAsync: {ex.Message}");
            
            // Load sample data if database fails
            System.Windows.Application.Current.Dispatcher.Invoke(LoadSampleData);
        }
        finally
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsLoading = false);
        }
    }

    private async Task LoadTonKhoAsync()
    {
        var tonKhos = await _databaseService.GetTonKhosAsync();
        
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TonKhoItems.Clear();
            
            if (tonKhos.Count == 0)
            {
                // Sample data if no data from DB
                LoadSampleData();
                return;
            }
            
            foreach (var tk in tonKhos)
            {
                string mucDo = GetMucDoTonKho(tk.SoLuongTon);
                
                TonKhoItems.Add(new TonKhoItemViewModel
                {
                    NguyenLieuID = tk.NguyenLieuID ?? 0,
                    TenNguyenLieu = tk.NguyenLieu?.TenNguyenLieu ?? "",
                    HinhAnh = tk.NguyenLieu?.HinhAnh,
                    SoLuongTon = tk.SoLuongTon,
                    DonViTinh = tk.NguyenLieu?.DonViTinh?.TenDonVi ?? "",
                    MucDoTonKho = mucDo,
                    EditCommand = OpenEditPopupCommand,
                    DeleteCommand = DeleteItemCommand
                });
            }
            
            SoNguyenLieuTonKho = TonKhoItems.Count;
            ApplyFilters();
        });
    }

    private void LoadSampleData()
    {
        // Sample Loai Nguyen Lieu
        if (LoaiNguyenLieus.Count <= 1)
        {
            LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 1, TenLoai = "Bột" });
            LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 2, TenLoai = "Phô mai" });
            LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 3, TenLoai = "Thịt" });
            LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 4, TenLoai = "Rau củ" });
            LoaiNguyenLieus.Add(new LoaiNguyenLieu { LoaiNLID = 5, TenLoai = "Sốt" });
        }

        // Sample Don Vi Tinh
        if (DonViTinhs.Count == 0)
        {
            DonViTinhs.Add(new DonViTinh { DonViID = 1, TenDonVi = "Kg" });
            DonViTinhs.Add(new DonViTinh { DonViID = 2, TenDonVi = "Gram" });
            DonViTinhs.Add(new DonViTinh { DonViID = 3, TenDonVi = "Bao" });
            DonViTinhs.Add(new DonViTinh { DonViID = 4, TenDonVi = "Thùng" });
            DonViTinhs.Add(new DonViTinh { DonViID = 5, TenDonVi = "Chai" });
            DonViTinhs.Add(new DonViTinh { DonViID = 6, TenDonVi = "Hộp" });
        }

        // Sample Ton Kho
        TonKhoItems.Clear();
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 1, TenNguyenLieu = "Bột mì", SoLuongTon = 15, DonViTinh = "Kg", MucDoTonKho = "Thấp", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 2, TenNguyenLieu = "Bột ngô", SoLuongTon = 25, DonViTinh = "Kg", MucDoTonKho = "Trung bình", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 3, TenNguyenLieu = "Phô mai Mozzarella", SoLuongTon = 80, DonViTinh = "Kg", MucDoTonKho = "Cao", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 4, TenNguyenLieu = "Phô mai Cheddar", SoLuongTon = 45, DonViTinh = "Kg", MucDoTonKho = "Trung bình", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 5, TenNguyenLieu = "Xúc xích Đức", SoLuongTon = 10, DonViTinh = "Kg", MucDoTonKho = "Thấp", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 6, TenNguyenLieu = "Thịt xông khói", SoLuongTon = 30, DonViTinh = "Kg", MucDoTonKho = "Trung bình", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 7, TenNguyenLieu = "Ớt chuông", SoLuongTon = 18, DonViTinh = "Kg", MucDoTonKho = "Thấp", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 8, TenNguyenLieu = "Nấm", SoLuongTon = 22, DonViTinh = "Kg", MucDoTonKho = "Trung bình", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 9, TenNguyenLieu = "Sốt cà chua", SoLuongTon = 60, DonViTinh = "Chai", MucDoTonKho = "Cao", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        TonKhoItems.Add(new TonKhoItemViewModel { NguyenLieuID = 10, TenNguyenLieu = "Dầu olive", SoLuongTon = 12, DonViTinh = "Chai", MucDoTonKho = "Thấp", EditCommand = OpenEditPopupCommand, DeleteCommand = DeleteItemCommand });
        
        SoNguyenLieuTonKho = TonKhoItems.Count;
        
        FilteredNguyenLieus.Clear();
        foreach (var item in TonKhoItems)
        {
            FilteredNguyenLieus.Add(item);
        }
        
        ApplyFilters();
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
                
                if (nguyenLieus.Count == 0)
                {
                    // Use sample data
                    foreach (var item in TonKhoItems)
                    {
                        FilteredNguyenLieus.Add(item);
                    }
                    return;
                }
                
                foreach (var nl in nguyenLieus)
                {
                    var tonKhoItem = TonKhoItems.FirstOrDefault(t => t.NguyenLieuID == nl.NguyenLieuID);
                    
                    FilteredNguyenLieus.Add(new TonKhoItemViewModel
                    {
                        NguyenLieuID = nl.NguyenLieuID,
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
        catch
        {
            // Use TonKhoItems as fallback
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredNguyenLieus.Clear();
                foreach (var item in TonKhoItems)
                {
                    FilteredNguyenLieus.Add(item);
                }
            });
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
                        DeleteCommand = DeleteQuyDoiCommand
                    });
                }
            }
            else
            {
                // No conversion units found - add base unit from the ingredient
                System.Diagnostics.Debug.WriteLine($"=== No QuyDoi found, adding base unit: {SelectedNguyenLieu.DonViTinh} ===");
                
                // Find the DonViID for the current unit
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
                        DeleteCommand = DeleteQuyDoiCommand
                    });
                }
                else
                {
                    // If we can't find the unit, add it as is
                    QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                    {
                        QuyDoiID = 0,
                        DonViID = null,
                        TenDonVi = SelectedNguyenLieu.DonViTinh,
                        HeSo = 1.0m,
                        LaDonViChuan = true,
                        EditCommand = EditQuyDoiCommand,
                        DeleteCommand = DeleteQuyDoiCommand
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

        try
        {
            foreach (var qd in QuyDoiDonVis)
            {
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
            
            IsEditing = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
        }
    }

    private async Task ExecuteSaveNewQuyDoiAsync()
    {
        if (SelectedNguyenLieu == null || SelectedDonViXuat == null) return;
        
        if (!decimal.TryParse(HeSoNhap, out decimal heSo) || heSo <= 0)
        {
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
                // Add to list
                QuyDoiDonVis.Add(new QuyDoiDonViItemViewModel
                {
                    QuyDoiID = 0,
                    DonViID = SelectedDonViXuat.DonViID,
                    TenDonVi = SelectedDonViXuat.TenDonVi,
                    HeSo = heSo,
                    LaDonViChuan = false,
                    EditCommand = EditQuyDoiCommand,
                    DeleteCommand = DeleteQuyDoiCommand
                });
                
                IsAddQuyDoiPopupOpen = false;
                HeSoNhap = string.Empty;
                SelectedDonViXuat = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving new QuyDoi: {ex.Message}");
        }
    }

    private void ExecuteEditItem(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            // Open edit dialog or navigate to edit page
            // For now, we'll just show a debug message
            System.Diagnostics.Debug.WriteLine($"Edit item: {item.TenNguyenLieu} (ID: {item.NguyenLieuID})");
            
            // TODO: Implement edit functionality
            // This could open a dialog to edit the ingredient details
            // For example:
            // - Open a popup to edit ingredient name, quantity, unit, etc.
            // - Or navigate to a dedicated edit page
        }
    }

    private async Task ExecuteDeleteItemAsync(object? parameter)
    {
        if (parameter is TonKhoItemViewModel item)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Delete item: {item.TenNguyenLieu} (ID: {item.NguyenLieuID})");
                
                // Delete from database
                var success = await _databaseService.DeleteNguyenLieuAsync(item.NguyenLieuID);
                
                if (success)
                {
                    // Remove from UI
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TonKhoItems.Remove(item);
                        FilteredNguyenLieus.Remove(item);
                        SoNguyenLieuTonKho = TonKhoItems.Count;
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Successfully deleted item: {item.TenNguyenLieu}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete item: {item.TenNguyenLieu}");
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
            // Enable editing mode for this specific item
            IsEditing = true;
        }
    }

    private async Task ExecuteDeleteQuyDoiAsync(object? parameter)
    {
        if (parameter is QuyDoiDonViItemViewModel quyDoi && quyDoi.QuyDoiID > 0)
        {
            try
            {
                var success = await _databaseService.DeleteQuyDoiDonViAsync(quyDoi.QuyDoiID);
                
                if (success)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        QuyDoiDonVis.Remove(quyDoi);
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Successfully deleted quy doi: {quyDoi.TenDonVi}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting quy doi: {ex.Message}");
            }
        }
    }
}

