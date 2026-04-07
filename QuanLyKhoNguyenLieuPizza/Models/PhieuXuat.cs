namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuXuat
{
    public int PhieuXuatID { get; set; }
    public string? MaPhieuXuat { get; set; }
    public int? NhanVienYeuID { get; set; }
    public int? NhanVienDuyetID { get; set; }
    public DateTime NgayYeuCau { get; set; } = DateTime.Now;
    public DateTime? NgayDuyet { get; set; }
    public byte TrangThai { get; set; } = 1;

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVienYeuCau { get; set; }
    public virtual NhanVien? NhanVienDuyet { get; set; }
    public virtual ICollection<CT_PhieuXuat> CT_PhieuXuats { get; set; } = new List<CT_PhieuXuat>();
}

