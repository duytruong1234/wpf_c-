/* =========================================================================
   SQL CAP NHAT: TIEU HAO VA DU BAO NHU CAU NGUYEN LIEU (HOLT-WINTERS)
   1. Bo sung 4 bang: PhuongPhapDuBao, DuLieuTieuHao, DanhGiaDuBao, DuBao
   2. Nap PhuongPhapDuBao (Holt-Winters cong tinh & nhan tinh, L = 12)
   3. Nap du lieu mau PhieuBanHang, CT_PhieuBan, PhieuXuat, CT_PhieuXuat
      tu 01/01/2024 den 30/10/2026 (5 don/ngay, cao diem thang 1,2,4,6,9,12)
   4. Tong hop luong tieu hao theo thang vao bang DuLieuTieuHao
========================================================================= */
USE [QLKNL_PIZZA];
GO

-- 1. BỔ SUNG CƠ SỞ DỮ LIỆU
IF OBJECT_ID('dbo.PhuongPhapDuBao', 'U') IS NULL
BEGIN
    CREATE TABLE PhuongPhapDuBao
    (
        PhuongPhapID INT IDENTITY(1,1) PRIMARY KEY,
        TenPhuongPhap NVARCHAR(100) NOT NULL,
        LoaiMuaVu NVARCHAR(50),
        ChuKyMuaVu INT,
        TrangThai BIT NOT NULL DEFAULT 1
    );
END
GO

IF OBJECT_ID('dbo.DuLieuTieuHao', 'U') IS NULL
BEGIN
    CREATE TABLE DuLieuTieuHao
    (
        TieuHaoID INT IDENTITY(1,1) PRIMARY KEY,
        NguyenLieuID INT NOT NULL,
        DonViID INT NOT NULL,
        KyTieuHao DATE NOT NULL,
        SoLuongTieuHao DECIMAL(18,3) NOT NULL,

        CONSTRAINT FK_DuLieuTieuHao_NguyenLieu
            FOREIGN KEY (NguyenLieuID)
            REFERENCES NguyenLieu(NguyenLieuID),

        CONSTRAINT FK_DuLieuTieuHao_DonViTinh
            FOREIGN KEY (DonViID)
            REFERENCES DonViTinh(DonViID)
    );
END
GO

IF OBJECT_ID('dbo.DanhGiaDuBao', 'U') IS NULL
BEGIN
    CREATE TABLE DanhGiaDuBao
    (
        DanhGiaID INT IDENTITY(1,1) PRIMARY KEY,
        NguyenLieuID INT NOT NULL,
        PhuongPhapID INT NOT NULL,
        TuKyTrain DATE NOT NULL,
        DenKyTrain DATE NOT NULL,
        TuKyTest DATE NOT NULL,
        DenKyTest DATE NOT NULL,
        MAE DECIMAL(18,4),
        RMSE DECIMAL(18,4),
        MAPE DECIMAL(18,4),
        NgayDanhGia DATETIME DEFAULT GETDATE(),

        CONSTRAINT FK_DanhGiaDuBao_NguyenLieu
            FOREIGN KEY (NguyenLieuID)
            REFERENCES NguyenLieu(NguyenLieuID),

        CONSTRAINT FK_DanhGiaDuBao_PhuongPhap
            FOREIGN KEY (PhuongPhapID)
            REFERENCES PhuongPhapDuBao(PhuongPhapID)
    );
END
GO

IF OBJECT_ID('dbo.DuBao', 'U') IS NULL
BEGIN
    CREATE TABLE DuBao
    (
        DuBaoID INT IDENTITY(1,1) PRIMARY KEY,
        NguyenLieuID INT NOT NULL,
        PhuongPhapID INT NOT NULL,
        KyDuBao DATE NOT NULL,
        SoLuongDuBao DECIMAL(18,3) NOT NULL,
        LaMoHinhDuocChon BIT NOT NULL DEFAULT 0,
        NgayDuBao DATETIME DEFAULT GETDATE(),
        TrangThai BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_DuBao_NguyenLieu
            FOREIGN KEY (NguyenLieuID)
            REFERENCES NguyenLieu(NguyenLieuID),

        CONSTRAINT FK_DuBao_PhuongPhap
            FOREIGN KEY (PhuongPhapID)
            REFERENCES PhuongPhapDuBao(PhuongPhapID)
    );
