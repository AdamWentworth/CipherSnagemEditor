using Avalonia;
using CipherSnagemEditor.Core.GameCube;
using ToolApplication = CipherSnagemEditor.App.App;

namespace CipherSnagemEditor.GoDTool;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ToolApplication.ConfigureStartupGame(GameCubeGame.PokemonXD);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<ToolApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
