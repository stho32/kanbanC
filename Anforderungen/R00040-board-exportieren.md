---
id: R00040
status: Neu
datum: 2026-09-08
---

# R00040: Board exportieren

## Beschreibung

Eine neue Route schreibt **ein einzelnes Board als eine Datei** heraus: `GET /api/boards/{boardId}/export.json` liefert über `Results.File` ein JSON-Dokument, das Board, Spalten, Kartenklassen, die referenzierten Kontributoren, **alle** Karten samt Ort, Archivmarke, Klassenzuordnung, Sollband und den fünf Listen sowie **alle** Zeiteinträge trägt — dazu einen Kopf mit Fassungsnummer, Erzeugungszeitpunkt und der ausdrücklichen Angabe, dass die Bytes der Anhänge nicht mitreisen. Im ⋯-Menü der Boardkachel führt ein dritter Punkt `Exportieren` als `<a href>` direkt auf diese Route; ein Klick lädt die Datei, ohne die Seite zu verlassen. **Dieselbe Adresse bedient den Browser und den Agenten** — eine Ressource, zwei Verwendungen, keine zweite Wahrheit.

Zahlt ein auf: [Vision](R00000-vision.md) — „Lokale Datenhaltung … die Daten liegen auf der eigenen Maschine und sind von dort **unmittelbar zugänglich**"; die Portabilität, die eine Datei je Board gebracht hätte, ist bei `R00001` bewusst gegen **eine** SQLite-Datei für alle Boards eingetauscht worden und kommt seitdem über `I0038`/`I0039` zurück (WBS, Notiz zur Datenhaltung).

**Die eine Regel dieses Slice, wörtlich:**

> *Eigenständig ist eine Datei, die niemanden mehr braucht: keinen zweiten Aufruf, keinen Auflöser, kein Werkzeug. Was sie nicht tragen kann, sagt sie.*

Daraus folgt alles Übrige ohne Ausnahmeklausel:

| Lage | Folge, ohne Sonderregel |
|---|---|
| **eine Nummer in einer Zeile** (Spalte, Kartenklasse, Kontributor, Karte) | die Zeile, auf die sie zeigt, steht **in derselben Datei** — kein Verweis ins Leere |
| **eine Nummer, die ein Mensch liest** | **daneben steht ihr Name** im selben Dokument — lesbar, nicht nur auflösbar |
| **archivierte Karte, weggekürzte Karte** | stehen **darin**, mit Marke; die Anzeigeregeln des Boards gelten hier nicht |
| **Kontributor „7" an einer Karte** | reist als **ganzer Kontributor** mit Name, Art und Stilllegung — aber nur, wenn ihn eine Zeile dieses Boards nennt |
| **Anhang** | **Metadaten ja, Bytes nein** — und der Kopf sagt es, statt es zu verschweigen |
| **Sollband einer Karte** (`Kartensollzeit`) | reist mit; ohne es verlöre eine ausgeleitete Karte das Band, das der WBS-Import geschrieben hat |
| **Zählerstand der Klassenzuordnung** | reist **getrennt** von der fertigen `Kartennummer`, weil er aus ihr nicht sicher zurückzurechnen ist |
| **leeres Board** | **200 mit einer vollständigen Datei**, die Board, Spalten und leere Listen trägt — kein Fehler |
| **unbekanntes Board** | zurückgewiesen mit Grund, Werten und Kompensationsaktion (`Nichtgefunden.Board`, unverändert genutzt) |

**Was dieser Slice nicht ist:** kein Schema, keine Migration, kein Schreibweg, keine Rechnung, kein Import. Eine Route, **drei** neue Leseweiten, ein Zusammenbau, zwei pure Operationen und ein Menüpunkt.

## Geschäftlicher Nutzen

**Nachgezählt, nicht angenommen:** das Schema führt für ein Board **19 Tabellen**, und **16 davon reisen im grünen Bestand bereits mit** — `GET /api/boards/{boardId}/karten` (`R00039`) trägt Karte, Karteneigenschaft, Karteerledigung, Kartenarchivierung, Etikett, Teilaufgabe, Kommentar, Anhang (als Metadaten) und Dateiverweis samt Ort, Archivmarke und Kartenklasse in **einer** Lesetransaktion; `…/zeiten` die Zeiteinträge mit **ganzem** Kontributor; `…/kartenklassen` die Klassen mit Präfix und Zählerstand; `GET /api/boards/{boardId}` Board, Boardeinstellung und Boardarchivierung.

**Drei Lücken fehlen wirklich, und sie fehlen an nachgesehenen Stellen:**

| Lücke | Beleg | Was ohne sie verloren ginge |
|---|---|---|
| **`Kartensollzeit`** reist in **keinem** DTO mit | gelesen einzig im Soll-Ist-Join, `Auswertungsrepository.cs:48`, und dort **als gerechnete Abweichung, nicht als gespeicherte Zeile** | das **Sollband**, das der WBS-Import (`R00033`) geschrieben hat — kein späterer Lauf gäbe es zurück |
| **`Zaehlerstand`** der Klassenzuordnung wird nur **verrechnet** | steht in der Abfrage (`Kartenleser.cs:55`), geht aber allein in die fertige `Kartennummer` | die **Zuordnung selbst**: aus „AB203" ist der Stand nicht sicher zurückzurechnen — `Kartennummer.Aus` nutzt Mindestbreite `D2`, und ein Präfix darf selbst Ziffern tragen (`AB2`+`03` und `AB`+`203` ergeben dieselbe Zeichenfolge) |
| **Kontributoren boardweit** gibt es nicht | `Karte.Kontributor` ist eine **Nummer**; `GET /api/kontributoren` ist **installationsweit** (Migration 006) | die Lesbarkeit: eine Datei mit „Verantwortlich: 7" ist weder für einen Menschen lesbar noch für `I0039` auflösbar |

Und der **Gegenstand selbst fehlt**: es gibt vier Antworten an vier Adressen, aber **kein Dokument**. Wer heute ein Board sichern, weitergeben oder auf einer anderen Installation wieder aufbauen will, muss vier Abrufe von Hand zusammenheften und die drei Lücken selbst schließen — was er nicht kann, weil zwei davon in keiner Antwort stehen.

