using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace CipherSnagemEditor.App.Controls;

public sealed class LegacyPicker : Control
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LegacyPicker, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<LegacyPicker, object?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<LegacyPicker, IBrush?>(
            nameof(Background),
            SolidColorBrush.Parse("#666666"));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<LegacyPicker, IBrush?>(
            nameof(BorderBrush),
            SolidColorBrush.Parse("#747474"));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<LegacyPicker, IBrush?>(
            nameof(Foreground),
            Brushes.White);

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<LegacyPicker, double>(nameof(FontSize), 10);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<LegacyPicker, FontWeight>(nameof(FontWeight), FontWeight.Bold);

    public static readonly StyledProperty<string?> StringFormatProperty =
        AvaloniaProperty.Register<LegacyPicker, string?>(nameof(StringFormat));

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<LegacyPicker, double>(nameof(MaxDropDownHeight), 260);

    private static readonly IBrush IndicatorBrush = SolidColorBrush.Parse("#0878E7");
    private static readonly IBrush IndicatorBorderBrush = SolidColorBrush.Parse("#66B4FF");
    private static readonly Pen DefaultBorderPen = new(SolidColorBrush.Parse("#747474"), 1);
    private Flyout? _flyout;

    static LegacyPicker()
    {
        AffectsRender<LegacyPicker>(
            BackgroundProperty,
            BorderBrushProperty,
            ForegroundProperty,
            FontSizeProperty,
            FontWeightProperty,
            SelectedItemProperty,
            StringFormatProperty);
    }

    public LegacyPicker()
    {
        MinHeight = 0;
        Height = 20;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public string? StringFormat
    {
        get => GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var borderPen = BorderBrush is null ? DefaultBorderPen : new Pen(BorderBrush, 1);
        context.DrawRectangle(Background, borderPen, rect, 2, 2);

        DrawText(context, rect);
        DrawIndicator(context, rect);
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(0, Height);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            Focus();
            ShowPicker();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Enter or Key.Space or Key.Down)
        {
            e.Handled = true;
            ShowPicker();
        }
    }

    private void DrawText(DrawingContext context, Rect rect)
    {
        var textAreaWidth = Math.Max(0, rect.Width - 22);
        if (textAreaWidth <= 0)
        {
            return;
        }

        var text = FormatValue(SelectedItem);
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight, FontStretch.Normal),
            FontSize,
            Foreground ?? Brushes.White)
        {
            MaxTextWidth = textAreaWidth,
            MaxTextHeight = rect.Height,
            MaxLineCount = 1,
            TextAlignment = TextAlignment.Center,
            Trimming = TextTrimming.CharacterEllipsis
        };

        var y = Math.Max(0, (rect.Height - formattedText.Height) / 2);
        context.DrawText(formattedText, new Point(3, y));
    }

    private static void DrawIndicator(DrawingContext context, Rect rect)
    {
        var indicatorRect = new Rect(rect.Right - 18, rect.Center.Y - 8, 15, 16);
        context.DrawRectangle(IndicatorBrush, new Pen(IndicatorBorderBrush, 1), indicatorRect, 4, 4);
        DrawTriangle(context, new Point(indicatorRect.Center.X, indicatorRect.Y + 4.2), isUp: true);
        DrawTriangle(context, new Point(indicatorRect.Center.X, indicatorRect.Bottom - 4.2), isUp: false);
    }

    private static void DrawTriangle(DrawingContext context, Point center, bool isUp)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            if (isUp)
            {
                geometryContext.BeginFigure(new Point(center.X, center.Y - 1.6), isFilled: true);
                geometryContext.LineTo(new Point(center.X - 2.5, center.Y + 1.2));
                geometryContext.LineTo(new Point(center.X + 2.5, center.Y + 1.2));
            }
            else
            {
                geometryContext.BeginFigure(new Point(center.X - 2.5, center.Y - 1.2), isFilled: true);
                geometryContext.LineTo(new Point(center.X + 2.5, center.Y - 1.2));
                geometryContext.LineTo(new Point(center.X, center.Y + 1.6));
            }

            geometryContext.EndFigure(isClosed: true);
        }

        context.DrawGeometry(Brushes.White, null, geometry);
    }

    private void ShowPicker()
    {
        var items = ItemsSource?.Cast<object>().ToArray() ?? [];
        if (items.Length == 0)
        {
            return;
        }

        var listBox = new ListBox
        {
            ItemsSource = items,
            SelectedItem = SelectedItem,
            Width = Math.Max(90, Bounds.Width),
            MaxHeight = MaxDropDownHeight,
            Background = SolidColorBrush.Parse("#444444"),
            Foreground = Brushes.White
        };
        listBox.SelectionChanged += (_, _) =>
        {
            if (listBox.SelectedItem is not null)
            {
                SelectedItem = listBox.SelectedItem;
                _flyout?.Hide();
            }
        };

        _flyout = new Flyout
        {
            Content = listBox,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Transient
        };
        _flyout.ShowAt(this);
    }

    private string FormatValue(object? value)
    {
        if (value is null)
        {
            return "-";
        }

        var format = StringFormat;
        if (!string.IsNullOrWhiteSpace(format))
        {
            return string.Format(CultureInfo.CurrentCulture, format, value);
        }

        return value.ToString() ?? "-";
    }
}
