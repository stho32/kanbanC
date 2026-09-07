---
id: R00035
status: Neu
datum: 2026-09-07
---

# R00035: Import-Ergebnis sehen

## Beschreibung

Nach einem Schreiblauf ist ablesbar, **was er getan hat**: der dritte Schritt zeigt statt einer Prosazeile den Kopf des Laufs, alle fünf Zahlen und eine Zeile je Knoten mit Marke, Kartennummer und Grund — übersprungene mit ihrer Kennung, verwaiste dahinter —, und jede Zeile mit Karte führt auf sie. Dieselbe Auskunft steht dem Agenten in der **201-Antwort** zur Verfügung, denn sie **ist** der Bericht; ein neuer Endpunkt entsteht nicht.

Zahlt ein auf: [Vision](R00000-vision.md) — „Aufgaben aus mehreren Quellen": der Import ist die erste und wichtigste, und ein Vorgang, dessen Wirkung man hinterher nicht nachlesen kann, ist keiner, den ein Mensch oder ein Agent guten Gewissens anstößt.

**Der Befund, der diesen Slice trägt: der Bericht ist fast fertig gebaut — aber nur die Vorschau zeigt ihn.**

| Was schon steht | Wo |
|---|---|
| Fünf Bilanzzahlen (angelegt · geändert · unverändert · übersprungen · nicht mehr in der Datei) | `Import.razor:138-144`, **Schritt 2** |
| Zeilen mit Marken `+ ~ ? !`, Kennung, Wirkung und Grund | `Import.razor:120-134`, **Schritt 2** |
| `Importbericht` mit fünf Zahlen, `Importzeile` mit `Kennung`, `Wirkung`, `Grund`, `Kartennummer` | `KanbanC.Contracts/Import/` |
| **Schritt 3** | `Import.razor:163-171` — **eine** Prosazeile („n Karten angelegt, m geändert, k unverändert") und der Weg zum Board |

**Es fehlen genau drei Dinge**, und dieser Slice ist die Summe dieser drei:

1. **Übersprungen und verwaist kommen in Schritt 3 nicht vor** — obwohl das Fertig-Kriterium „übersprungen" **ausdrücklich nennt**. Schritt 3 zeigt heute drei der fünf Zahlen und keine einzige Zeile.
2. **Die Kartennummer der angelegten Karten fehlt im Bericht** — systembedingt, nicht aus Versehen: `WbsImportService.Bilanziere` rechnet den Bericht **vor** `_importRepository.Schreibe(...)` (`WbsImportService.cs:131-140`), und `Schreibe` gibt nur ein `int` zurück (`IWbsImportRepository.cs:23`). An einer `Angelegt`-Zeile steht `Kartennummer: null`.
3. **Der Weg von der Zeile zur Karte fehlt** — `Importzeile` trägt keine `KarteId`, und die Kartenseite hängt an `/karten/{KarteId:long}` (`Kartendetail.razor:1`).

**Der Slice trägt beide Seiten.** Anders als `I0022` (nur API) und `I0026` (nur Oberfläche) hat er auf beiden eine Lücke: die 201-Antwort ist unvollständig, und der Schirm zeigt nicht, was sie trägt.

## Geschäftlicher Nutzen

Ein WBS-Import schreibt in einem Zug Dutzende Karten — auf der echten Datei 41 bei Interaction-Schnitt. Wer ihn ausgelöst hat, sieht heute danach **eine Zahl** und einen Knopf zum Board. Was übersprungen wurde und warum, welche Nummer eine neue Karte bekommen hat, welche Karte nicht mehr in der Datei steht: alles das hat der Lauf gerechnet, gezeigt wird es nur in der Vorschau — also **bevor** er lief, als Ankündigung.

Damit fehlt genau die Auskunft, die man nach einem Massenvorgang braucht: **hat er getan, was angekündigt war?** Übersprungene Zeilen sind der wunde Punkt — ein Import ist nicht alles-oder-nichts, und eine übersprungene Zeile, die im Ergebnis nicht auftaucht, ist eine stille Auslassung derselben Sorte, die `R00024` behoben hat.

Für den Agenten gilt dasselbe auf der Vertragsseite: seine 201-Antwort nennt an einer angelegten Zeile keine Nummer und keine Id. Er weiß, dass 41 Karten entstanden sind, aber nicht **welche** — und kann deshalb keine davon anfassen, ohne das Board erneut abzufragen und die Zuordnung selbst zu raten.

## Funktionale Anforderungen

- Der Schreiblauf gibt je angelegter Karte **Dateiverweis, `KarteId` und Kartennummer** zurück, nicht nur die Anzahl.
- Der Dienst trägt diese Werte **nach dem Schreiben** in die schon gerechneten `Angelegt`-Zeilen des Berichts nach; die Bilanzzahlen bleiben unverändert.
- `Importzeile` trägt die **`KarteId` neben** der Kartennummer; beide sind leer, wo aus der Zeile nie eine Karte wurde.
- Wiedererkannte und **verwaiste** Zeilen nennen ebenfalls Nummer **und** `KarteId`.
- Der `Importbericht` trägt einen **Laufkopf**: Zeitpunkt des Laufs, Urhebernummer, Urhebername und Pfad der Datei.
- Schritt 3 zeigt Laufkopf, **alle fünf** Zahlen und **eine Zeile je Knoten** mit Marke, Kennung, Nummer, Wirkung und Grund — übersprungene inbegriffen, verwaiste dahinter.
- Die Zeilenliste ist **eine** Komponente, die Schritt 2 und Schritt 3 einsetzen.
- Jede Zeile mit `KarteId` führt auf `/karten/{KarteId}`; Zeilen ohne Karte tragen keinen Weg.
- Der ganze Bericht lässt sich **als Text in die Zwischenablage** nehmen.
- Die Vorschau (`trocken=true`) nennt an `Angelegt`-Zeilen **weiterhin keine** Nummer und keine `KarteId`.

## Nicht-funktionale Anforderungen

- **Keine zweite Abfrage:** die Nummern entstehen im Schreiblauf selbst — `OrdneKartenklasseZu` hält den vergebenen Zählerstand schon in der Hand, und `Kartennummer.Aus(praefix, stand)` steht im selben Repository (`WbsImportRepository.cs:266`). Rechenbeispiel: 41 angelegte Karten kosten **null** zusätzliche Lesevorgänge.
- **Kein neues Schema, keine Migration, kein neues Paket, kein neuer Endpunkt.** Was der Bericht nennt, entsteht im selben Lauf.
- **Der Deckel hält:** bricht der Schreiblauf ab, gibt es keinen Bericht, sondern eine Zurückweisung — die Transaktion aus `B0421`/`B0447` bleibt unberührt.
- **Fehlervertrag (Hausregel):** jede Zurückweisung nennt Grund mit Werten und Kompensationsaktion, auch bei 404. Dieser Slice fügt keine neue Zurückweisung hinzu.
- **Die Zwischenablage darf fehlen:** der Rückfall ist der Normalfall, nicht die Zugabe — `Pfadkopie`/`Kopierergebnis` behandeln das seit `R00020` und werden wiederverwendet, nicht nachgebaut.

## Akzeptanzkriterien

### Der Bericht nennt, was entstanden ist (`F0058`)

Fertig-Kriterium des Features, wörtlich: *„Der Schreiblauf antwortet mit einem Bericht, in dem jede Zeile mit Karte ihre Kartennummer und ihre KarteId trägt — auch die neu angelegten — und der Kopf des Laufs Zeitpunkt, Urheber und Datei nennt; die fünf Zahlen und die Gründe bleiben, wie `I0031` sie gebaut hat."*

- [ ] `WbsImportRepository.Schreibe(...)` liefert je **angelegter** Karte ihren **Dateiverweis**, ihre **`KarteId`** und ihre **Kartennummer**. Rechenbeispiel: ein Lauf mit 4 Anlagen und 6 Aktualisierungen liefert **4** Einträge, nicht 10 und nicht die Zahl `4`.
- [ ] Der **Schlüssel ist der Dateiverweis** — dieselbe Kupplung, an der `F0055` wiedererkennt. Über ihn und nicht über Reihenfolge oder Titel findet der Dienst die Zeile wieder.
- [ ] `Importzeile` trägt **`KarteId` neben** `Kartennummer`. Beide sind `null` an übersprungenen Zeilen und an der Zeile des Zielboards — aus ihnen wurde nie eine Karte.
- [ ] Eine **wiedererkannte** Zeile (`Geaendert`, `Unveraendert`) trägt Nummer **und** `KarteId` aus dem Iststand.
- [ ] Eine **verwaiste** Zeile trägt Nummer **und** `KarteId` — die Kompensationsaktion lautet „archivieren", und wer archivieren soll, muss hinkommen.
- [ ] Der Bericht trägt einen **Laufkopf** mit `Zeitpunkt`, `Urhebernummer`, `Urhebername` und `Pfad der Datei`. Der Zeitpunkt ist der des **Laufs**, nicht der der Anzeige.
- [ ] Der Laufkopf steht **im Bericht** und nicht in der Oberfläche — sonst hätte der Agent ihn nicht und der kopierte Text auch nicht.
- [ ] Die **fünf Bilanzzahlen ändern sich durch den Nachzug nicht**: nachgetragen wird nur, was vorher unbekannt war. Rechenbeispiel: 4 angelegt vor dem Schreiben, 4 angelegt danach.
- [ ] `POST /api/boards/{boardId}/wbs-import` **ohne** `trocken` antwortet **201** mit Laufkopf, fünf Zahlen und je angelegter Zeile Kartennummer und `KarteId`, **die auf eine wirklich vorhandene Karte zeigen** — nachgewiesen durch einen Abruf der Karte unter dieser `KarteId`.
- [ ] Derselbe Aufruf **mit** `trocken=true` nennt an `Angelegt`-Zeilen **weiterhin keine** Nummer und keine `KarteId` — der Unterschied zwischen „so sähe es aus" und „so ist es jetzt" ist Absicht, keine Lücke.
- [ ] **Kein neuer Endpunkt.** Die 201-Antwort ist der Bericht; ein `GET .../wbs-import/bericht` entsteht nicht.

### Der dritte Schritt zeigt den Bericht (`F0059`)

Fertig-Kriterium des Features, wörtlich: *„Schritt 3 zeigt statt der einen Prosazeile den Kopf des Laufs, alle fünf Zahlen und eine Zeile je Knoten mit Marke, Kartennummer und Grund — übersprungene mit ihrer Kennung und ihrem Grund, verwaiste dahinter; jede Zeile mit Karte führt auf sie, und der ganze Bericht lässt sich als Text mitnehmen."*

- [ ] Schritt 3 zeigt den **Laufkopf**: Zeitpunkt, Urheber, Dateiname.
- [ ] Schritt 3 zeigt **alle fünf** Zahlen, jede auch als **Null**. Heute nennt es drei (`Import.razor:168`); **übersprungen fehlt, obwohl das Fertig-Kriterium der Interaction es ausdrücklich verlangt**, und verwaist ebenso.
- [ ] Schritt 3 zeigt **eine Zeile je Knoten** mit Marke (`+` angelegt, `~` geändert, `?` nicht mehr in der Datei, `!` Zeile mit Grund), Kennung, Kartennummer, Wirkung und Grund.
- [ ] **Übersprungene Zeilen sind hier zum ersten Mal im Ergebnis sichtbar** — mit ihrer Kennung (Knoten-ID, oder Zeilennummer, wo die Zeile keine ID hergab) und ihrem Grund. Rechenbeispiel: eine Datei mit einer Zeile im Status `verworfen` zeigt in Schritt 3 `1 übersprungen` **und** die Zeile mit ihrem Grund.
- [ ] **Verwaiste Zeilen stehen dahinter** — sie haben keine Zeilennummer in der Datei und können nicht einsortiert werden.
- [ ] Die Zeilenliste ist **eine Komponente**, die **Schritt 2 und Schritt 3** einsetzen: derselbe `Importbericht`, dieselben Marken, derselbe Grund. Schritt 3 fügt nur Nummer und Weg hinzu.
- [ ] Jede Zeile mit `KarteId` trägt einen Verweis auf **`/karten/{KarteId}`**; eine Zeile ohne Karte trägt keinen. **In der Vorschau** bleibt eine `Angelegt`-Zeile ohne Weg — die Karte gibt es noch nicht —, eine **wiedererkannte** Zeile führt dort schon hin.
- [ ] Der Weg hängt an der **`KarteId`, nicht an der Nummer**: die Kartenseite ist `/karten/{KarteId:long}`, eine Nummer wäre ein zweiter Suchweg.
- [ ] **„Bericht als Text kopieren"** legt Kopf, fünf Zahlen und je Berichtszeile eine Textzeile mit Marke, Kennung, Nummer und Grund in die Zwischenablage; fehlt die Zwischenablage, greift der bestehende Rückfall aus `Pfadkopie`/`Kopierergebnis`.
- [ ] **E2E:** eine Datei mit einer Zeile im Status `verworfen` einfahren → in Schritt 3 stehen fünf Zahlen und die Zeilen, `1 übersprungen` ist darunter, und dem Verweis einer angelegten Zeile folgen landet auf **ihrer** Karte. Der Sprung ist der Beweis, dass die Nummer eine **wirkliche** Karte meint und kein gerechnetes Etikett.

### Die benannte Änderung an grünem Bestand

- [ ] **`int Schreibe(...)` von `IWbsImportRepository` weicht einem Ergebnis je Anlage.** Das ist die einzige Kompatibilitätsbrücke, die dieser Slice bricht — benannt und erwartet, nicht entdeckt.
- [ ] Mitgezogen werden: **`WbsImportService`** (Aufrufstelle und Nachzug), **`TestWbsImportRepository`** (`KanbanC.BL.Tests/TestHelpers/`) und **`WbsImportRepositoryTests`** (`KanbanC.WebApi.IntegrationTests/Persistenz/Import/`).
- [ ] Ein Rückgabewert „Anzahl" ist danach **überflüssig**, weil die Liste ihn trägt — ein zweiter Weg zur selben Zahl wäre eine zweite Wahrheit.
- [ ] Der **Endpunktvertrag bleibt sonst gleich**: dieselbe Route, dasselbe multipart-Formular, dieselben Statuscodes, dieselbe Ereignisregel (0 angelegt und 0 geändert → kein `Importereignis`).

### Der grüne Bestand bleibt grün

- [ ] Die Zeile **`#import-ergebnis`** bleibt als Überschrift stehen, damit die **drei** E2E-Tests aus `R00033`/`R00034` grün bleiben, die auf „n Karten angelegt" prüfen (`WbsImportE2ETests.cs:102`, `:126`, `:164`).
- [ ] **Schritt 2 verhält sich unverändert**: dieselben fünf Zahlen, dieselben Marken, derselbe Regler, dieselbe Knopfbeschriftung — die Zeilenliste wird herausgehoben, nicht umgebaut.
- [ ] Der Leser aus `F0051`, der Vergleich aus `F0055` und die Ränderprüfung aus `F0057` sind **nicht Gegenstand** dieses Slice und bleiben unberührt.
- [ ] Die Bilanzzahlen der Probeläufe bleiben: erster Lauf auf leerem Board **41 angelegt**, zweiter Lauf auf unveränderter Datei **0 angelegt, 0 geändert, 41 unverändert**.
- [ ] Die Kartenzahlen je Schnittebene bleiben (**9 / 41 / 79 / 445** auf der eingefrorenen Datei), ebenso die Live-Suite aus `R00031`/`R00032`.
- [ ] Alle bestehenden Tests bleiben grün; `TreatWarningsAsErrors` bleibt aktiv.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Archiv der Läufe** — keine Tabelle `Boardimport`, keine Migration, kein `GET .../bericht`, keine Liste vergangener Läufe.
- [ ] **Kein Wiederherstellen beim Wiederkommen:** wer die Seite frisch öffnet, sieht **Schritt 1**. Das bleibt so.
- [ ] **Kein Titel, keine Spalte, keine Teilaufgabenzahl** an der Berichtszeile — bewusste Abweichung von der Artboard-Tabelle (Zustand 5).
- [ ] **Kein Filtern und kein Ausklappen** der Zeilenliste.
- [ ] **Keine Nummer an angelegten Zeilen der Vorschau.**
- [ ] **Kein Rückfluss ins Markdown**, keine Sollzeit an der Karte (`I0033`), kein Löschen oder Archivieren durch den Import.

## Betroffene Verzeichnisstruktur

- **`Source/KanbanC.Contracts/Import/`** — `Importzeile` bekommt die `KarteId`; `Importbericht` bekommt den Laufkopf (als eigenes immutables DTO, C08).
- **`Source/KanbanC.BL/Interfaces/Import/IWbsImportRepository.cs`** — `Schreibe` gibt Anlageergebnisse statt `int` zurück.
- **`Source/KanbanC.BL/Models/Import/`** — neu: das Ergebnis je Anlage (Dateiverweis, `KarteId`, Kartennummer) und seine benannte Collection.
- **`Source/KanbanC.BL/Persistenz/Import/WbsImportRepository.cs`** — sammelt beim Anlegen, was schon in der Hand liegt.
- **`Source/KanbanC.BL/Operations/Import/`** — `Importwirkungsbildner` trägt die `KarteId` an wiedererkannte und verwaiste Zeilen; ein Nachzug-Bauteil verheiratet Bericht und Schreibergebnis über den Dateiverweis.
- **`Source/KanbanC.BL/Integrations/Import/WbsImportService.cs`** — der Nachzug zwischen `Schreibe` und `Erfolg`.
- **`Source/KanbanC.WebApi/Endpunkte/WbsImportEndpunkte.cs`** — der Laufkopf entsteht hier, wo Urheber, Uhr und Anfrage zusammenkommen.
- **`Source/KanbanC.Blazor/Components/`** — die Berichtszeilenliste als eigene Komponente; `Import.razor` setzt sie in Schritt 2 **und** Schritt 3 ein.
- **`Source/KanbanC.Blazor/Services/`** — der Textaufbau des kopierten Berichts; `Pfadkopie`/`Kopierergebnis` unverändert wiederverwendet.
- **Tests** spiegeln die Themenordner: `KanbanC.BL.Tests/Operations/Import/`, `KanbanC.WebApi.IntegrationTests/Api/` und `.../Persistenz/Import/`, `KanbanC.Blazor.Tests/Services/`, `KanbanC.PlaywrightTests/`.

## Technische Überlegungen

### Warum die Nummer nicht schon da ist — und wo sie herkommt

`Bilanziere` rechnet den Bericht **vor** dem Schreiben, und das ist richtig so: Vorschau und Schreiblauf rechnen dieselbe Bilanz, sonst verspräche die eine etwas anderes, als die andere tut (`F0055`). Der Bericht ist also fertig, bevor eine Karte existiert — und deshalb kann er an einer `Angelegt`-Zeile keine Nummer nennen.

**Die Lösung ist ein Nachzug, keine Umstellung.** Zwischen `Schreibe(...)` und der Rückgabe kommt ein Schritt dazu, der die Anlageergebnisse über den **Dateiverweis** den `Angelegt`-Zeilen zuordnet. Das ist **die eine Stelle, an der Vorschau und Bericht auseinandergehen dürfen** — und sie gehen nur dort auseinander, wo vorher schlicht nichts bekannt war.

**Ohne zweite Abfrage.** Das Repository hält beim Anlegen bereits alles in der Hand: `OrdneKartenklasseZu` vergibt den Zählerstand, `Kartennummer.Aus(praefix, stand)` steht sieben Zeilen weiter im selben Typ (`WbsImportRepository.cs:266`, `:448`). Es muss nur zurückgeben, was es ohnehin weiß.

### Warum die `KarteId` neben der Nummer steht und nicht statt ihrer

Beide werden gebraucht, für Verschiedenes:

| | wofür |
|---|---|
| **Kartennummer** (`WBS-32`) | was ein Mensch liest, in einem Kommentar nennt, in eine Mail schreibt |
| **`KarteId`** (`4711`) | was ein **Weg** braucht — die Kartenseite hängt an `/karten/{KarteId:long}` (`Kartendetail.razor:1`) |

Ein Weg über die Nummer wäre ein **zweiter Suchweg** zur Karte: eine Route, die es nicht gibt, oder eine Auflösung, die eine Abfrage kostet und bei zwei Klassen mit gleichem Präfix zweideutig wäre. Die `KarteId` liegt an jeder Stelle schon vor — an der Anlage aus dem `INSERT`, an der Wiedererkennung aus `Karteniststand.KarteId` (seit `B0436` da und bisher nur zum Schreiben gebraucht).

### Der Bericht bleibt flüchtig — mit Ausgang statt Archiv

Das Fertig-Kriterium sagt **„nach dem Import ist ablesbar"**, nicht „jederzeit nachlesbar". Das Artboard führt „ein Verlauf der Läufe" ausdrücklich unter **„nicht gezeichnet und bewusst so"**, weil er keinen WBS-Knoten hätte (`D0008.dc.html:689`). Ein Archiv bräuchte Tabelle, Migration, Endpunkt und Liste — vier Bauteile für eine Fähigkeit, die niemand bestellt hat; das ist ein eigener Slice, keine Nebenwirkung dieses.

**„Ablesbar" heißt hier also:** solange Schritt 3 steht, und solange die Antwort des Laufs in der Hand ist. Wer die Seite frisch öffnet, sieht Schritt 1.

**Der Ersatz für das Archiv ist der Ausgang aus der flüchtigen Anzeige:** „Bericht als Text kopieren". Kein Schema, kein Endpunkt, kein Zustand. Das Muster liegt bereit — die Zwischenablage aus dem Blazor-Kreislauf ist in `B0282` erprobt und in `B0291` eingesetzt (`Pfadkopie`, `Kopierergebnis`), samt Rückfall für den Fall, dass `navigator.clipboard` nicht da ist.

**Und daraus folgt der Laufkopf.** Ein Bericht ohne „welcher Lauf, wann, aus welcher Datei" ist in dem Augenblick wertlos, in dem er den Schirm verlässt — und genau das tut er beim Kopieren. Deshalb steht der Kopf **im Bericht** und nicht in der Oberfläche: sonst hätte der Agent ihn nicht und der kopierte Text auch nicht. Die Uhr wird direkt gelesen (C03), wie schon beim `Importereignis` (`WbsImportEndpunkte.cs:87`).

### Kein neuer Endpunkt — die 201-Antwort **ist** der Bericht

`WbsImportEndpunkte.cs:81` gibt `Results.Created(..., ergebnis.Wert)` zurück; `ergebnis.Wert` ist der `Importbericht`. Der Agent bekommt also bereits heute den Bericht — nur einen unvollständigen. Dieser Slice **vervollständigt ihn**, statt einen zweiten Weg zu derselben Auskunft zu bauen. Ein `GET .../wbs-import/bericht` wäre das Archiv aus dem vorigen Abschnitt und damit dieselbe verworfene Option unter anderem Namen.

Das ist zugleich die eingelöste Zusage **„was die Oberfläche kann, kann die API"** — und sie kommt hier sogar zuerst: `F0058` ist **ohne Schirm** prüfbar, `F0059` setzt darauf auf.

### Derselbe Bau wie die Vorschau — DRY greift, weil es dieselbe Sache ist

Die Zeilenliste steht heute **inline** in Schritt 2 (`Import.razor:120-134`). Sie wird zu einer Komponente, die beide Schritte einsetzen. Das ist kein vorsorgliches Extrahieren, sondern **derselbe** `Importbericht`, **dieselben** Marken (`Importzeilenmarke`), **derselbe** Grund — Schritt 3 fügt nur Nummer und Weg hinzu (C23: DRY nur bei semantischer Äquivalenz; sie liegt hier vor).

Das Artboard sagt es in einem Satz: *„Derselbe Bau wie die Vorschau — und das ist der Punkt: was in Schritt 2 angekündigt war, steht in Schritt 3 mit der Kartennummer daneben."*

### Die bewusste Abweichung vom Artboard

Zustand 5 zeichnet die Berichtstabelle mit **fünf** Spalten: Knoten · Karte · **Titel** · **Spalte** · **Teilaufgaben**. Gebaut werden die ersten beiden plus Wirkung und Grund; die drei anderen bleiben draußen.

**Grund:** das Fertig-Kriterium fragt nach der **Wirkung** („was angelegt, geändert und übersprungen wurde"), nicht nach dem Karteninhalt. Jede der drei Spalten hängte ein Feld an `Importzeile`, das ausschließlich die Anzeige braucht — Titel, Spaltenbezeichnung und Teilaufgabenzahl reisen sonst nirgends mit. Und die Karte steht über den Verweis **einen Klick entfernt**, wo alle drei ohnehin stehen.

Das Artboard ist Entwurfsquelle, nie Kriterienquelle: **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet.** Wer die drei Spalten will, bestellt sie — sie sind dann eine Erweiterung von `Importzeile` und ein eigener Knoten.

### Ablauf

1. **Anfrage annehmen** — unverändert: dieselbe Route, dasselbe multipart-Formular.
2. **Lesen, entwerfen, Iststand holen, Ränder prüfen, vergleichen, bilanzieren** — unverändert aus `F0051`–`F0057`.
3. **Bei `trocken=true` endet der Lauf hier** — 200 mit dem Bericht, an angelegten Zeilen **ohne** Nummer und `KarteId`.
4. **Schreiben** — `WbsImportRepository.Schreibe(anlagen, aktualisierungen, …)` in **einer** Transaktion.
   - 4.1 Je Anlage wird festgehalten, was ohnehin entsteht: `KarteId` aus dem `INSERT`, Zählerstand aus `OrdneKartenklasseZu`, Nummer aus `Kartennummer.Aus(praefix, stand)`.
   - 4.2 Zurück kommt die Liste dieser Ergebnisse, geschlüsselt über den **Dateiverweis**.
5. **Nachziehen** — die `Angelegt`-Zeilen des schon gerechneten Berichts bekommen Nummer und `KarteId`; alles andere bleibt, wie es war.
6. **Laufkopf setzen** — Zeitpunkt des Laufs, Urhebernummer, Urhebername, Pfad der Datei.
7. **Melden und antworten** — unverändert: ein `Importereignis`, wenn angelegt + geändert > 0; **201** mit dem vollständigen Bericht.
8. **Schirm** — Schritt 3 zeigt Kopf, fünf Zahlen und die Zeilenliste; jede Zeile mit `KarteId` verlinkt; „als Text kopieren" nimmt den Bericht mit.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** dieselbe Route `POST /api/boards/{boardId}/wbs-import`, derselbe Schirm `/boards/{BoardId}/import`. **Dieser Slice fügt keinen Einstieg hinzu** — er ändert das Verhalten der vorhandenen. Keine Migration, kein neues Paket, kein neues Projekt.

**In `KanbanC.Contracts/Import`:**
- `Importzeile` (DTO, immutable) — bekommt `long? KarteId` **neben** `string? Kartennummer`.
- `Importbericht` (DTO, immutable) — bekommt den Laufkopf.
- `Importlaufkopf` (DTO, immutable) — Zeitpunkt, Urhebernummer, Urhebername, Pfad der Datei. Eigener Typ statt vier Felder am Bericht, weil er als Ganzes gelesen, angezeigt und kopiert wird.

**In `KanbanC.BL/Models/Import` (neu):**
- `Kartenanlageergebnis` (DTO, immutable) — Dateiverweis, `KarteId`, Kartennummer. Was eine Anlage hinterlässt.
- `Kartenanlageergebnisse` (benannte Collection, C09) — beantwortet „welche Karte entstand für diesen Dateiverweis?".

**In `KanbanC.BL/Operations/Import`:**
- `Berichtsnachzug` (Operation, pure Logik) — Bericht + `Kartenanlageergebnisse` → Bericht, dessen `Angelegt`-Zeilen Nummer und `KarteId` tragen. Ohne Abhängigkeiten und damit ohne Board prüfbar.
- `Importwirkungsbildner` (Operation, vorhanden) — trägt die `KarteId` an wiedererkannte Zeilen und an die Verwaistenzeilen.

**In `KanbanC.BL/Integrations/Import`:**
- `WbsImportService` (Integration, fängt/loggt) — Signatur unverändert; ruft nach `Schreibe` den `Berichtsnachzug` auf.

**In `KanbanC.BL/Interfaces/Import` und `KanbanC.BL/Persistenz/Import`:**
- `IWbsImportRepository` / `WbsImportRepository` (Provider, wirft, IOSP-**Integration** nach Projektregel)
  - `Kartenanlageergebnisse Schreibe(IReadOnlyList<Kartenschreibauftrag> anlagen, IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen, long kartenklasseId, long kontributorId)` — **statt `int`**.

**In `KanbanC.WebApi/Endpunkte`:**
- `WbsImportEndpunkte` — setzt den Laufkopf: Urheber aus der Anfrage, Name aus dem schon geladenen Kontributor, Uhr direkt (C03), Pfad aus `Importanfrage`.

**In `KanbanC.Blazor`:**
- `Importberichtliste` (Razor-Komponente) — nimmt `IReadOnlyList<Importzeile>` und ein Kennzeichen, ob Wege gezeigt werden; von Schritt 2 **und** Schritt 3 eingesetzt.
- `Berichtstext` (Operation) — `Importbericht` → mehrzeiliger Text: Kopf, fünf Zahlen, je Zeile Marke, Kennung, Nummer, Wirkung, Grund.
- `Pfadkopie` / `Kopierergebnis` — **unverändert wiederverwendet**, nicht erweitert.

**Kein Interface** für die neuen Bauteile: je Aufgabe genau eine Implementation (C25). `IWbsImportRepository` besteht fort, weil es schon da ist und `TestWbsImportRepository` daran hängt.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Contracts/Import/Importzeile` | `KarteId` neben `Kartennummer` |
| `Contracts/Import/Importbericht` | Laufkopf |
| `BL/Interfaces/Import/IWbsImportRepository` | **`Schreibe` gibt Anlageergebnisse statt `int` zurück** |
| `BL/Persistenz/Import/WbsImportRepository` | sammelt `KarteId`, Zählerstand und Nummer beim Anlegen |
| `BL/Operations/Import/Importwirkungsbildner` | `KarteId` an wiedererkannten und verwaisten Zeilen |
| `BL/Operations/Import/Importberichtbildner` | reicht den Laufkopf durch |
| `BL/Integrations/Import/WbsImportService` | Nachzug zwischen `Schreibe` und Rückgabe |
| `WebApi/Endpunkte/WbsImportEndpunkte` | Laufkopf setzen |
| `Blazor/Components/Pages/Import.razor` | Schritt 2 setzt die Komponente ein; Schritt 3 bekommt Kopf, fünf Zahlen, Zeilenliste, Kopierknopf |
| `BL.Tests/TestHelpers/TestWbsImportRepository` | neue Signatur |
| `WebApi.IntegrationTests/Persistenz/Import/WbsImportRepositoryTests` | erwartet Anlageergebnisse |
| `WebApi.IntegrationTests/Api/WbsImportEndpunkteTests` | erwartet Laufkopf, Nummer und `KarteId` |
| `Blazor.Tests/Services/ImportApiKlientTests` | die neuen Felder kommen an |

**Nicht geändert:** `Migrationen/` in Gänze, `Frontmatterleser`, `Zeilenzerleger`, `Knotenleser`, `Wbsbaumbildner`, `Zielspaltenwahl`, `Kartenfelder`, `Herkunftsverweis`, `SollIstVergleicher`, `Kartenabbildvergleich`, `Teilaufgabenabgleich`, `Wiedererkennungspruefung`, `Ereignisdrehscheibe`, `Importereignis`, `Importzeilenmarke`, `Pfadkopie`, `Kopierergebnis`.

## Tests

Nach Skill `test-pyramide`, jeder Test nach Skill `test-ehrlichkeit`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `Berichtsnachzug` — eine `Angelegt`-Zeile bekommt Nummer und `KarteId`; eine `Uebersprungen`-Zeile **nicht**; eine Zeile ohne passendes Anlageergebnis bleibt unverändert; die **fünf Bilanzzahlen sind vor und nach dem Nachzug gleich**.
- `Importwirkungsbildner` — wiedererkannte und verwaiste Zeilen tragen `KarteId` neben der Nummer.
- `Berichtstext` — Kopf, fünf Zahlen und je Zeile eine Textzeile; eine übersprungene Zeile steht mit ihrem Grund darin; eine Zeile ohne Nummer nennt keine.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `WbsImportRepository.Schreibe` liefert je Anlage Dateiverweis, `KarteId` und Nummer; 4 Anlagen und 6 Aktualisierungen ergeben **4** Ergebnisse.
- **Die Nummern stimmen mit dem Bestand überein**: jede zurückgegebene `KarteId` findet eine Karte, deren Kartenklassenzuordnung genau die zurückgegebene Nummer trägt — geprüft gegen die Datenbank, nicht gegen die eigene Rechnung.
- `POST` ohne `trocken` → **201** mit Laufkopf und an jeder `Angelegt`-Zeile Nummer und `KarteId`; ein `GET /api/karten/{KarteId}` (bzw. der vorhandene Ladeweg) liefert die Karte.
- `POST` mit `trocken=true` → **200**, an `Angelegt`-Zeilen **weder** Nummer **noch** `KarteId`; die Kartenzahl vor und nach dem Aufruf ist gleich.
- Ein Lauf mit übersprungenen und verwaisten Zeilen → beide tragen ihre Kennung und ihren Grund; die verwaiste trägt zusätzlich Nummer und `KarteId`.
- Der Laufkopf nennt den **Urheber der Anfrage** und den **Pfad der Anfrage**, nicht den Dateinamen allein.
- Die Ereignisregel bleibt: 0 angelegt und 0 geändert → **kein** `Importereignis`.

**`KanbanC.Blazor.Tests`:** `ImportApiKlient` — Laufkopf, `KarteId` und Nummer kommen aus der 201-Antwort an; ein Bericht ohne Laufkopf-Felder bricht den Klienten nicht. `Berichtstext` und `Pfadkopie`-Rückfall: kein Zugriff auf die Zwischenablage → lesbarer Rückfall statt Ausnahme. **Diese Pfade sind über den Browser nicht auslösbar** — genau der Grund, aus dem dieses Testprojekt existiert.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** **ein** Lauf — eine kleine WBS mit **einer Zeile im Status `verworfen`** einfahren, in Schritt 3 die fünf Zahlen und die Zeilen lesen (`1 übersprungen` mit Grund), dem Verweis einer angelegten Zeile folgen und auf **ihrer** Karte landen. Die Testdatei ist eine kleine WBS wie in `B0431`/`B0454`, nicht `kanbanc.md`.

Repositories, DAL-Klassen und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten — `WbsImportRepository.Schreibe` wird über die Integrationsebene geprüft.

## Abhängigkeiten

- Abhängig von: **`R00033`** (WBS-Datei importieren — `I0030`, **grün**; das ist der Knoten der WBS-Spalte `Braucht` an der Interaction) und **`R00034`** (Import wiederholen — `I0031`, **grün**; `Braucht` von `F0058`). Sachlich hängt der Slice an `B0441`: die fünf Zahlen, die vier Wirkungen und das Feld `Kartennummer` an der Zeile stammen von dort, und `B0441` hat wörtlich notiert, `I0032` fülle die Nummer für die übrigen Zeilen und zeige sie. **Beide Vorbedingungen sind grün, der Slice ist frei.**
- Setzt außerdem auf (alle grün): **`R00022`**/**`R00023`** (Kartenklassen und Kartennummern), **`R00017`** (Kartendetailseite `/karten/{KarteId}` als Sprungziel), **`R00020`** (`Pfadkopie`/`Kopierergebnis` als Zwischenablage-Muster), **`R00016`** (Archivieren als Kompensationsaktion an der verwaisten Zeile).
- Blockiert: **nichts** — kein Knoten der WBS führt `I0032` in seiner Spalte `Braucht`.
- **`D0008` wird mit diesem Slice grün.** `I0032` ist die letzte rote Interaction des Dialogs.

## Umfang

```
Import-Ergebnis sehen (I0032) = 11 Bubbles: 10 Standard (10,4h), 1 unklar (2,0–4,0h).
Rest: 10,4h klar + 2,0–4,0h unklar · 0 von 11 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0032` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **zwei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0058` Der Bericht nennt, was entstanden ist | `B0460`–`B0465` (6) | 6 (5,6h) | 0 | `I0031` |
| `F0059` Der dritte Schritt zeigt den Bericht | `B0466`–`B0470` (5) | 4 (4,8h) | 1 (2,0–4,0h) | `F0058` |

**Warum zwei Features:** weil zwei Aspekte **getrennt fertig** werden. `F0058` ist **ohne Schirm** prüfbar — die 201-Antwort trägt alles — und ist damit die eingelöste Zusage „was die Oberfläche kann, kann die API", die hier sogar zuerst kommt. `F0059` zeigt, was `F0058` nennt, und könnte auch dann noch fehlen, wenn die API vollständig ist.

**Die eine unklare Bubble** ist `B0470` (E2E): offen ist, ob die Zeilenliste im Seitenobjekt `ImportSeite` neue Locator braucht oder die der Vorschau nach dem Zusammenlegen trägt.

**Der Slice ist klein, und das gehört in die Zählung.** 11 Bubbles gegen 31 bei `I0030` und 24 bei `I0031` — weil der große Teil schon steht: Bericht, Zahlen, Zeilen, Marken und Gründe sind gebaut, sie stehen nur an der falschen Stelle.

**Nach gemessenem Durchsatz ist mit etwa 0,8–1,4h zu rechnen.** Die Richtwert-Konvention seit `I0004` überschätzt messbar, und das ist inzwischen dreifach belegt (`Schaetzungen/_ist-zeiten.md`):

| Slice | gezählt | gemessen | Faktor |
|---|---|---|---|
| `I0028` | 22,4–37,9h | 1,9h | ~12–20 |
| `I0029` | 15,6–24,0h | 1,0h | ~16–24 |
| `I0030` | 32,0–44,0h | **2,4h** | ~13–18 |

Die Zählung wird trotzdem nicht still gekippt — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0032` und an beiden Features** — dieselbe Form wie bei `R00031`/`I0028`, `R00032`/`I0029`, `R00033`/`I0030` und `R00034`/`I0031`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Bleibt der Bericht flüchtig, oder bekommt er ein Archiv?** — **entschieden: flüchtig, mit Ausgang.** Das Fertig-Kriterium sagt „**nach** dem Import", nicht „jederzeit nachlesbar"; `D0008.dc.html:689` führt „ein Verlauf der Läufe" ausdrücklich unter „nicht gezeichnet und bewusst so — hätte keinen Knoten". Ein Archiv bräuchte Tabelle, Migration, Endpunkt und Liste und ist ein eigener Slice. Der Ersatz ist „als Text kopieren" (`B0469`). **Preis, benannt:** wer die Seite frisch öffnet, sieht Schritt 1 und findet den letzten Lauf nicht wieder. **Nicht am Menschen geprüft.**
- **Bekommt der Agent einen eigenen Endpunkt für den Bericht?** — **entschieden: nein.** Die 201-Antwort **ist** der Bericht (`WbsImportEndpunkte.cs:81`); der Slice macht sie nur vollständig. Ein `GET .../bericht` wäre das Archiv unter anderem Namen. **Nicht am Menschen geprüft.**
- **Nennt die Vorschau an angelegten Zeilen eine Nummer?** — **entschieden: nein.** Sie kann keine nennen, weil es die Karte noch nicht gibt; genau diese Spalte ist der Unterschied zwischen „so sähe es aus" und „so ist es jetzt". **Nicht am Menschen geprüft.**
- **Führt der Weg zur Karte über die Nummer oder die `KarteId`?** — **entschieden: über die `KarteId`.** Die Kartenseite hängt an `/karten/{KarteId:long}`; eine Nummer wäre ein zweiter Suchweg und bei zwei Klassen mit gleichem Präfix zweideutig. Die Nummer bleibt daneben stehen, weil sie das ist, was ein Mensch liest. **Nicht am Menschen geprüft.**
- **Bekommt die Berichtszeile Titel, Spalte und Teilaufgabenzahl wie im Artboard?** — **entschieden: nein.** Das Fertig-Kriterium fragt nach der **Wirkung**, nicht nach dem Karteninhalt; jede der drei Spalten hängte ein reines Anzeigefeld an `Importzeile`, und die Karte steht einen Klick entfernt. **Bewusste Abweichung vom Entwurf, nicht am Menschen abgestimmt.**
- **Gehört der Laufkopf (`B0464`) überhaupt in diesen Slice?** — **entschieden: ja, und er ist die einzige Zutat, die das Fertig-Kriterium nicht verlangt.** Er kommt aus dem Artboard (Zustand 5: „07.09. 14:12 · Stefan · aus kanbanc.md") und ist die praktische Voraussetzung dafür, dass der kopierte Bericht außerhalb des Schirms noch etwas aussagt. **Wer den Slice kleiner will, streicht `B0464`** — die Zahlen und Zeilen des Kriteriums stehen auch ohne ihn. **Nicht am Menschen geprüft.**
- **Wo entsteht der Laufkopf — im Dienst oder im Endpunkt?** — **angenommen: im Endpunkt**, wo Urheber, Uhr und Anfrage ohnehin zusammenkommen und wo schon das `Importereignis` entsteht (`B0375`, `B0434`). Der Dienst bliebe damit frei von der Uhr. **Angenommen, nicht belegt** — es ginge auch im Dienst, und dann wäre der Kopf schon in der Vorschau da.
- **Trägt die Vorschau denselben Laufkopf?** — **angenommen: ja, mit Zeitpunkt und Datei, weil es dieselbe Antwortform ist.** Er ist dort nur weniger nützlich, weil noch nichts geschehen ist. Eine zweite Berichtsform für die Vorschau wäre zwei Wahrheiten. **Angenommen, nicht belegt.**
- **Braucht die Zeilenliste im Seitenobjekt `ImportSeite` neue Locator?** — **offen** (`B0470`). Nach dem Zusammenlegen zu einer Komponente könnten die Locator der Vorschau beide Schritte tragen; ob die Ids dabei eindeutig bleiben, entscheidet der Bau.

## Manuelle Vorbereitungstätigkeiten

- Keine. Kein Schema, keine Migration, kein Paket, keine Konfiguration.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist konkret: ein Importlauf schreibt in einem Zug 41 Karten, und danach steht auf dem Schirm **eine Zahl**. Was übersprungen wurde und warum, welche Nummer eine neue Karte trägt, welche Karte nicht mehr in der Datei steht — alles das hat der Lauf gerechnet und zeigt es nur **vorher**, als Ankündigung in Schritt 2. Wenn der Schreiblauf zurückgibt, was er angelegt hat, und der Dienst diese Werte in den bereits gerechneten Bericht nachzieht (X), dann trägt die 201-Antwort erstmals die vollständige Auskunft über den Lauf (Y), und derselbe Bericht kann in Schritt 3 mit denselben Bauteilen gezeigt werden, mit denen die Vorschau ihn schon zeigt (Z) — inklusive der übersprungenen Zeilen, die das Fertig-Kriterium ausdrücklich verlangt und die heute im Ergebnis nirgends vorkommen. Der Hebel sitzt genau hier und nicht vorgelagert: die Zahlen, die Wirkungen, die Marken und die Gründe sind seit `I0030`/`I0031` gebaut, es fehlt nur die Rückgabe des Schreiblaufs und ihre Anzeige. Und er sitzt nicht nachgelagert: ein Archiv der Läufe würde dieselbe Auskunft haltbar machen, aber nicht **erzeugen** — sie existiert bis heute nicht.

## Missing-Docs

- **Dapper und die zurückgegebene `KarteId` bei Massenanlagen** — der Bestand schreibt hunderte Vorgänge in einer Transaktion; wie die `last_insert_rowid()`-Auswertung sich dabei verhält, wenn sie je Zeile gesammelt statt nur gezählt wird, ist nirgends notiert.
- **Zwischenablage in Blazor Server** — `Pfadkopie` behandelt den Rückfall, aber es fehlt eine Notiz, unter welchen Bedingungen `navigator.clipboard` im LAN-Betrieb ohne TLS überhaupt zur Verfügung steht. Für `B0469` ist das die einzige offene Größe.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Ein Archiv der Läufe** (Tabelle `Boardimport`, Migration, `GET .../bericht`, Liste) | Vier Bauteile für eine Fähigkeit, die kein Fertig-Kriterium verlangt; `D0008.dc.html:689` führt sie ausdrücklich unter „nicht gezeichnet und bewusst so". Wäre ein eigener Slice. |
| **Ein zweiter Endpunkt für den Bericht** | Zweiter Weg zu derselben Auskunft — und ohne Archiv hätte er nichts zu liefern. Die 201-Antwort ist der Bericht. |
| **Den Bericht erst nach dem Schreiben rechnen** | Bräche die Zusage aus `F0055`, dass Vorschau und Schreiblauf dasselbe rechnen: die Vorschau hätte dann eine zweite Rechenstrecke. Der Nachzug ändert nur, was vorher unbekannt war. |
| **Die Nummern nach dem Schreiben nachlesen** | Eine zweite Abfrage über 41 Karten für Werte, die das Repository beim Anlegen schon in der Hand hatte. |
| **Statt `KarteId` die Kartennummer als Sprungziel** | Bräuchte eine Route, die es nicht gibt, oder eine Auflösung, die eine Abfrage kostet und bei gleichem Präfix zweier Klassen zweideutig wäre. |
| **`Kartennummer` durch `KarteId` ersetzen** | Die Nummer ist das, was ein Mensch liest und in Kommentaren nennt; die Id das, was ein Weg braucht. Beide, nicht eine. |
| **Titel, Spalte und Teilaufgabenzahl an der Berichtszeile** (Artboard, Zustand 5) | Drei reine Anzeigefelder an `Importzeile`, für eine Auskunft, die die verlinkte Karte vollständig trägt. |
| **Eine eigene Zeilenliste für Schritt 3** | Dieselbe Sache zweimal: derselbe `Importbericht`, dieselben Marken, dieselben Gründe (C23). |
| **Den Laufkopf in der Oberfläche zusammensetzen** | Dann hätte ihn weder der Agent noch der kopierte Text — und genau dort wird er gebraucht. |
| **`B0464` (Laufkopf) streichen** | Nicht verworfen, sondern **benannt**: er ist die einzige Zutat über das Fertig-Kriterium hinaus. Wer den Slice kleiner will, streicht ihn — dann verliert der kopierte Bericht seinen Bezug. |

### Bewusst out of scope

- Archiv der Läufe, „zuletzt eingefahren"-Zeile, gespeicherte Importdatei.
- Wiederherstellen des letzten Berichts beim erneuten Öffnen der Seite.
- Filter und Ausklappen an der Zeilenliste.
- Titel, Spalte und Teilaufgabenzahl an der Berichtszeile.
- Rückfluss vom Board in die Markdown-Datei (Vision: offene Richtungsfrage).
- Sollzeit an der Karte aus der Spalte `Aufwand` (`I0033`).

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Der Bericht bleibt flüchtig**, und „als Text kopieren" ist der Ersatz für ein Archiv.
2. **Kein neuer Endpunkt** — die 201-Antwort wird vervollständigt.
3. **Die Vorschau nennt an angelegten Zeilen keine Nummer** und keine `KarteId`.
4. **Der Weg zur Karte hängt an der `KarteId`**, die Nummer steht daneben.
5. **Titel, Spalte und Teilaufgabenzahl bleiben draußen** — bewusste Abweichung vom Artboard, Zustand 5.
6. **`B0464` (Laufkopf) ist die einzige Zutat über das Fertig-Kriterium hinaus** und stammt aus dem Artboard.
7. **Der Laufkopf entsteht im Endpunkt** und reist auch in der Vorschau mit.
8. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `Dokumentation/Wireframes/D0008.dc.html`, **Zustand 5** (Schritt 3, Bericht) ist der Verweis für die Gestaltung von `F0059`; kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet — die drei Tabellenspalten, die es zeichnet und die hier nicht gebaut werden, sind der Beleg dafür.
