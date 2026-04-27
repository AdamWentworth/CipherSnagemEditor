using OrreForge.Colosseum;

namespace OrreForge.Tests;

public sealed class ColosseumCatalogTests
{
    [Fact]
    public void HomeToolsMatchLegacyOrder()
    {
        var names = ColosseumToolCatalog.HomeTools.Select(tool => tool.Title).ToArray();

        Assert.Equal(
        [
            "Trainer Editor",
            "Pokemon Stats Editor",
            "Move Editor",
            "Item Editor",
            "Gift Pokemon Editor",
            "Type Editor",
            "Treasure Editor",
            "Patches",
            "Randomizer",
            "Message Editor",
            "Collision Viewer",
            "Interaction Editor",
            "Vertex Filters",
            "Table Editor",
            "ISO Explorer"
        ], names);
    }
}
