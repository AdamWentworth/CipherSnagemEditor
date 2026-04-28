namespace CipherSnagemEditor.Colosseum;

public static class ColosseumToolCatalog
{
    public static readonly IReadOnlyList<ColosseumToolDefinition> HomeTools =
    [
        new("Trainer Editor", "toTrainerVC", "CTTrainerViewController.swift"),
        new("Pokemon Stats Editor", "toStatsVC", "GoDStatsViewController.swift"),
        new("Move Editor", "toMoveVC", "GoDMovesViewController.swift"),
        new("Item Editor", "toItemVC", "GoDItemViewController.swift"),
        new("Gift Pokemon Editor", "toGiftVC", "GoDGiftViewController.swift"),
        new("Type Editor", "toTypeVC", "GoDTypeViewController.swift"),
        new("Treasure Editor", "toTreasureVC", "GoDTreasureViewController.swift"),
        new("Patches", "toPatchVC", "GoDPatchViewController.swift"),
        new("Randomizer", "toRandomiserVC", "GoDRandomiserViewController.swift"),
        new("Message Editor", "toMessageVC", "GoDMessageViewController.swift"),
        new("Collision Viewer", "toCollisionVC", "GoDCollisionViewController.swift"),
        new("Interaction Editor", "toInteractionVC", "GoDInteractionViewController.swift"),
        new("Vertex Filters", "toFiltersVC", "GoDFiltersViewController.swift"),
        new("Table Editor", "toUniversalVC", "UniversalEditorViewController.swift"),
        new("ISO Explorer", "toISOVC", "GoDISOViewController.swift")
    ];
}
