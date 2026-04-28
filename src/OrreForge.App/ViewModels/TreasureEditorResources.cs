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
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")]);

    public IReadOnlyList<PickerOptionViewModel> ModelOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> RoomOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ItemOptions { get; }

    public static TreasureEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var treasures = commonRel.Treasures;
        var roomOptions = ColosseumRoomCatalog.AllRooms
            .Select(room => new PickerOptionViewModel(room.Index, room.Name))
            .Concat(treasures.Select(treasure => new PickerOptionViewModel(treasure.RoomId, treasure.RoomName)))
            .GroupBy(option => option.Value)
            .Select(group => group.First())
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .Prepend(new PickerOptionViewModel(0, "-"))
            .GroupBy(option => option.Value)
            .Select(group => group.First())
            .ToArray();
        var itemOptions = commonRel.Items.Count == 0
            ? [new PickerOptionViewModel(0, "-")]
            : commonRel.Items
                .Where(item => item.Index == 0 || !string.IsNullOrWhiteSpace(item.Name))
                .SelectMany(item => BuildItemOptions(item.Index, item.Name))
                .Concat(treasures.Select(treasure => new PickerOptionViewModel(
                    treasure.ItemId,
                    treasure.ItemId == 0 ? "-" : $"{treasure.ItemName} ({treasure.ItemId})")))
                .GroupBy(option => option.Value)
                .Select(group => group.First())
                .OrderBy(option => option.Value == 0 ? string.Empty : option.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new TreasureEditorResources(BuildModelOptions(), roomOptions, itemOptions);
    }

    public PickerOptionViewModel ModelOption(int value)
        => _modelOptionsByValue.TryGetValue(value, out var option)
            ? option
            : ModelOptions[0];

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
            new(0, "-"),
            new(0x24, "Chest"),
            new(0x44, "Sparkle")
        ];

    private static IEnumerable<PickerOptionViewModel> BuildItemOptions(int itemIndex, string itemName)
    {
        var label = itemIndex == 0 ? "-" : $"{itemName} ({itemIndex})";
        yield return new PickerOptionViewModel(ScriptIndexForItem(itemIndex), label);

        if (itemIndex >= FirstScriptKeyItemIndex)
        {
            yield return new PickerOptionViewModel(itemIndex, label);
        }
    }

    private const int FirstScriptKeyItemIndex = 0x15e;

    private static int ScriptIndexForItem(int itemIndex)
        => itemIndex >= FirstScriptKeyItemIndex ? itemIndex + 151 : itemIndex;
}
