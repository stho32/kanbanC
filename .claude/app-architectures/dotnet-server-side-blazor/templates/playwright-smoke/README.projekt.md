# {{BLAZOR_PROJEKT}}.PlaywrightTests — Smoke-Tests der Seitenerreichbarkeit

Prueft jede `@page`-Route der Blazor-Anwendung: Seite erreichbar, kein ASP.NET-/Blazor-Fehler, ein bis zwei Schluesselelemente vorhanden. Die Ausgabe ist maschinenlesbar, damit ein KI-Assistent nach Codeaenderungen selbst pruefen und Fehler analysieren kann.

## Ablauf eines Testlaufs

1. Anwendung starten (`{{BASE_URL}}`; andere URL per `PLAYWRIGHT_BASE_URL`)
2. Chrome mit Remote Debugging starten: `./start-chrome-debug.sh` (Windows: `.\start-chrome-debug.ps1`)
3. Im geoeffneten Chrome-Fenster bei der Anwendung einloggen (inkl. 2FA); Fenster offen lassen
4. Tests ausfuehren:
   ```bash
   dotnet test {{BLAZOR_PROJEKT}}.PlaywrightTests --logger "console;verbosity=detailed"
   ```

Die Tests verbinden sich per Chrome DevTools Protocol (CDP) mit dem laufenden Chrome und nutzen dessen Session samt Cookies. Ohne erreichbaren CDP-Endpunkt starten sie einen headless Browser ohne Login — geschuetzte Seiten enden dann als `Inconclusive`, nie als Gruen.

### Warum ein separates Chrome-Profil?

Seit Chrome 136 (Mai 2025) ist Remote Debugging mit dem Standardprofil abgeschaltet (App-Bound Encryption gegen Cookie-Diebstahl durch fremde Prozesse). Die Start-Scripts legen deshalb ein eigenes Profil an — Linux/macOS `~/.local/share/PlaywrightTestProfile`, Windows `%LOCALAPPDATA%\PlaywrightTestProfile` — und starten Chrome mit `--remote-debugging-port` **und** `--user-data-dir`. Das Profil speichert die Login-Session zwischen Testlaeufen; nur beim ersten Start ist ein Login noetig. Quelle: https://developer.chrome.com/blog/remote-debugging-port

## Umgebungsvariablen

| Variable | Default | Beschreibung |
|---|---|---|
| `PLAYWRIGHT_BASE_URL` | `{{BASE_URL}}` | Basis-URL der Anwendung |
| `PLAYWRIGHT_CDP_URL` | `http://localhost:9222` | Chrome-DevTools-Protocol-Endpunkt, den die Tests ansprechen |
| `PLAYWRIGHT_CDP_PORT` | `9222` | Port, den die Start-Scripts oeffnen (muss zu `PLAYWRIGHT_CDP_URL` passen) |

## Selektive Ausfuehrung

```bash
dotnet test {{BLAZOR_PROJEKT}}.PlaywrightTests --filter "Category=SmokeTest"       # Sammeltest (ein Durchlauf ueber alle Routen)
dotnet test {{BLAZOR_PROJEKT}}.PlaywrightTests --filter "Category=PageAvailable"   # ein Test je Route
```

## Ausgabe-Marker

| Marker | Bedeutung |
|---|---|
| `[PAGE_OK] /route - h1: gefunden, form: gefunden` | Seite erreichbar, Elemente vorhanden |
| `[PAGE_ERROR] /route - <ExceptionTyp>: <Message>` | ASP.NET-/Blazor-Fehler oder fehlende Elemente; `[STACKTRACE]` folgt bei Developer Exception Page |
| `[PAGE_WARNING] /route - Authentifizierung erforderlich` | Login-Seite statt Inhalt; Test endet `Inconclusive` |
| `[AUTH_REDIRECT] /route -> <Location>` | Cross-Origin-Redirect zu einem Auth-Server (OAuth/OIDC) |
| `[SUMMARY] Erfolgreich / Auth erforderlich / Fehler` | Zaehler des Sammeltests |
| `[SMOKE_TEST_OK]`, `[SMOKE_TEST_FAILED]`, `[SMOKE_TEST_AUTH]`, `[SMOKE_TEST_EMPTY]` | Endergebnis des Sammeltests |
| `[SCREENSHOT] <pfad>` | Screenshot bei Fehler unter `bin/Debug/<tfm>/Screenshots/` |

## Aufbau

| Datei | Zweck |
|---|---|
| `PlaywrightTestBase.cs` | CDP-Verbindung mit Fallback, Tab-Management (`CreatePageAsync`, `[TearDown]` schliesst Pages — keine offenen Tabs bei langen Laeufen), `CheckPageAsync` (Navigation bis NetworkIdle, Element-Wartebedingung mit Obergrenze, Login- und Auth-Redirect-Erkennung), Screenshot- und Ausgabe-Helfer |
| `Helpers/AspNetErrorParser.cs` | Erkennt Blazor Error Boundary, Developer Exception Page, generische ASP.NET-Fehlerseiten und HTTP-Fehlertitel; extrahiert Typ, Message, gekuerzten Stacktrace |
| `PageTests/AlleSeiten_SmokeTests.cs` | Tabelle `AllPages` (Route, Name, Selektoren); Sammeltest `[Category("SmokeTest")]` und parametrisierte Einzeltests `[Category("PageAvailable")]` |
| `PageTests/<Seite>PageTests.cs` | optional — nur fuer Seiten, die mehr brauchen als die Elementpruefung |
| `start-chrome-debug.sh` / `.ps1` | Chrome mit Remote Debugging und Test-Profil starten |

Neue Seite aufnehmen: eine Zeile in `AllPages` ergaenzen — Route, Name, ein bis zwei charakteristische Selektoren (Ueberschrift + Hauptinhalt wie `form`, `table`, `.card`, `[class*='grid']`). Routen mit Parametern (`/kunde/{id:int}`) mit einer existierenden Beispiel-ID eintragen.

## Fehlerbehebung

| Symptom | Ursache / Abhilfe |
|---|---|
| `[AUTH] CDP-Verbindung fehlgeschlagen` | Chrome laeuft nicht mit Remote Debugging: alle Chrome-Fenster schliessen (`pkill chrome` bzw. `taskkill /IM chrome.exe /F`), Start-Script ausfuehren, einloggen |
| Remote Debugging startet nicht | Chrome ohne `--user-data-dir` gestartet (Chrome 136+); immer ueber das Start-Script starten |
| Seiten zeigen Login trotz CDP | Im Chrome-Fenster nicht (mehr) eingeloggt — Session abgelaufen oder erster Start des Test-Profils; neu einloggen |
| Port 9222 belegt | `PLAYWRIGHT_CDP_PORT=9223 ./start-chrome-debug.sh` und `PLAYWRIGHT_CDP_URL=http://localhost:9223 dotnet test ...`; Belegung pruefen mit `ss -ltnp | grep 9222` (Windows: `netstat -ano | findstr 9222`) |
| Playwright-Browser fehlen (Fallback ohne CDP) | `pwsh bin/Debug/<tfm>/playwright.ps1 install` |
