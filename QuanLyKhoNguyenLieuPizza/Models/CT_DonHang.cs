namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_DonHang
{
    public int ChiTietID { get; set; }
    public int DonHangID { get; set; }
    public int PizzaID { get; set; }
    public int SoLuong { get; set; } = 1;
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }

    // Thuộc tính điều hướng
    public virtual DonHang? DonHang { get; set; }
    public virtual Pizza? Pizza { get; set; }
}

