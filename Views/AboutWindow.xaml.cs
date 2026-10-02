using System.Windows;
using ImageConverterPro.Services;

namespace ImageConverterPro.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow() => InitializeComponent();

        private void Website_Click(object sender, RoutedEventArgs e) => ServiceLocator.GetService<IApplicationShellService>().OpenWebsite();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
