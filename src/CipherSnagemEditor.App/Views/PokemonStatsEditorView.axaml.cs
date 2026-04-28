using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class PokemonStatsEditorView : UserControl
{
    public PokemonStatsEditorView()
    {
        InitializeComponent();
    }

    private void PokemonStatsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PokemonStatsEntryViewModel pokemon }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedPokemonStats = pokemon;
            e.Handled = true;
        }
    }

    private void TmPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PokemonStatsTmViewModel tm })
        {
            tm.IsLearnable = !tm.IsLearnable;
            e.Handled = true;
        }
    }
}
