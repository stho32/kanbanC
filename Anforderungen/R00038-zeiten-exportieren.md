---
id: R00038
status: In Arbeit
datum: 2026-09-07
---

# R00038: Zeiten exportieren

## Beschreibung

Für einen Kartenbestand — Board und Kartenklasse zusammen — lassen sich die erfassten Zeiten als **CSV-Datei herausziehen**: eine Zeile je Zeiteintrag mit Kartennummer, Kartentitel, Kontributor, Art, Beginn, Ende und Dauer. Die Datei kommt über `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/zeitexport.csv` aus der API; `?von=&bis=` schneidet einen Zeitraum heraus. Auf `/auswertungen` wird `Zeiten exportieren` wählbar und zeigt zwei Zeitraumgrenzen, die Zählzeile, den Dateinamen und den Verweis, über den der Browser die Datei **direkt von der WebApi** lädt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Auswertungen aus vollständigen Daten. … dazu **Zeiten je Aufgabe und Kontributor, exportierbar**."

**Die eine Regel dieses Slice, wörtlich:**

> *Eine Zeile je Zeiteintrag — nicht je Summe. Die Datei glättet nichts und rechnet nichts weg.*

„Zeiten je Aufgabe **und** Kontributor" verlangt beide Achsen. Aus Einträgen lässt sich summieren, aus Summen nicht aufteilen; deshalb ist der Eintrag die Zeile, und deshalb gibt es weder eine Summenzeile noch ein aggregiertes zweites Blatt. Aus derselben Regel folgt der Rest ohne Ausnahmeklausel:

| Lage | Folge, ohne Sonderregel |
|---|---|
| **laufender Eintrag** (kein Ende) | steht mit darin, `Ende` und `Dauer` bleiben **leer** — dieselbe Regel wie im Zeitenblock (`I0026`): eine mitlaufende Dauer wäre ab der ersten Sekunde falsch |
| **überlappende Einträge** | stehen **unkorrigiert** darin; `I0025` hat sie erlaubt und den Preis benannt. Eine Warnung hätte hier keine Kompensationsaktion |
| **Dauer über 24 Stunden** | `31:40`, nicht `07:40` — dieselbe Form wie am Schirm, damit beide Zahlen dieselbe Zahl sind |
| **archivierte Karte, stillgelegter Kontributor** | stehen mit darin — ihre Zeit wurde geleistet; dieselben zwei Regeln, die `Auswertungsrepository` und `Zeitenleser` schon führen |
| **Eintrag über Mitternacht** | bleibt **eine** Zeile mit seiner ganzen Dauer; geschnitten wird am **Beginn**, nie am Ende |

**Die Datengrundlage ist vollständig da** — anders als bei `I0033` gibt es keine geerbte Lücke zu schließen: `Zeiteintrag` trägt seit `I0024`/`I0025` Karte, Kontributor, `Beginn` und `Ende?`, `Kontributorart` seit `I0006` die Unterscheidung Mensch · Agent · abgebildet, und `Zeitenleser` liest laufende wie abgeschlossene Einträge (`Zeitenleser.cs:26-40`). **Deshalb hat dieser Slice zwei Features und nicht drei.**

**Und der Weg der Datei zum Browser steht schon.** Ein Dateidownload gibt es im Projekt bereits: `KartenEndpunkte.LiesAnhang` antwortet mit `Results.File(inhalt, typ, dateiname)` (`KartenEndpunkte.cs:210-220`), die Kartenseite zeichnet dafür nichts als ein `<a href>` **direkt auf die WebApi** (`Kartendetail.razor:375-381`, `Anhangadresse.cs`), und ein E2E-Test lädt die Datei wirklich herunter und prüft ihren Inhalt (`DateiAnKarteHaengenE2ETests.cs:208-218`). **Eine `dependency-probe` entfällt damit** — es wird nichts Unbelegtes angenommen; der Mechanismus steht mit grünem Test im Bestand.

## Geschäftlicher Nutzen

Die Vision nennt den Export in einem Atemzug mit den Auswertungen und gibt ihm einen zweiten Adressaten: „dazu Zeiten je Aufgabe und Kontributor, exportierbar. **Und dieselben Ist-Zeiten als Futter für die KI, die daraus künftige Aufgaben besser einschätzt.**" Genau dafür braucht es die Einträge und nicht ihre Auslegung.

Der Wert liegt im **Verlassen der Anwendung**. `I0033` und `I0034` beantworten Fragen, die das Board sich selbst stellt; dieser Slice beantwortet die Fragen, die es *nicht* kennt — die Abrechnung eines Monats, eine Pivot-Tabelle je Kontributor, das Nachrechnen einer Schätzung. Heute stehen die Zeiten je Karte auf der Kartenseite und je Board in der Kopfzeile, aber es gibt keinen Weg, sie **als Ganzes** herauszunehmen: wer den August auswerten will, klickt 34 Karten durch und tippt ab.

Und er kostet nichts an neuer Datenhaltung. Keine Migration, keine neue Spalte, kein zweiter Erzeuger — dieser Slice liest, was seit `I0024` ohnehin geschrieben wird, und schreibt es in eine Form, die ohne die Anwendung lesbar ist.

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/zeitexport.csv` liefert für den Kartenbestand eine CSV-Datei mit **einer Zeile je Zeiteintrag** in Beginn-Folge.
- Die Datei ist **UTF-8 mit BOM**, **CRLF**, **Semikolon**-getrennt, nach **RFC 4180** maskiert; die Kopfzeile lautet wörtlich `Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer`.
- `?von=&bis=` schneidet am **Beginn** eines Eintrags; beide Grenzen sind einschließlich und dürfen einzeln oder zusammen fehlen.
- Der Dateiname steht im `Content-Disposition` und nennt Board und die **tatsächlich gelieferten** Grenzen.
- `GET …/zeitexport` liefert für denselben Ausschnitt **nur den Stand** als JSON: Einträge, Karten, Kontributoren, davon laufende, die gelieferten Grenzen und den Dateinamen — **keine Zeilen**.
- Unbekanntes Board, unbekannte Kartenklasse sowie **unlesbares oder verdrehtes** `von`/`bis` werden mit Grund, Werten und Kompensationsaktion zurückgewiesen.
- Auf `/auswertungen` ist `Zeiten exportieren` wählbar und zeigt zwei Zeitraumgrenzen, die Zählzeile, den Dateinamen und den Verweis `CSV`.
- Der Verweis zeigt **direkt auf die WebApi**, nicht auf eine Blazor-Route; der Aufruf im Fuß folgt der gewählten Auswertung.

## Nicht-funktionale Anforderungen

- **Ein Lesevorgang je Bestand, nicht je Karte** — dieselbe Regel, unter der `LiesSollIst` seit `B0478` steht. 218 Einträge kosten dieselbe Zahl Abfragen wie einer.
- **Die Bytes fließen einmal über das Netz.** Der Browser holt die Datei bei der WebApi; über den Blazor-Prozess gingen sie zweimal und zusätzlich durch den SignalR-Kreislauf (wörtlich der Grund aus `Anhangadresse.cs`).
- **Kein JS-Interop, keine eigene `.js`-Datei.** `Pfadkopie.cs:11-13` schließt sie ausdrücklich aus; ein `<a href>` braucht keine.
- **In Excel ohne Handgriff lesbar**: Semikolon, weil die deutsche Ländereinstellung eine Kommadatei nicht spaltet; BOM, weil Excel ohne es die Umlaute der Kartentitel als Buchstabensalat zeigt.
- **Kein Limit, keine Seitengröße.** Der Ausschnitt ist der Filter, nicht die Antwortgröße.
- Gestaltungswerte ausschließlich aus `gestaltung.css` — keine Farb-, Abstands- oder Radiusliterale.

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Zeiten je Aufgabe und Kontributor lassen sich als Datei herausziehen."*

### Das durchgehende Rechenbeispiel

Board 4 „KanbanC — Release 2", Kartenklasse `WBS` (Präfix `WBS-`), heute ist der **07.09.2026**, Ortszeit `+02:00`. Fünf Zeiteinträge auf drei Karten, zwei Kontributoren:

| | Karte | Kontributor | Art | Beginn | Ende | Dauer |
|---|---|---|---|---|---|---|
| `Z5` | `WBS-12` (Titel mit `;`, `"` und Zeilenumbruch) | Claude-Agent | Agent | 2026-08-31T22:00 | 2026-09-02T05:40 | **31:40** |
| `Z3` | `WBS-24` | Stefan | Mensch | 2026-09-06T14:02 | 2026-09-06T14:50 | 0:48 |
| `Z1` | `WBS-30` | Claude-Agent | Agent | 2026-09-07T09:12 | 2026-09-07T11:24 | 2:12 |
| `Z2` | `WBS-30` | Stefan | Mensch | 2026-09-07T09:40 | 2026-09-07T09:52 | 0:12 |
| `Z4` | `WBS-24` | Stefan | Mensch | 2026-09-07T13:00 | — (**laufend**) | — |

