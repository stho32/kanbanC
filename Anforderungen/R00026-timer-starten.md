---
id: R00026
status: In Arbeit
datum: 2026-09-06
---

# R00026: Timer starten

## Beschreibung

Auf einer Karte lässt sich für den **gewählten Kontributor** ein Timer starten. Danach ist der laufende Timer **von außen zu sehen**: der Boardabruf trägt ihn, die Kartenseite sagt „läuft seit 08:04" statt einen Startknopf anzubieten, und die Karte in der Bahn trägt eine Plakette, die den Reload überlebt. **Gestoppt wird in diesem Slice nichts** — das ist `I0024`; hier entsteht der offene Eintrag und nur er.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt. Ein Timer, den man startet und stoppt, ohne Umstand." Und auf die Zusage darüber: „Der Agent bewegt Karten, legt Aufgaben an und **erfasst Zeiten über die API**; … an jeder Karte und jeder **Zeit** ist ablesbar, wer oder was gehandelt hat."

**Dies ist der erste Slice von `D0006` „Zeiterfassung"** — und der erste Slice überhaupt, der Zeit erfasst. Er legt die Tabelle `Zeiteintrag`, die Antwortgestalt `Zeiteintrag` und den `ZeitenService` an; `I0024` bis `I0027` bauen darauf.

**Die auffälligste Entscheidung dieses Slice:** ein Kontributor darf **mehrere Timer zugleich** laufen lassen. Die Obergrenze ist nicht der Kontributor, sondern das Paar (Karte, Kontributor). Sie steht unter „Technische Überlegungen → Die Entscheidung über mehrere Timer" mit ihrem **Preis** und ist hier zu bestätigen oder zu verwerfen, nicht in der Umsetzung.

## Geschäftlicher Nutzen

Ohne Zeiterfassung ist die halbe Vision unerreichbar: der Soll-Ist-Vergleich gegen die WBS-Zählung, der Puffer-Verbrauch der Critical Chain, die Zeiten je Aufgabe und Kontributor und „dieselben Ist-Zeiten als Futter für die KI" hängen alle an Einträgen, die es heute nicht gibt. Dieser Slice ist der erste Spatenstich: er schafft den Gegenstand (`Zeiteintrag`), die Adresse (`POST …/zeiten/laufend`) und die Sichtbarkeit — und zwar **auf beiden Seiten der Systemgrenze**, weil das Fertig-Kriterium „ist als laufend erkennbar" verlangt, dass ein Mensch es sieht und ein Agent es zurücklesen kann.

Der zweite Nutzen ist der billigere Einstieg für den Menschen: ein Timer, der mit **einem Klick auf der Karte** anfängt, wird benutzt; einer, der ein Formular verlangt, nicht. Deshalb entsteht hier der Knopf und nicht das Nachtragen (`I0025`).

## Funktionale Anforderungen

- Auf einer Karte lässt sich für einen Kontributor eine Zeitmessung starten: `POST /api/karten/{karteId}/zeiten/laufend` mit dem Kontributor im Rumpf.
- Der gestartete Eintrag trägt Karte, Kontributor und Beginn und hat **kein Ende**; „läuft" heißt genau das.
- Der laufende Eintrag ist **zurückzulesen**: über `GET /api/boards/{boardId}` (alle laufenden des Boards) und über `GET /api/karten/{karteId}` (die Einträge dieser Karte).
- Die Kartenseite bietet „Timer starten" an; läuft für die gewählte Identität schon einer, steht dort stattdessen „läuft seit <Uhrzeit>".
- Die Karte in der Bahn trägt eine Plakette, solange auf ihr ein Timer läuft — gefüllt für die eigene Identität, ruhig für einen fremden Timer.
- **Ein Kontributor darf auf mehreren Karten zugleich messen.** Auf **derselben** Karte bekommt er keinen zweiten laufenden Eintrag: der zweite Start ist **idempotent** und liefert den bereits laufenden zurück.
- Ohne gewählte Identität öffnet der Klick auf „Timer starten" die **Identitätswahl**; nach der Wahl läuft der Timer, ohne dass ein zweites Mal geklickt werden muss.
- Ein **stillgelegter** Kontributor bekommt keinen Timer; unbekannte Karte und unbekannter Kontributor werden mit Befund zurückgewiesen.
- **Kein Eintrag wird in diesem Slice geschlossen.** `Ende` bleibt über den ganzen Slice `NULL`.

## Nicht-funktionale Anforderungen

- **Jede Interaction gilt über beide Systemgrenzen** (Leitplanke an `A0001`): was die Oberfläche kann, kann die API. Dieser Slice ist der ausdrückliche Fall — „ist als laufend erkennbar" wird sowohl im Browser als auch in der Boardantwort geprüft.
- **Fehlerantworten für Agenten:** jeder Befund nennt Grund **mit Werten** und die **Kompensationsaktion**, auch der 404 (Projektregel).
- **`Nichtgefunden` wächst nicht.** `Karte(karteId)` und `Kontributor(kontributorId)` stehen seit den Kommentar- und Anhang-Slices und werden hier nur benutzt. `Stillgelegt.Zeitmesser(kontributorId)` kommt hinzu — **fünfte Schwester mit demselben Code** `kontributor-stillgelegt` und eigener Meldung.
- **Begriff (C06):** `Zeiteintrag` im ganzen Stack — Tabelle, Contracts, Leser, Dienst, Route (`zeiten`). Kein zweiter Begriff für dieselbe Sache; insbesondere **kein DTO `LaufenderTimer`**: `Ende is null` heißt „läuft", wie `StillgelegtAm is null` „aktiv" heißt.
- **C07:** Bezeichner ohne echte Umlaute (`ae/oe/ue/ss`); UI-Texte, Meldungen und Kommentare **mit** echten Umlauten.
- **C08:** `Zeiteintrag` und `ZeitmessungStartenAnfrage` sind immutable Records in `KanbanC.Contracts`.
- **C24 (keine tote Flexibilität):** kein Stopp-, Nachtrag- oder Löschglied „für später" — `I0024` bis `I0027` bauen ihres selbst, mit Aufrufer daneben.
- **Migration `018`** als eigene, idempotente Datei mit `CREATE TABLE IF NOT EXISTS`; **kein `ALTER TABLE ADD COLUMN`**, weil der `Migrationslaeufer` jedes Skript bei jedem Start ohne Journal ausführt.
- **Zeit als `TEXT` in ISO-8601 UTC**, Umrechnung sichtbar in C# über `ParseExact("O")` — Muster `Kommentarleser`, `Anhangleser`, `Dateiverweisleser`; belegt in `SqliteEigenschaftenTests`.
- **Gestaltungswerte aus `gestaltung.css`**, nie als Literal in eine Komponenten-CSS (Projektregel); kein CSS-Framework.
- **Die Kernregel bleibt:** `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL` — die Oberfläche spricht auch hier ausschließlich HTTP.

