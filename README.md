# MultiStreamVlc

MultiStreamVlc is a simple WPF application that leverages `LibVLCSharp` to display and control multiple video streams (HLS/m3u8) simultaneously in a grid layout. I created this application to watch multiple streams at once during collab streams.

![MultiStreamVlc](https://github.com/mgenc2077/MultiStreamVlc/blob/main/MultiStreamVlc.png?raw=true)

## Features

-   **Multi-View**: Watch 6 video streams at once in a 2x3 grid.
-   **Granular Audio Control**: Individual volume sliders for each stream (0-100%).
-   **Independent Playback**: Play, Stop, and Reconnect each stream independently.
-   **Global Controls**: Play All, Stop All, and Reconnect All buttons for mass management.
-   **Dynamic Source**: "Change URL" button allows you to update the stream URL for any tile on the fly.
-   **Reliable Backend**: Built on `LibVLCSharp` and `VideoLAN.LibVLC.Windows` for robust playback support.

## Prerequisites

-   Windows 10/11
-   .NET 8.0 SDK (or Runtime)

## Development Setup

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/mgenc2077/MultiStreamVlc.git
    cd MultiStreamVlc
    ```

2.  **Open in Visual Studio Code/Vscode**:
    Open `MultiStreamVlc.csproj` or the solution file in Visual Studio / Vscode.

3.  **Restore Nuget Packages**:
    The project relies on the following packages:
    -   `LibVLCSharp`
    -   `LibVLCSharp.WPF`
    -   `VideoLAN.LibVLC.Windows`

    Visual Studio should restore these automatically on build.

4.  **Run**:
    Press `F5` or run `dotnet run` to start the application.

## Usage

-   **Streams**: By default, the app is configured with placeholder URLs (`https://example.com/streamN.m3u8`).
-   **Change URL**: Click "Change URL" on any tile to set a real HLS/m3u8 stream link.
-   **Volume**: Use the slider on each tile to adjust volume. Slide to the far left (0) to mute.
-   **Reconnecting**: If a stream stalls or disconnects, click "Reconnect" on its specific panel.

