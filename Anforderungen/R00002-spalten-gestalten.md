---
id: R00002
status: In Arbeit
datum: 2026-08-29
---

# R00002: Spalten gestalten

## Beschreibung

Die Spalten eines Boards lassen sich frei gestalten: anlegen, umbenennen, umsortieren und entfernen. Jede Spalte ist außerdem als Abschlussspalte mit einer Anzeigegrenze markierbar — die Oberfläche lädt dann nur die N neuesten Karten dieser Spalte und den Rest erst auf Anforderung. Jede dieser Fähigkeiten steht über die API und über die Oberfläche zur Verfügung.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche": nicht nur Karten lesen und schreiben, sondern Boards gestalten — Spalten, Struktur.

Damit wird der Spaltenbestand, den `R00001` als feste Dreier-Vorlage anlegt, zum ersten Mal veränderlich.

## Geschäftlicher Nutzen

Die Standardspalten aus `R00001` passen auf kein zweites Board. Ohne diesen Slice ist jedes Board dieselbe Schablone, und drei weitere Slices hängen daran: `I0010` (Board ansehen) braucht eine gestaltbare Spaltenstruktur als Grundlage, `I0004` (Kartenzahl je Spalte) und `I0013` (Erledigte gebündelt sehen) bauen auf der Abschlussspalten-Markierung auf. Für Agenten ist es der erste Beweis, dass die API die Struktur eines Boards verändert und nicht nur Inhalte einträgt.

## Funktionale Anforderungen

- Eine Spalte wird mit Bezeichnung an einem bestehenden Board angelegt und erscheint als letzte in der Reihenfolge.
- Die Bezeichnung einer Spalte lässt sich ändern.
- Die Reihenfolge aller Spalten eines Boards wird in einem Aufruf gesetzt; die Positionen sind danach lückenlos 1..n.
- Eine Spalte lässt sich entfernen; die Positionen der verbleibenden Spalten werden anschließend wieder lückenlos verdichtet.
- Ein Board darf ohne Spalten existieren — die letzte Spalte ist entfernbar.
- Jede Spalte lässt sich als Abschlussspalte mit einer Anzeigegrenze markieren und wieder entmarkieren.
- Ein Board darf beliebig viele Abschlussspalten tragen, auch keine.
- Spaltenbezeichnungen müssen nicht eindeutig sein; die Identität ist die `SpalteId`.
- Jede dieser Fähigkeiten ist über die API und über die Oberfläche erreichbar.

## Nicht-funktionale Anforderungen

