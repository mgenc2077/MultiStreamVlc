if (typeof browser === "undefined") globalThis.browser = chrome;

const hostInput = document.getElementById("host");
const portInput = document.getElementById("port");
const btnTest = document.getElementById("btn-test");
const btnSave = document.getElementById("btn-save");
const statusEl = document.getElementById("status");
const previewEl = document.getElementById("endpoint-preview");

function setStatus(text, cls) {
    statusEl.textContent = text;
    statusEl.className = cls || "";
}

function updatePreview() {
    const host = hostInput.value || "localhost";
    const port = portInput.value || "55432";
    previewEl.textContent = `http://${host}:${port}/`;
}

hostInput.addEventListener("input", updatePreview);
portInput.addEventListener("input", updatePreview);

async function loadSettings() {
    const resp = await browser.runtime.sendMessage({ type: "GET_SETTINGS" });
    hostInput.value = resp.host;
    portInput.value = resp.port;
    updatePreview();
}

btnTest.addEventListener("click", async () => {
    const host = hostInput.value.trim() || "localhost";
    const port = parseInt(portInput.value, 10);

    if (!port) {
        setStatus("Enter a port number.", "status-err");
        return;
    }

    btnTest.disabled = true;
    setStatus("Testing connection...", "status-info");

    await browser.runtime.sendMessage({ type: "SAVE_SETTINGS", host, port });

    const result = await browser.runtime.sendMessage({ type: "TEST_CONNECTION" });

    btnTest.disabled = false;
    if (result.success) {
        setStatus("Connection successful!", "status-ok");
    } else {
        setStatus(`Connection failed: ${result.error}`, "status-err");
    }
});

btnSave.addEventListener("click", async () => {
    const host = hostInput.value.trim() || "localhost";
    const port = parseInt(portInput.value, 10);

    if (!host || !port || port < 1 || port > 65535) {
        setStatus("Invalid host or port (1–65535).", "status-err");
        return;
    }

    await browser.runtime.sendMessage({ type: "SAVE_SETTINGS", host, port });
    setStatus("Settings saved.", "status-ok");
});

loadSettings();
