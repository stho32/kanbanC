---
id: R00037
status: Neu
datum: 2026-09-07
---

# R00037: Burndown sehen

## Beschreibung

Für einen Kartenbestand — Board und Kartenklasse zusammen — ist der **Restumfang über die Zeit** als Verlauf zu sehen: je Kalendertag der Stand offener Karten, die an dem Tag erledigten Karten mit Nummer und Titel, dazu die Kopfzahlen offen · erledigt · im Bestand. Die Reihe kommt **gerechnet** über `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/burndown` aus der API und wird auf `/auswertungen` als Kurve, Kopfzahlen und Tagestabelle gezeigt; `?seit=YYYY-MM-DD` schneidet den Beginn der Achse.

Zahlt ein auf: [Vision](R00000-vision.md) — „Auswertungen aus vollständigen Daten. Soll-Ist-Vergleich gegen die WBS-Zählung, **Burndown**, Critical Chain mit Puffer-Verbrauch".

**Die eine Regel dieses Slice, wörtlich:**

> *Eine Karte gilt an einem Kalendertag als offen, wenn sie zum Kartenbestand gehört und ihr `ErledigtAm` an diesem Tag entweder **fehlt oder nach ihm liegt**.*

Am Tag ihres `ErledigtAm` ist sie **bereits erledigt** — das ist die Lesart „an dessen Ende noch leer" des Artboards. Die Regel steht an **genau einer Stelle** (`Burndownrechner`) und trägt Kurve, Tagestabelle und Kopfzahlen gleichermaßen.

**Damit sind alle Randfälle ohne Ausnahmeklausel entschieden** — das ist der eigentliche Gewinn dieser Formulierung:

| Lage | Folge, ohne Sonderregel |
|---|---|
| `ErledigtAm` **vor** dem ersten Tag der Achse | an **keinem** Tag der Achse offen; die Kurve wird nicht ein zweites Mal gesenkt |
| `ErledigtAm` **nach** dem letzten Tag (künftiges Datum) | an **jedem** Tag der Achse offen — und die Achse verlängert sich **nicht** |
| **kein** `ErledigtAm` | offen, auch wenn die Karte archiviert ist oder in einer Abschlussspalte steht |

**Die Datengrundlage ist vollständig da** — anders als bei `I0033` gibt es hier keine geerbte Lücke zu schließen: `ErledigtAm` entsteht beim Zug in die Abschlussspalte (`KartenRepository.SetzeErledigung`, Migration `008`) und wird vom Import mitgegeben (`WbsImportRepository.SchreibeErledigung`); der Kartenbestand ist Board × Kartenklasse, dasselbe Set wie in `I0022` und `I0033`. **Deshalb hat dieser Slice zwei Features und nicht drei.**

## Geschäftlicher Nutzen

Die Vision nennt den Burndown im **Anlass**, nicht erst in den Zielen: „Für eine eigene Implementation von Burndown-Chart und Critical Chain werden sehr spezielle Daten gebraucht, und zwar schnell; eine fremde Cloud-API mit Limits gibt sie nicht her." Genau diese Auswertung ist einer der Gründe, aus denen das Board überhaupt gebaut wird.

Der Wert liegt in der Zeitachse, die keine andere Auswertung hat. `I0033` sagt, **wie viel** Arbeit in einem Bestand steckt; erst der Burndown sagt, **wie er sich bewegt hat** — ob in den letzten Tagen etwas fertig geworden ist, ob die Kurve flach liegt, ob der Rest schrumpft. Das ist die Frage, die ein Mensch am Morgen und ein Agent vor der nächsten Interaction stellt, und heute beantwortet sie niemand: das Repository hat die Daten je Karte, stellt sie aber nirgends über die Zeit dar.

Und er kostet nichts an neuer Datenhaltung. Keine Migration, keine neue Spalte, kein zweiter Erzeuger — dieser Slice liest, was seit `I0011` und `I0030` ohnehin geschrieben wird.

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/burndown` liefert je Kalendertag **lückenlos** den Stand offener Karten am Tagesende und die an dem Tag erledigten Karten mit Nummer und Titel.
- Die Antwort trägt die Kopfzahlen **offen · erledigt · im Bestand** und die Zahl der Karten **ohne Erledigungsdatum in Abschlussspalte oder Archiv**.
- `?seit=YYYY-MM-DD` schneidet den **Beginn** der Achse; das Ende ist **immer heute**.
- Unbekanntes Board, unbekannte Kartenklasse und **unlesbares `seit`** werden mit Grund, Werten und Kompensationsaktion zurückgewiesen.
- Auf `/auswertungen` ist `Burndown` wählbar und zeigt Kurve, Kopfzahlen, Tagestabelle, den Zeitraum als Bedienelement und den API-Aufruf im Fuß.
- Die Kurve entsteht als **SVG-Polylinie aus der Tagesreihe**, ohne Diagrammpaket.
- Der Fuß der Fläche zeigt den Aufruf der **gewählten** Auswertung — `soll-ist` oder `burndown?seit=…`.

## Nicht-funktionale Anforderungen

- **Ein Lesevorgang je Bestand, nicht je Karte** — dieselbe Regel, unter der `LiesSollIst` seit `B0478` steht. 41 Karten kosten dieselbe Zahl Abfragen wie eine.
- **Die Antwort ist gerechnet, nicht roh.** Sie trägt weder Zeiteinträge noch Kartendetails; ein Agent bekommt die Reihe, nicht ihre Summanden. `I0037` bleibt der Ort der Rohdaten.
- **Prüfbar am DOM, nicht am Bild**: die Kurve ist eine `<polyline>` mit lesbarem `points`-Attribut, kein Canvas.
- **Keine Fremdabhängigkeit.** Die Oberfläche hat heute kein CSS-Framework und keine Diagrammbibliothek; das bleibt so.
- Gestaltungswerte ausschließlich aus `gestaltung.css`, **auch im SVG** — keine Farb-, Abstands- oder Radiusliterale.

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Der Restumfang über die Zeit ist als Verlauf dargestellt."*

### Die eine Regel und ihre Ränder

Das durchgehende Rechenbeispiel: Bestand aus **fünf** Karten, heute ist der **07.09.2026**.
`K1` erledigt am **03.09.**, `K2` und `K3` am **05.09.**, `K4` **ohne** `ErledigtAm` (archiviert), `K5` erledigt am **09.09.** (künftiges Datum).

- [ ] Ohne `seit` läuft die Achse vom frühesten `ErledigtAm` bis heute: **03., 04., 05., 06., 07.09.** — fünf Tage, lückenlos.
- [ ] Die offenen Karten am Tagesende ergeben die Reihe **4, 4, 2, 2, 2**: am 03. ist `K1` bereits erledigt, am 05. kommen `K2` und `K3` dazu; `K4` (ohne Datum) und `K5` (künftiges Datum) sind an **jedem** Tag offen.
- [ ] Die erledigten Karten je Tag: **03. → `K1`**, 04. → keine, **05. → `K2`, `K3`**, 06. → keine, 07. → keine. Tage ohne Abschluss stehen mit `0` in der Reihe und **fehlen nicht**.
- [ ] Das künftige `ErledigtAm` von `K5` **verlängert die Achse nicht** — sie endet am 07.09.
- [ ] Die Kopfzahlen: **offen 2 · erledigt 3 · im Bestand 5**. `offen` ist der Wert der Kurve am letzten Tag, `erledigt` ist `im Bestand − offen`.
- [ ] `?seit=2026-09-05` liefert die Achse **05., 06., 07.** mit der Reihe **2, 2, 2** — die **Kopfzahlen bleiben 2 · 3 · 5**, weil sie dem ganzen Bestand gelten und nicht dem Ausschnitt.
- [ ] `?seit=2026-09-01` (vor dem frühesten Abschluss) liefert sieben Tage mit führendem flachem Stück **5, 5, 4, 4, 2, 2, 2**.
- [ ] `?seit=2026-09-20` (nach heute) liefert **genau den einen Tag heute**, nicht die leere Reihe.
- [ ] Die Zahl der **Karten ohne Erledigungsdatum in Abschlussspalte oder Archiv** ist **1** (`K4`).

### Die Burndown-Reihe über die API (`F0063`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/burndown` liefert für den Kartenbestand je Kalendertag lückenlos den Stand offener Karten am Tagesende und die an dem Tag erledigten Karten mit Nummer und Titel, dazu die Kopfzahlen offen · erledigt · im Bestand und die Zahl der Karten ohne Erledigungsdatum in Abschlussspalte oder Archiv; `?seit=YYYY-MM-DD` schneidet den Beginn; unbekanntes Board, unbekannte Kartenklasse und unlesbares `seit` werden mit Grund, Werten und Kompensationsaktion zurückgewiesen. Ohne Schirm allein an der 200-Antwort prüfbar."*

