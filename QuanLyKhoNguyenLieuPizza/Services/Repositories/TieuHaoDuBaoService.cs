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
                       ISNULL(thPrevY.SoLuongTieuHao, 0) AS PrevYVal
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                LEFT JOIN DuLieuTieuHao thCur ON nl.NguyenLieuID = thCur.NguyenLieuID AND thCur.KyTieuHao = @KyCur
                LEFT JOIN DuLieuTieuHao thPrevM ON nl.NguyenLieuID = thPrevM.NguyenLieuID AND thPrevM.KyTieuHao = @KyPrevM
                LEFT JOIN DuLieuTieuHao thPrevY ON nl.NguyenLieuID = thPrevY.NguyenLieuID AND thPrevY.KyTieuHao = @KyPrevY
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
                    TieuHaoCungKyNamTruoc = prevYVal
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

            decimal ConvertToStock(decimal amount, int recipeDonViId)
            {
                if (recipeDonViId == stockDonViId) return amount;
                var heSoFrom = quyDoiMap.TryGetValue(recipeDonViId, out var hf) ? hf : 1m;
                var heSoTo = quyDoiMap.TryGetValue(stockDonViId, out var ht) ? ht : 1m;
                if (heSoTo == 0) return amount;
                return amount * (heSoFrom / heSoTo);
            }

            const string sql = @"
                WITH SoldByPizza AS (
                    SELECT ct.MaHangHoa,
                           RTRIM(ct.SizeID) AS SizeID,
                           RTRIM(ct.MaDeBanh) AS MaDeBanh,
                           SUM(ISNULL(ct.SoLuong, 0)) AS TotalSold
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                    GROUP BY ct.MaHangHoa, RTRIM(ct.SizeID), RTRIM(ct.MaDeBanh)
                ),
                BomMatches AS (
                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(cp.SoLuong * s.TotalSold AS DECIMAL(18,4)) AS TotalBomAmount,
                           CAST(cp.SoLuong AS DECIMAL(18,3)) AS UnitBomAmount,
                           ISNULL(cp.DonViID, 2) AS BomDonViID,
                           s.TotalSold AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN CongThuc_Pizza cp ON s.MaHangHoa = cp.MaHangHoa AND s.SizeID = RTRIM(cp.SizeID)
                    WHERE cp.NguyenLieuID = @NguyenLieuID

                    UNION ALL

                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(qb.TrongLuongBot * s.TotalSold AS DECIMAL(18,4)) AS TotalBomAmount,
                           CAST(qb.TrongLuongBot AS DECIMAL(18,3)) AS UnitBomAmount,
                           ISNULL(qb.DonViID, 2) AS BomDonViID,
                           s.TotalSold AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN DoanhMuc_De dd ON s.MaDeBanh = RTRIM(dd.MaDeBanh)
                    INNER JOIN QuyDinh_Bot qb ON s.SizeID = RTRIM(qb.SizeID) AND RTRIM(dd.LoaiCotBanh) = RTRIM(qb.LoaiCotBanh)
                    WHERE @NguyenLieuID = 26

                    UNION ALL

                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(qv.SoLuongVien * s.TotalSold AS DECIMAL(18,4)) AS TotalBomAmount,
                           CAST(qv.SoLuongVien AS DECIMAL(18,3)) AS UnitBomAmount,
                           ISNULL(qv.DonViID, 2) AS BomDonViID,
                           s.TotalSold AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN QuyDinh_Vien qv ON s.MaDeBanh = RTRIM(qv.MaDeBanh) AND s.SizeID = RTRIM(qv.SizeID)
                    WHERE qv.NguyenLieuID = @NguyenLieuID
                ),
                PizzaSoldTotals AS (
                    SELECT MaHangHoa, SizeID, SUM(TotalSold) AS TotalPizzaSold
                    FROM SoldByPizza
                    GROUP BY MaHangHoa, SizeID
                )
                SELECT bm.MaHangHoa,
                       ISNULL(hh.TenHangHoa, bm.MaHangHoa) AS TenHangHoa,
                       bm.SizeID,
                       SUM(bm.TotalBomAmount) AS TotalRawConsumed,
                       AVG(bm.UnitBomAmount) AS AvgBomPerUnit,
                       MAX(bm.BomDonViID) AS BomDonViID,
                       ISNULL(dvt.TenDonVi, 'g') AS TenDonViBom,
                       MAX(pst.TotalPizzaSold) AS SoLuongDaBan
                FROM BomMatches bm
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

    /// <summary>
    /// Đọc dữ liệu dự báo & đánh giá trực tiếp từ bảng SQL DuBao và DanhGiaDuBao.
    /// Chỉ khi kỳ dự báo mới chưa có bản ghi trong CSDL (ví dụ phát sinh giao dịch kỳ mới),
    /// hệ thống mới tính toán Holt-Winters từ bảng DuLieuTieuHao và lưu vào DuBao / DanhGiaDuBao.
    /// </summary>
    public async Task<List<KetQuaDuBaoItem>> RunHoltWintersForecastAsync(DateTime currentKy, int? filterNguyenLieuId = null)
    {
        var results = new List<KetQuaDuBaoItem>();
        var kyEnd = new DateTime(currentKy.Year, currentKy.Month, 1);
        var kyForecast = kyEnd.AddMonths(1);

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            int additiveId = 1;
            int multiplicativeId = 2;
            string additiveName = "Holt-Winters cộng tính";
            string multiplicativeName = "Holt-Winters nhân tính";

            using (var ppCmd = new SqlCommand("SELECT PhuongPhapID, TenPhuongPhap, LoaiMuaVu FROM PhuongPhapDuBao WHERE TrangThai = 1", conn))
            {
                using var r = await ppCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = r.GetInt32(0);
                    var ten = r.IsDBNull(1) ? "" : r.GetString(1);
                    var loai = r.IsDBNull(2) ? "" : r.GetString(2);
                    if (loai.Contains("Add", StringComparison.OrdinalIgnoreCase))
                    {
                        additiveId = id;
                        if (!string.IsNullOrWhiteSpace(ten)) additiveName = ten;
                    }
                    else if (loai.Contains("Multi", StringComparison.OrdinalIgnoreCase))
                    {
                        multiplicativeId = id;
                        if (!string.IsNullOrWhiteSpace(ten)) multiplicativeName = ten;
                    }
                }
            }

            // 1) Đọc trực tiếp kết quả dự báo đã có trong bảng DuBao & DanhGiaDuBao của SQL Server
            var existingSql = @"
                SELECT nl.NguyenLieuID,
                       nl.TenNguyenLieu,
                       ISNULL(dvt.TenDonVi, 'kg') AS TenDonVi,
                       ISNULL(tk.SoLuongTon, 0) AS SoLuongTon,
                       ISNULL(thCur.SoLuongTieuHao, 0) AS TieuHaoHienTai,
                       dbAdd.SoLuongDuBao AS DuBaoAdd,
                       dbAdd.LaMoHinhDuocChon AS ChonAdd,
                       ISNULL(dgAdd.MAE, 0) AS MaeAdd,
                       ISNULL(dgAdd.RMSE, 0) AS RmseAdd,
                       ISNULL(dgAdd.MAPE, 0) AS MapeAdd,
                       dbMul.SoLuongDuBao AS DuBaoMul,
                       ISNULL(dgMul.MAE, 0) AS MaeMul,
                       ISNULL(dgMul.RMSE, 0) AS RmseMul,
                       ISNULL(dgMul.MAPE, 0) AS MapeMul
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                LEFT JOIN DuLieuTieuHao thCur ON nl.NguyenLieuID = thCur.NguyenLieuID AND thCur.KyTieuHao = @KyEnd
                INNER JOIN DuBao dbAdd ON nl.NguyenLieuID = dbAdd.NguyenLieuID AND dbAdd.KyDuBao = @KyDuBao AND dbAdd.PhuongPhapID = @AddId AND dbAdd.TrangThai = 1
                INNER JOIN DuBao dbMul ON nl.NguyenLieuID = dbMul.NguyenLieuID AND dbMul.KyDuBao = @KyDuBao AND dbMul.PhuongPhapID = @MulId AND dbMul.TrangThai = 1
                LEFT JOIN DanhGiaDuBao dgAdd ON nl.NguyenLieuID = dgAdd.NguyenLieuID AND dgAdd.DenKyTest = @KyEnd AND dgAdd.PhuongPhapID = @AddId
                LEFT JOIN DanhGiaDuBao dgMul ON nl.NguyenLieuID = dgMul.NguyenLieuID AND dgMul.DenKyTest = @KyEnd AND dgMul.PhuongPhapID = @MulId
                WHERE ISNULL(nl.TrangThai, 1) = 1";

            if (filterNguyenLieuId.HasValue)
                existingSql += " AND nl.NguyenLieuID = @NlId";
            existingSql += " ORDER BY nl.NguyenLieuID";

            int expectedCount = 0;
            var countSql = "SELECT COUNT(*) FROM NguyenLieu WHERE ISNULL(TrangThai, 1) = 1" +
                           (filterNguyenLieuId.HasValue ? " AND NguyenLieuID = @NlId" : "");
            using (var cntCmd = new SqlCommand(countSql, conn))
            {
                if (filterNguyenLieuId.HasValue)
                    cntCmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);
                expectedCount = Convert.ToInt32(await cntCmd.ExecuteScalarAsync() ?? 0);
            }

            using (var existCmd = new SqlCommand(existingSql, conn))
            {
                existCmd.Parameters.AddWithValue("@KyEnd", kyEnd);
                existCmd.Parameters.AddWithValue("@KyDuBao", kyForecast);
                existCmd.Parameters.AddWithValue("@AddId", additiveId);
                existCmd.Parameters.AddWithValue("@MulId", multiplicativeId);
                if (filterNguyenLieuId.HasValue)
                    existCmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);

                using var r = await existCmd.ExecuteReaderAsync();
                int sttRead = 1;
                while (await r.ReadAsync())
                {
                    int id = r.GetInt32(0);
                    string ten = r.GetString(1);
                    string dvt = r.GetString(2);
                    decimal tonKho = r.GetDecimal(3);
                    decimal curVal = r.GetDecimal(4);
                    decimal dbAdd = r.GetDecimal(5);
                    bool chooseAdd = r.GetBoolean(6);
                    decimal maeAdd = r.GetDecimal(7);
                    decimal rmseAdd = r.GetDecimal(8);
                    decimal mapeAdd = r.GetDecimal(9);
                    decimal dbMul = r.GetDecimal(10);
                    decimal maeMul = r.GetDecimal(11);
                    decimal rmseMul = r.GetDecimal(12);
                    decimal mapeMul = r.GetDecimal(13);

                    results.Add(new KetQuaDuBaoItem
                    {
                        STT = sttRead++,
                        NguyenLieuID = id,
                        MaNguyenLieu = $"NL{id:D3}",
                        TenNguyenLieu = ten,
                        DonViKho = dvt,
                        TieuHaoKyHienTai = Math.Round(curVal, 2),
                        DuBaoCongTinh = Math.Round(dbAdd, 2),
                        MaeCongTinh = Math.Round(maeAdd, 2),
                        RmseCongTinh = Math.Round(rmseAdd, 2),
                        MapeCongTinh = Math.Round(mapeAdd, 2),
                        DuBaoNhanTinh = Math.Round(dbMul, 2),
                        MaeNhanTinh = Math.Round(maeMul, 2),
                        RmseNhanTinh = Math.Round(rmseMul, 2),
                        MapeNhanTinh = Math.Round(mapeMul, 2),
                        PhuongPhapDuocChon = chooseAdd ? additiveName : multiplicativeName,
                        SoLuongDuBaoChon = Math.Round(chooseAdd ? dbAdd : dbMul, 2),
                        MaeChon = Math.Round(chooseAdd ? maeAdd : maeMul, 2),
                        RmseChon = Math.Round(chooseAdd ? rmseAdd : rmseMul, 2),
                        MapeChon = Math.Round(chooseAdd ? mapeAdd : mapeMul, 2),
                        TonKhoHienTai = Math.Round(tonKho, 2)
                    });
                }
            }

            if (results.Count > 0 && results.Count >= expectedCount)
            {
                return results;
            }

            results.Clear();

            // 2) Nếu kỳ mới chưa có bản ghi dự báo trong DuBao, tính Holt-Winters từ DuLieuTieuHao và lưu vào SQL
            var ingredients = new List<(int Id, string Ma, string Ten, string DonVi, decimal TonKho)>();
            var nlSql = @"
                SELECT nl.NguyenLieuID,
                       nl.TenNguyenLieu,
                       ISNULL(dvt.TenDonVi, 'kg') AS TenDonVi,
                       ISNULL(tk.SoLuongTon, 0) AS SoLuongTon
                FROM NguyenLieu nl
                LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID
                LEFT JOIN TonKho tk ON nl.NguyenLieuID = tk.NguyenLieuID
                WHERE ISNULL(nl.TrangThai, 1) = 1";
            if (filterNguyenLieuId.HasValue)
                nlSql += " AND nl.NguyenLieuID = @NlId";
            nlSql += " ORDER BY nl.NguyenLieuID";

            using (var nlCmd = new SqlCommand(nlSql, conn))
            {
                if (filterNguyenLieuId.HasValue)
                    nlCmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);
                using var r = await nlCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = r.GetInt32(0);
                    ingredients.Add((id, $"NL{id:D3}", r.GetString(1), r.GetString(2), r.GetDecimal(3)));
                }
            }

            var historyMap = new Dictionary<int, List<(DateTime Ky, double Value)>>();
            const string thSql = @"
                SELECT NguyenLieuID, KyTieuHao, SoLuongTieuHao
                FROM DuLieuTieuHao
                WHERE KyTieuHao <= @KyEnd
                ORDER BY NguyenLieuID, KyTieuHao ASC";
            using (var thCmd = new SqlCommand(thSql, conn))
            {
                thCmd.Parameters.AddWithValue("@KyEnd", kyEnd);
                using var r = await thCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = r.GetInt32(0);
                    var ky = r.GetDateTime(1);
                    var val = (double)r.GetDecimal(2);
                    if (!historyMap.TryGetValue(id, out var list))
                    {
                        list = new List<(DateTime, double)>();
                        historyMap[id] = list;
                    }
                    list.Add((ky, val));
                }
            }

            using var transaction = conn.BeginTransaction();
            try
            {
                var delDuBaoSql = filterNguyenLieuId.HasValue
                    ? "DELETE FROM DuBao WHERE KyDuBao = @KyDuBao AND NguyenLieuID = @NlId"
                    : "DELETE FROM DuBao WHERE KyDuBao = @KyDuBao";
                using (var delCmd = new SqlCommand(delDuBaoSql, conn, transaction))
                {
                    delCmd.Parameters.AddWithValue("@KyDuBao", kyForecast);
                    if (filterNguyenLieuId.HasValue)
                        delCmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);
                    await delCmd.ExecuteNonQueryAsync();
                }

                var delDanhGiaSql = filterNguyenLieuId.HasValue
                    ? "DELETE FROM DanhGiaDuBao WHERE DenKyTest = @KyEnd AND NguyenLieuID = @NlId"
                    : "DELETE FROM DanhGiaDuBao WHERE DenKyTest = @KyEnd";
                using (var delDgCmd = new SqlCommand(delDanhGiaSql, conn, transaction))
                {
                    delDgCmd.Parameters.AddWithValue("@KyEnd", kyEnd);
                    if (filterNguyenLieuId.HasValue)
                        delDgCmd.Parameters.AddWithValue("@NlId", filterNguyenLieuId.Value);
                    await delDgCmd.ExecuteNonQueryAsync();
                }

                int stt = 1;
                foreach (var nl in ingredients)
                {
                    if (!historyMap.TryGetValue(nl.Id, out var series) || series.Count == 0)
                        continue;

                    var values = series.Select(s => s.Value).ToArray();
                    var dates = series.Select(s => s.Ky).ToArray();
                    double curVal = values[^1];

                    int n = values.Length;
                    int seasonLength = 12;

                    var addEval = FitAndEvaluateHoltWinters(values, seasonLength, isMultiplicative: false);
                    var mulEval = FitAndEvaluateHoltWinters(values, seasonLength, isMultiplicative: true);

                    DateTime tuKyTrain = dates[0];
                    DateTime denKyTrain = dates[Math.Clamp(addEval.TrainEndIdx, 0, n - 1)];
                    DateTime tuKyTest = dates[Math.Clamp(addEval.TestStartIdx, 0, n - 1)];
                    DateTime denKyTest = dates[^1];

                    bool chooseAdditive = addEval.Mape <= mulEval.Mape;

                    const string insertDgSql = @"
                        INSERT INTO DanhGiaDuBao (NguyenLieuID, PhuongPhapID, TuKyTrain, DenKyTrain, TuKyTest, DenKyTest, MAE, RMSE, MAPE, NgayDanhGia)
                        VALUES (@NlId, @PpId, @TuTrain, @DenTrain, @TuTest, @DenTest, @Mae, @Rmse, @Mape, GETDATE())";

                    using (var dgAdd = new SqlCommand(insertDgSql, conn, transaction))
                    {
                        dgAdd.Parameters.AddWithValue("@NlId", nl.Id);
                        dgAdd.Parameters.AddWithValue("@PpId", additiveId);
                        dgAdd.Parameters.AddWithValue("@TuTrain", tuKyTrain);
                        dgAdd.Parameters.AddWithValue("@DenTrain", denKyTrain);
                        dgAdd.Parameters.AddWithValue("@TuTest", tuKyTest);
                        dgAdd.Parameters.AddWithValue("@DenTest", denKyTest);
                        dgAdd.Parameters.AddWithValue("@Mae", Math.Round((decimal)addEval.Mae, 4));
                        dgAdd.Parameters.AddWithValue("@Rmse", Math.Round((decimal)addEval.Rmse, 4));
                        dgAdd.Parameters.AddWithValue("@Mape", Math.Round((decimal)addEval.Mape, 4));
                        await dgAdd.ExecuteNonQueryAsync();
                    }

                    using (var dgMul = new SqlCommand(insertDgSql, conn, transaction))
                    {
                        dgMul.Parameters.AddWithValue("@NlId", nl.Id);
                        dgMul.Parameters.AddWithValue("@PpId", multiplicativeId);
                        dgMul.Parameters.AddWithValue("@TuTrain", tuKyTrain);
                        dgMul.Parameters.AddWithValue("@DenTrain", denKyTrain);
                        dgMul.Parameters.AddWithValue("@TuTest", tuKyTest);
                        dgMul.Parameters.AddWithValue("@DenTest", denKyTest);
                        dgMul.Parameters.AddWithValue("@Mae", Math.Round((decimal)mulEval.Mae, 4));
                        dgMul.Parameters.AddWithValue("@Rmse", Math.Round((decimal)mulEval.Rmse, 4));
                        dgMul.Parameters.AddWithValue("@Mape", Math.Round((decimal)mulEval.Mape, 4));
                        await dgMul.ExecuteNonQueryAsync();
                    }

                    const string insertDbSql = @"
                        INSERT INTO DuBao (NguyenLieuID, PhuongPhapID, KyDuBao, SoLuongDuBao, LaMoHinhDuocChon, NgayDuBao, TrangThai)
                        VALUES (@NlId, @PpId, @KyDuBao, @SoLuong, @LaChon, GETDATE(), 1)";

                    using (var dbAdd = new SqlCommand(insertDbSql, conn, transaction))
                    {
                        dbAdd.Parameters.AddWithValue("@NlId", nl.Id);
                        dbAdd.Parameters.AddWithValue("@PpId", additiveId);
                        dbAdd.Parameters.AddWithValue("@KyDuBao", kyForecast);
                        dbAdd.Parameters.AddWithValue("@SoLuong", Math.Round((decimal)addEval.NextForecast, 3));
                        dbAdd.Parameters.AddWithValue("@LaChon", chooseAdditive);
                        await dbAdd.ExecuteNonQueryAsync();
                    }

                    using (var dbMul = new SqlCommand(insertDbSql, conn, transaction))
                    {
                        dbMul.Parameters.AddWithValue("@NlId", nl.Id);
                        dbMul.Parameters.AddWithValue("@PpId", multiplicativeId);
                        dbMul.Parameters.AddWithValue("@KyDuBao", kyForecast);
                        dbMul.Parameters.AddWithValue("@SoLuong", Math.Round((decimal)mulEval.NextForecast, 3));
                        dbMul.Parameters.AddWithValue("@LaChon", !chooseAdditive);
                        await dbMul.ExecuteNonQueryAsync();
                    }

                    var chosenForecast = chooseAdditive ? addEval.NextForecast : mulEval.NextForecast;
                    var chosenMape = chooseAdditive ? addEval.Mape : mulEval.Mape;
                    var chosenRmse = chooseAdditive ? addEval.Rmse : mulEval.Rmse;
                    var chosenMae = chooseAdditive ? addEval.Mae : mulEval.Mae;

                    results.Add(new KetQuaDuBaoItem
                    {
                        STT = stt++,
                        NguyenLieuID = nl.Id,
                        MaNguyenLieu = nl.Ma,
                        TenNguyenLieu = nl.Ten,
                        DonViKho = nl.DonVi,
                        TieuHaoKyHienTai = Math.Round((decimal)curVal, 2),
                        DuBaoCongTinh = Math.Round((decimal)addEval.NextForecast, 2),
                        MapeCongTinh = Math.Round((decimal)addEval.Mape, 2),
                        RmseCongTinh = Math.Round((decimal)addEval.Rmse, 2),
                        MaeCongTinh = Math.Round((decimal)addEval.Mae, 2),
                        DuBaoNhanTinh = Math.Round((decimal)mulEval.NextForecast, 2),
                        MapeNhanTinh = Math.Round((decimal)mulEval.Mape, 2),
                        RmseNhanTinh = Math.Round((decimal)mulEval.Rmse, 2),
                        MaeNhanTinh = Math.Round((decimal)mulEval.Mae, 2),
                        PhuongPhapDuocChon = chooseAdditive ? additiveName : multiplicativeName,
                        SoLuongDuBaoChon = Math.Round((decimal)chosenForecast, 2),
                        MapeChon = Math.Round((decimal)chosenMape, 2),
                        RmseChon = Math.Round((decimal)chosenRmse, 2),
                        MaeChon = Math.Round((decimal)chosenMae, 2),
                        TonKhoHienTai = Math.Round(nl.TonKho, 2)
                    });
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch { }

        return results;
    }

    private static (double NextForecast, double Mae, double Rmse, double Mape, int TrainEndIdx, int TestStartIdx) FitAndEvaluateHoltWinters(
        double[] y, int L, bool isMultiplicative)
    {
        int n = y.Length;
        double[] alphas = { 0.2, 0.35, 0.5, 0.65 };
        double[] betas = { 0.05, 0.15, 0.25 };
        double[] gammas = { 0.2, 0.35, 0.5 };

        if (n < L)
        {
            double bestShortMape = double.MaxValue;
            double bestShortMae = 0;
            double bestShortRmse = 0;
            double bestShortForecast = y.Length > 0 ? y[^1] : 0.1;

            foreach (var alpha in alphas)
            {
                foreach (var beta in betas)
                {
                    var (f, mae, rmse, mape) = RunShortHoltTrend(y, alpha, beta, isMultiplicative);
                    if (mape < bestShortMape)
                    {
                        bestShortMape = mape;
                        bestShortMae = mae;
                        bestShortRmse = rmse;
                        bestShortForecast = f;
                    }
                }
            }

            int evalStart = n > 1 ? Math.Max(1, n - Math.Max(1, n / 3)) : 0;
            int trEnd = Math.Max(0, evalStart - 1);
            int teStart = Math.Min(Math.Max(0, n - 1), evalStart);
            return (Math.Max(0.1, bestShortForecast), bestShortMae, bestShortRmse, bestShortMape, trEnd, teStart);
        }

        int testCount = Math.Min(6, Math.Max(2, n / 5));
        int trainCount = n > L ? Math.Max(L, n - testCount) : L - 2;

        double bestMape = double.MaxValue;
        double bestMae = 0;
        double bestRmse = 0;
        double bestForecast = y[^1];

        foreach (var alpha in alphas)
        {
            foreach (var beta in betas)
            {
                foreach (var gamma in gammas)
                {
                    var (forecast, mae, rmse, mape) = RunSingleHoltWinters(y, L, trainCount, alpha, beta, gamma, isMultiplicative);
                    if (mape < bestMape)
                    {
                        bestMape = mape;
                        bestMae = mae;
                        bestRmse = rmse;
                        bestForecast = forecast;
                    }
                }
            }
        }

        int trainEndIdx = Math.Clamp(trainCount - 1, 0, n - 1);
        int testStartIdx = Math.Clamp(trainCount, 0, n - 1);
        return (Math.Max(0.1, bestForecast), bestMae, bestRmse, bestMape, trainEndIdx, testStartIdx);
    }

    private static (double NextForecast, double Mae, double Rmse, double Mape) RunShortHoltTrend(
        double[] y, double alpha, double beta, bool isMultiplicative)
    {
        int n = y.Length;
        if (n == 0) return (0.1, 0, 0, 0);
        if (n == 1)
        {
            double f1 = y[0] * (isMultiplicative ? 1.02 : 1.0);
            double err = Math.Abs(y[0] - f1);
            double pct = y[0] > 0.001 ? (err / y[0]) * 100.0 : 0.0;
            return (Math.Max(0.1, f1), err, err, pct);
        }

        double level = y[0] > 0 ? y[0] : 1.0;
        double trend = isMultiplicative
            ? Math.Clamp(y[0] > 0.001 ? y[1] / y[0] : 1.0, 0.85, 1.15)
            : (y[1] - y[0]);

        int evalStart = Math.Max(1, n - Math.Max(1, n / 3));
        double sumAbs = 0, sumSq = 0, sumPct = 0;
        int samples = 0;

        for (int t = 1; t < n; t++)
        {
            double yHat = isMultiplicative ? (level * trend) : (level + trend);
            if (t >= evalStart)
            {
                double actual = y[t];
                double err = Math.Abs(actual - yHat);
                sumAbs += err;
                sumSq += err * err;
                if (actual > 0.001) sumPct += (err / actual) * 100.0;
                samples++;
            }

            double prevL = level;
            if (isMultiplicative)
            {
                double safeT = Math.Abs(trend) > 0.001 ? trend : 1.0;
                level = alpha * y[t] + (1.0 - alpha) * (prevL * safeT);
                double safePrev = Math.Abs(prevL) > 0.001 ? prevL : 1.0;
                trend = Math.Clamp(beta * (level / safePrev) + (1.0 - beta) * safeT, 0.85, 1.15);
            }
            else
            {
                level = alpha * y[t] + (1.0 - alpha) * (prevL + trend);
                trend = beta * (level - prevL) + (1.0 - beta) * trend;
            }
        }

        double nextF = isMultiplicative ? (level * trend) : (level + trend);
        if (samples == 0) samples = 1;
        return (nextF, sumAbs / samples, Math.Sqrt(sumSq / samples), sumPct / samples);
    }

    private static (double NextForecast, double Mae, double Rmse, double Mape) RunSingleHoltWinters(
        double[] y, int L, int trainCount, double alpha, double beta, double gamma, bool isMultiplicative)
    {
        int n = y.Length;
        double[] seasonals = new double[n + L + 4];

        double level;
        double trend;
        if (n >= 2 * L)
        {
            double avgFirstCycle = 0;
            double avgSecondCycle = 0;
            for (int i = 0; i < L; i++)
            {
                avgFirstCycle += y[i];
                avgSecondCycle += y[i + L];
            }
            avgFirstCycle /= L;
            avgSecondCycle /= L;
            level = avgFirstCycle > 0 ? avgFirstCycle : 1.0;
            trend = (avgSecondCycle - avgFirstCycle) / (L * L);
        }
        else
        {
            double avgFirstCycle = 0;
            for (int i = 0; i < L; i++) avgFirstCycle += y[i];
            avgFirstCycle /= L;
            level = avgFirstCycle > 0 ? avgFirstCycle : 1.0;
            trend = (y[^1] - y[0]) / Math.Max(1.0, (n - 1) * 2.0);
        }

        for (int i = 0; i < L; i++)
        {
            if (isMultiplicative)
                seasonals[i] = level > 0.001 ? Math.Max(0.1, y[i] / level) : 1.0;
            else
                seasonals[i] = y[i] - level;
        }

        double sumAbsErr = 0;
        double sumSqErr = 0;
        double sumPctErr = 0;
        int testSamples = 0;

        for (int t = L; t < n; t++)
        {
            double sPrev = seasonals[t - L];
            double yHat = isMultiplicative
                ? (level + trend) * sPrev
                : (level + trend) + sPrev;

            if (t >= trainCount)
            {
                double actual = y[t];
                double err = Math.Abs(actual - yHat);
                sumAbsErr += err;
                sumSqErr += err * err;
                if (actual > 0.001)
                    sumPctErr += (err / actual) * 100.0;
                testSamples++;
            }

            double prevLevel = level;
            if (isMultiplicative)
            {
                double safeS = Math.Abs(sPrev) > 0.001 ? sPrev : 1.0;
                level = alpha * (y[t] / safeS) + (1.0 - alpha) * (prevLevel + trend);
                trend = beta * (level - prevLevel) + (1.0 - beta) * trend;
                double safeL = Math.Abs(level) > 0.001 ? level : 1.0;
                seasonals[t] = gamma * (y[t] / safeL) + (1.0 - gamma) * sPrev;
            }
            else
            {
                level = alpha * (y[t] - sPrev) + (1.0 - alpha) * (prevLevel + trend);
                trend = beta * (level - prevLevel) + (1.0 - beta) * trend;
                seasonals[t] = gamma * (y[t] - level) + (1.0 - gamma) * sPrev;
            }
        }

        if (n == L)
        {
            double lvl = y[0];
            double tr = (y[^1] - y[0]) / Math.Max(1.0, L - 1);
            int evalStart = Math.Max(2, L - 3);
            for (int t = evalStart; t < L; t++)
            {
                double pred = lvl + tr;
                double actual = y[t];
                double err = Math.Abs(actual - pred);
                sumAbsErr += err;
                sumSqErr += err * err;
                if (actual > 0.001) sumPctErr += (err / actual) * 100.0;
                testSamples++;
                double prevL = lvl;
                lvl = alpha * actual + (1.0 - alpha) * (prevL + tr);
                tr = beta * (lvl - prevL) + (1.0 - beta) * tr;
            }
        }

        double nextSeasonal = seasonals[n - L];
        double nextForecast = isMultiplicative
            ? (level + trend) * nextSeasonal
            : (level + trend) + nextSeasonal;

        if (testSamples == 0) testSamples = 1;
        double mae = sumAbsErr / testSamples;
        double rmse = Math.Sqrt(sumSqErr / testSamples);
        double mape = sumPctErr / testSamples;

        return (nextForecast, mae, rmse, mape);
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
