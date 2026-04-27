namespace OrreForge.Core.Files;

public static class GameFileTypes
{
    private static readonly Dictionary<string, GameFileType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".atx"] = GameFileType.Atx,
        [".bin"] = GameFileType.Bin,
        [".ccd"] = GameFileType.Collision,
        [".cms"] = GameFileType.Cms,
        [".dat"] = GameFileType.Dat,
        [".dats"] = GameFileType.DatSet,
        [".dol"] = GameFileType.Dol,
        [".fsys"] = GameFileType.Fsys,
        [".gci"] = GameFileType.Gci,
        [".gtx"] = GameFileType.Gtx,
        [".iso"] = GameFileType.Iso,
        [".lzss"] = GameFileType.Lzss,
        [".msg"] = GameFileType.Message,
        [".nkit"] = GameFileType.Nkit,
        [".pkx"] = GameFileType.Pkx,
        [".png"] = GameFileType.Png,
        [".raw"] = GameFileType.Raw,
        [".rdat"] = GameFileType.RoomData,
        [".rel"] = GameFileType.Rel,
        [".scd"] = GameFileType.Script,
        [".toc"] = GameFileType.Toc,
        [".txt"] = GameFileType.Text,
        [".wzx"] = GameFileType.Wzx
    };

    public static GameFileType FromExtension(string path)
    {
        var extension = Path.GetExtension(path);

        if (path.EndsWith(".nkit.iso", StringComparison.OrdinalIgnoreCase))
        {
            return GameFileType.Nkit;
        }

        return ExtensionMap.TryGetValue(extension, out var type) ? type : GameFileType.Unknown;
    }

    public static GameFileType FromFsysIdentifier(uint identifier)
    {
        var rawType = (identifier & 0x0000ff00) >> 8;
        return Enum.IsDefined(typeof(GameFileType), (int)rawType)
            ? (GameFileType)rawType
            : GameFileType.Unknown;
    }

    public static string ExtensionFor(GameFileType type) => type switch
    {
        GameFileType.Atx => ".atx",
        GameFileType.Bin => ".bin",
        GameFileType.Collision => ".ccd",
        GameFileType.Cms => ".cms",
        GameFileType.Dat => ".dat",
        GameFileType.DatSet => ".dats",
        GameFileType.Dol => ".dol",
        GameFileType.Fsys => ".fsys",
        GameFileType.Gci => ".gci",
        GameFileType.Gtx => ".gtx",
        GameFileType.Iso => ".iso",
        GameFileType.Lzss => ".lzss",
        GameFileType.Message => ".msg",
        GameFileType.Pkx => ".pkx",
        GameFileType.Png => ".png",
        GameFileType.Raw => ".raw",
        GameFileType.Rel => ".rel",
        GameFileType.RoomData => ".rdat",
        GameFileType.Script => ".scd",
        GameFileType.Toc => ".toc",
        GameFileType.Text => ".txt",
        GameFileType.Wzx => ".wzx",
        _ => ".bin"
    };
}
