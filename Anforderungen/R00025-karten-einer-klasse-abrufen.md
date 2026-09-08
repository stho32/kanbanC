---
id: R00025
status: Erledigt
datum: 2026-09-06
---

# R00025: Karten einer Klasse abrufen

## Beschreibung

Ein Aufruf liefert **genau die Karten einer Kartenklasse** — über alle Spalten des Boards hinweg, ohne die Karten anderer Klassen, ohne die klassenlosen und ohne die eines anderen Boards: `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten`. Jede Karte kommt mit dem Ort, an dem sie liegt; die Reihenfolge ist die des Nummernkreises. Das ist die Einlösung der Vision-Zusage, „gezielt das richtige Set zu greifen statt des ganzen Boards".

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine Klasse fasst zusammengehörige Karten (etwa alle aus der WBS) und vergibt eine eigene, klassenspezifische Nummerierung, so dass ein Agent über die API gezielt das richtige Set greift statt des ganzen Boards."

**Was `R00023` gebaut hat und dieser Slice benutzt:** die Zuordnung samt vergebenem Zählerstand. `R00023` hängt die Karten an den Nummernkreis — **hier werden sie darüber wiedergefunden.**

**Dieser Slice hat bewusst keine Oberfläche.** Das ist die auffälligste Entscheidung an ihm und steht unter „Technische Überlegungen → Warum kein Schirm" begründet; sie ist **hier zu bestätigen oder zu verwerfen**, nicht in der Umsetzung.

**Dies ist der dritte Slice von `D0005` „Karten-Klassen".**

## Geschäftlicher Nutzen

Die Vision nennt das gezielte Greifen eines Sets als den Zweck der ganzen Kartenklasse — nicht die Klasse selbst ist der Wert, sondern der Abruf über sie. `R00022` hat den Nummernkreis gebaut, `R00023` die Karten daran gehängt; heute kann ein Agent eine Nummer **sehen**, aber sein Set nicht **holen**. Er müßte `GET /api/boards/{boardId}` abrufen, alle Spalten durchgehen, jede Karte auf ihr Präfix prüfen und dabei genau das tun, was die Vision ihm ersparen will — und er bekäme das falsche Ergebnis, weil die Boardantwort eine Abschlussspalte an ihrer Anzeigegrenze kürzt.

Nach diesem Slice ist ein Set **eine** Adresse. Davon leben die Verbraucher, die es genau brauchen: `I0030`/`I0031` (WBS-Import und Wiederholung — der Soll-Ist-Vergleich braucht den vollständigen Ist-Bestand einer Klasse), `I0033` (Soll-Ist) und `I0038` (Board exportieren).

## Funktionale Anforderungen

- `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten` liefert die Karten **dieser** Kartenklasse aus **allen** Spalten des Boards, mit HTTP 200.
- Karten anderer Kartenklassen, Karten **ohne** Kartenklasse und Karten eines **anderen** Boards sind nicht darunter — auch dann nicht, wenn dieses andere Board eine Kartenklasse mit **demselben Präfix** führt.
- Jede gelieferte Karte reist als das gebaute `Karte`-DTO und trägt den **Ort** dazu: `Spalte` und `Spaltenbezeichnung`.
- Die Reihenfolge ist der **Zählerstand aufsteigend** — die Ordnung des Nummernkreises, nicht die des Boards.
- Die Antwort ist **ungekürzt**: die Anzeigegrenze einer Abschlussspalte wird hier nicht angewandt.
- Ohne Parameter kommt der **aktive** Bestand; `?archiviert=true` liefert die archivierten Karten dieser Kartenklasse.
- Eine Kartenklasse **ohne** Karten ist kein Fehler: HTTP 200 mit leerer Liste.
- Ein **unbekanntes Board**, eine **unbekannte Kartenklasse** und eine Kartenklasse **eines fremden Boards** werden mit Befund zurückgewiesen; ein unlesbarer Archivfilter ebenso.
- Die Kartenklasse wird über ihre `KartenklasseId` adressiert, nicht über ihr Präfix.

## Nicht-funktionale Anforderungen

