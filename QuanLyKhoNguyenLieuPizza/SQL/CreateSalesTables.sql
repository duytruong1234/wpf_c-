-- =============================================
-- Pizza Sales Feature - Database Tables
-- Run this script on your SQL Server database
-- =============================================

-- 1. Pizza (Danh mục Pizza)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Pizza')
BEGIN
    CREATE TABLE Pizza (
        PizzaID INT IDENTITY(1,1) PRIMARY KEY,
        MaPizza NVARCHAR(20) NULL,
        TenPizza NVARCHAR(200) NOT NULL,
        MoTa NVARCHAR(500) NULL,
        HinhAnh NVARCHAR(500) NULL,
        KichThuoc NVARCHAR(10) DEFAULT N'M',       -- S, M, L
        GiaBan DECIMAL(18,2) NOT NULL DEFAULT 0,
        TrangThai BIT NOT NULL DEFAULT 1            -- 1: Còn bán, 0: Ngừng bán
    );
END
GO

-- 2. CongThuc (Công thức - nguyên liệu cần cho mỗi Pizza)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CongThuc')
BEGIN
    CREATE TABLE CongThuc (
        CongThucID INT IDENTITY(1,1) PRIMARY KEY,
        PizzaID INT NOT NULL,
        NguyenLieuID INT NOT NULL,
        SoLuong DECIMAL(18,4) NOT NULL DEFAULT 0,
        DonViID INT NULL,
        CONSTRAINT FK_CongThuc_Pizza FOREIGN KEY (PizzaID) REFERENCES Pizza(PizzaID),
        CONSTRAINT FK_CongThuc_NguyenLieu FOREIGN KEY (NguyenLieuID) REFERENCES NguyenLieu(NguyenLieuID),
        CONSTRAINT FK_CongThuc_DonViTinh FOREIGN KEY (DonViID) REFERENCES DonViTinh(DonViID)
    );
END
GO

