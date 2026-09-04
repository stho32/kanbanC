---
id: R00010
status: Neu
datum: 2026-09-04
---

# R00010: Board umbenennen und archivieren

## Beschreibung

Ein Board bekommt nachträglich einen neuen Namen, und ein Board, das ausgedient hat, verschwindet aus der Standardliste, ohne gelöscht zu werden. Beides geht über die API und über die Oberfläche: umbenannt wird im ⋯-Menü der Kachel, archiviert ebenso; der Filter „archivierte“ im Fuß der Übersicht zeigt die abgelegten Boards und holt sie über „zurückholen“ zurück. Ein archiviertes Board bleibt unter seiner Nummer vollständig abrufbar — Archivieren ist das Verschwinden aus einer Liste, nicht das Ende des Boards.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

## Geschäftlicher Nutzen

Ein Board wird beim Anlegen benannt, bevor jemand weiß, wie das Vorhaben am Ende heißt; ohne Umbenennen bleibt der erste Wurf für immer stehen oder es entsteht ein zweites Board mit dem richtigen Namen und ohne Inhalt. Und weil KanbanC Projektboards führt, die auslaufen, wächst die Übersicht sonst monoton: jedes abgeschlossene Vorhaben bleibt zwischen den laufenden stehen, bis die Liste nur noch aus Vergangenheit besteht. Archivieren löst das, ohne die Auswertungen zu beschädigen — die Daten bleiben da, wo sie sind, und die späteren Soll-Ist- und Burndown-Auswertungen (`D0009`) finden ein archiviertes Board unverändert vor. Löschen wäre die einfachere Antwort und die falsche: es nimmt der Zeiterfassung ihre Geschichte.

## Funktionale Anforderungen

