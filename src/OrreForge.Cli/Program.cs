using OrreForge.Colosseum;

if (args.Length < 2 || !args[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("OrreForge CLI");
    Console.WriteLine("Usage: orreforge inspect <.iso|.fsys|.msg|.gtx|.atx>");
    return 1;
}

try
{
    var context = ColosseumProjectContext.Open(args[1]);
    Console.WriteLine($"Source: {context.SourcePath}");
    Console.WriteLine($"Kind: {context.SourceKind}");

    if (context.Iso is not null)
    {
        Console.WriteLine($"Game ID: {context.Iso.GameId}");
        Console.WriteLine($"Region: {context.Iso.Region}");
        Console.WriteLine($"Workspace: {context.WorkspaceDirectory}");
        Console.WriteLine($"FST files: {context.Iso.Files.Count}");
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

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
