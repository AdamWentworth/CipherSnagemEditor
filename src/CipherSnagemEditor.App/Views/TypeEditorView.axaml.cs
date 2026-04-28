using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class TypeEditorView : UserControl
{
    public TypeEditorView()
    {
        InitializeComponent();
    }

    private void TypePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TypeEntryViewModel type }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedType = type;
            e.Handled = true;
        }
    }
}
