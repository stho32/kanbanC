---
id: R00042
status: Neu
datum: 2026-09-08
---

# R00042: Puffer-Verbrauch sehen

## Beschreibung

Für einen Kartenbestand — Board und Kartenklasse zusammen — ist der **verbrauchte Puffer gegen den Fortschritt** ablesbar: als Fieberkurve mit drei Zonen und einem Punkt „heute", darüber Kettenpuffer, verbrauchte Stunden und ihr Anteil sowie der Soll-gewichtete Fortschritt, darunter je Karte Sollband, erfasste Zeit und verbrauchter Puffer. Die Zahlen kommen **gerechnet** über `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/puffer` aus der API und werden auf `/auswertungen` gezeigt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Auswertungen aus vollständigen Daten. Soll-Ist-Vergleich gegen die WBS-Zählung, Burndown, **Critical Chain mit Puffer-Verbrauch**".

### Der Gegenstand ist gefunden, nicht erfunden

Die Vision bestellt „Critical Chain mit Puffer-Verbrauch" (`R00000-vision.md:64`), definiert den Begriff aber nicht; keine andere Interaction nennt das Wort, und das Artboard stellt drei Lesarten nebeneinander, statt eine zu wählen. **Der Gegenstand dieses Slice wurde deshalb entschieden — und zwar in der Form, die der Bestand wirklich trägt:**

> *Die **Kette** ist der Kartenbestand (Board + Kartenklasse). Der **Puffer** ist die Spanne zwischen der aggressiven und der abgesicherten Soll-Summe: `Σ SollzeitVon` gegen `Σ SollzeitBis`. **Verbraucht** ist er, soweit die erfasste Zeit je Karte deren Untergrenze überschreitet. Der **Fortschritt** ist der Anteil des Solls der erledigten Karten am Soll der Kette.*

**Warum das kein erfundener Begriff ist — dreifach belegt:**

1. **Goldratts Puffer *ist* die Differenz zwischen aggressiver Schätzung und abgesicherter Zusage.** Genau diese beiden Zahlen führt die WBS bereits, weil ihre Aufwandsspalte Bandbreiten trägt (`2-4`, `0,4-1,5`) und weil **`I0033` entschieden hat, sie als Band zu speichern** statt auf eine Zahl zu reduzieren (`Kartensollzeit.SollzeitVonStunden` / `SollzeitBisStunden`, `Zeitband`). **Der Puffer musste nicht erfunden werden — er steht seit `R00036` in der Datenbank.**
2. **Nachgezählt an der eigenen WBS** (`Dokumentation/Planung/kanbanc.md`): **90 von 559 Aufwandszeilen tragen ein Band**; über alle Zeilen stehen **695,6 h Untergrenze gegen 841,4 h Obergrenze — 145,8 h Puffer, 21 % der aggressiven Summe.** Der Puffer ist im Bestand messbar vorhanden.
3. **`unter dem Band` / `im Band` / `über dem Band` aus `I0033` ist dieselbe Größe**, hier über die Kette summiert und als Anteil gelesen. `Abweichungsrechner` beantwortet sie je Zeile, `Pufferrechner` summiert sie zur Kette.

**Und sie rechnet ausschließlich aus Ist-Daten** — damit steht sie auf der richtigen Seite des Nicht-Ziels „Kein Gantt, keine Ressourcenauslastung, keine Termin- und Kapazitätsplanung. Burndown und Critical Chain rechnen aus den Ist-Daten; sie planen nicht" (`R00000-vision.md:116-118`).

## Geschäftlicher Nutzen

`I0033` sagt, **wie viel** Arbeit in einem Bestand steckt. `I0034` sagt, **wie er sich bewegt hat**. Keiner von beiden beantwortet die Frage, die vor einer Zusage steht: **wie viel Luft ist noch da, und ist sie schneller weg als die Arbeit fertig wird?** Genau das leistet die Gegenüberstellung von Verbrauch und Fortschritt: 40 % Verbrauch bei 80 % Fortschritt ist gesund, dieselben 40 % bei 10 % Fortschritt sind ein Alarm — und keine der beiden Zahlen sagt das für sich allein.

Der Wert liegt darin, dass die Antwort **nichts kostet**: keine Migration, keine neue Spalte, kein zweiter Erzeuger, keine Planungsgröße. Die 145,8 h Puffer der eigenen WBS liegen seit `I0030` in der Datei und seit `I0033` in der Datenbank; bisher hat sie niemand gelesen.

