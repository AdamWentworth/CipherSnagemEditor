using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CipherSnagemEditor.App.ViewModels;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.App.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Window> _toolWindows = [];
    private int _toolWindowOffset;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                ApplyWindowIcon(viewModel.MainWindowIconResource);
            }
        };
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
            Title = "Open Cipher Snagem Editor File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Cipher Snagem Editor Files")
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

    public Task OpenPathFromStartupAsync(string path)
        => OpenPathAsync(path);

    private async Task OpenPathAsync(string path)
    {
        CloseToolWindows();
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        await viewModel.OpenPathAsync(path);
        ApplyWindowIcon(viewModel.MainWindowIconResource);
    }

    private void OpenToolWindow(ToolEntryViewModel tool, MainWindowViewModel viewModel)
    {
        var totalTimer = Stopwatch.StartNew();
        if (_toolWindows.TryGetValue(tool.Title, out var existing))
        {
            existing.Activate();
            viewModel.LogPerformance($"{tool.Title} activate existing window", totalTimer);
            return;
        }

        var contentTimer = Stopwatch.StartNew();
        var content = CreateToolContent(tool);
        viewModel.LogPerformance($"{tool.Title} create content", contentTimer);
        content.DataContext = viewModel;
        ViewPerformanceDiagnostics.AttachFirstRenderLogs(content, $"{tool.Title} content");

        var size = ToolWindowSize(tool);
        var minSize = ToolWindowMinSize(tool);
        var windowBuildTimer = Stopwatch.StartNew();
        var window = new Window
        {
            Title = $"{tool.Title} - {tool.LegacyToolName}",
            Width = size.Width,
            Height = size.Height,
            MinWidth = minSize.Width,
            MinHeight = minSize.Height,
            CanResize = true,
            FontFamily = FontFamily,
            Background = SolidColorBrush.Parse("#F0F0FC"),
            WindowDecorations = Avalonia.Controls.WindowDecorations.None,
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaTitleBarHeightHint = 24,
            DataContext = viewModel
        };
        window.Content = CreateToolWindowShell(window, tool.Title, content);
        ViewPerformanceDiagnostics.AttachFirstRenderLogs(window, $"{tool.Title} window");
        viewModel.LogPerformance($"{tool.Title} build window shell", windowBuildTimer, ViewPerformanceDiagnostics.CountVisuals(window));

        if (Icon is not null)
        {
            window.Icon = Icon;
        }

        var offset = 28 + (_toolWindowOffset++ % 6 * 24);
        window.Position = new PixelPoint(Position.X + offset, Position.Y + offset);
        window.Closed += (_, _) => _toolWindows.Remove(tool.Title);
        _toolWindows[tool.Title] = window;

        var showTimer = Stopwatch.StartNew();
        window.Opened += (_, _) =>
        {
            viewModel.LogPerformance($"{tool.Title} opened", showTimer, ViewPerformanceDiagnostics.CountVisuals(window));
            var renderAfterOpenTimer = Stopwatch.StartNew();
            Dispatcher.UIThread.Post(
                () => viewModel.LogPerformance($"{tool.Title} after open render queue", renderAfterOpenTimer, ViewPerformanceDiagnostics.CountVisuals(window)),
                DispatcherPriority.Render);
        };

        window.Show(this);
        window.Activate();
        viewModel.LogPerformance($"{tool.Title} open tool window total", totalTimer, ViewPerformanceDiagnostics.CountVisuals(window));
    }

    private static Control CreateToolContent(ToolEntryViewModel tool)
    {
        if (tool.Game == GameCubeGame.PokemonXD)
        {
            return tool.Title switch
            {
                "Trainer Editor" => new TrainerEditorView(),
                "Shadow Pokemon Editor" => new XdShadowPokemonEditorView(),
                "Pokemon Stats Editor" => new PokemonStatsEditorView(),
                "Move Editor" => new MoveEditorView(),
                "Item Editor" => new ItemEditorView(),
                "Pokespot Editor" => new XdPokespotEditorView(),
                "Type Editor" => new TypeEditorView(),
                "Treasure Editor" => new TreasureEditorView(),
                "ISO Explorer" => new IsoExplorerView(),
                _ => new XdToolView()
            };
        }

        return tool.Title switch
        {
            "Trainer Editor" => new TrainerEditorView(),
            "Pokemon Stats Editor" => new PokemonStatsEditorView(),
            "Move Editor" => new MoveEditorView(),
            "Item Editor" => new ItemEditorView(),
            "Gift Pokemon Editor" => new GiftPokemonEditorView(),
            "Type Editor" => new TypeEditorView(),
            "Treasure Editor" => new TreasureEditorView(),
            "Patches" => new PatchEditorView(),
            "Randomizer" => new RandomizerView(),
            "Message Editor" => new MessageEditorView(),
            "Collision Viewer" => new CollisionViewerView(),
            "Interaction Editor" => new InteractionEditorView(),
            "Vertex Filters" => new VertexFiltersView(),
            "Table Editor" => new TableEditorView(),
            "ISO Explorer" => new IsoExplorerView(),
            _ => new LegacySimpleToolView(tool.Title, ToolContentSize(tool.Title))
        };
    }

    private void ApplyWindowIcon(string resourceUri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(resourceUri));
            Icon = new WindowIcon(stream);
        }
        catch
        {
            // Keep the packaged executable icon if the resource cannot be loaded.
        }
    }

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
        AddResizeHitTargets(root);
        return root;
    }

    private static void AddResizeHitTargets(Grid root)
    {
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeN, HorizontalAlignment.Stretch, VerticalAlignment.Top, new Thickness(6, 0, 144, 0), double.NaN, 6);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeS, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, new Thickness(6, 0), double.NaN, 6);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeW, HorizontalAlignment.Left, VerticalAlignment.Stretch, new Thickness(0, 24, 0, 6), 6, double.NaN);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeE, HorizontalAlignment.Right, VerticalAlignment.Stretch, new Thickness(0, 24, 0, 6), 6, double.NaN);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeNW, HorizontalAlignment.Left, VerticalAlignment.Top, new Thickness(0), 6, 6);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeNE, HorizontalAlignment.Right, VerticalAlignment.Top, new Thickness(0, 0, 138, 0), 6, 6);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeSW, HorizontalAlignment.Left, VerticalAlignment.Bottom, new Thickness(0), 12, 12);
        AddResizeHitTarget(root, WindowDecorationsElementRole.ResizeSE, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0), 12, 12);
    }

    private static void AddResizeHitTarget(
        Grid root,
        WindowDecorationsElementRole role,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        Thickness margin,
        double width,
        double height)
    {
        var hitTarget = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            Margin = margin,
            Width = width,
            Height = height,
            IsHitTestVisible = true
        };
        WindowDecorationProperties.SetElementRole(hitTarget, role);
        Grid.SetRowSpan(hitTarget, 2);
        root.Children.Add(hitTarget);
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

    private static Size ToolWindowSize(ToolEntryViewModel tool)
    {
        if (tool.Game == GameCubeGame.PokemonXD)
        {
            return tool.Title switch
            {
                "Trainer Editor" => new Size(1280, 724),
                "Shadow Pokemon Editor" => new Size(620, 430),
                "Pokespot Editor" => new Size(560, 430),
                "Gift Pokemon Editor" => new Size(560, 354),
                "Script Compiler" => new Size(640, 504),
                _ => ToolWindowSize(tool.Title)
            };
        }

        return ToolWindowSize(tool.Title);
    }

    private static Size ToolWindowSize(string title)
        => title switch
        {
            "Trainer Editor" => new Size(1420, 760),
            "Pokemon Stats Editor" => new Size(910, 571),
            "Move Editor" => new Size(874, 504),
            "Item Editor" => new Size(860, 504),
            "Gift Pokemon Editor" => new Size(560, 354),
            "Type Editor" => new Size(640, 540),
            "Treasure Editor" => new Size(642, 410),
            "Patches" => new Size(400, 504),
            "Randomizer" => new Size(390, 529),
            "Message Editor" => new Size(680, 324),
            "Collision Viewer" => new Size(840, 504),
            "Interaction Editor" => new Size(530, 449),
            "Vertex Filters" => new Size(580, 690),
            "Table Editor" => new Size(560, 590),
            "ISO Explorer" => new Size(570, 424),
            _ => new Size(620, 320)
        };

    private static Size ToolWindowMinSize(ToolEntryViewModel tool)
    {
        var size = ToolWindowSize(tool);
        return tool.Title == "Trainer Editor"
            ? size
            : new Size(Math.Min(size.Width, 900), Math.Min(size.Height, 560));
    }

    private static Size ToolWindowMinSize(string title)
    {
        var size = ToolWindowSize(title);
        return title == "Trainer Editor"
            ? size
            : new Size(Math.Min(size.Width, 900), Math.Min(size.Height, 560));
    }

    private static Size ToolContentSize(string title)
    {
        var size = ToolWindowSize(title);
        return new Size(size.Width, Math.Max(0, size.Height - 24));
    }

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
