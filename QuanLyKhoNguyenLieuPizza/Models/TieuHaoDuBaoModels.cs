using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace QuanLyKhoNguyenLieuPizza.Models;

public static class ForecastMethodHelper
{
    public static string ToBadgeName(int ppId, string? dbName = null) => ppId switch
    {
        1 => "Holt-Winters Cộng",
        2 => "Holt-Winters Nhân",
        3 => "WMA",
        4 => "SES",
        5 => "Holt Linear",
        _ => string.IsNullOrWhiteSpace(dbName) ? "Holt-Winters" : dbName
    };

    public static string ToShortName(int ppId, string? dbName = null) => ppId switch
    {
        1 => "HW Additive",
        2 => "HW Multiplicative",
        3 => "WMA",
        4 => "SES",
        5 => "Holt Linear",
        _ => string.IsNullOrWhiteSpace(dbName) ? "HW Additive" : dbName
    };

    public static string ToChartName(int ppId) => ppId switch
    {
        1 => "Holt-Winters Additive",
        2 => "Holt-Winters Multiplicative",
        3 => "WMA",
        4 => "SES",
        5 => "Holt",
        _ => $"Model #{ppId}"
    };
}

public class PhuongPhapDuBao
{
    public int PhuongPhapID { get; set; }
    public string TenPhuongPhap { get; set; } = string.Empty;
    public string? LoaiMuaVu { get; set; }
    public int? ChuKyMuaVu { get; set; }
    public bool TrangThai { get; set; } = true;
}

public class DuLieuTieuHao
{
    public int TieuHaoID { get; set; }
    public int NguyenLieuID { get; set; }
    public int DonViID { get; set; }
    public DateTime KyTieuHao { get; set; }
    public decimal SoLuongTieuHao { get; set; }
}

public class DanhGiaDuBao
{
    public int DanhGiaID { get; set; }
    public int NguyenLieuID { get; set; }
    public int PhuongPhapID { get; set; }
    public DateTime TuKyTrain { get; set; }
    public DateTime DenKyTrain { get; set; }
    public DateTime TuKyTest { get; set; }
    public DateTime DenKyTest { get; set; }
    public decimal? MAE { get; set; }
    public decimal? RMSE { get; set; }
    public decimal? MAPE { get; set; }
    public DateTime NgayDanhGia { get; set; } = DateTime.Now;
}

public class DuBao
{
    public int DuBaoID { get; set; }
    public int NguyenLieuID { get; set; }
    public int PhuongPhapID { get; set; }
    public DateTime KyDuBao { get; set; }
    public decimal SoLuongDuBao { get; set; }
    public bool LaMoHinhDuocChon { get; set; }
    public DateTime NgayDuBao { get; set; } = DateTime.Now;
    public bool TrangThai { get; set; } = true;
}

public class NguyenLieuFilterOption
{
    public int? NguyenLieuID { get; set; }
    public string TenHienThi { get; set; } = string.Empty;
    public string DisplayText => TenHienThi;
    public override string ToString() => DisplayText;
}

public class KyThoiGianOption
{
    public DateTime KyDate { get; set; }
    public DateTime KyThoiGian => KyDate;
    public string DisplayEnglish { get; set; } = string.Empty;
    public string DisplayMMYYYY { get; set; } = string.Empty;
    public string DisplayMonthVN => $"Tháng {KyDate:MM/yyyy}";
    public string DisplayText => DisplayMonthVN;
    public override string ToString() => DisplayText;
}

