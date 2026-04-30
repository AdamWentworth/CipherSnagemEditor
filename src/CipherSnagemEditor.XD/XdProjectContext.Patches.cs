using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    private const int EvolutionMethodLevelUp = 0x04;
    private const int EvolutionMethodTrade = 0x05;
    private const int EvolutionMethodTradeWithItem = 0x06;
    private const int EvolutionMethodStone = 0x07;
    private const int EvolutionPatchLevel = 40;
    private const int BattleStyleSingle = 0x01;
    private const int BattleStyleDouble = 0x02;
    private const int DeckVirtualId = 0x04;
    private const int DeckBingoId = 0x05;

    public XdPatchApplyResult ApplyPatch(XdPatchKind kind)
    {
        var definition = XdPatchDefinition.ForKind(kind);
        var writtenFiles = new List<string>();
        var messages = new List<string>();

        switch (kind)
        {
            case XdPatchKind.TradeEvolutions:
                PatchTradeOrItemEvolutions(
                    writtenFiles,
                    messages,
                    method => method is EvolutionMethodTrade or EvolutionMethodTradeWithItem,
                    "trade");
                break;

            case XdPatchKind.RemoveItemEvolutions:
                PatchTradeOrItemEvolutions(
                    writtenFiles,
                    messages,
                    method => method == EvolutionMethodStone,
                    "evolution stone");
                break;

            case XdPatchKind.PokemonCanLearnAnyTm:
                PatchPokemonCanLearnAnyTm(writtenFiles, messages);
                break;

            case XdPatchKind.PokemonHaveMaxCatchRate:
                PatchPokemonHaveMaxCatchRate(writtenFiles, messages);
                break;

            case XdPatchKind.AllSingleBattles:
                PatchAllBattlesTo(BattleStyleSingle, "single", writtenFiles, messages);
                break;

            case XdPatchKind.AllDoubleBattles:
                PatchAllBattlesTo(BattleStyleDouble, "double", writtenFiles, messages);
                break;

            default:
                throw new NotSupportedException($"{definition.Name} still needs Start.dol assembly/script parity from the Swift GoD Tool.");
        }

        if (writtenFiles.Count == 0)
        {
            messages.Add("Patch was already applied; no files needed to be rewritten.");
        }

        return new XdPatchApplyResult(definition, writtenFiles, messages);
    }

    private void PatchPokemonCanLearnAnyTm(ICollection<string> writtenFiles, ICollection<string> messages)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            throw new InvalidDataException("Pokemon stats table is outside common.rel.");
        }

        var changedFlags = 0;
        for (var pokemon = 0; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            for (var tm = 0; tm < XdPokemonTmCount; tm++)
            {
                var flagOffset = rowOffset + XdPokemonFirstTmOffset + tm;
                if (data.ReadByte(flagOffset) == 1)
                {
                    continue;
                }

                data.WriteByte(flagOffset, 1);
                changedFlags++;
            }
        }

        if (changedFlags > 0)
        {
            writtenFiles.Add(WriteCommonRel(data));
        }

        messages.Add($"Enabled every TM flag for {count:N0} Pokemon ({changedFlags:N0} changed flags).");
    }

    private void PatchPokemonHaveMaxCatchRate(ICollection<string> writtenFiles, ICollection<string> messages)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var statsStart = table.GetPointer(XdPokemonStatsIndex);
        var pokemonCount = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, statsStart, pokemonCount, XdPokemonStatsSize, maxCount: 1000))
        {
            throw new InvalidDataException("Pokemon stats table is outside common.rel.");
        }

        var statChanges = 0;
        for (var pokemon = 0; pokemon < pokemonCount; pokemon++)
        {
            var catchRateOffset = statsStart + (pokemon * XdPokemonStatsSize) + XdPokemonCatchRateOffset;
            var catchRate = data.ReadByte(catchRateOffset);
            if (catchRate == 0 || catchRate == byte.MaxValue)
            {
                continue;
            }

            data.WriteByte(catchRateOffset, byte.MaxValue);
            statChanges++;
        }

        if (statChanges > 0)
        {
            writtenFiles.Add(WriteCommonRel(data));
        }

        var archive = TryReadFsys("deck_archive.fsys", out var error)
            ?? throw new InvalidDataException(error ?? "deck_archive.fsys was not found.");
        var darkEntry = FindDeckEntry(archive, "DeckData_DarkPokemon")
            ?? throw new InvalidDataException("DeckData_DarkPokemon was not found in deck_archive.fsys.");
        var darkBytes = archive.Extract(darkEntry);
        var shadowChanges = 0;
        var activeShadows = 0;

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

                if (!IsActiveShadowPokemonRow(darkBytes, rowOffset))
                {
                    continue;
                }

                activeShadows++;
                if (darkBytes[rowOffset + XdShadowCatchRateOffset] == byte.MaxValue)
                {
                    continue;
                }

                darkBytes[rowOffset + XdShadowCatchRateOffset] = byte.MaxValue;
                shadowChanges++;
            }
        }

        if (shadowChanges > 0)
        {
            writtenFiles.Add(WriteFsysEntries("deck_archive.fsys", new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
            {
                [darkEntry.Name] = darkBytes
            }));
        }

        messages.Add($"Set catch rate 255 for {statChanges:N0} base Pokemon rows and {shadowChanges:N0}/{activeShadows:N0} active shadow rows.");
    }

    private void PatchTradeOrItemEvolutions(
        ICollection<string> writtenFiles,
        ICollection<string> messages,
        Func<int, bool> matchesMethod,
        string label)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var start = table.GetPointer(XdPokemonStatsIndex);
        var count = table.GetValueAtPointer(XdNumberOfPokemonIndex);
        if (!IsSafeTableRange(data, start, count, XdPokemonStatsSize, maxCount: 1000))
        {
            throw new InvalidDataException("Pokemon stats table is outside common.rel.");
        }

        var changedEvolutions = 0;
        for (var pokemon = 0; pokemon < count; pokemon++)
        {
            var rowOffset = start + (pokemon * XdPokemonStatsSize);
            for (var evolution = 0; evolution < XdPokemonEvolutionCount; evolution++)
            {
                var evolutionOffset = rowOffset + XdPokemonFirstEvolutionOffset + (evolution * 6);
                var method = data.ReadByte(evolutionOffset);
                if (!matchesMethod(method))
                {
                    continue;
                }

                data.WriteByte(evolutionOffset, EvolutionMethodLevelUp);
                data.WriteUInt16(evolutionOffset + 2, EvolutionPatchLevel);
                changedEvolutions++;
            }
        }

        if (changedEvolutions > 0)
        {
            writtenFiles.Add(WriteCommonRel(data));
        }

        messages.Add($"Converted {changedEvolutions:N0} {label} evolution(s) to level {EvolutionPatchLevel}.");
    }

    private void PatchAllBattlesTo(int battleStyle, string label, ICollection<string> writtenFiles, ICollection<string> messages)
    {
        var (data, table, _) = ReadCommonRelOrThrow();
        var start = table.GetPointer(XdBattlesIndex);
        var count = table.GetValueAtPointer(XdNumberOfBattlesIndex);
        var battleSize = XdBattleSize(Iso.Region);
        if (!IsSafeTableRange(data, start, count, battleSize, maxCount: 5000))
        {
            throw new InvalidDataException("Battle table is outside common.rel.");
        }

        var changedBattles = 0;
        var skippedBingoOrVirtual = 0;
        for (var battle = 0; battle < count; battle++)
        {
            var offset = start + (battle * battleSize);
            var playerTwoDeck = ReadU16(data, offset + XdBattlePlayerDeckOffset(Iso.Region, player: 1));
            if (playerTwoDeck is DeckVirtualId or DeckBingoId)
            {
                skippedBingoOrVirtual++;
                continue;
            }

            if (data.ReadByte(offset + XdBattleStyleOffset) == battleStyle)
            {
                continue;
            }

            data.WriteByte(offset + XdBattleStyleOffset, ClampByte(battleStyle));
            changedBattles++;
        }

        if (changedBattles > 0)
        {
            writtenFiles.Add(WriteCommonRel(data));
        }

        messages.Add($"Set {changedBattles:N0} battle(s) to {label}; skipped {skippedBingoOrVirtual:N0} Battle Bingo/Virtual row(s).");
    }

    private static bool IsActiveShadowPokemonRow(byte[] bytes, int rowOffset)
    {
        var storyIndex = ReadU16(bytes, rowOffset + XdShadowStoryIndexOffset);
        var level = bytes[rowOffset + XdShadowLevelOffset];
        var catchRate = bytes[rowOffset + XdShadowCatchRateOffset];
        var heartGauge = ReadU16(bytes, rowOffset + XdShadowHeartGaugeOffset);
        return storyIndex != 0 || level != 0 || catchRate != 0 || heartGauge != 0;
    }
}
