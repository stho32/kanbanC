# Developer Onboarding

## Voraussetzungen

| Tool | Version | Installation | Pruefen |
|---|---|---|---|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) | `dotnet --version` |
| PowerShell | 7+ | [github.com/PowerShell](https://github.com/PowerShell/PowerShell) | `pwsh --version` |
| Docker | 24+ | [docs.docker.com](https://docs.docker.com/get-docker/) | `docker --version` |
| Git | 2.40+ | [git-scm.com](https://git-scm.com/) | `git --version` |
| IDE | VS Code / Visual Studio / Rider | — | — |

## Setup in einem Befehl

```bash
git clone <repo-url> && cd MyApp && dotnet build
```

## Entwicklungsserver starten

```bash
# Aus dem Root-Verzeichnis
dotnet run --project Source/MyApp.Web/

# Mit Hot Reload
dotnet watch --project Source/MyApp.Web/

# Mit spezifischem Port
dotnet run --project Source/MyApp.Web/ --urls "https://localhost:5001;http://localhost:5000"
```

Die Anwendung ist dann unter `https://localhost:5001` erreichbar.

## Tests ausfuehren

```bash
# Alle Tests
dotnet test

# Nur Unit Tests (schnell, kein Server noetig)
dotnet test Source/MyApp.BL.Tests/

# Nur Integration Tests
dotnet test Source/MyApp.BL.IntegrationTests/

# Playwright-Browser installieren (einmalig)
dotnet build Source/MyApp.PlaywrightTests/
pwsh Source/MyApp.PlaywrightTests/bin/Debug/net10.0/playwright.ps1 install

# E2E Tests
dotnet test Source/MyApp.PlaywrightTests/

# Mit Coverage
dotnet test Source/MyApp.BL.Tests/ --collect:"XPlat Code Coverage"
```

## Build erstellen

```bash
# Debug-Build
dotnet build

# Release-Build
dotnet build -c Release

# Publish fuer Deployment
dotnet publish Source/MyApp.Web/ -c Release -o publish/
```

## Projekt-Layout (Quick Map)

```
MyApp/
├── CLAUDE.md                          ← Projekt-Konventionen fuer AI-Assistenz
├── README.md                          ← Projekt-Uebersicht
├── MyApp.sln                          ← Solution-Datei (oeffnen in IDE)
├── Anforderungen/                     ← Anforderungsdokumente (RXXXXX)
├── Dokumentation/                     ← Projektdokumentation
└── Source/                            ← Gesamter Quellcode
    ├── MyApp.Web/                     ← Blazor Server App (hier starten)
    │   ├── Program.cs                 ← Einstiegspunkt, DI, Middleware
    │   └── Components/Pages/          ← Blazor-Seiten
    ├── MyApp.BL/                      ← Business Logic (hier Logik schreiben)
    │   ├── Operations/                ← Reine Logik (IOSP)
    │   └── Integrations/              ← Orchestrierung (IOSP)
    ├── MyApp.BL.Tests/                ← Unit Tests
    ├── MyApp.BL.IntegrationTests/     ← Integration Tests
    └── MyApp.PlaywrightTests/         ← E2E Browser Tests
```

## Umgebungsvariablen

Fuer lokale Entwicklung sind normalerweise keine Umgebungsvariablen noetig. Die Standardkonfiguration in `appsettings.Development.json` reicht aus.

Falls eine Datenbank verwendet wird:
```
ConnectionStrings__DefaultConnection=Host=localhost;Database=myapp;Username=postgres;Password=postgres
```

## Externe Services

Falls die Anwendung eine Datenbank benoetigt:

```bash
# PostgreSQL via Docker starten
docker compose -f docker/docker-compose.yml up db -d

# Oder SQLite (keine externe Abhaengigkeit)
# In appsettings.Development.json konfigurieren
```

## IOSP-Kurzreferenz

Beim Schreiben von Code in `MyApp.BL`:

- **Neue Logik?** → Statische Methode in `Operations/` (keine DI, keine Aufrufe eigener Methoden)
- **Orchestrierung?** → Klasse in `Integrations/` (DI via Constructor, nur Aufrufe, keine Logik)
- **Neues Modell?** → Record/Class in `Models/` (reine Daten)
- **Externe Abhaengigkeit?** → Interface in `Interfaces/`, Implementierung im Web-Projekt

## Haeufige Probleme

| Problem | Loesung |
|---|---|
| `dotnet: command not found` | .NET SDK installieren und `PATH` pruefen |
| Port bereits belegt | `--urls` Parameter mit anderem Port verwenden |
| Playwright-Browser fehlen | `pwsh .../playwright.ps1 install` ausfuehren |
| Hot Reload funktioniert nicht | `dotnet watch` statt `dotnet run` verwenden |
| NuGet-Restore schlaegt fehl | `dotnet nuget locals all --clear` und erneut `dotnet restore` |
| CSS-Aenderungen nicht sichtbar | Browser-Cache leeren (Ctrl+Shift+R) |

## IDE-Setup

### Visual Studio
- Solution-Datei `MyApp.sln` oeffnen
- Startprojekt: `MyApp.Web`

### Visual Studio Code
- Ordner oeffnen, C# Dev Kit Extension installieren
- `F5` startet die Anwendung (`.vscode/launch.json` wird automatisch erstellt)

### JetBrains Rider
- Solution-Datei `MyApp.sln` oeffnen
- Run Configuration zeigt auf `MyApp.Web`
