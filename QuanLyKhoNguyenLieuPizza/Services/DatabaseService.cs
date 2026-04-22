using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Core.Interfaces;
using QuanLyKhoNguyenLieuPizza.Services.Repositories;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Facade class ủy quyền (delegate) mọi method calls xuống các service con.
/// Giữ nguyên interface IDatabaseService để backward-compatible với tất cả ViewModels.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    // Sub-services
    private readonly AuthService _auth;
    private readonly NguyenLieuService _nguyenLieu;
    private readonly TonKhoService _tonKho;
    private readonly PhieuNhapService _phieuNhap;
    private readonly PhieuXuatService _phieuXuat;
    private readonly NhaCungCapService _nhaCungCap;
    private readonly NhanVienService _nhanVien;
    private readonly PizzaService _pizza;
    private readonly BanHangService _banHang;
    private readonly DashboardService _dashboard;

    public DatabaseService()
    {
        _connectionString = ConfigurationService.Instance.GetConnectionString();
        _auth = new AuthService(_connectionString);
        _nguyenLieu = new NguyenLieuService(_connectionString);
        _tonKho = new TonKhoService(_connectionString);
        _phieuNhap = new PhieuNhapService(_connectionString);
        _phieuXuat = new PhieuXuatService(_connectionString);
        _nhaCungCap = new NhaCungCapService(_connectionString);
        _nhanVien = new NhanVienService(_connectionString);
        _pizza = new PizzaService(_connectionString);
        _banHang = new BanHangService(_connectionString);
        _dashboard = new DashboardService(_connectionString);
    }

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        _auth = new AuthService(_connectionString);
        _nguyenLieu = new NguyenLieuService(_connectionString);
        _tonKho = new TonKhoService(_connectionString);
        _phieuNhap = new PhieuNhapService(_connectionString);
        _phieuXuat = new PhieuXuatService(_connectionString);
        _nhaCungCap = new NhaCungCapService(_connectionString);
        _nhanVien = new NhanVienService(_connectionString);
        _pizza = new PizzaService(_connectionString);
        _banHang = new BanHangService(_connectionString);
        _dashboard = new DashboardService(_connectionString);
    }

    // ═══════════════ AUTH ═══════════════
    public Task<TaiKhoan?> AuthenticateAsync(string username, string password)
        => _auth.AuthenticateAsync(username, password);
    public Task<List<ChucVu>> GetChucVusAsync()
        => _auth.GetChucVusAsync();
    public Task<NhanVien?> VerifyUserInfoAsync(string email, string hoTen, DateTime ngaySinh, int chucVuId)
        => _auth.VerifyUserInfoAsync(email, hoTen, ngaySinh, chucVuId);
    public Task<bool> ChangePasswordAsync(string email, string newPassword)
        => _auth.ChangePasswordAsync(email, newPassword);

    // ═══════════════ NGUYÊN LIỆU ═══════════════
    public Task<List<LoaiNguyenLieu>> GetLoaiNguyenLieusAsync()
        => _nguyenLieu.GetLoaiNguyenLieusAsync();
    public Task<bool> SaveLoaiNguyenLieuAsync(LoaiNguyenLieu loaiNguyenLieu)
        => _nguyenLieu.SaveLoaiNguyenLieuAsync(loaiNguyenLieu);
    public Task<bool> DeleteLoaiNguyenLieuAsync(int loaiNLID)
        => _nguyenLieu.DeleteLoaiNguyenLieuAsync(loaiNLID);
    public Task<List<NguyenLieu>> GetNguyenLieusAsync(int? loaiNLID = null)
        => _nguyenLieu.GetNguyenLieusAsync(loaiNLID);
    public Task<List<NguyenLieu>> GetAllNguyenLieusWithDetailsAsync()
        => _nguyenLieu.GetAllNguyenLieusWithDetailsAsync();
    public Task<NguyenLieu?> GetNguyenLieuByIdAsync(int id)
        => _nguyenLieu.GetNguyenLieuByIdAsync(id);
    public Task<bool> SaveNguyenLieuAsync(NguyenLieu nguyenLieu)
        => _nguyenLieu.SaveNguyenLieuAsync(nguyenLieu);
    public Task<bool> DeleteNguyenLieuAsync(int nguyenLieuId)
        => _nguyenLieu.DeleteNguyenLieuAsync(nguyenLieuId);
    public Task<List<DonViTinh>> GetDonViTinhsAsync()
        => _nguyenLieu.GetDonViTinhsAsync();
    public Task<List<QuyDoiDonVi>> GetQuyDoiDonVisAsync(int nguyenLieuID)
        => _nguyenLieu.GetQuyDoiDonVisAsync(nguyenLieuID);
    public Task<bool> SaveQuyDoiDonViAsync(QuyDoiDonVi quyDoi)
        => _nguyenLieu.SaveQuyDoiDonViAsync(quyDoi);
    public Task<bool> DeleteQuyDoiDonViAsync(int quyDoiId)
        => _nguyenLieu.DeleteQuyDoiDonViAsync(quyDoiId);
    public Task<bool> UpdateTonKhoAsync(int nguyenLieuId, decimal soLuong)
        => _nguyenLieu.UpdateTonKhoAsync(nguyenLieuId, soLuong);
    public Task<bool> UpdateTonKhoDonViAsync(int nguyenLieuId, decimal soLuong, int donViId)
        => _nguyenLieu.UpdateTonKhoDonViAsync(nguyenLieuId, soLuong, donViId);

    // ═══════════════ TỒN KHO ═══════════════
    public Task<List<TonKho>> GetTonKhosAsync()
        => _tonKho.GetTonKhosAsync();
    public Task<TonKho?> GetTonKhoByNguyenLieuIdAsync(int nguyenLieuId)
        => _nguyenLieu.GetTonKhoByNguyenLieuIdAsync(nguyenLieuId);

    // ═══════════════ NHÀ CUNG CẤP ═══════════════
    public Task<List<NhaCungCap>> GetNhaCungCapsAsync()
        => _nhaCungCap.GetNhaCungCapsAsync();
    public Task<List<NhaCungCap>> GetNhaCungCapsByNguyenLieuAsync(int nguyenLieuId)
        => _nhaCungCap.GetNhaCungCapsByNguyenLieuAsync(nguyenLieuId);
    public Task<List<NguyenLieuNhaCungCap>> GetNguyenLieuNhaCungCapsAsync(int nguyenLieuId)
        => _nhaCungCap.GetNguyenLieuNhaCungCapsAsync(nguyenLieuId);
    public Task<bool> UpsertNguyenLieuNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId, decimal giaNhap)
        => _nhaCungCap.UpsertNguyenLieuNhaCungCapAsync(nguyenLieuId, nhaCungCapId, giaNhap);

    // ═══════════════ DASHBOARD ═══════════════
    public Task<int> GetTotalNguyenLieuCountAsync()
        => _dashboard.GetTotalNguyenLieuCountAsync();
    public Task<int> GetTotalTonKhoCountAsync()
        => _dashboard.GetTotalTonKhoCountAsync();
    public Task<int> GetLowStockCountAsync(decimal threshold = 20)
        => _dashboard.GetLowStockCountAsync(threshold);
    public Task<int> GetNearExpiryCountAsync(int days = 7)
        => _dashboard.GetNearExpiryCountAsync(days);
    public Task<int> GetExpiredCountAsync()
        => _dashboard.GetExpiredCountAsync();
    public Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetLowStockItemsAsync(decimal threshold = 20)
        => _dashboard.GetLowStockItemsAsync(threshold);
    public Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNearExpiryItemsAsync(int days = 7)
        => _dashboard.GetNearExpiryItemsAsync(days);
    public Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetExpiredItemsAsync()
        => _dashboard.GetExpiredItemsAsync();
    public Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetNormalStockItemsAsync(decimal lowThreshold = 20)
        => _dashboard.GetNormalStockItemsAsync(lowThreshold);
    public Task<List<(string TenNguyenLieu, decimal SoLuongTon, string DonVi, DateTime? HanSuDung)>> GetOutOfStockItemsAsync()
        => _dashboard.GetOutOfStockItemsAsync();

    // ═══════════════ NHÂN VIÊN ═══════════════
    public Task<List<NhanVien>> GetNhanViensAsync()
        => _phieuNhap.GetNhanViensAsync();

    // ═══════════════ PIZZA ═══════════════
    public Task<List<Pizza>> GetPizzasAsync()
        => _pizza.GetPizzasAsync();
    public Task<Pizza?> GetPizzaByIdAsync(int pizzaId)
        => _pizza.GetPizzaByIdAsync(pizzaId);
    public Task<bool> SavePizzaAsync(Pizza pizza)
        => _pizza.SavePizzaAsync(pizza);
    public Task<bool> DeletePizzaAsync(int pizzaId)
        => _pizza.DeletePizzaAsync(pizzaId);
    public Task<bool> DeletePizzaByMaAsync(string maHangHoa)
        => _pizza.DeletePizzaByMaAsync(maHangHoa);
    public Task<bool> TogglePizzaTrangThaiAsync(string maHangHoa, bool newStatus)
        => _pizza.TogglePizzaTrangThaiAsync(maHangHoa, newStatus);
    public Task<List<CongThuc>> GetCongThucsAsync(int pizzaId)
        => _pizza.GetCongThucsAsync(pizzaId);
    public Task<bool> SaveCongThucAsync(CongThuc congThuc)
        => _pizza.SaveCongThucAsync(congThuc);
    public Task<bool> DeleteCongThucAsync(int congThucId)
        => _pizza.DeleteCongThucAsync(congThucId);
    public Task<decimal> CalculateGiaVonAsync(int pizzaId)
        => _pizza.CalculateGiaVonAsync(pizzaId);
    public Task<decimal> CalculateGiaVonByMaAsync(string maHangHoa, string sizeId)
        => _pizza.CalculateGiaVonByMaAsync(maHangHoa, sizeId);
    public Task<List<HangHoa>> GetHangHoasAsync()
        => _pizza.GetHangHoasAsync();
    public Task<HangHoa?> GetHangHoaByIdAsync(string maHangHoa)
        => _pizza.GetHangHoaByIdAsync(maHangHoa);
    public Task<Dictionary<string, List<string>>> GetOutOfStockIngredientsByHangHoaAsync(IEnumerable<string> maHangHoas)
        => _pizza.GetOutOfStockIngredientsByHangHoaAsync(maHangHoas);
    public Task<List<DoanhMuc_Size>> GetDoanhMucSizesAsync()
        => _pizza.GetDoanhMucSizesAsync();
    public Task<List<DoanhMuc_De>> GetDoanhMucDesAsync()
        => _pizza.GetDoanhMucDesAsync();
    public Task<List<LoaiHangHoa>> GetLoaiHangHoasAsync()
        => _pizza.GetLoaiHangHoasAsync();
    public Task<List<GiaTheo_Size>> GetGiaTheoSizeByHangHoaAsync(string maHangHoa)
        => _pizza.GetGiaTheoSizeByHangHoaAsync(maHangHoa);
    public Task<List<GiaTheo_De>> GetGiaTheoDeAsync(string sizeId)
        => _pizza.GetGiaTheoDeAsync(sizeId);
    public Task<List<CongThuc_Pizza>> GetCongThucPizzaAsync(string maHangHoa, string sizeId)
        => _pizza.GetCongThucPizzaAsync(maHangHoa, sizeId);
    public Task<bool> SaveCongThucPizzaAsync(CongThuc_Pizza congThuc)
        => _pizza.SaveCongThucPizzaAsync(congThuc);
    public Task<bool> DeleteCongThucPizzaAsync(string maHangHoa, string sizeId, int nguyenLieuId)
        => _pizza.DeleteCongThucPizzaAsync(maHangHoa, sizeId, nguyenLieuId);

    // ═══════════════ BÁN HÀNG ═══════════════
    public Task<List<DonHang>> GetDonHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        => _banHang.GetDonHangsAsync(fromDate, toDate);
    public Task<DonHang?> GetDonHangByIdAsync(int donHangId)
        => _banHang.GetDonHangByIdAsync(donHangId);
    public Task<List<CT_DonHang>> GetDonHangChiTietsAsync(int donHangId)
        => _banHang.GetDonHangChiTietsAsync(donHangId);
    public Task<int> SaveDonHangAsync(DonHang donHang, List<CT_DonHang> chiTiets)
        => _banHang.SaveDonHangAsync(donHang, chiTiets);
    public Task<bool> UpdateDonHangStatusAsync(int donHangId, byte trangThai)
        => _banHang.UpdateDonHangStatusAsync(donHangId, trangThai);
    public Task<bool> DeleteDonHangAsync(DonHang donHang)
        => _banHang.DeleteDonHangAsync(donHang);
    public Task<List<PhieuBanHang>> GetPhieuBanHangsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        => _banHang.GetPhieuBanHangsAsync(fromDate, toDate);
    public Task<PhieuBanHang?> GetPhieuBanHangByIdAsync(string maPhieuBan)
        => _banHang.GetPhieuBanHangByIdAsync(maPhieuBan);
    public Task<List<CT_PhieuBan>> GetChiTietPhieuBanAsync(string maPhieuBan)
        => _banHang.GetChiTietPhieuBanAsync(maPhieuBan);
    public Task<string> SavePhieuBanHangAsync(PhieuBanHang phieuBan, List<CT_PhieuBan> chiTiets)
        => _banHang.SavePhieuBanHangAsync(phieuBan, chiTiets);
    public Task<string> GenerateMaPhieuBanAsync()
        => _banHang.GenerateMaPhieuBanAsync();
    public Task<bool> UpdatePhieuBanHangAsync(PhieuBanHang pb)
        => _banHang.UpdatePhieuBanHangAsync(pb);
    public Task<bool> DeletePhieuBanHangAsync(string maPhieuBan)
        => _banHang.DeletePhieuBanHangAsync(maPhieuBan);
    public Task<bool> DeletePhieuBanHangWithRestoreAsync(string maPhieuBan)
        => _banHang.DeletePhieuBanHangWithRestoreAsync(maPhieuBan);

    // ═══════════════ THỐNG KÊ BÁN HÀNG ═══════════════
    public Task<decimal> GetDoanhThuAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetDoanhThuAsync(fromDate, toDate);
    public Task<int> GetTotalDonHangCountAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetTotalDonHangCountAsync(fromDate, toDate);
    public Task<decimal> GetTotalLoiNhuanAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetTotalLoiNhuanAsync(fromDate, toDate);
    public Task<decimal> GetChiPhiNguyenLieuAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetChiPhiNguyenLieuAsync(fromDate, toDate);
    public Task<List<(string TenPizza, string KichThuoc, int SoLuongBan, decimal DoanhThu)>> GetTopPizzasAsync(DateTime fromDate, DateTime toDate, int top = 5)
        => _dashboard.GetTopPizzasAsync(fromDate, toDate, top);
    public Task<List<DonHang>> GetRecentDonHangsAsync(int top = 10)
        => _dashboard.GetRecentDonHangsAsync(top);
    public Task<decimal> GetDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetDoanhThuBanHangAsync(fromDate, toDate);
    public Task<int> GetTotalPhieuBanCountAsync(DateTime fromDate, DateTime toDate)
        => _dashboard.GetTotalPhieuBanCountAsync(fromDate, toDate);

    // ═══════════════ PHIẾU NHẬP ═══════════════
    public Task<List<PhieuNhap>> GetPhieuNhapsAsync(string? searchText = null, int? nhanVienId = null, int? nhaCungCapId = null, DateTime? tuNgay = null, DateTime? denNgay = null, List<byte>? trangThaiFilter = null)
        => _phieuNhap.GetPhieuNhapsAsync(searchText, nhanVienId, nhaCungCapId, tuNgay, denNgay, trangThaiFilter);
    public Task<PhieuNhap?> GetPhieuNhapByIdAsync(int phieuNhapId)
        => _phieuNhap.GetPhieuNhapByIdAsync(phieuNhapId);
    public Task<List<CT_PhieuNhap>> GetChiTietPhieuNhapAsync(int phieuNhapId)
        => _phieuNhap.GetChiTietPhieuNhapAsync(phieuNhapId);
    public Task<bool> DeletePhieuNhapAsync(int phieuNhapId)
        => _phieuNhap.DeletePhieuNhapAsync(phieuNhapId);
    public Task<bool> ApprovePhieuNhapAsync(int phieuNhapId, int nguoiDuyetId)
        => _phieuNhap.ApprovePhieuNhapAsync(phieuNhapId, nguoiDuyetId);
    public Task<bool> CancelPhieuNhapAsync(int phieuNhapId, int nguoiHuyId, string? lyDoHuy = null)
        => _phieuNhap.CancelPhieuNhapAsync(phieuNhapId, nguoiHuyId, lyDoHuy);
    public Task<string> GenerateMaPhieuNhapAsync()
        => _phieuNhap.GenerateMaPhieuNhapAsync();
    public Task<int> SavePhieuNhapAsync(PhieuNhap phieuNhap, List<CT_PhieuNhap> chiTiets)
        => _phieuNhap.SavePhieuNhapAsync(phieuNhap, chiTiets);
    public Task<decimal> GetTotalTongTienPhieuNhapAsync(int? nhanVienId = null, int? nhaCungCapId = null, DateTime? tuNgay = null, DateTime? denNgay = null)
        => _phieuNhap.GetTotalTongTienPhieuNhapAsync(nhanVienId, nhaCungCapId, tuNgay, denNgay);
    public Task<List<NguyenLieu>> GetAllNguyenLieusWithPriceAsync()
        => _phieuNhap.GetAllNguyenLieusWithPriceAsync();
    public Task<List<NguyenLieu>> GetNguyenLieusByNhaCungCapAsync(int nhaCungCapId)
        => _phieuNhap.GetNguyenLieusByNhaCungCapAsync(nhaCungCapId);

    // ═══════════════ PHIẾU XUẤT (extra methods) ═══════════════
    public Task<List<PhieuXuat>> GetPhieuXuatsAsync(string? searchText = null, int? nhanVienYeuCauId = null, DateTime? tuNgay = null, DateTime? denNgay = null, List<byte>? trangThaiFilter = null)
        => _phieuXuat.GetPhieuXuatsAsync(searchText, nhanVienYeuCauId, tuNgay, denNgay, trangThaiFilter);
    public Task<PhieuXuat?> GetPhieuXuatByIdAsync(int phieuXuatId)
        => _phieuXuat.GetPhieuXuatByIdAsync(phieuXuatId);
    public Task<List<CT_PhieuXuat>> GetChiTietPhieuXuatAsync(int phieuXuatId)
        => _phieuXuat.GetChiTietPhieuXuatAsync(phieuXuatId);
    public Task<string> GenerateMaPhieuXuatAsync()
        => _phieuXuat.GenerateMaPhieuXuatAsync();
    public Task<int> SavePhieuXuatAsync(PhieuXuat phieuXuat, List<CT_PhieuXuat> chiTiets)
        => _phieuXuat.SavePhieuXuatAsync(phieuXuat, chiTiets);
    public Task<bool> ApprovePhieuXuatAsync(int phieuXuatId, int nhanVienDuyetId)
        => _phieuXuat.ApprovePhieuXuatAsync(phieuXuatId, nhanVienDuyetId);
    public Task<bool> CancelPhieuXuatAsync(int phieuXuatId)
        => _phieuXuat.CancelPhieuXuatAsync(phieuXuatId);
    public Task<bool> DeletePhieuXuatAsync(int phieuXuatId)
        => _phieuXuat.DeletePhieuXuatAsync(phieuXuatId);
    public Task<List<NguyenLieu>> GetNguyenLieusWithTonKhoAsync()
        => _phieuXuat.GetNguyenLieusWithTonKhoAsync();

    // ═══════════════ NHÀ CUNG CẤP (extra CRUD) ═══════════════
    public Task<List<NhaCungCap>> GetAllNhaCungCapsAsync(string? searchText = null, bool? trangThai = null)
        => _nhaCungCap.GetAllNhaCungCapsAsync(searchText, trangThai);
    public Task<NhaCungCap?> GetNhaCungCapByIdAsync(int nhaCungCapId)
        => _nhaCungCap.GetNhaCungCapByIdAsync(nhaCungCapId);
    public Task<int> SaveNhaCungCapAsync(NhaCungCap nhaCungCap)
        => _nhaCungCap.SaveNhaCungCapAsync(nhaCungCap);
    public Task<bool> UpdateNhaCungCapTrangThaiAsync(int nhaCungCapId, bool trangThai)
        => _nhaCungCap.UpdateNhaCungCapTrangThaiAsync(nhaCungCapId, trangThai);
    public Task<bool> DeleteNhaCungCapAsync(int nhaCungCapId)
        => _nhaCungCap.DeleteNhaCungCapAsync(nhaCungCapId);
    public Task<bool> AddNguyenLieuToNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId, decimal giaNhap)
        => _nhaCungCap.AddNguyenLieuToNhaCungCapAsync(nguyenLieuId, nhaCungCapId, giaNhap);
    public Task<bool> DeleteNguyenLieuFromNhaCungCapAsync(int nguyenLieuId, int nhaCungCapId)
        => _nhaCungCap.DeleteNguyenLieuFromNhaCungCapAsync(nguyenLieuId, nhaCungCapId);
    public Task<List<NguyenLieu>> GetNguyenLieusByNhaCungCapIdAsync(int nhaCungCapId)
        => _nhaCungCap.GetNguyenLieusByNhaCungCapIdAsync(nhaCungCapId);

    // ═══════════════ NHÂN VIÊN (extra CRUD) ═══════════════
    public Task<List<ChucVu>> GetAllChucVusAsync()
        => _nhanVien.GetAllChucVusAsync();
    public Task<int> SaveChucVuAsync(ChucVu chucVu)
        => _nhanVien.SaveChucVuAsync(chucVu);
    public Task<bool> DeleteChucVuAsync(int chucVuId)
        => _nhanVien.DeleteChucVuAsync(chucVuId);
    public Task<List<NhanVien>> GetAllNhanViensFullAsync(string? searchText = null, bool? trangThai = null, List<int>? chucVuIds = null)
        => _nhanVien.GetAllNhanViensFullAsync(searchText, trangThai, chucVuIds);
    public Task<int> SaveNhanVienAsync(NhanVien nhanVien)
        => _nhanVien.SaveNhanVienAsync(nhanVien);
    public Task<bool> UpdateNhanVienTrangThaiAsync(int nhanVienId, bool trangThai)
        => _nhanVien.UpdateNhanVienTrangThaiAsync(nhanVienId, trangThai);
    public Task<bool> DeleteNhanVienAsync(int nhanVienId)
        => _nhanVien.DeleteNhanVienAsync(nhanVienId);
    public Task<bool> UpdateNhanVienAvatarAsync(int nhanVienId, string hinhAnh)
        => _nhanVien.UpdateNhanVienAvatarAsync(nhanVienId, hinhAnh);
    public Task<TaiKhoan?> GetTaiKhoanByNhanVienIDAsync(int nhanVienId)
        => _nhanVien.GetTaiKhoanByNhanVienIDAsync(nhanVienId);
    public Task<bool> CreateTaiKhoanForNhanVienAsync(int nhanVienId, string username, string password)
        => _nhanVien.CreateTaiKhoanForNhanVienAsync(nhanVienId, username, password);

    // ═══════════════ PIZZA / QUY ĐỊNH (extra) ═══════════════
    public Task<List<QuyDinh_Bot>> GetQuyDinhBotsAsync()
        => _pizza.GetQuyDinhBotsAsync();
    public Task<List<QuyDinh_Vien>> GetQuyDinhViensAsync()
        => _pizza.GetQuyDinhViensAsync();
    public Task<bool> SaveQuyDinhBotAsync(QuyDinh_Bot item)
        => _pizza.SaveQuyDinhBotAsync(item);
    public Task<bool> SaveQuyDinhVienAsync(QuyDinh_Vien item)
        => _pizza.SaveQuyDinhVienAsync(item);
    public Task<bool> DeleteQuyDinhBotAsync(string sizeId, string loaiCotBanh)
        => _pizza.DeleteQuyDinhBotAsync(sizeId, loaiCotBanh);
    public Task<bool> DeleteQuyDinhVienAsync(string maDeBanh, string sizeId, int nguyenLieuId)
        => _pizza.DeleteQuyDinhVienAsync(maDeBanh, sizeId, nguyenLieuId);
}