## Akzeptanzkriterien

Das Fertig-Kriterium des Slice lautet wörtlich: **„Ein Timer läuft auf einer Karte für den gewählten Kontributor und ist als laufend erkennbar."** Die Gruppen unten zerlegen genau diesen Satz in das, was von außen prüfbar ist.

### Ein Timer läuft — auf einer Karte, für den gewählten Kontributor

- [x] `POST /api/karten/{karteId}/zeiten/laufend` mit `{"kontributor": 3}` antwortet mit **201** und einem `Zeiteintrag`, der `karte`, `kontributor` und `beginn` trägt.
- [x] Der gelieferte Eintrag hat **kein Ende** (`ende` ist `null`) — und behält es über den ganzen Slice.
- [x] Der Kontributor reist als **ganzer Kontributor** (Nummer, Name, Art, `stillgelegtAm`), nicht als nackte Nummer — dieselbe Gestalt wie `Kommentar.Urheber`.
- [x] `beginn` ist ein Zeitpunkt in UTC im Format `O`; zwei unmittelbar nacheinander gestartete Timer tragen zwei Zeitpunkte, die sich in ihrer Ordnung nicht widersprechen.
- [x] Der Kontributor wird **mitgegeben und nie erraten**: ein Aufruf ohne `kontributor` wird zurückgewiesen und legt nichts an.

### Ist als laufend erkennbar — über die API

- [x] `GET /api/boards/{boardId}` trägt nach dem Start eine Liste **laufender** Zeiteinträge, in der der neue Eintrag mit **Karte**, **Kontributor** und **Beginn** steht.
- [x] Diese Liste enthält **nur** Einträge ohne Ende und **nur** solche zu Karten dieses Boards.
- [x] `GET /api/karten/{karteId}` trägt den Eintrag in seiner Zeiteinträge-Liste, in Beginn-Folge.
- [x] Ein Board **ohne** laufenden Timer liefert eine **leere** Liste — kein `null`, kein Fehler.
- [x] Nach einem Neustart der WebApi steht derselbe laufende Eintrag noch da: er liegt in der Datenbank, nicht im Prozessgedächtnis.

### Ist als laufend erkennbar — in der Oberfläche

- [x] Die Kartenseite `/karten/{karteId}` zeigt bei gewählter Identität einen Abschnitt „Zeiten" mit dem Knopf **„Timer starten"**.
- [x] Nach dem Start steht an derselben Stelle **„läuft seit <Uhrzeit>"** und **kein** Startknopf mehr.
- [x] Die Karte in der Spaltenbahn trägt eine Plakette, solange auf ihr ein Timer läuft; ohne laufenden Timer trägt sie **keine**.
- [x] Läuft der Timer für die **gewählte** Identität, ist die Plakette gefüllt und nennt die Startzeit; läuft ein **fremder**, ist sie ruhig und nennt den fremden Kontributor. Unterschieden wird über **Füllung und Wortlaut, nie über die Farbe** — Olive und Terrakotta tragen in diesem Canvas die *Art* des Kontributors.
- [x] Beides **überlebt den Reload**: nach `F5` steht dieselbe Plakette und dieselbe Zeile — der Zustand kommt aus der API, nicht aus dem Browser.
- [x] Ohne gewählte Identität gibt es kein „mich": jeder laufende Timer wird als fremder dargestellt.

### Mehrere Timer je Kontributor sind erlaubt

- [x] Läuft für einen Kontributor bereits ein Timer auf Karte A, so lässt sich für **denselben** Kontributor auf Karte B ein zweiter starten: **201**, und danach laufen **beide**.
- [x] Der Boardabruf zeigt beide; auf dem Board stehen dann **zwei** gefüllte Plaketten.
- [x] Zwei **verschiedene** Kontributoren dürfen auf **derselben** Karte gleichzeitig messen: der zweite Start antwortet mit **201**, und die Karte trägt danach zwei laufende Einträge.
- [x] Laufen auf einer Karte mehrere Timer, zeigt die Plakette den **eigenen** zuerst, sonst den **am längsten laufenden**; der `title` nennt alle.

### Der zweite Start auf derselben Karte ist idempotent

- [x] Ein zweiter `POST …/zeiten/laufend` mit **demselben** Kontributor auf **derselben** Karte antwortet mit **200** — nicht 201, nicht 400, nicht 409 — und liefert **denselben** Eintrag mit **unverändertem** `zeiteintragId` und **unverändertem** `beginn`.
- [x] Danach gibt es zu diesem Paar (Karte, Kontributor) **genau einen** offenen Eintrag; die Zeiteinträge der Karte sind um **keinen** gewachsen.
- [x] **Rechenbeispiel:** Karte 14, Kontributor 3. Start → 201, `zeiteintragId` = 7, `beginn` = 08:04. Zweiter Start → 200, `zeiteintragId` = **7**, `beginn` = **08:04**. Dritter Start desselben Kontributors auf Karte 21 → 201, `zeiteintragId` = 8. Der Boardabruf zeigt danach **zwei** laufende Einträge (7 und 8), nicht drei und nicht einen.
- [x] Die Schranke steht **im Schema** und nicht nur im Dienst: ein Schreibweg, der am Dienst vorbeigeht, läuft in einen sichtbaren Anschlag statt still einen zweiten offenen Eintrag anzulegen.

### Ohne gewählte Identität

- [x] Ein Klick auf „Timer starten" **ohne** Eintrag in `sessionStorage` öffnet die **Identitätswahl** — keine Fehlermeldung: „nicht gewählt" ist eine fehlende Angabe, und die Wahl ist die Kompensationsaktion.
- [x] Nach der Wahl **läuft der Timer unmittelbar**, ohne zweiten Klick.
- [x] Die gewählte Identität gilt anschließend auch in der Kopfzeile — es entsteht kein zweiter Identitätsbegriff.

### Fehlerantworten für Agenten

- [x] **Unbekannte Karte** → HTTP 404 mit `karte-unbekannt`, Grund **mit der Nummer** und Kompensationsaktion.
- [x] **Unbekannter Kontributor** → HTTP 404 mit `kontributor-unbekannt`, Grund mit der Nummer und Kompensationsaktion.
- [x] **Stillgelegter Kontributor** → HTTP **400** mit `kontributor-stillgelegt`; die Meldung sagt, dass er **keine Zeit mehr erfassen** kann — nicht „kann nicht verantwortlich sein", nicht „kann keinen Kommentar mehr schreiben", nicht „kann keine Datei mehr anhängen" und nicht „kann auf keine Datei mehr verweisen": alle vier wären hier eine Falschaussage.
- [x] Jede Zurückweisung hinterlässt **keinen** Eintrag; nach einem abgelehnten Aufruf ist die Zeiteinträge-Liste der Karte unverändert.
- [x] `FehlervertragTests` deckt die neue Route ab; sie steht **nicht** auf `RoutenOhneFehlerantwort`.

