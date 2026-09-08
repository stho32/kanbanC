---
id: R00041
status: Neu
datum: 2026-09-08
---

# R00041: Board importieren

## Beschreibung

Eine neue Route liest eine **exportierte Boarddatei** ein: `POST /api/boards/import` nimmt die Datei als multipart entgegen, prüft sie vollständig, **bevor** etwas geschrieben wird, und legt daraus ein **neues** Board mit Spalten, Kartenklassen, Kontributoren, allen Karten samt ihrem Beiwerk und allen Zeiteinträgen an — in **einer** Transaktion. Derselbe Aufruf mit `trocken=true` — **der Vorgabe** — liefert dieselbe Antwort, ohne etwas anzulegen. Auf `/boards` steht neben „+ Board anlegen" ein zweiter Knopf `Board importieren`, der eine Ablegefläche öffnet, die Vorschau samt ihren beiden Preisen zeigt und erst auf ausdrücklichen Klick anlegt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Lokale Datenhaltung … die Daten liegen auf der eigenen Maschine und sind von dort **unmittelbar zugänglich**"; die Portabilität, die bei `R00001` gegen **eine** SQLite-Datei für alle Boards eingetauscht wurde, ist mit `I0038` herausgeschrieben worden und kommt hier **zurück in eine Installation**. Und auf „Eine API auf Augenhöhe mit der Oberfläche": der Schirm setzt nur die zwei Felder, die die Route ohnehin führt.

**Die eine Regel dieses Slice, wörtlich:**

> *Das Board entsteht **neu**. Nichts, was schon da ist, wird angefasst — und was der Import nicht halten kann, sagt er **vor** dem Schreiben.*

Daraus folgt alles Übrige ohne Ausnahmeklausel:

| Lage | Folge, ohne Sonderregel |
|---|---|
| **`BoardId 1` steht in der Datei** | das neue Board bekommt eine **neue** Nummer; die Nummer der Datei lebt nur im Lauf, in einer Nummernabbildung |
| **eine Zeile zeigt auf eine Nummer, die nicht in der Datei steht** | die **ganze** Datei wird zurückgewiesen — ein halbes Board ließe sich nicht mehr entfernen |
| **eine fremde Fassungsnummer im Kopf** | zurückgewiesen, **nicht geraten**; geprüft wird, **bevor** geschrieben wird |
| **„Stefan" steht schon in der Personenliste** | der Kontributor der Datei wird **neu angelegt**, nicht zusammengeführt — und die Vorschau nennt den Namen **vorher** |
| **ein Anhang in der Datei** | die **Zeile** entsteht, die **Bytes** fehlen; der Bericht sagt es, statt es zu verschweigen |
| **`trocken` fehlt im Aufruf** | es wird **nichts** geschrieben — die teure Richtung gehört nie in die Vorgabe |
| **das Schreiben bricht in der Mitte ab** | **kein** halbes Board: der ganze Lauf ist **eine** Transaktion |
| **Kartennummer `WBS-32` in der Datei** | steht danach **unverändert** als `WBS-32` am neuen Board |

**Was dieser Slice nicht ist:** kein Schema, keine Migration, kein zweiter Dateityp, kein Zusammenführen von Personen, kein Wiedererkennen eines schon importierten Boards, kein Ereignis auf dem Live-Kanal, kein Löschweg.

## Geschäftlicher Nutzen

`I0038` hat den **Gegenstand** geschaffen — eine Datei, die ein Board vollständig trägt und ohne die Anwendung lesbar ist. Was fehlt, ist der Weg **zurück hinein**: heute kann man ein Board herausschreiben, sichern und weitergeben, aber niemand kann es wieder aufbauen. Die Datei ist damit ein Endpunkt statt eines Austauschformats, und die bei `R00001` aufgegebene Portabilität ist erst zur Hälfte zurück.

**Was der Slice ermöglicht, und zwar erst mit ihm:**

| Anlass | Heute | Mit diesem Slice |
|---|---|---|
| Ein Board auf eine **andere** Installation bringen | die Datei liegt vor und niemand kann sie lesen | ein Aufruf, ein neues Board |
| Ein abgelegtes Board **wieder aufmachen** | die Zeilen sind exportiert, aber nicht rückführbar | Datei ablegen, Vorschau ansehen, anlegen |
| Ein Board als **Vorlage** weitergeben | — | dieselbe Datei ergibt auf jeder Installation ein Board |
| Ein Agent will ein Board **aus Daten aufbauen** | er muss 19 Tabellen über sieben Routen füllen und die Reihenfolge selbst kennen | **eine** Route, **ein** Aufruf, **eine** Transaktion |

Der Wert für den Agenten ist dabei der größere: `POST /api/boards/import` ist der **einzige** Weg im ganzen Bestand, auf dem ein Board samt Inhalt in **einem** Aufruf entsteht. Die Zusage der Vision — „was die Oberfläche kann, kann die API" — gilt hier von Anfang an in beide Richtungen, weil die Vorschau **dieselbe** Antwort ist, die der Mensch im Schirm sieht.

## Funktionale Anforderungen

- `POST /api/boards/import` nimmt eine Boarddatei als multipart entgegen und antwortet mit einem **Bericht**.
- Die Datei wird **vollständig geprüft, bevor etwas geschrieben wird**: lesbares JSON, vorhandener Exportkopf, bekannte Fassungsnummer, bekannter Anwendungsname und **jede** Nummer, auf die eine Zeile zeigt, als Zeile in **derselben** Datei.
- Jede Zurückweisung nennt **Grund, gefundene Werte und Kompensationsaktion**; nach einer Zurückweisung ist **nichts** geschrieben.
- `trocken=true` ist die **Vorgabe**: der Lauf liest, prüft und zählt, schreibt aber nichts, und die Datenbank ist danach unverändert.
- Der Bericht trägt in **jeder** Antwort denselben Satz Zahlen — Spalten, Kartenklassen, Kontributoren, Karten, Etiketten, Teilaufgaben, Kommentare, Anhänge, Dateiverweise, Zeiteinträge —, dazu den Boardnamen, die **Namen der Personen, die es hier schon gibt**, und den Hinweis, dass die Anhänge **ohne Inhalt** ankommen.
- Mit `trocken=false` entsteht in **einer** Transaktion ein neues Board mit **neuer** `BoardId`; die Antwort ist **201** mit dem Bericht und dieser Nummer.
- **Jeder Verweis der Datei zeigt danach auf die neue Nummer**; die Nummern der Datei erscheinen nach außen nicht.
- **Kartennummern bleiben, wie sie waren**: Präfix und Zählerstand reisen unverändert mit.
- **Kein Kontributor wird zusammengeführt**: jeder Kontributor der Datei entsteht neu, auch wenn sein Name hier schon steht.
- **Anhänge bekommen ihre Zeile, aber keine Bytes**; ein Abruf ihres Inhalts antwortet mit dem vorhandenen Befund `anhang-bytes-fehlen`.
- **Keine Zeile eines bestehenden Boards wird verändert.**
- Auf `/boards` steht neben „+ Board anlegen" ein zweiter Knopf `Board importieren`; er öffnet eine Ablegefläche, zeigt nach der Dateiwahl die Vorschau und legt erst auf ausdrücklichen Klick an.
- Nach dem Anlegen führt ein Verweis auf `/boards/{BoardId}`, und die Boardliste wird neu geholt.

## Nicht-funktionale Anforderungen

- **Ein Dateityp, nicht zwei.** Gelesen wird `Boardexport` aus `KanbanC.Contracts/Export` — derselbe Vertrag, den `I0038` schreibt. Ein eigener Import-Dateityp liefe bei der nächsten Ergänzung auseinander.
- **Eine Leseform, nicht zwei.** Der Leser nutzt **dieselben** Serialisierungsoptionen wie `Exportdatei` (`JsonSerializerDefaults.Web`, entspannter Encoder) — sonst läse die Anwendung ihre eigene Datei nicht.
- **Eine Prüfreihenfolge, nicht zwei.** Trockener und schreibender Lauf gehen denselben Weg; ein zweiter Prüfpfad wäre eine zweite Wahrheit darüber, was eine gültige Datei ist.
- **Alles oder nichts.** `BEGIN IMMEDIATE` über den ganzen Schreiblauf, Muster `WbsImportRepository`. Bricht er ab, steht **kein** halbes Board da — und ein halbes Board ließe sich nicht entfernen: im ganzen Bestand gibt es **kein** `DELETE` auf ein Board (`MapDelete` findet nur Spalte, Zeiteintrag, Anhang, Dateiverweis), nur Archivieren (`I0005`).
- **Kein Ereignis auf dem Live-Kanal.** Der Kanal führt `Kartenereignis` und `Importereignis`; ein Boardereignis träfe auch `I0001` und `I0005` und ist ein eigener Slice. **Folge, ausdrücklich benannt:** ein importiertes Board erscheint bei anderen Betrachtern erst nach dem nächsten Laden.
- **Keine Migration.** Das Schema trägt alles; die 19 Tabellen werden nur geschrieben, nicht geändert.
- Datums- und Zeitwerte als ISO-Text wie in `WbsImportRepository` — **Dapper materialisiert `DateOnly` und `DateTimeOffset` aus SQLite nicht von sich aus**.
- Gestaltungswerte ausschließlich aus `gestaltung.css` — keine Farb-, Abstands- oder Radiusliterale.
- **Keine Projektreferenz `KanbanC.Blazor` → `KanbanC.BL`** (Kernregel des Projekts).

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Eine exportierte Board-Datei wird eingelesen; das Board erscheint mit seinem Inhalt in der Liste, ohne bestehende Boards zu veraendern."*