public class TieuHaoNguyenLieuItem
{
    public int STT { get; set; }
    public int NguyenLieuID { get; set; }
    public string MaNguyenLieu { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonViKho { get; set; } = string.Empty;
    public int SoMonAnDaBan { get; set; }
    public decimal TongTieuHao { get; set; }

    public decimal TieuHaoThangTruoc { get; set; }
    public decimal TieuHaoCungKyNamTruoc { get; set; }

    // Cột Dự báo kỳ tiếp theo trên Bảng tổng quan
    public int DuBaoPhuongPhapID { get; set; } = 1;
    public string DuBaoTenPhuongPhapBadge { get; set; } = "Holt-Winters Cộng";
    public decimal DuBaoKyToi { get; set; }

    public string SoMonAnText => $"{SoMonAnDaBan:D2} món";
    public string TongTieuHaoText => $"{TongTieuHao:N2} {DonViKho}";
    public string DuBaoKyToiText => DuBaoKyToi > 0 ? $"{DuBaoKyToi:N2} {DonViKho}" : $"0.00 {DonViKho}";

    // So với tháng trước
    public decimal DiffPrevMonth => TongTieuHao - TieuHaoThangTruoc;
    public decimal PctPrevMonth => TieuHaoThangTruoc > 0
        ? Math.Round((TongTieuHao - TieuHaoThangTruoc) / TieuHaoThangTruoc * 100m, 1)
        : (TongTieuHao > 0 ? 100m : 0m);

    public int PrevMonthTrend => Math.Abs(DiffPrevMonth) < 0.005m ? 0 : (DiffPrevMonth > 0 ? 1 : -1);

    public string PrevMonthDiffText => PrevMonthTrend switch
    {
        1 => $"+{DiffPrevMonth:N2} {DonViKho} ▲",
        -1 => $"{DiffPrevMonth:N2} {DonViKho} ▼",
        _ => $"0.00 {DonViKho} ■"
    };

    public string PrevMonthPctText => PrevMonthTrend switch
    {
        1 => $"(+{PctPrevMonth:N1}%)",
        -1 => $"({PctPrevMonth:N1}%)",
        _ => "(0.0%)"
    };

    public string PrevMonthInlineText => $"{PrevMonthDiffText} {PrevMonthPctText}";

    // So với cùng kỳ năm trước
    public decimal DiffPrevYear => TongTieuHao - TieuHaoCungKyNamTruoc;
    public decimal PctPrevYear => TieuHaoCungKyNamTruoc > 0
        ? Math.Round((TongTieuHao - TieuHaoCungKyNamTruoc) / TieuHaoCungKyNamTruoc * 100m, 1)
        : (TongTieuHao > 0 ? 100m : 0m);

    public int PrevYearTrend => Math.Abs(DiffPrevYear) < 0.005m ? 0 : (DiffPrevYear > 0 ? 1 : -1);

    public string PrevYearDiffText => PrevYearTrend switch
    {
        1 => $"+{DiffPrevYear:N2} {DonViKho} ▲",
        -1 => $"{DiffPrevYear:N2} {DonViKho} ▼",
        _ => $"0.00 {DonViKho} ■"
    };

    public string PrevYearPctText => PrevYearTrend switch
    {
        1 => $"(+{PctPrevYear:N1}%)",
        -1 => $"({PctPrevYear:N1}%)",
        _ => "(0.0%)"
    };

    public string PrevYearInlineText => $"{PrevYearDiffText} {PrevYearPctText}";

    public string DiffPrevMonthValueText => PrevMonthDiffText;
    public string DiffPrevMonthPercentText => PrevMonthPctText;
    public string DiffPrevYearValueText => PrevYearDiffText;
    public string DiffPrevYearPercentText => PrevYearPctText;
}

public class MonAnTieuHaoItem
{
    public int STT { get; set; }
    public string MaHangHoa { get; set; } = string.Empty;
    public string TenMonAn { get; set; } = string.Empty;
    public string SizeID { get; set; } = string.Empty;
    public string KichCoText { get; set; } = string.Empty;
    public string DinhMucBomText { get; set; } = string.Empty;
    public string DinhMucBOMText => DinhMucBomText;
    public int SoLuongDaBan { get; set; }
    public decimal TongTieuHaoKho { get; set; }
    public string DonViKho { get; set; } = string.Empty;

