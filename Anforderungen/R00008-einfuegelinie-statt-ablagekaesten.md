---
id: R00008
status: Erledigt
datum: 2026-09-03
---

# R00008: Einfügelinie statt Ablagekästen

## Beschreibung

Beim Ziehen einer Karte zeigt das Board nicht mehr eine Reihe beschrifteter Kästen, sondern **eine schmale Linie** an der Stelle, an der die Karte landen würde. Abgelegt wird auf der Karte, die man gerade überfährt — obere Hälfte heißt davor, untere Hälfte dahinter. Die Fläche unter der letzten Karte einer Bahn nimmt ganzflächig an und hängt die Karte ans Ende; eine leere Bahn ebenso.

Das ist die zweite Fassung derselben Bedienung: Verschieben konnte das Board seit `R00007`. Neu ist, wie ruhig es dabei bleibt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Visuelle Haltung an Kanbanflow orientiert".

## Geschäftlicher Nutzen

Die bisherigen Ablagestellen sind 35 px hohe Kästen, und es gibt je Bahn einen mehr als Karten. Sobald ein Zug beginnt, erscheinen sie alle gleichzeitig und schieben jede Bahn auseinander: das Board springt in dem Moment, in dem der Nutzer zielen will. Er zielt damit auf ein Layout, das sich gerade verändert hat.

Eine Einfügelinie sagt dasselbe, ohne etwas zu verschieben — das Board bleibt stehen, während die Karte wandert. Dazu kommt die große Fläche unter der letzten Karte: „ans Ende" ist der häufigste Zug auf einem Kanban-Board und braucht kein 35-px-Ziel, das man treffen muss.

## Funktionale Anforderungen

- Während eines Zugs erscheint eine schmale Linie an der Stelle, an der die Karte landen würde — sie folgt dem Zeiger und verdrängt keine Karten.
- Drop-Ziel ist die überfahrene Karte: obere Hälfte legt davor ab, untere Hälfte dahinter.
- Die Fläche unter der letzten Karte einer Bahn nimmt die Karte an und hängt sie ans Ende.
- Eine leere Bahn nimmt über ihre gesamte Fläche an.
- Die beschrifteten Ablagekästen samt Positionsangabe entfallen ersatzlos.
- Was die Karte am Ende tut — Spalte und Position wechseln, Reload-fest, lückenlose Positionen — bleibt unverändert; diese Anforderung ändert nur, wie gezielt wird.

## Nicht-funktionale Anforderungen

- Performance: `dragover` läuft weiterhin ohne Serverbeteiligung. Je überfahrener Hälfte höchstens ein Ereignis über den Live-Kanal — nicht mehr als bei den bisherigen Ablagestellen.
- Benutzerfreundlichkeit: Das Layout der Bahnen darf sich durch den Beginn eines Zugs **nicht** verändern. Karten behalten Position und Größe; die Linie liegt im Zwischenraum.

## Akzeptanzkriterien

### Die Einfügelinie

- [x] Während eines Zugs ist genau **eine** Einfügelinie sichtbar — die an der Stelle, die das aktuelle Ziel ist; nicht mehrere gleichzeitig.
- [x] Die Linie trägt **keine Beschriftung**; die Zeichenkette „hier ablegen" kommt in der Oberfläche nicht mehr vor.
- [x] Beginnt ein Zug, ändern die Karten einer Bahn ihre Position auf dem Schirm nicht — gemessen an der Kartenreihenfolge und daran, dass keine Karte aus dem sichtbaren Bereich rückt.
- [x] Endet der Zug ohne Ablegen, verschwindet die Linie und das Board ist unverändert.

### Ablegen auf einer Karte

- [x] Wird über der **oberen** Hälfte einer Karte losgelassen, landet die gezogene Karte **davor**.
- [x] Wird über der **unteren** Hälfte losgelassen, landet sie **dahinter**.
- [x] Rechenbeispiel, Zug aus einer anderen Bahn: Zielbahn `[A, B, C]`. Über `A` oben → Position 1, über `A` unten → 2, über `C` unten → 4.
- [x] Rechenbeispiel, Zug **innerhalb** derselben Bahn: `[A, B, C, D]`, gezogen wird `D`. Über `A` oben → `[D, A, B, C]`; über `B` unten → `[A, B, D, C]`. Gezogen wird `A`: über `C` unten → `[B, C, A, D]`.
- [x] Wird eine Karte über sich selbst losgelassen, ändert sich die Reihenfolge nicht.

### Restfläche und leere Bahn

