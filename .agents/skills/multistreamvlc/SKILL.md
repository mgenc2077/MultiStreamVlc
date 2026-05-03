---
name: multistreamvlc
description: Project-specific skill for MultiStreamVlc — a cross-platform Avalonia 12 + LibVLCSharp desktop app that plays multiple HLS/m3u8 streams in floating windows or a 2x3 grid, with a companion HTTP listener for the Firefox extension. Activate when working on any file in this repository.
---

# MultiStreamVlc — Project Skill

## Quick Reference

- **Framework**: Avalonia 12 (.axaml files, not .xaml)
- **Runtime**: .NET 8.0
- **Video**: LibVLCSharp 3.9.7.1 with LibVLCSharp.Avalonia VideoView
- **Companion**: `System.Net.HttpListener` (built-in, no NuGet)
- **Build**: `dotnet build` / `dotnet run`
- **No tests, no lint, no CI**

## File Map

| File | Role |
|------|------|
| `Program.cs` | Avalonia entry point. Sets `GDK_BACKEND=x11` for XWayland before anything else. |
| `App.axaml(.cs)` | Application XAML — FluentTheme, dark mode, crash logging to `~/Desktop/MultiStreamVlc-crashlog.txt` |
| `DashboardWindow.axaml(.cs)` | Main window (800x500). Manages `ObservableCollection<StreamEntry>`. Toolbar: New Stream, Quick-Create From Clipboard, Launch Grid View, Settings. Each row: volume slider, Grid slot ComboBox, status, play/stop/reconnect/float/URL/remove. Starts/stops `CompanionListener`. |
| `StreamWindow.axaml(.cs)` | Floating window for a single stream. `VideoView` + volume/stop/reconnect controls. Detaches `Video.MediaPlayer = null` on close. |
| `MainWindow.axaml(.cs)` | 2x3 grid view. Observes `StreamEntry.GridSlot` changes. Two-pass refresh: detach all views, then assign pinned entries. |
| `StreamEntry.cs` | Data model (`INotifyPropertyChanged`): Id, Title, Url, Player, FloatWindow, GridSlot, GridSlotIndex, Status. Titles auto-renumber on deletion. |
| `AppSettings.cs` | Settings model (Host, Port). Load/save to `settings.json` in app base directory. `RandomPort()` generates ports in 49152-65535 range. |
| `SettingsWindow.axaml(.cs)` | Modal dialog for companion listener config. Host, Port, Randomize button, Copy Port button, OK/Cancel. |
| `CompanionListener.cs` | Background `HttpListener`. POST `{"name","url"}` endpoint. Validates URL, dispatches to UI thread via `Dispatcher.UIThread.Invoke()`. Returns JSON `{"status":"ok"}` or `{"error":"..."}`. |
| `ChangeUrlDialog.axaml(.cs)` | Modal to change a stream's URL |
| `ErrorDialog.axaml(.cs)` | Simple error popup (Avalonia has no MessageBox) |
| `browser-extensions/M3U8-Sniffer/` | Archived Firefox extension — DO NOT MODIFY |
| `browser-extensions/MultiStreamVlc-Companion/` | Active Firefox extension that sends streams to the HTTP listener |

## Architecture

- Single `LibVLC` instance created in `DashboardWindow`, shared by all windows
- Unlimited streams managed from dashboard, up to 6 pinned to grid via `StreamEntry.GridSlot`
- `GridSlotIndex` (0=unpinned, 1-6=slot) provides ComboBox-friendly binding
- Grid observes `ObservableCollection.CollectionChanged` and `StreamEntry.PropertyChanged` for live updates
- `Play` button auto-opens a `StreamWindow` if no float window exists (so `MediaPlayer` has a `VideoView`)
- `Float` button unpins from grid before opening float window
- Grid slot ComboBox rejects already-taken slots
- Companion listener starts on app launch, restarts on settings change, stops on app close
- Companion callback returns `bool` (accepted/rejected) so listener sends correct HTTP status
- Settings persisted to `settings.json` in app base directory

## Companion API

