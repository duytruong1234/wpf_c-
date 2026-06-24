namespace QuanLyKhoNguyenLieuPizza.Models;

public class PhieuXuat
{
    public int PhieuXuatID { get; set; }
    public string? MaPhieuXuat { get; set; }
    public int? NhanVienYeuID { get; set; }
    public int? NhanVienDuyetID { get; set; }
    public DateTime NgayYeuCau { get; set; } = DateTime.Now;
    public DateTime? NgayDuyet { get; set; }
    public byte TrangThai { get; set; } = 1;
    public string? GhiChu { get; set; } // Ghi chú / lý do hủy
    public string? ChuKySoDuyet { get; set; } // Chữ ký số duyệt phiếu (Base64 PNG)
    public string? ChuKyXuat { get; set; } // Chữ ký số người xuất
    public string? LoaiPhieu { get; set; } // Loại phiếu: "Xuất kho chế biến", "Xuất hủy hàng", "Xuất trả nhà cung cấp", "Xuất điều chỉnh hao hụt"
    public int? KiemKeID { get; set; } // Liên kết phiếu kiểm kê (nếu sinh từ kiểm kê)

    // Thuộc tính điều hướng
    public virtual NhanVien? NhanVienYeuCau { get; set; }
    public virtual NhanVien? NhanVienDuyet { get; set; }
    public virtual ICollection<CT_PhieuXuat> CT_PhieuXuats { get; set; } = new List<CT_PhieuXuat>();
}
