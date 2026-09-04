---
id: R00009
status: Neu
datum: 2026-09-04
---

# R00009: Kartenzahl je Spalte anzeigen

## Beschreibung

Ein Board lässt sich so einstellen, dass jede Spaltenbahn die Zahl der in ihr liegenden Karten in ihrer Kopfzeile trägt. Die Einstellung gehört dem **Board**, nicht dem Browser: sie ist über die API und über einen Schalter in der Navigationszeile setzbar, gilt für jeden Betrachter und überlebt einen Neustart. Voreinstellung ist `aus` — bestehende Boards ändern ihr Aussehen nicht von selbst. Bei eingeschaltetem Board folgt die Zahl dem Anlegen und Ablegen einer Karte ohne Reload.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

## Geschäftlicher Nutzen

Die Kartenzahl je Bahn ist die kleinste Auswertung, die ein Kanban-Board kennt: sie sagt auf einen Blick, wo sich Arbeit staut, ohne dass jemand zählt. Sie ist zugleich die erste Board-**Einstellung** überhaupt — bis heute hat ein Board nur Stammdaten (Name, Art, Termine) und Struktur (Spalten, Karten), aber nichts, was seine Darstellung steuert. Mit dieser Anforderung entsteht der Ort, an dem solche Schalter liegen, samt Endpunkt und Migrationsmuster; die nächsten Einstellungen (Anzeigegrenzen, Filter, Sichtbarkeiten) folgen ihm, statt jede für sich eine Spalte an `Board` zu hängen.

## Funktionale Anforderungen

- Ein Board merkt sich, ob es die Kartenzahl je Spalte zeigt; die Einstellung ist Teil des Boards und für jeden Betrachter dieselbe.
- Die API bietet das Umschalten als eigenen Aufruf an und liefert den Wert beim Lesen eines Boards mit.
- Die Oberfläche bietet dasselbe als Schalter in der Navigationszeile an.
- Bei eingeschaltetem Board steht in jeder Bahnenkopfzeile die Zahl der enthaltenen Karten; bei ausgeschaltetem bleibt die Stelle leer.
- Die Zahl folgt dem Anlegen und dem Ablegen einer Karte, ohne dass die Seite neu geladen wird.
- Ein unbekanntes Board wird beim Umschalten mit einem Befund zurückgewiesen, der Grund, Werte und Kompensationsaktion nennt.

## Nicht-funktionale Anforderungen

