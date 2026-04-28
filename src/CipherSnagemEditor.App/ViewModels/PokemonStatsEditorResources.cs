using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed class PokemonStatsEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _typeOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _abilityOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _itemOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _expRateOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _genderRatioOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _speciesOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _moveOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _evolutionMethodOptionsByValue;

    private PokemonStatsEditorResources(
        IReadOnlyList<PickerOptionViewModel> typeOptions,
        IReadOnlyList<PickerOptionViewModel> abilityOptions,
        IReadOnlyList<PickerOptionViewModel> itemOptions,
        IReadOnlyList<PickerOptionViewModel> expRateOptions,
        IReadOnlyList<PickerOptionViewModel> genderRatioOptions,
        IReadOnlyList<PickerOptionViewModel> speciesOptions,
        IReadOnlyList<PickerOptionViewModel> moveOptions,
        IReadOnlyList<PickerOptionViewModel> evolutionMethodOptions,
        IReadOnlyList<ColosseumTmMove> tmMoves)
    {
        TypeOptions = typeOptions;
        AbilityOptions = abilityOptions;
        ItemOptions = itemOptions;
        ExpRateOptions = expRateOptions;
        GenderRatioOptions = genderRatioOptions;
        SpeciesOptions = speciesOptions;
        MoveOptions = moveOptions;
        EvolutionMethodOptions = evolutionMethodOptions;
        TmMoves = tmMoves;
        LevelOptions = Enumerable.Range(0, 101).ToArray();
        _typeOptionsByValue = typeOptions.ToDictionary(option => option.Value);
        _abilityOptionsByValue = abilityOptions.ToDictionary(option => option.Value);
        _itemOptionsByValue = itemOptions.ToDictionary(option => option.Value);
        _expRateOptionsByValue = expRateOptions.ToDictionary(option => option.Value);
        _genderRatioOptionsByValue = genderRatioOptions.ToDictionary(option => option.Value);
        _speciesOptionsByValue = speciesOptions.ToDictionary(option => option.Value);
        _moveOptionsByValue = moveOptions.ToDictionary(option => option.Value);
        _evolutionMethodOptionsByValue = evolutionMethodOptions.ToDictionary(option => option.Value);
    }

    public static PokemonStatsEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "Normal")],
        [new PickerOptionViewModel(0, "Ability 0")],
        [new PickerOptionViewModel(0, "-")],
        BuildExpRateOptions(),
        BuildGenderRatioOptions(),
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        BuildEvolutionMethodOptions(),
        []);

    public IReadOnlyList<PickerOptionViewModel> TypeOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> AbilityOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ItemOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ExpRateOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> GenderRatioOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> MoveOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EvolutionMethodOptions { get; }

    public IReadOnlyList<ColosseumTmMove> TmMoves { get; }

    public IReadOnlyList<int> LevelOptions { get; }

    public static PokemonStatsEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var typeOptions = commonRel.Types
            .Select(type => new PickerOptionViewModel(type.Index, type.Name))
            .ToArray();
        var abilityOptions = commonRel.Abilities.Count == 0
            ? [new PickerOptionViewModel(0, "Ability 0")]
            : commonRel.Abilities
                .Select(ability => new PickerOptionViewModel(ability.Index, ability.Name))
                .ToArray();
        var itemOptions = commonRel.Items.Count == 0
            ? [new PickerOptionViewModel(0, "-")]
            : commonRel.Items
                .Select(item => new PickerOptionViewModel(item.Index, item.Index == 0 ? "-" : item.Name))
                .ToArray();
        var speciesOptions = commonRel.PokemonStats
            .Select(pokemon => new PickerOptionViewModel(pokemon.Index, pokemon.Index == 0 ? "-" : pokemon.Name))
            .ToArray();
        var moveOptions = commonRel.Moves
            .Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name))
            .ToArray();

        return new PokemonStatsEditorResources(
            typeOptions,
            abilityOptions,
            itemOptions,
            BuildExpRateOptions(),
            BuildGenderRatioOptions(),
            speciesOptions,
            moveOptions,
            BuildEvolutionMethodOptions(),
            commonRel.TmMoves);
    }

    public PickerOptionViewModel TypeOption(int value)
        => OptionFor(_typeOptionsByValue, value, $"Type {value}");

    public PickerOptionViewModel AbilityOption(int value)
        => OptionFor(_abilityOptionsByValue, value, $"Ability {value}");

    public PickerOptionViewModel ItemOption(int value)
        => OptionFor(_itemOptionsByValue, value, value == 0 ? "-" : $"Item {value}");

    public PickerOptionViewModel ExpRateOption(int value)
        => OptionFor(_expRateOptionsByValue, value, $"Exp Rate {value}");

    public PickerOptionViewModel GenderRatioOption(int value)
        => OptionFor(_genderRatioOptionsByValue, value, $"Gender Ratio {value}");

    public PickerOptionViewModel SpeciesOption(int value)
        => OptionFor(_speciesOptionsByValue, value, value == 0 ? "-" : $"Pokemon {value}");

    public PickerOptionViewModel MoveOption(int value)
        => OptionFor(_moveOptionsByValue, value, value == 0 ? "-" : $"Move {value}");

    public PickerOptionViewModel EvolutionMethodOption(int value)
        => OptionFor(_evolutionMethodOptionsByValue, value, $"Evolution Method {value}");

    public IReadOnlyList<PickerOptionViewModel> EvolutionConditionOptions(int method)
        => EvolutionConditionKind(method) switch
        {
            EvolutionConditionValueKind.Level => LevelOptions.Select(level => new PickerOptionViewModel(level, $"Lv. {level}")).ToArray(),
            EvolutionConditionValueKind.Item => ItemOptions,
            _ => [new PickerOptionViewModel(0, "-")]
        };

    public PickerOptionViewModel EvolutionConditionOption(int method, int condition)
    {
        var options = EvolutionConditionOptions(method);
        return options.FirstOrDefault(option => option.Value == condition)
            ?? new PickerOptionViewModel(condition, condition == 0 ? "-" : condition.ToString());
    }

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildExpRateOptions()
        =>
        [
            new(0, "Standard"),
            new(1, "Very Fast"),
            new(2, "Slowest"),
            new(3, "Slow"),
            new(4, "Fast"),
            new(5, "Very Slow")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildGenderRatioOptions()
        =>
        [
            new(0x00, "Male Only"),
            new(0x1f, "87.5% Male"),
            new(0x3f, "75% Male"),
            new(0x7f, "50% Male"),
            new(0xbf, "75% Female"),
            new(0xdf, "87.5% Female"),
            new(0xfe, "Female Only"),
            new(0xff, "Genderless")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildEvolutionMethodOptions()
        =>
        [
            new(0x00, "None"),
            new(0x01, "Max Happiness"),
            new(0x02, "Happiness (Day)"),
            new(0x03, "Happiness (Night)"),
            new(0x04, "Level Up"),
            new(0x05, "Trade"),
            new(0x06, "Trade With Item"),
            new(0x07, "Evolution Stone"),
            new(0x08, "Atk > Def"),
            new(0x09, "Atk = Def"),
            new(0x0a, "Atk < Def"),
            new(0x0b, "Silcoon evolution method"),
            new(0x0c, "Cascoon evolution method"),
            new(0x0d, "Ninjask evolution method"),
            new(0x0e, "Shedinja evolution method"),
            new(0x0f, "Max Beauty"),
            new(0x10, "Level Up With Key Item"),
            new(0x11, "Evolves in Generation 4 (XG)")
        ];

    private static EvolutionConditionValueKind EvolutionConditionKind(int method)
        => method switch
        {
            0x04 or 0x08 or 0x09 or 0x0a or 0x0b or 0x0c or 0x0d or 0x0e => EvolutionConditionValueKind.Level,
            0x06 or 0x07 or 0x10 => EvolutionConditionValueKind.Item,
            _ => EvolutionConditionValueKind.None
        };

    private enum EvolutionConditionValueKind
    {
        None,
        Level,
        Item
    }
}
