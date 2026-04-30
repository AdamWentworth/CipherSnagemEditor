using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class XdShadowPokemonEditorView : UserControl
{
    private readonly DispatcherTimer _spriteTimer;
    private TimeSpan _spriteClock = TimeSpan.Zero;
    private IReadOnlyList<Image> _bodyImages = [];

    public XdShadowPokemonEditorView()
    {
        InitializeComponent();

        _spriteTimer = new DispatcherTimer { Interval = SpriteTimerInterval() };
        _spriteTimer.Tick += AnimatePokemonBodies;
        AttachedToVisualTree += (_, _) => _spriteTimer.Start();
        DetachedFromVisualTree += (_, _) => _spriteTimer.Stop();
    }

    private void AnimatePokemonBodies(object? sender, EventArgs e)
    {
        _spriteClock += _spriteTimer.Interval;
        if (_bodyImages.Count == 0)
        {
            _bodyImages = this.GetVisualDescendants()
                .OfType<Image>()
                .Where(image => image.Classes.Contains("PokemonBodyImage"))
                .ToArray();
        }

        foreach (var image in _bodyImages)
        {
            if (!image.IsVisible
                || image.DataContext is not XdShadowPokemonEntryViewModel shadow
                || shadow.BodyFrames.Count == 0)
            {
                continue;
            }

            var frame = SelectFrame(shadow.BodyFrames, _spriteClock);
            if (!ReferenceEquals(image.Source, frame))
            {
                image.Source = frame;
            }
        }
    }

    private static TimeSpan SpriteTimerInterval()
        => OperatingSystem.IsLinux()
            ? TimeSpan.FromMilliseconds(100)
            : TimeSpan.FromMilliseconds(40);

    private static IImage SelectFrame(IReadOnlyList<PokemonBodyFrame> frames, TimeSpan clock)
    {
        if (frames.Count == 1)
        {
            return frames[0].Image;
        }

        var totalMilliseconds = frames.Sum(frame => Math.Max(1, frame.Duration.TotalMilliseconds));
        var position = clock.TotalMilliseconds % totalMilliseconds;

        foreach (var frame in frames)
        {
            position -= Math.Max(1, frame.Duration.TotalMilliseconds);
            if (position <= 0)
            {
                return frame.Image;
            }
        }

        return frames[^1].Image;
    }
}
