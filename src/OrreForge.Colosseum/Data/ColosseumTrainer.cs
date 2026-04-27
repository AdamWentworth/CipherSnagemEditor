namespace OrreForge.Colosseum.Data;

public sealed record ColosseumTrainer(
    int Index,
    int TrainerClassId,
    string TrainerClassName,
    int TrainerModelId,
    int Ai,
    int NameId,
    string Name,
    int FirstPokemonIndex,
    IReadOnlyList<ColosseumTrainerPokemon> Pokemon,
    IReadOnlyList<int> ItemIds,
    int PreBattleTextId,
    int VictoryTextId,
    int DefeatTextId)
{
    public bool HasShadow => Pokemon.Any(pokemon => pokemon.IsShadow);

    public string FullName => $"{TrainerClassName} {Name}".Trim();
}
