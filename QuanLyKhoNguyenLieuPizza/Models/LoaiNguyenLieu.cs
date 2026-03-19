namespace QuanLyKhoNguyenLieuPizza.Models;

public class LoaiNguyenLieu
{
    public int LoaiNLID { get; set; }
    public string TenLoai { get; set; } = string.Empty;

    // Thuộc tính điều hướng
    public virtual ICollection<NguyenLieu> NguyenLieus { get; set; } = new List<NguyenLieu>();
}

