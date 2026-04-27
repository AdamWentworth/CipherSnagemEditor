namespace OrreForge.Colosseum.Data;

public sealed record ColosseumTrainerPokemon(
    int Slot,
    int Index,
    int SpeciesId,
    int Level,
    int ShadowId,
    int ItemId,
    int Ability,
    int Nature,
    int Gender,
    IReadOnlyList<int> MoveIds)
{
    public bool IsSet => SpeciesId > 0;

    public bool IsShadow => ShadowId > 0;
}
