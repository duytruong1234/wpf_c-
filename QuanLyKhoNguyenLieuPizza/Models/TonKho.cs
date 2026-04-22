namespace QuanLyKhoNguyenLieuPizza.Models;

public class TonKho
{
    public int TonKhoID { get; set; }
    public int? NguyenLieuID { get; set; }
    public decimal SoLuongTon { get; set; } = 0;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;
    public int? DonViID { get; set; }
    
    /// <summary>
    /// Hệ số đơn vị chuẩn (từ QuyDoiDonVi), dùng để quy đổi ngược về đơn vị gốc khi tính MucDoTonKho
    /// </summary>
    public decimal HeSoChuan { get; set; } = 1m;

    // Thuộc tính điều hướng
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }
}

