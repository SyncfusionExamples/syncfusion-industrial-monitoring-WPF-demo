using System.Windows.Controls;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;

namespace Syncfusion.IndustrialMonitoring.POC.Views
{
    public partial class AlarmGridView : UserControl
    {
        public AlarmGridView()
        {
            InitializeComponent();
        }

        private void OnExportExcelClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "تصدير بيانات الإنذارات إلى Excel",
                Filter = "ملف Excel (*.xlsx)|*.xlsx",
                FileName = "سجل_الإنذارات.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                var options = new ExcelExportingOptions
                {
                    ExcelVersion = ExcelVersion.Excel2013
                };

                var excelEngine = AlarmGrid.ExportToExcel(AlarmGrid.View, options);
                var workBook = excelEngine.Excel.Workbooks[0];
                workBook.SaveAs(dialog.FileName);
            }
        }
    }
}
