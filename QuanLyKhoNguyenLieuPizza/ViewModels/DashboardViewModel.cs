using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

// Model items cho Dashboard
public class CanhBaoHSDItem : BaseViewModel
{
    public string MaPhieuNhap { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public DateTime HanSuDung { get; set; }
    public string SoLuongCon { get; set; } = string.Empty;
    public int SoNgayConLai { get; set; }
    public string TrangThai => SoNgayConLai <= 0 ? "Đã hết hạn" 
        : SoNgayConLai <= 3 ? "Sắp hết hạn - Ưu tiên xuất"
        : "Cần theo dõi";
    public string TrangThaiColor => SoNgayConLai <= 0 ? "#DC2626"
        : SoNgayConLai <= 3 ? "#EA580C" : "#D97706";
    public string TrangThaiBg => SoNgayConLai <= 0 ? "#FEE2E2"
        : SoNgayConLai <= 3 ? "#FFF7ED" : "#FEF9C3";
}

public class DoiSoatRangeItem : BaseViewModel
{
    public string MaNL { get; set; } = string.Empty;
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public decimal TongLuongNhap { get; set; }
    public decimal TongLuongTieuHao { get; set; }
}

public class BienDongHaoHutItem : BaseViewModel
{
    public DateTime NgayKiem { get; set; }
    public string MaNL { get; set; } = string.Empty;
    public string HSD { get; set; } = string.Empty;
    public decimal TonHeThong { get; set; }
    public decimal TonThucTe { get; set; }
    public decimal ChenhLech { get; set; }
    public string ChenhLechDisplay => ChenhLech == 0 ? "0" : ChenhLech > 0 ? $"+{ChenhLech:0.##}" : $"{ChenhLech:0.##}";
    public string ChenhLechColor => ChenhLech > 0 ? "#10B981" : ChenhLech < 0 ? "#DC2626" : "#6B7280";
    public string LoaiDieuChinh { get; set; } = string.Empty;
    public string MaPhieuDieuChinh { get; set; } = string.Empty;
    public string ChungTuDisplay => string.IsNullOrEmpty(MaPhieuDieuChinh) ? "" : $"{MaPhieuDieuChinh} ({LoaiDieuChinh})";
    public string ChungTuColor => LoaiDieuChinh.Contains("Xuất") ? "#DC2626" : "#2563EB";
    public string ChungTuBg => LoaiDieuChinh.Contains("Xuất") ? "#FEE2E2" : "#DBEAFE";
}

public class TopMonAnItem : BaseViewModel
{
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoLuongBan { get; set; }
    public decimal DoanhThu { get; set; }
    public double TyTrong { get; set; }
}

public class DashboardViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private bool _isLoading;
    private DateTime _ngayBatDau;
    private DateTime _ngayKetThuc;

    // Section 1
    private decimal _tongDoanhThuBanHang;
    private decimal _tongChiPhiNhapHang;
    private decimal _chenhLechLoiNhuan;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public DateTime NgayBatDau
    {
        get => _ngayBatDau;
        set => SetProperty(ref _ngayBatDau, value);
    }

    public DateTime NgayKetThuc
    {
        get => _ngayKetThuc;
        set => SetProperty(ref _ngayKetThuc, value);
    }

    public decimal TongDoanhThuBanHang
    {
        get => _tongDoanhThuBanHang;
        set => SetProperty(ref _tongDoanhThuBanHang, value);
    }

    public decimal TongChiPhiNhapHang
    {
        get => _tongChiPhiNhapHang;
        set => SetProperty(ref _tongChiPhiNhapHang, value);
    }

    public decimal ChenhLechLoiNhuan
    {
        get => _chenhLechLoiNhuan;
        set => SetProperty(ref _chenhLechLoiNhuan, value);
    }

    // Collections
    public ObservableCollection<CanhBaoHSDItem> CanhBaoHSDItems { get; } = new();
    public ObservableCollection<DoiSoatRangeItem> DoiSoatItems { get; } = new();
    public ObservableCollection<BienDongHaoHutItem> BienDongItems { get; } = new();
    public ObservableCollection<TopMonAnItem> TopMonAnItems { get; } = new();

    // Commands
    public ICommand ApDungCommand { get; }
    public ICommand XoaLocCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExportReportCommand { get; }

