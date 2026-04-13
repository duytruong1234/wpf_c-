using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class QuyDinhViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<QuyDinh_Bot> QuyDinhBots { get; } = new();
    public ObservableCollection<QuyDinh_Vien> QuyDinhViens { get; } = new();

    public ObservableCollection<DoanhMuc_Size> Sizes { get; } = new();
    public ObservableCollection<DoanhMuc_De> DeBanhs { get; } = new();
    public ObservableCollection<DonViTinh> DonVis { get; } = new();
    public ObservableCollection<NguyenLieu> NguyenLieus { get; } = new();

    // Form Quy Ð?nh B?t
    private DoanhMuc_Size? _botSelectedSize;
    public DoanhMuc_Size? BotSelectedSize { get => _botSelectedSize; set => SetProperty(ref _botSelectedSize, value); }
    
    private DoanhMuc_De? _botSelectedDe;
    public DoanhMuc_De? BotSelectedDe { get => _botSelectedDe; set => SetProperty(ref _botSelectedDe, value); }

    private string _botTrongLuong = string.Empty;
    public string BotTrongLuong { get => _botTrongLuong; set => SetProperty(ref _botTrongLuong, value); }

    private DonViTinh? _botSelectedDonVi;
    public DonViTinh? BotSelectedDonVi { get => _botSelectedDonVi; set => SetProperty(ref _botSelectedDonVi, value); }

    // Form Quy Ð?nh Vi?n
    private DoanhMuc_De? _vienSelectedDe;
    public DoanhMuc_De? VienSelectedDe { get => _vienSelectedDe; set => SetProperty(ref _vienSelectedDe, value); }

    private DoanhMuc_Size? _vienSelectedSize;
    public DoanhMuc_Size? VienSelectedSize { get => _vienSelectedSize; set => SetProperty(ref _vienSelectedSize, value); }

    private NguyenLieu? _vienSelectedNguyenLieu;
    public NguyenLieu? VienSelectedNguyenLieu { get => _vienSelectedNguyenLieu; set => SetProperty(ref _vienSelectedNguyenLieu, value); }

    private string _vienSoLuong = string.Empty;
    public string VienSoLuong { get => _vienSoLuong; set => SetProperty(ref _vienSoLuong, value); }

    private DonViTinh? _vienSelectedDonVi;
    public DonViTinh? VienSelectedDonVi { get => _vienSelectedDonVi; set => SetProperty(ref _vienSelectedDonVi, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public ICommand LoadDataCommand { get; }
    public ICommand SaveBotCommand { get; }
    public ICommand DeleteBotCommand { get; }
    public ICommand EditBotCommand { get; }
    public ICommand ClearBotCommand { get; }

    public ICommand SaveVienCommand { get; }
    public ICommand DeleteVienCommand { get; }
    public ICommand EditVienCommand { get; }
    public ICommand ClearVienCommand { get; }

    public QuyDinhViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();
        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        
        SaveBotCommand = new AsyncRelayCommand(async _ => await SaveBotAsync());
        DeleteBotCommand = new AsyncRelayCommand(async param => { if (param is QuyDinh_Bot b) await DeleteBotAsync(b); });
        EditBotCommand = new RelayCommand<QuyDinh_Bot>(item => { if (item != null) EditBot(item); });
        ClearBotCommand = new RelayCommand(_ => ClearBotForm());

        SaveVienCommand = new AsyncRelayCommand(async _ => await SaveVienAsync());
        DeleteVienCommand = new AsyncRelayCommand(async param => { if (param is QuyDinh_Vien v) await DeleteVienAsync(v); });
        EditVienCommand = new RelayCommand<QuyDinh_Vien>(item => { if (item != null) EditVien(item); });
        ClearVienCommand = new RelayCommand(_ => ClearVienForm());

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        
        var sizes = await _databaseService.GetDoanhMucSizesAsync();
        Sizes.Clear();
        foreach (var s in sizes) Sizes.Add(s);

        var des = await _databaseService.GetDoanhMucDesAsync();
        DeBanhs.Clear();
        foreach (var d in des) DeBanhs.Add(d);

        var donvis = await _databaseService.GetDonViTinhsAsync();
        DonVis.Clear();
        foreach (var dv in donvis) DonVis.Add(dv);

        var nls = await _databaseService.GetNguyenLieusAsync();
        NguyenLieus.Clear();
        foreach (var nl in nls) NguyenLieus.Add(nl);

        await RefreshBotGridAsync();
        await RefreshVienGridAsync();

        IsLoading = false;
    }

    private async Task RefreshBotGridAsync()
    {
        var bots = await _databaseService.GetQuyDinhBotsAsync();
        QuyDinhBots.Clear();
        foreach (var b in bots) QuyDinhBots.Add(b);
    }

    private async Task RefreshVienGridAsync()
    {
        var viens = await _databaseService.GetQuyDinhViensAsync();
        QuyDinhViens.Clear();
        foreach (var v in viens) QuyDinhViens.Add(v);
    }

    private void ClearBotForm()
    {
        BotSelectedSize = null;
        BotSelectedDe = null;
        BotTrongLuong = string.Empty;
        BotSelectedDonVi = null;
    }

    private void EditBot(QuyDinh_Bot item)
    {
        BotSelectedSize = Sizes.FirstOrDefault(s => s.SizeID == item.SizeID);
        BotSelectedDe = DeBanhs.FirstOrDefault(d => d.LoaiCotBanh == item.LoaiCotBanh);
        BotTrongLuong = item.TrongLuongBot?.ToString("G") ?? "";
        BotSelectedDonVi = DonVis.FirstOrDefault(d => d.DonViID == item.DonViID);
    }

    private async Task SaveBotAsync()
    {
        if (BotSelectedSize == null || BotSelectedDe == null || string.IsNullOrWhiteSpace(BotSelectedDe.LoaiCotBanh)) return;
        
        double.TryParse(BotTrongLuong.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double tl);
        
        var bot = new QuyDinh_Bot
        {
            SizeID = BotSelectedSize.SizeID,
            LoaiCotBanh = BotSelectedDe.LoaiCotBanh,
            TrongLuongBot = tl,
            DonViID = BotSelectedDonVi?.DonViID
        };

        if (await _databaseService.SaveQuyDinhBotAsync(bot))
        {
            await RefreshBotGridAsync();
            ClearBotForm();
        }
    }

    private async Task DeleteBotAsync(QuyDinh_Bot item)
    {
        var confirmed = await ShowDeleteConfirmation(
            $"{item.SizeID} - {item.LoaiCotBanh}",
            "Xóa cấu hình bột",
            $"Bạn có chắc chắn muốn xóa cấu hình bột \"{item.SizeID} - {item.LoaiCotBanh}\"?\nHành động này không thể hoàn tác.");
        if (!confirmed) return;

        if (await _databaseService.DeleteQuyDinhBotAsync(item.SizeID, item.LoaiCotBanh))
        {
            await RefreshBotGridAsync();
        }
    }

    private void ClearVienForm()
    {
        VienSelectedDe = null;
        VienSelectedSize = null;
        VienSelectedNguyenLieu = null;
        VienSoLuong = string.Empty;
        VienSelectedDonVi = null;
    }

    private void EditVien(QuyDinh_Vien item)
    {
        VienSelectedDe = DeBanhs.FirstOrDefault(d => d.MaDeBanh == item.MaDeBanh);
        VienSelectedSize = Sizes.FirstOrDefault(s => s.SizeID == item.SizeID);
        VienSelectedNguyenLieu = NguyenLieus.FirstOrDefault(n => n.NguyenLieuID == item.NguyenLieuID);
        VienSoLuong = item.SoLuongVien?.ToString("G") ?? "";
        VienSelectedDonVi = DonVis.FirstOrDefault(d => d.DonViID == item.DonViID);
    }

    private async Task SaveVienAsync()
    {
        if (VienSelectedDe == null || VienSelectedSize == null || VienSelectedNguyenLieu == null) return;

        double.TryParse(VienSoLuong.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sl);

        var vien = new QuyDinh_Vien
        {
            MaDeBanh = VienSelectedDe.MaDeBanh,
            SizeID = VienSelectedSize.SizeID,
            NguyenLieuID = VienSelectedNguyenLieu.NguyenLieuID,
            SoLuongVien = sl,
            DonViID = VienSelectedDonVi?.DonViID
        };

        if (await _databaseService.SaveQuyDinhVienAsync(vien))
        {
            await RefreshVienGridAsync();
            ClearVienForm();
        }
    }

    private async Task DeleteVienAsync(QuyDinh_Vien item)
    {
        var confirmed = await ShowDeleteConfirmation(
            $"{item.MaDeBanh} - {item.SizeID}",
            "Xóa cấu hình viên",
            $"Bạn có chắc chắn muốn xóa cấu hình viên \"{item.MaDeBanh} - {item.SizeID}\"?\nHành động này không thể hoàn tác.");
        if (!confirmed) return;

        if (await _databaseService.DeleteQuyDinhVienAsync(item.MaDeBanh, item.SizeID, item.NguyenLieuID))
        {
            await RefreshVienGridAsync();
        }
    }
}

