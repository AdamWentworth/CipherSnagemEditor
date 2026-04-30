using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Colosseum.Data;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.XD;

namespace CipherSnagemEditor.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush TrainerNormalBrush = SolidColorBrush.Parse("#FC6848");
    private static readonly IBrush TrainerShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private readonly GameCubeGame _startupGame;
    private readonly List<IsoFileEntryViewModel> _allIsoFiles = [];
    private readonly List<TrainerEntryViewModel> _allTrainers = [];
    private readonly List<PokemonStatsEntryViewModel> _allPokemonStats = [];
    private readonly List<MoveEntryViewModel> _allMoves = [];
    private readonly List<ItemEntryViewModel> _allItems = [];
    private readonly List<GiftPokemonEntryViewModel> _allGiftPokemon = [];
    private readonly List<TypeEntryViewModel> _allTypes = [];
    private readonly List<TreasureEntryViewModel> _allTreasures = [];
    private readonly List<InteractionEntryViewModel> _allInteractions = [];
    private readonly List<MessageStringEntryViewModel> _allMessageStrings = [];
    private readonly List<TableEditorEntryViewModel> _allTableEditorEntries = [];
    private readonly List<PatchEntryViewModel> _allPatches = [];
    private readonly List<CollisionFileEntryViewModel> _allCollisionFiles = [];
    private readonly List<VertexFilterFileEntryViewModel> _allVertexFilterFiles = [];
    private TrainerPokemonEditorResources _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
    private PokemonStatsEditorResources _pokemonStatsResources = PokemonStatsEditorResources.Empty;
    private MoveEditorResources _moveEditorResources = MoveEditorResources.Empty;
    private ItemEditorResources _itemEditorResources = ItemEditorResources.Empty;
    private GiftPokemonEditorResources _giftPokemonResources = GiftPokemonEditorResources.Empty;
    private TreasureEditorResources _treasureEditorResources = TreasureEditorResources.Empty;
    private InteractionEditorResources _interactionEditorResources = InteractionEditorResources.Empty;
    private TrainerEntryViewModel? _lastSelectedTrainer;

    [ObservableProperty]
    private ToolEntryViewModel? _selectedTool;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMessageEditorReadOnly))]
    private bool _isBusy;

    [ObservableProperty]
    private string _windowTitle = "Colosseum Tool - Cipher Snagem Editor";

    [ObservableProperty]
    private string _legacyToolTitle = "Colosseum Tool";

    [ObservableProperty]
    private string _mainWindowIconResource = "avares://CipherSnagemEditor.App/Assets/AppIcon.png";

    [ObservableProperty]
    private string _projectTitle = "No file loaded";

    [ObservableProperty]
    private string _workspaceStatus = "Open a Pokemon Colosseum ISO, FSYS archive, message table, or texture file.";

    [ObservableProperty]
    private string _selectedToolDetail = "Trainer Editor is the first parity target.";

    [ObservableProperty]
    private IsoFileEntryViewModel? _selectedIsoFile;

    [ObservableProperty]
    private TrainerEntryViewModel? _selectedTrainer;

    [ObservableProperty]
    private PokemonStatsEntryViewModel? _selectedPokemonStats;

    [ObservableProperty]
    private PokemonStatsEditorViewModel? _selectedPokemonStatsDetail;

    [ObservableProperty]
    private MoveEntryViewModel? _selectedMove;

    [ObservableProperty]
    private MoveEditorViewModel? _selectedMoveDetail;

    [ObservableProperty]
    private ItemEntryViewModel? _selectedItem;

    [ObservableProperty]
    private ItemEditorViewModel? _selectedItemDetail;

    [ObservableProperty]
    private GiftPokemonEntryViewModel? _selectedGiftPokemon;

    [ObservableProperty]
    private GiftPokemonEditorViewModel? _selectedGiftPokemonDetail;

    [ObservableProperty]
    private TypeEntryViewModel? _selectedType;

    [ObservableProperty]
    private TypeEditorViewModel? _selectedTypeDetail;

    [ObservableProperty]
    private TreasureEntryViewModel? _selectedTreasure;

    [ObservableProperty]
    private TreasureEditorViewModel? _selectedTreasureDetail;

    [ObservableProperty]
    private InteractionEntryViewModel? _selectedInteraction;

    [ObservableProperty]
    private InteractionEditorViewModel? _selectedInteractionDetail;

    [ObservableProperty]
    private MessageTableViewModel? _selectedMessageTable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMessageStringSelected))]
    [NotifyPropertyChangedFor(nameof(IsMessageEditorReadOnly))]
    private MessageStringEntryViewModel? _selectedMessageString;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTableEditorEntry))]
    [NotifyPropertyChangedFor(nameof(SelectedTableEditorName))]
    [NotifyPropertyChangedFor(nameof(SelectedTableEditorDetails))]
    private TableEditorEntryViewModel? _selectedTableEditorEntry;

    [ObservableProperty]
    private PatchEntryViewModel? _selectedPatch;

    [ObservableProperty]
    private CollisionFileEntryViewModel? _selectedCollisionFile;

    [ObservableProperty]
    private ColosseumCollisionData? _selectedCollisionData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCollisionInteractionValue))]
    private PickerOptionViewModel? _selectedCollisionInteraction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCollisionSectionValue))]
    private PickerOptionViewModel? _selectedCollisionSection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedVertexFilterFilePath))]
    [NotifyPropertyChangedFor(nameof(SelectedVertexFilterFileName))]
    private VertexFilterFileEntryViewModel? _selectedVertexFilterFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedVertexFilterValue))]
    private PickerOptionViewModel? _selectedVertexFilter;

    [ObservableProperty]
    private string _selectedMessageIdText = string.Empty;

    [ObservableProperty]
    private string _selectedMessageText = string.Empty;

    [ObservableProperty]
    private string _trainerSearchText = string.Empty;

    [ObservableProperty]
    private string _pokemonStatsSearchText = string.Empty;

    [ObservableProperty]
    private string _moveSearchText = string.Empty;

    [ObservableProperty]
    private string _itemSearchText = string.Empty;

    [ObservableProperty]
    private string _giftPokemonSearchText = string.Empty;

    [ObservableProperty]
    private string _typeSearchText = string.Empty;

    [ObservableProperty]
    private string _treasureSearchText = string.Empty;

    [ObservableProperty]
    private string _messageSearchText = string.Empty;

    [ObservableProperty]
    private string _isoSearchText = string.Empty;

    [ObservableProperty]
    private string _tableEditorSearchText = string.Empty;

    [ObservableProperty]
    private string _collisionSearchText = string.Empty;

    [ObservableProperty]
    private string _vertexFilterSearchText = string.Empty;

    [ObservableProperty]
    private bool _showIsoExplorer;

    [ObservableProperty]
    private bool _showTrainerEditor;

    [ObservableProperty]
    private bool _showPokemonStatsEditor;

    [ObservableProperty]
    private bool _showReturnHome;

    [ObservableProperty]
    private string _isoExplorerStatus = "Open a Colosseum ISO to browse its files.";

    [ObservableProperty]
    private string _patchStatus = "Select a patch to apply it.";

    [ObservableProperty]
    private string _collisionStatus = "Extract ISO files, then select a collision file.";

    [ObservableProperty]
    private string _vertexFilterStatus = "Extract and decode texture DAT files before using vertex filters.";

    [ObservableProperty]
    private string _randomizerStatus = "Select randomizer options.";

    [ObservableProperty]
    private bool _randomizerStarterPokemon = true;

    [ObservableProperty]
    private bool _randomizerShadowPokemon = true;

    [ObservableProperty]
    private bool _randomizerNpcPokemon = true;

    [ObservableProperty]
    private bool _randomizerPokemonMoves;

    [ObservableProperty]
    private bool _randomizerPokemonTypes;

    [ObservableProperty]
    private bool _randomizerPokemonAbilities;

    [ObservableProperty]
    private bool _randomizerPokemonStats;

    [ObservableProperty]
    private bool _randomizerPokemonEvolutions;

    [ObservableProperty]
    private bool _randomizerMoveTypes;

    [ObservableProperty]
    private bool _randomizerTypeMatchups;

    [ObservableProperty]
    private bool _randomizerTmMoves;

    [ObservableProperty]
    private bool _randomizerItemBoxes;

    [ObservableProperty]
    private bool _randomizerShopItems;

    [ObservableProperty]
    private bool _randomizerSimilarBaseStatTotal;

    [ObservableProperty]
    private bool _randomizerRemoveItemOrTradeEvolutions;

    [ObservableProperty]
    private string _leftPanelTitle = "Tools";

    [ObservableProperty]
    private bool _showLeftPanelTitle = true;

    [ObservableProperty]
    private bool _showHomeTools = true;

    [ObservableProperty]
    private bool _showToolPlaceholder = true;

    [ObservableProperty]
    private GridLength _leftPanelWidth = new(220);

    [ObservableProperty]
    private GridLength _workspacePanelHeight = GridLength.Auto;

    [ObservableProperty]
    private GridLength _logPanelHeight = new(150);

    [ObservableProperty]
    private string _isoExplorerFilesText = "-";

    [ObservableProperty]
    private string _selectedIsoFileName = "File name: -";

    [ObservableProperty]
    private string _selectedIsoFileSize = "File size: -";

    [ObservableProperty]
    private string _selectedTrainerName = "Name";

    [ObservableProperty]
    private string _selectedTrainerClass = "Class";

    [ObservableProperty]
    private string _selectedTrainerModel = "Model";

    [ObservableProperty]
    private string _selectedTrainerAi = "AI";

    [ObservableProperty]
    private string _selectedTrainerBattleStyle = "-";

    [ObservableProperty]
    private string _selectedTrainerBattleType = "-";

    [ObservableProperty]
    private string _selectedTrainerBattleId = "0";

    [ObservableProperty]
    private string _selectedTrainerBgm = "-";

    [ObservableProperty]
    private string _selectedTrainerStringIds = "Name ID: -   Pre: -   Win: -   Loss: -";

    [ObservableProperty]
    private IBrush _trainerDetailBackgroundBrush = TrainerNormalBrush;

    public MainWindowViewModel()
        : this(GameCubeGame.PokemonColosseum)
    {
    }

    public MainWindowViewModel(GameCubeGame startupGame)
    {
        _startupGame = startupGame;
        Tools = [];
        ApplyGameMode(startupGame);
        foreach (var option in BuildVertexFilterOptions())
        {
            VertexFilterOptions.Add(option);
        }

        SelectedVertexFilter = VertexFilterOptions.FirstOrDefault();
        Logs.Add("Cipher Snagem Editor ready.");
        Logs.Add($"Legacy mode: {LegacyToolTitle}.");
    }

    public ObservableCollection<ToolEntryViewModel> Tools { get; }

    public ObservableCollection<string> Logs { get; } = [];

    public ObservableCollection<IsoFileEntryViewModel> IsoFiles { get; } = [];

    public ObservableCollection<TrainerEntryViewModel> Trainers { get; } = [];

    public ObservableCollection<PokemonStatsEntryViewModel> PokemonStatsEntries { get; } = [];

    public ObservableCollection<MoveEntryViewModel> MoveEntries { get; } = [];

    public ObservableCollection<ItemEntryViewModel> ItemEntries { get; } = [];

    public ObservableCollection<GiftPokemonEntryViewModel> GiftPokemonEntries { get; } = [];

    public ObservableCollection<TypeEntryViewModel> TypeEntries { get; } = [];

    public ObservableCollection<TreasureEntryViewModel> TreasureEntries { get; } = [];

    public ObservableCollection<InteractionEntryViewModel> InteractionEntries { get; } = [];

    public ObservableCollection<MessageTableViewModel> MessageTables { get; } = [];

    public ObservableCollection<MessageStringEntryViewModel> MessageStrings { get; } = [];

    public ObservableCollection<TableEditorEntryViewModel> TableEditorEntries { get; } = [];

    public ObservableCollection<PatchEntryViewModel> PatchEntries { get; } = [];

    public ObservableCollection<CollisionFileEntryViewModel> CollisionFiles { get; } = [];

    public ObservableCollection<PickerOptionViewModel> CollisionInteractionOptions { get; } = [];

    public ObservableCollection<PickerOptionViewModel> CollisionSectionOptions { get; } = [];

    public ObservableCollection<VertexFilterFileEntryViewModel> VertexFilterFiles { get; } = [];

    public ObservableCollection<PickerOptionViewModel> VertexFilterOptions { get; } = [];

    public bool CanAddFileToSelectedIsoFile => CanExportSelectedIsoFile() && SelectedIsoFile?.IsFsys == true;

    public int SelectedCollisionInteractionValue => SelectedCollisionInteraction?.Value ?? -1;

    public int SelectedCollisionSectionValue => SelectedCollisionSection?.Value ?? -1;

    public int SelectedVertexFilterValue => SelectedVertexFilter?.Value ?? 0;

    public string? SelectedVertexFilterFilePath => SelectedVertexFilterFile?.File.Path;

    public string SelectedVertexFilterFileName => SelectedVertexFilterFile?.File.FileName ?? "File";

    public bool IsMessageStringSelected => SelectedMessageString is not null;

    public bool IsMessageEditorReadOnly => SelectedMessageString is null || IsBusy;

    [ObservableProperty]
    private IReadOnlyList<TrainerPokemonSlotViewModel> _selectedTrainerPokemon = [];

    public bool HasSelectedTableEditorEntry => SelectedTableEditorEntry is not null;

    public string SelectedTableEditorName => SelectedTableEditorEntry?.Name ?? "Data Table";

    public string SelectedTableEditorDetails => SelectedTableEditorEntry?.Details ?? "Details: -";

    public ColosseumProjectContext? CurrentProject { get; private set; }

    public XdProjectContext? CurrentXdProject { get; private set; }

    public GameCubeGame CurrentGame { get; private set; } = GameCubeGame.PokemonColosseum;

    private void LoadToolCatalog(GameCubeGame game)
    {
        Tools.Clear();
        IEnumerable<ToolEntryViewModel> tools = game == GameCubeGame.PokemonXD
            ? XdToolCatalog.HomeTools.Select((tool, index) => new ToolEntryViewModel(index + 1, tool))
            : ColosseumToolCatalog.HomeTools.Select((tool, index) => new ToolEntryViewModel(index + 1, tool));

        foreach (var tool in tools)
        {
            Tools.Add(tool);
        }

        SelectedTool = Tools.FirstOrDefault();
    }

    private void ApplyGameMode(GameCubeGame game)
    {
        CurrentGame = game;
        LegacyToolTitle = game == GameCubeGame.PokemonXD ? "GoD Tool" : "Colosseum Tool";
        WindowTitle = $"{LegacyToolTitle} - Cipher Snagem Editor";
        MainWindowIconResource = game == GameCubeGame.PokemonXD
            ? "avares://CipherSnagemEditor.App/Assets/AppIconXd.png"
            : "avares://CipherSnagemEditor.App/Assets/AppIcon.png";
        LoadToolCatalog(game);
    }

    public async Task OpenPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        IsBusy = true;
        Logs.Add($"Opening {path}");
        var openTimer = Stopwatch.StartNew();

        try
        {
            if (GameFileTypes.FromExtension(path) == GameFileType.Iso)
            {
                var probeTimer = Stopwatch.StartNew();
                var probeIso = await Task.Run(() => GameCubeIsoReader.Open(path));
                LogPerformance("Open probe ISO game", probeTimer);

                if (_startupGame == GameCubeGame.PokemonXD)
                {
                    if (!probeIso.IsPokemonXD)
                    {
                        throw new InvalidDataException($"Expected Pokemon XD ISO GXXE/GXXP/GXXJ, found {probeIso.GameId}.");
                    }

                    await OpenXdPathAsync(path, openTimer);
                    return;
                }

                if (!probeIso.IsPokemonColosseum)
                {
                    throw new InvalidDataException($"Expected Pokemon Colosseum ISO GC6E/GC6P/GC6J, found {probeIso.GameId}.");
                }
            }
            else if (_startupGame == GameCubeGame.PokemonXD)
            {
                await OpenXdPathAsync(path, openTimer);
                return;
            }

            var contextTimer = Stopwatch.StartNew();
            var context = await Task.Run(() => ColosseumProjectContext.Open(path));
            LogPerformance("Open project context", contextTimer);

            var resetTimer = Stopwatch.StartNew();
            ApplyGameMode(GameCubeGame.PokemonColosseum);
            CurrentProject = context;
            CurrentXdProject = null;
            HasProject = true;
            ProjectTitle = BuildProjectTitle(context);
            WorkspaceStatus = BuildWorkspaceStatus(context);
            _allIsoFiles.Clear();
            _allTrainers.Clear();
            _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
            _allPokemonStats.Clear();
            _pokemonStatsResources = PokemonStatsEditorResources.Empty;
            _allMoves.Clear();
            _moveEditorResources = MoveEditorResources.Empty;
            _allItems.Clear();
            _itemEditorResources = ItemEditorResources.Empty;
            _allGiftPokemon.Clear();
            _giftPokemonResources = GiftPokemonEditorResources.Empty;
            _allTypes.Clear();
            _allTreasures.Clear();
            _treasureEditorResources = TreasureEditorResources.Empty;
            _allInteractions.Clear();
            _interactionEditorResources = InteractionEditorResources.Empty;
            _allMessageStrings.Clear();
            _allTableEditorEntries.Clear();
            _allPatches.Clear();
            _allCollisionFiles.Clear();
            _allVertexFilterFiles.Clear();
            Trainers.Clear();
            PokemonStatsEntries.Clear();
            MoveEntries.Clear();
            ItemEntries.Clear();
            GiftPokemonEntries.Clear();
            TypeEntries.Clear();
            TreasureEntries.Clear();
            InteractionEntries.Clear();
            MessageTables.Clear();
            MessageStrings.Clear();
            TableEditorEntries.Clear();
            PatchEntries.Clear();
            CollisionFiles.Clear();
            VertexFilterFiles.Clear();
            CollisionInteractionOptions.Clear();
            CollisionSectionOptions.Clear();
            TrainerSearchText = string.Empty;
            PokemonStatsSearchText = string.Empty;
            MoveSearchText = string.Empty;
            ItemSearchText = string.Empty;
            GiftPokemonSearchText = string.Empty;
            TypeSearchText = string.Empty;
            TreasureSearchText = string.Empty;
            MessageSearchText = string.Empty;
            IsoSearchText = string.Empty;
            TableEditorSearchText = string.Empty;
            CollisionSearchText = string.Empty;
            VertexFilterSearchText = string.Empty;
            SelectedTrainer = null;
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            SelectedMove = null;
            SelectedMoveDetail = null;
            SelectedItem = null;
            SelectedItemDetail = null;
            SelectedGiftPokemon = null;
            SelectedGiftPokemonDetail = null;
            SelectedType = null;
            SelectedTypeDetail = null;
            SelectedTreasure = null;
            SelectedTreasureDetail = null;
            SelectedInteraction = null;
            SelectedInteractionDetail = null;
            SelectedMessageTable = null;
            SelectedMessageString = null;
            SelectedTableEditorEntry = null;
            SelectedPatch = null;
            SelectedCollisionFile = null;
            SelectedCollisionData = null;
            SelectedCollisionInteraction = null;
            SelectedCollisionSection = null;
            SelectedVertexFilterFile = null;
            SelectedVertexFilter = VertexFilterOptions.FirstOrDefault();
            PatchStatus = "Select a patch to apply it.";
            CollisionStatus = "Extract ISO files, then select a collision file.";
            VertexFilterStatus = "Extract and decode texture DAT files before using vertex filters.";
            ResetRandomizerDefaults();
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            SelectedTrainerPokemon = [];
            LogPerformance("Open reset UI state", resetTimer);
            PopulateIsoFiles(context);

            var selectedToolTimer = Stopwatch.StartNew();
            RefreshSelectedToolView(SelectedTool);
            LogPerformance("Open refresh selected tool", selectedToolTimer);
            Logs.Add(BuildLogSummary(context));
            LogPerformance("Open total", openTimer);
        }
        catch (Exception ex)
        {
            HasProject = false;
            CurrentProject = null;
            CurrentXdProject = null;
            ApplyGameMode(_startupGame);
            ProjectTitle = "Open failed";
            WorkspaceStatus = ex.Message;
            _allIsoFiles.Clear();
            IsoFiles.Clear();
            _allTrainers.Clear();
            _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
            _allPokemonStats.Clear();
            _pokemonStatsResources = PokemonStatsEditorResources.Empty;
            _allMoves.Clear();
            _moveEditorResources = MoveEditorResources.Empty;
            _allItems.Clear();
            _itemEditorResources = ItemEditorResources.Empty;
            _allGiftPokemon.Clear();
            _giftPokemonResources = GiftPokemonEditorResources.Empty;
            _allTypes.Clear();
            _allTreasures.Clear();
            _treasureEditorResources = TreasureEditorResources.Empty;
            _allInteractions.Clear();
            _interactionEditorResources = InteractionEditorResources.Empty;
            _allMessageStrings.Clear();
            _allTableEditorEntries.Clear();
            _allPatches.Clear();
            _allCollisionFiles.Clear();
            _allVertexFilterFiles.Clear();
            Trainers.Clear();
            PokemonStatsEntries.Clear();
            MoveEntries.Clear();
            ItemEntries.Clear();
            GiftPokemonEntries.Clear();
            TypeEntries.Clear();
            TreasureEntries.Clear();
            InteractionEntries.Clear();
            MessageTables.Clear();
            MessageStrings.Clear();
            TableEditorEntries.Clear();
            PatchEntries.Clear();
            CollisionFiles.Clear();
            VertexFilterFiles.Clear();
            CollisionInteractionOptions.Clear();
            CollisionSectionOptions.Clear();
            TrainerSearchText = string.Empty;
            PokemonStatsSearchText = string.Empty;
            MoveSearchText = string.Empty;
            ItemSearchText = string.Empty;
            GiftPokemonSearchText = string.Empty;
            TypeSearchText = string.Empty;
            TreasureSearchText = string.Empty;
            MessageSearchText = string.Empty;
            IsoSearchText = string.Empty;
            TableEditorSearchText = string.Empty;
            CollisionSearchText = string.Empty;
            VertexFilterSearchText = string.Empty;
            SelectedIsoFile = null;
            SelectedTrainer = null;
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            SelectedMove = null;
            SelectedMoveDetail = null;
            SelectedItem = null;
            SelectedItemDetail = null;
            SelectedGiftPokemon = null;
            SelectedGiftPokemonDetail = null;
            SelectedType = null;
            SelectedTypeDetail = null;
            SelectedTreasure = null;
            SelectedTreasureDetail = null;
            SelectedInteraction = null;
            SelectedInteractionDetail = null;
            SelectedMessageTable = null;
            SelectedMessageString = null;
            SelectedTableEditorEntry = null;
            SelectedPatch = null;
            SelectedCollisionFile = null;
            SelectedCollisionData = null;
            SelectedCollisionInteraction = null;
            SelectedCollisionSection = null;
            SelectedVertexFilterFile = null;
            SelectedVertexFilter = VertexFilterOptions.FirstOrDefault();
            PatchStatus = "Select a patch to apply it.";
            CollisionStatus = "Extract ISO files, then select a collision file.";
            VertexFilterStatus = "Extract and decode texture DAT files before using vertex filters.";
            ResetRandomizerDefaults();
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            SelectedTrainerPokemon = [];
            RefreshSelectedToolView(SelectedTool);
            Logs.Add($"Error: {ex.Message}");
            LogPerformance("Open failed total", openTimer);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenXdPathAsync(string path, Stopwatch openTimer)
    {
        var contextTimer = Stopwatch.StartNew();
        var context = await Task.Run(() => XdProjectContext.Open(path));
        LogPerformance("Open XD project context", contextTimer);

        var resetTimer = Stopwatch.StartNew();
        ApplyGameMode(GameCubeGame.PokemonXD);
        CurrentProject = null;
        CurrentXdProject = context;
        HasProject = true;
        ProjectTitle = BuildProjectTitle(context);
        WorkspaceStatus = BuildWorkspaceStatus(context);
        _allIsoFiles.Clear();
        _allTrainers.Clear();
        _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
        _allPokemonStats.Clear();
        _pokemonStatsResources = PokemonStatsEditorResources.Empty;
        _allMoves.Clear();
        _moveEditorResources = MoveEditorResources.Empty;
        _allItems.Clear();
        _itemEditorResources = ItemEditorResources.Empty;
        _allGiftPokemon.Clear();
        _giftPokemonResources = GiftPokemonEditorResources.Empty;
        _allTypes.Clear();
        _allTreasures.Clear();
        _treasureEditorResources = TreasureEditorResources.Empty;
        _allInteractions.Clear();
        _interactionEditorResources = InteractionEditorResources.Empty;
        _allMessageStrings.Clear();
        _allTableEditorEntries.Clear();
        _allPatches.Clear();
        _allCollisionFiles.Clear();
        _allVertexFilterFiles.Clear();
        Trainers.Clear();
        PokemonStatsEntries.Clear();
        MoveEntries.Clear();
        ItemEntries.Clear();
        GiftPokemonEntries.Clear();
        TypeEntries.Clear();
        TreasureEntries.Clear();
        InteractionEntries.Clear();
        MessageTables.Clear();
        MessageStrings.Clear();
        TableEditorEntries.Clear();
        PatchEntries.Clear();
        CollisionFiles.Clear();
        VertexFilterFiles.Clear();
        CollisionInteractionOptions.Clear();
        CollisionSectionOptions.Clear();
        TrainerSearchText = string.Empty;
        PokemonStatsSearchText = string.Empty;
        MoveSearchText = string.Empty;
        ItemSearchText = string.Empty;
        GiftPokemonSearchText = string.Empty;
        TypeSearchText = string.Empty;
        TreasureSearchText = string.Empty;
        MessageSearchText = string.Empty;
        IsoSearchText = string.Empty;
        TableEditorSearchText = string.Empty;
        CollisionSearchText = string.Empty;
        VertexFilterSearchText = string.Empty;
        SelectedIsoFile = null;
        SelectedTrainer = null;
        SelectedPokemonStats = null;
        SelectedPokemonStatsDetail = null;
        SelectedMove = null;
        SelectedMoveDetail = null;
        SelectedItem = null;
        SelectedItemDetail = null;
        SelectedGiftPokemon = null;
        SelectedGiftPokemonDetail = null;
        SelectedType = null;
        SelectedTypeDetail = null;
        SelectedTreasure = null;
        SelectedTreasureDetail = null;
        SelectedInteraction = null;
        SelectedInteractionDetail = null;
        SelectedMessageTable = null;
        SelectedMessageString = null;
        SelectedTableEditorEntry = null;
        SelectedPatch = null;
        SelectedCollisionFile = null;
        SelectedCollisionData = null;
        SelectedCollisionInteraction = null;
        SelectedCollisionSection = null;
        SelectedVertexFilterFile = null;
        SelectedVertexFilter = VertexFilterOptions.FirstOrDefault();
        PatchStatus = "Select a patch to apply it.";
        CollisionStatus = "Extract ISO files, then select a collision file.";
        VertexFilterStatus = "Extract and decode texture DAT files before using vertex filters.";
        ResetRandomizerDefaults();
        SelectedMessageIdText = string.Empty;
        SelectedMessageText = string.Empty;
        SelectedTrainerPokemon = [];
        LogPerformance("Open XD reset UI state", resetTimer);
        PopulateIsoFiles(context.Iso);

        var selectedToolTimer = Stopwatch.StartNew();
        RefreshSelectedToolView(SelectedTool);
        LogPerformance("Open XD refresh selected tool", selectedToolTimer);
        Logs.Add(BuildLogSummary(context));
        LogPerformance("Open XD total", openTimer);
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task ExportAndDecodeSelectedIsoFileAsync()
    {
        await ExportSelectedIsoFileAsync(extractFsysContents: true, decode: true, label: "Export and decode");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task QuickExportSelectedIsoFileAsync()
    {
        await ExportSelectedIsoFileAsync(extractFsysContents: true, decode: false, label: "Export");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task DecodeSelectedIsoFileAsync()
    {
        await ExportSelectedIsoFileAsync(extractFsysContents: false, decode: true, label: "Decode only");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task EncodeAndImportSelectedIsoFileAsync()
    {
        await ImportSelectedIsoFileAsync(encode: true, label: "Encode and import");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task ImportSelectedIsoFileAsync()
    {
        await ImportSelectedIsoFileAsync(encode: false, label: "Import");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task EncodeSelectedIsoFileAsync()
    {
        if (CurrentProject?.Iso is null || SelectedIsoFile is null)
        {
            return;
        }

        IsBusy = true;
        var selectedFile = SelectedIsoFile;
        Logs.Add($"Encode: {selectedFile.Name}");

        try
        {
            var result = await Task.Run(() => CurrentProject.EncodeIsoFile(selectedFile.Entry));
            IsoExplorerStatus = BuildIsoEncodeStatus(selectedFile.Name, result);
            Logs.Add(IsoExplorerStatus);
            RefreshSelectedIsoFileDetails(selectedFile);
        }
        catch (Exception ex)
        {
            IsoExplorerStatus = ex.Message;
            Logs.Add($"Encode failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private async Task DeleteSelectedIsoFileAsync()
    {
        if (CurrentProject?.Iso is null || SelectedIsoFile is null)
        {
            return;
        }

        IsBusy = true;
        var fileName = SelectedIsoFile.Name;
        var selectedEntry = SelectedIsoFile.Entry;
        Logs.Add($"Delete: {fileName}");

        try
        {
            var result = await Task.Run(() => CurrentProject.DeleteIsoFile(selectedEntry));
            IsoExplorerStatus = BuildIsoDeleteStatus(result);
            Logs.Add(IsoExplorerStatus);
            RepopulateIsoFiles(fileName);
        }
        catch (Exception ex)
        {
            IsoExplorerStatus = ex.Message;
            Logs.Add($"Delete failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AddFileToSelectedFsysAsync(string sourcePath, string identifierText)
    {
        if (CurrentProject?.Iso is null || SelectedIsoFile is null)
        {
            return;
        }

        if (!TryParseFsysIdentifier(identifierText, out var identifier))
        {
            IsoExplorerStatus = "File identifier must be a unique number between 1-4 hexadecimal digits.";
            Logs.Add($"Add file failed: {IsoExplorerStatus}");
            return;
        }

        IsBusy = true;
        var fileName = SelectedIsoFile.Name;
        var selectedEntry = SelectedIsoFile.Entry;
        Logs.Add($"Add file to {fileName}: {Path.GetFileName(sourcePath)} as 0x{identifier:x4}");

        try
        {
            var result = await Task.Run(() => CurrentProject.AddFileToIsoFsys(selectedEntry, sourcePath, identifier));
            IsoExplorerStatus = $"Added {result.EntryName} (0x{result.ShortIdentifier:x4}) to {fileName}; wrote {result.ImportResult.WrittenBytes:N0} bytes.";
            Logs.Add(IsoExplorerStatus);
            RepopulateIsoFiles(fileName);
        }
        catch (Exception ex)
        {
            IsoExplorerStatus = ex.Message;
            Logs.Add($"Add file failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveTrainer))]
    private async Task SaveTrainerAsync()
    {
        if (CurrentProject is null || SelectedTrainer is null || SelectedTrainerPokemon.Count == 0)
        {
            return;
        }

        IsBusy = true;
        Logs.Add($"Saving trainer {SelectedTrainer.Trainer.Index}: {SelectedTrainer.Trainer.FullName}");

        try
        {
            var updates = SelectedTrainerPokemon.Select(pokemon => pokemon.ToUpdate()).ToArray();
            var path = await Task.Run(() => CurrentProject.SaveTrainerPokemon(updates));
            foreach (var pokemon in SelectedTrainerPokemon)
            {
                pokemon.MarkSaved();
            }

            Logs.Add($"Trainer Pokemon saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Trainer save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSavePokemonStats))]
    private async Task SavePokemonStatsAsync()
    {
        if (CurrentProject is null || SelectedPokemonStatsDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedPokemonStatsDetail.Stats.Index;
        Logs.Add($"Saving Pokemon stats {index}: {SelectedPokemonStatsDetail.Name}");

        try
        {
            var update = SelectedPokemonStatsDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SavePokemonStats(update));
            RefreshSavedPokemonStatsEntry(index);
            Logs.Add($"Pokemon stats saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Pokemon stats save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveMove))]
    private async Task SaveMoveAsync()
    {
        if (CurrentProject is null || SelectedMoveDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedMoveDetail.Move.Index;
        Logs.Add($"Saving move {index}: {SelectedMoveDetail.Name}");

        try
        {
            var update = SelectedMoveDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveMove(update));
            RefreshSavedMoveEntry(index);
            Logs.Add($"Move saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Move save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveItem))]
    private async Task SaveItemAsync()
    {
        if (CurrentProject is null || SelectedItemDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedItemDetail.Item.Index;
        Logs.Add($"Saving item {index}: {SelectedItemDetail.Name}");

        try
        {
            var update = SelectedItemDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveItem(update));
            RefreshSavedItemEntry(index);
            Logs.Add($"Item saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Item save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveGiftPokemon))]
    private async Task SaveGiftPokemonAsync()
    {
        if (CurrentProject is null || SelectedGiftPokemonDetail is null)
        {
            return;
        }

        IsBusy = true;
        var rowId = SelectedGiftPokemonDetail.Gift.RowId;
        Logs.Add($"Saving gift Pokemon {rowId}: {SelectedGiftPokemonDetail.Name}");

        try
        {
            var update = SelectedGiftPokemonDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveGiftPokemon(update));
            RefreshSavedGiftPokemonEntry(rowId);
            Logs.Add($"Gift Pokemon saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Gift Pokemon save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveType))]
    private async Task SaveTypeAsync()
    {
        if (CurrentProject is null || SelectedTypeDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedTypeDetail.Type.Index;
        Logs.Add($"Saving type {index}: {SelectedTypeDetail.Name}");

        try
        {
            var update = SelectedTypeDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveType(update));
            RefreshSavedTypeEntry(index);
            Logs.Add($"Type saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Type save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveTreasure))]
    private async Task SaveTreasureAsync()
    {
        if (CurrentProject is null || SelectedTreasureDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedTreasureDetail.Treasure.Index;
        Logs.Add($"Saving treasure {index}: {SelectedTreasureDetail.Name}");

        try
        {
            var update = SelectedTreasureDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveTreasure(update));
            RefreshSavedTreasureEntry(index);
            Logs.Add($"Treasure saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Treasure save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveInteraction))]
    private async Task SaveInteractionAsync()
    {
        if (CurrentProject is null || SelectedInteractionDetail is null)
        {
            return;
        }

        IsBusy = true;
        var index = SelectedInteractionDetail.Interaction.Index;
        Logs.Add($"Saving interaction point {index}");

        try
        {
            var update = SelectedInteractionDetail.ToUpdate();
            var path = await Task.Run(() => CurrentProject.SaveInteractionPoint(update));
            RefreshSavedInteractionEntry(index);
            Logs.Add($"Interaction point saved to {path}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Interaction save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveMessage))]
    private async Task SaveMessageAsync()
    {
        if (CurrentProject is null
            || SelectedMessageTable is null
            || !TryParseMessageId(SelectedMessageIdText, out var messageId))
        {
            return;
        }

        IsBusy = true;
        Logs.Add($"Saving message {messageId:X} in {SelectedMessageTable.Table.DisplayName}");

        try
        {
            var updated = await Task.Run(() => CurrentProject.SaveMessageString(
                SelectedMessageTable.Table,
                messageId,
                SelectedMessageText));
            SelectedMessageTable.ReplaceTable(updated);
            LoadMessageStrings(SelectedMessageTable);
            SelectedMessageString = MessageStrings.FirstOrDefault(entry => entry.Message.Id == messageId);
            Logs.Add($"Message saved: {updated.DisplayName} id 0x{messageId:X}");
        }
        catch (Exception ex)
        {
            Logs.Add($"Message save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private async Task EncodeTableEditorAsync()
    {
        await RunTableEditorActionAsync("Encode for editing via text files", definition => CurrentProject!.EncodeRawTable(definition));
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private async Task DecodeTableEditorAsync()
    {
        await RunTableEditorActionAsync("Decode edited files back into the game", definition => CurrentProject!.DecodeRawTable(definition));
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private async Task DocumentTableEditorAsync()
    {
        await RunTableEditorActionAsync("Document as text files for reference", definition => CurrentProject!.DocumentRawTable(definition));
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private void EditTableEditor()
    {
        LogTableEditorAction("Edit");
    }

    [RelayCommand(CanExecute = nameof(CanApplyPatch))]
    private async Task ApplyPatchAsync(PatchEntryViewModel? patch)
    {
        if (patch is null || CurrentProject is null)
        {
            return;
        }

        SelectedPatch = patch;
        PatchStatus = $"Applying: {patch.Name}";
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => CurrentProject.ApplyPatch(patch.Definition.Kind));
            PatchStatus = "Patch completed. Rebuild the ISO after exporting/importing changed files.";
            Logs.Add($"Patch applied: {result.Patch.Name}");
            foreach (var message in result.Messages)
            {
                Logs.Add(message);
            }

            foreach (var file in result.WrittenFiles)
            {
                Logs.Add($"Wrote {file}");
            }
        }
        catch (Exception ex)
        {
            PatchStatus = ex.Message;
            Logs.Add($"Patch failed: {patch.Name} - {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunRandomizer))]
    private async Task RunRandomizerAsync()
    {
        if (CurrentProject is null)
        {
            return;
        }

        var options = new ColosseumRandomizerOptions(
            RandomizerStarterPokemon,
            RandomizerShadowPokemon,
            RandomizerNpcPokemon,
            RandomizerPokemonMoves,
            RandomizerPokemonTypes,
            RandomizerPokemonAbilities,
            RandomizerPokemonStats,
            RandomizerPokemonEvolutions,
            RandomizerMoveTypes,
            RandomizerTypeMatchups,
            RandomizerTmMoves,
            RandomizerItemBoxes,
            RandomizerShopItems,
            RandomizerSimilarBaseStatTotal,
            RandomizerRemoveItemOrTradeEvolutions);

        IsBusy = true;
        RandomizerStatus = "Randomisation starting.";
        Logs.Add("Randomisation starting.");

        try
        {
            var result = await Task.Run(() => CurrentProject.Randomize(options));
            RandomizerStatus = "Randomisation complete.";
            foreach (var message in result.Messages)
            {
                Logs.Add(message);
            }

            foreach (var file in result.WrittenFiles)
            {
                Logs.Add($"Wrote {file}");
            }

            RefreshLoadedEditorRowsAfterRandomizer();
        }
        catch (Exception ex)
        {
            RandomizerStatus = ex.Message;
            Logs.Add($"Randomizer failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RunRandomizerCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanUseVertexFilter))]
    private async Task SaveVertexFilterAsync()
    {
        if (CurrentProject is null || SelectedVertexFilterFile is null)
        {
            return;
        }

        var selectedFile = SelectedVertexFilterFile;
        var filter = (ColosseumVertexColorFilter)SelectedVertexFilterValue;
        IsBusy = true;
        VertexFilterStatus = $"Applying {ColosseumDatVertexColorModel.FilterName(filter)} to {selectedFile.File.FileName}.";

        try
        {
            var result = await Task.Run(() => CurrentProject.ApplyVertexFilter(selectedFile.File, filter));
            VertexFilterStatus = $"{result.FilterName} saved to {result.FileName}: {result.ColorCount} vertex colours updated.";
            Logs.Add(VertexFilterStatus);
            OnPropertyChanged(nameof(SelectedVertexFilterFilePath));
            OnPropertyChanged(nameof(SelectedVertexFilterValue));
        }
        catch (Exception ex)
        {
            VertexFilterStatus = ex.Message;
            Logs.Add($"Vertex filter save failed: {selectedFile.File.FileName} - {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            SaveVertexFilterCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void ReturnHome()
    {
        SelectedTool = Tools.FirstOrDefault();
        ShowIsoExplorer = false;
        ShowTrainerEditor = false;
        ShowPokemonStatsEditor = false;
        ShowReturnHome = false;
        ShowHomeTools = true;
        ShowToolPlaceholder = true;
        LeftPanelTitle = "Tools";
        ShowLeftPanelTitle = true;
        LeftPanelWidth = new GridLength(220);
        WorkspacePanelHeight = GridLength.Auto;
        LogPanelHeight = new GridLength(150);
    }

    private async Task ExportSelectedIsoFileAsync(bool extractFsysContents, bool decode, string label)
    {
        if (CurrentProject?.Iso is null || SelectedIsoFile is null)
        {
            return;
        }

        IsBusy = true;
        var fileName = SelectedIsoFile.Name;
        Logs.Add($"{label}: {fileName}");

        try
        {
            var selectedFile = SelectedIsoFile;
            var result = await Task.Run(() => CurrentProject.ExportIsoFile(
                selectedFile.Entry,
                extractFsysContents,
                decode));
            IsoExplorerStatus = BuildIsoExportStatus(fileName, result);
            Logs.Add(IsoExplorerStatus);
            RefreshSelectedIsoFileDetails(selectedFile);
        }
        catch (Exception ex)
        {
            IsoExplorerStatus = ex.Message;
            Logs.Add($"Export failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportSelectedIsoFileAsync(bool encode, string label)
    {
        if (CurrentProject?.Iso is null || SelectedIsoFile is null)
        {
            return;
        }

        IsBusy = true;
        var fileName = SelectedIsoFile.Name;
        var selectedEntry = SelectedIsoFile.Entry;
        Logs.Add($"{label}: {fileName}");

        try
        {
            var result = await Task.Run(() => CurrentProject.ImportIsoFile(selectedEntry, encode));
            IsoExplorerStatus = BuildIsoImportStatus(fileName, result);
            Logs.Add(IsoExplorerStatus);
            RepopulateIsoFiles(fileName);
        }
        catch (Exception ex)
        {
            IsoExplorerStatus = ex.Message;
            Logs.Add($"{label} failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExportSelectedIsoFile()
        => CurrentProject?.Iso is not null && SelectedIsoFile is not null && !IsBusy;

    private bool CanSaveTrainer()
        => CurrentProject?.Iso is not null && SelectedTrainer is not null && !IsBusy;

    private bool CanSavePokemonStats()
        => CurrentProject?.Iso is not null && SelectedPokemonStatsDetail is not null && !IsBusy;

    private bool CanSaveMove()
        => CurrentProject?.Iso is not null && SelectedMoveDetail is not null && !IsBusy;

    private bool CanSaveItem()
        => CurrentProject?.Iso is not null && SelectedItemDetail is not null && !IsBusy;

    private bool CanSaveGiftPokemon()
        => CurrentProject?.Iso is not null && SelectedGiftPokemonDetail is not null && !IsBusy;

    private bool CanSaveType()
        => CurrentProject?.Iso is not null && SelectedTypeDetail is not null && !IsBusy;

    private bool CanSaveTreasure()
        => CurrentProject?.Iso is not null && SelectedTreasureDetail is not null && !IsBusy;

    private bool CanSaveInteraction()
        => CurrentProject?.Iso is not null && SelectedInteractionDetail is not null && !IsBusy;

    private bool CanSaveMessage()
        => CurrentProject is not null
            && SelectedMessageTable is not null
            && SelectedMessageString is not null
            && TryParseMessageId(SelectedMessageIdText, out _)
            && !IsBusy;

    private bool CanRunTableEditorAction()
        => SelectedTableEditorEntry?.RawDefinition is not null && CurrentProject is not null && !IsBusy;

    private bool CanApplyPatch(PatchEntryViewModel? patch)
        => patch is not null && CurrentProject?.Iso is not null && !IsBusy;

    private bool CanRunRandomizer()
        => CurrentProject?.Iso is not null && !IsBusy;

    private bool CanUseVertexFilter()
        => CurrentProject is not null && SelectedVertexFilterFile is not null && !IsBusy;

    private void LogTableEditorAction(string action)
    {
        if (SelectedTableEditorEntry is null)
        {
            return;
        }

        Logs.Add($"{action}: {SelectedTableEditorEntry.Name} writes editable text files with named fields where the legacy schema is known; unsupported tables preserve raw bytes.");
    }

    private async Task RunTableEditorActionAsync(
        string action,
        Func<ColosseumRawTableDefinition, ColosseumRawTableActionResult> operation)
    {
        if (SelectedTableEditorEntry?.RawDefinition is null || CurrentProject is null)
        {
            return;
        }

        var table = SelectedTableEditorEntry;
        Logs.Add($"{action}: {table.Name}");
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => operation(table.RawDefinition));
            Logs.Add($"{result.Message} Wrote {result.FilePath}");
        }
        catch (Exception ex)
        {
            Logs.Add($"{action} failed for {table.Name}: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetRandomizerDefaults()
    {
        RandomizerStatus = "Select randomizer options.";
        RandomizerStarterPokemon = true;
        RandomizerShadowPokemon = true;
        RandomizerNpcPokemon = true;
        RandomizerPokemonMoves = false;
        RandomizerPokemonTypes = false;
        RandomizerPokemonAbilities = false;
        RandomizerPokemonStats = false;
        RandomizerPokemonEvolutions = false;
        RandomizerMoveTypes = false;
        RandomizerTypeMatchups = false;
        RandomizerTmMoves = false;
        RandomizerItemBoxes = false;
        RandomizerShopItems = false;
        RandomizerSimilarBaseStatTotal = false;
        RandomizerRemoveItemOrTradeEvolutions = false;
    }

    private static IReadOnlyList<PickerOptionViewModel> BuildVertexFilterOptions()
        =>
        [
            new PickerOptionViewModel(0, "None"),
            new PickerOptionViewModel(1, "Minor Red Shift"),
            new PickerOptionViewModel(2, "Red Scale"),
            new PickerOptionViewModel(3, "Primary Shift"),
            new PickerOptionViewModel(4, "Reverse Primary Shift")
        ];

    private void RefreshLoadedEditorRowsAfterRandomizer()
    {
        _allTrainers.Clear();
        _allPokemonStats.Clear();
        _allMoves.Clear();
        _allItems.Clear();
        _allGiftPokemon.Clear();
        _allTypes.Clear();
        _allTreasures.Clear();
        Trainers.Clear();
        PokemonStatsEntries.Clear();
        MoveEntries.Clear();
        ItemEntries.Clear();
        GiftPokemonEntries.Clear();
        TypeEntries.Clear();
        TreasureEntries.Clear();
        SelectedTrainer = null;
        SelectedPokemonStats = null;
        SelectedPokemonStatsDetail = null;
        SelectedMove = null;
        SelectedMoveDetail = null;
        SelectedItem = null;
        SelectedItemDetail = null;
        SelectedGiftPokemon = null;
        SelectedGiftPokemonDetail = null;
        SelectedType = null;
        SelectedTypeDetail = null;
        SelectedTreasure = null;
        SelectedTreasureDetail = null;
        SelectedTrainerPokemon = [];
    }

    public bool PrepareToolWindow(ToolEntryViewModel tool)
    {
        SelectedTool = tool;
        if (CurrentGame == GameCubeGame.PokemonXD)
        {
            if (CurrentXdProject?.Iso is null)
            {
                Logs.Add($"Open a Pokemon XD ISO before launching {tool.Title}.");
                return false;
            }

            if (tool.Game != GameCubeGame.PokemonXD)
            {
                Logs.Add($"Open a {tool.LegacyToolName} ISO before launching {tool.Title}.");
                return false;
            }

            if (tool.Title != "ISO Explorer")
            {
                SelectedToolDetail =
                    $"{tool.Title}\nLegacy segue: {tool.LegacySegue}\nReference: {tool.LegacySource}\nXD editor backend parity is the next porting pass.";
            }

            return true;
        }

        if (CurrentProject?.Iso is null && !(tool.Title == "Message Editor" && CurrentProject?.MessageTable is not null))
        {
            Logs.Add($"Open a Colosseum ISO before launching {tool.Title}.");
            return false;
        }

        switch (tool.Title)
        {
            case "Trainer Editor":
                LoadTrainerRows();
                return true;
            case "Pokemon Stats Editor":
                LoadPokemonStatsRows();
                return true;
            case "Move Editor":
                LoadMoveRows();
                return true;
            case "Item Editor":
                LoadItemRows();
                return true;
            case "Gift Pokemon Editor":
                LoadGiftPokemonRows();
                return true;
            case "Type Editor":
                LoadTypeRows();
                return true;
            case "Treasure Editor":
                LoadTreasureRows();
                return true;
            case "Patches":
                LoadPatchRows();
                return true;
            case "Randomizer":
                RandomizerStatus = "Select randomizer options.";
                RunRandomizerCommand.NotifyCanExecuteChanged();
                return true;
            case "Interaction Editor":
                LoadInteractionRows();
                return true;
            case "Message Editor":
                LoadMessageRows();
                return true;
            case "Collision Viewer":
                LoadCollisionRows();
                return true;
            case "Vertex Filters":
                LoadVertexFilterRows();
                return true;
            case "Table Editor":
                LoadTableEditorRows();
                return true;
            default:
                return true;
        }
    }

    partial void OnSelectedToolChanged(ToolEntryViewModel? value)
    {
        RefreshSelectedToolView(value);
    }

    partial void OnSelectedIsoFileChanged(IsoFileEntryViewModel? value)
    {
        IsoExplorerStatus = value is null
            ? "Select an ISO file to inspect or export."
            : $"{value.Name} - {value.SizeText} at {value.OffsetHex}";
        foreach (var file in _allIsoFiles)
        {
            file.IsSelected = ReferenceEquals(file, value);
        }

        RefreshSelectedIsoFileDetails(value);
        NotifyIsoExplorerCommands();
    }

    partial void OnSelectedTrainerChanged(TrainerEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        if (!ReferenceEquals(_lastSelectedTrainer, value))
        {
            if (_lastSelectedTrainer is not null)
            {
                _lastSelectedTrainer.IsSelected = false;
            }

            if (value is not null)
            {
                value.IsSelected = true;
            }

            _lastSelectedTrainer = value;
        }

        RefreshSelectedTrainerDetails(value);
        if (value is not null)
        {
            LogPerformance("Trainer Editor selected trainer detail", timer);
        }

        SaveTrainerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPokemonStatsChanged(PokemonStatsEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var pokemon in _allPokemonStats)
        {
            pokemon.IsSelected = ReferenceEquals(pokemon, value);
        }

        SelectedPokemonStatsDetail = value is null
            ? null
            : new PokemonStatsEditorViewModel(value.Stats, _pokemonStatsResources, value.FaceImage, OnPokemonStatsChanged);
        if (value is not null)
        {
            LogPerformance("Pokemon Stats Editor selected detail", timer);
        }

        SavePokemonStatsCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMoveChanged(MoveEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var move in _allMoves)
        {
            move.IsSelected = ReferenceEquals(move, value);
        }

        SelectedMoveDetail = value is null
            ? null
            : new MoveEditorViewModel(value.Move, _moveEditorResources, OnMoveChanged);
        if (value is not null)
        {
            LogPerformance("Move Editor selected detail", timer);
        }

        SaveMoveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedItemChanged(ItemEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var item in _allItems)
        {
            item.IsSelected = ReferenceEquals(item, value);
        }

        SelectedItemDetail = value is null
            ? null
            : new ItemEditorViewModel(value.Item, _itemEditorResources, OnItemChanged);
        if (value is not null)
        {
            LogPerformance("Item Editor selected detail", timer);
        }

        SaveItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGiftPokemonChanged(GiftPokemonEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var gift in _allGiftPokemon)
        {
            gift.IsSelected = ReferenceEquals(gift, value);
        }

        SelectedGiftPokemonDetail = value is null
            ? null
            : new GiftPokemonEditorViewModel(value.Gift, _giftPokemonResources, value.FaceImage, OnGiftPokemonChanged);
        if (value is not null)
        {
            LogPerformance("Gift Pokemon Editor selected detail", timer);
        }

        SaveGiftPokemonCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTypeChanged(TypeEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var type in _allTypes)
        {
            type.IsSelected = ReferenceEquals(type, value);
        }

        SelectedTypeDetail = value is null
            ? null
            : new TypeEditorViewModel(value.Type, _allTypes.Select(entry => entry.Type).ToArray(), OnTypeChanged);
        if (value is not null)
        {
            LogPerformance("Type Editor selected detail", timer);
        }

        SaveTypeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTreasureChanged(TreasureEntryViewModel? value)
    {
        var timer = Stopwatch.StartNew();
        foreach (var treasure in _allTreasures)
        {
            treasure.IsSelected = ReferenceEquals(treasure, value);
        }

        SelectedTreasureDetail = value is null
            ? null
            : new TreasureEditorViewModel(value.Treasure, _treasureEditorResources, OnTreasureChanged);
        if (value is not null)
        {
            LogPerformance("Treasure Editor selected detail", timer);
        }

        SaveTreasureCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedInteractionChanged(InteractionEntryViewModel? value)
    {
        foreach (var interaction in _allInteractions)
        {
            interaction.IsSelected = ReferenceEquals(interaction, value);
        }

        SelectedInteractionDetail = value is null
            ? null
            : new InteractionEditorViewModel(value.Interaction, _interactionEditorResources, OnInteractionChanged);
        SaveInteractionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMessageTableChanged(MessageTableViewModel? value)
    {
        LoadMessageStrings(value);
        SaveMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMessageStringChanged(MessageStringEntryViewModel? value)
    {
        foreach (var message in _allMessageStrings)
        {
            message.IsSelected = ReferenceEquals(message, value);
        }

        SelectedMessageIdText = value?.Message.IdHex ?? string.Empty;
        SelectedMessageText = value?.Message.Text ?? string.Empty;
        SaveMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTableEditorEntryChanged(TableEditorEntryViewModel? value)
    {
        foreach (var table in _allTableEditorEntries)
        {
            table.IsSelected = ReferenceEquals(table, value);
        }

        EncodeTableEditorCommand.NotifyCanExecuteChanged();
        DecodeTableEditorCommand.NotifyCanExecuteChanged();
        DocumentTableEditorCommand.NotifyCanExecuteChanged();
        EditTableEditorCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPatchChanged(PatchEntryViewModel? value)
    {
        foreach (var patch in _allPatches)
        {
            patch.IsSelected = ReferenceEquals(patch, value);
        }

        ApplyPatchCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCollisionFileChanged(CollisionFileEntryViewModel? value)
    {
        foreach (var file in _allCollisionFiles)
        {
            file.IsSelected = ReferenceEquals(file, value);
        }

        LoadSelectedCollisionData(value);
    }

    partial void OnSelectedVertexFilterFileChanged(VertexFilterFileEntryViewModel? value)
    {
        foreach (var file in _allVertexFilterFiles)
        {
            file.IsSelected = ReferenceEquals(file, value);
        }

        VertexFilterStatus = value is null
            ? "No images to import. Export and decode some texture files from the ISO."
            : value.File.FileName;
        SaveVertexFilterCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMessageIdTextChanged(string value)
    {
        SaveMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMessageTextChanged(string value)
    {
        SaveMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnTrainerSearchTextChanged(string value)
    {
        ApplyTrainerFilter(value);
    }

    partial void OnPokemonStatsSearchTextChanged(string value)
    {
        ApplyPokemonStatsFilter(value);
    }

    partial void OnMoveSearchTextChanged(string value)
    {
        ApplyMoveFilter(value);
    }

    partial void OnItemSearchTextChanged(string value)
    {
        ApplyItemFilter(value);
    }

    partial void OnGiftPokemonSearchTextChanged(string value)
    {
        ApplyGiftPokemonFilter(value);
    }

    partial void OnTypeSearchTextChanged(string value)
    {
        ApplyTypeFilter(value);
    }

    partial void OnTreasureSearchTextChanged(string value)
    {
        ApplyTreasureFilter(value);
    }

    partial void OnMessageSearchTextChanged(string value)
    {
        ApplyMessageFilter(value);
    }

    partial void OnIsoSearchTextChanged(string value)
    {
        ApplyIsoFileFilter(value);
    }

    partial void OnTableEditorSearchTextChanged(string value)
    {
        ApplyTableEditorFilter(value);
    }

    partial void OnCollisionSearchTextChanged(string value)
    {
        ApplyCollisionFilter(value);
    }

    partial void OnVertexFilterSearchTextChanged(string value)
    {
        ApplyVertexFilter(value);
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyIsoExplorerCommands();
        SaveTrainerCommand.NotifyCanExecuteChanged();
        SavePokemonStatsCommand.NotifyCanExecuteChanged();
        SaveMoveCommand.NotifyCanExecuteChanged();
        SaveItemCommand.NotifyCanExecuteChanged();
        SaveGiftPokemonCommand.NotifyCanExecuteChanged();
        SaveTypeCommand.NotifyCanExecuteChanged();
        SaveTreasureCommand.NotifyCanExecuteChanged();
        SaveInteractionCommand.NotifyCanExecuteChanged();
        SaveMessageCommand.NotifyCanExecuteChanged();
        EncodeTableEditorCommand.NotifyCanExecuteChanged();
        DecodeTableEditorCommand.NotifyCanExecuteChanged();
        DocumentTableEditorCommand.NotifyCanExecuteChanged();
        EditTableEditorCommand.NotifyCanExecuteChanged();
        ApplyPatchCommand.NotifyCanExecuteChanged();
        RunRandomizerCommand.NotifyCanExecuteChanged();
        SaveVertexFilterCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSelectedToolView(ToolEntryViewModel? value)
    {
        foreach (var tool in Tools)
        {
            tool.IsSelected = ReferenceEquals(tool, value);
        }

        if (value is null)
        {
            SelectedToolDetail = string.Empty;
            ShowIsoExplorer = false;
            ShowTrainerEditor = false;
            ShowPokemonStatsEditor = false;
            ShowReturnHome = false;
            ShowHomeTools = true;
            ShowToolPlaceholder = true;
            LeftPanelTitle = "Tools";
            ShowLeftPanelTitle = true;
            LeftPanelWidth = new GridLength(220);
            WorkspacePanelHeight = GridLength.Auto;
            LogPanelHeight = new GridLength(150);
            return;
        }

        SelectedToolDetail = $"{value.Title}\nLegacy segue: {value.LegacySegue}\nReference: {value.LegacySource}";
        ShowIsoExplorer = false;
        ShowTrainerEditor = false;
        ShowPokemonStatsEditor = false;
        ShowHomeTools = true;
        ShowToolPlaceholder = true;
        ShowReturnHome = false;
        LeftPanelTitle = "Tools";
        ShowLeftPanelTitle = true;
        LeftPanelWidth = new GridLength(220);
        WorkspacePanelHeight = GridLength.Auto;
        LogPanelHeight = new GridLength(150);
    }

    private void PopulateIsoFiles(ColosseumProjectContext context)
        => PopulateIsoFiles(context.Iso);

    private void PopulateIsoFiles(GameCubeIso? iso)
    {
        var totalTimer = Stopwatch.StartNew();
        _allIsoFiles.Clear();
        IsoFiles.Clear();
        if (iso is null)
        {
            SelectedIsoFile = null;
            return;
        }

        var buildTimer = Stopwatch.StartNew();
        foreach (var entry in iso.Files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
        {
            _allIsoFiles.Add(new IsoFileEntryViewModel(entry));
        }
        LogPerformance("ISO Explorer build row cache", buildTimer, _allIsoFiles.Count);

        var filterTimer = Stopwatch.StartNew();
        ApplyIsoFileFilter(IsoSearchText);
        LogPerformance("ISO Explorer apply filter", filterTimer, IsoFiles.Count);
        SelectedIsoFile = IsoFiles.FirstOrDefault(file =>
                string.Equals(Path.GetFileName(file.Name), "Start.dol", StringComparison.OrdinalIgnoreCase))
            ?? IsoFiles.FirstOrDefault();
        NotifyIsoExplorerCommands();
        LogPerformance("ISO Explorer populate total", totalTimer, _allIsoFiles.Count);
    }

    private void RepopulateIsoFiles(string selectedFileName)
    {
        var iso = CurrentProject?.Iso ?? CurrentXdProject?.Iso;
        if (iso is null)
        {
            return;
        }

        PopulateIsoFiles(iso);
        SelectedIsoFile = IsoFiles.FirstOrDefault(file =>
            string.Equals(file.Name, selectedFileName, StringComparison.OrdinalIgnoreCase)) ?? SelectedIsoFile;
    }

    private void LoadTrainerRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allTrainers.Clear();
            _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
            Trainers.Clear();
            SelectedTrainer = null;
            return;
        }

        if (_allTrainers.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyTrainerFilter(TrainerSearchText, selectFirstWhenMissing: false);
            LogPerformance("Trainer Editor cached filter", cachedFilterTimer, Trainers.Count);
            LogPerformance("Trainer Editor cached total", totalTimer, _allTrainers.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Trainer Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _trainerPokemonResources = TrainerPokemonEditorResources.FromCommonRel(commonRel);
            var trainers = commonRel.LoadStoryTrainers();
            LogPerformance("Trainer Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var trainer in trainers)
            {
                _allTrainers.Add(new TrainerEntryViewModel(trainer));
            }
            LogPerformance("Trainer Editor build row cache", rowTimer, _allTrainers.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyTrainerFilter(TrainerSearchText, selectFirstWhenMissing: false);
            LogPerformance("Trainer Editor apply filter", filterTimer, Trainers.Count);
            Logs.Add($"Trainer Editor loaded: {_allTrainers.Count} story trainers.");
            LogPerformance("Trainer Editor load total", totalTimer, _allTrainers.Count);
        }
        catch (Exception ex)
        {
            SelectedTrainer = null;
            SelectedToolDetail = $"Trainer Editor\n{ex.Message}";
            Logs.Add($"Trainer load failed: {ex.Message}");
        }
    }

    private void LoadPokemonStatsRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allPokemonStats.Clear();
            _pokemonStatsResources = PokemonStatsEditorResources.Empty;
            PokemonStatsEntries.Clear();
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            return;
        }

        if (_allPokemonStats.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyPokemonStatsFilter(PokemonStatsSearchText);
            LogPerformance("Pokemon Stats Editor cached filter", cachedFilterTimer, PokemonStatsEntries.Count);
            LogPerformance("Pokemon Stats Editor cached total", totalTimer, _allPokemonStats.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Pokemon Stats Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _pokemonStatsResources = PokemonStatsEditorResources.FromCommonRel(commonRel);
            LogPerformance("Pokemon Stats Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var pokemon in commonRel.PokemonStats)
            {
                _allPokemonStats.Add(new PokemonStatsEntryViewModel(pokemon));
            }
            LogPerformance("Pokemon Stats Editor build row cache", rowTimer, _allPokemonStats.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyPokemonStatsFilter(PokemonStatsSearchText);
            LogPerformance("Pokemon Stats Editor apply filter", filterTimer, PokemonStatsEntries.Count);
            SelectedPokemonStats = PokemonStatsEntries.FirstOrDefault();
            Logs.Add($"Pokemon Stats Editor loaded: {_allPokemonStats.Count} Pokemon.");
            LogPerformance("Pokemon Stats Editor load total", totalTimer, _allPokemonStats.Count);
        }
        catch (Exception ex)
        {
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            SelectedToolDetail = $"Pokemon Stats Editor\n{ex.Message}";
            Logs.Add($"Pokemon stats load failed: {ex.Message}");
        }
    }

    private void LoadMoveRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allMoves.Clear();
            _moveEditorResources = MoveEditorResources.Empty;
            MoveEntries.Clear();
            SelectedMove = null;
            SelectedMoveDetail = null;
            return;
        }

        if (_allMoves.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyMoveFilter(MoveSearchText);
            LogPerformance("Move Editor cached filter", cachedFilterTimer, MoveEntries.Count);
            LogPerformance("Move Editor cached total", totalTimer, _allMoves.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Move Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _moveEditorResources = MoveEditorResources.FromCommonRel(commonRel);
            LogPerformance("Move Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var move in commonRel.Moves)
            {
                _allMoves.Add(new MoveEntryViewModel(move));
            }
            LogPerformance("Move Editor build row cache", rowTimer, _allMoves.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyMoveFilter(MoveSearchText);
            LogPerformance("Move Editor apply filter", filterTimer, MoveEntries.Count);
            SelectedMove = MoveEntries.FirstOrDefault();
            Logs.Add($"Move Editor loaded: {_allMoves.Count} moves.");
            LogPerformance("Move Editor load total", totalTimer, _allMoves.Count);
        }
        catch (Exception ex)
        {
            SelectedMove = null;
            SelectedMoveDetail = null;
            SelectedToolDetail = $"Move Editor\n{ex.Message}";
            Logs.Add($"Move load failed: {ex.Message}");
        }
    }

    private void LoadItemRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allItems.Clear();
            _itemEditorResources = ItemEditorResources.Empty;
            ItemEntries.Clear();
            SelectedItem = null;
            SelectedItemDetail = null;
            return;
        }

        if (_allItems.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyItemFilter(ItemSearchText);
            LogPerformance("Item Editor cached filter", cachedFilterTimer, ItemEntries.Count);
            LogPerformance("Item Editor cached total", totalTimer, _allItems.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Item Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _itemEditorResources = ItemEditorResources.FromCommonRel(commonRel);
            LogPerformance("Item Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var item in commonRel.ItemData)
            {
                _allItems.Add(new ItemEntryViewModel(item));
            }
            LogPerformance("Item Editor build row cache", rowTimer, _allItems.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyItemFilter(ItemSearchText);
            LogPerformance("Item Editor apply filter", filterTimer, ItemEntries.Count);
            SelectedItem = ItemEntries.FirstOrDefault();
            Logs.Add($"Item Editor loaded: {_allItems.Count} items.");
            LogPerformance("Item Editor load total", totalTimer, _allItems.Count);
        }
        catch (Exception ex)
        {
            SelectedItem = null;
            SelectedItemDetail = null;
            SelectedToolDetail = $"Item Editor\n{ex.Message}";
            Logs.Add($"Item load failed: {ex.Message}");
        }
    }

    private void LoadGiftPokemonRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allGiftPokemon.Clear();
            _giftPokemonResources = GiftPokemonEditorResources.Empty;
            GiftPokemonEntries.Clear();
            SelectedGiftPokemon = null;
            SelectedGiftPokemonDetail = null;
            return;
        }

        if (_allGiftPokemon.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyGiftPokemonFilter(GiftPokemonSearchText);
            LogPerformance("Gift Pokemon Editor cached filter", cachedFilterTimer, GiftPokemonEntries.Count);
            LogPerformance("Gift Pokemon Editor cached total", totalTimer, _allGiftPokemon.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Gift Pokemon Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _giftPokemonResources = GiftPokemonEditorResources.FromCommonRel(commonRel);
            LogPerformance("Gift Pokemon Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            var rowIndex = 0;
            foreach (var gift in commonRel.GiftPokemon)
            {
                _allGiftPokemon.Add(new GiftPokemonEntryViewModel(gift, rowIndex++));
            }
            LogPerformance("Gift Pokemon Editor build row cache", rowTimer, _allGiftPokemon.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyGiftPokemonFilter(GiftPokemonSearchText);
            LogPerformance("Gift Pokemon Editor apply filter", filterTimer, GiftPokemonEntries.Count);
            SelectedGiftPokemon = GiftPokemonEntries.FirstOrDefault();
            Logs.Add($"Gift Pokemon Editor loaded: {_allGiftPokemon.Count} gifts.");
            LogPerformance("Gift Pokemon Editor load total", totalTimer, _allGiftPokemon.Count);
        }
        catch (Exception ex)
        {
            SelectedGiftPokemon = null;
            SelectedGiftPokemonDetail = null;
            SelectedToolDetail = $"Gift Pokemon Editor\n{ex.Message}";
            Logs.Add($"Gift Pokemon load failed: {ex.Message}");
        }
    }

    private void LoadTypeRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allTypes.Clear();
            TypeEntries.Clear();
            SelectedType = null;
            SelectedTypeDetail = null;
            return;
        }

        if (_allTypes.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyTypeFilter(TypeSearchText);
            LogPerformance("Type Editor cached filter", cachedFilterTimer, TypeEntries.Count);
            LogPerformance("Type Editor cached total", totalTimer, _allTypes.Count);
            return;
        }

        try
        {
            var typeLoadTimer = Stopwatch.StartNew();
            var types = CurrentProject.LoadTypes();
            LogPerformance("Type Editor load types", typeLoadTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var type in types)
            {
                _allTypes.Add(new TypeEntryViewModel(type));
            }
            LogPerformance("Type Editor build row cache", rowTimer, _allTypes.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyTypeFilter(TypeSearchText);
            LogPerformance("Type Editor apply filter", filterTimer, TypeEntries.Count);
            SelectedType = TypeEntries.FirstOrDefault();
            Logs.Add($"Type Editor loaded: {_allTypes.Count} types.");
            LogPerformance("Type Editor load total", totalTimer, _allTypes.Count);
        }
        catch (Exception ex)
        {
            SelectedType = null;
            SelectedTypeDetail = null;
            SelectedToolDetail = $"Type Editor\n{ex.Message}";
            Logs.Add($"Type load failed: {ex.Message}");
        }
    }

    private void LoadTreasureRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allTreasures.Clear();
            _treasureEditorResources = TreasureEditorResources.Empty;
            TreasureEntries.Clear();
            SelectedTreasure = null;
            SelectedTreasureDetail = null;
            return;
        }

        if (_allTreasures.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyTreasureFilter(TreasureSearchText);
            LogPerformance("Treasure Editor cached filter", cachedFilterTimer, TreasureEntries.Count);
            LogPerformance("Treasure Editor cached total", totalTimer, _allTreasures.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Treasure Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _treasureEditorResources = TreasureEditorResources.FromCommonRel(commonRel);
            LogPerformance("Treasure Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var treasure in commonRel.Treasures)
            {
                _allTreasures.Add(new TreasureEntryViewModel(treasure));
            }
            LogPerformance("Treasure Editor build row cache", rowTimer, _allTreasures.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyTreasureFilter(TreasureSearchText);
            LogPerformance("Treasure Editor apply filter", filterTimer, TreasureEntries.Count);
            SelectedTreasure = TreasureEntries.FirstOrDefault();
            Logs.Add($"Treasure Editor loaded: {_allTreasures.Count} treasure boxes.");
            LogPerformance("Treasure Editor load total", totalTimer, _allTreasures.Count);
        }
        catch (Exception ex)
        {
            SelectedTreasure = null;
            SelectedTreasureDetail = null;
            SelectedToolDetail = $"Treasure Editor\n{ex.Message}";
            Logs.Add($"Treasure load failed: {ex.Message}");
        }
    }

    private void LoadPatchRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allPatches.Clear();
            PatchEntries.Clear();
            SelectedPatch = null;
            PatchStatus = "Open a Colosseum ISO before applying patches.";
            return;
        }

        if (_allPatches.Count == 0)
        {
            var rowTimer = Stopwatch.StartNew();
            foreach (var definition in ColosseumPatchDefinition.ColosseumPatches.Select((patch, index) => new PatchEntryViewModel(patch, index)))
            {
                _allPatches.Add(definition);
            }
            LogPerformance("Patches build row cache", rowTimer, _allPatches.Count);
        }

        var filterTimer = Stopwatch.StartNew();
        PatchEntries.Clear();
        foreach (var patch in _allPatches)
        {
            PatchEntries.Add(patch);
        }
        LogPerformance("Patches apply rows", filterTimer, PatchEntries.Count);

        SelectedPatch ??= PatchEntries.FirstOrDefault();
        PatchStatus = "Click a patch row to apply it to the workspace files.";
        Logs.Add($"Patches loaded: {_allPatches.Count} Colosseum patches.");
        LogPerformance("Patches load total", totalTimer, _allPatches.Count);
        ApplyPatchCommand.NotifyCanExecuteChanged();
    }

    private void LoadCollisionRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allCollisionFiles.Clear();
            CollisionFiles.Clear();
            SelectedCollisionFile = null;
            SelectedCollisionData = null;
            CollisionInteractionOptions.Clear();
            CollisionSectionOptions.Clear();
            CollisionStatus = "Open a Colosseum ISO before launching the Collision Viewer.";
            return;
        }

        try
        {
            var rowTimer = Stopwatch.StartNew();
            _allCollisionFiles.Clear();
            foreach (var file in CurrentProject.LoadCollisionFiles())
            {
                _allCollisionFiles.Add(new CollisionFileEntryViewModel(file));
            }
            LogPerformance("Collision Viewer build row cache", rowTimer, _allCollisionFiles.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyCollisionFilter(CollisionSearchText);
            LogPerformance("Collision Viewer apply filter", filterTimer, CollisionFiles.Count);
            SelectedCollisionFile = CollisionFiles.FirstOrDefault();
            CollisionStatus = _allCollisionFiles.Count == 0
                ? $"No .col files found in {Path.Combine(CurrentProject.WorkspaceDirectory ?? string.Empty, "Game Files")}. Extract ISO files and try again."
                : $"Collision files loaded: {_allCollisionFiles.Count}.";
            Logs.Add(CollisionStatus);
            LogPerformance("Collision Viewer load total", totalTimer, _allCollisionFiles.Count);
        }
        catch (Exception ex)
        {
            _allCollisionFiles.Clear();
            CollisionFiles.Clear();
            SelectedCollisionFile = null;
            SelectedCollisionData = null;
            CollisionInteractionOptions.Clear();
            CollisionSectionOptions.Clear();
            CollisionStatus = ex.Message;
            Logs.Add($"Collision Viewer load failed: {ex.Message}");
        }
    }

    private void LoadVertexFilterRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allVertexFilterFiles.Clear();
            VertexFilterFiles.Clear();
            SelectedVertexFilterFile = null;
            VertexFilterStatus = "Open a Colosseum ISO before launching Vertex Filters.";
            return;
        }

        try
        {
            var rowTimer = Stopwatch.StartNew();
            _allVertexFilterFiles.Clear();
            foreach (var file in CurrentProject.LoadVertexFilterFiles())
            {
                _allVertexFilterFiles.Add(new VertexFilterFileEntryViewModel(file));
            }
            LogPerformance("Vertex Filters build row cache", rowTimer, _allVertexFilterFiles.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyVertexFilter(VertexFilterSearchText);
            LogPerformance("Vertex Filters apply filter", filterTimer, VertexFilterFiles.Count);
            SelectedVertexFilterFile = VertexFilterFiles.FirstOrDefault();
            SelectedVertexFilter = VertexFilterOptions.FirstOrDefault();
            VertexFilterStatus = _allVertexFilterFiles.Count == 0
                ? "No images to import. Export and decode some texture files from the ISO."
                : $"Vertex filter files loaded: {_allVertexFilterFiles.Count}.";
            Logs.Add(VertexFilterStatus);
            LogPerformance("Vertex Filters load total", totalTimer, _allVertexFilterFiles.Count);
            SaveVertexFilterCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _allVertexFilterFiles.Clear();
            VertexFilterFiles.Clear();
            SelectedVertexFilterFile = null;
            VertexFilterStatus = ex.Message;
            Logs.Add($"Vertex Filters load failed: {ex.Message}");
            SaveVertexFilterCommand.NotifyCanExecuteChanged();
        }
    }

    private void LoadInteractionRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allInteractions.Clear();
            _interactionEditorResources = InteractionEditorResources.Empty;
            InteractionEntries.Clear();
            SelectedInteraction = null;
            SelectedInteractionDetail = null;
            return;
        }

        if (_allInteractions.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            InteractionEntries.Clear();
            foreach (var interaction in _allInteractions)
            {
                InteractionEntries.Add(interaction);
            }
            LogPerformance("Interaction Editor cached rows", cachedFilterTimer, InteractionEntries.Count);

            SelectedInteraction ??= InteractionEntries.FirstOrDefault();
            LogPerformance("Interaction Editor cached total", totalTimer, _allInteractions.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Interaction Editor load common.rel", commonTimer);

            var resourceTimer = Stopwatch.StartNew();
            _interactionEditorResources = InteractionEditorResources.FromCommonRel(commonRel);
            LogPerformance("Interaction Editor build resources", resourceTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var interaction in commonRel.InteractionPoints)
            {
                _allInteractions.Add(new InteractionEntryViewModel(interaction));
            }
            LogPerformance("Interaction Editor build row cache", rowTimer, _allInteractions.Count);

            var filterTimer = Stopwatch.StartNew();
            InteractionEntries.Clear();
            foreach (var interaction in _allInteractions)
            {
                InteractionEntries.Add(interaction);
            }
            LogPerformance("Interaction Editor apply rows", filterTimer, InteractionEntries.Count);

            SelectedInteraction = InteractionEntries.FirstOrDefault();
            Logs.Add($"Interaction Editor loaded: {_allInteractions.Count} interaction points.");
            LogPerformance("Interaction Editor load total", totalTimer, _allInteractions.Count);
        }
        catch (Exception ex)
        {
            SelectedInteraction = null;
            SelectedInteractionDetail = null;
            SelectedToolDetail = $"Interaction Editor\n{ex.Message}";
            Logs.Add($"Interaction load failed: {ex.Message}");
        }
    }

    private void LoadMessageRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null && CurrentProject?.MessageTable is null)
        {
            MessageTables.Clear();
            _allMessageStrings.Clear();
            MessageStrings.Clear();
            SelectedMessageTable = null;
            SelectedMessageString = null;
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            return;
        }

        if (MessageTables.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyMessageFilter(MessageSearchText);
            LogPerformance("Message Editor cached filter", cachedFilterTimer, MessageStrings.Count);
            LogPerformance("Message Editor cached total", totalTimer, MessageTables.Count);
            return;
        }

        try
        {
            var tableTimer = Stopwatch.StartNew();
            foreach (var table in CurrentProject.LoadMessageTables())
            {
                MessageTables.Add(new MessageTableViewModel(table));
            }
            LogPerformance("Message Editor load tables", tableTimer, MessageTables.Count);

            SelectedMessageTable = MessageTables.FirstOrDefault();
            Logs.Add($"Message Editor loaded: {MessageTables.Count} message tables.");
            LogPerformance("Message Editor load total", totalTimer, MessageTables.Count);
        }
        catch (Exception ex)
        {
            SelectedMessageTable = null;
            SelectedMessageString = null;
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            SelectedToolDetail = $"Message Editor\n{ex.Message}";
            Logs.Add($"Message load failed: {ex.Message}");
        }
    }

    private void LoadTableEditorRows()
    {
        var totalTimer = Stopwatch.StartNew();
        if (CurrentProject?.Iso is null)
        {
            _allTableEditorEntries.Clear();
            TableEditorEntries.Clear();
            SelectedTableEditorEntry = null;
            return;
        }

        if (_allTableEditorEntries.Count > 0)
        {
            var cachedFilterTimer = Stopwatch.StartNew();
            ApplyTableEditorFilter(TableEditorSearchText);
            LogPerformance("Table Editor cached filter", cachedFilterTimer, TableEditorEntries.Count);
            LogPerformance("Table Editor cached total", totalTimer, _allTableEditorEntries.Count);
            return;
        }

        try
        {
            var commonTimer = Stopwatch.StartNew();
            var commonRel = CurrentProject.LoadCommonRel();
            LogPerformance("Table Editor load common.rel", commonTimer);

            var rowTimer = Stopwatch.StartNew();
            foreach (var table in BuildTableEditorEntries(commonRel, CurrentProject))
            {
                _allTableEditorEntries.Add(table);
            }
            LogPerformance("Table Editor build row cache", rowTimer, _allTableEditorEntries.Count);

            var filterTimer = Stopwatch.StartNew();
            ApplyTableEditorFilter(TableEditorSearchText);
            LogPerformance("Table Editor apply filter", filterTimer, TableEditorEntries.Count);
            SelectedTableEditorEntry = TableEditorEntries.FirstOrDefault();
            Logs.Add($"Table Editor loaded: {_allTableEditorEntries.Count} universal tables.");
            LogPerformance("Table Editor load total", totalTimer, _allTableEditorEntries.Count);
        }
        catch (Exception ex)
        {
            SelectedTableEditorEntry = null;
            SelectedToolDetail = $"Table Editor\n{ex.Message}";
            Logs.Add($"Table Editor load failed: {ex.Message}");
        }
    }

    private void ApplyTrainerFilter(string? filterText, bool selectFirstWhenMissing = true)
    {
        if (_allTrainers.Count == 0)
        {
            Trainers.Clear();
            SelectedTrainer = null;
            return;
        }

        var filterTokens = SimplifySearchTokens(filterText);
        var filtered = filterTokens.Count == 0
            ? _allTrainers
            : _allTrainers.Where(trainer => TrainerMatchesFilter(trainer, filterTokens)).ToList();
        var selectedTrainer = SelectedTrainer;
        var keepsSelectedTrainer = selectedTrainer is not null && filtered.Contains(selectedTrainer);

        Trainers.Clear();
        foreach (var trainer in filtered)
        {
            Trainers.Add(trainer);
        }

        if (!keepsSelectedTrainer)
        {
            SelectedTrainer = selectFirstWhenMissing
                ? Trainers.FirstOrDefault()
                : null;
        }
        else if (selectedTrainer is not null)
        {
            selectedTrainer.IsSelected = true;
        }
    }

    private void ApplyIsoFileFilter(string? filterText)
    {
        if (_allIsoFiles.Count == 0)
        {
            IsoFiles.Clear();
            return;
        }

        var filters = (filterText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SimplifySearchText)
            .Where(filter => filter.Length > 0)
            .ToArray();
        var filtered = filters.Length == 0
            ? _allIsoFiles
            : _allIsoFiles.Where(file => filters.Any(filter => Contains(file.Name, filter))).ToList();

        IsoFiles.Clear();
        foreach (var file in filtered)
        {
            IsoFiles.Add(file);
        }

        if (SelectedIsoFile is null || !IsoFiles.Contains(SelectedIsoFile))
        {
            SelectedIsoFile = IsoFiles.FirstOrDefault();
        }
        else
        {
            foreach (var file in _allIsoFiles)
            {
                file.IsSelected = ReferenceEquals(file, SelectedIsoFile);
            }
        }
    }

    private void ApplyPokemonStatsFilter(string? filterText)
    {
        if (_allPokemonStats.Count == 0)
        {
            PokemonStatsEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allPokemonStats
            : _allPokemonStats.Where(pokemon => PokemonStatsMatchesFilter(pokemon, filter)).ToList();

        PokemonStatsEntries.Clear();
        foreach (var pokemon in filtered)
        {
            PokemonStatsEntries.Add(pokemon);
        }

        if (SelectedPokemonStats is null || !PokemonStatsEntries.Contains(SelectedPokemonStats))
        {
            SelectedPokemonStats = PokemonStatsEntries.FirstOrDefault();
        }
        else
        {
            foreach (var pokemon in _allPokemonStats)
            {
                pokemon.IsSelected = ReferenceEquals(pokemon, SelectedPokemonStats);
            }
        }
    }

    private void ApplyMoveFilter(string? filterText)
    {
        if (_allMoves.Count == 0)
        {
            MoveEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allMoves
            : _allMoves.Where(move => MoveMatchesFilter(move, filter)).ToList();

        MoveEntries.Clear();
        foreach (var move in filtered)
        {
            MoveEntries.Add(move);
        }

        if (SelectedMove is null || !MoveEntries.Contains(SelectedMove))
        {
            SelectedMove = MoveEntries.FirstOrDefault();
        }
        else
        {
            foreach (var move in _allMoves)
            {
                move.IsSelected = ReferenceEquals(move, SelectedMove);
            }
        }
    }

    private void ApplyItemFilter(string? filterText)
    {
        if (_allItems.Count == 0)
        {
            ItemEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allItems
            : _allItems.Where(item => ItemMatchesFilter(item, filter)).ToList();

        ItemEntries.Clear();
        foreach (var item in filtered)
        {
            ItemEntries.Add(item);
        }

        if (SelectedItem is null || !ItemEntries.Contains(SelectedItem))
        {
            SelectedItem = ItemEntries.FirstOrDefault();
        }
        else
        {
            foreach (var item in _allItems)
            {
                item.IsSelected = ReferenceEquals(item, SelectedItem);
            }
        }
    }

    private void ApplyGiftPokemonFilter(string? filterText)
    {
        if (_allGiftPokemon.Count == 0)
        {
            GiftPokemonEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allGiftPokemon
            : _allGiftPokemon.Where(gift => Contains(gift.SearchText, filter)).ToList();

        GiftPokemonEntries.Clear();
        foreach (var gift in filtered)
        {
            GiftPokemonEntries.Add(gift);
        }

        if (SelectedGiftPokemon is null || !GiftPokemonEntries.Contains(SelectedGiftPokemon))
        {
            SelectedGiftPokemon = GiftPokemonEntries.FirstOrDefault();
        }
        else
        {
            foreach (var gift in _allGiftPokemon)
            {
                gift.IsSelected = ReferenceEquals(gift, SelectedGiftPokemon);
            }
        }
    }

    private void ApplyTypeFilter(string? filterText)
    {
        if (_allTypes.Count == 0)
        {
            TypeEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTypes
            : _allTypes.Where(type => Contains(type.SearchText, filter)).ToList();

        TypeEntries.Clear();
        foreach (var type in filtered)
        {
            TypeEntries.Add(type);
        }

        if (SelectedType is null || !TypeEntries.Contains(SelectedType))
        {
            SelectedType = TypeEntries.FirstOrDefault();
        }
        else
        {
            foreach (var type in _allTypes)
            {
                type.IsSelected = ReferenceEquals(type, SelectedType);
            }
        }
    }

    private void ApplyTreasureFilter(string? filterText)
    {
        if (_allTreasures.Count == 0)
        {
            TreasureEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTreasures
            : _allTreasures.Where(treasure => TreasureMatchesFilter(treasure, filter)).ToList();

        TreasureEntries.Clear();
        foreach (var treasure in filtered)
        {
            TreasureEntries.Add(treasure);
        }

        if (SelectedTreasure is null || !TreasureEntries.Contains(SelectedTreasure))
        {
            SelectedTreasure = TreasureEntries.FirstOrDefault();
        }
        else
        {
            foreach (var treasure in _allTreasures)
            {
                treasure.IsSelected = ReferenceEquals(treasure, SelectedTreasure);
            }
        }
    }

    private void ApplyMessageFilter(string? filterText)
    {
        if (_allMessageStrings.Count == 0)
        {
            MessageStrings.Clear();
            return;
        }

        var rawFilter = filterText?.Trim() ?? string.Empty;
        var filter = SimplifySearchText(rawFilter);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allMessageStrings
            : _allMessageStrings.Where(message => MessageMatchesFilter(message, rawFilter, filter)).ToList();

        MessageStrings.Clear();
        foreach (var message in filtered)
        {
            MessageStrings.Add(message);
        }

        if (SelectedMessageString is not null && !MessageStrings.Contains(SelectedMessageString))
        {
            SelectedMessageString = null;
        }

        if (SelectedMessageString is not null)
        {
            foreach (var message in _allMessageStrings)
            {
                message.IsSelected = ReferenceEquals(message, SelectedMessageString);
            }
        }
    }

    private static bool MessageMatchesFilter(MessageStringEntryViewModel message, string rawFilter, string simplifiedFilter)
    {
        if (int.TryParse(rawFilter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            && message.Message.Id == id)
        {
            return true;
        }

        if (rawFilter.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(rawFilter[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexId)
            && message.Message.Id == hexId)
        {
            return true;
        }

        return Contains(message.SearchText, simplifiedFilter);
    }

    private void ApplyTableEditorFilter(string? filterText)
    {
        if (_allTableEditorEntries.Count == 0)
        {
            TableEditorEntries.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTableEditorEntries
            : _allTableEditorEntries.Where(table => Contains(table.SearchText, filter)).ToList();

        TableEditorEntries.Clear();
        foreach (var table in filtered)
        {
            TableEditorEntries.Add(table);
        }

        if (SelectedTableEditorEntry is null || !TableEditorEntries.Contains(SelectedTableEditorEntry))
        {
            SelectedTableEditorEntry = TableEditorEntries.FirstOrDefault();
        }
        else
        {
            foreach (var table in _allTableEditorEntries)
            {
                table.IsSelected = ReferenceEquals(table, SelectedTableEditorEntry);
            }
        }
    }

    private void ApplyCollisionFilter(string? filterText)
    {
        if (_allCollisionFiles.Count == 0)
        {
            CollisionFiles.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allCollisionFiles
            : _allCollisionFiles.Where(file => Contains(file.SearchText, filter)).ToList();

        CollisionFiles.Clear();
        foreach (var file in filtered)
        {
            CollisionFiles.Add(file);
        }

        if (SelectedCollisionFile is null || !CollisionFiles.Contains(SelectedCollisionFile))
        {
            SelectedCollisionFile = CollisionFiles.FirstOrDefault();
        }
        else
        {
            foreach (var file in _allCollisionFiles)
            {
                file.IsSelected = ReferenceEquals(file, SelectedCollisionFile);
            }
        }
    }

    private void ApplyVertexFilter(string? filterText)
    {
        if (_allVertexFilterFiles.Count == 0)
        {
            VertexFilterFiles.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allVertexFilterFiles
            : _allVertexFilterFiles.Where(file => Contains(file.SearchText, filter)).ToList();

        VertexFilterFiles.Clear();
        foreach (var file in filtered)
        {
            VertexFilterFiles.Add(file);
        }

        if (SelectedVertexFilterFile is null || !VertexFilterFiles.Contains(SelectedVertexFilterFile))
        {
            SelectedVertexFilterFile = VertexFilterFiles.FirstOrDefault();
        }
        else
        {
            foreach (var file in _allVertexFilterFiles)
            {
                file.IsSelected = ReferenceEquals(file, SelectedVertexFilterFile);
            }
        }
    }

    private static IReadOnlyList<TableEditorEntryViewModel> BuildTableEditorEntries(
        ColosseumCommonRel commonRel,
        ColosseumProjectContext project)
    {
        var firstItemOffset = commonRel.ItemData.FirstOrDefault()?.StartOffset;
        var firstTypeOffset = commonRel.TypeData.FirstOrDefault()?.StartOffset;
        var entries = new List<TableEditorDefinition>
        {
            Common("AI Weight Effect", 56),
            Common("Battle", 50),
            Common("Battle Styles", 42),
            Common("Battle Types", 32),
            Common("Battlefield", 28),
            Common("Character", 6),
            Common("Character Model", 72),
            Common("Door", 30),
            Common("Interaction Point", 86),
            Common("Move", 62),
            Common("Multiplier", 70),
            Common("Nature", 64),
            Common("Pokemon AI Roles", 58),
            Common("Pokemon Stats", 68),
            Common("Pokeface", 4),
            Common("Room", 14),
            Common("Shadow Pokemon Data", 80),
            Common("Sounds", 52),
            Common("Trainer AI", 46),
            Common("Trainer Class", 24),
            Common("Treasure", 60),
            Common("Trainer Pokemon", 48, category: TableEditorCategoryDeckPokemon),
            Common("Trainer", 44, category: TableEditorCategoryDeckTrainer),
            Dol("Ability", project, count: commonRel.Abilities.Count),
            Dol("Item", project, startOffset: firstItemOffset, count: commonRel.ItemData.Count, entryLength: 0x28),
            Dol("PKX Pokemon Model", project, count: 417, entryLength: 0x0c),
            Dol("PKX Trainer Model", project, count: 76, entryLength: 0x0c),
            Dol("Script Functions", project, count: 242, entryLength: 0x0c),
            Dol("Status Effects", project),
            Dol("Texture", project, count: 0x3da),
            Dol("Texture Rendering Info", project, count: 0x12d2),
            Dol("TM Or HM", project, count: 58, entryLength: 0x08),
            Dol("Type", project, startOffset: firstTypeOffset, count: commonRel.TypeData.Count, entryLength: 0x2c),
            Dol("Valid Item", project, count: 1220, entryLength: 0x02),
            Dol("Valid Item 2", project, count: 1220, entryLength: 0x02),
            Codable("Gift Pokemon", 4),
            Codable("Shops", null),
            Codable("Starter Pokemon", 2)
        };

        return entries
            .Select(definition => BuildTableEditorEntry(definition, commonRel, project))
            .ToArray();
    }

    private static TableEditorDefinition Common(string name, int commonIndex, string category = TableEditorCategoryCommon)
        => new(name, category, "common.rel", ColosseumRawTableSource.CommonRel, commonIndex, commonIndex + 1, null, null, null);

    private static TableEditorDefinition Dol(
        string name,
        ColosseumProjectContext project,
        int? startOffset = null,
        int? count = null,
        int? entryLength = null)
        => new(name, TableEditorCategoryOther, "Start.dol", ColosseumRawTableSource.StartDol, null, null, startOffset, count, entryLength);

    private static TableEditorDefinition Codable(string name, int? count)
        => new(name, TableEditorCategoryCodable, string.Empty, null, null, null, null, count, null);

    private static TableEditorEntryViewModel BuildTableEditorEntry(
        TableEditorDefinition definition,
        ColosseumCommonRel commonRel,
        ColosseumProjectContext project)
    {
        var details = definition.CommonIndex is null
            ? BuildStaticTableDetails(definition)
            : BuildCommonTableDetails(definition, commonRel, project);
        var searchText = $"{definition.Name} {definition.Category} {details}";
        return new TableEditorEntryViewModel(
            definition.Name,
            definition.Category,
            searchText,
            details,
            ColourForTableEditorCategory(definition.Category),
            BuildRawTableDefinition(definition));
    }

    private static ColosseumRawTableDefinition? BuildRawTableDefinition(TableEditorDefinition definition)
    {
        if (definition.Source is null)
        {
            return null;
        }

        if (definition.Source == ColosseumRawTableSource.StartDol
            && (definition.StartOffset is null || definition.Count is null || definition.EntryLength is null))
        {
            return null;
        }

        return new ColosseumRawTableDefinition(
            definition.Name,
            definition.Category,
            definition.Source.Value,
            definition.FileName,
            definition.CommonIndex,
            definition.CountIndex,
            definition.StartOffset,
            definition.Count,
            definition.EntryLength);
    }

    private static string BuildCommonTableDetails(
        TableEditorDefinition definition,
        ColosseumCommonRel commonRel,
        ColosseumProjectContext project)
    {
        var commonIndex = definition.CommonIndex!.Value;
        var countIndex = definition.CountIndex!.Value;
        var startOffset = commonRel.RelocationTable.GetPointer(commonIndex);
        var count = commonRel.RelocationTable.GetValueAtPointer(countIndex);
        var symbolLength = commonRel.RelocationTable.GetSymbolLength(commonIndex);
        var entryLength = count > 0 && symbolLength > 0 && symbolLength % count == 0
            ? symbolLength / count
            : definition.EntryLength;

        return BuildDetails(
            PathForTableFile(project, "common.rel"),
            startOffset,
            count,
            entryLength);
    }

    private static string BuildStaticTableDetails(TableEditorDefinition definition)
    {
        if (definition.Category == TableEditorCategoryCodable)
        {
            return definition.Count is null
                ? "Details:\nNumber of Entries: -"
                : $"Details:\nNumber of Entries: {HexAndDecimal(definition.Count.Value)}";
        }

        return BuildDetails(definition.FileName, definition.StartOffset, definition.Count, definition.EntryLength);
    }

    private static string BuildDetails(string file, int? startOffset, int? count, int? entryLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Details:");
        builder.AppendLine($"File: {file}");
        builder.AppendLine($"Start Offset: {HexAndDecimal(startOffset)}");
        builder.AppendLine($"Number of Entries: {HexAndDecimal(count)}");
        builder.Append($"Entry Length: {HexAndDecimal(entryLength)}");
        return builder.ToString();
    }

    private static string HexAndDecimal(int? value)
        => value is null || value < 0 ? "-" : $"0x{value.Value:X} ({value.Value})";

    private static string PathForTableFile(ColosseumProjectContext project, string fileName)
    {
        if (project.WorkspaceDirectory is null)
        {
            return fileName;
        }

        return Path.Combine(project.WorkspaceDirectory, "Game Files", "common.fsys", fileName);
    }

    private static string ColourForTableEditorCategory(string category)
        => category switch
        {
            TableEditorCategorySaveData => "#FC80F6",
            TableEditorCategoryEReader => "#FFE8D0",
            TableEditorCategoryDeckTrainer => "#B8F0FF",
            TableEditorCategoryDeckPokemon => "#D0FFD0",
            TableEditorCategoryDeckAi => "#FC6848",
            TableEditorCategoryCommon => "#F8F888",
            TableEditorCategoryOther => "#FFD080",
            TableEditorCategoryCodable => "#F0F0FC",
            _ => "#F0F0FC"
        };

    private const string TableEditorCategorySaveData = "Save Data";
    private const string TableEditorCategoryEReader = "E-Reader";
    private const string TableEditorCategoryDeckTrainer = "Deck Trainer";
    private const string TableEditorCategoryDeckPokemon = "Deck Pokemon";
    private const string TableEditorCategoryDeckAi = "Deck AI";
    private const string TableEditorCategoryCommon = "Common";
    private const string TableEditorCategoryOther = "Other";
    private const string TableEditorCategoryCodable = "Codable";

    private static bool TrainerMatchesFilter(TrainerEntryViewModel entry, IReadOnlyList<string> filterTokens)
    {
        var trainer = entry.Trainer;
        if (filterTokens.Count == 1 && filterTokens[0] == "shadow" && trainer.HasShadow)
        {
            return true;
        }

        if (filterTokens.Count == 1
            && int.TryParse(filterTokens[0], out var numericFilter)
            && trainer.Index == numericFilter)
        {
            return true;
        }

        var visibleTokens = SimplifySearchTokens($"{trainer.Name} {trainer.TrainerClassName}");
        return filterTokens.All(filter =>
            visibleTokens.Any(token => token.StartsWith(filter, StringComparison.Ordinal)));
    }

    private static bool PokemonStatsMatchesFilter(PokemonStatsEntryViewModel entry, string filter)
    {
        var stats = entry.Stats;
        if (int.TryParse(filter, out var numericFilter)
            && (stats.Index == numericFilter || stats.NationalIndex == numericFilter))
        {
            return true;
        }

        return Contains(entry.SearchText, filter);
    }

    private static bool MoveMatchesFilter(MoveEntryViewModel entry, string filter)
    {
        var move = entry.Move;
        if (int.TryParse(filter, out var numericFilter)
            && (move.Index == numericFilter || move.EffectId == numericFilter))
        {
            return true;
        }

        return Contains(entry.SearchText, filter);
    }

    private static bool ItemMatchesFilter(ItemEntryViewModel entry, string filter)
    {
        var item = entry.Item;
        if (int.TryParse(filter, out var numericFilter)
            && (item.Index == numericFilter
                || item.TmIndex == numericFilter
                || item.InBattleUseId == numericFilter
                || item.HoldItemId == numericFilter))
        {
            return true;
        }

        return Contains(entry.SearchText, filter);
    }

    private static bool TreasureMatchesFilter(TreasureEntryViewModel entry, string filter)
    {
        var treasure = entry.Treasure;
        if (int.TryParse(filter, out var numericFilter)
            && (treasure.Index == numericFilter
                || treasure.RoomId == numericFilter
                || treasure.ItemId == numericFilter
                || treasure.Flag == numericFilter))
        {
            return true;
        }

        return Contains(entry.SearchText, filter);
    }

    private static bool Contains(string value, string filter)
        => SimplifySearchText(value).Contains(filter, StringComparison.Ordinal);

    private static List<string> SimplifySearchTokens(string? value)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return tokens;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            FlushToken(tokens, builder);
        }

        FlushToken(tokens, builder);
        return tokens;
    }

    private static void FlushToken(ICollection<string> tokens, StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }

    private static string SimplifySearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static bool TryParseMessageId(string? value, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out id)
                && id is > 0 and <= 0x000fffff;
        }

        return int.TryParse(text, out id) && id is > 0 and <= 0x000fffff;
    }

    private void RefreshSelectedTrainerDetails(TrainerEntryViewModel? value)
    {
        SelectedTrainerPokemon = [];

        if (value is null)
        {
            SelectedTrainerName = "Name";
            SelectedTrainerClass = "Class";
            SelectedTrainerModel = "Model";
            SelectedTrainerAi = "AI";
            SelectedTrainerBattleStyle = "-";
            SelectedTrainerBattleType = "-";
            SelectedTrainerBattleId = "0";
            SelectedTrainerBgm = "-";
            SelectedTrainerStringIds = "Name ID: -   Pre: -   Win: -   Loss: -";
            TrainerDetailBackgroundBrush = TrainerNormalBrush;
            return;
        }

        var trainer = value.Trainer;
        SelectedTrainerName = trainer.Name;
        SelectedTrainerClass = $"{trainer.TrainerClassName} ({trainer.TrainerClassId})";
        SelectedTrainerModel = trainer.TrainerModelName;
        SelectedTrainerAi = trainer.Ai.ToString();
        SelectedTrainerBattleStyle = trainer.Battle?.BattleStyleLabel ?? "-";
        SelectedTrainerBattleType = trainer.Battle?.BattleTypeLabel ?? "-";
        SelectedTrainerBattleId = trainer.Battle?.Index.ToString() ?? "0";
        SelectedTrainerBgm = trainer.Battle?.BgmHex ?? "-";
        SelectedTrainerStringIds = $"Name ID: {trainer.NameId}   Pre: {trainer.PreBattleTextId}   Win: {trainer.VictoryTextId}   Loss: {trainer.DefeatTextId}";
        TrainerDetailBackgroundBrush = trainer.HasShadow ? TrainerShadowBrush : TrainerNormalBrush;

        var slotTimer = Stopwatch.StartNew();
        SelectedTrainerPokemon = trainer.Pokemon
            .Select(pokemon => new TrainerPokemonSlotViewModel(pokemon, _trainerPokemonResources, OnTrainerPokemonChanged))
            .ToArray();
        LogPerformance("Trainer Editor build selected Pokemon slots", slotTimer, SelectedTrainerPokemon.Count);

        SaveTrainerCommand.NotifyCanExecuteChanged();
    }

    private void LoadSelectedCollisionData(CollisionFileEntryViewModel? value)
    {
        CollisionInteractionOptions.Clear();
        CollisionSectionOptions.Clear();
        SelectedCollisionInteraction = null;
        SelectedCollisionSection = null;

        if (value is null || CurrentProject is null)
        {
            SelectedCollisionData = null;
            CollisionStatus = _allCollisionFiles.Count == 0
                ? "No .col files found. Extract ISO files and try again."
                : "Select a collision file.";
            return;
        }

        try
        {
            var data = CurrentProject.LoadCollisionData(value.File);
            SelectedCollisionData = data;
            CollisionInteractionOptions.Add(new PickerOptionViewModel(-1, "-"));
            foreach (var index in data.InteractableIndexes)
            {
                CollisionInteractionOptions.Add(new PickerOptionViewModel(index, $"interactable region {index}"));
            }

            CollisionSectionOptions.Add(new PickerOptionViewModel(-1, "-"));
            foreach (var index in data.SectionIndexes)
            {
                CollisionSectionOptions.Add(new PickerOptionViewModel(index, $"section {index}"));
            }

            SelectedCollisionInteraction = CollisionInteractionOptions.FirstOrDefault();
            SelectedCollisionSection = CollisionSectionOptions.FirstOrDefault();
            CollisionStatus = $"{value.File.FileName}: {data.Triangles.Count} faces, {data.InteractableIndexes.Count} interactable regions, {data.SectionIndexes.Count} sections.";
        }
        catch (Exception ex)
        {
            SelectedCollisionData = ColosseumCollisionData.Empty;
            CollisionStatus = $"{value.File.FileName}: {ex.Message}";
            Logs.Add($"Collision parse failed: {value.File.FileName} - {ex.Message}");
        }
    }

    private void OnTrainerPokemonChanged()
    {
        SaveTrainerCommand.NotifyCanExecuteChanged();
    }

    private void OnPokemonStatsChanged()
    {
        SavePokemonStatsCommand.NotifyCanExecuteChanged();
    }

    private void OnMoveChanged()
    {
        SaveMoveCommand.NotifyCanExecuteChanged();
    }

    private void OnItemChanged()
    {
        SaveItemCommand.NotifyCanExecuteChanged();
    }

    private void OnGiftPokemonChanged()
    {
        SaveGiftPokemonCommand.NotifyCanExecuteChanged();
    }

    private void OnTypeChanged()
    {
        SaveTypeCommand.NotifyCanExecuteChanged();
    }

    private void OnTreasureChanged()
    {
        SaveTreasureCommand.NotifyCanExecuteChanged();
    }

    private void OnInteractionChanged()
    {
        SaveInteractionCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSavedPokemonStatsEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().PokemonStatsFor(index);
        if (updated is null)
        {
            SelectedPokemonStatsDetail?.MarkSaved();
            return;
        }

        var replacement = new PokemonStatsEntryViewModel(updated);
        var listIndex = _allPokemonStats.FindIndex(entry => entry.Stats.Index == index);
        if (listIndex >= 0)
        {
            _allPokemonStats[listIndex] = replacement;
        }

        ApplyPokemonStatsFilter(PokemonStatsSearchText);
        SelectedPokemonStats = PokemonStatsEntries.FirstOrDefault(entry => entry.Stats.Index == index) ?? replacement;
    }

    private void RefreshSavedMoveEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().MoveById(index);
        if (updated is null)
        {
            SelectedMoveDetail?.MarkSaved();
            return;
        }

        var replacement = new MoveEntryViewModel(updated);
        var listIndex = _allMoves.FindIndex(entry => entry.Move.Index == index);
        if (listIndex >= 0)
        {
            _allMoves[listIndex] = replacement;
        }

        ApplyMoveFilter(MoveSearchText);
        SelectedMove = MoveEntries.FirstOrDefault(entry => entry.Move.Index == index) ?? replacement;
    }

    private void RefreshSavedItemEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().ItemById(index);
        if (updated is null)
        {
            SelectedItemDetail?.MarkSaved();
            return;
        }

        var replacement = new ItemEntryViewModel(updated);
        var listIndex = _allItems.FindIndex(entry => entry.Item.Index == index);
        if (listIndex >= 0)
        {
            _allItems[listIndex] = replacement;
        }

        ApplyItemFilter(ItemSearchText);
        SelectedItem = ItemEntries.FirstOrDefault(entry => entry.Item.Index == index) ?? replacement;
    }

    private void RefreshSavedGiftPokemonEntry(int rowId)
    {
        var updated = CurrentProject?.LoadCommonRel().GiftPokemonByRow(rowId);
        if (updated is null)
        {
            SelectedGiftPokemonDetail?.MarkSaved();
            return;
        }

        var rowIndex = Math.Max(0, _allGiftPokemon.FindIndex(entry => entry.Gift.RowId == rowId));
        var replacement = new GiftPokemonEntryViewModel(updated, rowIndex);
        var listIndex = _allGiftPokemon.FindIndex(entry => entry.Gift.RowId == rowId);
        if (listIndex >= 0)
        {
            _allGiftPokemon[listIndex] = replacement;
        }

        ApplyGiftPokemonFilter(GiftPokemonSearchText);
        SelectedGiftPokemon = GiftPokemonEntries.FirstOrDefault(entry => entry.Gift.RowId == rowId) ?? replacement;
    }

    private void RefreshSavedTypeEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().TypeById(index);
        if (updated is null)
        {
            SelectedTypeDetail?.MarkSaved();
            return;
        }

        var replacement = new TypeEntryViewModel(updated);
        var listIndex = _allTypes.FindIndex(entry => entry.Type.Index == index);
        if (listIndex >= 0)
        {
            _allTypes[listIndex] = replacement;
        }

        ApplyTypeFilter(TypeSearchText);
        SelectedType = TypeEntries.FirstOrDefault(entry => entry.Type.Index == index) ?? replacement;
    }

    private void RefreshSavedTreasureEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().TreasureById(index);
        if (updated is null)
        {
            SelectedTreasureDetail?.MarkSaved();
            return;
        }

        var replacement = new TreasureEntryViewModel(updated);
        var listIndex = _allTreasures.FindIndex(entry => entry.Treasure.Index == index);
        if (listIndex >= 0)
        {
            _allTreasures[listIndex] = replacement;
        }

        ApplyTreasureFilter(TreasureSearchText);
        SelectedTreasure = TreasureEntries.FirstOrDefault(entry => entry.Treasure.Index == index) ?? replacement;
    }

    private void RefreshSavedInteractionEntry(int index)
    {
        var updated = CurrentProject?.LoadCommonRel().InteractionPointById(index);
        if (updated is null)
        {
            SelectedInteractionDetail?.MarkSaved();
            return;
        }

        var replacement = new InteractionEntryViewModel(updated);
        var listIndex = _allInteractions.FindIndex(entry => entry.Interaction.Index == index);
        if (listIndex >= 0)
        {
            _allInteractions[listIndex] = replacement;
        }

        InteractionEntries.Clear();
        foreach (var interaction in _allInteractions)
        {
            InteractionEntries.Add(interaction);
        }

        SelectedInteraction = InteractionEntries.FirstOrDefault(entry => entry.Interaction.Index == index) ?? replacement;
    }

    private void LoadMessageStrings(MessageTableViewModel? table)
    {
        _allMessageStrings.Clear();
        MessageStrings.Clear();
        SelectedMessageString = null;
        SelectedMessageIdText = string.Empty;
        SelectedMessageText = string.Empty;

        if (table is null)
        {
            return;
        }

        foreach (var message in table.Table.Strings)
        {
            _allMessageStrings.Add(new MessageStringEntryViewModel(message));
        }

        ApplyMessageFilter(MessageSearchText);
    }

    private void RefreshSelectedIsoFileDetails(IsoFileEntryViewModel? value)
    {
        if (value is null)
        {
            SelectedIsoFileName = "File name: -";
            SelectedIsoFileSize = "File size: -";
            IsoExplorerFilesText = "-";
            return;
        }

        SelectedIsoFileSize = value.FileSizeText;
        var detailTimer = Stopwatch.StartNew();
        IsoExplorerFilesText = BuildIsoFileDetails(value, out var groupId);
        LogPerformance("ISO Explorer selected file details", detailTimer, value.IsFsys ? 1 : 0);
        SelectedIsoFileName = groupId is null
            ? value.FileNameText
            : $"{value.FileNameText} GID: {groupId.Value}";
    }

    private string BuildIsoFileDetails(IsoFileEntryViewModel value, out uint? groupId)
    {
        groupId = null;
        if (CurrentProject is null)
        {
            return "-";
        }

        if (!value.IsFsys)
        {
            return "-";
        }

        try
        {
            var archive = CurrentProject.ReadIsoFsysArchive(value.Entry);
            groupId = archive.GroupId;

            return archive.Entries.Count == 0
                ? "No files found in archive."
                : Environment.NewLine + string.Join(Environment.NewLine, archive.Entries.Select(entry =>
                    $"{entry.Index}: {entry.Name} ({entry.Identifier:x8})"));
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void NotifyIsoExplorerCommands()
    {
        ExportAndDecodeSelectedIsoFileCommand.NotifyCanExecuteChanged();
        QuickExportSelectedIsoFileCommand.NotifyCanExecuteChanged();
        DecodeSelectedIsoFileCommand.NotifyCanExecuteChanged();
        EncodeAndImportSelectedIsoFileCommand.NotifyCanExecuteChanged();
        ImportSelectedIsoFileCommand.NotifyCanExecuteChanged();
        EncodeSelectedIsoFileCommand.NotifyCanExecuteChanged();
        DeleteSelectedIsoFileCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanAddFileToSelectedIsoFile));
    }

    private static bool TryParseFsysIdentifier(string? text, out ushort identifier)
    {
        identifier = 0;
        var normalized = (text ?? string.Empty).Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (normalized.Length is < 1 or > 4)
        {
            return false;
        }

        if (!ushort.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
            || value == 0)
        {
            return false;
        }

        identifier = value;
        return true;
    }

    private static string BuildIsoExportStatus(string fileName, IsoExportResult result)
    {
        var status = $"Exported {fileName} to {result.FilePath}";
        if (result.ExtractedFiles.Count > 0)
        {
            status += $" | extracted {result.ExtractedFiles.Count} files";
        }

        if (result.DecodedFiles.Count > 0)
        {
            status += $" | decoded {result.DecodedFiles.Count} files";
        }

        return status;
    }

    private static string BuildIsoEncodeStatus(string fileName, IsoEncodeResult result)
    {
        var status = $"Encoded {fileName} workspace file: {result.FilePath}";
        if (result.EncodedFiles.Count > 0)
        {
            status += $" | encoded {result.EncodedFiles.Count} message JSON file(s)";
        }

        if (result.PackedFiles.Count > 0)
        {
            status += $" | packed {result.PackedFiles.Count} archive file(s)";
        }

        return status;
    }

    private static string BuildIsoImportStatus(string fileName, IsoImportResult result)
    {
        var status = $"Imported {fileName} to ISO from {result.FilePath} ({result.WrittenBytes:N0}/{result.MaximumBytes:N0} bytes)";
        if (result.InsertedBytes > 0)
        {
            status += $" | shifted later ISO data by {result.InsertedBytes:N0} bytes";
        }

        if (result.EncodeResult is not null)
        {
            if (result.EncodeResult.EncodedFiles.Count > 0)
            {
                status += $" | encoded {result.EncodeResult.EncodedFiles.Count} message JSON file(s)";
            }

            if (result.EncodeResult.PackedFiles.Count > 0)
            {
                status += $" | packed {result.EncodeResult.PackedFiles.Count} archive file(s)";
            }
        }

        return status;
    }

    private static string BuildIsoDeleteStatus(IsoDeleteResult result)
    {
        var status = $"Deleted {result.FileName} from ISO with legacy preserved marker ({result.WrittenBytes:N0} bytes)";
        if (!string.IsNullOrWhiteSpace(result.BackupPath))
        {
            status += $" | backup/export: {result.BackupPath}";
        }

        return status;
    }

    private static string BuildProjectTitle(ColosseumProjectContext context)
    {
        var name = Path.GetFileName(context.SourcePath);
        return context.Iso is null ? name : $"{name} ({context.Iso.GameId}, {context.Iso.Region})";
    }

    private static string BuildProjectTitle(XdProjectContext context)
    {
        var name = Path.GetFileName(context.SourcePath);
        return $"{name} ({context.Iso.GameId}, {context.Iso.Region})";
    }

    private static string BuildWorkspaceStatus(ColosseumProjectContext context)
    {
        return context.SourceKind switch
        {
            ColosseumSourceKind.Iso => $"Workspace: {context.WorkspaceDirectory}",
            ColosseumSourceKind.Fsys => $"FSYS entries: {context.FsysArchive?.Entries.Count ?? 0}",
            ColosseumSourceKind.Message => $"Messages: {context.MessageTable?.Strings.Count ?? 0}",
            ColosseumSourceKind.Texture => "Texture file loaded.",
            _ => "File loaded."
        };
    }

    private static string BuildWorkspaceStatus(XdProjectContext context)
        => $"Workspace: {context.WorkspaceDirectory}";

    public void LogPerformance(string label, Stopwatch stopwatch, int? count = null)
    {
        stopwatch.Stop();
        LogPerformance(label, stopwatch.Elapsed, count);
    }

    public void LogPerformance(string label, TimeSpan elapsed, int? count = null)
    {
        var countText = count is null ? string.Empty : $" ({count.Value:N0})";
        Logs.Add($"[perf] {label}{countText}: {elapsed.TotalMilliseconds:N0} ms");
    }

    public void LogPerformanceDetail(string message)
    {
        Logs.Add($"[perf] {message}");
    }

    private static string BuildLogSummary(ColosseumProjectContext context)
    {
        return context.SourceKind switch
        {
            ColosseumSourceKind.Iso => $"ISO loaded: {context.Iso?.Files.Count ?? 0} FST files.",
            ColosseumSourceKind.Fsys => $"FSYS loaded: {context.FsysArchive?.Entries.Count ?? 0} entries.",
            ColosseumSourceKind.Message => $"Message table loaded: {context.MessageTable?.Strings.Count ?? 0} strings.",
            ColosseumSourceKind.Texture => "Texture file recognized.",
            _ => "File loaded."
        };
    }

    private static string BuildLogSummary(XdProjectContext context)
        => $"XD ISO loaded: {context.Iso.Files.Count} FST files.";

    private sealed record TableEditorDefinition(
        string Name,
        string Category,
        string FileName,
        ColosseumRawTableSource? Source,
        int? CommonIndex,
        int? CountIndex,
        int? StartOffset,
        int? Count,
        int? EntryLength);
}
