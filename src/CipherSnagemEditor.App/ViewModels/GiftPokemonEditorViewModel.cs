using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class GiftPokemonEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public GiftPokemonEditorViewModel(
        ColosseumGiftPokemon gift,
        GiftPokemonEditorResources resources,
        Bitmap? faceImage,
        Action? changed = null)
    {
        Gift = gift;
        Resources = resources;
        FaceImage = faceImage;
        _changed = changed;
        _selectedSpecies = resources.SpeciesOption(gift.SpeciesId);
        _level = gift.Level;
        _selectedMove1 = resources.MoveOption(gift.MoveIds.Count > 0 ? gift.MoveIds[0] : 0);
        _selectedMove2 = resources.MoveOption(gift.MoveIds.Count > 1 ? gift.MoveIds[1] : 0);
        _selectedMove3 = resources.MoveOption(gift.MoveIds.Count > 2 ? gift.MoveIds[2] : 0);
        _selectedMove4 = resources.MoveOption(gift.MoveIds.Count > 3 ? gift.MoveIds[3] : 0);
        _selectedShiny = resources.ShinyOption(gift.ShinyValue);
        _selectedGender = resources.GenderOption(gift.Gender);
        _selectedNature = resources.NatureOption(gift.Nature);
        _isInitializing = false;
    }

    public ColosseumGiftPokemon Gift { get; }

    public GiftPokemonEditorResources Resources { get; }

    public Bitmap? FaceImage { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions => Resources.SpeciesOptions;

    public IReadOnlyList<int> LevelOptions => Resources.LevelOptions;

    public IReadOnlyList<PickerOptionViewModel> MoveOptions => Resources.MoveOptions;

    public IReadOnlyList<PickerOptionViewModel> ShinyOptions => Resources.ShinyOptions;

    public IReadOnlyList<PickerOptionViewModel> GenderOptions => Resources.GenderOptions;

    public IReadOnlyList<PickerOptionViewModel> NatureOptions => Resources.NatureOptions;

    public string Name => Gift.SpeciesName;

    public string GiftType => Gift.GiftType;

    public string RowIdText => Gift.RowId.ToString();

    public string IndexText => Gift.DataIndex.ToString();

    public string HexText => $"0x{Gift.DataIndex:X}";

    public string StartOffsetText => Gift.StartOffset <= 0 ? "-" : $"0x{Gift.StartOffset:X}";

    public bool SupportsNatureGender => Gift.SupportsNatureGender;

    public bool IsDistroGift => !Gift.UsesLevelUpMoves;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedSpecies;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove1;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove2;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove3;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove4;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedShiny;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedGender;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedNature;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumGiftPokemonUpdate ToUpdate()
        => new(
            Gift.RowId,
            SelectedSpecies?.Value ?? Gift.SpeciesId,
            Level,
            [
                SelectedMove1?.Value ?? 0,
                SelectedMove2?.Value ?? 0,
                SelectedMove3?.Value ?? 0,
                SelectedMove4?.Value ?? 0
            ],
            SelectedShiny?.Value ?? Gift.ShinyValue,
            SelectedGender?.Value ?? Gift.Gender,
            SelectedNature?.Value ?? Gift.Nature);

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnSelectedSpeciesChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnLevelChanged(int value) => MarkChanged();

    partial void OnSelectedMove1Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedMove2Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedMove3Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedMove4Changed(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedShinyChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedGenderChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedNatureChanged(PickerOptionViewModel? value) => MarkChanged();

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