`Z1` und `Z2` überlappen. `Z5` läuft über zwei Mitternachte. `WBS-12` ist **archiviert**.

- [x] Ohne `von`/`bis` enthält die Datei **fünf** Zeilen in der Reihenfolge `Z5`, `Z3`, `Z1`, `Z2`, `Z4` — sortiert nach `Beginn`, `ZeiteintragId` als Zweitschlüssel.
- [x] `Z4` steht mit darin, `Ende` und `Dauer` sind **leer** — zwei aufeinanderfolgende Semikolons am Zeilenende, kein `0:00` und kein Platzhalter.
- [x] `Z1` und `Z2` stehen **beide unverändert** darin; die Datei meldet die Überlappung nicht und rechnet sie nicht weg.
- [x] `Z5` steht als **eine** Zeile mit der Dauer `31:40` — nicht als drei Tageszeilen und nicht als `7:40`.
- [x] `WBS-12` steht trotz Archivierung darin; ein stillgelegter Kontributor ebenso.
- [x] Die Zählzeile lautet **5 Einträge · 3 Karten · 2 Kontributoren**, davon **1 laufend**.
- [x] `?von=2026-09-01` liefert **vier** Zeilen: `Z5` fällt heraus, obwohl es bis zum 02.09. lief — geschnitten wird am **Beginn**.
- [x] `?von=2026-09-06&bis=2026-09-06` liefert **genau `Z3`**; die gelieferten Grenzen sind `2026-09-06` und `2026-09-06`.
- [x] `?bis=2026-09-06` liefert `Z5` und `Z3` — `bis` gilt **bis zum Ende** seines Tages.
- [x] `?von=2026-09-08` liefert **200 mit der Kopfzeile allein**; die gelieferten Grenzen sind beide **heute**, und der Dateiname nennt sie.
- [x] `?von=2026-09-07&bis=2026-09-01` (`bis` vor `von`) → **400** mit Grund, den beiden gelesenen Werten und Kompensationsaktion.

### Die Datei selbst (`F0065`)

- [x] Die ersten drei Bytes sind das **UTF-8-BOM** `EF BB BF`.
- [x] Die Kopfzeile lautet **wörtlich** `Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer`.
- [x] Jede Zeile endet mit **CRLF**, auch die letzte.
- [x] Ein Kartentitel mit `;` steht in Anführungszeichen; ein enthaltenes `"` wird **verdoppelt**; ein enthaltener Zeilenumbruch steht **innerhalb** der Anführungszeichen (RFC 4180). Für `WBS-12` heißt das genau eine Datensatzzeile, deren Titelfeld einen Umbruch enthält.
- [x] `Beginn` und `Ende` stehen als **ISO 8601 mit Offset** (`2026-09-07T09:12:00.0000000+02:00`), nicht als Ortszeit ohne Zone.
- [x] `Art` trägt `Mensch`, `Agent` oder `Abgebildet` — dieselben Werte wie `Kontributorart`.
- [x] **Keine `ZeiteintragId`-Spalte**, **keine Summenzeile**, **keine zweite Dauerspalte in Dezimalstunden**. Es geht **keine Dezimalzahl** in die Datei; ein Dezimaltrennerproblem entsteht deshalb nicht.
- [x] Der Bytestrom ist **ohne HTTP** prüfbar: die Festlegungen stehen an genau einer Stelle und werden dort an den Bytes geprüft.

### Die zwei Routen (`F0065`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/zeitexport.csv` liefert für den Kartenbestand eine UTF-8-Datei mit BOM, CRLF und Semikolon: die Kopfzeile `Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer` und je Zeiteintrag **eine** Zeile in Beginn-Folge — laufende ohne Ende und ohne Dauer, Dauer als `h:mm` über 24 Stunden hinaus, Felder nach RFC 4180 maskiert, der Dateiname im `Content-Disposition`; `?von=&bis=` schneidet am Beginn; `GET …/zeitexport` liefert für denselben Ausschnitt den Stand (Einträge, Karten, Kontributoren, laufende, die gelieferten Grenzen, den Dateinamen); ein leerer Ausschnitt ist 200 mit der Kopfzeile allein; unbekanntes Board, unbekannte Kartenklasse sowie unlesbares oder verdrehtes `von`/`bis` werden mit Grund, Werten und Kompensationsaktion zurückgewiesen. Ohne Schirm allein an der Antwort prüfbar."*

