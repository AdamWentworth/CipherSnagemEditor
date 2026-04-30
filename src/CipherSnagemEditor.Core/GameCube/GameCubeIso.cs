namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeIso(
    string Path,
    string GameId,
    GameCubeRegion Region,
    IReadOnlyList<GameCubeIsoFileEntry> Files)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    public GameCubeGame Game => GameId switch
    {
        "GC6E" or "GC6P" or "GC6J" => GameCubeGame.PokemonColosseum,
        "GXXE" or "GXXP" or "GXXJ" => GameCubeGame.PokemonXD,
        _ => GameCubeGame.Unknown
    };

    public string LegacyToolName => Game switch
    {
        GameCubeGame.PokemonColosseum => "Colosseum Tool",
        GameCubeGame.PokemonXD => "GoD Tool",
        _ => "Cipher Snagem Editor"
    };

    public string WorkspaceDirectory => System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(Path) ?? Environment.CurrentDirectory,
        $"{NameWithoutExtension} {WorkspaceSuffix}");

    private string WorkspaceSuffix => Game switch
    {
        GameCubeGame.PokemonColosseum => "CM Tool",
        GameCubeGame.PokemonXD => "GoD Tool",
        _ => "Cipher Snagem Editor"
    };

    public bool IsPokemonColosseum => GameId is "GC6E" or "GC6P" or "GC6J";

    public bool IsPokemonXD => GameId is "GXXE" or "GXXP" or "GXXJ";

    public bool IsSupportedPokemonGame => IsPokemonColosseum || IsPokemonXD;
}
