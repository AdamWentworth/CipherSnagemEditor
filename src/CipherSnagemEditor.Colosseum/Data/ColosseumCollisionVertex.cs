namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumCollisionVertex(
    float X,
    float Y,
    float Z,
    float NormalX,
    float NormalY,
    float NormalZ,
    int Type,
    int Index,
    bool IsInteractable,
    int InteractionIndex,
    int SectionIndex,
    int SectionIndex2);
