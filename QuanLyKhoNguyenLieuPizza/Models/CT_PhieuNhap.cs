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
                // Tự động tính lại thành tiền
                ThanhTien = _soLuong * HeSo * DonGia;
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
                ThanhTien = SoLuong * _heSo * DonGia;
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
                ThanhTien = SoLuong * HeSo * _donGia;
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
                _selectedDonViNhap = value;

                if (value != null)
                {
                    DonViID = value.DonViID;
                    DonViTinh = value.DonViTinh;
                    HeSo = value.HeSo <= 0 ? 1m : value.HeSo;
                }

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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
