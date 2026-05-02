# AGENTS.md

## Project Overview

Cross-platform desktop app (.NET 8.0) that plays multiple HLS/m3u8 streams simultaneously using LibVLCSharp. Built with Avalonia 12 UI framework. Single csproj, no solution file.

Streams can be viewed in floating individual windows or in a 2×3 grid view. A dashboard window manages all streams.

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

- **Program.cs** — Avalonia entry point, sets `GDK_BACKEND=x11` for XWayland compat, bootstraps `App`
- **App.axaml + App.axaml.cs** — application setup, dark FluentTheme, crash logging to `~/Desktop/MultiStreamVlc-crashlog.txt`. Launches `DashboardWindow` as main window.
- **DashboardWindow.axaml + .cs** — app's home screen. Manages a dynamic list of `StreamEntry` items. "New Stream" adds entries, "Launch Grid View" opens the 2×3 grid. Each row has play/stop/reconnect/float/URL/remove controls.
- **StreamWindow.axaml + .cs** — floating window for a single stream. Contains `VideoView` + minimal control bar (volume, stop, reconnect).
- **MainWindow.axaml + .cs** — 2×3 grid view. Accepts up to 6 `StreamEntry` items from the dashboard. Grid-only, no side panel.
- **StreamEntry.cs** — data model (`INotifyPropertyChanged`): Id, Title, Url, Player, FloatWindow, Status
- **ChangeUrlDialog.axaml + .cs** — modal dialog for per-tile URL editing
- **ErrorDialog.axaml + .cs** — simple error popup (Avalonia has no built-in MessageBox)

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
- URL validation accepts schemes: http, https, rtsp, rtmp, udp, file; extensions: .m3u8, .mp4, .mkv, .ts, .flv, .avi, .mov
- VLC audio backend is platform-conditional: `--aout=pulse` on Linux (PipeWire compat), `--aout=directsound` on Windows
- Icons must use `<AvaloniaResource>` in csproj (not `<Resource>`) to be accessible via `avares://` URIs
- `ListBox` for item lists (Avalonia has no `ListView`)
- Compiled bindings require `x:DataType` on `DataTemplate` in Avalonia 12

## Critical Gotchas

- **XWayland required** — LibVLCSharp's `VideoView` embeds video via X11 window handles (`MediaPlayer.XWindow`). Native Wayland surfaces won't work. `Program.cs` forces `GDK_BACKEND=x11` at startup. Removing this causes VLC to open streams in separate windows instead of embedding them.
- **MediaPlayer assignment timing** — `VideoView.MediaPlayer` must be set in the `Opened` event (after native handles are created), not in the constructor. Setting it too early causes `Attach()` to fail silently and VLC opens separate windows.
- **Single LibVLC instance** — created once in `DashboardWindow`, shared by all `StreamWindow` and `MainWindow` instances. Do not create additional `LibVLC` instances.
- **No built-in MessageBox** — use the custom `ErrorDialog`
- **No `ResizeMode`** property on Window — does not exist in Avalonia
- **Clipboard API** — Avalonia 12 uses `clipboard.TryGetTextAsync()` (extension method from `Avalonia.Input.Platform`)
- **VideoView airspace** — UI overlays on `VideoView` must be children of the `VideoView` element, not siblings
- **`ShowDialog()`** is async — `await dlg.ShowDialog(this)` returns after close
- **`RangeBaseValueChangedEventArgs`** lives in `Avalonia.Controls.Primitives`
- **`AvaloniaXamlLoader.Load(this)`** in `App.axaml.cs` requires `using Avalonia.Markup.Xaml;`
