namespace CipherSnagemEditor.Core.Relocation;

public sealed record RelocationSection(
    int Index,
    int SectionInfoOffset,
    int DataOffset,
    bool IsTextSection,
    int Length);
