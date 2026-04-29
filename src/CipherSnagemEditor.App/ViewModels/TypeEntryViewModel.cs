using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TypeEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly Dictionary<int, Bitmap?> TypeImageCache = [];

    public TypeEntryViewModel(ColosseumTypeData type)
    {
        Type = type;
        TypeImage = LoadTypeImage(type.Index);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumTypeData Type { get; }

    public Bitmap? TypeImage { get; }

    public string RowText => Type.Name;

    public string SearchText => $"{Type.Index} {Type.Name} {Type.CategoryName}";

    public IBrush BackgroundBrush => SolidColorBrush.Parse("#303030");

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private static Bitmap? LoadTypeImage(int id)
    {
        if (TypeImageCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "assets", "images", "Types", $"type_{id}.png");
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var image = ApngImageLoader.Load(path).FirstOrDefault()?.Image;
                TypeImageCache[id] = image;
                return image;
            }
            catch
            {
                break;
            }
        }

        TypeImageCache[id] = null;
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
