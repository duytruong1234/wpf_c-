using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScottPlot.WPF;
using QuanLyKhoNguyenLieuPizza.ViewModels;
using WpfLine = System.Windows.Shapes.Line;
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfPolygon = System.Windows.Shapes.Polygon;
using WpfPolyline = System.Windows.Shapes.Polyline;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class DashboardView : UserControl
{
    private WpfPlot? _barChart;
    // ⚡ Lưu reference để có thể unsubscribe — tránh memory leak
    private DashboardViewModel? _vm;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateChartControls();

        if (DataContext is DashboardViewModel vm)
        {
            // ⚡ Unsubscribe cũ trước (phòng trường hợp Loaded gọi nhiều lần)
            if (_vm != null)
                _vm.OnDataLoaded -= OnVmDataLoaded;

            _vm = vm;
            _vm.OnDataLoaded += OnVmDataLoaded;

            if (vm.DailyRevenueValues.Length > 0)
            {
                UpdateCharts(vm);
            }
        }
    }

    /// <summary>
    /// ⚡ Hủy đăng ký event khi View unload — tránh memory leak
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.OnDataLoaded -= OnVmDataLoaded;
            _vm = null;
        }

        // Dispose ScottPlot chart resources
        if (_barChart is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>
    /// ⚡ Named method thay vì lambda — có thể unsubscribe
    /// </summary>
    private void OnVmDataLoaded()
    {
        // ⚡ BeginInvoke (async, không block) thay vì Invoke (sync, block)
        Dispatcher.BeginInvoke(() =>
        {
            if (_vm != null) UpdateCharts(_vm);
        });
    }

    private void CreateChartControls()
    {
        // Biểu đồ cột Top Pizza - vẫn dùng ScottPlot
        _barChart = new WpfPlot();
        _barChart.MinHeight = 280;
        StyleCartesianChart(_barChart);
        TopPizzaChartHost.Child = _barChart;
    }

    private static void StyleCartesianChart(WpfPlot chart)
    {
        chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        chart.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FAFBFC");
        chart.Plot.Axes.Bottom.FrameLineStyle.Color = ScottPlot.Color.FromHex("#E2E8F0");
        chart.Plot.Axes.Left.FrameLineStyle.Color = ScottPlot.Color.FromHex("#E2E8F0");
        chart.Plot.Axes.Top.FrameLineStyle.Width = 0;
        chart.Plot.Axes.Right.FrameLineStyle.Width = 0;
        chart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#F1F5F9");
        chart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#94A3B8");
        chart.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#94A3B8");
        chart.Plot.Axes.Bottom.MajorTickStyle.Color = ScottPlot.Color.FromHex("#E2E8F0");
        chart.Plot.Axes.Left.MajorTickStyle.Color = ScottPlot.Color.FromHex("#E2E8F0");
        chart.Plot.Axes.Bottom.MinorTickStyle.Length = 0;
        chart.Plot.Axes.Left.MinorTickStyle.Length = 0;
    }

    private void UpdateCharts(DashboardViewModel vm)
    {
        UpdateRevenueChart(vm);
        UpdateStockStatusChart(vm);
        UpdateBarChart(vm);
    }

    #region Revenue Area Chart (Custom WPF)
    private void UpdateRevenueChart(DashboardViewModel vm)
    {
        RevenueChartCanvas.Children.Clear();

        var values = vm.DailyRevenueValues;
        var labels = vm.DailyRevenueLabels;
        if (values.Length == 0) return;

        double canvasWidth = RevenueChartCanvas.ActualWidth > 0 ? RevenueChartCanvas.ActualWidth : 520;
        double canvasHeight = RevenueChartCanvas.ActualHeight > 0 ? RevenueChartCanvas.ActualHeight : 220;

        double paddingLeft = 55;
        double paddingRight = 20;
        double paddingTop = 20;
        double paddingBottom = 40;

        double chartWidth = canvasWidth - paddingLeft - paddingRight;
        double chartHeight = canvasHeight - paddingTop - paddingBottom;

        double maxVal = values.Max();
        if (maxVal <= 0) maxVal = 100;
        maxVal *= 1.2;

        // Vẽ grid lines ngang
        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            double y = paddingTop + chartHeight - (i * chartHeight / gridLines);
            double val = (i * maxVal / gridLines);

            var line = new WpfLine
            {
                X1 = paddingLeft,
                X2 = paddingLeft + chartWidth,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF1, 0xF5, 0xF9)),
                StrokeThickness = 1
            };
            RevenueChartCanvas.Children.Add(line);

            // Nhãn trục Y
            var label = new TextBlock
            {
                Text = val >= 1000 ? $"{val / 1000:0.#}M" : $"{val:0}k",
                FontSize = 10,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x94, 0xA3, 0xB8)),
            };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y - 7);
            RevenueChartCanvas.Children.Add(label);
        }

        // Tạo points
        var points = new PointCollection();
        var dataPoints = new List<Point>();
        for (int i = 0; i < values.Length; i++)
        {
            double x = paddingLeft + (i * chartWidth / (values.Length - 1));
            double y = paddingTop + chartHeight - (values[i] / maxVal * chartHeight);
            points.Add(new Point(x, y));
            dataPoints.Add(new Point(x, y));
        }

        // Vẽ Area gradient
        var areaPoints = new PointCollection(points);
        areaPoints.Add(new Point(paddingLeft + chartWidth, paddingTop + chartHeight));
        areaPoints.Add(new Point(paddingLeft, paddingTop + chartHeight));

        var areaPolygon = new WpfPolygon
        {
            Points = areaPoints,
            Fill = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(System.Windows.Media.Color.FromArgb(60, 0x5B, 0x6A, 0xFF), 0),
                    new GradientStop(System.Windows.Media.Color.FromArgb(15, 0x5B, 0x6A, 0xFF), 0.6),
                    new GradientStop(System.Windows.Media.Color.FromArgb(0, 0x5B, 0x6A, 0xFF), 1),
                },
                new Point(0, 0),
                new Point(0, 1)
            ),
            Opacity = 0
        };
        RevenueChartCanvas.Children.Add(areaPolygon);

        // Animation cho area
        var areaAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(800))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        areaPolygon.BeginAnimation(UIElement.OpacityProperty, areaAnim);

        // Vẽ Line chính
        var polyline = new WpfPolyline
        {
            Points = points,
            Stroke = new LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(0x5B, 0x6A, 0xFF),
                System.Windows.Media.Color.FromRgb(0x81, 0x8C, 0xF8),
                new Point(0, 0),
                new Point(1, 0)
            ),
            StrokeThickness = 3,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = 0
        };
        RevenueChartCanvas.Children.Add(polyline);

        var lineAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(200)
        };
        polyline.BeginAnimation(UIElement.OpacityProperty, lineAnim);

        // Vẽ các điểm data với glow effect + tooltip
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var pt = dataPoints[i];
            double val = values[i];

            // Glow circle
            var glow = new WpfEllipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0x5B, 0x6A, 0xFF)),
                Opacity = 0
            };
            Canvas.SetLeft(glow, pt.X - 10);
            Canvas.SetTop(glow, pt.Y - 10);
            RevenueChartCanvas.Children.Add(glow);

            // Main dot
            var dot = new WpfEllipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5B, 0x6A, 0xFF)),
                Stroke = new SolidColorBrush(System.Windows.Media.Colors.White),
                StrokeThickness = 2.5,
                Opacity = 0,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Canvas.SetLeft(dot, pt.X - 5);
            Canvas.SetTop(dot, pt.Y - 5);

            // Tooltip
            var tooltipText = $"{labels[i]}: {val * 1000:N0}đ";
            dot.ToolTip = new ToolTip
            {
                Content = tooltipText,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B)),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
            };

            // Hiệu ứng di chuột
            dot.MouseEnter += (s, e) =>
            {
                dot.Width = 14;
                dot.Height = 14;
                Canvas.SetLeft(dot, pt.X - 7);
                Canvas.SetTop(dot, pt.Y - 7);
                glow.Opacity = 1;
            };
            dot.MouseLeave += (s, e) =>
            {
                dot.Width = 10;
                dot.Height = 10;
                Canvas.SetLeft(dot, pt.X - 5);
                Canvas.SetTop(dot, pt.Y - 5);
                glow.Opacity = 0;
            };

            RevenueChartCanvas.Children.Add(dot);

            // Hiệu ứng điểm chấm
            var dotAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = TimeSpan.FromMilliseconds(400 + i * 100),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            dot.BeginAnimation(UIElement.OpacityProperty, dotAnim);

            // Nhãn giá trị phía trên điểm
            if (val > 0)
            {
                var valueLabel = new TextBlock
                {
                    Text = val >= 1000 ? $"{val / 1000:0.#}M" : $"{val:0}k",
                    FontSize = 9,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5B, 0x6A, 0xFF)),
                    Opacity = 0
                };
                Canvas.SetLeft(valueLabel, pt.X - 12);
                Canvas.SetTop(valueLabel, pt.Y - 20);
                RevenueChartCanvas.Children.Add(valueLabel);

                var valAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
                {
                    BeginTime = TimeSpan.FromMilliseconds(600 + i * 100),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                valueLabel.BeginAnimation(UIElement.OpacityProperty, valAnim);
            }
        }

        // Nhãn trục X
        for (int i = 0; i < labels.Length; i++)
        {
            double x = paddingLeft + (i * chartWidth / (values.Length - 1));
            var label = new TextBlock
            {
                Text = labels[i],
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x94, 0xA3, 0xB8)),
                TextAlignment = TextAlignment.Center,
                Width = 40,
            };
            Canvas.SetLeft(label, x - 20);
            Canvas.SetTop(label, paddingTop + chartHeight + 10);
            RevenueChartCanvas.Children.Add(label);
        }
    }
    #endregion

    #region Biểu đồ Trạng thái Kho (WPF Tùy chỉnh - Thanh ngang)
    private void UpdateStockStatusChart(DashboardViewModel vm)
    {
        StockStatusPanel.Children.Clear();

        int total = vm.TongSoNguyenLieu;
        if (total <= 0) total = 1;
        
        // Bình thường = Số lượng có trong kho (SoLuongTonKho) - Tồn kho thấp - Sắp hết hạn - Đã hết hạn
        int normal = Math.Max(vm.SoLuongTonKho - vm.SoLuongTonKhoThap - vm.SoLuongSapHetHan - vm.SoLuongHetHan, 0);

        var categories = new List<(string Label, int Value, string Color, string BgColor, string Icon)>
        {
            ("Bình thường", normal, "#5B6AFF", "#EEF2FF", "✓"),
            ("Tồn kho thấp", vm.SoLuongTonKhoThap, "#F59E0B", "#FFF7ED", "⚠"),
            ("Sắp hết hạn", vm.SoLuongSapHetHan, "#06B6D4", "#ECFEFF", "⏱"),
            ("Đã hết hạn", vm.SoLuongHetHan, "#EF4444", "#FEF2F2", "✕"),
            ("Hết hàng", vm.SoLuongHetHang, "#64748b", "#f1f5f9", "✕")
        };

        // Tổng số ở trên cùng
        var totalPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        var totalHeader = new Grid();
        var totalLabel = new TextBlock
        {
            Text = "Tổng lượng cung hệ thống",
            FontSize = 13,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x64, 0x74, 0x8B)),
        };
        var totalValue = new TextBlock
        {
            Text = vm.TongSoNguyenLieu.ToString(),
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B)),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        totalPanel.Children.Add(totalLabel);
        totalPanel.Children.Add(totalValue);

        // Thanh ngang trang trí (1 màu duy nhất)
        var decorativeBar = new Border
        {
            Height = 8,
            Margin = new Thickness(0, 8, 0, 12),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5B, 0x6A, 0xFF)),
            CornerRadius = new CornerRadius(4),
            Opacity = 0
        };
        
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        decorativeBar.BeginAnimation(UIElement.OpacityProperty, anim);

        totalPanel.Children.Add(decorativeBar);
        StockStatusPanel.Children.Add(totalPanel);

        // Đường phân cách
        var sep = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF1, 0xF5, 0xF9)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        StockStatusPanel.Children.Add(sep);

        // Từng category detail
        int delayIndex = 0;
        string[] statusKeys = ["BinhThuong", "TonThap", "SapHetHan", "HetHan", "HetHang"];
        foreach (var cat in categories)
        {
            double pct = total > 0 ? (double)cat.Value / total * 100 : 0;
            var currentStatusKey = statusKeys[delayIndex];

            var itemGrid = new Grid
            { 
                Margin = new Thickness(0, 0, 0, 14),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Viền bao bọc cho hiệu ứng hover và click
            var clickBorder = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(-6, -4, -6, -4)
            };
            clickBorder.MouseEnter += (s, e) =>
            {
                clickBorder.Background = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(15,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).R,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).G,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).B));
            };
            clickBorder.MouseLeave += (s, e) =>
            {
                clickBorder.Background = Brushes.Transparent;
            };
            clickBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (DataContext is DashboardViewModel dashVm && dashVm.ShowStatusDetailCommand.CanExecute(currentStatusKey))
                    dashVm.ShowStatusDetailCommand.Execute(currentStatusKey);
            };

            // Hàng 1: Icon + Nhãn + Giá trị + Phần trăm
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon chấm tròn
            var dotBorder = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dotBorder, 0);
            infoGrid.Children.Add(dotBorder);

            // Nhãn danh mục
            var catLabel = new TextBlock
            {
                Text = cat.Label,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x47, 0x55, 0x69)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(catLabel, 1);
            infoGrid.Children.Add(catLabel);

            // Giá trị + phần trăm
            var valuePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            valuePanel.Children.Add(new TextBlock
            {
                Text = cat.Value.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)),
                Margin = new Thickness(0, 0, 6, 0)
            });
            valuePanel.Children.Add(new TextBlock
            {
                Text = $"({pct:0.0}%)",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x94, 0xA3, 0xB8)),
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(valuePanel, 2);
            infoGrid.Children.Add(valuePanel);

            Grid.SetRow(infoGrid, 0);
            itemGrid.Children.Add(infoGrid);

            // Hàng 2: Thanh tiến trình
            var progressBg = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.BgColor)),
                Margin = new Thickness(18, 6, 0, 0),
                ClipToBounds = true
            };

            var progressFill = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0, // Sẽ animate
                Background = new LinearGradientBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color),
                    System.Windows.Media.Color.FromArgb(180,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).R,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).G,
                        ((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cat.Color)).B
                    ),
                    new Point(0, 0),
                    new Point(1, 0)
                )
            };
            progressBg.Child = progressFill;

            Grid.SetRow(progressBg, 1);
            itemGrid.Children.Add(progressBg);

            // Opacity animate for the whole item
            itemGrid.Opacity = 0;
            var itemAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(200 + delayIndex * 100),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            itemGrid.BeginAnimation(UIElement.OpacityProperty, itemAnim);

            // Width animate for progress bar (need to wait for actual width)
            progressBg.Loaded += (s, e) =>
            {
                double actualWidth = progressBg.ActualWidth;
                if (actualWidth <= 0) actualWidth = 300;
                double targetWidth = Math.Max(pct / 100.0 * actualWidth, 2);

                var widthAnim = new DoubleAnimation(0, targetWidth, TimeSpan.FromMilliseconds(700))
                {
                    BeginTime = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                progressFill.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
            };

            clickBorder.Child = itemGrid;
            StockStatusPanel.Children.Add(clickBorder);
            delayIndex++;
        }
    }
    #endregion

    #region Top Pizza Bar Chart
    private void UpdateBarChart(DashboardViewModel vm)
    {
        if (_barChart == null) return;

        _barChart.Plot.Clear();

        var data = vm.TopPizzaData;

        if (data.Count == 0)
        {
            var text = _barChart.Plot.Add.Text("Chưa có dữ liệu bán hàng", 0.5, 0.5);
            text.LabelFontSize = 14;
            text.LabelFontColor = ScottPlot.Color.FromHex("#94A3B8");
            text.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
            _barChart.Plot.Axes.SetLimits(0, 1, 0, 1);
            _barChart.Refresh();
            return;
        }

        var colors = new[]
        {
            ScottPlot.Color.FromHex("#5B6AFF"),
            ScottPlot.Color.FromHex("#818CF8"),
            ScottPlot.Color.FromHex("#A78BFA"),
            ScottPlot.Color.FromHex("#C4B5FD"),
            ScottPlot.Color.FromHex("#DDD6FE"),
        };

        var bars = new List<ScottPlot.Bar>();
        var tickLabels = new List<string>();

        for (int i = 0; i < data.Count; i++)
        {
            bars.Add(new ScottPlot.Bar
            {
                Position = i,
                Value = data[i].SoLuongBan,
                FillColor = colors[i % colors.Length],
                LineWidth = 0,
                Size = 0.6,
            });
            tickLabels.Add($"{data[i].TenPizza}\n({data[i].KichThuoc})");
        }

        _barChart.Plot.Add.Bars(bars.ToArray());

        var ticks = new ScottPlot.TickGenerators.NumericManual();
        for (int i = 0; i < tickLabels.Count; i++)
        {
            ticks.AddMajor(i, tickLabels[i]);
        }
        _barChart.Plot.Axes.Bottom.TickGenerator = ticks;
        _barChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 11;
        _barChart.Plot.Axes.SetLimitsY(0, data.Max(d => d.SoLuongBan) * 1.3 + 1);
        _barChart.Plot.Axes.Margins(0.15, 0.1);

        _barChart.Refresh();
    }
    #endregion
}
