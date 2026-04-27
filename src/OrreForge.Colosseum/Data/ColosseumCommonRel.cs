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
    private const int PokemonStatsCatchRateOffset = 0x01;
    private const int PokemonStatsNameIdOffset = 0x18;
    private const int PokemonStatsType1Offset = 0x30;
    private const int PokemonStatsType2Offset = 0x31;
    private const int PokemonStatsAbility1Offset = 0x32;
    private const int PokemonStatsAbility2Offset = 0x33;

    private const int MoveSize = 0x38;
    private const int MovePpOffset = 0x01;
    private const int MoveTypeOffset = 0x02;
    private const int MoveAccuracyOffset = 0x04;
    private const int MoveBasePowerOffset = 0x17;
    private const int MoveNameIdOffset = 0x20;

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
    private readonly Lazy<IReadOnlyDictionary<int, string>> _items;
    private readonly Lazy<IReadOnlyDictionary<int, string>> _abilities;

    private ColosseumCommonRel(GameCubeRegion region, byte[] bytes, byte[]? startDolBytes, RelocationTable table, GameStringTable strings)
    {
        _region = region;
        _data = new BinaryData(bytes);
        _dol = startDolBytes is null ? null : new BinaryData(startDolBytes);
        RelocationTable = table;
        Strings = strings;
        _trainerClasses = new Lazy<IReadOnlyDictionary<int, ColosseumTrainerClass>>(LoadTrainerClasses);
        _pokemonStats = new Lazy<IReadOnlyDictionary<int, ColosseumPokemonStats>>(LoadPokemonStats);
        _moves = new Lazy<IReadOnlyDictionary<int, ColosseumMove>>(LoadMoves);
        _items = new Lazy<IReadOnlyDictionary<int, string>>(LoadItems);
        _abilities = new Lazy<IReadOnlyDictionary<int, string>>(LoadAbilities);
    }

    public RelocationTable RelocationTable { get; }

    public GameStringTable Strings { get; }

    public IReadOnlyList<ColosseumPokemonStats> PokemonStats
        => _pokemonStats.Value.Values.OrderBy(pokemon => pokemon.Index).ToArray();

    public IReadOnlyList<ColosseumMove> Moves
        => _moves.Value.Values.OrderBy(move => move.Index).ToArray();

    public IReadOnlyList<ColosseumNamedResource> Items
        => _items.Value
            .OrderBy(item => item.Key)
            .Select(item => new ColosseumNamedResource(item.Key, item.Value))
            .ToArray();

    public int ShadowPokemonCount => GetCount(ColosseumCommonIndex.NumberOfShadowPokemon);

    public byte[] ToArray() => _data.ToArray();

    public static ColosseumCommonRel Parse(GameCubeRegion region, byte[] bytes, byte[]? startDolBytes = null)
    {
        var table = RelocationTable.Parse(bytes);
        var stringIndex = ColosseumCommonIndexes.IndexFor(ColosseumCommonIndex.StringTable1, region);
        var stringBytes = table.ReadSymbol(stringIndex);
        if (stringBytes.Length == 0)
        {
            throw new InvalidDataException("Could not locate the Colosseum common.rel string table.");
        }

        return new ColosseumCommonRel(region, bytes, startDolBytes, table, GameStringTable.Parse(stringBytes));
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
            ai,
            nameId,
            Strings.StringWithId(nameId),
            firstPokemonIndex,
            pokemon,
            items,
            preBattleTextId,
            victoryTextId,
            defeatTextId);
    }

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
            : new ColosseumPokemonStats(speciesId, speciesId == 0 ? "-" : $"Pokemon {speciesId}", 0, 0, "Normal", 0, "Normal", 0, "Ability 0", 0, "Ability 0", 0);

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
        var start = PointerFor(ColosseumCommonIndex.PokemonStats);
        var pokemon = new Dictionary<int, ColosseumPokemonStats>();

        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * PokemonStatsSize);
            var type1 = _data.ReadByte(offset + PokemonStatsType1Offset);
            var type2 = _data.ReadByte(offset + PokemonStatsType2Offset);
            var ability1 = _data.ReadByte(offset + PokemonStatsAbility1Offset);
            var ability2 = _data.ReadByte(offset + PokemonStatsAbility2Offset);
            var nameId = checked((int)_data.ReadUInt32(offset + PokemonStatsNameIdOffset));

            pokemon[index] = new ColosseumPokemonStats(
                index,
                Strings.StringWithId(nameId),
                nameId,
                type1,
                TypeName(type1),
                type2,
                TypeName(type2),
                ability1,
                NameForAbility(ability1),
                ability2,
                NameForAbility(ability2),
                _data.ReadByte(offset + PokemonStatsCatchRateOffset));
        }

        return pokemon;
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
        var moves = new Dictionary<int, ColosseumMove>();

        for (var index = 0; index < count; index++)
        {
            var offset = start + (index * MoveSize);
            var nameId = checked((int)_data.ReadUInt32(offset + MoveNameIdOffset));
            var typeId = _data.ReadByte(offset + MoveTypeOffset);
            moves[index] = new ColosseumMove(
                index,
                Strings.StringWithId(nameId),
                TypeName(typeId),
                _data.ReadByte(offset + MoveBasePowerOffset),
                _data.ReadByte(offset + MoveAccuracyOffset),
                _data.ReadByte(offset + MovePpOffset));
        }

        return moves;
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
            : new ColosseumMove(id, id == 0 ? "-" : $"Move {id}", "-", 0, 0, 0);

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

    private static int FirstItemOffset(GameCubeRegion region)
        => region switch
        {
            GameCubeRegion.UnitedStates => 0x360ce8,
            GameCubeRegion.Japan => 0x34d428,
            GameCubeRegion.Europe => 0x3adda0,
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

    private int PointerFor(ColosseumCommonIndex index)
        => RelocationTable.GetPointer(ColosseumCommonIndexes.IndexFor(index, _region));

    private int GetCount(ColosseumCommonIndex index)
        => RelocationTable.GetValueAtPointer(ColosseumCommonIndexes.IndexFor(index, _region));
}
