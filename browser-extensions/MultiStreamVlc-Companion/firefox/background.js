const MAX_ITEMS_PER_TAB = 25;
const DEFAULT_HOST = "localhost";
const DEFAULT_PORT = 55432;
const SEND_TIMEOUT_MS = 5000;

const lastSeenByTab = new Map();
const historyByTab = new Map();

function looksLikeM3U8(url) {
    try {
        const u = new URL(url);
        const combined = u.pathname.toLowerCase() + "?" + u.search.toLowerCase();
        return /\.m3u8(?=[?#\/]|$)/i.test(u.pathname + u.search);
    } catch {
        return false;
    }
}

async function getSettings() {
    const data = await browser.storage.local.get({ host: DEFAULT_HOST, port: DEFAULT_PORT });
    return { host: data.host, port: Number(data.port) };
}

async function sendToApp(name, url) {
    const settings = await getSettings();
    const endpoint = `http://${settings.host}:${settings.port}/`;

    try {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), SEND_TIMEOUT_MS);

        const resp = await fetch(endpoint, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, url }),
            signal: controller.signal
        });

        clearTimeout(timer);

        if (resp.ok) {
            const json = await resp.json().catch(() => null);
            if (json && json.status === "ok") {
                return { success: true };
            }
            return { success: false, error: json?.error || "Unknown server response" };
        }

        if (resp.status === 400) {
            const json = await resp.json().catch(() => null);
            return { success: false, error: json?.error || `Server rejected (HTTP ${resp.status})` };
        }

        return { success: false, error: `HTTP ${resp.status}` };
    } catch (err) {
        if (err.name === "AbortError") {
            return { success: false, error: "Connection timed out" };
        }
        if (err.message?.includes("Failed to fetch") || err.message?.includes("NetworkError") || err.message?.includes("ERR_CONNECTION_REFUSED")) {
            return { success: false, error: `Cannot reach ${settings.host}:${settings.port}. Is MultiStreamVlc running?` };
        }
        return { success: false, error: err.message || "Unknown error" };
    }
}

async function showNotification(title, message, isError = false) {
    try {
        await browser.notifications.create({
            type: "basic",
            iconUrl: browser.runtime.getURL("icon.svg"),
            title,
            message
        });
    } catch {}
}

function addToHistory(tabId, tabTitle, url) {
    if (!historyByTab.has(tabId)) {
        historyByTab.set(tabId, { title: tabTitle, items: [], lastUpdated: Date.now() });
    }
    const h = historyByTab.get(tabId);
    h.title = tabTitle;
    h.lastUpdated = Date.now();

    if (h.items.some(x => x.url === url)) return false;

    h.items.unshift({
        label: `${tabTitle} -- #${h.items.length + 1}`,
        url,
        ts: Date.now()
    });
    if (h.items.length > MAX_ITEMS_PER_TAB) h.items.length = MAX_ITEMS_PER_TAB;
    return true;
}

async function saveAndSend(tab) {
    const last = lastSeenByTab.get(tab.id);
    if (!last) {
        await showNotification("MultiStreamVlc", "No .m3u8 detected on this tab yet. Start playback first.", true);
        return;
    }

    addToHistory(tab.id, tab.title || "Untitled", last.url);

    const result = await sendToApp(tab.title || "Stream", last.url);
    if (result.success) {
        await showNotification("MultiStreamVlc", `Stream sent: ${tab.title || "Untitled"}`);
    } else {
        await showNotification("MultiStreamVlc", `Failed: ${result.error}`, true);
    }
}

async function copyToClipboardInTab(tabId, text) {
    try {
        await browser.scripting.executeScript({
            target: { tabId },
            func: (t) => {
                navigator.clipboard.writeText(t).catch(() => {
                    const ta = document.createElement('textarea');
                    ta.value = t;
                    document.body.appendChild(ta);
                    ta.select();
                    document.execCommand('copy');
                    ta.remove();
                });
            },
            args: [text]
        });
        return true;
    } catch {
        return false;
    }
}

browser.webRequest.onBeforeRequest.addListener(
    (details) => {
        if (details.tabId >= 0 && looksLikeM3U8(details.url)) {
            lastSeenByTab.set(details.tabId, { url: details.url, ts: Date.now() });
        }
    },
    { urls: ["<all_urls>"] }
);

browser.runtime.onInstalled.addListener((details) => {
    if (details.reason === "install") {
        browser.tabs.create({ url: browser.runtime.getURL("welcome.html") });
    }
});

browser.contextMenus.create({
    id: "send-to-multistreamvlc",
    title: "Send to MultiStreamVlc",
    contexts: ["all"]
});

browser.contextMenus.onClicked.addListener(async (info, tab) => {
    if (info.menuItemId === "send-to-multistreamvlc" && tab) {
        await saveAndSend(tab);
    }
});

browser.commands.onCommand.addListener(async (command) => {
    if (command === "send-to-multistreamvlc") {
        const tabs = await browser.tabs.query({ active: true, currentWindow: true });
        if (tabs.length > 0) {
            await saveAndSend(tabs[0]);
        }
    }
});

browser.tabs.onRemoved.addListener((tabId) => {
    lastSeenByTab.delete(tabId);
    historyByTab.delete(tabId);
});

browser.tabs.onUpdated.addListener((tabId, changeInfo) => {
    if (changeInfo.title) {
        const h = historyByTab.get(tabId);
        if (h) h.title = changeInfo.title;
    }
});

browser.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === "GET_HISTORY") {
        const serialized = {};
        for (const [tabId, entry] of historyByTab) {
            serialized[tabId] = entry;
        }
        sendResponse(serialized);
        return;
    }

    if (message.type === "CLEAR_TAB") {
        historyByTab.delete(message.tabId);
        sendResponse({ ok: true });
        return;
    }

    if (message.type === "CLEAR_ALL") {
        historyByTab.clear();
        sendResponse({ ok: true });
        return;
    }

    if (message.type === "SEND_STREAM") {
        sendToApp(message.name, message.url).then(sendResponse);
        return true;
    }

    if (message.type === "GET_SETTINGS") {
        getSettings().then(sendResponse);
        return true;
    }

    if (message.type === "SAVE_SETTINGS") {
        browser.storage.local.set({ host: message.host, port: message.port }).then(() => {
            sendResponse({ ok: true });
        });
        return true;
    }

    if (message.type === "TEST_CONNECTION") {
        (async () => {
            const settings = await getSettings();
            const endpoint = `http://${settings.host}:${settings.port}/`;
            try {
                const controller = new AbortController();
                const timer = setTimeout(() => controller.abort(), SEND_TIMEOUT_MS);
                const resp = await fetch(endpoint, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ test: true }),
                    signal: controller.signal
                });
                clearTimeout(timer);
                sendResponse(resp.ok ? { success: true } : { success: false, error: `HTTP ${resp.status}` });
            } catch (err) {
                if (err.name === "AbortError") {
                    sendResponse({ success: false, error: "Connection timed out" });
                } else {
                    sendResponse({ success: false, error: `Cannot reach ${settings.host}:${settings.port}. Is MultiStreamVlc running?` });
                }
            }
        })();
        return true;
    }

    if (message.type === "COPY_TO_CLIPBOARD") {
        copyToClipboardInTab(message.tabId, message.text).then(sendResponse);
        return true;
    }
});
