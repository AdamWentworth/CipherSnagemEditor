using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace OrreForge.App.Controls;

public sealed class LegacyPokemonComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);

    public LegacyPokemonComboBox()
    {
        MinHeight = 0;
        Height = 20;
        Padding = new Thickness(3, 0, 20, 0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }
}
