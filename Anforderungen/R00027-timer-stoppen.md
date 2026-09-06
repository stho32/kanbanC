---
id: R00027
status: Neu
datum: 2026-09-06
---

# R00027: Timer stoppen

## Beschreibung

Ein laufender Timer lässt sich beenden. Danach ist der Eintrag **vollständig**: er trägt `Beginn`, `Ende` und den Kontributor, für den gemessen wurde, und ist über `GET /api/karten/{karteId}` in genau dieser Gestalt zurückzulesen. Auf der Kartenseite wird aus der stillen Zeile „läuft seit 08:04" ein Knopf **„Stoppen"**, danach steht dort wieder „Timer starten"; die Plakette auf der Karte in der Bahn verschwindet von selbst.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt. Ein Timer, den man startet und **stoppt**, ohne Umstand." `R00026` hat den Timer startbar gemacht; erst dieser Slice erzeugt den **abgeschlossenen** Eintrag, von dem Soll-Ist-Vergleich, Burndown und Zeitexport später leben.

**Kein Schema:** `Ende TEXT NULL` steht seit Migration `018` in der Tabelle (`R00026`, `B0319`). Dieser Slice **füllt** die Spalte, ohne die Tabelle anzufassen und ohne neue Migration.

**Die auffälligste Entscheidung dieses Slice:** **jeder darf stoppen, auch einen fremden Timer** — der Aufruf bekommt deshalb **keinen** Kontributor mitgegeben. Sie steht unter „Technische Überlegungen → Wer stoppen darf" mit ihrer Begründung und ist hier zu bestätigen oder zu verwerfen, nicht in der Umsetzung.

## Geschäftlicher Nutzen

