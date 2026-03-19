using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class NguyenLieuItemViewModel : BaseViewModel
{
    public int NguyenLieuID { get; set; }
    public string? MaNguyenLieu { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public string? LoaiNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }
    public string? NhaCungCap { get; set; }
    public decimal GiaNhap { get; set; }
    public bool TrangThai { get; set; }
    
    public string TrangThaiText => TrangThai ? "Đang sử dụng" : "Ngừng sử dụng";
    public bool IsTrangThaiActive => TrangThai;
    
    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class NguyenLieuViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    
    private ObservableCollection<NguyenLieuItemViewModel> _nguyenLieus = new();
    private ObservableCollection<NguyenLieuItemViewModel> _filteredNguyenLieus = new();
    private ObservableCollection<LoaiNguyenLieu> _loaiNguyenLieus = new();
    private ObservableCollection<NhaCungCap> _nhaCungCaps = new();
    private ObservableCollection<DonViTinh> _donViTinhs = new();
    
    private string _searchText = string.Empty;
    private bool _filterDangSuDung = false;
    private bool _filterNgungSuDung = false;
    private LoaiNguyenLieu? _selectedLoaiFilter;
    private NhaCungCap? _selectedNhaCungCapFilter;
    private int _totalNguyenLieu;
    private int _countDangSuDung;
    private int _countNgungSuDung;
    private bool _isLoading;
    
    // Popup thêm/sửa
    private bool _isAddEditPopupOpen;
    private bool _isEditing;
    private NguyenLieuItemViewModel? _selectedNguyenLieu;
    
    // Trường form thêm/sửa
    private string _formMaNguyenLieu = string.Empty;
    private string _formTenNguyenLieu = string.Empty;
    private string _formHinhAnh = string.Empty;
    private LoaiNguyenLieu? _formLoaiNguyenLieu;
    private DonViTinh? _formDonViTinh;
    private NhaCungCap? _formNhaCungCap;
    private string _formGiaNhap = string.Empty;
    private bool _formTrangThai = true;
    
    // Quản lý loại nguyên liệu
    private bool _isLoaiNguyenLieuViewVisible;
    
    public ObservableCollection<NguyenLieuItemViewModel> FilteredNguyenLieus
    {
        get => _filteredNguyenLieus;
        set => SetProperty(ref _filteredNguyenLieus, value);
    }
    
    public ObservableCollection<LoaiNguyenLieu> LoaiNguyenLieus
    {
        get => _loaiNguyenLieus;
        set => SetProperty(ref _loaiNguyenLieus, value);
    }
    
    public ObservableCollection<NhaCungCap> NhaCungCaps
    {
        get => _nhaCungCaps;
        set => SetProperty(ref _nhaCungCaps, value);
    }
    
    public ObservableCollection<DonViTinh> DonViTinhs
    {
        get => _donViTinhs;
        set => SetProperty(ref _donViTinhs, value);
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
    
    public bool FilterDangSuDung
    {
        get => _filterDangSuDung;
        set
        {
            if (SetProperty(ref _filterDangSuDung, value))
            {
                ApplyFilters();
            }
        }
    }
    
    public bool FilterNgungSuDung
    {
        get => _filterNgungSuDung;
        set
        {
            if (SetProperty(ref _filterNgungSuDung, value))
            {
                ApplyFilters();
            }
        }
    }
    
    public LoaiNguyenLieu? SelectedLoaiFilter
    {
        get => _selectedLoaiFilter;
        set
        {
            if (SetProperty(ref _selectedLoaiFilter, value))
            {
                ApplyFilters();
            }
        }
    }
    
    public NhaCungCap? SelectedNhaCungCapFilter
    {
        get => _selectedNhaCungCapFilter;
        set
        {
            if (SetProperty(ref _selectedNhaCungCapFilter, value))
            {
                ApplyFilters();
            }
        }
    }
    
    public int TotalNguyenLieu
    {
        get => _totalNguyenLieu;
        set => SetProperty(ref _totalNguyenLieu, value);
    }

    public int CountDangSuDung
    {
        get => _countDangSuDung;
        set => SetProperty(ref _countDangSuDung, value);
    }

    public int CountNgungSuDung
    {
        get => _countNgungSuDung;
        set => SetProperty(ref _countNgungSuDung, value);
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
    
    public NguyenLieuItemViewModel? SelectedNguyenLieu
    {
        get => _selectedNguyenLieu;
        set => SetProperty(ref _selectedNguyenLieu, value);
    }
    
    // Thuộc tính form
    public string FormMaNguyenLieu
    {
        get => _formMaNguyenLieu;
        set => SetProperty(ref _formMaNguyenLieu, value);
    }
    
    public string FormTenNguyenLieu
    {
        get => _formTenNguyenLieu;
        set => SetProperty(ref _formTenNguyenLieu, value);
    }
    
    public string FormHinhAnh
    {
        get => _formHinhAnh;
        set => SetProperty(ref _formHinhAnh, value);
    }
    
    public LoaiNguyenLieu? FormLoaiNguyenLieu
    {
        get => _formLoaiNguyenLieu;
        set => SetProperty(ref _formLoaiNguyenLieu, value);
    }
    
    public DonViTinh? FormDonViTinh
    {
        get => _formDonViTinh;
        set => SetProperty(ref _formDonViTinh, value);
    }
    
    public NhaCungCap? FormNhaCungCap
    {
        get => _formNhaCungCap;
        set => SetProperty(ref _formNhaCungCap, value);
    }
    
    public string FormGiaNhap
    {
        get => _formGiaNhap;
        set => SetProperty(ref _formGiaNhap, value);
    }
    
    public bool FormTrangThai
    {
        get => _formTrangThai;
        set => SetProperty(ref _formTrangThai, value);
    }
    
    public bool IsLoaiNguyenLieuViewVisible
    {
        get => _isLoaiNguyenLieuViewVisible;
        set => SetProperty(ref _isLoaiNguyenLieuViewVisible, value);
    }
    
    // Lệnh
    public ICommand LoadDataCommand { get; }
    public ICommand OpenAddPopupCommand { get; }
    public ICommand ClosePopupCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand OpenLoaiNguyenLieuCommand { get; }
    public ICommand BackFromLoaiNguyenLieuCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand BrowseImageCommand { get; }
    
    // LoaiNguyenLieu ViewModel
    public LoaiNguyenLieuViewModel LoaiNguyenLieuVM { get; }
    
    public NguyenLieuViewModel()
    {
        _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        
        LoaiNguyenLieuVM = new LoaiNguyenLieuViewModel();
        LoaiNguyenLieuVM.OnBack += () => IsLoaiNguyenLieuViewVisible = false;
        
        // ⚡ AsyncRelayCommand thay vì async void trong RelayCommand
        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        OpenAddPopupCommand = new RelayCommand(_ => OpenAddPopup());
        ClosePopupCommand = new RelayCommand(_ => ClosePopup());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        OpenLoaiNguyenLieuCommand = new RelayCommand(_ => OpenLoaiNguyenLieu());
        BackFromLoaiNguyenLieuCommand = new RelayCommand(_ => IsLoaiNguyenLieuViewVisible = false);
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        BrowseImageCommand = new RelayCommand(_ => BrowseImage());
        
        // ⚡ SafeInitializeAsync thay vì fire-and-forget
        SafeInitializeAsync(LoadDataAsync);
    }
    
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        
        try
        {
            var loaiList = await _databaseService.GetLoaiNguyenLieusAsync();
            LoaiNguyenLieus = new ObservableCollection<LoaiNguyenLieu>(loaiList);
            
            var nccList = await _databaseService.GetNhaCungCapsAsync();
            NhaCungCaps = new ObservableCollection<NhaCungCap>(nccList);
            
            var dvList = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs = new ObservableCollection<DonViTinh>(dvList);
            
            var nguyenLieuList = await _databaseService.GetAllNguyenLieusWithDetailsAsync();
            _nguyenLieus.Clear();
            
            foreach (var nl in nguyenLieuList)
            {
                var item = new NguyenLieuItemViewModel
                {
                    NguyenLieuID = nl.NguyenLieuID,
                    MaNguyenLieu = nl.MaNguyenLieu,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    HinhAnh = nl.HinhAnh,
                    LoaiNguyenLieu = nl.LoaiNguyenLieu?.TenLoai,
                    DonViTinh = nl.DonViTinh?.TenDonVi,
                    TrangThai = nl.TrangThai,
                    NhaCungCap = nl.NguyenLieuNhaCungCaps?.FirstOrDefault()?.NhaCungCap?.TenNCC,
                    GiaNhap = nl.NguyenLieuNhaCungCaps?.FirstOrDefault()?.GiaNhap ?? 0
                };
                item.EditCommand = new RelayCommand(p => EditNguyenLieu(item));
                item.DeleteCommand = new RelayCommand(async p => await DeleteNguyenLieuAsync(item));
                _nguyenLieus.Add(item);
            }
            
            ApplyFilters();
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
    
    private void ApplyFilters()
    {
        var filtered = _nguyenLieus.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(n => 
                n.TenNguyenLieu.ToLower().Contains(searchLower) ||
                (n.MaNguyenLieu?.ToLower().Contains(searchLower) ?? false));
        }
        
        if (FilterDangSuDung && !FilterNgungSuDung)
        {
            filtered = filtered.Where(n => n.TrangThai);
        }
        else if (FilterNgungSuDung && !FilterDangSuDung)
        {
            filtered = filtered.Where(n => !n.TrangThai);
        }
        
        if (SelectedLoaiFilter != null)
        {
            filtered = filtered.Where(n => n.LoaiNguyenLieu == SelectedLoaiFilter.TenLoai);
        }
        
        if (SelectedNhaCungCapFilter != null)
        {
            filtered = filtered.Where(n => n.NhaCungCap == SelectedNhaCungCapFilter.TenNCC);
        }
        
        FilteredNguyenLieus = new ObservableCollection<NguyenLieuItemViewModel>(filtered);
        TotalNguyenLieu = _nguyenLieus.Count;
        CountDangSuDung = _nguyenLieus.Count(n => n.TrangThai);
        CountNgungSuDung = _nguyenLieus.Count(n => !n.TrangThai);
    }
    
    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterDangSuDung = false;
        FilterNgungSuDung = false;
        SelectedLoaiFilter = null;
        SelectedNhaCungCapFilter = null;
    }
    
    private void OpenAddPopup()
    {
        IsEditing = false;
        SelectedNguyenLieu = null;
        ClearForm();
        IsAddEditPopupOpen = true;
    }
    
    private void EditNguyenLieu(NguyenLieuItemViewModel item)
    {
        IsEditing = true;
        SelectedNguyenLieu = item;
        
        FormMaNguyenLieu = item.MaNguyenLieu ?? string.Empty;
        FormTenNguyenLieu = item.TenNguyenLieu;
        FormHinhAnh = item.HinhAnh ?? string.Empty;
        FormLoaiNguyenLieu = LoaiNguyenLieus.FirstOrDefault(l => l.TenLoai == item.LoaiNguyenLieu);
        FormDonViTinh = DonViTinhs.FirstOrDefault(d => d.TenDonVi == item.DonViTinh);
        FormNhaCungCap = NhaCungCaps.FirstOrDefault(n => n.TenNCC == item.NhaCungCap);
        FormGiaNhap = item.GiaNhap.ToString("N0");
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
        FormMaNguyenLieu = string.Empty;
        FormTenNguyenLieu = string.Empty;
        FormHinhAnh = string.Empty;
        FormLoaiNguyenLieu = null;
        FormDonViTinh = null;
        FormNhaCungCap = null;
        FormGiaNhap = string.Empty;
        FormTrangThai = true;
    }
    
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTenNguyenLieu))
        {
            return;
        }
        
        var nguyenLieu = new NguyenLieu
        {
            NguyenLieuID = IsEditing ? SelectedNguyenLieu?.NguyenLieuID ?? 0 : 0,
            MaNguyenLieu = FormMaNguyenLieu,
            TenNguyenLieu = FormTenNguyenLieu,
            HinhAnh = FormHinhAnh,
            LoaiNLID = FormLoaiNguyenLieu?.LoaiNLID,
            DonViID = FormDonViTinh?.DonViID,
            TrangThai = FormTrangThai
        };
        
        var success = await _databaseService.SaveNguyenLieuAsync(nguyenLieu);
        
        if (success)
        {
            ClosePopup();
            await LoadDataAsync();
        }
    }
    
    private async Task DeleteNguyenLieuAsync(NguyenLieuItemViewModel item)
    {
        var success = await _databaseService.DeleteNguyenLieuAsync(item.NguyenLieuID);
        
        if (success)
        {
            await LoadDataAsync();
        }
    }
    
    private void OpenLoaiNguyenLieu()
    {
        IsLoaiNguyenLieuViewVisible = true;
        LoaiNguyenLieuVM.LoadDataCommand.Execute(null);
    }
    
    private void BrowseImage()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn hình ảnh nguyên liệu",
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif, *.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Lấy đường dẫn file nguồn
                var sourceFile = openFileDialog.FileName;
                var fileName = System.IO.Path.GetFileName(sourceFile);
                
                // Tạo tên file duy nhất để tránh ghi đè
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var extension = System.IO.Path.GetExtension(fileName);
                var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                var uniqueFileName = $"{nameWithoutExt}_{timestamp}{extension}";
                
                // Xác định đường dẫn đích trong thư mục Resources
                var projectPath = AppDomain.CurrentDomain.BaseDirectory;
                var resourcesPath = System.IO.Path.Combine(projectPath, "Resources", "Images");
                
                // Tạo thư mục nếu chưa tồn tại
                System.IO.Directory.CreateDirectory(resourcesPath);
                
                // Đường dẫn đích đầy đủ
                var destPath = System.IO.Path.Combine(resourcesPath, uniqueFileName);
                
                // Sao chép file vào thư mục Resources
                System.IO.File.Copy(sourceFile, destPath, true);
                
                // Lưu đường dẫn tương đối cho cơ sở dữ liệu
                FormHinhAnh = $"/Resources/Images/{uniqueFileName}";
                
                System.Diagnostics.Debug.WriteLine($"Image saved to: {destPath}");
                System.Diagnostics.Debug.WriteLine($"Relative path: {FormHinhAnh}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error browsing image: {ex.Message}");
            // Có thể hiển thị thông báo lỗi cho người dùng tại đây
        }
    }
}

