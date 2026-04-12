using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
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

    private string _formTinhTP = string.Empty;
    public string FormTinhTP
    {
        get => _formTinhTP;
        set => SetProperty(ref _formTinhTP, value);
    }

    private string _formPhuongXa = string.Empty;
    public string FormPhuongXa
    {
        get => _formPhuongXa;
        set => SetProperty(ref _formPhuongXa, value);
    }

    private string _formQuanHuyen = string.Empty;
    public string FormQuanHuyen
    {
        get => _formQuanHuyen;
        set => SetProperty(ref _formQuanHuyen, value);
    }

    private string _formDiaChiChiTiet = string.Empty;
    public string FormDiaChiChiTiet
    {
        get => _formDiaChiChiTiet;
        set => SetProperty(ref _formDiaChiChiTiet, value);
    }

    private string _formThonXom = string.Empty;
    public string FormThonXom
    {
        get => _formThonXom;
        set => SetProperty(ref _formThonXom, value);
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

    
    // Error properties
    private string _errorHoTen = string.Empty;
    public string ErrorHoTen { get => _errorHoTen; set => SetProperty(ref _errorHoTen, value); }
    private string _errorNgaySinh = string.Empty;
    public string ErrorNgaySinh { get => _errorNgaySinh; set => SetProperty(ref _errorNgaySinh, value); }
    private string _errorChucVu = string.Empty;
    public string ErrorChucVu { get => _errorChucVu; set => SetProperty(ref _errorChucVu, value); }
    private string _errorSDT = string.Empty;
    public string ErrorSDT { get => _errorSDT; set => SetProperty(ref _errorSDT, value); }
    private string _errorTinhTP = string.Empty;
    public string ErrorTinhTP { get => _errorTinhTP; set => SetProperty(ref _errorTinhTP, value); }
    private string _errorPhuongXa = string.Empty;
    public string ErrorPhuongXa { get => _errorPhuongXa; set => SetProperty(ref _errorPhuongXa, value); }
    private string _errorThonXom = string.Empty;
    public string ErrorThonXom { get => _errorThonXom; set => SetProperty(ref _errorThonXom, value); }
    private string _errorDiaChiChiTiet = string.Empty;
    public string ErrorDiaChiChiTiet { get => _errorDiaChiChiTiet; set => SetProperty(ref _errorDiaChiChiTiet, value); }

    private bool _isProgrammaticAddressUpdate = false;

    private ObservableCollection<ApiProvince> _provinces = new();
    public ObservableCollection<ApiProvince> Provinces
    {
        get => _provinces;
        set => SetProperty(ref _provinces, value);
    }

    private ObservableCollection<ApiDistrict> _districts = new();
    public ObservableCollection<ApiDistrict> Districts
    {
        get => _districts;
        set => SetProperty(ref _districts, value);
    }

    private ObservableCollection<ApiWard> _wards = new();
    public ObservableCollection<ApiWard> Wards
    {
        get => _wards;
        set => SetProperty(ref _wards, value);
    }

    private ApiProvince? _selectedProvince;
    public ApiProvince? SelectedProvince
    {
        get => _selectedProvince;
        set
        {
            if (SetProperty(ref _selectedProvince, value))
            {
                FormTinhTP = value?.name ?? string.Empty;
                if (!_isProgrammaticAddressUpdate)
                {
                    SelectedWard = null;
                    Wards.Clear();
                    if (value != null)
                    {
                        Task.Run(async () => {
                            var wards = await LocationService.Instance.GetWardsAsync(value.code);
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                Wards.Clear();
                                foreach (var w in wards) Wards.Add(w);
                            });
                        });
                    }
                }
            }
        }
    }

    private ApiDistrict? _selectedDistrict;
    public ApiDistrict? SelectedDistrict
    {
        get => _selectedDistrict;
        set
        {
            if (SetProperty(ref _selectedDistrict, value))
            {
                FormQuanHuyen = value?.name ?? string.Empty;
                if (!_isProgrammaticAddressUpdate)
                {
                    SelectedWard = null;
                    Wards.Clear();
                    if (value != null)
                    {
                        Task.Run(async () => {
                            var wards = await LocationService.Instance.GetWardsAsync(value.code);
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                Wards.Clear();
                                foreach (var w in wards) Wards.Add(w);
                            });
                        });
                    }
                }
            }
        }
    }

    private ApiWard? _selectedWard;
    public ApiWard? SelectedWard
    {
        get => _selectedWard;
        set
        {
            if (SetProperty(ref _selectedWard, value))
            {
                if (value != null) FormPhuongXa = value.name;
            }
        }
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

    private bool _isStatusDialogOpen;
    public bool IsStatusDialogOpen
    {
        get => _isStatusDialogOpen;
        set => SetProperty(ref _isStatusDialogOpen, value);
    }

    private bool _isAccountEditMode;
    public bool IsAccountEditMode
    {
        get => _isAccountEditMode;
        set => SetProperty(ref _isAccountEditMode, value);
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
    
    // Lệnh chức vụ
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

    // Status dialog commands
    public ICommand CloseStatusDialogCommand { get; }
    public ICommand ConfirmToggleStatusCommand { get; }
    #endregion

    public NhanVienViewModel()
    {
        _databaseService = new DatabaseService();

        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        CreateNhanVienCommand = new RelayCommand(_ => OpenCreateDialog());
        EditNhanVienCommand = new RelayCommand(p => OpenEditDialog(p));
        DeleteNhanVienCommand = new AsyncRelayCommand(async p => await DeleteNhanVienAsync(p));
        ToggleStatusCommand = new AsyncRelayCommand(async p => await ToggleStatusAsync(p));
        SaveNhanVienCommand = new AsyncRelayCommand(async _ => await SaveNhanVienAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        
        OpenChucVuViewCommand = new RelayCommand(_ => OpenChucVuView());
        BackToMainViewCommand = new RelayCommand(_ => BackToMainView());
        CreateChucVuCommand = new RelayCommand(_ => OpenCreateChucVuDialog());
        EditChucVuCommand = new RelayCommand(p => OpenEditChucVuDialog(p));
        DeleteChucVuCommand = new AsyncRelayCommand(async p => await DeleteChucVuAsync(p));
        SaveChucVuCommand = new AsyncRelayCommand(async _ => await SaveChucVuAsync());
        CancelChucVuDialogCommand = new RelayCommand(_ => CloseChucVuDialog());
        
        OpenAccountDialogCommand = new AsyncRelayCommand(async p => await OpenAccountDialogAsync(p));
        SaveAccountCommand = new AsyncRelayCommand(async _ => await SaveAccountAsync());
        CancelAccountDialogCommand = new RelayCommand(_ => CloseAccountDialog());
        
        ApplyChucVuFilterCommand = new AsyncRelayCommand(async _ => await LoadNhanViensAsync());
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        
        SelectImageCommand = new RelayCommand(_ => SelectImage());

        CloseStatusDialogCommand = new RelayCommand(_ => IsStatusDialogOpen = false);
        ConfirmToggleStatusCommand = new AsyncRelayCommand(async _ => await ConfirmToggleStatusAsync());

        SafeInitializeAsync(LoadDataAsync);
    }

    #region Phương thức
    private void SelectImage()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Chọn hình ảnh",
            Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|Tất cả tệp (*.*)|*.*",
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

    private async Task SetupAddressAsync(string provinceName, string districtName, string wardName)
    {
        _isProgrammaticAddressUpdate = true;
        
        try
        {
            if (Provinces.Count == 0)
            {
                var p = await LocationService.Instance.GetProvincesAsync();
                Provinces.Clear();
                foreach (var item in p) Provinces.Add(item);
            }

            var prov = Provinces.FirstOrDefault(x => x.name == provinceName);
            SelectedProvince = prov;
            
            if (prov != null)
            {
                var wards = await LocationService.Instance.GetWardsAsync(prov.code);
                Wards.Clear();
                foreach (var w in wards) Wards.Add(w);
                SelectedWard = Wards.FirstOrDefault(x => x.name == wardName);
            }
            else 
            {
                SelectedWard = null;
                Wards.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up address: {ex.Message}");
        }
        
        _isProgrammaticAddressUpdate = false;
    }

    private void OpenCreateDialog()
    {
        IsCreateMode = true;
        FormHoTen = string.Empty;
        FormNgaySinh = null;
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;
        FormSDT = string.Empty;
        FormEmail = string.Empty;
        FormChucVu = null;
        FormHinhAnh = string.Empty;
        FormTrangThai = true;
        _ = SetupAddressAsync("", "", "");
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
            FormTinhTP = string.Empty;
            FormPhuongXa = string.Empty;
            FormQuanHuyen = string.Empty;
            FormDiaChiChiTiet = string.Empty;
            FormThonXom = string.Empty;
            if (!string.IsNullOrWhiteSpace(nv.DiaChi))
            {
                var parts = nv.DiaChi.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    FormDiaChiChiTiet = parts[0];
                    FormThonXom = parts[1];
                    FormPhuongXa = parts[2];
                    FormQuanHuyen = parts[3];
                    FormTinhTP = string.Join(", ", parts.Skip(4));
                }
                else if (parts.Length == 4)
                {
                    FormDiaChiChiTiet = parts[0];
                    FormPhuongXa = parts[1];
                    FormQuanHuyen = parts[2];
                    FormTinhTP = parts[3];
                }
                else if (parts.Length == 3)
                {
                    FormDiaChiChiTiet = parts[0];
                    FormQuanHuyen = parts[1];
                    FormTinhTP = parts[2];
                }
                else if (parts.Length == 2)
                {
                    FormDiaChiChiTiet = parts[0];
                    FormTinhTP = parts[1];
                }
                else if (parts.Length > 0) FormDiaChiChiTiet = parts[0];
            }
            _ = SetupAddressAsync(FormTinhTP, FormQuanHuyen, FormPhuongXa);
            FormSDT = nv.SDT ?? string.Empty;
            FormEmail = nv.Email ?? string.Empty;
            FormChucVu = ChucVus.FirstOrDefault(cv => cv.ChucVuID == nv.ChucVuID);
            FormHinhAnh = nv.HinhAnh ?? string.Empty;
            FormTrangThai = nv.TrangThai;
            IsDialogOpen = true;
        }
    }

    
    private bool ValidateForm()
    {
        bool isValid = true;
        ErrorHoTen = string.Empty;
        ErrorNgaySinh = string.Empty;
        ErrorChucVu = string.Empty;
        ErrorSDT = string.Empty;
        ErrorTinhTP = string.Empty;
        ErrorPhuongXa = string.Empty;
        ErrorThonXom = string.Empty;
        ErrorDiaChiChiTiet = string.Empty;

        if (string.IsNullOrWhiteSpace(FormHoTen))
        {
            ErrorHoTen = "Vui lòng nhập họ tên nhân viên";
            isValid = false;
        }

        if (!FormNgaySinh.HasValue)
        {
            ErrorNgaySinh = "Vui lòng chọn ngày sinh";
            isValid = false;
        }
        else if (FormNgaySinh.Value.AddYears(16) > DateTime.Today)
        {
            ErrorNgaySinh = "Nhân viên phải từ đủ 16 tuổi trở lên";
            isValid = false;
        }

        if (FormChucVu == null)
        {
            ErrorChucVu = "Vui lòng chọn chức vụ";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(FormSDT))
        {
            ErrorSDT = "Vui lòng nhập số điện thoại";
            isValid = false;
        }
        else
        {
            var sdt = FormSDT.Trim();
            if (sdt.Length != 10 || !sdt.StartsWith("0") || !sdt.All(char.IsDigit))
            {
                ErrorSDT = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0";
                isValid = false;
            }
        }

        if (string.IsNullOrWhiteSpace(FormTinhTP)) { ErrorTinhTP = "Vui lòng chọn Tỉnh/TP"; isValid = false; }
        if (string.IsNullOrWhiteSpace(FormPhuongXa)) { ErrorPhuongXa = "Vui lòng chọn Phường/Xã"; isValid = false; }
        if (string.IsNullOrWhiteSpace(FormThonXom)) { ErrorThonXom = "Vui lòng nhập Thôn/Xóm"; isValid = false; }
        if (string.IsNullOrWhiteSpace(FormDiaChiChiTiet)) { ErrorDiaChiChiTiet = "Vui lòng nhập Số nhà/Đường"; isValid = false; }

        return isValid;
    }

    private async Task SaveNhanVienAsync()
    {
        if (string.IsNullOrWhiteSpace(FormHoTen))
        {
            System.Windows.MessageBox.Show("Vui lòng nhập họ tên nhân viên.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!FormNgaySinh.HasValue)
        {
            System.Windows.MessageBox.Show("Vui lòng chọn ngày sinh.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (FormNgaySinh.HasValue && FormNgaySinh.Value.AddYears(16) > DateTime.Today)
        {
            System.Windows.MessageBox.Show("Nhân viên phải từ đủ 16 tuổi trở lên.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (FormChucVu == null)
        {
            System.Windows.MessageBox.Show("Vui lòng chọn chức vụ.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(FormSDT))
        {
            System.Windows.MessageBox.Show("Vui lòng nhập số điện thoại.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(FormSDT))
        {
            var sdt = FormSDT.Trim();
            if (sdt.Length != 10 || !sdt.StartsWith("0") || !sdt.All(char.IsDigit))
            {
                System.Windows.MessageBox.Show("Số điện thoại phải có 10 chữ số và bắt đầu bằng 0.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(FormTinhTP) || string.IsNullOrWhiteSpace(FormPhuongXa) || string.IsNullOrWhiteSpace(FormThonXom) || string.IsNullOrWhiteSpace(FormDiaChiChiTiet))
        {
            System.Windows.MessageBox.Show("Vui lòng nhập đầy đủ địa chỉ (Số nhà/Đường, Thôn/Xóm, Phường/Xã, Tỉnh/TP).", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(FormEmail))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(FormEmail.Trim());
                if (addr.Address != FormEmail.Trim()) throw new Exception();
            }
            catch
            {
                System.Windows.MessageBox.Show("Email không đúng định dạng. VD: abc@gmail.com", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var nhanVien = new NhanVien
            {
                HoTen = FormHoTen.Trim(),
                NgaySinh = FormNgaySinh,
                DiaChi = string.Join(", ", new[] { FormDiaChiChiTiet, FormThonXom, FormPhuongXa, FormTinhTP }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())),
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
            SelectedNhanVien = nv;
            IsStatusDialogOpen = true;
        }
    }

    private async Task ConfirmToggleStatusAsync()
    {
        if (SelectedNhanVien == null) return;

        try
        {
            var newStatus = !SelectedNhanVien.TrangThai;
            await _databaseService.UpdateNhanVienTrangThaiAsync(SelectedNhanVien.NhanVienID, newStatus);
            IsStatusDialogOpen = false;
            await LoadNhanViensAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling status: {ex.Message}");
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
    private async Task OpenAccountDialogAsync(object? parameter)
    {
        if (parameter is NhanVien nv)
        {
            SelectedNhanVien = nv;
            var taiKhoan = await _databaseService.GetTaiKhoanByNhanVienIDAsync(nv.NhanVienID);
            if (taiKhoan != null)
            {
                IsAccountEditMode = true;
                FormUsername = taiKhoan.Username;
                FormPassword = taiKhoan.Password;
            }
            else
            {
                IsAccountEditMode = false;
                FormUsername = string.Empty;
                FormPassword = string.Empty;
            }
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








