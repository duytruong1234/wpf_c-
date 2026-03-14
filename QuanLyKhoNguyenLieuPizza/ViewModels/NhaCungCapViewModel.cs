using System.Collections.ObjectModel;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class NhaCungCapViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    #region Properties
    private ObservableCollection<NhaCungCap> _nhaCungCaps = new();
    public ObservableCollection<NhaCungCap> NhaCungCaps
    {
        get => _nhaCungCaps;
        set => SetProperty(ref _nhaCungCaps, value);
    }

    private NhaCungCap? _selectedNhaCungCap;
    public NhaCungCap? SelectedNhaCungCap
    {
        get => _selectedNhaCungCap;
        set => SetProperty(ref _selectedNhaCungCap, value);
    }

    private ObservableCollection<NguyenLieu> _nguyenLieusOfNCC = new();
    public ObservableCollection<NguyenLieu> NguyenLieusOfNCC
    {
        get => _nguyenLieusOfNCC;
        set => SetProperty(ref _nguyenLieusOfNCC, value);
    }

    // Filter properties
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadNhaCungCapsAsync();
            }
        }
    }

    private bool _filterDangHopTac = true;
    public bool FilterDangHopTac
    {
        get => _filterDangHopTac;
        set
        {
            if (SetProperty(ref _filterDangHopTac, value))
            {
                _ = LoadNhaCungCapsAsync();
            }
        }
    }

    private bool _filterNgungHopTac;
    public bool FilterNgungHopTac
    {
        get => _filterNgungHopTac;
        set
        {
            if (SetProperty(ref _filterNgungHopTac, value))
            {
                _ = LoadNhaCungCapsAsync();
            }
        }
    }

    private int _tongSo;
    public int TongSo
    {
        get => _tongSo;
        set => SetProperty(ref _tongSo, value);
    }

    // Dialog properties
    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    private bool _isDetailDialogOpen;
    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    private bool _isStatusDialogOpen;
    public bool IsStatusDialogOpen
    {
        get => _isStatusDialogOpen;
        set => SetProperty(ref _isStatusDialogOpen, value);
    }

    private bool _isCreateMode;
    public bool IsCreateMode
    {
        get => _isCreateMode;
        set => SetProperty(ref _isCreateMode, value);
    }

    // Form properties
    private string _formTenNCC = string.Empty;
    public string FormTenNCC
    {
        get => _formTenNCC;
        set => SetProperty(ref _formTenNCC, value);
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

    private int _countDangHopTac;
    public int CountDangHopTac
    {
        get => _countDangHopTac;
        set => SetProperty(ref _countDangHopTac, value);
    }

    private int _countNgungHopTac;
    public int CountNgungHopTac
    {
        get => _countNgungHopTac;
        set => SetProperty(ref _countNgungHopTac, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region Commands
    public ICommand LoadDataCommand { get; }
    public ICommand CreateNhaCungCapCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand EditNhaCungCapCommand { get; }
    public ICommand DeleteNhaCungCapCommand { get; }
    public ICommand OpenStatusDialogCommand { get; }
    public ICommand ToggleStatusCommand { get; }
    public ICommand SaveNhaCungCapCommand { get; }
    public ICommand CancelDialogCommand { get; }
    public ICommand CloseDetailDialogCommand { get; }
    public ICommand CloseStatusDialogCommand { get; }
    public ICommand ClearFilterCommand { get; }
    #endregion

    public NhaCungCapViewModel()
    {
        _databaseService = new DatabaseService();

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        CreateNhaCungCapCommand = new RelayCommand(_ => OpenCreateDialog());
        ViewDetailCommand = new RelayCommand(async p => await ViewDetailAsync(p));
        EditNhaCungCapCommand = new RelayCommand(p => OpenEditDialog(p));
        DeleteNhaCungCapCommand = new RelayCommand(async p => await DeleteNhaCungCapAsync(p));
        OpenStatusDialogCommand = new RelayCommand(p => OpenStatusDialog(p));
        ToggleStatusCommand = new RelayCommand(async _ => await ToggleStatusAsync());
        SaveNhaCungCapCommand = new RelayCommand(async _ => await SaveNhaCungCapAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        CloseDetailDialogCommand = new RelayCommand(_ => IsDetailDialogOpen = false);
        CloseStatusDialogCommand = new RelayCommand(_ => IsStatusDialogOpen = false);
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());

        // Load data on initialization
        _ = LoadDataAsync();
    }

    #region Methods
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadNhaCungCapsAsync();
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

    private async Task LoadNhaCungCapsAsync()
    {
        try
        {
            bool? trangThaiFilter = null;
            
            if (FilterDangHopTac && !FilterNgungHopTac)
            {
                trangThaiFilter = true;
            }
            else if (!FilterDangHopTac && FilterNgungHopTac)
            {
                trangThaiFilter = false;
            }
            // If both checked or both unchecked, show all

            var nhaCungCaps = await _databaseService.GetAllNhaCungCapsAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                trangThaiFilter);

            NhaCungCaps = new ObservableCollection<NhaCungCap>(nhaCungCaps);
            TongSo = nhaCungCaps.Count;
            CountDangHopTac = nhaCungCaps.Count(n => n.TrangThai);
            CountNgungHopTac = nhaCungCaps.Count(n => !n.TrangThai);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NhaCungCaps: {ex.Message}");
        }
    }

    private void ClearFilter()
    {
        SearchText = string.Empty;
        FilterDangHopTac = true;
        FilterNgungHopTac = false;
    }

    private void OpenCreateDialog()
    {
        IsCreateMode = true;
        FormTenNCC = string.Empty;
        FormDiaChi = string.Empty;
        FormSDT = string.Empty;
        FormEmail = string.Empty;
        IsDialogOpen = true;
    }

    private void OpenEditDialog(object? parameter)
    {
        if (parameter is NhaCungCap ncc)
        {
            SelectedNhaCungCap = ncc;
            IsCreateMode = false;
            FormTenNCC = ncc.TenNCC;
            FormDiaChi = ncc.DiaChi ?? string.Empty;
            FormSDT = ncc.SDT ?? string.Empty;
            FormEmail = ncc.Email ?? string.Empty;
            IsDialogOpen = true;
        }
    }

    private void OpenStatusDialog(object? parameter)
    {
        if (parameter is NhaCungCap ncc)
        {
            SelectedNhaCungCap = ncc;
            IsStatusDialogOpen = true;
        }
    }

    private async Task ViewDetailAsync(object? parameter)
    {
        int nhaCungCapId = 0;
        
        if (parameter is NhaCungCap ncc)
        {
            nhaCungCapId = ncc.NhaCungCapID;
            SelectedNhaCungCap = ncc;
        }
        else if (parameter is int id)
        {
            nhaCungCapId = id;
            SelectedNhaCungCap = await _databaseService.GetNhaCungCapByIdAsync(nhaCungCapId);
        }

        if (nhaCungCapId == 0) return;

        try
        {
            var nguyenLieus = await _databaseService.GetNguyenLieusByNhaCungCapIdAsync(nhaCungCapId);
            NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>(nguyenLieus);
            IsDetailDialogOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading detail: {ex.Message}");
        }
    }

    private async Task SaveNhaCungCapAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTenNCC))
        {
            return;
        }

        try
        {
            var nhaCungCap = new NhaCungCap
            {
                TenNCC = FormTenNCC.Trim(),
                DiaChi = string.IsNullOrWhiteSpace(FormDiaChi) ? null : FormDiaChi.Trim(),
                SDT = string.IsNullOrWhiteSpace(FormSDT) ? null : FormSDT.Trim(),
                Email = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail.Trim(),
                TrangThai = true // Mặc định "Đang hợp tác"
            };

            if (!IsCreateMode && SelectedNhaCungCap != null)
            {
                nhaCungCap.NhaCungCapID = SelectedNhaCungCap.NhaCungCapID;
                nhaCungCap.TrangThai = SelectedNhaCungCap.TrangThai;
            }

            await _databaseService.SaveNhaCungCapAsync(nhaCungCap);
            
            CloseDialog();
            await LoadNhaCungCapsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NhaCungCap: {ex.Message}");
        }
    }

    private async Task ToggleStatusAsync()
    {
        if (SelectedNhaCungCap == null) return;

        try
        {
            var newStatus = !SelectedNhaCungCap.TrangThai;
            var result = await _databaseService.UpdateNhaCungCapTrangThaiAsync(SelectedNhaCungCap.NhaCungCapID, newStatus);
            
            if (result)
            {
                IsStatusDialogOpen = false;
                await LoadNhaCungCapsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling status: {ex.Message}");
        }
    }

    private async Task DeleteNhaCungCapAsync(object? parameter)
    {
        if (parameter is not NhaCungCap ncc) return;

        try
        {
            var result = await _databaseService.DeleteNhaCungCapAsync(ncc.NhaCungCapID);
            if (result)
            {
                await LoadNhaCungCapsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting NhaCungCap: {ex.Message}");
        }
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        FormTenNCC = string.Empty;
        FormDiaChi = string.Empty;
        FormSDT = string.Empty;
        FormEmail = string.Empty;
        SelectedNhaCungCap = null;
    }
    #endregion
}


