using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace LEMORA.Services
{
    public static class ThemeService
    {
        private static readonly string ThemeFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LEMORA", "theme.txt");

        public static bool IsDark { get; private set; }

        public static void Initialize()
        {
            bool useDark = false;
            try
            {
                useDark = File.Exists(ThemeFile) &&
                    string.Equals(File.ReadAllText(ThemeFile).Trim(), "Dark", StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            ApplyTheme(useDark, false);
        }

        public static void ApplyTheme(bool dark)
        {
            ApplyTheme(dark, true);
        }

        private static void ApplyTheme(bool dark, bool savePreference)
        {
            var light = new Dictionary<string, string>
            {
                ["PrimaryBrush"] = "#6BB1AD",
                ["SecondaryBrush"] = "#A7BCBD",
                ["BackgroundBrush"] = "#EDECDB",
                ["MelonBrush"] = "#E5A9A9",
                ["AccentBrush"] = "#E6748E",
                ["TextBrush"] = "#243238",
                ["MutedTextBrush"] = "#738184",
                ["CardBrush"] = "#FFFFFF",
                ["BorderBrush"] = "#E4E7DF",
                ["SidebarBrush"] = "#F8F7EE",
                ["HeaderBrush"] = "#F8F7EE",
                ["SurfaceBrush"] = "#F3F1E7",
                ["ElevatedBrush"] = "#F8F7F1",
                ["PreviewBrush"] = "#F6F5EF",
                ["SlotBrush"] = "#EDECDB",
                ["SoftAccentBrush"] = "#EDF4F0",
                ["WarmPanelBrush"] = "#EFF0E4",
                ["DangerBrush"] = "#B2334A"
            };
            var night = new Dictionary<string, string>
            {
                ["PrimaryBrush"] = "#356D6B",
                ["SecondaryBrush"] = "#658E8E",
                ["BackgroundBrush"] = "#151F23",
                ["MelonBrush"] = "#67444E",
                ["AccentBrush"] = "#EB96AA",
                ["TextBrush"] = "#F1F5F3",
                ["MutedTextBrush"] = "#B3C4C5",
                ["CardBrush"] = "#233238",
                ["BorderBrush"] = "#405458",
                ["SidebarBrush"] = "#19272B",
                ["HeaderBrush"] = "#19272B",
                ["SurfaceBrush"] = "#2B3C41",
                ["ElevatedBrush"] = "#304146",
                ["PreviewBrush"] = "#2A393D",
                ["SlotBrush"] = "#324248",
                ["SoftAccentBrush"] = "#30494C",
                ["WarmPanelBrush"] = "#2B3A3E",
                ["DangerBrush"] = "#F19AA5"
            };

            var resources = Application.Current.Resources;
            foreach (var item in dark ? night : light)
            {
                if (resources[item.Key] is not SolidColorBrush brush)
                    throw new InvalidOperationException($"Missing theme brush: {item.Key}");

                var color = (Color)ColorConverter.ConvertFromString(item.Value)!;
                if (brush.IsFrozen)
                    resources[item.Key] = new SolidColorBrush(color);
                else
                    brush.Color = color;
            }

            IsDark = dark;

            if (savePreference)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemeFile)!);
                File.WriteAllText(ThemeFile, dark ? "Dark" : "Light");
            }
        }
    }
}
