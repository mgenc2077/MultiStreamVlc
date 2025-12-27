using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Linq;
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
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
            "https://edge9-sof.live.mmcdn.com/live-edge/amlst:squrlgurl-sd-1bb81c5d4c793f70b9878f9880467171af6d4f594e6ecfb73cc8d286a2fab8ac_trns_h264/chunklist_w489189762_b5128000_t64RlBTOjMwLjA=.m3u8",
        };

        public MainWindow()
        {
            InitializeComponent();

            Core.Initialize();

            // If VLC is installed normally, libvlc.dll is usually in one of these.
            // Pick whichever exists on your machine.
            var candidates = new[]
            {
                @"C:\Program Files\VideoLAN\VLC",
                @"C:\Program Files (x86)\VideoLAN\VLC"
            };

            var vlcDir = candidates.FirstOrDefault(Directory.Exists);
            if (vlcDir == null)
            {
                MessageBox.Show("VLC not found. Install VLC (64-bit) first, or bundle libVLC with the app.");
                Close();
                return;
            }

            // Tell LibVLCSharp where libvlc is
            // (Important: libvlc.dll and plugins folder must be there)
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", Path.Combine(vlcDir, "plugins"));
            _libVlc = new LibVLC(new[]
            {
                // Helpful stability/perf flags (tweak later):
                "--quiet",
                "--no-video-title-show",
                "--drop-late-frames",
                "--skip-frames",
                "--network-caching=1000", // ms; tune for your latency/jitter
            });

            // Create 6 players (one per view)
            _players = new[]
            {
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
                new MediaPlayer(_libVlc),
            };

            // Attach to VideoViews
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
            if (_libVlc == null) return;

            for (int i = 0; i < _players.Length; i++)
            {
                var url = _urls[i];
                if (string.IsNullOrWhiteSpace(url)) continue;

                // Dispose previous media if any by just creating a new Media each time.
                using var media = new Media(_libVlc, new Uri(url));

                // Optional: if your streams require headers (Referer / Cookie), you can add options here:
                // media.AddOption(":http-referrer=https://yoursite.example/");
                // media.AddOption(":http-user-agent=Mozilla/5.0 ...");
                // media.AddOption(":http-cookie=key=value; key2=value2;");

                _players[i].Play(media);
            }
        }

        private void StopAll()
        {
            foreach (var p in _players) p.Stop();
        }

        private void MuteAll(bool mute)
        {
            foreach (var p in _players) p.Mute = mute;
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
            catch { /* ignore */ }
        }

        private void PlayAll_Click(object sender, RoutedEventArgs e) => PlayAll();
        private void StopAll_Click(object sender, RoutedEventArgs e) => StopAll();
        private void MuteAll_Click(object sender, RoutedEventArgs e) => MuteAll(true);
        private void UnmuteAll_Click(object sender, RoutedEventArgs e) => MuteAll(false);
    }
}
