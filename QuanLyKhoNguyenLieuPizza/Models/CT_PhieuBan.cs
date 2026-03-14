namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_PhieuBan
{
    public int ChiTietBanID { get; set; }
    public string? MaPhieuBan { get; set; }
    public string? MaHangHoa { get; set; }
    public string? SizeID { get; set; }
    public string? MaDeBanh { get; set; }
    public int? SoLuong { get; set; }
    public decimal? ThanhTien { get; set; }

    // Navigation properties
    public virtual PhieuBanHang? PhieuBanHang { get; set; }
    public virtual HangHoa? HangHoa { get; set; }
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
    public virtual DoanhMuc_De? DoanhMucDe { get; set; }
}

