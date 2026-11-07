using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class TieuHaoDuBaoViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private bool _isInitializing;

    private NguyenLieuFilterOption? _selectedNguyenLieuOption;
    private KyThoiGianOption? _selectedKyOption;
    private bool _isLoading;

    // Popup 1: Danh sách món ăn bán ra theo nguyên liệu
    private bool _isDishPopupOpen;
    private string _dishPopupTitle = string.Empty;
    private string _totalSoldPortionsText = "0 phần/bánh";
    private string _totalConsumedPopupText = "0.00 kg";

    // Popup 2: Dự báo nhu cầu nguyên liệu (Holt-Winters)
    private bool _isForecastPopupOpen;
    private bool _isForecastLoading;
    private string _forecastPopupTitle = string.Empty;
    private string _forecastSummaryText = string.Empty;
    private string _forecastTotalChipText = "Tổng: 25 nguyên liệu";
    private string _forecastAdditiveChipText = "Cộng tính tối ưu: 20 NL";
    private string _forecastMultiplicativeChipText = "Nhân tính tối ưu: 05 NL";
    private string _nextKyHeaderText = "DỰ BÁO KỲ TỚI";

    public ObservableCollection<NguyenLieuFilterOption> NguyenLieuOptions { get; } = new();
    public ObservableCollection<KyThoiGianOption> KyOptions { get; } = new();
    public ObservableCollection<TieuHaoNguyenLieuItem> TieuHaoItems { get; } = new();
    public ObservableCollection<MonAnTieuHaoItem> DishPopupItems { get; } = new();
    public ObservableCollection<KetQuaDuBaoItem> ForecastItems { get; } = new();

    public NguyenLieuFilterOption? SelectedNguyenLieuOption
    {
        get => _selectedNguyenLieuOption;
        set
        {
            if (SetProperty(ref _selectedNguyenLieuOption, value) && !_isInitializing)
            {
                _ = LoadBaoCaoTieuHaoAsync();
            }
        }
    }

    public KyThoiGianOption? SelectedKyOption
    {
        get => _selectedKyOption;
        set
        {
            if (SetProperty(ref _selectedKyOption, value))
            {
                OnPropertyChanged(nameof(ReportHeaderTitle));
                OnPropertyChanged(nameof(PrevMonthHeaderText));
                OnPropertyChanged(nameof(PrevYearHeaderText));
                OnPropertyChanged(nameof(NextKyButtonText));
                if (!_isInitializing)
                {
                    _ = LoadBaoCaoTieuHaoAsync();
                }
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ReportHeaderTitle
    {
        get
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            return $"Báo Cáo Tiêu Hao Nguyên Liệu - Kỳ {ky:MM/yyyy}";
        }
    }

    public string TotalIngredientsCountText => $"{TieuHaoItems.Count:D2} Nguyên liệu";

    public string PrevMonthHeaderText
    {
        get
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            return $"SO VỚI {ky.AddMonths(-1):MM/yyyy}";
        }
    }

    public string PrevYearHeaderText
    {
        get
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            return $"SO VỚI {ky.AddYears(-1):MM/yyyy}";
        }
    }

    public string NextKyButtonText
    {
        get
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            return $"Dự báo kỳ {ky.AddMonths(1):MM/yyyy}";
        }
    }

    // Popup 1 Properties
    public bool IsDishPopupOpen
    {
        get => _isDishPopupOpen;
        set => SetProperty(ref _isDishPopupOpen, value);
    }

    public string DishPopupTitle
    {
        get => _dishPopupTitle;
        set => SetProperty(ref _dishPopupTitle, value);
    }

    public string TotalSoldPortionsText
    {
        get => _totalSoldPortionsText;
        set => SetProperty(ref _totalSoldPortionsText, value);
    }

    public string TotalConsumedPopupText
    {
        get => _totalConsumedPopupText;
        set => SetProperty(ref _totalConsumedPopupText, value);
    }

    // Popup 2 Properties
    public bool IsForecastPopupOpen
    {
        get => _isForecastPopupOpen;
        set => SetProperty(ref _isForecastPopupOpen, value);
    }

    public bool IsForecastLoading
    {
        get => _isForecastLoading;
        set => SetProperty(ref _isForecastLoading, value);
    }

    public string ForecastPopupTitle
    {
        get => _forecastPopupTitle;
        set => SetProperty(ref _forecastPopupTitle, value);
    }

    public string ForecastSummaryText
    {
        get => _forecastSummaryText;
        set => SetProperty(ref _forecastSummaryText, value);
    }

    public string ForecastTotalChipText
    {
        get => _forecastTotalChipText;
        set => SetProperty(ref _forecastTotalChipText, value);
    }

    public string ForecastAdditiveChipText
    {
        get => _forecastAdditiveChipText;
        set => SetProperty(ref _forecastAdditiveChipText, value);
    }

    public string ForecastMultiplicativeChipText
    {
        get => _forecastMultiplicativeChipText;
        set => SetProperty(ref _forecastMultiplicativeChipText, value);
    }

    public string NextKyHeaderText
    {
        get => _nextKyHeaderText;
        set => SetProperty(ref _nextKyHeaderText, value);
    }

    // Commands
    public ICommand OpenDishPopupCommand { get; }
    public ICommand CloseDishPopupCommand { get; }
    public ICommand OpenForecastPopupCommand { get; }
    public ICommand CloseForecastPopupCommand { get; }
    public ICommand RefreshCommand { get; }

    public TieuHaoDuBaoViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        OpenDishPopupCommand = new RelayCommand(async param =>
        {
            if (param is TieuHaoNguyenLieuItem item)
            {
                await OpenDishPopupAsync(item);
            }
        });

        CloseDishPopupCommand = new RelayCommand(_ => IsDishPopupOpen = false);

        OpenForecastPopupCommand = new RelayCommand(async _ => await OpenForecastPopupAsync());
        CloseForecastPopupCommand = new RelayCommand(_ => IsForecastPopupOpen = false);
        RefreshCommand = new RelayCommand(async _ => await InitializeAsync());

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            _isInitializing = true;
            IsLoading = true;

            var nlOptions = await _databaseService.GetNguyenLieuFilterOptionsAsync();
            var kyOptions = await _databaseService.GetAvailablePeriodsAsync();

            NguyenLieuOptions.Clear();
            foreach (var opt in nlOptions)
                NguyenLieuOptions.Add(opt);

            KyOptions.Clear();
            foreach (var ky in kyOptions)
                KyOptions.Add(ky);

            _selectedNguyenLieuOption = NguyenLieuOptions.FirstOrDefault();
            _selectedKyOption = KyOptions.FirstOrDefault();

            OnPropertyChanged(nameof(SelectedNguyenLieuOption));
            OnPropertyChanged(nameof(SelectedKyOption));
            OnPropertyChanged(nameof(ReportHeaderTitle));
            OnPropertyChanged(nameof(PrevMonthHeaderText));
            OnPropertyChanged(nameof(PrevYearHeaderText));
            OnPropertyChanged(nameof(NextKyButtonText));

            _isInitializing = false;

            await LoadBaoCaoTieuHaoAsync();
        });
    }

    public async Task LoadBaoCaoTieuHaoAsync()
    {
        try
        {
            IsLoading = true;
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            var nlId = SelectedNguyenLieuOption?.NguyenLieuID;

            var items = await _databaseService.GetBaoCaoTieuHaoAsync(ky, nlId);

            TieuHaoItems.Clear();
            foreach (var item in items)
                TieuHaoItems.Add(item);

            OnPropertyChanged(nameof(TotalIngredientsCountText));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadBaoCaoTieuHaoAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenDishPopupAsync(TieuHaoNguyenLieuItem item)
    {
        try
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            DishPopupTitle = $"Danh Sách Món Ăn Bán Ra - [{item.MaNguyenLieu} - {item.TenNguyenLieu}]";

            var details = await _databaseService.GetMonAnDaBanTheoNguyenLieuAsync(
                item.NguyenLieuID, item.DonViKho, ky);

            DishPopupItems.Clear();
            int totalSold = 0;
            double totalConsumed = 0;

            foreach (var d in details)
            {
                DishPopupItems.Add(d);
                totalSold += d.SoLuongDaBan;
                totalConsumed += (double)d.TongTieuHaoKho;
            }

            TotalSoldPortionsText = $"{totalSold} phần/bánh";
            TotalConsumedPopupText = $"{totalConsumed:N2} {item.DonViKho}";
            IsDishPopupOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OpenDishPopupAsync: {ex.Message}");
        }
    }

    private async Task OpenForecastPopupAsync()
    {
        try
        {
            var ky = SelectedKyOption?.KyThoiGian ?? new DateTime(2026, 10, 1);
            var nextKy = ky.AddMonths(1);
            var nlId = SelectedNguyenLieuOption?.NguyenLieuID;

            ForecastPopupTitle = $"Dự Báo Nhu Cầu Nguyên Liệu (Holt-Winters) - Kỳ {nextKy:MM/yyyy}";
            IsForecastPopupOpen = true;
            IsForecastLoading = true;

            var results = await Task.Run(() => _databaseService.RunHoltWintersForecastAsync(ky, nlId));

            ForecastItems.Clear();
            int addCount = 0;
            int mulCount = 0;
            int needImportCount = 0;

            foreach (var r in results)
            {
                ForecastItems.Add(r);
                if (r.IsAdditiveSelected) addCount++;
                else mulCount++;
                if (r.CanNhapThem > 0.01) needImportCount++;
            }

            ForecastTotalChipText = $"Tổng: {results.Count:D2} nguyên liệu";
            ForecastAdditiveChipText = $"Cộng tính tối ưu: {addCount:D2} NL";
            ForecastMultiplicativeChipText = $"Nhân tính tối ưu: {mulCount:D2} NL";
            NextKyHeaderText = $"DỰ BÁO {nextKy:MM/yyyy}";
            ForecastSummaryText = $"Chu kỳ mùa vụ L = 12 • Huấn luyện & kiểm thử trên chuỗi dữ liệu 01/2024 – {ky:MM/yyyy}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OpenForecastPopupAsync: {ex.Message}");
        }
        finally
        {
            IsForecastLoading = false;
        }
    }
}
