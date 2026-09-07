---
id: R00034
status: Neu
datum: 2026-09-07
---

# R00034: Import wiederholen

## Beschreibung

Dieselbe WBS-Datei ein zweites Mal einzufahren legt die Karten **nicht erneut** an: der Lauf erkennt die vorhandenen Karten am **Dateiverweis** `pfad#ID` wieder, vergleicht Soll (Datei) gegen Ist (Board) und schreibt nur die Unterschiede. Die Vorschau zeigt ab hier **fünf Zahlen** — angelegt, geändert, unverändert, übersprungen und *nicht mehr in der Datei* — und je Knoten eine Zeile mit seiner Wirkung. Wo die Wiedererkennung **flächig** ausfällt, wird der Lauf vor der Vorschau zurückgewiesen; wo sie **einzeln** ausfällt, entsteht die Karte und der Verdacht steht daneben.

Zahlt ein auf: [Vision](R00000-vision.md) — die Brücke zwischen Planung und Board. `R00033` hat sie gebaut, aber nur in einer Richtung und nur einmal: ein zweiter Lauf verdoppelte bisher das Board. Erst die Wiedererkennung macht den Import zu etwas, das man **wiederholen** darf, und damit aus einer einmaligen Übertragung eine laufende Brücke.

**Die eine Regel dieses Slice ist der Schnitt zwischen zwei Wahrheiten.** Sie ist keine Beigabe des Entwurfs, sondern der Inhalt — und sie steht so im Artboard (`D0008.dc.html`, Zustand 6):

| Die Datei zieht nach | Das Board behält |
|---|---|
| Titel | Spalte und Position |
| Beschreibung | Verantwortlicher · Fälligkeit · Farbe |
| Etikett des Dialogs | Zeiteinträge |
| **Teilaufgaben und ihre Haken** | Kommentare · Anhänge |
| Dateiverweise | Kartennummer · Archivierung · `ErledigtAm` |

Links steht **Abschrift** — die Felder stehen in der Datei, und die Datei ist die Wahrheit über Umfang und Fortschritt. Rechts steht **Arbeit am Board** — Felder, die die Datei nie hatte und aus denen `D0009` Soll-Ist und Burndown rechnet.

**Der Schlüssel ist der Herkunftsverweis, nicht der Titel.** `pfad#ID` (`Dokumentation/Planung/kanbanc.md#I0001`) ist die einzige Kupplung, die `I0030` gelegt hat, und die einzige, die zurückführt. Der Titel `[I0001] Board anlegen` bleibt **Verdachtsmoment** — er ist änderbar, führt nirgendwohin zurück, und zwei Kupplungen nebeneinander wären zwei Wahrheiten.

**Das vierte Fach heißt `Verwaist`, nicht `ZuLoeschen`.** Es bleibt leer: der Import geht in eine Richtung, und aus ihm wird nie gelöscht. Gemeldet wird trotzdem — mit Kartennummer, Spalte, erfasster Zeit und Kommentarzahl und der Kompensationsaktion **archivieren** (`I0014`) daneben.

**`I0032` ist nicht enthalten.** Dieser Slice **erzeugt** den vollständigen Bericht (fünf Zahlen, eine Zeile je Knoten, verwaiste Karten dahinter) und zeigt im Schirm die fünf Zahlen und eine Marke je Zeile. Den ausklappbaren, filterbaren und kopierbaren Bericht als eigene Ansicht trägt `I0032` nach.

## Geschäftlicher Nutzen

Nach `R00033` steht die WBS als Karten auf dem Board — aber nur mit dem Stand des Tages, an dem sie eingefahren wurde. Die Datei lebt weiter: Knoten werden grün, Bubbles kommen dazu, Fertig-Kriterien werden geschärft. Ein zweiter Lauf war bisher keine Aktualisierung, sondern eine Verdopplung; die einzige Alternative war Handarbeit an 41 Karten oder ein leergeräumtes Board.

Damit war die Brücke aus `R00033` faktisch eine Einbahnstraße mit einer einzigen Fahrt. `R00034` macht sie **idempotent**: derselbe Aufruf, beliebig oft, verändert nur das, was sich in der Datei verändert hat. Das ist die Voraussetzung dafür, dass ein Agent den Import überhaupt automatisch fahren darf — ein Vorgang, den man nicht wiederholen kann, ohne Schaden anzurichten, wird nie geplant angestoßen.

## Funktionale Anforderungen

- Der Lauf liest vor der Vorschau den **Iststand** der Karten der gewählten Kartenklasse auf dem Zielboard.
- Karten werden am **Dateiverweis** `pfad#ID` wiedererkannt; der Titel ist kein Schlüssel.
- Soll und Ist werden zu einem gemeinsamen **Kartenabbild** verglichen; das Ergebnis fällt in vier Fächer: angelegt, geändert, unverändert, verwaist.
- Der Bericht trägt **fünf Zahlen** und je Knoten eine Zeile in Dateireihenfolge; verwaiste Karten stehen mit eigenen Zeilen dahinter.
- Ein Lauf **ohne `trocken`** legt nur neue Karten an und zieht an wiedererkannten Karten Titel, Beschreibung, Etiketten und Teilaufgaben samt Haken nach — in **einer** Transaktion.
- Kartennummer, Spalte, Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge und Archivstand bleiben unberührt.
- Weicht der Status des Knotens von der Spalte der Karte ab, wird das als **Grund an der Zeile** gemeldet; die Karte bleibt stehen.
- Ein Lauf **ohne Wirkung** (0 angelegt, 0 geändert) meldet **kein** Importereignis.
- Ein Lauf, dessen Wiedererkennung flächig ausfällt — anderer `pfad`, andere Schnittebene, zwei Karten auf einem Knoten — wird **vor der Vorschau** zurückgewiesen, mit Grund, Werten und Kompensationsaktion.
- Eine einzelne Karte ohne Kupplung wird **angelegt**, und der Dublettenverdacht steht mit der Nummer der ähnlichen Karte an ihrer Zeile.

## Nicht-funktionale Anforderungen

- **Idempotenz:** der dritte Lauf auf unveränderter Datei schreibt **nichts** und meldet alles unverändert — messbar, nicht behauptet (`B0452` fährt die echte Datei zweimal).
- **Alles oder nichts:** Anlegen und Aktualisieren laufen in **einer** Transaktion; bricht ein Schritt ab, steht danach kein Teilergebnis.
- **Größe:** der Iststand wird in **einem** Lesevorgang je Lauf geholt, nicht je Karte. Rechenbeispiel: 41 Karten, ein Lesevorgang — nicht 41.
- **Fehlervertrag (Hausregel):** jede Zurückweisung nennt **Grund mit Werten und Kompensationsaktion**, auch bei 404.
- **Keine stille Korrektur:** jede zurückgenommene Abhakung erscheint als Grund an ihrer Zeile.

