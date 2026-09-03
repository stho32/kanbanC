---
id: R00007
status: Neu
datum: 2026-09-02
---

# R00007: Karte verschieben

## Beschreibung

Eine Karte wechselt ihre Spalte und ihre Position innerhalb der Spalte — in der Oberfläche durch Ziehen und Ablegen, über die API durch einen Aufruf. Die neue Lage ist dauerhaft und steht nach einem Reload unverändert da. Die Positionen einer Spalte bleiben dabei lückenlos von 1 an.

Mit demselben Zug hebt diese Anforderung den **Fehlervertrag der ganzen API**: jede Fehlerantwort — die Zurückweisung einer Prüfung wie das „gibt es nicht" — trägt je Befund einen stabilen Code, eine Meldung mit den konkreten Werten und die Kompensationsaktion, also den nächsten Aufruf, der den Aufrufer aus der Lage bringt. Heute antworten sechs Stellen der API mit einem leeren 404-Rumpf und die übrigen mit reiner Prosa für Menschen.

Zahlt ein auf: [Vision](R00000-vision.md) — „Der Agent bewegt Karten […] über die API; die Weboberfläche ist der menschliche Blick auf denselben Datenbestand." und „Eine API auf Augenhöhe mit der Oberfläche."

## Geschäftlicher Nutzen

Ein Kanban-Board, auf dem sich nichts bewegt, ist eine Liste. Der Fluss der Arbeit — von `Rückstand` über `In Arbeit` nach `Fertig` — ist der Kern des Verfahrens, und er entsteht erst mit dieser Interaction. Bis hierher konnten Mensch und Agent Karten anlegen und ansehen; ab hier führen sie den Stand der Arbeit tatsächlich. Alles, was danach an D0003 hängt, setzt darauf auf: die Abschlussspalte gruppiert Erledigtes (I0013), der Live-Kanal überträgt genau diese Bewegung an alle offenen Sichten (I0028).

## Funktionale Anforderungen

- Eine Karte lässt sich in eine andere Spalte desselben Boards verschieben, an eine gewählte Position.
- Eine Karte lässt sich innerhalb ihrer Spalte an eine andere Position verschieben.
- Die Positionen der Quell- und der Zielspalte sind nach jedem Zug lückenlos von 1 an durchnummeriert.
- Die API bietet den Zug als eigenen Aufruf an, mit Zielspalte und Zielposition als Angabe.
- Die Oberfläche bewegt eine Karte durch Ziehen und Ablegen; Ablagestellen zeigen während des Zugs, wo die Karte landet.
- Eine unmögliche Lage wird lesbar zurückgewiesen, ohne die Karte zu bewegen.
- Ein Zug auf eine fremde oder verschwundene Karte, Spalte oder ein unbekanntes Board wird als „gibt es nicht" beantwortet, nicht als Fehler.
- **Jede** Fehlerantwort der API nennt je Befund einen stabilen Code, eine Meldung mit den konkreten Werten und die Kompensationsaktion — auch die Antworten mit Status 404, die heute einen leeren Rumpf haben.

## Nicht-funktionale Anforderungen

- Performance: Ein Zug schreibt in **einer** Transaktion; die Oberfläche kommt mit einem Aufruf je Ablegevorgang aus. Während eines Zugs erzeugt das Überfahren einer Ablagestelle höchstens ein Ereignis je betretener Stelle — `dragover` läuft ohne Serverbeteiligung.
- Benutzerfreundlichkeit: Die Ablagestellen erscheinen erst mit dem Beginn eines Zugs und verschwinden mit seinem Ende; die überfahrene Stelle ist hervorgehoben. Ein abgebrochener Zug lässt das Board unverändert.
- Sicherheit: unverändert Full-Trust im LAN — jeder darf jede Karte bewegen.
- Bedienbarkeit durch Agenten: Eine Fehlerantwort reicht allein aus, um weiterzukommen — ohne die Oberfläche zu öffnen, ohne die Anforderung zu lesen und ohne zu raten. Die Codes sind stabil und dürfen sich nicht mit jeder Formulierung der Meldung ändern.

## Akzeptanzkriterien

### Karte in eine andere Spalte bewegen (API)

- [ ] `PUT /api/boards/{boardId}/karten/{karteId}/lage` mit `{ "spalteId": <Ziel>, "position": <n> }` antwortet mit HTTP 200 und den Spalten des Boards, jede mit ihren Karten in der neuen Reihenfolge.
- [ ] Die Karte steht danach in der Zielspalte an Position `n` und in keiner anderen Spalte mehr.
- [ ] `GET /api/boards/{boardId}` liefert sie an derselben Stelle.
- [ ] Rechenbeispiel: Quellspalte `[A, B, C]`, Zielspalte `[X, Y]`; `B` nach Zielspalte Position 1 ergibt Quellspalte `[A(1), C(2)]` und Zielspalte `[B(1), X(2), Y(3)]`.

### Karte innerhalb ihrer Spalte umsortieren (API)

