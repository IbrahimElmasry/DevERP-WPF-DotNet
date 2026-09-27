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
                InvoiceStatus.Paid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                InvoiceStatus.Sent => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                InvoiceStatus.Draft => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                InvoiceStatus.Overdue => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
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
                InvoiceStatus.Paid => new SolidColorBrush(Color.FromArgb(90, 6, 78, 59)),
                InvoiceStatus.Sent => new SolidColorBrush(Color.FromArgb(90, 30, 58, 138)),
                InvoiceStatus.Draft => new SolidColorBrush(Color.FromArgb(70, 51, 65, 85)),
                InvoiceStatus.Overdue => new SolidColorBrush(Color.FromArgb(90, 127, 29, 29)),
                _ => new SolidColorBrush(Color.FromArgb(50, 100, 116, 139))
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
                InvoiceStatus.Sent => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                InvoiceStatus.Draft => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                InvoiceStatus.Overdue => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
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
