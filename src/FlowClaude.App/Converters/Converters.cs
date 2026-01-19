using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FlowClaude.App.ViewModels;
using FlowClaude.Core.Entities;

namespace FlowClaude.App.Converters;

/// <summary>
/// Converts enum to boolean for radio button binding
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            var enumType = targetType.IsGenericType 
                ? targetType.GetGenericArguments()[0] 
                : targetType;
            return Enum.Parse(enumType, parameter.ToString() ?? "");
        }
        return Enum.Parse(targetType, "0");
    }
}

/// <summary>
/// Converts boolean to brush for highlighting
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        var mode = (parameter as string ?? "").ToLower();
        
        return mode switch
        {
            "selected" => isTrue ? Application.Current?.FindResource("PrimaryColor") as Avalonia.Media.Brush 
                                 : Avalonia.Media.Brushes.Transparent,
            "plan" => isTrue ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFB74D")) 
                             : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1A1A1A")),
            "error" => isTrue ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F44336")) 
                              : Avalonia.Media.Brushes.Transparent,
            _ => isTrue ? Application.Current?.FindResource("PrimaryColor") as Avalonia.Media.Brush 
                        : Avalonia.Media.Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Avalonia.Media.IBrush brush && brush != Avalonia.Media.Brushes.Transparent;
    }
}

/// <summary>
/// Converts boolean to opposite boolean
/// </summary>
public class BoolToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = (parameter as string ?? "").ToLower() == "invert";
        return !(value is true) == invert;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = (parameter as string ?? "").ToLower() == "invert";
        return !(value is true) == invert;
    }
}

/// <summary>
/// Converts number to boolean
/// </summary>
public class NumberToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = value as int?;
        var invert = (parameter as string ?? "").ToLower() == "invert";
        return (number ?? 0) > 0 == !invert;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return 0;
    }
}

/// <summary>
/// Converts boolean to theme icon
/// </summary>
public class BoolToThemeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isDark = value is true;
        // Return appropriate icon path
        return isDark 
            ? PathGeometry.Parse("M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9c0-.46-.04-.92-.1-1.36-.98 1.37-2.58 2.26-4.4 2.26-2.98 0-5.4-2.42-5.4-5.4 0-1.81.89-3.42 2.26-4.4-.44-.06-.9-.1-1.36-.1z")
            : PathGeometry.Parse("M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.79 1.42-1.41zM4 10.5H1v2h3v-2zm9-9.95h-2V3.5h2V.55zm7.45 3.91l-1.41-1.41-1.79 1.79 1.41 1.41 1.79-1.79zm-3.21 13.7l1.79 1.8 1.41-1.41-1.8-1.79-1.4 1.4zM20 10.5v2h3v-2h-3z");
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts MessageRole to brush
/// </summary>
public class RoleToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0078D4")),
                MessageRole.Assistant => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50")),
                MessageRole.System => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF9800")),
                _ => Avalonia.Media.Brushes.Gray
            };
        }
        return Avalonia.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return MessageRole.User;
    }
}

/// <summary>
/// Converts MessageRole to icon
/// </summary>
public class RoleToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => PathGeometry.Parse("M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"),
                MessageRole.Assistant => PathGeometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-2.21 0-4 1.79-4 4h2c0-1.1.9-2 2-2s2 .9 2 2c0 2-3 1.75-3 5h2c0-2.25 3-2.5 3-5 0-2.21-1.79-4-4-4z"),
                MessageRole.System => PathGeometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"),
                _ => new PathGeometry()
            };
        }
        return new PathGeometry();
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return MessageRole.User;
    }
}

/// <summary>
/// Converts MessageRole to text
/// </summary>
public class RoleToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => "You",
                MessageRole.Assistant => "FlowClaude",
                MessageRole.System => "System",
                _ => role.ToString()
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return MessageRole.User;
    }
}

/// <summary>
/// Converts string to boolean
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return "";
    }
}

/// <summary>
/// Checks if collection contains item
/// </summary>
public class ContainsConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is System.Collections.IEnumerable collection && values[1] != null)
        {
            return collection.Cast<object>().Contains(values[1]);
        }
        return false;
    }
}

/// <summary>
/// Checks equality
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            return values[0]?.Equals(values[1]) ?? false;
        }
        return false;
    }
}

/// <summary>
/// Converts GitFileStatus to brush
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChangeStatus status)
        {
            return status switch
            {
                ChangeStatus.Added => new SolidColorBrush(Color.Parse("#4CAF50")),
                ChangeStatus.Modified => new SolidColorBrush(Color.Parse("#2196F3")),
                ChangeStatus.Deleted => new SolidColorBrush(Color.Parse("#F44336")),
                ChangeStatus.Renamed => new SolidColorBrush(Color.Parse("#9C27B0")),
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ChangeStatus.Untracked;
    }
}
