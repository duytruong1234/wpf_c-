namespace QuanLyKhoNguyenLieuPizza.Models;

public class QuyDoiDonVi
{
    public int QuyDoiID { get; set; }
    public int? NguyenLieuID { get; set; }
    public int? DonViID { get; set; }
    public decimal HeSo { get; set; }
    public bool LaDonViChuan { get; set; } = false;

    // Thuộc tính điều hướng
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

