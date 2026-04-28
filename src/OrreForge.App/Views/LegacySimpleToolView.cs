using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace OrreForge.App.Views;

public sealed class LegacySimpleToolView : UserControl
{
    private static readonly IBrush DarkBackground = SolidColorBrush.Parse("#303030");
    private static readonly IBrush PanelBorder = SolidColorBrush.Parse("#545454");
    private static readonly IBrush LightText = SolidColorBrush.Parse("#E6E6E6");
    private static readonly IBrush FieldBackground = SolidColorBrush.Parse("#666666");

    public LegacySimpleToolView(string title, Size contentSize)
    {
        FontFamily = new FontFamily("Arial");
        Background = DarkBackground;
        Width = contentSize.Width;
        Height = contentSize.Height;
        Content = BuildContent(title, contentSize);
    }

    private static Control BuildContent(string title, Size size)
        => title switch
        {
            "Patches" => BuildPatches(size),
            "Randomizer" => BuildRandomizer(size),
            "Collision Viewer" => BuildCollision(size),
            "Interaction Editor" => BuildInteractions(size),
            "Vertex Filters" => BuildVertexFilters(size),
            "Table Editor" => BuildTableEditor(size),
            _ => BuildGeneric(title, size)
        };

    private static Control BuildPatches(Size size)
    {
        var list = new StackPanel();
        var rows = new[]
        {
            "Apply the gen IV physical/special split and set moves to their default category",
            "Disables some save file checks to prevent the save from being corrupted",
            "Adds the ability to soft reset using B + X + Start button combo",
            "Press R in the overworld to open the PC menu from anywhere (Make sure you don't softlock yourself)",
            "Remove shiny locks from gift pokemon (espeon, umbreon, plusle, pikachu, celebi, hooh)",
            "Allow starter pokemon to be female",
            "TMs can be reused infinitely",
            "Gen 6+ critical hit multiplier (1.5x)",
            "Gen 7+ critical hit probablities",
            "Trade evolutions become level 40",
            "Evolution stone evolutions become level 40",
            "Starter pokemon can be shiny",
            "Starter pokemon can never be shiny",
            "Starter pokemon are always shiny",
            "Enable Debug Logs (Only useful for script development)",
            "When a shadow pokemon has locked moves the move doesn't show the ??? type icon",
            "Any pokemon can learn any TM",
            "All pokemon have the maximum catch rate of 255",
            "Set all battles to single battles",
            "Set all battles to double battles",
            "Modify the ASM so it allows any region's colbtl.bin to be imported. Trades will be locked to whichever region's colbtl.bin was imported"
        };
        for (var index = 0; index < rows.Length; index++)
        {
            list.Children.Add(new Border
            {
                Height = 48,
                Background = SolidColorBrush.Parse(index % 2 == 0 ? "#FFFFFF" : "#DADADA"),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = new TextBlock
                {
                    Text = rows[index],
                    Foreground = Brushes.Black,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        return new Grid
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground,
            Children =
            {
                new Border
                {
                    Margin = new Thickness(8),
                    Background = SolidColorBrush.Parse("#222222"),
                    Child = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                        Content = list
                    }
                }
            }
        };
    }

    private static Control BuildRandomizer(Size size)
    {
        var panel = new WrapPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(20, 18, 20, 54),
            Children =
            {
                LegacyCheck("Randomise Starter Pokemon"),
                LegacyCheck("Shadow Pokemon"),
                LegacyCheck("Randomise Unobtainable Pokemon"),
                LegacyCheck("Randomise Pokemon Moves"),
                LegacyCheck("Randomise Pokemon Types"),
                LegacyCheck("Randomise Pokemon Abilities"),
                LegacyCheck("Randomise Pokemon Stats"),
                LegacyCheck("Randomise Evolutions"),
                LegacyCheck("Randomise Move Types"),
                LegacyCheck("Randomise TMs"),
                LegacyCheck("Randomise Items"),
                LegacyCheck("Randomise Type Matchups"),
                LegacyCheck("Randomise Shops"),
                LegacyCheck("Randomise by BST"),
                LegacyCheck("Remove Trade Evolutions")
            }
        };

        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        canvas.Children.Add(panel);
        Canvas.SetLeft(panel, 0);
        Canvas.SetTop(panel, 0);
        var randomise = LegacyButton("Randomise");
        Canvas.SetLeft(randomise, (size.Width - 120) / 2);
        Canvas.SetTop(randomise, size.Height - 42);
        canvas.Children.Add(randomise);
        return canvas;
    }

    private static Control BuildCollision(Size size)
    {
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        AddPanel(canvas, 10, 10, 190, size.Height - 20, "Rooms", ["Room", "Collision", "Export"]);
        AddPanel(canvas, 210, 10, size.Width - 220, size.Height - 20, "Collision Viewer", ["X", "Y", "Z", "Faces", "Materials"]);
        return canvas;
    }

    private static Control BuildInteractions(Size size)
    {
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        AddPanel(canvas, 10, 10, 190, size.Height - 20, "Files", ["common.rel", "script FSYS", "room data"]);
        AddPanel(canvas, 210, 10, size.Width - 220, 150, "Interaction", ["Character", "Script", "Flags"]);
        AddPanel(canvas, 210, 170, size.Width - 220, size.Height - 180, "Script Preview", ["Function", "Parameter", "Message"]);
        return canvas;
    }

    private static Control BuildVertexFilters(Size size)
    {
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        AddPanel(canvas, 10, 10, 180, size.Height - 20, "Filters", ["Current filter", "Rooms", "Models", "Materials"]);
        AddPanel(canvas, 200, 10, size.Width - 210, 170, "Search", ["Name", "Index", "Vertex Group"]);
        AddPanel(canvas, 200, 190, size.Width - 210, size.Height - 200, "Results", ["Index", "Vertices", "Texture"]);
        return canvas;
    }

    private static Control BuildTableEditor(Size size)
    {
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        AddPanel(canvas, 10, 10, 180, size.Height - 20, "Tables", ["Save Data", "E-Reader", "Common", "Deck Pokemon", "Deck Trainer", "Deck AI", "Other"]);
        AddPanel(canvas, 200, 10, size.Width - 210, 72, "Table", ["Decode", "Encode", "Document"]);
        AddPanel(canvas, 200, 92, size.Width - 210, size.Height - 102, "Fields", ["Offset", "Type", "Value"]);
        return canvas;
    }

    private static Control BuildGeneric(string title, Size size)
    {
        var canvas = new Canvas
        {
            Width = size.Width,
            Height = size.Height,
            Background = DarkBackground
        };
        AddPanel(canvas, 10, 10, size.Width - 20, size.Height - 20, title, ["Legacy shell is ready."]);
        return canvas;
    }

    private static void AddPanel(Canvas canvas, double left, double top, double width, double height, string title, IReadOnlyList<string> rows)
    {
        var stack = new StackPanel { Margin = new Thickness(10), Spacing = 8 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = LightText,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        });
        foreach (var row in rows)
        {
            stack.Children.Add(new TextBlock
            {
                Text = row,
                Foreground = LightText,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
        }

        var border = new Border
        {
            Width = width,
            Height = height,
            Background = DarkBackground,
            BorderBrush = PanelBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = stack
        };
        Canvas.SetLeft(border, left);
        Canvas.SetTop(border, top);
        canvas.Children.Add(border);
    }

    private static CheckBox LegacyCheck(string text)
        => new()
        {
            Content = text,
            Foreground = LightText,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 10),
            Width = 330,
            Height = 22
        };

    private static Button LegacyButton(string text)
        => new()
        {
            Content = text,
            Width = 120,
            Height = 30,
            Background = FieldBackground,
            BorderBrush = SolidColorBrush.Parse("#7B7B7B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(0)
        };
}
