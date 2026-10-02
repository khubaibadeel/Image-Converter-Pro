using System.Windows;

namespace ImageConverterPro.Services
{
    public interface IWindowStateService
    {
        void Restore(Window window);
        void Save(Window window);
    }
}
