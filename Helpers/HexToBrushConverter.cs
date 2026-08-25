using System;
using System.Collections.Concurrent;
using Microsoft.UI.Xaml.Media;

namespace Kil0bitSystemMonitor.Helpers
{
    /// <summary>
    /// Converts an "#AARRGGBB" / "#RRGGBB" hex string into a SolidColorBrush.
    /// Null/empty values fall back to the parameter color (or dark gray) so that
    /// "unset" per-section colors render like the WPF DataTrigger version did.
    /// </summary>
    public class HexToBrushConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        private static readonly ConcurrentDictionary<string, SolidColorBrush> BrushCache = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string hex = value as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(hex))
            {
                hex = parameter as string ?? "#333333";
            }

            var brush = BrushCache.GetOrAdd(hex, h =>
            {
                var color = ParseHex(h);
                return new SolidColorBrush(color);
            });
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        internal static Windows.UI.Color ParseHex(string hex)
        {
            try
            {
                hex = hex.Trim().TrimStart('#');
                byte ByteAt(int i) => System.Convert.ToByte(hex.Substring(i, 2), 16);
                if (hex.Length == 8) return Windows.UI.Color.FromArgb(ByteAt(0), ByteAt(2), ByteAt(4), ByteAt(6));
                if (hex.Length == 6) return Windows.UI.Color.FromArgb(255, ByteAt(0), ByteAt(2), ByteAt(4));
            }
            catch { }
            return Microsoft.UI.Colors.White;
        }
    }
}
