---
id: R00036
status: In Arbeit
datum: 2026-09-07
---

# R00036: Soll-Ist-Vergleich abrufen

## Beschreibung

Für einen Kartenbestand — Board und Kartenklasse zusammen — steht die **erfasste Zeit** der **WBS-Zählung** gegenüber: je Karte Nummer, Titel, Ist, Sollband und Abweichung, darunter eine Summenzeile über den Bestand. Der Vergleich kommt über `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/soll-ist` **gerechnet** aus der API und wird auf dem neuen Schirm `/auswertungen` gezeigt.

Damit es überhaupt ein Soll gibt, schließt dieser Slice die **geerbte Lücke**: die Karte bekommt ein Sollband, und der Import füllt es aus der Spalte `Aufwand` der WBS-Datei.

Zahlt ein auf: [Vision](R00000-vision.md) — „Auswertungen aus vollständigen Daten. Soll-Ist-Vergleich gegen die WBS-Zählung".

**Der Befund, der diesen Slice trägt: im Bestand gibt es keine Sollzeit — und das ist zweimal ausdrücklich entschieden worden, nicht vergessen.**

| Wer | Was er festgehalten hat | Wo |
|---|---|---|
| `I0026` (`R00028`) | ein Feld hier zu erfinden nähme `I0033` die Entscheidung vorweg — **tote Flexibilität** (C24) | Zeitenblock der Kartenseite |
| `I0030` (`R00033`) | die Spalte `Aufwand` wird **bewusst nicht** importiert: „wer `I0033` baut, braucht ein Feld an der Karte und danach einen erneuten Import" | `Wbsknoten.Aufwand` wird gelesen und liegt seit `B0405` vor — **weggeworfen hat der Leser sie nie** |
| `D0008`/`D0009` | die Zeile steht als gestrichelter Kasten mit der Adresse `I0033?`; Zustand 3 zeichnet die Lücke samt Weg in drei Schritten | `D0009.dc.html`, Zustand 3 |

**Die Hälften des Vergleichs stehen ungleich da.** Die Ist-Hälfte ist gebaut: `Zeiteintrag` je Karte und Kontributor seit `I0023`/`I0024`, die Summe je Karte seit `I0026`, und `WbsImportRepository.LiesErfassteZeiten` liest sie über genau den Schnitt Board × Kartenklasse, den dieser Slice braucht (`Karteniststand.ErfassteZeit`, `B0436`). Die Soll-Hälfte gibt es nirgends.

**Dieser Slice schließt sie ganz**, und zwar in der Reihenfolge, die das Artboard zeichnet:

1. **Ein Feld an der Karte** — `B0471`–`B0475`.
2. **Der Import füllt es aus der Spalte `Aufwand`** — `B0472`, `B0473`.
3. **Ein erneuter Lauf zieht es an bestehende Karten nach.** Das ist **keine Bubble, sondern eine Bedienhandlung**: der wiederholte Import steht seit `I0031` (`R00034`). Gebaut wird nur, **was ihm heute fehlt** (`B0476`) — sein Vergleich kennt das Sollband nicht und meldete eine vor diesem Slice angelegte Karte als `unveraendert`, sodass die Soll-Spalte auch nach dem zweiten Lauf leer bliebe.

**Das Feld hatte keinen WBS-Knoten**, und es entsteht trotzdem hier statt als eigene Interaction unter `D0004`: eine Sollzeit ohne Erzeuger und ohne Leser wäre genau die tote Flexibilität, mit der `I0026` das Feld nicht erfunden hat. Erzeuger (der Import) und Leser (die Auswertung) stehen in **diesem** Slice — davor gibt es keinen ehrlichen Ort dafür.

**Der Slice trägt beide Systemgrenzen.** Anders als `I0022` (bewusst nur API) und `I0026` (bewusst nur Oberfläche): das Fertig-Kriterium sagt „steht gegenüber", und ein Gegenüberstellen ist eine Darstellung. `Kopfzeile.razor:31-35` führt „Auswertungen" heute als gesperrten `span` mit `title="Noch nicht verfügbar"` — er wird ein `NavLink`.

## Geschäftlicher Nutzen

Die Tabelle, die dieser Slice rechnen lässt, **wird heute von Hand geführt**: `Schaetzungen/_ist-zeiten.md` trägt je abgeschlossenem Slice die gezählte Spanne neben der gemessenen Dauer, und jede dieser Zeilen ist Handarbeit an zwei Dateien. Das Board hat beide Zahlen bereits — die Zählung steht in der Spalte `Aufwand` der WBS-Datei, die Ist-Zeit in `Zeiteintrag` — und stellt sie nur nirgends gegenüber.

Der Wert ist nicht die Kontrolle, sondern die **Kalibrierung**: der gemessene Faktor zwischen Zählung und Wirklichkeit liegt in diesem Projekt zwischen 12 und 20 (`I0028` 22,4–37,9h gegen 1,9h, `I0029` 15,6–24,0h gegen 1,0h, `I0030` 32,0–44,0h gegen 2,4h). Solange diese Gegenüberstellung Handarbeit ist, wird sie für jeden neuen Slice neu gemacht oder gar nicht. Und ein Agent, der seinen eigenen Fortschritt gegen die Planung halten will, bekommt sie **gerechnet** statt als Haufen Zeilen zum Selberaddieren — das ist die Kernzusage der Vision an der Stelle, an der sie am meisten wert ist.

## Funktionale Anforderungen

- Eine Karte kann ein **Sollband** aus zwei Stundenzahlen tragen (`SollzeitVonStunden`, `SollzeitBisStunden`); ein Einzelwert setzt beide gleich, kein Band ist erlaubt.
- Der Import liest die Zelle `Aufwand` je Knoten als Band und schreibt je Karte die **Summe über ihren Teilbaum** — eigener Aufwand plus alle zählenden Nachfahren.
- Ein zweiter Import zieht ein geändertes Sollband an einer wiedererkannten Karte nach; eine Karte ohne Band in der Datei verliert ihres.
- `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/soll-ist` liefert je Karte des Bestands Nummer, Titel, erfasste Zeit, Sollband und Abweichung sowie eine Summenzeile.
- Unbekanntes Board und unbekannte Kartenklasse werden mit Grund, Werten und Kompensationsaktion zurückgewiesen.
- `/auswertungen` ist aus der Kopfzeile erreichbar, lässt Board und Kartenklasse wählen und zeigt die Tabelle Karte · Ist · Soll · Abweichung mit Summenzeile.
- Die Sollzeit ist **nicht von Hand änderbar** — weder an der Karte noch am Kartendetail.

## Nicht-funktionale Anforderungen