- [ ] Derselbe Aufruf mit der Spalte, in der die Karte schon liegt, setzt sie auf die genannte Position.
- [ ] Rechenbeispiel: Spalte `[A, B, C, D]`; `D` auf Position 2 ergibt `[A(1), D(2), B(3), C(4)]`. `A` auf Position 4 ergibt `[B(1), C(2), D(3), A(4)]`.
- [ ] Ein Zug auf die Position, an der die Karte bereits steht, ist erlaubt und lässt die Reihenfolge unverändert.

### Lückenlose Positionen und Dauerhaftigkeit

- [ ] Nach jedem Zug tragen die Karten der Quellspalte die Positionen 1..n-1 und die der Zielspalte 1..m, jeweils ohne Lücke und ohne Dublette.
- [ ] Nach einem Neustart der WebApi auf derselben Datenbankdatei liegen alle Karten unverändert an ihren neuen Stellen.
- [ ] Ein Zug schreibt in einer Transaktion: bricht er ab, ist keine der beiden Spalten halb verändert.

### Verschieben in der Oberfläche

- [ ] Eine Karte auf dem Board lässt sich mit der Maus aufnehmen; während des Zugs zeigen die Bahnen Ablagestellen zwischen, vor und nach ihren Karten.
- [ ] Wird die Karte auf einer Ablagestelle abgelegt, steht sie danach genau dort — in derselben oder einer anderen Bahn.
- [ ] Wird der Zug außerhalb jeder Ablagestelle beendet, bleibt das Board unverändert und die Ablagestellen verschwinden wieder.
- [ ] Die Bahnen zeigen nach dem Ablegen den Stand, den auch `GET /api/boards/{boardId}` liefert; ein Reload ändert daran nichts.
- [ ] Im Layout-Modus (R00004) wird nicht gezogen — dort werden Spalten gepflegt, nicht Karten bewegt.

### Zurückweisung und Fehlerpfade

- [ ] Eine Position kleiner als 1 oder größer als die Zahl der Karten, die die Zielspalte nach dem Zug trägt, wird mit HTTP 400 zurückgewiesen; keine Karte bewegt sich.
- [ ] Rechenbeispiel: Zielspalte mit 3 Karten, die Karte kommt aus einer anderen Spalte → gültig sind 1 bis 4; 0 und 5 werden zurückgewiesen. Liegt die Karte schon in dieser Spalte → gültig sind 1 bis 3.
- [ ] Ein unbekanntes Board, eine unbekannte Karte, eine Karte eines anderen Boards oder eine Zielspalte eines anderen Boards ergeben HTTP 404, und nichts wird geschrieben.
- [ ] Verschwindet die Karte zwischen Prüfung und Schreiben, endet der Aufruf mit 404 statt mit einem Serverfehler.
- [ ] Ist die WebApi beim Ablegen nicht erreichbar, erscheint an der Board-Seite die Ausfallmeldung statt einer Ausnahmeseite; das Board bleibt bedienbar.
- [ ] Eine Zurückweisung erscheint in der Oberfläche als lesbare Meldung, und die Karte kehrt sichtbar an ihre alte Stelle zurück.

### Fehlerantworten, die ein Agent benutzen kann

- [ ] Jede Fehlerantwort der API (Status 400 wie 404) trägt einen Rumpf mit mindestens einem Befund; **keine** Fehlerantwort hat einen leeren Rumpf.
- [ ] Jeder Befund trägt drei nichtleere Felder: `code`, `meldung`, `kompensation`. Ein Test geht **alle** Fehlerantworten aller Endpunkte durch und weist das nach — nicht nur die der Lage-Route.
- [ ] `code` ist stabil und maschinenlesbar (kebab-case, z. B. `position-ausserhalb`); eine geänderte Formulierung der Meldung ändert ihn nicht.
- [ ] `meldung` nennt die **konkreten Werte** des Vorgangs, nicht nur die Regel. Beispiel: „Position 5 liegt außerhalb der Zielspalte „In Arbeit“ (SpalteId 7): nach dem Zug trägt sie 4 Karten, gültig sind 1 bis 4." — nicht „Ungültige Position."
- [ ] `kompensation` nennt einen ausführbaren nächsten Schritt mit Route. Beispiel: „`GET /api/boards/3` abrufen, die Karten der Zielspalte zählen und den Zug mit einer Position zwischen 1 und 4 wiederholen."
- [ ] Die Codes der Lage-Route sind: `position-ausserhalb`, `board-unbekannt`, `karte-unbekannt`, `karte-fremd`, `spalte-unbekannt`, `spalte-fremd`, `bestand-geaendert`.
- [ ] Alle **15** Befunde, die die API heute schon liefert, bekommen Code und Kompensationsaktion; keiner bleibt als nackter String zurück. Gezählt am 2026-09-03: `BoardAnlegenValidator` 3, `SpaltenValidator` 5, `SpaltenreihenfolgeValidator` 3, `KartenValidator` 2, `SpaltenRepository` 2.
- [ ] Die Oberfläche zeigt weiterhin **nur** `meldung` an — Code und Kompensationsaktion sind für den Agenten, nicht für den Menschen am Bildschirm.

