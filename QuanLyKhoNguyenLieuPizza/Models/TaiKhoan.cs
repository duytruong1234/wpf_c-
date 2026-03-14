namespace QuanLyKhoNguyenLieuPizza.Models;

public class TaiKhoan
{
    public int TaiKhoanID { get; set; }
    public int? NhanVienID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool TrangThai { get; set; } = true;

    // Navigation property
    public virtual NhanVien? NhanVien { get; set; }
}
