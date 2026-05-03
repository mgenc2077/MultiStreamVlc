using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;

namespace MultiStreamVlc;

public class StreamEntry : INotifyPropertyChanged
{
    private string _title = "New Stream";
    private string _url = "";
    private StreamWindow? _floatWindow;

    public Guid Id { get; } = Guid.NewGuid();
    public int Index { get; set; }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    public MediaPlayer? Player { get; set; }

    private int? _gridSlot;
    public int? GridSlot
    {
        get => _gridSlot;
        set
        {
            _gridSlot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GridSlotIndex));
        }
    }

    public int GridSlotIndex
    {
        get => _gridSlot.HasValue ? _gridSlot.Value + 1 : 0;
        set
        {
            var newSlot = value <= 0 ? (int?)null : value - 1;
            if (_gridSlot != newSlot)
            {
                _gridSlot = newSlot;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GridSlot));
            }
        }
    }

    public StreamWindow? FloatWindow
    {
        get => _floatWindow;
        set { _floatWindow = value; OnPropertyChanged(); }
    }

    public string Status => Player?.IsPlaying == true ? "Playing" : "Stopped";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshStatus() => OnPropertyChanged(nameof(Status));

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
