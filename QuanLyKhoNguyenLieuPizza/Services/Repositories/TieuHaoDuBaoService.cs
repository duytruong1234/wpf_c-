using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class TieuHaoDuBaoService : DatabaseContext
{
    public TieuHaoDuBaoService(string connectionString) : base(connectionString) { }

    public async Task<List<NguyenLieuFilterOption>> GetNguyenLieuFilterOptionsAsync()
    {
        var list = new List<NguyenLieuFilterOption>
        {
            new NguyenLieuFilterOption { NguyenLieuID = null, TenHienThi = "Tất cả nguyên liệu" }
        };

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, ISNULL(dvt.TenDonVi, 'kg')
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                WHERE ISNULL(nl.TrangThai, 1) = 1
                ORDER BY nl.NguyenLieuID";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var ma = $"NL{id:D3}";
                var ten = reader.GetString(2);
                var dvt = reader.IsDBNull(3) ? "kg" : reader.GetString(3);
                list.Add(new NguyenLieuFilterOption
                {
                    NguyenLieuID = id,
                    TenHienThi = $"{ma} - {ten} ({dvt})"
                });
            }
        }
        catch { }

        return list;
    }

    public async Task<List<KyThoiGianOption>> GetAvailablePeriodsAsync()
    {
        var list = new List<KyThoiGianOption>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"
                SELECT DISTINCT KyTieuHao
                FROM DuLieuTieuHao
                ORDER BY KyTieuHao DESC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dt = reader.GetDateTime(0);
                var ky = new DateTime(dt.Year, dt.Month, 1);
                list.Add(new KyThoiGianOption
                {
                    KyDate = ky,
                    DisplayEnglish = ky.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    DisplayMMYYYY = ky.ToString("MM/yyyy")
                });
            }
        }
        catch { }

        return list;
    }

    public async Task<List<TieuHaoNguyenLieuItem>> GetBaoCaoTieuHaoAsync(DateTime kyTieuHao, int? nguyenLieuId = null)
    {
        var result = new List<TieuHaoNguyenLieuItem>();
        var kyCur = new DateTime(kyTieuHao.Year, kyTieuHao.Month, 1);
        var kyPrevMonth = kyCur.AddMonths(-1);
        var kyPrevYear = kyCur.AddYears(-1);
        var nextMonth = kyCur.AddMonths(1);

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // Đảm bảo kỳ dự báo nextMonth đã có trong bảng DuBao (nếu vừa phát sinh giao dịch kỳ mới)
            int dbCountForNext = 0;
            using (var chkCmd = new SqlCommand("SELECT COUNT(*) FROM DuBao WHERE KyDuBao = @KyNext AND LaMoHinhDuocChon = 1 AND TrangThai = 1", conn))
            {
                chkCmd.Parameters.AddWithValue("@KyNext", nextMonth);
                dbCountForNext = Convert.ToInt32(await chkCmd.ExecuteScalarAsync() ?? 0);
            }
            if (dbCountForNext == 0)
            {
                await EnsureFiveModelsForecastForPeriodAsync(conn, kyCur);
            }

            var dishCountMap = new Dictionary<int, int>();
            const string dishCountSql = @"
                WITH SoldPizzas AS (
                    SELECT DISTINCT ct.MaHangHoa, RTRIM(ct.SizeID) AS SizeID, RTRIM(ct.MaDeBanh) AS MaDeBanh
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                ),
                IngredientDishes AS (
                    SELECT cp.NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN CongThuc_Pizza cp ON sp.MaHangHoa = cp.MaHangHoa AND sp.SizeID = RTRIM(cp.SizeID)
                    UNION
                    SELECT 26 AS NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN DoanhMuc_De dd ON sp.MaDeBanh = RTRIM(dd.MaDeBanh)
                    INNER JOIN QuyDinh_Bot qb ON sp.SizeID = RTRIM(qb.SizeID) AND RTRIM(dd.LoaiCotBanh) = RTRIM(qb.LoaiCotBanh)
                    UNION
                    SELECT qv.NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN QuyDinh_Vien qv ON sp.MaDeBanh = RTRIM(qv.MaDeBanh) AND sp.SizeID = RTRIM(qv.SizeID)
                )
                SELECT NguyenLieuID, COUNT(DISTINCT MaHangHoa + '|' + SizeID) AS DishCount
                FROM IngredientDishes
                GROUP BY NguyenLieuID";

            using (var dishCmd = new SqlCommand(dishCountSql, conn))
            {
                dishCmd.Parameters.AddWithValue("@FromDate", kyCur);
                dishCmd.Parameters.AddWithValue("@ToDate", nextMonth);
                using var r = await dishCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    dishCountMap[r.GetInt32(0)] = r.GetInt32(1);
                }
            }

            var sql = @"
                SELECT nl.NguyenLieuID,
                       ISNULL(nl.MaNguyenLieu, 'NL' + RIGHT('000' + CAST(nl.NguyenLieuID AS VARCHAR(10)), 3)) AS MaNL,
                       nl.TenNguyenLieu,
                       ISNULL(dvt.TenDonVi, 'kg') AS TenDonVi,
                       ISNULL(thCur.SoLuongTieuHao, 0) AS CurVal,
                       ISNULL(thPrevM.SoLuongTieuHao, 0) AS PrevMVal,
                       ISNULL(thPrevY.SoLuongTieuHao, 0) AS PrevYVal,
                       ISNULL(dbChosen.PhuongPhapID, 1) AS ChosenPpId,
                       ISNULL(dbChosen.TenPhuongPhap, N'Holt-Winters cộng tính') AS ChosenPpName,
                       ISNULL(dbChosen.SoLuongDuBao, 0) AS NextForecastVal
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                LEFT JOIN DuLieuTieuHao thCur ON nl.NguyenLieuID = thCur.NguyenLieuID AND thCur.KyTieuHao = @KyCur
                LEFT JOIN DuLieuTieuHao thPrevM ON nl.NguyenLieuID = thPrevM.NguyenLieuID AND thPrevM.KyTieuHao = @KyPrevM
                LEFT JOIN DuLieuTieuHao thPrevY ON nl.NguyenLieuID = thPrevY.NguyenLieuID AND thPrevY.KyTieuHao = @KyPrevY
                OUTER APPLY (
                    SELECT TOP 1 db.PhuongPhapID, pp.TenPhuongPhap, db.SoLuongDuBao
                    FROM DuBao db
                    LEFT JOIN PhuongPhapDuBao pp ON db.PhuongPhapID = pp.PhuongPhapID
                    WHERE db.NguyenLieuID = nl.NguyenLieuID
                      AND db.KyDuBao = @KyNext
                      AND db.TrangThai = 1
                    ORDER BY db.LaMoHinhDuocChon DESC, db.PhuongPhapID ASC
                ) dbChosen
                WHERE ISNULL(nl.TrangThai, 1) = 1";

            if (nguyenLieuId.HasValue)
            {
                sql += " AND nl.NguyenLieuID = @NguyenLieuID";
            }
            sql += " ORDER BY nl.NguyenLieuID ASC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KyCur", kyCur);
            cmd.Parameters.AddWithValue("@KyPrevM", kyPrevMonth);
            cmd.Parameters.AddWithValue("@KyPrevY", kyPrevYear);
            cmd.Parameters.AddWithValue("@KyNext", nextMonth);
            if (nguyenLieuId.HasValue)
            {
                cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId.Value);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            int stt = 1;
            while (await reader.ReadAsync())
            {
                var nlId = reader.GetInt32(0);
                var tenNl = reader.GetString(2);
                var donVi = reader.GetString(3);
                var curVal = reader.GetDecimal(4);
                var prevMVal = reader.GetDecimal(5);
                var prevYVal = reader.GetDecimal(6);
                var ppId = reader.GetInt32(7);
                var ppName = reader.GetString(8);
                var nextForecast = reader.GetDecimal(9);

                dishCountMap.TryGetValue(nlId, out var soMon);

                result.Add(new TieuHaoNguyenLieuItem
                {
                    STT = stt++,
                    NguyenLieuID = nlId,
                    MaNguyenLieu = $"NL{nlId:D3}",
                    TenNguyenLieu = tenNl,
                    DonViKho = donVi,
                    SoMonAnDaBan = soMon,
                    TongTieuHao = curVal,
                    TieuHaoThangTruoc = prevMVal,
                    TieuHaoCungKyNamTruoc = prevYVal,
                    DuBaoPhuongPhapID = ppId,
                    DuBaoTenPhuongPhapBadge = ForecastMethodHelper.ToBadgeName(ppId, ppName),
                    DuBaoKyToi = Math.Round(nextForecast, 2)
                });
            }
        }
        catch { }

        return result;
    }

    public async Task<List<MonAnTieuHaoItem>> GetMonAnDaBanTheoNguyenLieuAsync(
        int nguyenLieuId,
        string donViKho,
        DateTime kyTieuHao)
    {
        var result = new List<MonAnTieuHaoItem>();
        var fromDate = new DateTime(kyTieuHao.Year, kyTieuHao.Month, 1);
        var toDate = fromDate.AddMonths(1);

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var quyDoiMap = new Dictionary<int, decimal>();
            int stockDonViId = 1;
            using (var nlCmd = new SqlCommand("SELECT ISNULL(DonViID, 1) FROM NguyenLieu WHERE NguyenLieuID = @Id", conn))
            {
                nlCmd.Parameters.AddWithValue("@Id", nguyenLieuId);
                var obj = await nlCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    stockDonViId = Convert.ToInt32(obj);
            }

            using (var qdCmd = new SqlCommand("SELECT DonViID, HeSo FROM QuyDoiDonVi WHERE NguyenLieuID = @Id", conn))
            {
                qdCmd.Parameters.AddWithValue("@Id", nguyenLieuId);
                using var r = await qdCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    quyDoiMap[r.GetInt32(0)] = r.GetDecimal(1);
                }
            }

            decimal ConvertToStock(decimal amount, int fromDonViId)
            {
                if (fromDonViId == stockDonViId) return amount;
                decimal heSoFrom = quyDoiMap.TryGetValue(fromDonViId, out var hf) ? hf : 1m;
                decimal heSoStock = quyDoiMap.TryGetValue(stockDonViId, out var hs) ? hs : 1m;
                if (heSoStock <= 0) heSoStock = 1m;
                return amount * (heSoFrom / heSoStock);
            }

            const string sql = @"
                WITH SoldLines AS (
                    SELECT ct.MaHangHoa,
                           RTRIM(ct.SizeID) AS SizeID,
                           RTRIM(ct.MaDeBanh) AS MaDeBanh,
                           SUM(ct.SoLuong) AS SoLuongBan
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                    GROUP BY ct.MaHangHoa, RTRIM(ct.SizeID), RTRIM(ct.MaDeBanh)
                ),
                BomMatched AS (
                    SELECT sl.MaHangHoa,
                           sl.SizeID,
                           sl.SoLuongBan,
                           CAST(cp.SoLuong AS DECIMAL(18,4)) AS BomPerPizza,
                           ISNULL(cp.DonViID, 2) AS BomDonViID,
                           sl.SoLuongBan * CAST(cp.SoLuong AS DECIMAL(18,4)) AS TotalBomAmount
                    FROM SoldLines sl
                    INNER JOIN CongThuc_Pizza cp
                        ON sl.MaHangHoa = cp.MaHangHoa
                       AND sl.SizeID = RTRIM(cp.SizeID)
                    WHERE cp.NguyenLieuID = @NguyenLieuID

                    UNION ALL

                    SELECT sl.MaHangHoa,
                           sl.SizeID,
                           sl.SoLuongBan,
                           CAST(qb.TrongLuongBot AS DECIMAL(18,4)) AS BomPerPizza,
                           ISNULL(qb.DonViID, 2) AS BomDonViID,
                           sl.SoLuongBan * CAST(qb.TrongLuongBot AS DECIMAL(18,4)) AS TotalBomAmount
                    FROM SoldLines sl
                    INNER JOIN DoanhMuc_De dd ON sl.MaDeBanh = RTRIM(dd.MaDeBanh)
                    INNER JOIN QuyDinh_Bot qb
                        ON sl.SizeID = RTRIM(qb.SizeID)
                       AND RTRIM(dd.LoaiCotBanh) = RTRIM(qb.LoaiCotBanh)
                    WHERE @NguyenLieuID = 26

                    UNION ALL

                    SELECT sl.MaHangHoa,
                           sl.SizeID,
                           sl.SoLuongBan,
                           CAST(qv.SoLuongVien AS DECIMAL(18,4)) AS BomPerPizza,
                           ISNULL(qv.DonViID, 2) AS BomDonViID,
                           sl.SoLuongBan * CAST(qv.SoLuongVien AS DECIMAL(18,4)) AS TotalBomAmount
                    FROM SoldLines sl
                    INNER JOIN QuyDinh_Vien qv
                        ON sl.MaDeBanh = RTRIM(qv.MaDeBanh)
                       AND sl.SizeID = RTRIM(qv.SizeID)
                    WHERE qv.NguyenLieuID = @NguyenLieuID
                ),
                PizzaSoldTotals AS (
                    SELECT MaHangHoa, SizeID, SUM(SoLuongBan) AS TotalPizzaSold
                    FROM SoldLines
                    GROUP BY MaHangHoa, SizeID
                )
                SELECT bm.MaHangHoa,
                       ISNULL(hh.TenHangHoa, bm.MaHangHoa) AS TenHangHoa,
                       bm.SizeID,
                       SUM(bm.TotalBomAmount) AS TotalRawConsumed,
                       AVG(bm.BomPerPizza) AS AvgBomPerPizza,
                       MAX(bm.BomDonViID) AS BomDonViID,
                       ISNULL(dvt.TenDonVi, 'g') AS TenDonViBom,
                       MAX(pst.TotalPizzaSold) AS TotalSold
                FROM BomMatched bm
                INNER JOIN PizzaSoldTotals pst ON bm.MaHangHoa = pst.MaHangHoa AND bm.SizeID = pst.SizeID
                LEFT JOIN HangHoa hh ON bm.MaHangHoa = hh.MaHangHoa
                LEFT JOIN DonViTinh dvt ON bm.BomDonViID = dvt.DonViID
                GROUP BY bm.MaHangHoa, hh.TenHangHoa, bm.SizeID, dvt.TenDonVi
                ORDER BY SUM(bm.TotalBomAmount) DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", fromDate);
            cmd.Parameters.AddWithValue("@ToDate", toDate);
            cmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);

            using var reader = await cmd.ExecuteReaderAsync();
            int stt = 1;
            while (await reader.ReadAsync())
            {
                var maHh = reader.GetString(0);
                var tenHh = reader.GetString(1);
                var sizeId = reader.GetString(2).Trim();
                var totalRaw = reader.GetDecimal(3);
                var avgBom = reader.GetDecimal(4);
                var bomDvId = reader.GetInt32(5);
                var tenDvBom = reader.GetString(6);
                var soLuongBan = reader.GetInt32(7);

                var totalStockConsumed = Math.Round(ConvertToStock(totalRaw, bomDvId), 3);
                var effectivePerUnit = soLuongBan > 0 ? Math.Round(totalRaw / soLuongBan, 1) : avgBom;

                var sizeLabel = sizeId.ToUpperInvariant() switch
                {
                    "L" => "Size L (12 inch)",
                    "R" => "Size M (9 inch)",
                    "M" => "Size M (9 inch)",
                    "S" => "Size S (7 inch)",
                    _ => $"Size {sizeId}"
                };

                result.Add(new MonAnTieuHaoItem
                {
                    STT = stt++,
                    MaHangHoa = maHh,
                    TenMonAn = tenHh,
                    SizeID = sizeId,
                    KichCoText = sizeLabel,
                    DinhMucBomText = $"{effectivePerUnit:0.##} {tenDvBom} / cái",
                    SoLuongDaBan = soLuongBan,
                    TongTieuHaoKho = totalStockConsumed,
                    DonViKho = donViKho
                });
            }
        }
        catch { }

        return result;
    }

    // =========================================================================
    // DỰ BÁO 5 MÔ HÌNH (WMA, SES, HOLT, HW ADDITIVE, HW MULTIPLICATIVE)
    // =========================================================================

    public async Task<SingleIngredientForecastDetail> GetSingleIngredientForecastDetailAsync(
        int nguyenLieuId,
        DateTime? customTrainStart = null,
        DateTime? customTrainEnd = null,
        DateTime? customTestStart = null,
        DateTime? customTestEnd = null,
        bool persistCustomEval = false)
    {
        var detail = new SingleIngredientForecastDetail
        {
            NguyenLieuID = nguyenLieuId,
            MaNguyenLieu = $"NL{nguyenLieuId:D3}"
        };

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            // 1) Thông tin nguyên liệu + Tồn kho quy đổi đúng đơn vị chuẩn của nguyên liệu
            const string infoSql = @"
                SELECT nl.TenNguyenLieu,
                       ISNULL(dvtNl.TenDonVi, 'kg') AS DonViKho,
                       ROUND(ISNULL(tk.SoLuongTon, 0) *
                             CASE
                                 WHEN ISNULL(tk.DonViID, nl.DonViID) = nl.DonViID THEN 1.0
                                 ELSE ISNULL(qdTk.HeSo, 1.0) / NULLIF(ISNULL(qdNl.HeSo, 1.0), 0)
                             END, 2) AS TonKhoChuan
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvtNl ON nl.DonViID = dvtNl.DonViID
                LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                LEFT JOIN QuyDoiDonVi qdTk ON qdTk.NguyenLieuID = nl.NguyenLieuID AND qdTk.DonViID = tk.DonViID
                LEFT JOIN QuyDoiDonVi qdNl ON qdNl.NguyenLieuID = nl.NguyenLieuID AND qdNl.DonViID = nl.DonViID
                WHERE nl.NguyenLieuID = @NlId";

            using (var infoCmd = new SqlCommand(infoSql, conn))
            {
                infoCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                using var r = await infoCmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    detail.TenNguyenLieu = r.GetString(0);
                    detail.DonViKho = r.GetString(1);
                    detail.TonKhoHienTai = r.GetDecimal(2);
                }
            }

            // 2) Đọc toàn bộ chuỗi lịch sử tiêu hao của nguyên liệu từ SQL
            var allDates = new List<DateTime>();
            var allVals = new List<double>();
            using (var thCmd = new SqlCommand(@"
                SELECT KyTieuHao, SoLuongTieuHao
                FROM DuLieuTieuHao
                WHERE NguyenLieuID = @NlId
                ORDER BY KyTieuHao ASC", conn))
            {
                thCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                using var r = await thCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var d = r.GetDateTime(0);
                    allDates.Add(new DateTime(d.Year, d.Month, 1));
                    allVals.Add((double)r.GetDecimal(1));
                }
            }

            if (allDates.Count == 0)
                return detail;

            var latestKy = allDates[^1];
            var nextKy = latestKy.AddMonths(1);
            detail.LatestActualKy = latestKy;
            detail.NextForecastKy = nextKy;
            detail.LastKyActual = (decimal)allVals[^1];

            // Đảm bảo đã có đủ 5 mô hình trong SQL cho nextKy
            int countNextInSql = 0;
            using (var cntCmd = new SqlCommand("SELECT COUNT(*) FROM DuBao WHERE NguyenLieuID = @NlId AND KyDuBao = @KyNext AND TrangThai = 1", conn))
            {
                cntCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                cntCmd.Parameters.AddWithValue("@KyNext", nextKy);
                countNextInSql = Convert.ToInt32(await cntCmd.ExecuteScalarAsync() ?? 0);
            }
            if (countNextInSql < 5)
            {
                await EnsureFiveModelsForecastForPeriodAsync(conn, latestKy);
            }

            // 3) Đọc mô hình đang được chọn (LaMoHinhDuocChon = 1) và số lượng dự báo cho nextKy từ SQL DuBao
            var sqlForecastNextMap = new Dictionary<int, decimal>();
            int activePpId = 2;
            using (var dbNextCmd = new SqlCommand(@"
                SELECT PhuongPhapID, SoLuongDuBao, LaMoHinhDuocChon
                FROM DuBao
                WHERE NguyenLieuID = @NlId AND KyDuBao = @KyNext AND TrangThai = 1
                ORDER BY PhuongPhapID", conn))
            {
                dbNextCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                dbNextCmd.Parameters.AddWithValue("@KyNext", nextKy);
                using var r = await dbNextCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int pid = r.GetInt32(0);
                    decimal fval = r.GetDecimal(1);
                    bool chosen = r.GetBoolean(2);
                    sqlForecastNextMap[pid] = fval;
                    if (chosen) activePpId = pid;
                }
            }
            detail.ActivePhuongPhapID = activePpId;

            // 4) Đọc kết quả dự báo của tháng vừa rồi (latestKy) từ SQL DuBao
            using (var lastKyCmd = new SqlCommand(@"
                SELECT TOP 1 db.PhuongPhapID, pp.TenPhuongPhap, db.SoLuongDuBao
                FROM DuBao db
                LEFT JOIN PhuongPhapDuBao pp ON db.PhuongPhapID = pp.PhuongPhapID
                WHERE db.NguyenLieuID = @NlId AND db.KyDuBao = @LatestKy AND db.TrangThai = 1
                ORDER BY db.LaMoHinhDuocChon DESC, db.PhuongPhapID ASC", conn))
            {
                lastKyCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                lastKyCmd.Parameters.AddWithValue("@LatestKy", latestKy);
                using var r = await lastKyCmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    int lPid = r.GetInt32(0);
                    string lName = r.IsDBNull(1) ? "" : r.GetString(1);
                    detail.HasLastKyData = true;
                    detail.LastKyMethodName = ForecastMethodHelper.ToShortName(lPid, lName);
                    detail.LastKyForecast = Math.Round(r.GetDecimal(2), 2);
                }
            }

            // 5) Xác định khoảng Huấn luyện (Train) & Kiểm thử (Test)
            var (defTrEndIdx, defTeStartIdx) = DefaultSplitIndices(allDates.Count);
            DateTime trStart = customTrainStart ?? allDates[0];
            DateTime trEnd = customTrainEnd ?? allDates[defTrEndIdx];
            DateTime teStart = customTestStart ?? allDates[defTeStartIdx];
            DateTime teEnd = customTestEnd ?? latestKy;

            // Nếu chưa truyền custom, ưu tiên đọc khoảng Train/Test đã lưu trong DanhGiaDuBao của SQL
            if (!customTrainStart.HasValue)
            {
                using var dgRangeCmd = new SqlCommand(@"
                    SELECT TOP 1 TuKyTrain, DenKyTrain, TuKyTest, DenKyTest
                    FROM DanhGiaDuBao
                    WHERE NguyenLieuID = @NlId AND DenKyTest = @LatestKy
                    ORDER BY DanhGiaID DESC", conn);
                dgRangeCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                dgRangeCmd.Parameters.AddWithValue("@LatestKy", latestKy);
                using var r = await dgRangeCmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    trStart = new DateTime(r.GetDateTime(0).Year, r.GetDateTime(0).Month, 1);
                    trEnd = new DateTime(r.GetDateTime(1).Year, r.GetDateTime(1).Month, 1);
                    teStart = new DateTime(r.GetDateTime(2).Year, r.GetDateTime(2).Month, 1);
                    teEnd = new DateTime(r.GetDateTime(3).Year, r.GetDateTime(3).Month, 1);
                }
            }

            // Chuẩn hóa chỉ số trên mảng thời gian thực tế
            int idxStart = FindNearestIndex(allDates, trStart);
            int idxTrEnd = Math.Max(idxStart, FindNearestIndex(allDates, trEnd));
            int idxTeStart = Math.Max(idxTrEnd + 1, FindNearestIndex(allDates, teStart));
            if (idxTeStart >= allDates.Count) idxTeStart = allDates.Count - 1;
            if (idxTrEnd >= idxTeStart && idxTeStart > idxStart) idxTrEnd = idxTeStart - 1;
            int idxTeEnd = Math.Max(idxTeStart, FindNearestIndex(allDates, teEnd));

            detail.TrainStart = allDates[idxStart];
            detail.TrainEnd = allDates[idxTrEnd];
            detail.TestStart = allDates[idxTeStart];
            detail.TestEnd = allDates[idxTeEnd];
            detail.TrainMonthsCount = idxTrEnd - idxStart + 1;
            detail.TestMonthsCount = idxTeEnd - idxTeStart + 1;

            // Cắt chuỗi từ TrainStart..TestEnd để huấn luyện & kiểm thử đúng khoảng chọn
            int subLen = idxTeEnd - idxStart + 1;
            double[] ySub = new double[subLen];
            for (int i = 0; i < subLen; i++)
                ySub[i] = allVals[idxStart + i];

            int localTrainEnd = idxTrEnd - idxStart;
            int localTestStart = idxTeStart - idxStart;
            int localTestEnd = subLen - 1;

            var evalAdd = FitEvalHw(ySub, 12, localTrainEnd, localTestStart, localTestEnd, isMul: false);
            var evalMul = FitEvalHw(ySub, 12, localTrainEnd, localTestStart, localTestEnd, isMul: true);
            var evalWma = RunWma(ySub, localTestStart, localTestEnd);
            var evalSes = FitEvalSes(ySub, localTrainEnd, localTestStart, localTestEnd);
            var evalHolt = FitEvalHolt(ySub, localTrainEnd, localTestStart, localTestEnd);

            var rawModels = new Dictionary<int, (double NextF, double[] Preds, double Mae, double Rmse, double Mape)>
            {
                [1] = evalAdd,
                [2] = evalMul,
                [3] = evalWma,
                [4] = evalSes,
                [5] = evalHolt
            };

            int testCount = localTestEnd - localTestStart + 1;
            detail.TestActualValues = new double[testCount];
            for (int i = 0; i < testCount; i++)
            {
                var d = allDates[idxTeStart + i];
                detail.TestMonthLabels.Add($"{d:MM/yyyy}");
                detail.TestActualValues[i] = Math.Round(ySub[localTestStart + i], 2);
            }

            int bestPp = rawModels.OrderBy(kv => kv.Value.Mape).First().Key;
            detail.OptimalPhuongPhapID = bestPp;

            foreach (var (ppId, res) in rawModels)
            {
                var testPreds = new double[testCount];
                for (int i = 0; i < testCount; i++)
                    testPreds[i] = Math.Round(res.Preds[localTestStart + i], 2);

                // Nếu đang ở kỳ mới nhất và không chạy custom backtest, lấy đúng số dự báo đã lưu trong bảng DuBao của SQL
                decimal nextF = (!persistCustomEval && idxTeEnd == allDates.Count - 1 && sqlForecastNextMap.TryGetValue(ppId, out var sqlF))
                    ? Math.Round(sqlF, 2)
                    : Math.Round((decimal)res.NextF, 2);

                detail.Models[ppId] = new ModelEvalSummary
                {
                    PhuongPhapID = ppId,
                    DonViKho = detail.DonViKho,
                    NextForecast = nextF,
                    Mae = Math.Round((decimal)res.Mae, 2),
                    Rmse = Math.Round((decimal)res.Rmse, 2),
                    Mape = Math.Round((decimal)res.Mape, 2),
                    TestPredictions = testPreds
                };
            }

            // Tạo danh sách các dòng đối chiếu từng tháng kiểm thử
            for (int i = 0; i < testCount; i++)
            {
                decimal act = (decimal)detail.TestActualValues[i];
                detail.MonthRows.Add(new BacktestMonthRow
                {
                    ThangText = detail.TestMonthLabels[i],
                    Actual = act,
                    DonViKho = detail.DonViKho,
                    Wma = new ModelCellValue { Forecast = (decimal)detail.Models[3].TestPredictions[i], Actual = act, DonViKho = detail.DonViKho },
                    Ses = new ModelCellValue { Forecast = (decimal)detail.Models[4].TestPredictions[i], Actual = act, DonViKho = detail.DonViKho },
                    Holt = new ModelCellValue { Forecast = (decimal)detail.Models[5].TestPredictions[i], Actual = act, DonViKho = detail.DonViKho },
                    HwAdd = new ModelCellValue { Forecast = (decimal)detail.Models[1].TestPredictions[i], Actual = act, DonViKho = detail.DonViKho },
                    HwMul = new ModelCellValue { Forecast = (decimal)detail.Models[2].TestPredictions[i], Actual = act, DonViKho = detail.DonViKho }
                });
            }

            // Nếu người dùng bấm "Chạy lại kiểm thử", lưu kết quả đánh giá mới vào bảng DanhGiaDuBao trong SQL
            if (persistCustomEval)
            {
                using var tx = conn.BeginTransaction();
                try
                {
                    using (var delCmd = new SqlCommand("DELETE FROM DanhGiaDuBao WHERE NguyenLieuID = @NlId AND DenKyTest = @DenTest", conn, tx))
                    {
                        delCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                        delCmd.Parameters.AddWithValue("@DenTest", detail.TestEnd);
                        await delCmd.ExecuteNonQueryAsync();
                    }

                    foreach (var (ppId, mSum) in detail.Models)
                    {
                        using var insCmd = new SqlCommand(@"
                            INSERT INTO DanhGiaDuBao (NguyenLieuID, PhuongPhapID, TuKyTrain, DenKyTrain, TuKyTest, DenKyTest, MAE, RMSE, MAPE, NgayDanhGia)
                            VALUES (@NlId, @PpId, @TuTr, @DenTr, @TuTe, @DenTe, @Mae, @Rmse, @Mape, GETDATE())", conn, tx);
                        insCmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
                        insCmd.Parameters.AddWithValue("@PpId", ppId);
                        insCmd.Parameters.AddWithValue("@TuTr", detail.TrainStart);
                        insCmd.Parameters.AddWithValue("@DenTr", detail.TrainEnd);
                        insCmd.Parameters.AddWithValue("@TuTe", detail.TestStart);
                        insCmd.Parameters.AddWithValue("@DenTe", detail.TestEnd);
                        insCmd.Parameters.AddWithValue("@Mae", mSum.Mae);
                        insCmd.Parameters.AddWithValue("@Rmse", mSum.Rmse);
                        insCmd.Parameters.AddWithValue("@Mape", mSum.Mape);
                        await insCmd.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                }
            }
        }
        catch { }

        return detail;
    }

    /// <summary>
    /// Cập nhật mô hình được chọn làm chính (LaMoHinhDuocChon = 1) trong bảng SQL DuBao.
    /// </summary>
    public async Task<bool> SetActiveForecastModelAsync(int nguyenLieuId, DateTime kyDuBao, int phuongPhapId, decimal? soLuongDuBao = null)
    {
        var ky = new DateTime(kyDuBao.Year, kyDuBao.Month, 1);
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
                UPDATE DuBao
                SET LaMoHinhDuocChon = CASE WHEN PhuongPhapID = @PpId THEN 1 ELSE 0 END,
                    SoLuongDuBao = CASE WHEN PhuongPhapID = @PpId AND @NewVal IS NOT NULL THEN @NewVal ELSE SoLuongDuBao END,
                    NgayDuBao = GETDATE()
                WHERE NguyenLieuID = @NlId AND KyDuBao = @KyDuBao AND TrangThai = 1", conn);
            cmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
            cmd.Parameters.AddWithValue("@KyDuBao", ky);
            cmd.Parameters.AddWithValue("@PpId", phuongPhapId);
            cmd.Parameters.AddWithValue("@NewVal", soLuongDuBao.HasValue ? Math.Round(soLuongDuBao.Value, 3) : (object)DBNull.Value);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Đọc lịch sử dự báo của một nguyên liệu qua các kỳ từ bảng SQL DuBao & DuLieuTieuHao.
    /// </summary>
    public async Task<List<LichSuDuBaoRow>> GetLichSuDuBaoAsync(int nguyenLieuId, string donViKho)
    {
        var rows = new List<LichSuDuBaoRow>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT db.KyDuBao,
                       db.NgayDuBao,
                       db.PhuongPhapID,
                       pp.TenPhuongPhap,
                       db.SoLuongDuBao,
                       th.SoLuongTieuHao
                FROM DuBao db
                LEFT JOIN PhuongPhapDuBao pp ON db.PhuongPhapID = pp.PhuongPhapID
                LEFT JOIN DuLieuTieuHao th ON th.NguyenLieuID = db.NguyenLieuID AND th.KyTieuHao = db.KyDuBao
                WHERE db.NguyenLieuID = @NlId
                  AND db.LaMoHinhDuocChon = 1
                  AND db.TrangThai = 1
                ORDER BY db.KyDuBao DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NlId", nguyenLieuId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var ky = r.GetDateTime(0);
                var ngayDb = r.IsDBNull(1) ? ky.AddDays(-1) : r.GetDateTime(1);
                int ppId = r.GetInt32(2);
                string ppName = r.IsDBNull(3) ? "" : r.GetString(3);
                decimal duBao = Math.Round(r.GetDecimal(4), 2);
                bool isPending = r.IsDBNull(5);

                string thucTeStr = "--";
                string saiSoStr = "--";
                string apeStr = "--";

                if (!isPending)
                {
                    decimal thucTe = Math.Round(r.GetDecimal(5), 2);
                    decimal saiSo = Math.Abs(thucTe - duBao);
                    decimal ape = thucTe > 0.001m ? Math.Round(saiSo / thucTe * 100m, 2) : 0m;
                    thucTeStr = $"{thucTe:N2} {donViKho}";
                    saiSoStr = $"{saiSo:N2} {donViKho}";
                    apeStr = $"{ape:N2}%";
                }

                rows.Add(new LichSuDuBaoRow
                {
                    KyDuBao = ky,
                    IsPending = isPending,
                    NgayThucHienText = $"{ngayDb:dd/MM/yyyy}",
                    TenPhuongPhap = ForecastMethodHelper.ToShortName(ppId, ppName),
                    TieuHaoDuBaoText = $"{duBao:N2} {donViKho}",
                    TieuHaoThucTeText = thucTeStr,
                    SaiSoLechText = saiSoStr,
                    ApeText = apeStr
                });
            }
        }
        catch { }

        return rows;
    }

    private async Task EnsureFiveModelsForecastForPeriodAsync(SqlConnection conn, DateTime kyEnd)
    {
        var kyForecast = kyEnd.AddMonths(1);

        // Đảm bảo đủ 5 dòng trong PhuongPhapDuBao
        int ppCount = Convert.ToInt32(await new SqlCommand("SELECT COUNT(*) FROM PhuongPhapDuBao", conn).ExecuteScalarAsync() ?? 0);
        if (ppCount < 5)
        {
            const string ensurePpSql = @"
                IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao WHERE PhuongPhapID = 1)
                    INSERT INTO PhuongPhapDuBao (TenPhuongPhap, LoaiMuaVu, ChuKyMuaVu, TrangThai) VALUES (N'Holt-Winters cộng tính', N'Additive', 12, 1);
                IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao WHERE PhuongPhapID = 2)
                    INSERT INTO PhuongPhapDuBao (TenPhuongPhap, LoaiMuaVu, ChuKyMuaVu, TrangThai) VALUES (N'Holt-Winters nhân tính', N'Multiplicative', 12, 1);
                IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao WHERE TenPhuongPhap LIKE N'%WMA%')
                    INSERT INTO PhuongPhapDuBao (TenPhuongPhap, LoaiMuaVu, ChuKyMuaVu, TrangThai) VALUES (N'Trung bình trượt có trọng số (WMA)', N'None', 3, 1);
                IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao WHERE TenPhuongPhap LIKE N'%SES%')
                    INSERT INTO PhuongPhapDuBao (TenPhuongPhap, LoaiMuaVu, ChuKyMuaVu, TrangThai) VALUES (N'San mũ đơn giản (SES)', N'None', 0, 1);
                IF NOT EXISTS (SELECT 1 FROM PhuongPhapDuBao WHERE TenPhuongPhap LIKE N'%Holt Linear%' OR TenPhuongPhap LIKE N'%Holt tuyến tính%')
                    INSERT INTO PhuongPhapDuBao (TenPhuongPhap, LoaiMuaVu, ChuKyMuaVu, TrangThai) VALUES (N'Holt tuyến tính (Holt Linear)', N'Trend', 0, 1);";
            using var ppCmd = new SqlCommand(ensurePpSql, conn);
            await ppCmd.ExecuteNonQueryAsync();
        }

        var nlIds = new List<int>();
        using (var nlCmd = new SqlCommand("SELECT NguyenLieuID FROM NguyenLieu WHERE ISNULL(TrangThai, 1) = 1 ORDER BY NguyenLieuID", conn))
        {
            using var r = await nlCmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) nlIds.Add(r.GetInt32(0));
        }

        var seriesMap = new Dictionary<int, List<(DateTime Ky, double Val)>>();
        using (var thCmd = new SqlCommand("SELECT NguyenLieuID, KyTieuHao, SoLuongTieuHao FROM DuLieuTieuHao WHERE KyTieuHao <= @KyEnd ORDER BY NguyenLieuID, KyTieuHao", conn))
        {
            thCmd.Parameters.AddWithValue("@KyEnd", kyEnd);
            using var r = await thCmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int id = r.GetInt32(0);
                if (!seriesMap.TryGetValue(id, out var list))
                {
                    list = new List<(DateTime, double)>();
                    seriesMap[id] = list;
                }
                list.Add((r.GetDateTime(1), (double)r.GetDecimal(2)));
            }
        }

        using var tx = conn.BeginTransaction();
        try
        {
            using (var delDb = new SqlCommand("DELETE FROM DuBao WHERE KyDuBao = @KyDuBao", conn, tx))
            {
                delDb.Parameters.AddWithValue("@KyDuBao", kyForecast);
                await delDb.ExecuteNonQueryAsync();
            }
            using (var delDg = new SqlCommand("DELETE FROM DanhGiaDuBao WHERE DenKyTest = @KyEnd", conn, tx))
            {
                delDg.Parameters.AddWithValue("@KyEnd", kyEnd);
                await delDg.ExecuteNonQueryAsync();
            }

            foreach (var nlId in nlIds)
            {
                if (!seriesMap.TryGetValue(nlId, out var pts) || pts.Count == 0) continue;
                var y = pts.Select(x => x.Val).ToArray();
                int n = y.Length;
                var (trEnd, teStart) = DefaultSplitIndices(n);
                int teEnd = n - 1;

                var models = new Dictionary<int, (double NextF, double[] Preds, double Mae, double Rmse, double Mape)>
                {
                    [1] = FitEvalHw(y, 12, trEnd, teStart, teEnd, false),
                    [2] = FitEvalHw(y, 12, trEnd, teStart, teEnd, true),
                    [3] = RunWma(y, teStart, teEnd),
                    [4] = FitEvalSes(y, trEnd, teStart, teEnd),
                    [5] = FitEvalHolt(y, trEnd, teStart, teEnd)
                };

                int bestPp = models.OrderBy(kv => kv.Value.Mape).First().Key;

                foreach (var (ppId, m) in models)
                {
                    using (var insDg = new SqlCommand(@"
                        INSERT INTO DanhGiaDuBao (NguyenLieuID, PhuongPhapID, TuKyTrain, DenKyTrain, TuKyTest, DenKyTest, MAE, RMSE, MAPE, NgayDanhGia)
                        VALUES (@NlId, @PpId, @TuTr, @DenTr, @TuTe, @DenTe, @Mae, @Rmse, @Mape, GETDATE())", conn, tx))
                    {
                        insDg.Parameters.AddWithValue("@NlId", nlId);
                        insDg.Parameters.AddWithValue("@PpId", ppId);
                        insDg.Parameters.AddWithValue("@TuTr", pts[0].Ky);
                        insDg.Parameters.AddWithValue("@DenTr", pts[trEnd].Ky);
                        insDg.Parameters.AddWithValue("@TuTe", pts[teStart].Ky);
                        insDg.Parameters.AddWithValue("@DenTe", kyEnd);
                        insDg.Parameters.AddWithValue("@Mae", Math.Round((decimal)m.Mae, 4));
                        insDg.Parameters.AddWithValue("@Rmse", Math.Round((decimal)m.Rmse, 4));
                        insDg.Parameters.AddWithValue("@Mape", Math.Round((decimal)m.Mape, 4));
                        await insDg.ExecuteNonQueryAsync();
                    }

                    using (var insDb = new SqlCommand(@"
                        INSERT INTO DuBao (NguyenLieuID, PhuongPhapID, KyDuBao, SoLuongDuBao, LaMoHinhDuocChon, NgayDuBao, TrangThai)
                        VALUES (@NlId, @PpId, @KyDuBao, @Val, @Chosen, GETDATE(), 1)", conn, tx))
                    {
                        insDb.Parameters.AddWithValue("@NlId", nlId);
                        insDb.Parameters.AddWithValue("@PpId", ppId);
                        insDb.Parameters.AddWithValue("@KyDuBao", kyForecast);
                        insDb.Parameters.AddWithValue("@Val", Math.Round((decimal)m.NextF, 3));
                        insDb.Parameters.AddWithValue("@Chosen", ppId == bestPp);
                        await insDb.ExecuteNonQueryAsync();
                    }
                }
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
        }
    }

    public async Task<List<KetQuaDuBaoItem>> RunHoltWintersForecastAsync(DateTime currentKy, int? filterNguyenLieuId = null)
    {
        var results = new List<KetQuaDuBaoItem>();
        var kyEnd = new DateTime(currentKy.Year, currentKy.Month, 1);
        var kyForecast = kyEnd.AddMonths(1);

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT nl.NguyenLieuID,
                       nl.TenNguyenLieu,
                       ISNULL(dvt.TenDonVi, 'kg') AS TenDonVi,
                       ROUND(ISNULL(tk.SoLuongTon, 0) *
                             CASE
                                 WHEN ISNULL(tk.DonViID, nl.DonViID) = nl.DonViID THEN 1.0
                                 ELSE ISNULL(qdTk.HeSo, 1.0) / NULLIF(ISNULL(qdNl.HeSo, 1.0), 0)
                             END, 2) AS SoLuongTon,
                       ISNULL(thCur.SoLuongTieuHao, 0) AS TieuHaoHienTai,
                       ISNULL(dbAdd.SoLuongDuBao, 0) AS DuBaoAdd,
                       ISNULL(dgAdd.MAE, 0) AS MaeAdd,
                       ISNULL(dgAdd.RMSE, 0) AS RmseAdd,
                       ISNULL(dgAdd.MAPE, 0) AS MapeAdd,
                       ISNULL(dbMul.SoLuongDuBao, 0) AS DuBaoMul,
                       ISNULL(dgMul.MAE, 0) AS MaeMul,
                       ISNULL(dgMul.RMSE, 0) AS RmseMul,
                       ISNULL(dgMul.MAPE, 0) AS MapeMul,
                       ISNULL(dbChon.PhuongPhapID, 1) AS ChonPpId,
                       ISNULL(dbChon.TenPhuongPhap, N'Holt-Winters cộng tính') AS ChonTen,
                       ISNULL(dbChon.SoLuongDuBao, 0) AS ChonVal,
                       ISNULL(dgChon.MAE, 0) AS ChonMae,
                       ISNULL(dgChon.RMSE, 0) AS ChonRmse,
                       ISNULL(dgChon.MAPE, 0) AS ChonMape
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                LEFT JOIN QuyDoiDonVi qdTk ON qdTk.NguyenLieuID = nl.NguyenLieuID AND qdTk.DonViID = tk.DonViID
                LEFT JOIN QuyDoiDonVi qdNl ON qdNl.NguyenLieuID = nl.NguyenLieuID AND qdNl.DonViID = nl.DonViID
                LEFT JOIN DuLieuTieuHao thCur ON nl.NguyenLieuID = thCur.NguyenLieuID AND thCur.KyTieuHao = @KyEnd
                LEFT JOIN DuBao dbAdd ON nl.NguyenLieuID = dbAdd.NguyenLieuID AND dbAdd.KyDuBao = @KyDuBao AND dbAdd.PhuongPhapID = 1 AND dbAdd.TrangThai = 1
                LEFT JOIN DuBao dbMul ON nl.NguyenLieuID = dbMul.NguyenLieuID AND dbMul.KyDuBao = @KyDuBao AND dbMul.PhuongPhapID = 2 AND dbMul.TrangThai = 1
                LEFT JOIN DanhGiaDuBao dgAdd ON nl.NguyenLieuID = dgAdd.NguyenLieuID AND dgAdd.DenKyTest = @KyEnd AND dgAdd.PhuongPhapID = 1
                LEFT JOIN DanhGiaDuBao dgMul ON nl.NguyenLieuID = dgMul.NguyenLieuID AND dgMul.DenKyTest = @KyEnd AND dgMul.PhuongPhapID = 2
                OUTER APPLY (
                    SELECT TOP 1 db.PhuongPhapID, pp.TenPhuongPhap, db.SoLuongDuBao
                    FROM DuBao db
                    LEFT JOIN PhuongPhapDuBao pp ON db.PhuongPhapID = pp.PhuongPhapID
                    WHERE db.NguyenLieuID = nl.NguyenLieuID AND db.KyDuBao = @KyDuBao AND db.TrangThai = 1
                    ORDER BY db.LaMoHinhDuocChon DESC, db.PhuongPhapID ASC
                ) dbChon
                LEFT JOIN DanhGiaDuBao dgChon ON nl.NguyenLieuID = dgChon.NguyenLieuID AND dgChon.DenKyTest = @KyEnd AND dgChon.PhuongPhapID = dbChon.PhuongPhapID
                WHERE ISNULL(nl.TrangThai, 1) = 1";

            if (filterNguyenLieuId.HasValue)
                sql += " AND nl.NguyenLieuID = @NlId";
            sql += " ORDER BY nl.NguyenLieuID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@KyEnd", kyEnd);
            cmd.Parameters.AddWithValue("@KyDuBao", kyForecast);
            if (filterNguyenLieuId.HasValue)
                cmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);

            using var r = await cmd.ExecuteReaderAsync();
            int stt = 1;
            while (await r.ReadAsync())
            {
                int id = r.GetInt32(0);
                results.Add(new KetQuaDuBaoItem
                {
                    STT = stt++,
                    NguyenLieuID = id,
                    MaNguyenLieu = $"NL{id:D3}",
                    TenNguyenLieu = r.GetString(1),
                    DonViKho = r.GetString(2),
                    TonKhoHienTai = r.GetDecimal(3),
                    TieuHaoKyHienTai = Math.Round(r.GetDecimal(4), 2),
                    DuBaoCongTinh = Math.Round(r.GetDecimal(5), 2),
                    MaeCongTinh = Math.Round(r.GetDecimal(6), 2),
                    RmseCongTinh = Math.Round(r.GetDecimal(7), 2),
                    MapeCongTinh = Math.Round(r.GetDecimal(8), 2),
                    DuBaoNhanTinh = Math.Round(r.GetDecimal(9), 2),
                    MaeNhanTinh = Math.Round(r.GetDecimal(10), 2),
                    RmseNhanTinh = Math.Round(r.GetDecimal(11), 2),
                    MapeNhanTinh = Math.Round(r.GetDecimal(12), 2),
                    PhuongPhapDuocChon = ForecastMethodHelper.ToShortName(r.GetInt32(13), r.GetString(14)),
                    SoLuongDuBaoChon = Math.Round(r.GetDecimal(15), 2),
                    MaeChon = Math.Round(r.GetDecimal(16), 2),
                    RmseChon = Math.Round(r.GetDecimal(17), 2),
                    MapeChon = Math.Round(r.GetDecimal(18), 2)
                });
            }
        }
        catch { }

        return results;
    }

    // =========================================================================
    // THUẬT TOÁN 5 MÔ HÌNH DỰ BÁO CHUỖI THỜI GIAN
    // =========================================================================

    private static (int TrainEndIdx, int TestStartIdx) DefaultSplitIndices(int n)
    {
        if (n >= 25) return (23, 24);
        if (n > 12) return (11, 12);
        if (n > 3)
        {
            int testLen = Math.Max(1, n / 3);
            return (n - testLen - 1, n - testLen);
        }
        if (n > 1) return (n - 2, n - 1);
        return (0, 0);
    }

    private static int FindNearestIndex(List<DateTime> dates, DateTime target)
    {
        var norm = new DateTime(target.Year, target.Month, 1);
        for (int i = 0; i < dates.Count; i++)
        {
            if (dates[i] >= norm) return i;
        }
        return dates.Count - 1;
    }

    private static (double Mae, double Rmse, double Mape) CalcMetrics(double[] y, double[] preds, int startIdx, int endIdx)
    {
        int n = y.Length;
        if (n == 0) return (0, 0, 0);
        int s = Math.Clamp(startIdx, 0, n - 1);
        int e = Math.Clamp(endIdx, s, n - 1);
        double sumAbs = 0, sumSq = 0, sumPct = 0;
        int cnt = 0;
        for (int t = s; t <= e; t++)
        {
            double err = Math.Abs(y[t] - preds[t]);
            sumAbs += err;
            sumSq += err * err;
            if (y[t] > 0.001) sumPct += (err / y[t]) * 100.0;
            cnt++;
        }
        if (cnt == 0) return (0, 0, 0);
        return (sumAbs / cnt, Math.Sqrt(sumSq / cnt), sumPct / cnt);
    }

    private static (double NextF, double[] Preds, double Mae, double Rmse, double Mape) RunWma(double[] y, int testStart, int testEnd)
    {
        int n = y.Length;
        var preds = new double[n];
        for (int t = 0; t < n; t++)
        {
            if (t == 0) preds[t] = y[0];
            else if (t == 1) preds[t] = y[0];
            else if (t == 2) preds[t] = 0.6 * y[1] + 0.4 * y[0];
            else preds[t] = 0.5 * y[t - 1] + 0.3 * y[t - 2] + 0.2 * y[t - 3];
        }

        double nextF = n switch
        {
            0 => 0.1,
            1 => y[0],
            2 => 0.6 * y[1] + 0.4 * y[0],
            _ => 0.5 * y[n - 1] + 0.3 * y[n - 2] + 0.2 * y[n - 3]
        };

        var (mae, rmse, mape) = CalcMetrics(y, preds, testStart, testEnd);
        return (Math.Max(0.1, nextF), preds, mae, rmse, mape);
    }

    private static (double NextF, double[] Preds) SimSes(double[] y, int len, double alpha)
    {
        var preds = new double[len];
        double level = len > 0 && y[0] > 0 ? y[0] : 1.0;
        for (int t = 0; t < len; t++)
        {
            preds[t] = Math.Max(0.1, level);
            level = alpha * y[t] + (1.0 - alpha) * level;
        }
        return (Math.Max(0.1, level), preds);
    }

    private static (double NextF, double[] Preds, double Mae, double Rmse, double Mape) FitEvalSes(double[] y, int trainEnd, int testStart, int testEnd)
    {
        double bestA = 0.35;
        int trainLen = trainEnd + 1;
        if (trainEnd >= 2)
        {
            double bestMp = double.MaxValue;
            foreach (var a in new[] { 0.15, 0.30, 0.45, 0.60, 0.75 })
            {
                var (_, p) = SimSes(y, trainLen, a);
                var (_, _, mp) = CalcMetrics(y, p, 1, trainEnd);
                if (mp < bestMp) { bestMp = mp; bestA = a; }
            }
        }
        var (nextF, preds) = SimSes(y, y.Length, bestA);
        var (mae, rmse, mape) = CalcMetrics(y, preds, testStart, testEnd);
        return (nextF, preds, mae, rmse, mape);
    }

    private static (double NextF, double[] Preds) SimHolt(double[] y, int len, double alpha, double beta)
    {
        var preds = new double[len];
        if (len == 0) return (0.1, preds);
        double level = y[0] > 0 ? y[0] : 1.0;
        int stepRef = Math.Min(len - 1, 3);
        double trend = len > 1 ? (y[stepRef] - y[0]) / Math.Max(1, stepRef) : 0.0;
        preds[0] = level;
        for (int t = 1; t < len; t++)
        {
            preds[t] = Math.Max(0.1, level + trend);
            double prevL = level;
            level = alpha * y[t] + (1.0 - alpha) * (prevL + trend);
            trend = beta * (level - prevL) + (1.0 - beta) * trend;
        }
        return (Math.Max(0.1, level + trend), preds);
    }

    private static (double NextF, double[] Preds, double Mae, double Rmse, double Mape) FitEvalHolt(double[] y, int trainEnd, int testStart, int testEnd)
    {
        double bestA = 0.35, bestB = 0.15;
        int trainLen = trainEnd + 1;
        if (trainEnd >= 2)
        {
            double bestMp = double.MaxValue;
            foreach (var a in new[] { 0.20, 0.35, 0.50, 0.65 })
            {
                foreach (var b in new[] { 0.05, 0.15, 0.25 })
                {
                    var (_, p) = SimHolt(y, trainLen, a, b);
                    var (_, _, mp) = CalcMetrics(y, p, 1, trainEnd);
                    if (mp < bestMp) { bestMp = mp; bestA = a; bestB = b; }
                }
            }
        }
        var (nextF, preds) = SimHolt(y, y.Length, bestA, bestB);
        var (mae, rmse, mape) = CalcMetrics(y, preds, testStart, testEnd);
        return (nextF, preds, mae, rmse, mape);
    }

    private static (double NextF, double[] Preds) SimHw(double[] y, int len, int L, double alpha, double beta, double gamma, bool isMul)
    {
        var preds = new double[len];
        if (len == 0) return (0.1, preds);
        if (len < L)
        {
            double lvl = y[0] > 0 ? y[0] : 1.0;
            double tr = isMul
                ? Math.Clamp(len > 1 && y[0] > 0.001 ? y[1] / y[0] : 1.0, 0.85, 1.15)
                : (len > 1 ? y[1] - y[0] : 0.0);
            preds[0] = lvl;
            for (int t = 1; t < len; t++)
            {
                preds[t] = Math.Max(0.1, isMul ? lvl * tr : lvl + tr);
                double prevL = lvl;
                if (isMul)
                {
                    double safeT = Math.Abs(tr) > 0.001 ? tr : 1.0;
                    lvl = alpha * y[t] + (1.0 - alpha) * (prevL * safeT);
                    double safeP = Math.Abs(prevL) > 0.001 ? prevL : 1.0;
                    tr = Math.Clamp(beta * (lvl / safeP) + (1.0 - beta) * safeT, 0.85, 1.15);
                }
                else
                {
                    lvl = alpha * y[t] + (1.0 - alpha) * (prevL + tr);
                    tr = beta * (lvl - prevL) + (1.0 - beta) * tr;
                }
            }
            return (Math.Max(0.1, isMul ? lvl * tr : lvl + tr), preds);
        }

        var seasonals = new double[len + L + 4];
        double level, trend;
        if (len >= 2 * L)
        {
            double avg1 = 0, avg2 = 0;
            for (int i = 0; i < L; i++) { avg1 += y[i]; avg2 += y[i + L]; }
            avg1 /= L; avg2 /= L;
            level = avg1 > 0 ? avg1 : 1.0;
            trend = (avg2 - avg1) / (L * L);
        }
        else
        {
            double avg1 = 0;
            for (int i = 0; i < L; i++) avg1 += y[i];
            avg1 /= L;
            level = avg1 > 0 ? avg1 : 1.0;
            trend = (y[len - 1] - y[0]) / Math.Max(1.0, (len - 1) * 2.0);
        }

        for (int i = 0; i < L; i++)
        {
            seasonals[i] = isMul
                ? (level > 0.001 ? Math.Max(0.1, y[i] / level) : 1.0)
                : (y[i] - level);
            preds[i] = y[i];
        }

        for (int t = L; t < len; t++)
        {
            double sPrev = seasonals[t - L];
            double yHat = isMul ? (level + trend) * sPrev : (level + trend) + sPrev;
            preds[t] = Math.Max(0.1, yHat);

            double prevL = level;
            if (isMul)
            {
                double safeS = Math.Abs(sPrev) > 0.001 ? sPrev : 1.0;
                level = alpha * (y[t] / safeS) + (1.0 - alpha) * (prevL + trend);
                trend = beta * (level - prevL) + (1.0 - beta) * trend;
                double safeL = Math.Abs(level) > 0.001 ? level : 1.0;
                seasonals[t] = gamma * (y[t] / safeL) + (1.0 - gamma) * sPrev;
            }
            else
            {
                level = alpha * (y[t] - sPrev) + (1.0 - alpha) * (prevL + trend);
                trend = beta * (level - prevL) + (1.0 - beta) * trend;
                seasonals[t] = gamma * (y[t] - level) + (1.0 - gamma) * sPrev;
            }
        }

        double nextS = seasonals[len - L];
        double nextF = Math.Max(0.1, isMul ? (level + trend) * nextS : (level + trend) + nextS);
        return (nextF, preds);
    }

    private static (double NextF, double[] Preds, double Mae, double Rmse, double Mape) FitEvalHw(
        double[] y, int L, int trainEnd, int testStart, int testEnd, bool isMul)
    {
        double bestA = 0.35, bestB = 0.15, bestG = 0.35;
        int trainLen = trainEnd + 1;
        double[] alphas = { 0.20, 0.35, 0.50, 0.65 };
        double[] betas = { 0.05, 0.15, 0.25 };
        double[] gammas = { 0.20, 0.35, 0.50 };

        if (trainLen > L)
        {
            double bestMp = double.MaxValue;
            foreach (var a in alphas)
                foreach (var b in betas)
                    foreach (var g in gammas)
                    {
                        var (_, p) = SimHw(y, trainLen, L, a, b, g, isMul);
                        var (_, _, mp) = CalcMetrics(y, p, L, trainEnd);
                        if (mp < bestMp) { bestMp = mp; bestA = a; bestB = b; bestG = g; }
                    }
        }
        else if (y.Length > L)
        {
            double bestMp = double.MaxValue;
            foreach (var a in alphas)
                foreach (var b in betas)
                    foreach (var g in gammas)
                    {
                        var (_, p) = SimHw(y, y.Length, L, a, b, g, isMul);
                        var (_, _, mp) = CalcMetrics(y, p, Math.Max(L, testStart), testEnd);
                        if (mp < bestMp) { bestMp = mp; bestA = a; bestB = b; bestG = g; }
                    }
        }

        var (nextF, preds) = SimHw(y, y.Length, L, bestA, bestB, bestG, isMul);
        var (mae, rmse, mape) = CalcMetrics(y, preds, testStart, testEnd);
        return (nextF, preds, mae, rmse, mape);
    }

    public async Task<ThongKeMuaVu12ThangResult> GetThongKeMuaVu12ThangAsync(int nguyenLieuId, int nam)
    {
        var res = new ThongKeMuaVu12ThangResult
        {
            NguyenLieuID = nguyenLieuId,
            MaNguyenLieu = $"NL{nguyenLieuId:D3}",
            TenNguyenLieu = $"Nguyên liệu #{nguyenLieuId}",
            DonViKho = "kg",
            Nam = nam
        };

        var monthMap = new Dictionary<int, decimal>();

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            if (nguyenLieuId > 0)
            {
                const string nlSql = @"
                    SELECT nl.TenNguyenLieu, ISNULL(dvt.TenDonVi, 'kg')
                    FROM NguyenLieu nl
                    LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                    WHERE nl.NguyenLieuID = @NguyenLieuID";
                using (var nlCmd = new SqlCommand(nlSql, conn))
                {
                    nlCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
                    using var r = await nlCmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        res.TenNguyenLieu = r.GetString(0);
                        res.DonViKho = r.GetString(1);
                    }
                }

                const string thSql = @"
                    SELECT MONTH(KyTieuHao) AS Thang, SUM(SoLuongTieuHao) AS TongTieuHao
                    FROM DuLieuTieuHao
                    WHERE NguyenLieuID = @NguyenLieuID AND YEAR(KyTieuHao) = @Nam
                    GROUP BY MONTH(KyTieuHao)";
                using (var thCmd = new SqlCommand(thSql, conn))
                {
                    thCmd.Parameters.AddWithValue("@NguyenLieuID", nguyenLieuId);
                    thCmd.Parameters.AddWithValue("@Nam", nam);
                    using var r = await thCmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        monthMap[r.GetInt32(0)] = r.GetDecimal(1);
                    }
                }
            }
            else
            {
                res.MaNguyenLieu = "TẤT CẢ";
                res.TenNguyenLieu = "Tổng hợp nguyên liệu";
                res.DonViKho = "đơn vị";

                const string thSql = @"
                    SELECT MONTH(KyTieuHao) AS Thang, SUM(SoLuongTieuHao) AS TongTieuHao
                    FROM DuLieuTieuHao
                    WHERE YEAR(KyTieuHao) = @Nam
                    GROUP BY MONTH(KyTieuHao)";
                using (var thCmd = new SqlCommand(thSql, conn))
                {
                    thCmd.Parameters.AddWithValue("@Nam", nam);
                    using var r = await thCmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        monthMap[r.GetInt32(0)] = r.GetDecimal(1);
                    }
                }
            }
        }
        catch { }

        for (int m = 1; m <= 12; m++)
        {
            monthMap.TryGetValue(m, out var val);
            res.ThangItems.Add(new ThangTieuHaoChartItem
            {
                Thang = m,
                SoLuongTieuHao = Math.Round(val, 1),
                DonViKho = res.DonViKho
            });
        }

        return res;
    }
}
