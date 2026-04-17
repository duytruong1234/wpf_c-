using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_PhieuNhap : INotifyPropertyChanged
{
    public int ChiTietID { get; set; }
    public int? PhieuNhapID { get; set; }
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
                OnPropertyChanged();
                RecalculateThanhTien();
            }
        }
    }

    public int? DonViID { get; set; }

    private decimal _heSo = 1m;
    public decimal HeSo
    {
        get => _heSo;
        set
        {
            if (_heSo != value)
            {
                _heSo = value;
                OnPropertyChanged();
                RecalculateThanhTien();
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
                RecalculateThanhTien();
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

    public DateTime? HSD { get; set; }

    private ObservableCollection<QuyDoiDonVi> _donViNhapOptions = new();
    public ObservableCollection<QuyDoiDonVi> DonViNhapOptions
    {
        get => _donViNhapOptions;
        set
        {
            if (!ReferenceEquals(_donViNhapOptions, value))
            {
                _donViNhapOptions = value;
                OnPropertyChanged();
            }
        }
    }

    private QuyDoiDonVi? _selectedDonViNhap;
    public QuyDoiDonVi? SelectedDonViNhap
    {
        get => _selectedDonViNhap;
        set
        {
            if (!ReferenceEquals(_selectedDonViNhap, value))
            {
                var oldHeSo = _heSo;
                if (oldHeSo <= 0) oldHeSo = 1m;

                _selectedDonViNhap = value;

                if (value != null)
                {
                    var newHeSo = value.HeSo <= 0 ? 1m : value.HeSo;

                    // Quy đổi Đơn giá theo tỷ lệ thuận với Hệ số
                    // Vì HeSo giờ chuẩn rồi: kg = 1, g = 0.001, Bao = 25
                    if (_donGia > 0 && oldHeSo != newHeSo)
                    {
                        _donGia = _donGia * newHeSo / oldHeSo;
                        OnPropertyChanged(nameof(DonGia));
                    }

                    DonViID = value.DonViID;
                    DonViTinh = value.DonViTinh;
                    _heSo = newHeSo;
                    OnPropertyChanged(nameof(HeSo));
                }

                RecalculateThanhTien();
                OnPropertyChanged();
                OnPropertyChanged(nameof(TenDonViNhap));
            }
        }
    }

    public string TenDonViNhap => SelectedDonViNhap?.DonViTinh?.TenDonVi ?? DonViTinh?.TenDonVi ?? string.Empty;

    // Thuộc tính điều hướng
    public virtual PhieuNhap? PhieuNhap { get; set; }
    public virtual NguyenLieu? NguyenLieu { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }

    private void RecalculateThanhTien()
    {
        // ThanhTien = SoLuong * DonGia
        ThanhTien = _soLuong * _donGia;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
