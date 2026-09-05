---
id: R00016
status: Neu
datum: 2026-09-05
---

# R00016: Karte archivieren

## Beschreibung

Eine Karte, die nicht mehr auf das Board gehört, bekommt über das `⋯`-Menü in ihrer Kopfzeile den Eintrag „Archivieren". Danach ist sie vom Board fort: sie fehlt in `GET /api/boards/{boardId}`, in der Antwort von `PUT …/lage` und in ihrer Bahn; die Restspalte behält lückenlose Positionen, und der Bahnenkopf nennt die wahre Zahl. Fort heißt nicht weg: dieselbe Karte steht unter der Adresse `GET /api/boards/{boardId}/spalten/{spalteId}/karten?archiviert=true` im Archiv ihrer Spalte, und dieselbe `PUT`-Route, die sie archiviert hat, holt sie zurück.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

**Diese Anforderung ist bewusst kleiner als das Fertig-Kriterium ihrer Interaction — und das darf nicht still passieren.** `I0014` sagt: „Eine archivierte Karte verschwindet vom Board, bleibt aber über API **und Archiv** auffindbar." Das Verschwinden wird hier gebaut und geprüft, und das Archiv wird als **Adresse** gebaut und geprüft. Eine **Archivsicht in der Oberfläche** — eine Liste archivierter Karten mit „zurückholen" — entsteht hier **nicht**: ein Kartenarchiv ist nirgends gezeichnet, weder als Interaction noch als Artboard, und die Projektkonvention verlangt, vor einem neuen Schirm die Skizze anzusehen (`CLAUDE.md`, „Zieldesign der Oberfläche"). Ein erfundener Schirm wäre ein Verstoß dagegen und stünde ohne Gestaltungsvorgabe da. **Die Lücke geht mit Adresse weiter:** die Archivsicht ist eine **eigene Interaction unter `D0003`** und braucht zuerst ein Artboard — `/wireframe verfeinern D0003`, danach `/planung verfeinern D0003`, danach eine eigene Anforderung. Dieselbe Behandlung wie in `R00014` für die zweite Hälfte von `I0009`. Diese Anforderung schafft dafür die Voraussetzung, die nicht nachträglich herstellbar wäre: eine Karte wird **nie gelöscht**, sondern nur archiviert und zurückgeholt, und das Archiv ist von Anfang an über die API adressierbar.

## Geschäftlicher Nutzen

Bis heute kann eine Karte das Board nur verlassen, indem sie in die Abschlussspalte wandert — und dort bündelt `R00015` sie nach Datum, statt sie fortzunehmen. Für alles, was **nicht** erledigt, sondern hinfällig ist (der Bug ist keiner, die Aufgabe entfällt, die Karte war doppelt), gibt es keinen Weg: sie bleibt in einer Arbeitsbahn stehen und verfälscht jede Zahl, die diese Bahn nennt — die Kartenzahl im Bahnenkopf, die Zählung der Zielspalte beim Zug, später jede Auswertung, die den Bestand misst. Löschen wäre die einfache Antwort und die falsche: ein gelöschter Vorgang ist in keiner Auswertung mehr nachvollziehbar, und die Vision verlangt „Auswertungen aus vollständigen Daten". Archivieren trennt beides sauber — die Karte hört auf, Bestand zu sein, und hört nicht auf, zu existieren. Für Agenten ist genau das der Unterschied zwischen einer Handlung, die sie ausführen dürfen, und einer, vor der sie zurückschrecken müssen: sie ist umkehrbar, über dieselbe Route, mit demselben DTO.

## Funktionale Anforderungen

- Eine Karte lässt sich über `PUT /api/boards/{boardId}/karten/{karteId}/archivierung` archivieren und über dieselbe Route zurückholen.
- Eine archivierte Karte erscheint nicht mehr im Bestand: nicht in `GET /api/boards/{boardId}`, nicht in der Antwort von `PUT …/lage`, nicht in `GET …/spalten/{spalteId}/karten` und nicht auf dem Board.
- Die aktiven Karten der betroffenen Spalte werden beim Archivieren auf lückenlose Positionen `1..n` verdichtet.
- Jede Zahl, die den Bestand einer Spalte nennt, meint danach die aktiven Karten: Kartenzahl der Spalte, Zahl im Bahnenkopf, Prüfung der Zielposition eines Zuges.
- Die Kopfzeile jeder Karte trägt einen `⋯`-Schalter; er öffnet ein Menü mit dem Eintrag „Archivieren" samt Erläuterung, ein zweiter Klick schließt es.
- Ein Klick auf „Archivieren" nimmt die Karte aus ihrer Bahn, ohne dass die Seite neu geladen wird.
- `GET /api/boards/{boardId}/spalten/{spalteId}/karten?archiviert=true` liefert genau die archivierten Karten der Spalte in Anzeigereihenfolge; ohne den Parameter bleibt die Adresse, was sie ist, und liefert nur die aktiven.
- Ein unlesbarer Wert des Parameters wird mit Befund zurückgewiesen; eine unbekannte oder fremde Karte am `PUT` ebenso.
- Eine zurückgeholte Karte steht wieder in ihrer Bahn, mit lückenlosen Positionen und unverändertem Erledigungsdatum.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** Die Migration `009-kartenarchivierung.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-23`). Deshalb eine eigene Tabelle statt `ALTER TABLE`, wie bei den Migrationen 004, 005, 007 und 008.
- **Umkehrbarkeit:** Es entsteht kein Löschweg. Archivieren ist eine Sichtbarkeitsaussage, keine Datenvernichtung; jede Karte, die je bestand, bleibt lesbar.
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der neuen Route und der neue 400-Fall des Karten-`GET` tragen einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"). Symbole als Inline-SVG, keine Dingbats.
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier keine Projektreferenz auf `KanbanC.BL`; das Archivieren geht über HTTP.
- **Rückwirkungsfreiheit:** Der Bestand bleibt grün, ohne dass seine Tests umgeschrieben werden — siehe Akzeptanzkriterien „Der grüne Bestand bleibt grün".

