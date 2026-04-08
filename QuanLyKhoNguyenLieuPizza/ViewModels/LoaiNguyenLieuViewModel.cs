using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class LoaiNguyenLieuItemViewModel : BaseViewModel
{
    public int LoaiNLID { get; set; }
    public string TenLoai { get; set; } = string.Empty;
    
    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
}

public class LoaiNguyenLieuViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    
    private ObservableCollection<LoaiNguyenLieuItemViewModel> _loaiNguyenLieus = new();
    private bool _isLoading;
    
    // Popup thêm/s?a
    private bool _isAddEditPopupOpen;
    private bool _isEditing;
    private LoaiNguyenLieuItemViewModel? _selectedLoaiNguyenLieu;
    
    // Tru?ng form
    private string _formTenLoai = string.Empty;
    
    public event Action? OnBack;
    public event Action? OnDataChanged;
    
    public ObservableCollection<LoaiNguyenLieuItemViewModel> LoaiNguyenLieus
    {
        get => _loaiNguyenLieus;
        set => SetProperty(ref _loaiNguyenLieus, value);
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
    
    public LoaiNguyenLieuItemViewModel? SelectedLoaiNguyenLieu
    {
        get => _selectedLoaiNguyenLieu;
        set => SetProperty(ref _selectedLoaiNguyenLieu, value);
    }
    
    public string FormTenLoai
    {
        get => _formTenLoai;
        set => SetProperty(ref _formTenLoai, value);
    }
    
    // L?nh
    public ICommand LoadDataCommand { get; }
    public ICommand OpenAddPopupCommand { get; }
    public ICommand ClosePopupCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }
    
    public LoaiNguyenLieuViewModel()
    {
        _databaseService = ServiceLocator.Instance.GetService<IDatabaseService>();
        
        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        OpenAddPopupCommand = new RelayCommand(_ => OpenAddPopup());
        ClosePopupCommand = new RelayCommand(_ => ClosePopup());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
        BackCommand = new RelayCommand(_ => OnBack?.Invoke());
        
        _ = LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        
        try
        {
            var list = await _databaseService.GetLoaiNguyenLieusAsync();
            LoaiNguyenLieus.Clear();
            
            foreach (var item in list)
            {
                var vm = new LoaiNguyenLieuItemViewModel
                {
                    LoaiNLID = item.LoaiNLID,
                    TenLoai = item.TenLoai
                };
                vm.EditCommand = new RelayCommand(p => EditLoaiNguyenLieu(vm));
                vm.DeleteCommand = new AsyncRelayCommand(async p => await DeleteLoaiNguyenLieuAsync(vm));
                LoaiNguyenLieus.Add(vm);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading LoaiNguyenLieu: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void OpenAddPopup()
    {
        IsEditing = false;
        SelectedLoaiNguyenLieu = null;
        FormTenLoai = string.Empty;
        IsAddEditPopupOpen = true;
    }
    
    private void EditLoaiNguyenLieu(LoaiNguyenLieuItemViewModel item)
    {
        IsEditing = true;
        SelectedLoaiNguyenLieu = item;
        FormTenLoai = item.TenLoai;
        IsAddEditPopupOpen = true;
    }
    
    private void ClosePopup()
    {
        IsAddEditPopupOpen = false;
        FormTenLoai = string.Empty;
    }
    
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTenLoai))
        {
            return;
        }
        
        var loaiNguyenLieu = new LoaiNguyenLieu
        {
            LoaiNLID = IsEditing ? SelectedLoaiNguyenLieu?.LoaiNLID ?? 0 : 0,
            TenLoai = FormTenLoai
        };
        
        var success = await _databaseService.SaveLoaiNguyenLieuAsync(loaiNguyenLieu);
        
        if (success)
        {
            ClosePopup();
            await LoadDataAsync();
            OnDataChanged?.Invoke();
        }
    }
    
    private async Task DeleteLoaiNguyenLieuAsync(LoaiNguyenLieuItemViewModel item)
    {
        var success = await _databaseService.DeleteLoaiNguyenLieuAsync(item.LoaiNLID);
        
        if (success)
        {
            await LoadDataAsync();
            OnDataChanged?.Invoke();
        }
    }
}

