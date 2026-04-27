using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TrainerPokemonSlotViewModel : ObservableObject
{
    private static readonly IBrush EmptyBrush = SolidColorBrush.Parse("#C0C0C8");
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A070FF");
    private static readonly IBrush NormalBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly Dictionary<int, Bitmap?> BodyImageCache = [];
    private readonly TrainerPokemonEditorResources _resources;
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public TrainerPokemonSlotViewModel(
        ColosseumTrainerPokemon pokemon,
        TrainerPokemonEditorResources resources,
        Action? changed = null)
    {
        Pokemon = pokemon;
        _resources = resources;
        _changed = changed;

        _selectedSpecies = resources.SpeciesOption(pokemon.SpeciesId);
        _selectedShadow = resources.ShadowOption(pokemon.ShadowId);
        _selectedItem = resources.ItemOption(pokemon.ItemId);
        _selectedPokeball = resources.ItemOption(pokemon.PokeballId);
        _selectedNature = resources.NatureOption(pokemon.Nature);
        _selectedGender = resources.GenderOption(pokemon.Gender);
        _selectedMove1 = resources.MoveOption(MoveId(0));
        _selectedMove2 = resources.MoveOption(MoveId(1));
        _selectedMove3 = resources.MoveOption(MoveId(2));
        _selectedMove4 = resources.MoveOption(MoveId(3));
        _level = pokemon.Level;
        _happiness = pokemon.Happiness;
        _iv = pokemon.Iv;
        _hpEv = Ev(0);
        _attackEv = Ev(1);
        _defenseEv = Ev(2);
        _specialAttackEv = Ev(3);
        _specialDefenseEv = Ev(4);
        _speedEv = Ev(5);
        _shadowHeartGauge = pokemon.ShadowData?.HeartGauge ?? 0;
        _shadowFirstTrainerId = pokemon.ShadowData?.FirstTrainerId ?? 0;
        _shadowAlternateFirstTrainerId = pokemon.ShadowData?.AlternateFirstTrainerId ?? 0;
        _shadowCatchRate = pokemon.ShadowData?.CatchRate ?? 0;

        RefreshAbilityOptions(pokemon.Ability);
        BodyImage = LoadBodyImage(SpeciesId);
        _isInitializing = false;
    }

    public ColosseumTrainerPokemon Pokemon { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions => _resources.SpeciesOptions;

    public IReadOnlyList<PickerOptionViewModel> ItemOptions => _resources.ItemOptions;

    public IReadOnlyList<PickerOptionViewModel> MoveOptions => _resources.MoveOptions;

    public IReadOnlyList<PickerOptionViewModel> NatureOptions => _resources.NatureOptions;

    public IReadOnlyList<PickerOptionViewModel> GenderOptions => _resources.GenderOptions;

    public IReadOnlyList<PickerOptionViewModel> ShadowOptions => _resources.ShadowOptions;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedSpecies;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShadow;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedItem;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedPokeball;

    [ObservableProperty]
    private IReadOnlyList<PickerOptionViewModel> _abilityOptions = [];

    [ObservableProperty]
    private PickerOptionViewModel? _selectedAbility;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedNature;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedGender;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove3;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove4;

    [ObservableProperty]
    private Bitmap? _bodyImage;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private int _happiness;

    [ObservableProperty]
    private int _iv;

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
    private int _shadowHeartGauge;

    [ObservableProperty]
    private int _shadowFirstTrainerId;

    [ObservableProperty]
    private int _shadowAlternateFirstTrainerId;

    [ObservableProperty]
    private int _shadowCatchRate;

    [ObservableProperty]
    private bool _hasChanges;

    public bool IsSet => SpeciesId > 0;

    public bool IsShadow => ShadowId > 0;

    public string DeckIndexText => $"PKM {Pokemon.Index}";

    public string SpeciesText => SpeciesId > 0 ? $"{SelectedSpecies?.Name ?? "Pokemon"} ({SpeciesId})" : "-";

    public string ShadowIndexText => $"Shadow ID {ShadowId}";

    public double Opacity => IsSet ? 1 : 0.5;

    public IBrush BackgroundBrush => IsShadow ? ShadowBrush : IsSet ? NormalBrush : EmptyBrush;

    public string Move1Text => MoveText(SelectedMove1?.Value ?? 0);

    public string Move2Text => MoveText(SelectedMove2?.Value ?? 0);

    public string Move3Text => MoveText(SelectedMove3?.Value ?? 0);

    public string Move4Text => MoveText(SelectedMove4?.Value ?? 0);

    private int SpeciesId => SelectedSpecies?.Value ?? 0;

    private int ShadowId => SelectedShadow?.Value ?? 0;

    public ColosseumTrainerPokemonUpdate ToUpdate()
        => new(
            Pokemon.Index,
            SpeciesId,
            Level,
            ShadowId,
            SelectedItem?.Value ?? 0,
            SelectedPokeball?.Value ?? 0,
            SelectedAbility?.Value ?? 0xff,
            SelectedNature?.Value ?? 0xff,
            SelectedGender?.Value ?? 0xff,
            Happiness,
            Iv,
            [HpEv, AttackEv, DefenseEv, SpecialAttackEv, SpecialDefenseEv, SpeedEv],
            [SelectedMove1?.Value ?? 0, SelectedMove2?.Value ?? 0, SelectedMove3?.Value ?? 0, SelectedMove4?.Value ?? 0],
            ShadowHeartGauge,
            ShadowFirstTrainerId,
            ShadowAlternateFirstTrainerId,
            ShadowCatchRate);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnSelectedSpeciesChanged(PickerOptionViewModel? value)
    {
        BodyImage = LoadBodyImage(SpeciesId);
        RefreshAbilityOptions(SelectedAbility?.Value ?? Pokemon.Ability);
        NotifyDerivedState();
        MarkChanged();
    }

    partial void OnSelectedShadowChanged(PickerOptionViewModel? value)
    {
        if (!_isInitializing && value is not null && _resources.ShadowData(value.Value) is { } shadow)
        {
            ShadowHeartGauge = shadow.HeartGauge;
            ShadowFirstTrainerId = shadow.FirstTrainerId;
            ShadowAlternateFirstTrainerId = shadow.AlternateFirstTrainerId;
            ShadowCatchRate = shadow.CatchRate;
        }

        NotifyDerivedState();
        MarkChanged();
    }

    partial void OnSelectedItemChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedPokeballChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedAbilityChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedNatureChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedGenderChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedMove1Changed(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(Move1Text));
        MarkChanged();
    }

    partial void OnSelectedMove2Changed(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(Move2Text));
        MarkChanged();
    }

    partial void OnSelectedMove3Changed(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(Move3Text));
        MarkChanged();
    }

    partial void OnSelectedMove4Changed(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(Move4Text));
        MarkChanged();
    }

    partial void OnLevelChanged(int value) => MarkChanged();

    partial void OnHappinessChanged(int value) => MarkChanged();

    partial void OnIvChanged(int value) => MarkChanged();

    partial void OnHpEvChanged(int value) => MarkChanged();

    partial void OnAttackEvChanged(int value) => MarkChanged();

    partial void OnDefenseEvChanged(int value) => MarkChanged();

    partial void OnSpecialAttackEvChanged(int value) => MarkChanged();

    partial void OnSpecialDefenseEvChanged(int value) => MarkChanged();

    partial void OnSpeedEvChanged(int value) => MarkChanged();

    partial void OnShadowHeartGaugeChanged(int value) => MarkChanged();

    partial void OnShadowFirstTrainerIdChanged(int value) => MarkChanged();

    partial void OnShadowAlternateFirstTrainerIdChanged(int value) => MarkChanged();

    partial void OnShadowCatchRateChanged(int value) => MarkChanged();

    private void RefreshAbilityOptions(int ability)
    {
        var stats = _resources.PokemonStats(SpeciesId);
        AbilityOptions =
        [
            new PickerOptionViewModel(0, stats?.Ability1Name ?? "Ability 0"),
            new PickerOptionViewModel(1, stats?.Ability2Name ?? "Ability 1"),
            new PickerOptionViewModel(0xff, "Random")
        ];
        SelectedAbility = AbilityOptions.FirstOrDefault(option => option.Value == ability)
            ?? AbilityOptions.Last();
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(IsSet));
        OnPropertyChanged(nameof(IsShadow));
        OnPropertyChanged(nameof(Opacity));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(SpeciesText));
        OnPropertyChanged(nameof(ShadowIndexText));
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
        => index < Pokemon.Evs.Count ? Pokemon.Evs[index] : 0;

    private int MoveId(int index)
        => index < Pokemon.Moves.Count ? Pokemon.Moves[index].Index : 0;

    private string MoveText(int id)
    {
        var move = _resources.Move(id);
        return move is null || move.Index == 0
            ? "-"
            : $"{move.Name} ({move.Index}) {move.TypeName} {move.Power}/{move.Accuracy}/{move.Pp}";
    }

    private static Bitmap? LoadBodyImage(int speciesId)
    {
        if (BodyImageCache.TryGetValue(speciesId, out var cached))
        {
            return cached;
        }

        var path = ResolveBodyImagePath(speciesId);
        if (path is null)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }

        try
        {
            var image = new Bitmap(path);
            BodyImageCache[speciesId] = image;
            return image;
        }
        catch (IOException)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }
    }

    private static string? ResolveBodyImagePath(int speciesId)
    {
        var fileName = $"body_{speciesId:000}.png";
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "images", "PokeBody", fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateAssetRoots()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        foreach (var root in roots)
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                yield return current.FullName;
                current = current.Parent;
            }
        }
    }
}
