using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    private const int XdDolToRamOffsetDifference = 0x30a0;
    private const uint XdNopInstruction = 0x60000000;
    private const int ShinyNever = 0x0000;
    private const int ShinyAlways = 0x0001;
    private const int ShinyRandom = 0xffff;
    private const int GenderRandom = 0xff;
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
            case XdPatchKind.DisableSaveCorruption:
                PatchStartDol(writtenFiles, messages, PatchDisableSaveCorruption, "Disabled save-corruption checks.");
                break;

            case XdPatchKind.InfiniteTms:
                PatchStartDol(writtenFiles, messages, PatchInfiniteTms, "Made TMs reusable.");
                break;

            case XdPatchKind.ExpAll:
                PatchStartDol(writtenFiles, messages, PatchExpAll, "All party Pokemon now gain EXP without battle participation.");
                break;

            case XdPatchKind.AllowFemaleStarters:
                PatchStartDol(writtenFiles, messages, PatchAllowFemaleStarters, "Demo starter Pokemon gender set to random.");
                break;

            case XdPatchKind.BetaStartersApply:
                PatchStartDol(writtenFiles, messages, PatchBetaStartersApply, "Enabled the beta two-starter routine.");
                break;

            case XdPatchKind.BetaStartersRemove:
                PatchStartDol(writtenFiles, messages, PatchBetaStartersRemove, "Restored the vanilla one-starter routine.");
                break;

            case XdPatchKind.FixShinyGlitch:
                PatchStartDol(writtenFiles, messages, PatchFixShinyGlitch, "Fixed the XD shiny purification glitch.");
                break;

            case XdPatchKind.ReplaceShinyGlitch:
                PatchStartDol(writtenFiles, messages, PatchReplaceShinyGlitch, "Restored the vanilla shiny purification behavior.");
                break;

            case XdPatchKind.AllowShinyShadowPokemon:
                PatchStartDol(writtenFiles, messages, dol => PatchShadowPokemonShininess(dol, ShinyRandom), "Shadow Pokemon shininess set to random.");
                break;

            case XdPatchKind.ShinyLockShadowPokemon:
                PatchStartDol(writtenFiles, messages, dol => PatchShadowPokemonShininess(dol, ShinyNever), "Shadow Pokemon shininess locked to never.");
                break;

            case XdPatchKind.AlwaysShinyShadowPokemon:
                PatchStartDol(writtenFiles, messages, dol => PatchShadowPokemonShininess(dol, ShinyAlways), "Shadow Pokemon shininess set to always.");
                break;

            case XdPatchKind.Gen7CritRatios:
                PatchStartDol(writtenFiles, messages, PatchGen7CritRatios, "Applied Gen 7+ critical-hit ratios.");
                break;

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

            case XdPatchKind.Type9IndependentApply:
                PatchStartDol(writtenFiles, messages, PatchType9Independent, "Made the ??? type independent in the XD battle engine.");
                break;

            case XdPatchKind.MaxPokespotEntries:
                PatchStartDol(writtenFiles, messages, PatchMaxPokespotEntries, "Set the max Pokespot entries per spot to 100.");
                break;

            case XdPatchKind.PreventPokemonRelease:
                PatchStartDol(writtenFiles, messages, PatchPreventPokemonRelease, "Disabled Pokemon release.");
                break;

            case XdPatchKind.CompleteStrategyMemo:
                PatchStartDol(writtenFiles, messages, PatchCompleteStrategyMemo, "Unlocked all Strategy Memo species.");
                break;

            case XdPatchKind.DisableBattleAnimations:
                PatchStartDol(writtenFiles, messages, PatchDisableBattleAnimations, "Disabled battle attack animations.");
                break;

            case XdPatchKind.EnableDebugLogs:
                PatchStartDol(writtenFiles, messages, PatchEnableDebugLogs, "Enabled debug logs.");
                break;

            case XdPatchKind.RemoveEvCap:
                PatchStartDol(writtenFiles, messages, PatchRemoveEvCap, "Raised the EV total cap to 1530.");
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

    private void PatchStartDol(
        ICollection<string> writtenFiles,
        ICollection<string> messages,
        Action<BinaryData> patch,
        string message)
    {
        var dolEntry = FindIsoFile("Start.dol")
            ?? throw new FileNotFoundException("Start.dol was not found in the ISO.");
        var dol = ReadStartDolOrThrow();
        var original = dol.ToArray();
        patch(dol);
        var changed = !original.SequenceEqual(dol.ToArray());
        if (changed)
        {
            writtenFiles.Add(WriteStartDol(dol, dolEntry));
        }

        messages.Add(changed ? message : $"{message} Already applied.");
    }

    private void PatchDisableSaveCorruption(BinaryData dol)
    {
        var saveCountRamOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1cc304,
            GameCubeRegion.Europe => 0x1cd764,
            GameCubeRegion.Japan => 0x1c7984,
            _ => throw UnsupportedXdPatchRegion()
        };
        var memoryCardMatchOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1cc4b0,
            GameCubeRegion.Europe => 0x1cd910,
            GameCubeRegion.Japan => 0x1c7b30,
            _ => throw UnsupportedXdPatchRegion()
        };

        WriteRamAsm(dol, saveCountRamOffset, Stw(7, 5, 0x2c));
        WriteRamAsm(dol, memoryCardMatchOffset, XdNopInstruction);
    }

    private void PatchInfiniteTms(BinaryData dol)
    {
        var ramOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x0a5158,
            GameCubeRegion.Europe => 0x0a61f0,
            GameCubeRegion.Japan => throw new NotSupportedException("Infinite TMs is not implemented for Japanese Pokemon XD in the legacy patcher."),
            _ => throw UnsupportedXdPatchRegion()
        };

        WriteRamAsm(dol, ramOffset, Li(0, 0));
    }

    private void PatchExpAll(BinaryData dol)
    {
        RequireUsXdPatch("EXP All");
        WriteRamAsm(
            dol,
            0x80212c94,
            XdNopInstruction,
            Rlwinm(3, 0, 31, 17, 31),
            Mr(0, 3));
        WriteRamAsm(dol, 0x80212dec, XdNopInstruction);
        WriteRamAsm(
            dol,
            0x80212f6c,
            XdNopInstruction,
            Mr(27, 3),
            XdNopInstruction);
    }

    private void PatchAllowFemaleStarters(BinaryData dol)
    {
        foreach (var layout in XdGiftLayouts(Iso.Region).Where(layout => layout.RowId is 1 or 2))
        {
            WriteU16Checked(dol, layout.StartOffset + 0x56, GenderRandom);
        }
    }

    private void PatchBetaStartersApply(BinaryData dol)
    {
        var codeStartOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x801ced68,
            GameCubeRegion.Europe => 0x801d083c,
            GameCubeRegion.Japan => 0x801c9c0c,
            _ => throw UnsupportedXdPatchRegion()
        };
        var demoStarterFunctions = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => (0x80152674, 0x8015279c),
            GameCubeRegion.Europe => (0x80153f38, 0x80154060),
            GameCubeRegion.Japan => (0x8014d99c, 0x8014dac4),
            _ => throw UnsupportedXdPatchRegion()
        };
        var codeStart = NormalizeRamOffset(codeStartOffset);
        WriteRamAsm(
            dol,
            codeStartOffset,
            BranchLink(codeStart, demoStarterFunctions.Item1),
            Mr(3, 31),
            BranchLink(codeStart + 8, demoStarterFunctions.Item2));
    }

    private void PatchBetaStartersRemove(BinaryData dol)
    {
        var codeStartOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x801ced68,
            GameCubeRegion.Europe => 0x801d083c,
            GameCubeRegion.Japan => 0x801c9c0c,
            _ => throw UnsupportedXdPatchRegion()
        };
        var starterFunction = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x80152ae0,
            GameCubeRegion.Europe => 0x801543a4,
            GameCubeRegion.Japan => 0x8014de08,
            _ => throw UnsupportedXdPatchRegion()
        };
        var codeStart = NormalizeRamOffset(codeStartOffset);
        WriteRamAsm(
            dol,
            codeStartOffset,
            Stw(6, 1, 0x38),
            Stw(0, 1, 0x3c),
            BranchLink(codeStart + 8, starterFunction));
    }

    private void PatchFixShinyGlitch(BinaryData dol)
    {
        var codeStartOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1fa930,
            GameCubeRegion.Europe => 0x1fc664,
            GameCubeRegion.Japan => 0x1f55f4,
            _ => throw UnsupportedXdPatchRegion()
        };
        var getTrainerData = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1cefb4,
            GameCubeRegion.Europe => 0x1d0a8c,
            GameCubeRegion.Japan => 0x1c9e54,
            _ => throw UnsupportedXdPatchRegion()
        };
        var trainerGetTid = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x14e118,
            GameCubeRegion.Europe => 0x14f9dc,
            GameCubeRegion.Japan => 0x149460,
            _ => throw UnsupportedXdPatchRegion()
        };
        var tidRerollOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1fa9c8,
            GameCubeRegion.Europe => 0x1fc6fc,
            GameCubeRegion.Japan => 0x1f568c,
            _ => throw UnsupportedXdPatchRegion()
        };
        var codeStart = NormalizeRamOffset(codeStartOffset);
        WriteRamAsm(
            dol,
            codeStartOffset,
            Li(3, 0),
            Li(4, 2),
            BranchLink(codeStart + 8, getTrainerData),
            BranchLink(codeStart + 12, trainerGetTid));
        WriteRamAsm(dol, tidRerollOffset, BranchForward(0, 0x2c));
    }

    private void PatchReplaceShinyGlitch(BinaryData dol)
    {
        if (Iso.Region == GameCubeRegion.Japan)
        {
            throw new NotSupportedException("Replacing the shiny glitch is not implemented for Japanese Pokemon XD in the legacy patcher.");
        }

        var codeStartOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1fa930,
            GameCubeRegion.Europe => 0x1fc664,
            _ => throw UnsupportedXdPatchRegion()
        };
        var trainerGetValue = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x14d6e0,
            GameCubeRegion.Europe => 0x14efa4,
            _ => throw UnsupportedXdPatchRegion()
        };
        var codeStart = NormalizeRamOffset(codeStartOffset);
        WriteRamAsm(
            dol,
            codeStartOffset,
            Mr(3, 28),
            Li(4, 2),
            Li(5, 0),
            BranchLink(codeStart + 12, trainerGetValue));
    }

    private void PatchShadowPokemonShininess(BinaryData dol, int shinyValue)
    {
        var shadowOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x1fc2b2,
            GameCubeRegion.Europe => 0x1fdfe6,
            GameCubeRegion.Japan => 0x1f6f76,
            _ => throw UnsupportedXdPatchRegion()
        };
        var tradeShadowOffset = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => 0x152bfe,
            GameCubeRegion.Europe => 0x1544c2,
            GameCubeRegion.Japan => 0x14df26,
            _ => throw UnsupportedXdPatchRegion()
        };

        WriteU16Checked(dol, RamToDolOffset(shadowOffset), shinyValue);
        WriteU16Checked(dol, RamToDolOffset(tradeShadowOffset), shinyValue);
    }

    private void PatchGen7CritRatios(BinaryData dol)
    {
        RequireUsXdPatch("Gen 7 critical-hit ratios");
        dol.WriteBytes(0x41dd20, [24, 8, 2, 1, 1]);
    }

    private void PatchType9Independent(BinaryData dol)
    {
        RequireUsXdPatch("??? type independence");
        dol.WriteUInt32(0x2c55b0, 0x480000e4);
        dol.WriteUInt32(0x031230, 0x3b400000);
        foreach (var offset in new[] { 0x2c59fc, 0x2c5d8c, 0x2c8870, 0x2c895c })
        {
            dol.WriteUInt32(offset, XdNopInstruction);
        }
    }

    private void PatchMaxPokespotEntries(BinaryData dol)
    {
        RequireUsXdPatch("Max Pokespot entries");
        WriteU16Checked(dol, RamToDolOffset(0x1faf52), 100);
    }

    private void PatchPreventPokemonRelease(BinaryData dol)
    {
        RequireUsXdPatch("Prevent Pokemon release");
        WriteRamAsm(dol, 0x8005aa38, Li(0, 0));
    }

    private void PatchCompleteStrategyMemo(BinaryData dol)
    {
        RequireUsXdPatch("Complete Strategy Memo");
        WriteRamAsm(dol, 0x80234b0c, Addi(3, 4, 1), Blr());
        WriteRamAsm(dol, 0x80234b6c, Li(3, 0x19e), Blr());
        WriteRamAsm(dol, 0x80234a64, Li(3, 0), Blr());
        WriteRamAsm(dol, 0x802349bc, Li(3, 0), Blr());
        WriteRamAsm(dol, 0x80055948, Blr());
    }

    private void PatchDisableBattleAnimations(BinaryData dol)
    {
        RequireUsXdPatch("Disable battle animations");
        WriteRamAsm(dol, 0x80205eb4, XdNopInstruction);
    }

    private void PatchEnableDebugLogs(BinaryData dol)
    {
        RequireUsXdPatch("Debug logs");
        WriteRamAsm(dol, 0x2a65cc, Branch(0x2a65cc, 0x0abc80));
        foreach (var first in new[] { 0x2aae4c, 0x2a85c4, 0x2a89cc, 0x2a8be0, 0x2a9104, 0x2a9308, 0x2a964c, 0x2a9874, 0x2a9a58, 0x2aa908 })
        {
            for (var index = 0; index < 4; index++)
            {
                WriteRamAsm(dol, first + (index * 20), XdNopInstruction);
            }
        }
    }

    private void PatchRemoveEvCap(BinaryData dol)
    {
        var offsets = Iso.Region switch
        {
            GameCubeRegion.UnitedStates => (
                Plus: new[] { 0x140528, 0x140578, 0x04c2f8, 0x1603c0, 0x160420, 0x160558, 0x1605b8, 0x1609e0, 0x160a40, 0x160b78, 0x160bd8, 0x160d10, 0x160d70, 0x160ea8, 0x160f08 },
                Minus: new[] { 0x140580, 0x160428, 0x1605c0, 0x160a48, 0x160be0, 0x160d78, 0x160f10 }),
            GameCubeRegion.Europe => (
                Plus: new[] { 0x141dec, 0x141e3c, 0x04c34c, 0x161c70, 0x161cd0, 0x161e08, 0x161e68, 0x162290, 0x1622f0, 0x162428, 0x162488, 0x1625c0, 0x162620, 0x162758, 0x1627b8 },
                Minus: new[] { 0x141e44, 0x161cd8, 0x161e70, 0x1622f8, 0x162490, 0x162628, 0x1627c0 }),
            GameCubeRegion.Japan => throw new NotSupportedException("Removing the EV cap is not implemented for Japanese Pokemon XD in the legacy patcher."),
            _ => throw UnsupportedXdPatchRegion()
        };

        const int newMaxEvs = 1530;
        foreach (var offset in offsets.Plus)
        {
            WriteU16Checked(dol, offset + 2 - XdDolToRamOffsetDifference, newMaxEvs);
        }

        var negativeValue = unchecked((ushort)(-newMaxEvs));
        foreach (var offset in offsets.Minus)
        {
            WriteU16Checked(dol, offset + 2 - XdDolToRamOffsetDifference, negativeValue);
        }
    }

    private void RequireUsXdPatch(string patchName)
    {
        if (Iso.Region != GameCubeRegion.UnitedStates)
        {
            throw new NotSupportedException($"{patchName} is only implemented for US Pokemon XD in the legacy patcher.");
        }
    }

    private NotSupportedException UnsupportedXdPatchRegion()
        => new($"Patch is not implemented for this Pokemon XD region: {Iso.Region}.");

    private static void WriteU16Checked(BinaryData data, int offset, int value)
    {
        if (offset < 0 || offset + 2 > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} is outside Start.dol.");
        }

        data.WriteUInt16(offset, checked((ushort)value));
    }

    private static void WriteRamAsm(BinaryData dol, long ramOffset, params uint[] instructions)
    {
        var dolOffset = RamToDolOffset(ramOffset);
        for (var index = 0; index < instructions.Length; index++)
        {
            dol.WriteUInt32(dolOffset + (index * 4), instructions[index]);
        }
    }

    private static int RamToDolOffset(long ramOffset)
        => NormalizeRamOffset(ramOffset) - XdDolToRamOffsetDifference;

    private static int NormalizeRamOffset(long offset)
    {
        var value = unchecked((uint)offset);
        if (value >= 0x80000000u)
        {
            value -= 0x80000000u;
        }

        return checked((int)value);
    }

    private static uint Addi(int rd, int ra, int simm)
        => (14u << 26) | ((uint)rd << 21) | ((uint)ra << 16) | (unchecked((uint)simm) & 0xffff);

    private static uint Branch(long currentRamOffset, long targetRamOffset)
    {
        var current = NormalizeRamOffset(currentRamOffset);
        var target = NormalizeRamOffset(targetRamOffset);
        return 0x48000000 | (unchecked((uint)(target - current)) & 0x03ffffff);
    }

    private static uint BranchForward(int origin, int target)
        => 0x48000000 | (unchecked((uint)(target - origin)) & 0x03ffffff);

    private static uint BranchLink(long currentRamOffset, long targetRamOffset)
        => Branch(currentRamOffset, targetRamOffset) | 1;

    private static uint Blr()
        => 0x4e800020;

    private static uint Li(int rd, int simm)
        => Addi(rd, 0, simm);

    private static uint Mr(int rd, int rs)
        => (31u << 26) | ((uint)rs << 21) | ((uint)rd << 16) | ((uint)rs << 11) | (444u << 1);

    private static uint Rlwinm(int rd, int ra, uint sh, uint mb, uint me)
        => (21u << 26) | ((uint)ra << 21) | ((uint)rd << 16) | (sh << 11) | (mb << 6) | (me << 1);

    private static uint Stw(int rs, int ra, int d)
        => (36u << 26) | ((uint)rs << 21) | ((uint)ra << 16) | (unchecked((uint)d) & 0xffff);

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
