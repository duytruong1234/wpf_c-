using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class PrintService
{
    private static void ConfigureQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Xuất Phiếu Nhập
    public static void PrintPhieuNhap(PhieuNhap phieuNhap, IEnumerable<CT_PhieuNhap> chiTiets)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu Phiếu Nhập",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"PhieuNhap_{phieuNhap.MaPhieuNhap}.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            ConfigureQuestPdf();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("PHIẾU NHẬP KHO").FontSize(24).Bold();
                        col.Item().PaddingBottom(20).AlignCenter().Text("PIZZINN - Quản Lý Kho Nguyên Liệu").FontSize(14);
                        
                        col.Item().Text(t => { t.Span("Mã phiếu: ").Bold(); t.Span(phieuNhap.MaPhieuNhap); });
                        string trangThaiNhap = phieuNhap.TrangThai == 1 ? "Chờ duyệt" : (phieuNhap.TrangThai == 2 ? "Đã duyệt" : (phieuNhap.TrangThai == 3 ? "Đã hủy" : "Không xác định"));
                        col.Item().Text(t => { t.Span("Trạng thái: ").Bold(); t.Span(trangThaiNhap); });
                        col.Item().Text(t => { t.Span("Ngày nhập: ").Bold(); t.Span(phieuNhap.NgayNhap.ToString("dd/MM/yyyy HH:mm")); });
                        col.Item().Text(t => { t.Span("Nhân viên nhập: ").Bold(); t.Span(phieuNhap.NhanVienNhap?.HoTen ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Nhà cung cấp: ").Bold(); t.Span(phieuNhap.NhaCungCap?.TenNCC ?? "N/A"); });
                        if (phieuNhap.NgayDuyet.HasValue)
                            col.Item().Text(t => { t.Span("Ngày duyệt: ").Bold(); t.Span(phieuNhap.NgayDuyet.Value.ToString("dd/MM/yyyy HH:mm")); });
                        if (phieuNhap.NhanVienDuyet != null)
                            col.Item().Text(t => { t.Span("Người duyệt: ").Bold(); t.Span(phieuNhap.NhanVienDuyet.HoTen); });
                        
                        col.Item().PaddingBottom(15);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("STT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Tên nguyên liệu").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Đơn vị").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Số lượng").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Đơn giá").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Thành tiền").Bold();
                            });
                            
                            int stt = 1;
                            foreach(var ct in chiTiets)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(stt.ToString());
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(ct.NguyenLieu?.TenNguyenLieu ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(ct.DonViTinh?.TenDonVi ?? ct.NguyenLieu?.DonViTinh?.TenDonVi ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text($"{ct.SoLuong:N2}");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignRight().Text($"{ct.DonGia:N0}");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignRight().Text($"{ct.ThanhTien:N0}");
                                stt++;
                            }
                        });
                        
                        col.Item().PaddingTop(15).AlignRight().Text(t => 
                        {
                            t.Span("TỔNG TIỀN: ").Bold().FontSize(14);
                            t.Span($"{phieuNhap.TongTien:N0} VNĐ").Bold().FontSize(14);
                        });

                        col.Item().PaddingTop(40).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            
                            table.Cell().AlignCenter().Text("Người lập phiếu").Bold();
                            table.Cell().AlignCenter().Text("Người duyệt").Bold();
                            table.Cell().AlignCenter().Text("Người nhận hàng").Bold();
                            
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuNhap.ChuKyNhap));
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuNhap.ChuKySoDuyet));
                            table.Cell().Height(80);
                            
                            table.Cell().AlignCenter().Text(phieuNhap.NhanVienNhap?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text(phieuNhap.NhanVienDuyet?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text("(Ký và ghi rõ họ tên)");
                        });
                    });
                    
                    page.Footer().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                });
            }).GeneratePdf(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region Xuất Phiếu Xuất
    public static void PrintPhieuXuat(PhieuXuat phieuXuat, IEnumerable<CT_PhieuXuat> chiTiets)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu Phiếu Xuất",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"PhieuXuat_{phieuXuat.MaPhieuXuat}.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            ConfigureQuestPdf();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("PHIẾU XUẤT KHO").FontSize(24).Bold();
                        col.Item().PaddingBottom(20).AlignCenter().Text("PIZZINN - Quản Lý Kho Nguyên Liệu").FontSize(14);
                        
                        col.Item().Text(t => { t.Span("Mã phiếu: ").Bold(); t.Span(phieuXuat.MaPhieuXuat); });
                        string trangThaiXuat = phieuXuat.TrangThai == 1 ? "Chờ duyệt" : (phieuXuat.TrangThai == 2 ? "Đã duyệt" : (phieuXuat.TrangThai == 3 ? "Đã hủy" : "Không xác định"));
                        col.Item().Text(t => { t.Span("Trạng thái: ").Bold(); t.Span(trangThaiXuat); });
                        col.Item().Text(t => { t.Span("Lý do xuất: ").Bold(); t.Span(phieuXuat.GhiChu ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Ngày yêu cầu: ").Bold(); t.Span(phieuXuat.NgayYeuCau.ToString("dd/MM/yyyy HH:mm")); });
                        col.Item().Text(t => { t.Span("Nhân viên yêu cầu: ").Bold(); t.Span(phieuXuat.NhanVienYeuCau?.HoTen ?? "N/A"); });
                        if (phieuXuat.NgayDuyet.HasValue)
                            col.Item().Text(t => { t.Span("Ngày duyệt: ").Bold(); t.Span(phieuXuat.NgayDuyet.Value.ToString("dd/MM/yyyy HH:mm")); });
                        if (phieuXuat.NhanVienDuyet != null)
                            col.Item().Text(t => { t.Span("Người duyệt: ").Bold(); t.Span(phieuXuat.NhanVienDuyet.HoTen); });
                        
                        col.Item().PaddingBottom(15);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("STT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Tên nguyên liệu").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Đơn vị").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Số lượng").Bold();
                            });
                            
                            int stt = 1;
                            foreach(var ct in chiTiets)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(stt.ToString());
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(ct.NguyenLieu?.TenNguyenLieu ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(ct.DonViTinh?.TenDonVi ?? ct.NguyenLieu?.DonViTinh?.TenDonVi ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text($"{ct.SoLuong:N2}");
                                stt++;
                            }
                        });

                        col.Item().PaddingTop(40).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            
                            table.Cell().AlignCenter().Text("Người yêu cầu").Bold();
                            table.Cell().AlignCenter().Text("Người duyệt").Bold();
                            table.Cell().AlignCenter().Text("Thủ kho").Bold();
                            
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuXuat.ChuKyXuat));
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuXuat.ChuKySoDuyet));
                            table.Cell().Height(80);
                            
                            table.Cell().AlignCenter().Text(phieuXuat.NhanVienYeuCau?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text(phieuXuat.NhanVienDuyet?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text("(Ký và ghi rõ họ tên)");
                        });
                    });
                    
                    page.Footer().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                });
            }).GeneratePdf(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region Xuất Phiếu Kiểm Kê
    public static void PrintPhieuKiemKe(PhieuKiemKe phieuKiem, IEnumerable<CT_PhieuKiemKe> chiTiets)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu Phiếu Kiểm Kê",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"PhieuKiemKe_{phieuKiem.MaPhieuKiem}.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            ConfigureQuestPdf();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("PHIẾU KIỂM KÊ KHO").FontSize(24).Bold();
                        col.Item().PaddingBottom(20).AlignCenter().Text("PIZZINN - Quản Lý Kho Nguyên Liệu").FontSize(14);
                        
                        col.Item().Text(t => { t.Span("Mã phiếu: ").Bold(); t.Span(phieuKiem.MaPhieuKiem); });
                        col.Item().Text(t => { t.Span("Trạng thái: ").Bold(); t.Span(phieuKiem.TrangThai); });
                        col.Item().Text(t => { t.Span("Loại kiểm kê: ").Bold(); t.Span(phieuKiem.LoaiKiemKe ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Ngày kiểm kê: ").Bold(); t.Span(phieuKiem.NgayKiem?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Nhân viên kiểm: ").Bold(); t.Span(phieuKiem.NhanVienKiem?.HoTen ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Ghi chú: ").Bold(); t.Span(phieuKiem.LyDoKiemKe ?? "N/A"); });
                        if (phieuKiem.NgayDuyet.HasValue)
                            col.Item().Text(t => { t.Span("Ngày duyệt: ").Bold(); t.Span(phieuKiem.NgayDuyet.Value.ToString("dd/MM/yyyy HH:mm")); });
                        if (phieuKiem.NhanVienDuyet != null)
                            col.Item().Text(t => { t.Span("Người duyệt: ").Bold(); t.Span(phieuKiem.NhanVienDuyet.HoTen); });
                        
                        col.Item().PaddingBottom(15);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("STT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Nguyên liệu").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("ĐVT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Tồn HT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Tồn TT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Chênh lệch").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Ghi chú").Bold();
                            });
                            
                            int stt = 1;
                            foreach(var ct in chiTiets)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(stt.ToString());
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(ct.NguyenLieu?.TenNguyenLieu ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(ct.DonViKiem?.TenDonVi ?? ct.NguyenLieu?.DonViTinh?.TenDonVi ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text($"{ct.TonHeThong:N2}");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text($"{ct.TonThucTe:N2}");
                                
                                decimal chenhLech = ct.TonThucTe - ct.TonHeThong;
                                string chenhLechStr = chenhLech > 0 ? $"+{chenhLech:N2}" : $"{chenhLech:N2}";
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(chenhLechStr);
                                
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(ct.GhiChuNhanVien ?? "");
                                stt++;
                            }
                        });

                        col.Item().PaddingTop(40).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            
                            table.Cell().AlignCenter().Text("Nhân viên kiểm kê").Bold();
                            table.Cell().AlignCenter().Text("Người duyệt").Bold();
                            table.Cell().AlignCenter().Text("Đại diện kho").Bold();
                            
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuKiem.ChuKyKiem));
                            table.Cell().Height(80).AlignCenter().AlignMiddle().Element(e => RenderSignature(e, phieuKiem.ChuKyDuyet));
                            table.Cell().Height(80);
                            
                            table.Cell().AlignCenter().Text(phieuKiem.NhanVienKiem?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text(phieuKiem.NhanVienDuyet?.HoTen ?? "(Ký và ghi rõ họ tên)");
                            table.Cell().AlignCenter().Text("(Ký và ghi rõ họ tên)");
                        });
                    });
                    
                    page.Footer().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                });
            }).GeneratePdf(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region Xuất Hóa Đơn Bán Hàng
    public static void PrintHoaDonBanHang(DonHang donHang, IEnumerable<CT_DonHang> chiTiets)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu Hóa Đơn",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"HoaDon_{donHang.MaDonHang}.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            ConfigureQuestPdf();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("HÓA ĐƠN BÁN HÀNG").FontSize(24).Bold();
                        col.Item().PaddingBottom(5).AlignCenter().Text("PIZZINN - Pizza & Đồ Ăn Nhanh").FontSize(14);
                        col.Item().PaddingBottom(15).AlignCenter().Text("Địa chỉ: TP. Hồ Chí Minh").FontSize(10).FontColor(Colors.Grey.Darken2);
                        
                        col.Item().PaddingBottom(15).AlignCenter().Text("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━").FontColor(Colors.Grey.Darken1);
                        
                        col.Item().Text(t => { t.Span("Mã hóa đơn: ").Bold(); t.Span(donHang.MaDonHang); });
                        col.Item().Text(t => { t.Span("Ngày bán: ").Bold(); t.Span(donHang.NgayTao.ToString("dd/MM/yyyy HH:mm")); });
                        col.Item().Text(t => { t.Span("Nhân viên: ").Bold(); t.Span(donHang.NhanVien?.HoTen ?? "N/A"); });
                        col.Item().Text(t => { t.Span("Phương thức TT: ").Bold(); t.Span(donHang.PhuongThucTT ?? "N/A"); });
                        
                        col.Item().PaddingBottom(15);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.6f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("STT").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Sản phẩm").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Size").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("SL").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Đơn giá").Bold();
                                header.Cell().Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Thành tiền").Bold();
                            });
                            
                            int stt = 1;
                            foreach(var ct in chiTiets)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(stt.ToString());
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).Text(ct.Pizza?.TenPizza ?? "N/A");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text(ct.Pizza?.KichThuoc ?? "");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignCenter().Text($"{ct.SoLuong}");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignRight().Text($"{ct.DonGia:N0}đ");
                                table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignRight().Text($"{ct.ThanhTien:N0}đ");
                                stt++;
                            }
                        });
                        
                        col.Item().PaddingTop(15).AlignRight().Text($"Tổng tiền hàng: {donHang.TongTien:N0}đ").FontSize(13);
                        if (donHang.GiamGia > 0)
                        {
                            col.Item().AlignRight().Text($"Giảm giá: -{donHang.GiamGia:N0}đ").FontSize(13);
                        }
                        
                        col.Item().PaddingTop(5).AlignRight().Text(t => 
                        {
                            t.Span("THANH TOÁN: ").Bold().FontSize(16);
                            t.Span($"{donHang.ThanhToan:N0} VNĐ").Bold().FontSize(16);
                        });

                        if (donHang.TienKhachDua.HasValue && donHang.TienKhachDua.Value > 0)
                        {
                            col.Item().PaddingTop(5).AlignRight().Text($"Tiền khách đưa: {donHang.TienKhachDua.Value:N0}đ").FontSize(13);
                            decimal tienThua = donHang.TienKhachDua.Value - donHang.ThanhToan;
                            if (tienThua > 0)
                            {
                                col.Item().AlignRight().Text($"Tiền thừa: {tienThua:N0}đ").FontSize(13);
                            }
                        }

                        col.Item().PaddingTop(30).AlignCenter().Text("Cảm ơn quý khách! Hẹn gặp lại!").FontSize(12).Italic();

                        // Mã QR cho chuyển khoản
                        if (donHang.PhuongThucTT != null && donHang.PhuongThucTT.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string accountNo = "1234567890";
                                string accountName = "PIZZAINN";
                                string addInfo = $"TT {donHang.MaDonHang}";
                                string qrData = $"Ngân hàng: Vietcombank\nSTK: {accountNo}\nTên: {accountName}\nSố tiền: {donHang.ThanhToan:N0} VNĐ\nNội dung: {addInfo}";

                                using var qrGenerator = new QRCodeGenerator();
                                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                                using var qrCode = new QRCode(qrCodeData);
                                using var qrBitmap = qrCode.GetGraphic(6);
                                using var memory = new MemoryStream();
                                qrBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] qrBytes = memory.ToArray();

                                col.Item().PaddingTop(20).AlignCenter().Column(qrCol =>
                                {
                                    qrCol.Item().AlignCenter().Text("━━━ QUÉT MÃ ĐỂ CHUYỂN KHOẢN ━━━").FontSize(11).FontColor(Colors.Grey.Darken1);
                                    qrCol.Item().PaddingTop(5).Height(150).Width(150).Image(qrBytes).FitArea();
                                    qrCol.Item().PaddingTop(5).AlignCenter().Text("Vietcombank - " + accountNo).FontSize(11).Bold();
                                    qrCol.Item().AlignCenter().Text(accountName).FontSize(11).Bold();
                                    qrCol.Item().AlignCenter().Text($"Số tiền: {donHang.ThanhToan:N0} VNĐ").FontSize(11);
                                });
                            }
                            catch { }
                        }
                    });
                    
                    page.Footer().AlignRight().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).Italic();
                });
            }).GeneratePdf(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region Phương thức hỗ trợ
    private static void RenderSignature(IContainer container, string? chuKySoBase64)
    {
        if (string.IsNullOrEmpty(chuKySoBase64)) return;
        try
        {
            byte[] imageBytes = Convert.FromBase64String(chuKySoBase64);
            container.Image(imageBytes).FitArea();
        }
        catch
        {
            container.Text("[Chữ ký số đã xác nhận]").Italic().FontSize(9);
        }
    }
    #endregion
}
