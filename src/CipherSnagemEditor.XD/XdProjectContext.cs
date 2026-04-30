using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.XD;

public sealed class XdProjectContext
{
    private XdProjectContext(
        string sourcePath,
        XdSourceKind sourceKind,
        string workspaceDirectory,
        GameCubeIso iso,
        XdSettings settings)
    {
        SourcePath = sourcePath;
        SourceKind = sourceKind;
        WorkspaceDirectory = workspaceDirectory;
        Iso = iso;
        Settings = settings;
    }

    public string SourcePath { get; }

    public XdSourceKind SourceKind { get; }

    public string WorkspaceDirectory { get; }

    public GameCubeIso Iso { get; }

    public XdSettings Settings { get; }

    public static bool IsSupportedPath(string path)
        => GameFileTypes.FromExtension(path) == GameFileType.Iso;

    public static XdProjectContext Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input file does not exist.", path);
        }

        return GameFileTypes.FromExtension(path) switch
        {
            GameFileType.Iso => OpenIso(path),
            GameFileType.Nkit => throw new NotSupportedException("nkit ISO files are not supported. Convert to a regular ISO first."),
            _ => throw new NotSupportedException("XD mode currently opens .iso files. Standalone XD file workflows will be ported with the XD editors.")
        };
    }

    private static XdProjectContext OpenIso(string path)
    {
        var iso = GameCubeIsoReader.Open(path);
        if (!iso.IsPokemonXD)
        {
            throw new InvalidDataException($"Expected Pokemon XD ISO GXXE/GXXP/GXXJ, found {iso.GameId}.");
        }

        Directory.CreateDirectory(iso.WorkspaceDirectory);
        var settings = XdSettings.LoadOrCreate(iso.WorkspaceDirectory);
        return new XdProjectContext(path, XdSourceKind.Iso, iso.WorkspaceDirectory, iso, settings);
    }
}
