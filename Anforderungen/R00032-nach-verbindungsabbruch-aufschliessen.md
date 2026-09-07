---
id: R00032
status: Neu
datum: 2026-09-07
---

# R00032: Nach Verbindungsabbruch aufschließen

## Beschreibung

Reißt die Verbindung ab, sagt die Oberfläche es — und wenn sie zurückkommt, holt jede offene Sicht ihren Stand frisch, nennt in einem Band, wie viele Änderungen in der Lücke zusammenkamen und seit wann, und markiert die Karten, die sich bewegt haben. Ein offenes Feld wird dabei nie überschrieben. Reißt statt der Leitung der Kreislauf zwischen Browser und Blazor ab, spricht der Trennungsdialog deutsch und nennt den Zeitpunkt, seit dem der Schirm alt ist.

Zahlt ein auf: [Vision](R00000-vision.md) — „**Live überall.** Bewegt ein Mensch oder die API eine Karte, sehen alle offenen Oberflächen die Änderung unverzüglich — ohne Reload, ohne Nachfragen." `R00031` hat den Kanal gebaut und die Lücke wörtlich stehen lassen: „Nachgeholt wird beim Wiederaufnehmen **nichts**." Diese Anforderung schließt genau sie.

**Die tragende Unterscheidung dieses Slice: es gibt zwei verschiedene Abbrüche, und der Trenner ist „wer noch rendern kann".**

1. **Blazor ↔ WebApi** — die Ereignisleitung (`GET /api/ereignisse`) reißt. Der Blazor-Prozess lebt, jede Sicht ist sichtbar **und bedienbar**; schreibende Aufrufe scheitern und melden sich über den bestehenden `WebApiAufruf.MitAusfallmeldung`. Neu ist allein, dass die Sicht **weiß, dass sie altert** — und danach aufschließt.
2. **Browser ↔ Blazor** — der SignalR-Kreislauf reißt. Davon merkt der Server nichts, und **bedienbar ist nichts**: der Kreislauf *ist* die Bedienung. Hier gibt es **nichts aufzuschließen**, nur einen Dialog, der die Wahrheit sagt.

**Nachgesehen statt vermutet:** `Source/KanbanC.Blazor/Program.cs` setzt keine `CircuitOptions`; es gilt die Vorgabe-Aufbewahrung des getrennten Kreislaufs (drei Minuten). Während einer kurzen Trennung lebt die Komponente serverseitig weiter, nimmt Kartenereignisse an und rendert; beim Wiederanschluss bekommt der Browser den aktuellen Stand. Wird der Kreislauf endgültig verworfen, lädt die Seite neu und holt ohnehin frisch. **Fall 2 braucht deshalb kein Aufschließen.**

**Folge, die man leicht übersieht: die Kopfzeilenmarke „nicht live · Stand von 09:12" gehört ausschließlich zu Fall 1.** Auf einem abgerissenen Kreislauf kann der Server nichts mehr zeichnen — eine gerenderte Marke erreichte den Browser nie. Die „drei Stufen" des Artboards (`D0007.dc.html`, Zustand 5) sind damit **nicht drei Stufen eines Abbruchs**: Stufe 1 und 2 sind Fall 1, Stufe 3 ist Fall 2.

**Der Slice ändert die WebApi nicht.** Anders als `I0028` entsteht kein Endpunkt, kein Vertrag und keine Migration — alles Neue wohnt in `KanbanC.Blazor`. Die Zusage „was die Oberfläche kann, kann die API" bleibt trotzdem gehalten: der Ereignisstrom steht jedem Agenten schon offen, und der Neuabruf läuft über `GET /api/boards/{boardId}` und `GET /api/karten/{karteId}`, die es beide gibt. Aufschließen ist eine Eigenschaft einer *Sicht*, nicht eine Fähigkeit, die jemandem fehlt.

**`D0007` wird mit diesem Slice grün** — es ist der letzte offene Knoten des Dialogs.

## Geschäftlicher Nutzen

`R00031` hat den Live-Kanal gebaut und dabei eine Lücke ausdrücklich offen gelassen und benannt: was während einer Trennung geschieht, ist weg, und **niemand wird darauf hingewiesen**. Das ist die unangenehmste Sorte Lücke, weil sie sich wie ihr Gegenteil anfühlt. Ein Board ohne Live-Kanal erzieht seinen Benutzer dazu, nachzuladen. Ein Board **mit** Live-Kanal erzieht ihn dazu, es nicht mehr zu tun — und genau dann kostet eine stille Trennung, was sie vorher nicht gekostet hätte: der Mensch arbeitet weiter, überzeugt, alles zu sehen.

Der Betrieb macht das nicht theoretisch. Die WebApi darf neu starten, während die Oberfläche läuft — `R00031` hat die Wiederaufnahme genau dafür gebaut. Ein Laptop klappt zu, ein WLAN fällt für zwanzig Sekunden aus, ein `dotnet run` wird neu gestartet. Jedes Mal ist die Sicht danach still falsch.

Der zweite Nutzen ist die **Ehrlichkeit des getrennten Schirms**. Solange die Verbindung steht, sagt die Kopfzeile nichts — dieselbe Regel, mit der `Laufzaehler.Fuer` `null` liefert, wenn nichts läuft. Bricht sie ab, ist genau das die Nachricht: der Schirm altert, und er sagt, seit wann. Der Wert eines getrennten Schirms liegt nicht darin, dass er noch etwas kann, sondern darin, dass er über sein Alter nicht lügt.

