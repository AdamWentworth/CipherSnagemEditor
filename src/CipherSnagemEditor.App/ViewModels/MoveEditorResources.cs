using System.Text.Json;
using System.Text.RegularExpressions;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed class MoveEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _typeOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _categoryOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _targetOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _animationOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _effectOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _effectTypeOptionsByValue;
    private readonly IReadOnlyDictionary<int, int> _typeCategoryByValue;

    private MoveEditorResources(
        IReadOnlyList<PickerOptionViewModel> typeOptions,
        IReadOnlyList<PickerOptionViewModel> categoryOptions,
        IReadOnlyList<PickerOptionViewModel> targetOptions,
        IReadOnlyList<PickerOptionViewModel> animationOptions,
        IReadOnlyList<PickerOptionViewModel> effectOptions,
        IReadOnlyList<PickerOptionViewModel> effectTypeOptions,
        IReadOnlyDictionary<int, int> typeCategoryByValue,
        bool isPhysicalSpecialSplitImplemented)
    {
        TypeOptions = typeOptions;
        CategoryOptions = categoryOptions;
        TargetOptions = targetOptions;
        AnimationOptions = animationOptions;
        EffectOptions = effectOptions;
        EffectTypeOptions = effectTypeOptions;
        IsPhysicalSpecialSplitImplemented = isPhysicalSpecialSplitImplemented;
        _typeCategoryByValue = typeCategoryByValue;
        _typeOptionsByValue = typeOptions.ToDictionary(option => option.Value);
        _categoryOptionsByValue = categoryOptions.ToDictionary(option => option.Value);
        _targetOptionsByValue = targetOptions.ToDictionary(option => option.Value);
        _animationOptionsByValue = animationOptions.ToDictionary(option => option.Value);
        _effectOptionsByValue = effectOptions.ToDictionary(option => option.Value);
        _effectTypeOptionsByValue = effectTypeOptions.ToDictionary(option => option.Value);
    }

    public static MoveEditorResources Empty { get; } = new(
        [new PickerOptionViewModel(0, "Normal")],
        BuildCategoryOptions(),
        BuildTargetOptions(),
        [new PickerOptionViewModel(0, "-")],
        [new PickerOptionViewModel(0, "-")],
        BuildEffectTypeOptions(),
        new Dictionary<int, int>(),
        false);

    public IReadOnlyList<PickerOptionViewModel> TypeOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> CategoryOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> TargetOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> AnimationOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EffectOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EffectTypeOptions { get; }

    public bool IsPhysicalSpecialSplitImplemented { get; }

    public static MoveEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var moves = commonRel.Moves;
        var typeCategoryByValue = commonRel.TypeData.ToDictionary(type => type.Index, type => type.CategoryId);
        var typeOptions = commonRel.Types
            .Select(type => new PickerOptionViewModel(type.Index, $"{type.Name.ToUpperInvariant()} ({type.Index})"))
            .ToArray();

        var maxEffect = Math.Max(0, moves.Count == 0 ? 0 : moves.Max(move => move.EffectId));
        var maxAnimation = Math.Max(0, moves.Count == 0 ? 0 : moves.Max(move => move.AnimationId));

        return new MoveEditorResources(
            typeOptions,
            BuildCategoryOptions(),
            BuildTargetOptions(),
            BuildJsonBackedOptions("Original Moves.json", maxAnimation, index => index == 0 ? "-" : $"Animation {index}", appendIndex: true),
            BuildJsonBackedOptions("Move Effects.json", maxEffect, index => index == 0 ? "-" : $"Effect {index}", appendIndex: true),
            BuildEffectTypeOptions(),
            typeCategoryByValue,
            commonRel.IsPhysicalSpecialSplitImplemented);
    }

    public static MoveEditorResources FromRows(
        IReadOnlyList<ColosseumMove> moveRows,
        IReadOnlyList<ColosseumTypeData> typeRows)
    {
        var typeCategoryByValue = typeRows.ToDictionary(type => type.Index, type => type.CategoryId);
        var typeOptions = typeRows
            .Select(type => new PickerOptionViewModel(type.Index, $"{type.Name.ToUpperInvariant()} ({type.Index})"))
            .ToArray();
        var maxEffect = Math.Max(0, moveRows.Count == 0 ? 0 : moveRows.Max(move => move.EffectId));
        var maxAnimation = Math.Max(0, moveRows.Count == 0 ? 0 : moveRows.Max(move => move.AnimationId));

        return new MoveEditorResources(
            typeOptions.Length == 0 ? [new PickerOptionViewModel(0, "Normal")] : typeOptions,
            BuildCategoryOptions(),
            BuildTargetOptions(),
            BuildJsonBackedOptions("Original Moves.json", maxAnimation, index => index == 0 ? "-" : $"Animation {index}", appendIndex: true),
            BuildJsonBackedOptions("Move Effects.json", maxEffect, index => index == 0 ? "-" : $"Effect {index}", appendIndex: true),
            BuildEffectTypeOptions(),
            typeCategoryByValue,
            true);
    }

    public PickerOptionViewModel TypeOption(int value)
        => OptionFor(_typeOptionsByValue, value, $"TYPE ({value})");

    public PickerOptionViewModel CategoryOption(int value)
        => OptionFor(_categoryOptionsByValue, value, $"Category ({value})");

    public PickerOptionViewModel TargetOption(int value)
        => OptionFor(_targetOptionsByValue, value, $"Target ({value})");

    public PickerOptionViewModel AnimationOption(int value)
        => OptionFor(_animationOptionsByValue, value, value == 0 ? "-" : $"Animation {value}");

    public PickerOptionViewModel EffectOption(int value)
        => OptionFor(_effectOptionsByValue, value, value == 0 ? "-" : $"Effect {value}");

    public PickerOptionViewModel EffectTypeOption(int value)
        => OptionFor(_effectTypeOptionsByValue, value, $"Effect Type {value}");

    public int CategoryForType(int typeId)
        => _typeCategoryByValue.TryGetValue(typeId, out var categoryId)
            ? categoryId
            : 0;

    private static PickerOptionViewModel OptionFor(
        IReadOnlyDictionary<int, PickerOptionViewModel> options,
        int value,
        string fallbackName)
        => options.TryGetValue(value, out var option)
            ? option
            : new PickerOptionViewModel(value, fallbackName);

    private static IReadOnlyList<PickerOptionViewModel> BuildJsonBackedOptions(
        string fileName,
        int minimumMaximumIndex,
        Func<int, string> fallbackName,
        bool appendIndex)
    {
        var labels = LoadJsonLabels(fileName);
        var count = Math.Max(minimumMaximumIndex + 1, labels.Count);
        var options = new PickerOptionViewModel[count];
        for (var index = 0; index < count; index++)
        {
            var baseLabel = index < labels.Count && !string.IsNullOrWhiteSpace(labels[index])
                ? labels[index]
                : fallbackName(index);
            var label = appendIndex && index > 0 && !EndsWithNumericSuffix(baseLabel, index)
                ? $"{baseLabel} {index}"
                : baseLabel;
            options[index] = new PickerOptionViewModel(index, label);
        }

        return options;
    }

    private static IReadOnlyList<string> LoadJsonLabels(string fileName)
    {
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "assets", "json", "Colosseum", fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var text = File.ReadAllText(path);
                try
                {
                    return JsonSerializer.Deserialize<string[]>(text) ?? [];
                }
                catch (JsonException)
                {
                    return ExtractQuotedLabels(text);
                }
            }
            catch
            {
                return [];
            }
        }

        return [];
    }

    private static IReadOnlyList<string> ExtractQuotedLabels(string text)
        => Regex.Matches(text, "\"(?:\\\\.|[^\"])*\"")
            .Select(match =>
            {
                try
                {
                    return JsonSerializer.Deserialize<string>(match.Value) ?? string.Empty;
                }
                catch (JsonException)
                {
                    return match.Value.Trim('"');
                }
            })
            .ToArray();

    private static IReadOnlyList<PickerOptionViewModel> BuildCategoryOptions()
        =>
        [
            new(0, "Neither (0)"),
            new(1, "Physical (1)"),
            new(2, "Special (2)")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildTargetOptions()
        =>
        [
            new(0, "Selected Target (0)"),
            new(1, "Depends On Move (1)"),
            new(2, "All Pokemon (2)"),
            new(3, "Random (3)"),
            new(4, "Both Foes (4)"),
            new(5, "User (5)"),
            new(6, "Both Foes and Ally (6)"),
            new(7, "Opponent's Feet (7)")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildEffectTypeOptions()
        =>
        [
            new(0x00, "None (0)"),
            new(0x01, "Attack (1)"),
            new(0x02, "Healing (2)"),
            new(0x03, "Stat Nerf (3)"),
            new(0x04, "Stat Buff (4)"),
            new(0x05, "Status Effect (5)"),
            new(0x06, "Field Effect (6)"),
            new(0x07, "Affects Incoming Move (7)"),
            new(0x08, "OHKO (8)"),
            new(0x09, "Multi-Turn (9)"),
            new(0x0a, "Misc (10)"),
            new(0x0b, "Misc2 (11)"),
            new(0x0c, "Misc3 (12)"),
            new(0x0d, "Misc4 (13)"),
            new(0x0e, "Unknown (14)")
        ];

    private static bool EndsWithNumericSuffix(string label, int index)
        => label.EndsWith($" {index}", StringComparison.Ordinal)
            || label.EndsWith($"({index})", StringComparison.Ordinal);

    private static IEnumerable<string> CandidateAssetRoots()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        foreach (var root in roots)
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                yield return current.FullName;
                current = current.Parent;
            }
        }
    }
}
