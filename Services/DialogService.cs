using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Keeps file pickers and message boxes out of the view models so they stay testable.
    /// </summary>
    public interface IDialogService
    {
        string? ShowSaveFileDialog(string title, string filter, string defaultFileName);
        bool Confirm(string message, string title);
        void ShowError(string message, string title);
        void OpenFile(string filePath);
    }

    public class DialogService : IDialogService
    {
        public string? ShowSaveFileDialog(string title, string filter, string defaultFileName)
        {
            var dialog = new SaveFileDialog { Title = title, Filter = filter, FileName = defaultFileName };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public bool Confirm(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.Yes, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == MessageBoxResult.Yes;

        public void ShowError(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error,
                MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

        public void OpenFile(string filePath) =>
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }
}