## Akzeptanzkriterien

### Archivieren und Zurückholen über die API (F0038)

- [ ] `PUT /api/boards/{boardId}/karten/{karteId}/archivierung` mit dem Rumpf `{"istArchiviert": true}` antwortet mit HTTP 200 und **den Spalten des Boards** — dieselbe Antwortgestalt wie `PUT …/lage`, weil dieselbe Wirkung eintritt.
- [ ] Nach dem Archivieren fehlt die Karte in `GET /api/boards/{boardId}`. Rechenbeispiel: Spalte mit den Karten `A`(1), `B`(2), `C`(3) → nach dem Archivieren von `B` liefert die Spalte zwei Karten.
- [ ] Die verbleibenden Karten der Spalte tragen lückenlose Positionen `1..n`. Rechenbeispiel: `A`(1), `B`(2), `C`(3), `B` archiviert → `A` hat Position 1, `C` hat Position 2 (nicht 3).
- [ ] Die Karten anderer Spalten bleiben in Spalte und Position unverändert.
- [ ] `PUT …/archivierung` mit `{"istArchiviert": false}` holt die Karte zurück; sie steht danach wieder in ihrer **alten** Spalte, und die Positionen der Spalte sind erneut lückenlos.
- [ ] Ein zweites Archivieren derselben, bereits archivierten Karte ist kein Fehler und ändert nichts; ein Zurückholen einer nicht archivierten Karte ebenso. Die Route ist ein Umschalter auf einen Zielzustand, kein Ereignis.
- [ ] Eine **unbekannte** `karteId` beantwortet die Route mit HTTP 404 **und einem Rumpf**: mindestens ein Befund mit nichtleerem `code`, einer `meldung`, welche die aufgerufenen Nummern nennt, und einer `kompensation` mit einem ausführbaren nächsten Aufruf.
- [ ] Eine Karte, die es gibt, aber zu einem **anderen** Board gehört, beantwortet die Route ebenso mit HTTP 404 und einem Befund, der das tatsächliche Board der Karte nennt.
- [ ] Der Vertragstest über alle registrierten Routen bleibt grün: die neue Route wird von ihm abgerufen und ist nicht als ungeprüft übrig.
- [ ] Eine zurückgewiesene Archivierung schreibt nichts: nach einem 404 sind Positionen, Spaltenzugehörigkeit und Archivstand aller Karten unverändert.
- [ ] Das Erledigungsdatum bleibt unberührt: eine Karte in der Abschlussspalte behält `erledigtAm` beim Archivieren und beim Zurückholen. Archivieren ist kein Austritt aus der Abschlussspalte.
- [ ] Ein zweiter Lauf der Migration auf einer bestehenden Datei lässt Schema **und** Daten unverändert; ein gesetzter Archivstand bleibt stehen.
- [ ] Nach einem Neustart der WebApi auf derselben Datei ist die archivierte Karte weiterhin archiviert.

### Die archivierte Karte ist kein Bestand mehr (F0038)

- [ ] Die Antwort von `PUT /api/boards/{boardId}/karten/{karteId}/lage` enthält die archivierte Karte nicht.
- [ ] `GET /api/boards/{boardId}/spalten/{spalteId}/karten` **ohne** Parameter enthält die archivierte Karte nicht.
- [ ] Das Feld `kartenzahl` einer Spalte zählt nur die aktiven Karten. Rechenbeispiel: Spalte mit 3 Karten, eine archiviert → `kartenzahl` ist 2.
- [ ] Bei eingeschalteter Kartenzahl zeigt der Bahnenkopf die Zahl der aktiven Karten. Rechenbeispiel: Bahn mit 3 Karten zeigt `3`; nach dem Archivieren einer Karte zeigt sie `2`.
- [ ] Eine Zielposition wird gegen die **aktiven** Karten der Zielspalte geprüft. Rechenbeispiel: Zielspalte mit 3 Karten, eine davon archiviert → ein Zug auf Position 3 wird angenommen, ein Zug auf Position 4 zurückgewiesen.
- [ ] Eine neu angelegte Karte bekommt die nächste Position **nach der letzten aktiven** Karte. Rechenbeispiel: Spalte mit `A`(1), `B`(2), `B` archiviert und die Spalte auf `A`(1) verdichtet → die neue Karte bekommt Position 2.
- [ ] Eine archivierte Karte ist kein gültiges Zugziel und kein Zugobjekt: `PUT …/lage` auf eine archivierte Karte oder mit ihr als Bezugspunkt verhält sich wie bei einer nicht vorhandenen Karte.
- [ ] Die Kürzung der Abschlussspalte rechnet auf den aktiven Karten. Rechenbeispiel: 21 erledigte Karten bei Anzeigegrenze 20, eine archiviert → die Bahn ist nicht mehr gekürzt und der Kopf zeigt `20` statt `20+`.

### Archivieren in der Oberfläche (F0038)

- [ ] Jede Karte auf dem Board trägt in ihrer Kopfzeile einen `⋯`-Schalter.
- [ ] Ein Klick darauf öffnet ein Menü mit genau einem Eintrag „Archivieren" und einer Erläuterung darunter; ein zweiter Klick auf den Schalter schließt es.
- [ ] Der Klick auf den `⋯`-Schalter löst **keinen** Ziehvorgang der Karte aus; das Menü liegt sichtbar über der Karte und über den beiden Ablagezonen, nicht dahinter.
- [ ] Ein Klick auf „Archivieren" nimmt die Karte aus ihrer Bahn, ohne dass die Seite neu geladen wird; die übrigen Bahnen werden aus derselben Antwort neu gezeichnet, ein zweiter Abruf findet nicht statt.
- [ ] Nach einem Reload ist die Karte weiterhin fort.
- [ ] Ist die WebApi beim Archivieren nicht erreichbar, erscheint eine lesbare Ausfallmeldung statt einer Ausnahmeseite; das Board bleibt bedienbar und die Karte steht sichtbar an ihrer alten Stelle.
- [ ] Die Karte bleibt ziehbar wie zuvor: Kartenhälften, Einfügelinie und Zielposition verhalten sich unverändert (`R00008`).

