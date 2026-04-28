using OrreForge.Colosseum;
using OrreForge.Colosseum.Data;

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

    [Fact]
    public void RoomCatalogMatchesLegacyTreasureRooms()
    {
        Assert.Equal("M2_police_1F", ColosseumRoomCatalog.NameFor(0x10));
        Assert.Equal("Pyrite Town", ColosseumRoomCatalog.MapNameForRoom("M2_police_1F"));
        Assert.Contains(ColosseumRoomCatalog.AllRooms, room => room.Index == 0x10 && room.Name == "M2_police_1F");
    }
}
