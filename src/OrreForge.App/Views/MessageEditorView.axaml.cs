using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class MessageEditorView : UserControl
{
    public MessageEditorView()
    {
        InitializeComponent();
    }

    private void MessageStringPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: MessageStringEntryViewModel message }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedMessageString = message;
            e.Handled = true;
        }
    }
}
