---
id: R00003
status: Erledigt
datum: 2026-08-29
---

# R00003: Boards auflisten und öffnen

## Beschreibung

Die Board-Liste zeigt alle Boards mit Name und Art in einer festen, nachvollziehbaren Reihenfolge. Von dort lässt sich ein Board **öffnen**: es bekommt eine eigene Adresse (`/boards/3`) und erscheint als betriebsbereites Board — Kopfdaten und die Spalten als nebeneinanderliegende Bahnen, in die später Karten wandern. Eine unbekannte Board-Nummer führt zu einer lesbaren Meldung, nicht zu einer kaputten Seite.

Zahlt ein auf: [Vision](R00000-vision.md) — „Visuelle Haltung an Kanbanflow orientiert" und „Eine API auf Augenhöhe mit der Oberfläche".

Der Slice erbt aus `R00001` eine Liste und einen Detail-Abruf, die beide schon stehen. Neu ist der Schritt vom *Anzeigen* zum *Öffnen*: das Board wird ein eigener Ort mit eigener Adresse statt eines eingeblendeten Panels auf der Listenseite. Damit entsteht die Seite, in die `I0010 Board ansehen` später die Karten setzt, ohne sie aus der Liste herausoperieren zu müssen.

## Geschäftlicher Nutzen

Ohne eigene Adresse ist ein Board nicht verweisbar: weder ein Mensch kann einem anderen den Link schicken, noch bleibt nach einem Reload sichtbar, woran gerade gearbeitet wird. Vor allem aber liegt die gesamte Board-Ansicht heute in der Listenseite — jeder folgende Slice des Dialogs „Board bedienen" (`I0010` bis `I0014`) würde dort weiterwachsen, bis Liste, Anlegeformular und Arbeitsfläche in einer Datei stecken. Der Slice zieht die Grenze jetzt, solange sie ein Umzug von dreißig Zeilen ist.

Zweitens wird die Liste bei wachsender Board-Zahl erst durch eine verlässliche Reihenfolge brauchbar. Heute sortiert sie nach Anlagereihenfolge — nicht als Entscheidung, sondern als Nebenwirkung der Abfrage, ungetestet und für einen Suchenden nutzlos.

## Funktionale Anforderungen

- Die Board-Liste zeigt jedes Board mit Name und Art.
- Die Liste ist alphabetisch nach Name sortiert, Groß- und Kleinschreibung ohne Einfluss; bei gleichem Namen entscheidet die kleinere `BoardId`.
- Jeder Listeneintrag führt zum Öffnen des Boards.
- Ein geöffnetes Board hat die eigene Adresse `/boards/{boardId}` und ist darüber direkt aufrufbar.
- Das geöffnete Board zeigt Name, Art, Start- und Zieltermin sowie seine Spalten in ihrer Position-Reihenfolge, nebeneinander als Bahnen.
- Die Abschlussspalte ist als solche erkennbar.
- Eine nicht vergebene Board-Nummer führt zu einer lesbaren Meldung mit Weg zurück zur Liste.
- Vom geöffneten Board führt ein Weg zurück zur Liste.
- Jede dieser Fähigkeiten ist über die API erreichbar (`GET /api/boards`, `GET /api/boards/{boardId}`).

## Nicht-funktionale Anforderungen

- **Architektur:** `KanbanC.Blazor` erhält weiterhin keine Projektreferenz auf `KanbanC.BL`. Auch die neue Seite kommt ausschließlich über `BoardApiKlient` an die Daten.
- **Benutzerfreundlichkeit:** Die Spaltenbahnen tragen die Optik, in die Karten ohne Umbau passen — feste Bahnbreite, Kopfzeile je Spalte, waagerechtes Scrollen statt Umbruch (Maßstab Kanbanflow, Leitplanke der Vision).
- **Sortierung:** Die Reihenfolge ist deterministisch — zwei Aufrufe derselben Liste liefern dieselbe Folge.
- **Sicherheit:** Full-Trust, unverändert. Eine Board-Nummer in der Adresse ist kein Geheimnis.

## Akzeptanzkriterien

