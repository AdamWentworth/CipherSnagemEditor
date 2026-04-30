using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Relocation;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    private const int MinimumRandomStatSmallTotal = 10;
    private const int MinimumRandomStatLargeTotal = 40;
    private const int WonderGuardAbilityId = 25;
    private const int XdPocketMartItemsIndex = 4;
    private const int XdPocketNumberOfMartItemsIndex = 5;

    public XdRandomizerApplyResult Randomize(XdRandomizerOptions options)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("No Pokemon XD ISO is loaded.");
        }

        var writtenFiles = new List<string>();
        var messages = new List<string>();
        var commonChanged = false;
        var (data, table, _) = ReadCommonRelOrThrow();

        if (options.StarterPokemon || options.ObtainablePokemon || options.UnobtainablePokemon)
        {
            var speciesChanges = RandomizePokemonSpecies(data, table, options, writtenFiles);
            commonChanged |= speciesChanges.Pokespots > 0;
            messages.Add(
                $"Randomized XD Pokemon species: {speciesChanges.Gifts:N0} gifts, {speciesChanges.Pokespots:N0} Pokespot rows, {speciesChanges.TrainerPokemon:N0} trainer/deck Pokemon.");
        }

        if (options.PokemonMoves)
        {
            var count = RandomizePokemonLevelUpMoves(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD Pokemon level-up moves for {count:N0} Pokemon.");
        }

        if (options.PokemonTypes)
        {
            var count = RandomizePokemonTypes(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD Pokemon types for {count:N0} Pokemon.");
        }

        if (options.PokemonAbilities)
        {
            var count = RandomizePokemonAbilities(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD Pokemon abilities for {count:N0} Pokemon.");
        }

        if (options.PokemonStats)
        {
            var count = RandomizePokemonBaseStats(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD base stats for {count:N0} Pokemon.");
        }

        if (options.PokemonEvolutions)
        {
            var count = RandomizePokemonEvolutions(data, table);
            commonChanged = true;
            messages.Add($"Randomized {count:N0} XD evolution target(s).");
        }

        if (options.MoveTypes)
        {
            var count = RandomizeMoveTypes(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD move types for {count:N0} moves.");
        }

        if (options.TypeMatchups)
        {
            var count = RandomizeTypeMatchups(data, table);
            commonChanged = true;
            messages.Add($"Randomized XD type matchup rows for {count:N0} types.");
        }

        if (options.ItemBoxes)
        {
            var count = RandomizeTreasureBoxes(data, table);
            commonChanged = true;
            messages.Add($"Randomized {count:N0} XD treasure box item(s).");
        }

        if (options.RemoveItemOrTradeEvolutions)
        {
            var tradeChanges = RewriteEvolutionMethods(data, table, method => method is EvolutionMethodTrade or EvolutionMethodTradeWithItem);
            var itemChanges = RewriteEvolutionMethods(data, table, method => method == EvolutionMethodStone);
            commonChanged = true;
            messages.Add($"Converted {tradeChanges:N0} trade evolution(s) and {itemChanges:N0} evolution-stone evolution(s) to level {EvolutionPatchLevel}.");
        }

        if (commonChanged)
        {
            writtenFiles.Add(WriteCommonRel(data));
        }

        if (options.TmMoves)
        {
            writtenFiles.Add(RandomizeTmMoves(data, table, messages));
        }

        if (options.ShopItems)
        {
            var shopResult = RandomizePocketMenuShops(data, table);
            writtenFiles.Add(shopResult.PocketMenuRelPath);
            commonChanged = true;
            messages.Add($"Randomized {shopResult.ChangedItems:N0} XD shop item slot(s).");
        }

        if (options.BattleBingo)
        {
            messages.Add("XD Battle Bingo randomization is pending Battle Bingo card table parity.");
        }

        if (options.ShinyHues)
        {
            messages.Add("XD shiny hue randomization is pending Start.dol assembly parity.");
        }

        if (messages.Count == 0)
        {
            messages.Add("No randomizer options were selected.");
        }

        return new XdRandomizerApplyResult(writtenFiles, messages);
    }

    private int RandomizePokemonLevelUpMoves(BinaryData data, RelocationTable table)
    {
        var movePool = EligibleMoveIds(data, table, includeShadow: false);
        if (movePool.Count == 0)
        {
            return 0;
        }

        var start = PokemonStatsTableStart(data, table, out var count);
        var changedPokemon = 0;
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0)
            {
                continue;
            }

            var usedMoveIds = new HashSet<int>();
            var changed = false;
            for (var row = 0; row < XdPokemonLevelUpMoveCount; row++)
            {
                var moveOffset = rowOffset + XdPokemonFirstLevelUpMoveOffset + (row * 4);
                if (data.ReadByte(moveOffset) == 0)
                {
                    continue;
                }

                data.WriteUInt16(moveOffset + 2, ClampUInt16ToU16(RandomMoveId(movePool, usedMoveIds)));
                changed = true;
            }

            if (changed)
            {
                changedPokemon++;
            }
        }

        return changedPokemon;
    }

    private XdRandomizedSpeciesCounts RandomizePokemonSpecies(
        BinaryData data,
        RelocationTable table,
        XdRandomizerOptions options,
        ICollection<string> writtenFiles)
    {
        var speciesPool = EligibleSpecies(data, table);
        if (speciesPool.Count == 0)
        {
            return new XdRandomizedSpeciesCounts(0, 0, 0);
        }

        var speciesById = speciesPool.ToDictionary(species => species.Index);
        var usedObtainableSpecies = new HashSet<int>();
        var gifts = RandomizeGiftPokemonSpecies(data, table, speciesPool, usedObtainableSpecies, options, writtenFiles);
        var pokespots = options.ObtainablePokemon
            ? RandomizePokespotSpecies(data, table, speciesPool, speciesById, usedObtainableSpecies, options.SimilarBaseStatTotal)
            : 0;
        var trainerPokemon = RandomizeDeckPokemonSpecies(data, table, speciesPool, speciesById, usedObtainableSpecies, options, writtenFiles);
        return new XdRandomizedSpeciesCounts(gifts, pokespots, trainerPokemon);
    }

    private int RandomizeGiftPokemonSpecies(
        BinaryData data,
        RelocationTable table,
        IReadOnlyList<XdSpeciesInfo> speciesPool,
        ISet<int> usedObtainableSpecies,
        XdRandomizerOptions options,
        ICollection<string> writtenFiles)
    {
        var layouts = XdGiftLayouts(Iso.Region);
        if (layouts.Count == 0)
        {
            return 0;
        }

        var dolEntry = FindIsoFile("Start.dol")
            ?? throw new FileNotFoundException("Start.dol was not found in the ISO.");
        var dol = new BinaryData(GameCubeIsoReader.ReadFile(Iso, dolEntry));
        var changed = 0;
        foreach (var layout in layouts)
        {
            var isStarter = layout.GiftType.Contains("Starter", StringComparison.OrdinalIgnoreCase);
            if ((isStarter && !options.StarterPokemon) || (!isStarter && !options.ObtainablePokemon))
            {
                continue;
            }

            var speciesOffset = layout.StartOffset + layout.SpeciesOffset;
            if (speciesOffset < 0 || speciesOffset + 2 > dol.Length)
            {
                continue;
            }

            var oldSpecies = dol.ReadUInt16(speciesOffset);
            var level = GiftLevel(dol, layout);
            var newSpecies = RandomSpeciesFor(oldSpecies, speciesPool, options.SimilarBaseStatTotal, usedObtainableSpecies);
            dol.WriteUInt16(speciesOffset, ClampUInt16ToU16(newSpecies.Index));
            if (!layout.UsesLevelUpMoves)
            {
                var moves = DefaultMovesForLevel(data, table, newSpecies.Index, level);
                for (var move = 0; move < layout.MoveOffsets.Count; move++)
                {
                    dol.WriteUInt16(layout.StartOffset + layout.MoveOffsets[move], ClampUInt16ToU16(ValueAt(moves, move)));
                }
            }

            changed++;
        }

        if (changed > 0)
        {
            writtenFiles.Add(WriteStartDol(dol, dolEntry));
        }

        return changed;
    }

    private int RandomizePokespotSpecies(
        BinaryData data,
        RelocationTable table,
        IReadOnlyList<XdSpeciesInfo> speciesPool,
        IReadOnlyDictionary<int, XdSpeciesInfo> speciesById,
        ISet<int> usedObtainableSpecies,
        bool similarBaseStatTotal)
    {
        var changed = 0;
        foreach (var spot in XdPokespotTargets)
        {
            var start = table.GetPointer(spot.PointerIndex);
            var count = table.GetValueAtPointer(spot.CountIndex);
            if (!IsSafeTableRange(data, start, count, XdPokespotEntrySize, maxCount: 64))
            {
                continue;
            }

            for (var index = 0; index < count; index++)
            {
                var rowOffset = start + (index * XdPokespotEntrySize);
                var oldSpecies = data.ReadUInt16(rowOffset + 2);
                if (!speciesById.ContainsKey(oldSpecies))
                {
                    continue;
                }

                var newSpecies = RandomSpeciesFor(oldSpecies, speciesPool, similarBaseStatTotal, usedObtainableSpecies);
                data.WriteUInt16(rowOffset + 2, ClampUInt16ToU16(newSpecies.Index));
                changed++;
            }
        }

        return changed;
    }

    private int RandomizeDeckPokemonSpecies(
        BinaryData data,
        RelocationTable table,
        IReadOnlyList<XdSpeciesInfo> speciesPool,
        IReadOnlyDictionary<int, XdSpeciesInfo> speciesById,
        ISet<int> usedObtainableSpecies,
        XdRandomizerOptions options,
        ICollection<string> writtenFiles)
    {
        if (!options.ObtainablePokemon && !options.UnobtainablePokemon)
        {
            return 0;
        }

        var archive = TryReadFsys("deck_archive.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "deck_archive.fsys was not found.");
        var replacements = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var darkEntry = FindDeckEntry(archive, "DeckData_DarkPokemon");
        byte[]? darkBytes = null;
        var shadowStoryRows = new Dictionary<int, int>();
        if (darkEntry is not null)
        {
            darkBytes = archive.Extract(darkEntry);
            if (darkBytes.Length >= 0x20)
            {
                var entries = checked((int)BigEndian.ReadUInt32(darkBytes, 0x18));
                for (var index = 0; index < entries; index++)
                {
                    var rowOffset = 0x20 + (index * XdShadowPokemonSize);
                    if (rowOffset + XdShadowPokemonSize > darkBytes.Length)
                    {
                        break;
                    }

                    if (IsActiveShadowPokemonRow(darkBytes, rowOffset))
                    {
                        shadowStoryRows[ReadU16(darkBytes, rowOffset + XdShadowStoryIndexOffset)] = rowOffset;
                    }
                }
            }
        }

        var changed = 0;
        foreach (var deckName in XdTrainerDecks.Where(deck => deck is "DeckData_Story" or "DeckData_Colosseum" or "DeckData_Hundred"))
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

            var deckChanged = false;
            for (var index = 0; index < layout.PokemonEntries; index++)
            {
                var rowOffset = layout.PokemonDataOffset + (index * XdDeckPokemonSize);
                if (rowOffset + XdDeckPokemonSize > bytes.Length)
                {
                    break;
                }

                var oldSpecies = ReadU16(bytes, rowOffset + XdDeckPokemonSpeciesOffset);
                if (!speciesById.ContainsKey(oldSpecies))
                {
                    continue;
                }

                var isShadow = deckName == "DeckData_Story" && shadowStoryRows.ContainsKey(index);
                if ((isShadow && !options.ObtainablePokemon) || (!isShadow && !options.UnobtainablePokemon))
                {
                    continue;
                }

                var newSpecies = RandomSpeciesFor(
                    oldSpecies,
                    speciesPool,
                    options.SimilarBaseStatTotal,
                    isShadow ? usedObtainableSpecies : null);
                WriteU16(bytes, rowOffset + XdDeckPokemonSpeciesOffset, newSpecies.Index);
                bytes[rowOffset + 0x03] = 128;
                var level = bytes[rowOffset + XdDeckPokemonLevelOffset];
                var moves = DefaultMovesForLevel(data, table, newSpecies.Index, level);
                for (var move = 0; move < 4; move++)
                {
                    WriteU16(bytes, rowOffset + XdDeckPokemonFirstMoveOffset + (move * 2), ValueAt(moves, move));
                }

                if (isShadow && darkBytes is not null && shadowStoryRows.TryGetValue(index, out var darkRow))
                {
                    darkBytes[darkRow + XdShadowCatchRateOffset] = ClampByte(newSpecies.CatchRate);
                }

                deckChanged = true;
                changed++;
            }

            if (deckChanged)
            {
                replacements[entry.Name] = bytes;
            }
        }

        if (darkEntry is not null && darkBytes is not null && changed > 0)
        {
            replacements[darkEntry.Name] = darkBytes;
        }

        if (replacements.Count > 0)
        {
            writtenFiles.Add(WriteFsysEntries("deck_archive.fsys", replacements));
        }

        return changed;
    }

    private int RandomizePokemonTypes(BinaryData data, RelocationTable table)
    {
        var typeIds = RandomTypeIds(data, table);
        var start = PokemonStatsTableStart(data, table, out var count);
        var changed = 0;
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0)
            {
                continue;
            }

            data.WriteByte(rowOffset + XdPokemonType1Offset, ClampByte(RandomElement(typeIds)));
            data.WriteByte(rowOffset + XdPokemonType2Offset, ClampByte(RandomElement(typeIds)));
            changed++;
        }

        return changed;
    }

    private int RandomizePokemonAbilities(BinaryData data, RelocationTable table)
    {
        var abilityIds = Enumerable.Range(1, Gen3AbilityNames.Length - 1)
            .Where(id => id != WonderGuardAbilityId)
            .ToArray();
        var start = PokemonStatsTableStart(data, table, out var count);
        var changed = 0;
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0
                || data.ReadByte(rowOffset + XdPokemonAbility1Offset) == WonderGuardAbilityId
                || data.ReadByte(rowOffset + XdPokemonAbility2Offset) == WonderGuardAbilityId)
            {
                continue;
            }

            data.WriteByte(rowOffset + XdPokemonAbility1Offset, ClampByte(RandomElement(abilityIds)));
            if (data.ReadByte(rowOffset + XdPokemonAbility2Offset) > 0)
            {
                data.WriteByte(rowOffset + XdPokemonAbility2Offset, ClampByte(RandomElement(abilityIds)));
            }

            changed++;
        }

        return changed;
    }

    private int RandomizePokemonBaseStats(BinaryData data, RelocationTable table)
    {
        var start = PokemonStatsTableStart(data, table, out var count);
        var changed = 0;
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0)
            {
                continue;
            }

            var stats = RandomBaseStatsFor(
                data.ReadByte(rowOffset + XdPokemonHpOffset),
                data.ReadByte(rowOffset + XdPokemonAttackOffset),
                data.ReadByte(rowOffset + XdPokemonDefenseOffset),
                data.ReadByte(rowOffset + XdPokemonSpecialAttackOffset),
                data.ReadByte(rowOffset + XdPokemonSpecialDefenseOffset),
                data.ReadByte(rowOffset + XdPokemonSpeedOffset));

            data.WriteByte(rowOffset + XdPokemonHpOffset, ClampByte(stats.Hp));
            data.WriteByte(rowOffset + XdPokemonAttackOffset, ClampByte(stats.Attack));
            data.WriteByte(rowOffset + XdPokemonDefenseOffset, ClampByte(stats.Defense));
            data.WriteByte(rowOffset + XdPokemonSpecialAttackOffset, ClampByte(stats.SpecialAttack));
            data.WriteByte(rowOffset + XdPokemonSpecialDefenseOffset, ClampByte(stats.SpecialDefense));
            data.WriteByte(rowOffset + XdPokemonSpeedOffset, ClampByte(stats.Speed));
            changed++;
        }

        return changed;
    }

    private int RandomizePokemonEvolutions(BinaryData data, RelocationTable table)
    {
        var speciesPool = EligibleSpeciesIds(data, table);
        if (speciesPool.Count == 0)
        {
            return 0;
        }

        var start = PokemonStatsTableStart(data, table, out var count);
        var changed = 0;
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0)
            {
                continue;
            }

            for (var evolution = 0; evolution < XdPokemonEvolutionCount; evolution++)
            {
                var evolutionOffset = rowOffset + XdPokemonFirstEvolutionOffset + (evolution * 6);
                if (data.ReadUInt16(evolutionOffset + 4) == 0)
                {
                    continue;
                }

                data.WriteUInt16(evolutionOffset + 4, ClampUInt16ToU16(RandomElement(speciesPool)));
                changed++;
            }
        }

        return changed;
    }

    private int RandomizeMoveTypes(BinaryData data, RelocationTable table)
    {
        var typeIds = RandomTypeIds(data, table);
        var movesStart = MovesTableStart(data, table, out var moveCount);
        var changed = 0;
        for (var move = 1; move < moveCount; move++)
        {
            var rowOffset = movesStart + (move * XdMoveSize);
            if (data.ReadUInt32(rowOffset + XdMoveNameIdOffset) == 0)
            {
                continue;
            }

            data.WriteByte(rowOffset + XdMoveTypeOffset, ClampByte(RandomElement(typeIds)));
            changed++;
        }

        return changed;
    }

    private int RandomizeTypeMatchups(BinaryData data, RelocationTable table)
    {
        var start = TypeTableStart(data, table, out var count);
        var changed = 0;
        for (var type = 0; type < count; type++)
        {
            var rowOffset = start + (type * XdTypeSize);
            var values = Enumerable.Range(0, count)
                .Select(index => (int)data.ReadByte(rowOffset + XdTypeFirstEffectivenessOffset + index))
                .ToArray();
            Shuffle(values);
            for (var index = 0; index < values.Length; index++)
            {
                data.WriteByte(rowOffset + XdTypeFirstEffectivenessOffset + index, ClampByte(values[index]));
            }

            changed++;
        }

        return changed;
    }

    private int RandomizeTreasureBoxes(BinaryData data, RelocationTable table)
    {
        var itemPool = EligibleItemIds(data, table);
        if (itemPool.Count == 0)
        {
            return 0;
        }

        var start = table.GetPointer(XdTreasureIndex);
        var count = table.GetValueAtPointer(XdNumberOfTreasuresIndex);
        if (!IsSafeTableRange(data, start, count, XdTreasureSize, maxCount: 1000))
        {
            throw new InvalidDataException("Treasure table is outside common.rel.");
        }

        var changed = 0;
        for (var treasure = 0; treasure < count; treasure++)
        {
            var rowOffset = start + (treasure * XdTreasureSize);
            var currentItem = data.ReadUInt16(rowOffset + XdTreasureItemOffset);
            if (!itemPool.Contains(currentItem))
            {
                continue;
            }

            data.WriteByte(rowOffset + XdTreasureQuantityOffset, ClampByte(Random.Shared.Next(1, 4)));
            data.WriteUInt16(rowOffset + XdTreasureItemOffset, ClampUInt16ToU16(RandomElement(itemPool)));
            changed++;
        }

        return changed;
    }

    private XdShopRandomizerResult RandomizePocketMenuShops(BinaryData commonData, RelocationTable commonTable)
    {
        var itemPool = EligibleShopItemIds(commonData, commonTable);
        if (itemPool.Count == 0)
        {
            return new XdShopRandomizerResult(string.Empty, 0);
        }

        var archive = TryReadFsys("pocket_menu.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "pocket_menu.fsys was not found.");
        var relEntry = archive.Entries.FirstOrDefault(entry => entry.Name.Equals("pocket_menu.rel", StringComparison.OrdinalIgnoreCase))
            ?? archive.Entries.FirstOrDefault(entry => entry.Name.EndsWith(".rel", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("pocket_menu.rel was not found inside pocket_menu.fsys.");
        var rel = new BinaryData(archive.Extract(relEntry));
        var pocketTable = RelocationTable.Parse(rel.ToArray());
        var itemOffset = pocketTable.GetPointer(XdPocketMartItemsIndex);
        var itemCount = pocketTable.GetValueAtPointer(XdPocketNumberOfMartItemsIndex);
        if (!IsSafeTableRange(rel, itemOffset, itemCount, 2, maxCount: 5000))
        {
            throw new InvalidDataException("XD shop item table is outside pocket_menu.rel.");
        }

        var itemPoolSet = itemPool.ToHashSet();
        var changed = 0;
        for (var index = 0; index < itemCount; index++)
        {
            var offset = itemOffset + (index * 2);
            var itemId = rel.ReadUInt16(offset);
            if (itemId == 0 || !itemPoolSet.Contains(itemId))
            {
                continue;
            }

            rel.WriteUInt16(offset, ClampUInt16ToU16(RandomElement(itemPool)));
            changed++;
        }

        UpdateCouponPrices(commonData, commonTable);
        var path = changed > 0
            ? WriteFsysEntries("pocket_menu.fsys", new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
            {
                [relEntry.Name] = rel.ToArray()
            })
            : string.Empty;
        return new XdShopRandomizerResult(path, changed);
    }

    private int RewriteEvolutionMethods(BinaryData data, RelocationTable table, Func<int, bool> matchesMethod)
    {
        var start = PokemonStatsTableStart(data, table, out var count);
        var changed = 0;
        for (var pokemon = 0; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            for (var evolution = 0; evolution < XdPokemonEvolutionCount; evolution++)
            {
                var evolutionOffset = rowOffset + XdPokemonFirstEvolutionOffset + (evolution * 6);
                if (!matchesMethod(data.ReadByte(evolutionOffset)))
                {
                    continue;
                }

                data.WriteByte(evolutionOffset, EvolutionMethodLevelUp);
                data.WriteUInt16(evolutionOffset + 2, EvolutionPatchLevel);
                changed++;
            }
        }

        return changed;
    }

    private string RandomizeTmMoves(BinaryData commonData, RelocationTable commonTable, ICollection<string> messages)
    {
        var movePool = EligibleMoveIds(commonData, commonTable, includeShadow: false);
        if (movePool.Count == 0)
        {
            messages.Add("No eligible XD moves were found for TM randomization.");
            return string.Empty;
        }

        var dolEntry = FindIsoFile("Start.dol")
            ?? throw new FileNotFoundException("Start.dol was not found in the ISO.");
        var dol = new BinaryData(GameCubeIsoReader.ReadFile(Iso, dolEntry));
        var start = FirstXdTmListOffset(Iso.Region);
        if (start <= 0 || start + XdTmMoveOffset >= dol.Length)
        {
            throw new InvalidDataException("XD TM table is outside Start.dol.");
        }

        var usedMoveIds = new HashSet<int>();
        var changed = 0;
        for (var index = 1; index <= XdPokemonTmCount; index++)
        {
            var offset = start + XdTmMoveOffset + ((index - 1) * XdTmEntrySize);
            if (offset + 2 > dol.Length)
            {
                break;
            }

            dol.WriteUInt16(offset, ClampUInt16ToU16(RandomMoveId(movePool, usedMoveIds)));
            changed++;
        }

        messages.Add($"Randomized {changed:N0} XD TM/HM move entries.");
        return WriteStartDol(dol, dolEntry);
    }

    private string WriteStartDol(BinaryData dol, GameCubeIsoFileEntry dolEntry)
    {
        var bytes = dol.ToArray();
        WriteIsoEntry(dolEntry, bytes);
        var workspacePath = Path.Combine(WorkspaceDirectory, "Game Files", "Start.dol");
        Directory.CreateDirectory(Path.GetDirectoryName(workspacePath)!);
        File.WriteAllBytes(workspacePath, bytes);
        return workspacePath;
    }

    private int PokemonStatsTableStart(BinaryData data, RelocationTable table, out int count)
    {
        var start = table.GetPointer(XdPokemonStatsIndex);
        count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            throw new InvalidDataException("Pokemon stats table is outside common.rel.");
        }

        return start;
    }

    private int MovesTableStart(BinaryData data, RelocationTable table, out int count)
    {
        var start = table.GetPointer(XdMovesIndex);
        count = table.GetValueAtPointer(XdNumberOfMovesIndex);
        if (!IsSafeTableRange(data, start, count, XdMoveSize, maxCount: 1000))
        {
            throw new InvalidDataException("Move table is outside common.rel.");
        }

        return start;
    }

    private int TypeTableStart(BinaryData data, RelocationTable table, out int count)
    {
        var start = table.GetPointer(XdTypeIndex);
        count = table.GetValueAtPointer(XdNumberOfTypesIndex);
        if (!IsSafeTableRange(data, start, count, XdTypeSize, maxCount: 64))
        {
            throw new InvalidDataException("Type table is outside common.rel.");
        }

        return start;
    }

    private IReadOnlyList<int> EligibleSpeciesIds(BinaryData data, RelocationTable table)
    {
        var start = PokemonStatsTableStart(data, table, out var count);
        var species = new List<int>();
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) > 0
                && data.ReadByte(rowOffset + XdPokemonCatchRateOffset) > 0)
            {
                species.Add(pokemon);
            }
        }

        return species;
    }

    private IReadOnlyList<XdSpeciesInfo> EligibleSpecies(BinaryData data, RelocationTable table)
    {
        var start = PokemonStatsTableStart(data, table, out var count);
        var species = new List<XdSpeciesInfo>();
        for (var pokemon = 1; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            var catchRate = data.ReadByte(rowOffset + XdPokemonCatchRateOffset);
            if (data.ReadUInt32(rowOffset + XdPokemonStatsNameIdOffset) == 0 || catchRate == 0)
            {
                continue;
            }

            var baseStatTotal =
                data.ReadByte(rowOffset + XdPokemonHpOffset)
                + data.ReadByte(rowOffset + XdPokemonAttackOffset)
                + data.ReadByte(rowOffset + XdPokemonDefenseOffset)
                + data.ReadByte(rowOffset + XdPokemonSpecialAttackOffset)
                + data.ReadByte(rowOffset + XdPokemonSpecialDefenseOffset)
                + data.ReadByte(rowOffset + XdPokemonSpeedOffset);
            species.Add(new XdSpeciesInfo(pokemon, catchRate, baseStatTotal));
        }

        return species;
    }

    private IReadOnlyList<int> EligibleMoveIds(BinaryData data, RelocationTable table, bool includeShadow)
    {
        var start = MovesTableStart(data, table, out var count);
        var moves = new List<int>();
        for (var move = 1; move < count; move++)
        {
            var rowOffset = start + (move * XdMoveSize);
            if (data.ReadUInt32(rowOffset + XdMoveNameIdOffset) == 0
                || data.ReadUInt32(rowOffset + XdMoveDescriptionIdOffset) == 0)
            {
                continue;
            }

            if (!includeShadow && move >= 355)
            {
                continue;
            }

            moves.Add(move);
        }

        return moves;
    }

    private IReadOnlyList<int> EligibleItemIds(BinaryData data, RelocationTable table)
    {
        var start = table.GetPointer(XdItemsIndex);
        var count = table.GetValueAtPointer(XdNumberOfItemsIndex);
        if (!IsSafeTableRange(data, start, count, XdItemSize, maxCount: 2000))
        {
            throw new InvalidDataException("Item table is outside common.rel.");
        }

        var items = new List<int>();
        for (var item = 1; item < count; item++)
        {
            var rowOffset = start + (item * XdItemSize);
            if (data.ReadUInt32(rowOffset + XdItemNameIdOffset) > 0
                && data.ReadUInt32(rowOffset + XdItemDescriptionIdOffset) > 0
                && data.ReadByte(rowOffset + XdItemBagSlotOffset) < 5
                && data.ReadUInt16(rowOffset + XdItemPriceOffset) > 0)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private IReadOnlyList<int> EligibleShopItemIds(BinaryData data, RelocationTable table)
    {
        var start = table.GetPointer(XdItemsIndex);
        var count = table.GetValueAtPointer(XdNumberOfItemsIndex);
        if (!IsSafeTableRange(data, start, count, XdItemSize, maxCount: 2000))
        {
            throw new InvalidDataException("Item table is outside common.rel.");
        }

        var items = new List<int>();
        for (var item = 1; item < count; item++)
        {
            var rowOffset = start + (item * XdItemSize);
            var bagSlot = data.ReadByte(rowOffset + XdItemBagSlotOffset);
            if (bagSlot is >= 5 and <= 7)
            {
                continue;
            }

            if (data.ReadUInt16(rowOffset + XdItemPriceOffset) > 0)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static void UpdateCouponPrices(BinaryData data, RelocationTable table)
    {
        var start = table.GetPointer(XdItemsIndex);
        var count = table.GetValueAtPointer(XdNumberOfItemsIndex);
        if (!IsSafeTableRange(data, start, count, XdItemSize, maxCount: 2000))
        {
            throw new InvalidDataException("Item table is outside common.rel.");
        }

        for (var item = 1; item < count; item++)
        {
            var rowOffset = start + (item * XdItemSize);
            var price = data.ReadUInt16(rowOffset + XdItemPriceOffset);
            if (price > 0)
            {
                data.WriteUInt16(rowOffset + XdItemCouponOffset, ClampUInt16ToU16(checked(price * 10)));
            }
        }
    }

    private IReadOnlyList<int> RandomTypeIds(BinaryData data, RelocationTable table)
    {
        TypeTableStart(data, table, out var count);
        return Enumerable.Range(0, count)
            .Where(index => index != 9)
            .ToArray();
    }

    private static int RandomMoveId(IReadOnlyList<int> movePool, HashSet<int>? usedMoveIds = null)
    {
        var options = usedMoveIds is null || usedMoveIds.Count >= movePool.Count
            ? movePool
            : movePool.Where(move => !usedMoveIds.Contains(move)).ToArray();
        var selected = RandomElement(options.Count > 0 ? options : movePool);
        usedMoveIds?.Add(selected);
        return selected;
    }

    private static XdSpeciesInfo RandomSpeciesFor(
        int oldSpeciesId,
        IReadOnlyList<XdSpeciesInfo> speciesPool,
        bool similarBaseStatTotal,
        ISet<int>? usedSpecies)
    {
        var oldSpecies = speciesPool.FirstOrDefault(species => species.Index == oldSpeciesId);
        var options = speciesPool;
        if (usedSpecies is not null && usedSpecies.Count >= speciesPool.Count)
        {
            usedSpecies.Clear();
        }

        if (usedSpecies is not null && options.Any(species => !usedSpecies.Contains(species.Index)))
        {
            options = options.Where(species => !usedSpecies.Contains(species.Index)).ToArray();
        }

        if (similarBaseStatTotal && oldSpecies is not null)
        {
            var radius = 50;
            while (radius <= 600)
            {
                var filtered = options
                    .Where(species => species.BaseStatTotal >= oldSpecies.BaseStatTotal - radius)
                    .Where(species => species.BaseStatTotal <= oldSpecies.BaseStatTotal + radius)
                    .ToArray();
                if (filtered.Length > 1)
                {
                    options = filtered;
                    break;
                }

                radius += 20;
            }
        }

        var selected = options.Count == 0 ? oldSpecies ?? RandomElement(speciesPool) : RandomElement(options);
        usedSpecies?.Add(selected.Index);
        return selected;
    }

    private static int GiftLevel(BinaryData dol, XdGiftLayout layout)
    {
        if (layout.LevelOffset >= 0)
        {
            var offset = layout.StartOffset + layout.LevelOffset;
            return offset >= 0 && offset < dol.Length ? dol.ReadByte(offset) : 0;
        }

        return layout.SharedLevelOffset >= 0 && layout.SharedLevelOffset < dol.Length
            ? dol.ReadByte(layout.SharedLevelOffset)
            : 0;
    }

    private static IReadOnlyList<int> DefaultMovesForLevel(BinaryData data, RelocationTable table, int species, int level)
    {
        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000)
            || species < 0
            || species >= count)
        {
            return [0, 0, 0, 0];
        }

        var rowOffset = start + (species * XdPokemonStatsSize);
        var moves = Enumerable.Range(0, XdPokemonLevelUpMoveCount)
            .Select(row =>
            {
                var moveOffset = rowOffset + XdPokemonFirstLevelUpMoveOffset + (row * 4);
                return (Level: (int)data.ReadByte(moveOffset), Move: (int)data.ReadUInt16(moveOffset + 2));
            })
            .Where(move => move.Move > 0 && move.Level <= level)
            .Select(move => move.Move)
            .Distinct()
            .TakeLast(4)
            .ToList();
        while (moves.Count < 4)
        {
            moves.Insert(0, 0);
        }

        return moves;
    }

    private static RandomizedStats RandomBaseStatsFor(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
    {
        var remaining = hp + attack + defense + specialAttack + specialDefense + speed;
        var maxStat = Math.Min(Math.Max(1, remaining / 4), byte.MaxValue);
        var minimum = remaining <= 240 ? MinimumRandomStatSmallTotal : MinimumRandomStatLargeTotal;
        var stats = new[] { minimum, minimum, minimum, minimum, minimum, minimum };
        remaining -= minimum * stats.Length;

        void AddToRandomStat(int value)
        {
            if (remaining <= 0)
            {
                return;
            }

            value = Math.Min(value, remaining);
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var index = Random.Shared.Next(stats.Length);
                if (stats[index] + value <= maxStat)
                {
                    stats[index] += value;
                    remaining -= value;
                    return;
                }
            }

            var fallback = Array.FindIndex(stats, stat => stat < byte.MaxValue);
            if (fallback < 0)
            {
                remaining = 0;
                return;
            }

            var added = Math.Min(value, byte.MaxValue - stats[fallback]);
            stats[fallback] += added;
            remaining -= added;
        }

        while (remaining > 150)
        {
            AddToRandomStat(Math.Max(1, remaining / 6));
        }

        while (remaining > 25)
        {
            AddToRandomStat(10);
        }

        while (remaining > 0)
        {
            AddToRandomStat(1);
        }

        return new RandomizedStats(
            Hp: stats[5],
            Attack: stats[0],
            Defense: stats[1],
            SpecialAttack: stats[2],
            SpecialDefense: stats[3],
            Speed: stats[4]);
    }

    private static void Shuffle(int[] values)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static T RandomElement<T>(IReadOnlyList<T> values)
        => values.Count == 0 ? throw new InvalidOperationException("Cannot choose a random value from an empty list.") : values[Random.Shared.Next(values.Count)];

    private sealed record RandomizedStats(
        int Hp,
        int Attack,
        int Defense,
        int SpecialAttack,
        int SpecialDefense,
        int Speed);

    private sealed record XdSpeciesInfo(int Index, int CatchRate, int BaseStatTotal);

    private sealed record XdRandomizedSpeciesCounts(int Gifts, int Pokespots, int TrainerPokemon);

    private sealed record XdShopRandomizerResult(string PocketMenuRelPath, int ChangedItems);
}