Der dritte ist ein **Befund am Bestand**, der hier mit erledigt wird: `Components/Layout/ReconnectModal.razor` steht aus der Blazor-Vorlage englisch in einer durchgehend deutschen Oberfläche („Rejoining the server…", „Retry", „The session has been paused by the server.") und wird heute **von keinem Test berührt**. Die eine Zeile, die das Artboard über die Vorlage hinaus zeichnet — seit wann der Stand alt ist —, ist Inhalt genau dieses Slice; die Datei wird ohnehin geöffnet. Eine eigene Anforderung für sieben Zeilen Übersetzung in einer Datei, die hier sowieso angefasst wird, kostete mehr, als sie ordnete.

## Funktionale Anforderungen

- Reißt die Ereignisleitung zur WebApi ab, tragen **alle** offenen Sichten des Blazor-Prozesses in der Kopfzeile die Marke „nicht live · Stand von 09:12".
- Steht die Leitung, steht in der Kopfzeile **nichts** — es gibt keine dauerhafte „live"-Marke.
- Die Marke nennt einen **Zeitpunkt**, keine mitzählende Dauer.
- Der genannte Zeitpunkt ist der **Zeitpunkt des Abrisses**, nicht der des letzten empfangenen Ereignisses.
- Während der Trennung wird die Fläche **ruhiger, nicht unlesbar** — wer liest, was dasteht, darf weiterlesen.
- Kommt die Leitung zurück, holt jede offene Sicht ihren Stand über den bestehenden Ladeweg **frisch**.
- Ein Band über den Bahnen nennt, **wie viele** Änderungen seit dem Abriss zusammenkamen und **seit wann**.
- Die geänderten Karten tragen eine **Nachholmarke**; sie bleibt stehen, bis das Band geschlossen wird.
- Sind es mehr Änderungen als die Zusammenfassungsschwelle, bleibt es bei der Zahl im Band ohne einzelne Marken.
- Hat sich **nichts** geändert, erscheint **kein** Band.
- Läuft gerade ein Zug, wartet das Aufschließen bis zum Loslassen — wie eine fremde Bewegung.
- Steht auf der Kartenseite ein Feld offen, wird nichts ausgetauscht; der ungesendete Text bleibt stehen.
- Die **erste** Verbindung nach dem Start meldet keine Rückkehr und löst kein Aufschließen aus.
- Reißt der Kreislauf zwischen Browser und Blazor ab, ist **jede Zeile** des Trennungsdialogs deutsch.
- Der Trennungsdialog nennt den Zeitpunkt, seit dem der Schirm alt ist.

## Nicht-funktionale Anforderungen

- **Kein neuer Dienst und kein zweiter Singleton:** der Verbindungsstand hängt am bestehenden `Ereignisverteiler`, an dem `Board.razor` und `Kartendetail.razor` schon hängen.
- **Kernregel:** `KanbanC.Blazor` bekommt weiterhin keine Projektreferenz auf `KanbanC.BL`. Es entsteht kein neuer Leseweg neben den bestehenden HTTP-Abrufen.
- **Kein neues Paket** in keinem Projekt, keine Migration, kein Schema.
- **Kein Ereignisspeicher und keine Folgenummer** in der WebApi — beides bleibt tote Flexibilität (C24), so wie `R00031` es entschieden hat.
- **Gestaltung:** alle Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei.
- **Prüfbarkeit:** die Zusammenfassungsschwelle steht an einer Stelle, an der ein Test sie senken kann — wie `Markenstandzeit`. Die `WiederaufnahmepauseInSekunden` ist bereits so gebaut (s. u.).
- **Umlaute:** C07 gilt für Bezeichner, nicht für Anzeigetexte — die deutschen Zeilen des Trennungsdialogs bekommen echte Umlaute.

## Akzeptanzkriterien

### Fall 1 — die Sicht merkt, dass die Leitung weg ist

- [ ] Reißt die Ereignisleitung ab, erscheint in der Kopfzeile **jeder** offenen Sicht dieses Blazor-Prozesses die Marke „nicht live · Stand von HH:mm".
- [ ] Steht die Leitung, steht an dieser Stelle **nichts** — keine „live"-Marke, kein Nullwert, keine graue Attrappe.
- [ ] Die Marke nennt einen **Zeitpunkt** und keine mitzählende Dauer; sie steht nach fünf Minuten Trennung unverändert da.
- [ ] Der Zeitpunkt ist der des **Abrisses**. Rechenbeispiel: letztes Ereignis 07:10, Abriss 09:12 — die Marke sagt **09:12**, nicht 07:10.
- [ ] Kommt die Leitung zurück, verschwindet die Marke.
- [ ] Während der Trennung ist die Fläche **zurückgenommen und weiterhin lesbar** — Kartentitel, Bahnennamen und Zahlen bleiben erkennbar.
- [ ] Die Zurücknahme gilt dem **ganzen Schirm** (eine Klasse am Layout), nicht einer einzelnen Komponente.
- [ ] Der Verbindungsstand hängt am `Ereignisverteiler`; es entsteht **kein zweiter Dienst**, an dem sich Sichten anmelden.
- [ ] Eine Sicht, die verlassen wird, meldet sich ab — der Singleton hält keine toten Kreisläufe fest.

### Fall 1 — das Aufschließen

- [ ] Kommt die Leitung zurück, holt ein offenes Board seinen Stand über `LadeBoard` **frisch**; die Karten stehen danach dort, wo sie jetzt liegen, und die Bahnenzahlen stimmen.
- [ ] Es wird **nichts nachgespielt** — es gibt keinen Ereignisspeicher und keine Folgenummer.
- [ ] Ein Band über den Bahnen nennt Zahl und Zeitpunkt: „Wieder verbunden. 4 Änderungen seit 09:12 sind nachgeholt und unten markiert." mit einem „schließen".
- [ ] Die Zahl entsteht aus dem **Vergleich** des frisch geholten Bildes mit dem alten. Rechenbeispiel: „Bereit" trug A, B, C, D, danach trägt „Bereit" A, D und „In Arbeit" B, C — das sind **2** geänderte Karten (B und C), nicht 4 und nicht 0.
- [ ] Verglichen wird die **Lage** (Spalte und Position). Eine neu erschienene und eine verschwundene Karte zählen mit; eine unverändert liegende zählt nicht.
- [ ] **Null Änderungen ergeben kein Band** — die Sicht war getrennt, aber es ist nichts passiert.
- [ ] Jede geänderte Karte trägt eine **Nachholmarke** mit dem Wortlaut „geändert, während die Verbindung weg war" — **ohne Namen und ohne Uhrzeit**.
- [ ] Die Nachholmarke hat **keine Frist**: sie verschwindet nicht von selbst, sondern erst, wenn das Band geschlossen wird.
- [ ] „Schließen" räumt Band **und** Nachholmarken zusammen weg.
- [ ] Übersteigt die Zahl die Zusammenfassungsschwelle (**etwa zehn**, Größenordnung), erscheinen **keine** einzelnen Marken; das Band nennt nur die Zahl. Rechenbeispiel: 4 geänderte Karten → Band **und** 4 Marken; 25 geänderte Karten → Band ohne Marken.
- [ ] **Läuft gerade ein Zug**, wartet das Aufschließen bis zum Loslassen — dieselbe Regel wie für eine fremde Bewegung, kein zweiter Mechanismus.
- [ ] Eine geöffnete Kartenseite holt nach der Rückkehr ihr Kartendetail frisch; die Kopfzeile nennt die aktuelle Spalte.
- [ ] Die Kartenseite bekommt **kein Band und keine Zahl** — sie zeigt eine Karte, nicht einen Bestand.
- [ ] **Steht ein Feld offen**, wird nichts ausgetauscht: der ungesendete Text bleibt stehen, die Meldung wartet sichtbar und wird nach dem Schließen des Felds eingespielt.
- [ ] Die **erste** Verbindung nach dem Start der Anwendung meldet **keine** Rückkehr: eine frisch geöffnete Sicht schließt nicht gegen ein Bild auf, das sie nie hatte, und zeigt kein Band.
- [ ] Fällt die WebApi zwischen Rückkehr und Neuabruf wieder aus, landet der Fehler an derselben Stelle wie jeder andere (`WebApiAufruf.MitAusfallmeldung`) — keine Ausnahmeseite.

### Fall 2 — der abgerissene Browserkreislauf

- [ ] **Jede** Zeile des Trennungsdialogs ist deutsch — alle sieben Texte aus `ReconnectModal.razor`, einschließlich der beiden Knöpfe.
- [ ] Der Dialog nennt den Zeitpunkt, seit dem der Schirm alt ist: „Was du siehst, ist der Stand von 09:12."
- [ ] Der Zeitpunkt entsteht **im Browser**, weil der Server in diesem Fall nicht erreichbar ist.
- [ ] Die Element-Ids und die `components-*-visible`-Klassen bleiben **unverändert** — das Blazor-Laufzeitteil findet den Dialog über sie.
- [ ] Der Dialog erscheint weiterhin bei Trennung und verschwindet beim Wiederanschluss; „Erneut verbinden" tut, was „Retry" tat.
- [ ] Nach einem kurzen Abriss und Wiederanschluss zeigt die Seite den **aktuellen** Stand — der Kreislauf hat serverseitig weitergelebt.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Kein Ereignisjournal, kein Ereignisspeicher, keine Folgenummer** in der WebApi.
- [ ] **Kein Nachspielen** verpasster Ereignisse — nur ein frisches Holen mit Vergleich.
- [ ] **Kein Wer und kein Wann an der Nachholmarke** — der Zeitpunkt steht einmal im Band.
- [ ] **Keine Ereignisspur und kein Laufband** (`D0007.dc.html`, Rand C — ohne Knoten in der WBS).
- [ ] **Kein Angebot über einem offenen Feld** bei einer fremden **Feldänderung** — das gehört dem Slice, der Feldänderungen nachzieht, und den gibt es nicht.
- [ ] **Keine mitlaufende Dauer, die angehalten werden müsste** — im Bestand tickt nichts; `I0028` hat den Sekundentakt ausdrücklich einem eigenen Slice überlassen, den es nicht gibt.
- [ ] **Kein neuer Endpunkt, kein neues Contracts-DTO, keine Migration, kein neues Paket.**
- [ ] **Kein Nachziehen anderer Gegenstände** — Kartenänderungen, Kommentare, Etiketten, Teilaufgaben, Anhänge, Klassen, Farbe, Archivierung, Spalten und Boards, Zeitereignisse bleiben draußen wie in `R00031`.

### Der grüne Bestand bleibt grün

- [ ] Die **`R00031`-Suite** bleibt grün: die Einflugmarke, ihre Frist, die Warteregeln am Board und auf der Kartenseite und die Wiederaufnahme der Leitung verhalten sich unverändert.
- [ ] `Ereignisleitung` meldet weiterhin jedes gelesene Element an den Verteiler; das zweite Ereignis (Verbindungsstand) tritt **daneben**, nicht an seine Stelle.
- [ ] Die bestehende Ausfallmeldung schreibender Aufrufe (`WebApiAufruf.MitAusfallmeldung`) bleibt unverändert — während Fall 1 ist die Oberfläche **bedienbar**, und ein scheiternder Schreibaufruf meldet sich wie bisher.
- [ ] Alle bestehenden E2E-Tests bleiben grün, obwohl in jedem Lauf nun ein Verbindungsstand mitläuft.
- [ ] Die vorhandenen Konfigurationsschlüssel `Oberflaeche:MarkenstandzeitInSekunden` und `Oberflaeche:WiederaufnahmepauseInSekunden` behalten Namen und Bedeutung.

## Betroffene Verzeichnisstruktur

- **`Source/KanbanC.Blazor/Services/`** — die neuen Operationen der Oberflächenschicht neben `Einflugmarke`, `Laufplakette`, `Laufzaehler`, `Dauerform` und `Zeitpunktform`: `Verbindungsmarke`, `Standvergleich`, `Nachholmarke`, `Aufschliessschwelle`. `Ereignisverteiler` wächst um den Verbindungsstand, `Ereignisleitung` meldet ihn.
- **`Source/KanbanC.Blazor/Components/Layout/`** — `Kopfzeile.razor(.css)` (die Marke), `MainLayout.razor(.css)` (die ruhigere Fläche), `ReconnectModal.razor` und `ReconnectModal.razor.js` (Fall 2).
- **`Source/KanbanC.Blazor/Components/Pages/`** — `Board.razor(.css)` (Aufschließen, Band, Nachholmarken), `Kartendetail.razor` (Aufschließen mit der bestehenden Warteregel).
- **`Source/KanbanC.Blazor/Components/Karten/`** — **unberührt**: die Nachholmarke nutzt dieselbe Akzentkante und Fußzeile wie die Einflugmarke; die Karte bekommt kein zweites Aussehen.
- **`Source/KanbanC.Blazor/wwwroot/gestaltung.css`** — Token für Band und Zurücknahme der Fläche, falls die vorhandenen nicht reichen. `.washed` (`saturate(0.6) contrast(0.85) brightness(1.1) opacity(0.94)`) steht bereits dort und ist der nächste Vorgänger für die ruhigere Fläche.
- **`Source/KanbanC.Blazor/appsettings.json`** — ein Schlüssel unter `Oberflaeche` für die Zusammenfassungsschwelle, leer wie die zwei vorhandenen.
- **`Source/KanbanC.WebApi/`, `Source/KanbanC.BL/`, `Source/KanbanC.Contracts/`** — **unberührt**. Der Verbindungsstand verlässt den Blazor-Prozess nie und ist deshalb kein Vertrag zwischen zweien.
- **Tests** — `Source/KanbanC.Blazor.Tests/Services/` (Verbindungsmarke, Standvergleich, Nachholmarke, Schwelle, Verteiler- und Leitungsverhalten), `Source/KanbanC.PlaywrightTests/Tests/` und `PageObjects/` (die zwei E2E-Läufe).

## Technische Überlegungen

### Zwei Abbrüche, ein Trenner: wer noch rendern kann

Das Fertig-Kriterium sagt „eine unterbrochene Sicht" und lässt offen, welche Verbindung gemeint ist. Es sind zwei, und sie verhalten sich gegensätzlich:

| | Fall 1 · Blazor ↔ WebApi | Fall 2 · Browser ↔ Blazor |
|---|---|---|
| Was reißt | die Ereignisleitung (SSE) | der SignalR-Kreislauf |
| Merkt es der Server? | ja — `Ereignisleitung` fängt den Abriss | nein |
| Ist die Sicht bedienbar? | **ja** — schreibende Aufrufe scheitern und melden sich | **nein** — der Kreislauf *ist* die Bedienung |
| Kann der Server rendern? | ja | nein |
| Braucht es Aufschließen? | **ja** | **nein** — der Kreislauf lebt drei Minuten weiter und rendert beim Wiederanschluss den aktuellen Stand |
| Was wird gebaut | Kopfzeilenmarke, ruhigere Fläche, Aufschließen (`F0048`, `F0049`) | ein deutscher Dialog, der sein Alter nennt (`F0050`) |

**Daraus folgt die Zuordnung der Artboard-Stufen** (`D0007.dc.html`, Zustand 5): Stufe 1 („verbunden") und Stufe 2 („getrennt, verbindet neu") gehören Fall 1, Stufe 3 („endgültig gescheitert") gehört Fall 2. Sie sind **nicht** drei Stufen desselben Abbruchs. Wer sie so liest, baut eine Kopfzeilenmarke für einen Zustand, in dem der Server nichts mehr zeichnen kann.

**Eine Abweichung vom Artboard, die daraus folgt:** die Lesehilfe zu Zustand 5 schreibt „Bedienbar bleibt bei Blazor Server ohnehin nichts". Für Fall 2 stimmt das; für Fall 1 nicht — dort läuft der Blazor-Prozess weiter, und der Mensch kann Karten verschieben, während die Ereignisleitung weg ist. Der Satz gilt der dritten Stufe, nicht allen dreien.

### Frisch holen, nicht nachspielen — und woher die Zahl kommt

Das Fertig-Kriterium sagt „holt den verpassten **Stand** nach", nicht „die Ereignisse". `R00031` hat Ereignisspeicher und Folgenummer ausdrücklich als tote Flexibilität verworfen; im Bestand gibt es kein Ereignisjournal. **Nachgespielt werden kann also nichts, und es soll auch nicht.**

Geholt wird über den **bestehenden Ladeweg jeder offenen Sicht** — `Board.razor.LadeBoard` (`GET /api/boards/{boardId}`) und der Weg, den `Kartendetail.razor` in `OnParametersSetAsync` ohnehin geht (`GET /api/karten/{karteId}`). Kein zweiter Leseweg, genau wie bei `R00031`.

**Der scheinbare Widerspruch des Bandes — „4 Änderungen nachgeholt" ohne Ereignisse — löst sich über die Herkunft der Zahl:** sie kommt aus einem **Vergleich des frisch geholten Bildes mit dem alten**, das die Sicht noch in der Hand hält. Das alte `_board` wird **vor** dem Neuabruf festgehalten, sonst gibt es nichts zu vergleichen. Muster liegt bereit: `.claude/app-architectures/Common/snippets/SollIstVergleich.md`. Verglichen wird die **Lage** — Spalte und Position —, weil das Fertig-Kriterium des Dialogs die Bewegung meint.

### Der Preis des Vergleichs, ausdrücklich benannt — und die einzige bewusste Abweichung vom Artboard

Ein Vergleich weiß, **was** sich geändert hat. Er weiß **nicht, wer** es war und **wann**.

Das Artboard (`D0007.dc.html`, Zustand 6) zeichnet an den nachgeholten Karten Fußzeilen wie „Claude-Agent · über die API · 09:31" und „Nina Barth · 09:24". **Diese Zeilen sind aus einem Vergleich nicht ableitbar.** Sie bräuchten genau das Ereignisjournal, das `I0028` verworfen hat — und ein Journal einzuführen, damit eine Fußzeile stimmt, wäre der teuerste denkbare Weg zu einer Randangabe.

**Gebaut wird deshalb eine Nachholmarke ohne Wer und ohne Wann:** dieselbe Akzentkante und Fußzeile wie die `Einflugmarke`, aber mit dem Wortlaut „geändert, während die Verbindung weg war". **Der Zeitpunkt steht einmal im Band** („seit 09:12"), wo er stimmt, statt N-mal an den Karten, wo er geraten wäre. Das ist die **einzige bewusste Abweichung vom Artboard** in diesem Slice, und sie ist eine Entscheidung gegen eine erfundene Genauigkeit, kein Versäumnis.

**Die Marke hat keine Frist** — als einzige im Projekt. Nach einer Trennung ist sie kein Zuruf mehr, sondern das Protokoll der Lücke, und das darf man in Ruhe lesen; sie geht mit dem Band.

### Kein zweiter Singleton: der Verbindungsstand hängt am Verteiler

`Ereignisverteiler` ist die Stelle, an der jede Sicht schon hängt (`Board.razor`, `Kartendetail.razor`, beide mit Abmeldung in `Dispose`). Er bekommt neben `Gemeldet` ein zweites Ereignis `Verbindungsstandgewechselt` und eine Eigenschaft `Verbindungsstand`. Ein eigener Dienst wäre ein **zweiter Anmeldeort für dieselbe Leitung** — und damit eine zweite Abmeldedisziplin, die jemand vergisst.

`Ereignisleitung.Lausche` weiß den Stand bereits: `EnsureSuccessStatusCode` bestanden heißt „verbunden", der `catch`-Zweig in `VersucheZuLauschen` heißt „getrennt". Es kommt kein neuer Mechanismus dazu, nur zwei Meldungen an einer Stelle, die es schon gibt. **Dieselbe Fadengrenze wie bei `Gemeldet`:** gemeldet wird aus dem Hintergrunddienst, jeder Hörer muss über `InvokeAsync` in den Renderfaden zurück.

**Die erste Verbindung nach dem Start meldet keine Rückkehr.** Sonst schlösse jede frisch geöffnete Sicht gegen ein Bild auf, das sie nie hatte, und zeigte ein Band über Änderungen, die sie nie hätte sehen können.

### „Stand von 09:12" ist der Zeitpunkt des Abrisses

Nicht der des letzten empfangenen Ereignisses. Ein Board, auf dem sich zwei Stunden nichts bewegt hat, wäre sonst zwei Stunden „alt" — obwohl es aktuell ist. Der Stand trägt den Abrisszeitpunkt mit, damit die Marke ihn nennen kann; die Ortszeitform kommt aus `Zeitpunktform.AlsTageszeit`, nicht neu gerechnet.

**Ein Zeitpunkt statt einer Sekundenzählung**, weil eine ohne Verbindung weiterlaufende Zahl die eine ist, die sicher falsch wäre — dieselbe Begründung, mit der `Laufplakette` und `B0335` schon keine Dauer zeigen.

### Die Warteregeln werden wiederverwendet, nicht neu gebaut

An zwei Stellen wäre ein Austausch ein Schaden, und beide haben ihre Antwort schon:

- **Am Board:** `Board.razor` führt `_einZugLaeuft` und `_zurueckgehalteneEreignisse` (aus `B0379`). Das Aufschließen ist ein **zweiter Anlass für dieselbe Regel** — läuft ein Zug, wartet es bis zum Loslassen. Kein zweiter Mechanismus.
- **Auf der Kartenseite:** `Kartendetail.razor` führt `_offenesFeld` und `_zurueckgehaltenesEreignis` (aus `B0388`). Auch hier nur ein zweiter Anlass. Der ungesendete Text überlebt die Trennung — „ein Aufschließen, das ein offenes Feld überschreibt, wäre die teuerste Art, recht zu haben".

### Die Zusammenfassungsschwelle — eine gesetzte Zahl

Ab **etwa zehn** nachgeholten Änderungen erscheinen keine einzelnen Marken mehr; das Band nennt nur die Zahl. Das ist eine **Größenordnung und kein Messwert**, genau wie „etwa zehn Sekunden" bei der `Markenstandzeit`. Die Begründung: ist die halbe Bahn markiert, ist die Marke keine Auskunft mehr, sondern Tapete — und die Zahl im Band sagt dasselbe kürzer.

Die Schwelle steht wie `Markenstandzeit` an **einer** Stelle mit einem Konfigurationsschlüssel, damit ein Test sie senken kann, statt als Literal im Renderzweig. Damit ist auch die offene Frage 26 des Wireframe-Index beantwortet („wie viele Änderungen ein Aufschließen einzeln markiert, bevor die Sicht sie zusammenfasst").

**Nachgesehen statt vermutet:** die `WiederaufnahmepauseInSekunden`, die ein E2E-Lauf senken muss, damit er nicht zwei Sekunden je Versuch wartet, ist **bereits so gebaut** — `Source/KanbanC.PlaywrightTests/Testumgebung.cs:20` setzt sie auf `0.2` und reicht sie als `Oberflaeche__WiederaufnahmepauseInSekunden` in beide Prozesse. Hier ist nichts zu bauen; die Schwelle wird nach demselben Muster ergänzt.

### Fall 2: die Zeile entsteht im Browser

Der Server ist in diesem Fall weg — die Zeile „Was du siehst, ist der Stand von 09:12." kann nicht gerendert werden. Sie muss in `ReconnectModal.razor.js` entstehen: das Skript hält den Zeitpunkt fest, an dem die Trennung beginnt (`components-reconnect-state-changed` mit `state === "show"`), und schreibt ihn in die Zeile. Es führt schon den Countdown bis zum nächsten Versuch und ist damit die Stelle, die es weiß. Skill `javascript-stil` vor dem Schreiben.

**Die Element-Ids bleiben unangetastet** — `components-reconnect-modal`, `components-reconnect-button`, `components-resume-button`, `components-seconds-to-next-attempt` und die `components-*-visible`-Klassen. Das Blazor-Laufzeitteil findet den Dialog über sie; ein deutscher Bezeichner bräche ihn.

**Kleiner Nebeneffekt, benannt:** die Uhrzeit in Fall 1 ist Serverzeit (Blazor Server, siehe Kommentar in `Zeitpunktform`), die in Fall 2 Browserzeit. Im LAN-Betrieb auf einer Maschine ist das derselbe Wert; auf zwei Maschinen in verschiedenen Zeitzonen wären es zwei. Das zu vereinheitlichen kostete einen Interop-Aufruf und ist hier nicht geplant.

### Gestaltungsvorgabe

- **Marke** in der Kopfzeile: Plakette links vom Laufzeitplatz, Kontur in `--color-accent-700`, Muster der bestehenden `kopfzeile-laufzeit`. Anmeldung beim Aufbau, **Abmeldung in `Dispose`** — die Kopfzeile führt ihre Abmeldungen bereits (`Navigation.LocationChanged`, `Identitaetsspeicher.Gewechselt`, `Laufzeitmelder.Gemeldet`).
- **Ruhigere Fläche:** eine Klasse am Layout, nicht je Komponente — die Aussage gilt dem ganzen Schirm. `gestaltung.css` trägt mit `.washed` bereits eine Zurücknahme über **Sättigung, Kontrast und Helligkeit** bei nahezu voller Deckkraft; das ist der nächste Vorgänger und die Antwort auf die Frage „Sättigung oder Deckkraft".
- **Band** über den Bahnen, **nicht** in der Kopfzeile: die Kopfzeile steht auf jeder Seite, das Band gehört dem Board.
- **Nachholmarke:** dieselbe Akzentkante und Fußzeile wie die `Einflugmarke`; `Karte.razor(.css)` bleibt unverändert.
- Alle Werte aus `gestaltung.css`, kein Literal in einer Komponenten-CSS-Datei.

### Ablauf

1. **Die Leitung reißt**
   - 1.1 `Ereignisleitung.VersucheZuLauschen` fängt den Abriss → `Ereignisverteiler.MeldeGetrennt(jetzt)`
   - 1.2 `Ereignisverteiler` merkt sich `Getrennt(seit)` und meldet `Verbindungsstandgewechselt`
2. **Jede offene Sicht reagiert**
   - 2.1 `Kopfzeile.razor` → `InvokeAsync` → `Verbindungsmarke.Fuer(stand, jetzt)` → „nicht live · Stand von 09:12"
   - 2.2 `MainLayout.razor` → Klasse am Rumpf, die die Fläche zurücknimmt
   - 2.3 `Board.razor` und `Kartendetail.razor` tun **nichts** — sie bleiben bedienbar
3. **Die Leitung kommt zurück**
   - 3.1 `Ereignisleitung.Lausche` besteht `EnsureSuccessStatusCode` → `Ereignisverteiler.MeldeVerbunden()`
   - 3.2 War es die **erste** Verbindung nach dem Start: nur Stand setzen, **keine Rückkehr melden**
4. **Das offene Board schließt auf**
   - 4.1 Läuft gerade ein Zug (`_einZugLaeuft`): zurückhalten, beim Loslassen weiter bei 4.2
   - 4.2 altes `_board` festhalten → `LadeBoard()` (über die bestehende Ausfallmeldung)
   - 4.3 `Standvergleich.Geaenderte(alt, neu)` → KarteIds und Zahl
   - 4.4 Zahl `0`: **kein Band, keine Marken**, fertig
   - 4.5 Zahl über der Schwelle: **nur** das Band mit der Zahl
   - 4.6 sonst: Band **und** je geänderter Karte eine `Nachholmarke` **ohne Frist**
   - 4.7 „schließen" räumt Band und Marken zusammen weg
5. **Die offene Kartenseite schließt auf**
   - 5.1 Steht ein Feld offen (`_offenesFeld`): zurückhalten, beim Schließen weiter bei 5.2
   - 5.2 Kartendetail frisch holen → neue Spalte in der Kopfzeile; **kein Band, keine Zahl**
6. **Fall 2 — der Kreislauf reißt (unabhängig von 1 bis 5)**
   - 6.1 Blazor löst `components-reconnect-state-changed` mit `state === "show"` aus
   - 6.2 `ReconnectModal.razor.js` hält den Zeitpunkt fest und schreibt „Was du siehst, ist der Stand von 09:12."
   - 6.3 Der Dialog zeigt seine deutschen Zeilen; „Erneut verbinden" ruft wie bisher `Blazor.reconnect()`

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** `Ereignisleitung` (`Services/Ereignisleitung.cs`) — die eine Stelle, an der ein Abriss und eine Rückkehr überhaupt bekannt sind; `Ereignisverteiler` als der Ort, an dem alle Sichten schon hängen; `Kopfzeile.razor` und `MainLayout.razor` für Fall 1 sichtbar; `Board.razor` und `Kartendetail.razor` für das Aufschließen; `ReconnectModal.razor(.js)` für Fall 2. **Kein neuer Schirm, keine neue Seite, kein neuer Dienst, keine Route, keine Migration.**

**In `KanbanC.Blazor/Services` (neu):**
- `Verbindungsstand` (DTO, immutable, C08) — verbunden oder getrennt seit einem Zeitpunkt.
  - `static Verbindungsstand Verbunden()` · `static Verbindungsstand Getrennt(DateTimeOffset seit)`
- `Verbindungsmarke` (Operation, pure) — rechnet die Beschriftung der Kopfzeile; `null` heißt „keine Marke", dieselbe Form wie `Laufzaehler.Fuer`.
  - `static string? Fuer(Verbindungsstand stand)`
- `Standvergleich` (Operation, pure, ohne Uhr) — welche Karten zwischen altem und neuem Board ihre Lage geändert haben, samt Zahl.
  - `static Standunterschied Geaenderte(Board alt, Board neu)`
- `Standunterschied` (DTO, immutable) — die geänderten `KarteId`s und ihre Zahl.
- `Nachholmarke` (Operation, pure) — der Wortlaut „geändert, während die Verbindung weg war", ohne Wer und ohne Wann.
- `Aufschliessschwelle` (DTO, immutable, Muster `Markenstandzeit`) — ab wie vielen Änderungen nur noch gezählt wird; `Vorgabe = 10`, gelesen aus `Oberflaeche:AufschliessschwelleInAenderungen`.
  - `static Aufschliessschwelle Aus(string? anzahl)`

**In `KanbanC.Blazor/Services` (geändert):**
- `Ereignisverteiler` — neben `Gemeldet` ein `Verbindungsstandgewechselt` und eine Eigenschaft `Verbindungsstand`.
  - `event Action<Verbindungsstand>? Verbindungsstandgewechselt`
  - `void MeldeVerbunden()` · `void MeldeGetrennt(DateTimeOffset seit)`
- `Ereignisleitung` — meldet Abriss und Rückkehr; die **erste** Verbindung nach dem Start meldet keine Rückkehr. Die Uhr wird hereingereicht, Muster `Einflugmarke.Fuer`.

**Kein Interface** für die neuen Bauteile: es gibt je Aufgabe genau eine Implementation (C25). **Keine** neue Klasse in `KanbanC.BL`, `KanbanC.WebApi` oder `KanbanC.Contracts`.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Services/Ereignisverteiler` | zweites Ereignis und Eigenschaft für den Verbindungsstand; `Gemeldet` bleibt unverändert |
| `Services/Ereignisleitung` | meldet `MeldeVerbunden` / `MeldeGetrennt`; erste Verbindung nach dem Start ohne Rückkehrmeldung |
| `Components/Layout/Kopfzeile.razor(.css)` | meldet sich beim Verteiler an und in `Dispose` ab, rendert die `Verbindungsmarke` links vom Laufzeitplatz |
| `Components/Layout/MainLayout.razor(.css)` | Klasse am Rumpf, die bei `Getrennt` die Fläche zurücknimmt |
| `Components/Layout/ReconnectModal.razor` | alle sieben Texte deutsch; Ids und `components-*-visible`-Klassen unangetastet |
| `Components/Layout/ReconnectModal.razor.js` | hält den Trennungszeitpunkt fest und schreibt „Was du siehst, ist der Stand von HH:mm." |
| `Components/Pages/Board.razor(.css)` | hört auf den Verbindungsstand, hält das alte `_board` fest, schließt auf, führt Band und Nachholmarken; nutzt `_einZugLaeuft` als Warteregel |
| `Components/Pages/Kartendetail.razor` | hört auf den Verbindungsstand, holt frisch; nutzt `_offenesFeld` als Warteregel; **kein Band** |
| `Program.cs` (Blazor) | registriert `Aufschliessschwelle` wie `Markenstandzeit` |
| `appsettings.json` (Blazor) | Schlüssel `Oberflaeche:AufschliessschwelleInAenderungen`, leer |
| `PlaywrightTests/Testumgebung.cs` | setzt die Schwelle herunter, wie sie Markenstandzeit und Wiederaufnahmepause schon setzt |

**Nicht geändert:** `KanbanC.BL`, `KanbanC.WebApi` und `KanbanC.Contracts` in Gänze; `Components/Karten/Karte.razor(.css)` (die Nachholmarke nutzt die vorhandene Form); `Einflugmarke` und `Markenstandzeit` (die Einflugmarke behält ihre Frist).

## Tests

Nach Skill `test-pyramide`, jeder Test nach Skill `test-ehrlichkeit`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.Blazor.Tests`):**
- `Verbindungsmarke` — verbunden ergibt `null`; getrennt ergibt „nicht live · Stand von HH:mm"; der genannte Zeitpunkt ist der Abrisszeitpunkt und wächst nicht mit „jetzt".
- `Standvergleich` — Spaltenwechsel zählt; Positionswechsel innerhalb einer Bahn zählt; unverändert liegende Karte zählt nicht; neu erschienene und verschwundene Karte zählen mit; gleiches Board gegen sich selbst ergibt **0**. Rein rechnend und ohne Uhr, damit er ohne Kreislauf prüfbar ist.
- `Nachholmarke` — der Wortlaut trägt weder Namen noch Uhrzeit.
- `Aufschliessschwelle` — leerer, unlesbarer und nicht positiver Wert ergeben die Vorgabe 10; ein gesetzter Wert gilt.
- `Ereignisverteiler` — Anmelden, Verbindungsstand melden, Abmelden; ein abgemeldeter Hörer bekommt nichts mehr; `Gemeldet` und `Verbindungsstandgewechselt` stören einander nicht.
- `Ereignisleitung` — ein Abriss meldet `Getrennt` **und** führt zum nächsten Versuch; die **erste** Verbindung nach dem Start meldet **keine** Rückkehr, die zweite schon. Diese Pfade sind über den Browser nicht auslösbar — genau der Grund, aus dem `KanbanC.Blazor.Tests` existiert (Testhilfe `TestEreignisstrom` steht bereit).

**Integration:** **keine.** Dieser Slice ändert die WebApi nicht; es entsteht kein Endpunkt, kein Vertrag und keine Migration. Das ist ein Befund, kein Versäumnis — und der Grund, aus dem die Test-Pyramide hier auf zwei Ebenen steht.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** zwei Läufe.

1. **Aufschließen nach dem Abriss (Fall 1).** Board offen; die WebApi wird **angehalten** — so, wie `B0026` es für die Ausfallmeldung schon tut. Erwartet: die Kopfzeilenmarke „nicht live" erscheint. Während der Trennung wird eine Karte über einen `HttpClient` gegen die **neu gestartete** WebApi bewegt (kein zweiter Browser nötig). Erwartet nach der Rückkehr: Marke weg, Band mit der Zahl und dem Zeitpunkt, Karte an der neuen Stelle und markiert; „schließen" räumt Band und Marke weg. Die `WiederaufnahmepauseInSekunden` ist bereits gesenkt (`Testumgebung.cs`), es wird **auf Zustände gewartet, nie auf feste Pausen**.
2. **Der Trennungsdialog (Fall 2).** **Der erste Test überhaupt auf `ReconnectModal.razor`.** Der Abbruch wird über die Browserseite erzeugt (`Context.SetOfflineAsync`) und **nicht** durch Anhalten des Blazor-Prozesses — nur so bleibt der Kreislauf auf dem Server am Leben und die Vorlage zeigt ihren Dialog. Erwartet: sichtbarer Dialog, deutsche Zeilen, genannter Zeitpunkt.

Die Locator wachsen in `BoardSeite`; ein neues Seitenobjekt entsteht nicht. Für die Kopfzeilenmarke und den Trennungsdialog kommt je ein Locator dazu.

**Repositories, DAL-Klassen und alles mit Datenbank-Abhängigkeit sind keine Unit-Test-Kandidaten** — hier entsteht davon nichts.

## Abhängigkeiten

- Abhängig von: **`R00031`** (Änderung ohne Reload sehen — `I0028`, **grün**). Das ist genau der eine Knoten der WBS-Spalte `Braucht`; er ist erfüllt, der Slice ist **frei**. Ohne eine Leitung, die abreißen kann, gibt es nichts aufzuschließen — und `Ereignisleitung`, `Ereignisverteiler`, `Einflugmarke`, `Markenstandzeit` und die zwei Warteregeln stammen von dort.
- Setzt außerdem auf: **`R00003`** (`LadeBoard` und die Boardseite), **`R00008`** (`_einZugLaeuft` als Anker der Warteregel), **`R00009`** (`Bahnenkopfzahl`), **`R00017`** (Kartenseite und `_offenesFeld`), **`R00005`** (`gestaltung.css` samt `.washed`), **`R00030`** (`Laufzaehler` und der Kopfzeilenplatz, neben dem die Marke sitzt). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **nichts.** Kein Knoten der WBS führt `I0029` in seiner Spalte `Braucht`.
- **`D0007` wird mit diesem Slice grün** — `I0029` ist der letzte offene Knoten des Dialogs.

## Umfang

```
Nach Verbindungsabbruch aufschließen (I0029) = 15 Bubbles: 9 Standard (10,0h), 6 unklar (5,6–14,0h).
Rest: 10,0h klar + 5,6–14,0h unklar · 0 von 15 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 15 Bubbles gruen (0 %) · 0 laufen · 15 offen
```

`I0029` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **drei Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0048` Die Sicht merkt, dass die Leitung weg ist | `B0390`–`B0394` (5) | 3 (2,8h) | 2 (0,8–3,0h) | — |
| `F0049` Das Aufschließen | `B0395`–`B0401` (7) | 5 (6,8h) | 2 (2,4–5,5h) | `F0048` |
| `F0050` Der Trennungsdialog spricht deutsch | `B0402`–`B0404` (3) | 1 (0,4h) | 2 (2,4–5,5h) | — |

**Warum drei Features:** weil drei Aspekte **getrennt fertig** werden. `F0048` ist ohne das Aufschließen prüfbar — die Marke erscheint, die Fläche wird ruhig, das genügt. `F0050` hat **gar keine Vorbedingung**: andere Systemgrenze, andere Datei, kein gemeinsamer Code; es ist parallel zu `F0048`/`F0049` machbar. Nur `F0049` braucht `F0048`, weil ohne den Verbindungsstand niemand weiß, wann aufzuschließen ist.

| Bubble | Art | Aufwand |
|---|---|---|
| `B0390` Der Verbindungsstand am Verteiler | Integration (Blazor) | 0,4h (Richtwert) |
| `B0391` Die Leitung meldet Abriss und Rückkehr | Integration (Blazor) | 0,4–1,5h (**unklar**) |
| `B0392` Die Verbindungsmarke rechnen | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0393` Die Marke in der Kopfzeile | UI | 2h (Richtwert) |
| `B0394` Der getrennte Schirm wird ruhiger | UI | 0,4–1,5h (**unklar**) |
| `B0395` Der Standvergleich | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0396` Das Board schließt auf | UI | 2h (Richtwert) |
| `B0397` Die Nachholmarke | UI | 0,4–1,5h (**unklar**) |
| `B0398` Das Aufschließband | UI | 2h (Richtwert) |
| `B0399` Ab wann nur noch gezählt wird | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0400` Die offene Kartenseite schließt auf | UI | 2h (Richtwert) |
| `B0401` E2E Aufschließen | E2E | 2–4h (**unklar**) |
| `B0402` Der Trennungsdialog spricht deutsch | UI | 0,4h (Richtwert) |
| `B0403` Der Dialog nennt sein Alter | JavaScript | 0,4–1,5h (**unklar**) |
| `B0404` E2E Der Trennungsdialog | E2E | 2–4h (**unklar**) |

Mit **15 Bubbles** ist das ein mittelgroßer Slice — deutlich unter `I0028` (20) und `I0015` (30), über `I0023` und `I0027` (je 11). **Sechs der fünfzehn tragen eine Bandbreite**, also deutlich weniger als der halbe Anteil von `I0028`, und mit Grund: **die Technik ist diesmal vorgemacht.** Der Ereignisstrom läuft, der Verteiler steht, die Warteregeln sind gebaut, die Markenform ist da. Neu und ohne Vorbild sind nur drei Stellen: die Zurücknahme der Fläche (Sättigung oder Deckkraft), das Skript im Trennungsdialog und ein E2E-Lauf, der einen Kreislauf-Abriss im Browser erzeugt.

Derselbe Vermerk wie bei allen Slices seit `I0005`: die 2h-Richtwerte für UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Die Requirement-Klammer sitzt an `I0029` und an allen drei Features** — dieselbe Form wie bei `R00017`/`I0015` und `R00031`/`I0028`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Unterscheidet `I0029` zwei Abbrüche?** — **entschieden: ja, und der Trenner ist „wer noch rendern kann".** Fall 1 (Blazor ↔ WebApi) lässt die Sicht bedienbar und braucht Marke und Aufschließen; Fall 2 (Browser ↔ Blazor) lässt nichts bedienbar und braucht **kein** Aufschließen, weil der Kreislauf drei Minuten weiterlebt (nachgesehen: `Program.cs` setzt keine `CircuitOptions`). **Die Kopfzeilenmarke gehört nur zu Fall 1** — auf einem abgerissenen Kreislauf erreichte eine gerenderte Marke den Browser nie. Damit ist die in der WBS zu `I0028` offen gelassene Frage beantwortet. **Nicht am Menschen geprüft.**
- **Aufschließen: nachspielen oder frisch holen?** — **entschieden: frisch holen**, über den bestehenden Ladeweg jeder Sicht. Es gibt kein Ereignisjournal, und `I0028` hat Speicher und Folgenummer als tote Flexibilität verworfen. **Nicht am Menschen geprüft.**
- **Woher kommt die Zahl „4 Änderungen"?** — **entschieden: aus dem Vergleich** des frisch geholten Bildes mit dem alten, nicht aus Ereignissen. **Der Preis, benannt:** ein Vergleich kennt kein Wer und kein Wann. **Folge: die Nachholmarke trägt weder Namen noch Uhrzeit** — das ist die **einzige bewusste Abweichung vom Artboard** (`D0007.dc.html`, Zustand 6 zeichnet „Claude-Agent · über die API · 09:31"). Der Zeitpunkt steht einmal im Band, wo er stimmt. **Die Umkehrung** wäre ein Ereignisjournal, eingeführt für eine Fußzeile. **Nicht am Menschen geprüft.**
- **Meint „Stand von 09:12" den Abriss oder das letzte Ereignis?** — **entschieden: den Abriss.** Ein Board, auf dem sich zwei Stunden nichts bewegt, wäre sonst zwei Stunden alt, obwohl es aktuell ist. **Nicht am Menschen geprüft.**
- **Wo hängt der Verbindungsstand?** — **entschieden: am bestehenden `Ereignisverteiler`**, nicht an einem neuen Dienst. Ein zweiter Singleton wäre ein zweiter Anmeldeort für dieselbe Leitung. **Nicht am Menschen geprüft.**
- **Ab wie vielen Änderungen wird nur noch gezählt?** — **entschieden: etwa zehn, als Größenordnung** und mit einem Konfigurationsschlüssel, den ein Test senken kann. Ist die halbe Bahn markiert, ist die Marke Tapete. **Nicht am Menschen geprüft.**
- **Gehört die Umschrift des `ReconnectModal` in diesen Slice?** — **entschieden: ja.** Es ist der einzige Ort, an dem Fall 2 sichtbar wird, und die eine Zeile, die das Artboard über die Vorlage hinaus zeichnet, ist Inhalt genau dieses Slice; die Datei wird ohnehin geöffnet. **Nebenbefund:** sie wird heute von keinem Test berührt — `B0404` bringt den ersten. **Nicht am Menschen geprüft.**
- **Meldet die erste Verbindung nach dem Start eine Rückkehr?** — **entschieden: nein.** Sonst schlösse jede frisch geöffnete Sicht gegen ein Bild auf, das sie nie hatte. **Nicht am Menschen geprüft.**
- **Was gilt als „verbunden" — die Antwortkopfzeilen oder das erste gelesene Element?** — **offen und bewusst nicht entschieden** (`B0391`). Die Kopfzeilen sind der früheste ehrliche Zeitpunkt; ein Strom, der sofort wieder reißt, fände dann aber zweimal statt. Gehört in die Umsetzung.
- **Läuft die Zurücknahme der Fläche über Sättigung oder über Deckkraft?** — **offen** (`B0394`). Deckkraft nimmt auch dem Text die Lesbarkeit, die der Entwurf ausdrücklich erhalten will. **Hinweis aus dem Bestand:** `gestaltung.css` trägt mit `.washed` bereits eine Zurücknahme über Sättigung, Kontrast und Helligkeit bei nahezu voller Deckkraft — das spricht für Sättigung, entscheidet es aber nicht.
- **Reißt `Context.SetOfflineAsync` eine schon offene WebSocket-Verbindung wirklich?** — **offen** (`B0404`). Bis das gezeigt ist, ist es eine Annahme und kein Messwert. Sonst bleibt das Abweisen der `_blazor`-Route als Weg.
- **Koexistieren Nachholmarke und frische Einflugmarke an derselben Karte?** — **offen** (`B0397`). Eine Karte, die während der Trennung bewegt wurde und unmittelbar nach der Rückkehr noch einmal, trüge beide. Beide führen, die jüngere gewinnen lassen oder die Nachholmarke vorziehen — eine Frage der Anzeige, keine der Fachlichkeit; gehört in die Umsetzung.

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration.

