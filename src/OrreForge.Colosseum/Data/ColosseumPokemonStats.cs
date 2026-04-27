namespace OrreForge.Colosseum.Data;

public sealed record ColosseumPokemonStats(
    int Index,
    string Name,
    int NameId,
    int Type1,
    string Type1Name,
    int Type2,
    string Type2Name,
    int Ability1,
    string Ability1Name,
    int Ability2,
    string Ability2Name,
    int CatchRate);