### Bestandsschutz der Tests

- [ ] Alle E2E-, Integrations- und Unit-Tests aus `R00001` bis `R00006` bleiben grün; insbesondere die Aussagen über Kartenreihenfolge und leere Bahn aus `R00006`.
- [ ] Tests, die heute auf **Befundtexte als Zeichenketten** prüfen, werden auf die neue Form gezogen — das ist eine Anpassung an den geänderten Vertrag, keine Ausnahme vom Bestandsschutz. **Keine fachliche Aussage eines bestehenden Tests darf dabei entfallen**: wo heute ein Meldungstext geprüft wird, wird danach `Meldung` geprüft, nicht weniger.
- [ ] Jede in der Oberfläche sichtbare Meldung lautet nach dem Umbau wie vorher; die E2E-Tests aus `R00001`–`R00006`, die Meldungstexte lesen, laufen unverändert.
- [ ] `TreatWarningsAsErrors` bleibt aktiv, der Bau bleibt warnungsfrei.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Karten/` nimmt `Kartenlage` auf — die Angabe „Zielspalte und Zielposition". Neu ist `Source/KanbanC.Contracts/Fehler/` mit `Fehlerbefund`; `Zurueckweisung` zieht aus `Contracts/Boards/` dorthin um, weil sie der Rumpf **jeder** Fehlerantwort ist und unter `Boards/` schon heute falsch lag (Karten benutzen sie ebenfalls). `Karte` und `Spalte` bleiben unverändert; das Schema auch, weil `Karte` bereits `Spalte` und `Position` trägt (`003-karten.sql`). **Diese Anforderung braucht keine Migration.**
- **Fachlogik:** `Source/KanbanC.BL/Operations/Karten/` (neu: `KartenlageValidator`), `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` (erweitert), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs` (erweitert).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/KartenRepository.cs` (erweitert); gelesen wird über die vorhandenen `Kartenleser` und `Spaltenleser`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` (erweitert um die Lage-Route); `Zurueckweisungen.cs` wird zum Ort, der Befunde in Fehlerantworten übersetzt; `BoardEndpunkte.cs` und `SpaltenEndpunkte.cs` ziehen mit, weil dort die sechs leeren 404 sitzen.
- **Fachlogik, quer:** `Source/KanbanC.BL/Models/Pruefbefunde.cs` trägt künftig `Fehlerbefund` statt `string`; alle Validatoren unter `Operations/Boards/` und `Operations/Karten/` liefern Code und Kompensationsaktion mit.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (erweitert), `Source/KanbanC.Blazor/Components/Karten/Karte.razor` (+ CSS, wird ziehbar), `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (+ CSS, bekommt die Ablagestellen), `Source/KanbanC.Blazor/Components/Pages/Board.razor` (Meldung nach einem gescheiterten Zug), `Source/KanbanC.Blazor/Services/Zurueckweisungsleser.cs` (liest die neue Form), sowie `Kartenanlage.razor` und `Spaltenpflege.razor`, die die Befunde heute als Strings rendern und künftig `meldung` nehmen.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Karten/` und `Integrations/Karten/` (dazu `TestKartenRepository` in `TestHelpers/` erweitert), `Source/KanbanC.Blazor.Tests/Services/KartenApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/Api/KartenEndpunkteTests.cs` und `Persistenz/Karten/KartenRepositoryTests.cs`, `Source/KanbanC.PlaywrightTests/` mit erweitertem Seitenobjekt `BoardSeite` und einer neuen Testklasse.
- **Unberührt:** `wwwroot/gestaltung.css` und die Schriften — diese Anforderung nutzt die Tokens, sie ändert sie nicht.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0003.dc.html`](../Dokumentation/Wireframes/D0003.dc.html) (Dialog `D0003 · Board bedienen`, Stand zurückgeholt am 2026-09-02) ist die Gestaltungsvorgabe. Für diese Anforderung gelten daraus die beiden Stellen, die die Lesehilfe am Fuß ausdrücklich `I0012` zuschreibt: die **gezogene Karte**, die über den Bahnen schwebt, und die **Ablagestelle** in der Bahn `In Arbeit`.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung** — aus ihm entstehen keine Akzeptanzkriterien. Geprüft wird gegen die User Story. Was es an Umfang klärt: `I0013` (Gruppierung der Abschlussspalte, „Ältere nachladen") und `I0014` (Kartenmenü mit „Archivieren") sind im selben Schirm gezeichnet und gehören **nicht** hierher.

### Ablauf

1. **Zug beginnt (Oberfläche)**
   - 1.1 `Karte.razor` ist `draggable`; `@ondragstart` meldet `KarteId` und Herkunftsspalte an `Spaltenbahnen.razor`
   - 1.2 `Spaltenbahnen.razor` blendet je Bahn die Ablagestellen ein — vor der ersten, zwischen je zwei und nach der letzten Karte; eine leere Bahn bekommt genau eine
   - 1.3 `@ondragover:preventDefault` ohne Handler hält den Browser bei der Sache, ohne den Server zu fragen; `@ondragenter`/`@ondragleave` heben die überfahrene Stelle hervor
2. **Ablegen**
   - 2.1 `@ondrop` an der Ablagestelle kennt Zielspalte und Zielposition aus ihrer eigenen Stelle in der Bahn
   - 2.2 `KartenApiKlient.VerschiebeKarte(boardId, karteId, lage)` ruft `PUT /api/boards/{boardId}/karten/{karteId}/lage`
   - 2.3 Erfolg → das Board wird neu geladen; die Karte steht an ihrer neuen Stelle
   - 2.4 Zurückweisung → lesbare Meldung, Board unverändert neu geladen (die Karte springt sichtbar zurück)
   - 2.5 `HttpRequestException` → `WebApiAufruf.MitAusfallmeldung`
3. **Zug prüfen (Fachlogik)**
   - 3.1 `KartenService.VerschiebeKarte`: `ISpaltenRepository.LadeAlle(boardId)` liefert die Spalten samt Karten
     - 3.1.1 Board unbekannt → `null` → HTTP 404
     - 3.1.2 Karte in keiner Spalte des Boards → `null` → HTTP 404
     - 3.1.3 Zielspalte nicht unter den Spalten des Boards → `null` → HTTP 404
   - 3.2 Kartenzahl der Zielspalte **nach** dem Zug rechnen: dieselbe Spalte → unverändert, andere Spalte → plus eins
   - 3.3 `KartenlageValidator.Pruefe(lage, kartenzahlNachDemZug)` → `Pruefbefunde`
     - 3.3.1 Befunde vorhanden → `Ergebnis.Zurueckgewiesen` → HTTP 400 mit `Zurueckweisungen.Aus(...)`
4. **Zug schreiben (Datenzugriff)**
   - 4.1 `KartenRepository.Verschiebe` öffnet eine Transaktion und liest die Karte innerhalb dieser Transaktion
     - 4.1.1 Karte inzwischen verschwunden oder fremd → `null` → HTTP 404
   - 4.2 Quellspalte: die Karte herausnehmen, verbleibende Positionen auf 1..n-1 verdichten
   - 4.3 Zielspalte: Positionen ab der Zielposition um eins hochschieben, die Karte auf die Zielposition setzen
   - 4.4 Zurücklesen über `Kartenleser` und `Spaltenleser`; deckt der Bestand den Zug nicht mehr, `Ergebnis.Zurueckgewiesen` statt Commit — das Muster von `SpaltenRepository.SetzeReihenfolge`
   - 4.5 Commit; das Ergebnis sind die Spalten des Boards mit ihren Karten

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `KartenEndpunkte` — die neue Route liegt **am Board**, nicht unter einer Spalte (`/api/boards/{boardId}/karten/{karteId}/lage`). Ein Zug wechselt die Spalte; sie in der Route festzuhalten würde die Herkunft zur Adresse machen.
- `Spaltenbahnen.razor` — hier entsteht der Zustand „ein Zug läuft"; die Komponente rendert ohnehin alle Bahnen und ist damit der einzige Ort, der Herkunft und Ziel gleichzeitig kennt.
- `Karte.razor` — bis heute reine Anzeige; sie wird zum Bedienelement.
- `IKartenRepository` — die zweite Methode neben `LegeAn`; sie zieht `TestKartenRepository` und die Service-Tests nach.

**Klassen-Entwurf:**

- `Fehlerbefund` (Contract, DTO, immutable) — **ein** Grund, warum ein Aufruf nicht durchging, in der Form, die ein Agent allein benutzen kann. Drei Felder, alle Pflicht und alle nichtleer: `Code` stabil und maschinenlesbar, `Meldung` mit den konkreten Werten des Vorgangs, `Kompensation` als ausführbarer nächster Schritt mit Route.
  - `public record Fehlerbefund(string Code, string Meldung, string Kompensation)`
- `Zurueckweisung` (Contract, DTO, immutable) — der Rumpf **jeder** Fehlerantwort der API, bei 400 wie bei 404. Der Name bleibt: die API weist den Aufruf zurück, und ob wegen einer verletzten Regel oder wegen eines Dings, das es nicht gibt, sagen Statuscode und `Code` des Befunds. Zieht nach `Contracts/Fehler/` um.
  - `public record Zurueckweisung(IReadOnlyList<Fehlerbefund> Befunde)` — bisher `IReadOnlyList<string>`
- `Kartenlage` (Contract, DTO, immutable) — wohin eine Karte soll: Zielspalte und Zielposition. Die Karte selbst steht in der Route.
  - `public record Kartenlage(long SpalteId, int Position)`
- `KartenlageValidator` (Operation, statisch) — prüft eine Ziellage gegen die Zahl der Karten, die die Zielspalte nach dem Zug trägt, und formuliert je Befund Code, Meldung und Kompensationsaktion. Pure Logik ohne Seiteneffekte, nach dem Muster von `SpaltenreihenfolgeValidator`. Dass der Validator die Kompensationsaktion selbst kennt, ist Absicht: nur er weiß, welche Regel verletzt wurde und welcher Wert gültig gewesen wäre.
  - `public static Pruefbefunde Pruefe(Kartenlage lage, int kartenzahlNachDemZug)`
- `Nichtgefunden` (Operation, statisch, WebApi) — die eine Stelle, die aus „Board/Karte/Spalte gibt es nicht" eine 404-Antwort mit Rumpf macht. Existiert, weil dieselbe Antwort an sechs Stellen gebraucht wird und sechs handgeschriebene Varianten auseinanderlaufen würden.
  - `public static IResult Board(long boardId)` · `public static IResult Karte(long boardId, long karteId)` · `public static IResult Spalte(long boardId, long spalteId)`
- `Kartenzug` (Model, Blazor) — der laufende Zug: welche Karte und aus welcher Bahn. Eigener Typ statt zweier loser Felder, damit „kein Zug läuft" ein `null` ist und nicht eine Kombination aus zwei Nullwerten.
  - `public sealed record Kartenzug(long KarteId, long SpalteId)`
- `KartenService` (Integration) — bekommt die zweite Methode; verdrahtet Laden, Prüfen und Schreiben, ohne selbst eine Regel zu tragen.
  - `public Ergebnis<IReadOnlyList<Spalte>>? VerschiebeKarte(long boardId, long karteId, Kartenlage lage)`
- `IKartenRepository` / `KartenRepository` (Provider, Ressourcenzugriff) — schreibt den Zug in einer Transaktion und liest den neuen Stand zurück; `null` heißt „Karte oder Zielspalte gibt es nicht", die Zurückweisung heißt „der Bestand hat sich geändert".
  - `Ergebnis<IReadOnlyList<Spalte>>? Verschiebe(long boardId, long karteId, Kartenlage lage)`
- `KartenEndpunkte` (Integration, statisch) — die Lage-Route neben der Anlage-Route.
  - `routen.MapPut("/api/boards/{boardId:long}/karten/{karteId:long}/lage", VerschiebeKarte).WithName("KarteVerschieben")`
- `KartenApiKlient` (Integration, Blazor) — der HTTP-Weg der Oberfläche; übersetzt 400 in eine `Zurueckweisung` und 404 in eine feste Meldung, wie die vorhandene Methode.
  - `public Task<ApiErgebnis<IReadOnlyList<Spalte>>> VerschiebeKarte(long boardId, long karteId, Kartenlage lage)`

### Änderungen an bestehenden Klassen

- `Karte.razor` (+ `.razor.css`) — `draggable="true"`, `@ondragstart`, `@ondragend`; zwei neue `EventCallback`-Parameter für Beginn und Ende des Zugs. Die Darstellung bleibt, wie sie ist.
- `Spaltenbahnen.razor` (+ `.razor.css`) — hält den laufenden `Kartenzug`, rendert je Bahn die Ablagestellen und meldet den fertigen Zug als `EventCallback<...>` nach oben. Ablagestellen erscheinen nur, solange ein Zug läuft, und nur, wenn `IstBearbeitbar` **nicht** gesetzt ist.
- `Board.razor` — nimmt den Zug entgegen, ruft den Klienten, lädt das Board neu und zeigt Zurückweisung oder Ausfallmeldung. Der vorhandene `LadeBoard`-Pfad wird wiederverwendet.
- `KartenService`, `IKartenRepository`, `KartenRepository`, `KartenEndpunkte`, `KartenApiKlient` — je eine Methode mehr, siehe Grobentwurf.
- `TestKartenRepository` — die zweite Methode, samt Beobachterflag, damit ein Test beweisen kann, dass eine zurückgewiesene Lage **nicht** schreibt.
- `BoardSeite` (Seitenobjekt der E2E-Tests) — Locator für eine Karte je Bahn und für die Ablagestellen.

**Der Fehlervertrag, quer durch den Bestand** — das ist der spürbarste Teil dieser Anforderung, vergleichbar mit `Spalte.Karten` in `R00006`:

- `Pruefbefunde` (BL-Model) — trägt `Fehlerbefund` statt `string`. Jeder Ort, der heute eine Meldung hineinlegt, muss Code und Kompensationsaktion mitgeben; jeder Ort, der sie herausliest, bekommt ein Objekt statt einer Zeichenkette.
- `BoardAnlegenValidator`, `SpaltenValidator`, `SpaltenreihenfolgeValidator`, `KartenValidator` — je Befund Code und Kompensationsaktion formulieren. Ihre Unit-Tests prüfen künftig alle drei Felder, nicht den Meldungstext allein.
- `SpaltenRepository` — die beiden Zurückweisungen, die es selbst formuliert (`Spalte trägt noch Karten`, `Spaltenbestand hat sich geändert`), bekommen dasselbe.
- `BoardEndpunkte`, `SpaltenEndpunkte`, `KartenEndpunkte` — die **sechs** `Results.NotFound()` ohne Rumpf werden zu `Nichtgefunden.*`-Antworten mit Befund.
- `Zurueckweisungen.Aus` — übersetzt weiterhin `Pruefbefunde` in eine `Zurueckweisung`, jetzt ohne die Zeichenketten-Schleife.
- `Zurueckweisungsleser`, `ApiErgebnis`, `Kartenanlage.razor`, `Spaltenpflege.razor` (Blazor) — lesen und rendern `befund.Meldung` statt des nackten Strings. Die Oberfläche zeigt Code und Kompensationsaktion **nicht** an.
- Alle Integrations- und E2E-Tests aus `R00001`–`R00006`, die auf Befundtexte prüfen, werden auf die neue Form gezogen — **14 Testdateien** lesen heute `Befunde`.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KartenlageValidator` — Position 0 und negativ ergeben einen Befund; 1 und die Höchstposition sind ohne Befund; eine Position darüber ergibt einen Befund. Die Randwerte einzeln, nicht als Sammeltest. Je Befund wird geprüft, dass `Code` der vereinbarte ist, `Meldung` die gelieferte Position **und** die gültige Obergrenze nennt und `Kompensation` eine Route enthält.
- `KartenService.VerschiebeKarte` — gegen `TestSpaltenRepository` und `TestKartenRepository`: unbekanntes Board, fremde Karte und fremde Zielspalte reichen `null` durch; eine ungültige Position ergibt `Zurueckgewiesen` **ohne** Schreibzugriff (Beobachterflag); die Kartenzahl nach dem Zug wird für dieselbe Spalte anders gerechnet als für eine andere.
- `KartenApiKlient.VerschiebeKarte` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 wird Erfolg mit den Spalten, 400 wird Zurückweisung mit den Befunden aus dem Rumpf, 404 wird die feste Meldung. Diese Pfade sind über den Browser nicht auslösbar.

