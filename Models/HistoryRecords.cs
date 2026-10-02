using System;
using System.IO;

namespace ImageConverterPro.Models
{
    public sealed class ConversionHistoryRecord
    {
        public string SourceFile { get; init; } = string.Empty;
        public string? OutputFile { get; init; }
        public string SourceFormat { get; init; } = string.Empty;
        public string OutputFormat { get; init; } = string.Empty;
        public long OriginalSize { get; init; }
        public long OutputSize { get; init; }
        public int Quality { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public DateTime CreatedDate { get; init; } = DateTime.Now;
        public string Status { get; init; } = "Completed";
        public string? ErrorDetails { get; init; }
    }

    public sealed class EditingHistoryRecord
    {
        public string SourceFile { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public string Operations { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; } = DateTime.Now;
    }

    public enum HistoryRecordType
    {
        Conversion,
        Editing
    }

    public sealed class HistoryItem
    {
        public int Id { get; init; }
        public HistoryRecordType RecordType { get; init; }
        public string SourceFile { get; init; } = string.Empty;
        public string? OutputFile { get; init; }
        public string Operation { get; init; } = string.Empty;
        public string? SourceFormat { get; init; }
        public string? OutputFormat { get; init; }
        public long OriginalSize { get; init; }
        public long OutputSize { get; init; }
        public DateTime CreatedDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? ErrorDetails { get; init; }

        public string FileName => Path.GetFileName(SourceFile);
        public string OutputFileName => !string.IsNullOrWhiteSpace(OutputFile) ? Path.GetFileName(OutputFile) : "—";
        public string OriginalSizeDisplay => OriginalSize > 0 ? FormatSize(OriginalSize) : "—";
        public string OutputSizeDisplay => OutputSize > 0 ? FormatSize(OutputSize) : "—";
        public string SizeDescription => OutputSize > 0
            ? $"{FormatSize(OriginalSize)} → {FormatSize(OutputSize)}"
            : OriginalSize > 0 ? FormatSize(OriginalSize) : "—";

        private static string FormatSize(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB"];
            double size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.##} {units[unit]}";
        }
    }
}
