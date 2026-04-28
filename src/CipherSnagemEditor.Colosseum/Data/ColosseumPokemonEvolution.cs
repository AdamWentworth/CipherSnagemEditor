namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumPokemonEvolution(
    int Index,
    int Method,
    string MethodName,
    int Condition,
    string ConditionName,
    int EvolvedSpeciesId,
    string EvolvedSpeciesName);
