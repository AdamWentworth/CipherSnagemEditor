using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class PokemonStatsEvolutionViewModel : ObservableObject
{
    private readonly PokemonStatsEditorResources _resources;
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public PokemonStatsEvolutionViewModel(
        ColosseumPokemonEvolution evolution,
        PokemonStatsEditorResources resources,
        Action? changed)
    {
        Index = evolution.Index;
        _resources = resources;
        _changed = changed;
        SpeciesOptions = resources.SpeciesOptions;
        MethodOptions = resources.EvolutionMethodOptions;
        _selectedSpecies = resources.SpeciesOption(evolution.EvolvedSpeciesId);
        _selectedMethod = resources.EvolutionMethodOption(evolution.Method);
        _conditionOptions = resources.EvolutionConditionOptions(evolution.Method);
        _selectedCondition = resources.EvolutionConditionOption(evolution.Method, evolution.Condition);
        _isInitializing = false;
    }

    public int Index { get; }

    public IReadOnlyList<PickerOptionViewModel> SpeciesOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> MethodOptions { get; }

    [ObservableProperty]
    private IReadOnlyList<PickerOptionViewModel> _conditionOptions = [];

    [ObservableProperty]
    private PickerOptionViewModel? _selectedSpecies;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMethod;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedCondition;

    public ColosseumPokemonEvolution ToData()
        => new(
            Index,
            SelectedMethod?.Value ?? 0,
            SelectedMethod?.Name ?? "None",
            SelectedCondition?.Value ?? 0,
            SelectedCondition?.Name ?? "-",
            SelectedSpecies?.Value ?? 0,
            SelectedSpecies?.Name ?? "-");

    partial void OnSelectedSpeciesChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedMethodChanged(PickerOptionViewModel? value)
    {
        if (!_isInitializing)
        {
            ConditionOptions = _resources.EvolutionConditionOptions(value?.Value ?? 0);
            SelectedCondition = ConditionOptions.FirstOrDefault();
        }

        MarkChanged();
    }

    partial void OnSelectedConditionChanged(PickerOptionViewModel? value) => MarkChanged();

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        _changed?.Invoke();
    }
}
