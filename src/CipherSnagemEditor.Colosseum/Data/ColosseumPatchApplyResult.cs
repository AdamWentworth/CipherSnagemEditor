namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumPatchApplyResult(
    ColosseumPatchDefinition Patch,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Messages);