- **Architektur:** `KanbanC.Blazor` bekommt weiterhin keine Projektreferenz auf `KanbanC.BL`; die Spaltenpflege der Oberfläche läuft ausschließlich über HTTP.
- **Konsistenz:** Umsortieren und Entfernen laufen je in einer Transaktion — es gibt keinen Zwischenstand mit doppelten oder fehlenden Positionen.
- **Schema:** Die Tabelle `Spalte` aus Migration `001` trägt bereits alle benötigten Felder. Dieser Slice braucht **keine** Schemamigration und damit auch noch kein Migrations-Journal (siehe „Notizen → Befunde aus R00001").
- **Sicherheit:** Full-Trust — keine Authentifizierung, keine Rechteprüfung (Leitplanke der Vision).

## Akzeptanzkriterien

### Spalte anlegen
- [x] `POST /api/boards/{boardId}/spalten` mit einer Bezeichnung legt eine Spalte an und liefert sie mit vergebener `SpalteId` zurück (HTTP 201).
- [x] Die neue Spalte steht als letzte in der Reihenfolge: bei drei vorhandenen Spalten erhält sie Position 4.
- [x] Die neue Spalte erscheint danach in `GET /api/boards/{boardId}` an dieser Stelle.
- [x] Zwei Spalten mit derselben Bezeichnung sind erlaubt und erhalten verschiedene `SpalteId`.
- [x] Eine Spalte lässt sich auch an einem Board anlegen, das keine Spalte mehr hat — sie erhält Position 1.
- [x] `POST` auf eine nicht vergebene `boardId` liefert HTTP 404; es entsteht keine Spalte.

### Spalte umbenennen und markieren
- [x] `PUT /api/boards/{boardId}/spalten/{spalteId}` ändert die Bezeichnung; der Abruf des Boards liefert die neue Bezeichnung.
- [x] Derselbe Aufruf setzt und entfernt die Abschlussspalten-Markierung samt Anzeigegrenze.
- [x] Die Position der Spalte bleibt dabei unverändert.
- [x] `PUT` auf eine `spalteId`, die zu einem anderen Board gehört, liefert HTTP 404 und ändert nichts.
- [x] `PUT` auf eine nicht vergebene `spalteId` liefert HTTP 404.

### Abschlussspalte und Anzeigegrenze
- [x] Mehrere Spalten desselben Boards lassen sich gleichzeitig als Abschlussspalte markieren; alle behalten ihre eigene Anzeigegrenze.
- [x] Ein Board ohne markierte Spalte ist zulässig — das Entmarkieren der einzigen Abschlussspalte wird nicht abgelehnt.
- [x] Eine markierte Spalte ohne Anzeigegrenze wird mit HTTP 400 zurückgewiesen.
- [x] Eine Anzeigegrenze von 0 oder kleiner wird mit HTTP 400 zurückgewiesen.
- [x] Eine Anzeigegrenze an einer nicht markierten Spalte wird mit HTTP 400 zurückgewiesen; eine nicht markierte Spalte trägt danach keine Grenze.

### Spalten umsortieren
- [x] `PUT /api/boards/{boardId}/spalten/reihenfolge` mit der vollständigen Liste der `SpalteId` setzt die Reihenfolge und liefert die Spalten in der neuen Ordnung (HTTP 200).
- [x] Die Positionen sind danach lückenlos aufsteigend 1..n: aus `[C, A, B]` wird C=1, A=2, B=3.
- [x] Eine unvollständige Liste (2 von 3 Spalten) wird mit HTTP 400 zurückgewiesen; die bestehende Reihenfolge bleibt unverändert.
- [x] Eine Liste mit einer doppelten `SpalteId` wird mit HTTP 400 zurückgewiesen.
- [x] Eine Liste, die eine `SpalteId` eines anderen Boards enthält, wird mit HTTP 400 zurückgewiesen.

### Spalte entfernen
- [x] `DELETE /api/boards/{boardId}/spalten/{spalteId}` entfernt die Spalte (HTTP 204); der Abruf des Boards zeigt sie nicht mehr.
- [x] Die Positionen der verbleibenden Spalten sind danach wieder lückenlos 1..n: nach dem Entfernen der mittleren von drei Spalten haben die beiden übrigen Position 1 und 2.
- [x] Auch die letzte verbliebene Spalte lässt sich entfernen; das Board bleibt bestehen und liefert eine leere Spaltenliste.
- [x] Eine als Abschlussspalte markierte Spalte lässt sich ohne Vorbedingung entfernen.
- [x] `DELETE` auf eine nicht vergebene `spalteId` oder auf eine Spalte eines anderen Boards liefert HTTP 404.

### Zurückweisung ungültiger Eingaben
- [x] Eine leere oder nur aus Leerzeichen bestehende Bezeichnung wird beim Anlegen und beim Ändern mit HTTP 400 zurückgewiesen; der Bestand bleibt unverändert.
- [x] Jede Zurückweisung liefert den Rumpf `Zurueckweisung` mit mindestens einem lesbaren Befund.

### Oberfläche
- [ ] Die Spaltenansicht eines Boards zeigt je Spalte Bezeichnung, Position und die Abschlussspalten-Markierung mit ihrer Anzeigegrenze.
- [ ] Ein Formular legt eine weitere Spalte an; sie erscheint danach am Ende der Liste.
- [ ] Je Spalte lassen sich Bezeichnung, Markierung und Anzeigegrenze bearbeiten und speichern.
- [ ] Je Spalte verschieben zwei Bedienelemente sie um eine Position nach oben bzw. unten; die Liste zeigt die neue Ordnung.
- [ ] Je Spalte entfernt ein Bedienelement sie; sie verschwindet aus der Liste.
- [ ] Eine Zurückweisung der API erscheint als lesbare Meldung, ohne dass die Seite abstürzt.
- [ ] Die Oberfläche erreicht die Spalten ausschließlich über HTTP-Aufrufe der WebApi.

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Pages/` (Spaltenpflege im Board-Detail von `Boards.razor`), `Source/KanbanC.Blazor/Services/` (HTTP-Klient der Spalten).
- **API:** `Source/KanbanC.WebApi/Endpunkte/` — eigene Datei `SpaltenEndpunkte.cs` neben `BoardEndpunkte.cs`.
- **Fachlogik:** `Source/KanbanC.BL/Operations/Boards/` (Validatoren, Positionsvergabe), `Integrations/Boards/` (Dienst), `Interfaces/Boards/` (Repository-Vertrag); Datenzugriff unter `Source/KanbanC.BL/Persistenz/Boards/`.
- **Verträge:** `Source/KanbanC.Contracts/Boards/` — die neuen Anfrage-DTOs.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Boards/` und `Integrations/Boards/`, `Source/KanbanC.Blazor.Tests/Services/`, `Source/KanbanC.WebApi.IntegrationTests/Api/` und `Persistenz/Boards/`, `Source/KanbanC.PlaywrightTests/Tests/` mit Erweiterung des Seitenobjekts `BoardsSeite`.

## Technische Überlegungen

### Ablauf

1. **Spalte anlegen** (`POST /api/boards/{boardId}/spalten`)
   - 1.1 `SpalteAnlegenAnfrage` aus dem Rumpf lesen
   - 1.2 `SpaltenValidator.Pruefe(anfrage)` — Bezeichnung nicht leer, Markierung und Anzeigegrenze konsistent
     - 1.2.1 Bei Befunden: HTTP 400 mit der Liste der Befunde, kein Schreibzugriff
   - 1.3 `ISpaltenRepository.LegeAn(boardId, anfrage)` — höchste vorhandene Position + 1, in einer Transaktion
     - 1.3.1 Board unbekannt: HTTP 404
   - 1.4 HTTP 201 mit der angelegten Spalte
2. **Spalte ändern** (`PUT /api/boards/{boardId}/spalten/{spalteId}`)
   - 2.1 Dieselbe Prüfung wie 1.2
   - 2.2 `ISpaltenRepository.Aendere(boardId, spalteId, anfrage)` — Bezeichnung, Markierung, Anzeigegrenze; Position unberührt
     - 2.2.1 Spalte unbekannt oder an fremdem Board: HTTP 404
3. **Reihenfolge setzen** (`PUT /api/boards/{boardId}/spalten/reihenfolge`)
   - 3.1 Ist-Spalten des Boards laden
   - 3.2 `SpaltenreihenfolgeValidator.Pruefe(gewuenschteReihenfolge, vorhandeneSpaltenIds)` — reine Mengenprüfung: vollständig, ohne Dublette, ohne Fremde
     - 3.2.1 Bei Befunden: HTTP 400, kein Schreibzugriff
   - 3.3 `ISpaltenRepository.SetzeReihenfolge(boardId, reihenfolge)` — Positionen 1..n in einer Transaktion
4. **Spalte entfernen** (`DELETE /api/boards/{boardId}/spalten/{spalteId}`)
   - 4.1 `ISpaltenRepository.Entferne(boardId, spalteId)` — löschen und die verbleibenden Positionen in derselben Transaktion auf 1..n verdichten
     - 4.1.1 Spalte unbekannt oder an fremdem Board: HTTP 404
   - 4.2 HTTP 204
5. **Oberfläche**
   - 5.1 `SpaltenApiKlient` ruft die vier Endpunkte über den benannten `HttpClient` „KanbanC"
   - 5.2 Das Board-Detail zeigt die Spalten als bearbeitbare Liste; Hoch/Runter sendet die **ganze** neue Reihenfolge (Punkt 3), nicht eine Einzelposition
   - 5.3 Zurückweisungen erscheinen wie in `R00001` als Meldung über der Liste

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** `SpaltenEndpunkte.Registriere(app)` in `Program.cs` der WebApi neben der bestehenden Board-Registrierung; die Spaltenpflege hängt sich in den bestehenden Abschnitt `#board-details` von `Boards.razor`, der die Spalten heute nur anzeigt.

**KanbanC.Contracts**
- `SpalteAnlegenAnfrage` (DTO, immutable record) — Bezeichnung, ob Abschlussspalte, Anzeigegrenze. Keine Position: die vergibt der Server.
- `SpalteAendernAnfrage` (DTO, immutable record) — Bezeichnung, ob Abschlussspalte, Anzeigegrenze. Ebenfalls ohne Position.
- `Spaltenreihenfolge` (DTO, immutable record) — die `SpalteId` in der gewünschten Ordnung als `IReadOnlyList<long>`; Adaptermodell der JSON-Serialisierung (C04) wie die übrigen Contracts.
- `Spalte` — unverändert aus `R00001` wiederverwendet, auch als Antwort dieser Endpunkte.

**KanbanC.BL**
- `ISpaltenRepository` (Provider, Interface) — Zugriff auf die Spalten eines Boards
  - `Spalte? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)`
  - `Spalte? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)`
  - `IReadOnlyList<Spalte>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge)`
  - `bool Entferne(long boardId, long spalteId)`
  - `IReadOnlyList<Spalte>? LadeAlle(long boardId)`
  - `null` bzw. `false` heißt durchgängig „Board oder Spalte gibt es nicht" — die Unterscheidung zwischen 404 und 400 trifft der Endpunkt.
- `SpaltenRepository` (Provider, Dapper) — Implementierung gegen SQLite; Umsortieren und Verdichten je in einer Transaktion
- `SpaltenValidator` (Operation) — prüft Anlegen und Ändern; reine Logik, wirft nicht
  - `Pruefbefunde Pruefe(string bezeichnung, bool istAbschlussspalte, int? anzeigegrenze)`
- `SpaltenreihenfolgeValidator` (Operation) — prüft die gewünschte Ordnung gegen die vorhandenen `SpalteId`; reine Mengenlogik ohne Datenzugriff
  - `Pruefbefunde Pruefe(IReadOnlyList<long> gewuenscht, IReadOnlyList<long> vorhanden)`
- `SpaltenService` (Integration) — verdrahtet Validatoren und Repository; enthält keine eigene Logik. Eigene Klasse statt Erweiterung von `BoardService`, weil sonst zwei Unterthemen in einer Integration lägen (C21).
  - `Ergebnis<Spalte>? LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage)`
  - `Ergebnis<Spalte>? AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)`
  - `Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, Spaltenreihenfolge reihenfolge)`
  - `bool EntferneSpalte(long boardId, long spalteId)`
- `Pruefbefunde`, `Ergebnis<T>` — unverändert aus `R00001`. `Pruefbefunde` liegt heute unter `Models/Boards/`; mit dem zweiten Nutzer ist der Zeitpunkt gekommen, es nach `Models/` hochzuziehen (offener Punkt aus `R00001`).

**KanbanC.WebApi**
- `SpaltenEndpunkte` (Integration, statische Registrierung) — bildet die vier Routen auf den `SpaltenService` ab und übersetzt Befunde in HTTP 400, fehlende Entitäten in HTTP 404
  - `static void Registriere(IEndpointRouteBuilder routen)`

**KanbanC.Blazor**
- `SpaltenApiKlient` (Integration) — ruft die vier Endpunkte; kennt kein SQL und keine BL. Eigene Klasse, damit `BoardApiKlient` bei drei Methoden bleibt.
  - `Task<ApiErgebnis<Spalte>> LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage)`
  - `Task<ApiErgebnis<Spalte>> AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)`
  - `Task<ApiErgebnis<IReadOnlyList<Spalte>>> SetzeReihenfolge(long boardId, Spaltenreihenfolge reihenfolge)`
  - `Task EntferneSpalte(long boardId, long spalteId)`
- `ApiErgebnis<T>` — unverändert aus `R00001`.

**Migration**
- Keine. Die Tabelle `Spalte` aus `001-boards-und-spalten.sql` trägt `Bezeichnung`, `Position`, `IstAbschlussspalte` und `Anzeigegrenze` bereits; kein Feld kommt hinzu, keins ändert seinen Typ.

### Änderungen an bestehenden Klassen

- `Source/KanbanC.WebApi/Program.cs` — `ISpaltenRepository`/`SpaltenRepository` und `SpaltenService` registrieren, `SpaltenEndpunkte.Registriere(app)` aufrufen.
- `Source/KanbanC.Blazor/Program.cs` — `SpaltenApiKlient` registrieren.
- `Source/KanbanC.Blazor/Components/Pages/Boards.razor` — der Abschnitt `#board-details` wird von der reinen Anzeige zur Spaltenpflege: Anlegeformular, je Zeile Bearbeiten/Hoch/Runter/Entfernen, eigene Meldungszeile für Zurückweisungen der Spalten-Endpunkte.
- `Source/KanbanC.PlaywrightTests/PageObjects/BoardsSeite.cs` — Zugriffe auf die neuen Bedienelemente; die bestehenden Locator (`#spalten-liste`) bleiben, damit die E2E-Tests aus `R00001` weiterlaufen.
- `Source/KanbanC.BL/Models/Boards/Pruefbefunde.cs` → `Source/KanbanC.BL/Models/Pruefbefunde.cs` — mit dem zweiten Unterthema ist die Klasse nicht mehr board-spezifisch.

## Tests

Nach Skill `test-pyramide`: alle drei Ebenen, die Given/When/Then-Szenarien der User Story werden E2E-Tests.

**Kandidaten für Unit-Tests (pure Logik nach IOSP):**
- `SpaltenValidator` — leere Bezeichnung, markiert ohne Grenze, Grenze ≤ 0, Grenze ohne Markierung, gültiger Fall.
- `SpaltenreihenfolgeValidator` — vollständige Liste, zu kurze Liste, Dublette, fremde `SpalteId`, leere Liste an einem Board ohne Spalten.
- `SpaltenService` — gegen ein Test-Repository nach dem Muster von `TestBoardRepository`: dass eine zurückgewiesene Anfrage das Repository nicht erreicht, und dass ein unbekanntes Board vor der Prüfung zu `null` führt.
- `SpaltenApiKlient` in `KanbanC.Blazor.Tests` — die Fehlerpfade (HTTP 400 mit fremdem Rumpf, 404, leerer Rumpf) sind über den Browser nicht auslösbar; Muster `TestKlientFabrik` aus `R00001`.

**Integration:** `SpaltenRepository` gegen eine echte SQLite-Datei im Temp-Verzeichnis (je Test frisch) — Anlegen ans Ende, Ändern ohne Positionsverlust, Umsortieren in einer Transaktion, Entfernen mit anschließender Verdichtung auf 1..n, Entfernen der letzten Spalte. Die vier Endpunkte über `WebApplicationFactory` samt Statuscodes 201, 200, 204, 400 und 404 — einschließlich der Fremdboard-Fälle.

**E2E:** Spalte anlegen und am Ende der Liste wiederfinden; Spalte umbenennen; Spalte nach oben schieben und die neue Ordnung sehen; Spalte entfernen; Spalte als Abschlussspalte mit Grenze markieren; leere Bezeichnung absenden und die Meldung sehen. Anwendung auf freien Ports (Skill `freier-port`).

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `I0001` Board anlegen (`R00001`, Status `gruen`) — ohne Board keine Spalten.
- Blockiert: `I0004` Kartenzahl je Spalte anzeigen, `I0010` Board ansehen, `I0013` Erledigte Karten gebündelt sehen — alle drei führen `I0003` in ihrer Spalte `Braucht`.

## Umfang

`Keine Bubbles unter I0003 (erreicht: Interaction) — /planung verfeinern I0003 --bis Bubble`

Der Slice ist bis zur Ebene Interaction geplant, nicht bis Bubble; es gibt keine Zählung. Eine Schätzung wird hier bewusst nicht danebengeschrieben.

## Offene Fragen

Keine. Sechs Entscheidungen wurden vor dem Schreiben getroffen und stehen unter „Notizen".

## Manuelle Vorbereitungstätigkeiten

Keine.

## Manuelle Nachbereitungstätigkeiten

Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass `R00001` jedem Board dieselben drei Spalten verpasst und niemand sie danach anfassen kann — die Struktur eines Boards ist heute eine Schablone, kein Werkzeug. Das Zielbild verlangt eine API, die Boards nicht nur befüllt, sondern gestaltet: „Spalten, Klassen, Struktur". Wenn wir die vier Spalten-Endpunkte bauen und die Oberfläche ausschließlich über sie bedienen, dann wird erstens der Spaltenbestand veränderlich und `I0010` bekommt eine Struktur, die es überhaupt lohnt anzusehen; zweitens erhält die Abschlussspalten-Markierung ihre fachliche Bedeutung — sie ist die Ansage „diese Spalte wird nicht vollständig geladen", auf der `I0013` das Nachladen aufbaut. Gerade dieser Slice ist der Hebel und nicht `I0011 Karte anlegen`, weil Karten in eine Spalte gehören und ein Kartenmodell auf einer unveränderlichen Dreier-Schablone die falschen Annahmen erben würde; und nicht ein noch kleinerer Schnitt (etwa nur Anlegen), weil erst Umsortieren und Entfernen zeigen, dass die Positionsvergabe trägt — genau die Stelle, an der die späteren Kartenslices andocken.

## Missing-Docs

Keine bekannte Lücke. Die beiden Themen dieses Slice sind belegt:

- Transaktionen über mehrere Zeilen mit Dapper und SQLite → [Dokumentation/Bibliotheken/dapper-sqlite-transaktionen.md](../Dokumentation/Bibliotheken/dapper-sqlite-transaktionen.md)
- Kein neues Bibliotheksthema: Umsortieren und Verdichten sind gewöhnliche `UPDATE`-Anweisungen, die Oberfläche nutzt Blazor-Bordmittel ohne Drag-and-drop.

## Notizen

### Getroffene Entscheidungen

- **Voller Umfang in einem Slice** — alle fünf Fähigkeiten über beide Systemgrenzen, wie das Fertig-Kriterium es wörtlich verlangt. Eine Aufteilung in Ausbaustufen hätte `I0010` und `I0013` auf die zweite Stufe warten lassen.
- **Mehrere Abschlussspalten je Board erlaubt, auch keine.** Die Markierung ist keine Auszeichnung „hier endet der Fluss", sondern die Ladeeigenschaft „die Inhalte dieser Spalte werden nicht vollständig geladen, sondern erst bei Bedarf". Eine solche Eigenschaft darf an mehreren Spalten hängen; ein Board ganz ohne sie ist ebenfalls sinnvoll.
- **Umsortieren als vollständige Reihenfolge in einem Aufruf.** Der Aufrufer schreibt die Zielordnung hin, der Server vergibt 1..n. Atomar, keine Sonderfälle beim Verschieben, und ein Agent braucht einen Aufruf statt n.
- **Hartes Löschen ohne Guard.** Weder Position noch Markierung noch „letzte Spalte" schützen eine Spalte. Ein Board ohne Spalten ist ein zulässiger Zustand.
- **Positionen bleiben lückenlos.** Nach jedem Entfernen werden die verbleibenden Spalten in derselben Transaktion auf 1..n verdichtet — sonst müsste jede spätere Umsortierung mit Lücken rechnen.
- **Anzeigegrenze und Markierung sind ein Paar.** Markiert ohne Grenze und Grenze ohne Markierung werden beide zurückgewiesen, statt still korrigiert zu werden (C24, sichtbarer Fehlerpfad).

### Vorentscheidung für spätere Slices

**Karten beim Entfernen einer Spalte.** Sobald es Karten gibt (`I0011`), darf eine Spalte mit nicht archivierten Karten nicht wortlos verschwinden — der Löschvorgang fragt dann, ob die Karten mitgelöscht oder in eine andere Spalte verschoben werden. In `R00002` ist das **kein Akzeptanzkriterium**, weil es keine Kartentabelle gibt und die Regel deshalb durch keinen Test belegbar wäre. Die API-Form ist so gewählt, dass der Zusatz später additiv passt (optionaler Parameter am `DELETE`), ohne bestehende Aufrufer zu brechen. Der Punkt gehört als Nachzug an `I0011` bzw. `I0014`.

### Befunde aus R00001

- **Migrations-Journal:** `R00001` nennt unter „Offene Punkte" ein Journal „vor `I0003`", weil `CREATE TABLE IF NOT EXISTS` ab der ersten `ALTER TABLE` nicht mehr reicht. Geprüft: dieser Slice braucht keine Schemaänderung — alle vier Spaltenfelder existieren. Das Journal bleibt offen und wird beim ersten Slice fällig, der das Schema wirklich verändert.
- **`Pruefbefunde` hochziehen:** ebenfalls aus `R00001`; mit `SpaltenValidator` gibt es den zweiten Nutzer, der Umzug nach `Models/` ist Teil dieses Slice.

### Verworfene Alternativen

- *Zwei Ausbaustufen (erst anlegen/umbenennen/entfernen, dann umsortieren/markieren)* — früher grün, aber drei Folgeslices hätten auf die zweite Stufe gewartet.
- *Nur API in diesem Slice, Oberfläche später* — widerspricht der WBS-Leitentscheidung, dass jede Interaction über beide Systemgrenzen gilt.
- *Genau eine Abschlussspalte je Board* — hätte zu `I0013` („die Abschlussspalte") gepasst, macht die Markierung aber zu einer Struktur-Aussage statt einer Ladeeigenschaft.
- *Einzelne Spalte an eine Zielposition verschieben (`PATCH` mit Position)* — näher an Drag-and-drop, aber jede Verschiebung wäre ein Mehrzeilen-Update mit Rand- und Kollisionsfällen.
- *Relatives Hoch/Runter als eigene Endpunkte* — trivial zu bauen, für einen Agenten aber umständlich: n Aufrufe für eine Umordnung. Die Oberfläche bietet Hoch/Runter trotzdem an — sie sendet dabei die ganze Reihenfolge.
- *Stilllegen statt löschen (`IstEntfernt`-Flag)* — nimmt eine Entscheidung vorweg, die erst mit Karten anfällt, und kostet eine Schemamigration ohne heutigen Bedarf (C16).
- *Position vom Aufrufer beim Anlegen bestimmen lassen* — zweiter Weg, die Reihenfolge zu setzen; die Reihenfolge-Route ist der eine Weg.

### Out of scope

- Karten jeder Art, ihr Verbleib beim Löschen und das Nachladen jenseits der Anzeigegrenze (`D0003`, `I0013`).
- Kartenzahl in der Spaltenkopfzeile (`I0004`).
- Live-Aktualisierung der Spaltenstruktur in anderen offenen Sichten (`D0007`).
- Spaltenvorlagen je Board-Art oder wiederverwendbare Spaltensets — kein Slice der WBS verlangt sie.
- Board umbenennen und archivieren (`I0005`).
