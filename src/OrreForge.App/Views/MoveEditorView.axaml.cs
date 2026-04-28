using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class MoveEditorView : UserControl
{
    public MoveEditorView()
    {
        InitializeComponent();
    }

    private void MovePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: MoveEntryViewModel move }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedMove = move;
            e.Handled = true;
        }
    }
}
