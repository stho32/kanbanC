---
id: R00039
status: In Arbeit
datum: 2026-09-08
---

# R00039: Rohdaten über die API abrufen

## Beschreibung

Zwei neue Routen liefern den **Bestand eines Boards als Rohdaten**: `GET /api/boards/{boardId}/karten` gibt **alle** Karten des Boards in einer Antwort — mit Ort, Kartenklasse, Archivmarke und ihren fünf n-Listen —, `GET /api/boards/{boardId}/zeiten` **alle** Zeiteinträge in Beginn-Folge. Beide ohne Seitengröße, ohne Anzeigekürzung, ohne stillen Archivfilter und ohne Folgeaufruf je Karte. Auf `/auswertungen` wird der bisher gesperrte Punkt `Rohdaten über die API` wählbar und **nennt die zwei Aufrufe** — er bedient nichts.

Zahlt ein auf: [Vision](R00000-vision.md) — „Die erfassten Ist-Zeiten und der **vollständige Datenbestand** sind unmittelbar für eigene Auswertungen zugänglich"; der Maßstab steht im Anlass: „eine fremde Cloud-API mit Limits gibt sie nicht her."

**Die eine Regel dieses Slice, wörtlich:**

> *Rohdaten sind die gespeicherten Zeilen ohne Auslegung — vollständig, oder sichtbar gescheitert. Nie stillschweigend unvollständig.*

Daraus folgt alles Übrige ohne Ausnahmeklausel:

| Lage | Folge, ohne Sonderregel |
|---|---|
| **Abschlussspalte über ihrer Anzeigegrenze** | alle Karten kommen mit; die Grenze ist eine **Anzeige**regel des Boards (`Abschlussbahn.Gekuerzt`, `BoardService.cs:48`) und gilt hier nicht — wörtlich dieselbe Regel, die `Kartenleser.cs:70` und `KartenklassenEndpunkte.cs:57` schon führen |
| **archivierte Karte** | kommt mit, **mit Marke**; die Boardantwort lässt sie über `AND a.Karte IS NULL` weg (`Kartenleser.cs:31`) — ein Abruf, der stillschweigend Karten wegließe, sähe für einen Agenten wie ein Erfolg aus |
| **klassenlose Karte** | kommt mit; sie steht heute in **keinem** Ausschnittsabruf — `…/kartenklassen/{id}/karten` lässt sie liegen |
| **laufender Zeiteintrag** | kommt mit, `Ende` bleibt `null` — `Ende is null` heißt „läuft", die Antwortgestalt seit `I0023` |
| **archivierte Karte, stillgelegter Kontributor an einem Zeiteintrag** | kommen mit; ihre Zeit wurde geleistet — dieselben zwei Regeln, die `Zeitenleser` schon führt |
| **Board ohne Karte / ohne Zeiteintrag** | **200 mit leerer Liste**, nicht 404 — wie bei `soll-ist` |
| **großer Bestand** | die Antwort wird **groß**, nicht kurz: 431 Karten sind einige Megabyte in einem Stück, und das ist eine Größe, keine Kürzung |

**Was dieser Slice nicht ist:** kein Schema, keine Migration, kein Schreibweg, keine Rechnung. Zwei Routen, sechs Leseweiten und ein Schirmpunkt, der zwei Pfade nennt.

## Geschäftlicher Nutzen

Die Vision hat für diesen Slice zwei Sätze übrig, und beide sind konkret. Der Anlass: „Für eine eigene Implementation von Burndown-Chart und Critical Chain werden sehr spezielle Daten gebraucht, und zwar schnell; eine fremde Cloud-API mit Limits gibt sie nicht her." Das Zielbild: „Die erfassten Ist-Zeiten und der vollständige Datenbestand sind unmittelbar für eigene Auswertungen zugänglich."

**Heute ist genau das nicht erfüllt — an drei nachgesehenen Stellen.** `GET /api/boards/{boardId}` heißt „das Board", liefert aber die **Anzeige**: die Abschlussspalte ist auf ihre Anzeigegrenze gekürzt, die archivierten Karten fehlen ganz, und die fünf n-Listen einer Karte stehen nur am Kartendetail. Wer die Etiketten von 431 Karten braucht, macht 431 Folgeaufrufe von `GET /api/karten/{karteId}` — das ist die fremde Grenze, die das Motiv ausschließt, nur selbst gebaut. Und **boardweite Zeiten als JSON gibt es nirgends**: am Board hängen die *laufenden*, am Kartendetail die *einer Karte*, als CSV die *einer Kartenklasse*.

