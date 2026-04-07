namespace QuanLyKhoNguyenLieuPizza.Models;

public class ChucVu
{
    public int ChucVuID { get; set; }
    public string TenChucVu { get; set; } = string.Empty;

    // Thuộc tính điều hướng
    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}

