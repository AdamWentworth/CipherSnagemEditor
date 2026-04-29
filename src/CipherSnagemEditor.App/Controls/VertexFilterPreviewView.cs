using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.Controls;

public sealed class VertexFilterPreviewView : Control
{
    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<VertexFilterPreviewView, string?>(nameof(FilePath));

    public static readonly StyledProperty<int> FilterValueProperty =
        AvaloniaProperty.Register<VertexFilterPreviewView, int>(nameof(FilterValue));

    private static readonly IBrush EmptyBackgroundBrush = SolidColorBrush.Parse("#1F1F1F");
    private static readonly IPen BorderPen = new Pen(SolidColorBrush.Parse("#505050"), 1);
    private static readonly Dictionary<string, CachedProfile> ProfileCache = new(StringComparer.OrdinalIgnoreCase);

    static VertexFilterPreviewView()
    {
        AffectsRender<VertexFilterPreviewView>(FilePathProperty, FilterValueProperty);
    }

    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public int FilterValue
    {
        get => GetValue(FilterValueProperty);
        set => SetValue(FilterValueProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(EmptyBackgroundBrush, bounds);

        var colours = ColoursFor(FilePath, (ColosseumVertexColorFilter)FilterValue);
        if (colours.Count == 0)
        {
            DrawEmptyPreview(context, bounds);
            context.DrawRectangle(null, BorderPen, bounds);
            return;
        }

        var columns = Math.Max(1, (int)Math.Sqrt(colours.Count));
        var rows = Math.Max(1, (int)Math.Ceiling(colours.Count / (double)columns));
        var cellWidth = bounds.Width / columns;
        var cellHeight = bounds.Height / rows;

        for (var index = 0; index < colours.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            context.FillRectangle(
                new SolidColorBrush(colours[index]),
                new Rect(column * cellWidth, row * cellHeight, Math.Ceiling(cellWidth) + 0.5, Math.Ceiling(cellHeight) + 0.5));
        }

        context.DrawRectangle(null, BorderPen, bounds);
    }

    public static void ClearCache(string? path)
    {
        if (path is not null)
        {
            ProfileCache.Remove(path);
        }
    }

    private static IReadOnlyList<Color> ColoursFor(string? path, ColosseumVertexColorFilter filter)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return [];
        }

        var info = new FileInfo(path);
        if (ProfileCache.TryGetValue(path, out var cached)
            && cached.Length == info.Length
            && cached.LastWriteTicks == info.LastWriteTimeUtc.Ticks)
        {
            return ApplyFilter(cached.Colours, filter);
        }

        var model = ColosseumDatVertexColorModel.Load(path);
        var colours = model.VertexColors
            .OrderByDescending(Hue)
            .ThenByDescending(Saturation)
            .ThenByDescending(Value)
            .ThenByDescending(colour => colour.Alpha)
            .ToArray();
        ProfileCache[path] = new CachedProfile(info.Length, info.LastWriteTimeUtc.Ticks, colours);
        return ApplyFilter(colours, filter);
    }

    private static IReadOnlyList<Color> ApplyFilter(
        IReadOnlyList<ColosseumDatVertexColor> colours,
        ColosseumVertexColorFilter filter)
    {
        return colours
            .Select(colour => ColosseumDatVertexColorModel.ApplyFilter(colour, filter))
            .Select(colour => Color.FromArgb((byte)colour.Alpha, (byte)colour.Red, (byte)colour.Green, (byte)colour.Blue))
            .ToArray();
    }

    private static double Hue(ColosseumDatVertexColor colour)
    {
        var red = colour.Red / 255d;
        var green = colour.Green / 255d;
        var blue = colour.Blue / 255d;
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;
        if (delta == 0)
        {
            return 0;
        }

        var hue = max == red
            ? ((green - blue) / delta) % 6
            : max == green
                ? ((blue - red) / delta) + 2
                : ((red - green) / delta) + 4;

        return hue * 60 < 0 ? (hue * 60) + 360 : hue * 60;
    }

    private static double Saturation(ColosseumDatVertexColor colour)
    {
        var red = colour.Red / 255d;
        var green = colour.Green / 255d;
        var blue = colour.Blue / 255d;
        var max = Math.Max(red, Math.Max(green, blue));
        return max == 0 ? 0 : (max - Math.Min(red, Math.Min(green, blue))) / max;
    }

    private static double Value(ColosseumDatVertexColor colour)
        => Math.Max(colour.Red, Math.Max(colour.Green, colour.Blue));

    private static void DrawEmptyPreview(DrawingContext context, Rect bounds)
    {
        var center = bounds.Center;
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.26;
        context.DrawEllipse(SolidColorBrush.Parse("#303030"), null, center, radius, radius);
        context.DrawEllipse(SolidColorBrush.Parse("#555555"), null, center, radius * 0.62, radius * 0.62);
        context.DrawEllipse(SolidColorBrush.Parse("#222222"), null, center, radius * 0.3, radius * 0.3);
        context.DrawLine(new Pen(SolidColorBrush.Parse("#8A8A8A"), 2), new Point(center.X - radius, center.Y), new Point(center.X + radius, center.Y));
    }

    private sealed record CachedProfile(long Length, long LastWriteTicks, IReadOnlyList<ColosseumDatVertexColor> Colours);
}
