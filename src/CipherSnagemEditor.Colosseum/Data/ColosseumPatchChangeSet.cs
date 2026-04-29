namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumPatchChangeSet(
    bool StartDolChanged,
    bool CommonRelChanged,
    IReadOnlyList<string> Messages)
{
    public static ColosseumPatchChangeSet Empty { get; } = new(false, false, []);

    public ColosseumPatchChangeSet WithStartDol()
        => this with { StartDolChanged = true };

    public ColosseumPatchChangeSet WithCommonRel()
        => this with { CommonRelChanged = true };

    public ColosseumPatchChangeSet WithMessage(string message)
        => this with { Messages = [.. Messages, message] };
}
