using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class CollisionViewerView : UserControl
{
    public CollisionViewerView()
    {
        InitializeComponent();
    }

    private void CollisionFilePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: CollisionFileEntryViewModel file }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedCollisionFile = file;
            e.Handled = true;
        }
    }
}