Der Wert liegt darin, dass jemand **außerhalb** rechnen kann, ohne die Anwendung zu fragen, was sie für richtig hält. `I0033` und `I0034` beantworten Fragen, die das Board sich selbst stellt — Soll-Ist und Burndown sind Auslegungen. Dieser Slice liefert das, woraus sie entstehen. Wer eigene Auswertungen rechnen will, will die Einträge, nicht deren Auslegung.

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/karten` liefert **alle** Karten des Boards in **einer** Antwort — auch die archivierten und die, die die Anzeigegrenze der Abschlussspalte wegkürzt.
- Je Karte reisen ihr **Ort** (Spalte mit Bezeichnung), ihre **Archivmarke**, ihre **Kartenklasse** (`null` heißt „ohne Klasse") und ihre **fünf Listen**: Etiketten, Teilaufgaben, Kommentare, Anhänge **als Metadaten** und Dateiverweise.
- `GET /api/boards/{boardId}/zeiten` liefert **alle** Zeiteinträge des Boards in Beginn-Folge — laufende ohne Ende, abgeschlossene mit —, je Eintrag `ZeiteintragId`, Karte, Kontributor, Beginn und Ende.
- **Keine Seitengröße**: kein `?limit`, kein `?offset`, kein Standardmaximum, keine Sortierregel der Anzeige, kein Zeitraumparameter und kein Ausschnitt nach Kartenklasse.
- Ein Board **ohne Karten** bzw. **ohne Zeiteintrag** ist 200 mit leerer Liste; ein **unbekanntes** Board wird mit Grund, Werten und Kompensationsaktion zurückgewiesen.
- Auf `/auswertungen` ist `Rohdaten über die API` **wählbar** und zeigt für das gewählte Board die **zwei** Aufrufe mit vollständigem Pfad statt einer Auswertung; der Fuß folgt der Wahl.
- Die **Kartenklassenwahl tritt für diesen Eintrag zurück** — der Bestand ist hier das Board; `Puffer-Verbrauch` bleibt der einzige gesperrte Eintrag.

## Nicht-funktionale Anforderungen

- **Ein Lesevorgang je Board, nicht je Karte.** Sechs Abfragen für ein Board mit 431 Karten, nicht 431 (bzw. 1.293 für die drei Kartenleser des Details). Das ist die Stelle, an der „ohne Limit" gemessen wird.
- **Die Bytes der Anhänge reisen nicht mit** — nur die Metadaten; den Inhalt holt der Aufrufer über die bestehende Anhangroute. Dieselbe Entscheidung, die `Kartendetail` schon trägt.
- **Die Antwort entsteht ganz im Speicher und geht in einem Stück über die Leitung.** Bei einem Board der Größenordnung aus `I0030` — 431 Karten, eine Notiz mit 7.988 Zeichen — sind das einige Megabyte. **Das ist die eine Grenze dieses Slice, und sie wird genannt statt verschwiegen**: der Abruf ist vollständig oder er scheitert sichtbar. Ein Strom als Antwortform wäre eine eigene Entscheidung mit eigenem Slice und ist hier nicht geplant.
- **Keine zweite Wahrheit.** Die Karte behält überall dieselbe Gestalt (`Kartenleser.AlsKarte`); neu ist nur, was um sie herum mitreist. Die Zeiteinträge stehen **nicht auch noch** an der Rohdatenkarte — die `KarteId` verbindet die beiden Antworten.
- Gestaltungswerte ausschließlich aus `gestaltung.css` — keine Farb-, Abstands- oder Radiusliterale.
- **Keine Projektreferenz `KanbanC.Blazor` → `KanbanC.BL`** (Kernregel des Projekts).

## Akzeptanzkriterien

Fertig-Kriterium der Interaction wörtlich: *„Karten, Zeiten und Verläufe sind über die API vollständig und ohne Limit abrufbar."*

Das Kriterium trägt drei Gegenstände und zwei Zusagen. Die Gegenstände sind **Karten** (`F0067`), **Zeiten** (`F0068`) und **Verläufe** — für die es im Bestand keinen dritten Gegenstand gibt; was das Wort hier heißt, steht unten und ist geprüft, nicht angenommen. Die Zusagen sind **vollständig** und **ohne Limit**, und beide sind unten in einzeln prüfbare Sätze zerlegt.

### Das durchgehende Rechenbeispiel

Board 2 „KanbanC — Release 2" mit den Bahnen „Bereit", „In Arbeit" und **„Erledigt" als Abschlussspalte mit Anzeigegrenze 20**. Darauf:

| | Lage | Zweck im Beispiel |
|---|---|---|
| `K1`–`K21` | **21** erledigte Karten in „Erledigt" | eine mehr als die Anzeigegrenze |
| `K22` | eine **archivierte** Karte | die Boardantwort lässt sie weg |
| `K23` | eine Karte **ohne Kartenklasse** | steht in keinem Ausschnittsabruf |
| `K24` | eine Karte mit **je einem Eintrag in allen fünf Listen** | Etikett, Teilaufgabe, Kommentar, Anhang, Dateiverweis |
| `Z1` | ein **abgeschlossener** Zeiteintrag auf `K24` | Beginn und Ende |
| `Z2` | ein **laufender** Zeiteintrag auf `K22` (archiviert) | ohne Ende, auf archivierter Karte |

- [x] `GET /api/boards/2/karten` liefert **24** Karten — `K1`–`K21` vollständig, `K22` **mit Archivmarke**, `K23` mit `Kartenklasse: null`, `K24` mit allen fünf Listen.
- [x] **Gegenprobe**: `GET /api/boards/2` liefert für „Erledigt" weiterhin **20** Karten und **ohne** `K22`. Aus dem Rohdatenabruf ist keine Änderung der Anzeige geworden.
- [x] `GET /api/boards/2/zeiten` liefert **beide** Einträge in Beginn-Folge; `Z2` steht mit darin, `Ende` ist `null`, obwohl `K22` archiviert ist.
- [x] Kein Aufruf trägt `?limit`, `?offset`, `?von`, `?bis` oder `?kartenklasse`; ein mitgegebener unbekannter Abfrageparameter ändert die Antwort nicht.
- [x] `GET /api/boards/999/karten` und `GET /api/boards/999/zeiten` → **404**, Code `board-unbekannt`, Meldung mit der Nummer **999**, Kompensation `GET /api/boards` (`Nichtgefunden.Board`, unverändert genutzt).
- [x] Ein Board **ohne jede Karte** liefert `[]` mit **200**; ein Board **ohne jeden Zeiteintrag** ebenso.

### „Ohne Limit" — vier Sätze, jeder einzeln prüfbar

- [x] **Keine Seitengröße.** Es gibt keinen `?limit`, keinen `?offset` und kein Standardmaximum; die Zahl der gelieferten Karten hängt allein am Bestand.
- [x] **Keine Anzeigekürzung.** Bei 21 Karten in einer Abschlussspalte mit Grenze 20 liefert die Rohdatenroute **21**. `Abschlussbahn.Gekuerzt` läuft an dieser Route nicht.
- [x] **Kein stiller Archivfilter.** Archivierte Karten kommen mit und sind an ihrer **Marke** erkennbar — nicht daran, dass sie fehlen.
- [x] **Kein N+1 beim Aufrufer.** Die fünf n-Listen reisen mit; ein Aufrufer, der Etiketten, Teilaufgaben, Kommentare, Anhänge und Dateiverweise aller Karten will, macht **einen** Aufruf, nicht einen je Karte.
- [x] Und der Preis wird gezeigt, nicht verschwiegen: die Antwort ist bei großem Bestand **groß**. Ein Test, der 21 Karten mit ihren Listen holt, beweist den Weg; die Größenordnung steht in dieser Anforderung, nicht in einem Limit.

### „Verläufe" — was das Wort hier heißt, und was es nicht heißt

- [x] Der **Erledigungsverlauf** ist abrufbar: `ErledigtAm` steht an jeder Karte (`Karteerledigung.ErledigtAm`, die Rohform des Burndowns aus `I0034`) und kommt mit `GET …/karten`.
- [x] Der **Zeitverlauf** ist abrufbar: jeder Zeiteintrag mit Beginn und Ende (die Rohform von Soll-Ist und Zeitexport) kommt mit `GET …/zeiten`.
- [x] **Damit trägt der Abruf jeden Verlauf, den der Bestand kennt** — und keinen erfundenen.
- [x] Es entsteht **keine Route `…/verlaeufe`**, keine Ereignistabelle, keine Migration und keine Schreibpflicht an einer Bewegung. Ein **Bewegungsverlauf** (wer wann welche Karte über welche Grenze bewegt hat) existiert im Bestand nicht und wird hier nicht gebaut; die Adresse steht unter „Offene Fragen".

### Die Karten des Boards (`F0067`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/karten` liefert **alle** Karten des Boards in **einer** Antwort — auch die archivierten (mit Marke) und die, die die Anzeigegrenze der Abschlussspalte in der Boardantwort wegkürzt —, je Karte ihren Ort (Spalte mit Bezeichnung), ihre Kartenklasse und ihre fünf Listen (Etiketten, Teilaufgaben, Kommentare, Anhänge als Metadaten, Dateiverweise); keine Seitengröße, kein `?limit`, kein `?offset`, keine Sortierregel der Anzeige; ein Board ohne Karten ist 200 mit leerer Liste, ein unbekanntes Board wird mit Grund, Werten und Kompensationsaktion zurückgewiesen. Ohne Schirm allein an der Antwort prüfbar."*

- [x] Die Antwort ist eine **flache Liste** von Rohdatenkarten — **keine Hülle mit Zählangabe**: es wird nichts gekürzt, also **ist** die Länge der Liste die Zahl (anders als bei `Spalte.Kartenzahl`, wo sie es nicht ist).
- [x] Jede Rohdatenkarte trägt die **unveränderte** `Karte` aus dem Bestand plus Ort, Archivmarke, Kartenklasse und die fünf Listen — **Zusammensetzung statt Verdopplung**, wie `Klassenkarte` es schon macht.
- [x] Die Kartenklasse reist als **ganzes DTO**; `null` heißt „ohne Klasse".
- [x] **Kein Zeiteintragsfeld an der Rohdatenkarte** — die Zeiten haben ihre eigene Route.
- [x] Anhänge reisen **als Metadaten**; die Bytes holt der Aufrufer über die bestehende Anhangroute.
- [x] Anhang und Dateiverweis bleiben **zwei** Listen: ein Anhang bringt eine Kopie mit, ein Dateiverweis zeigt auf eine Datei, die woanders weiterlebt.
- [x] Die Ordnung ist die **Lage im Board** (`ORDER BY s.Position, k.Position`), nicht die Anzeigeordnung der Abschlussbahn.
- [x] **Die Route ist eine Adressebene, die es noch nicht gab**: `…/spalten/{spalteId}/karten` ist je Spalte, `…/kartenklassen/{kartenklasseId}/karten` je Klasse. Kein Konflikt mit den bestehenden Routen.
- [x] Der Fehlervertragstest (`FehlervertragTests`, aus `B0102`) nimmt die Route auf — sonst schlägt `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` fehl.
- [x] **Ohne Schirm prüfbar**: jedes Kriterium dieser Gruppe ist an der Antwort allein zu zeigen.

