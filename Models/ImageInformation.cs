namespace ImageConverterPro.Models
{
    public sealed class ImageInformation
    {
        public string FilePath { get; init; } = string.Empty;
        public int Width { get; init; }
        public int Height { get; init; }
        public string Format { get; init; } = "Unknown";
        public long FileSize { get; init; }
        public int ColorDepth { get; init; }
        public bool HasAlpha { get; init; }
        public bool HasMetadata { get; init; }

        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string Resolution => Width > 0 && Height > 0 ? $"{Width} × {Height}" : "Unknown";
        public string FileSizeDisplay => FileSize < 1024 * 1024
            ? $"{FileSize / 1024d:0.0} KB"
            : $"{FileSize / (1024d * 1024d):0.00} MB";
        public string ColorModeDisplay => ColorDepth > 0 ? $"{ColorDepth}-bit {(HasAlpha ? "RGBA" : "RGB")}" : "Unknown";
        public string TransparencyDisplay => HasAlpha ? "Yes (Alpha)" : "No (Opaque)";
        public string MetadataAvailability => HasMetadata ? "Available" : "None";
    }

    public sealed class QualityComparison
    {
        public long OriginalFileSize { get; init; }
        public int OriginalWidth { get; init; }
        public int OriginalHeight { get; init; }
        public long EstimatedFileSize { get; init; }
        public int EstimatedWidth { get; init; }
        public int EstimatedHeight { get; init; }

        public double CompressionPercentage => OriginalFileSize <= 0
            ? 0
            : Math.Round((1d - (double)EstimatedFileSize / OriginalFileSize) * 100d, 1);

        public string OriginalSizeDisplay => FormatSize(OriginalFileSize);
        public string EstimatedSizeDisplay => FormatSize(EstimatedFileSize);
        public string OriginalDimensions => $"{OriginalWidth} x {OriginalHeight}";
        public string EstimatedDimensions => $"{EstimatedWidth} x {EstimatedHeight}";

        private static string FormatSize(long bytes) => bytes < 1024 * 1024
            ? $"{bytes / 1024d:0.0} KB"
            : $"{bytes / (1024d * 1024d):0.00} MB";
    }

    public sealed class OptimizationPlan
    {
        public int Quality { get; init; }
        public bool ResizeEnabled { get; init; }
        public int ResizeWidth { get; init; }
        public int ResizeHeight { get; init; }
        public ResizeMode ResizeMode { get; init; }
        public bool MaintainAspectRatio { get; init; }
    }
}
