using System.IO;
using ImageConverterPro.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ModelResizeMode = ImageConverterPro.Models.ResizeMode;

namespace ImageConverterPro.Services
{
    /// <summary>
    /// Provides explicit, content-neutral output presets and estimates. This
    /// service only resizes/re-encodes; it contains no overlay or watermark path.
    /// </summary>
    public sealed class ImageOptimizationService : IImageOptimizationService
    {
        private readonly IMetadataService _metadataService;

        public ImageOptimizationService(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public OptimizationPlan CreatePlan(ConversionSettings settings)
        {
            var quality = Math.Clamp(settings.Quality, 1, 100);
            var resize = settings.ResizeEnabled;
            var width = Math.Max(1, settings.ResizeWidth);
            var height = Math.Max(1, settings.ResizeHeight);

            switch (settings.OptimizationPreset)
            {
                case OptimizationPreset.Website:
                    quality = 82;
                    resize = true;
                    width = 1920;
                    height = 1080;
                    break;
                case OptimizationPreset.Email:
                    quality = 70;
                    resize = true;
                    width = 1280;
                    height = 1280;
                    break;
                case OptimizationPreset.Instagram:
                    quality = 85;
                    resize = true;
                    width = 1080;
                    height = 1350;
                    break;
                case OptimizationPreset.Facebook:
                    quality = 82;
                    resize = true;
                    width = 2048;
                    height = 2048;
                    break;
            }

            return new OptimizationPlan
            {
                Quality = quality,
                ResizeEnabled = resize,
                ResizeWidth = width,
                ResizeHeight = height,
                ResizeMode = settings.OptimizationPreset == OptimizationPreset.None ? settings.ResizeMode : ModelResizeMode.FitWithin,
                MaintainAspectRatio = settings.OptimizationPreset == OptimizationPreset.None
                    ? settings.MaintainAspectRatio
                    : true
            };
        }

        public async Task<QualityComparison> EstimateAsync(string sourcePath, ConversionSettings settings, CancellationToken cancellationToken = default)
        {
            var originalFile = new FileInfo(sourcePath);
            using var image = await Image.LoadAsync(sourcePath, cancellationToken);
            var originalWidth = image.Width;
            var originalHeight = image.Height;
            var plan = CreatePlan(settings);
            ApplyResize(image, plan);
            _metadataService.ApplyMetadataHandling(image, settings.MetadataHandling);

            await using var stream = new MemoryStream();
            await image.SaveAsync(stream, GetEncoder(settings.TargetFormat, plan.Quality), cancellationToken);

            return new QualityComparison
            {
                OriginalFileSize = originalFile.Exists ? originalFile.Length : 0,
                OriginalWidth = originalWidth,
                OriginalHeight = originalHeight,
                EstimatedFileSize = stream.Length,
                EstimatedWidth = image.Width,
                EstimatedHeight = image.Height
            };
        }

        internal static void ApplyResize(Image image, OptimizationPlan plan)
        {
            if (!plan.ResizeEnabled)
            {
                return;
            }

            var resizeOptions = new ResizeOptions
            {
                Size = new Size(plan.ResizeWidth, plan.ResizeHeight),
                Mode = plan.ResizeMode switch
                {
                    ModelResizeMode.Exact => SixLabors.ImageSharp.Processing.ResizeMode.Stretch,
                    ModelResizeMode.FillCover => SixLabors.ImageSharp.Processing.ResizeMode.Crop,
                    _ => SixLabors.ImageSharp.Processing.ResizeMode.Max
                }
            };

            if (!plan.MaintainAspectRatio && plan.ResizeMode == ModelResizeMode.Exact)
            {
                resizeOptions.Mode = SixLabors.ImageSharp.Processing.ResizeMode.Stretch;
            }

            image.Mutate(context => context.Resize(resizeOptions));
        }

        internal static IImageEncoder GetEncoder(OutputFormat format, int quality) => format switch
        {
            OutputFormat.JPG => new JpegEncoder { Quality = quality },
            OutputFormat.PNG => new PngEncoder(),
            OutputFormat.WEBP => new WebpEncoder { Quality = quality },
            OutputFormat.BMP => new BmpEncoder(),
            OutputFormat.TIFF => new TiffEncoder(),
            _ => new PngEncoder()
        };
    }
}
