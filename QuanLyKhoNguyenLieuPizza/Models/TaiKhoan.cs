namespace QuanLyKhoNguyenLieuPizza.Models;

public class TaiKhoan
{
    public int TaiKhoanID { get; set; }
    public int? NhanVienID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool TrangThai { get; set; } = true;

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVien { get; set; }
}

