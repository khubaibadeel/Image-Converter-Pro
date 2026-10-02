using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageConverterPro.Data.Entities;
using ImageConverterPro.Models;
using ImageConverterPro.Services;
using Microsoft.EntityFrameworkCore;

namespace ImageConverterPro.Data
{
    public sealed class HistoryRepository : IHistoryRepository
    {
        private readonly IDbContextFactory<ImageConverterDbContext> _dbContextFactory;

        public HistoryRepository(IDbContextFactory<ImageConverterDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task SaveConversionHistoryAsync(ConversionHistoryRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            db.ConversionHistory.Add(new ConversionHistoryEntity
            {
                SourceFile = record.SourceFile,
                OutputFile = record.OutputFile,
                SourceFormat = record.SourceFormat,
                OutputFormat = record.OutputFormat,
                OriginalSize = record.OriginalSize,
                OutputSize = record.OutputSize,
                Quality = record.Quality,
                Width = record.Width,
                Height = record.Height,
                CreatedDate = record.CreatedDate,
                Status = record.Status,
                ErrorDetails = record.ErrorDetails
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveEditingHistoryAsync(EditingHistoryRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            db.EditingHistory.Add(new EditingHistoryEntity
            {
                SourceFile = record.SourceFile,
                OutputFile = record.OutputFile,
                Operations = record.Operations,
                CreatedDate = record.CreatedDate
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await GetHistoryItemsAsync(db, cancellationToken);
        }

        public async Task<IReadOnlyList<HistoryItem>> SearchHistoryAsync(string searchText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return await GetHistoryAsync(cancellationToken);

            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedSearch = searchText.Trim().ToLower();
            var conversionRecords = await db.ConversionHistory.AsNoTracking()
                .Where(x => x.SourceFile.ToLower().Contains(normalizedSearch)
                    || (x.OutputFile != null && x.OutputFile.ToLower().Contains(normalizedSearch))
                    || x.SourceFormat.ToLower().Contains(normalizedSearch)
                    || x.OutputFormat.ToLower().Contains(normalizedSearch)
                    || x.Status.ToLower().Contains(normalizedSearch))
                .ToListAsync(cancellationToken);
            var editingRecords = await db.EditingHistory.AsNoTracking()
                .Where(x => x.SourceFile.ToLower().Contains(normalizedSearch)
                    || x.OutputFile.ToLower().Contains(normalizedSearch)
                    || x.Operations.ToLower().Contains(normalizedSearch))
                .ToListAsync(cancellationToken);
            return Combine(conversionRecords, editingRecords);
        }

        public async Task<IReadOnlyList<HistoryItem>> FilterByDateAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var conversionQuery = db.ConversionHistory.AsNoTracking().AsQueryable();
            var editingQuery = db.EditingHistory.AsNoTracking().AsQueryable();
            if (from.HasValue)
            {
                conversionQuery = conversionQuery.Where(x => x.CreatedDate >= from.Value);
                editingQuery = editingQuery.Where(x => x.CreatedDate >= from.Value);
            }
            if (to.HasValue)
            {
                conversionQuery = conversionQuery.Where(x => x.CreatedDate <= to.Value);
                editingQuery = editingQuery.Where(x => x.CreatedDate <= to.Value);
            }
            return Combine(await conversionQuery.ToListAsync(cancellationToken), await editingQuery.ToListAsync(cancellationToken));
        }

        public async Task<IReadOnlyList<HistoryItem>> FilterByFormatAsync(string format, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(format)) return await GetHistoryAsync(cancellationToken);

            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedFormat = format.Trim().ToUpperInvariant();
            var conversionRecords = await db.ConversionHistory.AsNoTracking()
                .Where(x => x.SourceFormat == normalizedFormat || x.OutputFormat == normalizedFormat)
                .ToListAsync(cancellationToken);
            return Combine(conversionRecords, []);
        }

        public async Task DeleteHistoryAsync(int id, HistoryRecordType recordType, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            if (recordType == HistoryRecordType.Conversion)
            {
                var record = await db.ConversionHistory.FindAsync([id], cancellationToken);
                if (record is not null) db.ConversionHistory.Remove(record);
            }
            else
            {
                var record = await db.EditingHistory.FindAsync([id], cancellationToken);
                if (record is not null) db.EditingHistory.Remove(record);
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await db.ConversionHistory.ExecuteDeleteAsync(cancellationToken);
            await db.EditingHistory.ExecuteDeleteAsync(cancellationToken);
        }

        private static async Task<IReadOnlyList<HistoryItem>> GetHistoryItemsAsync(ImageConverterDbContext db, CancellationToken cancellationToken)
        {
            return Combine(
                await db.ConversionHistory.AsNoTracking().ToListAsync(cancellationToken),
                await db.EditingHistory.AsNoTracking().ToListAsync(cancellationToken));
        }

        private static IReadOnlyList<HistoryItem> Combine(IEnumerable<ConversionHistoryEntity> conversions, IEnumerable<EditingHistoryEntity> edits)
        {
            return conversions.Select(x => new HistoryItem
                {
                    Id = x.Id,
                    RecordType = HistoryRecordType.Conversion,
                    SourceFile = x.SourceFile,
                    OutputFile = x.OutputFile,
                    Operation = $"{x.SourceFormat} → {x.OutputFormat}",
                    SourceFormat = x.SourceFormat,
                    OutputFormat = x.OutputFormat,
                    OriginalSize = x.OriginalSize,
                    OutputSize = x.OutputSize,
                    CreatedDate = x.CreatedDate,
                    Status = x.Status,
                    ErrorDetails = x.ErrorDetails
                })
                .Concat(edits.Select(x => new HistoryItem
                {
                    Id = x.Id,
                    RecordType = HistoryRecordType.Editing,
                    SourceFile = x.SourceFile,
                    OutputFile = x.OutputFile,
                    Operation = x.Operations,
                    CreatedDate = x.CreatedDate,
                    Status = "Completed"
                }))
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }
    }
}
