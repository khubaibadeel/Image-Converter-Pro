using System;
using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConverterPro.Models;
using ImageConverterPro.Services;
using Microsoft.Win32;

namespace ImageConverterPro.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private bool _isLoading;

        public IReadOnlyList<string> AvailableThemes { get; } = ["Light", "Dark", "System"];
        public IReadOnlyList<string> AvailableFormats { get; } = ["PNG", "JPG", "WEBP", "BMP", "TIFF"];
        public string ApplicationVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        public string DeveloperInformation => "Image Converter Pro";

        [ObservableProperty] private string _selectedTheme = "System";
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
        [ObservableProperty] private bool _hasUnsavedChanges;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadSettings();
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (_isLoading) return;
            ApplyTheme();
            PersistSettings();
        }

        partial void OnDefaultOutputFormatChanged(string value) => PersistSettings();
        partial void OnDefaultJpegQualityChanged(int value) => PersistSettings();
        partial void OnDefaultOutputDirectoryChanged(string value) => PersistSettings();
        partial void OnDefaultBackgroundColorChanged(string value) => PersistSettings();
        partial void OnDefaultZoomLevelChanged(double value) => PersistSettings();
        partial void OnThumbnailSizeChanged(int value) => PersistSettings();
        partial void OnMaximumParallelConversionsChanged(int value) => PersistSettings();
        partial void OnCacheSizeMbChanged(int value) => PersistSettings();
        partial void OnRestorePreviousSessionChanged(bool value) => PersistSettings();
        partial void OnOpenOutputAfterConversionChanged(bool value) => PersistSettings();
        partial void OnShowNotificationsChanged(bool value) => PersistSettings();
        partial void OnRememberLastSettingsChanged(bool value) => PersistSettings();
        partial void OnMaximumRecentFilesChanged(int value) => PersistSettings();

        [RelayCommand]
        private void SaveSettings() => PersistSettings();

        [RelayCommand]
        private void ResetToDefaults()
        {
            ApplySettings(new ApplicationSettings());
            ApplyTheme();
            PersistSettings();
        }

        [RelayCommand]
        private void BrowseDefaultOutput()
        {
            var dialog = new OpenFolderDialog { Title = "Select default output folder" };
            if (dialog.ShowDialog() == true) DefaultOutputDirectory = dialog.FolderName;
        }

        private void LoadSettings()
        {
            ApplySettings(_settingsService.LoadSettings());
            ApplyTheme();
            HasUnsavedChanges = false;
        }

        private void ApplySettings(ApplicationSettings settings)
        {
            _isLoading = true;
            SelectedTheme = string.IsNullOrWhiteSpace(settings.Theme) ? "System" : settings.Theme;
            DefaultOutputFormat = string.IsNullOrWhiteSpace(settings.DefaultOutputFormat) ? "PNG" : settings.DefaultOutputFormat;
            DefaultJpegQuality = Math.Clamp(settings.DefaultJpegQuality, 1, 100);
            DefaultOutputDirectory = settings.DefaultOutputDirectory ?? string.Empty;
            DefaultBackgroundColor = string.IsNullOrWhiteSpace(settings.DefaultBackgroundColor) ? "#FFFFFF" : settings.DefaultBackgroundColor;
            DefaultZoomLevel = Math.Clamp(settings.DefaultZoomLevel, 0.25, 4.0);
            ThumbnailSize = Math.Clamp(settings.ThumbnailSize, 64, 512);
            MaximumParallelConversions = Math.Clamp(settings.MaximumParallelConversions, 1, 8);
            CacheSizeMb = Math.Clamp(settings.CacheSizeMb, 0, 4096);
            RestorePreviousSession = settings.RestorePreviousSession;
            OpenOutputAfterConversion = settings.OpenOutputAfterConversion;
            ShowNotifications = settings.ShowNotifications;
            RememberLastSettings = settings.RememberLastSettings;
            MaximumRecentFiles = Math.Clamp(settings.MaximumRecentFiles, 1, 50);
            _isLoading = false;
        }

        private void PersistSettings()
        {
            if (_isLoading) return;
            HasUnsavedChanges = true;
            _settingsService.SaveSettings(new ApplicationSettings
            {
                Theme = SelectedTheme,
                DefaultOutputFormat = DefaultOutputFormat,
                DefaultJpegQuality = Math.Clamp(DefaultJpegQuality, 1, 100),
                DefaultOutputDirectory = DefaultOutputDirectory,
                DefaultBackgroundColor = DefaultBackgroundColor,
                DefaultZoomLevel = Math.Clamp(DefaultZoomLevel, 0.25, 4.0),
                ThumbnailSize = Math.Clamp(ThumbnailSize, 64, 512),
                MaximumParallelConversions = Math.Clamp(MaximumParallelConversions, 1, 8),
                CacheSizeMb = Math.Clamp(CacheSizeMb, 0, 4096),
                RestorePreviousSession = RestorePreviousSession,
                OpenOutputAfterConversion = OpenOutputAfterConversion,
                ShowNotifications = ShowNotifications,
                RememberLastSettings = RememberLastSettings,
                MaximumRecentFiles = Math.Clamp(MaximumRecentFiles, 1, 50)
            });
            HasUnsavedChanges = false;
        }

        private void ApplyTheme() => App.SwitchTheme(string.Equals(SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase));
    }
}