Ein Timer, den man nicht beenden kann, ist keine Zeiterfassung, sondern ein Zähler. Solange kein Eintrag ein `Ende` trägt, gibt es **keine einzige gemessene Dauer** — und damit keinen Soll-Ist-Vergleich gegen die WBS-Zählung, keinen Puffer-Verbrauch, keine Zeitsumme je Kontributor und nichts, was ein Export (`I0038`) oder eine Auswertung lesen könnte. `I0026` („Zeiteinträge und Summe je Kontributor"), `I0033` (Soll-Ist) und `I0038` (Board exportieren) hängen alle daran, dass hier zum ersten Mal ein Eintrag geschlossen wird.

Der zweite Nutzen ist ein **Betriebsproblem**, das die Vision selbst erzeugt: Agenten laufen ohne Bildschirm und ohne Anmeldung. Ein Agenten-Timer, der über Nacht weiterläuft, muss von **irgendjemandem** beendet werden können — sonst wächst eine Messung ins Unendliche und vergiftet jede spätere Summe. Deshalb darf hier jeder stoppen.

Der dritte: erst mit dem Stopp wird der Start wiederholbar. Der partielle `UNIQUE`-Index auf (`Karte`, `Kontributor`) `WHERE Ende IS NULL` gibt das Paar frei, sobald `Ende` steht — „stoppen und später neu starten" ist damit ohne eine Zeile zusätzlichen Codes möglich, muss aber bewiesen werden.

## Funktionale Anforderungen

- Ein laufender Zeiteintrag lässt sich beenden: `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}/ende`, **ohne Rumpf**.
- Der beendete Eintrag trägt `Beginn`, `Ende` und denselben Kontributor wie zuvor — gestoppt wird der Timer, nicht die Urheberschaft.
- **Jeder darf stoppen**, auch einen fremden Timer; der Aufruf nennt keinen Kontributor.
- Das `Ende` setzt die Anwendung aus ihrer Uhr; der Aufrufer gibt keinen Zeitpunkt mit.
- Ein **zweiter** Stopp desselben Eintrags ist **idempotent**: HTTP 200 mit dem Eintrag, dessen `Ende` **unverändert** stehen bleibt.
- Eine Dauer von **null** ist erlaubt; ein `Ende` **vor** dem `Beginn` entsteht nie — bei zurückgesprungener Uhr wird auf den `Beginn` geklemmt.
- Ein **stillgelegter** Kontributor hindert den Stopp nicht: wer nicht mehr mitarbeitet, darf keinen Timer mehr *starten*, aber sein noch laufender muss beendbar sein.
- Unbekannte Karte und ein an dieser Karte unbekannter Zeiteintrag werden je mit Befund zurückgewiesen (404).
- Nach dem Stopp fällt der Eintrag aus den laufenden Einträgen des Boards heraus, und für dasselbe Paar (Karte, Kontributor) lässt sich **erneut** starten.
- Die Kartenseite bietet für den **eigenen** laufenden Timer den Knopf „Stoppen" an; danach steht dort wieder „Timer starten".

## Nicht-funktionale Anforderungen

- **Jede Interaction gilt über beide Systemgrenzen** (Leitplanke an `A0001`): was die Oberfläche kann, kann die API. **Hier kann die Oberfläche bewusst weniger** — fremde Timer sind nur über die API beendbar; die Zusage gilt in **eine** Richtung (Vorbild `I0022`). Siehe „Technische Überlegungen → Die Oberfläche kann hier weniger als die API".
- **Fehlerantworten für Agenten:** jeder Befund nennt Grund **mit Werten** und die **Kompensationsaktion**, auch der 404 (Projektregel).
- **Kein Schema und keine Migration.** `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich, und ein `ALTER TABLE ADD COLUMN` scheiterte im zweiten Lauf des `Migrationslaeufer` — deshalb steht `Ende` seit `018` in der Tabelle.
- **Begriff (C06):** `Zeiteintrag` bleibt der eine Begriff im ganzen Stack; **kein zweites DTO** für den beendeten Eintrag. `Ende is null` heißt „läuft", `Ende is not null` heißt „abgeschlossen" — ein Feld, eine Regel.
- **C07:** Bezeichner ohne echte Umlaute (`ae/oe/ue/ss`); UI-Texte, Meldungen und Kommentare **mit** echten Umlauten.
- **C08:** `Zeiteintrag` bleibt ein immutable Record in `KanbanC.Contracts`; es kommt **kein** Anfrage-DTO hinzu, weil der Aufruf keinen Rumpf hat.
- **C24 (keine tote Flexibilität):** kein `Kontributor` im Aufruf, den niemand auswertet; kein Korrektur-, Nachtrag- oder Löschglied „für später" — `I0025` und `I0026` bauen ihres selbst.
- **Zeit als `TEXT` in ISO-8601 UTC**, Umrechnung sichtbar in C# über `ParseExact("O")` — Dapper materialisiert `DateTimeOffset` nicht aus SQLite (belegt in `SqliteEigenschaftenTests`). `Ende` ist der **erste nullable** Zeitpunkt, der aus dieser Spalte gelesen und geschrieben wird.
- **Gestaltungswerte aus `gestaltung.css`**, nie als Literal in eine Komponenten-CSS (Projektregel); kein CSS-Framework.
- **Die Kernregel bleibt:** `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL` — die Oberfläche spricht auch hier ausschließlich HTTP.

## Akzeptanzkriterien

Das Fertig-Kriterium des Slice lautet wörtlich: **„Der gestoppte Timer hinterlässt einen Zeiteintrag mit Beginn, Ende und Kontributor."** Die Gruppen unten zerlegen genau diesen Satz in das, was von außen prüfbar ist.

### Der gestoppte Timer — der Stopp geschieht

- [ ] `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}/ende` **ohne Rumpf** antwortet mit **200** und dem beendeten `Zeiteintrag`.
- [ ] Es gibt **genau einen** Erfolgsstatus: 201 wäre falsch, weil nichts entsteht.
- [ ] Der Aufruf nennt **keinen** Kontributor — weder im Rumpf noch in der Adresse noch als Query.
- [ ] Ein **fremder** laufender Timer lässt sich über die API genauso beenden wie der eigene; die Antwort ist dieselbe.
- [ ] Ein Timer eines **stillgelegten** Kontributors lässt sich beenden — der Stopp wird **nicht** mit `kontributor-stillgelegt` zurückgewiesen.

### Hinterlässt einen Zeiteintrag mit Beginn, Ende und Kontributor

- [ ] Der zurückgegebene Eintrag trägt denselben `zeiteintragId`, dieselbe `karte`, denselben **unveränderten** `beginn` und denselben **Kontributor** wie vor dem Stopp — als ganzer Kontributor (Nummer, Name, Art, `stillgelegtAm`), nicht als nackte Nummer.
- [ ] `ende` ist nach dem Stopp **nicht null** und ein Zeitpunkt in UTC im Format `O`.
- [ ] `GET /api/karten/{karteId}` trägt denselben Eintrag mit `beginn`, `ende` und Kontributor — das ist die tragende Zusage des Slice.
- [ ] Nach einem Neustart der WebApi steht derselbe beendete Eintrag noch da: er liegt in der Datenbank, nicht im Prozessgedächtnis.
- [ ] `GET /api/boards/{boardId}` führt den beendeten Eintrag **nicht** mehr in den laufenden Einträgen; laufen sonst keine, ist die Liste **leer** — kein `null`, kein Fehler.

### Der zweite Stopp verschiebt nichts

- [ ] Ein zweiter `PUT …/ende` auf **denselben** Eintrag antwortet mit **200** — nicht 400, nicht 404, nicht 409.
- [ ] Das `ende` ist danach **identisch** mit dem des ersten Stopps; es wird nie nach hinten geschoben.
- [ ] **Rechenbeispiel:** Eintrag 7, `beginn` 08:04. Erster Stopp um 09:40 → 200, `ende` = 09:40. Zweiter Stopp um 11:15 → 200, `ende` = **09:40**, nicht 11:15. Die gemessene Dauer bleibt 1:36 und wächst nicht auf 3:11.
- [ ] Der Schutz sitzt **im Schreibweg selbst** (`AND Ende IS NULL` im `UPDATE`), nicht nur in einer vorgelagerten Prüfung: ein Aufruf, der die Prüfung überholt, schreibt trotzdem kein zweites Ende.
- [ ] Nach dem zweiten Stopp ist die Zeiteinträge-Liste der Karte um **keinen** Eintrag gewachsen.

### Dauer null ja, negative Dauer nie

- [ ] Start und Stopp im **selben** Zeitpunkt ergeben einen gültigen Eintrag mit `ende` = `beginn`; die Dauer ist **0:00** und der Aufruf wird **nicht** zurückgewiesen.
- [ ] Springt die Uhr zwischen Start und Stopp **zurück**, wird `ende` auf den `beginn` **geklemmt**; es entsteht nie ein `ende` **vor** dem `beginn`.
- [ ] **Rechenbeispiel:** `beginn` 08:04:00, Uhr beim Stopp 08:03:30 → `ende` = **08:04:00**, Dauer 0:00 — nicht 08:03:30 und nicht eine Dauer von minus 30 Sekunden.
- [ ] Die Klemmung ist eine **stille Korrektur**: der Aufruf antwortet mit 200 und ohne Meldung; welches `ende` gilt, sagt der zurückgegebene Eintrag selbst.
- [ ] Es gibt in diesem Slice **keine** Zurückweisung „Das Ende liegt vor dem Beginn" — die gehört `I0025`, wo der Mensch beide Zeitpunkte selbst eingibt und korrigieren kann.

### Nach dem Stopp lässt sich neu starten

- [ ] Nach dem Stopp legt `POST /api/karten/{karteId}/zeiten/laufend` mit **demselben** Kontributor auf **derselben** Karte einen **neuen** Eintrag an: **201**, neue `zeiteintragId`.
- [ ] Der beendete Eintrag bleibt daneben stehen; die Karte trägt danach **zwei** Einträge, einen abgeschlossenen und einen laufenden.
- [ ] Der partielle `UNIQUE`-Index schlägt dabei **nicht** an — er gibt das Paar frei, sobald `Ende` steht.

### Fehlerantworten für Agenten

- [ ] **Unbekannte Karte** → HTTP 404 mit `karte-unbekannt`, Grund **mit der Nummer** und ausführbarer Kompensationsaktion.
- [ ] **Zeiteintrag, den es an dieser Karte nicht gibt** → HTTP 404 mit einem eigenen Code, Grund mit **beiden** Nummern (Karte und Zeiteintrag) und Kompensationsaktion.
- [ ] Der 404 ist **zweistufig**: gibt es schon die Karte nicht, antwortet der Befund über die **Karte** — ein Befund über den Zeiteintrag schickte den Aufrufer auf eine Kartenadresse, die selbst 404 antwortet, und die Kompensation wäre nicht ausführbar.
- [ ] Ein Zeiteintrag, den es zwar gibt, der aber an einer **anderen** Karte liegt, wird wie ein unbekannter behandelt — dieselbe Regel wie bei Anhang und Dateiverweis.
- [ ] Jede Zurückweisung hinterlässt **keine** Änderung; nach einem abgelehnten Aufruf tragen die Zeiteinträge der Karte dieselben Werte wie zuvor.
- [ ] `FehlervertragTests` deckt die neue Route ab; sie steht **nicht** auf `RoutenOhneFehlerantwort`.

### In der Oberfläche

- [ ] Läuft für die gewählte Identität auf dieser Karte ein Timer, steht im Abschnitt „Zeiten" ein Knopf **„Stoppen"** an derselben Stelle, an der zuvor die stille Zeile stand — mit der Angabe, seit wann er läuft.
- [ ] Nach dem Klick steht dort wieder **„Timer starten"** und **kein** Stoppknopf mehr.
- [ ] Die Plakette auf der Karte in der Bahn ist danach **fort** und bleibt es nach dem Reload.
- [ ] Ein anschließender Start auf derselben Karte läuft wieder — der Weg „stoppen, später weiterarbeiten" funktioniert durch die Oberfläche.
- [ ] Der Knopf zeigt **keine gerenderte Dauer** (kein „Stoppen 1:36"): ohne Live-Kanal wäre sie ab der ersten Sekunde falsch.
- [ ] Fällt die WebApi aus, erscheint eine lesbare Meldung statt einer Ausnahmeseite — derselbe Weg wie beim Start.

### Was dieser Slice ausdrücklich nicht tut

- [ ] Es gibt **keine** Zeitensumme und **keine** Einträgeliste in der Oberfläche — das ist `I0026`. Auch nach dem ersten abgeschlossenen Eintrag erscheint keine Summenzeile.
- [ ] **Fremde Timer sind in der Oberfläche nicht beendbar**: an einer fremden Plakette auf der Karte in der Bahn hängt weiterhin **keine** Handlung. Über die API geht es.
- [ ] Es gibt **kein** Nachtragen und **kein** Ändern eines Eintrags von Hand — das ist `I0025`; `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}` (ohne `/ende`) entsteht hier **nicht**.
- [ ] Es gibt **keine** Kopfzeilen-Übersicht der laufenden Timer — das ist `I0027`.
- [ ] Es entsteht **keine** Migration und **keine** Schemaänderung.

### Der grüne Bestand bleibt grün

- [ ] Die Testsuiten aus `R00001`–`R00026` laufen weiter; **eine benannte Ausnahme** (siehe „Änderungen an bestehenden Klassen"): der Routentabellen-Test aus `R00026` erwartet ab hier **zwei** Zeitenrouten statt einer, und sein Name sagt das auch.
- [ ] `ZeitenEndpunkteTests.Wenn_mehrere_Timer_gestartet_und_wiederholt_wurden_dann_traegt_kein_einziger_Eintrag_ein_Ende` bleibt **unangetastet grün** — sie ruft den Stopp nicht auf.
- [ ] `POST /api/karten/{karteId}/zeiten/laufend` antwortet unverändert; Start und Idempotenz des Starts ändern sich nicht.
- [ ] `GET /api/boards/{boardId}` und `GET /api/karten/{karteId}` antworten im Übrigen unverändert.
- [ ] Der zweite Lauf der Migrationen auf einer bestehenden Datei lässt Schema und Daten unverändert; es kommt keine Datei hinzu.

## Betroffene Verzeichnisstruktur

- **Contracts:** **unberührt.** `Zeiteintrag` trägt `Ende` schon; es gibt keinen Rumpf und damit kein neues Anfrage-DTO.
- **Schema:** **unberührt.** Keine neue Datei unter `Source/KanbanC.BL/Persistenz/Migrationen/`.
- **Fachlogik:** `Source/KanbanC.BL/Operations/Zeiten/Zeitmessungsende.cs` (neu) — die eine Stelle, an der aus `Beginn` und Uhr ein `Ende` wird.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Zeiten/ZeitenRepository.cs` und `Source/KanbanC.BL/Interfaces/Zeiten/IZeitenRepository.cs` wachsen je um eine Methode; `Zeitenleser` bleibt unberührt (`LiesZeiteintrag` liest schon beides).
- **Dienste:** `Source/KanbanC.BL/Integrations/Zeiten/ZeitenService.cs` — ein zweites Glied am bestehenden Dienst, **kein neuer Dienst**.
- **Fehler:** `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` — eine Schwester `Zeiteintrag(karteId, zeiteintragId)` mit eigenem Code in `AlleCodes`. **`Stillgelegt` bleibt unberührt.**
- **API:** `Source/KanbanC.WebApi/Endpunkte/ZeitenEndpunkte.cs` — eine zweite Route an derselben Familie; `Program.cs` unberührt, der Dienst ist registriert.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/ZeitenApiKlient.cs` (eine Methode mehr), `Components/Pages/Kartendetail.razor(.css)` (aus `#zeiten-laeuft` wird ein Knopf).
- **Unberührt:** `Karte.razor`, `Spaltenbahnen.razor`, `Board.razor` — die Plakette verschwindet **von selbst**, weil `Zeitenleser.LiesLaufendeZeiteintraegeDesBoards` auf `Ende IS NULL` filtert; `Identitaetswahl.razor`, sämtliche Validatoren (**kein Validator**), `KartenService`, `BoardRepository`, `KartenRepository`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Zeiten/ZeitmessungsendeTests.cs` (neu), `Integrations/Zeiten/ZeitenServiceTests.cs` und `TestHelpers/TestZeitenRepository.cs` wachsen; `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Zeiten/ZeitenRepositoryTests.cs`, `Api/ZeitenEndpunkteTests.cs` und `Api/FehlervertragTests.cs` wachsen; `Source/KanbanC.Blazor.Tests/Services/ZeitenApiKlientTests.cs` wächst; `Source/KanbanC.PlaywrightTests/Tests/TimerStoppenE2ETests.cs` (neu) mit Locatoren in `PageObjects/KartendetailSeite.cs` und `PageObjects/BoardSeite.cs`.

## Technische Überlegungen

### Wer stoppen darf — und warum der Aufruf keinen Kontributor trägt

Das Artboard hat die **erlaubende** Fassung gezeichnet und ihre Begründung gleich mitgeliefert: „Das Stoppquadrat steht auch an **fremden** laufenden Einträgen — Full Trust ohne Anmeldung ist eine Leitplanke der Vision, und ein Agenten-Timer, der über Nacht weiterläuft, muss von jemandem beendet werden können. Der Eintrag behält dabei den Kontributor, für den er läuft; **gestoppt wird der Timer, nicht die Urheberschaft**" (`Dokumentation/Wireframes/D0006.dc.html:315`, Zustand 2 Fassung C).

Die Gegenprobe steht im selben Artboard: an der Karte **in der Bahn** trägt die fremde Plakette ausdrücklich **keine** Handlung — „wer stoppen will, öffnet die Karte" (Zustand 3, `:394`). Das Bild ist also nicht unentschieden, sondern verteilt die Handlung bewusst auf die Eintragszeile.

**Folge für die Adresse:** weil jeder stoppen darf, wäre ein `Kontributor` im Aufruf ein Feld, das **niemand auswertet** (C24, tote Flexibilität) — anders als beim Start, wo er entscheidet, **für wen** gemessen wird. Der Eintrag weiß selbst, wem er gehört.

**Folge für die Stilllegung:** eine Stilllegungsprüfung findet hier **nicht** statt. Wer nicht mehr mitarbeitet, darf keinen Timer mehr *starten* (`Stillgelegt.Zeitmesser`, `R00026`) — aber sein noch laufender muss beendet werden können, sonst liefe er für immer. Der `Zeitenleser` liefert stillgelegte Kontributoren aus demselben Grund ausdrücklich weiter mit.

### Die Adresse

`PUT /api/karten/{karteId:long}/zeiten/{zeiteintragId:long}/ende` — **ohne Rumpf**.

Adressiert wird über die **`ZeiteintragId`** und nicht über das Paar (Karte, Kontributor). Beide wären eindeutig, aber die Nummer steht in jedem `Zeiteintrag`, den Kartenseite und Boardantwort ohnehin schon in der Hand halten, und sie trägt die Entscheidung oben: über das Paar zu adressieren hieße, den Kontributor doch wieder mitzugeben.

Die Adresse folgt **zwei Hausformen zugleich**:

1. **Geschachtelte Nummer für Unterressourcen der Karte** — `PUT /api/karten/{karteId}/teilaufgaben/{teilaufgabeId}`, `DELETE …/anhaenge/{anhangId}`, `DELETE …/dateiverweise/{dateiverweisId}`.
2. **`/ende` als gesetzter Zustand** — wie `/archivierung`, `/stilllegung`, `/lage`, `/kartenzahl`.

Kein Konflikt mit `POST …/zeiten/laufend`: anderes Verb, und der `long`-Constraint trennt `{zeiteintragId:long}` von `laufend`.

**Kein Rumpf**, weil der Aufrufer nichts entscheidet: das `Ende` setzt die Anwendung — aus demselben Grund, aus dem `ZeitmessungStartenAnfrage` keinen `Beginn` trägt. Könnte ein Agent es mitgeben, könnte er die Dauer erfinden.

**Verworfen:** `DELETE …/zeiten/laufend` — es wird nichts gelöscht (der Eintrag bleibt, er bekommt nur ein Ende), und die Adresse brauchte den Kontributor zurück, weil auf einer Karte mehrere Timer laufen dürfen (`R00026`). **Verworfen:** `POST …/zeiten/laufend/stopp` — ein Verb in der Adresse hat dieses Projekt nirgends.

**Freigehalten:** `POST /api/karten/{karteId}/zeiten` bleibt dem **Nachtragen** aus `I0025`, `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}` (ohne `/ende`) dem **Ändern des ganzen Eintrags** aus `I0025`, `GET /api/zeiten/laufend` bleibt `I0027`.

### Der zweite Stopp — und der Vorbehalt, unter dem die Idempotenz steht

Der Stopp eines Eintrags, der nicht mehr läuft, antwortet mit **200** und dem Eintrag, dessen `Ende` **unverändert** stehen bleibt. Die Begründung aus `R00026` trägt: „das Ziel ist erreicht, ein Befund wäre eine Meldung ohne Kompensationsaktion".

**Der Unterschied zum zweiten Start gehört benannt und wird nicht weggewischt:** beim Start verliert der Aufrufer nichts — der laufende Eintrag kommt unverändert zurück. Beim Stopp verlöre er **gemessene Zeit**, wenn ein zweiter Aufruf das Ende nach hinten schöbe. Deshalb wird das Ende nie überschrieben; der Schutz sitzt im `UPDATE` selbst (`AND Ende IS NULL`), nicht nur in einer vorgelagerten Prüfung. Dasselbe Verhältnis wie `ON CONFLICT DO NOTHING` in `SchreibeStilllegung` (`KontributorenRepository.cs:88-101`): „stillgelegt seit" bezeichnet den Beginn und darf durch einen zweiten Klick nicht verschoben werden.

**Der Vorbehalt:** die Begründung „ein Befund hätte keine Kompensationsaktion" trägt nur, **solange `I0025` rot ist**. Heute kann der Aufrufer gegen ein bereits gesetztes Ende nichts tun; ein Befund, der auf einen ungebauten Korrektur-Endpunkt zeigte, wäre eine **Falschauskunft**. Sobald `I0025` steht, gäbe es eine ausführbare Kompensation — und die Frage ist **neu zu stellen**. Sie steht unter „Offene Fragen" mit ihrer Adresse.

**Ein Erfolgsstatus, nicht zwei:** anders als beim Start entsteht hier nichts, 201 wäre falsch. Welches `Ende` gilt, sagt der zurückgegebene Eintrag selbst — ein Agent, dessen Antwort unterwegs verlorenging, wiederholt den Aufruf gefahrlos und liest am `ende` ab, wann tatsächlich gestoppt wurde.

### Dauer null, keine negative Dauer — und die Klemmung als stille Korrektur

Start und Stopp in derselben Sekunde sind eine wahre Aussage über eine sehr kurze Messung. Eine Zurückweisung hätte als Kompensation nur „warte eine Sekunde und wiederhole" und ließe den Timer bis dahin laufen.

Springt die Uhr zurück (NTP-Korrektur; Sommerzeit nicht, gerechnet wird in UTC), wird **`Ende = Beginn`** gesetzt statt einer negativen Dauer. Eine negative Dauer vergiftete jede spätere Summe (`I0026`, `I0033` Soll-Ist, Burndown), und gegen die Uhr des Servers hat der Aufrufer keine Kompensationsaktion.

**Die Klemmung ist eine stille Korrektur — das gehört ausgesprochen:** die Anwendung schreibt 0:00, wo die Uhr eine negative Dauer nahelegte, und sagt es niemandem. Sie ist deshalb an **einer** isoliert prüfbaren Stelle untergebracht (`Zeitmessungsende.Fuer`) statt verstreut im Schreibweg, und sie hat einen eigenen Test, der genau diesen Fall benennt. Wer die gemessene Zeit später anzweifelt, findet die Regel an einer Stelle, nicht in drei Zeilen mitten in einem `UPDATE`.

**Die gezeichnete Zurückweisung „Das Ende liegt vor dem Beginn"** (`D0006.dc.html`, Rand C, `:655`) gehört **`I0025`**: dort gibt der Mensch beide Zeitpunkte selbst ein und **kann** sie korrigieren. Dieselbe Invariante, zwei Antworten — weil nur dort eine Kompensation existiert.

### Die Oberfläche kann hier weniger als die API

Aus der stillen Zeile „läuft seit 08:04" (`Kartendetail.razor:505`, `#zeiten-laeuft`) wird ein Knopf `#timer-stoppen` an derselben Stelle, an der der Startknopf sitzt. Nach dem Stopp steht dort wieder „Timer starten"; die Plakette auf der Karte in der Bahn verschwindet **von selbst**, weil `Zeitenleser.LiesLaufendeZeiteintraegeDesBoards` auf `Ende IS NULL` filtert — gebaut ist daran nichts, bewiesen werden muss es trotzdem.

