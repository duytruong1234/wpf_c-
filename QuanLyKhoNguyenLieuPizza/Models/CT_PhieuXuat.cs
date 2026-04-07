using System.Collections.ObjectModel;
using System.ComponentModel;
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
                OnPropertyChanged();
                // Tự động tính lại thành tiền
                ThanhTien = _soLuong * HeSo * DonGia;
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
