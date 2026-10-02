using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConverterPro.Models;
using ImageConverterPro.Services;

namespace ImageConverterPro.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly IHistoryRepository _historyRepository;

        [ObservableProperty] private ObservableCollection<HistoryItem> _historyItems = new();
        [ObservableProperty] private HistoryItem? _selectedItem;
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _selectedFormat = "All formats";
        [ObservableProperty] private string _selectedOperation = "All operations";
        [ObservableProperty] private string _selectedDateRange = "All time";
        [ObservableProperty] private bool _isLoading;

        public ICollectionView FilteredHistory { get; }
        public IReadOnlyList<string> Formats { get; } = ["All formats", "PNG", "JPG", "WEBP", "BMP", "TIFF"];
        public IReadOnlyList<string> Operations { get; } = ["All operations", "Conversion", "Editing"];
        public IReadOnlyList<string> DateRanges { get; } = ["All time", "Today", "Last 7 days", "Last 30 days"];
        public int TotalHistoryItems => HistoryItems.Count;

        public HistoryViewModel(IHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
            FilteredHistory = CollectionViewSource.GetDefaultView(HistoryItems);
            FilteredHistory.Filter = MatchesActiveFilters;
            HistoryItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TotalHistoryItems));
        }

        partial void OnSearchQueryChanged(string value) => FilteredHistory.Refresh();
        partial void OnSelectedFormatChanged(string value) => RefreshHistoryInBackground();
        partial void OnSelectedOperationChanged(string value) => FilteredHistory.Refresh();
        partial void OnSelectedDateRangeChanged(string value) => RefreshHistoryInBackground();

        [RelayCommand]
        private async Task LoadHistoryAsync() => await RefreshHistoryAsync();

        [RelayCommand]
        private async Task SearchAsync() => await RefreshHistoryAsync();

        [RelayCommand]
        private async Task ClearHistoryAsync()
        {
            await _historyRepository.ClearHistoryAsync();
            HistoryItems.Clear();
        }

        [RelayCommand]
        private void OpenFile(HistoryItem? item)
        {
            var path = GetExistingFile(item);
            if (path is null) return;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenFolder(HistoryItem? item)
        {
            var path = GetExistingFile(item) ?? item?.OutputFile ?? item?.SourceFile;
            if (string.IsNullOrWhiteSpace(path)) return;
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }

        [RelayCommand]
        private async Task DeleteAsync(HistoryItem? item)
        {
            if (item is null) return;
            await _historyRepository.DeleteHistoryAsync(item.Id, item.RecordType);
            HistoryItems.Remove(item);
        }

        private async Task RefreshHistoryAsync()
        {
            try
            {
                IsLoading = true;
                IReadOnlyList<HistoryItem> items;
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    items = await _historyRepository.SearchHistoryAsync(SearchQuery);
                }
                else if (SelectedDateRange != "All time")
                {
                    var from = SelectedDateRange switch
                    {
                        "Today" => DateTime.Today,
                        "Last 7 days" => DateTime.Today.AddDays(-7),
                        "Last 30 days" => DateTime.Today.AddDays(-30),
                        _ => (DateTime?)null
                    };
                    items = await _historyRepository.FilterByDateAsync(from, null);
                }
                else if (SelectedFormat != "All formats")
                {
                    items = await _historyRepository.FilterByFormatAsync(SelectedFormat);
                }
                else
                {
                    items = await _historyRepository.GetHistoryAsync();
                }

                HistoryItems.Clear();
                foreach (var item in items) HistoryItems.Add(item);
                FilteredHistory.Refresh();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool MatchesActiveFilters(object obj)
        {
            if (obj is not HistoryItem item) return false;
            if (!string.IsNullOrWhiteSpace(SearchQuery) && !MatchesSearch(item, SearchQuery)) return false;
            if (SelectedFormat != "All formats"
                && !string.Equals(item.SourceFormat, SelectedFormat, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.OutputFormat, SelectedFormat, StringComparison.OrdinalIgnoreCase)) return false;
            if (SelectedOperation == "Conversion" && item.RecordType != HistoryRecordType.Conversion) return false;
            if (SelectedOperation == "Editing" && item.RecordType != HistoryRecordType.Editing) return false;
            return MatchesDateRange(item.CreatedDate);
        }

        private bool MatchesDateRange(DateTime date) => SelectedDateRange switch
        {
            "Today" => date >= DateTime.Today,
            "Last 7 days" => date >= DateTime.Today.AddDays(-7),
            "Last 30 days" => date >= DateTime.Today.AddDays(-30),
            _ => true
        };

        private static bool MatchesSearch(HistoryItem item, string search) =>
            item.SourceFile.Contains(search, StringComparison.OrdinalIgnoreCase)
            || (item.OutputFile?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            || item.Operation.Contains(search, StringComparison.OrdinalIgnoreCase)
            || item.Status.Contains(search, StringComparison.OrdinalIgnoreCase);

        private void RefreshHistoryInBackground() => _ = RefreshHistoryAsync();

        private static string? GetExistingFile(HistoryItem? item)
        {
            if (item is null) return null;
            if (!string.IsNullOrWhiteSpace(item.OutputFile) && File.Exists(item.OutputFile)) return item.OutputFile;
            return File.Exists(item.SourceFile) ? item.SourceFile : null;
        }
    }
}
