#!/usr/bin/env bash
# Startet Chrome mit Remote Debugging fuer die Playwright-Smoke-Tests (Linux/macOS).
# Windows: start-chrome-debug.ps1 (gleiche Logik in PowerShell).
#
# Verwendung: ./start-chrome-debug.sh -> bei der Anwendung einloggen -> dotnet test
#
# Chrome 136+ verlangt fuer Remote Debugging ein separates User-Data-Verzeichnis;
# das Standardprofil wird aus Sicherheitsgruenden abgelehnt (App-Bound Encryption).
# Quelle: https://developer.chrome.com/blog/remote-debugging-port
set -euo pipefail

PORT="${PLAYWRIGHT_CDP_PORT:-9222}"
BASE_URL_DEFAULT="{{BASE_URL}}"
BASE_URL="${PLAYWRIGHT_BASE_URL:-$BASE_URL_DEFAULT}"
PROFILE_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/PlaywrightTestProfile"
TEST_PROJEKT="{{BLAZOR_PROJEKT}}.PlaywrightTests"

debug_endpunkt_antwortet() {
    curl -s "http://localhost:${PORT}/json/version" >/dev/null 2>&1
}

if debug_endpunkt_antwortet; then
    echo "Chrome laeuft bereits mit Remote Debugging auf Port ${PORT}."
    echo "Bei der Anwendung einloggen, dann: dotnet test ${TEST_PROJEKT}"
    exit 0
fi

CHROME=""
for kandidat in google-chrome google-chrome-stable chromium chromium-browser \
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"; do
    if command -v "$kandidat" >/dev/null 2>&1 || [ -x "$kandidat" ]; then
        CHROME="$kandidat"
        break
    fi
done

if [ -z "$CHROME" ]; then
    echo "FEHLER: Chrome nicht gefunden (google-chrome, chromium oder Chrome.app)." >&2
    echo "Chrome installieren oder Pfad in CHROME eintragen." >&2
    exit 1
fi

echo "============================================================"
echo "Chrome Remote Debugging fuer Playwright-Tests"
echo "============================================================"
echo "Chrome:        ${CHROME}"
echo "Port:          ${PORT}"
echo "Test-Profil:   ${PROFILE_DIR}  (separates Profil, Chrome 136+)"
echo "Anwendung:     ${BASE_URL}"
echo
echo "Beim ERSTEN Start einmalig einloggen; die Session bleibt im Test-Profil."
echo "Chrome-Fenster offen lassen, dann: dotnet test ${TEST_PROJEKT}"
echo

mkdir -p "$PROFILE_DIR"
"$CHROME" --remote-debugging-port="$PORT" --user-data-dir="$PROFILE_DIR" "$BASE_URL" >/dev/null 2>&1 &

# Auf den Debug-Endpunkt warten (Bedingung mit Obergrenze statt fester Wartezeit)
for _ in $(seq 1 20); do
    if debug_endpunkt_antwortet; then
        echo "[OK] Remote Debugging aktiv auf Port ${PORT}"
        exit 0
    fi
    sleep 0.5
done

echo "[WARNUNG] Remote Debugging nach 10 s nicht erreichbar - Chrome-Fenster pruefen." >&2
exit 1
