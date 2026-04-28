using OrreForge.Core.Binary;
using OrreForge.Core.GameCube;
using OrreForge.Core.Relocation;
using OrreForge.Core.Text;

namespace OrreForge.Colosseum.Data;

public sealed class ColosseumCommonRel
{
    private const int TrainerSize = 0x34;
    private const int TrainerPokemonCount = 6;
    private const int TrainerClassOffset = 0x03;
    private const int TrainerFirstPokemonOffset = 0x04;
    private const int TrainerAiOffset = 0x06;
    private const int TrainerNameIdOffset = 0x08;
    private const int TrainerModelOffset = 0x13;
    private const int TrainerFirstItemOffset = 0x14;
    private const int TrainerPreBattleTextIdOffset = 0x24;
    private const int TrainerVictoryTextIdOffset = 0x28;
    private const int TrainerDefeatTextIdOffset = 0x2c;

    private const int TrainerPokemonSize = 0x50;
    private const int PokemonAbilityOffset = 0x00;
    private const int PokemonGenderOffset = 0x01;
    private const int PokemonNatureOffset = 0x02;
    private const int PokemonShadowIdOffset = 0x03;
    private const int PokemonLevelOffset = 0x04;
    private const int PokemonHappinessOffset = 0x08;
    private const int PokemonSpeciesOffset = 0x0a;
    private const int PokemonPokeballOffset = 0x0d;
    private const int PokemonItemOffset = 0x12;
    private const int PokemonNameIdOffset = 0x14;
    private const int PokemonIvOffset = 0x1c;
    private const int PokemonFirstEvOffset = 0x23;
    private const int PokemonMove1Offset = 0x36;
    private const int PokemonMove2Offset = 0x3e;
    private const int PokemonMove3Offset = 0x46;
    private const int PokemonMove4Offset = 0x4e;

    private const int PokemonStatsSize = 0x11c;
    private const int PokemonStatsExpRateOffset = 0x00;
    private const int PokemonStatsCatchRateOffset = 0x01;
    private const int PokemonStatsGenderRatioOffset = 0x02;
    private const int PokemonStatsBaseExpOffset = 0x07;
    private const int PokemonStatsBaseHappinessOffset = 0x09;
    private const int PokemonStatsHeightOffset = 0x0a;
    private const int PokemonStatsWeightOffset = 0x0c;
    private const int PokemonStatsNationalIndexOffset = 0x10;
    private const int PokemonStatsNameIdOffset = 0x18;
    private const int PokemonStatsType1Offset = 0x30;
    private const int PokemonStatsType2Offset = 0x31;
    private const int PokemonStatsAbility1Offset = 0x32;
    private const int PokemonStatsAbility2Offset = 0x33;
    private const int PokemonStatsFirstTmOffset = 0x34;
    private const int PokemonStatsHeldItem1Offset = 0x70;
    private const int PokemonStatsHeldItem2Offset = 0x72;
    private const int PokemonStatsHpOffset = 0x85;
    private const int PokemonStatsAttackOffset = 0x87;
    private const int PokemonStatsDefenseOffset = 0x89;
    private const int PokemonStatsSpecialAttackOffset = 0x8b;
    private const int PokemonStatsSpecialDefenseOffset = 0x8d;
    private const int PokemonStatsSpeedOffset = 0x8f;
    private const int PokemonStatsFirstEvYieldOffset = 0x90;
    private const int PokemonStatsFirstEvolutionOffset = 0x9c;
    private const int PokemonStatsFirstLevelUpMoveOffset = 0xba;
    private const int PokemonStatsTmCount = 0x3a;
    private const int PokemonStatsEvolutionCount = 0x05;
    private const int PokemonStatsEvolutionSize = 0x06;
    private const int PokemonStatsEvolutionMethodOffset = 0x00;
    private const int PokemonStatsEvolutionConditionOffset = 0x02;
    private const int PokemonStatsEvolutionSpeciesOffset = 0x04;
    private const int PokemonStatsLevelUpMoveCount = 0x13;
    private const int PokemonStatsLevelUpMoveSize = 0x04;
    private const int PokemonStatsLevelUpMoveLevelOffset = 0x00;
    private const int PokemonStatsLevelUpMoveMoveOffset = 0x02;

    private const int MoveSize = 0x38;
    private const int MovePriorityOffset = 0x00;
    private const int MovePpOffset = 0x01;
    private const int MoveTypeOffset = 0x02;
    private const int MoveTargetsOffset = 0x03;
    private const int MoveAccuracyOffset = 0x04;
    private const int MoveEffectAccuracyOffset = 0x05;
    private const int MoveContactFlagOffset = 0x06;
    private const int MoveProtectFlagOffset = 0x07;
    private const int MoveMagicCoatFlagOffset = 0x08;
    private const int MoveSnatchFlagOffset = 0x09;
    private const int MoveMirrorMoveFlagOffset = 0x0a;
    private const int MoveKingsRockFlagOffset = 0x0b;
    private const int MoveSoundBasedFlagOffset = 0x10;
    private const int MoveHmFlagOffset = 0x12;
    private const int MoveBasePowerOffset = 0x17;
    private const int MoveEffectOffset = 0x1a;
    private const int MoveAnimationIndexOffset = 0x1c;
    private const int MoveCategoryOffset = 0x1f;
    private const int MoveNameIdOffset = 0x20;
    private const int MoveDescriptionIdOffset = 0x2c;
    private const int MoveAnimation2IndexOffset = 0x32;
    private const int MoveEffectTypeOffset = 0x34;

    private const int TrainerClassSize = 0x0c;
    private const int TrainerClassPayoutOffset = 0x00;
    private const int TrainerClassNameIdOffset = 0x04;

    private const int ShadowDataSize = 0x38;
    private const int ShadowCatchRateOffset = 0x00;
    private const int ShadowSpeciesOffset = 0x02;
    private const int ShadowFirstTrainerOffset = 0x04;
    private const int ShadowAlternateFirstTrainerOffset = 0x06;
    private const int ShadowHeartGaugeOffset = 0x08;

    private const int ItemCount = 0x18d;
    private const int ItemSize = 0x28;
    private const int ItemNameIdOffset = 0x10;

    private const int BattleSize = 0x38;
    private const int BattleTypeOffset = 0x00;
    private const int BattleStyleOffset = 0x01;
    private const int BattleBgmOffset = 0x0c;
    private const int BattlePlayersOffset = 0x18;
    private const int BattlePlayerSize = 0x08;
    private const int BattlePlayerTrainerIdOffset = 0x00;
    private const int TmEntrySize = 0x08;
    private const int TmMoveOffset = 0x06;

    private static readonly int[] PokemonUnsetFillOffsets =
    [
        0x00, 0x01, 0x02, 0x08, 0x09, 0x10, 0x11, 0x10, 0x13, 0x1c, 0x1d, 0x1e, 0x1f, 0x20,
        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d
    ];

