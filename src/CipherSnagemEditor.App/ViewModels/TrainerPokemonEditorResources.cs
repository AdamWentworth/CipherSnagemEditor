using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed class TrainerPokemonEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _speciesOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _itemOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _moveOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _shadowOptionsByValue;
    private readonly IReadOnlyDictionary<int, ColosseumPokemonStats> _pokemonStatsByValue;
    private readonly IReadOnlyDictionary<int, ColosseumMove> _movesByValue;
    private readonly IReadOnlyDictionary<int, ColosseumShadowPokemonData> _shadowDataByValue;

    private TrainerPokemonEditorResources(
        IReadOnlyList<PickerOptionViewModel> speciesOptions,
        IReadOnlyList<PickerOptionViewModel> itemOptions,
        IReadOnlyList<PickerOptionViewModel> pokeballOptions,
        IReadOnlyList<PickerOptionViewModel> moveOptions,
        IReadOnlyList<PickerOptionViewModel> natureOptions,
        IReadOnlyList<PickerOptionViewModel> genderOptions,
        IReadOnlyList<PickerOptionViewModel> shadowOptions,
        IReadOnlyDictionary<int, ColosseumPokemonStats> pokemonStatsByValue,
        IReadOnlyDictionary<int, ColosseumMove> movesByValue,
        IReadOnlyDictionary<int, ColosseumShadowPokemonData> shadowDataByValue)
    {
        SpeciesOptions = speciesOptions;
        ItemOptions = itemOptions;
        PokeballOptions = pokeballOptions;
        MoveOptions = moveOptions;
        NatureOptions = natureOptions;
        GenderOptions = genderOptions;
        ShadowOptions = shadowOptions;
        _pokemonStatsByValue = pokemonStatsByValue;
        _movesByValue = movesByValue;
        _shadowDataByValue = shadowDataByValue;
        _speciesOptionsByValue = speciesOptions.ToDictionary(option => option.Value);
        _itemOptionsByValue = itemOptions.ToDictionary(option => option.Value);
        _moveOptionsByValue = moveOptions.ToDictionary(option => option.Value);
        _shadowOptionsByValue = shadowOptions.ToDictionary(option => option.Value);
    }

    public static TrainerPokemonEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        BuildNatureOptions(),
        BuildGenderOptions(),
        [new PickerOptionViewModel(0, "Shadow ID 0")],
        new Dictionary<int, ColosseumPokemonStats>(),
        new Dictionary<int, ColosseumMove>(),
        new Dictionary<int, ColosseumShadowPokemonData>());

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ItemOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> PokeballOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> MoveOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> NatureOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> GenderOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ShadowOptions { get; }

    public static TrainerPokemonEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var pokemonStats = commonRel.PokemonStats.ToDictionary(pokemon => pokemon.Index);
        var moves = commonRel.Moves.ToDictionary(move => move.Index);
        var shadowData = Enumerable.Range(0, commonRel.ShadowPokemonCount)
            .Select(index => commonRel.ShadowDataById(index))
            .OfType<ColosseumShadowPokemonData>()
            .ToDictionary(shadow => shadow.Index);
        var speciesOptions = commonRel.PokemonStats
            .Select(pokemon => new PickerOptionViewModel(pokemon.Index, pokemon.Index == 0 ? "-" : pokemon.Name))
            .ToArray();
        var itemOptions = commonRel.Items.Count == 0
            ? [new PickerOptionViewModel(0, "-")]
            : commonRel.Items
                .Select(item => new PickerOptionViewModel(item.Index, item.Index == 0 ? "-" : item.Name))
                .ToArray();
        var pokeballOptions = itemOptions.Where(IsPokeballOption).ToArray();
        if (pokeballOptions.Length == 0)
        {
            pokeballOptions = itemOptions;
        }

        var moveOptions = commonRel.Moves
            .Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name))
            .ToArray();
        var shadowOptions = Enumerable.Range(0, commonRel.ShadowPokemonCount)
            .Select(index =>
            {
                var shadow = commonRel.ShadowDataById(index);
                var name = index == 0 ? "Shadow ID 0" : shadow?.SpeciesId > 0
                    ? $"Shadow {commonRel.PokemonStatsFor(shadow.SpeciesId)?.Name ?? shadow.SpeciesId.ToString()}"
                    : $"Shadow ID {index}";
                return new PickerOptionViewModel(index, name);
            })
            .ToArray();

        return new TrainerPokemonEditorResources(
            speciesOptions,
            itemOptions,
            pokeballOptions,
            moveOptions,
            BuildNatureOptions(),
            BuildGenderOptions(),
            shadowOptions,
            pokemonStats,
            moves,
            shadowData);
    }

    public static TrainerPokemonEditorResources FromRows(
        IReadOnlyList<ColosseumPokemonStats> pokemonStatsRows,
        IReadOnlyList<ColosseumMove> moveRows,
        IReadOnlyList<ColosseumItem> itemRows,
        IReadOnlyList<ColosseumShadowPokemonData> shadowRows)
    {
        var pokemonStats = pokemonStatsRows.ToDictionary(pokemon => pokemon.Index);
        var moves = moveRows.ToDictionary(move => move.Index);
        var shadowData = shadowRows.ToDictionary(shadow => shadow.Index);
        var speciesOptions = pokemonStatsRows
            .Select(pokemon => new PickerOptionViewModel(pokemon.Index, pokemon.Index == 0 ? "-" : pokemon.Name))
            .ToArray();
        var itemOptions = itemRows.Count == 0
            ? [new PickerOptionViewModel(0, "-")]
            : itemRows
                .Select(item => new PickerOptionViewModel(item.Index, item.Index == 0 ? "-" : item.Name))
                .ToArray();
        var pokeballOptions = itemOptions.Where(IsPokeballOption).ToArray();
        if (pokeballOptions.Length == 0)
        {
            pokeballOptions = itemOptions;
        }

        var moveOptions = moveRows
            .Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name))
            .ToArray();
        var shadowOptions = shadowRows.Count == 0
            ? [new PickerOptionViewModel(0, "Shadow ID 0")]
            : shadowRows
                .Prepend(new ColosseumShadowPokemonData(0, 0, 0, 0, 0, 0))
                .Select(shadow =>
                {
                    var name = shadow.Index == 0 ? "Shadow ID 0" : shadow.SpeciesId > 0
                        ? $"Shadow {(pokemonStats.TryGetValue(shadow.SpeciesId, out var pokemon) ? pokemon.Name : shadow.SpeciesId)}"
                        : $"Shadow ID {shadow.Index}";
                    return new PickerOptionViewModel(shadow.Index, name);
                })
                .GroupBy(option => option.Value)
                .Select(group => group.First())
                .ToArray();

        return new TrainerPokemonEditorResources(
            speciesOptions,
            itemOptions,
            pokeballOptions,
            moveOptions,
            BuildNatureOptions(),
            BuildGenderOptions(),
            shadowOptions,
            pokemonStats,
            moves,
            shadowData);
    }

    public PickerOptionViewModel SpeciesOption(int value)
        => OptionFor(_speciesOptionsByValue, SpeciesOptions, value, value == 0 ? "-" : $"Pokemon {value}");

    public PickerOptionViewModel ItemOption(int value)
        => OptionFor(_itemOptionsByValue, ItemOptions, value, value == 0 ? "-" : $"Item {value}");

    public PickerOptionViewModel PokeballOption(int value)
        => PokeballOptions.FirstOrDefault(option => option.Value == value)
            ?? ItemOption(value);

    public PickerOptionViewModel MoveOption(int value)
        => OptionFor(_moveOptionsByValue, MoveOptions, value, value == 0 ? "-" : $"Move {value}");

    public PickerOptionViewModel NatureOption(int value)
        => NatureOptions.FirstOrDefault(option => option.Value == value) ?? new PickerOptionViewModel(value, $"Nature {value}");

    public PickerOptionViewModel GenderOption(int value)
        => GenderOptions.FirstOrDefault(option => option.Value == value) ?? new PickerOptionViewModel(value, $"Gender {value}");

    public PickerOptionViewModel ShadowOption(int value)
        => OptionFor(_shadowOptionsByValue, ShadowOptions, value, $"Shadow ID {value}");

    public ColosseumPokemonStats? PokemonStats(int value)
        => _pokemonStatsByValue.TryGetValue(value, out var pokemon) ? pokemon : null;

    public ColosseumMove? Move(int value)
        => _movesByValue.TryGetValue(value, out var move) ? move : null;

    public ColosseumShadowPokemonData? ShadowData(int value)
        => _shadowDataByValue.TryGetValue(value, out var shadow) ? shadow : null;

    private static bool IsPokeballOption(PickerOptionViewModel option)
        => option.Value == 0 || option.Name.Contains("BALL", StringComparison.OrdinalIgnoreCase);

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        IReadOnlyList<PickerOptionViewModel> fallbackOptions,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildNatureOptions()
        =>
        [
            new(0x00, "Hardy"),
            new(0x01, "Lonely"),
            new(0x02, "Brave"),
            new(0x03, "Adamant"),
            new(0x04, "Naughty"),
            new(0x05, "Bold"),
            new(0x06, "Docile"),
            new(0x07, "Relaxed"),
            new(0x08, "Impish"),
            new(0x09, "Lax"),
            new(0x0a, "Timid"),
            new(0x0b, "Hasty"),
            new(0x0c, "Serious"),
            new(0x0d, "Jolly"),
            new(0x0e, "Naive"),
            new(0x0f, "Modest"),
            new(0x10, "Mild"),
            new(0x11, "Quiet"),
            new(0x12, "Bashful"),
            new(0x13, "Rash"),
            new(0x14, "Calm"),
            new(0x15, "Gentle"),
            new(0x16, "Sassy"),
            new(0x17, "Careful"),
            new(0x18, "Quirky"),
            new(0xff, "Random")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildGenderOptions()
        =>
        [
            new(0x00, "Male"),
            new(0x01, "Female"),
            new(0x02, "Genderless"),
            new(0xff, "Random")
        ];
}
