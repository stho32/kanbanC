---
id: R00019
status: In Arbeit
datum: 2026-09-05
---

# R00019: Karte kommentieren

## Beschreibung

Eine Karte trägt **Kommentare**: Fließtexte, die untereinander in der Reihenfolge ihres Entstehens stehen und je Zeile sagen, **wer** sie geschrieben hat und **wann**. Geschrieben wird über `POST /api/karten/{karteId}/kommentare`; die Antwort ist HTTP 200 mit dem **ganzen `Kartendetail`** — dieselbe Antwortgestalt, die `R00017` für diese Seite festgelegt und `R00018` fortgeführt hat. In der Oberfläche steht der Abschnitt „Kommentare" auf `/karten/{karteId}` hinter „Teilaufgaben", mit einer Anzahl im Kopf, je Zeile Kürzel, Text und einer Metazeile „Name · Zeitpunkt", und einer Schreibzeile mit dem Kürzel des gewählten Kontributors und dem Knopf „senden".

Zahlt ein auf: [Vision](R00000-vision.md) — „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat."

**Der Urheber kommt aus der Identitätswahl des Browsers — damit löst dieser Slice die Zusage von `R00013` ein.** `R00013` hat den `Identitaetsspeicher` gebaut und ihn ausdrücklich als „die **eine Stelle**, an der die Identität dieses Browsers gelesen und geschrieben wird … zugleich die Naht, aus der `I0017`, `I0023` und `I0024` später den Urheber ziehen" beschrieben (`R00013`, Grobentwurf, Zeile 125); unter „Bewusst out of scope" steht dort: „**Der Urheberschaftsparameter an schreibenden Endpunkten** (`I0017`, …). Er entsteht dort, wo der Urheber zum ersten Mal abgelegt und gelesen wird; hier entsteht nur die Naht, aus der er gezogen wird" (Zeile 236), und unter „Blockiert" nennt `R00013` `I0017` als ersten der drei Knoten, die darauf warten (Zeile 170). **Hier ist das Erste, was abgelegt und gelesen wird**, und damit die erste Route, die einen Urheber trägt.

**Der Zeitpunkt trägt als erster im Projekt eine Uhrzeit.** Der ganze Stack kennt bisher nur reines Datum (`ErledigtAm`, `FaelligAm`, `StillgelegtAm` — alle `DateOnly?`). Deshalb heißt das Feld **`Zeitpunkt`** und nicht `GeschriebenAm`: die Konvention `<Verb>Am` steht im Bestand ausnahmslos für ein Datum ohne Uhrzeit, und ein `…Am` mit Uhrzeit machte aus einer verlässlichen Namensregel eine, bei der man erst den Typ nachsehen muss. `Zeitpunkt` ist zudem das Wort des Fertig-Kriteriums von `I0017`.

## Geschäftlicher Nutzen

Eine Karte kann seit `R00017` sagen, **was** zu tun ist, und seit `R00018`, **woraus** die Arbeit besteht. Sie kann bis heute nicht sagen, **wer etwas dazu gesagt hat**. Wer eine Rückfrage stellt oder eine Entscheidung begründet, hat dafür nur die Beschreibung — und die trägt keinen Absender: der nächste, der sie ändert, überschreibt den vorigen, und niemand sieht mehr, von wem der Satz stammte oder wann er entstand.

Damit fällt genau die Zusage aus, die den Kern der Vision trägt: dass an jeder Karte ablesbar ist, wer oder was gehandelt hat. Für den Menschen heißt der Kommentar ein Ort für die Rückfrage, die die Beschreibung nicht überschreibt. Für den KI-Agenten heißt er mehr: er kann melden, was er gefunden hat, während er arbeitet, und ein Mensch sieht am Kürzel sofort, dass die Meldung von einem Agenten kommt und nicht von einem Kollegen — die Karte wird das Gespräch über die Arbeit, geführt von beiden Akteuren über dieselbe Route. Und weil der Urheber ab hier abgelegt wird, hat der Timer aus `I0023`/`I0024` später eine Form, der er folgen kann.

## Funktionale Anforderungen

- `POST /api/karten/{karteId}/kommentare` schreibt **einen** Kommentar an die Karte und antwortet mit dem vollständigen `Kartendetail`.
- Der Aufruf trägt **im Rumpf** den Text und die `KontributorId` des Urhebers; es gibt keinen Query-Parameter dafür.
- Das `Kartendetail` trägt die Kommentare der Karte, sortiert nach **Zeitpunkt** (älteste oben); die Reihenfolge ist damit die Zeitordnung und wird nicht getrennt gespeichert.
- Jeder Kommentar trägt eine eigene Nummer (`KommentarId`), den **ganzen** Urheber (Nummer, Name, Art, Stilllegungsstand) und seinen `Zeitpunkt`.
- Der Zeitpunkt wird beim Schreiben von der Anwendung gesetzt, nicht vom Aufrufer mitgegeben.
- Ein **leerer oder zu langer** Text wird mit Befund zurückgewiesen; gespeichert wird nichts.
- Zwei gleichlautende Kommentare an derselben Karte sind **erlaubt** — zwei Äußerungen sind zwei Äußerungen.
- Eine **unbekannte Karte** und ein **unbekannter Kontributor** werden mit HTTP 404 samt Rumpf beantwortet, ein **stillgelegter** Kontributor mit HTTP 400 samt Rumpf.
- Ein Kommentar **ohne** Urheber ist nicht möglich: die Anfrage führt die `KontributorId` als Pflichtwert, die Spalte ist `NOT NULL`.
- Die Kartenseite zeigt den Abschnitt „Kommentare" mit der Anzahl, je Zeile Kürzel, Text und „Name · Zeitpunkt", und eine Schreibzeile mit dem Kürzel des Gewählten und dem Knopf „senden".
- Hat die Karte keinen Kommentar, steht dort die **Handlung statt einer Null**: „Noch kein Kommentar · schreiben".
- Der Zeitpunkt erscheint **je nach Alter verschieden**: innerhalb der letzten Stunde relativ („vor 22 Min"), heute und gestern mit Tageszeit („gestern 17:40"), älter mit ISO-Datum und Tageszeit („2026-08-30 17:40").
- Ist **keine Identität gewählt**, ist „senden" gesperrt und die Schreibzeile sagt, was zu tun ist; die übrige Anwendung bleibt unverändert benutzbar.
- Alle Kommentare sind nach einem Reload und nach einem Neustart unverändert da.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** `013-kartenkommentar.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Migrationslaeufer.cs:16-23`). Deshalb eine eigene Tabelle statt `ALTER TABLE`, wie bei den Migrationen 004, 005, 007, 008, 009, 010, 011 und 012.
- **Zeitform:** `DateTimeOffset` in den Contracts, **ISO-8601-Text in UTC** in der Spalte. `DateTimeOffset` statt `DateTime`, weil der Wert über HTTP zu Agenten reist und ein `DateTime` unterwegs seine Zeitzone verliert; **UTC**, weil Text nur dann lexikografisch wie chronologisch sortiert und `ORDER BY Zeitpunkt` sonst eine stille Lüge wäre.
- **Unbelegter Boden wird erst belegt:** Die Materialisierung eines Zeitstempels mit Uhrzeit ist im Repository nirgends nachgewiesen — der Bestand führt ausschließlich `DateOnly`. Vor der ersten produktiven Nutzung steht deshalb ein Probe-Test nach Skill `dependency-probe` (Rundreise durch eine TEXT-Spalte, Textsortierung, Fault-Injection mit einem Text, der kein ISO-Zeitstempel ist). Vorbild sind die drei `DateOnly`-Fälle derselben Datei (`SqliteEigenschaftenTests.cs:89-131`), die die bequeme Annahme gerade **widerlegt** haben (Dapper 2.1.79 weist `DateOnly` als Parameterwert ab); in `R00018` fiel dieselbe Klasse von Annahme ein zweites Mal (`Int64` statt `bool`).
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der neuen Route trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404. **Die Vertragsfälle gehören in denselben Arbeitsgang wie die Route** (`FehlervertragTests.cs:41-58`; Lehre aus `B0152`/`B0159`): der Test liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot.
- **Antwortgestalt:** **200 mit dem ganzen `Kartendetail`**, nicht 201 mit der geschriebenen Zeile — wie `R00018` und aus demselben Grund: die Antwort trägt die Seite, die der Aufrufer betrachtet, und ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"). Das Kürzel nutzt die bestehende `Kontributorartform.Kuerzelklasse`, damit Mensch und Agent auch hier verschieden aussehen.
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier keine Projektreferenz auf `KanbanC.BL`; der Kommentarabschnitt spricht ausschließlich über HTTP.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün, mit den fünf benannten Änderungen (siehe Akzeptanzkriterien). Insbesondere bleibt `Kopfzeile.razor` und damit `R00013` **unberührt**.

