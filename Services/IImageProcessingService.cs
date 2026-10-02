using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IImageProcessingService
    {
        Task<BitmapSource> LoadImageAsync(string path);
        Task<BitmapSource> LoadThumbnailAsync(string path, int maxSize = 200);
        Task<BitmapSource> CreateConvertedPreviewAsync(string sourcePath, ConversionSettings settings, CancellationToken ct = default);
        Task<string> ConvertImageAsync(string sourcePath, ConversionSettings settings, IProgress<double>? progress = null, CancellationToken ct = default);
        Task<BitmapSource> ApplyAdjustmentsAsync(string sourcePath, ImageAdjustments adjustments);
        Task<BitmapSource> CropImageAsync(string sourcePath, CropRegion region);
        Task<string> SaveEditedImageAsync(BitmapSource image, string outputPath, OutputFormat format, int quality);
        (int Width, int Height) GetImageDimensions(string path);
        Task<List<string>> BatchConvertAsync(List<string> sourcePaths, ConversionSettings settings, IProgress<(int current, int total, string fileName)>? progress = null, CancellationToken ct = default);
    }
}
