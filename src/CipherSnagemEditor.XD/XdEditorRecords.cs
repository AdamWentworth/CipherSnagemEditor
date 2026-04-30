namespace CipherSnagemEditor.XD;

public sealed record XdTrainerRecord(
    int Index,
    string DeckName,
    string Name,
    string ClassName,
    int ClassId,
    int ModelId,
    string ModelName,
    int Ai,
    int NameId,
    int TrainerStringId,
    string TrainerString,
    int FirstPokemonIndex,
    int PreBattleTextId,
    int VictoryTextId,
    int DefeatTextId,
    int CameraEffects,
    string Location,
    bool HasShadow,
    IReadOnlyList<XdTrainerPokemonRecord> Pokemon,
    XdBattleRecord? Battle);

public sealed record XdBattleRecord(
    int Index,
    int BattleType,
    string BattleTypeName,
    int BattleStyle,
    string BattleStyleName,
    int BgmId,
    IReadOnlyList<XdBattlePlayerRecord> Players);

public sealed record XdBattlePlayerRecord(
    int DeckId,
    int TrainerId,
    int Controller);

public sealed record XdTrainerPokemonRecord(
    int Slot,
    int DeckIndex,
    int SpeciesId,
    string SpeciesName,
    int Level,
    int ShadowId,
    int ItemId,
    string ItemName,
    int Ability,
    int Nature,
    int Gender,
    int Happiness,
    int Iv,
    IReadOnlyList<int> Evs,
    IReadOnlyList<int> MoveIds,
    IReadOnlyList<string> MoveNames,
    XdShadowPokemonRecord? ShadowData);

public sealed record XdShadowPokemonRecord(
    int Index,
    int StoryPokemonIndex,
    int SpeciesId,
    string SpeciesName,
    int Level,
    int CatchRate,
    int HeartGauge,
    int InUseFlag,
    int FleeValue,
    int Aggression,
    int AlwaysFlee,
    IReadOnlyList<int> MoveIds,
    IReadOnlyList<string> MoveNames,
    int ShadowBoostLevel,
    int ItemId,
    string ItemName,
    int Ability,
    string AbilityName,
    int Nature,
    int Gender,
    int Happiness,
    int Iv,
    IReadOnlyList<int> Evs,
    IReadOnlyList<int> RegularMoveIds,
    IReadOnlyList<string> RegularMoveNames);

public sealed record XdPokemonStatsRecord(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    int NationalIndex,
    int ExpRate,
    int GenderRatio,
    int BaseExp,
    int BaseHappiness,
    double Height,
    double Weight,
    int Type1,
    string Type1Name,
    int Type2,
    string Type2Name,
    int Ability1,
    string Ability1Name,
    int Ability2,
    string Ability2Name,
    int HeldItem1,
    string HeldItem1Name,
    int HeldItem2,
    string HeldItem2Name,
    int CatchRate,
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed,
    int HpYield,
    int AttackYield,
    int DefenseYield,
    int SpecialAttackYield,
    int SpecialDefenseYield,
    int SpeedYield,
    IReadOnlyList<bool> LearnableTms,
    IReadOnlyList<XdLevelUpMoveRecord> LevelUpMoves,
    IReadOnlyList<XdEvolutionRecord> Evolutions);

public sealed record XdLevelUpMoveRecord(int Level, int MoveId, string MoveName);

public sealed record XdEvolutionRecord(int Method, int Condition, int EvolvedSpeciesId, string EvolvedSpeciesName);

public sealed record XdMoveRecord(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    string Description,
    int DescriptionId,
    int TypeId,
    string TypeName,
    int TargetId,
    int CategoryId,
    int AnimationId,
    int Animation2Id,
    int EffectId,
    int EffectTypeId,
    int Power,
    int Accuracy,
    int Pp,
    int Priority,
    int EffectAccuracy,
    bool HmFlag,
    bool SoundBasedFlag,
    bool ContactFlag,
    bool KingsRockFlag,
    bool ProtectFlag,
    bool SnatchFlag,
    bool MagicCoatFlag,
    bool MirrorMoveFlag,
    bool IsShadow);

public sealed record XdTmMoveRecord(
    int Index,
    int MoveId,
    string MoveName,
    int TypeId,
    string TypeName);

public sealed record XdItemRecord(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    string Description,
    int DescriptionId,
    int BagSlotId,
    bool CanBeHeld,
    int Price,
    int CouponPrice,
    int Parameter,
    int HoldItemId,
    int InBattleUseId,
    IReadOnlyList<int> FriendshipEffects);

public sealed record XdTypeRecord(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    int CategoryId,
    IReadOnlyList<int> Effectiveness);

