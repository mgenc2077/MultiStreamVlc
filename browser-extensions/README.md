# Browser Extensions

Browser extensions for use with [MultiStreamVlc](https://github.com/mgenc2077/MultiStreamVlc).

## MultiStreamVlc Companion

The active companion extension. Detects .m3u8 stream URLs and sends them directly to the MultiStreamVlc desktop app.

| | Links |
|---|---|
| Firefox | [addons.mozilla.org](https://addons.mozilla.org/en-US/firefox/addon/multistreamvlc-companion/) |
| Chromium | Load unpacked from `chromium/` directory |

### Features

- **Passive sniffing** — Detects .m3u8 URLs via network request monitoring (no page injection)
- **One-click send** — Right-click → "Send to MultiStreamVlc" or `Ctrl+Shift+S`
- **History panel** — Toolbar popup shows all sent streams with **Send** (resend) and **Copy** (clipboard) buttons
- **Configurable connection** — Set host/port to match the desktop app's companion listener
- **Connection test** — Verify connectivity from settings or welcome page
- **Welcome page** — First-install setup wizard with connection configuration
- **Notifications** — Success/failure feedback on send

### Setup

1. Open MultiStreamVlc desktop app → Settings → note the host and port
2. Install the extension (Firefox: from Addon Marketplace; Chromium: `chrome://extensions` → Developer Mode → Load unpacked → select `chromium/`)
3. On first install, the welcome page opens — enter the host/port from the desktop app and click "Test Connection"
4. Navigate to a page with a video stream and start playback
5. Right-click → **Send to MultiStreamVlc** (or `Ctrl+Shift+S`)
6. The stream appears in the desktop app's dashboard

### Screenshots

![Companion Welcome](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/companion_welcome.png?raw=true)

![Companion Right Click](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/companion_rightClick.png?raw=true)

![Companion History](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/companion_history.png?raw=true)

### Companion API

The extension sends POST requests to the desktop app's HTTP listener:

**Endpoint**: `POST http://{host}:{port}/`

**Send a stream**:
```json
{"name": "Page Title", "url": "https://example.com/stream.m3u8"}
```

**Test connection**:
```json
{"test": true}
```

**Responses**:
- `200` `{"status":"ok"}` — Success
- `400` `{"error":"..."}` — Invalid request or unsupported URL
- `405` — Method not allowed

### Directory Structure

```
MultiStreamVlc-Companion/
├── firefox/            # Firefox Manifest V3 extension
│   ├── manifest.json
│   ├── background.js
│   ├── popup.html / popup.js / popup.css
│   ├── options.html / options.js
│   ├── welcome.html / welcome.js
│   └── icon.svg
└── chromium/           # Chromium Manifest V3 extension
    ├── manifest.json       # service_worker bg, PNG icons
    ├── background.js       # chrome.* namespace, scripting API
    ├── icon-16/32/48/128.png
    └── (symlinks to ../firefox/ for shared UI files)
```

### Building the Chromium Icons

If you modify `firefox/icon.svg`, regenerate the Chromium PNGs:

```bash
rsvg-convert -w 16 -h 16 firefox/icon.svg -o chromium/icon-16.png
rsvg-convert -w 32 -h 32 firefox/icon.svg -o chromium/icon-32.png
rsvg-convert -w 48 -h 48 firefox/icon.svg -o chromium/icon-48.png
rsvg-convert -w 128 -h 128 firefox/icon.svg -o chromium/icon-128.png
```

---

## M3U8 Sniffer

Archived. A simple Firefox extension that captures .m3u8 URLs and saves them to a history popup for clipboard copying. The predecessor to MultiStreamVlc Companion — it does **not** send streams to the desktop app.

| | Links |
|---|---|
| Firefox | [addons.mozilla.org](https://addons.mozilla.org/en-US/firefox/addon/m3u8-sniffer/) |

### Features

- Passive .m3u8 detection via `webRequest` API
- Right-click → "Save + copy last .m3u8" (`Ctrl+Shift+L`)
- Toolbar popup with per-tab history
- In-memory storage (cleared on browser close)
- 25-item cap per tab with deduplication

### Usage

1. Install from [Firefox Add-ons](https://addons.mozilla.org/en-US/firefox/addon/m3u8-sniffer/)
2. Navigate to a page with a video stream and start playback
3. Right-click → **Save + copy last .m3u8** — the URL is copied to clipboard and saved to history
4. Click the toolbar icon to view history

![M3U8 Sniffer Right Click](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/m3u8_rightClick.png?raw=true)

![M3U8 Sniffer History](https://github.com/mgenc2077/MultiStreamVlc/blob/main/screenshots/m3u8_history.png?raw=true)
