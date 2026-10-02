using System;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageConverterPro.Models
{
    public enum ConversionStatus
    {
        Pending,
        Converting,
        Completed,
        Failed
    }

    public partial class ImageFile : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _extension = string.Empty;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private int _width;

        [ObservableProperty]
        private int _height;

        [ObservableProperty]
        private BitmapSource? _thumbnail;

        [ObservableProperty]
        private ConversionStatus _status = ConversionStatus.Pending;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _outputPath;

        [ObservableProperty]
        private DateTime _addedAt = DateTime.Now;
    }
}
