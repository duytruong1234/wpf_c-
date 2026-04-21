using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class NhaCungCapViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    #region Thuộc tính
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

    // Thuộc tính lọc
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
    private bool _isUpdatingFilter;
    public bool FilterDangHopTac
    {
        get => _filterDangHopTac;
        set
        {
            if (SetProperty(ref _filterDangHopTac, value))
            {
                if (!_isUpdatingFilter && value)
                {
                    _isUpdatingFilter = true;
                    FilterNgungHopTac = false;
                    _isUpdatingFilter = false;
                }
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
                if (!_isUpdatingFilter && value)
                {
                    _isUpdatingFilter = true;
                    FilterDangHopTac = false;
                    _isUpdatingFilter = false;
                }
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

    // Thuộc tính hộp thoại
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

    // Thuộc tính form
    private string _formTenNCC = string.Empty;
    public string FormTenNCC
    {
        get => _formTenNCC;
        set
        {
            if (SetProperty(ref _formTenNCC, value))
                ErrorTenNCC = string.Empty;
        }
    }

    private string _formDiaChi = string.Empty;
    public string FormDiaChi
    {
        get => _formDiaChi;
        set => SetProperty(ref _formDiaChi, value);
    }

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
                ErrorTinhTP = string.Empty;
                if (value != null) FormTinhTP = value.name;
                
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
                                Wards = new ObservableCollection<ApiWard>(wards);
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
                ErrorPhuongXa = string.Empty;
                if (value != null) FormPhuongXa = value.name;
            }
        }
    }

    // Trường địa chỉ có cấu trúc
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
        set
        {
            if (SetProperty(ref _formDiaChiChiTiet, value))
                ErrorDiaChiChiTiet = string.Empty;
        }
    }

    private string _formThonXom = string.Empty;
    public string FormThonXom
    {
        get => _formThonXom;
        set
        {
            if (SetProperty(ref _formThonXom, value))
                ErrorThonXom = string.Empty;
        }
    }

    private string _formSDT = string.Empty;
    public string FormSDT
    {
        get => _formSDT;
        set
        {
            if (SetProperty(ref _formSDT, value))
                ErrorSDT = string.Empty;
        }
    }

    private string _formEmail = string.Empty;
    public string FormEmail
    {
        get => _formEmail;
        set
        {
            if (SetProperty(ref _formEmail, value))
                ErrorEmail = string.Empty;
        }
    }

    private string _errorSDT = string.Empty;
    public string ErrorSDT
    {
        get => _errorSDT;
        set => SetProperty(ref _errorSDT, value);
    }

    private string _errorEmail = string.Empty;
    public string ErrorEmail
    {
        get => _errorEmail;
        set => SetProperty(ref _errorEmail, value);
    }

    private string _errorTenNCC = string.Empty;
    public string ErrorTenNCC
    {
        get => _errorTenNCC;
        set => SetProperty(ref _errorTenNCC, value);
    }

    private string _errorDiaChiChiTiet = string.Empty;
    public string ErrorDiaChiChiTiet
    {
        get => _errorDiaChiChiTiet;
        set => SetProperty(ref _errorDiaChiChiTiet, value);
    }

    private string _errorThonXom = string.Empty;
    public string ErrorThonXom
    {
        get => _errorThonXom;
        set => SetProperty(ref _errorThonXom, value);
    }

    private string _errorTinhTP = string.Empty;
    public string ErrorTinhTP
    {
        get => _errorTinhTP;
        set => SetProperty(ref _errorTinhTP, value);
    }

    private string _errorPhuongXa = string.Empty;
    public string ErrorPhuongXa
    {
        get => _errorPhuongXa;
        set => SetProperty(ref _errorPhuongXa, value);
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

    // Hộp thoại thêm NL vào NCC
    private bool _isAddNLDialogOpen;
    public bool IsAddNLDialogOpen
    {
        get => _isAddNLDialogOpen;
        set => SetProperty(ref _isAddNLDialogOpen, value);
    }

    private bool _isEditingNL;
    public bool IsEditingNL
    {
        get => _isEditingNL;
        set => SetProperty(ref _isEditingNL, value);
    }

    private ObservableCollection<NguyenLieu> _allNguyenLieus = new();
    public ObservableCollection<NguyenLieu> AllNguyenLieus
    {
        get => _allNguyenLieus;
        set => SetProperty(ref _allNguyenLieus, value);
    }

    private NguyenLieu? _selectedNguyenLieuToAdd;
    public NguyenLieu? SelectedNguyenLieuToAdd
    {
        get => _selectedNguyenLieuToAdd;
        set
        {
            if (SetProperty(ref _selectedNguyenLieuToAdd, value))
            {
                if (value != null && value.DonViID.HasValue)
                {
                    FormDonViNhap = DonViTinhs.FirstOrDefault(d => d.DonViID == value.DonViID.Value);
                }
                else
                {
                    FormDonViNhap = null;
                }
            }
        }
    }

    private string _formGiaNhap = string.Empty;
    public string FormGiaNhap
    {
        get => _formGiaNhap;
        set => SetProperty(ref _formGiaNhap, value);
    }

    private ObservableCollection<DonViTinh> _donViTinhs = new();
    public ObservableCollection<DonViTinh> DonViTinhs
    {
        get => _donViTinhs;
        set => SetProperty(ref _donViTinhs, value);
    }

    private DonViTinh? _formDonViNhap;
    public DonViTinh? FormDonViNhap
    {
        get => _formDonViNhap;
        set => SetProperty(ref _formDonViNhap, value);
    }
    #endregion

    #region Lệnh
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
    public ICommand OpenAddNLDialogCommand { get; }
    public ICommand CloseAddNLDialogCommand { get; }
    public ICommand SaveAddNLCommand { get; }
    public ICommand DeleteNLFromNCCCommand { get; }
    public ICommand EditNLPriceCommand { get; }
    public ICommand ComposeDiaChiCommand { get; }
    public ICommand ResetDiaChiCommand { get; }
    #endregion

    public NhaCungCapViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        CreateNhaCungCapCommand = new RelayCommand(_ => OpenCreateDialog());
        ViewDetailCommand = new AsyncRelayCommand(async p => await ViewDetailAsync(p));
        EditNhaCungCapCommand = new RelayCommand(p => OpenEditDialog(p));
        DeleteNhaCungCapCommand = new AsyncRelayCommand(async p => await DeleteNhaCungCapAsync(p));
        OpenStatusDialogCommand = new RelayCommand(p => OpenStatusDialog(p));
        ToggleStatusCommand = new AsyncRelayCommand(async _ => await ToggleStatusAsync());
        SaveNhaCungCapCommand = new AsyncRelayCommand(async _ => await SaveNhaCungCapAsync());
        CancelDialogCommand = new RelayCommand(_ => CloseDialog());
        CloseDetailDialogCommand = new RelayCommand(_ => IsDetailDialogOpen = false);
        CloseStatusDialogCommand = new RelayCommand(_ => IsStatusDialogOpen = false);
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        OpenAddNLDialogCommand = new AsyncRelayCommand(async _ => await OpenAddNLDialogAsync());
        CloseAddNLDialogCommand = new RelayCommand(_ => IsAddNLDialogOpen = false);
        SaveAddNLCommand = new AsyncRelayCommand(async _ => await SaveAddNLAsync());
        DeleteNLFromNCCCommand = new AsyncRelayCommand(async p => await DeleteNLFromNCCAsync(p));
        EditNLPriceCommand = new AsyncRelayCommand(async p => await OpenEditNLDialogAsync(p));
        ComposeDiaChiCommand = new RelayCommand(_ => ComposeDiaChi());
        ResetDiaChiCommand = new RelayCommand(_ => ResetDiaChi());

        // Tải dữ liệu khi khởi tạo
        SafeInitializeAsync(LoadDataAsync);
    }

    #region Phương thức
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
            // Nếu cả hai được chọn hoặc cả hai không được chọn, hiển thị tất cả

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
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;
        FormSDT = string.Empty;
        _ = SetupAddressAsync("", "", "");
        FormEmail = string.Empty;
        ErrorSDT = string.Empty;
        ErrorEmail = string.Empty;
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

            // Phân tích địa chỉ có cấu trúc từ Địa chỉ
            ParseDiaChi(ncc.DiaChi);
            _ = SetupAddressAsync(FormTinhTP, FormQuanHuyen, FormPhuongXa);

            IsDialogOpen = true;
        }
    }


    private async Task SetupAddressAsync(string provinceName, string districtName, string wardName)
    {
        _isProgrammaticAddressUpdate = true;
        
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
        
        _isProgrammaticAddressUpdate = false;
    }

    private void ParseDiaChi(string? diaChi)
    {
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;

        if (string.IsNullOrWhiteSpace(diaChi)) return;

        // Phân tích: "Địa chỉ chi tiết, Thôn/Xóm, Phường/Xã, Quận/Huyện, Tỉnh/TP"
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
            FormPhuongXa = parts[1];
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

    private void ComposeDiaChi()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(FormDiaChiChiTiet)) parts.Add(FormDiaChiChiTiet.Trim());
        if (!string.IsNullOrWhiteSpace(FormThonXom)) parts.Add(FormThonXom.Trim());
        if (!string.IsNullOrWhiteSpace(FormPhuongXa)) parts.Add(FormPhuongXa.Trim());
        if (!string.IsNullOrWhiteSpace(FormTinhTP)) parts.Add(FormTinhTP.Trim());
        FormDiaChi = string.Join(", ", parts);
    }

    private void ResetDiaChi()
    {
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;
        FormDiaChi = string.Empty;
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

    private bool ValidateForm()
    {
        bool isValid = true;

        ErrorSDT = string.Empty;
        ErrorEmail = string.Empty;
        ErrorTenNCC = string.Empty;
        ErrorDiaChiChiTiet = string.Empty;
        ErrorThonXom = string.Empty;
        ErrorTinhTP = string.Empty;
        ErrorPhuongXa = string.Empty;

        if (string.IsNullOrWhiteSpace(FormTenNCC))
        {
            ErrorTenNCC = "Vui lòng nhập tên nhà cung cấp";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(FormDiaChiChiTiet))
        {
            ErrorDiaChiChiTiet = "Vui lòng nhập số nhà, đường";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(FormThonXom))
        {
            ErrorThonXom = "Vui lòng nhập thôn xóm";
            isValid = false;
        }

        if (SelectedProvince == null)
        {
            ErrorTinhTP = "Vui lòng chọn tỉnh thành phố";
            isValid = false;
        }

        if (SelectedWard == null)
        {
            ErrorPhuongXa = "Vui lòng chọn phường xã";
            isValid = false;
        }

        // Kiểm tra SĐT
        if (string.IsNullOrWhiteSpace(FormSDT))
        {
            ErrorSDT = "Vui lòng nhập số điện thoại";
            isValid = false;
        }
        else
        {
            var sdt = FormSDT.Trim();
            if (!Regex.IsMatch(sdt, @"^\d+$"))
            {
                ErrorSDT = "Số điện thoại chỉ được chứa chữ số";
                isValid = false;
            }
            else if (sdt.Length < 10 || sdt.Length > 11)
            {
                ErrorSDT = "Số điện thoại phải có 10-11 chữ số";
                isValid = false;
            }
        }

        // Kiểm tra Email
        if (string.IsNullOrWhiteSpace(FormEmail))
        {
            ErrorEmail = "Vui lòng nhập email";
            isValid = false;
        }
        else
        {
            var email = FormEmail.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorEmail = "Email không đúng định dạng";
                isValid = false;
            }
        }

        return isValid;
    }

    private async Task SaveNhaCungCapAsync()
    {
        ComposeDiaChi();

        if (!ValidateForm())
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

        var confirmed = await ShowDeleteConfirmation(
            ncc.TenNCC ?? "",
            "Xóa nhà cung cấp",
            $"Bạn có chắc chắn muốn xóa nhà cung cấp \"{ncc.TenNCC}\"?\nTất cả dữ liệu liên quan sẽ bị xóa theo.");
        if (!confirmed) return;

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
        FormTinhTP = string.Empty;
        FormPhuongXa = string.Empty;
        FormQuanHuyen = string.Empty;
        FormDiaChiChiTiet = string.Empty;
        FormThonXom = string.Empty;
        FormSDT = string.Empty;
        _ = SetupAddressAsync("", "", "");
        FormEmail = string.Empty;
        ErrorSDT = string.Empty;
        ErrorEmail = string.Empty;
        SelectedNhaCungCap = null;
    }

    private async Task OpenAddNLDialogAsync()
    {
        try
        {
            var allNL = await _databaseService.GetNguyenLieusAsync();
            AllNguyenLieus = new ObservableCollection<NguyenLieu>(allNL);

            var dvtList = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs = new ObservableCollection<DonViTinh>(dvtList);

            SelectedNguyenLieuToAdd = null;
            FormGiaNhap = string.Empty;
            FormDonViNhap = null;
            IsEditingNL = false;
            IsAddNLDialogOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieus: {ex.Message}");
        }
    }

    private async Task OpenEditNLDialogAsync(object? parameter)
    {
        if (parameter is not NguyenLieu nl) return;

        try
        {
            var allNL = await _databaseService.GetNguyenLieusAsync();
            AllNguyenLieus = new ObservableCollection<NguyenLieu>(allNL);

            var dvtList = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs = new ObservableCollection<DonViTinh>(dvtList);

            SelectedNguyenLieuToAdd = AllNguyenLieus.FirstOrDefault(x => x.NguyenLieuID == nl.NguyenLieuID);
            
            var giaNhap = nl.NguyenLieuNhaCungCaps?.FirstOrDefault()?.GiaNhap ?? 0;
            FormGiaNhap = giaNhap.ToString("G");
            
            IsEditingNL = true;
            IsAddNLDialogOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NguyenLieus: {ex.Message}");
        }
    }

    private async Task SaveAddNLAsync()
    {
        if (SelectedNhaCungCap == null || SelectedNguyenLieuToAdd == null)
            return;

        if (!decimal.TryParse(FormGiaNhap, out var giaNhap) || giaNhap < 0)
            return;

        try
        {
            var success = await _databaseService.AddNguyenLieuToNhaCungCapAsync(
                SelectedNguyenLieuToAdd.NguyenLieuID,
                SelectedNhaCungCap.NhaCungCapID,
                giaNhap);

            if (success)
            {
                IsAddNLDialogOpen = false;
                // Tải lại danh sách chi tiết
                var nguyenLieus = await _databaseService.GetNguyenLieusByNhaCungCapIdAsync(SelectedNhaCungCap.NhaCungCapID);
                NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>(nguyenLieus);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding NL to NCC: {ex.Message}");
        }
    }

    private async Task DeleteNLFromNCCAsync(object? parameter)
    {
        if (SelectedNhaCungCap == null) return;
        if (parameter is not NguyenLieu nl) return;

        var confirmed = await ShowDeleteConfirmation(
            nl.TenNguyenLieu,
            "Xóa nguyên liệu",
            $"Bạn có chắc chắn muốn xóa \"{nl.TenNguyenLieu}\" khỏi nhà cung cấp \"{SelectedNhaCungCap.TenNCC}\"?");
        if (!confirmed) return;

        try
        {
            var success = await _databaseService.DeleteNguyenLieuFromNhaCungCapAsync(
                nl.NguyenLieuID, SelectedNhaCungCap.NhaCungCapID);

            if (success)
            {
                var nguyenLieus = await _databaseService.GetNguyenLieusByNhaCungCapIdAsync(SelectedNhaCungCap.NhaCungCapID);
                NguyenLieusOfNCC = new ObservableCollection<NguyenLieu>(nguyenLieus);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting NL from NCC: {ex.Message}");
        }
    }
    #endregion
}