Der Wert ist damit doppelt: ein **abgeschlossenes Board wird ablegbar**, ohne dass sein Inhalt in der Datenbank aller Boards liegen bleibt, und `I0039` bekommt den Gegenstand, den er einlesen soll. Der Slice ist klein — aber er ist **kein Zusammenkopieren vorhandener Antworten**.

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/export.json` liefert **eine** Datei, die das ganze Board trägt, als Download mit sprechendem Namen.
- Die Datei trägt **einen Kopf** mit Fassungsnummer, Erzeugungszeitpunkt, dem Namen der Anwendung und dem Satz, dass die Bytes der Anhänge nicht mitreisen.
- Die Datei trägt das **Board** mit Art, Terminen, Kartenzahlanzeige und Archivstand, seine **Spalten** mit Position, Abschlussmarke und Anzeigegrenze, seine **Kartenklassen** mit Präfix und Zählerstand.
- Die Datei trägt die **referenzierten** Kontributoren mit Name, Art und Stilllegung — aus Karte, Kommentar, Anhang, Dateiverweis und Zeiteintrag —, **nicht** die Personenliste der Installation.
- Die Datei trägt **alle** Karten samt Ort, Archivmarke, Klassenzuordnung **mit Zählerstand**, **Sollband** und den fünf Listen (Etiketten, Teilaufgaben, Kommentare, Anhänge als Metadaten, Dateiverweise) — **keine Kürzung, kein Archivfilter, keine Seitengröße**.
- Die Datei trägt **alle** Zeiteinträge des Boards, laufende ohne Ende.
- Ein **unbekanntes** Board wird mit Grund, Werten und Kompensationsaktion zurückgewiesen; ein **leeres** Board ergibt eine vollständige Datei mit leeren Listen.
- Das ⋯-Menü einer Boardkachel führt als **dritten** Punkt `Exportieren`; ein Klick lädt die Datei, ohne die Seite zu verlassen.
- Der Punkt steht **auch an einer archivierten Kachel** — ein abgelegtes Board bleibt ausleitbar.

## Nicht-funktionale Anforderungen

- **Kein zweiter Weg zu denselben Daten.** Die Datei entsteht aus **denselben Lesern** wie die Rohdatenrouten, in **einer** Lesetransaktion — nicht aus einem eigenen Satz Abfragen und nicht aus HTTP-Aufrufen auf die eigene API.
- **Ein Lesevorgang je Sorte, nicht je Karte.** Die drei neuen Leseweiten sind je **eine** Boardabfrage; das N+1 wird nicht vom Aufrufer in den Server verschoben.
- **Die Bytes der Anhänge reisen nicht mit** — Base64 machte aus einem 50-MB-Anhang eine 67-MB-Textzeile. Die Lücke wird **im Kopf benannt**, nicht verschwiegen.
- **Die Datei entsteht im Speicher und geht in einem Stück heraus.** Bei einem Board der Größenordnung aus `I0030` (431 Karten) sind das einige Megabyte — dieselbe Grenze, die `R00039` schon genannt hat, und dieselbe Antwort: eine Größe, keine Kürzung.
- **Der Browser holt die Datei selbst**, über einen Verweis direkt auf die WebApi — kein JS-Interop, kein Blob, kein Fluss durch den SignalR-Kreislauf.
- Gestaltungswerte ausschließlich aus `gestaltung.css` — keine Farb-, Abstands- oder Radiusliterale.
- **Keine Projektreferenz `KanbanC.Blazor` → `KanbanC.BL`** (Kernregel des Projekts).

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Ein einzelnes Board wird als eigenstaendige Datei herausgeschrieben; sie enthaelt Board, Spalten, Karten, Klassenzuordnungen und Zeiteintraege vollstaendig und ist ohne die Anwendung lesbar."*

Das Kriterium trägt **einen Gegenstand** (die Datei), **fünf Inhalte** (Board, Spalten, Karten, Klassenzuordnungen, Zeiteinträge) und **zwei Zusagen** (*vollständig*, *ohne die Anwendung lesbar*). Alle drei sind unten in einzeln prüfbare Sätze zerlegt.

### Das durchgehende Rechenbeispiel

Board 2 „KanbanC — Release 2", Projektboard mit Zieltermin, Bahnen „Bereit", „In Arbeit" und **„Erledigt" als Abschlussspalte mit Anzeigegrenze 20**. Darauf:

| | Lage | Zweck im Beispiel |
|---|---|---|
| `K1`–`K21` | **21** erledigte Karten in „Erledigt" | eine mehr als die Anzeigegrenze |
| `K22` | eine **archivierte** Karte | `GET /api/boards/2` lässt sie weg |
| `K23` | eine Karte **ohne Kartenklasse** und **ohne Sollband** | die Leerfälle beider neuer Leseweiten |
| `K24` | Karte der Klasse `WBS-` mit **Zählerstand 32**, **Sollband 2,0–4,0 h**, Verantwortlichem `Stefan` (7) und je einem Eintrag in **allen fünf Listen** | der volle Fall |
| `Z1` | abgeschlossener Zeiteintrag auf `K24`, Kontributor `Claude-Agent` (9) | Zeiteintrag mit Ende |
| `Z2` | **laufender** Zeiteintrag auf `K22` (archiviert), Kontributor `Alt-Kollege` (11, **stillgelegt**) | laufend, archiviert, stillgelegt |

- [ ] `GET /api/boards/2/export.json` antwortet mit **200**, `Content-Type: application/json` und einem Dateinamen im `Content-Disposition` — der Aufruf liefert eine **Datei**, keine nackte Antwort.
- [ ] Die Datei enthält **24** Karten, **2** Zeiteinträge, **3** Spalten, **1** Kartenklasse und **3** Kontributoren (`Stefan`, `Claude-Agent`, `Alt-Kollege`) — nicht mehr und nicht weniger.
- [ ] `GET /api/boards/999/export.json` → **404**, Code `board-unbekannt`, Meldung mit der Nummer **999**, Kompensation `GET /api/boards` abrufen.
- [ ] Ein Board **ohne jede Karte** ergibt **200** und eine vollständige Datei: Kopf, Board, Spalten, leere Listen.

### „Vollständig" — die fünf Inhalte des Kriteriums

- [ ] **Board**: Name, Art (Projekt), Starttermin, Zieltermin, Kartenzahlanzeige und Archivstand stehen in der Datei.
- [ ] **Spalten**: alle drei mit `SpalteId`, Bezeichnung, Position, Abschlussmarke und **Anzeigegrenze 20** — die Grenze wird **berichtet**, nicht **angewendet**.
- [ ] **Karten**: alle **24** — `K1`–`K21` vollständig (nicht 20), `K22` **mit Archivmarke**, `K23` mit `Kartenklasse: null`, `K24` mit allen fünf Listen; Anhänge **als Metadaten** (AnhangId, Dateiname, Größe, Urheber, Zeitpunkt).
- [ ] **Klassenzuordnungen**: an `K24` stehen die Kartenklasse **und der Zählerstand 32** — als eigener Wert **neben** der fertigen Nummer `WBS-32`, nicht nur in ihr. Gegenprobe der Notwendigkeit: bei Präfix `AB2` und Stand 3 ergibt `Kartennummer.Aus` „AB203", ebenso bei Präfix `AB` und Stand 203 — aus der Nummer allein ist der Stand **nicht** rückgewinnbar.
- [ ] **Zeiteinträge**: beide, `Z1` mit Ende, `Z2` ohne (`Ende: null`) und auf einer archivierten Karte von einem **stillgelegten** Kontributor — ihre Arbeit steht in der Datei.
- [ ] **Sollband**: `K24` trägt `2,0–4,0`, `K23` trägt `null` — **kein Ersatzwert**, wo die Zeile fehlt.
- [ ] **Gegenprobe zur Vollständigkeit**: `GET /api/boards/2` liefert für „Erledigt" **weiterhin 20** Karten und **ohne** `K22`. Aus dem Export ist keine Änderung der Anzeige geworden.

### „Ohne die Anwendung lesbar" — drei Proben und eine Gegenprobe, alle an derselben Datei

- [ ] **Probe 1 — parsbar ohne die Anwendung.** Die Datei parst mit `JsonDocument` **ohne einen einzigen KanbanC-Typ**: kein Contracts-Verweis, kein Deserialisierer, keine Kenntnis des Schemas.
- [ ] **Probe 2 — kein Verweis ins Leere.** **Jede** Nummer, auf die eine Zeile zeigt — Spalte einer Karte, Kartenklasse einer Karte, Kontributor an Karte, Kommentar, Anhang, Dateiverweis und Zeiteintrag, Karte eines Zeiteintrags —, steht als Zeile **in derselben Datei**. Geprüft wird die Menge, nicht ein Beispiel.
- [ ] **Probe 3 — lesbar, nicht nur auflösbar.** **Neben jeder Nummer steht im selben Dokument ihr Name**: die Spalte mit Bezeichnung, die Kartenklasse mit Name und Präfix, der Kontributor mit Name. Ein Mensch, der die Datei öffnet, liest „Stefan" und nicht „7".
- [ ] **Gegenprobe** im selben Test: die **archivierte** und die **weggekürzte** Karte stehen darin, während `GET /api/boards/{boardId}` weiter kürzt.
- [ ] **Keine Zeile der Datei behauptet etwas, was die Datei selbst widerlegt.** Konkret: **kein Zeiteintrag steht zweimal** darin, und **keine Zahl neben einer Spalte nennt eine Kartenzahl, die nicht der Zahl ihrer Karten in der Datei entspricht**. Eine „Kartenzahl: 0" neben 24 Karten wäre genau die stille Lüge, die `Spalte.Kartenzahl` ausdrücklich verhindern soll.
- [ ] **Der Kopf sagt die Lücke.** Fassungsnummer, Erzeugungszeitpunkt, Name der Anwendung und der Satz, dass die **Bytes** der Anhänge nicht mitreisen, stehen als Text in der Datei — nicht in dieser Anforderung allein.

### Die Boarddatei über die API (`F0070`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/export.json` liefert **eine** Datei, die das ganze Board trägt: das Board mit Art, Terminen, Kartenzahlanzeige und Archivstand, seine Spalten mit Position, Abschlussmarke und Anzeigegrenze, seine Kartenklassen mit Präfix und Zählerstand, die referenzierten Kontributoren mit Name, Art und Stilllegung, **alle** Karten samt Ort, Archivmarke, Klassenzuordnung mit Zählerstand, Sollband und den fünf Listen, und **alle** Zeiteinträge; dazu einen Kopf mit Fassungsnummer, Erzeugungszeitpunkt und der Angabe, dass die Bytes der Anhänge nicht mitreisen. Keine Kürzung, kein Archivfilter, keine Seitengröße; ein unbekanntes Board wird mit Grund, Werten und Kompensationsaktion zurückgewiesen. Ohne Schirm allein an der Antwort prüfbar."*

