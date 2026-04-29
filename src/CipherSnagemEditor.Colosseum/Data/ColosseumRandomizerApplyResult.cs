namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumRandomizerApplyResult(
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Messages);
