using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace TextureHawk.UI.Converters;

/// <summary>
/// Converts processing status strings to appropriate color brushes.
/// </summary>
public sealed class StatusToColorConverter : IValueConverter
{
    public static StatusToColorConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string status)
            return Brushes.Gray;

        return status switch
        {
            "Completed" => new SolidColorBrush(Color.Parse("#4ADE80")),
            "Failed" => new SolidColorBrush(Color.Parse("#EF5350")),
            "Processing" or "Loading" or "Writing" => new SolidColorBrush(Color.Parse("#E91E63")),
            _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
