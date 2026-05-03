# MultiStreamVlc

![MultiStreamVlc-logo](https://github.com/mgenc2077/MultiStreamVlc/blob/main/Assets/MultiStreamVLC.png?raw=true)

MultiStreamVlc is a cross-platform desktop application built with **Avalonia 12** and **LibVLCSharp** that displays and controls multiple video streams (HLS/m3u8) simultaneously. Originally created to watch multiple streams at once during collab streams.

![MultiStreamVlc](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/screenshot.png?raw=true)

## Features

- **Dashboard**: Central hub for managing all streams. Add, remove, and control streams from one window.
- **Floating Windows**: Pop out any stream into its own resizable window with volume control.
- **Grid View**: 2x3 grid displaying up to 6 pinned streams simultaneously.
- **Grid Slot Selector**: Assign streams to specific grid slots (1-6) via dropdown — freely mix and match which streams appear in the grid.
- **Quick-Create From Clipboard**: Instantly create a new stream from a URL copied to your clipboard.
- **Companion Listener**: Built-in HTTP server receives streams from the MultiStreamVlc-Companion Firefox extension automatically.
- **Granular Audio Control**: Individual volume sliders on the dashboard and in each floating window.
- **Independent Playback**: Play, Stop, and Reconnect each stream independently.
- **URL Editing**: Change any stream's URL on the fly.
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

- **Dashboard**: The main window. Add streams with "New Stream" or "Quick-Create From Clipboard", then set URLs and control playback.
- **Grid Slot Selector**: Each stream has a "Grid:" dropdown. Select a slot number (1-6) to pin the stream to that grid position, or select "—" to unpin. The grid window opens automatically when the first stream is pinned.
- **Floating Windows**: Click "Float" to pop out a stream into its own window. If the stream was in the grid, it is automatically unpinned.
- **Change URL**: Click "URL" for a popup to update the stream URL.
- **Volume**: Use the volume slider on each dashboard row or in each floating window.
- **Reconnecting**: If a stream stalls or disconnects, click "Reconnect".
- **Companion Listener**: Click "Settings" to configure the HTTP listener (host and port). The companion Firefox extension sends stream URLs to this endpoint.

## Companion API

The app listens for POST requests from the MultiStreamVlc-Companion Firefox extension.

**Endpoint**: `POST http://{host}:{port}/`

**Request**:
```json
{"name": "Stream Name", "url": "https://example.com/stream.m3u8"}
```

**Responses**:
- `200` — `{"status":"ok"}` — Stream added to dashboard
- `400` — `{"error":"..."}` — Invalid JSON or unsupported URL
- `405` — Method not allowed (non-POST)

Configure host and port via the Settings button on the dashboard. Port defaults to a random number in the 49152-65535 range.

## Tech Stack

- **.NET 8.0** — target framework
- **Avalonia 12** — cross-platform UI framework
- **LibVLCSharp 3.9** — VLC media player bindings
- **LibVLCSharp.Avalonia** — `VideoView` control for embedding VLC video in Avalonia
- **System.Net.HttpListener** — built-in HTTP server for companion extension