Das Kriterium trägt **einen Vorgang** (eine exportierte Datei wird eingelesen), **eine Wirkung** (das Board erscheint mit seinem Inhalt in der Liste) und **eine Zusage** (*ohne bestehende Boards zu verändern*). Alle drei sind unten in einzeln prüfbare Sätze zerlegt; die Zusage ist die tragende und wird **bewiesen, nicht behauptet**.

### Das durchgehende Rechenbeispiel

Eine Installation führt bereits **zwei** Boards: `Board 1` „Betrieb" (Linie, 3 Spalten, 5 Karten) und `Board 2` „Release 1" (Projekt, 4 Spalten, 12 Karten). In der Personenliste stehen `Stefan` (7) und `Zora` (8).

Eingelesen wird eine Datei aus einer **anderen** Installation, ausgeleitet mit `GET /api/boards/{boardId}/export.json`:

| | Inhalt der Datei | Zweck im Beispiel |
|---|---|---|
| Board | `BoardId 1` „KanbanC — Release 2", Projekt, Zieltermin | **dieselbe Nummer wie ein vorhandenes Board** |
| Spalten | 3, darunter „Erledigt" als Abschlussspalte mit Anzeigegrenze 20 | Bezeichnungen, die es hier auch gibt |
| Kartenklassen | 1, Präfix `WBS-`, Zählerstand 40 | der Nummernkreis |
| Kontributoren | `Stefan` (7, aktiv), `Alt-Kollege` (11, **stillgelegt**) | ein Name, den es hier schon gibt; einer, den es nicht gibt |
| Karten | 24, darunter eine **archivierte**, eine **ohne Klasse und ohne Sollband**, eine mit **`WBS-32`**, Sollband 2,0–4,0 h, Verantwortlichem `Stefan` (7) und je einem Eintrag in **allen fünf Listen** | der volle Fall |
| Zeiteinträge | 2, davon einer **laufend** (`Ende: null`) von `Alt-Kollege` | laufend, stillgelegt |

- [ ] `POST /api/boards/import` mit dieser Datei und `trocken=true` antwortet mit **200** und einem Bericht: Boardname „KanbanC — Release 2", **3** Spalten, **1** Kartenklasse, **2** Kontributoren, **24** Karten, **2** Zeiteinträge, dazu die Zahlen der fünf Listen — und die Datenbank ist danach **unverändert** (Boardliste weiterhin `Betrieb`, `Release 1`; Personenliste weiterhin `Stefan`, `Zora`).
- [ ] Derselbe Aufruf mit `trocken=false` antwortet mit **201**, einer **neuen** `BoardId` (**3**, nicht 1) und **denselben zehn Zahlen** wie die Vorschau.
- [ ] `GET /api/boards` liefert danach **drei** Boards; das neue trägt den Namen aus der Datei und lässt sich unter `/boards/3` öffnen.
- [ ] Ein Aufruf **ohne** das Feld `trocken` schreibt **nichts** und antwortet mit **200** — die Vorgabe ist `true`.

### „Ohne bestehende Boards zu verändern" — die tragende Zusage, bewiesen

- [ ] **Board 1 und Board 2 sind Zeile für Zeile unverändert**: Board, Boardeinstellung, Boardarchivierung, Spalte, Karte, Karteneigenschaft, Karteerledigung, Kartenarchivierung, Kartenklasse, Kartenklassenzuordnung, Kartensollzeit, Etikett, Teilaufgabe, Kommentar, Anhang, Dateiverweis und Zeiteintrag. Geprüft wird der **abgezogene Bestand vor dem Lauf** gegen den Bestand danach, nicht ein Beispiel.
- [ ] **Ehrlich benannt, was sich doch ändert**: die Tabelle `Kontributor` **wächst** um die Personen der Datei — sie ist als einzige installationsweit und gehört keinem Board. **Keine vorhandene Zeile** wird dabei angefasst: `Stefan` (7) behält Nummer, Name, Art und Stilllegungsstand.
- [ ] **Die Nummer der Datei überschreibt nichts.** Die Datei nennt `BoardId 1`, und `Board 1` „Betrieb" existiert — nach dem Lauf heißt `Board 1` weiterhin „Betrieb" und hat weiterhin 5 Karten.
- [ ] **Kein halbes Board bei einem Abbruch.** Bricht das Schreiben nach den Spalten ab, ist danach **kein** neues Board, **keine** Spalte, **keine** Karte und **kein** neuer Kontributor da — der ganze Lauf ist eine Transaktion. Rechenbeispiel: Boardzahl vor dem Lauf 2, nach dem gescheiterten Lauf 2.
- [ ] **Keine Migration und keine Schemaänderung** — das Schema vor und nach dem Slice ist dasselbe.

### „Das Board erscheint mit seinem Inhalt" — neue Nummern, gleicher Inhalt

- [ ] **Jeder Verweis zeigt auf die neue Nummer**: die Karten liegen in den **neuen** Spalten, tragen die **neue** Kartenklasse, den **neuen** Verantwortlichen; Kommentare, Anhänge und Dateiverweise tragen ihre **neuen** Urheber; die Zeiteinträge zeigen auf die **neue** Karte und den **neuen** Kontributor. Keine Nummer der Datei steht nach dem Lauf in einer Zeile.
- [ ] **Was ein Mensch liest, bleibt gleich**: die Karte mit `WBS-32` in der Datei trägt danach `WBS-32`. Rechnerisch: `UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand` gilt **je Klasse**, die Klasse ist neu, also passt Zählerstand 32 unverändert hinein — und der Zählerstand der Klasse selbst steht danach auf **40** wie in der Datei, sodass die nächste Karte `WBS-41` heißt und nicht `WBS-1`.
- [ ] **Bezeichnungen kollidieren nicht**: `UX_Spalte_Board_Bezeichnung` (Migration 002) und `UX_Kartenklasse_Board_Praefix` (Migration 016) gelten **je Board** — beim neuen Board kann keiner der Indizes greifen, auch wenn `Betrieb` eine Spalte „Erledigt" und ein Präfix `WBS-` führt.
- [ ] **Der laufende Zeiteintrag bleibt laufend** (`Ende: null`) — `UX_Zeiteintrag_Karte_Kontributor_Laufend` gilt je Karte und Kontributor, und beide sind neu.
- [ ] **Die archivierte Karte kommt archiviert an**, die Karte ohne Klasse bleibt ohne Klasse, die Karte ohne Sollband bekommt **kein Ersatzband**, und die Spalte „Erledigt" behält Abschlussmarke und Anzeigegrenze 20.
- [ ] **Der stillgelegte Kontributor kommt stillgelegt an** — seine Arbeit steht in der Datei, und ein Import, der ihn aktiv anlegte, erfände einen Zustand.
- [ ] **Gegenprobe durch das Nadelöhr**: das exportierte und wieder eingelesene Board liefert bei `GET /api/boards/{neueId}/export.json` dieselben Inhalte wie die Ausgangsdatei — bis auf die Nummern und den Kopfzeitpunkt.

### Prüfen, bevor geschrieben wird (`F0072`)

Fertig-Kriterium wörtlich: *„`POST /api/boards/import` nimmt eine Datei entgegen und weist zurueck, was kein Board wiederherstellen kann: keine JSON-Datei, kein Exportkopf, eine fremde Fassungsnummer, ein Verweis auf eine Nummer, die nicht in derselben Datei steht. Jede Zurueckweisung nennt Grund, gefundene Werte und Kompensationsaktion; geschrieben wird nichts."*

