using System.Text.Json;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.Colosseum.Data;

public sealed partial class ColosseumCommonRel
{
    private const uint PatchMarker = 0x0DE1E7ED;
    private const uint NopInstruction = 0x60000000;
    private const int ShinyNever = 0x0000;
    private const int ShinyAlways = 0x0001;
    private const int ShinyRandom = 0xffff;
    private const int GenderRandom = 0xff;

    public ColosseumPatchChangeSet ApplyPatch(ColosseumPatchKind patch)
        => patch switch
        {
            ColosseumPatchKind.PhysicalSpecialSplitApply => ApplyPhysicalSpecialSplitPatch(),
            ColosseumPatchKind.DisableSaveCorruption => PreventSaveFileCorruption(),
            ColosseumPatchKind.AddSoftReset => AddSoftReset(),
            ColosseumPatchKind.LoadPcFromAnywhere => LoadPcFromAnywhere(),
            ColosseumPatchKind.RemoveShinyLocksFromGiftPokemon => RemoveShinyLocksFromGiftPokemon(),
            ColosseumPatchKind.AllowFemaleStarters => AllowFemaleStarters(),
            ColosseumPatchKind.InfiniteTms => InfiniteUseTms(),
            ColosseumPatchKind.Gen6CritMultipliers => Gen6CritMultipliers(),
            ColosseumPatchKind.Gen7CritRatios => Gen7CritRatios(),
            ColosseumPatchKind.TradeEvolutions => RemoveTradeEvolutions(),
            ColosseumPatchKind.RemoveItemEvolutions => RemoveItemEvolutions(),
            ColosseumPatchKind.AllowShinyStarters => SetStarterShininess(ShinyRandom),
            ColosseumPatchKind.ShinyLockStarters => SetStarterShininess(ShinyNever),
            ColosseumPatchKind.AlwaysShinyStarters => SetStarterShininess(ShinyAlways),
            ColosseumPatchKind.EnableDebugLogs => EnableDebugLogs(),
            ColosseumPatchKind.NoTypeIconForLockedMoves => NoTypeIconForLockedMoves(),
            ColosseumPatchKind.PokemonCanLearnAnyTm => PokemonCanLearnAnyTm(),
            ColosseumPatchKind.PokemonHaveMaxCatchRate => PokemonHaveMaxCatchRate(),
            ColosseumPatchKind.AllSingleBattles => SetAllBattlesTo(1),
            ColosseumPatchKind.AllDoubleBattles => SetAllBattlesTo(2),
            ColosseumPatchKind.RemoveColbtlRegionLock => UnlockColbtlBin(),
            _ => throw new NotSupportedException($"Patch {patch} is not available.")
        };

    private ColosseumPatchChangeSet ApplyPhysicalSpecialSplitPatch()
    {
        var typeGetCategory = _region switch
        {
            GameCubeRegion.UnitedStates => 0x10c4a0,
            GameCubeRegion.Europe => 0x10fab8,
            GameCubeRegion.Japan => 0x109e48,
            _ => throw UnsupportedRegion()
        };
        var move17Offsets = _region switch
        {
            GameCubeRegion.UnitedStates => new[] { 0x232938, 0x2324c8 },
            GameCubeRegion.Europe => [0x236fc8, 0x237438],
            GameCubeRegion.Japan => [0x22e0d0, 0x22dc60],
            _ => []
        };
        var move23Offsets = _region switch
        {
            GameCubeRegion.UnitedStates => new[] { 0x24462c, 0x2447b8 },
            GameCubeRegion.Europe => [0x249154, 0x2492e0],
            GameCubeRegion.Japan => [0x23fdc4, 0x23ff50],
            _ => []
        };
        var move30Offsets = _region switch
        {
            GameCubeRegion.UnitedStates => new[] { 0x226c18 },
            GameCubeRegion.Europe => [0x22b718],
            GameCubeRegion.Japan => [0x2223b0],
            _ => []
        };
        var move31Offsets = _region switch
        {
            GameCubeRegion.UnitedStates => new[] { 0x228b5c },
            GameCubeRegion.Europe => [0x22d65c],
            GameCubeRegion.Japan => [0x2242f4],
            _ => []
        };

        WriteRamAsm(
            typeGetCategory,
            Mulli(0, 3, 56),
            Lwz(3, 13, -0x7a24),
            Add(3, 3, 0),
            Lbz(3, 3, 0x1f),
            Blr());
        foreach (var offset in move17Offsets)
        {
            WriteDolUInt32(offset - DolTableToRamOffsetDifference, 0x7e238b78);
        }

        foreach (var offset in move23Offsets)
        {
            WriteDolUInt32(offset - DolTableToRamOffsetDifference, 0x7ee3bb78);
        }

        foreach (var offset in move30Offsets)
        {
            WriteDolUInt32(offset - DolTableToRamOffsetDifference, 0x7fc3f378);
        }

        foreach (var offset in move31Offsets)
        {
            WriteDolUInt32(offset - DolTableToRamOffsetDifference, 0x7fe3fb78);
        }

        ApplyDefaultMoveCategories();
        return ColosseumPatchChangeSet.Empty
            .WithStartDol()
            .WithCommonRel()
            .WithMessage("Applied physical/special split and default move categories.");
    }

