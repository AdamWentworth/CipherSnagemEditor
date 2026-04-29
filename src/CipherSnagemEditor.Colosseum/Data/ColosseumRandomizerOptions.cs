namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumRandomizerOptions(
    bool StarterPokemon,
    bool ShadowPokemon,
    bool NpcPokemon,
    bool PokemonMoves,
    bool PokemonTypes,
    bool PokemonAbilities,
    bool PokemonStats,
    bool PokemonEvolutions,
    bool MoveTypes,
    bool TypeMatchups,
    bool TmMoves,
    bool ItemBoxes,
    bool ShopItems,
    bool SimilarBaseStatTotal,
    bool RemoveItemOrTradeEvolutions);