### Das Archiv der Spalte (F0039)

- [ ] `GET /api/boards/{boardId}/spalten/{spalteId}/karten?archiviert=true` antwortet mit HTTP 200 und **genau** den archivierten Karten dieser Spalte, in Anzeigereihenfolge.
- [ ] Dieselbe Adresse **ohne** Parameter liefert unverändert nur die aktiven Karten — vollständig und ungekürzt, wie seit `R00015`.
- [ ] `?archiviert=false` ist gleichbedeutend mit dem Weglassen des Parameters.
- [ ] Hat eine Spalte keine archivierten Karten, antwortet die Adresse mit HTTP 200 und einer leeren Liste — nicht mit 404.
- [ ] Ein unlesbarer Wert (`?archiviert=vielleicht`) wird mit HTTP 400 **und einem Rumpf** zurückgewiesen: Code, Meldung mit dem aufgerufenen Wert, und eine Kompensation, die **diese** Adresse nennt — nicht `GET /api/boards`.
- [ ] Der bestehende 400-Fall an `GET /api/boards?archiviert=…` nennt weiterhin `GET /api/boards` in seiner Kompensation. Jede der beiden Adressen erklärt sich selbst.
- [ ] Eine unbekannte `boardId` oder eine unbekannte bzw. fremde `spalteId` beantwortet die Adresse weiterhin mit HTTP 404 und Rumpf — mit und ohne Parameter.
- [ ] Die archivierten Karten tragen ihr `erledigtAm` wie die aktiven.
- [ ] Rundlauf: archivieren → die Karte steht im Archiv der Spalte und nicht im Board → zurückholen → sie steht wieder im Board an lückenloser Position und nicht mehr im Archiv.

### Der grüne Bestand bleibt grün

