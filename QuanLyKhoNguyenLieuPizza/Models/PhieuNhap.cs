namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuNhap
{
    public int PhieuNhapID { get; set; }
    public string? MaPhieuNhap { get; set; }
    public int? NhanVienNhapID { get; set; }
    public int? NhaCungCapID { get; set; }
    public DateTime NgayNhap { get; set; } = DateTime.Now;
    public decimal TongTien { get; set; } = 0;
    public byte TrangThai { get; set; } = 1;

    // Navigation properties
    public virtual NhanVien? NhanVienNhap { get; set; }
    public virtual NhaCungCap? NhaCungCap { get; set; }
    public virtual ICollection<CT_PhieuNhap> CT_PhieuNhaps { get; set; } = new List<CT_PhieuNhap>();
}