public sealed record XdTreasureRecord(
    int Index,
    int StartOffset,
    int ModelId,
    int Quantity,
    int Angle,
    int RoomId,
    string RoomName,
    int Flag,
    int ItemId,
    string ItemName,
    float X,
    float Y,
    float Z);

public sealed record XdPokespotRecord(
    int Index,
    string SpotName,
    int SpeciesId,
    string SpeciesName,
    int MinLevel,
    int MaxLevel,
    int EncounterPercentage,
    int StepsPerSnack,
    int StartOffset);

public sealed record XdShadowPokemonUpdate(
    int Index,
    int StoryPokemonIndex,
    int SpeciesId,
    int Level,
    int CatchRate,
    int HeartGauge,
    int InUseFlag,
    int FleeValue,
    int Aggression,
    int AlwaysFlee,
    IReadOnlyList<int> ShadowMoveIds,
    int ShadowBoostLevel,
    int ItemId,
    int Ability,
    int Nature,
    int Gender,
    int Happiness,
    int Iv,
    IReadOnlyList<int> Evs,
    IReadOnlyList<int> RegularMoveIds);

public sealed record XdPokespotUpdate(
    int StartOffset,
    int SpeciesId,
    int MinLevel,
    int MaxLevel,
    int EncounterPercentage,
    int StepsPerSnack);

public sealed record XdGiftPokemonRecord(
    int RowId,
    int DataIndex,
    int StartOffset,
    string GiftType,
    int SpeciesId,
    string SpeciesName,
    int Level,
    IReadOnlyList<int> MoveIds,
    IReadOnlyList<string> MoveNames,
    bool UsesLevelUpMoves);

public sealed record XdGiftPokemonUpdate(
    int RowId,
    int SpeciesId,
    int Level,
    IReadOnlyList<int> MoveIds);

public sealed record XdMessageTable(
    string DisplayName,
    string IsoFileName,
    string EntryName,
    IReadOnlyList<XdMessageString> Strings);

public sealed record XdMessageString(
    int Id,
    string IdHex,
    string Text);

public sealed record XdPatchApplyResult(
    XdPatchDefinition Patch,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Messages);

public sealed record XdRandomizerOptions(
    bool StarterPokemon,
    bool ObtainablePokemon,
    bool UnobtainablePokemon,
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
    bool BattleBingo,
    bool ShinyHues,
    bool SimilarBaseStatTotal,
    bool RemoveItemOrTradeEvolutions);

public sealed record XdRandomizerApplyResult(
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Messages);

public sealed record XdPokemonStatsUpdate(
    int Index,
    int NameId,
    int ExpRate,
    int GenderRatio,
    int BaseExp,
    int BaseHappiness,
    double Height,
    double Weight,
    int Type1,
    int Type2,
    int Ability1,
    int Ability2,
    int HeldItem1,
    int HeldItem2,
    int CatchRate,
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed,
    int HpYield,
    int AttackYield,
    int DefenseYield,
    int SpecialAttackYield,
    int SpecialDefenseYield,
    int SpeedYield,
    IReadOnlyList<bool> LearnableTms,
    IReadOnlyList<XdPokemonLevelUpMoveUpdate> LevelUpMoves,
    IReadOnlyList<XdPokemonEvolutionUpdate> Evolutions);

public sealed record XdPokemonLevelUpMoveUpdate(int Level, int MoveId);

public sealed record XdPokemonEvolutionUpdate(int Method, int Condition, int EvolvedSpeciesId);

public sealed record XdMoveUpdate(
    int Index,
    int NameId,
    int DescriptionId,
    int TypeId,
    int TargetId,
    int CategoryId,
    int AnimationId,
    int Animation2Id,
    int EffectId,
    int EffectTypeId,
    int Power,
    int Accuracy,
    int Pp,
    int Priority,
    int EffectAccuracy,
    bool HmFlag,
    bool SoundBasedFlag,
    bool ContactFlag,
    bool KingsRockFlag,
    bool ProtectFlag,
    bool SnatchFlag,
    bool MagicCoatFlag,
    bool MirrorMoveFlag);

public sealed record XdItemUpdate(
    int Index,
    int NameId,
    int DescriptionId,
    int BagSlotId,
    bool CanBeHeld,
    int Price,
    int CouponPrice,
    int Parameter,
    int HoldItemId,
    int InBattleUseId,
    IReadOnlyList<int> FriendshipEffects);

public sealed record XdTypeUpdate(
    int Index,
    int NameId,
    int CategoryId,
    IReadOnlyList<int> Effectiveness);

public sealed record XdTreasureUpdate(
    int Index,
    int ModelId,
    int Quantity,
    int Angle,
    int RoomId,
    int ItemId,
    float X,
    float Y,
    float Z);