### Die Zeiten des Boards (`F0068`)

Fertig-Kriterium wörtlich: *„`GET /api/boards/{boardId}/zeiten` liefert **alle** Zeiteinträge des Boards in Beginn-Folge — laufende ohne Ende, abgeschlossene mit, Einträge auf archivierten Karten und von stillgelegten Kontributoren mit —, je Eintrag `ZeiteintragId`, Karte, Kontributor, Beginn und Ende; keine Seitengröße, kein Zeitraumparameter und kein Ausschnitt nach Kartenklasse; ein Board ohne Zeiteintrag ist 200 mit leerer Liste, ein unbekanntes Board wird mit Grund, Werten und Kompensationsaktion zurückgewiesen. Ohne Schirm allein an der Antwort prüfbar."*

- [x] Die Antwort besteht aus **`Zeiteintrag`-Zeilen ohne neues DTO** — `ZeiteintragId`, `Karte` als Nummer, ganzer `Kontributor`, `Beginn`, `Ende?`.
- [x] **Kein Kartentitel daneben**: den trägt `F0067`; derselbe Titel an zwei Adressen wären zwei Wahrheiten. Die `KarteId` verbindet die beiden Antworten.
- [x] Laufende Einträge kommen **mit**, `Ende` ist `null`; abgeschlossene tragen ihr Ende.
- [x] Einträge auf **archivierten** Karten und von **stillgelegten** Kontributoren fallen nicht heraus.
- [x] Ordnung: `Beginn`, `ZeiteintragId` als Zweitschlüssel; Beginn und Ende als ISO-Text in der Spalte, in C# als `DateTimeOffset`.
- [x] **Kein Zeitraumparameter**, anders als `zeitexport`: dieser Abruf sagt „vollständig" zu; ein `von`/`bis` wäre ein zweiter Ort für die Schnittregel aus `B0504`.
- [x] Fremder Bestand bleibt draußen: Einträge eines anderen Boards stehen nicht in der Antwort.
- [x] Der Fehlervertragstest nimmt auch diese Route auf.

### Der Schirm nennt die zwei Aufrufe (`F0069`)

Fertig-Kriterium wörtlich: *„Auf `/auswertungen` ist `Rohdaten über die API` wählbar und zeigt für das gewählte Board die **zwei** Aufrufe mit vollständigem Pfad statt einer Auswertung; der Fuß folgt der Wahl; die Kartenklassenwahl tritt für diesen Eintrag zurück, weil der Bestand hier das Board ist; `Puffer-Verbrauch` bleibt der einzige gesperrte Eintrag."*

- [x] Der Punkt `Rohdaten über die API` ist **wählbar** — kein gesperrter `span#auswertung-rohdaten` mehr. `Auswertungen.razor:169` führt ihn heute in `NochNichtGebaut`; danach steht dort nur noch `Puffer-Verbrauch`.
- [x] Die Fläche zeigt **zwei** Pfade mit der gewählten Board-Nummer darin: `GET /api/boards/{boardId}/karten` und `GET /api/boards/{boardId}/zeiten`.
- [x] Der **Aufruffuß** (aus `B0501`) führt für diese Wahl erstmals **zwei** Pfade; für die übrigen Wahlen bleibt er einzeilig.
- [x] Die **Boardwahl bleibt** und füllt beide Pfade; die **Kartenklassenwahl tritt zurück** — sie hätte hier keine Wirkung, und ein Bedienelement ohne Wirkung ist eine stille Lüge.
- [x] Der Eintrag **bedient nichts**: **kein Rohdaten-Knopf**, kein Download, kein Abruf aus dem Schirm heraus. Er zeigt die Aufrufe.
- [x] Ohne gewähltes Board zeigt die Fläche keine halben Pfade — der Zustand ist derselbe, den die übrigen Auswertungen für „Bestand ungewählt" schon führen.
- [x] **Kein Gestaltungsliteral** in der neuen Fläche — geprüft wie in `AuswertungsflaecheTests`.

### Der grüne Bestand bleibt grün

- [x] `GET /api/boards/{boardId}` bleibt **unverändert** die Anzeige: gekürzt, ohne archivierte Karten, ohne die n-Listen. **Zwei Zusagen, zwei Ressourcen.**
- [x] `Kartenleser.LiesKartenNachPosition`, `LiesKartenDerSpalte` und `LiesKartenDerKartenklasse` werden **nicht umgebaut**; die neuen Leseformen stehen **daneben**.
- [x] Die fünf vorhandenen `LiesXDerKarte` bleiben; jede bekommt ein `LiesXDesBoards` **neben** sich.
- [x] `Kartendetail` und `GET /api/karten/{karteId}` bleiben, wie sie sind.
- [x] `LiesLaufendeZeiteintraegeDesBoards` bleibt unverändert — die neue Leseform ist **dieselbe Abfrage ohne `AND z.Ende IS NULL`**, nicht ihr Umbau.
- [x] **Keine Migration, keine Schemaänderung, kein Schreibweg.** Alle Tabellen werden nur gelesen.
- [x] `I0036` (Zeitexport) bleibt unberührt; sein JSON-Zwilling `GET …/zeitexport` trägt weiter **keine Zeilen** — er ist bewusst kein zweiter Weg zu den Daten neben diesem Slice.

### Was dieser Slice ausdrücklich nicht tut

- [x] **Keine dritte Route `…/verlaeufe`** — sie hätte keinen Gegenstand.
- [x] **Kein Ereignisjournal**, keine Bewegungstabelle, keine Schreibpflicht am Live-Kanal, kein Nachtragen dessen, was vor ihm geschah.
- [x] **Kein Rohdaten-Knopf, kein JSON-Download aus dem Browser** — das wäre ein zweiter Export neben `I0036`.
- [x] **Keine Rechnung**: keine Summe, kein Band, keine Kurve, keine Anzeigeregel, keine Sortierung nach Erledigungsdatum. Das ist der Unterschied zu `soll-ist` und `burndown`.
- [x] **Kein Ausschnitt**: keine Kartenklasse, kein Zeitraum, kein Archivfilter als Parameter, kein Spaltenfilter.
- [x] **Kein Strom, kein Chunking, keine Kompressionsverhandlung** — die Antwortform ist ein Stück JSON.
- [ ] Kein Board-Export als Datei (`I0038`), kein Puffer-Verbrauch (`I0035`).

## Betroffene Verzeichnisstruktur

Ein neuer Themenordner kommt hinzu: **`Rohdaten`** — in Contracts, BL und Tests. Der Rest wächst an vorhandenen Stellen.

