using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

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
}
