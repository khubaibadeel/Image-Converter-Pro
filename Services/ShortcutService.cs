using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ImageConverterPro.Services
{
    public sealed class ShortcutService : IShortcutService
    {
        private readonly Dictionary<Window, List<InputBinding>> _bindings = new();

        public void Register(Window window, ShortcutCommandSet commands)
        {
            Unregister(window);
            var bindings = new List<InputBinding>
            {
                new KeyBinding(commands.Open, Key.O, ModifierKeys.Control),
                new KeyBinding(commands.Save, Key.S, ModifierKeys.Control),
                new KeyBinding(commands.Undo, Key.Z, ModifierKeys.Control),
                new KeyBinding(commands.Redo, Key.Y, ModifierKeys.Control),
                new KeyBinding(commands.Delete, Key.Delete, ModifierKeys.None)
            };

            foreach (var binding in bindings) window.InputBindings.Add(binding);
            _bindings[window] = bindings;
        }

        public void Unregister(Window window)
        {
            if (!_bindings.Remove(window, out var bindings)) return;
            foreach (var binding in bindings) window.InputBindings.Remove(binding);
        }
    }
}