- [x] Die Fläche zwischen der letzten Karte und dem Bahnenfuß nimmt an; die Karte landet als letzte der Bahn.
- [x] Rechenbeispiel: Zielbahn `[A, B, C]`, Zug aus einer anderen Bahn auf die Restfläche → Position 4. Wird `A` aus derselben Bahn auf die Restfläche gezogen → `[B, C, A]`.
- [x] Eine Bahn ohne Karten nimmt über ihre gesamte Fläche an; die Karte wird ihre erste.
- [x] Der Leer-Hinweis „Noch keine Karte" bleibt sichtbar und verhindert das Ablegen nicht.

### Was entfällt

- [x] Es gibt keine `n+1` Ablagestellen je Bahn mehr; das Element mit der Klasse `ablagestelle` existiert nicht mehr.
- [x] Im Layout-Modus (`R00004`) sind weiterhin weder Karten ziehbar noch Ablageziele aktiv.

### Bestandsschutz

- [x] Alle Akzeptanzkriterien aus `R00007` gelten unverändert weiter: Spalten- und Positionswechsel über die API, lückenlose Positionen, Reload- und Neustartfestigkeit, Zurückweisung und Ausfallmeldung. **An API, Fachlogik und Datenhaltung ändert diese Anforderung nichts.**
- [x] Die sechs E2E-Tests aus `R00007`, die heute über `BoardSeite.AblagestelleDerBahn` zielen, werden auf die neue Bedienung gezogen — **keine ihrer fachlichen Aussagen entfällt**.
- [x] Alle übrigen Tests aus `R00001`–`R00007` bleiben grün; `TreatWarningsAsErrors` bleibt aktiv, der Bau warnungsfrei.

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Services/Ablagestellen.cs` (die Positionsrechnung wird ersetzt), `Source/KanbanC.Blazor/Components/Karten/Karte.razor` (+ CSS — die Karte wird Ablageziel), `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (+ CSS — Linie und Restfläche statt Ablagestellen).
- **Tests:** `Source/KanbanC.Blazor.Tests/Services/AblagestellenTests.cs`, `Source/KanbanC.PlaywrightTests/PageObjects/BoardSeite.cs` und `Tests/KarteVerschiebenE2ETests.cs`.
- **Unberührt:** `KanbanC.BL`, `KanbanC.WebApi`, `KanbanC.Contracts` — kein Endpunkt, kein Vertrag, kein Schema. Ebenso `wwwroot/gestaltung.css`: die Anforderung nutzt die Tokens, sie ändert sie nicht.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0003.dc.html`](../Dokumentation/Wireframes/D0003.dc.html) (Stand zurückgeholt und nachgezogen am 2026-09-03) zeigt den Zustand „ein Zug läuft" in der Bahn *In Arbeit*: die Einfügelinie zwischen zwei Karten und die annehmende Restfläche darunter. Die Lesehilfe am Fuß hält fest, dass eine leere Bahn ganzflächig annimmt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung** — aus ihm entstehen keine Akzeptanzkriterien. Die konkreten Werte (Strichstärke, Deckkraft der Restfläche) stehen dort und im Token-Sheet; ob die Linie am Ende 2 oder 3 px trägt, entscheidet sich beim Bauen am Bild, nicht an diesem Dokument.

Unverändert gilt die Abweichung aus `R00007`: die schwebende gedrehte Karte wird nicht gebaut, die Herkunftskarte bleibt gedimmt.

### Ablauf

1. **Zug beginnt** — unverändert: `Karte.razor` meldet `ZugBegann`, `Spaltenbahnen.razor` merkt sich den `Kartenzug`.
2. **Zielen**
   - 2.1 Jede Karte trägt während eines Zugs zwei Ablagezonen (obere und untere Hälfte) mit `@ondragover:preventDefault` und `@ondragenter`
   - 2.2 `@ondragenter` meldet Zielkarte und Hälfte nach oben; die Bahn zeichnet die Linie an der entsprechenden Fuge
   - 2.3 Die Restfläche der Bahn ist eine dritte Zone; sie zielt ans Ende
3. **Ablegen**
   - 3.1 `@ondrop` an der getroffenen Zone rechnet über `Ablagestellen` die Zielposition
   - 3.2 Ab hier unverändert: `Kartenablage` nach oben, `Board.razor` ruft `KartenApiKlient.VerschiebeKarte`, Board neu laden
4. **Zug endet ohne Ablegen** — `ZugEndete` räumt den Zustand; Linie verschwindet

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `Ablagestellen` — die Positionsrechnung ist der Kern der Änderung. Bisher `(Stelle, Stelle der gezogenen Karte)`, künftig `(Index der Zielkarte, Hälfte, Index der gezogenen Karte)`.
- `Karte.razor` — bisher nur Ziehquelle, künftig auch Ablageziel. Die zwei Hälften vermeiden, die Mausposition gegen die Kartengeometrie rechnen zu müssen (das bräuchte JS-Interop).
- `Spaltenbahnen.razor` — hier verschwinden die `n+1` Ablagestellen; es bleibt ein Zustand „welche Fuge ist gerade Ziel".

**Klassen-Entwurf:**

- `Kartenhaelfte` (Model, Blazor) — welche Hälfte einer Karte überfahren wird. Ein Aufzählungstyp statt eines `bool`, weil `true` an der Aufrufstelle nicht sagt, welche Hälfte gemeint ist.
  - `public enum Kartenhaelfte { Oben, Unten }`
- `Ablageziel` (Model, Blazor, immutable) — die Fuge, auf die gerade gezielt wird: Bahn und Fugennummer. „Kein Ziel" ist `null`.
  - `public sealed record Ablageziel(long SpalteId, int Fuge)`
- `Ablagestellen` (Operation, statisch) — rechnet aus Zielkarte, Hälfte und der Lage der gezogenen Karte die Position nach dem Zug. Die Korrektur bei einem Zug innerhalb derselben Bahn bleibt: liegt die gezogene Karte vor der Fuge, rückt die Zielposition um eins vor.
  - `public static int Zielposition(int indexDerZielkarte, Kartenhaelfte haelfte, int? indexDerGezogenenKarte)`
  - `public static int ZielpositionAmEnde(int kartenzahl, int? indexDerGezogenenKarte)`

### Änderungen an bestehenden Klassen

- `Ablagestellen` — `Zielposition(int stelle, int? stelleDerGezogenenKarte)` entfällt und wird durch die zwei Methoden oben ersetzt. Der bestehende Kommentar über die `n+1` Stellen wird gegenstandslos.
- `Karte.razor` (+ `.razor.css`) — zwei Ablagezonen als Kinder des `article`, nur während eines Zugs aktiv; zwei neue `EventCallback`-Parameter für „Hälfte überfahren" und „hier ablegen". Die Darstellung der Karte selbst bleibt.
- `Spaltenbahnen.razor` (+ `.razor.css`) — das `RenderFragment Ablagestelle` und `Ablagestellenklassen` entfallen; neu sind die Einfügelinie zwischen den Karten, die annehmende Restfläche und der Zustand `Ablageziel?`. `.ablagestelle` und `.ablagestelle-ueberfahren` weichen `.einfuegelinie` und `.ablageflaeche`.
- `BoardSeite` (Seitenobjekt) — `Ablagestellen`, `AblagestelleDerBahn(bahn, stelle)` entfallen; neu: Locator für die Kartenhälften, die Restfläche und die Einfügelinie. `ZieheKarteAuf` bleibt in seiner Mechanik (der Zug wird offen gehalten, weil die Zonen erst nach einem Rundlauf über den Live-Kanal erscheinen).
- `KarteVerschiebenE2ETests` — die sechs Tests, die heute eine Ablagestelle ansteuern, zielen künftig auf eine Kartenhälfte oder die Restfläche.
- `AblagestellenTests` — auf die neue Signatur gezogen; die Randfälle der Korrektur bei gleicher Bahn bleiben erhalten.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Ablagestellen.Zielposition` — beide Rechenbeispiele der Akzeptanzkriterien einzeln: Zug aus fremder Bahn (obere/untere Hälfte, erste/letzte Karte) und Zug innerhalb derselben Bahn (gezogene Karte vor und hinter der Fuge). Die Randwerte einzeln, nicht als Sammeltest.
- `Ablagestellen.ZielpositionAmEnde` — Bahn mit und ohne die gezogene Karte.

