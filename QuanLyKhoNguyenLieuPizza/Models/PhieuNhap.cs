namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuNhap
{
    public int PhieuNhapID { get; set; }
    public string? MaPhieuNhap { get; set; }
    public int? NhanVienNhapID { get; set; }
    public int? NhaCungCapID { get; set; }
    public int? NhanVienDuyetID { get; set; }
    public DateTime NgayNhap { get; set; } = DateTime.Now;
    public DateTime? NgayDuyet { get; set; }
    public decimal TongTien { get; set; } = 0;
    public byte TrangThai { get; set; } = 1; // 1: Chờ duyệt, 2: Đã duyệt, 3: Đã hủy
    public string? GhiChu { get; set; } // Lý do hủy phiếu (3.1.4)

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVienNhap { get; set; }
    public virtual NhanVien? NhanVienDuyet { get; set; }
    public virtual NhaCungCap? NhaCungCap { get; set; }
    public virtual ICollection<CT_PhieuNhap> CT_PhieuNhaps { get; set; } = new List<CT_PhieuNhap>();
}

