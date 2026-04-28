using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace OrreForge.App.Controls;

public sealed class LegacyPokemonComboBox : ComboBox
{
    private static readonly IBrush IndicatorBrush = SolidColorBrush.Parse("#0878E7");
    private static readonly IBrush DisabledIndicatorBrush = SolidColorBrush.Parse("#4D7AAE");
    private static readonly Pen IndicatorBorderPen = new(SolidColorBrush.Parse("#66B4FF"), 0.8);
    private static readonly IBrush GlyphBrush = Brushes.White;

    protected override Type StyleKeyOverride => typeof(ComboBox);

    public LegacyPokemonComboBox()
    {
        MinHeight = 0;
        Height = 20;
        Padding = new Thickness(3, 0, 20, 0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width < 22 || Bounds.Height < 14)
        {
            return;
        }

        var indicatorHeight = Math.Min(16, Bounds.Height - 4);
        var indicatorWidth = Math.Min(15, Bounds.Height - 5);
        var x = Bounds.Width - indicatorWidth - 3;
        var y = (Bounds.Height - indicatorHeight) / 2;
        var indicator = new Rect(x, y, indicatorWidth, indicatorHeight);

        context.DrawRectangle(
            IsEffectivelyEnabled ? IndicatorBrush : DisabledIndicatorBrush,
            IndicatorBorderPen,
            indicator,
            4,
            4);

        var centerX = x + (indicatorWidth / 2);
        DrawTriangle(context, new Point(centerX, y + 4.4), isUp: true);
        DrawTriangle(context, new Point(centerX, y + indicatorHeight - 4.4), isUp: false);
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
