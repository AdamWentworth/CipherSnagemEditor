using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TypeEffectivenessViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public TypeEffectivenessViewModel(
        string typeName,
        int value,
        IReadOnlyList<PickerOptionViewModel> options,
        Action? changed)
    {
        TypeName = typeName;
        Options = options;
        _changed = changed;
        _selectedEffectiveness = options.FirstOrDefault(option => option.Value == value)
            ?? options.FirstOrDefault(option => option.Value == 0x3f)
            ?? options.FirstOrDefault();
        _isInitializing = false;
    }

    public string TypeName { get; }

    public IReadOnlyList<PickerOptionViewModel> Options { get; }

    [ObservableProperty]
    private PickerOptionViewModel? _selectedEffectiveness;

    public int Value => SelectedEffectiveness?.Value ?? 0x3f;

    partial void OnSelectedEffectivenessChanged(PickerOptionViewModel? value)
    {
        if (_isInitializing)
        {
            return;
        }

        _changed?.Invoke();
    }
}
