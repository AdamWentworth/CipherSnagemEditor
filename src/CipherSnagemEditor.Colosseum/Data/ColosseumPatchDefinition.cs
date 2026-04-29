namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumPatchDefinition(ColosseumPatchKind Kind, string Name)
{
    public static IReadOnlyList<ColosseumPatchDefinition> ColosseumPatches { get; } =
    [
        new(ColosseumPatchKind.PhysicalSpecialSplitApply, "Apply the gen IV physical/special split and set moves to their default category"),
        new(ColosseumPatchKind.DisableSaveCorruption, "Disables some save file checks to prevent the save from being corrupted"),
        new(ColosseumPatchKind.AddSoftReset, "Adds the ability to soft reset using B + X + Start button combo"),
        new(ColosseumPatchKind.LoadPcFromAnywhere, "Press R in the overworld to open the PC menu from anywhere (Make sure you don't softlock yourself)"),
        new(ColosseumPatchKind.RemoveShinyLocksFromGiftPokemon, "Remove shiny locks from gift pokemon (espeon, umbreon, plusle, pikachu, celebi, hooh)"),
        new(ColosseumPatchKind.AllowFemaleStarters, "Allow starter pokemon to be female"),
        new(ColosseumPatchKind.InfiniteTms, "TMs can be reused infinitely"),
        new(ColosseumPatchKind.Gen6CritMultipliers, "Gen 6+ critical hit multiplier (1.5x)"),
        new(ColosseumPatchKind.Gen7CritRatios, "Gen 7+ critical hit probablities"),
        new(ColosseumPatchKind.TradeEvolutions, "Trade evolutions become level 40"),
        new(ColosseumPatchKind.RemoveItemEvolutions, "Evolution stone evolutions become level 40"),
        new(ColosseumPatchKind.AllowShinyStarters, "Starter pokemon can be shiny"),
        new(ColosseumPatchKind.ShinyLockStarters, "Starter pokemon can never be shiny"),
        new(ColosseumPatchKind.AlwaysShinyStarters, "Starter pokemon are always shiny"),
        new(ColosseumPatchKind.EnableDebugLogs, "Enable Debug Logs (Only useful for script development)"),
        new(ColosseumPatchKind.NoTypeIconForLockedMoves, "When a shadow pokemon has locked moves the move doesn't show the ??? type icon"),
        new(ColosseumPatchKind.PokemonCanLearnAnyTm, "Any pokemon can learn any TM"),
        new(ColosseumPatchKind.PokemonHaveMaxCatchRate, "All pokemon have the maximum catch rate of 255"),
        new(ColosseumPatchKind.AllSingleBattles, "Set all battles to single battles"),
        new(ColosseumPatchKind.AllDoubleBattles, "Set all battles to double battles"),
        new(ColosseumPatchKind.RemoveColbtlRegionLock, "Modify the ASM so it allows any region's colbtl.bin to be imported. Trades will be locked to whichever region's colbtl.bin was imported")
    ];

    public static ColosseumPatchDefinition ForKind(ColosseumPatchKind kind)
        => ColosseumPatches.First(patch => patch.Kind == kind);
}
