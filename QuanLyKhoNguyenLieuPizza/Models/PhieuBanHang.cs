namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuBanHang
{
    public string MaPhieuBan { get; set; } = string.Empty;
    public int? NhanVienBanID { get; set; }
    public DateTime? NgayBan { get; set; } = DateTime.Now;
    public decimal? TongTien { get; set; }
    public string? PhuongThucTT { get; set; } = "Tiền mặt";
    public string? GhiChu { get; set; }

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVienBan { get; set; }
    public virtual ICollection<CT_PhieuBan> CT_PhieuBans { get; set; } = new List<CT_PhieuBan>();
}