- [ ] **Die Endung steht im Pfad** (`export.json`), dieselbe Adressform wie `zeitexport.csv`; kein Konflikt mit `…/karten` und `…/zeiten`.
- [ ] Die Auslieferung geht über `Results.File` mit `application/json` — derselbe Weg wie Anhang-Download und Zeitexport. **Dieselbe Route bedient Browser-Download und Agenten-Abruf.**
- [ ] Der Dateiname nennt **Board und Tag** (`<board>-<datum>.kanbanc.json`), damit zwei Ausleitungen desselben Boards nebeneinander liegen können. Zeichen, die ein Dateisystem nicht trägt, werden **ersetzt, nicht weggelassen**: ein Board „Release 1/2" und ein Board „Release 12" dürfen nicht denselben Namen bekommen.
- [ ] Es gibt **keine Seitengröße, keinen Archivfilter, keinen Zeitraum, keinen Kartenklassenausschnitt** — kein Abfrageparameter ändert den Inhalt der Datei.
- [ ] Der Fehlervertragstest (`FehlervertragTests`, aus `B0102`) nimmt die Route auf — sonst schlägt `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` fehl.
- [ ] **Ohne Schirm prüfbar**: jedes Kriterium dieser Gruppe ist an der Antwort allein zu zeigen.

### Der Menüpunkt an der Kachel (`F0071`)

Fertig-Kriterium wörtlich: *„Das ⋯-Menü einer Boardkachel führt als dritten Punkt „Exportieren"; ein Klick lädt die Datei mit ihrem Namen in den Browser, ohne die Seite zu verlassen. Der Punkt steht auch an einer archivierten Kachel — ein abgelegtes Board bleibt ausleitbar."*

- [ ] Das Menü führt **drei** Punkte: `Umbenennen`, `Archivieren` (bzw. an der Archivansicht nur `Umbenennen`) und **`Exportieren`** mit Pfeil-nach-unten-Symbol.
- [ ] Der Punkt ist ein **`<a href>`** und kein `<button>`: der Browser holt die Datei selbst, damit sie nicht durch den Blazor-Kreislauf fließt und der Download Name, Fortschritt und Abbruch behält.
- [ ] Der Verweis zeigt **direkt auf die WebApi** (öffentliche Basisadresse), nicht auf eine Blazor-Route.
- [ ] Ein Klick **verlässt die Seite nicht**: die Boardliste bleibt stehen, das Menü schließt wie bei den zwei vorhandenen Punkten.
- [ ] Der Punkt steht **auch an der archivierten Kachel** — dort, wo `Archivieren` fehlt und `zurückholen` steht.
- [ ] Die geladene Datei trägt **denselben Namen**, den die Route im `Content-Disposition` nennt.
- [ ] **Kein Gestaltungsliteral** in der Menüzeile; Werte aus `gestaltung.css`.
- [ ] **Keine Ausfallmeldung am Menüpunkt**: eine nicht erreichbare WebApi meldet der Browser selbst — `WebApiAufruf.MitAusfallmeldung` gehört zu Aufrufen, die die Anwendung macht.

### Der grüne Bestand bleibt grün

- [ ] `GET /api/boards/{boardId}` bleibt **unverändert** die Anzeige: gekürzt, ohne archivierte Karten, ohne die n-Listen.
- [ ] `GET /api/boards/{boardId}/karten` und `…/zeiten` bleiben unverändert; die Exportdatei nutzt **dieselben** Leser, statt sie umzubauen.
- [ ] `Rohdatenkarte` bleibt, wie sie ist — die Exportkarte **setzt zusammen**, statt zu verdoppeln.
- [ ] **Keine Migration, keine Schemaänderung, kein Schreibweg.** Alle 19 Tabellen werden nur gelesen.
- [ ] `Kartennummer.Aus` bleibt unverändert; der Zählerstand reist **daneben**, statt die Nummer umzubauen.
- [ ] Der Zeitexport (`I0036`) und die Anhangroute bleiben unberührt.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Import** — `I0039` liest die Datei, dieser Slice schreibt sie. Keine Dateiwahl, keine Vorschau, kein Bericht: der Ablauf, den das Artboard als Lücke markiert, gehört dorthin.
- [ ] **Kein Archiv aus JSON und Dateien**, keine Anhangbytes, kein ZIP — das wäre keine „eigenständige Datei" mehr, sondern ein eigener Slice.
- [ ] **Keine Kopie der SQLite-Datei** — sie wäre ohne Werkzeug unlesbar und trüge das Schema statt des Boards.
- [ ] **Kein Export mehrerer Boards**, kein Gesamtexport der Installation, keine Personenliste der Installation.
- [ ] **Kein eigener Schirm, kein Dialog, kein Fortschrittsbalken.** Ein Export ist ein Klick und eine Datei.
- [ ] **Kein Strom, kein Chunking, keine Kompressionsverhandlung, kein ETag.**

