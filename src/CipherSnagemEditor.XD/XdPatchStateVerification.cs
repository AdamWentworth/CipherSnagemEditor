using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.XD;

public sealed record XdPatchStateVerification(
    XdPatchKind Patch,
    int Checks,
    IReadOnlyList<string> Failures)
{
    public bool Verified => Failures.Count == 0;
}

public sealed partial class XdProjectContext
{
    public XdPatchStateVerification VerifyPatchState(XdPatchKind kind)
    {
        var failures = new List<string>();
        var checks = 0;

        switch (kind)
        {
            case XdPatchKind.PhysicalSpecialSplitApply:
                checks += VerifyIdempotentDolPatch(kind, PatchPhysicalSpecialSplitApplyDol, failures);
                break;

            case XdPatchKind.PhysicalSpecialSplitRemove:
                checks += VerifyIdempotentDolPatch(kind, PatchPhysicalSpecialSplitRemove, failures);
                break;

            case XdPatchKind.DisableSaveCorruption:
                checks += VerifyIdempotentDolPatch(kind, PatchDisableSaveCorruption, failures);
                break;

            case XdPatchKind.InfiniteTms:
                checks += VerifyIdempotentDolPatch(kind, PatchInfiniteTms, failures);
                break;

            case XdPatchKind.ExpAll:
                checks += VerifyIdempotentDolPatch(kind, PatchExpAll, failures);
                break;

            case XdPatchKind.AllowFemaleStarters:
                checks += VerifyIdempotentDolPatch(kind, PatchAllowFemaleStarters, failures);
                break;

            case XdPatchKind.BetaStartersApply:
                checks += VerifyIdempotentDolPatch(kind, PatchBetaStartersApply, failures);
                break;

            case XdPatchKind.BetaStartersRemove:
                checks += VerifyIdempotentDolPatch(kind, PatchBetaStartersRemove, failures);
                break;

            case XdPatchKind.FixShinyGlitch:
                checks += VerifyIdempotentDolPatch(kind, PatchFixShinyGlitch, failures);
                break;

            case XdPatchKind.ReplaceShinyGlitch:
                checks += VerifyIdempotentDolPatch(kind, PatchReplaceShinyGlitch, failures);
                break;

            case XdPatchKind.AllowShinyShadowPokemon:
                checks += VerifyIdempotentDolPatch(kind, dol => PatchShadowPokemonShininess(dol, ShinyRandom), failures);
                break;

            case XdPatchKind.ShinyLockShadowPokemon:
                checks += VerifyIdempotentDolPatch(kind, dol => PatchShadowPokemonShininess(dol, ShinyNever), failures);
                break;

            case XdPatchKind.AlwaysShinyShadowPokemon:
                checks += VerifyIdempotentDolPatch(kind, dol => PatchShadowPokemonShininess(dol, ShinyAlways), failures);
                break;

            case XdPatchKind.Gen6CritMultipliers:
                checks += VerifyGen6CritMultiplierState(failures);
                break;

            case XdPatchKind.Gen7CritRatios:
                checks += VerifyIdempotentDolPatch(kind, PatchGen7CritRatios, failures);
                break;

            case XdPatchKind.Type9IndependentApply:
                checks += VerifyIdempotentDolPatch(kind, PatchType9Independent, failures);
                break;

            case XdPatchKind.MaxPokespotEntries:
                checks += VerifyIdempotentDolPatch(kind, PatchMaxPokespotEntries, failures);
                break;

            case XdPatchKind.PreventPokemonRelease:
                checks += VerifyIdempotentDolPatch(kind, PatchPreventPokemonRelease, failures);
                break;

            case XdPatchKind.CompleteStrategyMemo:
                checks += VerifyIdempotentDolPatch(kind, PatchCompleteStrategyMemo, failures);
                break;

            case XdPatchKind.DisableBattleAnimations:
                checks += VerifyIdempotentDolPatch(kind, PatchDisableBattleAnimations, failures);
                break;

            case XdPatchKind.EnableDebugLogs:
                checks += VerifyIdempotentDolPatch(kind, PatchEnableDebugLogs, failures);
                break;

            case XdPatchKind.RemoveEvCap:
                checks += VerifyIdempotentDolPatch(kind, PatchRemoveEvCap, failures);
                break;
        }

        return new XdPatchStateVerification(kind, checks, failures);
    }

