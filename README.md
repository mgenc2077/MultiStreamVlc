# MultiStreamVlc

![MultiStreamVlc-logo](https://github.com/mgenc2077/MultiStreamVlc/blob/main/Assets/MultiStreamVLC.png?raw=true)

MultiStreamVlc is a cross-platform desktop application built with **Avalonia 12** and **LibVLCSharp** that displays and controls multiple video streams (HLS/m3u8) simultaneously in a grid layout. Originally created to watch multiple streams at once during collab streams.

![MultiStreamVlc](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/screenshot.png?raw=true)

## Features

- **Multi-View**: Watch 6 video streams at once in a 2x3 grid.
- **Granular Audio Control**: Individual volume sliders for each stream (0-100%).
- **Independent Playback**: Play, Stop, and Reconnect each stream independently.
- **Global Controls**: Play All, Stop All, and Reconnect All buttons for mass management.
- **Dynamic Source**: "Change URL" button allows you to update the stream URL for any tile on the fly.
- **Clipboard URL**: Paste a stream URL directly from clipboard to any tile.
- **Cross-Platform**: Runs on Linux and Windows (via Avalonia 12 / .NET 8.0).

## M3U8 Sniffer

M3U8 Sniffer is a Firefox extension that allows you to capture .m3u8 URLs from a right click button or keyboard shortcut (Ctrl+Shift+L) and shows history in a toolbar popup. ([Link to Firefox Addon Marketplace](https://addons.mozilla.org/en-US/firefox/addon/m3u8-sniffer/))

![M3U8 Sniffer Right Click](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/m3u8_rightClick.png?raw=true)

![M3U8 Sniffer History](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/m3u8_history.png?raw=true)

## Prerequisites

### Linux (Arch)

```bash
sudo pacman -S dotnet-sdk-8.0 vlc
```

### Windows

- Windows 10/11
- .NET 8.0 SDK (or Runtime)

VLC binaries are provided automatically via the `VideoLAN.LibVLC.Windows` NuGet package.

### Wayland Note

On Wayland systems, the app runs under XWayland for video embedding compatibility (LibVLCSharp's `VideoView` requires X11 window handles). This is handled automatically — no manual configuration needed.

## Development Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/mgenc2077/MultiStreamVlc.git
   cd MultiStreamVlc
   ```

2. **Restore NuGet Packages**:
   ```bash
   dotnet restore
   ```

3. **Run**:
   ```bash
   dotnet run
   ```

## Usage

- **Streams**: By default, the app is configured with placeholder URLs (`https://example.com/streamN.m3u8`).
- **Change URL**: Click "Change URL" for a popup to set a real HLS/m3u8 stream link per tile or "Clipboard URL" to paste directly from clipboard.
- **Volume**: Use the slider on each tile to adjust volume. Slide to the far left (0) to mute.
- **Reconnecting**: If a stream stalls or disconnects, click "Reconnect" on its specific panel.

## Tech Stack

- **.NET 8.0** — target framework
- **Avalonia 12** — cross-platform UI framework
- **LibVLCSharp 3.9** — VLC media player bindings
- **LibVLCSharp.Avalonia** — `VideoView` control for embedding VLC video in Avalonia
