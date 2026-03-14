namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_PhieuXuat
{
    public int ChiTietID { get; set; }
    public int? PhieuXuatID { get; set; }
    public int? NguyenLieuID { get; set; }
    public decimal SoLuong { get; set; }
    public int? DonViID { get; set; }
    public decimal HeSo { get; set; }

    // Navigation properties
    public virtual PhieuXuat? PhieuXuat { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}
