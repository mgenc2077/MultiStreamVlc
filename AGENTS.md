# AGENTS.md

## Project Overview

Cross-platform desktop app (.NET 8.0) that plays up to 6 HLS/m3u8 streams simultaneously using LibVLCSharp. Built with Avalonia 12 UI framework. Single csproj, no solution file.

## Build & Run

```bash
dotnet build
dotnet run
```

No tests, no lint, no CI pipelines.

## Linux Prerequisites (Arch)

```bash
sudo pacman -S dotnet-sdk-8.0 vlc
```

VLC provides `libvlc.so` / `libvlccore.so` at the system level — no NuGet package for Linux. On Windows, `VideoLAN.LibVLC.Windows` NuGet handles this automatically.

## Architecture

- **Program.cs** — Avalonia entry point, bootstraps `App`
- **App.axaml + App.axaml.cs** — application setup, dark FluentTheme, crash logging to `~/Desktop/MultiStreamVlc-crashlog.txt`
- **MainWindow.axaml + MainWindow.axaml.cs** — all app logic: creates 6 `MediaPlayer` instances, handles play/stop/reconnect/volume/URL changes. Per-tile controls use `Tag` properties (0–5)
- **ChangeUrlDialog.axaml + .cs** — modal dialog for per-tile URL editing
- **ErrorDialog.axaml + .cs** — simple error popup (replaces WPF `MessageBox` which Avalonia lacks)

## Key NuGet Packages

- `Avalonia` / `Avalonia.Desktop` / `Avalonia.Themes.Fluent` / `Avalonia.Fonts.Inter` — UI framework (v12.0.2)
- `LibVLCSharp` (3.9.7.1) — VLC bindings
- `LibVLCSharp.Avalonia` (3.9.7.1) — `VideoView` control for Avalonia
- `VideoLAN.LibVLC.Windows` (3.0.23.1) — native VLC binaries for Windows builds (no-op on Linux)

## M3U8-Sniffer/

Standalone Firefox extension (Manifest V2) for capturing .m3u8 URLs. Not part of the .NET build.

## Conventions

- XAML files use `.axaml` extension (Avalonia convention, not `.xaml`)
- Avalonia uses 8-character ARGB hex for colors (e.g. `#FF111111` not `#111`)
- Nullable reference types enabled
- Default stream URLs are placeholders (`https://example.com/streamN.m3u8`)
- URL validation accepts schemes: http, https, rtsp, rtmp, udp, file; extensions: .m3u8, .mp4, .mkv, .ts, .flv, .avi, .mov
- VLC audio backend is platform-conditional: `--aout=pulse` on Linux (PipeWire compat), `--aout=directsound` on Windows

## Avalonia Gotchas

- **No built-in MessageBox** — use the custom `ErrorDialog`
- **No `ResizeMode`** property on Window — removed during WPF migration
- **Clipboard API** — Avalonia 12 uses `clipboard.TryGetTextAsync()` (extension method from `Avalonia.Input.Platform`)
- **VideoView airspace** — UI overlays on `VideoView` must be children of the `VideoView` element, not siblings
- **`ShowDialog()`** is async — `await dlg.ShowDialog(this)` returns after close
- **`RangeBaseValueChangedEventArgs`** lives in `Avalonia.Controls.Primitives`
- **Wayland** — Avalonia 12 and VLC both auto-detect Wayland. No X11 dependency required. X11 fallback is not currently implemented.
