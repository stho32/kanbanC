---
id: R00013
status: Neu
datum: 2026-09-04
---

# R00013: Identität wählen

## Beschreibung

Wer die Oberfläche öffnet, wählt am Identitätsplatz der Kopfzeile aus, wer er ist. Ein Klick auf den Platz öffnet ein Popover „Ich bin …" mit den Kontributoren der Art `Mensch`; die Wahl trägt der Platz danach als Namen statt „nicht gewählt" und übersteht einen Reload desselben Tabs. Agenten und abgebildete Kontributoren stehen sichtbar unter einer Trennlinie, tragen die Plakette „nur API" bzw. „abgebildet" und sind nicht wählbar. Die Wahl ist **Browserzustand** — sie liegt in `sessionStorage`, nicht auf dem Server; die WebApi bekommt für sie keinen Endpunkt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Wer die Oberfläche öffnet, wählt aus, wer er ist"; und „niemand wählt deren Identität, um in ihrem Namen zu arbeiten."

**Abweichung von der Vision, ausdrücklich und nicht still:** Die Vision nennt `localStorage` (`R00000-vision.md:101-102`), diese Anforderung speichert in `sessionStorage`. Damit trägt **jeder Tab seine eigene Wahl**, und ein neu geöffneter Tab beginnt wieder bei „nicht gewählt". Motiv ist dasselbe, das schon die Gestaltungsvariante trägt: ein Rechner, mehrere Menschen — die „einmal je Browser"-Wahl wäre für zwei Menschen an einer Maschine die falsche. Vom Menschen entschieden; die Vision wird über `/vision fortschreiben` nachgezogen, **nicht** von dieser Anforderung (`Dokumentation/Planung/kanbanc.md:304`). Das Fertig-Kriterium der Interaction — „die Wahl überlebt einen Reload" — ist damit erfüllt; die weitergehende Lesart „einmal wählen je Browser" ist es nicht und stand auch nicht im Kriterium.

## Geschäftlicher Nutzen

Die Anwendung kennt heute keinen Handelnden. Kontributoren gibt es seit `R00011` als Datensätze, ändern lassen sie sich seit `R00012` — aber niemand ist einer von ihnen: der Identitätsplatz der Kopfzeile sagt seit `R00005` unverändert „nicht gewählt". Alles, was danach kommt und einen Urheber braucht, hängt daran: der Kommentar mit Kontributor und Zeitpunkt (`I0017`), der Timer, der für einen Kontributor läuft (`I0023`, `I0024`), der Verantwortliche an der Karte (`I0015`). Alle drei tragen `I0008` in ihrer Spalte `Braucht` — nicht als Formalie, sondern weil es ohne diese Wahl keine Antwort auf „wer war das" gäbe. Und die Sperre der zweiten Hälfte ist kein Beiwerk: eine Identitätswahl, in der jeder jeden anklicken kann, macht aus „wer war das" eine Vermutung.

## Funktionale Anforderungen

- Der Identitätsplatz der Kopfzeile wird zum Bedienelement: ein Klick öffnet und schließt ein Popover.
- Das Popover listet die Kontributoren der Art `Mensch` als wählbare Zeilen mit Kürzel und Name; die gewählte trägt einen Haken.
- Nach der Wahl trägt der Identitätsplatz den Namen des Gewählten statt „nicht gewählt".
- Die Wahl übersteht einen Reload desselben Tabs; ein unabhängig geöffneter Tab beginnt bei „nicht gewählt".
- Gespeichert wird **nur die `KontributorId`** — nie Name oder Art.
- Ein Umbenennen aus `I0007` zieht ohne erneute Wahl nach; eine gespeicherte `KontributorId`, die es nicht mehr gibt, führt zu „nicht gewählt", nicht zu einem Fehler.
- Kontributoren der Art `Agent` und `Abgebildet` stehen sichtbar unter einer Trennlinie, mit Plakette „nur API" bzw. „abgebildet", und sind weder mit der Maus noch mit der Tastatur wählbar.
- Eine Fußzeile des Popovers führt auf `/kontributoren`, damit ein fehlender Mensch dort angelegt werden kann.
- Ist die WebApi beim Laden der Kontributoren nicht erreichbar, bleibt der Identitätsplatz bei „nicht gewählt" stehen und bedienbar — keine Ausnahmeseite.
- Die Wahl wird **nicht erzwungen**: man kann als „nicht gewählt" weiterarbeiten. Der Zwang entsteht dort, wo er eine Folge hat (`I0023`, der Timer), nicht hier.

## Nicht-funktionale Anforderungen

