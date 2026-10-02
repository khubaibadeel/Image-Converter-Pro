using System;

namespace ImageConverterPro.Data.Entities
{
    public sealed class EditingHistoryEntity
    {
        public int Id { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public string Operations { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