## Manuelle Nachbereitungstätigkeiten

- Keine. `Oberflaeche:AufschliessschwelleInAenderungen` bleibt im Betrieb leer und nimmt damit die Vorgabe; der Schlüssel steht in `appsettings.json`, damit man ihn findet.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Lücke, die `R00031` selbst benannt und offen gelassen hat: die Ereignisleitung hat **kein Gedächtnis**, und was während einer Trennung geschieht, ist weg, ohne dass jemand darauf hingewiesen wird. Das wiegt schwerer als es klingt, weil ein Live-Kanal seinen Benutzer erzieht, nicht mehr nachzuladen — eine stille Trennung kostet danach genau das, was sie vorher nicht gekostet hätte. Die Kausalkette: **wenn** die Sicht den Abriss überhaupt bemerkt und ihn in der Kopfzeile mit dem Zeitpunkt benennt, und **wenn** sie bei der Rückkehr ihren Stand über den vorhandenen Ladeweg frisch holt und ihn gegen das alte Bild vergleicht (X), **dann** weiß der Mensch in jedem Moment, ob er ein aktuelles oder ein alterndes Bild vor sich hat, und sieht nach der Rückkehr, wie viel und was sich in der Lücke bewegt hat (Y), **und dann** hält der Live-Kanal seine Zusage auch über eine Störung hinweg, statt sie nur solange zu halten, wie nichts passiert (Z). **Der Hebel liegt beim Bemerken und beim Vergleich, nicht beim Nachspielen:** ein Ereignisjournal, das die verpassten Bewegungen einzeln nachreichte, wäre die vorgelagerte Änderung — es kostete Speicher, Folgenummern und eine zweite Wahrheit darüber, wie eine Bahn zustande kommt, und lieferte als einzigen Mehrwert eine Fußzeile mit Wer und Wann. **Und er liegt nicht bei einer Vereinheitlichung der zwei Abbrüche:** wer sie in eine Anzeige zwänge, baute eine Kopfzeilenmarke für einen Zustand, in dem der Server nichts mehr zeichnen kann — die Trennung ist deshalb keine Verkomplizierung, sondern der Grund, warum beide Fälle überhaupt korrekt behandelbar sind.

