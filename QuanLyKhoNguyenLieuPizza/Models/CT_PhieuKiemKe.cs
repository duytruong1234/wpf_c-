using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace QuanLyKhoNguyenLieuPizza.Models;

public class CT_PhieuKiemKe : INotifyPropertyChanged
{
    public int ChiTietKiemID { get; set; }
    public int? KiemKeID { get; set; }
    private int? _nguyenLieuID;
    public int? NguyenLieuID
    {
        get => _nguyenLieuID;
        set
        {
            if (_nguyenLieuID != value)
            {
                _nguyenLieuID = value;
                OnPropertyChanged();
            }
        }
    }

    private decimal _tonHeThong;
    public decimal TonHeThong
    {
        get => _tonHeThong;
        set
        {
            if (_tonHeThong != value)
            {
                _tonHeThong = value;
                OnPropertyChanged();
                RecalculateChenhLech();
            }
        }
    }

    public int? DonViKhoID { get; set; }

    private decimal _tonThucTe;
    public decimal TonThucTe
    {
        get => _tonThucTe;
        set
        {
            if (_tonThucTe != value)
            {
                _tonThucTe = value;
                _tonThucTeText = value.ToString("0.####", CultureInfo.InvariantCulture);
                OnPropertyChanged();
                OnPropertyChanged(nameof(TonThucTeText));
                RecalculateChenhLech();
            }
        }
    }

