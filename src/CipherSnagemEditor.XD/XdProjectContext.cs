using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
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

    public GameCubeIso Iso { get; private set; }

    public XdSettings Settings { get; }

    public XdToolContent BuildToolContent(string title)
    {
        var sections = new List<XdToolSection>
        {
            BuildProjectSection()
        };

        sections.AddRange(title switch
        {
            "Trainer Editor" => BuildDeckEditorSections(includeShadowDeck: false),
            "Shadow Pokemon Editor" => BuildShadowPokemonSections(),
            "Pokemon Stats Editor" => BuildCommonRelSections("Pokemon stats table", ["PokemonData", "NumberOfPokemonData", "PokemonTable.swift"]),
            "Move Editor" => BuildCommonRelSections("Moves table", ["Moves", "MoveData", "MovesTable.swift"]),
            "Item Editor" => BuildCommonRelSections("Items and valid-items tables", ["ValidItems", "Items", "ItemsTable.swift"]),
            "Pokespot Editor" => BuildPokespotSections(),
            "Gift Pokemon Editor" => BuildCommonRelSections("Gift Pokemon tables", ["LegendaryPokemon", "MasterBallPokemon", "XGGiftPokemon.swift"]),
            "Type Editor" => BuildCommonRelSections("Type matchup table", ["TypesTable.swift", "Effectiveness", "MoveTypes"]),
            "Treasure Editor" => BuildCommonRelSections("Treasure box table", ["TreasureBoxData", "NumberTreasureBoxes", "TreasureTable.swift"]),
            "Patches" => BuildPatchSections(),
            "Randomizer" => BuildRandomizerSections(),
            "Message Editor" => BuildMessageSections(),
            "Script Compiler" => BuildScriptSections(),
            "Collision Viewer" => BuildIsoTypeSections("Collision files", file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase), "Scans room FSYS archives for collision data after export/decode."),
            "Interaction Editor" => BuildCommonRelSections("Interaction point and room tables", ["InteractionPoints", "NumberOfInteractionPoints", "Rooms", "Doors"]),
            "Vertex Filters" => BuildIsoTypeSections("Model/texture archives", file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase), "Targets exported DAT/WZX model files for vertex colour filtering."),
            "Table Editor" => BuildTableEditorSections(),
            _ => BuildGenericSections(title)
        });

        return new XdToolContent(
            title,
            $"{title} content is loaded from the XD ISO source map and legacy Swift editor targets.",
            sections);
    }

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

    private XdToolSection BuildProjectSection()
        => new("Loaded XD Project",
        [
            new("ISO", Iso.FileName, $"{Iso.GameId}, {Iso.Region}, {Iso.Files.Count:N0} FST files"),
            new("Workspace", WorkspaceDirectory, Directory.Exists(WorkspaceDirectory) ? "Ready" : "Missing"),
            new("Legacy Tool", Iso.LegacyToolName, "Separate GoD Tool release using shared Cipher Snagem core")
        ]);

    private IReadOnlyList<XdToolSection> BuildDeckEditorSections(bool includeShadowDeck)
    {
        var rows = new List<XdToolRow>
        {
            SourceFileRow("deck_archive.fsys", "Deck archive"),
            new("Deck tables", "Trainer / Pokemon / AI", "DeckData_Story, DeckData_Colosseum, DeckData_Hundred, DeckData_Virtual, DeckData_Imasugu, DeckData_Bingo, DeckData_Sample")
        };

        if (includeShadowDeck)
        {
            rows.Add(new XdToolRow("Shadow deck", "DeckData_DarkPokemon.bin", "DDPK shadow Pokemon table"));
        }

        var sections = new List<XdToolSection>
        {
            new("Legacy Data Sources", rows),
            BuildDeckArchiveSection(includeShadowDeck),
            includeShadowDeck ? BuildShadowPokemonPreviewSection() : BuildTrainerPreviewSection()
        };

        return sections;
    }

    private IReadOnlyList<XdToolSection> BuildShadowPokemonSections()
    {
        var sections = new List<XdToolSection>(BuildDeckEditorSections(includeShadowDeck: true))
        {
            new("Shadow Fields", [
                new("Story Pokemon Link", "Pokemon Index In Story Deck", "Connects each shadow ID to a DeckData_Story Pokemon row"),
                new("Purification", "Heart Gauge", "Controls how long purification takes"),
                new("Shadow Moves", "4 move slots", "Legacy editor exposes four shadow move fields"),
                new("Behavior", "Aggression / flee flag", "Reverse mode and Miror B handling fields")
            ])
        };

        return sections;
    }

    private IReadOnlyList<XdToolSection> BuildPokespotSections()
        =>
        [
            new("Legacy Data Sources", [
                SourceFileRow("common.fsys", "Common archive"),
                new("Tables", "Rock / Oasis / Cave / All", "CommonIndexes 12/15/18/21 with entry counts at 13/16/19/22")
            ]),
            BuildCommonArchiveSection(),
            BuildPokespotEncounterSection(),
            new("Pokespot Fields", [
                new("Species", "Pokemon ID", "Editable per encounter slot"),
                new("Level Range", "Min / Max", "Byte fields in each encounter row"),
                new("Encounter Chance", "Percentage", "Word field in each encounter row"),
                new("Snack Steps", "Steps per snack", "Word field controlling snack consumption rate")
            ])
        ];

    private IReadOnlyList<XdToolSection> BuildPatchSections()
        =>
        [
            new("Legacy Data Sources", [
                SourceFileRow("Start.dol", "Executable"),
                SourceFileRow("common.fsys", "Common archive"),
                SourceFileRow("deck_archive.fsys", "Deck archive")
            ]),
            new("Patch Surface", [
                new("ASM patches", "Start.dol", "Legacy XD offsets ported for the patch list"),
                new("Data patches", "common.rel / deck data", "Targets parsed XD tables and deck archives")
            ])
        ];

    private IReadOnlyList<XdToolSection> BuildRandomizerSections()
        =>
        [
            new("Randomizer Sources", [
                SourceFileRow("common.fsys", "Pokemon, moves, items, types"),
                SourceFileRow("deck_archive.fsys", "Trainers, shadow Pokemon, battle Pokemon"),
                SourceFileRow("Start.dol", "Executable constants and patches")
            ]),
            BuildDeckArchiveSection(includeShadowDeck: true)
        ];

    private IReadOnlyList<XdToolSection> BuildMessageSections()
    {
        var messageCandidates = Iso.Files
            .Where(file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase)
                || file.Name.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
            .Take(12)
            .Select(file => new XdToolRow(file.Name, $"{file.Size:N0} bytes", "Message tables are stored directly or inside FSYS archives."))
            .ToArray();

        return
        [
            new("Message Sources", messageCandidates.Length == 0
                ? [new XdToolRow("Messages", "Not discovered", "No message-like ISO entries were visible in the FST.")]
                : messageCandidates)
        ];
    }

    private IReadOnlyList<XdToolSection> BuildScriptSections()
    {
        var workspaceScripts = Directory.Exists(WorkspaceDirectory)
            ? Directory.EnumerateFiles(WorkspaceDirectory, "*.xds", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(WorkspaceDirectory, "*.xds.txt", SearchOption.AllDirectories))
                .Take(20)
                .Select(path => new XdToolRow(Path.GetFileName(path), "Workspace script", Path.GetRelativePath(WorkspaceDirectory, path)))
                .ToArray()
            : [];

        var scriptArchiveRows = Iso.Files
            .Where(file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase))
            .Take(16)
            .Select(file => new XdToolRow(file.Name, $"{file.Size:N0} bytes", "Potential script/collision/room archive. Export/decode to expose .xds text."))
            .ToArray();

        return
        [
            new("Workspace Scripts", workspaceScripts.Length == 0
                ? [new XdToolRow("No exported scripts", "0 files", "Export/decode room FSYS archives to populate editable .xds script text.")]
                : workspaceScripts),
            new("Potential Script Archives", scriptArchiveRows)
        ];
    }

    private IReadOnlyList<XdToolSection> BuildTableEditorSections()
    {
        var sections = new List<XdToolSection>
        {
            BuildCommonArchiveSection(),
            BuildDeckArchiveSection(includeShadowDeck: true),
            new("Legacy Table Groups", [
                new("Common", "common.rel", "Pokemon, moves, items, types, rooms, treasures, interactions, pokespots"),
                new("Deck Trainer", "deck_archive.fsys", "Trainer rows for each XD deck"),
                new("Deck Pokemon", "deck_archive.fsys", "Pokemon rows for each XD deck"),
                new("Deck AI", "deck_archive.fsys", "AI rows for each XD deck")
            ])
        };

        return sections;
    }

    private IReadOnlyList<XdToolSection> BuildCommonRelSections(string label, IReadOnlyList<string> legacyTargets)
        =>
        [
            new("Legacy Data Sources", [
                SourceFileRow("common.fsys", "Common archive"),
                new(label, "common.rel", string.Join(", ", legacyTargets))
            ]),
            BuildCommonRelTargetSection(label, legacyTargets),
            BuildCommonArchiveSection()
        ];

    private IReadOnlyList<XdToolSection> BuildIsoTypeSections(string title, Func<GameCubeIsoFileEntry, bool> predicate, string detail)
    {
        var rows = Iso.Files
            .Where(predicate)
            .Take(18)
            .Select(file => new XdToolRow(file.Name, $"{file.Size:N0} bytes", detail))
            .ToArray();

        return
        [
            new(title, rows.Length == 0 ? [new XdToolRow(title, "0 files", "No matching ISO files were found.")] : rows)
        ];
    }

    private IReadOnlyList<XdToolSection> BuildGenericSections(string title)
        =>
        [
            new("Editor Source", [
                new(title, "XD mode", "No legacy source map has been assigned for this editor yet.")
            ])
        ];

    private XdToolSection BuildCommonArchiveSection()
    {
        var archive = TryReadFsys("common.fsys", out var error);
        if (archive is null)
        {
            return new XdToolSection("common.fsys", [SourceFileRow("common.fsys", error ?? "Not found")]);
        }

        return new XdToolSection("common.fsys Contents",
            archive.Entries
                .OrderBy(entry => entry.Index)
                .Take(12)
                .Select(entry => new XdToolRow(entry.Name, $"{entry.UncompressedSize:N0} bytes", $"{entry.FileType}, id 0x{entry.Identifier:x8}"))
                .ToArray());
    }

    private XdToolSection BuildDeckArchiveSection(bool includeShadowDeck)
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error);
        if (archive is null)
        {
            return new XdToolSection("deck_archive.fsys", [SourceFileRow("deck_archive.fsys", error ?? "Not found")]);
        }

        var rows = new List<XdToolRow>();
        foreach (var entry in archive.Entries
            .Where(entry => includeShadowDeck || !entry.Name.Contains("DarkPokemon", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Name)
            .Take(18))
        {
            rows.Add(DeckEntryRow(archive, entry));
        }

        return new XdToolSection("deck_archive.fsys Contents", rows);
    }

    private XdToolRow DeckEntryRow(FsysArchive archive, FsysEntry entry)
    {
        var detail = $"{entry.FileType}, id 0x{entry.Identifier:x8}";
        try
        {
            var bytes = archive.Extract(entry);
            detail = entry.Name.Contains("DarkPokemon", StringComparison.OrdinalIgnoreCase)
                ? ShadowDeckSummary(bytes)
                : DeckSummary(bytes);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
        {
            detail = $"Header parse failed: {ex.Message}";
        }

        return new XdToolRow(entry.Name, $"{entry.UncompressedSize:N0} bytes", detail);
    }

    private XdToolRow SourceFileRow(string fileName, string detail)
    {
        var entry = FindIsoFile(fileName);
        return entry is null
            ? new XdToolRow(fileName, "Missing", detail)
            : new XdToolRow(entry.Name, $"{entry.Size:N0} bytes", detail);
    }

    public FsysArchive ReadIsoFsysArchive(GameCubeIsoFileEntry entry)
    {
        if (GameFileTypes.FromExtension(entry.Name) != GameFileType.Fsys)
        {
            throw new InvalidDataException($"{entry.Name} is not an FSYS archive.");
        }

        return FsysArchive.Parse(entry.Name, GameCubeIsoReader.ReadFile(Iso, entry));
    }

    private FsysArchive? TryReadFsys(string fileName, out string? error)
    {
        var entry = FindIsoFile(fileName);
        if (entry is null)
        {
            error = "ISO file not found.";
            return null;
        }

        try
        {
            error = null;
            return FsysArchive.Parse(entry.Name, GameCubeIsoReader.ReadFile(Iso, entry));
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
        {
            error = ex.Message;
            return null;
        }
    }

    private GameCubeIsoFileEntry? FindIsoFile(string fileName)
        => Iso.Files.FirstOrDefault(file => string.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase))
            ?? Iso.Files.FirstOrDefault(file => string.Equals(Path.GetFileName(file.Name), fileName, StringComparison.OrdinalIgnoreCase));

    private static string ShadowDeckSummary(byte[] bytes)
    {
        if (bytes.Length < 0x1c)
        {
            return "DDPK header is too short.";
        }

        var size = BigEndian.ReadUInt32(bytes, 0x14);
        var entries = BigEndian.ReadUInt32(bytes, 0x18);
        return $"DDPK shadow table: {entries:N0} rows, {size:N0} bytes";
    }

    private static string DeckSummary(byte[] bytes)
    {
        if (bytes.Length < 0x1c)
        {
            return "Deck header is too short.";
        }

        var trainerSize = checked((int)BigEndian.ReadUInt32(bytes, 0x14));
        var trainerEntries = BigEndian.ReadUInt32(bytes, 0x18);
        var pokemonHeader = 0x10 + trainerSize;
        if (pokemonHeader + 0x0c > bytes.Length)
        {
            return $"Trainer rows: {trainerEntries:N0}; Pokemon header outside file.";
        }

        var pokemonSize = checked((int)BigEndian.ReadUInt32(bytes, pokemonHeader + 0x04));
        var pokemonEntries = BigEndian.ReadUInt32(bytes, pokemonHeader + 0x08);
        var aiHeader = pokemonHeader + pokemonSize;
        if (aiHeader + 0x0c > bytes.Length)
        {
            return $"Trainer rows: {trainerEntries:N0}; Pokemon rows: {pokemonEntries:N0}; AI header outside file.";
        }

        var aiEntries = BigEndian.ReadUInt32(bytes, aiHeader + 0x08);
        return $"Trainer rows: {trainerEntries:N0}; Pokemon rows: {pokemonEntries:N0}; AI rows: {aiEntries:N0}";
    }
}
