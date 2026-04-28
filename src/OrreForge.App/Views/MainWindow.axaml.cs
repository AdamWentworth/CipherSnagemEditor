using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using Avalonia.Platform.Storage;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Window> _toolWindows = [];
    private int _toolWindowOffset;

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

    private void MainTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MainCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MainZoomClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
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
            if (viewModel.PrepareToolWindow(tool))
            {
                OpenToolWindow(tool, viewModel);
            }

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
        CloseToolWindows();
        return DataContext is MainWindowViewModel viewModel
            ? viewModel.OpenPathAsync(path)
            : Task.CompletedTask;
    }

    private void OpenToolWindow(ToolEntryViewModel tool, MainWindowViewModel viewModel)
    {
        if (_toolWindows.TryGetValue(tool.Title, out var existing))
        {
            existing.Activate();
            return;
        }

        var content = CreateToolContent(tool);
        content.DataContext = viewModel;
        var size = ToolWindowSize(tool.Title);
        var window = new Window
        {
            Title = $"{tool.Title} - Colosseum Tool",
            Width = size.Width,
            Height = size.Height,
            MinWidth = Math.Min(size.Width, 900),
            MinHeight = Math.Min(size.Height, 560),
            FontFamily = FontFamily,
            Background = SolidColorBrush.Parse("#F0F0FC"),
            WindowDecorations = Avalonia.Controls.WindowDecorations.None,
            DataContext = viewModel
        };
        window.Content = CreateToolWindowShell(window, tool.Title, content);

        if (Icon is not null)
        {
            window.Icon = Icon;
        }

        var offset = 28 + (_toolWindowOffset++ % 6 * 24);
        window.Position = new PixelPoint(Position.X + offset, Position.Y + offset);
        window.Closed += (_, _) => _toolWindows.Remove(tool.Title);
        _toolWindows[tool.Title] = window;
        window.Show(this);
        window.Activate();
    }

    private static Control CreateToolContent(ToolEntryViewModel tool)
        => tool.Title switch
        {
            "Trainer Editor" => new TrainerEditorView(),
            "Pokemon Stats Editor" => new PokemonStatsEditorView(),
            "Move Editor" => new MoveEditorView(),
            "ISO Explorer" => new IsoExplorerView(),
            _ => CreatePlaceholderContent(tool)
        };

    private static Control CreateToolWindowShell(Window window, string title, Control content)
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("24,*"),
            Background = SolidColorBrush.Parse("#242424")
        };

        var titleBar = new Border
        {
            Background = SolidColorBrush.Parse("#3A3A3A"),
            Height = 24
        };
        WindowDecorationProperties.SetElementRole(titleBar, WindowDecorationsElementRole.TitleBar);
        titleBar.PointerPressed += (_, e) =>
        {
            if (e.Source is Button)
            {
                return;
            }

            if (e.GetCurrentPoint(titleBar).Properties.IsLeftButtonPressed)
            {
                window.BeginMoveDrag(e);
            }
        };

        var titleLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("138,*,138")
        };

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = SolidColorBrush.Parse("#CFCFCF"),
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 1);
        titleLayout.Children.Add(titleText);

        var windowControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        windowControls.Children.Add(CreateTitleButton(TitleButtonKind.Minimize, () => window.WindowState = WindowState.Minimized));
        windowControls.Children.Add(CreateTitleButton(TitleButtonKind.Maximize, () =>
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }));
        windowControls.Children.Add(CreateTitleButton(TitleButtonKind.Close, window.Close));
        Grid.SetColumn(windowControls, 2);
        titleLayout.Children.Add(windowControls);

        titleBar.Child = titleLayout;
        root.Children.Add(titleBar);

        Grid.SetRow(content, 1);
        root.Children.Add(content);
        return root;
    }

    private static Button CreateTitleButton(TitleButtonKind kind, Action action)
    {
        var role = kind switch
        {
            TitleButtonKind.Minimize => WindowDecorationsElementRole.MinimizeButton,
            TitleButtonKind.Maximize => WindowDecorationsElementRole.MaximizeButton,
            _ => WindowDecorationsElementRole.CloseButton
        };
        var button = new Button
        {
            Content = CreateTitleButtonIcon(kind),
            Width = 46,
            Height = 24,
            MinHeight = 0,
            Padding = new Thickness(0),
            FontSize = 12,
            Foreground = SolidColorBrush.Parse("#DCDCDC"),
            Background = SolidColorBrush.Parse("#3A3A3A"),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        WindowDecorationProperties.SetElementRole(button, role);
        button.PointerEntered += (_, _) => button.Background = SolidColorBrush.Parse(kind == TitleButtonKind.Close ? "#E81123" : "#505050");
        button.PointerExited += (_, _) => button.Background = SolidColorBrush.Parse("#3A3A3A");
        button.Click += (_, _) => action();

        return button;
    }

    private static Control CreateTitleButtonIcon(TitleButtonKind kind)
    {
        var foreground = SolidColorBrush.Parse("#DCDCDC");
        return kind switch
        {
            TitleButtonKind.Minimize => new Border
            {
                Width = 12,
                Height = 1,
                Background = foreground,
                VerticalAlignment = VerticalAlignment.Center
            },
            TitleButtonKind.Maximize => new Border
            {
                Width = 12,
                Height = 12,
                BorderBrush = foreground,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent
            },
            _ => CreateCloseIcon(foreground)
        };
    }

    private static Control CreateCloseIcon(IBrush foreground)
        => new Avalonia.Controls.Shapes.Path
        {
            Width = 12,
            Height = 12,
            Stretch = Stretch.None,
            Stroke = foreground,
            StrokeThickness = 1.2,
            StrokeLineCap = PenLineCap.Square,
            Data = Geometry.Parse("M 2,2 L 10,10 M 10,2 L 2,10")
        };

    private enum TitleButtonKind
    {
        Minimize,
        Maximize,
        Close
    }

    private static Size ToolWindowSize(string title)
        => title switch
        {
            "Trainer Editor" => new Size(1420, 760),
            "Pokemon Stats Editor" => new Size(910, 571),
            "Move Editor" => new Size(874, 504),
            "ISO Explorer" => new Size(900, 650),
            _ => new Size(620, 320)
        };

    private static Control CreatePlaceholderContent(ToolEntryViewModel tool)
        => new Border
        {
            Background = Brushes.White,
            BorderBrush = SolidColorBrush.Parse("#C0C0C8"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18),
            Margin = new Thickness(18),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = tool.Title,
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.Black
                    },
                    new TextBlock
                    {
                        Text = $"Legacy segue: {tool.LegacySegue}{Environment.NewLine}Reference: {tool.LegacySource}",
                        Foreground = Brushes.Black,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

    private void CloseToolWindows()
    {
        foreach (var window in _toolWindows.Values.ToArray())
        {
            window.Close();
        }

        _toolWindows.Clear();
    }
}
