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
    private readonly string _imageFolder;
    private readonly string _imageFileName;
    private readonly IBrush _normalBrush;
    private Bitmap? _trainerImage;
    private bool _trainerImageLoaded;

    public TrainerEntryViewModel(ColosseumTrainer trainer)
        : this(
            trainer,
            "ColoTrainers",
            $"colo_trainer_{trainer.TrainerModelId}.png",
            $"{trainer.Name}{Environment.NewLine}{trainer.Index}: {trainer.FullName}",
            StoryBrush)
    {
    }

    public TrainerEntryViewModel(
        ColosseumTrainer trainer,
        string imageFolder,
        string imageFileName,
        string rowText,
        IBrush? normalBrush = null)
    {
        Trainer = trainer;
        _imageFolder = imageFolder;
        _imageFileName = imageFileName;
        RowText = rowText;
        _normalBrush = normalBrush ?? StoryBrush;
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
                _trainerImage = RuntimeImageAssets.LoadImage(_imageFolder, _imageFileName);
                _trainerImageLoaded = true;
            }

            return _trainerImage;
        }
    }

    public string RowText { get; }

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBrush
        : Trainer.HasShadow ? ShadowBrush : _normalBrush;

}
