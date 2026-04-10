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

        private LocationService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://provinces.open-api.vn/api/") };
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<List<ApiProvince>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("p/");
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var provinces = await JsonSerializer.DeserializeAsync<List<ApiProvince>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return provinces?.OrderBy(p => p.name).ToList() ?? new List<ApiProvince>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching provinces: {ex.Message}");
            }
            return new List<ApiProvince>();
        }

        public async Task<List<ApiDistrict>> GetDistrictsAsync(int provinceCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"p/{provinceCode}?depth=2");
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var province = await JsonSerializer.DeserializeAsync<ApiProvinceResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return province?.districts?.OrderBy(d => d.name).ToList() ?? new List<ApiDistrict>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching districts: {ex.Message}");
            }
            return new List<ApiDistrict>();
        }

        public async Task<List<ApiWard>> GetWardsAsync(int districtCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"d/{districtCode}?depth=2");
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var district = await JsonSerializer.DeserializeAsync<ApiDistrictResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return district?.wards?.OrderBy(w => w.name).ToList() ?? new List<ApiWard>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching wards: {ex.Message}");
            }
            return new List<ApiWard>();
        }
    }

    public class ApiProvince
    {
        public int code { get; set; }
        public string name { get; set; } = "";
    }

    public class ApiDistrict
    {
        public int code { get; set; }
        public string name { get; set; } = "";
    }

    public class ApiWard
    {
        public int code { get; set; }
        public string name { get; set; } = "";
    }

    public class ApiProvinceResponse
    {
        public List<ApiDistrict> districts { get; set; } = new();
    }

    public class ApiDistrictResponse
    {
        public List<ApiWard> wards { get; set; } = new();
    }
}