- [x] `GET …/zeitexport.csv` antwortet **200** mit `Content-Type: text/csv` und einem `Content-Disposition`, dessen `filename` der gerechnete Dateiname ist.
- [x] `GET …/zeitexport` antwortet **200** mit dem Stand als JSON: Zahl der Einträge, Karten, Kontributoren, der **laufenden**, `Von`, `Bis` und der Dateiname — **und ohne Zeilen**.
- [x] **Beide Routen entstehen aus einem Dienstaufruf**: Stand und Datei rechnen denselben Ausschnitt, damit der Schirm nicht etwas anderes zählt, als die Datei enthält.
- [x] Der **Bestand** ist Board × Kartenklasse — dasselbe Set wie `GET .../kartenklassen/{kartenklasseId}/karten` (`I0022`), **archivierte Karten eingeschlossen**.
- [x] Der Dateiname folgt der Form `<board-slug>-zeiten-<von>_<bis>.csv`, der Slug ohne echte Umlaute (`ae/oe/ue/ss`) und ohne Zeichen, die ein Dateisystem nicht mag. Die genannten Grenzen sind die **gelieferten**, nie die angefragten — ein Name, der eine nicht gelieferte Spanne nennt, lügt.
- [x] Ein Bestand **ohne jeden Zeiteintrag** antwortet **200 mit der Kopfzeile allein** — **nicht 404** und kein leerer Rumpf. Eine Datei mit Kopfzeile ist die Antwort „hier wurde nichts erfasst", wie die leere Zeilenliste bei `soll-ist`.
- [x] Unbekanntes Board → **404**, Code `board-unbekannt`, Meldung mit der Board-Nummer, Kompensation `GET /api/boards`.
- [x] Unbekannte Kartenklasse → **404**, Code `kartenklasse-unbekannt`; eine Kartenklasse eines **fremden** Boards ist der eigene Fall `kartenklasse-fremd`.
- [x] Unlesbares `von` oder `bis` (z. B. `?von=gestern`) → **400** mit eigenem Befund: die Meldung nennt den **gelesenen Wert**, **welche der beiden Grenzen** gemeint ist und die erwartete Form `YYYY-MM-DD`.
- [x] Ein **fehlendes** `von` oder `bis` ist kein Fehler — die Grenze schneidet dann nicht.
- [x] Der Fehlervertragstest aus `B0102` nimmt **beide** neuen Routen auf.
- [x] **Ohne Schirm prüfbar**: alle Kriterien dieser Gruppe sind an der Antwort allein zu zeigen.

### Der Schirm reicht die Datei heraus (`F0066`)

Fertig-Kriterium wörtlich: *„Auf `/auswertungen` ist `Zeiten exportieren` wählbar und zeigt für den gewählten Bestand zwei Zeitraumgrenzen, die Zählzeile Einträge · Karten · Kontributoren mit den laufenden getrennt genannt, den Dateinamen und den Verweis, der die Datei wirklich herunterlädt — direkt von der WebApi; der Aufruf im Fuß folgt der Wahl; die vier Ränder tragen: Board ohne Kartenklasse, Bestand ohne jeden Zeiteintrag, leerer Ausschnitt trotz vorhandener Zeiten, WebApi nicht erreichbar."*