- **Benutzerfreundlichkeit:** Der Schalter ist ein Kontrollfeld mit Beschriftung, kein Knopf — die Kartenzahl ist ein Zustand, der stehen bleibt, keine Handlung. Er fügt sich in die 35,2 px hohe Navigationszeile ein, ohne sie zu sprengen.
- **Datenhaltung:** Die Migration ist idempotent; der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-23`).
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").

## Akzeptanzkriterien

### Die Einstellung am Board

- [ ] Ein neu angelegtes Board zeigt die Kartenzahl **nicht**; `GET /api/boards/{boardId}` liefert `zeigtKartenzahl: false`.
- [ ] Ein Board, das vor dieser Anforderung angelegt wurde, liefert nach der Migration ebenfalls `false` — es gibt keine Zeile für es, und „keine Zeile" heißt `aus`.
- [ ] `PUT /api/boards/{boardId}/kartenzahl` mit `{ "zeigtKartenzahl": true }` antwortet mit HTTP 200 und dem Board, dessen `zeigtKartenzahl` `true` ist.
- [ ] Ein anschließendes `GET /api/boards/{boardId}` liefert denselben Wert; ein zweites Einschalten ändert nichts, ein Ausschalten setzt zurück.
- [ ] Nach einem Neustart der WebApi auf derselben Datei steht die Einstellung unverändert da.
- [ ] Ein zweiter Lauf der Migration auf einer bestehenden Datei lässt Schema **und** Daten unverändert — eine eingeschaltete Einstellung bleibt eingeschaltet.

### Zurückweisung und Fehlerpfade

- [ ] `PUT /api/boards/{boardId}/kartenzahl` auf eine unbekannte `boardId` antwortet mit HTTP 404 **und einem Rumpf**: mindestens ein Befund mit nichtleerem `Code`, einer `Meldung`, welche die aufgerufene Nummer nennt, und einer `Kompensation`, die einen ausführbaren nächsten Aufruf enthält.
- [ ] Nach einer solchen Zurückweisung ist nichts geschrieben: die Tabelle der Boardeinstellungen trägt keine Zeile für die unbekannte Nummer.
- [ ] Ist die WebApi beim Umschalten nicht erreichbar, erscheint an der Board-Seite die Ausfallmeldung statt einer Ausnahmeseite; das Board bleibt bedienbar.

### Der Schalter in der Oberfläche

- [ ] Auf der Board-Seite steht in der Navigationszeile ein beschriftetes Kontrollfeld `Kartenzahl`; sein Zustand entspricht beim Öffnen dem, was die API für dieses Board liefert.
- [ ] Ein Klick darauf schaltet die Einstellung um; das Board zeigt den neuen Zustand, ohne dass der Betrachter die Seite neu lädt.
- [ ] Nach einem Reload steht der Schalter unverändert — sein Zustand kommt vom Board, nicht aus dem Browser.
- [ ] Eine zweite, unabhängig geöffnete Sitzung (eigener Browser-Kontext) sieht beim Öffnen desselben Boards denselben Zustand, ohne dort etwas geschaltet zu haben.
- [ ] Wird die Einstellung über die API umgeschaltet, zeigt eine danach geöffnete Oberfläche den neuen Zustand.

### Die Zahl im Bahnenkopf

- [ ] Bei eingeschaltetem Board trägt jede Bahnenkopfzeile die Zahl der in dieser Bahn liegenden Karten. Rechenbeispiel: `Rückstand` mit drei Karten zeigt `3`, eine leere Bahn zeigt `0`.
- [ ] Bei ausgeschaltetem Board bleibt die Stelle **leer** — keine `0`, keine Klammer, kein Platzhalter.
- [ ] Wird eine Karte angelegt, steht in ihrer Bahn unmittelbar danach eine um eins höhere Zahl, ohne Reload.
- [ ] Wird eine Karte in eine andere Bahn abgelegt, sinkt die Zahl der Quellbahn um eins und steigt die der Zielbahn um eins, ohne Reload. Rechenbeispiel: `Rückstand` 3 / `In Arbeit` 1 → nach dem Zug 2 / 2; die Summe bleibt 4.
- [ ] Wird eine Karte innerhalb ihrer Bahn verschoben, ändert sich keine Zahl.
- [ ] Die API liefert **keine** zweite Zahl neben den Karten einer Spalte: die angezeigte Zahl ist die Länge der Kartenliste, die `GET /api/boards/{boardId}` für diese Spalte ausgibt. Es entsteht kein zweiter Ort, der gepflegt werden müsste.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Boards/` — `Board` wächst um `ZeigtKartenzahl`; neu ist `Kartenzahlanzeige` als Rumpf des Umschalt-Aufrufs. `BoardUebersicht` bleibt unverändert (die Liste zeigt keine Bahnen).
- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/004-boardeinstellung.sql` — neue, idempotente Migration.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Boards/BoardRepository.cs` (erweitert: `Lade` liest die Einstellung mit, neue Schreibmethode), `Source/KanbanC.BL/Interfaces/Boards/IBoardRepository.cs` (erweitert).
- **Fachlogik:** `Source/KanbanC.BL/Integrations/Boards/BoardService.cs` (erweitert). Kein neuer Validator — ein Wahrheitswert hat keinen ungültigen Fall.
- **API:** `Source/KanbanC.WebApi/Endpunkte/BoardEndpunkte.cs` (erweitert um die Unterressource `kartenzahl`); `Nichtgefunden.Board` und `Zurueckweisungen.AlsNichtgefunden` werden unverändert benutzt.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/BoardApiKlient.cs` (erweitert), `Source/KanbanC.Blazor/Components/Pages/Board.razor` (Schalter in `SectionContent kopfzeile-bedienung`), `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (+ `.razor.css`: die Zahl in der schon reservierten Stelle `.spaltenbahn-kartenzahl`).
- **Tests:** `Source/KanbanC.BL.Tests/Integrations/Boards/BoardServiceTests.cs` und `TestHelpers/TestBoardRepository.cs`, `Source/KanbanC.Blazor.Tests/Services/BoardApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Boards/BoardRepositoryTests.cs`, `Api/BoardEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Source/KanbanC.PlaywrightTests/` (Seitenobjekt `BoardSeite` und eine neue Testklasse).
- **Unberührt:** `wwwroot/gestaltung.css` und `oberflaeche.css` — die Klasse `.kontrollfeld` existiert dort bereits (`oberflaeche.css:48`) und wird benutzt, nicht geändert.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0001.dc.html`](../Dokumentation/Wireframes/D0001.dc.html), **Zustand 4** (`Kartenzahl je Spalte · I0004`, Zeilen 544–570), ist die Gestaltungsvorgabe. Daraus gelten für diese Anforderung zwei Stellen: das Kontrollfeld in **Zone 3** der Navigationszeile, links vom Layout-Schalter, und die Zahl im **Bahnenkopf** an der Stelle, die `.spaltenbahn-kartenzahl` heute leer reserviert (`Spaltenbahnen.razor:20`). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md`), die Dateien im Repository sind damit der einzige Stand; ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung** — aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Was es an Umfang klärt: die dort gezeichnete Form **`20+`** der Abschlussspalte gehört **nicht** hierher. Sie setzt die Kürzung der Bahn aus `I0013` (Erledigte Karten gebündelt sehen, `rot`) voraus; solange die Bahn alle Karten hält, ist die genaue Zahl die richtige Anzeige.

