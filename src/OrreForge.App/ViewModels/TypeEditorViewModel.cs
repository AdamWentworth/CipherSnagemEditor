using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TypeEditorViewModel : ObservableObject
{
    private readonly Action? _changed;
    private bool _isInitializing = true;

    public TypeEditorViewModel(
        ColosseumTypeData type,
        IReadOnlyList<ColosseumTypeData> allTypes,
        Action? changed = null)
    {
        Type = type;
        _changed = changed;
        CategoryOptions =
        [
            new PickerOptionViewModel(0, "Neither"),
            new PickerOptionViewModel(1, "Physical"),
            new PickerOptionViewModel(2, "Special")
        ];
        EffectivenessOptions =
        [
            new PickerOptionViewModel(0x41, "Super Effective"),
            new PickerOptionViewModel(0x3f, "Neutral"),
            new PickerOptionViewModel(0x42, "Not Very Effective"),
            new PickerOptionViewModel(0x43, "No Effect")
        ];

        _nameId = type.NameId;
        _selectedCategory = CategoryOptions.FirstOrDefault(option => option.Value == type.CategoryId) ?? CategoryOptions[0];
        for (var index = 0; index < allTypes.Count; index++)
        {
            var value = index < type.Effectiveness.Count ? type.Effectiveness[index] : 0x3f;
            Effectiveness.Add(new TypeEffectivenessViewModel(allTypes[index].Name, value, EffectivenessOptions, MarkChanged));
        }

        _isInitializing = false;
    }

    public ColosseumTypeData Type { get; }

    public IReadOnlyList<PickerOptionViewModel> CategoryOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EffectivenessOptions { get; }

    public ObservableCollection<TypeEffectivenessViewModel> Effectiveness { get; } = [];

    public string Name => Type.Name;

    public string IndexText => Type.Index.ToString();

    public string HexText => $"0x{Type.Index:X}";

    public string StartOffsetText => $"0x{Type.StartOffset:X}";

    [ObservableProperty]
    private int _nameId;

    [ObservableProperty]
    private PickerOptionViewModel? _selectedCategory;

    [ObservableProperty]
    private bool _hasChanges;

    public ColosseumTypeUpdate ToUpdate()
        => new(
            Type.Index,
            NameId,
            SelectedCategory?.Value ?? Type.CategoryId,
            Effectiveness.Select(effect => effect.Value).ToArray());

    public void MarkSaved()
    {
        HasChanges = false;
    }

    partial void OnNameIdChanged(int value) => MarkChanged();

    partial void OnSelectedCategoryChanged(PickerOptionViewModel? value) => MarkChanged();

    private void MarkChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        HasChanges = true;
        _changed?.Invoke();
    }
}
