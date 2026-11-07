namespace QuanLyKhoNguyenLieuPizza.Models;

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
    public string DisplayEnglish { get; set; } = string.Empty; // e.g., "October 2026"
    public string DisplayMMYYYY { get; set; } = string.Empty;  // e.g., "10/2026"
    public string DisplayText => DisplayEnglish;
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

    public string SoMonAnText => $"{SoMonAnDaBan:D2} món";
    public string TongTieuHaoText => $"{TongTieuHao:N2} {DonViKho}";

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
