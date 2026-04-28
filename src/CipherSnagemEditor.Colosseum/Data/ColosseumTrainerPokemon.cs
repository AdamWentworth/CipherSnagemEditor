namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumTrainerPokemon(
    int Slot,
    int Index,
    int SpeciesId,
    string SpeciesName,
    int Level,
    int ShadowId,
    int ItemId,
    string ItemName,
    int PokeballId,
    string PokeballName,
    int Ability,
    string AbilityName,
    int Nature,
    string NatureName,
    int Gender,
    string GenderName,
    int Happiness,
    int Iv,
    IReadOnlyList<int> Evs,
    IReadOnlyList<ColosseumMove> Moves,
    ColosseumShadowPokemonData? ShadowData)
{
    public bool IsSet => SpeciesId > 0;

    public bool IsShadow => ShadowId > 0;
}
