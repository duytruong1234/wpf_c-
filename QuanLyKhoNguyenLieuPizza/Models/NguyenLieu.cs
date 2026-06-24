using System.Linq;

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
    public int? HanCanhBao { get; set; } = 7; // Số ngày cảnh báo trước HSD (mặc định 7)

    // Thuộc tính điều hướng
    public virtual LoaiNguyenLieu? LoaiNguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
    public virtual TonKho? TonKho { get; set; }
    public virtual ICollection<NguyenLieuNhaCungCap> NguyenLieuNhaCungCaps { get; set; } = new List<NguyenLieuNhaCungCap>();
    public virtual ICollection<QuyDoiDonVi> QuyDoiDonVis { get; set; } = new List<QuyDoiDonVi>();
    public virtual ICollection<CT_PhieuNhap> CT_PhieuNhaps { get; set; } = new List<CT_PhieuNhap>();
    public virtual ICollection<CT_PhieuXuat> CT_PhieuXuats { get; set; } = new List<CT_PhieuXuat>();

    public override bool Equals(object? obj)
    {
        if (obj is NguyenLieu other)
        {
            if (NguyenLieuID == 0 && other.NguyenLieuID == 0)
                return ReferenceEquals(this, other);
            return NguyenLieuID == other.NguyenLieuID;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return NguyenLieuID == 0 ? base.GetHashCode() : NguyenLieuID.GetHashCode();
    }

    // Computed property cho binding XAML
    public decimal GiaNhap => NguyenLieuNhaCungCaps?.FirstOrDefault()?.GiaNhap ?? 0;
    public int? NhaCungCapMacDinhID => NguyenLieuNhaCungCaps?.FirstOrDefault()?.NhaCungCapID;
    public string TenNhaCungCapMacDinh => NguyenLieuNhaCungCaps?.FirstOrDefault()?.NhaCungCap?.TenNCC ?? string.Empty;
}
