using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.Tests;

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

    [Fact]
    public void PatchListMatchesLegacyColosseumOrder()
    {
        var names = ColosseumPatchDefinition.ColosseumPatches.Select(patch => patch.Name).ToArray();

        Assert.Equal(
        [
            "Apply the gen IV physical/special split and set moves to their default category",
            "Disables some save file checks to prevent the save from being corrupted",
            "Adds the ability to soft reset using B + X + Start button combo",
            "Press R in the overworld to open the PC menu from anywhere (Make sure you don't softlock yourself)",
            "Remove shiny locks from gift pokemon (espeon, umbreon, plusle, pikachu, celebi, hooh)",
            "Allow starter pokemon to be female",
            "TMs can be reused infinitely",
            "Gen 6+ critical hit multiplier (1.5x)",
            "Gen 7+ critical hit probablities",
            "Trade evolutions become level 40",
            "Evolution stone evolutions become level 40",
            "Starter pokemon can be shiny",
            "Starter pokemon can never be shiny",
            "Starter pokemon are always shiny",
            "Enable Debug Logs (Only useful for script development)",
            "When a shadow pokemon has locked moves the move doesn't show the ??? type icon",
            "Any pokemon can learn any TM",
            "All pokemon have the maximum catch rate of 255",
            "Set all battles to single battles",
            "Set all battles to double battles",
            "Modify the ASM so it allows any region's colbtl.bin to be imported. Trades will be locked to whichever region's colbtl.bin was imported"
        ], names);
    }
}
