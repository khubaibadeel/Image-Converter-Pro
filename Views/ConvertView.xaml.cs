using System.Windows;
using System.Windows.Controls;
using ImageConverterPro.ViewModels;

namespace ImageConverterPro.Views
{
    public partial class ConvertView : UserControl
    {
        public ConvertView()
        {
            InitializeComponent();
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (DataContext is ConvertViewModel viewModel)
                {
                    viewModel.AddFiles(files);
                }
                var border = sender as Border;
                if (border != null)
                {
                    border.Background = FindResource("DragDropBackground") as System.Windows.Media.Brush;
                }
            }
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                var border = sender as Border;
                if (border != null)
                {
                    border.Background = FindResource("DragDropBackground") as System.Windows.Media.Brush;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = FindResource("DragDropBackground") as System.Windows.Media.Brush;
            }
        }
    }
}
