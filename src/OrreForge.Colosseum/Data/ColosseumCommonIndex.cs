using OrreForge.Core.GameCube;

namespace OrreForge.Colosseum.Data;

public enum ColosseumCommonIndex
{
    TrainerClasses = 24,
    NumberOfTrainerClasses = 25,
    Doors = 30,
    NumberOfDoors = 31,
    Trainers = 44,
    NumberOfTrainers = 45,
    TrainerPokemonData = 48,
    NumberOfTrainerPokemonData = 49,
    Battles = 50,
    NumberOfBattles = 51,
    TreasureBoxData = 60,
    NumberTreasureBoxes = 61,
    Moves = 62,
    NumberOfMoves = 63,
    PokemonStats = 68,
    NumberOfPokemon = 69,
    ShadowData = 80,
    NumberOfShadowPokemon = 81,
    InteractionPoints = 86,
    NumberOfInteractionPoints = 87,
    StringTable1 = 98
}

public static class ColosseumCommonIndexes
{
    public static int IndexFor(ColosseumCommonIndex index, GameCubeRegion region)
    {
        if (index == ColosseumCommonIndex.StringTable1)
        {
            return region switch
            {
                GameCubeRegion.Europe => 99,
                GameCubeRegion.Japan => 95,
                _ => 98
            };
        }

        return (int)index;
    }
}
