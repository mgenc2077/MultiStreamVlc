const listEl = document.getElementById("list");
const statusEl = document.getElementById("status");

document.getElementById("refresh").addEventListener("click", () => render());
document.getElementById("clearAll").addEventListener("click", async () => {
    await browser.runtime.sendMessage({ type: "CLEAR_ALL" });
    await render("Cleared all history.");
});

function showStatus(text) {
    statusEl.textContent = text;
    statusEl.style.display = "block";
    setTimeout(() => (statusEl.style.display = "none"), 1200);
}

async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        const ta = document.createElement("textarea");
        ta.value = text;
        document.body.appendChild(ta);
        ta.select();
        const ok = document.execCommand("copy");
        ta.remove();
        return ok;
    }
}

function createTabBlock(tabId, entry) {
    const wrap = document.createElement("div");
    wrap.className = "tab";

    const header = document.createElement("div");
    header.className = "tabHeader";

    const title = document.createElement("div");
    title.className = "tabTitle";
    title.title = entry.title || `Tab ${tabId}`;

    const count = document.createElement("span");
    count.className = "small";
    const itemCount = entry.items?.length || 0;
    count.textContent = ` (${itemCount})`;

    title.textContent = entry.title || `Tab ${tabId}`;
    title.appendChild(count);

    const buttons = document.createElement("div");
    buttons.className = "tabButtons";

    const clearBtn = document.createElement("button");
    clearBtn.textContent = "Clear tab";
    clearBtn.addEventListener("click", async (e) => {
        e.stopPropagation();
        await browser.runtime.sendMessage({ type: "CLEAR_TAB", tabId: Number(tabId) });
        await render(`Cleared tab: ${entry.title || tabId}`);
    });

    buttons.appendChild(clearBtn);

    header.appendChild(title);
    header.appendChild(buttons);

    const urlList = document.createElement("div");
    urlList.className = "urlList";

    for (const item of (entry.items || [])) {
        const row = document.createElement("div");
        row.className = "urlItem";
        row.title = "Click to copy URL";

        // Show label (readable), not URL
        const lbl = document.createElement("div");
        lbl.textContent = item.label || "Saved stream";
        lbl.style.fontWeight = "600";
        lbl.style.marginBottom = "4px";

        // Optional: show a tiny hint without leaking full ugly URL
        const hint = document.createElement("div");
        hint.className = "small";
        hint.textContent = new Date(item.ts || Date.now()).toLocaleString();

        row.appendChild(lbl);
        row.appendChild(hint);

        row.addEventListener("click", async () => {
            const ok = await copyToClipboard(item.url);
            showStatus(ok ? "Copied URL to clipboard." : "Copy failed.");
        });

        urlList.appendChild(row);
    }

    wrap.appendChild(header);
    wrap.appendChild(urlList);
    return wrap;
}

async function render(optionalStatus) {
    listEl.innerHTML = "";
    const history = await browser.runtime.sendMessage({ type: "GET_HISTORY" });

    const tabIds = Object.keys(history);
    if (!tabIds.length) {
        const empty = document.createElement("div");
        empty.className = "small";
        empty.textContent =
            "No saved entries yet. Right-click the page → “Add last .m3u8 to history” after starting playback.";
        listEl.appendChild(empty);
        if (optionalStatus) showStatus(optionalStatus);
        return;
    }

    tabIds.sort((a, b) => (history[b].lastUpdated || 0) - (history[a].lastUpdated || 0));

    for (const tabId of tabIds) {
        const entry = history[tabId];
        if (!entry?.items?.length) continue;
        listEl.appendChild(createTabBlock(tabId, entry));
    }

    if (optionalStatus) showStatus(optionalStatus);
}

render();
