using System;
using System.Windows;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public sealed class WindowStateService : IWindowStateService
    {
        private readonly ISettingsService _settingsService;
        public WindowStateService(ISettingsService settingsService) => _settingsService = settingsService;

        public void Restore(Window window)
        {
            var settings = _settingsService.LoadSettings();
            if (!settings.RestorePreviousSession) return;
            var position = settings.LastWindowPosition;

            if (!double.IsNaN(position.Width) && position.Width >= window.MinWidth) window.Width = position.Width;
            if (!double.IsNaN(position.Height) && position.Height >= window.MinHeight) window.Height = position.Height;
            if (!double.IsNaN(position.Left) && !double.IsNaN(position.Top)
                && position.Left < SystemParameters.VirtualScreenWidth && position.Top < SystemParameters.VirtualScreenHeight)
            {
                window.Left = Math.Max(SystemParameters.VirtualScreenLeft, position.Left);
                window.Top = Math.Max(SystemParameters.VirtualScreenTop, position.Top);
            }
            if (position.IsMaximized)
                window.Dispatcher.BeginInvoke(() => window.WindowState = WindowState.Maximized);
        }

        public void Save(Window window)
        {
            var settings = _settingsService.LoadSettings();
            if (!settings.RestorePreviousSession) return;

            var bounds = window.WindowState == WindowState.Normal ? window : null;
            settings.LastWindowPosition = new WindowPosition
            {
                Left = bounds?.Left ?? window.RestoreBounds.Left,
                Top = bounds?.Top ?? window.RestoreBounds.Top,
                Width = bounds?.Width ?? window.RestoreBounds.Width,
                Height = bounds?.Height ?? window.RestoreBounds.Height,
                IsMaximized = window.WindowState == WindowState.Maximized
            };
            _settingsService.SaveSettings(settings, notifyChanges: false);
        }
    }
}
