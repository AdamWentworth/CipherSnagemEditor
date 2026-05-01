using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class InteractionEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;
    private int _triggerMethodId;

    public InteractionEditorViewModel(
        ColosseumInteractionPoint interaction,
        InteractionEditorResources resources,
        Action? changed = null)
    {
        Interaction = interaction;
        Resources = resources;
        _changed = changed;
        _triggerMethodId = interaction.InteractionMethodId;
        _selectedRoom = resources.RoomOption(interaction.RoomId);
        _regionId = interaction.RegionId;
        _infoKind = interaction.InfoKind;
        var scriptType = ScriptTypeFor(interaction.InfoKind);
        _selectedScriptType = resources.ScriptTypeOption(scriptType);
        _scriptIndexOptions = resources.ScriptOptionsFor(scriptType);
        _selectedScriptIndex = resources.ScriptIndexOption(scriptType, interaction.ScriptIndex);
        _selectedTargetRoom = resources.RoomOption(interaction.TargetRoomId);
        _selectedDirection = resources.DirectionOption(interaction.DirectionId);
        _sound = interaction.Sound;
        LoadFields(interaction);
        RefreshInfoLayout();
        _isInitializing = false;
    }

    public ColosseumInteractionPoint Interaction { get; }

    public InteractionEditorResources Resources { get; }

    public IReadOnlyList<PickerOptionViewModel> RoomOptions => Resources.RoomOptions;

    public IReadOnlyList<PickerOptionViewModel> ScriptTypeOptions => Resources.ScriptTypeOptions;

    public IReadOnlyList<PickerOptionViewModel> DirectionOptions => Resources.DirectionOptions;

    public string TriggerNoneTitle => "Press A Button (2)";

    public bool IsTriggerPressA2Visible => Resources.SupportsPressA2;

    public bool IsTriggerPressA2
    {
        get => _triggerMethodId == 4;
        set
        {
            if (value)
            {
                SetTriggerMethod(4);
            }
        }
    }

    public bool IsTriggerPressA
    {
        get => _triggerMethodId == 3;
        set
        {
            if (value)
            {
                SetTriggerMethod(3);
            }
        }
    }

    public bool IsTriggerWalkUp
    {
        get => _triggerMethodId == 2;
        set
        {
            if (value)
            {
                SetTriggerMethod(2);
            }
        }
    }

    public bool IsTriggerWalkThrough
    {
        get => _triggerMethodId == 1;
        set
        {
            if (value)
            {
                SetTriggerMethod(1);
            }
        }
    }

    [ObservableProperty]
    private PickerOptionViewModel? _selectedRoom;

    [ObservableProperty]
    private int _regionId;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedScriptType;

    [ObservableProperty]
    private IReadOnlyList<PickerOptionViewModel> _scriptIndexOptions = [];

    [ObservableProperty]
    private PickerOptionViewModel? _selectedScriptIndex;

    [ObservableProperty]
    private ColosseumInteractionInfoKind _infoKind;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedTargetRoom;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedDirection;

    [ObservableProperty]
    private bool _sound;

    [ObservableProperty]
    private string _label1 = "Parameter1";

    [ObservableProperty]
    private string _label2 = "Parameter2";

    [ObservableProperty]
    private string _label3 = "Parameter3";

    [ObservableProperty]
    private string _label4 = "Parameter4";

    [ObservableProperty]
    private int _field1;

    [ObservableProperty]
    private int _field2;

    [ObservableProperty]
    private int _field3;

    [ObservableProperty]
    private int _field4;

    [ObservableProperty]
    private bool _hasChanges;

    public bool IsField1Visible { get; private set; }

    public bool IsField2Visible { get; private set; }

    public bool IsField3Visible { get; private set; }

    public bool IsField4Visible { get; private set; }

    public bool IsTargetRoomVisible { get; private set; }

    public bool IsSoundVisible { get; private set; }

    public bool IsDirectionVisible { get; private set; }

    public bool IsTextPreviewVisible { get; private set; }

    public string TextPreview => Interaction.InfoKind == ColosseumInteractionInfoKind.Text
        ? Interaction.Description
        : string.Empty;

    public ColosseumInteractionPointUpdate ToUpdate()
    {
        var targetRoom = SelectedTargetRoom?.Value ?? Interaction.TargetRoomId;
        var scriptIndex = SelectedScriptIndex?.Value ?? Interaction.ScriptIndex;
        var targetEntry = 0;
        var doorId = 0;
        var elevatorId = 0;
        var targetElevatorId = 0;
        var stringId = 0;
        var cutsceneId = 0;
        var cameraId = 0;
        var pcUnknown = 0;
        var parameter1 = 0u;
        var parameter2 = 0u;
        var parameter3 = 0u;
        var parameter4 = 0u;

        switch (InfoKind)
        {
            case ColosseumInteractionInfoKind.Warp:
                targetEntry = Field1;
                break;
            case ColosseumInteractionInfoKind.Door:
                doorId = Field1;
                break;
            case ColosseumInteractionInfoKind.Text:
                stringId = Field1;
                break;
            case ColosseumInteractionInfoKind.Elevator:
                elevatorId = Field1;
                targetElevatorId = Field2;
                break;
            case ColosseumInteractionInfoKind.CutsceneWarp:
                targetEntry = Field1;
                cutsceneId = Field2;
                cameraId = Field3;
                break;
            case ColosseumInteractionInfoKind.Pc:
                pcUnknown = Field1;
                break;
            case ColosseumInteractionInfoKind.CurrentScript:
            case ColosseumInteractionInfoKind.CommonScript:
                parameter1 = UIntField(Field1);
                parameter2 = UIntField(Field2);
                parameter3 = UIntField(Field3);
                parameter4 = UIntField(Field4);
                break;
        }

        return new ColosseumInteractionPointUpdate(
            Interaction.Index,
            SelectedRoom?.Value ?? Interaction.RoomId,
            RegionId,
            _triggerMethodId,
            InfoKind,
            scriptIndex,
            targetRoom,
            targetEntry,
            Sound,
            doorId,
            elevatorId,
            targetElevatorId,
            SelectedDirection?.Value ?? 0,
            stringId,
            cutsceneId,
            cameraId,
            pcUnknown,
            parameter1,
            parameter2,
            parameter3,
            parameter4);
    }

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnSelectedRoomChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnRegionIdChanged(int value) => MarkChanged();

    partial void OnSelectedScriptTypeChanged(PickerOptionViewModel? value)
    {
        if (_isInitializing)
        {
            return;
        }

        var scriptType = value?.Value ?? 0;
        ScriptIndexOptions = Resources.ScriptOptionsFor(scriptType);
        SelectedScriptIndex = ScriptIndexOptions.FirstOrDefault();
        ApplyInfoKindFromSelection();
    }

    partial void OnSelectedScriptIndexChanged(PickerOptionViewModel? value)
    {
        if (_isInitializing)
        {
            return;
        }

        ApplyInfoKindFromSelection();
    }

    partial void OnSelectedTargetRoomChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSelectedDirectionChanged(PickerOptionViewModel? value) => MarkChanged();

    partial void OnSoundChanged(bool value) => MarkChanged();

    partial void OnField1Changed(int value) => MarkChanged();

    partial void OnField2Changed(int value) => MarkChanged();

    partial void OnField3Changed(int value) => MarkChanged();

    partial void OnField4Changed(int value) => MarkChanged();

    private static int ScriptTypeFor(ColosseumInteractionInfoKind infoKind)
        => infoKind switch
        {
            ColosseumInteractionInfoKind.None => 0,
            ColosseumInteractionInfoKind.CurrentScript => 2,
            _ => 1
        };

    private void SetTriggerMethod(int methodId)
    {
        if (_triggerMethodId == methodId)
        {
            return;
        }

        _triggerMethodId = methodId;
        OnPropertyChanged(nameof(IsTriggerPressA2));
        OnPropertyChanged(nameof(IsTriggerPressA));
        OnPropertyChanged(nameof(IsTriggerWalkUp));
        OnPropertyChanged(nameof(IsTriggerWalkThrough));
        MarkChanged();
    }

    private void ApplyInfoKindFromSelection()
    {
        var scriptType = SelectedScriptType?.Value ?? 0;
        var scriptIndex = SelectedScriptIndex?.Value ?? 0;
        InfoKind = scriptType switch
        {
            0 => ColosseumInteractionInfoKind.None,
            2 => ColosseumInteractionInfoKind.CurrentScript,
            _ => Resources.CommonInfoKindForScriptIndex(scriptIndex)
        };

        LoadDefaultFields();
        RefreshInfoLayout();
        MarkChanged();
    }

    private void LoadFields(ColosseumInteractionPoint interaction)
    {
        switch (interaction.InfoKind)
        {
            case ColosseumInteractionInfoKind.Warp:
                Field1 = interaction.TargetEntryId;
                break;
            case ColosseumInteractionInfoKind.Door:
                Field1 = interaction.DoorId;
                Field2 = 0;
                break;
            case ColosseumInteractionInfoKind.Text:
                Field1 = interaction.StringId;
                break;
            case ColosseumInteractionInfoKind.Elevator:
                Field1 = interaction.ElevatorId;
                Field2 = interaction.TargetElevatorId;
                break;
            case ColosseumInteractionInfoKind.CutsceneWarp:
                Field1 = interaction.TargetEntryId;
                Field2 = interaction.CutsceneId;
                Field3 = interaction.CameraFsysId;
                break;
            case ColosseumInteractionInfoKind.Pc:
                Field1 = interaction.PcUnknown;
                break;
            case ColosseumInteractionInfoKind.CurrentScript:
            case ColosseumInteractionInfoKind.CommonScript:
                Field1 = IntField(interaction.Parameter1);
                Field2 = IntField(interaction.Parameter2);
                Field3 = IntField(interaction.Parameter3);
                Field4 = IntField(interaction.Parameter4);
                break;
            default:
                LoadDefaultFields();
                break;
        }
    }

    private void LoadDefaultFields()
    {
        Field1 = 0;
        Field2 = 0;
        Field3 = 0;
        Field4 = 0;
        Sound = false;
        SelectedTargetRoom = Resources.RoomOption(0xaf);
        SelectedDirection = Resources.DirectionOption(0);
    }

    private void RefreshInfoLayout()
    {
        Label1 = InfoKind switch
        {
            ColosseumInteractionInfoKind.Warp => "Entry point ID",
            ColosseumInteractionInfoKind.Door => "Door ID",
            ColosseumInteractionInfoKind.Text => "String ID",
            ColosseumInteractionInfoKind.Elevator => "Elevator ID",
            ColosseumInteractionInfoKind.CutsceneWarp => "Entry ID",
            ColosseumInteractionInfoKind.Pc => "Unknown",
            _ => "Parameter1"
        };
        Label2 = InfoKind switch
        {
            ColosseumInteractionInfoKind.Door => "File Identifier",
            ColosseumInteractionInfoKind.Elevator => "Target Elevator ID",
            ColosseumInteractionInfoKind.CutsceneWarp => "Cutscene ID",
            _ => "Parameter2"
        };
        Label3 = InfoKind == ColosseumInteractionInfoKind.CutsceneWarp ? "Camera ID" : "Parameter3";
        Label4 = "Parameter4";

        IsField1Visible = InfoKind is not ColosseumInteractionInfoKind.None;
        IsField2Visible = InfoKind is ColosseumInteractionInfoKind.Door
            or ColosseumInteractionInfoKind.Elevator
            or ColosseumInteractionInfoKind.CutsceneWarp
            or ColosseumInteractionInfoKind.CurrentScript
            or ColosseumInteractionInfoKind.CommonScript;
        IsField3Visible = InfoKind is ColosseumInteractionInfoKind.CutsceneWarp
            or ColosseumInteractionInfoKind.CurrentScript
            or ColosseumInteractionInfoKind.CommonScript;
        IsField4Visible = InfoKind is ColosseumInteractionInfoKind.CurrentScript
            or ColosseumInteractionInfoKind.CommonScript;
        IsTargetRoomVisible = InfoKind is ColosseumInteractionInfoKind.Warp
            or ColosseumInteractionInfoKind.Elevator
            or ColosseumInteractionInfoKind.CutsceneWarp
            or ColosseumInteractionInfoKind.Pc;
        IsSoundVisible = InfoKind == ColosseumInteractionInfoKind.Warp;
        IsDirectionVisible = InfoKind == ColosseumInteractionInfoKind.Elevator;
        IsTextPreviewVisible = InfoKind == ColosseumInteractionInfoKind.Text && !string.IsNullOrWhiteSpace(TextPreview);

        OnPropertyChanged(nameof(IsField1Visible));
        OnPropertyChanged(nameof(IsField2Visible));
        OnPropertyChanged(nameof(IsField3Visible));
        OnPropertyChanged(nameof(IsField4Visible));
        OnPropertyChanged(nameof(IsTargetRoomVisible));
        OnPropertyChanged(nameof(IsSoundVisible));
        OnPropertyChanged(nameof(IsDirectionVisible));
        OnPropertyChanged(nameof(IsTextPreviewVisible));
        OnPropertyChanged(nameof(TextPreview));
    }

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        HasChanges = true;
        _changed?.Invoke();
    }

    private static int IntField(uint value)
        => value > int.MaxValue ? int.MaxValue : (int)value;

    private static uint UIntField(int value)
        => checked((uint)Math.Max(0, value));
}