**Integration:** keine. Diese Anforderung fasst weder API noch Datenhaltung an — das ist selbst eine prüfbare Aussage: die Integrationstests aus `R00007` laufen unverändert.

**E2E:** Linie erscheint während eines Zugs und verschwindet danach (Kriteriengruppe 1); Ablegen auf oberer und unterer Hälfte mit beiden Rechenbeispielen (Gruppe 2); Restfläche und leere Bahn (Gruppe 3); Layout-Modus bleibt ohne Ablageziele (Gruppe 4). Dazu die sechs nachgezogenen Tests aus `R00007`.

## Abhängigkeiten

- Abhängig von: `R00007` (Karte verschieben, erledigt) — sie hat das Verschieben gebaut; diese Anforderung ersetzt nur dessen Zielhilfe. In der WBS: `F0022` braucht `F0019` (grün).
- Blockiert: nichts.

## Umfang

```
Einfügelinie statt Ablagekästen (F0022) = 5 Bubbles: 4 Standard (6,4h), 1 unklar (2–4h).
Rest: 6,4h klar + 2–4h unklar · 1 von 5 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 5 Bubbles gruen (0 %) · 0 laufen · 5 offen
```

`F0022` ist die **Ausbaustufe 2** der Ziehbedienung; `F0019` bleibt als Stufe 1 grün stehen. Die Bubbles `B0103`–`B0107` sind die Vorplanung — welche es am Ende werden, entscheidet der Entwickler beim Bauen.

