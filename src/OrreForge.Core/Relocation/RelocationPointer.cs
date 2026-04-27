namespace OrreForge.Core.Relocation;

public sealed record RelocationPointer(
    int Index,
    int Section,
    IReadOnlyList<int> Addresses,
    int DataOffset);