## Missing-Docs

- **Aufbewahrung und Wiederanschluss eines Blazor-Server-Kreislaufs:** dass ein getrennter Kreislauf ohne gesetzte `CircuitOptions` drei Minuten aufbewahrt wird, weiterrendert und beim Wiederanschluss den aktuellen Stand ausliefert, ist der Angelpunkt der Entscheidung „Fall 2 braucht kein Aufschließen". Im Repository ist es nirgends belegt, und die Vorgabewerte von `DisconnectedCircuitRetentionPeriod` und `DisconnectedCircuitMaxRetained` stehen in keiner Projektdokumentation.
- **`components-reconnect-state-changed`:** welche Zustände das Blazor-Laufzeitteil sendet (`show`, `hide`, `failed`, `rejected`), in welcher Reihenfolge und ob `show` genau einmal je Trennung kommt, steht nur implizit im Vorlagenskript. Der Trennungszeitpunkt hängt daran.
- **Ein Kreislauf-Abriss im Playwright-Test:** ob `BrowserContext.SetOfflineAsync` eine bereits offene WebSocket-Verbindung reißt oder nur neue Verbindungen unterbindet, ist nicht belegt und entscheidet, ob `B0404` so baubar ist.
- **Zwei Ereignisse an einem Singleton über Kreisläufe hinweg:** `Ereignisverteiler` bekommt ein zweites Ereignis; wie sich Nebenläufigkeit und Abmeldung bei zwei Hörerlisten in Blazor Server verhalten, ist im Projekt nur einmal (mit einem Ereignis) vorgemacht.

