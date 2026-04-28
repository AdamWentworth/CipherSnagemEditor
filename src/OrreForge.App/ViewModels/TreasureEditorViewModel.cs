using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TreasureEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public TreasureEditorViewModel(
        ColosseumTreasure treasure,
        TreasureEditorResources resources,
        Action? changed = null)
    {
        Treasure = treasure;
        Resources = resources;
        _changed = changed;
        _selectedModel = resources.ModelOption(treasure.ModelId);
        _quantity = treasure.Quantity;
        _angle = treasure.Angle;
        _selectedRoom = resources.RoomOption(treasure.RoomId);
        _selectedItem = resources.ItemOption(treasure.ItemId);
        _x = treasure.X;
        _y = treasure.Y;
        _z = treasure.Z;
        _isInitializing = false;
    }

    public ColosseumTreasure Treasure { get; }

    public TreasureEditorResources Resources { get; }

    public IReadOnlyList<PickerOptionViewModel> ModelOptions => Resources.ModelOptions;

    public IReadOnlyList<PickerOptionViewModel> RoomOptions => Resources.RoomOptions;

    public IReadOnlyList<PickerOptionViewModel> ItemOptions => Resources.ItemOptions;

    public string Name => Treasure.ItemName;

    public string IndexText => Treasure.Index.ToString();

    public string HexText => $"0x{Treasure.Index:X}";

    public string StartOffsetText => $"0x{Treasure.StartOffset:X}";

    public string FlagText => Treasure.Flag.ToString();

    [ObservableProperty]
    private PickerOptionViewModel? _selectedModel;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private int _angle;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRoom;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedItem;

    [ObservableProperty]
    private float _x;

    [ObservableProperty]
    private float _y;

    [ObservableProperty]
    private float _z;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumTreasureUpdate ToUpdate()
        => new(
            Treasure.Index,
            SelectedModel?.Value ?? Treasure.ModelId,
            Quantity,
            Angle,
            SelectedRoom?.Value ?? Treasure.RoomId,
            SelectedItem?.Value ?? Treasure.ItemId,
            X,
            Y,
            Z);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnSelectedModelChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnQuantityChanged(int value) => MarkChanged();

    partial void OnAngleChanged(int value) => MarkChanged();

    partial void OnSelectedRoomChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedItemChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnXChanged(float value) => MarkChanged();

    partial void OnYChanged(float value) => MarkChanged();

    partial void OnZChanged(float value) => MarkChanged();

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        HasChanges = true;
        _changed?.Invoke();
    }
}
