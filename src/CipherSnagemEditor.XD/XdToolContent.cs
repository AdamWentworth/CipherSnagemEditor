namespace CipherSnagemEditor.XD;

public sealed record XdToolContent(
    string Title,
    string Summary,
    IReadOnlyList<XdToolSection> Sections)
{
    public static XdToolContent Empty { get; } = new(
        "GoD Tool",
        "Open a Pokemon XD ISO to load editor content.",
        []);
}

public sealed record XdToolSection(string Title, IReadOnlyList<XdToolRow> Rows);

public sealed record XdToolRow(string Name, string Value, string Detail);
