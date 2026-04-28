namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumGiftPokemon(
    int RowId,
    int DataIndex,
    int StartOffset,
    string GiftType,
    int SpeciesId,
    string SpeciesName,
    int Level,
    IReadOnlyList<int> MoveIds,
    IReadOnlyList<string> MoveNames,
    int ShinyValue,
    string ShinyLabel,
    int Gender,
    string GenderLabel,
    int Nature,
    string NatureLabel,
    bool UsesLevelUpMoves,
    bool SupportsNatureGender);

public sealed record ColosseumGiftPokemonUpdate(
    int RowId,
    int SpeciesId,
    int Level,
    IReadOnlyList<int> MoveIds,
    int ShinyValue,
    int Gender,
    int Nature);
