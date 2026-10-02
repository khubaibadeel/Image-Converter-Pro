using System;

namespace ImageConverterPro.Data.Entities
{
    public sealed class ConversionHistoryEntity
    {
        public int Id { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public string? OutputFile { get; set; }
        public string SourceFormat { get; set; } = string.Empty;
        public string OutputFormat { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public long OutputSize { get; set; }
        public int Quality { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
    }
}
