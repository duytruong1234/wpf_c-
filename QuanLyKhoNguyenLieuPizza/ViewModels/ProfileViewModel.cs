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
    public string? Email => CurrentUserSession.Instance.CurrentUser?.NhanVien?.Email;
    public string? SDT => CurrentUserSession.Instance.CurrentUser?.NhanVien?.SDT;
    public DateTime? NgaySinh => CurrentUserSession.Instance.CurrentUser?.NhanVien?.NgaySinh;
    public string? DiaChi => CurrentUserSession.Instance.CurrentUser?.NhanVien?.DiaChi;
    public string ChucVu => CurrentUserSession.Instance.CurrentUser?.NhanVien?.ChucVu?.TenChucVu ?? "Nhân viên";
    
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
        
        // T?i ?nh d?i di?n hi?n t?i
        _hinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
    }

    private void SelectAvatarAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Ch?n ?nh d?i di?n",
            Filter = "T?p hình ?nh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|T?t c? t?p (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Luu file ngu?n và hi?n th? xem tru?c
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
                MessageBox.Show("Không th? t?i ?nh. Vui lòng th? l?i!", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var fileName = $"avatar_{CurrentUserSession.Instance.CurrentUser?.NhanVien?.NhanVienID}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourceFile)}";
            var relativePath = Path.Combine("Resources", "Images", fileName);
            var destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

            // Ð?m b?o thu m?c t?n t?i
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Sao chép file vào Resources/Images
            File.Copy(sourceFile, destinationPath, true);

            // C?p nh?t co s? d? li?u
            var nhanVien = CurrentUserSession.Instance.CurrentUser?.NhanVien;
            if (nhanVien != null)
            {
                var success = await _databaseService.UpdateNhanVienAvatarAsync(nhanVien.NhanVienID, relativePath);
                
                if (success)
                {
                    // C?p nh?t phiên
                    nhanVien.HinhAnh = relativePath;
                    
                    // C?p nh?t giao di?n
                    HinhAnh = relativePath;
                    IsAvatarChanged = false;
                    _pendingAvatarSourceFile = null;
                    
                    OnPropertyChanged(nameof(HasAvatar));
                    
                    MessageBox.Show("C?p nh?t ?nh d?i di?n thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Debug.WriteLine($"Avatar updated successfully: {relativePath}");
                }
                else
                {
                    MessageBox.Show("Không th? luu ?nh vào database!", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private void CancelAvatarChange()
    {
        // Khôi ph?c ?nh d?i di?n g?c
        HinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        IsAvatarChanged = false;
        _pendingAvatarSourceFile = null;
        
        OnPropertyChanged(nameof(HasAvatar));
        System.Diagnostics.Debug.WriteLine("Avatar change cancelled");
    }

    public void RefreshData()
    {
        _hinhAnh = CurrentUserSession.Instance.CurrentUser?.NhanVien?.HinhAnh;
        
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


