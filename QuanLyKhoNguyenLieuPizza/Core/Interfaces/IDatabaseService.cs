using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

/// <summary>
/// Interface dịch vụ cơ sở dữ liệu cho dependency injection
/// </summary>
public interface IDatabaseService
{
    // Xác thực
    Task<TaiKhoan?> AuthenticateAsync(string username, string password);
    
    // Loại Nguyên Liệu
    Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync();
    Task<bool> SaveLoaiNguyenLieuAsync(LoaiNguyenLieu loaiNguyenLieu);
    Task<bool> DeleteLoaiNguyenLieuAsync(int loaiNLID);
    
    // Nhà Cung Cấp
    Task<List<NhaCungCap>> GetNhaCungCapsAsync();
    Task<List<NhaCungCap>> GetNhaCungCapsByNguyenLieuAsync(int nguyenLieuId);
    Task<List<NguyenLieuNhaCungCap>> GetNguyenLieuNhaCungCapsAsync(int nguyenLieuId);
    Task<bool> UpsertNguyenLieuNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId, decimal giaNhap);
    
    // Nguyên Liệu
    Task<List<NguyenLieu>> GetNguyenLieusAsync(int? loaiNLID = null);
    Task<List<NguyenLieu>> GetAllNguyenLieusWithDetailsAsync();
    Task<NguyenLieu?> GetNguyenLieuByIdAsync(int id);
    Task<bool> SaveNguyenLieuAsync(NguyenLieu nguyenLieu);
    Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId);
    
    // Tồn Kho
    Task<List<TonKho>> GetTonKhosAsync();
    Task<TonKho?> GetTonKhoByNguyenLieuIdAsync(int nguyenLieuId);
    Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong);
    
    // Đơn Vị Tính
    Task<List<DonViTinh>> GetDonViTinhsAsync();
    
    // Quy Đổi Đơn Vị
    Task<List<QuyDoiDonVi>> GetQuyDoiDonVisAsync(int nguyenLieuID);
    Task<bool> SaveQuyDoiDonViAsync(QuyDoiDonVi quyDoi);
    Task<bool> DeleteQuyDoiDonViAsync(int quyDoiId);
    
    // Thống kê cho Dashboard
    Task<int> GetTotalNguyenLieuCountAsync();
    Task<int> GetTotalTonKhoCountAsync();
    Task<int> GetLowStockCountAsync(decimal threshold = 20);
    Task<int> GetNearExpiryCountAsync(int days = 7);
    Task<int> GetExpiredCountAsync();
    Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetLowStockItemsAsync(decimal threshold = 20);
    Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNearExpiryItemsAsync(int days = 7);
    Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetExpiredItemsAsync();
    Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNormalStockItemsAsync(decimal lowThreshold = 20);
    Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetOutOfStockItemsAsync();

    // Quản lý người dùng
    Task<List<NhanVien>> GetNhanViensAsync();
    Task<List<ChucVu>> GetChucVusAsync();
    Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId);
    Task<bool> ChangePasswordAsync(string email, string newPassword);

    // Pizza
    Task<List<Pizza>> GetPizzasAsync();
    Task<Pizza?> GetPizzaByIdAsync(int pizzaId);
    Task<bool> SavePizzaAsync(Pizza pizza);
    Task<bool> DeletePizzaAsync(int pizzaId);
    Task<bool> DeletePizzaByMaAsync(string maHangHoa);
    Task<bool> TogglePizzaTrangThaiAsync(string maHangHoa, bool newStatus);

    // Công Thức (Đơn)
    Task<List<CongThuc>> GetCongThucsAsync(int pizzaId);
    Task<bool> SaveCongThucAsync(CongThuc congThuc);
    Task<bool> DeleteCongThucAsync(int congThucId);
    Task<decimal> CalculateGiaVonAsync(int pizzaId);
    Task<decimal> CalculateGiaVonByMaAsync(string maHangHoa, string sizeId);

    // Đơn Hàng
    Task<List<DonHang>> GetDonHangsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<DonHang?> GetDonHangByIdAsync(int donHangId);
    Task<List<CT_DonHang>> GetDonHangChiTietsAsync(int donHangId);
    Task<int> SaveDonHangAsync(DonHang donHang, List<CT_DonHang> chiTiets);
    Task<bool> UpdateDonHangStatusAsync(int donHangId, byte trangThai);
    Task<bool> DeleteDonHangAsync(DonHang donHang);

    // Thống kê bán hàng
    Task<decimal> GetDoanhThuAsync(DateTime fromDate, DateTime toDate);
    Task<int> GetTotalDonHangCountAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetTotalLoiNhuanAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetChiPhiNguyenLieuAsync(DateTime fromDate, DateTime toDate);
    Task<List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)>> GetTopPizzasAsync(DateTime fromDate, DateTime toDate, int top = 5);
    Task<List<DonHang>> GetRecentDonHangsAsync(int top = 10);

    // Hàng Hóa (Sản phẩm)
    Task<List<HangHoa>> GetHangHoasAsync();
    Task<HangHoa?> GetHangHoaByIdAsync(string maHangHoa);
    Task<Dictionary<string, List<string>>> GetOutOfStockIngredientsByHangHoaAsync(IEnumerable<string> maHangHoas);

    // DoanhMuc_Size & DoanhMuc_De
    Task<List<DoanhMuc_Size>> GetDoanhMucSizesAsync();
    Task<List<DoanhMuc_De>> GetDoanhMucDesAsync();
    
    // Loại Hàng Hóa
    Task<List<LoaiHangHoa>> GetLoaiHangHoasAsync();

    // GiaTheo_Size & GiaTheo_De
    Task<List<GiaTheo_Size>> GetGiaTheoSizeByHangHoaAsync(string maHangHoa);
    Task<List<GiaTheo_De>> GetGiaTheoDeAsync(string sizeId);

    // Phiếu Bán Hàng
    Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<PhieuBanHang?> GetPhieuBanHangByIdAsync(string maPhieuBan);
    Task<List<CT_PhieuBan>> GetChiTietPhieuBanAsync(string maPhieuBan);
    Task<string> SavePhieuBanHangAsync(PhieuBanHang phieuBan, List<CT_PhieuBan> chiTiets);
    Task<string> GenerateMaPhieuBanAsync();
    Task<bool> UpdatePhieuBanHangAsync(PhieuBanHang pb);
    Task<bool> DeletePhieuBanHangAsync(string maPhieuBan);

    // CongThuc_Pizza
    Task<List<CongThuc_Pizza>> GetCongThucPizzaAsync(string maHangHoa, string sizeId);
    Task<bool> SaveCongThucPizzaAsync(CongThuc_Pizza congThuc);
    Task<bool> DeleteCongThucPizzaAsync(string maHangHoa, string sizeId, int nguyenLieuId);

    // Thống kê bán hàng (PhiếuBánHàng)
    Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate);
    Task<int> GetTotalPhieuBanCountAsync(DateTime fromDate, DateTime toDate);

    // Phiếu Nhập
    Task<List<PhieuNhap>> GetPhieuNhapsAsync(string? searchText = null, int? nhanVienId = null, int? nhaCungCapId = null, DateTime? tuNgay = null, DateTime? denNgay = null, List<byte>? trangThaiFilter = null);
    Task<PhieuNhap?> GetPhieuNhapByIdAsync(int phieuNhapId);
    Task<List<CT_PhieuNhap>> GetChiTietPhieuNhapAsync(int phieuNhapId);
    Task<bool> DeletePhieuNhapAsync(int phieuNhapId);
    Task<bool> ApprovePhieuNhapAsync(int phieuNhapId, int nguoiDuyetId);
    Task<bool> CancelPhieuNhapAsync(int phieuNhapId, int nguoiHuyId, string? lyDoHuy = null);
    Task<string> GenerateMaPhieuNhapAsync();
    Task<int> SavePhieuNhapAsync(PhieuNhap phieuNhap, List<CT_PhieuNhap> chiTiets);
    Task<decimal> GetTotalTongTienPhieuNhapAsync(int? nhanVienId = null, int? nhaCungCapId = null, DateTime? tuNgay = null, DateTime? denNgay = null);
    Task<List<NguyenLieu>> GetAllNguyenLieusWithPriceAsync();
    Task<List<NguyenLieu>> GetNguyenLieusByNhaCungCapAsync(int nhaCungCapId);
}