- [ ] `GekuerzteAbschlussspalteTests` bleibt **ohne Änderung** grün — alle acht Zusicherungen auf `Kartenzahl` und `Karten.Count`.
- [ ] `KartenzahlImBahnenkopfE2ETests` und `BahnenkopfzahlTests` bleiben ohne Änderung grün; die exakten Zahlen (`3`, `1`, `0`) stehen weiterhin.
- [ ] `KarteVerschiebenE2ETests`, `EinfuegelinieE2ETests` und `AbschlussbahnAblageE2ETests` bleiben ohne Änderung grün: sie zählen Karten, Kartenhälften und ziehbare Karten, und die neue Kopfzeile darf keine dieser Zählungen verschieben.
- [ ] `Abschlussbahn.Gekuerzt` bleibt unverändert: es gibt keinen Archivbegriff in der Kürzung.
- [ ] Die Aufrufstelle `BoardEndpunkte.LadeAlleBoards` verhält sich unverändert, obwohl `Archivfilter` die Route nun als Parameter bekommt.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/009-kartenarchivierung.sql` — neue, idempotente Migration.
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Boards/Archivfilter.cs` — die Kompensationsmeldung bekommt die aufrufende Route als Parameter statt der festen Konstante `GET /api/boards`.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (beide Lesewege filtern archivierte Karten aus; `LiesKartenEinerSpalte` bekommt den Archivstand als Eingang), `Source/KanbanC.BL/Persistenz/Karten/KartenRepository.cs` (Schreiben und Löschen des Archivstands samt Verdichtung in einer Transaktion; `KarteIdsNachPosition` und `NaechstePosition` sehen nur aktive Karten; `LadeKartenDerSpalte` bekommt den Archivstand durchgereicht), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `SchalteArchivierung` und der Archivstand an `LadeKartenDerSpalte`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — eine neue `PUT`-Route unterhalb der Kartenadresse und der Abfrageparameter an der bestehenden `GET`-Basisroute.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`SchalteArchivierung`), `Source/KanbanC.Blazor/Components/Karten/Karte.razor` (+ `.razor.css`: Kopfzeile, `⋯`-Schalter, Menü), `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (das Archivieren auslösen und die Antwort übernehmen).
- **Contracts:** **keine Änderung.** `KanbanC.Contracts.Boards.Archivierung` und `KanbanC.Contracts.Karten.Karte` bleiben, wie sie sind.
- **Tests:** `Source/KanbanC.BL.Tests/Integrations/` samt `TestHelpers/TestKartenRepository.cs`, `Source/KanbanC.BL.Tests/Operations/Boards/ArchivfilterTests.cs`, `Source/KanbanC.Blazor.Tests/Services/KartenApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/` (`Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/KartenAmBoardTests.cs`, `Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (Seitenobjekt `BoardSeite` und eine neue Testklasse).
- **Unberührt:** `Source/KanbanC.BL/Operations/Karten/Abschlussbahn.cs`, `KartenlageValidator.cs`, `Source/KanbanC.BL/Operations/Karten/Erledigungsstand.cs` und `Source/KanbanC.BL/Persistenz/Boards/Spaltenleser.cs` — sie bekommen ihre richtigen Zahlen dadurch, dass der Leser unter ihnen filtert, nicht durch einen eigenen Eingriff.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0003.dc.html`](../Dokumentation/Wireframes/D0003.dc.html), **die Karte mit offenem Kartenmenü** (Zeilen 163–179), ist die Gestaltungsvorgabe. Daraus gelten für diese Anforderung drei Stellen: die Kartenkopfzeile mit dem `⋯`-Schalter rechts als Inline-SVG, das Menü als Auflage über der Karte mit genau einem Eintrag „Archivieren" nebst Symbol, und die Erläuterungszeile darunter. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Diese Anforderung beantwortet zugleich die offene Frage 6 des Wireframe-Index** (`Dokumentation/Wireframes/_wireframes.md:283-287`), die seit `R00006` als „gesetzt, nicht abgestimmt" geführt wird: das Bedienelement ist das **`⋯`-Menü auf der Karte**, nicht das Archivieren erst im Kartendetail. Die Alternative ist in diesem Slice nicht baubar — `D0004` (Kartendetail) ist vollständig rot; `I0014` hätte auf dem Board kein Bedienelement, „verschwindet vom Board" wäre nicht durch die Oberfläche prüfbar und der Slice nicht abschließbar. Die Begründung des Index (`:129`) gilt unverändert: „ein Menü mit einem Eintrag ist ehrlicher, als Einträge aus `D0004` dazuzuerfinden." Kommt `D0004`, bekommt das Kartendetail denselben Eintrag **zusätzlich**; das Menü auf der Karte bleibt. Der Index wird von dieser Anforderung nicht geändert — `/wireframe` schreibt ihn, nicht `/anforderung`.

### Ablauf

1. **Archivieren** (`PUT /api/boards/{boardId}/karten/{karteId}/archivierung`)
   - 1.1 `KartenEndpunkte` nimmt `Archivierung` als Rumpf entgegen und ruft `KartenService.SchalteArchivierung(boardId, karteId, archivierung)`
   - 1.2 `KartenService` verdrahtet ohne Validator — ein Wahrheitswert hat keinen ungültigen Fall
     - 1.2.1 `KartenRepository.SetzeArchivierung` liefert `null`, wenn die Karte unbekannt oder fremd ist → `BefundZurFehlendenKarte` wählt zwischen `Nichtgefunden.Karte` und `Nichtgefunden.FremdeKarte` → 404 mit Rumpf
   - 1.3 `KartenRepository.SetzeArchivierung` in **einer** Transaktion:
     - 1.3.1 `istArchiviert = true` → `INSERT INTO Kartenarchivierung … ON CONFLICT DO NOTHING`; `false` → `DELETE`
     - 1.3.2 die aktiven Karten der Spalte auf `1..n` verdichten — dieselbe Routine wie beim Zug (`SchreibeOrdnung`, `KartenRepository.cs:233-244`)
     - 1.3.3 Rücklesen über `Kartenleser` und `Spaltenleser`, Commit
   - 1.4 `KartenService` gibt die Spalten in derselben gekürzten Gestalt zurück wie `PUT …/lage` → HTTP 200
2. **Der Bestand sieht die Karte nicht mehr**
   - 2.1 `Kartenleser.LiesKartenNachPosition` und `.LiesKartenEinerSpalte` schließen archivierte Karten aus (`LEFT JOIN Kartenarchivierung … WHERE a.Karte IS NULL`)
   - 2.2 `KartenRepository.KarteIdsNachPosition` und `.NaechstePosition` ebenso — sonst zählte die Anlage über eine archivierte Zeile hinweg
   - 2.3 `Spalte.Kartenzahl`, `Bahnenkopfzahl`, `KartenService.KartenzahlNachDemZug` und `KartenlageValidator` stimmen dadurch **ohne eigenen Eingriff**
3. **Archiv lesen** (`GET /api/boards/{boardId}/spalten/{spalteId}/karten?archiviert=true`)
   - 3.1 `Archivfilter.Aus(archiviert, route)` liefert `Archivierung` oder eine Zurückweisung mit Befund → 400
   - 3.2 `KartenService.LadeKartenDerSpalte(boardId, spalteId, archivstand)` prüft wie bisher erst das Board, dann die Spalte
   - 3.3 `KartenRepository.LadeKartenDerSpalte` reicht den Archivstand an den Leser durch; die Abfrage kehrt ihre `WHERE`-Bedingung um
4. **Archivieren in der Oberfläche**
   - 4.1 Klick auf `⋯` → `Karte.razor` schaltet den Menüzustand um; das Ereignis darf `@ondragstart` des `<article>` nicht auslösen
   - 4.2 Klick auf „Archivieren" → Rückmeldung an `Spaltenbahnen.razor` → `KartenApiKlient.SchalteArchivierung`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 4.3 Erfolg → die gelieferten Spalten sind die neue Quelle der Anzeige; kein zweiter Board-Abruf
   - 4.4 `HttpRequestException` → Ausfallmeldung, das Board bleibt bedienbar

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — die neue `PUT`-Route sitzt als Unterressource der Kartenadresse neben `/lage` (`KartenEndpunkte.cs:14`); das Muster für den Namen `…/archivierung` liefert das Board (`BoardEndpunkte.cs:19`).
- **`Kartenleser`** — der eine Ort, an dem entschieden wird, was als Bestand gilt. Ein Filter hier trifft Kartenzahl, Bahnenkopf und Zugprüfung in einem Zug.
- **`Migrationslaeufer`** — die neunte Migration reiht sich ein; kein Journal, also idempotent.
- **`Karte.razor`** — die Karte bekommt erstmals eine Kopfzeile und damit eine eigene Aktion.

**Klassen-Entwurf:**

- `IKartenRepository` / `KartenRepository` (Provider, Ressourcenzugriff) — Archivstand schreiben und die Spalte verdichten; `null` heißt „diese Karte gibt es an dieser Stelle nicht".
  - `IReadOnlyList<Spalte>? SetzeArchivierung(long boardId, long karteId, Archivierung archivierung)`
  - `IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand)`
- `KartenService` (Integration) — verdrahtet beides; kein Validator, weil ein Wahrheitswert keinen ungültigen Fall hat (wie `B0130`).
  - `Ergebnis<IReadOnlyList<Spalte>> SchalteArchivierung(long boardId, long karteId, Archivierung archivierung)`
  - `Ergebnis<IReadOnlyList<Karte>> LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand)`
- `Archivfilter` (Operation, pure Logik) — die Route wandert von der Konstante in den Parameter, damit die Kompensation die Adresse nennt, die der Aufrufer wirklich gerufen hat.
  - `static Ergebnis<Archivierung> Aus(string? abfragewert, string route)`
- `KartenApiKlient` (Integration, Blazor) — der HTTP-Weg des Archivierens; Muster `BoardApiKlient.SchalteArchivierung` (`BoardApiKlient.cs:69`).
  - `public Task<ApiErgebnis<IReadOnlyList<Spalte>>> SchalteArchivierung(long boardId, long karteId, Archivierung archivierung)`
- `Karte` (Blazor-Komponente) — bekommt eine Kopfzeile mit `⋯`-Schalter, einen Menüzustand und einen Rückkanal.
  - `[Parameter] public EventCallback<long> ArchivierungGewuenscht { get; set; }`
- **Migration** `009-kartenarchivierung.sql` (Skript, idempotent) — eine Zeile je archivierter Karte; der Fremdschlüssel **ist** der Schlüssel, sonst trüge eine Karte zwei Archivstände:
  ```sql
  CREATE TABLE IF NOT EXISTS Kartenarchivierung
  (
      Karte INTEGER PRIMARY KEY REFERENCES Karte (KarteId)
  );
  ```
  Kein `ArchiviertAm`: das Artboard zeichnet keins. Gegenprobe `B0170` — bei der Kontributor-Stilllegung kam das Datum, weil die gezeichnete Zeile „stillgelegt seit 12.08.2026" trug.

### Änderungen an bestehenden Klassen

- `Kartenleser` (`:14-20` und `:33-38`) — beide Abfragen bekommen den Ausschluss archivierter Karten; `LiesKartenEinerSpalte` bekommt den Archivstand als Eingang und kehrt die Bedingung für `?archiviert=true` um. **Änderung an grünem Bestand.**
- `KartenRepository` (`:223-231` `KarteIdsNachPosition`, `:256-262` `NaechstePosition`) — beide sehen nur aktive Karten. Ohne das bliebe beim Verdichten eine Lücke, und `NaechstePosition` zählte über die archivierte Zeile hinweg. **Änderung an grünem Bestand.**
- `Archivfilter` (`:12` Konstante, `:38` Kompensationstext) — die Route wird Parameter; die Aufrufstelle `BoardEndpunkte.LadeAlleBoards` (`:37`) zieht mit und übergibt weiterhin `GET /api/boards`. **Änderung an grünem Bestand**, sichtbar in `ArchivfilterTests`.
- `KartenEndpunkte` — die `GET`-Basisroute nimmt `string? archiviert` entgegen; die neue `PUT`-Route kommt hinzu. **Der 404-Vertragsfall der neuen Route gehört in denselben Arbeitsgang wie die Route**: `FehlervertragTests.cs:53-56` liest die registrierten Routen aus dem Testhost, und zwischen Route und Vertragsfall ist die Suite rot (Lehre aus `B0152`/`B0159`).
- `Karte.razor` (`:1-21`) und `Karte.razor.css` (`:32-37`) — die Karte bekommt eine Kopfzeile und ein Menü. Zwei Fallstricke, beide schon einmal aufgetreten: der `⋯`-Schalter darf den `@ondragstart` des `<article>` **nicht** auslösen, und das Menü braucht einen `z-index` über den beiden `.kartenhaelfte`-Auflagen — derselbe Stapelkontext-Konflikt wie bei `B0123` (Boardkachel), der dort im E2E-Test tatsächlich zuschlug.
- `Spaltenbahnen.razor` (`:123-146`) — reicht den neuen Rückkanal an beide `Karte`-Einbettungen durch (gewöhnliche Bahn und Abschlussbahn) und übernimmt die gelieferten Spalten.
- `TestKartenRepository`, `BoardSeite` (`:54-58`, Locator für `⋯`-Schalter und Menüpunkt) — je um das Nötige erweitert.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Archivfilter.Aus` — ohne Parameter die aktiven, `true`/`false` gelesen, unlesbarer Wert zurückgewiesen; die **Kompensation nennt die übergebene Route** und für `GET /api/boards` unverändert die alte.
- `KartenService.SchalteArchivierung` gegen `TestKartenRepository` — Erfolg reicht die Spalten durch; unbekannte Karte liefert `Nichtgefunden.Karte`, fremde Karte `Nichtgefunden.FremdeKarte`, beide mit nichtleerem Code, Meldung und Kompensation.
- `KartenService.LadeKartenDerSpalte` mit Archivstand — unbekanntes Board und fremde Spalte liefern die Zurückweisung **ohne** Lesezugriff auf die Karten.
- `KartenApiKlient.SchalteArchivierung` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert die Spalten, 404 die Zurückweisung mit Befund; Methode, Adresse und Rumpf des abgesetzten Aufrufs werden mitgeprüft. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository` gegen eine `TemporaereDatenbank` — archivieren, zweimal archivieren, zurückholen, Verdichtung auf `1..n`, andere Spalten unberührt, `null` bei unbekannter und fremder Karte, alles in einer Transaktion; `LadeKartenDerSpalte` mit beiden Archivständen; `NaechstePosition` und `KarteIdsNachPosition` ohne archivierte Karten. `Migrationslaeufer` — zweiter Lauf lässt Schema und Archivstände unverändert. `KartenEndpunkte` über `TestWebApi` — `PUT …/archivierung` mit 200 und den Spalten, 404 mit Rumpf für unbekannte und fremde Karte; `GET …?archiviert=true` mit genau den archivierten Karten, ohne Parameter mit den aktiven, 400 mit Rumpf bei unlesbarem Wert; danach `GET /api/boards/{boardId}` ohne die Karte und mit lückenlosen Positionen; Rundlauf archivieren → Archiv → zurückholen mit unverändertem `erledigtAm`. `FehlervertragTests` — die neue Route wird abgerufen. `WebApiNeustartTests` — der Archivstand übersteht den Neustart. `GekuerzteAbschlussspalteTests` und `BahnenkopfzahlTests` laufen **unverändert** mit.

**E2E:** Ein Board mit Karten in zwei Bahnen; das `⋯`-Menü öffnen und wieder schließen (US-1); archivieren, die Karte ist aus der Bahn fort, die Bahnenkopfzahl ist um eins kleiner, nach einem Reload ist sie weiterhin fort (US-1); der Agent findet dieselbe Karte über das Archiv der Spalte, während die Oberfläche sie nicht zeigt (US-2); Ziehen und Ablegen der übrigen Karten funktionieren nach dem Einbau der Kopfzeile unverändert (US-3). Dazu laufen `KarteVerschiebenE2ETests`, `EinfuegelinieE2ETests`, `AbschlussbahnAblageE2ETests` und `KartenzahlImBahnenkopfE2ETests` sowie alle übrigen E2E-Tests aus `R00001`–`R00015` **ohne Änderung** weiter — das ist die eigentliche Gegenprobe des Slice.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: `R00006` (Karte anlegen und sehen — `I0011`, grün). Das ist die einzige Vorbedingung, die die WBS-Spalte `Braucht` von `I0014` führt; sie ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: `R00015` (`I0013`, grün — der Slice ändert deren `Kartenleser`, die Kartenzahl der Spalte und die Adresse `GET …/spalten/{spalteId}/karten`), `R00007` (Fehlervertrag und `Nichtgefunden.Karte`/`FremdeKarte`, `KartenlageValidator`), `R00008` (Einfügelinie — die neue Kopfzeile darf die Kartenhälften nicht verschieben), `R00009` (Bahnenkopfzahl), `R00010` (Board archivieren — Muster der Route, der Tabelle und des DTOs `Archivierung`).
- Blockiert: **keinen** Knoten außerhalb dieses Slice — kein anderer Slice der WBS nennt `I0014`, `F0038` oder `F0039` in seiner Spalte `Braucht` (geprüft am 2026-09-05 über `Dokumentation/Planung/kanbanc.md`).
- Reihenfolge innerhalb der Anforderung: `F0038` → `F0039`; so führt es die Spalte `Braucht`. Ohne Archivstand gibt es kein Archiv zu lesen.
- **Nachgelagert mit Adresse:** die Archivsicht in der Oberfläche — eigene Interaction unter `D0003`, zuerst `/wireframe verfeinern D0003`, dann `/planung verfeinern D0003`.

## Umfang

```
Karte archivieren (I0014) = 12 Bubbles: 10 Standard (13,6h), 2 unklar (2,4–5,5h).
Rest: 13,6h klar + 2,4–5,5h unklar · 4 von 12 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0014` ist vollständig bis zur Bubble geplant, in **zwei** Slices — die Reihenfolge ist die der Spalte `Braucht`:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0038` Karte archivieren | B0201–B0209 (9) | 10,8h klar + 2,4–5,5h unklar | `I0011` |
| `F0039` Archiv der Spalte | B0210–B0212 (3) | 2,8h klar | `F0038` |

