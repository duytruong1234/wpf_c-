namespace QuanLyKhoNguyenLieuPizza.Models;

public class Pizza
{
    public int PizzaID { get; set; }
    public string? MaPizza { get; set; }
    public string TenPizza { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public string KichThuoc { get; set; } = "M";
    public string? SizeID { get; set; }
    public decimal GiaBan { get; set; }
    public bool TrangThai { get; set; } = true;

    // Calculated properties
    public decimal GiaVon { get; set; }
    public decimal LoiNhuan => GiaBan - GiaVon;
    public double TyLeLoi => GiaBan > 0 ? (double)((GiaBan - GiaVon) / GiaBan * 100) : 0;

    // Navigation properties
    public virtual ICollection<CongThuc> CongThucs { get; set; } = new List<CongThuc>();
    public virtual ICollection<CT_DonHang> CT_DonHangs { get; set; } = new List<CT_DonHang>();
}
