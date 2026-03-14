namespace QuanLyKhoNguyenLieuPizza.Models;

public class CongThuc_Pizza
{
    public string MaHangHoa { get; set; } = string.Empty;
    public string SizeID { get; set; } = string.Empty;
    public int NguyenLieuID { get; set; }
    public double? SoLuong { get; set; }
    public int? DonViID { get; set; }

    // Navigation properties
    public virtual HangHoa? HangHoa { get; set; }
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

