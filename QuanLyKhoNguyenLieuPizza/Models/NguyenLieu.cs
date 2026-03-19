namespace QuanLyKhoNguyenLieuPizza.Models;

public class NguyenLieu
{
    public int NguyenLieuID { get; set; }
    public string? MaNguyenLieu { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public int? LoaiNLID { get; set; }
    public int? DonViID { get; set; }
    public bool TrangThai { get; set; } = true;

    // Thuộc tính điều hướng
    public virtual LoaiNguyenLieu? LoaiNguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
    public virtual TonKho? TonKho { get; set; }
    public virtual ICollection<NguyenLieuNhaCungCap> NguyenLieuNhaCungCaps { get; set; } = new List<NguyenLieuNhaCungCap>();
    public virtual ICollection<QuyDoiDonVi> QuyDoiDonVis { get; set; } = new List<QuyDoiDonVi>();
    public virtual ICollection<CT_PhieuNhap> CT_PhieuNhaps { get; set; } = new List<CT_PhieuNhap>();
    public virtual ICollection<CT_PhieuXuat> CT_PhieuXuats { get; set; } = new List<CT_PhieuXuat>();
}

