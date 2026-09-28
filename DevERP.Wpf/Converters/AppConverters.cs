using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevERP.Core.Enums;

namespace DevERP.Desktop.Converters;

public class InvoiceStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Paid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399")),     // Vibrant Emerald
                InvoiceStatus.Sent => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),     // Crisp Cyan / Sky
                InvoiceStatus.Draft => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),    // Crisp Slate
                InvoiceStatus.Overdue => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB7185")),  // Rose Red
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InvoiceStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Paid => new SolidColorBrush(Color.FromArgb(50, 6, 95, 70)),      // Emerald tint
                InvoiceStatus.Sent => new SolidColorBrush(Color.FromArgb(50, 14, 116, 144)),    // Sky tint
                InvoiceStatus.Draft => new SolidColorBrush(Color.FromArgb(40, 51, 65, 85)),    // Slate tint
                InvoiceStatus.Overdue => new SolidColorBrush(Color.FromArgb(50, 159, 18, 57)),  // Rose tint
                _ => new SolidColorBrush(Color.FromArgb(30, 100, 116, 139))
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InvoiceStatusToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Paid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
                InvoiceStatus.Sent => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7")),
                InvoiceStatus.Draft => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                InvoiceStatus.Overdue => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TransactionTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Inflow => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399")),
                TransactionType.Outflow => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB7185")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TransactionTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Inflow => new SolidColorBrush(Color.FromArgb(90, 6, 78, 59)),
                TransactionType.Outflow => new SolidColorBrush(Color.FromArgb(90, 76, 5, 25)),
                _ => new SolidColorBrush(Color.FromArgb(40, 30, 41, 59))
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TransactionTypeToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Inflow => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
                TransactionType.Outflow => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TransactionTypeToPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type == TransactionType.Inflow ? "+ " : "- ";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ProjectStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Active => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                ProjectStatus.Completed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                ProjectStatus.OnHold => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        bool invert = parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
        {
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return new SolidColorBrush(Colors.Transparent);
        bool matches = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return matches ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152238")) : new SolidColorBrush(Colors.Transparent);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return new SolidColorBrush(Colors.Transparent);
        bool matches = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return matches ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#223554")) : new SolidColorBrush(Colors.Transparent);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

