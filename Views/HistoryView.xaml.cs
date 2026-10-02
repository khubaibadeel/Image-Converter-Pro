using System.Windows;
using System.Windows.Controls;
using ImageConverterPro.ViewModels;

namespace ImageConverterPro.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel viewModel && viewModel.LoadHistoryCommand.CanExecute(null))
            {
                viewModel.LoadHistoryCommand.Execute(null);
            }
        }
    }
}