## Betroffene Verzeichnisstruktur

Ein neuer Themenordner kommt hinzu: **`Export`** — in Contracts, BL und Tests. Der Rest wächst an vorhandenen Stellen.

- **Contracts**: `KanbanC.Contracts/Export` — `Boardexport`, `Exportkopf`, `Exportboard`, `Exportspalte`, `Exportkarte` (alle immutable, C08).
- **BL**: `KanbanC.BL/Persistenz/Karten/Sollzeitleser.cs` (die Sollbänder des Boards — die Zeile hängt an der Karte, nicht an einer Auswertung), `KanbanC.BL/Persistenz/Kontributoren/Kontributorenleser.cs` (die referenzierten Kontributoren), Erweiterung von `KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (der Zählerstand verlässt die Leseform getrennt), `KanbanC.BL/Interfaces/Export` (`IBoardexportRepository`), `KanbanC.BL/Persistenz/Export` (`BoardexportRepository`), `KanbanC.BL/Integrations/Export` (`BoardexportService`), `KanbanC.BL/Operations/Export` (`Exportdateiname`).
- **API**: `KanbanC.WebApi/Endpunkte/ExportEndpunkte.cs` — **erster Endpunktsatz dieses Themas**, eine Route; registriert in `Program.cs` wie `RohdatenEndpunkte`.
- **Oberfläche**: `KanbanC.Blazor/Services/Exportadresse.cs` (pure Rechnung, Muster `Anhangadresse`/`Zeitexportadresse`), `KanbanC.Blazor/Components/Boards/Boardkachel.razor` (+ `.razor.css`). **Kein API-Klient** — der Browser holt die Datei selbst. **Keine Projektreferenz auf `KanbanC.BL`.**
- **Tests**: `KanbanC.BL.Tests/Integrations/Export` und `Operations/Export`, `KanbanC.WebApi.IntegrationTests/{Api,Persistenz/Export}`, `KanbanC.Blazor.Tests/Services` (`ExportadresseTests`) und `…/Gestaltung`, `KanbanC.PlaywrightTests` (`BoardkachelMenueE2ETests`, Seitenobjekt `BoardsSeite`).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Warum eine Datei und nicht vier Antworten

**Geprüft, nicht angenommen.** Der grüne Bestand liefert 16 der 19 boardbezogenen Tabellen — aber an **vier** Adressen, in vier Antworten, ohne die drei Lücken und ohne einen Kopf, der sagt, was fehlt. Ein Aufrufer, der ein Board sichern will, müsste vier Abrufe zusammenheften, ihre Konsistenz selbst herstellen (sie stammen aus vier Transaktionen) und die drei Lücken schließen, die in keiner Antwort stehen. **Die Datei ist der Gegenstand, den es nicht gibt** — und `I0039` braucht genau ihn, nicht vier Ströme.

### Die Form: JSON, nicht die Datenbank

Eine Kopie der SQLite-Datei wäre ohne Werkzeug unlesbar und trüge **das Schema statt des Boards**; ein CSV-Satz träfe die Schachtelung nicht (fünf n-Listen je Karte). JSON ist die Form, an der die drei Proben oben überhaupt prüfbar sind. Die Endung steht im **Pfad** (`export.json`), weil die Anwendung dieselbe Adressform schon führt (`zeitexport.csv`) — eine Ressource, deren Gestalt man an ihrer Adresse liest.

### Drei neue Leseweiten, je eine Boardabfrage

- **`Sollzeitleser.LiesSollbaenderDesBoards`** — `Kartensollzeit` über `Karte JOIN Spalte` auf das Board eingegrenzt, je `KarteId` ein `Zeitband`. **Eine** Boardabfrage, nicht eine je Karte; derselbe Schnitt wie die fünf n-Leser aus `R00039`. Fehlt die Zeile, fehlt das Band — kein Ersatzwert.
- **`Kontributorenleser.LiesKontributorenDesBoards`** — **fünf Herkünfte in einer Abfrage**: `Karteneigenschaft.Kontributor`, die Urheber von `Kartenkommentar`, `Kartenanhang` und `Kartendateiverweis` und `Zeiteintrag.Kontributor`, je über `Karte JOIN Spalte` auf das Board eingegrenzt und **vereinigt**. Stillgelegte fallen **nicht** heraus — dieselbe Regel, die `Zeitenleser` schon führt.
- **Der Zählerstand aus dem `Kartenleser`** — die Spalte `z.Zaehlerstand` steht **bereits** in `LiesRohdatenkartenDesBoards` (`Kartenleser.cs:55`) und geht heute allein in `Kartennummer.Aus`. Sie muss **zusätzlich** herauskommen: das interne Zeilenmodell `Rohdatenkartenlage` bekommt den Wert daneben. **Kein Contracts-Umbau** — `Rohdatenkarte` bleibt unverändert, der Wert hängt sich an der Exportkarte an.

SQL nach Skill `sql-stil`: Fluss-Ausrichtung, explizite Spalten, Schlüsselwörter groß.

### Eine Lesetransaktion, kein zweiter Weg

Der `BoardexportRepository` liest **alle** Sorten in **einer** Verbindung und **einer** Transaktion — Vorbild ist `RohdatenRepository`, das dieselbe Regel mit derselben Begründung führt: ohne gemeinsame Transaktion käme eine Karte, die zwischen der ersten und der letzten Abfrage entsteht, mit leeren Listen zurück, und die Datei wäre stillschweigend unvollständig. **Kein HTTP-Aufruf auf die eigenen Rohdatenrouten** — das wäre ein zweiter Weg zu denselben Zeilen und zugleich ein zweiter Konsistenzstand.

Die Vorprüfung sitzt **vor** dem Lesen und ist dieselbe wie im `RohdatenService`: Board unbekannt → `Nichtgefunden.Board(boardId)`, das Codes, Nummer im Klartext und Kompensationsaktion schon trägt. Ein **leeres** Board ist kein Fehler.

### Die Verträge: Zusammensetzung, wo sie trägt — ein eigener Typ, wo sie lügen würde

**Die Karte wird zusammengesetzt**: `Exportkarte` ist `Rohdatenkarte` plus `Zeitband? Sollband` und `int? Zaehlerstand`. Ein zweiter Kartentyp liefe bei der nächsten Ergänzung auseinander — dieselbe Regel, die `Klassenkarte` und `Rohdatenkarte` schon führen.

**Board und Spalte werden nicht wiederverwendet**, und das ist eine begründete Abweichung vom Bubble-Entwurf (`B0535` schlägt `Spaltenleser.LiesSpaltenNachPosition` mit leerer Kartenzuordnung vor). Der Grund steht in den Verträgen selbst:

| Wiederverwendeter Typ | Was er mitbrächte | Warum das die Datei beschädigt |
|---|---|---|
| `Spalte` | `IReadOnlyList<Karte> Karten` **und** `int Kartenzahl` | mit leerer Zuordnung stünde **„Kartenzahl: 0"** neben 24 Karten — genau die stille Lüge, gegen die der Kommentar an `Spalte` geschrieben wurde; mit gefüllter Zuordnung stünde **jede Karte zweimal** in der Datei |
| `Board` | `IReadOnlyList<Zeiteintrag> LaufendeZeiteintraege` | entweder **doppelte** Zeiteinträge (auch in der Zeitenliste) oder eine **leere** Liste, obwohl ein Timer läuft |

Deshalb tragen `Exportboard` und `Exportspalte` nur, was sie wirklich wissen: das Board ohne Zeitenliste, die Spalte mit Position, Abschlussmarke und **Anzeigegrenze** — die in der Datei **berichtet**, nicht **angewendet** wird. Der Ort jeder Karte steht **an der Karte** (`Rohdatenkarte.Spalte` samt Bezeichnung), also braucht die Spalte keine Kartenliste. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.**

### Für `I0039` gebaut, ohne ihn vorwegzunehmen

- Jede Zeile behält ihre **ursprüngliche Nummer** — die Datei ist ein Abbild, kein frischer Satz.
- Jeder Verweis zeigt auf eine Nummer **in derselben Datei**.
- Der `Zaehlerstand` reist **getrennt** von der fertigen `Kartennummer`.
- Der Kopf trägt eine **Fassungsnummer**, gegen die `I0039` prüft, bevor er etwas schreibt.
- Die Reihenfolge (Kopf, Board, Spalten, Kartenklassen, Kontributoren, Karten, Zeiteinträge) ist zugleich eine **gangbare** Schreibreihenfolge; **verbindlich ist sie nicht** — das entscheidet der Import.

### Der Downloadweg existiert schon

`Results.File(...)` liefert die Datei; ein **nacktes `<a href>`** direkt auf die WebApi holt sie in den Browser. Beides ist geübtes Gelände: `Anhangadresse` und `Zeitexportadresse` sind pure Operationen derselben Bauform, `WebApibasisadresse` trägt die Adresse, die der **Browser** sieht, und `RunAndWaitForDownloadAsync` steht in zwei E2E-Tests. **Kein JS-Interop, kein Blob, kein `WebApiAufruf`.**

### Ablauf

1. **Der Mensch** öffnet das ⋯-Menü einer Boardkachel und klickt `Exportieren`.
   - 1.1 Der Punkt ist ein `<a href="@Exportadresse.Fuer(WebApibasis.Adresse, Board.BoardId)">`; das Menü schließt.
   - 1.2 Der **Browser** ruft die Adresse selbst — die Seite bleibt stehen.
2. **Oder der Agent** ruft `GET /api/boards/{boardId}/export.json` direkt. **Dieselbe Route.**
3. `ExportEndpunkte` → `BoardexportService.Datei(boardId)`
   - 3.1 Vorprüfung: Board unbekannt → `Nichtgefunden.Board(boardId)` → **404** mit Grund, Werten, Kompensationsaktion
4. **Eine Lesetransaktion** über alle Sorten
   - 4.1 Board, Boardeinstellung, Boardarchivierung; Spalten nach Position
   - 4.2 Kartenklassen des Boards mit Präfix und Zählerstand
   - 4.3 Rohdatenkarten des Boards (Karte, Ort, Archivmarke, Kartenklasse, **Zählerstand**) und die fünf n-Listen
   - 4.4 Sollbänder des Boards je `KarteId`
   - 4.5 die referenzierten Kontributoren
   - 4.6 alle Zeiteinträge des Boards
5. **Der Zusammenbau** setzt Sollband und Zählerstand an ihre Karte und schreibt den `Exportkopf` (Fassung, Zeitpunkt, Anwendung, Anhanghinweis).
6. `Exportdateiname.Fuer(boardname, tag)` → `<board>-<datum>.kanbanc.json`
7. **200** über `Results.File(bytes, "application/json", dateiname)`.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** der neue Endpunktsatz `ExportEndpunkte` in `Program.cs` (WebApi); das ⋯-Menü in `Boardkachel.razor`; die vorhandenen Leser `Kartenleser`, `Kartenklassenleser`, `Zeitenleser` und `BoardRepository` in `KanbanC.BL/Persistenz`.

**In `KanbanC.Contracts/Export`** (immutable, C08):
- `Boardexport` (DTO) — das ganze Board als **ein** Gegenstand.
  - `Boardexport(Exportkopf Kopf, Exportboard Board, IReadOnlyList<Exportspalte> Spalten, IReadOnlyList<Kartenklasse> Kartenklassen, IReadOnlyList<Kontributor> Kontributoren, IReadOnlyList<Exportkarte> Karten, IReadOnlyList<Zeiteintrag> Zeiteintraege)`
- `Exportkopf` (DTO) — was die Datei über sich selbst sagt, **einschließlich der Lücke**.
  - `Exportkopf(string Anwendung, int Fassung, DateTimeOffset ErzeugtAm, string Anhanghinweis)`
- `Exportboard` (DTO) — Name, Art, Termine, Kartenzahlanzeige, Archivstand. **Ohne Zeitenliste**, weil die Zeiteinträge ihre eigene Liste haben.
- `Exportspalte` (DTO) — `SpalteId`, Bezeichnung, Position, Abschlussmarke, Anzeigegrenze. **Ohne Kartenliste und ohne Kartenzahl**; der Ort steht an der Karte.
- `Exportkarte` (DTO) — Zusammensetzung: `Exportkarte(Rohdatenkarte Karte, Zeitband? Sollband, int? Zaehlerstand)`

**In `KanbanC.BL/Persistenz/Karten`:**
- `Sollzeitleser` (Provider) — die Sollbänder eines Boards in **einer** Abfrage.
  - `IReadOnlyDictionary<long, Zeitband> LiesSollbaenderDesBoards(IDbConnection, IDbTransaction?, long boardId)`
- `Kartenleser` — die vorhandene Rohdaten-Leseform gibt den `VergebenerZaehlerstand` **zusätzlich** heraus (internes Zeilenmodell `Rohdatenkartenlage`).

**In `KanbanC.BL/Persistenz/Kontributoren`:**
- `Kontributorenleser` (Provider) — die **referenzierten** Kontributoren eines Boards, fünf Herkünfte vereinigt, Stillgelegte inbegriffen.
  - `IReadOnlyList<Kontributor> LiesKontributorenDesBoards(IDbConnection, IDbTransaction?, long boardId)`

**In `KanbanC.BL/Interfaces/Export` und `KanbanC.BL/Persistenz/Export`:**
- `IBoardexportRepository` / `BoardexportRepository` (Integration, Ressourcenzugriff) — **eine** Lesetransaktion über alle Leser; `null` heißt „dieses Board gibt es nicht".
  - `Boardexport? LiesBoardexport(long boardId)`

**In `KanbanC.BL/Operations/Export`:**
- `Exportdateiname` (Operation, pure Rechnung; Muster `Zeitexportname`) — Board und Tag im Namen; nicht tragbare Zeichen werden **ersetzt**.
  - `string Fuer(string boardname, DateOnly tag)`

**In `KanbanC.BL/Integrations/Export`:**
- `BoardexportService` (Integration, fängt/loggt) — Vorprüfung, Zusammenbau, Kopf.
  - `Ergebnis<Boardexport> Datei(long boardId)`

**In `KanbanC.WebApi/Endpunkte`:**
- `ExportEndpunkte` (Integration) — `GET /api/boards/{boardId}/export.json`; 200 über `Results.File`, 404 über `Zurueckweisungen.AlsFehlerantwort`.

**In `KanbanC.Blazor/Services`:**
- `Exportadresse` (Operation in der Oberflächenschicht, pure Rechnung) — die Basisadresse kommt als **Parameter**, damit die Rechnung ohne Browser und ohne Konfiguration prüfbar bleibt.
  - `string Fuer(string oeffentlicheBasisAdresse, long boardId)`

**Kein Interface** für die zwei neuen Leser: je Aufgabe genau eine Implementation (C25). **Keine tote Flexibilität** (C24): kein `ExportApiKlient` ohne Aufrufer.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Kartenleser` | die Rohdaten-Leseform gibt den `VergebenerZaehlerstand` getrennt heraus; die Abfrage bleibt, wie sie ist |
| `Boardkachel.razor` (+ `.razor.css`) | dritter Menüpunkt `Exportieren` als `<a href>`; steht auch in der Archivansicht |
| `Program.cs` (WebApi) | `ExportEndpunkte` registrieren, `BoardexportService` und `BoardexportRepository` verdrahten |
| `FehlervertragTests` | nimmt die neue Route auf |
| `BoardsSeite` (E2E-Seitenobjekt) | wächst um den Menüpunkt |

