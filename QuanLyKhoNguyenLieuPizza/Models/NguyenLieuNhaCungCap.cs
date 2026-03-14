namespace QuanLyKhoNguyenLieuPizza.Models;

public class NguyenLieuNhaCungCap
{
    public int NguyenLieuID { get; set; }
    public int NhaCungCapID { get; set; }
    public decimal GiaNhap { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual NhaCungCap? NhaCungCap { get; set; }
}

