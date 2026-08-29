---
id: R00001
status: Neu
datum: 2026-08-29
---

# R00001: Board anlegen

## Beschreibung

Ein Board entsteht mit Name und Art (Linie oder Projekt) und erscheint anschließend in der Board-Liste. Beim Anlegen bekommt es drei Standardspalten, damit es sofort benutzbar ist. Optional lassen sich Start- und Zieltermin setzen — unabhängig von der Art, damit ein Board später von Projekt nach Linie wechseln kann, ohne Daten zu verlieren.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zwei Sorten Board" und „Eine API auf Augenhöhe mit der Oberfläche".

Dies ist der erste umgesetzte Slice der Anwendung. Er trägt deshalb die gesamte vertikale Kette zum ersten Mal: SQLite-Datei, Migrationsläufer, Repository, API-Endpunkte, HTTP-Aufruf aus der Oberfläche und eine Blazor-Seite. Der fachliche Umfang ist klein, der bauliche nicht.

## Geschäftlicher Nutzen

Ohne Board gibt es nichts, woran gearbeitet werden kann — jeder weitere Slice der WBS hängt direkt oder mittelbar hier dran (`I0002`, `I0003`, `I0005`, `I0020`). Der Slice beweist außerdem zum ersten Mal, dass die Leitentscheidung des Projekts trägt: die Oberfläche kommt ausschließlich über die API an die Daten.

## Funktionale Anforderungen

- Ein Board wird mit Name und Art angelegt; Start- und Zieltermin sind optional.
- Board-Arten sind `Linie` (dauerhafte Arbeitsbegleitung) und `Projekt` (befristetes Vorhaben).
- Beim Anlegen entstehen drei Standardspalten: `Zu erledigen`, `In Arbeit`, `Erledigt`.
- Die letzte Standardspalte ist Abschlussspalte mit Anzeigegrenze 20.
- Alle Boards lassen sich auflisten; ein einzelnes Board ist samt seiner Spalten abrufbar.
- Jede dieser Fähigkeiten ist über die API und über die Oberfläche erreichbar.
- Ein Board wird über eine laufende Ganzzahl adressiert (`/api/boards/3`).
- Board-Namen müssen nicht eindeutig sein; die Identität ist die Nummer.

## Nicht-funktionale Anforderungen

- **Architektur:** `KanbanC.Blazor` erhält keine Projektreferenz auf `KanbanC.BL`. Die Oberfläche ruft ausschließlich die WebApi.
- **Datenhaltung:** eine SQLite-Datei für alle Boards; jede Zeile trägt ihre `BoardId`. Kein Sharding je Board.
- **Idempotenz:** Der Migrationsläufer ist mehrfach ausführbar, ohne zu scheitern und ohne Daten zu verändern.
- **Sicherheit:** Full-Trust — keine Authentifizierung, keine Rechteprüfung (Leitplanke der Vision).

## Akzeptanzkriterien

### Board anlegen
- [ ] `POST /api/boards` mit Name und Art legt ein Board an und liefert es mit vergebener Nummer zurück (HTTP 201).
- [ ] Das angelegte Board erscheint danach in `GET /api/boards`.
- [ ] Ein Board lässt sich mit Art `Linie` und mit Art `Projekt` anlegen.
- [ ] Start- und Zieltermin sind bei beiden Arten setzbar und werden zurückgegeben.
- [ ] Wird kein Termin angegeben, bleiben beide Felder leer — das Anlegen scheitert nicht.
- [ ] Zwei Boards mit demselben Namen sind erlaubt und erhalten verschiedene Nummern.
- [ ] Die erste vergebene Nummer ist 1, die zweite 2 — die Nummer wächst und wird nicht wiederverwendet.

