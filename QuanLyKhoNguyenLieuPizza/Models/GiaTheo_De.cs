namespace QuanLyKhoNguyenLieuPizza.Models;

public class GiaTheo_De
{
    public string SizeID { get; set; } = string.Empty;
    public string MaDeBanh { get; set; } = string.Empty;
    public decimal? GiaThem { get; set; }

    // Navigation properties
    public virtual DoanhMuc_Size? DoanhMucSize { get; set; }
    public virtual DoanhMuc_De? DoanhMucDe { get; set; }
}

