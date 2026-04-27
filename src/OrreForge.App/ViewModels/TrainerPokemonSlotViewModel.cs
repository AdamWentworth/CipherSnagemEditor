using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TrainerPokemonSlotViewModel : ObservableObject
{
    private static readonly IBrush EmptyBrush = SolidColorBrush.Parse("#C0C0C8");
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A070FF");
    private static readonly IBrush NormalBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly Dictionary<int, Bitmap?> BodyImageCache = [];

    public TrainerPokemonSlotViewModel(ColosseumTrainerPokemon pokemon)
    {
        Pokemon = pokemon;
        BodyImage = LoadBodyImage(pokemon.SpeciesId);
    }

    public ColosseumTrainerPokemon Pokemon { get; }

    public Bitmap? BodyImage { get; }

    public bool IsShadow => Pokemon.IsShadow;

    public string DeckIndexText => $"PKM {Pokemon.Index}";

    public string ShadowIndexText => Pokemon.IsShadow ? $"Shadow ID {Pokemon.ShadowId}" : "Shadow ID 0";

    public string SpeciesText => Pokemon.IsSet ? $"{Pokemon.SpeciesName} ({Pokemon.SpeciesId})" : "-";

    public string LevelText => Pokemon.IsSet ? $"Lv. {Pokemon.Level}" : "-";

    public string ItemText => Pokemon.IsSet ? $"{Pokemon.ItemName} ({Pokemon.ItemId})" : "-";

    public string PokeballText => Pokemon.IsSet ? $"{Pokemon.PokeballName} ({Pokemon.PokeballId})" : "-";

    public string AbilityText => Pokemon.IsSet ? $"{Pokemon.AbilityName} ({AbilitySlotText})" : "-";

    public string AbilitySlotText => Pokemon.Ability == 0xff ? "Random" : $"Slot {Pokemon.Ability + 1}";

    public string NatureText => Pokemon.IsSet ? $"{Pokemon.NatureName} ({FormatByte(Pokemon.Nature)})" : "-";

    public string GenderText => Pokemon.IsSet ? $"{Pokemon.GenderName} ({FormatByte(Pokemon.Gender)})" : "-";

    public string HappinessText => Pokemon.IsSet ? Pokemon.Happiness.ToString() : "-";

    public string IvText => Pokemon.IsSet ? Pokemon.Iv.ToString() : "-";

    public string HpEvText => EvText(0);

    public string AttackEvText => EvText(1);

    public string DefenseEvText => EvText(2);

    public string SpecialAttackEvText => EvText(3);

    public string SpecialDefenseEvText => EvText(4);

    public string SpeedEvText => EvText(5);

    public string Move1Text => MoveText(0);

    public string Move2Text => MoveText(1);

    public string Move3Text => MoveText(2);

    public string Move4Text => MoveText(3);

    public string ShadowHeartGaugeText => Pokemon.ShadowData?.HeartGauge.ToString() ?? "-";

    public string ShadowFirstTrainerText => Pokemon.ShadowData?.FirstTrainerId.ToString() ?? "-";

    public string ShadowAlternateTrainerText => Pokemon.ShadowData?.AlternateFirstTrainerId.ToString() ?? "-";

    public string ShadowCatchRateText => Pokemon.ShadowData?.CatchRate.ToString() ?? "-";

    public double Opacity => Pokemon.IsSet ? 1 : 0.5;

    public IBrush BackgroundBrush => Pokemon.IsShadow ? ShadowBrush : Pokemon.IsSet ? NormalBrush : EmptyBrush;

    private string EvText(int index)
        => Pokemon.IsSet && index < Pokemon.Evs.Count ? Pokemon.Evs[index].ToString() : "-";

    private string MoveText(int index)
    {
        if (!Pokemon.IsSet || index >= Pokemon.Moves.Count)
        {
            return "-";
        }

        var move = Pokemon.Moves[index];
        if (move.Index == 0)
        {
            return "-";
        }

        return $"{move.Name} ({move.Index})  {move.TypeName}  Pow {move.Power}  Acc {move.Accuracy}  PP {move.Pp}";
    }

    private static string FormatByte(int value)
        => value == 0xff ? "0xFF" : value.ToString();

    private static Bitmap? LoadBodyImage(int speciesId)
    {
        if (BodyImageCache.TryGetValue(speciesId, out var cached))
        {
            return cached;
        }

        var path = ResolveBodyImagePath(speciesId);
        if (path is null)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }

        try
        {
            var image = new Bitmap(path);
            BodyImageCache[speciesId] = image;
            return image;
        }
        catch (IOException)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            BodyImageCache[speciesId] = null;
            return null;
        }
    }

    private static string? ResolveBodyImagePath(int speciesId)
    {
        var fileName = $"body_{speciesId:000}.png";
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "images", "PokeBody", fileName);
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
