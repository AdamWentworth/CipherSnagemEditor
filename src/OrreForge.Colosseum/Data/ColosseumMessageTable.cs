namespace OrreForge.Colosseum.Data;

public sealed record ColosseumMessageTable(
    string DisplayName,
    string IsoFileName,
    string EntryName,
    IReadOnlyList<ColosseumMessageString> Strings);

public sealed record ColosseumMessageString(
    int Id,
    string IdHex,
    string Text);