## Akzeptanzkriterien

### Der Kommentar wird mit Urheber und Zeitpunkt festgehalten (API)

- [x] `POST /api/karten/{karteId}/kommentare` mit Text und `KontributorId` antwortet mit HTTP 200 und einem `Kartendetail`, dessen Kommentarliste diesen Text als **letzten** Eintrag trägt.
- [x] Der Eintrag trägt eine eigene, von den anderen verschiedene `KommentarId`.
- [x] Der Eintrag trägt den **ganzen** Urheber: Nummer, Name, Art und Stilllegungsstand — nicht nur die Nummer.
- [x] Der Eintrag trägt einen `Zeitpunkt` mit Uhrzeit. Rechenbeispiel: wird die Uhr **vor** dem Aufruf als `t0` und **nach** dem Aufruf als `t1` gemerkt, gilt `t0 ≤ Zeitpunkt ≤ t1`.
- [x] Der Aufrufer kann den Zeitpunkt **nicht** mitgeben: die Anfrage hat kein Feld dafür, und ein mitgeschicktes Feld ändert nichts am gespeicherten Wert.
- [x] `GET /api/karten/{karteId}` liefert danach dieselbe Liste in derselben Reihenfolge.
- [x] Rechenbeispiel Reihenfolge: an eine Karte ohne Kommentare werden nacheinander `A`, `B`, `C` geschrieben → die Liste lautet in jedem folgenden Abruf `A`, `B`, `C` (ältester oben).
- [x] Zwei Aufrufe mit **demselben** Text und demselben Urheber werden beide angenommen und erzeugen zwei Einträge mit verschiedenen Nummern.
- [x] Die Kommentare hängen am `Kartendetail` und **nicht** an `Karte`: `GET /api/boards/{boardId}` liefert die Karten unverändert ohne Kommentarliste.
- [x] Es gibt **kein** gespeichertes Zählfeld und **keine** gespeicherte Position: die Antwort trägt weder eine Anzahl noch eine Ordnungszahl neben der Liste.
- [x] Ein Kommentar eines inzwischen **stillgelegten** Kontributors bleibt an der Karte sichtbar, mit Name und Stilllegungsstand — das ist der zweite Halbsatz des Fertig-Kriteriums von `I0009`, den `R00014` ausdrücklich hierher weitergereicht hat.
- [x] Ein Neustart der Anwendung lässt Texte, Urheber, Zeitpunkte und Reihenfolge unverändert.
- [x] Die Sortierung ist die Zeitordnung: werden zwei Kommentare **in der Datenbank** auf verschiedene Zeitpunkte gesetzt, steht der ältere in der Antwort oben — unabhängig von der Reihenfolge, in der sie geschrieben wurden.

### Kein Kommentar ohne Urheber — Zurückweisung und Fehlerantworten für Agenten

- [x] Ein leerer Text (auch ein Text nur aus Leerzeichen) wird mit HTTP 400 **und Rumpf** zurückgewiesen; die Liste der Karte bleibt danach unverändert.
- [x] Ein zu langer Text wird ebenso mit HTTP 400 und Rumpf zurückgewiesen; nichts wurde gespeichert.
- [x] Randleerzeichen fallen weg, der Text im Übrigen nicht: `"  Bitte prüfen  "` wird als `"Bitte prüfen"` gespeichert, Groß- und Kleinschreibung bleibt.
- [x] Eine **unbekannte** `karteId` wird mit HTTP 404 und einem Befund beantwortet, der Code, die aufgerufene Kartennummer und einen ausführbaren nächsten Aufruf nennt. Der Befund nennt **kein** Board — die Route kennt keins.
- [x] Eine **unbekannte** `KontributorId` wird mit HTTP 404 und einem Befund beantwortet, der die aufgerufene Nummer und den Weg zur Kontributorenliste nennt; an der Karte hat sich nichts geändert.
- [x] Eine **stillgelegte** `KontributorId` wird mit HTTP **400** und Rumpf zurückgewiesen — es fehlt kein Ding, es wurde eine Regel verletzt; an der Karte hat sich nichts geändert.
- [x] Die Meldung der Stilllegung passt zum Kommentar: sie sagt **nicht** „kann nicht verantwortlich sein" — dieser Wortlaut gehört dem Verantwortlichen an der Karte und wäre hier eine Falschaussage.
- [x] Die Route liefert **keine** Fehlerantwort mit leerem Rumpf.
- [x] Der Vertragstest über alle registrierten Routen bleibt grün: die neue Route wird von ihm abgerufen und steht nicht als ungeprüft übrig (`FehlervertragTests.cs:41-58`).

### Der Abschnitt auf der Kartenseite

