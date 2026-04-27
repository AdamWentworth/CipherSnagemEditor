using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using OrreForge.App.ViewModels;
using OrreForge.App.Views;

namespace OrreForge.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            if (desktop.Args is { Length: > 0 } && desktop.MainWindow.DataContext is MainWindowViewModel viewModel)
            {
                _ = viewModel.OpenPathAsync(desktop.Args[0]);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