## Notizen

### Verworfene Alternativen

- **Beide Abbrüche in eine Anzeige zwingen** — auf einem abgerissenen Kreislauf kann der Server nichts zeichnen; eine gemeinsame Marke erreichte den Browser in Fall 2 nie.
- **Ein Ereignisjournal in der WebApi mit Folgenummer und Nachspielen** — tote Flexibilität (C24), von `I0028` bereits verworfen; es kostete Speicher und eine zweite Art, wie eine Bahn entsteht, und lieferte als Mehrwert eine Fußzeile.
- **Die Nachholmarke mit Wer und Wann zeichnen, wie das Artboard sie zeigt** — aus einem Vergleich nicht ableitbar; sie wäre geraten. Der Zeitpunkt steht stattdessen einmal im Band, wo er stimmt.
- **Den Zeitpunkt N-mal an die Karten schreiben statt einmal ins Band** — dieselbe Angabe an vielen Orten, an denen sie nicht bekannt ist.
- **Ein eigener Verbindungsdienst als zweiter Singleton** — ein zweiter Anmeldeort für dieselbe Leitung und eine zweite Abmeldedisziplin, die jemand vergisst.
- **Eine mitzählende Sekundendauer in der Marke** — eine ohne Verbindung weiterlaufende Zahl ist die eine, die sicher falsch ist; dieselbe Begründung wie bei `Laufplakette` und `B0335`.
- **Den Zeitpunkt des letzten empfangenen Ereignisses nennen** — ein Board, auf dem sich zwei Stunden nichts bewegt, wäre zwei Stunden „alt", obwohl es aktuell ist.
- **Eine dauerhafte Marke „● live"** (so im älteren Wireframe-Satz, `wireframes.js`, `liveMarke`) — der knappste Platz der Anwendung gehört dem, was gerade gilt; Abwesenheit heißt „es steht".
- **Die Fläche unlesbar machen statt sie zurückzunehmen** — wer liest, was dasteht, soll weiterlesen dürfen; der getrennte Schirm ist nicht wertlos, er ist nur alt.
- **Eine Klasse je Komponente statt einer am Layout** — die Aussage „dieser Schirm altert" gilt dem ganzen Schirm.
- **Eine eigene Warteregel für das Aufschließen** — `_einZugLaeuft` und `_offenesFeld` stehen; das Aufschließen ist ein zweiter Anlass für dieselbe Regel, kein zweiter Mechanismus.
- **Auch bei null Änderungen ein Band zeigen** — es gäbe nichts zu sagen; ein Band ohne Inhalt wäre eine Meldung über die Abwesenheit einer Meldung.
- **Ein Band auch auf der Kartenseite** — sie zeigt eine Karte, nicht einen Bestand; die Kopfzeile nennt die Spalte, und mehr gibt es dort nicht zu vergleichen.
- **Die Nachholmarke mit einer Frist versehen wie die Einflugmarke** — nach einer Trennung ist sie das Protokoll der Lücke und kein Zuruf; sie geht mit dem Band.
- **Die Zusammenfassungsschwelle als Literal im Renderzweig** — dann könnte kein Test sie senken; sie gehört an dieselbe Sorte Stelle wie `Markenstandzeit`.
- **Die erste Verbindung nach dem Start als Rückkehr behandeln** — jede frisch geöffnete Sicht schlösse gegen ein Bild auf, das sie nie hatte, und zeigte ein Band über nichts.
- **Die Umschrift des `ReconnectModal` in eine eigene Anforderung schieben** — sieben Zeilen in einer Datei, die dieser Slice ohnehin öffnet; die Trennung kostete mehr, als sie ordnete.
- **Den Trennungsdialog serverseitig rendern** — in Fall 2 ist der Server weg; die Zeile muss im Browser entstehen.
- **Die Element-Ids des Trennungsdialogs eindeutschen** — das Blazor-Laufzeitteil findet den Dialog über sie; C07 gilt Bezeichnern, und diese hier gehören dem Framework.
- **Den Blazor-Prozess anhalten, um Fall 2 im E2E-Test zu erzeugen** — dann stirbt der Kreislauf serverseitig, und die Vorlage zeigt ihren Dialog nicht; der Abbruch gehört auf die Browserseite.
- **Ein neues Seitenobjekt für die E2E-Locator** — `BoardSeite` führt die betroffenen Orte bereits.

