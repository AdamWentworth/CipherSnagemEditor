using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Text;
using CipherSnagemEditor.Colosseum.Data;
using System.Text.Json;

namespace CipherSnagemEditor.Colosseum;

public sealed partial class ColosseumProjectContext
{
    private const int NumberOfTrainerModels = 0x4b;
    private const int ModelDictionaryModelOffset = 0x04;
    private static readonly JsonSerializerOptions GameStringJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

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

    public GameCubeIso? Iso { get; private set; }

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
        var entryFileType = GameFileTypes.FromExtension(entry.Name);
        if (entryFileType == GameFileType.Fsys)
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
                decodedFiles.AddRange(DecodeWorkspaceBinaryFiles(folder, overwrite));
            }
        }
        else if (decode && entryFileType is GameFileType.Gtx or GameFileType.Atx)
        {
            var pngPath = targetPath + ".png";
            if ((!File.Exists(pngPath) || overwrite)
                && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(targetPath), out var pngBytes))
            {
                File.WriteAllBytes(pngPath, pngBytes);
                decodedFiles.Add(pngPath);
            }
        }
        else if (decode && entryFileType == GameFileType.Gsw)
        {
            decodedFiles.AddRange(DecodeGswTextures(targetPath, overwrite));
        }

        return new IsoExportResult(targetPath, extractedFiles, decodedFiles);
    }

    public IsoEncodeResult EncodeIsoFile(GameCubeIsoFileEntry entry)
        => PrepareWorkspaceIsoFile(entry, encodeDecodedFiles: true, packArchive: true);

    public IsoImportResult ImportIsoFile(GameCubeIsoFileEntry entry, bool encode)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        if (string.Equals(entry.Name, "Game.toc", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Replacing Game.toc directly is not supported.");
        }

        var encodeResult = PrepareWorkspaceIsoFile(
            entry,
            encodeDecodedFiles: encode,
            packArchive: GameFileTypes.FromExtension(entry.Name) == GameFileType.Fsys);
        var sourceBytes = File.ReadAllBytes(encodeResult.FilePath);
        var writeResult = WriteIsoEntry(entry, sourceBytes);
        return new IsoImportResult(
            encodeResult.FilePath,
            sourceBytes.Length,
            writeResult.MaximumBytes,
            writeResult.InsertedBytes,
            encodeResult);
    }

    public IsoDeleteResult DeleteIsoFile(GameCubeIsoFileEntry entry)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        if (string.Equals(entry.Name, "Start.dol", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.Name, "Game.toc", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"{entry.Name} cannot be deleted.");
        }

        var backupPath = ResolveIsoExtractPath(entry.Name, null);
        if (!File.Exists(backupPath))
        {
            backupPath = ExportIsoFile(entry, extractFsysContents: true, decode: false, overwrite: false).FilePath;
        }

        var replacement = GameFileTypes.FromExtension(entry.Name) == GameFileType.Fsys
            ? NullFsys()
            : "DELETED DELETED\0"u8.ToArray();
        if (replacement.Length > entry.Size)
        {
            throw new InvalidDataException($"{entry.Name} is too small to replace with the legacy deleted marker.");
        }

        WriteIsoEntry(entry, replacement);
        return new IsoDeleteResult(entry.Name, replacement.Length, backupPath);
    }

    public IsoFsysAddFileResult AddFileToIsoFsys(GameCubeIsoFileEntry entry, string sourcePath, ushort shortIdentifier)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        if (GameFileTypes.FromExtension(entry.Name) != GameFileType.Fsys)
        {
            throw new InvalidDataException($"{entry.Name} is not an FSYS archive.");
        }

        var targetPath = EnsureRawIsoWorkspaceFile(entry);
        var folder = GetIsoExportDirectory(entry.Name);
        Directory.CreateDirectory(folder);

        var archive = FsysArchive.Load(targetPath);
        var addResult = archive.AddFile(sourcePath, shortIdentifier, compress: true);
        File.WriteAllBytes(targetPath, addResult.ArchiveBytes);
        LoadedFsys[targetPath] = FsysArchive.Parse(targetPath, addResult.ArchiveBytes);
        LoadedFiles[entry.Name] = addResult.ArchiveBytes;

        var workspaceFilePath = Path.Combine(folder, addResult.EntryName);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(workspaceFilePath), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, workspaceFilePath, overwrite: true);
        }

        var importResult = ImportIsoFile(entry, encode: false);
        return new IsoFsysAddFileResult(
            targetPath,
            workspaceFilePath,
            addResult.EntryName,
            addResult.ShortIdentifier,
            addResult.SourceSize,
            addResult.ArchiveSize,
            addResult.Compressed,
            importResult);
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
        ColosseumShopRandomizerResult? shopResult = null;
        if (options.ShopItems)
        {
            shopResult = RandomizePocketMenuShops(commonRel);
            changes = changes
                .WithStartDol()
                .WithMessage($"Randomized {shopResult.ChangedItems} shop item slots.");
        }

        var writtenFiles = new List<string>();
        if (changes.CommonRelChanged)
        {
            writtenFiles.Add(SaveCommonRel(commonRel));
        }

        if (changes.StartDolChanged)
        {
            writtenFiles.Add(SaveStartDol(commonRel));
        }

        if (shopResult is not null)
        {
            writtenFiles.Add(shopResult.PocketMenuRelPath);
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
        var bytes = updatedTable.ToArray(allowGrowth: Settings.IncreaseFileSizes);
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

    private IsoEncodeResult PrepareWorkspaceIsoFile(
        GameCubeIsoFileEntry entry,
        bool encodeDecodedFiles,
        bool packArchive)
    {
        var targetPath = EnsureRawIsoWorkspaceFile(entry);
        var encodedFiles = new List<string>();
        var packedFiles = new List<string>();

        switch (GameFileTypes.FromExtension(entry.Name))
        {
            case GameFileType.Fsys:
            {
                var folder = GetIsoExportDirectory(entry.Name);
                Directory.CreateDirectory(folder);
                if (encodeDecodedFiles)
                {
                    encodedFiles.AddRange(EncodeDecodedMessageFiles(folder));
                    encodedFiles.AddRange(EncodeWorkspaceBinaryFiles(folder));
                }

                if (packArchive)
                {
                    var archive = FsysArchive.Load(targetPath);
                    var result = archive.ReplaceFilesFromDirectory(folder, encodeCompressed: true);
                    File.WriteAllBytes(targetPath, result.ArchiveBytes);
                    LoadedFsys[targetPath] = FsysArchive.Parse(targetPath, result.ArchiveBytes);
                    packedFiles.AddRange(result.ReplacedFiles.Select(file => file.SourcePath));
                }

                break;
            }

            case GameFileType.Message:
            {
                if (encodeDecodedFiles)
                {
                    var jsonPath = targetPath + ".json";
                    if (File.Exists(jsonPath))
                    {
                        EncodeMessageJson(jsonPath, targetPath);
                        encodedFiles.Add(jsonPath);
                    }
                }

                break;
            }

            case GameFileType.Gtx:
            case GameFileType.Atx:
            {
                if (encodeDecodedFiles)
                {
                    var pngPath = targetPath + ".png";
                    if (File.Exists(pngPath)
                        && GameCubeTextureCodec.TryImportPng(File.ReadAllBytes(targetPath), File.ReadAllBytes(pngPath), out var importedTexture))
                    {
                        File.WriteAllBytes(targetPath, importedTexture);
                        encodedFiles.Add(pngPath);
                    }
                }

                break;
            }

            case GameFileType.Gsw:
            {
                if (encodeDecodedFiles)
                {
                    encodedFiles.AddRange(EncodeGswTextures(targetPath));
                }

                break;
            }
        }

        return new IsoEncodeResult(targetPath, encodedFiles, packedFiles);
    }

    private string EnsureRawIsoWorkspaceFile(GameCubeIsoFileEntry entry)
    {
        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
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

    private IEnumerable<string> EncodeDecodedMessageFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var jsonPath in Directory.EnumerateFiles(folder, "*.msg.json", SearchOption.TopDirectoryOnly))
        {
            var messagePath = jsonPath[..^".json".Length];
            EncodeMessageJson(jsonPath, messagePath);
            yield return jsonPath;
        }
    }

    private void EncodeMessageJson(string jsonPath, string messagePath)
    {
        var strings = JsonSerializer.Deserialize<GameString[]>(File.ReadAllText(jsonPath), GameStringJsonOptions);
        if (strings is null)
        {
            throw new InvalidDataException($"Could not read message JSON: {jsonPath}");
        }

        var table = File.Exists(messagePath)
            ? GameStringTable.Parse(File.ReadAllBytes(messagePath)).WithStrings(strings)
            : GameStringTable.FromStrings(strings);
        var bytes = table.ToArray(allowGrowth: Settings.IncreaseFileSizes);
        File.WriteAllBytes(messagePath, bytes);
        LoadedStringTables[messagePath] = table;
        LoadedFiles[messagePath] = bytes;
    }

    private static IEnumerable<string> DecodeWorkspaceBinaryFiles(string folder, bool overwrite)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var fileType = GameFileTypes.FromExtension(filePath);
            if (fileType == GameFileType.Pkx)
            {
                var datPath = filePath + ".dat";
                if (File.Exists(datPath) && !overwrite)
                {
                    continue;
                }

                if (GameCubeLegacyFileCodecs.TryExportPkxDat(File.ReadAllBytes(filePath), out var dat))
                {
                    File.WriteAllBytes(datPath, dat);
                    yield return datPath;
                }
            }
            else if (fileType == GameFileType.Wzx)
            {
                foreach (var model in GameCubeLegacyFileCodecs.ExtractWzxDatModels(File.ReadAllBytes(filePath)))
                {
                    var modelPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}_{model.Index}.wzx.dat");
                    if (File.Exists(modelPath) && !overwrite)
                    {
                        continue;
                    }

                    File.WriteAllBytes(modelPath, model.Data);
                    yield return modelPath;
                }
            }
            else if (fileType == GameFileType.Thh)
            {
                var basePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(filePath));
                var bodyPath = basePath + GameFileTypes.ExtensionFor(GameFileType.Thd);
                if (!File.Exists(bodyPath))
                {
                    continue;
                }

                var thpPath = basePath + GameFileTypes.ExtensionFor(GameFileType.Thp);
                if (File.Exists(thpPath) && !overwrite)
                {
                    continue;
                }

                File.WriteAllBytes(
                    thpPath,
                    GameCubeLegacyFileCodecs.CombineThp(
                        File.ReadAllBytes(filePath),
                        File.ReadAllBytes(bodyPath)));
                yield return thpPath;
            }
            else if (fileType is GameFileType.Gtx or GameFileType.Atx)
            {
                var pngPath = filePath + ".png";
                if (File.Exists(pngPath) && !overwrite)
                {
                    continue;
                }

                if (GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(filePath), out var pngBytes))
                {
                    File.WriteAllBytes(pngPath, pngBytes);
                    yield return pngPath;
                }
            }
            else if (fileType == GameFileType.Gsw)
            {
                foreach (var decodedFile in DecodeGswTextures(filePath, overwrite))
                {
                    yield return decodedFile;
                }
            }
        }

        foreach (var modelPath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Dat or GameFileType.RoomData)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var texture in GameCubeDatTextureCodec.ExtractTextures(File.ReadAllBytes(modelPath)))
            {
                var texturePath = ModelTexturePath(modelPath, texture.Index);
                if (!File.Exists(texturePath) || overwrite)
                {
                    File.WriteAllBytes(texturePath, texture.TextureBytes);
                    yield return texturePath;
                }

                var pngPath = texturePath + ".png";
                if ((!File.Exists(pngPath) || overwrite)
                    && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(texturePath), out var pngBytes))
                {
                    File.WriteAllBytes(pngPath, pngBytes);
                    yield return pngPath;
                }
            }
        }
    }

    private static IEnumerable<string> EncodeWorkspaceBinaryFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var thpPath in Directory.EnumerateFiles(folder, "*.thp", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!GameCubeLegacyFileCodecs.TrySplitThp(File.ReadAllBytes(thpPath), out var header, out var body))
            {
                continue;
            }

            var basePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(thpPath));
            File.WriteAllBytes(basePath + GameFileTypes.ExtensionFor(GameFileType.Thh), header);
            File.WriteAllBytes(basePath + GameFileTypes.ExtensionFor(GameFileType.Thd), body);
            yield return thpPath;
        }

        foreach (var texturePath in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Gtx or GameFileType.Atx)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var pngPath = texturePath + ".png";
            if (!File.Exists(pngPath))
            {
                continue;
            }

            if (GameCubeTextureCodec.TryImportPng(
                File.ReadAllBytes(texturePath),
                File.ReadAllBytes(pngPath),
                out var importedTexture))
            {
                File.WriteAllBytes(texturePath, importedTexture);
                yield return pngPath;
            }
        }

        foreach (var gswPath in Directory.EnumerateFiles(folder, "*.gsw", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var importedTexturePath in EncodeGswTextures(gswPath))
            {
                yield return importedTexturePath;
            }
        }

        foreach (var modelPath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Dat or GameFileType.RoomData)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var replacements = GameCubeDatTextureCodec.ExtractTextures(File.ReadAllBytes(modelPath))
                .Select(texture => (texture.Index, Path: ModelTexturePath(modelPath, texture.Index)))
                .Where(texture => File.Exists(texture.Path))
                .ToDictionary(texture => texture.Index, texture => File.ReadAllBytes(texture.Path));
            if (replacements.Count == 0)
            {
                continue;
            }

            if (GameCubeDatTextureCodec.TryImportTextures(
                File.ReadAllBytes(modelPath),
                replacements,
                out var importedModel,
                out var importedCount)
                && importedCount > 0)
            {
                File.WriteAllBytes(modelPath, importedModel);
                yield return modelPath;
            }
        }

        foreach (var pkxPath in Directory.EnumerateFiles(folder, "*.pkx", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var datPath = pkxPath + ".dat";
            if (!File.Exists(datPath))
            {
                continue;
            }

            if (GameCubeLegacyFileCodecs.TryImportPkxDat(
                File.ReadAllBytes(pkxPath),
                File.ReadAllBytes(datPath),
                out var importedPkx))
            {
                File.WriteAllBytes(pkxPath, importedPkx);
                yield return datPath;
            }
        }

        foreach (var wzxPath in Directory.EnumerateFiles(folder, "*.wzx", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var wzxBytes = File.ReadAllBytes(wzxPath);
            var models = GameCubeLegacyFileCodecs.ExtractWzxDatModels(wzxBytes);
            var changed = false;
            foreach (var model in models)
            {
                var modelPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(wzxPath)}_{model.Index}.wzx.dat");
                if (!File.Exists(modelPath))
                {
                    continue;
                }

                if (!GameCubeLegacyFileCodecs.TryImportWzxDatModel(
                    wzxBytes,
                    model.Index,
                    File.ReadAllBytes(modelPath),
                    out var importedWzx))
                {
                    continue;
                }

                wzxBytes = importedWzx;
                changed = true;
                yield return modelPath;
            }

            if (changed)
            {
                File.WriteAllBytes(wzxPath, wzxBytes);
            }
        }
    }

    private IsoWriteResult WriteIsoEntry(GameCubeIsoFileEntry entry, byte[] sourceBytes)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        var maximumBytes = MaximumReplacementSize(entry);
        if (entry.TocEntryOffset is null && sourceBytes.Length > entry.Size)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = 0;
        using (var stream = File.Open(Iso.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            insertedBytes = EnsureIsoCapacity(stream, entry, sourceBytes.Length, maximumBytes);
            maximumBytes = checked(maximumBytes + (uint)insertedBytes);

            stream.Position = entry.Offset;
            stream.Write(sourceBytes);

            if (entry.Size > sourceBytes.Length)
            {
                WriteZeros(stream, checked((int)(entry.Size - sourceBytes.Length)));
            }

            if (entry.TocEntryOffset is not null)
            {
                Span<byte> sizeBytes = stackalloc byte[4];
                BigEndian.WriteUInt32(sizeBytes, 0, checked((uint)sourceBytes.Length));
                stream.Position = entry.TocEntryOffset.Value + 8;
                stream.Write(sizeBytes);
            }
        }

        LoadedFiles[entry.Name] = sourceBytes;
        Iso = GameCubeIsoReader.Open(Iso.Path);
        return new IsoWriteResult(maximumBytes, insertedBytes);
    }

    private uint MaximumReplacementSize(GameCubeIsoFileEntry entry)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No ISO is loaded.");
        }

        if (entry.TocEntryOffset is null)
        {
            return entry.Size;
        }

        var nextFile = Iso.Files
            .Where(file => file.Offset > entry.Offset)
            .OrderBy(file => file.Offset)
            .FirstOrDefault();
        var maxEnd = nextFile?.Offset ?? checked((uint)new FileInfo(Iso.Path).Length);
        return maxEnd <= entry.Offset ? entry.Size : maxEnd - entry.Offset;
    }

    private int EnsureIsoCapacity(FileStream stream, GameCubeIsoFileEntry entry, int sourceLength, uint currentMaximumBytes)
    {
        if (Iso is null || sourceLength <= currentMaximumBytes)
        {
            return 0;
        }

        if (entry.TocEntryOffset is null)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = Align16(checked(sourceLength - (int)currentMaximumBytes));
        var insertOffset = checked((long)entry.Offset + entry.Size);
        InsertZeros(stream, insertOffset, insertedBytes);

        Span<byte> offsetBytes = stackalloc byte[4];
        foreach (var shiftedEntry in Iso.Files
            .Where(file => file.TocEntryOffset is not null && file.Offset > entry.Offset)
            .OrderBy(file => file.Offset))
        {
            BigEndian.WriteUInt32(offsetBytes, 0, checked(shiftedEntry.Offset + (uint)insertedBytes));
            stream.Position = shiftedEntry.TocEntryOffset!.Value + 4;
            stream.Write(offsetBytes);
        }

        UpdateUserDataSize(stream);
        return insertedBytes;
    }

    private static void InsertZeros(FileStream stream, long offset, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var oldLength = stream.Length;
        if (offset < 0 || offset > oldLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} is outside the ISO.");
        }

        var buffer = new byte[1024 * 1024];
        stream.SetLength(oldLength + count);
        var readEnd = oldLength;
        while (readEnd > offset)
        {
            var readStart = Math.Max(offset, readEnd - buffer.Length);
            var length = checked((int)(readEnd - readStart));
            stream.Position = readStart;
            ReadExactly(stream, buffer.AsSpan(0, length));
            stream.Position = readStart + count;
            stream.Write(buffer, 0, length);
            readEnd = readStart;
        }

        stream.Position = offset;
        WriteZeros(stream, count);
    }

    private static void UpdateUserDataSize(FileStream stream)
    {
        const int userDataStartOffsetLocation = 0x434;
        const int userDataSizeLocation = 0x438;

        Span<byte> headerBytes = stackalloc byte[4];
        stream.Position = userDataStartOffsetLocation;
        ReadExactly(stream, headerBytes);
        var userDataStart = BigEndian.ReadUInt32(headerBytes, 0);
        if (userDataStart == 0 || userDataStart > stream.Length)
        {
            return;
        }

        BigEndian.WriteUInt32(headerBytes, 0, checked((uint)(stream.Length - userDataStart)));
        stream.Position = userDataSizeLocation;
        stream.Write(headerBytes);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer[read..]);
            if (count == 0)
            {
                throw new EndOfStreamException("Unexpected end of ISO while shifting file data.");
            }

            read += count;
        }
    }

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private static void WriteZeros(Stream stream, int count)
    {
        Span<byte> zeros = stackalloc byte[4096];
        while (count > 0)
        {
            var length = Math.Min(count, zeros.Length);
            stream.Write(zeros[..length]);
            count -= length;
        }
    }

    private static byte[] NullFsys()
        =>
        [
            0x46, 0x53, 0x59, 0x53, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x4E, 0x55, 0x4C, 0x4C, 0x46, 0x53, 0x59, 0x53
        ];

    private sealed record IsoWriteResult(uint MaximumBytes, int InsertedBytes);

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
        return type is GameFileType.Iso or GameFileType.Fsys or GameFileType.Message or GameFileType.Gtx or GameFileType.Atx or GameFileType.Gsw;
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
            GameFileType.Gtx or GameFileType.Atx or GameFileType.Gsw => OpenTexture(path),
            GameFileType.Nkit => throw new NotSupportedException("nkit ISO files are not supported. Convert to a regular ISO first."),
            _ => throw new NotSupportedException("Supported file types are .iso, .fsys, .msg, .gtx, .atx, and .gsw.")
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

    private static string ModelTexturePath(string modelPath, int textureIndex)
    {
        var directory = Path.GetDirectoryName(modelPath) ?? string.Empty;
        var fileName = Path.GetFileName(modelPath);
        var extensionIndex = fileName.IndexOf('.');
        var stem = extensionIndex < 0 ? fileName : fileName[..extensionIndex];
        var extensions = extensionIndex < 0 ? string.Empty : fileName[extensionIndex..];
        return Path.Combine(directory, $"{stem}_{textureIndex}{extensions}.gtx");
    }

    private static string GswTexturePath(string gswPath, int textureId)
    {
        var directory = Path.GetDirectoryName(gswPath) ?? string.Empty;
        var stem = RemoveFileExtensionsLikeLegacy(Path.GetFileName(gswPath));
        return Path.Combine(directory, $"{stem}_gsw_{textureId}.gtx");
    }

    private static IEnumerable<string> DecodeGswTextures(string gswPath, bool overwrite)
    {
        foreach (var texture in GameCubeGswTextureCodec.ExtractTextures(File.ReadAllBytes(gswPath)))
        {
            var texturePath = GswTexturePath(gswPath, texture.Id);
            if (!File.Exists(texturePath) || overwrite)
            {
                File.WriteAllBytes(texturePath, texture.TextureBytes);
                yield return texturePath;
            }

            var pngPath = texturePath + ".png";
            if ((!File.Exists(pngPath) || overwrite)
                && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(texturePath), out var pngBytes))
            {
                File.WriteAllBytes(pngPath, pngBytes);
                yield return pngPath;
            }
        }
    }

    private static IEnumerable<string> EncodeGswTextures(string gswPath)
    {
        var replacements = GameCubeGswTextureCodec.ExtractTextures(File.ReadAllBytes(gswPath))
            .Select(texture => (texture.Id, Path: GswTexturePath(gswPath, texture.Id)))
            .Where(texture => File.Exists(texture.Path))
            .ToDictionary(texture => texture.Id, texture => File.ReadAllBytes(texture.Path));
        if (replacements.Count == 0)
        {
            yield break;
        }

        if (GameCubeGswTextureCodec.TryImportTextures(
            File.ReadAllBytes(gswPath),
            replacements,
            out var importedGsw,
            out var importedCount)
            && importedCount > 0)
        {
            File.WriteAllBytes(gswPath, importedGsw);
            foreach (var texturePath in replacements.Keys.Select(id => GswTexturePath(gswPath, id)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return texturePath;
            }
        }
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
            var json = JsonSerializer.Serialize(table.Strings, GameStringJsonOptions);
            File.WriteAllText(jsonPath, json);
            yield return jsonPath;
        }
    }
}
