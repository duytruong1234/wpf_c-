namespace QuanLyKhoNguyenLieuPizza.Models;

public class LoaiNguyenLieu
{
    public int LoaiNLID { get; set; }
    public string TenLoai { get; set; } = string.Empty;

    // Navigation property
    public virtual ICollection<NguyenLieu> NguyenLieus { get; set; } = new List<NguyenLieu>();
}