- **Contracts**: `KanbanC.Contracts/Karten` — `Rohdatenkarte` neben `Kartendetail`, dessen Bauform sie folgt.
- **BL**: `KanbanC.BL/Persistenz/Karten` (die boardweiten Leseformen an den fünf vorhandenen Lesern und am `Kartenleser`), `KanbanC.BL/Persistenz/Zeiten/Zeitenleser.cs` (die boardweite Zeitform), `KanbanC.BL/Integrations/Rohdaten` (`RohdatenService`), `KanbanC.BL/Interfaces/Rohdaten` (`IRohdatenRepository`), `KanbanC.BL/Persistenz/Rohdaten` (`RohdatenRepository`).
- **API**: `KanbanC.WebApi/Endpunkte/RohdatenEndpunkte.cs` — **erster Endpunktsatz dieses Themas**, zwei Routen, Adressform wie die übrigen Bestandsrouten.
- **Oberfläche**: `KanbanC.Blazor/Components/Pages/Auswertungen.razor` (+ `.razor.css`), `Components/Auswertungen/` für die Aufruffläche. **Kein API-Klient** — der Schirm ruft nichts ab, er nennt Pfade. **Keine Projektreferenz auf `KanbanC.BL`.**
- **Tests**: `KanbanC.WebApi.IntegrationTests/{Api,Persistenz/Rohdaten}`, `KanbanC.BL.Tests/Integrations/Rohdaten`, `KanbanC.Blazor.Tests/Gestaltung`, `KanbanC.PlaywrightTests` (Seitenobjekt `AuswertungenSeite` wächst um den Aufrufblock).
- **Keine Änderung**: `Persistenz/Migrationen/` — dieser Slice bringt keine Migration mit.

## Technische Überlegungen

### Zwei Routen, keine dritte — und warum eine Zusammenstellung aus dem Bestand nicht trägt

**Geprüft, nicht angenommen.** Der Bestand (Commit `0938283`) führt `GET /api/boards/{id}`, `/api/karten/{karteId}`, `…/spalten/{id}/karten`, `…/kartenklassen/{id}/karten`, `…/soll-ist`, `…/burndown`, `…/zeitexport(.csv)` und `GET /api/ereignisse`. Keine Kombination daraus erfüllt die Zusage:

| Vorhandener Weg | Warum er die Zusage nicht trägt |
|---|---|
| `GET /api/boards/{id}` | **gekürzt** (`Abschlussbahn.Gekuerzt`, `BoardService.cs:48`, Grenze 20 aus `StandardspaltenVorlage.cs:7`), **ohne archivierte** (`AND a.Karte IS NULL`, `Kartenleser.cs:31`), **ohne die n-Listen** |
| `GET /api/karten/{karteId}` | trägt die sieben Listen — aber **je Karte**: 431 Karten sind 431 Folgeaufrufe |
| `…/spalten/{spalteId}/karten` | je Spalte, ohne die n-Listen |
| `…/kartenklassen/{kartenklasseId}/karten` | je Klasse — und **lässt die klassenlosen Karten liegen** |
| `Board.LaufendeZeiteintraege` | nur die **laufenden** |
| `…/zeitexport.csv` | CSV, je **Kartenklasse**, mit Zeitraumschnitt |
| `…/zeitexport` (JSON) | trägt **bewusst keine Zeilen** — `I0036` hat ihn so gebaut, damit er kein zweiter Weg neben diesem Slice wird |

**Boardweite Zeiten als JSON gibt es damit nirgends**, und klassenlose Karten stehen in keinem Ausschnittsabruf. Deshalb zwei neue Routen — und **keine dritte**: `…/verlaeufe` hätte keinen Gegenstand.

### Der Skopus ist das Board, nicht der Kartenbestand

