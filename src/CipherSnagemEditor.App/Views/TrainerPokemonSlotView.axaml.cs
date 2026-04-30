using System.ComponentModel;
using Avalonia.Controls;
using CipherSnagemEditor.App.ViewModels;

namespace CipherSnagemEditor.App.Views;

public partial class TrainerPokemonSlotView : UserControl
{
    private TrainerPokemonSlotViewModel? _slot;
    private bool? _showFullSlot;

    public TrainerPokemonSlotView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachSlot(DataContext as TrainerPokemonSlotViewModel);
        AttachedToVisualTree += (_, _) => AttachSlot(DataContext as TrainerPokemonSlotViewModel);
        DetachedFromVisualTree += (_, _) => AttachSlot(null);
    }

    private void AttachSlot(TrainerPokemonSlotViewModel? slot)
    {
        if (ReferenceEquals(_slot, slot))
        {
            return;
        }

        if (_slot is not null)
        {
            _slot.PropertyChanged -= SlotPropertyChanged;
        }

        _slot = slot;
        if (_slot is not null)
        {
            _slot.PropertyChanged += SlotPropertyChanged;
        }

        _showFullSlot = null;
        UpdateSlotContent();
    }

    private void SlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrainerPokemonSlotViewModel.IsSet))
        {
            UpdateSlotContent();
        }
    }

    private void UpdateSlotContent()
    {
        if (_slot is null)
        {
            SlotHost.Content = null;
            return;
        }

        var showFullSlot = _slot.IsSet;
        if (_showFullSlot == showFullSlot)
        {
            return;
        }

        _showFullSlot = showFullSlot;
        SlotHost.Content = showFullSlot
            ? new TrainerPokemonSetSlotView { DataContext = _slot }
            : new TrainerPokemonEmptySlotView { DataContext = _slot };
    }
}
