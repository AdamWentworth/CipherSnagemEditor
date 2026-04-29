namespace CipherSnagemEditor.Colosseum.Data;

public sealed partial class ColosseumCommonRel
{
    private const int MinimumRandomStatSmallTotal = 10;
    private const int MinimumRandomStatLargeTotal = 40;

    public ColosseumPatchChangeSet Randomize(ColosseumRandomizerOptions options)
    {
        var changes = ColosseumPatchChangeSet.Empty;

        if (options.StarterPokemon || options.ShadowPokemon || options.NpcPokemon)
        {
            var counts = RandomizePokemonSpecies(
                options.StarterPokemon,
                options.ShadowPokemon,
                options.NpcPokemon,
                options.SimilarBaseStatTotal);

            if (counts.Gifts > 0)
            {
                changes = changes.WithStartDol();
            }

            if (counts.TrainerPokemon > 0)
            {
                changes = changes.WithCommonRel();
            }

            changes = changes.WithMessage(
                $"Randomized Pokemon species: {counts.Gifts} gifts, {counts.TrainerPokemon} trainer Pokemon.");
        }

        if (options.PokemonMoves)
        {
            var counts = RandomizePokemonMoves();
            changes = changes
                .WithCommonRel()
                .WithStartDol()
                .WithMessage($"Randomized Pokemon moves: {counts.TrainerPokemon} trainer Pokemon, {counts.Gifts} gifts, {counts.LevelUpTables} level-up tables.");
        }

        if (options.PokemonTypes)
        {
            var count = RandomizePokemonTypes();
            changes = changes.WithCommonRel().WithMessage($"Randomized Pokemon types for {count} Pokemon.");
        }

        if (options.PokemonAbilities)
        {
            var count = RandomizePokemonAbilities();
            changes = changes.WithCommonRel().WithMessage($"Randomized Pokemon abilities for {count} Pokemon.");
        }

        if (options.PokemonStats)
        {
            var count = RandomizePokemonBaseStats();
            changes = changes.WithCommonRel().WithMessage($"Randomized base stats for {count} Pokemon.");
        }

        if (options.PokemonEvolutions)
        {
            var count = RandomizePokemonEvolutions();
            changes = changes.WithCommonRel().WithMessage($"Randomized {count} evolution targets.");
        }

        if (options.MoveTypes)
        {
            var count = RandomizeMoveTypes();
            changes = changes.WithCommonRel().WithMessage($"Randomized move types for {count} moves.");
        }

        if (options.TypeMatchups)
        {
            var count = RandomizeTypeMatchups();
            changes = changes.WithStartDol().WithMessage($"Randomized type matchup rows for {count} types.");
        }

        if (options.TmMoves)
        {
            var count = RandomizeTmMoves();
            changes = changes.WithStartDol().WithMessage($"Randomized {count} TM/HM moves.");
        }

        if (options.ItemBoxes)
        {
            var count = RandomizeTreasureBoxes();
            changes = changes.WithCommonRel().WithMessage($"Randomized {count} item boxes.");
        }

        if (options.RemoveItemOrTradeEvolutions)
        {
            var tradeChanges = RemoveTradeEvolutions();
            var itemChanges = RemoveItemEvolutions();
            changes = changes
                .WithCommonRel()
                .WithMessage(tradeChanges.Messages.FirstOrDefault() ?? "Removed trade evolutions.")
                .WithMessage(itemChanges.Messages.FirstOrDefault() ?? "Removed item evolutions.");
        }

        return changes.Messages.Count == 0 && !options.ShopItems
            ? changes.WithMessage("No randomizer options were selected.")
            : changes;
    }

