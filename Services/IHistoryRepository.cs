using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IHistoryRepository
    {
        Task SaveConversionHistoryAsync(ConversionHistoryRecord record, CancellationToken cancellationToken = default);
        Task SaveEditingHistoryAsync(EditingHistoryRecord record, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistoryItem>> SearchHistoryAsync(string searchText, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistoryItem>> FilterByDateAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistoryItem>> FilterByFormatAsync(string format, CancellationToken cancellationToken = default);
        Task DeleteHistoryAsync(int id, HistoryRecordType recordType, CancellationToken cancellationToken = default);
        Task ClearHistoryAsync(CancellationToken cancellationToken = default);
    }
}