### Ablauf

1. **Board öffnen**
   - 1.1 `BoardApiKlient.LadeBoard(boardId)` → `GET /api/boards/{boardId}`
   - 1.2 `BoardRepository.Lade` liest das Board mit `LEFT JOIN Boardeinstellung`; fehlt die Zeile, ist `ZeigtKartenzahl` `false`
   - 1.3 `Board.razor` setzt den Zustand des Kontrollfelds aus `_board.ZeigtKartenzahl` und reicht den Wert an `Spaltenbahnen` weiter
2. **Umschalten (Oberfläche)**
   - 2.1 Klick auf das Kontrollfeld in `SectionContent kopfzeile-bedienung`
   - 2.2 `BoardApiKlient.SchalteKartenzahl(boardId, new Kartenzahlanzeige(gewuenschterWert))` → `PUT /api/boards/{boardId}/kartenzahl`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 2.3 Erfolg → `LadeBoard()`; Kopfzeile und Bahnen zeigen den neuen Stand
   - 2.4 `HttpRequestException` → Ausfallmeldung, das Board bleibt stehen
3. **Umschalten (Fachlogik)**
   - 3.1 `BoardService.SchalteKartenzahl(boardId, anzeige)` reicht an das Repository durch — es gibt nichts zu prüfen außer der Existenz des Boards, und die kennt nur der Bestand
   - 3.2 Repository liefert `null` → `BoardEndpunkte` antwortet mit `Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId))` (HTTP 404 mit Rumpf)
4. **Schreiben (Datenzugriff)**
   - 4.1 `BoardRepository.SetzeKartenzahlanzeige` öffnet eine Transaktion und liest **in ihr** das Board
     - 4.1.1 Board unbekannt → `null`, kein Schreibzugriff (SQLite erzwingt die Fremdschlüssel nicht — `SqliteVerbindungsfabrik` setzt kein `PRAGMA foreign_keys`, die Prüfung muss die Abfrage selbst leisten)
   - 4.2 `INSERT … ON CONFLICT(Board) DO UPDATE SET ZeigtKartenzahl = excluded.ZeigtKartenzahl`
   - 4.3 Commit, danach das Board zurücklesen und liefern
5. **Anzeigen**
   - 5.1 `Spaltenbahnen.razor` schreibt bei `ZeigtKartenzahl` die Länge von `spalte.Karten` in `.spaltenbahn-kartenzahl`, sonst nichts
   - 5.2 Anlegen und Ablegen einer Karte laden das Board ohnehin neu (`Board.razor`, `LadeBoard`); die Zahl folgt daher ohne eigenen Mechanismus

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `BoardEndpunkte` — die neue Route ist eine **Unterressource** (`/api/boards/{boardId}/kartenzahl`), wie `spalten/reihenfolge` und `karten/{karteId}/lage` es schon sind. Damit bleibt `PUT /api/boards/{boardId}` frei für `I0005` (Board umbenennen und archivieren).
- `Board.razor` — die Seite kennt das Board und die Kopfzeilen-Sektion; sie ist der einzige Ort, der Schalter und Bahnen gleichzeitig sieht.
- `Spaltenbahnen.razor` — die reservierte Stelle existiert seit `B0063`; sie wird gefüllt, nicht angelegt.
- `IBoardRepository` — die vierte Methode; sie zieht `TestBoardRepository` und die Service-Tests nach.

