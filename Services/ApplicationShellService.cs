using System.Diagnostics;
using System.Windows;
using ImageConverterPro.Views;

namespace ImageConverterPro.Services
{
    public sealed class ApplicationShellService : IApplicationShellService
    {
        public bool ConfirmExitDuringConversion() => MessageBox.Show(
            "Conversion is currently running. Exit anyway?",
            "Image Converter Pro",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

        public void RequestExit() => Application.Current.Shutdown();

        public void ShowAbout()
        {
            var dialog = new AboutWindow { Owner = Application.Current.MainWindow };
            dialog.ShowDialog();
        }

        public void OpenWebsite()
        {
            Process.Start(new ProcessStartInfo("https://github.com/", "") { UseShellExecute = true });
        }
    }
}
