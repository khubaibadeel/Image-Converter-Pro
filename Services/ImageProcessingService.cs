using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ImageConverterPro.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ImageConverterPro.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly IMetadataService _metadataService;
        private readonly IImageOptimizationService _optimizationService;

        public ImageProcessingService(
            IMetadataService metadataService,
            IImageOptimizationService optimizationService)
        {
            _metadataService = metadataService;
            _optimizationService = optimizationService;
        }

        public async Task<BitmapSource> LoadImageAsync(string path)
        {
            return await Task.Run(async () =>
            {
                using var image = await Image.LoadAsync(path);
                if (image.Width > 3840 || image.Height > 3840)
                {
                    using var previewCopy = image.Clone(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(3840, 3840),
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                    }));
                    return CreateBitmapSourceFromImage(previewCopy);
                }
                return CreateBitmapSourceFromImage(image);
            });
        }

        public async Task<BitmapSource> LoadThumbnailAsync(string path, int maxSize = 200)
        {
            return await Task.Run(async () =>
            {
                using var image = await Image.LoadAsync(path);
                
                int width = image.Width;
                int height = image.Height;
                
                if (width > maxSize || height > maxSize)
                {
                    var ratioX = (double)maxSize / width;
                    var ratioY = (double)maxSize / height;
                    var ratio = Math.Min(ratioX, ratioY);

                    var newWidth = Math.Max(1, (int)(width * ratio));
                    var newHeight = Math.Max(1, (int)(height * ratio));

                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                return CreateBitmapSourceFromImage(image);
            });
        }

        public async Task<BitmapSource> CreateConvertedPreviewAsync(
            string sourcePath,
            ConversionSettings settings,
            CancellationToken ct = default)
        {
            return await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                using var image = await Image.LoadAsync(sourcePath, ct);
                var optimizationPlan = _optimizationService.CreatePlan(settings);

                ApplyOutputTransformations(image, settings, optimizationPlan);

                if (image.Width > 3840 || image.Height > 3840)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(3840, 3840),
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                    }));
                }

                await using var encodedPreview = new MemoryStream();
                await image.SaveAsync(encodedPreview, GetEncoder(settings.TargetFormat, optimizationPlan.Quality), ct);
                encodedPreview.Position = 0;
                ct.ThrowIfCancellationRequested();
                using var previewImage = await Image.LoadAsync(encodedPreview, ct);
                return CreateBitmapSourceFromImage(previewImage);
            }, ct);
        }

        public (int Width, int Height) GetImageDimensions(string path)
        {
            var info = Image.Identify(path);
            if (info != null)
            {
                return (info.Width, info.Height);
            }
            return (0, 0);
        }

        public async Task<string> ConvertImageAsync(string sourcePath, ConversionSettings settings, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            using var image = await Image.LoadAsync(sourcePath, ct);
            
            var optimizationPlan = _optimizationService.CreatePlan(settings);
            ApplyOutputTransformations(image, settings, optimizationPlan);

            string outputPath = GenerateOutputPath(sourcePath, settings);

            if (!settings.OverwriteExisting && File.Exists(outputPath))
            {
                string dir = Path.GetDirectoryName(outputPath) ?? string.Empty;
                string name = Path.GetFileNameWithoutExtension(outputPath);
                string ext = Path.GetExtension(outputPath);
                int counter = 1;

                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(dir, $"{name}_{counter}{ext}");
                    counter++;
                }
            }

            IImageEncoder encoder = GetEncoder(settings.TargetFormat, optimizationPlan.Quality);
            
            await image.SaveAsync(outputPath, encoder, ct);
            
            progress?.Report(100.0);
            return outputPath;
        }

        public async Task<List<string>> BatchConvertAsync(List<string> sourcePaths, ConversionSettings settings, IProgress<(int current, int total, string fileName)>? progress = null, CancellationToken ct = default)
        {
            var results = new List<string>();
            int total = sourcePaths.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var sourcePath = sourcePaths[i];
                progress?.Report((i + 1, total, Path.GetFileName(sourcePath)));

                var outputPath = await ConvertImageAsync(sourcePath, settings, null, ct);
                results.Add(outputPath);
            }

            return results;
        }

        public async Task<BitmapSource> ApplyAdjustmentsAsync(string sourcePath, ImageAdjustments adjustments)
        {
            using var image = await Image.LoadAsync(sourcePath);

            image.Mutate(x =>
            {
                if (adjustments.Brightness != 0)
                {
                    float brightness = 1f + (float)(adjustments.Brightness / 100f);
                    x.Brightness(brightness);
                }

                if (adjustments.Contrast != 0)
                {
                    float contrast = 1f + (float)(adjustments.Contrast / 100f);
                    x.Contrast(contrast);
                }

                if (adjustments.Saturation != 0)
                {
                    float saturation = 1f + (float)(adjustments.Saturation / 100f);
                    x.Saturate(saturation);
                }

                if (adjustments.Sharpness > 0)
                {
                    float sigma = (float)adjustments.Sharpness / 20f;
                    if (sigma > 0)
                        x.GaussianSharpen(sigma);
                }

                if (adjustments.FlipHorizontal && adjustments.FlipVertical)
                {
                    x.Flip(FlipMode.Horizontal).Flip(FlipMode.Vertical);
                }
                else if (adjustments.FlipHorizontal)
                {
                    x.Flip(FlipMode.Horizontal);
                }
                else if (adjustments.FlipVertical)
                {
                    x.Flip(FlipMode.Vertical);
                }

                if (adjustments.Rotation == 90) x.Rotate(RotateMode.Rotate90);
                else if (adjustments.Rotation == 180) x.Rotate(RotateMode.Rotate180);
                else if (adjustments.Rotation == 270) x.Rotate(RotateMode.Rotate270);
            });

            return CreateBitmapSourceFromImage(image);
        }

        public async Task<BitmapSource> CropImageAsync(string sourcePath, CropRegion region)
        {
            using var image = await Image.LoadAsync(sourcePath);
            
            var rect = new Rectangle(
                (int)region.X, 
                (int)region.Y, 
                (int)region.Width, 
                (int)region.Height);

            // Ensure bounds
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));

            if (rect.Width > 0 && rect.Height > 0)
            {
                image.Mutate(x => x.Crop(rect));
            }

            return CreateBitmapSourceFromImage(image);
        }

        public async Task<string> SaveEditedImageAsync(BitmapSource source, string outputPath, OutputFormat format, int quality)
        {
            using var image = await CreateImageFromBitmapSourceAsync(source);
            IImageEncoder encoder = GetEncoder(format, quality);
            await image.SaveAsync(outputPath, encoder);
            return outputPath;
        }

        private BitmapSource CreateBitmapSourceFromImage(Image image)
        {
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private async Task<Image> CreateImageFromBitmapSourceAsync(BitmapSource bitmapSource)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            return await Image.LoadAsync(memoryStream);
        }

        private IImageEncoder GetEncoder(OutputFormat format, int quality)
        {
            return format switch
            {
                OutputFormat.JPG => new JpegEncoder { Quality = quality },
                OutputFormat.PNG => new PngEncoder(),
                OutputFormat.WEBP => new WebpEncoder { Quality = quality },
                OutputFormat.BMP => new BmpEncoder(),
                OutputFormat.TIFF => new TiffEncoder(),
                _ => new PngEncoder()
            };
        }

        private void ApplyOutputTransformations(Image image, ConversionSettings settings, OptimizationPlan optimizationPlan)
        {
            ImageOptimizationService.ApplyResize(image, optimizationPlan);

            // Flatten only when the chosen output format cannot represent alpha.
            if (settings.TargetFormat == OutputFormat.JPG || settings.TargetFormat == OutputFormat.BMP)
            {
                try
                {
                    var background = string.IsNullOrWhiteSpace(settings.BackgroundColor)
                        ? Color.White
                        : Color.Parse(settings.BackgroundColor);
                    image.Mutate(context => context.BackgroundColor(background));
                }
                catch
                {
                    image.Mutate(context => context.BackgroundColor(Color.White));
                }
            }

            _metadataService.ApplyMetadataHandling(image, settings.MetadataHandling);
        }

        private string GenerateOutputPath(string sourcePath, ConversionSettings settings)
        {
            string directory = string.IsNullOrWhiteSpace(settings.OutputDirectory) 
                ? Path.GetDirectoryName(sourcePath) ?? string.Empty
                : settings.OutputDirectory;
            
            string originalFileName = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = settings.TargetFormat.ToString().ToLowerInvariant();

            string newFileName = settings.NamingConvention switch
            {
                NamingConvention.AddSuffix => $"{originalFileName}_converted.{extension}",
                NamingConvention.AddPrefix => $"converted_{originalFileName}.{extension}",
                NamingConvention.Custom => string.IsNullOrWhiteSpace(settings.CustomNamingPattern) 
                    ? $"{originalFileName}.{extension}" 
                    : $"{settings.CustomNamingPattern.Replace("{original}", originalFileName)}.{extension}",
                _ => $"{originalFileName}.{extension}"
            };

            return Path.Combine(directory, newFileName);
        }
    }
}
