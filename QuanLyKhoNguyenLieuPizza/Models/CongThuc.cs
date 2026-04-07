namespace QuanLyKhoNguyenLieuPizza.Models;

public class CongThuc
{
    public int CongThucID { get; set; }
    public int PizzaID { get; set; }
    public int NguyenLieuID { get; set; }
    public decimal SoLuong { get; set; }
    public int? DonViID { get; set; }

    // Thuộc tính điều hướng
    public virtual Pizza? Pizza { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

