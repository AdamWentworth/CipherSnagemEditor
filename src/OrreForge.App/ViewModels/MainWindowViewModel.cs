using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrreForge.Colosseum;

namespace OrreForge.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush TrainerNormalBrush = SolidColorBrush.Parse("#FC6848");
    private static readonly IBrush TrainerShadowBrush = SolidColorBrush.Parse("#A070FF");

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
    private bool _showIsoExplorer;

    [ObservableProperty]
    private bool _showTrainerEditor;

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
            Trainers.Clear();
            SelectedTrainer = null;
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
            Trainers.Clear();
            SelectedIsoFile = null;
            SelectedTrainer = null;
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

    [RelayCommand]
    private void ReturnHome()
    {
        SelectedTool = Tools.FirstOrDefault();
        ShowIsoExplorer = false;
        ShowTrainerEditor = false;
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
        foreach (var trainer in Trainers)
        {
            trainer.IsSelected = ReferenceEquals(trainer, value);
        }

        RefreshSelectedTrainerDetails(value);
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyIsoExplorerCommands();
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
        ShowIsoExplorer = value.Title == "ISO Explorer" && CurrentProject?.Iso is not null;
        ShowTrainerEditor = value.Title == "Trainer Editor" && CurrentProject?.Iso is not null;
        ShowHomeTools = !ShowIsoExplorer && !ShowTrainerEditor;
        ShowToolPlaceholder = !ShowIsoExplorer && !ShowTrainerEditor;
        ShowReturnHome = ShowIsoExplorer || ShowTrainerEditor;
        LeftPanelTitle = ShowIsoExplorer ? "Files" : ShowTrainerEditor ? string.Empty : "Tools";
        ShowLeftPanelTitle = !ShowTrainerEditor;
        LeftPanelWidth = new GridLength(ShowTrainerEditor ? 250 : 220);
        WorkspacePanelHeight = ShowTrainerEditor ? new GridLength(0) : GridLength.Auto;
        LogPanelHeight = ShowTrainerEditor ? new GridLength(0) : new GridLength(150);
        if (value.Title == "ISO Explorer" && CurrentProject?.Iso is null)
        {
            IsoExplorerStatus = "Open a Colosseum ISO to browse its files.";
        }

        if (ShowTrainerEditor)
        {
            LoadTrainerRows();
        }
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
            Trainers.Clear();
            SelectedTrainer = null;
            return;
        }

        if (Trainers.Count > 0)
        {
            SelectedTrainer ??= Trainers.FirstOrDefault();
            return;
        }

        try
        {
            var trainers = CurrentProject.LoadStoryTrainers();
            foreach (var trainer in trainers)
            {
                Trainers.Add(new TrainerEntryViewModel(trainer));
            }

            SelectedTrainer = Trainers.FirstOrDefault();
            Logs.Add($"Trainer Editor loaded: {Trainers.Count} story trainers.");
        }
        catch (Exception ex)
        {
            SelectedTrainer = null;
            SelectedToolDetail = $"Trainer Editor\n{ex.Message}";
            Logs.Add($"Trainer load failed: {ex.Message}");
        }
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
        SelectedTrainerClass = trainer.TrainerClassName;
        SelectedTrainerModel = $"Model {trainer.TrainerModelId}";
        SelectedTrainerAi = trainer.Ai.ToString();
        SelectedTrainerBattleStyle = "-";
        SelectedTrainerBattleType = "-";
        SelectedTrainerBattleId = "0";
        SelectedTrainerBgm = "-";
        SelectedTrainerStringIds = $"Name ID: {trainer.NameId}   Pre: {trainer.PreBattleTextId}   Win: {trainer.VictoryTextId}   Loss: {trainer.DefeatTextId}";
        TrainerDetailBackgroundBrush = trainer.HasShadow ? TrainerShadowBrush : TrainerNormalBrush;

        foreach (var pokemon in trainer.Pokemon)
        {
            SelectedTrainerPokemon.Add(new TrainerPokemonSlotViewModel(pokemon));
        }
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
