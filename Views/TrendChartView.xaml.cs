using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Syncfusion.IndustrialMonitoring.POC.Views
{
    public partial class TrendChartView : UserControl
    {
        public TrendChartView()
        {
            InitializeComponent();
        }

        private void OnExportImageClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "تصدير الرسم البياني كصورة",
                Filter = "صورة PNG (*.png)|*.png|صورة JPEG (*.jpg)|*.jpg|صورة Bitmap (*.bmp)|*.bmp",
                FileName = "مخطط_الاتجاه.png"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = dialog.FilterIndex switch
                {
                    1 => new PngBitmapEncoder(),
                    2 => new JpegBitmapEncoder(),
                    _ => new BmpBitmapEncoder()
                };

                // Render the chart at a fixed, full resolution so the entire
                // chart (including axis labels) is captured instead of being
                // cropped to the current visible size.
                const int exportWidth = 1280;
                const int exportHeight = 720;

                var renderTarget = new RenderTargetBitmap(
                    exportWidth, exportHeight, 96, 96, PixelFormats.Pbgra32);

                // Force a full measure/arrange pass at the export size so the
                // chart lays out all of its content before being captured.
                TrendChart.Measure(new Size(exportWidth, exportHeight));
                TrendChart.Arrange(new Rect(0, 0, exportWidth, exportHeight));
                TrendChart.UpdateLayout();

                renderTarget.Render(TrendChart);

                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }
    }
}