**Keine gerenderte Dauer im Knopf**, abweichend vom gezeichneten „Stoppen 1:36" (`D0006.dc.html:256ff`, Zustand 2 Fassung A): ohne Live-Kanal (`D0007`, `I0028`) wäre sie ab der ersten Sekunde falsch, und ein Sekundentakt über den Blazor-Kreislauf machte jeden E2E-Lauf wackelig — dieselbe Begründung wie beim Start (`R00026`).

**Fremde Timer sind in der Oberfläche noch nicht beendbar.** Das Bild setzt das Stoppquadrat an die **Eintragszeile**, und die Einträgeliste gehört `I0026`; an der Karte in der Bahn trägt die fremde Plakette ausdrücklich keine Handlung. Über die API geht es.

Das ist **kein Bruch der Kernregel.** „Was die Oberfläche kann, kann die API" ist eine Zusage in **eine** Richtung — die API darf mehr können, die Oberfläche nie mehr als die API. Der umgekehrte Fall wäre der gefährliche: eine UI-Funktion ohne Endpunkt. Derselbe Fall wurde bei `I0022` schon so entschieden. **Das gezeichnete Bild ist damit erst mit `I0026` vollständig** — das ist eine benannte Teillieferung, keine Nachlässigkeit.

### Keine Summe in diesem Slice

