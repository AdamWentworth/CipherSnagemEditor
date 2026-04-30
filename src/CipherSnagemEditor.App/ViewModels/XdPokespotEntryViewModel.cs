using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdPokespotEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IReadOnlyList<int> LevelValues = Enumerable.Range(0, 101).ToArray();
    private readonly TrainerPokemonEditorResources _resources;
    private readonly Action? _changed;
    private Bitmap? _faceImage;
    private Bitmap? _bodyImage;
    private Bitmap? _typeImage;
    private IReadOnlyList<PokemonBodyFrame>? _bodyFrames;
    private int _loadedFaceSpecies = -1;
    private int _loadedBodySpecies = -1;
    private bool _typeLoaded;
    private bool _isInitializing = true;

    public XdPokespotEntryViewModel(
        XdPokespotRecord encounter,
        TrainerPokemonEditorResources resources,
        Action? changed = null)
    {
        Encounter = encounter;
        _resources = resources;
        _changed = changed;
        _selectedSpecies = resources.SpeciesOption(encounter.SpeciesId);
        _minLevel = encounter.MinLevel;
        _maxLevel = encounter.MaxLevel;
        _encounterPercentage = encounter.EncounterPercentage;
        _stepsPerSnack = encounter.StepsPerSnack;
        _isInitializing = false;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeImageOpacity))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedSpecies;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowText))]
    private int _minLevel;

    [ObservableProperty]
    private int _maxLevel;

    [ObservableProperty]
    private int _encounterPercentage;

    [ObservableProperty]
    private int _stepsPerSnack;

    [ObservableProperty]
    private bool _hasChanges;

    public XdPokespotRecord Encounter { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions => _resources.SpeciesOptions;

    public IReadOnlyList<int> LevelOptions => LevelValues;

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

    public Bitmap? TypeImage
    {
        get
        {
            if (!_typeLoaded)
            {
                _typeImage = RuntimeImageAssets.LoadImage("Types", TypeImageName(Encounter.SpotName));
                _typeLoaded = true;
            }

            return _typeImage;
        }
    }

    public string RowText => SpeciesName;

    public string SpeciesName => SelectedSpecies?.Name ?? Encounter.SpeciesName;

    public string SearchText => $"{Encounter.SpotName} {Encounter.Index} {SpeciesId} {SpeciesName}";

    public double TypeImageOpacity => IsSelected ? 1.0 : 0.75;

    public IBrush FallbackBrush => SolidColorBrush.Parse(Encounter.SpotName switch
    {
        "Rock" => "#C8B070",
        "Oasis" => "#80ACFF",
        "Cave" => "#707080",
        _ => "#D0D0D0"
    });

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private int SpeciesId => SelectedSpecies?.Value ?? 0;

    public XdPokespotUpdate ToUpdate()
        => new(
            Encounter.StartOffset,
            SpeciesId,
            MinLevel,
            MaxLevel,
            EncounterPercentage,
            StepsPerSnack);

    public void MarkSaved()
    {
        HasChanges = false;
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
        MarkChanged();
    }

    partial void OnMinLevelChanged(int value) => MarkChanged();

    partial void OnMaxLevelChanged(int value) => MarkChanged();

    partial void OnEncounterPercentageChanged(int value) => MarkChanged();

    partial void OnStepsPerSnackChanged(int value) => MarkChanged();

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

    private static string TypeImageName(string spot)
        => spot switch
        {
            "Rock" => "type_5.png",
            "Oasis" => "type_11.png",
            "Cave" => "type_17.png",
            _ => "type_0.png"
        };
}