- Ein bestehendes Board bekommt einen neuen Namen; Art, Termine, Spalten und Karten bleiben davon unberührt.
- Ein leerer Name wird zurückgewiesen — mit demselben Befund wie beim Anlegen, aber mit der Kompensationsaktion dieser Route.
- Ein Board lässt sich archivieren und wieder zurückholen; beides ist derselbe Aufruf mit dem gewünschten Zustand.
- Die Standardliste zeigt nur die aktiven Boards; die archivierten sind über denselben Listenaufruf mit gesetztem Filter erreichbar.
- Ein archiviertes Board bleibt unter seiner Nummer vollständig abrufbar und sagt selbst, dass es archiviert ist.
- Die Oberfläche bietet Umbenennen, Archivieren und Zurückholen an der Kachel des Boards an; der Filter sitzt im Fuß der Übersicht.
- Ein unbekanntes Board wird bei jedem dieser Aufrufe mit einem Befund zurückgewiesen, der Grund, Werte und Kompensationsaktion nennt.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** Die Migration ist idempotent; der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-23`).
- **Fehlervertrag:** Jede Fehlerantwort trägt einen Rumpf mit `Code`, `Meldung` und `Kompensation` (R00007, geprüft von `FehlervertragTests`). Das gilt auch für den neuen Abfrageparameter der Liste.
- **Bedienbarkeit:** Umbenannt wird **in** der Kachel, nicht auf einem zweiten Schirm; die ganze Kachel bleibt der Weg ins Board, das ⋯-Menü liegt darüber.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"; geprüft von `GestaltungsfundamentTests`).

## Akzeptanzkriterien

### Umbenennen über die API

- [ ] `PUT /api/boards/{boardId}` mit `{ "name": "KanbanC — Release 2" }` antwortet mit HTTP 200 und dem Board, dessen `name` der neue ist.
- [ ] Ein anschließendes `GET /api/boards/{boardId}` liefert denselben Namen; `GET /api/boards` zeigt das Board mit dem neuen Namen an der Stelle, an die es alphabetisch gehört.
- [ ] `art`, `starttermin`, `zieltermin`, die Spalten und deren Karten sind nach dem Umbenennen unverändert — der Rumpf trägt nur den Namen, und was er nicht trägt, wird nicht geändert.
- [ ] Nach einem Neustart der WebApi auf derselben Datei steht der neue Name unverändert da.
- [ ] Zwei Boards dürfen denselben Namen tragen; Umbenennen auf einen schon vergebenen Namen wird **nicht** zurückgewiesen (die Eindeutigkeitsregel aus `R00004` gilt für Spaltenbezeichnungen je Board, nicht für Boardnamen).

### Umbenennen in der Oberfläche

- [ ] Jede Board-Kachel trägt ein ⋯-Bedienelement; ein Klick öffnet ein Menü mit „Umbenennen" und „Archivieren", ein zweiter Klick auf ⋯ schließt es wieder.
- [ ] Das offene Menü liegt über der Verweisfläche der Kachel: ein Klick auf einen Menüpunkt öffnet **nicht** das Board. Ein Klick auf die übrige Kachelfläche führt weiterhin ins Board.
- [ ] „Umbenennen" ersetzt den Namen in der Kachel durch ein Namensfeld mit „Speichern" und „Abbrechen"; kein zweiter Schirm, kein Dialogfenster.
- [ ] „Speichern" schreibt den Namen und zeigt die Liste mit dem neuen Namen; „Abbrechen" lässt den alten Namen stehen und schreibt nichts.
- [ ] Nach einem Reload der Board-Übersicht steht der neue Name da.
- [ ] Ein leerer Name führt zu einer lesbaren Meldung an der Kachel; das Namensfeld bleibt offen, und der alte Name ist unverändert gespeichert.
- [ ] Ist die WebApi beim Speichern nicht erreichbar, erscheint die Ausfallmeldung statt einer Ausnahmeseite; die Liste bleibt bedienbar.

### Archivieren und Zurückholen über die API

- [ ] `PUT /api/boards/{boardId}/archivierung` mit `{ "istArchiviert": true }` antwortet mit HTTP 200 und dem Board, dessen `istArchiviert` `true` ist.
- [ ] `GET /api/boards` liefert dieses Board danach **nicht** mehr; `GET /api/boards?archiviert=true` liefert es. Rechenbeispiel: drei Boards, eines archiviert → die Standardliste hat zwei Einträge, die archivierte Liste einen, zusammen drei.
- [ ] `GET /api/boards/{boardId}` liefert das archivierte Board unverändert — mit Spalten, Karten und allen Feldern; es trägt zusätzlich `istArchiviert: true`.
- [ ] Derselbe Aufruf mit `{ "istArchiviert": false }` holt es zurück: es steht wieder in `GET /api/boards` und fehlt in der archivierten Liste.
- [ ] Ein zweites `true` auf dasselbe Board ändert nichts und antwortet wie das erste; ein `false` auf ein nie archiviertes Board ebenso.
- [ ] `GET /api/boards` ohne Parameter und `GET /api/boards?archiviert=false` liefern dieselbe Liste — die Voreinstellung ist die Standardliste.
- [ ] Die Reihenfolge beider Listen ist unverändert alphabetisch nach Name (`COLLATE NOCASE`), `BoardId` als Zweitschlüssel.
- [ ] Nach einem Neustart der WebApi auf derselben Datei ist der Archivstand unverändert; ein zweiter Lauf der Migration lässt Schema **und** Daten unberührt.
- [ ] Ein archiviertes Board bleibt über die API bedienbar: Spalten- und Kartenaufrufe darauf verhalten sich wie zuvor (Archivieren sperrt nichts).

### Archivierte in der Oberfläche

- [ ] Im Fuß der Board-Übersicht steht die Wahl „aktive" / „archivierte"; beim Öffnen der Seite ist „aktive" gewählt.
- [ ] „Archivieren" im ⋯-Menü lässt das Board aus der angezeigten Standardliste verschwinden, ohne dass die Seite neu geladen wird.
- [ ] Ein Klick auf „archivierte" zeigt die archivierten Boards; ihre Kacheln sind als archiviert erkennbar und tragen „zurückholen".
- [ ] „zurückholen" lässt das Board aus der archivierten Ansicht verschwinden; unter „aktive" steht es wieder.
- [ ] Nach einem Reload steht die Wahl wieder auf „aktive" — der Filter ist eine Ansicht, kein gespeicherter Zustand.
- [ ] Was über die API archiviert wurde, fehlt in der danach geöffneten Standardliste der Oberfläche und steht unter „archivierte".

### Zurückweisung und Fehlerpfade

- [ ] `PUT /api/boards/{boardId}` mit leerem oder nur aus Leerzeichen bestehendem `name` antwortet mit HTTP 400 und einem Befund, dessen `Code` `board-name-leer` ist und dessen `Kompensation` **diese** Route nennt, nicht `POST /api/boards`.
- [ ] Nach einer solchen Zurückweisung ist nichts geschrieben: das Board trägt weiterhin seinen alten Namen.
- [ ] `PUT /api/boards/{boardId}` und `PUT /api/boards/{boardId}/archivierung` auf eine unbekannte `boardId` antworten mit HTTP 404 **und einem Rumpf**: ein Befund mit nichtleerem `Code`, einer `Meldung`, welche die aufgerufene Nummer nennt, und einer ausführbaren `Kompensation`.
- [ ] Nach einer solchen Zurückweisung ist nichts geschrieben: die Tabelle `Boardarchivierung` trägt keine Zeile für die unbekannte Nummer.
- [ ] `GET /api/boards?archiviert=<unlesbarer Wert>` antwortet mit HTTP 400 **und einem Befund** samt Kompensation — keine Fehlerantwort ohne Rumpf und kein stilles Ausweichen auf die Standardliste.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Boards/` — neu `BoardUmbenennenAnfrage` (Rumpf des Umbenennens) und `Archivierung` (Rumpf des Archivierens und des Zurückholens); `Board` wächst um `IstArchiviert`. `BoardUebersicht` bleibt unverändert — die Liste ist gefiltert und sagt damit schon, was sie zeigt.
- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/005-boardarchivierung.sql` — neue, idempotente Migration.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Boards/BoardRepository.cs` (erweitert: `Lade` liest den Archivstand mit, `LadeAlle` filtert danach, je eine Schreibmethode für Name und Archivstand), `Source/KanbanC.BL/Interfaces/Boards/IBoardRepository.cs` (erweitert).
- **Prüfung:** `Source/KanbanC.BL/Operations/Boards/` — der Namensprüfer des Umbenennens; die Regel „Name nicht leer" wird mit `BoardAnlegenValidator` geteilt, nicht abgeschrieben.
- **Fachlogik:** `Source/KanbanC.BL/Integrations/Boards/BoardService.cs` (erweitert). Kein Validator für das Archivieren — ein Wahrheitswert hat keinen ungültigen Fall.
- **API:** `Source/KanbanC.WebApi/Endpunkte/BoardEndpunkte.cs` — `PUT /api/boards/{boardId}`, die Unterressource `archivierung` und der Abfrageparameter an `GET /api/boards`; `Nichtgefunden.Board` und `Zurueckweisungen.AlsFehlerantwort` werden unverändert benutzt.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/BoardApiKlient.cs` (erweitert), `Source/KanbanC.Blazor/Components/Boards/Boardkachel.razor` (+ `.razor.css`: ⋯-Menü, Namensfeld, „zurückholen", archivierte Darstellung), `Source/KanbanC.Blazor/Components/Pages/Boards.razor` (+ `.razor.css`: Filter im Fuß, Liste mit Archivstand laden).
- **Tests:** `Source/KanbanC.BL.Tests/Integrations/Boards/BoardServiceTests.cs` und `TestHelpers/TestBoardRepository.cs`, `Source/KanbanC.BL.Tests/Operations/Boards/`, `Source/KanbanC.Blazor.Tests/Services/BoardApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Boards/BoardRepositoryTests.cs`, `Api/BoardEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Source/KanbanC.PlaywrightTests/` (Seitenobjekt `BoardsSeite` und zwei neue Testklassen).
- **Unberührt:** `wwwroot/gestaltung.css` und `oberflaeche.css` — die Kachel bringt ihre Gestaltung in `Boardkachel.razor.css` mit, mit Werten aus dem Token-Sheet.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0001.dc.html`](../Dokumentation/Wireframes/D0001.dc.html) ist die Gestaltungsvorgabe; einschlägig sind **Zustand 5** (`Board pflegen · I0005`, Zeilen 621–674: Umbenennen in der Kachel, archivierte Ansicht mit „zurückholen") und aus **Zustand 1** das ⋯-Menü der Kachel (Zeilen 176–182) sowie die Filterwahl im Fuß der Übersicht (Zeilen 195–208). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`), die Dateien im Repository sind damit der einzige Stand; ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung** — aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Was es an Umfang klärt: der dritte Menüpunkt „Exportieren" gehört zu `I0038` und **nicht** hierher.

### Ablauf

1. **Übersicht öffnen**
   - 1.1 `Boards.razor` lädt mit dem Archivstand der gewählten Ansicht, voreingestellt „aktive"
   - 1.2 `BoardApiKlient.LadeAlleBoards(archivstand)` → `GET /api/boards` bzw. `GET /api/boards?archiviert=true`
2. **Umbenennen**
   - 2.1 Klick auf ⋯ öffnet das Menü der Kachel; „Umbenennen" ersetzt den Namen durch das Namensfeld
   - 2.2 „Speichern" → `BoardApiKlient.BenenneUm(boardId, new BoardUmbenennenAnfrage(name))` → `PUT /api/boards/{boardId}`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 2.3 `BoardService.BenenneBoardUm` prüft den Namen
     - 2.3.1 Name leer → `Ergebnis<Board>.Zurueckgewiesen` → HTTP 400 mit Befund
     - 2.3.2 Board unbekannt → Befund `Nichtgefunden.Board(boardId)` → HTTP 404 mit Befund
   - 2.4 Erfolg → die Kachel meldet den neuen Stand über `EventCallback` an `Boards.razor`, die Liste wird neu geladen
3. **Archivieren und Zurückholen**
   - 3.1 Menüpunkt „Archivieren" bzw. „zurückholen" → `BoardApiKlient.SchalteArchivierung(boardId, new Archivierung(gewuenschterStand))` → `PUT /api/boards/{boardId}/archivierung`
   - 3.2 `BoardService.SchalteArchivierung` reicht an das Repository durch — zu prüfen ist nur, ob es das Board gibt
   - 3.3 `BoardRepository.SetzeArchivierung` öffnet eine Transaktion und liest **in ihr** das Board
     - 3.3.1 Board unbekannt → `null`, kein Schreibzugriff (SQLite erzwingt die Fremdschlüssel nicht — `SqliteVerbindungsfabrik` setzt kein `PRAGMA foreign_keys`, die Prüfung muss die Abfrage selbst leisten)
     - 3.3.2 archivieren → `INSERT … ON CONFLICT (Board) DO NOTHING`; zurückholen → `DELETE FROM Boardarchivierung WHERE Board = @Board`
   - 3.4 Commit, danach das Board zurücklesen und liefern
   - 3.5 Erfolg → Liste neu geladen; das Board verschwindet aus der gerade gezeigten Ansicht
4. **Lesen**
   - 4.1 `BoardRepository.Lade` liest mit `LEFT JOIN Boardarchivierung`; fehlt die Zeile, ist `IstArchiviert` `false`
   - 4.2 `BoardRepository.LadeAlle` filtert über dieselbe Verknüpfung (`WHERE a.Board IS NULL` bzw. `IS NOT NULL`), die Sortierung bleibt unverändert

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `BoardEndpunkte` — `PUT /api/boards/{boardId}` ist die Route, die `B0112` bewusst für diesen Slice freigehalten hat (siehe `R00009`, „Verworfene Alternativen"). Die Archivierung kommt als Unterressource daneben, wie `kartenzahl`, `spalten/reihenfolge` und `karten/{karteId}/lage`.
- `Boardkachel.razor` — die Kachel kennt ihr Board und ist der einzige Ort, an dem Menü, Namensfeld und „zurückholen" sitzen; sie meldet Änderungen nach oben, statt selbst eine zweite Liste zu führen.
- `Boards.razor` — hält die gewählte Ansicht und ist die eine Quelle der geladenen Liste.
- `IBoardRepository` — die Schnittstelle wächst um drei Zugriffe und zieht `TestBoardRepository` und die Service-Tests nach.

**Klassen-Entwurf:**

- `BoardUmbenennenAnfrage` (Contract, DTO, immutable) — der Rumpf des Umbenennens. Nur der Name: Art und Termine gehören dem Anlegen.
  - `public record BoardUmbenennenAnfrage(string Name)`
- `Archivierung` (Contract, DTO, immutable) — der gewünschte Archivstand. Ein benanntes Feld statt eines nackten `bool`, damit ein Agent im JSON sieht, was er setzt — und damit dieselbe Route zurückholt.
  - `public record Archivierung(bool IstArchiviert)`
- `Board` (Contract, DTO, immutable) — wächst um ein Feld am Ende, damit die vorhandenen Aufrufstellen ihre Reihenfolge behalten.
  - `public record Board(long BoardId, string Name, BoardArt Art, DateOnly? Starttermin, DateOnly? Zieltermin, IReadOnlyList<Spalte> Spalten, bool ZeigtKartenzahl, bool IstArchiviert)`
- `BoardUmbenennenValidator` (Operation, pure Logik) — prüft den Namen und nennt in der Kompensation die Umbenennen-Route. Die Regel „Name nicht leer" wird mit `BoardAnlegenValidator` **geteilt**, nicht kopiert; wie die Teilung aussieht (gemeinsame Prüfmethode mit Route als Parameter oder ein eigener Prüfer `Boardname`), entscheidet der Entwickler beim Bauen.
  - `Pruefbefunde Pruefe(BoardUmbenennenAnfrage anfrage)`
- `IBoardRepository` / `BoardRepository` (Provider, Ressourcenzugriff) — schreibt in einer Transaktion und liest das Board zurück; `null` heißt „dieses Board gibt es nicht".
  - `Board? BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage)`
  - `Board? SetzeArchivierung(long boardId, Archivierung archivierung)`
  - `IReadOnlyList<BoardUebersicht> LadeAlle(Archivierung archivstand)` — löst das heutige parameterlose `LadeAlle` ab; ob der Archivstand als `Archivierung` oder als eigener kleiner Typ hereinkommt, entscheidet der Entwickler. Ein nacktes `bool` an dieser Stelle wäre ein Flag-Argument.
- `BoardService` (Integration) — verdrahtet.
  - `Ergebnis<Board> BenenneBoardUm(long boardId, BoardUmbenennenAnfrage anfrage)` — ein `Ergebnis` statt `null`, weil zwei Lagen zu unterscheiden sind: leerer Name (400) und unbekanntes Board (404)
  - `Board? SchalteArchivierung(long boardId, Archivierung archivierung)` — `null` heißt „Board unbekannt"; ob der Einheitlichkeit halber ein `Ergebnis<Board>` genommen wird (so notiert `B0130`), ändert an den HTTP-Antworten nichts und entscheidet der Entwickler
  - `IReadOnlyList<BoardUebersicht> LadeAlleBoards(Archivierung archivstand)`
- `BoardEndpunkte` (Integration, statisch) — zwei Routen mehr, eine geändert.
  - `routen.MapPut(Basisroute + "/{boardId:long}", BenenneBoardUm).WithName("BoardUmbenennen")`
  - `routen.MapPut(Basisroute + "/{boardId:long}/archivierung", SchalteArchivierung).WithName("ArchivierungSchalten")`
  - `LadeAlleBoards` nimmt den Abfrageparameter `archiviert` entgegen; `Zurueckweisungen.AlsFehlerantwort` trennt 400 und 404 am Code des Befunds
- `BoardApiKlient` (Integration, Blazor) — der HTTP-Weg der Oberfläche; 400 und 404 tragen beide eine `Zurueckweisung` und laufen denselben Weg.
  - `public Task<ApiErgebnis<Board>> BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage)`
  - `public Task<ApiErgebnis<Board>> SchalteArchivierung(long boardId, Archivierung archivierung)`
  - `public Task<IReadOnlyList<BoardUebersicht>> LadeAlleBoards(Archivierung archivstand)`
- **Migration** `005-boardarchivierung.sql` (Skript, idempotent) — die Zeile selbst ist die Aussage: vorhanden heißt archiviert.
  ```sql
  CREATE TABLE IF NOT EXISTS Boardarchivierung
  (
      Board INTEGER PRIMARY KEY REFERENCES Board (BoardId)
  );
  ```
  Der Spaltenname `Board` folgt der Fremdschlüsselregel des Projekts. Ein `ArchiviertAm` gibt es bewusst nicht, solange niemand danach fragt; es käme als zweite Migration nach.

### Änderungen an bestehenden Klassen

- `Board` (Contract) — ein Feld mehr. Betroffen sind **elf** Konstruktionsstellen: `BoardRepository` (2), `TestBoardRepository` (1), `BoardServiceTests` (7), `ApiErgebnisTests` (1). Nachgezählt am 2026-09-04 gegen `06d2fbc`.
- `BoardRepository` — `Lade` und `LiesBoardzeile` bekommen den zweiten `LEFT JOIN`; `LadeAlle` bekommt den Filter; `LegeAn` liefert `false` mit, ohne eine Zeile anzulegen.
- `IBoardRepository`, `BoardService`, `BoardEndpunkte`, `BoardApiKlient` — je die Methoden aus dem Grobentwurf; `LadeAlle` / `LadeAlleBoards` ändern ihre Signatur und ziehen ihre heutigen Aufrufstellen nach.
- `Boardkachel.razor` (+ `.razor.css`) — bekommt das ⋯-Menü, das Namensfeld, die Menüpunkte und die archivierte Darstellung. Das Menü bekommt `position: relative` und einen `z-index` **über** der Verweisfläche; `.board-verweis::after { inset: 0 }` (`Boardkachel.razor.css:19-26`) bleibt unangetastet, damit die ganze Kachel der Weg ins Board bleibt.
- `Boards.razor` (+ `.razor.css`) — hält die gewählte Ansicht, zeichnet den Filter im Fuß und lädt die Liste damit; nimmt die Meldung der Kachel über `EventCallback` entgegen.
- `TestBoardRepository` — die neuen Methoden samt Beobachterflags, damit ein Test beweisen kann, dass ein unbekanntes Board **nicht** schreibt.
- `BoardsSeite` (Seitenobjekt der E2E-Tests) — Locator für ⋯-Menü, Menüpunkte, Namensfeld, Speichern, Filter und archivierte Kachel.
- `FehlervertragTests` — nimmt die zwei neuen Routen auf; `GET /api/boards` verlässt die Liste `RoutenOhneFehlerantwort` (`FehlervertragTests.cs:13-19`), weil der Abfrageparameter dort erstmals eine Fehlerantwort möglich macht.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `BoardUmbenennenValidator` — leerer Name, nur Leerzeichen, gültiger Name; und der Nachweis, dass die Kompensation die Umbenennen-Route nennt, nicht `POST /api/boards`.
- `BoardService.BenenneBoardUm` / `SchalteArchivierung` — gegen `TestBoardRepository`: gültiger Name schreibt und liefert das Board; leerer Name liefert eine Zurückweisung **ohne** Schreibzugriff (Beobachterflag); unbekanntes Board liefert den Nichtgefunden-Befund bzw. `null`, ebenfalls ohne Schreibzugriff.
- `BoardApiKlient.BenenneUm` / `SchalteArchivierung` / `LadeAlleBoards` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Board, 400 und 404 liefern die Zurückweisung mit ihren Befunden; geprüft wird zusätzlich, dass Methode, Adresse und Abfrageparameter des abgesetzten Aufrufs stimmen. Diese Pfade sind über den Browser nicht auslösbar.

**Integration:**
- `BoardRepository` gegen eine `TemporaereDatenbank` — Umbenennen ändert nur den Namen; unbekanntes Board liefert `null`; Archivieren, zweites Archivieren (keine zweite Zeile), Zurückholen, Zurückholen eines nie archivierten Boards; `LadeAlle` liefert je Archivstand die richtige Menge in unveränderter Reihenfolge; `Lade` liefert ein archiviertes Board vollständig mit `IstArchiviert`.
- `Migrationslaeufer` — zweiter Lauf auf einer Datei mit archiviertem Board: Schema und Archivstand unverändert.
- `BoardEndpunkte` über `TestWebApi` — 200 mit dem Board, 400 mit Befund bei leerem Namen, 404 mit Rumpf bei unbekannter Nummer, die gefilterten Listen, `GET /api/boards/{boardId}` auf ein archiviertes Board, unlesbarer Wert des Abfrageparameters.
- `FehlervertragTests` — die beiden neuen Routen und die Liste mit Parameter werden in die Prüfung aufgenommen, die für **jede** Fehlerantwort `Code`, `Meldung` und `Kompensation` nichtleer verlangt.
- `WebApiNeustartTests` — neuer Name und Archivstand überstehen den Neustart.

**E2E:** Menü öffnen, umbenennen, Reload zeigt den neuen Namen (US-1); leerer Name zeigt die Meldung, der alte Name bleibt (US-2); archivieren, das Board fehlt in der Standardliste (US-4); Filter „archivierte" zeigt es, „zurückholen" bringt es zurück (US-5); über die API archiviert, in der Oberfläche gesehen (US-7). Dazu laufen alle E2E-Tests aus `R00001`–`R00009` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `R00001` (Board anlegen, erledigt — `I0001`, grün). Das ist die einzige Vorbedingung der WBS-Spalte `Braucht` von `I0005`; der Slice ist **frei**.
- Setzt außerdem auf: `R00003` (Boards auflisten und öffnen — die Liste und die Kachel), `R00005` (Kacheln und Bänder statt Tabelle), `R00007` (Fehlervertrag — `Nichtgefunden.Board` und `Zurueckweisungen.AlsFehlerantwort` werden unverändert benutzt, es entsteht kein neuer Fehlercode für unbekannte Boards), `R00009` (die für diesen Slice freigehaltene Route `PUT /api/boards/{boardId}` und das Muster der Unterressource).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0005`, `F0025` oder `F0026` in seiner Spalte `Braucht` (geprüft am 2026-09-04 über `Dokumentation/Planung/kanbanc.md`).
- Reihenfolge innerhalb der Anforderung: `F0025` (Umbenennen) vor `F0026` (Archivieren) — `F0026` nennt `F0025` in `Braucht`, weil das ⋯-Menü der Kachel dort entsteht und hier nur einen Punkt dazubekommt.

## Umfang

```
Board umbenennen und archivieren (I0005) = 20 Bubbles: 19 Standard (25,2h), 1 unklar (2–4h).
Rest: 25,2h klar + 2–4h unklar · 8 von 20 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 20 Bubbles gruen (0 %) · 0 laufen · 20 offen
```

`I0005` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0025` Board umbenennen | B0118–B0125 (8) | 11,2h klar | `I0001` |
| `F0026` Board archivieren und zurückholen | B0126–B0137 (12) | 14h klar + 2–4h unklar | `F0025` |

Belegt sind die acht Prüf-, Datenzugriffs- und Verdrahtungs-Bubbles (`B0118`–`B0120`, `B0126`–`B0130`; Vergleichswerte `B0002`, `B0004`, `B0016`, `B0027`, `B0028`, `B0029` in `Schaetzungen/_ist-zeiten.md`); die Endpunkt-, Klienten-, UI- und E2E-Bubbles tragen Richtwerte. Die eine unklare Bubble ist `B0137`: nicht die Technik ist offen, sondern wie viel am Ausbau von `BoardsSeite` um Filter und archivierte Kachel hängt. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Ein Vermerk, der nicht als Beifang durchgehen soll: die WBS-Notiz zu `I0005` hält fest, dass die 2h-Richtwerte für Endpunkt-, Klienten-, UI- und E2E-Bubbles über den gemessenen Werten liegen (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde nicht geändert, weil das die Zählung des ganzen Baums verschöbe; die Frage gehört einmal entschieden, nicht je Slice (`Dokumentation/Planung/kanbanc.md:245`).

## Offene Fragen

- ~~Eigene Tabelle oder Spalte an `Board` / `Boardeinstellung`?~~ — entschieden am 2026-09-04: **eigene Tabelle `Boardarchivierung`**, Schlüssel `Board`, Zeile vorhanden = archiviert. `ALTER TABLE … ADD COLUMN` ist in SQLite nicht idempotent und scheitert beim zweiten Lauf des `Migrationslaeufer` (`Migrationslaeufer.cs:16-23`), und eine bestehende `CREATE TABLE IF NOT EXISTS Boardeinstellung` wächst nicht nachträglich um eine Spalte. Kein `ArchiviertAm` — niemand hat bisher nach dem Zeitpunkt gefragt.
- ~~Zweite Route `/api/boards/archiv` oder Filter an `GET /api/boards`?~~ — entschieden am 2026-09-04: **Abfrageparameter `archiviert`**, Voreinstellung `false`. Ein Agent kennt eine Adresse für „welche Boards gibt es"; bestehende Aufrufe bekommen unverändert die aktiven Boards.
- ~~Trägt `BoardUebersicht` den Archivstand?~~ — entschieden am 2026-09-04: **nein**. Die Liste ist gefiltert und sagt schon, was sie zeigt; nur `Board` trägt den Stand, damit `GET /api/boards/{boardId}` einem Agenten die Lage nennt. Wird die Übersicht später gemischt ausgeliefert, muss das DTO nachziehen.
- ~~Was passiert bei einem unlesbaren Wert des Abfrageparameters?~~ — entschieden am 2026-09-04: **400 mit Befund und Kompensation**. Ohne Zutun antwortet ASP.NET auf `?archiviert=vielleicht` mit einer Fehlerantwort ohne unseren Befund — das bricht die Zusage aus `R00007`. Der Guard ist klein; er geht minimal über das Fertig-Kriterium hinaus und steht deshalb hier und als Kriterium, nicht stillschweigend im Code. Beleg, dass die Route bisher als fehlerfrei geführt wird: `Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs:13-19`.
- ~~Leerer Name auf unbekannter Nummer — 400 oder 404?~~ — entschieden am 2026-09-04: **400**, die Prüfung der Anfrage geht dem Nachschlagen des Boards voraus (wie in `BoardService.LegeBoardAn`). Der Agent erfährt zuerst, dass sein Rumpf nicht taugt; nach der Korrektur nennt ihm der zweite Aufruf die unbekannte Nummer.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start der WebApi mit; bestehende Boards bekommen keine Zeile und gelten damit als aktiv.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser sind zwei Sackgassen der heutigen Board-Übersicht: ein Name, der beim Anlegen fällt und danach für immer steht, und eine Liste, die nur wachsen kann. Wenn ein Board umbenennbar wird, hört der erste Wurf auf, eine Festlegung zu sein — und wenn Archivieren möglich ist, trennt sich die Liste dessen, woran gearbeitet wird, von der Menge dessen, was je angelegt wurde, ohne dass Daten verschwinden. Die Kausalkette der Archivierung führt weiter als bis zur Übersicht: weil das Board erhalten bleibt und unter seiner Nummer vollständig abrufbar ist, finden die späteren Auswertungen (`D0009`: Soll-Ist, Burndown, Zeitsummen) ihren Bestand unversehrt vor — ein Löschen hätte die Übersicht genauso aufgeräumt und dabei die Geschichte der Zeiterfassung mitgenommen. Der Hebel sitzt genau hier und nicht später, weil jede weitere Anforderung die Board-Liste als „alles, was es gibt" voraussetzt; je mehr darauf aufbaut, desto teurer wird die Unterscheidung zwischen aktiv und abgelegt. Und dass beides über die API genauso geht wie über die Kachel, ist keine Zugabe: ein Agent, der ein Vorhaben abschließt, muss dessen Board selbst weglegen können, sonst räumt am Ende doch wieder ein Mensch hinterher.

## Missing-Docs

- **Bindung optionaler Abfrageparameter in Minimal APIs und ihr Fehlerverhalten.** Ob ein nicht lesbarer `bool`-Wert vor dem Handler abgewiesen wird (und damit ein eigener Guard nur mit `string?`-Bindung möglich ist) oder ob der Handler noch die Kontrolle behält, ist im Bestand nirgends belegt — bisher hat kein Endpunkt einen Abfrageparameter. Vor dem Bauen mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`).
- **Schließverhalten von Menüs ohne JS-Interop in Blazor Server.** Das ⋯-Menü schließt laut Vorplanung nur über einen zweiten Klick auf ⋯, wie das Anlegeformular. Ob ein „Klick daneben schließt" ohne JS-Interop und ohne globalen Klick-Handler sauber zu haben ist, steht nirgends; davon hängt ab, ob die Bedienung später nachgebessert werden muss.

## Notizen

### Verworfene Alternativen

- **Board löschen statt archivieren.** Weniger Code, kein Filter, keine Migration. Verworfen: Karten, Zeiteinträge und die spätere Auswertung hingen daran; das Fertig-Kriterium verlangt ausdrücklich „bleibt abrufbar".
- **Spalte `IstArchiviert` an `Board`.** Ein Feld weniger im Modell, kein zweiter JOIN. Verworfen: nicht idempotent migrierbar, siehe „Offene Fragen".
- **Eine Spalte an `Boardeinstellung`.** Naheliegend, weil die Tabelle schon existiert. Verworfen: eine bestehende `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich um eine Spalte; und Archivierung ist ein Zustand des Boards, keine Anzeigeeinstellung.
- **Zweite Route `GET /api/boards/archiv`.** Klar getrennt, ohne Parameter. Verworfen: zwei Adressen für dieselbe Frage; ein Agent müsste beide kennen.
- **Umbenennen auf einem eigenen Schirm oder in einem Dialogfenster.** Verworfen: das Artboard antwortet mit der Kachel, wie `D0002` es für die Kontributorzeile tut; ein zweiter Schirm für ein Textfeld ist ein Weg zu viel.
- **Voller `PUT` mit dem ganzen Board** (Name, Art, Termine). Eine Route, die alles ändert. Verworfen: Art und Termine gehören dem Anlegen; ein Agent, der nur den Namen ändern will, müsste erst alles lesen und riskierte, dabei etwas zu überschreiben.
- **Eigener Fehlercode für den leeren Namen beim Umbenennen** (z. B. `board-name-leer-umbenennen`). Verworfen: es ist dieselbe verletzte Regel; unterschieden wird in der Kompensationsaktion, die die jeweilige Route nennt — das ist genau das, was ein Agent zum Weiterkommen braucht.
- **Getrennte Routen für Archivieren und Zurückholen** (`POST …/archivierung`, `DELETE …/archivierung`). Verworfen: der gewünschte Zustand im Rumpf macht denselben Aufruf idempotent und spart die zweite Route; das Muster ist dasselbe wie bei `kartenzahl`.

### Bewusst out of scope

- **Export im ⋯-Menü** (`I0038`) — der dritte Menüpunkt des Artboards gehört zu einem anderen Slice und wird hier nicht gebaut.
- **Zeitpunkt der Archivierung** (`ArchiviertAm`) und eine Sortierung danach.
- **Sammelbedienung** („mehrere Boards archivieren", Auswahl über Kacheln hinweg).
- **Live-Übertragung an andere offene Sichten.** Archiviert ein Betrachter, sieht ein zweiter es erst beim nächsten Laden — das ist `I0028`.
- **Eindeutige Boardnamen.** Zwei Boards dürfen gleich heißen; das war schon beim Anlegen so und wird hier nicht nachträglich verschärft.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Neben den fünf Entscheidungen unter „Offene Fragen" stehen vier weitere Annahmen mit Beleg:

1. **Ein archiviertes Board wird auf seiner eigenen Seite nicht gekennzeichnet und nicht gesperrt** — Karten und Spalten bleiben unverändert bedienbar. Das Fertig-Kriterium verlangt nur das Verschwinden aus der Standardliste und die Abrufbarkeit; eine Sperre wäre eine eigene Entscheidung (`Dokumentation/Planung/kanbanc.md:244`).
2. **Der Filter ist eine Ansicht, kein gespeicherter Zustand** — nach einem Reload steht er wieder auf „aktive" (`Dokumentation/Planung/kanbanc.md:105`, `B0135`). Damit ist er anders als die Kartenzahl aus `R00009` bewusst **kein** Board-Zustand: er sagt nichts über das Board, sondern über den Blick darauf.
3. **Das ⋯-Menü schließt nur über einen zweiten Klick auf ⋯**, nicht über einen Klick daneben — wie das Anlegeformular in `Boards.razor` (`Dokumentation/Planung/kanbanc.md:92`, `B0123`).
4. **Der neue Name überschreibt ohne Warnung**, auch wenn ein anderes Board schon so heißt. Der Bestand kennt keine Eindeutigkeit für Boardnamen (`001-boards-und-spalten.sql`); nur Spaltenbezeichnungen sind je Board eindeutig (`002-spalte-bezeichnung-eindeutig.sql`, `R00004`).

Wer eine dieser Annahmen anders will, ändert sie vor dem Bauen — nach `B0126` kostet die Tabellenfrage eine zweite Migration.