    public string SoLuongDaBanText => $"{SoLuongDaBan:N0} cái";
    public string TongTieuHaoKhoText => $"{TongTieuHaoKho:N2} {DonViKho}";
}

public class KetQuaDuBaoItem
{
    public int STT { get; set; }
    public int NguyenLieuID { get; set; }
    public string MaNguyenLieu { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonViKho { get; set; } = string.Empty;

    public decimal TieuHaoKyHienTai { get; set; }
    public decimal DuBaoCongTinh { get; set; }
    public decimal MapeCongTinh { get; set; }
    public decimal RmseCongTinh { get; set; }
    public decimal MaeCongTinh { get; set; }

    public decimal DuBaoNhanTinh { get; set; }
    public decimal MapeNhanTinh { get; set; }
    public decimal RmseNhanTinh { get; set; }
    public decimal MaeNhanTinh { get; set; }

    public string PhuongPhapDuocChon { get; set; } = string.Empty;
    public decimal SoLuongDuBaoChon { get; set; }
    public decimal MapeChon { get; set; }
    public decimal RmseChon { get; set; }
    public decimal MaeChon { get; set; }

    public decimal TonKhoHienTai { get; set; }
    public decimal GoiYNhapHang => Math.Max(0m, Math.Round(SoLuongDuBaoChon - TonKhoHienTai, 2));

    public string TieuHaoHienTaiText => $"{TieuHaoKyHienTai:N2} {DonViKho}";
    public string TieuHaoKyHienTaiText => TieuHaoHienTaiText;
    public string DuBaoCongTinhText => $"{DuBaoCongTinh:N2} {DonViKho}";
    public string DuBaoNhanTinhText => $"{DuBaoNhanTinh:N2} {DonViKho}";
    public decimal MAPECongTinh => MapeCongTinh;
    public decimal MAPENhanTinh => MapeNhanTinh;
    public string MoHinhChon => PhuongPhapDuocChon;
    public string MAPEChonText => $"{MapeChon:N2}%";
    public string MetricCongTinhText => $"MAPE: {MapeCongTinh:N2}% • MAE: {MaeCongTinh:N2}";
    public string MetricNhanTinhText => $"MAPE: {MapeNhanTinh:N2}% • MAE: {MaeNhanTinh:N2}";
    public string MoHinhChonShort => IsAdditiveSelected ? "Cộng tính (Additive)" : "Nhân tính (Multiplicative)";
    public string MetricChonText => $"MAPE: {MapeChon:N2}% • RMSE: {RmseChon:N2}";
    public decimal DiffForecast => SoLuongDuBaoChon - TieuHaoKyHienTai;
    public decimal PctForecast => TieuHaoKyHienTai > 0
        ? Math.Round((SoLuongDuBaoChon - TieuHaoKyHienTai) / TieuHaoKyHienTai * 100m, 1)
        : 0m;
    public int ForecastTrend => Math.Abs(DiffForecast) < 0.005m ? 0 : (DiffForecast > 0 ? 1 : -1);
    public string ForecastDiffPctText => ForecastTrend switch
    {
        1 => $"+{DiffForecast:N2} {DonViKho} ▲ (+{PctForecast:N1}%)",
        -1 => $"{DiffForecast:N2} {DonViKho} ▼ ({PctForecast:N1}%)",
        _ => $"0.00 {DonViKho} ■ (0.0%)"
    };
    public string SoLuongDuBaoChonText => $"{SoLuongDuBaoChon:N2} {DonViKho}";
    public string TonKhoHienTaiText => $"{TonKhoHienTai:N2} {DonViKho}";
    public string GoiYNhapHangText => GoiYNhapHang > 0 ? $"+{GoiYNhapHang:N2} {DonViKho}" : $"Đủ tồn ({0:N2} {DonViKho})";
    public string CanNhapThemText => GoiYNhapHang > 0 ? $"Cần nhập +{GoiYNhapHang:N2} {DonViKho}" : "Đủ tồn kho";
    public double CanNhapThem => (double)GoiYNhapHang;
    public bool IsAdditiveSelected => PhuongPhapDuocChon.Contains("cộng", StringComparison.OrdinalIgnoreCase) || PhuongPhapDuocChon.Contains("Additive", StringComparison.OrdinalIgnoreCase);
    public bool CanImportMore => GoiYNhapHang > 0;
}

public class NamKhaoSatOption
{
    public int Nam { get; set; }
    public string DisplayText => $"Năm {Nam}";
    public override string ToString() => DisplayText;
}

public class YAxisTickItem
{
    public string Label { get; set; } = string.Empty;
}

public class ThangTieuHaoChartItem
{
    public int Thang { get; set; }
    public string ThangLabel => $"T{Thang:D2}";
    public string ThangTableHeader => $"{Thang:D2}";
    public decimal SoLuongTieuHao { get; set; }
    public string DonViKho { get; set; } = string.Empty;

    public string SoLuongTableText => SoLuongTieuHao <= 0
        ? "--"
        : (Math.Abs(SoLuongTieuHao - Math.Round(SoLuongTieuHao)) < 0.05m
            ? $"{Math.Round(SoLuongTieuHao):N0}"
            : $"{SoLuongTieuHao:N1}");

    public string ToolTipText => SoLuongTieuHao > 0
        ? $"Tháng {Thang:D2}: {SoLuongTieuHao:N2} {DonViKho}"
        : $"Tháng {Thang:D2}: Chưa có dữ liệu";

    public bool IsMax { get; set; }
    public bool IsMin { get; set; }
    public double BarHeight { get; set; }
    public bool HasData => SoLuongTieuHao > 0;

    // 1: Cao nhất, -1: Thấp nhất, 0: Bình thường
    public int EvaluationType => IsMax ? 1 : (IsMin ? -1 : 0);
}

public class ThongKeMuaVu12ThangResult
{
    public int NguyenLieuID { get; set; }
    public string MaNguyenLieu { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonViKho { get; set; } = string.Empty;
    public int Nam { get; set; }
    public List<ThangTieuHaoChartItem> ThangItems { get; set; } = new();
}

// =========================================================================
// MODELS CHO TAB "DỰ BÁO TIÊU HAO VÀ ĐỀ XUẤT NHẬP KHO" & POPUP LỊCH SỬ
// =========================================================================

public class ForecastChartSeries : INotifyPropertyChanged
{
    private bool _isVisible = true;
    private string _name = string.Empty;
    private bool _isSelectedModel;
    private Color _lineColor;
    private Brush _lineBrush = Brushes.SteelBlue;

