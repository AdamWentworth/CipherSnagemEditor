using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Colosseum.Data;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Text;
using CipherSnagemEditor.XD;

if (args.Length == 0)
{
    Console.WriteLine("Cipher Snagem Editor CLI");
    Console.WriteLine("Usage: ciphersnagem inspect <.iso|.fsys|.msg|.gtx|.atx>");
    Console.WriteLine("       ciphersnagem trainers <iso>");
    Console.WriteLine("       ciphersnagem extract-iso <iso> <file-name> [output-path]");
    Console.WriteLine("       ciphersnagem xd-probe <iso>");
    Console.WriteLine("       ciphersnagem xd-editors-probe <iso>");
    Console.WriteLine("       ciphersnagem xd-trainer-probe <iso> <search>");
    Console.WriteLine("       ciphersnagem xd-smoke-apply <iso> <patch:Kind|randomizer-data|randomizer-species|randomizer-bingo|randomizer-shiny-hues>");
    Console.WriteLine("       ciphersnagem smoke-apply <iso> <operation>");
    Console.WriteLine("       ciphersnagem closeout-probe <iso>");
    Console.WriteLine("       ciphersnagem parity-probe <iso> [--messages N] [--assets N]");
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

    if (args[0].Equals("xd-probe", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem xd-probe <iso>");
            return 1;
        }

        RunXdProbe(args[1]);
        return 0;
    }

    if (args[0].Equals("xd-editors-probe", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem xd-editors-probe <iso>");
            return 1;
        }

        RunXdEditorsProbe(args[1]);
        return 0;
    }

    if (args[0].Equals("xd-trainer-probe", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: ciphersnagem xd-trainer-probe <iso> <search>");
            return 1;
        }

        RunXdTrainerProbe(args[1], args[2]);
        return 0;
    }

    if (args[0].Equals("xd-smoke-apply", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: ciphersnagem xd-smoke-apply <iso> <patch:Kind|randomizer-data|randomizer-species|randomizer-bingo|randomizer-shiny-hues>");
            return 1;
        }

        ApplyXdSmokeOperation(args[1], args[2]);
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

    if (args[0].Equals("parity-probe", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem parity-probe <iso> [--messages N] [--assets N]");
            return 1;
        }

        RunParityProbe(
            args[1],
            ReadIntOption(args, "--messages", 50),
            ReadIntOption(args, "--assets", 50));
        return 0;
    }

    if (args[0].Equals("closeout-probe", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: ciphersnagem closeout-probe <iso>");
            return 1;
        }

        RunCloseoutProbe(args[1]);
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
    if (GameFileTypes.FromExtension(path) == GameFileType.Iso)
    {
        InspectIso(path);
        return;
    }

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

static void InspectIso(string path)
{
    var iso = GameCubeIsoReader.Open(path);
    var workspace = iso.IsPokemonXD
        ? XdProjectContext.Open(path).WorkspaceDirectory
        : iso.IsPokemonColosseum
            ? ColosseumProjectContext.Open(path).WorkspaceDirectory
            : iso.WorkspaceDirectory;

    Console.WriteLine($"Source: {path}");
    Console.WriteLine("Kind: Iso");
    Console.WriteLine($"Game ID: {iso.GameId}");
    Console.WriteLine($"Game: {iso.Game}");
    Console.WriteLine($"Legacy tool: {iso.LegacyToolName}");
    Console.WriteLine($"Region: {iso.Region}");
    Console.WriteLine($"Workspace: {workspace}");
    Console.WriteLine($"FST files: {iso.Files.Count}");
    foreach (var entry in iso.Files.Take(20))
    {
        Console.WriteLine($"0x{entry.Offset:x8} {entry.Size,10} {entry.Name}");
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

static void RunXdProbe(string isoPath)
{
    var context = XdProjectContext.Open(isoPath);
    var iso = context.Iso;
    var failures = new List<string>();

    Expect(iso.IsPokemonXD, failures, $"Expected an XD ISO, found {iso.GameId}.");
    Expect(iso.Region != GameCubeRegion.OtherGame, failures, $"Unexpected XD region for {iso.GameId}.");
    Expect(Directory.Exists(context.WorkspaceDirectory), failures, "XD workspace was not created.");
    Expect(File.Exists(Path.Combine(context.WorkspaceDirectory, "Settings.json")), failures, "XD Settings.json was not created.");
    Expect(iso.Files.Any(file => file.Name.Equals("Start.dol", StringComparison.OrdinalIgnoreCase)), failures, "Start.dol is missing from the FST.");
    Expect(iso.Files.Any(file => file.Name.Equals("Game.toc", StringComparison.OrdinalIgnoreCase)), failures, "Game.toc is missing from the FST.");
    Expect(iso.Files.Count > 100, failures, $"FST file count is unexpectedly low: {iso.Files.Count}.");
    Expect(iso.Files.Any(file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase)), failures, "No FSYS archives were found in the XD ISO.");
    foreach (var tool in XdToolCatalog.HomeTools.Where(tool => !tool.Title.Equals("ISO Explorer", StringComparison.OrdinalIgnoreCase)))
    {
        var content = context.BuildToolContent(tool.Title);
        var rowCount = content.Sections.Sum(section => section.Rows.Count);
        Expect(content.Sections.Count > 1, failures, $"{tool.Title} did not build tool content sections.");
        Expect(rowCount > 3, failures, $"{tool.Title} content is unexpectedly sparse: {rowCount} rows.");
        foreach (var requiredSection in RequiredXdContentSections(tool.Title))
        {
            Expect(
                content.Sections.Any(section => section.Title.Equals(requiredSection, StringComparison.OrdinalIgnoreCase)),
                failures,
                $"{tool.Title} is missing required content section: {requiredSection}.");
        }
    }

    Console.WriteLine($"XD ISO: {iso.Path}");
    Console.WriteLine($"Game ID: {iso.GameId}");
    Console.WriteLine($"Region: {iso.Region}");
    Console.WriteLine($"Workspace: {context.WorkspaceDirectory}");
    Console.WriteLine($"FST files: {iso.Files.Count}");
    Console.WriteLine($"FSYS files: {iso.Files.Count(file => file.Name.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase))}");
    foreach (var entry in iso.Files.Take(12))
    {
        Console.WriteLine($"0x{entry.Offset:x8} {entry.Size,10} {entry.Name}");
    }

    foreach (var tool in XdToolCatalog.HomeTools.Where(tool => !tool.Title.Equals("ISO Explorer", StringComparison.OrdinalIgnoreCase)))
    {
        var content = context.BuildToolContent(tool.Title);
        Console.WriteLine($"XD content: {tool.Title}: {content.Sections.Count} sections, {content.Sections.Sum(section => section.Rows.Count)} rows.");
    }

    if (failures.Count == 0)
    {
        Console.WriteLine("XD open probe passed.");
        return;
    }

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"XD probe failure: {failure}");
    }

    throw new InvalidDataException($"XD probe failed with {failures.Count} failure(s).");
}

static IReadOnlyList<string> RequiredXdContentSections(string toolTitle)
    => toolTitle switch
    {
        "Trainer Editor" => ["Trainer Preview"],
        "Shadow Pokemon Editor" => ["Shadow Pokemon Preview"],
        "Pokemon Stats Editor" => ["common.rel Table Map"],
        "Move Editor" => ["common.rel Table Map"],
        "Item Editor" => ["common.rel Table Map"],
        "Pokespot Editor" => ["Pokespot Encounters"],
        "Gift Pokemon Editor" => ["common.rel Table Map"],
        "Type Editor" => ["common.rel Table Map"],
        "Treasure Editor" => ["common.rel Table Map"],
        "Interaction Editor" => ["common.rel Table Map"],
        _ => []
    };

static string Simplify(string value)
{
    var builder = new System.Text.StringBuilder(value.Length);
    foreach (var character in value)
    {
        if (char.IsLetterOrDigit(character))
        {
            builder.Append(char.ToLowerInvariant(character));
        }
    }

    return builder.ToString();
}

static void RunXdEditorsProbe(string isoPath)
{
    var context = XdProjectContext.Open(isoPath);
    var trainers = context.LoadTrainerRecords();
    var shadows = context.LoadShadowPokemonRecords();
    var stats = context.LoadPokemonStatsRecords();
    var moves = context.LoadMoveRecords();
    var tms = context.LoadTmMoveRecords();
    var items = context.LoadItemRecords();
    var pokespots = context.LoadPokespotRecords();
    var gifts = context.LoadGiftPokemonRecords();
    var messages = context.LoadMessageTables();
    var types = context.LoadTypeRecords();
    var treasures = context.LoadTreasureRecords();
    var trainersWithBattleData = trainers.Count(trainer => trainer.Battle is not null);
    var trainersWithResolvedClasses = trainers.Count(trainer => !trainer.ClassName.StartsWith("Class ", StringComparison.Ordinal));
    var firstBattleTrainer = trainers.FirstOrDefault(trainer => trainer.Battle is not null);
    var firstResolvedMoveDescription = moves.FirstOrDefault(move => move.DescriptionId > 0
        && !move.Description.StartsWith("String ", StringComparison.Ordinal)
        && !move.Description.StartsWith('#'));

    Console.WriteLine($"XD editor records: trainers={trainers.Count}, shadows={shadows.Count}, stats={stats.Count}, moves={moves.Count}, tms={tms.Count}, items={items.Count}, pokespots={pokespots.Count}, gifts={gifts.Count}, messages={messages.Count}, types={types.Count}, treasures={treasures.Count}");
    Console.WriteLine($"XD trainer header data: classes={trainersWithResolvedClasses}, battles={trainersWithBattleData}, first battle={firstBattleTrainer?.Index}: {firstBattleTrainer?.ClassName} {firstBattleTrainer?.Name} / {firstBattleTrainer?.Battle?.BattleStyleName} {firstBattleTrainer?.Battle?.BattleTypeName}");
    Console.WriteLine($"First XD TM: {tms.FirstOrDefault()?.Index}: {tms.FirstOrDefault()?.MoveName}");
    Console.WriteLine($"First XD move description: {firstResolvedMoveDescription?.Index}: {firstResolvedMoveDescription?.Description}");
    Console.WriteLine($"First trainer: {trainers.FirstOrDefault()?.Index}: {trainers.FirstOrDefault()?.ClassName} {trainers.FirstOrDefault()?.Name}");
    Console.WriteLine($"First shadow: {shadows.FirstOrDefault()?.Index}: {shadows.FirstOrDefault()?.SpeciesName}");
    Console.WriteLine($"First pokespot: {pokespots.FirstOrDefault()?.SpotName} {pokespots.FirstOrDefault()?.SpeciesName}");
    Console.WriteLine("XD gift roster: " + string.Join(", ", gifts.Select(gift => $"{gift.RowId}:{gift.SpeciesName}/{gift.GiftType}")));

    var failures = new List<string>();
    var expectedGiftTypes = new[]
    {
        "Starter Pokemon",
        "Demo Starter Pokemon",
        "Demo Starter Pokemon",
        "Duking's Plusle",
        "Mt.Battle Ho-oh",
        "Agate Pikachu",
        "Agate Celebi",
        "Shadow Pokemon Gift",
        "Hordel Trade",
        "Duking Trade",
        "Duking Trade",
        "Duking Trade",
        "Mt. Battle Prize",
        "Mt. Battle Prize",
        "Mt. Battle Prize"
    };

    Expect(trainers.Count > 100, failures, $"Trainer parser is unexpectedly sparse: {trainers.Count}.");
    Expect(trainersWithResolvedClasses > 100, failures, $"Trainer class lookup is unexpectedly sparse: {trainersWithResolvedClasses}.");
    Expect(trainersWithBattleData > 0, failures, "Trainer battle lookup returned no battle-linked trainers.");
    Expect(shadows.Count > 40, failures, $"Shadow parser is unexpectedly sparse: {shadows.Count}.");
    Expect(stats.Count > 300, failures, $"Pokemon stats parser is unexpectedly sparse: {stats.Count}.");
    Expect(moves.Count > 350, failures, $"Move parser is unexpectedly sparse: {moves.Count}.");
    Expect(firstResolvedMoveDescription is not null, failures, "Move descriptions did not resolve from the XD string table.");
    Expect(tms.Count == 58, failures, $"TM/HM parser should resolve the Swift GoD Tool's 58 TM/HM rows, but returned {tms.Count}.");
    Expect(tms.Any(tm => !tm.MoveName.StartsWith("Move ", StringComparison.Ordinal) && tm.MoveName != "-"), failures, "TM/HM parser did not resolve TM move names.");
    Expect(items.Count > 300, failures, $"Item parser is unexpectedly sparse: {items.Count}.");
    Expect(pokespots.Count > 0, failures, "Pokespot parser returned no encounters.");
    Expect(gifts.Count == 15, failures, $"Gift Pokemon parser should match the Swift GoD Tool's 15 gift rows, but returned {gifts.Count}.");
    Expect(gifts.Select(gift => gift.GiftType).SequenceEqual(expectedGiftTypes), failures, "XD Gift Pokemon roster drifted from the Swift GoD Tool order.");
    Expect(gifts.Where(gift => gift.RowId is >= 3 and <= 6).All(gift => gift.UsesLevelUpMoves), failures, "XD CMGiftPokemon rows should use level-up moves like the Swift GoD Tool.");
    Expect(messages.Count > 0, failures, "Message Editor did not discover any XD message tables.");
    Expect(messages.Any(table => table.Strings.Count > 0), failures, "Message Editor found XD message tables but no strings.");
    Expect(types.Count >= 18, failures, $"Type parser is unexpectedly sparse: {types.Count}.");
    Expect(treasures.Count > 0, failures, "Treasure parser returned no rows.");

    if (failures.Count == 0)
    {
        Console.WriteLine("XD editor probe passed.");
        return;
    }

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"XD editor probe failure: {failure}");
    }

    throw new InvalidDataException($"XD editor probe failed with {failures.Count} failure(s).");
}