### Standardspalten
- [ ] Ein neu angelegtes Board hat genau drei Spalten in der Reihenfolge `Zu erledigen`, `In Arbeit`, `Erledigt`.
- [ ] `GET /api/boards/{id}` liefert das Board samt dieser drei Spalten.
- [ ] Genau eine dieser Spalten ist als Abschlussspalte markiert: `Erledigt`.
- [ ] Die Abschlussspalte trägt die Anzeigegrenze 20.

### Zurückweisung ungültiger Eingaben
- [ ] Ein leerer oder nur aus Leerzeichen bestehender Name wird mit HTTP 400 zurückgewiesen; es entsteht kein Board.
- [ ] Ein Zieltermin vor dem Starttermin wird mit HTTP 400 zurückgewiesen (Start `2026-09-01`, Ziel `2026-08-01` → abgelehnt).
- [ ] Eine unbekannte Board-Art wird mit HTTP 400 zurückgewiesen.
- [ ] `GET /api/boards/{id}` auf eine nicht vergebene Nummer liefert HTTP 404.

### Oberfläche
- [ ] Die Board-Liste zeigt alle Boards mit Name und Art.
- [ ] Ein Formular legt ein Board mit Name, Art und optionalen Terminen an; danach steht es in der Liste.
- [ ] Eine Zurückweisung der API erscheint als lesbare Meldung, ohne dass die Seite abstürzt.
- [ ] Die Oberfläche erreicht die Daten ausschließlich über HTTP-Aufrufe der WebApi.

### Datenhaltung
- [ ] Beim Start der WebApi entsteht die SQLite-Datei mit dem Schema, falls sie fehlt.
- [ ] Ein zweiter Start ändert an einer bestehenden Datei nichts — angelegte Boards bleiben erhalten.

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Pages/` (Board-Seite), `Source/KanbanC.Blazor/Services/` (API-Klient als Integration).
- **API:** `Source/KanbanC.WebApi/Endpunkte/` — je Thema eine Datei mit Minimal-API-Endpunkten.
- **Fachlogik:** `Source/KanbanC.BL/Operations/`, `Integrations/`, `Interfaces/` nach IOSP; Datenzugriff unter `Source/KanbanC.BL/Persistenz/`, Schema unter `Source/KanbanC.BL/Persistenz/Migrationen/` als eingebettete `.sql`-Dateien.
- **Verträge:** `Source/KanbanC.Contracts/Boards/` — DTOs, die Oberfläche und API teilen.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/` und `Integrations/` (spiegeln die BL-Struktur), `Source/KanbanC.WebApi.IntegrationTests/Api/`, `Source/KanbanC.PlaywrightTests/Tests/` mit Seitenobjekt unter `PageObjects/`.

## Technische Überlegungen

### Ablauf

1. **Start der WebApi**
   - 1.1 Verbindungszeichenfolge aus `appsettings.json`, Abschnitt `Datenhaltung`, lesen
   - 1.2 `Migrationslaeufer.FuehreAus()` — eingebettete `.sql`-Dateien in Namensreihenfolge, jede idempotent
2. **Board anlegen** (`POST /api/boards`)
   - 2.1 `BoardAnlegenAnfrage` aus dem Rumpf lesen
   - 2.2 `BoardAnlegenValidator.Pruefe(anfrage)` — Name nicht leer, Art bekannt, Zieltermin nicht vor Starttermin
     - 2.2.1 Bei Befunden: HTTP 400 mit der Liste der Befunde, kein Schreibzugriff
   - 2.3 `StandardspaltenVorlage.FuerNeuesBoard()` — die drei Spalten als reine Logik
   - 2.4 `BoardRepository.LegeAn(anfrage, standardspalten)` — eine Transaktion, Board und Spalten
   - 2.5 HTTP 201 mit dem angelegten Board