END
GO

-- 2. NẠP DỮ LIỆU BẢNG PhuongPhapDuBao
IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao)
BEGIN
    INSERT INTO PhuongPhapDuBao
    (
        TenPhuongPhap,
        LoaiMuaVu,
        ChuKyMuaVu,
        TrangThai
    )
    VALUES
    (
        N'Holt-Winters cộng tính',
        N'Additive',
        12,
        1
    ),
    (
        N'Holt-Winters nhân tính',
        N'Multiplicative',
        12,
        1
    );
END
GO

-- Đảm bảo đầy đủ định mức BOM cho VEG-R và Sốt cà chua nền (NguyenLieuID = 19)
IF NOT EXISTS (SELECT 1 FROM CongThuc_Pizza WHERE MaHangHoa = 'VEG-R' AND SizeID = 'R')
BEGIN
    INSERT INTO CongThuc_Pizza (MaHangHoa, SizeID, NguyenLieuID, SoLuong, DonViID)
    VALUES
        ('VEG-R', 'R', 11, 30, 2),
        ('VEG-R', 'R', 12, 30, 2),
        ('VEG-R', 'R', 13, 30, 2),
        ('VEG-R', 'R', 15, 20, 2),
        ('VEG-R', 'R', 24, 100, 2);
END
GO

IF NOT EXISTS (SELECT 1 FROM CongThuc_Pizza WHERE NguyenLieuID = 19)
BEGIN
    INSERT INTO CongThuc_Pizza (MaHangHoa, SizeID, NguyenLieuID, SoLuong, DonViID)
    VALUES
        ('GOLD-L', 'L', 19, 60, 7), ('GOLD-R', 'R', 19, 30, 7),
        ('HAW-L', 'L', 19, 60, 7), ('HAW-R', 'R', 19, 30, 7),
        ('POTATO-L', 'L', 19, 60, 7), ('POTATO-R', 'R', 19, 30, 7),
        ('SEAFOOD-L', 'L', 19, 60, 7), ('SEAFOOD-R', 'R', 19, 30, 7),
        ('SUPPER-L', 'L', 19, 60, 7), ('SUPPER-R', 'R', 19, 30, 7),
        ('TEXAS-L', 'L', 19, 60, 7), ('TEXAS-R', 'R', 19, 30, 7),
        ('VEG-L', 'L', 19, 60, 7), ('VEG-R', 'R', 19, 30, 7);
END
GO

