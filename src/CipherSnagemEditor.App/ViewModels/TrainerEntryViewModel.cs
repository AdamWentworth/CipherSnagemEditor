using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TrainerEntryViewModel : ObservableObject
{
    private static readonly IBrush StoryBrush = SolidColorBrush.Parse("#8BB9FF");
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#F6BC00");
    private Bitmap? _trainerImage;
    private bool _trainerImageLoaded;

    public TrainerEntryViewModel(ColosseumTrainer trainer)
    {
        Trainer = trainer;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumTrainer Trainer { get; }

    public Bitmap? TrainerImage
    {
        get
        {
            if (!_trainerImageLoaded)
            {
                _trainerImage = RuntimeImageAssets.LoadImage("ColoTrainers", $"colo_trainer_{Trainer.TrainerModelId}.png");
                _trainerImageLoaded = true;
            }

            return _trainerImage;
        }
    }

    public string RowText => $"{Trainer.Name}{Environment.NewLine}{Trainer.Index}: {Trainer.FullName}";

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBrush
        : Trainer.HasShadow ? ShadowBrush : StoryBrush;

}
