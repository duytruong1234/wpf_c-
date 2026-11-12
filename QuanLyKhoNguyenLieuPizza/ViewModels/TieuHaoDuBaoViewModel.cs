using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public class TieuHaoDuBaoViewModel : BaseViewModel
{
    private const double ChartPlotMaxHeight = 216.0;

    private readonly DatabaseService _databaseService;
    private bool _isInitializing;
    private bool _isSyncingBacktestCombos;

    // Navigation state: Overview Table (image1) vs Analysis & Forecast (image2..5)
    private bool _isOverviewMode = true;
    private bool _isForecastTab;

    // Overview filters
    private NguyenLieuFilterOption? _selectedOverviewNguyenLieuOption;
    private KyThoiGianOption? _selectedKyOption;

    // Analysis filters
    private NguyenLieuFilterOption? _selectedAnalysisNguyenLieu;
    private NamKhaoSatOption? _selectedNamOption;
    private bool _isLoading;

    // Tab 1: 12-Month Seasonality properties
    private string _barChartHeaderTitle = "Lượng Tiêu Hao 12 Tháng Năm 2026 (kg)";
    private string _seasonMaxValueText = "0 kg";
    private string _seasonMaxSubText = "Tháng 01";
    private string _seasonMinValueText = "0 kg";
    private string _seasonMinSubText = "Tháng 01";
    private string _seasonAvgValueText = "0.0 kg";
    private string _seasonTotalValueText = "0.0 kg";
    private Thickness _avgLineMargin = new(0, 0, 0, 0);

    // Tab 2: Forecast & Import Recommendation properties
    private SingleIngredientForecastDetail? _currentForecastDetail;
    private int _activePhuongPhapId = 2;
    private int _optimalPhuongPhapId = 2;

    private string _nextPlanHeaderTitle = "KẾ HOẠCH DỰ BÁO THÁNG TIẾP THEO";
    private string _activeModelNameText = "HW Multiplicative";
    private string _nextForecastQtyText = "0.00 kg";
    private string _currentStockQtyText = "0.00 kg";
    private string _suggestedImportQtyText = "0.00 kg";

    private string _lastResultHeaderTitle = "KẾT QUẢ DỰ BÁO THÁNG VỪA RỒI";
    private string _lastModelNameText = "--";
    private string _lastForecastQtyText = "--";
    private string _lastActualQtyText = "--";
    private string _lastAbsErrorText = "--";
    private string _lastApeText = "--";
    private string _lastConfidenceText = "Độ tin cậy: --";

    private KyThoiGianOption? _selectedTrainStartKy;
    private KyThoiGianOption? _selectedTrainEndKy;
    private KyThoiGianOption? _selectedTestStartKy;
    private KyThoiGianOption? _selectedTestEndKy;
    private string _comparisonChartTitle = "ĐỐI CHIẾU THỰC NGHIỆM GIỮA CÁC MÔ HÌNH VỚI THỰC TẾ";
    private string _forecastUnitLabelText = "Đơn vị tính: kg";

    private string _wmaMaeText = "--", _wmaRmseText = "--", _wmaMapeText = "--";
    private string _sesMaeText = "--", _sesRmseText = "--", _sesMapeText = "--";
    private string _holtMaeText = "--", _holtRmseText = "--", _holtMapeText = "--";
    private string _hwAddMaeText = "--", _hwAddRmseText = "--", _hwAddMapeText = "--";
    private string _hwMulMaeText = "--", _hwMulRmseText = "--", _hwMulMapeText = "--";

    // Popup 1: Danh sách món ăn bán ra theo nguyên liệu
    private bool _isDishPopupOpen;
    private string _dishPopupTitle = string.Empty;
    private string _totalSoldPortionsText = "0 phần/bánh";
    private string _totalConsumedPopupText = "0.00 kg";

    // Popup 2: Lịch sử dự báo tiêu hao nguyên liệu (image6)
    private bool _isHistoryPopupOpen;
    private string _historyPopupTitle = string.Empty;

    // Collections
    public ObservableCollection<NguyenLieuFilterOption> NguyenLieuOptions { get; } = new();
    public ObservableCollection<NguyenLieuFilterOption> AnalysisNguyenLieuOptions { get; } = new();
    public ObservableCollection<KyThoiGianOption> KyOptions { get; } = new();
    public ObservableCollection<KyThoiGianOption> BacktestKyOptions { get; } = new();
    public ObservableCollection<NamKhaoSatOption> NamOptions { get; } = new();

    public ObservableCollection<TieuHaoNguyenLieuItem> TieuHaoItems { get; } = new();
    public ObservableCollection<MonAnTieuHaoItem> DishPopupItems { get; } = new();
    public ObservableCollection<LichSuDuBaoRow> HistoryItems { get; } = new();

    // Tab 1 Chart Collections
    public ObservableCollection<ThangTieuHaoChartItem> SeasonThangItems { get; } = new();
    public ObservableCollection<YAxisTickItem> YAxisTicks { get; } = new();
    public ObservableCollection<NamTieuHaoSeries> YearSeries { get; } = new();

    // Tab 2 Chart & Table Collections
    public ObservableCollection<ForecastChartSeries> ForecastChartSeriesList { get; } = new();
    public ObservableCollection<BacktestMonthRow> BacktestRows { get; } = new();

    // =========================================================================
    // MODE & TAB PROPERTIES
    // =========================================================================

    public bool IsOverviewMode
    {
        get => _isOverviewMode;
        set
        {
            if (SetProperty(ref _isOverviewMode, value))
            {
                OnPropertyChanged(nameof(IsAnalysisMode));
            }
        }
    }

    public bool IsAnalysisMode => !_isOverviewMode;

    public bool IsForecastTab
    {
        get => _isForecastTab;
        set
        {
            if (SetProperty(ref _isForecastTab, value))
            {
                OnPropertyChanged(nameof(IsConsumptionTab));
            }
        }
    }

    public bool IsConsumptionTab => !_isForecastTab;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // =========================================================================
    // OVERVIEW PROPERTIES (IMAGE 1)
    // =========================================================================

    public NguyenLieuFilterOption? SelectedOverviewNguyenLieuOption
    {
        get => _selectedOverviewNguyenLieuOption;
        set
        {
            if (SetProperty(ref _selectedOverviewNguyenLieuOption, value) && !_isInitializing)
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
                OnPropertyChanged(nameof(ReportMonthHighlightText));
                OnPropertyChanged(nameof(PrevMonthHeaderText));
                OnPropertyChanged(nameof(PrevYearHeaderText));
                OnPropertyChanged(nameof(NextForecastHeaderText));
                if (!_isInitializing)
                {
                    _ = LoadBaoCaoTieuHaoAsync();
                }
            }
        }
    }

    private DateTime DefaultKy => SelectedKyOption?.KyThoiGian
                                  ?? KyOptions.FirstOrDefault()?.KyThoiGian
                                  ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string ReportMonthHighlightText => $"Tháng {DefaultKy:MM/yyyy}";
    public string TotalIngredientsCountText => $"{TieuHaoItems.Count:D2} Nguyên liệu";
    public string PrevMonthHeaderText => $"SO VỚI THÁNG {DefaultKy.AddMonths(-1):MM/yyyy}";
    public string PrevYearHeaderText => $"SO VỚI THÁNG {DefaultKy.AddYears(-1):MM/yyyy}";
    public string NextForecastHeaderText => $"DỰ BÁO (THÁNG {DefaultKy.AddMonths(1):MM/yyyy})";

    // =========================================================================
    // ANALYSIS & FORECAST SCREEN PROPERTIES (IMAGE 2..6)
    // =========================================================================

    public NguyenLieuFilterOption? SelectedAnalysisNguyenLieu
    {
        get => _selectedAnalysisNguyenLieu;
        set
        {
            if (SetProperty(ref _selectedAnalysisNguyenLieu, value) && !_isInitializing && value?.NguyenLieuID != null)
            {
                _ = ReloadAllAnalysisDataAsync();
            }
        }
    }

    public NamKhaoSatOption? SelectedNamOption
    {
        get => _selectedNamOption;
        set
        {
            if (SetProperty(ref _selectedNamOption, value) && !_isInitializing && IsAnalysisMode)
            {
                _ = LoadMuaVu12ThangAsync();
            }
        }
    }

    // Tab 1 Bindings
    public string BarChartHeaderTitle { get => _barChartHeaderTitle; set => SetProperty(ref _barChartHeaderTitle, value); }
    public string SeasonMaxValueText { get => _seasonMaxValueText; set => SetProperty(ref _seasonMaxValueText, value); }
    public string SeasonMaxSubText { get => _seasonMaxSubText; set => SetProperty(ref _seasonMaxSubText, value); }
    public string SeasonMinValueText { get => _seasonMinValueText; set => SetProperty(ref _seasonMinValueText, value); }
    public string SeasonMinSubText { get => _seasonMinSubText; set => SetProperty(ref _seasonMinSubText, value); }
    public string SeasonAvgValueText { get => _seasonAvgValueText; set => SetProperty(ref _seasonAvgValueText, value); }
    public string SeasonTotalValueText { get => _seasonTotalValueText; set => SetProperty(ref _seasonTotalValueText, value); }
    public Thickness AvgLineMargin { get => _avgLineMargin; set => SetProperty(ref _avgLineMargin, value); }

    // Tab 2 Top Cards Bindings
    public string NextPlanHeaderTitle { get => _nextPlanHeaderTitle; set => SetProperty(ref _nextPlanHeaderTitle, value); }
    public string ActiveModelNameText { get => _activeModelNameText; set => SetProperty(ref _activeModelNameText, value); }
    public string NextForecastQtyText { get => _nextForecastQtyText; set => SetProperty(ref _nextForecastQtyText, value); }
    public string CurrentStockQtyText { get => _currentStockQtyText; set => SetProperty(ref _currentStockQtyText, value); }
    public string SuggestedImportQtyText { get => _suggestedImportQtyText; set => SetProperty(ref _suggestedImportQtyText, value); }

    public string LastResultHeaderTitle { get => _lastResultHeaderTitle; set => SetProperty(ref _lastResultHeaderTitle, value); }
    public string LastModelNameText { get => _lastModelNameText; set => SetProperty(ref _lastModelNameText, value); }
    public string LastForecastQtyText { get => _lastForecastQtyText; set => SetProperty(ref _lastForecastQtyText, value); }
    public string LastActualQtyText { get => _lastActualQtyText; set => SetProperty(ref _lastActualQtyText, value); }
    public string LastAbsErrorText { get => _lastAbsErrorText; set => SetProperty(ref _lastAbsErrorText, value); }
    public string LastApeText { get => _lastApeText; set => SetProperty(ref _lastApeText, value); }
    public string LastConfidenceText { get => _lastConfidenceText; set => SetProperty(ref _lastConfidenceText, value); }

    // Tab 2 Backtest Range Bindings
    public KyThoiGianOption? SelectedTrainStartKy
    {
        get => _selectedTrainStartKy;
        set { if (SetProperty(ref _selectedTrainStartKy, value)) OnPropertyChanged(nameof(TrainCountLabelText)); }
    }

    public KyThoiGianOption? SelectedTrainEndKy
    {
        get => _selectedTrainEndKy;
        set { if (SetProperty(ref _selectedTrainEndKy, value)) OnPropertyChanged(nameof(TrainCountLabelText)); }
    }

    public KyThoiGianOption? SelectedTestStartKy
    {
        get => _selectedTestStartKy;
        set { if (SetProperty(ref _selectedTestStartKy, value)) OnPropertyChanged(nameof(TestCountLabelText)); }
    }

    public KyThoiGianOption? SelectedTestEndKy
    {
        get => _selectedTestEndKy;
        set { if (SetProperty(ref _selectedTestEndKy, value)) OnPropertyChanged(nameof(TestCountLabelText)); }
    }

    public string TrainCountLabelText => CountMonthsLabel(SelectedTrainStartKy?.KyDate, SelectedTrainEndKy?.KyDate);
    public string TestCountLabelText => CountMonthsLabel(SelectedTestStartKy?.KyDate, SelectedTestEndKy?.KyDate);

    private static string CountMonthsLabel(DateTime? from, DateTime? to)
    {
        if (from == null || to == null) return "(0 tháng)";
        int diff = (to.Value.Year - from.Value.Year) * 12 + (to.Value.Month - from.Value.Month) + 1;
        return $"({Math.Max(0, diff)} tháng)";
    }

    public string ComparisonChartTitle { get => _comparisonChartTitle; set => SetProperty(ref _comparisonChartTitle, value); }
    public string ForecastUnitLabelText { get => _forecastUnitLabelText; set => SetProperty(ref _forecastUnitLabelText, value); }

    // 5 Model Metrics & Active Flags (3=WMA, 4=SES, 5=HOLT, 1=HW_ADD, 2=HW_MUL)
    public string WmaMaeText { get => _wmaMaeText; set => SetProperty(ref _wmaMaeText, value); }
    public string WmaRmseText { get => _wmaRmseText; set => SetProperty(ref _wmaRmseText, value); }
    public string WmaMapeText { get => _wmaMapeText; set => SetProperty(ref _wmaMapeText, value); }

    public string SesMaeText { get => _sesMaeText; set => SetProperty(ref _sesMaeText, value); }
    public string SesRmseText { get => _sesRmseText; set => SetProperty(ref _sesRmseText, value); }
    public string SesMapeText { get => _sesMapeText; set => SetProperty(ref _sesMapeText, value); }

    public string HoltMaeText { get => _holtMaeText; set => SetProperty(ref _holtMaeText, value); }
    public string HoltRmseText { get => _holtRmseText; set => SetProperty(ref _holtRmseText, value); }
    public string HoltMapeText { get => _holtMapeText; set => SetProperty(ref _holtMapeText, value); }

    public string HwAddMaeText { get => _hwAddMaeText; set => SetProperty(ref _hwAddMaeText, value); }
    public string HwAddRmseText { get => _hwAddRmseText; set => SetProperty(ref _hwAddRmseText, value); }
    public string HwAddMapeText { get => _hwAddMapeText; set => SetProperty(ref _hwAddMapeText, value); }

    public string HwMulMaeText { get => _hwMulMaeText; set => SetProperty(ref _hwMulMaeText, value); }
    public string HwMulRmseText { get => _hwMulRmseText; set => SetProperty(ref _hwMulRmseText, value); }
    public string HwMulMapeText { get => _hwMulMapeText; set => SetProperty(ref _hwMulMapeText, value); }

    public bool IsWmaActive => _activePhuongPhapId == 3;
    public bool IsSesActive => _activePhuongPhapId == 4;
    public bool IsHoltActive => _activePhuongPhapId == 5;
    public bool IsHwAddActive => _activePhuongPhapId == 1;
    public bool IsHwMulActive => _activePhuongPhapId == 2;

    public bool IsWmaLowestMape => _optimalPhuongPhapId == 3;
    public bool IsSesLowestMape => _optimalPhuongPhapId == 4;
    public bool IsHoltLowestMape => _optimalPhuongPhapId == 5;
    public bool IsHwAddLowestMape => _optimalPhuongPhapId == 1;
    public bool IsHwMulLowestMape => _optimalPhuongPhapId == 2;

    // Popup 1 Properties
    public bool IsDishPopupOpen { get => _isDishPopupOpen; set => SetProperty(ref _isDishPopupOpen, value); }
    public string DishPopupTitle { get => _dishPopupTitle; set => SetProperty(ref _dishPopupTitle, value); }
    public string TotalSoldPortionsText { get => _totalSoldPortionsText; set => SetProperty(ref _totalSoldPortionsText, value); }
    public string TotalConsumedPopupText { get => _totalConsumedPopupText; set => SetProperty(ref _totalConsumedPopupText, value); }

    // Popup 2 Properties (History)
    public bool IsHistoryPopupOpen { get => _isHistoryPopupOpen; set => SetProperty(ref _isHistoryPopupOpen, value); }
    public string HistoryPopupTitle { get => _historyPopupTitle; set => SetProperty(ref _historyPopupTitle, value); }

    // Commands
    public ICommand OpenAnalysisViewCommand { get; }
    public ICommand BackToOverviewCommand { get; }
    public ICommand SelectSingleIngredientCommand { get; }
    public ICommand OpenIngredientForecastCommand { get; }
    public ICommand SwitchToConsumptionTabCommand { get; }
    public ICommand SwitchToForecastTabCommand { get; }
    public ICommand RefreshAnalysisCommand { get; }
    public ICommand RunBacktestCommand { get; }
    public ICommand SelectMainModelCommand { get; }
    public ICommand ToggleSeriesVisibilityCommand { get; }
    public ICommand OpenDishPopupCommand { get; }
    public ICommand CloseDishPopupCommand { get; }
    public ICommand OpenHistoryPopupCommand { get; }
    public ICommand CloseHistoryPopupCommand { get; }

    public TieuHaoDuBaoViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        OpenAnalysisViewCommand = new RelayCommand(async _ =>
        {
            var targetId = SelectedOverviewNguyenLieuOption?.NguyenLieuID
                           ?? AnalysisNguyenLieuOptions.FirstOrDefault()?.NguyenLieuID;
            await EnterAnalysisModeAsync(targetId, openForecastTab: false);
        });

        BackToOverviewCommand = new RelayCommand(async _ =>
        {
            IsOverviewMode = true;
            await LoadBaoCaoTieuHaoAsync();
        });

        SelectSingleIngredientCommand = new RelayCommand(async param =>
        {
            if (param is TieuHaoNguyenLieuItem item)
            {
                await EnterAnalysisModeAsync(item.NguyenLieuID, openForecastTab: false);
            }
        });

        OpenIngredientForecastCommand = new RelayCommand(async param =>
        {
            if (param is TieuHaoNguyenLieuItem item)
            {
                await EnterAnalysisModeAsync(item.NguyenLieuID, openForecastTab: true);
            }
        });

        SwitchToConsumptionTabCommand = new RelayCommand(_ => IsForecastTab = false);
        SwitchToForecastTabCommand = new RelayCommand(_ => IsForecastTab = true);

        RefreshAnalysisCommand = new RelayCommand(async _ => await ReloadAllAnalysisDataAsync());

        RunBacktestCommand = new RelayCommand(async _ => await RunCustomBacktestAsync());

        SelectMainModelCommand = new RelayCommand(async param =>
        {
            if (param != null && int.TryParse(param.ToString(), out int ppId))
            {
                await SelectMainModelAsync(ppId);
            }
        });

        ToggleSeriesVisibilityCommand = new RelayCommand(param =>
        {
            if (param is ForecastChartSeries s)
            {
                s.IsVisible = !s.IsVisible;
            }
        });

        OpenDishPopupCommand = new RelayCommand(async param =>
        {
            if (param is TieuHaoNguyenLieuItem item)
                await OpenDishPopupAsync(item);
        });
        CloseDishPopupCommand = new RelayCommand(_ => IsDishPopupOpen = false);

        OpenHistoryPopupCommand = new RelayCommand(async _ => await OpenHistoryPopupAsync());
        CloseHistoryPopupCommand = new RelayCommand(_ => IsHistoryPopupOpen = false);

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
            AnalysisNguyenLieuOptions.Clear();
            foreach (var opt in nlOptions)
            {
                NguyenLieuOptions.Add(opt);
                if (opt.NguyenLieuID != null)
                    AnalysisNguyenLieuOptions.Add(opt);
            }

            KyOptions.Clear();
            foreach (var ky in kyOptions)
                KyOptions.Add(ky);

            BacktestKyOptions.Clear();
            foreach (var ky in kyOptions.OrderBy(k => k.KyDate))
                BacktestKyOptions.Add(ky);

            NamOptions.Clear();
            var years = kyOptions.Select(k => k.KyDate.Year).Distinct().OrderBy(y => y).ToList();
            foreach (var y in years)
                NamOptions.Add(new NamKhaoSatOption { Nam = y });

            try
            {
                _selectedOverviewNguyenLieuOption = NguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == null)
                                                    ?? NguyenLieuOptions.FirstOrDefault();
                _selectedAnalysisNguyenLieu = AnalysisNguyenLieuOptions.FirstOrDefault();
                _selectedKyOption = KyOptions.FirstOrDefault();
                // Mặc định chọn năm mới nhất (ví dụ Năm 2026) cho màn hình Biểu đồ phân tích
                _selectedNamOption = NamOptions.LastOrDefault();
                _isOverviewMode = true;
                _isForecastTab = false;

                OnPropertyChanged(nameof(SelectedOverviewNguyenLieuOption));
                OnPropertyChanged(nameof(SelectedAnalysisNguyenLieu));
                OnPropertyChanged(nameof(SelectedKyOption));
                OnPropertyChanged(nameof(SelectedNamOption));
                OnPropertyChanged(nameof(IsOverviewMode));
                OnPropertyChanged(nameof(IsAnalysisMode));
                OnPropertyChanged(nameof(IsForecastTab));
                OnPropertyChanged(nameof(IsConsumptionTab));
                OnPropertyChanged(nameof(ReportMonthHighlightText));
                OnPropertyChanged(nameof(PrevMonthHeaderText));
                OnPropertyChanged(nameof(PrevYearHeaderText));
                OnPropertyChanged(nameof(NextForecastHeaderText));
            }
            finally
            {
                _isInitializing = false;
            }

            await LoadBaoCaoTieuHaoAsync();
        });
    }

    private async Task EnterAnalysisModeAsync(int? nguyenLieuId, bool openForecastTab)
    {
        _isInitializing = true;
        try
        {
            var found = AnalysisNguyenLieuOptions.FirstOrDefault(x => x.NguyenLieuID == nguyenLieuId)
                        ?? AnalysisNguyenLieuOptions.FirstOrDefault();
            _selectedAnalysisNguyenLieu = found;
            OnPropertyChanged(nameof(SelectedAnalysisNguyenLieu));

            var foundYear = NamOptions.FirstOrDefault(n => n.Nam == DefaultKy.Year)
                            ?? NamOptions.LastOrDefault();
            _selectedNamOption = foundYear;
            OnPropertyChanged(nameof(SelectedNamOption));
        }
        finally
        {
            _isInitializing = false;
        }

        IsForecastTab = openForecastTab;
        IsOverviewMode = false;
        await ReloadAllAnalysisDataAsync();
    }

    private async Task ReloadAllAnalysisDataAsync()
    {
        await LoadMuaVu12ThangAsync();
        await LoadForecastDetailAsync(persistCustom: false, useCustomRange: false);
    }

    public async Task LoadBaoCaoTieuHaoAsync()
    {
        try
        {
            IsLoading = true;
            var ky = DefaultKy;
            var nlId = SelectedOverviewNguyenLieuOption?.NguyenLieuID;

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

    // =========================================================================
    // TAB 1: PHÂN TÍCH TIÊU HAO (IMAGE 2)
    // =========================================================================

    public async Task LoadMuaVu12ThangAsync()
    {
        var nlId = SelectedAnalysisNguyenLieu?.NguyenLieuID ?? 0;
        if (nlId <= 0) return;

        try
        {
            IsLoading = true;
            var nam = SelectedNamOption?.Nam ?? NamOptions.LastOrDefault()?.Nam ?? DateTime.Today.Year;
            var data = await _databaseService.GetThongKeMuaVu12ThangAsync(nlId, nam);
            var dvt = data.DonViKho;

            var activeMonths = data.ThangItems.Where(x => x.SoLuongTieuHao > 0).ToList();
            if (activeMonths.Count == 0) activeMonths = data.ThangItems;

            var maxItem = activeMonths.OrderByDescending(x => x.SoLuongTieuHao).ThenByDescending(x => x.Thang).First();
            var minItem = activeMonths.OrderBy(x => x.SoLuongTieuHao).ThenBy(x => x.Thang).First();

            decimal totalVal = data.ThangItems.Sum(x => x.SoLuongTieuHao);
            decimal avgVal = activeMonths.Count > 0 ? Math.Round(totalVal / activeMonths.Count, 1) : 0m;

            double rawMax = (double)maxItem.SoLuongTieuHao;
            if (rawMax <= 0) rawMax = 80.0;
            double rawStep = (rawMax * 1.06) / 8.0;
            double niceStep = rawStep <= 1 ? 1
                : rawStep <= 2 ? 2
                : rawStep <= 2.5 ? 2.5
                : rawStep <= 5 ? 5
                : rawStep <= 10 ? 10
                : rawStep <= 15 ? 15
                : rawStep <= 20 ? 20
                : rawStep <= 25 ? 25
                : rawStep <= 50 ? 50
                : Math.Ceiling(rawStep / 50.0) * 50.0;

            const int tickIntervals = 8;
            double yMax = niceStep * tickIntervals;

            YAxisTicks.Clear();
            for (int i = tickIntervals; i >= 0; i--)
            {
                double tickVal = i * niceStep;
                string tickStr = Math.Abs(tickVal - Math.Round(tickVal)) < 0.01 ? $"{tickVal:N0} {dvt}" : $"{tickVal:N1} {dvt}";
                YAxisTicks.Add(new YAxisTickItem { Label = tickStr });
            }

            const double plotHeight = 208.0; // 8 intervals * 26px per row
            SeasonThangItems.Clear();
            foreach (var mItem in data.ThangItems)
            {
                mItem.IsMax = mItem.Thang == maxItem.Thang && maxItem.SoLuongTieuHao > 0;
                mItem.IsMin = mItem.Thang == minItem.Thang && minItem.SoLuongTieuHao > 0 && maxItem.Thang != minItem.Thang;
                if (mItem.SoLuongTieuHao > 0 && yMax > 0)
                {
                    double ratio = (double)mItem.SoLuongTieuHao / yMax;
                    mItem.BarHeight = Math.Max(6.0, Math.Min(plotHeight, ratio * plotHeight));
                }
                else
                {
                    mItem.BarHeight = 0.0;
                }
                SeasonThangItems.Add(mItem);
            }

            double avgRatio = yMax > 0 ? (double)avgVal / yMax : 0.0;
            double avgPx = Math.Max(0.0, Math.Min(plotHeight, avgRatio * plotHeight));
            AvgLineMargin = new Thickness(0, 0, 0, avgPx);

            string FormatVal(decimal v) => Math.Abs(v - Math.Round(v)) < 0.05m ? $"{Math.Round(v):N0}" : $"{v:N1}";

            BarChartHeaderTitle = $"Lượng Tiêu Hao 12 Tháng Năm {nam} ({dvt})";
            SeasonTotalValueText = $"{totalVal:N1} {dvt}";
            SeasonAvgValueText = $"{avgVal:N1} {dvt}";
            SeasonMaxValueText = $"{FormatVal(maxItem.SoLuongTieuHao)} {dvt}";
            SeasonMaxSubText = $"Đạt đỉnh vào Tháng {maxItem.Thang:D2}";
            SeasonMinValueText = $"{FormatVal(minItem.SoLuongTieuHao)} {dvt}";
            SeasonMinSubText = $"Chạm đáy vào Tháng {minItem.Thang:D2}";

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

    private static readonly Color[] SeriesColors =
    {
        Color.FromRgb(0x02, 0x84, 0xC7), // xanh dương
        Color.FromRgb(0x10, 0xB9, 0x81), // xanh lá
        Color.FromRgb(0xF5, 0x9E, 0x0B), // cam
        Color.FromRgb(0x8B, 0x5C, 0xF6), // tím
        Color.FromRgb(0xEF, 0x44, 0x44), // đỏ
    };

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

    // =========================================================================
    // TAB 2: DỰ BÁO TIÊU HAO VÀ ĐỀ XUẤT NHẬP KHO (IMAGE 3, 4, 5)
    // =========================================================================

    private async Task LoadForecastDetailAsync(bool persistCustom, bool useCustomRange)
    {
        var nlId = SelectedAnalysisNguyenLieu?.NguyenLieuID ?? 0;
        if (nlId <= 0) return;

        try
        {
            IsLoading = true;

            DateTime? trStart = useCustomRange ? SelectedTrainStartKy?.KyDate : null;
            DateTime? trEnd = useCustomRange ? SelectedTrainEndKy?.KyDate : null;
            DateTime? teStart = useCustomRange ? SelectedTestStartKy?.KyDate : null;
            DateTime? teEnd = useCustomRange ? SelectedTestEndKy?.KyDate : null;

            var detail = await Task.Run(() => _databaseService.GetSingleIngredientForecastDetailAsync(
                nlId, trStart, trEnd, teStart, teEnd, persistCustom));

            _currentForecastDetail = detail;
            _activePhuongPhapId = detail.ActivePhuongPhapID;
            _optimalPhuongPhapId = detail.OptimalPhuongPhapID;

            if (!_isSyncingBacktestCombos && !useCustomRange)
            {
                _isSyncingBacktestCombos = true;
                try
                {
                    SelectedTrainStartKy = BacktestKyOptions.FirstOrDefault(x => x.KyDate == detail.TrainStart) ?? BacktestKyOptions.FirstOrDefault();
                    SelectedTrainEndKy = BacktestKyOptions.FirstOrDefault(x => x.KyDate == detail.TrainEnd) ?? BacktestKyOptions.FirstOrDefault();
                    SelectedTestStartKy = BacktestKyOptions.FirstOrDefault(x => x.KyDate == detail.TestStart) ?? BacktestKyOptions.LastOrDefault();
                    SelectedTestEndKy = BacktestKyOptions.FirstOrDefault(x => x.KyDate == detail.TestEnd) ?? BacktestKyOptions.LastOrDefault();
                }
                finally
                {
                    _isSyncingBacktestCombos = false;
                }
            }

            ApplyForecastDetailToBindings(detail);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadForecastDetailAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RunCustomBacktestAsync()
    {
        if (SelectedTrainStartKy == null || SelectedTrainEndKy == null ||
            SelectedTestStartKy == null || SelectedTestEndKy == null)
            return;

        if (SelectedTrainStartKy.KyDate > SelectedTrainEndKy.KyDate)
        {
            MessageBox.Show("Kỳ bắt đầu huấn luyện phải nhỏ hơn hoặc bằng kỳ kết thúc huấn luyện.",
                "Kiểm tra kỳ huấn luyện", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedTestStartKy.KyDate > SelectedTestEndKy.KyDate)
        {
            MessageBox.Show("Kỳ bắt đầu kiểm thử phải nhỏ hơn hoặc bằng kỳ kết thúc kiểm thử.",
                "Kiểm tra kỳ kiểm thử", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedTrainEndKy.KyDate >= SelectedTestStartKy.KyDate)
        {
            MessageBox.Show("Kỳ kết thúc huấn luyện phải đứng trước kỳ bắt đầu kiểm thử.",
                "Kiểm tra khoảng thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadForecastDetailAsync(persistCustom: true, useCustomRange: true);
    }

    private async Task SelectMainModelAsync(int ppId)
    {
        if (_currentForecastDetail == null) return;
        _activePhuongPhapId = ppId;
        _currentForecastDetail.ActivePhuongPhapID = ppId;

        decimal? newNextVal = _currentForecastDetail.Models.TryGetValue(ppId, out var mSum) ? mSum.NextForecast : null;
        await _databaseService.SetActiveForecastModelAsync(
            _currentForecastDetail.NguyenLieuID,
            _currentForecastDetail.NextForecastKy,
            ppId,
            newNextVal);

        ApplyForecastDetailToBindings(_currentForecastDetail);
    }

    private void ApplyForecastDetailToBindings(SingleIngredientForecastDetail d)
    {
        string dvt = d.DonViKho;

        // Card A: Kế hoạch dự báo tháng tiếp theo
        NextPlanHeaderTitle = $"KẾ HOẠCH DỰ BÁO THÁNG TIẾP THEO (THÁNG {d.NextForecastKy:MM/yyyy})";
        ActiveModelNameText = ForecastMethodHelper.ToShortName(_activePhuongPhapId);

        decimal nextF = d.Models.TryGetValue(_activePhuongPhapId, out var activeSum) ? activeSum.NextForecast : 0m;
        decimal suggestImport = Math.Max(0m, Math.Round(nextF - d.TonKhoHienTai, 2));

        NextForecastQtyText = $"{nextF:N2} {dvt}";
        CurrentStockQtyText = $"{d.TonKhoHienTai:N2} {dvt}";
        SuggestedImportQtyText = $"{suggestImport:N2} {dvt}";

        // Card B: Kết quả dự báo tháng vừa rồi
        LastResultHeaderTitle = $"KẾT QUẢ DỰ BÁO THÁNG VỪA RỒI ({d.LatestActualKy:MM/yyyy})";
        if (d.HasLastKyData)
        {
            decimal err = Math.Abs(d.LastKyActual - d.LastKyForecast);
            decimal ape = d.LastKyActual > 0.001m ? Math.Round(err / d.LastKyActual * 100m, 2) : 0m;
            decimal conf = Math.Max(0m, Math.Round(100m - ape, 2));

            LastModelNameText = d.LastKyMethodName;
            LastForecastQtyText = $"{d.LastKyForecast:N2} {dvt}";
            LastActualQtyText = $"{d.LastKyActual:N2} {dvt}";
            LastAbsErrorText = $"{err:N2} {dvt}";
            LastApeText = $"{ape:N2}%";
            LastConfidenceText = $"Độ tin cậy: {conf:N2}%";
        }
        else
        {
            LastModelNameText = "--";
            LastForecastQtyText = "--";
            LastActualQtyText = $"{d.LastKyActual:N2} {dvt}";
            LastAbsErrorText = "--";
            LastApeText = "--";
            LastConfidenceText = "Độ tin cậy: --";
        }

        // Card C: Tiêu đề biểu đồ & bảng
        ComparisonChartTitle = $"ĐỐI CHIẾU THỰC NGHIỆM GIỮA CÁC MÔ HÌNH VỚI THỰC TẾ ({d.TestStart:MM/yyyy} - {d.TestEnd:MM/yyyy})";
        ForecastUnitLabelText = $"Đơn vị tính: {dvt}";

        BacktestRows.Clear();
        foreach (var r in d.MonthRows)
            BacktestRows.Add(r);

        if (d.Models.TryGetValue(3, out var mWma)) { WmaMaeText = mWma.MaeText; WmaRmseText = mWma.RmseText; WmaMapeText = mWma.MapeText; }
        if (d.Models.TryGetValue(4, out var mSes)) { SesMaeText = mSes.MaeText; SesRmseText = mSes.RmseText; SesMapeText = mSes.MapeText; }
        if (d.Models.TryGetValue(5, out var mHolt)) { HoltMaeText = mHolt.MaeText; HoltRmseText = mHolt.RmseText; HoltMapeText = mHolt.MapeText; }
        if (d.Models.TryGetValue(1, out var mAdd)) { HwAddMaeText = mAdd.MaeText; HwAddRmseText = mAdd.RmseText; HwAddMapeText = mAdd.MapeText; }
        if (d.Models.TryGetValue(2, out var mMul)) { HwMulMaeText = mMul.MaeText; HwMulRmseText = mMul.RmseText; HwMulMapeText = mMul.MapeText; }

        OnPropertyChanged(nameof(IsWmaActive));
        OnPropertyChanged(nameof(IsSesActive));
        OnPropertyChanged(nameof(IsHoltActive));
        OnPropertyChanged(nameof(IsHwAddActive));
        OnPropertyChanged(nameof(IsHwMulActive));
        OnPropertyChanged(nameof(IsWmaLowestMape));
        OnPropertyChanged(nameof(IsSesLowestMape));
        OnPropertyChanged(nameof(IsHoltLowestMape));
        OnPropertyChanged(nameof(IsHwAddLowestMape));
        OnPropertyChanged(nameof(IsHwMulLowestMape));

        // Dựng 6 chuỗi đường cho biểu đồ đối chiếu (Thực tế + Mô hình đang chọn xếp trước, sau đó các mô hình còn lại)
        var hiddenIds = ForecastChartSeriesList.Where(s => !s.IsVisible).Select(s => s.PhuongPhapID).ToHashSet();
        var labels = d.TestMonthLabels.ToArray();

        var newSeries = new List<ForecastChartSeries>
        {
            new ForecastChartSeries
            {
                PhuongPhapID = 0,
                IsActual = true,
                Name = "Số lượng tiêu hao thực tế",
                LineColor = Color.FromRgb(0xDC, 0x26, 0x26), // Đỏ đậm
                Values = d.TestActualValues,
                MonthLabels = labels,
                DonViKho = dvt,
                IsVisible = !hiddenIds.Contains(0)
            }
        };

        // Đưa mô hình đang được chọn (hoặc tối ưu) đứng ngay sau Thực tế trên thanh Legend
        int[] orderCandidates = { 2, 1, 5, 3, 4 };
        var orderedModels = orderCandidates
            .OrderByDescending(id => id == _activePhuongPhapId)
            .ToList();

        foreach (int id in orderedModels)
        {
            if (!d.Models.TryGetValue(id, out var ms)) continue;
            bool isChosen = id == _activePhuongPhapId;
            bool isOpt = id == _optimalPhuongPhapId;
            string suffix = isOpt ? " (Tối ưu)" : (isChosen ? " (Đang chọn)" : "");

            Color color = isChosen
                ? Color.FromRgb(0x7C, 0x3A, 0xED) // Tím đậm nổi bật cho mô hình đang áp dụng
                : id switch
                {
                    1 => Color.FromRgb(0x02, 0x84, 0xC7), // Xanh dương (HW Additive)
                    2 => Color.FromRgb(0x25, 0x63, 0xEB), // Xanh lam đậm (HW Multiplicative khi không chọn)
                    5 => Color.FromRgb(0xD9, 0x77, 0x06), // Cam (Holt)
                    3 => Color.FromRgb(0x0D, 0x94, 0x88), // Xanh ngọc (WMA)
                    _ => Color.FromRgb(0x94, 0xA3, 0xB8)  // Xám xanh (SES)
                };

            newSeries.Add(new ForecastChartSeries
            {
                PhuongPhapID = id,
                IsActual = false,
                IsSelectedModel = isChosen,
                Name = ms.ChartBaseName + suffix,
                LineColor = color,
                Values = ms.TestPredictions,
                MonthLabels = labels,
                DonViKho = dvt,
                IsVisible = !hiddenIds.Contains(id)
            });
        }

        ForecastChartSeriesList.Clear();
        foreach (var s in newSeries)
            ForecastChartSeriesList.Add(s);
    }

    // =========================================================================
    // POPUPS (MÓN ĂN BÁN RA & LỊCH SỬ DỰ BÁO)
    // =========================================================================

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

    private async Task OpenHistoryPopupAsync()
    {
        if (_currentForecastDetail == null) return;
        try
        {
            var d = _currentForecastDetail;
            HistoryPopupTitle = $"LỊCH SỬ DỰ BÁO TIÊU HAO NGUYÊN LIỆU ({d.MaNguyenLieu} - {d.TenNguyenLieu.ToUpperInvariant()})";

            var rows = await _databaseService.GetLichSuDuBaoAsync(d.NguyenLieuID, d.DonViKho);
            HistoryItems.Clear();
            foreach (var r in rows)
                HistoryItems.Add(r);

            IsHistoryPopupOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OpenHistoryPopupAsync: {ex.Message}");
        }
    }
}