**Klassen-Entwurf:**

- `Kartenzahlanzeige` (Contract, DTO, immutable) — der gewünschte Zustand, als Rumpf des Umschalt-Aufrufs. Ein eigener Typ statt eines nackten `bool`, damit der Rumpf ein benanntes Feld trägt und ein Agent im JSON sieht, was er setzt.
  - `public record Kartenzahlanzeige(bool ZeigtKartenzahl)`
- `Board` (Contract, DTO, immutable) — wächst um ein Feld am Ende, damit die vorhandenen Aufrufstellen ihre Reihenfolge behalten.
  - `public record Board(long BoardId, string Name, BoardArt Art, DateOnly? Starttermin, DateOnly? Zieltermin, IReadOnlyList<Spalte> Spalten, bool ZeigtKartenzahl)`
- `IBoardRepository` / `BoardRepository` (Provider, Ressourcenzugriff) — schreibt die Einstellung in einer Transaktion und liest das Board zurück; `null` heißt „dieses Board gibt es nicht".
  - `Board? SetzeKartenzahlanzeige(long boardId, Kartenzahlanzeige anzeige)`
- `BoardService` (Integration) — verdrahtet; ohne Validator, weil ein Wahrheitswert keinen ungültigen Fall hat.
  - `Board? SchalteKartenzahl(long boardId, Kartenzahlanzeige anzeige)`
  - Die WBS-Bubble `B0111` notiert `Ergebnis<Board>`. Ein `Ergebnis` ohne möglichen Zurückweisungsfall trägt nichts; ob es dennoch der Einheitlichkeit halber genommen wird, entscheidet der Entwickler beim Bauen — an den HTTP-Antworten ändert sich dadurch nichts.
- `BoardEndpunkte` (Integration, statisch) — die Unterressource neben den drei vorhandenen Routen.
  - `routen.MapPut(Basisroute + "/{boardId:long}/kartenzahl", SchalteKartenzahl).WithName("KartenzahlSchalten")`
- `BoardApiKlient` (Integration, Blazor) — der HTTP-Weg der Oberfläche; 404 wird zu `null` wie bei `LadeBoard`.
  - `public Task<Board?> SchalteKartenzahl(long boardId, Kartenzahlanzeige anzeige)`
- **Migration** `004-boardeinstellung.sql` (Skript, idempotent) — eine Zeile je Board, der Fremdschlüssel **ist** der Schlüssel:
  ```sql
  CREATE TABLE IF NOT EXISTS Boardeinstellung
  (
      Board           INTEGER PRIMARY KEY REFERENCES Board (BoardId),
      ZeigtKartenzahl INTEGER NOT NULL
  );
  ```
  Der Spaltenname `Board` folgt der Fremdschlüsselregel des Projekts (Fremdschlüssel tragen den Namen der referenzierten Tabelle). Ein zusätzliches `BoardeinstellungId` gibt es bewusst nicht: es erlaubte zwei Einstellungszeilen für dasselbe Board.

### Änderungen an bestehenden Klassen

