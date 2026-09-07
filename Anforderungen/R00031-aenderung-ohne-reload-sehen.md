---
id: R00031
status: Neu
datum: 2026-09-07
---

# R00031: Änderung ohne Reload sehen

## Beschreibung

Bewegt jemand eine Karte — ein Mensch im Browser oder ein Agent über die API —, zeigt **jede andere offene Sicht** die Karte an ihrer neuen Stelle, ohne dass jemand nachladen oder nachfragen muss. Das offene Board zieht nach, die Bahnenzahlen ziehen mit, und die betroffene Karte trägt für kurze Zeit eine **Einflugmarke**: wer sie bewegt hat, über welchen Weg und wann. Die geöffnete Kartenseite nennt ihre neue Spalte. Wer gerade selbst eine Karte in der Hand hält oder ein Feld offen stehen hat, bekommt nichts unter der Hand ausgetauscht — die fremde Änderung wartet sichtbar, bis losgelassen ist.

Zahlt ein auf: [Vision](R00000-vision.md) — „**Live überall.** Bewegt ein Mensch oder die API eine Karte, sehen alle offenen Oberflächen die Änderung unverzüglich — ohne Reload, ohne Nachfragen." Und auf „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat": die Einflugmarke ist der erste Ort im Projekt, an dem **der Weg** einer Handlung sichtbar wird.

**Die zentrale Entscheidung dieses Slice — sie trägt alles andere:** **der Rückweg von der WebApi in die Oberfläche entsteht als Ereignisstrom (Server-Sent Events) auf `GET /api/ereignisse`.** Die Blazor-Anwendung hält **genau eine** Leitung je Prozess (ein `BackgroundService`) und verteilt im Prozess an alle offenen Kreisläufe — nicht eine Leitung je Browser. Fünf Alternativen sind geprüft und verworfen; die zwei stärksten Gründe stehen unter „Technische Überlegungen → Warum ein Ereignisstrom und nichts anderes" und sind hier zu bestätigen oder zu verwerfen, nicht in der Umsetzung.

**Es entsteht genau eine neue Route.** `GET /api/ereignisse` ist die **zweite boardlose Wurzelressource** des Projekts (nach `/api/zeiten`) und der **erste Endpunkt, der nicht antwortet und schließt**, sondern offen bleibt. Kein Schema, keine Migration, **kein neues NuGet-Paket**: `TypedResults.ServerSentEvents` steckt in `Microsoft.AspNetCore.App.Ref/10.0.0`, `SseParser` in `Microsoft.NETCore.App/10.0.0/System.Net.ServerSentEvents.dll` — nachgesehen, nicht vermutet.

**Der Endpunkt ist zugleich der Gewinn für das Kernmotiv:** `GET /api/ereignisse` steht **jedem Agenten** offen. Die Oberfläche bekommt keinen Sonderweg, sondern ist der erste Abonnent einer Leitung, die für alle da ist — genau die Zusage der Vision, dass die API alles kann, was die Oberfläche kann.

**`D0007` wird mit diesem Slice nicht grün:** `I0029` (Aufschließen nach dem Abbruch) bleibt offen. Was dieser Slice ausdrücklich **nicht** kann und was `I0029` nachträgt, steht unter „Akzeptanzkriterien → Was dieser Slice ausdrücklich nicht tut" und ist Teil der Vereinbarung.

## Geschäftlicher Nutzen

Bis heute ist jede offene Sicht dieser Anwendung eine Momentaufnahme. `Board.razor` lädt beim Aufbau und nach der **eigenen** Handlung; `I0027`/`B0366` hat für die Kopfzeile vier Ladeanlässe festgelegt und den Abfragetakt **ausdrücklich vermieden** — mit der dort wörtlich benannten Lücke: „startet oder stoppt jemand anders, während meine Seite offen steht, bleibt meine Zahl stehen." Für eine Zahl in der Kopfzeile ist das unschön. Für ein Kanban-Board ist es der Kern des Problems.

