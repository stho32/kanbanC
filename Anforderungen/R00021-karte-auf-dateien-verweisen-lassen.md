---
id: R00021
status: In Arbeit
datum: 2026-09-06
---

# R00021: Karte auf Dateien verweisen lassen

## Beschreibung

Eine Karte trägt **Dateiverweise**: Pfade auf Dateien, die nicht in der Anwendung liegen, sondern im Repository daneben — `Dokumentation/Planung/kanbanc.md`, `Anforderungen/R00000-vision.md`, ein Architekturdokument. Sie stehen untereinander in der Reihenfolge ihres Entstehens, je Zeile der Pfad, ein `↗`, das ihn in die Zwischenablage legt, und ein `×`, das ihn wieder entfernt. Eingetragen wird über `POST /api/karten/{karteId}/dateiverweise`, entfernt über `DELETE /api/karten/{karteId}/dateiverweise/{dateiverweisId}`; beide antworten mit HTTP 200 und dem **ganzen `Kartendetail`** — dieselbe Antwortgestalt, die `R00017` für diese Seite festgelegt und `R00018` bis `R00020` fortgeführt haben. In der Oberfläche steht der Abschnitt „Dateiverweise" auf `/karten/{karteId}` als **rechte** Hälfte der zweispaltigen Sektion, deren linke `R00020` gebaut hat.

Zahlt ein auf: [Vision](R00000-vision.md) — „Kein Ersatz für die Anforderungs- und Planungs-Commands. Vision, Anforderungen und WBS bleiben als Markdown-Dokumente die Wahrheit; das Board führt den Arbeitsfluss." Der Dateiverweis ist genau die Klammer zwischen beidem. Und: „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat."

**Der Unterschied zum Anhang daneben ist der ganze Zweck.** Ein Anhang bringt die Datei mit; ein Dateiverweis zeigt auf eine Datei, die dort bleibt, wo sie hingehört und weiterlebt. Eine WBS-Datei, die im Repository gepflegt wird, will nicht als Kopie an der Karte hängen — die Kopie veraltet am Tag ihres Entstehens. Deshalb stehen beide Hälften nebeneinander und unterscheiden sich sichtbar: Schreibmaschinenschrift an oliver linker Kante gegen Büroklammer und Größenangabe.

**Dies ist der letzte Slice von `D0004` „Karteninhalt pflegen".** Mit ihm wird der dritte Dialog der Anwendung grün.

## Geschäftlicher Nutzen

Die Karte kann seit `R00017` sagen, **was** zu tun ist, seit `R00018`, **woraus** die Arbeit besteht, seit `R00019`, **wer etwas dazu gesagt hat**, und seit `R00020`, **was sie mitbringt**. Sie kann bis heute nicht sagen, **worauf sie sich bezieht**.

Für den Menschen heißt der Dateiverweis: der Weg von der Karte zur Anforderung, zur WBS-Zeile oder zum Architekturdokument steht an der Karte statt im Gedächtnis. Für den KI-Agenten heißt er mehr — er ist die Stelle, an der das Board und die Markdown-Wahrheit dieses Projekts aneinander andocken, ohne dass eine der beiden die andere kopiert. Ein Agent, der `GET /api/karten/14` aufruft, bekommt mit der Aufgabe zugleich die Dateien geliefert, die er dafür lesen muss — und das ist genau die Bewegung, die die Vision mit dem WBS-Import vorhat.

