using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

/// <summary>
/// Database service interface for dependency injection
/// </summary>
public interface IDatabaseService
{
    // Authentication
    Task<TaiKhoan?> AuthenticateAsync(string username, string password);
    
    // Loai Nguyen Lieu
    Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync();
    Task<bool> SaveLoaiNguyenLieuAsync(LoaiNguyenLieu loaiNguyenLieu);
    Task<bool> DeleteLoaiNguyenLieuAsync(int loaiNLID);
    
    // Nha Cung Cap
    Task<List<NhaCungCap>> GetNhaCungCapsAsync();
    Task<List<NhaCungCap>> GetNhaCungCapsByNguyenLieuAsync(int nguyenLieuId);
    Task<List<NguyenLieuNhaCungCap>> GetNguyenLieuNhaCungCapsAsync(int nguyenLieuId);
    
    // Nguyen Lieu
    Task<List<NguyenLieu>> GetNguyenLieusAsync(int? loaiNLID = null);
    Task<List<NguyenLieu>> GetAllNguyenLieusWithDetailsAsync();
    Task<NguyenLieu?> GetNguyenLieuByIdAsync(int id);
    Task<bool> SaveNguyenLieuAsync(NguyenLieu nguyenLieu);
    Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId);
    
    // Ton Kho
    Task<List<TonKho>> GetTonKhosAsync();
    Task<TonKho?> GetTonKhoByNguyenLieuIdAsync(int nguyenLieuId);
    Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong);
    
    // Don Vi Tinh
    Task<List<DonViTinh>> GetDonViTinhsAsync();
    
    // Quy Doi Don Vi
    Task<List<QuyDoiDonVi>> GetQuyDoiDonVisAsync(int nguyenLieuID);
    Task<bool> SaveQuyDoiDonViAsync(QuyDoiDonVi quyDoi);
    Task<bool> DeleteQuyDoiDonViAsync(int quyDoiId);
    
    // Statistics for Dashboard
    Task<int> GetTotalTonKhoCountAsync();
    Task<int> GetLowStockCountAsync(decimal threshold = 20);
    Task<int> GetNearExpiryCountAsync(int days = 7);
    Task<int> GetExpiredCountAsync();

    // User Management
    Task<List<ChucVu>> GetChucVusAsync();
    Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId);
    Task<bool> ChangePasswordAsync(string email, string newPassword);

    // Pizza
    Task<List<Pizza>> GetPizzasAsync();
    Task<Pizza?> GetPizzaByIdAsync(int pizzaId);
    Task<bool> SavePizzaAsync(Pizza pizza);
    Task<bool> DeletePizzaAsync(int pizzaId);
    Task<bool> DeletePizzaByMaAsync(string maHangHoa);

    // Cong Thuc (Recipe)
    Task<List<CongThuc>> GetCongThucsAsync(int pizzaId);
    Task<bool> SaveCongThucAsync(CongThuc congThuc);
    Task<bool> DeleteCongThucAsync(int congThucId);
    Task<decimal> CalculateGiaVonAsync(int pizzaId);
    Task<decimal> CalculateGiaVonByMaAsync(string maHangHoa, string sizeId);

    // Don Hang (Order)
    Task<List<DonHang>> GetDonHangsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<DonHang?> GetDonHangByIdAsync(int donHangId);
    Task<List<CT_DonHang>> GetDonHangChiTietsAsync(int donHangId);
    Task<int> SaveDonHangAsync(DonHang donHang, List<CT_DonHang> chiTiets);
    Task<bool> UpdateDonHangStatusAsync(int donHangId, byte trangThai);

    // Sales Statistics
    Task<decimal> GetDoanhThuAsync(DateTime fromDate, DateTime toDate);
    Task<int> GetTotalDonHangCountAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetTotalLoiNhuanAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetChiPhiNguyenLieuAsync(DateTime fromDate, DateTime toDate);
    Task<List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)>> GetTopPizzasAsync(DateTime fromDate, DateTime toDate, int top = 5);
    Task<List<DonHang>> GetRecentDonHangsAsync(int top = 10);

    // HangHoa (Product)
    Task<List<HangHoa>> GetHangHoasAsync();
    Task<HangHoa?> GetHangHoaByIdAsync(string maHangHoa);

    // DoanhMuc_Size & DoanhMuc_De
    Task<List<DoanhMuc_Size>> GetDoanhMucSizesAsync();
    Task<List<DoanhMuc_De>> GetDoanhMucDesAsync();

    // GiaTheo_Size & GiaTheo_De
    Task<List<GiaTheo_Size>> GetGiaTheoSizeByHangHoaAsync(string maHangHoa);
    Task<List<GiaTheo_De>> GetGiaTheoDeAsync(string sizeId);

    // PhieuBanHang (Sales Receipt)
    Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<PhieuBanHang?> GetPhieuBanHangByIdAsync(string maPhieuBan);
    Task<List<CT_PhieuBan>> GetChiTietPhieuBanAsync(string maPhieuBan);
    Task<string> SavePhieuBanHangAsync(PhieuBanHang phieuBan, List<CT_PhieuBan> chiTiets);
    Task<string> GenerateMaPhieuBanAsync();

    // CongThuc_Pizza
    Task<List<CongThuc_Pizza>> GetCongThucPizzaAsync(string maHangHoa, string sizeId);

    // Sales Statistics (PhieuBanHang)
    Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate);
    Task<int> GetTotalPhieuBanCountAsync(DateTime fromDate, DateTime toDate);
}


