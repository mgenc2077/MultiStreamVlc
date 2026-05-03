const statusEl = document.getElementById("status");
const statusDot = document.getElementById("status-dot");
const statusEndpoint = document.getElementById("status-endpoint");
const listEl = document.getElementById("list");

function setStatus(text, cls) {
    statusEl.textContent = text;
    statusEl.className = cls || "";
    if (text) {
        clearTimeout(setStatus._timer);
        setStatus._timer = setTimeout(() => {
            statusEl.textContent = "";
            statusEl.className = "";
        }, 4000);
    }
}

async function updateConnectionStatus() {
    const settings = await browser.runtime.sendMessage({ type: "GET_SETTINGS" });
    statusEndpoint.textContent = `${settings.host}:${settings.port}`;

    try {
        const result = await browser.runtime.sendMessage({ type: "TEST_CONNECTION" });
        statusDot.className = result.success ? "status-dot connected" : "status-dot disconnected";
    } catch {
        statusDot.className = "status-dot disconnected";
    }
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
    const block = document.createElement("div");
    block.className = "tab-block";

    const header = document.createElement("div");
    header.className = "tab-header";

    const title = document.createElement("span");
    title.className = "tab-title";
    title.textContent = entry.title;
    title.title = entry.title;

    const count = document.createElement("span");
    count.className = "tab-count";
    count.textContent = `(${entry.items.length})`;

    const clearBtn = document.createElement("button");
    clearBtn.className = "btn-clear-tab";
    clearBtn.textContent = "Clear";
    clearBtn.addEventListener("click", async () => {
        await browser.runtime.sendMessage({ type: "CLEAR_TAB", tabId: Number(tabId) });
        render();
    });

    header.append(title, count, clearBtn);

    const ul = document.createElement("ul");
    ul.className = "item-list";

    for (const item of entry.items) {
        const li = document.createElement("li");
        li.className = "item";

        const info = document.createElement("div");
        info.className = "item-info";

        const label = document.createElement("div");
        label.className = "item-label";
        label.textContent = item.label;

        const time = document.createElement("div");
        time.className = "item-time";
        time.textContent = new Date(item.ts).toLocaleString();

        const urlLine = document.createElement("div");
        urlLine.className = "item-url";
        urlLine.textContent = item.url;
        urlLine.title = item.url;

        info.append(label, time, urlLine);

        const actions = document.createElement("div");
        actions.className = "item-actions";

        const sendBtn = document.createElement("button");
        sendBtn.className = "btn-send";
        sendBtn.textContent = "Send";
        sendBtn.addEventListener("click", async () => {
            sendBtn.disabled = true;
            const result = await browser.runtime.sendMessage({
                type: "SEND_STREAM",
                name: item.label,
                url: item.url
            });
            sendBtn.disabled = false;
            if (result.success) {
                setStatus("Sent!", "ok");
            } else {
                setStatus(result.error, "err");
            }
        });

        const copyBtn = document.createElement("button");
        copyBtn.className = "btn-copy";
        copyBtn.textContent = "Copy";
        copyBtn.addEventListener("click", async () => {
            const ok = await copyToClipboard(item.url);
            setStatus(ok ? "Copied to clipboard." : "Failed to copy.", ok ? "ok" : "err");
        });

        actions.append(sendBtn, copyBtn);
        li.append(info, actions);
        ul.appendChild(li);
    }

    block.append(header, ul);
    return block;
}

async function render(optionalStatus, optionalCls) {
    listEl.innerHTML = "";

    const history = await browser.runtime.sendMessage({ type: "GET_HISTORY" });

    const entries = Object.entries(history)
        .map(([tabId, entry]) => ({ tabId, entry }))
        .sort((a, b) => b.entry.lastUpdated - a.entry.lastUpdated);

    if (entries.length === 0) {
        const hint = document.createElement("div");
        hint.className = "empty-hint";
        hint.innerHTML = 'No saved entries yet.<br>Right-click → <strong>Send to MultiStreamVlc</strong><br>or press <kbd>Ctrl+Shift+S</kbd> after starting playback.';
        listEl.appendChild(hint);
    } else {
        for (const { tabId, entry } of entries) {
            listEl.appendChild(createTabBlock(tabId, entry));
        }
    }

    if (optionalStatus) {
        setStatus(optionalStatus, optionalCls);
    }
}

document.getElementById("btn-refresh").addEventListener("click", () => render());

document.getElementById("btn-clear-all").addEventListener("click", async () => {
    await browser.runtime.sendMessage({ type: "CLEAR_ALL" });
    render("History cleared.", "info");
});

document.getElementById("btn-settings").addEventListener("click", () => {
    browser.runtime.openOptionsPage();
});

updateConnectionStatus();
render();