- [ ] Der Aufruf antwortet **200** mit einer Tagesliste in Kalenderfolge und den vier Kopfzahlen.
- [ ] Der **Bestand** ist Board × Kartenklasse — dasselbe Set wie `GET .../kartenklassen/{kartenklasseId}/karten` (`I0022`), **archivierte Karten eingeschlossen**.
- [ ] Das **Ende der Achse ist immer heute**, auch wenn seit Tagen nichts erledigt wurde; es gibt **keinen `bis`-Parameter**.
- [ ] Der **Beginn** ist das angefragte `seit`, sonst das früheste `ErledigtAm` des Bestands, sonst heute.
- [ ] Ein Bestand **ohne Karten** antwortet **200** mit dem einen Tag heute, der Reihe `0` und den Kopfzahlen `0 · 0 · 0` — **nicht 404**.
- [ ] Ein Bestand **ohne jedes Erledigungsdatum** antwortet **200** mit dem einen Tag heute; alle Karten stehen offen.
- [ ] Unbekanntes Board → **404**, Code `board-unbekannt`, Meldung mit der Board-Nummer, Kompensation `GET /api/boards`.
- [ ] Unbekannte Kartenklasse → **404**, Code `kartenklasse-unbekannt`; eine Kartenklasse eines **fremden** Boards ist der eigene Fall `kartenklasse-fremd`.
- [ ] Unlesbares `seit` (z. B. `?seit=gestern`) → **400** mit **eigenem Befund**: die Meldung nennt den **gelesenen Wert** und die erwartete Form `YYYY-MM-DD`, die Kompensation nennt die Route ohne Parameter.
- [ ] Ein **fehlendes** `seit` ist kein Fehler — es ist die Standardachse.
- [ ] Der Fehlervertragstest aus `B0102` nimmt die neue Route auf.
- [ ] **Ohne Schirm prüfbar**: alle Kriterien dieser Gruppe sind an der Antwort allein zu zeigen.

### Der Schirm zeichnet die Kurve (`F0064`)

Fertig-Kriterium wörtlich: *„Auf `/auswertungen` ist `Burndown` wählbar und zeigt für den gewählten Bestand die Kurve offener Karten über Kalendertage, die Kopfzahlen offen · erledigt · im Bestand darüber, die Tagestabelle Tag · erledigt · offen · Karten darunter, den Zeitraum als Bedienelement und den API-Aufruf im Fuß; die vier Ränder tragen: Bestand ohne Karten, Bestand ohne jedes Erledigungsdatum, alle Karten erledigt, WebApi nicht erreichbar."*

