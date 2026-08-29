# Playwright Smoke-Test Templates (Blazor Server)

Vorlagen fuer ein NUnit-Testprojekt, das jede `@page`-Route einer Blazor-Server-Anwendung auf Erreichbarkeit, ASP.NET-/Blazor-Fehler und Schluesselelemente prueft — mit KI-lesbarer Ausgabe. Instanziiert werden sie durch `/erstelle-blazor-playwright-tests`; Einordnung in die Test-Pyramide: [05-testing-strategy.md](../../05-testing-strategy.md), Abschnitt „Playwright Smoke-Tests".

Abgrenzung zu `Source/MyApp.PlaywrightTests/` aus Kapitel 05: dort laufen E2E-Journeys gegen eine per `WebApplicationFactory` gestartete Instanz; die Smoke-Tests hier verbinden sich per CDP mit einem laufenden, eingeloggten Chrome und decken die Breite aller Routen ab, nicht die Tiefe einzelner Flows.

## Dateien und Zielpfade

| Template | Zielpfad im Testprojekt | Zweck |
|---|---|---|
| `PlaywrightTests.csproj` | `{{BLAZOR_PROJEKT}}.PlaywrightTests.csproj` | Projektdatei, Paketversionen laut Versionstabelle |
| `PlaywrightTestBase.cs` | `PlaywrightTestBase.cs` | CDP-Verbindung, Tab-Management, `CheckPageAsync`, Ausgabe-Helfer |
| `AspNetErrorParser.cs` | `Helpers/AspNetErrorParser.cs` | Fehlerseiten erkennen, Exception-Typ/Message/Stacktrace extrahieren |
| `AlleSeiten_SmokeTests.cs` | `PageTests/AlleSeiten_SmokeTests.cs` | Routen-Tabelle `AllPages`, Sammeltest und parametrisierte Einzeltests |
| `PageTests.cs` | `PageTests/<Seite>PageTests.cs` | optional: Einzelseiten-Klasse fuer Seiten mit Zusatzpruefungen |
| `start-chrome-debug.sh` | `start-chrome-debug.sh` | Chrome mit Remote Debugging starten (Linux/macOS) |
| `start-chrome-debug.ps1` | `start-chrome-debug.ps1` | dito fuer Windows |
| `README.projekt.md` | `README.md` | Betriebsanleitung des Testprojekts: Ablauf, Chrome-136-Profil, Umgebungsvariablen, Marker, Fehlerbehebung |

## Platzhalter

| Platzhalter | Bedeutung | Beispiel |
|---|---|---|
| `{{BLAZOR_PROJEKT}}` | Name des Blazor-Projekts, Namensmuster der Solution | `MyApp.Web` |
| `{{NAMESPACE}}` | Root-Namespace des Blazor-Projekts | `MyApp.Web` |
| `{{BASE_URL}}` | URL der laufenden Anwendung aus `launchSettings.json` (https-Profil) | `https://localhost:5001` |
| `{{TARGET_FRAMEWORK}}` | TFM des Blazor-Projekts | `net10.0` |
| `{{ROUTEN_TABELLE}}` | Kommentar in `AllPages`; die Beispielzeilen davor und der Kommentar werden durch die echten Routen ersetzt | `("/kunden", "Kunden", ["h1", "table"]),` |
| `{{SEITE}}`, `{{ROUTE}}`, `{{SELEKTOR_1}}`, `{{SELEKTOR_2}}` | nur `PageTests.cs` | `Kunden`, `/kunden`, `h1`, `table` |

## Versionstabelle (Stand: 2026-08-27)

Die eine Stelle fuer Versionen dieser Templates; `PlaywrightTests.csproj` muss ihr entsprechen.

| Paket / Werkzeug | Version im Template | Quelle | Anmerkung |
|---|---|---|---|
| NUnit | 4.4.0 | `commands/upgrade/nunit.md` (kanonische Matrix, Stand Dezember 2025) | NuGet aktuell 4.6.1 |
| NUnit3TestAdapter | 6.0.0 | `commands/upgrade/nunit.md` | NuGet aktuell 6.3.0; Freigabe fuer net10.0 bei Ausfuehrung pruefen |
| Microsoft.NET.Test.Sdk | 18.0.1 | `commands/upgrade/nunit.md` | NuGet aktuell 18.9.0 |
| Microsoft.Playwright, Microsoft.Playwright.NUnit | 1.62.0 | NuGet (nicht in `nunit.md` gefuehrt) | am 2026-08-27 neueste stabile Version |
| Chrome | 136+ | https://developer.chrome.com/blog/remote-debugging-port | Remote Debugging nur mit separatem `--user-data-dir` |
| .NET TFM | `{{TARGET_FRAMEWORK}}` | Blazor-Projekt | Testprojekt folgt dem TFM der Anwendung |

Die NUnit-Trias (NUnit, Adapter, Test.Sdk) folgt bewusst `nunit.md` statt dem NuGet-Tagesstand, damit alle Testprojekte einer Solution dieselben Versionen tragen; ein Hebel laeuft ueber `/upgrade nunit`.

## Grundsaetze der Templates

- **Keine festen Wartezeiten**: Navigation wartet auf `NetworkIdle`, Elemente per `WaitForSelectorAsync` mit Obergrenze (`ElementTimeoutMs`) — Bedingung statt `WaitForTimeoutAsync` (Skill `test-ehrlichkeit`).
- **Kein Gruen ohne Nachweis**: erfordert eine Seite Login oder ist keine Route eingetragen, endet der Test `Inconclusive`, nicht `Pass`.
- **Tab-Management**: eine Page je Test, `[TearDown]` schliesst sie — lange Laeufe hinterlassen keine Tabs.
- **Auth-Redirects sind kein Fehler**: `ERR_HTTP_RESPONSE_CODE_FAILURE` wird per HTTP-Request ohne Auto-Redirect auf Cross-Origin-/Login-Redirect geprueft und als „Authentifizierung erforderlich" gemeldet.
- **Ausgabe fuer Maschinen**: alle Meldungen tragen Marker (`[PAGE_OK]`, `[PAGE_ERROR]`, `[SUMMARY]` …) — Liste in `README.projekt.md`.
