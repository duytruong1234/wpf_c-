using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class TieuHaoDuBaoViewModel : BaseViewModel
{
    private const double ChartPlotMaxHeight = 216.0;

    private readonly DatabaseService _databaseService;
    private bool _isInitializing;
    private bool _isChartViewMode = true;

    private NguyenLieuFilterOption? _selectedNguyenLieuOption;
    private KyThoiGianOption? _selectedKyOption;
    private NamKhaoSatOption? _selectedNamOption;
    private bool _isLoading;

    // 12-Month Seasonality Dashboard properties
    private string _seasonMaxValueText = "0 kg";
    private string _seasonMaxSubText = "Đạt đỉnh vào Tháng 01";
    private string _seasonMinValueText = "0 kg";
    private string _seasonMinSubText = "Chạm đáy vào Tháng 01";
    private string _seasonAvgValueText = "0.0 kg";
    private string _seasonTotalValueText = "0.0 kg";
    private string _seasonUnitSubText = "Đơn vị tính: kg";
    private string _seasonChartIngredientTitle = string.Empty;
    private string _seasonLegendBarText = "Lượng tiêu hao (kg)";
    private string _seasonLegendAvgText = "Mức trung bình (0.0 kg)";
    private Thickness _avgLineMargin = new(0, 0, 0, 0);

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
    public ObservableCollection<NamKhaoSatOption> NamOptions { get; } = new();
    public ObservableCollection<TieuHaoNguyenLieuItem> TieuHaoItems { get; } = new();
    public ObservableCollection<MonAnTieuHaoItem> DishPopupItems { get; } = new();
    public ObservableCollection<KetQuaDuBaoItem> ForecastItems { get; } = new();

    // 12-Month Seasonality Collections
    public ObservableCollection<ThangTieuHaoChartItem> SeasonThangItems { get; } = new();
    public ObservableCollection<YAxisTickItem> YAxisTicks { get; } = new();

    // So sánh chuỗi tiêu hao giữa các năm (biểu đồ đường)
    public ObservableCollection<NamTieuHaoSeries> YearSeries { get; } = new();

    public bool IsSingleIngredientMode => _isChartViewMode;
    public bool IsAllIngredientsMode => !_isChartViewMode;

    public NguyenLieuFilterOption? SelectedNguyenLieuOption
    {
        get => _selectedNguyenLieuOption;
        set
        {
            if (SetProperty(ref _selectedNguyenLieuOption, value))
            {
                if (!_isInitializing)
                {
                    if (value?.NguyenLieuID != null)
                    {
                        _isChartViewMode = true;
                        OnPropertyChanged(nameof(IsSingleIngredientMode));
                        OnPropertyChanged(nameof(IsAllIngredientsMode));
                        _ = LoadMuaVu12ThangAsync();
                    }
                    else
                    {
                        _isChartViewMode = false;
                        OnPropertyChanged(nameof(IsSingleIngredientMode));
                        OnPropertyChanged(nameof(IsAllIngredientsMode));
                        _ = LoadBaoCaoTieuHaoAsync();
                    }
                }
                else
                {
                    OnPropertyChanged(nameof(IsSingleIngredientMode));
                    OnPropertyChanged(nameof(IsAllIngredientsMode));
                }
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

    public NamKhaoSatOption? SelectedNamOption
    {
        get => _selectedNamOption;
        set
        {
            if (SetProperty(ref _selectedNamOption, value) && !_isInitializing)
            {
                if (IsSingleIngredientMode)
                {
                    _ = LoadMuaVu12ThangAsync();
                }
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private DateTime DefaultKy => SelectedKyOption?.KyThoiGian ?? KyOptions.FirstOrDefault()?.KyThoiGian ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string ReportHeaderTitle
    {
        get
        {
            var ky = DefaultKy;
            return $"Báo Cáo Tiêu Hao Nguyên Liệu - Kỳ {ky:MM/yyyy}";
        }
    }

    public string TotalIngredientsCountText => $"{TieuHaoItems.Count:D2} Nguyên liệu";

    public string PrevMonthHeaderText
    {
        get
        {
            var ky = DefaultKy;
            return $"SO VỚI {ky.AddMonths(-1):MM/yyyy}";
        }
    }

    public string PrevYearHeaderText
    {
        get
        {
            var ky = DefaultKy;
            return $"SO VỚI {ky.AddYears(-1):MM/yyyy}";
        }
    }

    public string NextKyButtonText
    {
        get
        {
            var ky = DefaultKy;
            return $"Dự báo kỳ {ky.AddMonths(1):MM/yyyy}";
        }
    }

    // 12-Month Seasonality Properties
    public string SeasonMaxValueText
    {
        get => _seasonMaxValueText;
        set => SetProperty(ref _seasonMaxValueText, value);
    }

    public string SeasonMaxSubText
    {
        get => _seasonMaxSubText;
        set => SetProperty(ref _seasonMaxSubText, value);
    }

    public string SeasonMinValueText
    {
        get => _seasonMinValueText;
        set => SetProperty(ref _seasonMinValueText, value);
    }

    public string SeasonMinSubText
    {
        get => _seasonMinSubText;
        set => SetProperty(ref _seasonMinSubText, value);
    }

    public string SeasonAvgValueText
    {
        get => _seasonAvgValueText;
        set => SetProperty(ref _seasonAvgValueText, value);
    }

    public string SeasonTotalValueText
    {
        get => _seasonTotalValueText;
        set => SetProperty(ref _seasonTotalValueText, value);
    }

    public string SeasonUnitSubText
    {
        get => _seasonUnitSubText;
        set => SetProperty(ref _seasonUnitSubText, value);
    }

    public string SeasonChartIngredientTitle
    {
        get => _seasonChartIngredientTitle;
        set => SetProperty(ref _seasonChartIngredientTitle, value);
    }

    public string SeasonLegendBarText
    {
        get => _seasonLegendBarText;
        set => SetProperty(ref _seasonLegendBarText, value);
    }

    public string SeasonLegendAvgText
    {
        get => _seasonLegendAvgText;
        set => SetProperty(ref _seasonLegendAvgText, value);
    }

    public Thickness AvgLineMargin
    {
        get => _avgLineMargin;
        set => SetProperty(ref _avgLineMargin, value);
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
    public ICommand SelectSingleIngredientCommand { get; }
    public ICommand BackToAllIngredientsCommand { get; }
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

        SelectSingleIngredientCommand = new RelayCommand(param =>
        {
            if (param is TieuHaoNguyenLieuItem item)
            {
                var found = NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == item.NguyenLieuID);
                if (found != null)
                {
                    SelectedNguyenLieuOption = found;
                }
            }
        });

        BackToAllIngredientsCommand = new RelayCommand(_ =>
        {
            if (_isChartViewMode)
            {
                var allOpt = NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == null);
                if (allOpt != null)
                {
                    SelectedNguyenLieuOption = allOpt;
                }
                else
                {
                    _isChartViewMode = false;
                    OnPropertyChanged(nameof(IsSingleIngredientMode));
                    OnPropertyChanged(nameof(IsAllIngredientsMode));
                    _ = LoadBaoCaoTieuHaoAsync();
                }
            }
            else
            {
                var firstNl = NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == 19)
                              ?? NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID != null);
                if (firstNl != null)
                {
                    SelectedNguyenLieuOption = firstNl;
                }
                else
                {
                    _isChartViewMode = true;
                    OnPropertyChanged(nameof(IsSingleIngredientMode));
                    OnPropertyChanged(nameof(IsAllIngredientsMode));
                    _ = LoadMuaVu12ThangAsync();
                }
            }
        });

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

            NamOptions.Clear();
            var years = kyOptions
                .Select(k => k.KyDate.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();
            foreach (var y in years)
            {
                NamOptions.Add(new NamKhaoSatOption { Nam = y });
            }

            try
            {
                // Mặc định mở màn hình Bảng báo cáo kỳ (tất cả nguyên liệu);
                // bấm nút "Biểu đồ mùa vụ 12 tháng" mới chuyển sang biểu đồ.
                _selectedNguyenLieuOption = NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == null)
                                            ?? NguyenLieuOptions.FirstOrDefault();
                _selectedKyOption = KyOptions.FirstOrDefault();
                _selectedNamOption = NamOptions.FirstOrDefault();
                _isChartViewMode = false;

                OnPropertyChanged(nameof(SelectedNguyenLieuOption));
                OnPropertyChanged(nameof(SelectedKyOption));
                OnPropertyChanged(nameof(SelectedNamOption));
                OnPropertyChanged(nameof(IsSingleIngredientMode));
                OnPropertyChanged(nameof(IsAllIngredientsMode));
                OnPropertyChanged(nameof(ReportHeaderTitle));
                OnPropertyChanged(nameof(PrevMonthHeaderText));
                OnPropertyChanged(nameof(PrevYearHeaderText));
                OnPropertyChanged(nameof(NextKyButtonText));
            }
            finally
            {
                _isInitializing = false;
            }

            await LoadBaoCaoTieuHaoAsync();
            if (_isChartViewMode)
                await LoadMuaVu12ThangAsync();
        });
    }

    public async Task LoadBaoCaoTieuHaoAsync()
    {
        try
        {
            IsLoading = true;
            var ky = DefaultKy;
            var nlId = IsAllIngredientsMode ? SelectedNguyenLieuOption?.NguyenLieuID : null;

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

    public async Task LoadMuaVu12ThangAsync()
    {
        var nlId = SelectedNguyenLieuOption?.NguyenLieuID ?? 0;

        try
        {
            IsLoading = true;
            var nam = SelectedNamOption?.Nam ?? NamOptions.FirstOrDefault()?.Nam ?? DateTime.Today.Year;
            var data = await _databaseService.GetThongKeMuaVu12ThangAsync(nlId, nam);
            var dvt = data.DonViKho;

            // Only consider months that have > 0 if partial year, or all 12 months
            var activeMonths = data.ThangItems.Where(x => x.SoLuongTieuHao > 0).ToList();
            if (activeMonths.Count == 0) activeMonths = data.ThangItems;

            var maxItem = activeMonths.OrderByDescending(x => x.SoLuongTieuHao).ThenByDescending(x => x.Thang).First();
            var minItem = activeMonths.OrderBy(x => x.SoLuongTieuHao).ThenBy(x => x.Thang).First();

            decimal totalVal = data.ThangItems.Sum(x => x.SoLuongTieuHao);
            decimal avgVal = activeMonths.Count > 0 ? Math.Round(totalVal / activeMonths.Count, 1) : 0m;

            // Calculate nice Y-axis max (9 intervals -> 10 tick labels)
            double rawMax = (double)maxItem.SoLuongTieuHao;
            if (rawMax <= 0) rawMax = 90.0;
            double rawStep = rawMax / 8.2;
            double niceStep;
            if (rawStep <= 1) niceStep = 1;
            else if (rawStep <= 2) niceStep = 2;
            else if (rawStep <= 5) niceStep = 5;
            else if (rawStep <= 10) niceStep = 10;
            else if (rawStep <= 20) niceStep = 20;
            else if (rawStep <= 25) niceStep = 25;
            else if (rawStep <= 50) niceStep = 50;
            else if (rawStep <= 100) niceStep = 100;
            else niceStep = Math.Ceiling(rawStep / 50.0) * 50.0;

            double yMax = niceStep * 9.0;
            if (yMax < rawMax * 1.05)
            {
                niceStep = Math.Ceiling((rawMax * 1.08) / 9.0);
                yMax = niceStep * 9.0;
            }

            YAxisTicks.Clear();
            for (int i = 9; i >= 0; i--)
            {
                double tickVal = i * niceStep;
                string tickStr = tickVal == Math.Floor(tickVal) ? $"{tickVal:N0} {dvt}" : $"{tickVal:N1} {dvt}";
                YAxisTicks.Add(new YAxisTickItem { Label = tickStr });
            }

            SeasonThangItems.Clear();
            foreach (var mItem in data.ThangItems)
            {
                mItem.IsMax = (mItem.Thang == maxItem.Thang && maxItem.SoLuongTieuHao > 0);
                mItem.IsMin = (mItem.Thang == minItem.Thang && minItem.SoLuongTieuHao > 0 && maxItem.Thang != minItem.Thang);
                double ratio = yMax > 0 ? (double)mItem.SoLuongTieuHao / yMax : 0.0;
                mItem.BarHeight = Math.Max(4.0, Math.Min(ChartPlotMaxHeight, ratio * ChartPlotMaxHeight));
                SeasonThangItems.Add(mItem);
            }

            double avgRatio = yMax > 0 ? (double)avgVal / yMax : 0.0;
            double avgPx = Math.Max(0.0, Math.Min(ChartPlotMaxHeight, avgRatio * ChartPlotMaxHeight));
            AvgLineMargin = new Thickness(0, 0, 0, avgPx);

            string FormatVal(decimal v) => Math.Abs(v - Math.Round(v)) < 0.05m ? $"{Math.Round(v):N0}" : $"{v:N1}";

            SeasonMaxValueText = $"{FormatVal(maxItem.SoLuongTieuHao)} {dvt}";
            SeasonMaxSubText = $"Đạt đỉnh vào Tháng {maxItem.Thang:D2}";
            SeasonMinValueText = $"{FormatVal(minItem.SoLuongTieuHao)} {dvt}";
            SeasonMinSubText = $"Chạm đáy vào Tháng {minItem.Thang:D2}";
            SeasonAvgValueText = $"{avgVal:N1} {dvt}";
            SeasonTotalValueText = $"{totalVal:N1} {dvt}";
            SeasonUnitSubText = $"Đơn vị tính: {dvt}";

            SeasonChartIngredientTitle = $"{data.MaNguyenLieu} - {data.TenNguyenLieu} ({dvt}) ({nam})";
            SeasonLegendBarText = $"Lượng tiêu hao ({dvt})";
            SeasonLegendAvgText = $"Mức trung bình ({avgVal:N1} {dvt})";

            await LoadSoSanhNamAsync(nlId, nam, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadMuaVu12ThangAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static readonly System.Windows.Media.Color[] SeriesColors =
    {
        System.Windows.Media.Color.FromRgb(0x02, 0x84, 0xC7), // xanh dương
        System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81), // xanh lá
        System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B), // cam
        System.Windows.Media.Color.FromRgb(0x8B, 0x5C, 0xF6), // tím
        System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44), // đỏ
    };

    /// <summary>
    /// Nạp chuỗi tiêu hao 12 tháng của tất cả các năm cho biểu đồ đường so sánh.
    /// </summary>
    private async Task LoadSoSanhNamAsync(int nlId, int namHienTai, ThongKeMuaVu12ThangResult dataNamHienTai)
    {
        var hiddenYears = YearSeries.Where(s => !s.IsVisible).Select(s => s.Nam).ToHashSet();
        var years = NamOptions.Select(n => n.Nam).OrderBy(y => y).ToList();
        if (!years.Contains(namHienTai)) years.Add(namHienTai);
        years.Sort();

        var list = new List<NamTieuHaoSeries>();
        for (int i = 0; i < years.Count; i++)
        {
            var y = years[i];
            var d = y == namHienTai ? dataNamHienTai : await _databaseService.GetThongKeMuaVu12ThangAsync(nlId, y);

            var values = new decimal?[12];
            foreach (var item in d.ThangItems)
            {
                if (item.Thang >= 1 && item.Thang <= 12 && item.SoLuongTieuHao > 0)
                    values[item.Thang - 1] = item.SoLuongTieuHao;
            }

            list.Add(new NamTieuHaoSeries
            {
                Nam = y,
                DonViKho = string.IsNullOrEmpty(d.DonViKho) ? dataNamHienTai.DonViKho : d.DonViKho,
                LineColor = SeriesColors[i % SeriesColors.Length],
                Values = values,
                IsVisible = !hiddenYears.Contains(y)
            });
        }

        YearSeries.Clear();
        foreach (var s in list)
            YearSeries.Add(s);
    }

    private async Task OpenDishPopupAsync(TieuHaoNguyenLieuItem item)
    {
        try
        {
            var ky = DefaultKy;
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
            var ky = DefaultKy;
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

            var minKy = KyOptions.LastOrDefault()?.KyThoiGian ?? ky;
            ForecastTotalChipText = $"Tổng: {results.Count:D2} nguyên liệu";
            ForecastAdditiveChipText = $"Cộng tính tối ưu: {addCount:D2} NL";
            ForecastMultiplicativeChipText = $"Nhân tính tối ưu: {mulCount:D2} NL";
            NextKyHeaderText = $"DỰ BÁO {nextKy:MM/yyyy}";
            ForecastSummaryText = $"Chu kỳ mùa vụ L = 12 • Huấn luyện & kiểm thử trên chuỗi dữ liệu {minKy:MM/yyyy} – {ky:MM/yyyy}";
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
