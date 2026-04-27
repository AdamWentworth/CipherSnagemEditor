namespace OrreForge.App.ViewModels;

public sealed class PickerOptionViewModel
{
    public PickerOptionViewModel(int value, string name)
    {
        Value = value;
        Name = string.IsNullOrWhiteSpace(name) ? "-" : name;
    }

    public int Value { get; }

    public string Name { get; }

    public string Label => Value == 0 ? Name : $"{Name} ({Value})";

    public override string ToString() => Label;
}
