using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class TreasureEditorView : UserControl
{
    public TreasureEditorView()
    {
        InitializeComponent();
    }

    private void TreasurePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TreasureEntryViewModel treasure }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTreasure = treasure;
            e.Handled = true;
        }
    }
}