### Was dieser Slice ausdrücklich nicht tut

- [ ] Es gibt **keinen** Weg, einen Eintrag zu beenden — kein Stoppknopf, keine Route, keine Methode. `Ende` ist über den ganzen Slice `NULL`, und ein Test belegt das nach jedem Szenario.
- [ ] Es gibt **keine** Zeitensumme, **keine** Eintragsliste in der Oberfläche und **kein** Nachtragen — das sind `I0025` und `I0026`.
- [ ] Es gibt **keine** Kopfzeilen-Plakette „laufende Timer" — das ist `I0027`.

### Der grüne Bestand bleibt grün

- [x] Die Testsuiten aus `R00001`–`R00025` laufen unverändert weiter; insbesondere die `R00005`-Suite, die 17 `ToHaveCountAsync`-Zusagen der `LayoutModusE2ETests` und die Ziehproben (`KindZiehbarkeitProbeE2ETests`, `VerweisInZiehbarerKarteProbeE2ETests`) — die Karte in der Bahn wächst um eine Plakette.
- [x] `GET /api/boards/{boardId}` und `GET /api/karten/{karteId}` antworten im Übrigen unverändert; die Abschlussspalte kürzt weiter wie bisher.
- [x] Der zweite Lauf der Migrationen auf einer bestehenden Datei lässt Schema und Daten unverändert.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Zeiten/Zeiteintrag.cs` und `Source/KanbanC.Contracts/Zeiten/ZeitmessungStartenAnfrage.cs` (neu, immutable Records) — der Ordner `Zeiten/` liegt seit dem Anlegen leer im Bestand und ist genau dafür da. Dazu `Boards/Board.cs` (ein Feld mehr) und `Karten/Kartendetail.cs` (eine Liste mehr).
- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/018-zeiteintrag.sql` (neu).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Zeiten/Zeitenleser.cs` und `Source/KanbanC.BL/Persistenz/Zeiten/ZeitenRepository.cs` (neu), `Source/KanbanC.BL/Interfaces/Zeiten/IZeitenRepository.cs` (neu); `Persistenz/Boards/BoardRepository.cs` und `Persistenz/Karten/KartenRepository.cs` reichen die neuen Listen in ihre Antwortgestalten durch.
- **Dienste:** `Source/KanbanC.BL/Integrations/Zeiten/ZeitenService.cs` (neu) — **kein** sechstes Glied am `KartenService`.
- **Fehler:** `Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs` — eine Schwester mehr (`Zeitmesser`). **`Nichtgefunden` bleibt unberührt.**
- **API:** `Source/KanbanC.WebApi/Endpunkte/ZeitenEndpunkte.cs` (neu), Registrierung in `Program.cs`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/ZeitenApiKlient.cs` (neu); `Components/Pages/Kartendetail.razor(.css)` (Zeitenblock in der rechten Eigenschaftenspalte), `Components/Karten/Karte.razor(.css)` (Plakette in der Kartenkopfzeile, rechts neben der Nummer), `Components/Spalten/Spaltenbahnen.razor` (reicht die laufenden Einträge je Karte durch), `Components/Pages/Board.razor` (reicht sie von der Boardantwort weiter); `Components/Layout/Identitaetswahl.razor` wird auf der Kartenseite ein zweites Mal eingesetzt.
- **Unberührt:** `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs`, `Operations/Karten/Abschlussbahn.cs`, `Operations/Boards/Archivfilter.cs`, sämtliche bestehenden Validatoren (**kein Validator** in diesem Slice).
- **Tests:** `Source/KanbanC.BL.Tests/Integrations/Zeiten/ZeitenServiceTests.cs` und `TestHelpers/TestZeitenRepository.cs` (neu); `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Zeiten/ZeitenRepositoryTests.cs` und `Api/ZeitenEndpunkteTests.cs` (neu), `Persistenz/MigrationslaeuferTests.cs` und `Api/FehlervertragTests.cs` wachsen mit; `Source/KanbanC.Blazor.Tests/Services/ZeitenApiKlientTests.cs` (neu, Fehlerpfade); `Source/KanbanC.PlaywrightTests/Tests/TimerStartenE2ETests.cs` (neu) mit Locatoren in `PageObjects/KartendetailSeite.cs` und `PageObjects/BoardSeite.cs`.

## Technische Überlegungen

### Die Entscheidung über mehrere Timer — und ihr Preis

Das Artboard lässt die Frage **ausdrücklich offen** und zeichnet beide Lesarten nebeneinander: Rand B zeigt „Für dich läuft schon ein Timer auf WBS-21" mit den Knöpfen **„Umschalten"** und **„Beide laufen lassen"**, darüber ein gestrichelter Kasten „nicht entschieden · I0023" (`Dokumentation/Wireframes/D0006.dc.html:632-654`), und die Lesehilfe führt es unter „Bewusst offen gelassen" (`:689`).

**Entschieden ist: beide laufen lassen** — mit genau einer Schranke, dem Paar (Karte, Kontributor). Zwei Gründe, beide aus dem Bestand:

1. **„Höchstens einer je Kontributor" ist eine Regel über die Arbeitsweise von *Menschen*.** Ein Mensch arbeitet an einer Sache; ein Agent rechnet an mehreren zugleich. Die Vision stellt beide gleichberechtigt — der Agent müsste eine seiner Karten falsch buchen oder gar nicht. Eine Obergrenze, die für die eine Hälfte der Akteure passt und die andere zum Falschbuchen zwingt, ist keine Regel, sondern eine Annahme über den Benutzer.
2. **Die gezeichnete Auflösung „Umschalten" hieße, dass `I0023` beim Start ein *Ende* schreibt.** Damit nähme dieser Slice `I0024` („Der gestoppte Timer hinterlässt einen Zeiteintrag mit Beginn, Ende und Kontributor") seine Aufgabe vorweg und wäre nicht mehr für sich prüfbar.

