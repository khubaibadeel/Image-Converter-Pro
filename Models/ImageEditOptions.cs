using System.ComponentModel;
using System.Windows;

namespace ImageConverterPro.Models
{
    /// <summary>
    /// Describes a non-destructive image edit. A null crop rectangle leaves the source uncropped.
    /// </summary>
    public sealed class ImageEditOptions
    {
        public Int32Rect? CropRectangle { get; init; }
        public int RotationAngle { get; init; }
        public ImageFlipMode FlipMode { get; init; }
        public double Brightness { get; init; }
        public double Contrast { get; init; }
        public double Saturation { get; init; }
        public double Sharpness { get; init; }
    }

    public enum ImageFlipMode
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public enum CropAspectRatio
    {
        [Description("Free")]
        Free,
        [Description("Square (1:1)")]
        Square,
        [Description("16:9")]
        SixteenByNine,
        [Description("4:3")]
        FourByThree
    }
}
