using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class TableEditorView : UserControl
{
    public TableEditorView()
    {
        InitializeComponent();
    }

    private void TableEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TableEditorEntryViewModel table }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTableEditorEntry = table;
            e.Handled = true;
        }
    }
}
