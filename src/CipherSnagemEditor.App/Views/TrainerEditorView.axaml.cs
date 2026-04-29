using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class TrainerEditorView : UserControl
{
    private readonly DispatcherTimer _spriteTimer;
    private TimeSpan _spriteClock = TimeSpan.Zero;
    private IReadOnlyList<Image> _bodyImages = [];
    private int _bodyImageRefreshTicks;

    public TrainerEditorView()
    {
        InitializeComponent();

        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _spriteTimer.Tick += AnimatePokemonBodies;
        AttachedToVisualTree += (_, _) => _spriteTimer.Start();
        DetachedFromVisualTree += (_, _) => _spriteTimer.Stop();
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

    private void AnimatePokemonBodies(object? sender, EventArgs e)
    {
        _spriteClock += _spriteTimer.Interval;
        if (_bodyImages.Count == 0 || _bodyImageRefreshTicks-- <= 0)
        {
            _bodyImages = this.GetVisualDescendants()
                .OfType<Image>()
                .Where(image => image.Classes.Contains("PokemonBodyImage"))
                .ToArray();
            _bodyImageRefreshTicks = 25;
        }

        foreach (var image in _bodyImages)
        {
            if (!image.IsVisible)
            {
                continue;
            }

            if (image.DataContext is not TrainerPokemonSlotViewModel slot || slot.BodyFrames.Count == 0)
            {
                continue;
            }

            image.Source = SelectFrame(slot.BodyFrames, _spriteClock);
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
