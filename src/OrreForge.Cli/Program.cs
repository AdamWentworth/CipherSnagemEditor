using OrreForge.Colosseum;

if (args.Length == 0)
{
    Console.WriteLine("OrreForge CLI");
    Console.WriteLine("Usage: orreforge inspect <.iso|.fsys|.msg|.gtx|.atx>");
    Console.WriteLine("       orreforge extract-iso <iso> <file-name> [output-path]");
    return 1;
}

try
{
    if (args[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: orreforge inspect <.iso|.fsys|.msg|.gtx|.atx>");
            return 1;
        }

        Inspect(args[1]);
        return 0;
    }

    if (args[0].Equals("extract-iso", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: orreforge extract-iso <iso> <file-name> [output-path]");
            return 1;
        }

        ExtractIsoFile(args[1], args[2], args.Length > 3 ? args[3] : null);
        return 0;
    }

    Console.Error.WriteLine($"Unknown command: {args[0]}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static void Inspect(string path)
{
    var context = ColosseumProjectContext.Open(path);
    Console.WriteLine($"Source: {context.SourcePath}");
    Console.WriteLine($"Kind: {context.SourceKind}");

    if (context.Iso is not null)
    {
        Console.WriteLine($"Game ID: {context.Iso.GameId}");
        Console.WriteLine($"Region: {context.Iso.Region}");
        Console.WriteLine($"Workspace: {context.WorkspaceDirectory}");
        Console.WriteLine($"FST files: {context.Iso.Files.Count}");
        foreach (var entry in context.Iso.Files.Take(20))
        {
            Console.WriteLine($"0x{entry.Offset:x8} {entry.Size,10} {entry.Name}");
        }
    }
 
    if (context.FsysArchive is not null)
    {
        Console.WriteLine($"FSYS entries: {context.FsysArchive.Entries.Count}");
        foreach (var entry in context.FsysArchive.Entries.Take(20))
        {
            Console.WriteLine($"{entry.Index,3} 0x{entry.StartOffset:x8} {entry.CompressedSize,8} {entry.Name}");
        }
    }

    if (context.MessageTable is not null)
    {
        Console.WriteLine($"Messages: {context.MessageTable.Strings.Count}");
        foreach (var message in context.MessageTable.Strings.Take(10))
        {
            Console.WriteLine($"{message.Id}: {message.Text}");
        }
    }
}

static void ExtractIsoFile(string isoPath, string fileName, string? outputPath)
{
    var context = ColosseumProjectContext.Open(isoPath);
    if (context.Iso is null)
    {
        throw new InvalidOperationException("Input path did not load as an ISO.");
    }

    var entry = context.Iso.Files.FirstOrDefault(file => file.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    if (entry is null)
    {
        throw new FileNotFoundException($"Could not find {fileName} in ISO.");
    }

    var extractedPath = context.ExtractIsoFile(entry, outputPath);
    Console.WriteLine(extractedPath);
}
