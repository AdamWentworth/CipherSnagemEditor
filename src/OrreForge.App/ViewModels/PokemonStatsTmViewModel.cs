using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class PokemonStatsTmViewModel : ObservableObject
{
    private static readonly IBrush BorderBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly IBrush TransparentBorderBrush = SolidColorBrush.Parse("#00FFFFFF");
    private static readonly Dictionary<int, Bitmap?> TypeImageCache = [];
    private readonly Action? _changed;

    public PokemonStatsTmViewModel(ColosseumTmMove tm, bool isLearnable, Action? changed)
    {
        Tm = tm;
        _isLearnable = isLearnable;
        _changed = changed;
        TypeImage = LoadTypeImage(tm.TypeId);
    }

    public ColosseumTmMove Tm { get; }

    public string MoveName => Tm.MoveName;

    public Bitmap? TypeImage { get; }

    public Thickness LearnableBorderThickness => IsLearnable ? new Thickness(1) : new Thickness(0);

    public IBrush LearnableBorderBrush => IsLearnable ? BorderBrush : TransparentBorderBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LearnableBorderThickness))]
    [NotifyPropertyChangedFor(nameof(LearnableBorderBrush))]
    private bool _isLearnable;

    partial void OnIsLearnableChanged(bool value)
    {
        _changed?.Invoke();
    }

    private static Bitmap? LoadTypeImage(int typeId)
    {
        if (TypeImageCache.TryGetValue(typeId, out var cached))
        {
            return cached;
        }

        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "images", "Types", $"type_{typeId}.png");
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var image = new Bitmap(path);
                TypeImageCache[typeId] = image;
                return image;
            }
            catch (IOException)
            {
                break;
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
        }

        TypeImageCache[typeId] = null;
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