**Nicht geändert:** `RohdatenRepository`, `RohdatenService`, `RohdatenEndpunkte`, `Rohdatenkarte`, `BoardService`, `Abschlussbahn`, `Kartennummer`, `AuswertungsService`, `Zeitenleser`, `Kartenklassenleser`, alles unter `Persistenz/Migrationen/`.

### Wireframe

`Dokumentation/Wireframes/D0001.dc.html:181`, **Zustand 1 und 5** (offenes ⋯-Menü der Boardkachel) ist der **Verweis für die Gestaltung** von `F0071`: der Punkt `Exportieren` mit Pfeil-nach-unten-Symbol unter einem Trennstrich, dazu die Markierung „`I0005 · I0038` — Zielform, noch nicht gebaut". Das Bild ist **Entwurfsquelle, nie Kriterienquelle**; kein Akzeptanzkriterium dieser Anforderung ist aus ihm abgeleitet. Den Ablauf, den das Artboard als **Lücke** markiert (Dateiwahl, Vorschau, Bericht), braucht dieser Slice nicht — er gehört `I0039`.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md`; jeder Test verifiziert eine echte Zustandsänderung (Skill `test-ehrlichkeit`).

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Exportdateiname.Fuer` — Slug ohne echte Umlaute, nicht tragbare Zeichen **ersetzt** statt weggelassen („Release 1/2" ≠ „Release 12"), Tag im Namen, Endung `.kanbanc.json`, namenloses Board.
- `BoardexportService.Datei` gegen ein **Test-Repository** — unbekanntes Board → `Nichtgefunden.Board`; das Zusammensetzen von Sollband und Zählerstand an die **richtige** Karte; das leere Board mit vollständigem Kopf und leeren Listen; der Kopf trägt Fassung und Anhanghinweis.

**`KanbanC.Blazor.Tests`:** `ExportadresseTests` — absolute Adresse direkt auf die WebApi, Basis mit und ohne Schrägstrich am Ende. Dazu die Gestaltungsprüfung der Menüzeile (kein Farb-, Abstands- oder Radiusliteral).

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `Sollzeitleser.LiesSollbaenderDesBoards` — Band je Karte, **kein Ersatzwert** ohne Zeile, fremde Boards draußen, **eine** Abfrage.
- `Kontributorenleser.LiesKontributorenDesBoards` — alle **fünf** Herkünfte werden gefunden, jeder Kontributor **einmal**, Stillgelegte bleiben drin, die Personen ohne Bezug zum Board bleiben **draußen**.
- `BoardexportRepository.LiesBoardexport` — Zählerstand neben der Kartennummer, alle Karten inklusive archivierter, Ordnung nach Lage im Board, fremder Bestand draußen.
- Die Route: **200** als Datei mit `Content-Disposition` und dem gerechneten Namen; **404** mit Code, Werten und Kompensationsaktion; `FehlervertragTests` erweitert.
- **Der Beweis, dass die Datei ohne die Anwendung lesbar ist** (nur Test, kein Produktionscode — Muster `B0525` und `B0017`): ein Board mit archivierter Karte, mehr erledigten Karten als die Anzeigegrenze, Klasse, Sollband, allen fünf Listen und Zeiteinträgen; die Datei wird **allein mit `JsonDocument`** geprüft — drei Proben (parsbar ohne KanbanC-Typ, jede Nummer in derselben Datei, jeder Name daneben) plus die Gegenprobe gegen `GET /api/boards/{boardId}`.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — Board mit Karte, Kartenklasse und Zeiteintrag aufbauen, ⋯-Menü öffnen, `Exportieren` klicken, den Download über `RunAndWaitForDownloadAsync` annehmen und **den Inhalt der geladenen Datei gegen den Aufbau halten**. Ein Download mit dem richtigen Namen und falschem Inhalt wäre schlimmer als keiner.

## Abhängigkeiten

- Abhängig von (alle **grün**): **`R00006`** (`I0011` — Karte anlegen), **`R00023`** (`I0021` — Karte einer Klasse zuordnen, liefert `Kartenklassenzuordnung` samt Zählerstand), **`R00027`** (`I0024` — Timer stoppen, liefert Zeiteinträge mit Ende). Das sind die drei `Braucht` der Interaction.
- `F0070` braucht zusätzlich **`R00039`** (`I0037` — Rohdaten über die API, **grün**): dessen Leser und `Rohdatenkarte` sind die Grundlage, auf der die Datei entsteht.
- Setzt außerdem auf (alle grün): **`R00010`** (`I0005` — Archivansicht der Kachel, das ⋯-Menü und der Punkt an der archivierten Kachel), **`R00016`**–**`R00021`** (Archivmarke und die fünf Listen), **`R00022`** (`I0020` — Kartenklassen mit Präfix und Zählerstand), **`R00033`** (`I0030` — der WBS-Import schreibt `Kartensollzeit`), **`R00038`** (`I0036` — das Muster `Results.File` plus `<a href>` plus Adressoperation), **`R00005`** (Gestaltungstokens).
- `F0071` braucht **`F0070`** — sonst zeigte der Verweis auf eine Adresse, die nicht antwortet.
- **Blockiert: `I0039`** (Board importieren) — der einzige Knoten der WBS, der `I0038` in seiner `Braucht`-Spalte führt. Er liest die Datei, die hier entsteht.
- **`D0001` wird mit diesem Slice nicht grün** — `I0039` bleibt rot.

## Umfang

```
Board exportieren (I0038) = 12 Bubbles: 12 Standard (14,4h), 0 unklar.
Rest: 14,4h klar · 0 von 12 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0038` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **zwei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0070` Die Boarddatei über die API | `B0531`–`B0539` (9) | 9 (10,0h) | 0 | `I0011`, `I0021`, `I0024`, `I0037` |
| `F0071` Exportieren im ⋯-Menü der Kachel | `B0540`–`B0542` (3) | 3 (4,4h) | 0 | `F0070` |

