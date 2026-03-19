namespace QuanLyKhoNguyenLieuPizza.Models;

public class QuyDinh_Bot
{
    public string SizeID { get; set; } = string.Empty;
    public string LoaiCotBanh { get; set; } = string.Empty;
    public double? TrongLuongBot { get; set; }
    public int? DonViID { get; set; }

    // Thuộc tính điều hướng
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

