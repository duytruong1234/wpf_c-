using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QuanLyKhoNguyenLieuPizza.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return null;

        var imagePath = value.ToString();

        try
        {
            // Kiểm tra nếu là URL
            if (imagePath!.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri(imagePath));
            }

            // Sử dụng helper để phân giải đường dẫn ảnh từ AppData, BaseDirectory, hoặc đường dẫn tuyệt đối
            var absolutePath = Helpers.ImageStorageHelper.ResolveImagePath(imagePath);

            if (absolutePath != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                bitmap.EndInit();
                return bitmap;
            }

            // Kiểm tra nếu là đường dẫn tuyệt đối
            if (File.Exists(imagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                return bitmap;
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

