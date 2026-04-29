using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Text;
using CipherSnagemEditor.Colosseum.Data;
using System.Text.Json;

namespace CipherSnagemEditor.Colosseum;

public sealed class ColosseumProjectContext
{
    private const int NumberOfTrainerModels = 0x4b;
    private const int ModelDictionaryModelOffset = 0x04;

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

    private IReadOnlyDictionary<int, string>? TrainerModelNames { get; set; }

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
        CommonRel = ColosseumCommonRel.Parse(
            Iso.Region,
            commonRelBytes,
            LoadStartDol(),
            LoadPocketMenuStringTable(),
            BuildTrainerModelNames());
        return CommonRel;
    }

    public IReadOnlyList<ColosseumTrainer> LoadStoryTrainers()
        => LoadCommonRel().LoadStoryTrainers();

    public IReadOnlyList<ColosseumPokemonStats> LoadPokemonStats()
        => LoadCommonRel().PokemonStats;

    public IReadOnlyList<ColosseumTypeData> LoadTypes()
        => LoadCommonRel().TypeData;

    public IReadOnlyList<ColosseumMove> LoadMoves()
        => LoadCommonRel().Moves;

    public IReadOnlyList<ColosseumItem> LoadItems()
        => LoadCommonRel().ItemData;

    public IReadOnlyList<ColosseumGiftPokemon> LoadGiftPokemon()
        => LoadCommonRel().GiftPokemon;

    public IReadOnlyList<ColosseumTreasure> LoadTreasures()
        => LoadCommonRel().Treasures;

    public IReadOnlyList<ColosseumInteractionPoint> LoadInteractionPoints()
        => LoadCommonRel().InteractionPoints;

    public IReadOnlyList<ColosseumCollisionFile> LoadCollisionFiles()
    {
        if (WorkspaceDirectory is null)
        {
            return [];
        }

        var gameFilesDirectory = Path.Combine(WorkspaceDirectory, "Game Files");
        if (!Directory.Exists(gameFilesDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(gameFilesDirectory, "*.col", SearchOption.AllDirectories)
            .Select(path =>
            {
                var fileName = Path.GetFileName(path);
                var mapCode = fileName.Length >= 2 ? fileName[..2] : string.Empty;
                return new ColosseumCollisionFile(path, fileName, mapCode, MapNameForCode(mapCode));
            })
            .OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ColosseumCollisionData LoadCollisionData(ColosseumCollisionFile file)
        => ColosseumCollisionData.Parse(File.ReadAllBytes(file.Path));

    public IReadOnlyList<ColosseumVertexFilterFile> LoadVertexFilterFiles()
    {
        if (WorkspaceDirectory is null)
        {
            return [];
        }

        var gameFilesDirectory = Path.Combine(WorkspaceDirectory, "Game Files");
        if (!Directory.Exists(gameFilesDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(gameFilesDirectory, "*.wzx.dat", SearchOption.AllDirectories)
            .Select(path => new ColosseumVertexFilterFile(
                path,
                Path.GetFileName(path),
                Path.GetRelativePath(gameFilesDirectory, path)))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ColosseumVertexFilterApplyResult ApplyVertexFilter(
        ColosseumVertexFilterFile file,
        ColosseumVertexColorFilter filter)
    {
        if (!File.Exists(file.Path))
        {
            throw new FileNotFoundException($"Vertex filter file not found: {file.Path}", file.Path);
        }

        var model = ColosseumDatVertexColorModel.Load(file.Path);
        var count = model.ApplyFilter(filter);
        model.Save(file.Path);
        return new ColosseumVertexFilterApplyResult(
            file.FileName,
            ColosseumDatVertexColorModel.FilterName(filter),
            count);
    }

    private IReadOnlyDictionary<int, string> BuildTrainerModelNames()
    {
        if (TrainerModelNames is not null)
        {
            return TrainerModelNames;
        }

        if (Iso is null)
        {
            return new Dictionary<int, string>();
        }

        var dol = new BinaryData(LoadStartDol());
        var modelTableOffset = TrainerPkxIdentifierOffset(Iso.Region);
        if (modelTableOffset <= 0 || modelTableOffset >= dol.Length)
        {
            return new Dictionary<int, string>();
        }

        var pkxNamesByIdentifier = new Dictionary<uint, string>();
        foreach (var entry in Iso.Files)
        {
            if (!entry.Name.Contains("pkx", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(entry.Name), ".fsys", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var archive = FsysArchive.Parse(entry.Name, GameCubeIsoReader.ReadFile(Iso, entry));
                var firstEntry = archive.Entries.FirstOrDefault();
                if (firstEntry is not null)
                {
                    pkxNamesByIdentifier[firstEntry.Identifier] = Path.GetFileNameWithoutExtension(entry.Name);
                }
            }
            catch (InvalidDataException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        var names = new Dictionary<int, string>();
        for (var modelId = 0; modelId < NumberOfTrainerModels; modelId++)
        {
            var offset = modelTableOffset + (modelId * 12) + ModelDictionaryModelOffset;
            if (offset + 4 > dol.Length)
            {
                break;
            }

            var identifier = dol.ReadUInt32(offset);
            if (pkxNamesByIdentifier.TryGetValue(identifier, out var name))
            {
                names[modelId] = name;
            }
        }

        TrainerModelNames = names;
        return names;
    }

    private static int TrainerPkxIdentifierOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.Japan => 0x359fa8,
            GameCubeRegion.UnitedStates => 0x36d840,
            GameCubeRegion.Europe => 0x3ba938,
            _ => 0
        };

    private static string MapNameForCode(string code)
        => code switch
        {
            "B1" => "Demo Area",
            "D1" => "Shadow Pokemon Lab",
            "D2" => "Mt. Battle",
            "D3" => "S.S. Libra",
            "D4" => "Realgam Tower",
            "D5" => "Cipher Key Lair",
            "D6" => "Citadark Isle",
            "D7" => "Orre Colosseum",
            "M1" => "Phenac City",
            "M2" => "Pyrite Town",
            "M3" => "Agate Village",
            "M4" => "The Under",
            "M5" => "Pokemon HQ Lab",
            "M6" => "Gateon Port",
            "S1" => "Outskirt Stand",
            "S2" => "Team Snagem's Hideout",
            "S3" => "Kaminko's House",
            "T1" => "Ancient Colosseum",
            "es" => "Pokespot",
            _ => string.Empty
        };

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

    public string SavePokemonStats(ColosseumPokemonStatsUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WritePokemonStats(update);

        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    public string SaveMove(ColosseumMoveUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteMove(update);

        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    public string SaveItem(ColosseumItemUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteItem(update);

        var targetPath = ResolveIsoExtractPath("Start.dol", null);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.StartDolToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["Start.dol"] = bytes;
        return targetPath;
    }

    public string SaveType(ColosseumTypeUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteType(update);
        return SaveStartDol(commonRel);
    }

    public string SaveGiftPokemon(ColosseumGiftPokemonUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteGiftPokemon(update);
        return SaveStartDol(commonRel);
    }

    public string SaveTreasure(ColosseumTreasureUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteTreasure(update);

        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    public string SaveInteractionPoint(ColosseumInteractionPointUpdate update)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        commonRel.WriteInteractionPoint(update);

        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    public ColosseumPatchApplyResult ApplyPatch(ColosseumPatchKind patchKind)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var definition = ColosseumPatchDefinition.ForKind(patchKind);
        var commonRel = LoadCommonRel();
        var changes = commonRel.ApplyPatch(patchKind);
        var writtenFiles = new List<string>();
        if (changes.CommonRelChanged)
        {
            writtenFiles.Add(SaveCommonRel(commonRel));
        }

        if (changes.StartDolChanged)
        {
            writtenFiles.Add(SaveStartDol(commonRel));
        }

        return new ColosseumPatchApplyResult(definition, writtenFiles, changes.Messages);
    }

    public ColosseumRandomizerApplyResult Randomize(ColosseumRandomizerOptions options)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Colosseum ISO is loaded.");
        }

        var commonRel = LoadCommonRel();
        var changes = commonRel.Randomize(options);
        var writtenFiles = new List<string>();
        if (changes.CommonRelChanged)
        {
            writtenFiles.Add(SaveCommonRel(commonRel));
        }

        if (changes.StartDolChanged)
        {
            writtenFiles.Add(SaveStartDol(commonRel));
        }

        return new ColosseumRandomizerApplyResult(writtenFiles, changes.Messages);
    }

    public IReadOnlyList<ColosseumMessageTable> LoadMessageTables()
    {
        if (MessageTable is not null)
        {
            return
            [
                new ColosseumMessageTable(
                    Path.GetFileName(SourcePath),
                    SourcePath,
                    Path.GetFileName(SourcePath),
                    MessageTable.Strings.Select(message => new ColosseumMessageString(
                        message.Id,
                        $"0x{message.Id:X}",
                        message.Text)).ToArray())
            ];
        }

        if (Iso is null)
        {
            return [];
        }

        var tables = new List<ColosseumMessageTable>();
        foreach (var isoEntry in Iso.Files.Where(entry => string.Equals(Path.GetExtension(entry.Name), ".fsys", StringComparison.OrdinalIgnoreCase)))
        {
            FsysArchive archive;
            try
            {
                archive = FsysArchive.Parse(isoEntry.Name, GameCubeIsoReader.ReadFile(Iso, isoEntry));
            }
            catch (InvalidDataException)
            {
                continue;
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }
            catch (EndOfStreamException)
            {
                continue;
            }

            foreach (var messageEntry in archive.Entries.Where(entry => entry.FileType == GameFileType.Message))
            {
                try
                {
                    var savedMessagePath = WorkspaceDirectory is null
                        ? null
                        : Path.Combine(GetIsoExportDirectory(isoEntry.Name), SafeFileName(messageEntry.Name));
                    var tableBytes = savedMessagePath is not null && File.Exists(savedMessagePath)
                        ? File.ReadAllBytes(savedMessagePath)
                        : archive.Extract(messageEntry);
                    var table = GameStringTable.Parse(tableBytes);
                    var strings = table.Strings
                        .Select(message => new ColosseumMessageString(message.Id, $"0x{message.Id:X}", message.Text))
                        .ToArray();
                    tables.Add(new ColosseumMessageTable(
                        $"{Path.GetFileNameWithoutExtension(isoEntry.Name)}/{messageEntry.Name}",
                        isoEntry.Name,
                        messageEntry.Name,
                        strings));
                }
                catch (InvalidDataException)
                {
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (EndOfStreamException)
                {
                }
            }
        }

        return tables.OrderBy(table => table.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public ColosseumMessageTable SaveMessageString(ColosseumMessageTable table, int id, string text)
    {
        var sourceBytes = LoadMessageTableBytes(table, out var targetPath);
        var updatedTable = GameStringTable.Parse(sourceBytes).WithString(id, text);
        var bytes = updatedTable.ToArray();
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllBytes(targetPath, bytes);
        LoadedStringTables[targetPath] = updatedTable;

        return new ColosseumMessageTable(
            table.DisplayName,
            table.IsoFileName,
            table.EntryName,
            updatedTable.Strings.Select(message => new ColosseumMessageString(
                message.Id,
                $"0x{message.Id:X}",
                message.Text)).ToArray());
    }

    private byte[] LoadMessageTableBytes(ColosseumMessageTable table, out string targetPath)
    {
        if (MessageTable is not null
            && string.Equals(table.IsoFileName, SourcePath, StringComparison.OrdinalIgnoreCase))
        {
            targetPath = SourcePath;
            return File.ReadAllBytes(SourcePath);
        }

        if (Iso is null)
        {
            throw new InvalidOperationException("No message table is selected.");
        }

        targetPath = Path.Combine(GetIsoExportDirectory(table.IsoFileName), SafeFileName(table.EntryName));
        if (File.Exists(targetPath))
        {
            return File.ReadAllBytes(targetPath);
        }

        var isoEntry = Iso.Files.FirstOrDefault(entry =>
            string.Equals(entry.Name, table.IsoFileName, StringComparison.OrdinalIgnoreCase));
        if (isoEntry is null)
        {
            throw new FileNotFoundException($"Could not find {table.IsoFileName} in the ISO.");
        }

        var archive = FsysArchive.Parse(isoEntry.Name, GameCubeIsoReader.ReadFile(Iso, isoEntry));
        var messageEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, table.EntryName, StringComparison.OrdinalIgnoreCase));
        if (messageEntry is null)
        {
            throw new FileNotFoundException($"Could not find {table.EntryName} in {table.IsoFileName}.");
        }

        return archive.Extract(messageEntry);
    }

    private string SaveStartDol(ColosseumCommonRel commonRel)
    {
        var targetPath = ResolveIsoExtractPath("Start.dol", null);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.StartDolToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["Start.dol"] = bytes;
        return targetPath;
    }

    private string SaveCommonRel(ColosseumCommonRel commonRel)
    {
        var targetPath = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? WorkspaceDirectory!);
        var bytes = commonRel.ToArray();
        File.WriteAllBytes(targetPath, bytes);
        LoadedFiles["common.rel"] = bytes;
        return targetPath;
    }

    private GameStringTable? LoadPocketMenuStringTable()
    {
        if (Iso is null)
        {
            return null;
        }

        var pocketEntry = Iso.Files.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.Name), "pocket_menu.fsys", StringComparison.OrdinalIgnoreCase));
        if (pocketEntry is null)
        {
            return null;
        }

        try
        {
            var archive = FsysArchive.Parse(pocketEntry.Name, GameCubeIsoReader.ReadFile(Iso, pocketEntry));
            var messageEntry = archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.Name, "pocket_menu.msg", StringComparison.OrdinalIgnoreCase));
            if (messageEntry is null)
            {
                return null;
            }

            return GameStringTable.Parse(archive.Extract(messageEntry));
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
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