- **„Genau ihre Karten" ist eine Zusage ohne stilles Kleingedrucktes.** Ein Abruf, der Karten wegließe, wäre für einen Agenten schlimmer als ein Fehler — er sähe wie ein Erfolg aus. Deshalb keine Anzeigegrenze, kein Limit, keine Seitung.
- **Fehlerantworten für Agenten:** jeder Befund nennt Grund **mit Werten** und die **Kompensationsaktion**, auch der 404 (Projektregel, `Nichtgefunden`).
- **`Nichtgefunden` wächst nicht.** `Kartenklasse(boardId, kartenklasseId)` und `FremdeKartenklasse(boardId, kartenklasseId, boardIdDerKartenklasse)` stehen seit `R00023`; dieser Slice benutzt sie.
- **Begriff (C06):** `Kartenklasse` im ganzen Stack — Tabelle, Contracts, Route, Leser, Dienst. Die Beschriftung „Klassen" gilt nur in der Oberfläche und kommt hier nicht vor, weil es hier keine Oberfläche gibt. C07: Bezeichner ohne echte Umlaute, UI-Texte und Kommentare mit.
- **C08:** `Klassenkarte` ist ein immutable Record in `KanbanC.Contracts`.
- **C24 (keine tote Flexibilität):** kein Glied am `KartenklassenApiKlient`, keine Komponente, kein Seitenweg — solange niemand ruft, entsteht nichts.
- **Keine Migration.** Der Slice liest; `016` und `017` tragen bereits alles, was er braucht.
- **SQL nach Skill `sql-stil`:** Fluss-Ausrichtung, explizite Spalten, Schlüsselwörter GROSS. `JOIN Kartenklassenzuordnung` ist der Filter, `JOIN Spalte` liefert die Bezeichnung.
- **Die Kernregel bleibt:** `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL`.

## Akzeptanzkriterien

### Genau ihre Karten, ohne die übrigen

- [x] `GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten` antwortet mit HTTP 200 und den Karten dieser Kartenklasse.
- [x] **Rechenbeispiel:** Board 1 führt die Kartenklassen `WBS-` (Stand 3) und `BUG-` (Stand 2). In drei Spalten liegen: `WBS-01`, `WBS-02`, `WBS-03`, dazu `BUG-01`, `BUG-02` und zwei Karten ohne Kartenklasse. Board 2 führt ebenfalls eine Kartenklasse mit Präfix `WBS-` und hat zwei Karten darin. Der Abruf auf die `WBS-`-Kartenklasse des Boards 1 liefert **genau 3** Karten: `WBS-01`, `WBS-02`, `WBS-03` — **nicht 5, nicht 7, nicht 9**.
- [x] Die Karten kommen **über Spaltengrenzen hinweg**: liegen die drei in drei verschiedenen Spalten, sind alle drei in **einer** Antwort.
- [x] Ein zweites Board mit **demselben Präfix** verändert das Ergebnis nicht — adressiert wird die `KartenklasseId`, nicht das Präfix.

### Ungekürzt — auch aus einer Abschlussspalte

- [x] Liegen zwei Karten der Kartenklasse in einer **Abschlussspalte mit Anzeigegrenze 1**, liefert der Abruf **beide**. `Abschlussbahn.Gekuerzt` wird auf diese Antwort **nicht** angewandt.
- [x] Dieselbe Boardantwort (`GET /api/boards/{boardId}`) zeigt diese Abschlussspalte weiterhin gekürzt — die Anzeigeregel des Boards bleibt unangetastet.

### Die Ordnung ist die des Nummernkreises

- [x] Sortiert wird nach `Zaehlerstand` **aufsteigend**: `WBS-01`, `WBS-02`, `WBS-03` — unabhängig von Spalte und Position.
- [x] **Rechenbeispiel gegen die Textsortierung:** eine Kartenklasse mit den vergebenen Ständen 99 und 100 liefert `WBS-99` **vor** `WBS-100`. Nach der Kartennummer als Text stünde `WBS-100` vorn, weil `Kartennummer.Aus` nur zweistellig auffüllt.

### Der Archivfilter

- [x] **Ohne Parameter** liefert der Abruf den **aktiven** Bestand; archivierte Karten der Kartenklasse fehlen.
- [x] `?archiviert=true` liefert **genau die archivierten** Karten dieser Kartenklasse und keine aktive.
- [x] Ein unlesbarer Wert (`?archiviert=vielleicht`) ergibt HTTP 400 mit `archiv-filter-unlesbar`; die Kompensation nennt **die aufgerufene Adresse**, nicht eine fremde.

### Die Antwortgestalt

- [x] Jeder Eintrag ist eine `Klassenkarte` aus dem gebauten `Karte`-DTO plus `Spalte` und `Spaltenbezeichnung` — **keine zweite Kartengestalt**.
- [x] Die Karte trägt darin ihre `Kartennummer` (`WBS-02`), wie überall sonst auch.
- [x] `Board` und `Boardname` reisen **nicht** mit — sie stehen in der Adresse.

### Die leere Klasse

- [x] Eine Kartenklasse **ohne** zugeordnete Karte antwortet mit HTTP 200 und `[]` — **kein** 404. Dieselbe Entscheidung wie beim Board ohne Kartenklasse in `R00022`.
- [x] Ein Board, dessen sämtliche Karten der Kartenklasse archiviert sind, liefert ohne Parameter ebenfalls `[]` und mit `?archiviert=true` die archivierten.

### Fehlerantworten für Agenten