    private ColosseumPatchChangeSet PreventSaveFileCorruption()
    {
        var saveCountRamOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x1cfc2c,
            GameCubeRegion.Europe => 0x1d429c,
            GameCubeRegion.Japan => 0x1cb5b8,
            _ => throw UnsupportedRegion()
        };
        var memoryCardMatchOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x1cfc7c,
            GameCubeRegion.Europe => 0x1d42ec,
            GameCubeRegion.Japan => 0x1cb608,
            _ => throw UnsupportedRegion()
        };

        WriteRamAsm(saveCountRamOffset, Stw(0, 5, 0x2c));
        WriteRamAsm(memoryCardMatchOffset, NopInstruction);
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Disabled save-corruption checks.");
    }

    private ColosseumPatchChangeSet AddSoftReset()
    {
        var resetBranchOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x5c50,
            GameCubeRegion.Europe => 0x5d48,
            GameCubeRegion.Japan => 0x5bcc,
            _ => throw UnsupportedRegion()
        };
        var resetCheckOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x5c6c,
            GameCubeRegion.Europe => 0x5d64,
            GameCubeRegion.Japan => 0x5be8,
            _ => throw UnsupportedRegion()
        };
        var gsGetInput = _region switch
        {
            GameCubeRegion.UnitedStates => 0xf7bc4,
            GameCubeRegion.Europe => 0xfb244,
            GameCubeRegion.Japan => 0xf588c,
            _ => throw UnsupportedRegion()
        };

        WriteRamAsm(resetBranchOffset, BranchForward(0, 0x1c));
        var checkStart = NormalizeRamOffset(resetCheckOffset);
        WriteRamAsm(
            resetCheckOffset,
            Li(3, 1),
            BranchLink(checkStart + 4, gsGetInput),
            Andi(0, 3, 0x1600),
            Cmpwi(0, 0x1600),
            NopInstruction);
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Added B + X + Start soft reset.");
    }

    private ColosseumPatchChangeSet LoadPcFromAnywhere()
    {
        var freeSpace = AllocateDolFreeSpace(64);
        var processEventsInjectionOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x12eda8,
            GameCubeRegion.Europe => 0x132fd4,
            GameCubeRegion.Japan => 0x12c4e4,
            _ => throw UnsupportedRegion()
        };
        var runScriptAddress = _region switch
        {
            GameCubeRegion.UnitedStates => 0x12bad0,
            GameCubeRegion.Europe => 0x12fcfc,
            GameCubeRegion.Japan => 0x12920c,
            _ => throw UnsupportedRegion()
        };
        var gsGetInput = _region switch
        {
            GameCubeRegion.UnitedStates => 0xf7bc4,
            GameCubeRegion.Europe => 0xfb244,
            GameCubeRegion.Japan => 0xf588c,
            _ => throw UnsupportedRegion()
        };
        var getCurrentRoomId = _region switch
        {
            GameCubeRegion.UnitedStates => 0xff56c,
            GameCubeRegion.Europe => 0x102a80,
            GameCubeRegion.Japan => 0xfcf5c,
            _ => throw UnsupportedRegion()
        };
        var lastWarpPointAddress = _region switch
        {
            GameCubeRegion.UnitedStates => 0x80408378u,
            GameCubeRegion.Europe => 0x804557f8u,
            GameCubeRegion.Japan => 0x803f4a38u,
            _ => throw UnsupportedRegion()
        };

        const int rButtonMask = 0x20;
        var processStart = NormalizeRamOffset(processEventsInjectionOffset);
        var freeStart = NormalizeRamOffset(freeSpace);
        WriteRamAsm(
            processEventsInjectionOffset,
            BranchLink(processStart, gsGetInput),
            Mr(31, 3),
            Cmpwi(3, rButtonMask),
            Bne(processStart + 12, processStart + 20),
            Branch(processStart + 16, freeSpace),
            Rlwinm(0, 31, 0, 19, 21, true));

        var loadLastWarpPoint = LoadImmediateShifted32Bit(5, lastWarpPointAddress);
        WriteRamAsm(
            freeSpace,
            BranchLink(freeStart, getCurrentRoomId),
            Mr(4, 3),
            loadLastWarpPoint.High,
            loadLastWarpPoint.Low,
            Lwz(5, 5, 0x0c),
            Li(6, 0),
            Li(7, 0),
            Lis(3, 0x596),
            Addi(3, 3, 13),
            BranchLink(freeStart + 36, runScriptAddress),
            Branch(freeStart + 40, NormalizeRamOffset(processEventsInjectionOffset) + 0x14));

        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Added R-button PC shortcut.");
    }

    private ColosseumPatchChangeSet RemoveShinyLocksFromGiftPokemon()
    {
        foreach (var gift in GiftPokemon)
        {
            WriteGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                gift.SpeciesId,
                gift.Level,
                gift.MoveIds,
                ShinyRandom,
                gift.Gender,
                gift.Nature));
        }

        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Removed gift Pokemon shiny locks.");
    }

    private ColosseumPatchChangeSet AllowFemaleStarters()
    {
        foreach (var gift in GiftPokemon.Where(gift => gift.RowId is 1 or 2))
        {
            WriteGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                gift.SpeciesId,
                gift.Level,
                gift.MoveIds,
                gift.ShinyValue,
                GenderRandom,
                gift.Nature));
        }

        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Starter Pokemon gender set to random.");
    }

    private ColosseumPatchChangeSet InfiniteUseTms()
    {
        var ramOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x22b14,
            GameCubeRegion.Europe => 0x246fc,
            GameCubeRegion.Japan => throw new NotSupportedException("Infinite TMs is not implemented for the Japanese Colosseum DOL."),
            _ => throw UnsupportedRegion()
        };

        WriteRamAsm(ramOffset, Li(0, 0));
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Made TMs reusable.");
    }

    private ColosseumPatchChangeSet Gen6CritMultipliers()
    {
        if (_region != GameCubeRegion.UnitedStates)
        {
            throw new NotSupportedException("Gen 6 critical-hit multipliers are only implemented for US Colosseum in the legacy patcher.");
        }

        ApplyGen6CritMultiplier(0x227d14, source: 31, destination: 27, next: 25);
        ApplyGen6CritMultiplier(0x211640, source: 25, destination: 25, next: 14);
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Applied Gen 6+ critical-hit multiplier.");
    }

    private void ApplyGen6CritMultiplier(int entryPoint, int source, int destination, int next)
    {
        var freeSpace = AllocateDolFreeSpace(36);
        var entry = NormalizeRamOffset(entryPoint);
        var free = NormalizeRamOffset(freeSpace);
        WriteRamAsm(
            entryPoint,
            Branch(entry, freeSpace),
            NopInstruction,
            Mr(3, next));

        WriteRamAsm(
            freeSpace,
            Cmpwi(3, 2),
            Bne(free + 4, free + 20),
            Mulli(destination, source, 3),
            Srawi(destination, destination, 1),
            Branch(free + 16, free + 24),
            Mr(destination, source),
            Branch(free + 24, entry + 4));
    }

    private ColosseumPatchChangeSet Gen7CritRatios()
    {
        var critRatiosStartOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x397c18,
            GameCubeRegion.Japan => 0x384388,
            GameCubeRegion.Europe => throw new NotSupportedException("Gen 7 critical-hit ratios are not implemented for European Colosseum in the legacy patcher."),
            _ => throw UnsupportedRegion()
        };

        Dol().WriteBytes(critRatiosStartOffset, [24, 8, 2, 1, 1]);
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Applied Gen 7+ critical-hit ratios.");
    }

    private ColosseumPatchChangeSet RemoveTradeEvolutions()
        => RewriteEvolutionMethods(
            method => method is 0x05 or 0x06,
            "Trade evolutions now evolve at level 40.");

    private ColosseumPatchChangeSet RemoveItemEvolutions()
        => RewriteEvolutionMethods(
            method => method == 0x07,
            "Evolution-stone evolutions now evolve at level 40.");

    private ColosseumPatchChangeSet RewriteEvolutionMethods(Func<int, bool> predicate, string message)
    {
        var start = PointerFor(ColosseumCommonIndex.PokemonStats);
        var count = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        for (var pokemon = 0; pokemon < count; pokemon++)
        {
            var offset = start + (pokemon * PokemonStatsSize);
            for (var index = 0; index < PokemonStatsEvolutionCount; index++)
            {
                var evolutionStart = offset + PokemonStatsFirstEvolutionOffset + (index * PokemonStatsEvolutionSize);
                var method = _data.ReadByte(evolutionStart + PokemonStatsEvolutionMethodOffset);
                if (!predicate(method))
                {
                    continue;
                }

                _data.WriteByte(evolutionStart + PokemonStatsEvolutionMethodOffset, 0x04);
                _data.WriteUInt16(evolutionStart + PokemonStatsEvolutionConditionOffset, 40);
            }

            RefreshPokemonStatsCache(pokemon);
        }

        return ColosseumPatchChangeSet.Empty.WithCommonRel().WithMessage(message);
    }

    private ColosseumPatchChangeSet SetStarterShininess(int shinyValue)
    {
        foreach (var gift in GiftPokemon.Where(gift => gift.RowId is 1 or 2))
        {
            WriteGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                gift.SpeciesId,
                gift.Level,
                gift.MoveIds,
                shinyValue,
                gift.Gender,
                gift.Nature));
        }

        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Updated starter Pokemon shininess.");
    }

    private ColosseumPatchChangeSet EnableDebugLogs()
    {
        if (_region != GameCubeRegion.UnitedStates)
        {
            throw new NotSupportedException("Debug logs are only implemented for US Colosseum in the legacy patcher.");
        }

        WriteRamAsm(0x0dd970, Branch(0x0dd970, 0x09c2e0));
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Enabled debug logs.");
    }

    private ColosseumPatchChangeSet NoTypeIconForLockedMoves()
    {
        if (_region != GameCubeRegion.UnitedStates)
        {
            throw new NotSupportedException("The locked-move type-icon patch is only implemented for US Colosseum in the legacy patcher.");
        }

        WriteRamAsm(0x094b0c, Li(0, 0));
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Removed the locked-shadow-move type icon.");
    }

    private ColosseumPatchChangeSet PokemonCanLearnAnyTm()
    {
        var start = PointerFor(ColosseumCommonIndex.PokemonStats);
        var count = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        for (var pokemon = 0; pokemon < count; pokemon++)
        {
            var offset = start + (pokemon * PokemonStatsSize);
            for (var tm = 0; tm < PokemonStatsTmCount; tm++)
            {
                _data.WriteByte(offset + PokemonStatsFirstTmOffset + tm, 1);
            }

            RefreshPokemonStatsCache(pokemon);
        }

        return ColosseumPatchChangeSet.Empty.WithCommonRel().WithMessage("Every Pokemon can now learn every TM.");
    }

    private ColosseumPatchChangeSet PokemonHaveMaxCatchRate()
    {
        var pokemonStart = PointerFor(ColosseumCommonIndex.PokemonStats);
        var pokemonCount = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        for (var pokemon = 0; pokemon < pokemonCount; pokemon++)
        {
            var offset = pokemonStart + (pokemon * PokemonStatsSize);
            if (_data.ReadByte(offset + PokemonStatsCatchRateOffset) > 0)
            {
                _data.WriteByte(offset + PokemonStatsCatchRateOffset, 255);
            }

            RefreshPokemonStatsCache(pokemon);
        }

        var shadowStart = PointerFor(ColosseumCommonIndex.ShadowData);
        var shadowCount = GetCount(ColosseumCommonIndex.NumberOfShadowPokemon);
        for (var shadow = 1; shadow < shadowCount; shadow++)
        {
            _data.WriteByte(shadowStart + (shadow * ShadowDataSize) + ShadowCatchRateOffset, 255);
        }

        return ColosseumPatchChangeSet.Empty.WithCommonRel().WithMessage("Set Pokemon and shadow catch rates to 255.");
    }

    private ColosseumPatchChangeSet SetAllBattlesTo(int battleStyle)
    {
        var start = PointerFor(ColosseumCommonIndex.Battles);
        var count = GetCount(ColosseumCommonIndex.NumberOfBattles);
        for (var battle = 0; battle < count; battle++)
        {
            _data.WriteByte(start + (battle * BattleSize) + BattleStyleOffset, ClampByte(battleStyle));
        }

        return ColosseumPatchChangeSet.Empty
            .WithCommonRel()
            .WithMessage(battleStyle == 1 ? "Set all battles to single battles." : "Set all battles to double battles.");
    }

    private ColosseumPatchChangeSet UnlockColbtlBin()
    {
        var regionStringCheckRamOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x7448c,
            GameCubeRegion.Europe => 0x77990,
            GameCubeRegion.Japan => 0x735b0,
            _ => throw UnsupportedRegion()
        };
        var validResponseHardcodeRamOffset = _region switch
        {
            GameCubeRegion.UnitedStates => 0x939dc,
            GameCubeRegion.Europe => 0x96f00,
            GameCubeRegion.Japan => 0x9169c,
            _ => throw UnsupportedRegion()
        };

        WriteRamAsm(regionStringCheckRamOffset, BranchForward(0, 12));
        WriteRamAsm(validResponseHardcodeRamOffset, Li(6, 0x23));
        return ColosseumPatchChangeSet.Empty.WithStartDol().WithMessage("Removed the colbtl.bin region lock.");
    }

    private void ApplyDefaultMoveCategories()
    {
        var categories = LoadDefaultMoveCategories();
        var moveCount = Math.Min(GetCount(ColosseumCommonIndex.NumberOfMoves), categories.Count);
        var start = PointerFor(ColosseumCommonIndex.Moves);
        for (var move = 0; move < moveCount; move++)
        {
            _data.WriteByte(start + (move * MoveSize) + MoveCategoryOffset, ClampByte(categories[move]));
            RefreshMoveCache(move);
        }

        if (GetCount(ColosseumCommonIndex.NumberOfMoves) > 355)
        {
            _data.WriteByte(start + (355 * MoveSize) + MoveCategoryOffset, 1);
            RefreshMoveCache(355);
        }
    }

    private static IReadOnlyList<int> LoadDefaultMoveCategories()
    {
        var relativeParts = new[] { "assets", "json", "Colosseum", "Move Categories.json" };
        foreach (var candidate in CandidateAssetPaths(relativeParts))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var values = JsonSerializer.Deserialize<int[]>(File.ReadAllText(candidate));
            if (values is { Length: > 0 })
            {
                return values;
            }
        }

        throw new FileNotFoundException("Could not find assets/json/Colosseum/Move Categories.json for the physical/special split patch.");
    }

    private static IEnumerable<string> CandidateAssetPaths(IReadOnlyList<string> relativeParts)
    {
        var roots = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var root in roots)
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                yield return Path.Combine([current.FullName, .. relativeParts]);
                yield return Path.Combine([current.FullName, "Assets", "Json", "Colosseum", "Move Categories.json"]);
                current = current.Parent;
            }
        }
    }

    private int AllocateDolFreeSpace(int numberOfBytes)
    {
        var dol = Dol();
        var start = FreeSpaceStart();
        var end = FreeSpaceEnd();
        if (dol.ReadUInt32(start) != PatchMarker)
        {
            for (var clearOffset = start; clearOffset < end; clearOffset += 4)
            {
                dol.WriteUInt32(clearOffset, NopInstruction);
            }

            dol.WriteUInt32(start, PatchMarker);
            dol.WriteUInt32(start + 4, 0xffffffff);
            dol.WriteUInt32(start + 8, 0xffffffff);
            dol.WriteUInt32(start + 12, 0xffffffff);
        }

        var offsetCandidate = dol.ReadUInt32(start + 4);
        int offset;
        if (offsetCandidate == 0xffffffff)
        {
            offset = start + 16;
        }
        else
        {
            var normalized = offsetCandidate - DolTableToRamOffsetDifference;
            if (normalized >= 0x80000000u)
            {
                normalized -= 0x80000000u;
            }

            offset = checked((int)normalized);
        }

        if (offset < start + 16 || offset > end)
        {
            offset = start + 16;
        }

        while (offset % 4 != 0)
        {
            offset++;
        }

        var wordsToCheck = Math.Max(1, (numberOfBytes + 3) / 4);
        while (offset + (wordsToCheck * 4) < end && !DolRangeIsFree(dol, offset, wordsToCheck))
        {
            offset += 4;
        }

        if (offset + (wordsToCheck * 4) >= end)
        {
            throw new InvalidOperationException("Could not find enough free DOL space for the patch.");
        }

        var ramOffset = offset + DolTableToRamOffsetDifference;
        dol.WriteUInt32(start + 4, unchecked((uint)ramOffset + 0x80000000u));
        return ramOffset;
    }

    private static bool DolRangeIsFree(BinaryData dol, int offset, int wordsToCheck)
    {
        for (var index = 0; index < wordsToCheck; index++)
        {
            var value = dol.ReadUInt32(offset + (index * 4));
            if (value is not (0 or NopInstruction))
            {
                return false;
            }
        }

        return true;
    }

    private int FreeSpaceStart()
        => _region switch
        {
            GameCubeRegion.UnitedStates => 0xbe348 - DolTableToRamOffsetDifference,
            GameCubeRegion.Europe => 0xc1948 - DolTableToRamOffsetDifference,
            GameCubeRegion.Japan => 0xbb4a8 - DolTableToRamOffsetDifference,
            _ => throw UnsupportedRegion()
        };

    private int FreeSpaceEnd()
        => _region switch
        {
            GameCubeRegion.UnitedStates => 0xc459c - DolTableToRamOffsetDifference,
            GameCubeRegion.Europe => 0xc7b9c - DolTableToRamOffsetDifference,
            GameCubeRegion.Japan => 0xc16f8 - DolTableToRamOffsetDifference,
            _ => throw UnsupportedRegion()
        };

    private void RefreshPokemonStatsCache(int pokemonIndex)
    {
        if (_pokemonStats.IsValueCreated && _pokemonStats.Value is Dictionary<int, ColosseumPokemonStats> stats)
        {
            stats[pokemonIndex] = ReadPokemonStats(pokemonIndex);
        }
    }

    private void RefreshMoveCache(int moveIndex)
    {
        if (_moves.IsValueCreated && _moves.Value is Dictionary<int, ColosseumMove> moves)
        {
            moves[moveIndex] = ReadMove(
                moveIndex,
                PointerFor(ColosseumCommonIndex.Moves),
                UseHmFlagForShadowMoves(GetCount(ColosseumCommonIndex.NumberOfMoves)));
        }
    }

    private BinaryData Dol()
        => _dol ?? throw new InvalidOperationException("Start.dol was not loaded.");

    private void WriteDolUInt32(int offset, uint value)
        => Dol().WriteUInt32(offset, value);

    private void WriteRamAsm(int ramOffset, params uint[] instructions)
    {
        var dolOffset = RamToDolOffset(ramOffset);
        for (var index = 0; index < instructions.Length; index++)
        {
            WriteDolUInt32(dolOffset + (index * 4), instructions[index]);
        }
    }

    private static int NormalizeRamOffset(int offset)
    {
        var value = unchecked((uint)offset);
        if (value >= 0x80000000u)
        {
            value -= 0x80000000u;
        }

        return checked((int)value);
    }

    private static int RamToDolOffset(int ramOffset)
        => NormalizeRamOffset(ramOffset) - DolTableToRamOffsetDifference;

    private NotSupportedException UnsupportedRegion()
        => new($"Patch is not implemented for this Colosseum region: {_region}.");

    private static uint Add(int rd, int ra, int rb)
        => (31u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | ((uint)rb << 11) | (266u << 1);

    private static uint Addi(int rd, int ra, int simm)
        => (14u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | (unchecked((uint)simm) & 0xffff);

    private static uint Andi(int rd, int ra, uint uimm)
        => (28u << 26) | ((uint)ra << 21) | ((uint)rd << 16) | (uimm & 0xffff);

    private static uint Branch(int currentRamOffset, int targetRamOffset)
    {
        var current = NormalizeRamOffset(currentRamOffset);
        var target = NormalizeRamOffset(targetRamOffset);
        return 0x48000000 | (unchecked((uint)(target - current)) & 0x03ffffff);
    }

    private static uint BranchForward(int origin, int target)
        => 0x48000000 | (unchecked((uint)(target - origin)) & 0x03ffffff);

    private static uint BranchLink(int currentRamOffset, int targetRamOffset)
        => Branch(currentRamOffset, targetRamOffset) | 1;

    private static uint Bne(int currentRamOffset, int targetRamOffset)
        => 0x40820000 | (unchecked((uint)(NormalizeRamOffset(targetRamOffset) - NormalizeRamOffset(currentRamOffset))) & 0xffff);

    private static uint Blr()
        => 0x4e800020;

    private static uint Cmpwi(int ra, int simm)
        => (11u << 26) | ((uint)ra << 16) | (unchecked((uint)simm) & 0xffff);

    private static uint Lbz(int rd, int ra, int d)
        => (34u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | (unchecked((uint)d) & 0xffff);

    private static uint Li(int rd, int simm)
        => Addi(rd, 0, simm);

    private static uint Lis(int rd, int simm)
        => (15u << 26) | ((uint)rd << 21) | (unchecked((uint)simm) & 0xffff);

    private static uint Lwz(int rd, int ra, int d)
        => (32u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | (unchecked((uint)d) & 0xffff);

    private static uint Mr(int rd, int rs)
        => (31u << 26) | ((uint)rs << 21) | ((uint)rd << 16) | ((uint)rs << 11) | (444u << 1);

    private static uint Mulli(int rd, int ra, int simm)
        => (7u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | (unchecked((uint)simm) & 0xffff);

    private static uint Rlwinm(int rd, int ra, uint sh, uint mb, uint me, bool affectsConditionRegister = false)
    {
        var code = (21u << 26) | ((uint)ra << 21) | ((uint)rd << 16) | (sh << 11) | (mb << 6) | (me << 1);
        return affectsConditionRegister ? code | 1 : code;
    }

    private static uint Srawi(int rd, int ra, uint sh)
        => (31u << 26) | ((uint)ra << 21) | ((uint)rd << 16) | (sh << 11) | (824u << 1);

    private static uint Stw(int rs, int ra, int d)
        => (36u << 26) | ((uint)rs << 21) | ((uint)ra << 16) | (unchecked((uint)d) & 0xffff);

    private static (uint High, uint Low) LoadImmediateShifted32Bit(int register, uint value)
    {
        var low = value & 0xffff;
        var addition = low < 0x8000;
        var high = (int)(value >> 16) + (addition ? 0 : 1);
        var highInstruction = Lis(register, high);
        var lowInstruction = addition
            ? Addi(register, register, (int)low)
            : Addi(register, register, -((int)(0x10000 - low)));
        return (highInstruction, lowInstruction);
    }
}
