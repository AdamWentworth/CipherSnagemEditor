using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.Controls;

public sealed class CollisionRenderView : Control
{
    public static readonly StyledProperty<ColosseumCollisionData?> CollisionDataProperty =
        AvaloniaProperty.Register<CollisionRenderView, ColosseumCollisionData?>(nameof(CollisionData));

    public static readonly StyledProperty<int> HighlightInteractionProperty =
        AvaloniaProperty.Register<CollisionRenderView, int>(nameof(HighlightInteraction), -1);

    public static readonly StyledProperty<int> HighlightSectionProperty =
        AvaloniaProperty.Register<CollisionRenderView, int>(nameof(HighlightSection), -1);

    private static readonly IBrush BackgroundBrush = SolidColorBrush.Parse("#B8B8B8");
    private static readonly IPen EdgePen = new Pen(SolidColorBrush.Parse("#1E1E1E"), 0.8);
    private static readonly IPen HighlightPen = new Pen(SolidColorBrush.Parse("#FFFFFF"), 1.4);
    private static readonly IBrush NormalFaceBrush = new SolidColorBrush(Color.FromArgb(105, 72, 72, 72));
    private static readonly IBrush InteractableBrush = new SolidColorBrush(Color.FromArgb(145, 146, 95, 245));
    private static readonly IBrush InteractionHighlightBrush = new SolidColorBrush(Color.FromArgb(210, 252, 180, 72));
    private static readonly IBrush SectionHighlightBrush = new SolidColorBrush(Color.FromArgb(185, 104, 220, 255));

    private Point? _lastPointer;
    private double _yaw = -0.55;
    private double _pitch = 0.62;
    private double _zoom = 0.92;

    static CollisionRenderView()
    {
        AffectsRender<CollisionRenderView>(
            CollisionDataProperty,
            HighlightInteractionProperty,
            HighlightSectionProperty);
    }

    public CollisionRenderView()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public ColosseumCollisionData? CollisionData
    {
        get => GetValue(CollisionDataProperty);
        set => SetValue(CollisionDataProperty, value);
    }

    public int HighlightInteraction
    {
        get => GetValue(HighlightInteractionProperty);
        set => SetValue(HighlightInteractionProperty, value);
    }

    public int HighlightSection
    {
        get => GetValue(HighlightSectionProperty);
        set => SetValue(HighlightSectionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(BackgroundBrush, bounds);

        var data = CollisionData;
        if (data is null || data.Triangles.Count == 0)
        {
            return;
        }

        var projected = data.Triangles
            .Select(triangle => new ProjectedTriangle(
                triangle,
                Project(triangle.A, bounds),
                Project(triangle.B, bounds),
                Project(triangle.C, bounds)))
            .OrderBy(triangle => triangle.Depth)
            .ToArray();

        foreach (var triangle in projected)
        {
            var geometry = new StreamGeometry();
            using (var stream = geometry.Open())
            {
                stream.BeginFigure(triangle.A, true);
                stream.LineTo(triangle.B);
                stream.LineTo(triangle.C);
                stream.EndFigure(true);
            }

            var brush = BrushFor(triangle.Source);
            var pen = IsHighlighted(triangle.Source) ? HighlightPen : EdgePen;
            context.DrawGeometry(brush, pen, geometry);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _lastPointer = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_lastPointer is null || e.Pointer.Captured != this)
        {
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _lastPointer.Value;
        _yaw += delta.X * 0.01;
        _pitch = Math.Clamp(_pitch + delta.Y * 0.01, -1.45, 1.45);
        _lastPointer = current;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _lastPointer = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _zoom = Math.Clamp(_zoom + e.Delta.Y * 0.08, 0.25, 2.5);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Space:
                _yaw = -0.55;
                _pitch = 0.62;
                _zoom = 0.92;
                break;
            case Key.Left:
                _yaw -= 0.08;
                break;
            case Key.Right:
                _yaw += 0.08;
                break;
            case Key.Up:
                _pitch = Math.Clamp(_pitch - 0.08, -1.45, 1.45);
                break;
            case Key.Down:
                _pitch = Math.Clamp(_pitch + 0.08, -1.45, 1.45);
                break;
            case Key.OemPlus:
            case Key.Add:
                _zoom = Math.Clamp(_zoom + 0.1, 0.25, 2.5);
                break;
            case Key.OemMinus:
            case Key.Subtract:
                _zoom = Math.Clamp(_zoom - 0.1, 0.25, 2.5);
                break;
            default:
                return;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    private Point Project(ColosseumCollisionVertex vertex, Rect bounds)
    {
        var x = vertex.X;
        var y = vertex.Y;
        var z = vertex.Z;

        var cosYaw = Math.Cos(_yaw);
        var sinYaw = Math.Sin(_yaw);
        var yawX = (x * cosYaw) - (z * sinYaw);
        var yawZ = (x * sinYaw) + (z * cosYaw);

        var cosPitch = Math.Cos(_pitch);
        var sinPitch = Math.Sin(_pitch);
        var pitchY = (y * cosPitch) - (yawZ * sinPitch);

        var scale = Math.Min(bounds.Width, bounds.Height) * 0.43 * _zoom;
        return new Point(
            (bounds.Width * 0.5) + (yawX * scale),
            (bounds.Height * 0.55) - (pitchY * scale));
    }

    private IBrush BrushFor(ColosseumCollisionTriangle triangle)
    {
        if (HighlightInteraction >= 0 && triangle.IsInteractable && triangle.InteractionIndex == HighlightInteraction)
        {
            return InteractionHighlightBrush;
        }

        if (HighlightSection >= 0 && triangle.SectionIndex == HighlightSection)
        {
            return SectionHighlightBrush;
        }

        return triangle.IsInteractable ? InteractableBrush : NormalFaceBrush;
    }

    private bool IsHighlighted(ColosseumCollisionTriangle triangle)
        => (HighlightInteraction >= 0 && triangle.IsInteractable && triangle.InteractionIndex == HighlightInteraction)
           || (HighlightSection >= 0 && triangle.SectionIndex == HighlightSection);

    private sealed record ProjectedTriangle(
        ColosseumCollisionTriangle Source,
        Point A,
        Point B,
        Point C)
    {
        public double Depth => A.Y + B.Y + C.Y;
    }
}
