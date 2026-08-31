---
id: R00006
status: Neu
datum: 2026-08-30
---

# R00006: Karten anlegen und am Board sehen

## Beschreibung

Das Board bekommt seinen eigentlichen Inhalt: die **Karte**. Sie entsteht in einer Spalte — über die Oberfläche wie über die API — und erscheint in der Bahn dieser Spalte an ihrer Position. Damit entsteht das Kartenmodell überhaupt erst: Tabelle, Contract, Endpunkt, Klient und Kartenform in der Bahn. Die Karte trägt in dieser Anforderung genau einen fachlichen Wert, ihren Titel; alles Weitere, was auf ihr später steht, gehört anderen Interactions.

Zwei Slices in einer Anforderung, weil `I0010` allein nicht ehrlich prüfbar wäre: Ohne einen Weg, eine Karte anzulegen, ließe sich „das Board zeigt die enthaltenen Karten" nur belegen, indem der Test Zeilen an der Anwendung vorbei in die Datenbank schreibt.

Zahlt ein auf: [Vision](R00000-vision.md) — ein Board, auf dem Mensch und Agent gleichberechtigt arbeiten; eine API auf Augenhöhe mit der Oberfläche.

## Geschäftlicher Nutzen

Nach fünf Anforderungen hat KanbanC Boards, Spalten, eine Board-Seite, einen Layout-Modus und die gezeichnete Gestaltung — aber keine einzige Aufgabe. Die Bahnen sind leer, und die Stellen für Kartenzahl und `+ Karte` stehen aus `R00005` ausdrücklich als reservierte Leerstellen im Markup. Alles, wofür die Anwendung gebaut wird, hängt an dieser einen fehlenden Sache: Zeiterfassung braucht eine Karte, an der die Zeit hängt; der WBS-Import braucht Karten, die er erzeugt; Burndown und Soll-Ist rechnen über Karten. Solange es keine Karte gibt, ist keiner dieser Wege begehbar, und das Board ist eine Demonstration seiner selbst.

Der zweite Nutzen ist die Zusage der Vision, eingelöst am ersten Gegenstand, der beiden Akteuren gehört: Ein Agent, der eine Aufgabe anlegt, und ein Mensch, der sie anlegt, benutzen ab hier denselben Weg — der Mensch über die Oberfläche, der Agent über denselben Endpunkt, den die Oberfläche ruft.

## Funktionale Anforderungen

- Eine Karte gehört zu genau einer Spalte und trägt einen Titel sowie eine Position innerhalb ihrer Spalte.
- Das Board liefert seine Spalten mit den enthaltenen Karten in ihrer Reihenfolge — über die API und in der Oberfläche.
- Eine neue Karte entsteht in einer gewählten Spalte, über die Oberfläche wie über die API, und erscheint als letzte in dieser Spalte.
- Eine Kartenanlage ohne Titel wird als lesbare Zurückweisung gemeldet, nicht als Serverfehler.
- Zwei Karten desselben Boards dürfen denselben Titel tragen; anders als die Spaltenbezeichnung ist der Kartentitel kein Unterscheidungsmerkmal.
- Eine Bahn ohne Karten ist als leer erkennbar, statt als unbestimmte Fläche.
- Die Oberfläche erreicht Karten ausschließlich über HTTP-Aufrufe der WebApi.

## Nicht-funktionale Anforderungen

- **Benutzerfreundlichkeit:** Das Anlegen geschieht dort, wo die Karte entsteht — im Fuß der betroffenen Bahn, nicht in einem Formular über oder neben dem Board. Messbar daran, dass zwischen dem Bedienelement zum Anlegen und der Bahn, in der die Karte erscheint, kein Wechsel der Ansicht liegt.
- **Wartbarkeit:** Die Karte ist ein Datensatz mit vier Feldern und bleibt es in dieser Anforderung. Jedes Feld, das eine spätere Interaction braucht (Klasse, Verantwortlicher, Fälligkeit, Farbe, Etiketten, Beschreibung), wird dort angelegt und nicht hier vorweggenommen — eine Spalte im Schema, die niemand füllt, ist eine Zusage ohne Test.
- **Sicherheit:** Unverändert Full-Trust im LAN ohne Authentifizierung (Leitplanke der Vision). Wer eine Karte anlegt, wird in dieser Anforderung nicht festgehalten; die Zuordnung zu einem Kontributor gehört zu `D0002`.
- **Betrieb:** Die Migration läuft bei jedem Start erneut (der `Migrationslaeufer` führt kein Journal) und muss deshalb idempotent sein.