- **Kernregel des Projekts:** `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL` (`CLAUDE.md`). Sie bleibt hier auch dadurch gewahrt, dass die Identitätswahl **kein Serverzustand** ist: es gibt keine Oberflächenfunktion ohne Endpunkt, weil es überhaupt keinen Endpunkt zu diesem Zustand gibt — weder in der Oberfläche noch in der API.
- **Kein neuer Endpunkt, kein Schema:** `KanbanC.WebApi`, `KanbanC.BL`, `KanbanC.Contracts` und `Persistenz/Migrationen/` bleiben unverändert. `FehlervertragTests` bekommt nichts zu prüfen, weil keine Route hinzukommt.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"; geprüft von `GestaltungsfundamentTests`).
- **Der Rahmen darf nicht reißen:** Kopfzeile und Popover stehen auf *jeder* Seite. Ein Ausfall der WebApi und ein werfender Interop-Aufruf enden in einer stehenden Kopfzeile, nicht in der Ausnahmeseite.
- **Bedienbarkeit:** Das Popover schließt mit `Escape` und mit einem Klick daneben; gesperrte Zeilen tragen `aria-disabled="true"` und sind nicht fokussierbar.
- **Erste JS-Interop-Nutzung des Repositoriums:** Vor dem produktiven Einsatz steht ein Probe-Test nach `~/.claude/skills/dependency-probe/SKILL.md` (`B0161`). Im Bestand gibt es bislang keinen `IJSRuntime`-Aufruf — nur das `@using Microsoft.JSInterop` in `Components/_Imports.razor:9` und das vom Gerüst mitgelieferte `ReconnectModal.razor.js`, das per `<script type="module">` geladen wird und kein Interop ist.
- **Benennung:** `Identitaetsspeicher`, `Identitaetswahl`, `Kontributor`, `Kontributorart` — kontexteindeutige deutsche Domänensprache, Bezeichner ohne echte Umlaute, UI-Texte und Meldungen mit (C06, C07).

## Akzeptanzkriterien

### Wählen am Identitätsplatz

- [ ] Ein Klick auf den Identitätsplatz der Kopfzeile öffnet ein Popover mit der Überschrift „Ich bin …"; ein zweiter Klick schließt es, ebenso `Escape` und ein Klick daneben.
- [ ] Das Popover zeigt **je Kontributor der Art `Mensch` eine wählbare Zeile** mit Kürzel und Name. Rechenbeispiel: `Stefan` (Mensch), `Nina Barth` (Mensch), `Claude-Agent` (Agent), `Maria Lenz` (abgebildet) angelegt → zwei wählbare Zeilen, zwei gesperrte.
- [ ] Ein Klick auf eine wählbare Zeile schließt das Popover, und der Identitätsplatz trägt danach den Namen des Gewählten statt „nicht gewählt".
- [ ] Beim erneuten Öffnen trägt genau die gewählte Zeile den Haken; eine zweite Wahl ersetzt die erste.
- [ ] Eine Fußzeile „Kontributor anlegen" führt auf `/kontributoren`.
- [ ] Gibt es keinen Kontributor der Art `Mensch`, zeigt das Popover keine leere Fläche, sondern die Fußzeile als den Weg, der weiterhilft.

### Die Wahl überlebt den Reload — je Tab

- [ ] Nach einem Reload desselben Tabs trägt der Identitätsplatz weiterhin den gewählten Namen; es ist keine erneute Wahl nötig.
- [ ] Ein **unabhängig geöffneter** zweiter Tab beginnt bei „nicht gewählt" — er erbt die Wahl des ersten nicht.
- [ ] Gespeichert ist ausschließlich die `KontributorId` unter dem Schlüssel `kanbanc.identitaet`; Name und Art stehen nicht im Browser-Speicher.
- [ ] Wird der gewählte Kontributor über `/kontributoren` oder `PUT /api/kontributoren/{kontributorId}` umbenannt, zeigt der Identitätsplatz nach dem nächsten Laden **den neuen Namen** — ohne erneute Wahl.
- [ ] Steht im Speicher eine `KontributorId`, die die WebApi nicht mehr liefert, zeigt der Identitätsplatz „nicht gewählt" — keine Fehlermeldung, keine Ausnahmeseite.
- [ ] Ist die WebApi beim Laden der Kontributoren nicht erreichbar, bleibt der Identitätsplatz bei „nicht gewählt" stehen und bleibt bedienbar; die übrige Seite erscheint wie bisher.

### Nicht wählbare Kontributoren sind sichtbar gesperrt

