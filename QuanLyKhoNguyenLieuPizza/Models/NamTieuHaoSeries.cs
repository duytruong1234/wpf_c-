using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace QuanLyKhoNguyenLieuPizza.Models;

/// <summary>
/// Một chuỗi tiêu hao 12 tháng của một năm (dùng cho biểu đồ đường so sánh giữa các năm).
/// </summary>
public class NamTieuHaoSeries : INotifyPropertyChanged
{
    private bool _isVisible = true;

    public int Nam { get; set; }
    public string NamLabel => $"Năm {Nam}";
    public string DonViKho { get; set; } = string.Empty;

    public Color LineColor { get; set; } = Colors.SteelBlue;
    public SolidColorBrush LineBrush => new(LineColor);

    /// <summary>12 giá trị theo tháng; null = tháng chưa có dữ liệu.</summary>
    public decimal?[] Values { get; set; } = new decimal?[12];

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnPropertyChanged();
        }
    }

    public IEnumerable<string> ThangTexts =>
        Values.Select(v => v.HasValue ? FormatVal(v.Value) : "--");

    public decimal Tong => Values.Where(v => v.HasValue).Sum(v => v!.Value);
    public int SoThangCoDuLieu => Values.Count(v => v.HasValue);
    public decimal TrungBinh => SoThangCoDuLieu > 0 ? Math.Round(Tong / SoThangCoDuLieu, 1) : 0m;

    public string TongText => $"{Tong:N1} {DonViKho}";
    public string TrungBinhText => $"{TrungBinh:N1} {DonViKho}";

    public static string FormatVal(decimal v) =>
        Math.Abs(v - Math.Round(v)) < 0.05m ? $"{Math.Round(v):N0}" : $"{v:N1}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