- [ ] Der Punkt `Zeiten exportieren` im Umschalter ist **wählbar** — kein gesperrter `span#auswertung-zeitexport` mehr; `Puffer-Verbrauch` und `Rohdaten über die API` bleiben gesperrt.
- [x] Board- und Kartenklassenwahl bleiben **gemeinsam** für alle drei Auswertungen; ein Wechsel wirft die Wahl nicht weg.
- [x] Die Filterzeile trägt für den Zeitexport **zwei** Datumsfelder statt des einen `Beginn` des Burndowns; **beide dürfen leer bleiben**.
- [x] Eine Änderung an `von` oder `bis` lässt **Zahlen, Dateiname und Verweis** folgen — über einen erneuten Abruf des Stands, nicht über eine zweite Rechnung im Schirm.
- [x] Die **Zählzeile** kommt gerechnet aus der API; der Schirm zählt nichts nach.
- [x] Läuft mindestens ein Eintrag, **sagt die Zeile es dazu** („davon n laufend — ohne Ende und ohne Dauer in der Datei") — dieselbe Trennung, die der Zeitenblock aus `I0026` schon macht.
- [x] Der Verweis `CSV` ist ein **`<a href>` auf die WebApi**; ein Klick löst einen **echten Browser-Download** mit dem gerechneten Dateinamen aus.
- [x] Eine **verdrehte Spanne** zeigt der Schirm als **Zurückweisung der API** über den vorhandenen Block `auswertung-zurueckweisung` — **keine zweite Prüfung in der Oberfläche**, sonst stünde dieselbe Regel an zwei Stellen.
- [x] Der **Fuß** zeigt den Aufruf der gewählten Auswertung: `GET …/soll-ist`, `GET …/burndown?seit=…` bzw. `GET …/zeitexport.csv`.
- [x] Rand 1 — **Board ohne Kartenklasse**: der vorhandene Hinweis `auswertung-ohne-kartenklasse` trägt auch hier.
- [x] Rand 2 — **Bestand ohne jeden Zeiteintrag**: Meldung mit Werten und Kompensationsaktion („Starte einen Timer auf einer Karte oder trage eine Zeit nach") statt eines Verweises auf eine leere Datei.
- [x] Rand 3 — **leerer Ausschnitt trotz vorhandener Zeiten**: dieselbe Form, aber mit der gewählten Spanne in der Meldung — der Unterschied zu Rand 2 muss lesbar sein.
- [x] Rand 4 — **WebApi nicht erreichbar**: lesbare Meldung über `WebApiAufruf.MitAusfallmeldung`; der Umschalter bleibt stehen.
- [x] **Der Verweis verschwindet bei null Einträgen** — die API liefert trotzdem 200 mit der Kopfzeile für den, der die Adresse direkt ruft: die Oberfläche darf **weniger** anbieten als die API, nicht mehr.
- [x] **Kein Gestaltungsliteral** in der neuen Fläche — geprüft wie in `AuswertungsflaecheTests`.

### Der grüne Bestand bleibt grün

- [x] `Zeitraumfilter` wächst um eine **benannte** Grenze und ein Paar; der Burndown ruft ihn unverändert mit dem Namen `seit`. **`ZeitraumfilterTests` und `B0494` bleiben grün.**
- [x] `Anhangbasisadresse` heißt danach `WebApibasisadresse`; **Wert und Voreinstellung bleiben unverändert** (interne Basisadresse als Rückfall, `WebApi:OeffentlicheBasisAdresse` für den LAN-Betrieb). `Program.cs`, `Kartendetail.razor` und die Tests ziehen mit, der Anhang-Download bleibt grün.
- [x] `I0033` und `I0034` werden **nicht** umgebaut: `soll-ist`, `burndown`, `SollIstTabelle.razor` und `Burndownkurve.razor` bleiben, wie sie sind. Sie **wachsen** nur dort, wo eine dritte Auskunft dazukommt.
- [x] **Keine Migration, keine Schemaänderung.** `Zeiteintrag`, `Kontributor` und `Kartenklassenzuordnung` werden nur gelesen.
- [x] `I0037` bleibt unberührt.

### Was dieser Slice ausdrücklich nicht tut

- [x] **Kein Kontributorenfilter** am Schirm, obwohl das Artboard einen zeichnet — die Datei führt den Kontributor in **jeder** Zeile.
- [x] **Keine Summen, kein zweites Blatt, kein XLSX, kein JSON-Zeilenexport.**
- [x] **Kein Verlauf, keine Rohdaten je Karte** — das ist `I0037`: **board**weit, JSON, **ohne** Kartenklasse.
- [x] Kein Puffer-Verbrauch (`I0035`).
- [x] Kein JS-Interop, keine eigene `.js`-Datei, keine Blazor-Durchreiche für die Bytes.

## Betroffene Verzeichnisstruktur

Alle Themenordner stehen bereits — dieser Slice legt **keinen** neuen an.

- **Contracts**: `KanbanC.Contracts/Auswertungen` — `Zeitexportstand`.
- **BL**: `KanbanC.BL/Models/Auswertungen` (`Zeitexportzeile`, Zeitraumwahl mit zwei Grenzen), `Operations/Auswertungen` (Ausschnitt, Satz, Name, Filter), `Integrations/Auswertungen` (`AuswertungsService`), `Interfaces/Auswertungen` (`IAuswertungsrepository`), `Persistenz/Auswertungen` (`Auswertungsrepository`).
- **API**: `KanbanC.WebApi/Endpunkte/AuswertungsEndpunkte.cs` — dritte und vierte Route, keine neue Datei.
- **Oberfläche**: `KanbanC.Blazor/Components/Pages/Auswertungen.razor` (+ `.razor.css`), `Components/Auswertungen/` für die Exportfläche, `Services/AuswertungenApiKlient.cs`, `Services/Zeitexportadresse.cs` und die Umbenennung `Anhangbasisadresse` → `WebApibasisadresse`. **Keine Projektreferenz auf `KanbanC.BL`** — der Weg führt über HTTP.
- **Tests**: `KanbanC.BL.Tests/{Operations,Integrations}/Auswertungen`, `KanbanC.WebApi.IntegrationTests/{Api,Persistenz/Auswertungen}`, `KanbanC.Blazor.Tests/{Services,Gestaltung}`, `KanbanC.PlaywrightTests` (Seitenobjekt `AuswertungenSeite` wächst).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Die Festlegungen der Datei wohnen an einer Stelle

BOM, Kopfzeile, Semikolon, CRLF, RFC-4180-Maskierung, leeres Ende bei laufenden Einträgen und die Dauerform stehen **in einer einzigen Operation**, die Zeilen in Bytes verwandelt. Das ist zugleich die Stelle, an der sie **ohne HTTP** prüfbar sind: der Test vergleicht Bytes, nicht eine Antwort. Stünden sie im Endpunkt, wäre jede dieser Festlegungen nur über einen Integrationstest erreichbar, und die Maskierungsfälle würden nicht geschrieben.

### Die Dauer ist heute in der falschen Schicht — und das ist der eine Umbau, der noch fehlt

`Dauerform` liegt in `KanbanC.Blazor/Services` (`Dauerform.cs`). Die Datei entsteht in der BL, und **die BL kann die Blazor-Schicht nicht sehen** — sie darf es auch nicht. Zugleich verlangt die Entscheidung ausdrücklich, dass die Zahl in der Datei und die Zahl am Schirm **dieselbe** Zahl sind.

**Der Weg: `Dauerform` zieht nach `KanbanC.Contracts` um.** Beide Seiten referenzieren Contracts bereits (`KanbanC.Blazor.csproj`, `KanbanC.BL.csproj`), die Regel bleibt an genau einer Stelle, und `DauerformTests` zieht mit. **Verworfen: eine zweite Implementation in der BL** — hier liegt echte semantische Äquivalenz vor, und zwei Stellen für „über 24 Stunden hinaus" wären genau die zweite Wahrheit, die die Entscheidung ausschließt. Das ist eine **Änderung an grünem Bestand**, für die die Bubble-Vorplanung keine eigene Bubble führt; sie gehört zu der Bubble, die die Datei schreibt. **Befund, nicht am Menschen geprüft** — siehe „Offene Fragen".

### Zwei Grenzen an einer Grenze, kein Zwilling

`Zeitraumfilter` liest heute genau eine Grenze mit dem festen Namen `seit`. Er wächst um den **Namen** der Grenze und um ein Paar; ein zweiter Filtertyp daneben wäre dieselbe Frage zweimal gestellt (C22). Der Grund seiner Entstehung gilt unverändert: ASP.NET bindet einen unlesbaren `DateOnly` **vor** dem Handler ab und antwortete ohne unseren Befund (`Zeitraumfilter.cs`, Kommentarkopf). Die verdrehte Spanne (`bis` vor `von`) ist **dieselbe Grenze**: sie wird dort geprüft, nicht im Dienst und nicht im Schirm.

### Der Schnitt am Beginn, mit der Uhr des Eintrags

`von` gilt ab 00:00 seines Tages, `bis` bis zum Ende seines Tages, beide einschließlich. **Der Tag eines Eintrags ist der Tag seiner eigenen Uhr** — der Wert trägt seinen Offset, und das ist die Zeit, die der Mensch beim Starten gesehen hat. Die Regel steht in einer **puren** Operation, die die Grenzen als Eingang bekommt; „heute" für den Leerfall kommt wie beim Burndown aus der Integration, nicht aus einer Uhr-Abstraktion (`AuswertungsService.Heute()`, Hausregel).

### Der Leseweg: ein Vorgang, das Vorbild steht daneben

`Auswertungsrepository` bekommt seine **dritte** Auskunft neben `LiesSollIst` (`B0478`) und `LiesErledigungsstaende` (`B0489`) — derselbe Schnitt über `Spalte`, `Kartenklassenzuordnung` und `Kartenklasse`, dazu `JOIN Kontributor`. **Ohne `AND Ende IS NOT NULL`**, anders als `LiesErfassteZeiten` daneben: die laufenden Einträge gehören in die Datei. `ORDER BY z.Beginn, z.ZeiteintragId`. **Der Boardname reist mit**, weil der Dateiname aus ihm entsteht und ein zweiter Lesevorgang dafür Verschwendung wäre.

### Der Stand ist bewusst arm

`GET …/zeitexport` liefert **keine Zeilen**. Er ist damit **kein zweiter Weg zu den Daten** und nimmt `I0037` nichts vorweg. Gebraucht wird er aus zwei Gründen, die beide am Schirm sitzen: der Leerfall muss nach der Hausregel „Grund mit Werten und Kompensationsaktion" beantwortet werden, statt einen Verweis auf eine leere Datei anzubieten, und die gezeichnete Zählzeile braucht gerechnete Zahlen. **Fällt die Zählzeile, fällt der Stand mit** — und mit ihm zwei Bubbles.

### Die Adressform folgt dem Bestand, nicht dem Artboard

`…/kartenklassen/{kartenklasseId}/zeitexport.csv` statt des gezeichneten `…/auswertungen/zeiten.csv?kartenklasse=1` (`D0009.dc.html:527`): der Bestand ist ein Pflichtbezug und gehört in die Adresse, wie bei `soll-ist` und `burndown` (`I0034`, Entscheidung 2). Die Endung `.csv` steht **in der Adresse** und nicht in einem `Accept`-Kopf — ein `<a href>` kann keinen Kopf setzen.

### Ablauf

1. **Der Mensch wählt** auf `/auswertungen` den Punkt `Zeiten exportieren`.
   - Board und Kartenklasse stehen bereits; der Umschalter wirft sie nicht weg.
2. **Der Schirm holt den Stand**
   - 2.1 `AuswertungenApiKlient.LadeZeitexportstand(boardId, kartenklasseId, von, bis)`
   - 2.2 `GET …/zeitexport` → `Zeitraumfilter` liest beide Grenzen
     - 2.2.1 unlesbar oder verdreht → **400**, der Schirm zeigt `auswertung-zurueckweisung`
   - 2.3 `AuswertungsService.Zeitexport` → `PruefeBestand` → Repository → Ausschnitt → Stand
3. **Der Schirm zeigt** Zählzeile, Dateiname und den Verweis
   - 3.1 null Einträge → Meldung mit Kompensationsaktion, **kein Verweis**
   - 3.2 sonst `<a href="@Zeitexportadresse.Fuer(...)">CSV</a>` — absolut, auf die WebApi
4. **Der Browser lädt die Datei** — ohne Blazor dazwischen
   - 4.1 `GET …/zeitexport.csv` → derselbe Dienstaufruf, dieselben Zeilen
   - 4.2 Zeilen → CSV-Bytes → `Results.File(bytes, "text/csv", dateiname)`
5. **Der Agent** ruft Schritt 4 direkt — er bekommt dieselben Daten, und er bekommt sie zuerst.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** die dritte und vierte Route in `AuswertungsEndpunkte`; der Punkt `zeitexport` in `Auswertungen.razor`, der heute in der Liste `NochNichtGebaut` steht (`Auswertungen.razor:150`); die dritte Methode an `IAuswertungsrepository`, `AuswertungsService` und `AuswertungenApiKlient`.

**In `KanbanC.Contracts/Auswertungen`** (immutable, C08):
- `Zeitexportstand` (DTO) — Zahl der Einträge, Karten, Kontributoren und der laufenden, die gelieferten `Von`/`Bis` und der Dateiname. **Trägt keine Zeilen.**

**In `KanbanC.Contracts`** (Umzug aus `KanbanC.Blazor/Services`):
- `Dauerform` (Operation, pur) — unverändert; nur der Ort ändert sich, damit Datei und Schirm dieselbe Zahl schreiben.

**In `KanbanC.BL/Models/Auswertungen`:**
- `Zeitexportzeile` (Modell, immutable) — `ZeiteintragId`, Kartennummer, Kartentitel, Kontributorname, `Kontributorart`, `Beginn`, `Ende?`. **Bleibt in der BL** — sie überquert die Prozessgrenze nie, sondern wird zu Text.
- `Zeitexportzeilen` (benannte Collection) — der gelesene Bestand samt Boardname.
- `Zeitraumwahl` **wächst** um eine zweite Grenze: `Von?` und `Bis?` statt `Seit?`.
- `Zeitausschnitt` (Ergebnis des Schnitts) — die verbliebenen Zeilen und die **tatsächlich gelieferten** Grenzen.

**In `KanbanC.BL/Operations/Auswertungen`:**
- `Zeitausschnitt` (Operation, pur) — schneidet am **Beginn** und liefert die gelieferten Grenzen mit.
  - `Zeitausschnitt Schneide(Zeitexportzeilen zeilen, DateOnly? von, DateOnly? bis, DateOnly heute)`
- `Zeitexportsatz` (Operation, pur) — **die eine Stelle aller Dateifestlegungen**.
  - `byte[] AlsCsv(Zeitexportzeilen zeilen)`
- `Zeitexportname` (Operation, pur) — `<board-slug>-zeiten-<von>_<bis>.csv`.
  - `string Fuer(string boardname, DateOnly von, DateOnly bis)`
- `Zeitraumfilter` **wächst** um den Namen der Grenze und um das Paar.
  - `Ergebnis<Zeitraumwahl> Aus(string? abfragewert, string parametername, string route)`
  - `Ergebnis<Zeitraumwahl> AusPaar(string? von, string? bis, string route)`

**In `KanbanC.BL/Interfaces/Auswertungen` und `KanbanC.BL/Persistenz/Auswertungen`:**
- `IAuswertungsrepository` bekommt `Zeitexportzeilen LiesZeiteintraege(long boardId, long kartenklasseId)`; `Auswertungsrepository` setzt sie um (Integration, ein Lesevorgang, SQL nach Skill `sql-stil`).

**In `KanbanC.BL/Integrations/Auswertungen`:**
- `AuswertungsService` bekommt `Ergebnis<Zeitexport> Zeitexport(long boardId, long kartenklasseId, DateOnly? von, DateOnly? bis)` — **dieselbe** `PruefeBestand`-Vorprüfung, **ein** Aufruf für beide Routen; `Zeitexport` trägt Zeilen **und** Stand.

**In `KanbanC.WebApi/Endpunkte`:**
- `AuswertungsEndpunkte` bekommt `ZeitexportstandRoute` und `ZeitexportdateiRoute`; die Datei über `Results.File(bytes, "text/csv", dateiname)`.

**In `KanbanC.Blazor`:**
- `WebApibasisadresse` (Umbenennung von `Anhangbasisadresse`) — Wert und Voreinstellung unverändert.
- `Zeitexportadresse` (Operation, pur, Muster `Anhangadresse`) — `string Fuer(string oeffentlicheBasisAdresse, long boardId, long kartenklasseId, DateOnly? von, DateOnly? bis)`.
- `AuswertungenApiKlient` bekommt `Task<ApiErgebnis<Zeitexportstand>> LadeZeitexportstand(...)` — **nur der Stand reist über den Klienten**.
- `Zeitexportflaeche.razor` (+ `.razor.css`) — zwei Datumsfelder, Zählzeile, Dateiname, Verweis, Ränder.

**Kein Interface** für die reinen Operationen: je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Zeitraumfilter` | benannte Grenze statt festem `seit`; zweite Einstiegsmethode für das Paar samt Prüfung `bis` vor `von`. **Der Burndown ruft unverändert** |
| `Zeitraumwahl` | `Von?`/`Bis?` statt `Seit?`; der Burndown liest die erste Grenze |
| `IAuswertungsrepository` / `Auswertungsrepository` | dritte Auskunft `LiesZeiteintraege` |
| `AuswertungsService` | dritte Auskunft `Zeitexport` — dieselbe `PruefeBestand` |
| `AuswertungsEndpunkte` | zwei Routen; `BurndownAdresse` wird zu einer Adressform für drei Auswertungen |
| `AuswertungenApiKlient` | dritte Methode `LadeZeitexportstand` |
| `Auswertungen.razor` | `zeitexport` wandert von `NochNichtGebaut` nach `Gebaut`; Filterzeile trägt für diese Wahl zwei Datumsfelder; der Fuß nennt `…/zeitexport.csv` |
| `Anhangbasisadresse` → `WebApibasisadresse` | Umbenennung; `Program.cs`, `Kartendetail.razor`, `AnhangbasisadresseTests` ziehen mit |
| `Dauerform` | Umzug `KanbanC.Blazor/Services` → `KanbanC.Contracts`; `DauerformTests` zieht mit |
| `AuswertungenSeite` (E2E-Seitenobjekt) | wächst um zwei Datumsfelder, Zählzeile und Verweis |

**Nicht geändert:** `SollIstTabelle.razor`, `Burndownkurve.razor`, `Burndownrechner`, `Burndownzeitraum`, alle `SollIst…`- und `Burndown…`-Verträge, `Zeitenleser`, `KartenRepository`, `Pfadkopie`, alles unter `Persistenz/Migrationen/`.

### Wireframe

`Dokumentation/Wireframes/D0009.dc.html`, **Zustand 5** (Ausschnitt der Exportfläche: Umfang, Zählzeile, Verweis `CSV`, Dateiname, die Spalten der Datei, „was die Datei nicht glattzieht") und **Zustand 7 A/B/C** (die Ränder) sind der **Verweis für die Gestaltung** von `F0066`. Das Bild ist Entwurfsquelle, **nie Kriterienquelle**; kein Akzeptanzkriterium dieser Anforderung ist aus ihm abgeleitet.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md`; jeder Test verifiziert eine echte Zustandsänderung (Skill `test-ehrlichkeit`).

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Zeitexportsatz.AlsCsv` — BOM, Kopfzeile im Wortlaut, CRLF, Semikolon; ein Titel mit `;`, einer mit `"`, einer mit Zeilenumbruch; ein laufender Eintrag (Ende und Dauer leer); eine Dauer über 24 Stunden; ISO-Form mit Offset. **An den Bytes, ohne HTTP.**
- `Zeitausschnitt.Schneide` — beide Grenzen, nur `von`, nur `bis`, keine; der Eintrag über Mitternacht bleibt ganz; der Eintrag mit Beginn vor `von` fällt heraus; leerer Ausschnitt liefert heute für beide Grenzen.
- `Zeitexportname.Fuer` — Umlaute im Boardnamen, Zeichen, die ein Dateisystem nicht mag, gleiche Grenzen.
- `Zeitraumfilter` — beide Namen, unlesbarer Wert je Grenze, `bis` vor `von`; **und die bestehenden `seit`-Tests bleiben unverändert grün**.
- `Dauerform` nach dem Umzug — dieselben Fälle, neuer Ort.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `Auswertungsrepository.LiesZeiteintraege` — laufende Einträge kommen mit, archivierte Karten und stillgelegte Kontributoren stehen mit darin, Fremdbestand bleibt draußen, Sortierung nach `Beginn` und `ZeiteintragId`.
- Beide Routen: 200 mit `Content-Type` und `Content-Disposition`, 200 mit Kopfzeile allein bei leerem Ausschnitt, 400 bei unlesbarem und bei verdrehtem `von`/`bis`, die drei 404-Fälle; der Fehlervertragstest aus `B0102` erweitert.
- `AuswertungsService.Zeitexport` gegen das Test-Repository — Stand und Zeilen aus **einem** Aufruf.

**`KanbanC.Blazor.Tests`:** `AuswertungenApiKlient.LadeZeitexportstand` — 200 wird gelesen, 400 und 404 werden zur lesbaren Zurückweisung, `HttpRequestException` zur Ausfallmeldung. **Diese Pfade sind über den Browser nicht auslösbar** — genau der Grund, aus dem dieses Testprojekt existiert. Dazu `Zeitexportadresse.Fuer` (absolute Adresse, Grenzen in der Abfrage, Basisadresse als Parameter) und die Gestaltungsprüfung ohne Farb-, Abstands- und Radiusliteral.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — Karte anlegen, eine Zeit nachtragen, `/auswertungen` öffnen, `Zeiten exportieren` wählen, Zählzeile lesen, **die Datei über `RunAndWaitForDownloadAsync` wirklich herunterladen**, `SuggestedFilename` und den Inhalt prüfen (`DateiAnKarteHaengenE2ETests.cs:208-218` als Vorbild). Dazu die **Gegenprobe, dass der `href` auf die WebApi und nicht auf Blazor zeigt**, und der Fuß, der nach dem Umschalten `zeitexport.csv` nennt.

## Abhängigkeiten

- Abhängig von: **`R00026`**/**`R00027`**/**`R00028`**/**`R00029`** (`I0026` — Zeiten einer Karte sehen, **grün**), das `Braucht` der Interaction und von `F0065`: erst dort ist entschieden, wie ein laufender Eintrag zählt.
- Und von **`R00037`** (Burndown sehen — `I0034`, **grün**): `Zeitraumfilter`, der Umschalter, die Fläche und der Fuß von `/auswertungen` stammen von dort; **`R00036`** (Soll-Ist — `I0033`) trägt `Auswertungsrepository`, `AuswertungsService`, `AuswertungsEndpunkte` und `AuswertungenApiKlient`.
- Setzt außerdem auf (alle grün): **`R00020`** (Anhang-Download — `Results.File`, `Anhangadresse`, `Anhangbasisadresse`, der E2E-Downloadtest), **`R00011`**/**`R00014`** (Kontributor, `Kontributorart`, Stilllegung), **`R00022`**/**`R00023`**/**`R00025`** (Kartenklassen, Kartennummern, der Mengenbegriff „Karten einer Klasse"), **`R00016`** (Archivstand), **`R00005`** (Kopfzeile und Gestaltungstokens).
- Blockiert: nichts. `I0035` führt `Braucht: I0034`, `I0037` führt `Braucht: I0011` — **keiner von beiden hängt an diesem Slice**.
- **`D0009` wird mit diesem Slice nicht grün** — `I0035` und `I0037` bleiben rot.

## Umfang

```
Zeiten exportieren (I0036) = 16 Bubbles: 15 Standard (20,4h), 1 unklar (2,0-4,0h).
Rest: 20,4h klar + 2,0-4,0h unklar · 0 von 16 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 16 Bubbles gruen (0 %) · 0 laufen · 16 offen
```

`I0036` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **zwei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0065` Die Zeiten des Bestands als Datei über die API | `B0503`–`B0511` (9) | 9 (11,6h) | 0 | `I0026` |
| `F0066` Der Schirm reicht die Datei heraus | `B0512`–`B0518` (7) | 6 (8,8h) | 1 (2,0–4,0h) | `F0065` |

**Warum zwei Features:** weil zwei Aspekte **getrennt fertig** werden. `F0065` ist allein an der Antwort prüfbar und könnte vollständig sein, während der Schirm noch nichts zeigt; `F0066` reicht heraus, was `F0065` liefert. **Ein drittes Feature für die Datengrundlage entfällt** — anders als bei `I0033` ist sie vollständig da.

Die eine unklare Bubble ist `B0518` (E2E über beide Prozesse, mit echtem Download). Alles davor ist pure Logik oder eine dritte Auskunft an einer Klasse, die es schon gibt.

**Nach gemessenem Durchsatz ist mit etwa 0,5–1,0 h zu rechnen.** Die Richtwert-Konvention seit `I0004` überschätzt messbar; die Zählung wird trotzdem nicht still gekippt — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0036` und an beiden Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00037`/`I0034`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **`Dauerform` liegt in der Oberflächenschicht und wird in der BL gebraucht.** `Dauerform.cs` steht in `KanbanC.Blazor/Services`; die Datei entsteht in der BL, und die BL sieht die Blazor-Schicht nicht (`KanbanC.BL.csproj` referenziert nur `KanbanC.Contracts`). **Angenommen: `Dauerform` zieht nach `KanbanC.Contracts` um** — beide Seiten referenzieren Contracts bereits, die Regel bleibt an einer Stelle, `DauerformTests` zieht mit. Die Alternative, eine zweite Implementation in der BL, ist verworfen: hier liegt echte semantische Äquivalenz vor, und zwei Stellen wären die zweite Wahrheit, die Entscheidung 5 ausschließt. **Der Umzug ist eine Änderung an grünem Bestand ohne eigene Bubble** — er gehört zu `B0507`. **Nicht am Menschen geprüft.**
- **Der JSON-Zwilling `GET …/zeitexport` ist die teuerste Annahme dieses Slice.** Er trägt **nur den Stand, bewusst ohne Zeilen**, damit er kein zweiter Weg zu den Daten neben `I0037` wird. Nötig ist er, weil der Schirm den Leerfall nach der Hausregel „Grund mit Werten und Kompensationsaktion" beantworten muss, statt einen Verweis auf eine leere Datei anzubieten, und weil er die gezeichnete Zählzeile trägt. **Fällt die Zählzeile, fallen `B0511` und `B0516` mit** — und der Schirm bestünde dann nur noch aus zwei Datumsfeldern und einem Verweis. **Nicht am Menschen geprüft.**
- **ISO 8601 mit Offset und Sekunden statt der Artboardform.** Das Bild zeichnet `2026-09-07T09:12` (`D0009.dc.html:502-504`) — ohne Sekunden, ohne Zone. Gewählt ist die Roundtrip-Form „O" wie in der Ablage: „ein DateTime verliert unterwegs seine Zeitzone" steht wörtlich am `Zeiteintrag`-DTO, und diese Datei reist zu Agenten. **Der Preis ist benannt: Excel führt die Spalte als Text**, nicht als Datum. Wer in Excel rechnen will, rechnet mit der Dauerspalte. **Nicht am Menschen geprüft.**
- **Der Dateiname bei leerem Ausschnitt nennt heute für beide Grenzen.** Ein Name ohne Spanne (`…-zeiten-leer.csv`) wäre eine dritte Form; ein Name mit den *angefragten* Grenzen behauptete eine Lieferung, die es nicht gab. **Nicht am Menschen geprüft.**
- **Der Board-Slug nimmt den ganzen Boardnamen.** Das Artboard schreibt `release-2-zeiten-…` für das Board „KanbanC — Release 2" und lässt den Anfang weg (`D0009.dc.html:506`); hier steht `kanbanc-release-2-zeiten-…`. Ein gekürzter Name unterschiede zwei Boards nicht mehr, deren Namen sich am Anfang trennen. **Angenommen, Abweichung vom Bild — das Bild ist eine Absicht, kein Vertrag.**
- **`I0035` (Puffer-Verbrauch) hat keinen definierten Gegenstand.** Das Artboard zeichnet ihn als **Frage** mit zwei fehlenden Voraussetzungen. Er kommt zuletzt und wird hier nicht vorbereitet. **Befund für `/planung`, hier nicht geändert** — diese Familie ändert keine Knoten.

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit und liest nur, was bereits geschrieben wird.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Konfiguration `WebApi:OeffentlicheBasisAdresse` bleibt unter demselben Schlüssel; nur der C#-Typ heißt anders.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt steht in der Vision und ist konkret: „dazu Zeiten je Aufgabe und Kontributor, exportierbar. Und dieselben Ist-Zeiten als Futter für die KI." Das Board erfasst diese Zeiten seit `I0024` vollständig — mit Karte, Kontributor, Art, Beginn und Ende —, aber es gibt keinen Weg, sie **als Ganzes** herauszunehmen: wer einen Monat abrechnen will, klickt Karte für Karte durch und tippt ab, und ein Agent, der aus Ist-Zeiten schätzen soll, bekommt sie nur häppchenweise je Karte. Wenn eine einzige Operation die gelesenen Einträge in eine CSV-Datei verwandelt und die WebApi sie über eine Adresse ausliefert (X), dann holt der Browser dieselben Bytes über ein nacktes `<a href>` und ein Agent über denselben `GET` (Y), sodass die Zeiten in jedem Tabellenwerkzeug und in jedem Skript liegen, ohne dass die Anwendung dafür etwas nachbauen müsste (Z). Der Hebel sitzt genau hier und nicht vorgelagert: an der Datenhaltung ist nichts zu tun, und ein Aggregat in der Anwendung wäre die falsche Richtung — aus Einträgen lässt sich summieren, aus Summen nicht aufteilen. Und er sitzt nicht nachgelagert bei `I0037`: dort kämen **boardweite** JSON-Rohdaten ohne Kartenklassenbezug heraus, die niemand ohne Programm liest; „als Datei herausziehen" heißt eine Form, die **ohne** die Anwendung lesbar ist.

## Missing-Docs

- **CSV für Excel in deutscher Ländereinstellung.** Dass Semikolon plus BOM die Kombination ist, mit der Excel eine UTF-8-Datei ohne Importassistenten korrekt öffnet, ist im Repository nirgends notiert; der Bestand schreibt bisher keine Datei zum Weiterverarbeiten. Für `B0507` ist das die einzige Größe, die von außen kommt.
- **`Results.File` mit erzeugten Bytes.** Der Bestand ruft `Results.File` bisher nur mit gespeicherten Anhangbytes (`KartenEndpunkte.cs:210-220`). Wie sich `Content-Disposition` bei einem Dateinamen mit Nicht-ASCII verhält (RFC 5987, `filename*`), ist nicht notiert — der Slug vermeidet das Problem, aber die Notiz fehlt.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Blob über `IJSRuntime`** (`URL.createObjectURL` plus erzeugtes Ankerelement) | Die Bytes flössen durch den SignalR-Kreislauf, und es bräuchte die **erste eigene `.js`-Datei** des Projekts, die `Pfadkopie.cs:11-13` ausdrücklich ausschließt. |
| **Eine Blazor-Route als Durchreiche** | Die Bytes gingen zweimal über das Netz, und der Browser bekäme keinen echten Download mit Name, Fortschritt und Abbruch — wörtlich der Grund aus `Anhangadresse.cs`. |
| **Eine `dependency-probe` für den Download** | Es wird nichts Unbelegtes angenommen: `Results.File`, das `<a href>` und der E2E-Download stehen mit grünem Test im Bestand. |
| **„Als Text kopieren" wie `Pfadkopie`** | Das Muster gehört **flüchtigen** Ausgaben; das Kriterium sagt „als Datei", und eine Datei ist der Unterschied. |
| **Ein Kontributorenfilter am Schirm** (das Artboard zeichnet einen) | Die Datei führt den Kontributor in **jeder** Zeile; ein drittes Bedienelement wäre eine Bedienfrage ohne Kriterium und für einen Agenten tote Flexibilität (C24) — dasselbe Argument, mit dem `I0022` den Klassenfilter weggelassen hat. |
| **Eine zweite Filterzeile für den Export** | Der Umfang ist derselbe Bestand wie oben; das Artboard sagt es wörtlich. |
| **Nur eine Zeitraumgrenze wie beim Burndown** | Dessen Achse **muss** bis heute laufen; eine Exportdatei ist ein Dokument über eine **abgeschlossene** Spanne — „den August herausziehen" ist der gewöhnliche Anlass. |
| **Am Ende eines Eintrags schneiden oder ihn an der Grenze kürzen** | Ein Eintrag über Mitternacht würde zerschnitten oder verlöre Dauer — genau das Glattziehen, das dieser Slice ausschließt. |
| **Komma als Trennzeichen** | Excel in deutscher Ländereinstellung spaltet eine Kommadatei nicht. |
| **UTF-8 ohne BOM** | Excel zeigt die Umlaute der Kartentitel als Buchstabensalat. |
| **Eine zweite Dauerspalte in Dezimalstunden** | Eine zweite Wahrheit über dieselbe Zahl — und sie brächte das Dezimaltrennerproblem erst herein. Beginn und Ende stehen daneben; wer genauer rechnen will, rechnet aus ihnen. |
| **Eine Summenzeile oder ein aggregiertes zweites Blatt** | Aus Einträgen lässt sich summieren, aus Summen nicht aufteilen. |
| **Eine `ZeiteintragId`-Spalte** | Das Kriterium fragt nach Zeiten je Aufgabe und Kontributor, nicht nach Schlüsseln. Rohdaten samt Schlüsseln sind der Gegenstand von `I0037`. |
| **Überlappungen warnen oder wegrechnen** | `I0025` hat sie erlaubt und den Preis benannt; eine Warnung hätte hier keine Kompensationsaktion, und für einen Agenten, der an mehreren Karten zugleich rechnet, wäre sie eine Falschauskunft. |
| **Archivierte Karten oder stillgelegte Kontributoren herausnehmen** | Ihre Zeit wurde geleistet — dieselben zwei Regeln, die `Auswertungsrepository` und `Zeitenleser` schon führen. |
| **Laufende Einträge weglassen** oder mit mitlaufender Dauer schreiben | Weglassen verschwiege erfasste Arbeit; eine mitlaufende Dauer wäre ab der ersten Sekunde falsch (`I0026`, Entscheidung 2). Leeres Ende und leere Dauer sind die ehrliche Form. |
| **Ein leerer Ausschnitt als 404** | Eine Datei mit Kopfzeile ist die Antwort „hier wurde nichts erfasst" — wie die leere Zeilenliste bei `soll-ist`. |
| **`GET …/zeitexport` mit Zeilen im JSON** | Das wäre ein zweiter Weg zu den Daten und nähme `I0037` vorweg. Der Stand trägt bewusst keine Zeilen. |
| **Zwei Dienstaufrufe für Stand und Datei** | Der Schirm zählte dann etwas anderes, als die Datei enthält. |
| **Die Adressform des Artboards** (`…/auswertungen/zeiten.csv?kartenklasse=1`) | Der Bestand ist ein Pflichtbezug und gehört in die Adresse, wie bei `soll-ist` und `burndown`. |
| **`.csv` über einen `Accept`-Kopf statt in der Adresse** | Ein `<a href>` kann keinen Kopf setzen. |
| **Ein `Zeitraumfilter`-Zwilling für das Paar** | Dieselbe Frage zweimal gestellt (C22); der Filter bekommt stattdessen den Namen der Grenze als Eingang. |
| **Die verdrehte Spanne in der Oberfläche prüfen** | Dieselbe Regel stünde an zwei Stellen. Der Schirm zeigt die Zurückweisung der API. |
| **Eine zweite `Dauerform` in der BL** | Echte semantische Äquivalenz — zwei Stellen für „über 24 Stunden hinaus" wären die zweite Wahrheit, die Entscheidung 5 ausschließt. |
| **Eine eigene Seite `/auswertungen/zeitexport`** | Der Umschalter steht seit `B0484` genau dafür; eine zweite Adresse machte Board- und Kartenklassenwahl zu zwei Zuständen. |

### Bewusst out of scope

- Puffer-Verbrauch (`I0035`) und Rohdaten über die API (`I0037` — **boardweit, JSON, ohne Kartenklasse**).
- Kontributorenfilter, Summen, aggregiertes zweites Blatt, XLSX, JSON-Zeilenexport, Export der Soll-Ist- oder Burndown-Reihe.
- Wiederkehrende oder geplante Exporte, Versand, Ablage im Dateisystem der WebApi.
- `ZeiteintragId` in der Datei, Verlauf je Karte, Ereignisspur.
- Änderungen an `I0033`/`I0034` über die dritte Auskunft hinaus.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Der Weg der Datei ist ein Verweis auf die API-Route** — nachgesehen, nicht angenommen: `Results.File`, das nackte `<a href>` und der E2E-Download stehen mit grünem Test im Bestand. Der Knopf des Menschen ist nichts als dieser Verweis; **der Agent bekommt dieselben Daten, und er bekommt sie zuerst.**
2. **Umfang = Board + Kartenklasse** (derselbe Bestand wie `I0022`/`I0033`/`I0034`) **plus optionaler Zeitraum**; **kein Kontributorenfilter**, obwohl das Artboard einen zeichnet.
3. **Zwei Zeitraumgrenzen** statt der einen des Burndowns; **geschnitten wird am Beginn, nie am Ende**.
4. **Die CSV-Festlegungen**: Semikolon · UTF-8 **mit** BOM · CRLF · RFC 4180 · Kopfzeile im Wortlaut · Dauer als `h:mm` über 24 h hinaus · Beginn/Ende als ISO 8601 **mit Offset** · laufende mit leerem Ende und leerer Dauer · archivierte Karten und stillgelegte Kontributoren mit darin · sortiert nach Beginn, `ZeiteintragId` als Zweitschlüssel.
5. **Kein Dezimaltrennerproblem, weil keine Dezimalzahl in die Datei geht** — und deshalb keine zweite Dauerspalte.
6. **Die Datei glättet nichts**: Überlappungen und Dauern über 24 h bleiben unkorrigiert.
7. **Der JSON-Zwilling trägt nur den Stand, ohne Zeilen** — kein zweiter Weg zu den Daten neben `I0037`.
8. **`Dauerform` zieht nach `KanbanC.Contracts` um**, damit Datei und Schirm dieselbe Zahl schreiben.
9. **`Anhangbasisadresse` heißt `WebApibasisadresse`**; **`Zeitraumfilter` wächst um eine benannte Grenze und `bis`** — beides Änderungen an grünem Bestand, der grün bleiben muss.
10. **Der Board-Slug nimmt den ganzen Boardnamen**; der Dateiname bei leerem Ausschnitt nennt heute für beide Grenzen.
11. **Der Slice gilt über beide Systemgrenzen**; `I0035` und `I0037` bleiben unberührt.
12. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `D0009.dc.html`, **Zustand 5** und **Zustand 7** sind der Verweis für die Gestaltung von `F0066`; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet** — die abweichende Adressform, der gezeichnete Kontributorenfilter und die verkürzte Zeitform sind drei Belege dafür, dass das Bild eine Absicht zeigt und keinen Vertrag.