## Akzeptanzkriterien

### Kartenmodell und Datenhaltung
- [ ] Das Schema trägt eine Tabelle `Karte` mit `KarteId` als Primärschlüssel, `Spalte` als Fremdschlüssel auf `Spalte (SpalteId)`, `Titel` und `Position`.
- [ ] Die Migration ist idempotent: Ein zweiter Lauf auf einer Datei, die die Tabelle bereits trägt, ändert weder Schema noch Daten.
- [ ] Ein Titel wird ohne umschließende Leerzeichen gespeichert; der Abruf liefert ihn getrimmt zurück.
- [ ] Karten überleben einen Neustart der WebApi: Nach dem Neustart auf derselben Datei liefert das Board dieselben Karten in derselben Reihenfolge.

### Karten am Board über die API
- [ ] `GET /api/boards/{boardId}` liefert je Spalte ihre Karten; jede Karte trägt `KarteId` und `Titel`.
- [ ] Die Karten einer Spalte stehen in aufsteigender Position: Karten mit den Positionen 1, 2, 3 erscheinen in genau dieser Folge, unabhängig von ihrer `KarteId`.
- [ ] Eine Spalte ohne Karten liefert eine leere Kartenliste, nicht `null`.
- [ ] Eine Karte erscheint ausschließlich bei ihrer eigenen Spalte; die Karten einer Spalte tauchen in keiner anderen Spalte desselben Boards auf.
- [ ] `GET` auf eine nicht vergebene `boardId` liefert unverändert HTTP 404.

### Karten in der Oberfläche
- [ ] Das geöffnete Board zeigt in jeder Bahn die Karten dieser Spalte, jede mit ihrem Titel.
- [ ] Die Karten stehen in der Bahn in derselben Reihenfolge, in der die API sie liefert.
- [ ] Eine Bahn ohne Karten zeigt einen Hinweis darauf, dass sie leer ist, statt einer unbeschrifteten Fläche.
- [ ] Ein Reload der Seite zeigt dieselben Karten in derselben Reihenfolge.
- [ ] Die Bahnen zeigen die Karten in beiden Zuständen der Seite — Arbeitsansicht und Layout-Modus aus `R00004` — und die acht Kriterien der Gruppe „Spaltenpflege im Layout-Modus" aus `R00004` gelten unverändert weiter.

### Karte anlegen über die API
- [ ] `POST /api/boards/{boardId}/spalten/{spalteId}/karten` mit einem Titel legt eine Karte an und liefert sie mit vergebener `KarteId` zurück (HTTP 201) samt `Location` auf die angelegte Karte.
- [ ] Die neue Karte steht als letzte ihrer Spalte: Bei drei vorhandenen Karten erhält sie Position 4.
- [ ] Die neue Karte erscheint danach in `GET /api/boards/{boardId}` an dieser Stelle.
- [ ] Eine Karte lässt sich auch in einer Spalte anlegen, die keine Karte hat — sie erhält Position 1.
- [ ] Zwei Karten mit demselben Titel sind zulässig, auch innerhalb einer Spalte, und erhalten verschiedene `KarteId`.
- [ ] `POST` auf eine nicht vergebene `spalteId` oder auf eine Spalte, die zu einem anderen Board gehört, liefert HTTP 404; es entsteht keine Karte.
- [ ] `POST` auf eine nicht vergebene `boardId` liefert HTTP 404.

### Karte anlegen in der Oberfläche
- [ ] Im Fuß jeder Bahn steht ein Bedienelement, das die Anlage einer Karte in genau dieser Spalte beginnt.
- [ ] Nach dem Anlegen erscheint die Karte in derselben Bahn als letzte, ohne dass die Seite neu geladen werden muss.
- [ ] Das Anlegen lässt sich abbrechen, ohne dass eine Karte entsteht.
- [ ] Wird die Anlage in einer Bahn begonnen und danach in einer zweiten, entsteht die Karte in der Spalte, deren Fuß bedient wurde — je Bahn eine eigene Anlage.
- [ ] Die Oberfläche erreicht die Karten ausschließlich über HTTP-Aufrufe der WebApi; `KanbanC.Blazor` hat weiterhin keine Projektreferenz auf `KanbanC.BL`.

