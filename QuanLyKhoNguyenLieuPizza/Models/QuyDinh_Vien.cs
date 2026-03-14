namespace QuanLyKhoNguyenLieuPizza.Models;

public class QuyDinh_Vien
{
    public string MaDeBanh { get; set; } = string.Empty;
    public string SizeID { get; set; } = string.Empty;
    public int NguyenLieuID { get; set; }
    public double? SoLuongVien { get; set; }
    public int? DonViID { get; set; }

    // Navigation properties
    public virtual DoanhMuc_De? DoanhMucDe { get; set; }
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