- `Board` (Contract) — ein Feld mehr. Betroffen sind **acht** Konstruktionsstellen: `BoardRepository` (2), `TestBoardRepository` (1), `BoardServiceTests` (4), `ApiErgebnisTests` (1). Nachgezählt am 2026-09-04 gegen `a941f9b`.
- `BoardRepository.Lade` — `LEFT JOIN Boardeinstellung`; `LegeAn` liefert `false` mit, ohne eine Zeile anzulegen (die Voreinstellung ist die Abwesenheit der Zeile).
- `BoardService`, `IBoardRepository`, `BoardEndpunkte`, `BoardApiKlient` — je eine Methode mehr, siehe Grobentwurf.
- `Board.razor` — nimmt das Kontrollfeld in die Kopfzeilen-Sektion auf, ruft den Klienten und reicht `ZeigtKartenzahl` an `Spaltenbahnen` weiter.
- `Spaltenbahnen.razor` (+ `.razor.css`) — neuer Parameter `ZeigtKartenzahl`; die Stelle `.spaltenbahn-kartenzahl` verliert ihr `aria-hidden`, sobald sie eine Zahl trägt.
- `TestBoardRepository` — die neue Methode samt Beobachterflag, damit ein Test beweisen kann, dass ein unbekanntes Board **nicht** schreibt.
- `BoardSeite` (Seitenobjekt der E2E-Tests) — der Locator `Kartenzahlstellen` existiert bereits (`BoardSeite.cs:45`); hinzu kommt einer für das Kontrollfeld.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `BoardService.SchalteKartenzahl` — gegen `TestBoardRepository`: ein bekanntes Board liefert das Board mit dem gesetzten Wert; ein unbekanntes liefert `null` **ohne** Schreibzugriff (Beobachterflag).
- `BoardApiKlient.SchalteKartenzahl` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Board mit dem neuen Wert, 404 liefert `null`; geprüft wird zusätzlich, dass Methode und Adresse des abgesetzten Aufrufs stimmen. Diese Pfade sind über den Browser nicht auslösbar.

**Integration:**
- `BoardRepository.SetzeKartenzahlanzeige` gegen eine `TemporaereDatenbank` — Einschalten, erneutes Einschalten (Wert bleibt, keine zweite Zeile), Ausschalten, unbekanntes Board liefert `null` und legt keine Zeile an; `Lade` liefert danach den geschriebenen Wert, und ein Board ohne Einstellungszeile liefert `false`.
- `Migrationslaeufer` — zweiter Lauf auf einer Datei mit eingeschalteter Einstellung: Schema und Wert unverändert.
- `BoardEndpunkte` über `TestWebApi` — 200 mit dem Board, 404 mit Rumpf; `GET /api/boards/{boardId}` liefert danach denselben Wert.
- `FehlervertragTests` — die neue Route wird in die Prüfung aufgenommen, die für **jede** Fehlerantwort `Code`, `Meldung` und `Kompensation` nichtleer verlangt.
- `WebApiNeustartTests` — eine eingeschaltete Einstellung übersteht den Neustart.

**E2E:** Schalter einschalten und die Zahlen in den Bahnen sehen (US-1); Reload und der Stand bleibt (US-2); eine **zweite Sitzung** in eigenem Browser-Kontext sieht denselben Stand (US-3 — dieser Test ist der Beweis, dass die Einstellung am Board hängt und nicht am Browser); Karte anlegen und Karte in eine andere Bahn ziehen, die Zahlen folgen ohne Reload (US-4); über die API geschaltet, in der Oberfläche gesehen (US-5); ausgeschaltet bleibt die Stelle leer (US-1). Dazu laufen alle E2E-Tests aus `R00001`–`R00008` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `R00002` (Spalten gestalten, erledigt — `I0003`, grün) und `R00006` (Karten anlegen und am Board sehen, erledigt — `I0011`, grün). Beide Vorbedingungen der WBS-Spalte `Braucht` von `I0004` sind erfüllt; der Slice ist **frei**.
- Setzt außerdem auf: `R00005` (Kopfzeile in drei Zonen — dort sitzt der Schalter), `R00007` (Fehlervertrag — `Nichtgefunden.Board` und `Zurueckweisungen.AlsNichtgefunden` werden unverändert benutzt, es entsteht kein neuer Fehlercode).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0004` in seiner Spalte `Braucht` (geprüft am 2026-09-04 über `Dokumentation/Planung/kanbanc.md`).
- Reihenfolge innerhalb der Anforderung: `F0023` (Schalter) vor `F0024` (Zahl im Bahnenkopf) — die Zahl hat ohne die Einstellung keinen Schalter, der sie sichtbar macht. `F0024` nennt `F0023` in `Braucht`.

## Umfang

```
Kartenzahl je Spalte anzeigen (I0004) = 10 Bubbles: 10 Standard (12h), 0 unklar.
Rest: 12h klar · 4 von 10 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 10 Bubbles gruen (0 %) · 0 laufen · 10 offen
```

`I0004` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0023` Kartenzahl je Board schalten | B0108–B0115 (8) | 9,6h klar | `I0003`, `I0011` |
| `F0024` Zahl im Bahnenkopf | B0116–B0117 (2) | 2,4h klar | `F0023` |

