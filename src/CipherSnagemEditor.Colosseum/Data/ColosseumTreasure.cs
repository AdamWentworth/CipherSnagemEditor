namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumTreasure(
    int Index,
    int StartOffset,
    int ModelId,
    string ModelName,
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

public sealed record ColosseumTreasureUpdate(
    int Index,
    int ModelId,
    int Quantity,
    int Angle,
    int RoomId,
    int ItemId,
    float X,
    float Y,
    float Z);