**Warum zwei Features:** weil zwei Aspekte **getrennt fertig** werden. `F0070` ist ohne Schirm allein an der Antwort prüfbar und könnte vollständig sein, während die Kachel noch kein Menü dafür hat; `F0071` macht die Fähigkeit im Browser greifbar.

**Wo der Aufwand liegt:** in den drei Lücken (`B0532`–`B0534`, davon `B0534` allein mit 2,0h wegen der fünf vereinigten Herkünfte), im Zusammenbau (`B0536`), in der Route (`B0538`) und in den zwei Beweisen (`B0539` ohne die Anwendung, `B0542` durch den Browser). **Keine Bubble trägt eine Bandbreite** — das Muster ist an jeder Stelle bekannt: `Results.File`, `<a href>` und `RunAndWaitForDownloadAsync` stehen im Bestand.

**Nach gemessenem Durchsatz ist mit etwa 0,7–1,5 h zu rechnen** (`Schaetzungen/_ist-zeiten.md`: vergleichbare Slices lagen bei 0,7–2,4 h gegen 3–44 h geplant). Die Richtwert-Konvention seit `I0004` überschätzt messbar; sie wird **nicht still gekippt** — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0038` und an beiden Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00039`/`I0037`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **`Kartensollzeit` reist mit, obwohl das Fertig-Kriterium sie nicht aufzählt.** Das Kriterium nennt Board, Spalten, Karten, Klassenzuordnungen und Zeiteinträge. **Angenommen ist**: das Sollband gehört zur Karte, weil es eine **gespeicherte Zeile** ist (Migration 019, geschrieben vom WBS-Import) und nicht eine gerechnete Größe; ohne es verlöre eine ausgeleitete und wieder eingelesene Karte ihr Band, und kein späterer Lauf gäbe es zurück. **Nicht geprüft**, ob der Mensch das Sollband als reine Auswertungsgröße ansieht — dann fiele `B0532` weg und der Slice wäre eine Bubble kleiner. **Nicht am Menschen geprüft.**
- **Die Fassungsnummer im Kopf ist eine Zugabe des Laufs.** Das Fertig-Kriterium verlangt sie nicht. **Angenommen ist**: eine Datei, die `I0039` später wieder einliest, soll sagen, nach welchen Regeln sie entstand — sonst prüft der Import gegen nichts. **Nicht am Menschen geprüft.**
- **Die Bytes der Anhänge bleiben draußen, die Metadaten reisen, und der Kopf sagt die Lücke.** Base64 machte aus einem 50-MB-Anhang eine 67-MB-Textzeile; dieselbe Entscheidung tragen `Kartendetail` und `Rohdatenkarte` schon. **Nicht geprüft**, ob stattdessen ein Archiv aus JSON **und** Dateien gewünscht ist — das wäre keine „eigenständige Datei" mehr, sondern ein eigener Slice, und `I0039` stellte dann auch die Dateien wieder her. **Nicht am Menschen geprüft.**
- **Nur die referenzierten Kontributoren reisen mit, nicht die Personenliste der Installation.** Ein Board exportiert seinen Inhalt, nicht das Adressbuch daneben. **Nicht geprüft**, ob ein Import auf einer **anderen** Installation dieselben Personen zusammenführen oder neu anlegen soll — das ist eine Frage von `I0039`; die Datei liefert für beide Wege genug (Nummer **und** Name **und** Art **und** Stilllegung). **Nicht am Menschen geprüft.**
- **`Exportboard` und `Exportspalte` statt der vorhandenen `Board`- und `Spalte`-Verträge — Abweichung vom Bubble-Entwurf `B0535`.** Der Grund ist geprüft, nicht stilistisch: `Spalte` trägt `Kartenzahl`, und eine leere Kartenzuordnung schriebe **„Kartenzahl: 0" neben 24 Karten**; `Board` trägt `LaufendeZeiteintraege`, die sonst **doppelt** in der Datei stünden. Beides widerspräche der Zusage „lesbar", weil die Datei sich selbst widerspräche. **Angenommen ist** deshalb ein eigener flacher Vertrag für Board und Spalte, während die Karte weiterhin **zusammengesetzt** wird. Die Alternative wäre, `Spalte` die **wahre** Kartenzahl mitzugeben und die Kartenliste leer zu lassen — das ginge ohne zusätzliche Abfrage, ist aber eine Bedeutung mehr für ein Feld, das an der Boardroute etwas anderes meint. **Nicht am Menschen geprüft.**
- **Der Menüpunkt steht auch an der archivierten Kachel.** Angenommen, weil ein archiviertes Board „aus der Standardliste verschwunden, aber abrufbar" ist (`I0005`) und gerade die Ausleitung eines **abgeschlossenen** Boards der wahrscheinliche Anlass ist. **Nicht geprüft**, ob das Menü dort schlanker sein soll. **Nicht am Menschen geprüft.**
- **JSON als Form.** **Nicht geprüft**, ob eine andere Form gewünscht ist; eine Kopie der SQLite-Datei wäre ohne Werkzeug unlesbar und trüge das Schema statt des Boards, ein CSV-Satz träfe die Schachtelung nicht. **Nicht am Menschen geprüft.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit und liest nur, was ohnehin geschrieben wird.

