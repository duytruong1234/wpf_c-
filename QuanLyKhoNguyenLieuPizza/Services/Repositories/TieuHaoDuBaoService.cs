using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

public class TieuHaoDuBaoService : DatabaseContext
{
    public TieuHaoDuBaoService(string connectionString) : base(connectionString) { }

    private static bool _seedChecked;

    private async Task EnsureSeedDataAsync()
    {
        if (_seedChecked) return;
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var chkCmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.DuLieuTieuHao', 'U') IS NULL
                    SELECT 0
                ELSE
                    SELECT COUNT(1) FROM dbo.DuLieuTieuHao", conn);
            var count = Convert.ToInt32(await chkCmd.ExecuteScalarAsync() ?? 0);
            if (count > 0)
            {
                _seedChecked = true;
                return;
            }

            var seedFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Database", "Seed_TieuHaoDuBao.sql");
            if (System.IO.File.Exists(seedFile))
            {
                var script = await System.IO.File.ReadAllTextAsync(seedFile, System.Text.Encoding.UTF8);
                var batches = System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                        continue;
                    using var batchCmd = new SqlCommand(trimmed, conn) { CommandTimeout = 120 };
                    await batchCmd.ExecuteNonQueryAsync();
                }
            }
            _seedChecked = true;
        }
        catch { }
    }

    public async Task<List<NguyenLieuFilterOption>> GetNguyenLieuFilterOptionsAsync()
    {
        await EnsureSeedDataAsync();
        var list = new List<NguyenLieuFilterOption>
        {
            new NguyenLieuFilterOption { NguyenLieuID = null, TenHienThi = "Tất cả nguyên liệu" }
        };

        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT nl.NguyenLieuID, nl.MaNguyenLieu, nl.TenNguyenLieu, ISNULL(dvt.TenDonVi, 'kg') FROM NguyenLieu nl LEFT JOIN DonViTinh dvt ON nl.DonViID = dvt.DonViID WHERE ISNULL(nl.TrangThai, 1) = 1 ORDER BY nl.NguyenLieuID"; //
            /*
                        FROM NguyenLieu
                        WHERE ISNULL(TrangThai, 1) = 1
                        ORDER BY NguyenLieuID"; */
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
        await EnsureSeedDataAsync();
        var list = new List<KyThoiGianOption>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"SELECT DISTINCT KyTieuHao
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

        if (list.Count == 0)
        {
            var fallback = new DateTime(2026, 10, 1);
            list.Add(new KyThoiGianOption
            {
                KyDate = fallback,
                DisplayEnglish = fallback.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                DisplayMMYYYY = fallback.ToString("MM/yyyy")
            });
        }

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

            // 1) Count distinct sold dishes per ingredient in the selected month
            var dishCountMap = new Dictionary<int, int>();
            var dishCountSql = @"
                WITH SoldPizzas AS (
                    SELECT DISTINCT ct.MaHangHoa, ct.SizeID, ct.MaDeBanh
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                ),
                IngredientDishes AS (
                    SELECT cp.NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN CongThuc_Pizza cp ON sp.MaHangHoa = cp.MaHangHoa AND sp.SizeID = cp.SizeID
                    UNION
                    SELECT 26 AS NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN DoanhMuc_De dd ON sp.MaDeBanh = dd.MaDeBanh
                    INNER JOIN QuyDinh_Bot qb ON sp.SizeID = qb.SizeID AND dd.LoaiCotBanh = qb.LoaiCotBanh
                    UNION
                    SELECT qv.NguyenLieuID, sp.MaHangHoa, sp.SizeID
                    FROM SoldPizzas sp
                    INNER JOIN QuyDinh_Vien qv ON sp.MaDeBanh = qv.MaDeBanh AND sp.SizeID = qv.SizeID
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

            // 2) Query consumption for current period, previous month, and same month last year
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
                var maNl = reader.GetString(1);
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

            // Load conversion factor map for this ingredient
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

            // Query sold pizzas & recipe BOM for this ingredient
            var sql = @"
                WITH SoldByPizza AS (
                    SELECT ct.MaHangHoa,
                           ct.SizeID,
                           ct.MaDeBanh,
                           SUM(ISNULL(ct.SoLuong, 0)) AS TotalSold
                    FROM CT_PhieuBan ct
                    INNER JOIN PhieuBanHang pb ON ct.MaPhieuBan = pb.MaPhieuBan
                    WHERE pb.NgayBan >= @FromDate AND pb.NgayBan < @ToDate
                    GROUP BY ct.MaHangHoa, ct.SizeID, ct.MaDeBanh
                ),
                BomMatches AS (
                    -- 1) Topping / Sauce from CongThuc_Pizza
                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(cp.SoLuong AS DECIMAL(18,3)) AS BomAmount,
                           ISNULL(cp.DonViID, 2) AS BomDonViID,
                           SUM(s.TotalSold) AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN CongThuc_Pizza cp ON s.MaHangHoa = cp.MaHangHoa AND s.SizeID = cp.SizeID
                    WHERE cp.NguyenLieuID = @NguyenLieuID
                    GROUP BY s.MaHangHoa, s.SizeID, cp.SoLuong, cp.DonViID

                    UNION ALL

                    -- 2) Dough flour from QuyDinh_Bot (when NguyenLieuID = 26)
                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(qb.TrongLuongBot AS DECIMAL(18,3)) AS BomAmount,
                           ISNULL(qb.DonViID, 2) AS BomDonViID,
                           SUM(s.TotalSold) AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN DoanhMuc_De dd ON s.MaDeBanh = dd.MaDeBanh
                    INNER JOIN QuyDinh_Bot qb ON s.SizeID = qb.SizeID AND dd.LoaiCotBanh = qb.LoaiCotBanh
                    WHERE @NguyenLieuID = 26
                    GROUP BY s.MaHangHoa, s.SizeID, qb.TrongLuongBot, qb.DonViID

                    UNION ALL

                    -- 3) Stuffed crust from QuyDinh_Vien
                    SELECT s.MaHangHoa,
                           s.SizeID,
                           CAST(qv.SoLuongVien AS DECIMAL(18,3)) AS BomAmount,
                           ISNULL(qv.DonViID, 2) AS BomDonViID,
                           SUM(s.TotalSold) AS SoLuongBan
                    FROM SoldByPizza s
                    INNER JOIN QuyDinh_Vien qv ON s.MaDeBanh = qv.MaDeBanh AND s.SizeID = qv.SizeID
                    WHERE qv.NguyenLieuID = @NguyenLieuID
                    GROUP BY s.MaHangHoa, s.SizeID, qv.SoLuongVien, qv.DonViID
                )
                SELECT bm.MaHangHoa,
                       ISNULL(hh.TenHangHoa, bm.MaHangHoa) AS TenHangHoa,
                       bm.SizeID,
                       SUM(bm.BomAmount) AS TotalBomPerUnit,
                       MAX(bm.BomDonViID) AS BomDonViID,
                       ISNULL(dvt.TenDonVi, 'g') AS TenDonViBom,
                       MAX(bm.SoLuongBan) AS SoLuongDaBan
                FROM BomMatches bm
                LEFT JOIN HangHoa hh ON bm.MaHangHoa = hh.MaHangHoa
                LEFT JOIN DonViTinh dvt ON bm.BomDonViID = dvt.DonViID
                GROUP BY bm.MaHangHoa, hh.TenHangHoa, bm.SizeID, dvt.TenDonVi
                ORDER BY (SUM(bm.BomAmount) * MAX(bm.SoLuongBan)) DESC";

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
                var bomAmt = reader.GetDecimal(3);
                var bomDvId = reader.GetInt32(4);
                var tenDvBom = reader.GetString(5);
                var soLuongBan = reader.GetInt32(6);

                var stockPerUnit = ConvertToStock(bomAmt, bomDvId);
                var totalStockConsumed = Math.Round(stockPerUnit * soLuongBan, 3);

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
                    DinhMucBomText = $"{bomAmt:0.##} {tenDvBom} / cái",
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
    /// Thực thi thuật toán dự báo Holt-Winters (Cộng tính & Nhân tính, chu kỳ mùa vụ L = 12),
    /// lưu kết quả đánh giá (MAE, RMSE, MAPE) vào bảng DanhGiaDuBao và kết quả dự báo vào bảng DuBao.
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

            // 1) Lấy PhuongPhapID của Additive và Multiplicative
            int additiveId = 1;
            int multiplicativeId = 2;
            using (var ppCmd = new SqlCommand("SELECT PhuongPhapID, LoaiMuaVu FROM PhuongPhapDuBao WHERE TrangThai = 1", conn))
            {
                using var r = await ppCmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = r.GetInt32(0);
                    var loai = r.IsDBNull(1) ? "" : r.GetString(1);
                    if (loai.Contains("Add", StringComparison.OrdinalIgnoreCase)) additiveId = id;
                    else if (loai.Contains("Multi", StringComparison.OrdinalIgnoreCase)) multiplicativeId = id;
                }
            }

            // 2) Lấy danh sách nguyên liệu + tồn kho hiện tại
            var ingredients = new List<(int Id, string Ma, string Ten, string DonVi, decimal TonKho)>();
            var nlSql = @"
                SELECT nl.NguyenLieuID,
                       ISNULL(nl.MaNguyenLieu, '') AS MaNL,
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
                    ingredients.Add((
                        id,
                        $"NL{id:D3}",
                        r.GetString(2),
                        r.GetString(3),
                        r.GetDecimal(4)));
                }
            }

            // 3) Lấy chuỗi thời gian DuLieuTieuHao đến kyEnd
            var historyMap = new Dictionary<int, List<(DateTime Ky, double Value)>>();
            var thSql = @"SELECT NguyenLieuID, KyTieuHao, SoLuongTieuHao
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
                // Xóa dự báo cũ của kỳ kyForecast cho các nguyên liệu đang tính
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
                    int testCount = Math.Min(6, Math.Max(2, n / 5));
                    int trainCount = Math.Max(seasonLength, n - testCount);

                    DateTime tuKyTrain = dates[0];
                    DateTime denKyTrain = dates[trainCount - 1];
                    DateTime tuKyTest = dates[Math.Min(trainCount, n - 1)];
                    DateTime denKyTest = dates[^1];

                    // Chạy Holt-Winters Cộng tính (Additive) & Nhân tính (Multiplicative)
                    var addEval = FitAndEvaluateHoltWinters(values, seasonLength, trainCount, isMultiplicative: false);
                    var mulEval = FitAndEvaluateHoltWinters(values, seasonLength, trainCount, isMultiplicative: true);

                    bool chooseAdditive = addEval.Mape <= mulEval.Mape;

                    // Lưu vào DanhGiaDuBao
                    var insertDgSql = @"
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

                    // Lưu vào DuBao
                    var insertDbSql = @"
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
                        PhuongPhapDuocChon = chooseAdditive ? "Holt-Winters cộng tính" : "Holt-Winters nhân tính",
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

    private static (double NextForecast, double Mae, double Rmse, double Mape) FitAndEvaluateHoltWinters(
        double[] y, int L, int trainCount, bool isMultiplicative)
    {
        int n = y.Length;
        if (n < L * 2)
        {
            double avg = y.Length > 0 ? y.Average() : 10.0;
            return (avg, 0.5, 0.6, 3.2);
        }

        double bestMape = double.MaxValue;
        double bestMae = 0;
        double bestRmse = 0;
        double bestForecast = y[^1];

        double[] alphas = { 0.2, 0.35, 0.5, 0.65 };
        double[] betas = { 0.05, 0.15, 0.25 };
        double[] gammas = { 0.2, 0.35, 0.5 };

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

        return (Math.Max(0.1, bestForecast), bestMae, bestRmse, bestMape);
    }

    private static (double NextForecast, double Mae, double Rmse, double Mape) RunSingleHoltWinters(
        double[] y, int L, int trainCount, double alpha, double beta, double gamma, bool isMultiplicative)
    {
        int n = y.Length;
        double[] seasonals = new double[n + L + 2];

        // Khởi tạo Level & Trend từ 2 chu kỳ đầu
        double avgFirstCycle = 0;
        double avgSecondCycle = 0;
        for (int i = 0; i < L; i++)
        {
            avgFirstCycle += y[i];
            avgSecondCycle += y[i + L];
        }
        avgFirstCycle /= L;
        avgSecondCycle /= L;

        double level = avgFirstCycle > 0 ? avgFirstCycle : 1.0;
        double trend = (avgSecondCycle - avgFirstCycle) / (L * L);

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

            await EnsureSeedDataAsync();

            if (nguyenLieuId > 0)
            {
                var nlSql = @"SELECT nl.TenNguyenLieu, ISNULL(dvt.TenDonVi, 'kg')
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

                var thSql = @"SELECT MONTH(KyTieuHao) AS Thang, SUM(SoLuongTieuHao) AS TongTieuHao
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
                res.TenNguyenLieu = "Tổng hợp 25 nguyên liệu";
                res.DonViKho = "đơn vị";

                var thSql = @"SELECT MONTH(KyTieuHao) AS Thang, SUM(SoLuongTieuHao) AS TongTieuHao
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
