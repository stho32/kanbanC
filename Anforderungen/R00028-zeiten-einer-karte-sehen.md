---
id: R00028
status: In Arbeit
datum: 2026-09-06
---

# R00028: Zeiten einer Karte sehen

## Beschreibung

Die Kartenseite zeigt im Abschnitt „Zeiten" alle Zeiteinträge dieser Karte — je Eintrag Kontributor, Zeitraum und Dauer, neueste zuerst — und darüber die **Summe je Kontributor** samt der Gesamtsumme neben dem Timerknopf. Ein laufender Eintrag steht dabei ohne Dauer in der Liste und wird in der Bilanz getrennt gezählt. Eine Karte ohne Eintrag sagt „Noch keine Zeit erfasst." und sonst nichts.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt" und „Zeiten je Aufgabe und Kontributor". `R00026` hat den Timer startbar, `R00027` stoppbar gemacht; dieser Slice ist der erste, der die gemessene Zeit **zeigt**.

**Dieser Slice liegt ganz in der Oberfläche — serverseitig fehlt nichts.** Nachgesehen, nicht vermutet: `Zeitenleser.LiesZeiteintraegeDerKarte` (`Source/KanbanC.BL/Persistenz/Zeiten/Zeitenleser.cs:25`) liest **alle** Einträge der Karte ohne Ende-Filter, mit ganzem Kontributor, `ORDER BY z.Beginn, z.ZeiteintragId`; `Kartendetail` trägt sie seit `B0321` als siebte Liste `Zeiteintraege`. Es entsteht **keine neue Route**, kein Endpunkt, kein Klientenglied, keine Migration und kein Contracts-Feld.

**Die auffälligste Entscheidung dieses Slice:** **ein laufender Eintrag zählt nicht in die Summe.** Sie steht unter „Technische Überlegungen → Der laufende Eintrag zählt nicht" mit ihrer Begründung, weicht sichtbar vom Artboard ab und ist hier zu bestätigen oder zu verwerfen, nicht in der Umsetzung.

## Geschäftlicher Nutzen