## Manuelle Nachbereitungstätigkeiten

- Keine. Es ändert sich keine Konfiguration und kein Betriebsweg.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist bei `R00001` entstanden und dort ausdrücklich vermerkt: KanbanC hält **eine** SQLite-Datei für alle Boards, weil Cross-Board-Abfragen sonst ein Merge über N Dateien wären — und hat damit die Portabilität aufgegeben, die eine Datei je Board mitgebracht hätte; die Notiz nennt `I0038`/`I0039` als die Adresse, an der sie zurückkommt. Heute gibt es an dieser Adresse **vier Antworten und kein Dokument**: 16 der 19 boardbezogenen Tabellen reisen bereits, aber verteilt auf vier Abrufe aus vier Transaktionen, und drei Inhalte stehen in **keinem** von ihnen — das Sollband der Karte, der Zählerstand ihrer Klassenzuordnung und die Namen der Kontributoren. Wenn **eine** Route den Bestand eines Boards in **einer** Lesetransaktion zu **einer** JSON-Datei zusammenschreibt, in der jede Nummer eine Zeile derselben Datei trifft und neben jeder Nummer ihr Name steht (X), dann ist ein Board erstmals ein Gegenstand, den man weglegen, weitergeben und ohne die Anwendung lesen kann (Y), und `I0039` bekommt genau das, was er einlesen soll — statt eines Bündels von Antworten, dessen Konsistenz niemand zusichert (Z). Der Hebel sitzt hier und nicht vorgelagert: an der Datenhaltung fehlt nichts, jede Zeile steht seit `I0011` bis `I0030` in der Datenbank, und drei der Lücken sind **Lesewege**, die es nicht gibt. Und er sitzt nicht nachgelagert beim Import: `I0039` kann nichts einlesen, was niemand herausgeschrieben hat.