- [x] **Unbekanntes Board** → HTTP 404 mit `board-unbekannt`, Grund mit der Nummer und Kompensationsaktion.
- [x] **Unbekannte Kartenklasse** → HTTP 404 mit `kartenklasse-unbekannt`; die Kompensation nennt `GET /api/boards/{boardId}/kartenklassen`.
- [x] **Kartenklasse eines fremden Boards** → HTTP 404 mit `kartenklasse-fremd` und **nicht** mit `kartenklasse-unbekannt`: es gibt sie, nur nicht hier. Der Befund nennt beide Boardnummern.
- [x] Die Prüfreihenfolge ist **Board, dann Kartenklasse, dann Karten**: ein Lesezugriff auf die Karten einer fremden Kartenklasse findet **nicht** statt.
- [x] `FehlervertragTests` deckt die neue Route mit allen drei Fällen ab; sie steht **nicht** auf `RoutenOhneFehlerantwort`.

### Der grüne Bestand bleibt grün

- [x] `GET /api/boards/{boardId}` und `GET /api/boards/{boardId}/spalten/{spalteId}/karten` antworten unverändert; insbesondere kürzt die Boardantwort ihre Abschlussspalte weiter.
- [x] Kein Glied wächst in `KanbanC.Blazor` — die Oberfläche ist von diesem Slice **nicht betroffen**, und keine E2E-Suite ändert sich.
- [x] Die Testsuiten aus `R00001`–`R00024` laufen unverändert weiter.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Klassen/Klassenkarte.cs` (neu, immutable Record).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` — eine Leseabfrage mehr (die Karten einer Kartenklasse samt Spalte und Bezeichnung, mit Archivfilter); `Source/KanbanC.BL/Persistenz/Klassen/KartenklassenRepository.cs` und `Source/KanbanC.BL/Interfaces/Klassen/IKartenklassenRepository.cs` — je eine Methode mehr.
- **Dienste:** `Source/KanbanC.BL/Integrations/Klassen/KartenklassenService.cs` — `LadeKartenDerKartenklasse`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenklassenEndpunkte.cs` — eine Route mehr.
- **Unberührt:** `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` (die drei Befunde stehen seit `R00023`), `Source/KanbanC.BL/Operations/Boards/Archivfilter.cs`, `Source/KanbanC.BL/Operations/Karten/Abschlussbahn.cs`, `Source/KanbanC.BL/Persistenz/Migrationen/` (**keine Migration**), **das gesamte `Source/KanbanC.Blazor/`** und `Source/KanbanC.PlaywrightTests/`.
- **Tests:** `Source/KanbanC.BL.Tests/Integrations/Klassen/KartenklassenServiceTests.cs`, `Source/KanbanC.BL.Tests/TestHelpers/TestKartenklassenRepository.cs`, `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Klassen/KartenklassenRepositoryTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/Api/KartenklassenEndpunkteTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs` und eine neue Testklasse für den Bestandsfall (`B0318`).

## Technische Überlegungen

### Warum kein Schirm — die Entscheidung dieses Slice

Die Projektregel an `A0001` lautet „was die Oberfläche kann, kann die API" und steht in der WBS als „Dass die API alles kann, ist Fertig-Kriterium an jedem Slice". Das ist eine Zusage in **eine** Richtung: sie verlangt für jede Oberflächenfähigkeit einen Endpunkt, aber **keinen Schirm für eine Fähigkeit, die bei der API anfängt.** Das Fertig-Kriterium dieses Slice nennt ausdrücklich nur die API: „**Über die API** liefert eine Klasse genau ihre Karten, ohne die übrigen."

Das Artboard entscheidet dieselbe Frage selbst und ausdrücklich (`Dokumentation/Wireframes/D0005.dc.html:354-379`): es zeichnet den Aufruf als „das Bedienelement des Agenten" und lässt die Oberflächenentsprechung — einen Klassenfilter in Zone 3 der Navigationszeile — als markierte Lücke daneben stehen, weil sie eigene Fragen aufwirft: *was zeigt eine gefilterte Bahn in ihrer Kartenzahl, und was heißt Ziehen in einer gefilterten Ansicht.* Wörtlich: „Das ist eine Entscheidung ihres Slice, keine Beigabe."

Ein `KartenklassenApiKlient`-Glied ohne Aufrufer wäre tote Flexibilität (C24). **Wo die Fähigkeit sichtbar wird:** beim Agenten im Aufruf selbst, und im Bestand bei `I0030`/`I0031`, `I0033` und `I0038`. `I0037` „Rohdaten über die API abrufen" ist dieselbe Lage und wird in der WBS ebenfalls ohne Oberflächenknoten geführt.

**Diese Anforderung ist der Ort, an dem die Entscheidung bestätigt oder verworfen wird.** Wird sie verworfen, ist die Folge nicht ein Zusatz zu diesem Dokument, sondern ein eigener Slice „Klassenfilter in Zone 3" mit den beiden offenen Fragen des Artboards — die WBS bekäme einen Knoten, nicht dieser Slice eine Bubble.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0005.dc.html`](../Dokumentation/Wireframes/D0005.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gilt daraus **Zustand 5** (`:346-379`) — der gezeichnete Aufruf und die begründete Lücke daneben. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Route heißt `…/kartenklassen/{kartenklasseId}/karten`, nicht `…/klassen/1/karten`.** Das Artboard schreibt `GET /api/boards/2/klassen/1/karten` (`:360`) und nennt den Aufruf selbst „Entwurf, keine Zusage". Gebaut wird `kartenklassen` — **dieselbe Entscheidung wie in `R00022` und `R00023`**: C06 verlangt einen Begriff in einer Schreibweise, und die Route ist eine Stelle des Bezeichners.
2. **Die Sortierung ist aufsteigend.** Das Artboard zeigt sein Beispiel **absteigend** (`WBS-31`, `WBS-28`, `WBS-09`, `:358-360`), sagt aber nichts über die Ordnung — die drei Zeilen stammen als Zitat aus `D0001` und `D0003`. Gewählt ist **aufsteigend**, weil das die Leserichtung eines Nummernkreises ist und `R00022` die Anlagereihenfolge schon für die Klassenliste gewählt hat.
3. **Die Antwortgestalt trägt die ganze Karte.** Das Artboard skizziert je Zeile vier Felder (`karteId`, `nummer`, `titel`, `spalte`). Geliefert wird das gebaute `Karte`-DTO plus Ort — eine zweite, abgespeckte Kartengestalt wäre eine zweite Wahrheit über dieselbe Sache und liefe beim ersten neuen Kartenfeld auseinander.

### Die Adresse

`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten[?archiviert=]` — Unterressource der Kartenklasse nach dem gebauten Muster `…/spalten/{spalteId}/karten` (`KartenEndpunkte.cs:13`). **Keine Erfindung dieses Slice:** `/api/boards/{boardId}/kartenklassen/{kartenklasseId}` steht seit `R00022` im `Location`-Kopf der Anlage; hier kommt `karten` daran.

Adressiert wird über die **`KartenklasseId`**, nicht über das Präfix: die Id-Konvention und jede gebaute Unterressource führen den Schlüssel, und ein Präfix ist nur je Board und nur `COLLATE NOCASE` eindeutig — es wäre eine zweite Adressierung derselben Sache.

### Ablauf

1. **Karten einer Kartenklasse abrufen** (`GET /api/boards/{boardId}/kartenklassen/{kartenklasseId}/karten?archiviert=`)
   - 1.1 `Archivfilter.Aus(archiviert, route)` — ohne Parameter der aktive Bestand
     - 1.1.1 Unlesbarer Wert → HTTP 400 mit `archiv-filter-unlesbar`, die Kompensation nennt die aufgerufene Adresse
   - 1.2 `KartenklassenService.LadeKartenDerKartenklasse(boardId, kartenklasseId, archivstand)`
     - 1.2.1 Board unbekannt → `Nichtgefunden.Board(boardId)` → HTTP 404
     - 1.2.2 Kartenklasse gehört nicht zu diesem Board → `BoardDerKartenklasse(kartenklasseId)` entscheidet zwischen `Nichtgefunden.Kartenklasse(…)` und `Nichtgefunden.FremdeKartenklasse(…)` → HTTP 404, **ohne** Lesezugriff auf Karten
     - 1.2.3 Sonst: `KartenklassenRepository.LadeKartenDerKartenklasse(…)`
   - 1.3 HTTP 200 mit der Liste — auch wenn sie leer ist
2. **Die Karten lesen** (`Kartenleser`)
   - 2.1 `JOIN Kartenklassenzuordnung z ON z.Karte = k.KarteId` — **der Filter**: nur zugeordnete Karten, und nur die dieser Kartenklasse
   - 2.2 `JOIN Spalte s ON s.SpalteId = k.Spalte` — liefert `Spalte` und `Spaltenbezeichnung`; der Board-Skopus fällt über die Kartenklasse ohnehin, die Spalte wird für die Bezeichnung gebraucht
   - 2.3 `LEFT JOIN Kartenklasse n` für das Präfix, `LEFT JOIN` auf Erledigung, Archivierung und Eigenschaft wie in den Schwesterabfragen
   - 2.4 Archivfilter wie in `LiesKartenEinerSpalte`: ohne Parameter `a.Karte IS NULL`, mit `?archiviert=true` `a.Karte IS NOT NULL`
   - 2.5 `ORDER BY z.Zaehlerstand` — aufsteigend
3. **Keine Kürzung**
   - 3.1 `Abschlussbahn.Gekuerzt` und `Abschlussbahn.InAnzeigereihenfolge` kommen in diesem Weg **nicht** vor: die Ordnung ist die des Nummernkreises, und gekürzt wird nichts

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenklassenEndpunkte`** — die dritte Route dieser Ressourcenfamilie, erstmals eine Unterressource.
- **`Kartenleser`** — der eine Ort, an dem entschieden wird, was als Bestand gilt; der Archivfilter sitzt dort und aus demselben Grund wie bei `LiesKartenEinerSpalte`.
- **`KartenklassenService`** — heute zwei Methoden, hier die dritte; das Muster liefert `KartenService.LadeKartenDerSpalte` (`:609`).
- **`FehlervertragTests`** — jede neue Route mit Fehlerantwort zieht ihre drei Vertragsfälle **in derselben Bubble** mit (`:53-59`).

**Klassen-Entwurf:**

- `Klassenkarte` (DTO, immutable, `KanbanC.Contracts/Klassen/`) — die Karte plus der Ort, an dem sie liegt. Genau die Zusammensetzung, die `Kartendetail` schon macht; `Board` und `Boardname` fehlen, weil sie in der Adresse stehen.
  - `record Klassenkarte(Karte Karte, long Spalte, string Spaltenbezeichnung)`
- `IKartenklassenRepository` (Interface, wächst) — eine Methode mehr, damit der Dienst gegen `TestKartenklassenRepository` prüfbar bleibt.
  - `IReadOnlyList<Klassenkarte>? LadeKartenDerKartenklasse(long boardId, long kartenklasseId, Archivierung archivstand)` — `null` heißt „diese Kartenklasse gehört nicht zu diesem Board"
- `KartenklassenRepository` (Provider) — prüft die Zugehörigkeit über `Kartenklassenleser.LiesKartenklasseDesBoards` und liest dann über den `Kartenleser`. Nur lesend, keine Transaktion nötig.
- `Kartenleser` (Provider, wächst) — eine Leseabfrage mehr, nach dem Muster von `LiesKartenEinerSpalte`, mit `JOIN Kartenklassenzuordnung` als Filter und `ORDER BY z.Zaehlerstand`.
  - `IReadOnlyList<Klassenkarte> LiesKartenDerKartenklasse(IDbConnection verbindung, IDbTransaction? transaktion, long kartenklasseId, Archivierung archivstand)`
- `KartenklassenService.LadeKartenDerKartenklasse` (Integration) — erst das Board, dann die Kartenklasse, dann die Karten; die Unterscheidung „gibt es nicht" gegen „gibt es, nur nicht hier" über `BoardDerKartenklasse`. IOSP: gelesen und geprüft wird darunter.
  - `Ergebnis<IReadOnlyList<Klassenkarte>> LadeKartenDerKartenklasse(long boardId, long kartenklasseId, Archivierung archivstand)`
- **Kein Validator** — es gibt nichts syntaktisch zu prüfen; der Archivfilter prüft sich an der Grenze selbst, alles andere braucht den Bestand.
- **Kein `KartenklassenApiKlient`-Glied, keine Komponente, kein E2E** — siehe „Warum kein Schirm".

### Änderungen an bestehenden Klassen

- `KartenklassenEndpunkte` — eine Routenkonstante und ein Handler mehr; `FehlervertragTests` zieht **in derselben Bubble** mit, weil der Vertragstest rot wird, sobald eine Route ohne Vertragsfall registriert ist.
- `IKartenklassenRepository`, `KartenklassenRepository`, `TestKartenklassenRepository` — je eine Methode mehr.
- `Kartenleser` — eine Leseabfrage mehr; die bestehenden bleiben unverändert.
- **`Nichtgefunden` wächst nicht** — `Kartenklasse` und `FremdeKartenklasse` stehen seit `R00023` und werden hier nur benutzt.
- **`Karte`, `Kartendetail`, `Kartenklasse`, `Kartennummer`, `Abschlussbahn`, `Archivfilter` werden nicht angefasst.**

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KartenklassenService.LadeKartenDerKartenklasse` gegen `TestKartenklassenRepository` — unbekanntes Board liefert `board-unbekannt`; eine Kartenklasse, die es nirgends gibt, liefert `kartenklasse-unbekannt`; eine Kartenklasse eines **anderen** Boards liefert `kartenklasse-fremd` und **nicht** `kartenklasse-unbekannt` (die Unterscheidung ist der ganze Zweck des zweiten Codes); bei jedem Befund wurde **nicht** auf Karten zugegriffen; der Erfolg reicht die Liste unverändert durch, auch die leere.

**Integration:** `Kartenleser` und `KartenklassenRepository.LadeKartenDerKartenklasse` gegen eine `TemporaereDatenbank` — Karten aus **drei** Spalten kommen in **einer** Antwort; Karten anderer Klassen, ohne Klasse und eines zweiten Boards mit **demselben Präfix** fehlen; die Ordnung ist `Zaehlerstand` aufsteigend, geprüft an den Ständen **99 und 100** gegen die Textsortierung; eine Abschlussspalte mit **Anzeigegrenze 1** liefert trotzdem beide Karten; ohne Parameter fehlen die archivierten, mit `?archiviert=true` kommen genau sie; die leere Kartenklasse liefert eine leere Liste, die fremde `null`. `KartenklassenEndpunkte` über `TestWebApi` — 200 (Bestand, leer, archiviert), 400 (unlesbarer Archivfilter) und 404 (unbekanntes Board, unbekannte Kartenklasse, fremde Kartenklasse) samt Rumpf; `FehlervertragTests` ruft die neue Route ab. **Der Bestandsfall bekommt einen eigenen Test** (`B0318`, nur Test, kein Produktionscode — Muster `B0017`): die Zusage „genau ihre Karten, ohne die übrigen" fällt erst auf, wenn all das andere danebenliegt.

**E2E: keiner in diesem Slice.** Er hat keine Oberfläche; der Aufruf ist hier selbst die äußere Systemgrenze, und die Integrationsebene ist genau diese Grenze. Die bestehenden E2E-Suiten laufen unverändert als Gegenprobe, dass die Oberfläche nicht berührt wurde.

Repositories, `Kartenleser`, `Kartenklassenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00023`** (Karte einer Klasse zuordnen — `I0021`, grün). Das ist der einzige Knoten der WBS-Spalte `Braucht` von `I0022`; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00022`** (`I0020`, grün — Kartenklasse, Route, `Kartennummer`), **`R00016`** (`I0014`, grün — `Archivfilter` und die Haltung, wo er sitzt), **`R00015`** (`I0013`, grün — Abschlussspalte und Anzeigegrenze, gegen die hier ausdrücklich **nicht** gekürzt wird), **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`). Die Spalte `Braucht` nennt diese nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **formal niemanden.** Die Rückwärtssuche in der WBS findet **keinen** Knoten, der `I0022` in seiner Spalte `Braucht` nennt — `I0030`, `I0031`, `I0033` und `I0038` nennen `I0021`. Fachlich sind sie trotzdem die Verbraucher dieses Abrufs (Notiz an `I0022`); die Lücke ist unter „Offene Fragen" mit ihrer Adresse benannt.

