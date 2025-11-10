using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TodoListApp.Converters;

public class OverdueBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOverdue && isOverdue)
        {
            return new SolidColorBrush(Color.Parse("#E53E3E")); // Red background for overdue
        }
        return new SolidColorBrush(Color.Parse("#101D42")); // Dark blue background normal
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class OverdueTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOverdue && isOverdue)
        {
            return new SolidColorBrush(Color.Parse("#FFFFFF")); // White text for overdue
        }
        return new SolidColorBrush(Color.Parse("#89D2DC")); // Cyan text normal
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrWhiteSpace(str);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullableDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is DateTime;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
