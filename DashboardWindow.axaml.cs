using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using LibVLCSharp.Shared;

namespace MultiStreamVlc;

public partial class DashboardWindow : Window
{
    private LibVLC? _libVlc;
    private readonly ObservableCollection<StreamEntry> _streams = new();
    private MainWindow? _gridWindow;
    private int _nextIndex = 1;

    public DashboardWindow()
    {
        InitializeComponent();

        Core.Initialize();

        var vlcArgs = new System.Collections.Generic.List<string>
        {
            "--no-video-title-show",
            "--drop-late-frames",
            "--skip-frames",
            "--network-caching=1000",
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            vlcArgs.Add("--aout=pulse");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            vlcArgs.Add("--aout=directsound");
            vlcArgs.Add("--directx-volume=0.35");
        }

        _libVlc = new LibVLC(vlcArgs.ToArray());

        StreamList.ItemsSource = _streams;
        Closed += OnClosed;
    }

    private StreamEntry? FindById(Guid id)
        => _streams.FirstOrDefault(s => s.Id == id);

    private Guid? GetIdFromTag(object? sender)
    {
        if (sender is Button btn && btn.Tag is Guid id)
            return id;
        return null;
    }

    private void PlayStream(StreamEntry entry)
    {
        if (_libVlc == null || string.IsNullOrWhiteSpace(entry.Url)) return;
        if (entry.Player == null)
        {
            entry.Player = new MediaPlayer(_libVlc);
        }
        entry.Player.Stop();
        using var media = new Media(_libVlc, new Uri(entry.Url));
        entry.Player.Play(media);
        entry.RefreshStatus();
    }

    private void StopStream(StreamEntry entry)
    {
        entry.Player?.Stop();
        entry.RefreshStatus();
    }