    private const int DolTableToRamOffsetDifference = 0x3000;
    private const int CommonRelToRamOffsetDifferenceUs = 0x7628a0;

    private readonly BinaryData _data;
    private readonly BinaryData? _dol;
    private readonly GameCubeRegion _region;
    private readonly Lazy<IReadOnlyDictionary<int, ColosseumTrainerClass>> _trainerClasses;
    private readonly Lazy<IReadOnlyDictionary<int, ColosseumPokemonStats>> _pokemonStats;
    private readonly Lazy<IReadOnlyDictionary<int, ColosseumMove>> _moves;
    private readonly Lazy<IReadOnlyList<ColosseumTmMove>> _tmMoves;
    private readonly Lazy<IReadOnlyDictionary<int, string>> _items;
    private readonly Lazy<IReadOnlyDictionary<int, string>> _abilities;
    private readonly Lazy<IReadOnlyDictionary<int, ColosseumBattle>> _battles;
    private readonly IReadOnlyDictionary<int, string> _trainerModelNames;

    private ColosseumCommonRel(
        GameCubeRegion region,
        byte[] bytes,
        byte[]? startDolBytes,
        RelocationTable table,
        GameStringTable strings,
        IReadOnlyDictionary<int, string>? trainerModelNames)
    {
        _region = region;
        _data = new BinaryData(bytes);
        _dol = startDolBytes is null ? null : new BinaryData(startDolBytes);
        RelocationTable = table;
        Strings = strings;
        _trainerModelNames = trainerModelNames ?? new Dictionary<int, string>();
        _trainerClasses = new Lazy<IReadOnlyDictionary<int, ColosseumTrainerClass>>(LoadTrainerClasses);
        _pokemonStats = new Lazy<IReadOnlyDictionary<int, ColosseumPokemonStats>>(LoadPokemonStats);
        _moves = new Lazy<IReadOnlyDictionary<int, ColosseumMove>>(LoadMoves);
        _tmMoves = new Lazy<IReadOnlyList<ColosseumTmMove>>(LoadTmMoves);
        _items = new Lazy<IReadOnlyDictionary<int, string>>(LoadItems);
        _abilities = new Lazy<IReadOnlyDictionary<int, string>>(LoadAbilities);
        _battles = new Lazy<IReadOnlyDictionary<int, ColosseumBattle>>(LoadBattles);
    }

    public RelocationTable RelocationTable { get; }

    public GameStringTable Strings { get; }

    public IReadOnlyList<ColosseumPokemonStats> PokemonStats
        => _pokemonStats.Value.Values.OrderBy(pokemon => pokemon.Index).ToArray();

    public IReadOnlyList<ColosseumMove> Moves
        => _moves.Value.Values.OrderBy(move => move.Index).ToArray();

    public IReadOnlyList<ColosseumTmMove> TmMoves => _tmMoves.Value;

    public IReadOnlyList<ColosseumNamedResource> Items
        => _items.Value
            .OrderBy(item => item.Key)
            .Select(item => new ColosseumNamedResource(item.Key, item.Value))
            .ToArray();

    public IReadOnlyList<ColosseumNamedResource> Abilities
        => _abilities.Value
            .OrderBy(ability => ability.Key)
            .Select(ability => new ColosseumNamedResource(ability.Key, ability.Value))
            .ToArray();

    public IReadOnlyList<ColosseumNamedResource> Types
        => Enumerable.Range(0, 18)
            .Select(index => new ColosseumNamedResource(index, TypeName(index)))
            .ToArray();

    public int ShadowPokemonCount => GetCount(ColosseumCommonIndex.NumberOfShadowPokemon);

    public byte[] ToArray() => _data.ToArray();

    public static ColosseumCommonRel Parse(
        GameCubeRegion region,
        byte[] bytes,
        byte[]? startDolBytes = null,
        IReadOnlyDictionary<int, string>? trainerModelNames = null)
    {
        var table = RelocationTable.Parse(bytes);
        var stringIndex = ColosseumCommonIndexes.IndexFor(ColosseumCommonIndex.StringTable1, region);
        var stringBytes = table.ReadSymbol(stringIndex);
        if (stringBytes.Length == 0)
        {
            throw new InvalidDataException("Could not locate the Colosseum common.rel string table.");
        }

        return new ColosseumCommonRel(region, bytes, startDolBytes, table, GameStringTable.Parse(stringBytes), trainerModelNames);
    }

