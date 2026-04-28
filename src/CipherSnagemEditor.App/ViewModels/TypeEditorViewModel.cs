using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

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
            new PickerOptionViewModel(0x41, "Super Effective (65)"),
            new PickerOptionViewModel(0x3f, "Neutral (63)"),
            new PickerOptionViewModel(0x42, "Not Very Effective (66)"),
            new PickerOptionViewModel(0x43, "No Effect (67)")
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

    public bool IsPhysicalCategory
    {
        get => SelectedCategory?.Value == 1;
        set
        {
            if (value)
            {
                SelectCategory(1);
            }
        }
    }

    public bool IsSpecialCategory
    {
        get => SelectedCategory?.Value == 2;
        set
        {
            if (value)
            {
                SelectCategory(2);
            }
        }
    }

    public bool IsNeitherCategory
    {
        get => SelectedCategory?.Value == 0;
        set
        {
            if (value)
            {
                SelectCategory(0);
            }
        }
    }

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

    partial void OnSelectedCategoryChanged(PickerOptionViewModel? value)
    {
        OnPropertyChanged(nameof(IsPhysicalCategory));
        OnPropertyChanged(nameof(IsSpecialCategory));
        OnPropertyChanged(nameof(IsNeitherCategory));
        MarkChanged();
    }

    private void SelectCategory(int value)
    {
        if (SelectedCategory?.Value == value)
        {
            return;
        }

        SelectedCategory = CategoryOptions.First(option => option.Value == value);
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
}
