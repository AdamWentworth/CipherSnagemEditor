namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeIsoFileEntry(string Name, uint Offset, uint Size, uint? TocEntryOffset = null)
{
    public string DiscPath { get; init; } = Name;
}