- [ ] **Kein JSON** (Textdatei, abgeschnittene Datei, leere Datei) → **400**, Grund mit der Meldung des Parsers, Kompensationsaktion „das Board mit dieser Anwendung neu ausleiten".
- [ ] **Kein Exportkopf** (gültiges JSON ohne `kopf`) → **400** mit demselben Befundträger; eine fremde JSON-Datei ist kein Board.
- [ ] **Fremde Fassungsnummer** (Kopf mit `fassung: 2`, erwartet `1`) → **400**, der Befund nennt die **gefundene** und die **erwartete** Zahl. Die Fassung wird **zurückgewiesen, nicht geraten**: ein nachsichtiger Leser, der unbekannte Felder überginge, schriebe ein halbes Board.
- [ ] **Fremder Anwendungsname** im Kopf → **400**; die Zahl allein genügt nicht.
- [ ] **Ein Verweis ins Leere** weist die **ganze** Datei zurück und nennt **Zeilenart, Feld und Nummer**. Geprüft werden **acht** Verweisarten: Karte → Spalte, Karte → Kartenklasse, Karte → Verantwortlicher, Urheber von Kommentar, Anhang und Dateiverweis, Zeiteintrag → Karte, Zeiteintrag → Kontributor.
- [ ] **Nach jeder Zurückweisung ist nichts geschrieben** — geprüft am Bestand, nicht am Statuscode: Boardzahl, Kartenzahl und Personenzahl sind unverändert.
- [ ] **Geprüft wird auch bei `trocken=false`, und zwar vorher** — die Prüfung sitzt vor dem Schreiben, nicht daneben.
- [ ] **Ohne Board und ohne Datenbank prüfbar**: jedes Kriterium dieser Gruppe ist an der Antwort allein zu zeigen.

### Vorschau vor dem Schreiben (`F0073`)

Fertig-Kriterium wörtlich: *„Derselbe Aufruf mit `trocken=true` — der Vorgabe — liefert den Namen des Boards und zehn Zahlen, die entstuenden (Spalten, Kartenklassen, Kontributoren, Karten, Etiketten, Teilaufgaben, Kommentare, Anhaenge, Dateiverweise, Zeiteintraege), nennt die Personennamen, die es in dieser Installation schon gibt, und sagt, dass die Anhaenge ohne Inhalt ankommen. Die Datenbank ist danach unveraendert."*

- [ ] Der Bericht trägt **genau zehn** Zahlen, und sie stimmen mit dem Inhalt der Datei überein — im Beispiel 3 / 1 / 2 / 24 / 1 / 1 / 1 / 1 / 1 / 2.
- [ ] **Dieselben Zahlen stehen im Bericht des geschriebenen Laufs.** Wer Vorschau und Bericht nebeneinanderlegt, sieht, dass unterwegs nichts verlorenging — und ein Agent bekommt ohne zweiten Aufruf, was ein Mensch im Schirm sieht.
- [ ] **Die doppelten Namen stehen vorher da**: die Vorschau nennt `Stefan`, weil dieser Name in der Personenliste schon steht — und nennt `Alt-Kollege` **nicht**. Bei zwei Personen gleichen Namens in der Datei steht der Name so oft, wie er entsteht.
- [ ] **Der Anhanghinweis steht im Bericht**, nicht nur in dieser Anforderung: die Anhänge kommen **ohne Inhalt** an.
- [ ] **`BoardId` ist in der Vorschau `null`** — das Board entsteht erst beim Schreiben, und eine erfundene Nummer wäre eine Zusage, die niemand hält.
- [ ] **Die Datenbank ist danach unverändert** — geprüft wird, dass der Dienst **keine** Schreibmethode ruft, und zwar am Test-Repository, nicht am Vertrauen.

### Das Board entsteht mit neuen Nummern (`F0074`)

Fertig-Kriterium wörtlich: *„Mit `trocken=false` entsteht in **einer** Transaktion ein neues Board mit neuer `BoardId`; jeder Verweis der Datei zeigt danach auf die neue Nummer, die Kartennummern aus Praefix und Zaehlerstand sind dieselben wie in der Datei, und keine Zeile eines bestehenden Boards ist veraendert. Die Antwort ist 201 mit dem Bericht und der neuen `BoardId`."*

- [ ] **201** mit dem Bericht und der neuen `BoardId`; **200** bleibt der Vorschau vorbehalten — der Unterschied zwischen „so sähe es aus" und „so ist es jetzt" steht im Statuscode.
- [ ] **Die Adresse trägt keine `boardId`** (`POST /api/boards/import`), weil es das Board noch nicht gibt; kein Konflikt mit `GET /api/boards/{boardId:long}`, dessen `:long`-Constraint `import` nicht zulässt.
- [ ] Datei und Feld `trocken` reisen im **selben** multipart-Rumpf; `trocken` trägt `[FromForm]`, sonst bände es aus der Query.
- [ ] **Alle 19 boardbezogenen Tabellen werden bedient**, soweit die Datei sie füllt: Board, Boardeinstellung mit `ZeigtKartenzahl`, Boardarchivierung nur bei archiviertem Board, Spalte, Kartenklasse mit Zählerstand, Kontributor, Kontributorstilllegung, Karte, Karteneigenschaft, Karteerledigung, Kartenarchivierung, Kartenklassenzuordnung mit Zählerstand, Kartensollzeit, Etikett, Teilaufgabe, Kommentar, Anhang, Dateiverweis, Zeiteintrag.
- [ ] **Der Anhang bekommt seine Zeile, aber keine Bytes.** Ein Abruf seines Inhalts antwortet mit dem **vorhandenen** Befund `anhang-bytes-fehlen` — Grund, Werte und die Kompensation „entfernen und neu anhängen" —, kein Absturz und kein leerer Download, der wie ein Erfolg aussähe.
- [ ] Der Fehlervertragstest (`FehlervertragTests`, aus `B0102`) nimmt die Route auf — sonst schlägt `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` fehl.

### Einstieg auf der Boardliste (`F0075`)

Fertig-Kriterium wörtlich: *„Auf `/boards` steht neben „+ Board anlegen" ein zweiter Knopf „Board importieren"; er oeffnet eine Ablegeflaeche, zeigt nach der Dateiwahl die Vorschau mit ihren Zahlen und ihren beiden Preisen und legt erst auf ausdruecklichen Klick an. Danach steht das neue Board in der Liste und laesst sich von dort oeffnen."*

- [ ] Der Knopf steht **im Seitenkopf neben „+ Board anlegen"** und **nicht** im ⋯-Menü einer Kachel: das Menü handelt an einem **vorhandenen** Board, der Import **erzeugt** eines.
- [ ] **Kein eigener Schirm und keine eigene Route** — der Ablauf sitzt auf `/boards`.
- [ ] **Zwei Schritte, nicht drei**: Datei wählen und Vorschau ansehen, dann bestätigen. Anders als beim WBS-Import gibt es **nichts zu wählen, nur zu bestätigen**.
- [ ] **Die beiden Preise stehen vor dem Knopf, nicht danach**: welche Personennamen ein zweites Mal entstehen und dass die Anhänge ohne Inhalt ankommen.
- [ ] Die Ablegefläche ist **während des Laufs gesperrt** (`R00024`) — eine zweite Ablage während des ersten Laufs verlöre den ersten still.
- [ ] Nach dem Anlegen führt ein Verweis auf `/boards/{BoardId}`, und die Liste wird **neu geholt** — sie ist die eine Quelle, wie schon nach einer Kacheländerung.
- [ ] Eine nicht erreichbare WebApi ergibt eine **lesbare Meldung** statt einer Ausnahmeseite (`WebApiAufruf.MitAusfallmeldung`).
- [ ] **`trocken` reist in beiden Schritten ausdrücklich mit** — im ersten als `true`, im zweiten als `false`; eine Auslassung ließe die Vorgabe stillschweigend gelten.

### Der grüne Bestand bleibt grün

