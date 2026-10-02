using System.IO;
using ImageConverterPro.Models;
using SixLabors.ImageSharp;

namespace ImageConverterPro.Services
{
    public sealed class ImageInformationService : IImageInformationService
    {
        private readonly IMetadataService _metadataService;

        public ImageInformationService(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<ImageInformation> GetInformationAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(filePath);
            using var image = await Image.LoadAsync(filePath, cancellationToken);
            string formatName = image.Metadata.DecodedImageFormat?.Name?.ToUpperInvariant() ?? Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
            bool hasAlpha = (image.PixelType.BitsPerPixel == 32 || image.PixelType.BitsPerPixel == 64 || image.PixelType.BitsPerPixel == 16) &&
                            (formatName == "PNG" || formatName == "WEBP" || formatName == "TIFF" || formatName == "GIF");

            return new ImageInformation
            {
                FilePath = filePath,
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                ColorDepth = image.PixelType.BitsPerPixel,
                HasAlpha = hasAlpha,
                HasMetadata = _metadataService.HasMetadata(image)
            };
        }
    }
}
