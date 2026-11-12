using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Views.Components;

/// <summary>
/// Biểu đồ đường đối chiếu thực nghiệm giữa các mô hình dự báo (WMA, SES, Holt, HW Additive, HW Multiplicative)
/// với Số lượng tiêu hao thực tế trên tập kiểm thử.
/// </summary>
public class ForecastComparisonChart : FrameworkElement
{
    private const double LeftPad = 62;
    private const double RightPad = 18;
    private const double TopPad = 12;
    private const double BottomPad = 28;
    private const int TickCount = 8;

    private static readonly Typeface LabelFace = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Brush AxisTextBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)));
    private static readonly Pen GridPen = Freeze(new Pen(new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)), 1));
    private static readonly Pen AxisPen = Freeze(new Pen(new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)), 1));
    private static readonly DashStyle DashedPattern = Freeze(new DashStyle(new double[] { 4, 3 }, 0));

    private readonly ToolTip _toolTip = new() { Placement = System.Windows.Controls.Primitives.PlacementMode.Relative };
    private readonly List<(double X, int MonthIndex, string Label, List<(string Name, double Val, string Unit)> Entries)> _columns = new();

    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(IEnumerable), typeof(ForecastComparisonChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

    public IEnumerable? Series
    {
        get => (IEnumerable?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public ForecastComparisonChart()
    {
        _toolTip.PlacementTarget = this;
    }

    private static T Freeze<T>(T f) where T : Freezable { f.Freeze(); return f; }

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (ForecastComparisonChart)d;
        if (e.OldValue is INotifyCollectionChanged oldCol) oldCol.CollectionChanged -= chart.OnCollectionChanged;
        if (e.OldValue is IEnumerable oldItems)
            foreach (var s in oldItems.OfType<INotifyPropertyChanged>()) s.PropertyChanged -= chart.OnItemChanged;

        if (e.NewValue is INotifyCollectionChanged newCol) newCol.CollectionChanged += chart.OnCollectionChanged;
        if (e.NewValue is IEnumerable newItems)
            foreach (var s in newItems.OfType<INotifyPropertyChanged>()) s.PropertyChanged += chart.OnItemChanged;

        chart.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (var s in e.OldItems.OfType<INotifyPropertyChanged>()) s.PropertyChanged -= OnItemChanged;
        if (e.NewItems != null)
            foreach (var s in e.NewItems.OfType<INotifyPropertyChanged>()) s.PropertyChanged += OnItemChanged;
        InvalidateVisual();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    private FormattedText Text(string s, double size = 11) =>
        new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, LabelFace, size, AxisTextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        _columns.Clear();

        double w = ActualWidth, h = ActualHeight;
        if (w < LeftPad + RightPad + 30 || h < TopPad + BottomPad + 30) return;

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var all = Series?.OfType<ForecastChartSeries>().ToList() ?? new List<ForecastChartSeries>();
        var visible = all.Where(s => s.IsVisible && s.Values.Length > 0).ToList();
        if (visible.Count == 0) return;

        string[] labels = visible[0].MonthLabels;
        int mCount = labels.Length;
        if (mCount == 0) return;
        string unit = visible[0].DonViKho;

        var allVals = visible.SelectMany(s => s.Values).ToList();
        double rawMin = allVals.Min();
        double rawMax = allVals.Max();
        if (rawMax <= rawMin) { rawMax = rawMin + 10; }

        double span = rawMax - rawMin;
        double paddedMin = Math.Max(0, rawMin - span * 0.14);
        double paddedMax = rawMax + span * 0.14;
        double step = NiceStep((paddedMax - paddedMin) / TickCount);
        double yMin = Math.Floor(paddedMin / step) * step;
        double yMax = yMin + step * TickCount;
        while (yMax < rawMax * 1.02)
        {
            yMax += step;
        }
        int intervals = Math.Max(1, (int)Math.Round((yMax - yMin) / step));

        double plotL = LeftPad, plotR = w - RightPad, plotT = TopPad, plotB = h - BottomPad;
        double plotW = plotR - plotL, plotH = plotB - plotT;

        double YAt(double val) => plotB - ((val - yMin) / (yMax - yMin)) * plotH;
        double XAt(int idx) => mCount == 1 ? plotL + plotW / 2.0 : plotL + plotW * idx / (mCount - 1.0);

        // Lưới ngang + nhãn trục Y
        for (int i = 0; i <= intervals; i++)
        {
            double val = yMin + i * step;
            double y = YAt(val);
            dc.DrawLine(GridPen, new Point(plotL, y), new Point(plotR, y));
            string lbl = Math.Abs(val - Math.Round(val)) < 0.01 ? $"{val:N0} {unit}" : $"{val:N1} {unit}";
            var ft = Text(lbl, 10.5);
            dc.DrawText(ft, new Point(plotL - 8 - ft.Width, y - ft.Height / 2));
        }

        // Nhãn trục X (01/2026 .. 10/2026)
        int labelStride = mCount > 16 ? (int)Math.Ceiling(mCount / 12.0) : 1;
        for (int m = 0; m < mCount; m++)
        {
            double x = XAt(m);
            if (m % labelStride == 0 || m == mCount - 1)
            {
                var ft = Text(labels[m], 11);
                double tx = Math.Min(Math.Max(x - ft.Width / 2, plotL - 12), plotR - ft.Width + 12);
                dc.DrawText(ft, new Point(tx, plotB + 7));
            }

            var colEntries = new List<(string Name, double Val, string Unit)>();
            foreach (var s in visible)
            {
                if (m < s.Values.Length)
                    colEntries.Add((s.Name, s.Values[m], s.DonViKho));
            }
            _columns.Add((x, m, labels[m], colEntries));
        }

        dc.DrawLine(AxisPen, new Point(plotL, plotT), new Point(plotL, plotB));
        dc.DrawLine(AxisPen, new Point(plotL, plotB), new Point(plotR, plotB));

        // Sắp xếp Z-order: mô hình phụ (nét đứt) dưới cùng -> mô hình được chọn (nét liền tím) -> Thực tế (đỏ đậm) trên cùng
        var ordered = visible
            .OrderBy(s => s.IsActual ? 2 : (s.IsSelectedModel ? 1 : 0))
            .ToList();

        foreach (var s in ordered)
        {
            var brush = new SolidColorBrush(s.LineColor);
            brush.Freeze();

            double thickness = s.IsActual ? 3.0 : (s.IsSelectedModel ? 2.2 : 1.55);
            var pen = new Pen(brush, thickness)
            {
                LineJoin = PenLineJoin.Round,
                DashStyle = s.IsDashed ? DashedPattern : DashStyles.Solid
            };
            pen.Freeze();

            int len = Math.Min(mCount, s.Values.Length);
            var pts = new Point[len];
            for (int m = 0; m < len; m++)
            {
                pts[m] = new Point(XAt(m), YAt(s.Values[m]));
            }

            if (len >= 2)
            {
                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(pts[0], false, false);
                    for (int m = 1; m < len; m++)
                        ctx.LineTo(pts[m], true, true);
                }
                geo.Freeze();
                dc.DrawGeometry(null, pen, geo);
            }

            double radius = s.IsActual ? 5.2 : (s.IsSelectedModel ? 4.0 : 2.8);
            var ringPen = Freeze(new Pen(Brushes.White, s.IsActual ? 1.5 : 1.0));
            var hollowPen = Freeze(new Pen(brush, 1.4));

            for (int m = 0; m < len; m++)
            {
                if (s.IsDashed)
                    dc.DrawEllipse(Brushes.White, hollowPen, pts[m], radius, radius);
                else
                    dc.DrawEllipse(brush, ringPen, pts[m], radius, radius);
            }
        }
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0) return 5;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 2.5 ? 2.5 : norm <= 5 ? 5 : 10;
        return nice * mag;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_columns.Count == 0) return;

        var pos = e.GetPosition(this);
        var nearest = _columns
            .Select(c => (c, dist: Math.Abs(c.X - pos.X)))
            .OrderBy(x => x.dist)
            .First();

        if (nearest.dist <= 28 && pos.Y >= TopPad && pos.Y <= ActualHeight - BottomPad + 10)
        {
            var lines = new List<string> { $"Kỳ {nearest.c.Label}" };
            foreach (var entry in nearest.c.Entries)
            {
                lines.Add($"• {entry.Name}: {entry.Val:N2} {entry.Unit}");
            }
            _toolTip.Content = string.Join("\n", lines);
            _toolTip.PlacementTarget = this;
            _toolTip.HorizontalOffset = Math.Min(ActualWidth - 220, nearest.c.X + 12);
            _toolTip.VerticalOffset = Math.Max(10, pos.Y - 48);
            _toolTip.IsOpen = true;
            Cursor = Cursors.Hand;
        }
        else
        {
            _toolTip.IsOpen = false;
            Cursor = null;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _toolTip.IsOpen = false;
    }
}
