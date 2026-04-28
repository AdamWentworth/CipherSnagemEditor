using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class MoveEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public MoveEditorViewModel(
        ColosseumMove move,
        MoveEditorResources resources,
        Action? changed = null)
    {
        Move = move;
        Resources = resources;
        _changed = changed;

        _nameId = move.NameId;
        _descriptionId = move.DescriptionId;
        _selectedType = resources.TypeOption(move.TypeId);
        _selectedCategory = resources.CategoryOption(move.CategoryId);
        _selectedTarget = resources.TargetOption(move.TargetId);
        _selectedAnimation = resources.AnimationOption(move.AnimationId);
        _selectedEffect = resources.EffectOption(move.EffectId);
        _selectedEffectType = resources.EffectTypeOption(move.EffectTypeId);
        _animation2Id = move.Animation2Id;
        _power = move.Power;
        _accuracy = move.Accuracy;
        _pp = move.Pp;
        _priority = move.Priority;
        _effectAccuracy = move.EffectAccuracy;
        _hmFlag = move.HmFlag;
        _soundBasedFlag = move.SoundBasedFlag;
        _contactFlag = move.ContactFlag;
        _kingsRockFlag = move.KingsRockFlag;
        _protectFlag = move.ProtectFlag;
        _snatchFlag = move.SnatchFlag;
        _magicCoatFlag = move.MagicCoatFlag;
        _mirrorMoveFlag = move.MirrorMoveFlag;

        _isInitializing = false;
    }

    public ColosseumMove Move { get; }

    public MoveEditorResources Resources { get; }

    public IReadOnlyList<PickerOptionViewModel> TypeOptions => Resources.TypeOptions;

    public IReadOnlyList<PickerOptionViewModel> CategoryOptions => Resources.CategoryOptions;

    public IReadOnlyList<PickerOptionViewModel> TargetOptions => Resources.TargetOptions;

    public IReadOnlyList<PickerOptionViewModel> AnimationOptions => Resources.AnimationOptions;

    public IReadOnlyList<PickerOptionViewModel> EffectOptions => Resources.EffectOptions;

    public IReadOnlyList<PickerOptionViewModel> EffectTypeOptions => Resources.EffectTypeOptions;

    public string Name => Move.Name;

    public string Description => Move.Description;

    public string IndexText => Move.Index.ToString();

    public string HexText => $"0x{Move.Index:X}";

    public string StartOffsetText => $"0x{Move.StartOffset:X}";

    public string Animation2Text => $"Anim 2: {Animation2Id}";

    [ObservableProperty]
    private int _nameId;

    [ObservableProperty]
    private int _descriptionId;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedType;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedCategory;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedTarget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Animation2Text))]
    private PickerOptionViewModel? _selectedAnimation;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedEffect;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedEffectType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Animation2Text))]
    private int _animation2Id;

    [ObservableProperty]
    private int _power;

    [ObservableProperty]
    private int _accuracy;

    [ObservableProperty]
    private int _pp;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private int _effectAccuracy;

    [ObservableProperty]
    private bool _hmFlag;

    [ObservableProperty]
    private bool _soundBasedFlag;

    [ObservableProperty]
    private bool _contactFlag;

    [ObservableProperty]
    private bool _kingsRockFlag;

    [ObservableProperty]
    private bool _protectFlag;

    [ObservableProperty]
    private bool _snatchFlag;

    [ObservableProperty]
    private bool _magicCoatFlag;

    [ObservableProperty]
    private bool _mirrorMoveFlag;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumMoveUpdate ToUpdate()
        => new(
            Move.Index,
            NameId,
            DescriptionId,
            SelectedType?.Value ?? Move.TypeId,
            SelectedTarget?.Value ?? Move.TargetId,
            SelectedCategory?.Value ?? Move.CategoryId,
            SelectedAnimation?.Value ?? Move.AnimationId,
            Animation2Id,
            SelectedEffect?.Value ?? Move.EffectId,
            SelectedEffectType?.Value ?? Move.EffectTypeId,
            Power,
            Accuracy,
            Pp,
            Priority,
            EffectAccuracy,
            HmFlag,
            SoundBasedFlag,
            ContactFlag,
            KingsRockFlag,
            ProtectFlag,
            SnatchFlag,
            MagicCoatFlag,
            MirrorMoveFlag);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnNameIdChanged(int value) => MarkChanged();

    partial void OnDescriptionIdChanged(int value) => MarkChanged();

    partial void OnSelectedTypeChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedCategoryChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedTargetChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedAnimationChanged(PickerOptionViewModel? value)
    {
        if (!_isInitializing && value is not null)
        {
            Animation2Id = value.Value < 0x164 ? value.Value : value.Value - 1;
        }

        MarkChanged();
    }

    partial void OnSelectedEffectChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedEffectTypeChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnAnimation2IdChanged(int value) => MarkChanged();

    partial void OnPowerChanged(int value) => MarkChanged();

    partial void OnAccuracyChanged(int value) => MarkChanged();

    partial void OnPpChanged(int value) => MarkChanged();

    partial void OnPriorityChanged(int value) => MarkChanged();

    partial void OnEffectAccuracyChanged(int value) => MarkChanged();

    partial void OnHmFlagChanged(bool value) => MarkChanged();

    partial void OnSoundBasedFlagChanged(bool value) => MarkChanged();

    partial void OnContactFlagChanged(bool value) => MarkChanged();

    partial void OnKingsRockFlagChanged(bool value) => MarkChanged();

    partial void OnProtectFlagChanged(bool value) => MarkChanged();

    partial void OnSnatchFlagChanged(bool value) => MarkChanged();

    partial void OnMagicCoatFlagChanged(bool value) => MarkChanged();

    partial void OnMirrorMoveFlagChanged(bool value) => MarkChanged();

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