- **Ein Lesevorgang je Bestand, nicht je Karte.** 41 Karten kosten dieselbe Zahl Abfragen wie eine — dieselbe Regel, unter der `LiesIststand` seit `B0436` steht. Kein Aufruf über die Prozessgrenze je Karte.
- **Die Antwort ist gerechnet, nicht roh.** Sie trägt die einzelnen Zeiteinträge nicht mit; ein Agent bekommt den Vergleich, keine Summanden.
- Benutzerfreundlichkeit: **keine Karte fällt aus der Tabelle** — ohne Zeiteintrag `0:00`, ohne Soll `—` samt Fußzeile mit ihrer Zahl, archivierte Karten markiert.

## Akzeptanzkriterien

### Die Karte trägt ihre Sollzeit (`F0060`)

Fertig-Kriterium wörtlich: *„Ein Import schreibt je Karte das Aufwandsband ihres Teilbaums; ein zweiter Lauf zieht ein geändertes Band nach, und eine vor diesem Slice angelegte Karte bekommt ihr Band beim nächsten Lauf. Ohne Auswertung und ohne Schirm an der zurückgelesenen Zahl prüfbar."*

- [x] Ein Import auf eine Datei mit Aufwänden legt je Karte ein Sollband an; ein zurückgelesener Iststand nennt es.
- [x] **Die Zelle wird als Band gelesen**: `0,4` → `0,4–0,4`; `2` → `2,0–2,0`; `2-4` → `2,0–4,0`; `0,4-1,5` → `0,4–1,5`; leer oder unlesbar → **kein Band** und **keine Ausnahme**.
- [x] **Das Band einer Karte ist die Summe ihres Teilbaums**, nicht ihre eigene Zelle: eine Interaction-Zeile trägt in der WBS keinen Aufwand. Rechenbeispiel an der echten Datei: die Karte `[I0022]` hat vier Bubbles mit `0,4`, `0,4`, `2` und `0,4-1,5` und trägt damit **3,2–4,3 h**; die Karte `[I0030]` hat 31 Bubbles und trägt **38,0–44,0 h**.
- [x] `option`, `ausbau` und `verworfen` zählen **samt Teilbaum** nicht mit.
- [x] Ein zweiter Lauf auf **geändertem** Aufwand meldet die Karte `geaendert` und schreibt das neue Band; ein Knoten, dessen Aufwand verschwindet, verliert sein Band.
- [x] **Prüfbar ohne Auswertung und ohne Schirm**: alle Kriterien dieser Gruppe sind an der zurückgelesenen Zahl zu zeigen.

### Der Vergleich über die API (`F0061`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/soll-ist` liefert je Karte des Bestands Nummer, Titel, erfasste Zeit, Sollband und Abweichung sowie eine Summenzeile über den Bestand; unbekanntes Board und unbekannte Kartenklasse werden mit Grund, Werten und Kompensationsaktion zurückgewiesen."*

- [x] Der Aufruf antwortet **200** mit je einer Zeile pro Karte des Bestands: Kartennummer, `KarteId`, Titel, erfasste Zeit, Sollband, Abweichung, Archivstand.
- [x] Der **Bestand** ist Board × Kartenklasse — dasselbe Set, das `GET .../kartenklassen/{kartenklasseId}/karten` (`I0022`) liefert, **archivierte Karten eingeschlossen**.
- [x] Die **Abweichung** ist dreiwertig: `unter dem Band`, `im Band`, `über dem Band um h`. Rechenbeispiel gegen `38,0–44,0 h`: Ist `2:24` → *unter dem Band*; Ist `40:00` → *im Band*; Ist `50:00` → *über dem Band um 6,0 h*.
- [x] Eine Karte **ohne Sollband** trägt kein Sollband und **keine** Abweichung — nicht `im Band`, nicht `0`.
- [x] Die **Summenzeile** trägt die Summe der Ist-Zeiten und die Summe der Sollbänder; **die Summe eines Bandes ist selbst ein Band** (Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze), und sie zählt nur Karten mit Band. Ihre Abweichung wird nach derselben Regel gebildet.
- [x] **Überlappende Zeiteinträge zählen doppelt**, und eine Summe über 24 h an einem Tag ist richtig: Personenstunden gegen Personenstunden. Ein laufender Timer (`Ende IS NULL`) zählt nicht mit.
- [x] Unbekanntes Board → **404** mit Grund, der die Board-Nummer nennt, und der Kompensationsaktion `GET /api/boards`.
- [x] Unbekannte Kartenklasse → **404** mit Grund, der ihre Nummer nennt, und der Kompensationsaktion `GET /api/boards/{boardId}/kartenklassen`; eine Kartenklasse eines **fremden** Boards ist der eigene Fall `kartenklasse-fremd`.
- [x] **Ohne Schirm prüfbar**: alle Kriterien dieser Gruppe sind an der Antwort allein zu zeigen.

### Der Schirm zeigt den Vergleich (`F0062`)

Fertig-Kriterium wörtlich: *„`/auswertungen` ist aus der Kopfzeile erreichbar und zeigt für den gewählten Bestand die Tabelle Karte · Ist · Soll · Abweichung mit Summenzeile; die vier Ränder tragen: Bestand ohne Karten, Karte ohne Zeiteintrag, Karte ohne Soll, WebApi nicht erreichbar."*

- [x] Der Punkt „Auswertungen" in der Kopfzeile führt auf `/auswertungen` — kein gesperrter `span` mehr.
- [x] Der Schirm lässt **Board und Kartenklasse** wählen und zeigt danach die Tabelle **Karte · Ist · Soll · Abweichung** in Kartennummernfolge, darunter die Summenzeile.
- [x] Das **Ist** steht als `h:mm` und läuft über 24 hinaus (`Dauerform`), das **Soll** als Band in Stunden mit Dezimalkomma (`38,0–44,0 h`).
- [x] Rand 1 — **Bestand ohne Karten**: lesbare Leermeldung statt leerer Tabelle.
- [x] Rand 2 — **Karte ohne Zeiteintrag**: steht mit `0:00` da, fällt nicht heraus.
- [x] Rand 3 — **Karte ohne Soll**: steht mit `—` in Soll und Abweichung; die **Fußzeile nennt, wie viele es sind**, damit die Summe nicht als vollständig gelesen wird.
- [x] Rand 4 — **WebApi nicht erreichbar**: lesbare Meldung statt Ausnahmeseite.
- [x] **Archivierte Karten stehen markiert mit** — ihre Zeit wurde geleistet.

### Die Sollzeit ist nicht von Hand änderbar

- [x] Weder `Karte` noch `Kartendetail` zeigen ein Sollzeitfeld, und es gibt keinen Endpunkt, der es setzt. Der einzige Erzeuger ist der Import.

