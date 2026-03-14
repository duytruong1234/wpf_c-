namespace QuanLyKhoNguyenLieuPizza.Models;

public class HangHoa
{
    public string MaHangHoa { get; set; } = string.Empty;
    public string? TenHangHoa { get; set; }
    public string? HinhAnh { get; set; }
    public int? DonViID { get; set; }
    public string? LoaiHangHoaID { get; set; }
    public bool? TinhTrang { get; set; } = true;

    // Navigation properties
    public virtual DonViTinh? DonViTinh { get; set; }
    public virtual LoaiHangHoa? LoaiHangHoa { get; set; }
    public virtual ICollection<GiaTheo_Size> GiaTheoSizes { get; set; } = new List<GiaTheo_Size>();
    public virtual ICollection<CongThuc_Pizza> CongThucs { get; set; } = new List<CongThuc_Pizza>();
}
