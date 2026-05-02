using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using LibVLCSharp.Shared;

namespace MultiStreamVlc;

public partial class MainWindow : Window
{
    private LibVLC? _libVlc;
    private MediaPlayer[] _players = Array.Empty<MediaPlayer>();

    private readonly string[] _urls = new[]
    {
        "https://example.com/stream1.m3u8",
        "https://example.com/stream2.m3u8",
        "https://example.com/stream3.m3u8",
        "https://example.com/stream4.m3u8",
        "https://example.com/stream5.m3u8",
        "https://example.com/stream6.m3u8",
    };

    public MainWindow()
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

        _players = new[]
        {
            new MediaPlayer(_libVlc),
            new MediaPlayer(_libVlc),
            new MediaPlayer(_libVlc),
            new MediaPlayer(_libVlc),
            new MediaPlayer(_libVlc),
            new MediaPlayer(_libVlc),
        };

        Opened += OnOpened;
        Closed += (_, _) => Cleanup();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        V1.MediaPlayer = _players[0];
        V2.MediaPlayer = _players[1];
        V3.MediaPlayer = _players[2];
        V4.MediaPlayer = _players[3];
        V5.MediaPlayer = _players[4];
        V6.MediaPlayer = _players[5];

        PlayAll();
    }

    private void PlayAll()
    {
        for (int i = 0; i < _players.Length; i++)
            PlayIndex(i);
    }

    private void PlayIndex(int i)
    {
        if (_libVlc == null) return;
        if (i < 0 || i >= _players.Length) return;

        var url = _urls[i];
        if (string.IsNullOrWhiteSpace(url)) return;

        using var media = new Media(_libVlc, new Uri(url));
        _players[i].Play(media);
    }

    private void ReconnectIndex(int i)
    {
        if (i < 0 || i >= _players.Length) return;
        _players[i].Stop();
        PlayIndex(i);
    }

    private int? GetIndexFromTag(object? sender)
    {
        if (sender is Button btn &&
            int.TryParse(btn.Tag?.ToString(), out var idx))
            return idx;
        return null;
    }

    private void PlayOne_Click(object? sender, RoutedEventArgs e)
    {
        var idx = GetIndexFromTag(sender);
        if (idx == null) return;
        _players[idx.Value].Stop();
        PlayIndex(idx.Value);
    }

    private void StopOne_Click(object? sender, RoutedEventArgs e)
    {
        var idx = GetIndexFromTag(sender);
        if (idx == null) return;
        _players[idx.Value].Stop();
    }

    private void VolumeOne_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider slider &&
            int.TryParse(slider.Tag?.ToString(), out var idx))
        {
            if (idx < 0 || idx >= _players.Length) return;
            _players[idx].Volume = (int)e.NewValue;
        }
    }

    private async void ChangeUrlOne_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
        {
            if (idx < 0 || idx >= _urls.Length) return;

            var dlg = new ChangeUrlDialog(idx, _urls[idx]);
            await dlg.ShowDialog(this);
            if (dlg.IsOk)
            {
                TrySetUrl(dlg.SelectedIndex, dlg.EnteredUrl);
            }
        }
    }

    private async void ChangeUrlClipboard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
        {
            if (idx < 0 || idx >= _urls.Length) return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;

            var url = await clipboard.TryGetTextAsync();
            if (url != null)
            {
                TrySetUrl(idx, url);
            }
        }
    }

    private void StopAll()
    {
        foreach (var p in _players) p.Stop();
    }

    private void Cleanup()
    {
        try
        {
            foreach (var p in _players)
            {
                try { p.Stop(); } catch { }
                p.Dispose();
            }
            _players = Array.Empty<MediaPlayer>();
            _libVlc?.Dispose();
            _libVlc = null;
        }
        catch { }
    }

    private void PlayAll_Click(object? sender, RoutedEventArgs e) => PlayAll();
    private void StopAll_Click(object? sender, RoutedEventArgs e) => StopAll();
    private void ReconnectAll_Click(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < _players.Length; i++) ReconnectIndex(i);
    }

    private void ReconnectOne_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
        {
            ReconnectIndex(idx);
        }
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

    private async void ShowUrlError(string invalidValue)
    {
        var dlg = new ErrorDialog("Unsupported Value",
            $"Unsupported Value: {invalidValue}\n\nMust start with http/rtsp/etc. and end with .m3u8/.mp4/etc.");
        await dlg.ShowDialog(this);
    }

    private void TrySetUrl(int index, string url)
    {
        if (index < 0 || index >= _urls.Length) return;

        if (!IsValidUrl(url))
        {
            ShowUrlError(url);
            return;
        }

        _urls[index] = url;
        ReconnectIndex(index);
    }
}
