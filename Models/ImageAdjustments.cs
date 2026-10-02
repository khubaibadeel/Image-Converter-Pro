using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageConverterPro.Models
{
    public partial class ImageAdjustments : ObservableObject
    {
        [ObservableProperty]
        private double _brightness = 0; // range -100 to 100

        [ObservableProperty]
        private double _contrast = 0; // range -100 to 100

        [ObservableProperty]
        private double _saturation = 0; // range -100 to 100

        [ObservableProperty]
        private double _sharpness = 0; // range -100 to 100

        [ObservableProperty]
        private int _rotation = 0; // 0, 90, 180, 270

        [ObservableProperty]
        private bool _flipHorizontal = false;

        [ObservableProperty]
        private bool _flipVertical = false;
    }
}