static void RunXdTrainerProbe(string isoPath, string searchText)
{
    var context = XdProjectContext.Open(isoPath);
    var filter = Simplify(searchText);
    var trainers = context.LoadTrainerRecords()
        .Where(trainer => Simplify($"{trainer.Index} {trainer.Name} {trainer.ClassName} {trainer.ModelName} {trainer.Location}")
            .Contains(filter, StringComparison.Ordinal))
        .Take(12)
        .ToArray();

    foreach (var trainer in trainers)
    {
        var battle = trainer.Battle is null
            ? "no battle"
            : $"battle {trainer.Battle.Index}: {trainer.Battle.BattleStyleName} ({trainer.Battle.BattleStyle}), {trainer.Battle.BattleTypeName} ({trainer.Battle.BattleType}), bgm 0x{trainer.Battle.BgmId:x}";
        Console.WriteLine($"{trainer.DeckName} #{trainer.Index}: {trainer.ClassName} {trainer.Name}; model {trainer.ModelName}; AI {trainer.Ai}; intro {trainer.CameraEffects}; {battle}; messages name {trainer.NameId}, pre {trainer.PreBattleTextId}, win {trainer.VictoryTextId}, loss {trainer.DefeatTextId}; {trainer.Location}");
    }
}

static void RunParityProbe(string isoPath, int messageLimit, int assetLimit)
{
    var context = ColosseumProjectContext.Open(isoPath);
    var iso = context.Iso ?? throw new InvalidOperationException("Input path did not load as an ISO.");
    var failures = new List<string>();

    var messageResult = ProbeMessages(iso, messageLimit, failures);
    var collisionResult = ProbeCollision(iso, assetLimit, failures);
    var vertexResult = ProbeVertexModels(iso, assetLimit, failures);

    Console.WriteLine($"ISO: {iso.Path}");
    Console.WriteLine($"Game ID: {iso.GameId}");
    Console.WriteLine($"Messages: {messageResult.Tables} tables, {messageResult.Strings} strings, {messageResult.Bytes} bytes round-tripped.");
    Console.WriteLine($"Collision: {collisionResult.Files} files, {collisionResult.NonEmptyFiles} non-empty, {collisionResult.Triangles} triangles parsed.");
    Console.WriteLine($"Vertex models: {vertexResult.WzxFiles} WZX files, {vertexResult.Models} embedded DAT models, {vertexResult.VertexColours} vertex colours parsed.");

    if (failures.Count == 0)
    {
        Console.WriteLine("Parity probe passed.");
        return;
    }

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"Probe failure: {failure}");
    }

    throw new InvalidDataException($"Parity probe failed with {failures.Count} failure(s).");
}