    public DashboardViewModel()
    {
        _databaseService = App.Services.GetRequiredService<DatabaseService>();

        _ngayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ngayKetThuc = DateTime.Today;

        ApDungCommand = new AsyncRelayCommand(async _ => await LoadFilteredDataAsync());
        XoaLocCommand = new AsyncRelayCommand(async _ => 
        {
            NgayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            NgayKetThuc = DateTime.Today;
            await LoadFilteredDataAsync();
        });
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAllDataAsync());
        ExportReportCommand = new RelayCommand(_ => ExportReport());

        SafeInitializeAsync(LoadAllDataAsync);
    }

    public DashboardViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        _ngayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _ngayKetThuc = DateTime.Today;

        ApDungCommand = new AsyncRelayCommand(async _ => await LoadFilteredDataAsync());
        XoaLocCommand = new AsyncRelayCommand(async _ => 
        {
            NgayBatDau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            NgayKetThuc = DateTime.Today;
            await LoadFilteredDataAsync();
        });
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAllDataAsync());
        ExportReportCommand = new RelayCommand(_ => ExportReport());

        SafeInitializeAsync(LoadAllDataAsync);
    }

    private async Task LoadAllDataAsync()
    {
        IsLoading = true;
        try
        {
            // Load cảnh báo HSD (real-time, không phụ thuộc bộ lọc)
            await LoadCanhBaoHSDAsync();

            // Load dữ liệu theo bộ lọc
            await LoadFilteredDataAsync();
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCanhBaoHSDAsync()
    {
        try
        {
            var data = await _databaseService.GetLoHangCanhBaoHSDAsync();
            CanhBaoHSDItems.Clear();
            foreach (var item in data)
            {
                var donVi = item.DonVi;
                CanhBaoHSDItems.Add(new CanhBaoHSDItem
                {
                    MaPhieuNhap = item.MaPhieuNhap,
                    TenNguyenLieu = item.TenNguyenLieu,
                    HanSuDung = item.HSD,
                    SoLuongCon = $"{item.SoLuongNhap:G29} {donVi}",
                    SoNgayConLai = item.SoNgayConLai
                });
            }
        }
        catch { }
    }

    private async Task LoadFilteredDataAsync()
    {
        IsLoading = true;
        try
        {
            var fromDate = NgayBatDau.Date;
            var toDate = NgayKetThuc.Date;
            var toDateEnd = toDate.AddDays(1).AddTicks(-1);

            // Parallel load all filtered sections
            var doanhThuTask = _databaseService.GetDoanhThuBanHangAsync(fromDate, toDateEnd);
            var chiPhiTask = _databaseService.GetTotalTongTienPhieuNhapAsync(null, null, fromDate, toDate);
            var doiSoatTask = _databaseService.GetDoiSoatTieuHaoByRangeAsync(fromDate, toDate);
            var bienDongTask = _databaseService.GetBienDongHaoHutAsync(fromDate, toDate);
            var topMonAnTask = _databaseService.GetTopMonAnBanChayAsync(fromDate, toDate, 10);

            await Task.WhenAll(doanhThuTask, chiPhiTask, doiSoatTask, bienDongTask, topMonAnTask);

            // Section 1: Cards
            TongDoanhThuBanHang = doanhThuTask.Result;
            TongChiPhiNhapHang = chiPhiTask.Result;
            ChenhLechLoiNhuan = TongDoanhThuBanHang - TongChiPhiNhapHang;

            // Section 2: Đối soát tiêu hao
            DoiSoatItems.Clear();
            foreach (var (maNL, tenNL, donVi, tongNhap, tongTieuHao) in doiSoatTask.Result)
            {
                DoiSoatItems.Add(new DoiSoatRangeItem
                {
                    MaNL = maNL,
                    TenNguyenLieu = tenNL,
                    DonVi = donVi,
                    TongLuongNhap = tongNhap,
                    TongLuongTieuHao = tongTieuHao
                });
            }

            // Section 3: Biến động hao hụt
            BienDongItems.Clear();
            foreach (var (ngayKiem, maNL, tenNL, hsd, tonHT, tonTT, chenhLech, loaiDC, maPhieuDC) in bienDongTask.Result)
            {
                BienDongItems.Add(new BienDongHaoHutItem
                {
                    NgayKiem = ngayKiem,
                    MaNL = maNL,
                    HSD = hsd,
                    TonHeThong = tonHT,
                    TonThucTe = tonTT,
                    ChenhLech = chenhLech,
                    LoaiDieuChinh = loaiDC,
                    MaPhieuDieuChinh = maPhieuDC
                });
            }

            // Section 4: Top món ăn
            TopMonAnItems.Clear();
            foreach (var (maMon, tenMon, soLuongBan, doanhThu, tyTrong) in topMonAnTask.Result)
            {
                TopMonAnItems.Add(new TopMonAnItem
                {
                    MaMon = maMon,
                    TenMon = tenMon,
                    SoLuongBan = soLuongBan,
                    DoanhThu = doanhThu,
                    TyTrong = tyTrong
                });
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\""; 
        return value;
    }

    private void ExportReport()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Xuất Báo Cáo Dashboard",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"BaoCao_Dashboard_{NgayBatDau:ddMMyyyy}_{NgayKetThuc:ddMMyyyy}.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Column(col =>
                    {
                        col.Item().PaddingBottom(8).Text("BÁO CÁO TỔNG HỢP DASHBOARD - PIZZA INN").FontSize(18).Bold().FontColor(Colors.Black);
                        col.Item().Text($"Kỳ báo cáo: {NgayBatDau:dd/MM/yyyy} - {NgayKetThuc:dd/MM/yyyy}").FontSize(11).FontColor(Colors.Black);
                        col.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(11).FontColor(Colors.Black);
                        col.Item().PaddingTop(10).LineHorizontal(1f).LineColor(Colors.Black);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Section 1: Tổng hợp doanh thu
                        col.Item().PaddingTop(10).PaddingBottom(5).Text("TỔNG HỢP DOANH THU & CHI PHÍ").FontSize(12).Bold().FontColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Chỉ tiêu").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Giá trị (VNĐ)").SemiBold().AlignRight();
                            });

                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tổng doanh thu bán hàng");
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{TongDoanhThuBanHang:N0} đ").AlignRight();
                            
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tổng chi phí nhập hàng");
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{TongChiPhiNhapHang:N0} đ").AlignRight();
                            
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Chênh lệch lợi nhuận");
                            table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{ChenhLechLoiNhuan:N0} đ").AlignRight().Bold();
                        });

                        // Section 2: Cảnh báo HSD
                        col.Item().PaddingTop(20).PaddingBottom(5).Text("CẢNH BÁO HẠN SỬ DỤNG").FontSize(12).Bold().FontColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Mã lô").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Nguyên liệu").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("HSD").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("SL Còn").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Còn lại").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Trạng thái").SemiBold();
                            });

                            foreach (var item in CanhBaoHSDItems)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.MaPhieuNhap);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.TenNguyenLieu);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.HanSuDung.ToString("dd/MM/yyyy"));
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.SoLuongCon);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.SoNgayConLai} ngày");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.TrangThai);
                            }
                        });

                        // Section 3: Đối soát tiêu hao
                        col.Item().PaddingTop(20).PaddingBottom(5).Text("ĐỐI SOÁT TIÊU HAO NGUYÊN LIỆU").FontSize(12).Bold().FontColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Mã NL").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tên nguyên liệu").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Đơn vị").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tổng nhập").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tổng tiêu hao").SemiBold().AlignRight();
                            });

                            foreach (var item in DoiSoatItems)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.MaNL);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.TenNguyenLieu);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.DonVi);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.TongLuongNhap:G29}").AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.TongLuongTieuHao:G29}").AlignRight();
                            }
                        });

                        // Section 4: Biến động hao hụt
                        col.Item().PaddingTop(20).PaddingBottom(5).Text("BIẾN ĐỘNG HAO HỤT (KIỂM KÊ)").FontSize(12).Bold().FontColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Ngày kiểm").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Mã NL").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tồn HT").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tồn TT").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Chênh lệch").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Loại ĐC").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Chứng từ").SemiBold();
                            });

                            foreach (var item in BienDongItems)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.NgayKiem.ToString("dd/MM/yyyy"));
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.MaNL);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.TonHeThong:G29}").AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.TonThucTe:G29}").AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.ChenhLechDisplay).AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.LoaiDieuChinh);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.MaPhieuDieuChinh);
                            }
                        });

                        // Section 5: Top món ăn
                        col.Item().PaddingTop(20).PaddingBottom(5).Text("TOP MÓN ĂN BÁN CHẠY").FontSize(12).Bold().FontColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Mã món").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tên món").SemiBold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("SL bán").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Doanh thu").SemiBold().AlignRight();
                                header.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text("Tỷ trọng").SemiBold().AlignRight();
                            });

                            foreach (var item in TopMonAnItems)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.MaMon);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text(item.TenMon);
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.SoLuongBan:N0}").AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.DoanhThu:N0}").AlignRight();
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(5).Text($"{item.TyTrong}%").AlignRight();
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(dialog.FileName);

            MessageBox.Show($"Xuất báo cáo PDF thành công!\n{dialog.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất báo cáo:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
