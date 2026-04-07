namespace QuanLyKhoNguyenLieuPizza.Models;

public class TonKho
{
    public int TonKhoID { get; set; }
    public int? NguyenLieuID { get; set; }
    public decimal SoLuongTon { get; set; } = 0;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Thuộc tính điều hướng
    public virtual NguyenLieu? NguyenLieu { get; set; }
}

