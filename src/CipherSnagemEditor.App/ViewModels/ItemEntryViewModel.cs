using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class ItemEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.White;
    private static readonly IBrush TransparentSelectionBrush = SolidColorBrush.Parse("#00000000");
    private static Bitmap? s_itemCellImage;
    private static bool s_itemCellImageLoaded;

    public ItemEntryViewModel(ColosseumItem item)
    {
        Item = item;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumItem Item { get; }

    public Bitmap? ItemCellImage => LoadItemCellImage();

    public string RowText => Item.Name;

    public string SearchText => $"{Item.Index} {Item.Name} {Item.BagSlotName} {Item.TmIndex} {Item.InBattleUseId} {Item.HoldItemId}";

    public IBrush BackgroundBrush => BagSlotBrush(Item.BagSlotId);

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentSelectionBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private static Bitmap? LoadItemCellImage()
    {
        if (s_itemCellImageLoaded)
        {
            return s_itemCellImage;
        }

        s_itemCellImageLoaded = true;
        foreach (var root in CandidateAssetRoots())
        {
            var candidates = new[]
            {
                Path.Combine(root, "assets", "ui", "cells", "item-cell.png")
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    s_itemCellImage = new Bitmap(path);
                    return s_itemCellImage;
                }
                catch
                {
                    return null;
                }
            }
        }

        return null;
    }

    private static IBrush BagSlotBrush(int bagSlot)
        => SolidColorBrush.Parse(bagSlot switch
        {
            0 => "#FFFFFFFF",
            1 => "#FC6848FF",
            2 => "#F8F888FF",
            3 => "#A8E79CFF",
            4 => "#80ACFFFF",
            5 => "#A070FFFF",
            6 => "#FC80F6FF",
            7 => "#C0C0C8FF",
            _ => "#FFFFFFFF"
        });

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