- [x] Auf `/karten/{karteId}` steht hinter „Teilaufgaben" ein Abschnitt mit der Überschrift **„Kommentare"** und der Anzahl daneben. Rechenbeispiel: drei Kommentare → „3".
- [x] Jede Zeile zeigt das **Kürzel** des Urhebers, den Text und darunter „Name · Zeitpunkt".
- [x] Ein Kommentar eines Agenten und einer eines Menschen sind am Kürzel auseinanderzuhalten (verschiedene Kürzelklasse aus `Kontributorartform`).
- [x] Hat die Karte **keinen** Kommentar, steht dort „Noch kein Kommentar · schreiben" — keine „0", keine leere Liste.
- [x] Die Schreibzeile trägt das Kürzel des **gewählten** Kontributors und den Knopf „senden"; nach dem Senden steht der neue Kommentar als letzter in der Liste und das Feld ist wieder leer.
- [ ] Getippte Zeichen gehen **nicht** verloren, auch wenn währenddessen eine Antwort eintrifft: das Eingabefeld führt seinen Text selbst (`@ref` + `@oninput`, **kein** `value`-Attribut) — dieselbe Bauform wie das Teilaufgaben- und das Etikettenfeld, und die Lehre aus dem Oberflächenfehler in `I0015`.
- [x] Ein leerer Text bringt eine lesbare Meldung auf der Seite und schreibt nichts.
- [x] **Ist keine Identität gewählt, ist „senden" gesperrt** und die Schreibzeile weist auf die Identitätswahl in der Kopfzeile hin. Board, Kartenseite und alle übrigen Handlungen bleiben ohne Wahl unverändert benutzbar.
- [x] Wird die Identität in der Kopfzeile gewechselt, **während** die Kartenseite offen ist, trägt der nächste gesendete Kommentar den **neu** gewählten Urheber — ohne Reload.
- [x] Die Zeitangabe richtet sich nach dem Alter. Rechenbeispiele bei „jetzt" = `2026-08-31 12:00`: `11:38` desselben Tages → „vor 22 Min"; `2026-08-30 17:40` → „gestern 17:40"; `2026-08-25 17:40` → „2026-08-25 17:40".
- [x] Nach einem Reload zeigt die Seite dieselben Kommentare mit denselben Urhebern und denselben Zeitangaben.
- [x] Die Anzahl im Kopf kommt **nicht** aus der Antwort: es gibt kein Feld im `Kartendetail`, das sie trägt.

### Der grüne Bestand bleibt grün — mit fünf benannten Änderungen