- [ ] `GET /api/boards/{boardId}/export.json` bleibt **unverändert** — dieser Slice liest die Datei, er ändert sie nicht.
- [ ] `Boardexport`, `Exportkopf`, `Exportboard`, `Exportspalte` und `Exportkarte` bleiben, wie sie sind. **Kein zweiter Dateityp.**
- [ ] Der WBS-Import (`I0030`, `I0031`) bleibt unberührt; `Importanfrage`, `Importbericht` und `WbsImportEndpunkte` werden **nicht** angefasst.
- [ ] `Nichtgefunden`, `Doppelt` und `Stillgelegt` bleiben unverändert; der neue Befundträger tritt **daneben**.
- [ ] `POST /api/boards` (Board anlegen) und `GET /api/boards/{boardId}` bleiben unverändert erreichbar.
- [ ] **Keine Migration, keine Schemaänderung.**

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Zusammenführen von Kontributoren** — jeder Kontributor der Datei entsteht neu.
- [ ] **Kein Wiedererkennen** eines schon importierten Boards: dieselbe Datei zweimal eingelesen ergibt **zwei** unabhängige Boards.
- [ ] **Kein Überschreiben oder Aktualisieren** eines bestehenden Boards, kein Soll-Ist-Abgleich wie beim WBS-Import (`I0031`).
- [ ] **Keine Anhangbytes**, kein Archiv aus JSON und Dateien, kein ZIP.
- [ ] **Kein Löschweg für ein Board** — er fehlt im Bestand und wird hier nicht nachgeholt.
- [ ] **Kein Ereignis auf dem Live-Kanal**; ein importiertes Board erscheint bei anderen Betrachtern erst nach dem nächsten Laden.
- [ ] **Kein Import mehrerer Boards** aus einer Datei, kein Gesamtimport einer Installation.
- [ ] **Kein Fortschrittsbalken, kein Strom, kein Chunking.**

## Betroffene Verzeichnisstruktur

Ein neuer Themenordner kommt hinzu: **`Boardimport`** — in Contracts, BL und Tests. Der Rest wächst an vorhandenen Stellen.

- **Contracts**: `KanbanC.Contracts/Boardimport` — `Boardimportanfrage`, `Boardimportzahlen`, `Boardimportbericht` (alle immutable, C08). **Gelesen** wird `KanbanC.Contracts/Export/Boardexport` — unverändert.
- **BL**: `KanbanC.BL/Operations/Boardimport` (`Boarddateileser`, `Fassungspruefung`, `Geschlossenheitspruefung`, `Boardimportzaehlung`, `Namensdubletten`), `KanbanC.BL/Models/Boardimport` (`Nummernabbildung`), `KanbanC.BL/Operations/Fehler/Unlesbar.cs` (**neuer Befundträger neben `Nichtgefunden`, `Doppelt`, `Stillgelegt`**), `KanbanC.BL/Interfaces/Boardimport` (`IBoardimportRepository`), `KanbanC.BL/Persistenz/Boardimport` (`BoardimportRepository`), `KanbanC.BL/Integrations/Boardimport` (`BoardimportService`).
- **API**: `KanbanC.WebApi/Endpunkte/BoardimportEndpunkte.cs` — eine Route; registriert in `Program.cs` wie `WbsImportEndpunkte`.
- **Oberfläche**: `KanbanC.Blazor/Services/BoardimportApiKlient.cs`, `KanbanC.Blazor/Components/Pages/Boards.razor` (+ `.razor.css`) oder eine eingebundene Komponente unter `Components/Boards/`. **Keine Projektreferenz auf `KanbanC.BL`.**
- **Tests**: `KanbanC.BL.Tests/Operations/Boardimport`, `…/Models/Boardimport`, `…/Integrations/Boardimport`; `KanbanC.WebApi.IntegrationTests/{Api,Persistenz/Boardimport}`; `KanbanC.Blazor.Tests/Services` und `…/Gestaltung`; `KanbanC.PlaywrightTests` (Seitenobjekt `BoardsSeite`).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Die tragende Entscheidung: neue Nummern, und sie ist belegt

Das Board entsteht **neu** und behält die Nummern der Datei **nicht**. Der Grund ist nachgesehen, nicht stilistisch:

- Alle Schlüssel sind `INTEGER PRIMARY KEY AUTOINCREMENT`; eine Nummer zu **setzen** hieße, eine vorhandene Zeile zu treffen.
- Die Datei kommt aus einer **anderen** Installation, in der `BoardId 1` und `KontributorId 7` etwas anderes bezeichnen als hier.
- **Die Nummern zu behalten hieße, vorhandene Zeilen zu überschreiben — genau das, was „ohne bestehende Boards zu verändern" ausschließt.**

Die Nummern der Datei leben deshalb nur im Lauf, in einer **Nummernabbildung** je Tabelle (Spalte, Kartenklasse, Kontributor, Karte); nach außen erscheinen ausschließlich die neuen.

**Was ein Mensch liest, bleibt trotzdem gleich — nachgesehen:** `UX_Kartenklasse_Board_Praefix` (Migration 016) und `UX_Spalte_Board_Bezeichnung` (Migration 002) gelten **je Board**, `UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand` (Migration 017) **je Klasse**, `UX_Zeiteintrag_Karte_Kontributor_Laufend` (Migration 018) je Karte und Kontributor. Board, Klasse und Karte sind neu — also kann keiner dieser Indizes greifen, und jeder Zählerstand passt unverändert hinein: `WBS-32` bleibt `WBS-32`.

**Der Preis, ausgesprochen:** eine Datei, die in **dieselbe** Installation zurückgelesen wird, ergibt ein **zweites, unabhängiges Board**. Wiedererkennen wäre ein eigener Slice und bräuchte einen installationsübergreifend stabilen Schlüssel — **den die Datei nicht trägt** (`Exportkopf` führt Anwendung, Fassung, Zeitpunkt und Anhanghinweis, keine Laufkennung).

### Kontributoren: neu anlegen, nicht zusammenführen

`Kontributor` hat **kein `UNIQUE` auf `Name`** — Migration 006 sagt es im Kommentar ausdrücklich: „zwei Menschen dürfen gleich heißen, und unterschieden werden sie über die KontributorId". Ein Abgleich nach Namen wäre damit **eine Vermutung, keine Identität** — und er schriebe Arbeit, Zeiten und Kommentare eines fremden Boards **still** einer hiesigen Person zu. `D0007` (Zeiten) und `D0009` (Auswertungen) rechnen daraus, und **keine spätere Auswertung könnte es zurücknehmen**.

**Der Preis, ausgesprochen:** „Stefan" steht danach zweimal in der Personenliste, und Auswertungen zählen beide getrennt. **Dieser Preis ist sichtbar und von Hand reparierbar — die stille Falschzuschreibung wäre es nicht.** `B0548` macht ihn **vor** dem Schreiben sichtbar, statt ihn zu verschweigen.

Der Kontributor ist die **einzige** installationsweite Tabelle des Laufs; sie wächst, ohne dass eine vorhandene Zeile angefasst wird. Genau das steht als Ausnahme im Beweis `B0556`.

### Anhänge: Zeile ja, Bytes nein

