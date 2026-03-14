using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Commands;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class VerifyInfoViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
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

    public event Action<string>? OnVerifySuccess; // Truyền email khi verify thành công
    public event Action? OnBack;

    public VerifyInfoViewModel()
    {
        try
        {
            _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        }
        catch
        {
            _databaseService = new DatabaseService();
        }

        VerifyCommand = new AsyncRelayCommand(ExecuteVerifyAsync, CanExecuteVerify);
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());

        _ = LoadChucVusAsync();
    }

    private async Task LoadChucVusAsync()
    {
        try 
        {
            var chucVus = await _databaseService.GetChucVusAsync();
            ChucVus.Clear();
            foreach (var cv in chucVus)
            {
                ChucVus.Add(cv);
            }
        }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Error loading ChucVus: {ex.Message}");
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
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin!";
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
                ErrorMessage = "Thông tin không chính xác!";
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

