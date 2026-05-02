using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace MultiStreamVlc;

public partial class MainWindow : Window
{
    private LibVLC? _libVlc;
    private ObservableCollection<StreamEntry>? _streams;
    private readonly VideoView[] _views = Array.Empty<VideoView>();

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(LibVLC libVlc, ObservableCollection<StreamEntry> streams) : this()
    {
        _libVlc = libVlc;
        _streams = streams;
        _views = new[] { V1, V2, V3, V4, V5, V6 };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_streams == null || _libVlc == null) return;

        _streams.CollectionChanged += OnStreamsChanged;
        foreach (var entry in _streams)
        {
            entry.PropertyChanged += OnEntryPropertyChanged;
        }

        RefreshGrid();

        Closed += OnClosed;
    }

    private void OnStreamsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (StreamEntry entry in e.NewItems)
                entry.PropertyChanged += OnEntryPropertyChanged;
        }
        if (e.OldItems != null)
        {
            foreach (StreamEntry entry in e.OldItems)
                entry.PropertyChanged -= OnEntryPropertyChanged;
        }
        RefreshGrid();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StreamEntry.GridSlot))
        {
            RefreshGrid();
        }
    }

    public void Refresh()
    {
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_streams == null || _libVlc == null) return;

        for (int i = 0; i < _views.Length; i++)
        {
            var entry = _streams.FirstOrDefault(s => s.GridSlot == i);

            if (entry != null)
            {
                entry.Player ??= new MediaPlayer(_libVlc);

                if (_views[i].MediaPlayer != entry.Player)
                {
                    _views[i].MediaPlayer = entry.Player;
                }

                if (!string.IsNullOrWhiteSpace(entry.Url) && entry.Player.State != VLCState.Playing)
                {
                    PlayEntry(entry);
                }
            }
            else
            {
                if (_views[i].MediaPlayer != null)
                {
                    _views[i].MediaPlayer.Stop();
                    _views[i].MediaPlayer = null;
                }
            }
        }
    }

    private void PlayEntry(StreamEntry entry)
    {
        if (entry.Player == null || _libVlc == null || string.IsNullOrWhiteSpace(entry.Url)) return;
        entry.Player.Stop();
        using var media = new Media(_libVlc, new Uri(entry.Url));
        entry.Player.Play(media);
        entry.RefreshStatus();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_streams != null)
        {
            _streams.CollectionChanged -= OnStreamsChanged;
            foreach (var entry in _streams)
            {
                entry.PropertyChanged -= OnEntryPropertyChanged;
            }
        }

        foreach (var view in _views)
        {
            try
            {
                view.MediaPlayer?.Stop();
                view.MediaPlayer = null;
            }
            catch { }
        }
    }
}
