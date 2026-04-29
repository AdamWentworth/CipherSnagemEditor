using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Colosseum.Data;

if (args.Length == 0)
{
    Console.WriteLine("Cipher Snagem Editor CLI");
    Console.WriteLine("Usage: ciphersnagem inspect <.iso|.fsys|.msg|.gtx|.atx>");
    Console.WriteLine("       ciphersnagem trainers <iso>");
    Console.WriteLine("       ciphersnagem extract-iso <iso> <file-name> [output-path]");
    Console.WriteLine("       ciphersnagem smoke-apply <iso> <operation>");
    return 1;
}

try
{
    if (args[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem inspect <.iso|.fsys|.msg|.gtx|.atx>");
            return 1;
        }

        Inspect(args[1]);
        return 0;
    }

    if (args[0].Equals("extract-iso", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: ciphersnagem extract-iso <iso> <file-name> [output-path]");
            return 1;
        }

        ExtractIsoFile(args[1], args[2], args.Length > 3 ? args[3] : null);
        return 0;
    }

    if (args[0].Equals("trainers", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem trainers <iso>");
            return 1;
        }

        PrintTrainers(args[1]);
        return 0;
    }

    if (args[0].Equals("smoke-apply", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: ciphersnagem smoke-apply <iso> <patch:Kind|editor-move|randomizer-species|randomizer-shops>");
            return 1;
        }

        ApplySmokeOperation(args[1], args[2]);
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

static void PrintTrainers(string isoPath)
{
    var context = ColosseumProjectContext.Open(isoPath);
    var trainers = context.LoadStoryTrainers();
    Console.WriteLine($"Trainers: {trainers.Count}");
    foreach (var trainer in trainers.Take(20))
    {
        var firstPokemon = trainer.Pokemon.FirstOrDefault(pokemon => pokemon.IsSet);
        var firstMove = firstPokemon?.Moves.FirstOrDefault(move => move.Index > 0);
        var battle = trainer.Battle is null
            ? "battle -"
            : $"battle {trainer.Battle.Index}: {trainer.Battle.BattleStyleLabel}, {trainer.Battle.BattleTypeLabel}, bgm {trainer.Battle.BgmHex}";
        Console.WriteLine(
            $"{trainer.Index,3}: {trainer.FullName} | model {trainer.TrainerModelId}: {trainer.TrainerModelName} | {battle} | pokemon {trainer.FirstPokemonIndex}"
            + (firstPokemon is null
                ? string.Empty
                : $" | first: {firstPokemon.SpeciesName} Lv.{firstPokemon.Level}, {firstPokemon.ItemName}, {firstMove?.Name ?? "-"}"));
    }
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

static void ApplySmokeOperation(string isoPath, string operation)
{
    var context = ColosseumProjectContext.Open(isoPath);
    if (context.Iso is null)
    {
        throw new InvalidOperationException("Input path did not load as an ISO.");
    }

    var writtenFiles = new List<string>();
    if (operation.StartsWith("patch:", StringComparison.OrdinalIgnoreCase))
    {
        var patchName = operation["patch:".Length..];
        var patchKind = ParsePatchKind(patchName);
        var result = context.ApplyPatch(patchKind);
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine($"Applied patch: {result.Patch.Kind}");
        Console.WriteLine(result.Patch.Name);
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("editor-move", StringComparison.OrdinalIgnoreCase))
    {
        var move = context.LoadMoves()
            .FirstOrDefault(candidate => candidate.Name.Equals("ICE PUNCH", StringComparison.OrdinalIgnoreCase))
            ?? context.LoadMoves().First(candidate => !candidate.IsShadow && candidate.Pp > 0);
        var replacementPp = move.Pp == 16 ? 15 : 16;
        writtenFiles.Add(context.SaveMove(new ColosseumMoveUpdate(
            move.Index,
            move.NameId,
            move.DescriptionId,
            move.TypeId,
            move.TargetId,
            move.CategoryId,
            move.AnimationId,
            move.Animation2Id,
            move.EffectId,
            move.EffectTypeId,
            move.Power,
            move.Accuracy,
            replacementPp,
            move.Priority,
            move.EffectAccuracy,
            move.HmFlag,
            move.SoundBasedFlag,
            move.ContactFlag,
            move.KingsRockFlag,
            move.ProtectFlag,
            move.SnatchFlag,
            move.MagicCoatFlag,
            move.MirrorMoveFlag)));
        Console.WriteLine($"Edited move: {move.Name} PP {move.Pp} -> {replacementPp}");
    }
    else if (operation.Equals("randomizer-species", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new ColosseumRandomizerOptions(
            StarterPokemon: true,
            ShadowPokemon: false,
            NpcPokemon: true,
            PokemonMoves: false,
            PokemonTypes: false,
            PokemonAbilities: false,
            PokemonStats: false,
            PokemonEvolutions: false,
            MoveTypes: false,
            TypeMatchups: false,
            TmMoves: false,
            ItemBoxes: false,
            ShopItems: false,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied randomizer: species smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("randomizer-shops", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new ColosseumRandomizerOptions(
            StarterPokemon: false,
            ShadowPokemon: false,
            NpcPokemon: false,
            PokemonMoves: false,
            PokemonTypes: false,
            PokemonAbilities: false,
            PokemonStats: false,
            PokemonEvolutions: false,
            MoveTypes: false,
            TypeMatchups: false,
            TmMoves: false,
            ItemBoxes: false,
            ShopItems: true,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied randomizer: shops smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else
    {
        throw new InvalidOperationException($"Unknown smoke operation: {operation}");
    }

    if (writtenFiles.Count == 0)
    {
        Console.WriteLine("No workspace files were written.");
        return;
    }

    ImportWrittenFiles(context, writtenFiles);
}

static ColosseumPatchKind ParsePatchKind(string patchName)
{
    if (Enum.TryParse<ColosseumPatchKind>(patchName, ignoreCase: true, out var parsed))
    {
        return parsed;
    }

    var normalized = patchName.Replace("-", string.Empty, StringComparison.Ordinal)
        .Replace("_", string.Empty, StringComparison.Ordinal)
        .Replace(" ", string.Empty, StringComparison.Ordinal);
    foreach (var value in Enum.GetValues<ColosseumPatchKind>())
    {
        if (value.ToString().Equals(normalized, StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }
    }

    throw new InvalidOperationException($"Unknown patch kind: {patchName}");
}

static void ImportWrittenFiles(ColosseumProjectContext context, IReadOnlyList<string> writtenFiles)
{
    var fullPaths = writtenFiles.Select(Path.GetFullPath).ToArray();
    foreach (var path in fullPaths)
    {
        Console.WriteLine($"Workspace write: {path}");
    }

    if (fullPaths.Any(IsStartDolPath))
    {
        ImportEntry(context, "Start.dol", encode: false);
    }

    if (fullPaths.Any(path => IsNamedPath(path, "common.rel") || ContainsDirectory(path, "common")))
    {
        ImportEntry(context, "common.fsys", encode: true);
    }

    if (fullPaths.Any(path => IsNamedPath(path, "pocket_menu.rel") || ContainsDirectory(path, "pocket_menu")))
    {
        ImportEntry(context, "pocket_menu.fsys", encode: true);
    }
}

static void ImportEntry(ColosseumProjectContext context, string fileName, bool encode)
{
    if (context.Iso is null)
    {
        throw new InvalidOperationException("No ISO is loaded.");
    }

    var entry = context.Iso.Files.FirstOrDefault(file =>
        file.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    if (entry is null)
    {
        throw new FileNotFoundException($"Could not find {fileName} in ISO.");
    }

    var result = context.ImportIsoFile(entry, encode);
    Console.WriteLine(
        $"Imported {fileName}: {result.WrittenBytes} bytes, max {result.MaximumBytes}, inserted {result.InsertedBytes}.");
}

static bool IsStartDolPath(string path)
    => IsNamedPath(path, "Start.dol");

static bool IsNamedPath(string path, string fileName)
    => Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase);

static bool ContainsDirectory(string path, string directoryName)
    => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(part => part.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
