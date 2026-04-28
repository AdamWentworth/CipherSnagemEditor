namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeIso(
    string Path,
    string GameId,
    GameCubeRegion Region,
    IReadOnlyList<GameCubeIsoFileEntry> Files)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    public string WorkspaceDirectory => System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(Path) ?? Environment.CurrentDirectory,
        $"{NameWithoutExtension} CM Tool");

    public bool IsPokemonColosseum => GameId is "GC6E" or "GC6P" or "GC6J";
}
