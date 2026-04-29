namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumDatVertexColor(
    int ModelOffset,
    int FileOffset,
    ColosseumDatVertexColorFormat Format,
    int Stride,
    int Red,
    int Green,
    int Blue,
    int Alpha,
    int Extra);