### Liste
- [x] `GET /api/boards` liefert die Boards alphabetisch nach Name sortiert.
- [x] Die Sortierung ignoriert Groß- und Kleinschreibung: Boards `Wartung`, `beschaffung`, `KanbanC` erscheinen in der Folge `beschaffung`, `KanbanC`, `Wartung` — unabhängig davon, in welcher Reihenfolge sie angelegt wurden.
- [x] Bei gleichem Namen steht das Board mit der kleineren `BoardId` vorn (zwei Boards `Wartung` mit `BoardId` 5 und 2 → Folge 2, 5).
- [x] Die Board-Liste der Oberfläche zeigt jedes Board mit Name und Art in derselben Reihenfolge wie die API.
- [x] Jeder Listeneintrag trägt einen Verweis auf `/boards/{boardId}` des jeweiligen Boards.

### Board öffnen
- [x] Der Aufruf von `/boards/3` zeigt das Board mit der `BoardId` 3, ohne dass zuvor die Liste besucht wurde.
- [x] Das geöffnete Board zeigt Name, Art, Starttermin und Zieltermin; fehlende Termine erscheinen als `—`.
- [x] Das geöffnete Board zeigt genau die Spalten des Boards in der Reihenfolge ihrer Position — bei einem neu angelegten Board `Zu erledigen`, `In Arbeit`, `Erledigt`.
- [x] Die Spalten stehen nebeneinander als Bahnen, nicht als Aufzählung untereinander.
- [x] Die Abschlussspalte ist in ihrer Kopfzeile als solche markiert und nennt ihre Anzeigegrenze (`Erledigt`, Abschlussspalte, Anzeigegrenze 20).
- [x] Ein Klick auf einen Listeneintrag öffnet dieses Board; die Adresszeile zeigt danach `/boards/{boardId}`.
- [x] Ein Reload des geöffneten Boards zeigt dasselbe Board erneut.
- [x] Vom geöffneten Board führt ein Verweis zurück zur Board-Liste.

### Unbekanntes Board
- [x] Der Aufruf von `/boards/999` bei nicht vergebener Nummer 999 zeigt eine lesbare Meldung, die die Nummer nennt; die Seite stürzt nicht ab.
- [x] Diese Meldung enthält einen Verweis zurück zur Board-Liste.
- [x] Ist die WebApi beim Öffnen eines Boards nicht erreichbar, erscheint eine lesbare Meldung statt einer Ausnahmeseite.