    private void VolumeSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || slider.Tag is not Guid id) return;
        var entry = FindById(id);
        if (entry?.Player != null)
        {
            entry.Player.Volume = (int)e.NewValue;
        }
    }

    private void NewStream_Click(object? sender, RoutedEventArgs e)
    {
        var entry = new StreamEntry
        {
            Index = _nextIndex++,
            Title = $"Stream {_streams.Count + 1}",
            Url = ""
        };
        _streams.Add(entry);
    }

    private void PlayOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null || _libVlc == null) return;

        if (entry.FloatWindow == null)
        {
            var win = new StreamWindow(_libVlc, entry);
            win.Show(this);
        }
        else
        {
            PlayStream(entry);
        }
    }

    private void StopOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null) return;
        StopStream(entry);
    }

    private void ReconnectOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null || _libVlc == null) return;

        if (entry.FloatWindow == null)
        {
            var win = new StreamWindow(_libVlc, entry);
            win.Show(this);
        }
        else
        {
            StopStream(entry);
            PlayStream(entry);
        }
    }

    private void FloatOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null || _libVlc == null) return;

        if (entry.FloatWindow != null)
        {
            entry.FloatWindow.Activate();
            return;
        }

        if (entry.GridSlot.HasValue)
        {
            entry.GridSlot = null;
            _gridWindow?.Refresh();
        }

        var win = new StreamWindow(_libVlc, entry);
        win.Show(this);
    }

    private void GridSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || cb.Tag is not Guid id) return;
        var entry = FindById(id);
        if (entry == null) return;

        var newIndex = cb.SelectedIndex;

        var usedSlots = new HashSet<int>(_streams
            .Where(s => s.GridSlot.HasValue && s.Id != entry.Id)
            .Select(s => s.GridSlot!.Value));

        if (newIndex == 0)
        {
            entry.GridSlot = null;
            _gridWindow?.Refresh();
            return;
        }

        var slot = newIndex - 1;
        if (slot < 0 || slot > 5) return;

        if (usedSlots.Contains(slot))
        {
            cb.SelectedIndex = entry.GridSlot.HasValue ? entry.GridSlot.Value + 1 : 0;
            return;
        }

        if (entry.FloatWindow != null)
        {
            entry.Player?.Stop();
            entry.FloatWindow.Close();
        }

        entry.GridSlot = slot;

        if (_gridWindow == null && _libVlc != null)
        {
            _gridWindow = new MainWindow(_libVlc, _streams);
            _gridWindow.Show(this);
            _gridWindow.Closed += (_, _) =>
            {
                _gridWindow = null;
            };
        }
        else
        {
            _gridWindow?.Refresh();
        }
    }

    private async void ChangeUrlOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null) return;

        var dlg = new ChangeUrlDialog(_streams.IndexOf(entry), entry.Url);
        await dlg.ShowDialog(this);
        if (dlg.IsOk)
        {
            var target = _streams.ElementAtOrDefault(dlg.SelectedIndex);
            if (target != null)
            {
                var url = dlg.EnteredUrl;
                if (!IsValidUrl(url))
                {
                    var err = new ErrorDialog("Unsupported Value",
                        $"Unsupported Value: {url}\n\nMust start with http/rtsp/etc. and end with .m3u8/.mp4/etc.");
                    await err.ShowDialog(this);
                    return;
                }
                target.Url = url;
                if (target.Player != null)
                {
                    StopStream(target);
                    PlayStream(target);
                }
            }
        }
    }

    private void RemoveOne_Click(object? sender, RoutedEventArgs e)
    {
        var id = GetIdFromTag(sender);
        if (id == null) return;
        var entry = FindById(id.Value);
        if (entry == null) return;

        StopStream(entry);
        entry.GridSlot = null;
        entry.FloatWindow?.Close();
        entry.Player?.Dispose();
        entry.Player = null;
        _streams.Remove(entry);

        for (int i = 0; i < _streams.Count; i++)
        {
            _streams[i].Title = $"Stream {i + 1}";
        }
    }

    private void LaunchGrid_Click(object? sender, RoutedEventArgs e)
    {
        if (_gridWindow != null)
        {
            _gridWindow.Activate();
            return;
        }

        if (_libVlc == null) return;

        _gridWindow = new MainWindow(_libVlc, _streams);
        _gridWindow.Show(this);
        _gridWindow.Closed += (_, _) =>
        {
            _gridWindow = null;
        };
    }

    private async void ClipboardStream_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var url = await clipboard.TryGetTextAsync();
        if (string.IsNullOrWhiteSpace(url)) return;

        url = url.Trim();
        if (!IsValidUrl(url))
        {
            var err = new ErrorDialog("Unsupported Value",
                $"Clipboard content is not a valid stream URL:\n{url}\n\nMust start with http/rtsp/etc. and end with .m3u8/.mp4/etc.");
            await err.ShowDialog(this);
            return;
        }

        var entry = new StreamEntry
        {
            Index = _nextIndex++,
            Title = $"Stream {_streams.Count + 1}",
            Url = url
        };
        _streams.Add(entry);
    }

    private bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var validSchemes = new[] { "http", "https", "rtsp", "rtmp", "udp", "file" };
        bool schemeOk = false;
        foreach (var s in validSchemes)
        {
            if (s.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                schemeOk = true;
                break;
            }
        }
        if (!schemeOk) return false;

        var validExts = new[] { ".m3u8", ".mp4", ".mkv", ".ts", ".flv", ".avi", ".mov" };
        var ext = Path.GetExtension(uri.AbsolutePath);
        bool extOk = false;
        foreach (var ve in validExts)
        {
            if (ve.Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                extOk = true;
                break;
            }
        }
        return extOk;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        foreach (var entry in _streams)
        {
            try
            {
                entry.Player?.Stop();
                entry.Player?.Dispose();
                entry.FloatWindow?.Close();
            }
            catch { }
        }
        _streams.Clear();
        _libVlc?.Dispose();
        _libVlc = null;
    }
}
