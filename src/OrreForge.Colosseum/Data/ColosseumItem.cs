namespace OrreForge.Colosseum.Data;

public sealed record ColosseumItem(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    string Description,
    int DescriptionId,
    int BagSlotId,
    string BagSlotName,
    bool CanBeHeld,
    int Price,
    int CouponPrice,
    int Parameter,
    int HoldItemId,
    int InBattleUseId,
    IReadOnlyList<int> FriendshipEffects,
    int TmIndex,
    int TmMoveId,
    string TmMoveName);

public sealed record ColosseumItemUpdate(
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
    IReadOnlyList<int> FriendshipEffects,
    int TmIndex,
    int TmMoveId);