## Umfang

```
Karten einer Klasse abrufen (I0022) = 4 Bubbles: 3 Standard (2,8h), 1 unklar (0,4–1,5h).
Rest: 2,8h klar + 0,4–1,5h unklar · 2 von 4 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 4 Bubbles gruen (0 %) · 0 laufen · 4 offen
```

`I0022` ist vollständig bis zur Bubble geplant und trägt seine vier Bubbles (`B0315`–`B0318`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: der Slice hat **einen** prüfbaren Aspekt, den Abruf. **Die Requirement-Klammer sitzt deshalb allein an `I0022`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0315` Karten einer Kartenklasse lesen | Contracts + Provider | 0,4h (belegt) |
| `B0316` Abruf der Kartenklasse verdrahten | Integration | 0,4h (belegt) |
| `B0317` Endpunkt der Karten einer Kartenklasse | Integration | 2h (Richtwert) |
| `B0318` Genau ihre Karten, ohne die übrigen | Integration, nur Test | 0,4–1,5h (**unklar**) |

Es ist der kleinste Slice von `D0005` — halb so viele Bubbles wie `I0020`, gut ein Drittel von `I0021`. Der Grund ist genau die Entscheidung oben: keine Oberfläche heißt keine Klienten-, keine Komponenten- und keine E2E-Bubble. Die eine unklare Bubble ist `B0318` und unklar **wegen des Arrange**, nicht wegen des Assert: drei Spalten, zwei Kartenklassen und zwei Boards aufzubauen ist die Arbeit, das Zählen danach ist trivial. Derselbe Vermerk wie bei `I0005` bis `I0021`: der 2h-Richtwert für Endpunkt-Bubbles liegt über den gemessenen Werten vergleichbarer Bubbles; die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0022` trägt keine eigene Zählzeile; sie hält die Entscheidung gegen den Schirm fest. Die Zahlen oben sind über die Aufwandsspalte der vier Bubbles gezählt.

## Offene Fragen

- **Bleibt es dabei, dass dieser Slice keine Oberfläche bekommt?** — **entschieden: ja**, mit der Begründung unter „Warum kein Schirm". Das ist die auffälligste Entscheidung an dieser Anforderung und die einzige, die den Zuschnitt des Slice ändert, wenn sie fällt: ein Klassenfilter in Zone 3 wäre dann **ein eigener Slice** in der WBS, nicht eine Bubble hier, und er hätte die beiden Fragen zu beantworten, die das Artboard aufwirft — was eine gefilterte Bahn in ihrer Kartenzahl zeigt, und was Ziehen in einer gefilterten Ansicht heißt. **Adresse:** `/planung verfeinern D0005`. Nicht am Menschen geprüft.
- **Ist die Sortierung aufsteigend richtig?** — **entschieden: ja.** Das Artboard zeigt sein Beispiel absteigend, trifft dazu aber keine Aussage; die drei Zeilen sind ein Zitat aus `D0001`/`D0003`. Aufsteigend ist die Leserichtung eines Nummernkreises und die Ordnung, die `R00022` für die Klassenliste schon gewählt hat. Fiele die Entscheidung anders, wäre es eine Zeile im `ORDER BY` — reversibel, solange niemand die Reihenfolge in einem Import verankert. Nicht am Menschen geprüft.
- **Braucht der Abruf eine Seitung oder ein Limit?** — **entschieden: nein.** Ein Board mit tausend Karten einer Klasse ist im LAN-Betrieb einer Person kein Lastfall, und die Zusage „genau ihre Karten" verträgt keine stille Grenze. Käme die Frage später, wäre die Antwort ein ausdrücklicher Parameter mit Gesamtzahl in der Antwort — wie `Spalte.Kartenzahl` es neben der gekürzten Liste vormacht — und nicht eine unsichtbare Obergrenze.
- **Soll ein Agent auch klassenübergreifend abrufen können („alle Karten mit irgendeiner Klasse")?** — **out of scope, und die Adresse ist bekannt:** `I0037` „Rohdaten über die API abrufen" hat genau diese Aufgabe und ist noch nicht verfeinert. Hier ein zweiter Weg wäre eine zweite Wahrheit.
- **Warum nennt kein Knoten `I0022` in seiner Spalte `Braucht`, obwohl `I0030`, `I0031`, `I0033` und `I0038` diesen Abruf fachlich brauchen?** — **Befund, nicht hier zu beheben.** Alle vier nennen `I0021`; die Notiz an `I0022` führt sie ausdrücklich als „wo die Fähigkeit sichtbar wird". Ob die Vorbedingung wirklich `I0021` ist (die Karten hängen am Nummernkreis) oder `I0022` (das Set ist über eine Adresse greifbar), entscheidet die Verfeinerung dieser Knoten. **Adresse:** `/planung aendern I0030` bzw. die Verfeinerung von `D0006`/`D0007`. Diese Anforderung ändert die WBS nicht über ihre eigene Klammer hinaus.
- ~~Wird die Kartenklasse über ihre Kennung oder über ihr Präfix adressiert?~~ — **entschieden: über die `KartenklasseId`.** Das Artboard lässt es ausdrücklich offen (`:361`). Ein Präfix ist nur je Board und nur `COLLATE NOCASE` eindeutig; es wäre eine zweite Adressierung derselben Sache.
- ~~Wird die Anzeigegrenze der Abschlussspalte angewandt?~~ — **entschieden: nein.** Sie ist eine Anzeigeregel des Boards; ein Abruf, der stillschweigend Karten wegließe, bräche das Fertig-Kriterium und wäre für einen Agenten eine stille Lüge. Dieselbe Haltung wie `LadeKartenDerSpalte` und wie das Fertig-Kriterium von `I0037` („vollständig und ohne Limit").
- ~~Bekommt die Karte eine eigene, abgespeckte Antwortgestalt?~~ — **entschieden: nein.** Die Karte reist als das gebaute `Karte`-DTO, der Umschlag legt nur den Ort dazu.
- ~~Ist eine Kartenklasse ohne Karten ein Fehler?~~ — **entschieden: nein, 200 mit leerer Liste** — dieselbe Entscheidung wie beim Board ohne Kartenklasse in `R00022`.

## Manuelle Vorbereitungstätigkeiten

- Keine. Der Slice liest nur; es gibt keine Migration.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Zusage, die zwei Slices lang aufgebaut und noch nicht eingelöst wurde: die Vision verlangt, dass ein Agent „gezielt das richtige Set greift statt des ganzen Boards" — heute kann er die Nummern **sehen**, aber das Set nicht **holen**. Die Kausalkette: **wenn** die Kartenklasse eine eigene Unterressource bekommt, die über `JOIN Kartenklassenzuordnung` genau ihre Karten sammelt (X), **dann** ist ein Set eine einzige Adresse statt eines Boardabrufs mit anschließendem Filtern über Präfixe (Y), **und dann** können der WBS-Import seinen Ist-Bestand vergleichen (`I0030`/`I0031`), der Soll-Ist-Vergleich seinen Kartenbestand bestimmen (`I0033`) und der Export ein Board vollständig herausschreiben (`I0038`) — jeder von ihnen über dieselbe geprüfte Adresse statt über eine eigene Nachbildung (Z). **Der Hebel liegt beim Datenzugriff, nicht bei einer Ansicht**: das Filtern über Präfixe wäre in jedem Verbraucher wiederholbar, aber jede Wiederholung wäre eine Stelle, an der die Abschlussgrenze des Boards still Karten frißt — genau der Fehler, den ein Agent nicht bemerken kann. Und der Hebel liegt hier und nicht in einem Oberflächenfilter: der Filter zeigt einem Menschen weniger, dieser Abruf gibt einer Maschine alles.

## Missing-Docs

- **Ein `JOIN` als Filter über eine Zuordnungstabelle mit gleichzeitigem `LEFT JOIN` auf die Archivierung:** die bestehenden Leseabfragen des `Kartenleser` filtern über `WHERE`, nicht über die Kartenzugehörigkeit. Dass ein `JOIN Kartenklassenzuordnung` die klassenlosen Karten bereits ausschließt und der Archivfilter trotzdem als `LEFT JOIN` + `WHERE` daneben stehen muss, ist im Repository an keiner Stelle belegt.
- **`ORDER BY` über eine Spalte der Zuordnungstabelle statt über die der Hauptressource:** im Bestand ordnet jede Kartenabfrage nach `k.Position` oder `k.Spalte`. Ob die Ordnung nach `z.Zaehlerstand` im Zusammenspiel mit `UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand` ohne zusätzlichen Index auskommt, ist unbelegt — die Menge ist klein genug, dass es hier nicht auffällt, aber der Befund gehört nach dem Bau festgehalten.

## Notizen

### Warum der Ort mitreist und das Board nicht

Der Abruf sammelt über die Spaltengrenze hinweg — genau das, was ein Boardabruf nicht leistet. Eine Karte ohne ihre Spalte bliebe damit die Auskunft schuldig, die den Abruf erst nützlich macht: *wo steht das gerade?* Das Board dagegen steht in der Adresse; es mitzuschicken hieße, dieselbe Angabe in jeder Zeile zu wiederholen, die der Aufrufer selbst eingetippt hat. Das ist derselbe Unterschied, der `Kartendetail` sein `Board` gibt und `Klassenkarte` nicht: die Kartenadresse `/api/karten/14` kennt kein Board, diese Adresse kennt es.

### Verworfene Alternativen

- **Ein Abfrageparameter an `GET /api/boards/{boardId}`** (`?kartenklasse=`) — beschnitte die Boardantwort und wirft genau die Frage auf, die das Artboard für den Oberflächenfilter zurückweist: was zeigt dann die Kartenzahl je Spalte. Eine Ressource, die je nach Parameter etwas anderes bedeutet, ist für einen Agenten die schlechtere Adresse.
- **Eine flache Route `GET /api/karten?kartenklasse=`** — neue Wurzelressource ohne Vorbild im Bestand; verlöre den Board-Skopus und damit den 404-Weg über das Board, und der Befund „gibt es, nur nicht hier" hätte kein Board mehr zu nennen.
- **Adressierung über das Präfix** (`…/kartenklassen/WBS-/karten`) — nur je Board und nur `COLLATE NOCASE` eindeutig; eine zweite Adressierung derselben Sache neben der `KartenklasseId`.
- **Eine eigene, schlanke Kartengestalt für diese Antwort** — eine zweite Wahrheit über dieselbe Sache, die beim ersten neuen Kartenfeld auseinanderliefe.
- **Sortierung über `Karte.Kartennummer`** — als Text stünde `WBS-100` vor `WBS-99`, weil `Kartennummer.Aus` nur zweistellig auffüllt. Der Zählerstand ist dieselbe Ordnung, nur exakt.
- **Die Anzeigegrenze auch hier anwenden** — bräche das Fertig-Kriterium „genau ihre Karten" und wäre nicht als Fehler erkennbar, sondern als Erfolg mit zu wenig Inhalt.
- **Ein `KartenklassenApiKlient.LadeKartenDerKartenklasse` „für später"** — tote Flexibilität (C24). Wenn `I0030` ihn braucht, baut `I0030` ihn, und dann mit einem Aufrufer daneben.

### Bewusst out of scope

- **Klassenfilter in der Oberfläche.** Eigener Slice, sobald ihn jemand bestellt; die beiden Fragen, die er zu beantworten hätte, stehen im Artboard.
- **Klassenübergreifender Rohdatenabruf.** Das ist `I0037`.
- **Seitung, Limit, Gesamtzahl.** Siehe offene Fragen — nicht ohne Anlass.
- **Filtern nach Spalte, Etikett, Verantwortlichem oder Erledigungsstand innerhalb der Klasse.** Der Slice hat einen prüfbaren Aspekt; jede weitere Achse ist eine eigene Frage mit eigenem Rand.
- **Zugriffsschutz.** Die Anwendung läuft im LAN ohne Anmeldung — Leitplanke der Vision.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber nicht am Menschen geprüft. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Keine Oberfläche** — die auffälligste Annahme; sie stammt aus der Zerlegung (`S4`) und dem Artboard, nicht aus diesem Dokument, und wird hier zur Bestätigung vorgelegt.
2. **Aufsteigende Sortierung**, obwohl das Artboard sein Beispiel absteigend zeigt.
3. **Kein Limit und keine Seitung.**
4. **Die Route heißt `…/kartenklassen/{kartenklasseId}/karten`** und weicht damit von `…/klassen/1/karten` im Artboard ab — dieselbe Entscheidung wie in `R00022` und `R00023`.
5. **Die Antwort trägt das ganze `Karte`-DTO** statt der vier im Artboard skizzierten Felder.