-- 3. DonHang (Đơn hàng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonHang')
BEGIN
    CREATE TABLE DonHang (
        DonHangID INT IDENTITY(1,1) PRIMARY KEY,
        MaDonHang NVARCHAR(50) NULL,
        NhanVienID INT NULL,
        NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
        TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
        GiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
        ThanhToan DECIMAL(18,2) NOT NULL DEFAULT 0,
        PhuongThucTT NVARCHAR(50) DEFAULT N'Tiền mặt',
        TrangThai TINYINT NOT NULL DEFAULT 1,       -- 1: Đang xử lý, 2: Hoàn thành, 3: Hủy
        GhiChu NVARCHAR(500) NULL,
        CONSTRAINT FK_DonHang_NhanVien FOREIGN KEY (NhanVienID) REFERENCES NhanVien(NhanVienID)
    );
END
GO

-- 4. CT_DonHang (Chi tiết đơn hàng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CT_DonHang')
BEGIN
    CREATE TABLE CT_DonHang (
        ChiTietID INT IDENTITY(1,1) PRIMARY KEY,
        DonHangID INT NOT NULL,
        PizzaID INT NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
        ThanhTien DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT FK_CTDonHang_DonHang FOREIGN KEY (DonHangID) REFERENCES DonHang(DonHangID),
        CONSTRAINT FK_CTDonHang_Pizza FOREIGN KEY (PizzaID) REFERENCES Pizza(PizzaID)
    );
END
GO

-- =============================================
-- 5. Thêm chức vụ "Nhân viên bán hàng" (ChucVuID = 5)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM ChucVu WHERE TenChucVu = N'Nhân viên bán hàng')
BEGIN
    SET IDENTITY_INSERT ChucVu ON;
    INSERT INTO ChucVu (ChucVuID, TenChucVu) VALUES (5, N'Nhân viên bán hàng');
    SET IDENTITY_INSERT ChucVu OFF;
END
GO

-- =============================================
-- DỮ LIỆU MẪU (Sample Data)
-- =============================================

-- ═══════════════ PIZZA MẪU ═══════════════
SET IDENTITY_INSERT Pizza ON;

INSERT INTO Pizza (PizzaID, MaPizza, TenPizza, MoTa, HinhAnh, KichThuoc, GiaBan, TrangThai) VALUES
(1, N'PZ001', N'Margherita', N'Pizza truyền thống Ý với sốt cà chua, phô mai Mozzarella và lá húng quế tươi', NULL, N'S', 89000, 1),
(2, N'PZ002', N'Margherita', N'Pizza truyền thống Ý với sốt cà chua, phô mai Mozzarella và lá húng quế tươi', NULL, N'M', 129000, 1),
(3, N'PZ003', N'Margherita', N'Pizza truyền thống Ý với sốt cà chua, phô mai Mozzarella và lá húng quế tươi', NULL, N'L', 169000, 1),
(4, N'PZ004', N'Pepperoni', N'Pizza với lát pepperoni giòn tan, phô mai Mozzarella kéo sợi và sốt cà chua đặc biệt', NULL, N'S', 99000, 1),
(5, N'PZ005', N'Pepperoni', N'Pizza với lát pepperoni giòn tan, phô mai Mozzarella kéo sợi và sốt cà chua đặc biệt', NULL, N'M', 149000, 1),
(6, N'PZ006', N'Pepperoni', N'Pizza với lát pepperoni giòn tan, phô mai Mozzarella kéo sợi và sốt cà chua đặc biệt', NULL, N'L', 189000, 1),
(7, N'PZ007', N'Hawaiian', N'Pizza với giăm bông, dứa tươi và phô mai Mozzarella trên nền sốt cà chua', NULL, N'S', 99000, 1),
(8, N'PZ008', N'Hawaiian', N'Pizza với giăm bông, dứa tươi và phô mai Mozzarella trên nền sốt cà chua', NULL, N'M', 139000, 1),
(9, N'PZ009', N'Hawaiian', N'Pizza với giăm bông, dứa tươi và phô mai Mozzarella trên nền sốt cà chua', NULL, N'L', 179000, 1),
(10, N'PZ010', N'Bò BBQ', N'Pizza với thịt bò nướng BBQ, hành tây, ớt chuông và sốt BBQ đặc biệt', NULL, N'S', 109000, 1),
(11, N'PZ011', N'Bò BBQ', N'Pizza với thịt bò nướng BBQ, hành tây, ớt chuông và sốt BBQ đặc biệt', NULL, N'M', 159000, 1),
(12, N'PZ012', N'Bò BBQ', N'Pizza với thịt bò nướng BBQ, hành tây, ớt chuông và sốt BBQ đặc biệt', NULL, N'L', 199000, 1),
(13, N'PZ013', N'Hải sản', N'Pizza với tôm, mực, sò điệp, phô mai Mozzarella và sốt kem tỏi', NULL, N'S', 119000, 1),
(14, N'PZ014', N'Hải sản', N'Pizza với tôm, mực, sò điệp, phô mai Mozzarella và sốt kem tỏi', NULL, N'M', 169000, 1),
(15, N'PZ015', N'Hải sản', N'Pizza với tôm, mực, sò điệp, phô mai Mozzarella và sốt kem tỏi', NULL, N'L', 219000, 1),
(16, N'PZ016', N'Gà Teriyaki', N'Pizza với gà teriyaki, nấm, hành tây và rau mùi', NULL, N'M', 139000, 1),
(17, N'PZ017', N'Rau củ', N'Pizza chay với ớt chuông, nấm, olive, hành tây và cà chua', NULL, N'M', 109000, 1),
(18, N'PZ018', N'Phô mai 4 loại', N'Pizza với 4 loại phô mai: Mozzarella, Cheddar, Parmesan và Gorgonzola', NULL, N'M', 149000, 1);

SET IDENTITY_INSERT Pizza OFF;
GO

-- ═══════════════ CÔNG THỨC MẪU (dựa trên NguyenLieu có sẵn) ═══════════════
-- Lưu ý: Các NguyenLieuID và DonViID phải tồn tại trong DB của bạn
-- Script này sẽ chỉ INSERT nếu NguyenLieu tồn tại

-- Công thức cho Margherita M (PizzaID = 2)
INSERT INTO CongThuc (PizzaID, NguyenLieuID, SoLuong, DonViID)
SELECT 2, nl.NguyenLieuID, 
    CASE 
        WHEN nl.TenNguyenLieu LIKE N'%bột%' THEN 0.25
        WHEN nl.TenNguyenLieu LIKE N'%phô mai%' OR nl.TenNguyenLieu LIKE N'%pho mai%' OR nl.TenNguyenLieu LIKE N'%cheese%' THEN 0.15
        WHEN nl.TenNguyenLieu LIKE N'%cà chua%' OR nl.TenNguyenLieu LIKE N'%sốt%' THEN 0.10
        ELSE 0.05
    END,
    (SELECT TOP 1 DonViID FROM DonViTinh WHERE TenDonVi LIKE N'%kg%' OR TenDonVi LIKE N'%Kg%')
FROM NguyenLieu nl
WHERE nl.TenNguyenLieu LIKE N'%bột%' 
   OR nl.TenNguyenLieu LIKE N'%phô mai%' 
   OR nl.TenNguyenLieu LIKE N'%pho mai%'
   OR nl.TenNguyenLieu LIKE N'%cheese%'
   OR nl.TenNguyenLieu LIKE N'%cà chua%';
GO

-- Công thức cho Pepperoni M (PizzaID = 5)
INSERT INTO CongThuc (PizzaID, NguyenLieuID, SoLuong, DonViID)
SELECT 5, nl.NguyenLieuID,
    CASE 
        WHEN nl.TenNguyenLieu LIKE N'%bột%' THEN 0.25
        WHEN nl.TenNguyenLieu LIKE N'%phô mai%' OR nl.TenNguyenLieu LIKE N'%pho mai%' OR nl.TenNguyenLieu LIKE N'%cheese%' THEN 0.15
        WHEN nl.TenNguyenLieu LIKE N'%pepperoni%' OR nl.TenNguyenLieu LIKE N'%xúc xích%' THEN 0.10
        WHEN nl.TenNguyenLieu LIKE N'%cà chua%' OR nl.TenNguyenLieu LIKE N'%sốt%' THEN 0.10
        ELSE 0.05
    END,
    (SELECT TOP 1 DonViID FROM DonViTinh WHERE TenDonVi LIKE N'%kg%' OR TenDonVi LIKE N'%Kg%')
FROM NguyenLieu nl
WHERE nl.TenNguyenLieu LIKE N'%bột%'
   OR nl.TenNguyenLieu LIKE N'%phô mai%'
   OR nl.TenNguyenLieu LIKE N'%pho mai%'
   OR nl.TenNguyenLieu LIKE N'%cheese%'
   OR nl.TenNguyenLieu LIKE N'%cà chua%'
   OR nl.TenNguyenLieu LIKE N'%pepperoni%'
   OR nl.TenNguyenLieu LIKE N'%xúc xích%';
GO

-- ═══════════════ ĐƠN HÀNG MẪU ═══════════════
-- Sử dụng NhanVienID đầu tiên tìm được
DECLARE @NhanVienID INT;
SELECT TOP 1 @NhanVienID = NhanVienID FROM NhanVien WHERE TrangThai = 1;

IF @NhanVienID IS NOT NULL
BEGIN
    -- Đơn hàng 1: Hoàn thành - hôm nay
    INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
    VALUES (N'DH20250101001', @NhanVienID, GETDATE(), 278000, 0, 278000, N'Tiền mặt', 2, N'Khách quen');

    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien)
    VALUES (SCOPE_IDENTITY(), 2, 1, 129000, 129000);

    DECLARE @DH1 INT = (SELECT MAX(DonHangID) FROM DonHang);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien)
    VALUES (@DH1, 5, 1, 149000, 149000);

    -- Đơn hàng 2: Hoàn thành - hôm nay
    INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
    VALUES (N'DH20250101002', @NhanVienID, GETDATE(), 487000, 0, 487000, N'Chuyển khoản', 2, NULL);

    DECLARE @DH2 INT = SCOPE_IDENTITY();
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH2, 15, 1, 219000, 219000);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH2, 12, 1, 199000, 199000);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH2, 1, 1, 89000, 89000);
    UPDATE DonHang SET TongTien = 507000, ThanhToan = 507000 WHERE DonHangID = @DH2;

    -- Đơn hàng 3: Hoàn thành - hôm qua
    INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
    VALUES (N'DH20250100001', @NhanVienID, DATEADD(DAY, -1, GETDATE()), 338000, 20000, 318000, N'Tiền mặt', 2, N'Giảm giá VIP');

    DECLARE @DH3 INT = SCOPE_IDENTITY();
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH3, 8, 2, 139000, 278000);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH3, 17, 1, 109000, 109000);
    UPDATE DonHang SET TongTien = 387000, ThanhToan = 367000 WHERE DonHangID = @DH3;

    -- Đơn hàng 4: Đã hủy - hôm qua
    INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
    VALUES (N'DH20250100002', @NhanVienID, DATEADD(DAY, -1, GETDATE()), 149000, 0, 0, N'Tiền mặt', 3, N'Khách hủy đơn');

    DECLARE @DH4 INT = SCOPE_IDENTITY();
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH4, 5, 1, 149000, 149000);

    -- Đơn hàng 5: Hoàn thành - 3 ngày trước
    INSERT INTO DonHang (MaDonHang, NhanVienID, NgayTao, TongTien, GiamGia, ThanhToan, PhuongThucTT, TrangThai, GhiChu)
    VALUES (N'DH20250098001', @NhanVienID, DATEADD(DAY, -3, GETDATE()), 557000, 0, 557000, N'Chuyển khoản', 2, NULL);

    DECLARE @DH5 INT = SCOPE_IDENTITY();
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH5, 14, 2, 169000, 338000);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH5, 16, 1, 139000, 139000);
    INSERT INTO CT_DonHang (DonHangID, PizzaID, SoLuong, DonGia, ThanhTien) VALUES (@DH5, 1, 1, 89000, 89000);
    UPDATE DonHang SET TongTien = 566000, ThanhToan = 566000 WHERE DonHangID = @DH5;
END
GO

-- ═══════════════ NHÂN VIÊN BÁN HÀNG MẪU ═══════════════
-- Thêm 1 nhân viên bán hàng + tài khoản để test
IF NOT EXISTS (SELECT 1 FROM NhanVien WHERE HoTen = N'Nguyễn Văn Bán')
BEGIN
    INSERT INTO NhanVien (HoTen, HinhAnh, NgaySinh, DiaChi, SDT, Email, ChucVuID, TrangThai)
    VALUES (N'Nguyễn Văn Bán', NULL, '1998-05-15', N'456 Lê Lợi, Q.1, TP.HCM', N'0901234567', N'nvban@pizzainn.vn', 5, 1);

    DECLARE @NVBH INT = SCOPE_IDENTITY();
    INSERT INTO TaiKhoan (NhanVienID, Username, Password, TrangThai)
    VALUES (@NVBH, N'nvbanhang', N'123', 1);
END
GO

PRINT N'✅ Hoàn tất: 4 bảng + chức vụ Nhân viên bán hàng + 18 pizza + đơn hàng mẫu + tài khoản test (nvbanhang/123)';
