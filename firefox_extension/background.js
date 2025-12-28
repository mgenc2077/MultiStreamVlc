// Store per-tab last seen m3u8
const lastM3U8ByTab = new Map();

function looksLikeM3U8(url) {
    try {
        const u = new URL(url);
        // Basic: .m3u8 in path or query
        return u.pathname.toLowerCase().includes(".m3u8") ||
            u.search.toLowerCase().includes(".m3u8");
    } catch {
        return false;
    }
}

// Listen to network requests
browser.webRequest.onBeforeRequest.addListener(
    (details) => {
        if (details.tabId >= 0 && looksLikeM3U8(details.url)) {
            lastM3U8ByTab.set(details.tabId, details.url);
            // Optional: console log
            // console.log("Captured m3u8:", details.tabId, details.url);
        }
    },
    { urls: ["<all_urls>"] }
);

// Context menu
browser.contextMenus.create({
    id: "copy-m3u8",
    title: "Copy last .m3u8 URL",
    contexts: ["all"]
});

browser.contextMenus.onClicked.addListener(async (info, tab) => {
    if (info.menuItemId !== "copy-m3u8") return;

    const url = lastM3U8ByTab.get(tab.id);
    if (!url) {
        // Optional: notify user
        await browser.notifications?.create({
            type: "basic",
            iconUrl: browser.runtime.getURL("icon.png"),
            title: "M3U8 Sniffer",
            message: "No .m3u8 captured on this tab yet. Start playback first."
        }).catch(() => { });
        return;
    }

    // Copy to clipboard: easiest via offscreen doc isn't available in MV2.
    // In Firefox MV2, simplest is to message a content script to copy.
    await browser.tabs.executeScript(tab.id, {
        code: `
      (async () => {
        try {
          await navigator.clipboard.writeText(${JSON.stringify(url)});
        } catch (e) {
          const ta = document.createElement('textarea');
          ta.value = ${JSON.stringify(url)};
          document.body.appendChild(ta);
          ta.select();
          document.execCommand('copy');
          ta.remove();
        }
      })();
    `
    });

});
