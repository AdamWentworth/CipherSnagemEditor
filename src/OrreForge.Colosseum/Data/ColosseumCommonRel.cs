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
    private const int PokemonSpeciesOffset = 0x0a;
    private const int PokemonItemOffset = 0x12;
    private const int PokemonMove1Offset = 0x36;
    private const int PokemonMove2Offset = 0x3e;
    private const int PokemonMove3Offset = 0x46;
    private const int PokemonMove4Offset = 0x4e;

    private const int TrainerClassSize = 0x0c;
    private const int TrainerClassPayoutOffset = 0x00;
    private const int TrainerClassNameIdOffset = 0x04;

    private readonly BinaryData _data;
    private readonly GameCubeRegion _region;
    private readonly Lazy<IReadOnlyDictionary<int, ColosseumTrainerClass>> _trainerClasses;

    private ColosseumCommonRel(GameCubeRegion region, byte[] bytes, RelocationTable table, GameStringTable strings)
    {
        _region = region;
        _data = new BinaryData(bytes);
        RelocationTable = table;
        Strings = strings;
        _trainerClasses = new Lazy<IReadOnlyDictionary<int, ColosseumTrainerClass>>(LoadTrainerClasses);
    }

    public RelocationTable RelocationTable { get; }

    public GameStringTable Strings { get; }

    public static ColosseumCommonRel Parse(GameCubeRegion region, byte[] bytes)
    {
        var table = RelocationTable.Parse(bytes);
        var stringIndex = ColosseumCommonIndexes.IndexFor(ColosseumCommonIndex.StringTable1, region);
        var stringBytes = table.ReadSymbol(stringIndex);
        if (stringBytes.Length == 0)
        {
            throw new InvalidDataException("Could not locate the Colosseum common.rel string table.");
        }

        return new ColosseumCommonRel(region, bytes, table, GameStringTable.Parse(stringBytes));
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
        var moves = new[]
        {
            _data.ReadUInt16(start + PokemonMove1Offset),
            _data.ReadUInt16(start + PokemonMove2Offset),
            _data.ReadUInt16(start + PokemonMove3Offset),
            _data.ReadUInt16(start + PokemonMove4Offset)
        };

        return new ColosseumTrainerPokemon(
            slot,
            index,
            _data.ReadUInt16(start + PokemonSpeciesOffset),
            _data.ReadByte(start + PokemonLevelOffset),
            _data.ReadByte(start + PokemonShadowIdOffset),
            _data.ReadUInt16(start + PokemonItemOffset),
            _data.ReadByte(start + PokemonAbilityOffset),
            _data.ReadByte(start + PokemonNatureOffset),
            _data.ReadByte(start + PokemonGenderOffset),
            moves.Select(move => (int)move).ToArray());
    }

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
