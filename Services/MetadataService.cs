using SixLabors.ImageSharp;
using ImageConverterPro.Models;

namespace ImageConverterPro.Services
{
    /// <summary>
    /// Applies metadata policy to an already-loaded output image. It never opens,
    /// writes, or changes the source file.
    /// </summary>
    public sealed class MetadataService : IMetadataService
    {
        public void ApplyMetadataHandling(Image image, MetadataHandling handling)
        {
            if (handling != MetadataHandling.Remove)
            {
                return;
            }

            // EXIF contains GPS, camera/device and capture-date fields. Removing the
            // complete profiles also prevents related XMP/IPTC information surviving.
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
        }

        public bool HasMetadata(Image image) =>
            image.Metadata.ExifProfile is not null ||
            image.Metadata.IptcProfile is not null ||
            image.Metadata.XmpProfile is not null;
    }
}