Die eine unklare Bubble ist `B0107`: nicht die Technik ist offen, sondern wie viel am Nachzug der sechs bestehenden E2E-Tests hängt, wenn `AblagestelleDerBahn` entfällt.

## Offene Fragen

- ~~Wohin fällt die Karte, wenn man über einer Karte loslässt?~~ — entschieden am 2026-09-03: **obere Hälfte davor, untere dahinter**. Die Alternative „immer davor" macht das Anhängen ans Ende ohne die Restfläche unerreichbar.
- ~~Bleibt die Positionsangabe erhalten?~~ — entschieden am 2026-09-03: **entfällt ersatzlos**. Eine Linie trägt keinen Text, und die Stelle ist am Bild ablesbar.
- ~~Wie verhält sich eine leere Bahn?~~ — entschieden am 2026-09-03: **nimmt über die gesamte Fläche an**, wie die Restfläche einer gefüllten Bahn.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist ein Verhalten, das erst am laufenden Board auffiel: Sobald man eine Karte anhebt, erscheinen in jeder Bahn `n+1` Kästen à 35 px, und alle Karten rutschen nach unten. Der Nutzer zielt damit auf ein Layout, das sich in dem Moment verändert hat, in dem er zu zielen begann — das Board springt, statt ruhig zu bleiben. Wenn das Ablageziel statt eines Kastens eine Linie im Zwischenraum ist und die Karten selbst die Ziele werden, verschiebt der Beginn eines Zugs nichts mehr; der Nutzer zielt auf das, was er ohnehin sieht. Über die große Restfläche wird zusätzlich der häufigste Zug — „ans Ende dieser Spalte" — vom Treffer eines schmalen Streifens zu einer Geste, die kaum danebengehen kann. Der Hebel sitzt genau hier und nicht an der Kartengröße oder der Bahnenbreite: nicht die Dichte des Boards ist das Problem, sondern dass die Zielhilfe selbst das Ziel bewegt.

## Missing-Docs

- **`dragenter`/`dragleave` bei verschachtelten Ablagezonen unter Blazor Server.** Zwei Hälften innerhalb eines `article`, das selbst Ziehquelle ist — welche Ereignisse in welcher Reihenfolge feuern und ob `dragleave` beim Wechsel zwischen den Hälften ein zwischenzeitliches „kein Ziel" erzeugt, ist nicht belegt. Das entscheidet, ob die Linie flackert. Vor dem Bauen mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`); die Probe aus `R00007` (`ZiehenUndAblegenProbeE2ETests`) ist die Vorlage.

## Notizen

### Verworfene Alternativen

- **Mausposition gegen die Kartengeometrie rechnen** (`clientY` gegen `getBoundingClientRect`). Genauer als zwei Zonen, braucht aber JS-Interop für die Geometrie — eine neue Abhängigkeitsart in einer Oberfläche, die bisher ohne auskommt.
- **Nur die obere Hälfte auswerten** („immer davor einfügen"). Ein Ereignis weniger je Karte, aber das Anhängen ans Ende wäre nur über die Restfläche möglich — und in einer vollen Bahn gibt es keine.
- **Die Ablagestellen behalten und nur schmaler machen.** Kleinster Eingriff, löst das Problem aber nicht: auch ein 8-px-Kasten je Fuge verschiebt beim Erscheinen alle Karten.
- **Die Linie über der Karte einblenden statt im Zwischenraum** (absolut positioniert). Verschiebt garantiert nichts, überdeckt aber Karteninhalt und liegt bei der ersten und letzten Fuge halb außerhalb der Bahn.

### Bewusst out of scope

- **Automatisches Scrollen der Bahn**, wenn man eine Karte an ihren oberen oder unteren Rand zieht. Fällt auf, sobald eine Bahn länger als der Schirm ist — eigener Slice.
- **Bedienung ohne Zeigegerät** — unverändert out of scope, wie in `R00007` festgehalten.
- **Verschieben über Board-Grenzen hinweg** — die WBS kennt es nicht.
