using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Relocation;
using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    private const int XdStringTableIndex = 136;
    private const int XdPokemonStatsIndex = 88;
    private const int XdNumberOfPokemonIndex = 89;
    private const int XdPokemonStatsSize = 0x124;
    private const int XdPokemonStatsNameIdOffset = 0x18;
    private const int XdMoveSize = 0x38;
    private const int XdMoveNameIdOffset = 0x20;
    private const int XdItemSize = 0x28;
    private const int XdItemNameIdOffset = 0x10;
    private const int XdTrainerSize = 0x38;
    private const int XdTrainerStringOffset = 0x00;
    private const int XdTrainerShadowMaskOffset = 0x04;
    private const int XdTrainerClassOffset = 0x05;
    private const int XdTrainerNameIdOffset = 0x06;
    private const int XdTrainerModelOffset = 0x11;
    private const int XdTrainerCameraOffset = 0x12;
    private const int XdTrainerPreBattleTextOffset = 0x14;
    private const int XdTrainerVictoryTextOffset = 0x16;
    private const int XdTrainerDefeatTextOffset = 0x18;
    private const int XdTrainerFirstPokemonOffset = 0x1c;
    private const int XdTrainerAiOffset = 0x28;
    private const int XdDeckPokemonSize = 0x20;
    private const int XdDeckPokemonSpeciesOffset = 0x00;
    private const int XdDeckPokemonLevelOffset = 0x02;
    private const int XdDeckPokemonItemOffset = 0x04;
    private const int XdDeckPokemonFirstMoveOffset = 0x14;
    private const int XdShadowPokemonSize = 0x18;
    private const int XdShadowFleeOffset = 0x00;
    private const int XdShadowCatchRateOffset = 0x01;
    private const int XdShadowLevelOffset = 0x02;
    private const int XdShadowInUseOffset = 0x03;
    private const int XdShadowStoryIndexOffset = 0x06;
    private const int XdShadowHeartGaugeOffset = 0x08;
    private const int XdShadowFirstMoveOffset = 0x0c;
    private const int XdShadowAggressionOffset = 0x14;
    private const int XdShadowAlwaysFleeOffset = 0x15;
    private const int XdPokespotEntrySize = 0x0c;

    private static readonly IReadOnlyList<string> XdTrainerDecks =
    [
        "DeckData_Story",
        "DeckData_Colosseum",
        "DeckData_Hundred",
        "DeckData_Virtual",
        "DeckData_Imasugu",
        "DeckData_Bingo",
        "DeckData_Sample"
    ];

    private XdToolSection BuildCommonRelTargetSection(string label, IReadOnlyList<string> legacyTargets)
    {
        if (!TryReadCommonRel(out var data, out var table, out _, out var error) || data is null || table is null)
        {
            return new XdToolSection("common.rel Table Map", [new XdToolRow("common.rel", "Unavailable", error ?? "Could not parse common.rel")]);
        }

        var rows = CommonTargetsFor(label, legacyTargets)
            .Select(target => CommonTargetRow(table, target))
            .ToArray();

        return new XdToolSection("common.rel Table Map",
            rows.Length == 0
                ? [new XdToolRow(label, "No direct map", string.Join(", ", legacyTargets))]
                : rows);
    }

    private XdToolSection BuildTrainerPreviewSection()
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error);
        if (archive is null)
        {
            return new XdToolSection("Trainer Preview", [SourceFileRow("deck_archive.fsys", error ?? "Not found")]);
        }

        var names = BuildCommonNameLookup();
        var rows = new List<XdToolRow>();
        foreach (var deckName in XdTrainerDecks)
        {
            var entry = FindDeckEntry(archive, deckName);
            if (entry is null)
            {
                rows.Add(new XdToolRow(deckName, "Missing", "Deck file was not found in deck_archive.fsys."));
                continue;
            }

            try
            {
                var bytes = archive.Extract(entry);
                if (!TryReadDeckLayout(bytes, out var layout, out var layoutError))
                {
                    rows.Add(new XdToolRow(entry.Name, "Unreadable", layoutError));
                    continue;
                }

                rows.AddRange(ReadTrainerPreviewRows(deckName, bytes, layout, names, maxRows: 3));
            }
            catch (Exception ex) when (IsParseException(ex))
            {
                rows.Add(new XdToolRow(entry.Name, "Parse failed", ex.Message));
            }
        }

        return new XdToolSection("Trainer Preview",
            rows.Count == 0
                ? [new XdToolRow("Trainers", "0 rows", "No non-empty trainer rows were found in the XD decks.")]
                : rows);
    }

    private XdToolSection BuildShadowPokemonPreviewSection()
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error);
        if (archive is null)
        {
            return new XdToolSection("Shadow Pokemon Preview", [SourceFileRow("deck_archive.fsys", error ?? "Not found")]);
        }

        var darkEntry = FindDeckEntry(archive, "DeckData_DarkPokemon");
        var storyEntry = FindDeckEntry(archive, "DeckData_Story");
        if (darkEntry is null || storyEntry is null)
        {
            return new XdToolSection("Shadow Pokemon Preview",
            [
                new("DeckData_DarkPokemon", darkEntry is null ? "Missing" : "Found", "DDPK shadow deck"),
                new("DeckData_Story", storyEntry is null ? "Missing" : "Found", "Story DPKM deck used by DDPK story indexes")
            ]);
        }

        try
        {
            var darkBytes = archive.Extract(darkEntry);
            var storyBytes = archive.Extract(storyEntry);
            if (!TryReadDeckLayout(storyBytes, out var storyLayout, out var storyError))
            {
                return new XdToolSection("Shadow Pokemon Preview", [new XdToolRow(storyEntry.Name, "Unreadable", storyError)]);
            }

            var names = BuildCommonNameLookup();
            var rows = ReadShadowPokemonRows(darkBytes, storyBytes, storyLayout, names, maxRows: 24);
            return new XdToolSection("Shadow Pokemon Preview",
                rows.Count == 0
                    ? [new XdToolRow("Shadow Pokemon", "0 rows", "No active DDPK shadow rows were found.")]
                    : rows);
        }
        catch (Exception ex) when (IsParseException(ex))
        {
            return new XdToolSection("Shadow Pokemon Preview", [new XdToolRow("DeckData_DarkPokemon", "Parse failed", ex.Message)]);
        }
    }

    private XdToolSection BuildPokespotEncounterSection()
    {
        if (!TryReadCommonRel(out var data, out var table, out _, out var error) || data is null || table is null)
        {
            return new XdToolSection("Pokespot Encounters", [new XdToolRow("common.rel", "Unavailable", error ?? "Could not parse common.rel")]);
        }

        var names = BuildPokemonNameMap(data, table, TryReadStringTable(table));
        var rows = new List<XdToolRow>();
        foreach (var spot in XdPokespotTargets)
        {
            var offset = table.GetPointer(spot.PointerIndex);
            var count = table.GetValueAtPointer(spot.CountIndex);
            if (!IsSafeTableRange(data, offset, count, XdPokespotEntrySize, maxCount: 64))
            {
                rows.Add(new XdToolRow(spot.Name, "Unavailable", $"Pointer {spot.PointerIndex} or count {spot.CountIndex} is outside common.rel."));
                continue;
            }

            for (var index = 0; index < count; index++)
            {
                var start = offset + (index * XdPokespotEntrySize);
                var minLevel = data.ReadByte(start);
                var maxLevel = data.ReadByte(start + 1);
                var species = data.ReadUInt16(start + 2);
                var chance = data.ReadByte(start + 7);
                var steps = data.ReadUInt16(start + 0x0a);
                rows.Add(new XdToolRow(
                    $"{spot.Name} #{index + 1}",
                    $"{PokemonName(names, species)} Lv. {minLevel}-{maxLevel}",
                    $"Species {species}; encounter {chance}%; {steps} steps per Poke Snack; offset 0x{start:x}"));
            }
        }

        return new XdToolSection("Pokespot Encounters", rows);
    }

    private bool TryReadCommonRel(
        out BinaryData? data,
        out RelocationTable? table,
        out GameStringTable? strings,
        out string? error)
    {
        data = null;
        table = null;
        strings = null;

        var archive = TryReadFsys("common.fsys", out error);
        if (archive is null)
        {
            return false;
        }

        var entry = archive.Entries.FirstOrDefault(entry => entry.Name.Equals("common.rel", StringComparison.OrdinalIgnoreCase))
            ?? archive.Entries.FirstOrDefault(entry => entry.Name.Contains("common", StringComparison.OrdinalIgnoreCase)
                && entry.Name.EndsWith(".rel", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            error = "common.rel was not found inside common.fsys.";
            return false;
        }

        try
        {
            var bytes = archive.Extract(entry);
            data = new BinaryData(bytes);
            table = RelocationTable.Parse(bytes);
            strings = TryReadStringTable(table);
            error = null;
            return true;
        }
        catch (Exception ex) when (IsParseException(ex))
        {
            error = ex.Message;
            return false;
        }
    }

    private IReadOnlyDictionary<int, string> BuildCommonNameLookup()
    {
        if (!TryReadCommonRel(out var data, out var table, out var strings, out _) || data is null || table is null)
        {
            return new Dictionary<int, string>();
        }

        var names = new Dictionary<int, string>();
        foreach (var (id, name) in BuildPokemonNameMap(data, table, strings))
        {
            names[id] = name;
        }

        foreach (var (id, name) in BuildMoveNameMap(data, table, strings))
        {
            names[MoveNameKey(id)] = name;
        }

        foreach (var (id, name) in BuildItemNameMap(data, table, strings))
        {
            names[ItemNameKey(id)] = name;
        }

        return names;
    }

    private static IReadOnlyDictionary<int, string> BuildPokemonNameMap(BinaryData data, RelocationTable table, GameStringTable? strings)
    {
        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            return new Dictionary<int, string>();
        }

        var names = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdPokemonStatsSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdPokemonStatsNameIdOffset));
            names[index] = CleanName(strings?.StringWithId(nameId), index, "Pokemon");
        }

        return names;
    }

    private static IReadOnlyDictionary<int, string> BuildMoveNameMap(BinaryData data, RelocationTable table, GameStringTable? strings)
    {
        const int movesIndex = 124;
        const int numberOfMovesIndex = 125;
        var start = table.GetPointer(movesIndex);
        var count = table.GetValueAtPointer(numberOfMovesIndex);
        if (!IsSafeTableRange(data, start, count, XdMoveSize, maxCount: 1000))
        {
            return new Dictionary<int, string>();
        }

        var names = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdMoveSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdMoveNameIdOffset));
            names[index] = CleanName(strings?.StringWithId(nameId), index, "Move");
        }

        return names;
    }

    private static IReadOnlyDictionary<int, string> BuildItemNameMap(BinaryData data, RelocationTable table, GameStringTable? strings)
    {
        const int itemsIndex = 70;
        const int numberOfItemsIndex = 71;
        var start = table.GetPointer(itemsIndex);
        var count = table.GetValueAtPointer(numberOfItemsIndex);
        if (!IsSafeTableRange(data, start, count, XdItemSize, maxCount: 1000))
        {
            return new Dictionary<int, string>();
        }

        var names = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdItemSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdItemNameIdOffset));
            names[index] = CleanName(strings?.StringWithId(nameId), index, "Item");
        }

        return names;
    }

    private static IReadOnlyList<XdToolRow> ReadTrainerPreviewRows(
        string deckName,
        byte[] bytes,
        XdDeckLayout layout,
        IReadOnlyDictionary<int, string> names,
        int maxRows)
    {
        var rows = new List<XdToolRow>();
        for (var index = 1; index < layout.TrainerEntries && rows.Count < maxRows; index++)
        {
            var start = layout.TrainerDataOffset + (index * XdTrainerSize);
            if (start + XdTrainerSize > bytes.Length)
            {
                break;
            }

            var pokemonIds = Enumerable.Range(0, 6)
                .Select(slot => ReadU16(bytes, start + XdTrainerFirstPokemonOffset + (slot * 2)))
                .ToArray();
            if (pokemonIds.All(id => id == 0)
                && bytes[start + XdTrainerClassOffset] == 0
                && bytes[start + XdTrainerNameIdOffset] == 0)
            {
                continue;
            }

            var shadowMask = bytes[start + XdTrainerShadowMaskOffset];
            var trainerStringOffset = ReadU16(bytes, start + XdTrainerStringOffset);
            var nameId = ReadU16(bytes, start + XdTrainerNameIdOffset);
            var classId = bytes[start + XdTrainerClassOffset];
            var modelId = bytes[start + XdTrainerModelOffset];
            var ai = ReadU16(bytes, start + XdTrainerAiOffset);
            var camera = ReadU16(bytes, start + XdTrainerCameraOffset);
            var messages = $"pre {ReadU16(bytes, start + XdTrainerPreBattleTextOffset)}, win {ReadU16(bytes, start + XdTrainerVictoryTextOffset)}, lose {ReadU16(bytes, start + XdTrainerDefeatTextOffset)}";
            var trainerString = ReadDeckString(bytes, layout.StringDataOffset, trainerStringOffset);
            var pokemon = FormatTrainerPokemon(bytes, layout, names, shadowMask, pokemonIds);

            rows.Add(new XdToolRow(
                $"{deckName.Replace("DeckData_", "", StringComparison.Ordinal)} #{index:D3}",
                $"Class {classId}, model {modelId}",
                $"{trainerString}; name ID {nameId}; AI {ai}; camera {camera}; {messages}; {pokemon}"));
        }

        return rows;
    }

    private static IReadOnlyList<XdToolRow> ReadShadowPokemonRows(
        byte[] shadowBytes,
        byte[] storyBytes,
        XdDeckLayout storyLayout,
        IReadOnlyDictionary<int, string> names,
        int maxRows)
    {
        var rows = new List<XdToolRow>();
        if (shadowBytes.Length < 0x20)
        {
            return rows;
        }

        var entries = checked((int)BigEndian.ReadUInt32(shadowBytes, 0x18));
        for (var index = 0; index < entries && rows.Count < maxRows; index++)
        {
            var start = 0x20 + (index * XdShadowPokemonSize);
            if (start + XdShadowPokemonSize > shadowBytes.Length)
            {
                break;
            }

            var storyIndex = ReadU16(shadowBytes, start + XdShadowStoryIndexOffset);
            var level = shadowBytes[start + XdShadowLevelOffset];
            var catchRate = shadowBytes[start + XdShadowCatchRateOffset];
            var heartGauge = ReadU16(shadowBytes, start + XdShadowHeartGaugeOffset);
            var inUse = shadowBytes[start + XdShadowInUseOffset] == 0x80;
            if (storyIndex == 0 && level == 0 && catchRate == 0 && heartGauge == 0)
            {
                continue;
            }

            var species = ReadDeckPokemonSpecies(storyBytes, storyLayout, storyIndex);
            var moves = Enumerable.Range(0, 4)
                .Select(slot => ReadU16(shadowBytes, start + XdShadowFirstMoveOffset + (slot * 2)))
                .Where(move => move > 0)
                .Select(move => MoveName(names, move))
                .DefaultIfEmpty("-")
                .ToArray();

            rows.Add(new XdToolRow(
                $"Shadow #{index:D2}",
                $"{PokemonName(names, species)} Lv. {level}+",
                $"Story DPKM {storyIndex}; catch {catchRate}; heart {heartGauge}; in use {inUse}; moves {string.Join(", ", moves)}; aggression {shadowBytes[start + XdShadowAggressionOffset]}; flee {shadowBytes[start + XdShadowFleeOffset]}; always flee {shadowBytes[start + XdShadowAlwaysFleeOffset]}"));
        }

        return rows;
    }

    private static string FormatTrainerPokemon(
        byte[] bytes,
        XdDeckLayout layout,
        IReadOnlyDictionary<int, string> names,
        int shadowMask,
        IReadOnlyList<int> pokemonIds)
    {
        var slots = new List<string>();
        for (var slot = 0; slot < pokemonIds.Count; slot++)
        {
            var id = pokemonIds[slot];
            if (id == 0)
            {
                continue;
            }

            var isShadow = ((shadowMask >> slot) & 1) == 1;
            if (isShadow)
            {
                slots.Add($"slot {slot + 1}: Shadow #{id}");
                continue;
            }

            var start = layout.PokemonDataOffset + (id * XdDeckPokemonSize);
            if (start < 0 || start + XdDeckPokemonSize > bytes.Length)
            {
                slots.Add($"slot {slot + 1}: DPKM {id}");
                continue;
            }

            var species = ReadU16(bytes, start + XdDeckPokemonSpeciesOffset);
            var level = bytes[start + XdDeckPokemonLevelOffset];
            var item = ReadU16(bytes, start + XdDeckPokemonItemOffset);
            var moves = Enumerable.Range(0, 4)
                .Select(move => ReadU16(bytes, start + XdDeckPokemonFirstMoveOffset + (move * 2)))
                .Where(move => move > 0)
                .Select(move => MoveName(names, move))
                .Take(2)
                .ToArray();

            slots.Add($"slot {slot + 1}: {PokemonName(names, species)} Lv. {level} item {ItemName(names, item)} moves {string.Join("/", moves.DefaultIfEmpty("-"))}");
        }

        return slots.Count == 0 ? "no Pokemon refs" : string.Join("; ", slots);
    }

    private static bool TryReadDeckLayout(byte[] bytes, out XdDeckLayout layout, out string error)
    {
        layout = new XdDeckLayout(0, 0, 0, 0, 0, 0);
        error = string.Empty;
        if (bytes.Length < 0x20)
        {
            error = "Deck file is too short.";
            return false;
        }

        var trainerSize = checked((int)BigEndian.ReadUInt32(bytes, 0x14));
        var trainerEntries = checked((int)BigEndian.ReadUInt32(bytes, 0x18));
        var pokemonHeader = 0x10 + trainerSize;
        if (pokemonHeader + 0x10 > bytes.Length)
        {
            error = "DPKM header is outside the deck file.";
            return false;
        }

        var pokemonSize = checked((int)BigEndian.ReadUInt32(bytes, pokemonHeader + 0x04));
        var pokemonEntries = checked((int)BigEndian.ReadUInt32(bytes, pokemonHeader + 0x08));
        var aiHeader = pokemonHeader + pokemonSize;
        if (aiHeader + 0x10 > bytes.Length)
        {
            error = "DTAI header is outside the deck file.";
            return false;
        }

        var aiSize = checked((int)BigEndian.ReadUInt32(bytes, aiHeader + 0x04));
        var aiEntries = checked((int)BigEndian.ReadUInt32(bytes, aiHeader + 0x08));
        var stringHeader = aiHeader + aiSize;
        var stringDataOffset = stringHeader + 0x10;
        if (stringDataOffset > bytes.Length)
        {
            stringDataOffset = bytes.Length;
        }

        layout = new XdDeckLayout(
            trainerEntries,
            0x20,
            pokemonEntries,
            pokemonHeader + 0x10,
            aiEntries,
            stringDataOffset);
        return true;
    }

    private static int ReadDeckPokemonSpecies(byte[] bytes, XdDeckLayout layout, int dpkmIndex)
    {
        var start = layout.PokemonDataOffset + (dpkmIndex * XdDeckPokemonSize);
        return start >= 0 && start + XdDeckPokemonSize <= bytes.Length
            ? ReadU16(bytes, start + XdDeckPokemonSpeciesOffset)
            : 0;
    }

    private static string ReadDeckString(byte[] bytes, int stringDataOffset, int offset)
    {
        var start = stringDataOffset + offset;
        if (start < 0 || start >= bytes.Length)
        {
            return "string -";
        }

        var end = start;
        while (end < bytes.Length && bytes[end] != 0)
        {
            end++;
        }

        if (end == start)
        {
            return "string -";
        }

        var text = System.Text.Encoding.ASCII.GetString(bytes, start, end - start);
        return $"string \"{text}\"";
    }

    private FsysEntry? FindDeckEntry(FsysArchive archive, string deckName)
        => archive.Entries.FirstOrDefault(entry => entry.Name.Equals(deckName + ".bin", StringComparison.OrdinalIgnoreCase))
            ?? archive.Entries.FirstOrDefault(entry => entry.Name.StartsWith(deckName, StringComparison.OrdinalIgnoreCase));

    private static XdToolRow CommonTargetRow(RelocationTable table, XdCommonTarget target)
    {
        var start = table.GetPointer(target.PointerIndex);
        var count = table.GetValueAtPointer(target.CountIndex);
        var length = table.GetSymbolLength(target.PointerIndex);
        var value = count > 0 ? $"{count:N0} rows" : "No rows";
        var detail = start < 0
            ? $"Pointer {target.PointerIndex} was not found. Legacy target: {target.LegacyTarget}."
            : $"Start 0x{start:x}; count index {target.CountIndex}; row size 0x{target.RowSize:x}; symbol {length:N0} bytes; {target.LegacyTarget}";
        return new XdToolRow(target.Name, value, detail);
    }

    private static IReadOnlyList<XdCommonTarget> CommonTargetsFor(string label, IReadOnlyList<string> legacyTargets)
    {
        var joined = string.Join(" ", legacyTargets);
        if (label.StartsWith("Pokemon stats", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Pokemon Stats", 88, 89, 0x124, joined)];
        }

        if (label.StartsWith("Moves", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Moves", 124, 125, 0x38, joined), new("Tutor Moves", 126, 127, 0x08, joined)];
        }

        if (label.StartsWith("Items", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Valid Items", 68, 69, 0x02, joined), new("Items", 70, 71, 0x28, joined)];
        }

        if (label.StartsWith("Gift", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Legendary Pokemon", 72, 73, 0x70, joined), new("Master Ball Pokemon", 74, 75, 0x70, joined)];
        }

        if (label.StartsWith("Type", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Types", 130, 131, 0x30, joined)];
        }

        if (label.StartsWith("Treasure", StringComparison.OrdinalIgnoreCase))
        {
            return [new("Treasure Boxes", 66, 67, 0x1c, joined)];
        }

        if (label.StartsWith("Interaction", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new("Rooms", 58, 59, 0x44, joined),
                new("Doors", 60, 61, 0x14, joined),
                new("Interaction Points", 62, 63, 0x1c, joined)
            ];
        }

        return [];
    }

    private static GameStringTable? TryReadStringTable(RelocationTable table)
    {
        try
        {
            var bytes = table.ReadSymbol(XdStringTableIndex);
            return bytes.Length == 0 ? null : GameStringTable.Parse(bytes);
        }
        catch (Exception ex) when (IsParseException(ex))
        {
            return null;
        }
    }

    private static bool IsSafeTableRange(BinaryData data, int offset, int count, int rowSize, int maxCount)
        => offset >= 0
            && count >= 0
            && count <= maxCount
            && rowSize > 0
            && offset + (count * rowSize) <= data.Length;

    private static int ReadU16(byte[] bytes, int offset)
        => offset >= 0 && offset + 2 <= bytes.Length ? BigEndian.ReadUInt16(bytes, offset) : 0;

    private static string PokemonName(IReadOnlyDictionary<int, string> names, int species)
        => names.TryGetValue(species, out var name) ? name : $"Pokemon {species}";

    private static string MoveName(IReadOnlyDictionary<int, string> names, int move)
        => names.TryGetValue(MoveNameKey(move), out var name) ? name : $"Move {move}";

    private static string ItemName(IReadOnlyDictionary<int, string> names, int item)
        => names.TryGetValue(ItemNameKey(item), out var name) ? name : $"Item {item}";

    private static int MoveNameKey(int move) => 0x10000 + move;

    private static int ItemNameKey(int item) => 0x20000 + item;

    private static string CleanName(string? value, int index, string fallback)
        => string.IsNullOrWhiteSpace(value) || value == "-"
            ? $"{fallback} {index}"
            : value.Replace('\n', ' ').Trim();

    private static bool IsParseException(Exception ex)
        => ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException;

    private static readonly IReadOnlyList<XdPokespotTarget> XdPokespotTargets =
    [
        new("Rock", 12, 13),
        new("Oasis", 15, 16),
        new("Cave", 18, 19),
        new("All", 21, 22)
    ];

    private sealed record XdCommonTarget(string Name, int PointerIndex, int CountIndex, int RowSize, string LegacyTarget);

    private sealed record XdDeckLayout(
        int TrainerEntries,
        int TrainerDataOffset,
        int PokemonEntries,
        int PokemonDataOffset,
        int AiEntries,
        int StringDataOffset);

    private sealed record XdPokespotTarget(string Name, int PointerIndex, int CountIndex);
}
