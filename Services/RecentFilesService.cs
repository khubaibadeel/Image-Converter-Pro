using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public sealed class RecentFilesService : IRecentFilesService
    {
        private readonly ISettingsService _settingsService;

        public RecentFilesService(ISettingsService settingsService) => _settingsService = settingsService;

        public event EventHandler? RecentFilesChanged;

        public IReadOnlyList<RecentFile> GetRecentFiles() => _settingsService.LoadSettings().RecentFiles
            .OrderByDescending(x => x.LastUsed)
            .Select(Clone)
            .ToList();

        public void AddRecentFile(string filePath, string operationType)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var settings = _settingsService.LoadSettings();
            var now = DateTime.Now;
            var existing = settings.RecentFiles.FirstOrDefault(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                settings.RecentFiles.Add(new RecentFile
                {
                    FilePath = filePath,
                    OperationType = operationType,
                    DateOpened = now,
                    LastUsed = now
                });
            }
            else
            {
                existing.OperationType = operationType;
                existing.LastUsed = now;
            }

            settings.RecentFiles = settings.RecentFiles
                .OrderByDescending(x => x.LastUsed)
                .Take(Math.Clamp(settings.MaximumRecentFiles, 1, 50))
                .ToList();
            _settingsService.SaveSettings(settings, notifyChanges: false);
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveRecentFile(string filePath)
        {
            var settings = _settingsService.LoadSettings();
            settings.RecentFiles.RemoveAll(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            _settingsService.SaveSettings(settings, notifyChanges: false);
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearRecentFiles()
        {
            var settings = _settingsService.LoadSettings();
            settings.RecentFiles.Clear();
            _settingsService.SaveSettings(settings, notifyChanges: false);
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        private static RecentFile Clone(RecentFile item) => new()
        {
            FilePath = item.FilePath,
            OperationType = item.OperationType,
            DateOpened = item.DateOpened,
            LastUsed = item.LastUsed
        };
    }
}
