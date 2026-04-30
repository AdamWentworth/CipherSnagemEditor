using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.App.ViewModels;
using CipherSnagemEditor.App.Views;

namespace CipherSnagemEditor.App;

public partial class App : Application
{
    private static GameCubeGame StartupGame { get; set; } = GameCubeGame.PokemonColosseum;

    public static void ConfigureStartupGame(GameCubeGame game)
    {
        StartupGame = game;
    }

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
                DataContext = new MainWindowViewModel(StartupGame),
            };

            var startupPath = GetStartupPath(desktop.Args);
            if (startupPath is not null && desktop.MainWindow is MainWindow mainWindow)
            {
                _ = mainWindow.OpenPathFromStartupAsync(startupPath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string? GetStartupPath(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].Equals("--iso", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            if (GameFileTypes.FromExtension(args[index]) is GameFileType.Iso
                or GameFileType.Fsys
                or GameFileType.Message
                or GameFileType.Gtx
                or GameFileType.Atx
                or GameFileType.Gsw)
            {
                return args[index];
            }
        }

        return null;
    }
}