## Akzeptanzkriterien

### Vier Fächer statt einem (`F0055`)

- [ ] Der Iststand einer Kartenklasse auf einem Board wird in **einem** Lesevorgang geholt und trägt je Karte: Nummer, Titel, Beschreibung, Etiketten, Teilaufgaben mit Position und Haken, Herkunftsverweis, Spalte, Archivstand, **erfasste Zeit** und **Kommentarzahl**.
- [ ] Soll und Ist werden über ein gemeinsames **`Kartenabbild`** verglichen (Titel, Beschreibung, Etiketten, Teilaufgaben) — beide Seiten behalten daneben, was nur ihnen gehört.
- [ ] Der Vergleicher liefert vier Fächer: **`ZuErstellen`**, **`ZuAktualisieren`**, **`Unveraendert`** und **`Verwaist`**. Rechenbeispiel: Soll `{A,B}`, Ist `{B',C}` mit `B ≠ B'` ergibt 1 zu erstellen (A), 1 zu ändern (B), 0 unverändert, 1 verwaist (C).
- [ ] **Geändert heißt Feld für Feld festgelegt:** zwei Abbilder sind gleich, wenn **Titel**, **Beschreibung**, **Etikettenmenge** und die **ID-tragenden Teilaufgaben mit ihren Haken** übereinstimmen. Reihenfolge der Etiketten spielt keine Rolle (Menge, nicht Liste).
- [ ] **Nie verglichen** und damit nie ein Grund für „geändert": Spalte, Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Kartennummer, Archivstand, `ErledigtAm`. Rechenbeispiel: eine Karte, die von „Bereit" nach „In Arbeit" gezogen wurde und 4:20 erfasste Zeit trägt, ist bei unveränderter Datei **unverändert**.
- [ ] Der **Dateiverweis steht auf keiner der beiden Listen** — er ist der Schlüssel und kein Vergleichsfeld.
- [ ] Der Teilaufgabenabgleich trennt **anzulegen**, **zu ändern**, **unverändert** und **fremd**; Abgleichsschlüssel ist die **ID vorn** (`B0405 …`).
- [ ] Eine Teilaufgabe **ohne ID-Präfix** ist fremd: sie steht außerhalb des Vergleichs, wird nie geändert und nie entfernt. Rechenbeispiel: eine Karte mit 12 ID-tragenden und 2 von Hand angelegten Teilaufgaben hat nach dem Lauf **14**.
- [ ] Ein **Etikett**, das die Datei nicht erzeugen kann, bleibt an der Karte und macht sie nicht „geändert".
- [ ] Der `Importbericht` trägt **fünf** Zahlen: `Angelegt`, `Geaendert`, `Unveraendert`, `Uebersprungen`, `Verwaist`.
- [ ] Jede Zeile trägt ihre **Wirkung** aus `Angelegt`, `Geaendert`, `Unveraendert`, `Verwaist`, `Etikett`, `Teilaufgabe`, `Zielboard`, `Uebersprungen` — und, wo es eine gibt, die **Kartennummer**.
- [ ] Die Zeilen stehen in **Dateireihenfolge**; **verwaiste Karten stehen dahinter** — sie haben keine Zeilennummer in der Datei.
- [ ] Die Zeile einer verwaisten Karte nennt **Kartennummer, Spalte, erfasste Zeit und Kommentarzahl** und die Kompensationsaktion **archivieren** (`I0014`). Beispiel: „`WBS-47` steht nicht mehr in der Datei — unberührt geblieben, in „In Arbeit", 4:20 erfasste Zeit, 2 Kommentare. Wenn sie weg soll: archivieren."
- [ ] `POST /api/boards/{boardId}/wbs-import` mit `trocken=true` auf ein bereits eingefahrenes Board liefert **200** mit **0 angelegt** und **n unverändert** und **schreibt nichts** — die Kartenzahl vor und nach dem Aufruf ist gleich.

### Der zweite Lauf schreibt nur die Unterschiede (`F0056`)

