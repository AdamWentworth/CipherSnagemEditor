using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrreForge.Colosseum;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush TrainerNormalBrush = SolidColorBrush.Parse("#FC6848");
    private static readonly IBrush TrainerShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private readonly List<TrainerEntryViewModel> _allTrainers = [];
    private readonly List<PokemonStatsEntryViewModel> _allPokemonStats = [];
    private readonly List<MoveEntryViewModel> _allMoves = [];
    private TrainerPokemonEditorResources _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
    private PokemonStatsEditorResources _pokemonStatsResources = PokemonStatsEditorResources.Empty;
    private MoveEditorResources _moveEditorResources = MoveEditorResources.Empty;

    [ObservableProperty]
    private ToolEntryViewModel? _selectedTool;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _windowTitle = "Colosseum Tool - OrreForge Studio";

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
    private string _trainerSearchText = string.Empty;

    [ObservableProperty]
    private string _pokemonStatsSearchText = string.Empty;

    [ObservableProperty]
    private string _moveSearchText = string.Empty;

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
        Logs.Add("OrreForge Studio ready.");
        Logs.Add("Legacy mode: Colosseum Tool.");
    }

    public ObservableCollection<ToolEntryViewModel> Tools { get; }

    public ObservableCollection<string> Logs { get; } = [];

    public ObservableCollection<IsoFileEntryViewModel> IsoFiles { get; } = [];

    public ObservableCollection<TrainerEntryViewModel> Trainers { get; } = [];

    public ObservableCollection<PokemonStatsEntryViewModel> PokemonStatsEntries { get; } = [];

    public ObservableCollection<MoveEntryViewModel> MoveEntries { get; } = [];

    public ObservableCollection<TrainerPokemonSlotViewModel> SelectedTrainerPokemon { get; } = [];

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
            _allTrainers.Clear();
            _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
            _allPokemonStats.Clear();
            _pokemonStatsResources = PokemonStatsEditorResources.Empty;
            _allMoves.Clear();
            _moveEditorResources = MoveEditorResources.Empty;
            Trainers.Clear();
            PokemonStatsEntries.Clear();
            MoveEntries.Clear();
            TrainerSearchText = string.Empty;
            PokemonStatsSearchText = string.Empty;
            MoveSearchText = string.Empty;
            SelectedTrainer = null;
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            SelectedMove = null;
            SelectedMoveDetail = null;
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
            IsoFiles.Clear();
            _allTrainers.Clear();
            _trainerPokemonResources = TrainerPokemonEditorResources.Empty;
            _allPokemonStats.Clear();
            _pokemonStatsResources = PokemonStatsEditorResources.Empty;
            _allMoves.Clear();
            _moveEditorResources = MoveEditorResources.Empty;
            Trainers.Clear();
            PokemonStatsEntries.Clear();
            MoveEntries.Clear();
            TrainerSearchText = string.Empty;
            PokemonStatsSearchText = string.Empty;
            MoveSearchText = string.Empty;
            SelectedIsoFile = null;
            SelectedTrainer = null;
            SelectedPokemonStats = null;
            SelectedPokemonStatsDetail = null;
            SelectedMove = null;
            SelectedMoveDetail = null;
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

    public bool PrepareToolWindow(ToolEntryViewModel tool)
    {
        SelectedTool = tool;
        if (CurrentProject?.Iso is null)
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
        foreach (var file in IsoFiles)
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

    partial void OnIsBusyChanged(bool value)
    {
        NotifyIsoExplorerCommands();
        SaveTrainerCommand.NotifyCanExecuteChanged();
        SavePokemonStatsCommand.NotifyCanExecuteChanged();
        SaveMoveCommand.NotifyCanExecuteChanged();
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
        IsoFiles.Clear();
        if (context.Iso is null)
        {
            SelectedIsoFile = null;
            return;
        }

        foreach (var entry in context.Iso.Files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
        {
            IsoFiles.Add(new IsoFileEntryViewModel(entry));
        }

        SelectedIsoFile = IsoFiles.FirstOrDefault();
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

    private void ApplyTrainerFilter(string? filterText)
    {
        if (_allTrainers.Count == 0)
        {
            Trainers.Clear();
            return;
        }

        var filter = SimplifySearchText(filterText);
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTrainers
            : _allTrainers.Where(trainer => TrainerMatchesFilter(trainer, filter)).ToList();

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

    private static bool TrainerMatchesFilter(TrainerEntryViewModel entry, string filter)
    {
        var trainer = entry.Trainer;
        if (filter == "shadow" && trainer.HasShadow)
        {
            return true;
        }

        return Contains(trainer.Index.ToString(), filter)
            || Contains(trainer.Name, filter)
            || Contains(trainer.TrainerClassName, filter)
            || Contains(trainer.FullName, filter)
            || trainer.Pokemon.Any(pokemon =>
                Contains(pokemon.SpeciesName, filter)
                || pokemon.Moves.Any(move => Contains(move.Name, filter)));
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

    private static bool Contains(string value, string filter)
        => SimplifySearchText(value).Contains(filter, StringComparison.Ordinal);

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

    private void RefreshSelectedIsoFileDetails(IsoFileEntryViewModel? value)
    {
        if (value is null)
        {
            SelectedIsoFileName = "File name: -";
            SelectedIsoFileSize = "File size: -";
            IsoExplorerFilesText = "-";
            return;
        }

        SelectedIsoFileName = value.FileNameText;
        SelectedIsoFileSize = value.FileSizeText;
        IsoExplorerFilesText = BuildIsoFileDetails(value);
    }

    private string BuildIsoFileDetails(IsoFileEntryViewModel value)
    {
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

            return archive.Entries.Count == 0
                ? "No files found in archive."
                : string.Join(Environment.NewLine, archive.Entries.Select(entry =>
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
}