- [ ] Kontributoren der Art `Agent` und `Abgebildet` stehen **unter einer Trennlinie** im selben Popover — sichtbar, nicht ausgeblendet.
- [ ] Eine Agenten-Zeile trägt die Plakette „nur API", eine abgebildete die Plakette „abgebildet".
- [ ] Ein Klick auf eine gesperrte Zeile ändert den Identitätsplatz nicht und schließt das Popover nicht.
- [ ] Gesperrte Zeilen tragen `aria-disabled="true"` und sind mit der Tabulatortaste nicht erreichbar: wer sich vom letzten wählbaren Eintrag weiterbewegt, landet auf der Fußzeile, nicht auf einem Agenten.
- [ ] Rechenbeispiel: `Stefan` (Mensch), `Claude-Agent` (Agent), `Maria Lenz` (abgebildet) angelegt, `Stefan` gewählt; danach Klick auf `Claude-Agent` und Tabulatorlauf über das Popover → der Identitätsplatz trägt weiterhin `Stefan`.

### Der grüne Bestand bleibt grün

- [ ] Der Identitätsplatz behält die id `identitaet` und — solange nichts gewählt ist — den Wortlaut „nicht gewählt"; `RahmenE2ETests.cs:70` und `:102` bleiben **unverändert** grün.
- [ ] Das Chevron des Schalters ist ein SVG ohne Textinhalt, damit `ToHaveTextAsync("nicht gewählt")` weiterhin genau greift.
- [ ] Die Lageprüfung `RahmenE2ETests.cs:23-29` (Seitentitel und Identitätsplatz auf einer waagerechten Zeile, Titel links davon) bleibt unverändert grün. Sprengen die Randabstände des Bedienelements sie doch, ist das eine **benannte Änderung an grünem Bestand** und wird als solche entschieden — nicht als Testanpassung nebenbei.
- [ ] Alle E2E-Tests aus `R00001`–`R00012` laufen weiter; kein Test wird gelöscht oder abgeschwächt.

### Probe vor dem produktiven Einsatz

- [ ] Ein eigener Probe-Test belegt vor der ersten produktiven Nutzung: (1) zu welchem Zeitpunkt im Lebenszyklus ein `IJSRuntime`-Aufruf trägt (Annahme: schon in `OnInitializedAsync`, weil `App.razor:19` mit `prerender: false` rendert), (2) dass ein unabhängig geöffneter Tab den `sessionStorage` des ersten nicht erbt und wie er dafür geöffnet werden muss, (3) dass ein werfender Interop-Aufruf in einer stehenden Seite endet, nicht in der Ausnahmeseite.
- [ ] Der Probe-Test bleibt als Regressionsschutz stehen, wie `ZiehenUndAblegenProbeE2ETests` und `ZweiterBrowserkontextProbeE2ETests`.

## Betroffene Verzeichnisstruktur

