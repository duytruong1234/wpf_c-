using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services.Repositories;

/// <summary>
/// Lớp cơ sở chứa các phương thức truy vấn dùng chung cho tất cả service.
/// </summary>
public abstract class DatabaseContext
{
    protected readonly string _connectionString;

    protected DatabaseContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected SqlConnection GetConnection() => new SqlConnection(_connectionString);

    protected sealed class IngredientUnitContext
    {
        public int IngredientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? StockUnitId { get; init; }
        public string? StockUnitName { get; init; }
        public Dictionary<int, decimal> UnitFactors { get; } = new();
    }

    protected static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction? transaction = null)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (transaction != null)
            cmd.Transaction = transaction;
        return cmd;
    }

    protected static string NormalizeUnitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    protected static bool TryGetWeightFactorToKilogram(string? unitName, out decimal factor)
    {
        factor = NormalizeUnitToken(unitName) switch
        {
            "g" => 0.001m,
            "kg" => 1m,
            _ => 0m
        };

        return factor > 0;
    }

    protected static bool TryGetVolumeFactorToLiter(string? unitName, out decimal factor)
    {
        factor = NormalizeUnitToken(unitName) switch
        {
            "ml" => 0.001m,
            "l" => 1m,
            "lit" => 1m,
            _ => 0m
        };

        return factor > 0;
    }

    protected static bool IsPackageLikeUnit(string? unitName)
    {
        return NormalizeUnitToken(unitName) switch
        {
            "bao" => true,
            "thung" => true,
            "hop" => true,
            "goi" => true,
            "cai" => true,
            "can" => true,
            _ => false
        };
    }

    protected async Task<string?> GetUnitNameAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int? unitId,
        Dictionary<int, string> unitNameCache)
    {
        if (!unitId.HasValue)
            return null;

        if (unitNameCache.TryGetValue(unitId.Value, out var cached))
            return cached;

        const string sql = "SELECT TenDonVi FROM DonViTinh WHERE DonViID = @DonViID";
        using var cmd = CreateCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("@DonViID", unitId.Value);
        var result = await cmd.ExecuteScalarAsync();
        var name = result?.ToString();

        if (!string.IsNullOrWhiteSpace(name))
            unitNameCache[unitId.Value] = name;

        return name;
    }

    protected async Task<IngredientUnitContext> GetIngredientUnitContextAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int ingredientId,
        Dictionary<int, IngredientUnitContext> contextCache)
    {
        if (contextCache.TryGetValue(ingredientId, out var cached))
            return cached;

        const string ingredientSql = @"SELECT nl.NguyenLieuID, nl.TenNguyenLieu, nl.DonViID, dv.TenDonVi
                                       FROM NguyenLieu nl
                                       LEFT JOIN DonViTinh dv ON nl.DonViID = dv.DonViID
                                       WHERE nl.NguyenLieuID = @NguyenLieuID";

        IngredientUnitContext? context = null;
        using (var cmd = CreateCommand(ingredientSql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", ingredientId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                context = new IngredientUnitContext
                {
                    IngredientId = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? $"ID {ingredientId}" : reader.GetString(1),
                    StockUnitId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    StockUnitName = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
        }

        context ??= new IngredientUnitContext
        {
            IngredientId = ingredientId,
            Name = $"ID {ingredientId}"
        };

        const string factorSql = @"SELECT DonViID, HeSo
                                   FROM QuyDoiDonVi
                                   WHERE NguyenLieuID = @NguyenLieuID";

        using (var cmd = CreateCommand(factorSql, conn, transaction))
        {
            cmd.Parameters.AddWithValue("@NguyenLieuID", ingredientId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0))
                    continue;

                var unitId = reader.GetInt32(0);
                var factor = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                if (factor > 0)
                    context.UnitFactors[unitId] = factor;
            }
        }

        if (context.StockUnitId is int stockUnitId && !context.UnitFactors.ContainsKey(stockUnitId))
            context.UnitFactors[stockUnitId] = 1m;

        contextCache[ingredientId] = context;
        return context;
    }

    private static decimal GetReliableFactor(int unitId, string? unitName, Dictionary<int, decimal> dbFactors)
    {
        // CHỈ lấy hệ số từ bảng QuyDoiDonVi — KHÔNG tự suy diễn
        // Đảm bảo tính toàn vẹn: Nhập, Xuất, Bán hàng đều tuân theo cùng 1 quy tắc
        if (dbFactors.TryGetValue(unitId, out var factor) && factor > 0)
            return factor;

        // Nếu DB không có → trả về 0 (không quy đổi được)
        // Buộc người dùng phải cấu hình QuyDoiDonVi trước khi sử dụng
        return 0m;
    }

    protected static bool TryConvertByConfiguredFactors(
        IngredientUnitContext context,
        int sourceUnitId,
        string? sourceUnitName,
        decimal amount,
        out decimal convertedAmount)
    {
        convertedAmount = 0m;

        if (context.StockUnitId is not int stockUnitId)
            return false;

        var sourceFactor = GetReliableFactor(sourceUnitId, sourceUnitName, context.UnitFactors);
        if (sourceFactor <= 0) return false;

        var stockFactor = GetReliableFactor(stockUnitId, context.StockUnitName, context.UnitFactors);
        if (stockFactor <= 0) return false;

        convertedAmount = amount * sourceFactor / stockFactor;
        return true;
    }

    protected async Task<decimal> ConvertAmountToStockUnitAsync(
        SqlConnection conn,
        SqlTransaction? transaction,
        int ingredientId,
        decimal amount,
        int? sourceUnitId,
        Dictionary<int, IngredientUnitContext> contextCache,
        Dictionary<int, string> unitNameCache)
    {
        if (amount <= 0)
            return 0m;

        var context = await GetIngredientUnitContextAsync(conn, transaction, ingredientId, contextCache);

        // Nếu không có đơn vị nguồn hoặc đơn vị tồn kho → dùng nguyên số lượng gốc
        if (sourceUnitId == null || context.StockUnitId == null || sourceUnitId == context.StockUnitId)
            return amount;

        var sourceUnitName = await GetUnitNameAsync(conn, transaction, sourceUnitId, unitNameCache);
        if (TryConvertByConfiguredFactors(context, sourceUnitId.Value, sourceUnitName, amount, out var convertedAmount))
            return convertedAmount;

        // Fallback: Khi chưa cấu hình quy đổi → dùng nguyên số lượng gốc (đơn vị gốc)
        // thay vì throw lỗi, để hệ thống vẫn hoạt động được
        System.Diagnostics.Debug.WriteLine(
            $"[WARN] Chưa cấu hình quy đổi đơn vị cho nguyên liệu '{context.Name}' " +
            $"({sourceUnitName ?? $"ID {sourceUnitId.Value}"} -> {context.StockUnitName ?? "đơn vị tồn kho"}). " +
            $"Sử dụng số lượng gốc: {amount}");
        return amount;
    }

    /// <summary>
    /// Thực thi truy vấn và ánh xạ kết quả thành danh sách.
    /// </summary>
    protected async Task<List<T>> ExecuteQueryListAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var result = new List<T>();
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(mapper(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Query Error: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Thực thi truy vấn trả về một giá trị đơn.
    /// </summary>
    protected async Task<T> ExecuteScalarValueAsync<T>(string sql, T defaultValue, params SqlParameter[] parameters)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Scalar Error: {ex.Message}");
        }
        return defaultValue;
    }

    /// <summary>
    /// Thực thi lệnh không trả về dữ liệu (INSERT/UPDATE/DELETE).
    /// </summary>
    protected async Task<bool> ExecuteCommandAsync(string sql, params SqlParameter[] parameters)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Command Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thực thi truy vấn trả về một đối tượng hoặc null.
    /// </summary>
    protected async Task<T?> ExecuteQuerySingleAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters) where T : class
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters.Length > 0) cmd.Parameters.AddRange(parameters);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Query Error: {ex.Message}");
        }
        return null;
    }
}
