using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IImageEditingService
    {
        Task<BitmapSource> RenderAsync(string sourcePath, ImageEditOptions options, CancellationToken cancellationToken = default);
        Task<string> SaveAsync(BitmapSource image, string outputPath, OutputFormat format, int quality, CancellationToken cancellationToken = default);
    }
}