**Der Vertragstest:** Eine eigene Testklasse ruft **jede** Fehlerantwort **jedes** Endpunkts über `TestWebApi` ab — die sechs 404-Fälle und jede Zurückweisung — und weist für jede nach, dass der Rumpf mindestens einen Befund trägt und jeder Befund `Code`, `Meldung` und `Kompensation` nichtleer füllt. Das ist das Kriterium „keine Fehlerantwort mit leerem Rumpf" in prüfbarer Form; ohne diesen Test bliebe die Zusage eine Absichtserklärung.

**Integration:** `KartenRepository.Verschiebe` gegen eine `TemporaereDatenbank` — Zug in eine andere Spalte (beide Rechenbeispiele der Akzeptanzkriterien), Zug innerhalb derselben Spalte nach vorn und nach hinten, Zug auf die eigene Position, lückenlose Positionen in beiden Spalten danach, fremde Karte und fremde Zielspalte liefern `null`. `KartenEndpunkte` über `TestWebApi` — 200 mit den Spalten, 400 mit `Zurueckweisung`, 404 in den drei genannten Fällen, und `GET /api/boards/{boardId}` liefert danach den neuen Stand. Der Neustart-Test wird um eine verschobene Karte erweitert.

**E2E:** Eine Karte wird per Ziehen in eine andere Bahn gelegt und steht dort an der erwarteten Stelle (US-1); eine Karte wird innerhalb ihrer Bahn nach oben gezogen (US-2); nach einem Reload steht alles unverändert (US-3); eine über die API verschobene Karte erscheint nach dem Öffnen des Boards an ihrer neuen Stelle (US-4); ein außerhalb jeder Ablagestelle beendeter Zug lässt das Board unverändert (US-5). Gezogen wird über `Locator.DragToAsync`. Dazu laufen alle E2E-Tests aus `R00001` bis `R00006` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Berührt rückwirkend: `R00001`–`R00006` — der gehobene Fehlervertrag ändert ihre Endpunkte, Validatoren und Tests. Keine ihrer fachlichen Zusagen wird aufgehoben; die Form der Antwort ändert sich, nicht ihr Inhalt.
- Abhängig von: `R00006` (Karten anlegen und am Board sehen, erledigt) — ohne Karte gibt es nichts zu bewegen; `R00002` (Spalten gestalten, erledigt) — ohne zweite Spalte keinen Spaltenwechsel; `R00004` (Layout-Modus, erledigt) — die Arbeitsansicht, in der gezogen wird. In der WBS: `I0012` braucht `I0011` (grün).
- Blockiert: `I0013` (Erledigte Karten gebündelt sehen) und `I0028` (Änderung ohne Reload sehen) — beide nennen `I0012` in ihrer Spalte `Braucht`.
- Reihenfolge innerhalb der Anforderung: `F0021` (Fehlervertrag) → `F0019` (Verschieben) → `F0020` (Fehlerpfad des Zugs). Wer mit `F0019` beginnt, baut ein 404 ohne Rumpf und schreibt es danach um.

