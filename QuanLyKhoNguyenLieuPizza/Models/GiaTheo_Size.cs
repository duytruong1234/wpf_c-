namespace QuanLyKhoNguyenLieuPizza.Models;

public class GiaTheo_Size
{
    public string MaHangHoa { get; set; } = string.Empty;
    public string SizeID { get; set; } = string.Empty;
    public decimal? GiaBan { get; set; }

    // Navigation properties
    public virtual HangHoa? HangHoa { get; set; }
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
}

