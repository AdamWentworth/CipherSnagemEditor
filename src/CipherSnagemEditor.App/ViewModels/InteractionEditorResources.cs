using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed class InteractionEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _roomOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _scriptTypeOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _directionOptionsByValue;

    private InteractionEditorResources(
        IReadOnlyList<PickerOptionViewModel> roomOptions,
        IReadOnlyList<PickerOptionViewModel> scriptTypeOptions,
        IReadOnlyList<PickerOptionViewModel> commonScriptOptions,
        IReadOnlyList<PickerOptionViewModel> currentScriptOptions,
        IReadOnlyList<PickerOptionViewModel> directionOptions)
    {
        RoomOptions = roomOptions;
        ScriptTypeOptions = scriptTypeOptions;
        CommonScriptOptions = commonScriptOptions;
        CurrentScriptOptions = currentScriptOptions;
        DirectionOptions = directionOptions;
        _roomOptionsByValue = roomOptions.GroupBy(option => option.Value).ToDictionary(group => group.Key, group => group.First());
        _scriptTypeOptionsByValue = scriptTypeOptions.ToDictionary(option => option.Value);
        _directionOptionsByValue = directionOptions.ToDictionary(option => option.Value);
    }

    public static InteractionEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "-")],
        BuildScriptTypeOptions(),
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        BuildDirectionOptions());

    public IReadOnlyList<PickerOptionViewModel> RoomOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> ScriptTypeOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> CommonScriptOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> CurrentScriptOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> DirectionOptions { get; }

    public static InteractionEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var interactionRooms = commonRel.InteractionPoints
            .Select(point => new PickerOptionViewModel(point.RoomId, point.RoomName))
            .Concat(commonRel.InteractionPoints.Select(point => new PickerOptionViewModel(point.TargetRoomId, point.TargetRoomName)));
        var roomOptions = ColosseumRoomCatalog.AllRooms
            .Select(room => new PickerOptionViewModel(room.Index, room.Name))
            .Concat(interactionRooms)
            .Where(option => option.Value > 0 || option.Name == "-")
            .GroupBy(option => option.Value)
            .Select(group => group.First())
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .Prepend(new PickerOptionViewModel(0, "-"))
            .GroupBy(option => option.Value)
            .Select(group => group.First())
            .ToArray();

        return new InteractionEditorResources(
            roomOptions,
            BuildScriptTypeOptions(),
            BuildCommonScriptOptions(),
            BuildCurrentScriptOptions(),
            BuildDirectionOptions());
    }

    public PickerOptionViewModel RoomOption(int value)
        => OptionFor(_roomOptionsByValue, value, value == 0 ? "-" : ColosseumRoomCatalog.NameFor(value));

    public PickerOptionViewModel ScriptTypeOption(int value)
        => OptionFor(_scriptTypeOptionsByValue, value, "-");

    public PickerOptionViewModel DirectionOption(int value)
        => OptionFor(_directionOptionsByValue, value, value == 1 ? "Down" : "Up");

    public IReadOnlyList<PickerOptionViewModel> ScriptOptionsFor(int scriptType)
        => scriptType switch
        {
            1 => CommonScriptOptions,
            2 => CurrentScriptOptions,
            _ => [new PickerOptionViewModel(0, "-")]
        };

    public PickerOptionViewModel ScriptIndexOption(int scriptType, int value)
    {
        var options = ScriptOptionsFor(scriptType);
        return options.FirstOrDefault(option => option.Value == value)
            ?? new PickerOptionViewModel(value, scriptType == 2 ? $"Function {value}" : CommonScriptName(value));
    }

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildScriptTypeOptions()
        =>
        [
            new(0, "-"),
            new(1, "Common"),
            new(2, "Current Room")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildDirectionOptions()
        =>
        [
            new(0, "Up"),
            new(1, "Down")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildCurrentScriptOptions()
        => Enumerable.Range(0, 256)
            .Select(index => new PickerOptionViewModel(index, index == 0 ? "-" : $"Function {index}"))
            .ToArray();

    private static IReadOnlyList<PickerOptionViewModel> BuildCommonScriptOptions()
        => Enumerable.Range(0, 256)
            .Select(index => new PickerOptionViewModel(index, CommonScriptName(index)))
            .ToArray();

    private static string CommonScriptName(int index)
        => index switch
        {
            0 => "-",
            4 => "Warp",
            5 => "Door",
            6 => "Elevator",
            0x0b => "Text",
            0x0c => "Cutscene Warp",
            0x0d => "PC",
            _ => $"Function {index}"
        };
}
