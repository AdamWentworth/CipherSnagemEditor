using OrreForge.Core.Archives;
using OrreForge.Core.Files;
using OrreForge.Core.GameCube;
using OrreForge.Core.Text;
using OrreForge.Colosseum.Data;
using System.Text.Json;

namespace OrreForge.Colosseum;

public sealed class ColosseumProjectContext
{
    private ColosseumProjectContext(
        string sourcePath,
        ColosseumSourceKind sourceKind,
        string? workspaceDirectory,
        GameCubeIso? iso,
        FsysArchive? fsysArchive,
        GameStringTable? messageTable,
        ColosseumSettings settings)
    {
        SourcePath = sourcePath;
        SourceKind = sourceKind;
        WorkspaceDirectory = workspaceDirectory;
        Iso = iso;
        FsysArchive = fsysArchive;
        MessageTable = messageTable;
        Settings = settings;
    }

    public string SourcePath { get; }

    public ColosseumSourceKind SourceKind { get; }

    public string? WorkspaceDirectory { get; }

    public GameCubeIso? Iso { get; }

    public FsysArchive? FsysArchive { get; }

    public GameStringTable? MessageTable { get; }

    public ColosseumSettings Settings { get; }

    public Dictionary<string, byte[]> LoadedFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, FsysArchive> LoadedFsys { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, GameStringTable> LoadedStringTables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ColosseumCommonRel? CommonRel { get; private set; }

    public string ExtractIsoFile(GameCubeIsoFileEntry entry, string? outputPath = null, bool overwrite = true)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        var targetPath = ResolveIsoExtractPath(entry.Name, outputPath);
        if (!overwrite && File.Exists(targetPath))
        {
            throw new IOException($"Refusing to overwrite existing file: {targetPath}");
        }

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        File.WriteAllBytes(targetPath, data);
        LoadedFiles[entry.Name] = data;
        return targetPath;
    }

    public IsoExportResult ExportIsoFile(
        GameCubeIsoFileEntry entry,
        bool extractFsysContents = true,
        bool decode = true,
        bool overwrite = false)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        if (!File.Exists(targetPath) || overwrite)
        {
            File.WriteAllBytes(targetPath, data);
        }

        LoadedFiles[entry.Name] = data;

        var extractedFiles = new List<string>();
        var decodedFiles = new List<string>();
        if (GameFileTypes.FromExtension(entry.Name) == GameFileType.Fsys)
        {
            var folder = GetIsoExportDirectory(entry.Name);
            var archive = FsysArchive.Parse(targetPath, data);
            LoadedFsys[targetPath] = archive;

            if (extractFsysContents)
            {
                extractedFiles.AddRange(ExtractFsysFiles(archive, folder, overwrite));
            }

            if (decode)
            {
                decodedFiles.AddRange(DecodeExtractedFsysFiles(archive, folder, overwrite));
            }
        }