Und: **dieser Slice macht `D0009` und damit die Application `A0001` grün.** Er ist der letzte offene Slice des Projekts.

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/puffer` liefert für den Kartenbestand den **Kettenpuffer in Stunden**, die **verbrauchten Stunden** und ihren **Anteil**, den **Soll-gewichteten Fortschritt** sowie je Karte Nummer, Titel, Sollband, erfasste Zeit und verbrauchten Puffer.
- Der verbrauchte Puffer je Karte ist **`max(0; Ist − Von)`** — ohne Deckel nach oben und ohne Gegenrechnung nach unten.
- Ein **Bestand ohne Band** und eine **Kette ohne Puffer** antworten **200 ohne Prozentwerte** statt eines Fehlers oder einer Division durch null.
- Unbekanntes Board, unbekannte und **fremde** Kartenklasse antworten **404 mit Grund, Werten und Kompensationsaktion**.
- Auf `/auswertungen` ist `Puffer-Verbrauch` **wählbar** und zeigt die Fieberkurve mit den drei Zonen und dem Punkt „heute", darüber die Kopfzahlen, darunter die Kartentabelle.
- Die Kurve entsteht als **SVG von Hand**, ohne Diagrammpaket; der Punkt trägt seine Werte lesbar im DOM.
- Der **Fuß** der Fläche nennt den Aufruf der gewählten Auswertung — `GET …/puffer`.
- **Kein Zeitraumfilter**: der Verbrauch ist ein Stand, kein Verlauf. Die Filterzeile zeigt für diese Auswertung nur Board und Kartenklasse.

## Nicht-funktionale Anforderungen

- **Ein Lesevorgang je Bestand, nicht je Karte** — dieselbe Regel, unter der `LiesSollIst` und `LiesErledigungsstaende` stehen. 41 Karten kosten dieselbe Zahl Abfragen wie eine.
- **Die Antwort ist gerechnet, nicht roh.** Ein Agent bekommt Kettenpuffer, Verbrauch und Fortschritt, nicht deren Summanden; `I0037` bleibt der Ort der Rohdaten.
- **Prüfbar am DOM, nicht am Bild**: Zonen als `<polygon>`/`<path>`, der Punkt als `<circle>` mit lesbaren `cx`/`cy` — kein Canvas, kein Screenshot-Vergleich.
- **Keine Fremdabhängigkeit.** Kein CSS-Framework, kein Diagrammpaket, kein JS-Interop.
- Gestaltungswerte **ausschließlich** aus `gestaltung.css`, auch im SVG — keine Farb-, Abstands- oder Radiusliterale.
- **Keine Migration, keine Schemaänderung.** `Kartensollzeit`, `Zeiteintrag`, `Karteerledigung` und `Kartenarchivierung` werden nur gelesen.

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Für eine Kette ist der verbrauchte Puffer gegen den Fortschritt ablesbar."*

### Die vier Größen und ihr Rechenbeispiel

Das durchgehende Beispiel: Bestand aus **fünf** Karten der Kartenklasse `WBS` auf Board 4.

| Karte | Sollband (h) | erfasste Zeit (h) | erledigt | verbrauchter Puffer `max(0; Ist − Von)` |
|---|---|---|---|---|
| `K1` | 2,0–4,0 | 5,0 | ja | **3,0** |
| `K2` | 0,4–1,5 | 0,2 | nein | **0,0** (Untererfüllung wird nicht gutgeschrieben) |
| `K3` | 2,0–2,0 | 3,0 | ja | **1,0** |
| `K4` | 1,0–3,0 | 0,0 | nein | **0,0** |
| `K5` | *kein Band* | 8,0 | nein | *kein Wert* (nicht 0) |

- [ ] **Kettenpuffer** = `Σ Bis − Σ Von` = `10,5 − 5,4` = **5,1 h**. `K5` zählt in keiner der beiden Summen mit.
- [ ] **Verbrauchte Stunden** = `3,0 + 0,0 + 1,0 + 0,0` = **4,0 h**. Die 8,0 h von `K5` gehen **nicht** ein — ohne Band gibt es nichts zu verbrauchen.
- [ ] **Verbrauchsanteil** = `4,0 / 5,1` = **78 %** (78,4 %, kaufmännisch auf ganze Prozent).
- [ ] **Fortschritt (Soll-gewichtet)** = `Σ Von der erledigten Karten / Σ Von der Kette` = `4,0 / 5,4` = **74 %**.
- [ ] **Zweite Kopfzahl nach Kartenzahl**: **2 von 5 erledigt**. Beide Lesarten stehen nebeneinander, wie `I0034` es hält.
- [ ] **Karten ohne Soll**: **1** (`K5`) — als Zahl in der Fußzeile, wie in `I0033` und `I0034`.
- [ ] `K1` überschreitet mit 5,0 h auch ihre **Obergrenze** von 4,0 h; ihr Verbrauch bleibt **3,0 h und wird nicht auf 2,0 h gedeckelt**. Der Kettenanteil darf über 100 % laufen — das ist die rote Zone, keine Zahl, die man kappt.
- [ ] `K2` liegt mit 0,2 h **unter** ihrer Untergrenze von 0,4 h; ihr Verbrauch ist **0,0 und nicht −0,2**. Ohne diese Regel verdeckte eine kaum begonnene Karte den Überzug einer anderen.

### Die drei Leerfälle sind drei verschiedene Fälle

- [ ] **Bestand ohne Karten** → **200**, alle vier Größen ohne Wert, Kartenanzahl 0 — **nicht 404**.
- [ ] **Bestand ohne jedes Sollband** → **200**; Kettenpuffer, Verbrauch, Anteil und Fortschritt sind **ohne Wert** (`null`), **nicht 0,0**. Ohne Band gibt es weder Puffer noch Verbrauch (dieselbe Regel wie `Abweichungsrechner`).
- [ ] **Kette ohne Puffer** — jede Karte trägt eine **Punktschätzung**, also `Σ Bis − Σ Von = 0`: **200** mit Kettenpuffer **`0,0 h`** (eine echte Zahl!), verbrauchten Stunden als Zahl und **Verbrauchsanteil ohne Wert** statt einer Division durch null. Der **Fortschritt bleibt eine Zahl**, solange `Σ Von > 0`.
- [ ] Rechenbeispiel dazu: drei Karten mit `2,0–2,0` / 3,0 h Ist, `0,4–0,4` / 0,6 h und `1,0–1,0` / 1,4 h ergeben **Kettenpuffer 0,0 h · verbraucht 1,6 h** (`1,0 + 0,2 + 0,4`) **· Anteil ohne Wert · Fortschritt nach `Σ Von`**.
- [ ] **Der Unterschied zwischen „kein Band" (`null`) und „kein Puffer" (`0,0`) ist prüfbar** und wird nicht eingeebnet — er ist der Regelfall: **469 von 559 Aufwandszeilen der eigenen WBS sind Punktschätzungen**.

### Der Pufferstand über die API (`F0076`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/puffer` liefert für den Kartenbestand den Kettenpuffer in Stunden, die verbrauchten Stunden und ihren Anteil, den Soll-gewichteten Fortschritt und je Karte Nummer, Titel, Sollband, erfasste Zeit und verbrauchten Puffer; ein Bestand ohne Band und eine Kette ohne Puffer antworten 200 ohne Prozentwerte statt eines Fehlers; unbekanntes Board, unbekannte und fremde Kartenklasse antworten 404 mit Grund, Werten und Kompensationsaktion."*

- [ ] Der Aufruf antwortet **200** mit Kopfzahlen und Kartenzeilen in **Kartennummernfolge** — derselbe Schnitt und dieselbe Ordnung wie `soll-ist`.
- [ ] Der **Bestand** ist Board × Kartenklasse, dasselbe Set wie `GET …/kartenklassen/{kartenklasseId}/karten` (`I0022`), **archivierte Karten eingeschlossen** — ihre Zeit wurde geleistet.
- [ ] Je Zeile stehen **Nummer, Titel, Sollband, erfasste Zeit und verbrauchter Puffer**; eine Karte ohne Band trägt Band und Verbrauch **ohne Wert**.
- [ ] Die **erfasste Zeit** kommt über denselben Weg wie in `I0033`: ein **laufender Timer zählt nicht mit** (`AND e.Ende IS NOT NULL`).
- [ ] „**Erledigt**" heißt wortgleich wie in `I0034`: **`ErledigtAm` ist gesetzt** — nicht „steht in einer Abschlussspalte", nicht „ist archiviert".
- [ ] **Kein Zeitraumparameter.** Ein `?seit=` oder `?von=` wird nicht gelesen und nicht dokumentiert.
- [ ] Unbekanntes Board → **404**, Code `board-unbekannt`, Meldung mit der Board-Nummer, Kompensation `GET /api/boards`.
- [ ] Unbekannte Kartenklasse → **404**, Code `kartenklasse-unbekannt`; eine Kartenklasse eines **fremden** Boards ist der eigene Fall `kartenklasse-fremd` und nennt beide Board-Nummern.
- [ ] Die **Prüfreihenfolge ist dieselbe wie bei `Burndown`**: erst das Board, dann die Kartenklasse — die Karten einer fremden Klasse werden gar nicht erst gelesen.
- [ ] **Der Fehlervertragstest (`FehlervertragTests`) nimmt die neue Route auf** — sonst schlägt er fehl, weil er jede Route ohne Fehlerprüfung meldet.
- [ ] **Ohne Schirm prüfbar**: jedes Kriterium dieser Gruppe ist allein an der Antwort zu zeigen.

### Der Schirm zeigt die Fieberkurve (`F0077`)

Fertig-Kriterium wörtlich: *„Auf `/auswertungen` ist `Puffer-Verbrauch` wählbar und zeigt für den gewählten Bestand die Fieberkurve mit den drei Zonen und dem Punkt „heute" (Fortschritt waagerecht, Puffer-Verbrauch senkrecht), darüber Kettenpuffer, verbrauchte Stunden und Anteil sowie den Fortschritt, darunter je Karte Sollband, erfasste Zeit und verbrauchten Puffer; ein Bestand ohne Soll und eine Kette ohne Puffer stehen als Satz statt als Zahl; der Fuß nennt den Aufruf."*

