using System.Windows;
using System.Windows.Input;
using ImageConverterPro.Services;
using ImageConverterPro.ViewModels;

namespace ImageConverterPro;

public partial class MainWindow : Window
{
    private readonly IShortcutService _shortcutService;
    private readonly IWindowStateService _windowStateService;
    private readonly IApplicationShellService _applicationShellService;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = ServiceLocator.GetService<MainViewModel>();
        _shortcutService = ServiceLocator.GetService<IShortcutService>();
        _windowStateService = ServiceLocator.GetService<IWindowStateService>();
        _applicationShellService = ServiceLocator.GetService<IApplicationShellService>();

        DataContext = _viewModel;

        _shortcutService.Register(this, new ShortcutCommandSet
        {
            Open = _viewModel.OpenFilesCommand,
            Save = _viewModel.SaveCurrentCommand,
            Undo = _viewModel.UndoCommand,
            Redo = _viewModel.RedoCommand,
            Delete = _viewModel.DeleteSelectedCommand
        });

        _windowStateService.Restore(this);

        Closing += MainWindow_Closing;
        Closed += (_, _) => _shortcutService.Unregister(this);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
            return;

        if (e.ClickCount == 2)
            Maximize_Click(sender, e);
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => _applicationShellService.RequestExit();

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.ConvertViewModel.IsConverting &&
            !_applicationShellService.ConfirmExitDuringConversion())
        {
            e.Cancel = true;
            return;
        }

        _windowStateService.Save(this);
    }
}
