using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed class GiftPokemonEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _speciesOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _moveOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _shinyOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _genderOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _natureOptionsByValue;

    private GiftPokemonEditorResources(
        IReadOnlyList<PickerOptionViewModel> speciesOptions,
        IReadOnlyList<PickerOptionViewModel> moveOptions)
    {
        SpeciesOptions = speciesOptions;
        MoveOptions = moveOptions;
        ShinyOptions = [new(0xffff, "Random"), new(0x0000, "Never"), new(0x0001, "Always")];
        GenderOptions = [new(0xff, "Random"), new(0, "Male"), new(1, "Female"), new(2, "Genderless")];
        NatureOptions = TrainerPokemonEditorResources.Empty.NatureOptions;
        _speciesOptionsByValue = speciesOptions.ToDictionary(option => option.Value);
        _moveOptionsByValue = moveOptions.ToDictionary(option => option.Value);
        _shinyOptionsByValue = ShinyOptions.ToDictionary(option => option.Value);
        _genderOptionsByValue = GenderOptions.ToDictionary(option => option.Value);
        _natureOptionsByValue = NatureOptions.ToDictionary(option => option.Value);
    }

    public static GiftPokemonEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")]);

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> MoveOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ShinyOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> GenderOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> NatureOptions { get; }

    public IReadOnlyList<int> LevelOptions { get; } = Enumerable.Range(0, 101).ToArray();

    public static GiftPokemonEditorResources FromCommonRel(ColosseumCommonRel commonRel)
        => new(
            commonRel.PokemonStats.Select(pokemon => new PickerOptionViewModel(pokemon.Index, pokemon.Index == 0 ? "-" : pokemon.Name)).ToArray(),
            commonRel.Moves.Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name)).ToArray());

    public static GiftPokemonEditorResources FromRows(
        IReadOnlyList<ColosseumPokemonStats> pokemonRows,
        IReadOnlyList<ColosseumMove> moveRows)
        => new(
            pokemonRows.Select(pokemon => new PickerOptionViewModel(pokemon.Index, pokemon.Index == 0 ? "-" : pokemon.Name)).ToArray(),
            moveRows.Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name)).ToArray());

    public PickerOptionViewModel SpeciesOption(int value)
        => OptionFor(_speciesOptionsByValue, value, value == 0 ? "-" : $"Pokemon {value}");

    public PickerOptionViewModel MoveOption(int value)
        => OptionFor(_moveOptionsByValue, value, value == 0 ? "-" : $"Move {value}");

    public PickerOptionViewModel ShinyOption(int value)
        => OptionFor(_shinyOptionsByValue, value, $"Shiny {value}");

    public PickerOptionViewModel GenderOption(int value)
        => OptionFor(_genderOptionsByValue, value, $"Gender {value}");

    public PickerOptionViewModel NatureOption(int value)
        => OptionFor(_natureOptionsByValue, value, $"Nature {value}");

    private static PickerOptionViewModel OptionFor(IReadOnlyDictionary<int, PickerOptionViewModel> options, int value, string fallbackName)
        => options.TryGetValue(value, out var option) ? option : new PickerOptionViewModel(value, fallbackName);
}
