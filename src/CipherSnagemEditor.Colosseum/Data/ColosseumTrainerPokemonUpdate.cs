namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumTrainerPokemonUpdate(
    int Index,
    int SpeciesId,
    int Level,
    int ShadowId,
    int ItemId,
    int PokeballId,
    int Ability,
    int Nature,
    int Gender,
    int Happiness,
    int Iv,
    IReadOnlyList<int> Evs,
    IReadOnlyList<int> MoveIds,
    int ShadowHeartGauge,
    int ShadowFirstTrainerId,
    int ShadowAlternateFirstTrainerId,
    int ShadowCatchRate);