Das **Entfernen** kommt aus einem Grund mit, der hier ein anderer ist als beim Anhang: die Anwendung prüft ausdrücklich **nicht**, ob ein Pfad noch stimmt (siehe „Nicht-funktionale Anforderungen"). Sie kann den Verfall also nicht selbst bemerken. Dateien werden umbenannt, verschoben, gelöscht; eine Liste, aus der nichts herausgeht, behauptet nach einiger Zeit Verweise, die ins Leere zeigen — und wäre damit schlechter als keine. Der Einzige, der den Verfall sieht, ist der Mensch, also braucht er das Bedienelement.

## Funktionale Anforderungen

- `POST /api/karten/{karteId}/dateiverweise` nimmt einen **Pfad** und die `KontributorId` des Urhebers im JSON-Rumpf entgegen und antwortet mit dem vollständigen `Kartendetail`.
- `DELETE /api/karten/{karteId}/dateiverweise/{dateiverweisId}` entfernt die Zeile und antwortet mit dem vollständigen `Kartendetail`.
- Das `Kartendetail` trägt die Dateiverweise der Karte, sortiert nach **Zeitpunkt** (ältester oben); die Reihenfolge ist die Zeitordnung und wird nicht getrennt gespeichert.
- Jeder Dateiverweis trägt eine eigene Nummer (`DateiverweisId`), den **Pfad**, den **ganzen** Urheber (Nummer, Name, Art, Stilllegungsstand) und seinen `Zeitpunkt`.
- Der Zeitpunkt wird beim Eintragen von der Anwendung gesetzt, nicht vom Aufrufer mitgegeben.
- Der Pfad wird an den Rändern **getrimmt** und sonst **unverändert** gespeichert — keine Umschreibung von Trennzeichen, keine Normalisierung, keine Kürzung.
- Ein **leerer** Pfad und ein **zu langer** Pfad werden mit Befund zurückgewiesen; es entsteht keine Zeile.
- **Derselbe Pfad zweimal an derselben Karte** wird mit Befund zurückgewiesen — anders als bei Anhang, Teilaufgabe und Kommentar.
- Derselbe Pfad an **zwei verschiedenen** Karten ist erlaubt.
- Eine **unbekannte Karte**, ein **unbekannter Dateiverweis** und ein **unbekannter Kontributor** werden mit HTTP 404 samt Rumpf beantwortet, ein **stillgelegter** Kontributor mit HTTP 400 samt Rumpf.
- Eine `DateiverweisId`, die es gibt, aber nicht an **dieser** Karte, liefert 404 und entfernt nichts.
- Ein Dateiverweis **ohne** Urheber ist nicht möglich: die Anfrage führt die `KontributorId` als Pflichtwert, die Spalte ist `NOT NULL`.
- Die Kartenseite zeigt den Abschnitt „Dateiverweise" mit je Zeile dem Pfad in Schreibmaschinenschrift an oliver linker Kante, einem `↗` und einem `×`, darunter die Eingabezeile „Pfad im Repository eintragen".
- Der Pfad wird mit der **Eingabetaste** abgeschickt; das Artboard zeichnet keinen Knopf.
- Ein Klick auf `↗` legt den Pfad **in die Zwischenablage**; er navigiert **nicht**.
- Ist **keine Identität gewählt**, ist die Eingabezeile gesperrt und sagt, was zu tun ist; die übrige Anwendung bleibt unverändert benutzbar.
- Hat die Karte **weder Anhang noch Dateiverweis**, steht über beiden Hälften **eine** gemeinsame Zeile; hat sie nur eines von beidem, steht die halbe Zeile der leeren Hälfte; hat sie beides, steht keine.
- Alle Dateiverweise sind nach einem Reload und nach einem Neustart unverändert da.

## Nicht-funktionale Anforderungen

- **Begriff (C06):** Der Gegenstand heißt im ganzen Stack **`Dateiverweis`** — Tabelle, Spaltennamen, Contracts, Route, Leser, Validator, Befundcodes **und Beschriftung**. **Nicht `Verweis`**: das Wort ist im Code als Oberflächenbegriff für einen Hyperlink vergeben (69 Zeilen in 22 Dateien, davon 51 Bezeichner: `Boardverweis`, `VerweisZurListe`, `.board-verweis`, `TitelverweisDerKarte`, `verweisplatz`, `NavigationsVerweise`). `Dateiverweis` hat **0 Treffer** im Code und steht bereits in `R00006:254` und im Wireframe-Index. Zur Abweichung vom Artboard siehe „Gestaltungsvorgabe".
- **Datenhaltung:** `015-kartendateiverweis.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal. Deshalb eine eigene Tabelle statt `ALTER TABLE`, wie bei den Migrationen 004 bis 014.
- **Ein eindeutiger Index auf `(Karte, Pfad)`** — die einzige Stelle, an der dieses Projekt eine Dublette im Schema ausschließt. Begründung unter „Verworfene Alternativen"; die Antwort für den Aufrufer entsteht trotzdem als lesbarer Befund, nicht als nackte Datenbankmeldung.
- **Geprüft werden Leere und Länge, sonst nichts.** Ausdrücklich **nicht** geprüft: ob die Datei existiert, ob der Pfad in einem Repository liegt, ob er relativ oder absolut ist, ob er auf `.md` endet, ob er einem Formmuster folgt. Der Server kennt **keinen** Repository-Wurzelpfad — KanbanC ist ein Board über beliebige Vorhaben und hat kein Arbeitsverzeichnis, und der Klon des Lesenden liegt ohnehin woanders. Eine Prüfung, die nur der Browser des Eintragenden anstellen könnte, wäre über die API nicht wiederholbar und verletzte die Kernregel: **die API muss können, was die Oberfläche kann.**
- **Zeitform:** `DateTimeOffset` in den Contracts, **ISO-8601-Text in UTC** in der Spalte — wie `R00019` und `R00020` und aus denselben Gründen. Der Leser rechnet in C# um; Dapper materialisiert die TEXT-Spalte nicht direkt (dreifach belegt).
- **Unbelegter Boden wird erst belegt:** Die **Zwischenablage ist im ganzen Repository nie benutzt**, und `navigator.clipboard` existiert **nur im sicheren Kontext** — die Anwendung läuft im LAN über `http://<host>:5180`, also außerhalb davon. Vor der ersten produktiven Nutzung steht deshalb ein Probe-Test nach Skill `dependency-probe` **mit Fault-Injection**: der unsichere Fall wird erzwungen, indem `navigator.clipboard` vor dem Klick im Browser weggenommen wird. **Der E2E-Lauf arbeitet auf `127.0.0.1` und ist damit sicher; er würde die Lücke von selbst nie zeigen** — dieselbe Falle, die `R00020` bei der öffentlichen Basisadresse benannt hat. Siehe „Offene Fragen": das verbleibende Restrisiko ist benannt.
- **Rückfall statt Ausnahmeseite:** Fällt die Zwischenablage aus, markiert die Anwendung den Pfad in der Zeile (Textauswahl), statt einen Fehler an den Nutzer durchzureichen. Der Aufruf läuft wie im `Identitaetsspeicher` über `IJSRuntime` mit einem Inline-Ausdruck, **ohne eigene `.js`-Datei**, und fängt den Ausfall wie dort.
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der zwei neuen Routen trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404. **Die Vertragsfälle beider Routen gehören in denselben Arbeitsgang wie die Routen** (`FehlervertragTests.cs:41-58`): der Test liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot.
- **Antwortgestalt:** **200 mit dem ganzen `Kartendetail`** beim Eintragen und beim Entfernen, nicht 201 mit der geschriebenen Zeile — wie `R00018` bis `R00020` und aus demselben Grund.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier **keine** Projektreferenz auf `KanbanC.BL`. Anders als beim Anhang gibt es **keinen Byte-Rückweg und keine Direktadresse an der WebApi** — ein Dateiverweis trägt einen Pfad, keine Bytes; alles läuft über `KartenApiKlient` als JSON.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün, mit den benannten Änderungen (siehe Akzeptanzkriterien). **Eine** davon ändert grünes *Verhalten*: die gemeinsame Leerzeile.

## Akzeptanzkriterien

### Der Dateiverweis wird mit Urheber und Zeitpunkt festgehalten (API)

- [x] `POST /api/karten/{karteId}/dateiverweise` mit einem Pfad und einer `KontributorId` antwortet mit HTTP 200 und einem `Kartendetail`, dessen Dateiverweisliste diesen Pfad als **letzten** Eintrag trägt.
- [x] Der Eintrag trägt eine eigene, von den anderen verschiedene `DateiverweisId`.
- [x] Der Eintrag trägt den Pfad **unverändert bis auf die Ränder**. Rechenbeispiel: `"  Dokumentation/Planung/kanbanc.md  "` wird als `Dokumentation/Planung/kanbanc.md` gespeichert; `Dokumentation\Planung\kanbanc.md` bleibt mit **Rückstrichen** stehen und wird nicht umgeschrieben.
- [x] Der Eintrag trägt den **ganzen** Urheber: Nummer, Name, Art und Stilllegungsstand — nicht nur die Nummer.
- [x] Der Eintrag trägt einen `Zeitpunkt` mit Uhrzeit. Rechenbeispiel: wird die Uhr **vor** dem Aufruf als `t0` und **nach** dem Aufruf als `t1` gemerkt, gilt `t0 ≤ Zeitpunkt ≤ t1`.
- [x] Der Aufrufer kann den Zeitpunkt **nicht** mitgeben; ein mitgeschicktes Feld ändert nichts am gespeicherten Wert.
- [x] `GET /api/karten/{karteId}` liefert danach dieselbe Liste in derselben Reihenfolge.
- [x] Rechenbeispiel Reihenfolge: an eine Karte ohne Dateiverweise werden nacheinander `a.md`, `b.md`, `c.md` eingetragen → die Liste lautet in jedem folgenden Abruf `a.md`, `b.md`, `c.md` (ältester oben).
- [x] Werden zwei Dateiverweise **in der Datenbank** auf verschiedene Zeitpunkte gesetzt, steht der ältere oben — unabhängig von der Reihenfolge des Eintragens.
- [x] Die Dateiverweise hängen am `Kartendetail` und **nicht** an `Karte`: `GET /api/boards/{boardId}` liefert die Karten unverändert ohne Dateiverweisliste.
- [x] Es gibt **kein** gespeichertes Zählfeld und **keine** gespeicherte Position: die Antwort trägt weder eine Anzahl noch eine Ordnungszahl neben der Liste.
- [x] Ein Dateiverweis eines inzwischen **stillgelegten** Kontributors bleibt an der Karte sichtbar, mit Name und Stilllegungsstand.
- [x] Ein Neustart der Anwendung lässt Pfade, Urheber, Zeitpunkte und Reihenfolge unverändert.
- [x] Der zweite Lauf des `Migrationslaeufer` auf einer bestehenden Datei lässt Schema und Daten unverändert.

### Was geprüft wird — und was ausdrücklich nicht

- [x] Ein **leerer** Pfad wird mit HTTP 400 **und Rumpf** zurückgewiesen; es entsteht keine Zeile.
- [x] Ein Pfad, der **nur aus Leerzeichen** besteht, gilt als leer und wird ebenso zurückgewiesen.
- [x] Ein Pfad **über der Höchstlänge** wird mit HTTP 400 und Rumpf zurückgewiesen; der Befund **nennt die Höchstlänge**. Rechenbeispiel: bei einer Höchstlänge von 500 geht ein Pfad aus 500 Zeichen durch, einer aus 501 nicht.
- [x] Ein Pfad auf eine **nicht existierende Datei** wird **angenommen** — die Anwendung prüft das Dasein nicht.
- [x] Ein **absoluter** Pfad (`/home/…`, `C:\…`), ein Pfad **außerhalb jedes Repositorys** und ein Pfad **ohne Endung** werden alle angenommen.
- [x] Ein Windows-Pfad mit Rückstrichen kommt **mit Rückstrichen** zurück; ein Unix-Pfad mit Schrägstrichen mit Schrägstrichen. Die Anwendung schreibt Trennzeichen **nicht** um.
- [x] Die Zurückweisung nennt in der Kompensationsaktion die **Route samt Kartennummer**, wie bei Etikett, Teilaufgabe, Kommentar und Anhang.

### Derselbe Pfad zweimal an derselben Karte wird zurückgewiesen

- [x] Ein zweiter `POST` mit **demselben** Pfad an **dieselbe** Karte antwortet mit HTTP 400 und Rumpf; die Liste bleibt bei **einem** Eintrag.
- [x] Die Prüfung greift auch, wenn sich die beiden Aufrufe nur an den **Rändern** unterscheiden: `"kanbanc.md"` und `" kanbanc.md "` sind derselbe Pfad, weil getrimmt wird.
- [x] Die Prüfung greift auch bei **verschiedenen Urhebern**: derselbe Pfad, von zwei Kontributoren eingetragen, bleibt eine Zeile.
- [x] Zwei Pfade, die sich in der **Groß-/Kleinschreibung** unterscheiden, gelten als **verschieden** — Pfade sind auf der Zielplattform der Vision (Linux-Repositorys) unterschiedlich, und ein Vergleich, der das einebnet, wäre eine Annahme über fremde Dateisysteme.
- [x] Derselbe Pfad an **zwei verschiedenen** Karten wird zweimal angenommen.
- [x] Nach dem Entfernen lässt sich derselbe Pfad **wieder** eintragen.
- [x] Der Befund ist **lesbar** und nennt den doppelten Pfad und die Kartennummer — der Aufrufer trifft **nie** auf eine nackte Datenbankmeldung über einen verletzten Index.
- [x] Das Schema sichert die Regel zusätzlich ab: ein direkter zweiter `INSERT` mit demselben `(Karte, Pfad)` scheitert an der Datenbank.

### Entfernen nimmt die Zeile

- [x] `DELETE /api/karten/{karteId}/dateiverweise/{dateiverweisId}` antwortet mit HTTP 200 und dem `Kartendetail` **ohne** diesen Eintrag.
- [x] Ein zweiter `DELETE` derselben Nummer liefert HTTP 404 mit Rumpf.
- [x] Eine `dateiverweisId` einer **anderen** Karte entfernt nichts und liefert HTTP 404 mit Rumpf; die andere Karte trägt ihren Dateiverweis danach unverändert.
- [x] Die übrigen Dateiverweise derselben Karte bleiben unverändert.
- [x] Es gibt **keine** Route zum **Ändern** eines Dateiverweises.

### Fehlerantworten für Agenten

- [x] Eine **unbekannte Karte** liefert HTTP 404 **mit Rumpf** (Code, Meldung mit der aufgerufenen Nummer, Kompensationsaktion) — bei beiden Routen.
- [x] Ein **unbekannter Dateiverweis** liefert HTTP 404 mit Rumpf; der Grund nennt **beide** Nummern (Karte und Dateiverweis).
- [x] Ein **unbekannter Kontributor** liefert HTTP 404 mit Rumpf.
- [x] Ein **stillgelegter** Kontributor liefert HTTP 400 mit Rumpf; die Meldung sagt, dass er **keinen Dateiverweis mehr eintragen** kann — nicht „kann nicht verantwortlich sein" und nicht „kann keinen Kommentar mehr schreiben".
- [x] Nach jeder Zurückweisung wurde **nicht geschrieben**: die Liste der Karte ist unverändert.
- [x] `FehlervertragTests` ruft **beide** neuen Routen ab und bleibt grün.

### Der Abschnitt auf der Kartenseite

- [x] Auf `/karten/{karteId}` steht in der zweispaltigen Sektion hinter „Kommentare" **rechts** ein Abschnitt mit der Überschrift **„Dateiverweise"**; links steht unverändert „Anhänge".
- [x] Jede Zeile zeigt den Pfad in **Schreibmaschinenschrift** an einer **oliven linken Kante**, ein `↗` und ein `×`.
- [x] Ein zu langer Pfad wird in der Zeile **gekürzt dargestellt** (Ellipse) und zieht die Spalte nicht auf; gespeichert und kopiert wird der **ganze** Pfad.
- [x] **Urheber und Zeitpunkt stehen im `title` der Zeile** — die gezeichnete einzeilige Form bleibt, die Zusage der Vision wird trotzdem eingelöst.
- [x] Unter der Liste steht die Eingabezeile mit dem Text „Pfad im Repository eintragen".
- [x] Das Eingabefeld trägt **kein `value`-Attribut** (wie bei Teilaufgabe und Kommentar); abgeschickt wird mit der **Eingabetaste**, es gibt keinen Knopf.
- [x] Nach dem Eintragen steht die neue Zeile als **letzte** in der Liste und das Feld ist leer.
- [x] **Ist keine Identität gewählt, ist die Eingabezeile gesperrt** und weist auf die Identitätswahl in der Kopfzeile hin — mit demselben Muster wie die Ablegefläche daneben. Board, Kartenseite und alle übrigen Handlungen bleiben ohne Wahl unverändert benutzbar.
- [x] Wird die Identität in der Kopfzeile gewechselt, **während** die Kartenseite offen ist, trägt der nächste Dateiverweis den **neu** gewählten Urheber — ohne Reload.
- [x] Ein leerer oder doppelter Pfad bringt eine **lesbare Meldung** auf der Seite; die Liste bleibt unverändert.
- [x] Ein Klick auf `×` nimmt die Zeile sofort aus der Liste; nach einem Reload ist sie weiterhin weg.
- [x] Nach einem Reload zeigt die Seite dieselben Dateiverweise in derselben Reihenfolge.
- [x] Die Zeile eines Dateiverweises ist von der Zeile eines Anhangs **ohne Beschriftung** zu unterscheiden: Schreibmaschinenschrift und olive Kante hier, Büroklammer und Größenangabe dort.

### Der Pfeil kopiert den Pfad

- [x] Ein Klick auf `↗` legt den **ganzen** Pfad in die Zwischenablage; ein anschließendes Einfügen liefert genau diesen Text.
- [x] Die Zeile gibt eine **sichtbare Rückmeldung**, dass kopiert wurde.
- [x] Der Klick **navigiert nicht**: die Seite bleibt auf `/karten/{karteId}`, es öffnet sich kein Fenster und kein Download beginnt.
- [x] **Ist die Zwischenablage nicht verfügbar** (unsicherer Kontext, im Test durch Wegnehmen von `navigator.clipboard` erzwungen), erscheint **keine Ausnahmeseite**: der Pfad wird stattdessen in der Zeile **markiert**, so dass er von Hand kopiert werden kann.
- [x] Der Aufruf kommt **ohne eigene `.js`-Datei** aus.

### Die gemeinsame Leerzeile über beide Hälften

- [x] Karte **ohne Anhang und ohne Dateiverweis**: es steht **eine** Zeile — „Keine Anhänge, keine Dateiverweise · hinzufügen" — und **nicht** zwei halbe.
- [x] Karte **mit Anhang, ohne Dateiverweis**: es steht die halbe Zeile der leeren Hälfte („Keine Dateiverweise · eintragen").
- [x] Karte **ohne Anhang, mit Dateiverweis**: es steht die halbe Zeile der anderen leeren Hälfte („Keine Anhänge · hinzufügen").
- [x] Karte **mit beidem**: es steht **keine** Leerzeile.
- [x] Der Wechsel zwischen den vier Zuständen geschieht **ohne Reload**: wird der letzte Dateiverweis einer Karte ohne Anhang entfernt, erscheint die gemeinsame Zeile sofort.
- [ ] Die Handlung in der Zeile ist erreichbar: der Klick darauf führt zum jeweiligen Eingabeort.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [x] **Benannte Änderung 1:** `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) wächst um `IReadOnlyList<Dateiverweis> Dateiverweise` — die **sechste** Liste. Das sind **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:667`); beide werden angepasst, ihre Zusicherungen nicht.
- [x] **Benannte Änderung 2:** `Kartendetailvergleich` (`Source/KanbanC.WebApi.IntegrationTests/Infrastructure/Kartendetailvergleich.cs`) vergleicht auch die neue Liste; sein Kommentarkopf nennt die richtige Zahl (**sechs** statt fünf). Ohne das nennt er zwei Details still gleich, die es in der neuen Liste nicht sind — die Datei sagt genau das über sich selbst voraus.
- [x] **Benannte Änderung 3:** `Stillgelegt` (`Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs`) bekommt die **vierte** Schwester `Dateiverweisurheber`. **Derselbe Code** `kontributor-stillgelegt` (400) wie bei den drei anderen, damit `Nichtgefunden.MeldetEinFehlendesDing` und die Statusabbildung unangetastet bleiben; eigen ist nur die Meldung.
- [x] **Benannte Änderung 4:** `Nichtgefunden` (`Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs`) bekommt `Dateiverweis(karteId, dateiverweisId)` neben `Anhang(karteId, anhangId)` — Grund mit **beiden** Nummern und Kompensationsaktion, auch bei 404; `AlleCodes` wächst um den neuen Code.
- [x] **Benannte Änderung 5:** `IKartenRepository` und `TestKartenRepository` (`Source/KanbanC.BL.Tests/TestHelpers/`) ziehen mit den neuen Signaturen mit.
- [x] **Benannte Änderung 6 — die einzige, die grünes *Verhalten* ändert:** `Kartendetail.razor` ersetzt `<div class="blatthalbabschnitt" id="verweisplatz" aria-hidden="true"></div>` (`:347`) durch den gebauten Abschnitt, und der Leerstand (`:324-326`) wird zur gemeinsamen Zeile über beide Hälften. Der Kommentar bei `:284-292` („die rechte Hälfte bleibt in diesem Slice leer", „die gemeinsame Fassung mit den Verweisen entsteht mit I0019") wird damit falsch und wird nachgezogen.
- [x] **Benannte Änderung 7 — vier grüne Tests ziehen zwingend mit:**
  - `AnhangabschnittTests.cs:46` prüft `id="verweisplatz"` — der Platzhalter ist weg, der Test prüft künftig den gebauten Abschnitt.
  - `KartendetailSeite.cs:204` (`Verweisplatz`-Locator) wird durch die Locator des Abschnitts ersetzt.
  - `DateiAnKarteHaengenE2ETests.cs:247` (`Expect(Verweisplatz).ToBeEmptyAsync()`) — die Hälfte ist dann gerade **nicht** mehr leer; die Zusicherung wird ersetzt, nicht gelöscht.
  - `KartendetailOeffnenE2ETests.cs:96-99` nagelt den **halben** Leerzeilen-Wortlaut fest; sein eigener Kommentar kündigt den Wechsel an („entsteht erst mit I0019").
- [x] **Benannte Änderung 8:** `WebApiKlient` (`Source/KanbanC.PlaywrightTests/Infrastructure/`) wächst um `TrageDateiverweisEin` für den Aufbau der E2E-Lage.
- [x] `Kopfzeile.razor` bleibt **unverändert** — die Kartenseite injiziert den `Identitaetsspeicher` selbst; kein `CascadingValue`, kein Zustandsdienst, kein `EventCallback`. `R00013` wird nicht angefasst.
- [x] `Karte.cs` und `Karte.razor` bleiben unverändert — **auf der Bahn ist kein Dateiverweiszeichen**, und die Kartenzahl im Bahnenkopf zählt unverändert.
- [x] `Anhang.cs`, `Anhangablage`, `Anhangpfad`, `Anhangadresse` und `Dateigroesseform` bleiben unverändert — dieser Slice fasst die **Bytes** nicht an.
- [x] `KartenRepository.Heute()` und `KontributorenRepository` bleiben unverändert — es wird **keine** Uhr-Abstraktion eingeführt.
- [x] Alle E2E-Suiten aus `R00001`–`R00020` bleiben grün; geändert werden **nur** die vier oben benannten Stellen.
- [x] `GET /api/boards/{boardId}` und alle bestehenden Kartenrouten bleiben in Adresse, Verb und Antwortgestalt unverändert.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/015-kartendateiverweis.sql` — neue, idempotente Migration; Tabelle `Dateiverweis` mit `DateiverweisId` als Primärschlüssel, `Karte` und `Kontributor` als Fremdschlüssel (nach der Projektregel benannt wie die referenzierte Tabelle), `Pfad TEXT NOT NULL`, `Zeitpunkt TEXT NOT NULL`, ein Index auf `Karte` und ein **eindeutiger** Index auf `(Karte, Pfad)`. **Keine `Position`.**
- **Contracts:** `Source/KanbanC.Contracts/Karten/Dateiverweis.cs` (neu), `DateiverweisEintragenAnfrage.cs` (neu), `Kartendetail.cs` (wächst um die Dateiverweisliste).
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/DateiverweisValidator.cs` (neu), `Dateiverweispfad.cs` (neu — das Trimmen, Muster `Kommentartext`/`Anhangname`); `Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs` (vierte Schwester), `Nichtgefunden.cs` (Schwester `Dateiverweis`), `Doppelt.cs` (neu — der 400er-Befund zum doppelten Pfad, eigene Klasse neben `Stillgelegt` aus demselben Grund: es fehlt kein Ding).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Dateiverweisleser.cs` (neu), `Kartenleser.cs` (`LiesKartendetail` führt die Dateiverweise mit, `:93`), `KartenRepository.cs` (`TrageDateiverweisEin`, `EntferneDateiverweis`), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `TrageDateiverweisEin`, `EntferneDateiverweis`, mit dem Urheberbefund neben den bestehenden für Kommentar und Anhang und dem neuen Dublettenbefund.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — **zwei** neue Routen als Unterressourcen der boardlosen Kartenadresse, neben `/etiketten`, `/teilaufgaben`, `/kommentare` und `/anhaenge`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`TrageDateiverweisEin`, `EntferneDateiverweis` über `AlsKartendetail`), `Source/KanbanC.Blazor/Services/Pfadkopie.cs` (neu — Zwischenablage mit Rückfall auf Textauswahl, Muster `Identitaetsspeicher`), `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` (rechte Hälfte der zweispaltigen Sektion, `:284-352`; gemeinsame Leerzeile).
- **Unberührt:** `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor`, `Source/KanbanC.Contracts/Karten/Karte.cs`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor`, die gesamte Anhangablage — **auf der Bahn und in der Kopfzeile ändert sich nichts, und die Bytes werden nicht angefasst.**
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Karten/DateiverweisValidatorTests.cs`, `Operations/Karten/DateiverweispfadTests.cs`, `Integrations/Karten/KartenServiceTests.cs`, `TestHelpers/TestKartenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenApiKlientTests.cs`, `Gestaltung/AnhangabschnittTests.cs`, neue `Gestaltung/Dateiverweisabschnitt`-Prüfung), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Infrastructure/Kartendetailvergleich.cs`), `Source/KanbanC.PlaywrightTests/` (`ZwischenablageProbeTests.cs` — die Probe, `PageObjects/KartendetailSeite.cs`, `Infrastructure/WebApiKlient.cs`, `Tests/KartendetailOeffnenE2ETests.cs`, `Tests/DateiAnKarteHaengenE2ETests.cs`, neue Testklasse `DateiverweisAnKarteE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0004.dc.html`](../Dokumentation/Wireframes/D0004.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gilt daraus der **Abschnitt mit dem Vermerk `I0019`** (`:216-225`), die **rechte** Hälfte der zweispaltigen Sektion, deren linke `R00020` gebaut hat: Überschrift, Zeilen aus Pfad in Schreibmaschinenschrift an oliver linker Kante mit `↗` am Zeilenende, darunter die Eingabezeile „Pfad im Repository eintragen". Dazu der **gemeinsame Leerzustand** der frischen Karte (`:425`) und die Lesehilfe (`:536`), die den Abschnitt `I0019` zuordnet („Verweise als Repository-Pfade, an der Olivkante und der Schreibmaschinenschrift von den Anhängen unterschieden"). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Vier bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Überschrift heißt „Dateiverweise", nicht „Verweise"** (`:219`). Dieselbe Bewegung wie „Subtasks" → „Teilaufgaben" bei `R00018`, aber aus einem anderen Grund: dort war es die Sprache, hier ist es die Eindeutigkeit. C06 verlangt **einen** Begriff in **einer** Schreibweise, ausdrücklich „auch in den SQL-Spaltennamen" — eine Beschriftung, die den Begriff auf sein Grundwort verkürzt, ist genau die zweite Schreibweise, die die Regel ausschließt. Und „Verweis" ist im Code als Hyperlink-Begriff belegt; zwei Bedeutungen desselben Worts stünden **schon heute im selben Schirm** nebeneinander (`Kartendetail.razor:308` „der Verweis" = Download-Link gegen `:347` „verweisplatz" = diese Spalte). Das kostet ein Wort in einer Überschrift neben „Anhänge" und gewinnt, dass das Wort auch sagt, worauf verwiesen wird. **Der Halbsatz des Fertig-Kriteriums „der Verweis ist als solcher erkennbar" bleibt davon unberührt:** erkennbar machen ihn Schreibmaschinenschrift und olive Kante, so sagt es die Lesehilfe des Artboards selbst — nicht die Überschrift.
2. **Das `↗` ist ein Bedienelement und kopiert den Pfad.** Im Artboard ist es ein `<span>` (`:222-223`), steht aber in der Akzentfarbe genau an der Stelle, an der die Anhangzeile daneben ihre Handlung trägt — eine Zierde dort führte in die Irre. **Es navigiert nicht**, und das ist keine Sparsamkeit, sondern nicht baubar: ein Repository-Pfad ist keine URL; welcher Rechner welchen Klon an welcher Stelle liegen hat, weiß der Server nicht und kann es nicht wissen; und ein `file://`-Ziel lässt sich aus einer über HTTP ausgelieferten Seite nicht öffnen. Kopieren ist dagegen genau das, was mit einem Pfad geschieht: er wandert in den Editor, die Shell oder den Prompt eines Agenten.
3. **Ein `×` am Zeilenende.** Das Artboard zeichnet an der Verweiszeile kein Entfernen. Gebaut wird es trotzdem — Begründung im „Geschäftlichen Nutzen" und unter „Verworfene Alternativen". Dieselbe bewusste Abweichung wie bei `R00020`, mit anderem Grund.
4. **Urheber und Zeitpunkt im `title` der Zeile.** Das Artboard zeichnet an der Zeile nur Pfad und Pfeil. Beide reisen trotzdem mit, weil die Vision sie verlangt („An jeder Karte ist ablesbar, wer oder was gehandelt hat") und der Zeitpunkt zugleich die einzige Ordnung der Liste ist. Im `title` und **nicht** als zweite Textzeile — so bleibt die gezeichnete einzeilige Form unangetastet. Wörtlich das Vorgehen aus `R00020`.

Alles andere am Abschnitt folgt der Skizze.

### Ablauf

1. **Dateiverweis eintragen** (`POST /api/karten/{karteId}/dateiverweise`)
   - 1.1 `DateiverweisValidator.Pruefe(karteId, anfrage)` — **nur** leerer Pfad und Pfad über der Höchstlänge, jeweils nach dem Trimmen; die Kompensation nennt die Route samt Kartennummer und die Höchstlänge. **Der Urheber wird hier nicht geprüft** (braucht den Kontributorenbestand), **die Dublette auch nicht** (braucht den Bestand der Karte) — genau das sagt `KommentarValidator` über seine beiden Auslassungen
   - 1.2 Bei Befunden: HTTP 400 mit Rumpf, **kein** Schreibzugriff
   - 1.3 `KartenService` prüft den Urheber — dieselben zwei Regeln wie bei Kommentar und Anhang, der Urheber ist **Pflicht** und nicht `long?`
     - 1.3.1 Kontributor unbekannt → `Nichtgefunden.Kontributor(kontributorId)` → HTTP 404
     - 1.3.2 Kontributor stillgelegt → `Stillgelegt.Dateiverweisurheber(kontributorId)` → HTTP 400
   - 1.4 `KartenRepository.TrageDateiverweisEin` in **einer** Transaktion
     - 1.4.1 Karte lesen; fehlt sie → `null` → `Nichtgefunden.Karte(karteId)` → HTTP 404
     - 1.4.2 **Bestand der Karte auf denselben Pfad prüfen** — in derselben Transaktion, vor dem `INSERT`
     - 1.4.3 Ist der Pfad schon da → als eigenes Ergebnis melden (nicht als `null`, das heißt „Karte unbekannt") → `Doppelt.Dateiverweis(karteId, pfad)` → HTTP 400
     - 1.4.4 Sonst `INSERT` mit `Zeitpunkt` = `DateTimeOffset.UtcNow` als ISO-8601-UTC-Text
     - 1.4.5 Ganzes `Kartendetail` in derselben Transaktion zurücklesen
   - 1.5 HTTP 200 mit dem `Kartendetail`
2. **Dateiverweis entfernen** (`DELETE /api/karten/{karteId}/dateiverweise/{dateiverweisId}`)
   - 2.1 `KartenRepository.EntferneDateiverweis` in einer Transaktion; Karte unbekannt oder Dateiverweis gehört nicht zu **dieser** Karte → `null`
   - 2.2 `KartenService` unterscheidet beide Lagen und liefert `Nichtgefunden.Karte(karteId)` bzw. `Nichtgefunden.Dateiverweis(karteId, dateiverweisId)` → HTTP 404
   - 2.3 Sonst Zeile weg, ganzes `Kartendetail` zurück → HTTP 200
3. **Pfad kopieren** (Oberfläche)
   - 3.1 Klick auf `↗` → `Pfadkopie.Kopiere(pfad)`
   - 3.2 `IJSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", pfad)` — Inline-Ausdruck, keine eigene `.js`-Datei
   - 3.3 Bei `JSException` (unsicherer Kontext) → **Rückfall**: den Pfad in der Zeile über `window.getSelection` markieren
   - 3.4 In beiden Fällen sichtbare Rückmeldung an der Zeile; **nie** eine Ausnahmeseite
4. **Die gemeinsame Leerzeile** (Oberfläche)
   - 4.1 Vier Zustände aus zwei Listen: `{leer, leer}` → **eine** Zeile · `{voll, leer}` → halbe Zeile rechts · `{leer, voll}` → halbe Zeile links · `{voll, voll}` → keine
   - 4.2 Heute stehen zwei getrennte `@if` (`Kartendetail.razor:324-326` und die leere rechte Hälfte); daraus wird **eine** Stelle, die beide Listen kennt

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — zwei neue Routen als Unterressourcen der boardlosen Kartenadresse, neben den drei Anhangrouten. Die Adressen tragen kein Board, weil die Seite keins kennt.
- **`Kartenleser.LiesKartendetail`** (`:93`) — der eine Ort, an dem das Detail entsteht; hier reihen sich die Dateiverweise als sechste Liste ein.
- **`Migrationslaeufer`** — die fünfzehnte Migration reiht sich ein; kein Journal, also idempotent.
- **`Kartendetail.razor`** (`:284-352`) — die rechte Hälfte der bestehenden zweispaltigen Sektion; hier steht heute der Platzhalter `verweisplatz`.
- **`Identitaetsspeicher`** (`Blazor/Program.cs:26`) — die Naht aus `R00013`, dieselbe wie in `R00019` und `R00020`, und zugleich das Muster für `Pfadkopie` (Inline-`IJSRuntime`-Aufruf mit gefangenem Ausfall).

**Klassen-Entwurf:**

- `Dateiverweis` (DTO, immutable) — ein an der Karte hinterlegter Pfad. **Der Urheber reist als ganzer `Kontributor`**, damit Name, Kürzel und der Zusatz „stillgelegt" ohne zweiten Abruf entstehen — dieselbe Entscheidung wie bei `Kommentar` und `Anhang`.
  - `record Dateiverweis(long DateiverweisId, string Pfad, Kontributor Urheber, DateTimeOffset Zeitpunkt)`
- `DateiverweisEintragenAnfrage` (DTO, immutable) — Pfad und Urheber; **`Kontributor` ist Pflicht** (`long`, nicht `long?`). Kein Feld für den Zeitpunkt.
  - `record DateiverweisEintragenAnfrage(string Pfad, long Kontributor)`
- `Kartendetail` (DTO, immutable) — wächst um `IReadOnlyList<Dateiverweis> Dateiverweise`. **Kein Zählfeld daneben** und **keine Position**.
- `Dateiverweispfad` (Operation, pure Logik) — schneidet Randleerzeichen. **Schreibt keine Trennzeichen um.** Muster `Kommentartext`, `Anhangname`.
  - `static string Normalisiert(string pfad)`
- `DateiverweisValidator` (Operation, pure Logik) — Muster `KommentarValidator`. **Kein Dublettenbefund, keine Urheberprüfung, keine Formprüfung.**
  - `static Pruefbefunde Pruefe(long karteId, DateiverweisEintragenAnfrage anfrage)`
- `Doppelt` (Operation) — der 400er-Befund zum doppelten Pfad. Eigene Klasse neben `Stillgelegt` aus demselben Grund: **es fehlt kein Ding**, eine Regel wurde verletzt; `Nichtgefunden.MeldetEinFehlendesDing` kennt den Code bewusst nicht.
  - `static Fehlerbefund Dateiverweis(long karteId, string pfad)`
- `Dateiverweisleser` (Provider/Ressourcenzugriff) — liest die Dateiverweise einer Karte in Zeitpunkt-Reihenfolge, mit dem ganzen Urheber, in der laufenden Transaktion. Zeitpunkt als Text lesen und in C# umrechnen, wie `Kommentarleser` und `Anhangleser`.
  - `static IReadOnlyList<Dateiverweis> LiesDateiverweiseDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)`
- `Dateiverweiseintragung` (DTO in `KanbanC.BL/Models`, immutable) — das Ergebnis des Eintragens mit **drei** unterscheidbaren Lagen: Karte unbekannt, Pfad schon vorhanden, Erfolg mit dem Detail. **Nötig, weil `null` allein zwei verschiedene Dinge sagen müsste** — das ist der einzige Punkt, an dem dieser Slice von der Antwortgestalt der Nachbarn abweicht.
  - `record Dateiverweiseintragung(Kartendetail? Detail, bool PfadSchonVorhanden)`
- `KartenRepository` (Provider, Integration nach Hausregel) — zwei Wege, beide mit dem ganzen Detail als Rückgabe.
  - `Dateiverweiseintragung TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage)`
  - `Kartendetail? EntferneDateiverweis(long karteId, long dateiverweisId)`
- `KartenService` (Integration, prüft/fängt) — dieselbe Antwortgestalt wie `SchreibeKommentar` und `HaengeAnhangAn`.
  - `Ergebnis<Kartendetail> TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage)`
  - `Ergebnis<Kartendetail> EntferneDateiverweis(long karteId, long dateiverweisId)`
- `Nichtgefunden` (Operation) — eine Schwester mehr, mit **beiden** Nummern im Grund.
  - `static Fehlerbefund Dateiverweis(long karteId, long dateiverweisId)`
- `Stillgelegt` (Operation) — die vierte Schwester, gleicher Code, eigene Meldung.
  - `static Fehlerbefund Dateiverweisurheber(long kontributorId)`
- `Pfadkopie` (Integration in der Oberflächenschicht) — Zwischenablage mit Rückfall auf Textauswahl; fängt `JSException` wie der `Identitaetsspeicher`. **Ohne eigene `.js`-Datei.**
  - `Task<Kopierergebnis> Kopiere(string pfad, string elementId)`
- `KartenApiKlient` (Integration) — zwei Aufrufe mehr, beide über `AlsKartendetail`. **JSON in beide Richtungen**, kein Byte-Rückweg, keine multipart-Form, keine Direktadresse an der WebApi.
  - `Task<ApiErgebnis<Kartendetail>> TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage)`
  - `Task<ApiErgebnis<Kartendetail>> EntferneDateiverweis(long karteId, long dateiverweisId)`

### Änderungen an bestehenden Klassen

- `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) — ein Feld mehr, die **sechste** Liste. Genau **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:667`).
- `Kartenleser` (`:93`) — `LiesKartendetail` führt die Dateiverweise mit, wie schon Etiketten, Teilaufgaben, Kommentare und Anhänge. Kein Archivfilter.
- `KartenRepository` — zwei Wege dazu, Muster `SchreibeKommentar`/`HaengeAnhangAn`: Existenzprüfung, Schreiben und Rückgabe des ganzen Details in **einer** Transaktion. Der Zeitpunkt kommt aus `DateTimeOffset.UtcNow` an derselben Stelle wie beim Kommentar und beim Anhang — **keine Uhr-Abstraktion**.
- `IKartenRepository` — zwei Signaturen dazu; `TestKartenRepository` zieht mit.
- `KartenService` — ein Prüfweg für den Dateiverweisurheber neben denen für Kommentar- und Anhangurheber, dazu die Abbildung der Dublette auf `Doppelt.Dateiverweis`.
- `Nichtgefunden` — `Dateiverweis(karteId, dateiverweisId)`; `AlleCodes` wächst.
- `Stillgelegt` — `Dateiverweisurheber` als vierte Schwester.
- `KartenEndpunkte` — eine Routenkonstante für die Liste, eine für die einzelne Zeile, zwei Registrierungen. **Die Vertragsfälle beider Routen gehören in denselben Arbeitsgang wie die Routen**: `FehlervertragTests.cs:41-58` liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot.
- `Kartendetail.razor` (`:284-352`) — die rechte Hälfte wird gebaut, der Platzhalter `verweisplatz` (`:347`) fällt, der Anhang-Leerstand (`:324-326`) wird zur gemeinsamen Zeile, der Kommentar bei `:284-292` wird nachgezogen.
- `Kartendetail.razor.css` (`:617-619`) — der Kommentar zur zweispaltigen Sektion („rechts der Platz, den I0019 füllen wird") wird nachgezogen; die Klassen des neuen Abschnitts kommen dazu, alle Werte aus `gestaltung.css`.
- `Kartendetailvergleich` — vergleicht die sechste Liste; Kommentarkopf nennt sechs.
- `AnhangabschnittTests.cs:46`, `KartendetailSeite.cs:204`, `DateiAnKarteHaengenE2ETests.cs:247`, `KartendetailOeffnenE2ETests.cs:96-99` — die vier grünen Tests, die mitziehen (siehe „Benannte Änderung 7").
- `WebApiKlient` (`Source/KanbanC.PlaywrightTests/Infrastructure/`) — `TrageDateiverweisEin` für den Aufbau der E2E-Lage.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Probe vor der ersten produktiven Nutzung** (Skill `dependency-probe`, in `ZwischenablageProbeTests` im E2E-Browser, **ohne eine Zeile Produktionscode**): (1) `IJSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", pfad)` gelingt aus dem SignalR-Kreislauf heraus **ohne eigene Nutzergeste** und der Text lässt sich zurücklesen; (2) **Fault-Injection** — wird `navigator.clipboard` vor dem Klick über `Object.defineProperty` weggenommen, **wirft** der Aufruf als `JSException`, statt still nichts zu tun; (3) die Textauswahl über `window.getSelection` trägt in **beiden** Kontexten. Fällt (3), bleibt nur das Markieren des Pfads; das ändert `B0291`, nicht die Anforderung. **Warum die Probe unverzichtbar ist:** der E2E-Lauf arbeitet auf `127.0.0.1` und damit im sicheren Kontext — er würde das Fehlen der Zwischenablage im LAN von selbst **nie** zeigen.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Dateiverweispfad.Normalisiert` — Randleerzeichen fallen weg; ein Pfad mit Rückstrichen bleibt **unverändert**; ein Pfad mit doppelten Schrägstrichen bleibt unverändert; ein Pfad, der nur aus Leerzeichen besteht, wird leer.
- `DateiverweisValidator.Pruefe` — leerer Pfad (Befund), nur Leerzeichen (Befund), genau an der Höchstlänge (**kein** Befund), Höchstlänge + 1 (Befund, der die Höchstlänge nennt); **eine nicht existierende Datei ergibt keinen Befund**; **ein absoluter Pfad ergibt keinen Befund**; **ein Pfad ohne Endung ergibt keinen Befund**; **zwei gleiche Pfade ergeben hier keinen Befund** (die Dublette sitzt im Dienst); die Kompensation nennt `POST /api/karten/{karteId}/dateiverweise` samt Nummer.
- `Doppelt.Dateiverweis` — Code, Meldung mit Pfad und Kartennummer, nichtleere Kompensation; **`Nichtgefunden.MeldetEinFehlendesDing` ist für diesen Befund `false`** (400, nicht 404).
- `Nichtgefunden.Dateiverweis` / `Stillgelegt.Dateiverweisurheber` — beide Nummern im Grund bzw. eigene Meldung bei gleichem Code; die Meldung unterscheidet sich **nachweislich** von der des Kommentar- und der des Anhangurhebers.
- `KartenService.TrageDateiverweisEin` / `EntferneDateiverweis` gegen `TestKartenRepository` — Erfolg reicht das Detail durch; unbekannte Karte, unbekannter Dateiverweis, **doppelter Pfad**, unbekannter Kontributor und stillgelegter Kontributor liefern Befunde mit nichtleerem Code, Meldung und Kompensation; nach einer Zurückweisung wurde **nicht geschrieben**.
- `KartenApiKlient.TrageDateiverweisEin` / `EntferneDateiverweis` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und **Rumpf** des abgesetzten Aufrufs werden mitgeprüft, insbesondere dass die `KontributorId` **im JSON-Rumpf** und nicht in der Query steht. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository.TrageDateiverweisEin` / `EntferneDateiverweis` und `Dateiverweisleser` gegen eine `TemporaereDatenbank` — schreiben und wieder lesen; der gespeicherte Zeitpunkt liegt im **Zeitfenster** `t0 ≤ Zeitpunkt ≤ t1`; der Pfad kommt **zeichengleich** zurück (auch mit Rückstrichen); zwei nachträglich verschieden datierte Zeilen kommen in Zeitordnung zurück; der Urheber kommt vollständig zurück, auch wenn er stillgelegt ist; **ein zweiter Eintrag desselben Pfads an derselben Karte wird gemeldet und schreibt nicht**; derselbe Pfad an einer **zweiten** Karte geht durch; nach dem Entfernen lässt sich derselbe Pfad **wieder** eintragen; eine fremde `DateiverweisId` liefert `null` und entfernt nichts. **Der eindeutige Index selbst** wird geprüft: ein direkter zweiter `INSERT` am Repository vorbei scheitert an der Datenbank. `Kartenleser.LiesKartendetail` liefert die Liste auch für eine **archivierte** Karte. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — beide Routen mit 200, 400 (leerer Pfad; zu langer Pfad; doppelter Pfad; stillgelegter Kontributor) und 404 (unbekannte Karte; unbekannter Dateiverweis; fremder Dateiverweis; unbekannter Kontributor) samt Rumpf; `GET /api/boards/{boardId}` trägt danach **keine** Dateiverweisliste an den Karten; `FehlervertragTests` ruft beide Routen ab. `WebApiNeustartTests` — Pfade, Urheber, Zeitpunkte und Reihenfolge überstehen den Neustart.

**E2E:** Eine Karte auf `/karten/{karteId}` ohne Anhang und ohne Dateiverweis zeigt **eine** gemeinsame Leerzeile. Mit gewählter Identität einen Pfad eintragen und mit der Eingabetaste abschicken → die Zeile erscheint in Schreibmaschinenschrift mit oliver Kante, Reload zeigt sie unverändert (US-1). Ein leerer und ein doppelter Pfad werden **sichtbar** zurückgewiesen, die Liste bleibt unverändert (US-2). Der Klick auf `↗` legt den Pfad in die Zwischenablage — **ausgelesen im Browser**, nicht an der Rückmeldung abgelesen; dazu der erzwungen unsichere Kontext, in dem der Pfad **markiert** wird statt eine Ausnahmeseite zu zeigen (US-3). Das `×` nimmt die Zeile, Reload bestätigt es (US-4). In einem **frischen Browserkontext** ohne gewählte Identität ist die Eingabezeile gesperrt (US-5). Die **vier Zustände der gemeinsamen Leerzeile** brauchen vier Aufbaulagen: Karte mit Anhang ohne Dateiverweis, mit Dateiverweis ohne Anhang, mit beidem, mit keinem (US-6). Dazu laufen die E2E-Suiten aus `R00001`–`R00020` weiter, mit den vier benannten Anpassungen; das ist die Gegenprobe des Slice.

Repositories, `Dateiverweisleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün). Das ist der einzige Knoten, den die WBS-Spalte `Braucht` von `I0019` nennt; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00017`** (`I0015`, grün — die Kartenseite, die Adresse `/karten/{karteId}`, das `Kartendetail` als Antwortgestalt, `KartenApiKlient.AlsKartendetail`, `KartendetailSeite`), **`R00018`** (`I0016`, grün — das Muster der Eingabezeile ohne `value`), **`R00019`** (`I0017`, grün — der Urheber aus dem `Identitaetsspeicher`, die Zeitform als ISO-8601-UTC-Text) und **`R00020`** (`I0018`, grün — die zweispaltige Sektion, der Platzhalter `verweisplatz`, die halbe Leerzeile, die dritte Urheber-Schwester). Die Spalte `Braucht` von `I0019` nennt weder `I0015` noch `I0008` noch `I0018`; das ist in der WBS als offene Frage vermerkt und gehört in `/planung aendern I0019`, wenn die Herkunft dokumentiert bleiben soll. An Front und Welle ändert es nichts, weil alle grün sind.
- Setzt ferner auf: **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css`), **`R00011`**/**`R00014`** (`Kontributor`, `Kontributorstilllegung`, `Kontributorartform`), **`R00013`** (`Identitaetsspeicher`, zugleich das Muster für `Pfadkopie`), **`R00016`** (`LiesKartendetail` ohne Archivfilter).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0019` in seiner Spalte `Braucht` (geprüft am 2026-09-06 über `Dokumentation/Planung/kanbanc.md`). **Schließt aber `D0004` ab:** `I0019` ist die letzte rote Interaction des Dialogs „Karteninhalt pflegen"; mit ihr wird der dritte Dialog der Anwendung grün.

## Umfang

```
Karte auf Dateien verweisen lassen (I0019) = 12 Bubbles: 9 Standard (8,4h), 3 unklar (2,8–7,0h).
Rest: 8,4h klar + 2,8–7,0h unklar · 6 von 12 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0019` ist vollständig bis zur Bubble geplant und trägt seine zwölf Bubbles (`B0282`–`B0293`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: die Interaction hat einen prüfbaren Aspekt, nicht mehrere. Eintragen, Kopieren und Entfernen teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; als getrennte Slices geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0016`, `I0017` und `I0018`. Ein zweites Feature „Zurückweisung ungültiger Pfade" wurde geprüft und verworfen: es ist ein Fehlerpfad derselben Route, im selben E2E-Lauf belegt. **Die Requirement-Klammer sitzt deshalb allein an `I0019`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0282` Probe: Zwischenablage aus dem Blazor-Kreislauf | Probe (`dependency-probe`) | 0,4–1,5h (**unklar**) |
| `B0283` Dateiverweistabelle anlegen | Provider (Migration) | 0,4h (belegt) |
| `B0284` Dateiverweise am Kartendetail lesen | Contracts + Provider | 0,4h (belegt) |
| `B0285` Pfad prüfen | Operation | 0,4h (belegt) |
| `B0286` Dateiverweis eintragen und entfernen | Provider | 0,4h (belegt) |
| `B0287` Dateiverweise verdrahten | Integration | 0,4h (belegt) |
| `B0288` Endpunkte der Dateiverweise | Integration | 2h (Richtwert) |
| `B0289` API-Klient der Dateiverweise | Integration | 2h (Richtwert) |
| `B0290` Dateiverweisabschnitt der Kartenseite | UI | 2h (Richtwert) |
| `B0291` Pfad kopieren am Pfeil | UI + Integration | 0,4h (belegt) |
| `B0292` Gemeinsame Leerzeile über beide Hälften | UI | 0,4–1,5h (**unklar**) |
| `B0293` E2E Karte auf Dateien verweisen lassen | E2E | 2–4h (**unklar**) |

Mit 12 Bubbles ist das **vier weniger als `I0018`** und derselbe Umfang wie `I0017` — kein Byte verlässt hier die Datenbank, es gibt keine Ablage, keine Rahmengrenzen und keine Direktadresse an der WebApi. Die drei unklaren liegen an drei verschiedenen Ursachen: die Zwischenablage ist im Repository **nie** benutzt und ihr Ausfall im E2E-Lauf nur durch Fault-Injection erreichbar (`B0282`), die Leerzeile bringt an einer Stelle mit heute zwei Zuständen deren vier zusammen und nimmt einen grünen Testwortlaut mit (`B0292`), und der E2E-Lauf braucht vier Aufbaulagen plus das Auslesen der Zwischenablage (`B0293`). Derselbe Vermerk wie bei `I0005` bis `I0018`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles. Die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen — das verschöbe die Zählung des ganzen Baums. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die dortige Zählzeile zu `I0019` sagt „12 Bubbles, 9 Standardmuster, 3 unklar — `B0282`, `B0292` und `B0293`". Gezählt über die Aufwandsspalte kommt dasselbe heraus. Anders als bei `I0018` gibt es hier keine Abweichung.

## Offene Fragen

- **Steht die gemeinsame Leerzeile über oder unter der zweispaltigen Sektion?** — **nicht entschieden, erst am Schirm zu klären.** Beides ist mit dem Artboard vereinbar, das den Leerzustand nur in der verkleinerten Zustandsansicht zeigt (`:425`). Betrifft `B0292` und verschiebt eine Zeile Markup, nicht das Verhalten: die vier Zustände und ihr Wortlaut stehen in den Akzeptanzkriterien und gelten unabhängig vom Ort.
- **Wie hoch ist die Höchstlänge des Pfads?** — **nicht entschieden, im stillen Lauf mit 500 angenommen.** Das Muster der Nachbarn: Etikett 100, Teilaufgabe 200, Kartentitel 1000, Kommentar 2000; die WBS sagt, der Wert liege „zwischen Teilaufgabe und Titel". 500 trägt einen tief verschachtelten Repository-Pfad samt langem Dateinamen und bleibt deutlich unter dem Kartentitel. Vor `B0285` zu bestätigen; eine andere Zahl ändert genau eine Konstante und ein Rechenbeispiel.
- **Trägt die Zwischenablage im LAN-Betrieb über `http://`?** — **offen und durch keinen E2E-Test gedeckt.** `navigator.clipboard` existiert nur im sicheren Kontext; der E2E-Lauf arbeitet auf `127.0.0.1`, wo er gegeben ist. **Restrisiko, benannt:** wer die Anwendung zum ersten Mal von einem fremden Browser im LAN bedient, prüft den Pfeil von Hand. `B0282` erzwingt den unsicheren Fall im Test durch Fault-Injection und `B0291` baut den Rückfall auf Textauswahl — damit ist der Ausfall abgefangen, aber nicht bewiesen, dass der Rückfall im echten LAN-Browser dasselbe tut. **Dieselbe Klasse von Restrisiko wie die öffentliche Basisadresse in `R00020`**, und aus demselben Grund unentdeckbar: der Testlauf steht auf der falschen Seite der Grenze.
- **Soll vor dem Entfernen eine Rückfrage stehen?** — **nicht entschieden.** Gebaut wird zunächst ohne, wie beim Anhang und beim Abhaken. Ein versehentliches `×` kostet hier weniger als beim Anhang: der Pfad ist neu eintragbar, die Datei war nie hier.
- **Hätte der Mensch die kürzere Überschrift „Verweise" trotzdem lieber?** — **nicht entschieden.** Gebaut wird „Dateiverweise", weil C06 eine Schreibweise verlangt. Wäre die kürzere gewünscht, ist das eine Zeile in `B0290` und **keine** Änderung am Datenmodell — der Begriff im Code bliebe `Dateiverweis`.
- **Hätte der Mensch Urheber und Zeitpunkt lieber sichtbar statt im `title`?** — **nicht entschieden.** Gebaut wird der `title`, damit die gezeichnete einzeilige Form bleibt. Wäre eine Metazeile gewünscht, ist das eine Zeile in `B0290`; beide Werte reisen ohnehin mit.
- **Sollen zwei gleiche Pfade mit verschiedenen Urhebern doch zulässig sein?** — **entschieden: nein** (siehe unten), aber ungeprüft am Menschen. Fiele die Entscheidung anders, fielen der eindeutige Index und der Befund — sonst nichts.
- **Ist `Dateiverweiseintragung` die richtige Form für das dritte Ergebnis?** — **offen, Entwurfsfrage von `B0286`/`B0287`.** Die Nachbarn kommen mit `Kartendetail?` aus, weil `null` dort genau eine Lage bedeutet. Hier sind es zwei (Karte unbekannt, Pfad doppelt), also braucht das Repository einen dritten Ausgang. Ob das ein kleiner Record, ein Aufzählungswert oder eine gefangene Datenbankausnahme wird, entscheidet der Entwickler; die **Anforderung** verlangt nur, dass der Aufrufer einen lesbaren Befund bekommt und nie eine nackte Datenbankmeldung.
- ~~Heißt der Gegenstand „Verweis" oder „Dateiverweis"?~~ — **entschieden: `Dateiverweis`**, im ganzen Stack und in der Beschriftung. Begründung unter „Verworfene Alternativen".
- ~~Navigiert das `↗` oder kopiert es?~~ — **entschieden: es kopiert.** Navigieren ist nicht baubar; Begründung unter „Verworfene Alternativen".
- ~~Prüft die Anwendung, ob der Pfad existiert?~~ — **entschieden: nein.** Nur Leere und Länge; Begründung in den Nicht-funktionalen Anforderungen.
- ~~Kommt das Entfernen mit?~~ — **entschieden: ja**, aus dem Verfallsargument im „Geschäftlichen Nutzen". **Ändern kommt nicht.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft bei jedem Start des `KanbanC.WebApi` mit.

## Manuelle Nachbereitungstätigkeiten

- **Beim ersten Betrieb über das LAN:** den Pfeil `↗` an einer Dateiverweiszeile von einem **zweiten** Rechner aus prüfen. Kein Test deckt das ab (siehe „Offene Fragen"). Erwartet wird: entweder der Pfad liegt in der Zwischenablage, oder er ist markiert — **nie** eine Ausnahmeseite.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Lücke, die dieses Projekt an sich selbst hat: die Wahrheit über Vorhaben liegt in Markdown-Dateien (Vision, Anforderungen, WBS), der Arbeitsfluss liegt auf dem Board — und zwischen beidem gibt es bisher keine Verbindung außer einem Pfad im Fließtext einer Beschreibung, den niemand als Verweis erkennt und kein Agent zuverlässig herausliest. Der Anhang aus `R00020` löst das nicht: er würde eine **Kopie** an die Karte hängen, und eine Kopie einer gepflegten Datei ist am Tag ihres Entstehens veraltet. Das Zielbild ist eine Karte, die auf die lebende Datei **zeigt**, ohne sie zu duplizieren — und die diese Auskunft über dieselbe Route gibt, über die auch die Oberfläche sie bekommt. Die Kausalkette: **wenn** der Pfad eine eigene Zeile mit Fremdschlüsseln auf Karte und Kontributor bekommt und als eigenes Feld am `Kartendetail` reist (X), **dann** ist der Bezug maschinenlesbar statt in Prosa versteckt, und ein Agent, der `GET /api/karten/14` aufruft, bekommt mit der Aufgabe die Liste der Dateien, die er dafür lesen muss (Y), **und dann** docken Board und Markdown-Wahrheit aneinander an, ohne dass eine die andere kopiert — genau die Bewegung, die die Vision mit dem WBS-Import vorhat, im Kleinen und ohne Importmaschine (Z). Der Hebel liegt genau hier und nicht davor: ein allgemeiner „Bezug"-Mechanismus ohne einen Ort, an dem Bezüge wirklich gebraucht werden, wäre tote Flexibilität (C17). Und nicht danach: würde erst der WBS-Import (`I0031`) Pfade führen, entstünde der Begriff in einem Slice, dessen Fertig-Kriterium vom Einlesen spricht — die Karte ist der einfachste Träger, an dem sich der Weg Pfad → Zeile → Zwischenablage ganz zeigt.

## Missing-Docs

- **`navigator.clipboard` aus Blazor Server heraus:** Im Repository nirgends belegt — die Zwischenablage ist nie benutzt. Ob der Aufruf aus dem SignalR-Kreislauf **ohne eigene Nutzergeste** gelingt, ist unbelegt. `B0282` belegt es; das Ergebnis gehört danach in `Dokumentation/Bibliotheken/`, falls es sich online nicht belegen lässt.
- **Verhalten im unsicheren Kontext:** Ob `navigator.clipboard` über `http://` schlicht `undefined` ist (dann wirft der `IJSRuntime`-Aufruf) oder vorhanden ist und die Zusage still bricht, entscheidet, ob der Rückfall überhaupt auslöst. Das ist die **Fault-Injection** von `B0282` und die wichtigste der drei Annahmen.
- **`window.getSelection` und Textauswahl aus C# heraus:** Ob sich ein Textknoten aus Blazor heraus zuverlässig markieren lässt, und ob das im sicheren wie im unsicheren Kontext gleich funktioniert, ist unbelegt. Teil von `B0282`.
- **Auslesen der Zwischenablage in Playwright:** Ob der Testbrowser die Zwischenablage ohne zusätzliche Berechtigungen lesen darf, ist im Repository unbelegt; betrifft `B0293` und entscheidet, ob das Kriterium „liegt in der Zwischenablage" direkt oder nur über einen Einfüge-Umweg prüfbar ist.
- **Eindeutige Indizes in SQLite und die Fehlerform bei Verletzung:** Das Projekt hat bisher genau einen (`002-spalte-bezeichnung-eindeutig.sql`). Wie die Verletzung als Ausnahme ankommt und ob sie sich sicher von anderen Datenbankfehlern unterscheiden lässt, ist unbelegt — relevant nur, falls `B0286` den Index statt einer Vorabprüfung als Erkennungsweg wählt.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Den Begriff `Verweis`** (wie im Artboard) | Im Code als Oberflächenbegriff für einen Hyperlink vergeben: 69 Zeilen in 22 Dateien, davon 51 Bezeichner (`Boardverweis` 13×, `VerweisZurListe` 7×, `.board-verweis` 6×, `TitelverweisDerKarte` 4×, `verweisplatz` 5×, `NavigationsVerweise` 2×). Zwei Bedeutungen lägen **schon heute im selben Schirm** nebeneinander. C06 verlangt eine Schreibweise „auch in den SQL-Spaltennamen"; die Verkürzung auf das Grundwort in der Beschriftung wäre genau die zweite. |
| **Den bestehenden Begriff umbenennen** und `Verweis` freimachen | 51 Bezeichner in 20 Dateien und 12 grüne Testdateien, ohne dass ein Kriterium es verlangt. Ein Umbau dieser Breite ohne fachlichen Anlass ist kein Teil dieses Slice. |
| **`Dateipfad`** | 281 Treffer im Code, durchgehend der Pfad der SQLite-Datei. |
| **`Referenz`** | Kollidiert mit „Projektreferenz", dem tragenden Wort der Kernregel (`CLAUDE.md`). |
| **`Quelle`** | Das Artboard führt „Quelle: Oberfläche / API" an den Kommentaren; `R00019` hat sie dort herausgenommen, der Begriff bleibt für den späteren Verlauf-Slice belegt. |
| **`Fundstelle`** | Frei, aber fachlich schief: eine Karte verweist, sie hat nichts gefunden. |
| **Das `↗` navigiert zur Datei** | Nicht baubar, nicht bloß aufwendig: ein Repository-Pfad ist keine URL; welcher Rechner welchen Klon an welcher Stelle liegen hat, weiß der Server nicht und kann es nicht wissen (Schreiber und Leser sitzen an verschiedenen Maschinen im LAN); und ein `file://`-Ziel lässt sich aus einer über HTTP ausgelieferten Seite nicht öffnen. |
| **Eine `vscode://`- oder `x-github-client://`-Adresse** | Setzte eine Vereinbarung darüber voraus, wo der Klon des Lesenden liegt — die es nicht gibt und die die Anwendung nicht treffen kann. |
| **Das `↗` als bloße Zierde stehen lassen** (wie gezeichnet) | Es steht in der Akzentfarbe genau dort, wo die Anhangzeile daneben ihre Handlung trägt. Eine Zierde an der Stelle einer Handlung führt in die Irre. |
| **Prüfen, ob die Datei existiert** | Der Server kennt keinen Repository-Wurzelpfad — KanbanC ist ein Board über beliebige Vorhaben und hat kein Arbeitsverzeichnis; der Klon des Lesenden liegt ohnehin woanders. Eine Prüfung, die nur der Browser des Eintragenden anstellen könnte, wäre über die API nicht wiederholbar und verletzte die Kernregel. |
| **Ein Formmuster („sieht aus wie ein Pfad")** | Eine Regel ohne Rückhalt in irgendeinem Kriterium; sie stieße sich am ersten Windows-Pfad, am ersten UNC-Pfad und an der ersten Datei ohne Endung. |
| **Trennzeichen vereinheitlichen** (`\` → `/`) | Ein Pfad, den die Anwendung umschreibt, ist nicht mehr der Pfad, den jemand gemeint hat — und beim Kopieren bekäme der Nutzer etwas anderes zurück, als er eingetragen hat. |
| **Doppelte Pfade zulassen** (wie bei Anhang, Teilaufgabe, Kommentar) | Dort waren es **zwei Dinge** mit demselben Namen: zwei Dateien, zwei Arbeiten, zwei Äußerungen. Hier ist es **dasselbe Ding**: zwei Zeilen mit demselben Pfad zeigen auf dieselbe Datei, die zweite trägt keine Aussage. |
| **Die Dublette nur im Schema abfangen** | Der Aufrufer träfe auf eine nackte Datenbankmeldung ohne Code, Grund und Kompensationsaktion — genau das, was der Fehlervertrag aus `R00007` für Agenten ausschließt. Der Index bleibt trotzdem: er sichert die Regel gegen jeden Weg, der am Dienst vorbeischreibt. |
| **Groß-/Kleinschreibung beim Dublettenvergleich einebnen** | Wäre eine Annahme über fremde Dateisysteme. Die Zielplattform der Vision sind Linux-Repositorys, dort sind `README.md` und `readme.md` zwei Dateien. |
| **Ändern eines Dateiverweises** | Nicht gezeichnet, und die Zeile trägt nichts als den Pfad — entfernen und neu eintragen ist derselbe Vorgang in zwei Griffen. Eine Route, die niemand ruft, wäre tote Flexibilität (C17). |
| **Kein Entfernen** (Muster `I0017`) | Beim Kommentar galt „eine Route, die niemand ruft, wäre tote Flexibilität". Hier trägt das nicht: weil die Anwendung ausdrücklich nicht prüft, ob ein Pfad noch stimmt, **kann sie den Verfall nicht selbst bemerken**. Eine Liste, aus der nichts herausgeht, behauptet nach einiger Zeit Verweise, die ins Leere zeigen — und wäre schlechter als keine. |
| **Eine `Position` neben dem Zeitpunkt** | Der Zeitpunkt liefert die Ordnung, wie bei Kommentar und Anhang. Eine Position daneben wäre eine zweite Wahrheit und liefe beim ersten Zurückdatieren auseinander. |
| **HTTP 201 mit der geschriebenen Zeile** | Die Antwort trägt die Seite, die der Aufrufer betrachtet, nicht die geschriebene Zeile. Ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite (`R00018`–`R00020`). |
| **Der Urheber als Query-Parameter** | Der Rumpf ist der Ort, an dem dieses Projekt Kontributoren übergibt. Zwei Wege für denselben Wert wären Synonym-Wildwuchs. |
| **Zwei getrennte Leerzeilen** (je Hälfte eine) | Das Artboard zeichnet **eine** (`:425`), neben den Zeilen für Teilaufgaben und Kommentare. `R00020` hat sie bewusst halb gelassen und im Testkommentar festgehalten, dass die gemeinsame Fassung „erst mit I0019" entsteht. |
| **Eine eigene `.js`-Datei für die Zwischenablage** | Der `Identitaetsspeicher` zeigt, dass ein Inline-Ausdruck über `IJSRuntime` für einen einzeiligen Browseraufruf genügt; eine eigene Datei brächte einen zweiten Auslieferungsweg und eine Version, die mit dem C#-Code auseinanderlaufen kann. |
| **Ein zweites Feature „Zurückweisung ungültiger Pfade"** | Ein Fehlerpfad derselben Route, im selben E2E-Lauf belegt. Ein Feature, das nur die Interaction wiederholt, ist ein Fehler. |

### Bewusst out of scope

- **Vorschau des verwiesenen Inhalts.** Die Anwendung kann die Datei nicht lesen — sie liegt auf einer anderen Maschine. Im Artboard nicht gezeichnet, im Fertig-Kriterium nicht gefordert.
- **Prüfung, ob ein Verweis noch gültig ist** (ein Lauf, der Pfade gegen ein Dateisystem abgleicht). Setzte einen Wurzelpfad voraus, den es nicht gibt; siehe „Verworfene Alternativen". **Lücke mit Adresse:** falls das je gewünscht ist, braucht es zuerst eine Vereinbarung über den Klon — das ist ein eigener Slice samt Skizze.
- **Ein Kommentar oder eine Beschriftung am Dateiverweis** („warum dieser Pfad"). Die Zeile trägt nur den Pfad; wer etwas dazu sagen will, kommentiert die Karte.
- **Verweise auf andere Karten oder Boards** (interne Verknüpfung). Ein anderer Gegenstand mit anderer Prüfbarkeit; nicht gezeichnet, in keinem Fertig-Kriterium.
- **Sortieren oder Umordnen der Liste.** Der Zeitpunkt ordnet, wie bei Kommentar und Anhang.
- **Live-Aktualisierung der Dateiverweise** (ein zweiter Betrachter sieht den neuen Verweis ohne Reload). Gehört zu `D0007` und ist im Artboard nicht gezeichnet.
- **Übernahme der Dateiverweise in den Board-Export.** Gehört zu `I0038`.

### Angenommen im stillen Lauf

- **Höchstlänge 500 Zeichen** für den Pfad — zwischen Teilaufgabe (200) und Kartentitel (1000), wie die WBS es verlangt. Vor `B0285` zu bestätigen.
- **Der Abschnitt heißt „Dateiverweise"** und steht als **rechte** Hälfte der zweispaltigen Sektion — Ort wie im Artboard, Wortlaut nach der Begriffsentscheidung.
- **Der Wortlaut der gemeinsamen Leerzeile** ist der des Artboards mit einem getauschten Wort: „Keine Anhänge, keine Dateiverweise · hinzufügen". Die halben Fassungen lauten „Keine Anhänge · hinzufügen" (bestehend) und „Keine Dateiverweise · eintragen".
- **Der Urheber reist als ganzer `Kontributor`** im DTO, nicht als Nummer — dieselbe Entscheidung wie bei `Kommentar` und `Anhang`.
- **Die Begriffe stehen fest (C06):** `Dateiverweis` = die Zeile und der ganze Gegenstand · `Pfad` = der Text in der Spalte · `Urheber` = wer eingetragen hat · `Zeitpunkt` = wann · `Pfadkopie` = der Weg in die Zwischenablage. **Nicht** `Verweis` (im Code als Hyperlink vergeben), **nicht** `Dateipfad` (im Code der Pfad der SQLite-Datei), **nicht** `Referenz`, **nicht** `EingetragenAm` (die in `R00019` gesetzte Regel: `…Am` trägt im Stack nur reines Datum).
- **Die Fremdschlüsselspalten heißen `Karte` und `Kontributor`** nach den referenzierten Tabellen — Projektregel.
- **Der Befundcode der Dublette** lautet `dateiverweis-doppelt` und liegt **nicht** in `Nichtgefunden.AlleCodes` — 400, kein fehlendes Ding; dieselbe Logik wie bei `kontributor-stillgelegt`.
