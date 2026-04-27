using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private async void OpenFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Colosseum Tool File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Colosseum Tool Files")
                {
                    Patterns = ["*.iso", "*.fsys", "*.msg", "*.gtx", "*.atx"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            ]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
        {
            await OpenPathAsync(path);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            return;
        }

        var path = e.DataTransfer.TryGetFiles()?.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
        {
            await OpenPathAsync(path);
        }
    }

    private void ToolPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ToolEntryViewModel tool }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTool = tool;
            e.Handled = true;
        }
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

    private void TrainerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TrainerEntryViewModel trainer }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedTrainer = trainer;
            e.Handled = true;
        }
    }

    private void PokemonStatsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PokemonStatsEntryViewModel pokemon }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedPokemonStats = pokemon;
            e.Handled = true;
        }
    }

    private Task OpenPathAsync(string path)
    {
        return DataContext is MainWindowViewModel viewModel
            ? viewModel.OpenPathAsync(path)
            : Task.CompletedTask;
    }
}
