using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdShadowPokemonEntryViewModel : ObservableObject
{
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#20F020");
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IReadOnlyList<int> LevelValues = Enumerable.Range(0, 101).ToArray();
    private readonly TrainerPokemonEditorResources _resources;
    private readonly Action? _changed;
    private Bitmap? _faceImage;
    private Bitmap? _bodyImage;
    private IReadOnlyList<PokemonBodyFrame>? _bodyFrames;
    private int _loadedFaceSpecies = -1;
    private int _loadedBodySpecies = -1;
    private bool _isInitializing = true;

    public XdShadowPokemonEntryViewModel(
        XdShadowPokemonRecord shadow,
        TrainerPokemonEditorResources resources,
        IReadOnlyList<PickerOptionViewModel> storyPokemonOptions,
        Action? changed = null)
    {
        Shadow = shadow;
        _resources = resources;
        _changed = changed;
        StoryPokemonOptions = storyPokemonOptions;
        _selectedStoryPokemon = storyPokemonOptions.FirstOrDefault(option => option.Value == shadow.StoryPokemonIndex)
            ?? new PickerOptionViewModel(shadow.StoryPokemonIndex, StoryPokemonTextFor(shadow));
        _selectedSpecies = resources.SpeciesOption(shadow.SpeciesId);
        _selectedItem = resources.ItemOption(shadow.ItemId);
        _selectedNature = resources.NatureOption(shadow.Nature);
        _selectedGender = resources.GenderOption(shadow.Gender);
        _selectedRegularMove1 = resources.MoveOption(MoveId(shadow.RegularMoveIds, 0));
        _selectedRegularMove2 = resources.MoveOption(MoveId(shadow.RegularMoveIds, 1));
        _selectedRegularMove3 = resources.MoveOption(MoveId(shadow.RegularMoveIds, 2));
        _selectedRegularMove4 = resources.MoveOption(MoveId(shadow.RegularMoveIds, 3));
        _selectedShadowMove1 = resources.MoveOption(MoveId(shadow.MoveIds, 0));
        _selectedShadowMove2 = resources.MoveOption(MoveId(shadow.MoveIds, 1));
        _selectedShadowMove3 = resources.MoveOption(MoveId(shadow.MoveIds, 2));
        _selectedShadowMove4 = resources.MoveOption(MoveId(shadow.MoveIds, 3));
        _level = shadow.Level;
        _shadowBoostLevel = shadow.ShadowBoostLevel;
        _iv = shadow.Iv;
        _happiness = shadow.Happiness;
        _hpEv = Ev(0);
        _attackEv = Ev(1);
        _defenseEv = Ev(2);
        _specialAttackEv = Ev(3);
        _specialDefenseEv = Ev(4);
        _speedEv = Ev(5);
        _heartGauge = shadow.HeartGauge;
        _fleeValue = shadow.FleeValue;
        _aggression = shadow.Aggression;
        _catchRate = shadow.CatchRate;
        RefreshAbilityOptions(shadow.Ability);
        _isInitializing = false;
    }

    public XdShadowPokemonRecord Shadow { get; }

    public IReadOnlyList<PickerOptionViewModel> StoryPokemonOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions => _resources.SpeciesOptions;

    public IReadOnlyList<PickerOptionViewModel> ItemOptions => _resources.ItemOptions;

    public IReadOnlyList<PickerOptionViewModel> MoveOptions => _resources.MoveOptions;

    public IReadOnlyList<PickerOptionViewModel> NatureOptions => _resources.NatureOptions;

    public IReadOnlyList<PickerOptionViewModel> GenderOptions => _resources.GenderOptions;

    public IReadOnlyList<int> LevelOptions => LevelValues;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedStoryPokemon;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedSpecies;

    [ObservableProperty]
    private IReadOnlyList<PickerOptionViewModel> _abilityOptions = [];

    [ObservableProperty]
    private PickerOptionViewModel? _selectedAbility;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedItem;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedNature;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedGender;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRegularMove1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRegularMove2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRegularMove3;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRegularMove4;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShadowMove1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShadowMove2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShadowMove3;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShadowMove4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowText))]
    private int _level;

    [ObservableProperty]
    private int _shadowBoostLevel;

    [ObservableProperty]
    private int _iv;

    [ObservableProperty]
    private int _happiness;

    [ObservableProperty]
    private int _hpEv;

    [ObservableProperty]
    private int _attackEv;

    [ObservableProperty]
    private int _defenseEv;

    [ObservableProperty]
    private int _specialAttackEv;

    [ObservableProperty]
    private int _specialDefenseEv;

    [ObservableProperty]
    private int _speedEv;

    [ObservableProperty]
    private int _heartGauge;

    [ObservableProperty]
    private int _fleeValue;

    [ObservableProperty]
    private int _aggression;

    [ObservableProperty]
    private int _catchRate;

    [ObservableProperty]
    private bool _hasChanges;

    public Bitmap? FaceImage
    {
        get
        {
            var species = SpeciesId;
            if (_loadedFaceSpecies != species)
            {
                _faceImage = RuntimeImageAssets.LoadImage("PokeFace", $"face_{species:000}.png");
                _loadedFaceSpecies = species;
            }

            return _faceImage;
        }
    }

    public Bitmap? BodyImage
    {
        get
        {
            EnsureBodyFrames();
            return _bodyImage;
        }
    }

    public IReadOnlyList<PokemonBodyFrame> BodyFrames
    {
        get
        {
            EnsureBodyFrames();
            return _bodyFrames ?? [];
        }
    }

    public string RowText => $"{Shadow.Index}: Shadow {SpeciesName}{Environment.NewLine}Lv. {Level}+";

    public string SearchText => $"{Shadow.Index} {SpeciesId} {SpeciesName} {SelectedStoryPokemon?.Value} {SelectedItem?.Name} {SelectedRegularMove1?.Name} {SelectedRegularMove2?.Name} {SelectedRegularMove3?.Name} {SelectedRegularMove4?.Name} {SelectedShadowMove1?.Name} {SelectedShadowMove2?.Name} {SelectedShadowMove3?.Name} {SelectedShadowMove4?.Name}";

    public string SpeciesName => SelectedSpecies?.Name ?? Shadow.SpeciesName;

    public string StoryPokemonText => SelectedStoryPokemon?.Name ?? StoryPokemonTextFor(Shadow);

    public IBrush BackgroundBrush => IsSelected ? SelectedBrush : ShadowBrush;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private int SpeciesId => SelectedSpecies?.Value ?? 0;

    public XdShadowPokemonUpdate ToUpdate()
        => new(
            Shadow.Index,
            SelectedStoryPokemon?.Value ?? Shadow.StoryPokemonIndex,
            SpeciesId,
            Level,
            CatchRate,
            HeartGauge,
            Shadow.InUseFlag,
            FleeValue,
            Aggression,
            Shadow.AlwaysFlee,
            [MoveValue(SelectedShadowMove1), MoveValue(SelectedShadowMove2), MoveValue(SelectedShadowMove3), MoveValue(SelectedShadowMove4)],
            ShadowBoostLevel,
            SelectedItem?.Value ?? 0,
            SelectedAbility?.Value ?? Shadow.Ability,
            SelectedNature?.Value ?? Shadow.Nature,
            SelectedGender?.Value ?? Shadow.Gender,
            Happiness,
            Iv,
            [HpEv, AttackEv, DefenseEv, SpecialAttackEv, SpecialDefenseEv, SpeedEv],
            [MoveValue(SelectedRegularMove1), MoveValue(SelectedRegularMove2), MoveValue(SelectedRegularMove3), MoveValue(SelectedRegularMove4)]);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnSelectedStoryPokemonChanged(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(StoryPokemonText));
        MarkChanged();
    }

    partial void OnSelectedSpeciesChanged(PickerOptionViewModel? value)
    {
        _loadedFaceSpecies = -1;
        _loadedBodySpecies = -1;
        _bodyFrames = null;
        _bodyImage = null;
        OnPropertyChanged(nameof(FaceImage));
        OnPropertyChanged(nameof(BodyImage));
        OnPropertyChanged(nameof(BodyFrames));
        OnPropertyChanged(nameof(SpeciesName));
        OnPropertyChanged(nameof(RowText));
        RefreshAbilityOptions(SelectedAbility?.Value ?? Shadow.Ability);
        MarkChanged();
    }

    partial void OnSelectedAbilityChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedItemChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedNatureChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedGenderChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedRegularMove1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedRegularMove2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedRegularMove3Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedRegularMove4Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedShadowMove1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedShadowMove2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedShadowMove3Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedShadowMove4Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnLevelChanged(int value) => MarkChanged();

    partial void OnShadowBoostLevelChanged(int value) => MarkChanged();

    partial void OnIvChanged(int value) => MarkChanged();

    partial void OnHappinessChanged(int value) => MarkChanged();

    partial void OnHpEvChanged(int value) => MarkChanged();

    partial void OnAttackEvChanged(int value) => MarkChanged();

    partial void OnDefenseEvChanged(int value) => MarkChanged();

    partial void OnSpecialAttackEvChanged(int value) => MarkChanged();

    partial void OnSpecialDefenseEvChanged(int value) => MarkChanged();

    partial void OnSpeedEvChanged(int value) => MarkChanged();

    partial void OnHeartGaugeChanged(int value) => MarkChanged();

    partial void OnFleeValueChanged(int value) => MarkChanged();

    partial void OnAggressionChanged(int value) => MarkChanged();

    partial void OnCatchRateChanged(int value) => MarkChanged();

    private void RefreshAbilityOptions(int ability)
    {
        var currentName = Shadow.SpeciesId == SpeciesId ? Shadow.AbilityName : null;
        AbilityOptions =
        [
            new PickerOptionViewModel(0, ability == 0 && !string.IsNullOrWhiteSpace(currentName) ? currentName : "Ability 0"),
            new PickerOptionViewModel(1, ability == 1 && !string.IsNullOrWhiteSpace(currentName) ? currentName : "Ability 1")
        ];
        SelectedAbility = AbilityOptions.FirstOrDefault(option => option.Value == ability)
            ?? AbilityOptions.First();
    }

    private void EnsureBodyFrames()
    {
        var species = SpeciesId;
        if (_loadedBodySpecies == species)
        {
            return;
        }

        _bodyFrames = RuntimeImageAssets.LoadBodyFrames(species);
        _bodyImage = _bodyFrames.FirstOrDefault()?.Image;
        _loadedBodySpecies = species;
    }

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        HasChanges = true;
        _changed?.Invoke();
    }

    private int Ev(int index)
        => index < Shadow.Evs.Count ? Shadow.Evs[index] : 0;

    private static int MoveId(IReadOnlyList<int> moves, int index)
        => index < moves.Count ? moves[index] : 0;

    private static int MoveValue(PickerOptionViewModel? option)
        => option?.Value ?? 0;

    private static string StoryPokemonTextFor(XdShadowPokemonRecord shadow)
        => $"{shadow.StoryPokemonIndex}: Lv. {shadow.ShadowBoostLevel} {shadow.SpeciesName}";
}