## Umfang

```
Karte verschieben (I0012) = 18 Bubbles: 14 Standard (23,2h), 4 unklar (8–16h).
Rest: 23,2h klar + 8–16h unklar · 3 von 18 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 18 Bubbles gruen (0 %) · 0 laufen · 18 offen
```

`I0012` ist seit dem 2026-09-03 vollständig bis zur Bubble geplant, in **drei** Slices — die Reihenfolge ist die der Spalte `Braucht`, nicht die der Nummern:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0021` Fehlerantworten für Agenten | B0096–B0102 (7) | 10h klar + 4–8h unklar | `I0011` |
| `F0019` Karte verschieben | B0085–B0092 (8) | 10,4h klar + 4–8h unklar | `F0021` |
| `F0020` Unmöglichen Zug zurückweisen | B0093–B0095 (3) | 2,8h klar | `F0019` |

**Der Fehlervertrag kostet ungefähr so viel wie das Verschieben selbst.** Das ist der Preis der Entscheidung, ihn nicht als eigene Anforderung zu führen; er steht hier, damit er nicht als Beifang durchgeht.

`F0021` kommt zuerst, weil `B0087` (Lage-Endpunkt) bereits ein 404 liefert — es soll nicht nackt gebaut und danach umgeschrieben werden. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Die vier unklaren Bubbles sind zwei Paare mit verschiedener Ursache: `B0090`/`B0092` tragen das Drag-and-Drop-Risiko (siehe „Risiko, das benannt gehört"), `B0097`/`B0101` sind reine Mengenarbeit — 15 Kompensationssätze und 14 Testdateien. Bei ihnen ist die Bandbreite Fleiß, nicht Unsicherheit.

## Offene Fragen

- ~~Wie wird eine Karte in der Oberfläche bewegt?~~ — entschieden am 2026-09-02: **ausschließlich Ziehen und Ablegen**, wie im Artboard gezeichnet. Kein zweiter Bedienweg über Knöpfe oder Menü. Die Folgen stehen unter „Notizen".
- ~~Wo gehört der neue Fehlervertrag hin?~~ — entschieden am 2026-09-02: **in diese Anforderung**, samt Nachziehen der bestehenden Endpunkte. Nicht als eigene Refactoring-Anforderung und nicht nur für die neue Route — zwei Fehlerformen nebeneinander wären für einen Agenten schlimmer als gar keine Änderung.

## Manuelle Vorbereitungstätigkeiten

- Keine. Das Schema aus `003-karten.sql` trägt `Spalte` und `Position` bereits; es gibt keine Migration und keinen Datenumbau.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist, dass das Board seit `R00006` Karten zeigt, aber keinen Fluss: eine Aufgabe, die begonnen wird, bleibt sichtbar im Rückstand liegen, und wer den Stand der Arbeit wissen will, muss ihn woanders nachschlagen — genau der Zustand, den die Vision als „das Board hinkt der Realität hinterher" benennt. Wenn die Lage einer Karte über eine Route veränderbar wird, die Oberfläche und Agent gleichermaßen benutzen, dann führt jeder Akteur den Stand dort, wo er ohnehin arbeitet: der Mensch zieht die Karte, der Agent ruft dieselbe Route auf, wenn er die Aufgabe erledigt hat. Damit hört das Nachtragen von Hand auf, und die Positionsdaten werden zum ersten Mal eine Aussage über die Wirklichkeit statt über die Eingabereihenfolge. Der Hebel liegt genau hier und nicht später: `I0013` gruppiert erledigte Karten nach Datum und `I0028` überträgt Änderungen live — beide setzen voraus, dass eine Karte ihre Spalte überhaupt wechseln kann, und beide wären ohne diesen Slice Übertragungen von etwas, das nie passiert. Er liegt auch nicht früher, weil ein Zug eine Karte braucht, und die entstand erst mit `R00006`.

Für den Fehlervertrag gilt dieselbe Kette einen Schritt weiter: Ein Agent, der einen Zug versucht und ein leeres 404 zurückbekommt, weiß nicht, ob das Board, die Karte oder die Spalte fehlte — er kann weder korrigieren noch berichten und fällt auf Raten oder Stehenbleiben zurück. Wenn jede Fehlerantwort Grund und nächsten Schritt mitliefert, kommt er allein weiter, und die Zusage „Was ein Mensch klicken kann, kann ein Agent aufrufen" hält auch dort, wo etwas schiefgeht — bisher hielt sie nur auf dem Erfolgsweg. Der Hebel sitzt am Vertrag und nicht an einzelnen Meldungen, weil ein Agent die API als **ein** Ding bedient: eine Route, die Codes liefert, neben fünf, die es nicht tun, zwingt ihn zu genau dem Sonderfallwissen, das die einheitliche Antwort ihm ersparen soll.

## Missing-Docs

- **HTML5-Drag-and-Drop unter Blazor Server.** Welche Ereignisse über SignalR laufen und welche der Browser allein behandelt, ist in der Blazor-Dokumentation nur verstreut belegt. Insbesondere: dass `@ondragover:preventDefault` ohne Handler keinen Rundlauf erzeugt, und wie sich `dragenter`/`dragleave` bei verschachtelten Ablagestellen verhalten. Vor dem Bauen mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`).
- **`Locator.DragToAsync` gegen HTML5-DnD.** Playwright dokumentiert die Methode, aber nicht, unter welchen Bedingungen sie die nativen `drag`-Ereignisse auslöst statt reiner Mausereignisse. Das ist die Voraussetzung dafür, dass dieser Slice überhaupt grün werden kann — siehe Notizen.