Das Artboard zeichnet „Ist 1:36 von 3:00 Soll", aber das Fertig-Kriterium verlangt keine Summe, und `I0026` trägt sie wörtlich: „Die Karte zeigt ihre Zeiteinträge und deren Summe je Kontributor". Das Bild sagt es selbst — „Die **Summen entstehen mit dem ersten Eintrag**" (`D0006.dc.html:292`, Zustand 2 Fassung B). Der erste abgeschlossene Eintrag entsteht **hier**, gezeigt wird er **dort**. Wer nach dem Stopp eine Zahl erwartet, sieht sie erst mit `I0026`.

Das „Soll" aus dem Bild („von 3:00 Soll") hat unter `D0004`/`D0006` überhaupt keinen Knoten — es gehört zur Schätz-Rückkopplung der Vision und braucht einen eigenen Slice.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0006.dc.html`](../Dokumentation/Wireframes/D0006.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gelten daraus **Zustand 2 Fassung A** (`:254`, der Zeitenblock mit laufendem Timer — genau die Form ohne Einträgeliste), **Zustand 2 Fassung C** (`:296`, die Anatomie eines Eintrags samt der Begründung für den fremden Stopp) und **Zustand 3** (`:328`, die Karte in der Bahn, hier als Gegenprobe: keine Handlung an der fremden Plakette). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen **keine** Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Keine gerenderte Dauer im Knopf** („Stoppen" statt „Stoppen 1:36") — Begründung oben.
2. **Keine Einträgeliste und keine Summenzeile**, obwohl Fassung A beides zeigt — `I0026`.
3. **Kein Stoppquadrat an fremden Einträgen in der Oberfläche** — die Zeile, an der es hinge, entsteht erst mit `I0026`; über die API ist der fremde Stopp da.

### Ablauf

1. **Timer stoppen** (`PUT /api/karten/{karteId}/zeiten/{zeiteintragId}/ende`)
   - 1.1 `ZeitenService.BeendeZeitmessung(karteId, zeiteintragId)` — **kein Validator**: eine Nummer hat keinen ungültigen Fall (dieselbe Überlegung wie bei `EntferneDateiverweis`); **kein Kontributor**, **keine Stilllegungsprüfung**
   - 1.2 `ZeitenRepository.BeendeZeitmessung(karteId, zeiteintragId, uhrzeit)`
   - 1.3 Repository liefert `null` → zweistufiger Befund
     - 1.3.1 Gibt es die Karte nicht → `Nichtgefunden.Karte(karteId)` → HTTP 404
     - 1.3.2 Sonst → `Nichtgefunden.Zeiteintrag(karteId, zeiteintragId)` → HTTP 404
   - 1.4 Erfolg → HTTP **200** mit dem Eintrag
2. **Schreiben** (`ZeitenRepository`, **eine** Transaktion)
   - 2.1 Den Eintrag **dieser Karte** lesen — `WHERE ZeiteintragId = @Id AND Karte = @KarteId`; nicht gefunden → `null`
   - 2.2 Läuft er noch (`Ende IS NULL`)?
     - 2.2.1 Ja → `Ende` über `Zeitmessungsende.Fuer(beginn, uhrzeit)` rechnen und schreiben: `UPDATE Zeiteintrag SET Ende = @Ende WHERE ZeiteintragId = @Id AND Ende IS NULL`
     - 2.2.2 Nein → **nichts schreiben**, den Eintrag unverändert zurückgeben
   - 2.3 Lesen, Rechnen und Schreiben stehen in **einer** Transaktion (Muster `B0320`, `B0305`, `B0042`), damit zwischen „läuft der noch?" und „dann schließe ich ihn" kein Fenster bleibt; der `Beginn` wird dabei mitgelesen, weil die Klemmung ihn braucht
   - 2.4 **Die Uhr wird hereingereicht**, nicht im Repository gelesen — sonst wäre das `Ende` im Test nicht setzbar (Muster `StarteZeitmessung`)
   - 2.5 Sobald `Ende` steht, gibt der partielle `UNIQUE`-Index aus `R00026` das Paar (`Karte`, `Kontributor`) wieder frei — das ist die Mechanik hinter „stoppen und neu starten"; sie kostet keine Zeile Code, wohl aber einen Test
3. **Ende rechnen** (`Zeitmessungsende`, pure Operation)
   - 3.1 Uhrzeit ≥ `Beginn` → Uhrzeit
   - 3.2 Uhrzeit < `Beginn` → `Beginn` (Klemmung)
4. **In der Oberfläche**
   - 4.1 Läuft für die gewählte Identität ein Timer → Knopf „Stoppen" statt der stillen Zeile
   - 4.2 Klick → `ZeitenApiKlient.BeendeZeitmessung(karteId, zeiteintragId)`; der zurückkommende Eintrag ersetzt den alten über das vorhandene `MitEintrag`, damit Knopf und Zustand ohne zweiten Abruf nachziehen
   - 4.3 Ausfall der WebApi → `WebApiAufruf.MitAusfallmeldung`, wie beim Start
   - 4.4 Danach zeigt der Block wieder „Timer starten"; die Bahnenplakette fällt beim nächsten Boardabruf von selbst heraus

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`ZeitenEndpunkte`** — die **zweite** Route der Familie; hier wächst der Routentabellen-Test aus `R00026` mit, und `FehlervertragTests` braucht **im selben Commit** einen Vertragsfall.
- **`ZeitenRepository`** — der erste Schreibweg des Projekts, der eine Zeitspalte **füllt** statt sie anzulegen.
- **`Nichtgefunden`** — die eine Stelle für 404-Befunde; sie wächst um eine Schwester statt eines zweiten handgeschriebenen 404-Rumpfs.
- **`Kartendetail.razor`, Zeitenblock** — dieselbe Stelle, an der seit `R00026` „läuft seit" steht.

**Klassen-Entwurf:**

- `Zeitmessungsende` (Operation, pure Logik, `KanbanC.BL/Operations/Zeiten/`) — die eine Stelle, an der aus `Beginn` und Uhr ein `Ende` wird; hier wohnen beide Randfälle (Dauer null, Klemmung).
  - `static DateTimeOffset Fuer(DateTimeOffset beginn, DateTimeOffset uhrzeit)`
- `IZeitenRepository` (Interface, wächst) — eine Methode mehr, damit der Dienst gegen das Test-Repository prüfbar bleibt.
  - `Zeiteintrag? BeendeZeitmessung(long karteId, long zeiteintragId, DateTimeOffset uhrzeit)` — `null` heißt „diesen Zeiteintrag gibt es an dieser Karte nicht"
- `ZeitenRepository` (Provider, wirft, wächst) — liest, rechnet und schreibt in **einer** Transaktion; `AND Ende IS NULL` im `UPDATE` ist der Wiederhol-Schutz im Schreibweg selbst.
- `ZeitenService` (Integration, fängt/loggt, wächst) — **zweites Glied am bestehenden Dienst**, kein neuer; **kein Validator**, **kein Kontributor**, **keine Stilllegungsprüfung**.
  - `Ergebnis<Zeiteintrag> BeendeZeitmessung(long karteId, long zeiteintragId)`
- `Nichtgefunden.Zeiteintrag(long karteId, long zeiteintragId)` (Operation, wächst) — eigener Code in `AlleCodes`, Grund mit **beiden** Nummern, Kompensation über `GET /api/karten/{karteId}`; der zweistufige 404 entsteht im Dienst, Muster `BefundZumFehlendenDateiverweis` (`KartenService.cs:473-484`).
- `ZeitenEndpunkte` (Integration, wächst) — eine zweite Route, **ein** Erfolgsstatus, 404 mit Befund.
- `ZeitenApiKlient` (Integration, wächst, `KanbanC.Blazor/Services/`) — `PutAsync` **ohne Inhalt** statt `PutAsJsonAsync`; die Antwort läuft durch dieselbe Lesehilfe, die 400 und 404 schon in eine `Zurueckweisung` übersetzt.
  - `Task<ApiErgebnis<Zeiteintrag>> BeendeZeitmessung(long karteId, long zeiteintragId)`
- **Kein neues DTO**, **kein Anfrage-Record**, **kein Validator**, **kein zweiter Dienst**, **keine Migration**.

### Änderungen an bestehenden Klassen

- `IZeitenRepository`, `ZeitenRepository`, `TestZeitenRepository` — je eine Methode mehr; das Test-Repository hält denselben Vertrag (kein Schreibzugriff beim zweiten Stopp, `Ende` bleibt stehen).
- `ZeitenService` — ein zweites Glied; `StarteZeitmessung` bleibt unverändert.
- `Nichtgefunden` — eine Schwester und ein Code mehr in `AlleCodes`. **`Stillgelegt` wächst nicht.**
- `ZeitenEndpunkte` — eine Route mehr.
- `ZeitenApiKlient` — eine Methode mehr.
- `Kartendetail.razor(.css)` — aus `<p id="zeiten-laeuft">` wird ein Knopf `#timer-stoppen`; die Startlogik daneben bleibt.
- `KartendetailSeite`, `BoardSeite` (Seitenobjekte) — je ein paar Locatoren mehr.

