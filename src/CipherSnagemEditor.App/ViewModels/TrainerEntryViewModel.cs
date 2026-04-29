using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TrainerEntryViewModel : ObservableObject
{
    private static readonly IBrush StoryBrush = SolidColorBrush.Parse("#8BB9FF");
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#F6BC00");
    private static readonly Dictionary<int, Bitmap?> ImageCache = [];

    public TrainerEntryViewModel(ColosseumTrainer trainer)
    {
        Trainer = trainer;
        TrainerImage = LoadTrainerImage(trainer.TrainerModelId);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumTrainer Trainer { get; }

    public Bitmap? TrainerImage { get; }

    public string RowText => $"{Trainer.Name}{Environment.NewLine}{Trainer.Index}: {Trainer.FullName}";

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBrush
        : Trainer.HasShadow ? ShadowBrush : StoryBrush;

    private static Bitmap? LoadTrainerImage(int modelId)
    {
        if (ImageCache.TryGetValue(modelId, out var cached))
        {
            return cached;
        }

        var path = ResolveTrainerImagePath(modelId);
        if (path is null)
        {
            ImageCache[modelId] = null;
            return null;
        }

        try
        {
            var image = new Bitmap(path);
            ImageCache[modelId] = image;
            return image;
        }
        catch (IOException)
        {
            ImageCache[modelId] = null;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            ImageCache[modelId] = null;
            return null;
        }
    }

    private static string? ResolveTrainerImagePath(int modelId)
    {
        var fileName = $"colo_trainer_{modelId}.png";
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "assets", "images", "ColoTrainers", fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateAssetRoots()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        foreach (var root in roots)
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                yield return current.FullName;
                current = current.Parent;
            }
        }
    }
}
