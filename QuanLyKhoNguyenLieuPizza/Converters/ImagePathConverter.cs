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

            // Kiểm tra nếu là đường dẫn tương đối bắt đầu bằng Resources
            if (imagePath.StartsWith("Resources", StringComparison.OrdinalIgnoreCase))
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var absolutePath = Path.Combine(basePath, imagePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(absolutePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                    bitmap.EndInit();
                    return bitmap;
                }
            }

            // Kiểm tra nếu là đường dẫn tương đối bắt đầu bằng /
            if (imagePath.StartsWith("/"))
            {
                // Chuyển thành đường dẫn tuyệt đối
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var absolutePath = Path.Combine(basePath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(absolutePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                    bitmap.EndInit();
                    return bitmap;
                }
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

