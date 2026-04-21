using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace QuanLyKhoNguyenLieuPizza.Models;

public class DonViOption
{
    public int DonViID { get; set; }
    public string TenDonVi { get; set; } = string.Empty;
    public decimal HeSo { get; set; } = 1;
}

public class CT_PhieuXuat : INotifyPropertyChanged
{
    public int ChiTietID { get; set; }
    public int? PhieuXuatID { get; set; }
    public int? NguyenLieuID { get; set; }

    private decimal _soLuong;
    public decimal SoLuong
    {
        get => _soLuong;
        set
        {
            if (_soLuong != value)
            {
                _soLuong = value;
                _soLuongText = value.ToString("0.####", CultureInfo.InvariantCulture);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoLuongText));
                // Tự động tính lại thành tiền
                ThanhTien = _soLuong * DonGia;
            }
        }
    }

    private string _soLuongText = "0";
    public string SoLuongText
    {
        get => _soLuongText;
        set
        {
            if (_soLuongText != value)
            {
                _soLuongText = value;
                OnPropertyChanged();

                // Normalize separator
                var normalized = value?.Replace(',', '.') ?? "0";
                if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    if (_soLuong != parsed)
                    {
                        _soLuong = parsed;
                        OnPropertyChanged(nameof(SoLuong));
                        ThanhTien = _soLuong * DonGia;
                    }
                }
            }
        }
    }

    public int? DonViID { get; set; }

    private decimal _heSo;
    public decimal HeSo
    {
        get => _heSo;
        set
        {
            if (_heSo != value)
            {
                _heSo = value;
                OnPropertyChanged();
                ThanhTien = SoLuong * DonGia;
            }
        }
    }

    private decimal _donGia;
    public decimal DonGia
    {
        get => _donGia;
        set
        {
            if (_donGia != value)
            {
                _donGia = value;
                OnPropertyChanged();
                ThanhTien = SoLuong * _donGia;
            }
        }
    }

    private decimal? _thanhTien;
    public decimal? ThanhTien
    {
        get => _thanhTien;
        set
        {
            if (_thanhTien != value)
            {
                _thanhTien = value;
                OnPropertyChanged();
            }
        }
    }

    // Danh sách đơn vị khả dụng cho dropdown
    private ObservableCollection<DonViOption> _donViOptions = new();
    public ObservableCollection<DonViOption> DonViOptions
    {
        get => _donViOptions;
        set
        {
            _donViOptions = value;
            OnPropertyChanged();
        }
    }

    // Đơn vị được chọn trong dropdown
    private DonViOption? _selectedDonVi;
    public DonViOption? SelectedDonVi
    {
        get => _selectedDonVi;
        set
        {
            if (_selectedDonVi != value)
            {
                _selectedDonVi = value;
                OnPropertyChanged();
                if (value != null)
                {
                    DonViID = value.DonViID;
                    HeSo = value.HeSo;
                    DonViTinh = new DonViTinh { DonViID = value.DonViID, TenDonVi = value.TenDonVi };
                }
            }
        }
    }

    // Thuộc tính điều hướng
    public virtual PhieuXuat? PhieuXuat { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
