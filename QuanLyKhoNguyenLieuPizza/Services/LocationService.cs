using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuanLyKhoNguyenLieuPizza.Services
{
    public class LocationService
    {
        private static LocationService? _instance;
        public static LocationService Instance => _instance ??= new LocationService();

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private LocationService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://tinhthanhpho.com/api/v1/") };
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Lấy danh sách tỉnh/thành phố mới (34 tỉnh - cấu trúc sau 1/7/2025)
        /// </summary>
        public async Task<List<ApiProvince>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("new-provinces?limit=100");
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<ApiResponse<List<ApiProvince>>>(stream, _jsonOptions);
                    return result?.data?.OrderBy(p => p.name).ToList() ?? new List<ApiProvince>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching provinces: {ex.Message}");
            }
            return new List<ApiProvince>();
        }

        /// <summary>
        /// Lấy danh sách phường/xã theo tỉnh (cấu trúc mới - không còn quận/huyện)
        /// </summary>
        public async Task<List<ApiWard>> GetWardsAsync(string provinceCode)
        {
            try
            {
                // API có pagination, cần lấy hết - load trang đầu rồi check total
                var allWards = new List<ApiWard>();
                int page = 1;
                int limit = 100;
                int total = 0;

                do
                {
                    var response = await _httpClient.GetAsync($"new-provinces/{provinceCode}/wards?limit={limit}&page={page}");
                    if (response.IsSuccessStatusCode)
                    {
                        using var stream = await response.Content.ReadAsStreamAsync();
                        var result = await JsonSerializer.DeserializeAsync<ApiResponse<List<ApiWard>>>(stream, _jsonOptions);
                        if (result?.data != null)
                        {
                            allWards.AddRange(result.data);
                            total = result.metadata?.total ?? 0;
                        }
                        else break;
                    }
                    else break;
                    page++;
                } while (allWards.Count < total);

                return allWards.OrderBy(w => w.name).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching wards: {ex.Message}");
            }
            return new List<ApiWard>();
        }

        // Giữ lại method cũ cho backward compatibility (trả về list rỗng)
        public Task<List<ApiDistrict>> GetDistrictsAsync(int provinceCode)
        {
            return Task.FromResult(new List<ApiDistrict>());
        }

        public Task<List<ApiWard>> GetWardsAsync(int districtCode)
        {
            return Task.FromResult(new List<ApiWard>());
        }
    }

    // Response wrapper theo format API mới
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public T? data { get; set; }
        public ApiMetadata? metadata { get; set; }
    }

    public class ApiMetadata
    {
        public int total { get; set; }
        public int page { get; set; }
        public int limit { get; set; }
    }

    public class ApiProvince
    {
        public int province_id { get; set; }
        public string code { get; set; } = "";
        public string name { get; set; } = "";
        public string type { get; set; } = "";
    }

    public class ApiDistrict
    {
        public int code { get; set; }
        public string name { get; set; } = "";
    }

    public class ApiWard
    {
        public int ward_id { get; set; }
        public string code { get; set; } = "";
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string province_code { get; set; } = "";
    }

    // Giữ lại cho backward compatibility
    public class ApiProvinceResponse
    {
        public List<ApiDistrict> districts { get; set; } = new();
    }

    public class ApiDistrictResponse
    {
        public List<ApiWard> wards { get; set; } = new();
    }
}