    private RandomizedSpeciesCounts RandomizePokemonSpecies(
        bool includeStarters,
        bool includeShadowPokemon,
        bool includeNpcPokemon,
        bool similarBaseStatTotal)
    {
        var eligibleSpecies = EligiblePokemonSpecies();
        var usedSpecies = new HashSet<int>();
        var shadowSpeciesById = new Dictionary<int, ColosseumPokemonStats>();
        var giftCount = 0;
        var trainerCount = 0;

        foreach (var gift in GiftPokemon)
        {
            if ((gift.RowId is 1 or 2 && !includeStarters) || (gift.RowId > 2 && !includeShadowPokemon))
            {
                continue;
            }

            var newSpecies = RandomSpeciesFor(gift.SpeciesId, eligibleSpecies, similarBaseStatTotal, usedSpecies);
            var moves = LevelUpMovesFor(newSpecies.Index, gift.Level);
            WriteGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                newSpecies.Index,
                gift.Level,
                moves,
                gift.ShinyValue,
                gift.Gender,
                gift.Nature));
            giftCount++;
        }

        var trainerCountLimit = GetCount(ColosseumCommonIndex.NumberOfTrainers);
        for (var trainerIndex = 1; trainerIndex < trainerCountLimit; trainerIndex++)
        {
            foreach (var pokemon in ReadTrainer(trainerIndex).Pokemon.Where(pokemon => pokemon.IsSet))
            {
                if ((pokemon.IsShadow && !includeShadowPokemon) || (!pokemon.IsShadow && !includeNpcPokemon))
                {
                    continue;
                }

                ColosseumPokemonStats newSpecies;
                if (pokemon.ShadowId > 0 && shadowSpeciesById.TryGetValue(pokemon.ShadowId, out var cachedSpecies))
                {
                    newSpecies = cachedSpecies;
                }
                else
                {
                    newSpecies = RandomSpeciesFor(
                        pokemon.SpeciesId,
                        eligibleSpecies,
                        similarBaseStatTotal,
                        pokemon.IsShadow ? usedSpecies : null);
                    if (pokemon.ShadowId > 0)
                    {
                        shadowSpeciesById[pokemon.ShadowId] = newSpecies;
                    }
                }

                WriteTrainerPokemon(TrainerPokemonUpdateFor(
                    pokemon,
                    newSpecies.Index,
                    LevelUpMovesFor(newSpecies.Index, pokemon.Level),
                    pokemon.IsShadow ? newSpecies.CatchRate : pokemon.ShadowData?.CatchRate ?? 0,
                    happiness: LegacyRandomizedTrainerHappiness));
                trainerCount++;
            }
        }

        return new RandomizedSpeciesCounts(giftCount, trainerCount);
    }

    private RandomizedMoveCounts RandomizePokemonMoves()
    {
        var trainerCount = 0;
        var giftCount = 0;
        var levelUpTableCount = 0;

        var trainerCountLimit = GetCount(ColosseumCommonIndex.NumberOfTrainers);
        for (var trainerIndex = 1; trainerIndex < trainerCountLimit; trainerIndex++)
        {
            foreach (var pokemon in ReadTrainer(trainerIndex).Pokemon.Where(pokemon => pokemon.IsSet))
            {
                WriteTrainerPokemon(TrainerPokemonUpdateFor(
                    pokemon,
                    pokemon.SpeciesId,
                    RandomMoveset(),
                    pokemon.ShadowData?.CatchRate ?? 0,
                    pokemon.Happiness));
                trainerCount++;
            }
        }

        foreach (var gift in GiftPokemon)
        {
            WriteGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                gift.SpeciesId,
                gift.Level,
                RandomMoveset(),
                gift.ShinyValue,
                gift.Gender,
                gift.Nature));
            giftCount++;
        }

        foreach (var pokemon in PokemonStats.Where(HasPokemonName))
        {
            var usedMoveIds = new HashSet<int>();
            var levelUpMoves = pokemon.LevelUpMoves
                .Select(move =>
                {
                    if (move.Level <= 0)
                    {
                        return move;
                    }

                    var moveId = RandomMoveId(usedMoveIds: usedMoveIds);
                    return move with { MoveId = moveId, MoveName = MoveFor(moveId).Name };
                })
                .ToArray();
            WritePokemonStats(PokemonStatsUpdateFor(pokemon, levelUpMoves: levelUpMoves));
            levelUpTableCount++;
        }

        return new RandomizedMoveCounts(trainerCount, giftCount, levelUpTableCount);
    }

    private int RandomizePokemonTypes()
    {
        var typeIds = RandomTypeIds();
        var count = 0;
        foreach (var pokemon in PokemonStats.Where(HasPokemonName))
        {
            WritePokemonStats(PokemonStatsUpdateFor(
                pokemon,
                type1: RandomElement(typeIds),
                type2: RandomElement(typeIds)));
            count++;
        }

        return count;
    }

    private int RandomizePokemonAbilities()
    {
        var abilityIds = Abilities
            .Where(ability => ability.Index > 0 && !IsWonderGuard(ability.Name))
            .Select(ability => ability.Index)
            .ToArray();
        if (abilityIds.Length == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var pokemon in PokemonStats.Where(HasPokemonName))
        {
            if (IsWonderGuard(pokemon.Ability1Name) || IsWonderGuard(pokemon.Ability2Name))
            {
                continue;
            }

            WritePokemonStats(PokemonStatsUpdateFor(
                pokemon,
                ability1: RandomElement(abilityIds),
                ability2: pokemon.Ability2 > 0 ? RandomElement(abilityIds) : pokemon.Ability2));
            count++;
        }

        return count;
    }

    private int RandomizePokemonBaseStats()
    {
        var count = 0;
        foreach (var pokemon in PokemonStats)
        {
            var randomized = RandomBaseStatsFor(pokemon);
            WritePokemonStats(PokemonStatsUpdateFor(
                pokemon,
                hp: randomized.Hp,
                attack: randomized.Attack,
                defense: randomized.Defense,
                specialAttack: randomized.SpecialAttack,
                specialDefense: randomized.SpecialDefense,
                speed: randomized.Speed));
            count++;
        }

        return count;
    }

    private int RandomizePokemonEvolutions()
    {
        var eligibleSpecies = EligiblePokemonSpecies();
        var count = 0;
        foreach (var pokemon in PokemonStats.Where(HasPokemonName))
        {
            var evolutions = pokemon.Evolutions
                .Select(evolution =>
                {
                    if (evolution.EvolvedSpeciesId <= 0)
                    {
                        return evolution;
                    }

                    var species = RandomSpeciesFor(
                        evolution.EvolvedSpeciesId,
                        eligibleSpecies,
                        similarBaseStatTotal: false,
                        usedSpecies: null,
                        preserveInvalidOldSpecies: false);
                    count++;
                    return evolution with { EvolvedSpeciesId = species.Index, EvolvedSpeciesName = species.Name };
                })
                .ToArray();

            WritePokemonStats(PokemonStatsUpdateFor(pokemon, evolutions: evolutions));
        }

        return count;
    }

    private int RandomizeMoveTypes()
    {
        var typeIds = RandomTypeIds();
        var count = 0;
        foreach (var move in Moves.Where(move => move.Index > 0))
        {
            WriteMove(MoveUpdateFor(move, typeId: RandomElement(typeIds)));
            count++;
        }

        return count;
    }

    private int RandomizeTypeMatchups()
    {
        var count = 0;
        foreach (var type in TypeData)
        {
            WriteType(new ColosseumTypeUpdate(
                type.Index,
                type.NameId,
                type.CategoryId,
                Shuffle(type.Effectiveness)));
            count++;
        }

        return count;
    }

    private int RandomizeTmMoves()
    {
        var usedMoveIds = new HashSet<int>();
        var count = 0;
        foreach (var tm in TmMoves.ToArray())
        {
            WriteTmMove(tm.Index, RandomMoveId(usedMoveIds: usedMoveIds));
            RefreshItemForTm(tm.Index);
            count++;
        }

        return count;
    }

    private int RandomizeTreasureBoxes()
    {
        var itemPool = EligibleRandomItems();
        if (itemPool.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var treasure in Treasures)
        {
            var currentItem = ItemById(treasure.ItemId);
            if (currentItem is null || currentItem.BagSlotId >= 5 || currentItem.Price <= 0)
            {
                continue;
            }

            WriteTreasure(new ColosseumTreasureUpdate(
                treasure.Index,
                treasure.ModelId,
                Random.Shared.Next(1, 4),
                treasure.Angle,
                treasure.RoomId,
                RandomElement(itemPool).Index,
                treasure.X,
                treasure.Y,
                treasure.Z));
            count++;
        }

        return count;
    }

    private IReadOnlyList<ColosseumPokemonStats> EligiblePokemonSpecies()
        => PokemonStats.Where(IsEligibleRandomSpecies).ToArray();

    private IReadOnlyList<ColosseumItem> EligibleRandomItems()
        => ItemData
            .Where(item => item.Index > 0)
            .Where(item => item.NameId > 0 && item.DescriptionId > 0)
            .Where(item => item.BagSlotId < 5 && item.Price > 0)
            .ToArray();

    private IReadOnlyList<ColosseumMove> EligibleRandomMoves()
        => Moves
            .Where(IsUsableMove)
            .Where(move => move.Index is not (355 or 357))
            .Where(move => !move.IsShadow)
            .ToArray();

    private ColosseumPokemonStats RandomSpeciesFor(
        int oldSpeciesId,
        IReadOnlyList<ColosseumPokemonStats> eligibleSpecies,
        bool similarBaseStatTotal,
        HashSet<int>? usedSpecies,
        bool preserveInvalidOldSpecies = true)
    {
        var options = eligibleSpecies;
        var oldSpecies = PokemonStatsFor(oldSpeciesId);
        if (preserveInvalidOldSpecies && (oldSpecies is null || oldSpecies.Index <= 0 || oldSpecies.CatchRate <= 0))
        {
            return oldSpecies ?? UnknownPokemonStats(oldSpeciesId);
        }

        if (usedSpecies is not null && usedSpecies.Count >= eligibleSpecies.Count)
        {
            usedSpecies.Clear();
        }

        if (usedSpecies is not null && options.Any(species => !usedSpecies.Contains(species.Index)))
        {
            options = options.Where(species => !usedSpecies.Contains(species.Index)).ToArray();
        }

        if (similarBaseStatTotal && oldSpecies is not null)
        {
            var radius = 50;
            while (radius <= 600)
            {
                var filtered = options
                    .Where(species => species.BaseStatTotal >= oldSpecies.BaseStatTotal - radius)
                    .Where(species => species.BaseStatTotal <= oldSpecies.BaseStatTotal + radius)
                    .ToArray();
                if (filtered.Length > 1)
                {
                    options = filtered;
                    break;
                }

                radius += 20;
            }
        }

        if (options.Count == 0)
        {
            var fallback = oldSpecies ?? UnknownPokemonStats(oldSpeciesId);
            if (usedSpecies is not null)
            {
                StrikeEvolutionLineForSpecies(fallback.Index, eligibleSpecies, usedSpecies);
            }

            return fallback;
        }

        var selected = RandomElement(options);
        if (usedSpecies is not null)
        {
            StrikeEvolutionLineForSpecies(selected.Index, eligibleSpecies, usedSpecies);
        }

        return selected;
    }

    private IReadOnlyList<int> RandomMoveset(int count = 4)
    {
        var movePool = EligibleRandomMoves();
        var damagingPool = movePool.Where(move => move.Power > 0).ToArray();
        var moves = new List<int>(count);
        var usedMoveIds = new HashSet<int>();
        if (count > 0)
        {
            moves.Add(RandomElement(damagingPool.Length > 0 ? damagingPool : movePool).Index);
            usedMoveIds.Add(moves[0]);
        }

        while (moves.Count < count)
        {
            moves.Add(RandomMoveId(movePool, usedMoveIds));
        }

        while (moves.Count < 4)
        {
            moves.Add(0);
        }

        return moves;
    }

    private int RandomMoveId(IReadOnlyList<ColosseumMove>? movePool = null, HashSet<int>? usedMoveIds = null)
    {
        movePool ??= EligibleRandomMoves();
        if (movePool.Count == 0)
        {
            return 0;
        }

        var options = usedMoveIds is null || usedMoveIds.Count >= movePool.Count
            ? movePool
            : movePool.Where(move => !usedMoveIds.Contains(move.Index)).ToArray();
        var selected = RandomElement(options.Count > 0 ? options : movePool);
        usedMoveIds?.Add(selected.Index);
        return selected.Index;
    }

    private IReadOnlyList<int> RandomTypeIds()
        => RandomTypeIdsFor(TypeData);

    internal static IReadOnlyList<int> RandomTypeIdsFor(IReadOnlyList<ColosseumTypeData> typeData)
    {
        if (typeData.Count == 0)
        {
            return Enumerable.Range(0, TypeCount).Where(id => id != 9).ToArray();
        }

        return typeData
            .Where(type => type.Index != 9 || !type.Name.Contains('?', StringComparison.Ordinal))
            .Select(type => type.Index)
            .ToArray();
    }

    private static RandomizedStats RandomBaseStatsFor(ColosseumPokemonStats pokemon)
    {
        var remaining = pokemon.BaseStatTotal;
        var maxStat = Math.Min(Math.Max(1, remaining / 4), byte.MaxValue);
        var minimum = remaining <= 240 ? MinimumRandomStatSmallTotal : MinimumRandomStatLargeTotal;
        var stats = new[] { minimum, minimum, minimum, minimum, minimum, minimum };
        remaining -= minimum * stats.Length;

        void AddToRandomStat(int value)
        {
            if (remaining <= 0)
            {
                return;
            }

            value = Math.Min(value, remaining);
            var attempts = 0;
            while (attempts < 100)
            {
                var index = Random.Shared.Next(0, stats.Length);
                if (stats[index] + value <= maxStat)
                {
                    stats[index] += value;
                    remaining -= value;
                    return;
                }

                attempts++;
            }

            var fallback = Array.FindIndex(stats, stat => stat < byte.MaxValue);
            if (fallback < 0)
            {
                remaining = 0;
                return;
            }

            var added = Math.Min(value, byte.MaxValue - stats[fallback]);
            stats[fallback] += added;
            remaining -= added;
        }

        while (remaining > 150)
        {
            AddToRandomStat(Math.Max(1, remaining / 6));
        }

        while (remaining > 25)
        {
            AddToRandomStat(10);
        }

        while (remaining > 0)
        {
            AddToRandomStat(1);
        }

        return new RandomizedStats(
            Hp: stats[5],
            Attack: stats[0],
            Defense: stats[1],
            SpecialAttack: stats[2],
            SpecialDefense: stats[3],
            Speed: stats[4]);
    }

    private static IReadOnlyList<int> Shuffle(IReadOnlyList<int> values)
    {
        var shuffled = values.ToArray();
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled;
    }

    internal static void StrikeEvolutionLineForSpecies(
        int speciesId,
        IReadOnlyList<ColosseumPokemonStats> eligibleSpecies,
        ISet<int> usedSpecies)
    {
        usedSpecies.Add(speciesId);

        var firstPreEvolution = eligibleSpecies.FirstOrDefault(pokemon =>
            pokemon.Evolutions.Any(evolution => evolution.EvolvedSpeciesId == speciesId));
        if (firstPreEvolution is not null)
        {
            usedSpecies.Add(firstPreEvolution.Index);
            var secondPreEvolution = eligibleSpecies.FirstOrDefault(pokemon =>
                pokemon.Evolutions.Any(evolution => evolution.EvolvedSpeciesId == firstPreEvolution.Index));
            if (secondPreEvolution is not null)
            {
                usedSpecies.Add(secondPreEvolution.Index);
            }
        }

        var species = eligibleSpecies.FirstOrDefault(pokemon => pokemon.Index == speciesId);
        if (species is null)
        {
            return;
        }

        foreach (var evolution in species.Evolutions.Where(evolution => evolution.EvolvedSpeciesId > 0))
        {
            usedSpecies.Add(evolution.EvolvedSpeciesId);
            var evolvedSpecies = eligibleSpecies.FirstOrDefault(pokemon => pokemon.Index == evolution.EvolvedSpeciesId);
            if (evolvedSpecies is null)
            {
                continue;
            }

            foreach (var secondEvolution in evolvedSpecies.Evolutions.Where(evolution => evolution.EvolvedSpeciesId > 0))
            {
                usedSpecies.Add(secondEvolution.EvolvedSpeciesId);
            }
        }
    }

    private const int LegacyRandomizedTrainerHappiness = 128;

    private static bool IsEligibleRandomSpecies(ColosseumPokemonStats pokemon)
        => pokemon.Index > 0 && pokemon.NameId > 0 && pokemon.CatchRate > 0;

    private static bool HasPokemonName(ColosseumPokemonStats pokemon)
        => pokemon.Index > 0 && pokemon.NameId > 0;

    private static bool IsUsableMove(ColosseumMove move)
        => move.Index > 0 && move.NameId > 0 && move.DescriptionId > 0;

    private static bool IsWonderGuard(string name)
        => string.Equals(Simplify(name), "wonderguard", StringComparison.Ordinal);

    private static string Simplify(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static T RandomElement<T>(IReadOnlyList<T> values)
        => values.Count == 0 ? throw new InvalidOperationException("Cannot choose a random value from an empty list.") : values[Random.Shared.Next(values.Count)];

    private static ColosseumTrainerPokemonUpdate TrainerPokemonUpdateFor(
        ColosseumTrainerPokemon pokemon,
        int speciesId,
        IReadOnlyList<int> moveIds,
        int shadowCatchRate,
        int happiness)
        => new(
            pokemon.Index,
            speciesId,
            pokemon.Level,
            pokemon.ShadowId,
            pokemon.ItemId,
            pokemon.PokeballId,
            pokemon.Ability,
            pokemon.Nature,
            pokemon.Gender,
            happiness,
            pokemon.Iv,
            pokemon.Evs,
            moveIds,
            pokemon.ShadowData?.HeartGauge ?? 0,
            pokemon.ShadowData?.FirstTrainerId ?? 0,
            pokemon.ShadowData?.AlternateFirstTrainerId ?? 0,
            shadowCatchRate);

    private static ColosseumPokemonStatsUpdate PokemonStatsUpdateFor(
        ColosseumPokemonStats pokemon,
        int? type1 = null,
        int? type2 = null,
        int? ability1 = null,
        int? ability2 = null,
        int? hp = null,
        int? attack = null,
        int? defense = null,
        int? specialAttack = null,
        int? specialDefense = null,
        int? speed = null,
        IReadOnlyList<ColosseumPokemonLevelUpMove>? levelUpMoves = null,
        IReadOnlyList<ColosseumPokemonEvolution>? evolutions = null)
        => new(
            pokemon.Index,
            pokemon.NameId,
            pokemon.ExpRate,
            pokemon.GenderRatio,
            pokemon.BaseExp,
            pokemon.BaseHappiness,
            pokemon.Height,
            pokemon.Weight,
            type1 ?? pokemon.Type1,
            type2 ?? pokemon.Type2,
            ability1 ?? pokemon.Ability1,
            ability2 ?? pokemon.Ability2,
            pokemon.HeldItem1,
            pokemon.HeldItem2,
            pokemon.CatchRate,
            hp ?? pokemon.Hp,
            attack ?? pokemon.Attack,
            defense ?? pokemon.Defense,
            specialAttack ?? pokemon.SpecialAttack,
            specialDefense ?? pokemon.SpecialDefense,
            speed ?? pokemon.Speed,
            pokemon.HpYield,
            pokemon.AttackYield,
            pokemon.DefenseYield,
            pokemon.SpecialAttackYield,
            pokemon.SpecialDefenseYield,
            pokemon.SpeedYield,
            pokemon.LearnableTms,
            levelUpMoves ?? pokemon.LevelUpMoves,
            evolutions ?? pokemon.Evolutions);

    private static ColosseumMoveUpdate MoveUpdateFor(ColosseumMove move, int? typeId = null)
        => new(
            move.Index,
            move.NameId,
            move.DescriptionId,
            typeId ?? move.TypeId,
            move.TargetId,
            move.CategoryId,
            move.AnimationId,
            move.Animation2Id,
            move.EffectId,
            move.EffectTypeId,
            move.Power,
            move.Accuracy,
            move.Pp,
            move.Priority,
            move.EffectAccuracy,
            move.HmFlag,
            move.SoundBasedFlag,
            move.ContactFlag,
            move.KingsRockFlag,
            move.ProtectFlag,
            move.SnatchFlag,
            move.MagicCoatFlag,
            move.MirrorMoveFlag);

    private void RefreshItemForTm(int tmIndex)
    {
        var itemIndex = FirstTmItemIndex + tmIndex - 1;
        if (_itemData.IsValueCreated && _itemData.Value is Dictionary<int, ColosseumItem> itemData)
        {
            itemData[itemIndex] = ReadItem(itemIndex);
        }
    }

    private sealed record RandomizedSpeciesCounts(int Gifts, int TrainerPokemon);

    private sealed record RandomizedMoveCounts(int TrainerPokemon, int Gifts, int LevelUpTables);

    private sealed record RandomizedStats(
        int Hp,
        int Attack,
        int Defense,
        int SpecialAttack,
        int SpecialDefense,
        int Speed);
}
