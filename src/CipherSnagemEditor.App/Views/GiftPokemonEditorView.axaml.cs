using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class GiftPokemonEditorView : UserControl
{
    public GiftPokemonEditorView()
    {
        InitializeComponent();
    }

    private void GiftPokemonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: GiftPokemonEntryViewModel gift }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedGiftPokemon = gift;
            e.Handled = true;
        }
    }
}
