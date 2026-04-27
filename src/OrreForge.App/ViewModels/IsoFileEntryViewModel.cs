using OrreForge.Core.GameCube;

namespace OrreForge.App.ViewModels;

public sealed class IsoFileEntryViewModel
{
    public IsoFileEntryViewModel(GameCubeIsoFileEntry entry)
    {
        Entry = entry;
    }

    public GameCubeIsoFileEntry Entry { get; }

    public string Name => Entry.Name;

    public string OffsetHex => $"0x{Entry.Offset:x8}";

    public string SizeText => $"{Entry.Size:N0} bytes";
}
