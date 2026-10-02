using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ImageConverterPro.Models;
using ImageConverterPro.Services;

namespace ImageConverterPro.ViewModels
{
    public partial class ConvertViewModel : ObservableObject
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IHistoryRepository _historyRepository;
        private readonly ISettingsService _settingsService;
        private readonly IRecentFilesService _recentFilesService;
        private readonly IImageInformationService _imageInformationService;
        private readonly IImageOptimizationService _imageOptimizationService;
        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _analysisCancellationTokenSource;
        private CancellationTokenSource? _convertedPreviewCancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<ImageFile> _files = new();

        [ObservableProperty]
        private bool _hasFiles;

        [ObservableProperty]
        private int _queueCount;

        [ObservableProperty]
        private ImageFile? _selectedFile;

        [ObservableProperty]
        private BitmapSource? _originalPreview;

        [ObservableProperty]
        private BitmapSource? _convertedPreview;

        [ObservableProperty]
        private bool _isLoadingPreview;

        [ObservableProperty]
        private double _previewZoom = 1d;

        [ObservableProperty]
        private OutputFormat _targetFormat;

        public IEnumerable<OutputFormat> AvailableFormats => Enum.GetValues<OutputFormat>();

        public IEnumerable<MetadataHandling> AvailableMetadataHandling => Enum.GetValues<MetadataHandling>();

        public IEnumerable<OptimizationPreset> AvailableOptimizationPresets => Enum.GetValues<OptimizationPreset>();

        [ObservableProperty]
        private int _quality = 90;

        [ObservableProperty]
        private bool _resizeEnabled;

        [ObservableProperty]
        private int _resizeWidth = 1920;

        [ObservableProperty]
        private int _resizeHeight = 1080;

        [ObservableProperty]
        private bool _maintainAspectRatio = true;

        [ObservableProperty]
        private string _backgroundColor = "White";

        [ObservableProperty]
        private string _outputDirectory = string.Empty;

        [ObservableProperty]
        private MetadataHandling _metadataHandling = MetadataHandling.Keep;

        [ObservableProperty]
        private OptimizationPreset _optimizationPreset = OptimizationPreset.None;

        [ObservableProperty]
        private ImageInformation? _selectedImageInformation;

        [ObservableProperty]
        private QualityComparison? _qualityComparison;

        [ObservableProperty]
        private bool _isAnalyzingImage;

        [ObservableProperty]
        private double _overallProgress;

        [ObservableProperty]
        private string _progressText = string.Empty;

        [ObservableProperty]
        private bool _isConverting;

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private int _completedFiles;

        [ObservableProperty]
        private int _failedFiles;