Seit `R00027` entstehen abgeschlossene Zeiteinträge — **gelesen hat sie bisher niemand.** Wer wissen will, wie viel Zeit eine Karte gekostet hat, muss heute `GET /api/karten/{karteId}` aufrufen und die Spannen von Hand addieren; im Browser ist die gemessene Zeit unsichtbar, sobald der Timer steht. Genau die Rückkopplung, für die die Vision die Ist-Zeiten erfasst („dieselben Ist-Zeiten als Futter für die KI, die daraus künftige Aufgaben besser einschätzt"), beginnt damit, dass ein Mensch die Zahl an der Karte sieht, an der er gerade arbeitet.

Dazu zwei konkrete Schulden, die dieser Slice einlöst: `I0025` („Zeiteintrag nachtragen und ändern") **wartet auf diese Liste** — sein Änderungsformular klappt in einer ihrer Zeilen auf, und ohne Zeile gibt es nichts zu ändern; die WBS führt `I0026` deshalb in der Spalte `Braucht` von `I0025`. Und `R00027` hat ausdrücklich hierher verwiesen: fremde Timer sind in der Oberfläche bis heute nicht beendbar, weil das Stoppquadrat an einer Eintragszeile hängt, die es nicht gab.

## Funktionale Anforderungen

- Die Kartenseite listet alle Zeiteinträge der Karte mit Kontributor, Zeitraum und Dauer.
- Die Liste steht flach und chronologisch, **neueste zuerst**, und wird nicht nach Kontributor gruppiert.
- Über der Liste steht je Kontributor eine Zeile mit seiner Summe; laufende Einträge werden dort **getrennt** genannt.
- Die Gesamtsumme steht als „Ist h:mm" neben dem Timerknopf.
- Summiert werden ausschließlich **abgeschlossene** Einträge; ein laufender Eintrag trägt in der Liste keine Dauer.
- Ein laufender Eintrag ist ohne Farbvergleich als laufend erkennbar und trägt ein **Stoppquadrat** — auch der eines fremden Kontributors.
- Eine Karte ohne Zeiteintrag zeigt den Satz „Noch keine Zeit erfasst." statt Summenzeilen, Liste und Kopfzahl; der Startknopf bleibt stehen.
- Ein stillgelegter Kontributor behält seine Zeilen und seine Summe.

## Nicht-funktionale Anforderungen

- **Kein zweiter Abruf:** Liste, Summen und Gesamtsumme entstehen aus `Kartendetail.Zeiteintraege`, das die Seite ohnehin lädt. Es gibt keine zusätzliche HTTP-Anfrage und damit kein Fenster, in dem zwei Antworten auseinanderlaufen.
- **Eine Rechenstelle:** Kontributorensummen und Gesamtsumme kommen aus **einem** Aufruf; dieselbe Zahl entsteht nirgends ein zweites Mal.
- **Benutzerfreundlichkeit:** laufend und abgeschlossen unterscheiden sich an mehr als der Farbe (Kante, Wortlaut, Handlung) — die Anzeige bleibt ohne Farbwahrnehmung lesbar.
- **Gestaltung:** sämtliche Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in der Komponenten-CSS, kein CSS-Framework.
- **Keine mitlaufende Uhr:** die Anzeige rendert keine Dauer, die ab der ersten Sekunde falsch wäre; „jetzt" ist überall ein Parameter, keine Uhr im Inneren einer Form.

## Akzeptanzkriterien

Das Fertig-Kriterium des Slice lautet wörtlich: **„Die Karte zeigt ihre Zeiteinträge und deren Summe je Kontributor."**

Das durchgehende Rechenbeispiel: Karte 14 trägt drei Einträge — `#7` Claude (Agent), **heute 09:12, ohne Ende**; `#6` Stefan (Mensch), gestern 17:40 – 18:25; `#5` Claude, gestern 11:05 – 11:42.

### Die Karte zeigt ihre Zeiteinträge

- [x] Auf `/karten/14` steht im Abschnitt „Zeiten" eine Liste mit **drei** Zeilen; die Kopfzahl daneben zeigt `3`.
- [x] Die Reihenfolge ist **neueste zuerst**: `#7`, `#6`, `#5` — nicht die Beginn-aufsteigende Folge, in der die API sie liefert.
- [x] Jede Zeile trägt den Initialenkreis ihres Kontributors (dasselbe Kürzel und dieselbe Artfarbe wie in der Kommentarliste), den Zeitraum und — wenn abgeschlossen — die Dauer.
- [x] Zeile `#6` liest sich `gestern 17:40 – 18:25` mit der Dauer `0:45`; Zeile `#5` liest sich `gestern 11:05 – 11:42` mit `0:37`.
- [x] Die Liste ist **nicht** nach Kontributor gruppiert: `#7` (Claude) steht über `#6` (Stefan), obwohl `#5` demselben Kontributor gehört wie `#7`.
- [x] Ein Reload der Seite zeigt dieselbe Liste in derselben Reihenfolge.

### Und deren Summe je Kontributor

- [x] Über der Liste steht **je Kontributor mit abgeschlossener Zeit** eine Zeile mit Initialenkreis, Name und Summe.
- [x] Im Beispiel: Stefan `0:45` und Claude `0:37` — Claude **nicht** `2:51`, weil `#7` noch läuft.
- [x] Die Reihenfolge ist absteigend nach Summe, bei Gleichstand nach Name: Stefan (`0:45`) steht über Claude (`0:37`).
- [x] Neben dem Timerknopf steht die Gesamtsumme als **`Ist 1:22`** (0:45 + 0:37) — nicht `3:36`.
- [x] Ein **stillgelegter** Kontributor bekommt seine Summenzeile wie jeder andere; seine erfasste Zeit verschwindet nicht mit seiner Stilllegung.
- [x] Es steht **kein „von h:mm Soll"** neben der Ist-Summe.

### Laufend und abgeschlossen sind unterscheidbar

- [x] Zeile `#7` liest sich `heute 09:12 – läuft` und trägt **keine** Dauer.
- [x] Sie unterscheidet sich von den abgeschlossenen an mindestens drei Merkmalen, die nicht nur Farbe sind: Akzentkante links, das Wort „läuft" an der Stelle des Endes, und ein **Stoppquadrat** als Handlung rechts.
- [ ] Eine abgeschlossene Zeile trägt **keine** Handlung — kein Stift, kein Löschen, kein Stoppquadrat.
- [x] Der Stoppknopf oben erscheint unverändert nur für den **eigenen** laufenden Timer, trägt weiterhin „läuft seit hh:mm" (`#zeiten-laeuft`) und weiterhin **keine** Dauer.

### Der laufende Eintrag zählt nicht in die Summe

- [x] Claudes Summenzeile lautet `0:37 · 1 läuft` — die laufende Messung ist **genannt**, aber nicht addiert.
- [x] Wird `#7` gestoppt, etwa um 09:49, wächst Claudes Summe auf `1:14`, die Gesamtsumme auf `Ist 1:59`, und der Zusatz `· 1 läuft` verschwindet.
- [x] Ein Kontributor, für den **nur** ein Timer läuft und der keine abgeschlossene Zeit hat, bekommt **keine** Summenzeile mit `0:00`.
- [x] Die angezeigte Summe ändert sich nicht dadurch, dass die Seite länger offen steht — sie wird nicht nachgeführt.

### Dauer und Zeitraum als Text

- [x] `1:22`, `0:45` und `0:37` erscheinen mit Stunde ohne führende Null und zweistelliger Minute.
- [x] Eine Dauer jenseits eines Tages läuft in den Stunden weiter: 26 Stunden und 3 Minuten erscheinen als `26:03`, **nicht** als `1:02:03` und nicht gekappt.
- [x] Eine Spanne unter einer Minute erscheint als `0:00`; eine negative Spanne erscheint nie — sie wird zu `0:00`.
- [x] Ein Eintrag von vorgestern trägt statt „gestern" das ISO-Datum, in derselben Form wie die Kommentar-Metazeile: `2026-09-04 11:05 – 11:42`.

### Überlappungen werden gezeigt, nicht gemeldet

- [ ] Zwei Einträge desselben Kontributors, deren Zeiträume sich überschneiden, stehen beide in der Liste, jeder mit seinem eigenen Zeitraum, in Zeitfolge.
- [ ] Es erscheint **keine** Warnung und **keine** stille Korrektur — kein Zeitraum wird gekürzt, kein Eintrag ausgelassen.
- [x] Drei überlappende Einträge desselben Tages von je 10 Stunden (08:00–18:00, 09:00–19:00, 10:00–20:00) ergeben die Summe **`30:00`** — sie wird weder auf 24 Stunden gekappt noch als Tag ausgewiesen.

### Leerzustand

- [x] Eine Karte ohne Zeiteintrag zeigt den Satz **„Noch keine Zeit erfasst."**
- [x] Sie zeigt **keine** Summenzeile, **keine** Kopfzahl, **keine** Liste und **keine** Ist-Summe `0:00`.
- [x] Der Knopf „Timer starten" steht unverändert dort.
- [x] Mit dem ersten Start verschwindet der Satz, und die laufende Zeile erscheint — **ohne** Summenzeile, weil noch nichts abgeschlossen ist.

### Fremde laufende Timer sind in der Oberfläche beendbar

- [x] Das Stoppquadrat steht an **jeder** laufenden Zeile, auch an der eines fremden Kontributors und auch ohne gewählte Identität.
- [x] Ein Klick darauf beendet **genau diesen** Eintrag: die Zeile bekommt ihr Ende und ihre Dauer, die Summenzeile des Kontributors wächst, die Gesamtsumme wächst — **ohne** zweiten Abruf der Kartenseite.
- [x] Der beendete Eintrag trägt weiterhin **seinen** Kontributor: gestoppt wurde der Timer, nicht die Urheberschaft.
- [x] Läuft mein eigener Timer, beenden **beide** Wege denselben Eintrag — der Stoppknopf oben und das Stoppquadrat an seiner Zeile; danach steht oben wieder „Timer starten".
- [x] Ein Reload zeigt den beendeten Eintrag mit Ende und Dauer.

### Was dieser Slice ausdrücklich nicht tut

- [ ] Es entsteht **kein** Nachtragsformular, **kein** Stift an abgeschlossenen Zeilen und **kein** Löschen — das ist `I0025`.
- [ ] Es entsteht **keine** neue HTTP-Route, kein neues Endpunkt-Glied und kein neues Glied am `ZeitenApiKlient`.
- [x] Es entsteht **keine** Migration und **kein** neues Feld an `Kartendetail` oder `Zeiteintrag`.
- [ ] Es entsteht **keine** Summe in SQL und keine Zeitsumme in `KanbanC.BL`.
- [ ] Es entsteht **keine** Live-Nachführung und keine mitlaufende Dauer — das ist `I0028`.
- [ ] Es entsteht **keine** Kopfzeilenübersicht laufender Timer — das ist `I0027`.

### Der grüne Bestand bleibt grün

- [ ] Die Suiten aus `R00026` (Timer starten) und `R00027` (Timer stoppen) laufen unverändert grün; `#zeitenabschnitt`, `#timer-starten`, `#timer-stoppen` und `#zeiten-laeuft` behalten Kennung und Bedeutung.
- [x] Die Integrations- und Endpunkttests der WebApi bleiben **unverändert** — das ist zugleich der Beleg, dass serverseitig nichts fehlt.
- [x] `Zeitpunktform.AlsText` und `AlsTageszeit` verhalten sich unverändert; die neue Form kommt daneben, nicht an ihre Stelle.
- [x] Build ohne Warnung (`TreatWarningsAsErrors`).

## Betroffene Verzeichnisstruktur

- **Schema:** **unberührt.** Keine neue Datei unter `Source/KanbanC.BL/Persistenz/Migrationen/`.
- **Contracts:** **unberührt.** `Kartendetail.Zeiteintraege` und `Zeiteintrag` tragen alles, was gebraucht wird.
- **Fachlogik, Datenzugriff, Dienste, API:** **unberührt.** Kein Glied an `ZeitenService`, `ZeitenRepository`, `Zeitenleser` oder `ZeitenEndpunkte`; `Program.cs` unberührt.
- **Oberfläche — Formen:** `Source/KanbanC.Blazor/Services/Dauerform.cs` (**neu**, neben `Dateigroesseform`, `Kontributorartform`, `Kartenfarbform` und `Zeitpunktform` — dort wohnen in diesem Projekt die Formen); `Source/KanbanC.Blazor/Services/Zeitpunktform.cs` wächst um **eine** Methode und bekommt **keine** Schwesterklasse.
- **Oberfläche — Rechnung:** `Source/KanbanC.Blazor/Services/Zeitbilanz.cs` (**neu**), Muster `Teilaufgabenfortschritt` und `Laufplakette`.
- **Oberfläche — Ansicht:** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` — der bestehende `#zeitenabschnitt` wächst um Summenzeilen, Liste und Leerzustand; `SendeZeitmessungsende` bekommt einen zweiten Aufrufer.
- **Unberührt:** `Karte.razor`, `Spaltenbahnen.razor`, `Board.razor`, `Kopfzeile.razor`, `Identitaetswahl.razor`, `ZeitenApiKlient` (die gebrauchte Methode steht seit `B0334`), `wwwroot/gestaltung.css` (die Werte stehen dort).
- **Tests:** `Source/KanbanC.Blazor.Tests/Services/DauerformTests.cs` (**neu**), `Services/ZeitbilanzTests.cs` (**neu**), `Services/ZeitpunktformTests.cs` wächst; `Source/KanbanC.PlaywrightTests/Tests/ZeitenSehenE2ETests.cs` (**neu**) mit Locatoren in `PageObjects/KartendetailSeite.cs`.

## Technische Überlegungen

### Warum serverseitig nichts entsteht — und warum das die Kernregel nicht bricht

Die Projektregel lautet „was die Oberfläche kann, kann die API". Sie ist eine Zusage in **eine** Richtung: die API darf nicht weniger können als der Browser. Sie verlangt nicht, dass jede **Darstellung** eine eigene Route bekommt. Die API liefert über `GET /api/karten/{karteId}` jeden Eintrag vollständig — Kontributor, Beginn, Ende —; wer summieren will, kann es, und ein Agent, der über einen ganzen Kartenbestand rechnet, braucht ohnehin eine andere Frage als „was steht auf dieser einen Karte".

Das ist die **Gegenlage zu `I0022`** („Karten einer Klasse abrufen"), und beide Male trägt dieselbe Regel: dort blieb ein Slice bewusst **ohne Oberfläche**, weil eine Fähigkeit, die bei der API anfängt, keinen Schirm braucht; hier bleibt ein Slice bewusst **ohne API-Glied**, weil eine Darstellung, die bei der Oberfläche anfängt, keine Route braucht. Die Zusage ist in beiden Fällen unverletzt.

**Verworfen: eine Route `GET /api/karten/{karteId}/zeiten/bilanz`.** Sie wäre ein zweiter Abruf für einen Block, den die Seite bereits vollständig in der Hand hat — und damit ein Fenster, in dem Liste und Summe auseinanderlaufen, sobald zwischen beiden Abrufen jemand stoppt.

### Gerechnet wird in der Oberflächenschicht, an genau einer Stelle

`Zeitbilanz.Fuer(_detail.Zeiteintraege)` liefert Kontributorensummen **und** Gesamtsumme aus **einem** Aufruf. Das ist keine neue Wahl, sondern die Hausregel dieses Projekts, wörtlich am `Kartendetail` über den Teilaufgaben: „ein gespeicherter Fortschritt wäre eine zweite Wahrheit neben der Liste, die ihn trägt … Gerechnet wird er in der Oberflächenschicht." `Teilaufgabenfortschritt`, `Bahnenkopfzahl` und `Laufplakette` folgen ihr bereits.

**Verworfen: `SUM` in SQL.** Es wäre ein zweiter Leseweg neben der Einträgeliste; die Dauer je Zeile müsste trotzdem in C# entstehen, und der Filter „nur abgeschlossene" stünde dann an zwei Stellen, die auseinanderlaufen können.

**Verworfen: ein Summenfeld am `Kartendetail`.** Eine gespeicherte Summe neben den Einträgen, die sie tragen, ist genau die zweite Wahrheit, die das DTO für die Teilaufgaben ausschließt.

**Für `I0033` und `I0036`:** die Kartensumme wohnt heute in der Oberfläche, weil nur sie sie braucht. Rechnet `I0033` über einen **Kartenbestand**, ist das eine andere Frage und kein zweiter Weg zu derselben Zahl. Braucht es dort doch die Kartensumme, wandert `Zeitbilanz` in die BL und die Oberfläche verbraucht sie — das entscheidet `I0033` mit dem dann bekannten Bedarf; heute wäre es geraten.

### Der laufende Eintrag zählt nicht — und was das Artboard stattdessen zeigt

Summiert werden nur Einträge mit `Ende is not null`, deren Dauer **feststeht und wahr bleibt**. Eine mitlaufende Dauer wäre ab der ersten Sekunde falsch, solange kein Live-Kanal sie nachführt — und Blazor Server rendert nur, wenn etwas passiert. Es ist dieselbe Begründung, mit der `I0023` „läuft seit 08:04" statt „1:36 läuft" gewählt hat (`B0326`, `B0328`, `Laufplakette`, `Zeitpunktform.AlsTageszeit`) und `I0024` den Stoppknopf ohne Dauer gelassen hat (`B0335`). Sie hier umzudrehen hieße, dieselbe Frage an derselben Seite dreimal verschieden zu beantworten.

Damit die fehlende Zeit nicht **stillschweigend verschwindet**, nennt die Bilanz die laufenden getrennt: `0:37 · 1 läuft`. Wer die Zahl liest, sieht, dass sie unvollständig ist, und warum.

**Abweichung vom Artboard, benannt:** `D0006.dc.html` zeichnet die laufende Zeile mit `2:14` und Claudes Summe als `2:51` (= 2:14 + 0:37), die Ist-Summe als `3:36`. Hier steht in der Zeile `heute 09:12 – läuft` **ohne Dauer**, Claudes Summe ist `0:37 · 1 läuft` und die Ist-Summe `1:22`. Die mitlaufende Dauer ist keine verworfene Idee, sondern eine **Ausbaustufe an `I0028`** (Live-Kanal); geplant wird sie dort, wenn er läuft — nicht hier.

### Die Liste ist flach und chronologisch

Neueste zuerst, nicht nach Kontributor gruppiert. Die Gruppierung leistet die Summenzeile darüber; eine gruppierte Liste zerrisse die Zeitfolge — und genau die macht eine **Überlappung** lesbar. Die Umkehrung entsteht in der Anzeige: `LiesZeiteintraegeDerKarte` bleibt bei `ORDER BY z.Beginn, z.ZeiteintragId`, weil eine Reihenfolge in der Persistenz keine Wahrheit behauptet und `MitEintrag` in `Kartendetail.razor` dieselbe Folge hält.

### Überlappungen werden gezeigt, nicht gemeldet und nicht korrigiert

Seit `I0023` dürfen zwei Timer desselben Menschen gleichzeitig laufen (auf verschiedenen Karten), und `I0025` wird das Überlappen auch beim Nachtragen erlauben. Der Preis ist benannt: **eine Summe je Kontributor kann mehr als 24 Stunden je Tag ausweisen.** Für einen Agenten, der an mehreren Karten zugleich rechnet, ist diese Zahl wahr; für einen Menschen ist sie ein sichtbarer Erfassungsfehler.

Eine Warnung wäre trotzdem falsch: sie hätte in **diesem** Slice keine Kompensationsaktion, denn korrigieren kann erst `I0025`. Sichtbar bleibt es dadurch, dass jede Zeile ihren Zeitraum einzeln nennt und in Zeitfolge steht. Deshalb lässt `Dauerform.AlsText` die Stunden über 24 hinauslaufen (`30:00`), statt in Tage umzubrechen: eine Karte sammelt Arbeitszeit über Wochen, und `1:06:00` wäre eine dritte Zeitform in derselben Spalte.

### Der Leerzustand ist ein Satz, keine leere Tabelle

Ohne Eintrag steht „Noch keine Zeit erfasst." und sonst nichts — keine Summenzeile, keine Kopfzahl, kein Nullwert je Kontributor. Das Artboard sagt es selbst: „Die Summen entstehen mit dem ersten Eintrag; vorher gäbe es nur Nullen zu lesen." Aus demselben Grund bekommt ein Kontributor, für den nur ein Timer läuft, keine Zeile mit `0:00`. Muster ist die leere Kommentar- und Anhangliste derselben Seite.

Anders als dort trägt der Satz **keine** angehängte Handlung („· nachtragen"): die Handlung dieses Blocks ist der Startknopf, der ohnehin darüber steht, und der Nachtrag gehört `I0025`.

### Die eingelöste Schuld aus `I0024`

`R00027` hat es wörtlich hierher verwiesen: „Fremde Timer sind in der Oberfläche noch nicht beendbar: das Bild setzt das Stoppquadrat an die *Eintragszeile*, und die Einträgeliste gehört `I0026`." Der Kommentar im Zeitenblock sagt dasselbe. Mit der Liste entsteht die Zeile — und damit der Ort für das Quadrat.

Es entsteht dabei **keine neue Fähigkeit**: Endpunkt (`B0333`) und Klientenglied (`B0334`) sind grün, und `SendeZeitmessungsende(long zeiteintragId)` in `Kartendetail.razor` nimmt die Nummer bereits entgegen. Die Zeilenhandlung ist ein **zweiter Aufrufer** desselben Sendeglieds; `StoppeTimer` (der Knopf oben) sucht weiterhin den eigenen laufenden Eintrag und ruft dasselbe. Ob beide Wege zusammengelegt werden, entscheidet die Umsetzung — die Fähigkeit ist in beiden Fällen dieselbe.

**Der Stift zum Ändern kommt nicht mit.** Das Artboard zeichnet ihn an abgeschlossenen Zeilen; er gehört `I0025` und braucht dessen Formular. Eine Handlung, die nichts tut, wäre schlechter als keine.

### Der Stoppknopf oben bleibt unverändert

`#zeiten-laeuft` sagt dasselbe wie die laufende Zeile in der Liste, liest aber **denselben** Eintrag — keine zweite Wahrheit, sondern zwei Blicke auf einen Wert. Er behält Kennung, Wortlaut und Verhalten, damit die grüne `R00027`-Suite unangetastet bleibt.

### Kein „von h:mm Soll"

Das Artboard zeichnet „Ist 3:36 **von 5:00 Soll**". Eine Sollzeit gibt es im Bestand nirgends — weder an `Karte` noch an `Kartendetail` noch in der Datenbank. Sie gehört `I0033` („Soll-Ist-Vergleich abrufen") und braucht dort ihre eigene Quelle (die WBS-Zählung). Angezeigt wird hier nur das Ist.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0006.dc.html`](../Dokumentation/Wireframes/D0006.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gelten daraus **Zustand 1** (`:91`, der ganze Zeitenblock in seiner Zielform — Reihenfolge Handlung, Bilanz, Belege), **Zustand 2 Fassung B** (`:275`, der Leerzustand) und **Zustand 2 Fassung C** (`:296`, die Anatomie eines Eintrags samt der Begründung für den fremden Stopp). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen **keine** Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Keine Dauer an der laufenden Zeile und keine laufende Zeit in den Summen** — begründet oben; das Bild zeigt `2:14`, `2:51` und `3:36`, hier stehen „läuft", `0:37 · 1 läuft` und `Ist 1:22`.
2. **Kein „von 5:00 Soll"** neben der Ist-Summe — es gibt keine Sollzeit im Bestand (`I0033`).
3. **Kein Stift an abgeschlossenen Zeilen** und keine Nachtragszeile am Fuß — beides `I0025`.

### Ablauf

1. **Kartenseite laden** (unverändert)
   - 1.1 `KartenApiKlient.LadeDetail(karteId)` → `Kartendetail` mit `Zeiteintraege` (seit `B0321`)
2. **Zeitenblock rendern**
   - 2.1 `Zeitbilanz.Fuer(_detail.Zeiteintraege)` — **eine** Rechenstelle
     - 2.1.1 Je Kontributor die Summe über `Ende is not null` (`Ende - Beginn`), nie negativ
     - 2.1.2 Je Kontributor die Zahl der laufenden (`Ende is null`)
     - 2.1.3 Kontributoren **ohne** abgeschlossene Zeit erhalten keine Zeile
     - 2.1.4 Sortierung absteigend nach Summe, bei Gleichstand nach Name
     - 2.1.5 Gesamtsumme = Summe der Kontributorensummen
   - 2.2 Ist-Summe neben den Timerknopf: `Dauerform.AlsText(bilanz.Gesamtsumme)`
   - 2.3 Summenzeilen über die Liste; bei `Laufende > 0` der Zusatz `· n läuft`
   - 2.4 Liste: `Zeiteintraege` absteigend nach `Beginn`, bei Gleichstand absteigend nach `ZeiteintragId`
     - 2.4.1 Je Zeile `Zeitpunktform.AlsZeitraum(beginn, ende, jetzt)`
     - 2.4.2 Abgeschlossen → Dauer über `Dauerform.AlsText(ende - beginn)`, keine Handlung
     - 2.4.3 Laufend → Akzentkante, „läuft" statt des Endes, keine Dauer, **Stoppquadrat**
   - 2.5 Ist `Zeiteintraege` leer → nur der Satz „Noch keine Zeit erfasst."; Summenzeilen, Kopfzahl, Liste und Ist-Summe entfallen
3. **Stoppquadrat an einer Zeile**
   - 3.1 `SendeZeitmessungsende(zeile.ZeiteintragId)` — dasselbe Glied wie der Knopf oben
   - 3.2 Der zurückkommende Eintrag ersetzt den alten über `MitEintrag`
   - 3.3 Bilanz, Zeile und Ist-Summe ziehen beim Neuaufbau nach — **kein** zweiter Abruf
   - 3.4 Zurückweisung oder Ausfall → die bestehenden Meldewege der Seite, unverändert

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** der bestehende `#zeitenabschnitt` in `Kartendetail.razor` (`:501`) — zwischen Timerknopf und dem Platz, den `I0025` für den Nachtrag bekommt. Kein neuer Schirm, keine neue Route, kein neuer Dienst in `Program.cs`.

- `Dauerform` (Operation, statisch, Oberflächenschicht) — eine Zeitspanne als `h:mm`; die Stunden laufen über 24 hinaus, eine Spanne unter einer Minute wird `0:00`, eine negative nie ausgegeben.
  - `static string AlsText(TimeSpan dauer)`
- `Zeitpunktform` (bestehend, Operation) — wächst um **eine** Methode; der Tagesbezug („heute", „gestern", sonst ISO-Datum) entsteht **nicht** ein zweites Mal, sondern wird von `AlsText` mitgenutzt. **Ohne** den Zweig „vor n Min": als Anfang einer Spanne ist eine relative Angabe unlesbar („vor 3 Min – 18:25"). Das Ende trägt nur die Tageszeit. „jetzt" bleibt ein Parameter.
  - `static string AlsZeitraum(DateTimeOffset beginn, DateTimeOffset? ende, DateTimeOffset jetzt)`
- `Zeitbilanz` (Record, immutable; Muster `Teilaufgabenfortschritt` und `Laufplakette` — Record mit statischem `Fuer`) — die eine Rechenstelle des Blocks: Summen je Kontributor **und** Gesamtsumme aus einem Aufruf.
  - `static Zeitbilanz Fuer(IReadOnlyList<Zeiteintrag> zeiteintraege)`
  - `IReadOnlyList<Kontributorenzeitsumme> Kontributorensummen`
  - `TimeSpan Gesamtsumme`
- `Kontributorenzeitsumme` (DTO der Oberflächenschicht, immutable) — ein Kontributor, seine Summe über die abgeschlossenen Einträge und die Zahl seiner laufenden.

**Keine** Klasse in `KanbanC.BL`, **keine** in `KanbanC.Contracts`, **kein** Interface: es gibt je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

- `Zeitpunktform` — eine Methode mehr (`AlsZeitraum`); die bestehenden bleiben unverändert.
- `Kartendetail.razor` — der Zeitenblock wächst um Summenzeilen, Einträgeliste und Leerzustand; `SendeZeitmessungsende` bekommt einen zweiten Aufrufer aus der Zeile. `MitEintrag`, `StarteTimer`, `StoppeTimer` und die Identitätswahl bleiben unverändert.
- `Kartendetail.razor.css` — Zeilenform, Akzentkante und Summenzeilen; alle Werte über Variablen aus `gestaltung.css`.
- `KartendetailSeite` (Playwright) — neue Locatoren neben `Zeitenabschnitt` und `ZeitenLaeuft`, die unverändert bleiben.

**Die Datei ist heute 1726 Zeilen lang.** Ob der Zeitenblock dabei in eine eigene Komponente zieht, entscheidet die Umsetzung — es ist eine Frage der Lesbarkeit, keine der Fachlichkeit, und `I0025` wird denselben Block noch einmal anfassen.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP), im Projekt `KanbanC.Blazor.Tests`:**
- `Dauerform.AlsText` — `2:14`; `0:45`; `26:03` jenseits eines Tages (nicht `1:02:03`); `30:00` aus drei überlappenden Zehnstündern; unter einer Minute `0:00`; eine negative Spanne ergibt `0:00` und nie ein Minuszeichen.
- `Zeitpunktform.AlsZeitraum` — abgeschlossen von heute, von gestern, von vorgestern (ISO-Datum); laufend („… – läuft") **ohne** Dauer; kein „vor n Min" als Anfang einer Spanne; „jetzt" als Parameter, keine Uhr im Inneren.
- `Zeitbilanz.Fuer` — **drei Ränder als drei Tests, nicht als einer**: leere Liste (keine Zeile, Gesamtsumme `0:00`); nur laufende Einträge (keine Summenzeile, `Laufende` gezählt); Gleichstand zweier Summen (Reihenfolge nach Name). Dazu der Regelfall des Beispiels: Stefan `0:45`, Claude `0:37 · 1 läuft`, Gesamtsumme `1:22` — und die Gegenprobe, dass **kein** laufender Eintrag in eine Summe eingeht.
- Der Beweis der Rechnung ist die Zahl, nicht der Aufruf: jeder Test vergleicht Summen, nicht das Vorhandensein einer Zeile.

**Integration:** **keine neuen.** Es entsteht kein Endpunkt, kein Repository-Glied und keine Migration. Dass die bestehenden Integrations- und Endpunkttests **unverändert** grün bleiben, ist der Beleg dafür, dass serverseitig nichts fehlt — und gehört als Aussage in den Abschluss, nicht als neuer Test.

**Blazor-Tests (unterhalb E2E):** `ZeitenApiKlient` bekommt **kein** neues Glied und damit keinen neuen Test; die vorhandenen bleiben.

**E2E** (`ZeitenSehenE2ETests`, beide Prozesse auf freien Ports nach Skill `freier-port`): eine frische Karte zeigt „Noch keine Zeit erfasst."; nach Start und Stopp steht eine abgeschlossene Zeile mit Dauer und eine Summenzeile; ein **zweiter** Kontributor (über die Identitätswahl) bekommt seine eigene Zeile; ein laufender Eintrag zeigt „läuft" **ohne** Dauer und zählt **nicht** in die Summe; das **Stoppquadrat beendet einen fremden Timer**, und die Summe wächst danach; jeder Stand überlebt den Reload. Der Lauf braucht **kein** `I0025`: zwei Kontributoren entstehen über die Identitätswahl, Einträge über Start und Stopp.

Repositories, `Zeitenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00027`** (Timer stoppen — `I0024`, **grün**). Das ist genau der eine Knoten der WBS-Spalte `Braucht` von `I0026`; er ist erfüllt, der Slice ist **frei**. Ohne abgeschlossene Einträge gäbe es keine Dauer zu zeigen und keine Summe zu bilden.
- Setzt außerdem auf: **`R00026`** (`I0023` — Tabelle, `Kartendetail.Zeiteintraege`, Zeitenblock, `Zeitpunktform`), **`R00017`** (Kartendetailseite), **`R00013`** (Identität wählen), **`R00011`**/**`R00014`** (Kontributor mit Stilllegung), **`R00019`** (Muster der Kommentarliste: Initialenkreis, Kopfzahl, Leerstand), **`R00005`** (`gestaltung.css`). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **`I0025`** („Zeiteintrag nachtragen und ändern") führt `I0026` ausdrücklich in seiner Spalte `Braucht` — sein Änderungsformular klappt in einer Zeile **dieser** Liste auf. Ebenso **`I0033`** („Soll-Ist-Vergleich abrufen", `Braucht: I0030, I0026`) und **`I0036`** („Zeiten exportieren", `Braucht: I0026`).

## Umfang

```
Zeiten einer Karte sehen (I0026) = 9 Bubbles: 5 Standard (5,2h), 4 unklar (3,2–8,5h).
Rest: 5,2h klar + 3,2–8,5h unklar · 2 von 9 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 9 Bubbles gruen (0 %) · 0 laufen · 9 offen
```

`I0026` ist vollständig bis zur Bubble geplant und trägt seine neun Bubbles (`B0349`–`B0357`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: der Slice hat **einen** prüfbaren Aspekt — die Karte zeigt ihre Zeiten; Liste, Summen und Leerzustand teilen Datenquelle, Komponente und E2E-Weg. Getrennt geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0020` bis `I0025`. **Die Requirement-Klammer sitzt deshalb allein an `I0026`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0349` Dauer als Text | Operation | 0,4h (belegt) |
| `B0350` Zeitraum einer Zeile als Text | Operation | 0,4h (belegt) |
| `B0351` Zeitbilanz je Kontributor | Operation | 0,4–1,5h (**unklar**) |
| `B0352` Einträgeliste im Zeitenblock | UI | 2h (Richtwert) |
| `B0353` Laufend und abgeschlossen unterscheiden | UI | 0,4–1,5h (**unklar**) |
| `B0354` Summenzeilen und Ist-Summe | UI | 2h (Richtwert) |
| `B0355` Leerzustand des Zeitenblocks | UI | 0,4h (Richtwert) |
| `B0356` Stoppquadrat an der laufenden Zeile | UI | 0,4–1,5h (**unklar**) |
| `B0357` E2E Zeiten einer Karte sehen | E2E | 2–4h (**unklar**) |

Mit neun Bubbles liegt der Slice zwischen `I0024` (sieben) und `I0023` (elf) — und das, obwohl **nichts** serverseitig entsteht: alle neun sitzen in der Oberflächenschicht, drei davon in reinen Formen und Rechnungen. Die vier unklaren Bubbles haben verschiedene Ursachen: `B0351` hält drei Ränder unter einer Rechnung (leere Liste, nur laufende, Gleichstand); `B0353` muss vier Unterschiede in eine bestehende Zeilenform bringen, ohne die Kommentar- und Anhangzeilen zu verbiegen; `B0356` klärt, ob Zeilenhandlung und Kopfknopf denselben Weg teilen; `B0357` braucht zwei Kontributoren, Start, Stopp, fremden Stopp und Reload in einem Lauf. Derselbe Vermerk wie bei `I0005` bis `I0024`: die 2h-Richtwerte für UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0026` trägt keine eigene Zählzeile; sie hält die sieben Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der neun Bubbles gezählt.

## Offene Fragen

- **Zählt ein laufender Eintrag in die Summe?** — **entschieden: nein.** Summiert werden nur abgeschlossene; die laufenden werden **getrennt genannt** (`0:37 · 1 läuft`), damit die fehlende Zeit nicht stillschweigend verschwindet. Begründung: eine gerenderte Dauer ist ohne Live-Kanal ab der ersten Sekunde falsch — dieselbe Entscheidung wie in `B0326`, `B0328` und `B0335`. **Die Umkehrung** wäre eine Summe, die stimmt, solange man nicht hinsieht. **Mit `I0028` (Live-Kanal) fällt der Grund weg** — die mitlaufende Dauer ist dann eine Ausbaustufe und wird **dort** geplant. **Sichtbare Abweichung vom Artboard** (dort `2:14` / `2:51` / `3:36`, hier „läuft" / `0:37 · 1 läuft` / `1:22`). **Nicht am Menschen geprüft.**
- **Darf eine Kontributorensumme mehr als 24 Stunden je Tag ausweisen?** — **entschieden: ja, ohne Warnung.** Überlappende Einträge sind seit `I0023` möglich und werden in `I0025` erlaubt; sie werden **gezeigt, nicht gemeldet und nicht korrigiert**. Eine Warnung wäre für einen Agenten, der an mehreren Karten zugleich misst, eine Falschauskunft — und hätte hier **keine Kompensationsaktion**, weil korrigieren erst `I0025` kann. `Dauerform.AlsText` lässt die Stunden deshalb über 24 hinauslaufen (`30:00`). **Die Umkehrung** wäre eine Meldung ohne ausführbaren Ausweg oder eine stille Kappung, die Messdaten verfälscht. **Neu zu stellen, sobald `I0025` steht.** **Nicht am Menschen geprüft.**
- **Wird die Liste nach Kontributor gruppiert?** — **entschieden: nein, flach und chronologisch, neueste zuerst.** Die Gruppierung leistet die Summenzeile; die Zeitfolge macht die Überlappung lesbar. **Nicht am Menschen geprüft.**
- **Zeigt der Leerzustand Nullen?** — **entschieden: nein**, nur den Satz „Noch keine Zeit erfasst." Beleg: `D0006.dc.html`, Zustand 2 Fassung B („Die Summen entstehen mit dem ersten Eintrag"). **Nicht am Menschen geprüft.**
- **Kommt das Stoppquadrat an fremden laufenden Zeilen mit?** — **entschieden: ja.** Es ist die von `R00027` ausdrücklich hierher verwiesene Schuld, und es entsteht **keine neue Fähigkeit** (Endpunkt und Klientenglied sind grün). **Nicht geprüft ist, ob der Mensch beim Stoppen eines fremden Timers eine Bestätigung erwartet** („Der Timer von Claude läuft seit 3:12 — wirklich beenden?"). `R00027` hat genau diese Frage hierher weitergereicht; sie wird hier **ohne** Bestätigung beantwortet, weil die Anwendung im Full-Trust-Modell läuft und ein Rückfragedialog an keiner anderen Handlung dieses Projekts existiert. **Nicht am Menschen geprüft.**
- **Entsteht eine Route für die Bilanz?** — **entschieden: nein.** Gerechnet wird in der Oberflächenschicht, an genau einer Stelle. Die Kernregel bleibt gewahrt: sie ist eine Zusage in **eine** Richtung, und die API liefert jeden Eintrag vollständig. **Die Umkehrung** wäre ein zweiter Abruf für Daten, die die Seite schon hat — und ein Fenster, in dem Liste und Summe auseinanderlaufen. **Zu prüfen, wenn `I0033` über einen Kartenbestand rechnet.** **Nicht am Menschen geprüft.**
- **Zieht der Zeitenblock in eine eigene Komponente?** — **offen und bewusst nicht entschieden.** `Kartendetail.razor` ist 1726 Zeilen lang, `I0025` fasst denselben Block noch einmal an. Das ist eine Frage der Lesbarkeit, keine der Fachlichkeit; sie gehört in die Umsetzung, nicht in die Anforderung.

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration und keine Konfiguration.

## Manuelle Nachbereitungstätigkeiten

- Keine. Bestehende Zeiteinträge erscheinen nach dem Deployment ohne Zutun in der Liste.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Erfassung, die nichts zurückgibt: seit `R00027` entstehen abgeschlossene Zeiteinträge, aber **im Browser sieht sie niemand** — die gemessene Zeit verschwindet in dem Moment, in dem der Timer steht, und wer sie braucht, addiert JSON von Hand. Die Kausalkette: **wenn** die Kartenseite die Einträge, die sie ohnehin lädt, als Liste zeigt und daraus an **einer** Stelle die Summe je Kontributor rechnet (X), **dann** hat die Karte zum ersten Mal eine ablesbare Ist-Zahl, und zugleich entsteht die Zeile, an der Änderung (`I0025`) und der fremde Stopp hängen (Y), **und dann** wird aus einer Messung eine Rückkopplung: der Mensch sieht am Ort der Arbeit, was sie gekostet hat, und Soll-Ist (`I0033`) sowie Export (`I0036`) bekommen den Weg dorthin freigemacht (Z). **Der Hebel liegt bei der Anzeige und nicht beim Server:** die Daten sind vollständig da, sie sind nur unsichtbar — eine weitere Route hätte nichts hinzugefügt, was `GET /api/karten/{karteId}` nicht schon liefert, und hätte eine zweite Rechenstelle geschaffen. Und er liegt vor `I0025`: korrigieren kann man nur, was man zuerst sieht.

## Missing-Docs

- **`TimeSpan` als `h:mm` jenseits von 24 Stunden formatieren:** `TimeSpan.ToString("h\\:mm")` bricht bei einem Tag um; im Repository gibt es dafür kein Vorbild — `Dateigroesseform` und `Zeitpunktform` formatieren keine Spannen. Wie die Stunden über 24 hinaus sauber entstehen (`TotalHours` abgeschnitten plus Minuten), ist nirgends vorgemacht.
- **Blazor Server und Zeitzonen:** `Zeitpunktform` rechnet in die Zeitzone des **Servers**, nicht des Browsers, und sagt das in seinem Kopfkommentar. Für „heute/gestern" an einer Zeitraumangabe wird diese Annahme zum zweiten Mal getragen; ob sie im LAN-Betrieb mit mehreren Rechnern trägt, ist im Repository nicht belegt.

## Notizen

### Verworfene Alternativen

- **`GET /api/karten/{karteId}/zeiten/bilanz`** — ein zweiter Abruf für einen Block, den die Seite vollständig in der Hand hat; das Fenster zwischen beiden Antworten wäre ein echter Fehlerfall.
- **`SUM` über die Zeiteinträge in SQL** — ein zweiter Leseweg neben der Liste; die Dauer je Zeile entstünde trotzdem in C#, und „nur abgeschlossene" stünde an zwei Stellen.
- **Ein Summenfeld an `Kartendetail`** — genau die zweite Wahrheit, die das DTO für den Teilaufgabenfortschritt ausschließt.
- **Die laufende Zeit mitzählen und mitrendern** — ab der ersten Sekunde falsch, solange kein Live-Kanal sie nachführt; Blazor Server rendert nur auf Anlass. Kommt als Ausbaustufe mit `I0028`.
- **Die Liste nach Kontributor gruppieren** — zerrisse die Zeitfolge, an der die Überlappung ablesbar ist; die Gruppierung leistet die Summenzeile.
- **Überlappungen melden oder still zusammenfassen** — eine Meldung ohne Kompensationsaktion; korrigieren kann erst `I0025`, und eine Kappung verfälschte Messdaten.
- **Die Summe auf 24 Stunden je Tag deckeln oder in Tagen ausweisen** — eine dritte Zeitform in derselben Spalte, und ein Deckel machte aus einem sichtbaren Erfassungsfehler eine unsichtbare Lüge.
- **Eine Summenzeile mit `0:00` für Kontributoren, die nur laufen** — Nullen zu lesen ist keine Auskunft; die laufenden nennt der Zusatz `· n läuft` an den Zeilen, die es gibt.
- **Den Stift zum Ändern gleich mitnehmen** — er braucht das Formular aus `I0025`; eine Handlung, die nichts tut, ist schlechter als keine.
- **Eine eigene `Zeitraumform` neben `Zeitpunktform`** — der Tagesbezug („heute", „gestern", ISO) stünde dann zweimal; er wächst an der bestehenden Form.
- **`Dauerform` in `KanbanC.BL`** — dort braucht sie heute niemand; ein Glied ohne Aufrufer wäre tote Flexibilität (C24). Wandert mit `I0033`, wenn dort ein Bedarf entsteht.
- **Eine Bestätigungsfrage vor dem fremden Stopp** — es gibt in diesem Projekt an keiner Handlung einen Rückfragedialog, und Full Trust ohne Anmeldung ist eine Leitplanke der Vision.

### Bewusst out of scope

- **Zeiteintrag nachtragen, ändern, löschen** — `I0025`; das Änderungsformular klappt in einer Zeile **dieser** Liste auf.
- **Laufende Timer in der Kopfzeile** — `I0027`.
- **Live-Nachführung ohne Reload und die mitlaufende Dauer** — `I0028`/`D0007`.
- **„von h:mm Soll" neben dem Ist** — `I0033`; im Bestand gibt es keine Sollzeit.
- **Zeiten exportieren** — `I0036`.
- **Eine Zeitsumme über mehr als eine Karte** — `I0033`.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Der Slice liegt ganz in der Oberfläche** — keine Route, kein Endpunkt, kein Klientenglied, keine Migration.
2. **Gerechnet wird in der Oberflächenschicht, an genau einer Stelle** (`Zeitbilanz.Fuer`).
3. **Ein laufender Eintrag zählt nicht in die Summe**, wird aber getrennt genannt — **sichtbare Abweichung vom Artboard.**
4. **Überlappungen werden gezeigt, nicht gemeldet und nicht korrigiert**; eine Summe darf 24 Stunden je Tag überschreiten.
5. **Die Liste ist flach und chronologisch, neueste zuerst.**
6. **Der Leerzustand ist ein Satz, keine Nullen.**
7. **Das Stoppquadrat kommt mit, auch am fremden Timer, und ohne Bestätigungsfrage.**
8. **Kein „von h:mm Soll".**
