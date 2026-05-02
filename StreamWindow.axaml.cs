using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using LibVLCSharp.Shared;

namespace MultiStreamVlc;

public partial class StreamWindow : Window
{
    private readonly LibVLC _libVlc = null!;
    private readonly StreamEntry _entry = null!;

    public StreamWindow()
    {
        InitializeComponent();
    }

    public StreamWindow(LibVLC libVlc, StreamEntry entry) : this()
    {
        _libVlc = libVlc;
        _entry = entry;

        Title = entry.Title;
        entry.FloatWindow = this;

        Closed += OnClosed;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_entry.Player == null)
        {
            _entry.Player = new MediaPlayer(_libVlc);
        }

        Video.MediaPlayer = _entry.Player;
        VolumeSlider.ValueChanged += OnVolumeChanged;

        PlayStream();
    }

    private void OnVolumeChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_entry.Player != null)
        {
            _entry.Player.Volume = (int)e.NewValue;
        }
    }

    private void PlayStream()
    {
        if (_entry.Player == null || string.IsNullOrWhiteSpace(_entry.Url)) return;
        using var media = new Media(_libVlc, new Uri(_entry.Url));
        _entry.Player.Play(media);
        _entry.RefreshStatus();
    }

    private void Stop_Click(object? sender, RoutedEventArgs e)
    {
        _entry.Player?.Stop();
        _entry.RefreshStatus();
    }

    private void Reconnect_Click(object? sender, RoutedEventArgs e)
    {
        _entry.Player?.Stop();
        PlayStream();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _entry.Player?.Stop();
        _entry.RefreshStatus();
        _entry.FloatWindow = null;
        VolumeSlider.ValueChanged -= OnVolumeChanged;
    }
}