### Bewusst out of scope

- **Ein Ereignisjournal, eine Folgenummer, ein Nachspielen** — bleibt tote Flexibilität; kein Knoten in der WBS.
- **Ereignisspur und Laufband** — `D0007.dc.html`, Rand C, ausdrücklich nicht gezeichnet, kein Knoten.
- **Ein Angebot über einem offenen Feld bei einer fremden Feldänderung** — gehört dem Slice, der Feldänderungen nachzieht; den gibt es nicht.
- **Die mitlaufende Dauer und ihr Anhalten während der Trennung** — **nachgesehen: es gibt sie nicht.** `I0028` hat den Sekundentakt einem eigenen Slice überlassen, im Bestand tickt nichts. Das Artboard-Wort „die mitlaufenden Dauern halten an" ist heute von selbst erfüllt und bekommt keine Bubble. Für den Takt selbst fehlt weiterhin ein Knoten unter `D0006`.
- **Nachziehen anderer Gegenstände als der Kartenbewegung** — unverändert wie in `R00031`.
- **Eine zugesagte Zeit, nach der ein Abriss bemerkt wird** — sie hinge an der Wiederaufnahmepause und an der Gegenseite; eine Zahl wäre eine Zusage ohne Grundlage.
- **Eine gemeinsame Zeitzone für Fall 1 und Fall 2** — Serverzeit gegen Browserzeit; im LAN auf einer Maschine derselbe Wert, sonst ein Interop-Aufruf, der hier nicht geplant ist.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede steht oben unter „Offene Fragen" mit ihrer Umkehrung.

