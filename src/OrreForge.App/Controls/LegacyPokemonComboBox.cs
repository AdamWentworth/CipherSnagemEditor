using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace OrreForge.App.Controls;

public sealed class LegacyPokemonComboBox : ComboBox
{
    private static readonly IBrush IndicatorBrush = SolidColorBrush.Parse("#0878E7");
    private static readonly IBrush IndicatorBorderBrush = SolidColorBrush.Parse("#66B4FF");

    protected override Type StyleKeyOverride => typeof(ComboBox);

    public LegacyPokemonComboBox()
    {
        MinHeight = 0;
        Height = 20;
        Padding = new Thickness(3, 0, 2, 0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<Border>("DropDownOverlay") is { } overlay)
        {
            overlay.Width = 15;
            overlay.Height = 16;
            overlay.Margin = new Thickness(0, 0, 3, 0);
            overlay.HorizontalAlignment = HorizontalAlignment.Right;
            overlay.VerticalAlignment = VerticalAlignment.Center;
            overlay.Background = IndicatorBrush;
            overlay.BorderBrush = IndicatorBorderBrush;
            overlay.BorderThickness = new Thickness(1);
            overlay.CornerRadius = new CornerRadius(4);
            overlay.IsVisible = true;
            overlay.Child = new LegacyComboGlyph
            {
                Width = 8,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            if (overlay.GetVisualParent() is Grid templateGrid
                && templateGrid.ColumnDefinitions.Count > 1)
            {
                templateGrid.ColumnDefinitions[1].Width = new GridLength(18);
            }
        }

        if (e.NameScope.Find<Control>("DropDownGlyph") is { } stockGlyph)
        {
            stockGlyph.Opacity = 0;
            stockGlyph.IsHitTestVisible = false;
        }
    }
}

internal sealed class LegacyComboGlyph : Control
{
    private static readonly IBrush GlyphBrush = Brushes.White;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        DrawTriangle(context, new Point(Bounds.Width / 2, 2.2), isUp: true);
        DrawTriangle(context, new Point(Bounds.Width / 2, Bounds.Height - 2.2), isUp: false);
    }

    private static void DrawTriangle(DrawingContext context, Point center, bool isUp)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            if (isUp)
            {
                geometryContext.BeginFigure(new Point(center.X, center.Y - 1.7), isFilled: true);
                geometryContext.LineTo(new Point(center.X - 2.6, center.Y + 1.3));
                geometryContext.LineTo(new Point(center.X + 2.6, center.Y + 1.3));
            }
            else
            {
                geometryContext.BeginFigure(new Point(center.X - 2.6, center.Y - 1.3), isFilled: true);
                geometryContext.LineTo(new Point(center.X + 2.6, center.Y - 1.3));
                geometryContext.LineTo(new Point(center.X, center.Y + 1.7));
            }

            geometryContext.EndFigure(isClosed: true);
        }

        context.DrawGeometry(GlyphBrush, null, geometry);
    }
}