Belegt sind die vier Datenzugriffs- und Verdrahtungs-Bubbles (`B0108`–`B0111`, Vergleichswerte `B0002`, `B0004`, `B0028`, `B0029` in `Schaetzungen/_ist-zeiten.md`); die fünf Endpunkt-, Klienten-, UI- und E2E-Bubbles sowie `B0116` tragen Richtwerte. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- ~~Gehört der Schalter dem Board oder dem Browser?~~ — entschieden am 2026-09-04: **dem Board**, persistiert und für jeden Betrachter gleich. Belege: das Fertig-Kriterium sagt „je Board einschaltbar" (`Dokumentation/Planung/kanbanc.md:72`), und die Vision verlangt eine API auf Augenhöhe mit der Oberfläche (`Anforderungen/R00000-vision.md:71-73`) — ein reiner Browserzustand bliebe ohne Endpunkt und für einen Agenten unsichtbar.
- ~~Voreinstellung an oder aus?~~ — entschieden am 2026-09-04: **aus**. Bestehende Boards ändern ihr Aussehen nicht von selbst; wer die Zahl will, schaltet sie ein. Nicht geprüft, ob der Mensch sie lieber überall gleich an hätte — das wäre eine Zeile Migration und kann jederzeit nachgezogen werden.
- ~~Eigene Tabelle oder Spalte an `Board`?~~ — entschieden am 2026-09-04: **eigene Tabelle `Boardeinstellung`**. `ALTER TABLE Board ADD COLUMN` ist in SQLite nicht idempotent und scheitert beim zweiten Lauf des `Migrationslaeufer`, der jedes Skript bei jedem Start ausführt.
- ~~Eigener Endpunkt oder `PUT /api/boards/{boardId}`?~~ — entschieden am 2026-09-04: **eigene Unterressource** `kartenzahl`, wie `spalten/reihenfolge` und `karten/{karteId}/lage`. So bleibt der Board-`PUT` für `I0005` (Umbenennen, Archivieren) frei.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start der WebApi mit; bestehende Datenbestände bekommen keine Zeile und damit die Voreinstellung `aus`.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist, dass die Bahnen seit `R00006` Karten tragen, aber niemand sieht, wie viele — bei einer vollen Bahn zählt man von Hand oder scrollt, und genau die Frage „wo staut es sich" ist die, wegen der man ein Kanban-Board benutzt. Wenn die Zahl in der Kopfzeile steht, beantwortet das Board diese Frage im Vorbeigehen, und weil sie aus der gelieferten Kartenliste gerechnet wird, kann sie nicht von der Wirklichkeit abweichen — es gibt keinen zweiten Ort, der gepflegt werden müsste und veralten könnte. Dass der Schalter dazu eine Eigenschaft des **Boards** wird und nicht des Browsers, ist der eigentliche Hebel: eine Einstellung im Browser sähe für einen Menschen gleich aus, hätte aber keinen Endpunkt — und damit wäre zum ersten Mal etwas in der Oberfläche schaltbar, das ein Agent nicht schalten kann. Die Zusage „was ein Mensch klicken kann, kann ein Agent aufrufen" hält nicht dadurch, dass man sie wiederholt, sondern dadurch, dass jede einzelne Entscheidung wie diese sie einlöst. Der Hebel sitzt genau hier und nicht später, weil `Boardeinstellung` mit dieser Anforderung als Ort für **jede** künftige Board-Einstellung entsteht; wer die erste Einstellung als Browserzustand baut, muss die zweite entweder genauso bauen oder die erste umbauen.

## Missing-Docs