- [x] **Benannte Änderung 1:** `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) wächst um `IReadOnlyList<Kommentar> Kommentare`. Das sind **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:353`); beide werden angepasst, ihre Zusicherungen nicht.
- [x] **Benannte Änderung 2:** `Stillgelegt` (`Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs`) bekommt eine **Schwester** für den Urheber: **derselbe Code** `kontributor-stillgelegt` (400), aber eine eigene Meldung. Wortlaut und Kompensation des bestehenden `Stillgelegt.Kontributor` bleiben unverändert. `Nichtgefunden` bekommt **keinen** neuen Eintrag — `Nichtgefunden.Karte(karteId)` und `Nichtgefunden.Kontributor(kontributorId)` bestehen beide.
- [x] **Benannte Änderung 3:** `FehlervertragTests` (`Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs:41-58`) wird rot, sobald die Route ohne Vertragsfall registriert ist. Die Vertragsfälle entstehen deshalb **in demselben Arbeitsgang** wie die Route, nicht danach.
- [x] **Benannte Änderung 4:** `KartendetailSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs`) wächst um die Locator des Abschnitts, und `KartendetailOeffnenE2ETests` um den Kommentar-Leerstand in der Leerzustandszeile; die bestehenden Locator und Zusicherungen bleiben unverändert.
- [x] **Benannte Änderung 5:** `Testdatenbank` (`Source/KanbanC.PlaywrightTests`) bekommt `SetzeKommentarzeitpunkt` neben dem bestehenden `SetzeErledigung` (`:27`).
- [x] **`Kopfzeile.razor` bleibt unverändert** — die Kartenseite injiziert den `Identitaetsspeicher` selbst; kein `CascadingValue`, kein Zustandsdienst, kein `EventCallback`. `R00013` wird nicht angefasst.
- [x] `KartenRepository.SchreibeErledigung`/`Heute()` (`:335-341`) und `KontributorenRepository` (`:96`) bleiben unverändert — es wird **keine** Uhr-Abstraktion eingeführt.
- [ ] Alle E2E-Suiten aus `R00001`–`R00018` bleiben **ohne Änderung** grün, insbesondere die Kartendetail-Suiten von `R00017` und `R00018` und die Identitätssuite von `R00013`.
- [x] `GET /api/boards/{boardId}` und alle bestehenden Kartenrouten bleiben in Adresse, Verb und Antwortgestalt unverändert.
- [x] Der zweite Lauf des `Migrationslaeufer` auf einer bestehenden Datei lässt Schema und Daten unverändert.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/013-kartenkommentar.sql` — neue, idempotente Migration; Tabelle `Kommentar` mit `KommentarId` als Primärschlüssel, `Karte` und `Kontributor` als Fremdschlüssel (nach der Projektregel benannt wie die referenzierte Tabelle), `Text`, `Zeitpunkt TEXT NOT NULL`, dazu ein eigener Index auf `Karte`. **Keine `Position`.**
- **Contracts:** `Source/KanbanC.Contracts/Karten/Kommentar.cs` (neu), `KommentarSchreibenAnfrage.cs` (neu), `Kartendetail.cs` (wächst um die Kommentarliste).
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/KommentarValidator.cs` und `Kommentartext.cs` (neu); `Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs` (Schwester für den Urheber).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Kommentarleser.cs` (neu), `Kartenleser.cs` (`LiesKartendetail` führt die Kommentare mit, `:93`), `KartenRepository.cs` (`SchreibeKommentar`), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `SchreibeKommentar`, mit einem Befund zum Urheber neben dem bestehenden `BefundZumVerantwortlichen` (`:253-274`).
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — **eine** neue Route als Unterressource der boardlosen Kartenadresse, neben `/etiketten` (`:28`) und `/teilaufgaben` (`:33`).
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`SchreibeKommentar` über `AlsKartendetail`, `:64`), `Source/KanbanC.Blazor/Services/Zeitpunktform.cs` (neu), `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` (Abschnitt hinter „Teilaufgaben", `Kartendetail.razor:113-192`) — die Seite injiziert zusätzlich den bestehenden `Identitaetsspeicher` (`Program.cs:26`).
- **Unberührt:** `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor`, `Source/KanbanC.Contracts/Karten/Karte.cs`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor` — **auf der Bahn und in der Kopfzeile ändert sich nichts**.
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Karten/KommentarValidatorTests.cs`, `Integrations/Karten/KartenServiceTests.cs`, `TestHelpers/TestKartenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenApiKlientTests.cs`, `Services/ZeitpunktformTests.cs`), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/SqliteEigenschaftenTests.cs` — die Probe, `Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (`PageObjects/KartendetailSeite.cs`, `Infrastruktur/Testdatenbank.cs`, `Tests/KartendetailOeffnenE2ETests.cs`, neue Testklasse `KarteKommentierenE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0004.dc.html`](../Dokumentation/Wireframes/D0004.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gilt daraus der **Abschnitt mit dem Vermerk `I0017`** (`:177-202`): Überschrift mit Anzahl, Zeilen aus rundem Kürzel, Text und Metazeile, Schreibzeile mit Kürzel, Feld und Knopf „senden". Dazu der **Leerzustand** der frischen Karte (`:424`) als Handlung statt Null. Die Lesehilfe (`:536`) ordnet den Abschnitt `I0017` zu. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Eine bewusste Abweichung, benannt statt stillschweigend:** Das Artboard schreibt in die Metazeile jeder Kommentarzeile eine **Quelle** — „Claude-Agent · vor 22 Min · **API**" (`:186`) und „Stefan · gestern 17:40 · **Oberfläche**" (`:195`) — und nennt sie in der Lesehilfe (`:536`). Gebaut wird die Metazeile **ohne** sie; die Begründung steht unter „Bewusst out of scope". Alles andere am Abschnitt folgt der Skizze.

### Ablauf

1. **Kommentar schreiben** (`POST /api/karten/{karteId}/kommentare`)
   - 1.1 `KommentarValidator.Pruefe(karteId, anfrage)` — leerer Text, zu langer Text; die Kompensation nennt die Route des Aufrufers samt Kartennummer. **Der Urheber wird hier nicht geprüft**: seine beiden Regeln brauchen den Kontributorenbestand
   - 1.2 Bei Befunden: HTTP 400 mit Rumpf, **kein** Schreibzugriff
   - 1.3 `KartenService` prüft den Urheber — dieselben zwei Regeln wie `BefundZumVerantwortlichen` (`KartenService.cs:253-274`), nur ist der Urheber **Pflicht** und nicht `long?`
     - 1.3.1 Kontributor unbekannt → `Nichtgefunden.Kontributor(kontributorId)` → HTTP 404
     - 1.3.2 Kontributor stillgelegt → die neue Schwester in `Stillgelegt` → HTTP 400
   - 1.4 `KartenRepository.SchreibeKommentar(karteId, anfrage)` in **einer** Transaktion
     - 1.4.1 Existiert die Karte nicht: `null` → HTTP 404 mit `Nichtgefunden.Karte(karteId)`
     - 1.4.2 `INSERT` mit `DateTimeOffset.UtcNow`, als ISO-8601-Text abgelegt
     - 1.4.3 `Kartenleser.LiesKartendetail` in derselben Transaktion, dann `Commit`
   - 1.5 HTTP 200 mit dem ganzen `Kartendetail`
2. **Lesen** (`GET /api/karten/{karteId}`)
   - 2.1 `Kommentarleser.LiesKommentareDerKarte` — `ORDER BY Zeitpunkt, KommentarId`
   - 2.2 Die beiden JOINs auf `Kontributor` und `Kontributorstilllegung` wie im `Kartenleser` (`:71-76`), damit der ganze Urheber mitreist
   - 2.3 Die Liste hängt am `Kartendetail`, nicht an `Karte`; **kein** Archivfilter, wie das ganze Kartendetail
3. **Oberfläche**
   - 3.1 Die Kartenseite injiziert den `Identitaetsspeicher` selbst und liest die Wahl **beim Senden**, nicht beim Laden
   - 3.2 Ohne gewählte Identität: „senden" gesperrt, Hinweis an der Schreibzeile; kein Aufruf
   - 3.3 `KartenApiKlient.SchreibeKommentar` über `AlsKartendetail` (`KartenApiKlient.cs:64`) — 400 und 404 laufen denselben Weg
   - 3.4 `Zeitpunktform.AlsText(zeitpunkt, jetzt)` formt die Metazeile; `jetzt` ist ein **Parameter**
   - 3.5 `Kartendetail.razor` ersetzt nach der Antwort das ganze `_detail` — kein zweiter Abruf

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — eine neue Route als Unterressource der boardlosen Kartenadresse, neben der Teilaufgabenroute (`:33`). Die Adresse trägt kein Board, weil die Seite keins kennt.
- **`Kartenleser.LiesKartendetail`** (`:93`) — der eine Ort, an dem das Detail entsteht; hier reihen sich die Kommentare ein.
- **`Migrationslaeufer`** — die dreizehnte Migration reiht sich ein; kein Journal, also idempotent.
- **`Kartendetail.razor`** (`:113-192`) — der Abschnitt kommt hinter „Teilaufgaben" in die linke Spalte.
- **`Identitaetsspeicher`** (`Source/KanbanC.Blazor/Services/Identitaetsspeicher.cs`, `AddScoped` in `Program.cs:26`) — die Naht aus `R00013`; die Kartenseite zieht den Urheber hier heraus.

**Klassen-Entwurf:**

- `Kommentar` (DTO, immutable) — eine Äußerung an der Karte mit eigener Identität. **Der Urheber reist als ganzer `Kontributor`**, nicht als Nummer: die Zeile zeigt Name und Kürzel, das Kürzel folgt aus Name und Art, und `StillgelegtAm` liefert den Zusatz „stillgelegt" ohne ein zweites Feld — dieselbe Entscheidung wie bei `Kartendetail.Verantwortlicher`.
  - `record Kommentar(long KommentarId, string Text, Kontributor Urheber, DateTimeOffset Zeitpunkt)`
- `KommentarSchreibenAnfrage` (DTO, immutable) — Text und Urheber; **`Kontributor` ist Pflicht** (`long`, nicht `long?`) — es gibt kein „niemand". Kein Feld für den Zeitpunkt: den setzt die Anwendung.
  - `record KommentarSchreibenAnfrage(string Text, long Kontributor)`
- `Kartendetail` (DTO, immutable) — wächst um `IReadOnlyList<Kommentar> Kommentare`. **Kein Zählfeld daneben** und **keine Position**: die Anzahl rechnet die Oberfläche, die Ordnung liefert der Zeitpunkt.
- `Kommentartext` (Operation, pure Logik) — Muster `Teilaufgabentext`: nur Randleerzeichen fallen weg.
  - `static string Normalisiert(string text)`
- `KommentarValidator` (Operation, pure Logik) — Muster `TeilaufgabenValidator`, mit der Route des Aufrufers in der Kompensation. **Kein Dublettenbefund**, **keine Urheberprüfung**.
  - `static Pruefbefunde Pruefe(long karteId, KommentarSchreibenAnfrage anfrage)`
- `Kommentarleser` (Provider/Ressourcenzugriff) — liest die Kommentare einer Karte in Zeitpunkt-Reihenfolge, mit dem ganzen Urheber, in der laufenden Transaktion.
  - `static IReadOnlyList<Kommentar> LiesKommentareDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)`
- `KartenRepository` (Provider, Integration nach Hausregel) — ein Schreibweg, mit dem ganzen Detail als Rückgabe, `null` bei fehlender Karte.
  - `Kartendetail? SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage)`
- `KartenService` (Integration, prüft/fängt) — dieselbe Antwortgestalt wie `AendereKarte`, `SetzeEtiketten` und `LegeTeilaufgabeAn`.
  - `Ergebnis<Kartendetail> SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage)`
- `Stillgelegt` (Operation) — eine Schwester mehr, derselbe Code, eigene Meldung für den Urheber.
  - `static Fehlerbefund Urheber(long kontributorId)`
- `Zeitpunktform` (Operation in der Oberflächenschicht, pure Logik) — Muster `Teilaufgabenfortschritt` und `Bahnenkopfzahl`. **„jetzt" ist ein Parameter, keine Uhr im Inneren.**
  - `static string AlsText(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)`
- `KartenApiKlient` (Integration) — ein Aufruf mehr, über `AlsKartendetail` (`:64`).
  - `Task<ApiErgebnis<Kartendetail>> SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage)`

### Änderungen an bestehenden Klassen

- `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) — ein Feld mehr. **Änderung an grünem Bestand, aber mit kleiner Breite:** genau **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:353`) gegen 16 an `Karte`. Genau deshalb hängt die Liste hier und nicht an `Karte`.
- `Kartenleser` (`:93`) — `LiesKartendetail` führt die Kommentare mit, wie schon Etiketten und Teilaufgaben. Kein Archivfilter.
- `KartenRepository` — ein Schreibweg dazu, Muster `LegeTeilaufgabeAn` (`:211-230`): Existenzprüfung, Schreiben und Rückgabe des ganzen Details in **einer** Transaktion. Der Zeitpunkt kommt aus `DateTimeOffset.UtcNow` an derselben Stelle, an der `DateTime.Today` für die Erledigung steht (`:335-341`) — **keine Uhr-Abstraktion**.
- `IKartenRepository` — eine Signatur dazu; `TestKartenRepository` zieht mit.
- `KartenService` — ein Prüfweg für den Urheber neben `BefundZumVerantwortlichen` (`:253-274`). Die zwei Regeln sind dieselben; der Unterschied ist, dass der Urheber Pflicht ist: beim Verantwortlichen ist `null` ein gültiger Wert („niemand"), beim Urheber gibt es kein „niemand".
- `Stillgelegt` (`:11`) — die Schwester für den Urheber. **Derselbe Code** `kontributor-stillgelegt`, damit `Nichtgefunden.MeldetEinFehlendesDing` ihn weiterhin **nicht** zu 404 zählt; eigene Meldung, weil die bestehende wörtlich „kann nicht verantwortlich sein" sagt.
- `KartenEndpunkte` (`:33` Teilaufgabenroute als Muster) — eine Routenkonstante und eine Registrierung. **Die Vertragsfälle jeder Route gehören in denselben Arbeitsgang wie die Route**: `FehlervertragTests.cs:41-58` liest die registrierten Routen aus dem Testhost, und zwischen Route und Vertragsfall ist die Suite rot. Vier Fälle: 400 leerer Text, 404 unbekannte Karte, 404 unbekannter Kontributor, 400 stillgelegter Kontributor.
- `Kartendetail.razor` (`:113-192`) — ein Abschnitt mehr hinter „Teilaufgaben", samt Schreibzeile und Zurückweisungsmeldung. Die Eingabezeile ohne `value`-Attribut, mit `@ref` + `@oninput` + `@onkeydown` — dieselbe Bauform wie das Teilaufgabenfeld.
- `KartendetailSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs`) — Locator für Abschnitt, Anzahl, Zeilen, Kürzel, Metazeile, Eingabefeld, Sendeknopf, Leerzustand und Meldung.
- `KartendetailOeffnenE2ETests` — die Leerzustandszeile wächst um den Kommentar-Leerstand.
- `Testdatenbank` (`:27` `SetzeErledigung` als Nachbar) — `SetzeKommentarzeitpunkt`, für den zurückdatierten E2E-Fall.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Probe vor der ersten produktiven Nutzung** (Skill `dependency-probe`, in `SqliteEigenschaftenTests`): (1) Dapper materialisiert eine ISO-8601-TEXT-Spalte in einen `DateTimeOffset`-Record-Parameter; (2) ein UTC-Zeitstempel im Format `O` sortiert als Text chronologisch, `ORDER BY Zeitpunkt` ist also die Zeitordnung; (3) **Fault-Injection**: ein Text, der kein ISO-Zeitstempel ist, scheitert sichtbar, statt still zu irgendeinem Moment zu werden. Fällt (1), geht der Zeitpunkt denselben Weg wie das Datum — als Text gelesen, in C# umgerechnet; das ändert den Leser, nicht die Anforderung.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Kommentartext.Normalisiert` — Randleerzeichen fallen weg, Groß-/Kleinschreibung und innere Leerzeichen bleiben.
- `KommentarValidator.Pruefe` — leerer Text, Text nur aus Leerzeichen, zu langer Text, gültiger Text ohne Befund; **zwei gleichlautende Texte ergeben keinen Befund**; die Kompensation nennt `POST /api/karten/{karteId}/kommentare` samt Nummer.
- `Zeitpunktform.AlsText` (in `KanbanC.Blazor.Tests`) — alle Formen und ihre **Ränder**, mit „jetzt" als Parameter: 59 Minuten („vor 59 Min") gegen 61 Minuten (Tageszeit), Mitternachtsgrenze heute/gestern, gestern gegen vorgestern (ISO-Datum). Ohne jede Zeitmanipulation prüfbar — das ist die stellbare Uhr an der einen Stelle, an der sie gebraucht wird.
- `KartenService.SchreibeKommentar` gegen `TestKartenRepository` — Erfolg reicht das Detail durch; unbekannte Karte, unbekannter Kontributor und stillgelegter Kontributor liefern Befunde mit nichtleerem Code, Meldung und Kompensation; nach einer Zurückweisung wurde **nicht geschrieben**; der Befund zum stillgelegten Urheber trägt eine **andere Meldung** als der zum stillgelegten Verantwortlichen.
- `KartenApiKlient.SchreibeKommentar` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und **Rumpf** des abgesetzten Aufrufs werden mitgeprüft, insbesondere dass die `KontributorId` im Rumpf und **nicht** in der Query steht. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository.SchreibeKommentar` und `Kommentarleser` gegen eine `TemporaereDatenbank` — schreiben und wieder lesen; der gespeicherte Zeitpunkt liegt im **Zeitfenster** `t0 ≤ Zeitpunkt ≤ t1` (echte Zustandsänderung, kein zurückgelesener Testwert); zwei nachträglich verschieden datierte Zeilen kommen in Zeitordnung zurück; der Urheber kommt vollständig zurück, auch wenn er stillgelegt ist; `null` bei unbekannter Karte; alles in einer Transaktion. `Kartenleser.LiesKartendetail` liefert die Liste auch für eine **archivierte** Karte. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — die Route mit 200, 400 (leerer Text; stillgelegter Kontributor) und 404 (unbekannte Karte; unbekannter Kontributor) samt Rumpf; `GET /api/boards/{boardId}` trägt danach **keine** Kommentarliste an den Karten; `FehlervertragTests` ruft die Route ab. `WebApiNeustartTests` — Texte, Urheber, Zeitpunkte und Reihenfolge überstehen den Neustart.

**E2E:** Eine Karte auf `/karten/{karteId}` ohne Kommentar zeigt „Noch kein Kommentar · schreiben". Mit gewählter Identität einen Kommentar schreiben → die Zeile erscheint mit Kürzel, Text und „Name · Zeitpunkt", Anzahl springt auf „1"; Reload zeigt sie unverändert (US-1). Leerer Text → lesbare Meldung, nichts geschrieben (US-3). Ein zurückdatierter Kommentar zeigt „gestern HH:MM" — **am Dienst vorbei** über `Testumgebung.Aktuelle.Datenbank.SetzeKommentarzeitpunkt`, derselbe Weg, den `AeltereNachladenE2ETests.cs:202` für zwei Erledigungstage geht, und aus demselben Grund: über die Uhr des Testlaufs ließen sich zwei verschiedene Zeitpunkte nicht herstellen (US-2). In einem **frischen Browserkontext** ohne gewählte Identität ist „senden" gesperrt (`sessionStorage` ist je Tab eigen, `R00013`) (US-5). Dazu laufen die E2E-Suiten aus `R00001`–`R00018` weiter — **ohne Änderung**; das ist die Gegenprobe des Slice.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün) und **`R00013`** (Identität wählen — `I0008`, grün). Beide nennt die WBS-Spalte `Braucht` von `I0017`; beide sind erfüllt, der Slice ist **frei**. `R00013` ist die fachlich tragende: ohne die gewählte Identität im Browser gäbe es keinen Urheber zu ziehen.
- Setzt außerdem auf: **`R00017`** (`I0015`, grün — die Kartenseite, die Adresse `/karten/{karteId}`, das `Kartendetail` als Antwortgestalt, `KartenApiKlient.AlsKartendetail`, `KartendetailSeite`) und **`R00018`** (`I0016`, grün — der Abschnitt darüber, das korrigierte Eingabefeld ohne `value`, die zweite Aufrufstelle von `new Kartendetail(`). Die Spalte `Braucht` von `I0017` nennt `I0015` nicht; das ist in der WBS als offene Frage vermerkt und gehört in `/planung aendern I0017`, wenn die Herkunft dokumentiert bleiben soll. An Front und Welle ändert es nichts, weil `I0015` grün ist.
- Setzt ferner auf: **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css`), **`R00011`**/**`R00014`** (`Kontributor`, `Kontributorstilllegung`, `Kontributorartform`), **`R00016`** (`LiesKartendetail` ohne Archivfilter).
- Löst ein: die von **`R00013`** hierher weitergereichte Zusage (der `Identitaetsspeicher` als Naht, aus der der Urheber gezogen wird) und den zweiten Halbsatz des Fertig-Kriteriums von **`I0009`**/`R00014` („bleibt aber an alten Karten … sichtbar"), der dort mangels Kontributorbezug an der Karte nicht ehrlich prüfbar war.
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0017` in seiner Spalte `Braucht` (geprüft am 2026-09-05 über `Dokumentation/Planung/kanbanc.md`). Fachlich folgen ihm `I0023`/`I0024` (Timer), die denselben Urheber-Weg gehen werden.

## Umfang

```
Karte kommentieren (I0017) = 12 Bubbles: 10 Standard (8,8h), 2 unklar (2,4–5,5h).
Rest: 8,8h klar + 2,4–5,5h unklar · 7 von 12 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0017` ist vollständig bis zur Bubble geplant und trägt seine zwölf Bubbles (`B0254`–`B0265`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: die Interaction hat einen prüfbaren Aspekt, nicht mehrere. Schreiben und Lesen teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; als zwei Features geführt wären es zwei Slices, die nur nacheinander gehen und dasselbe Verhalten teilen. Ein zweites Feature „Zurückweisung ohne Urheber" wurde geprüft und verworfen: es ist ein Fehlerpfad derselben Route, im selben E2E-Lauf belegt. **Die Requirement-Klammer sitzt deshalb allein an `I0017`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0254` Probe: Zeitpunkt durch eine TEXT-Spalte | Probe (`dependency-probe`) | 0,4–1,5h (**unklar**) |
| `B0255` Kommentartabelle anlegen | Provider (Migration) | 0,4h (belegt über `B0243`) |
| `B0256` Kommentare am Kartendetail lesen | Contracts + Provider | 0,4h (belegt über `B0244`) |
| `B0257` Kommentartext prüfen | Operation | 0,4h (belegt über `B0245`) |
| `B0258` Kommentar schreiben | Provider | 0,4h (belegt über `B0246`) |
| `B0259` Kommentar verdrahten | Integration | 0,4h (belegt über `B0248`) |
| `B0260` Endpunkt des Kommentars | Integration | 2h (Richtwert) |
| `B0261` API-Klient der Kommentare | Integration | 2h (Richtwert) |
| `B0262` Zeitpunkt als Text | Operation (Oberfläche) | 0,4h (belegt über `B0251`) |
| `B0263` Urheber aus dem Browser in den Aufruf | UI-Verdrahtung | 0,4h (belegt über `B0229`) |
| `B0264` Kommentarabschnitt der Kartenseite | UI | 2h (Richtwert) |
| `B0265` E2E Karte kommentieren | E2E | 2–4h (**unklar**) |

Mit 12 Bubbles ist das ein mittelgroßer Slice, eine Bubble über `I0016`. Die zusätzliche ist die **Probe** — sie steht hier und nicht bei `I0016`, weil dieser Slice als erster einen Zeitstempel mit Uhrzeit speichert und der Boden dafür im Repository unbelegt ist. Die zweite unklare Bubble ist wie überall die E2E-Bubble; derselbe Vermerk wie bei `I0005` bis `I0016`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`). Die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen — das verschöbe die Zählung des ganzen Baums. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- **Bekäme der Mensch statt der Sperre lieber die Identitätswahl als Popover an der Schreibzeile?** — **nicht entschieden**, bewusst nicht geraten. Gebaut wird zunächst die Sperre mit Hinweis auf die Kopfzeile (Muster „Leerzustand als Handlung"). Ein Popover wäre die zweite Stelle im Programm, an der die Identität gewählt wird, und die Kapselung der Kopfzeile stünde dann zur Debatte. Vor `B0264` zu bestätigen.
- **Bleibt die Schreibzeile nach dem Senden offen und behält den Fokus?** — **nicht entschieden.** Das Artboard zeigt einen Zustand, keinen Ablauf. Gebaut wird zunächst: Feld bleibt stehen und ist leer, Fokus bleibt darin — dieselbe Antwort wie bei der Teilaufgabenzeile in `R00018`. Vor `B0264` zu bestätigen.
- **Ist der gesperrte Sendeknopf im Zustand „nicht gewählt" ohne einen eigenen Browserkontext prüfbar?** — **offen.** `sessionStorage` ist je Tab eigen (`R00013`), ein frischer Kontext beginnt ohne Wahl; ob die bestehende Testumgebung einen zweiten Kontext hergibt, ist nicht geprüft. Betrifft `B0265`, nicht das Kriterium.
- ~~Woher kommt der Urheber?~~ — **entschieden: aus dem `Identitaetsspeicher`, gelesen beim Senden.** Die Kartenseite injiziert ihn selbst (`AddScoped`, `Program.cs:26` — in Blazor Server je Kreislauf dieselbe Instanz). Das ändert **keinen** grünen Bestand, während `CascadingValue`, ein Zustandsdienst oder ein `EventCallback` `Kopfzeile.razor` und damit `R00013` anfassten, ohne dass ein Kriterium es verlangt. Gelesen wird **beim Senden**, nicht beim Laden: nur so gilt eine Wahl, die während der offenen Kartenseite wechselt, sofort.
- ~~Bekommt die Anwendung eine stellbare Uhr?~~ — **entschieden: nein.** Begründung und die drei Ersatzwege unter „Bewusst out of scope".
- ~~Trägt die Kommentarzeile die Quelle (Oberfläche / API)?~~ — **entschieden: nein.** Begründung unter „Bewusst out of scope".

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft bei jedem Start des `KanbanC.WebApi` mit.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Zusage der Vision, die seit `R00013` **halb** eingelöst ist: die Anwendung weiß seither, wer am Browser sitzt, aber nichts, was geschrieben wird, trägt diesen Namen — „an jeder Karte ist ablesbar, wer oder was gehandelt hat" gilt an keiner einzigen Karte. Das Zielbild ist eine Karte, an der eine Äußerung mit Absender und Zeitpunkt steht, gleich ob ein Mensch sie getippt oder ein Agent sie abgesetzt hat. Die Kausalkette: **wenn** der Kommentar eine eigene Zeile mit Fremdschlüssel auf den Kontributor und einem gesetzten Zeitpunkt bekommt (X), **dann** hat der Urheber zum ersten Mal einen Ort, an dem er abgelegt und wieder gelesen wird (Y), **und dann** wird die Naht aus `R00013` von einer Zusage zu einer geprüften Verbindung, an der auch der Timer aus `I0023`/`I0024` andocken kann, und der zweite Halbsatz des Fertig-Kriteriums von `I0009` („bleibt an alten Karten sichtbar") wird zum ersten Mal ehrlich testbar (Z). Der Hebel liegt genau hier und nicht davor: ein Urheberparameter an den bestehenden schreibenden Routen wäre in `R00013` tote Flexibilität gewesen (C17) — niemand hätte ihn ausgewertet, und kein ehrlicher Test hätte ihn grün bekommen, weil er keine Zustandsänderung bewirkt. Und nicht danach: würde erst der Timer den Urheber einführen, entstünde er in einem Slice, dessen Fertig-Kriterium von Zeiten spricht und nicht von Urheberschaft — der Kommentar ist der einfachste Träger, an dem sich die Verbindung ganz zeigt, weil er außer Text, Urheber und Zeitpunkt nichts enthält.

## Missing-Docs

- **Dapper und `DateTimeOffset` gegen eine SQLite-TEXT-Spalte:** Im Repository nirgends belegt — der Bestand führt Zeitwerte ausschließlich als `DateOnly` (ISO-Datum ohne Uhrzeit), und dort hat eine Probe die bequeme Annahme bereits widerlegt (`SqliteEigenschaftenTests.cs:89-131`: Dapper 2.1.79 weist `DateOnly` als Parameterwert ab). Ob ein `DateTimeOffset` als Parameter angenommen und aus einer TEXT-Spalte in einen Record-Parameter materialisiert wird, ist unbelegt. `B0254` belegt es; das Ergebnis gehört danach in `Dokumentation/Bibliotheken/`, falls es sich online nicht belegen lässt.
- **Microsoft.Data.Sqlite und die lexikografische Ordnung von ISO-8601-Zeitstempeln:** Dass `ORDER BY` auf einer TEXT-Spalte mit UTC-Zeitstempeln im Format `O` die Zeitordnung liefert, ist eine Eigenschaft des Formats, nicht der Bibliothek — für Werte mit **verschiedenen** Zeitzonenversätzen gälte sie nicht. Deshalb UTC in der Spalte; der Beleg gehört zu `B0254`.
- **Blazor Server und die Zeitzone der Anzeige:** `Zeitpunktform` rechnet von UTC in die Ortszeit; in Blazor Server ist das die Zeitzone des **Servers**, nicht die des Browsers. Im LAN-Betrieb auf einer Maschine derselbe Wert; eine echte Browserzeitzone bräuchte einen Interop-Aufruf. Nirgends im Repository dokumentiert.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Urheber über `CascadingValue`, einen Zustandsdienst oder `EventCallback` aus der Kopfzeile** | Alle drei fassen `Kopfzeile.razor` an und damit grünen Bestand aus `R00013`, ohne dass ein Kriterium dieses Slices es verlangt. Die Kartenseite injiziert den `Identitaetsspeicher` selbst — `AddScoped` (`Program.cs:26`) liefert in Blazor Server je Kreislauf dieselbe Instanz. |
| **Die Wahl einmal beim Laden der Seite lesen** | Eine Identität, die in der Kopfzeile wechselt, während die Kartenseite offen steht, gälte dann nicht. Ein einmaliges Lesen bräuchte genau die Benachrichtigung, die beim Lesen zum Sendezeitpunkt entfällt. |
| **Urheber als Query-Parameter** | Der Rumpf ist der Ort, an dem dieses Projekt Kontributoren übergibt (`KarteAendernAnfrage.Kontributor`). Zwei Wege für denselben Wert wären Synonym-Wildwuchs an der Schnittstelle. |
| **`DateTime` statt `DateTimeOffset`** | Der Wert reist über HTTP zu Agenten; ein `DateTime` verliert unterwegs seine Zeitzone, und der Empfänger rät sie. |
| **Ortszeit in der Spalte** | Text sortiert nur bei einheitlichem Versatz lexikografisch wie chronologisch. Mit Ortszeit wäre `ORDER BY Zeitpunkt` eine stille Lüge, sobald die Uhr umgestellt oder der Server verschoben wird. |
| **Feldname `GeschriebenAm`** | `<Verb>Am` trägt im ganzen Stack bisher **nur reines Datum** (`ErledigtAm`, `FaelligAm`, `StillgelegtAm`, alle `DateOnly?`). Ein `…Am` mit Uhrzeit machte aus einer verlässlichen Namensregel eine, bei der man erst den Typ nachsehen muss. `Zeitpunkt` ist zudem das Wort des Fertig-Kriteriums. |
| **Eine stellbare Uhr (`IUhr`) einführen** | Ehrlich eingeführt träfe sie auch `KartenRepository.Heute()` (`:335-341`) und `KontributorenRepository` (`:96`) — zwei grüne Stellen, die kein Kriterium dieses Slices anfasst. Nur hier eingeführt, stünden zwei Uhren nebeneinander, und das ist schlechter als eine. Ersatz siehe „Bewusst out of scope". |
| **Eine `Position` neben dem Zeitpunkt** (Muster `012`) | Der umgekehrte Fall zu `I0016`: dort lieferte nichts eine Ordnung, hier liefert der Zeitpunkt sie. Eine Position daneben wäre eine zweite Wahrheit und liefe beim ersten Zurückdatieren auseinander. |
| **Karte und Text als Schlüssel** (Muster `011-kartenetikett.sql`) | Ein Kommentar ist eine Äußerung mit Identität, und zwei gleichlautende sind zwei Äußerungen. Deshalb eine eigene `KommentarId`, wie bei der Teilaufgabe. |
| **HTTP 201 mit der geschriebenen Zeile** | Die Antwort trägt die Seite, die der Aufrufer betrachtet, nicht die geschriebene Zeile. Ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite (`B0224`, `B0238`, `B0249`). |
| **Ein gespeicherter Kommentarzähler am `Kartendetail`** | Zweite Wahrheit neben der Liste, die ihn trägt. Die Anzahl ist `.Count` an der gelieferten Liste — hier ist nichts zu rechnen, also braucht es dafür nicht einmal eine eigene Operation. |
| **Ein zweites Feature „Zurückweisung ohne Urheber"** | Es ist ein Fehlerpfad derselben Route, im selben E2E-Lauf belegt. Ein Feature, das nur die Interaction wiederholt, ist ein Fehler. |
| **Einen anonymen Kommentar zulassen** | Das Fertig-Kriterium sagt „**mit** Kontributor", die Vision sagt, an jeder Karte sei ablesbar, wer gehandelt hat, und die Spalte trägt `NOT NULL`. Ein anonymer Kommentar löste keines der drei ein. |

### Bewusst out of scope

- **Die „Quelle" in der Metazeile (Oberfläche / API).** Das Artboard zeichnet sie an **jeder** Kommentarzeile (`D0004.dc.html:186`, `:195`) und nennt sie in der Lesehilfe (`:536`); das Fertig-Kriterium von `I0017` kennt sie nicht, und der Code hat keinen Träger dafür. Schwerer als der fehlende Rückhalt wiegt, dass sie **nicht ehrlich feststellbar** ist: die WebApi sieht in beiden Fällen denselben HTTP-Aufruf — die Oberfläche ist selbst nur ein Klient (`KartenApiKlient`) —, und ein Feld, das den Kanal rät, wäre genau die stille Lüge, gegen die `I0013` seinerzeit `Spalte.Kartenzahl` eingeführt hat. Ein selbstgemeldetes Feld, das jeder Aufrufer auf „Oberfläche" setzen könnte, wäre tote Flexibilität (C17). Was die Zeile ohne sie verliert, ist wenig: die Kontributorart trennt Mensch und Agent bereits, und sie steht im Kürzel. **Lücke mit Adresse:** die Quelle gehört zum **Verlauf** („wer, wann, über welche Grenze"), zu dem die WBS **keinen Knoten** hat — der Befund steht im Artboard selbst (`D0004.dc.html:539`). Sie kommt über eine **eigene Anforderung und einen eigenen Slice unter `D0004`**, dann mit einem Träger, den die API wirklich führt. Hier wird sie nicht gebaut und nicht grün getestet.
- **Eine stellbare Uhr.** Der Zeitpunkt kommt aus `DateTimeOffset.UtcNow` im Repository, dort, wo `DateTime.Today` für die Erledigung und die Stilllegung schon steht. Ehrlich getestet wird stattdessen auf **drei** Wegen, die alle schon im Repository stehen: das Repository über ein **Zeitfenster** (`t0 ≤ Zeitpunkt ≤ t1` — eine echte Zustandsänderung, kein zurückgelesener Testwert); die Darstellung über die **reine Operation** `Zeitpunktform.AlsText(zeitpunkt, jetzt)` mit „jetzt" als **Parameter** — das ist die stellbare Uhr an der einen Stelle, an der sie gebraucht wird, und macht alle Formen und ihre Ränder ohne Zeitmanipulation prüfbar; und der E2E-Fall „gestern 17:40" am Dienst vorbei über `Testumgebung.Aktuelle.Datenbank`, derselbe Weg wie `AeltereNachladenE2ETests.cs:202`. **Lücke mit Adresse:** braucht ein späterer Slice die Uhr wirklich stellbar (`I0023`–`I0026` Timer, `D0009` Auswertungen), ist das ein **Refactoring** über `KartenRepository` und `KontributorenRepository` — `/anforderung refactoring`, nicht nebenbei hier.
- **Ändern und Löschen eines Kommentars.** Im Artboard **nicht gezeichnet** (`D0004.dc.html:177-202` zeigt weder Stift noch `✕`) und im Fertig-Kriterium nicht gefordert — anders als bei `I0016`, wo Anlegen und Abhaken beide gezeichnet waren und deshalb beide eine Route bekamen. **Lücke mit Adresse:** sie brauchen zuerst eine Skizze über `/wireframe verfeinern D0004` und danach eine eigene Anforderung.
- **Ein Zwang zur Identitätswahl beim Betreten der Anwendung.** Den hat `R00013` ausdrücklich zurückgestellt, und er bleibt zurückgestellt. Die Sperre des Sendeknopfs ist **nicht** dieser Zwang: die ganze Anwendung bleibt ohne Wahl benutzbar, nur diese eine Handlung nicht — und das aus fachlichem Grund, weil ein Kommentar ohne Urheber keiner ist.
- **Der Urheberparameter an den übrigen schreibenden Routen** (Board anlegen, Karte verschieben, Spalte ändern …). `R00013` hat ihn als tote Flexibilität verworfen, und daran ändert dieser Slice nichts: er legt den Urheber dort ab, wo er auch wieder gelesen wird. Wo keine Spalte ihn aufnimmt, entsteht auch kein Parameter.
- **Live-Aktualisierung der Kommentare** (ein zweiter Betrachter sieht den neuen Kommentar ohne Reload). Gehört zu `D0007` und ist im Artboard nicht gezeichnet.

### Angenommen im stillen Lauf

- **Der Abschnitt heißt „Kommentare"** und steht hinter „Teilaufgaben" in der linken Spalte, wie im Artboard — nicht im Eigenschaftenblatt rechts.
- **Höchstlänge des Kommentartexts:** großzügiger als beim Etikett (100) und bei der Teilaufgabe (200) — der Kommentar ist Fließtext, und das Artboard zeichnet volle Zeilen über die Breite der linken Spalte. Der genaue Wert wird in `B0257` festgelegt und dort begründet.
- **Der Urheber reist als ganzer `Kontributor`** im DTO, nicht als Nummer — dieselbe Entscheidung wie bei `Kartendetail.Verantwortlicher`, damit Name, Kürzel und der Zusatz „stillgelegt" ohne einen zweiten Abruf und ohne ein zweites Feld entstehen.
- **Die Begriffe stehen fest (C06):** `Urheber` = wer gehandelt hat (DTO-Eigenschaft, Beschriftung, Kriterium) · `Verantwortlicher` = wer zuständig ist (die Karte) · `Kontributor` = Tabelle und Rolle, und damit der Name jedes Fremdschlüssels, der auf sie zeigt (Spalte `Kommentar.Kontributor`, Anfragefeld `KommentarSchreibenAnfrage.Kontributor`). Dieselbe Trennung, die `F0042` für den Verantwortlichen getroffen hat.
- **Das ISO-Datum in der ältesten Zeitform** folgt dem bestehenden `Terminformatierer` — kein zweites Datumsformat im Projekt. Die offene Formatfrage aus `B0192` wird damit nicht entschieden, nur nicht verletzt.
- **Die Stilllegungs-Schwester trägt denselben Code** `kontributor-stillgelegt` und damit denselben HTTP-Status 400; nur die Meldung ist eigen. Ein zweiter Code würde `Nichtgefunden.AlleCodes` und die Statusabbildung berühren, ohne dass sich die Lage unterscheidet.
