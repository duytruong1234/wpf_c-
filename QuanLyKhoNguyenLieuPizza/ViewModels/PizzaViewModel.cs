using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class CongThucItemViewModel : BaseViewModel
{
    public string MaHangHoa { get; set; } = string.Empty;
    public string SizeID { get; set; } = string.Empty;
    public int NguyenLieuID { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public double? SoLuong { get; set; }
    public int? DonViID { get; set; }
    public string? TenDonVi { get; set; }

    public string SoLuongFormatted => SoLuong?.ToString("N2") ?? "0";

    public ICommand? DeleteCommand { get; set; }
    public ICommand? EditCommand { get; set; }
}

public class PizzaItemViewModel : BaseViewModel
{
    public int PizzaID { get; set; }
    public string? MaPizza { get; set; }
    public string TenPizza { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public string KichThuoc { get; set; } = "M";
    public string? SizeID { get; set; }
    public decimal GiaBan { get; set; }
    public decimal GiaVon { get; set; }
    public bool TrangThai { get; set; }
    public string? LoaiHangHoaID { get; set; }
    public string? LoaiMonAn { get; set; }
    public int? DonViID { get; set; }
    public string? TenDonVi { get; set; }

    public string TrangThaiText => TrangThai ? "Đang bán" : "Ngừng bán";
    public bool IsTrangThaiActive => TrangThai;
    public string GiaBanFormatted => GiaBan.ToString("N0");
    public string GiaVonFormatted => GiaVon.ToString("N0");

    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    public ICommand? ToggleStatusCommand { get; set; }
    public ICommand? OpenRecipeCommand { get; set; }

    public void NotifyPropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}

public class PizzaViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private ObservableCollection<PizzaItemViewModel> _pizzas = new();
    private ObservableCollection<PizzaItemViewModel> _filteredPizzas = new();

    private string _searchText = string.Empty;
    private bool _filterDangBan;
    private bool _filterNgungBan;
    private string? _selectedKichThuocFilter;
    private int _totalPizza;
    private int _countDangBan;
    private int _countNgungBan;
    private bool _isLoading;

    // Popup thêm/sửa
    private bool _isAddEditPopupOpen;
    private bool _isEditing;
    private PizzaItemViewModel? _selectedPizza;

    // Trường form
    private string _formMaPizza = string.Empty;
    private string _formTenPizza = string.Empty;
    private string _formHinhAnh = string.Empty;
    private string _formKichThuoc = "M";
    private string _formGiaBan = string.Empty;
    private bool _formTrangThai = true;
    private LoaiHangHoa? _formLoaiMonAn;
    private ObservableCollection<LoaiHangHoa> _loaiHangHoas = new();
    private DonViTinh? _formDonViTinh;
    private ObservableCollection<DonViTinh> _donViTinhs = new();

    // ====== Popup công thức ======
    private bool _isRecipePopupOpen;
    private PizzaItemViewModel? _recipePizza;
    private ObservableCollection<CongThucItemViewModel> _recipeItems = new();
    private ObservableCollection<NguyenLieu> _allNguyenLieus = new();

    // Form thêm nguyên liệu vào công thức
    private NguyenLieu? _recipeSelectedNguyenLieu;
    private string _recipeSoLuong = string.Empty;
    private DonViTinh? _recipeSelectedDonVi;
    private ObservableCollection<DonViTinh> _recipeDonViOptions = new();
    private bool _isRecipeEditing;
    private CongThucItemViewModel? _editingRecipeItem;

    // Hộp thoại trạng thái
    private bool _isStatusDialogOpen;
    private PizzaItemViewModel? _statusPizza;

    #region Thuộc tính
    public ObservableCollection<PizzaItemViewModel> FilteredPizzas
    {
        get => _filteredPizzas;
        set => SetProperty(ref _filteredPizzas, value);
    }

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

    public bool FilterDangBan
    {
        get => _filterDangBan;
        set
        {
            if (SetProperty(ref _filterDangBan, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool FilterNgungBan
    {
        get => _filterNgungBan;
        set
        {
            if (SetProperty(ref _filterNgungBan, value))
            {
                ApplyFilters();
            }
        }
    }

    public string? SelectedKichThuocFilter
    {
        get => _selectedKichThuocFilter;
        set
        {
            if (SetProperty(ref _selectedKichThuocFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public int TotalPizza
    {
        get => _totalPizza;
        set => SetProperty(ref _totalPizza, value);
    }

    public int CountDangBan
    {
        get => _countDangBan;
        set => SetProperty(ref _countDangBan, value);
    }

    public int CountNgungBan
    {
        get => _countNgungBan;
        set => SetProperty(ref _countNgungBan, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsAddEditPopupOpen
    {
        get => _isAddEditPopupOpen;
        set => SetProperty(ref _isAddEditPopupOpen, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public PizzaItemViewModel? SelectedPizza
    {
        get => _selectedPizza;
        set => SetProperty(ref _selectedPizza, value);
    }

    // Thuộc tính form
    public string FormMaPizza
    {
        get => _formMaPizza;
        set => SetProperty(ref _formMaPizza, value);
    }

    public string FormTenPizza
    {
        get => _formTenPizza;
        set => SetProperty(ref _formTenPizza, value);
    }

    public string FormHinhAnh
    {
        get => _formHinhAnh;
        set => SetProperty(ref _formHinhAnh, value);
    }

    public string FormKichThuoc
    {
        get => _formKichThuoc;
        set => SetProperty(ref _formKichThuoc, value);
    }

    public string FormGiaBan
    {
        get => _formGiaBan;
        set => SetProperty(ref _formGiaBan, value);
    }

    public bool FormTrangThai
    {
        get => _formTrangThai;
        set => SetProperty(ref _formTrangThai, value);
    }

    public LoaiHangHoa? FormLoaiMonAn
    {
        get => _formLoaiMonAn;
        set => SetProperty(ref _formLoaiMonAn, value);
    }

    public ObservableCollection<LoaiHangHoa> LoaiHangHoas
    {
        get => _loaiHangHoas;
        set => SetProperty(ref _loaiHangHoas, value);
    }

    public DonViTinh? FormDonViTinh
    {
        get => _formDonViTinh;
        set => SetProperty(ref _formDonViTinh, value);
    }

    public ObservableCollection<DonViTinh> DonViTinhs
    {
        get => _donViTinhs;
        set => SetProperty(ref _donViTinhs, value);
    }

    public ObservableCollection<string> KichThuocOptions { get; } = new();

    // ====== Thuộc tính popup công thức ======
    public bool IsRecipePopupOpen
    {
        get => _isRecipePopupOpen;
        set => SetProperty(ref _isRecipePopupOpen, value);
    }

    public PizzaItemViewModel? RecipePizza
    {
        get => _recipePizza;
        set => SetProperty(ref _recipePizza, value);
    }

    public ObservableCollection<CongThucItemViewModel> RecipeItems
    {
        get => _recipeItems;
        set => SetProperty(ref _recipeItems, value);
    }

    public ObservableCollection<NguyenLieu> AllNguyenLieus
    {
        get => _allNguyenLieus;
        set => SetProperty(ref _allNguyenLieus, value);
    }

    public NguyenLieu? RecipeSelectedNguyenLieu
    {
        get => _recipeSelectedNguyenLieu;
        set
        {
            if (SetProperty(ref _recipeSelectedNguyenLieu, value))
            {
                _ = LoadRecipeDonViOptionsAsync(value);
            }
        }
    }

    public string RecipeSoLuong
    {
        get => _recipeSoLuong;
        set => SetProperty(ref _recipeSoLuong, value);
    }

    public DonViTinh? RecipeSelectedDonVi
    {
        get => _recipeSelectedDonVi;
        set => SetProperty(ref _recipeSelectedDonVi, value);
    }

    public ObservableCollection<DonViTinh> RecipeDonViOptions
    {
        get => _recipeDonViOptions;
        set => SetProperty(ref _recipeDonViOptions, value);
    }

    public bool IsRecipeEditing
    {
        get => _isRecipeEditing;
        set => SetProperty(ref _isRecipeEditing, value);
    }

    // ====== Thuộc tính hộp thoại trạng thái ======
    public bool IsStatusDialogOpen
    {
        get => _isStatusDialogOpen;
        set => SetProperty(ref _isStatusDialogOpen, value);
    }

    public PizzaItemViewModel? StatusPizza
    {
        get => _statusPizza;
        set => SetProperty(ref _statusPizza, value);
    }
    #endregion

    #region Lệnh
    public ICommand LoadDataCommand { get; private set; } = null!;
    public ICommand OpenAddPopupCommand { get; private set; } = null!;
    public ICommand ClosePopupCommand { get; private set; } = null!;
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    public ICommand BrowseImageCommand { get; private set; } = null!;

    // Lệnh công thức
    public ICommand CloseRecipePopupCommand { get; private set; } = null!;
    public ICommand SaveRecipeItemCommand { get; private set; } = null!;
    public ICommand CancelRecipeEditCommand { get; private set; } = null!;

    // Lệnh hộp thoại trạng thái
    public ICommand CloseStatusDialogCommand { get; private set; } = null!;
    public ICommand ConfirmToggleStatusCommand { get; private set; } = null!;
    #endregion

    public PizzaViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        InitializeCommands();
        SafeInitializeAsync(LoadDataAsync);
    }

    public PizzaViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeCommands();
        // Skip SafeInitializeAsync for tests unless specifically needed, or let test trigger it manually
        // SafeInitializeAsync(LoadDataAsync);
    }

    private void InitializeCommands()
    {
        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        OpenAddPopupCommand = new RelayCommand(_ => OpenAddPopup());
        ClosePopupCommand = new RelayCommand(_ => ClosePopup());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        BrowseImageCommand = new RelayCommand(_ => BrowseImage());
        CloseRecipePopupCommand = new RelayCommand(_ => CloseRecipePopup());
        CloseStatusDialogCommand = new RelayCommand(_ => IsStatusDialogOpen = false);
        ConfirmToggleStatusCommand = new AsyncRelayCommand(async _ => await ConfirmTogglePizzaStatusAsync());
        SaveRecipeItemCommand = new AsyncRelayCommand(async _ => await SaveRecipeItemAsync());
        CancelRecipeEditCommand = new RelayCommand(_ => CancelRecipeEdit());
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
            // Load loại hàng hoá
            var loaiList = await _databaseService.GetLoaiHangHoasAsync();
            LoaiHangHoas.Clear();
            foreach (var l in loaiList)
                LoaiHangHoas.Add(l);

            // Load đơn vị tính
            var dvtList = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs.Clear();
            foreach (var d in dvtList)
                DonViTinhs.Add(d);

            var pizzaList = await _databaseService.GetPizzasAsync();
            _pizzas.Clear();

            foreach (var p in pizzaList)
            {
                var giaVon = !string.IsNullOrEmpty(p.MaPizza) && !string.IsNullOrEmpty(p.SizeID)
                    ? await _databaseService.CalculateGiaVonByMaAsync(p.MaPizza, p.SizeID)
                    : 0;
                var item = new PizzaItemViewModel
                {
                    PizzaID = p.PizzaID,
                    MaPizza = p.MaPizza,
                    TenPizza = p.TenPizza,
                    HinhAnh = p.HinhAnh,
                    KichThuoc = p.KichThuoc,
                    SizeID = p.SizeID,
                    GiaBan = p.GiaBan,
                    GiaVon = giaVon,
                    TrangThai = p.TrangThai,
                    LoaiHangHoaID = p.LoaiHangHoaID,
                    LoaiMonAn = p.LoaiMonAn,
                    DonViID = p.DonViID,
                    TenDonVi = p.TenDonVi
                };
                item.EditCommand = new RelayCommand(_ => EditPizza(item));
                item.DeleteCommand = new AsyncRelayCommand(async _ => await DeletePizzaAsync(item));
                item.ToggleStatusCommand = new AsyncRelayCommand(async _ => await TogglePizzaStatusAsync(item));
                item.OpenRecipeCommand = new AsyncRelayCommand(async _ => await OpenRecipePopupAsync(item));
                _pizzas.Add(item);
            }

            // Cập nhật danh sách kích thước từ bảng DoanhMuc_Size trong database
            var allSizes = await _databaseService.GetDoanhMucSizesAsync();
            KichThuocOptions.Clear();
            foreach (var s in allSizes.OrderBy(s => s.SizeID))
                KichThuocOptions.Add(s.TenSize ?? s.SizeID);

            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading pizza data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _pizzas.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(p =>
                p.TenPizza.ToLower().Contains(searchLower) ||
                (p.MaPizza?.ToLower().Contains(searchLower) ?? false));
        }

        if (FilterDangBan && !FilterNgungBan)
        {
            filtered = filtered.Where(p => p.TrangThai);
        }
        else if (FilterNgungBan && !FilterDangBan)
        {
            filtered = filtered.Where(p => !p.TrangThai);
        }

        if (!string.IsNullOrEmpty(SelectedKichThuocFilter))
        {
            filtered = filtered.Where(p => p.KichThuoc == SelectedKichThuocFilter);
        }

        FilteredPizzas = new ObservableCollection<PizzaItemViewModel>(filtered);
        TotalPizza = _pizzas.Count;
        CountDangBan = _pizzas.Count(p => p.TrangThai);
        CountNgungBan = _pizzas.Count(p => !p.TrangThai);
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterDangBan = false;
        FilterNgungBan = false;
        SelectedKichThuocFilter = null;
    }

    private void OpenAddPopup()
    {
        IsEditing = false;
        SelectedPizza = null;
        ClearForm();
        IsAddEditPopupOpen = true;
    }

    private void EditPizza(PizzaItemViewModel item)
    {
        IsEditing = true;
        SelectedPizza = item;

        FormMaPizza = item.MaPizza ?? string.Empty;
        FormTenPizza = item.TenPizza;
        FormHinhAnh = item.HinhAnh ?? string.Empty;
        FormKichThuoc = item.KichThuoc;
        FormGiaBan = item.GiaBan.ToString("N0");
        FormTrangThai = item.TrangThai;
        FormLoaiMonAn = LoaiHangHoas.FirstOrDefault(l => l.LoaiHangHoaID == item.LoaiHangHoaID);
        FormDonViTinh = DonViTinhs.FirstOrDefault(d => d.DonViID == item.DonViID);

        IsAddEditPopupOpen = true;
    }

    private void ClosePopup()
    {
        IsAddEditPopupOpen = false;
        ClearForm();
    }

    private void ClearForm()
    {
        FormMaPizza = string.Empty;
        FormTenPizza = string.Empty;
        FormHinhAnh = string.Empty;
        FormKichThuoc = "M";
        FormGiaBan = string.Empty;
        FormTrangThai = true;
        FormLoaiMonAn = null;
        FormDonViTinh = null;
    }

    private async Task SaveAsync()
    {
        // Validate tất cả các trường bắt buộc
        var errors = new System.Collections.Generic.List<string>();

        if (string.IsNullOrWhiteSpace(FormMaPizza))
            errors.Add("Mã món ăn");
        if (string.IsNullOrWhiteSpace(FormTenPizza))
            errors.Add("Tên món ăn");
        if (FormLoaiMonAn == null)
            errors.Add("Loại món ăn");
        if (FormDonViTinh == null)
            errors.Add("Đơn vị tính");
        if (string.IsNullOrWhiteSpace(FormKichThuoc))
            errors.Add("Kích thước");

        decimal giaBan = 0;
        if (string.IsNullOrWhiteSpace(FormGiaBan))
        {
            errors.Add("Giá bán");
        }
        else if (!decimal.TryParse(FormGiaBan.Replace(",", "").Replace(".", ""), out giaBan) || giaBan <= 0)
        {
            errors.Add("Giá bán (phải là số lớn hơn 0)");
        }

        if (string.IsNullOrWhiteSpace(FormHinhAnh))
            errors.Add("Hình ảnh");

        if (errors.Count > 0)
        {
            System.Windows.MessageBox.Show(
                $"Vui lòng điền đầy đủ các trường bắt buộc:\n• {string.Join("\n• ", errors)}",
                "Thiếu thông tin",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var pizza = new Pizza
        {
            PizzaID = IsEditing ? SelectedPizza?.PizzaID ?? 0 : 0,
            MaPizza = FormMaPizza,
            TenPizza = FormTenPizza,
            HinhAnh = FormHinhAnh,
            KichThuoc = FormKichThuoc,
            SizeID = IsEditing ? SelectedPizza?.SizeID : null,
            GiaBan = giaBan,
            TrangThai = FormTrangThai,
            LoaiHangHoaID = FormLoaiMonAn?.LoaiHangHoaID,
            DonViID = FormDonViTinh?.DonViID
        };

        var success = await _databaseService.SavePizzaAsync(pizza);

        if (success)
        {
            ClosePopup();
            await LoadDataAsync();
        }
    }

    private async Task DeletePizzaAsync(PizzaItemViewModel item)
    {
        if (string.IsNullOrEmpty(item.MaPizza))
            return;

        var confirmed = await ShowDeleteConfirmation(
            item.TenPizza ?? item.MaPizza,
            "Xóa pizza",
            $"Bạn có chắc chắn muốn xóa pizza \"{item.TenPizza}\"?\nTất cả công thức liên quan sẽ bị xóa theo.");
        if (!confirmed) return;

        var success = await _databaseService.DeletePizzaByMaAsync(item.MaPizza);

        if (success)
        {
            await LoadDataAsync();
        }
    }

    private async Task TogglePizzaStatusAsync(PizzaItemViewModel item)
    {
        if (string.IsNullOrEmpty(item.MaPizza))
            return;
            
        // Mở dialog xác nhận thay vì toggle trực tiếp
        StatusPizza = item;
        IsStatusDialogOpen = true;
    }

    private async Task ConfirmTogglePizzaStatusAsync()
    {
        if (StatusPizza == null || string.IsNullOrEmpty(StatusPizza.MaPizza)) return;

        try
        {
            var newStatus = !StatusPizza.TrangThai;
            var success = await _databaseService.TogglePizzaTrangThaiAsync(StatusPizza.MaPizza, newStatus);
            
            if (success)
            {
                IsStatusDialogOpen = false;
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling pizza status: {ex.Message}");
        }
    }

    private void BrowseImage()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn hình ảnh món ăn",
                Filter = "Tệp hình ảnh (*.jpg, *.jpeg, *.png, *.gif, *.bmp, *.webp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Tất cả tệp (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Sử dụng helper để lưu ảnh vào AppData
                FormHinhAnh = Helpers.ImageStorageHelper.CopyImageToStorage(openFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error browsing image: {ex.Message}");
        }
    }

    #region Quản lý Công thức

    private async Task OpenRecipePopupAsync(PizzaItemViewModel item)
    {
        RecipePizza = item;
        IsRecipeEditing = false;
        _editingRecipeItem = null;
        ClearRecipeForm();

        // Tải tất cả nguyên liệu cho combobox
        var nlList = await _databaseService.GetNguyenLieusAsync();
        AllNguyenLieus.Clear();
        foreach (var nl in nlList)
            AllNguyenLieus.Add(nl);

        // Tải danh sách công thức hiện có
        await LoadRecipeItemsAsync();

        IsRecipePopupOpen = true;
    }

    private async Task LoadRecipeItemsAsync()
    {
        if (RecipePizza == null || string.IsNullOrEmpty(RecipePizza.MaPizza) || string.IsNullOrEmpty(RecipePizza.SizeID))
            return;

        var items = await _databaseService.GetCongThucPizzaAsync(RecipePizza.MaPizza, RecipePizza.SizeID);
        RecipeItems.Clear();
        foreach (var ct in items)
        {
            var vm = new CongThucItemViewModel
            {
                MaHangHoa = ct.MaHangHoa,
                SizeID = ct.SizeID,
                NguyenLieuID = ct.NguyenLieuID,
                TenNguyenLieu = ct.NguyenLieu?.TenNguyenLieu ?? $"NL#{ct.NguyenLieuID}",
                SoLuong = ct.SoLuong,
                DonViID = ct.DonViID,
                TenDonVi = ct.DonViTinh?.TenDonVi ?? ""
            };
            vm.DeleteCommand = new AsyncRelayCommand(async _ => await DeleteRecipeItemAsync(vm));
            vm.EditCommand = new RelayCommand(_ => EditRecipeItem(vm));
            RecipeItems.Add(vm);
        }
    }

    private void CloseRecipePopup()
    {
        IsRecipePopupOpen = false;
        RecipePizza = null;
        RecipeItems.Clear();
        ClearRecipeForm();
    }

    private void ClearRecipeForm()
    {
        RecipeSelectedNguyenLieu = null;
        RecipeSoLuong = string.Empty;
        RecipeSelectedDonVi = null;
        IsRecipeEditing = false;
        _editingRecipeItem = null;
    }

    private async void EditRecipeItem(CongThucItemViewModel item)
    {
        IsRecipeEditing = true;
        _editingRecipeItem = item;
        // Set ingredient first (this triggers loading QuyDoiDonVi options)
        _recipeSelectedNguyenLieu = AllNguyenLieus.FirstOrDefault(n => n.NguyenLieuID == item.NguyenLieuID);
        OnPropertyChanged(nameof(RecipeSelectedNguyenLieu));
        await LoadRecipeDonViOptionsAsync(_recipeSelectedNguyenLieu);
        RecipeSoLuong = item.SoLuong?.ToString("G") ?? "";
        RecipeSelectedDonVi = RecipeDonViOptions.FirstOrDefault(d => d.DonViID == item.DonViID)
                              ?? RecipeDonViOptions.FirstOrDefault();
    }

    private void CancelRecipeEdit()
    {
        ClearRecipeForm();
    }

    /// <summary>
    /// Load danh sách đơn vị từ bảng QuyDoiDonVi cho nguyên liệu được chọn.
    /// Chỉ hiển thị các đơn vị đã cấu hình hệ số quy đổi.
    /// </summary>
    private async Task LoadRecipeDonViOptionsAsync(NguyenLieu? nguyenLieu)
    {
        RecipeDonViOptions.Clear();
        RecipeSelectedDonVi = null;

        if (nguyenLieu == null) return;

        try
        {
            var quyDois = await _databaseService.GetQuyDoiDonVisAsync(nguyenLieu.NguyenLieuID);
            
            if (quyDois.Count > 0)
            {
                foreach (var qd in quyDois)
                {
                    if (qd.DonViTinh != null)
                    {
                        RecipeDonViOptions.Add(qd.DonViTinh);
                    }
                }
                // Tự động chọn đơn vị chuẩn
                var donViChuan = quyDois.FirstOrDefault(q => q.LaDonViChuan);
                if (donViChuan?.DonViTinh != null)
                {
                    RecipeSelectedDonVi = RecipeDonViOptions.FirstOrDefault(d => d.DonViID == donViChuan.DonViID);
                }
            }
            else
            {
                // Fallback: nếu chưa cấu hình QuyDoiDonVi → dùng đơn vị gốc của nguyên liệu
                if (nguyenLieu.DonViTinh != null)
                {
                    RecipeDonViOptions.Add(nguyenLieu.DonViTinh);
                    RecipeSelectedDonVi = nguyenLieu.DonViTinh;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recipe DonVi options: {ex.Message}");
            // Fallback
            if (nguyenLieu.DonViTinh != null)
            {
                RecipeDonViOptions.Add(nguyenLieu.DonViTinh);
                RecipeSelectedDonVi = nguyenLieu.DonViTinh;
            }
        }
    }

    private async Task SaveRecipeItemAsync()
    {
        System.Diagnostics.Debug.WriteLine("=== SaveRecipeItemAsync START ===");
        
        if (RecipePizza == null || string.IsNullOrEmpty(RecipePizza.MaPizza) || string.IsNullOrEmpty(RecipePizza.SizeID))
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: RecipePizza null or missing data. MaPizza={RecipePizza?.MaPizza}, SizeID={RecipePizza?.SizeID}");
            System.Windows.MessageBox.Show("Món ăn này chưa có kích thước hợp lệ, vui lòng cập nhật kích thước trước.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (RecipeSelectedNguyenLieu == null)
        {
            System.Diagnostics.Debug.WriteLine("ERROR: RecipeSelectedNguyenLieu is null");
            System.Windows.MessageBox.Show("Vui lòng chọn nguyên liệu cần thêm.", "Thiếu thông tin", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        System.Diagnostics.Debug.WriteLine($"RecipeSoLuong raw = '{RecipeSoLuong}'");
        double soLuong = 0;
        if (string.IsNullOrWhiteSpace(RecipeSoLuong) || !double.TryParse(RecipeSoLuong.Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out soLuong) || soLuong <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Invalid SoLuong. Raw='{RecipeSoLuong}', Parsed={soLuong}");
            System.Windows.MessageBox.Show("Vui lòng nhập định lượng hợp lệ (lớn hơn 0).", "Thiếu thông tin", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Parsed soLuong = {soLuong}");
        System.Diagnostics.Debug.WriteLine($"RecipeSelectedDonVi = {RecipeSelectedDonVi?.TenDonVi} (ID={RecipeSelectedDonVi?.DonViID})");

        var congThuc = new CongThuc_Pizza
        {
            MaHangHoa = RecipePizza.MaPizza,
            SizeID = RecipePizza.SizeID,
            NguyenLieuID = RecipeSelectedNguyenLieu.NguyenLieuID,
            SoLuong = soLuong,
            DonViID = RecipeSelectedDonVi?.DonViID
        };

        System.Diagnostics.Debug.WriteLine($"Saving: MaHangHoa={congThuc.MaHangHoa}, SizeID={congThuc.SizeID}, NLID={congThuc.NguyenLieuID}, SL={congThuc.SoLuong}, DVID={congThuc.DonViID}");

        try
        {
            var success = await _databaseService.SaveCongThucPizzaAsync(congThuc);
            System.Diagnostics.Debug.WriteLine($"SaveCongThucPizzaAsync result: {success}");
            
            if (success)
            {
                ClearRecipeForm();
                await LoadRecipeItemsAsync();

                // Tính lại giá vốn cho pizza này
                var giaVon = await _databaseService.CalculateGiaVonByMaAsync(RecipePizza.MaPizza, RecipePizza.SizeID);
                RecipePizza.GiaVon = giaVon;
                RecipePizza.NotifyPropertyChanged(nameof(RecipePizza.GiaVonFormatted));
            }
            else
            {
                System.Windows.MessageBox.Show("Không thể lưu công thức. Vui lòng thử lại.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EXCEPTION in SaveRecipeItemAsync: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        
        System.Diagnostics.Debug.WriteLine("=== SaveRecipeItemAsync END ===");
    }

    private async Task DeleteRecipeItemAsync(CongThucItemViewModel item)
    {
        if (RecipePizza == null)
            return;

        var confirmed = await ShowDeleteConfirmation(
            $"{item.TenNguyenLieu} ({item.SizeID})",
            "Xóa nguyên liệu khỏi công thức",
            $"Bạn có chắc chắn muốn xóa \"{item.TenNguyenLieu}\" khỏi công thức?\nHành động này không thể hoàn tác.");
        if (!confirmed) return;

        var success = await _databaseService.DeleteCongThucPizzaAsync(item.MaHangHoa, item.SizeID, item.NguyenLieuID);
        if (success)
        {
            await LoadRecipeItemsAsync();

            // Tính lại giá vốn
            if (!string.IsNullOrEmpty(RecipePizza.MaPizza) && !string.IsNullOrEmpty(RecipePizza.SizeID))
            {
                var giaVon = await _databaseService.CalculateGiaVonByMaAsync(RecipePizza.MaPizza, RecipePizza.SizeID);
                RecipePizza.GiaVon = giaVon;
                RecipePizza.NotifyPropertyChanged(nameof(RecipePizza.GiaVonFormatted));
            }
        }
    }

    #endregion
}

