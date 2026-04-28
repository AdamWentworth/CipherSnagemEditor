using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed class ItemEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _bagSlotOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _moveOptionsByValue;

    private ItemEditorResources(
        IReadOnlyList<PickerOptionViewModel> bagSlotOptions,
        IReadOnlyList<PickerOptionViewModel> moveOptions)
    {
        BagSlotOptions = bagSlotOptions;
        MoveOptions = moveOptions;
        _bagSlotOptionsByValue = bagSlotOptions.ToDictionary(option => option.Value);
        _moveOptionsByValue = moveOptions.ToDictionary(option => option.Value);
    }

    public static ItemEditorResources Empty { get; } = new(
        BuildBagSlotOptions(),
        [new PickerOptionViewModel(0, "-")]);

    public IReadOnlyList<PickerOptionViewModel> BagSlotOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> MoveOptions { get; }

    public static ItemEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var moveOptions = commonRel.Moves
            .Select(move => new PickerOptionViewModel(move.Index, move.Index == 0 ? "-" : move.Name))
            .ToArray();

        return new ItemEditorResources(BuildBagSlotOptions(), moveOptions);
    }

    public PickerOptionViewModel BagSlotOption(int value)
        => OptionFor(_bagSlotOptionsByValue, value, $"Pocket {value}");

    public PickerOptionViewModel MoveOption(int value)
        => OptionFor(_moveOptionsByValue, value, value == 0 ? "-" : $"Move {value}");

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildBagSlotOptions()
        =>
        [
            new(0, "None"),
            new(1, "Pokeballs"),
            new(2, "Items"),
            new(3, "Berries"),
            new(4, "TMs"),
            new(5, "Key Items"),
            new(6, "Colognes")
        ];
}