### Abgrenzung
- [x] Das geöffnete Board zeigt keine Karten und bietet kein Anlegen von Karten an (`I0011`).
- [x] Das geöffnete Board bietet kein Umbenennen, Anlegen oder Entfernen von Spalten an (`I0003`).

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Pages/` — neue Seite `Board.razor` neben der bestehenden `Boards.razor`; das Detail-Panel wandert aus der Liste in die neue Seite. Bahnen-Layout in `Source/KanbanC.Blazor/wwwroot/app.css` bzw. als isolierte `Board.razor.css`.
- **API:** unverändert — `Source/KanbanC.WebApi/Endpunkte/BoardEndpunkte.cs` trägt die benötigten Routen bereits.
- **Fachlogik:** `Source/KanbanC.BL/Persistenz/Boards/BoardRepository.cs` — die `ORDER BY`-Klausel von `LadeAlle`.
- **Verträge:** unverändert.
- **Tests:** `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Boards/` (Sortierung), `Source/KanbanC.WebApi.IntegrationTests/Api/` (Sortierung über den Endpunkt), `Source/KanbanC.PlaywrightTests/Tests/` und `PageObjects/` (neues Seitenobjekt `BoardSeite`, Anpassung von `BoardsSeite`).

## Technische Überlegungen

### Ablauf

1. **Boards auflisten** (`GET /api/boards`)
   - 1.1 `BoardRepository.LadeAlle()` sortiert in SQL: `ORDER BY Name COLLATE NOCASE, BoardId`
   - 1.2 Die Oberfläche übernimmt die Reihenfolge der API unverändert — keine zweite Sortierung im Client
2. **Board öffnen** (Oberfläche)
   - 2.1 Listeneintrag ist ein `NavLink` auf `/boards/{boardId}`
   - 2.2 `Board.razor` liest den Routenparameter `BoardId`
   - 2.3 `BoardApiKlient.LadeBoard(boardId)`
     - 2.3.1 Board vorhanden → Kopfdaten und Spaltenbahnen rendern
     - 2.3.2 `null` (HTTP 404 der API) → Meldung „Ein Board mit der Nummer {boardId} gibt es nicht." plus Verweis auf `/boards`
     - 2.3.3 `HttpRequestException` → Meldung „Die WebApi ist nicht erreichbar." (dieselbe Formulierung wie in `Boards.razor`)
3. **Zurück zur Liste** — Verweis auf `/boards` im Kopf der Board-Seite

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** Die neue Route `/boards/{BoardId:long}` im Blazor-Router — ab hier existiert ein Board als eigener Ort in der Anwendung. Der zweite Eingriff ist die `ORDER BY`-Klausel in `BoardRepository.LadeAlle`; sie legt eine bisher unbestimmte Reihenfolge fest.

**KanbanC.Blazor**
- `Components/Pages/Board.razor` (UI-Seite, Route `/boards/{BoardId:long}`) — lädt ein Board über den `BoardApiKlient` und stellt es dar: Kopfzeile mit Name, Art und Terminen, darunter die Spalten als Bahnen. Drei Zustände: geladen, unbekannt, WebApi nicht erreichbar.
  - Parameter: `public long BoardId { get; set; }`
- `Components/Pages/Boards.razor` (bestehend) — behält Liste und Anlegeformular; das Detail-Panel samt `ZeigeSpalten` entfällt, die Spalte „Spalten anzeigen" wird zum Verweis auf das Board.

**KanbanC.BL**
- `BoardRepository.LadeAlle()` — die Abfrage bekommt `ORDER BY Name COLLATE NOCASE, BoardId`. Formatierung nach Skill `sql-stil`; die Sortierung gehört in die Abfrage, nicht hinter sie.

**KanbanC.PlaywrightTests**
- `PageObjects/BoardSeite.cs` (Seitenobjekt) — die geöffnete Board-Seite: direkter Aufruf über die Adresse, Kopfdaten, Spaltenbahnen, Meldung bei unbekannter Nummer.
  - `Task Oeffne(long boardId)`, `ILocator Spaltenbahnen`, `ILocator MeldungUnbekanntesBoard`, `ILocator Kopfzeile`
- `PageObjects/BoardsSeite.cs` — `ZeigeSpalten` wird zu `OeffneBoard(long boardId)` und navigiert; die Spalten-Locator wandern in `BoardSeite`.

Kein neues DTO, kein neuer Endpunkt, keine Migration. Der Slice ist überwiegend Oberfläche und eine Zeile SQL.

### Änderungen an bestehenden Klassen

- `Source/KanbanC.BL/Persistenz/Boards/BoardRepository.cs` — `LadeAlle`: `ORDER BY BoardId` → `ORDER BY Name COLLATE NOCASE, BoardId`.
- `Source/KanbanC.Blazor/Components/Pages/Boards.razor` — Detail-Panel (`#board-details`, `#spalten-liste`, `#details-starttermin`, `#details-zieltermin`), das Feld `_gewaehltesBoard` und die Methode `ZeigeSpalten` entfallen; der Button „Spalten anzeigen" wird ein Verweis auf `/boards/{boardId}`.
- `Source/KanbanC.PlaywrightTests/PageObjects/BoardsSeite.cs` — `ZeigeSpalten`, `Spalten`, `DetailsStarttermin`, `DetailsZieltermin` entfallen bzw. wandern in `BoardSeite`.
- `Source/KanbanC.PlaywrightTests/Tests/BoardAnlegenE2ETests.cs` — zwei Tests (Zeilen 43 und 59) prüfen Standardspalten und Termine über das entfallende Panel. Sie prüfen Akzeptanzkriterien von `R00001` und müssen erhalten bleiben: sie ziehen auf `BoardSeite` um, nicht auf die Abschussliste.

