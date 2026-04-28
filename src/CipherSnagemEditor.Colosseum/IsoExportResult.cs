namespace CipherSnagemEditor.Colosseum;

public sealed record IsoExportResult(
    string FilePath,
    IReadOnlyList<string> ExtractedFiles,
    IReadOnlyList<string> DecodedFiles);
