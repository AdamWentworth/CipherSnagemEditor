using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class PokemonStatsTmViewModel : ObservableObject
{
    private static readonly IBrush BorderBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly IBrush TransparentBorderBrush = SolidColorBrush.Parse("#00FFFFFF");
    private readonly Action? _changed;
    private Bitmap? _typeImage;
    private bool _typeImageLoaded;

    public PokemonStatsTmViewModel(ColosseumTmMove tm, bool isLearnable, Action? changed)
    {
        Tm = tm;
        _isLearnable = isLearnable;
        _changed = changed;
    }

    public ColosseumTmMove Tm { get; }

    public string MoveName => Tm.MoveName;

    public Bitmap? TypeImage
    {
        get
        {
            if (!_typeImageLoaded)
            {
                _typeImage = RuntimeImageAssets.LoadImage("Types", $"type_{Tm.TypeId}.png");
                _typeImageLoaded = true;
            }

            return _typeImage;
        }
    }

    public IBrush TypeFallbackBrush => FallbackForType(Tm.TypeId);

    public Thickness LearnableBorderThickness => IsLearnable ? new Thickness(1) : new Thickness(0);

    public IBrush LearnableBorderBrush => IsLearnable ? BorderBrush : TransparentBorderBrush;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LearnableBorderThickness))]
    [NotifyPropertyChangedFor(nameof(LearnableBorderBrush))]
    private bool _isLearnable;

    partial void OnIsLearnableChanged(bool value)
    {
        _changed?.Invoke();
    }

    private static IBrush FallbackForType(int typeId)
        => SolidColorBrush.Parse(typeId switch
        {
            0 => "#D0D0D0",
            1 => "#E0A060",
            2 => "#B8D8FF",
            3 => "#E080F8",
            4 => "#E8C878",
            5 => "#C8B070",
            6 => "#A8E868",
            7 => "#A070FF",
            8 => "#B0B0C0",
            9 => "#C0C0C8",
            10 => "#FC6848",
            11 => "#80ACFF",
            12 => "#20F020",
            13 => "#F8F888",
            14 => "#FF90C8",
            15 => "#B8F0FF",
            16 => "#B090F8",
            17 => "#707080",
            _ => "#80ACFF"
        });

}
