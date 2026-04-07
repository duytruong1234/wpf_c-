using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using QuanLyKhoNguyenLieuPizza.Models;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class PrintService
{
    #region In Phiếu Nhập
    public static void PrintPhieuNhap(PhieuNhap phieuNhap, IEnumerable<CT_PhieuNhap> chiTiets)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreatePhieuNhapDocument(phieuNhap, chiTiets, printDialog.PrintableAreaWidth);
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu Nhập - {phieuNhap.MaPhieuNhap}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi in phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static FlowDocument CreatePhieuNhapDocument(PhieuNhap phieuNhap, IEnumerable<CT_PhieuNhap> chiTiets, double pageWidth)
    {
        var document = new FlowDocument
        {
            PageWidth = pageWidth,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        // Tiêu đề
        var headerPara = new Paragraph(new Run("PHIẾU NHẬP KHO"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(headerPara);

        // Tên công ty
        var companyPara = new Paragraph(new Run("PIZZINN - Quản Lý Kho Nguyên Liệu"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };
        document.Blocks.Add(companyPara);

        // Thông tin phiếu
        var infoPara = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        infoPara.Inlines.Add(new Run("Mã phiếu: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.MaPhieuNhap}\n"));
        infoPara.Inlines.Add(new Run("Ngày nhập: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NgayNhap:dd/MM/yyyy HH:mm}\n"));
        infoPara.Inlines.Add(new Run("Nhân viên nhập: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NhanVienNhap?.HoTen ?? "N/A"}\n"));
        infoPara.Inlines.Add(new Run("Nhà cung cấp: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NhaCungCap?.TenNCC ?? "N/A"}\n"));
        document.Blocks.Add(infoPara);

        // Bảng chi tiết
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });  // STT
        table.Columns.Add(new TableColumn { Width = new GridLength(2.5, GridUnitType.Star) }); // Tên nguyên liệu
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) });  // Đơn vị
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) });  // Số lượng
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) }); // Đơn giá
        table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) }); // Thành tiền

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        // Dòng tiêu đề
        var headerRow = new TableRow { Background = Brushes.LightGray };
        headerRow.Cells.Add(CreateTableCell("STT", true));
        headerRow.Cells.Add(CreateTableCell("Tên nguyên liệu", true));
        headerRow.Cells.Add(CreateTableCell("Đơn vị", true));
        headerRow.Cells.Add(CreateTableCell("Số lượng", true));
        headerRow.Cells.Add(CreateTableCell("Đơn giá", true));
        headerRow.Cells.Add(CreateTableCell("Thành tiền", true));
        rowGroup.Rows.Add(headerRow);

        // Dòng dữ liệu
        int stt = 1;
        foreach (var ct in chiTiets)
        {
            var row = new TableRow();
            row.Cells.Add(CreateTableCell(stt.ToString()));
            row.Cells.Add(CreateTableCell(ct.NguyenLieu?.TenNguyenLieu ?? "N/A"));
            row.Cells.Add(CreateTableCell(ct.DonViTinh?.TenDonVi ?? ct.NguyenLieu?.DonViTinh?.TenDonVi ?? "N/A"));
            row.Cells.Add(CreateTableCell($"{ct.SoLuong:N2}"));
            row.Cells.Add(CreateTableCell($"{ct.DonGia:N0} VND"));
            row.Cells.Add(CreateTableCell($"{ct.ThanhTien:N0} VND"));
            rowGroup.Rows.Add(row);
            stt++;
        }

        document.Blocks.Add(table);

        // Tổng tiền
        var totalPara = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        totalPara.Inlines.Add(new Run("TỔNG TIỀN: ") { FontWeight = FontWeights.Bold, FontSize = 16 });
        totalPara.Inlines.Add(new Run($"{phieuNhap.TongTien:N0} VNĐ") { FontWeight = FontWeights.Bold, FontSize = 16, Foreground = Brushes.Red });
        document.Blocks.Add(totalPara);

        // Chân trang - Chữ ký
        var signatureTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 50, 0, 0) };
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        
        var signatureRowGroup = new TableRowGroup();
        signatureTable.RowGroups.Add(signatureRowGroup);
        
        var signatureRow = new TableRow();
        signatureRow.Cells.Add(CreateSignatureCell("Người lập phiếu"));
        signatureRow.Cells.Add(CreateSignatureCell("Người nhận hàng"));
        signatureRowGroup.Rows.Add(signatureRow);

        document.Blocks.Add(signatureTable);

        // Ngày in
        var printDatePara = new Paragraph(new Run($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 30, 0, 0)
        };
        document.Blocks.Add(printDatePara);

        return document;
    }
    #endregion

    #region In Phiếu Xuất
    public static void PrintPhieuXuat(PhieuXuat phieuXuat, IEnumerable<CT_PhieuXuat> chiTiets)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreatePhieuXuatDocument(phieuXuat, chiTiets, printDialog.PrintableAreaWidth);
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu Xuất - {phieuXuat.MaPhieuXuat}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi in phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static FlowDocument CreatePhieuXuatDocument(PhieuXuat phieuXuat, IEnumerable<CT_PhieuXuat> chiTiets, double pageWidth)
    {
        var document = new FlowDocument
        {
            PageWidth = pageWidth,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        // Tiêu đề
        var headerPara = new Paragraph(new Run("PHIẾU XUẤT KHO"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(headerPara);

        // Tên công ty
        var companyPara = new Paragraph(new Run("PIZZINN - Quản Lý Kho Nguyên Liệu"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };
        document.Blocks.Add(companyPara);

        // Trạng thái
        string trangThaiText = phieuXuat.TrangThai switch
        {
            1 => "Chờ duyệt",
            2 => "Đã xuất kho",
            3 => "Từ chối",
            _ => "Không xác định"
        };

        // Thông tin phiếu
        var infoPara = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        infoPara.Inlines.Add(new Run("Mã phiếu: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.MaPhieuXuat}\n"));
        infoPara.Inlines.Add(new Run("Trạng thái: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{trangThaiText}\n") { Foreground = phieuXuat.TrangThai == 2 ? Brushes.Green : (phieuXuat.TrangThai == 3 ? Brushes.Red : Brushes.Orange) });
        infoPara.Inlines.Add(new Run("Ngày yêu cầu: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.NgayYeuCau:dd/MM/yyyy HH:mm}\n"));
        infoPara.Inlines.Add(new Run("Người yêu cầu: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.NhanVienYeuCau?.HoTen ?? "N/A"}\n"));

        if (phieuXuat.NgayDuyet.HasValue)
        {
            infoPara.Inlines.Add(new Run("Ngày duyệt: ") { FontWeight = FontWeights.Bold });
            infoPara.Inlines.Add(new Run($"{phieuXuat.NgayDuyet:dd/MM/yyyy HH:mm}\n"));
        }
        if (phieuXuat.NhanVienDuyet != null)
        {
            infoPara.Inlines.Add(new Run("Người duyệt: ") { FontWeight = FontWeights.Bold });
            infoPara.Inlines.Add(new Run($"{phieuXuat.NhanVienDuyet.HoTen}\n"));
        }
        document.Blocks.Add(infoPara);

        // Bảng chi tiết
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });  // STT
        table.Columns.Add(new TableColumn { Width = new GridLength(3.0, GridUnitType.Star) }); // Tên nguyên liệu
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) }); // Đơn vị
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) }); // Số lượng

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        // Dòng tiêu đề
        var headerRow = new TableRow { Background = Brushes.LightGray };
        headerRow.Cells.Add(CreateTableCell("STT", true));
        headerRow.Cells.Add(CreateTableCell("Tên nguyên liệu", true));
        headerRow.Cells.Add(CreateTableCell("Đơn vị", true));
        headerRow.Cells.Add(CreateTableCell("Số lượng", true));
        rowGroup.Rows.Add(headerRow);

        // Dòng dữ liệu
        int stt = 1;
        foreach (var ct in chiTiets)
        {
            var row = new TableRow();
            row.Cells.Add(CreateTableCell(stt.ToString()));
            row.Cells.Add(CreateTableCell(ct.NguyenLieu?.TenNguyenLieu ?? "N/A"));
            row.Cells.Add(CreateTableCell(ct.DonViTinh?.TenDonVi ?? ct.NguyenLieu?.DonViTinh?.TenDonVi ?? "N/A"));
            row.Cells.Add(CreateTableCell($"{ct.SoLuong:N2}"));
            rowGroup.Rows.Add(row);
            stt++;
        }

        document.Blocks.Add(table);

        // Chân trang - Chữ ký
        var signatureTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 50, 0, 0) };
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        
        var signatureRowGroup = new TableRowGroup();
        signatureTable.RowGroups.Add(signatureRowGroup);
        
        var signatureRow = new TableRow();
        signatureRow.Cells.Add(CreateSignatureCell("Người yêu cầu"));
        signatureRow.Cells.Add(CreateSignatureCell("Người duyệt"));
        signatureRow.Cells.Add(CreateSignatureCell("Thủ kho"));
        signatureRowGroup.Rows.Add(signatureRow);

        document.Blocks.Add(signatureTable);

        // Ngày in
        var printDatePara = new Paragraph(new Run($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 30, 0, 0)
        };
        document.Blocks.Add(printDatePara);

        return document;
    }
    #endregion

    #region In Hóa Đơn Bán Hàng
    public static void PrintHoaDonBanHang(DonHang donHang, IEnumerable<CT_DonHang> chiTiets)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreateHoaDonBanHangDocument(donHang, chiTiets, printDialog.PrintableAreaWidth);
                var docTitle = donHang.MaDonHang == "TẠM TÍNH" ? "HoaDon - Tam Tinh" : $"HoaDon - {donHang.MaDonHang}";
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, docTitle);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi in hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static FlowDocument CreateHoaDonBanHangDocument(DonHang donHang, IEnumerable<CT_DonHang> chiTiets, double pageWidth)
    {
        var document = new FlowDocument
        {
            PageWidth = pageWidth,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue,
            FontFamily = new FontFamily("Arial"),
            FontSize = 12
        };

        // Tiêu đề
        var headerPara = new Paragraph(new Run("HÓA ĐƠN BÁN HÀNG"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        document.Blocks.Add(headerPara);

        // Tên công ty
        var companyPara = new Paragraph(new Run("PIZZINN - Pizza & Đồ Ăn Nhanh"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5)
        };
        document.Blocks.Add(companyPara);

        // Địa chỉ (placeholder)
        var addressPara = new Paragraph(new Run("Địa chỉ: TP. Hồ Chí Minh"))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 25)
        };
        document.Blocks.Add(addressPara);

        // Đường kẻ ngang
        var linePara = new Paragraph(new Run("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 15)
        };
        document.Blocks.Add(linePara);

        // Thông tin hóa đơn
        var infoPara = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        infoPara.Inlines.Add(new Run("Mã hóa đơn: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{donHang.MaDonHang}\n"));
        infoPara.Inlines.Add(new Run("Ngày bán: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{donHang.NgayTao:dd/MM/yyyy HH:mm}\n"));
        infoPara.Inlines.Add(new Run("Nhân viên: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{donHang.NhanVien?.HoTen ?? "N/A"}\n"));
        infoPara.Inlines.Add(new Run("Phương thức TT: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{donHang.PhuongThucTT}\n"));
        document.Blocks.Add(infoPara);

        // Bảng chi tiết sản phẩm
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });  // STT
        table.Columns.Add(new TableColumn { Width = new GridLength(2.5, GridUnitType.Star) }); // Tên SP
        table.Columns.Add(new TableColumn { Width = new GridLength(0.8, GridUnitType.Star) }); // Size
        table.Columns.Add(new TableColumn { Width = new GridLength(0.6, GridUnitType.Star) }); // SL
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) }); // Đơn giá
        table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) }); // Thành tiền

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        // Dòng tiêu đề
        var headerRow = new TableRow { Background = Brushes.LightGray };
        headerRow.Cells.Add(CreateTableCell("STT", true));
        headerRow.Cells.Add(CreateTableCell("Sản phẩm", true));
        headerRow.Cells.Add(CreateTableCell("Size", true));
        headerRow.Cells.Add(CreateTableCell("SL", true));
        headerRow.Cells.Add(CreateTableCell("Đơn giá", true));
        headerRow.Cells.Add(CreateTableCell("Thành tiền", true));
        rowGroup.Rows.Add(headerRow);

        // Dòng dữ liệu
        int stt = 1;
        foreach (var ct in chiTiets)
        {
            var row = new TableRow();
            row.Cells.Add(CreateTableCell(stt.ToString()));
            row.Cells.Add(CreateTableCell(ct.Pizza?.TenPizza ?? "N/A"));
            row.Cells.Add(CreateTableCell(ct.Pizza?.KichThuoc ?? ""));
            row.Cells.Add(CreateTableCell($"{ct.SoLuong}"));
            row.Cells.Add(CreateTableCell($"{ct.DonGia:N0}đ"));
            row.Cells.Add(CreateTableCell($"{ct.ThanhTien:N0}đ"));
            rowGroup.Rows.Add(row);
            stt++;
        }

        document.Blocks.Add(table);

        // Tổng kết thanh toán
        var summaryPara = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0)
        };
        summaryPara.Inlines.Add(new Run($"Tổng tiền hàng: {donHang.TongTien:N0}đ\n") { FontSize = 13 });
        if (donHang.GiamGia > 0)
        {
            summaryPara.Inlines.Add(new Run($"Giảm giá: -{donHang.GiamGia:N0}đ\n") { FontSize = 13, Foreground = Brushes.Red });
        }
        document.Blocks.Add(summaryPara);

        // Thanh toán
        var totalPara = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 5, 0, 0)
        };
        totalPara.Inlines.Add(new Run("THANH TOÁN: ") { FontWeight = FontWeights.Bold, FontSize = 16 });
        totalPara.Inlines.Add(new Run($"{donHang.ThanhToan:N0} VNĐ") { FontWeight = FontWeights.Bold, FontSize = 16, Foreground = Brushes.Red });
        document.Blocks.Add(totalPara);

        // Tiền khách đưa & Tiền thừa
        if (donHang.TienKhachDua.HasValue && donHang.TienKhachDua.Value > 0)
        {
            var paymentDetailPara = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0)
            };
            paymentDetailPara.Inlines.Add(new Run($"Tiền khách đưa: {donHang.TienKhachDua.Value:N0}đ\n") { FontSize = 13 });
            var tienThua = donHang.TienKhachDua.Value - donHang.ThanhToan;
            if (tienThua > 0)
            {
                paymentDetailPara.Inlines.Add(new Run($"Tiền thừa: {tienThua:N0}đ") { FontSize = 13, Foreground = Brushes.Green });
            }
            document.Blocks.Add(paymentDetailPara);
        }

        // Lời cảm ơn
        var thanksPara = new Paragraph(new Run("Cảm ơn quý khách! Hẹn gặp lại!"))
        {
            FontSize = 12,
            FontStyle = FontStyles.Italic,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 30, 0, 10)
        };
        document.Blocks.Add(thanksPara);

        // Mã QR cho chuyển khoản
        if (donHang.PhuongThucTT != null && donHang.PhuongThucTT.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Tạo VietQR data string
                // Thông tin ngân hàng PizzaInn (Vietcombank mẫu)
                string bankId = "970436"; // Vietcombank BIN
                string accountNo = "1234567890"; // Số tài khoản
                string accountName = "PIZZAINN";
                string amount = ((int)donHang.ThanhToan).ToString();
                string addInfo = $"TT {donHang.MaDonHang}";

                // VietQR URL format (sử dụng VietQR API)
                string qrContent = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={amount}&addInfo={Uri.EscapeDataString(addInfo)}&accountName={Uri.EscapeDataString(accountName)}";

                // Tạo QR code chứa nội dung thanh toán
                string qrData = $"Ngân hàng: Vietcombank\nSTK: {accountNo}\nTên: {accountName}\nSố tiền: {donHang.ThanhToan:N0} VNĐ\nNội dung: {addInfo}";

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrCodeData);
                using var qrBitmap = qrCode.GetGraphic(6);

                // Convert Bitmap to BitmapSource
                using var memory = new MemoryStream();
                qrBitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                // Thêm section QR vào document
                var qrSection = new Paragraph
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 5)
                };

                var qrTitleRun = new Run("━━━ QUÉT MÃ ĐỂ CHUYỂN KHOẢN ━━━\n")
                {
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                qrSection.Inlines.Add(qrTitleRun);

                var qrImage = new Image
                {
                    Source = bitmapImage,
                    Width = 180,
                    Height = 180,
                    Stretch = Stretch.Uniform
                };

                var qrContainer = new InlineUIContainer(qrImage);
                qrSection.Inlines.Add(qrContainer);
                document.Blocks.Add(qrSection);

                // Thông tin tài khoản
                var bankInfoPara = new Paragraph
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                bankInfoPara.Inlines.Add(new Run("Vietcombank - ") { FontSize = 11, FontWeight = FontWeights.Bold });
                bankInfoPara.Inlines.Add(new Run(accountNo + "\n") { FontSize = 11 });
                bankInfoPara.Inlines.Add(new Run(accountName + "\n") { FontSize = 11, FontWeight = FontWeights.Bold });
                bankInfoPara.Inlines.Add(new Run($"Số tiền: {donHang.ThanhToan:N0} VNĐ") { FontSize = 11 });
                document.Blocks.Add(bankInfoPara);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating QR code: {ex.Message}");
            }
        }

        // Ngày in
        var printDatePara = new Paragraph(new Run($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
        {
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        document.Blocks.Add(printDatePara);

        return document;
    }
    #endregion

    #region Phương thức hỗ trợ
    private static TableCell CreateTableCell(string text, bool isHeader = false)
    {
        var cell = new TableCell(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(5)
        })
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(5)
        };

        if (isHeader)
        {
            cell.Background = Brushes.LightGray;
        }

        return cell;
    }

    private static TableCell CreateSignatureCell(string title)
    {
        var cell = new TableCell();
        var para = new Paragraph
        {
            TextAlignment = TextAlignment.Center
        };
        para.Inlines.Add(new Run(title) { FontWeight = FontWeights.Bold });
        para.Inlines.Add(new Run("\n\n\n\n"));
        para.Inlines.Add(new Run("(Ký và ghi rõ họ tên)") { FontStyle = FontStyles.Italic, FontSize = 10 });
        cell.Blocks.Add(para);
        return cell;
    }
    #endregion
}