### Die benannte Änderung an grünem Bestand

- [x] `I0030`: `Kartenentwurf`, `Kartenentwurfsbildner` und `WbsImportRepository.Schreibe` wachsen um das Sollband.
- [x] `I0031`: `Kartenabbild`, `Kartenabbildvergleich`, `Karteniststand`, `Kartenaktualisierungsauftrag` und `LiesIststand` wachsen um das Sollband.
- [x] `I0026` wird **nicht** angefasst.
- [x] **Zwei grüne E2E-Zusagen kippen und werden nachgezogen**: `RahmenE2ETests.cs:53-58` erwartet `aria-disabled="true"` am Punkt „Auswertungen" und **zwei** Navigationsverweise, `KontributorenlisteE2ETests.cs:27` erwartet dasselbe Attribut. Nach `B0485` sind es **drei** Verweise ohne `aria-disabled`.

### Der grüne Bestand bleibt grün

- [x] **Das Sollband ist nullbar** — jede Zusage von `I0030` und `I0031` gilt unverändert weiter: eine Datei ohne Aufwandsspalte erzeugt dieselben Karten wie heute.
- [x] **Eine Folge ist gewollt und wird nicht als Rückschritt gelesen**: `B0452` („zweiter Lauf: 0 angelegt, 0 geändert") gilt weiter für zwei Läufe **desselben** Standes. Eine Karte aus einem Lauf **vor** diesem Slice wird beim nächsten Lauf **einmalig** `geaendert` — das ist Schritt 3 des Artboards.
- [x] Die Suiten von `R00033`, `R00034` und `R00035` bleiben grün.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Burndown** (`I0034`), **kein Puffer-Verbrauch** (`I0035`), **kein Zeitexport** (`I0036`).
- [x] **`I0037` bleibt unberührt**: hier eine **gerechnete** Antwort über **einen** Bestand, dort die **ungerechneten** Rohdaten über alles und ohne Limit.
- [ ] **Kein Zeitraumfilter** — der Zeitraum gehört der Kalenderachse, also `I0034`.
- [x] Kein Sollzeitfeld an Karte oder Kartendetail, kein Rückfluss ins Markdown.

## Betroffene Verzeichnisstruktur

- **Schema**: `Source/KanbanC.BL/Persistenz/Migrationen/019-kartensollzeit.sql` — **eigene Tabelle, kein `ALTER TABLE`**. Der `Migrationslaeufer` kennt kein Journal und führt jedes Skript bei jedem Start aus; `ADD COLUMN` ist nicht idempotent, und eine bestehende `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich. Muster: `004-boardeinstellung.sql`, `010-karteneigenschaft.sql`, `017-kartenklassenzuordnung.sql`.
- **Import** (bestehender Themenordner): `KanbanC.BL/Models/Import`, `Operations/Import`, `Persistenz/Import`.
- **Auswertungen** (neuer Themenordner, in jeder Schicht): `KanbanC.Contracts/Auswertungen`, `KanbanC.BL/Models/Auswertungen`, `Operations/Auswertungen`, `Integrations/Auswertungen`, `Interfaces/Auswertungen`, `Persistenz/Auswertungen`.
- **API**: `KanbanC.WebApi/Endpunkte/AuswertungsEndpunkte.cs`, Registrierung in `Program.cs`.
- **Oberfläche**: `KanbanC.Blazor/Components/Pages/Auswertungen.razor` (+ `.razor.css`), `Components/Auswertungen/` für die Tabelle, `Services/AuswertungenApiKlient.cs`, `Components/Layout/Kopfzeile.razor`. **Keine Projektreferenz auf `KanbanC.BL`** — der Weg führt über HTTP.
- **Tests**: `KanbanC.BL.Tests/{Operations,Models}/…` spiegeln die Themenordner, `KanbanC.WebApi.IntegrationTests`, `KanbanC.Blazor.Tests`, `KanbanC.PlaywrightTests` (Seitenobjekt `AuswertungenSeite`, `Rahmen.PunktAuswertungen`).
- **Gestaltung**: Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`, keine Literale in der Komponenten-CSS.

## Technische Überlegungen

### Warum ein Band und nicht eine Zahl

Die Aufwandsspalte führt `0,4` und `2` **neben** `2-4` und `0,4-1,5`. Eine Spanne beim Import auf eine Zahl zu reduzieren wäre ein **verlustbehaftetes Schreiben**, das kein späterer Lauf zurücknehmen könnte — die Datei bliebe die einzige Stelle, an der die Unsicherheit noch stünde. Und eine erfundene Mitte gäbe einer Schätzung die Genauigkeit, die die Datei nie behauptet hat.

Gespeichert werden deshalb **zwei** Zahlen; ein Einzelwert setzt beide gleich. Daraus folgt der Rest: die Abweichung heißt `unter dem Band` / `im Band` / `über dem Band um h`, und **die Summe über den Bestand ist selbst ein Band**. Genau in dieser Form führen `Schaetzungen/_ist-zeiten.md` und das Artboard ihre Zahlen (`32,0–44,0 h` gegen `2:24`) — der Slice erfindet keine Darstellung, er baut die vorhandene.

### Die Sollzeit ist die Summe des Teilbaums — und der Filter davor gilt schon

Ohne diesen Schritt bliebe die Spalte auf Interaction-Schnitt **leer**: eine Interaction-Zeile trägt in der WBS keinen Aufwand, nur Bubbles tun das. Gerechnet wird über den eigenen Aufwand plus alle zählenden Nachfahren.

**Die Ausschlussregel muss nicht gebaut werden — sie steht schon.** `WbsImportService.Bilanziere` ruft `Umfangsfilter.Filtere(bestand.Baum)` **vor** `Kartenentwurfsbildner.Bilde`, und der Filter entfernt `verworfen`, `option` und `ausbau` **samt Teilbaum**. Die Summierung arbeitet auf dem gefilterten Baum und erbt den Ausschluss; ihn ein zweites Mal zu formulieren wäre dieselbe Regel an zwei Orten (C23).

### Der Kartenbestand ist Board + Kartenklasse — der Leseweg ist trotzdem ein eigener

Nachgesehen, nicht angenommen: `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten` (`I0022`) liefert genau dieses Set, und das Artboard sagt es wörtlich („Board und Kartenklasse sind zusammen der ‚Kartenbestand'").

**Der Vergleich hängt aber nicht an jenem Endpunkt.** `Klassenkarte` trägt weder Zeiten noch Sollzeit; ein Schirm, der ihn plus N Kartendetails aufriefe, zahlte **N+1 Aufrufe über die Prozessgrenze**. Gelesen wird eigenständig, nach dem Vorbild von `WbsImportRepository.LiesIststand`: dieselbe `WHERE s.Board = @BoardId AND z.Kartenklasse = @KartenklasseId`-Verbindung über `Karte → Spalte → Kartenklassenzuordnung`, ein Lesevorgang je Bestand. Übernommen wird der **Mengenbegriff**, nicht der Leseweg — die Adresse folgt derselben Form eine Stufe weiter.

### Gerechnet wird in C#, an einer Stelle, in der BL — kein `SUM` in SQL

Die Hausregel aus `I0026` trägt hier in ihrer eigentlichen Fassung („kein Summenfeld neben der Liste, die es trägt; kein zweiter Leseweg in SQL"), **nicht aber in ihrem Ort**: die Antwort dieses Slice enthält die Zeiteinträge gar nicht, neben denen eine Summe eine zweite Wahrheit wäre — sie **ist** die Auswertung. Und die Kernregel des Projekts verlangt, dass ein Agent den Vergleich über die API bekommt statt Zeilen zum Selberaddieren.

Kein neues Muster: `WbsImportRepository.LiesErfassteZeiten` summiert seit `B0436` genau so — Rohzeilen über Dapper, Spanne in C#, `AND e.Ende IS NOT NULL`, also zählt ein laufender Timer nicht mit. Der Vermerk in `LiesKommentarzahlen` nennt auch den Grund, aus dem SQL-Aggregate hier ohnehin heikel sind: Microsoft.Data.Sqlite meldet für eine Aggregatspalte ohne Tabellentyp `Byte[]`, und Dapper findet dann keinen Konstruktor.

Überlappende Zeiteinträge zählen dabei **doppelt**, und eine Tagessumme darf über 24 h liegen. `I0025` erlaubt Überlappungen ausdrücklich, `Dauerform` lässt die Stunden über 24 hinauslaufen — und das ist richtig: das Soll ist eine Zahl in **Personenstunden**, das Ist muss dieselbe Größe messen. Zwei Kontributoren an einer Karte haben zwei Stunden geleistet, nicht eine.

### Warum das Sollband fast von selbst durch den Import reist

Der Schreibweg trägt es, sobald `Kartenentwurf` es trägt: `Kartenschreibauftrag` ist ein Wrapper um den Entwurf (`record Kartenschreibauftrag(Kartenentwurf Entwurf, long Spalte, bool InDerAbschlussspalte)`). Zu tun bleibt das, wo der Entwurf **nicht** hinkommt:

- `Kartenabbild` — **die Vergleichsgestalt beider Seiten** (`B0437`). Steht das Band nicht darin, ergibt `Kartenabbildvergleich.SindGleich` weiter `true`, und ein zweiter Lauf könnte das Band **nie** nachziehen. Das ist `B0476` und zugleich Schritt 3 des Artboards.
- `Kartenaktualisierungsauftrag` — was an einer wiedererkannten Karte nachgezogen wird.
- `Karteniststand` — was ein zweiter Lauf vorfindet; die Zeile wird in `LiesIststand` mitgelesen.

Das Band steht damit **im** Abbild und ist folglich ein Grund für „geändert". Das ist gewollt und die einzige Stelle, an der sich die Bilanz eines Laufs gegenüber heute verschiebt.

### Die neue Tabelle

```sql
CREATE TABLE IF NOT EXISTS Kartensollzeit
(
    Karte              INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
    SollzeitVonStunden REAL    NOT NULL,
    SollzeitBisStunden REAL    NOT NULL
);
```

Der Fremdschlüssel ist zugleich der Schlüssel — eine Karte trägt **höchstens ein** Sollband; der Name der Spalte ist der der referenzierten Tabelle (`Karte`), nie `<Tabelle>Nummer`. **Fehlt die Zeile, gibt es kein Band** — das ist die Darstellung von „ohne Soll", und deshalb sind beide Spalten `NOT NULL`: ein halbes Band gibt es nicht.

### Der Schirm

`/auswertungen` ist die Adresse des ganzen Dialogs, nicht nur dieses Slice. Der Schirm bekommt links die Auswertungsliste (Soll-Ist wählbar, die übrigen gesperrt) und oben die Wahl von Board und Kartenklasse. **Die Liste wächst mit** — eine sechste Auswertung ist ein Eintrag mehr, kein Umbau der Fläche.

Artboard: `Dokumentation/Wireframes/D0009.dc.html`, **Zustand 1** (wo die Auswertungen wohnen), **Zustand 3** (die Tabelle Karte · Ist · Soll · Abweichung) und **Zustand 7** (Leer und Ränder). Das Bild ist die Quelle für die Gestaltung, **nicht** für Kriterien — die Abweichungsspalte steht dort ausdrücklich **leer**, weil sie genau die Entscheidung braucht, die diese Anforderung trifft.

### Ablauf

1. **Schema** — Migration `019-kartensollzeit.sql`, idempotent, im `Migrationslaeufer` mitgeführt.
2. **Der Import gewinnt das Soll**
   - 2.1 `Aufwandsband.Lies(zelle)` → `Sollband?`; deutsches Dezimalkomma, invariant gelesen.
   - 2.2 `Sollbandrechner.RechneJeKartenknoten(baum, kartenknoten)` → Band je Karte, aus eigenem Aufwand plus allen Nachfahren des **gefilterten** Baums.
   - 2.3 `Kartenentwurfsbildner.Bilde` hängt das Band an den Entwurf.
3. **Der Import trägt das Soll**
   - 3.1 `Kartenabbildbildner.AusEntwurf` / `.AusIststand` nehmen es auf, `Kartenabbildvergleich.SindGleich` vergleicht es.
   - 3.2 `WbsImportRepository.Schreibe` legt die `Kartensollzeit`-Zeile an oder ändert sie — **dieselbe Transaktion** wie der übrige Schreiblauf; ein Knoten ohne Band verliert seine Zeile.
   - 3.3 `LiesIststand` gibt sie am `Karteniststand` zurück.
4. **Der Vergleich**
   - 4.1 `Auswertungsrepository.LiesSollIst(boardId, kartenklasseId)` → Rohzeilen, Spannen in C# summiert.
   - 4.2 `Abweichungsrechner.Rechne(erfassteZeit, sollband?)` → `Abweichung?`.
   - 4.3 `AuswertungsService.SollIst(boardId, kartenklasseId)` → `Ergebnis<SollIstAuswertung>` oder `Nichtgefunden.Board` / `Nichtgefunden.Kartenklasse` / `Nichtgefunden.FremdeKartenklasse`.
   - 4.4 `AuswertungsEndpunkte` → 200 / 404.
5. **Der Schirm** — `AuswertungenApiKlient.LadeSollIst`, `Auswertungen.razor`, Kopfzeilenpunkt als `NavLink`.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** die neue Migration `019`; die neue Route `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/soll-ist`; der neue Schirm `/auswertungen`; der Kopfzeilenpunkt „Auswertungen" (`Kopfzeile.razor:31-35`); die bestehende Route `POST /api/boards/{boardId}/wbs-import`, deren Verhalten wächst.

**In `KanbanC.BL/Models/Import`:**
- `Sollband` (DTO, immutable) — zwei Stundenzahlen; ein Einzelwert setzt beide gleich.
  - `static Sollband Summe(IEnumerable<Sollband> baender)`
- `Kartenentwurf`, `Kartenabbild`, `Karteniststand`, `Kartenaktualisierungsauftrag` — je um `Sollband? Sollband` gewachsen.

**In `KanbanC.BL/Operations/Import`:**
- `Aufwandsband` (Operation, pur) — liest eine Aufwandszelle als Band. Eine unlesbare Zelle liefert `null`, **keine Ausnahme**.
  - `static Sollband? Lies(string zelle)`
- `Sollbandrechner` (Operation, pur) — **die eine Rechenstelle des Solls**: Band je Kartenknoten aus eigenem Aufwand und allen Nachfahren.
  - `static IReadOnlyDictionary<string, Sollband> RechneJeKartenknoten(Wbsbaum baum, IReadOnlyList<Wbsknoten> kartenknoten)`
- `Kartenabbildvergleich` — vergleicht das Band mit.

**In `KanbanC.Contracts/Auswertungen`** (immutable, C08):
- `Zeitband` (DTO) — `decimal VonStunden`, `decimal BisStunden`.
- `Abweichung` (DTO) — `Abweichungslage Lage`, `decimal? UeberschussStunden`.
- `Abweichungslage` (Enum) — `UnterDemBand`, `ImBand`, `UeberDemBand`.
- `SollIstZeile` (DTO) — `long KarteId`, `string? Kartennummer`, `string Titel`, `TimeSpan ErfassteZeit`, `Zeitband? Sollband`, `Abweichung? Abweichung`, `bool IstArchiviert`.
- `SollIstAuswertung` (DTO) — `IReadOnlyList<SollIstZeile> Zeilen`, `SollIstSumme Summe`.
- `SollIstSumme` (DTO) — `TimeSpan ErfassteZeit`, `Zeitband? Sollband`, `Abweichung? Abweichung`, `int KartenOhneSoll`.

Stunden stehen als `decimal`, die erfasste Zeit als `TimeSpan` — wie überall sonst im Bestand.

**In `KanbanC.BL/Models/Auswertungen`:**
- `Sollistkarte` (DTO, immutable) — eine gelesene Karte des Bestands mit Nummer, Titel, erfasster Zeit, Band und Archivstand.
- `Sollistkarten` (benannte Collection) — beantwortet „wie viele ohne Soll" und „was ist die Bandsumme".

**In `KanbanC.BL/Operations/Auswertungen`:**
- `Abweichungsrechner` (Operation, pur) — Ist gegen Band. Ohne Band **keine** Abweichung.
  - `static Abweichung? Rechne(TimeSpan erfassteZeit, Zeitband? sollband)`

**In `KanbanC.BL/Interfaces/Auswertungen` und `KanbanC.BL/Persistenz/Auswertungen`:**
- `IAuswertungsrepository` (Interface, zwei Implementationen: echte und `TestAuswertungsrepository`)
  - `Sollistkarten? LiesSollIst(long boardId, long kartenklasseId)`
- `Auswertungsrepository` (Provider/Ressourcenzugriff) — ein Lesevorgang je Bestand, Rohzeilen über Dapper, Summierung in C#.

**In `KanbanC.BL/Integrations/Auswertungen`:**
- `AuswertungsService` (Integration) — prüft Board und Kartenklasse, ruft Repository und Rechner, baut die Summenzeile.
  - `Ergebnis<SollIstAuswertung> SollIst(long boardId, long kartenklasseId)`

**In `KanbanC.WebApi/Endpunkte`:**
- `AuswertungsEndpunkte` (Integration) — eine Route, 200 / 404. Der Fehlervertragstest aus `B0102` nimmt sie auf.

**In `KanbanC.Blazor`:**
- `AuswertungenApiKlient` (Integration) — `Task<ApiErgebnis<SollIstAuswertung>> LadeSollIst(long boardId, long kartenklasseId)`
- `Zeitbandform` (Operation, pur) — ein Band als `38,0–44,0 h`, ein fehlendes als `—`.
- `Abweichungswort` (Operation, pur) — eine Abweichung als Satz.
- `Auswertungen.razor` (Seite, Route `/auswertungen`), `SollIstTabelle.razor` (Komponente).
- `Kopfzeile.razor` — der `span` wird `NavLink href="auswertungen"`.

**Kein Interface** für die reinen Operationen: je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Kartenentwurf` | `Sollband? Sollband` kommt dazu |
| `Kartenabbild` | `Sollband? Sollband` kommt dazu — **die Stelle, ohne die ein zweiter Lauf nichts nachzieht** |
| `Kartenabbildbildner` | `AusEntwurf` und `AusIststand` nehmen das Band mit |
| `Kartenabbildvergleich` | vergleicht das Band |
| `Karteniststand` | `Sollband? Sollband` kommt dazu |
| `Kartenaktualisierungsauftrag` | `Sollband? Sollband` kommt dazu |
| `Kartenentwurfsbildner` | hängt das gerechnete Band an den Entwurf |
| `Importwirkungsbildner` | baut das Band in den Aktualisierungsauftrag |
| `WbsImportRepository` | `Schreibe` legt/ändert/löscht die `Kartensollzeit`-Zeile in derselben Transaktion; `LiesIststand` liest sie mit |
| `TestWbsImportRepository` | zieht mit |
| `Migrationslaeufer` | führt `019` mit |
| `Kopfzeile.razor` | `span#navigation-auswertungen` → `NavLink` |
| `Rahmen.cs`, `RahmenE2ETests.cs`, `KontributorenlisteE2ETests.cs` | die Zusage „Auswertungen ohne Weg" wird zur Zusage „Auswertungen mit Weg"; `NavigationsVerweise` steigt von 2 auf 3 |
| `Program.cs` (WebApi, Blazor) | Registrierung von Repository, Dienst, Endpunkten, Klient |

**Nicht geändert:** `Frontmatterleser`, `Zeilenzerleger`, `Knotenleser`, `Wbsbaumbildner`, `Umfangsfilter`, `Kartenknotenwahl`, `Zielspaltenwahl`, `Kartenfelder`, `Herkunftsverweis`, `SollIstVergleicher`, `Teilaufgabenabgleich`, `Wiedererkennungspruefung`, `Berichtsnachzug`, `Importbericht`, `Importzeile`, `Ereignisdrehscheibe`, `Importereignis`, `ZeitenService`, `ZeitenRepository`, `Kartendetail.razor`, `Board.razor` und alles unter `Migrationen/` außer der neuen Datei.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Aufwandsband` — `0,4`, `2`, `2-4`, `0,4-1,5`, leer, Unsinn, Leerzeichen um den Bindestrich. Kein Wurf, sondern `null`.
- `Sollbandrechner` — Interaction-Zeile ohne eigenen Aufwand summiert ihre Bubbles; `option`/`ausbau`/`verworfen` samt Teilbaum draußen; eine Karte, unter der kein Knoten Aufwand trägt, bekommt **kein** Band statt `0,0–0,0`.
- `Sollband.Summe` — Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze; leere Menge → kein Band.
- `Abweichungsrechner` — die drei Lagen samt Rändern (`Ist == Von`, `Ist == Bis`), ohne Band `null`.
- `Kartenabbildvergleich` — zwei Abbilder, die sich **nur** im Band unterscheiden, sind ungleich; `null` gegen `null` ist gleich.
- `Zeitbandform`, `Abweichungswort` (in `KanbanC.Blazor.Tests`).

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- Migration `019` zweimal gefahren — Schema und Daten unverändert.
- `Schreibe` legt das Band an, ändert es, **löscht** die Zeile, wenn das Band verschwindet; alles in einer Transaktion, ein abgebrochener Lauf hinterlässt keine halbe Sollzeit.
- `LiesIststand` gibt das Band zurück.
- **Zweiter Lauf mit geändertem Aufwand**: Karte `geaendert`, neues Band; zweiter Lauf mit **gleichem** Stand: `unveraendert` (`B0452` gilt weiter).
- **Probe an der echten Datei** (`Dokumentation/Planung/kanbanc.md`, wie `B0452`): die Summierung über den Teilbaum an einem Fall, den niemand für den Test zurechtgelegt hat.
- `GET .../soll-ist`: 200 mit Zeilen und Summe; 404 für unbekanntes Board, unbekannte und fremde Kartenklasse — jeweils mit Grund, Werten und Kompensationsaktion. Der Fehlervertragstest aus `B0102` nimmt die Route auf.
- Bestand ohne Karten → 200 mit leerer Zeilenliste, nicht 404.

**`KanbanC.Blazor.Tests`:** `AuswertungenApiKlient` — 200 wird gelesen, 404 wird zur lesbaren Zurückweisung, `HttpRequestException` zur Ausfallmeldung. **Diese Pfade sind über den Browser nicht auslösbar** — genau der Grund, aus dem dieses Testprojekt existiert.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — eine kleine WBS **mit Aufwänden** einfahren (wie `B0431`/`B0454`, nicht `kanbanc.md`), auf einer Karte eine Zeit nachtragen, `/auswertungen` **über die Kopfzeile** öffnen, Bestand wählen, Ist, Soll und Abweichung lesen. Dazu die nachgezogenen Zusagen in `RahmenE2ETests` und `KontributorenlisteE2ETests`.

## Abhängigkeiten

- Abhängig von: **`R00033`** (WBS-Datei importieren — `I0030`, **grün**) und **`R00028`** (Zeiten einer Karte sehen — `I0026`, **grün**); das sind die beiden Knoten der WBS-Spalte `Braucht` an `I0033`. Dazu **`R00034`** (Import wiederholen — `I0031`, **grün**), das `Braucht` von `F0060`: ohne den zweiten Lauf gäbe es Schritt 3 nicht.
- Setzt außerdem auf (alle grün): **`R00022`**/**`R00023`** (Kartenklassen, Kartennummern, `Kartenklassenzuordnung`), **`R00025`** (der Mengenbegriff „Karten einer Klasse"), **`R00026`**/**`R00027`** (`Zeiteintrag` mit `Ende`), **`R00016`** (Archivstand), **`R00005`** (Kopfzeile und Gestaltungstokens).
- Blockiert: **nichts** in der WBS — kein Knoten führt `I0033` in seiner Spalte `Braucht`. **Sachlich aber sehr wohl `I0035`**: ein Puffer ist die Differenz zweier Dauern und braucht das Soll. Die WBS führt dort nur `Braucht: I0034`. → **Befund für `/planung`**, unten unter „Offene Fragen".
- **`D0009` wird mit diesem Slice nicht grün** — `I0034`, `I0035`, `I0036` und `I0037` bleiben rot.

## Umfang

```
Soll-Ist-Vergleich abrufen (I0033) = 18 Bubbles: 16 Standard (22,4h), 2 unklar (4,0–8,0h).
Rest: 22,4h klar + 4,0–8,0h unklar · 1 von 18 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 18 Bubbles gruen (0 %) · 0 laufen · 18 offen
```

`I0033` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **drei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0060` Die Karte trägt ihre Sollzeit | `B0471`–`B0477` (7) | 6 (7,2h) | 1 (2,0–4,0h) | `I0031` |
| `F0061` Der Vergleich über die API | `B0478`–`B0483` (6) | 6 (8,8h) | 0 | `F0060` |
| `F0062` Der Schirm zeigt den Vergleich | `B0484`–`B0488` (5) | 4 (6,4h) | 1 (2,0–4,0h) | `F0061` |

**Warum drei Features:** weil drei Aspekte **getrennt fertig** werden und sie — anders als bei `I0020` bis `I0027` — weder Datenquelle noch Komponente noch Prüfweg teilen. `F0060` fasst den Import an und die Auswertung gar nicht und ist **ohne Schirm und ohne Auswertung** an der zurückgelesenen Zahl prüfbar; `F0061` ist allein an der 200-Antwort prüfbar; `F0062` zeigt, was `F0061` liefert, und könnte auch dann noch fehlen, wenn die API vollständig ist. Dieselbe Lage wie `F0051` bei `I0030`.

**Die beiden unklaren Bubbles** sind `B0477` (Probe an der echten Datei, wie `B0452`) und `B0488` (E2E über beide Prozesse). Alles dazwischen ist pure Logik über einem Muster, das im Repository liegt.

`B0471` trägt den einzigen belegten Wert (Migrationstabelle, `B0108` in `_ist-zeiten`).

**Nach gemessenem Durchsatz ist mit etwa 0,6–1,2 h zu rechnen.** Die Richtwert-Konvention seit `I0004` überschätzt messbar, und das ist dreifach belegt (`Schaetzungen/_ist-zeiten.md`; `I0031` und `I0032` sind dort noch nicht eingetragen):

| Slice | gezählt | gemessen | Faktor |
|---|---|---|---|
| `I0028` | 22,4–37,9h | 1,9h | ~12–20 |
| `I0029` | 15,6–24,0h | 1,0h | ~16–24 |
| `I0030` | 32,0–44,0h | **2,4h** | ~13–18 |

Die Zählung wird trotzdem nicht still gekippt — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0033` und an allen drei Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00035`/`I0032`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Untergrenze, Obergrenze oder Band?** — **entschieden: Band, gespeichert und verglichen.** Eine Reduktion beim Import wäre verlustbehaftetes Schreiben, eine erfundene Mitte gäbe der Schätzung eine Genauigkeit, die die Datei nie behauptet. Das Artboard nennt diese Frage ausdrücklich als Entscheidung von `I0033` und lässt die Abweichungsspalte deshalb leer. **Nicht am Menschen geprüft.**
- **Welche Zahl steht in der Soll-Spalte einer Interaction-Karte?** — **entschieden: die Summe ihres Teilbaums.** Ihre eigene Zelle ist leer, die Spalte bliebe auf Interaction-Schnitt sonst durchgehend leer. **Nicht am Menschen geprüft.**
- **Nachgerechnet, und es passt nicht zusammen: `B0477` erwartet für `[I0030]` 32,0–44,0 h und für `[I0022]` 2,8–3,9 h. Die Summe der Aufwandszellen ergibt 38,0–44,0 h und 3,2–4,3 h.** Der Grund ist belegbar: die Zahlen des Artboards und von `_ist-zeiten.md` stammen aus der **Berichtsform von `/planung zaehlen`**, die als Untergrenze bewusst nur die **Standardwerte** summiert (`I0030` = „28 Standard (32,0h), 3 unklar (6,0–12,0h)", `R00033`), damit das Unsicherheitscluster sichtbar bleibt. Das ist eine Darstellungsregel, keine Summe. Bei `[I0022]` kommt eine zweite Abweichung dazu: `R00025` zählt „3 Standard (2,8h), 1 unklar (0,4–1,5h)", was oben 4,3 ergäbe — `_ist-zeiten.md` führt 3,9. **Entschieden: gesummt wird, was in den Zellen steht** (Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze) — der Import liest Zellen, nicht Berichte, und eine Bandsumme, die die Untergrenzen unklarer Bubbles verschluckt, wäre nicht das Band der Datei. **Folge: die zwei Beispielzahlen in `B0477` sind beim Bauen auf 38,0–44,0 h und 3,2–4,3 h zu korrigieren** — eine Bubble ist Entwurf, kein Kriterium. **Nicht am Menschen geprüft; und die 3,9 in `_ist-zeiten.md` bleibt unerklärt.**
- **Woran hängt der Vergleich — am Endpunkt von `I0022` oder an einem eigenen Leseweg?** — **entschieden: eigener Leseweg.** `Klassenkarte` trägt weder Zeiten noch Soll; N+1 Aufrufe über die Prozessgrenze wären der Preis. Der Mengenbegriff bleibt derselbe. **Nicht am Menschen geprüft.**
- **Wird in SQL oder in C# summiert?** — **entschieden: in C#, in der BL**, nach dem Vorbild von `LiesErfassteZeiten`. Die Antwort trägt die Zeiteinträge nicht mit, neben denen eine Summe eine zweite Wahrheit wäre. **Nicht am Menschen geprüft.**
- **Ist die Sollzeit von Hand änderbar?** — **entschieden: nein**, weder an `Karte` noch am `Kartendetail`. Die Vision führt unter den Nicht-Zielen „Burndown und Critical Chain rechnen aus den Ist-Daten; sie planen nicht" — ein im Board editierbares Soll wäre Planung im Board. Ein ungezeigtes Feld wäre wieder C24. **Wer es sehen oder setzen will, bestellt dafür einen Slice.** **Nicht am Menschen geprüft.**
- **Fällt eine Karte ohne Soll oder ohne Zeit aus der Tabelle?** — **entschieden: nein.** Ohne Zeiteintrag `0:00` (bei vorhandenem Soll ist das die interessanteste Zeile), ohne Soll `—` plus Fußzeile mit ihrer Zahl. Archivierte Karten stehen markiert mit, wie in `B0436`. **Nicht am Menschen geprüft.**
- **Was zeigt eine Karte, deren Teilbaum nur teilweise Aufwände trägt?** — **angenommen: ein Untermaß, ohne Vollständigkeitszeichen.** Ein Hinweis „unvollständig" an der Zeile wäre eine **zweite Aussage neben der Zahl**, und die Fußzeile trägt die Auskunft für den Fall, der wirklich zählt (gar kein Soll). **Angenommen, nicht belegt** — der Preis ist eine Zahl, die kleiner ist als der wahre Umfang, ohne dass die Zeile es sagt.
- **`I0035` (Puffer-Verbrauch) führt `Braucht: I0034`, aber nicht `I0033`.** Ein Puffer ist die Differenz zweier Dauern und braucht das Soll; das Artboard hält das ausdrücklich als Befund fest (`D0009.dc.html`, Zustand 4). **Befund für `/planung`, hier nicht geändert** — diese Familie ändert keine Knoten.
- **`B0452` ändert seine Bedeutung nicht, aber sein erster Lauf nach diesem Slice sieht anders aus.** Eine vor diesem Slice angelegte Karte wird beim nächsten Import **einmalig** `geaendert`. Das ist Schritt 3 des Artboards, kein Rückschritt — steht als Kriterium oben, damit es niemanden überrascht. **Nicht am Menschen geprüft.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft beim Start mit.

## Manuelle Nachbereitungstätigkeiten

- **Ein erneuter Import je bestehendem Bestand.** Karten aus Läufen vor diesem Slice bekommen ihr Sollband erst beim nächsten Lauf; bis dahin steht ihre Soll-Spalte auf `—`. Das ist Schritt 3 und eine **Bedienhandlung**, keine Migration: der wiederholte Lauf steht seit `I0031` und erkennt die Karten am Herkunftsverweis wieder.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist konkret und zweimal aktenkundig: das Board kennt die geleistete Zeit je Karte seit `I0026`, aber es kennt kein Soll — `I0026` hat das Feld bewusst nicht erfunden, `I0030` hat die Spalte `Aufwand` bewusst nicht importiert, und beide haben denselben Grund genannt, nämlich dass ein Feld ohne Erzeuger und ohne Leser tote Flexibilität wäre. Genau diese drei fehlenden Teile — Feld, Erzeuger, Leser — entstehen hier zusammen (X), wodurch der Import je Karte das Aufwandsband ihres WBS-Teilbaums schreibt und ein zweiter Lauf es an bestehenden Karten nachzieht (Y), sodass die API und der Schirm für einen Bestand rechnen können, was heute in `Schaetzungen/_ist-zeiten.md` von Hand geführt wird (Z). Der Hebel sitzt genau hier und nicht vorgelagert: eine eigene Interaction „Sollzeit an der Karte" unter `D0004` hätte ein Feld erzeugt, das niemand füllt und niemand liest — dieselbe tote Flexibilität, die `I0026` vermieden hat. Und er sitzt nicht nachgelagert: `I0034` und `I0035` sind ohne dieses Feld überhaupt nicht baubar (das Artboard zeichnet für beide dieselbe Lücke), und `I0037` liefert Rohdaten, aus denen jeder Leser den Vergleich selbst rechnen müsste — was der Kernregel des Projekts widerspricht, dass ein Agent von der API bekommt, was ein Mensch am Schirm sieht.

## Missing-Docs

- **`REAL` in SQLite und Dezimalstunden.** Der Bestand legt bisher keine Fließkommazahl ab; ob `REAL` mit `decimal` in Dapper ohne Genauigkeitsverlust hin und her geht oder ob Zehntelstunden besser als `INTEGER` in Minuten stünden, ist nirgends notiert. Für `B0471` und `B0475` ist das die einzige offene Größe.
- **Kulturabhängiges Parsen der Aufwandszelle.** Die Datei führt deutsches Dezimalkomma, der Code liest invariant — eine Notiz, welche Kultur an welcher Stelle des Imports gilt, gibt es nicht (`Frontmatterleser` und `Zeilenzerleger` schweigen dazu).

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Eine Zahl statt eines Bandes** (Untergrenze, Obergrenze oder Mitte) | Verlustbehaftetes Schreiben, das kein späterer Lauf zurücknimmt; eine Mitte behauptet eine Genauigkeit, die die Datei nie hatte. |
| **Die Aufwandszelle als Text ablegen und erst beim Lesen deuten** | Verschöbe dieselbe Entscheidung an eine spätere Stelle und machte jede Auswertung zum Parser. |
| **Eine Spalte an `Karte` per `ALTER TABLE ADD COLUMN`** | Der `Migrationslaeufer` hat kein Journal und führt jedes Skript bei jedem Start aus — nicht idempotent, im zweiten Lauf ein Fehler. Eigene Tabelle, Muster `004`/`010`/`017`. |
| **Die Sollzeit am `Kartendetail` zeigen und editierbar machen** | Die Vision verbietet Planen im Board; ein ungezeigtes Feld wäre C24. Sichtbar wird das Soll in der Auswertung. |
| **Eine eigene Interaction „Sollzeit an der Karte" unter `D0004`** | Ein Feld ohne Erzeuger und ohne Leser — genau das Argument, mit dem `I0026` es nicht erfunden hat. |
| **Den Vergleich aus `GET .../kartenklassen/{id}/karten` plus N Kartendetails bauen** | N+1 Aufrufe über die Prozessgrenze für Daten, die ein Lesevorgang liefert; `Klassenkarte` trägt weder Zeiten noch Soll. |
| **`SUM` in SQL** | Der Bestand summiert Zeitspannen seit `B0436` in C#; Microsoft.Data.Sqlite meldet für Aggregatspalten ohne Tabellentyp `Byte[]`, und Dapper findet dann keinen Konstruktor. |
| **Die Zeiteinträge in der Antwort mitschicken** | Zwei Wahrheiten neben der Summe — und ein Agent, der selbst addieren muss. `I0037` liefert Rohdaten, dieser Slice liefert die Auswertung. |
| **Den Zeitraumfilter des Artboards mitbauen** | „Der Zeitraum gehört der Kalenderachse", also `I0034`. |
| **Karten ohne Soll aus der Tabelle nehmen** | Machte aus einer sichtbaren Lücke eine unsichtbare und ließe die Summe vollständig aussehen. |
| **Ein Vollständigkeitszeichen an Karten mit teilweise gefüllten Aufwänden** | Eine zweite Aussage neben der Zahl an jeder Zeile; die Fußzeile trägt den Fall, der zählt. Benannt statt gebaut. |
| **`Kartenabbild` nicht um das Band erweitern und stattdessen immer schreiben** | Machte jede Karte bei jedem Lauf `geaendert` und höhlte die Bilanz von `I0031` aus. |
| **`Kartenschreibauftrag` um ein eigenes Bandfeld erweitern** | Nicht nötig: er umschließt den `Kartenentwurf`, der das Band bereits trägt. |

### Bewusst out of scope

- Burndown (`I0034`), Puffer-Verbrauch (`I0035`), Zeitexport (`I0036`), Rohdaten über die API (`I0037`).
- Zeitraumfilter, Kalenderachse, Prognoselinie, Sollstrich.
- Sollzeit am Kartendetail, von Hand setzbares Soll, Endpunkt zum Setzen.
- Rückfluss vom Board in die Markdown-Datei.
- Eine Auswertung über mehrere Bestände oder über das ganze System.
- Aufwand aus einer anderen Quelle als der Spalte `Aufwand`.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Bandbreiten werden als Band gespeichert und als Band verglichen** — `SollzeitVonStunden`/`SollzeitBisStunden`, ein Einzelwert setzt beide gleich.
2. **Die Sollzeit einer Karte ist die Summe ihres Teilbaums**; `option`, `ausbau` und `verworfen` zählen samt Teilbaum nicht (und werden vom bestehenden `Umfangsfilter` bereits entfernt).
3. **Gesummt wird, was in den Zellen steht** — nicht die Berichtsform von `/planung zaehlen`. Daraus folgt die Korrektur der zwei Beispielzahlen in `B0477`.
4. **Der Kartenbestand ist Board × Kartenklasse, der Leseweg ist ein eigener** nach dem Vorbild von `LiesIststand`.
5. **Gerechnet wird in C#, in der BL**, kein `SUM` in SQL; überlappende Einträge zählen doppelt, Tagessummen über 24 h sind richtig.
6. **Der Slice gilt über beide Systemgrenzen**; `I0037` bleibt unberührt.
7. **Keine Karte fällt aus der Tabelle**; Karten mit teilweise gefüllten Aufwänden weisen ein Untermaß **ohne** Vollständigkeitszeichen aus.
8. **Die Sollzeit ist nicht von Hand änderbar.**
9. **Neue Tabelle statt Spalte**, weil der Migrationsläufer kein Journal hat.
10. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `Dokumentation/Wireframes/D0009.dc.html`, Zustände 1, 3 und 7 sind der Verweis für die Gestaltung von `F0062`; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet** — die dort leere Abweichungsspalte ist der Beleg dafür, dass das Bild die Entscheidung offenließ, die hier getroffen wird.