## Notizen

### Verworfene Alternativen

- **Ziehen und Ablegen plus Kartenmenü „Verschieben".** Zwei Bedienwege, davon einer ohne Zeigegerät bedienbar und im E2E deterministisch klickbar. Verworfen: doppelter Bedienweg und doppelter Testpfad für einen Slice, der eine Sache tut.
- **Nur Knöpfe je Karte (hoch/runter/links/rechts), wie die Spaltenpflege sie hat.** Kleinster Umfang, deterministische E2E. Verworfen: weicht sichtbar vom Artboard ab, das das verbindliche Zieldesign ist, und hätte das Ziehen als zweiten Slice nachgezogen.
- **Route unter der Herkunftsspalte** (`PUT /api/boards/{b}/spalten/{s}/karten/{k}`). Verworfen: ein Zug wechselt die Spalte; die Herkunft in der Adresse festzuhalten macht aus einer Bewegung eine Eigenschaft ihres Ausgangspunkts.
- **Vollständige Kartenreihenfolge je Spalte schicken**, wie `Spaltenreihenfolge` es tut. Verworfen: ein Spaltenwechsel berührt zwei Spalten und bräuchte zwei Listen; für einen Agenten ist „diese Karte, dorthin" die natürliche Aussage.
- **Antwort `204 No Content`.** Verworfen: die Spalten des Boards zurückzugeben erspart dem Aufrufer den zweiten Abruf und folgt dem, was `SetzeReihenfolge` schon tut.
- **Den Fehlervertrag als eigene Refactoring-Anforderung `R00008` vorziehen.** Sauberste Trennung — verworfen, weil es `I0012` hinter ein Refactoring schiebt, das ohne den ersten echten Agenten-Anwendungsfall entworfen würde.
- **Den Fehlervertrag nur für die Lage-Route.** Schnellster Weg zum Slice — verworfen: zwei Fehlerformen nebeneinander zwingen einen Agenten zu Sonderfallwissen über die eigene API.
- **`Zurueckweisung` in `Fehlerantwort` umbenennen**, weil ein 404 keine Prüfung zurückweist. Verworfen: der Name trägt auch so („die API weist den Aufruf zurück"), und eine Umbenennung quer durch Blazor und alle Tests kostet mehr, als sie an Klarheit bringt.
- **RFC 9457 Problem Details** als Rumpfformat. Verworfen: der Standard kennt kein Feld für die Kompensationsaktion, und seine englischen Pflichtfelder (`type`, `title`, `detail`) stünden quer zur deutschen Domänensprache (C06). Ein Agent liest ohnehin, was dasteht, nicht das Schema.
- **Werte als eigenes Feld** (`{"spalteId": 7, "gueltigBis": 4}`) statt in der Meldung. Verworfen: eine lose `Dictionary<string, object?>` in einem immutablen DTO (C08) ist schwach typisiert, und ein Sprachmodell liest die Werte im Satz genauso gut — die Meldung ist verpflichtet, sie zu nennen.

### Bewusst out of scope

- **Bedienung ohne Zeigegerät.** Mit der Entscheidung für reines Ziehen ist eine Karte per Tastatur nicht bewegbar. Das ist eine bewusste Wahl, keine Lücke; sie gehört, wenn sie kommt, in einen eigenen Slice.
- **Live-Übertragung des Zugs an andere Sichten** — das ist `I0028`, und sie braucht diesen Slice, nicht umgekehrt.
- **Kartenzahl in der Spaltenkopfzeile** (`I0004`), **Gruppierung der Abschlussspalte** (`I0013`) und **Archivieren** (`I0014`). Alle drei sind im Artboard gezeichnet und gehören anderen Slices.
- **Verschieben über Board-Grenzen hinweg.** Die WBS kennt es nicht; das Fertig-Kriterium spricht von Spalte und Position.
- **Ein maschinenlesbarer Katalog aller Fehlercodes** (etwa unter `/api/fehler` oder in einem OpenAPI-Dokument). Nützlich für einen Agenten, der die API erkundet, aber eine eigene Zusage — sie gehört zu `I0037` „Rohdaten über die API abrufen", nicht hierher.

### Risiko, das benannt gehört

Der einzige Beweis für die Oberfläche hängt an `Locator.DragToAsync`. Erweist sich das im Zusammenspiel mit Blazor Server als unzuverlässig, hat dieser Slice keinen zweiten Weg, grün zu werden — das ist der Preis der Entscheidung gegen einen Zweitbedienweg. Der Probe-Test aus „Missing-Docs" gehört deshalb an den Anfang der Umsetzung, nicht ans Ende.