    private int VerifyIdempotentDolPatch(
        XdPatchKind kind,
        Action<BinaryData> patch,
        ICollection<string> failures)
    {
        try
        {
            var current = ReadStartDolOrThrow();
            var expected = new BinaryData(current.ToArray());
            patch(expected);

            var currentBytes = current.ToArray();
            var expectedBytes = expected.ToArray();
            if (!currentBytes.SequenceEqual(expectedBytes))
            {
                failures.Add($"{kind}: Start.dol does not match the expected patched state ({FirstDifferentByte(currentBytes, expectedBytes)}).");
            }
        }
        catch (Exception ex) when (ex is InvalidDataException
            or InvalidOperationException
            or IOException
            or ArgumentOutOfRangeException
            or EndOfStreamException
            or OverflowException
            or NotSupportedException)
        {
            failures.Add($"{kind}: Start.dol state verification failed: {ex.Message}");
        }

        return 1;
    }

    private int VerifyGen6CritMultiplierState(ICollection<string> failures)
    {
        try
        {
            RequireUsXdPatch("Gen 6 critical-hit multipliers");

            const long entryPoint = 0x8020dafc;
            var dol = ReadStartDolOrThrow();
            var branchInstruction = ReadRamUInt32(dol, entryPoint);
            var target = TryDecodeBranchTarget(entryPoint, branchInstruction);
            if (target is null)
            {
                failures.Add("Gen6CritMultipliers: entry point does not branch to a free-space routine.");
                return 1;
            }

            var targetDolOffset = RamToDolOffset(target.Value);
            if (targetDolOffset < XdDolFreeSpaceStart() + 16 || targetDolOffset + 28 > XdDolFreeSpaceEnd())
            {
                failures.Add($"Gen6CritMultipliers: free-space routine target 0x{target.Value:x} is outside the XD patch area.");
                return 1;
            }

            ExpectRamWord(dol, entryPoint + 4, XdNopInstruction, failures, "Gen6CritMultipliers entry NOP");
            ExpectRamWord(dol, entryPoint + 8, Mr(3, 26), failures, "Gen6CritMultipliers entry move-next");

            var free = target.Value;
            ExpectDolWord(dol, targetDolOffset, Cmpwi(3, 2), failures, "Gen6CritMultipliers routine crit-stage compare");
            ExpectDolWord(dol, targetDolOffset + 4, Bne(free + 4, free + 20), failures, "Gen6CritMultipliers routine non-crit branch");
            ExpectDolWord(dol, targetDolOffset + 8, Mulli(27, 31, 3), failures, "Gen6CritMultipliers routine 1.5x multiply");
            ExpectDolWord(dol, targetDolOffset + 12, Srawi(27, 27, 1), failures, "Gen6CritMultipliers routine 1.5x divide");
            ExpectDolWord(dol, targetDolOffset + 16, Branch(free + 16, free + 24), failures, "Gen6CritMultipliers routine skip vanilla");
            ExpectDolWord(dol, targetDolOffset + 20, Mr(27, 31), failures, "Gen6CritMultipliers routine vanilla damage");
            ExpectDolWord(dol, targetDolOffset + 24, Branch(free + 24, NormalizeRamOffset(entryPoint) + 4), failures, "Gen6CritMultipliers routine return");
        }
        catch (Exception ex) when (ex is InvalidDataException
            or InvalidOperationException
            or IOException
            or ArgumentOutOfRangeException
            or EndOfStreamException
            or OverflowException
            or NotSupportedException)
        {
            failures.Add($"Gen6CritMultipliers: Start.dol state verification failed: {ex.Message}");
        }

        return 1;
    }

    private static string FirstDifferentByte(byte[] current, byte[] expected)
    {
        var length = Math.Min(current.Length, expected.Length);
        for (var index = 0; index < length; index++)
        {
            if (current[index] != expected[index])
            {
                return $"first diff 0x{index:x}: actual 0x{current[index]:x2}, expected 0x{expected[index]:x2}";
            }
        }

        return current.Length == expected.Length
            ? "same bytes"
            : $"length differs: actual {current.Length:N0}, expected {expected.Length:N0}";
    }

    private static void ExpectDolWord(BinaryData dol, int offset, uint expected, ICollection<string> failures, string label)
    {
        var actual = dol.ReadUInt32(offset);
        if (actual != expected)
        {
            failures.Add($"{label}: expected 0x{expected:x8} at DOL 0x{offset:x}, found 0x{actual:x8}.");
        }
    }

    private static void ExpectRamWord(BinaryData dol, long ramOffset, uint expected, ICollection<string> failures, string label)
        => ExpectDolWord(dol, RamToDolOffset(ramOffset), expected, failures, label);

    private static uint ReadRamUInt32(BinaryData dol, long ramOffset)
        => dol.ReadUInt32(RamToDolOffset(ramOffset));
}
