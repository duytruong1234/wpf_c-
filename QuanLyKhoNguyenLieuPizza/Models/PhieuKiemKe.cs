namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuKiemKe
{
    public int KiemKeID { get; set; }
    public string? MaPhieuKiem { get; set; }
    public int? NhanVienKiemID { get; set; }
    public int? NhanVienDuyetID { get; set; }
    public DateTime? NgayKiem { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public string? GhiChuDuyet { get; set; }
    public string? LyDoKiemKe { get; set; }
    public string? LoaiKiemKe { get; set; }
    public string TrangThai { get; set; } = "Bản Nháp"; // Bản Nháp, Chờ Duyệt, Đã Duyệt, Đã Hủy
    public string? ChuKyKiem { get; set; }
    public string? ChuKyDuyet { get; set; }

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVienKiem { get; set; }
    public virtual NhanVien? NhanVienDuyet { get; set; }
    public virtual ICollection<CT_PhieuKiemKe> CT_PhieuKiemKes { get; set; } = new List<CT_PhieuKiemKe>();
}