- **Oberfläche — neu:** `Source/KanbanC.Blazor/Services/Identitaetsspeicher.cs`; `Source/KanbanC.Blazor/Components/Layout/Identitaetswahl.razor` (+ `.razor.css`).
- **Oberfläche — geändert:** `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor` (+ `.razor.css`), `Source/KanbanC.Blazor/Program.cs` (Registrierung des `Identitaetsspeicher`).
- **Tests (Dienstebene):** `Source/KanbanC.Blazor.Tests/Services/IdentitaetsspeicherTests.cs` (neu, gegen eine Attrappe des `IJSRuntime`) und ein passender Helfer unter `Source/KanbanC.Blazor.Tests/TestHelpers/` — genau die Ebene, für die dieses Projekt laut `CLAUDE.md` existiert.
- **E2E:** `Source/KanbanC.PlaywrightTests/Tests/SessionStorageProbeE2ETests.cs` (neu, Probe), `Tests/IdentitaetWaehlenE2ETests.cs` (neu), `PageObjects/Rahmen.cs` (geändert — Locator für Schalter, Popover, wählbare und gesperrte Zeilen).
- **Unberührt:** `Source/KanbanC.WebApi/`, `Source/KanbanC.BL/`, `Source/KanbanC.Contracts/` und `Persistenz/Migrationen/` — diese Anforderung fügt keine Route, kein DTO und keine Tabelle hinzu. `wwwroot/gestaltung.css` und `oberflaeche.css` bleiben ebenfalls unverändert; das Popover bringt seine Gestaltung in `Identitaetswahl.razor.css` mit, mit Werten aus dem Token-Sheet.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0002.dc.html`](../Dokumentation/Wireframes/D0002.dc.html) ist die Gestaltungsvorgabe; einschlägig ist **Variante C — Popover an der Kopfzeile** (Zeilen 318–386) mit dem Identitätsplatz als Pille samt Chevron, dem Popover „Ich bin …", den wählbaren Zeilen mit Kürzel und Haken, der Trennlinie, den beiden gesperrten Zeilen mit Plakette und der Fußzeile „Kontributor anlegen". Die Lesehilfe (`:392`) legt die drei Farbrollen fest. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Variante C ist **vom Menschen entschieden**, nicht im stillen Lauf geraten (`Dokumentation/Planung/kanbanc.md:303`, Begründung im Wireframe-Index `_wireframes.md:264-278`, Frage 4). Der ganzflächige Vorschirm (Variante B, `:240-315`) ist damit verworfen; `D0001` bleibt der Einstieg, `Main.dc.html` braucht keine neue Kante.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

### Ablauf

1. **Aufbau der Kopfzeile**
   - 1.1 `Kopfzeile` liest die gemerkte `KontributorId` über `Identitaetsspeicher.Lies()` (`sessionStorage`, Schlüssel `kanbanc.identitaet`)
   - 1.2 `KontributorenApiKlient.LadeAlle()`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
     - 1.2.1 Ausfall → Liste bleibt leer, der Platz bleibt bei „nicht gewählt" und bedienbar
   - 1.3 die gemerkte Id wird gegen die Liste aufgelöst
     - 1.3.1 Treffer → der Platz trägt den Namen
     - 1.3.2 keine Id oder unbekannte Id → „nicht gewählt"
2. **Wählen**
   - 2.1 Klick auf den Platz → Popover auf; `Escape` oder Klick daneben → zu
   - 2.2 die Liste wird in der Oberfläche geteilt: `Kontributorart.Mensch` über der Trennlinie als Schalter, `Agent` und `Abgebildet` darunter als gesperrte Zeilen
   - 2.3 Klick auf eine wählbare Zeile → `Identitaetsspeicher.Merke(kontributorId)`, Popover zu, Platz trägt den Namen
   - 2.4 Klick auf eine gesperrte Zeile → nichts; sie ist kein Schalter und trägt keinen Handler
3. **Reload**
   - 3.1 derselbe Tab → 1.1 findet die Id wieder, der Name steht sofort
   - 3.2 unabhängig geöffneter Tab → eigener `sessionStorage`, 1.1 findet nichts, „nicht gewählt"

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `Kopfzeile.razor:38` — aus dem statischen `<span class="kopfzeile-identitaet" id="identitaet">nicht gewählt</span>` wird ein Schalter mit Chevron. Der Platz steht schon; genau das ist das Motiv der Variante C.
- `Identitaetsspeicher` — die **eine Stelle**, an der die Identität dieses Browsers gelesen und geschrieben wird. Sie ist zugleich die Naht, aus der `I0017`, `I0023` und `I0024` später den Urheber ziehen (siehe „Abgrenzung").
- `Program.cs:22-25` — die Registrierung als `Scoped` neben den vier API-Klienten.

**Klassen-Entwurf:**

- `Identitaetsspeicher` (Integration, Blazor) — merkt, liest und vergisst die gewählte `KontributorId` im `sessionStorage` des Tabs. Abgelegt wird **nur die Id**; die Auflösung zum Namen gehört der Kopfzeile. Ein werfender Interop-Aufruf wird gefangen und als „keine Wahl" beantwortet, statt die Seite zu reißen.
  - `Task Merke(long kontributorId)`
  - `Task<long?> Lies()`
  - `Task Vergiss()`
- `Identitaetswahl` (UI-Komponente) — das Popover: wählbare Menschen über der Trennlinie, gesperrte darunter, Fußzeile auf `/kontributoren`. Bekommt die geladenen Kontributoren und die gewählte Id als Parameter und meldet die Wahl über einen `EventCallback` zurück; sie lädt selbst nichts.
  - `[Parameter] IReadOnlyList<Kontributor> Kontributoren`
  - `[Parameter] long? GewaehlteKontributorId`
  - `[Parameter] EventCallback<long> Gewaehlt`
- `Kopfzeile` (UI, **geändert**) — hält zusätzlich die Kontributorenliste, die gewählte Id und den offenen/geschlossenen Zustand des Popovers; zeichnet den Platz als Schalter mit Name oder „nicht gewählt".
- `Rahmen` (PageObject, **geändert**) — Locator für Schalter, Popover, wählbare Zeilen, gesperrte Zeilen und Fußzeile; `Identitaetsplatz` (`:30`) bleibt unverändert `#identitaet`.

### Änderungen an bestehenden Klassen

