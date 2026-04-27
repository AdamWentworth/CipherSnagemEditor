using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TrainerPokemonSlotViewModel : ObservableObject
{
    private static readonly IBrush EmptyBrush = SolidColorBrush.Parse("#C0C0C8");
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A070FF");
    private static readonly IBrush NormalBrush = SolidColorBrush.Parse("#FFFFFF");

    public TrainerPokemonSlotViewModel(ColosseumTrainerPokemon pokemon)
    {
        Pokemon = pokemon;
    }

    public ColosseumTrainerPokemon Pokemon { get; }

    public string DeckIndexText => $"DPKM {Pokemon.Index}";

    public string ShadowIndexText => Pokemon.IsShadow ? $"DDPK {Pokemon.ShadowId}" : "DDPK 0";

    public string SpeciesText => Pokemon.IsSet ? $"Species {Pokemon.SpeciesId}" : "-";

    public string LevelText => Pokemon.IsSet ? $"Level {Pokemon.Level}" : "-";

    public string ItemText => Pokemon.IsSet ? $"Item {Pokemon.ItemId}" : "-";

    public string AbilityText => Pokemon.IsSet ? $"Ability {Pokemon.Ability}" : "-";

    public string NatureText => Pokemon.IsSet ? $"Nature {Pokemon.Nature}" : "-";

    public string GenderText => Pokemon.IsSet ? $"Gender {Pokemon.Gender}" : "-";

    public string MovesText => Pokemon.IsSet ? string.Join("  ", Pokemon.MoveIds.Select(id => id == 0 ? "-" : id.ToString())) : "-";

    public double Opacity => Pokemon.IsSet ? 1 : 0.5;

    public IBrush BackgroundBrush => Pokemon.IsShadow ? ShadowBrush : Pokemon.IsSet ? NormalBrush : EmptyBrush;
}
