using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConverterPro.Models;
using ImageConverterPro.Services;

namespace ImageConverterPro.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IRecentFilesService _recentFilesService;
        private readonly IApplicationShellService _applicationShellService;

        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private ObservableCollection<RecentFile> _recentFiles = new();

        public ConvertViewModel ConvertViewModel { get; }
        public EditViewModel EditViewModel { get; }
        public HistoryViewModel HistoryViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public MainViewModel(
            ConvertViewModel convertViewModel,
            EditViewModel editViewModel,
            HistoryViewModel historyViewModel,
            SettingsViewModel settingsViewModel,
            IRecentFilesService recentFilesService,
            IApplicationShellService applicationShellService)
        {
            ConvertViewModel = convertViewModel;
            EditViewModel = editViewModel;
            HistoryViewModel = historyViewModel;
            SettingsViewModel = settingsViewModel;
            _recentFilesService = recentFilesService;
            _applicationShellService = applicationShellService;
            _recentFilesService.RecentFilesChanged += (_, _) => ReloadRecentFiles();
            ReloadRecentFiles();
        }

        [RelayCommand] private void SwitchToConvert() => SelectedTabIndex = 0;
        [RelayCommand] private void SwitchToEdit() => SelectedTabIndex = 1;
        [RelayCommand] private void SwitchToHistory() => SelectedTabIndex = 2;
        [RelayCommand] private void SwitchToSettings() => SelectedTabIndex = 3;

        [RelayCommand]
        private void OpenFiles()
        {
            if (SelectedTabIndex == 1)
            {
                EditViewModel.OpenImageCommand.Execute(null);
                return;
            }
            ConvertViewModel.AddFilesCommand.Execute(null);
        }

        [RelayCommand]
        private void SaveCurrent()
        {
            if (SelectedTabIndex == 1 && EditViewModel.SaveAsCommand.CanExecute(null))
                EditViewModel.SaveAsCommand.Execute(null);
        }

        [RelayCommand]
        private void Undo()
        {
            if (SelectedTabIndex == 1 && EditViewModel.UndoCommand.CanExecute(null))
                EditViewModel.UndoCommand.Execute(null);
        }

        [RelayCommand]
        private void Redo()
        {
            if (SelectedTabIndex == 1 && EditViewModel.RedoCommand.CanExecute(null))
                EditViewModel.RedoCommand.Execute(null);
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (SelectedTabIndex == 0 && ConvertViewModel.SelectedFile is not null
                && ConvertViewModel.RemoveFileCommand.CanExecute(ConvertViewModel.SelectedFile))
            {
                ConvertViewModel.RemoveFileCommand.Execute(ConvertViewModel.SelectedFile);
            }
        }

        [RelayCommand]
        private async Task OpenRecentFileAsync(RecentFile? recentFile)
        {
            if (recentFile is null) return;
            if (!File.Exists(recentFile.FilePath))
            {
                _recentFilesService.RemoveRecentFile(recentFile.FilePath);
                return;
            }

            if (string.Equals(recentFile.OperationType, "Edit", StringComparison.OrdinalIgnoreCase))
            {
                SelectedTabIndex = 1;
                await EditViewModel.OpenImageFileAsync(recentFile.FilePath);
            }
            else
            {
                SelectedTabIndex = 0;
                ConvertViewModel.AddFiles([recentFile.FilePath]);
            }
        }

        [RelayCommand] private void ClearRecentFiles() => _recentFilesService.ClearRecentFiles();
        [RelayCommand] private void ShowAbout() => _applicationShellService.ShowAbout();
        [RelayCommand] private void ExitApplication() => _applicationShellService.RequestExit();

        private void ReloadRecentFiles()
        {
            RecentFiles.Clear();
            foreach (var file in _recentFilesService.GetRecentFiles()) RecentFiles.Add(file);
        }
    }
}
