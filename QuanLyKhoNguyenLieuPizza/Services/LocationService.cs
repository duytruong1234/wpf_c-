using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuanLyKhoNguyenLieuPizza.Services
{
    public class LocationService
    {
        private static LocationService? _instance;
        public static LocationService Instance => _instance ??= new LocationService();

        private Dictionary<string, TinhThanh> _data = new();

        private LocationService()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "LocationData", "tree.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    _data = JsonSerializer.Deserialize<Dictionary<string, TinhThanh>>(json) ?? new Dictionary<string, TinhThanh>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load location data: {ex.Message}");
            }
        }

        public List<string> GetTinhThanhs()
        {
            return _data.Values.Select(x => x.name_with_type ?? x.name).OrderBy(x => x).ToList();
        }

        public List<string> GetQuanHuyens(string tinhThanhName)
        {
            var tinh = _data.Values.FirstOrDefault(x => (x.name_with_type ?? x.name) == tinhThanhName);
            if (tinh != null && tinh.quan_huyen != null)
            {
                return tinh.quan_huyen.Values.Select(x => x.name_with_type ?? x.name).OrderBy(x => x).ToList();
            }
            return new List<string>();
        }

        public List<string> GetPhuongXas(string tinhThanhName, string quanHuyenName)
        {
            var tinh = _data.Values.FirstOrDefault(x => (x.name_with_type ?? x.name) == tinhThanhName);
            if (tinh != null && tinh.quan_huyen != null)
            {
                var quan = tinh.quan_huyen.Values.FirstOrDefault(x => (x.name_with_type ?? x.name) == quanHuyenName);
                if (quan != null && quan.xa_phuong != null)
                {
                    return quan.xa_phuong.Values.Select(x => x.name_with_type ?? x.name).OrderBy(x => x).ToList();
                }
            }
            return new List<string>();
        }
    }

    public class TinhThanh
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string slug { get; set; } = "";
        public string name_with_type { get; set; } = "";
        public string path { get; set; } = "";
        public string path_with_type { get; set; } = "";
        public string code { get; set; } = "";
        
        [JsonPropertyName("quan-huyen")]
        public Dictionary<string, QuanHuyen>? quan_huyen { get; set; }
    }

    public class QuanHuyen
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string slug { get; set; } = "";
        public string name_with_type { get; set; } = "";
        public string path { get; set; } = "";
        public string path_with_type { get; set; } = "";
        public string code { get; set; } = "";
        public string parent_code { get; set; } = "";
        
        [JsonPropertyName("xa-phuong")]
        public Dictionary<string, PhuongXa>? xa_phuong { get; set; }
    }

    public class PhuongXa
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string slug { get; set; } = "";
        public string name_with_type { get; set; } = "";
        public string path { get; set; } = "";
        public string path_with_type { get; set; } = "";
        public string code { get; set; } = "";
        public string parent_code { get; set; } = "";
    }
}
