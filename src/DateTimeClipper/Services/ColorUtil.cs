using System.Windows.Media;

namespace DateTimeClipper.Services;

public static class ColorUtil
{
    /// <summary>"#RRGGBB" 等の文字列を Color に。不正な値は fallback を返す。</summary>
    public static Color ParseOrDefault(string hex, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or NullReferenceException)
        {
            return fallback;
        }
    }
}
