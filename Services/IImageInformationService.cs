using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IImageInformationService
    {
        Task<ImageInformation> GetInformationAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
