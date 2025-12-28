using LibVLCSharp.Shared;
using System;
using System.Windows;

namespace MultiStreamVlc
{
    public partial class MainWindow : Window
    {


        private LibVLC? _libVlc;
        private MediaPlayer[] _players = Array.Empty<MediaPlayer>();

        // Put your m3u8 URLs here (6 of them)
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

            // With VideoLAN.LibVLC.Windows package installed, this is enough:
            Core.Initialize();

            _libVlc = new LibVLC(new[]
            {
                "--no-video-title-show",
                "--drop-late-frames",
                "--skip-frames",
                "--network-caching=1000", // ms; tune later
                "--aout=directsound",
                "--directx-volume=0.35",
            });

            _players = new[]
            {
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
            };

            V1.MediaPlayer = _players[0];
            V2.MediaPlayer = _players[1];
            V3.MediaPlayer = _players[2];
            V4.MediaPlayer = _players[3];
            V5.MediaPlayer = _players[4];
            V6.MediaPlayer = _players[5];

            Loaded += (_, _) => PlayAll();
            Closed += (_, _) => Cleanup();
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

            // If your stream requires headers/cookies:
            // media.AddOption(":http-referrer=https://the-site/");
            // media.AddOption(":http-user-agent=Mozilla/5.0 ...");
            // media.AddOption(":http-cookie=SESSION=...; other=...");

            _players[i].Play(media);
        }

        private void ReconnectIndex(int i)
        {
            if (i < 0 || i >= _players.Length) return;
            _players[i].Stop();
            PlayIndex(i);
        }

        private int? GetIndexFromTag(object sender)
        {
            if (sender is System.Windows.Controls.Button btn &&
                int.TryParse(btn.Tag?.ToString(), out var idx))
                return idx;
            return null;
        }

        private void PlayOne_Click(object sender, RoutedEventArgs e)
        {
            var idx = GetIndexFromTag(sender);
            if (idx == null) return;
            _players[idx.Value].Stop();
            PlayIndex(idx.Value);
        }

        private void StopOne_Click(object sender, RoutedEventArgs e)
        {
            var idx = GetIndexFromTag(sender);
            if (idx == null) return;
            _players[idx.Value].Stop();
        }

        private void VolumeOne_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is System.Windows.Controls.Slider slider && 
                int.TryParse(slider.Tag?.ToString(), out var idx))
            {
                if (idx < 0 || idx >= _players.Length) return;
                _players[idx].Volume = (int)e.NewValue;
            }
        }


        private void ChangeUrlOne_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
            {
                if (idx < 0 || idx >= _urls.Length) return;

                var dlg = new ChangeUrlDialog(idx, _urls[idx]) { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    TrySetUrl(dlg.SelectedIndex, dlg.EnteredUrl);
                }
            }
        }

        private void ChangeUrlClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
            {
                if (idx < 0 || idx >= _urls.Length) return;

                var url = Clipboard.GetText();
                TrySetUrl(idx, url);
            }
        }


        private void StopAll() { foreach (var p in _players) p.Stop(); }
        //private void MuteAll(bool mute) { foreach (var p in _players) p.Mute = mute; }

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

        // Toolbar buttons
        private void PlayAll_Click(object sender, RoutedEventArgs e) => PlayAll();
        private void StopAll_Click(object sender, RoutedEventArgs e) => StopAll();
        private void ReconnectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _players.Length; i++) ReconnectIndex(i);
        }
        //private void MuteAll_Click(object sender, RoutedEventArgs e) => MuteAll(true);
        //private void UnmuteAll_Click(object sender, RoutedEventArgs e) => MuteAll(false);

        // Per-tile reconnect buttons (Tag holds index 0..5)
        private void ReconnectOne_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out var idx))
            {
                ReconnectIndex(idx);
            }
        }

        // Change URL popup
        private void ChangeUrl_Click(object sender, RoutedEventArgs e)
        {
            // Default to tile 1; you can improve later (e.g., last clicked tile)
            int currentIndex = 0;
            var dlg = new ChangeUrlDialog(currentIndex, _urls[currentIndex])
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                TrySetUrl(dlg.SelectedIndex, dlg.EnteredUrl);
            }
        }

        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            // 1. Check Scheme
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

            // 2. Check Extension (strict mode)
            var validExts = new[] { ".m3u8", ".mp4", ".mkv", ".ts", ".flv", ".avi", ".mov" };
            var ext = System.IO.Path.GetExtension(uri.AbsolutePath);
            bool extOk = false;
            foreach (var e in validExts)
            {
                if (e.Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    extOk = true;
                    break;
                }
            }
            return extOk;
        }

        private void ShowUrlError(string invalidValue)
        {
            try { System.Media.SystemSounds.Hand.Play(); } catch { }
            MessageBox.Show(this, 
                $"Unsupported Value: {invalidValue}\n\nMust start with http/rtsp/etc. and end with .m3u8/.mp4/etc.", 
                "Unsupported Value", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
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
}
