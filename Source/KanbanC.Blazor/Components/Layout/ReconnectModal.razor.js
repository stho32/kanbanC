// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

const verbindungsalter = document.getElementById("verbindungsalter");

function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        nenneStandDesSchirms(new Date());
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
        reconnectModal.close();
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // Reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect();
        if (!successful) {
            // We have been able to reach the server, but the circuit is no longer available.
            // We'll reload the page so the user can continue using the app as quickly as possible.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        location.reload();
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}

// Der Server ist in diesem Fall weg und kann nichts mehr zeichnen: die Zeile entsteht deshalb im
// Browser, in dem Moment, in dem die Trennung beginnt. Der Zeitpunkt ist der des Abrisses und
// waechst nicht mit — eine ohne Verbindung weiterlaufende Zahl waere die eine, die sicher falsch
// ist.
function nenneStandDesSchirms(abrisszeitpunkt) {
    verbindungsalter.textContent = `Was du siehst, ist der Stand von ${alsTageszeit(abrisszeitpunkt)}.`;
}

function alsTageszeit(zeitpunkt) {
    const stunden = String(zeitpunkt.getHours()).padStart(2, "0");
    const minuten = String(zeitpunkt.getMinutes()).padStart(2, "0");
    return `${stunden}:${minuten}`;
}
