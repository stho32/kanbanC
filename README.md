# KanbanC

Ein lokal betriebenes Kanban-Board, auf dem Menschen und KI-Agenten gleichberechtigt arbeiten. Was die Weboberfläche kann, kann auch die API — Boards anlegen, gestalten, Karten führen, Zeiten erfassen —, ohne Rate Limits und ohne Kosten. Jede Änderung erscheint unverzüglich in allen offenen Sichten.

Das vollständige Zielbild steht in [Anforderungen/R00000-vision.md](Anforderungen/R00000-vision.md), der geplante Umfang in [Dokumentation/Planung/kanbanc.md](Dokumentation/Planung/kanbanc.md).

## Tech-Stack

| Technologie | Version | Zweck |
|---|---|---|
| .NET | 10.0 | Runtime und SDK |
| ASP.NET Core | 10.0 | Web-Framework |
| Blazor Server | 10.0 | Weboberfläche (Server-Side Rendering über SignalR) |
| C# | 13 | Programmiersprache |
| Dapper | 2.x | Micro-ORM — SQL bleibt sichtbar |
| Microsoft.Data.Sqlite | 10.0 | lokale Datenhaltung |
| NUnit | 4.x | Unit- und Integrationstests |
| Microsoft.Playwright.NUnit | 1.x | E2E-Browsertests |

## Aufbau

```
KanbanC.Blazor  ──→  KanbanC.Contracts  ←──  KanbanC.BL  ←──  KanbanC.WebApi
       └──────────── HTTP + Live-Push ─────────────────────────────┘
```

| Projekt | Zweck |
|---|---|
| `Source/KanbanC.Blazor` | Weboberfläche — **keine Projektreferenz auf die BL** |
| `Source/KanbanC.WebApi` | REST und Live-Push, einziger Datenweg |
| `Source/KanbanC.BL` | Fachlogik nach IOSP (`Operations`, `Integrations`, `Interfaces`, `Models`, `Persistenz`) |
| `Source/KanbanC.Contracts` | DTOs, von Oberfläche und API geteilt |
| `Source/KanbanC.BL.Tests` | Unit-Tests |
| `Source/KanbanC.WebApi.IntegrationTests` | Integrationstests über `WebApplicationFactory` |
| `Source/KanbanC.PlaywrightTests` | E2E-Tests gegen die Oberfläche |

Die Oberfläche referenziert die Fachlogik bewusst **nicht**. Eine Funktion, die es in der Oberfläche gibt, aber nicht in der API, lässt sich damit nicht bauen — die API-Vollständigkeit ist baulich erzwungen, nicht bloß vorgenommen.

## Starten

Zwei Prozesse, zwei Terminals — die API zuerst:

```bash
dotnet run --project Source/KanbanC.WebApi      # http://localhost:5280
dotnet run --project Source/KanbanC.Blazor      # http://localhost:5180
```

Beide lauschen auf `0.0.0.0` und sind damit im LAN erreichbar. Full-Trust: keine Anmeldung, wer die Oberfläche öffnet, wählt aus, wer er ist.

| Adresse | Inhalt |
|---|---|
| `http://localhost:5180` | Weboberfläche |
| `http://localhost:5280/api/zustand` | Bereitschaft der API |
| `http://localhost:5280/openapi/v1.json` | OpenAPI-Beschreibung (nur Development) |

## Entwicklung

```bash
dotnet build                                    # baut alles, Warnungen sind Fehler
dotnet test                                     # Unit, Integration, E2E
dotnet test Source/KanbanC.BL.Tests             # nur Unit
```

Die Playwright-Browser sind einmalig zu installieren: `npx playwright@1.62.0 install chromium`.

## Datenhaltung

SQLite unter `kanbanc.db` im Arbeitsverzeichnis der API (Pfad in `appsettings.json`, Abschnitt `Datenhaltung`). Das Schema entsteht aus versionierten, idempotenten `.sql`-Dateien unter `Source/KanbanC.BL/Persistenz/Migrationen/`.
