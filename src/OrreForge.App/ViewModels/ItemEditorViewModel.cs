using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class ItemEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public ItemEditorViewModel(
        ColosseumItem item,
        ItemEditorResources resources,
        Action? changed = null)
    {
        Item = item;
        Resources = resources;
        _changed = changed;

        _nameId = item.NameId;
        _descriptionId = item.DescriptionId;
        _selectedBagSlot = resources.BagSlotOption(item.BagSlotId);
        _canBeHeld = item.CanBeHeld;
        _price = item.Price;
        _couponPrice = item.CouponPrice;
        _parameter = item.Parameter;
        _holdItemId = item.HoldItemId;
        _inBattleUseId = item.InBattleUseId;
        _friendship1 = item.FriendshipEffects.Count > 0 ? item.FriendshipEffects[0] : 0;
        _friendship2 = item.FriendshipEffects.Count > 1 ? item.FriendshipEffects[1] : 0;
        _friendship3 = item.FriendshipEffects.Count > 2 ? item.FriendshipEffects[2] : 0;
        _selectedTmMove = resources.MoveOption(item.TmMoveId);

        _isInitializing = false;
    }

    public ColosseumItem Item { get; }

    public ItemEditorResources Resources { get; }

    public IReadOnlyList<PickerOptionViewModel> BagSlotOptions => Resources.BagSlotOptions;

    public IReadOnlyList<PickerOptionViewModel> MoveOptions => Resources.MoveOptions;

    public string Name => Item.Name;

    public string Description => Item.Description;

    public string IndexText => Item.Index.ToString();

    public string HexText => $"0x{Item.Index:X}";

    public string StartOffsetText => $"0x{Item.StartOffset:X}";

    public bool IsTmVisible => Item.TmIndex > 0;

    [ObservableProperty]
    private int _nameId;

    [ObservableProperty]
    private int _descriptionId;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedBagSlot;

    [ObservableProperty]
    private bool _canBeHeld;

    [ObservableProperty]
    private int _price;

    [ObservableProperty]
    private int _couponPrice;

    [ObservableProperty]
    private int _parameter;

    [ObservableProperty]
    private int _holdItemId;

    [ObservableProperty]
    private int _inBattleUseId;

    [ObservableProperty]
    private int _friendship1;

    [ObservableProperty]
    private int _friendship2;

    [ObservableProperty]
    private int _friendship3;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedTmMove;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumItemUpdate ToUpdate()
        => new(
            Item.Index,
            NameId,
            DescriptionId,
            SelectedBagSlot?.Value ?? Item.BagSlotId,
            CanBeHeld,
            Price,
            CouponPrice,
            Parameter,
            HoldItemId,
            InBattleUseId,
            [Friendship1, Friendship2, Friendship3],
            Item.TmIndex,
            SelectedTmMove?.Value ?? Item.TmMoveId);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnNameIdChanged(int value) => MarkChanged();

    partial void OnDescriptionIdChanged(int value) => MarkChanged();

    partial void OnSelectedBagSlotChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnCanBeHeldChanged(bool value) => MarkChanged();

    partial void OnPriceChanged(int value) => MarkChanged();

    partial void OnCouponPriceChanged(int value) => MarkChanged();

    partial void OnParameterChanged(int value) => MarkChanged();

    partial void OnHoldItemIdChanged(int value) => MarkChanged();

    partial void OnInBattleUseIdChanged(int value) => MarkChanged();

    partial void OnFriendship1Changed(int value) => MarkChanged();

    partial void OnFriendship2Changed(int value) => MarkChanged();

    partial void OnFriendship3Changed(int value) => MarkChanged();

    partial void OnSelectedTmMoveChanged(PickerOptionViewModel? value) => MarkChanged();

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