## Missing-Docs

- **`Results.File` mit einem `byte[]` gegen einen Strom bei mehreren Megabyte.** Der Bestand liefert so bisher nur Anhänge (Dateien von der Platte) und eine CSV-Datei (klein). Ab welcher Größe die im Speicher erzeugte Datei praktisch wehtut — Kestrel-Standardgrenzen, Verhalten des Browsers bei fehlender `Content-Length` —, ist im Repository nirgends notiert.
- **Vereinigende Abfragen über fünf Herkünfte in SQLite mit Dapper.** Der Bestand liest Kontributoren bisher nur installationsweit oder je Zeile; ob `UNION` über fünf Teilabfragen oder ein `IN`-Filter über eine gesammelte Nummernmenge der bessere Weg ist, ist nicht belegt. Für `B0534` wäre eine Notiz hilfreich.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Eine Kopie der SQLite-Datei ausliefern** | Ohne Werkzeug unlesbar, und sie trüge **das Schema statt des Boards** — dazu alle anderen Boards, die in derselben Datei liegen. Der Gegenentwurf zu „eigenständig". |
| **Ein ZIP aus JSON und den Anhangdateien** | Keine „eigenständige **Datei**" mehr im Sinn des Kriteriums, und `I0039` müsste dann auch die Dateien wiederherstellen. Eigener Slice, sobald ihn jemand bestellt. |
| **Anhangbytes als Base64 im JSON** | Aus 50 MB würden 67 MB Text in **einer** Zeile; aus „lesbar" würde ein Versprechen. |
| **Die Datei aus HTTP-Aufrufen auf die eigenen Rohdatenrouten zusammensetzen** | Ein zweiter Weg zu denselben Zeilen **und** ein zweiter Konsistenzstand: vier Antworten aus vier Transaktionen. |
| **Ein eigener Satz Abfragen nur für den Export** | Dieselbe Verdopplung eine Ebene tiefer; die Leser aus `R00039` sind genau dafür gebaut. |
| **Nur die Kartennummer statt Nummer und Zählerstand** | Aus „AB203" ist der Stand nicht rückgewinnbar (`Kartennummer.Aus` nutzt Mindestbreite `D2`, und ein Präfix darf Ziffern tragen) — `I0039` könnte den Nummernkreis nicht wiederherstellen. |
| **Nur die Kontributor-Nummern** | „Verantwortlich: 7" ist weder lesbar noch auflösbar; die installationsweite Personenliste steht in keiner Boarddatei. |
| **Die ganze Personenliste der Installation mitschicken** | Ein Board exportiert seinen Inhalt, nicht das Adressbuch daneben. |
| **Das Sollband weglassen, weil das Kriterium es nicht nennt** | Es ist eine **gespeicherte Zeile**, kein Rechenergebnis; ohne sie verlöre die wieder eingelesene Karte das Band, das der WBS-Import geschrieben hat. Als Annahme benannt. |
| **`Spalte` und `Board` unverändert wiederverwenden** (`B0535`) | „Kartenzahl: 0" neben 24 Karten und doppelte laufende Zeiteinträge — die Datei widerspräche sich selbst. |
| **Die Karten in die Spalten schachteln statt in eine flache Liste** | Jede Karte stünde zweimal in der Datei, sobald die Kartenliste auch flach reist — und einmal zu wenig, wenn nicht. Der Ort steht **an der Karte**. |
| **Eine zweite Route `…/export` ohne Endung neben `export.json`** | Zwei Adressen für dieselbe Datei; die Endung im Pfad ist die Form, die `zeitexport.csv` schon führt. |
| **Ein `<button>` mit JS-Interop und Blob** | Die Bytes flössen durch den Blazor-Prozess und den SignalR-Kreislauf; der Browser bekäme keinen echten Download mit Name, Fortschritt und Abbruch. |
| **Ein `ExportApiKlient` in der Oberfläche** | Ein Glied ohne Aufrufer — tote Flexibilität (C24). Der Browser holt die Datei selbst. |
| **Ein eigener Exportschirm mit Dateiwahl, Vorschau und Bericht** | Genau der Ablauf, den das Artboard als **Lücke** markiert — und er gehört `I0039`. Ein Export ist ein Klick und eine Datei. |
| **Den Menüpunkt an der archivierten Kachel weglassen** | Die Ausleitung eines abgeschlossenen Boards ist der wahrscheinlichste Anlass überhaupt. |
| **Mehrere Boards in einer Datei / ein Gesamtexport** | Der Slice heißt „Board exportieren"; ein Gesamtexport wäre wieder die Datenbank, nur in JSON. |
| **Einen Archivfilter oder eine Seitengröße als Parameter** | Aus dem Vollständigkeitsversprechen würde eine Option — dieselbe Zurückweisung wie bei `R00039`. |

### Bewusst out of scope

- Der **Import** (`I0039`) samt Dateiwahl, Vorschau, Bericht, Fassungsprüfung und Konfliktbehandlung.
- Anhangbytes, Archive, Kompression; Strom, Chunking, ETag, Zwischenspeicher.
- Export mehrerer Boards, Gesamtexport der Installation, Zeitplan-gesteuerte Sicherung.
- Jede Rechnung: Summen, Bänder, Kurven, Auswertungen — die Datei trägt die gespeicherten Zeilen.
- Schema, Migrationen, Schreibwege.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **JSON als Form**, Endung im Pfad, Auslieferung über `Results.File` — **eine** Route für Browser und Agent.
2. **`Kartensollzeit` reist mit**, obwohl das Fertig-Kriterium sie nicht aufzählt.
3. **Eine Fassungsnummer im Kopf**, obwohl das Fertig-Kriterium sie nicht verlangt.
4. **Anhangbytes bleiben draußen; der Kopf benennt die Lücke** statt sie zu verschweigen.
5. **Nur die referenzierten Kontributoren** reisen mit, nicht die Personenliste der Installation.
6. **`Exportboard`/`Exportspalte` statt `Board`/`Spalte`** — begründete Abweichung von `B0535`, weil die wiederverwendeten Verträge die Datei zu einer Falschaussage zwängen.
7. **Der Menüpunkt steht auch an der archivierten Kachel.**
8. **Für `I0039` gebaut, ohne ihn vorwegzunehmen**: ursprüngliche Nummern, Verweise nur innerhalb der Datei, Zählerstand getrennt, Fassungsnummer im Kopf — die **Schreibreihenfolge ist gangbar, aber nicht verbindlich**.
9. **Kein Schema, keine Migration, kein Schreibweg** — drei neue Leseweiten neben vorhandenen.
10. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `D0001.dc.html:181` ist der Verweis für die Gestaltung von `F0071`; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet** — der dort gezeichnete Ablauf mit Dateiwahl und Bericht ist ausdrücklich **nicht** Teil dieses Slice.
