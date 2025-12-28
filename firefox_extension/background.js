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

function ensureHistory(tabId, title) {
    let entry = historyByTab.get(tabId);
    if (!entry) {
        entry = { title: title || `Tab ${tabId}`, items: [], lastUpdated: Date.now() };
        historyByTab.set(tabId, entry);
    } else {
        entry.title = title || entry.title || `Tab ${tabId}`;
    }
    return entry;
}

// Capture network requests, but only keep the latest one per tab
browser.webRequest.onBeforeRequest.addListener(
    (details) => {
        if (details.tabId >= 0 && looksLikeM3U8(details.url)) {
            lastSeenByTab.set(details.tabId, { url: details.url, ts: Date.now() });
        }
    },
    { urls: ["<all_urls>"] }
);

// Single context menu item: save + copy
browser.contextMenus.create({
    id: "m3u8-save-copy-last",
    title: "Save + copy last .m3u8",
    contexts: ["all"]
});

browser.contextMenus.onClicked.addListener(async (info, tab) => {
    if (info.menuItemId !== "m3u8-save-copy-last") return;
    if (!tab?.id || tab.id < 0) return;

    const last = lastSeenByTab.get(tab.id);
    if (!last?.url) {
        await browser.notifications?.create({
            type: "basic",
            iconUrl: browser.runtime.getURL("icon.png"),
            title: "M3U8 Sniffer",
            message: "No .m3u8 captured on this tab yet. Start playback first."
        }).catch(() => { });
        return;
    }

    // 1) Save to history (label based on tab title, not URL)
    const title = tab.title || `Tab ${tab.id}`;
    const h = ensureHistory(tab.id, title);

    // Dedup by URL (optional but usually nice)
    const alreadySaved = h.items.some(x => x.url === last.url);
    if (!alreadySaved) {
        const n = h.items.length + 1;
        const label = `${title} — #${n}`;
        h.items.unshift({ label, url: last.url, ts: Date.now() });
        if (h.items.length > MAX_ITEMS_PER_TAB) h.items.length = MAX_ITEMS_PER_TAB;
    }
    h.lastUpdated = Date.now();

    // 2) Copy to clipboard (do it in page context for reliability)
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
});

// Cleanup on tab close
browser.tabs.onRemoved.addListener((tabId) => {
    lastSeenByTab.delete(tabId);
    historyByTab.delete(tabId);
});

// Keep titles updated in history when they change
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
});

// Keyboard shortcut
browser.commands.onCommand.addListener(async (command) => {
    if (command !== "m3u8-save-copy") return;

    const tabs = await browser.tabs.query({ active: true, currentWindow: true });
    const tab = tabs[0];
    if (!tab?.id) return;

    const last = lastSeenByTab.get(tab.id);
    if (!last?.url) return;

    // Save to history
    const title = tab.title || `Tab ${tab.id}`;
    const h = ensureHistory(tab.id, title);

    if (!h.items.some(x => x.url === last.url)) {
        const label = `${title} — #${h.items.length + 1}`;
        h.items.unshift({ label, url: last.url, ts: Date.now() });
    }
    h.lastUpdated = Date.now();

    // Copy to clipboard
    await browser.tabs.executeScript(tab.id, {
        code: `
      (async () => {
        const url = ${JSON.stringify(last.url)};
        try {
          await navigator.clipboard.writeText(url);
        } catch {
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
});