- `Kopfzeile.razor` (`:1-2`, `:38`) — kennt heute nur den `NavigationManager` und bekommt `KontributorenApiKlient` und `Identitaetsspeicher`. Die Liste wird **je Kreislauf einmal** geladen, nicht je Seitenwechsel: `OnInitializedAsync` lädt, `LocationChanged` zeichnet nur neu.
- `Kopfzeile.razor.css` (`:92-105`) — `.kopfzeile-identitaet` trägt heute reine Textdarstellung und bekommt die Zustände geschlossen / offen / gewählt; die Randabstände bleiben so klein, dass die Lageprüfung `RahmenE2ETests.cs:23-29` hält.
- `Program.cs` — eine Registrierung mehr.
- `Rahmen.cs` — Locator kommen hinzu; `Identitaetsplatz` bleibt unberührt.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Identitaetsspeicher` gegen eine Attrappe des `IJSRuntime` (in `KanbanC.Blazor.Tests`) — `Merke` ruft `sessionStorage.setItem` mit dem Schlüssel `kanbanc.identitaet` und der Id auf; `Lies` gibt die gemerkte Id zurück; ein leerer Speicher liefert `null`; ein nicht zahliger Wert liefert `null` statt einer Ausnahme; ein **werfender** Interop-Aufruf liefert `null` und reißt nichts. Diese Pfade sind über den Browser nicht auslösbar — genau der Grund, aus dem `KanbanC.Blazor.Tests` existiert.
- Die Aufteilung der Kontributorenliste in wählbar und gesperrt (Filter nach `Kontributorart`) — pure Logik, prüfbar ohne Browser; wächst sie über ein `Where` hinaus, bekommt sie eine eigene Operation.

**Integration:** keine. Diese Anforderung berührt weder Datenbank noch WebApi; `KontributorenEndpunkteTests`, `FehlervertragTests` und `WebApiNeustartTests` bekommen nichts hinzu.

**E2E:**
- `SessionStorageProbeE2ETests` (Probe, `B0161`) — Lebenszyklus, unabhängiger zweiter Tab, werfender Interop-Aufruf. Muster: `ZiehenUndAblegenProbeE2ETests`, `ZweiterBrowserkontextProbeE2ETests`.
- `IdentitaetWaehlenE2ETests` — Popover öffnen und schließen, wählen, Reload desselben Tabs (US-1, US-2), unabhängiger zweiter Tab (US-2), Umbenennen zieht nach (US-3), Ausfall der WebApi (US-4), gesperrte Zeilen mit Maus und Tastatur (US-5, US-6). Das Arrange legt je einen Kontributor der drei Arten über die API an (Muster: `KontributorenlisteE2ETests`).
- Bestand: `RahmenE2ETests` läuft **unverändert** mit.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten — hier gibt es davon nichts.

## Abhängigkeiten

- Abhängig von: **`R00011`** (Kontributor anlegen). Die WBS-Spalte `Braucht` von `I0008` nennt `I0006`; der Knoten ist `gruen` (`Dokumentation/Planung/kanbanc.md:144, 173`). Ohne Kontributoren gibt es nichts zu wählen.
- Setzt auf vorhandene Bausteine auf: `R00005` (Kopfzeile, Identitätsplatz, Token-Sheet), `R00006` (`WebApiAufruf.MitAusfallmeldung`), `R00011` (`KontributorenApiKlient.LadeAlle`, `Kontributorartform.Kuerzel`), `R00012` (Umbenennen — die Grundlage dafür, dass ein Name nachziehen *kann*).
- Ändert bestehende Klassen mit grünen Tests: `Kopfzeile.razor(.css)` und `Rahmen.cs`. Die drei Prüfungen aus `RahmenE2ETests` bleiben als **Auflage an die Gestaltung** unverändert (siehe Akzeptanzkriterien).
- Blockiert: **`I0017`** (Karte kommentieren), **`I0023`** und **`I0024`** (Timer) — alle drei nennen `I0008` in ihrer Spalte `Braucht` (`Dokumentation/Planung/kanbanc.md:242, 250`), weil sie einen Urheber brauchen. Sinngemäß auch `I0015` (Verantwortlicher), das über `I0006` hängt.
- Reihenfolge innerhalb der Anforderung: `B0161` (Probe) **vor** allem anderen — `dependency-probe` verlangt den Beweis vor dem produktiven Einsatz, und drei Entwurfsentscheidungen hängen an seinem Ergebnis. Danach `F0031` vor `F0032`; `F0032` nennt `F0031` in `Braucht`.

## Umfang

```
Identität wählen (I0008) = 9 Bubbles: 8 Standard (14,4h), 1 unklar (2-4h).
Rest: 14,4h klar + 2-4h unklar · 1 von 9 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 9 Bubbles gruen (0 %) · 0 laufen · 9 offen
```

`I0008` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0031` Identität wählen und behalten | B0161–B0167 (7) | 10,4h klar + 2-4h unklar | — |
| `F0032` Nicht wählbare Kontributoren sichtbar sperren | B0168, B0169 (2) | 4,0h klar | `F0031` |

Belegt ist allein `B0166` (Ausfall der WebApi im Identitätsplatz, 0,4h; Vergleichswert `B0041` in `Schaetzungen/_ist-zeiten.md`); die UI-, Dienst- und E2E-Bubbles tragen den Richtwert 2h ohne Messung. Unklar ist `B0161`, weil es die erste JS-Interop-Nutzung des Repositoriums ist — die Bandbreite bleibt sichtbar und wird nicht in eine Summe gerechnet. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Derselbe Vermerk wie bei `I0005` bis `I0007`, damit er nicht als Beifang durchgeht: die 2h-Richtwerte liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde auch hier nicht geändert, weil das die Zählung des ganzen Baums verschöbe; die Frage gehört einmal entschieden, nicht je Slice (`Dokumentation/Planung/kanbanc.md:311`).