**Wächter:** Nach dem Umzug muss die Testsuite von `R00001` unverändert grün sein. Bleibt ein Kriterium aus `R00001` ohne Test, ist der Slice nicht fertig.

## Tests

Nach Skill `test-pyramide`: alle drei Ebenen, die Given/When/Then-Szenarien der User Story werden E2E-Tests.

**Kandidaten für Unit-Tests (pure Logik nach IOSP):**
- Keine. Der Slice fügt keine Entscheidungslogik hinzu — die Sortierung liegt in SQL, die Darstellung in Razor. Eine Sortier-Operation in C# zu bauen, nur damit ein Unit-Test existiert, wäre eine Kopie der Datenbankarbeit an zweiter Stelle (C16).

**Integration:**
- `BoardRepository.LadeAlle` gegen eine echte SQLite-Datei: drei Boards in gemischter Groß-/Kleinschreibung anlegen, Reihenfolge prüfen; zwei gleichnamige Boards, Zweitschlüssel `BoardId` prüfen.
- `GET /api/boards` über `WebApplicationFactory`: die Sortierung erreicht die Antwort und wird nicht unterwegs verloren.
- `GET /api/boards/{boardId}` mit unbekannter Nummer liefert 404 — besteht bereits aus `R00001`, wird nicht dupliziert.

**E2E:**
- Board über einen Listeneintrag öffnen; Adresszeile zeigt `/boards/{boardId}`, Kopfzeile nennt Name und Art.
- Board direkt über die Adresse aufrufen, ohne die Liste besucht zu haben.
- Reload des geöffneten Boards zeigt dasselbe Board.
- `/boards/999` bei nicht vergebener Nummer zeigt die Meldung und den Weg zur Liste.
- Board mit drei Standardspalten öffnen: drei Bahnen in Reihenfolge, `Erledigt` als Abschlussspalte mit Anzeigegrenze 20 (umgezogen aus `BoardAnlegenE2ETests`).
- WebApi angehalten, dann `/boards/1` aufrufen: lesbare Meldung statt Ausnahmeseite (Muster aus `WebApiAusfallE2ETests`).
- Liste mit drei Boards unterschiedlicher Schreibweise: die Zeilen stehen alphabetisch.

Anwendung auf freien Ports (Skill `freier-port`), Prozesse danach stoppen.

## Abhängigkeiten

- Abhängig von: `I0001` Board anlegen (`R00001`, Status `gruen`) — liefert Liste, Detail-Abruf und die Standardspalten, auf die dieser Slice aufsetzt.
- Blockiert: `I0010` Board ansehen führt `I0003` als Vorbedingung, nicht `I0002`; formal blockiert dieser Slice in der WBS keinen anderen. Faktisch entsteht hier die Seite, auf der `I0010` die Karten zeigt.

## Umfang

`I0002` ist bis zur Ebene Interaction geplant, nicht bis Bubble — es gibt keine Bubbles und damit keine Zählung. `/planung verfeinern I0002 --bis Bubble` liefert sie. Eine Schätzung wird hier bewusst nicht danebengeschrieben.

## Offene Fragen

Keine. Drei Entscheidungen wurden vor dem Schreiben getroffen und stehen unter „Notizen".

## Manuelle Vorbereitungstätigkeiten

Keine.

## Manuelle Nachbereitungstätigkeiten

Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass ein Board heute kein Ort ist, sondern ein Zustand der Listenseite: `R00001` blendet die Spalten als Panel unter der Tabelle ein, weil der Slice nur beweisen musste, dass die Kette von der Oberfläche bis in die Datenhaltung trägt. Das Zielbild verlangt eine Arbeitsfläche in der Haltung von Kanbanflow, auf der später Karten liegen, bewegt und kommentiert werden. Wenn wir dem Board jetzt eine eigene Route und eine eigene Komponente geben, dann bekommt erstens jedes Board eine teilbare, reload-feste Adresse, und zweitens haben `I0010` bis `I0014` einen leeren Rahmen, in den sie hineinbauen, statt die Listenseite immer weiter aufzuladen — die Trennung kostet heute einen Umzug von dreißig Zeilen und in fünf Slices ein Refactoring quer durch die Oberfläche. Gerade diese Änderung ist der Hebel und nicht `I0003 Spalten gestalten`, weil Gestalten eine Fläche voraussetzt, auf der man sieht, was man gestaltet; und nicht erst `I0010`, weil dort die Karten dazukommen und Umzug plus Neubau in einem Schritt zwei Fehlerquellen zu einer verrührt.