        public ConvertViewModel(
            IImageProcessingService imageProcessingService,
            IHistoryRepository historyRepository,
            ISettingsService settingsService,
            IRecentFilesService recentFilesService,
            IImageInformationService imageInformationService,
            IImageOptimizationService imageOptimizationService)
        {
            _imageProcessingService = imageProcessingService;
            _historyRepository = historyRepository;
            _settingsService = settingsService;
            _recentFilesService = recentFilesService;
            _imageInformationService = imageInformationService;
            _imageOptimizationService = imageOptimizationService;
            var applicationSettings = _settingsService.LoadSettings();
            ApplyApplicationSettings(applicationSettings);
            RestoreLastConversionSettings(applicationSettings);
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        partial void OnSelectedFileChanged(ImageFile? value)
        {
            if (value != null)
            {
                _ = LoadSelectedFileAsync(value);
            }
            else
            {
                OriginalPreview = null;
                ConvertedPreview = null;
                SelectedImageInformation = null;
                QualityComparison = null;
            }
        }

        private async Task LoadSelectedFileAsync(ImageFile file)
        {
            IsLoadingPreview = true;
            try
            {
                var originalTask = _imageProcessingService.LoadImageAsync(file.FilePath);
                var thumbnailTask = file.Thumbnail is null
                    ? _imageProcessingService.LoadThumbnailAsync(file.FilePath)
                    : Task.FromResult(file.Thumbnail);

                await Task.WhenAll(originalTask, thumbnailTask);

                // Do not let a slower prior selection overwrite the current one.
                if (!ReferenceEquals(SelectedFile, file)) return;

                OriginalPreview = await originalTask;
                file.Thumbnail ??= await thumbnailTask;
                file.Width = OriginalPreview.PixelWidth;
                file.Height = OriginalPreview.PixelHeight;

                await RefreshImageAnalysisAsync();
                await RefreshConvertedPreviewAsync();
            }
            catch
            {
                if (ReferenceEquals(SelectedFile, file))
                {
                    OriginalPreview = null;
                    ConvertedPreview = null;
                }
            }
            finally
            {
                if (ReferenceEquals(SelectedFile, file))
                {
                    IsLoadingPreview = false;
                }
            }
        }

        private bool _isUpdatingDimensions;

        partial void OnTargetFormatChanged(OutputFormat value) => RefreshOutputArtifacts();
        partial void OnQualityChanged(int value) => RefreshOutputArtifacts();
        partial void OnResizeEnabledChanged(bool value) => RefreshOutputArtifacts();
        
        partial void OnResizeWidthChanged(int value)
        {
            if (!_isUpdatingDimensions && MaintainAspectRatio && SelectedFile != null && SelectedFile.Width > 0 && SelectedFile.Height > 0)
            {
                _isUpdatingDimensions = true;
                try
                {
                    double ratio = (double)SelectedFile.Height / SelectedFile.Width;
                    ResizeHeight = Math.Max(1, (int)Math.Round(value * ratio));
                }
                finally
                {
                    _isUpdatingDimensions = false;
                }
            }
            RefreshOutputArtifacts();
        }

        partial void OnResizeHeightChanged(int value)
        {
            if (!_isUpdatingDimensions && MaintainAspectRatio && SelectedFile != null && SelectedFile.Width > 0 && SelectedFile.Height > 0)
            {
                _isUpdatingDimensions = true;
                try
                {
                    double ratio = (double)SelectedFile.Width / SelectedFile.Height;
                    ResizeWidth = Math.Max(1, (int)Math.Round(value * ratio));
                }
                finally
                {
                    _isUpdatingDimensions = false;
                }
            }
            RefreshOutputArtifacts();
        }

        partial void OnMaintainAspectRatioChanged(bool value) => RefreshOutputArtifacts();
        partial void OnBackgroundColorChanged(string value) => RefreshOutputArtifacts();
        partial void OnMetadataHandlingChanged(MetadataHandling value) => RefreshOutputArtifacts();
        partial void OnOptimizationPresetChanged(OptimizationPreset value) => RefreshOutputArtifacts();

        private void RefreshOutputArtifacts()
        {
            _ = RefreshImageAnalysisAsync();
            _ = RefreshConvertedPreviewAsync();
        }

        private async Task RefreshImageAnalysisAsync()
        {
            var selected = SelectedFile;
            if (selected is null || !File.Exists(selected.FilePath))
            {
                return;
            }

            _analysisCancellationTokenSource?.Cancel();
            var source = new CancellationTokenSource();
            _analysisCancellationTokenSource = source;
            var cancellationToken = source.Token;
            IsAnalyzingImage = true;

            try
            {
                await Task.Delay(50, cancellationToken);
                var settings = CreateConversionSettings();
                var informationTask = _imageInformationService.GetInformationAsync(selected.FilePath, cancellationToken);
                var comparisonTask = _imageOptimizationService.EstimateAsync(selected.FilePath, settings, cancellationToken);
                await Task.WhenAll(informationTask, comparisonTask);

                if (!cancellationToken.IsCancellationRequested && ReferenceEquals(SelectedFile, selected))
                {
                    SelectedImageInformation = await informationTask;
                    QualityComparison = await comparisonTask;
                    selected.Width = SelectedImageInformation.Width;
                    selected.Height = SelectedImageInformation.Height;
                }
            }
            catch (OperationCanceledException)
            {
                // A newer selection or setting superseded this estimate.
            }
            catch
            {
                if (!cancellationToken.IsCancellationRequested && ReferenceEquals(SelectedFile, selected))
                {
                    SelectedImageInformation = null;
                    QualityComparison = null;
                }
            }
            finally
            {
                if (ReferenceEquals(_analysisCancellationTokenSource, source))
                {
                    IsAnalyzingImage = false;
                    _analysisCancellationTokenSource = null;
                }
                source.Dispose();
            }
        }

        private async Task RefreshConvertedPreviewAsync()
        {
            var selected = SelectedFile;
            if (selected is null || !File.Exists(selected.FilePath))
            {
                ConvertedPreview = null;
                return;
            }

            _convertedPreviewCancellationTokenSource?.Cancel();
            var source = new CancellationTokenSource();
            _convertedPreviewCancellationTokenSource = source;
            var token = source.Token;
            IsLoadingPreview = true;

            try
            {
                await Task.Delay(80, token);
                var preview = await _imageProcessingService.CreateConvertedPreviewAsync(
                    selected.FilePath,
                    CreateConversionSettings(),
                    token);

                if (!token.IsCancellationRequested && ReferenceEquals(SelectedFile, selected))
                {
                    ConvertedPreview = preview;
                }
            }
            catch (OperationCanceledException)
            {
                // A more recent setting or selected image superseded this render.
            }
            catch
            {
                if (!token.IsCancellationRequested && ReferenceEquals(SelectedFile, selected))
                {
                    ConvertedPreview = null;
                }
            }
            finally
            {
                if (ReferenceEquals(_convertedPreviewCancellationTokenSource, source))
                {
                    IsLoadingPreview = false;
                    _convertedPreviewCancellationTokenSource = null;
                }
                source.Dispose();
            }
        }

        [RelayCommand]
        private void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.webp|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                AddFiles(dialog.FileNames);
            }
        }

        [RelayCommand]
        private void AddFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder Containing Images"
            };

            if (dialog.ShowDialog() == true)
            {
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp"
                };

                var files = Directory.GetFiles(dialog.FolderName, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => extensions.Contains(Path.GetExtension(f)))
                    .ToArray();

                if (files.Length > 0)
                {
                    AddFiles(files);
                }
            }
        }

        public void AddFiles(string[] paths)
        {
            var addedFiles = new List<ImageFile>();
            foreach (var path in paths)
            {
                if (!Files.Any(f => f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    var fileInfo = new FileInfo(path);
                    var imageFile = new ImageFile
                    {
                        FilePath = path,
                        FileName = fileInfo.Name,
                        Extension = fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                        FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                        AddedAt = DateTime.Now
                    };
                    Files.Add(imageFile);
                    addedFiles.Add(imageFile);
                    _recentFilesService.AddRecentFile(path, "Convert");
                }
            }
            UpdateQueueState();

            if (string.IsNullOrWhiteSpace(OutputDirectory) && addedFiles.Count > 0)
            {
                OutputDirectory = Path.GetDirectoryName(addedFiles[0].FilePath) ?? string.Empty;
            }

            if (SelectedFile is null && addedFiles.Count > 0)
            {
                SelectedFile = addedFiles[0];
            }

            foreach (var imageFile in addedFiles)
            {
                _ = LoadQueueThumbnailAsync(imageFile);
            }
        }

        private async Task LoadQueueThumbnailAsync(ImageFile file)
        {
            try
            {
                var thumbnail = await _imageProcessingService.LoadThumbnailAsync(file.FilePath);
                file.Thumbnail = thumbnail;
            }
            catch
            {
                // An unreadable queue item remains visible and will report its conversion error.
            }
        }

        [RelayCommand]
        private void RemoveFile(ImageFile? file)
        {
            if (file != null)
            {
                var removedIndex = Files.IndexOf(file);
                Files.Remove(file);
                if (ReferenceEquals(SelectedFile, file))
                {
                    SelectedFile = Files.ElementAtOrDefault(Math.Min(removedIndex, Files.Count - 1));
                }
                UpdateQueueState();
            }
        }

        [RelayCommand]
        private void ClearFiles()
        {
            Files.Clear();
            SelectedFile = null;
            TotalFiles = 0;
            CompletedFiles = 0;
            FailedFiles = 0;
            OverallProgress = 0;
            ProgressText = string.Empty;
            UpdateQueueState();
        }

        [RelayCommand]
        private void BrowseOutput()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Output Directory"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputDirectory = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                System.Windows.MessageBox.Show("Output folder is empty.", "Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{OutputDirectory}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open output directory:\n{ex.Message}", "Directory Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenConvertedFile()
        {
            string? targetPath = SelectedFile?.OutputPath;
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
            {
                if (SelectedFile != null && !string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    string ext = TargetFormat.ToString().ToLowerInvariant();
                    string baseName = Path.GetFileNameWithoutExtension(SelectedFile.FilePath);
                    string expectedPath = Path.Combine(OutputDirectory, $"{baseName}.{ext}");
                    if (File.Exists(expectedPath)) targetPath = expectedPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(targetPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Unable to open file:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("The converted file is not available on disk yet. Please run 'Convert All' or 'Save As'.", "File Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private async Task SaveAsConvertedFileAsync()
        {
            if (SelectedFile == null || !File.Exists(SelectedFile.FilePath))
            {
                System.Windows.MessageBox.Show("Please select an image to save.", "No Image Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            string originalName = Path.GetFileNameWithoutExtension(SelectedFile.FilePath);
            string ext = TargetFormat.ToString().ToLowerInvariant();

            var dialog = new SaveFileDialog
            {
                Title = "Save Converted Image As",
                FileName = $"{originalName}.{ext}",
                Filter = "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG (*.png)|*.png|WebP (*.webp)|*.webp|BMP (*.bmp)|*.bmp|TIFF (*.tiff)|*.tiff|All Files (*.*)|*.*",
                DefaultExt = ext
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var settings = CreateConversionSettings();
                    settings.OutputDirectory = Path.GetDirectoryName(dialog.FileName) ?? OutputDirectory;
                    
                    string outputPath = await _imageProcessingService.ConvertImageAsync(SelectedFile.FilePath, settings);
                    
                    if (File.Exists(outputPath) && !outputPath.Equals(dialog.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(outputPath, dialog.FileName, true);
                        outputPath = dialog.FileName;
                    }

                    SelectedFile.OutputPath = outputPath;
                    SelectedFile.Status = ConversionStatus.Completed;
                    await RecordHistoryAsync(SelectedFile, outputPath, "Completed", null);

                    System.Windows.MessageBox.Show($"Converted image saved successfully to:\n{outputPath}", "Save Successful", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to save converted image:\n{ex.Message}", "Save Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanConvertAll))]
        private async Task ConvertAllAsync()
        {
            if (Files.Count == 0) return;

            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                System.Windows.MessageBox.Show("Output directory is empty. Please select a destination folder.", "Output Folder Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to create output directory:\n{ex.Message}\n\nPlease choose a valid directory with write permissions.", "Directory Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var applicationSettings = _settingsService.LoadSettings();
            if (applicationSettings.RememberLastSettings)
            {
                _settingsService.SaveLastConversionSettings(CreateConversionSettings());
            }

            IsConverting = true;
            CompletedFiles = 0;
            FailedFiles = 0;
            OverallProgress = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;

            try
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var file = Files[i];
                    file.Status = ConversionStatus.Converting;
                    ProgressText = $"Converting {i + 1} of {Files.Count}: {file.FileName}";

                    try
                    {
                        var settings = CreateConversionSettings();
                        var outputPath = await _imageProcessingService.ConvertImageAsync(
                            file.FilePath, settings, null, token);

                        file.OutputPath = outputPath;
                        file.Status = ConversionStatus.Completed;
                        CompletedFiles++;

                        await RecordHistoryAsync(file, outputPath, "Completed", null);
                    }
                    catch (Exception ex)
                    {
                        file.Status = ConversionStatus.Failed;
                        file.ErrorMessage = ex.Message;
                        FailedFiles++;
                        await RecordHistoryAsync(file, null, "Failed", ex.Message);
                    }

                    OverallProgress = (double)(i + 1) / Files.Count * 100;
                }

                ProgressText = token.IsCancellationRequested ? "Conversion cancelled" : "Conversion completed";

                if (!token.IsCancellationRequested)
                {
                    string completionNotification = $"Conversion Completed\n\nFiles converted: {CompletedFiles}\nOutput: {OutputDirectory}";
                    System.Windows.MessageBox.Show(completionNotification, "Conversion Completed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    if (CompletedFiles > 0 && applicationSettings.OpenOutputAfterConversion)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{OutputDirectory}\"") { UseShellExecute = true });
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                IsConverting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private bool CanConvertAll() => !IsConverting && Files.Count > 0 && !string.IsNullOrWhiteSpace(OutputDirectory);

        private void UpdateQueueState()
        {
            QueueCount = Files.Count;
            TotalFiles = Files.Count;
            HasFiles = Files.Count > 0;
            ConvertAllCommand.NotifyCanExecuteChanged();
        }

        partial void OnOutputDirectoryChanged(string value) => ConvertAllCommand.NotifyCanExecuteChanged();
        partial void OnIsConvertingChanged(bool value) => ConvertAllCommand.NotifyCanExecuteChanged();

        [RelayCommand]
        private void ZoomIn() => PreviewZoom = Math.Min(4d, Math.Round(PreviewZoom + .25d, 2));

        [RelayCommand]
        private void ZoomOut() => PreviewZoom = Math.Max(.25d, Math.Round(PreviewZoom - .25d, 2));

        [RelayCommand]
        private void FitPreview() => PreviewZoom = 1d;

        private ConversionSettings CreateConversionSettings() => new()
        {
            TargetFormat = TargetFormat,
            Quality = Quality,
            ResizeEnabled = ResizeEnabled,
            ResizeWidth = ResizeWidth,
            ResizeHeight = ResizeHeight,
            MaintainAspectRatio = MaintainAspectRatio,
            BackgroundColor = BackgroundColor,
            OutputDirectory = OutputDirectory,
            MetadataHandling = MetadataHandling,
            OptimizationPreset = OptimizationPreset
        };

        private async Task RecordHistoryAsync(ImageFile file, string? outputPath, string status, string? errorDetails)
        {
            try
            {
                var outputInfo = outputPath is null ? null : new FileInfo(outputPath);
                var dimensions = GetDimensionsSafely(outputPath ?? file.FilePath);
                await _historyRepository.SaveConversionHistoryAsync(new ConversionHistoryRecord
                {
                    SourceFile = file.FilePath,
                    OutputFile = outputPath,
                    SourceFormat = file.Extension,
                    OutputFormat = TargetFormat.ToString(),
                    OriginalSize = file.FileSize,
                    OutputSize = outputInfo?.Exists == true ? outputInfo.Length : 0,
                    Quality = Quality,
                    Width = dimensions.Width,
                    Height = dimensions.Height,
                    CreatedDate = DateTime.Now,
                    Status = status,
                    ErrorDetails = errorDetails
                });
            }
            catch
            {
                // History persistence must never change the outcome of a conversion.
            }
        }

        private (int Width, int Height) GetDimensionsSafely(string path)
        {
            try
            {
                return _imageProcessingService.GetImageDimensions(path);
            }
            catch
            {
                return (0, 0);
            }
        }

        private void OnSettingsChanged(object? sender, ApplicationSettings settings)
        {
            if (!IsConverting) ApplyApplicationSettings(settings);
        }

        private void ApplyApplicationSettings(ApplicationSettings settings)
        {
            if (Enum.TryParse<OutputFormat>(settings.DefaultOutputFormat, true, out var format)) TargetFormat = format;
            Quality = Math.Clamp(settings.DefaultJpegQuality, 1, 100);
            BackgroundColor = string.IsNullOrWhiteSpace(settings.DefaultBackgroundColor) ? "#FFFFFF" : settings.DefaultBackgroundColor;
            OutputDirectory = settings.DefaultOutputDirectory ?? string.Empty;
            PreviewZoom = Math.Clamp(settings.DefaultZoomLevel, .25d, 4d);
        }

        private void RestoreLastConversionSettings(ApplicationSettings settings)
        {
            if (!settings.RestorePreviousSession || !settings.RememberLastSettings) return;

            var lastSettings = _settingsService.LoadLastConversionSettings();
            if (string.IsNullOrWhiteSpace(lastSettings.OutputDirectory)) return;

            TargetFormat = lastSettings.TargetFormat;
            Quality = Math.Clamp(lastSettings.Quality, 1, 100);
            BackgroundColor = string.IsNullOrWhiteSpace(lastSettings.BackgroundColor) ? BackgroundColor : lastSettings.BackgroundColor;
            OutputDirectory = lastSettings.OutputDirectory;
            ResizeEnabled = lastSettings.ResizeEnabled;
            ResizeWidth = lastSettings.ResizeWidth;
            ResizeHeight = lastSettings.ResizeHeight;
            MaintainAspectRatio = lastSettings.MaintainAspectRatio;
            MetadataHandling = lastSettings.MetadataHandling;
            OptimizationPreset = lastSettings.OptimizationPreset;
        }

        [RelayCommand]
        private void CancelConversion()
        {
            if (IsConverting)
            {
                _cancellationTokenSource?.Cancel();
            }
        }
    }
}
