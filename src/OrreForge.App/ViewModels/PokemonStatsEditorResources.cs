using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed class PokemonStatsEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _typeOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _abilityOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _itemOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _expRateOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _genderRatioOptionsByValue;

    private PokemonStatsEditorResources(
        IReadOnlyList<PickerOptionViewModel> typeOptions,
        IReadOnlyList<PickerOptionViewModel> abilityOptions,
        IReadOnlyList<PickerOptionViewModel> itemOptions,
        IReadOnlyList<PickerOptionViewModel> expRateOptions,
        IReadOnlyList<PickerOptionViewModel> genderRatioOptions)
    {
        TypeOptions = typeOptions;
        AbilityOptions = abilityOptions;
        ItemOptions = itemOptions;
        ExpRateOptions = expRateOptions;
        GenderRatioOptions = genderRatioOptions;
        _typeOptionsByValue = typeOptions.ToDictionary(option => option.Value);
        _abilityOptionsByValue = abilityOptions.ToDictionary(option => option.Value);
        _itemOptionsByValue = itemOptions.ToDictionary(option => option.Value);
        _expRateOptionsByValue = expRateOptions.ToDictionary(option => option.Value);
        _genderRatioOptionsByValue = genderRatioOptions.ToDictionary(option => option.Value);
    }

    public static PokemonStatsEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "Normal")],
        [new PickerOptionViewModel(0, "Ability 0")],
        [new PickerOptionViewModel(0, "-")],
        BuildExpRateOptions(),
        BuildGenderRatioOptions());

    public IReadOnlyList<PickerOptionViewModel> TypeOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> AbilityOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ItemOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ExpRateOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> GenderRatioOptions { get; }

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

        return new PokemonStatsEditorResources(
            typeOptions,
            abilityOptions,
            itemOptions,
            BuildExpRateOptions(),
            BuildGenderRatioOptions());
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
}
