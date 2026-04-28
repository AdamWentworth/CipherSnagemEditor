namespace OrreForge.Colosseum.Data;

public sealed record ColosseumTypeData(
    int Index,
    int StartOffset,
    string Name,
    int NameId,
    int CategoryId,
    string CategoryName,
    IReadOnlyList<int> Effectiveness);

public sealed record ColosseumTypeUpdate(
    int Index,
    int NameId,
    int CategoryId,
    IReadOnlyList<int> Effectiveness);