- [ ] Der Punkt `Burndown` im Umschalter ist **wählbar** — kein gesperrter `span` mehr; die übrigen drei bleiben gesperrt.
- [ ] Board- und Kartenklassenwahl bleiben **gemeinsam** für beide Auswertungen: derselbe Bestand, ein Wechsel der Auswertung wirft die Wahl nicht weg.
- [ ] Die Kurve ist eine **SVG-`<polyline>`**, deren `points`-Attribut so viele Paare trägt, wie die Achse Tage hat, und deren Werte der Tagesreihe folgen. Am letzten Punkt steht der Wert lesbar.
- [ ] Die **Kopfzahlen** stehen über der Kurve, die **Tagestabelle** darunter mit Tag · erledigt · offen · Karten; die Kartennummern des Tages stehen in der Zeile.
- [ ] Die Tabelle zeigt **nur Tage mit mindestens einem Abschluss**, die Kurve **alle** — und beide entstehen aus **derselben** Reihe, ohne zweite Rechnung.
- [ ] Der **Zeitraum** ist ein Bedienelement neben Board und Kartenklasse; seine Vorgabe zeigt den frühesten Erledigungstag, und eine Änderung lässt Kurve, Kopfzahlen und Tabelle folgen.
- [ ] Der **Fuß** zeigt den Aufruf der gewählten Auswertung: `GET …/soll-ist` bzw. `GET …/burndown?seit=…` — nicht mehr fest den einen.
- [ ] Rand 1 — **Bestand ohne Karten**: lesbare Leermeldung statt leerer Fläche.
- [ ] Rand 2 — **Bestand ohne jedes Erledigungsdatum**: Meldung **mit Kompensationsaktion** („eine Karte in die Abschlussspalte ziehen oder einen früheren Beginn wählen") statt einer Kurve aus einem Punkt.
- [ ] Rand 3 — **alle Karten erledigt**: die Kurve endet auf **0**, und das ist kein Sonderfall, sondern das Ergebnis.
- [ ] Rand 4 — **WebApi nicht erreichbar**: lesbare Meldung über `WebApiAufruf.MitAusfallmeldung` statt Ausnahmeseite; der Umschalter bleibt stehen.
- [ ] Die **Fußzeile nennt die Zahl der Karten ohne Erledigungsdatum** in Abschlussspalte oder Archiv — gezeigt statt versteckt, wie `I0033` es mit den Karten ohne Soll hält.
- [ ] **Kein Gestaltungsliteral** in `Burndownkurve.razor` und in der Stilvorlage der Fläche: Farben und Maße kommen aus `gestaltung.css` — geprüft wie in `AuswertungsflaecheTests`.

### Der grüne Bestand bleibt grün

- [ ] `I0033` wird **nicht** umgebaut: `LiesSollIst`, `AuswertungsService.SollIst`, `SollIstTabelle.razor` und die Route `soll-ist` bleiben, wie sie sind. Sie **wachsen** nur dort, wo eine zweite Auskunft dazukommt.
- [ ] `Auswertungen.razor` führt den Soll-Ist-Vergleich nach diesem Slice **unverändert**; die Suite von `R00036` bleibt grün.
- [ ] **Keine Migration, keine Schemaänderung.** `Karteerledigung` und `Kartenarchivierung` werden nur gelesen.
- [ ] `I0037` bleibt unberührt.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Sollstrich, keine Prognoselinie, keine Stunden auf der Y-Achse** — alle drei setzten eine geplante Dauer voraus, die die Vision ausschließt („Burndown und Critical Chain rechnen aus den Ist-Daten; sie planen nicht").
- [ ] **Kein `bis`-Parameter**, kein `ArchiviertAm`, kein Verlauf je Karte.
- [ ] **Kein Diagrammpaket**, kein JS-Interop, kein Canvas.
- [ ] Kein Puffer-Verbrauch (`I0035`), kein Zeitexport (`I0036`), keine Rohdaten (`I0037`).

## Betroffene Verzeichnisstruktur

Alle Themenordner stehen bereits — dieser Slice legt **keinen** neuen an.

- **Contracts**: `KanbanC.Contracts/Auswertungen` — `Burndownauswertung`, `Burndowntag`, `Burndownkarte`, `Burndownkopfzahlen`.
- **BL**: `KanbanC.BL/Models/Auswertungen` (Lesegestalt und Kalenderachse), `Operations/Auswertungen` (Zeitraum, Rechner, Filter), `Integrations/Auswertungen` (`AuswertungsService`), `Interfaces/Auswertungen` (`IAuswertungsrepository`), `Persistenz/Auswertungen` (`Auswertungsrepository`).
- **API**: `KanbanC.WebApi/Endpunkte/AuswertungsEndpunkte.cs` — zweite Route, keine neue Datei.
- **Oberfläche**: `KanbanC.Blazor/Components/Pages/Auswertungen.razor` (+ `.razor.css`), `Components/Auswertungen/` für Fläche, Kurve und Tabelle, `Services/AuswertungenApiKlient.cs` und die reinen Formoperationen daneben. **Keine Projektreferenz auf `KanbanC.BL`** — der Weg führt über HTTP.
- **Tests**: `KanbanC.BL.Tests/{Operations,Models}/Auswertungen`, `KanbanC.WebApi.IntegrationTests/Api`, `KanbanC.Blazor.Tests/{Services,Gestaltung}`, `KanbanC.PlaywrightTests` (Seitenobjekt `AuswertungenSeite` wächst).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Die Regel wohnt an einer Stelle

`Burndownrechner` ist die **einzige** Stelle, an der „offen an einem Tag" entschieden wird. Kurve, Tagestabelle und Kopfzahlen lesen alle aus **derselben** Tagesreihe; keine der drei rechnet nach. Das ist nicht Sparsamkeit, sondern die Bedingung dafür, dass die drei nie auseinanderlaufen — dieselbe Hausregel, unter der die Summenzeile von `I0033` steht.

Aus der Regel folgen die Ränder ohne eine einzige Ausnahmeklausel. Wer sie ändern will, ändert eine Methode und sieht sofort alle drei Darstellungen mitgehen; wer sie an drei Stellen schriebe, bekäme drei Wahrheiten über denselben Tag.

### Die Achse endet heute, und „heute" ist die Uhr der WebApi

`DateOnly.FromDateTime(DateTime.Today)` — **dieselbe** Uhr, aus der `KartenRepository.Heute()` und `WbsImportRepository.Heute()` das `ErledigtAm` schreiben. Eine andere Uhr für das Lesen als für das Schreiben erzeugte Tage, an denen eine Karte erledigt und zugleich offen wäre.

**Keine Uhr-Abstraktion** — die Hausregel steht im Bestand (`KartenRepository`, Kommentar bei `Jetzt()`): geprüft wird über ein Zeitfenster, nicht über eine eingespritzte Uhr. Der `Burndownrechner` und `Burndownzeitraum` bleiben trotzdem **pur**, weil sie „heute" als **Eingang** bekommen; nur die Integration liest die Uhr. Damit sind alle Rechenbeispiele oben als Unit Tests schreibbar, ohne dass eine Schnittstelle für Zeit entsteht.

Kein `bis`: ein Ende in der Vergangenheit wäre eine zweite Bedienfrage ohne Nutzen, und die Kurve muss bis heute laufen, gerade wenn seit Tagen nichts erledigt wurde — die flache Strecke am rechten Rand ist die Aussage.

### `seit` kommt als Text herein — belegt, nicht vermutet

Der Abfrageparameter wird als `string?` gebunden und an der Grenze geprüft, **nicht** als `DateOnly?`. Der Grund liegt im Repository als Probe vor: `AbfrageparameterProbeTests` zeigt, dass ASP.NET einen unlesbaren Wert **vor** dem Handler abbindet und mit einer 400 **ohne unseren Befund** antwortet. Genau daran hängt das Kriterium „unlesbares `seit` wird mit Grund, Werten und Kompensationsaktion zurückgewiesen": mit `DateOnly?` wäre es nicht erfüllbar.

Das Muster ist gebaut und heißt `Archivfilter.Aus(abfragewert, route)` → `Ergebnis<Archivierung>`; der neue `Zeitraumfilter` ist dessen Zwilling und bekommt die Route ebenso als Eingang, damit die Kompensation die Adresse nennt, die der Aufrufer wirklich gerufen hat. Geprüft wird **vor** dem Dienst — dieselbe Reihenfolge wie bei `archiviert` in `BoardEndpunkte`.

**Eine `dependency-probe` entfällt damit**: die Frage ist im Repository beantwortet, `DateOnlyEingabeProbeTests` belegt zusätzlich das Lesen und Schreiben von `DateOnly` über die Grenze.

### Gezeichnet wird SVG von Hand

Das Artboard zeichnet Gitter, Achse, Polylinie, Punkte und Beschriftungen in rund **40 Zeilen SVG**, die eine Razor-Komponente aus der Tagesreihe erzeugt. Ein Diagrammpaket brächte drei Kosten, die keinen Gegenwert haben: JS-Interop in einer Anwendung, die heute ohne auskommt; in der Regel eine **Canvas-Fläche ohne prüfbaren DOM**; und die **erste Fremdabhängigkeit** in einer Oberfläche ohne CSS-Framework und ohne Diagrammbibliothek.

**Die Polylinie ist zugleich der prüfbarere Weg.** Der E2E-Test liest ihr `points`-Attribut und vergleicht Zahlen — nicht ein Bild, nicht einen Screenshot. Die Skalierung (Höchstwert auf Höhe, Tageszahl auf Breite) ist pure Arithmetik und gehört in eine eigene Operation, damit sie im Unit-Test steht statt im Markup.

Farben und Maße kommen aus `gestaltung.css` (`--color-accent` für die Kurve, `--color-divider` für das Gitter, `--color-surface` für die Fläche); die Gestaltungsprüfung liest die Datei und weist Literale zurück, wie `AuswertungsflaecheTests` es für `I0033` tut.

### Der Leseweg: ein Vorgang, das Vorbild steht daneben

`LiesErledigungsstaende` ist der Zwilling von `LiesSollIst` (`B0478`): derselbe Schnitt über `Karte → Spalte → Kartenklassenzuordnung → Kartenklasse`, gebunden an `s.Board` und `z.Kartenklasse`, dazu `LEFT JOIN Karteerledigung` und `LEFT JOIN Kartenarchivierung`. Neu ist allein `Spalte.IstAbschlussspalte`, weil die Fußzeile die Karten ohne Datum **in Abschlussspalte oder Archiv** zählen muss.

Das Datum steht als ISO-Text in der Spalte und wird in C# nach `DateOnly` gelesen — wie in `KartenRepository.LiesErledigung`. **Keine Aggregate in SQL**: Microsoft.Data.Sqlite meldet für eine Aggregatspalte ohne Tabellentyp `Byte[]`, und Dapper findet dann keinen Konstruktor (im Bestand belegt und kommentiert). Gezählt wird ohnehin in C#, weil dort die eine Regel steht.

### Die Adressform folgt dem Bestand, nicht dem Artboard

Der Fuß in Zustand 2 des Artboards nennt `GET /api/boards/2/auswertungen/burndown?kartenklasse=1&seit=…`. **Gebaut wird eine andere Form** — und das ist eine bewusste Abweichung, keine Unachtsamkeit:

```
GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/burndown?seit=YYYY-MM-DD
```

Dieselbe Adressform wie `soll-ist` (`AuswertungsEndpunkte`) und wie die Kartenroute aus `I0022`. **Der Bestand steht in der Adresse, weil er den Bestand benennt; der Zeitraum steht in der Abfrage, weil er ihn zuschneidet.** Zwei Adressformen für dieselbe Auswertungsfläche wären zwei Hausregeln — und die Kartenklasse als Abfrageparameter wäre die einzige Stelle im Bestand, an der ein Pflichtbezug optional aussähe.

### Archiv und Karten ohne Datum bleiben im Bestand

Gelesen wird derselbe Bestand wie in `I0033`, archivierte Karten eingeschlossen: der Umfang **war** da. Und `Kartenarchivierung` trägt **bewusst kein Datum** — Migration `009` sagt es wörtlich: „Kein ArchiviertAm: das Artboard zeichnet kein Datum, und ein erfundenes verdürbe die Auswertung."

**Die Folge gehört benannt**: eine archivierte oder in einer Abschlussspalte stehende Karte **ohne** `ErledigtAm` verlässt die Kurve nie. Sie stammt aus der Zeit vor Migration `008` oder vor `I0030`. Ein Datum dafür zu erfinden verdürbe genau die Auswertung, um die es hier geht; sie zu verschweigen ließe die Kurve falsch aussehen, ohne zu sagen warum. Deshalb steht sie in der Kurve **und** ihre Zahl in der Fußzeile — dieselbe Handhabung, mit der `I0033` die Karten ohne Soll zeigt.

### Ablauf

1. **Lesen** — `Auswertungsrepository.LiesErledigungsstaende(boardId, kartenklasseId)` → `Erledigungsstandkarten` (ein Vorgang).
2. **Achse bestimmen**
   - 2.1 `Zeitraumfilter.Aus(seitText, route)` → `Ergebnis<Zeitraumwahl>`; unlesbar → 400 an der Grenze.
   - 2.2 `Burndownzeitraum.Bestimme(bestand, seit, heute)` → `Kalenderachse` (lückenlose Tagesliste).
3. **Rechnen** — `Burndownrechner.Rechne(bestand, achse)` → `IReadOnlyList<Burndowntag>`; **hier steht die Regel**.
4. **Zusammensetzen** — `AuswertungsService.Burndown(boardId, kartenklasseId, seit)` → `Ergebnis<Burndownauswertung>` oder `Nichtgefunden.Board` / `Nichtgefunden.Kartenklasse` / `Nichtgefunden.FremdeKartenklasse`; **dieselbe Vorprüfung wie `SollIst`**.
5. **Ausliefern** — `AuswertungsEndpunkte` → 200 / 400 / 404.
6. **Zeigen** — `AuswertungenApiKlient.LadeBurndown`, `Auswertungen.razor` mit Umschalter, `Burndownflaeche.razor`, `Burndownkurve.razor`.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** die zweite Route in `AuswertungsEndpunkte`; der Umschalter in `Auswertungen.razor`, dessen Eintrag `burndown` heute in der Liste `NochNichtGebaut` steht; die zweite Methode an `IAuswertungsrepository`, `AuswertungsService` und `AuswertungenApiKlient`.

**In `KanbanC.Contracts/Auswertungen`** (immutable, C08):
- `Burndownkarte` (DTO) — eine an einem Tag erledigte Karte.
  - `long KarteId`, `string? Kartennummer`, `string Titel`
- `Burndowntag` (DTO) — ein Kalendertag der Reihe.
  - `DateOnly Tag`, `IReadOnlyList<Burndownkarte> ErledigteKarten`, `int OffeneKarten`
- `Burndownkopfzahlen` (DTO) — die Zahlen über der Kurve, **gerechnet geliefert**; die Oberfläche summiert nichts nach.
  - `int Offen`, `int Erledigt`, `int ImBestand`, `int OhneErledigungsdatum`
- `Burndownauswertung` (DTO) — `IReadOnlyList<Burndowntag> Tage`, `Burndownkopfzahlen Kopfzahlen`

**In `KanbanC.BL/Models/Auswertungen`:**
- `Erledigungsstandkarte` (DTO, immutable) — eine gelesene Karte des Bestands mit Nummer, Titel, `DateOnly? ErledigtAm`, Archivstand und der Auskunft, ob sie in einer Abschlussspalte steht.
- `Erledigungsstandkarten` (benannte Collection) — beantwortet „wie viele im Bestand", „welches ist das früheste `ErledigtAm`" und „wie viele ohne Datum in Abschlussspalte oder Archiv".
- `Kalenderachse` (DTO, immutable) — die lückenlose Tagesliste von erstem bis letztem Tag.
- `Zeitraumwahl` (DTO, immutable) — `DateOnly? Seit`; die gelesene Fassung des Abfrageparameters.

**In `KanbanC.BL/Operations/Auswertungen`:**
- `Zeitraumfilter` (Operation, pur) — liest `?seit=` als Datum oder weist es zurück. Zwilling von `Archivfilter`.
  - `static Ergebnis<Zeitraumwahl> Aus(string? abfragewert, string route)`
- `Burndownzeitraum` (Operation, pur) — erster und letzter Kalendertag, lückenlos dazwischen.
  - `static Kalenderachse Bestimme(Erledigungsstandkarten bestand, DateOnly? seit, DateOnly heute)`
- `Burndownrechner` (Operation, pur) — **die eine Stelle, an der die Regel steht.**
  - `static IReadOnlyList<Burndowntag> Rechne(Erledigungsstandkarten bestand, Kalenderachse achse)`

**In `KanbanC.BL/Interfaces/Auswertungen` und `KanbanC.BL/Persistenz/Auswertungen`:**
- `IAuswertungsrepository` — wächst um eine zweite Auskunft (zwei Implementationen: echte und `TestAuswertungsrepository`).
  - `Erledigungsstandkarten LiesErledigungsstaende(long boardId, long kartenklasseId)`
- `Auswertungsrepository` (Provider/Ressourcenzugriff) — ein Lesevorgang je Bestand, Rohzeilen über Dapper.

**In `KanbanC.BL/Integrations/Auswertungen`:**
- `AuswertungsService` (Integration) — wächst um eine zweite Auskunft; die Vorprüfung Board → Kartenklasse ist dieselbe und wird **nicht** ein zweites Mal geschrieben.
  - `Ergebnis<Burndownauswertung> Burndown(long boardId, long kartenklasseId, DateOnly? seit)`

**In `KanbanC.WebApi/Endpunkte`:**
- `AuswertungsEndpunkte` — zweite Route `…/burndown`, 200 / 400 / 404.

**In `KanbanC.Blazor`:**
- `AuswertungenApiKlient` — `Task<ApiErgebnis<Burndownauswertung>> LadeBurndown(long boardId, long kartenklasseId, DateOnly? seit)`
- `Kurvenpunkte` (Operation, pur) — rechnet aus Tagesreihe und Zeichenfläche das `points`-Attribut und die Achsenbeschriftungen. **Die Arithmetik des Bildes gehört in einen Unit Test, nicht ins Markup.**
- `Burndownflaeche.razor` (Komponente) — Kopfzahlen, Kurve, Tagestabelle, Fußzeile.
- `Burndownkurve.razor` (Komponente) — das SVG: Gitter, Achsen, Polylinie, Punkte, Wert am letzten Punkt.
- `Auswertungen.razor` — Umschalter zwischen Soll-Ist und Burndown, Zeitraum in der Filterzeile, Aufruf im Fuß folgt der Wahl.

**Kein Interface** für die reinen Operationen: je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `IAuswertungsrepository` | zweite Auskunft `LiesErledigungsstaende` |
| `Auswertungsrepository` | zweiter Lesevorgang, Muster von `LiesSollIst` |
| `TestAuswertungsrepository` | zieht mit |
| `AuswertungsService` | zweite Auskunft `Burndown`; die Vorprüfung Board → Kartenklasse wird **geteilt**, nicht kopiert |
| `AuswertungsEndpunkte` | zweite Route, Filterprüfung an der Grenze |
| `AuswertungenApiKlient` | `LadeBurndown` |
| `Auswertungen.razor` | Umschalter statt fester Auswertung; `burndown` verlässt die Liste `NochNichtGebaut`; Zeitraumelement; Fuß folgt der Wahl |
| `Auswertungen.razor.css` | Fläche für Kurve und Tagestabelle |
| `AuswertungenSeite` (E2E) | Locator für Kurve, Kopfzahlen, Tagestabelle, Zeitraum |
| `Program.cs` (WebApi) | nichts Neues zu registrieren — Repository, Dienst und Endpunkte stehen |

**Nicht geändert:** `SollIstTabelle.razor`, `Abweichungsrechner`, `SollIstKarte`, `SollIstKarten`, alle `SollIst…`-Verträge, `Kopfzeile.razor` (der Punkt „Auswertungen" ist seit `R00036` ein `NavLink`), `KartenRepository`, `WbsImportRepository`, alles unter `Persistenz/Migrationen/`.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Burndownrechner` — **das Rechenbeispiel oben in voller Länge**: fünf Karten, fünf Tage, Reihe `4, 4, 2, 2, 2`; dazu je ein Test für „vor dem ersten Tag erledigt", „künftiges Datum", „ohne Datum", „Tag ohne Abschluss steht mit 0".
- `Burndownzeitraum` — Beginn aus `seit`, aus dem frühesten `ErledigtAm`, aus heute; `seit` vor dem frühesten Abschluss; `seit` nach heute → genau ein Tag; leerer Bestand → genau ein Tag.
- `Zeitraumfilter` — fehlender Parameter → kein Schnitt; `2026-09-05` → Datum; `gestern`, `05.09.2026`, `2026-13-01` → Zurückweisung mit dem gelesenen Wert im Text; **kein Wurf**.
- `Erledigungsstandkarten` — früheste Erledigung, Zahl im Bestand, Zahl ohne Datum in Abschlussspalte oder Archiv (und **nicht** die ohne Datum in einer normalen Bahn).
- `Kurvenpunkte` (in `KanbanC.Blazor.Tests`) — ein Tag, zwei Tage, alle Werte gleich (keine Division durch Null), Höchstwert 0.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `LiesErledigungsstaende`: Bestand mit erledigten, offenen, archivierten Karten und Karten in einer Abschlussspalte ohne Datum — **ein** Lesevorgang, alle Felder gefüllt.
- `GET .../burndown`: 200 mit Tagen und Kopfzahlen; mit `?seit=`; **400** bei unlesbarem `seit` mit unserem Befund im Rumpf (`befunde` vorhanden — das ist der Unterschied zur ASP.NET-Antwort); 404 für unbekanntes Board, unbekannte und fremde Kartenklasse.
- Bestand ohne Karten → 200 mit einem Tag, nicht 404.
- Der Fehlervertragstest aus `B0102` nimmt die Route auf.
- **Eine Karte aus der Abschlussspalte herausziehen** und erneut abrufen: sie ist danach an allen Tagen offen — die Momentaufnahme ist damit **belegt statt behauptet**.

**`KanbanC.Blazor.Tests`:** `AuswertungenApiKlient.LadeBurndown` — 200 wird gelesen, 400 und 404 werden zur lesbaren Zurückweisung, `HttpRequestException` zur Ausfallmeldung. **Diese Pfade sind über den Browser nicht auslösbar** — genau der Grund, aus dem dieses Testprojekt existiert. Dazu die Gestaltungsprüfung: `Burndownkurve.razor` und die Stilvorlage der Fläche ohne Farb-, Abstands- und Radiusliteral.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — eine kleine WBS einfahren (wie `B0431`/`B0454`, nicht `kanbanc.md`), Karten in die Abschlussspalte ziehen, `/auswertungen` öffnen, `Burndown` wählen, **Kopfzahlen, `points`-Attribut der Polylinie und Tageszeilen lesen**, den Zeitraum einschränken und die kürzere Reihe sehen. Dazu der Fuß, der nach dem Umschalten `burndown` zeigt.

## Abhängigkeiten

- Abhängig von: **`R00033`** (WBS-Datei importieren — `I0030`, **grün**), das `Braucht` der Interaction; und **`R00036`** (Soll-Ist-Vergleich abrufen — `I0033`, **grün**), das `Braucht` von `F0063`: `Auswertungsrepository`, `AuswertungsService`, `AuswertungsEndpunkte`, `AuswertungenApiKlient`, der Schirm `/auswertungen` und sein Umschalter stammen von dort.
- Setzt außerdem auf (alle grün): **`R00007`**/**`R00015`** (`ErledigtAm` beim Zug in die Abschlussspalte, Migration `008`), **`R00016`** (Archivstand, Migration `009`), **`R00022`**/**`R00023`**/**`R00025`** (Kartenklassen, Kartennummern, der Mengenbegriff „Karten einer Klasse"), **`R00005`** (Kopfzeile und Gestaltungstokens).
- Blockiert: **`I0035`** (Puffer-Verbrauch) — es führt `Braucht: I0034` in der WBS.
- **`D0009` wird mit diesem Slice nicht grün** — `I0035`, `I0036` und `I0037` bleiben rot.

## Umfang

```
Burndown sehen (I0034) = 14 Bubbles: 13 Standard (21,2h), 1 unklar (2,0-4,0h).
Rest: 21,2h klar + 2,0-4,0h unklar · 0 von 14 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 14 Bubbles gruen (0 %) · 0 laufen · 14 offen
```

`I0034` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **zwei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0063` Die Burndown-Reihe über die API | `B0489`–`B0495` (7) | 7 (10,8h) | 0 | `I0033` |
| `F0064` Der Schirm zeichnet die Kurve | `B0496`–`B0502` (7) | 6 (10,4h) | 1 (2,0–4,0h) | `F0063` |

**Warum zwei Features:** weil zwei Aspekte **getrennt fertig** werden. `F0063` ist allein an der 200-Antwort prüfbar und könnte vollständig sein, während der Schirm noch nichts zeigt; `F0064` zeigt, was `F0063` liefert. **Ein drittes Feature für die Datengrundlage entfällt** — anders als bei `I0033` ist sie vollständig da.

Die eine unklare Bubble ist `B0502` (E2E über beide Prozesse). Alles davor ist pure Logik oder eine zweite Auskunft an einer Klasse, die es schon gibt.

**Nach gemessenem Durchsatz ist mit etwa 0,5–1,0 h zu rechnen.** Die Richtwert-Konvention seit `I0004` überschätzt messbar, und das ist dreifach belegt (`Schaetzungen/_ist-zeiten.md`; `I0031` bis `I0033` sind dort noch nicht eingetragen):

| Slice | gezählt | gemessen | Faktor |
|---|---|---|---|
| `I0028` | 22,4–37,9h | 1,9h | ~12–20 |
| `I0029` | 15,6–24,0h | 1,0h | ~16–24 |
| `I0030` | 32,0–44,0h | 2,4h | ~13–18 |

Die Zählung wird trotzdem nicht still gekippt — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0034` und an beiden Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00036`/`I0033`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Die Kurve ist eine Momentaufnahme, kein Ereignisprotokoll — und das ist eine echte Grenze.** Die Reihe wird bei **jedem** Abruf aus dem **heutigen** `ErledigtAm` je Karte zurückgerechnet. Wer eine Karte aus der Abschlussspalte herauszieht, verliert ihr Datum (`KartenRepository.LoescheErledigung`); sie ist danach **rückwirkend an allen Tagen offen**, und die Kurve von gestern sieht heute anders aus. Das ist die ehrliche Folge des Bestands: **wer wann welche Karte über welche Grenze bewegt hat, hält nichts fest** — die Ereignisspur wurde in `I0028` ausdrücklich verworfen, und ein Verlauf je Karte hat **keinen WBS-Knoten**. Der Slice baut deshalb keinen; er benennt die Grenze. **Befund für `/planung`, nicht am Menschen geprüft.**
- **Kein `ArchiviertAm` im Bestand.** `Kartenarchivierung` trägt bewusst kein Datum (Migration `009`), ein Archivzeitpunkt ist nicht rekonstruierbar. Eine archivierte Karte ohne `ErledigtAm` verlässt die Kurve daher nie. Ein Datum nachzurüsten wäre eine eigene Interaction unter `D0004` und **kein Teil dieses Slice**. **Nicht am Menschen geprüft.**
- **Kein `bis`-Parameter.** — **entschieden: das Ende ist immer heute.** Ein Ende in der Vergangenheit wäre eine zweite Bedienfrage ohne Nutzen, und die Kurve muss bis heute laufen. **Nicht am Menschen geprüft.**
- **Was heißt die Kopfzahl „offen" bei einem künftigen `ErledigtAm`?** — **entschieden: sie ist der Wert der Kurve am letzten Tag**, also inklusive der Karte mit künftigem Datum. Jede andere Lesart erzeugte eine Kopfzahl, die nicht zum rechten Rand der Kurve passt. **Nicht am Menschen geprüft.**
- **Gilt der Zeitraum auch für die Kopfzahlen?** — **entschieden: nein.** Sie gelten dem ganzen Bestand, nicht dem Ausschnitt; sonst hieße dieselbe Zahl an zwei Stellen Verschiedenes. **Nicht am Menschen geprüft.**
- **`I0035` (Puffer-Verbrauch) führt `Braucht: I0034`, aber nicht `I0033`.** Ein Puffer ist die Differenz zweier Dauern und braucht das Soll; `R00036` hat denselben Befund bereits festgehalten, und er steht weiterhin. **Befund für `/planung aendern I0035`, hier nicht geändert** — diese Familie ändert keine Knoten.
- **Was zeigt die Kurve bei sehr langen Zeiträumen?** — **angenommen: alle Tage, ohne Verdichtung.** Ein Bestand über ein Jahr ergäbe 365 Punkte in einer Polylinie; das ist lesbar gezeichnet, aber die Tagesbeschriftung wird dann ausgedünnt werden müssen. **Angenommen, nicht belegt** — die Ausdünnungsregel entscheidet der Entwickler beim Bauen, sie ist Gestaltung und kein Kriterium.

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit und liest nur, was bereits geschrieben wird.

## Manuelle Nachbereitungstätigkeiten

- Keine. Karten aus der Zeit vor Migration `008` tragen kein `ErledigtAm` und bleiben in der Kurve offen; ihre Zahl steht in der Fußzeile. **Ein nachträgliches Datum wird nicht gesetzt** — es wäre erfunden.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt steht im Anlass der Vision: für einen eigenen Burndown werden sehr spezielle Daten gebraucht, und eine fremde Cloud-API mit Limits gibt sie nicht her. Das Board hat diese Daten inzwischen vollständig — `ErledigtAm` je Karte seit `I0011`, der Kartenbestand als Board × Kartenklasse seit `I0022`, der Leseweg dorthin seit `I0033` —, es stellt sie nur nirgends über die Zeit dar. Wenn eine einzige Rechenstelle aus diesen Daten je Kalendertag den Stand offener Karten bildet (X), dann liefert dieselbe Reihe Kurve, Tagestabelle und Kopfzahlen an einen Menschen am Schirm **und** an einen Agenten an einer Route (Y), sodass die Frage „bewegt sich der Rest" ohne Handarbeit und ohne fremden Dienst beantwortet ist (Z). Der Hebel sitzt genau hier und nicht vorgelagert: an der Datenhaltung ist nichts zu tun, eine neue Spalte oder eine Ereignisspur wäre Aufwand ohne Gegenwert für diese Frage. Und er sitzt nicht nachgelagert bei `I0037`: dort kämen Rohdaten heraus, aus denen jeder Leser die Reihe selbst rechnen müsste — was der Kernregel des Projekts widerspricht, dass ein Agent von der API bekommt, was ein Mensch am Schirm sieht.

## Missing-Docs

- **SVG in Razor.** Wie sich Blazor-Bindungen in einem `<svg>`-Teilbaum verhalten (Attribute mit Bindestrich, `viewBox`-Schreibweise, `xmlns` bei Komponentenausgabe) ist im Repository nirgends notiert; der Bestand zeichnet bisher kein SVG aus Daten. Für `B0497` ist das die einzige offene Größe.
- **Kalenderarithmetik über lange Zeiträume.** `DateOnly.AddDays` über Zeitumstellungen hinweg ist unkritisch, aber es fehlt eine Notiz, dass der Burndown bewusst **ohne** Zeitzone rechnet: `ErledigtAm` ist ein Tag der Ortszeit der WebApi, kein Zeitpunkt.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Ein Diagrammpaket (Chart.js, ApexCharts, Plotly)** | JS-Interop, meist eine Canvas-Fläche ohne prüfbaren DOM und die erste Fremdabhängigkeit in einer Oberfläche ohne CSS-Framework. Rund 40 Zeilen SVG kosten weniger und sind besser prüfbar. |
| **Ein Bild serverseitig rendern** | Ein Bild ist im E2E-Test nicht lesbar; die Polylinie trägt ihre Zahlen im `points`-Attribut. |
| **Die Adressform des Artboards** (`/boards/2/auswertungen/burndown?kartenklasse=1`) | Zwei Adressformen für dieselbe Auswertungsfläche wären zwei Hausregeln; die Kartenklasse ist ein Pflichtbezug und gehört in die Adresse, nicht in die Abfrage. |
| **Ein `bis`-Parameter** | Ein Ende in der Vergangenheit ist eine zweite Bedienfrage ohne Nutzen; die Kurve muss bis heute laufen. |
| **`seit` als `DateOnly?` binden** | ASP.NET bindet einen unlesbaren Wert vor dem Handler ab und antwortet mit 400 **ohne unseren Befund** — belegt in `AbfrageparameterProbeTests`. Das Kriterium „Grund, Werte, Kompensation" wäre nicht erfüllbar. |
| **Ein unlesbares `seit` still ignorieren und die Standardachse liefern** | Der Aufrufer bekäme eine andere Reihe als die bestellte, ohne es zu erfahren — die schlechteste aller Antworten. |
| **Die Regel „offen" an drei Stellen schreiben** (Kurve, Tabelle, Kopfzahlen) | Drei Wahrheiten über denselben Tag. Alle drei lesen aus derselben Reihe. |
| **Karten ohne `ErledigtAm` in Abschlussspalte oder Archiv aus dem Bestand nehmen** | Machte aus einer sichtbaren Lücke eine unsichtbare und ließe die Kurve richtig aussehen, ohne es zu sein. Sie stehen mit, ihre Zahl in der Fußzeile. |
| **Ein `ArchiviertAm` nachrüsten, um sie herauszurechnen** | Migration `009` hält ausdrücklich fest, dass ein erfundenes Datum die Auswertung verdürbe. Wäre eine eigene Interaction unter `D0004`. |
| **Eine Ereignisspur je Kartenbewegung, um echten Verlauf zu bekommen** | In `I0028` verworfen, kein WBS-Knoten. Der Slice benennt die Grenze, statt sie heimlich zu überschreiten. |
| **Stunden statt Karten auf der Y-Achse** | Setzte eine geplante Dauer voraus; die Vision schließt Termin- und Kapazitätsplanung aus. |
| **Sollstrich oder Prognoselinie** | Dieselbe Voraussetzung, dasselbe Nicht-Ziel. Die Kurve sagt, was war — nicht, wann es fertig ist. |
| **Sprint- oder Iterationsachse statt Kalendertagen** | Das Projekt kennt keinen Sprintbegriff; die Kalenderachse ist die einzige, die der Bestand ohne neue Planungsgröße hergibt. |
| **Die Reihe in SQL rechnen** (rekursives CTE über Tage) | Die Regel gehörte dann in SQL statt in eine pure Operation, und Microsoft.Data.Sqlite meldet für Aggregatspalten ohne Tabellentyp `Byte[]`. Der Bestand rechnet seit `B0436` in C#. |
| **Eine Uhr-Schnittstelle einspritzen, um „heute" testbar zu machen** | Der Bestand hat diese Abstraktion bewusst nicht (`KartenRepository`, Kommentar bei `Jetzt()`). „Heute" kommt stattdessen als Eingang in die puren Operationen. |
| **Eine eigene Seite `/auswertungen/burndown`** | Der Umschalter steht seit `B0484` genau dafür; eine zweite Adresse machte Board- und Kartenklassenwahl zu zwei Zuständen. |

### Bewusst out of scope

- Puffer-Verbrauch (`I0035`), Zeitexport (`I0036`), Rohdaten über die API (`I0037`).
- `bis`-Parameter, Sollstrich, Prognoselinie, Stunden auf der Y-Achse, Sprintachse.
- Verlauf je Karte, Ereignisspur, `ArchiviertAm`.
- Export der Reihe als Datei, Vergleich mehrerer Bestände in einer Kurve.
- Änderungen an `I0033` über die zweite Auskunft hinaus.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Die eine Regel** — offen ist eine Karte an einem Tag, wenn ihr `ErledigtAm` fehlt oder **nach** dem Tag liegt; am Tag ihres `ErledigtAm` ist sie bereits erledigt. Sie steht an genau einer Stelle und entscheidet alle Ränder ohne Ausnahmeklausel.
2. **SVG von Hand, kein Diagrammpaket**; die Polylinie ist zugleich der prüfbarere Weg.
3. **Die Adressform folgt dem Bestand, nicht dem Artboard** — `…/kartenklassen/{kartenklasseId}/burndown?seit=…` statt der gezeichneten Form. **Eine bewusste Abweichung vom Bild.**
4. **Das Ende der Achse ist immer heute**, der Anfang `seit`, sonst das früheste `ErledigtAm`, sonst heute; **kein `bis`**.
5. **Archiv und Karten ohne Datum bleiben im Bestand**; eine solche Karte verlässt die Kurve nie, und ihre Zahl steht in der Fußzeile.
6. **Der Slice gilt über beide Systemgrenzen**; `I0037` bleibt unberührt (Rohdaten dort, gerechnete Reihe hier).
7. **Der Zeitraumfilter gehört hierher**, wie `R00036` es angekündigt hat.
8. **Kein Sollstrich, keine Prognoselinie, keine Stunden auf Y.**
9. **Leerfälle**: Bestand ohne erledigte Karten → Meldung mit Kompensationsaktion statt einer Kurve aus einem Punkt; alle erledigt → Kurve endet auf 0. Die API antwortet in beiden Fällen 200.
10. **Kein drittes Feature für die Datengrundlage** — sie ist vollständig da.
11. **Die Namen der Lesegestalt** (`Erledigungsstandkarte`, `Erledigungsstandkarten`) tragen bewusst das Wort „Karte", um sich vom bestehenden `Erledigungsstand` in `Operations/Karten` zu unterscheiden (C06). Ein zweiter `Erledigungsstand` in einem anderen Themenordner wäre kontextmehrdeutig.
12. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `Dokumentation/Wireframes/D0009.dc.html`, **Zustand 2** (Hauptzustand: Filterzeile, Kopfzahlen, Kurve, Tagestabelle, Fuß) und **Zustand 6** (die Trennung gerechnete Reihe / Rohdaten) sind der Verweis für die Gestaltung von `F0064`; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet** — die abweichende Adressform im Fuß des Bildes ist der Beleg dafür, dass das Bild eine Absicht zeigt und keinen Vertrag.
