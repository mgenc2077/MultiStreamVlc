---
name: multistreamvlc
description: Project-specific skill for MultiStreamVlc — a cross-platform Avalonia 12 + LibVLCSharp desktop app that plays 6 HLS/m3u8 streams simultaneously. Activate when working on any file in this repository.
---

# MultiStreamVlc — Project Skill

## Quick Reference

- **Framework**: Avalonia 12 (.axaml files, not .xaml)
- **Runtime**: .NET 8.0
- **Video**: LibVLCSharp 3.9.7.1 with LibVLCSharp.Avalonia VideoView
- **Build**: `dotnet build` / `dotnet run`
- **No tests, no lint, no CI**

## File Map

| File | Role |
|------|------|
| `Program.cs` | Avalonia entry point. Sets `GDK_BACKEND=x11` for XWayland before anything else. |
| `App.axaml` | Application XAML — FluentTheme, dark mode |
| `App.axaml.cs` | App lifecycle, crash logging to `~/Desktop/MultiStreamVlc-crashlog.txt` |
| `MainWindow.axaml` | Main layout — left panel (controls) + right panel (2x3 VideoView grid) |
| `MainWindow.axaml.cs` | All app logic — 6 MediaPlayers, play/stop/reconnect/volume/URL |
| `ChangeUrlDialog.axaml(.cs)` | Modal to change a tile's stream URL |
| `ErrorDialog.axaml(.cs)` | Simple error popup (Avalonia has no MessageBox) |
| `M3U8-Sniffer/` | Standalone Firefox extension, NOT part of .NET build |

## Architecture

- Single `LibVLC` instance shared by 6 `MediaPlayer` instances
- `VideoView` controls (V1–V6) in a 2×3 Grid, each assigned a `MediaPlayer`
- Side panel has per-stream controls using `Tag` property (int 0–5) to identify which stream
- Default URLs are placeholders (`https://example.com/streamN.m3u8`)
- Auto-plays all streams on window open

## Critical Patterns (do not break these)

### XWayland Requirement
`Program.cs` sets `GDK_BACKEND=x11` before Avalonia initializes. LibVLCSharp's `VideoView` on Linux only supports X11 window embedding via `MediaPlayer.XWindow`. Native Wayland surfaces cause VLC to open separate windows. **Do not remove this env var.**

### MediaPlayer Assignment Timing
`VideoView.MediaPlayer` is assigned in the `Opened` event handler (`OnOpened`), NOT in the constructor. The native platform handles don't exist until after the first layout pass. Assigning too early causes `Attach()` to fail silently → separate VLC windows.

### Platform-Conditional VLC Args
VLC CLI flags differ by OS:
- Linux: `--aout=pulse` (PipeWire/PulseAudio)
- Windows: `--aout=directsound`, `--directx-volume=0.35`
Selected via `RuntimeInformation.IsOSPlatform()`.

## Avalonia 12 API Notes

| WPF / Old API | Avalonia 12 Equivalent |
|---|---|
| `MessageBox.Show()` | Custom `ErrorDialog` window + `await dlg.ShowDialog(this)` |
| `Clipboard.GetText()` | `await clipboard.TryGetTextAsync()` (ext method, `Avalonia.Input.Platform`) |
| `Window.ResizeMode` | Does not exist — omit |
| `RoutedPropertyChangedEventArgs<double>` | `RangeBaseValueChangedEventArgs` (in `Avalonia.Controls.Primitives`) |
| `Loaded` event | `Opened` event |
| `<Resource>` in csproj | `<AvaloniaResource>` for assets accessed via `avares://` |
| Color `#RRGGBB` | Must use `#AARRGGBB` (8-char ARGB) |
| `DialogResult = true` | Set a public property + call `Close()`, check after `await ShowDialog()` returns |
| `AvaloniaXamlLoader.Load(this)` | Requires `using Avalonia.Markup.Xaml;` |
| `Owner` property on Window | Protected — cannot set externally. Use `ShowDialog(parent)` instead. |

## NuGet Packages (as of migration)

| Package | Version | Notes |
|---|---|---|
| `Avalonia` | 12.0.2 | Core framework |
| `Avalonia.Desktop` | 12.0.2 | Desktop platform |
| `Avalonia.Themes.Fluent` | 12.0.2 | Fluent theme |
| `Avalonia.Fonts.Inter` | 12.0.2 | Inter font |
| `LibVLCSharp` | 3.9.7.1 | VLC bindings |
| `LibVLCSharp.Avalonia` | 3.9.7.1 | VideoView for Avalonia |
| `VideoLAN.LibVLC.Windows` | 3.0.23.1 | Native VLC on Windows (no-op on Linux) |

## Linux System Dependencies (Arch)

```bash
sudo pacman -S dotnet-sdk-8.0 vlc
```

- `vlc` provides `libvlc.so` / `libvlccore.so` — LibVLCSharp discovers them from system paths
- No NuGet equivalent for Linux native libs
- `libx11` is optional (XWayland provides X11 compat on Wayland systems)

## URL Validation Rules

Schemes: `http`, `https`, `rtsp`, `rtmp`, `udp`, `file`
Extensions: `.m3u8`, `.mp4`, `.mkv`, `.ts`, `.flv`, `.avi`, `.mov`

## Known Issues / Future Work

- **Wayland native embedding**: Currently impossible — LibVLCSharp only supports `MediaPlayer.XWindow` (X11). If VLC/LibVLCSharp adds Wayland surface embedding, the `GDK_BACKEND=x11` hack can be removed.
- **Separate VLC window mode**: When `VideoView` fails to embed (e.g. no XWayland), VLC opens each stream in its own window. Controls still work. This could be intentionally implemented as a feature using `MediaPlayer` without `VideoView`.
- **X11 fallback**: Not currently implemented. The app runs under XWayland on Wayland systems.

## Logs

Crash logs: `~/Desktop/MultiStreamVlc-crashlog.txt`
Runtime logs: `logs/` (gitignored)
