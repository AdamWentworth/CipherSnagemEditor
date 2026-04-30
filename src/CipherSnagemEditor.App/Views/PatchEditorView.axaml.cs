using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class PatchEditorView : UserControl
{
    public PatchEditorView()
    {
        InitializeComponent();
    }

    private void PatchPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PatchEntryViewModel patch }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedPatch = patch;
            if (viewModel.ApplyPatchCommand.CanExecute(patch))
            {
                viewModel.ApplyPatchCommand.Execute(patch);
            }

            e.Handled = true;
        }
    }
}