3. **Boards auflisten** (`GET /api/boards`) → `BoardRepository.LadeAlle()`
4. **Board lesen** (`GET /api/boards/{id}`) → `BoardRepository.Lade(id)`; nicht gefunden → HTTP 404
5. **Oberfläche**
   - 5.1 `BoardApiKlient` ruft die Endpunkte über den benannten `HttpClient` „KanbanC"
   - 5.2 Die Seite zeigt Liste und Formular, meldet Fehler der API im Klartext

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** Die erste Migration legt das Schema an — ab hier existiert die Datenhaltung. `Program.cs` der WebApi bekommt den Migrationsaufruf und die Endpunkt-Registrierung. Die Blazor-Navigation bekommt den Eintrag zur Board-Seite; sie ist zugleich die erste Seite, die etwas Fachliches zeigt.

**KanbanC.Contracts**
- `BoardArt` (Enum) — `Linie`, `Projekt`
- `Board` (DTO, immutable record) — Nummer, Name, Art, Starttermin, Zieltermin, Angelegt-Zeitpunkt, Spalten
- `Spalte` (DTO, immutable record) — Nummer, Bezeichnung, Position, ob Abschlussspalte, Anzeigegrenze
- `Spalten` (benannte Collection) — beantwortet „welche Spalte ist die Abschlussspalte", „in welcher Reihenfolge stehen sie"
  - `Spalte? Abschlussspalte()`
- `Boards` (benannte Collection) — die Liste der Boards für Liste und API-Antwort
- `BoardAnlegenAnfrage` (DTO, immutable record) — Name, Art, Starttermin, Zieltermin

**KanbanC.BL**
- `IBoardRepository` (Provider, Interface) — Zugriff auf Boards und ihre Spalten
  - `Board LegeAn(BoardAnlegenAnfrage anfrage, Spalten standardspalten)`
  - `Boards LadeAlle()`
  - `Board? Lade(int boardNummer)`
- `BoardRepository` (Provider, Dapper) — Implementierung gegen SQLite; Board und Spalten in einer Transaktion
- `IDatenbankVerbindungsfabrik` (Provider, Interface) — liefert geöffnete Verbindungen
  - `IDbConnection Oeffne()`
- `SqliteVerbindungsfabrik` (Provider) — Implementierung über die Verbindungszeichenfolge
- `StandardspaltenVorlage` (Operation) — liefert die drei Spalten eines neuen Boards; reine Logik, keine Abhängigkeiten
  - `Spalten FuerNeuesBoard()`
- `BoardAnlegenValidator` (Operation) — prüft eine Anfrage; liefert Befunde, wirft nicht
  - `Pruefbefunde Pruefe(BoardAnlegenAnfrage anfrage)`
- `Pruefbefunde` (benannte Collection) — die Befunde einer Prüfung
  - `bool IstOhneBefund()`
- `BoardService` (Integration) — verdrahtet Validator, Vorlage und Repository; enthält keine eigene Logik
  - `Ergebnis<Board> LegeBoardAn(BoardAnlegenAnfrage anfrage)`
  - `Boards LadeAlleBoards()`
  - `Board? LadeBoard(int boardNummer)`
- `Migrationslaeufer` (Integration) — liest die eingebetteten `.sql`-Dateien und führt sie in Namensreihenfolge aus
  - `void FuehreAus()`
- `IMigrationsQuelle` (Provider, Interface) — liefert die Migrationsskripte
  - `MigrationsSkripte LadeAlle()`
- `EingebetteteMigrationsQuelle` (Provider) — liest sie aus den Ressourcen der Assembly

**KanbanC.WebApi**
- `BoardEndpunkte` (Integration, statische Registrierung) — bildet die drei Routen auf den `BoardService` ab und übersetzt Befunde in HTTP-Status
  - `static void Registriere(IEndpointRouteBuilder routen)`

**KanbanC.Blazor**
- `BoardApiKlient` (Integration) — ruft die WebApi über den benannten `HttpClient`; kennt kein SQL und keine BL
  - `Task<Boards> LadeAlleBoards()`
  - `Task<ApiErgebnis<Board>> LegeBoardAn(BoardAnlegenAnfrage anfrage)`
