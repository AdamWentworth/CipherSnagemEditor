using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class PokemonStatsEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public PokemonStatsEditorViewModel(
        ColosseumPokemonStats stats,
        PokemonStatsEditorResources resources,
        Bitmap? faceImage,
        Action? changed = null)
    {
        Stats = stats;
        Resources = resources;
        FaceImage = faceImage;
        _changed = changed;

        _nameId = stats.NameId;
        _selectedType1 = resources.TypeOption(stats.Type1);
        _selectedType2 = resources.TypeOption(stats.Type2);
        _selectedAbility1 = resources.AbilityOption(stats.Ability1);
        _selectedAbility2 = resources.AbilityOption(stats.Ability2);
        _selectedHeldItem1 = resources.ItemOption(stats.HeldItem1);
        _selectedHeldItem2 = resources.ItemOption(stats.HeldItem2);
        _selectedExpRate = resources.ExpRateOption(stats.ExpRate);
        _selectedGenderRatio = resources.GenderRatioOption(stats.GenderRatio);
        _catchRate = stats.CatchRate;
        _baseExp = stats.BaseExp;
        _baseHappiness = stats.BaseHappiness;
        _height = stats.Height;
        _weight = stats.Weight;
        _hp = stats.Hp;
        _attack = stats.Attack;
        _defense = stats.Defense;
        _specialAttack = stats.SpecialAttack;
        _specialDefense = stats.SpecialDefense;
        _speed = stats.Speed;
        _hpYield = stats.HpYield;
        _attackYield = stats.AttackYield;
        _defenseYield = stats.DefenseYield;
        _specialAttackYield = stats.SpecialAttackYield;
        _specialDefenseYield = stats.SpecialDefenseYield;
        _speedYield = stats.SpeedYield;

        _isInitializing = false;
    }

    public ColosseumPokemonStats Stats { get; }

    public PokemonStatsEditorResources Resources { get; }

    public Bitmap? FaceImage { get; }

    public IReadOnlyList<PickerOptionViewModel> TypeOptions => Resources.TypeOptions;

    public IReadOnlyList<PickerOptionViewModel> AbilityOptions => Resources.AbilityOptions;

    public IReadOnlyList<PickerOptionViewModel> ItemOptions => Resources.ItemOptions;

    public IReadOnlyList<PickerOptionViewModel> ExpRateOptions => Resources.ExpRateOptions;

    public IReadOnlyList<PickerOptionViewModel> GenderRatioOptions => Resources.GenderRatioOptions;

    public string Name => Stats.Name;

    public string IndexText => $"Index: {Stats.Index}";

    public string HexText => $"Hex: 0x{Stats.Index:x}";

    public string NationalIndexText => $"National: {Stats.NationalIndex}";

    public string StartOffsetText => $"Start: 0x{Stats.StartOffset:x}";

    public int BaseStatTotal => Hp + Attack + Defense + SpecialAttack + SpecialDefense + Speed;

    [ObservableProperty]
    private int _nameId;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedType1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedType2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedAbility1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedAbility2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedHeldItem1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedHeldItem2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedExpRate;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedGenderRatio;

    [ObservableProperty]
    private int _catchRate;

    [ObservableProperty]
    private int _baseExp;

    [ObservableProperty]
    private int _baseHappiness;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private double _weight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _hp;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _attack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _defense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _specialAttack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _specialDefense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseStatTotal))]
    private int _speed;

    [ObservableProperty]
    private int _hpYield;

    [ObservableProperty]
    private int _attackYield;

    [ObservableProperty]
    private int _defenseYield;

    [ObservableProperty]
    private int _specialAttackYield;

    [ObservableProperty]
    private int _specialDefenseYield;

    [ObservableProperty]
    private int _speedYield;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumPokemonStatsUpdate ToUpdate()
        => new(
            Stats.Index,
            NameId,
            SelectedExpRate?.Value ?? Stats.ExpRate,
            SelectedGenderRatio?.Value ?? Stats.GenderRatio,
            BaseExp,
            BaseHappiness,
            Height,
            Weight,
            SelectedType1?.Value ?? Stats.Type1,
            SelectedType2?.Value ?? Stats.Type2,
            SelectedAbility1?.Value ?? Stats.Ability1,
            SelectedAbility2?.Value ?? Stats.Ability2,
            SelectedHeldItem1?.Value ?? Stats.HeldItem1,
            SelectedHeldItem2?.Value ?? Stats.HeldItem2,
            CatchRate,
            Hp,
            Attack,
            Defense,
            SpecialAttack,
            SpecialDefense,
            Speed,
            HpYield,
            AttackYield,
            DefenseYield,
            SpecialAttackYield,
            SpecialDefenseYield,
            SpeedYield);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnNameIdChanged(int value) => MarkChanged();

    partial void OnSelectedType1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedType2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedAbility1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedAbility2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedHeldItem1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedHeldItem2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedExpRateChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedGenderRatioChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnCatchRateChanged(int value) => MarkChanged();

    partial void OnBaseExpChanged(int value) => MarkChanged();

    partial void OnBaseHappinessChanged(int value) => MarkChanged();

    partial void OnHeightChanged(double value) => MarkChanged();

    partial void OnWeightChanged(double value) => MarkChanged();

    partial void OnHpChanged(int value) => MarkChanged();

    partial void OnAttackChanged(int value) => MarkChanged();

    partial void OnDefenseChanged(int value) => MarkChanged();

    partial void OnSpecialAttackChanged(int value) => MarkChanged();

    partial void OnSpecialDefenseChanged(int value) => MarkChanged();

    partial void OnSpeedChanged(int value) => MarkChanged();

    partial void OnHpYieldChanged(int value) => MarkChanged();

    partial void OnAttackYieldChanged(int value) => MarkChanged();

    partial void OnDefenseYieldChanged(int value) => MarkChanged();

    partial void OnSpecialAttackYieldChanged(int value) => MarkChanged();

    partial void OnSpecialDefenseYieldChanged(int value) => MarkChanged();

    partial void OnSpeedYieldChanged(int value) => MarkChanged();

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
