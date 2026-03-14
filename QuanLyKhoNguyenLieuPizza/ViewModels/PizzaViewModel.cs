using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class PizzaItemViewModel : BaseViewModel
{
    public int PizzaID { get; set; }
    public string? MaPizza { get; set; }
    public string TenPizza { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public string KichThuoc { get; set; } = "M";
    public string? SizeID { get; set; }
    public decimal GiaBan { get; set; }
    public decimal GiaVon { get; set; }
    public bool TrangThai { get; set; }

    public string TrangThaiText => TrangThai ? "Đang bán" : "Ngừng bán";
    public bool IsTrangThaiActive => TrangThai;
    public string GiaBanFormatted => GiaBan.ToString("N0");
    public string GiaVonFormatted => GiaVon.ToString("N0");

    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class PizzaViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

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

    // Add/Edit popup
    private bool _isAddEditPopupOpen;
    private bool _isEditing;
    private PizzaItemViewModel? _selectedPizza;

    // Form fields
    private string _formMaPizza = string.Empty;
    private string _formTenPizza = string.Empty;
    private string _formMoTa = string.Empty;
    private string _formHinhAnh = string.Empty;
    private string _formKichThuoc = "M";
    private string _formGiaBan = string.Empty;
    private bool _formTrangThai = true;

    #region Properties
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

    // Form properties
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

    public string FormMoTa
    {
        get => _formMoTa;
        set => SetProperty(ref _formMoTa, value);
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

    public List<string> KichThuocOptions { get; } = ["S", "M", "L"];
    #endregion

    #region Commands
    public ICommand LoadDataCommand { get; }
    public ICommand OpenAddPopupCommand { get; }
    public ICommand ClosePopupCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand BrowseImageCommand { get; }
    #endregion

    public PizzaViewModel()
    {
        _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        OpenAddPopupCommand = new RelayCommand(_ => OpenAddPopup());
        ClosePopupCommand = new RelayCommand(_ => ClosePopup());
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        BrowseImageCommand = new RelayCommand(_ => BrowseImage());

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
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
                    MoTa = p.MoTa,
                    HinhAnh = p.HinhAnh,
                    KichThuoc = p.KichThuoc,
                    SizeID = p.SizeID,
                    GiaBan = p.GiaBan,
                    GiaVon = giaVon,
                    TrangThai = p.TrangThai
                };
                item.EditCommand = new RelayCommand(_ => EditPizza(item));
                item.DeleteCommand = new RelayCommand(async _ => await DeletePizzaAsync(item));
                _pizzas.Add(item);
            }

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
        FormMoTa = item.MoTa ?? string.Empty;
        FormHinhAnh = item.HinhAnh ?? string.Empty;
        FormKichThuoc = item.KichThuoc;
        FormGiaBan = item.GiaBan.ToString("N0");
        FormTrangThai = item.TrangThai;

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
        FormMoTa = string.Empty;
        FormHinhAnh = string.Empty;
        FormKichThuoc = "M";
        FormGiaBan = string.Empty;
        FormTrangThai = true;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTenPizza))
        {
            return;
        }

        decimal giaBan = 0;
        if (!string.IsNullOrWhiteSpace(FormGiaBan))
        {
            decimal.TryParse(FormGiaBan.Replace(",", "").Replace(".", ""), out giaBan);
        }

        var pizza = new Pizza
        {
            PizzaID = IsEditing ? SelectedPizza?.PizzaID ?? 0 : 0,
            MaPizza = FormMaPizza,
            TenPizza = FormTenPizza,
            MoTa = FormMoTa,
            HinhAnh = FormHinhAnh,
            KichThuoc = FormKichThuoc,
            SizeID = IsEditing ? SelectedPizza?.SizeID : null,
            GiaBan = giaBan,
            TrangThai = FormTrangThai
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

        var success = await _databaseService.DeletePizzaByMaAsync(item.MaPizza);

        if (success)
        {
            await LoadDataAsync();
        }
    }

    private void BrowseImage()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn hình ảnh pizza",
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif, *.bmp, *.webp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var sourceFile = openFileDialog.FileName;
                var fileName = System.IO.Path.GetFileName(sourceFile);

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var extension = System.IO.Path.GetExtension(fileName);
                var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                var uniqueFileName = $"{nameWithoutExt}_{timestamp}{extension}";

                var projectPath = AppDomain.CurrentDomain.BaseDirectory;
                var resourcesPath = System.IO.Path.Combine(projectPath, "Resources", "Images");

                System.IO.Directory.CreateDirectory(resourcesPath);

                var destPath = System.IO.Path.Combine(resourcesPath, uniqueFileName);

                System.IO.File.Copy(sourceFile, destPath, true);

                FormHinhAnh = $"/Resources/Images/{uniqueFileName}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error browsing image: {ex.Message}");
        }
    }
}

