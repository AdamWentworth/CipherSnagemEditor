using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OrreForge.App.ViewModels;

namespace OrreForge.App.Views;

public partial class TrainerEditorView : UserControl
{
    private readonly DispatcherTimer _spriteTimer;
    private double _spritePhase;

    public TrainerEditorView()
    {
        InitializeComponent();

        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
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
        _spritePhase = (_spritePhase + 0.18) % (Math.PI * 2);
        var offset = -1.5 + Math.Sin(_spritePhase) * 1.5;
        var scale = 1.0 + ((Math.Sin(_spritePhase + 0.9) + 1.0) * 0.007);

        foreach (var image in this.GetVisualDescendants().OfType<Image>())
        {
            if (!image.Classes.Contains("PokemonBodyImage") || !image.IsVisible || image.Source is null)
            {
                continue;
            }

            var transforms = new TransformGroup();
            transforms.Children.Add(new ScaleTransform(scale, scale));
            transforms.Children.Add(new TranslateTransform(0, offset));

            image.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            image.RenderTransform = transforms;
        }
    }
}
