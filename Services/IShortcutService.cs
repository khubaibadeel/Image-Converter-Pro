using System.Windows;
using System.Windows.Input;

namespace ImageConverterPro.Services
{
    public sealed class ShortcutCommandSet
    {
        public required ICommand Open { get; init; }
        public required ICommand Save { get; init; }
        public required ICommand Undo { get; init; }
        public required ICommand Redo { get; init; }
        public required ICommand Delete { get; init; }
    }

    public interface IShortcutService
    {
        void Register(Window window, ShortcutCommandSet commands);
        void Unregister(Window window);
    }
}
