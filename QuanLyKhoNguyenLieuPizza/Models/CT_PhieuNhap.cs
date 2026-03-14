namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_PhieuNhap
{
    public int ChiTietID { get; set; }
    public int? PhieuNhapID { get; set; }
    public int? NguyenLieuID { get; set; }
    public decimal SoLuong { get; set; }
    public int? DonViID { get; set; }
    public decimal HeSo { get; set; }
    public decimal DonGia { get; set; }
    public decimal? ThanhTien { get; set; }
    public DateTime? HSD { get; set; }

    // Navigation properties
    public virtual PhieuNhap? PhieuNhap { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}