- [ ] Derselbe Aufruf **ohne `trocken`**, zweimal hintereinander, antwortet **201** und lässt **dieselbe Kartenzahl** auf dem Board stehen. Rechenbeispiel: 41 Karten nach dem ersten Lauf, 41 nach dem zweiten — **keine** Dublette.
- [ ] Anlegen und Aktualisieren laufen in **einer** Transaktion: ein erzwungener Fehler mittendrin lässt weder eine neue Karte noch eine halb nachgezogene zurück.
- [ ] Eine wiedererkannte Karte zieht **Titel, Beschreibung, Etiketten und Teilaufgaben samt Haken** nach.
- [ ] **Die Datei gewinnt auch beim Haken.** Wird eine Teilaufgabe am Board abgehakt und ist ihr Knoten in der Datei nicht `gruen`, ist der Haken nach dem nächsten Lauf **zurückgenommen**.
- [ ] **Nie stillschweigend:** jede zurückgenommene Abhakung steht als **Grund an ihrer Zeile**, mit der Kompensationsaktion „setze den Knoten in der Datei auf `gruen`". Rechenbeispiel: 3 am Board gesetzte Haken auf nicht-grünen Knoten ergeben 3 Gründe, nicht eine Sammelmeldung.
- [ ] **Kartennummer und Zählerstand bleiben:** die Kartenklassenzuordnung einer wiedererkannten Karte wird nicht angefasst, `UNIQUE(Kartenklasse, Zaehlerstand)` bleibt unverletzt. Rechenbeispiel: Zählerstand 41 nach dem ersten Lauf, 41 nach einem zweiten ohne neue Knoten; kommen 4 Knoten dazu, steht er danach auf **45** und die Nummern `WBS-01`…`WBS-41` sind unverändert.
- [ ] **Der Zählerstand wächst je *neuer* Karte** — nicht je Karte des Laufs. Das ist die benannte Abweichung von `B0421` aus `R00033`.
- [ ] **Spalte und Position bleiben.** Steht der Knoten in der Datei inzwischen auf `gruen` und die Karte in „In Arbeit", **zieht die Karte nicht um**; die Abweichung erscheint als Grund an ihrer Zeile („Status `gruen`, Karte steht in „In Arbeit"").
- [ ] Ein **dritter** Lauf auf unveränderter Datei meldet **alles unverändert** und schreibt nichts. Auf der eingefrorenen Datei geprüft: zweiter Lauf **0 angelegt, 0 geändert, 41 unverändert**.
- [ ] Ein Lauf mit **0 angelegt und 0 geändert** meldet **kein** Importereignis; jeder andere meldet **eines** mit `Kartenzahl` = angelegt + geändert. Rechenbeispiel: 4 angelegt und 6 geändert ergibt ein Ereignis mit **10**.
- [ ] Der Schirm zeigt die **fünfte Zahl** und je Zeile eine Marke (`+` angelegt, `~` geändert, `?` nicht mehr in der Datei, `!` Zeile mit Grund).
- [ ] Die **Knopfbeschriftung** nennt beide Zahlen („4 anlegen, 6 ändern") statt nur der angelegten; ein Lauf mit 0 angelegt und n geändert ist **nicht** gesperrt — das ist die Korrektur an `Import.razor`, das heute auf `Angelegt == 0` sperrt.
- [ ] **E2E:** importieren, am Board eine Teilaufgabe abhaken und eine Karte in eine andere Bahn ziehen, denselben Import wiederholen → keine Dublette, gleiche Kartennummern, Karte steht noch in ihrer Bahn, Haken zurückgenommen **und gemeldet**.

### Die Ränder der Wiedererkennung (`F0057`)

- [ ] Tragen zwei Karten desselben Boards **denselben Herkunftsverweis**, wird der Lauf **vor der Vorschau** zurückgewiesen — mit **beiden Kartennummern** und dem Weg „Verweis an einer der beiden entfernen (`I0019`) oder eine archivieren (`I0014`)". Grund: welche nachzuziehen wäre, ist nicht entscheidbar.
- [ ] Treffen die **Knoten-IDs** der Anfrage auf vorhandene Karten, die **Pfade** aber nicht, wird zurückgewiesen — mit dem **Pfad des ersten Laufs**, dem Pfad der Anfrage und der Kompensationsaktion „`pfad` auf den Wert des ersten Laufs setzen".
- [ ] Weicht die **Schnittebene** der Anfrage von der Ebene der wiedererkannten Kartenknoten ab, wird zurückgewiesen — mit **beiden Ebenen**, der Kartenzahl und dem Weg über eine **zweite Kartenklasse** (`I0020`).
- [ ] Die vorige Schnittebene wird **aus der Ebene der verwiesenen Knoten abgeleitet**; es gibt **kein** neues Anfragefeld und **kein** Übersteuerungs-Flag.
- [ ] In allen drei Lagen **entsteht keine Karte** und **wird kein Board berührt**; die API antwortet **400** mit Grund, Werten und Kompensationsaktion.
- [ ] Ein **einzelner** vom Menschen entfernter Verweis weist den Lauf **nicht** zurück: die Karte wird angelegt, und an ihrer Zeile steht der **Dublettenverdacht mit der Nummer der ähnlichen Karte** und dem Weg „Dateiverweis `pfad#ID` an der alten Karte nachtragen (`I0019`), dann die neue archivieren".
- [ ] Der **Titel bleibt Verdachtsmoment und wird nie Schlüssel**: er löst den Hinweis aus, aber nie eine Wiedererkennung und nie ein Nachziehen.
- [ ] Rechenbeispiel zur Grenze: auf der eingefrorenen Datei (41 Karten, Interaction-Schnitt) erzeugte ein Lauf mit Bubble-Schnitt **436 neue Karten**, ließe **32** verwaist zurück und erkennte **9** wieder — flächiger Ausfall, deshalb Zurückweisung. Ein einzelner entfernter Verweis erzeugt **eine** Dublette — deshalb Meldung.

### Die benannte Änderung an grünem Bestand

- [ ] **`Importwirkung.Karte` entfällt** zugunsten von `Angelegt`, `Geaendert`, `Unveraendert` und `Verwaist`. Nach `I0030` war jede Karte „Karte"; ab hier ist „Karte" keine Auskunft mehr, sondern die Frage.
- [ ] `Uebersprungen` bleibt daneben — es ist **keins der vier Fächer**, sondern die fünfte Zahl aus `I0030` (Zeilen, die nie eine Karte werden).
- [ ] Die Anpassung ist erwartet und benannt: **`Kartenentwurfsbildner`** (setzt `Angelegt` statt `Karte`), **`KartenentwurfsbildnerTests`**, **`WbsImportEndpunkteTests`**, **`ImportApiKlientTests`** und **`Import.razor`**.
- [ ] Der Endpunktvertrag bleibt sonst gleich: dieselbe Route, dasselbe multipart-Formular, dieselben Statuscodes.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Löschen und kein Archivieren** durch den Import — auch nicht von verwaisten Karten. Das Fach heißt `Verwaist` und nicht `ZuLoeschen`, weil aus ihm nie gelöscht wird.
- [ ] **Kein Umziehen** einer Karte in eine andere Spalte; die Spalte wird weiterhin **nur beim Anlegen** aus dem Status gesetzt.
- [ ] **Kein Rückfluss ins Markdown** — die Vision führt ihn als offene Richtungsfrage.
- [ ] **Kein neues Schema, keine Migration, kein neues Paket.**
- [ ] **Keine Berichtsansicht** mit Filtern, Ausklappen und Kopieren — das ist `I0032`.
- [ ] **Keine Sollzeit an der Karte** (`Aufwand`) — das ist `I0033`.
- [ ] **Kein Archiv der Läufe** und kein Speichern der Importdatei; auch dieser Slice bringt keine Tabelle `Boardimport`.
- [ ] **Keine Wiedererkennung am Titel** und keine zweite Kupplung neben dem Dateiverweis.

### Der grüne Bestand bleibt grün

- [ ] Der **erste** Lauf auf ein leeres Board verhält sich unverändert: 41 Karten, 489 Teilaufgaben, 454 abgehakt, 31 in der Abschlussspalte, `ErledigtAm` am Tag des Laufs.
- [ ] Der Leser aus `F0051` bleibt unberührt: `Frontmatterleser`, `Zeilenzerleger`, `Knotenleser`, `Wbsbaumbildner` und die Probe an der eingefrorenen Datei sind nicht Gegenstand dieses Slice.
- [ ] Die **Kartenzahlen je Schnittebene** aus `I0030` bleiben unverändert (9 / 41 / 79 / 445 auf der eingefrorenen Datei).
- [ ] Die **Live-Suite** aus `R00031` und `R00032` bleibt grün; `Kartenereignis` behält Gestalt und Artnamen, `Importereignis` behält seine Felder.
- [ ] Die **Datumsgruppierung** aus `R00015` bleibt unverändert — `ErledigtAm` wird von einem zweiten Lauf nicht überschrieben.
- [ ] Alle bestehenden E2E-Tests bleiben grün.

## Betroffene Verzeichnisstruktur

- **`Source/KanbanC.BL/Operations/Import/`** — neu: `SollIstVergleicher`, `Kartenabbildvergleich`, `Teilaufgabenabgleich`, `Wiedererkennungspruefung`, `Verwaistengrund`, `Dublettenhinweis`, `Statusabweichung`. Alles pure Logik, alles ohne Board prüfbar. Geändert: `Importberichtbildner`, `Kartenentwurfsbildner`.
- **`Source/KanbanC.BL/Models/Import/`** — neu: `Kartenabbild`, `Karteniststand`, `SollIstVergleichErgebnis`, `Kartenaktualisierungsauftrag`. Geändert: `Kartenschreibauftrag` bleibt für Neuanlagen.
- **`Source/KanbanC.BL/Integrations/Import/`** — `WbsImportService` liest den Iststand, prüft die Ränder, vergleicht und bilanziert vor der Vorschau.
- **`Source/KanbanC.BL/Persistenz/Import/`** — `WbsImportRepository` bekommt `LiesIststand` und schreibt Anlage **und** Aktualisierung in derselben Transaktion.
- **`Source/KanbanC.Contracts/Import/`** — `Importbericht` bekommt die fünfte Zahl, `Importwirkung` die vier Fächer, `Importzeile` die Kartennummer.
- **`Source/KanbanC.Blazor/Components/Pages/Import.razor`** — fünfte Zahl, Marken je Zeile, Knopfbeschriftung aus beiden Zahlen.
- **Tests** spiegeln die Themenordner: `KanbanC.BL.Tests/Operations/Import/`, `KanbanC.WebApi.IntegrationTests/Api/`, `KanbanC.Blazor.Tests/Services/`, `KanbanC.PlaywrightTests/`.

## Technische Überlegungen

### Das Muster wird angewandt, nicht erfunden

`.claude/app-architectures/Common/snippets/SollIstVergleich.md` liegt seit Projektbeginn bereit, und die Projekt-CLAUDE.md nennt es **namentlich für diesen Slice**. Übernommen werden Aufbau und Rollenverteilung wörtlich: ein `SollIstVergleicher` als **reine Operation** ohne Abhängigkeiten, ein Ergebnis mit vier Fächern, ein Dienst als **Integration**, der Repository und Vergleicher zusammenspannt, und Repositories, die das Vergleichbare liefern.

**Drei benannte Abweichungen** — jede mit Grund, keine aus Bequemlichkeit:

1. **Das vierte Fach heißt `Verwaist`, nicht `ZuLoeschen`.** Aus ihm wird nie gelöscht: eine Karte trägt Zeiten, Kommentare und Anhänge, die die Datei nie hatte, und der Import geht laut Vision in eine Richtung. Ein Fach, das eine Löschung im Namen verspricht und keine ausführt, wäre eine **unehrliche Schnittstelle** (C25). Aus demselben Grund entfällt der Punkt „Repository-Methoden für Create/Update/**Delete**" der Muster-Checkliste ersatzlos.
2. **Zwei Typparameter statt einem.** Das Muster verlangt ein gemeinsames DTO für beide Datenquellen; hier sind die Seiten ehrlich verschieden — der Sollentwurf trägt seinen `Wbsknoten`, der Iststand seine `KarteId`, seine Kartennummer, seine Spalte und seine Zeiten. Ein gemeinsames DTO hätte zwei Hälften, die je eine Seite leer lässt. Gemeinsam ist stattdessen das **verglichene** `Kartenabbild` — und genau das ist der Kern der Musteranweisung, nicht der Buchstabe.
3. **Der Schlüsselselektor ist beidseitig verschieden:** links `Herkunftsverweis(pfad, ID)`, rechts der abgelegte `Dateiverweis.Pfad`. Das Muster kennt nur einen Selektor, weil es nur einen Typ kennt.

### Was „geändert" heißt — Feld für Feld, nicht nach Gefühl

| Verglichen | Nie verglichen |
|---|---|
| Titel | Spalte · Position |
| Beschreibung | Verantwortlicher · Fälligkeit · Farbe |
| Etikettenmenge (Menge, nicht Reihenfolge) | Zeiteinträge · Kommentare · Anhänge |
| ID-tragende Teilaufgaben **mit ihren Haken** | Kartennummer · Archivstand · `ErledigtAm` |

**Der Dateiverweis steht auf keiner der beiden Listen** — er ist der Schlüssel. Ein Schlüssel, der zugleich Vergleichsfeld ist, könnte nie „geändert" ergeben, weil ein abweichender Wert schon die Zuordnung verhindert.

**Fremdes bleibt außerhalb.** Ein Etikett, das die Datei nicht erzeugen kann, und eine Teilaufgabe ohne ID-Präfix stehen außerhalb des Vergleichs — sonst wäre jede von Hand ergänzte Karte auf ewig „geändert" und würde bei jedem Lauf zurückgeschrieben.

### Der Preis der Haken-Entscheidung — ausgesprochen, nicht versteckt

Die offene Frage aus dem Entwurf lautete: gewinnt beim **Teilaufgaben-Haken** die Datei oder das Board? Entschieden ist: **die Datei**. Drei Belege, keiner davon Geschmack:

1. Die Projektregel steht **wörtlich** in `CLAUDE.md`: „**Die WBS ist die Fortschrittswahrheit**; weicht eine andere Liste ab, hat die WBS recht." Eine Teilaufgabe ist die Abschrift eines Knotens, ihr Haken die Abschrift seines Status.
2. Das Artboard führt „Teilaufgaben **und ihre Haken**" **einzeln** unter „Die Datei zieht nach" (`D0008.dc.html`, Zustand 6) — der Haken ist dort ausdrücklich genannt, also kein Versehen.
3. Titel, Beschreibung, Etikett und Teilaufgaben waren der Datei bereits zugeschlagen; den Haken davon abzuspalten hieße, **eine Teilaufgabe in zwei Wahrheiten zu zerlegen**.

**Der Preis:** wer am Board abhakt, findet den Haken nach dem nächsten Lauf zurückgenommen. Deshalb **nie stillschweigend** — jede zurückgenommene Abhakung steht als Grund an ihrer Zeile, mit der Kompensationsaktion „setze den Knoten in der Datei auf `gruen`". Das ist dieselbe Sorte stiller Schaden, die `R00024` behoben hat, und sie wird hier nicht neu erzeugt.

### Die Kupplung und ihre Ränder — die Grenze liegt am Schaden

`I0030` hat die Kupplung gelegt und den wunden Punkt gleich mitvererbt: `I0019` lässt den Menschen einen Dateiverweis entfernen. Die Grenze zwischen **Zurückweisen** und **Melden** liegt nicht am Prinzip, sondern am angerichteten Schaden:

- **Flächiger Ausfall** (anderer `pfad`, andere Schnittebene, zwei Karten auf einem Knoten): auf einen Schlag Dutzende bis Hunderte Dubletten. Gemessen an der eingefrorenen Datei: ein Bubble-Lauf auf ein Interaction-Board legt **436** Karten an, lässt **32** verwaist und erkennt **9** wieder. Der Lauf wird **vor der Vorschau** zurückgewiesen, und die Kompensationsaktion ist ein **Feld der Anfrage** (`pfad`) beziehungsweise eine **zweite Kartenklasse**.
- **Einzelner Ausfall** (ein entfernter Verweis): genau **eine** Dublette. Eine Zurückweisung ließe den ganzen Import an einer einzigen Karte scheitern — 40 richtige Karten blieben ungeschrieben, weil eine falsch ist. Die Karte entsteht, der Verdacht steht daneben.
- **Zwei Karten auf einem Knoten** weisen zurück, obwohl es ein Einzelfall ist: hier ist nicht der Schaden groß, sondern die Frage **unentscheidbar** — welche der beiden nachzuziehen wäre, kann niemand raten.

Die **vorige Schnittebene** wird aus der Ebene der verwiesenen Knoten abgeleitet (`#I0001` → Interaction). Kein neues Anfragefeld, kein Übersteuerungs-Flag: ein Feld, das nur dazu da wäre, eine Prüfung abzuschalten, ist tote Flexibilität (C24), und ein zweites abgelegtes Datum über dieselbe Sache wäre eine zweite Wahrheit.

### Kein neues Schema, keine Migration — nachgesehen, nicht vermutet

Wiedererkennung und Nachziehen kommen mit dem vorhandenen Schema aus:

- **`Dateiverweis`** (Migration 015) trägt `Karte`, `Pfad`, `Kontributor`, `Zeitpunkt` und einen **eindeutigen Index `UX_Dateiverweis_Karte_Pfad`**. Der Herkunftsverweis liegt also schon eindeutig je Karte; gesucht wird über `Pfad`.
- **`Etikett`** (011) mit `PRIMARY KEY (Karte, Text)` — Einfügen und Entfernen einzelner Etiketten braucht nichts Neues.
- **`Teilaufgabe`** (012) mit `Text`, `Position`, `Abgehakt` — der Text trägt die ID vorn, also ist der Abgleichsschlüssel schon da.
- **`Kartenklassenzuordnung`** (017) wird von einem zweiten Lauf **nicht angefasst**; `UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand` bleibt damit automatisch unverletzt.

Die Migrationen enden bei `018`, und der `Migrationslaeufer` führt **ohne Journal** jedes Skript bei jedem Start aus. Eine Migration, die es hier nicht braucht, wäre also nicht nur überflüssig, sondern zusätzliches Risiko.

### Ablauf

1. **Anfrage annehmen** — dieselbe Route, dasselbe multipart-Formular wie `I0030`.
2. **Datei lesen und entwerfen** — unverändert aus `F0051`/`F0052`: `Wbsleser` → `Wbsbaum` → `Kartenentwurfsbildner` → `Kartenentwuerfe`.
3. **Iststand lesen** — `WbsImportRepository.LiesIststand(boardId, kartenklasseId)`, **ein** Lesevorgang.
4. **Ränder prüfen — vor der Vorschau**
   - 4.1 Zwei Karten mit demselben Herkunftsverweis → Zurückweisung.
   - 4.2 Knoten-IDs treffen, Pfade nicht → Zurückweisung mit dem Pfad des ersten Laufs.
   - 4.3 Ebene der wiedererkannten Kartenknoten ≠ angefragte Schnittebene → Zurückweisung mit beiden Ebenen.
5. **Vergleichen** — `SollIstVergleicher.Vergleiche(entwuerfe, iststaende)` über `Kartenabbild`.
   - 5.1 `Kartenabbildvergleich.SindGleich` entscheidet geändert/unverändert.
   - 5.2 `Teilaufgabenabgleich` trennt anzulegen / zu ändern / unverändert / fremd.
6. **Bilanzieren** — `Importberichtbildner` baut fünf Zahlen und die Zeilen: Dateizeilen in Dateireihenfolge, verwaiste Karten dahinter, Gründe an ihren Zeilen (zurückgenommener Haken, Statusabweichung, Dublettenverdacht).
7. **Bei `trocken=true` endet der Lauf hier** — 200, nichts geschrieben.
8. **Schreiben** — `WbsImportRepository.Schreibe(anlagen, aktualisierungen, …)` in **einer** Transaktion; wiedererkannte Karten behalten ihre Zuordnung, der Zählerstand wächst nur je neuer Karte.
9. **Melden** — der Endpunkt meldet **ein** `Importereignis` mit `Kartenzahl` = angelegt + geändert, **oder keines**, wenn beide 0 sind. 201.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** dieselbe Route `POST /api/boards/{boardId}/wbs-import` und derselbe Schirm `/boards/{BoardId}/import` — dieser Slice fügt **keinen** Einstieg hinzu, er ändert das Verhalten der vorhandenen. **Keine Migration, kein neues Paket, kein neues Startprojekt.**

**In `KanbanC.Contracts/Import`:**
- `Importbericht` (DTO, immutable) — bekommt `Verwaist` als fünfte Zahl.
- `Importwirkung` (Enum, Wort im JSON) — `Karte` weicht `Angelegt`, `Geaendert`, `Unveraendert`, `Verwaist`; `Etikett`, `Teilaufgabe`, `Zielboard`, `Uebersprungen` bleiben.
- `Importzeile` (DTO, immutable) — bekommt die **Kartennummer** (`string?`, null an Zeilen ohne Karte).

**In `KanbanC.BL/Models/Import` (neu):**
- `Kartenabbild` (DTO, immutable) — Titel, Beschreibung, Etiketten, Teilaufgaben. **Das eine Vergleichbare beider Seiten.**
- `Karteniststand` (DTO, immutable) — `KarteId`, Kartennummer, `Kartenabbild`, Herkunftsverweis, Spalte, Archivstand, erfasste Zeit, Kommentarzahl.
- `Karteniststaende` (benannte Collection) — beantwortet „welche Karte trägt diesen Herkunftsverweis?" und „welcher Verweis liegt doppelt?".
- `SollIstVergleichErgebnis<TSoll, TIst>` — `ZuErstellen`, `ZuAktualisieren` (Soll/Ist-Paare), `Unveraendert`, `Verwaist`; `HatAenderungen`.
- `Kartenaktualisierungsauftrag` (DTO, immutable) — `KarteId` plus die nachzuziehenden Felder und der Teilaufgabenabgleich.

**In `KanbanC.BL/Operations/Import` (neu, alles pure Logik):**
- `SollIstVergleicher<TSoll, TIst>` (Operation) — zwei Schlüsselselektoren und eine Gleichheitsregel → vier Fächer.
- `Kartenabbildvergleich` (Operation) — zwei `Kartenabbild` → gleich/ungleich nach der Feldliste oben.
- `Teilaufgabenabgleich` (Operation) — Entwürfe + vorhandene → anzulegen / zu ändern / unverändert / **fremd**.
- `Wiedererkennungspruefung` (Operation) — Iststände + Anfrage → `Fehlerbefund` oder frei; trägt die drei flächigen Lagen.
- `Verwaistengrund`, `Statusabweichung`, `Dublettenhinweis` (Operationen) — je ein Grund mit Werten und Kompensationsaktion.

**In `KanbanC.BL/Integrations/Import`:**
- `WbsImportService` (Integration, fängt/loggt) — Signatur unverändert; liest zusätzlich den Iststand, prüft die Ränder und vergleicht vor der Vorschau.

**In `KanbanC.BL/Persistenz/Import`:**
- `WbsImportRepository` (Provider, wirft, **IOSP-Integration** nach Projektregel)
  - `Karteniststaende LiesIststand(long boardId, long kartenklasseId)`
  - `int Schreibe(IReadOnlyList<Kartenschreibauftrag> anlagen, IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen, long kartenklasseId, long kontributorId)`

**Kein Interface** für die neuen Bauteile: je Aufgabe genau eine Implementation (C25). `IWbsImportRepository` besteht fort, weil es schon da ist.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Contracts/Import/Importbericht` | fünfte Zahl `Verwaist` |
| `Contracts/Import/Importwirkung` | `Karte` → `Angelegt`/`Geaendert`/`Unveraendert`/`Verwaist` |
| `Contracts/Import/Importzeile` | Kartennummer an der Zeile |
| `BL/Operations/Import/Kartenentwurfsbildner` | setzt `Angelegt` statt `Karte` |
| `BL/Operations/Import/Importberichtbildner` | fünf Zahlen, Gründe an den Zeilen, verwaiste Zeilen dahinter |
| `BL/Integrations/Import/WbsImportService` | Iststand, Ränderprüfung, Vergleich vor der Vorschau |
| `BL/Persistenz/Import/WbsImportRepository` | `LiesIststand`; `Schreibe` nimmt Aktualisierungen an |
| `Blazor/Components/Pages/Import.razor` | fünfte Zahl, Marken je Zeile, Knopfbeschriftung aus beiden Zahlen, Sperre nicht mehr allein an `Angelegt == 0` |
| `BL.Tests/…/KartenentwurfsbildnerTests` | erwartet `Angelegt` |
| `WebApi.IntegrationTests/Api/WbsImportEndpunkteTests` | erwartet die neuen Wirkungen |
| `Blazor.Tests/Services/ImportApiKlientTests` | erwartet `Angelegt` |

**Nicht geändert:** `Migrationen/` in Gänze, `Frontmatterleser`, `Zeilenzerleger`, `Knotenleser`, `Wbsbaumbildner`, `Zielspaltenwahl`, `Kartenfelder`, `Herkunftsverweis`, `KartenRepository`, `KartenklassenRepository`, `Ereignisdrehscheibe`, `Importereignis`.

## Tests

Nach Skill `test-pyramide`, jeder Test nach Skill `test-ehrlichkeit`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):** wie bei `I0030` ist der Löwenanteil **ohne Board** prüfbar.
- `SollIstVergleicher` — Soll leer, Ist leer, gleicher Schlüssel mit anderem Wert, identische Einträge; vier Fächer je einzeln getroffen.
- `Kartenabbildvergleich` — je ein Test **pro Feld** der Vergleichsliste (Titel, Beschreibung, Etikettenmenge, Teilaufgabe, Haken) und je einer, der beweist, dass Spalte, Zeiten, Kommentare, Nummer und Archivstand **nicht** wirken.
- `Teilaufgabenabgleich` — neue ID, entfallene ID, geänderter Text bei gleicher ID, geänderter Haken, **fremde ohne ID-Präfix bleiben unberührt**.
- `Wiedererkennungspruefung` — doppelter Verweis, abweichender Pfad, abweichende Schnittebene; je ein Befund mit Werten und Kompensation. Und die Gegenprobe: derselbe Pfad, dieselbe Ebene → frei.
- `Verwaistengrund` — Kartennummer, Spalte, erfasste Zeit und Kommentarzahl stehen im Text, Kompensationsaktion „archivieren" ebenfalls.
- `Statusabweichung` und `Dublettenhinweis` — Grund trägt Werte und Weg; der Titel löst den Hinweis aus und **nie** eine Zuordnung.
- `Importberichtbildner` — fünf Zahlen; Dateizeilen in Dateireihenfolge, verwaiste dahinter.
- **`B0452` als Probe an der echten Datei:** `Source/KanbanC.BL.Tests/Testdaten/kanbanc-2026-09-07.md` zweimal durch Dienst und Repository — zweiter Lauf **0 angelegt, 0 geändert, 41 unverändert, 0 verwaist**.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- derselbe Aufruf zweimal ohne `trocken` → 201, **41 Karten** auf dem Board, gleiche Kartennummern, Zählerstand unverändert.
- `trocken=true` auf ein eingefahrenes Board → 200, 0 angelegt, n unverändert, **keine Schreibwirkung**.
- geänderte Datei → Titel, Beschreibung, Etikett und Teilaufgaben nachgezogen; Spalte, Position, Zeiten und Kommentare unverändert.
- am Board gesetzter Haken auf nicht-grünem Knoten → nach dem Lauf zurückgenommen **und** als Grund gemeldet.
- **die Transaktion hält**: ein erzwungener Fehler mittendrin lässt weder eine neue noch eine halb aktualisierte Karte zurück.
- die drei Ränder → je **400** mit Grund, Werten und Kompensation; Kartenzahl vor und nach dem Aufruf gleich.
- entfernter Verweis an einer Karte → die neue Karte entsteht, der Hinweis steht an ihrer Zeile, der Lauf ist **nicht** zurückgewiesen.
- Ereignis: 0/0 → **kein** Importereignis; 4 angelegt + 6 geändert → **ein** Ereignis mit `Kartenzahl` 10.
- fremde Etiketten und von Hand angelegte Teilaufgaben stehen nach dem Lauf noch da.

**`KanbanC.Blazor.Tests`:** `ImportApiKlient` — die neuen Wirkungen und die fünfte Zahl kommen an; 400 der drei Ränder werden zu `ApiErgebnis` mit lesbarem Befund; ein Ausfall der WebApi wird zur Ausfallmeldung. Diese Pfade sind über den Browser nicht auslösbar — genau der Grund, aus dem dieses Projekt existiert.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** **ein** Lauf — importieren, am Board eine Teilaufgabe abhaken und eine Karte in eine andere Bahn ziehen, denselben Import wiederholen, und sehen: keine Dublette, Bilanz kippt auf unverändert, Nummern gleich, Karte steht noch in ihrer Bahn, Haken zurückgenommen **und gemeldet**. **Die Testdatei ist eine kleine WBS**, nicht `kanbanc.md`; die große läuft in `B0452`.

Repositories, DAL-Klassen und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten — `WbsImportRepository.LiesIststand` wird über die Integrationsebene geprüft.

## Abhängigkeiten

- Abhängig von: **`R00033`** (WBS-Datei importieren — `I0030`, **grün**). Das ist der eine Knoten der WBS-Spalte `Braucht`; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf (alle grün, die Spalte `Braucht` führt Vorbedingungen, keine Bauplätze): **`R00017`** (Etiketten, `I0015`), **`R00018`** (Teilaufgaben, `I0016`), **`R00021`** (Dateiverweise, `I0019` — die Kupplung **und** ihr wunder Punkt), **`R00022`**/**`R00023`** (Kartenklassen und Nummern), **`R00016`** (Archivieren als Kompensationsaktion, `I0014`), **`R00028`** (erfasste Zeit an der Karte), **`R00019`** (Kommentare), **`R00031`** (Live-Kanal und Drehscheibe).
- Blockiert: **nichts unmittelbar** — kein Knoten der WBS führt `I0031` in seiner Spalte `Braucht`. **`D0008` wird erst grün, wenn auch `I0032` steht.**
- **`I0032`** („Import-Ergebnis sehen") setzt auf dem Bericht auf, den dieser Slice erzeugt: `I0031` liefert die fünf Zahlen, die Wirkungen, die Kartennummern und die Gründe; `I0032` trägt die **Ansicht** nach (ausklappen, filtern, kopieren, wiederfinden). Ohne `I0031` hätte `I0032` nur drei Zahlen und eine Zeile je Knoten ohne Aussage.

## Umfang

```
Import wiederholen (I0031) = 24 Bubbles: 21 Standard (22,8h), 3 unklar (6,0–12,0h).
Rest: 22,8h klar + 6,0–12,0h unklar · 0 von 24 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 24 Bubbles gruen (0 %) · 0 laufen · 24 offen
```

`I0031` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **drei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0055` Vier Fächer statt einem | `B0436`–`B0445` (10) | 10 (13,6h) | 0 | `I0030` |
| `F0056` Der zweite Lauf schreibt nur die Unterschiede | `B0446`–`B0454` (9) | 6 (4,0h) | 3 (6,0–12,0h) | `F0055` |
| `F0057` Die Ränder der Wiedererkennung | `B0455`–`B0459` (5) | 5 (5,2h) | 0 | `F0055` |

**Warum drei Features:** weil drei Aspekte **getrennt fertig** werden. `F0055` ist ohne jede Schreibwirkung prüfbar und trägt allein die Vorschau — dieselbe Lage wie `F0052` bei `I0030`. `F0056` schreibt. `F0057` weist zurück und könnte auch dann noch fehlen, wenn alles andere steht. `F0056` und `F0057` bauen beide auf `F0055` und sind untereinander **unabhängig** — sie könnten parallel laufen.

**Alle drei unklaren Bubbles sitzen in `F0056`** und sind dieselbe Sorte: die Transaktion über Anlage **und** Aktualisierung (`B0447`), die Probe mit der echten Datei über zwei Läufe (`B0452`) und der E2E-Lauf über zwei Importe mit einer Handbewegung dazwischen (`B0454`). `F0055` und `F0057` sind vollständig pure Logik über einem Muster, das im Repository liegt.

Derselbe Vermerk wie bei allen Slices seit `I0005`: die 2h-Richtwerte für UI- und Endpunkt-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles; die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0031` und an allen drei Features** — dieselbe Form wie bei `R00031`/`I0028`, `R00032`/`I0029` und `R00033`/`I0030`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Gewinnt beim Teilaufgaben-Haken die Datei oder das Board?** — **entschieden: die Datei.** Drei Belege: die Projektregel steht wörtlich in `CLAUDE.md`; das Artboard nennt „Teilaufgaben **und ihre Haken**" einzeln unter „Die Datei zieht nach"; und den Haken von der Teilaufgabe abzuspalten zerlegte eine Teilaufgabe in zwei Wahrheiten. **Preis: wer am Board abhakt, findet den Haken zurückgenommen** — deshalb jede Rücknahme als Grund an ihrer Zeile, mit der Kompensationsaktion „setze den Knoten auf `gruen`". **Nicht am Menschen geprüft.**
- **Wird eine fehlende Kupplung zurückgewiesen oder gemeldet?** — **entschieden: beides, getrennt nach Schaden.** Flächig (Pfad, Schnittebene) zurückweisen, weil auf einen Schlag Hunderte Dubletten entstünden; einzeln melden, weil eine Zurückweisung den ganzen Import an einer Karte scheitern ließe. Zwei Karten auf einem Knoten weisen zurück, weil die Frage unentscheidbar ist. **Nicht am Menschen geprüft.**
- **Heißt das vierte Fach `ZuLoeschen` wie im Muster?** — **entschieden: nein, `Verwaist`.** Aus ihm wird nie gelöscht; ein Name, der eine Löschung verspricht, wäre eine unehrliche Schnittstelle (C25). **Nicht am Menschen geprüft.**
- **Ein Typparameter wie im Muster oder zwei?** — **entschieden: zwei.** Die Seiten sind ehrlich verschieden; gemeinsam ist das verglichene `Kartenabbild`. **Preis: eine benannte Abweichung vom Muster, die im Review erklärt werden muss** — deshalb steht sie hier. **Nicht am Menschen geprüft.**
- **Wächst der Zählerstand je Karte oder je neuer Karte?** — **entschieden: je *neuer* Karte.** `B0421` sagte „je Karte", weil es damals nur neue gab. **Weicht bewusst von einer grünen Bubble ab** — eine Bubble ist Entwurf, kein Vertrag. **Nicht am Menschen geprüft.**
- **Woher weiß der Lauf die Schnittebene des ersten Laufs?** — **entschieden: aus der Ebene der verwiesenen Knoten** (`#I0001` → Interaction). Kein neues Feld, kein Übersteuerungs-Flag (C24). **Nicht am Menschen geprüft.**
- **Meldet ein wirkungsloser Lauf ein Ereignis?** — **entschieden: nein.** `Importereignis.Kartenzahl` ist angelegt + geändert; ist beides 0, hat sich nichts bewegt, und jede offene Sicht lüde umsonst neu. **Nicht am Menschen geprüft.**
- **Stehen archivierte Karten im Vergleich?** — **angenommen: ja, wie jede andere.** Sie zu übergehen erzeugte eine zweite Karte für denselben Knoten, und die Wiedererkennung liefe ins Leere. **Angenommen, nicht belegt** — im Bestand ist der Fall nicht entschieden, und das Artboard sagt dazu nichts. Die Umkehrung (archivierte übergehen) wäre nur haltbar, wenn Archivieren zugleich den Dateiverweis löste, was es nicht tut.
- **Was bedeutet die Marke `!` im Schirm?** — **angenommen: eine Zeile mit Grund** (übersprungen oder Hinweis), neben `+` angelegt, `~` geändert, `?` nicht mehr in der Datei. `B0453` nennt vier Marken, das Artboard zeigt drei. **Angenommen, nicht belegt.**
- **Wie erkennt der Dublettenhinweis die „ähnliche" Karte?** — **offen** (`B0458`). Der Titel ist der einzige Kandidat und bleibt ausdrücklich **Verdachtsmoment, nie Schlüssel**; ob voller Titel, ID-Präfix oder Name reicht, entscheidet der Bau. Ein Fehlgriff kostet hier nichts: der Hinweis steht daneben, er wirkt nicht.
- **Wie hält die Transaktion Anlage und Aktualisierung zusammen?** — **offen** (`B0447`). Der Bestand schreibt 684 Vorgänge in einer Transaktion; ob die Aktualisierungen im selben Rutsch oder als zweiter Abschnitt innerhalb derselben Transaktion laufen, ist eine Frage der Umsetzung, keine der Fachlichkeit.
- **Wie prüft der E2E-Lauf zwei Importe mit einer Handbewegung dazwischen ohne feste Pausen?** — **offen** (`B0454`). Gewartet wird auf Zustände, nie auf Zeit.

## Manuelle Vorbereitungstätigkeiten

- Keine. Kein Schema, keine Migration, kein Paket, keine Konfiguration.

## Manuelle Nachbereitungstätigkeiten

- Keine. Wer nach dem ersten Lauf Karten von Hand in andere Bahnen gezogen hat, behält sie dort — das ist die Zusage dieses Slice, keine Nacharbeit.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist konkret: nach `R00033` steht die WBS als Karten auf dem Board, aber jeder zweite Lauf verdoppelte sie — die Brücke trug genau eine Fahrt. Wenn der Lauf die vorhandenen Karten am Dateiverweis wiedererkennt und Soll gegen Ist vergleicht (X), dann schreibt er nur noch die Unterschiede und wird **idempotent** (Y), und damit darf ein Mensch **oder ein Agent** ihn beliebig oft anstoßen, ohne Schaden anzurichten (Z). Genau diese Wiederholbarkeit ist die Voraussetzung dafür, dass die Datei und das Board dauerhaft dasselbe sagen — und ohne sie stünde `D0009` (Soll-Ist, Burndown, Puffer) auf Karten, deren Stand vom Tag des einmaligen Imports ist. Der Hebel sitzt genau hier und nicht vorgelagert (die Kupplung liegt seit `I0030` im Schema, es fehlt nur ihre Auswertung) und nicht nachgelagert (`I0032` zeigt einen Bericht, den es ohne diesen Vergleich gar nicht zu zeigen gäbe).

## Missing-Docs

- **`TypedResults.ServerSentEvents` und Ereignisunterdrückung** — dieser Slice meldet unter einer Bedingung **kein** Ereignis. Ob und wie das im Bestand belegt ist, entstand aus `I0028`/`I0030`; eine Notiz zum Ereignisvertrag (wann entsteht ein Ereignis, wann nicht) fehlt.
- **Dapper und Mengenschreiben in einer Transaktion** — der Bestand schreibt 684 Vorgänge in einer Transaktion, ohne dass irgendwo steht, welche Größenordnung getragen wird. Für `B0447` (Anlage **und** Aktualisierung) ist das die einzige offene Größe.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Wiedererkennung am Titelpräfix `[I0001]`** | Der Titel ist änderbar und führt nirgendwohin zurück; er bliebe eine zweite Kupplung neben dem Dateiverweis und damit eine zweite Wahrheit. Bleibt als **Verdachtsmoment** für den Dublettenhinweis. |
| **Ein eigenes Herkunftsfeld an der Karte** | Bräuchte eine Migration und einen Knoten unter `D0004`; der `Dateiverweis` mit `UX_Dateiverweis_Karte_Pfad` leistet dasselbe und existiert. |
| **Das Muster ohne Abweichung: ein gemeinsames DTO** | Hätte zwei Hälften, die je eine Seite leer lässt (`Wbsknoten` links, `KarteId`/Zeiten/Spalte rechts). Der Musterkern — beide Seiten vergleichen dasselbe — ist mit `Kartenabbild` erfüllt. |
| **Verwaiste Karten archivieren** | Der Import ginge damit rückwärts und entschiede über Karten, die er nie geschrieben hat; Zeiten und Kommentare hängen daran. Gemeldet wird trotzdem, mit `I0014` als Weg. |
| **Karten bei Statusänderung umziehen** | Nähme dem Board eine Aussage, die nur es kennt („Prüfung", „Bereit" haben in der WBS kein Gegenstück). Gemeldet statt gemacht. |
| **Ein Anfragefeld „vorige Schnittebene"** | Tote Flexibilität (C24) und eine zweite Wahrheit über etwas, das aus den Daten ablesbar ist. |
| **Flächigen Ausfall nur melden statt zurückweisen** | Erzeugte auf der echten Datei 436 Karten in einem Zug, bevor jemand die Meldung liest. |
| **Einzelnen Ausfall zurückweisen** | Ließe 40 richtige Karten an einer falschen scheitern. |

### Bewusst out of scope

- Rückfluss vom Board in die Markdown-Datei (Vision: offene Richtungsfrage).
- Archiv der Läufe, gespeicherte Importdatei, „zuletzt eingefahren"-Zeile.
- Die Berichtsansicht mit Filtern und Kopieren (`I0032`).
- Sollzeit an der Karte aus der Spalte `Aufwand` (`I0033`).
- Löschen oder Archivieren durch den Import.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Archivierte Karten stehen im Vergleich wie jede andere.** Angenommen, nicht belegt — siehe „Offene Fragen".
2. **Die Marke `!` steht für eine Zeile mit Grund.** `B0453` nennt vier Marken, das Artboard zeigt drei.
3. **Die Knopfbeschriftung nennt beide Zahlen**, und die Sperre hängt nicht mehr allein an `Angelegt == 0` — sonst wäre ein reiner Aktualisierungslauf über den Schirm nicht auslösbar.
4. **Befund zur Zahl „41 gegen 404" aus der WBS-Notiz:** nachgerechnet an der eingefrorenen Datei `kanbanc-2026-09-07.md` sind es bei Bubble-Schnitt auf einem Interaction-Board **436 neue, 32 verwaiste und 9 wiedererkannte** Karten (445 Karten bei Bubble-Schnitt, 41 bei Interaction-Schnitt, 9 Knoten sind bei beiden Schnitten eine Karte). „41 gegen 404" war eine überschlägige Differenz; die Größenordnung trägt die Entscheidung unverändert, die Kriterien nennen die nachgerechneten Werte.
5. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** Die Tabelle „Die Datei zieht nach / Das Board behält" steht in der Beschreibung als Verweis auf `Dokumentation/Wireframes/D0008.dc.html`, Zustand 6; kein Akzeptanzkriterium ist aus dem Bild abgeleitet.
