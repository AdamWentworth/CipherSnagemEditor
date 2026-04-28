using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class GiftPokemonEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = SolidColorBrush.Parse("#FFB000");
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly Dictionary<int, Bitmap?> FaceImageCache = [];

    public GiftPokemonEntryViewModel(ColosseumGiftPokemon gift, int rowIndex)
    {
        Gift = gift;
        RowIndex = rowIndex;
        FaceImage = LoadFaceImage(gift.SpeciesId);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumGiftPokemon Gift { get; }

    public int RowIndex { get; }

    public Bitmap? FaceImage { get; }

    public string RowText => $"{Gift.SpeciesName} Lv. {Gift.Level}\n{Gift.GiftType}";

    public string SearchText => $"{Gift.RowId} {Gift.SpeciesName} {Gift.GiftType}";

    public IBrush BackgroundBrush => SolidColorBrush.Parse(RowIndex switch
    {
        0 or 1 => "#80ACFFFF",
        2 or 3 => "#FC6848FF",
        _ => "#A8E79CFF"
    });

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(2) : new Thickness(0);

    private static Bitmap? LoadFaceImage(int speciesId)
    {
        if (FaceImageCache.TryGetValue(speciesId, out var cached))
        {
            return cached;
        }

        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "images", "PokeFace", $"face_{speciesId:000}.png");
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var image = ApngImageLoader.Load(path).FirstOrDefault()?.Image;
                FaceImageCache[speciesId] = image;
                return image;
            }
            catch
            {
                break;
            }
        }

        FaceImageCache[speciesId] = null;
        return null;
    }

    private static IEnumerable<string> CandidateAssetRoots()
    {
        var roots = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
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
