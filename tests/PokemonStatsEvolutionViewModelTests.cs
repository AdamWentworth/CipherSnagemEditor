using CipherSnagemEditor.App.ViewModels;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.Tests;

public sealed class PokemonStatsEvolutionViewModelTests
{
    [Fact]
    public void LevelEvolutionConditionIsSelectedFromDropdownOptions()
    {
        var resources = PokemonStatsEditorResources.FromRows(
            [Pokemon(1, "BULBASAUR"), Pokemon(2, "IVYSAUR")],
            [Move(0, "-")],
            [Item(0, "-")],
            [Type(0, "NORMAL")]);
        var evolution = new ColosseumPokemonEvolution(
            0,
            0x04,
            "Level Up",
            16,
            "Lv. 16",
            2,
            "IVYSAUR");

        var viewModel = new PokemonStatsEvolutionViewModel(evolution, resources, changed: null);

        Assert.NotNull(viewModel.SelectedCondition);
        Assert.Equal("Lv. 16", viewModel.SelectedCondition.Name);
        Assert.Contains(viewModel.SelectedCondition, viewModel.ConditionOptions);
    }

    private static ColosseumPokemonStats Pokemon(int index, string name)
        => new(
            index,
            0,
            name,
            0,
            index,
            0,
            "Standard",
            0x7f,
            "50% Male",
            0,
            70,
            0,
            0,
            0,
            "NORMAL",
            0,
            "NORMAL",
            0,
            "Ability 0",
            0,
            "Ability 0",
            0,
            "-",
            0,
            "-",
            45,
            1,
            1,
            1,
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            [],
            [],
            []);

    private static ColosseumMove Move(int index, string name)
        => new(
            index,
            0,
            name,
            0,
            "-",
            0,
            0,
            "NORMAL",
            0,
            "Selected Target",
            0,
            "Physical",
            0,
            0,
            0,
            "-",
            0,
            "Attack",
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false);

    private static ColosseumItem Item(int index, string name)
        => new(
            index,
            0,
            name,
            0,
            "-",
            0,
            0,
            "Items",
            true,
            0,
            0,
            0,
            0,
            0,
            [],
            0,
            0,
            "-");

    private static ColosseumTypeData Type(int index, string name)
        => new(index, 0, name, 0, 0, "Physical", []);
}
