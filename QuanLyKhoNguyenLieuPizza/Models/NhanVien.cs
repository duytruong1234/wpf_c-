namespace QuanLyKhoNguyenLieuPizza.Models;

public class NhanVien
{
    public int NhanVienID { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? DiaChi { get; set; }
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public int? ChucVuID { get; set; }
    public bool TrangThai { get; set; } = true;
    public string? HinhAnh { get; set; }

    // Navigation properties
    public virtual ChucVu? ChucVu { get; set; }
    public virtual TaiKhoan? TaiKhoan { get; set; }
    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
    public virtual ICollection<PhieuXuat> PhieuXuatYeuCaus { get; set; } = new List<PhieuXuat>();
    public virtual ICollection<PhieuXuat> PhieuXuatDuyets { get; set; } = new List<PhieuXuat>();
}