- `Components/Pages/Boards.razor` — Liste und Anlegeformular

**Migration**
- `Persistenz/Migrationen/001-boards-und-spalten.sql` — Tabellen `Board` und `Spalte`, beide mit `CREATE TABLE IF NOT EXISTS`; `Spalte.BoardNummer` als Fremdschlüssel. Formatierung nach Skill `sql-stil`.

### Änderungen an bestehenden Klassen

- `Source/KanbanC.WebApi/Program.cs` — Dienste registrieren (`IDatenbankVerbindungsfabrik`, `IBoardRepository`, `BoardService`), Migrationsläufer beim Start aufrufen, `BoardEndpunkte.Registriere(app)`. Der Platzhalter-Endpunkt `/api/zustand` bleibt.
- `Source/KanbanC.Blazor/Program.cs` — `BoardApiKlient` registrieren.
- `Source/KanbanC.Blazor/Components/Layout/NavMenu.razor` — Eintrag zur Board-Seite; die Beispielseiten `Counter` und `Weather` entfallen.
- `Source/KanbanC.BL/KanbanC.BL.csproj` — `.sql`-Dateien als `EmbeddedResource`.

## Tests

Nach Skill `test-pyramide`: alle drei Ebenen, die Given/When/Then-Szenarien der User Story werden E2E-Tests.

**Kandidaten für Unit-Tests (pure Logik nach IOSP):**
- `StandardspaltenVorlage` — liefert eine feste Struktur ohne Seiteneffekte; prüft Anzahl, Reihenfolge, Abschlussspalte, Anzeigegrenze.
- `BoardAnlegenValidator` — reine Entscheidungslogik; je ein Test für leeren Namen, unbekannte Art, Zieltermin vor Starttermin und den gültigen Fall.
- `Spalten.Abschlussspalte()` und `Pruefbefunde.IstOhneBefund()` — benannte Collections mit fachlicher Frage.

**Integration:** `BoardRepository` gegen eine echte SQLite-Datei im Temp-Verzeichnis (je Test frisch, danach gelöscht) — Anlegen mit Spalten in einer Transaktion, Auflisten, Lesen, laufende Nummernvergabe. `Migrationslaeufer` zweimal hintereinander auf derselben Datei (Idempotenz). Die drei Endpunkte über `WebApplicationFactory` samt Statuscodes 201, 400 und 404.

**E2E:** Board über die Oberfläche anlegen und in der Liste wiederfinden; Board mit Art `Projekt` und Terminen anlegen; leeren Namen absenden und die Fehlermeldung sehen. Anwendung auf einem freien Port (Skill `freier-port`), Server danach stoppen.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: keine. `I0001` ist in Welle 1 und hat keine Vorbedingung.
- Blockiert: `I0002` Boards auflisten und öffnen, `I0003` Spalten gestalten, `I0005` Board umbenennen und archivieren, `I0020` Klasse anlegen — alle vier tragen `I0001` in ihrer Spalte `Braucht`.

## Umfang

`I0001` ist bis zur Ebene Interaction geplant, nicht bis Bubble — es gibt keine Bubbles und damit keine Zählung. `/planung verfeinern I0001 --bis Bubble` liefert sie. Eine Schätzung wird hier bewusst nicht danebengeschrieben.

## Offene Fragen

