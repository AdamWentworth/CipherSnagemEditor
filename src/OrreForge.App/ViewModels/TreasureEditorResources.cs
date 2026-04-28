using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed class TreasureEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _modelOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _roomOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _itemOptionsByValue;

    private TreasureEditorResources(
        IReadOnlyList<PickerOptionViewModel> modelOptions,
        IReadOnlyList<PickerOptionViewModel> roomOptions,
        IReadOnlyList<PickerOptionViewModel> itemOptions)
    {
        ModelOptions = modelOptions;
        RoomOptions = roomOptions;
        ItemOptions = itemOptions;
        _modelOptionsByValue = modelOptions.ToDictionary(option => option.Value);
        _roomOptionsByValue = roomOptions.ToDictionary(option => option.Value);
        _itemOptionsByValue = itemOptions.ToDictionary(option => option.Value);
    }

    public static TreasureEditorResources Empty { get; } = new(
        BuildModelOptions(),
        [new PickerOptionViewModel(0, "Room 0")],
        [new PickerOptionViewModel(0, "-")]);

    public IReadOnlyList<PickerOptionViewModel> ModelOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> RoomOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ItemOptions { get; }

    public static TreasureEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var treasures = commonRel.Treasures;
        var roomOptions = treasures
            .Select(treasure => new PickerOptionViewModel(treasure.RoomId, treasure.RoomName))
            .GroupBy(option => option.Value)
            .Select(group => group.First())
            .OrderBy(option => option.Value)
            .DefaultIfEmpty(new PickerOptionViewModel(0, "Room 0"))
            .ToArray();
        var itemOptions = commonRel.Items.Count == 0
            ? [new PickerOptionViewModel(0, "-")]
            : commonRel.Items
                .Select(item => new PickerOptionViewModel(item.Index, item.Index == 0 ? "-" : item.Name))
                .ToArray();

        return new TreasureEditorResources(BuildModelOptions(), roomOptions, itemOptions);
    }

    public PickerOptionViewModel ModelOption(int value)
        => OptionFor(_modelOptionsByValue, value, $"Model {value}");

    public PickerOptionViewModel RoomOption(int value)
        => OptionFor(_roomOptionsByValue, value, $"Room {value}");

    public PickerOptionViewModel ItemOption(int value)
        => OptionFor(_itemOptionsByValue, value, value == 0 ? "-" : $"Item {value}");

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildModelOptions()
        =>
        [
            new(0, "None"),
            new(0x24, "Treasure Box"),
            new(0x44, "Sparkle")
        ];
}