## Offene Fragen

- ~~Vorschirm (Variante B) oder Popover an der Kopfzeile (Variante C)?~~ — entschieden: **Variante C**, vom Menschen. `D0001` bleibt der Einstieg, der Platz steht schon, und Umschalten mitten in der Arbeit ist ein Klick (`Dokumentation/Planung/kanbanc.md:303`). Der Preis von C — man kann versehentlich als „nicht gewählt" arbeiten — bleibt bestehen; das Gegenmittel ist der Timer in `I0023`, nicht dieser Slice.
- ~~`localStorage` oder `sessionStorage`?~~ — entschieden: **`sessionStorage`**, vom Menschen, **abweichend von der Vision** (siehe „Beschreibung"). Jeder Tab trägt seine eigene Wahl.
- ~~Bekommen die schreibenden Endpunkte jetzt einen Urheberschaftsparameter?~~ — entschieden: **nein, nicht in diesem Slice.** Kein schreibender Endpunkt hat heute ein Feld für den Handelnden, keine Tabelle eine Spalte dafür. Ein `kontributorId` an diesen Routen wäre tote Flexibilität (C17), die niemand auswertet, und kein ehrlicher Test bekäme sie grün, weil sie keine Zustandsänderung bewirkt (`test-ehrlichkeit`). Die Kernregel bleibt gewahrt, weil die Identitätswahl gar kein Serverzustand ist. Die Naht entsteht trotzdem schon jetzt: `Identitaetsspeicher` ist die eine Stelle, aus der `I0017`, `I0023` und `I0024` den Urheber ziehen (`Dokumentation/Planung/kanbanc.md:305`).
- ~~Bekommt `GET /api/kontributoren` einen Abfrageparameter `waehlbar`?~~ — entschieden: **nein**, gefiltert wird in der Oberfläche. Die Zielform zeigt Agenten und abgebildete Kontributoren *sichtbar und gesperrt* — ein serverseitiger Filter müsste umgangen oder zweimal aufgerufen werden; `Art` liegt ohnehin in jeder Antwortzeile; und „wählbar" ist eine Regel der Identitätswahl, die es auf dem Server nicht gibt. Das beantwortet die bei `I0006` offen gelassene Frage (`Dokumentation/Planung/kanbanc.md:289, 306`). Mit `I0009` bekommt sie einen echten Serverzustand und ist neu zu stellen; Muster wäre `archiviert` an `GET /api/boards`.
- ~~Wird Name oder Art mitgespeichert?~~ — entschieden: **nur die `KontributorId`**. Ein Umbenennen aus `I0007` zieht dadurch von selbst nach, und ein unbekannter Wert wird zu „nicht gewählt" statt zu einem verwaisten Namen (`Dokumentation/Planung/kanbanc.md:176`).
- **Offen geblieben, weil `I0009` noch rot ist:** Das Artboard nimmt **stillgelegte** Kontributoren ausdrücklich von der Wahl aus (`D0002.dc.html:308`). Das ist hier nicht baubar — es gibt den Zustand noch nicht. Gefiltert wird in diesem Slice allein nach `Kontributorart`. **Wenn `I0009` gebaut wird, muss die Filterung nachziehen**; ob das dort über `Braucht` festgehalten wird, ist nicht gesetzt (`Dokumentation/Planung/kanbanc.md:307`).
- **Offen geblieben, weil nicht Gegenstand dieses Slice:** ob die drei `.kuerzel-*`-Regeln aus `Kontributoren.razor.css:34-49` dauerhaft zweimal stehen (dort und in `Identitaetswahl.razor.css`) oder nach `oberflaeche.css` umziehen. Hier ist die zweite Regel gewählt — siehe „Angenommen im stillen Lauf" und „Verworfene Alternativen".

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- **`/vision fortschreiben`**: `R00000-vision.md:101-102` nennt `localStorage`; die Anwendung speichert nach dieser Anforderung in `sessionStorage`. Die Vision wird von der Familie `/vision` nachgezogen — nicht von dieser Anforderung und nicht von ihrer Umsetzung.
- Keine Migration, keine Datenbereinigung; bestehende Datenbanken bleiben unverändert.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist eine Zusage, die seit `R00005` sichtbar uneingelöst in der Kopfzeile steht: „nicht gewählt" — die Anwendung kennt Kontributoren, aber niemanden, der einer davon *ist*. Wenn der Identitätsplatz zum Bedienelement wird und die gewählte `KontributorId` im Browser liegt, bekommt jede Sitzung einen Handelnden; und weil nur die Id gespeichert wird, bleibt der Name eine Ableitung aus der Kontributorenliste statt einer zweiten Wahrheit, die beim ersten Umbenennen veraltet. Der Hebel sitzt genau hier und nicht später, weil `I0017`, `I0023` und `I0024` alle `I0008` in `Braucht` tragen: ohne diese Wahl gäbe es für Kommentar und Timer keinen Urheber, und man müsste ihn dort dreimal neu erfinden — mit dieser Wahl gibt es ihn einmal, an einer Stelle (`Identitaetsspeicher`). Der Hebel sitzt auch nicht *früher*, in Form eines Urheberschaftsparameters an den bestehenden schreibenden Endpunkten: den würde heute niemand auswerten, kein Test könnte ihn ehrlich prüfen, und er wäre bei der ersten echten Ablage anders zugeschnitten als geraten. Und dass die zweite Hälfte des Popovers sperrt statt zu verbergen, ist der Unterschied zwischen „ich weiß, wer sonst noch am Board schreibt" und „ich kann in seinem Namen arbeiten" — die Vision verlangt das erste ausdrücklich und schließt das zweite aus.

## Missing-Docs

- **Lebenszyklus von `IJSRuntime` unter `InteractiveServerRenderMode(prerender: false)`.** Ob ein Interop-Aufruf schon in `OnInitializedAsync` trägt oder erst in `OnAfterRenderAsync(firstRender)`, ist im Bestand nirgends belegt — es gibt bislang keinen einzigen Interop-Aufruf. `B0161` klärt es mit einem Probe-Test; das Ergebnis gehört danach dokumentiert, weil jede weitere Interop-Nutzung darauf aufsetzt.
- **Vererbung von `sessionStorage` an einen neuen Tab.** Ein per Verweis oder `window.open` geöffneter Tab erbt den `sessionStorage` des Öffners, ein unabhängig geöffneter nicht. Wie Playwright einen *unabhängigen* Tab öffnet (`Context.NewPageAsync` gegenüber `Page.RunAndWaitForPopupAsync`) und was davon im verwendeten Browser tatsächlich gilt, ist nicht belegt und entscheidet, wie `B0167` seinen zweiten Tab aufmacht.
- **Klick-daneben und `Escape` ohne JS-Framework.** Der Bestand kennt genau ein aufklappendes Menü — `Boardkachel.razor:129-132` —, und das schließt **nur** über seinen eigenen Schalter. Wie ein Popover in Blazor Server ohne Fremdbibliothek zuverlässig auf einen Klick daneben und auf `Escape` reagiert (unsichtbare Auffangfläche, `@onkeydown` auf dem fokussierten Element, oder Interop auf `document`), ist im Repository nicht belegt.

## Notizen

### Verworfene Alternativen

- **Variante B — ganzflächiger Vorschirm vor dem Board.** Deutlicher: niemand arbeitet je versehentlich als „nicht gewählt". Verworfen (vom Menschen entschieden): ein Schirm vor jedem Blick aufs Board ist teuer, `D0001` wäre nicht mehr der Einstieg, und bei einem Rechner für mehrere Menschen ist die einmalige Wahl die falsche.
- **Die Wahl auf dem Server halten** — ein Endpunkt `PUT /api/identitaet` und eine Spalte. Verworfen: Full-Trust ohne Authentifizierung heißt, dass der Server nicht weiß, *wessen* Identität er da hielte; ein globaler Serverzustand „die aktuelle Identität" wäre bei zwei offenen Browsern schlicht falsch. Die Wahl gehört dem Browser.
- **`localStorage` statt `sessionStorage`.** Näher an der Vision und bequemer für einen Menschen an seinem eigenen Rechner. Verworfen (vom Menschen entschieden) für den Fall, der hier realistisch ist: ein Rechner, mehrere Menschen. Die Vision wird nachgezogen statt überschrieben.
- **Name und Art mitspeichern**, um die Kopfzeile ohne Laden zeichnen zu können. Verworfen: das wäre eine zweite Wahrheit über den Namen, die beim ersten Umbenennen veraltet — und `R00012` hat das Umbenennen gerade erst möglich gemacht.
- **Gesperrte Kontributoren ausblenden statt sperren.** Einfacher und ohne Plakette. Verworfen: sichtbar bleiben sie, damit erkennbar ist, wer am Board mitschreibt; das Artboard zeichnet sie ausdrücklich sichtbar und abgeblendet (`D0002.dc.html:370-374`).
- **Einen Urheberschaftsparameter jetzt schon an allen schreibenden Endpunkten einführen.** Wäre „vorbereitet". Verworfen als tote Flexibilität (C17) ohne ehrlichen Test — siehe „Offene Fragen".
- **Die `.kuerzel-*`-Regeln nach `oberflaeche.css` heben**, damit Liste und Popover dieselbe Quelle haben. Die sauberere Stelle, aber ein Eingriff in die CSS von `R00011`/`R00012` und damit in grünen Bestand — und die beiden Kürzel sind nicht dasselbe: das Popover zeichnet sie kleiner als die Liste (C24, DRY nur bei semantischer Äquivalenz). Für diesen Slice verworfen; gehört als `/anforderung refactoring` gestellt, falls eine dritte Stelle hinzukommt.

### Bewusst out of scope

- **Der Urheberschaftsparameter an schreibenden Endpunkten** (`I0017`, `I0023`, `I0024`, sinngemäß `I0015`). Er entsteht dort, wo der Urheber zum ersten Mal abgelegt und gelesen wird; hier entsteht nur die Naht, aus der er gezogen wird.
- **Stilllegen und Zurückholen** (`I0009`) samt der Regel, dass Stillgelegte nicht wählbar sind.
- **Ein Zwang zur Wahl.** Man kann als „nicht gewählt" weiterarbeiten; der Timer (`I0023`) erzwingt sie, wo sie eine Folge hat.
- **Ein Abmelden / „Identität vergessen"** als Bedienelement. `Identitaetsspeicher.Vergiss` entsteht, weil `Merke`/`Lies` ohne Gegenstück eine halbe Schnittstelle wären und der Probe-Test das Zurücksetzen braucht; einen Menüpunkt dafür gibt es nicht — die Wahl wird durch eine andere ersetzt.
- **Übertragung der Wahl an andere offene Sichten** (`I0028`) — die Wahl ist Browserzustand und wandert nirgendwohin.
- **Ein Bild oder Avatar je Kontributor.** Das Artboard zeichnet Kürzel, keine Bilder.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Neben den Entscheidungen unter „Offene Fragen" stehen sechs Annahmen mit Beleg:

1. **Der Schlüssel im `sessionStorage` heißt `kanbanc.identitaet`** und trägt die `KontributorId` als Text. So steht es im Entwurf der Bubble (`Dokumentation/Planung/kanbanc.md:176`); ein Präfix, damit die Anwendung ihren eigenen Namensraum hat.
2. **Wählbare Zeilen sind `<button type="button">`, gesperrte sind es nicht.** Ein `<button disabled>` wäre die naheliegende Alternative, liest sich aber als „hier fehlt etwas"; eine Zeile ohne Schalterrolle, mit `aria-disabled="true"` und ohne `tabindex`, ist nicht fokussierbar und trägt keinen Handler — damit ist „weder mit der Maus noch mit der Tastatur" eine prüfbare Aussage.
3. **Die Kontributorenliste wird je Blazor-Kreislauf einmal geladen, nicht je Seitenwechsel.** `Kopfzeile` zeichnet bei `LocationChanged` nur neu (`Kopfzeile.razor:56-60`); ein Laden je Seitenwechsel wäre ein HTTP-Aufruf für jede Navigation. Folge: ein Umbenennen zieht beim nächsten *vollen* Laden nach, nicht mitten in der Sitzung — was zum Fertig-Kriterium passt („ohne erneute Wahl"), nicht zu einer Live-Übertragung (`I0028`).
4. **Ein werfender Interop-Aufruf wird im `Identitaetsspeicher` gefangen** und als „keine Wahl" beantwortet, statt nach oben zu laufen. Der Rahmen steht auf jeder Seite; ein gesperrter Browser-Speicher darf nicht die ganze Anwendung reißen. Muster und Motiv wie bei `WebApiAufruf.MitAusfallmeldung` (`Source/KanbanC.Blazor/Services/WebApiAufruf.cs:5-16`).
5. **Das Popover bringt seine Gestaltung in `Identitaetswahl.razor.css` mit**, mit Werten aus dem Token-Sheet; `Kontributorartform.Kuerzelklasse` (`Source/KanbanC.Blazor/Services/Kontributorartform.cs:39-52`) bleibt die eine Quelle für die Zuordnung Art → Klassenname. Die drei Farbregeln stehen dadurch ein zweites Mal — siehe „Verworfene Alternativen".
6. **Gibt es keinen wählbaren Menschen, zeigt das Popover die Fußzeile „Kontributor anlegen" als den Weg, der weiterhilft** — keine leere Fläche und kein zusätzlicher Satz. Das Artboard zeichnet diesen Rand nicht; die Fußzeile ist ohnehin da und beantwortet genau diese Lage.

Wer eine dieser Annahmen anders will, ändert sie vor dem Bauen — nach `B0163` kostet die Frage nach der Schalterrolle einen zweiten Umbau an Kopfzeile, CSS und Seitenobjekt.
