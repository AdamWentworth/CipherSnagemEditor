namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumTrainer(
    int Index,
    int TrainerClassId,
    string TrainerClassName,
    int TrainerModelId,
    string TrainerModelName,
    int Ai,
    int NameId,
    string Name,
    int FirstPokemonIndex,
    IReadOnlyList<ColosseumTrainerPokemon> Pokemon,
    IReadOnlyList<int> ItemIds,
    int PreBattleTextId,
    int VictoryTextId,
    int DefeatTextId,
    ColosseumBattle? Battle)
{
    public bool HasShadow => Pokemon.Any(pokemon => pokemon.IsShadow);

    public string FullName => $"{TrainerClassName} {Name}".Trim();
}
