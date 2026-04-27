using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrreForge.Colosseum;

namespace OrreForge.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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
    private bool _showIsoExplorer;

    [ObservableProperty]
    private string _isoExplorerStatus = "Open a Colosseum ISO to browse its files.";

    [ObservableProperty]
    private string _leftPanelTitle = "Tools";

    [ObservableProperty]
    private bool _showHomeTools = true;

    [ObservableProperty]
    private bool _showToolPlaceholder = true;

    [ObservableProperty]
    private string _isoExplorerFilesText = "-";

    [ObservableProperty]
    private string _selectedIsoFileName = "File name: -";

    [ObservableProperty]
    private string _selectedIsoFileSize = "File size: -";

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
            SelectedIsoFile = null;
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
        ShowHomeTools = true;
        ShowToolPlaceholder = true;
        LeftPanelTitle = "Tools";
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
            ShowHomeTools = true;
            ShowToolPlaceholder = true;
            LeftPanelTitle = "Tools";
            return;
        }

        SelectedToolDetail = $"{value.Title}\nLegacy segue: {value.LegacySegue}\nReference: {value.LegacySource}";
        ShowIsoExplorer = value.Title == "ISO Explorer" && CurrentProject?.Iso is not null;
        ShowHomeTools = !ShowIsoExplorer;
        ShowToolPlaceholder = !ShowIsoExplorer;
        LeftPanelTitle = ShowIsoExplorer ? "Files" : "Tools";
        if (value.Title == "ISO Explorer" && CurrentProject?.Iso is null)
        {
            IsoExplorerStatus = "Open a Colosseum ISO to browse its files.";
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
