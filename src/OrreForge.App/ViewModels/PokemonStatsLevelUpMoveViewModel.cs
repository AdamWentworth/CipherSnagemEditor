using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class PokemonStatsLevelUpMoveViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public PokemonStatsLevelUpMoveViewModel(
        ColosseumPokemonLevelUpMove move,
        PokemonStatsEditorResources resources,
        Action? changed)
    {
        Index = move.Index;
        MoveOptions = resources.MoveOptions;
        LevelOptions = resources.LevelOptions;
        _selectedMove = resources.MoveOption(move.MoveId);
        _level = move.Level;
        _changed = changed;
        _isInitializing = false;
    }

    public int Index { get; }

    public IReadOnlyList<PickerOptionViewModel> MoveOptions { get; }

    public IReadOnlyList<int> LevelOptions { get; }

    [ObservableProperty]
    private PickerOptionViewModel? _selectedMove;

    [ObservableProperty]
    private int _level;

    public ColosseumPokemonLevelUpMove ToData()
        => new(Index, Level, SelectedMove?.Value ?? 0, SelectedMove?.Name ?? "-");

    partial void OnSelectedMoveChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnLevelChanged(int value) => MarkChanged();

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        _changed?.Invoke();
    }
}