### Zurückweisung ungültiger Kartenanlage
- [ ] Ein leerer oder nur aus Leerzeichen bestehender Titel wird mit HTTP 400 zurückgewiesen; es entsteht keine Karte, und der Bestand der Spalte bleibt unverändert.
- [ ] Jede Zurückweisung liefert den Rumpf `Zurueckweisung` mit mindestens einem lesbaren Befund.
- [ ] Die Zurückweisung erscheint in der Oberfläche als lesbare Meldung an der betroffenen Bahn, ohne dass die Seite abstürzt; die Seite nimmt danach eine weitere Bedienung an und führt sie aus.
- [ ] Ist die WebApi beim Anlegen nicht erreichbar, erscheint eine lesbare Meldung statt einer Ausnahmeseite.

### Bestandsschutz der Tests
- [ ] Alle bestehenden Tests laufen grün. Der Stand vor dieser Anforderung ist 280 (BL 64, Blazor 50, Integration 92, E2E 74).
- [ ] Der Test `BahnenE2ETests.Wenn_ein_Board_geoeffnet_wird_dann_stehen_die_Stellen_fuer_Kartenzahl_und_neue_Karte_leer_bereit` aus `R00005` wird nachgezogen, nicht gelöscht: Seine Aussage über die Kartenzahlstelle (`I0004`, weiterhin leer) bleibt bestehen; seine Aussage über die leere Kartenstelle wird durch das Bedienelement zum Anlegen ersetzt.
- [ ] Kein anderer Test aus `R00001` bis `R00005` wird umgeschrieben, um grün zu bleiben.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Karten/` (heute leer angelegt) nimmt `Karte` und `KarteAnlegenAnfrage` auf. `Source/KanbanC.Contracts/Boards/Spalte.cs` wird um die Kartenliste erweitert.
- **Fachlogik:** `Source/KanbanC.BL/Operations/Karten/` (neu, `KartenValidator`), `Source/KanbanC.BL/Integrations/Karten/` (neu, `KartenService`), `Source/KanbanC.BL/Interfaces/Karten/` (neu, `IKartenRepository`).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/` (neu, `KartenRepository`, `Kartenleser`); Schema unter `Source/KanbanC.BL/Persistenz/Migrationen/` als `003-karten.sql`. Die Datei wird über `<EmbeddedResource>` mitgeliefert und über ihr Nummernpräfix einsortiert.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` (neu), Registrierung in `Program.cs` neben `BoardEndpunkte` und `SpaltenEndpunkte`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (neu), `Source/KanbanC.Blazor/Components/Karten/` (neu: `Karte.razor`, `Kartenanlage.razor` samt CSS), Erweiterung von `Components/Spalten/Spaltenbahnen.razor` und `.razor.css`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Karten/` und `Integrations/Karten/` (dazu ein `TestKartenRepository` in `TestHelpers/`), `Source/KanbanC.Blazor.Tests/Services/` (`KartenApiKlientTests`), `Source/KanbanC.WebApi.IntegrationTests/Api/` (`KartenEndpunkteTests`) und `Persistenz/Karten/` (`KartenRepositoryTests`), `Source/KanbanC.PlaywrightTests/` mit Erweiterung des Seitenobjekts `BoardSeite` und neuen Testklassen.
- **Unberührt:** Die Gestaltungsdateien aus `R00005` (`wwwroot/gestaltung.css`, `wwwroot/fonts/`) — diese Anforderung nutzt die Tokens, sie ändert sie nicht.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0003.dc.html`](../Dokumentation/Wireframes/D0003.dc.html) (Dialog `D0003 · Board bedienen`, Stand zurückgeholt am 2026-08-30) ist die Gestaltungsvorgabe für diese Anforderung. Für sie gelten daraus: die **Kartenform in der Bahn**, der **Zustand einer leeren Bahn**, der **geöffnete Anlegevorgang im Bahnenfuß** samt der Form seiner Zurückweisung, und der Randfall des **zu langen Titels**.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung** — aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Zwei Hinweise, die es dennoch verbindlich macht, weil sie den Umfang betreffen und nicht die Optik:

- Die Lesehilfe des Artboards trennt ausdrücklich, was auf der Karte gezeichnet, aber **später gebaut** wird: Klassennummer (`I0021`), Subtask-Zähler (`I0016`), Etikett (`I0015`), Soll- und Ist-Zeit samt laufendem Timer (`I0023`, `I0024`), Kontributor-Avatar (`I0008`). Diese Anforderung baut die Kartenform mit dem Titel; sie baut keinen dieser Werte.
- Das Artboard zeigt fünf Interactions im selben Schirm. `I0012` (gezogene Karte, Ablagestelle), `I0013` (Gruppierung nach Erledigungsdatum, „Ältere nachladen") und `I0014` (Kartenmenü mit „Archivieren") sind darin gezeichnet und gehören **nicht** hierher.

### Ablauf

1. **Schema erweitern**
   - 1.1 `003-karten.sql`: `CREATE TABLE IF NOT EXISTS Karte`, `CREATE INDEX IF NOT EXISTS IX_Karte_Spalte`
   - 1.2 Der `Migrationslaeufer` findet sie als eingebettete Ressource über das Nummernpräfix; ein Journal gibt es nicht, deshalb muss jedes Statement wiederholbar sein
2. **Board öffnen — Lesepfad**
   - 2.1 `BoardRepository.Lade(boardId)` liest Board und Spalten wie bisher
   - 2.2 `Kartenleser.LiesKartenNachPosition(...)` liest die Karten aller Spalten des Boards in **einer** Abfrage, gruppiert nach `Spalte`
   - 2.3 Jede `Spalte` wird mit ihren Karten aufgebaut; das Board reist als Ganzes über `GET /api/boards/{boardId}`
   - 2.4 `Spaltenbahnen.razor` rendert je Bahn die Karten, sonst den Leer-Hinweis
3. **Karte anlegen**
   - 3.1 Klick auf das Bedienelement im Bahnenfuß öffnet die Anlage für **diese** Bahn
   - 3.2 `KartenApiKlient.LegeKarteAn(boardId, spalteId, anfrage)` ruft `POST .../spalten/{spalteId}/karten`
   - 3.3 `KartenService.LegeKarteAn`: Spalten des Boards laden
     - 3.3.1 Die `spalteId` gehört nicht zu diesem Board oder es gibt sie nicht → `null` → HTTP 404
   - 3.4 `KartenValidator.Pruefe(anfrage)` → `Pruefbefunde`
     - 3.4.1 Befunde vorhanden → `Ergebnis.Zurueckgewiesen` → HTTP 400 mit `Zurueckweisungen.Aus(...)`
   - 3.5 `KartenRepository.LegeAn` schreibt in einer Transaktion: höchste Position der Spalte ermitteln, Karte mit `Position + 1` einfügen, Karte zurücklesen
   - 3.6 Die Oberfläche lädt das Board neu; die Karte steht in ihrer Bahn
4. **Fehlerpfade der Oberfläche**
   - 4.1 Zurückweisung → Meldung an der Bahn, die Anlage bleibt offen
   - 4.2 `HttpRequestException` → `WebApiAufruf.MitAusfallmeldung` liefert die Ausfallmeldung; kein Absturz

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `003-karten.sql` — die Migration, die es die Karte überhaupt geben lässt; sie ist die erste Datei, die entsteht.
- `Spalte` (Contract) — hier entscheidet sich, dass die Karten mit dem Board reisen statt über einen zweiten Abruf zu kommen; die Änderung berührt jeden Ort, der heute eine `Spalte` baut.
- `Spaltenbahnen.razor` — die eine Bahn-Komponente aus `R00004`/`R00005`; ihre reservierten Stellen `.spaltenbahn-flaeche` und `.spaltenbahn-kartenstelle` werden hier gefüllt.
- `Program.cs` der WebApi — `KartenEndpunkte.Registriere(app)` und die DI-Einträge für `IKartenRepository` und `KartenService`.

**Klassen-Entwurf:**

- `Karte` (Contract, DTO, immutable) — eine Aufgabe auf dem Board: ihre Nummer, ihr Titel und ihre Position innerhalb der Spalte. Vier Felder und keines mehr; jedes weitere kommt mit der Interaction, die es braucht.
  - `public record Karte(long KarteId, string Titel, int Position)`
- `KarteAnlegenAnfrage` (Contract, DTO, immutable) — was die Oberfläche und ein Agent schicken, um eine Karte entstehen zu lassen. Die Spalte steht in der Route, nicht im Rumpf.
  - `public record KarteAnlegenAnfrage(string Titel)`
- `KartenValidator` (Operation, statisch) — prüft eine Kartenanfrage gegen die fachlichen Regeln. Pure Logik ohne Seiteneffekte, wie `SpaltenValidator`.
  - `public static Pruefbefunde Pruefe(KarteAnlegenAnfrage anfrage)`
- `Kartentitel` (Operation, statisch) — die eine Stelle, die entscheidet, was ein Titel getrimmt bedeutet; von Validator und Repository gemeinsam genutzt, nach dem Muster von `Spaltenbezeichnung` (ADR-0001).
  - `public static string Normalisiert(string titel)`
- `IKartenRepository` (Interface) — Datenzugriff auf Karten. Existiert, weil die Unit-Tests des Service eine zweite Implementation brauchen (`TestKartenRepository`), wie bei Boards und Spalten.
  - `Ergebnis<Karte>? LegeAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)`
  - `IReadOnlyList<Karte>? LadeAlle(long boardId, long spalteId)`
- `KartenRepository` (Provider, Ressourcenzugriff) — schreibt und liest Karten über Dapper; eine Transaktion je Schreibvorgang, `null` für „gibt es nicht". Die Position der neuen Karte ist die höchste der Spalte plus eins.
  - `public KartenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)`
  - die beiden Methoden aus `IKartenRepository`
- `Kartenleser` (Provider, intern) — liest die Karten eines Boards nach Spalte gruppiert, damit der Board-Abruf ohne eine Abfrage je Spalte auskommt. Gegenstück zu `Spaltenleser`.
  - `public static IReadOnlyDictionary<long, IReadOnlyList<Karte>> LiesKartenNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)`
- `KartenService` (Integration) — verdrahtet Laden, Prüfen und Schreiben; enthält selbst keine Regel (IOSP).
  - `public KartenService(ISpaltenRepository spaltenRepository, IKartenRepository kartenRepository)`
  - `public Ergebnis<Karte>? LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)`
- `KartenEndpunkte` (Integration, statisch) — bindet die Kartenroute an den Service, nach dem Muster von `SpaltenEndpunkte`.
  - `public static void Registriere(IEndpointRouteBuilder routen)`
- `KartenApiKlient` (Integration, Blazor) — der HTTP-Weg der Oberfläche zu den Karten; übersetzt 400 in eine `Zurueckweisung` und 404 in eine feste Meldung, wie `SpaltenApiKlient`.
  - `public KartenApiKlient(IHttpClientFactory klientFabrik)`
  - `public Task<ApiErgebnis<Karte>> LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)`
- `Karte` (Blazor-Komponente, Datei `Components/Karten/Karte.razor`) — stellt eine Karte in der Bahn dar. Eigene Komponente, weil an ihr in `D0004`, `D0005` und `D0006` weitere Werte hinzukommen und die Bahn davon nichts wissen soll.
  - `[Parameter] KanbanC.Contracts.Karten.Karte Kartendaten`
- `Kartenanlage` (Blazor-Komponente) — der Anlegevorgang im Fuß **einer** Bahn: Bedienelement, Titelfeld, Anlegen und Abbrechen, Meldung bei Zurückweisung oder Ausfall. Je Bahn eine Instanz, damit der Zustand einer Bahn den einer anderen nicht berührt.
  - `[Parameter] long BoardId`
  - `[Parameter] long SpalteId`
  - `[Parameter] EventCallback KarteWurdeAngelegt`

### Änderungen an bestehenden Klassen

- `Spalte` (Contract) — bekommt `IReadOnlyList<Karte> Karten` als weiteres Glied des Records. Das ist die spürbarste Änderung dieser Anforderung: Jeder Ort, der heute eine `Spalte` erzeugt, muss die Karten mitgeben — `Spaltenleser`, `SpaltenRepository` (beide Schreibpfade), die Test-Repositories und jeder Test, der eine `Spalte` von Hand baut. Eine neu angelegte oder geänderte Spalte trägt eine leere Kartenliste.
- `BoardRepository.Lade` — reicht die gelesenen Karten an die Spalten durch.
- `Spaltenleser` — bekommt die Karten des Boards herein und hängt sie an die passende Spalte.
- `Spaltenbahnen.razor` (+ `.razor.css`) — die Fläche zwischen Kopf und Fuß rendert die Karten oder den Leer-Hinweis; die reservierte Kartenstelle im Fuß nimmt `Kartenanlage` auf. `IstBearbeitbar` und die vorhandenen Ereignisse bleiben unverändert.
- `Program.cs` (WebApi) — `IKartenRepository`, `KartenService`, `KartenEndpunkte.Registriere`.
- `Program.cs` (Blazor) — `AddScoped<KartenApiKlient>()`.
- `BoardSeite` (Seitenobjekt der E2E-Tests) — Locator für Karten je Bahn, für die Anlage im Bahnenfuß und für deren Meldung.
- `BahnenE2ETests` — die Aussage über die leere Kartenstelle wird nachgezogen (siehe Bestandsschutz).

## Tests

Nach `test-pyramide` und `test-ehrlichkeit`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KartenValidator` — die Titelregeln ohne Seiteneffekte: leerer Titel, Titel aus Leerzeichen, gültiger Titel ohne Befund.
- `Kartentitel` — das Trimmen als eigene Aussage.
- `KartenService` — gegen `TestKartenRepository` und `TestSpaltenRepository`: unbekannte Spalte reicht `null` durch, Befunde führen zu `Zurueckgewiesen` und **ohne** Schreibzugriff (Beobachterflag am Test-Repository), gültige Anfrage schreibt.
- `KartenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 201 wird Erfolg, 400 wird Zurückweisung mit den Befunden aus dem Rumpf, 404 wird die feste Meldung. Diese Pfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository` gegen eine `TemporaereDatenbank` (Position höchste + 1, leere Spalte beginnt bei 1, Reihenfolge nach Position, Fremdspalte liefert `null`, getrimmt gespeichert); `Migrationslaeufer` mit dem zweiten Lauf auf bestehender Datei; `KartenEndpunkte` über `TestWebApi` (201 mit `Location`, 400 mit `Zurueckweisung`, 404 bei fremder oder unbekannter Spalte, das Board liefert die Karten danach an der erwarteten Stelle); der Neustart-Test um Karten erweitert. Das Arrange für den reinen Lesepfad darf die Karten per SQL setzen — der Schreibweg wird an eigener Stelle geprüft.

