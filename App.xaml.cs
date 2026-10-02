using System;
using System.Linq;
using System.Windows;

using ImageConverterPro.Data;
using ImageConverterPro.Services;
using ImageConverterPro.ViewModels;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ImageConverterPro
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global UI exception handler
            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    args.Exception.ToString(),
                    "UI Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                args.Handled = true;
            };

            // Global non-UI exception handler
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    args.ExceptionObject?.ToString() ?? "Unknown application error.",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            try
            {
                var services = new ServiceCollection();

                ConfigureServices(services);

                Services = services.BuildServiceProvider();

                // Create/verify SQLite database.
                using (var dbContext = Services
                    .GetRequiredService<IDbContextFactory<ImageConverterDbContext>>()
                    .CreateDbContext())
                {
                    dbContext.Database.EnsureCreated();
                }

                // Initialize service locator used by the existing window/view code.
                ServiceLocator.Initialize(Services);

                // Load application settings.
                var settings = Services
                    .GetRequiredService<ISettingsService>()
                    .LoadSettings();

                // Apply selected theme without removing SharedStyles.xaml.
                SwitchTheme(
                    string.Equals(
                        settings.Theme,
                        "Dark",
                        StringComparison.OrdinalIgnoreCase));

                // Create main window.
                var mainWindow = new MainWindow();

                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Application Startup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Image services
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IImageOptimizationService, ImageOptimizationService>();
            services.AddSingleton<IImageInformationService, ImageInformationService>();
            services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            services.AddSingleton<IImageEditingService, ImageEditingService>();

            // Database
            services.AddDbContextFactory<ImageConverterDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={DatabasePaths.DatabasePath}"));

            services.AddSingleton<IHistoryRepository, HistoryRepository>();

            // Application services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRecentFilesService, RecentFilesService>();
            services.AddSingleton<IShortcutService, ShortcutService>();
            services.AddSingleton<IWindowStateService, WindowStateService>();
            services.AddSingleton<IApplicationShellService, ApplicationShellService>();

            // View models
            services.AddTransient<MainViewModel>();
            services.AddTransient<ConvertViewModel>();
            services.AddTransient<EditViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<SettingsViewModel>();
        }

        public static void SwitchTheme(bool isDarkTheme)
        {
            var themeName = isDarkTheme
                ? "DarkTheme"
                : "LightTheme";

            var newThemeUri = new Uri(
                $"/Themes/{themeName}.xaml",
                UriKind.Relative);

            // IMPORTANT:
            // Do not clear MergedDictionaries here.
            // SharedStyles.xaml must stay loaded.
            var dictionaries = Current.Resources.MergedDictionaries;

            var currentTheme = dictionaries.FirstOrDefault(dictionary =>
            {
                var source = dictionary.Source?.OriginalString;

                return !string.IsNullOrWhiteSpace(source) &&
                       (source.EndsWith("/Themes/LightTheme.xaml",
                           StringComparison.OrdinalIgnoreCase) ||
                        source.EndsWith("/Themes/DarkTheme.xaml",
                           StringComparison.OrdinalIgnoreCase));
            });

            var newTheme = new ResourceDictionary
            {
                Source = newThemeUri
            };

            if (currentTheme != null)
            {
                var index = dictionaries.IndexOf(currentTheme);
                dictionaries[index] = newTheme;
            }
            else
            {
                // Keep the theme before SharedStyles.xaml so dynamic theme resources
                // are available to all shared styles.
                dictionaries.Insert(0, newTheme);
            }
        }
    }
}
