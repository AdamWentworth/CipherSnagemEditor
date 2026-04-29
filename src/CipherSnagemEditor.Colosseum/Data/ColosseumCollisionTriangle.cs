namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumCollisionTriangle(
    ColosseumCollisionVertex A,
    ColosseumCollisionVertex B,
    ColosseumCollisionVertex C,
    int Type,
    bool IsInteractable,
    int InteractionIndex,
    int SectionIndex);
