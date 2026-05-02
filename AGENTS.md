# AGENTS.md

## Project Overview

WPF desktop app (.NET 8.0, Windows-only) that plays up to 6 HLS/m3u8 streams simultaneously using LibVLCSharp. Single csproj, no solution file.

## Build & Run

```bash
dotnet build
dotnet run
```

No tests, no lint, no CI pipelines.

## Architecture

- **App.xaml.cs** — entry point; wires up unhandled-exception logging to `~/Desktop/MultiStreamVlc-crashlog.txt`
- **MainWindow.xaml.cs** — all app logic: creates 6 `MediaPlayer` instances, handles play/stop/reconnect/volume/URL changes
- **ChangeUrlDialog.xaml.cs** — modal dialog for per-tile URL editing
- **MainWindow.xaml** — XAML layout with the 2×3 grid; per-tile controls use `Tag` properties (0–5) to identify which stream they target

## Key NuGet Dependencies

- `LibVLCSharp` / `LibVLCSharp.WPF` — VLC bindings
- `VideoLAN.LibVLC.Windows` — native VLC libraries; `Core.Initialize()` in MainWindow constructor discovers them automatically

## M3U8-Sniffer/

A standalone Firefox extension (Manifest V2) for capturing .m3u8 URLs. Not part of the .NET build — it has its own `manifest.json` and JS files. Changes here have no effect on the WPF app.

## Conventions

- No solution file; `dotnet build` / `dotnet run` works on the csproj directly
- Nullable reference types enabled (`Nullable>enable`)
- Default stream URLs are placeholders (`https://example.com/streamN.m3u8`)
- URL validation accepts schemes: http, https, rtsp, rtmp, udp, file; extensions: .m3u8, .mp4, .mkv, .ts, .flv, .avi, .mov
