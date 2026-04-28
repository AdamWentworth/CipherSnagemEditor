using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush TrainerNormalBrush = SolidColorBrush.Parse("#FC6848");
    private static readonly IBrush TrainerShadowBrush = SolidColorBrush.Parse("#A77AF4");
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
    private TrainerPokemonEditorResources _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
    private PokemonStatsEditorResources _pokemonStatsResources = PokemonStatsEditorResources.Empty;
    private MoveEditorResources _moveEditorResources = MoveEditorResources.Empty;
    private ItemEditorResources _itemEditorResources = ItemEditorResources.Empty;
    private GiftPokemonEditorResources _giftPokemonResources = GiftPokemonEditorResources.Empty;
    private TreasureEditorResources _treasureEditorResources = TreasureEditorResources.Empty;
    private InteractionEditorResources _interactionEditorResources = InteractionEditorResources.Empty;

    [ObservableProperty]
    private ToolEntryViewModel? _selectedTool;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _windowTitle = "Colosseum Tool - Cipher Snagem Editor";

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
    private MessageStringEntryViewModel? _selectedMessageString;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTableEditorEntry))]
    [NotifyPropertyChangedFor(nameof(SelectedTableEditorName))]
    [NotifyPropertyChangedFor(nameof(SelectedTableEditorDetails))]
    private TableEditorEntryViewModel? _selectedTableEditorEntry;

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
    {
        Tools = new ObservableCollection<ToolEntryViewModel>(
            ColosseumToolCatalog.HomeTools.Select((tool, index) => new ToolEntryViewModel(index + 1, tool)));
        SelectedTool = Tools.FirstOrDefault();
        Logs.Add("Cipher Snagem Editor ready.");
        Logs.Add("Legacy mode: Colosseum Tool.");
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

    public ObservableCollection<TrainerPokemonSlotViewModel> SelectedTrainerPokemon { get; } = [];

    public bool HasSelectedTableEditorEntry => SelectedTableEditorEntry is not null;

    public string SelectedTableEditorName => SelectedTableEditorEntry?.Name ?? "Data Table";

    public string SelectedTableEditorDetails => SelectedTableEditorEntry?.Details ?? "Details: -";

    public ColosseumProjectContext? CurrentProject { get; private set; }

    public async Task OpenPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        IsBusy = true;
        Logs.Add($"Opening {path}");

        try
        {
            var context = await Task.Run(() => ColosseumProjectContext.Open(path));
            CurrentProject = context;
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
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            SelectedTrainerPokemon.Clear();
            PopulateIsoFiles(context);
            RefreshSelectedToolView(SelectedTool);
            Logs.Add(BuildLogSummary(context));
        }
        catch (Exception ex)
        {
            HasProject = false;
            CurrentProject = null;
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
            SelectedMessageIdText = string.Empty;
            SelectedMessageText = string.Empty;
            SelectedTrainerPokemon.Clear();
            RefreshSelectedToolView(SelectedTool);
            Logs.Add($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
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
    private void ImportSelectedIsoFile()
    {
        IsoExplorerStatus = "Import is not implemented yet.";
        Logs.Add("Import is not implemented yet.");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private void EncodeSelectedIsoFile()
    {
        IsoExplorerStatus = "Encode is not implemented yet.";
        Logs.Add("Encode is not implemented yet.");
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedIsoFile))]
    private void DeleteSelectedIsoFile()
    {
        IsoExplorerStatus = "Delete is not implemented yet.";
        Logs.Add("Delete is not implemented yet.");
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
    private void EncodeTableEditor()
    {
        LogTableEditorAction("Encode for editing via text files");
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private void DecodeTableEditor()
    {
        LogTableEditorAction("Decode edited files back into the game");
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private void DocumentTableEditor()
    {
        LogTableEditorAction("Document as text files for reference");
    }

    [RelayCommand(CanExecute = nameof(CanRunTableEditorAction))]
    private void EditTableEditor()
    {
        LogTableEditorAction("Edit");
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
            && TryParseMessageId(SelectedMessageIdText, out _)
            && !IsBusy;

    private bool CanRunTableEditorAction()
        => SelectedTableEditorEntry is not null && !IsBusy;

    private void LogTableEditorAction(string action)
    {
        if (SelectedTableEditorEntry is null)
        {
            return;
        }

        Logs.Add($"{action}: {SelectedTableEditorEntry.Name} is queued for the universal table backend.");
    }

    public bool PrepareToolWindow(ToolEntryViewModel tool)
    {
        SelectedTool = tool;
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
            case "Interaction Editor":
                LoadInteractionRows();
                return true;
            case "Message Editor":
                LoadMessageRows();
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
        foreach (var trainer in _allTrainers)
        {
            trainer.IsSelected = ReferenceEquals(trainer, value);
        }

        RefreshSelectedTrainerDetails(value);
        SaveTrainerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPokemonStatsChanged(PokemonStatsEntryViewModel? value)
    {
        foreach (var pokemon in _allPokemonStats)
        {
            pokemon.IsSelected = ReferenceEquals(pokemon, value);
        }

        SelectedPokemonStatsDetail = value is null
            ? null
            : new PokemonStatsEditorViewModel(value.Stats, _pokemonStatsResources, value.FaceImage, OnPokemonStatsChanged);
        SavePokemonStatsCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedMoveChanged(MoveEntryViewModel? value)
    {
        foreach (var move in _allMoves)
        {
            move.IsSelected = ReferenceEquals(move, value);
        }

        SelectedMoveDetail = value is null
            ? null
            : new MoveEditorViewModel(value.Move, _moveEditorResources, OnMoveChanged);
        SaveMoveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedItemChanged(ItemEntryViewModel? value)
    {
        foreach (var item in _allItems)
        {
            item.IsSelected = ReferenceEquals(item, value);
        }

        SelectedItemDetail = value is null
            ? null
            : new ItemEditorViewModel(value.Item, _itemEditorResources, OnItemChanged);
        SaveItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGiftPokemonChanged(GiftPokemonEntryViewModel? value)
    {
        foreach (var gift in _allGiftPokemon)
        {
            gift.IsSelected = ReferenceEquals(gift, value);
        }

        SelectedGiftPokemonDetail = value is null
            ? null
            : new GiftPokemonEditorViewModel(value.Gift, _giftPokemonResources, value.FaceImage, OnGiftPokemonChanged);
        SaveGiftPokemonCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTypeChanged(TypeEntryViewModel? value)
    {
        foreach (var type in _allTypes)
        {
            type.IsSelected = ReferenceEquals(type, value);
        }

        SelectedTypeDetail = value is null
            ? null
            : new TypeEditorViewModel(value.Type, _allTypes.Select(entry => entry.Type).ToArray(), OnTypeChanged);
        SaveTypeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTreasureChanged(TreasureEntryViewModel? value)
    {
        foreach (var treasure in _allTreasures)
        {
            treasure.IsSelected = ReferenceEquals(treasure, value);
        }

        SelectedTreasureDetail = value is null
            ? null
            : new TreasureEditorViewModel(value.Treasure, _treasureEditorResources, OnTreasureChanged);
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
    {
        _allIsoFiles.Clear();
        IsoFiles.Clear();
        if (context.Iso is null)
        {
            SelectedIsoFile = null;
            return;
        }

        foreach (var entry in context.Iso.Files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
        {
            _allIsoFiles.Add(new IsoFileEntryViewModel(entry));
        }

        ApplyIsoFileFilter(IsoSearchText);
        SelectedIsoFile = IsoFiles.FirstOrDefault(file =>
                string.Equals(Path.GetFileName(file.Name), "Start.dol", StringComparison.OrdinalIgnoreCase))
            ?? IsoFiles.FirstOrDefault();
        NotifyIsoExplorerCommands();
    }

    private void LoadTrainerRows()
    {
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
            ApplyTrainerFilter(TrainerSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _trainerPokemonResources = TrainerPokemonEditorResources.FromCommonRel(commonRel);
            var trainers = commonRel.LoadStoryTrainers();
            foreach (var trainer in trainers)
            {
                _allTrainers.Add(new TrainerEntryViewModel(trainer));
            }

            ApplyTrainerFilter(TrainerSearchText);
            SelectedTrainer = Trainers.FirstOrDefault();
            Logs.Add($"Trainer Editor loaded: {_allTrainers.Count} story trainers.");
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
            ApplyPokemonStatsFilter(PokemonStatsSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _pokemonStatsResources = PokemonStatsEditorResources.FromCommonRel(commonRel);
            foreach (var pokemon in commonRel.PokemonStats)
            {
                _allPokemonStats.Add(new PokemonStatsEntryViewModel(pokemon));
            }

            ApplyPokemonStatsFilter(PokemonStatsSearchText);
            SelectedPokemonStats = PokemonStatsEntries.FirstOrDefault();
            Logs.Add($"Pokemon Stats Editor loaded: {_allPokemonStats.Count} Pokemon.");
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
            ApplyMoveFilter(MoveSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _moveEditorResources = MoveEditorResources.FromCommonRel(commonRel);
            foreach (var move in commonRel.Moves)
            {
                _allMoves.Add(new MoveEntryViewModel(move));
            }

            ApplyMoveFilter(MoveSearchText);
            SelectedMove = MoveEntries.FirstOrDefault();
            Logs.Add($"Move Editor loaded: {_allMoves.Count} moves.");
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
            ApplyItemFilter(ItemSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _itemEditorResources = ItemEditorResources.FromCommonRel(commonRel);
            foreach (var item in commonRel.ItemData)
            {
                _allItems.Add(new ItemEntryViewModel(item));
            }

            ApplyItemFilter(ItemSearchText);
            SelectedItem = ItemEntries.FirstOrDefault();
            Logs.Add($"Item Editor loaded: {_allItems.Count} items.");
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
            ApplyGiftPokemonFilter(GiftPokemonSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _giftPokemonResources = GiftPokemonEditorResources.FromCommonRel(commonRel);
            var rowIndex = 0;
            foreach (var gift in commonRel.GiftPokemon)
            {
                _allGiftPokemon.Add(new GiftPokemonEntryViewModel(gift, rowIndex++));
            }

            ApplyGiftPokemonFilter(GiftPokemonSearchText);
            SelectedGiftPokemon = GiftPokemonEntries.FirstOrDefault();
            Logs.Add($"Gift Pokemon Editor loaded: {_allGiftPokemon.Count} gifts.");
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
            ApplyTypeFilter(TypeSearchText);
            return;
        }

        try
        {
            var types = CurrentProject.LoadTypes();
            foreach (var type in types)
            {
                _allTypes.Add(new TypeEntryViewModel(type));
            }

            ApplyTypeFilter(TypeSearchText);
            SelectedType = TypeEntries.FirstOrDefault();
            Logs.Add($"Type Editor loaded: {_allTypes.Count} types.");
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
            ApplyTreasureFilter(TreasureSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _treasureEditorResources = TreasureEditorResources.FromCommonRel(commonRel);
            foreach (var treasure in commonRel.Treasures)
            {
                _allTreasures.Add(new TreasureEntryViewModel(treasure));
            }

            ApplyTreasureFilter(TreasureSearchText);
            SelectedTreasure = TreasureEntries.FirstOrDefault();
            Logs.Add($"Treasure Editor loaded: {_allTreasures.Count} treasure boxes.");
        }
        catch (Exception ex)
        {
            SelectedTreasure = null;
            SelectedTreasureDetail = null;
            SelectedToolDetail = $"Treasure Editor\n{ex.Message}";
            Logs.Add($"Treasure load failed: {ex.Message}");
        }
    }

    private void LoadInteractionRows()
    {
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
            InteractionEntries.Clear();
            foreach (var interaction in _allInteractions)
            {
                InteractionEntries.Add(interaction);
            }

            SelectedInteraction ??= InteractionEntries.FirstOrDefault();
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            _interactionEditorResources = InteractionEditorResources.FromCommonRel(commonRel);
            foreach (var interaction in commonRel.InteractionPoints)
            {
                _allInteractions.Add(new InteractionEntryViewModel(interaction));
            }

            InteractionEntries.Clear();
            foreach (var interaction in _allInteractions)
            {
                InteractionEntries.Add(interaction);
            }

            SelectedInteraction = InteractionEntries.FirstOrDefault();
            Logs.Add($"Interaction Editor loaded: {_allInteractions.Count} interaction points.");
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
            ApplyMessageFilter(MessageSearchText);
            return;
        }

        try
        {
            foreach (var table in CurrentProject.LoadMessageTables())
            {
                MessageTables.Add(new MessageTableViewModel(table));
            }

            SelectedMessageTable = MessageTables.FirstOrDefault();
            Logs.Add($"Message Editor loaded: {MessageTables.Count} message tables.");
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
        if (CurrentProject?.Iso is null)
        {
            _allTableEditorEntries.Clear();
            TableEditorEntries.Clear();
            SelectedTableEditorEntry = null;
            return;
        }

        if (_allTableEditorEntries.Count > 0)
        {
            ApplyTableEditorFilter(TableEditorSearchText);
            return;
        }

        try
        {
            var commonRel = CurrentProject.LoadCommonRel();
            foreach (var table in BuildTableEditorEntries(commonRel, CurrentProject))
            {
                _allTableEditorEntries.Add(table);
            }

            ApplyTableEditorFilter(TableEditorSearchText);
            SelectedTableEditorEntry = TableEditorEntries.FirstOrDefault();
            Logs.Add($"Table Editor loaded: {_allTableEditorEntries.Count} universal tables.");
        }
        catch (Exception ex)
        {
            SelectedTableEditorEntry = null;
            SelectedToolDetail = $"Table Editor\n{ex.Message}";
            Logs.Add($"Table Editor load failed: {ex.Message}");
        }
    }

    private void ApplyTrainerFilter(string? filterText)
    {
        if (_allTrainers.Count == 0)
        {
            Trainers.Clear();
            return;
        }

        var filterTokens = SimplifySearchTokens(filterText);
        var filtered = filterTokens.Count == 0
            ? _allTrainers
            : _allTrainers.Where(trainer => TrainerMatchesFilter(trainer, filterTokens)).ToList();

        Trainers.Clear();
        foreach (var trainer in filtered)
        {
            Trainers.Add(trainer);
        }

        if (SelectedTrainer is null || !Trainers.Contains(SelectedTrainer))
        {
            SelectedTrainer = Trainers.FirstOrDefault();
        }
        else
        {
            foreach (var trainer in _allTrainers)
            {
                trainer.IsSelected = ReferenceEquals(trainer, SelectedTrainer);
            }
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

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allMessageStrings
            : _allMessageStrings.Where(message => Contains(message.SearchText, filter)).ToList();

        MessageStrings.Clear();
        foreach (var message in filtered)
        {
            MessageStrings.Add(message);
        }

        if (SelectedMessageString is null || !MessageStrings.Contains(SelectedMessageString))
        {
            SelectedMessageString = MessageStrings.FirstOrDefault();
        }
        else
        {
            foreach (var message in _allMessageStrings)
            {
                message.IsSelected = ReferenceEquals(message, SelectedMessageString);
            }
        }
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

    private static IReadOnlyList<TableEditorEntryViewModel> BuildTableEditorEntries(
        ColosseumCommonRel commonRel,
        ColosseumProjectContext project)
    {
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
            Dol("Item", project, count: commonRel.ItemData.Count, entryLength: 0x28),
            Dol("PKX Pokemon Model", project, count: 417, entryLength: 0x0c),
            Dol("PKX Trainer Model", project, count: 76, entryLength: 0x0c),
            Dol("Script Functions", project, count: 242, entryLength: 0x0c),
            Dol("Status Effects", project),
            Dol("Texture", project, count: 0x3da),
            Dol("Texture Rendering Info", project, count: 0x12d2),
            Dol("TM Or HM", project, count: 58, entryLength: 0x05),
            Dol("Type", project, count: commonRel.TypeData.Count, entryLength: 0x2c),
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
        => new(name, category, "common.rel", commonIndex, commonIndex + 1, null, null);

    private static TableEditorDefinition Dol(
        string name,
        ColosseumProjectContext project,
        int? count = null,
        int? entryLength = null)
        => new(name, TableEditorCategoryOther, "Start.dol", null, null, count, entryLength);

    private static TableEditorDefinition Codable(string name, int? count)
        => new(name, TableEditorCategoryCodable, string.Empty, null, null, count, null);

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
            ColourForTableEditorCategory(definition.Category));
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

        return BuildDetails(definition.FileName, null, definition.Count, definition.EntryLength);
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
        SelectedTrainerPokemon.Clear();

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

        foreach (var pokemon in trainer.Pokemon)
        {
            SelectedTrainerPokemon.Add(new TrainerPokemonSlotViewModel(pokemon, _trainerPokemonResources, OnTrainerPokemonChanged));
        }

        SaveTrainerCommand.NotifyCanExecuteChanged();
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
        SelectedMessageString = MessageStrings.FirstOrDefault();
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
        IsoExplorerFilesText = BuildIsoFileDetails(value, out var groupId);
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
        ImportSelectedIsoFileCommand.NotifyCanExecuteChanged();
        EncodeSelectedIsoFileCommand.NotifyCanExecuteChanged();
        DeleteSelectedIsoFileCommand.NotifyCanExecuteChanged();
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

    private static string BuildProjectTitle(ColosseumProjectContext context)
    {
        var name = Path.GetFileName(context.SourcePath);
        return context.Iso is null ? name : $"{name} ({context.Iso.GameId}, {context.Iso.Region})";
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

    private sealed record TableEditorDefinition(
        string Name,
        string Category,
        string FileName,
        int? CommonIndex,
        int? CountIndex,
        int? Count,
        int? EntryLength);
}
