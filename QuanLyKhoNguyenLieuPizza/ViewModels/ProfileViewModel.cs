using System.IO;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class ProfileViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private string? _hinhAnh;
    private string? _pendingAvatarSourceFile;
    private bool _isAvatarChanged;

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
        _databaseService = new DatabaseService();
        
        CloseCommand = new RelayCommand(_ => OnClose?.Invoke());
        ChangePasswordCommand = new RelayCommand(_ => OnChangePassword?.Invoke());
        LogoutCommand = new RelayCommand(_ => OnLogout?.Invoke());
        ChangeAvatarCommand = new RelayCommand(_ => SelectAvatarAsync());
        SaveAvatarCommand = new AsyncRelayCommand(async _ => await SaveAvatarAsync(), _ => IsAvatarChanged);
        CancelAvatarChangeCommand = new RelayCommand(_ => CancelAvatarChange(), _ => IsAvatarChanged);
        EditProfileCommand = new RelayCommand(_ => IsEditing = true);
        CancelEditProfileCommand = new RelayCommand(_ =>
        {
            IsEditing = false;
            RefreshData();
        });
        SaveProfileCommand = new AsyncRelayCommand(async _ => await SaveProfileAsync());
        
        // T?i ?nh d?i di?n hi?n t?i
        _hinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        _email = CurrentUserSession.Instance.CurrentUser?.NhanVien?.Email;
        _sdt = CurrentUserSession.Instance.CurrentUser?.NhanVien?.SDT;
        _ngaySinh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.NgaySinh;
        _diaChi = CurrentUserSession.Instance.CurrentUser?.NhanVien?.DiaChi;
    }

    private void SelectAvatarAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Ch?n ?nh d?i di?n",
            Filter = "T?p h�nh ?nh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|T?t c? t?p (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Luu file ngu?n v� hi?n th? xem tru?c
                _pendingAvatarSourceFile = openFileDialog.FileName;
                
                // Hi?n th? xem tru?c ngay l?p t?c b?ng file ngu?n
                HinhAnh = _pendingAvatarSourceFile;
                IsAvatarChanged = true;
                
                OnPropertyChanged(nameof(HasAvatar));
                
                System.Diagnostics.Debug.WriteLine($"Avatar preview loaded: {_pendingAvatarSourceFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading avatar preview: {ex.Message}");
                MessageBox.Show("Kh�ng th? t?i ?nh. Vui l�ng th? l?i!", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // C?p nh?t co s? d? li?u
            var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
            if (nhanVien != null)
            {
                var success = await _databaseService.UpdateNhanVienAvatarAsync(nhanVien.NhanVienID, relativePath);
                
                if (success)
                {
                    // C?p nh?t phi�n
                    nhanVien.HinhAnh = relativePath;
                    
                    // C?p nh?t giao di?n
                    HinhAnh = relativePath;
                    IsAvatarChanged = false;
                    _pendingAvatarSourceFile = null;
                    
                    OnPropertyChanged(nameof(HasAvatar));
                    
                    MessageBox.Show("C?p nh?t ?nh d?i di?n th�nh c�ng!", "Th�nh c�ng", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Debug.WriteLine($"Avatar updated successfully: {relativePath}");
                }
                else
                {
                    MessageBox.Show("Kh�ng th? luu ?nh v�o database!", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("Failed to update avatar in database");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L?i khi luu ?nh: {ex.Message}", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine($"Error saving avatar: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task SaveProfileAsync()
    {
        try
        {
            var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
            if (nhanVien != null)
            {
                nhanVien.Email = Email;
                nhanVien.SDT = SDT;
                nhanVien.NgaySinh = NgaySinh;
                nhanVien.DiaChi = DiaChi;
                
                var success = await _databaseService.SaveNhanVienAsync(nhanVien);
                if (success > 0)
                {
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
        // Kh�i ph?c ?nh d?i di?n g?c
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





