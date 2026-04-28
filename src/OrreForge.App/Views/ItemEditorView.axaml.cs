using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class ItemEditorView : UserControl
{
    public ItemEditorView()
    {
        InitializeComponent();
    }

    private void ItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ItemEntryViewModel item }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedItem = item;
            e.Handled = true;
        }
    }
}
