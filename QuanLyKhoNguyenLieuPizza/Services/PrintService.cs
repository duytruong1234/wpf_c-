using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services;

public class PrintService
{
    #region Print Phieu Nhap
    public static void PrintPhieuNhap(PhieuNhap phieuNhap, IEnumerable<CT_PhieuNhap> chiTiets)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreatePhieuNhapDocument(phieuNhap, chiTiets, printDialog.PrintableAreaWidth);
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phi?u Nh?p - {phieuNhap.MaPhieuNhap}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L?i khi in phi?u: {ex.Message}", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Header
        var headerPara = new Paragraph(new Run("PHI?U NH?P KHO"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(headerPara);

        // Company name
        var companyPara = new Paragraph(new Run("PIZZINN - Qu?n L� Kho Nguy�n Li?u"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };
        document.Blocks.Add(companyPara);

        // Thong tin phieu
        var infoPara = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        infoPara.Inlines.Add(new Run("M� phi?u: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.MaPhieuNhap}\n"));
        infoPara.Inlines.Add(new Run("Ng�y nh?p: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NgayNhap:dd/MM/yyyy HH:mm}\n"));
        infoPara.Inlines.Add(new Run("Nh�n vi�n nh?p: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NhanVienNhap?.HoTen ?? "N/A"}\n"));
        infoPara.Inlines.Add(new Run("Nh� cung c?p: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuNhap.NhaCungCap?.TenNCC ?? "N/A"}\n"));
        document.Blocks.Add(infoPara);

        // Bang chi tiet
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });  // STT
        table.Columns.Add(new TableColumn { Width = new GridLength(2.5, GridUnitType.Star) }); // Ten nguyen lieu
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) });  // Don vi
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) });  // So luong
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) }); // Don gia
        table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) }); // Thanh tien

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        // Header row
        var headerRow = new TableRow { Background = Brushes.LightGray };
        headerRow.Cells.Add(CreateTableCell("STT", true));
        headerRow.Cells.Add(CreateTableCell("T�n nguy�n li?u", true));
        headerRow.Cells.Add(CreateTableCell("�on v?", true));
        headerRow.Cells.Add(CreateTableCell("S? lu?ng", true));
        headerRow.Cells.Add(CreateTableCell("�on gi�", true));
        headerRow.Cells.Add(CreateTableCell("Th�nh ti?n", true));
        rowGroup.Rows.Add(headerRow);

        // Data rows
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

        // Tong tien
        var totalPara = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        totalPara.Inlines.Add(new Run("T?NG TI?N: ") { FontWeight = FontWeights.Bold, FontSize = 16 });
        totalPara.Inlines.Add(new Run($"{phieuNhap.TongTien:N0} VN�") { FontWeight = FontWeights.Bold, FontSize = 16, Foreground = Brushes.Red });
        document.Blocks.Add(totalPara);

        // Footer - Chu ky
        var signatureTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 50, 0, 0) };
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        
        var signatureRowGroup = new TableRowGroup();
        signatureTable.RowGroups.Add(signatureRowGroup);
        
        var signatureRow = new TableRow();
        signatureRow.Cells.Add(CreateSignatureCell("Ngu?i l?p phi?u"));
        signatureRow.Cells.Add(CreateSignatureCell("Ngu?i nh?n h�ng"));
        signatureRowGroup.Rows.Add(signatureRow);

        document.Blocks.Add(signatureTable);

        // Print date
        var printDatePara = new Paragraph(new Run($"Ng�y in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
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

    #region Print Phieu Xuat
    public static void PrintPhieuXuat(PhieuXuat phieuXuat, IEnumerable<CT_PhieuXuat> chiTiets)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var document = CreatePhieuXuatDocument(phieuXuat, chiTiets, printDialog.PrintableAreaWidth);
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phi?u Xu?t - {phieuXuat.MaPhieuXuat}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L?i khi in phi?u: {ex.Message}", "L?i", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Header
        var headerPara = new Paragraph(new Run("PHI?U XU?T KHO"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(headerPara);

        // Company name
        var companyPara = new Paragraph(new Run("PIZZINN - Qu?n L� Kho Nguy�n Li?u"))
        {
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };
        document.Blocks.Add(companyPara);

        // Trang thai
        string trangThaiText = phieuXuat.TrangThai switch
        {
            1 => "Ch? duy?t",
            2 => "�� duy?t",
            3 => "T? ch?i",
            _ => "Kh�ng x�c d?nh"
        };

        // Thong tin phieu
        var infoPara = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        infoPara.Inlines.Add(new Run("M� phi?u: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.MaPhieuXuat}\n"));
        infoPara.Inlines.Add(new Run("Tr?ng th�i: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{trangThaiText}\n") { Foreground = phieuXuat.TrangThai == 2 ? Brushes.Green : (phieuXuat.TrangThai == 3 ? Brushes.Red : Brushes.Orange) });
        infoPara.Inlines.Add(new Run("Ng�y y�u c?u: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.NgayYeuCau:dd/MM/yyyy HH:mm}\n"));
        infoPara.Inlines.Add(new Run("Ngu?i y�u c?u: ") { FontWeight = FontWeights.Bold });
        infoPara.Inlines.Add(new Run($"{phieuXuat.NhanVienYeuCau?.HoTen ?? "N/A"}\n"));

        if (phieuXuat.NgayDuyet.HasValue)
        {
            infoPara.Inlines.Add(new Run("Ng�y duy?t: ") { FontWeight = FontWeights.Bold });
            infoPara.Inlines.Add(new Run($"{phieuXuat.NgayDuyet:dd/MM/yyyy HH:mm}\n"));
        }
        if (phieuXuat.NhanVienDuyet != null)
        {
            infoPara.Inlines.Add(new Run("Ngu?i duy?t: ") { FontWeight = FontWeights.Bold });
            infoPara.Inlines.Add(new Run($"{phieuXuat.NhanVienDuyet.HoTen}\n"));
        }
        document.Blocks.Add(infoPara);

        // Bang chi tiet
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });  // STT
        table.Columns.Add(new TableColumn { Width = new GridLength(3.0, GridUnitType.Star) }); // Ten nguyen lieu
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) }); // Don vi
        table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) }); // So luong

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        // Header row
        var headerRow = new TableRow { Background = Brushes.LightGray };
        headerRow.Cells.Add(CreateTableCell("STT", true));
        headerRow.Cells.Add(CreateTableCell("T�n nguy�n li?u", true));
        headerRow.Cells.Add(CreateTableCell("�on v?", true));
        headerRow.Cells.Add(CreateTableCell("S? lu?ng", true));
        rowGroup.Rows.Add(headerRow);

        // Data rows
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

        // Footer - Chu ky
        var signatureTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 50, 0, 0) };
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        
        var signatureRowGroup = new TableRowGroup();
        signatureTable.RowGroups.Add(signatureRowGroup);
        
        var signatureRow = new TableRow();
        signatureRow.Cells.Add(CreateSignatureCell("Ngu?i y�u c?u"));
        signatureRow.Cells.Add(CreateSignatureCell("Ngu?i duy?t"));
        signatureRow.Cells.Add(CreateSignatureCell("Th? kho"));
        signatureRowGroup.Rows.Add(signatureRow);

        document.Blocks.Add(signatureTable);

        // Print date
        var printDatePara = new Paragraph(new Run($"Ng�y in: {DateTime.Now:dd/MM/yyyy HH:mm}"))
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

    #region Helper Methods
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
        para.Inlines.Add(new Run("(K� v� ghi r� h? t�n)") { FontStyle = FontStyles.Italic, FontSize = 10 });
        cell.Blocks.Add(para);
        return cell;
    }
    #endregion
}