-- 3. NẠP DỮ LIỆU MẪU BÁN HÀNG & XUẤT KHO (01/01/2024 - 30/10/2026) VÀ TỔNG HỢP DuLieuTieuHao
IF NOT EXISTS (SELECT 1 FROM DuLieuTieuHao)
BEGIN
    SET NOCOUNT ON;

    DELETE FROM CT_PhieuBan;
    DELETE FROM PhieuBanHang;

    DECLARE @Menu TABLE (Idx INT IDENTITY(1,1), MaHangHoa VARCHAR(20), SizeID VARCHAR(10), GiaBan DECIMAL(18,2));
    INSERT INTO @Menu (MaHangHoa, SizeID, GiaBan)
    SELECT hh.MaHangHoa, gts.SizeID, gts.GiaBan
    FROM HangHoa hh
    INNER JOIN GiaTheo_Size gts ON hh.MaHangHoa = gts.MaHangHoa
    WHERE ISNULL(hh.TinhTrang, 1) = 1
    ORDER BY hh.MaHangHoa, gts.SizeID;

    DECLARE @MenuCount INT = (SELECT COUNT(*) FROM @Menu);
    DECLARE @CurDate DATE = '2024-01-01';
    DECLARE @EndDate DATE = '2026-10-30';
    DECLARE @PbIdx INT = 1;
    DECLARE @Seed INT = 20261030;

    WHILE @CurDate <= @EndDate
    BEGIN
        DECLARE @Month INT = MONTH(@CurDate);
        DECLARE @Year INT = YEAR(@CurDate);
        DECLARE @IsPeak BIT = CASE WHEN @Month IN (1, 2, 4, 6, 9, 12) THEN 1 ELSE 0 END;
        DECLARE @YearBoost INT = CASE WHEN @Year = 2024 THEN 0 WHEN @Year = 2025 THEN 1 ELSE 2 END;

        DECLARE @OrderNo INT = 1;
        WHILE @OrderNo <= 5
        BEGIN
            DECLARE @MaPB VARCHAR(20) = 'PB' + RIGHT('000000' + CAST(@PbIdx AS VARCHAR(10)), 6);
            SET @PbIdx = @PbIdx + 1;
            SET @Seed = (@Seed * 1103515245 + 12345) & 2147483647;

            DECLARE @NgayBan DATETIME = DATEADD(MINUTE, (@OrderNo * 115) + (@Seed % 45), CAST(@CurDate AS DATETIME));
            DECLARE @Pttt NVARCHAR(50) = CASE (@Seed % 3) WHEN 0 THEN N'Tiền mặt' WHEN 1 THEN N'Chuyển khoản' ELSE N'Thẻ ngân hàng' END;

            INSERT INTO PhieuBanHang (MaPhieuBan, NhanVienBanID, NgayBan, TongTien, GhiChu, PhuongThucTT, GiamGia, ThanhToan)
            VALUES (@MaPB, 4, @NgayBan, 0, N'Bán hàng tại quầy', @Pttt, 0, 0);

            DECLARE @ItemCount INT = CASE WHEN @IsPeak = 1 THEN 3 + (@Seed % 2) ELSE 2 + (@Seed % 2) END;
            DECLARE @ItemIdx INT = 1;
            DECLARE @TongTien DECIMAL(18,2) = 0;

            WHILE @ItemIdx <= @ItemCount
            BEGIN
                SET @Seed = (@Seed * 1103515245 + 12345) & 2147483647;
                DECLARE @PickIdx INT = ((@Seed + @ItemIdx * 3) % @MenuCount) + 1;
                DECLARE @MaHH VARCHAR(20), @Sz VARCHAR(10), @Gia DECIMAL(18,2);
                SELECT @MaHH = MaHangHoa, @Sz = SizeID, @Gia = GiaBan FROM @Menu WHERE Idx = @PickIdx;

                DECLARE @Qty INT = CASE WHEN @IsPeak = 1 THEN 2 + (@Seed % 3) + (@YearBoost / 2) ELSE 1 + (@Seed % 2) + (@YearBoost / 2) END;
                DECLARE @ThanhTien DECIMAL(18,2) = @Gia * @Qty;
                SET @TongTien = @TongTien + @ThanhTien;

                INSERT INTO CT_PhieuBan (MaPhieuBan, MaHangHoa, SizeID, MaDeBanh, SoLuong, ThanhTien)
                VALUES (@MaPB, @MaHH, @Sz, 'DAY', @Qty, @ThanhTien);

                SET @ItemIdx = @ItemIdx + 1;
            END

            UPDATE PhieuBanHang SET TongTien = @TongTien, ThanhToan = @TongTien WHERE MaPhieuBan = @MaPB;
            SET @OrderNo = @OrderNo + 1;
        END

        SET @CurDate = DATEADD(DAY, 1, @CurDate);
    END

    -- Tổng hợp lượng tiêu hao theo từng nguyên liệu và từng tháng vào bảng DuLieuTieuHao
    ;WITH AllBOM AS (
        SELECT cp.MaHangHoa, cp.SizeID, 'DAY' AS MaDeBanh, cp.NguyenLieuID, CAST(cp.SoLuong AS DECIMAL(18,4)) AS SoLuongBOM, cp.DonViID AS DonViBOM
        FROM CongThuc_Pizza cp
        UNION ALL
        SELECT hh.MaHangHoa, gts.SizeID, 'DAY' AS MaDeBanh, 26 AS NguyenLieuID,
               CAST(CASE WHEN RTRIM(gts.SizeID) = 'L' THEN 300 ELSE 150 END AS DECIMAL(18,4)) AS SoLuongBOM,
               2 AS DonViBOM
        FROM HangHoa hh
        INNER JOIN GiaTheo_Size gts ON hh.MaHangHoa = gts.MaHangHoa
    )
    INSERT INTO DuLieuTieuHao (NguyenLieuID, DonViID, KyTieuHao, SoLuongTieuHao)
    SELECT
        b.NguyenLieuID,
        nl.DonViID,
        DATEFROMPARTS(YEAR(pb.NgayBan), MONTH(pb.NgayBan), 1) AS KyTieuHao,
        ROUND(SUM(
            ct.SoLuong * b.SoLuongBOM *
            CASE
                WHEN b.DonViBOM = nl.DonViID THEN 1.0
                ELSE ISNULL(qdFrom.HeSo, 1.0) / NULLIF(ISNULL(qdTo.HeSo, 1.0), 0)
            END
        ), 3) AS SoLuongTieuHao
    FROM CT_PhieuBan ct
    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
    INNER JOIN AllBOM b ON ct.MaHangHoa = b.MaHangHoa AND ct.SizeID = b.SizeID
    INNER JOIN NguyenLieu nl ON b.NguyenLieuID = nl.NguyenLieuID
    LEFT JOIN QuyDoiDonVi qdFrom ON qdFrom.NguyenLieuID = b.NguyenLieuID AND qdFrom.DonViID = b.DonViBOM
    LEFT JOIN QuyDoiDonVi qdTo ON qdTo.NguyenLieuID = b.NguyenLieuID AND qdTo.DonViID = nl.DonViID
    GROUP BY b.NguyenLieuID, nl.DonViID, DATEFROMPARTS(YEAR(pb.NgayBan), MONTH(pb.NgayBan), 1)
    ORDER BY KyTieuHao, b.NguyenLieuID;

    -- Tổng hợp Phiếu Xuất và CT_Phiếu Xuất theo từng kỳ tháng
    DECLARE @KyCursor DATE = '2024-01-01';
    DECLARE @PxNo INT = 100;
    WHILE @KyCursor <= '2026-10-01'
    BEGIN
        DECLARE @MaPX VARCHAR(20) = 'PX' + RIGHT('000000' + CAST(@PxNo AS VARCHAR(10)), 6);
        SET @PxNo = @PxNo + 1;
        DECLARE @NgayXuat DATETIME = DATEADD(HOUR, 8, DATEADD(DAY, 14, CAST(@KyCursor AS DATETIME)));

        INSERT INTO PhieuXuat (MaPhieuXuat, NhanVienYeuID, NhanVienDuyetID, NgayYeuCau, NgayDuyet, TrangThai, GhiChu, LoaiPhieu)
        VALUES (@MaPX, 2, 1, @NgayXuat, DATEADD(MINUTE, 15, @NgayXuat), 2, N'Xuất bếp tự động kỳ ' + FORMAT(@KyCursor, 'MM/yyyy'), N'Xuất bếp chế biến');

        DECLARE @NewPxId INT = SCOPE_IDENTITY();

        INSERT INTO CT_PhieuXuat (PhieuXuatID, NguyenLieuID, SoLuong, DonViID, HeSo)
        SELECT @NewPxId, th.NguyenLieuID, th.SoLuongTieuHao, th.DonViID, 1
        FROM DuLieuTieuHao th
        WHERE th.KyTieuHao = @KyCursor;

        SET @KyCursor = DATEADD(MONTH, 1, @KyCursor);
    END

    SET NOCOUNT OFF;
END
GO
