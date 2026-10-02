using SixLabors.ImageSharp;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    public interface IMetadataService
    {
        void ApplyMetadataHandling(Image image, MetadataHandling handling);
        bool HasMetadata(Image image);
    }
}
