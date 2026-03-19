namespace QuanLyKhoNguyenLieuPizza.Models;

public class DonHang
{
    public int DonHangID { get; set; }
    public string? MaDonHang { get; set; }
    public int? NhanVienID { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhToan { get; set; }
    public string PhuongThucTT { get; set; } = "Tiền mặt";
    public byte TrangThai { get; set; } = 1; // 1: Đang xử lý, 2: Hoàn thành, 3: Hủy
    public string? GhiChu { get; set; }

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVien { get; set; }
    public virtual ICollection<CT_DonHang> CT_DonHangs { get; set; } = new List<CT_DonHang>();
}

