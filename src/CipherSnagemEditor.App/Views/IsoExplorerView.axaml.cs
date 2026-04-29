using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

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

    private async void AddFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel
            || !viewModel.CanAddFileToSelectedIsoFile)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Add File to FSYS",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("FSYS Inner Files")
                {
                    Patterns = ["*.bin", "*.dat", "*.rdat", "*.ccd", "*.msg", "*.gtx", "*.atx", "*.rel", "*.pkx", "*.wzx", "*.thh", "*.thd", "*.gsw"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            ]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        if (topLevel is not Window owner)
        {
            return;
        }

        var identifier = await ShowIdentifierPromptAsync(owner);
        if (identifier is null)
        {
            return;
        }

        await viewModel.AddFileToSelectedFsysAsync(path, identifier);
        e.Handled = true;
    }

    private static async Task<string?> ShowIdentifierPromptAsync(Window owner)
    {
        var input = new TextBox
        {
            Width = 160,
            Height = 28,
            MaxLength = 6,
            Text = string.Empty,
            PlaceholderText = "0001",
            Background = SolidColorBrush.Parse("#333333"),
            Foreground = Brushes.White,
            CaretBrush = Brushes.White,
            SelectionBrush = SolidColorBrush.Parse("#80ACFF"),
            BorderBrush = SolidColorBrush.Parse("#6A6A6A"),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var ok = DialogButton("OK");
        var cancel = DialogButton("Cancel");
        var dialog = new Window
        {
            Title = "Input file identifier",
            Width = 390,
            Height = 165,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = SolidColorBrush.Parse("#303030"),
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(14),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Select a unique identifier for the new file. Use 4 hexadecimal digits.",
                        Foreground = SolidColorBrush.Parse("#E6E6E6"),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    },
                    input,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, ok }
                    }
                }
            }
        };

        ok.Click += (_, _) => dialog.Close(input.Text);
        cancel.Click += (_, _) => dialog.Close(null);
        input.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                dialog.Close(input.Text);
                args.Handled = true;
            }
        };

        dialog.Opened += (_, _) => input.Focus();
        return await dialog.ShowDialog<string?>(owner);
    }

    private static Button DialogButton(string text)
        => new()
        {
            Content = text,
            Width = 82,
            Height = 26,
            MinHeight = 0,
            Padding = new Avalonia.Thickness(0),
            Background = SolidColorBrush.Parse("#707070"),
            BorderBrush = SolidColorBrush.Parse("#7B7B7B"),
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
}
