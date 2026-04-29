using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class VertexFiltersView : UserControl
{
    public VertexFiltersView()
    {
        InitializeComponent();
    }

    private void VertexFilterFilePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: VertexFilterFileEntryViewModel file }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedVertexFilterFile = file;
            e.Handled = true;
        }
    }
}
