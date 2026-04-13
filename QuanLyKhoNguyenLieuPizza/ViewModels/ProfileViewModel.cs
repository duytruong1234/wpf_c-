using System.IO;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ProfileViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly LocationService _locationService;
    private string? _hinhAnh;
    private string? _pendingAvatarSourceFile;
    private bool _isAvatarChanged;
    private bool _isProgrammaticAddressUpdate;

    public string Username => CurrentUserSession.Instance.CurrentUser?.Username ?? "N/A";
    public string HoTen => CurrentUserSession.Instance.CurrentUser?.NhanVien?.HoTen ?? "N/A";
    public string ChucVu => CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVu?.TenChucVu ?? "Nhân viên";
    
    private string? _email;
    public string? Email { get => _email; set => SetProperty(ref _email, value); }
    
    private string? _sdt;
    public string? SDT { get => _sdt; set => SetProperty(ref _sdt, value); }
    
    private DateTime? _ngaySinh;
    public DateTime? NgaySinh { get => _ngaySinh; set => SetProperty(ref _ngaySinh, value); }
    
    private string? _diaChi;
    public string? DiaChi { get => _diaChi; set => SetProperty(ref _diaChi, value); }
    
    // Address fields
    private string _formTinhTP = string.Empty;
    public string FormTinhTP { get => _formTinhTP; set => SetProperty(ref _formTinhTP, value); }

    private string _formQuanHuyen = string.Empty;
    public string FormQuanHuyen { get => _formQuanHuyen; set => SetProperty(ref _formQuanHuyen, value); }

    private string _formPhuongXa = string.Empty;
    public string FormPhuongXa { get => _formPhuongXa; set => SetProperty(ref _formPhuongXa, value); }

    private string _formDiaChiChiTiet = string.Empty;
    public string FormDiaChiChiTiet { get => _formDiaChiChiTiet; set => SetProperty(ref _formDiaChiChiTiet, value); }

    private string _formThonXom = string.Empty;
    public string FormThonXom { get => _formThonXom; set => SetProperty(ref _formThonXom, value); }

    // Location collections
    public ObservableCollection<ApiProvince> Provinces { get; set; } = new();
    public ObservableCollection<ApiDistrict> Districts { get; set; } = new();
    public ObservableCollection<ApiWard> Wards { get; set; } = new();

    private ApiProvince? _selectedProvince;
    public ApiProvince? SelectedProvince
    {
        get => _selectedProvince;
        set
        {
            if (SetProperty(ref _selectedProvince, value) && value != null && !_isProgrammaticAddressUpdate)
            {
                FormTinhTP = value.name;
                _ = LoadWardsAsync(value.code);
            }
        }
    }

    private ApiDistrict? _selectedDistrict;
    public ApiDistrict? SelectedDistrict
    {
        get => _selectedDistrict;
        set => SetProperty(ref _selectedDistrict, value);
    }

    private ApiWard? _selectedWard;
    public ApiWard? SelectedWard
    {
        get => _selectedWard;
        set
        {
            if (SetProperty(ref _selectedWard, value) && value != null && !_isProgrammaticAddressUpdate)
            {
                FormPhuongXa = value.name;
            }
        }
    }

    
    // Error properties
    private string _errorNgaySinh = string.Empty;
    public string ErrorNgaySinh { get => _errorNgaySinh; set => SetProperty(ref _errorNgaySinh, value); }
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

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }
    }
    public bool IsNotEditing => !IsEditing;
    
    public string? HinhAnh
    {
        get => _hinhAnh;
        set => SetProperty(ref _hinhAnh, value);
    }

    public bool HasAvatar => !string.IsNullOrEmpty(HinhAnh);

    public bool IsAvatarChanged
    {
        get => _isAvatarChanged;
        set => SetProperty(ref _isAvatarChanged, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ChangeAvatarCommand { get; }
    public ICommand SaveAvatarCommand { get; }
    public ICommand CancelAvatarChangeCommand { get; }
    public ICommand EditProfileCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand CancelEditProfileCommand { get; }

    public event Action? OnClose;
    public event Action? OnChangePassword;
    public event Action? OnLogout;

    public ProfileViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        _locationService = LocationService.Instance;
        
        CloseCommand = new RelayCommand(_ => OnClose?.Invoke());
        ChangePasswordCommand = new RelayCommand(_ => OnChangePassword?.Invoke());
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        ChangeAvatarCommand = new RelayCommand(_ => SelectAvatarAsync());
        SaveAvatarCommand = new AsyncRelayCommand(async _ => await SaveAvatarAsync(), _ => IsAvatarChanged);
        CancelAvatarChangeCommand = new RelayCommand(_ => CancelAvatarChange(), _ => IsAvatarChanged);
        EditProfileCommand = new RelayCommand(_ => StartEditing());
        CancelEditProfileCommand = new RelayCommand(_ =>
        {
            IsEditing = false;
            RefreshData();
        });
        SaveProfileCommand = new AsyncRelayCommand(async _ => await SaveProfileAsync());
        
        // Load current data
        _hinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        _email = CurrentUserSession.Instance.CurrentUser?.NhanVien?.Email;
        _sdt = CurrentUserSession.Instance.CurrentUser?.NhanVien?.SDT;
        _ngaySinh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NgaySinh;
        _diaChi = CurrentUserSession.Instance.CurrentUser?.NhanVien?.DiaChi;
    }

    private void StartEditing()
    {
        IsEditing = true;
        ParseDiaChi(DiaChi);
        _ = SetupAddressAsync(FormTinhTP, FormQuanHuyen, FormPhuongXa);
    }

    private void ParseDiaChi(string? diaChi)
    {
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;

        if (string.IsNullOrWhiteSpace(diaChi)) return;

        var parts = diaChi.Split(',').Select(p => p.Trim()).ToArray();
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
        else
        {
            FormDiaChiChiTiet = diaChi;
        }
    }

    private string ComposeDiaChi()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(FormDiaChiChiTiet)) parts.Add(FormDiaChiChiTiet.Trim());
        if (!string.IsNullOrWhiteSpace(FormThonXom)) parts.Add(FormThonXom.Trim());
        if (!string.IsNullOrWhiteSpace(FormPhuongXa)) parts.Add(FormPhuongXa.Trim());
        if (!string.IsNullOrWhiteSpace(FormTinhTP)) parts.Add(FormTinhTP.Trim());
        return string.Join(", ", parts);
    }

    private async Task SetupAddressAsync(string tinhTP, string quanHuyen, string phuongXa)
    {
        _isProgrammaticAddressUpdate = true;
        try
        {
            var provinces = await _locationService.GetProvincesAsync();
            Provinces.Clear();
            foreach (var p in provinces) Provinces.Add(p);

            if (!string.IsNullOrWhiteSpace(tinhTP))
            {
                var match = provinces.FirstOrDefault(p => p.name.Equals(tinhTP, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _selectedProvince = match;
                    OnPropertyChanged(nameof(SelectedProvince));

                    var wards = await _locationService.GetWardsAsync(match.code);
                    Wards.Clear();
                    foreach (var w in wards) Wards.Add(w);

                    if (!string.IsNullOrWhiteSpace(phuongXa))
                    {
                        var wMatch = wards.FirstOrDefault(w => w.name.Equals(phuongXa, StringComparison.OrdinalIgnoreCase));
                        if (wMatch != null)
                        {
                            _selectedWard = wMatch;
                            OnPropertyChanged(nameof(SelectedWard));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up address: {ex.Message}");
        }
        finally
        {
            _isProgrammaticAddressUpdate = false;
        }
    }

    private async Task LoadWardsAsync(string provinceCode)
    {
        try
        {
            Wards.Clear();
            _selectedWard = null;
            OnPropertyChanged(nameof(SelectedWard));

            var wards = await _locationService.GetWardsAsync(provinceCode);
            foreach (var w in wards) Wards.Add(w);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading wards: {ex.Message}");
        }
    }

    private void SelectAvatarAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Chọn ảnh đại diện",
            Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                _pendingAvatarSourceFile = openFileDialog.FileName;
                HinhAnh = _pendingAvatarSourceFile;
                IsAvatarChanged = true;
                OnPropertyChanged(nameof(HasAvatar));
                System.Diagnostics.Debug.WriteLine($"Avatar preview loaded: {_pendingAvatarSourceFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading avatar preview: {ex.Message}");
                MessageBox.Show("Không thể tải ảnh. Vui lòng thử lại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task SaveAvatarAsync()
    {
        if (string.IsNullOrEmpty(_pendingAvatarSourceFile))
            return;

        try
        {
            var sourceFile = _pendingAvatarSourceFile;
            var relativePath = Helpers.ImageStorageHelper.CopyImageToStorage(sourceFile, "NhanVien");

            var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
            if (nhanVien != null)
            {
                var success = await _databaseService.UpdateNhanVienAvatarAsync(nhanVien.NhanVienID, relativePath);
                
                if (success)
                {
                    nhanVien.HinhAnh = relativePath;
                    HinhAnh = relativePath;
                    IsAvatarChanged = false;
                    _pendingAvatarSourceFile = null;
                    OnPropertyChanged(nameof(HasAvatar));
                    MessageBox.Show("Cập nhật ảnh đại diện thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Debug.WriteLine($"Avatar updated successfully: {relativePath}");
                }
                else
                {
                    MessageBox.Show("Không thể lưu ảnh vào database!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("Failed to update avatar in database");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine($"Error saving avatar: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    
    private bool ValidateForm()
    {
        bool isValid = true;
        ErrorNgaySinh = string.Empty;
        ErrorSDT = string.Empty;
        ErrorTinhTP = string.Empty;
        ErrorPhuongXa = string.Empty;
        ErrorThonXom = string.Empty;
        ErrorDiaChiChiTiet = string.Empty;

        if (!NgaySinh.HasValue)
        {
            ErrorNgaySinh = "Vui lòng chọn ngày sinh";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(SDT))
        {
            ErrorSDT = "Vui lòng nhập số điện thoại";
            isValid = false;
        }
        else
        {
            var sdt = SDT.Trim();
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

    private async Task SaveProfileAsync()
    {
        if (!NgaySinh.HasValue)
        {
            MessageBox.Show("Vui lòng chọn ngày sinh.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SDT))
        {
            MessageBox.Show("Vui lòng nhập số điện thoại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(SDT))
        {
            var sdtStr = SDT.Trim();
            if (sdtStr.Length != 10 || !sdtStr.StartsWith("0") || !sdtStr.All(char.IsDigit))
            {
                MessageBox.Show("Số điện thoại phải có 10 chữ số và bắt đầu bằng 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(FormTinhTP) || string.IsNullOrWhiteSpace(FormPhuongXa) || string.IsNullOrWhiteSpace(FormThonXom) || string.IsNullOrWhiteSpace(FormDiaChiChiTiet))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ địa chỉ (Số nhà/Đường, Thôn/Xóm, Phường/Xã, Tỉnh/TP).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
            if (nhanVien != null)
            {
                nhanVien.Email = Email;
                nhanVien.SDT = SDT;
                nhanVien.NgaySinh = NgaySinh;
                nhanVien.DiaChi = ComposeDiaChi();
                
                var success = await _databaseService.SaveNhanVienAsync(nhanVien);
                if (success > 0)
                {
                    DiaChi = nhanVien.DiaChi;
                    IsEditing = false;
                    RefreshData();
                }
                else
                {
                    MessageBox.Show("Cập nhật thông tin thất bại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelAvatarChange()
    {
        HinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        IsAvatarChanged = false;
        _pendingAvatarSourceFile = null;
        
        OnPropertyChanged(nameof(HasAvatar));
        System.Diagnostics.Debug.WriteLine("Avatar change cancelled");
    }

    public void RefreshData()
    {
        _hinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        _email = CurrentUserSession.Instance.CurrentUser?.NhanVien?.Email;
        _sdt = CurrentUserSession.Instance.CurrentUser?.NhanVien?.SDT;
        _ngaySinh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NgaySinh;
        _diaChi = CurrentUserSession.Instance.CurrentUser?.NhanVien?.DiaChi;
        
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(HoTen));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(SDT));
        OnPropertyChanged(nameof(NgaySinh));
        OnPropertyChanged(nameof(DiaChi));
        OnPropertyChanged(nameof(ChucVu));
        OnPropertyChanged(nameof(HinhAnh));
        OnPropertyChanged(nameof(HasAvatar));
    }
}