Die Datei trägt von jedem Anhang nur Metadaten (`I0038`, Entscheidung 4). Der Bestand modelliert die Lage schon: `KartenService.cs:407` antwortet auf einen Anhang ohne Bytes mit `Nichtgefunden.Anhangbytes` — Grund („die Datei ist ausserhalb der Anwendung verschwunden"), Werte und die Kompensation „entfernen und über `POST /api/karten/{karteId}/anhaenge` erneut anhängen".

**Der Preis, ausgesprochen:** eine importierte Karte zeigt Anhänge, **die sich nicht öffnen lassen**, bis jemand sie neu ablegt; **der Bericht sagt es vorher**. Die Alternative — die Zeile weglassen — **verschwiege, dass es den Anhang je gab**: nicht erkennbar, nicht reparierbar.

### Die Vorschau ist nicht vom WBS-Import abgeschrieben

Das Artboard verbietet das Abschreiben ausdrücklich (`D0001.dc.html:664-670`): „Hier wird nichts erfunden und nichts vom WBS-Import (D0008) abgeschrieben: dessen drei Schritte lösen eine andere Aufgabe — Knoten in Karten überführen, nicht ein Board als Ganzes wiederherstellen."

Übernommen wird **nur die Hausregel** `trocken=true` als Vorgabe — und sie gilt hier **stärker** als beim WBS-Import. Der Grund ist harte Evidenz: **im ganzen Bestand gibt es kein `DELETE` auf ein Board**; `MapDelete` findet nur Spalte, Zeiteintrag, Anhang und Dateiverweis, und ein Board wird nur **archiviert** (`I0005`). **Ein versehentlicher Import bliebe für immer stehen.**

**Zwei Schritte statt der drei des WBS-Imports:** dort wählt der Mensch eine Kartenklasse und eine Schnittebene; hier gibt es **nichts zu wählen, nur zu bestätigen**.

### Eine Datei, ein Leser, eine Prüfreihenfolge

- Gelesen wird `Boardexport` aus `KanbanC.Contracts/Export` — **kein zweiter Dateityp**, sonst liefen Ausleitung und Einlesung bei der nächsten Ergänzung auseinander.
- Der Leser nutzt **dieselben** Serialisierungsoptionen wie `Exportdatei.AlsJson` (`JsonSerializerDefaults.Web`, `UnsafeRelaxedJsonEscaping`). Die sicherste Form ist, die Optionen dort **einmal** stehen zu lassen und im Leser wiederzuverwenden: zwei Optionssätze wären zwei Formate, und die Anwendung läse ihre eigene Datei nicht mehr. **`System.Text.Json` materialisiert `DateOnly` und `DateTimeOffset` von sich aus** — die bekannte Lücke liegt bei **Dapper** und trifft erst die Schreib-Bubbles.
- **Trockener und schreibender Lauf gehen denselben Weg**: lesen, Fassung prüfen, Geschlossenheit prüfen, zählen, Dubletten suchen — und erst danach trennt sich der schreibende ab. Eine zweite Prüfreihenfolge wäre eine zweite Wahrheit darüber, was eine gültige Datei ist.

### Die Geschlossenheitsprüfung: geprüft statt geglaubt

`B0539` hat die Geschlossenheit als Zusage der **Ausleitung** bewiesen. Hier wird sie **geprüft**, weil eine Datei von Hand bearbeitet worden sein kann. Acht Verweisarten:

| Zeile | Feld | zeigt auf |
|---|---|---|
| Karte | Spalte | `Exportspalte.SpalteId` |
| Karte | Kartenklasse | `Kartenklasse.KartenklasseId` |
| Karte | Verantwortlicher | `Kontributor.KontributorId` |
| Kommentar | Urheber | `Kontributor.KontributorId` |
| Anhang | Urheber | `Kontributor.KontributorId` |
| Dateiverweis | Urheber | `Kontributor.KontributorId` |
| Zeiteintrag | Karte | `Karte.KarteId` |
| Zeiteintrag | Kontributor | `Kontributor.KontributorId` |

Ein offener Verweis weist die **ganze** Datei zurück und nennt Zeilenart, Feld und Nummer. Ein halb eingelesenes Board wäre schlimmer als keines, **denn es ließe sich nicht mehr entfernen**.

### Der Lauf als eine Transaktion

`BEGIN IMMEDIATE` über den ganzen Lauf, Muster `WbsImportRepository` — das Schreibschloss fällt vor dem ersten Schreiben, und alles oder nichts gilt für 19 Tabellen. Die Schreibreihenfolge ist die des Dokuments: Board und Boardeinstellung, Spalten, Kartenklassen, Kontributoren, dann die Karten mit ihrem Beiwerk, zuletzt die Zeiteinträge — jede Stufe füllt die Nummernabbildung, die die nächste braucht.

Datums- und Zeitwerte gehen als **ISO-Text** in die Datenbank, wie in `WbsImportRepository`: **Dapper materialisiert `DateOnly` und `DateTimeOffset` aus SQLite nicht von sich aus.**

### Die Route

`POST /api/boards/import` — **die Adresse trägt keine `boardId`**, weil es das Board noch nicht gibt. Kein Konflikt mit `GET /api/boards/{boardId:long}` (anderes Verb, und die `:long`-Constraint ließe `import` ohnehin nicht zu) und keiner mit `POST /api/boards` (ein Segment weniger). Dieselbe Form wie `export.json` als Pfadstück.

`DisableAntiforgery` wie bei `WbsImportEndpunkte` und beim Anhang: eine Minimal-API-Route mit Formularbindung scheiterte sonst schon beim ersten Aufruf, und ein Agent trägt kein Token. `[FromForm]` an `trocken`, sonst bände es aus der Query (belegt in `DateiwegProbeTests`). **200 gegen 201** trennt „so sähe es aus" von „so ist es jetzt", wie beim WBS-Import.

### Ablauf

1. **Der Mensch** klickt auf `/boards` im Seitenkopf `Board importieren`.
   - 1.1 Eine Ablegefläche erscheint; die Datei wird gewählt, die Fläche sperrt während des Laufs.
   - 1.2 Die Oberfläche ruft mit **`trocken=true`** und zeigt Zahlen, doppelte Namen und den Anhanghinweis.
   - 1.3 Erst der Klick auf `Board anlegen` ruft mit **`trocken=false`**.
2. **Oder der Agent** ruft `POST /api/boards/import` direkt. **Dieselbe Route, dieselben zwei Felder.**
3. `BoardimportEndpunkte` → `BoardimportService.Importiere(anfrage, strom)`
   - 3.1 `Boarddateileser.Lies` → kein JSON / kein Kopf → `Unlesbar.Boarddatei` → **400**
   - 3.2 `Fassungspruefung.Pruefe(kopf)` → fremde Fassung oder fremde Anwendung → **400** mit gefundener und erwarteter Zahl
   - 3.3 `Geschlossenheitspruefung.Pruefe(boardexport)` → offener Verweis → **400** mit Zeilenart, Feld und Nummer
   - 3.4 `Boardimportzaehlung.Zaehle(boardexport)` → zehn Zahlen
   - 3.5 `Namensdubletten.Finde(kontributoren der Datei, vorhandene Kontributoren)` → Namen
4. **Ist `Trocken`**: Bericht ohne `BoardId` → **200**. **Keine Schreibmethode wird gerufen.**
5. **Sonst eine Transaktion** (`BEGIN IMMEDIATE`)
   - 5.1 Board, Boardeinstellung, Boardarchivierung → neue `BoardId`
   - 5.2 Spalten, Kartenklassen, Kontributoren, Kontributorstilllegung → Nummernabbildungen
   - 5.3 Karten mit Eigenschaft, Erledigung, Archivierung, Klassenzuordnung, Sollzeit und den fünf Listen
   - 5.4 Zeiteinträge
   - 5.5 `Commit`
6. Bericht mit **denselben** Zahlen plus der neuen `BoardId` → **201**.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** der neue Endpunktsatz `BoardimportEndpunkte` in `Program.cs` (WebApi); der Seitenkopf von `Boards.razor`; der vorhandene Vertrag `Boardexport` in `KanbanC.Contracts/Export`; die Fassungsnummer `BoardexportService.Fassung` (heute `1`) und der Anwendungsname `KanbanC`.

**In `KanbanC.Contracts/Boardimport`** (immutable, C08):
- `Boardimportanfrage` (DTO) — alles außer der Datei; reist als multipart wie `Importanfrage`. **`Trocken` hat die Vorgabe `true`.**
  - `Boardimportanfrage(bool Trocken, string Dateiname)`
- `Boardimportzahlen` (DTO) — die zehn Zahlen, die in **jeder** Antwort stehen.
  - `Boardimportzahlen(int Spalten, int Kartenklassen, int Kontributoren, int Karten, int Etiketten, int Teilaufgaben, int Kommentare, int Anhaenge, int Dateiverweise, int Zeiteintraege)`
- `Boardimportbericht` (DTO) — dieselbe Gestalt für Vorschau und Lauf; `BoardId` ist in der Vorschau `null`.
  - `Boardimportbericht(string Boardname, long? BoardId, Boardimportzahlen Zahlen, IReadOnlyList<string> DoppelteNamen, string Anhanghinweis)`

**In `KanbanC.BL/Operations/Fehler`:**
- `Unlesbar` (Operation, statisch; **neuer Befundträger** neben `Nichtgefunden`, `Doppelt`, `Stillgelegt`) — „die Datei sagt nicht, was sie sein müsste". Codes für unlesbare Datei, fremde Fassung und offenen Verweis; jeder Befund nennt Grund, Werte und Kompensationsaktion.

**In `KanbanC.BL/Operations/Boardimport`** (pure, ohne Datenbank):
- `Boarddateileser` (Operation) — Strom zu `Boardexport`, mit denselben Serialisierungsoptionen wie `Exportdatei`.
  - `Ergebnis<Boardexport> Lies(Stream inhalt)`
- `Fassungspruefung` (Operation, pure) — Anwendungsname und Fassungsnummer des Kopfes.
  - `Pruefbefunde Pruefe(Exportkopf kopf)`
- `Geschlossenheitspruefung` (Operation, pure) — die acht Verweisarten.
  - `Pruefbefunde Pruefe(Boardexport datei)`
- `Boardimportzaehlung` (Operation, pure; Muster `Kartenzahlen` beim WBS-Import) — die zehn Zahlen.
  - `Boardimportzahlen Zaehle(Boardexport datei)`
- `Namensdubletten` (Operation, pure) — welche Namen der Datei hier schon stehen.
  - `IReadOnlyList<string> Finde(IReadOnlyList<Kontributor> ausDerDatei, IReadOnlyList<Kontributor> vorhandene)`

**In `KanbanC.BL/Models/Boardimport`:**
- `Nummernabbildung` (Model, ohne Datenbank) — alte Nummer zu neuer Nummer, je Tabelle eine. Ein unbekannter Schlüssel ist ein Programmfehler, keine Nachsicht: die Geschlossenheitsprüfung hat ihn ausgeschlossen.

**In `KanbanC.BL/Interfaces/Boardimport` und `KanbanC.BL/Persistenz/Boardimport`:**
- `IBoardimportRepository` / `BoardimportRepository` (Integration, Ressourcenzugriff) — **eine** Schreibtransaktion über alle Tabellen.
  - `long SchreibeBoard(Boardexport datei)` (liefert die neue `BoardId`)
  - `IReadOnlyList<Kontributor> LiesVorhandeneKontributoren()` (für die Dublettensuche, auch im trockenen Lauf)

**In `KanbanC.BL/Integrations/Boardimport`:**
- `BoardimportService` (Integration, fängt/loggt; Muster `WbsImportService`) — lesen, prüfen, zählen, Dubletten suchen; **nur im schreibenden Lauf** die Schreibmethode.
  - `Ergebnis<Boardimportbericht> Importiere(Boardimportanfrage anfrage, Stream inhalt)`

**In `KanbanC.WebApi/Endpunkte`:**
- `BoardimportEndpunkte` (Integration) — `POST /api/boards/import`, `DisableAntiforgery`, `[FromForm] bool? trocken`; 200 / 201 / 400 über `Zurueckweisungen.AlsFehlerantwort`.

**In `KanbanC.Blazor/Services`:**
- `BoardimportApiKlient` (Integration der Oberflächenschicht; multipart wie `ImportApiKlient`) — **`trocken` reist immer ausdrücklich mit**.
  - `Task<ApiErgebnis<Boardimportbericht>> Importiere(Boardimportauftrag auftrag, Stream datei)`

**Kein Interface** für die fünf Operationen: je Aufgabe genau eine Implementation (C25). **Keine tote Flexibilität** (C24): kein Löschweg, keine Konfliktstrategie, kein Zusammenführungsschalter ohne Aufrufer.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Boards.razor` (+ `.razor.css`) | zweiter Knopf `Board importieren` im Seitenkopf, Ablegefläche, Vorschau, Bericht; die Liste wird nach dem Anlegen über das vorhandene `LadeListeNeu` neu geholt |
| `Program.cs` (WebApi) | `BoardimportEndpunkte` registrieren, `BoardimportService` und `BoardimportRepository` verdrahten |
| `Program.cs` (Blazor) | `BoardimportApiKlient` registrieren |
| `FehlervertragTests` | nimmt die neue Route auf |
| `BoardsSeite` (E2E-Seitenobjekt) | wächst um Knopf, Ablegefläche, Vorschau und Anlegeknopf |

**Nicht geändert:** `Boardexport` und die vier übrigen Exportverträge, `BoardexportService`, `BoardexportRepository`, `Exportdatei`, `ExportEndpunkte`, `WbsImportService`, `WbsImportRepository`, `WbsImportEndpunkte`, `Importanfrage`, `Importbericht`, `BoardService`, `BoardRepository`, `Nichtgefunden`, `Doppelt`, `Stillgelegt`, `Ereignisdrehscheibe`, alles unter `Persistenz/Migrationen/`.

### Wireframe

`Dokumentation/Wireframes/D0001.dc.html:664-670` markiert Dateiwahl, Vorschau und Bericht ausdrücklich als **Lücke** dieses Slice („I0038 · I0039 · Ablauf nicht gezeichnet") und sagt zugleich, dass der Ablauf des WBS-Imports (`D0008`) **nicht** abgeschrieben wird. Das Bild ist **Entwurfsquelle, nie Kriterienquelle**; **kein Akzeptanzkriterium dieser Anforderung ist aus ihm abgeleitet**. Für den Seitenkopf mit „+ Board anlegen" gilt der gezeichnete Zustand der Boardliste als Gestaltungsvorgabe; die Ablegefläche folgt der Form, die `Import.razor` und `Kartendetail.razor` schon führen.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md`; jeder Test verifiziert eine echte Zustandsänderung (Skill `test-ehrlichkeit`).

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Boarddateileser.Lies` — gültige Datei, kein JSON, abgeschnittene Datei, leere Datei, gültiges JSON ohne Kopf; `DateOnly` und `DateTimeOffset` kommen richtig an.
- `Fassungspruefung.Pruefe` — bekannte Fassung geht durch; fremde Fassung nennt gefundene **und** erwartete Zahl; fremder Anwendungsname wird zurückgewiesen.
- `Geschlossenheitspruefung.Pruefe` — **alle acht** Verweisarten je einzeln offen, der Befund nennt Zeilenart, Feld und Nummer; eine geschlossene Datei geht durch.
- `Boardimportzaehlung.Zaehle` — die zehn Zahlen an der Beispieldatei, dazu die leere Datei (Board ohne Karten) und die Karte ohne Klasse.
- `Namensdubletten.Finde` — Name vorhanden, Name nicht vorhanden, zwei gleiche Namen in der Datei, Groß-/Kleinschreibung.
- `Nummernabbildung` — jede alte Nummer trifft ihre neue; verschiedene Tabellen mit derselben alten Nummer laufen nicht zusammen.
- `BoardimportService.Importiere` gegen ein **Test-Repository** — im trockenen Lauf wird **keine** Schreibmethode gerufen (das Repository beweist es, statt dass der Test es glaubt); `BoardId` ist `null`; die Zahlen des schreibenden Laufs sind dieselben wie die der Vorschau; jede Zurückweisung stoppt vor dem Schreiben.

**`KanbanC.Blazor.Tests`:** `BoardimportApiKlient` — `trocken` reist in beiden Schritten mit, die Feldnamen stimmen mit der Route überein, ein 400 wird als `ApiErgebnis` mit Befund gelesen, eine nicht erreichbare WebApi ergibt eine lesbare Meldung. Dazu die Gestaltungsprüfung der neuen Fläche (kein Farb-, Abstands- oder Radiusliteral).

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `BoardimportRepository.SchreibeBoard` — alle 19 Tabellen gefüllt, jede Nummer neu, Kartennummer unverändert, laufender Zeiteintrag bleibt laufend, stillgelegter Kontributor bleibt stillgelegt, Anhangzeile ohne Bytes.
- **Der Lauf als eine Transaktion**: ein Abbruch mitten im Schreiben lässt **kein** neues Board, **keine** Spalte, **keine** Karte und **keinen** neuen Kontributor zurück.
- Die Route: **200** für den trockenen Lauf, **201** mit neuer `BoardId` für den schreibenden, **400** mit Code, Werten und Kompensationsaktion für jede der fünf Zurückweisungslagen; `FehlervertragTests` erweitert; ein Aufruf ohne `trocken` schreibt nichts.
- **Der Beweis, dass die bestehenden Boards bleiben, wie sie waren** (nur Test, kein Produktionscode — Muster `B0539`, `B0525`, `B0017`): eine Datenbank mit **zwei** gefüllten Boards, Import einer fremden Datei, danach **Zeile für Zeile** gegen den vorher abgezogenen Bestand gehalten — Board, Spalte, Karte, Kartenklasse, Zuordnung, Zeiteintrag und die fünf n-Listen. **Ehrlich benannt, was sich doch ändert**: `Kontributor` wächst, **keine vorhandene Zeile** wird angefasst. Gegenprobe: das neue Board trägt dieselben Kartennummern wie die Datei und eine andere `BoardId`.
- **Ausleitung und Einlesung meinen dasselbe Format**: ein Board ausleiten, die Datei einlesen, das neue Board wieder ausleiten und beide Dateien bis auf Nummern und Kopfzeitpunkt gegeneinanderhalten.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf **durch beide Slices** — ein Board mit Karte, Kartenklasse, Anhang und Zeiteintrag aufbauen, die Datei über den Menüpunkt aus `B0541` mit `RunAndWaitForDownloadAsync` herunterladen, über die Ablegefläche mit `SetInputFilesAsync` wieder ablegen, die Vorschau lesen, anlegen — und danach dreierlei prüfen: das neue Board steht in der Liste, das alte trägt **unverändert** seine Karten, und die Kartennummer der importierten Karte ist dieselbe wie im Original. Nur so ist bewiesen, dass Ausleitung und Einlesung dasselbe Format meinen.

## Abhängigkeiten

- Abhängig von: **`R00040`** (`I0038` — Board exportieren, **grün**). Der einzige `Braucht`-Eintrag der Interaction, und der einzige, den sie braucht: ohne die Datei gibt es nichts einzulesen. `Boardexport` samt Kopf und Fassungsnummer ist der Vertrag, gegen den hier geprüft wird.
- Setzt außerdem auf (alle grün): **`R00001`** (`I0001` — Board, Spalten, Migrationslauf), **`R00033`** (`I0030` — die Muster `trocken=true` als Vorgabe, multipart-Route mit `DisableAntiforgery`, `BEGIN IMMEDIATE` über einen ganzen Lauf, ISO-Text gegen die Dapper-Lücke), **`R00024`** (Ablegefläche mit Sperre), **`R00005`** (Gestaltungstokens), **`R00003`** (`I0002` — Boardliste und Boardseite, der Verweis nach dem Anlegen).
- Reihenfolge der Features: `F0073` braucht `F0072`, `F0074` braucht `F0073`, `F0075` braucht `F0074` — sonst zeigte der Schirm auf eine Adresse, die nicht antwortet.
- **Blockiert: nichts.** Kein Knoten der WBS führt `I0039` in seiner `Braucht`-Spalte.
- **`D0001` wird mit diesem Slice grün** — `I0039` ist die letzte offene Interaction des Dialogs „Boards führen".

## Umfang

```
Board importieren (I0039) = 18 Bubbles: 17 Standard (19,6h), 1 unklar (2-4h).
Rest: 19,6h klar + 2-4h unklar · 0 von 18 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 18 Bubbles gruen (0 %) · 0 laufen · 18 offen
```

`I0039` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **vier Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0072` Die Boarddatei lesen und pruefen | `B0543`–`B0546` (4) | 4 (1,6h) | 0 | — |
| `F0073` Vorschau vor dem Schreiben | `B0547`–`B0549` (3) | 3 (1,2h) | 0 | `F0072` |
| `F0074` Das Board entsteht mit neuen Nummern | `B0550`–`B0556` (7) | 7 (10,8h) | 0 | `F0073` |
| `F0075` Einstieg auf der Boardliste | `B0557`–`B0560` (4) | 3 (6,0h) | 1 (`B0560`, 2-4h) | `F0074` |

**Warum vier Features:** weil vier Aspekte **getrennt fertig** werden und drei davon ohne Schirm prüfbar sind. `F0072` weist zurück, ohne dass es ein Board oder eine Datenbank braucht; `F0073` zählt und zeigt, ohne etwas zu schreiben; `F0074` schreibt; erst `F0075` macht die Fähigkeit im Browser greifbar. Jede Stufe hat ihre eigene Zusage, und jede lässt sich einzeln beweisen.

**Wo der Aufwand liegt:** im Schreiben (`B0551`, `B0552` mit zusammen 18 Tabellen und je 2,0h), in der Transaktion (`B0554`), der Route (`B0555`), dem Beweis der Unversehrtheit (`B0556`) und im ganzen Schirm (`B0557`–`B0559`). Die acht Prüf- und Zähl-Bubbles sind zusammen 3,2h — sie sind pure Operationen ohne Datenbank. **Nur `B0560` trägt eine Bandbreite**, weil ein E2E-Lauf durch **beide** Slices geht (Download und Upload in einem Test).

**Nach gemessenem Durchsatz ist mit etwa 1,5–2,5 h zu rechnen** (`Schaetzungen/_ist-zeiten.md`: `R00033` lag bei 2,4 h gegen 32–44 h geplant, `R00040` bei 1,5 h gegen 14,4 h). Die Richtwert-Konvention seit `I0004` überschätzt messbar; sie wird **nicht still gekippt** — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0039` und an allen vier Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00040`/`I0038`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Die Nummern der Datei werden nicht behalten — die tragende Entscheidung.** **Angenommen ist**: das Board entsteht neu, weil alle Schlüssel `AUTOINCREMENT` sind und die Datei aus einer anderen Installation kommt; die Nummern zu behalten hieße, vorhandene Zeilen zu überschreiben — genau das, was das Fertig-Kriterium ausschließt. **Der Preis ist benannt**: eine Datei, die in dieselbe Installation zurückgelesen wird, ergibt ein **zweites, unabhängiges Board**. **Nicht geprüft**, ob stattdessen ein Wiedererkennen gewünscht ist — es bräuchte einen installationsübergreifend stabilen Schlüssel, den die Datei nicht trägt, und wäre ein eigener Slice. **Nicht am Menschen geprüft.**
- **Kontributoren werden neu angelegt und nicht zusammengeführt.** **Angenommen ist**: Migration 006 hat kein `UNIQUE` auf `Name`, ein Namensabgleich wäre eine Vermutung, und eine stille Falschzuschreibung von Arbeit, Zeiten und Kommentaren könnte keine spätere Auswertung zurücknehmen. **Der Preis ist benannt und wird vor dem Schreiben angezeigt**: „Stefan" steht danach zweimal in der Personenliste. **Nicht geprüft**, ob der Mensch stattdessen eine Zuordnung von Hand wünscht (Person der Datei → Person hier) — das wäre ein dritter Schritt im Schirm und ein eigener Slice. **Nicht am Menschen geprüft.**
- **Anhänge bekommen ihre Zeile, aber keine Bytes.** **Angenommen ist**: die Zeile weglassen verschwiege, dass es den Anhang je gab; mit Zeile ist die Lücke erkennbar und reparierbar, und der Bestand beantwortet sie schon mit `Nichtgefunden.Anhangbytes`. **Nicht geprüft**, ob eine importierte Karte ihre Anhangzeilen lieber gar nicht tragen soll. **Nicht am Menschen geprüft.**
- **Kein Ereignis auf dem Live-Kanal.** **Angenommen ist**: der Kanal führt nur `Kartenereignis` und `Importereignis`; ein Boardereignis träfe auch `I0001` und `I0005` und ist ein eigener Slice. **Folge**: ein importiertes Board erscheint bei anderen Betrachtern erst nach dem nächsten Laden. **Nicht am Menschen geprüft.**
- **Der Einstieg steht im Seitenkopf, nicht im ⋯-Menü der Kachel.** **Angenommen ist**: das Menü handelt an einem **vorhandenen** Board, der Import **erzeugt** eines. Das Artboard zeichnet den Ablauf nicht und markiert ihn als Lücke. **Nicht am Menschen geprüft.**
- **Die Prüfung ist streng statt nachsichtig**: eine fremde Fassungsnummer, ein fremder Anwendungsname und ein einziger offener Verweis weisen die **ganze** Datei zurück. **Angenommen ist**: ein halb eingelesenes Board ließe sich nicht mehr entfernen, weil es keinen Löschweg gibt. **Nicht geprüft**, ob ein Teilimport mit Bericht über das Weggelassene gewünscht ist. **Nicht am Menschen geprüft.**
- **Der Bericht trägt keinen Laufkopf** (Zeitpunkt, Urheber, Pfad), anders als der WBS-Import. **Angenommen ist**: der Import hat keinen Urheber — es gibt keinen Kontributor, dem der Lauf gehörte, und ein Pflichtfeld dafür wäre eine Frage im Schirm, die das Fertig-Kriterium nicht kennt. **Nicht am Menschen geprüft.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit; das Schema trägt alles.

## Manuelle Nachbereitungstätigkeiten

- **Nur wenn Anhänge im Spiel waren:** die Dateien der importierten Anhänge liegen nicht vor und müssen an der jeweiligen Karte neu angehängt werden (`DELETE` und `POST` auf die Anhangroute). Der Bericht nennt die Lücke; ein Abruf antwortet mit `anhang-bytes-fehlen` samt genau dieser Kompensation.
- **Nur wenn Namen doppelt entstanden sind:** die Personenliste trägt danach zwei Zeilen gleichen Namens. Sichtbar, benannt und von Hand aufräumbar.

## Warum löst diese Anforderung das Problem? (Pflicht)

Bei `R00001` wurde entschieden, **eine** SQLite-Datei für alle Boards zu führen, und der Preis war die Portabilität; `I0038` hat die eine Hälfte zurückgeholt, indem es ein Board als eigenständige Datei herausschreibt — aber eine Datei, die niemand wieder einlesen kann, ist ein Endpunkt und kein Austauschformat. Wenn **eine** Route diese Datei vollständig prüft, **bevor** sie etwas schreibt, und daraus in **einer** Transaktion ein **neues** Board mit neuen Nummern anlegt (X), dann ist ein Board erstmals zwischen Installationen bewegbar und ein Agent kann ein ganzes Board in einem Aufruf aufbauen, statt 19 Tabellen über sieben Routen in der richtigen Reihenfolge zu füllen (Y) — und die zweite Hälfte der bei `R00001` aufgegebenen Portabilität ist zurück (Z). Der Hebel sitzt genau hier und nicht vorgelagert: an der Datei fehlt nichts, `I0038` hat sie ausdrücklich für diesen Slice mit Fassungsnummer, geschlossenen Verweisen und getrenntem Zählerstand gebaut. Und er sitzt nicht nachgelagert bei einem Zusammenführen oder Wiedererkennen: beides bräuchte einen stabilen Schlüssel, den die Datei nicht trägt, und beides würde still Falsches behaupten — während das Neuanlegen einen sichtbaren, reparierbaren Preis hat und die eine Zusage des Fertig-Kriteriums, „ohne bestehende Boards zu verändern", überhaupt erst einlösbar macht.

## Missing-Docs

- **`AUTOINCREMENT` und das Setzen expliziter Schlüssel in SQLite.** Der Bestand schreibt Schlüssel nie selbst; ob und wie ein `INSERT` mit gesetzter `BoardId` sich zu `sqlite_sequence` verhält, ist nirgends notiert. Für die Begründung der Nummernabbildung wäre eine Notiz hilfreich — auch wenn die Entscheidung fachlich und nicht technisch fällt.
- **Größengrenzen einer multipart-Datei an einer Minimal-API-Route.** Der WBS-Import lädt Textdateien, der Anhang beliebige Dateien; ab welcher Größe eine Boarddatei mit einigen Megabyte gegen Kestrel-Vorgaben (`MultipartBodyLengthLimit`) läuft, ist im Repository nicht belegt.
- **Gemeinsame `JsonSerializerOptions` zwischen Schreiber und Leser.** Dass Leser und Schreiber denselben Optionssatz nutzen müssen, steht nirgends als Regel; die Stelle, an der sie heute definiert sind (`Exportdatei`), trägt keinen Hinweis darauf, dass ein zweiter Nutzer kommt.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Die Nummern der Datei behalten** | Alle Schlüssel sind `AUTOINCREMENT`, und die Datei kommt aus einer fremden Installation: `BoardId 1` und `KontributorId 7` bezeichnen hier etwas anderes. Die Nummern zu behalten hieße, vorhandene Zeilen zu überschreiben — genau das, was „ohne bestehende Boards zu verändern" ausschließt. |
| **Kontributoren nach Namen zusammenführen** | Migration 006 hat **kein `UNIQUE` auf `Name`** — zwei Menschen dürfen gleich heißen. Ein Abgleich schriebe Arbeit, Zeiten und Kommentare eines fremden Boards **still** einer hiesigen Person zu; `D0007` und `D0009` rechnen daraus, und keine Auswertung könnte es zurücknehmen. |
| **Ein dritter Schritt im Schirm: Personen von Hand zuordnen** | Löst dasselbe Problem sichtbar, ist aber ein eigener Slice — und macht aus „nichts zu wählen, nur zu bestätigen" wieder den dreistufigen Ablauf des WBS-Imports, den das Artboard ausdrücklich nicht abgeschrieben haben will. |
| **Ein schon importiertes Board wiedererkennen und aktualisieren** | Bräuchte einen installationsübergreifend stabilen Schlüssel; der `Exportkopf` trägt Anwendung, Fassung, Zeitpunkt und Anhanghinweis — **keine Laufkennung**. Eine Vermutung über den Namen wäre dieselbe stille Falschzuordnung wie bei den Personen. |
| **In ein bestehendes Board hineinimportieren** | Das Fertig-Kriterium sagt „ohne bestehende Boards zu verändern"; ein Hineinimport ist der Gegenentwurf dazu. |
| **`trocken=false` als Vorgabe** | Ein vergessenes Feld legte ein ganzes Board mit allen Karten, Personen und Zeiten an — und **es gibt keinen Weg zurück**: im ganzen Bestand kein `DELETE` auf ein Board, nur Archivieren. Die teure Richtung gehört nie in die Vorgabe. |
| **Erst schreiben, dann prüfen (nachsichtiger Leser)** | Ein halb eingelesenes Board wäre schlimmer als keines, weil es sich nicht entfernen ließe. |
| **Eine unbekannte Fassungsnummer raten und trotzdem lesen** | Schriebe ein halbes Board aus einer Datei, deren Regeln die Anwendung nicht kennt. Der Befund nennt stattdessen gefundene und erwartete Zahl. |
| **Einen eigenen Import-Dateityp neben `Boardexport`** | Zwei Typen für dieselbe Datei liefen bei der nächsten Ergänzung auseinander — Ausleitung und Einlesung meinten dann verschiedenes. |
| **Die Anhangzeilen weglassen, weil die Bytes fehlen** | Verschwiege, dass es den Anhang je gab: nicht erkennbar, nicht reparierbar. Mit Zeile antwortet der Bestand mit `anhang-bytes-fehlen` samt Kompensation. |
| **Anhangbytes doch mitschicken (Base64 oder ZIP)** | Wäre eine Änderung an `I0038`, nicht an diesem Slice — und aus der eigenständigen **Datei** würde ein Archiv. Eigener Slice, sobald ihn jemand bestellt. |
| **Mehrere Transaktionen (Board, dann Karten, dann Zeiten)** | Bricht der Lauf in der Mitte ab, stünde ein halbes Board da, das niemand entfernen könnte. |
| **Ein Boardereignis auf dem Live-Kanal melden** | Der Kanal führt `Kartenereignis` und `Importereignis`; ein Boardereignis träfe auch `I0001` und `I0005` und gehört in einen eigenen Slice. Die Folge — spätere Sichtbarkeit bei anderen Betrachtern — steht als Kriterium benannt. |
| **Den Import in das ⋯-Menü der Kachel legen** | Das Menü handelt an einem vorhandenen Board; der Import erzeugt eines. Es gäbe keine Kachel, an der er hinge. |
| **Ein eigener Importschirm unter eigener Route** | Das Artboard ordnet die Lücke `D0001` zu; ein zweiter Schirm für zwei Schritte wäre ein Umweg über eine Adresse, die niemand verlinkt. |
| **Den WBS-Import um einen Boarddatei-Modus erweitern** | Zwei Aufgaben in einer Route: Knoten in Karten überführen ist nicht ein Board wiederherstellen. Das Artboard sagt es wörtlich. |
| **`PUT /api/boards/{boardId}/import`** | Die Adresse verspräche ein Ziel-Board, das es nicht gibt — und lüde zum Hineinimportieren ein. |

### Bewusst out of scope

- Zusammenführen, Wiedererkennen, Aktualisieren, Hineinimportieren; jede Konfliktstrategie.
- Anhangbytes, Archive, ZIP, Kompression; Strom, Chunking, Fortschritt.
- Ein Löschweg für ein Board (`DELETE /api/boards/{boardId}`) — er fehlt im Bestand und wird hier nicht nachgeholt.
- Ein Ereignis auf dem Live-Kanal; jede Sichtbarkeit ohne Neuladen bei anderen Betrachtern.
- Import mehrerer Boards, Gesamtimport einer Installation, geplante Wiederherstellung.
- Jede Änderung an `I0038` und seinem Dateiformat.
- Schema, Migrationen.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Neue Nummern, keine Übernahme** — die Nummern der Datei leben nur im Lauf, in einer Nummernabbildung. Preis: eine Datei, die in dieselbe Installation zurückgeht, ergibt ein zweites, unabhängiges Board.
2. **Kontributoren werden neu angelegt, nicht zusammengeführt.** Preis: doppelte Namen in der Personenliste — sichtbar gemacht **vor** dem Schreiben.
3. **Anhänge bekommen ihre Zeile, aber keine Bytes**, und der Bericht sagt es. Preis: Anhänge, die sich nicht öffnen lassen, bis jemand sie neu ablegt.
4. **`trocken=true` als Vorgabe**, zwei Schritte im Schirm, nichts vom WBS-Import abgeschrieben.
5. **Route `POST /api/boards/import`** ohne `boardId` in der Adresse; multipart mit `[FromForm] trocken` und `DisableAntiforgery`.
6. **Fassung und Anwendungsname werden geprüft, bevor geschrieben wird**; eine fremde Fassung wird zurückgewiesen, nicht geraten.
7. **Jede Nummer der Datei muss in derselben Datei stehen** — ein offener Verweis weist die ganze Datei zurück.
8. **Der ganze Lauf ist eine Transaktion.**
9. **Kein Ereignis auf dem Live-Kanal** — ein importiertes Board erscheint bei anderen erst nach dem nächsten Laden.
10. **Keine Migration** — das Schema trägt alles.
11. **Der Knopf steht im Seitenkopf von `/boards`**, nicht im ⋯-Menü der Kachel.
12. **Der Bericht trägt keinen Laufkopf** — der Import hat keinen Urheber.
13. **Ein Dateityp und ein Optionssatz**: gelesen wird `Boardexport` mit denselben Serialisierungsoptionen, mit denen `Exportdatei` schreibt.
14. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `D0001.dc.html:664-670` markiert Dateiwahl, Vorschau und Bericht als Lücke; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet**.
