using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class VerifyInfoViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private string _hoTen = string.Empty;
    private DateTime? _ngaySinh;
    private ChucVu? _selectedChucVu;
    private string _email = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string HoTen
    {
        get => _hoTen;
        set => SetProperty(ref _hoTen, value);
    }

    public DateTime? NgaySinh
    {
        get => _ngaySinh;
        set => SetProperty(ref _ngaySinh, value);
    }

    public ChucVu? SelectedChucVu
    {
        get => _selectedChucVu;
        set => SetProperty(ref _selectedChucVu, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<ChucVu> ChucVus { get; } = new();

    public ICommand VerifyCommand { get; }
    public ICommand BackCommand { get; }

    public event Action<string>? OnVerifySuccess; // Truy?n email khi verify th�nh c�ng
    public event Action? OnBack;

    public VerifyInfoViewModel()
    {
        try
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }
        catch
        {
            _databaseService = App.Services.GetRequiredService<DatabaseService>();
        }

        VerifyCommand = new AsyncRelayCommand(ExecuteVerifyAsync, CanExecuteVerify);
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());
    }

    /// <summary>
    /// G?i m?i khi navigate d?n view n�y d? load/reload danh s�ch ch?c v?
    /// </summary>
    public async Task ReloadChucVusAsync()
    {
        await LoadChucVusAsync();
    }

    private async Task LoadChucVusAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadChucVusAsync: Starting...");
            var chucVus = await _databaseService.GetChucVusAsync();
            System.Diagnostics.Debug.WriteLine($"LoadChucVusAsync: Got {chucVus.Count} items from DB");

            // �?m b?o c?p nh?t ObservableCollection tr�n UI thread
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChucVus.Clear();
                    foreach (var cv in chucVus)
                    {
                        ChucVus.Add(cv);
                    }
                });
            }
            else
            {
                ChucVus.Clear();
                foreach (var cv in chucVus)
                {
                    ChucVus.Add(cv);
                }
            }

            System.Diagnostics.Debug.WriteLine($"LoadChucVusAsync: ChucVus.Count = {ChucVus.Count}");

            if (ChucVus.Count == 0)
            {
                ErrorMessage = "Chua c� d? li?u ch?c v?. Vui l�ng th�m ch?c v? trong m?c Qu?n l� nh�n vi�n.";
            }
            else
            {
                ErrorMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Kh�ng t?i du?c danh s�ch ch?c v?. Vui l�ng ki?m tra k?t n?i co s? d? li?u.";
            System.Diagnostics.Debug.WriteLine($"Error loading ChucVus: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error loading ChucVus Stack: {ex.StackTrace}");
        }
    }

    private bool CanExecuteVerify()
    {
        return !string.IsNullOrWhiteSpace(HoTen) &&
               NgaySinh.HasValue &&
               SelectedChucVu != null &&
               !string.IsNullOrWhiteSpace(Email) &&
               !IsLoading;
    }

    private async Task ExecuteVerifyAsync()
    {
        System.Diagnostics.Debug.WriteLine($"=== ExecuteVerify called ===");

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            if (!NgaySinh.HasValue || SelectedChucVu == null)
            {
                ErrorMessage = "Vui l�ng nh?p d?y d? th�ng tin!";
                return;
            }

            var user = await _databaseService.VerifyUserInfoAsync(Email, HoTen, NgaySinh.Value, SelectedChucVu.ChucVuID);

            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine($"Invoking OnVerifySuccess with email: {Email}");
                OnVerifySuccess?.Invoke(Email);
            }
            else
            {
                ErrorMessage = "Th�ng tin kh�ng ch�nh x�c!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"ExecuteVerify Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

