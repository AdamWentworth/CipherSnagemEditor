namespace CipherSnagemEditor.XD;

public sealed record XdPatchDefinition(string Name)
{
    public static IReadOnlyList<XdPatchDefinition> XdPatches { get; } =
    [
        new("Removes foreign language text from the US version"),
        new("Apply the gen IV physical/special split and set moves to their default category"),
        new("Remove the physical/special split"),
        new("Disables some save file checks to prevent the save from being corrupted"),
        new("TMs can be reused infinitely"),
        new("All party pokemon gain exp without having to battle"),
        new("Allow starter pokemon to be female"),
        new("Allows the player to start with 2 pokemon"),
        new("Revert to starting with 1 pokemon"),
        new("Fix shiny glitch"),
        new("Return to the default shiny glitch behaviour"),
        new("Shadow pokemon can be shiny"),
        new("Shadow pokemon can never be shiny"),
        new("Shadow pokemon are always shiny"),
        new("Gen 6+ critical hit multiplier (1.5x)"),
        new("Gen 7+ critical hit probablities"),
        new("Trade evolutions become level 40"),
        new("Evolution stone evolutions become level 40"),
        new("Enable Debug Logs (Only useful for script development)"),
        new("Any pokemon can learn any TM"),
        new("All pokemon have the maximum catch rate of 255"),
        new("Allow pokemon to have an EV total above 510"),
        new("Set all battles to single battles"),
        new("Set all battles to double battles"),
        new("Makes the battle engine treat the ??? type as regular type"),
        new("Change the max number of pokemon per pokespot from 3 to 100"),
        new("Disables the ability to release Pokemon"),
        new("All pokemon will be viewable in the strategy memo immediately"),
        new("Disable attack animations during battles")
    ];
}
