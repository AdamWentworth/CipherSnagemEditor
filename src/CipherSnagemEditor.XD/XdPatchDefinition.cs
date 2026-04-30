namespace CipherSnagemEditor.XD;

public enum XdPatchKind
{
    PurgeUnusedText,
    PhysicalSpecialSplitApply,
    PhysicalSpecialSplitRemove,
    DisableSaveCorruption,
    InfiniteTms,
    ExpAll,
    AllowFemaleStarters,
    BetaStartersApply,
    BetaStartersRemove,
    FixShinyGlitch,
    ReplaceShinyGlitch,
    AllowShinyShadowPokemon,
    ShinyLockShadowPokemon,
    AlwaysShinyShadowPokemon,
    Gen6CritMultipliers,
    Gen7CritRatios,
    TradeEvolutions,
    RemoveItemEvolutions,
    EnableDebugLogs,
    PokemonCanLearnAnyTm,
    PokemonHaveMaxCatchRate,
    RemoveEvCap,
    AllSingleBattles,
    AllDoubleBattles,
    Type9IndependentApply,
    MaxPokespotEntries,
    PreventPokemonRelease,
    CompleteStrategyMemo,
    DisableBattleAnimations
}

public sealed record XdPatchDefinition(XdPatchKind Kind, string Name)
{
    public static IReadOnlyList<XdPatchDefinition> XdPatches { get; } =
    [
        new(XdPatchKind.PurgeUnusedText, "Removes foreign language text from the US version"),
        new(XdPatchKind.PhysicalSpecialSplitApply, "Apply the gen IV physical/special split and set moves to their default category"),
        new(XdPatchKind.PhysicalSpecialSplitRemove, "Remove the physical/special split"),
        new(XdPatchKind.DisableSaveCorruption, "Disables some save file checks to prevent the save from being corrupted"),
        new(XdPatchKind.InfiniteTms, "TMs can be reused infinitely"),
        new(XdPatchKind.ExpAll, "All party pokemon gain exp without having to battle"),
        new(XdPatchKind.AllowFemaleStarters, "Allow starter pokemon to be female"),
        new(XdPatchKind.BetaStartersApply, "Allows the player to start with 2 pokemon"),
        new(XdPatchKind.BetaStartersRemove, "Revert to starting with 1 pokemon"),
        new(XdPatchKind.FixShinyGlitch, "Fix shiny glitch"),
        new(XdPatchKind.ReplaceShinyGlitch, "Return to the default shiny glitch behaviour"),
        new(XdPatchKind.AllowShinyShadowPokemon, "Shadow pokemon can be shiny"),
        new(XdPatchKind.ShinyLockShadowPokemon, "Shadow pokemon can never be shiny"),
        new(XdPatchKind.AlwaysShinyShadowPokemon, "Shadow pokemon are always shiny"),
        new(XdPatchKind.Gen6CritMultipliers, "Gen 6+ critical hit multiplier (1.5x)"),
        new(XdPatchKind.Gen7CritRatios, "Gen 7+ critical hit probablities"),
        new(XdPatchKind.TradeEvolutions, "Trade evolutions become level 40"),
        new(XdPatchKind.RemoveItemEvolutions, "Evolution stone evolutions become level 40"),
        new(XdPatchKind.EnableDebugLogs, "Enable Debug Logs (Only useful for script development)"),
        new(XdPatchKind.PokemonCanLearnAnyTm, "Any pokemon can learn any TM"),
        new(XdPatchKind.PokemonHaveMaxCatchRate, "All pokemon have the maximum catch rate of 255"),
        new(XdPatchKind.RemoveEvCap, "Allow pokemon to have an EV total above 510"),
        new(XdPatchKind.AllSingleBattles, "Set all battles to single battles"),
        new(XdPatchKind.AllDoubleBattles, "Set all battles to double battles"),
        new(XdPatchKind.Type9IndependentApply, "Makes the battle engine treat the ??? type as regular type"),
        new(XdPatchKind.MaxPokespotEntries, "Change the max number of pokemon per pokespot from 3 to 100"),
        new(XdPatchKind.PreventPokemonRelease, "Disables the ability to release Pokemon"),
        new(XdPatchKind.CompleteStrategyMemo, "All pokemon will be viewable in the strategy memo immediately"),
        new(XdPatchKind.DisableBattleAnimations, "Disable attack animations during battles")
    ];

    public static XdPatchDefinition ForKind(XdPatchKind kind)
        => XdPatches.First(patch => patch.Kind == kind);
}
