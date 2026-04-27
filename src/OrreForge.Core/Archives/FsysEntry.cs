using OrreForge.Core.Files;

namespace OrreForge.Core.Archives;

public sealed record FsysEntry(
    int Index,
    string Name,
    uint Identifier,
    GameFileType FileType,
    uint StartOffset,
    uint CompressedSize,
    uint UncompressedSize,
    bool IsCompressed);
