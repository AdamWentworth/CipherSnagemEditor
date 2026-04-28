namespace OrreForge.Colosseum.Data;

public sealed record ColosseumMove(
    int Index,
    string Name,
    int TypeId,
    string TypeName,
    int Power,
    int Accuracy,
    int Pp);