    public int PhuongPhapID { get; set; } // 0 = Thực tế, 1..5 = 5 mô hình
    public bool IsActual { get; set; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsSelectedModel
    {
        get => _isSelectedModel;
        set
        {
            _isSelectedModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashed));
        }
    }

    public bool IsDashed => !IsActual && !IsSelectedModel;

    public Color LineColor
    {
        get => _lineColor;
        set
        {
            _lineColor = value;
            var b = new SolidColorBrush(value);
            b.Freeze();
            _lineBrush = b;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineBrush));
        }
    }

    public Brush LineBrush => _lineBrush;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public double[] Values { get; set; } = Array.Empty<double>();
    public string[] MonthLabels { get; set; } = Array.Empty<string>();
    public string DonViKho { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public class ModelCellValue
{
    public decimal Forecast { get; set; }
    public decimal Actual { get; set; }
    public string DonViKho { get; set; } = string.Empty;

    public string ForecastText => $"{Forecast:N2}";
    public decimal Diff => Math.Round(Forecast - Actual, 2);
    public decimal Ape => Actual > 0.001m ? Math.Round(Math.Abs(Forecast - Actual) / Actual * 100m, 1) : 0m;
    public int DiffSign => Math.Abs(Diff) < 0.005m ? 0 : (Diff > 0 ? 1 : -1);

    public string DiffPctText => DiffSign switch
    {
        1 => $"+{Diff:N2} {DonViKho} ({Ape:N1}%)",
        -1 => $"{Diff:N2} {DonViKho} ({Ape:N1}%)",
        _ => $"0.00 {DonViKho} (0.0%)"
    };
}

public class BacktestMonthRow
{
    public string ThangText { get; set; } = string.Empty;
    public decimal Actual { get; set; }
    public string DonViKho { get; set; } = string.Empty;
    public string ActualText => $"{Actual:N2} {DonViKho}";

    public ModelCellValue Wma { get; set; } = new();
    public ModelCellValue Ses { get; set; } = new();
    public ModelCellValue Holt { get; set; } = new();
    public ModelCellValue HwAdd { get; set; } = new();
    public ModelCellValue HwMul { get; set; } = new();
}

public class ModelEvalSummary
{
    public int PhuongPhapID { get; set; }
    public string ShortName => ForecastMethodHelper.ToShortName(PhuongPhapID);
    public string ChartBaseName => ForecastMethodHelper.ToChartName(PhuongPhapID);
    public string DonViKho { get; set; } = string.Empty;

    public decimal NextForecast { get; set; }
    public decimal Mae { get; set; }
    public decimal Rmse { get; set; }
    public decimal Mape { get; set; }
    public double[] TestPredictions { get; set; } = Array.Empty<double>();

    public string MaeText => $"{Mae:N2} {DonViKho}";
    public string RmseText => $"{Rmse:N2} {DonViKho}";
    public string MapeText => $"{Mape:N2}%";
}

public class SingleIngredientForecastDetail
{
    public int NguyenLieuID { get; set; }
    public string MaNguyenLieu { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonViKho { get; set; } = "kg";

    public DateTime LatestActualKy { get; set; }
    public DateTime NextForecastKy { get; set; }
    public int ActivePhuongPhapID { get; set; } = 2;
    public int OptimalPhuongPhapID { get; set; } = 2;
    public decimal TonKhoHienTai { get; set; }

    // Kết quả dự báo tháng vừa rồi
    public bool HasLastKyData { get; set; }
    public string LastKyMethodName { get; set; } = "--";
    public decimal LastKyForecast { get; set; }
    public decimal LastKyActual { get; set; }

    // Khoảng huấn luyện & kiểm thử
    public DateTime TrainStart { get; set; }
    public DateTime TrainEnd { get; set; }
    public DateTime TestStart { get; set; }
    public DateTime TestEnd { get; set; }
    public int TrainMonthsCount { get; set; }
    public int TestMonthsCount { get; set; }

    public List<string> TestMonthLabels { get; set; } = new();
    public double[] TestActualValues { get; set; } = Array.Empty<double>();
    public Dictionary<int, ModelEvalSummary> Models { get; set; } = new();
    public List<BacktestMonthRow> MonthRows { get; set; } = new();
}

public class LichSuDuBaoRow
{
    public DateTime KyDuBao { get; set; }
    public string KyDuBaoText => $"{KyDuBao:MM/yyyy}";
    public bool IsPending { get; set; }
    public string StatusText => IsPending ? "Đang thực hiện" : "Đã chốt";
    public string NgayThucHienText { get; set; } = string.Empty;
    public string TenPhuongPhap { get; set; } = string.Empty;
    public string TieuHaoDuBaoText { get; set; } = string.Empty;
    public string TieuHaoThucTeText { get; set; } = "--";
    public string SaiSoLechText { get; set; } = "--";
    public string ApeText { get; set; } = "--";
}
