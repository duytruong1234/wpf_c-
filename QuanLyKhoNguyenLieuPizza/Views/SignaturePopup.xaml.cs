using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace QuanLyKhoNguyenLieuPizza.Views;

/// <summary>
/// Popup chữ ký số — Quản lý vẽ tay hoặc tải ảnh chữ ký khi duyệt phiếu nhập/xuất.
/// Trả về Base64 PNG qua property SignatureBase64.
/// </summary>
public partial class SignaturePopup : Window
{
    /// <summary>
    /// Kết quả chữ ký dạng Base64 PNG. Null nếu hủy.
    /// </summary>
    public string? SignatureBase64 { get; private set; }

    /// <summary>
    /// Ảnh chữ ký được tải từ file (nếu có). Ưu tiên hơn InkCanvas.
    /// </summary>
    private BitmapImage? _loadedImage;

    public SignaturePopup(string maPhieu, string nguoiDuyet)
    {
        InitializeComponent();

        txtMaPhieu.Text = $"Mã phiếu: {maPhieu}";
        txtNguoiDuyet.Text = $"Người duyệt: {nguoiDuyet}";

        // Ẩn placeholder khi bắt đầu vẽ
        inkCanvas.StrokeCollected += (s, e) =>
        {
            txtPlaceholder.Visibility = Visibility.Collapsed;
        };

        // Cho phép kéo cửa sổ
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.OriginalSource != inkCanvas)
                DragMove();
        };
    }

    /// <summary>
    /// Ẩn placeholder ngay khi người dùng nhấn chuột vào vùng vẽ
    /// </summary>
    private void InkCanvas_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        txtPlaceholder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Xóa toàn bộ chữ ký đã vẽ
    /// </summary>
    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        inkCanvas.Strokes.Clear();
        _loadedImage = null;
        txtPlaceholder.Visibility = Visibility.Visible;

        // Reset InkCanvas mode (xóa ảnh nếu có)
        inkCanvas.Children.Clear();
        inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
    }

    /// <summary>
    /// Tải ảnh chữ ký từ file (PNG/JPG)
    /// </summary>
    private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Chọn ảnh chữ ký",
            Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openDialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _loadedImage = bitmap;

                // Hiển thị ảnh trên InkCanvas
                inkCanvas.Strokes.Clear();
                inkCanvas.Children.Clear();
                txtPlaceholder.Visibility = Visibility.Collapsed;

                var imageControl = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    Width = inkCanvas.ActualWidth > 0 ? inkCanvas.ActualWidth - 20 : 460,
                    Height = inkCanvas.ActualHeight > 0 ? inkCanvas.ActualHeight - 20 : 180
                };

                InkCanvas.SetLeft(imageControl, 10);
                InkCanvas.SetTop(imageControl, 10);
                inkCanvas.Children.Add(imageControl);

                // Tạm tắt vẽ tay khi đã tải ảnh
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
            catch (Exception ex) {
                MessageBox.Show($"Không thể tải ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Xác nhận duyệt — chuyển chữ ký thành Base64 PNG
    /// </summary>
    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {

        // Kiểm tra có chữ ký không
        if (_loadedImage == null && inkCanvas.Strokes.Count == 0)
        {
            MessageBox.Show("Vui lòng ký tên hoặc tải ảnh chữ ký trước khi xác nhận!",
                "Chưa có chữ ký", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_loadedImage != null)
            {
                // Chuyển ảnh đã tải thành Base64
                SignatureBase64 = BitmapImageToBase64(_loadedImage);
            }
            else
            {
                // Chuyển InkCanvas strokes thành Base64 PNG
                SignatureBase64 = InkCanvasToBase64();
            }

            this.DialogResult = true;
        }
        catch (Exception ex) {
            MessageBox.Show($"Lỗi xử lý chữ ký: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Hủy — đóng popup không duyệt
    /// </summary>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Chuyển InkCanvas strokes thành Base64 PNG
    /// </summary>
    private string InkCanvasToBase64()
    {
        // Lấy bounds của strokes
        var bounds = inkCanvas.Strokes.GetBounds();
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return string.Empty;

        // Render InkCanvas ra bitmap
        int width = (int)inkCanvas.ActualWidth;
        int height = (int)inkCanvas.ActualHeight;

        var renderBitmap = new RenderTargetBitmap(
            width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(inkCanvas);

        // Encode PNG
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Chuyển BitmapImage thành Base64 PNG
    /// </summary>
    private static string BitmapImageToBase64(BitmapImage bitmapImage)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Chuyển Base64 string thành BitmapImage (dùng cho hiển thị chữ ký đã lưu)
    /// </summary>
    public static BitmapImage? Base64ToBitmapImage(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(base64);
            var bitmap = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