        return new IsoExportResult(targetPath, extractedFiles, decodedFiles);
    }

    public FsysArchive ReadIsoFsysArchive(GameCubeIsoFileEntry entry)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        if (GameFileTypes.FromExtension(entry.Name) != GameFileType.Fsys)
        {
            throw new InvalidDataException($"{entry.Name} is not an FSYS archive.");
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        var archive = FsysArchive.Parse(targetPath, data);
        LoadedFsys[targetPath] = archive;
        return archive;
    }

    public byte[] LoadStartDol()
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        if (LoadedFiles.TryGetValue("Start.dol", out var cached))
        {
            return cached;
        }

        var dolEntry = Iso.Files.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.Name), "Start.dol", StringComparison.OrdinalIgnoreCase));
        if (dolEntry is null)
        {
            throw new InvalidDataException("Could not find Start.dol in the ISO.");
        }

        var bytes = GameCubeIsoReader.ReadFile(Iso, dolEntry);
        LoadedFiles["Start.dol"] = bytes;
        return bytes;
    }

    public ColosseumCommonRel LoadCommonRel()
    {
        if (CommonRel is not null)
        {
            return CommonRel;
        }

        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonFsysEntry = Iso.Files.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.Name), "common.fsys", StringComparison.OrdinalIgnoreCase));
        if (commonFsysEntry is null)
        {
            throw new InvalidDataException("Could not find common.fsys in the ISO.");
        }

        var commonFsysBytes = GameCubeIsoReader.ReadFile(Iso, commonFsysEntry);
        var commonArchive = FsysArchive.Parse(ResolveIsoExtractPath(commonFsysEntry.Name, null), commonFsysBytes);
        LoadedFsys[commonFsysEntry.Name] = commonArchive;

        var commonRelEntry = commonArchive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, "common.rel", StringComparison.OrdinalIgnoreCase));
        if (commonRelEntry is null)
        {
            throw new InvalidDataException("Could not find common.rel inside common.fsys.");
        }

        var commonRelBytes = commonArchive.Extract(commonRelEntry);
        LoadedFiles["common.rel"] = commonRelBytes;
        CommonRel = ColosseumCommonRel.Parse(Iso.Region, commonRelBytes, LoadStartDol());
        return CommonRel;
    }

    public IReadOnlyList<ColosseumTrainer> LoadStoryTrainers()
        => LoadCommonRel().LoadStoryTrainers();

    public string SaveTrainerPokemon(IEnumerable<ColosseumTrainerPokemonUpdate> updates)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        foreach (var update in updates)
        {
            commonRel.WriteTrainerPokemon(update);
        }

        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    public string GetIsoExportDirectory(string fileName)
    {
        if (WorkspaceDirectory is null)
        {
            throw new InvalidOperationException("No Colosseum workspace is available.");
        }

        var safeFileName = SafeFileName(fileName);
        var folderName = RemoveFileExtensionsLikeLegacy(safeFileName);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = safeFileName;
        }

        return Path.Combine(WorkspaceDirectory, "Game Files", folderName);
    }

    public static bool IsSupportedPath(string path)
    {
        var type = GameFileTypes.FromExtension(path);
        return type is GameFileType.Iso or GameFileType.Fsys or GameFileType.Message or GameFileType.Gtx or GameFileType.Atx;
    }

    public static ColosseumProjectContext Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input file does not exist.", path);
        }

        return GameFileTypes.FromExtension(path) switch
        {
            GameFileType.Iso => OpenIso(path),
            GameFileType.Fsys => OpenFsys(path),
            GameFileType.Message => OpenMessage(path),
            GameFileType.Gtx or GameFileType.Atx => OpenTexture(path),
            GameFileType.Nkit => throw new NotSupportedException("nkit ISO files are not supported. Convert to a regular ISO first."),
            _ => throw new NotSupportedException("Supported file types are .iso, .fsys, .msg, .gtx, and .atx.")
        };
    }

    private static ColosseumProjectContext OpenIso(string path)
    {
        var iso = GameCubeIsoReader.Open(path);
        if (!iso.IsPokemonColosseum)
        {
            throw new InvalidDataException($"Expected Pokemon Colosseum ISO GC6E/GC6P/GC6J, found {iso.GameId}.");
        }

        Directory.CreateDirectory(iso.WorkspaceDirectory);
        var settings = ColosseumSettings.LoadOrCreate(iso.WorkspaceDirectory);
        return new ColosseumProjectContext(path, ColosseumSourceKind.Iso, iso.WorkspaceDirectory, iso, null, null, settings);
    }

    private static ColosseumProjectContext OpenFsys(string path)
    {
        var archive = FsysArchive.Load(path);
        return new ColosseumProjectContext(path, ColosseumSourceKind.Fsys, null, null, archive, null, new ColosseumSettings());
    }

    private static ColosseumProjectContext OpenMessage(string path)
    {
        var table = GameStringTable.Load(path);
        return new ColosseumProjectContext(path, ColosseumSourceKind.Message, null, null, null, table, new ColosseumSettings());
    }

    private static ColosseumProjectContext OpenTexture(string path)
        => new(path, ColosseumSourceKind.Texture, null, null, null, null, new ColosseumSettings());

    private string ResolveIsoExtractPath(string fileName, string? outputPath)
    {
        var safeFileName = SafeFileName(fileName);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(GetIsoExportDirectory(safeFileName), safeFileName);
        }

        if (Directory.Exists(outputPath)
            || outputPath.EndsWith(Path.DirectorySeparatorChar)
            || outputPath.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return Path.Combine(outputPath, safeFileName);
        }

        return outputPath;
    }

    private static string SafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeFileName) ? fileName : safeFileName;
    }

    private static string RemoveFileExtensionsLikeLegacy(string fileName)
    {
        var extensionIndex = fileName.IndexOf('.');
        return extensionIndex < 0 ? fileName : fileName[..extensionIndex];
    }

    private static IEnumerable<string> ExtractFsysFiles(FsysArchive archive, string folder, bool overwrite)
    {
        Directory.CreateDirectory(folder);

        foreach (var entry in archive.Entries)
        {
            var outputPath = Path.Combine(folder, SafeFileName(entry.Name));
            if (File.Exists(outputPath) && !overwrite)
            {
                continue;
            }

            File.WriteAllBytes(outputPath, archive.Extract(entry));
            yield return outputPath;
        }
    }

    private IEnumerable<string> DecodeExtractedFsysFiles(FsysArchive archive, string folder, bool overwrite)
    {
        foreach (var entry in archive.Entries)
        {
            if (entry.FileType != GameFileType.Message)
            {
                continue;
            }

            var messagePath = Path.Combine(folder, SafeFileName(entry.Name));
            if (!File.Exists(messagePath))
            {
                continue;
            }

            var jsonPath = messagePath + ".json";
            if (File.Exists(jsonPath) && !overwrite)
            {
                continue;
            }

            GameStringTable table;
            try
            {
                table = GameStringTable.Load(messagePath);
            }
            catch (InvalidDataException)
            {
                continue;
            }
            catch (EndOfStreamException)
            {
                continue;
            }

            LoadedStringTables[messagePath] = table;
            var json = JsonSerializer.Serialize(table.Strings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, json);
            yield return jsonPath;
        }
    }
}
