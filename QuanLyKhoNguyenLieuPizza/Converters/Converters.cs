using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyKhoNguyenLieuPizza.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Hỗ trợ binding với string hoặc các kiểu khác (dùng ToString())
        var str = value as string;
        bool isEmpty = string.IsNullOrWhiteSpace(str);
        bool inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);

        if (inverse)
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;

        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
        
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool inverse = parameter as string == "Inverse";
        
        if (inverse)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

public class StringLengthToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int length)
        {
            return length > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue 
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))  // Xanh lá
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Đỏ
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Chuyển đổi giá trị thành phần trăm cho hiển thị biểu đồ
/// Định dạng tham số: "giaTri_toiDa" hoặc "giaTri_toiDa,heSoNhan"
/// </summary>
public class ValueToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue) return 0.0;
        
        double maxValue = 100;
        double multiplier = 1;
        
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (parts.Length >= 1 && double.TryParse(parts[0], out double max))
                maxValue = max;
            if (parts.Length >= 2 && double.TryParse(parts[1], out double mult))
                multiplier = mult;
        }
        
        if (maxValue == 0) return 0.0;
        return Math.Min(100, (intValue / maxValue) * 100 * multiplier);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Chuyển đổi giá trị thành điểm cuối cung cho biểu đồ donut
/// </summary>
public class ValueToArcConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue) return new Point(50, 5);
        
        double maxValue = 100;
        if (parameter is string paramStr && double.TryParse(paramStr, out double max))
            maxValue = max;
        
        double percentage = maxValue > 0 ? Math.Min(1.0, (double)intValue / maxValue) : 0;
        double angle = percentage * 360;
        
        // Chuyển đổi góc sang radians và tính điểm cuối
        // Bắt đầu từ trên cùng (270 độ trong toạ độ chuẩn, hoặc -90 trong hệ của chúng ta)
        double radians = (angle - 90) * Math.PI / 180;
        double radius = 45;
        double centerX = 50;
        double centerY = 50;
        
        double endX = centerX + radius * Math.Cos(radians);
        double endY = centerY + radius * Math.Sin(radians);
        
        return new Point(endX, endY);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Chuyển đổi giá trị thành cờ IsLargeArc cho biểu đồ donut (>50%)
/// </summary>
public class ValueToLargeArcConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue) return false;
        
        double maxValue = 100;
        if (parameter is string paramStr && double.TryParse(paramStr, out double max))
            maxValue = max;
        
        double percentage = maxValue > 0 ? (double)intValue / maxValue : 0;
        return percentage > 0.5;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Chuyển đổi giá trị thành chiều cao thanh cho biểu đồ cột
/// </summary>
public class ValueToBarHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue) return 20.0;
        
        double maxHeight = 150;
        double maxValue = 100;
        
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (parts.Length >= 1 && double.TryParse(parts[0], out double mh))
                maxHeight = mh;
            if (parts.Length >= 2 && double.TryParse(parts[1], out double mv))
                maxValue = mv;
        }
        
        if (maxValue == 0) return 20.0;
        return Math.Max(20, (intValue / maxValue) * maxHeight);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// MultiValueConverter cho dữ liệu đường dẫn biểu đồ donut
/// </summary>
/// <summary>
/// So sánh hai giá trị chuỗi để kiểm tra bằng nhau (dùng cho trạng thái active của sidebar)
/// </summary>
public class StringEqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        var str1 = values[0]?.ToString();
        var str2 = values[1]?.ToString();
        return string.Equals(str1, str2, StringComparison.Ordinal);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DonutChartPathConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int currentValue || values[1] is not int maxValue)
            return Geometry.Empty;
        
        if (maxValue == 0 || currentValue == 0) return Geometry.Empty;
        
        double percentage = Math.Min(1.0, (double)currentValue / maxValue);
        if (percentage >= 1.0) percentage = 0.999; // Tránh lỗi vòng tròn đầy
        
        double angle = percentage * 360;
        double radians = (angle - 90) * Math.PI / 180;
        
        double radius = 40;
        double centerX = 50;
        double centerY = 50;
        
        double startX = centerX;
        double startY = centerY - radius;
        
        double endX = centerX + radius * Math.Cos(radians);
        double endY = centerY + radius * Math.Sin(radians);
        
        bool isLargeArc = percentage > 0.5;
        
        string pathData = $"M {startX.ToString(CultureInfo.InvariantCulture)},{startY.ToString(CultureInfo.InvariantCulture)} " +
                         $"A {radius.ToString(CultureInfo.InvariantCulture)},{radius.ToString(CultureInfo.InvariantCulture)} 0 " +
                         $"{(isLargeArc ? "1" : "0")} 1 " +
                         $"{endX.ToString(CultureInfo.InvariantCulture)},{endY.ToString(CultureInfo.InvariantCulture)}";
        
        return Geometry.Parse(pathData);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Chuyển đổi string thành bool bằng cách so sánh với ConverterParameter (dùng cho RadioButton binding)
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue && parameter is string paramStr)
            return string.Equals(strValue, paramStr, StringComparison.Ordinal);
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string paramStr)
            return paramStr;
        return Binding.DoNothing;
    }
}

