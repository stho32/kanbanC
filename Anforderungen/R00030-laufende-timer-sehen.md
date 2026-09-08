---
id: R00030
status: In Arbeit
datum: 2026-09-07
---

# R00030: Laufende Timer sehen

## Beschreibung

Die Kopfzeile trägt neben dem Identitätsplatz eine Plakette, die sagt, **wie viele** Timer gerade laufen — über **alle** Boards, nicht nur über das offene. Ein Klick klappt ein Popover auf, das jeden laufenden Eintrag einzeln zeigt: Kontributor, Kartennummer und Titel, seit wann er läuft und auf welchem Board die Karte liegt. Jede Zeile führt zur Karte und lässt sich an Ort und Stelle stoppen — auch die eines fremden Kontributors. Läuft nichts, verschwindet die Plakette ganz.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt" und „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat". `R00026` hat den Timer startbar, `R00027` stoppbar, `R00028` an der Karte sichtbar gemacht; dieser Slice ist der erste, der **über die Karte hinaus** sagt, was gerade läuft.

**Die auffälligste Entscheidung dieses Slice:** **die Plakette zählt und nennt keine Dauer** — „1 läuft", „3 laufen". Damit ist die Schuld eingelöst, die `R00026` ausdrücklich hierher verwiesen hat, und damit weicht die Plakette sichtbar vom Artboard ab, das an dieser Stelle „1:36 · 2 laufen" zeichnet. Die Entscheidung steht unter „Technische Überlegungen → Die Plakette zählt und nennt keine Dauer" mit ihrer Begründung und ist hier zu bestätigen oder zu verwerfen, nicht in der Umsetzung.

