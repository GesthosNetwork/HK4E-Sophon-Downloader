using System;

namespace Core.Utils
{
    internal class Formatter
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB" };

        public static string FormatSize(double value, int decimalPlaces = 2)
        {
            if (value < 0) return "-" + FormatSize(-value);
            if (value == 0) return "0 B";

            int mag = Math.Min(SizeSuffixes.Length - 1, (int)Math.Log(value, 1024));
            double adjustedSize = value / Math.Pow(1024, mag);

            return $"{Math.Round(adjustedSize, decimalPlaces)} {SizeSuffixes[mag]}";
        }
    }
}
