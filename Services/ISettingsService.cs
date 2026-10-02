using System;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface ISettingsService
    {
        event EventHandler<ApplicationSettings>? SettingsChanged;
        ApplicationSettings LoadSettings();
        void SaveSettings(ApplicationSettings settings, bool notifyChanges = true);
        ConversionSettings LoadLastConversionSettings();
        void SaveLastConversionSettings(ConversionSettings settings);
    }
}