## Missing-Docs

- Routenparameter in Blazor mit Typ-Constraint (`{BoardId:long}`) und das Verhalten bei nicht passender Eingabe (`/boards/abc`) — ob der Router dann die `NotFoundPage` zieht oder die Seite mit Standardwert rendert, ist für das Kriterium „stürzt nicht ab" relevant und wurde hier nicht belegt.
- `COLLATE NOCASE` in SQLite: die Klausel wirkt nur auf ASCII-Zeichen; Umlaute werden nicht sprachrichtig einsortiert. Belegstelle für die Grenze fehlt.

## Notizen

### Getroffene Entscheidungen

- **Eigene Route `/boards/{boardId}`** statt Panel oder Query-Parameter. Ein Board ist ein Ort, kein Zustand einer Liste — teilbar, reload-fest, und die Heimat für `I0010`.
- **Das geöffnete Board zeigt das betriebsbereite Layout ohne Karten**: Spalten als Bahnen, wie sie später aussehen, nur leer. Ein Board zu öffnen heißt, es benutzbar vor sich zu haben — nicht seine Spalten aufzuzählen.
- **Sortierung alphabetisch nach Name**, Groß-/Kleinschreibung ohne Einfluss, `BoardId` als Zweitschlüssel. Wer ein Board sucht, sucht am Namen; der Zweitschlüssel macht die Folge bei Namensgleichheit deterministisch (`R00001`: Namen sind nicht eindeutig).

### Verworfene Alternativen

- *Detail-Panel auf der Listenseite ausbauen* — die billigere Variante, aber ohne Deep-Link und ohne reload-festen Zustand; `I0010` müsste die Board-Ansicht später doch herauslösen, dann mit Karten daran.
- *Query-Parameter `/boards?board=3`* — reload-fest und teilbar, aber Liste und Board teilen sich Seitentitel, Fokus-Ziel und Ladelogik; die Vermischung erbt jeder folgende Slice.
- *Nur Kopfdaten ohne Spalten* — strikt am Wortlaut „mit Name und Art", zeigte aber weniger als das heutige Panel und wäre ein Rückschritt gegen `R00001`.
- *Nach `BoardId` sortieren wie bisher* — Anlagereihenfolge ist für einen Suchenden keine Ordnung; als Entscheidung wäre sie nur dann vertretbar, wenn die Nummer der geläufige Bezeichner wäre, und das ist der Name.
- *Nach Art gruppiert* — lädt das Etikett „Art" mit einer Bedeutung auf, die `R00001` ihm ausdrücklich nicht gegeben hat („Etikett ohne Verhalten").

### Out of scope

- Karten jeder Art auf dem geöffneten Board (`I0011`, `I0012`).
- Spalten anlegen, umbenennen, umsortieren, entfernen (`I0003`).
- Kartenzahl in der Spaltenkopfzeile (`I0004`).
- Board umbenennen und archivieren (`I0005`); archivierte Boards fehlen deshalb noch nicht in der Liste — es gibt sie nicht.
- Suchen und Filtern in der Board-Liste; kein Slice der WBS verlangt es.
- **Sprachrichtige Sortierung von Umlauten.** `COLLATE NOCASE` sortiert `Ärger` hinter `Zwischenstand`. Bewusst hingenommen: die Alternative wäre Sortieren in C# nach dem Laden — also eine zweite Sortierstelle neben der Datenbank, für einen Effekt, den bei der erwarteten Board-Zahl niemand bemerkt. Wird es je stören, ist es eine Zeile in einem eigenen Slice.