1. **Zwei Abbrüche werden getrennt behandelt**, Trenner ist „wer noch rendern kann"; **Fall 2 braucht kein Aufschließen** (Beleg: keine `CircuitOptions` in `Program.cs`).
2. **Die Kopfzeilenmarke gehört ausschließlich zu Fall 1**; die drei Artboard-Stufen sind 1+2 = Fall 1, 3 = Fall 2.
3. **Aufgeschlossen wird durch frisches Holen**, nicht durch Nachspielen — über den bestehenden Ladeweg.
4. **Die Zahl im Band kommt aus dem Standvergleich**, verglichen wird die Lage (Spalte und Position).
5. **Die Nachholmarke trägt weder Wer noch Wann** — die einzige bewusste Abweichung vom Artboard; der Zeitpunkt steht einmal im Band.
6. **Die Nachholmarke hat keine Frist** und geht mit dem Band.
7. **„Stand von 09:12" ist der Zeitpunkt des Abrisses**, nicht der des letzten Ereignisses.
8. **Der Verbindungsstand hängt am `Ereignisverteiler`** — kein zweiter Singleton.
9. **Die erste Verbindung nach dem Start meldet keine Rückkehr.**
10. **Die Zusammenfassungsschwelle liegt bei etwa zehn** — Größenordnung, kein Messwert, an einer Stelle, die ein Test senken kann.
11. **Die Warteregeln aus `B0379` und `B0388` werden wiederverwendet**, nicht neu gebaut.
12. **Die Umschrift des `ReconnectModal` gehört in diesen Slice**, samt der einen Zeile über das Alter des Stands.
