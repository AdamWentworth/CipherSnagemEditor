using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class PokemonStatsEntryViewModel : ObservableObject
{
    private static readonly IBrush NormalBrush = SolidColorBrush.Parse("#80ACFF");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#F7B409");
    private static readonly Dictionary<int, Bitmap?> FaceImageCache = [];
    private static readonly Dictionary<int, Bitmap?> TypeImageCache = [];

    public PokemonStatsEntryViewModel(ColosseumPokemonStats stats)
    {
        Stats = stats;
        FaceImage = LoadFaceImage(stats.Index);
        TypeImage = LoadTypeImage(stats.Type1);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(TypeImageOpacity))]
    private bool _isSelected;

    public ColosseumPokemonStats Stats { get; }

    public Bitmap? FaceImage { get; }

    public Bitmap? TypeImage { get; }

    public string RowText => Stats.Name;

    public string SearchText => $"{Stats.Index} {Stats.NationalIndex} {Stats.Name} {Stats.Type1Name} {Stats.Type2Name}";

    public IBrush BackgroundBrush => IsSelected ? SelectedBrush : NormalBrush;

    public double TypeImageOpacity => IsSelected ? 0.35 : 1;

    private static Bitmap? LoadFaceImage(int speciesId)
        => LoadCachedImage(FaceImageCache, speciesId, "PokeFace", $"face_{speciesId:000}.png");

    private static Bitmap? LoadTypeImage(int typeId)
        => LoadCachedImage(TypeImageCache, typeId, "Types", $"type_{typeId}.png");

    private static Bitmap? LoadCachedImage(
        IDictionary<int, Bitmap?> cache,
        int id,
        string folder,
        string fileName)
    {
        if (cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var path = ResolveImagePath(folder, fileName);
        if (path is null)
        {
            cache[id] = null;
            return null;
        }

        try
        {
            var image = new Bitmap(path);
            cache[id] = image;
            return image;
        }
        catch (IOException)
        {
            cache[id] = null;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            cache[id] = null;
            return null;
        }
    }

    private static string? ResolveImagePath(string folder, string fileName)
    {
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "images", folder, fileName);
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
