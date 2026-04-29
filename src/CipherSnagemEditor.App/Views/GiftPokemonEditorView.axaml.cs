using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CipherSnagemEditor.App.Services;
using Avalonia.Controls;
using Avalonia.Input;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class GiftPokemonEditorView : UserControl
{
    private readonly DispatcherTimer _spriteTimer;
    private TimeSpan _spriteClock = TimeSpan.Zero;

    public GiftPokemonEditorView()
    {
        InitializeComponent();

        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _spriteTimer.Tick += AnimatePokemonBodies;
        AttachedToVisualTree += (_, _) => _spriteTimer.Start();
        DetachedFromVisualTree += (_, _) => _spriteTimer.Stop();
    }

    private void GiftPokemonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: GiftPokemonEntryViewModel gift }
            && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedGiftPokemon = gift;
            e.Handled = true;
        }
    }

    private void AnimatePokemonBodies(object? sender, EventArgs e)
    {
        _spriteClock += _spriteTimer.Interval;

        foreach (var image in this.GetVisualDescendants().OfType<Image>())
        {
            if (!image.Classes.Contains("GiftPokemonBodyImage") || !image.IsVisible)
            {
                continue;
            }

            if (image.DataContext is not GiftPokemonEditorViewModel gift || gift.BodyFrames.Count == 0)
            {
                continue;
            }

            image.Source = SelectFrame(gift.BodyFrames, _spriteClock);
            image.RenderTransform = null;
        }
    }

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
