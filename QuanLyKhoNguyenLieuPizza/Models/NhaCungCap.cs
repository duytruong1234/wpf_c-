namespace QuanLyKhoNguyenLieuPizza.Models;

public class NhaCungCap
{
    public int NhaCungCapID { get; set; }
    public string TenNCC { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public bool TrangThai { get; set; } = true;

    // Thuộc tính điều hướng
    public virtual ICollection<NguyenLieuNhaCungCap> NguyenLieuNhaCungCaps { get; set; } = new List<NguyenLieuNhaCungCap>();
    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
}

