namespace CipherSnagemEditor.Tests;

public sealed class ToolLauncherContractTests
{
    [Fact]
    public void SolutionCarriesSeparateToolLaunchers()
    {
        var solution = File.ReadAllText(Path.Combine(RepoRoot, "CipherSnagemEditor.slnx"));

        Assert.Contains("CipherSnagemEditor.ColosseumTool", solution, StringComparison.Ordinal);
        Assert.Contains("CipherSnagemEditor.GoDTool", solution, StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchersForceTheirLegacyGameModes()
    {
        var colosseumProgram = File.ReadAllText(Path.Combine(RepoRoot, "src", "CipherSnagemEditor.ColosseumTool", "Program.cs"));
        var godProgram = File.ReadAllText(Path.Combine(RepoRoot, "src", "CipherSnagemEditor.GoDTool", "Program.cs"));

        Assert.Contains("ConfigureStartupGame(GameCubeGame.PokemonColosseum)", colosseumProgram, StringComparison.Ordinal);
        Assert.Contains("ConfigureStartupGame(GameCubeGame.PokemonXD)", godProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedAvaloniaProjectIsNotThePublishedExecutable()
    {
        var appProject = File.ReadAllText(Path.Combine(RepoRoot, "src", "CipherSnagemEditor.App", "CipherSnagemEditor.App.csproj"));

        Assert.DoesNotContain("<OutputType>WinExe</OutputType>", appProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<ApplicationIcon>", appProject, StringComparison.Ordinal);
    }

    private static string RepoRoot
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
