using System.Text.Json;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed class MoveEditorResources
{
    private readonly Dictionary<int, PickerOptionViewModel> _typeOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _categoryOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _targetOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _animationOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _effectOptionsByValue;
    private readonly Dictionary<int, PickerOptionViewModel> _effectTypeOptionsByValue;

    private MoveEditorResources(
        IReadOnlyList<PickerOptionViewModel> typeOptions,
        IReadOnlyList<PickerOptionViewModel> categoryOptions,
        IReadOnlyList<PickerOptionViewModel> targetOptions,
        IReadOnlyList<PickerOptionViewModel> animationOptions,
        IReadOnlyList<PickerOptionViewModel> effectOptions,
        IReadOnlyList<PickerOptionViewModel> effectTypeOptions)
    {
        TypeOptions = typeOptions;
        CategoryOptions = categoryOptions;
        TargetOptions = targetOptions;
        AnimationOptions = animationOptions;
        EffectOptions = effectOptions;
        EffectTypeOptions = effectTypeOptions;
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
        BuildEffectTypeOptions());

    public IReadOnlyList<PickerOptionViewModel> TypeOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> CategoryOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> TargetOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> AnimationOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EffectOptions { get; }

    public IReadOnlyList<PickerOptionViewModel> EffectTypeOptions { get; }

    public static MoveEditorResources FromCommonRel(ColosseumCommonRel commonRel)
    {
        var moves = commonRel.Moves;
        var typeOptions = commonRel.Types
            .Select(type => new PickerOptionViewModel(type.Index, type.Name))
            .ToArray();

        var maxEffect = Math.Max(0, moves.Count == 0 ? 0 : moves.Max(move => move.EffectId));
        var maxAnimation = Math.Max(0, moves.Count == 0 ? 0 : moves.Max(move => move.AnimationId));

        return new MoveEditorResources(
            typeOptions,
            BuildCategoryOptions(),
            BuildTargetOptions(),
            BuildJsonBackedOptions("Original Moves.json", maxAnimation, index => index == 0 ? "-" : $"Animation {index}"),
            BuildJsonBackedOptions("Move Effects.json", maxEffect, index => index == 0 ? "-" : $"Effect {index}"),
            BuildEffectTypeOptions());
    }

    public PickerOptionViewModel TypeOption(int value)
        => OptionFor(_typeOptionsByValue, value, $"Type {value}");

    public PickerOptionViewModel CategoryOption(int value)
        => OptionFor(_categoryOptionsByValue, value, $"Category {value}");

    public PickerOptionViewModel TargetOption(int value)
        => OptionFor(_targetOptionsByValue, value, $"Target {value}");

    public PickerOptionViewModel AnimationOption(int value)
        => OptionFor(_animationOptionsByValue, value, value == 0 ? "-" : $"Animation {value}");

    public PickerOptionViewModel EffectOption(int value)
        => OptionFor(_effectOptionsByValue, value, value == 0 ? "-" : $"Effect {value}");

    public PickerOptionViewModel EffectTypeOption(int value)
        => OptionFor(_effectTypeOptionsByValue, value, $"Effect Type {value}");

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
        Func<int, string> fallbackName)
    {
        var labels = LoadJsonLabels(fileName);
        var count = Math.Max(minimumMaximumIndex + 1, labels.Count);
        var options = new PickerOptionViewModel[count];
        for (var index = 0; index < count; index++)
        {
            var label = index < labels.Count && !string.IsNullOrWhiteSpace(labels[index])
                ? labels[index]
                : fallbackName(index);
            options[index] = new PickerOptionViewModel(index, label);
        }

        return options;
    }

    private static IReadOnlyList<string> LoadJsonLabels(string fileName)
    {
        foreach (var root in CandidateAssetRoots())
        {
            var path = Path.Combine(root, "legacy-assets", "json", "Colosseum", fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? [];
            }
            catch
            {
                return [];
            }
        }

        return [];
    }

    private static IReadOnlyList<PickerOptionViewModel> BuildCategoryOptions()
        =>
        [
            new(0, "Neither"),
            new(1, "Physical"),
            new(2, "Special")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildTargetOptions()
        =>
        [
            new(0, "Selected Target"),
            new(1, "Depends On Move"),
            new(2, "All Pokemon"),
            new(3, "Random"),
            new(4, "Both Foes"),
            new(5, "User"),
            new(6, "Both Foes and Ally"),
            new(7, "Opponent's Feet")
        ];

    private static IReadOnlyList<PickerOptionViewModel> BuildEffectTypeOptions()
        =>
        [
            new(0x00, "None"),
            new(0x01, "Attack"),
            new(0x02, "Healing"),
            new(0x03, "Stat Nerf"),
            new(0x04, "Stat Buff"),
            new(0x05, "Status Effect"),
            new(0x06, "Field Effect"),
            new(0x07, "Affects Incoming Move"),
            new(0x08, "OHKO"),
            new(0x09, "Multi-Turn"),
            new(0x0a, "Misc"),
            new(0x0b, "Misc2"),
            new(0x0c, "Misc3"),
            new(0x0d, "Misc4"),
            new(0x0e, "Unknown")
        ];

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