`I0036` hat es vorweggenommen („dessen Routen sind **board**weit und liefern JSON-Rohdaten ohne Kartenklasse"), das Artboard zeichnet `GET /api/boards/2/karten` und `GET /api/boards/2/zeiten`, und `Braucht` steht auf `I0011` und nicht auf `I0021`. Fachlich zwingend ist es ohnehin: eine Kartenklasse ist ein **Ausschnitt**, und Karten ohne Klasse stünden dann in keinem Abruf — „vollständig" verträgt keinen Ausschnitt. Der Board-Skopus ist zugleich die Adressform der übrigen Bestandsrouten.

### Der Leseweg: sechs Abfragen je Board, nicht sechs je Karte

Das ist der Kern des Aufwands und zugleich die Stelle, an der „ohne Limit" gemessen wird.

- `Kartenleser` bekommt eine **boardweite Rohdatenform**: derselbe Schnitt wie `LiesKartenNachPosition`, aber **ohne** `AND a.Karte IS NULL` — die archivierten gehören zu den Rohdaten, und `Kartenarchivierung` liefert statt des Filters die **Marke**. Die Karte entsteht über `Kartenleser.AlsKarte`, damit sie überall dieselbe Gestalt hat.
- Die fünf n-Leser (`Etikettenleser`, `Teilaufgabenleser`, `Kommentarleser`, `Anhangleser`, `Dateiverweisleser`) haben je ein `LiesXDerKarte` und bekommen ein `LiesXDesBoards` **daneben** — derselbe Schnitt über `Karte JOIN Spalte`, gruppiert nach `KarteId`. **Fünf Abfragen je Board statt fünf je Karte**: bei 431 Karten ist das der Unterschied zwischen 5 und 2.155 Abfragen.
- `Zeitenleser` bekommt `LiesZeiteintraegeDesBoards` — `LiesLaufendeZeiteintraegeDesBoards` **ohne** `AND z.Ende IS NULL`. Dieselbe Abfrage, eine Bedingung weniger; deshalb ist `F0068` klein.

Vorbild für „ein Lesevorgang je Bestand" sind `LiesSollIst` (`B0478`) und `LiesZeiteintraege` (`B0503`). SQL nach Skill `sql-stil`.

### Die Vorprüfung sitzt vor dem Lesen, und der Befund liegt vor

**Erst das Board, dann die Karten** — dieselbe Vorprüfung wie `PruefeBestand` im `AuswertungsService` (`B0481`), hier mit **einer** Nummer statt zweien. Der Befund `Nichtgefunden.Board(boardId)` existiert und wird nicht neu erfunden: er nennt Code, die Nummer im Klartext und die Kompensationsaktion `GET /api/boards`. Ein Board **ohne** Karten ist kein Fehler — die leere Liste ist die Antwort, nicht 404, wie bei `soll-ist`.

### Der Vertrag der Rohdatenkarte: Zusammensetzung statt Verdopplung

`Rohdatenkarte` liegt in `KanbanC.Contracts/Karten` neben `Kartendetail` und folgt dessen Bauform: die **unveränderte** `Karte` plus das, was um sie herum gehört. Kein zweiter Kartentyp, keine kopierten Felder — genau das macht `Klassenkarte` bereits vor (`Karte`, `Spalte`, `Spaltenbezeichnung`).

**Keine Hülle mit Zählangabe.** Bei `Spalte.Kartenzahl` ist die Zahl nötig, weil gekürzt wird und die Liste dann nicht mehr die Zahl ist. Hier wird nichts gekürzt — also **ist** die Länge der Liste die Zahl, und ein Zählfeld daneben wäre eine zweite Wahrheit.

**Fünf Listen, zwei mehr als im Bild.** Das Artboard nennt „Etiketten, Teilaufgaben, Dateiverweisen" (`D0009.dc.html:558`). Kommentare und Anhänge kommen dazu: „vollständig" kennt keine Auswahl unter gespeicherten Zeilen, und beide sind boardweit sonst unerreichbar. **Begründete Abweichung vom Entwurf** — eine schlankere Antwort wäre `B0521`/`B0522` und damit rund zwei Bubbles billiger.

### Der Schirm nennt Pfade und ruft nichts ab

`F0069` ist die kleinste Fläche des Dialogs: kein API-Klient, kein Zustand, kein Ladepfad, kein Fehlerpfad. Der Grund, den Punkt trotzdem wählbar zu machen, ist die Ehrlichkeit des Schirms — bliebe er gesperrt, führte die Fläche einen **gebauten** Slice als „noch nicht gebaut". Dieselbe Lage wie `I0022`: die Fähigkeit fängt bei der API an, und **das Bedienelement des Agenten ist der Aufruf**.

### Die Größe der Antwort ist die eine Grenze — und sie wird genannt

Die Antwort entsteht im Speicher und geht in einem Stück heraus. Bei 431 Karten mit Beschreibungen, Kommentaren und fünf Listen sind das einige Megabyte. **Das ist eine Größe, keine Kürzung.** Ein Standardmaximum „zur Sicherheit" wäre genau die fremde Grenze, die das Motiv ausschließt; ein Strom (`IAsyncEnumerable`, NDJSON) wäre eine andere Antwortform mit eigenen Fragen — Abbruch mitten im Strom, Fehler nach dem ersten Byte, Aufrufer, die kein Streaming lesen — und damit ein eigener Slice. Hier gilt: **vollständig oder sichtbar gescheitert**.

### Ablauf

1. **Der Agent** ruft `GET /api/boards/{boardId}/karten`.
   - 1.1 `RohdatenEndpunkte` → `RohdatenService.Karten(boardId)`
   - 1.2 Vorprüfung: Board unbekannt → `Nichtgefunden.Board(boardId)` → **404** mit Grund, Werten, Kompensationsaktion
2. **Ein Lesevorgang je Sorte**, alle in derselben Verbindung
   - 2.1 Karten mit Ort, Archivmarke und Kartenklasse (ohne Archivfilter, `ORDER BY s.Position, k.Position`)
   - 2.2 Etiketten, Teilaufgaben, Kommentare des Boards — je eine Abfrage, gruppiert nach `KarteId`
   - 2.3 Anhänge (Metadaten) und Dateiverweise des Boards — je eine Abfrage
3. **Die Integration setzt zusammen**: je Karte ihre fünf Listen aus 2.2/2.3 → `Rohdatenkarte`
4. **200** mit der flachen Liste — ohne Hülle, ohne Zählfeld, ohne Kürzung
5. **Der Agent** ruft `GET /api/boards/{boardId}/zeiten` — dieselbe Vorprüfung, ein Lesevorgang, 200 mit allen Einträgen in Beginn-Folge
6. **Der Mensch** sieht auf `/auswertungen` unter `Rohdaten über die API` genau diese zwei Pfade — und ruft sie, wenn er will, selbst.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** der neue Endpunktsatz `RohdatenEndpunkte` in `Program.cs`; der Punkt `rohdaten`, der heute in `Auswertungen.razor:169` unter `NochNichtGebaut` steht; die fünf vorhandenen n-Leser und der `Kartenleser` in `KanbanC.BL/Persistenz/Karten`; `Zeitenleser` in `KanbanC.BL/Persistenz/Zeiten`.

**In `KanbanC.Contracts/Karten`** (immutable, C08):
- `Rohdatenkarte` (DTO) — die Karte samt ihrem Ort, ihrer Archivmarke, ihrer Kartenklasse und ihren fünf Listen. Zusammensetzung statt Verdopplung.
  - `Rohdatenkarte(Karte Karte, long Spalte, string Spaltenbezeichnung, Archivierung Archivstand, Kartenklasse? Kartenklasse, IReadOnlyList<string> Etiketten, IReadOnlyList<Teilaufgabe> Teilaufgaben, IReadOnlyList<Kommentar> Kommentare, IReadOnlyList<Anhang> Anhaenge, IReadOnlyList<Dateiverweis> Dateiverweise)`

**In `KanbanC.BL/Interfaces/Rohdaten` und `KanbanC.BL/Persistenz/Rohdaten`:**
- `IRohdatenRepository` / `RohdatenRepository` (Integration, Ressourcenzugriff) — zwei Auskünfte, je **ein** Lesevorgang je Sorte; `null` heißt „dieses Board gibt es nicht".
  - `IReadOnlyList<Rohdatenkarte>? LiesKartenDesBoards(long boardId)`
  - `IReadOnlyList<Zeiteintrag>? LiesZeiteintraegeDesBoards(long boardId)`

**In `KanbanC.BL/Persistenz/Karten`** (Erweiterung der vorhandenen Leser, je eine boardweite Form **neben** der Kartenform):
- `Kartenleser` — `LiesRohdatenkartenDesBoards(…, long boardId)`: ohne Archivfilter, mit Archivmarke, Spaltenbezeichnung und Kartenklasse; `AlsKarte` unverändert.
- `Etikettenleser` — `LiesEtikettenDesBoards`; `Teilaufgabenleser` — `LiesTeilaufgabenDesBoards`; `Kommentarleser` — `LiesKommentareDesBoards`; `Anhangleser` — `LiesAnhaengeDesBoards`; `Dateiverweisleser` — `LiesDateiverweiseDesBoards`. Je Rückgabe eine Zuordnung `KarteId → Liste`.

**In `KanbanC.BL/Persistenz/Zeiten`:**
- `Zeitenleser` — `LiesZeiteintraegeDesBoards(…, long boardId)`: `LiesLaufendeZeiteintraegeDesBoards` ohne `AND z.Ende IS NULL`.

**In `KanbanC.BL/Integrations/Rohdaten`:**
- `RohdatenService` (Integration, fängt/loggt) — zwei Auskünfte mit derselben Vorprüfung; setzt die fünf Listen an ihre Karten.
  - `Ergebnis<IReadOnlyList<Rohdatenkarte>> Karten(long boardId)`
  - `Ergebnis<IReadOnlyList<Zeiteintrag>> Zeiten(long boardId)`

**In `KanbanC.WebApi/Endpunkte`:**
- `RohdatenEndpunkte` (Integration) — `GET /api/boards/{boardId}/karten` und `GET /api/boards/{boardId}/zeiten`; 200 bzw. 404 über `Zurueckweisungen.AlsNichtgefunden(Nichtgefunden.Board(boardId))`.

**In `KanbanC.Blazor`:**
- `Rohdatenflaeche.razor` (+ `.razor.css`) — nennt die zwei Pfade des gewählten Boards. **Kein Klient, kein Abruf, kein Zustand.**

**Kein Interface** für die Leser: je Aufgabe genau eine Implementation (C25). **Keine tote Flexibilität** (C24): kein `RohdatenApiKlient` ohne Aufrufer.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Kartenleser` | boardweite Rohdaten-Leseform **neben** den drei vorhandenen; ohne Archivfilter, mit Marke |
| `Etikettenleser`, `Teilaufgabenleser`, `Kommentarleser`, `Anhangleser`, `Dateiverweisleser` | je eine boardweite Leseform neben `LiesXDerKarte` |
| `Zeitenleser` | `LiesZeiteintraegeDesBoards` neben `LiesLaufendeZeiteintraegeDesBoards` |
| `Auswertungen.razor` | `rohdaten` wandert von `NochNichtGebaut` nach `Gebaut`; für diese Wahl tritt die Kartenklassenwahl zurück; der Fuß führt **zwei** Pfade |
| `Program.cs` (WebApi) | `RohdatenEndpunkte` registrieren |
| `FehlervertragTests` | nimmt die zwei neuen Routen auf |
| `AuswertungenSeite` (E2E-Seitenobjekt) | wächst um den Aufrufblock |

**Nicht geändert:** `BoardService`, `Abschlussbahn`, `StandardspaltenVorlage`, `KartenService`, `Kartendetail`, `KartenEndpunkte`, `KartenklassenEndpunkte`, `AuswertungsService`, `AuswertungsEndpunkte`, `EreignisEndpunkte`, alles unter `Persistenz/Migrationen/`.

### Wireframe

`Dokumentation/Wireframes/D0009.dc.html`, **Zustand 6** („Rohdaten über die API · I0037 · der Aufruf als Bedienelement") ist der **Verweis für die Gestaltung** von `F0069`: die zwei Aufrufzeilen, der Hinweis „was ein Mensch davon sieht — einen Fuß, und sonst nichts", und der gestrichelte Block zu `…/verlaeufe`, der das fehlende Wort benennt. Das Bild ist Entwurfsquelle, **nie Kriterienquelle**; kein Akzeptanzkriterium dieser Anforderung ist aus ihm abgeleitet — die fünf statt drei n-Listen sind der Beleg dafür.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md`; jeder Test verifiziert eine echte Zustandsänderung (Skill `test-ehrlichkeit`).

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):**
- `RohdatenService.Karten` / `.Zeiten` gegen ein **Test-Repository**: die Vorprüfung (unbekanntes Board → `Nichtgefunden.Board`), das Zusammensetzen der fünf Listen an die richtige Karte, die leere Liste bei einem Board ohne Karten. **Pure Zuordnungslogik ohne Datenbank.**
- Mehr gibt es hier nicht zu unit-testen: dieser Slice rechnet nichts. Das ist kein Mangel, sondern die Regel des Slice.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `RohdatenRepository.LiesKartenDesBoards` — archivierte Karten kommen **mit Marke** mit, klassenlose Karten kommen mit, die Ordnung ist `s.Position, k.Position`, fremde Boards bleiben draußen, die fünf Listen hängen an der richtigen `KarteId`.
- `RohdatenRepository.LiesZeiteintraegeDesBoards` — laufende kommen mit (`Ende` null), Einträge auf archivierten Karten und von stillgelegten Kontributoren bleiben drin, Ordnung `Beginn, ZeiteintragId`, fremder Bestand draußen.
- Beide Routen: 200 mit vollständiger Liste, 200 mit leerer Liste, 404 mit Code, Werten und Kompensationsaktion; `FehlervertragTests` erweitert.
- **Der Beweis, dass nichts abgeschnitten wird** (`B0525`, nur Test, kein Produktionscode — wie `B0017`): 21 erledigte Karten bei Anzeigegrenze 20, dazu eine archivierte und eine klassenlose Karte. Die Rohdatenroute führt alle **24**; die **Gegenprobe** zeigt, dass `GET /api/boards/{boardId}` weiter 20 liefert und die archivierte weglässt.

