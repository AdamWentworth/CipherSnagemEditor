using System.Text;
using System.Text.Json;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.Colosseum;

public enum ColosseumRawTableSource
{
    CommonRel,
    StartDol
}

public sealed record ColosseumRawTableDefinition(
    string Name,
    string Category,
    ColosseumRawTableSource Source,
    string FileName,
    int? CommonIndex,
    int? CountIndex,
    int? StartOffset,
    int? Count,
    int? EntryLength);

public sealed record ColosseumRawTableActionResult(string FilePath, int RowCount, string Message);

public sealed partial class ColosseumProjectContext
{
    private static readonly JsonSerializerOptions TableJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ColosseumRawTableActionResult EncodeRawTable(ColosseumRawTableDefinition definition)
    {
        var table = ResolveRawTable(definition);
        var schema = ColosseumRawTableSchema.For(definition, table.EntryLength);
        var encoded = new RawTableJson(
            definition.Name,
            definition.Category,
            table.Source.ToString(),
            table.FileName,
            table.StartOffset,
            table.Count,
            table.EntryLength,
            schema?.Fields.Select(field => new RawTableJsonField(field.Name, field.Offset, field.ByteLength, field.Kind.ToString())).ToArray(),
            Enumerable.Range(0, table.Count)
                .Select(index =>
                {
                    var offset = checked(table.StartOffset + (index * table.EntryLength));
                    var bytes = table.Bytes.AsSpan(offset, table.EntryLength).ToArray();
                    return new RawTableJsonRow(
                        index,
                        offset,
                        schema?.ReadFields(bytes),
                        Convert.ToHexString(bytes));
                })
                .ToArray());

        var path = RawTableJsonPath(definition);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(encoded, TableJsonOptions));
        var format = schema is null ? "raw byte JSON" : $"{schema.Fields.Count} named fields plus raw bytes";
        return new ColosseumRawTableActionResult(path, encoded.Rows.Count, $"Encoded {encoded.Rows.Count} rows for {definition.Name} as {format}.");
    }

    public ColosseumRawTableActionResult DocumentRawTable(ColosseumRawTableDefinition definition)
    {
        var table = ResolveRawTable(definition);
        var schema = ColosseumRawTableSchema.For(definition, table.EntryLength);
        var path = RawTableDocumentPath(definition);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        var builder = new StringBuilder();
        builder.AppendLine(definition.Name);
        builder.AppendLine($"Category: {definition.Category}");
        builder.AppendLine($"File: {table.FileName}");
        builder.AppendLine($"Start Offset: 0x{table.StartOffset:X}");
        builder.AppendLine($"Rows: {table.Count}");
        builder.AppendLine($"Entry Length: 0x{table.EntryLength:X} ({table.EntryLength})");
        builder.AppendLine(schema is null ? "Schema: raw bytes" : $"Schema: {schema.Fields.Count} named fields");
        builder.AppendLine();

        for (var index = 0; index < table.Count; index++)
        {
            var offset = checked(table.StartOffset + (index * table.EntryLength));
            var bytes = table.Bytes.AsSpan(offset, table.EntryLength).ToArray();
            builder.AppendLine($"[{index:D4}] 0x{offset:X}");
            if (schema is not null)
            {
                foreach (var (name, value) in schema.ReadFields(bytes))
                {
                    builder.AppendLine($"{name}: {value}");
                }

                builder.AppendLine("Bytes:");
            }

            builder.AppendLine(FormatHex(bytes));
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString());
        return new ColosseumRawTableActionResult(path, table.Count, $"Documented {table.Count} rows for {definition.Name}.");
    }

    public ColosseumRawTableActionResult DecodeRawTable(ColosseumRawTableDefinition definition)
    {
        var table = ResolveRawTable(definition);
        var schema = ColosseumRawTableSchema.For(definition, table.EntryLength);
        var path = RawTableJsonPath(definition);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Encode this table before decoding edited rows.", path);
        }

        var decoded = JsonSerializer.Deserialize<RawTableJson>(File.ReadAllText(path), TableJsonOptions)
            ?? throw new InvalidDataException($"Could not read table JSON: {path}");
        if (!string.Equals(decoded.Table, definition.Name, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Table JSON is for {decoded.Table}, not {definition.Name}.");
        }

        var bytes = table.Bytes.ToArray();
        foreach (var row in decoded.Rows)
        {
            if (row.Index < 0 || row.Index >= table.Count)
            {
                throw new InvalidDataException($"Row index {row.Index} is outside {definition.Name}.");
            }

            var rowBytes = ParseHex(row.Bytes);
            if (rowBytes.Length != table.EntryLength)
            {
                throw new InvalidDataException($"Row {row.Index} has {rowBytes.Length} bytes; expected {table.EntryLength}.");
            }

            schema?.ApplyFields(rowBytes, row.Fields);

            var offset = checked(table.StartOffset + (row.Index * table.EntryLength));
            rowBytes.CopyTo(bytes.AsSpan(offset, table.EntryLength));
        }

        var writtenPath = WriteRawTableBytes(table.Source, bytes);
        var format = schema is null ? "raw bytes" : "named fields";
        return new ColosseumRawTableActionResult(writtenPath, decoded.Rows.Count, $"Decoded {decoded.Rows.Count} rows for {definition.Name} from {format}.");
    }

    private ResolvedRawTable ResolveRawTable(ColosseumRawTableDefinition definition)
    {
        if (Iso is null)
        {
            throw new InvalidOperationException("Table editing requires an opened Colosseum ISO.");
        }

        return definition.Source switch
        {
            ColosseumRawTableSource.CommonRel => ResolveCommonRelRawTable(definition),
            ColosseumRawTableSource.StartDol => ResolveStartDolRawTable(definition),
            _ => throw new NotSupportedException($"{definition.Source} tables are not supported yet.")
        };
    }

    private ResolvedRawTable ResolveCommonRelRawTable(ColosseumRawTableDefinition definition)
    {
        if (definition.CommonIndex is null || definition.CountIndex is null)
        {
            throw new InvalidDataException($"{definition.Name} is missing common.rel relocation indexes.");
        }

        var commonRel = LoadCommonRel();
        var startOffset = commonRel.RelocationTable.GetPointer(definition.CommonIndex.Value);
        var count = commonRel.RelocationTable.GetValueAtPointer(definition.CountIndex.Value);
        var symbolLength = commonRel.RelocationTable.GetSymbolLength(definition.CommonIndex.Value);
        var entryLength = EntryLengthFor(definition, symbolLength, count);
        var bytes = commonRel.ToArray();
        ValidateTableRange(bytes, definition.Name, startOffset, count, entryLength);
        return new ResolvedRawTable(ColosseumRawTableSource.CommonRel, "common.rel", bytes, startOffset, count, entryLength);
    }

    private ResolvedRawTable ResolveStartDolRawTable(ColosseumRawTableDefinition definition)
    {
        if (definition.StartOffset is null || definition.Count is null || definition.EntryLength is null)
        {
            throw new NotSupportedException($"{definition.Name} needs a Start.dol offset before it can be encoded.");
        }

        var bytes = LoadStartDol();
        ValidateTableRange(bytes, definition.Name, definition.StartOffset.Value, definition.Count.Value, definition.EntryLength.Value);
        return new ResolvedRawTable(
            ColosseumRawTableSource.StartDol,
            "Start.dol",
            bytes,
            definition.StartOffset.Value,
            definition.Count.Value,
            definition.EntryLength.Value);
    }

    private static int EntryLengthFor(ColosseumRawTableDefinition definition, int symbolLength, int count)
    {
        if (count > 0 && symbolLength > 0 && symbolLength % count == 0)
        {
            return symbolLength / count;
        }

        return definition.EntryLength
            ?? throw new InvalidDataException($"{definition.Name} does not expose a fixed entry length.");
    }

    private string WriteRawTableBytes(ColosseumRawTableSource source, byte[] bytes)
    {
        switch (source)
        {
            case ColosseumRawTableSource.CommonRel:
            {
                var path = Path.Combine(GetIsoExportDirectory("common.fsys"), "common.rel");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? WorkspaceDirectory!);
                File.WriteAllBytes(path, bytes);
                LoadedFiles["common.rel"] = bytes;
                CommonRel = null;
                TrainerModelNames = null;
                return path;
            }

            case ColosseumRawTableSource.StartDol:
            {
                var path = ResolveIsoExtractPath("Start.dol", null);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? WorkspaceDirectory!);
                File.WriteAllBytes(path, bytes);
                LoadedFiles["Start.dol"] = bytes;
                CommonRel = null;
                TrainerModelNames = null;
                return path;
            }

            default:
                throw new NotSupportedException($"{source} tables are not supported yet.");
        }
    }

    private static void ValidateTableRange(byte[] bytes, string tableName, int startOffset, int count, int entryLength)
    {
        if (startOffset < 0 || count < 0 || entryLength <= 0)
        {
            throw new InvalidDataException($"{tableName} has invalid table metadata.");
        }

        var length = checked(count * entryLength);
        if (startOffset + length > bytes.Length)
        {
            throw new InvalidDataException($"{tableName} extends past the end of its source file.");
        }
    }

    private string RawTableJsonPath(ColosseumRawTableDefinition definition)
        => Path.Combine(RawTableDirectory(), SafeTableFileName(definition.Name) + ".json");

    private string RawTableDocumentPath(ColosseumRawTableDefinition definition)
        => Path.Combine(RawTableDirectory(), SafeTableFileName(definition.Name) + ".txt");

    private string RawTableDirectory()
        => Path.Combine(WorkspaceDirectory ?? Path.GetDirectoryName(SourcePath) ?? ".", "Tables");

    private static string SafeTableFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static string FormatHex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % 16 == 0 ? Environment.NewLine : ' ');
            }

            builder.Append(bytes[i].ToString("X2"));
        }

        return builder.ToString();
    }

    private static byte[] ParseHex(string hex)
    {
        var compact = new string(hex.Where(Uri.IsHexDigit).ToArray());
        if (compact.Length % 2 != 0)
        {
            throw new InvalidDataException("Hex byte data has an odd number of digits.");
        }

        return Convert.FromHexString(compact);
    }

    private sealed record ResolvedRawTable(
        ColosseumRawTableSource Source,
        string FileName,
        byte[] Bytes,
        int StartOffset,
        int Count,
        int EntryLength);

    private sealed record RawTableJson(
        string Table,
        string Category,
        string Source,
        string File,
        int StartOffset,
        int Count,
        int EntryLength,
        IReadOnlyList<RawTableJsonField>? Fields,
        IReadOnlyList<RawTableJsonRow> Rows);

    private sealed record RawTableJsonField(string Name, int Offset, int Length, string Kind);

    private sealed record RawTableJsonRow(int Index, int Offset, Dictionary<string, string>? Fields, string Bytes);
}