- **SQLite `ON CONFLICT … DO UPDATE` unter Microsoft.Data.Sqlite mit Dapper.** Der UPSERT ist im Bestand bisher nirgends benutzt; ob die Klausel in der eingesetzten Fassung ohne Weiteres durchgeht und wie sie sich innerhalb einer laufenden Transaktion verhält, ist nicht belegt. Vor dem Bauen mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`).
- **Zweiter Browser-Kontext in der vorhandenen Playwright-Testumgebung.** `Testumgebung` und `Dienstprozess` starten beide Prozesse je Testlauf; ob eine zweite, unabhängige Sitzung im selben Test ohne Umbau der Infrastruktur zu haben ist, steht nirgends. Davon hängt der Test ab, der beweist, dass die Einstellung am Board und nicht am Browser hängt.

## Notizen

### Verworfene Alternativen

- **Browser-Zustand (`localStorage` oder Komponentenzustand), wie beim Layout-Modus.** Kleinster Umfang, keine Migration, kein Endpunkt. Verworfen: „je Board einschaltbar" wäre nicht erfüllt, und eine für Agenten unsichtbare Oberflächenfunktion höhlt die Kernzusage der Vision aus. Der Layout-Modus darf Browserzustand sein, weil er eine **Arbeitsweise** ist, die nach dem Reload endet — die Kartenzahl ist eine Eigenschaft des Boards, die stehen bleibt.
- **`ALTER TABLE Board ADD COLUMN ZeigtKartenzahl`.** Ein Feld weniger im Modell, kein JOIN. Verworfen: nicht idempotent, und der `Migrationslaeufer` führt jedes Skript bei jedem Start aus.
- **Ein Journal im `Migrationslaeufer`, damit `ADD COLUMN` möglich wird.** Die saubere Lösung des allgemeinen Problems — verworfen als Nebenwirkung dieses Slice: das ist ein eigener Umbau mit eigenen Tests und gehört in eine eigene Anforderung.
- **Gespeicherter Zähler je Spalte, beim Anlegen und Ablegen fortgeschrieben.** Schnellste Anzeige. Verworfen: ein zweiter Ort für dieselbe Wahrheit, der bei jedem Pfad (Anlegen, Verschieben, künftig Archivieren, Import) mitgepflegt werden müsste und beim ersten vergessenen Pfad lügt.
- **`PUT /api/boards/{boardId}` mit dem ganzen Board.** Eine Route weniger. Verworfen: der Aufruf gehört `I0005`, und ein Umschalten müsste dann Name, Art und Termine mitschicken — ein Agent, der nur einen Schalter umlegen will, müsste erst alles lesen.
- **Nackter `bool` als Rumpf** (`PUT … Body: true`). Verworfen: ein JSON-Rumpf ohne Feldnamen sagt nicht, was er bedeutet; `{"zeigtKartenzahl": true}` liest sich für Mensch und Agent von selbst.
- **Die Zahl aus einem eigenen Endpunkt holen** (`GET /api/boards/{boardId}/kartenzahlen`). Verworfen: `GET /api/boards/{boardId}` liefert die Karten je Spalte bereits mit; ein zweiter Aufruf für eine Länge ist Aufwand ohne Gegenwert.

### Bewusst out of scope

- **Die Form `20+`** der Abschlussspalte aus dem Artboard — sie setzt die Kürzung der Bahn aus `I0013` voraus und wird dort mitgebaut.
- **Anzeigegrenze oder WIP-Limit-Warnung** („5 / 3", rote Zahl bei Überschreitung). Die Anzeigegrenze der Abschlussspalte steht schon als Vermerk in der Bahn; sie mit der Kartenzahl zu verrechnen ist eine eigene Aussage und ein eigener Slice.
- **Live-Übertragung an andere offene Sichten.** Schaltet ein Betrachter um, sieht ein zweiter es erst beim nächsten Laden — das ist `I0028`, nicht dieser Slice. Das Kriterium „ohne Reload" gilt für den Betrachter, der handelt.
- **Weitere Board-Einstellungen** in derselben Tabelle. Sie ist so gebaut, dass Spalten hinzukommen können; welche, entscheidet der Slice, der sie braucht.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Die vier Entscheidungen unter „Offene Fragen" sind Annahmen mit Beleg, keine bestätigten Vorgaben; die WBS führt drei davon bereits als Annahme (`Dokumentation/Planung/kanbanc.md:213-216`). Wer eine davon anders will, ändert sie vor dem Bauen — nach `B0108` kostet die Tabellenfrage eine zweite Migration.