```
POST http://{host}:{port}/
Content-Type: application/json

{"name": "Stream Name", "url": "https://example.com/stream.m3u8"}
```

- `200 {"status":"ok"}` — stream added to dashboard (no auto-play)
- `400 {"error":"..."}` — invalid JSON or unsupported URL
- `405` — non-POST methods

## Critical Patterns (do not break these)

### XWayland Requirement
`Program.cs` sets `GDK_BACKEND=x11` before Avalonia initializes. LibVLCSharp's `VideoView` on Linux only supports X11 window embedding via `MediaPlayer.XWindow`. Native Wayland surfaces cause VLC to open separate windows. **Do not remove this env var.**

### MediaPlayer Assignment Timing
`VideoView.MediaPlayer` is assigned in the `Opened` event handler (`OnOpened`), NOT in the constructor. The native platform handles don't exist until after the first layout pass. Assigning too early causes `Attach()` to fail silently -> separate VLC windows.

### MediaPlayer Detach Before Reassign
When moving a `MediaPlayer` between `VideoView`s (grid-to-grid, float-to-grid, grid-to-float), the player MUST be stopped and detached from the old view (`view.MediaPlayer = null`) before assigning to the new view. Otherwise VLC holds the old X11 handle and opens a new window. Key locations:
- `MainWindow.RefreshGrid()` — full detach-all pass, then reassign pass
- `StreamWindow.OnClosed` — sets `Video.MediaPlayer = null`
- `DashboardWindow.GridSelector_SelectionChanged` — stops player before closing float window
- `DashboardWindow.FloatOne_Click` — clears `GridSlot` before opening float

### Companion Listener Threading
`CompanionListener` runs `HttpListener` on a background `Task`. It uses `Dispatcher.UIThread.Invoke()` (synchronous) to call the stream-received callback on the UI thread. The callback is `Func<string, string, bool>` — returns `true` if accepted, `false` if URL validation failed. The listener returns the appropriate HTTP status based on this result.

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
| `Clipboard.SetText()` | `await clipboard.SetTextAsync()` |
| `Window.ResizeMode` | Does not exist — omit |
| `RoutedPropertyChangedEventArgs<double>` | `RangeBaseValueChangedEventArgs` (in `Avalonia.Controls.Primitives`) |
| `Loaded` event | `Opened` event |
| `<Resource>` in csproj | `<AvaloniaResource>` for assets accessed via `avares://` |
| Color `#RRGGBB` | Must use `#AARRGGBB` (8-char ARGB) |
| `DialogResult = true` | Set a public property + call `Close()`, check after `await ShowDialog()` returns |
| `AvaloniaXamlLoader.Load(this)` | Requires `using Avalonia.Markup.Xaml;` |
| `Owner` property on Window | Protected — cannot set externally. Use `ShowDialog(parent)` instead. |

## NuGet Packages

| Package | Version | Notes |
|---|---|---|
| `Avalonia` | 12.0.2 | Core framework |
| `Avalonia.Desktop` | 12.0.2 | Desktop platform |
| `Avalonia.Themes.Fluent` | 12.0.2 | Fluent theme |
| `Avalonia.Fonts.Inter` | 12.0.2 | Inter font |
| `LibVLCSharp` | 3.9.7.1 | VLC bindings |
| `LibVLCSharp.Avalonia` | 3.9.7.1 | VideoView for Avalonia |
| `VideoLAN.LibVLC.Windows` | 3.0.23.1 | Native VLC on Windows (no-op on Linux) |

No extra packages for companion — `System.Net.HttpListener` and `System.Text.Json` are built into .NET 8.0.

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
- **Drag-and-drop grid assignment**: Not implemented. Grid slots are assigned via ComboBox dropdown.
- **Grid view auto-updating with new streams**: Grid only shows streams with a `GridSlot` set. New streams are unpinned by default.
- **Stream window position/size memory**: Not persisted between sessions.
- **Config persistence**: Stream list and URLs are not saved between sessions (only companion host/port is persisted).

## Logs

Crash logs: `~/Desktop/MultiStreamVlc-crashlog.txt`
Runtime logs: `logs/` (gitignored)
