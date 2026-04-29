using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumRandomizerParityTests
{
    [Fact]
    public void RandomTypeIdsExcludeQuestionMarkTypeUntilItIsRenamed()
    {
        var types = Enumerable.Range(0, 18)
            .Select(index => Type(index, index == 9 ? "???" : $"Type {index}"))
            .ToArray();

        Assert.DoesNotContain(9, ColosseumCommonRel.RandomTypeIdsFor(types));

        types[9] = Type(9, "Fairy");

        Assert.Contains(9, ColosseumCommonRel.RandomTypeIdsFor(types));
    }

    [Fact]
    public void SpeciesDuplicateStrikeMatchesLegacyEvolutionLineRule()
    {
        var species = new[]
        {
            Stats(1, 2),
            Stats(2, 3),
            Stats(3, 4),
            Stats(4),
            Stats(7, 1)
        };
        var used = new HashSet<int>();

        ColosseumCommonRel.StrikeEvolutionLineForSpecies(2, species, used);

        Assert.Contains(1, used);
        Assert.Contains(2, used);
        Assert.Contains(3, used);
        Assert.Contains(4, used);
        Assert.Contains(7, used);
    }

    [Fact]
    public void ColosseumShopScriptItemIdsUseLegacyKeyItemShift()
    {
        Assert.Equal(0x015d, ColosseumPocketMenuRel.ScriptItemId(0x015d));
        Assert.Equal(0x01f5, ColosseumPocketMenuRel.ScriptItemId(0x015e));
        Assert.Equal(0x015e, ColosseumPocketMenuRel.NormalizeScriptItemId(0x01f5, 0x018d));
    }

    private static ColosseumTypeData Type(int index, string name)
        => new(index, 0, name, 3000 + index, 0, "Physical", Enumerable.Repeat(2, 18).ToArray());

    private static ColosseumPokemonStats Stats(int index, params int[] evolvesInto)
        => new(
            Index: index,
            StartOffset: 0,
            Name: $"Pokemon {index}",
            NameId: 1000 + index,
            NationalIndex: index,
            ExpRate: 0,
            ExpRateName: "Standard",
            GenderRatio: 0x7f,
            GenderRatioName: "50% Male",
            BaseExp: 0,
            BaseHappiness: 70,
            Height: 1,
            Weight: 1,
            Type1: 0,
            Type1Name: "Normal",
            Type2: 0,
            Type2Name: "Normal",
            Ability1: 1,
            Ability1Name: "Ability",
            Ability2: 0,
            Ability2Name: "-",
            HeldItem1: 0,
            HeldItem1Name: "-",
            HeldItem2: 0,
            HeldItem2Name: "-",
            CatchRate: 45,
            Hp: 45,
            Attack: 45,
            Defense: 45,
            SpecialAttack: 45,
            SpecialDefense: 45,
            Speed: 45,
            HpYield: 0,
            AttackYield: 0,
            DefenseYield: 0,
            SpecialAttackYield: 0,
            SpecialDefenseYield: 0,
            SpeedYield: 0,
            LearnableTms: [],
            LevelUpMoves: [],
            Evolutions: evolvesInto
                .Select((target, evolutionIndex) => new ColosseumPokemonEvolution(evolutionIndex, 4, "Level Up", 16, "Lv. 16", target, $"Pokemon {target}"))
                .ToArray());
}