**E2E:** Karten erscheinen in ihren Bahnen in der gelieferten Reihenfolge (US-1); die leere Bahn zeigt ihren Hinweis und die Reihenfolge überlebt den Reload (US-2); eine Karte wird über den Bahnenfuß angelegt und erscheint als letzte derselben Bahn, das Abbrechen legt keine an (US-3); eine über die API angelegte Karte erscheint nach dem Öffnen des Boards in der Oberfläche (US-4); ein leerer Titel erzeugt eine lesbare Meldung, und die Seite nimmt danach eine gültige Eingabe an (US-5). Dazu laufen alle E2E-Tests aus `R00001` bis `R00005` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `R00002` (Spalten gestalten, erledigt) — ohne Spalten gibt es keinen Ort für eine Karte; `R00003` (Board-Seite, erledigt) — die Seite, auf der die Bahnen stehen; `R00005` (Oberfläche nach Wireframes, erledigt) — die Bahnenform mit den reservierten Stellen, die hier gefüllt werden. In der WBS: `I0010` braucht `I0003` (grün), `F0017` braucht `I0010`, `F0018` braucht `F0017`.
- Blockiert: `I0004` (Kartenzahl im Spaltenkopf), `I0012` (Karte verschieben), `I0013` (Erledigte gebündelt sehen), `I0014` (Karte archivieren), `I0015`–`I0019` (Karteninhalt, `D0004`), `I0021` (Karte einer Klasse zuordnen), `I0023` (Timer starten), `I0037` (Rohdaten über die API), `I0038` (Board exportieren). Ohne Karte ist keiner dieser Slices begehbar.

