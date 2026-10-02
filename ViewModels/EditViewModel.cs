using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConverterPro.Models;
using ImageConverterPro.Services;
using Microsoft.Win32;

namespace ImageConverterPro.ViewModels
{
    public partial class EditViewModel : ObservableObject
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IImageEditingService _imageEditingService;
        private readonly IHistoryRepository _historyRepository;
        private readonly IRecentFilesService _recentFilesService;
        private CancellationTokenSource? _renderCancellationTokenSource;
        private bool _suppressChangeTracking;
        private bool _isRestoringHistory;
        private readonly Stack<ImageEditOptions> _undoHistory = new();
        private readonly Stack<ImageEditOptions> _redoHistory = new();
        private ImageEditOptions _lastAppliedOptions = new();

        [ObservableProperty] private string? _loadedImagePath;
        [ObservableProperty] private BitmapSource? _originalImage;
        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ApplyCommand)), NotifyCanExecuteChangedFor(nameof(SaveAsCommand))] private BitmapSource? _editedImage;
        [ObservableProperty] private double _brightness;
        [ObservableProperty] private double _contrast;
        [ObservableProperty] private double _saturation;
        [ObservableProperty] private double _sharpness;
        [ObservableProperty] private int _rotationAngle;
        [ObservableProperty] private bool _flipHorizontal;
        [ObservableProperty] private bool _flipVertical;
        [ObservableProperty] private bool _useCrop;
        [ObservableProperty] private double _cropX;
        [ObservableProperty] private double _cropY;
        [ObservableProperty] private double _cropWidth;
        [ObservableProperty] private double _cropHeight;
        [ObservableProperty] private CropAspectRatio _cropAspectRatio = CropAspectRatio.Free;
        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ApplyCommand)), NotifyCanExecuteChangedFor(nameof(SaveAsCommand))] private bool _isProcessing;
        [ObservableProperty] private bool _hasChanges;

        public IReadOnlyList<CropAspectRatio> CropAspectRatios { get; } = Enum.GetValues<CropAspectRatio>();
        public bool HasImage => OriginalImage is not null;

        public EditViewModel(
            IImageProcessingService imageProcessingService,
            IImageEditingService imageEditingService,
            IHistoryRepository historyRepository,
            IRecentFilesService recentFilesService)
        {
            _imageProcessingService = imageProcessingService;
            _imageEditingService = imageEditingService;
            _historyRepository = historyRepository;
            _recentFilesService = recentFilesService;
        }

        partial void OnOriginalImageChanged(BitmapSource? value)
        {
            OnPropertyChanged(nameof(HasImage));
            ApplyCommand.NotifyCanExecuteChanged();
            SaveAsCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        partial void OnBrightnessChanged(double value) => TrackChange();
        partial void OnContrastChanged(double value) => TrackChange();
        partial void OnSaturationChanged(double value) => TrackChange();
        partial void OnSharpnessChanged(double value) => TrackChange();
        partial void OnRotationAngleChanged(int value) => TrackChange();
        partial void OnFlipHorizontalChanged(bool value) => TrackChange();
        partial void OnFlipVerticalChanged(bool value) => TrackChange();
        partial void OnUseCropChanged(bool value) => TrackChange();
        partial void OnCropXChanged(double value) => TrackChange();
        partial void OnCropYChanged(double value) => TrackChange();
        partial void OnCropWidthChanged(double value) => TrackChange();
        partial void OnCropHeightChanged(double value) => TrackChange();
        partial void OnCropAspectRatioChanged(CropAspectRatio value)
        {
            TrackChange();
            if (!_suppressChangeTracking)
            {
                UseCrop = value != CropAspectRatio.Free;
                FitCropToAspectRatio();
            }
        }

        partial void OnIsProcessingChanged(bool value)
        {
            ApplyCommand.NotifyCanExecuteChanged();
            SaveAsCommand.NotifyCanExecuteChanged();
            NotifyHistoryChanged();
        }

        [RelayCommand]
        private async Task OpenImageAsync()
        {
            var dialog = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.webp|All Files|*.*" };
            if (dialog.ShowDialog() != true) return;

            await OpenImageFileAsync(dialog.FileName);
        }

        public async Task OpenImageFileAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                CancelRender();
                IsProcessing = true;
                var image = await _imageProcessingService.LoadImageAsync(path);
                LoadedImagePath = path;
                OriginalImage = image;
                EditedImage = image;
                ResetOptions();
                _lastAppliedOptions = CreateEditOptions();
                _undoHistory.Clear();
                _redoHistory.Clear();
                NotifyHistoryChanged();
                _recentFilesService.AddRecentFile(path, "Edit");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The image could not be opened.\n\n{ex.Message}", "Open image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsProcessing = false; }
        }

        [RelayCommand(CanExecute = nameof(CanApply))]
        private async Task ApplyAsync()
        {
            if (LoadedImagePath is null) return;

            var options = CreateEditOptions();
            var previousOptions = _lastAppliedOptions;

            CancelRender();
            var cancellationTokenSource = new CancellationTokenSource();
            _renderCancellationTokenSource = cancellationTokenSource;
            IsProcessing = true;

            try
            {
                var renderedImage = await _imageEditingService.RenderAsync(LoadedImagePath, options, cancellationTokenSource.Token);
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    EditedImage = renderedImage;
                    HasChanges = false;
                    if (!_isRestoringHistory && !OptionsEqual(options, previousOptions))
                    {
                        _undoHistory.Push(previousOptions);
                        _redoHistory.Clear();
                    }
                    _lastAppliedOptions = options;
                    NotifyHistoryChanged();
                }
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                // A later edit superseded this render; keep the existing preview.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The edits could not be applied.\n\n{ex.Message}", "Apply edits", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (ReferenceEquals(_renderCancellationTokenSource, cancellationTokenSource))
                {
                    _renderCancellationTokenSource = null;
                    IsProcessing = false;
                }
                cancellationTokenSource.Dispose();
            }
        }

        [RelayCommand]
        private void Reset()
        {
            CancelRender();
            EditedImage = OriginalImage;
            ResetOptions();
            _lastAppliedOptions = CreateEditOptions();
            _undoHistory.Clear();
            _redoHistory.Clear();
            NotifyHistoryChanged();
        }

        [RelayCommand] private void Rotate90() => RotationAngle = (RotationAngle + 90) % 360;
        [RelayCommand] private void Rotate180() => RotationAngle = (RotationAngle + 180) % 360;
        [RelayCommand] private void Rotate270() => RotationAngle = (RotationAngle + 270) % 360;
        [RelayCommand] private void ToggleHorizontalFlip() => FlipHorizontal = !FlipHorizontal;
        [RelayCommand] private void ToggleVerticalFlip() => FlipVertical = !FlipVertical;

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private async Task UndoAsync()
        {
            if (_undoHistory.Count == 0) return;
            var currentOptions = _lastAppliedOptions;
            var previousOptions = _undoHistory.Pop();
            _redoHistory.Push(currentOptions);
            await RestoreHistoryAsync(previousOptions);
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private async Task RedoAsync()
        {
            if (_redoHistory.Count == 0) return;
            var currentOptions = _lastAppliedOptions;
            var nextOptions = _redoHistory.Pop();
            _undoHistory.Push(currentOptions);
            await RestoreHistoryAsync(nextOptions);
        }

        [RelayCommand]
        private void SelectCropAspect(CropAspectRatio aspectRatio)
        {
            CropAspectRatio = aspectRatio;
            UseCrop = aspectRatio != CropAspectRatio.Free;
            FitCropToAspectRatio();
        }

        [RelayCommand(CanExecute = nameof(CanSaveAs))]
        private async Task SaveAsAsync()
        {
            if (EditedImage is null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|WebP Image|*.webp|Bitmap Image|*.bmp|TIFF Image|*.tiff",
                FileName = LoadedImagePath is null ? "edited-image" : $"{Path.GetFileNameWithoutExtension(LoadedImagePath)}_edited"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                IsProcessing = true;
                var imageToSave = HasChanges && LoadedImagePath is not null
                    ? await _imageEditingService.RenderAsync(LoadedImagePath, CreateEditOptions())
                    : EditedImage;

                await _imageEditingService.SaveAsync(imageToSave, dialog.FileName, GetFormat(dialog.FileName), 90);
                EditedImage = imageToSave;
                await RecordEditingHistoryAsync(dialog.FileName);
                _recentFilesService.AddRecentFile(dialog.FileName, "Edit");
                HasChanges = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The edited image could not be saved.\n\n{ex.Message}", "Save As", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsProcessing = false; }
        }

        private bool CanApply() => HasImage && !IsProcessing;
        private bool CanSaveAs() => EditedImage is not null && !IsProcessing;
        private bool CanUndo() => _undoHistory.Count > 0 && !IsProcessing;
        private bool CanRedo() => _redoHistory.Count > 0 && !IsProcessing;

        private void ResetOptions()
        {
            _suppressChangeTracking = true;
            Brightness = Contrast = Saturation = Sharpness = 0;
            RotationAngle = 0;
            FlipHorizontal = FlipVertical = false;
            UseCrop = false;
            CropAspectRatio = CropAspectRatio.Free;
            CropX = CropY = 0;
            CropWidth = OriginalImage?.PixelWidth ?? 0;
            CropHeight = OriginalImage?.PixelHeight ?? 0;
            _suppressChangeTracking = false;
            HasChanges = false;
        }

        private ImageEditOptions CreateEditOptions() => new()
        {
            CropRectangle = UseCrop ? GetCropRectangle() : null,
            RotationAngle = RotationAngle,
            FlipMode = FlipHorizontal && FlipVertical ? ImageFlipMode.Both : FlipHorizontal ? ImageFlipMode.Horizontal : FlipVertical ? ImageFlipMode.Vertical : ImageFlipMode.None,
            Brightness = Brightness,
            Contrast = Contrast,
            Saturation = Saturation,
            Sharpness = Sharpness
        };

        private Int32Rect? GetCropRectangle()
        {
            if (OriginalImage is null) return null;
            var x = Math.Clamp((int)Math.Round(CropX), 0, OriginalImage.PixelWidth);
            var y = Math.Clamp((int)Math.Round(CropY), 0, OriginalImage.PixelHeight);
            var width = Math.Clamp((int)Math.Round(CropWidth), 0, OriginalImage.PixelWidth - x);
            var height = Math.Clamp((int)Math.Round(CropHeight), 0, OriginalImage.PixelHeight - y);
            return width > 0 && height > 0 ? new Int32Rect(x, y, width, height) : null;
        }

        private void FitCropToAspectRatio()
        {
            if (OriginalImage is null || CropAspectRatio == CropAspectRatio.Free) return;
            var targetRatio = CropAspectRatio switch
            {
                CropAspectRatio.Square => 1d,
                CropAspectRatio.SixteenByNine => 16d / 9d,
                CropAspectRatio.FourByThree => 4d / 3d,
                _ => 1d
            };
            var availableWidth = Math.Max(1, OriginalImage.PixelWidth - CropX);
            var availableHeight = Math.Max(1, OriginalImage.PixelHeight - CropY);
            var width = availableWidth;
            var height = width / targetRatio;
            if (height > availableHeight)
            {
                height = availableHeight;
                width = height * targetRatio;
            }
            CropWidth = Math.Round(width);
            CropHeight = Math.Round(height);
        }

        private void TrackChange()
        {
            if (!_suppressChangeTracking && HasImage) HasChanges = true;
        }

        private void CancelRender()
        {
            _renderCancellationTokenSource?.Cancel();
        }

        private async Task RestoreHistoryAsync(ImageEditOptions options)
        {
            ApplyOptions(options);
            _isRestoringHistory = true;
            try
            {
                await ApplyAsync();
            }
            finally
            {
                _isRestoringHistory = false;
                NotifyHistoryChanged();
            }
        }

        private void ApplyOptions(ImageEditOptions options)
        {
            _suppressChangeTracking = true;
            Brightness = options.Brightness;
            Contrast = options.Contrast;
            Saturation = options.Saturation;
            Sharpness = options.Sharpness;
            RotationAngle = options.RotationAngle;
            FlipHorizontal = options.FlipMode is ImageFlipMode.Horizontal or ImageFlipMode.Both;
            FlipVertical = options.FlipMode is ImageFlipMode.Vertical or ImageFlipMode.Both;
            UseCrop = options.CropRectangle.HasValue;
            if (options.CropRectangle is { } crop)
            {
                CropX = crop.X;
                CropY = crop.Y;
                CropWidth = crop.Width;
                CropHeight = crop.Height;
            }
            CropAspectRatio = CropAspectRatio.Free;
            _suppressChangeTracking = false;
            HasChanges = false;
        }

        private void NotifyHistoryChanged()
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        private static bool OptionsEqual(ImageEditOptions left, ImageEditOptions right) =>
            Nullable.Equals(left.CropRectangle, right.CropRectangle)
            && left.RotationAngle == right.RotationAngle
            && left.FlipMode == right.FlipMode
            && left.Brightness.Equals(right.Brightness)
            && left.Contrast.Equals(right.Contrast)
            && left.Saturation.Equals(right.Saturation)
            && left.Sharpness.Equals(right.Sharpness);

        private static OutputFormat GetFormat(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => OutputFormat.JPG,
            ".webp" => OutputFormat.WEBP,
            ".bmp" => OutputFormat.BMP,
            ".tif" or ".tiff" => OutputFormat.TIFF,
            _ => OutputFormat.PNG
        };

        private async Task RecordEditingHistoryAsync(string outputPath)
        {
            if (LoadedImagePath is null) return;

            try
            {
                await _historyRepository.SaveEditingHistoryAsync(new EditingHistoryRecord
                {
                    SourceFile = LoadedImagePath,
                    OutputFile = outputPath,
                    Operations = DescribeOperations(),
                    CreatedDate = DateTime.Now
                });
            }
            catch
            {
                // The saved image remains valid when history persistence is unavailable.
            }
        }

        private string DescribeOperations()
        {
            var operations = new List<string>();
            if (UseCrop) operations.Add($"Crop ({CropAspectRatio})");
            if (RotationAngle != 0) operations.Add($"Rotate {RotationAngle}°");
            if (FlipHorizontal) operations.Add("Flip horizontal");
            if (FlipVertical) operations.Add("Flip vertical");
            if (Brightness != 0) operations.Add($"Brightness {Brightness:+0;-0;0}");
            if (Contrast != 0) operations.Add($"Contrast {Contrast:+0;-0;0}");
            if (Saturation != 0) operations.Add($"Saturation {Saturation:+0;-0;0}");
            if (Sharpness != 0) operations.Add($"Sharpness {Sharpness:0}");
            return operations.Count > 0 ? string.Join(" + ", operations) : "Save As";
        }
    }
}