- [ ] Der Punkt `Puffer-Verbrauch` im Umschalter ist **wählbar** — kein gesperrter `span#auswertung-puffer` mehr; die Liste `NochNichtGebaut` **bleibt leer stehen** und wird nicht abgebaut.
- [ ] Board- und Kartenklassenwahl bleiben **gemeinsam** über alle Auswertungen: ein Wechsel wirft die Wahl nicht weg.
- [ ] Die **Filterzeile zeigt für diese Auswertung kein Zeitraumelement** — anders als bei `Burndown` und `Zeitexport`.
- [ ] Die Fläche zeichnet **drei Zonen über zwei Geraden**, Achsen 0–100 % mit Beschriftung: waagerecht **Fortschritt**, senkrecht **Puffer-Verbrauch**.
- [ ] Die Zonengrenzen sind **festgelegt und geprüft**: grün/gelb verläuft von `(0 %, 0 %)` nach `(100 %, 66,7 %)`, gelb/rot von `(0 %, 33,3 %)` nach `(100 %, 100 %)`.
- [ ] **Zonenprobe am Rechenbeispiel**: bei 74 % Fortschritt liegt die grün/gelb-Grenze bei **49,3 %** und die gelb/rot-Grenze bei **82,7 %**; der Punkt bei **78 %** liegt damit in **Gelb**. Ein Verbrauch von 90 % läge bei demselben Fortschritt in **Rot**, einer von 30 % in **Grün**.
- [ ] Der **Punkt „heute"** ist ein `<circle>`, dessen `cx` und `cy` aus Fortschritt und Verbrauch entstehen und im Test lesbar sind; sein Wert steht daneben als Text.
- [ ] **Über** der Kurve stehen **Kettenpuffer · verbrauchte Stunden · Anteil · Fortschritt**, dazu die Kartenzahl als zweite Lesart des Fortschritts.
- [ ] **Unter** der Kurve steht je Karte **Nummer, Titel, Sollband, erfasste Zeit und verbrauchter Puffer** — in der Form der `SollIstTabelle`.
- [ ] **Archivierte Karten stehen markiert in der Tabelle**, sie werden nicht ausgelassen: ihre Zeit wurde geleistet.
- [ ] Die Tabelle ist die Antwort auf „**welche Karte frisst den Puffer**", die die Kurve nicht gibt.
- [ ] Rand 1 — **Bestand ohne Karten**: lesbare Leermeldung statt leerer Fläche.
- [ ] Rand 2 — **Bestand ohne jedes Sollband**: **ein Satz mit Kompensationsaktion** („die WBS-Datei mit Aufwandsspalte erneut einfahren") statt einer Kurve ohne Achsenwerte.
- [ ] Rand 3 — **Kette ohne Puffer**: **ein Satz** („dieser Bestand trägt nur Punktschätzungen — es gibt keinen Puffer zu verbrauchen") plus die **Stundenzahlen**, die es sehr wohl gibt; **keine Kurve**, weil die senkrechte Achse ohne Nenner keine Bedeutung hat.
- [ ] Rand 4 — **Verbrauch über 100 %**: der Punkt wird **am oberen Rand gezeigt und der Zahlenwert genannt** (z. B. „137 %"); er wird nicht auf 100 % zurückgezogen.
- [ ] Rand 5 — **Fortschritt 0 % bei laufender Arbeit**: der Punkt sitzt am linken Rand; jeder Verbrauch über 0 % steht damit sofort in Gelb oder Rot — das ist die Aussage, kein Fehler.
- [ ] Rand 6 — **Board ohne Kartenklasse**: derselbe Hinweis wie bei den übrigen Auswertungen, kein Aufruf.
- [ ] Rand 7 — **WebApi nicht erreichbar**: lesbare Meldung über `WebApiAufruf.MitAusfallmeldung` statt Ausnahmeseite; der Umschalter bleibt stehen.
- [ ] Der **Fuß** zeigt `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/puffer` — ohne Abfrageparameter.
- [ ] **Kein Gestaltungsliteral** in `Fieberkurve.razor`, `Pufferflaeche.razor` und ihren Stilvorlagen; geprüft wie in `AuswertungsflaecheTests`.

### Der grüne Bestand bleibt grün

- [ ] `I0033`, `I0034`, `I0036` und `I0037` werden **nicht umgebaut**: `LiesSollIst`, `LiesErledigungsstaende`, `LiesZeiteintraege`, `AuswertungsService.SollIst` / `Burndown` / `Zeitexport` und die drei bestehenden Routen bleiben, wie sie sind. Sie **wachsen** nur dort, wo eine vierte Auskunft dazukommt.
- [ ] Die Suiten von `R00036`, `R00037`, `R00038` und `R00039` bleiben grün.
- [ ] **Keine Migration, keine Schemaänderung, keine neue Spalte.**
- [ ] **`KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL`** — der Weg führt über HTTP.

### Was diese Anforderung ausdrücklich nicht leistet

- [ ] **Kein kritischer Pfad und keine Reihenfolge.** Die Menge heißt „Kette", weil **ohne Abhängigkeitsgraph jede Karte auf ihr liegt** — das ist die konservative Lesart, keine gerechnete.
- [ ] **Kein Feeding-Buffer**, keine Puffer zweiter Ordnung.
- [ ] **Keine Ressourcenkonkurrenz**, keine Kontributorenauslastung.
- [ ] **Keine Aussage darüber, welche Karte den Termin treibt.** Die Tabelle sagt, welche Karte am meisten verbraucht hat — nicht, welche kritisch ist.
- [ ] **Keine Kalenderaussage.** Die waagerechte Achse ist **erledigtes Soll, nicht verstrichene Zeit**: **ein ruhendes Board bewegt den Punkt nicht.**
- [ ] **Keine Bahn über Kalendertage.** „Ablesbar" meint den **Stand** — ein Punkt „heute", keine Historie der Punkte. Rechenbar wäre sie; bestellt ist sie nicht (Ausbaustufe).
- [ ] **Kein Board-Termin.** `Starttermin` und `Zieltermin` werden nicht gelesen.
- [ ] **Kein Abhängigkeitsgraph** und **kein Slice, der einen vorschlägt** — siehe „Verworfene Alternativen".

## Betroffene Verzeichnisstruktur

Alle Themenordner stehen bereits — dieser Slice legt **keinen** neuen an.

- **Contracts**: `KanbanC.Contracts/Auswertungen` — `Pufferauswertung`, `Pufferzeile`, `Pufferkopfzahlen`.
- **BL**: `KanbanC.BL/Models/Auswertungen` (`Pufferstandkarte`, `Pufferstandkarten`, `Pufferkette`), `Operations/Auswertungen` (`Pufferrechner`), `Integrations/Auswertungen` (`AuswertungsService`), `Interfaces/Auswertungen` (`IAuswertungsrepository`), `Persistenz/Auswertungen` (`Auswertungsrepository`).
- **API**: `KanbanC.WebApi/Endpunkte/AuswertungsEndpunkte.cs` — vierte Route, keine neue Datei.
- **Oberfläche**: `KanbanC.Blazor/Components/Pages/Auswertungen.razor` (+ `.razor.css`), `Components/Auswertungen/Pufferflaeche.razor` und `Fieberkurve.razor` (+ `.razor.css`), `Services/AuswertungenApiKlient.cs` und die reine Formoperation daneben. **Keine Projektreferenz auf `KanbanC.BL`.**
- **Tests**: `KanbanC.BL.Tests/{Operations,Models,Integrations}/Auswertungen`, `KanbanC.WebApi.IntegrationTests/{Persistenz/Auswertungen,Api}`, `KanbanC.Blazor.Tests/{Services,Gestaltung}`, `KanbanC.PlaywrightTests` (Seitenobjekt `AuswertungenSeite` wächst).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Die Datengrundlage ist vollständig da — nachgesehen, nicht angenommen

| Größe | Quelle | seit |
|---|---|---|
| Sollband je Karte | `Kartensollzeit.SollzeitVonStunden` / `SollzeitBisStunden`, gelesen als `Zeitband` | `I0033` (`R00036`), geschrieben vom WBS-Import `I0030` |
| erfasste Zeit je Karte | `Zeiteintrag`, summiert in C#, laufende Einträge draußen | `I0026` |
| Erledigung je Karte | `Karteerledigung.ErledigtAm` | `I0034` (`R00037`), geschrieben seit Migration `008` |
| Kartenbestand | Board × Kartenklasse über `Spalte` und `Kartenklassenzuordnung` | `I0022` |

**Ein drittes Feature für die Datengrundlage entfällt** — anders als bei `I0033`, das die Sollzeit erst erfinden musste.

### Der Leseweg: der dritte Schnitt derselben Form

`LiesPufferstaende` ist der Zwilling von `LiesSollIst` und `LiesErledigungsstaende`: derselbe Schnitt über `Karte → Spalte → Kartenklassenzuordnung → Kartenklasse`, gebunden an `s.Board` und `z.Kartenklasse`, `LEFT JOIN Kartensollzeit`, `LEFT JOIN Kartenarchivierung` — **neu ist allein der `LEFT JOIN Karteerledigung`**, weil der Fortschritt das `ErledigtAm` braucht. Die Zeiten kommen weiter über den bestehenden `LiesErfassteZeiten`-Weg.

**`LiesSollIst` bleibt unangetastet.** Die Versuchung, den Erledigungsstand dort anzuflanschen und beide Auswertungen aus einem Lesevorgang zu bedienen, wird nicht ergriffen: `I0033` ist grün, und eine grüne Auskunft um ein Feld zu erweitern, das sie nicht braucht, macht aus einem fertigen Slice einen halben.

**Keine Aggregate in SQL.** Microsoft.Data.Sqlite meldet für eine Aggregatspalte ohne Tabellentyp `Byte[]`, und Dapper findet dann keinen Konstruktor — im Bestand belegt und kommentiert. Summiert wird in C#, wo ohnehin die Regel steht.

### Die Regel wohnt an zwei Stellen, und nur an zweien

`Pufferrechner` entscheidet **je Karte**, was Puffer und Verbrauch sind; `Pufferkette` entscheidet, **was daraus für die Menge folgt**. Kopfzahlen, Kurve und Tabelle lesen aus **derselben** `Pufferauswertung`; keine der drei rechnet nach. Dieselbe Hausregel, unter der die Summenzeile von `I0033` und die Tagesreihe von `I0034` stehen.

`Pufferrechner` ist die **Schwester von `Abweichungsrechner`** und erbt dessen Umrechnung auf die Minute: die erfasste Zeit entsteht aus Zeitpunkten, das Soll aus Zehntelstunden — ohne diese Umrechnung stünden zwei Größen gegeneinander, die nie gleich sein können. Und er erbt dessen `null`-Regel: **ohne Sollband gibt es weder Puffer noch Verbrauch — `null`, nicht 0** (C25).

Die Summen wohnen bei der Menge (`Pufferkette`), nicht im Dienst — genau wie `SollIstKarten.Bandsumme` und `SollIstKarten.KartenOhneSoll`. Sie sind Aussagen über **diese** Menge.

### `null` und `0,0` sind verschiedene Antworten

Drei Lagen, drei Ergebnisse — und sie einzuebnen wäre der teuerste Fehler dieses Slice:

| Lage | Kettenpuffer | verbrauchte Stunden | Anteil | Fortschritt |
|---|---|---|---|---|
| keine Karte im Bestand | `null` | `null` | `null` | `null` |
| keine Karte trägt ein Band | `null` | `null` | `null` | `null` |
| alle Bänder sind Punktschätzungen | **`0,0`** | **Zahl** | `null` | **Zahl** |
| Regelfall | Zahl | Zahl | Zahl | Zahl |

`Bandsumme` liefert bereits heute `null` statt `0,0–0,0`, wenn keine Karte ein Band trägt (`SollIstKarten`); der Pufferstand folgt dieser Entscheidung, statt eine zweite zu treffen.

### Der Verbrauch hat keinen Deckel und keine Gegenrechnung

`max(0; Ist − Von)` je Karte, dann summiert. Zwei Versuchungen werden ausdrücklich abgelehnt:

- **Nach oben kappen** (auf den Kartenpuffer `Bis − Von`): dann liefe der Kettenanteil nie über 100 %, und **genau die rote Zone der Fieberkurve verschwände**. Ein Überzug ist eine ehrliche Zahl.
- **Nach unten gegenrechnen** (Untererfüllung gutschreiben): eine kaum begonnene Karte hat ein Ist weit unter ihrer Untergrenze; ein gegengerechneter Verbrauch wäre damit vor allem eine **Funktion des Nichtgetanen** — und der Überzug einer Karte verschwände hinter der Untätigkeit einer anderen.

### Der Fortschritt ist Soll-gewichtet — mit `Σ Von` als Nenner

Gewichtet wird nach der **aggressiven** Summe, nicht nach der abgesicherten: der Verbrauch wird gegen `Von` gemessen, also muss der Fortschritt es auch, sonst stünden auf den beiden Achsen zwei verschiedene Sollbegriffe. Nach Kartenzahl zu zählen wäre die zweite Möglichkeit — sie steht als **zweite Kopfzahl daneben**, wie `I0034` sie führt, damit beide Lesarten sichtbar bleiben; die Achse aber trägt Stunden gegen Stunden, sonst bedeuten die Zonen nichts.

**Karten ohne Soll zählen weder in Kette noch Verbrauch noch Fortschritt** und stehen als Zahl in der Fußzeile — dieselbe Handhabung wie in `I0033` und `I0034`.

### Die Zonen sind eine Darstellungskonvention, keine Domänenwahrheit

Zwei Geraden, festgelegt und an **einer** Stelle gerechnet:

```
gruen/gelb:  (0 %, 0,0 %)  → (100 %, 66,7 %)
gelb/rot:    (0 %, 33,3 %) → (100 %, 100 %)
```

Die Wahl ist die symmetrische Drittelung des Einheitsquadrats — die verbreitetste Form der Fieberkurve und die einzige, die ohne eine Planungsgröße auskommt. **Sie ist Konvention und wird als solche benannt**; sie steht als Konstanten in **einer** puren Operation, damit eine spätere Änderung eine Zeile ist und kein Umbau. Ein Bedienelement, mit dem der Nutzer die Zonen verschiebt, gehört nicht in diesen Slice.

Farben aus `gestaltung.css`: das Blatt führt **keine** Statuspalette, deshalb tragen die Zonen die vorhandenen Skalen — `--color-accent-2-*` (Olivgrün) für Grün, `--color-accent-300/400` für Gelb, `--color-accent-700` für Rot. **Kein Literal, und kein neuer Token, der nur hier gebraucht würde.**

### Gezeichnet wird SVG von Hand

Dieselbe Entscheidung und derselbe Grund wie in `B0497` (`I0034`): ein Diagrammpaket brächte JS-Interop in eine Anwendung, die ohne auskommt, in der Regel eine **Canvas-Fläche ohne prüfbaren DOM** und die **erste Fremdabhängigkeit** in einer Oberfläche ohne CSS-Framework.

**Und es ist der prüfbarere Weg**: der E2E-Test liest `cx` und `cy` des Punktes und vergleicht Zahlen — kein Bild, kein Screenshot. Die Skalierung (Prozent auf Zeichenfläche, Zonenpolygone aus den zwei Geraden) ist pure Arithmetik und gehört in eine eigene Operation, damit sie im Unit Test steht statt im Markup — wie `Kurvenpunkte` es für den Burndown tut.

### Die Adressform folgt dem Bestand

```
GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/puffer
```

Dieselbe Form wie `soll-ist`, `burndown` und `zeiten` — der Bestand steht in der Adresse, weil er den Bestand benennt. **Kein Zeitraumfilter**: der Verbrauch ist ein Stand und kein Verlauf; ein `?seit=` schnitte eine Achse zu, die es hier nicht gibt.

### Das Artboard ist Entwurf — und sein Zustand 4 ist überholt

`Dokumentation/Wireframes/D0009.dc.html`, **Zustand 4** zeichnet diesen Slice als **Frage mit drei Lesarten**, nicht als Antwort. Er ist die Quelle für die **Gestaltung** der Fläche (Filterzeile, Kopfzahlen, Fuß — in der Form von Zustand 2), **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet.**

Zwei seiner Aussagen sind vom Bestand überholt und werden hier ausdrücklich richtiggestellt, ohne das Bild zu ändern (die Wireframes werden nicht nachgeführt):

- „fehlt · der Puffer … es gibt keine Sollzeit" — **die Sollzeit gibt es seit `I0033`**, als Band.
- „`I0035` hängt in der WBS an `I0034`, nicht an `I0033` — ein Befund für `/planung`" — **behoben**: `Braucht` steht auf `I0033, I0034`, beide grün.

## Ablauf

1. **Prüfen** — `AuswertungsService.PruefeBestand(boardId, kartenklasseId)`: erst das Board, dann die Kartenklasse → `Nichtgefunden.Board` / `Nichtgefunden.Kartenklasse` / `Nichtgefunden.FremdeKartenklasse`.
2. **Lesen** — `Auswertungsrepository.LiesPufferstaende(boardId, kartenklasseId)` → `Pufferstandkarten` (**ein** Lesevorgang plus der bestehende Zeitenschnitt).
3. **Rechnen je Karte** — `Pufferrechner.Rechne(erfassteZeit, sollband)` → `Kartenpuffer?` mit `PufferStunden = Bis − Von` und `VerbrauchteStunden = max(0; Ist − Von)`.
4. **Rechnen je Kette** — `Pufferkette` über `Pufferstandkarten`
   - 4.1 `Kettenpuffer` = `Bandsumme.Bis − Bandsumme.Von`, `null` ohne Bandsumme
   - 4.2 `VerbrauchteStunden` = Summe der Kartenverbräuche
   - 4.3 `VerbrauchsanteilProzent` = `Verbrauch / Kettenpuffer`, **`null` bei Kettenpuffer 0**
   - 4.4 `FortschrittProzent` = `Σ Von der erledigten / Σ Von der Kette`, `null` ohne Bandsumme
5. **Zusammensetzen** — `AuswertungsService.Puffer(…)` → `Ergebnis<Pufferauswertung>`.
6. **Ausliefern** — `AuswertungsEndpunkte` → 200 / 404.
7. **Zeigen** — `AuswertungenApiKlient.LadePufferstand`, `Auswertungen.razor` (fünfter gebauter Punkt), `Pufferflaeche.razor`, `Fieberkurve.razor`.

## Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** die vierte Route in `AuswertungsEndpunkte`; der Punkt `puffer` im Umschalter von `Auswertungen.razor`, der heute als einziger Eintrag in `NochNichtGebaut` steht; die vierte Methode an `IAuswertungsrepository`, `AuswertungsService` und `AuswertungenApiKlient`.

**In `KanbanC.Contracts/Auswertungen`** (immutable, C08):
- `Pufferzeile` (DTO) — eine Karte des Bestands, wie die Tabelle sie zeigt.
  - `long KarteId`, `string? Kartennummer`, `string Titel`, `Zeitband? Sollband`, `TimeSpan ErfassteZeit`, `decimal? VerbrauchterPufferStunden`, `bool IstErledigt`, `bool IstArchiviert`
- `Pufferkopfzahlen` (DTO) — die Zahlen über der Kurve, **gerechnet geliefert**; die Oberfläche summiert nichts nach. Stunden als `decimal` wie `Zeitband`, Anteile als `decimal?` — **`null` heißt „diese Kette hat keinen Puffer" und nicht 0.**
  - `decimal? KettenpufferStunden`, `decimal? VerbrauchteStunden`, `decimal? VerbrauchsanteilProzent`, `decimal? FortschrittProzent`, `int ErledigteKarten`, `int Kartenanzahl`, `int KartenOhneSoll`
- `Pufferauswertung` (DTO) — `IReadOnlyList<Pufferzeile> Zeilen`, `Pufferkopfzahlen Kopfzahlen`

**In `KanbanC.BL/Models/Auswertungen`:**
- `Pufferstandkarte` (DTO, immutable) — eine gelesene Karte: Nummer, Titel, `TimeSpan ErfassteZeit`, `Zeitband? Sollband`, `DateOnly? ErledigtAm`, `bool IstArchiviert`.
- `Pufferstandkarten` (benannte Collection) — die Karten in Kartennummernfolge.
- `Pufferkette` (Modell auf `Pufferstandkarten`) — beantwortet die vier Fragen der Kopfzeile: Kettenpuffer, verbrauchte Stunden, Anteil, Soll-gewichteter Fortschritt; dazu erledigte Karten und Karten ohne Soll. **Die Summen wohnen bei der Menge, nicht im Dienst.**

**In `KanbanC.BL/Operations/Auswertungen`:**
- `Pufferrechner` (Operation, pur) — Schwester von `Abweichungsrechner`, dieselbe Umrechnung auf die Minute.
  - `static Kartenpuffer? Rechne(TimeSpan erfassteZeit, Zeitband? sollband)`
- `Kartenpuffer` (DTO, immutable) — `decimal PufferStunden`, `decimal VerbrauchteStunden`

**In `KanbanC.BL/Interfaces/Auswertungen` und `KanbanC.BL/Persistenz/Auswertungen`:**
- `IAuswertungsrepository` — wächst um eine **vierte** Auskunft (zwei Implementationen: echte und `TestAuswertungsrepository`).
  - `Pufferstandkarten LiesPufferstaende(long boardId, long kartenklasseId)`
- `Auswertungsrepository` (Provider/Ressourcenzugriff) — ein Lesevorgang je Bestand, Rohzeilen über Dapper, Summen in C#.

**In `KanbanC.BL/Integrations/Auswertungen`:**
- `AuswertungsService` (Integration) — wächst um eine vierte Auskunft; `PruefeBestand` wird **geteilt, nicht kopiert**.
  - `Ergebnis<Pufferauswertung> Puffer(long boardId, long kartenklasseId)`

**In `KanbanC.WebApi/Endpunkte`:**
- `AuswertungsEndpunkte` — vierte Route `…/puffer`, 200 / 404.

**In `KanbanC.Blazor`:**
- `AuswertungenApiKlient` — `Task<ApiErgebnis<Pufferauswertung>> LadePufferstand(long boardId, long kartenklasseId)`
- `Fieberkurvenbild` (Operation, pur) — rechnet aus Fortschritt und Verbrauch die Punktlage und aus den zwei Geraden die Zonenpolygone. **Die Arithmetik des Bildes gehört in einen Unit Test, nicht ins Markup.**
- `Fieberkurve.razor` (Komponente) — das SVG: Zonenflächen, Achsen mit Beschriftung, ein Punkt mit Wertangabe.
- `Pufferflaeche.razor` (Komponente) — Kopfzahlen, Kurve, Kartentabelle, Fußzeile.
- `Auswertungen.razor` — `Auswertungswahl` wächst um `Puffer`, `Gebaut` um einen Eintrag, `NochNichtGebaut` wird leer; der Fuß bekommt einen Zweig; **die Filterzeile zeigt kein Zeitraumelement für diese Wahl**.

**Kein Interface** für die reinen Operationen: je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `IAuswertungsrepository` | vierte Auskunft `LiesPufferstaende` |
| `Auswertungsrepository` | vierter Lesevorgang, Muster von `LiesSollIst` plus `LEFT JOIN Karteerledigung` |
| `TestAuswertungsrepository` | zieht mit |
| `AuswertungsService` | vierte Auskunft `Puffer`; `PruefeBestand` wird geteilt, nicht kopiert |
| `AuswertungsEndpunkte` | vierte Route |
| `AuswertungenApiKlient` | `LadePufferstand` |
| `Auswertungen.razor` | `Auswertungswahl.Puffer`; `puffer` verlässt `NochNichtGebaut` (die Liste bleibt leer stehen); Fuß folgt der Wahl; kein Zeitraumelement |
| `Auswertungen.razor.css` | Fläche für Kurve und Kartentabelle |
| `FehlervertragTests` | nimmt die neue Route auf |
| `AuswertungenSeite` (E2E) | Locator für Umschalterpunkt, Kopfzahlen, Kurve, Punkt und Kartentabelle |
| `Program.cs` (WebApi) | nichts Neues zu registrieren — Repository, Dienst und Endpunkte stehen |

**Nicht geändert:** `LiesSollIst`, `LiesErledigungsstaende`, `LiesZeiteintraege`, `Abweichungsrechner`, `Burndownrechner`, `Burndownzeitraum`, `Zeitraumfilter`, `Zeitausschnitt`, alle `SollIst…`-, `Burndown…`- und `Zeitexport…`-Verträge, `SollIstTabelle.razor`, `Burndownflaeche.razor`, `Zeitexportflaeche.razor`, `Rohdatenflaeche.razor`, `KartenRepository`, `WbsImportRepository`, alles unter `Persistenz/Migrationen/`.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Pufferrechner` — `2,0–4,0` mit 5,0 h Ist → Puffer 2,0 / Verbrauch 3,0; `0,4–1,5` mit 0,2 h → Verbrauch **0,0, nicht −0,2**; `2,0–2,0` mit 3,0 h → Puffer **0,0**, Verbrauch 1,0; **ohne Band → `null`, nicht 0**; Rundung auf die Minute (z. B. 90 Minuten = 1,5 h).
- `Pufferkette` — **das Rechenbeispiel oben in voller Länge**: Kettenpuffer 5,1 h, verbraucht 4,0 h, Anteil 78 %, Fortschritt 74 %, 2 von 5 erledigt, 1 ohne Soll. Dazu je ein Test für: leere Menge, Menge ohne jedes Band (alles `null`), Menge nur aus Punktschätzungen (**Kettenpuffer `0,0`, Anteil `null`, Fortschritt Zahl**), Verbrauch über 100 %, Fortschritt 0 % bei laufender Arbeit.
- `Fieberkurvenbild` (in `KanbanC.Blazor.Tests`) — Punktlage bei 0/0, 100/100, 74/78; Zonengrenzen bei 74 % Fortschritt (49,3 % und 82,7 %); Verbrauch über 100 % wird am oberen Rand gezeigt und nicht gekappt; keine Division durch null bei fehlenden Werten.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `LiesPufferstaende`: Bestand mit Bändern, Punktschätzungen, Karten ohne Band, erledigten, archivierten und offenen Karten — **ein** Lesevorgang, alle Felder gefüllt; ein **laufender** Zeiteintrag zählt nicht mit.
- `GET …/puffer`: 200 mit Kopfzahlen und Zeilen; **200 ohne Prozentwerte** für den Bestand ohne Band und für die Kette ohne Puffer; 404 für unbekanntes Board, unbekannte und fremde Kartenklasse — jeweils mit Grund, Werten und Kompensationsaktion.
- Bestand ohne Karten → **200**, nicht 404.
- `FehlervertragTests` nimmt die Route auf.
- **Eine Karte aus der Abschlussspalte herausziehen** und erneut abrufen: der Fortschritt sinkt, der Verbrauch bleibt — die beiden Achsen sind damit **belegt statt behauptet** unabhängig.

**`KanbanC.Blazor.Tests`:** `AuswertungenApiKlient.LadePufferstand` — 200 wird gelesen, 404 wird zur lesbaren Zurückweisung, `HttpRequestException` zur Ausfallmeldung. **Diese Pfade sind über den Browser nicht auslösbar** — genau der Grund, aus dem dieses Testprojekt existiert. Dazu die Gestaltungsprüfung: `Fieberkurve.razor`, `Pufferflaeche.razor` und ihre Stilvorlagen ohne Farb-, Abstands- und Radiusliteral.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — eine kleine WBS mit **Bändern und Punktschätzungen** einfahren (wie `B0431`/`B0454`, nicht `kanbanc.md`), auf einer Karte Zeit **über der Untergrenze** erfassen, eine Karte in die Abschlussspalte ziehen, `/auswertungen` öffnen, `Puffer-Verbrauch` wählen, **Kopfzahlen, `cx`/`cy` des Punktes und eine Kartenzeile lesen**; dazu der Fuß, der `…/puffer` zeigt.

## Abhängigkeiten

- Abhängig von: **`R00036`** (Soll-Ist-Vergleich abrufen — `I0033`, **grün**) und **`R00037`** (Burndown sehen — `I0034`, **grün**), die beiden `Braucht` der Interaction. `R00036` liefert das **Sollband** und den Leseweg, `R00037` den **Erledigungsbegriff** (`ErledigtAm`) und das Muster „SVG von Hand"; `Auswertungsrepository`, `AuswertungsService`, `AuswertungsEndpunkte`, `AuswertungenApiKlient` und der Schirm `/auswertungen` mit seinem Umschalter stammen von dort.
- Setzt außerdem auf (alle grün): **`R00033`** (WBS-Import schreibt `Kartensollzeit` als Band), **`R00028`** (erfasste Zeit je Karte), **`R00016`** (Archivstand), **`R00022`**/**`R00023`**/**`R00025`** (Kartenklassen, Kartennummern, der Mengenbegriff „Karten einer Klasse"), **`R00005`** (Kopfzeile und Gestaltungstokens).
- Blockiert: **nichts.** Kein Knoten der WBS führt `Braucht: I0035`.
- **Mit diesem Slice werden `D0009` und die Application `A0001` grün** — er ist der letzte offene Slice des Projekts.

## Umfang

```
Puffer-Verbrauch sehen (I0035) = 13 Bubbles: 12 Standard (17,6h), 1 unklar (2,0-4,0h).
Rest: 17,6h klar + 2,0-4,0h unklar · 0 von 13 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 13 Bubbles gruen (0 %) · 0 laufen · 13 offen
```

`I0035` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **zwei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0076` Der Pufferstand über die API | `B0561`–`B0567` (7) | 7 (9,2h) | 0 | — |
| `F0077` Der Schirm zeigt die Fieberkurve | `B0568`–`B0573` (6) | 5 (8,4h) | 1 (2,0–4,0h) | `F0076` |

**Warum zwei Features:** weil zwei Aspekte **getrennt fertig** werden. `F0076` ist allein an der 200-Antwort prüfbar und könnte vollständig sein, während der Schirm noch nichts zeigt; `F0077` zeichnet, was `F0076` liefert. **Ein drittes Feature für die Datengrundlage entfällt** — sie ist vollständig da.

Die eine unklare Bubble ist `B0573` (E2E über beide Prozesse); die Unklarheit ist benannt: *die Karte braucht ein Sollband mit Spanne, das nur der Import setzt.*

**Nach gemessenem Durchsatz ist mit etwa 1,5–2,0 h zu rechnen.** Die Richtwert-Konvention überschätzt messbar, und das ist über den ganzen Baum belegt (`Schaetzungen/_ist-zeiten.md`):

| Slice | gezählt | gemessen | Faktor |
|---|---|---|---|
| `I0030` | 32,0–44,0h | 2,4h | ~13–18 |
| `I0038` | 14,4h | 1,5h | ~10 |
| `I0039` | 21,6–23,6h | 2,2h | ~10–11 |

Die Zählung wird trotzdem nicht still gekippt — eine Konvention, die am Ende eines Baums wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0035` und an beiden Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00041`/`I0039`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Der Gegenstand war nicht vereinbart und ist hier entschieden.** Die Vision bestellt „Critical Chain mit Puffer-Verbrauch", ohne den Begriff zu definieren; das Artboard stellt drei Lesarten nebeneinander. Gewählt ist die erste, in der Form, die der Bestand trägt — die Begründung steht oben, die zwei verworfenen unter „Verworfene Alternativen". **Entschieden im stillen Lauf, nicht am Menschen geprüft.**
- **Der Nenner des Fortschritts ist `Σ Von`, nicht `Σ Bis`.** — **entschieden**: der Verbrauch wird gegen `Von` gemessen, also muss der Fortschritt es auch, sonst tragen die beiden Achsen zwei Sollbegriffe. **Nicht am Menschen geprüft.**
- **Die Zonengeometrie ist eine Konvention.** Die symmetrische Drittelung (`0 → 66,7` und `33,3 → 100`) ist begründbar, aber nicht die einzige mögliche. Sie steht als Konstanten an einer Stelle, damit eine andere Wahl eine Zeile ist. **Angenommen, nicht belegt.**
- **Die Fieberkurve über Kalendertage ist rechenbar und nicht bestellt.** `ErledigtAm` trägt einen Tag, Zeiteinträge tragen Beginn und Ende — eine Bahn wäre möglich, brächte aber die Tagesaufteilung überschlagender Einträge mit. **Als Ausbaustufe vorgemerkt; Befund für `/planung`, hier nicht geändert** — diese Familie ändert keine Knoten.
- **Der Punkt ist eine Momentaufnahme, kein Ereignisprotokoll** — dieselbe Grenze, die `R00037` bereits benannt hat: wer eine Karte aus der Abschlussspalte zieht, verliert ihr `ErledigtAm`, und der Fortschritt sinkt rückwirkend. Wer wann welche Karte bewegt hat, hält nichts fest; die Ereignisspur wurde in `I0028` verworfen. **Der Slice baut keine; er benennt die Grenze.**
- **Ein Abhängigkeitsgraph wird ausdrücklich nicht vorgeschlagen.** Er wäre kein Nachtrag, sondern eine **Änderung des Zielbilds** — siehe „Verworfene Alternativen". **Nicht am Menschen geprüft.**
- **Das Artboard bleibt stehen, obwohl zwei seiner Aussagen überholt sind.** Die Wireframes sind Dokumentation und werden nicht nachgeführt, wenn der Code sie einholt (Projektkonvention). Die Richtigstellung steht in dieser Anforderung. **Kein Änderungsauftrag an `/wireframe`.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit und liest nur, was seit `I0030`, `I0026` und `I0011` ohnehin geschrieben wird.

## Manuelle Nachbereitungstätigkeiten

- Keine. Karten aus der Zeit vor dem WBS-Import tragen kein Sollband und bleiben ohne Puffer; ihre Zahl steht in der Fußzeile. **Ein nachträgliches Band wird nicht gesetzt** — es wäre erfunden.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt steht im Anlass der Vision: für eine eigene Critical Chain werden sehr spezielle Daten gebraucht, und eine fremde Cloud-API mit Limits gibt sie nicht her. Diese Daten hat das Board inzwischen vollständig — das Sollband je Karte seit `I0033`, die erfasste Zeit seit `I0026`, das `ErledigtAm` seit `I0034` —, aber die eine Zahl, die sie zusammen ergeben, rechnet niemand aus: die 145,8 h Puffer der eigenen WBS liegen ungelesen in der Datenbank. Wenn eine einzige Rechenstelle aus dem Band je Karte den Kettenpuffer und aus `max(0; Ist − Von)` seinen Verbrauch bildet und beides gegen den Soll-gewichteten Fortschritt stellt (X), dann beantwortet dieselbe Auswertung dem Menschen am Schirm und dem Agenten an der Route die Frage „ist die Luft schneller weg als die Arbeit fertig wird" (Y), sodass eine Zusage auf einer gemessenen Größe steht statt auf einem Gefühl (Z). Der Hebel sitzt genau hier und nicht vorgelagert: an der Datenhaltung ist nichts zu tun, ein Abhängigkeitsfeld oder ein Board-Termin wäre Aufwand gegen ein Nicht-Ziel der Vision. Und er sitzt nicht nachgelagert bei `I0037`: dort kämen Rohdaten heraus, aus denen jeder Leser den Puffer selbst rechnen müsste — was der Kernregel des Projekts widerspricht, dass ein Agent von der API bekommt, was ein Mensch am Schirm sieht.

## Missing-Docs

- **Fieberkurve als Darstellungsform.** Wo die Zonengrenzen einer Critical-Chain-Fieberkurve herkommen und welche Varianten üblich sind, ist im Repository nirgends notiert; die hier gewählte Drittelung ist begründet, aber nicht belegt. Für `B0569` ist das die einzige offene Größe.
- **SVG-Flächen in Razor.** `Burndownkurve.razor` zeichnet Linien; Flächen (`<polygon>`, gestapelte Zonen) und ihr Zusammenspiel mit Scoped CSS sind im Bestand nicht erprobt.

## Notizen

### Verworfene Alternativen

**Die zwei verworfenen Lesarten des Gegenstands** — beide standen im Artboard gleichberechtigt neben der gewählten:

| Lesart | Warum verworfen |
|---|---|
| **Die Kette ist ein Pfad durch die `Braucht`-Beziehungen** | **Doppelt verworfen.** *Sachlich*: der WBS-Import legt `Braucht` als **Fließtext** unter die Beschreibung (`D0008`, Zustand 4) — **aus Prosa entsteht kein Graph**. *Grundsätzlich und schwerer wiegend*: **einen längsten Pfad zu rechnen IST Netzplanung** und trifft das Nicht-Ziel „Kein Gantt, keine Ressourcenauslastung, keine Termin- und Kapazitätsplanung" (`R00000-vision.md:116-118`). **Deshalb wird auch kein eigener Slice für den Abhängigkeitsgraph vorgeschlagen** — er wäre nicht fehlende Arbeit, sondern eine **Änderung des Zielbilds**; wer ihn will, ändert zuerst die Vision. |
| **Die Kette ist ein Board-Termin** (`Starttermin` → `Zieltermin`) | `Starttermin` und `Zieltermin` stehen am Board (`KanbanC.Contracts/Boards/Board.cs`), sind aber **beide nullbar und an den meisten Boards leer**. Und „Restzeit bis zum Ziel gegen Restumfang" ist **kein Goldratt-Puffer, sondern eine Terminaussage** — dasselbe Nicht-Ziel. Dazu bekäme „Puffer" eine **zweite Bedeutung** neben der aus der Aufwandsspalte. Diese Lesart hätte als einzige heute schon Daten; sie ist deshalb nicht die richtige, sondern die billigste. |

**Die verworfenen Entwurfsoptionen:**

| Option | Warum verworfen |
|---|---|
| **Verbrauch nach oben deckeln** (auf `Bis − Von`) | Der Kettenanteil liefe nie über 100 % — die rote Zone der Fieberkurve verschwände. Ein Überzug ist eine ehrliche Zahl. |
| **Untererfüllung gegenrechnen** (`Ist − Von` auch negativ) | Der Verbrauch wäre vor allem eine Funktion des Nichtgetanen; der Überzug einer Karte verschwände hinter der Untätigkeit einer anderen. |
| **Fortschritt nach Kartenzahl** | Auf den Achsen stünden Karten gegen Stunden, und die Zonen bedeuteten nichts. Die Kartenzahl steht als zweite Kopfzahl daneben. |
| **Fortschritt gegen `Σ Bis`** | Zwei Sollbegriffe auf zwei Achsen; der Verbrauch misst gegen `Von`. |
| **Bei Kettenpuffer 0 durch die Obergrenze teilen** oder **den Anteil auf 0 % setzen** | Eine erfundene Zahl an der Stelle, an der die ehrliche Antwort „es gibt hier nichts zu verbrauchen" lautet — und bei **84 % Punktschätzungen** in der eigenen WBS wäre das der Regelfall, nicht der Rand. |
| **`null` und `0,0` einebnen** | „Diese Kette hat keinen Puffer" und „diese Karten tragen kein Soll" sind zwei verschiedene Auskünfte mit zwei verschiedenen Kompensationsaktionen. |
| **Eine Bahn über Kalendertage statt eines Punktes** | Rechenbar, aber nicht bestellt: das Fertig-Kriterium verlangt „ablesbar", nicht „im Verlauf". Bringt die Tagesaufteilung überschlagender Zeiteinträge mit. Als Ausbaustufe vorgemerkt. |
| **Ein Diagrammpaket (Chart.js, ApexCharts, Plotly)** | JS-Interop, meist eine Canvas-Fläche ohne prüfbaren DOM und die erste Fremdabhängigkeit in einer Oberfläche ohne CSS-Framework — dieselbe Entscheidung wie in `B0497`. |
| **Ein Zeitraumfilter für den Puffer** | Der Verbrauch ist ein Stand, kein Verlauf; ein `?seit=` schnitte eine Achse zu, die es hier nicht gibt. |
| **Eine zweite Adressform** (`/boards/{id}/auswertungen/puffer?kartenklasse=…`) | Zwei Adressformen für dieselbe Auswertungsfläche wären zwei Hausregeln; der Bestand steht in der Adresse. |
| **`LiesSollIst` um den Erledigungsstand erweitern** und beide Auswertungen aus einem Lesevorgang bedienen | Machte aus einem grünen Slice einen halben; `I0033` braucht das Feld nicht. |
| **Die Kette aus einer manuell gelegten Kartenreihenfolge** | Erfände eine Planungsgröße, die es im Board nicht gibt — dasselbe Nicht-Ziel wie beim Graphen. |
| **Neue Farbtokens `--color-zone-*`** | Die drei Zonen sind der einzige Ort, an dem sie gebraucht würden; die vorhandenen Skalen `--color-accent-2-*` und `--color-accent-*` tragen sie. Ein Token, der genau einmal vorkommt, ist ein Literal mit Namen. |
| **Eine eigene Seite `/auswertungen/puffer`** | Der Umschalter steht seit `B0484` genau dafür; eine zweite Adresse machte Board- und Kartenklassenwahl zu zwei Zuständen. |

### Bewusst out of scope

- Kritischer Pfad, Reihenfolge, Feeding-Buffer, Ressourcenkonkurrenz, „welche Karte treibt den Termin".
- Kalenderaussage, Bahn über Kalendertage, Prognose eines Fertigstellungsdatums.
- Abhängigkeitsgraph aus `Braucht`, Board-Termin als Pufferquelle.
- Verstellbare Zonengrenzen, Alarm- oder Benachrichtigungsfunktion.
- Vergleich mehrerer Bestände in einer Kurve, Export der Pufferzahlen als Datei.
- Änderungen an `I0033`, `I0034`, `I0036`, `I0037` über die vierte Auskunft hinaus.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Der Gegenstand** — Kette = Kartenbestand, Puffer = `Σ Bis − Σ Von`, Verbrauch = `max(0; Ist − Von)`, Fortschritt = Soll-Anteil der erledigten Karten. **Gefunden im Bestand, nicht erfunden** (90 Bänder, 145,8 h Puffer, 21 %).
2. **Die zwei anderen Lesarten sind verworfen**, beide mit Grund; ein Slice für den Abhängigkeitsgraph wird **nicht** vorgeschlagen.
3. **Verbrauch ohne Deckel und ohne Gegenrechnung.**
4. **Fortschritt Soll-gewichtet mit `Σ Von` als Nenner**; Kartenzahl als zweite Kopfzahl.
5. **Ein Punkt „heute", keine Bahn** — die Bahn ist Ausbaustufe.
6. **`null` ≠ `0,0`**: kein Band → `null`, kein Puffer → `0,0` mit Anteil `null`.
7. **Zonengeometrie**: `(0,0) → (100; 66,7)` und `(0; 33,3) → (100,100)`, als Konvention benannt, an einer Stelle konstant.
8. **Zonenfarben aus den vorhandenen Skalen**, kein neuer Token, kein Literal.
9. **SVG von Hand, kein Diagrammpaket**; `cx`/`cy` sind der Prüfpunkt.
10. **Kein Zeitraumfilter**, Adressform `…/kartenklassen/{id}/puffer` wie `soll-ist` und `burndown`.
11. **`NochNichtGebaut` bleibt als leere Liste stehen** — ein künftiger Eintrag wäre wieder ein Eintrag und kein Umbau.
12. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `D0009.dc.html`, **Zustand 4** zeichnet die **Frage**; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet**, und zwei seiner Aussagen sind vom Bestand überholt.