Belegt sind die vier Migrations-, Service- und Provider-Bubbles `B0201`, `B0204`, `B0210`, `B0212` (Vergleichswerte `B0184`, `B0029`, `B0196`, `B0188`); die übrigen tragen Richtwerte. Die zwei unklaren Bubbles haben verschiedene Ursachen: `B0202` trägt vier Abfragen an drei Stellen, `B0209` die Gegenprobe über vier grüne E2E-Suiten, deren Umfang erst beim Bauen sichtbar wird. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- **Soll das Menü sich beim Klick daneben schließen?** — **nicht entschieden**, bewusst nicht geraten. Gebaut wird zunächst wie bei `B0123` (Boardkachel): nur der zweite Klick auf den `⋯`-Schalter schließt. Ein Schließen per Klick daneben braucht einen globalen Klick-Horcher, den es im Bestand nirgends gibt — das wäre eine eigene Entscheidung, keine Nebensache dieses Slice. **Vor `B0207` zu beantworten**, falls die Antwort „ja" lautet.
- ~~Wo sitzt das Bedienelement?~~ — entschieden am 2026-09-05: **`⋯`-Menü auf der Karte**, nicht das Kartendetail `D0004`. Damit ist Frage 6 des Wireframe-Index (`_wireframes.md:283-287`) beantwortet. `D0004` ist vollständig rot; ohne Bedienelement auf dem Board wäre der Slice weder bedienbar noch prüfbar noch abschließbar. Kommt `D0004`, bekommt es denselben Eintrag zusätzlich.
- ~~Was heißt „über Archiv auffindbar"?~~ — entschieden am 2026-09-05: **eine Adresse, kein Schirm** — `GET /api/boards/{boardId}/spalten/{spalteId}/karten?archiviert=true`. Ein Kartenarchiv ist nirgends gezeichnet; ein erfundener Schirm verstieße gegen die Wireframe-Konvention des Projekts. Die Archivsicht der Oberfläche ist eine eigene Interaction mit eigener Adresse (siehe Beschreibung und „Abhängigkeiten").
- ~~Zeigt das Karten-GET archivierte Karten mit?~~ — entschieden am 2026-09-05: **nein**, es bleibt ohne Parameter, was es seit `R00015` ist. Die Gegenlesart wurde verworfen: dann brächte „Ältere nachladen" archivierte Karten in genau die Bahn zurück, aus der sie eben verschwanden, und „über Archiv auffindbar" wäre nur eine Wiederholung von „über API auffindbar".
- ~~Eigene Tabelle oder Feld an `Karte`?~~ — entschieden am 2026-09-05: **eigene Tabelle `Kartenarchivierung` ohne Datum**, Muster `Boardarchivierung`. Kein `IstArchiviert` an `Karte`: das DTO kommt in drei gefilterten Antworten zurück und trüge dort überall denselben Wert — tote Flexibilität (C17), dasselbe Argument wie bei `B0127`. **Nicht geprüft**, ob eine spätere Auswertung (`D0009`) nach dem Archivierungsdatum fragt; dann kommt es über eine eigene Anforderung, statt nachträglich erfunden zu werden.
- ~~Wo sitzt der Archivfilter?~~ — entschieden am 2026-09-05: **im `Kartenleser`**, nicht am Ausgang der Dienste — umgekehrt zu `R00015` (`B0191`). Dort durfte die Kürzung nicht ins Repository, weil sie eine Anzeigesache ist und der Validator gegen den vollen Bestand rechnen muss. Hier ist es keine Anzeigesache: eine archivierte Karte ist kein Bestand, kein gültiges Zugziel und kein Zählglied.
- ~~Was wird aus der Spalte und der Position der archivierten Karte?~~ — entschieden am 2026-09-05: die Karte **behält ihre Spalte** (`Karte.Spalte` ist Pflichtfremdschlüssel, und die Spaltenbindung macht das Archiv adressierbar); die **aktiven** Karten werden auf `1..n` verdichtet; die archivierte Zeile behält ihre alte Positionszahl als bedeutungslosen Rest — sie ordnet das Archiv, sie behauptet nichts.
- ~~Verliert eine archivierte Karte ihr Erledigungsdatum?~~ — entschieden am 2026-09-05: **nein.** Archivieren ist kein Austritt aus der Abschlussspalte; „erledigt am" ist eine Aussage über die Karte, nicht über ihre Sichtbarkeit, und die Auswertungen aus `D0009` fragen genau danach. **Nicht geprüft**, ob der Mensch das Archivieren als Abschluss der Erledigung verstanden haben will.
- ~~Was liefert `PUT …/archivierung` zurück?~~ — entschieden am 2026-09-05: **die Spalten des Boards**, wie `PUT …/lage`, weil dieselbe Wirkung eintritt: die Spalte verliert eine Karte und wird neu durchnummeriert. Am Board liefert dieselbe Route das `Board`, weil dort nichts mitwandert. **Nicht geprüft**, ob ein Agent lieber die archivierte Karte zurückbekäme.
- ~~Braucht es ein zweites `Archivierung`-DTO für Karten?~~ — entschieden am 2026-09-05: **nein**, `KanbanC.Contracts.Boards.Archivierung` wird wiederverwendet — ein Begriff, eine Schreibweise (C06), und auf der Leitung steht in beiden Fällen `{"istArchiviert": true}`. Der Ordner `Boards/` beschreibt damit die Herkunft, nicht den Geltungsbereich. **Nicht geprüft**, ob der Mensch den Umzug in einen ordnerfreien Contracts-Namensraum trotzdem will; er fasst mehrere grüne Dateien an, ohne die Leitung zu ändern.
- **Bleibt das `⋯`-Menü auch an einer Karte der Abschlussbahn sichtbar?** — angenommen am 2026-09-05: **ja.** Die Abschlussbahn nimmt beim Ziehen ganzflächig an (`R00015`), aber ihre Karten sind gewöhnliche Karten; eine Kopfzeile, die dort fehlte, wäre eine Sonderregel ohne Kriterium. **Nicht geprüft**, ob der Mensch erledigte Karten lieber nur über das Datum verschwinden lässt.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start der WebApi mit. Bestehende Karten bekommen keine Zeile in `Kartenarchivierung` und sind damit alle aktiv — der sichtbare Zustand vor der Anforderung bleibt der sichtbare Zustand danach.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist, dass eine Karte das Board heute nur über die Abschlussspalte verlassen kann: alles, was hinfällig wird statt fertig, bleibt in einer Arbeitsbahn stehen und verfälscht jede Zahl, die diese Bahn nennt. Wenn eine Karte einen Archivstand bekommt und der Leser des Bestands ihn auswertet, verschwindet sie in einem Zug aus allem, was „Bestand" heißt — Kartenzahl der Spalte, Zahl im Bahnenkopf, Prüfung der Zielposition, Kürzung der Abschlussbahn —, ohne dass eine dieser vier Rechnungen einen eigenen Archivbegriff lernen muss. Genau darin sitzt der Hebel: filterte man am Ausgang der Dienste, wie es `R00015` für die Kürzung richtigerweise tut, müsste jede dieser Stellen einzeln nachgezogen werden, und die erste vergessene wäre eine stille Falschzahl. Dass das Archiv dabei eine **Adresse** bekommt statt eines Schirms, ist die zweite Hälfte desselben Hebels: nur so ist „bleibt über Archiv auffindbar" eine Zusage mit Inhalt, nur so kann ein Agent zurückholen, was er archiviert hat, und nur so entsteht kein gezeichneter Schirm, den niemand entworfen hat. Vor- oder nachgelagert geht es nicht: ohne Träger für den Archivstand ist nichts prüfbar, und ein späteres Umhängen des Filters vom Leser an die Dienste zöge vier Rechnungen und jeden Test daran mit.

