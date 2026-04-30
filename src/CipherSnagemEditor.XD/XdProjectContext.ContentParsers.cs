using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;
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
    private const int XdTypeSize = 0x30;
    private const int XdTypeNameIdOffset = 0x08;
    private const int XdTypeCategoryOffset = 0x00;
    private const int XdTypeFirstEffectivenessOffset = 0x0d;
    private const int XdMovePriorityOffset = 0x00;
    private const int XdMovePpOffset = 0x01;
    private const int XdMoveTypeOffset = 0x02;
    private const int XdMoveTargetsOffset = 0x03;
    private const int XdMoveAccuracyOffset = 0x04;
    private const int XdMoveEffectAccuracyOffset = 0x05;
    private const int XdMoveContactFlagOffset = 0x06;
    private const int XdMoveProtectFlagOffset = 0x07;
    private const int XdMoveMagicCoatFlagOffset = 0x08;
    private const int XdMoveSnatchFlagOffset = 0x09;
    private const int XdMoveMirrorMoveFlagOffset = 0x0a;
    private const int XdMoveKingsRockFlagOffset = 0x0b;
    private const int XdMoveSoundBasedFlagOffset = 0x10;
    private const int XdMoveHmFlagOffset = 0x12;
    private const int XdMoveCategoryOffset = 0x13;
    private const int XdMoveBasePowerOffset = 0x19;
    private const int XdMoveEffectOffset = 0x1c;
    private const int XdMoveAnimationOffset = 0x1e;
    private const int XdMoveDescriptionIdOffset = 0x2c;
    private const int XdMoveAnimation2Offset = 0x32;
    private const int XdMoveEffectTypeOffset = 0x34;
    private const int XdTypeIndex = 130;
    private const int XdNumberOfTypesIndex = 131;
    private const int XdItemsIndex = 70;
    private const int XdNumberOfItemsIndex = 71;
    private const int XdMovesIndex = 124;
    private const int XdNumberOfMovesIndex = 125;
    private const int XdTreasureIndex = 66;
    private const int XdNumberOfTreasuresIndex = 67;
    private const int XdRoomsIndex = 58;
    private const int XdNumberOfRoomsIndex = 59;
    private const int XdPokemonLevelUpMoveCount = 0x13;
    private const int XdPokemonEvolutionCount = 0x05;
    private const int XdPokemonTmCount = 0x3a;
    private const int XdPokemonExpRateOffset = 0x00;
    private const int XdPokemonCatchRateOffset = 0x01;
    private const int XdPokemonGenderRatioOffset = 0x02;
    private const int XdPokemonBaseExpOffset = 0x05;
    private const int XdPokemonBaseHappinessOffset = 0x07;
    private const int XdPokemonHeightOffset = 0x08;
    private const int XdPokemonWeightOffset = 0x0a;
    private const int XdPokemonNationalIndexOffset = 0x0e;
    private const int XdPokemonType1Offset = 0x30;
    private const int XdPokemonType2Offset = 0x31;
    private const int XdPokemonAbility1Offset = 0x32;
    private const int XdPokemonAbility2Offset = 0x33;
    private const int XdPokemonFirstTmOffset = 0x34;
    private const int XdPokemonHeldItem1Offset = 0x7a;
    private const int XdPokemonHeldItem2Offset = 0x7c;
    private const int XdPokemonHpOffset = 0x8f;
    private const int XdPokemonAttackOffset = 0x91;
    private const int XdPokemonDefenseOffset = 0x93;
    private const int XdPokemonSpecialAttackOffset = 0x95;
    private const int XdPokemonSpecialDefenseOffset = 0x97;
    private const int XdPokemonSpeedOffset = 0x99;
    private const int XdPokemonEvYieldOffset = 0x9a;
    private const int XdPokemonFirstEvolutionOffset = 0xa6;
    private const int XdPokemonFirstLevelUpMoveOffset = 0xc4;
    private const int XdItemBagSlotOffset = 0x00;
    private const int XdItemCantBeHeldOffset = 0x01;
    private const int XdItemInBattleUseOffset = 0x04;
    private const int XdItemPriceOffset = 0x06;
    private const int XdItemCouponOffset = 0x08;
    private const int XdItemBattleHoldOffset = 0x0b;
    private const int XdItemDescriptionIdOffset = 0x14;
    private const int XdItemParameterOffset = 0x1b;
    private const int XdItemFriendshipOffset = 0x24;
    private const int XdTreasureSize = 0x1c;
    private const int XdTreasureModelOffset = 0x00;
    private const int XdTreasureQuantityOffset = 0x01;
    private const int XdTreasureAngleOffset = 0x02;
    private const int XdTreasureRoomOffset = 0x04;
    private const int XdTreasureFlagOffset = 0x06;
    private const int XdTreasureItemOffset = 0x0e;
    private const int XdTreasureXOffset = 0x10;
    private const int XdTreasureYOffset = 0x14;
    private const int XdTreasureZOffset = 0x18;
    private const int XdRoomSize = 0x44;
    private const int XdStarterSpeciesOffset = 0x02;
    private const int XdStarterLevelOffset = 0x0b;
    private const int XdStarterMove1Offset = 0x12;
    private const int XdStarterMove2Offset = 0x16;
    private const int XdStarterMove3Offset = 0x1a;
    private const int XdStarterMove4Offset = 0x1e;
    private const int XdDemoGiftSpeciesOffset = 0x02;
    private const int XdDemoGiftLevelOffset = 0x07;
    private const int XdDemoGiftMove1Offset = 0x16;
    private const int XdDemoGiftMove2Offset = 0x26;
    private const int XdDemoGiftMove3Offset = 0x36;
    private const int XdDemoGiftMove4Offset = 0x46;
    private const int XdColosseumGiftSpeciesOffset = 0x02;
    private const int XdColosseumGiftLevelOffset = 0x07;
    private const int XdTradeShadowGiftSpeciesOffset = 0x02;
    private const int XdTradeShadowGiftLevelOffset = 0x0b;
    private const int XdTradeShadowGiftMove1Offset = 0x0e;
    private const int XdTradeShadowGiftMove2Offset = 0x12;
    private const int XdTradeShadowGiftMove3Offset = 0x16;
    private const int XdTradeShadowGiftMove4Offset = 0x1a;
    private const int XdTradeGiftSpeciesOffset = 0x02;
    private const int XdTradeGiftLevelOffset = 0x0b;
    private const int XdTradeGiftMove1Offset = 0x26;
    private const int XdTradeGiftMove2Offset = 0x2a;
    private const int XdTradeGiftMove3Offset = 0x2e;
    private const int XdTradeGiftMove4Offset = 0x32;
    private const int XdMtBattleGiftSpeciesOffset = 0x02;
    private const int XdMtBattleGiftMove1Offset = 0x06;
    private const int XdMtBattleGiftMove2Offset = 0x0a;
    private const int XdMtBattleGiftMove3Offset = 0x0e;
    private const int XdMtBattleGiftMove4Offset = 0x12;

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

    public IReadOnlyList<XdTrainerRecord> LoadTrainerRecords()
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "deck_archive.fsys was not found.");
        var common = ReadCommonRelOrThrow();
        var strings = common.Strings;
        var names = BuildCommonNameLookup();
        var shadows = LoadShadowPokemonRecords().ToDictionary(shadow => shadow.Index);
        var storyEntry = FindDeckEntry(archive, "DeckData_Story");
        var storyBytes = storyEntry is null ? null : archive.Extract(storyEntry);
        XdDeckLayout? storyLayout = null;
        if (storyBytes is not null && TryReadDeckLayout(storyBytes, out var parsedStoryLayout, out _))
        {
            storyLayout = parsedStoryLayout;
        }

        var trainers = new List<XdTrainerRecord>();
        foreach (var deckName in XdTrainerDecks)
        {
            var entry = FindDeckEntry(archive, deckName);
            if (entry is null)
            {
                continue;
            }

            var bytes = archive.Extract(entry);
            if (!TryReadDeckLayout(bytes, out var layout, out _))
            {
                continue;
            }

            for (var index = 0; index < layout.TrainerEntries; index++)
            {
                var start = layout.TrainerDataOffset + (index * XdTrainerSize);
                if (start + XdTrainerSize > bytes.Length)
                {
                    break;
                }

                var pokemonIds = Enumerable.Range(0, 6)
                    .Select(slot => ReadU16(bytes, start + XdTrainerFirstPokemonOffset + (slot * 2)))
                    .ToArray();
                var trainerStringOffset = ReadU16(bytes, start + XdTrainerStringOffset);
                var nameId = ReadU16(bytes, start + XdTrainerNameIdOffset);
                var classId = bytes[start + XdTrainerClassOffset];
                if (pokemonIds.All(id => id == 0) && classId == 0 && nameId == 0 && trainerStringOffset == 0)
                {
                    continue;
                }

                var shadowMask = bytes[start + XdTrainerShadowMaskOffset];
                var modelId = bytes[start + XdTrainerModelOffset];
                var trainerString = ReadDeckStringRaw(bytes, layout.StringDataOffset, trainerStringOffset);
                var trainerName = CleanName(strings?.StringWithId(nameId), index, "Trainer");
                var className = $"Class {classId}";
                var location = NormalizeXdLocation(trainerString);
                var pokemon = new List<XdTrainerPokemonRecord>();
                for (var slot = 0; slot < 6; slot++)
                {
                    var pokemonId = pokemonIds[slot];
                    var isShadow = ((shadowMask >> slot) & 1) == 1;
                    pokemon.Add(ReadTrainerPokemonRecord(
                        slot,
                        pokemonId,
                        isShadow,
                        bytes,
                        layout,
                        storyBytes,
                        storyLayout,
                        names,
                        shadows));
                }

                trainers.Add(new XdTrainerRecord(
                    index,
                    deckName.Replace("DeckData_", string.Empty, StringComparison.Ordinal),
                    trainerName,
                    className,
                    classId,
                    modelId,
                    ReadU16(bytes, start + XdTrainerAiOffset),
                    nameId,
                    pokemonIds.FirstOrDefault(),
                    ReadU16(bytes, start + XdTrainerPreBattleTextOffset),
                    ReadU16(bytes, start + XdTrainerVictoryTextOffset),
                    ReadU16(bytes, start + XdTrainerDefeatTextOffset),
                    ReadU16(bytes, start + XdTrainerCameraOffset),
                    location,
                    shadowMask > 0,
                    pokemon));
            }
        }

        return trainers;
    }

    public IReadOnlyList<XdShadowPokemonRecord> LoadShadowPokemonRecords()
    {
        var archive = TryReadFsys("deck_archive.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "deck_archive.fsys was not found.");
        var darkEntry = FindDeckEntry(archive, "DeckData_DarkPokemon")
            ?? throw new InvalidDataException("DeckData_DarkPokemon was not found in deck_archive.fsys.");
        var storyEntry = FindDeckEntry(archive, "DeckData_Story")
            ?? throw new InvalidDataException("DeckData_Story was not found in deck_archive.fsys.");
        var darkBytes = archive.Extract(darkEntry);
        var storyBytes = archive.Extract(storyEntry);
        if (!TryReadDeckLayout(storyBytes, out var storyLayout, out var storyError))
        {
            throw new InvalidDataException(storyError);
        }

        var names = BuildCommonNameLookup();
        var abilityNames = BuildPokemonAbilityNameLookup();
        var rows = new List<XdShadowPokemonRecord>();
        if (darkBytes.Length < 0x20)
        {
            return rows;
        }

        var entries = checked((int)BigEndian.ReadUInt32(darkBytes, 0x18));
        for (var index = 0; index < entries; index++)
        {
            var start = 0x20 + (index * XdShadowPokemonSize);
            if (start + XdShadowPokemonSize > darkBytes.Length)
            {
                break;
            }

            var storyIndex = ReadU16(darkBytes, start + XdShadowStoryIndexOffset);
            var level = darkBytes[start + XdShadowLevelOffset];
            var catchRate = darkBytes[start + XdShadowCatchRateOffset];
            var heartGauge = ReadU16(darkBytes, start + XdShadowHeartGaugeOffset);
            if (storyIndex == 0 && level == 0 && catchRate == 0 && heartGauge == 0)
            {
                continue;
            }

            var species = ReadDeckPokemonSpecies(storyBytes, storyLayout, storyIndex);
            var moveIds = Enumerable.Range(0, 4)
                .Select(slot => ReadU16(darkBytes, start + XdShadowFirstMoveOffset + (slot * 2)))
                .ToArray();
            var storyStart = storyLayout.PokemonDataOffset + (storyIndex * XdDeckPokemonSize);
            var storyInRange = storyStart >= 0 && storyStart + XdDeckPokemonSize <= storyBytes.Length;
            var item = storyInRange ? ReadU16(storyBytes, storyStart + XdDeckPokemonItemOffset) : 0;
            var pid = storyInRange ? storyBytes[storyStart + 0x1e] : 0;
            var ability = pid % 2;
            var regularMoveIds = storyInRange
                ? Enumerable.Range(0, 4)
                    .Select(slot => ReadU16(storyBytes, storyStart + XdDeckPokemonFirstMoveOffset + (slot * 2)))
                    .ToArray()
                : new[] { 0, 0, 0, 0 };

            rows.Add(new XdShadowPokemonRecord(
                index,
                storyIndex,
                species,
                PokemonName(names, species),
                level,
                catchRate,
                heartGauge,
                darkBytes[start + XdShadowInUseOffset],
                darkBytes[start + XdShadowFleeOffset],
                darkBytes[start + XdShadowAggressionOffset],
                darkBytes[start + XdShadowAlwaysFleeOffset],
                moveIds,
                moveIds.Select(move => MoveName(names, move)).ToArray(),
                storyInRange ? storyBytes[storyStart + XdDeckPokemonLevelOffset] : level,
                item,
                ItemName(names, item),
                ability,
                AbilityName(abilityNames, species, ability),
                pid / 8,
                (pid / 2) % 4,
                storyInRange ? storyBytes[storyStart + 0x03] : 0,
                storyInRange ? storyBytes[storyStart + 0x08] : 0,
                storyInRange
                    ? Enumerable.Range(0, 6).Select(ev => (int)storyBytes[storyStart + 0x0e + ev]).ToArray()
                    : new[] { 0, 0, 0, 0, 0, 0 },
                regularMoveIds,
                regularMoveIds.Select(move => MoveName(names, move)).ToArray()));
        }

        return rows;
    }

    public IReadOnlyList<XdPokemonStatsRecord> LoadPokemonStatsRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        var pokemonNames = BuildPokemonNameMap(data, table, strings);
        var itemNames = BuildItemNameMap(data, table, strings);
        var moveNames = BuildMoveNameMap(data, table, strings);
        var typeNames = BuildTypeNameMap(data, table, strings);
        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            throw new InvalidDataException("Pokemon stats table is outside common.rel.");
        }

        var rows = new List<XdPokemonStatsRecord>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdPokemonStatsSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdPokemonStatsNameIdOffset));
            var levelMoves = Enumerable.Range(0, XdPokemonLevelUpMoveCount)
                .Select(row =>
                {
                    var moveOffset = offset + XdPokemonFirstLevelUpMoveOffset + (row * 4);
                    var moveId = data.ReadUInt16(moveOffset + 2);
                    return new XdLevelUpMoveRecord(data.ReadByte(moveOffset), moveId, MoveNameById(moveNames, moveId));
                })
                .ToArray();
            var evolutions = Enumerable.Range(0, XdPokemonEvolutionCount)
                .Select(row =>
                {
                    var evolutionOffset = offset + XdPokemonFirstEvolutionOffset + (row * 6);
                    var evolvedSpecies = data.ReadUInt16(evolutionOffset + 4);
                    return new XdEvolutionRecord(
                        data.ReadByte(evolutionOffset),
                        data.ReadUInt16(evolutionOffset + 2),
                        evolvedSpecies,
                        PokemonName(pokemonNames, evolvedSpecies));
                })
                .ToArray();

            rows.Add(new XdPokemonStatsRecord(
                index,
                offset,
                CleanName(strings?.StringWithId(nameId), index, "Pokemon"),
                nameId,
                data.ReadUInt16(offset + XdPokemonNationalIndexOffset),
                data.ReadByte(offset + XdPokemonExpRateOffset),
                data.ReadByte(offset + XdPokemonGenderRatioOffset),
                data.ReadByte(offset + XdPokemonBaseExpOffset),
                data.ReadByte(offset + XdPokemonBaseHappinessOffset),
                data.ReadUInt16(offset + XdPokemonHeightOffset) / 10.0,
                data.ReadUInt16(offset + XdPokemonWeightOffset) / 10.0,
                data.ReadByte(offset + XdPokemonType1Offset),
                TypeName(typeNames, data.ReadByte(offset + XdPokemonType1Offset)),
                data.ReadByte(offset + XdPokemonType2Offset),
                TypeName(typeNames, data.ReadByte(offset + XdPokemonType2Offset)),
                data.ReadByte(offset + XdPokemonAbility1Offset),
                Gen3AbilityName(data.ReadByte(offset + XdPokemonAbility1Offset)),
                data.ReadByte(offset + XdPokemonAbility2Offset),
                Gen3AbilityName(data.ReadByte(offset + XdPokemonAbility2Offset)),
                data.ReadUInt16(offset + XdPokemonHeldItem1Offset),
                ItemNameById(itemNames, data.ReadUInt16(offset + XdPokemonHeldItem1Offset)),
                data.ReadUInt16(offset + XdPokemonHeldItem2Offset),
                ItemNameById(itemNames, data.ReadUInt16(offset + XdPokemonHeldItem2Offset)),
                data.ReadByte(offset + XdPokemonCatchRateOffset),
                data.ReadByte(offset + XdPokemonHpOffset),
                data.ReadByte(offset + XdPokemonAttackOffset),
                data.ReadByte(offset + XdPokemonDefenseOffset),
                data.ReadByte(offset + XdPokemonSpecialAttackOffset),
                data.ReadByte(offset + XdPokemonSpecialDefenseOffset),
                data.ReadByte(offset + XdPokemonSpeedOffset),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset + 2),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset + 4),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset + 6),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset + 8),
                data.ReadUInt16(offset + XdPokemonEvYieldOffset + 10),
                Enumerable.Range(0, XdPokemonTmCount).Select(tm => data.ReadByte(offset + XdPokemonFirstTmOffset + tm) == 1).ToArray(),
                levelMoves,
                evolutions));
        }

        return rows;
    }

    public IReadOnlyList<XdMoveRecord> LoadMoveRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        var moveNames = BuildMoveNameMap(data, table, strings);
        var typeNames = BuildTypeNameMap(data, table, strings);
        var start = table.GetPointer(XdMovesIndex);
        var count = table.GetValueAtPointer(XdNumberOfMovesIndex);
        if (!IsSafeTableRange(data, start, count, XdMoveSize, maxCount: 1000))
        {
            throw new InvalidDataException("Move table is outside common.rel.");
        }

        var rows = new List<XdMoveRecord>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdMoveSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdMoveNameIdOffset));
            var descriptionId = checked((int)data.ReadUInt32(offset + XdMoveDescriptionIdOffset));
            var typeId = data.ReadByte(offset + XdMoveTypeOffset);
            var priorityByte = data.ReadByte(offset + XdMovePriorityOffset);
            rows.Add(new XdMoveRecord(
                index,
                offset,
                CleanName(strings?.StringWithId(nameId), index, "Move"),
                nameId,
                $"String {descriptionId}",
                descriptionId,
                typeId,
                TypeName(typeNames, typeId),
                data.ReadByte(offset + XdMoveTargetsOffset),
                data.ReadByte(offset + XdMoveCategoryOffset),
                data.ReadUInt16(offset + XdMoveAnimationOffset),
                data.ReadUInt16(offset + XdMoveAnimation2Offset),
                data.ReadUInt16(offset + XdMoveEffectOffset),
                data.ReadByte(offset + XdMoveEffectTypeOffset),
                data.ReadByte(offset + XdMoveBasePowerOffset),
                data.ReadByte(offset + XdMoveAccuracyOffset),
                data.ReadByte(offset + XdMovePpOffset),
                priorityByte > 128 ? priorityByte - 256 : priorityByte,
                data.ReadByte(offset + XdMoveEffectAccuracyOffset),
                data.ReadByte(offset + XdMoveHmFlagOffset) == 1,
                data.ReadByte(offset + XdMoveSoundBasedFlagOffset) == 1,
                data.ReadByte(offset + XdMoveContactFlagOffset) == 1,
                data.ReadByte(offset + XdMoveKingsRockFlagOffset) == 1,
                data.ReadByte(offset + XdMoveProtectFlagOffset) == 1,
                data.ReadByte(offset + XdMoveSnatchFlagOffset) == 1,
                data.ReadByte(offset + XdMoveMagicCoatFlagOffset) == 1,
                data.ReadByte(offset + XdMoveMirrorMoveFlagOffset) == 1,
                index >= 355 || MoveNameById(moveNames, index).Contains("SHADOW", StringComparison.OrdinalIgnoreCase)));
        }

        return rows;
    }

    public IReadOnlyList<XdItemRecord> LoadItemRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        var itemNames = BuildItemNameMap(data, table, strings);
        var start = table.GetPointer(XdItemsIndex);
        var count = table.GetValueAtPointer(XdNumberOfItemsIndex);
        if (!IsSafeTableRange(data, start, count, XdItemSize, maxCount: 2000))
        {
            throw new InvalidDataException("Item table is outside common.rel.");
        }

        var rows = new List<XdItemRecord>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdItemSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdItemNameIdOffset));
            var descriptionId = checked((int)data.ReadUInt32(offset + XdItemDescriptionIdOffset));
            rows.Add(new XdItemRecord(
                index,
                offset,
                CleanName(strings?.StringWithId(nameId), index, "Item"),
                nameId,
                $"String {descriptionId}",
                descriptionId,
                data.ReadByte(offset + XdItemBagSlotOffset),
                data.ReadByte(offset + XdItemCantBeHeldOffset) == 0,
                data.ReadUInt16(offset + XdItemPriceOffset),
                data.ReadUInt16(offset + XdItemCouponOffset),
                data.ReadByte(offset + XdItemParameterOffset),
                data.ReadByte(offset + XdItemBattleHoldOffset),
                data.ReadByte(offset + XdItemInBattleUseOffset),
                Enumerable.Range(0, 3).Select(i => SignedByte(data.ReadByte(offset + XdItemFriendshipOffset + i))).ToArray()));
        }

        return rows;
    }

    public IReadOnlyList<XdTypeRecord> LoadTypeRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        return LoadTypeRecords(data, table, strings);
    }

    public IReadOnlyList<XdTreasureRecord> LoadTreasureRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        var itemNames = BuildItemNameMap(data, table, strings);
        var rooms = BuildRoomNameMap(data, table);
        var start = table.GetPointer(XdTreasureIndex);
        var count = table.GetValueAtPointer(XdNumberOfTreasuresIndex);
        if (!IsSafeTableRange(data, start, count, XdTreasureSize, maxCount: 1000))
        {
            throw new InvalidDataException("Treasure table is outside common.rel.");
        }

        var rows = new List<XdTreasureRecord>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdTreasureSize);
            var room = data.ReadUInt16(offset + XdTreasureRoomOffset);
            var item = data.ReadUInt16(offset + XdTreasureItemOffset);
            rows.Add(new XdTreasureRecord(
                index,
                offset,
                data.ReadByte(offset + XdTreasureModelOffset),
                data.ReadByte(offset + XdTreasureQuantityOffset),
                data.ReadUInt16(offset + XdTreasureAngleOffset),
                room,
                rooms.TryGetValue(room, out var roomName) ? roomName : $"Room 0x{room:x4}",
                data.ReadUInt16(offset + XdTreasureFlagOffset),
                item,
                ItemNameById(itemNames, item),
                ReadSingle(data, offset + XdTreasureXOffset),
                ReadSingle(data, offset + XdTreasureYOffset),
                ReadSingle(data, offset + XdTreasureZOffset)));
        }

        return rows;
    }

    public IReadOnlyList<XdPokespotRecord> LoadPokespotRecords()
    {
        var (data, table, strings) = ReadCommonRelOrThrow();
        var names = BuildPokemonNameMap(data, table, strings);
        var rows = new List<XdPokespotRecord>();
        foreach (var spot in XdPokespotTargets)
        {
            var offset = table.GetPointer(spot.PointerIndex);
            var count = table.GetValueAtPointer(spot.CountIndex);
            if (!IsSafeTableRange(data, offset, count, XdPokespotEntrySize, maxCount: 64))
            {
                continue;
            }

            for (var index = 0; index < count; index++)
            {
                var start = offset + (index * XdPokespotEntrySize);
                var species = data.ReadUInt16(start + 2);
                rows.Add(new XdPokespotRecord(
                    index,
                    spot.Name,
                    species,
                    PokemonName(names, species),
                    data.ReadByte(start),
                    data.ReadByte(start + 1),
                    data.ReadByte(start + 7),
                    data.ReadUInt16(start + 0x0a),
                    start));
            }
        }

        return rows;
    }

    public IReadOnlyList<XdGiftPokemonRecord> LoadGiftPokemonRecords()
    {
        var dol = ReadStartDolOrThrow();
        var names = BuildCommonNameLookup();
        var statsBySpecies = LoadPokemonStatsRecords().ToDictionary(pokemon => pokemon.Index);
        return XdGiftLayouts(Iso.Region)
            .Select(layout => ReadGiftPokemon(dol, layout, names, statsBySpecies))
            .ToArray();
    }

    private static XdGiftPokemonRecord ReadGiftPokemon(
        BinaryData dol,
        XdGiftLayout layout,
        IReadOnlyDictionary<int, string> names,
        IReadOnlyDictionary<int, XdPokemonStatsRecord> statsBySpecies)
    {
        var species = dol.ReadUInt16(layout.StartOffset + layout.SpeciesOffset);
        var level = layout.LevelOffset >= 0
            ? dol.ReadByte(layout.StartOffset + layout.LevelOffset)
            : dol.ReadByte(layout.SharedLevelOffset);
        var moveIds = layout.UsesLevelUpMoves
            ? DefaultMovesForLevel(statsBySpecies, species, level)
            : layout.MoveOffsets.Select(offset => (int)dol.ReadUInt16(layout.StartOffset + offset)).ToArray();

        return new XdGiftPokemonRecord(
            layout.RowId,
            layout.DataIndex,
            layout.StartOffset,
            layout.GiftType,
            species,
            PokemonName(names, species),
            level,
            moveIds,
            moveIds.Select(move => MoveName(names, move)).ToArray(),
            layout.UsesLevelUpMoves);
    }

    private static IReadOnlyList<int> DefaultMovesForLevel(
        IReadOnlyDictionary<int, XdPokemonStatsRecord> statsBySpecies,
        int species,
        int level)
    {
        if (!statsBySpecies.TryGetValue(species, out var stats))
        {
            return [0, 0, 0, 0];
        }

        var moves = stats.LevelUpMoves
            .Where(move => move.MoveId > 0 && move.Level <= level)
            .Select(move => move.MoveId)
            .Distinct()
            .TakeLast(4)
            .ToList();
        while (moves.Count < 4)
        {
            moves.Insert(0, 0);
        }

        return moves;
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

    private IReadOnlyDictionary<int, (string FirstAbility, string SecondAbility)> BuildPokemonAbilityNameLookup()
    {
        if (!TryReadCommonRel(out var data, out var table, out _, out _) || data is null || table is null)
        {
            return new Dictionary<int, (string FirstAbility, string SecondAbility)>();
        }

        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            return new Dictionary<int, (string FirstAbility, string SecondAbility)>();
        }

        var names = new Dictionary<int, (string FirstAbility, string SecondAbility)>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdPokemonStatsSize);
            names[index] = (
                Gen3AbilityName(data.ReadByte(offset + XdPokemonAbility1Offset)),
                Gen3AbilityName(data.ReadByte(offset + XdPokemonAbility2Offset)));
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

    private static IReadOnlyList<XdGiftLayout> XdGiftLayouts(GameCubeRegion region)
    {
        var mtLevelOffset = MtBattleLevelOffset(region) + 3;
        return
        [
            new(0, 0, GiftOffset(region, 0x1CBC50, 0x1CD724, 0x1C6AF4), "Starter Pokemon", XdStarterSpeciesOffset, XdStarterLevelOffset, [XdStarterMove1Offset, XdStarterMove2Offset, XdStarterMove3Offset, XdStarterMove4Offset], false, -1),
            new(1, 0, GiftOffset(region, 0x14F73C, 0x151000, 0x14AA64), "Demo Starter Pokemon", XdDemoGiftSpeciesOffset, XdDemoGiftLevelOffset, [XdDemoGiftMove1Offset, XdDemoGiftMove2Offset, XdDemoGiftMove3Offset, XdDemoGiftMove4Offset], false, -1),
            new(2, 1, GiftOffset(region, 0x14F614, 0x150ED8, 0x14A93C), "Demo Starter Pokemon", XdDemoGiftSpeciesOffset, XdDemoGiftLevelOffset, [XdDemoGiftMove1Offset, XdDemoGiftMove2Offset, XdDemoGiftMove3Offset, XdDemoGiftMove4Offset], false, -1),
            new(3, 0, GiftOffset(region, 0x14F514, 0x150DD8, 0x14A83C), "Duking's Plusle", XdColosseumGiftSpeciesOffset, XdColosseumGiftLevelOffset, [], true, -1),
            new(4, 1, GiftOffset(region, 0x14F430, 0x150CF4, 0x14A758), "Mt.Battle Ho-oh", XdColosseumGiftSpeciesOffset, XdColosseumGiftLevelOffset, [], true, -1),
            new(5, 3, GiftOffset(region, 0x14F310, 0x150BD4, 0x14A638), "Agate Pikachu", XdColosseumGiftSpeciesOffset, XdColosseumGiftLevelOffset, [], true, -1),
            new(6, 2, GiftOffset(region, 0x14F200, 0x150AC4, 0x14A528), "Agate Celebi", XdColosseumGiftSpeciesOffset, XdColosseumGiftLevelOffset, [], true, -1),
            new(7, 0, GiftOffset(region, 0x1C5760, 0x1C705C, 0x1C0C70), "Shadow Pokemon Gift", XdTradeShadowGiftSpeciesOffset, XdTradeShadowGiftLevelOffset, [XdTradeShadowGiftMove1Offset, XdTradeShadowGiftMove2Offset, XdTradeShadowGiftMove3Offset, XdTradeShadowGiftMove4Offset], false, -1),
            new(8, 0, GiftOffset(region, 0x1C57A4, 0x1C70A0, 0x1C0CB4), "Hordel Trade", XdTradeGiftSpeciesOffset, XdTradeGiftLevelOffset, [XdTradeGiftMove1Offset, XdTradeGiftMove2Offset, XdTradeGiftMove3Offset, XdTradeGiftMove4Offset], false, -1),
            new(9, 1, GiftOffset(region, 0x1C5888, 0x1C7184, 0x1C0D1C), "Duking Trade", XdTradeGiftSpeciesOffset, XdTradeGiftLevelOffset, [XdTradeGiftMove1Offset, XdTradeGiftMove2Offset, XdTradeGiftMove3Offset, XdTradeGiftMove4Offset], false, -1),
            new(10, 2, GiftOffset(region, 0x1C58D8, 0x1C71D4, 0x1C0D6C), "Duking Trade", XdTradeGiftSpeciesOffset, XdTradeGiftLevelOffset, [XdTradeGiftMove1Offset, XdTradeGiftMove2Offset, XdTradeGiftMove3Offset, XdTradeGiftMove4Offset], false, -1),
            new(11, 3, GiftOffset(region, 0x1C5928, 0x1C7224, 0x1C0DBC), "Duking Trade", XdTradeGiftSpeciesOffset, XdTradeGiftLevelOffset, [XdTradeGiftMove1Offset, XdTradeGiftMove2Offset, XdTradeGiftMove3Offset, XdTradeGiftMove4Offset], false, -1),
            new(12, 0, GiftOffset(region, 0x1C5974, 0x1C7270, 0x1C0E08), "Mt. Battle Prize", XdMtBattleGiftSpeciesOffset, -1, [XdMtBattleGiftMove1Offset, XdMtBattleGiftMove2Offset, XdMtBattleGiftMove3Offset, XdMtBattleGiftMove4Offset], false, mtLevelOffset),
            new(13, 1, GiftOffset(region, 0x1C59A0, 0x1C729C, 0x1C0E34), "Mt. Battle Prize", XdMtBattleGiftSpeciesOffset, -1, [XdMtBattleGiftMove1Offset, XdMtBattleGiftMove2Offset, XdMtBattleGiftMove3Offset, XdMtBattleGiftMove4Offset], false, mtLevelOffset),
            new(14, 2, GiftOffset(region, 0x1C59CC, 0x1C72C8, 0x1C0E60), "Mt. Battle Prize", XdMtBattleGiftSpeciesOffset, -1, [XdMtBattleGiftMove1Offset, XdMtBattleGiftMove2Offset, XdMtBattleGiftMove3Offset, XdMtBattleGiftMove4Offset], false, mtLevelOffset)
        ];
    }

    private static int GiftOffset(GameCubeRegion region, int us, int eu, int jp)
        => region switch
        {
            GameCubeRegion.UnitedStates => us,
            GameCubeRegion.Europe => eu,
            GameCubeRegion.Japan => jp,
            _ => 0
        };

    private static int MtBattleLevelOffset(GameCubeRegion region)
        => GiftOffset(region, 0x1C56E8, 0x1C6FE4, 0x1C0BF8);

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

    private XdTrainerPokemonRecord ReadTrainerPokemonRecord(
        int slot,
        int deckIndex,
        bool isShadow,
        byte[] deckBytes,
        XdDeckLayout deckLayout,
        byte[]? storyBytes,
        XdDeckLayout? storyLayout,
        IReadOnlyDictionary<int, string> names,
        IReadOnlyDictionary<int, XdShadowPokemonRecord> shadows)
    {
        if (deckIndex <= 0)
        {
            return EmptyTrainerPokemonRecord(slot);
        }

        XdShadowPokemonRecord? shadow = null;
        var pokemonIndex = deckIndex;
        var pokemonBytes = deckBytes;
        var pokemonLayout = deckLayout;
        if (isShadow)
        {
            shadows.TryGetValue(deckIndex, out shadow);
            if (shadow is not null && storyBytes is not null && storyLayout is not null)
            {
                pokemonIndex = shadow.StoryPokemonIndex;
                pokemonBytes = storyBytes;
                pokemonLayout = storyLayout;
            }
        }

        var start = pokemonLayout.PokemonDataOffset + (pokemonIndex * XdDeckPokemonSize);
        if (start < 0 || start + XdDeckPokemonSize > pokemonBytes.Length)
        {
            return EmptyTrainerPokemonRecord(slot) with { DeckIndex = deckIndex, ShadowId = isShadow ? deckIndex : 0, ShadowData = shadow };
        }

        var species = ReadU16(pokemonBytes, start + XdDeckPokemonSpeciesOffset);
        var item = ReadU16(pokemonBytes, start + XdDeckPokemonItemOffset);
        var pid = pokemonBytes[start + 0x1e];
        var moveIds = Enumerable.Range(0, 4)
            .Select(move => ReadU16(pokemonBytes, start + XdDeckPokemonFirstMoveOffset + (move * 2)))
            .ToArray();
        if (shadow is not null)
        {
            moveIds = shadow.MoveIds.ToArray();
        }

        return new XdTrainerPokemonRecord(
            slot,
            deckIndex,
            shadow?.SpeciesId ?? species,
            shadow?.SpeciesName ?? PokemonName(names, species),
            shadow?.Level ?? pokemonBytes[start + XdDeckPokemonLevelOffset],
            isShadow ? deckIndex : 0,
            item,
            ItemName(names, item),
            pid % 2,
            pid / 8,
            (pid / 2) % 4,
            pokemonBytes[start + 0x03],
            pokemonBytes[start + 0x08],
            Enumerable.Range(0, 6).Select(ev => (int)pokemonBytes[start + 0x0e + ev]).ToArray(),
            moveIds,
            moveIds.Select(move => MoveName(names, move)).ToArray(),
            shadow);
    }

    private static XdTrainerPokemonRecord EmptyTrainerPokemonRecord(int slot)
        => new(
            slot,
            0,
            0,
            "-",
            0,
            0,
            0,
            "-",
            0,
            0,
            0,
            0,
            0,
            [0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0],
            ["-", "-", "-", "-"],
            null);

    private (BinaryData Data, RelocationTable Table, GameStringTable? Strings) ReadCommonRelOrThrow()
    {
        if (!TryReadCommonRel(out var data, out var table, out var strings, out var error) || data is null || table is null)
        {
            throw new InvalidDataException(error ?? "Could not parse common.rel.");
        }

        return (data, table, strings);
    }

    private BinaryData ReadStartDolOrThrow()
    {
        var entry = FindIsoFile("Start.dol")
            ?? throw new FileNotFoundException("Start.dol was not found in the ISO.");
        return new BinaryData(GameCubeIsoReader.ReadFile(Iso, entry));
    }

    private static IReadOnlyDictionary<int, string> BuildTypeNameMap(BinaryData data, RelocationTable table, GameStringTable? strings)
    {
        var start = table.GetPointer(XdTypeIndex);
        var count = table.GetValueAtPointer(XdNumberOfTypesIndex);
        if (!IsSafeTableRange(data, start, count, XdTypeSize, maxCount: 64))
        {
            return new Dictionary<int, string>();
        }

        var names = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdTypeSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdTypeNameIdOffset));
            names[index] = CleanName(strings?.StringWithId(nameId), index, "Type");
        }

        return names;
    }

    private static IReadOnlyList<XdTypeRecord> LoadTypeRecords(BinaryData data, RelocationTable table, GameStringTable? strings)
    {
        var start = table.GetPointer(XdTypeIndex);
        var count = table.GetValueAtPointer(XdNumberOfTypesIndex);
        if (!IsSafeTableRange(data, start, count, XdTypeSize, maxCount: 64))
        {
            throw new InvalidDataException("Type table is outside common.rel.");
        }

        var rows = new List<XdTypeRecord>();
        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * XdTypeSize);
            var nameId = checked((int)data.ReadUInt32(offset + XdTypeNameIdOffset));
            rows.Add(new XdTypeRecord(
                index,
                offset,
                CleanName(strings?.StringWithId(nameId), index, "Type"),
                nameId,
                data.ReadByte(offset + XdTypeCategoryOffset),
                Enumerable.Range(0, count)
                    .Select(typeIndex => (int)data.ReadByte(offset + XdTypeFirstEffectivenessOffset + typeIndex))
                    .ToArray()));
        }

        return rows;
    }

    private static IReadOnlyDictionary<int, string> BuildRoomNameMap(BinaryData data, RelocationTable table)
    {
        var start = table.GetPointer(XdRoomsIndex);
        var count = table.GetValueAtPointer(XdNumberOfRoomsIndex);
        if (!IsSafeTableRange(data, start, count, XdRoomSize, maxCount: 1000))
        {
            return new Dictionary<int, string>();
        }

        var rooms = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            rooms[index] = $"Room 0x{index:x4}";
        }

        return rooms;
    }

    private static string ReadDeckStringRaw(byte[] bytes, int stringDataOffset, int offset)
    {
        var start = stringDataOffset + offset;
        if (start < 0 || start >= bytes.Length)
        {
            return "-";
        }

        var end = start;
        while (end < bytes.Length && bytes[end] != 0)
        {
            end++;
        }

        return end == start ? "-" : System.Text.Encoding.ASCII.GetString(bytes, start, end - start);
    }

    private static string NormalizeXdLocation(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "NULL")
        {
            return "-";
        }

        var text = value
            .Replace("_col_", " Colosseum ", StringComparison.Ordinal)
            .Replace("555_", "Battle ", StringComparison.Ordinal)
            .Replace("Esaba", "Pokespot", StringComparison.Ordinal)
            .Replace("Mirabo", "Miror B.", StringComparison.Ordinal)
            .Replace("mirabo", "Miror B.", StringComparison.Ordinal)
            .Replace("haihu", "Gift", StringComparison.Ordinal)
            .Replace('_', ' ')
            .Trim();

        if (text.StartsWith('N'))
        {
            return "Mt.Battle Zone " + text[1..];
        }

        if (text.StartsWith('P'))
        {
            return "Pyrite Colosseum " + text[1..];
        }

        if (text.StartsWith('O'))
        {
            return "Orre Colosseum " + text[1..];
        }

        if (text.StartsWith('T'))
        {
            return "Tower Colosseum " + text[1..];
        }

        return text;
    }

    private static string CleanNameById(IReadOnlyDictionary<int, string> names, int id, string fallback)
        => names.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : fallback;

    private static string TypeName(IReadOnlyDictionary<int, string> names, int type)
        => names.TryGetValue(type, out var name) ? name : $"Type {type}";

    private static string MoveNameById(IReadOnlyDictionary<int, string> names, int move)
        => names.TryGetValue(move, out var name) ? name : $"Move {move}";

    private static string ItemNameById(IReadOnlyDictionary<int, string> names, int item)
        => names.TryGetValue(item, out var name) ? name : $"Item {item}";

    private static int SignedByte(byte value)
        => value > 127 ? value - 256 : value;

    private static float ReadSingle(BinaryData data, int offset)
    {
        var bytes = data.ReadBytes(offset, 4);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToSingle(bytes, 0);
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

    private static string AbilityName(IReadOnlyDictionary<int, (string FirstAbility, string SecondAbility)> names, int species, int abilitySlot)
        => names.TryGetValue(species, out var pair)
            ? abilitySlot == 1 ? pair.SecondAbility : pair.FirstAbility
            : $"Ability {abilitySlot}";

    private static string Gen3AbilityName(int ability)
        => ability >= 0 && ability < Gen3AbilityNames.Length
            ? Gen3AbilityNames[ability]
            : $"Ability {ability}";

    private static int MoveNameKey(int move) => 0x10000 + move;

    private static int ItemNameKey(int item) => 0x20000 + item;

    private static readonly string[] Gen3AbilityNames =
    [
        "---",
        "STENCH",
        "DRIZZLE",
        "SPEED BOOST",
        "BATTLE ARMOR",
        "STURDY",
        "DAMP",
        "LIMBER",
        "SAND VEIL",
        "STATIC",
        "VOLT ABSORB",
        "WATER ABSORB",
        "OBLIVIOUS",
        "CLOUD NINE",
        "COMPOUND EYES",
        "INSOMNIA",
        "COLOR CHANGE",
        "IMMUNITY",
        "FLASH FIRE",
        "SHIELD DUST",
        "OWN TEMPO",
        "SUCTION CUPS",
        "INTIMIDATE",
        "SHADOW TAG",
        "ROUGH SKIN",
        "WONDER GUARD",
        "LEVITATE",
        "EFFECT SPORE",
        "SYNCHRONIZE",
        "CLEAR BODY",
        "NATURAL CURE",
        "LIGHTNING ROD",
        "SERENE GRACE",
        "SWIFT SWIM",
        "CHLOROPHYLL",
        "ILLUMINATE",
        "TRACE",
        "HUGE POWER",
        "POISON POINT",
        "INNER FOCUS",
        "MAGMA ARMOR",
        "WATER VEIL",
        "MAGNET PULL",
        "SOUNDPROOF",
        "RAIN DISH",
        "SAND STREAM",
        "PRESSURE",
        "THICK FAT",
        "EARLY BIRD",
        "FLAME BODY",
        "RUN AWAY",
        "KEEN EYE",
        "HYPER CUTTER",
        "PICKUP",
        "TRUANT",
        "HUSTLE",
        "CUTE CHARM",
        "PLUS",
        "MINUS",
        "FORECAST",
        "STICKY HOLD",
        "SHED SKIN",
        "GUTS",
        "MARVEL SCALE",
        "LIQUID OOZE",
        "OVERGROW",
        "BLAZE",
        "TORRENT",
        "SWARM",
        "ROCK HEAD",
        "DROUGHT",
        "ARENA TRAP",
        "VITAL SPIRIT",
        "WHITE SMOKE",
        "PURE POWER",
        "SHELL ARMOR",
        "CACOPHONY",
        "AIR LOCK"
    ];

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

    private sealed record XdGiftLayout(
        int RowId,
        int DataIndex,
        int StartOffset,
        string GiftType,
        int SpeciesOffset,
        int LevelOffset,
        IReadOnlyList<int> MoveOffsets,
        bool UsesLevelUpMoves,
        int SharedLevelOffset);

    private sealed record XdPokespotTarget(string Name, int PointerIndex, int CountIndex);
}
