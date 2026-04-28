using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class PokemonStatsEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = SolidColorBrush.Parse("#000000");
    private static readonly IBrush TransparentSelectionBrush = SolidColorBrush.Parse("#00000000");
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
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumPokemonStats Stats { get; }

    public Bitmap? FaceImage { get; }

    public Bitmap? TypeImage { get; }

    public string RowText => Stats.Name;

    public string SearchText => $"{Stats.Index} {Stats.NationalIndex} {Stats.Name} {Stats.Type1Name} {Stats.Type2Name}";

    public IBrush BackgroundBrush => TypeFallbackBrush(Stats.Type1);

    public double TypeImageOpacity => 1;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentSelectionBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

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
            var image = ApngImageLoader.Load(path).FirstOrDefault()?.Image;
            cache[id] = image;
            return image;
        }
        catch
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

    private static IBrush TypeFallbackBrush(int typeId)
        => SolidColorBrush.Parse(typeId switch
        {
            0 => "#D0D0D0",
            1 => "#E0A060",
            2 => "#B8D8FF",
            3 => "#E080F8",
            4 => "#E8C878",
            5 => "#C8B070",
            6 => "#A8E868",
            7 => "#A070FF",
            8 => "#B0B0C0",
            9 => "#C0C0C8",
            10 => "#FC6848",
            11 => "#80ACFF",
            12 => "#20F020",
            13 => "#F8F888",
            14 => "#FF90C8",
            15 => "#B8F0FF",
            16 => "#B090F8",
            17 => "#707080",
            _ => "#80ACFF"
        });
}
