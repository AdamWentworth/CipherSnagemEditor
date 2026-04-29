namespace CipherSnagemEditor.Colosseum;

public sealed record IsoExportResult(
    string FilePath,
    IReadOnlyList<string> ExtractedFiles,
    IReadOnlyList<string> DecodedFiles);

public sealed record IsoEncodeResult(
    string FilePath,
    IReadOnlyList<string> EncodedFiles,
    IReadOnlyList<string> PackedFiles);

public sealed record IsoImportResult(
    string FilePath,
    int WrittenBytes,
    uint MaximumBytes,
    IsoEncodeResult? EncodeResult);

public sealed record IsoDeleteResult(
    string FileName,
    int WrittenBytes,
    string? BackupPath);
