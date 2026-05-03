# AGENTS.md

## Project Overview

Cross-platform desktop app (.NET 8.0) that plays multiple HLS/m3u8 streams simultaneously using LibVLCSharp. Built with Avalonia 12 UI framework. Single csproj, no solution file.

A dashboard window manages all streams. Streams can be viewed in floating individual windows or pinned to specific slots in a 2x3 grid view. Streams are not capped — unlimited streams can be managed from the dashboard, with up to 6 pinned to the grid at once. A built-in HTTP listener receives streams from the MultiStreamVlc-Companion Firefox extension.

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
- **DashboardWindow.axaml + .cs** — app's home screen (800x500). Manages a dynamic `ObservableCollection<StreamEntry>`. Toolbar: "New Stream", "Quick-Create From Clipboard", "Launch Grid View", "Settings". Each stream row has a volume slider, Grid slot selector (ComboBox, slots 1-6 or "—"), status, play/stop/reconnect/float/URL/remove controls.
- **StreamWindow.axaml + .cs** — floating window for a single stream. Contains `VideoView` + minimal control bar (volume, stop, reconnect). On close, detaches `Video.MediaPlayer = null` to release the native X11 handle.
- **MainWindow.axaml + .cs** — 2x3 grid view. Observes the dashboard's `ObservableCollection<StreamEntry>` and reacts to `GridSlot` property changes. Uses a two-pass refresh: detach all views first, then assign pinned entries. Exposes `Refresh()` for manual invocations.
- **StreamEntry.cs** — data model (`INotifyPropertyChanged`): Id, Title, Url, Player, FloatWindow, GridSlot, GridSlotIndex (ComboBox-friendly), Status. Stream titles auto-renumber on deletion.
- **AppSettings.cs** — settings model (Host, Port) with JSON persistence to `settings.json` in the app base directory. Loaded on startup, saved on settings change.
- **SettingsWindow.axaml + .cs** — modal dialog for companion listener settings. Host textbox (default `localhost`), port textbox (default random 49152-65535), "Randomize" button, "Copy Port" button, OK/Cancel.
- **CompanionListener.cs** — background `HttpListener` that accepts POST requests with JSON `{"name":"...","url":"..."}`. Validates URLs, adds streams to dashboard via UI thread dispatch. Returns JSON responses: `{"status":"ok"}` on success, `{"error":"..."}` on failure.
- **ChangeUrlDialog.axaml + .cs** — modal dialog for per-stream URL editing
- **ErrorDialog.axaml + .cs** — simple error popup (Avalonia has no built-in MessageBox)

## Key NuGet Packages

- `Avalonia` / `Avalonia.Desktop` / `Avalonia.Themes.Fluent` / `Avalonia.Fonts.Inter` — UI framework (v12.0.2)
- `LibVLCSharp` (3.9.7.1) — VLC bindings
- `LibVLCSharp.Avalonia` (3.9.7.1) — `VideoView` control for Avalonia
- `VideoLAN.LibVLC.Windows` (3.0.23.1) — native VLC binaries for Windows builds (no-op on Linux)

No extra NuGet packages needed for companion listener — `System.Net.HttpListener` and `System.Text.Json` are built into .NET 8.0.

## browser-extensions/

Contains Firefox extensions. NOT part of the .NET build. Do not modify `M3U8-Sniffer/` — archival only. `MultiStreamVlc-Companion/` is the active extension that sends streams to the app's HTTP listener.

## Conventions

- XAML files use `.axaml` extension (Avalonia convention, not `.xaml`)
- Avalonia uses 8-character ARGB hex for colors (e.g. `#FF111111` not `#111`)
- Nullable reference types enabled
- URL validation accepts schemes: http, https, rtsp, rtmp, udp, file; extensions: .m3u8, .mp4, .mkv, .ts, .flv, .avi, .mov
- VLC audio backend is platform-conditional: `--aout=pulse` on Linux (PipeWire compat), `--aout=directsound` on Windows
- Icons must use `<AvaloniaResource>` in csproj (not `<Resource>`) to be accessible via `avares://` URIs
- `ListBox` for item lists (Avalonia has no `ListView`)
- Compiled bindings require `x:DataType` on `DataTemplate` in Avalonia 12
- Companion listener port range: 49152-65535 (dynamic/private range)

## Critical Gotchas

- **XWayland required** — LibVLCSharp's `VideoView` embeds video via X11 window handles (`MediaPlayer.XWindow`). Native Wayland surfaces won't work. `Program.cs` forces `GDK_BACKEND=x11` at startup. Removing this causes VLC to open streams in separate windows instead of embedding them.
- **MediaPlayer assignment timing** — `VideoView.MediaPlayer` must be set in the `Opened` event (after native handles are created), not in the constructor. Setting it too early causes `Attach()` to fail silently and VLC opens separate windows.
- **Single LibVLC instance** — created once in `DashboardWindow`, shared by all `StreamWindow` and `MainWindow` instances. Do not create additional `LibVLC` instances.
- **MediaPlayer detach before reassign** — When moving a `MediaPlayer` between `VideoView`s (grid slot change, float-to-grid, grid-to-float), the player MUST be stopped and detached from the old view (`view.MediaPlayer = null`) before assigning to a new view. Otherwise VLC holds the old X11 handle and opens a new window. `MainWindow.RefreshGrid()` does a full detach-all-pass then reassign-pass. `StreamWindow.OnClosed` sets `Video.MediaPlayer = null`.
- **Play button auto-floats** — Pressing "Play" on a stream without an existing float window opens a `StreamWindow` automatically, so the `MediaPlayer` has a `VideoView` to attach to. Without this, VLC opens a separate window.
- **Grid slot conflicts** — `GridSelector_SelectionChanged` rejects slot selections that are already taken by another stream, reverting the ComboBox to its previous value.
- **Companion listener UI thread** — `CompanionListener` uses `Dispatcher.UIThread.Invoke()` (synchronous) to add streams on the UI thread. The callback returns `bool` (accepted/rejected) so the listener can return the correct HTTP status code.
- **Companion listener lifecycle** — Started on `DashboardWindow` construction, stopped on `OnClosed`. Restarted when settings change via `Settings_Click`.
- **No built-in MessageBox** — use the custom `ErrorDialog`
- **No `ResizeMode`** property on Window — does not exist in Avalonia
- **Clipboard API** — Avalonia 12 uses `clipboard.TryGetTextAsync()` (extension method from `Avalonia.Input.Platform`) and `clipboard.SetTextAsync()`
- **VideoView airspace** — UI overlays on `VideoView` must be children of the `VideoView` element, not siblings
- **`ShowDialog()`** is async — `await dlg.ShowDialog(this)` returns after close
- **`RangeBaseValueChangedEventArgs`** lives in `Avalonia.Controls.Primitives`
- **`AvaloniaXamlLoader.Load(this)`** in `App.axaml.cs` requires `using Avalonia.Markup.Xaml;`
