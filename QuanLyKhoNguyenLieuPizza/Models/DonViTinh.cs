namespace QuanLyKhoNguyenLieuPizza.Models;

public class DonViTinh
{
    public int DonViID { get; set; }
    public string TenDonVi { get; set; } = string.Empty;

    // Thuộc tính điều hướng
    public virtual ICollection<NguyenLieu> NguyenLieus { get; set; } = new List<NguyenLieu>();
    public virtual ICollection<QuyDoiDonVi> QuyDoiDonVis { get; set; } = new List<QuyDoiDonVi>();
    public virtual ICollection<CT_PhieuNhap> CT_PhieuNhaps { get; set; } = new List<CT_PhieuNhap>();
    public virtual ICollection<CT_PhieuXuat> CT_PhieuXuats { get; set; } = new List<CT_PhieuXuat>();
}