    private string _tonThucTeText = "0";
    public string TonThucTeText
    {
        get => _tonThucTeText;
        set
        {
            if (_tonThucTeText != value)
            {
                _tonThucTeText = value;
                OnPropertyChanged();

                var normalized = value?.Replace(',', '.') ?? "0";
                if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    if (parsed < 0)
                    {
                        parsed = 0;
                        _tonThucTeText = "0";
                        OnPropertyChanged(nameof(TonThucTeText));
                    }

                    if (_tonThucTe != parsed)
                    {
                        _tonThucTe = parsed;
                        OnPropertyChanged(nameof(TonThucTe));
                        RecalculateChenhLech();
                    }
                }
            }
        }
    }

    public int? DonViKiemID { get; set; }

    private decimal _chenhLech;
    public decimal ChenhLech
    {
        get => _chenhLech;
        set
        {
            if (_chenhLech != value)
            {
                _chenhLech = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChenhLechDisplay));
                OnPropertyChanged(nameof(ChenhLechColor));
                OnPropertyChanged(nameof(ChenhLechLabel));
                OnPropertyChanged(nameof(ChenhLechPercent));
            }
        }
    }

    /// <summary>
    /// Chênh lệch đã quy đổi về đơn vị chuẩn (dùng khi duyệt phiếu).
    /// Được tính bên ngoài bằng hệ số quy đổi.
    /// </summary>
    private decimal _chenhLechQuyDoi;
    public decimal ChenhLechQuyDoi
    {
        get => _chenhLechQuyDoi;
        set
        {
            if (_chenhLechQuyDoi != value)
            {
                _chenhLechQuyDoi = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Hiển thị chênh lệch dạng "+5.00" hoặc "-0.50"
    /// </summary>
    public string ChenhLechDisplay
    {
        get
        {
            if (_chenhLech == 0) return "0";
            return _chenhLech > 0 ? $"+ {_chenhLech:0.##}" : $"- {Math.Abs(_chenhLech):0.##}";
        }
    }

    /// <summary>
    /// Màu sắc cho chênh lệch: xanh (tăng), đỏ (hao hụt), xám (bằng)
    /// </summary>
    public string ChenhLechColor => _chenhLech > 0 ? "#10B981" : _chenhLech < 0 ? "#EF4444" : "#6B7280";

    /// <summary>
    /// Nhãn "Tăng" hoặc "Hao hụt"
    /// </summary>
    public string ChenhLechLabel => _chenhLech > 0 ? "Tăng" : _chenhLech < 0 ? "Hao hụt" : "";

    /// <summary>
    /// Phần trăm chênh lệch (dựa trên tồn hệ thống)
    /// </summary>
    public string ChenhLechPercent
    {
        get
        {
            if (_tonHeThong == 0 || _chenhLech == 0) return "";
            var percent = Math.Abs(_chenhLech / _tonHeThong * 100);
            var label = _chenhLech > 0 ? "Tăng" : "Hao hụt";
            return $"{label} {percent:N1}%";
        }
    }

    private string? _ghiChuNhanVien;
    public string? GhiChuNhanVien
    {
        get => _ghiChuNhanVien;
        set
        {
            if (_ghiChuNhanVien != value)
            {
                _ghiChuNhanVien = value;
                OnPropertyChanged();
            }
        }
    }

    public string? PhanHoiDuyet { get; set; }

    // Hệ số quy đổi đơn vị kiểm → đơn vị chuẩn
    private decimal _heSoKiem = 1m;
    public decimal HeSoKiem
    {
        get => _heSoKiem;
        set
        {
            if (_heSoKiem != value)
            {
                _heSoKiem = value;
                OnPropertyChanged();
                RecalculateChenhLech();
            }
        }
    }

    // Hệ số của đơn vị chuẩn (ĐV lưu tồn kho) — dùng để quy đổi chính xác
    private decimal _heSoChuan = 1m;
    public decimal HeSoChuan
    {
        get => _heSoChuan;
        set
        {
            if (_heSoChuan != value)
            {
                _heSoChuan = value;
                OnPropertyChanged();
                RecalculateChenhLech();
            }
        }
    }

    // Dropdown đơn vị kiểm (giống DonViNhapOptions ở CT_PhieuNhap)
    private ObservableCollection<QuyDoiDonVi> _donViKiemOptions = new();
    public ObservableCollection<QuyDoiDonVi> DonViKiemOptions
    {
        get => _donViKiemOptions;
        set
        {
            if (!ReferenceEquals(_donViKiemOptions, value))
            {
                _donViKiemOptions = value;
                OnPropertyChanged();
            }
        }
    }

    private QuyDoiDonVi? _selectedDonViKiem;
    public QuyDoiDonVi? SelectedDonViKiem
    {
        get => _selectedDonViKiem;
        set
        {
            if (!ReferenceEquals(_selectedDonViKiem, value))
            {
                _selectedDonViKiem = value;

                if (value != null)
                {
                    DonViKiemID = value.DonViID;
                    DonViKiem = value.DonViTinh;
                    _heSoKiem = value.HeSo <= 0 ? 1m : value.HeSo;
                    OnPropertyChanged(nameof(HeSoKiem));
                }

                RecalculateChenhLech();
                OnPropertyChanged();
                OnPropertyChanged(nameof(TenDonViKiem));
            }
        }
    }

    public string TenDonViKiem => SelectedDonViKiem?.DonViTinh?.TenDonVi ?? DonViKiem?.TenDonVi ?? string.Empty;
    public string TenDonViKho => DonViKho?.TenDonVi ?? string.Empty;

    /// <summary>
    /// Chuỗi hiển thị hệ số quy đổi (Ví dụ: "1 Thùng = 5 kg")
    /// </summary>
    public string QuyDoiDisplay
    {
        get
        {
            if (SelectedDonViKiem == null || DonViKho == null) return string.Empty;
            if (SelectedDonViKiem.DonViID == DonViKhoID) return string.Empty;

            if (_heSoKiem < 1 && _heSoKiem > 0)
            {
                var inverse = 1 / _heSoKiem;
                return $"Quy đổi: 1 {TenDonViKho} = {inverse:0.##} {TenDonViKiem}";
            }
            return $"Quy đổi: 1 {TenDonViKiem} = {_heSoKiem:0.##} {TenDonViKho}";
        }
    }

    // Thuộc tính điều hướng
    public virtual PhieuKiemKe? PhieuKiemKe { get; set; }
    private NguyenLieu? _nguyenLieu;
    public virtual NguyenLieu? NguyenLieu
    {
        get => _nguyenLieu;
        set
        {
            if (_nguyenLieu != value)
            {
                _nguyenLieu = value;
                OnPropertyChanged();
            }
        }
    }
    public virtual DonViTinh? DonViKiem { get; set; }

    private DonViTinh? _donViKho;
    public virtual DonViTinh? DonViKho
    {
        get => _donViKho;
        set
        {
            if (_donViKho != value)
            {
                _donViKho = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TenDonViKho));
                OnPropertyChanged(nameof(QuyDoiDisplay));
            }
        }
    }

    /// <summary>
    /// Tính chênh lệch: TonThucTe (quy đổi về đơn vị chuẩn) - TonHeThong
    /// Công thức: convertedAmount = amount * sourceFactor / stockFactor
    /// </summary>
    private void RecalculateChenhLech()
    {
        // Quy đổi tồn thực tế về đơn vị chuẩn (giống ConvertAmountToStockUnitAsync)
        decimal heSoChuan = _heSoChuan > 0 ? _heSoChuan : 1m;
        decimal tonThucTeQuyDoi = _tonThucTe * _heSoKiem / heSoChuan;
        ChenhLech = tonThucTeQuyDoi - _tonHeThong;
        ChenhLechQuyDoi = ChenhLech;
        OnPropertyChanged(nameof(QuyDoiDisplay));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