**Zwei benannte Änderungen an grünem Bestand — beide im selben Commit wie die neue Route:**

1. **`ZeitenEndpunkteTests.Wenn_die_Routen_der_WebApi_gelesen_werden_dann_gibt_es_keine_zweite_Zeitenroute`** nagelt die Routentabelle der laufenden WebApi heute wörtlich auf `["POST /api/karten/{karteId:long}/zeiten/laufend"]` fest (`ZeitenEndpunkteTests.cs:297-305`). Ab dieser Route sind es **zwei**; **Erwartung und Testname wachsen mit**. Das ist eine erwartete Änderung, **kein Befund** — der Test hat genau dafür gestanden: er sollte auffallen, wenn eine Zeitenroute dazukommt.
2. **`FehlervertragTests`** braucht einen Vertragsfall für die neue Route, sonst wird `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` rot (Muster aus den Anhang- und Dateiverweis-Slices).

**Unberührt:** `Zeiteintrag` (Contracts), `Zeitenleser`, `Karte.razor`, `Spaltenbahnen.razor`, `Board.razor`, `Identitaetswahl.razor`, `KartenService`, `BoardRepository`, `KartenRepository`, `Stillgelegt`, sämtliche Validatoren, alle Migrationen.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Zeitmessungsende.Fuer` — zwei Tests, die je eine echte Aussage machen: gleicher Zeitpunkt ergibt Dauer **null**; zurückgesprungene Uhr ergibt `Ende = Beginn` statt einer **negativen** Dauer. Eine spätere Uhrzeit geht unverändert durch.
- `ZeitenService.BeendeZeitmessung` gegen `TestZeitenRepository` — Erfolg reicht den Eintrag unverändert durch; `null` aus dem Repository wird zum **zweistufigen** Befund (Karte unbekannt vs. Zeiteintrag an dieser Karte unbekannt); ein **stillgelegter** Kontributor führt **nicht** zur Zurückweisung; es wird **kein** Kontributor gelesen und keine Stilllegung geprüft.
- `Nichtgefunden.Zeiteintrag` — Code in `AlleCodes`, Grund mit beiden Nummern, ausführbare Kompensation.

**Integration:** `ZeitenRepository` gegen eine `TemporaereDatenbank` — der Stopp setzt `Ende` und lässt `Beginn`, `Karte` und `Kontributor` unverändert; der **zweite** Stopp gibt denselben Eintrag mit **demselben** `Ende` zurück und schreibt nichts; ein Eintrag einer **anderen** Karte liefert `null`; eine unbekannte Nummer liefert `null`; nach dem Stopp legt `StarteZeitmessung` desselben Paares wieder an, **ohne** dass der partielle `UNIQUE`-Index anschlägt (die Mechanik aus `R00026`, hier zum ersten Mal ausgelöst); ein `Ende` wird aus der TEXT-Spalte als **nullable** `DateTimeOffset` korrekt zurückgelesen. `ZeitenEndpunkte` über `TestWebApi` — 200 mit dem beendeten Eintrag, 200 beim zweiten Stopp mit unverändertem `ende`, 404 (unbekannte Karte) und 404 (Zeiteintrag an dieser Karte unbekannt) samt Rumpf; der fremde und der stillgelegte Stopp gehen durch; **hier wird das Fertig-Kriterium bewiesen**: nach dem Stopp trägt `GET /api/karten/{karteId}` den Eintrag mit `Beginn`, `Ende` und Kontributor. `BoardEndpunkteTests` — der beendete Eintrag fällt aus den laufenden des Boards heraus. `FehlervertragTests` ruft die neue Route ab. Der Routentabellen-Test erwartet ab hier **zwei** Zeitenrouten.

**Blazor-Tests (unterhalb E2E):** `ZeitenApiKlient.BeendeZeitmessung` — Erfolg, Zurückweisung mit lesbarem Befund und nicht erreichbare WebApi; über den Browser nicht auslösbar, das ist der Grund, aus dem dieses Testprojekt existiert (CLAUDE.md, Abweichung 4).

**E2E:** Identität wählen, auf der Kartenseite starten, **stoppen**; der Block zeigt wieder „Timer starten", die Plakette auf der Karte in der Bahn ist fort und bleibt es nach dem Reload; ein neuer Start auf derselben Karte läuft danach wieder. Der letzte Schritt ist die sichtbare Probe darauf, dass der partielle `UNIQUE`-Index das Paar mit gesetztem `Ende` wieder freigibt. Der **hinterlassene Eintrag** mit `Beginn`, `Ende` und Kontributor wird an der **API** geprüft und nicht im Browser: die Liste, die ihn zeigte, gehört `I0026`.

Repositories, `Zeitenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00026`** (Timer starten — `I0023`, **grün**). Das ist genau der eine Knoten der WBS-Spalte `Braucht` von `I0024`; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00006`** (Karte — `I0011`), **`R00013`** (Identität wählen — `I0008`), **`R00011`**/**`R00014`** (Kontributor mit Stilllegung), **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (`gestaltung.css`). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **`I0038`** („Board exportieren") nennt `I0024` ausdrücklich in seiner Spalte `Braucht` — ein Export ohne abgeschlossene Zeiteinträge wäre unvollständig. Mittelbar hängen **`I0026`** (Summe je Kontributor, braucht abgeschlossene Einträge) und **`I0033`** (Soll-Ist) daran.

## Umfang

```
Timer stoppen (I0024) = 7 Bubbles: 5 Standard (6,8h), 2 unklar (2,4–5,5h).
Rest: 6,8h klar + 2,4–5,5h unklar · 2 von 7 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 7 Bubbles gruen (0 %) · 0 laufen · 7 offen
```

`I0024` ist vollständig bis zur Bubble geplant und trägt seine sieben Bubbles (`B0330`–`B0336`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: der Slice hat **einen** prüfbaren Aspekt — der Stopp ist erst getan, wenn der Eintrag mit Beginn, Ende und Kontributor zurückzulesen ist; Schreiben und Zurücklesen teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg, dieselbe Lage wie bei `I0023`. **Die Requirement-Klammer sitzt deshalb allein an `I0024`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0330` Ende aus Beginn und Uhr rechnen | Operation | 0,4h (belegt) |
| `B0331` Zeitmessung beenden | Provider | 0,4–1,5h (**unklar**) |
| `B0332` Timerstopp verdrahten | Integration | 0,4h (belegt) |
| `B0333` Endpunkt des Timerstopps | Integration | 2h (Richtwert) |
| `B0334` Stopp im API-Klienten | Integration | 2h (Richtwert) |
| `B0335` Stoppknopf im Zeitenblock | UI | 2h (Richtwert) |
| `B0336` E2E Timer stoppen | E2E | 2–4h (**unklar**) |

Mit sieben Bubbles ist das deutlich weniger als `I0023` (elf) — aus drei Gründen, die alle in der Vorarbeit liegen: die **Tabelle steht** (keine Migration), die **Antwortgestalt steht** (kein Contracts-Wachstum), und die Oberfläche berührt nur **eine** Komponente statt drei (die Bahnenplakette verschwindet von selbst). Die zwei unklaren Bubbles haben zwei verschiedene Ursachen: `B0331` hält Klemmung, Wiederhol-Schutz und Fremdheitsprüfung in **einem** Schreibweg; `B0336` braucht Start, Stopp, Reload und einen erneuten Start in einem Lauf. Derselbe Vermerk wie bei `I0005` bis `I0023`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0024` trägt keine eigene Zählzeile; sie hält die sechs Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der sieben Bubbles gezählt.

## Offene Fragen

- **Bleibt der zweite Stopp idempotent, sobald `I0025` steht?** — **entschieden für jetzt: ja**, mit dem ausdrücklichen **Vorbehalt** aus „Der zweite Stopp". Die Begründung („ein Befund wäre eine Meldung ohne Kompensationsaktion") trägt **nur, solange `I0025` rot ist**: heute kann der Aufrufer gegen ein gesetztes Ende nichts tun, und eine Kompensation, die auf einen ungebauten Korrektur-Endpunkt zeigte, wäre eine Falschauskunft. Sobald ein Eintrag korrigierbar ist, gäbe es eine ausführbare Kompensation — dann ist neu zu entscheiden, ob der zweite Stopp weiterhin 200 antwortet oder einen Befund mit Verweis auf die Korrekturadresse liefert. **Adresse:** `/anforderung aus-slice I0025`. **Nicht am Menschen geprüft.**
- **Darf jeder einen fremden Timer stoppen — ohne Rückfrage?** — **entschieden: ja, ohne Rückfrage.** Beleg ist das Artboard (`D0006.dc.html:315`) und die Full-Trust-Leitplanke der Vision. **Nicht geprüft ist, ob der Mensch beim Stopp eines fremden Timers eine Bestätigung erwartet** („Der Timer von Claude läuft seit 3:12 — wirklich beenden?"). In diesem Slice stellt sich die Frage in der Oberfläche gar nicht, weil dort nur der eigene Timer beendbar ist; sie entsteht mit der Einträgeliste. **Adresse:** `I0026`. **Nicht am Menschen geprüft.**
- **Ist die stille Klemmung richtig, oder soll die Anwendung sie melden?** — **entschieden: still.** Sie schreibt 0:00, wo die Uhr eine negative Dauer nahelegte, und sagt es niemandem. Die Alternative wäre eine Zurückweisung ohne ausführbare Kompensation („die Uhr des Servers ist zurückgesprungen") und ein Timer, der bis zur Klärung weiterläuft. Eine dritte Möglichkeit — 200 mit einem Warnhinweis in der Antwort — existiert im Fehlervertrag dieses Projekts nicht; sie einzuführen wäre eine Entscheidung über den ganzen Vertrag, nicht über diesen Slice. **Nicht am Menschen geprüft.**
- **Ist es hinnehmbar, dass die Oberfläche hier weniger kann als die API?** — **entschieden: ja**, mit derselben Begründung wie bei `I0022`: die Kernregel ist eine Zusage in **eine** Richtung. Das gezeichnete Bild wird damit erst mit `I0026` vollständig. Fiele die Entscheidung anders, käme die Einträgeliste aus `I0026` teilweise in diesen Slice — und der Slice verlöre seine eine prüfbare Aussage. **Nicht am Menschen geprüft.**
- **Trägt die Adresse `/ende`, oder sollte sie anders heißen?** — **entschieden: `/ende`.** Sie folgt zwei Hausformen zugleich und hält `POST …/zeiten` für `I0025` sowie `GET /api/zeiten/laufend` für `I0027` frei. Das Artboard nennt seine Routen ausdrücklich „Entwurf, keine Zusage". **Nicht am Menschen geprüft.**
- **Dürfen sich zwei Zeiteinträge desselben Kontributors zeitlich überlappen?** — **weiterhin offen, hier nicht zu entscheiden und ausdrücklich nicht geraten.** Die Frage steht seit `R00026`; dieser Slice schließt zwar Einträge, beantwortet sie aber nicht: er erzeugt nur den Fall „abgeschlossen und laufend auf verschiedenen Karten", der aus der Mehrfach-Timer-Entscheidung ohnehin folgt. **Adresse:** `I0025` („Zeiteintrag nachtragen und ändern").

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration; `Ende` steht seit `018` in der Tabelle.

## Manuelle Nachbereitungstätigkeiten

- Keine. Bestehende laufende Einträge lassen sich nach dem Deployment beenden — genau dafür ist der Slice da.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine halbe Funktion: seit `R00026` lässt sich ein Timer starten, aber **kein einziger Eintrag trägt ein Ende** — und damit gibt es keine gemessene Dauer, kein Soll-Ist, keinen Burndown, keinen Puffer-Verbrauch und nichts, was ein Export lesen könnte. Dazu ein Betriebsproblem, das die Vision selbst erzeugt: ein Agent ohne Bildschirm lässt einen Timer über Nacht laufen, und niemand kann ihn beenden. Die Kausalkette: **wenn** eine Adresse den laufenden Eintrag schließt, ohne nach dem Kontributor zu fragen, und dabei ein `Ende` schreibt, das nie vor dem `Beginn` liegt und nie nachträglich verschoben wird (X), **dann** existiert zum ersten Mal ein vollständiger Zeiteintrag mit Beginn, Ende und Kontributor — und zugleich gibt der partielle Index das Paar (Karte, Kontributor) wieder frei, sodass „stoppen und später weiterarbeiten" trägt (Y), **und dann** bekommen Summe (`I0026`), Soll-Ist (`I0033`) und Export (`I0038`) ihre Datengrundlage statt einer Spalte voller `NULL` (Z). **Der Hebel liegt beim Stopp und nicht bei der Anzeige der Summe:** eine Summe über offene Einträge ist keine Summe, und eine Zeitenliste, die nur „läuft" zeigt, ist keine Erfassung. Und er liegt beim **Ende**, nicht beim Korrigieren (`I0025`): korrigieren kann man nur, was zuerst entstanden ist — die manuelle Eingabe ist die Ausnahme, der gestoppte Timer der Normalfall.

## Missing-Docs

- **`DateTimeOffset?` als Nullwert in eine SQLite-TEXT-Spalte schreiben und daraus zurücklesen:** dass `DateTimeOffset` nicht materialisiert, ist belegt (`SqliteEigenschaftenTests`); der **nullable** Fall wird hier zum ersten Mal tatsächlich mit einem Wert gefüllt und wieder gelesen. `R00026` hat ihn als ungeprüften Pfad benannt — hier wird er begangen.
- **Freigabe eines partiellen `UNIQUE`-Index nach dem Setzen der Filterspalte:** ob Microsoft.Data.Sqlite in der eingesetzten Fassung das Paar unmittelbar nach dem `UPDATE` innerhalb derselben Transaktion wieder freigibt, ist im Repository unbelegt — `R00026` hat genau diese Frage offen an `I0024` weitergereicht. Der E2E-Test ist die Antwort von außen; ein Repository-Test die von innen.
- **Bedingtes `UPDATE` als Nebenläufigkeitsschutz in SQLite:** dass `AND Ende IS NULL` im `UPDATE` bei zwei gleichzeitigen Aufrufen genau eine Zeile trifft, ist im Repository an keiner Stelle vorgemacht — der Bestand nutzt dafür bisher `ON CONFLICT DO NOTHING` beim `INSERT`.

## Notizen

### Warum der Stopp kein zweites DTO bekommt

Ein `BeendeterZeiteintrag` neben `Zeiteintrag` könnte `Ende` als nicht-nullable führen und hätte damit keinen unmöglichen Zustand. Der Preis wäre eine **zweite Wahrheit über denselben Gegenstand**: derselbe Eintrag hieße je nach Blickwinkel anders, die Kartenseite müsste zwei Listen führen, und `I0025` müsste beim Korrigieren zwischen den Gestalten übersetzen. `Ende is null` heißt „läuft" — ein Feld, eine Regel, fünf Interactions. Dieselbe Entscheidung hat `R00026` bereits gegen ein DTO `LaufenderTimer` getroffen; sie hier umzudrehen hieße, sie an derselben Stelle zweimal verschieden zu beantworten.

### Verworfene Alternativen

- **`DELETE /api/karten/{karteId}/zeiten/laufend`** — es wird nichts gelöscht, und die Adresse brauchte den Kontributor zurück, weil auf einer Karte mehrere Timer laufen dürfen.
- **`POST /api/karten/{karteId}/zeiten/laufend/stopp`** — ein Verb in der Adresse hat dieses Projekt nirgends.
- **Über das Paar (Karte, Kontributor) adressieren** — eindeutig, aber es gäbe den Kontributor in den Aufruf zurück, den die Entscheidung „jeder darf stoppen" gerade herausgenommen hat.
- **Den Stopp nur dem eigenen Kontributor erlauben** — es gibt keinen Login; „eigen" wäre ein Browserzustand, den ein Agent frei setzt. Die Prüfung wäre eine Zusage, die keine ist, und ein über Nacht laufender Agenten-Timer bliebe unbeendbar.
- **Den zweiten Stopp mit 409 zurückweisen** — die Meldung hätte heute keine ausführbare Kompensation; der Aufrufer kann ein gesetztes Ende nicht ändern, solange `I0025` fehlt.
- **Das Ende beim zweiten Stopp überschreiben** — der Aufrufer verlöre gemessene Zeit, und ein wiederholter Aufruf eines Agenten schöbe die Messung stillschweigend nach hinten.
- **Ein `Ende` vor dem `Beginn` zurückweisen** — die Kompensation wäre „stelle die Serveruhr", und der Timer liefe bis dahin weiter; eine negative Dauer vergiftete jede Summe. Die gezeichnete Zurückweisung gehört `I0025`, wo der Mensch die Werte selbst eingibt.
- **Die Klemmung im Repository statt in einer eigenen Operation** — Repositories sind in diesem Projekt IOSP-Integrations und tragen keine Fachlogik; die beiden Randfälle wären dann nur im Schreibweg prüfbar.
- **Das `Ende` vom Aufrufer entgegennehmen** — dann könnte ein Agent die Dauer erfinden; dieselbe Entscheidung wie beim `Beginn`.
- **Die Bahnenplakette explizit ausblenden** — überflüssig: `LiesLaufendeZeiteintraegeDesBoards` filtert auf `Ende IS NULL`, ein beendeter Eintrag fällt von selbst heraus. Gebaut wird nichts, bewiesen wird es trotzdem.
- **Die Summe gleich mitliefern** — kein Fertig-Kriterium verlangt sie, `I0026` trägt sie wörtlich, und das Artboard sagt selbst, dass die Summen mit dem ersten Eintrag entstehen.

### Bewusst out of scope

- **Zeiteintrag nachtragen, ändern, löschen** — `I0025`, samt der Zurückweisung „Das Ende liegt vor dem Beginn" und der Frage nach Überlappungen.
- **Zeitenliste und Summe je Kontributor auf der Karte** — `I0026`; dort entsteht auch das Stoppquadrat an **fremden** Einträgen.
- **Laufende Timer in der Kopfzeile** — `I0027`.
- **Live-Nachführung ohne Reload und gerenderte Dauer** — `I0028`/`D0007`.
- **Das „Soll" aus dem Bild** („von 3:00 Soll") — hat unter `D0004`/`D0006` keinen Knoten.
- **Zugriffsschutz** — die Anwendung läuft im LAN ohne Anmeldung; Leitplanke der Vision, und genau sie trägt „jeder darf stoppen".

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Jeder darf stoppen, auch einen fremden Timer** — der Aufruf bekommt keinen Kontributor mit. Belegt am Artboard (Zustand 2 Fassung C), gegengeprüft an Zustand 3.
2. **Die Adresse ist `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}/ende`, ohne Rumpf.**
3. **Der zweite Stopp ist idempotent (200) und das Ende wird nie verschoben** — **unter Vorbehalt**: die Begründung trägt nur, solange `I0025` rot ist; danach neu zu stellen.
4. **Dauer null ist erlaubt, `Ende` wird bei zurückgesprungener Uhr auf `Beginn` geklemmt** — die Klemmung ist eine **stille Korrektur** und wird als solche benannt.
5. **In der Oberfläche ist nur der eigene Timer beendbar**, obwohl die API jeden beendet — die Oberfläche kann hier bewusst weniger als die API.
6. **Keine Summe in diesem Slice.**
