using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ChucVuFilter : BaseViewModel
{
    public ChucVu ChucVu { get; set; } = new();
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class NhanVienViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    #region Thuộc tính
    private ObservableCollection<NhanVien> _nhanViens = new();
    public ObservableCollection<NhanVien> NhanViens
    {
        get => _nhanViens;
        set => SetProperty(ref _nhanViens, value);
    }

    private NhanVien? _selectedNhanVien;
    public NhanVien? SelectedNhanVien
    {
        get => _selectedNhanVien;
        set => SetProperty(ref _selectedNhanVien, value);
    }

    private ObservableCollection<ChucVu> _chucVus = new();
    public ObservableCollection<ChucVu> ChucVus
    {
        get => _chucVus;
        set => SetProperty(ref _chucVus, value);
    }

    private ObservableCollection<ChucVuFilter> _chucVuFilters = new();
    public ObservableCollection<ChucVuFilter> ChucVuFilters
    {
        get => _chucVuFilters;
        set => SetProperty(ref _chucVuFilters, value);
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
                _ = LoadNhanViensAsync();
            }
        }
    }

    private bool _filterDangLamViec = true;
    public bool FilterDangLamViec
    {
        get => _filterDangLamViec;
        set
        {
            if (SetProperty(ref _filterDangLamViec, value))
            {
                _ = LoadNhanViensAsync();
            }
        }
    }

    private bool _filterDaNghiViec;
    public bool FilterDaNghiViec
    {
        get => _filterDaNghiViec;
        set
        {
            if (SetProperty(ref _filterDaNghiViec, value))
            {
                _ = LoadNhanViensAsync();
            }
        }
    }

    private int _tongSo;
    public int TongSo
    {
        get => _tongSo;
        set => SetProperty(ref _tongSo, value);
    }

    // Trạng thái hiển thị
    private bool _isMainView = true;
    public bool IsMainView
    {
        get => _isMainView;
        set => SetProperty(ref _isMainView, value);
    }

    private bool _isChucVuView;
    public bool IsChucVuView
    {
        get => _isChucVuView;
        set => SetProperty(ref _isChucVuView, value);
    }

    // Thuộc tính hộp thoại
    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    private bool _isChucVuDialogOpen;
    public bool IsChucVuDialogOpen
    {
        get => _isChucVuDialogOpen;
        set => SetProperty(ref _isChucVuDialogOpen, value);
    }

    private bool _isAccountDialogOpen;
    public bool IsAccountDialogOpen
    {
        get => _isAccountDialogOpen;
        set => SetProperty(ref _isAccountDialogOpen, value);
    }

    private bool _isCreateMode;
    public bool IsCreateMode
    {
        get => _isCreateMode;
        set => SetProperty(ref _isCreateMode, value);
    }

    private bool _isChucVuCreateMode;
    public bool IsChucVuCreateMode
    {
        get => _isChucVuCreateMode;
        set => SetProperty(ref _isChucVuCreateMode, value);
    }

    // Thuộc tính form - Nhân viên
    private string _formHoTen = string.Empty;
    public string FormHoTen
    {
        get => _formHoTen;
        set => SetProperty(ref _formHoTen, value);
    }

    private DateTime? _formNgaySinh;
    public DateTime? FormNgaySinh
    {
        get => _formNgaySinh;
        set => SetProperty(ref _formNgaySinh, value);
    }

    private string _formDiaChi = string.Empty;
    public string FormDiaChi
    {
        get => _formDiaChi;
        set => SetProperty(ref _formDiaChi, value);
    }

    private string _formSDT = string.Empty;
    public string FormSDT
    {
        get => _formSDT;
        set => SetProperty(ref _formSDT, value);
    }

    private string _formEmail = string.Empty;
    public string FormEmail
    {
        get => _formEmail;
        set => SetProperty(ref _formEmail, value);
    }

    private ChucVu? _formChucVu;
    public ChucVu? FormChucVu
    {
        get => _formChucVu;
        set => SetProperty(ref _formChucVu, value);
    }

    private string _formHinhAnh = string.Empty;
    public string FormHinhAnh
    {
        get => _formHinhAnh;
        set => SetProperty(ref _formHinhAnh, value);
    }

    private bool _formTrangThai = true;
    public bool FormTrangThai
    {
        get => _formTrangThai;
        set => SetProperty(ref _formTrangThai, value);
    }

    // Thuộc tính form - Chức vụ
    private string _formTenChucVu = string.Empty;
    public string FormTenChucVu
    {
        get => _formTenChucVu;
        set => SetProperty(ref _formTenChucVu, value);
    }

    private ChucVu? _selectedChucVu;
    public ChucVu? SelectedChucVu
    {
        get => _selectedChucVu;
        set => SetProperty(ref _selectedChucVu, value);
    }

    // Thuộc tính form - Tài khoản
    private string _formUsername = string.Empty;
    public string FormUsername
    {
        get => _formUsername;
        set => SetProperty(ref _formUsername, value);
    }

    private string _formPassword = string.Empty;
    public string FormPassword
    {
        get => _formPassword;
        set => SetProperty(ref _formPassword, value);
    }

    private int _countDangLamViec;
    public int CountDangLamViec
    {
        get => _countDangLamViec;
        set => SetProperty(ref _countDangLamViec, value);
    }

    private int _countDaNghiViec;
    public int CountDaNghiViec
    {
        get => _countDaNghiViec;
        set => SetProperty(ref _countDaNghiViec, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region Lệnh
    public ICommand LoadDataCommand { get; }
    public ICommand CreateNhanVienCommand { get; }
    public ICommand EditNhanVienCommand { get; }
    public ICommand DeleteNhanVienCommand { get; }
    public ICommand ToggleStatusCommand { get; }
    public ICommand SaveNhanVienCommand { get; }
    public ICommand CancelDialogCommand { get; }
    
    // ChucVu commands
    public ICommand OpenChucVuViewCommand { get; }
    public ICommand BackToMainViewCommand { get; }
    public ICommand CreateChucVuCommand { get; }
    public ICommand EditChucVuCommand { get; }
    public ICommand DeleteChucVuCommand { get; }
    public ICommand SaveChucVuCommand { get; }
    public ICommand CancelChucVuDialogCommand { get; }
    
    // Lệnh tài khoản
    public ICommand OpenAccountDialogCommand { get; }
    public ICommand SaveAccountCommand { get; }
    public ICommand CancelAccountDialogCommand { get; }
    
    // Lệnh lọc
    public ICommand ApplyChucVuFilterCommand { get; }
    public ICommand ClearFilterCommand { get; }
    
    // Lệnh hình ảnh
    public ICommand SelectImageCommand { get; }
    #endregion

    public NhanVienViewModel()
    {
        _databaseService = new DatabaseService();

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        CreateNhanVienCommand = new RelayCommand(_ => OpenCreateDialog());
        EditNhanVienCommand = new RelayCommand(p => OpenEditDialog(p));
        DeleteNhanVienCommand = new RelayCommand(async p => await DeleteNhanVienAsync(p));
        ToggleStatusCommand = new RelayCommand(async p => await ToggleStatusAsync(p));
        SaveNhanVienCommand = new RelayCommand(async _ => await SaveNhanVienAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        
        OpenChucVuViewCommand = new RelayCommand(_ => OpenChucVuView());
        BackToMainViewCommand = new RelayCommand(_ => BackToMainView());
        CreateChucVuCommand = new RelayCommand(_ => OpenCreateChucVuDialog());
        EditChucVuCommand = new RelayCommand(p => OpenEditChucVuDialog(p));
        DeleteChucVuCommand = new RelayCommand(async p => await DeleteChucVuAsync(p));
        SaveChucVuCommand = new RelayCommand(async _ => await SaveChucVuAsync());
        CancelChucVuDialogCommand = new RelayCommand(_ => CloseChucVuDialog());
        
        OpenAccountDialogCommand = new RelayCommand(p => OpenAccountDialog(p));
        SaveAccountCommand = new RelayCommand(async _ => await SaveAccountAsync());
        CancelAccountDialogCommand = new RelayCommand(_ => CloseAccountDialog());
        
        ApplyChucVuFilterCommand = new RelayCommand(async _ => await LoadNhanViensAsync());
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        
        SelectImageCommand = new RelayCommand(_ => SelectImage());

        _ = LoadDataAsync();
    }

    #region Phương thức
    private void SelectImage()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Chọn hình ảnh",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Lấy đường dẫn file nguồn
                string sourceFilePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(sourceFilePath);
                
                // Tạo tên file duy nhất để tránh xung đột
                string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                
                // Lấy đường dẫn thư mục Images trong Resources
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagesFolder = Path.Combine(appDirectory, "Resources", "Images", "NhanVien");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }
                
                // Sao chép file vào thư mục Images
                string destFilePath = Path.Combine(imagesFolder, uniqueFileName);
                File.Copy(sourceFilePath, destFilePath, true);
                
                // Lưu đường dẫn tương đối
                FormHinhAnh = Path.Combine("Resources", "Images", "NhanVien", uniqueFileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying image: {ex.Message}");
            }
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadChucVusAsync();
            await LoadNhanViensAsync();
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

    private async Task LoadChucVusAsync()
    {
        var chucVus = await _databaseService.GetAllChucVusAsync();
        ChucVus = new ObservableCollection<ChucVu>(chucVus);
        
        // Cập nhật bộ lọc
        ChucVuFilters = new ObservableCollection<ChucVuFilter>(
            chucVus.Select(cv => new ChucVuFilter { ChucVu = cv, IsSelected = false })
        );
    }

    private async Task LoadNhanViensAsync()
    {
        try
        {
            bool? trangThaiFilter = null;
            
            if (FilterDangLamViec && !FilterDaNghiViec)
            {
                trangThaiFilter = true;
            }
            else if (!FilterDangLamViec && FilterDaNghiViec)
            {
                trangThaiFilter = false;
            }

            var selectedChucVuIds = ChucVuFilters
                .Where(f => f.IsSelected)
                .Select(f => f.ChucVu.ChucVuID)
                .ToList();

            var nhanViens = await _databaseService.GetAllNhanViensFullAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                trangThaiFilter,
                selectedChucVuIds.Any() ? selectedChucVuIds : null);

            NhanViens = new ObservableCollection<NhanVien>(nhanViens);
            TongSo = nhanViens.Count;
            CountDangLamViec = nhanViens.Count(n => n.TrangThai);
            CountDaNghiViec = nhanViens.Count(n => !n.TrangThai);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhanViens: {ex.Message}");
        }
    }

    private void ClearFilter()
    {
        SearchText = string.Empty;
        FilterDangLamViec = true;
        FilterDaNghiViec = false;
        foreach (var filter in ChucVuFilters)
        {
            filter.IsSelected = false;
        }
        _ = LoadNhanViensAsync();
    }

    #region CRUD Nhân Viên
    private void OpenCreateDialog()
    {
        IsCreateMode = true;
        FormHoTen = string.Empty;
        FormNgaySinh = null;
        FormDiaChi = string.Empty;
        FormSDT = string.Empty;
        FormEmail = string.Empty;
        FormChucVu = null;
        FormHinhAnh = string.Empty;
        FormTrangThai = true;
        IsDialogOpen = true;
    }

    private void OpenEditDialog(object? parameter)
    {
        if (parameter is NhanVien nv)
        {
            SelectedNhanVien = nv;
            IsCreateMode = false;
            FormHoTen = nv.HoTen;
            FormNgaySinh = nv.NgaySinh;
            FormDiaChi = nv.DiaChi ?? string.Empty;
            FormSDT = nv.SDT ?? string.Empty;
            FormEmail = nv.Email ?? string.Empty;
            FormChucVu = ChucVus.FirstOrDefault(cv => cv.ChucVuID == nv.ChucVuID);
            FormHinhAnh = nv.HinhAnh ?? string.Empty;
            FormTrangThai = nv.TrangThai;
            IsDialogOpen = true;
        }
    }

    private async Task SaveNhanVienAsync()
    {
        if (string.IsNullOrWhiteSpace(FormHoTen))
        {
            return;
        }

        try
        {
            var nhanVien = new NhanVien
            {
                HoTen = FormHoTen.Trim(),
                NgaySinh = FormNgaySinh,
                DiaChi = string.IsNullOrWhiteSpace(FormDiaChi) ? null : FormDiaChi.Trim(),
                SDT = string.IsNullOrWhiteSpace(FormSDT) ? null : FormSDT.Trim(),
                Email = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail.Trim(),
                ChucVuID = FormChucVu?.ChucVuID,
                HinhAnh = string.IsNullOrWhiteSpace(FormHinhAnh) ? null : FormHinhAnh.Trim(),
                TrangThai = FormTrangThai
            };

            if (!IsCreateMode && SelectedNhanVien != null)
            {
                nhanVien.NhanVienID = SelectedNhanVien.NhanVienID;
            }

            await _databaseService.SaveNhanVienAsync(nhanVien);
            
            CloseDialog();
            await LoadNhanViensAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NhanVien: {ex.Message}");
        }
    }

    private async Task ToggleStatusAsync(object? parameter)
    {
        if (parameter is NhanVien nv)
        {
            try
            {
                var newStatus = !nv.TrangThai;
                await _databaseService.UpdateNhanVienTrangThaiAsync(nv.NhanVienID, newStatus);
                await LoadNhanViensAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling status: {ex.Message}");
            }
        }
    }

    private async Task DeleteNhanVienAsync(object? parameter)
    {
        if (parameter is NhanVien nv)
        {
            try
            {
                await _databaseService.DeleteNhanVienAsync(nv.NhanVienID);
                await LoadNhanViensAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting NhanVien: {ex.Message}");
            }
        }
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        SelectedNhanVien = null;
    }
    #endregion

    #region CRUD Chức Vụ
    private void OpenChucVuView()
    {
        IsMainView = false;
        IsChucVuView = true;
    }

    private void BackToMainView()
    {
        IsChucVuView = false;
        IsMainView = true;
        _ = LoadChucVusAsync();
    }

    private void OpenCreateChucVuDialog()
    {
        IsChucVuCreateMode = true;
        FormTenChucVu = string.Empty;
        SelectedChucVu = null;
        IsChucVuDialogOpen = true;
    }

    private void OpenEditChucVuDialog(object? parameter)
    {
        if (parameter is ChucVu cv)
        {
            SelectedChucVu = cv;
            IsChucVuCreateMode = false;
            FormTenChucVu = cv.TenChucVu;
            IsChucVuDialogOpen = true;
        }
    }

    private async Task SaveChucVuAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTenChucVu))
        {
            return;
        }

        try
        {
            var chucVu = new ChucVu
            {
                TenChucVu = FormTenChucVu.Trim()
            };

            if (!IsChucVuCreateMode && SelectedChucVu != null)
            {
                chucVu.ChucVuID = SelectedChucVu.ChucVuID;
            }

            await _databaseService.SaveChucVuAsync(chucVu);
            
            CloseChucVuDialog();
            await LoadChucVusAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving ChucVu: {ex.Message}");
        }
    }

    private async Task DeleteChucVuAsync(object? parameter)
    {
        if (parameter is ChucVu cv)
        {
            try
            {
                var result = await _databaseService.DeleteChucVuAsync(cv.ChucVuID);
                if (result)
                {
                    await LoadChucVusAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting ChucVu: {ex.Message}");
            }
        }
    }

    private void CloseChucVuDialog()
    {
        IsChucVuDialogOpen = false;
        SelectedChucVu = null;
        FormTenChucVu = string.Empty;
    }
    #endregion

    #region Quản lý Tài khoản
    private void OpenAccountDialog(object? parameter)
    {
        if (parameter is NhanVien nv)
        {
            SelectedNhanVien = nv;
            FormUsername = string.Empty;
            FormPassword = string.Empty;
            IsAccountDialogOpen = true;
        }
    }

    private async Task SaveAccountAsync()
    {
        if (SelectedNhanVien == null || string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormPassword))
        {
            return;
        }

        try
        {
            var result = await _databaseService.CreateTaiKhoanForNhanVienAsync(
                SelectedNhanVien.NhanVienID, 
                FormUsername.Trim(), 
                FormPassword);
                
            if (result)
            {
                CloseAccountDialog();
                await LoadNhanViensAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating account: {ex.Message}");
        }
    }

    private void CloseAccountDialog()
    {
        IsAccountDialogOpen = false;
        SelectedNhanVien = null;
        FormUsername = string.Empty;
        FormPassword = string.Empty;
    }
    #endregion
    #endregion
}

