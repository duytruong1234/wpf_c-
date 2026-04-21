using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

/// <summary>
/// Item hiển thị trong lưới chọn nguyên liệu (bên trái popup)
/// </summary>
public class QuyDoiNguyenLieuItem : BaseViewModel
{
    public int NguyenLieuID { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public string? LoaiNguyenLieu { get; set; }
    public string? DonViChinh { get; set; }
    public int? DonViID { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// Item hiển thị trong bảng hệ số quy đổi (bên phải popup)
/// </summary>
public class QuyDoiRowItem : BaseViewModel
{
    public int QuyDoiID { get; set; }
    public int? NguyenLieuID { get; set; }
    public int? DonViID { get; set; }
    public string TenDonVi { get; set; } = string.Empty;

    public Action<QuyDoiRowItem>? OnDonViChuanChanged { get; set; }

    private string _heSoText = "1";
    public string HeSoText
    {
        get => _heSoText;
        set => SetProperty(ref _heSoText, value);
    }

    private bool _laDonViChuan;
    public bool LaDonViChuan
    {
        get => _laDonViChuan;
        set 
        {
            if (SetProperty(ref _laDonViChuan, value))
            {
                OnPropertyChanged(nameof(LoaiDonVi));
                if (value)
                {
                    HeSoText = "1";
                    OnDonViChuanChanged?.Invoke(this);
                }
            }
        }
    }

    /// <summary>
    /// Nhãn loại đơn vị: "ĐV Chuẩn" hoặc "Quy đổi"
    /// Đơn vị chuẩn (LaDonViChuan=true) = đơn vị lưu tồn kho
    /// Các đơn vị khác = đơn vị quy đổi (dùng khi nhập/xuất/bán hàng, sẽ quy về ĐV chuẩn)
    /// </summary>
    public string LoaiDonVi => LaDonViChuan ? "📦 ĐV Chuẩn" : "🔄 Quy đổi";

    public ICommand? DeleteCommand { get; set; }
    
    private bool _isBaseUnit;
    public bool IsBaseUnit
    {
        get => _isBaseUnit;
        set => SetProperty(ref _isBaseUnit, value);
    }
}

public class QuyDoiDonViViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private bool _isOpen;
    private bool _isLoading;
    private string _filterLoai = "Tất cả";
    private QuyDoiNguyenLieuItem? _selectedNguyenLieu;
    private ObservableCollection<QuyDoiNguyenLieuItem> _nguyenLieus = new();
    private ObservableCollection<QuyDoiNguyenLieuItem> _filteredNguyenLieus = new();
    private ObservableCollection<QuyDoiRowItem> _quyDoiRows = new();
    private ObservableCollection<string> _loaiFilters = new();
    private ObservableCollection<DonViTinh> _donViTinhs = new();
    private DonViTinh? _selectedNewDonVi;

    public bool IsOpen
    {
        get => _isOpen;
        set => SetProperty(ref _isOpen, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string FilterLoai
    {
        get => _filterLoai;
        set
        {
            if (SetProperty(ref _filterLoai, value))
                ApplyFilter();
        }
    }

    public QuyDoiNguyenLieuItem? SelectedNguyenLieu
    {
        get => _selectedNguyenLieu;
        set
        {
            // Bỏ chọn item cũ
            if (_selectedNguyenLieu != null)
                _selectedNguyenLieu.IsSelected = false;

            if (SetProperty(ref _selectedNguyenLieu, value))
            {
                // Đánh dấu item mới
                if (value != null)
                    value.IsSelected = true;

                OnPropertyChanged(nameof(HasSelectedNguyenLieu));
                SafeInitializeAsync(() => LoadQuyDoiForSelectedAsync());
            }
        }
    }

    public bool HasSelectedNguyenLieu => SelectedNguyenLieu != null;

    public ObservableCollection<QuyDoiNguyenLieuItem> FilteredNguyenLieus
    {
        get => _filteredNguyenLieus;
        set => SetProperty(ref _filteredNguyenLieus, value);
    }

    public ObservableCollection<QuyDoiRowItem> QuyDoiRows
    {
        get => _quyDoiRows;
        set => SetProperty(ref _quyDoiRows, value);
    }

    public ObservableCollection<string> LoaiFilters
    {
        get => _loaiFilters;
        set => SetProperty(ref _loaiFilters, value);
    }

    public ObservableCollection<DonViTinh> DonViTinhs
    {
        get => _donViTinhs;
        set => SetProperty(ref _donViTinhs, value);
    }

    public DonViTinh? SelectedNewDonVi
    {
        get => _selectedNewDonVi;
        set => SetProperty(ref _selectedNewDonVi, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddNewRowCommand { get; }
    public ICommand SelectNguyenLieuCommand { get; }

    public QuyDoiDonViViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        CloseCommand = new RelayCommand(_ => Close());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAllAsync());
        AddNewRowCommand = new RelayCommand(_ => AddNewRow());
        SelectNguyenLieuCommand = new RelayCommand(p =>
        {
            if (p is QuyDoiNguyenLieuItem item)
                SelectedNguyenLieu = item;
        });
    }

    public async Task OpenAsync()
    {
        IsOpen = true;
        IsLoading = true;

        try
        {
            // Tải danh sách đơn vị tính
            var donVis = await _databaseService.GetDonViTinhsAsync();
            DonViTinhs.Clear();
            foreach (var dv in donVis) DonViTinhs.Add(dv);

            // Tải danh sách nguyên liệu
            var nguyenLieus = await _databaseService.GetAllNguyenLieusWithDetailsAsync();
            _nguyenLieus.Clear();

            var loaiSet = new HashSet<string> { "Tất cả" };

            foreach (var nl in nguyenLieus.Where(n => n.TrangThai))
            {
                var item = new QuyDoiNguyenLieuItem
                {
                    NguyenLieuID = nl.NguyenLieuID,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    HinhAnh = nl.HinhAnh,
                    LoaiNguyenLieu = nl.LoaiNguyenLieu?.TenLoai ?? "",
                    DonViChinh = nl.DonViTinh?.TenDonVi ?? "",
                    DonViID = nl.DonViID
                };
                _nguyenLieus.Add(item);

                if (!string.IsNullOrEmpty(nl.LoaiNguyenLieu?.TenLoai))
                    loaiSet.Add(nl.LoaiNguyenLieu.TenLoai);
            }

            LoaiFilters = new ObservableCollection<string>(loaiSet);
            FilterLoai = "Tất cả";
            ApplyFilter();

            // Tự động chọn nguyên liệu đầu tiên
            if (FilteredNguyenLieus.Any())
                SelectedNguyenLieu = FilteredNguyenLieus.First();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening QuyDoi popup: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Close()
    {
        IsOpen = false;
        SelectedNguyenLieu = null;
        QuyDoiRows.Clear();
    }

    private void ApplyFilter()
    {
        IEnumerable<QuyDoiNguyenLieuItem> filtered = _nguyenLieus;

        if (!string.IsNullOrEmpty(FilterLoai) && FilterLoai != "Tất cả")
        {
            filtered = filtered.Where(n => n.LoaiNguyenLieu == FilterLoai);
        }

        FilteredNguyenLieus = new ObservableCollection<QuyDoiNguyenLieuItem>(filtered);
    }

    private async Task LoadQuyDoiForSelectedAsync()
    {
        if (SelectedNguyenLieu == null)
        {
            QuyDoiRows.Clear();
            return;
        }

        try
        {
            var quyDois = await _databaseService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);
            QuyDoiRows.Clear();

            // Tìm đơn vị gốc của nguyên liệu
            var donViGocId = SelectedNguyenLieu.DonViID;
            bool baseUnitAdded = false;
            
            // Thêm đơn vị gốc lên đầu tiên
            if (donViGocId.HasValue)
            {
                var baseQuyDoi = quyDois.FirstOrDefault(qd => qd.DonViID == donViGocId.Value);
                if (baseQuyDoi != null)
                {
                    var baseRow = new QuyDoiRowItem
                    {
                        QuyDoiID = baseQuyDoi.QuyDoiID,
                        NguyenLieuID = baseQuyDoi.NguyenLieuID,
                        DonViID = baseQuyDoi.DonViID,
                        TenDonVi = baseQuyDoi.DonViTinh?.TenDonVi ?? "",
                        HeSoText = baseQuyDoi.HeSo.ToString("G"),
                        LaDonViChuan = baseQuyDoi.LaDonViChuan,
                        IsBaseUnit = true
                    };
                    baseRow.OnDonViChuanChanged = r => {
                        foreach(var other in QuyDoiRows) {
                            if (other != r && other.LaDonViChuan) other.LaDonViChuan = false;
                        }
                    };
                    baseRow.DeleteCommand = new AsyncRelayCommand(async _ => await DeleteRowAsync(baseRow));
                    QuyDoiRows.Add(baseRow);
                    baseUnitAdded = true;
                }
            }

            foreach (var qd in quyDois)
            {
                // Bỏ qua đơn vị gốc vì đã thêm ở trên
                if (baseUnitAdded && donViGocId.HasValue && qd.DonViID == donViGocId.Value)
                    continue;
                    
                var row = new QuyDoiRowItem
                {
                    QuyDoiID = qd.QuyDoiID,
                    NguyenLieuID = qd.NguyenLieuID,
                    DonViID = qd.DonViID,
                    TenDonVi = qd.DonViTinh?.TenDonVi ?? "",
                    HeSoText = qd.HeSo.ToString("G"),
                    LaDonViChuan = qd.LaDonViChuan,
                    IsBaseUnit = false
                };
                row.OnDonViChuanChanged = r => {
                    foreach(var other in QuyDoiRows) {
                        if (other != r && other.LaDonViChuan) other.LaDonViChuan = false;
                    }
                };
                row.DeleteCommand = new AsyncRelayCommand(async _ => await DeleteRowAsync(row));
                QuyDoiRows.Add(row);
            }
            
            // Nếu chưa thêm đơn vị gốc, thêm mặc định
            if (!baseUnitAdded && !string.IsNullOrEmpty(SelectedNguyenLieu.DonViChinh))
            {
                var baseRow = new QuyDoiRowItem
                {
                    QuyDoiID = 0,
                    NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
                    DonViID = SelectedNguyenLieu.DonViID,
                    TenDonVi = SelectedNguyenLieu.DonViChinh,
                    HeSoText = "1",
                    LaDonViChuan = true,
                    IsBaseUnit = true
                };
                baseRow.OnDonViChuanChanged = r => {
                    foreach(var other in QuyDoiRows) {
                        if (other != r && other.LaDonViChuan) other.LaDonViChuan = false;
                    }
                };
                baseRow.DeleteCommand = new AsyncRelayCommand(async _ => await DeleteRowAsync(baseRow));
                QuyDoiRows.Insert(0, baseRow);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading QuyDoi: {ex.Message}");
        }
    }

    private void AddNewRow()
    {
        if (SelectedNguyenLieu == null)
        {
            MessageBox.Show(
                "Vui lòng chọn nguyên liệu trước!",
                "Thiếu thông tin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (SelectedNewDonVi == null)
        {
            MessageBox.Show(
                "Vui lòng chọn đơn vị quy đổi muốn thêm!",
                "Thiếu thông tin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Kiểm tra trùng lặp (bao gồm cả đơn vị gốc nếu đã có trong bảng)
        if (QuyDoiRows.Any(r => r.DonViID == SelectedNewDonVi.DonViID))
        {
            MessageBox.Show(
                $"Đơn vị '{SelectedNewDonVi.TenDonVi}' đã tồn tại trong bảng quy đổi!",
                "Trùng lặp",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var newRow = new QuyDoiRowItem
        {
            QuyDoiID = 0,
            NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
            DonViID = SelectedNewDonVi.DonViID,
            TenDonVi = SelectedNewDonVi.TenDonVi,
            HeSoText = "1",
            LaDonViChuan = !QuyDoiRows.Any()
        };
        newRow.OnDonViChuanChanged = r => {
            foreach(var other in QuyDoiRows) {
                if (other != r && other.LaDonViChuan) other.LaDonViChuan = false;
            }
        };
        newRow.DeleteCommand = new AsyncRelayCommand(async _ => await DeleteRowAsync(newRow));
        QuyDoiRows.Add(newRow);

        SelectedNewDonVi = null;
    }

    private async Task SaveAllAsync()
    {
        if (SelectedNguyenLieu == null) return;

        // Validate: phải có ít nhất 1 đơn vị chuẩn
        if (QuyDoiRows.Any() && !QuyDoiRows.Any(r => r.LaDonViChuan))
        {
            MessageBox.Show(
                "Phải có ít nhất 1 đơn vị được chọn làm ĐV Chuẩn (đơn vị lưu tồn kho)!",
                "Thiếu đơn vị chuẩn",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Lấy trạng thái cũ từ DB TRƯỚC KHI lưu để phát hiện thay đổi đơn vị chuẩn
            var oldQuyDois = await _databaseService.GetQuyDoiDonVisAsync(SelectedNguyenLieu.NguyenLieuID);
            var oldDonViChuan = oldQuyDois.FirstOrDefault(qd => qd.LaDonViChuan);

            foreach (var row in QuyDoiRows)
            {
                var cleanText = row.HeSoText?.Replace(",", ".") ?? "";
                if (!decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal heSo) || heSo <= 0)
                {
                    MessageBox.Show(
                        $"Hệ số của đơn vị '{row.TenDonVi}' không hợp lệ! Phải là số dương.",
                        "Lỗi hệ số",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var quyDoi = new QuyDoiDonVi
                {
                    QuyDoiID = row.QuyDoiID,
                    NguyenLieuID = SelectedNguyenLieu.NguyenLieuID,
                    DonViID = row.DonViID,
                    HeSo = heSo,
                    LaDonViChuan = row.LaDonViChuan
                };

                await _databaseService.SaveQuyDoiDonViAsync(quyDoi);
            }

            // So sánh đơn vị chuẩn CŨ (từ DB) với đơn vị chuẩn MỚI (từ UI)
            var newDonViChuan = QuyDoiRows.FirstOrDefault(r => r.LaDonViChuan);
            if (newDonViChuan?.DonViID != null &&
                (oldDonViChuan == null || oldDonViChuan.DonViID != newDonViChuan.DonViID))
            {
                // Quy đổi số lượng tồn kho theo đơn vị chuẩn mới
                // ⚡ BUG FIX: Lấy hệ số cũ từ giao diện (QuyDoiRows) thay vì từ DB (oldDonViChuan)
                var uiOldDonVi = QuyDoiRows.FirstOrDefault(r => r.DonViID == oldDonViChuan?.DonViID);
                decimal oldHeSo = 1m;
                if (uiOldDonVi != null)
                {
                    var cleanOld = uiOldDonVi.HeSoText?.Replace(",", ".") ?? "";
                    if (decimal.TryParse(cleanOld, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedOldHeSo))
                    {
                        oldHeSo = parsedOldHeSo;
                    }
                }
                else if (oldDonViChuan != null)
                {
                    oldHeSo = oldDonViChuan.HeSo;
                }

                var cleanNew = newDonViChuan.HeSoText?.Replace(",", ".") ?? "";
                if (decimal.TryParse(cleanNew, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal newHeSo) && oldHeSo > 0 && newHeSo > 0)
                {
                    var tonKho = await _databaseService.GetTonKhoByNguyenLieuIdAsync(SelectedNguyenLieu.NguyenLieuID);
                    if (tonKho != null)
                    {
                        if (oldHeSo > 0 && newHeSo > 0)
                        {
                            decimal conversionFactor = oldHeSo / newHeSo;
                            decimal newSoLuongTon = tonKho.SoLuongTon * conversionFactor;

                            await _databaseService.UpdateTonKhoAsync(SelectedNguyenLieu.NguyenLieuID, newSoLuongTon);
                        }
                    }
                }
                
                // Cập nhật DonViID trong bảng NguyenLieu
                var nguyenLieu = await _databaseService.GetNguyenLieuByIdAsync(SelectedNguyenLieu.NguyenLieuID);
                if (nguyenLieu != null)
                {
                    nguyenLieu.DonViID = newDonViChuan.DonViID;
                    await _databaseService.SaveNguyenLieuAsync(nguyenLieu);
                    SelectedNguyenLieu.DonViChinh = newDonViChuan.TenDonVi;
                    SelectedNguyenLieu.DonViID = newDonViChuan.DonViID;
                }
            }

            MessageBox.Show(
                "Đã lưu hệ số quy đổi thành công!",
                "Thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Reload lại để cập nhật QuyDoiID mới và tránh trùng lặp
            await LoadQuyDoiForSelectedAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lỗi khi lưu: {ex.Message}",
                "Lỗi",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DeleteRowAsync(QuyDoiRowItem row)
    {
        // Không cho phép xóa đơn vị gốc
        if (row.IsBaseUnit)
        {
            MessageBox.Show(
                "Không thể xóa đơn vị gốc của nguyên liệu!",
                "Không được phép",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        
        var confirmed = await ShowDeleteConfirmation(
            row.TenDonVi,
            "Xóa đơn vị quy đổi",
            $"Bạn có chắc chắn muốn xóa đơn vị \"{row.TenDonVi}\" khỏi bảng quy đổi?\nHành động này không thể hoàn tác.");
        if (!confirmed) return;

        if (row.QuyDoiID > 0)
        {
            await _databaseService.DeleteQuyDoiDonViAsync(row.QuyDoiID);
        }

        QuyDoiRows.Remove(row);
    }
}
