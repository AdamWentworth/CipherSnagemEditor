namespace CipherSnagemEditor.XD;

public static class XdToolCatalog
{
    public static readonly IReadOnlyList<XdToolDefinition> HomeTools =
    [
        new("Trainer Editor", "toTrainerVC", "GoDTrainerViewController.swift"),
        new("Shadow Pokemon Editor", "toShadowVC", "GoDShadowPokemonViewController.swift"),
        new("Pokemon Stats Editor", "toStatsVC", "GoDStatsViewController.swift"),
        new("Move Editor", "toMoveVC", "GoDMovesViewController.swift"),
        new("Item Editor", "toItemVC", "GoDItemViewController.swift"),
        new("Pokespot Editor", "toSpotVC", "GoDSpotViewController.swift"),
        new("Gift Pokemon Editor", "toGiftVC", "GoDGiftViewController.swift"),
        new("Type Editor", "toTypeVC", "GoDTypeViewController.swift"),
        new("Treasure Editor", "toTreasureVC", "GoDTreasureViewController.swift"),
        new("Patches", "toPatchVC", "GoDPatchViewController.swift"),
        new("Randomizer", "toRandomiserVC", "GoDRandomiserViewController.swift"),
        new("Message Editor", "toMessageVC", "GoDMessageViewController.swift"),
        new("Script Compiler", "toScriptVC", "GoDScriptViewController.swift"),
        new("Collision Viewer", "toCollisionVC", "GoDCollisionViewController.swift"),
        new("Interaction Editor", "toInteractionVC", "GoDInteractionViewController.swift"),
        new("Vertex Filters", "toFiltersVC", "GoDFiltersViewController.swift"),
        new("Table Editor", "toUniversalVC", "UniversalEditorViewController.swift"),
        new("ISO Explorer", "toISOVC", "GoDISOViewController.swift")
    ];
}
