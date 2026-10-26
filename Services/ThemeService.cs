using System.Windows;
using System.Windows.Media;
using Syncfusion.SfSkinManager;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Switches between the Syncfusion Windows11Light and Windows11Dark themes.
    /// SfSkinManager styles the Syncfusion and framework controls; the few dashboard
    /// surface brushes in App.xaml are swapped alongside it.
    /// </summary>
    public static class ThemeService
    {
        public static void Apply(Window window, bool dark)
        {
            SfSkinManager.SfSkinManager.SetTheme(window, new Theme(dark ? "Windows11Dark" : "Windows11Light"));

            var resources = Application.Current.Resources;
            resources["PageBackground"] = Brush(dark ? "#1C1C1C" : "#F5F5F5");
            resources["CardBackground"] = Brush(dark ? "#2B2B2B" : "#FFFFFF");
            resources["CardBorder"] = Brush(dark ? "#3D3D3D" : "#E0E0E0");
            resources["TextPrimary"] = Brush(dark ? "#F2F2F2" : "#1A1A1A");
            resources["TextSecondary"] = Brush(dark ? "#B0B0B0" : "#666666");
        }

        private static SolidColorBrush Brush(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}
