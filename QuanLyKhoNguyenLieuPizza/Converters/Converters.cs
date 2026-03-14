using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyKhoNguyenLieuPizza.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Support binding to string or to other types (use ToString())
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
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))  // Green
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
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
/// Converts a value to a percentage for chart display
/// Parameter format: "maxValue" or "maxValue,multiplier"
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
/// Converts a value to arc end point for donut chart
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
        
        // Convert angle to radians and calculate end point
        // Starting from top (270 degrees in standard coords, or -90 in our system)
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
/// Converts value to IsLargeArc flag for donut chart (>50%)
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
/// Converts value to bar height for bar charts
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
/// MultiValueConverter for donut chart path data
/// </summary>
public class DonutChartPathConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int currentValue || values[1] is not int maxValue)
            return Geometry.Empty;
        
        if (maxValue == 0 || currentValue == 0) return Geometry.Empty;
        
        double percentage = Math.Min(1.0, (double)currentValue / maxValue);
        if (percentage >= 1.0) percentage = 0.999; // Avoid full circle issue
        
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