## Offene Fragen

- **Was geschieht mit den Karten, wenn ihre Spalte entfernt wird?** `DELETE /api/boards/{boardId}/spalten/{spalteId}` aus `R00002` löscht heute eine Spalte ohne Rückfrage; ab dieser Anforderung können daran Karten hängen. *Vorschlag:* Eine Spalte, die Karten enthält, wird mit HTTP 400 und einem lesbaren Befund zurückgewiesen — Datenverlust ohne Rückfrage ist der schlechtere Ausgang, und der Ausweg über das Archiv (`I0014`) existiert noch nicht. Das wäre eine Änderung an einem Kriterium aus `R00002` („eine als Abschlussspalte markierte Spalte lässt sich ohne Vorbedingung entfernen") und gehört deshalb entschieden, bevor gebaut wird. Wird stattdessen das Mitlöschen gewählt, muss es ausdrücklich als Kriterium hier stehen, nicht als Nebenwirkung des Fremdschlüssels.
- **Soll der Kartentitel eine Längengrenze haben?** Das Artboard zeichnet den Randfall eines zu langen Titels als Darstellungsfrage (die Karte wächst). *Vorschlag:* keine Grenze in dieser Anforderung — die Darstellung trägt den langen Titel, und eine Grenze ohne fachlichen Grund wäre eine willkürliche Zahl im Validator. Sollte eine gewünscht sein, gehört sie als Kriterium in die Gruppe „Zurückweisung".
- **Trägt `GET /api/boards/{boardId}` künftig alle Karten, auch wenn es Tausende sind?** In dieser Anforderung ja — es gibt keine Grenze. Die erste Interaction, die das begrenzt, ist `I0013` (Abschlussspalte, 20 neueste). *Vorschlag:* offen lassen und mit `I0013` entscheiden; eine Grenze jetzt einzuziehen, ohne dass sie jemand braucht, wäre tote Flexibilität.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass nach fünf Anforderungen ein Board existiert, auf dem nichts liegt: `R00005` hat die Bahnen samt den Stellen für `+ Karte` und die Kartenzahl gebaut und beide ausdrücklich leer gelassen, weil es keine Karte gibt. Wenn das Kartenmodell jetzt entsteht — Tabelle, Contract, Endpunkt, Klient, Kartenform —, dann bekommt jede der neun folgenden Interaction-Gruppen ihren Anker: Verschieben braucht eine Karte mit Position, Zeiterfassung eine Karte, an der die Zeit hängt, der WBS-Import eine Karte, die er erzeugt, und die Auswertungen Karten, über die sie rechnen. Über diese Kette wird aus einem Gerüst ein benutzbares Board, und die Zusage der Vision — Mensch und Agent arbeiten gleichberechtigt — wird erstmals an einem Gegenstand einlösbar, der beiden gehört. Gerade diese Änderung ist der Hebel und keine vorgelagerte: Die Spalten stehen, die Board-Seite steht, die Bahnenform steht mit reservierten Stellen; alles, was der Karte vorausgehen musste, ist grün, und alles Weitere wartet auf sie. Und sie ist auch nicht nachgelagert zu verschieben, weil jede Interaction, die man stattdessen vorzöge, dieselbe Karte bereits voraussetzt.

Dass `I0010` und `I0011` zusammen geschnitten sind, ist Teil der Lösung und nicht Bequemlichkeit: Ein Slice, dessen Fertig-Kriterium sich nur durch direktes Schreiben in die Datenbank belegen ließe, hätte einen Test, der die Anwendung umgeht — und damit keinen ehrlichen Beweis im Sinne von `test-ehrlichkeit`.

## Missing-Docs

- **Gruppiertes Nachladen mit Dapper.** Wie die Karten aller Spalten eines Boards in einer Abfrage gelesen und den Spalten zugeordnet werden (Multi-Mapping, `QueryMultiple` oder Gruppierung im Speicher), ist im Projekt nirgends beschrieben; die bisherigen Leser holen je Board eine flache Liste. Die Wahl beeinflusst, wie `Spaltenleser` und `Kartenleser` zusammenspielen.
- **Fremdschlüssel-Durchsetzung in SQLite.** Ob die Anwendung `PRAGMA foreign_keys = ON` setzt, ist im Projekt nicht festgehalten; SQLite hat es je Verbindung standardmäßig aus. Die Antwort entscheidet mit über die erste offene Frage (Spalte mit Karten löschen) und darüber, ob der Fremdschlüssel der Migration mehr als Dokumentation ist.
- **Zustand je Bahn in einer Listenkomponente.** `Spaltenbahnen.razor` baut ihre Formulare heute in `OnParametersSet` nur neu, wenn sich die Spalten geändert haben, damit getippte Eingaben ein Elternrender überleben. Wie sich das verhält, wenn zusätzlich je Bahn eine eigene Kartenanlage Zustand hält, ist nicht beschrieben und wird beim Bauen erneut hergeleitet.

## Notizen

**Was das Artboard zeigt und was hier gebaut wird.** `D0003.dc.html` zeichnet fünf Interactions in einem Schirm. Gebaut werden hier zwei: die gefüllte Bahn (`I0010`) und die offene Anlage im Bahnenfuß samt Zurückweisung (`I0011`). Die gezogene Karte mit Ablagestelle (`I0012`), die Gruppierung nach Erledigungsdatum mit „Ältere nachladen" (`I0013`) und das Kartenmenü mit „Archivieren" (`I0014`) stehen im selben Bild und bleiben unangetastet.

**Die Karte ist hier absichtlich arm.** Auf der gezeichneten Karte stehen Klassennummer, Avatar, Subtask-Zähler, Etikett und zwei Zeitangaben. Keines dieser Felder entsteht hier — die Lesehilfe des Artboards ordnet sie `D0004`, `D0005`, `D0006` und `D0002` zu. Eine Spalte im Schema anzulegen, die erst in drei Anforderungen jemand füllt, wäre eine Zusage, die kein Test deckt.

### Verworfene Alternativen

- **Karten über einen eigenen Endpunkt laden** (`GET /api/boards/{boardId}/karten` oder je Spalte) — erwogen, weil es das `Spalte`-Contract unangetastet ließe und damit die Anpassung aller Stellen erspart, die heute eine `Spalte` bauen. Verworfen, weil das Fertig-Kriterium von `I0010` „das Board zeigt seine Spalten mit den enthaltenen Karten" lautet: Ein Board, das seine Karten nicht enthält, zwingt jeden Aufrufer — Oberfläche wie Agent — zu einem zweiten Aufruf und zum Zusammenfügen von Hand. Für `I0013` kann später ein gezielter Kartenabruf hinzukommen, ohne dass diese Entscheidung im Weg steht.
- **Karte mit dem vollen Feldsatz des Artboards anlegen** (Beschreibung, Verantwortlicher, Fälligkeit, Farbe, Etiketten, Klasse) — erwogen, weil das Schema dann nur einmal angefasst würde. Verworfen, weil kein Test diese Felder decken könnte: Sie haben in dieser Anforderung weder Endpunkt noch Oberfläche, und ein Feld ohne Weg dorthin ist keine Funktion, sondern eine Behauptung. Migrationen sind hier billig — der Läufer nimmt eine `004-` ohne Umstände auf.
- **Nur `I0010` schneiden und die Karten für den Test per SQL setzen** — verworfen, weil der Beleg dann an der Anwendung vorbeiginge; `test-ehrlichkeit` verlangt, dass eine echte Zustandsänderung über den echten Weg geprüft wird. Genau deshalb sind beide Slices in einer Anforderung.
- **Das Anlegen als eigenes Formular über dem Board** statt im Bahnenfuß — erwogen, weil ein Formular für alle Bahnen weniger Zustand hält als eine Anlage je Bahn. Verworfen, weil die Spalte dann als Auswahlfeld gewählt werden müsste, obwohl der Ort auf dem Schirm sie bereits benennt; das Artboard zeichnet die Anlage im Fuß der betroffenen Bahn, und `R00005` hat die Stelle dafür bereits reserviert.

### Bewusst out of scope

- `I0004` Kartenzahl im Spaltenkopf — die Stelle bleibt leer, wie sie `R00005` hinterlassen hat.
- `I0012` Karte verschieben (Spalte und Position ändern), `I0013` erledigte Karten gebündelt sehen, `I0014` Karte archivieren.
- Karte ändern und Karte löschen. Diese Anforderung kennt nur Anlegen und Lesen; ein `PUT` oder `DELETE` auf Karten entsteht mit der Interaction, die es braucht.
- Karteninhalt jeder Art (`D0004`): Beschreibung, Verantwortlicher, Fälligkeit, Farbe, Etiketten, Subtasks, Kommentare, Anhänge, Dateiverweise.
- Klassen und Kartennummern (`D0005`) — die gezeichnete Nummer auf der Karte gehört `I0021`.
- Zeiterfassung (`D0006`) — Soll-Zeit, Ist-Zeit und laufender Timer auf der Karte.
- Live-Aktualisierung (`D0007`) — eine Karte, die ein anderer Browser anlegt, erscheint hier erst nach einem Reload.
- Kontributor-Zuordnung und Avatar (`D0002`, `I0008`).
- Eine Grenze für die Zahl der gelieferten Karten; siehe Offene Fragen.
