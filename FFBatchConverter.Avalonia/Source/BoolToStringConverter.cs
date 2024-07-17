using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FFBatchConverter.Avalonia;

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; }
    public string FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? TrueValue : FalseValue;
        }

        return FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && s == TrueValue;
    }
}