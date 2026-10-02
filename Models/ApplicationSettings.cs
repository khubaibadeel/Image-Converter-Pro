using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace ImageConverterPro.Models
{
    public class WindowPosition
    {
        public double Left { get; set; } = double.NaN;
        public double Top { get; set; } = double.NaN;
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 600;
        public bool IsMaximized { get; set; }
    }

    public partial class ApplicationSettings : ObservableObject
    {
        [ObservableProperty] private string _theme = "System";
        [ObservableProperty] private string _defaultOutputFormat = "PNG";
        [ObservableProperty] private int _defaultJpegQuality = 90;
        [ObservableProperty] private string _defaultOutputDirectory = string.Empty;
        [ObservableProperty] private string _defaultBackgroundColor = "#FFFFFF";
        [ObservableProperty] private double _defaultZoomLevel = 1.0;
        [ObservableProperty] private int _thumbnailSize = 200;
        [ObservableProperty] private int _maximumParallelConversions = 2;
        [ObservableProperty] private int _cacheSizeMb = 256;
        [ObservableProperty] private bool _restorePreviousSession = true;
        [ObservableProperty] private bool _openOutputAfterConversion;
        [ObservableProperty] private bool _showNotifications = true;
        [ObservableProperty] private bool _rememberLastSettings = true;
        [ObservableProperty] private int _maximumRecentFiles = 10;
        [ObservableProperty] private List<RecentFile> _recentFiles = new();
        [ObservableProperty] private WindowPosition _lastWindowPosition = new();
    }

    public sealed class RecentFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public DateTime DateOpened { get; set; } = DateTime.Now;
        public DateTime LastUsed { get; set; } = DateTime.Now;
        public string DisplayName => System.IO.Path.GetFileName(FilePath);
    }
}
