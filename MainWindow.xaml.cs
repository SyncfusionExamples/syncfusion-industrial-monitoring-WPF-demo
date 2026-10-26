using System;
using System.Windows;
using Syncfusion.IndustrialMonitoring.POC.ViewModels;
using Syncfusion.SfSkinManager;
using Syncfusion.Licensing;

namespace Syncfusion.IndustrialMonitoring.POC
{
    public partial class MainWindow : Window
    {
        private DashboardViewModel _viewModel;

        public MainWindow()
        {
            // Register Syncfusion license FIRST (required for all controls)
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JBaF5cXGhAd1p/WGJbYl1xZFdFallYTnNWSV9jS3hTcURkWHddeXVXQ2dbVU91Wg==");

            // Enable theme as default style for framework controls
            SfSkinManager.SfSkinManager.ApplyThemeAsDefaultStyle = true;
            // Apply Windows11Light theme to this window and all child controls
            SfSkinManager.SfSkinManager.SetTheme(this, new Theme("Windows11Light"));

            // Set both Culture and UICulture to Arabic so dates/times and UI strings display in Arabic
            var arabicCulture = new System.Globalization.CultureInfo("ar-SA");
            System.Threading.Thread.CurrentThread.CurrentCulture = arabicCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = arabicCulture;
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.Cleanup();
        }
    }
}
