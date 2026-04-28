namespace OrreForge.Colosseum.Data;

public sealed record ColosseumBattle(
    int Index,
    int BattleType,
    string BattleTypeName,
    int BattleStyle,
    string BattleStyleName,
    int BgmId,
    IReadOnlyList<int> TrainerIds)
{
    public string BattleTypeLabel => $"{BattleTypeName} ({BattleType})";

    public string BattleStyleLabel => $"{BattleStyleName} ({BattleStyle})";

    public string BgmHex => $"0x{BgmId:x}";
}
