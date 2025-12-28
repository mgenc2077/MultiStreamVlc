// We keep:
// - lastSeenByTab: latest captured .m3u8 per tab (NOT shown as history until you manually add)
// - historyByTab: saved entries (shown in popup)

const lastSeenByTab = new Map();   // tabId -> { url, ts }
const historyByTab = new Map();    // tabId -> { title, items: [{label, url, ts}], lastUpdated }

const MAX_ITEMS_PER_TAB = 25;

function looksLikeM3U8(url) {
    try {
        const u = new URL(url);
        const path = u.pathname.toLowerCase();
        const query = u.search.toLowerCase();
        return path.includes(".m3u8") || query.includes(".m3u8");
    } catch {
        return false;
    }
}

async function getTabTitle(tabId) {
    try {
        const tab = await browser.tabs.get(tabId);
        return tab.title || `Tab ${tabId}`;
    } catch {
        return `Tab ${tabId}`;
    }
}

function ensureHistory(tabId, title) {
    let entry = historyByTab.get(tabId);
    if (!entry) {
        entry = { title, items: [], lastUpdated: Date.now() };
        historyByTab.set(tabId, entry);
    } else {
        entry.title = title || entry.title || `Tab ${tabId}`;
    }
    return entry;
}

// Capture network requests, but ONLY store the latest one per tab
browser.webRequest.onBeforeRequest.addListener(
    (details) => {
        if (details.tabId >= 0 && looksLikeM3U8(details.url)) {
            lastSeenByTab.set(details.tabId, { url: details.url, ts: Date.now() });
        }
    },
    { urls: ["<all_urls>"] }
);

// Context menu items
browser.contextMenus.create({
    id: "m3u8-add-last",
    title: "Add last .m3u8 to M3U8 Sniffer history",
    contexts: ["all"]
});

browser.contextMenus.create({
    id: "m3u8-copy-last",
    title: "Copy last .m3u8 (no save)",
    contexts: ["all"]
});

browser.contextMenus.onClicked.addListener(async (info, tab) => {
    if (!tab?.id || tab.id < 0) return;

    const last = lastSeenByTab.get(tab.id);
    if (!last?.url) {
        // Optional: notify user
        await browser.notifications?.create({
            type: "basic",
            iconUrl: browser.runtime.getURL("icon.png"),
            title: "M3U8 Sniffer",
            message: "No .m3u8 captured on this tab yet. Start playback first."
        }).catch(() => { });
        return;
    }

    if (info.menuItemId === "m3u8-copy-last") {
        // Copying from background is unreliable; do it via executeScript on the tab
        await browser.tabs.executeScript(tab.id, {
            code: `
        (async () => {
          const url = ${JSON.stringify(last.url)};
          try {
            await navigator.clipboard.writeText(url);
          } catch (e) {
            const ta = document.createElement('textarea');
            ta.value = url;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            ta.remove();
          }
        })();
      `
        });
        return;
    }

    if (info.menuItemId === "m3u8-add-last") {
        const title = tab.title || await getTabTitle(tab.id);
        const h = ensureHistory(tab.id, title);

        // Create a readable label that does NOT include URL
        const n = h.items.length + 1;
        const label = `${title} — #${n}`;

        // Dedup by URL (optional): if already saved, ignore
        if (h.items.some(x => x.url === last.url)) {
            // Still bump lastUpdated
            h.lastUpdated = Date.now();
            return;
        }

        h.items.unshift({ label, url: last.url, ts: Date.now() });
        if (h.items.length > MAX_ITEMS_PER_TAB) h.items.length = MAX_ITEMS_PER_TAB;

        h.lastUpdated = Date.now();
    }
});

// Cleanup on tab close
browser.tabs.onRemoved.addListener((tabId) => {
    lastSeenByTab.delete(tabId);
    historyByTab.delete(tabId);
});

// Keep titles updated in history
browser.tabs.onUpdated.addListener((tabId, changeInfo) => {
    if (changeInfo.title) {
        const h = historyByTab.get(tabId);
        if (h) h.title = changeInfo.title;
    }
});

// Popup API
browser.runtime.onMessage.addListener(async (msg) => {
    if (!msg?.type) return;

    if (msg.type === "GET_HISTORY") {
        const out = {};
        for (const [tabId, entry] of historyByTab.entries()) out[tabId] = entry;
        return out;
    }

    if (msg.type === "CLEAR_TAB" && typeof msg.tabId === "number") {
        historyByTab.delete(msg.tabId);
        return { ok: true };
    }

    if (msg.type === "CLEAR_ALL") {
        historyByTab.clear();
        return { ok: true };
    }

    return;
});
