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
/// Biểu đồ đường nhiều chuỗi (mỗi chuỗi = 1 năm, 12 điểm tháng) vẽ trực tiếp bằng DrawingContext.
/// </summary>
public class MultiLineChart : FrameworkElement
{
    private const double LeftPad = 52;
    private const double RightPad = 14;
    private const double TopPad = 10;
    private const double BottomPad = 26;
    private const int TickCount = 8; // số khoảng chia trục Y

    private static readonly Typeface LabelFace = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Brush AxisTextBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)));
    private static readonly Pen GridPen = Freeze(new Pen(new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)), 1));
    private static readonly Pen AxisPen = Freeze(new Pen(new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)), 1));

    private readonly ToolTip _toolTip = new() { Placement = System.Windows.Controls.Primitives.PlacementMode.Relative };
    private readonly List<(Point Pt, string Text)> _hitPoints = new();

    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(IEnumerable), typeof(MultiLineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

    public IEnumerable? Series
    {
        get => (IEnumerable?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public MultiLineChart()
    {
        _toolTip.PlacementTarget = this;
    }

    private static T Freeze<T>(T f) where T : Freezable { f.Freeze(); return f; }

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (MultiLineChart)d;
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
        _hitPoints.Clear();

        double w = ActualWidth, h = ActualHeight;
        if (w < LeftPad + RightPad + 20 || h < TopPad + BottomPad + 20) return;

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h)); // nhận sự kiện chuột

        var all = Series?.OfType<NamTieuHaoSeries>().ToList() ?? new List<NamTieuHaoSeries>();
        var visible = all.Where(s => s.IsVisible).ToList();
        string unit = all.FirstOrDefault()?.DonViKho ?? string.Empty;

        double rawMax = visible.SelectMany(s => s.Values).Where(v => v.HasValue).Select(v => (double)v!.Value).DefaultIfEmpty(0).Max();
        if (rawMax <= 0) rawMax = 80;
        double step = NiceStep(rawMax * 1.05 / TickCount);
        double yMax = step * TickCount;

        double plotL = LeftPad, plotR = w - RightPad, plotT = TopPad, plotB = h - BottomPad;
        double plotW = plotR - plotL, plotH = plotB - plotT;

        // Lưới ngang + nhãn trục Y
        for (int i = 0; i <= TickCount; i++)
        {
            double val = i * step;
            double y = plotB - (val / yMax) * plotH;
            dc.DrawLine(GridPen, new Point(plotL, y), new Point(plotR, y));
            var ft = Text(val == Math.Floor(val) ? $"{val:N0} {unit}" : $"{val:N1} {unit}", 10.5);
            dc.DrawText(ft, new Point(plotL - 8 - ft.Width, y - ft.Height / 2));
        }

        // Lưới dọc + nhãn trục X (T01..T12)
        double XAt(int m) => plotL + plotW * m / 11.0;
        for (int m = 0; m < 12; m++)
        {
            double x = XAt(m);
            dc.DrawLine(GridPen, new Point(x, plotT), new Point(x, plotB));
            var ft = Text($"T{m + 1:D2}", 11);
            double tx = Math.Min(Math.Max(x - ft.Width / 2, plotL - 6), plotR - ft.Width + 6);
            dc.DrawText(ft, new Point(tx, plotB + 6));
        }
        dc.DrawLine(AxisPen, new Point(plotL, plotT), new Point(plotL, plotB));
        dc.DrawLine(AxisPen, new Point(plotL, plotB), new Point(plotR, plotB));

        // Các đường
        foreach (var s in visible)
        {
            var brush = new SolidColorBrush(s.LineColor); brush.Freeze();
            var pen = new Pen(brush, 2) { LineJoin = PenLineJoin.Round }; pen.Freeze();

            var pts = new List<Point?>();
            for (int m = 0; m < 12; m++)
            {
                var v = s.Values[m];
                pts.Add(v.HasValue ? new Point(XAt(m), plotB - ((double)v.Value / yMax) * plotH) : null);
            }

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                bool started = false;
                foreach (var p in pts)
                {
                    if (p == null) { started = false; continue; }
                    if (!started) { ctx.BeginFigure(p.Value, false, false); started = true; }
                    else ctx.LineTo(p.Value, true, true);
                }
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);

            for (int m = 0; m < 12; m++)
            {
                if (pts[m] is not Point p) continue;
                dc.DrawEllipse(brush, new Pen(Brushes.White, 1.5), p, 4.5, 4.5);
                _hitPoints.Add((p, $"{s.NamLabel} - Tháng {m + 1:D2}: {NamTieuHaoSeries.FormatVal(s.Values[m]!.Value)} {s.DonViKho}"));
            }
        }
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0) return 10;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 2.5 ? 2.5 : norm <= 5 ? 5 : 10;
        return nice * mag;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);
        var hit = _hitPoints
            .Select(hp => (hp, d: (hp.Pt - pos).Length))
            .Where(x => x.d <= 10)
            .OrderBy(x => x.d)
            .Select(x => x.hp)
            .FirstOrDefault();

        if (hit.Text != null)
        {
            _toolTip.Content = hit.Text;
            _toolTip.PlacementTarget = this;
            _toolTip.HorizontalOffset = hit.Pt.X + 10;
            _toolTip.VerticalOffset = hit.Pt.Y - 34;
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
