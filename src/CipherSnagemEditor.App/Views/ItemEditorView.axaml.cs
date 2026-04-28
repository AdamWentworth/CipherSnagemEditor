using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

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
