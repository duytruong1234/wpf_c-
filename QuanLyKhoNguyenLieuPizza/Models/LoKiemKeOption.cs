namespace QuanLyKhoNguyenLieuPizza.Models;

public class LoKiemKeOption
{
    public int ChiTietPhieuNhapID { get; set; }
    public int PhieuNhapID { get; set; }
    public string MaPhieuNhap { get; set; } = string.Empty;
    public DateTime NgayNhap { get; set; }
    public int NguyenLieuID { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public int? DonViID { get; set; }
    public string TenDonVi { get; set; } = string.Empty;
    public decimal HeSo { get; set; } = 1m;
    public decimal SoLuongCon { get; set; }
    public DateTime? HSD { get; set; }

    public string HSDText => HSD?.ToString("dd/MM/yyyy") ?? "Khong co HSD";
    public string DisplayText => $"{MaPhieuNhap} | HSD {HSDText} | Con {SoLuongCon:0.##} {TenDonVi}";
}