Keine. Vier Entscheidungen wurden vor dem Schreiben getroffen und stehen unter „Notizen".

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass es die Anwendung noch nicht gibt: das Gerüst steht, aber kein einziger fachlicher Weg von der Oberfläche bis in die Datenhaltung. Das Zielbild verlangt ein Board, auf dem Mensch und Agent gleichberechtigt arbeiten — und dafür muss es überhaupt erst ein Board geben, das beide über denselben Weg erreichen. Wenn wir `POST /api/boards` samt Standardspalten bauen und die Blazor-Seite ausschließlich über diesen Endpunkt bedienen, dann existiert erstens der erste vollständige vertikale Schnitt (Schema, Repository, Endpunkt, Klient, Seite), und zweitens ist die Leitentscheidung des Projekts einmal real durchlaufen statt nur vereinbart — jede spätere Fähigkeit kopiert dann eine erprobte Kette statt eine erdachte. Gerade dieser Slice ist der Hebel und nicht `I0010 Board ansehen`, weil ohne Board nichts anzusehen ist und weil vier weitere Slices ihn als Vorbedingung führen; und nicht ein noch kleinerer Schnitt, weil ein Board ohne Spalten von außen nichts Prüfbares liefert und die Kette damit unbewiesen bliebe.

## Missing-Docs

- Dapper mit SQLite und Transaktionen über mehrere Tabellen: das Zusammenspiel `IDbConnection` / `IDbTransaction` / `connection.Execute(..., transaction)` ist aus der Erinnerung gebaut, nicht aus einer Quelle. Vor der Umsetzung prüfen.
- Eingebettete Ressourcen in .NET 10 (`EmbeddedResource`, `Assembly.GetManifestResourceNames`) — Namensbildung der Ressourcenschlüssel je nach Ordnerstruktur.

## Notizen

### Getroffene Entscheidungen

- **Standardspalten beim Anlegen** statt eines leeren Boards. Grund: ein Board ohne Spalten liefert von außen nichts Prüfbares. „Spalten gestalten" (`I0003`) bleibt davon unberührt — Standardspalten anlegen ist kein Gestalten.
- **Board-Art ist ein Etikett ohne Verhalten**, Start- und Zieltermin gelten für beide Arten. Damit kann ein Board von Projekt nach Linie wechseln, ohne Daten zu verlieren.
- **Laufende Ganzzahl als Nummer** statt GUID oder Slug — kurz zu lesen, im Dialog mit einem Agenten angenehm, passend zur Einzelinstanz.
- **Eine SQLite-Datei für alle Boards**, jede Zeile mit `BoardNummer`. Datei-pro-Board wurde geprüft und verworfen.

### Verworfene Alternativen

- *Board ohne Spalten anlegen* — strikt am Wortlaut des Fertig-Kriteriums, aber der Slice liefert allein nichts Benutzbares.
- *Je Board-Art ein eigenes Spaltenset* — nimmt Gestaltungsentscheidungen vorweg, die noch niemand getroffen hat.
- *Board-Art bestimmt den Lebenszyklus (nur Projektboards abschließbar)* — kein Slice der WBS verlangt das heute; wäre tote Flexibilität (C16).
- *GUID als Board-Identität* — kollisionsfrei bei verteiltem Betrieb, aber unhandlich in jeder URL und in jedem Gespräch mit der API. Nachrüstbar als zweite Spalte, falls die offene Richtungsfrage „Bleibt es lokal?" einmal anders beantwortet wird.
- *Slug aus dem Namen als Identität* — jede Umbenennung bräche bestehende Verweise, und Namen sind nicht eindeutig.
- *Eine SQLite-Datei je Board* — hätte Isolation und Verschickbarkeit gebracht, zersplittert aber genau den Datenbestand, aus dem die Auswertungen kommen sollen (`I0022`, `I0033`, `I0034`, `I0036`, `I0037`): Kontributoren wären dupliziert oder doch zentral, Cross-Board-Abfragen bräuchten `ATTACH` mit Standardgrenze 10, und jede Schemaänderung müsste über alle Dateien laufen. Portabilität kommt stattdessen später als Slice „Board exportieren/importieren".

### Out of scope

- Board umbenennen, archivieren oder löschen (`I0005`).
- Spalten anlegen, umbenennen, umsortieren, entfernen (`I0003`).
- Live-Aktualisierung der Board-Liste in anderen offenen Sichten (`D0007`).
- Karten jeder Art (`D0003`, `D0004`).
