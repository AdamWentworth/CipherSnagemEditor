using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.XD;

namespace CipherSnagemEditor.Tests;

public sealed class XdScaffoldTests
{
    [Fact]
    public void DetectsXdIsoRegionAndLegacyWorkspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Pokemon XD.iso");
        File.WriteAllBytes(isoPath, CreateMinimalIso("GXXE"));

        var iso = GameCubeIsoReader.Open(isoPath);

        Assert.Equal(GameCubeGame.PokemonXD, iso.Game);
        Assert.True(iso.IsPokemonXD);
        Assert.Equal(GameCubeRegion.UnitedStates, iso.Region);
        Assert.Equal("GoD Tool", iso.LegacyToolName);
        Assert.Equal(Path.Combine(temp, "Pokemon XD GoD Tool"), iso.WorkspaceDirectory);
    }

    [Fact]
    public void OpensMinimalXdIsoAndCreatesLegacyWorkspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        File.WriteAllBytes(isoPath, CreateMinimalIso("GXXP"));

        var context = XdProjectContext.Open(isoPath);

        Assert.Equal(XdSourceKind.Iso, context.SourceKind);
        Assert.Equal("GXXP", context.Iso.GameId);
        Assert.Equal(GameCubeRegion.Europe, context.Iso.Region);
        Assert.Equal(Path.Combine(temp, "Sample GoD Tool"), context.WorkspaceDirectory);
        Assert.True(Directory.Exists(context.WorkspaceDirectory));
        Assert.True(File.Exists(Path.Combine(context.WorkspaceDirectory, "Settings.json")));
    }

    [Fact]
    public void XdHomeToolsMatchLegacySwiftOrder()
    {
        var names = XdToolCatalog.HomeTools.Select(tool => tool.Title).ToArray();

        Assert.Equal(
            [
                "Trainer Editor",
                "Shadow Pokemon Editor",
                "Pokemon Stats Editor",
                "Move Editor",
                "Item Editor",
                "Pokespot Editor",
                "Gift Pokemon Editor",
                "Type Editor",
                "Treasure Editor",
                "Patches",
                "Randomizer",
                "Message Editor",
                "Script Compiler",
                "Collision Viewer",
                "Interaction Editor",
                "Vertex Filters",
                "Table Editor",
                "ISO Explorer"
            ],
            names);
    }

    [Fact]
    public void ColosseumWorkspaceNameRemainsCmTool()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Pokemon Colosseum.iso");
        File.WriteAllBytes(isoPath, CreateMinimalIso("GC6E"));

        var iso = GameCubeIsoReader.Open(isoPath);

        Assert.Equal(GameCubeGame.PokemonColosseum, iso.Game);
        Assert.Equal("Colosseum Tool", iso.LegacyToolName);
        Assert.Equal(Path.Combine(temp, "Pokemon Colosseum CM Tool"), iso.WorkspaceDirectory);
    }

    private static byte[] CreateMinimalIso(string gameId)
    {
        var bytes = new byte[0x500];
        for (var index = 0; index < gameId.Length; index++)
        {
            bytes[index] = (byte)gameId[index];
        }

        BigEndian.WriteUInt32(bytes, 0x420, 0x440);
        BigEndian.WriteUInt32(bytes, 0x424, 0x460);
        BigEndian.WriteUInt32(bytes, 0x428, 0x0c);
        return bytes;
    }
}
