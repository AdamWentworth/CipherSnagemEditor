using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdShadowPokemonEntryViewModel : ObservableObject
{
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#20F020");
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private Bitmap? _faceImage;
    private Bitmap? _bodyImage;
    private bool _faceLoaded;
    private bool _bodyLoaded;

    public XdShadowPokemonEntryViewModel(XdShadowPokemonRecord shadow)
    {
        Shadow = shadow;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public XdShadowPokemonRecord Shadow { get; }

    public Bitmap? FaceImage
    {
        get
        {
            if (!_faceLoaded)
            {
                _faceImage = RuntimeImageAssets.LoadImage("PokeFace", $"face_{Shadow.SpeciesId:000}.png");
                _faceLoaded = true;
            }

            return _faceImage;
        }
    }

    public Bitmap? BodyImage
    {
        get
        {
            if (!_bodyLoaded)
            {
                _bodyImage = RuntimeImageAssets.LoadBodyFrames(Shadow.SpeciesId).FirstOrDefault()?.Image;
                _bodyLoaded = true;
            }

            return _bodyImage;
        }
    }

    public string RowText => $"{Shadow.Index}: Shadow {Shadow.SpeciesName}{Environment.NewLine}Lv. {Shadow.Level}+";

    public string SearchText => $"{Shadow.Index} {Shadow.SpeciesId} {Shadow.SpeciesName} {Shadow.StoryPokemonIndex} {Shadow.ItemName} {string.Join(' ', Shadow.RegularMoveNames)} {string.Join(' ', Shadow.MoveNames)}";

    public string StoryPokemonText => $"{Shadow.StoryPokemonIndex}: Lv. {Shadow.ShadowBoostLevel} {Shadow.SpeciesName}";

    public string LevelText => $"Lv. {Shadow.Level}";

    public string ShadowBoostText => $"Lv. {Shadow.ShadowBoostLevel}";

    public string ItemText => Shadow.ItemId == 0 ? "---" : Shadow.ItemName;

    public string AbilityText => Shadow.AbilityName;

    public string NatureText => $"{NatureName(Shadow.Nature)} ({Shadow.Nature})";

    public string GenderText => $"{GenderName(Shadow.Gender)} ({Shadow.Gender})";

    public string RegularMove1Text => MoveText(Shadow.RegularMoveNames, 0);

    public string RegularMove2Text => MoveText(Shadow.RegularMoveNames, 1);

    public string RegularMove3Text => MoveText(Shadow.RegularMoveNames, 2);

    public string RegularMove4Text => MoveText(Shadow.RegularMoveNames, 3);

    public string ShadowMove1Text => MoveText(Shadow.MoveNames, 0);

    public string ShadowMove2Text => MoveText(Shadow.MoveNames, 1);

    public string ShadowMove3Text => MoveText(Shadow.MoveNames, 2);

    public string ShadowMove4Text => MoveText(Shadow.MoveNames, 3);

    public int HpEv => Ev(0);

    public int AttackEv => Ev(1);

    public int DefenseEv => Ev(2);

    public int SpecialAttackEv => Ev(3);

    public int SpecialDefenseEv => Ev(4);

    public int SpeedEv => Ev(5);

    public IBrush BackgroundBrush => IsSelected ? SelectedBrush : ShadowBrush;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private int Ev(int index)
        => index < Shadow.Evs.Count ? Shadow.Evs[index] : 0;

    private static string MoveText(IReadOnlyList<string> moves, int index)
        => index < moves.Count && moves[index] != "Move 0" ? moves[index] : "-";

    private static string NatureName(int value)
        => value switch
        {
            0x00 => "Hardy",
            0x01 => "Lonely",
            0x02 => "Brave",
            0x03 => "Adamant",
            0x04 => "Naughty",
            0x05 => "Bold",
            0x06 => "Docile",
            0x07 => "Relaxed",
            0x08 => "Impish",
            0x09 => "Lax",
            0x0a => "Timid",
            0x0b => "Hasty",
            0x0c => "Serious",
            0x0d => "Jolly",
            0x0e => "Naive",
            0x0f => "Modest",
            0x10 => "Mild",
            0x11 => "Quiet",
            0x12 => "Bashful",
            0x13 => "Rash",
            0x14 => "Calm",
            0x15 => "Gentle",
            0x16 => "Sassy",
            0x17 => "Careful",
            0x18 => "Quirky",
            _ => $"Nature {value}"
        };

    private static string GenderName(int value)
        => value switch
        {
            0 => "Male",
            1 => "Female",
            2 => "Genderless",
            3 => "Random",
            _ => $"Gender {value}"
        };
}