Warum die Schranke trotzdem existiert: zwei offene Einträge **desselben** Kontributors auf **derselben** Karte wären zwei Antworten auf eine Frage („seit wann arbeitet er hier?") und zählten dieselbe Uhrzeit doppelt. Sie steht deshalb **im Schema** — partieller `UNIQUE`-Index auf (`Karte`, `Kontributor`) `WHERE Ende IS NULL` — nach dem Muster von `UNIQUE(Kartenklasse, Zaehlerstand)` aus `R00023`: der lesbare Befund entsteht davor im Dienst, der Index fängt jeden Weg ab, der am Dienst vorbeischreibt.

**Der Preis dieser Wahl, ausdrücklich benannt statt stillschweigend in Kauf genommen:**

- Die Zusage des Artboards **„Es gibt höchstens eine solche Plakette je Bahnenbild"** (`D0006.dc.html:350`) **fällt.** Laufen zwei eigene Timer auf demselben Board, stehen zwei gefüllte Plaketten da. Das ist eine bewusste Abweichung vom Bild, keine Nachlässigkeit.
- Die **Kopfzeilen-Plakette aus `I0027`** („1:36 · 2 laufen", `:423`) kann ihre Dauer nicht mehr eindeutig **einer** eigenen Uhr zuordnen: bei zwei eigenen Timern gibt es keine „meine Zeit" mehr, sondern zwei. Was sie dann zeigt — die längste, die zuletzt gestartete, eine Summe oder gar keine Zahl —, **entscheidet `I0027`**. Hier steht der Befund und die Adresse, nicht die Lösung.
- Der **Moment aus Rand B mit seinen drei Knöpfen entsteht nie**: der zweite Start läuft einfach. Ein gezeichneter Dialog fällt damit ersatzlos weg.

**Ein zweiter Start auf derselben Karte ist idempotent, kein Fehler.** Das Ziel des Aufrufs — „für mich läuft hier ein Timer" — ist bereits erreicht; ein Befund wäre eine **Meldung ohne Kompensationsaktion**, dieselbe Begründung wie in `R00023` beim Lösen einer nicht bestehenden Zuordnung. Die zwei Erfolgsstatus an einer Route sind Absicht: **201 sagt „jetzt läuft er", 200 sagt „er lief schon"** — ein Agent, dessen Antwort unterwegs verlorenging, wiederholt den Aufruf gefahrlos.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0006.dc.html`](../Dokumentation/Wireframes/D0006.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gelten daraus **Zustand 1** (`:91`, der Zeitenblock in der rechten Eigenschaftenspalte der Kartenseite), **Zustand 2** (`:248`, dieselbe Stelle mit laufendem Timer) und **Zustand 3** (`:328`, die Karte in der Bahn in ihren drei Fassungen) sowie **Rand A** (`:607`, Start ohne gewählte Identität). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen **keine** Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Plakette zeigt „läuft seit 08:04" statt der verstrichenen Dauer „1:36 läuft"** (`D0006.dc.html:340`, ebenso der Knopf in Zustand 2, `:263`). Eine gerenderte Dauer ist **ab der ersten Sekunde falsch**, solange kein Live-Kanal sie nachführt (`D0007`, `I0028`); eine Startzeit bleibt wahr, wie lange die Seite auch offen steht. Ein Sekundentakt je Karte über den Blazor-Kreislauf wäre zudem in jedem E2E-Lauf ein Wackelkandidat. **Aus dem Stoppknopf des Bildes wird hier die stille Zeile „läuft seit"** — `I0024` macht daraus den Knopf.
2. **Die Route heißt `POST /api/karten/{karteId}/zeiten/laufend`, nicht `POST /api/karten/14/zeiten`** (`:587`). Das Artboard nennt seinen Aufruf selbst „Entwurf, keine Zusage" (`:590`). `POST …/zeiten` bleibt dem **Nachtragen** aus `I0025` — einem Eintrag mit Beginn **und** Ende —, und `GET /api/zeiten/laufend` bleibt `I0027`. Start und Nachtrag sind zwei Fragen und bekommen zwei Adressen.
3. **Höchstens eine gefüllte Plakette je Bahnenbild gilt nicht mehr** (`:350`) — Folge der Entscheidung oben, dort begründet.

### Die Adresse

`POST /api/karten/{karteId:long}/zeiten/laufend` — **boardlose Unterressource der Karte**, wie Teilaufgaben, Kommentare, Anhänge und Dateiverweise (`KartenEndpunkte.cs:35-57`). Kein Konflikt mit dem späteren `…/zeiten/{zeiteintragId:long}`: der `long`-Constraint trennt die beiden Wege, dasselbe Muster wie bei `/boards/{boardId:long}`.

Rumpf: `{"kontributor": 3}`. **Der Kontributor wird mitgegeben und nie erraten** — die Identität ist ein Browserzustand je Tab (`Identitaetsspeicher`, `sessionStorage`, `R00013`) und kein Login. Dasselbe tut `KommentarSchreibenAnfrage` für den Urheber. **Kein Feld für den Beginn:** den setzt die Anwendung; könnte der Aufrufer ihn mitgeben, könnte ein Agent die Reihenfolge fälschen — dieselbe Entscheidung wie beim Kommentarzeitpunkt.

### Wo die laufenden Einträge reisen — und wo nicht

`Board` wächst um **`LaufendeZeiteintraege`**, `Kartendetail` um die **siebte Liste `Zeiteintraege`**. **Nicht `Karte`:** dort wären es 57 Konstruktionsstellen und vier Leseabfragen des `Kartenleser` für eine n-Beziehung. Das Gegenbeispiel `Kartennummer` aus `R00023` trug **ein skalares Feld** — das ist der Unterschied. Am `Board` ist es **ein** Feld, 19 Konstruktionsstellen und **eine** zusätzliche Abfrage je Boardabruf; die Zuordnung Karte-zu-Eintrag macht die Bahn.

Damit ist „laufend" in der Bahn und auf der Kartenseite **ohne zweiten Abruf** zu sehen — über `GET /api/boards/{boardId}` auch für einen Agenten. Der **Archivstand filtert hier nicht**: die Bahn zeigt ohnehin nur ihre eigenen Karten, und ein laufender Timer auf einer archivierten Karte ist ein Befund, den `I0027` sehen soll, kein Rauschen.

### Ablauf

1. **Timer starten** (`POST /api/karten/{karteId}/zeiten/laufend`)
   - 1.1 `ZeitenService.StarteZeitmessung(karteId, anfrage)` — **kein Validator**: es gibt nichts syntaktisch zu prüfen, `long` ist `long`; beide Regeln brauchen den Bestand und sitzen deshalb im Dienst
     - 1.1.1 Kontributor unbekannt → `Nichtgefunden.Kontributor(kontributorId)` → HTTP 404
     - 1.1.2 Kontributor stillgelegt → `Stillgelegt.Zeitmesser(kontributorId)` → HTTP 400
     - 1.1.3 Sonst: `ZeitenRepository.StarteZeitmessung(karteId, kontributorId, uhrzeit)`
   - 1.2 Karte unbekannt (Repository liefert `null`) → `Nichtgefunden.Karte(karteId)` → HTTP 404
   - 1.3 Neuer Eintrag → HTTP **201**; bereits laufender Eintrag → HTTP **200** mit demselben Eintrag
2. **Schreiben** (`ZeitenRepository`, **eine** Transaktion)
   - 2.1 Laufenden Eintrag des Paares (Karte, Kontributor) suchen — `WHERE Ende IS NULL`
   - 2.2 Gefunden → unverändert zurückgeben, **nichts schreiben**
   - 2.3 Nicht gefunden → Zeile mit `Beginn` und `Ende = NULL` anlegen
   - 2.4 Suchen und Schreiben stehen in **einer** Transaktion, damit zwischen „läuft schon einer?" und „dann schreibe ich einen" kein Fenster bleibt; greift die Serialisierung nicht, schlägt der partielle `UNIQUE`-Index sichtbar an, statt still einen zweiten Eintrag zu legen
   - 2.5 **Die Uhr wird hereingereicht**, nicht im Repository gelesen — sonst wäre der Beginn im Test nicht setzbar (Muster `IKartenRepository`)
3. **Zurücklesen** (`Zeitenleser`)
   - 3.1 `LadeDerKarte(karteId)` — `JOIN Kontributor` und `LEFT JOIN Kontributorstilllegung`, `ORDER BY Beginn, ZeiteintragId`; wandert in `Kartendetail.Zeiteintraege`
   - 3.2 `LadeLaufendeDesBoards(boardId)` — `WHERE Ende IS NULL` über die Karten des Boards, flache Liste, jede Zeile mit ihrer `Karte`; wandert in `Board.LaufendeZeiteintraege`
   - 3.3 Zeitpunkt als `string` in der Lesezeile, Umrechnung über `ParseExact("O")`
4. **In der Oberfläche**
   - 4.1 Kartenseite: `Kartendetail.Zeiteintraege` + gewählte Identität → Startknopf **oder** „läuft seit <Uhrzeit>"
   - 4.2 Klick ohne Identität → `Identitaetswahl` öffnen; nach der Wahl **unmittelbar** starten, ohne zweiten Klick
   - 4.3 Bahn: `Board.LaufendeZeiteintraege` → `Spaltenbahnen` reicht je Karte durch → `Karte.razor` zeigt gefüllte oder ruhige Plakette
     - 4.3.1 Mehrere Timer auf einer Karte: der eigene hat Vorrang, sonst der am längsten laufende; der `title` nennt alle
     - 4.3.2 Ohne gewählte Identität ist jeder Timer ein fremder

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **Migration `018-zeiteintrag.sql`** — die erste Tabelle des Dialogs `D0006`; `CREATE TABLE IF NOT EXISTS`, weil der `Migrationslaeufer` kein Journal kennt.
- **`ZeitenEndpunkte`** — eine neue Ressourcenfamilie an der Karte; `FehlervertragTests` zieht **in derselben Bubble** mit, weil der Vertragstest rot wird, sobald eine Route ohne Vertragsfall registriert ist.
- **`BoardRepository` und `KartenRepository`** — die zwei Stellen, an denen die neuen Listen in `Board` und `Kartendetail` gelangen.
- **`Karte.razor`, Kartenkopfzeile** — dieselbe Zeile, in der seit `R00023` die Nummernplakette steht: links die Nummer, rechts der Timer.

**Klassen-Entwurf:**

- `Zeiteintrag` (DTO, immutable, `KanbanC.Contracts/Zeiten/`) — **eine** Antwortgestalt für alle fünf Interactions des Dialogs; `Ende is null` heißt „läuft".
  - `record Zeiteintrag(long ZeiteintragId, long Karte, Kontributor Kontributor, DateTimeOffset Beginn, DateTimeOffset? Ende)`
- `ZeitmessungStartenAnfrage` (DTO, immutable, `KanbanC.Contracts/Zeiten/`) — trägt den Kontributor in den Aufruf, wie `KommentarSchreibenAnfrage` den Urheber.
  - `record ZeitmessungStartenAnfrage(long Kontributor)`
- `IZeitenRepository` (Interface) — damit der Dienst gegen ein Test-Repository prüfbar bleibt.
  - `Zeitmessungsstart? StarteZeitmessung(long karteId, long kontributorId, DateTimeOffset beginn)` — `null` heißt „diese Karte gibt es nicht"
- `Zeitmessungsstart` (DTO, immutable) — der Eintrag **plus die Auskunft, ob er neu ist**; sie entscheidet zwischen 201 und 200 und darf nicht aus dem Eintrag geraten werden.
  - `record Zeitmessungsstart(Zeiteintrag Zeiteintrag, bool IstNeu)`
- `ZeitenRepository` (Provider, wirft) — sucht und schreibt in **einer** Transaktion; die Uhr wird hereingereicht.
- `Zeitenleser` (Provider) — die beiden Leseabfragen; Zeitpunkt als Text, `ParseExact("O")`. Muster `Kommentarleser`.
  - `IReadOnlyList<Zeiteintrag> LiesZeiteintraegeDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)`
  - `IReadOnlyList<Zeiteintrag> LiesLaufendeZeiteintraegeDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)`
- `ZeitenService` (Integration, fängt/loggt) — **eigener Dienst statt eines sechsten Glieds am `KartenService`**: der trägt schon Etiketten, Teilaufgaben, Kommentare, Anhänge und Dateiverweise auf über 600 Zeilen, und Zeiten sind ein eigenes Unterthema (C24/Kohäsion).
  - `Ergebnis<Zeitmessungsstart> StarteZeitmessung(long karteId, ZeitmessungStartenAnfrage anfrage)`
- `Stillgelegt.Zeitmesser(long kontributorId)` (Operation, wächst) — **fünfte Schwester** mit demselben Code `kontributor-stillgelegt`, demselben Kompensationsweg und eigener Meldung.
- `ZeitenEndpunkte` (Integration) — eine Route, zwei Erfolgsstatus, 400 und 404 je mit Befund.
- `ZeitenApiKlient` (Integration, `KanbanC.Blazor/Services/`) — JSON in beide Richtungen, Muster `KartenApiKlient`; Fehlerpfade in `KanbanC.Blazor.Tests`.
  - `Task<ApiErgebnis<Zeiteintrag>> StarteZeitmessung(long karteId, ZeitmessungStartenAnfrage anfrage)`
- **Kein Validator**, **kein `LaufenderTimer`-DTO**, **kein Stopp-, Nachtrag- oder Löschglied** — siehe „Nicht-funktionale Anforderungen".

### Änderungen an bestehenden Klassen

- `Board` (Contracts) — ein Feld `LaufendeZeiteintraege` mehr; **19 Konstruktionsstellen** in vier Dateien ziehen mit.
- `Kartendetail` (Contracts) — eine siebte Liste `Zeiteintraege`; die Konstruktionsstellen ziehen mit.
- `BoardRepository`, `KartenRepository` — je eine zusätzliche Leseabfrage im bestehenden Leseweg.
- `Stilllegt`/`Stillgelegt` (Operations/Fehler) — eine Schwester mehr; **`Nichtgefunden` wächst nicht**.
- `Program.cs` (WebApi) — `ZeitenEndpunkte` und `ZeitenService` registrieren; `Program.cs` (Blazor) — `ZeitenApiKlient` registrieren.
- `Kartendetail.razor(.css)` — der Zeitenblock in der rechten Eigenschaftenspalte unter „Klasse und Nummer", wo `D0004.dc.html` den Kasten „Zeiten und Timer · D0006 · I0023–I0026" schon freihält; dazu die zweite Einsetzung von `Identitaetswahl`.
- `Karte.razor(.css)`, `Spaltenbahnen.razor`, `Board.razor` — Plakette und Durchreichung.
- `KartendetailSeite`, `BoardSeite` (Seitenobjekte) — je ein paar Locatoren mehr.
- **Unberührt:** `Nichtgefunden`, `Abschlussbahn`, `Archivfilter`, `KartenService`, sämtliche Validatoren.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `ZeitenService.StarteZeitmessung` gegen `TestZeitenRepository` — unbekannter Kontributor liefert `kontributor-unbekannt`, **ohne** Schreibzugriff; ein stillgelegter liefert `kontributor-stillgelegt` mit **400**, ebenfalls ohne Schreibzugriff; eine unbekannte Karte liefert `karte-unbekannt`; der Erfolg reicht `Zeitmessungsstart` unverändert durch, und `IstNeu` kommt aus dem Repository und wird **nicht** abgeleitet.
- `Stillgelegt.Zeitmesser` — derselbe Code wie die vier Schwestern, eigene Meldung, derselbe Kompensationsweg.

**Integration:** `Migrationslaeufer` mit `018` — Tabelle, Index auf `Karte` und der **partielle** `UNIQUE`-Index entstehen; der zweite Lauf lässt Schema und Daten unverändert. Der partielle Index bekommt einen **eigenen** Test: zwei offene Einträge desselben Paares laufen in den Anschlag, ein **zweiter Kontributor** auf derselben Karte geht durch. `ZeitenRepository` gegen eine `TemporaereDatenbank` — Start legt einen Eintrag ohne `Ende` an; der zweite Start desselben Paares gibt **denselben** Eintrag zurück und schreibt **nichts**; ein anderer Kontributor auf derselben Karte und derselbe Kontributor auf einer anderen Karte legen jeweils an; eine unbekannte Karte liefert `null`. `Zeitenleser` — die Einträge einer Karte in Beginn-Folge, die laufenden eines Boards über alle Spalten, ein Board ohne Timer liefert eine leere Liste, ein Eintrag auf einer archivierten Karte fällt **nicht** heraus. `ZeitenEndpunkte` über `TestWebApi` — 201, 200 (zweiter Start), 400 (stillgelegt) und 404 (unbekannte Karte, unbekannter Kontributor) samt Rumpf; `FehlervertragTests` ruft die neue Route ab. `BoardEndpunkteTests` und `KartenEndpunkteTests` — die neuen Listen erscheinen in den Antworten und sind ohne laufenden Timer leer.

**Blazor-Tests (unterhalb E2E):** `ZeitenApiKlient` — Erfolg, Zurückweisung mit lesbarem Befund und nicht erreichbare WebApi; über den Browser nicht auslösbar, das ist der Grund, aus dem dieses Testprojekt existiert (CLAUDE.md, Abweichung 4).

**E2E:** Identität wählen, auf der Kartenseite starten; der Block zeigt „läuft seit", die Plakette steht auf der Karte in der Bahn und **überlebt den Reload**; ohne Identität öffnet der Klick die Wahl und der Timer läuft danach ohne zweiten Klick; ein zweiter Timer desselben Menschen auf einer **zweiten** Karte läuft daneben; ein zweiter Start auf **derselben** Karte legt **keinen** zweiten Eintrag an. Die letzten beiden Schritte sind die sichtbare Probe auf die Entscheidung über mehrere Timer und zugleich der Grund, aus dem dieser Slice **ohne** `I0024` prüfbar ist: geprüft wird das **Zurücklesen** des Zustands, nicht sein Ende.

Repositories, `Zeitenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün) und **`R00013`** (Identität wählen — `I0008`, grün). Das sind genau die zwei Knoten der WBS-Spalte `Braucht` von `I0023`; beide sind erfüllt, der Slice ist **frei**. `I0008` steht dort ausdrücklich wegen Rand A: der Wireframe-Index hält bei `I0008` fest, dass Variante C ein Gegenmittel braucht — „der Timer (`I0023`) erzwingt die Wahl, bevor er läuft".
- Setzt außerdem auf: **`R00011`**/**`R00014`** (Kontributor mit Art und Stilllegung — `I0005`, `I0009`, grün), **`R00019`** (Muster für Urheber im Aufruf, Zeitpunkt als TEXT, `Kommentarleser`), **`R00023`** (Nummernplakette in derselben Kartenkopfzeile, partieller Eindeutigkeitsindex als Vorbild), **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (`gestaltung.css`). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **`I0024`** („Timer stoppen") und **`I0027`** („Laufende Timer sehen") — beide nennen `I0023` in ihrer Spalte `Braucht`. `I0025` und `I0026` hängen über `I0024` mittelbar daran.

## Umfang

```
Timer starten (I0023) = 11 Bubbles: 7 Standard (9,2h), 4 unklar (3,2–8,5h).
Rest: 9,2h klar + 3,2–8,5h unklar · 3 von 11 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0023` ist vollständig bis zur Bubble geplant und trägt seine elf Bubbles (`B0319`–`B0329`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: der Slice hat **einen** prüfbaren Aspekt — der Start ist erst getan, wenn er von außen zu sehen ist; Start und Sichtbarkeit teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg, dieselbe Lage wie bei `I0020` bis `I0022`. **Die Requirement-Klammer sitzt deshalb allein an `I0023`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0319` Zeiteintragstabelle anlegen | Provider (Migration) | 0,4h (belegt) |
| `B0320` Timer starten | Provider | 0,4–1,5h (**unklar**) |
| `B0321` Zeiteinträge am Kartendetail lesen | Contracts + Provider | 0,4h (belegt) |
| `B0322` Laufende Zeiteinträge am Board lesen | Contracts + Provider | 0,4–1,5h (**unklar**) |
| `B0323` Timerstart verdrahten | Integration | 0,4h (belegt) |
| `B0324` Endpunkt des Timerstarts | Integration | 2h (Richtwert) |
| `B0325` API-Klient der Zeiten | Integration | 2h (Richtwert) |
| `B0326` Zeitenblock der Kartenseite mit Startknopf | UI | 2h (Richtwert) |
| `B0327` Timer starten ohne gewählte Identität | UI | 0,4–1,5h (**unklar**) |
| `B0328` Laufplakette auf der Karte in der Bahn | UI | 2h (Richtwert) |
| `B0329` E2E Timer starten | E2E | 2–4h (**unklar**) |

Mit 11 Bubbles ist das derselbe Umfang wie `I0021` und deutlich mehr als `I0022` (vier) — der Grund ist genau umgekehrt zu dort: dieser Slice **hat** eine Oberfläche, und zwar an zwei Stellen (Kartenseite und Bahn) plus einem Randfall mit eigener Komponente. Die vier unklaren Bubbles haben vier verschiedene Ursachen: `B0320` hält drei Idempotenzfälle unter **einem** Schloss (kein Eintrag, ein offener desselben Kontributors, ein offener eines **anderen** Kontributors auf derselben Karte — nur der mittlere gibt zurück statt zu schreiben); `B0322` zieht **19 `new Board(…)`-Stellen** und die Lesewege des `BoardRepository` mit; `B0327` muss das Identitätspopover samt Auffangfläche und Escape-Behandlung entweder aus `Kopfzeile.razor` herauslösen oder auf der Kartenseite ein zweites Mal aufhängen; `B0329` braucht zwei Karten, zwei Startwege und den Reload. Derselbe Vermerk wie bei `I0005` bis `I0022`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles; die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0023` trägt keine eigene Zählzeile; sie hält die sechs Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der elf Bubbles gezählt.

## Offene Fragen

- **Bleibt es dabei, dass ein Kontributor mehrere Timer zugleich laufen lassen darf?** — **entschieden: ja**, mit der Begründung unter „Die Entscheidung über mehrere Timer". Das ist die auffälligste Entscheidung dieser Anforderung und die einzige, die das Schema ändert, wenn sie fällt: „höchstens einer je Kontributor" hieße einen partiellen `UNIQUE`-Index auf (`Kontributor`) statt auf (`Karte`, `Kontributor`) — **und dazu, dass `I0023` beim Start ein `Ende` schreibt**, womit dieser Slice `I0024` vorwegnähme und nicht mehr für sich prüfbar wäre. Das Artboard lässt die Frage ausdrücklich offen (`D0006.dc.html:632-654`, `:689`). **Nicht am Menschen geprüft.**
- **Was zeigt die Kopfzeilen-Plakette aus `I0027` bei zwei eigenen laufenden Timern?** — **Befund, nicht hier zu entscheiden.** Das Bild zeigt „1:36 · 2 laufen" (`D0006.dc.html:423`) und setzt genau eine eigene Uhr voraus. Ob dort die längste, die zuletzt gestartete, eine Summe oder gar keine Zahl steht, ist eine Frage der Anzeige und gehört in den Slice, der sie baut. **Adresse:** `/planung verfeinern I0027`, spätestens `/anforderung aus-slice I0027`.
- **Dürfen sich zwei Zeiteinträge desselben Kontributors zeitlich überlappen?** — **offen, hier nicht zu entscheiden und ausdrücklich nicht geraten.** Kein Fertig-Kriterium sagt etwas dazu, und das Artboard führt es unter „Bewusst offen gelassen" (`:676`, `:689`). Der partielle Index beantwortet nur den Fall **gleiche Karte**; über zwei *abgeschlossene* oder einen abgeschlossenen und einen laufenden Eintrag auf **verschiedenen** Karten sagt dieser Slice nichts — er kann es auch nicht, weil er keinen Eintrag schließt. Die Frage entsteht erst mit dem Nachtragen von Hand. **Adresse:** `I0025` („Zeiteintrag nachtragen und ändern"). Solange sie offen ist, gilt: überlappende laufende Einträge auf verschiedenen Karten sind **erlaubt** — das ist die unmittelbare Folge der Entscheidung oben und keine zweite, stillschweigende Wahl.
- **Zeigt die Plakette die Startzeit oder die verstrichene Dauer?** — **entschieden: die Startzeit** („läuft seit 08:04"). Das Artboard zeichnet die Dauer. Fiele die Entscheidung anders, brauchte es entweder den Live-Kanal aus `I0028` oder einen Sekundentakt je Karte — Ersteres ist ein eigener Slice, Letzteres macht jeden E2E-Lauf wackelig. Reversibel, sobald `I0028` steht. **Nicht am Menschen geprüft.**
- **Zwei Erfolgsstatus (201/200) an einer Route — oder immer 200?** — **entschieden: zwei.** 201 sagt „jetzt läuft er", 200 sagt „er lief schon"; die Unterscheidung ist die einzige Stelle, an der ein Agent den Unterschied überhaupt erfahren kann. Immer 200 wäre einfacher und verschwiege ihn. **Nicht am Menschen geprüft.**
- **Warum trägt `Board` die laufenden Einträge und nicht `Karte`?** — **entschieden: `Board`.** An `Karte` wären es 57 Konstruktionsstellen und vier Leseabfragen für eine n-Beziehung; am `Board` ein Feld, 19 Stellen und eine Abfrage. Der Preis: die Bahn muss selbst zuordnen, welcher Eintrag zu welcher Karte gehört. **Nicht am Menschen geprüft.**
- **Wandert das Identitätspopover aus `Kopfzeile.razor` heraus oder entsteht es auf der Kartenseite ein zweites Mal?** — **offen, Entscheidung des Entwicklers beim Bauen** (`B0327`). `Identitaetswahl.razor` ist als Komponente schon zerlegt; unklar ist allein die Aufhängung — Popover, Auffangfläche und Escape-Behandlung stecken heute in der Kopfzeile. Beide Wege erfüllen das Kriterium; der zweite verdoppelt Markup, der erste berührt eine grüne Komponente.

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration `018` läuft beim Start der WebApi mit.

## Manuelle Nachbereitungstätigkeiten

- Keine. Bestehende Datenbestände bekommen die Tabelle beim nächsten Start; alte Boards haben schlicht keine laufenden Einträge.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Lücke, die die Vision an drei Stellen zugleich aufreißt: „Zeiterfassung, die zum Arbeiten passt", „an jeder Karte und **jeder Zeit** ist ablesbar, wer gehandelt hat" und „dieselben Ist-Zeiten als Futter für die KI" — heute gibt es **keinen einzigen Zeiteintrag**, und deshalb weder Soll-Ist-Vergleich noch Puffer-Verbrauch noch Export von Zeiten. Die Kausalkette: **wenn** eine Tabelle `Zeiteintrag` entsteht, in der ein Eintrag **ohne Ende** genau „läuft" bedeutet, und ein Start ihn über eine Adresse anlegt, die Karte und Kontributor nennt (X), **dann** existiert der Gegenstand, an dem alle weiteren Slices des Dialogs arbeiten können — stoppen (`I0024`), nachtragen (`I0025`), summieren (`I0026`), übersehen (`I0027`) —, **und dann** bekommen Burndown, Soll-Ist und Export ihre Datengrundlage statt eines leeren Feldes (Z). **Der Hebel liegt beim Start und nicht beim Stoppen**, obwohl das Stoppen den vollständigen Eintrag erzeugt: ein Stopp ohne vorherigen Start ist unmöglich, und ein Start, den man nicht sieht, wird nicht benutzt — deshalb bringt dieser Slice die Sichtbarkeit gleich mit, statt sie `I0027` zu überlassen. Und der Hebel liegt bei der **Karte** und nicht bei einer eigenen Zeitenseite: gestartet wird dort, wo gearbeitet wird, sonst kostet der Timer mehr Aufmerksamkeit, als er wert ist — genau das meint „ohne Umstand" im Zielbild.

## Missing-Docs

- **Partielle Indizes in SQLite** (`CREATE UNIQUE INDEX … WHERE Ende IS NULL`): im Repository neu. Unbelegt ist, ob Microsoft.Data.Sqlite in der eingesetzten Fassung die Verletzung mit demselben `SqliteException`-Fehlercode meldet wie ein vollständiger `UNIQUE`-Index — und ob der Index nach dem Schließen eines Eintrags (`I0024`) tatsächlich sofort wieder Platz für einen neuen offenen macht.
- **Zwei Erfolgsstatus (201 und 200) an derselben Minimal-API-Route:** der Bestand liefert je Route genau einen Erfolgsstatus. Wie sich das mit `TypedResults` sauber ausdrücken lässt, ohne den Rückgabetyp zu verlieren, ist im Repository an keiner Stelle vorgemacht.
- **`DateTimeOffset?` als Nullwert aus einer SQLite-TEXT-Spalte:** dass `DateTimeOffset` nicht materialisiert, ist belegt (`SqliteEigenschaftenTests`); der **nullable** Fall wird hier zum ersten Mal gelesen — auch wenn `Ende` in diesem Slice immer `NULL` bleibt, ist das genau der ungeprüfte Pfad.

## Notizen

### Warum der laufende Timer keinen eigenen Gegenstand bekommt

Ein DTO `LaufenderTimer` neben `Zeiteintrag` sähe zunächst ehrlicher aus: es könnte `Ende` weglassen und hätte damit keinen unmöglichen Zustand. Der Preis wäre eine **zweite Wahrheit über denselben Gegenstand** — derselbe Eintrag hieße je nach Blickwinkel anders, und `I0024` müsste ihn beim Stoppen von der einen Gestalt in die andere übersetzen. Der Bestand entscheidet dieselbe Frage schon zweimal so: `Kontributor.StillgelegtAm is null` heißt „aktiv" statt eines zweiten Typs, und `Karte` trägt ihre Archivierung nicht als eigene Klasse. `Ende is null` heißt „läuft" — ein Feld, eine Regel, fünf Interactions.

### Verworfene Alternativen

- **„Höchstens ein Timer je Kontributor", Auflösung durch Umschalten** — die gezeichnete Variante aus Rand B. Verworfen, weil sie eine Regel über Menschen auf Agenten überträgt und weil `I0023` damit beim Start ein `Ende` schriebe und `I0024` seine Aufgabe vorwegnähme.
- **„Höchstens ein Timer je Kontributor", Auflösung durch Zurückweisung** — bequemer zu bauen, aber die Meldung hätte keine Kompensationsaktion außer „stoppe den anderen", und ein Agent an zwei Karten müsste eine davon falsch buchen.
- **`ALTER TABLE Karte ADD COLUMN Timerbeginn`** — eine Karte trägt n Zeiteinträge; und der `Migrationslaeufer` führt jedes Skript bei jedem Start ohne Journal aus, ein `ALTER TABLE` scheiterte im zweiten Lauf (zehnter Fall derselben Lage).
- **`Ende` erst mit `I0024` anlegen** — die Tabelle wüchse nachträglich, was mit `CREATE TABLE IF NOT EXISTS` und ohne Journal genau nicht geht. `Ende TEXT NULL` steht deshalb von Anfang an drin.
- **Die laufenden Einträge an `Karte` hängen** — 57 Konstruktionsstellen und vier Leseabfragen für eine n-Beziehung; das Gegenbeispiel `Kartennummer` trug ein skalares Feld.
- **`Beginn` als `DateTimeOffset`-Spalte** — Dapper materialisiert ihn nicht aus SQLite und meldet stattdessen einen fehlenden Konstruktor; die Meldung zeigt nicht auf die Ursache.
- **Den Kontributor serverseitig erraten** — es gibt keinen Login; die Identität ist ein Browserzustand je Tab. Geraten hieße: immer denselben nehmen.
- **Ein sechstes Glied am `KartenService`** — der Dienst trägt schon fünf Unterthemen auf über 600 Zeilen; Zeiten sind ein eigenes.
- **Die verstrichene Dauer rendern** — ab der ersten Sekunde falsch ohne Live-Kanal, und ein Sekundentakt je Karte machte jeden E2E-Lauf wackelig.
- **`POST /api/karten/{karteId}/zeiten` als Startadresse** (wie gezeichnet) — dieselbe Adresse müsste später auch den Nachtrag mit Beginn **und** Ende annehmen; zwei Fragen an einer Adresse, unterschieden nur durch die Anwesenheit eines Feldes.

### Bewusst out of scope

- **Stoppen** — `I0024`. Kein Eintrag wird hier geschlossen.
- **Nachtragen, Ändern, Löschen von Zeiteinträgen** — `I0025`, samt der Frage nach Überlappungen.
- **Zeitenliste und Summe je Kontributor auf der Karte** — `I0026`. Dieser Slice baut den **Block**, nicht seinen Inhalt.
- **Laufende Timer in der Kopfzeile** — `I0027`.
- **Live-Nachführung ohne Reload** — `I0028`/`D0007`.
- **Das „Soll" aus dem Bild** („von 3:00 Soll") — hat unter `D0004`/`D0006` **keinen Knoten**; es gehört zur Schätz-Rückkopplung der Vision und braucht einen eigenen Slice, bevor es gebaut wird.
- **Zugriffsschutz** — die Anwendung läuft im LAN ohne Anmeldung; Leitplanke der Vision.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Mehrere Timer je Kontributor sind erlaubt**, die Obergrenze ist das Paar (Karte, Kontributor) — die auffälligste Annahme; sie stammt aus der Zerlegung (`S4`), nicht aus diesem Dokument, und wird hier zur Bestätigung vorgelegt. Ihr **Preis** (eine Artboard-Zusage fällt, die `I0027`-Plakette verliert ihre eindeutige Dauer) ist oben benannt.
2. **Der zweite Start auf derselben Karte ist idempotent** und antwortet mit 200 statt eines Befunds.
3. **Die Plakette zeigt „läuft seit <Uhrzeit>"** statt der gezeichneten Dauer.
4. **Die Route heißt `POST …/zeiten/laufend`** und weicht damit vom gezeichneten `POST …/zeiten` ab.
5. **Die laufenden Einträge reisen an `Board` und `Kartendetail`**, nicht an `Karte`.
6. **`Ende TEXT NULL` steht von Anfang an im Schema**, obwohl erst `I0024` die Spalte füllt.

**Nicht angenommen und ausdrücklich offen gelassen:** ob sich Zeiteinträge überlappen dürfen. Diese Frage wird hier **nicht** beantwortet, weil kein Fertig-Kriterium etwas dazu sagt und dieser Slice keinen Eintrag schließt; ihre Adresse ist `I0025`.
