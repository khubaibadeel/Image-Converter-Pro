using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageConverterPro.Models
{
    public enum OutputFormat
    {
        PNG,
        JPG,
        WEBP,
        BMP,
        TIFF
    }

    public enum ResizeMode
    {
        Exact,
        FitWithin,
        FillCover
    }

    public enum NamingConvention
    {
        KeepOriginal,
        AddSuffix,
        AddPrefix,
        Custom
    }

    /// <summary>
    /// Controls whether source metadata is carried into the newly written file.
    /// This never changes the source file.
    /// </summary>
    public enum MetadataHandling
    {
        Keep,
        Remove
    }

    /// <summary>
    /// Explicit output optimization choices. None leaves the conversion settings
    /// unchanged; every other value is applied only when selected by the user.
    /// </summary>
    public enum OptimizationPreset
    {
        None,
        Website,
        Email,
        Instagram,
        Facebook
    }

    public partial class ConversionSettings : ObservableObject
    {
        [ObservableProperty]
        private OutputFormat _targetFormat = OutputFormat.PNG;

        [ObservableProperty]
        private int _quality = 90;

        [ObservableProperty]
        private bool _resizeEnabled;

        [ObservableProperty]
        private int _resizeWidth;

        [ObservableProperty]
        private int _resizeHeight;

        [ObservableProperty]
        private bool _maintainAspectRatio = true;

        [ObservableProperty]
        private ResizeMode _resizeMode = ResizeMode.FitWithin;

        [ObservableProperty]
        private string _backgroundColor = "#FFFFFF";

        [ObservableProperty]
        private string _outputDirectory = string.Empty;

        [ObservableProperty]
        private bool _overwriteExisting;

        [ObservableProperty]
        private NamingConvention _namingConvention = NamingConvention.KeepOriginal;

        [ObservableProperty]
        private string _customNamingPattern = string.Empty;

        [ObservableProperty]
        private MetadataHandling _metadataHandling = MetadataHandling.Keep;

        [ObservableProperty]
        private OptimizationPreset _optimizationPreset = OptimizationPreset.None;
    }
}