Denn das Board ist genau der Ort, an dem sich zwei Akteure begegnen. Die Vision stellt Mensch und Agent gleich: **der Agent bewegt Karten über die API, während der Mensch dasselbe Board offen hat.** Ohne Rückweg sieht der Mensch davon nichts. Er arbeitet auf einem Bild, das mit jeder Minute weniger stimmt, zieht eine Karte auf eine Stelle, die es nicht mehr gibt, und erfährt vom Zug des Agenten erst, wenn er zufällig neu lädt. Ein Board, das der Wirklichkeit hinterherhinkt, ist genau das, wogegen die Vision gebaut wird („ein Board, das der Wirklichkeit nicht hinterherhinkt, weil die Agenten es selbst führen").

Der zweite, härtere Fall ist der **zweite Mensch im LAN**. Die Leitplanke „Weboberfläche, netzwerkfähig im LAN" und das Full-Trust-Modell laden ausdrücklich dazu ein, mehrere Browserfenster auf dieselbe Instanz zu richten. Zwei Menschen auf demselben Board ohne Live-Kanal sind zwei Menschen, die sich gegenseitig überschreiben, ohne es zu merken.

Und dritter Nutzen, der über das Sehen hinausgeht: **die Einflugmarke macht den Weg sichtbar.** Heute steht an einer Handlung, *wer* sie getan hat (Kontributor). Erstmals steht auch da, *worüber* — „über die API" oder gar nicht, wenn es die Oberfläche war. Das ist die kleinste Form der Zusage „An jeder Karte ist ablesbar, wer oder was gehandelt hat", und sie kostet ein Feld im Ereignisvertrag.

## Funktionale Anforderungen

- Die WebApi bietet einen offenen Ereignisstrom auf `GET /api/ereignisse` an, den jeder Abonnent lesen kann.
- Eine erfolgreiche Kartenbewegung (`PUT /api/boards/{boardId}/karten/{karteId}/lage`) erzeugt ein Ereignis mit Board, Karte, Zielspalte, Urheber, Weg und Zeitpunkt.
- Eine **zurückgewiesene** Bewegung erzeugt **kein** Ereignis.
- Mehrere gleichzeitige Abonnenten bekommen dasselbe Ereignis.
- Die Blazor-Anwendung hält **eine** Leitung je Prozess und verteilt im Prozess an alle offenen Kreisläufe.
- Reißt die Leitung ab (etwa durch einen Neustart der WebApi), nimmt sie sich von selbst wieder auf.
- Ein offenes Board zieht die fremde Bewegung ohne Zutun nach — die Karte steht an der Stelle, an die der Urheber sie gelegt hat.
- Die Bahnenzahlen der Spalten stimmen nach dem Nachziehen.
- Hält jemand gerade eine Karte in der Hand, wartet die fremde Bewegung **sichtbar** bis zum Loslassen.
- Die bewegte Karte trägt eine Einflugmarke aus Akzentkante und Fußzeile „Wer · über welchen Weg · wann".
- Ein Mensch an der Oberfläche wird **ohne** Wegangabe genannt, ein Aufruf der API **mit**.
- Die **eigene** Handlung bekommt **keine** Marke.
- Jede Marke verschwindet nach etwa zehn Sekunden von selbst.
- Die geöffnete Kartenseite nennt nach einer fremden Bewegung ihre neue Spalte und trägt die Marke.
- Steht auf der Kartenseite ein Feld offen, wartet die Meldung sichtbar und tauscht nichts aus — der ungesendete Text bleibt stehen.

## Nicht-funktionale Anforderungen

- **Aktualität:** die Änderung erreicht die andere Sicht ohne Zutun. Eine Zeitschranke wird **nicht** zugesagt — im LAN ist der Weg WebApi → Blazor-Prozess → SignalR → Browser kurz, aber er hängt an drei Übergängen, von denen keiner gemessen ist.
- **Last:** je offener Sicht ein Abruf je Bewegung (Signal statt Nutzlast, s. u.). Bei einer Handvoll Sichten im LAN tragbar; ausdrücklich **nicht** für Dutzende gleichzeitiger Sichten ausgelegt.
- **Ordnung der Abhängigkeiten:** die WebApi weiß von der Oberfläche weiterhin **nichts**. Der Rückweg entsteht ausschließlich dadurch, dass die Oberfläche als Klient abonniert.
- **Kernregel:** `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL`. Der Ereigniskanal läuft über HTTP wie jeder andere Weg.
- **Sicherheit:** die Marke `X-KanbanC-Weg` ist fälschbar. Im Full-Trust-Modell ohne Anmeldung (Leitplanke der Vision) schützt hier ohnehin nichts; die Marke ist eine **Auskunft, keine Zusage**.
- **Kein neues Paket** in keinem Projekt.

## Akzeptanzkriterien

### Der Rückweg von der WebApi in die Oberfläche

- [ ] `GET /api/ereignisse` antwortet mit **200** und `Content-Type: text/event-stream` und schließt nicht.
- [ ] Nach einem erfolgreichen `PUT /api/boards/{boardId}/karten/{karteId}/lage` liefert der Strom **genau ein** Ereignis mit **Board**, **Karte**, **Zielspalte**, **Urheber**, **Weg** und **Zeitpunkt**.
- [ ] **Zwei gleichzeitige Abonnenten** bekommen dasselbe Ereignis; keiner bekommt es doppelt, keiner gar nicht.
- [ ] Eine **zurückgewiesene** Bewegung (Zielspalte gehört nicht zum Board, unbekannte Karte, ungültige Position) meldet **nichts** — der Strom bleibt still.
- [ ] Bewegt sich nichts, bleibt der Strom **offen und leer** — kein 404, kein Schließen, keine Fehlermeldung.
- [ ] Trägt die Anfrage `X-KanbanC-Weg: oberflaeche`, steht im Ereignis der Weg `Oberflaeche`; **fehlt der Kopf, steht `Api`**.
- [ ] Nennt der Rumpf einen `Kontributor`, steht dessen Id als Urheber im Ereignis; nennt er keinen, ist der Urheber `null` und die Bewegung wird trotzdem ausgeführt.
- [ ] Die Blazor-Anwendung hält **genau eine** Leitung je Prozess — nicht eine je Browser und nicht eine je Kreislauf.
- [ ] Wird die WebApi angehalten und neu gestartet, während die Blazor-Anwendung läuft, **nimmt die Leitung sich von selbst wieder auf**; die nächste Bewegung nach dem Neustart erreicht die offenen Sichten wieder ohne Zutun.
- [ ] Nachgeholt wird beim Wiederaufnehmen **nichts** — was während der Trennung geschah, bleibt ungemeldet (das ist `I0029`).

### Das offene Board zieht nach

- [ ] Bewegt ein **zweiter Browser** eine Karte auf Board X, zeigt ein offenes Board X die Karte **ohne Zutun** an ihrer neuen Stelle.
- [ ] Dasselbe gilt für einen **Aufruf der API ohne Browser** (`PUT …/lage` mit `HttpClient`).
- [ ] Die Karte steht **an der Stelle, an die der Urheber sie gelegt hat** — nicht oben und nicht unten. Rechenbeispiel: liegt „Bereit" mit A, B, C und legt der Urheber D auf Position 1, steht danach in jeder Sicht A, D, B, C.
- [ ] Die **Bahnenzahlen** stimmen nach dem Nachziehen. Rechenbeispiel: „Bereit" 6 → **5**, „In Arbeit" 3 → **4**.
- [ ] In der Herkunftsbahn bleibt **kein Platzhalter** stehen.
- [ ] Ein Ereignis zu einem **anderen** Board lässt das offene Board unberührt.
- [ ] **Hält jemand gerade eine Karte in der Hand**, ordnet sich unter der Maus nichts um: die fremde Bewegung wird **sichtbar zurückgehalten** („1 Änderung wartet") und beim Loslassen eingespielt.
- [ ] Wird die Sicht verlassen (Seitenwechsel, geschlossener Browser), hört sie auf zuzuhören — der Verteiler hält keine toten Kreisläufe fest.

### Die Einflugmarke

- [ ] Eine fremde Bewegung trägt an der Karte eine **Akzentkante** und eine **Fußzeile** aus Wer, über welchen Weg und wann.
- [ ] Ein Mensch an der Oberfläche wird **ohne Weg** genannt: „Nina Barth · vor 3 Sek".
- [ ] Ein Aufruf der API wird **mit Weg** genannt: „Claude-Agent · über die API · gerade eben".
- [ ] Ein **unbekannter oder fehlender Urheber** ergibt eine Marke ohne Namen, keinen Absturz.
- [ ] Der Name kommt aus der **Kontributorenliste**, nicht aus dem Ereignis — ein Umbenennen zieht von selbst nach.
- [ ] Die **eigene** Handlung bekommt **keine** Marke. Rechenbeispiel: zieht Stefan in Browser 1 eine Karte, trägt sie in Browser 1 **keine** Marke und in Browser 2 eine — auch dann, wenn in Browser 2 ebenfalls „Stefan" gewählt ist.
- [ ] Jede Marke **verschwindet nach etwa zehn Sekunden** von selbst; danach steht die Karte wieder ruhig.
- [ ] Die Unterscheidung Mensch/API läuft über **Anwesenheit und Wortlaut**, **nie über die Farbe** — Olive und Terrakotta tragen in diesem Projekt die Art des Kontributors.
- [ ] „Etwa zehn Sekunden" ist eine **Größenordnung, kein Messwert**: geprüft wird, dass die Marke von selbst verschwindet, nicht ihre Standzeit auf die Sekunde.

### Die offene Kartenseite zieht nach

- [ ] Wird die geöffnete Karte von jemand anderem bewegt, nennt die **Kopfzeile der Kartenseite** ihre **neue Spalte** ohne Zutun.
- [ ] Die Kartenseite trägt dabei dieselbe **Einflugmarke** wie die Karte am Board.
- [ ] Ein Ereignis zu einer **anderen Karte** lässt die offene Kartenseite unberührt.
- [ ] **Steht ein Feld offen** (Titel, Beschreibung, Fälligkeit, …), wird **nichts ausgetauscht**: die Meldung wartet **sichtbar**, und der **ungesendete Text bleibt stehen**.
- [ ] Nach dem Schließen des Felds wird die zurückgehaltene Meldung eingespielt.
- [ ] Es gibt **kein Angebot über einem offenen Feld** — eine Bewegung fasst kein Feld an.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Nur die Kartenbewegung** zieht nach. Nicht dabei: Kartenanlage und -änderung, Kommentare, Etiketten, Teilaufgaben, Anhänge, Dateiverweise, Klassenzuordnung und Farbe.
- [ ] **Die Archivierung zieht nicht nach** — sie ist keine Bewegung und hat keinen WBS-Knoten.
- [ ] **Spalten- und Boardänderungen** ziehen nicht nach.
- [ ] **Zeitereignisse** ziehen nicht nach; die Kopfzeilenplakette aus `R00030` behält ihre vier Ladeanlässe und ihre benannte Lücke.
- [ ] **Keine mitlaufende Dauer** — weder in der Kopfzeile noch an der Karte noch auf der Kartenseite.
- [ ] **Keine Ereignisspur und kein Laufband** — eine Liste vergangener Ereignisse ist etwas anderes als eine ankommende Änderung.
- [ ] **Kein Ereignisspeicher, keine Folgenummer, kein Nachholen** verpasster Änderungen.
- [ ] **Kein deutsches `ReconnectModal`**, keine alternde Kopfzeile, keine Zusammenfassung mehrerer verpasster Änderungen — alles `I0029`.
- [ ] **Keine Migration**, kein Schema, kein neues NuGet-Paket.

### Der grüne Bestand bleibt grün

- [ ] Die **`R00007`-Suite** (Karte verschieben) bleibt unangetastet grün: `Kartenlage` wächst um ein Feld **mit Vorgabewert**, bestehende Aufrufe `new Kartenlage(a, b)` compilieren weiter, und ein JSON ohne das Feld wird zu `null`.
- [ ] Der **Routentabellen-Test** der WebApi zählt die neue Route mit; ein Test, der sie nicht mitzählt, ist rot — das ist gewollt.
- [ ] Die bestehende Behandlung **zurückgewiesener Züge** in `Board.razor` bleibt unverändert: sie ist bereits gebaut (`B0380` steht auf `bestehend`).
- [ ] Der **`Laufzeitmelder`** aus `R00030` bleibt bestehen und wird **nicht** ersetzt — er ist der Weg für die eigene Handlung ohne Umweg über die WebApi.
- [ ] Alle bestehenden E2E-Tests bleiben grün, obwohl in jedem Lauf nun eine Ereignisleitung mitläuft.

## Betroffene Verzeichnisstruktur

- **`Source/KanbanC.Contracts/Ereignisse/`** — der seit dem 29.08. **leer** angelegte Ordner bekommt seinen Inhalt: `Kartenereignis.cs` und `Ereignisweg.cs`, der Vertrag, den beide Prozesse sprechen.
- **`Source/KanbanC.Contracts/Karten/Kartenlage.cs`** — wächst um ein nullbares Feld für den Urheber.
- **`Source/KanbanC.WebApi/`** — die `Ereignisdrehscheibe` (Singleton) und `Endpunkte/EreignisEndpunkte.cs`; `Endpunkte/KartenEndpunkte.cs` meldet nach erfolgreicher Bewegung; `Program.cs` registriert beides.
- **`Source/KanbanC.BL/`** — **unberührt**. Die Fachlogik kennt keine Abonnenten.
- **`Source/KanbanC.Blazor/Services/`** — `Ereignisleitung` (`BackgroundService`) und `Ereignisverteiler` (Singleton) neben den bestehenden Diensten; `Einflugmarke` als Rechnung der Oberflächenschicht dort, wo `Laufplakette`, `Laufzaehler`, `Dauerform` und `Zeitpunktform` schon wohnen. `Program.cs` setzt den Kopf `X-KanbanC-Weg` am benannten `HttpClient "KanbanC"` und registriert die zwei neuen Dienste.
- **`Source/KanbanC.Blazor/Components/`** — `Pages/Board.razor`, `Pages/Kartendetail.razor`, `Spalten/Spaltenbahnen.razor(.css)`, `Karten/Karte.razor(.css)`.
- **`Source/KanbanC.Blazor/wwwroot/gestaltung.css`** — die Gestaltungswerte der Marke (Akzentkante, Schatten, Fußzeile) entstehen als Token; **kein Literal** in einer Komponenten-CSS-Datei.
- **Tests** — `Source/KanbanC.WebApi.IntegrationTests/Api/` (Strom-Endpunkt, Meldung, Zurückweisung, Routentabelle), `Source/KanbanC.Blazor.Tests/Services/` (Vertrag, Verteiler, Marke, Klient-Fehlerpfade), `Source/KanbanC.PlaywrightTests/Tests/` und `PageObjects/` (die drei E2E-Läufe).
- **Keine Migration** — `Source/KanbanC.BL/Persistenz/Migrationen/` bleibt unverändert.

## Technische Überlegungen

### Warum ein Ereignisstrom und nichts anderes

Die Frage dieses Slice ist nicht „wie zeige ich eine Änderung an", sondern „**wie erfährt die Oberfläche überhaupt davon**". Blazor Server hat den Weg **Server → Browser** längst (SignalR-Kreislauf); was fehlt, ist der Weg **WebApi → Blazor-Prozess**. Fünf Wege sind geprüft:

**Verworfen: ein Abfragetakt aus jedem Kreislauf.** Zwei Gründe, und der zweite ist der schwerere. Erstens hat `I0027`/`B0366` den Takt für die Kopfzeile **ausdrücklich vermieden** und die Entscheidung begründet — ein zweiter, gegenteiliger Weg an derselben Anwendung wäre eine zweite Wahrheit über Aktualität. Zweitens, und das ist ausschlaggebend: **ein Abfragetakt sieht nur den neuen Zustand, nie den Urheber.** Er kann sagen, dass die Karte jetzt woanders liegt, nicht, wer sie bewegt hat und über welchen Weg. **Die Einflugmarke wäre damit unbaubar** — und sie ist nicht Beiwerk, sondern die Einlösung der Vision-Zusage „An jeder Karte ist ablesbar, wer oder was gehandelt hat". Dazu kosteten N Sichten N Takte für eine Änderung, die selten kommt.

**Verworfen: ein HTTP-Rückruf der WebApi an die Oberfläche.** Er **kehrt die Abhängigkeitsrichtung um**: die WebApi müsste die Adresse der Oberfläche konfiguriert bekommen und **scheiterte, wenn die Oberfläche aus ist**. **Heute weiß die WebApi von der Oberfläche nichts — und das ist die Ordnung, die die Kernregel trägt.** Die Kernregel dieses Projekts („`KanbanC.Blazor` hat keine Projektreferenz auf `KanbanC.BL`") sagt, dass die Oberfläche ein Klient ist. Ein Rückruf machte die WebApi zum Klienten ihres Klienten; die nächste Oberfläche (ein zweiter Blazor-Prozess, ein Agent mit eigener Ansicht) bräuchte einen Eintrag in der Konfiguration der API. Ein abonnierbarer Strom braucht keinen.

**Verworfen: beide Prozesse lesen dieselbe SQLite.** Schlicht **verboten**: `KanbanC.Blazor` hat keine Referenz auf `KanbanC.BL`, der Datenzugriff wohnt in `BL/Persistenz`, die Oberfläche darf die Datei nicht öffnen. Wer diesen Weg nimmt, hebt das Kernmotiv des Projekts auf.

**Verworfen: eine Ereignistabelle in der Datenbank.** Sie **löst den Transport nicht** — jemand müsste die Tabelle immer noch abfragen, und man wäre beim Abfragetakt. Sie kostet eine Migration, und die Haltbarkeit, die sie brächte, wird erst von `I0029` gebraucht.

**Verworfen: ein SignalR-Hub in der WebApi.** Er bräuchte `Microsoft.AspNetCore.SignalR.Client` als **neues Paket** in `KanbanC.Blazor` und stellte ein zweites SignalR neben das, das der Browser schon spricht. **SSE kommt mit null neuen Paketen aus.**

**Gewählt: Server-Sent Events.** Eine Richtung, ein Text-Protokoll, im Framework enthalten, von jedem HTTP-Klienten lesbar — auch von `curl`. Und: **der Endpunkt steht jedem Agenten offen.** Die Oberfläche bekommt keinen Sonderweg; sie ist der erste Abonnent einer Leitung, die zur API gehört.

### Der Preis, ausdrücklich benannt

1. **Eine Richtung.** Der Strom trägt nur von der WebApi weg. Für diesen Slice reicht das: gehandelt wird über die bestehenden Endpunkte.
2. **Kein Gedächtnis.** Was während einer Trennung geschieht, ist **weg**. Ein Abonnent, der eine Minute nicht verbunden war, erfährt von den Bewegungen dieser Minute nichts und wird auch nicht darauf hingewiesen. **Das ist `I0029`** — und bis dahin eine echte, benannte Lücke, keine theoretische.
3. **Ein Wackelkandidat im Test.** Der Integrationstest muss auf einen **offenen** Strom mit Zeitschranke warten statt auf eine Antwort. Deshalb steht `B0369` als **Probe** davor (Skill `dependency-probe`, Muster `B0254`, `B0266`, `B0282`), die vier Annahmen widerlegen darf, bevor irgendetwas darauf gebaut wird.
4. **Je offener Sicht ein Abruf je Bewegung** (Folge von „Signal statt Nutzlast"). Im LAN mit einer Handvoll Sichten tragbar, für Dutzende nicht ausgelegt.

### Signal statt Nutzlast

Das Ereignis trägt **nicht die neue Karte**, sondern die Nachricht, dass sich an Board X etwas bewegt hat — samt Karte, Zielspalte, Urheber, Weg und Zeitpunkt. **Jede Sicht holt danach über ihren bestehenden Klienten neu** (`GET /api/boards/{boardId}` bzw. `GET /api/karten/{karteId}`).

So entsteht **kein zweiter Leseweg** neben dem, der schon da ist, und die Sicht zeigt **genau den Stand, den ein Reload auch zeigte** — es gibt keine zweite Art, wie eine Bahn zustande kommt. **Die Bahnenzahlen kosten dabei nichts:** `Bahnenkopfzahl.AlsText` rechnet aus `Spalte.Karten` und `Spalte.Kartenzahl`, und `LadeBoard` bringt beide neu — nachgesehen, nicht vermutet.

Der Preis ist der Abruf je Sicht und Bewegung. Die Umkehrung — die neue Karte im Ereignis mitschicken — spart den Abruf und handelt sich dafür zwei Wege ein, auf denen eine Bahn entsteht; der zweite wäre der, den niemand testet.

### Der Weg reist im Kopf, der Urheber im Rumpf

Das ist **eine** Entscheidung mit zwei verschiedenen Antworten, und der Unterschied hat einen Grund.

**Der Weg** ist eine Eigenschaft des **Aufrufers**, nicht der Handlung. Er reist als `X-KanbanC-Weg: oberflaeche` und wird an **genau einer Stelle** gesetzt: am benannten `HttpClient "KanbanC"` in `Blazor/Program.cs` als `DefaultRequestHeader`. Damit trägt ihn jeder Aufruf der Oberfläche, ohne dass ein einziger Aufrufer davon weiß. **Fehlt der Kopf, gilt `Api`** — die richtige Voreinstellung, weil **ein Agent nichts setzen muss** und die Oberfläche die Ausnahme ist, die sich meldet. Fälschbar ist er; im Full-Trust-Modell ohne Anmeldung schützt hier ohnehin nichts, und die Marke ist eine **Auskunft, keine Zusage**.

**Der Urheber** ist eine Eigenschaft der **Handlung** und reist deshalb im Rumpf, „wie jeder Kontributor in diesem Projekt" — Muster `KommentarSchreibenAnfrage`, `AnhangAnlegenAnfrage`, `ZeitmessungStartenAnfrage`. `Kartenlage` wächst um `long? Kontributor` **mit Vorgabewert**; so bleiben bestehende Aufrufe `new Kartenlage(a, b)` und die grüne `R00007`-Suite unangetastet, und ein fehlendes Feld im JSON wird zu `null`. **Nullbar**, weil ein Agent ohne Identität weiter verschieben können muss — dann nennt die Marke nur Weg und Zeitpunkt.

**Nur die Id, nicht der ganze Kontributor.** Den Namen löst die Sicht über die Kontributorenliste auf — wörtlich die Regel des `Identitaetsspeicher`: „damit ein Umbenennen von selbst nachzieht". Ein ganzer Kontributor kostete auf dem Bewegungsweg einen zusätzlichen Lesezugriff, und der Name im Ereignis wäre ab dem nächsten Umbenennen falsch.

### Die Meldung entsteht im Endpunkt, nicht im Dienst

`KartenEndpunkte.VerschiebeKarte` ruft nach einem **erfolgreichen** `KartenService.VerschiebeKarte` die `Ereignisdrehscheibe`. Zwei Gründe: der **Weg steht nur in der Anfrage** und wäre im Dienst nicht mehr bekannt, und **`KanbanC.BL` bleibt frei von Abonnenten** — die Fachlogik weiß nicht, dass jemand zuhört.

**Eine zurückgewiesene Bewegung meldet nichts.** Es hat sich nichts bewegt; ein Ereignis wäre eine Nachricht über etwas, das nicht geschehen ist.

**Der Preis, benannt:** wer die BL an der WebApi vorbei ruft, erzeugt kein Ereignis. Einen solchen Weg gibt es heute nicht — die Oberfläche darf ihn nicht haben (Kernregel), und ein Agent spricht ohnehin HTTP.

### Eine Leitung je Prozess, ein Verteiler je Prozess

Zwei Bauteile, und die Trennung ist wichtig:

- **`Ereignisleitung`** ist ein `BackgroundService` — **eine je Blazor-Prozess**, nicht eine je Kreislauf und schon gar nicht eine je Browser. Zehn offene Browser erzeugen eine Leitung, nicht zehn. Sie liest mit `HttpCompletionOption.ResponseHeadersRead` und `SseParser`, und **sie nimmt sich nach einem Abriss von selbst wieder auf**.
- **`Ereignisverteiler`** ist ein Singleton mit Ereignis, an dem sich jeder Kreislauf an- und abmeldet.

Das ist **das Muster `Laufzeitmelder`, eine Ebene höher.** Der Melder ist `AddScoped` und bleibt im eigenen Kreislauf („ein zweiter Browser und die WebApi erfahren nichts davon"); der Verteiler ist `AddSingleton` und trägt über alle Kreisläufe. **Der `Laufzeitmelder` bleibt richtig und wird nicht ersetzt** — er ist der Weg für die eigene Handlung ohne Umweg über die WebApi und damit schneller und unabhängig vom Kanal.

**Achtung Fadengrenze:** die Meldung kommt aus dem Hintergrunddienst. Jeder Hörer muss über `InvokeAsync` in den Renderfaden zurück, sonst rendert Blazor aus einem fremden Faden.

**Achtung Abmeldung:** ein Singleton, an dem sich Kreisläufe anmelden, hält tote Kreisläufe fest, wenn niemand abmeldet. `Board.razor` und `Kartendetail.razor` melden sich in `Dispose` ab.

**Die Wiederaufnahme gehört hierher und nicht zu `I0029`.** Ohne sie wäre das Fertig-Kriterium schon nach dem ersten Neustart der WebApi falsch, und ein Kriterium, das nur bis zum ersten Neustart hält, ist keines. **Nachgeholt wird dabei nichts** — das ist die Grenze zu `I0029`.

### Warteregeln statt Austausch: zwei Orte, eine Haltung

Der Neuabruf ersetzt den ganzen Zustand einer Sicht. An zwei Stellen wäre das ein Schaden, und beide bekommen dieselbe Antwort: **die Änderung wartet sichtbar.**

**Am Board (`B0379`):** ordnete sich die Bahn unter der Maus um, zeigte die Einfügelinie auf eine Stelle, die es beim Loslassen nicht mehr gibt. Der Zugzustand liegt **schon vor** — `Spaltenbahnen.razor` führt `_laufenderZug`, `BeginneZug` und `BeendeZug`. Die Bahnen melden Beginn und Ende nach oben (Muster `KarteWurdeAbgelegt`), `Board.razor` hält die Meldung zurück und zeigt „1 Änderung wartet". Denselben Gedanken kennt der Bestand als `ablegeflaeche-laeuft` auf der Kartenseite.

**Auf der Kartenseite (`B0388`):** der Bestand hat den Schalter schon — `_offenesFeld` in `Kartendetail.razor` („Höchstens ein Feld steht offen; welches, sagt sein Name"). Gebraucht wird die Regel **auch dann, wenn eine Bewegung kein Feld anfasst**: der Neuabruf ersetzt `_detail`, und das Beschreibungsfeld rendert seinen Inhalt aus `_detail.Karte.Beschreibung` — ein Austausch nähme den ungesendeten Text weg. „Eine Änderung, die mir unter der Hand den Text austauscht, wäre schlimmer als gar keine Live-Aktualisierung."

**Kein Angebot über einem offenen Feld.** Das Artboard zeichnet in Zustand 2 die Trennung „zieht nach" / „wird angeboten". Gebaut wird nur die erste Hälfte: **eine Bewegung fasst kein Feld an**, ein Angebot ohne Anlass wäre tote Flexibilität (C24). Das Angebot entsteht mit dem Slice, der fremde **Feldänderungen** nachzieht — und den gibt es noch nicht.

### Die Einflugmarke

**Akzentkante links, stärkerer Schatten, Fußzeile über einer Trennlinie** — die Marke steht **in** der Karte, nicht daneben: sie gehört der Karte, nicht der Bahn.

**Die Unterscheidung läuft über Anwesenheit und Wortlaut, nie über die Farbe.** Olive und Terrakotta tragen in diesem Canvas die **Art des Kontributors** (`D0002`, `D0006`); eine dritte Bedeutung derselben Farben machte beide unlesbar. Der **Weg steht nur bei `Api`** — die Oberfläche ist der Normalfall und braucht keine Nennung.

**Die eigene Handlung bekommt keine Marke.** Wer selbst zieht, weiß es schon; die Rückmeldung war die Bewegung unter der Maus. Der Vermerk **hängt am Kreislauf, nicht an der Identität** — sonst schwiege ein zweiter Browser derselben Person mit, und genau der soll die Änderung sehen. Er wird **vor** dem Aufruf gesetzt, weil das Ereignis vor der Antwort eintreffen kann, und verfällt mit der Standzeit der Marke.

**Die Uhr wird hereingereicht**, damit „vor 3 Sek" ohne Zeitmanipulation prüfbar ist — Muster `Zeitmessungsende.Fuer`.

**„Etwa zehn Sekunden" ist eine Größenordnung, kein Messwert.** Eine Marke, die bleibt, ist keine Nachricht mehr, sondern ein Verlauf — und den führt `D0007` nicht. Der Zeitgeber endet mit dem Abbau des Kreislaufs, sonst hält er eine tote Sicht am Leben. Und die Standzeit gehört an eine Stelle, an der ein Test sie kürzen kann: **kein E2E-Lauf darf zehn Sekunden warten müssen.**

### Gestaltungsvorgabe

Das Artboard ist `Dokumentation/Wireframes/D0007.dc.html` (`betrieb: lokal` — die Datei im Repository ist der einzige Stand). Für diesen Slice gelten **Zustand 1** (fremde Bewegung am Board, Fenster 1440×900), **Zustand 2** (fremde Änderung an der offenen Kartenseite, Trennung „zieht nach" / „wird angeboten"), **Zustand 3** (Anatomie der Einflugmarke in drei Fassungen), **Zustand 4** (Browser und API sind gleichberechtigt) sowie **Rand A** (Karte in der Hand) und **Rand C** (die ausdrückliche Nicht-Zeichnung). **Zustand 5 und 6 gehören `I0029`** und sind hier nicht Vorgabe. **Rand B** (die geöffnete Karte wird archiviert) ist keine Bewegung und hat keinen WBS-Knoten.

Das Bild ist die Quelle; es wird hier nicht nacherzählt. **Aus dem Artboard entstehen keine Akzeptanzkriterien** — es ist Vorgabe für die Gestaltung, wie eine Bubble Vorgabe für den Bau ist.

Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`, **kein Literal in einer Komponenten-CSS-Datei** (Projektkonvention).

**Drei Zusagen des Artboards lösen andere Slices ein — hier benannt, damit niemand sie in diesem Slice sucht:**

1. **Die mitlaufende Dauer** an Karte und Kartenseite. Der Kanal räumt den *einen* Grund weg („eine gerenderte Dauer ist ohne Live-Kanal ab der ersten Sekunde falsch"), **nicht den zweiten** aus `B0326`/`B0362`: ein Sekundentakt läuft nicht über den Ereigniskanal, sondern über einen **eigenen Zeitgeber je Kreislauf**, und der bleibt Wort für Wort der E2E-Wackelkandidat, der er war. Der Kanal macht die Dauer **richtig**, nicht **billig**.
2. **Die laufende Zeit in der Ist-Summe.**
3. **Die Kopfzeilenplakette, die ohne Ladeanlass mitwächst** (Zeitereignisse).

**Für keine der drei gibt es heute einen WBS-Knoten.** Sie brauchen einen eigenen Slice unter `D0006` oder eine Ausbaustufe an `I0026`; dieser Lauf legt ihn nicht an, weil er außerhalb des Teilbaums von `I0028` läge. Das ist eine **Lücke der Planung**, kein Versehen dieser Anforderung — und sie steht unter „Offene Fragen".

### Ablauf

1. **Ein Akteur bewegt eine Karte**
   - 1.1 Oberfläche: `Board.razor.VerschiebeKarte` → `KartenApiKlient.VerschiebeKarte(BoardId, karteId, new Kartenlage(spalteId, position, _gewaehlteKontributorId))`; der `HttpClient "KanbanC"` hängt `X-KanbanC-Weg: oberflaeche` an
   - 1.2 Agent: `PUT /api/boards/{boardId}/karten/{karteId}/lage` ohne den Kopf, mit oder ohne `Kontributor` im Rumpf
2. **Die WebApi führt aus und meldet**
   - 2.1 `KartenEndpunkte.VerschiebeKarte` → `KartenService.VerschiebeKarte(boardId, karteId, lage)`
   - 2.2 Bei Zurückweisung: `400` mit Grund und Kompensationsaktion — **keine Meldung**
   - 2.3 Bei Erfolg: `Ereignisweg.Aus(kopfzeilen)` → `Ereignisdrehscheibe.Melde(new Kartenereignis(...))` → `200`
3. **Die Drehscheibe verteilt an ihre Abonnenten**
   - 3.1 Je Abonnent ein begrenzter `Channel` mit `DropOldest` — ein langsamer Leser hält den Melder nicht an
   - 3.2 `EreignisEndpunkte` schreibt jedes Element auf `GET /api/ereignisse`
4. **Die Blazor-Anwendung liest**
   - 4.1 `Ereignisleitung` (`BackgroundService`) liest den Strom mit `SseParser`
   - 4.2 Bei Abriss: Pause, neuer Versuch — **ohne Nachholen**
   - 4.3 Je Element: `Ereignisverteiler.Melde(kartenereignis)`
5. **Jede offene Sicht entscheidet für sich**
   - 5.1 `Board.razor`: passt `Board`? — sonst nichts
     - 5.1.1 Läuft gerade ein Zug: Meldung zurückhalten, „1 Änderung wartet" zeigen, beim Loslassen weiter bei 5.1.2
     - 5.1.2 `InvokeAsync` → `LadeBoard()` → Bahnen und Kopfzahlen neu
     - 5.1.3 War es **nicht** die eigene Handlung: `Einflugmarke.Fuer(...)` an die Karte, Zeitgeber auf etwa zehn Sekunden
   - 5.2 `Kartendetail.razor`: passt `Karte`? — sonst nichts
     - 5.2.1 Steht ein Feld offen: Meldung zurückhalten, sichtbar warten, beim Schließen weiter bei 5.2.2
     - 5.2.2 `InvokeAsync` → Neuabruf → neue Spalte in der Kopfzeile, Marke wie bei 5.1.3

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** `KartenEndpunkte.VerschiebeKarte` (`KartenEndpunkte.cs:309`, Route `KartenEndpunkte.cs:18`) — die eine Stelle, an der gemeldet wird; `Program.cs` der WebApi für Drehscheibe und Endpunkt; `Program.cs` der Blazor-App für `DefaultRequestHeader`, `Ereignisverteiler` und `Ereignisleitung`; `Board.razor` und `Kartendetail.razor` als die zwei zuhörenden Sichten. **Kein neuer Schirm, keine neue Seite, kein neuer Dienst in der BL, keine Migration.**

**In `KanbanC.Contracts` (neu):**
- `Kartenereignis` (DTO, immutable, C08) — was sich bewegt hat, samt Urheber, Weg und Zeitpunkt. Ids nach Hauskonvention.
  - `Kartenereignis(long Board, long Karte, long SpalteId, long? Urheber, Ereignisweg Weg, DateTimeOffset Zeitpunkt)`
- `Ereignisweg` (Aufzählung, Muster `Kontributorart`, `BoardArt`) — `Oberflaeche` und `Api`.

**In `KanbanC.Contracts` (geändert):**
- `Kartenlage` — `public record Kartenlage(long SpalteId, int Position, long? Kontributor = null)`. **Der Vorgabewert ist die ganze Verträglichkeitszusage.**

**In `KanbanC.WebApi` (neu):**
- `Ereignisdrehscheibe` (Integration, Singleton) — nimmt Meldungen an und gibt je Abonnent einen Strom aus; begrenzter `Channel` mit `DropOldest`, An- und Abmelden unter Nebenläufigkeit.
  - `void Melde(Kartenereignis ereignis)`
  - `IAsyncEnumerable<Kartenereignis> Abonniere(CancellationToken abbruch)`
- `Ereignisweg`-Ableitung als **Operation** (pure Funktion über die Kopfzeilen) — `Oberflaeche`, wenn `X-KanbanC-Weg: oberflaeche` steht, sonst `Api`.
- `EreignisEndpunkte` (Integration) — registriert `GET /api/ereignisse`, antwortet mit `TypedResults.ServerSentEvents`.
  - `static void Registriere(IEndpointRouteBuilder routen)`

**In `KanbanC.Blazor/Services` (neu):**
- `Ereignisleitung` (`BackgroundService`, **eine je Prozess**) — hält die Leitung zur WebApi, liest mit `SseParser`, nimmt nach Abriss wieder auf, reicht jedes Element an den Verteiler.
- `Ereignisverteiler` (Integration, Singleton mit Ereignis) — verteilt an alle angemeldeten Kreisläufe. Muster `Laufzeitmelder`, eine Ebene höher.
  - `event Action<Kartenereignis>? Gemeldet`
  - `void Melde(Kartenereignis ereignis)`
- `Einflugmarke` (Operation, pure) — rechnet die Beschriftung aus Ereignis, Kontributorenliste, eigenen Vorgängen und einer hereingereichten Uhr; `null`, wenn keine Marke gehört wird.
  - `static string? Fuer(Kartenereignis ereignis, IReadOnlyList<Kontributor> kontributoren, DateTimeOffset jetzt)`

**Kein Interface** für `Ereignisverteiler`, `Ereignisleitung` oder `Ereignisdrehscheibe`: es gibt je Aufgabe genau eine Implementation (C25). **Keine** neue Klasse in `KanbanC.BL`.

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `Contracts/Karten/Kartenlage` | dritter Parameter `long? Kontributor = null` — **mit Vorgabewert**, damit die `R00007`-Suite und jeder bestehende Aufruf unangetastet bleiben |
| `WebApi/Endpunkte/KartenEndpunkte` | `VerschiebeKarte` bekommt Zugang zu Kopfzeilen und `Ereignisdrehscheibe` und meldet **nach erfolgreichem** `KartenService.VerschiebeKarte` |
| `WebApi/Program.cs` | `Ereignisdrehscheibe` als Singleton, `EreignisEndpunkte.Registriere` |
| `Blazor/Program.cs` | `DefaultRequestHeader` `X-KanbanC-Weg: oberflaeche` am `HttpClient "KanbanC"`; `Ereignisverteiler` als Singleton, `Ereignisleitung` als `HostedService` |
| `Blazor/Components/Pages/Board.razor` | meldet sich beim Verteiler an und ab (`IDisposable`), filtert auf `BoardId`, ruft `LadeBoard` über `InvokeAsync`; hält die Meldung während eines laufenden Zugs zurück; führt Marken und ihre Zeitgeber; vermerkt die eigene Bewegung **vor** dem Aufruf. `_gewaehlteKontributorId` geht in die `Kartenlage` |
| `Blazor/Components/Spalten/Spaltenbahnen.razor(.css)` | meldet Beginn und Ende des Zugs nach oben (Muster `KarteWurdeAbgelegt`); zeigt die zurückgehaltene Meldung |
| `Blazor/Components/Karten/Karte.razor(.css)` | Akzentkante, Schatten und Fußzeile der Marke |
| `Blazor/Components/Pages/Kartendetail.razor` | meldet sich beim Verteiler an und ab, filtert auf `KarteId`, ruft den bestehenden Ladeweg über `InvokeAsync`; hält bei `_offenesFeld` zurück |
| `Blazor/wwwroot/gestaltung.css` | Token der Marke |

**Nicht geändert:** `KanbanC.BL` in Gänze, `Laufzeitmelder` (bleibt und wird nicht ersetzt), die Behandlung zurückgewiesener Züge in `Board.razor` (`B0380`, bereits gebaut).

## Tests

Nach Skill `test-pyramide`, jeder Test nach Skill `test-ehrlichkeit`.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Einflugmarke` (`KanbanC.Blazor.Tests`) — Mensch ohne Weg, Agent mit Weg, unbekannter Urheber, `null`-Urheber, eigene Handlung ergibt `null`, „gerade eben" / „vor 3 Sek" über die hereingereichte Uhr.
- Die Ableitung des `Ereignisweg` aus den Kopfzeilen (`KanbanC.WebApi.IntegrationTests` oder als Operation dort, wo sie wohnt) — Kopf gesetzt, Kopf fehlt, Kopf mit fremdem Wert.
- `Ereignisverteiler` (`KanbanC.Blazor.Tests`) — anmelden, melden, abmelden; ein abgemeldeter Hörer bekommt nichts mehr.

**Probe vor allem anderen (`B0369`, Skill `dependency-probe`):** ein Integrationstest, der **vier Annahmen** prüft, bevor irgendetwas darauf gebaut wird — (1) `TypedResults.ServerSentEvents` schreibt jedes Element sofort und puffert nicht bis zum Ende; (2) `SseParser` liefert ein Element, sobald es kommt; (3) `HttpCompletionOption.ResponseHeadersRead` ist nötig, sonst blockiert `SendAsync` bis zum Strom-Ende; (4) ein abgerissener Strom endet als Ausnahme und nicht still. **Fault-Injection:** die WebApi anhalten, während der Strom offen ist, und den Fehlertyp festhalten. Zusätzlich zu klären, ob `WebApplicationFactory` einen offenen Strom bedienen kann, **ohne den Testlauf zu hängen** — sonst gehört die Prüfung nach `KanbanC.PlaywrightTests` mit zwei echten Prozessen. **Der Probe-Test bleibt als Integrationstest liegen.**

**Integration (`KanbanC.WebApi.IntegrationTests`):** `GET /api/ereignisse` antwortet 200 mit `text/event-stream`; eine erfolgreiche Bewegung erzeugt genau ein Ereignis mit allen sechs Feldern; **zwei gleichzeitige Abonnenten** bekommen dasselbe; eine **zurückgewiesene** Bewegung meldet nichts; ohne Bewegung bleibt der Strom offen und leer; der Kopf `X-KanbanC-Weg` steuert das Feld `Weg`, sein Fehlen ergibt `Api`; der `Kontributor` aus dem Rumpf steht als Urheber, ohne ihn steht `null` und die Bewegung gelingt trotzdem; der **Routentabellen-Test** zählt die neue Route mit. **Jeder Test mit Zeitschranke, keiner mit fester Pause.**

**Blazor-Tests (unterhalb E2E, `KanbanC.Blazor.Tests`):** `Ereignisleitung` — ein abgerissener Strom führt zu einem neuen Versuch und nicht zum stillen Ende; ein Element aus dem Strom erreicht den Verteiler; ein Strom, der nie etwas liefert, hält nichts an. Diese Pfade sind über den Browser nicht auslösbar — genau der Grund, aus dem es dieses Testprojekt gibt.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** drei Läufe, und **zwei Browserkontexte sind neu in diesem Projekt** — bisher hat kein Test zwei Sichten zugleich gehalten.
1. **Das Board zieht nach** — Kontext A bewegt eine Karte, Kontext B zeigt sie ohne Zutun an der neuen Stelle, die Bahnenzahlen stimmen; **derselbe Fall über einen `PUT …/lage` ohne Browser** (`HttpClient` gegen die WebApi). Erst beide Fälle zusammen tragen das Fertig-Kriterium.
2. **Die Einflugmarke** — Marke mit „über die API" nach dem `PUT`; Marke mit Namen und ohne Weg nach der Bewegung aus Kontext A; **keine Marke** nach der eigenen Bewegung. Der dritte Fall ist der, den ein Test am leichtesten übersieht und der am meisten wehtut: ohne ihn machte jeder Klick eine Benachrichtigung über sich selbst.
3. **Die offene Kartenseite** — die Spaltenangabe zieht nach und trägt die Marke; **mit offenem Beschreibungsfeld und ungesendetem Text bleibt der Text stehen und die Meldung wartet.** Der zweite Fall ist der teure und der wichtige.

Die Locator wachsen in `BoardSeite` und `KartendetailSeite`; ein neues Seitenobjekt entsteht nicht.

**Repositories, DAL-Klassen und alles mit Datenbank-Abhängigkeit sind keine Unit-Test-Kandidaten** — hier entsteht davon ohnehin nichts.

## Abhängigkeiten

- Abhängig von: **`R00007`** (Karte verschieben — `I0012`, **grün**). Das ist genau der eine Knoten der WBS-Spalte `Braucht` von `I0028`; er ist erfüllt, der Slice ist **frei**. Ohne eine Bewegung gäbe es nichts zu melden — und `Kartenlage` und die Lageroute, die dieser Slice erweitert, stammen von dort.
- Setzt außerdem auf: **`R00003`** (`I0002` — Board als eigene Seite, `LadeBoard`), **`R00008`** (`I0012`-Ausbau — Einfügelinie und der Zugzustand `_laufenderZug`, ohne den die Warteregel keinen Anker hätte), **`R00009`** (`Bahnenkopfzahl`), **`R00013`** (Identität wählen — ohne sie gäbe es keinen Urheber), **`R00011`**/**`R00014`** (Kontributor mit Stilllegung — die Liste, aus der die Marke ihren Namen holt), **`R00017`** (Kartenseite `/karten/{KarteId}` und `_offenesFeld`), **`R00005`** (`gestaltung.css`), **`R00030`** (`Laufzeitmelder` als Muster für den Verteiler). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **`I0029`** („Aufschließen nach dem Abbruch", `D0007`) — es ist der einzige Knoten, dessen Gegenstand ohne diesen hier nicht existiert. Ein Abriss lässt sich erst aufschließen, wenn es eine Leitung gibt, die abreißen kann.
- **`D0007` wird mit diesem Slice nicht grün** — `I0029` bleibt offen. Das ist der erste Dialog dieses Laufs, der mit seinem ersten Slice nicht fertig wird.

## Umfang

```
Änderung ohne Reload sehen (I0028) = 20 Bubbles: 10 Standard (10,4h), 10 unklar (12,0–27,5h).
Rest: 10,4h klar + 12,0–27,5h unklar · 0 von 20 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 20 Bubbles gruen (0 %) · 0 laufen · 20 offen
```

**21 Zeilen, 20 zählende:** `B0380` („Das Ablegen auf eine verschwundene Stelle") steht auf **`bestehend`** und zählt weder in Zähler noch Nenner — die Behandlung zurückgewiesener Züge ist in `Board.razor` bereits gebaut. Sie steht in der WBS, damit niemand sie ein zweites Mal baut.

`I0028` ist vollständig bis zur Bubble geplant und trägt seine Bubbles — **anders als alle Slices seit `I0020` — in vier Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0044` Der Rückweg entsteht | `B0369`–`B0377` (9) | 5 (2,0h) | 4 (4,8–11,0h) | `I0012` |
| `F0045` Das offene Board zieht nach | `B0378`–`B0381` (4, davon 1 `bestehend`) | 2 (4,0h) | 1 (2–4h) | `F0044` |
| `F0046` Die Einflugmarke | `B0382`–`B0386` (5) | 2 (2,4h) | 3 (2,8–7,0h) | `F0045` |
| `F0047` Die offene Kartenseite zieht nach | `B0387`–`B0389` (3) | 1 (2,0h) | 2 (2,4–5,5h) | `F0046` |

**Warum vier Features und nicht ein Slice:** weil vier Aspekte **getrennt fertig** werden. Anders als bei `I0020` bis `I0027` teilen sie weder Datenquelle noch Komponente noch E2E-Weg — `F0044` hat **gar keine Oberfläche** und ist ohne sie prüfbar (Integrationstests plus `KanbanC.Blazor.Tests`). Die Reihenfolge ist die der Spalte `Braucht` und zugleich eine Kette: ohne Rückweg zieht kein Board nach, ohne nachziehendes Board gibt es keine Marke, ohne Marke fehlte der Kartenseite die halbe Zusage.

**Die Requirement-Klammer sitzt allein an `I0028`** und nicht an den Features: die vier gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

| Bubble | Art | Aufwand |
|---|---|---|
| `B0369` Probe: Ereignisstrom über die Prozessgrenze | Probe (`dependency-probe`) | 0,4–1,5h (**unklar**) |
| `B0370` Der Ereignisvertrag | Contracts | 0,4h (Richtwert) |
| `B0371` Die Ereignisdrehscheibe | Integration (WebApi) | 0,4–1,5h (**unklar**) |
| `B0372` Der Ereignisendpunkt | Integration (WebApi) | 2–4h (**unklar**) |
| `B0373` Der Weg reist im Kopf mit | Operation | 0,4h (Richtwert) |
| `B0374` Der Urheber reist im Rumpf mit | Contracts | 0,4h (Richtwert) |
| `B0375` Die Bewegung meldet sich | Integration (WebApi) | 0,4h (Richtwert) |
| `B0376` Die Ereignisleitung der Oberfläche | Integration (Blazor) | 2–4h (**unklar**) |
| `B0377` Der Ereignisverteiler im Kreislauf | Integration (Blazor) | 0,4h (Richtwert) |
| `B0378` Das offene Board hört zu | UI | 2h (Richtwert) |
| `B0379` Die fremde Bewegung wartet auf das Loslassen | UI | 2h (Richtwert) |
| `B0380` Das Ablegen auf eine verschwundene Stelle | **bestehend** | — (zählt nicht) |
| `B0381` E2E Das Board zieht nach | E2E | 2–4h (**unklar**) |
| `B0382` Die Einflugmarke rechnen | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0383` Die eigene Handlung bekommt keine Marke | UI | 0,4–1,5h (**unklar**) |
| `B0384` Die Marke an der Karte | UI | 2h (Richtwert) |
| `B0385` Die Marke verschwindet von selbst | UI | 0,4–1,5h (**unklar**) |
| `B0386` E2E Die Einflugmarke | E2E | 2–4h (**unklar**) |
| `B0387` Die offene Kartenseite hört zu | UI | 2h (Richtwert) |
| `B0388` Offenes wird nie ausgetauscht | UI | 0,4–1,5h (**unklar**) |
| `B0389` E2E Die offene Kartenseite zieht nach | E2E | 2–4h (**unklar**) |

Mit **20 zählenden Bubbles** ist das der zweitgrößte Slice des Projekts nach `I0015` (30) und deutlich über `I0023` und `I0027` (je 11). **Die Hälfte ist unklar** — der höchste Anteil aller bisherigen Slices, und das mit Grund: **fünf Bubbles bauen etwas, das im Projekt kein Vorbild hat.** `B0369` prüft eine Technik, die hier noch nie eingesetzt wurde; `B0372` ist der erste Endpunkt, der nicht antwortet und schließt; `B0376` ist der erste `BackgroundService` der Blazor-Anwendung und muss eine Wiederaufnahme haben, die nicht wackelt; `B0383` hat ein Wettrennen zwischen Antwort und Ereignis zu klären; `B0385` braucht einen Zeitgeber je markierter Karte, den ein Test kürzen kann. Dazu **drei E2E-Bubbles** (`B0381`, `B0386`, `B0389`), von denen jede **zwei Browserkontexte** oder einen browserlosen `HttpClient` mitbringt — beides ist in `KanbanC.PlaywrightTests` neu.

Derselbe Vermerk wie bei `I0005` bis `I0027`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. **Für die fünf vorbildlosen Bubbles trägt sie ohnehin nicht** — dort ist der Richtwert eine Vermutung, keine Erfahrung. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0028` trägt keine eigene Zählzeile; sie hält die zentrale Entscheidung, die fünf verworfenen Alternativen und die Abgrenzung zu `I0029` fest. Die Zahlen oben sind über die Aufwandsspalte der 21 Bubble-Zeilen gezählt, `B0380` ausgenommen.

## Offene Fragen

- **Wie entsteht der Rückweg von der WebApi in die Oberfläche?** — **entschieden: als Ereignisstrom (Server-Sent Events) auf `GET /api/ereignisse`, eine Leitung je Blazor-Prozess.** Fünf Alternativen geprüft. **Die zwei stärksten Gründe:** ein **Abfragetakt sieht nur den neuen Zustand, nie den Urheber** — die Einflugmarke wäre unbaubar, und `I0027`/`B0366` hat den Takt bewusst vermieden; ein **HTTP-Rückruf kehrte die Abhängigkeitsrichtung um** — die WebApi müsste die Adresse der Oberfläche kennen und scheiterte ohne sie, während sie heute von der Oberfläche **nichts** weiß, und genau das ist die Ordnung, die die Kernregel trägt. **Die Umkehrung** wäre entweder eine Live-Aktualisierung ohne Urheber oder eine API, die ihren Klienten konfiguriert bekommen muss. **Nicht am Menschen geprüft.**
- **Wird der Ereignisstrom mit einem neuen Paket gebaut?** — **entschieden: nein, mit null neuen Paketen.** `TypedResults.ServerSentEvents` in `Microsoft.AspNetCore.App.Ref/10.0.0`, `SseParser` in `System.Net.ServerSentEvents.dll` — nachgesehen. Ein SignalR-Hub bräuchte `Microsoft.AspNetCore.SignalR.Client` und stellte ein zweites SignalR neben das des Browsers. **Nicht am Menschen geprüft.**
- **Trägt das Ereignis die neue Karte oder nur das Signal?** — **entschieden: nur das Signal**, jede Sicht holt über ihren bestehenden Klienten neu. So entsteht kein zweiter Leseweg, und die Sicht zeigt genau den Stand, den ein Reload auch zeigte. **Preis:** je offener Sicht ein Abruf je Bewegung. **Nicht am Menschen geprüft.**
- **Wie reisen Weg und Urheber?** — **entschieden: der Weg im Kopf `X-KanbanC-Weg`, an genau einer Stelle gesetzt; der Urheber im Rumpf als `long? Kontributor` mit Vorgabewert.** **Fehlt der Kopf, gilt `Api`** — ein Agent muss nichts setzen. **Nur die Kontributor-Id**, nicht der ganze Kontributor: den Namen löst die Sicht auf, damit ein Umbenennen nachzieht. **Nicht am Menschen geprüft.**
- **Wo entsteht die Meldung?** — **entschieden: im Endpunkt, nicht im `KartenService`.** Der Weg steht nur in der Anfrage, und `KanbanC.BL` bleibt frei von Abonnenten. **Eine zurückgewiesene Bewegung meldet nichts.** **Preis, benannt:** wer die BL an der WebApi vorbei ruft, erzeugt kein Ereignis — einen solchen Weg gibt es heute nicht. **Nicht am Menschen geprüft.**
- **Was zieht nach?** — **entschieden: allein die Kartenbewegung.** Das Fertig-Kriterium sagt „bewegt eine Karte". Ausdrücklich draußen: Kartenänderung, Kommentare, Etiketten, Teilaufgaben, Anhänge, Dateiverweise, Klassen, Farbe, **Archivierung** (keine Bewegung, kein Knoten), Spalten und Boards, **Zeitereignisse**, **Ereignisspur und Laufband**. **Die Umkehrung** wäre ein Slice, der alles nachzieht und dessen Fertig-Kriterium niemand prüfen kann. **Nicht am Menschen geprüft.**
- **Gibt es ein Angebot über einem offenen Feld?** — **entschieden: nein.** Eine Bewegung fasst kein Feld an; ein Angebot ohne Anlass wäre tote Flexibilität (C24). Gebaut wird die **Warteregel** (`B0388`) über den vorhandenen Schalter `_offenesFeld` — gebraucht auch dann, wenn kein Feld betroffen ist, weil der Neuabruf `_detail` ersetzt und die Textarea aus `_detail.Karte.Beschreibung` rendert. **Sichtbare Abweichung vom Artboard, Zustand 2** (dort ist die Trennung „zieht nach" / „wird angeboten" gezeichnet). **Nicht am Menschen geprüft.**
- **Wie lange steht eine Marke?** — **entschieden: etwa zehn Sekunden, als Größenordnung.** Eine bleibende Marke wäre ein Verlauf, und den führt `D0007` nicht. **Die eigene Handlung bekommt keine Marke**, und der Vermerk hängt **am Kreislauf, nicht an der Identität** — sonst schwiege ein zweiter Browser derselben Person mit. Die Standzeit gehört an eine Stelle, an der ein Test sie kürzen kann. **Nicht am Menschen geprüft.**
- **Gehört die Wiederaufnahme der Leitung zu `I0028` oder zu `I0029`?** — **entschieden: zu `I0028` (`B0376`).** Ohne sie wäre das Fertig-Kriterium schon nach dem ersten WebApi-Neustart falsch. **Nachgeholt wird nichts** — der verpasste Stand, der Abbruch Browser ↔ Blazor samt deutschem `ReconnectModal` (heute steht dort die englische Vorlagenfassung), die alternde Kopfzeile und die Zusammenfassung mehrerer verpasster Änderungen sind `I0029`. **Kein Ereignisspeicher und keine Folgenummer** in diesem Slice — beides wäre tote Flexibilität; `I0029` schließt über einen vollständigen Neuabruf auf, den der Bestand schon kann. **Nicht am Menschen geprüft.**
- **Löst dieser Slice die mitlaufende Dauer ein?** — **entschieden: nein, und das ist eine Planungslücke, keine Auslassung dieser Anforderung.** Der Kanal räumt den *einen* Grund weg („ohne Live-Kanal ab der ersten Sekunde falsch"), nicht den *zweiten* aus `B0326`/`B0362`: ein Sekundentakt läuft über einen **eigenen Zeitgeber je Kreislauf** und bleibt der E2E-Wackelkandidat, der er war. **Drei Zusagen des Artboards bleiben damit offen** — die mitlaufende Dauer, die laufende Zeit in der Ist-Summe und die ohne Ladeanlass mitwachsende Kopfzeilenplakette. **Für keine der drei gibt es einen WBS-Knoten.** Sie brauchen einen eigenen Slice unter `D0006` oder eine Ausbaustufe an `I0026`; dieser Lauf legt ihn nicht an, weil er außerhalb des Teilbaums von `I0028` läge. **Zur Entscheidung vorzulegen.**
- **Wird `WebApplicationFactory` einen offenen Strom bedienen können, ohne den Testlauf zu hängen?** — **offen und bewusst nicht entschieden.** Genau das klärt die Probe `B0369`. Antwort „nein" verschiebt die Prüfung nach `KanbanC.PlaywrightTests` mit zwei echten Prozessen und macht `B0372` teurer. Das ist die eine Frage, die den Umfang dieses Slice noch bewegen kann.
- **Hängt die Warteregel der Kartenseite am Feld oder an der Seite?** — **offen und bewusst nicht entschieden.** Eine Frage der Zuständigkeit innerhalb `Kartendetail.razor`, keine der Fachlichkeit; sie gehört in die Umsetzung.

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration und keine neue Konfiguration.

## Manuelle Nachbereitungstätigkeiten

- Keine. Nach dem Deployment nehmen offene Sichten den Kanal ohne Zutun auf.
- **Betriebshinweis, kein Handgriff:** Reverse Proxies puffern `text/event-stream` gelegentlich. Der Betrieb ist Einzelrechner bzw. LAN ohne Proxy (Leitplanke der Vision), deshalb ist hier nichts zu tun — der Hinweis steht für den Fall, dass später einer dazwischenkommt.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist ein Board, das seine eigene Wirklichkeit nicht kennt: `Board.razor` lädt beim Aufbau und nach der **eigenen** Handlung, und die Kopfzeile aus `R00030` hat vier Ladeanlässe und eine ausdrücklich benannte Lücke — was **jemand anders** tut, bleibt unsichtbar, bis jemand neu lädt. Genau in diese Lücke arbeitet die Vision hinein, indem sie den Agenten als gleichberechtigten Akteur an dasselbe Board setzt: der Agent bewegt Karten über die API, während der Mensch danebensteht und ein veraltetes Bild bedient. Die Kausalkette: **wenn** die WebApi jede erfolgreiche Bewegung auf einen offenen, für jeden abonnierbaren Strom meldet und die Oberfläche als gewöhnlicher Klient daran hängt (X), **dann** erfährt jede offene Sicht von jeder fremden Bewegung, ohne dass jemand fragt oder nachlädt, und holt sich über ihren bestehenden Leseweg genau den Stand, den ein Reload auch zeigte (Y), **und dann** hört das Board auf, eine Momentaufnahme zu sein, und wird der gemeinsame Arbeitsplatz, den die Vision meint — mit der Einflugmarke sogar einer, an dem ablesbar ist, wer und was gehandelt hat (Z). **Der Hebel liegt beim Rückweg und nicht bei einer schnelleren Sicht:** eine Sicht, die häufiger nachfragt, wäre ein Abfragetakt — er sähe den neuen Zustand, aber nie den Urheber, und machte die Marke unbaubar. Und er liegt **nicht** beim Nachholen: ein Kanal, der zuerst alles Verpasste aufholen wollte, bräuchte einen Ereignisspeicher, bevor er auch nur ein einziges Ereignis zugestellt hat — deshalb baut dieser Slice den Kanal samt Wiederaufnahme und überlässt das Aufschließen `I0029`.

## Missing-Docs

- **Server-Sent Events in ASP.NET Core 10 (`TypedResults.ServerSentEvents`) und `System.Net.ServerSentEvents.SseParser`:** beide sind im Framework enthalten, aber im Repository ohne Vorbild. Ungeklärt aus der Dokumentation: ob der Endpunkt jedes Element sofort schreibt, ob der Parser sofort liefert, welche Ausnahme ein Abriss wirft und wie sich das Ganze in `WebApplicationFactory` verhält. Genau diese vier Punkte klärt die Probe `B0369` — das ist die Lücke, die sie füllt.
- **`BackgroundService` in einer Blazor-Server-Anwendung, der in Kreisläufe hineinmeldet:** die Fadengrenze zwischen Hintergrunddienst und Renderfaden (`InvokeAsync`), die Abmeldedisziplin gegen tote Kreisläufe und die Frage, ob ein `HostedService` beim Herunterfahren sauber endet, sind im Repository nicht vorgemacht. `Laufzeitmelder` ist das nächste Vorbild und arbeitet ausschließlich innerhalb eines Kreislaufs.
- **Ein Singleton als Ereignisquelle über Kreisläufe hinweg:** `Identitaetsspeicher.Gewechselt` und `Laufzeitmelder.Gemeldet` sind beide `AddScoped`. Ein Singleton mit Ereignis, an dem sich beliebig viele Kreisläufe an- und abmelden, ist die erste Verwendung dieses Musters und hat im Projekt keine Vorlage für Nebenläufigkeit und Abräumen.
- **Zwei Browserkontexte in einem Playwright-Test (NUnit):** bisher hält kein Test dieses Projekts zwei Sichten zugleich. Wie zwei Kontexte gegen dieselben zwei Prozesse aufgebaut, aufgeräumt und auf ein Ereignis ohne feste Pause synchronisiert werden, ist nicht belegt.

## Notizen

### Verworfene Alternativen

- **Ein Abfragetakt (Polling) aus jedem Kreislauf** — er sieht nur den neuen Zustand, nie den Urheber; die Einflugmarke wäre unbaubar. Dazu N Sichten = N Takte, und `I0027`/`B0366` hat den Takt an derselben Anwendung schon bewusst vermieden.
- **Ein HTTP-Rückruf der WebApi an die Oberfläche** — kehrt die Abhängigkeitsrichtung um; die WebApi müsste die Adresse der Oberfläche kennen und scheiterte ohne sie. Heute weiß sie von der Oberfläche nichts, und das ist die Ordnung, die die Kernregel trägt.
- **Beide Prozesse lesen dieselbe SQLite** — verboten durch die Kernregel: `KanbanC.Blazor` hat keine Referenz auf `KanbanC.BL` und darf die Datei nicht öffnen.
- **Eine Ereignistabelle in der Datenbank** — löst den Transport nicht (jemand müsste sie abfragen), kostet eine Migration, und ihre Haltbarkeit wird erst von `I0029` gebraucht.
- **Ein SignalR-Hub in der WebApi** — bräuchte `Microsoft.AspNetCore.SignalR.Client` als neues Paket und ein zweites SignalR neben dem, das der Browser schon spricht.
- **Die neue Karte im Ereignis mitschicken** — spart einen Abruf und handelt sich zwei Wege ein, auf denen eine Bahn entsteht; der zweite wäre der, den niemand testet.
- **Den ganzen Kontributor im Ereignis** — kostete auf dem Bewegungsweg einen zusätzlichen Lesezugriff, und der Name wäre ab dem nächsten Umbenennen falsch.
- **Den Urheber im Kopf statt im Rumpf** — er ist eine Eigenschaft der Handlung, nicht des Aufrufers; jeder andere Kontributor dieses Projekts reist im Rumpf.
- **Den Weg an jeder Aufrufstelle setzen** — eine Stelle vergessen und die Marke lügt; am benannten `HttpClient` gibt es nur eine Stelle.
- **`Oberflaeche` als Voreinstellung bei fehlendem Kopf** — dann müsste jeder Agent etwas setzen, um korrekt genannt zu werden; die Oberfläche ist die Ausnahme, die sich meldet.
- **Die Meldung im `KartenService`** — der Weg steht dort nicht mehr zur Verfügung, und `KanbanC.BL` bekäme Abonnenten.
- **Auch die zurückgewiesene Bewegung melden** — eine Nachricht über etwas, das nicht geschehen ist.
- **Eine Leitung je Blazor-Kreislauf** — zehn Browser erzeugten zehn Ströme zur WebApi für dieselbe Information.
- **Den `Laufzeitmelder` durch den Verteiler ersetzen** — der Melder ist der Weg für die **eigene** Handlung ohne Umweg über die WebApi; er ist schneller und hängt nicht am Kanal.
- **Eine Folgenummer und ein Ereignisspeicher schon hier** — tote Flexibilität (C24); `I0029` schließt über einen vollständigen Neuabruf auf, den der Bestand schon kann.
- **Die Wiederaufnahme der Leitung nach `I0029` schieben** — das Fertig-Kriterium wäre nach dem ersten WebApi-Neustart falsch.
- **Die Karte oben oder unten in der Zielbahn einfügen** — eine Live-Aktualisierung, die die Reihenfolge selbst wählte, wäre eine zweite Wahrheit neben der, die `I0012` pflegt.
- **Die fremde Bewegung während eines laufenden Zugs sofort einspielen** — die Einfügelinie zeigte auf eine Stelle, die es beim Loslassen nicht mehr gibt.
- **Ein Angebot über einem offenen Feld** — eine Bewegung fasst kein Feld an; ein Angebot ohne Anlass wäre tote Flexibilität.
- **Die Marke über die Farbe unterscheiden (Mensch/API)** — Olive und Terrakotta tragen in diesem Canvas die Art des Kontributors; eine dritte Bedeutung machte beide unlesbar.
- **Auch der eigenen Handlung eine Marke geben** — jeder Klick machte eine Benachrichtigung über sich selbst.
- **Den Vermerk „war ich selbst" an der Identität statt am Kreislauf festmachen** — dann schwiege ein zweiter Browser derselben Person mit, obwohl er die Änderung sehen soll.
- **Eine bleibende Marke oder eine Ereignisspur an der Bahn** — eine Liste vergangener Ereignisse ist etwas anderes als eine ankommende Änderung, hat keinen WBS-Knoten und ist im Artboard (Rand C) ausdrücklich ausgeschlossen.
- **Ein Laufband über dem Board** — dieselbe Begründung wie die Ereignisspur.
- **Auch Archivierung, Kommentare, Etiketten und Feldänderungen nachziehen** — das Fertig-Kriterium sagt „bewegt eine Karte"; ein Slice, der alles nachzieht, hätte kein prüfbares Ende.
- **Ein neues Seitenobjekt für die E2E-Locator** — `BoardSeite` und `KartendetailSeite` führen die betroffenen Orte bereits; ein zweites wäre eine zweite Adresse für denselben Ort.

### Bewusst out of scope

- **Verpasster Stand nach einer Trennung, Abbruch Browser ↔ Blazor samt deutschem `ReconnectModal`, alternde Kopfzeile, Zusammenfassung mehrerer verpasster Änderungen** — `I0029`/`D0007`. Heute steht in `Components/Layout/ReconnectModal.razor` die englische Vorlagenfassung; sie bleibt in diesem Slice unangetastet.
- **Die mitlaufende Dauer, die laufende Zeit in der Ist-Summe, die ohne Ladeanlass mitwachsende Kopfzeilenplakette** — **kein WBS-Knoten**; brauchen einen Slice unter `D0006` oder eine Ausbaustufe an `I0026`.
- **Nachziehen von Kartenänderungen, Kommentaren, Etiketten, Teilaufgaben, Anhängen, Dateiverweisen, Klassen und Farbe** — je eigener Gegenstand, kein Knoten in `D0007`.
- **Nachziehen der Archivierung** — keine Bewegung, kein Knoten; `D0007.dc.html`, Rand B.
- **Nachziehen von Spalten- und Boardänderungen** — `D0001`, `D0003`.
- **Ereignisspur und Laufband** — `D0007.dc.html`, Rand C, ausdrücklich nicht gezeichnet.
- **Eine zugesagte Zeitschranke („innerhalb von n ms")** — der Weg hängt an drei Übergängen, von denen keiner gemessen ist; eine Zahl wäre eine Zusage ohne Grundlage.
- **Ein Filter, welche Ereignisse ein Abonnent bekommen will** — bei einer Handvoll Sichten im LAN wäre er Bedienung ohne Anlass; jede Sicht filtert selbst.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Der Rückweg ist ein Ereignisstrom (SSE) auf `GET /api/ereignisse`**, eine Leitung je Blazor-Prozess — fünf Alternativen verworfen, **null neue Pakete**.
2. **Signal statt Nutzlast** — jede Sicht holt über ihren bestehenden Klienten neu; Preis ist ein Abruf je Sicht und Bewegung.
3. **Der Weg reist im Kopf `X-KanbanC-Weg`**, an genau einer Stelle gesetzt; **fehlt er, gilt `Api`**.
4. **Der Urheber reist im Rumpf** als `long? Kontributor` **mit Vorgabewert** — nur die Id, nicht der ganze Kontributor.
5. **Die Meldung entsteht im Endpunkt**, nicht im `KartenService`; eine zurückgewiesene Bewegung meldet nichts.
6. **Nur die Kartenbewegung zieht nach** — alles andere ausdrücklich draußen.
7. **Kein Angebot über einem offenen Feld** — gebaut wird die Warteregel; **sichtbare Abweichung vom Artboard, Zustand 2.**
8. **Die Marke steht etwa zehn Sekunden** (Größenordnung), die eigene Handlung bekommt keine, und der Vermerk hängt am Kreislauf statt an der Identität.
9. **Die Wiederaufnahme der Leitung gehört zu `I0028`**, das Nachholen zu `I0029`; **kein Ereignisspeicher, keine Folgenummer.**
10. **Die drei offenen Artboard-Zusagen (mitlaufende Dauer, laufende Zeit in der Ist-Summe, mitwachsende Kopfzeilenplakette) bleiben ungeplant** — für keine gibt es einen WBS-Knoten. **Das ist die einzige Annahme, die eine Planungsentscheidung außerhalb dieses Slice verlangt.**
