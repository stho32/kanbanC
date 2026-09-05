---
id: R00015
status: Neu
datum: 2026-09-05
---

# R00015: Erledigte Karten gebündelt sehen

## Beschreibung

Eine Karte, die in die Abschlussspalte kommt, hält fest, an welchem Tag das war. Die Abschlussbahn zeigt ihre Karten daraufhin unter Datumsüberschriften — die neuesten zuerst — und nur so viele, wie ihre Anzeigegrenze erlaubt; der Bahnenkopf sagt mit `20+`, dass mehr da sind. Was nicht gezeigt wird, ist nicht verloren: die API liefert die Karten einer Spalte auf eigener Adresse vollständig, und in der Oberfläche holt „Ältere nachladen" sie in die Bahn.

Zahlt ein auf: [Vision](R00000-vision.md) — „Visuelle Haltung an Kanbanflow orientiert" und „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

## Geschäftlicher Nutzen

Eine Abschlussspalte wächst monoton: alles, was je fertig wurde, sammelt sich dort und drängt die drei Bahnen, in denen tatsächlich gearbeitet wird, an den Rand. Kanbanflow löst das, ohne dass jemand archivieren muss — nach Fertigstellungsdatum gruppieren, die 20 neuesten zeigen, den Rest liegen lassen ([kanbanflow.com/features](https://kanbanflow.com/features)). Dieselbe Lösung hier bringt zwei Dinge zugleich: das Board bleibt lesbar, ohne dass ein Pflegeschritt entsteht, und mit dem Erledigungsdatum entsteht der erste **zeitliche** Datenpunkt des Bestands. Ohne ihn ist keine der Auswertungen aus dem Zielbild — Burndown, Durchsatz, Soll-Ist — überhaupt rechenbar; sie brauchen alle die Frage „wann war was fertig", und die kann heute niemand beantworten.

## Funktionale Anforderungen

- Eine Karte, die in die Abschlussspalte gelangt, trägt danach das Datum dieses Eintritts.
- Ein Zug **innerhalb** der Abschlussspalte lässt das Datum unberührt; ein Zug **heraus** löscht es; ein erneuter Eintritt setzt das aktuelle Datum.
- `GET /api/boards/{boardId}` liefert das Erledigungsdatum je Karte mit.
- Die Abschlussspalte wird auf ihre Anzeigegrenze gekürzt geliefert und angezeigt — die neuesten zuerst.
- Jede Spalte nennt zusätzlich die Zahl **aller** ihrer Karten, damit eine gekürzte Liste als gekürzt erkennbar ist.
- Die Oberfläche gruppiert die Abschlussbahn nach Erledigungsdatum, mit Zählung je Gruppe, und zeigt im Bahnenkopf `N+` statt einer genauen Zahl, solange gekürzt wird.
- Die API liefert auf `GET /api/boards/{boardId}/spalten/{spalteId}/karten` alle Karten einer Spalte ungekürzt.
- In der Bahnenfläche der gekürzten Abschlussbahn steht ein Hinweis und das Bedienelement „Ältere nachladen", das alle Karten in die Bahn holt.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** Die Migration ist idempotent — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-23`). Deshalb eine eigene Tabelle statt `ALTER TABLE`, wie bei den Migrationen 004, 005 und 007.
- **Datenehrlichkeit:** Eine gekürzte Kartenliste wird nie ohne die wahre Kartenzahl ausgeliefert. Für Bestandskarten wird kein Datum erfunden.
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der neuen Adresse trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier keine Projektreferenz auf `KanbanC.BL`; die Nachladung geht über HTTP.

## Akzeptanzkriterien

### Das Erledigungsdatum (F0035)

- [ ] `GET /api/boards/{boardId}` liefert je Karte ein Feld `erledigtAm`. Für eine Karte außerhalb der Abschlussspalte ist es `null`.
- [ ] `PUT /api/boards/{boardId}/karten/{karteId}/lage` mit der `SpalteId` der Abschlussspalte setzt `erledigtAm` auf das heutige Datum im Format `JJJJ-MM-TT`.
- [ ] Ein zweiter Zug **innerhalb** der Abschlussspalte lässt `erledigtAm` unverändert. Rechenbeispiel: Karte am 1.9. abgelegt, am 3.9. innerhalb der Bahn von Position 4 auf Position 1 gezogen → `erledigtAm` ist weiterhin der 1.9.
- [ ] Ein Zug **aus** der Abschlussspalte heraus setzt `erledigtAm` auf `null`; in der Tabelle `Karteerledigung` steht danach keine Zeile mehr für diese Karte.
- [ ] Ein erneuter Eintritt setzt das **heutige** Datum, nicht das frühere. Rechenbeispiel: am 1.9. erledigt, am 2.9. zurück nach „In Arbeit", am 3.9. wieder abgelegt → `erledigtAm` ist der 3.9.
- [ ] Eine Karte, die über `POST /api/boards/{boardId}/spalten/{spalteId}/karten` direkt in der Abschlussspalte angelegt wird, trägt sofort das heutige Datum.
- [ ] Karten, die vor dieser Anforderung in einer Abschlussspalte lagen, tragen `erledigtAm: null` — die Migration trägt kein Datum nach.
- [ ] Wird ein Zug zurückgewiesen (unmögliche Position, unbekannte Karte, fremde Spalte), ist danach kein Erledigungsdatum geschrieben, gelöscht oder geändert.
- [ ] Ein zweiter Lauf der Migration auf einer bestehenden Datei lässt Schema **und** Daten unverändert; ein gesetztes Erledigungsdatum bleibt stehen.
- [ ] Nach einem Neustart der WebApi auf derselben Datei stehen die Erledigungsdaten unverändert da.

### Gruppierung und Kürzung der Abschlussspalte (F0036)

- [ ] `GET /api/boards/{boardId}` liefert für eine Abschlussspalte höchstens so viele Karten, wie ihre Anzeigegrenze `N` erlaubt. Rechenbeispiel: `N` = 20 bei 23 erledigten Karten → `karten` enthält 20 Einträge.
- [ ] Jede Spalte trägt ein Feld `kartenzahl` mit der Zahl **aller** Karten der Spalte. Rechenbeispiel: bei 23 Karten und `N` = 20 ist `kartenzahl` = 23 und `karten.length` = 20; bei einer ungekürzten Spalte sind beide gleich.
- [ ] Die gelieferten Karten der Abschlussspalte sind die `N` **neuesten**: geordnet nach `erledigtAm` absteigend, Karten ohne Datum zuletzt, innerhalb desselben Datums nach der Position der Spalte. Rechenbeispiel: `N` = 3 und die Karten 3.9., 2.9., 2.9., ohne Datum → geliefert werden die drei mit Datum; die Bestandskarte fällt als Erste heraus.
- [ ] Randwerte: 0 Karten → leere Liste, `kartenzahl` 0; `N-1` Karten → ungekürzt; genau `N` → ungekürzt, `kartenzahl` = `N`; `N+1` → gekürzt auf `N`, `kartenzahl` = `N+1`.
- [ ] `PUT /api/boards/{boardId}/karten/{karteId}/lage` liefert die Spalten in derselben gekürzten Gestalt wie `GET /api/boards/{boardId}` — es gibt nicht zwei Antwortgestalten für dieselbe Sache.
- [ ] Eine Spalte **ohne** Abschlussmarkierung wird nie gekürzt, gleich wie viele Karten sie trägt.
- [ ] Ein Zug bleibt gegen den **ganzen** Bestand geprüft: eine Position, die innerhalb der ungekürzten Zielspalte liegt, wird nicht deshalb zurückgewiesen, weil die gekürzte Liste kürzer ist. Rechenbeispiel: 23 Karten in „Erledigt", Zug auf Position 22 → keine Zurückweisung.
- [ ] In der Oberfläche steht über jeder Datumsgruppe der Abschlussbahn eine Überschrift mit der Zahl der Karten dieser Gruppe; die Summe der Gruppenzahlen ist die Zahl der gezeigten Karten. Rechenbeispiel: „Heute · 3" und „Gestern · 5" → acht Karten in der Bahn.
- [ ] Karten ohne Erledigungsdatum stehen unter einer eigenen, **letzten** Gruppe.
- [ ] Bei eingeschalteter Kartenzahl zeigt der Kopf der Abschlussbahn `N+`, solange gekürzt wird, sonst die genaue Zahl. Rechenbeispiel: 23 Karten bei `N` = 20 → `20+`; 20 Karten bei `N` = 20 → `20`; 7 Karten → `7`.
- [ ] Bei ausgeschalteter Kartenzahl bleibt die Stelle im Bahnenkopf leer — auch bei gekürzter Bahn; kein `+`, kein Platzhalter.
- [ ] Andere Bahnen bekommen weder Datumsüberschriften noch die Form `N+`; ihre Karten stehen unverändert in Positionsreihenfolge. Die bestehenden Zusagen aus `R00009` gelten unverändert — auf einem Board mit drei ungekürzten Bahnen und vier Karten stehen weiterhin die exakten Zahlen `3`, `1`, `0`.
- [ ] Ein laufender Zug über der Abschlussbahn zeigt **keine** Kartenhälften und **keine** Einfügelinie; die Bahn nimmt ganzflächig an, und das Ablegen bringt die Karte an Position 1.
- [ ] In allen anderen Bahnen bleiben Kartenhälften, Einfügelinie und Zielposition unverändert (`R00008`).

### Ältere Karten erreichbar (F0037)

- [ ] `GET /api/boards/{boardId}/spalten/{spalteId}/karten` antwortet mit HTTP 200 und **allen** Karten der Spalte in Anzeigereihenfolge — auch dann, wenn `GET /api/boards/{boardId}` dieselbe Spalte gekürzt liefert. Rechenbeispiel: 23 erledigte Karten bei `N` = 20 → 23 Karten.
- [ ] Jede so gelieferte Karte trägt ihr `erledigtAm`.
- [ ] Die Adresse gilt für **jede** Spalte, nicht nur für Abschlussspalten.
- [ ] Auf eine unbekannte `boardId` oder eine unbekannte bzw. fremde `spalteId` antwortet die Adresse mit HTTP 404 **und einem Rumpf**: mindestens ein Befund mit nichtleerem `code`, einer `meldung`, welche die aufgerufenen Nummern nennt, und einer `kompensation`, die einen ausführbaren nächsten Aufruf enthält.
- [ ] Der Vertragstest über alle registrierten Routen bleibt grün: die neue Route wird von ihm abgerufen und ist nicht als ungeprüft übrig.
- [ ] Ist die Abschlussbahn gekürzt, steht in der **Bahnenfläche** ein Hinweis, dass nur die neuesten gezeigt werden, und darunter das Bedienelement „Ältere nachladen".
- [ ] Ein Klick darauf zeigt alle Karten der Bahn, ohne dass die Seite neu geladen wird; danach sind Hinweis und Bedienelement fort, und der Bahnenkopf zeigt die genaue Zahl statt `N+`. Rechenbeispiel: `20+` und 20 sichtbare Karten → nach dem Klick `23` und 23 sichtbare Karten.
- [ ] Ist die Bahn nicht gekürzt, gibt es weder Hinweis noch Bedienelement.
- [ ] Der Fuß der Abschlussbahn trägt weiterhin „+ Karte" — das Nachladen sitzt in der Bahnenfläche und verdrängt das Anlegen nicht.
- [ ] Nach einem Reload ist die Bahn wieder gekürzt: das Nachladen ist eine Handlung, kein gespeicherter Zustand.
- [ ] Ist die WebApi beim Nachladen nicht erreichbar, erscheint eine lesbare Ausfallmeldung statt einer Ausnahmeseite; das Board bleibt bedienbar.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Karten/Karte.cs` (wächst um `ErledigtAm`), `Source/KanbanC.Contracts/Boards/Spalte.cs` (wächst um `Kartenzahl`).
- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/008-karteerledigung.sql` — neue, idempotente Migration.
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/` — `Erledigungsstand` (Regel, was ein Zug mit dem Datum macht) und `Abschlussbahn` (Ordnung und Kürzung).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (Erledigungs-Join in beiden Lesewegen), `KartenRepository.cs` (Schreiben und Löschen der Erledigung in derselben Transaktion; neue Lesemethode), `Source/KanbanC.BL/Persistenz/Boards/Spaltenleser.cs` (Kartenzahl je Spalte), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Boards/BoardService.cs` und `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — die Kürzung sitzt an ihrem **Ausgang**.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — die vorhandene Basisroute bekommt ein `MapGet` neben ihrem `MapPost`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (Nachladen), `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (+ `.razor.css`: Datumsgruppen, `N+`, Nachlade-Hinweis, ganzflächige Annahme der Abschlussbahn), `Source/KanbanC.Blazor/Services/Ablagestellen.cs`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Karten/`, `Source/KanbanC.BL.Tests/Integrations/` samt `TestHelpers/TestKartenRepository.cs` und `TestSpaltenRepository.cs`, `Source/KanbanC.Blazor.Tests/Services/KartenApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/` (`Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/KartenAmBoardTests.cs`, `Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (Seitenobjekt `BoardSeite` und zwei neue Testklassen).
- **Unberührt:** `Source/KanbanC.BL/Operations/Karten/KartenlageValidator.cs` und `KartenService.KartenzahlNachDemZug` — sie rechnen weiterhin gegen den ungekürzten Bestand.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0003.dc.html`](../Dokumentation/Wireframes/D0003.dc.html), **Bahn 5 „Fertig"** (Zeilen 311–366), ist die Gestaltungsvorgabe. Daraus gelten für diese Anforderung vier Stellen: der Bahnenkopf mit `Grenze 20` und der Zahl `20+`, die Gruppenüberschriften über den Karten, der Nachlade-Hinweis und das Bedienelement „Ältere nachladen" in der Bahnenfläche, und der Fuß, der weiterhin dem Anlegen gehört. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Eine begründete Abweichung ist bereits bekannt: die Gruppe **„Ohne Datum"** für Bestandskarten steht nicht im Bild, weil das Bild den Zielzustand zeigt, in dem jede erledigte Karte ein Datum hat.

### Ablauf

1. **Karte wird abgelegt** (`PUT /api/boards/{boardId}/karten/{karteId}/lage`)
   - 1.1 `KartenService.VerschiebeKarte` prüft wie bisher gegen die **ungekürzten** Spalten aus `SpaltenRepository.LadeAlle`
   - 1.2 `KartenRepository.Verschiebe` schreibt die Ordnung und in derselben Transaktion die Erledigung
     - 1.2.1 `Erledigungsstand.NachDemZug(zielspalte, quellspalte, bisherigeErledigung, heute)` liefert `Setzen(datum)`, `Loeschen` oder `Unveraendert`
     - 1.2.2 `Setzen` → `INSERT INTO Karteerledigung … ON CONFLICT(Karte) DO UPDATE`; `Loeschen` → `DELETE`; `Unveraendert` → kein Schreibzugriff
   - 1.3 Rücklesen über `Kartenleser` (mit `LEFT JOIN Karteerledigung`) und `Spaltenleser` (mit Kartenzahl), Commit
   - 1.4 `KartenService` kürzt am Ausgang: `Abschlussbahn.Gekuerzt(spalten)` → Antwort
2. **Board lesen** (`GET /api/boards/{boardId}`)
   - 2.1 `BoardRepository.Lade` liefert vollständig
   - 2.2 `BoardService.LadeBoard` kürzt am Ausgang mit derselben Operation wie 1.4
3. **Bahn anzeigen**
   - 3.1 `Spaltenbahnen.razor` gruppiert die Karten einer Abschlussbahn nach `ErledigtAm` und schreibt Überschrift plus Zählung je Gruppe
   - 3.2 Bahnenkopf: `spalte.Karten.Count < spalte.Kartenzahl` → `$"{spalte.Karten.Count}+"`, sonst `spalte.Kartenzahl`
   - 3.3 Abschlussbahn im Zug: keine Hälften, keine Einfügelinie, Ablegen ergibt Position 1
4. **Ältere nachladen**
   - 4.1 Klick → `KartenApiKlient.LadeKartenDerSpalte(boardId, spalteId)` → `GET /api/boards/{boardId}/spalten/{spalteId}/karten`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 4.2 Erfolg → die Bahn zeigt die gelieferte Liste; Hinweis und Bedienelement entfallen, der Kopf zeigt `spalte.Kartenzahl`
   - 4.3 `HttpRequestException` → Ausfallmeldung, das Board bleibt bedienbar
   - 4.4 Der nächste Board-Abruf (Reload, Anlegen, Zug) setzt die Bahn wieder auf den gekürzten Stand

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte.Basisroute`** — die Adresse existiert bereits als `POST`; es kommt ein `MapGet` auf dieselbe Konstante hinzu. Ein Agent, der weiß, wo eine Karte entsteht, weiß damit auch, wo alle stehen.
- **Ausgang der Dienste** (`BoardService.LadeBoard`, `KartenService.VerschiebeKarte`) — der einzige Ort, an dem gekürzt wird. Das Repository bleibt vollständig, damit die Prüfung eines Zuges gegen den ganzen Bestand rechnet.
- **`Migrationslaeufer`** — die achte Migration reiht sich ein; kein Journal, also idempotent.
- **`Spaltenbahnen.razor`** — der einzige Ort, der Bahnenkopf, Karten und Ablageverhalten gleichzeitig sieht.

**Klassen-Entwurf:**

- `Karte` (Contract, DTO, immutable) — wächst um ein Feld am Ende, damit vorhandene Aufrufstellen ihre Reihenfolge behalten.
  - `public record Karte(long KarteId, string Titel, int Position, DateOnly? ErledigtAm)`
- `Spalte` (Contract, DTO, immutable) — wächst um die wahre Kartenzahl. `Karten` kann gekürzt sein, `Kartenzahl` ist es nie.
  - `public record Spalte(long SpalteId, string Bezeichnung, int Position, bool IstAbschlussspalte, int? Anzeigegrenze, IReadOnlyList<Karte> Karten, int Kartenzahl)`
- `Erledigungsstand` (Operation, pure Logik) — die Regel, was ein Zug mit dem Datum macht. Drei Fälle, kein vierter.
  - `static Erledigungsaenderung NachDemZug(Spalte zielspalte, Spalte quellspalte, DateOnly? bisher, DateOnly heute)`
- `Erledigungsaenderung` (DTO, immutable) — `Setzen(datum)` / `Loeschen` / `Unveraendert`.
- `Abschlussbahn` (Operation, pure Logik) — Ordnung und Kürzung einer Spalte; für Nicht-Abschlussspalten die Identität.
  - `static IReadOnlyList<Spalte> Gekuerzt(IReadOnlyList<Spalte> spalten)`
  - `static Spalte Gekuerzt(Spalte spalte)`
- `IKartenRepository` / `KartenRepository` (Provider, Ressourcenzugriff) — die neue Lesemethode; `null` heißt „diese Spalte gibt es an dieser Stelle nicht".
  - `IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId)`
- `KartenService` (Integration) — verdrahtet die Lesemethode und kürzt am Ausgang von `VerschiebeKarte`.
  - `IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId)`
- `KartenEndpunkte` (Integration, statisch) — `GET` auf der vorhandenen Basisroute; 404 über `Nichtgefunden.Spalte` bzw. `Nichtgefunden.FremdeSpalte`, wie es `LegeKarteAn` schon tut.
  - `routen.MapGet(Basisroute, LiesKartenDerSpalte).WithName("KartenDerSpalteLesen")`
- `KartenApiKlient` (Integration, Blazor) — der HTTP-Weg der Nachladung.
  - `public Task<ApiErgebnis<IReadOnlyList<Karte>>> LadeKartenDerSpalte(long boardId, long spalteId)`
- `Datumsgruppen` (Operation, Blazor) — die Gruppierung der gezeigten Karten für die Anzeige; sie rechnet nur, sie holt nichts.
- **Migration** `008-karteerledigung.sql` (Skript, idempotent) — eine Zeile je erledigter Karte; der Fremdschlüssel **ist** der Schlüssel, sonst trüge eine Karte zwei Erledigungsdaten:
  ```sql
  CREATE TABLE IF NOT EXISTS Karteerledigung
  (
      Karte      INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
      ErledigtAm TEXT NOT NULL
  );
  ```
  `ErledigtAm` steht als ISO-Text in der Spalte und wird in C# umgerechnet: Dapper 2.1.79 weist ein `DateOnly` als Parameterwert ab — belegt in `Source/KanbanC.WebApi.IntegrationTests/Persistenz/SqliteEigenschaftenTests.cs:89-131` (Probe zu Migration 007). Denselben Weg gehen bereits die Board-Termine (`BoardRepository.cs:216-237`).

### Änderungen an bestehenden Klassen

- `Karte` (Contract) — ein Feld mehr. Betroffen sind **sechs** `new Karte(`-Stellen in **fünf** Dateien (WBS `B0187`, nachgezählt beim Planen am 2026-09-04).
- `Spalte` (Contract) — ein Feld mehr. Betroffen sind **neun** `new Spalte(`-Stellen in **sechs** Dateien (WBS `B0190`).
- `Kartenleser` — beide Lesewege bekommen `LEFT JOIN Karteerledigung`; `AlsKarte` rechnet den ISO-Text in `DateOnly?` um.
- `Spaltenleser` — `AlsSpalte` bekommt die wahre Kartenzahl der Spalte; sie kommt aus derselben Abfrage wie die Karten, nicht aus einer zweiten Runde.
- `KartenRepository.LegeAn` (`:23`) und `.Verschiebe` (`:43`) — schreiben bzw. löschen die Erledigung in der laufenden Transaktion. Eine Karte, die direkt in der Abschlussspalte entsteht, ist mit ihrer Anlage erledigt.
- `BoardService.LadeBoard`, `KartenService.VerschiebeKarte` — kürzen am Ausgang. `KartenService.KartenzahlNachDemZug` (`KartenService.cs:100-109`) und `KartenlageValidator` bleiben **unverändert** und rechnen weiter gegen den ganzen Bestand.
- `Spaltenbahnen.razor` (`:21-24`) — die Kartenzahl im Kopf wird aus `Karten.Count` **und** `Kartenzahl` gebildet statt allein aus `Karten.Count`. Auflage: `KartenzahlImBahnenkopfE2ETests` erwartet exakte Zahlen (`["3","1","0"]`, Zeilen 28/37/49/64/71); die dortigen Bahnen sind ungekürzt und müssen **ohne Änderung** grün bleiben.
- `Spaltenbahnen.razor` / `Karte.razor` / `Ablagestellen` — die Abschlussbahn nimmt ganzflächig an. Das ist eine Änderung an grünem Verhalten aus `R00008` (`F0022`): `Ablagestellen.ZielpositionAmEnde` nimmt heute an, die gezeigte Liste sei die ganze Spalte, und rechnete aus einer gekürzten Liste eine Position mitten in die Spalte. `KarteVerschiebenE2ETests.cs:85-86` prüft für die **leere** Erledigt-Bahn eine Ablagefläche und null Hälften und bleibt gültig.
- `FehlervertragTests` (`:53-56`) — liest die registrierten Routen aus dem Testhost und schlägt fehl, sobald eine Route ohne Vertragsfall dazukommt. Der 404-Fall des neuen `GET` gehört deshalb in denselben Arbeitsgang wie die Route (Muster `B0152`/`B0159`).
- `TestKartenRepository`, `TestSpaltenRepository`, `BoardSeite` — je um das Nötige erweitert.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Erledigungsstand.NachDemZug` — Eintritt setzt, Zug innerhalb lässt unberührt, Austritt löscht, erneuter Eintritt setzt neu; ein Zug zwischen zwei Nicht-Abschlussspalten ändert nichts.
- `Abschlussbahn.Gekuerzt` — Randwerte 0, `N-1`, `N`, `N+1`; Karten ohne Datum zuletzt und zuerst gekürzt; Ordnung innerhalb desselben Datums; eine Nicht-Abschlussspalte bleibt unverändert; `Kartenzahl` bleibt in jedem Fall die wahre Zahl.
- `Datumsgruppen` — Gruppenbildung und Zählung je Gruppe, Gruppe „ohne Datum" am Ende.
- `KartenApiKlient.LadeKartenDerSpalte` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert die Liste, 404 liefert die Zurückweisung mit Befund; Methode und Adresse des abgesetzten Aufrufs werden mitgeprüft. Diese Fehlerpfade sind über den Browser nicht auslösbar.
- `KartenService` gegen `TestKartenRepository`/`TestSpaltenRepository` — unbekanntes Board und fremde Spalte liefern `null` **ohne** Lesezugriff auf die Karten.

**Integration:** `KartenRepository` gegen eine `TemporaereDatenbank` — Erledigung schreiben, überschreiben, löschen, in einer Transaktion mit dem Zug; `LadeKartenDerSpalte` liefert alle Karten bzw. `null` bei fremder Spalte. `Migrationslaeufer` — zweiter Lauf lässt Schema und gesetzte Daten unverändert. `KartenEndpunkte` über `TestWebApi` — `GET` mit 200 und allen Karten, 404 mit Rumpf; `PUT .../lage` in die, innerhalb und aus der Abschlussspalte, danach `GET /api/boards/{boardId}` mit gesetztem, unverändertem und entfallenem `erledigtAm`; gekürzte Antwort mit `kartenzahl`. `FehlervertragTests` — die neue Route wird abgerufen. `WebApiNeustartTests` — Erledigungsdaten überstehen den Neustart.

**E2E:** Ein Board mit mehr als `N` erledigten Karten aus zwei Tagen zeigt Gruppenüberschriften mit Zählung, höchstens `N` Karten und `20+` im Kopf (US-1, US-2); „Ältere nachladen" holt die übrigen, danach sind Hinweis und Bedienelement fort und der Kopf zeigt die genaue Zahl (US-3); ein Reload kürzt wieder (US-3); eine Karte in die Erledigt-Bahn ziehen, sie erscheint oben unter „Heute", die Quellbahn verliert sie (US-1); die Bahn nimmt ganzflächig an, ohne Hälften und ohne Einfügelinie (US-1); der Agent liest die Spalte vollständig über die API, während die Oberfläche kürzt (US-4). Dazu laufen alle E2E-Tests aus `R00001`–`R00014` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: `R00007` (Karte verschieben — `I0012`, grün) und `R00002` (Spalten gestalten — `I0003`, grün, liefert Abschlussmarkierung und Anzeigegrenze). Beide Vorbedingungen der WBS-Spalte `Braucht` von `I0013` sind erfüllt; der Slice ist **frei**.
- Setzt außerdem auf: `R00009` (Kartenzahl je Spalte — der Bahnenkopf und die dort zurückgestellte Form `20+`, `R00009:85`, `:236`), `R00008` (Einfügelinie statt Ablagekästen — die Abschlussbahn weicht davon ab), `R00007` (Fehlervertrag — `Nichtgefunden.Spalte` und `Zurueckweisungen.AlsNichtgefunden` werden unverändert benutzt, es entsteht kein neuer Fehlercode).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0013`, `F0035`, `F0036` oder `F0037` in seiner Spalte `Braucht` (geprüft am 2026-09-05 über `Dokumentation/Planung/kanbanc.md`).
- Reihenfolge innerhalb der Anforderung: `F0035` → `F0036` → `F0037`. Ohne Erledigungsdatum gibt es nichts zu gruppieren, ohne Kürzung nichts nachzuladen; die WBS führt genau diese Kette in der Spalte `Braucht`.

## Umfang

```
Erledigte Karten gebündelt sehen (I0013) = 17 Bubbles: 14 Standard (18,4h), 3 unklar (4,4–9,5h).
Rest: 18,4h klar + 4,4–9,5h unklar · 6 von 17 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 17 Bubbles gruen (0 %) · 0 laufen · 17 offen
```

`I0013` ist vollständig bis zur Bubble geplant, in **drei** Slices — die Reihenfolge ist die der Spalte `Braucht`:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0035` Erledigungszeitpunkt festhalten | B0184–B0188 (5) | 3,2h klar + 0,4–1,5h unklar | `I0012`, `I0003` |
| `F0036` Abschlussspalte nach Datum gruppiert und gekürzt | B0189–B0195 (7) | 8,8h klar + 2–4h unklar | `F0035` |
| `F0037` Ältere Karten erreichbar | B0196–B0200 (5) | 6,4h klar + 2–4h unklar | `F0036` |

Belegt sind die sechs Migrations-, Operations- und Provider-Bubbles `B0184`, `B0185`, `B0188`, `B0189`, `B0190`, `B0196` (Vergleichswerte `B0027`, `B0028`, `B0067`, `B0070` in `Schaetzungen/_ist-zeiten.md`); die übrigen tragen Richtwerte. Die drei unklaren Bubbles haben zwei verschiedene Ursachen: `B0187` trägt die Contracts-Änderung mit sechs Aufrufstellen, `B0195` und `B0200` das Arrange-Problem der E2E-Tests (zwei verschiedene Erledigungsdaten herstellen). Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- **Wie heißen Datumsgruppen, die älter als „Gestern" sind?** — **nicht entschieden**, bewusst nicht geraten. Das Artboard zeichnet nur „Heute · 3" und „Gestern · 5" (`Dokumentation/Wireframes/D0003.dc.html:327`, `:340`); für den vorletzten und jeden früheren Tag steht keine Formatregel. Die Frage hängt an der offenen Datumsformat-Entscheidung des Projekts: `Terminformatierer.cs:17` schreibt ISO (`2026-09-05`), das Artboard zeigt deutsche Beschriftungen. **Vor `B0192` zu beantworten.** Bis dahin gilt nur, dass jede Gruppe eine Überschrift mit Zählung trägt.
- ~~Bringt dieser Slice den Erledigungszeitpunkt selbst mit?~~ — entschieden am 2026-09-04: **ja**, Migration `008-karteerledigung.sql` und Feld `Karte.ErledigtAm`. Es gibt keinen späteren Slice, dem der Träger gehörte; `I0013` ist der einzige, der nach dem Datum fragt.
- ~~Erledigungs- oder Ablagedatum?~~ — entschieden am 2026-09-04: **Erledigungsdatum**, gemessen am Eintritt in die Abschlussspalte. Ein Zug innerhalb lässt es unberührt, sonst datierte bloßes Umsortieren die Karte still um. Nicht geprüft, ob der Mensch stattdessen die **erste** Erledigung erhalten sehen möchte — heute löscht der Austritt die Zeile.
- ~~Bekommen Bestandskarten ein nachgetragenes Datum?~~ — entschieden am 2026-09-04: **nein**. Ein Datum, das die Migration erfände, wäre keins. Sie stehen in der Gruppe „Ohne Datum" am Ende und werden als Erstes gekürzt.
- ~~Wo stehen die älteren Karten für einen Agenten?~~ — entschieden am 2026-09-04: **`GET /api/boards/{boardId}/spalten/{spalteId}/karten`**. Seitenweises Nachladen (`?seite=2`) ist bewusst nicht geplant — „vollständig" sagt das Gegenteil; ob eine sehr große Abschlussspalte es später braucht, ist nicht geprüft.
- ~~Kürzt der Server oder die Oberfläche?~~ — entschieden am 2026-09-04: **der Server**, am Ausgang der Dienste. Läge die volle Liste ohnehin im Board, wäre „ältere sind über die API vollständig erreichbar" trivial erfüllt.
- ~~Wahrheitswert „gekürzt" oder wahre Kartenzahl?~~ — entschieden am 2026-09-04: **`int Kartenzahl`**. 20 Karten ohne die Auskunft, dass es 137 sind, ist die stille Lüge, die das Feld verhindert. Nicht geprüft, ob die Oberfläche die genaue Zahl später doch zeigen soll (etwa als `title` am Kopf).
- ~~Bleibt die Abschlussbahn beim Ziehen feingliedrig?~~ — entschieden am 2026-09-04: **nein**, sie nimmt ganzflächig an, das Ablegen ergibt Position 1. In einer nach Datum geordneten Bahn bezeichnet eine Fuge keine Stelle. Nicht geprüft, ob der Mensch innerhalb der Abschlussspalte weiter umsortieren möchte.
- **Woher kommt „heute"?** — angenommen am 2026-09-05: die **lokale Systemzeit der WebApi**. Die Anwendung läuft im LAN auf einer Maschine im Full-Trust-Modell; „heute" ist der Tag, den der Mensch vor dem Bildschirm meint, nicht UTC. Ob die Uhr hinter eine austauschbare Abstraktion gelegt wird, entscheidet der Entwickler beim Bauen — das E2E-Arrange braucht ohnehin einen Weg, zwei verschiedene Erledigungsdaten herzustellen (siehe Missing-Docs).
- **Überlebt das Nachladen einen Reload?** — angenommen am 2026-09-05: **nein**. Das Nachladen ist eine Handlung, keine Einstellung des Boards; nach einem Reload und nach jedem Board-Abruf ist die Bahn wieder gekürzt. Begründung wie beim Layout-Modus in `R00004`: eine Arbeitsweise, die nach dem Reload endet, darf Browserzustand sein — eine Eigenschaft des Boards nicht (`R00009`, „Verworfene Alternativen"). Nicht geprüft, ob der Mensch die Bahn dauerhaft offen halten möchte.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start der WebApi mit. Karten, die bereits in einer Abschlussspalte liegen, bekommen keine Zeile und damit kein Erledigungsdatum; sie stehen in der Gruppe „Ohne Datum" und wandern in ein Datum, sobald sie einmal die Spalte verlassen und wieder betreten.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist, dass die Abschlussspalte die einzige Bahn ist, die nie kleiner wird: seit `R00006` und `R00007` sammelt sich dort alles, was je fertig wurde, und verdrängt die Bahnen, in denen tatsächlich gearbeitet wird. Wenn jede erledigte Karte den Tag ihres Eintritts festhält, lässt sich die Bahn nach Datum ordnen und auf die neuesten kürzen — das Board bleibt lesbar, ohne dass jemand archivieren muss, und genau dieser fehlende Pflegeschritt ist der Grund, warum Kanbanflow es so und nicht mit einem Archiv-Knopf löst. Der Hebel sitzt beim **Datum** und nicht bei einer reinen Anzeigebegrenzung: eine Bahn, die einfach „die letzten 20 der Liste" zeigt, wäre nach dem ersten Umsortieren beliebig, und sie brächte keinen der zeitlichen Datenpunkte, ohne die Burndown, Durchsatz und Soll-Ist aus dem Zielbild nicht rechenbar sind. Dass die Kürzung auf dem Server sitzt und die vollständige Liste eine eigene Adresse bekommt, ist die zweite Hälfte desselben Hebels: nur so ist „ältere sind über die API vollständig erreichbar" eine Zusage mit Inhalt und nicht die Beschreibung eines Nebeneffekts — und nur so sieht ein Agent an `kartenzahl`, dass er einen Ausschnitt in der Hand hält. Vor- oder nachgelagert geht es nicht: ohne Träger für das Datum ist die Gruppierung nicht baubar, und ein späterer Umbau von „Anzeige kürzt" auf „Server kürzt" zöge Contracts, beide Dienste und jeden Test daran mit.

## Missing-Docs

- **Zwei verschiedene Erledigungsdaten in einem Playwright-Arrange.** Die E2E-Tests brauchen ein Board mit erledigten Karten aus mindestens zwei Tagen. Ob das über die Uhr des Testlaufs (gestellte Zeit im Dienstprozess), über direktes SQL auf die Testdatenbank oder über einen Testhaken geht, steht nirgends — `Testumgebung` und `Dienstprozess` starten die Prozesse heute ohne Möglichkeit, die Zeit zu setzen. Davon hängen `B0195` und `B0200` und ihre Bandbreite ab.
- **`ON CONFLICT … DO UPDATE` innerhalb einer laufenden Transaktion.** Mit `R00009` (`004-boardeinstellung.sql`) ist das Muster ins Repository gekommen; ob es sich innerhalb der Zug-Transaktion des `KartenRepository` genauso verhält, ist nicht belegt. Vor dem Bauen mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`), falls die vorhandenen Tests die Frage nicht bereits beantworten.

## Notizen

### Verworfene Alternativen

- **`ALTER TABLE Karte ADD COLUMN ErledigtAm`.** Ein Feld weniger im Modell, kein JOIN. Verworfen: in SQLite nicht idempotent, und der `Migrationslaeufer` führt jedes Skript bei jedem Start aus — dieselbe Begründung wie bei `004`, `005` und `007`.
- **Parameter `vollstaendig` an `GET /api/boards/{boardId}`.** Keine neue Route. Verworfen: ein Aufrufer müsste das ganze Board holen, um **eine** Bahn vollständig zu bekommen, und dieselbe Adresse hätte zwei Antwortgestalten. Das Muster `archiviert` an `GET /api/boards` (`R00010`) trägt hier nicht: dort war es dieselbe Frage mit zwei Ausschnitten, hier ist es eine andere Ressource.
- **Seitenweises Nachladen (`?seite=2`).** Verworfen: das Fertig-Kriterium sagt „vollständig". Eine Seitengröße einzuführen, hieße die Kürzung ein zweites Mal zu bauen, an einer Stelle, an der sie niemand verlangt hat.
- **Kürzung im Repository statt am Ausgang der Dienste.** Weniger Code. Verworfen: `KartenlageValidator` und `KartenService.KartenzahlNachDemZug` prüfen einen Zug gegen die Kartenzahl der Zielspalte — gegen eine gekürzte Liste wiesen sie gültige Züge zurück.
- **Wahrheitswert `IstGekuerzt` statt `Kartenzahl`.** Für die Anzeige genügte er, die Form `20+` nennt die Gesamtzahl gar nicht. Verworfen: für einen Agenten ist eine Liste von 20 ohne die Auskunft „es sind 137" wertlos.
- **Kürzung nur in der Oberfläche.** Kein API-Umbau. Verworfen: dann wäre „über die API vollständig erreichbar" trivial erfüllt, die Zusage inhaltsleer und `F0037` ohne Gegenstand.
- **Ein Datum für Bestandskarten nachtragen** (Anlagedatum, Migrationsdatum, „heute"). Verworfen: ein erfundenes Datum ist von einem echten nicht unterscheidbar und verdirbt genau die Auswertung, wegen der das Feld entsteht.
- **Voller Zeitstempel statt `DateOnly`.** Verworfen: gruppiert und gekürzt wird nach Tagen; innerhalb eines Tages ordnet die Position. Eine Uhrzeit erzeugte eine Genauigkeit, die niemand liest, und eine Zeitzonenfrage, die im LAN-Betrieb keine ist.
- **Ein viertes Feature „nur die N neuesten"** getrennt von der Gruppierung. Verworfen: beides ist dieselbe Rechnung über dieselbe Liste und wird in einem Zug fertig.

### Bewusst out of scope

- **Manuelles Archivieren erledigter Karten.** Die Kürzung macht es entbehrlich; ein Archiv wäre ein eigener Zustand mit eigenem Endpunkt und eigener Rückholung.
- **Auswertungen auf dem Erledigungsdatum** (Burndown, Durchsatz, Soll-Ist). Diese Anforderung legt den Datenpunkt an; wer ihn auswertet, ist ein eigener Dialog des Zielbilds.
- **Live-Übertragung an andere offene Sichten.** Wer eine Karte ablegt, sieht die neue Gruppierung sofort; ein zweiter Betrachter erst beim nächsten Laden. Das ist `I0028`, nicht dieser Slice.
- **Umsortieren innerhalb der Abschlussspalte.** Die Bahn ist nach Datum geordnet; eine Handordnung darin wäre eine zweite Ordnung über derselben Liste.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Die abgehakten Punkte unter „Offene Fragen" sind Annahmen mit Beleg aus der Planung, keine bestätigten Vorgaben; die WBS führt sie als Anmerkungen (`Dokumentation/Planung/kanbanc.md:358-367`). Zwei Annahmen sind **hier** neu getroffen und dort noch nicht vermerkt: die Herkunft von „heute" und die Flüchtigkeit des Nachladens. Die eine Frage, die im stillen Lauf **nicht** entschieden wurde, ist die Beschriftung älterer Datumsgruppen — sie steht offen und ist vor `B0192` zu beantworten.
