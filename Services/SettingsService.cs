using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly object _syncRoot = new();
        private readonly string _settingsFilePath;
        private readonly string _lastConversionSettingsFilePath;
        private readonly string _appDataFolder;

        public SettingsService(string? appDataFolder = null)
        {
            _appDataFolder = appDataFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageConverterPro");
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }
            _settingsFilePath = Path.Combine(_appDataFolder, "settings.json");
            _lastConversionSettingsFilePath = Path.Combine(_appDataFolder, "last_conversion_settings.json");
        }

        public event EventHandler<ApplicationSettings>? SettingsChanged;

        public ApplicationSettings LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<ApplicationSettings>(json, SettingsJsonOptions) ?? new ApplicationSettings();
                }
                catch
                {
                    // If error loading, return new default settings
                }
            }
            return new ApplicationSettings();
        }

        public void SaveSettings(ApplicationSettings settings, bool notifyChanges = true)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, SettingsJsonOptions);
                lock (_syncRoot)
                {
                    var temporaryPath = $"{_settingsFilePath}.tmp";
                    File.WriteAllText(temporaryPath, json);
                    File.Move(temporaryPath, _settingsFilePath, true);
                }
                if (notifyChanges) SettingsChanged?.Invoke(this, settings);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public ConversionSettings LoadLastConversionSettings()
        {
            if (File.Exists(_lastConversionSettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_lastConversionSettingsFilePath);
                    return JsonSerializer.Deserialize<ConversionSettings>(json) ?? new ConversionSettings();
                }
                catch
                {
                    // If error loading, return new default settings
                }
            }
            return new ConversionSettings();
        }

        public void SaveLastConversionSettings(ConversionSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_lastConversionSettingsFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private static JsonSerializerOptions SettingsJsonOptions { get; } = new()
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
    }
}