static void RunCloseoutProbe(string isoPath)
{
    isoPath = Path.GetFullPath(isoPath);
    var failures = new List<string>();

    ProbePriorityEditorRoundTrips(isoPath, failures);
    ProbeRepresentativePatchRoundTrip(isoPath, failures);
    ProbeRandomizerRoundTrip(isoPath, failures);

    Console.WriteLine($"Closeout ISO: {isoPath}");
    if (failures.Count == 0)
    {
        Console.WriteLine("Closeout probe passed.");
        return;
    }

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"Closeout failure: {failure}");
    }

    throw new InvalidDataException($"Closeout probe failed with {failures.Count} failure(s).");
}

static void ProbePriorityEditorRoundTrips(string isoPath, ICollection<string> failures)
{
    try
    {
        var context = ColosseumProjectContext.Open(isoPath);

        var trainer = context.LoadStoryTrainers().First(candidate => candidate.Pokemon.Any(pokemon => pokemon.IsSet));
        var trainerPokemon = trainer.Pokemon.First(pokemon => pokemon.IsSet);
        var trainerLevel = trainerPokemon.Level >= 99 ? trainerPokemon.Level - 1 : trainerPokemon.Level + 1;
        var trainerWrite = context.SaveTrainerPokemon(
        [
            TrainerPokemonUpdateFor(trainerPokemon, level: trainerLevel)
        ]);
        ImportWrittenFiles(context, [trainerWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedTrainerPokemon = context.LoadStoryTrainers()
            .SelectMany(candidate => candidate.Pokemon)
            .First(pokemon => pokemon.Index == trainerPokemon.Index);
        Expect(reopenedTrainerPokemon.Level == trainerLevel, failures, $"Trainer Editor did not persist Pokemon level for row {trainerPokemon.Index}.");

        var stats = context.LoadPokemonStats().First(candidate => candidate.Index > 0 && candidate.NameId > 0 && candidate.CatchRate > 0);
        var hp = stats.Hp >= 255 ? stats.Hp - 1 : stats.Hp + 1;
        var statsWrite = context.SavePokemonStats(PokemonStatsUpdateFor(stats, hp: hp));
        ImportWrittenFiles(context, [statsWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedStats = context.LoadPokemonStats().First(candidate => candidate.Index == stats.Index);
        Expect(reopenedStats.Hp == hp, failures, $"Pokemon Stats Editor did not persist HP for {stats.Name}.");

        var move = context.LoadMoves()
            .FirstOrDefault(candidate => candidate.Name.Equals("ICE PUNCH", StringComparison.OrdinalIgnoreCase))
            ?? context.LoadMoves().First(candidate => candidate.Index > 0 && !candidate.IsShadow && candidate.Pp > 0);
        var pp = move.Pp >= 64 ? move.Pp - 1 : move.Pp + 1;
        var moveWrite = context.SaveMove(MoveUpdateFor(move, pp: pp));
        ImportWrittenFiles(context, [moveWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedMove = context.LoadMoves().First(candidate => candidate.Index == move.Index);
        Expect(reopenedMove.Pp == pp, failures, $"Move Editor did not persist PP for {move.Name}.");

        var item = context.LoadItems().First(candidate => candidate.Index > 0 && candidate.NameId > 0 && candidate.Price > 0);
        var price = item.Price >= 65000 ? item.Price - 100 : item.Price + 100;
        var itemWrite = context.SaveItem(ItemUpdateFor(item, price: price));
        ImportWrittenFiles(context, [itemWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedItem = context.LoadItems().First(candidate => candidate.Index == item.Index);
        Expect(reopenedItem.Price == price, failures, $"Item Editor did not persist price for {item.Name}.");

        var type = context.LoadTypes().First(candidate => candidate.Effectiveness.Count >= 18);
        var effectiveness = type.Effectiveness.ToArray();
        effectiveness[0] = effectiveness[0] == 0x41 ? 0x3f : 0x41;
        var typeWrite = context.SaveType(new ColosseumTypeUpdate(type.Index, type.NameId, type.CategoryId, effectiveness));
        ImportWrittenFiles(context, [typeWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedType = context.LoadTypes().First(candidate => candidate.Index == type.Index);
        Expect(reopenedType.Effectiveness[0] == effectiveness[0], failures, $"Type Editor did not persist matchup value for {type.Name}.");

        var treasure = context.LoadTreasures().First(candidate => candidate.Index > 0 && candidate.ItemId > 0);
        var quantity = treasure.Quantity >= 99 ? treasure.Quantity - 1 : treasure.Quantity + 1;
        var treasureWrite = context.SaveTreasure(TreasureUpdateFor(treasure, quantity: quantity));
        ImportWrittenFiles(context, [treasureWrite]);

        context = ColosseumProjectContext.Open(isoPath);
        var reopenedTreasure = context.LoadTreasures().First(candidate => candidate.Index == treasure.Index);
        Expect(reopenedTreasure.Quantity == quantity, failures, $"Treasure Editor did not persist quantity for treasure {treasure.Index}.");

        var gifts = context.LoadGiftPokemon();
        var gift = gifts.FirstOrDefault(candidate => candidate.RowId == 5 && candidate.StartOffset > 0)
            ?? gifts.FirstOrDefault(candidate => candidate.StartOffset > 0);
        if (gift is not null)
        {
            var giftLevel = gift.Level >= 99 ? gift.Level - 1 : gift.Level + 1;
            var giftWrite = context.SaveGiftPokemon(new ColosseumGiftPokemonUpdate(
                gift.RowId,
                gift.SpeciesId,
                giftLevel,
                gift.MoveIds,
                gift.ShinyValue,
                gift.Gender,
                gift.Nature));
            ImportWrittenFiles(context, [giftWrite]);

            context = ColosseumProjectContext.Open(isoPath);
            var reopenedGift = context.LoadGiftPokemon().First(candidate => candidate.RowId == gift.RowId);
            Expect(reopenedGift.Level == giftLevel, failures, $"Gift Pokemon Editor did not persist level for {gift.GiftType}.");
        }

        Console.WriteLine("Priority editor save/import/reopen probe passed.");
    }
    catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or IOException or ArgumentOutOfRangeException)
    {
        failures.Add($"Priority editor probe failed: {ex.Message}");
    }
}

static void ProbeRepresentativePatchRoundTrip(string isoPath, ICollection<string> failures)
{
    try
    {
        var context = ColosseumProjectContext.Open(isoPath);
        var result = context.ApplyPatch(ColosseumPatchKind.PhysicalSpecialSplitApply);
        Expect(result.WrittenFiles.Count > 0, failures, "Physical/special split patch did not write any workspace files.");
        ImportWrittenFiles(context, result.WrittenFiles);

        var reopened = ColosseumProjectContext.Open(isoPath);
        Expect(reopened.LoadCommonRel().IsPhysicalSpecialSplitImplemented, failures, "Physical/special split patch was not detected after import/reopen.");
        Console.WriteLine("Representative patch apply/import/reopen probe passed.");
    }
    catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or IOException or ArgumentOutOfRangeException or NotSupportedException)
    {
        failures.Add($"Patch probe failed: {ex.Message}");
    }
}

static void ProbeRandomizerRoundTrip(string isoPath, ICollection<string> failures)
{
    try
    {
        var context = ColosseumProjectContext.Open(isoPath);
        var result = context.Randomize(new ColosseumRandomizerOptions(
            StarterPokemon: false,
            ShadowPokemon: false,
            NpcPokemon: false,
            PokemonMoves: false,
            PokemonTypes: false,
            PokemonAbilities: false,
            PokemonStats: false,
            PokemonEvolutions: false,
            MoveTypes: true,
            TypeMatchups: true,
            TmMoves: true,
            ItemBoxes: true,
            ShopItems: true,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        Expect(result.WrittenFiles.Count > 0, failures, "Randomizer did not write any workspace files.");
        Expect(result.Messages.Count > 0, failures, "Randomizer did not report any changes.");
        ImportWrittenFiles(context, result.WrittenFiles);

        var reopened = ColosseumProjectContext.Open(isoPath);
        Expect(reopened.LoadMoves().Any(move => move.Index > 0), failures, "Move table did not reload after randomizer import.");
        Expect(reopened.LoadTypes().All(type => type.Effectiveness.Count == 18), failures, "Type matchup rows did not reload with 18 entries after randomizer import.");
        Expect(reopened.LoadTreasures().Any(treasure => treasure.ItemId > 0), failures, "Treasure table did not reload after randomizer import.");
        Expect(reopened.LoadItems().Any(item => item.Index > 0), failures, "Item table did not reload after randomizer import.");
        Console.WriteLine("Randomizer write/import/reopen probe passed.");
    }
    catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or IOException or ArgumentOutOfRangeException)
    {
        failures.Add($"Randomizer probe failed: {ex.Message}");
    }
}

static ProbeMessageResult ProbeMessages(GameCubeIso iso, int limit, ICollection<string> failures)
{
    var tableCount = 0;
    var stringCount = 0;
    long bytesChecked = 0;

    foreach (var (isoEntry, archive) in EnumerateFsysArchives(iso))
    {
        foreach (var entry in archive.Entries.Where(entry => entry.FileType == GameFileType.Message))
        {
            if (tableCount >= limit)
            {
                return new ProbeMessageResult(tableCount, stringCount, bytesChecked);
            }

            try
            {
                var bytes = archive.Extract(entry);
                var table = GameStringTable.Parse(bytes);
                var rebuilt = table.ToArray(allowGrowth: false);
                var reparsed = GameStringTable.Parse(rebuilt);
                if (rebuilt.Length != bytes.Length)
                {
                    failures.Add($"{isoEntry.Name}/{entry.Name} rebuilt to {rebuilt.Length} bytes instead of {bytes.Length}.");
                }

                if (!SameStrings(table.Strings, reparsed.Strings))
                {
                    failures.Add($"{isoEntry.Name}/{entry.Name} did not preserve message IDs/text through rebuild.");
                }

                tableCount++;
                stringCount += table.Strings.Count;
                bytesChecked += bytes.Length;
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
            {
                failures.Add($"{isoEntry.Name}/{entry.Name} message parse/rebuild failed: {ex.Message}");
            }
        }
    }

    if (tableCount == 0)
    {
        failures.Add("No message tables were found to probe.");
    }

    return new ProbeMessageResult(tableCount, stringCount, bytesChecked);
}

static ProbeCollisionResult ProbeCollision(GameCubeIso iso, int limit, ICollection<string> failures)
{
    var files = 0;
    var nonEmpty = 0;
    var triangles = 0;

    foreach (var (isoEntry, archive) in EnumerateFsysArchives(iso))
    {
        foreach (var entry in archive.Entries.Where(entry => entry.FileType == GameFileType.Collision))
        {
            if (files >= limit)
            {
                return new ProbeCollisionResult(files, nonEmpty, triangles);
            }

            try
            {
                var collision = ColosseumCollisionData.Parse(archive.Extract(entry));
                files++;
                triangles += collision.Triangles.Count;
                if (collision.Triangles.Count > 0)
                {
                    nonEmpty++;
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
            {
                failures.Add($"{isoEntry.Name}/{entry.Name} collision parse failed: {ex.Message}");
            }
        }
    }

    if (files == 0)
    {
        failures.Add("No collision files were found to probe.");
    }
    else if (nonEmpty == 0)
    {
        failures.Add("Collision files were found, but none parsed into renderable triangles.");
    }

    return new ProbeCollisionResult(files, nonEmpty, triangles);
}

static ProbeVertexResult ProbeVertexModels(GameCubeIso iso, int limit, ICollection<string> failures)
{
    var wzxFiles = 0;
    var models = 0;
    var vertexColours = 0;

    foreach (var (isoEntry, archive) in EnumerateFsysArchives(iso))
    {
        foreach (var entry in archive.Entries.Where(entry => entry.FileType == GameFileType.Wzx))
        {
            if (wzxFiles >= limit)
            {
                return new ProbeVertexResult(wzxFiles, models, vertexColours);
            }

            try
            {
                wzxFiles++;
                foreach (var model in ColosseumLegacyFileCodecs.ExtractWzxDatModels(archive.Extract(entry)))
                {
                    models++;
                    var parsed = ColosseumDatVertexColorModel.Parse(model.Data);
                    vertexColours += parsed.VertexColors.Count;
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
            {
                failures.Add($"{isoEntry.Name}/{entry.Name} WZX/DAT vertex probe failed: {ex.Message}");
            }
        }
    }

    if (wzxFiles == 0)
    {
        failures.Add("No WZX files were found to probe.");
    }
    else if (models == 0)
    {
        failures.Add("WZX files were found, but no embedded DAT models were detected.");
    }

    return new ProbeVertexResult(wzxFiles, models, vertexColours);
}

static IEnumerable<(GameCubeIsoFileEntry Entry, FsysArchive Archive)> EnumerateFsysArchives(GameCubeIso iso)
{
    foreach (var entry in iso.Files.Where(entry => Path.GetExtension(entry.Name).Equals(".fsys", StringComparison.OrdinalIgnoreCase)))
    {
        FsysArchive archive;
        try
        {
            archive = FsysArchive.Parse(entry.Name, GameCubeIsoReader.ReadFile(iso, entry));
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or EndOfStreamException or OverflowException)
        {
            continue;
        }

        yield return (entry, archive);
    }
}

static bool SameStrings(IReadOnlyList<GameString> left, IReadOnlyList<GameString> right)
{
    if (left.Count != right.Count)
    {
        return false;
    }

    for (var index = 0; index < left.Count; index++)
    {
        if (left[index].Id != right[index].Id
            || !string.Equals(left[index].Text, right[index].Text, StringComparison.Ordinal))
        {
            return false;
        }
    }

    return true;
}

static int ReadIntOption(string[] args, string name, int fallback)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(args[index + 1], out var parsed)
            && parsed > 0)
        {
            return parsed;
        }
    }

    return fallback;
}

static void Expect(bool condition, ICollection<string> failures, string message)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static ColosseumTrainerPokemonUpdate TrainerPokemonUpdateFor(
    ColosseumTrainerPokemon pokemon,
    int? level = null,
    int? happiness = null)
    => new(
        pokemon.Index,
        pokemon.SpeciesId,
        level ?? pokemon.Level,
        pokemon.ShadowId,
        pokemon.ItemId,
        pokemon.PokeballId,
        pokemon.Ability,
        pokemon.Nature,
        pokemon.Gender,
        happiness ?? pokemon.Happiness,
        pokemon.Iv,
        pokemon.Evs,
        pokemon.Moves.Select(move => move.Index).ToArray(),
        pokemon.ShadowData?.HeartGauge ?? 0,
        pokemon.ShadowData?.FirstTrainerId ?? 0,
        pokemon.ShadowData?.AlternateFirstTrainerId ?? 0,
        pokemon.ShadowData?.CatchRate ?? 0);

static ColosseumPokemonStatsUpdate PokemonStatsUpdateFor(
    ColosseumPokemonStats pokemon,
    int? hp = null)
    => new(
        pokemon.Index,
        pokemon.NameId,
        pokemon.ExpRate,
        pokemon.GenderRatio,
        pokemon.BaseExp,
        pokemon.BaseHappiness,
        pokemon.Height,
        pokemon.Weight,
        pokemon.Type1,
        pokemon.Type2,
        pokemon.Ability1,
        pokemon.Ability2,
        pokemon.HeldItem1,
        pokemon.HeldItem2,
        pokemon.CatchRate,
        hp ?? pokemon.Hp,
        pokemon.Attack,
        pokemon.Defense,
        pokemon.SpecialAttack,
        pokemon.SpecialDefense,
        pokemon.Speed,
        pokemon.HpYield,
        pokemon.AttackYield,
        pokemon.DefenseYield,
        pokemon.SpecialAttackYield,
        pokemon.SpecialDefenseYield,
        pokemon.SpeedYield,
        pokemon.LearnableTms,
        pokemon.LevelUpMoves,
        pokemon.Evolutions);

static ColosseumMoveUpdate MoveUpdateFor(ColosseumMove move, int? pp = null)
    => new(
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
        pp ?? move.Pp,
        move.Priority,
        move.EffectAccuracy,
        move.HmFlag,
        move.SoundBasedFlag,
        move.ContactFlag,
        move.KingsRockFlag,
        move.ProtectFlag,
        move.SnatchFlag,
        move.MagicCoatFlag,
        move.MirrorMoveFlag);

static ColosseumItemUpdate ItemUpdateFor(ColosseumItem item, int? price = null)
    => new(
        item.Index,
        item.NameId,
        item.DescriptionId,
        item.BagSlotId,
        item.CanBeHeld,
        price ?? item.Price,
        item.CouponPrice,
        item.Parameter,
        item.HoldItemId,
        item.InBattleUseId,
        item.FriendshipEffects,
        item.TmIndex,
        item.TmMoveId);

static ColosseumTreasureUpdate TreasureUpdateFor(ColosseumTreasure treasure, int? quantity = null)
    => new(
        treasure.Index,
        treasure.ModelId,
        quantity ?? treasure.Quantity,
        treasure.Angle,
        treasure.RoomId,
        treasure.ItemId,
        treasure.X,
        treasure.Y,
        treasure.Z);

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

static void ApplyXdSmokeOperation(string isoPath, string operation)
{
    var context = XdProjectContext.Open(isoPath);
    var writtenFiles = new List<string>();
    if (operation.StartsWith("patch:", StringComparison.OrdinalIgnoreCase))
    {
        var patchName = operation["patch:".Length..];
        var patchKind = ParseXdPatchKind(patchName);
        var result = context.ApplyPatch(patchKind);
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine($"Applied XD patch: {result.Patch.Kind}");
        Console.WriteLine(result.Patch.Name);
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("randomizer-data", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new XdRandomizerOptions(
            StarterPokemon: false,
            ObtainablePokemon: false,
            UnobtainablePokemon: false,
            PokemonMoves: false,
            PokemonTypes: true,
            PokemonAbilities: true,
            PokemonStats: false,
            PokemonEvolutions: true,
            MoveTypes: true,
            TypeMatchups: true,
            TmMoves: true,
            ItemBoxes: true,
            ShopItems: true,
            BattleBingo: false,
            ShinyHues: false,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: true));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied XD randomizer: data-table smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("randomizer-species", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new XdRandomizerOptions(
            StarterPokemon: true,
            ObtainablePokemon: true,
            UnobtainablePokemon: true,
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
            BattleBingo: false,
            ShinyHues: false,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied XD randomizer: species smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("randomizer-bingo", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new XdRandomizerOptions(
            StarterPokemon: false,
            ObtainablePokemon: false,
            UnobtainablePokemon: false,
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
            BattleBingo: true,
            ShinyHues: false,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied XD randomizer: Battle Bingo smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else if (operation.Equals("randomizer-shiny-hues", StringComparison.OrdinalIgnoreCase))
    {
        var result = context.Randomize(new XdRandomizerOptions(
            StarterPokemon: false,
            ObtainablePokemon: false,
            UnobtainablePokemon: false,
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
            BattleBingo: false,
            ShinyHues: true,
            SimilarBaseStatTotal: false,
            RemoveItemOrTradeEvolutions: false));
        writtenFiles.AddRange(result.WrittenFiles);
        Console.WriteLine("Applied XD randomizer: shiny hue ASM smoke");
        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }
    }
    else
    {
        throw new InvalidOperationException($"Unknown XD smoke operation: {operation}");
    }

    foreach (var path in writtenFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
    {
        Console.WriteLine($"Workspace write: {Path.GetFullPath(path)}");
    }

    var reopened = XdProjectContext.Open(isoPath);
    Console.WriteLine(
        $"XD reopen: stats={reopened.LoadPokemonStatsRecords().Count}, moves={reopened.LoadMoveRecords().Count}, tms={reopened.LoadTmMoveRecords().Count}, types={reopened.LoadTypeRecords().Count}, treasures={reopened.LoadTreasureRecords().Count}");
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

static XdPatchKind ParseXdPatchKind(string patchName)
{
    if (Enum.TryParse<XdPatchKind>(patchName, ignoreCase: true, out var parsed))
    {
        return parsed;
    }

    var normalized = patchName.Replace("-", string.Empty, StringComparison.Ordinal)
        .Replace("_", string.Empty, StringComparison.Ordinal)
        .Replace(" ", string.Empty, StringComparison.Ordinal);
    foreach (var value in Enum.GetValues<XdPatchKind>())
    {
        if (value.ToString().Equals(normalized, StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }
    }

    throw new InvalidOperationException($"Unknown XD patch kind: {patchName}");
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

internal sealed record ProbeMessageResult(int Tables, int Strings, long Bytes);

internal sealed record ProbeCollisionResult(int Files, int NonEmptyFiles, int Triangles);

internal sealed record ProbeVertexResult(int WzxFiles, int Models, int VertexColours);
