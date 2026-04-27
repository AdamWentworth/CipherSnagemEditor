using Avalonia.Controls;
using Avalonia.Input;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class TrainerEditorView : UserControl
{
    public TrainerEditorView()
    {
        InitializeComponent();
    }

    private void TrainerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TrainerEntryViewModel trainer }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTrainer = trainer;
            e.Handled = true;
        }
    }
}