**Es entsteht genau eine neue Route.** `GET /api/zeiten/laufend` ist die **erste board- und kartenlose Zeitenroute** dieses Projekts — die Adresse, die `B0324` seit `R00026` für diesen Slice freigehalten hat, wörtlich vermerkt in `ZeitenEndpunkte.cs:11` („GET /api/zeiten/laufend bleibt I0027") und im Routentabellen-Test `ZeitenEndpunkteTests.cs:578`. Kein Schema, keine Migration: Tabelle `Zeiteintrag` und der partielle `UNIQUE`-Index stehen seit `018-zeiteintrag.sql`; dieser Slice **liest nur** und schreibt allein über den bestehenden Stopp aus `R00027`.

## Geschäftlicher Nutzen

Ein Timer, den niemand mehr sieht, läuft weiter. Seit `R00026` darf **ein Kontributor mehrere Timer zugleich** laufen lassen (die Schranke ist das Paar Karte + Kontributor, nicht der Kontributor), und seit `R00027` darf jeder jeden stoppen — aber gefunden werden konnte ein laufender Timer bisher nur, wenn man das richtige Board offen hatte (`Board.LaufendeZeiteintraege`, seit `B0322`) oder die richtige Karte. Wer morgens wissen will, ob über Nacht etwas weiterlief, müsste heute jedes Board einzeln aufsuchen.

Genau das trifft den Fall, für den die Vision Mensch und Agent gleichstellt: **ein Agent misst an mehreren Karten gleichzeitig, und er misst auf mehreren Boards.** Eine Übersicht je Board zeigte nie „alle" — sie zeigte immer nur die eines Ausschnitts. Die Kopfzeile steht auf jeder Seite und ist der einzige Ort in dieser Anwendung, der keinen Ausschnitt kennt.

Dazu ein zweiter, härterer Fall: **ein laufender Timer auf einer archivierten Karte** ist heute unerreichbar. Die Karte steht in keiner Bahn mehr, das Kartendetail müsste man über die Nummer raten. Mit diesem Popover gibt es zum ersten Mal einen Ort, an dem ein solcher Eintrag sichtbar **und** beendbar ist.

Und `D0006` wird mit diesem Slice grün: es ist der letzte offene der fünf Interactions des Dialogs Zeiterfassung.

## Funktionale Anforderungen

- Die Kopfzeile zeigt neben dem Identitätsplatz, **wie viele** Timer gerade laufen — über alle Boards.
- Die Plakette ist gefüllt, sobald einer der laufenden Timer dem gewählten Kontributor gehört; sonst ist sie ruhig.
- Die Plakette nennt **keine Dauer und keine Startzeit**; ihr `title` nennt jeden laufenden Timer einzeln.
- Läuft nichts, steht an der Stelle **nichts** — keine Plakette, kein „0 laufen".
- Ein Klick klappt ein Popover auf, das je laufendem Eintrag eine Zeile zeigt: Initialenkreis, Kartennummer und Titel, „seit hh:mm", darunter Boardname und Kontributor bzw. „für mich".
- Die Zeilen stehen flach und chronologisch: **eigene zuerst**, darin der am längsten laufende oben; danach die fremden in derselben Ordnung.
- Eine Zeile führt auf die Kartenseite `/karten/{KarteId}`.
- Jede Zeile trägt ein Stoppquadrat und beendet **genau diesen** Eintrag — auch den eines fremden Kontributors. **Gestartet wird hier nicht.**
- Timer an archivierten Karten und für stillgelegte Kontributoren werden gezeigt, gekennzeichnet und bleiben stoppbar.
- `GET /api/zeiten/laufend` liefert dieselbe Auskunft an die API — immer `200`, bei keinem laufenden Timer die leere Liste.

## Nicht-funktionale Anforderungen

- **Kein Abfragetakt und keine Uhr:** die Liste wird beim Aufbau des Blazor-Kreislaufs, bei jedem Seitenwechsel, beim Aufklappen des Popovers und nach eigenem Start oder Stopp geholt — sonst nie. Es läuft kein Timer im Server, der die Kopfzeile jeder offenen Sitzung neu rendert.
- **Ein Abruf je Anlass:** Plakette und Popover lesen dieselbe Liste; die Zahl in der Kopfzeile und die Zeilen darunter können nicht auseinanderlaufen.
- **Benutzerfreundlichkeit:** eigen und fremd unterscheiden sich über **Füllung und Wortlaut**, nie allein über die Farbe — Olive und Terrakotta tragen in diesem Canvas die *Art* des Kontributors (`Laufplakette.cs:6`), und „für mich" über den Farbton zu erzählen sagte zugleich etwas Falsches über die Art.
- **Gestaltung:** sämtliche Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in der Komponenten-CSS, kein CSS-Framework. Die Füllung ist der Akzentton, den `.karte-timer-eigen` schon trägt.
- **Die Kopfzeile darf nicht reißen:** ein Ausfall der WebApi läuft über `WebApiAufruf.MitAusfallmeldung` wie das Laden der Kontributoren; die Kopfzeile steht auf jeder Seite.
- **Fehlerantworten für Agenten:** die neue Route weist nichts zurück — es gibt keine Nummer im Aufruf und keinen Bestand, der fehlen könnte.

## Akzeptanzkriterien

Das Fertig-Kriterium des Slice lautet wörtlich: **„Alle gerade laufenden Timer sind mit Karte und Kontributor auf einen Blick sichtbar."**

Das durchgehende Beispiel geht **über zwei Boards**, weil „alle" sonst nicht geprüft ist:

- Board 1 „KanbanC — Release 2", Karte 21, Kartennummer `WBS-21`, Titel „Timer starten und stoppen" — `#8` **Stefan** (Mensch), läuft seit **08:04**.
- Board 2 „Beschaffung", Karte 14, Kartennummer `WBS-14`, Titel „WBS-Import: Markdown-Baum" — `#9` **Claude** (Agent), läuft seit **09:12**.
- Gewählte Identität: **Stefan**.

### Alle gerade laufenden Timer — über alle Boards

- [x] Auf **jeder** Seite der Anwendung (`/boards`, `/boards/1`, `/karten/14`, `/kontributoren`) steht dieselbe Plakette mit demselben Stand.
- [x] Sie zählt **beide** Einträge, obwohl sie auf zwei verschiedenen Boards liegen: `2 laufen` — auch wenn gerade Board 1 offen steht und `Board.LaufendeZeiteintraege` dort nur `#8` kennt.
- [x] Ein dritter Timer auf einem dritten Board macht daraus `3 laufen`.
- [x] Ein **abgeschlossener** Eintrag zählt nie mit: wird `#9` beendet, steht `1 läuft`.

### Die Plakette zählt und nennt keine Dauer

- [x] Die Beschriftung lautet bei genau einem laufenden Timer **`1 läuft`**, sonst **`n laufen`** — `2 laufen`, `3 laufen`.
- [x] Sie enthält **keine Dauer** (`1:36`) und **keine Startzeit** (`seit 08:04`).
- [x] Sie ist **gefüllt**, weil `#8` Stefan gehört und Stefan gewählt ist.
- [x] Wird die Identität auf Claude gewechselt, bleibt sie gefüllt (`#9` gehört dann mir); wird sie auf einen dritten Kontributor gewechselt oder **abgewählt**, wird sie ruhig — die Zahl bleibt `2 laufen`.
- [x] Ohne gewählte Identität ist jeder laufende Timer ein fremder: die Plakette ist ruhig, nie gefüllt.
- [x] Ihr `title` nennt **alle** einzeln, in Beginn-Folge, mit Trenner ` · `: `Stefan seit 08:04 · Claude seit 09:12`.
- [x] Die angezeigte Zahl ändert sich **nicht** dadurch, dass die Seite länger offen steht — sie wird nicht nachgeführt (siehe „Aktualität und ihre Grenze").

### Das Popover zeigt Karte und Kontributor

- [x] Ein Klick auf die Plakette klappt ein Popover mit dem Titel **„Läuft gerade …"** auf; ein zweiter Klick, ein Klick daneben und `Escape` schließen es — dieselbe Mechanik wie die Identitätswahl.
- [x] Höchstens **eines** der beiden Popover ist offen: das Öffnen der Identitätswahl schließt die Laufzeitliste und umgekehrt.
- [x] Es zeigt **zwei** Zeilen. Zeile 1: Initialenkreis `ST`, `WBS-21 Timer starten und stoppen`, `seit 08:04`; darunter `KanbanC — Release 2 · für mich`.
- [ ] Zeile 2: Initialenkreis `KI`, `WBS-14 WBS-Import: Markdown-Baum`, `seit 09:12`; darunter `Beschaffung · Claude`.
- [x] **Stefans Zeile steht oben**, weil eigene Zeilen Vorrang haben — auch dann, wenn Claudes Timer länger liefe (Gegenprobe: Claude seit 07:00, Stefan seit 08:04 → Stefan bleibt oben).
- [x] Unter den eigenen und unter den fremden gilt jeweils **Beginn aufsteigend**: laufen zwei eigene seit 08:04 und 09:30, steht 08:04 oben.
- [x] Die Liste ist **nicht** gruppiert — weder nach Kontributor noch nach Board.
- [x] Eine Zeile trägt **keine Dauer**; die Zeitangabe ist die Startzeit.
- [ ] Der Initialenkreis trägt dasselbe Kürzel und dieselbe Artfarbe wie in der Kommentar- und Zeitenliste der Kartenseite.
- [x] Eine Karte **ohne** Kartenklasse trägt keine Nummer: die Zeile zeigt dann nur den Titel, ohne Lücke und ohne Platzhalter.

### Eine Zeile führt zur Karte

- [x] Ein Klick auf Kartennummer und Titel führt auf `/karten/21` bzw. `/karten/14`.
- [x] Mit dem Seitenwechsel schließt sich das Popover.
- [x] Der Sprung funktioniert auch aus einer Zeile, deren Karte **archiviert** ist — die Kartenseite ist dann der einzige Weg dorthin.

### Aus dem Popover wird gestoppt

- [x] Jede Zeile trägt ein **Stoppquadrat**, auch die fremde (`#9`, Claude) und auch ohne gewählte Identität.
- [x] Ein Klick darauf beendet **genau diesen** Eintrag: die Zeile fällt aus der Liste, die Plakette zeigt danach `1 läuft`.
- [x] Wird auch die letzte Zeile gestoppt, **verschwindet die Plakette** — das Popover schließt mit ihr.
- [x] Der beendete Eintrag trägt weiterhin **seinen** Kontributor: gestoppt wurde der Timer, nicht die Urheberschaft.
- [x] Es gibt im Popover **keinen Startknopf** und keine Möglichkeit, einen Timer anzulegen.
- [x] Ein Stopp aus dem Popover und ein Stopp auf der Kartenseite beenden denselben Eintrag und ergeben denselben Zustand — es entsteht kein zweiter Weg, nur ein zweiter Ort.
- [ ] Nach dem Stopp wird die Liste **frisch geholt**, nicht in der Hand nachgezogen: sie geht über alle Boards und kann sich unterdessen anderswo geändert haben.

### Archivierte Karten und stillgelegte Kontributoren

- [x] Wird Karte 14 archiviert, während `#9` läuft, **bleibt die Zeile stehen** und trägt den Zusatz **„archiviert"**.
- [x] Dasselbe gilt, wenn statt der Karte ihr **Board** archiviert wird — ein Feld, eine Kennzeichnung, weil die Folge dieselbe ist: die Karte steht in keiner Bahn mehr.
- [x] Wird Claude nach dem Start **stillgelegt**, bleibt seine Zeile stehen und trägt den Zusatz **„stillgelegt"**.
- [x] Beide Zeilen bleiben **stoppbar** — das Popover ist dann der einzige Ort, an dem der Eintrag noch erreichbar ist.
- [x] Beide zählen in die Plakette mit: `2 laufen` bleibt `2 laufen`.

### Der Endpunkt `GET /api/zeiten/laufend`

- [x] `GET /api/zeiten/laufend` antwortet mit **200** und einer Liste aus zwei Einträgen.
- [x] Je Eintrag trägt die Antwort den **unveränderten** `zeiteintrag` (mit `zeiteintragId`, `karte`, ganzem `kontributor`, `beginn`, `ende: null`), die ganze `karte`, dazu `board`, `boardname` und `archiviert`.
- [x] `zeiteintrag` hat **dieselbe Gestalt** wie in `GET /api/karten/14` und in `POST …/zeiten/laufend`: es gibt kein zweites Zeiteintrag-DTO.
- [x] Läuft **nichts**, antwortet die Route mit **200 und `[]`** — nie mit 404: es fehlt nichts.
- [x] Die Reihenfolge der API-Antwort ist **Beginn aufsteigend**, `zeiteintragId` als Zweitschlüssel; die Vorrangordnung „eigene zuerst" entsteht in der Oberfläche, weil die API kein „mich" kennt.
- [x] Die Route ist **board- und kartenlos**: sie nennt weder eine `boardId` noch eine `karteId` im Pfad.
- [x] Der Routentabellen-Test führt danach **sechs** Zeitenrouten statt fünf; die fünf bestehenden behalten Pfad und Verb unverändert.
- [x] `GET /api/zeiten/laufend` und `PUT /api/karten/14/zeiten/9/ende` greifen nebeneinander — die neue Route verdeckt keine bestehende.

### Aktualität und ihre Grenze

- [ ] Die Liste wird geholt: beim Aufbau des Blazor-Kreislaufs, bei **jedem** Seitenwechsel, beim **Aufklappen** des Popovers und nach einem eigenen Start oder Stopp auf der Kartenseite.
- [x] Starte ich auf `/karten/21` einen Timer, zeigt die Plakette **ohne Seitenwechsel und ohne Reload** eine um eins höhere Zahl.
- [x] Stoppe ich ihn dort wieder, sinkt sie entsprechend.
- [x] Das aufgeklappte Popover zeigt nie einen alten Stand: das Aufklappen holt.
- [x] **Die benannte Lücke:** startet jemand *anders* einen Timer, während meine Seite offen steht, bleibt meine Zahl bis zum nächsten dieser Anlässe unverändert. Das ist gewollt und wird von `I0028` geschlossen.
- [x] Es entsteht **kein** Abfragetakt, **kein** Hintergrundtimer und **keine** mitlaufende Uhr.

### Was dieser Slice ausdrücklich nicht tut

- [x] Es entsteht **keine** Migration und **keine** Schemaänderung.
- [x] Es entsteht **kein** zweites Zeiteintrag-DTO und **kein** Feld an `Zeiteintrag`, `Karte`, `Board` oder `Kartendetail`.
- [x] Es entsteht **kein** Startknopf außerhalb der Kartenseite.
- [ ] Es entsteht **keine** Live-Nachführung, kein SignalR-Kanal und keine gerenderte Dauer — das ist `I0028`.
- [ ] Es entsteht **keine** Auswertung, keine Summe und kein Soll-Ist — das ist `I0033`.
- [ ] Es entsteht **kein** eigener Schirm und **kein** neuer Navigationspunkt; „Auswertungen" bleibt gesperrt.

### Der grüne Bestand bleibt grün

- [x] Die Suiten aus `R00026`, `R00027`, `R00028` und `R00029` laufen unverändert grün; `#zeitenabschnitt`, `#timer-starten`, `#timer-stoppen` und `#zeiten-laeuft` behalten Kennung und Bedeutung.
- [x] Die Identitätswahl behält `#identitaet`, `#identitaetspopover` und ihr Verhalten; die `R00013`- und `R00005`-Suiten bleiben grün.
- [x] `Laufplakette` an der Karte in der Bahn bleibt unverändert — sie nennt weiterhin eine **Startzeit**, nicht eine Anzahl.
- [x] Die fünf bestehenden Zeitenrouten und ihre Integrationstests bleiben unverändert.
- [x] Build ohne Warnung (`TreatWarningsAsErrors`).

## Betroffene Verzeichnisstruktur

- **Schema:** **unberührt.** Keine neue Datei unter `Source/KanbanC.BL/Persistenz/Migrationen/`; `018-zeiteintrag.sql` trägt Tabelle und partiellen Index bereits.
- **Contracts:** `Source/KanbanC.Contracts/Zeiten/LaufendeZeitmessung.cs` (**neu**) — der Umschlag; `Zeiteintrag.cs` bleibt unverändert.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Zeiten/Zeitenleser.cs` wächst um die **dritte** Leseform `LiesAlleLaufenden`; `Persistenz/Zeiten/ZeitenRepository.cs` um ein lesendes Glied.
- **Verträge der BL:** `Source/KanbanC.BL/Interfaces/Zeiten/IZeitenRepository.cs` wächst um `LiesLaufende`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Zeiten/ZeitenService.cs` wächst um `LiesLaufende` — kein neuer Dienst.
- **API:** `Source/KanbanC.WebApi/Endpunkte/ZeitenEndpunkte.cs` — eine Routenkonstante, ein `MapGet`, ein Handler. `Program.cs` unberührt (`ZeitenEndpunkte.Registriere` steht dort schon).
- **Oberfläche — Klient:** `Source/KanbanC.Blazor/Services/ZeitenApiKlient.cs` wächst um `LadeLaufende` (lesend, **ohne** `ApiErgebnis`).
- **Oberfläche — Formen:** `Source/KanbanC.Blazor/Services/Laufzaehler.cs` (**neu**, neben `Laufplakette`, `Dauerform`, `Zeitbilanz` und `Zeitpunktform` — dort wohnen in diesem Projekt die Formen).
- **Oberfläche — Meldung:** `Source/KanbanC.Blazor/Services/Laufzeitmelder.cs` (**neu**, `AddScoped` in `Program.cs`; Muster `Identitaetsspeicher.Gewechselt`).
- **Oberfläche — Ansicht:** `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor(.css)` — Plakette in Zone 3 **neben** `identitaetsplatz`, Laden und Popoverschaltung; `Components/Layout/Laufzeitpopover.razor(.css)` (**neu**, neben `Identitaetswahl.razor`).
- **Oberfläche — Melder-Aufrufer:** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` — Start und Stopp melden sich; sonst unverändert.
- **Unberührt:** `Karte.razor`, `Spaltenbahnen.razor`, `Board.razor`, `Identitaetswahl.razor`, `Laufplakette.cs`, `wwwroot/gestaltung.css` (die Werte stehen dort).
- **Tests:** `Source/KanbanC.BL.Tests/TestHelpers/TestZeitenRepository.cs` wächst; `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Zeiten/ZeitenRepositoryTests.cs` und `Api/ZeitenEndpunkteTests.cs` wachsen (Routentabelle: sechs statt fünf); `Source/KanbanC.Blazor.Tests/Services/LaufzaehlerTests.cs` (**neu**), `Services/ZeitenApiKlientTests.cs` wächst; `Source/KanbanC.PlaywrightTests/Tests/LaufendeTimerE2ETests.cs` (**neu**) mit Locatoren in `PageObjects/Rahmen.cs`.

## Technische Überlegungen

### Die Plakette zählt und nennt keine Dauer

`R00026` hat diese Frage wörtlich hierher verwiesen: „die Kopfzeilen-Plakette aus `I0027` (‚1:36 · 2 laufen') kann ihre Dauer nicht mehr eindeutig einer eigenen Uhr zuordnen. Das entscheidet `I0027`." Sie wird hier so entschieden: **eine Anzahl, sonst nichts.**

**Warum keine Dauer.** Eine gerenderte Dauer ist ab der ersten Sekunde falsch, solange kein Live-Kanal sie nachführt, und Blazor Server rendert nur auf Anlass. Es ist dieselbe Begründung, mit der `B0326`, `B0328` und `B0335` „läuft seit 08:04" statt „1:36 läuft" gewählt haben und mit der `R00028` die laufenden Einträge **getrennt** nennt statt sie mitzurechnen. Hier wiegt sie **schwerer als dort**: die Kopfzeile steht auf **jeder** Seite. Ein Sekundentakt an dieser Stelle liefe durch den ganzen Blazor-Kreislauf jeder offenen Sitzung und wäre in jedem E2E-Lauf ein Wackelkandidat.

**Warum auch keine Startzeit**, obwohl genau das der Ausweg der Kartenseite ist. Auf einer Karte ist „seit 08:04" eindeutig, weil der partielle `UNIQUE`-Index (`018-zeiteintrag.sql`) höchstens **einen** laufenden Eintrag je Paar (`Karte`, `Kontributor`) zulässt — es *gibt* dort nur einen eigenen. Über alle Boards fällt diese Schranke weg: `R00026` hat entschieden, dass ein Kontributor auf mehreren Karten zugleich messen darf. Die Kopfzeile müsste also **einen von mehreren eigenen** Timern auswählen und ihn als „meinen" ausgeben — und **jede Auswahl wäre eine Zusage, die sie nicht halten kann**. Die Zahl ist die eine Aussage, die in jeder Lage wahr bleibt.

Damit die Plakette nicht verschweigt, was sie zu einer Zahl zusammenzieht, nennt ihr `title` **alle** einzeln mit Kontributor und Startzeit — wörtlich das Muster `Laufplakette.AlsTitel` (`Laufplakette.cs:63`, „Der Titel nennt alle, auch wenn die Plakette nur einen zeigt").

**Abweichung vom Artboard, benannt:** `D0006.dc.html` zeichnet in **Zustand 4** (`:426`) „1:36 · 2 laufen". Übernommen wird die **Füllungsregel** des Bildes (`:478`: „Läuft **mein** Timer, ist sie gefüllt; laufen nur fremde, ist sie ruhig und zählt nur"), fallengelassen wird die Zahl davor — genau die Fassung, die dasselbe Artboard in **Zustand 1** (`:120`) selbst schon zeichnet: „2 laufen", ohne Dauer, weil keiner der beiden Stefan gehört. Das Bild widerspricht sich also nicht, es zeigt beide Fassungen; entschieden ist die knappere. „Wie lange denn" und „welcher denn" beantwortet einen Klick weiter das Popover, wo eine Zeile **ein** Eintrag ist und die Frage wieder eindeutig wird.

### `GET /api/zeiten/laufend` — die erste board- und kartenlose Zeitenroute

Alle fünf bisherigen Zeitenrouten hängen unter `/api/karten/{karteId}`. Diese nicht, und sie kann es nicht: **der Gegenstand hängt an keiner Karte und an keinem Board.** Eine kartengebundene Adresse könnte die Frage „alle" gar nicht stellen. Die Adresse ist seit `B0324` ausdrücklich freigehalten — `ZeitenEndpunkte.cs:11` nennt sie, und `ZeitenEndpunkteTests.cs:578` hält im Kommentar fest, dass sie hier entsteht.

**Immer 200, nie 404.** Läuft nichts, ist die leere Liste die richtige und vollständige Antwort; es fehlt nichts, und ein 404 wäre eine Fehlermeldung ohne Kompensationsaktion — dieselbe Begründung wie in `B0306`. Aus demselben Grund liefert der Dienst **kein** `Ergebnis<T>`: es gibt nichts zurückzuweisen, weil der Aufruf keine Nummer trägt und keinen Bestand voraussetzt. Muster sind die lesenden Glieder in `BoardService`.

**Der Routentabellen-Test wächst mit**, wie schon bei `B0333` und `B0342`: `Zeitenrouten` filtert über `route.Contains("zeiten")` (`ZeitenEndpunkteTests.cs:1107`) und fängt die neue Adresse damit von selbst; die Erwartungsliste geht von fünf auf sechs. Ein Test, der die neue Route *nicht* mitzählt, wäre rot — das ist gewollt.

### Ein Umschlag statt eines zweiten Zeiteintrag-DTOs

```
LaufendeZeitmessung(Zeiteintrag Zeiteintrag, Karte Karte, long Board, string Boardname, bool Archiviert)
```

`Zeiteintrag.cs:7` verbietet ausdrücklich ein zweites DTO: „Ein zweites DTO `LaufenderTimer` daneben wäre eine zweite Wahrheit über denselben Gegenstand." Das Verbot gilt einer **anderen Gestalt desselben Dings** — und genau die entsteht hier nicht: der `Zeiteintrag` reist **unverändert** weiter, der Umschlag legt nur den **Ort** dazu. Das ist wörtlich das Muster `Klassenkarte` („Die Karte bleibt dieselbe Gestalt wie überall; der Umschlag legt nur den Ort dazu", `Kartenleser.cs:163`) und dasselbe Prinzip wie `Kartendetail(Karte, Board, Boardname, …)`.

Die Karte reist als **ganze** `Karte`, wie der Kontributor am `Zeiteintrag` als ganzer Kontributor reist: `Kartennummer` und `Titel` stehen damit ohne zweiten Abruf in der Zeile, und `AlsKarte` (`Kartenleser.cs:150`) wird nicht ein zweites Mal geschrieben.

**Ein** Feld `Archiviert` für Karte **und** Board, nicht zwei. Für den Leser ist die Folge dieselbe (die Karte steht in keiner Bahn mehr) und die Kompensation dieselbe (öffnen und stoppen); zwei Felder trügen eine Unterscheidung, die niemand auswertet (C24).

**Verworfen: nur `KarteId` und `Titel` im Umschlag.** Dann stünde die Kartennummer nicht in der Zeile, obwohl die Karte sie fertig gebildet trägt — und die nächste Ansicht bräuchte ein weiteres Feld.

### Das Popover ist flach, chronologisch und stellt Eigenes voran

Gruppiert wird **weder** nach Kontributor **noch** nach Board — dieselbe Wahl wie bei den Einträgen einer Karte (`R00028`): eine Gruppierung zerrisse die Zeitfolge, und die macht erst lesbar, was seit wann läuft.

Der Vorrang der eigenen Zeile ist der Vorrang, den `Laufplakette` schon kennt („Der eigene Timer hat Vorrang, sonst steht der am längsten laufende", `Laufplakette.cs:21`). Er ist zugleich das Gegenmittel gegen den benannten Preis der Identitätswahl-Variante C (`D0002.dc.html`: „man arbeitet leichter versehentlich als jemand anderes") — meine laufende Zeit steht neben meinem Namen.

**Das Board steht in jeder Zeile**, weil die Liste über alle Boards geht und ein Kartentitel allein nicht sagt, wo die Karte liegt.

**Ein zweites Popover neben der Identitätswahl, kein gemeinsames:** zwei Fragen, zwei Knöpfe. Höchstens eines ist offen — das Öffnen des einen schließt das andere.

### Eine Zeile führt zur Karte

Ziel ist `/karten/{KarteId}`; die Route steht seit `R00017` (`Kartendetail.razor:1`). Ohne den Sprung wäre die Liste eine **Auskunft, aus der man nichts machen kann**: wer sieht, dass ein Timer seit vier Stunden läuft, will die Karte, nicht die Suche danach. Das Zielbild verlangt, „gezielt das richtige Set zu greifen".

### Gestoppt wird hier, gestartet nicht

`R00027` hat entschieden, dass jeder stoppen darf („ein Agenten-Timer, der über Nacht weiterläuft, muss von jemandem beendet werden können"), und `R00028` hat das Stoppquadrat an jede laufende Zeile gesetzt; das Artboard zeichnet es in Zustand 4 an **beiden** Zeilen. Endpunkt (`B0333`) und Klientenglied (`ZeitenApiKlient.BeendeZeitmessung`) sind grün, und die Zeile hält `KarteId` und `ZeiteintragId` schon in der Hand: **es entsteht keine neue Fähigkeit, nur ein zweiter Ort für denselben Aufruf.**

**Gestartet wird hier nicht.** Ein Start gehört auf die Karte (`R00026`); ein Startknopf ohne Karte müsste eine erfinden.

Nach dem Stopp wird die Liste **frisch geholt**, statt sie in der Hand nachzuziehen — sie geht über alle Boards und kann sich unterdessen anderswo geändert haben. Das ist der Unterschied zur Kartenseite, wo `MitEintrag` genügt, weil dort nur eine Karte im Spiel ist.

### Der leere Fall ist Abwesenheit, kein Satz

Läuft nichts, verschwindet die Plakette ganz — kein „0 laufen", keine graue Attrappe. Das Artboard sagt es selbst (`D0006.dc.html`, Zustand 4 rechts): „Die Kopfzeile ist der knappste Platz der Anwendung; ein Element, das nichts zu sagen hat, gibt ihn zurück." Dieselbe Regel, mit der `Laufplakette.Fuer` `null` liefert und die Stelle auf der Karte leer bleibt.

**Anders als bei `R00028`**, wo der Leerfall ein Satz war („Noch keine Zeit erfasst."): dort ist der Block ein reservierter Bereich einer Seite, in dem eine Leerstelle ratlos macht. Hier ist es eine Zeile, die um jeden Pixel konkurriert, und die Abwesenheit ist selbst die Auskunft.

### Archivierte Karten und stillgelegte Kontributoren bleiben sichtbar

`Zeitenleser.cs:14` hält den Grund schon fest: „ein laufender Timer auf einer archivierten Karte ist ein Befund, kein Rauschen", und ein nach dem Start Stillgelegter behält seine Zeit („seine erfasste Zeit bleibt seine").

**Hier wiegt das schwerer als anderswo:** die archivierte Karte steht in keiner Bahn mehr. Das Popover ist dann der **einzige** Ort, an dem der Eintrag noch sichtbar und beendbar ist. Ihn auszublenden hieße, einen laufenden Timer unerreichbar zu machen — genau die Lage, gegen die dieser Slice gebaut wird.

### Aktualität ohne Live-Kanal, und wo sie endet

Geholt wird bei **vier** Anlässen:

1. beim Aufbau des Blazor-Kreislaufs (`OnInitializedAsync`),
2. bei **jedem** `LocationChanged`,
3. beim **Aufklappen** des Popovers,
4. nach einem eigenen Start oder Stopp, über den `Laufzeitmelder`.

Anlass 2 ist der Unterschied zur Kontributorenliste, die die Kopfzeile ausdrücklich **nicht** je Seitenwechsel holt („Die Liste wird je Blazor-Kreislauf einmal geholt", `Kopfzeile.razor:88`). Der Grund: **eine Zahl in der Kopfzeile, die eine halbe Stunde alt ist, lügt** — eine ungeänderte Kontributorenliste veraltet nur. Anlass 3 ist wörtlich das Muster `SchaltePopover` („Wer die Wahl öffnet, will den heutigen Stand sehen"). Anlass 4 ist das Muster `Identitaetsspeicher.Gewechselt`: ein `AddScoped`-Dienst mit Ereignis, in das sich die Kopfzeile in `OnInitializedAsync` einhängt und in `Dispose` wieder aus. Ohne ihn zeigte die Kopfzeile unmittelbar **nach der eigenen Handlung** eine falsche Zahl — und genau dort schaut man hin.

**Kein Abfragetakt und keine Uhr.** Die Lücke, die dadurch bleibt, gehört ausgesprochen: **startet oder stoppt jemand anders einen Timer, während meine Seite offen steht, bleibt meine Zahl bis zum nächsten der vier Anlässe stehen.** Das ist genau die Lücke, die `I0028` (Live-Kanal) schließt, und dieser Slice nimmt sie **nicht** vorweg — auch der `Laufzeitmelder` nicht: seine Meldung bleibt im eigenen Blazor-Kreislauf, ein zweiter Browser und die API erfahren nichts davon. Das Popover selbst zeigt nie einen alten Stand, weil das Aufklappen holt.

**Verworfen: ein Abfragetakt (Polling) alle n Sekunden.** Er wäre eine halbe Live-Nachführung mit den Kosten einer ganzen — eine Anfrage je offener Sitzung und Intervall, für eine Zahl, die sich selten ändert —, und er machte `I0028` schwerer, weil zwei Nachführungswege nebeneinander stünden.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0006.dc.html`](../Dokumentation/Wireframes/D0006.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gelten daraus **Zustand 4** (`:407` — Plakette in Zone 3, das aufgeklappte Popover, der Leerfall und die Begründung gegen die drei verworfenen Orte) und **Zustand 1** (`:120` — die ruhige Fassung der Plakette in der Kopfzeile des Hauptzustands). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen **keine** Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Zwei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Plakette trägt keine Dauer** — begründet oben; das Bild zeigt in Zustand 4 „1:36 · 2 laufen", hier steht „2 laufen". Zustand 1 desselben Artboards zeichnet diese Fassung bereits.
2. **Die Popover-Zeilen tragen keine Dauer** — das Bild zeigt „1:36" und „2:14" in der Zeile und „seit 08:04" erst in der Unterzeile. Hier steht die **Startzeit in der Zeile**, und die Unterzeile trägt Board und Kontributor. Auf einer Zeile *wäre* die Dauer eindeutig — sie wäre nur ab der ersten Sekunde ebenso falsch wie oben.

Übernommen sind ohne Änderung: der Ort (Zone 3, **neben** dem Identitätsplatz, nicht im Ausgabefeld `kopfzeile-bedienung` — das füllt die offene Seite, dieser Slice gilt seitenübergreifend), die Füllungsregel, die Popoverform, der Titel „Läuft gerade …", der Zeilenaufbau, das Stoppquadrat an beiden Zeilen und der leere Fall.

### Ablauf

1. **Kopfzeile baut auf** (`Kopfzeile.OnInitializedAsync`)
   - 1.1 bestehend: Adresse übernehmen, Identität lesen, Kontributoren laden
   - 1.2 `Navigation.LocationChanged`, `Identitaetsspeicher.Gewechselt` **und** `Laufzeitmelder.Gemeldet` abonnieren
   - 1.3 `LadeLaufende()`
2. **`LadeLaufende()`**
   - 2.1 `WebApiAufruf.MitAusfallmeldung(async () => _laufende = await ZeitenApiKlient.LadeLaufende())`
   - 2.2 Ausfall → leere Liste, keine Plakette, kein Ausnahmefall in der Kopfzeile
3. **Plakette rendern**
   - 3.1 `Laufzaehler.Fuer(_laufende, _gewaehlteKontributorId)`
     - 3.1.1 `_laufende.Count == 0` → `null`; die Stelle bleibt leer
     - 3.1.2 Beschriftung: `1 läuft` bzw. `n laufen`
     - 3.1.3 Füllungsklasse: eigen, wenn ein Eintrag dem gewählten Kontributor gehört; ohne gewählte Identität immer fremd
     - 3.1.4 `title`: alle Einträge in Beginn-Folge als `<Name> seit hh:mm`, verbunden mit ` · `
   - 3.2 `null` → nichts rendern; sonst Knopf mit Uhrensymbol, Beschriftung und Chevron, `aria-haspopup` und `aria-expanded` wie `#identitaet`
4. **Popover aufklappen**
   - 4.1 `LadeLaufende()` **vor** dem Öffnen
   - 4.2 die Identitätswahl schließen, falls offen
   - 4.3 Zeilen sortieren: eigene zuerst, je Gruppe `Beginn` aufsteigend
   - 4.4 je Zeile: Initialenkreis, `Kartennummer + Titel` als Verweis auf `/karten/{KarteId}`, `seit hh:mm`, Stoppquadrat; darunter Boardname, Kontributor bzw. „für mich", Zusätze „archiviert" und „stillgelegt"
5. **Stoppquadrat einer Zeile**
   - 5.1 `ZeitenApiKlient.BeendeZeitmessung(zeile.Zeiteintrag.Karte, zeile.Zeiteintrag.ZeiteintragId)`
   - 5.2 `LadeLaufende()` — frisch geholt, nicht nachgezogen
   - 5.3 leere Liste → Popover schließen, Plakette verschwindet
6. **Seitenwechsel** → `LadeLaufende()`, Popover schließen (bestehendes `AufAdresswechsel`)
7. **Eigener Start oder Stopp auf der Kartenseite**
   - 7.1 `Kartendetail.razor` ruft `Laufzeitmelder.Melde()`
   - 7.2 die Kopfzeile holt neu, ohne Seitenwechsel
8. **`Dispose`** → alle drei Ereignisse wieder abmelden

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** `Kopfzeile.razor` Zone 3, **neben** `identitaetsplatz` (`Kopfzeile.razor:41`); `ZeitenEndpunkte.Registriere` (`ZeitenEndpunkte.cs:29`); `Program.cs` der Blazor-App für den `Laufzeitmelder`. Kein neuer Schirm, keine neue Seite, kein neuer Dienst in der BL.

- `LaufendeZeitmessung` (DTO, immutable, `KanbanC.Contracts/Zeiten/`) — ein laufender Zeiteintrag samt seinem Ort: der unveränderte Eintrag, die ganze Karte, ihr Board mit Namen und die Angabe, ob Karte oder Board archiviert sind.
  - `record LaufendeZeitmessung(Zeiteintrag Zeiteintrag, Karte Karte, long Board, string Boardname, bool Archiviert)`
- `Zeitenleser` (bestehend, Ressourcenzugriff) — wächst um die **dritte** Leseform neben `LiesZeiteintraegeDerKarte` und `LiesLaufendeZeiteintraegeDesBoards`: alle offenen Einträge über alle Boards, in Beginn-Folge mit `ZeiteintragId` als Zweitschlüssel. Neu gegenüber der Board-Abfrage sind die JOINs `Spalte → Board` und die beiden LEFT JOINs auf `Kartenarchivierung` und `Boardarchivierung`.
  - `static IReadOnlyList<LaufendeZeitmessung> LiesAlleLaufenden(IDbConnection verbindung, IDbTransaction? transaktion)`
- `IZeitenRepository` / `ZeitenRepository` (bestehend) — ein lesendes Glied mehr.
  - `IReadOnlyList<LaufendeZeitmessung> LiesLaufende()`
- `ZeitenService` (bestehend, Integration) — reicht durch, **ohne** `Ergebnis<T>`: es gibt nichts zurückzuweisen.
  - `IReadOnlyList<LaufendeZeitmessung> LiesLaufende()`
- `ZeitenEndpunkte` (bestehend, Integration) — eine Routenkonstante `"/api/zeiten/laufend"`, ein `MapGet` mit Namen `ZeitmessungenLaufend`, ein Handler, der immer `Results.Ok` liefert.
- `ZeitenApiKlient` (bestehend, Integration) — ein lesendes Glied, **ohne** `ApiErgebnis`; Muster `KontributorenApiKlient.LadeAlle`, der Ausfall läuft über `WebApiAufruf.MitAusfallmeldung`.
  - `Task<IReadOnlyList<LaufendeZeitmessung>> LadeLaufende()`
- `Laufzaehler` (Record, immutable, Oberflächenschicht; Muster `Laufplakette` — Record mit statischem `Fuer`) — was die Kopfzeile über alle laufenden Timer sagt: eine Anzahl, eine Füllung, ein Titel. **Zähler und nicht Plakette im Namen**, weil genau das der Unterschied zur Kartenplakette ist: `Laufplakette` nennt eine Startzeit, `Laufzaehler` eine Anzahl.
  - `static Laufzaehler? Fuer(IReadOnlyList<LaufendeZeitmessung> laufende, long? gewaehlteKontributorId)`
  - `string Beschriftung`, `string Fuellungsklasse`, `string Titel`
- `Laufzeitmelder` (Dienst, `AddScoped`; Muster `Identitaetsspeicher.Gewechselt`) — meldet der Kopfzeile, dass im selben Blazor-Kreislauf ein Timer gestartet oder gestoppt wurde.
  - `event Action? Gemeldet`
  - `void Melde()`
- `Laufzeitpopover` (Razor-Komponente, `Components/Layout/`) — die Zeilen der laufenden Timer; nimmt die Liste und die gewählte `KontributorId` entgegen und meldet einen Stoppwunsch nach oben.
  - `[Parameter] IReadOnlyList<LaufendeZeitmessung> Laufende`
  - `[Parameter] long? GewaehlteKontributorId`
  - `[Parameter] EventCallback<LaufendeZeitmessung> Gestoppt`

**Kein** Interface für `Laufzaehler` oder `Laufzeitmelder`: es gibt je Aufgabe genau eine Implementation (C25). **Keine** neue Klasse in `KanbanC.BL` außer den genannten Gliedern.

### Änderungen an bestehenden Klassen

- `Zeitenleser` — eine Leseform mehr; die bestehenden zwei bleiben unverändert. `AlsZeiteintrag`, `AlsZeitpunkt` und `AlsEndeOderNichts` werden mitgenutzt, nicht kopiert.
- `ZeitenRepository`, `IZeitenRepository`, `ZeitenService` — je ein lesendes Glied mehr; die schreibenden bleiben unberührt.
- `ZeitenEndpunkte` — eine Route mehr; die fünf bestehenden behalten Pfad, Verb und Namen.
- `ZeitenApiKlient` — ein Glied mehr; die fünf bestehenden bleiben unverändert.
- `Kopfzeile.razor(.css)` — Plakette in Zone 3, `_laufende`, `LadeLaufende`, ein zweites Popover und dessen Ausschluss gegen die Identitätswahl; `AufAdresswechsel` holt zusätzlich. Beschriftung, Kennungen und Verhalten der Identitätswahl bleiben unverändert.
- `Kartendetail.razor` — Start und Stopp rufen zusätzlich `Laufzeitmelder.Melde()`; sonst unverändert.
- `Program.cs` (Blazor) — `AddScoped<Laufzeitmelder>()`.
- `TestZeitenRepository` — das neue Glied, damit `ZeitenServiceTests` weiterläuft.
- `Rahmen` (Playwright) — Locatoren für Plakette und Popover neben den bestehenden Identitäts-Locatoren.

**Zur Materialisierung:** Dapper braucht eine flache Zeile, und `Kartenzeile` ist in `Kartenleser` privat. Ob `Zeitenleser` eine eigene flache `Zeitmessungszeile` bekommt oder `Kartenleser` seine Zeile öffnet, entscheidet die Umsetzung — es ist eine Frage der Sichtbarkeit, keine der Fachlichkeit. Fest steht nur: **`AlsKarte` wird nicht ein zweites Mal geschrieben.**

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP), im Projekt `KanbanC.Blazor.Tests`:**
- `Laufzaehler.Fuer` — **die Ränder als eigene Tests, nicht als einer**: leere Liste ergibt `null`; ein Eintrag ergibt `1 läuft`; zwei ergeben `2 laufen`; ein eigener unter fremden ergibt die **gefüllte** Klasse; nur fremde ergeben die ruhige; **ohne gewählte Identität** ist auch der eigene Eintrag ein fremder; der `title` nennt **alle** in Beginn-Folge mit ` · ` verbunden — die Gegenprobe ist ein Titel mit zwei Namen, nicht die Existenz eines Titels.
- Der Beweis ist der **Text und die Klasse**, nicht der Aufruf: jeder Test vergleicht die Beschriftung, nicht das Vorhandensein eines Zählers.

**Integration (`KanbanC.WebApi.IntegrationTests`):**
- `ZeitenRepositoryTests` — `LiesLaufende` gegen eine echte SQLite-Datei: zwei laufende Einträge auf **zwei verschiedenen Boards** kommen beide zurück (das ist der Test, den `Board.LaufendeZeiteintraege` nicht leisten kann); ein abgeschlossener Eintrag kommt **nicht**; eine archivierte Karte kommt **mit** `Archiviert = true`; ein archiviertes **Board** ebenso; ein stillgelegter Kontributor behält seine Zeile mit gesetztem `StillgelegtAm`; Reihenfolge `Beginn`, dann `ZeiteintragId`; Kartennummer und Boardname stehen ohne zweiten Abruf in der Zeile.
- `ZeitenEndpunkteTests` — `GET /api/zeiten/laufend` liefert 200 mit zwei Einträgen; **ohne laufenden Timer 200 mit leerer Liste, nicht 404**; die Antwortgestalt des `zeiteintrag` ist dieselbe wie bei `GET /api/karten/{karteId}`; der **Routentabellen-Test wächst auf sechs Zeitenrouten**; die neue Route und `PUT …/zeiten/{id}/ende` greifen nebeneinander.

**Blazor-Tests (unterhalb E2E, `KanbanC.Blazor.Tests`):** `ZeitenApiKlientTests` — `LadeLaufende` liest die Liste; eine leere Antwort ergibt eine leere Liste; ein `HttpRequestException` läuft bis zum Aufrufer durch (die Ausfallmeldung entsteht in der Kopfzeile, nicht im Klienten). Diese Fehlerpfade sind über den Browser nicht auslösbar — genau der Grund, aus dem es dieses Projekt gibt.

**E2E** (`LaufendeTimerE2ETests`, beide Prozesse auf freien Ports nach Skill `freier-port`): **zwei Boards im Aufbau**, weil „über alle Boards" sonst nicht geprüft ist. Ohne laufenden Timer steht **keine** Plakette; nach dem Start auf Board 1 steht `1 läuft` **gefüllt**; nach einem zweiten Start für einen anderen Kontributor auf **Board 2** steht `2 laufen`; das Popover zeigt beide Zeilen mit Kartennummer, Titel, Board und Kontributor, die eigene oben; ein Klick auf eine Zeile führt auf die Karte; das Stoppquadrat an der **fremden** Zeile beendet sie und die Plakette zeigt `1 läuft`; mit der letzten Zeile verschwindet die Plakette; jeder Stand überlebt den Reload; die Plakette steht auf `/boards`, `/kontributoren` und `/karten/{id}` gleich. Dazu der Melder: ein Start **auf der Kartenseite** hebt die Zahl **ohne Reload und ohne Seitenwechsel**.

Repositories, `Zeitenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00026`** (Timer starten — `I0023`, **grün**). Das ist genau der eine Knoten der WBS-Spalte `Braucht` von `I0027`; er ist erfüllt, der Slice ist **frei**. Ohne laufende Einträge gäbe es nichts zu zählen — und `I0023` ist zugleich der Slice, der die Mehrfachläufigkeit entschieden und die Plakettenfrage hierher verwiesen hat.
- Setzt außerdem auf: **`R00027`** (`I0024` — Stoppendpunkt und Klientenglied, die das Popover verbraucht), **`R00028`** (`I0026` — Muster der laufenden Zeile und des fremden Stopps), **`R00013`** (Identität wählen — ohne sie gibt es kein „mich"), **`R00011`**/**`R00014`** (Kontributor mit Stilllegung), **`R00017`** (Kartenseite `/karten/{KarteId}` als Sprungziel), **`R00023`** (Kartennummer an der Karte), **`R00010`**/**`R00016`** (Board- und Kartenarchivierung), **`R00005`** (`gestaltung.css` und die Kopfzeile in drei Zonen). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **niemanden unmittelbar** — kein Knoten der WBS führt `I0027` in seiner Spalte `Braucht`. Fachlich baut **`I0028`** („Änderung ohne Reload sehen", `D0007`) darauf auf: die Aktualitätslücke dieses Slice ist wörtlich die, die dort geschlossen wird, und die mitlaufende Dauer wird dort zur Ausbaustufe.
- **`D0006` wird mit diesem Slice grün** — `I0023` bis `I0026` sind es bereits.

## Umfang

```
Laufende Timer sehen (I0027) = 11 Bubbles: 6 Standard (8,8h), 5 unklar (3,6–10,0h).
Rest: 8,8h klar + 3,6–10,0h unklar · 0 von 11 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0027` ist vollständig bis zur Bubble geplant und trägt seine elf Bubbles (`B0358`–`B0368`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: der Slice hat **einen** prüfbaren Aspekt — alle laufenden Timer auf einen Blick; Plakette, Popover und Leerfall teilen Datenquelle, Komponente und E2E-Weg. Getrennt geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0020` bis `I0026`. **Die Requirement-Klammer sitzt deshalb allein an `I0027`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0358` Laufende Zeitmessungen über alle Boards lesen | Contracts + Provider | 0,4–1,5h (**unklar**) |
| `B0359` Laufende Zeitmessungen verdrahten | Integration | 0,4h (Richtwert) |
| `B0360` Endpunkt der laufenden Timer | Integration | 2h (Richtwert) |
| `B0361` Laufende Timer im API-Klienten | Integration | 2h (Richtwert) |
| `B0362` Laufzähler der Kopfzeile | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0363` Plakette in Zone 3 der Kopfzeile | UI | 2h (Richtwert) |
| `B0364` Popover der laufenden Timer | UI | 2h (Richtwert) |
| `B0365` Stoppen aus dem Popover | UI | 0,4–1,5h (**unklar**) |
| `B0366` Nachladen bei Seitenwechsel und beim Aufklappen | UI | 0,4–1,5h (**unklar**) |
| `B0367` Eigener Start und Stopp melden sich | UI | 0,4–1,5h (**unklar**) |
| `B0368` E2E Laufende Timer sehen | E2E | 2–4h (**unklar**) |

Mit elf Bubbles liegt der Slice gleichauf mit `I0023` (elf) und über `I0026` (neun) und `I0024` (sieben), aber unter `I0025` (zwölf). Auffällig ist die Verteilung: **vier Bubbles gehen durch den ganzen Stapel** (`B0358` bis `B0361` — Contracts, Provider, Dienst, Endpunkt, Klient) für **eine einzige lesende Fähigkeit**, und **sechs** sitzen in der Oberfläche. Die fünf unklaren Bubbles haben verschiedene Ursachen: `B0358` trägt die längste JOIN-Kette dieses Projekts und die offene Frage, wie die flache Zeile materialisiert wird; `B0365` klärt, ob das Popover nach dem Stopp schließt oder stehenbleibt; `B0366` muss zwei Popover gegeneinander ausschließen, ohne die grüne Identitätswahl anzufassen; `B0367` legt einen zweiten meldenden Dienst neben den `Identitaetsspeicher`; `B0368` braucht zwei Boards, zwei Kontributoren, zwei Starts, einen fremden Stopp, einen Sprung und Reload in einem Lauf. Derselbe Vermerk wie bei `I0005` bis `I0026`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0027` trägt keine eigene Zählzeile; sie hält die acht Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der elf Bubbles gezählt.

## Offene Fragen

- **Nennt die Kopfzeilen-Plakette eine Dauer?** — **entschieden: nein, sie zählt.** „1 läuft", „3 laufen", über alle Boards, gefüllt sobald einer davon meiner ist. Begründung: eine gerenderte Dauer ist ohne Live-Kanal ab der ersten Sekunde falsch (dieselbe Entscheidung wie `B0326`, `B0328`, `B0335` und `R00028`), und die Kopfzeile steht auf **jeder** Seite. **Auch keine Startzeit**, obwohl das der Ausweg der Karte ist: dort hält der partielle `UNIQUE`-Index höchstens einen eigenen laufenden Eintrag je Karte, über alle Boards fällt diese Schranke weg — die Kopfzeile müsste einen von mehreren eigenen auswählen, und jede Auswahl wäre eine Zusage, die sie nicht halten kann. **Die Umkehrung** wäre eine Zahl, die stimmt, solange man nicht hinsieht, plus ein Sekundentakt durch jeden offenen Blazor-Kreislauf. **Sichtbare Abweichung vom Artboard** (Zustand 4: „1:36 · 2 laufen"; Zustand 1 desselben Bildes zeichnet dagegen „2 laufen"). **Mit `I0028` fällt der Grund weg** — die mitlaufende Dauer ist dann eine Ausbaustufe und wird **dort** geplant. **Nicht am Menschen geprüft.**
- **Zeigen die Popover-Zeilen eine Dauer?** — **entschieden: nein, die Startzeit.** Auf einer Zeile wäre die Dauer eindeutig, aber ebenso ab der ersten Sekunde falsch. **Zweite sichtbare Abweichung vom Artboard**, das „1:36" und „2:14" in der Zeile zeichnet. **Nicht am Menschen geprüft.**
- **Bekommt die Route eine eigene Adresse ohne Karte?** — **entschieden: ja, `GET /api/zeiten/laufend`.** Der Gegenstand hängt an keiner Karte und an keinem Board; eine kartengebundene Adresse könnte die Frage „alle" nicht stellen. **Immer 200, nie 404.** Die Adresse ist seit `B0324` genau dafür freigehalten. **Die Umkehrung** wäre eine Sammlung von Board-Abrufen in der Oberfläche — n Anfragen für eine Auskunft und eine Zahl, die zwischen zwei Antworten auseinanderläuft. **Nicht am Menschen geprüft.**
- **Entsteht ein zweites Zeiteintrag-DTO?** — **entschieden: nein, ein Umschlag.** `LaufendeZeitmessung` legt Karte, Board und Archivstand um den **unveränderten** `Zeiteintrag`; Muster ist wörtlich `Klassenkarte`. **Ein** Feld `Archiviert` für Karte und Board (C24). **Die Umkehrung** verstieße gegen das Verbot in `Zeiteintrag.cs` und zwänge das Stoppen, zwischen zwei Gestalten zu übersetzen. **Nicht am Menschen geprüft.**
- **Wird das Popover gruppiert?** — **entschieden: nein, flach und chronologisch, eigene zuerst.** Dieselbe Wahl wie in `R00028`; der Vorrang des Eigenen ist der von `Laufplakette`. **Nicht am Menschen geprüft.**
- **Lässt sich aus dem Popover stoppen — auch fremd?** — **entschieden: ja, an jeder Zeile.** Es entsteht keine neue Fähigkeit; Endpunkt und Klientenglied sind grün, und `R00027`/`R00028` haben den fremden Stopp schon entschieden. **Ohne Bestätigungsfrage**, aus demselben Grund wie dort: Full Trust ohne Anmeldung ist eine Leitplanke der Vision, und ein Rückfragedialog existiert an keiner Handlung dieses Projekts. **Gestartet wird hier nicht.** **Nicht am Menschen geprüft.**
- **Was steht da, wenn nichts läuft?** — **entschieden: nichts.** Die Plakette verschwindet ganz, kein „0 laufen". Beleg: `D0006.dc.html`, Zustand 4 rechts. Anders als bei `R00028`, wo der Leerfall ein Satz ist: hier ist er Abwesenheit. **Nicht am Menschen geprüft.**
- **Werden archivierte Karten und stillgelegte Kontributoren gezeigt?** — **entschieden: ja, gekennzeichnet und stoppbar.** `Zeitenleser` hält die Begründung schon fest; hier wiegt sie schwerer, weil das Popover für eine archivierte Karte der **einzige** erreichbare Ort ist. **Die Umkehrung** machte einen laufenden Timer unauffindbar. **Nicht am Menschen geprüft.**
- **Wie aktuell ist die Zahl?** — **entschieden: vier Anlässe, kein Takt.** Kreislaufaufbau, jeder Seitenwechsel, das Aufklappen des Popovers, eigener Start/Stopp über den `Laufzeitmelder`. **Die Lücke ist benannt und bleibt:** eine lange offene Seite zeigt eine Zahl, die veralten kann, wenn jemand *anders* startet oder stoppt. Sie wird von `I0028` geschlossen und hier **nicht** vorweggenommen. **Die Umkehrung** — ein Abfragetakt — wäre eine halbe Live-Nachführung mit den Kosten einer ganzen. **Nicht am Menschen geprüft.**
- **Wo wachsen die E2E-Locatoren?** — **entschieden: in `Rahmen`, nicht in einem neuen `Kopfzeilenseite`-Objekt.** Die Bubble `B0368` schlägt ein eigenes Objekt vor; `PageObjects/Rahmen.cs` führt aber bereits `#kopfzeile`, `#identitaet` und `#identitaetspopover` — ein zweites Objekt über dieselbe Kopfzeile wäre eine zweite Adresse für denselben Ort. Bubbles sind Entwurf, keine Vereinbarung; die Abweichung ist hier benannt statt stillschweigend genommen. **Nicht am Menschen geprüft.**
- **Wie wird die flache Zeile materialisiert?** — **offen und bewusst nicht entschieden.** Dapper braucht eine flache Zeile, `Kartenzeile` ist in `Kartenleser` privat. Ob `Zeitenleser` eine eigene `Zeitmessungszeile` bekommt oder `Kartenleser` seine Zeile öffnet, ist eine Frage der Sichtbarkeit und gehört in die Umsetzung. Fest steht: `AlsKarte` wird nicht zweimal geschrieben.

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration und keine Konfiguration.

## Manuelle Nachbereitungstätigkeiten

- Keine. Bereits laufende Zeiteinträge erscheinen nach dem Deployment ohne Zutun in der Plakette.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist ein Timer, der weiterläuft, weil ihn niemand findet: seit `R00026` darf ein Kontributor **auf mehreren Karten zugleich** messen, seit `R00027` darf jeder jeden stoppen — aber gesucht werden konnte ein laufender Eintrag bisher nur je Board (`Board.LaufendeZeiteintraege`) oder je Karte, und auf einer archivierten Karte gar nicht mehr. Die Kausalkette: **wenn** eine board- und kartenlose Leseform alle offenen Einträge samt ihrem Ort liefert und die Kopfzeile — der einzige Ort der Anwendung ohne Ausschnitt — sie als Zahl und als Liste zeigt (X), **dann** ist zum ersten Mal beantwortbar, was gerade gemessen wird, und jeder gefundene Eintrag ist im selben Zug erreichbar und beendbar (Y), **und dann** wird aus einer Erfassung, die stillschweigend über Nacht weiterzählt, eine, die sich selbst meldet — und die Ist-Zeiten, aus denen `I0033` und `I0036` rechnen, bleiben belastbar (Z). **Der Hebel liegt bei der Übersicht und nicht bei einer Warnung oder einem automatischen Ende:** ein Timer, der von selbst stoppt, erfände eine Arbeitszeit, und eine Warnung ohne Ort, an dem man handeln kann, wäre eine Meldung ohne Kompensationsaktion. Und er liegt **vor** `I0028`: die Live-Nachführung macht eine Übersicht aktueller, aber sie ersetzt sie nicht — eine Kopfzeile ohne Plakette hätte nichts, was sich nachführen ließe.

## Missing-Docs

- **Zwei gleichzeitig mögliche Popover in einer Blazor-Komponente:** die Kopfzeile bekommt neben der Identitätswahl ein zweites Popover mit eigener Auffangfläche und eigenem `Escape`-Weg. Wie sich zwei solche Overlays sauber gegeneinander ausschließen (eine Zustandsvariable mit drei Werten statt zweier Booleans, ein gemeinsamer `@onkeydown`), ist im Repository nicht vorgemacht — `Kopfzeile.razor` kennt heute genau eines.
- **`AddScoped`-Dienste als Ereignisquelle im Blazor-Kreislauf:** `Identitaetsspeicher.Gewechselt` ist das einzige Vorbild, und es ist an einen Browser-Speicher gekoppelt. Ein reiner Melder ohne Zustand (`Laufzeitmelder`) ist die Zweitverwendung des Musters; ob `Dispose`-Disziplin und `InvokeAsync(StateHasChanged)` dabei ausreichen oder ob es einen Abmeldeweg für abgerissene Kreisläufe braucht, ist nicht belegt.
- **Vier LEFT JOINs in einer Dapper-Abfrage mit geschachteltem DTO-Aufbau:** die JOIN-Kette `Zeiteintrag → Karte → Spalte → Board → Kontributor` plus `Kontributorstilllegung`, `Kartenarchivierung`, `Boardarchivierung` ist die längste dieses Projekts. Wie die flache Zeile am günstigsten geschnitten wird, damit sowohl `AlsKarte` als auch `AlsZeiteintrag` sie füttern können, hat im Repository kein Vorbild.

## Notizen

### Verworfene Alternativen

- **„1:36 · 2 laufen" wie im Artboard** — die Dauer wäre ohne Live-Kanal ab der ersten Sekunde falsch, und über alle Boards ließe sich nicht einmal sagen, *welche* eigene Uhr sie meint.
- **„seit 08:04" in der Plakette** — auf der Karte eindeutig, hier nicht: über alle Boards kann ein Kontributor mehrere eigene laufende Einträge haben, und die Auswahl eines davon wäre eine Zusage, die die Plakette nicht halten kann.
- **Eine Übersicht je Board (Banner oder Zone im Board)** — zeigte die Timer nur, solange man auf dem richtigen Board steht; „alle auf einen Blick" wäre nicht erfüllt. Ein Timer hängt an einer Karte, nicht an dem Board, das gerade offen ist.
- **Ein eigener Schirm mit Navigationspunkt** — die WBS führt unter `D0006` keinen; ein neuer Punkt wäre eine erfundene Interaction, und „auf einen Blick" verträgt keinen Platz, den man erst aufsuchen muss.
- **Die Plakette in das Ausgabefeld `kopfzeile-bedienung`** — das füllt die offene Seite (`Board.razor`); dieser Slice gilt seitenübergreifend und stünde dort nur, solange gerade ein Board offen ist.
- **Die laufenden Einträge aller Boards aus n Board-Abrufen zusammensetzen** — n Anfragen für eine Auskunft, und zwischen der ersten und der letzten Antwort läuft die Zahl auseinander.
- **Ein zweites DTO `LaufenderTimer`** — verboten in `Zeiteintrag.cs`, und das Stoppen müsste zwischen zwei Gestalten desselben Dings übersetzen.
- **Zwei Felder `KarteArchiviert` und `BoardArchiviert`** — eine Unterscheidung, die niemand auswertet: die Folge ist dieselbe, die Kompensation ist dieselbe (C24).
- **Nur `KarteId` und `Titel` im Umschlag statt der ganzen Karte** — die Kartennummer stünde dann nicht in der Zeile, obwohl die Karte sie fertig gebildet trägt.
- **Das Popover nach Board oder nach Kontributor gruppieren** — zerrisse die Zeitfolge, an der ablesbar ist, was seit wann läuft; das Board steht ohnehin in jeder Zeile.
- **Ein gemeinsames Popover mit der Identitätswahl** — zwei Fragen, zwei Knöpfe; ein Popover, das beides kann, hätte keinen Titel, der stimmt.
- **Ein Abfragetakt (Polling)** — eine halbe Live-Nachführung mit den Kosten einer ganzen, und zwei Nachführungswege nebeneinander, sobald `I0028` kommt.
- **Ein Startknopf im Popover** — ein Start braucht eine Karte; ohne sie müsste er eine erfinden.
- **Archivierte und stillgelegte Zeilen ausblenden** — machte einen laufenden Timer unauffindbar und unbeendbar; das Popover ist für eine archivierte Karte der einzige Ort.
- **Ein „0 laufen" oder eine graue Attrappe im Leerfall** — die Kopfzeile ist der knappste Platz der Anwendung; ein Element, das nichts zu sagen hat, gibt ihn zurück.
- **Eine Bestätigungsfrage vor dem fremden Stopp** — es gibt in diesem Projekt an keiner Handlung einen Rückfragedialog, und Full Trust ohne Anmeldung ist eine Leitplanke der Vision.
- **Ein eigenes `Kopfzeilenseite`-Objekt für die E2E-Locatoren** — `Rahmen` ist bereits das Seitenobjekt der Kopfzeile; ein zweites wäre eine zweite Adresse für denselben Ort.

### Bewusst out of scope

- **Live-Nachführung ohne Reload, die mitlaufende Dauer und ein Abfragetakt** — `I0028`/`D0007`.
- **Einen Timer aus dem Popover starten** — `I0023`, und er braucht eine Karte.
- **Zeiteintrag nachtragen, ändern, löschen** — `I0025`, auf der Kartenseite.
- **Summen, Soll-Ist, Burndown** — `I0033`.
- **Zeiten exportieren** — `I0036`.
- **Ein Filter über Kontributor oder Board im Popover** — die Liste ist kurz, solange sie kurz ist; ein Filter für zwei Zeilen wäre Bedienung ohne Anlass.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Die Plakette zählt und nennt weder Dauer noch Startzeit** — **sichtbare Abweichung vom Artboard, Zustand 4.**
2. **Die Popover-Zeilen nennen die Startzeit statt einer Dauer** — **zweite sichtbare Abweichung vom Artboard.**
3. **`GET /api/zeiten/laufend`** ist board- und kartenlos und antwortet **immer 200**, leer statt 404.
4. **`LaufendeZeitmessung`** ist ein Umschlag um den unveränderten `Zeiteintrag`, mit **einem** Feld `Archiviert` für Karte und Board.
5. **Das Popover ist flach und chronologisch, eigene zuerst**, darin der am längsten laufende oben.
6. **Jede Zeile führt zur Karte und lässt sich stoppen, auch die fremde** — ohne Bestätigungsfrage; gestartet wird hier nicht.
7. **Der Leerfall ist Abwesenheit** — die Plakette verschwindet ganz.
8. **Archivierte Karten und stillgelegte Kontributoren werden gezeigt, gekennzeichnet und bleiben stoppbar.**
9. **Vier Ladeanlässe, kein Takt** — die Aktualitätslücke bis `I0028` ist benannt und bleibt bestehen.
10. **Die E2E-Locatoren wachsen in `Rahmen`**, nicht in einem neuen Seitenobjekt — Abweichung von der Bubble-Notiz `B0368`.
