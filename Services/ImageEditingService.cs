using System;
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
    public sealed class ImageEditingService : IImageEditingService
    {
        public async Task<BitmapSource> RenderAsync(string sourcePath, ImageEditOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            ArgumentNullException.ThrowIfNull(options);
            cancellationToken.ThrowIfCancellationRequested();

            using var image = await Image.LoadAsync(sourcePath, cancellationToken);
            ApplyEdits(image, options);
            cancellationToken.ThrowIfCancellationRequested();
            return CreateBitmapSource(image);
        }

        public async Task<string> SaveAsync(BitmapSource bitmapSource, string outputPath, OutputFormat format, int quality, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(bitmapSource);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
            cancellationToken.ThrowIfCancellationRequested();

            using var image = await CreateImageAsync(bitmapSource, cancellationToken);
            await image.SaveAsync(outputPath, GetEncoder(format, quality), cancellationToken);
            return outputPath;
        }

        private static void ApplyEdits(Image image, ImageEditOptions options)
        {
            image.Mutate(context =>
            {
                if (options.CropRectangle is { } crop)
                {
                    var cropRectangle = new Rectangle(crop.X, crop.Y, crop.Width, crop.Height);
                    cropRectangle.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                    if (cropRectangle.Width > 0 && cropRectangle.Height > 0)
                    {
                        context.Crop(cropRectangle);
                    }
                }

                switch (NormalizeRotation(options.RotationAngle))
                {
                    case 90:
                        context.Rotate(RotateMode.Rotate90);
                        break;
                    case 180:
                        context.Rotate(RotateMode.Rotate180);
                        break;
                    case 270:
                        context.Rotate(RotateMode.Rotate270);
                        break;
                }

                switch (options.FlipMode)
                {
                    case ImageFlipMode.Horizontal:
                        context.Flip(SixLabors.ImageSharp.Processing.FlipMode.Horizontal);
                        break;
                    case ImageFlipMode.Vertical:
                        context.Flip(SixLabors.ImageSharp.Processing.FlipMode.Vertical);
                        break;
                    case ImageFlipMode.Both:
                        context.Flip(SixLabors.ImageSharp.Processing.FlipMode.Horizontal)
                               .Flip(SixLabors.ImageSharp.Processing.FlipMode.Vertical);
                        break;
                }

                if (options.Brightness != 0)
                {
                    context.Brightness(1f + (float)Math.Clamp(options.Brightness, -100, 100) / 100f);
                }

                if (options.Contrast != 0)
                {
                    context.Contrast(1f + (float)Math.Clamp(options.Contrast, -100, 100) / 100f);
                }

                if (options.Saturation != 0)
                {
                    context.Saturate((float)Math.Clamp(options.Saturation, -100, 100) / 100f);
                }

                if (options.Sharpness > 0)
                {
                    context.GaussianSharpen((float)Math.Clamp(options.Sharpness, 0, 100) / 20f);
                }
            });
        }

        private static int NormalizeRotation(int angle) => ((angle % 360) + 360) % 360;

        private static BitmapSource CreateBitmapSource(Image image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            stream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static async Task<Image> CreateImageAsync(BitmapSource bitmapSource, CancellationToken cancellationToken)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;
            return await Image.LoadAsync(stream, cancellationToken);
        }

        private static IImageEncoder GetEncoder(OutputFormat format, int quality) => format switch
        {
            OutputFormat.JPG => new JpegEncoder { Quality = Math.Clamp(quality, 1, 100) },
            OutputFormat.PNG => new PngEncoder(),
            OutputFormat.WEBP => new WebpEncoder { Quality = Math.Clamp(quality, 1, 100) },
            OutputFormat.BMP => new BmpEncoder(),
            OutputFormat.TIFF => new TiffEncoder(),
            _ => new PngEncoder()
        };
    }
}
