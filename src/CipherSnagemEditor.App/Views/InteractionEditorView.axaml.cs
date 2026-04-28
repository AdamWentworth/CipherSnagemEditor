using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class InteractionEditorView : UserControl
{
    public InteractionEditorView()
    {
        InitializeComponent();
    }

    private void InteractionPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: InteractionEntryViewModel interaction }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedInteraction = interaction;
            e.Handled = true;
        }
    }
}
