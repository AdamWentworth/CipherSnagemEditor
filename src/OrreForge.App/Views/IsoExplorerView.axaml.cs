using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class IsoExplorerView : UserControl
{
    public IsoExplorerView()
    {
        InitializeComponent();
    }

    private void IsoFilePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: IsoFileEntryViewModel file }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedIsoFile = file;
            e.Handled = true;
        }
    }
}