**`KanbanC.Blazor.Tests`:** die Gestaltungsprüfung der neuen Fläche (kein Farb-, Abstands- oder Radiusliteral). **Kein Klienttest** — es gibt keinen Klienten; die Fehlerpfade, für die dieses Projekt existiert, entstehen hier nicht.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** ein Lauf — Board mit Karten und einem Zeiteintrag aufbauen, eine Karte archivieren, `/auswertungen` öffnen, `Rohdaten über die API` wählen, die **zwei Pfade lesen** und **beide Adressen neben dem Browser wirklich abrufen**; die Kartenzahl der Rohdatenantwort gegen die des Boards halten. Das ist der Beweis, dass der gezeigte Pfad der ist, der antwortet — ein Fuß mit einem falschen Pfad wäre schlimmer als keiner.

## Abhängigkeiten

- Abhängig von: **`R00006`** (`I0011` — Karte anlegen, **grün**), das `Braucht` von `I0037` und `F0067`: erst dort gibt es Karten mit Ort.
- **`R00027`** (`I0024` — Timer stoppen, **grün**), das `Braucht` von `F0068`: erst dort entsteht ein Eintrag **mit Ende**.
- Setzt außerdem auf (alle grün): **`R00016`** (Archivstand und `Kartenarchivierung`), **`R00017`**–**`R00021`** (Etiketten, Teilaufgaben, Kommentare, Anhänge, Dateiverweise samt ihren Lesern), **`R00022`**/**`R00023`** (Kartenklassen und Kartennummern), **`R00025`** (`I0022` — die Lage „Slice nur API, Bedienelement ist der Aufruf"), **`R00036`**/**`R00037`**/**`R00038`** (`/auswertungen`, Umschalter, Aufruffuß, `AuswertungenSeite`), **`R00005`** (Gestaltungstokens).
- `F0069` **braucht** `F0067` und `F0068` — sonst nännte der Fuß Pfade, die nicht antworten.
- Blockiert: **nichts.** Kein Knoten der WBS führt `I0037` in seiner `Braucht`-Spalte. `I0038` (Board exportieren) ist der nächste natürliche Verbraucher der neuen Leseformen, hängt aber formal an `I0011`, `I0021` und `I0024`.
- **`D0009` wird mit diesem Slice nicht grün** — `I0035` (Puffer-Verbrauch) bleibt rot.

## Umfang

```
Rohdaten über die API abrufen (I0037) = 12 Bubbles: 11 Standard (15,6h), 1 unklar (2,0-4,0h).
Rest: 15,6h klar + 2,0-4,0h unklar · 0 von 12 Werten belegt, alles Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0037` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **drei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0067` Die Karten des Boards als Rohdaten über die API | `B0519`–`B0525` (7) | 7 (10,8h) | 0 | `I0011` |
| `F0068` Die Zeiten des Boards als Rohdaten über die API | `B0526`–`B0528` (3) | 3 (2,8h) | 0 | `I0024` |
| `F0069` Der Schirm nennt die zwei Aufrufe | `B0529`–`B0530` (2) | 1 (2,0h) | 1 (2,0–4,0h) | `F0067`, `F0068` |

**Warum drei Features:** weil drei Aspekte **getrennt fertig** werden. `F0067` und `F0068` sind je allein an der Antwort prüfbar und könnten vollständig sein, während der Schirm noch nichts zeigt; `F0069` nennt, was die beiden liefern.

**Warum `F0068` so klein ist:** `Zeiteintrag` **ist** bereits die gespeicherte Zeile, und die Abfrage ist `LiesLaufendeZeiteintraegeDesBoards` **minus einer Bedingung**. Kein DTO, kein Zusammensetzen, keine n-Listen.

**Wo die Hälfte des Aufwands liegt:** in den fünf n-Listen, die boardweit statt je Karte gelesen werden müssen (`B0521`, `B0522` und ihr Zusammensetzen in `B0523`). Eine schlankere Antwort mit den drei Listen des Artboards wäre rund zwei Bubbles billiger — und unvollständig.

Die eine unklare Bubble ist `B0530` (E2E über beide Prozesse, mit Aufbau aus Karten, Archivierung und Zeiteintrag).

**Nach gemessenem Durchsatz ist mit etwa 0,5–1,0 h zu rechnen.** Die Richtwert-Konvention seit `I0004` überschätzt messbar; die Zählung wird trotzdem nicht still gekippt — eine Konvention, die mitten in einem Baum wechselt, erzeugt zwei Bäume. Sie wird genannt, damit die Zahl nicht als Zusage gelesen wird. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0037` und an allen drei Features** — dieselbe Form wie bei `R00031`/`I0028` bis `R00038`/`I0036`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **„Verläufe" im Fertig-Kriterium hat im Bestand keinen dritten Gegenstand — und das ist die dritte Fundstelle desselben fehlenden Knotens.** Nachgesehen, nicht angenommen: es gibt kein Ereignisjournal, keine Bewegungstabelle und keine Spur, wer wann welche Karte über welche Grenze bewegt hat. `I0028` hat die Ereignisspur ausdrücklich **verworfen** — „eine Liste vergangener Ereignisse ist etwas anderes als eine Einflugmarke" (`B0384`) —, und der Live-Kanal **sendet, ohne zu speichern** (`EreignisEndpunkte`, `GET /api/ereignisse`). **Entschieden**: „Verläufe" heißt hier die zwei zeitlichen Spuren, die der Bestand wirklich führt — `Karteerledigung.ErledigtAm` (die Rohform des Burndowns, reist schon an `Karte`) und die Zeiteinträge mit Beginn und Ende (die Rohform von Soll-Ist und Zeitexport). Beide kommen mit den zwei neuen Routen; damit trägt der Abruf **jeden Verlauf, den der Bestand kennt**. **Der Bewegungsverlauf wird hier nicht gebaut**, weil er kein Abruf wäre, sondern eine **Schreibpflicht an jeder Bewegung** samt Tabelle, Migration, Aufbewahrungsfrage und Nachtragslage für alles, was vor ihm geschah. **Die Adresse ist ein eigener Slice unter `D0007` (Ereignisjournal am Live-Kanal).** Vorher genannt haben das Wireframe-Frage 13 und `I0028`; das Artboard sagt es selbst (`D0009.dc.html:566-571`). **Befund für `/planung` und `/vision` — hier nicht geändert**, diese Familie legt keine Knoten an. **Nicht am Menschen geprüft.**
- **Fünf n-Listen statt der drei im Artboard.** Das Bild nennt Etiketten, Teilaufgaben und Dateiverweise (`D0009.dc.html:558`); **angenommen sind fünf** — Kommentare und Anhänge (als Metadaten) kommen dazu, weil „vollständig" keine Auswahl unter gespeicherten Zeilen kennt und beide boardweit sonst unerreichbar sind. **Der Preis ist benannt**: rund zwei Bubbles. Wer die schlanke Antwort will, streicht `B0521`/`B0522` um zwei Abfragen. **Abweichung vom Bild — das Bild ist eine Absicht, kein Vertrag. Nicht am Menschen geprüft.**
- **Die Antwortgröße ist die einzige verbleibende Grenze und wird genannt, nicht behoben.** Einige Megabyte in einem Stück bei einem Board der Größenordnung aus `I0030`. **Angenommen: das ist eine Größe, keine Kürzung** — ein Standardmaximum wäre die fremde Grenze, die das Motiv ausschließt, und ein Strom eine andere Antwortform mit eigenen Fragen (Abbruch mitten im Strom, Fehler nach dem ersten Byte). **Ein eigener Slice, falls jemand ihn bestellt. Nicht am Menschen geprüft.**
- **Der Umschalterpunkt wird wählbar, obwohl der Slice nichts bedient.** Ohne diesen Schritt führte der Schirm einen **gebauten** Slice als „noch nicht gebaut". Die Alternative — den Punkt gesperrt lassen und nur den Fuß ändern — wurde verworfen, weil der Fuß der gewählten Auswertung folgt und ohne wählbaren Punkt nie zu sehen wäre. **Nicht am Menschen geprüft.**
- **Der Archivstand reist als `Archivierung`-DTO an der Rohdatenkarte, nicht als `bool`.** Der Typ existiert (`KanbanC.Contracts/Boards/Archivierung.cs`) und wird an den Abrufrouten schon als Filter geführt; ein nackter `bool` daneben wäre derselbe Begriff in zwei Schreibweisen (C06). **Angenommen. Nicht am Menschen geprüft.**
- **`I0035` (Puffer-Verbrauch) bleibt der einzige gesperrte Eintrag und hat weiterhin keinen definierten Gegenstand.** Das Artboard zeichnet ihn als **Frage** mit zwei fehlenden Voraussetzungen. **Befund für `/planung`, hier nicht geändert.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Dieser Slice bringt keine Migration mit und liest nur, was ohnehin geschrieben wird.

## Manuelle Nachbereitungstätigkeiten

- Keine. Es ändert sich keine Konfiguration und kein Betriebsweg.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt steht in der Vision und ist gemessen, nicht vermutet: „Für eine eigene Implementation von Burndown-Chart und Critical Chain werden sehr spezielle Daten gebraucht, und zwar schnell; eine fremde Cloud-API mit Limits gibt sie nicht her." Genau diese Limits hat KanbanC heute selbst gebaut — nicht als Rate Limit, sondern als drei stille Kürzungen: die Boardantwort schneidet die Abschlussspalte auf 20, lässt die archivierten Karten ganz weg und trägt die fünf n-Listen einer Karte nicht, sodass ein Agent für 431 Karten 431 Folgeaufrufe macht; boardweite Zeiten als JSON gibt es überhaupt nicht. Wenn zwei Routen den Bestand des Boards **ungekürzt, ungefiltert und in einem Stück** herausgeben und dafür je Sorte **einen** Lesevorgang statt eines je Karte machen (X), dann bekommt ein Agent Karten, Erledigungsdaten und Zeiteinträge mit zwei Aufrufen statt mit vierhundert (Y), sodass eigene Auswertungen außerhalb der Anwendung entstehen können, ohne dass die Anwendung vorher entscheidet, was sie für sehenswert hält (Z). Der Hebel sitzt genau hier und nicht vorgelagert: an der Datenhaltung fehlt nichts — jede Zeile steht seit `I0011` bis `I0026` in der Datenbank —, das Fehlende ist allein der Weg heraus. Und er sitzt nicht nachgelagert bei einer weiteren gerechneten Auswertung: `soll-ist` und `burndown` sind **Auslegungen**, und wer eine eigene Auslegung rechnen will, braucht die Einträge, nicht eine dritte fremde.

## Missing-Docs

- **Antwortgrößen und JSON-Serialisierung in ASP.NET Minimal APIs.** Ab welcher Größe eine in einem Stück serialisierte Antwort praktisch wehtut (Speicher im Server, Verhalten des `HttpClient` auf der Gegenseite, Standardgrenzen von Kestrel), ist im Repository nirgends notiert; der Bestand hat bisher keine Antwort dieser Größenordnung erzeugt. Für die Entscheidung „kein Strom" ist das die einzige Größe, die von außen kommt.
- **Gruppiertes Lesen mit Dapper über mehrere n-Beziehungen.** Der Bestand liest n-Listen bisher immer je Karte; ob eine Mehrfachabfrage (`QueryMultiple`) hier der bessere Weg als fünf einzelne Abfragen ist, ist nicht belegt. Für `B0521`/`B0522` wäre eine Notiz hilfreich.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **`GET /api/boards/{id}` entkürzen** statt einer neuen Route | Die Boardantwort ist die **Anzeige** und wird von der Bahn gezeichnet; sie zu entkürzen änderte den Schirm still mit. Zwei Zusagen brauchen zwei Ressourcen. |
| **Ein `?rohdaten=true`-Schalter an der Boardroute** | Eine Route mit zwei Bedeutungen — und ein Schalter, den zu vergessen die stille Kürzung zurückbrächte. |
| **`?limit`/`?offset` „zur Sicherheit"** | Genau die fremde Grenze, die das Motiv ausschließt. Ein Standardmaximum wäre die schlimmere Form: sie sähe für einen Agenten wie ein Erfolg aus. |
| **Ein Strom (NDJSON, `IAsyncEnumerable`) als Antwortform** | Eine andere Antwortform mit eigenen Fragen: Abbruch mitten im Strom, Fehler nach dem ersten Byte, Aufrufer, die kein Streaming lesen. Eigener Slice, nicht Nebenwirkung dieses Abrufs. |
| **Eine dritte Route `…/verlaeufe`** | Sie hätte keinen Gegenstand. Der Bewegungsverlauf existiert im Bestand nicht. |
| **Das Ereignisjournal in diesem Slice mitbauen** | Kein Abruf, sondern eine **Schreibpflicht** an jeder Bewegung samt Tabelle, Migration, Aufbewahrungsfrage und Nachtragslage. `I0028` hat die Ereignisspur verworfen; der Live-Kanal sendet ohne zu speichern. Adresse: eigener Slice unter `D0007`. |
| **Die Rohdaten je Kartenklasse ausschneiden** | Eine Kartenklasse ist ein **Ausschnitt**; klassenlose Karten stünden dann in keinem Abruf. „Vollständig" verträgt keinen Ausschnitt. |
| **Ein Zeitraumparameter an `…/zeiten`** | Zweiter Ort für die Schnittregel aus `B0504`. Wer schneiden will, schneidet in seiner eigenen Auswertung. |
| **Ein Archivfilter als Parameter an `…/karten`** | Der Aufrufer bekommt die Marke und filtert selbst; ein Parameter machte aus dem Vollständigkeitsversprechen eine Option. |
| **Die Zeiteinträge auch an der Rohdatenkarte** | Dieselbe Zeile an zwei Adressen wäre genau die zweite Wahrheit, die dieser Slice vermeidet. Die `KarteId` verbindet beide Antworten. |
| **Der Kartentitel auch an der Zeitzeile** | Derselbe Titel an zwei Adressen; `F0067` trägt ihn. |
| **Eine Hülle mit Zählangabe um die Kartenliste** | Es wird nichts gekürzt — also **ist** die Länge der Liste die Zahl. Ein Zählfeld wäre eine zweite Wahrheit (anders als bei `Spalte.Kartenzahl`, wo gekürzt wird). |
| **Nur die drei Listen des Artboards** (Etiketten, Teilaufgaben, Dateiverweise) | „Vollständig" kennt keine Auswahl unter gespeicherten Zeilen; Kommentare und Anhänge wären boardweit unerreichbar. |
| **Die Anhangbytes mitschicken** | Die Antwort würde beliebig groß, und der Weg für Bytes steht seit `R00020`. Metadaten hier, Inhalt über die Anhangroute. |
| **Ein neues Zeit-DTO für die Rohdaten** | `Zeiteintrag` **ist** bereits die gespeicherte Zeile mit Schlüssel, Karte, Kontributor, Beginn und `Ende?`. Ein zweites wäre Verdopplung (C22). |
| **Die vorhandenen Leser umbauen statt zu ergänzen** | `LiesKartenNachPosition` trägt die Anzeigeregeln des Boards; ein Umbau brächte sie in die Rohdaten oder nähme sie der Anzeige. Die neuen Formen stehen **daneben**. |
| **Ein Folgeaufruf je Karte in der Integration** (die vorhandenen `LiesXDerKarte` in einer Schleife) | Das N+1 würde nur vom Aufrufer in den Server verschoben: 431 Karten wären 2.155 Abfragen in **einem** Aufruf. |
| **Ein Rohdaten-Knopf, der JSON in den Browser lädt** | Zweiter Export neben `I0036` und eine Fähigkeit, die bei der API anfängt — dieselbe Lage wie der Klassenfilter bei `I0022`, dort mit denselben Worten weggelassen. |
| **Den Umschalterpunkt gesperrt lassen** | Der Schirm führte einen gebauten Slice als „noch nicht gebaut", und der Fuß mit den zwei Pfaden wäre nie zu sehen. |
| **Ein `RohdatenApiKlient` in der Oberfläche** | Ein Glied ohne Aufrufer — tote Flexibilität (C24). Der Schirm ruft nichts ab. |
| **Zeilen in `GET …/zeitexport` nachrüsten** | `I0036` hat ihn bewusst ohne Zeilen gebaut, damit er kein zweiter Weg neben diesem Slice wird. |

### Bewusst out of scope

- Ereignisjournal, Bewegungsverlauf, Ereignisspur am Live-Kanal (`D0007`) — **die Adresse für „Verläufe" im engeren Sinn**.
- Puffer-Verbrauch (`I0035`), Board-Export als Datei (`I0038`).
- Strom, Chunking, Kompressionsverhandlung, ETag/Bedingte Abrufe, Zwischenspeicher.
- Schreibwege, Schema, Migrationen; jede Rechnung, jede Sortierregel der Anzeige.
- Jeder Ausschnitt: Kartenklasse, Zeitraum, Spalte, Archivstand als Parameter.

### Angenommen im stillen Lauf

Dieser Slice ist ohne Rückfrage entstanden; die folgenden Punkte sind **entschieden, nicht abgestimmt**:

1. **Zwei neue Routen, keine dritte** — geprüft am Bestand (Commit `0938283`): eine Zusammenstellung aus vorhandenen Wegen trägt die Zusage nicht.
2. **„Verläufe" = Erledigungsdatum + Zeiteinträge**; der Bewegungsverlauf wird nicht gebaut und hat die Adresse „eigener Slice unter `D0007`". **Dritte Fundstelle desselben fehlenden Knotens.**
3. **„Ohne Limit" = keine Seitengröße · keine Anzeigekürzung · kein stiller Archivfilter · kein N+1 beim Aufrufer.** Die eine bleibende Grenze — die Antwortgröße — wird **genannt**, nicht wegdefiniert.
4. **Boardweiter Skopus**, nicht je Kartenbestand; sonst stünden klassenlose Karten in keinem Abruf.
5. **Fünf n-Listen statt der drei im Bild**, mit benanntem Preis von rund zwei Bubbles.
6. **Archivierte Karten mit Marke statt ohne Filter**; die Marke reist als `Archivierung`-DTO.
7. **Kein Zeiteintragsfeld an der Rohdatenkarte, kein Kartentitel an der Zeitzeile** — die `KarteId` verbindet.
8. **Keine Hülle mit Zählangabe** — die Länge der Liste ist die Zahl.
9. **Der Slice ist API-first wie `I0022`**: der Mensch sieht einen Fuß, der Umschalterpunkt wird trotzdem wählbar, und **kein Rohdaten-Knopf** entsteht.
10. **Kein Schema, keine Migration, kein Schreibweg, keine Rechnung** — nur Leseformen neben vorhandenen.
11. **Das Artboard war Entwurfsquelle, nie Kriterienquelle.** `D0009.dc.html`, Zustand 6 ist der Verweis für die Gestaltung von `F0069`; **kein Akzeptanzkriterium dieser Anforderung ist aus dem Bild abgeleitet** — die fünf statt drei Listen und die entschiedene Verlaufsfrage sind zwei Belege dafür.
