using System.Globalization;
using System.Windows;
using Syncfusion.IndustrialMonitoring.POC.Services;
using Syncfusion.SfSkinManager;
using Syncfusion.Licensing;

namespace Syncfusion.IndustrialMonitoring.POC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register Syncfusion license FIRST (before any controls are created)

            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JBaF5cXGhAd1p/WGJbYl1xZFdFallYTnNWSV9jS3hTcURkWHddeXVXQ2dbVU91Wg==");

            // Set default culture to Arabic (for date/time formatting, number formatting, etc.)
            var arabicCulture = new CultureInfo("ar-SA");
            CultureInfo.DefaultThreadCurrentCulture = arabicCulture;
            CultureInfo.DefaultThreadCurrentUICulture = arabicCulture;

            // Enable theme as default style for framework controls
            SfSkinManager.SfSkinManager.ApplyThemeAsDefaultStyle = true;
    
            base.OnStartup(e);
            
            // Initialize database on app startup
            DatabaseInitializer.Initialize();
        }
    }
}
