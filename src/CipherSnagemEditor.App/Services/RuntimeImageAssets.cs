using System.Collections.Concurrent;
using Avalonia.Media.Imaging;

namespace CipherSnagemEditor.App.Services;

public static class RuntimeImageAssets
{
    private static readonly ConcurrentDictionary<string, Lazy<Bitmap?>> ImageCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<int, Lazy<IReadOnlyList<PokemonBodyFrame>>> BodyFrameCache = [];

    public static Bitmap? LoadImage(string folder, string fileName)
    {
        var key = $"{folder}/{fileName}";
        return ImageCache.GetOrAdd(
            key,
            _ => new Lazy<Bitmap?>(
                () => LoadImageCore(folder, fileName),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public static IReadOnlyList<PokemonBodyFrame> LoadBodyFrames(int speciesId)
        => BodyFrameCache.GetOrAdd(
            speciesId,
            _ => new Lazy<IReadOnlyList<PokemonBodyFrame>>(
                () => LoadBodyFramesCore(speciesId),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private static Bitmap? LoadImageCore(string folder, string fileName)
    {
        var path = ResolveImagePath(folder, fileName);
        if (path is null)
        {
            return null;
        }

        try
        {
            return ApngImageLoader.Load(path).FirstOrDefault()?.Image;
        }
        catch
        {
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return null;
            }
        }
    }

    private static IReadOnlyList<PokemonBodyFrame> LoadBodyFramesCore(int speciesId)
    {
        if (speciesId <= 0)
        {
            return [];
        }

        var path = ResolveImagePath("PokeBody", $"body_{speciesId:000}.png");
        if (path is null)
        {
            return [];
        }

        try
        {
            return ApngImageLoader.Load(path);
        }
        catch
        {
            try
            {
                return [new PokemonBodyFrame(new Bitmap(path), TimeSpan.FromMilliseconds(100))];
            }
            catch
            {
                return [];
            }
        }
    }

    private static string? ResolveImagePath(string folder, string fileName)
    {
        foreach (var root in CandidateAssetRoots())
        {
            foreach (var assetRoot in new[] { "assets", "Assets" })
            {
                var path = Path.Combine(root, assetRoot, "images", folder, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
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