## Missing-Docs

- **`ON CONFLICT DO NOTHING` auf einer Ein-Spalten-Tabelle mit Fremdschlüssel-Primärschlüssel.** Mit `R00010` (`005-boardarchivierung.sql`) und `R00015` (`008-karteerledigung.sql`) ist das Muster im Repository, dort aber jeweils mit einer Nutzspalte. Ob ein `INSERT` auf eine Tabelle **ohne** weitere Spalte in SQLite dieselbe Form nimmt, ist nicht belegt. Vor `B0203` mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`), falls die vorhandenen Tests die Frage nicht bereits beantworten.
- **Klick auf ein Kind eines `draggable`-Elements in Blazor.** Ob `@onclick:stopPropagation` genügt, damit der `⋯`-Schalter den `@ondragstart` des `<article>` nicht auslöst, oder ob das Element zusätzlich `draggable="false"` tragen muss, steht nirgends. Der verwandte Fall `B0123` betraf einen Stapelkontext, nicht das Zieh-Ereignis. Betrifft `B0207`.

## Notizen

### Verworfene Alternativen

- **`DELETE /api/boards/{boardId}/karten/{karteId}`.** Die einfachste Lösung: eine Zeile weg, kein zweites Konzept. Verworfen: eine gelöschte Karte ist in keiner Auswertung mehr nachvollziehbar, und die Vision verlangt „Auswertungen aus vollständigen Daten". Löschen wäre außerdem die Handlung, die das Zurückholen für immer unmöglich machte.
- **`ALTER TABLE Karte ADD COLUMN IstArchiviert`.** Ein JOIN weniger. Verworfen: in SQLite nicht idempotent, und der `Migrationslaeufer` führt jedes Skript bei jedem Start aus — dieselbe Begründung wie bei `004`, `005`, `007` und `008`.
- **Feld `IstArchiviert` am Contract `Karte`.** Verworfen: `Karte` kommt in `GET /api/boards/{boardId}`, im Karten-`GET` und in `PUT …/lage` zurück, und in jeder dieser gefilterten Antworten trüge das Feld denselben Wert — tote Flexibilität (C17). Anders als bei `R00014` zeigt kein Schirm aktive und archivierte Karten gemeinsam.
- **Tabelle `Kartenarchivierung` mit `ArchiviertAm`.** Verworfen: das Artboard zeichnet kein Datum. Gegenprobe `R00014`, wo das Datum aufgenommen wurde, weil die gezeichnete Zeile „stillgelegt seit 12.08.2026" trug. Fragt eine spätere Auswertung danach, kommt es über eine eigene Anforderung.
- **Eigene Route `GET /api/boards/{boardId}/spalten/{spalteId}/archiv`.** Verworfen: es ist dieselbe Ressource mit zwei Ausschnitten — genau die Lage, für die `R00010` den Parameter `archiviert` gewählt hat. Eine zweite Route hätte dieselbe Antwortgestalt an zwei Adressen.
- **Filter am Ausgang der Dienste statt im `Kartenleser`.** Symmetrisch zur Kürzung aus `R00015`. Verworfen: dort ist die Kürzung eine Anzeigesache und der Validator muss gegen den vollen Bestand rechnen; hier ist eine archivierte Karte kein Bestand — ein Filter am Ausgang ließe `Spalte.Kartenzahl`, `Bahnenkopfzahl`, `KartenzahlNachDemZug` und `KartenlageValidator` mit falschen Zahlen zurück.
- **Die archivierte Karte aus ihrer Spalte lösen** (`Karte.Spalte` auf `NULL`). Verworfen: `Karte.Spalte` ist Pflichtfremdschlüssel, ein spaltenloses Kartenmodell wäre ein Umbau, den kein Kriterium verlangt — und ohne Spalte hätte das Archiv keine Adresse.
- **Ein drittes Feature „Karte zurückholen".** Verworfen: dieselbe Route ist der Umschalter; das Zurückholen entsteht mit ihr und wird von `B0212` als Rundlauf belegt.
- **Zweiter Contracts-Record `Kartenarchivierung(bool IstArchiviert)`.** Verworfen: ein Begriff, eine Schreibweise (C06); auf der Leitung stünde zeichengleich dasselbe JSON.

### Bewusst out of scope

- **Eine Archivsicht in der Oberfläche.** Liste der archivierten Karten einer Spalte oder eines Boards, mit „zurückholen" am Eintrag. **Adresse:** eigene Interaction unter `D0003`; zuerst `/wireframe verfeinern D0003` (das Artboard fehlt), dann `/planung verfeinern D0003`, dann eine eigene Anforderung. Diese Anforderung baut die Voraussetzung dafür vollständig: Archivstand, Archivadresse und Rückholweg stehen.
- **Ein Board-weites Archiv** (`GET /api/boards/{boardId}/archiv`). Das Archiv hängt an der Spalte, weil die Karte ihre Spalte behält. Eine zweite, board-weite Sicht wäre eine eigene Adresse mit eigener Ordnungsfrage.
- **Archivierungsdatum und „archiviert von".** Kein Kriterium verlangt sie, kein Schirm zeigt sie. Braucht `D0009` sie für eine Auswertung, kommt der Träger dort — mit dem Wissen, wofür.
- **Massenaktionen** („alle erledigten archivieren"). Eine Karte, ein Aufruf; alles andere ist eine eigene Interaction.
- **Live-Übertragung an andere offene Sichten.** Wer archiviert, sieht die Bahn sofort ohne die Karte; ein zweiter Betrachter erst beim nächsten Laden. Das ist `I0028`, nicht dieser Slice.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Die abgehakten Punkte unter „Offene Fragen" sind Annahmen mit Beleg aus der Planung, keine bestätigten Vorgaben; die WBS führt sie als Anmerkungen (`Dokumentation/Planung/kanbanc.md:383-393`). **Hier** neu getroffen und dort noch nicht vermerkt ist eine Annahme: dass das `⋯`-Menü auch an den Karten der Abschlussbahn erscheint. Die eine Frage, die im stillen Lauf **nicht** entschieden wurde, ist das Schließen des Menüs per Klick daneben — sie steht offen und ist vor `B0207` zu beantworten.
