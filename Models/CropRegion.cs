using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageConverterPro.Models
{
    public partial class CropRegion : ObservableObject
    {
        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;
    }
}