    public IReadOnlyList<ColosseumTrainer> LoadStoryTrainers()
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfTrainers);
        var trainers = new List<ColosseumTrainer>(Math.Max(0, count - 1));

        for (var index = 1; index < count; index++)
        {
            trainers.Add(ReadTrainer(index));
        }

        return trainers;
    }

    public ColosseumTrainer ReadTrainer(int index)
    {
        var trainerStart = PointerFor(ColosseumCommonIndex.Trainers) + (index * TrainerSize);
        var classId = _data.ReadByte(trainerStart + TrainerClassOffset);
        var firstPokemonIndex = _data.ReadUInt16(trainerStart + TrainerFirstPokemonOffset);
        var ai = _data.ReadUInt16(trainerStart + TrainerAiOffset);
        var nameId = checked((int)_data.ReadUInt32(trainerStart + TrainerNameIdOffset));
        var modelId = _data.ReadByte(trainerStart + TrainerModelOffset);
        var preBattleTextId = checked((int)_data.ReadUInt32(trainerStart + TrainerPreBattleTextIdOffset));
        var victoryTextId = checked((int)_data.ReadUInt32(trainerStart + TrainerVictoryTextIdOffset));
        var defeatTextId = checked((int)_data.ReadUInt32(trainerStart + TrainerDefeatTextIdOffset));
        var items = new List<int>(4);
        for (var offset = 0; offset < 8; offset += 2)
        {
            items.Add(_data.ReadUInt16(trainerStart + TrainerFirstItemOffset + offset));
        }

        var pokemon = ReadTrainerPokemon(firstPokemonIndex);
        var trainerClass = _trainerClasses.Value.TryGetValue(classId, out var klass)
            ? klass
            : new ColosseumTrainerClass(classId, 0, 0, $"Class {classId}");

        return new ColosseumTrainer(
            index,
            classId,
            trainerClass.Name,
            modelId,
            TrainerModelName(modelId),
            ai,
            nameId,
            Strings.StringWithId(nameId),
            firstPokemonIndex,
            pokemon,
            items,
            preBattleTextId,
            victoryTextId,
            defeatTextId,
            BattleForTrainer(index));
    }

    public ColosseumBattle? BattleForTrainer(int trainerId)
        => _battles.Value.Values.FirstOrDefault(battle => battle.TrainerIds.Contains(trainerId));

    private IReadOnlyList<ColosseumTrainerPokemon> ReadTrainerPokemon(int firstPokemonIndex)
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfTrainerPokemonData);
        if (firstPokemonIndex < 0 || firstPokemonIndex >= count)
        {
            return [];
        }

        var pokemon = new List<ColosseumTrainerPokemon>(TrainerPokemonCount);
        for (var slot = 0; slot < TrainerPokemonCount; slot++)
        {
            var index = firstPokemonIndex + slot;
            if (index >= count)
            {
                break;
            }

            pokemon.Add(ReadTrainerPokemon(slot, index));
        }

        return pokemon;
    }

    private ColosseumTrainerPokemon ReadTrainerPokemon(int slot, int index)
    {
        var start = PointerFor(ColosseumCommonIndex.TrainerPokemonData) + (index * TrainerPokemonSize);
        var speciesId = _data.ReadUInt16(start + PokemonSpeciesOffset);
        var itemId = _data.ReadUInt16(start + PokemonItemOffset);
        var pokeballId = _data.ReadByte(start + PokemonPokeballOffset);
        var ability = _data.ReadByte(start + PokemonAbilityOffset);
        var nature = _data.ReadByte(start + PokemonNatureOffset);
        var gender = _data.ReadByte(start + PokemonGenderOffset);
        var shadowId = _data.ReadByte(start + PokemonShadowIdOffset);
        var moveIds = new[]
        {
            _data.ReadUInt16(start + PokemonMove1Offset),
            _data.ReadUInt16(start + PokemonMove2Offset),
            _data.ReadUInt16(start + PokemonMove3Offset),
            _data.ReadUInt16(start + PokemonMove4Offset)
        };
        var evs = new List<int>(6);
        for (var evIndex = 0; evIndex < 6; evIndex++)
        {
            evs.Add(_data.ReadByte(start + PokemonFirstEvOffset + (evIndex * 2)));
        }

        var species = _pokemonStats.Value.TryGetValue(speciesId, out var stats)
            ? stats
            : UnknownPokemonStats(speciesId);

        return new ColosseumTrainerPokemon(
            slot,
            index,
            speciesId,
            species.Name,
            _data.ReadByte(start + PokemonLevelOffset),
            shadowId,
            itemId,
            NameForItem(itemId),
            pokeballId,
            NameForItem(pokeballId),
            ability,
            AbilityName(ability, species),
            nature,
            NatureName(nature),
            gender,
            GenderName(gender),
            _data.ReadByte(start + PokemonHappinessOffset),
            _data.ReadByte(start + PokemonIvOffset),
            evs,
            moveIds.Select(id => MoveFor(id)).ToArray(),
            ReadShadowData(shadowId));
    }

    public void WriteTrainerPokemon(ColosseumTrainerPokemonUpdate pokemon)
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfTrainerPokemonData);
        if (pokemon.Index < 0 || pokemon.Index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(pokemon), $"Trainer Pokemon index {pokemon.Index} is outside the table.");
        }

        var species = PokemonStatsFor(pokemon.SpeciesId);
        var speciesId = ClampUInt16(pokemon.SpeciesId);
        var shadowId = ClampByte(pokemon.ShadowId);

        if (shadowId > 0 && shadowId < ShadowPokemonCount)
        {
            var shadowStart = PointerFor(ColosseumCommonIndex.ShadowData) + (shadowId * ShadowDataSize);
            _data.WriteByte(shadowStart + ShadowCatchRateOffset, ClampByte(pokemon.ShadowCatchRate));
            _data.WriteUInt16(shadowStart + ShadowHeartGaugeOffset, ClampUInt16(pokemon.ShadowHeartGauge));
            _data.WriteUInt16(shadowStart + ShadowFirstTrainerOffset, ClampUInt16(pokemon.ShadowFirstTrainerId));
            _data.WriteUInt16(shadowStart + ShadowAlternateFirstTrainerOffset, ClampUInt16(pokemon.ShadowAlternateFirstTrainerId));
            _data.WriteUInt16(shadowStart + ShadowSpeciesOffset, speciesId);
        }

        var start = PointerFor(ColosseumCommonIndex.TrainerPokemonData) + (pokemon.Index * TrainerPokemonSize);
        if (pokemon.SpeciesId > 0)
        {
            _data.WriteByte(start + PokemonPokeballOffset, ClampByte(pokemon.PokeballId));
            foreach (var offset in PokemonUnsetFillOffsets)
            {
                _data.WriteByte(start + offset, 0);
            }
        }

        _data.WriteByte(start + PokemonShadowIdOffset, shadowId);
        _data.WriteUInt16(start + PokemonSpeciesOffset, speciesId);
        _data.WriteUInt32(start + PokemonNameIdOffset, checked((uint)(species?.NameId ?? 0)));
        _data.WriteUInt16(start + PokemonItemOffset, ClampUInt16(pokemon.ItemId));
        _data.WriteByte(start + PokemonHappinessOffset, ClampByte(pokemon.Happiness));
        _data.WriteByte(start + PokemonLevelOffset, ClampByte(pokemon.Level));
        _data.WriteByte(start + PokemonAbilityOffset, ClampByte(NormalizeAbility(pokemon.Ability)));
        _data.WriteByte(start + PokemonNatureOffset, ClampByte(pokemon.Nature));
        _data.WriteByte(start + PokemonGenderOffset, ClampByte(pokemon.Gender));

        var iv = ClampByte(pokemon.Iv > 31 ? 31 : pokemon.Iv);
        for (var index = 0; index < 6; index++)
        {
            _data.WriteByte(start + PokemonIvOffset + index, iv);
        }

        for (var evIndex = 0; evIndex < 6; evIndex++)
        {
            var value = evIndex < pokemon.Evs.Count ? pokemon.Evs[evIndex] : 0;
            _data.WriteByte(start + PokemonFirstEvOffset + (evIndex * 2), ClampByte(value));
        }

        var moveOffsets = new[] { PokemonMove1Offset, PokemonMove2Offset, PokemonMove3Offset, PokemonMove4Offset };
        for (var moveIndex = 0; moveIndex < moveOffsets.Length; moveIndex++)
        {
            var moveId = moveIndex < pokemon.MoveIds.Count ? pokemon.MoveIds[moveIndex] : 0;
            _data.WriteUInt16(start + moveOffsets[moveIndex], ClampUInt16(moveId));
        }

        if (pokemon.SpeciesId == 0)
        {
            _data.WriteByte(start + PokemonPokeballOffset, 0);
            foreach (var offset in PokemonUnsetFillOffsets)
            {
                _data.WriteByte(start + offset, 0xff);
            }
        }
    }

    private IReadOnlyDictionary<int, ColosseumPokemonStats> LoadPokemonStats()
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        var pokemon = new Dictionary<int, ColosseumPokemonStats>();

        for (var index = 0; index < count; index++)
        {
            pokemon[index] = ReadPokemonStats(index);
        }

        return pokemon;
    }

    private ColosseumPokemonStats ReadPokemonStats(int index)
    {
        var offset = PointerFor(ColosseumCommonIndex.PokemonStats) + (index * PokemonStatsSize);
        var type1 = _data.ReadByte(offset + PokemonStatsType1Offset);
        var type2 = _data.ReadByte(offset + PokemonStatsType2Offset);
        var ability1 = _data.ReadByte(offset + PokemonStatsAbility1Offset);
        var ability2 = _data.ReadByte(offset + PokemonStatsAbility2Offset);
        var item1 = _data.ReadUInt16(offset + PokemonStatsHeldItem1Offset);
        var item2 = _data.ReadUInt16(offset + PokemonStatsHeldItem2Offset);
        var nameId = checked((int)_data.ReadUInt32(offset + PokemonStatsNameIdOffset));
        var expRate = _data.ReadByte(offset + PokemonStatsExpRateOffset);
        var genderRatio = _data.ReadByte(offset + PokemonStatsGenderRatioOffset);
        var learnableTms = ReadPokemonStatsLearnableTms(offset);
        var levelUpMoves = ReadPokemonStatsLevelUpMoves(offset);
        var evolutions = ReadPokemonStatsEvolutions(offset);

        return new ColosseumPokemonStats(
            index,
            offset,
            Strings.StringWithId(nameId),
            nameId,
            _data.ReadUInt16(offset + PokemonStatsNationalIndexOffset),
            expRate,
            ExpRateName(expRate),
            genderRatio,
            GenderRatioName(genderRatio),
            _data.ReadByte(offset + PokemonStatsBaseExpOffset),
            _data.ReadByte(offset + PokemonStatsBaseHappinessOffset),
            _data.ReadUInt16(offset + PokemonStatsHeightOffset) / 10d,
            _data.ReadUInt16(offset + PokemonStatsWeightOffset) / 10d,
            type1,
            TypeName(type1),
            type2,
            TypeName(type2),
            ability1,
            NameForAbility(ability1),
            ability2,
            NameForAbility(ability2),
            item1,
            NameForItem(item1),
            item2,
            NameForItem(item2),
            _data.ReadByte(offset + PokemonStatsCatchRateOffset),
            _data.ReadByte(offset + PokemonStatsHpOffset),
            _data.ReadByte(offset + PokemonStatsAttackOffset),
            _data.ReadByte(offset + PokemonStatsDefenseOffset),
            _data.ReadByte(offset + PokemonStatsSpecialAttackOffset),
            _data.ReadByte(offset + PokemonStatsSpecialDefenseOffset),
            _data.ReadByte(offset + PokemonStatsSpeedOffset),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset + 2),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset + 4),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset + 6),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset + 8),
            _data.ReadUInt16(offset + PokemonStatsFirstEvYieldOffset + 10),
            learnableTms,
            levelUpMoves,
            evolutions);
    }

    public void WritePokemonStats(ColosseumPokemonStatsUpdate pokemon)
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        if (pokemon.Index < 0 || pokemon.Index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(pokemon), $"Pokemon stats index {pokemon.Index} is outside the table.");
        }

        var offset = PointerFor(ColosseumCommonIndex.PokemonStats) + (pokemon.Index * PokemonStatsSize);
        _data.WriteByte(offset + PokemonStatsExpRateOffset, ClampByte(pokemon.ExpRate));
        _data.WriteByte(offset + PokemonStatsCatchRateOffset, ClampByte(pokemon.CatchRate));
        _data.WriteByte(offset + PokemonStatsGenderRatioOffset, ClampByte(pokemon.GenderRatio));
        _data.WriteByte(offset + PokemonStatsBaseExpOffset, ClampByte(pokemon.BaseExp));
        _data.WriteByte(offset + PokemonStatsBaseHappinessOffset, ClampByte(pokemon.BaseHappiness));
        _data.WriteUInt16(offset + PokemonStatsHeightOffset, ClampScaledUInt16(pokemon.Height));
        _data.WriteUInt16(offset + PokemonStatsWeightOffset, ClampScaledUInt16(pokemon.Weight));
        _data.WriteUInt32(offset + PokemonStatsNameIdOffset, checked((uint)Math.Clamp(pokemon.NameId, 0, int.MaxValue)));
        _data.WriteByte(offset + PokemonStatsType1Offset, ClampByte(pokemon.Type1));
        _data.WriteByte(offset + PokemonStatsType2Offset, ClampByte(pokemon.Type2));
        _data.WriteByte(offset + PokemonStatsAbility1Offset, ClampByte(pokemon.Ability1));
        _data.WriteByte(offset + PokemonStatsAbility2Offset, ClampByte(pokemon.Ability2));
        _data.WriteUInt16(offset + PokemonStatsHeldItem1Offset, ClampUInt16(pokemon.HeldItem1));
        _data.WriteUInt16(offset + PokemonStatsHeldItem2Offset, ClampUInt16(pokemon.HeldItem2));
        _data.WriteByte(offset + PokemonStatsHpOffset, ClampByte(pokemon.Hp));
        _data.WriteByte(offset + PokemonStatsAttackOffset, ClampByte(pokemon.Attack));
        _data.WriteByte(offset + PokemonStatsDefenseOffset, ClampByte(pokemon.Defense));
        _data.WriteByte(offset + PokemonStatsSpecialAttackOffset, ClampByte(pokemon.SpecialAttack));
        _data.WriteByte(offset + PokemonStatsSpecialDefenseOffset, ClampByte(pokemon.SpecialDefense));
        _data.WriteByte(offset + PokemonStatsSpeedOffset, ClampByte(pokemon.Speed));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset, ClampUInt16(pokemon.HpYield));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset + 2, ClampUInt16(pokemon.AttackYield));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset + 4, ClampUInt16(pokemon.DefenseYield));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset + 6, ClampUInt16(pokemon.SpecialAttackYield));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset + 8, ClampUInt16(pokemon.SpecialDefenseYield));
        _data.WriteUInt16(offset + PokemonStatsFirstEvYieldOffset + 10, ClampUInt16(pokemon.SpeedYield));
        for (var index = 0; index < PokemonStatsTmCount; index++)
        {
            var isLearnable = index < pokemon.LearnableTms.Count && pokemon.LearnableTms[index];
            _data.WriteByte(offset + PokemonStatsFirstTmOffset + index, isLearnable ? (byte)1 : (byte)0);
        }

        for (var index = 0; index < PokemonStatsLevelUpMoveCount; index++)
        {
            var move = index < pokemon.LevelUpMoves.Count
                ? pokemon.LevelUpMoves[index]
                : new ColosseumPokemonLevelUpMove(index, 0, 0, "-");
            var start = offset + PokemonStatsFirstLevelUpMoveOffset + (index * PokemonStatsLevelUpMoveSize);
            _data.WriteByte(start + PokemonStatsLevelUpMoveLevelOffset, ClampByte(move.Level));
            _data.WriteUInt16(start + PokemonStatsLevelUpMoveMoveOffset, ClampUInt16(move.MoveId));
        }

        for (var index = 0; index < PokemonStatsEvolutionCount; index++)
        {
            var evolution = index < pokemon.Evolutions.Count
                ? pokemon.Evolutions[index]
                : new ColosseumPokemonEvolution(index, 0, "None", 0, "-", 0, "-");
            var start = offset + PokemonStatsFirstEvolutionOffset + (index * PokemonStatsEvolutionSize);
            _data.WriteByte(start + PokemonStatsEvolutionMethodOffset, ClampByte(evolution.Method));
            _data.WriteUInt16(start + PokemonStatsEvolutionConditionOffset, ClampUInt16(evolution.Condition));
            _data.WriteUInt16(start + PokemonStatsEvolutionSpeciesOffset, ClampUInt16(evolution.EvolvedSpeciesId));
        }

        if (_pokemonStats.IsValueCreated && _pokemonStats.Value is Dictionary<int, ColosseumPokemonStats> stats)
        {
            stats[pokemon.Index] = ReadPokemonStats(pokemon.Index);
        }
    }

    public void WriteMove(ColosseumMoveUpdate move)
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfMoves);
        if (move.Index <= 0 || move.Index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(move), $"Move index {move.Index} is outside the editable table.");
        }

        var offset = PointerFor(ColosseumCommonIndex.Moves) + (move.Index * MoveSize);
        _data.WriteByte(offset + MovePriorityOffset, SignedPriorityByte(move.Priority));
        _data.WriteByte(offset + MovePpOffset, ClampByte(move.Pp));
        _data.WriteByte(offset + MoveTypeOffset, ClampByte(move.TypeId));
        _data.WriteByte(offset + MoveTargetsOffset, ClampByte(move.TargetId));
        _data.WriteByte(offset + MoveAccuracyOffset, ClampByte(move.Accuracy));
        _data.WriteByte(offset + MoveEffectAccuracyOffset, ClampByte(move.EffectAccuracy));
        _data.WriteByte(offset + MoveContactFlagOffset, BoolByte(move.ContactFlag));
        _data.WriteByte(offset + MoveProtectFlagOffset, BoolByte(move.ProtectFlag));
        _data.WriteByte(offset + MoveMagicCoatFlagOffset, BoolByte(move.MagicCoatFlag));
        _data.WriteByte(offset + MoveSnatchFlagOffset, BoolByte(move.SnatchFlag));
        _data.WriteByte(offset + MoveMirrorMoveFlagOffset, BoolByte(move.MirrorMoveFlag));
        _data.WriteByte(offset + MoveKingsRockFlagOffset, BoolByte(move.KingsRockFlag));
        _data.WriteByte(offset + MoveSoundBasedFlagOffset, BoolByte(move.SoundBasedFlag));
        _data.WriteByte(offset + MoveHmFlagOffset, BoolByte(move.HmFlag));
        _data.WriteByte(offset + MoveBasePowerOffset, ClampByte(move.Power));
        _data.WriteUInt16(offset + MoveEffectOffset, ClampUInt16(move.EffectId));
        _data.WriteUInt16(offset + MoveAnimationIndexOffset, ClampUInt16(move.AnimationId));
        _data.WriteByte(offset + MoveCategoryOffset, ClampByte(move.CategoryId));
        _data.WriteUInt32(offset + MoveNameIdOffset, checked((uint)Math.Clamp(move.NameId, 0, int.MaxValue)));
        _data.WriteUInt32(offset + MoveDescriptionIdOffset, checked((uint)Math.Clamp(move.DescriptionId, 0, int.MaxValue)));
        _data.WriteUInt16(offset + MoveAnimation2IndexOffset, ClampUInt16(move.Animation2Id));
        _data.WriteByte(offset + MoveEffectTypeOffset, ClampByte(move.EffectTypeId));

        if (_moves.IsValueCreated && _moves.Value is Dictionary<int, ColosseumMove> moves)
        {
            moves[move.Index] = ReadMove(move.Index, PointerFor(ColosseumCommonIndex.Moves), UseHmFlagForShadowMoves(count));
        }
    }

    public ColosseumPokemonStats? PokemonStatsFor(int id)
        => _pokemonStats.Value.TryGetValue(id, out var pokemon) ? pokemon : null;

    public ColosseumMove MoveById(int id)
        => MoveFor(id);

    public string ItemNameById(int id)
        => NameForItem(id);

    public ColosseumShadowPokemonData? ShadowDataById(int id)
        => ReadShadowData(id);

    private IReadOnlyDictionary<int, ColosseumMove> LoadMoves()
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfMoves);
        var start = PointerFor(ColosseumCommonIndex.Moves);
        var useHmFlagForShadowMoves = UseHmFlagForShadowMoves(count);
        var moves = new Dictionary<int, ColosseumMove>();

        for (var index = 0; index < count; index++)
        {
            moves[index] = ReadMove(index, start, useHmFlagForShadowMoves);
        }

        return moves;
    }

    private ColosseumMove ReadMove(int index, int tableStart, bool useHmFlagForShadowMoves)
    {
        var offset = tableStart + (index * MoveSize);
        var nameId = checked((int)_data.ReadUInt32(offset + MoveNameIdOffset));
        var descriptionId = checked((int)_data.ReadUInt32(offset + MoveDescriptionIdOffset));
        var typeId = _data.ReadByte(offset + MoveTypeOffset);
        var targetId = _data.ReadByte(offset + MoveTargetsOffset);
        var categoryId = _data.ReadByte(offset + MoveCategoryOffset);
        var effectId = _data.ReadUInt16(offset + MoveEffectOffset);
        var effectTypeId = _data.ReadByte(offset + MoveEffectTypeOffset);
        var hmFlag = _data.ReadByte(offset + MoveHmFlagOffset) == 1;
        var isShadow = useHmFlagForShadowMoves
            ? hmFlag
            : index is >= 0x164 and <= 0x164;

        return new ColosseumMove(
            index,
            offset,
            Strings.StringWithId(nameId),
            nameId,
            descriptionId == 0 ? "-" : $"String {descriptionId}",
            descriptionId,
            typeId,
            TypeName(typeId),
            targetId,
            MoveTargetName(targetId),
            categoryId,
            MoveCategoryName(categoryId),
            _data.ReadUInt16(offset + MoveAnimationIndexOffset),
            _data.ReadUInt16(offset + MoveAnimation2IndexOffset),
            effectId,
            MoveEffectName(effectId),
            effectTypeId,
            MoveEffectTypeName(effectTypeId),
            _data.ReadByte(offset + MoveBasePowerOffset),
            _data.ReadByte(offset + MoveAccuracyOffset),
            _data.ReadByte(offset + MovePpOffset),
            SignedByte(_data.ReadByte(offset + MovePriorityOffset)),
            _data.ReadByte(offset + MoveEffectAccuracyOffset),
            hmFlag,
            _data.ReadByte(offset + MoveSoundBasedFlagOffset) == 1,
            _data.ReadByte(offset + MoveContactFlagOffset) == 1,
            _data.ReadByte(offset + MoveKingsRockFlagOffset) == 1,
            _data.ReadByte(offset + MoveProtectFlagOffset) == 1,
            _data.ReadByte(offset + MoveSnatchFlagOffset) == 1,
            _data.ReadByte(offset + MoveMagicCoatFlagOffset) == 1,
            _data.ReadByte(offset + MoveMirrorMoveFlagOffset) == 1,
            isShadow);
    }

    private IReadOnlyList<ColosseumTmMove> LoadTmMoves()
    {
        var dol = _dol;
        var start = FirstTmListOffset(_region);
        if (dol is null || start <= 0)
        {
            return [];
        }

        var tms = new List<ColosseumTmMove>(PokemonStatsTmCount);
        for (var index = 1; index <= PokemonStatsTmCount; index++)
        {
            var offset = start + TmMoveOffset + ((index - 1) * TmEntrySize);
            if (offset + 2 > dol.Length)
            {
                break;
            }

            var moveId = dol.ReadUInt16(offset);
            var move = MoveFor(moveId);
            tms.Add(new ColosseumTmMove(index, moveId, move.Name, move.TypeId, move.TypeName));
        }

        return tms;
    }

    private IReadOnlyDictionary<int, string> LoadItems()
    {
        var dol = _dol;
        var itemStart = FirstItemOffset(_region);
        if (dol is null || itemStart <= 0 || itemStart + ItemSize > dol.Length)
        {
            return new Dictionary<int, string>();
        }

        var items = new Dictionary<int, string>();
        for (var index = 0; index < ItemCount; index++)
        {
            var offset = itemStart + (index * ItemSize);
            if (offset + ItemSize > dol.Length)
            {
                break;
            }

            var nameId = checked((int)dol.ReadUInt32(offset + ItemNameIdOffset));
            items[index] = Strings.StringWithId(nameId);
        }

        return items;
    }

    private IReadOnlyDictionary<int, string> LoadAbilities()
    {
        var dol = _dol;
        var abilityFunctionOffset = AbilityDataFunctionOffset(_region);
        var numberOffset = NumberOfAbilitiesOffset(_region);
        if (dol is null
            || abilityFunctionOffset <= 0
            || numberOffset <= 0
            || abilityFunctionOffset + 36 > dol.Length
            || numberOffset + 4 > dol.Length)
        {
            return new Dictionary<int, string>();
        }

        var entrySize = dol.ReadUInt16(abilityFunctionOffset + 26);
        if (entrySize is not (8 or 12))
        {
            return new Dictionary<int, string>();
        }

        var high = dol.ReadUInt16(abilityFunctionOffset + 30) & 0x0fff;
        var low = (short)dol.ReadUInt16(abilityFunctionOffset + 34);
        var ramOffset = (high << 16) + low;
        BinaryData data;
        int tableOffset;
        if (ramOffset < dol.Length + DolTableToRamOffsetDifference)
        {
            data = dol;
            tableOffset = ramOffset - DolTableToRamOffsetDifference;
        }
        else
        {
            data = _data;
            tableOffset = ramOffset - CommonRelToRamOffsetDifferenceUs;
        }

        var count = checked((int)dol.ReadUInt32(numberOffset));
        if (tableOffset < 0 || tableOffset >= data.Length || count <= 0)
        {
            return new Dictionary<int, string>();
        }

        var nameIdOffset = entrySize == 8 ? 0 : 4;
        var abilities = new Dictionary<int, string>();
        for (var index = 0; index < count; index++)
        {
            var offset = tableOffset + (index * entrySize);
            if (offset + entrySize > data.Length)
            {
                break;
            }

            var nameId = checked((int)data.ReadUInt32(offset + nameIdOffset));
            abilities[index] = Strings.StringWithId(nameId);
        }

        return abilities;
    }

    private ColosseumShadowPokemonData? ReadShadowData(int shadowId)
    {
        if (shadowId <= 0 || shadowId >= GetCount(ColosseumCommonIndex.NumberOfShadowPokemon))
        {
            return null;
        }

        var start = PointerFor(ColosseumCommonIndex.ShadowData) + (shadowId * ShadowDataSize);
        return new ColosseumShadowPokemonData(
            shadowId,
            _data.ReadByte(start + ShadowCatchRateOffset),
            _data.ReadUInt16(start + ShadowSpeciesOffset),
            _data.ReadUInt16(start + ShadowFirstTrainerOffset),
            _data.ReadUInt16(start + ShadowAlternateFirstTrainerOffset),
            _data.ReadUInt16(start + ShadowHeartGaugeOffset));
    }

    private ColosseumMove MoveFor(int id)
        => _moves.Value.TryGetValue(id, out var move)
            ? move
            : new ColosseumMove(
                id,
                0,
                id == 0 ? "-" : $"Move {id}",
                0,
                "-",
                0,
                0,
                TypeName(0),
                0,
                MoveTargetName(0),
                0,
                MoveCategoryName(0),
                0,
                0,
                0,
                MoveEffectName(0),
                0,
                MoveEffectTypeName(0),
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);

    private ColosseumPokemonStats UnknownPokemonStats(int id)
        => new(
            id,
            0,
            id == 0 ? "-" : $"Pokemon {id}",
            0,
            0,
            0,
            ExpRateName(0),
            0xff,
            GenderRatioName(0xff),
            0,
            0,
            0,
            0,
            0,
            TypeName(0),
            0,
            TypeName(0),
            0,
            NameForAbility(0),
            0,
            NameForAbility(0),
            0,
            NameForItem(0),
            0,
            NameForItem(0),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            [],
            [],
            []);

    private IReadOnlyList<bool> ReadPokemonStatsLearnableTms(int offset)
    {
        var tms = new bool[PokemonStatsTmCount];
        for (var index = 0; index < tms.Length; index++)
        {
            tms[index] = _data.ReadByte(offset + PokemonStatsFirstTmOffset + index) == 1;
        }

        return tms;
    }

    private IReadOnlyList<ColosseumPokemonLevelUpMove> ReadPokemonStatsLevelUpMoves(int offset)
    {
        var moves = new List<ColosseumPokemonLevelUpMove>(PokemonStatsLevelUpMoveCount);
        for (var index = 0; index < PokemonStatsLevelUpMoveCount; index++)
        {
            var start = offset + PokemonStatsFirstLevelUpMoveOffset + (index * PokemonStatsLevelUpMoveSize);
            var level = _data.ReadByte(start + PokemonStatsLevelUpMoveLevelOffset);
            var moveId = _data.ReadUInt16(start + PokemonStatsLevelUpMoveMoveOffset);
            moves.Add(new ColosseumPokemonLevelUpMove(index, level, moveId, MoveFor(moveId).Name));
        }

        return moves;
    }

    private IReadOnlyList<ColosseumPokemonEvolution> ReadPokemonStatsEvolutions(int offset)
    {
        var evolutions = new List<ColosseumPokemonEvolution>(PokemonStatsEvolutionCount);
        for (var index = 0; index < PokemonStatsEvolutionCount; index++)
        {
            var start = offset + PokemonStatsFirstEvolutionOffset + (index * PokemonStatsEvolutionSize);
            var method = _data.ReadByte(start + PokemonStatsEvolutionMethodOffset);
            var condition = _data.ReadUInt16(start + PokemonStatsEvolutionConditionOffset);
            var speciesId = _data.ReadUInt16(start + PokemonStatsEvolutionSpeciesOffset);
            evolutions.Add(new ColosseumPokemonEvolution(
                index,
                method,
                EvolutionMethodName(method),
                condition,
                EvolutionConditionName(method, condition),
                speciesId,
                PokemonStatsNameFor(speciesId)));
        }

        return evolutions;
    }

    private string NameForItem(int id)
    {
        if (id is 0 or 0xffff)
        {
            return "-";
        }

        var normalized = NormalizeItemIndex(id);
        return _items.Value.TryGetValue(normalized, out var name) ? name : normalized == 0 ? "-" : $"Item {normalized}";
    }

    private string NameForAbility(int id)
        => _abilities.Value.TryGetValue(id, out var name) ? name : $"Ability {id}";

    private string AbilityName(int abilitySlot, ColosseumPokemonStats species)
        => abilitySlot switch
        {
            0 => species.Ability1Name,
            1 => species.Ability2Name,
            0xff => "Random",
            _ => "Random"
        };

    private static int NormalizeAbility(int ability)
        => ability is 0 or 1 ? ability : 0xff;

    private static byte ClampByte(int value)
        => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);

    private static ushort ClampUInt16(int value)
        => (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue);

    private static ushort ClampScaledUInt16(double value)
        => (ushort)Math.Clamp((int)Math.Round(value * 10d), ushort.MinValue, ushort.MaxValue);

    private static byte BoolByte(bool value)
        => value ? (byte)1 : (byte)0;

    private static int SignedByte(byte value)
        => value > 127 ? value - 256 : value;

    private static byte SignedPriorityByte(int value)
    {
        var clamped = Math.Clamp(value, -128, 127);
        return (byte)(clamped < 0 ? 256 + clamped : clamped);
    }

    private bool UseHmFlagForShadowMoves(int moveCount)
    {
        const int firstShadowMove = 0x164;
        if (moveCount <= firstShadowMove)
        {
            return false;
        }

        var start = PointerFor(ColosseumCommonIndex.Moves) + (firstShadowMove * MoveSize);
        return start + MoveHmFlagOffset < _data.Length
            && _data.ReadByte(start + MoveHmFlagOffset) == 1;
    }

    private static int NormalizeItemIndex(int index)
        => index > ItemCount && index < 0x250 ? index - 151 : index;

    private static string NatureName(int id)
        => id switch
        {
            0x00 => "Hardy",
            0x01 => "Lonely",
            0x02 => "Brave",
            0x03 => "Adamant",
            0x04 => "Naughty",
            0x05 => "Bold",
            0x06 => "Docile",
            0x07 => "Relaxed",
            0x08 => "Impish",
            0x09 => "Lax",
            0x0a => "Timid",
            0x0b => "Hasty",
            0x0c => "Serious",
            0x0d => "Jolly",
            0x0e => "Naive",
            0x0f => "Modest",
            0x10 => "Mild",
            0x11 => "Quiet",
            0x12 => "Bashful",
            0x13 => "Rash",
            0x14 => "Calm",
            0x15 => "Gentle",
            0x16 => "Sassy",
            0x17 => "Careful",
            0x18 => "Quirky",
            0xff => "Random",
            _ => $"Nature {id}"
        };

    private static string GenderName(int id)
        => id switch
        {
            0x00 => "Male",
            0x01 => "Female",
            0x02 => "Genderless",
            0xff => "Random",
            _ => $"Gender {id}"
        };

    private static string ExpRateName(int id)
        => id switch
        {
            0 => "Standard",
            1 => "Very Fast",
            2 => "Slowest",
            3 => "Slow",
            4 => "Fast",
            5 => "Very Slow",
            _ => $"Exp Rate {id}"
        };

    private static string GenderRatioName(int id)
        => id switch
        {
            0x00 => "Male Only",
            0x1f => "87.5% Male",
            0x3f => "75% Male",
            0x7f => "50% Male",
            0xbf => "75% Female",
            0xdf => "87.5% Female",
            0xfe => "Female Only",
            0xff => "Genderless",
            _ => $"Gender Ratio {id}"
        };

    private static string EvolutionMethodName(int id)
        => id switch
        {
            0x00 => "None",
            0x01 => "Max Happiness",
            0x02 => "Happiness (Day)",
            0x03 => "Happiness (Night)",
            0x04 => "Level Up",
            0x05 => "Trade",
            0x06 => "Trade With Item",
            0x07 => "Evolution Stone",
            0x08 => "Atk > Def",
            0x09 => "Atk = Def",
            0x0a => "Atk < Def",
            0x0b => "Silcoon evolution method",
            0x0c => "Cascoon evolution method",
            0x0d => "Ninjask evolution method",
            0x0e => "Shedinja evolution method",
            0x0f => "Max Beauty",
            0x10 => "Level Up With Key Item",
            0x11 => "Evolves in Generation 4 (XG)",
            _ => $"Evolution Method {id}"
        };

    private string EvolutionConditionName(int method, int condition)
        => EvolutionConditionKind(method) switch
        {
            EvolutionConditionValueKind.Level => $"Lv. {condition}",
            EvolutionConditionValueKind.Item => NameForItem(condition),
            _ => condition == 0 ? "-" : condition.ToString()
        };

    private static EvolutionConditionValueKind EvolutionConditionKind(int method)
        => method switch
        {
            0x04 or 0x08 or 0x09 or 0x0a or 0x0b or 0x0c or 0x0d or 0x0e => EvolutionConditionValueKind.Level,
            0x06 or 0x07 or 0x10 => EvolutionConditionValueKind.Item,
            _ => EvolutionConditionValueKind.None
        };

    private static string TypeName(int id)
        => id switch
        {
            0 => "Normal",
            1 => "Fighting",
            2 => "Flying",
            3 => "Poison",
            4 => "Ground",
            5 => "Rock",
            6 => "Bug",
            7 => "Ghost",
            8 => "Steel",
            9 => "???",
            10 => "Fire",
            11 => "Water",
            12 => "Grass",
            13 => "Electric",
            14 => "Psychic",
            15 => "Ice",
            16 => "Dragon",
            17 => "Dark",
            _ => $"Type {id}"
        };

    private static string MoveTargetName(int id)
        => id switch
        {
            0 => "Selected Target",
            1 => "Depends On Move",
            2 => "All Pokemon",
            3 => "Random",
            4 => "Both Foes",
            5 => "User",
            6 => "Both Foes and Ally",
            7 => "Opponent's Feet",
            _ => $"Target {id}"
        };

    private static string MoveCategoryName(int id)
        => id switch
        {
            0 => "Neither",
            1 => "Physical",
            2 => "Special",
            _ => $"Category {id}"
        };

    private static string MoveEffectName(int id)
        => id == 0 ? "-" : $"Effect {id}";

    private static string MoveEffectTypeName(int id)
        => id switch
        {
            0x00 => "None",
            0x01 => "Attack",
            0x02 => "Healing",
            0x03 => "Stat Nerf",
            0x04 => "Stat Buff",
            0x05 => "Status Effect",
            0x06 => "Field Effect",
            0x07 => "Affects Incoming Move",
            0x08 => "OHKO",
            0x09 => "Multi-Turn",
            0x0a => "Misc",
            0x0b => "Misc2",
            0x0c => "Misc3",
            0x0d => "Misc4",
            0x0e => "Unknown",
            _ => $"Effect Type {id}"
        };

    private static string BattleStyleName(int id)
        => id switch
        {
            0 => "None",
            1 => "Single",
            2 => "Double",
            3 => "Multi",
            _ => $"Battle Style {id}"
        };

    private static string BattleTypeName(int id)
        => id switch
        {
            0 => "None",
            1 => "Admin Battle",
            2 => "Story Battle",
            3 => "Colosseum Preliminary Round",
            4 => "Test",
            5 => "Colosseum Final Round",
            6 => "Phenac Colosseum Preliminary Round",
            7 => "Phenac Colosseum Final Round",
            8 => "Mt. Battle",
            9 => "Mt. Battle Final",
            10 => "E-Card Panel Battle",
            11 => "E-Card Endless Battle",
            12 => "Battle Mode Battle Now",
            13 => "Battle Mode Solo Colosseum",
            14 => "Battle Mode Solo Colosseum Final",
            15 => "Battle Mode Mt. Battle",
            16 => "Link Battle",
            17 => "E-Card Special Battle",
            18 => "Battle Mode Mt. Battle Final",
            _ => $"Battle Type {id}"
        };

    private static int FirstItemOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.UnitedStates => 0x360ce8,
            GameCubeRegion.Japan => 0x34d428,
            GameCubeRegion.Europe => 0x3adda0,
            _ => 0
        };

    private static int FirstTmListOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.UnitedStates => 0x365018,
            GameCubeRegion.Japan => 0x351758,
            GameCubeRegion.Europe => 0x3b20d0,
            _ => 0
        };

    private static int AbilityDataFunctionOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.UnitedStates => 0x119b6c,
            GameCubeRegion.Europe => 0x11db48,
            GameCubeRegion.Japan => 0x117354,
            _ => 0
        };

    private static int NumberOfAbilitiesOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.UnitedStates => 0x397a28,
            GameCubeRegion.Europe => 0x3e4ec8,
            GameCubeRegion.Japan => 0x384198,
            _ => 0
        };

    private IReadOnlyDictionary<int, ColosseumTrainerClass> LoadTrainerClasses()
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfTrainerClasses);
        var start = PointerFor(ColosseumCommonIndex.TrainerClasses);
        var classes = new Dictionary<int, ColosseumTrainerClass>();

        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * TrainerClassSize);
            var payout = _data.ReadUInt16(offset + TrainerClassPayoutOffset);
            var nameId = checked((int)_data.ReadUInt32(offset + TrainerClassNameIdOffset));
            classes[index] = new ColosseumTrainerClass(index, payout, nameId, Strings.StringWithId(nameId));
        }

        return classes;
    }

    private IReadOnlyDictionary<int, ColosseumBattle> LoadBattles()
    {
        var count = GetCount(ColosseumCommonIndex.NumberOfBattles);
        var start = PointerFor(ColosseumCommonIndex.Battles);
        var battles = new Dictionary<int, ColosseumBattle>();

        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * BattleSize);
            var battleType = _data.ReadByte(offset + BattleTypeOffset);
            var battleStyle = _data.ReadByte(offset + BattleStyleOffset);
            var bgmId = checked((int)_data.ReadUInt32(offset + BattleBgmOffset));
            var trainerIds = new List<int>(4);

            for (var player = 0; player < 4; player++)
            {
                var playerOffset = offset + BattlePlayersOffset + (player * BattlePlayerSize);
                trainerIds.Add(_data.ReadUInt16(playerOffset + BattlePlayerTrainerIdOffset));
            }

            battles[index] = new ColosseumBattle(
                index,
                battleType,
                BattleTypeName(battleType),
                battleStyle,
                BattleStyleName(battleStyle),
                bgmId,
                trainerIds);
        }

        return battles;
    }

    private string TrainerModelName(int modelId)
    {
        if (modelId == 0)
        {
            return "-";
        }

        return _trainerModelNames.TryGetValue(modelId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : $"Model {modelId}";
    }

    private string PokemonStatsNameFor(int id)
    {
        if (id == 0)
        {
            return "-";
        }

        var count = GetCount(ColosseumCommonIndex.NumberOfPokemon);
        if (id < 0 || id >= count)
        {
            return $"Pokemon {id}";
        }

        var offset = PointerFor(ColosseumCommonIndex.PokemonStats) + (id * PokemonStatsSize);
        var nameId = checked((int)_data.ReadUInt32(offset + PokemonStatsNameIdOffset));
        return Strings.StringWithId(nameId);
    }

    private int PointerFor(ColosseumCommonIndex index)
        => RelocationTable.GetPointer(ColosseumCommonIndexes.IndexFor(index, _region));

    private int GetCount(ColosseumCommonIndex index)
        => RelocationTable.GetValueAtPointer(ColosseumCommonIndexes.IndexFor(index, _region));

    private enum EvolutionConditionValueKind
    {
        None,
        Level,
        Item
    }
}
