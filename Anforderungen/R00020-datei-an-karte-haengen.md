---
id: R00020
status: Neu
datum: 2026-09-06
---

# R00020: Datei an Karte hängen

## Beschreibung

Eine Karte trägt **Anhänge**: hochgeladene Dateien, die untereinander in der Reihenfolge ihres Entstehens stehen und je Zeile Dateiname und Größe zeigen. Von dort lässt sich jede wieder **herunterladen** und wieder **entfernen**. Angehängt wird über `POST /api/karten/{karteId}/anhaenge` (multipart); die Antwort ist HTTP 200 mit dem **ganzen `Kartendetail`** — dieselbe Antwortgestalt, die `R00017` für diese Seite festgelegt und `R00018`/`R00019` fortgeführt haben. Heruntergeladen wird über `GET /api/karten/{karteId}/anhaenge/{anhangId}`, die einzige Route dieses Slices, die keine JSON liefert, sondern die Bytes. In der Oberfläche steht der Abschnitt „Anhänge" auf `/karten/{karteId}` hinter „Kommentare", als linke Hälfte einer zweispaltigen Sektion, deren rechte `I0019` bekommt.

Zahlt ein auf: [Vision](R00000-vision.md) — „lokale Datenhaltung … die Daten sind unmittelbar zugänglich"; und „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat."

**Vom Menschen entschieden: die Bytes liegen im Dateisystem, nicht als BLOB in SQLite.** Neben der Datenbankdatei entsteht ein Ordner `kanbanc.db-Files`, darin je Karte ein Unterordner mit der `KarteId`, darin je Anhang eine Datei mit der `AnhangId` als Namen. Das ist keine Abwägung dieser Anforderung, sondern eine Vorgabe: die Vision verlangt lokale Datenhaltung, in der die Daten unmittelbar zugänglich sind — wer die Datenbankdatei kopiert, soll den Dateiordner daneben sehen und die Dateien mit den Werkzeugen des Betriebssystems anfassen können. Offen war allein die Feinschreibung des Ordnernamens; sie ist im stillen Lauf entschieden (siehe „Angenommen im stillen Lauf").

**Der Anhang ist die erste Stelle im Projekt, an der Bytes außerhalb der Datenbank entstehen.** Damit kommt eine zweite Wahrheit ins Spiel, die es bisher nicht gab: eine Zeile in der Tabelle und eine Datei auf der Platte, die zueinander passen müssen. Die Reihenfolge von Schreiben und Commit ist deshalb Gegenstand der Anforderung und nicht der Implementierung (siehe „Ablauf").

## Geschäftlicher Nutzen

Eine Karte kann seit `R00017` sagen, **was** zu tun ist, seit `R00018`, **woraus** die Arbeit besteht, und seit `R00019`, **wer etwas dazu gesagt hat**. Sie kann bis heute nichts **mitbringen**. Der WBS-Export, das Burndown-Bild, das PDF der Beschaffung liegen irgendwo daneben, und wer sie zusammenbringen will, tut das über einen Pfad in der Beschreibung, den nur jemand mit demselben Rechner öffnen kann.

Für den Menschen heißt der Anhang: die Unterlage liegt bei der Aufgabe und nicht in einem zweiten Ablagesystem. Für den KI-Agenten heißt er mehr — er ist der erste Weg, auf dem ein Agent ein **Arbeitsergebnis** übergibt, statt es nur zu beschreiben: er lädt die erzeugte Datei über dieselbe Route hoch, die auch die Oberfläche ruft, und ein Mensch lädt sie im Browser herunter. Und weil die Bytes im Dateisystem neben der Datenbank liegen, bleibt der Bestand das, was die Vision verspricht: eine lokale Sammlung, die auch ohne die Anwendung noch da ist.

Das **Entfernen** kommt aus demselben Grund mit: die Bytes liegen auf derselben Platte wie die Datenbank, und eine versehentlich angehängte 10-MB-Datei, die niemand je wieder loswird, wäre ein Preis, den die Anwendung dauerhaft zahlt.

## Funktionale Anforderungen

- `POST /api/karten/{karteId}/anhaenge` nimmt **eine** Datei als `multipart/form-data` entgegen und antwortet mit dem vollständigen `Kartendetail`.
- Der Aufruf trägt **im selben multipart-Rumpf** die Datei und die `KontributorId` des Urhebers; es gibt keinen Query-Parameter dafür.
- `GET /api/karten/{karteId}/anhaenge/{anhangId}` liefert die **Bytes** der Datei samt `Content-Disposition` mit dem Originalnamen — die einzige Antwort dieses Slices, die keine JSON ist.
- `DELETE /api/karten/{karteId}/anhaenge/{anhangId}` entfernt Zeile **und** Datei und antwortet mit dem vollständigen `Kartendetail`.
- Das `Kartendetail` trägt die Anhänge der Karte, sortiert nach **Zeitpunkt** (ältester oben); die Reihenfolge ist damit die Zeitordnung und wird nicht getrennt gespeichert.
- Jeder Anhang trägt eine eigene Nummer (`AnhangId`), den **Dateinamen**, die **Dateigröße** in Bytes, den **ganzen** Urheber (Nummer, Name, Art, Stilllegungsstand) und seinen `Zeitpunkt`.
- Der Zeitpunkt wird beim Anhängen von der Anwendung gesetzt, nicht vom Aufrufer mitgegeben.
- Die Bytes liegen **im Dateisystem** unter `<Datenbankdatei>-Files/<KarteId>/<AnhangId>`, nicht in der Datenbank; der Ordnerpfad wird aus der Verbindungszeichenfolge **gerechnet** und nirgends gespeichert.
- Die Datei auf der Platte heißt **exakt** `<AnhangId>`, ohne Endung; der Originalname lebt nur in der Spalte `Dateiname`.
- Ein **leerer Dateiname**, eine Datei der Größe **0** und eine Datei **über der Obergrenze** werden mit Befund zurückgewiesen; weder Zeile noch Datei entstehen.
- Die Obergrenze beträgt **10 MB je Datei** und gilt **auch dann**, wenn ein Agent an der Oberfläche vorbei direkt die WebApi ruft.
- Zwei Dateien **gleichen Namens** an derselben Karte sind **erlaubt** — zwei Dateien sind zwei Dateien.
- Eine **unbekannte Karte**, ein **unbekannter Anhang** und ein **unbekannter Kontributor** werden mit HTTP 404 samt Rumpf beantwortet, ein **stillgelegter** Kontributor mit HTTP 400 samt Rumpf.
- Eine `AnhangId`, die es gibt, aber nicht an **dieser** Karte, liefert 404 und entfernt nichts.
- Ein Anhang **ohne** Urheber ist nicht möglich: die Anfrage führt die `KontributorId` als Pflichtwert, die Spalte ist `NOT NULL`.
- Die Kartenseite zeigt den Abschnitt „Anhänge" mit je Zeile Büroklammer, Dateiname, Größe, einem Symbol zum Herunterladen und einem `×` zum Entfernen, dazu eine Ablegefläche „Datei hierher ziehen oder wählen".
- Hat die Karte keinen Anhang, steht dort die **Handlung statt einer Null** — die Leerzeile teilt sich mit `I0019`.
- Die Größe erscheint als lesbarer Text: „41 kB", „118 kB", „1,2 MB".
- Ist **keine Identität gewählt**, ist die Ablegefläche gesperrt und sagt, was zu tun ist; die übrige Anwendung bleibt unverändert benutzbar.
- Der Browser lädt die Bytes **direkt von der WebApi**, nicht über den Blazor-Prozess.
- Alle Anhänge sind nach einem Reload und nach einem Neustart unverändert da, Bytes eingeschlossen.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** `014-kartenanhang.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal. Deshalb eine eigene Tabelle statt `ALTER TABLE`, wie bei den Migrationen 004 bis 013.
- **Ablage:** Die Bytes liegen im Dateisystem, der Pfad wird gerechnet. **Keine Spalte für den Ablagepfad** — eine gespeicherte zweite Wahrheit liefe beim ersten Verschieben der Datenbankdatei auseinander. Der Ordner heißt nach dem **vollen** Dateinamen der Datenbank samt `.db`.
- **Sicherheit im Full-Trust-Modell:** Nutzereingabe berührt die Platte **nie**. Weil die Datei nach der `AnhangId` heißt, gibt es weder Pfadausbruch (`../`) noch reservierte Namen noch Längengrenzen. Der gemeldete Name wird zusätzlich auf seinen letzten Pfadbestandteil gekürzt, bevor er in die Spalte geht — ein Browser meldet je nach Plattform `C:\Temp\a.md` oder `ordner/a.md`, und die Zeile soll den Dateinamen zeigen, nicht den Weg eines fremden Rechners.
- **Obergrenze an drei Stellen:** `Anhangsgrenze` als **eine** Konstante, dazu `MultipartBodyLengthLimit` an der Route (WebApi), `HubOptions.MaximumReceiveMessageSize` (Blazor) und `OpenReadStream(maxAllowedSize)` (Komponente). Die WebApi begrenzt selbst, **weil ein Agent an der Oberfläche vorbei lädt und eine Grenze, die nur in der Oberfläche steht, keine ist.**
- **Zwei Voreinstellungen stehen heute im Weg** und sind im Repository nirgends gesetzt: `HubOptions.MaximumReceiveMessageSize` (32 KB je SignalR-Nachricht) und `IBrowserFile.OpenReadStream` (512 KB). Schon die im Artboard gezeichnete 41-kB-Datei scheitert an der ersten.
- **Der Strom bleibt ein Strom:** Bytes gehen im Strom auf die Platte und vom Blazor-Prozess in die WebApi, nicht als Byte-Array durch den Arbeitsspeicher — bei 10 MB je Aufruf wäre das vermeidbarer Druck.
- **Unbelegter Boden wird erst belegt:** Im ganzen Repository gibt es **keinen** Datei-Upload — `IFormFile`, `InputFile`, `IBrowserFile`, `HubOptions` und `multipart` kommen in keiner Quelldatei vor. Vor der ersten produktiven Nutzung steht deshalb ein Probe-Test nach Skill `dependency-probe`, **ohne eine Zeile Produktionscode** (Probe-Host im Test selbst gebaut, wie `SqliteEigenschaftenTests`). In `R00017`, `R00018` und `R00019` ist je eine Annahme derselben Klasse gefallen (`DateOnly` als Dapper-Parameter, `Int64` statt `bool`, Materialisierung eines Zeitpunkts).
- **Zeitform:** `DateTimeOffset` in den Contracts, **ISO-8601-Text in UTC** in der Spalte — wie `R00019` und aus denselben Gründen. Dass Dapper die TEXT-Spalte nicht direkt materialisiert, ist dreifach belegt (`SqliteEigenschaftenTests`, `Kommentarleser`); der Leser rechnet in C# um.
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der drei neuen Routen trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404. **Die Vertragsfälle aller drei Routen gehören in denselben Arbeitsgang wie die Routen** (`FehlervertragTests.cs:41-58`; Lehre aus `B0152`/`B0159`): der Test liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot — hier kommen drei auf einmal.
- **Antwortgestalt:** **200 mit dem ganzen `Kartendetail`** beim Anhängen und beim Entfernen, nicht 201 mit der geschriebenen Zeile — wie `R00018`/`R00019` und aus demselben Grund.
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier **keine** Projektreferenz auf `KanbanC.BL`. Der Verweis des Browsers direkt auf die WebApi berührt die Kernregel nicht — sie verbietet die Projektreferenz, nicht den Weg über HTTP; ein `<a href>` auf die WebApi ist im Gegenteil der Beweis, dass die API alles kann.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün, mit den benannten Änderungen (siehe Akzeptanzkriterien).

## Akzeptanzkriterien

### Der Anhang wird mit Urheber und Zeitpunkt festgehalten (API)

- [ ] `POST /api/karten/{karteId}/anhaenge` mit einer Datei und einer `KontributorId` im multipart-Rumpf antwortet mit HTTP 200 und einem `Kartendetail`, dessen Anhangliste diese Datei als **letzten** Eintrag trägt.
- [ ] Der Eintrag trägt eine eigene, von den anderen verschiedene `AnhangId`.
- [ ] Der Eintrag trägt den **Originalnamen** der Datei und ihre **Größe in Bytes**. Rechenbeispiel: eine Datei aus 41 000 Bytes ergibt `Dateigroesse` = `41000` — nicht „41 kB", die Umrechnung ist Darstellung.
- [ ] Die `Dateigroesse` stammt aus dem **tatsächlich geschriebenen** Strom, nicht aus einer vom Aufrufer gemeldeten Länge: wird eine falsche Länge mitgeschickt, steht trotzdem die wirkliche Größe in der Antwort.
- [ ] Der Eintrag trägt den **ganzen** Urheber: Nummer, Name, Art und Stilllegungsstand — nicht nur die Nummer.
- [ ] Der Eintrag trägt einen `Zeitpunkt` mit Uhrzeit. Rechenbeispiel: wird die Uhr **vor** dem Aufruf als `t0` und **nach** dem Aufruf als `t1` gemerkt, gilt `t0 ≤ Zeitpunkt ≤ t1`.
- [ ] Der Aufrufer kann den Zeitpunkt **nicht** mitgeben; ein mitgeschicktes Feld ändert nichts am gespeicherten Wert.
- [ ] `GET /api/karten/{karteId}` liefert danach dieselbe Liste in derselben Reihenfolge.
- [ ] Rechenbeispiel Reihenfolge: an eine Karte ohne Anhänge werden nacheinander `a.md`, `b.png`, `c.pdf` gehängt → die Liste lautet in jedem folgenden Abruf `a.md`, `b.png`, `c.pdf` (ältester oben).
- [ ] Werden zwei Anhänge **in der Datenbank** auf verschiedene Zeitpunkte gesetzt, steht der ältere oben — unabhängig von der Reihenfolge des Anhängens.
- [ ] Zwei Aufrufe mit einer Datei **desselben Namens** werden beide angenommen und erzeugen zwei Einträge mit verschiedenen Nummern und je eigenen Bytes.
- [ ] Die Anhänge hängen am `Kartendetail` und **nicht** an `Karte`: `GET /api/boards/{boardId}` liefert die Karten unverändert ohne Anhangliste.
- [ ] Es gibt **kein** gespeichertes Zählfeld und **keine** gespeicherte Position: die Antwort trägt weder eine Anzahl noch eine Ordnungszahl neben der Liste.
- [ ] Ein Anhang eines inzwischen **stillgelegten** Kontributors bleibt an der Karte sichtbar, mit Name und Stilllegungsstand.
- [ ] Ein Neustart der Anwendung lässt Namen, Größen, Urheber, Zeitpunkte, Reihenfolge **und die Bytes** unverändert.

### Herunterladen gibt genau das zurück, was hochgeladen wurde

- [ ] `GET /api/karten/{karteId}/anhaenge/{anhangId}` antwortet mit HTTP 200 und den **Bytes** der Datei.
- [ ] Die zurückgegebenen Bytes sind **byteweise identisch** mit den hochgeladenen. Rechenbeispiel: eine Datei aus 41 000 Bytes kommt mit 41 000 Bytes und gleichem Inhaltsvergleich zurück.
- [ ] Die Antwort trägt einen `Content-Disposition`-Kopf mit dem **Originalnamen** (`wbs-export.md`), nicht mit der `AnhangId`.
- [ ] Die Route liefert **keine** JSON und trägt **keine** Metadaten im Rumpf — die stehen im `Kartendetail`.
- [ ] Eine `anhangId`, die es gibt, aber an einer **anderen** Karte, liefert HTTP 404 mit Rumpf und **keine** Bytes.
- [ ] Fehlt die Zeile nicht, aber die **Datei auf der Platte**, scheitert der Abruf **sichtbar** mit Befund statt mit einem leeren Download.

### Entfernen nimmt die Zeile und die Bytes

- [ ] `DELETE /api/karten/{karteId}/anhaenge/{anhangId}` antwortet mit HTTP 200 und dem `Kartendetail` **ohne** diesen Eintrag.
- [ ] Nach dem Entfernen ist die **Datei auf der Platte weg** — geprüft am Ablageordner, nicht nur an der Antwort.
- [ ] Ein zweiter `DELETE` derselben Nummer liefert HTTP 404 mit Rumpf.
- [ ] Eine `anhangId` einer **anderen** Karte entfernt nichts und liefert HTTP 404 mit Rumpf; die andere Karte trägt ihren Anhang danach unverändert.
- [ ] Die übrigen Anhänge derselben Karte bleiben unverändert, Bytes eingeschlossen.

### Die Bytes liegen im Dateisystem neben der Datenbank

- [ ] Neben der Datenbankdatei entsteht ein Ordner, dessen Name aus der **Verbindungszeichenfolge** gerechnet ist. Rechenbeispiel: `Data Source=kanbanc.db` ergibt `kanbanc.db-Files`; die Datei des Anhangs `7` an Karte `14` liegt unter `kanbanc.db-Files/14/7`.
- [ ] Der Ordnername folgt der Datenbankdatei: benennt jemand sie in `projekt.db` um, heißt der Ordner `projekt.db-Files` — **ohne** einen zweiten Konfigurationsschlüssel.
- [ ] Weitere Schlüssel in der Zeichenfolge (`Mode=`, `Cache=`) ändern den gerechneten Pfad nicht; ein absoluter `Data Source` ergibt einen absoluten Ablageordner.
- [ ] Fehlt `Data Source` in der Zeichenfolge, scheitert die Rechnung **sichtbar** — es entsteht kein stiller Ordner im Arbeitsverzeichnis.
- [ ] Die Datei auf der Platte heißt **exakt** die `AnhangId`, ohne Endung. Rechenbeispiel: `wbs-export.md` als Anhang `7` liegt als Datei `7`, nicht als `7.md` und nicht als `wbs-export.md`.
- [ ] **In der Datenbank stehen keine Bytes:** die Tabelle `Anhang` hat keine BLOB-Spalte, und die Größe der Datenbankdatei wächst durch einen 5-MB-Anhang nicht um 5 MB.
- [ ] **Es gibt keine Spalte für den Ablagepfad** — er wird gerechnet.
- [ ] Bricht das Anhängen nach dem Schreiben der Bytes ab, bleibt höchstens eine **verwaiste Datei** zurück — **nie** eine Zeile, deren Datei fehlt.
- [ ] Der zweite Lauf des `Migrationslaeufer` auf einer bestehenden Datei lässt Schema und Daten unverändert.

### Zurückweisung, Obergrenze und Fehlerantworten für Agenten

- [ ] Ein **leerer Dateiname** wird mit HTTP 400 **und Rumpf** zurückgewiesen; weder Zeile noch Datei entstehen.
- [ ] Eine Datei der Größe **0** wird mit HTTP 400 und Rumpf zurückgewiesen.
- [ ] Ein gemeldeter Name mit Pfadanteil wird auf den letzten Bestandteil gekürzt: `C:\Temp\wbs-export.md` und `ordner/wbs-export.md` werden beide als `wbs-export.md` gespeichert.
- [ ] **Obergrenze 10 MB je Datei.** Rechenbeispiele: 41 kB und 5 MB kommen durch; **10 MB + 1 Byte** wird mit HTTP 400 und Rumpf zurückgewiesen, und der Befund nennt die Obergrenze **in Bytes**.
- [ ] Die Obergrenze gilt **auch am direkten API-Aufruf**, der die Oberfläche nicht benutzt — sie steht in der WebApi und nicht nur in der Blazor-Anwendung.
- [ ] Eine zurückgewiesene zu große Datei hinterlässt **keine halbe Datei** in der Ablage und keine Zeile mit voller `Dateigroesse`.
- [ ] Eine **unbekannte** `karteId` wird an allen drei Routen mit HTTP 404 und einem Befund beantwortet, der Code, die aufgerufene Kartennummer und einen ausführbaren nächsten Aufruf nennt. Der Befund nennt **kein** Board — die Routen kennen keins.
- [ ] Eine **unbekannte** `anhangId` wird mit HTTP 404 und einem Befund beantwortet, der **beide** Nummern nennt und als Kompensation `GET /api/karten/{karteId}` angibt.
- [ ] Eine **unbekannte** `KontributorId` wird mit HTTP 404 und einem Befund beantwortet, der die Nummer und den Weg zur Kontributorenliste nennt; an der Karte hat sich nichts geändert.
- [ ] Eine **stillgelegte** `KontributorId` wird mit HTTP **400** und Rumpf zurückgewiesen — es fehlt kein Ding, es wurde eine Regel verletzt.
- [ ] Die Meldung der Stilllegung passt zum Anhang: sie sagt **weder** „kann nicht verantwortlich sein" **noch** „kann keinen Kommentar mehr schreiben" — beides wäre hier eine Falschaussage.
- [ ] Keine der drei Routen liefert eine Fehlerantwort mit leerem Rumpf.
- [ ] Der Vertragstest über alle registrierten Routen bleibt grün: **alle drei** neuen Routen werden von ihm abgerufen und stehen nicht als ungeprüft übrig (`FehlervertragTests.cs:41-58`).

### Der Abschnitt auf der Kartenseite

- [ ] Auf `/karten/{karteId}` steht hinter „Kommentare" ein Abschnitt mit der Überschrift **„Anhänge"**, als **linke** Hälfte einer zweispaltigen Sektion; die rechte Hälfte bleibt in diesem Slice leer und gehört `I0019`.
- [ ] Jede Zeile zeigt eine Büroklammer, den Dateinamen, die Größe als Text, ein Symbol zum **Herunterladen** und ein `×` zum **Entfernen**.
- [ ] Die Größe erscheint lesbar. Rechenbeispiele: `41000` → „41 kB"; `118000` → „118 kB"; `1200000` → „1,2 MB"; `0` → „0 kB"; `1000` → „1 kB"; `10485760` (die Obergrenze) → „10,5 MB".
- [ ] **Urheber und Zeitpunkt stehen im `title` der Zeile** — die gezeichnete Form bleibt einzeilig, die Zusage der Vision wird trotzdem eingelöst.
- [ ] Die Ablegefläche trägt den Text „Datei hierher ziehen oder wählen"; nach dem Anhängen steht die neue Zeile als letzte in der Liste.
- [ ] Hat die Karte **keinen** Anhang, steht dort die Handlung statt einer Null; die Zeile teilt sich mit `I0019` und nennt beide.
- [ ] **Ist keine Identität gewählt, ist die Ablegefläche gesperrt** und weist auf die Identitätswahl in der Kopfzeile hin. Board, Kartenseite und alle übrigen Handlungen bleiben ohne Wahl unverändert benutzbar.
- [ ] Wird die Identität in der Kopfzeile gewechselt, **während** die Kartenseite offen ist, trägt der nächste Anhang den **neu** gewählten Urheber — ohne Reload.
- [ ] Ein Klick auf das Download-Symbol lädt die Datei **mit ihrem Originalnamen** herunter, nicht mit der `AnhangId` als Namen.
- [ ] Ein Klick auf `×` nimmt die Zeile sofort aus der Liste; nach einem Reload ist sie weiterhin weg.
- [ ] Eine zu große Datei bringt eine **lesbare Meldung** auf der Seite; die Liste bleibt unverändert.
- [ ] Eine 41-kB-Datei und eine 5-MB-Datei gehen **durch den Blazor-Kreislauf** — die 32-KB-Voreinstellung von SignalR steht dem nicht mehr im Weg.
- [ ] Nach einem Reload zeigt die Seite dieselben Anhänge mit denselben Namen und Größen.
- [ ] Der Browser holt die Bytes **direkt von der WebApi**: der `href` des Download-Symbols zeigt auf die WebApi-Adresse, nicht auf eine Blazor-Route, und `KartenApiKlient` hat **keine** Methode, die Bytes liest.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] **Benannte Änderung 1:** `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) wächst um `IReadOnlyList<Anhang> Anhaenge` — die **fünfte** Liste. Das sind **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:470`); beide werden angepasst, ihre Zusicherungen nicht.
- [ ] **Benannte Änderung 2:** `Stillgelegt` (`Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs`) — die bestehende Schwester `Urheber` wird zu **`Kommentarurheber`** umbenannt (eine Aufrufstelle in `KartenService`, plus die Testzeile) und bekommt **`Anhangurheber`** daneben. **Derselbe Code** `kontributor-stillgelegt` (400) für beide, damit `Nichtgefunden.MeldetEinFehlendesDing` und die Statusabbildung unangetastet bleiben; eigen ist nur die Meldung. **Änderung an grünem Bestand aus Begriffsgründen** (C06): mit zwei Urhebersorten wäre `Urheber` nicht mehr kontexteindeutig.
- [ ] **Benannte Änderung 3:** `Nichtgefunden` (`Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs`) bekommt `Anhang(karteId, anhangId)` neben `Teilaufgabe(karteId, teilaufgabeId)` — Grund mit **beiden** Nummern und Kompensationsaktion, auch bei 404; `AlleCodes` wächst um den neuen Code.
- [ ] **Benannte Änderung 4:** `Kartendetailvergleich` (`Source/KanbanC.WebApi.IntegrationTests/Infrastructure/Kartendetailvergleich.cs`) vergleicht **alle** Listen des `Kartendetail`, also auch `Teilaufgaben`, `Kommentare` und die neuen `Anhaenge`; sein Kommentarkopf nennt die richtige Zahl. **Das ist zugleich die Behebung eines Bestandsbefunds** — siehe „Bestandsbefund" unten.
- [ ] **Benannte Änderung 5:** `TestKartenRepository` (`Source/KanbanC.BL.Tests/TestHelpers/`) zieht mit den neuen Signaturen von `IKartenRepository` mit.
- [ ] **Benannte Änderung 6:** `Blazor/Program.cs` setzt `HubOptions.MaximumReceiveMessageSize`, `WebApi/Program.cs` bzw. die Route setzt `MultipartBodyLengthLimit`; beide beziehen sich auf **dieselbe** Konstante `Anhangsgrenze`, es gibt keine zweite Zahl.
- [ ] **Benannte Änderung 7:** `Blazor/appsettings.json` bekommt `WebApi:OeffentlicheBasisAdresse`, `Blazor/Program.cs` liest sie mit `WebApi:BasisAdresse` als Voreinstellung. Der bestehende Schlüssel `WebApi:BasisAdresse` bleibt in Bedeutung und Wirkung **unverändert**.
- [ ] **Benannte Änderung 8:** `KartendetailSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/`) wächst um die Locator des Abschnitts, `WebApiKlient` (`Infrastructure/WebApiKlient.cs`) um `HaengeAnhangAn`, und `KartendetailOeffnenE2ETests` um den Anhang-Leerstand in der Leerzustandszeile; die bestehenden Locator und Zusicherungen bleiben unverändert.
- [ ] `Kopfzeile.razor` bleibt **unverändert** — die Kartenseite injiziert den `Identitaetsspeicher` selbst; kein `CascadingValue`, kein Zustandsdienst, kein `EventCallback`. `R00013` wird nicht angefasst.
- [ ] `Karte.cs` und `Karte.razor` bleiben unverändert — **auf der Bahn ist kein Anhangzeichen**, und die Kartenzahl im Bahnenkopf zählt unverändert.
- [ ] `KartenRepository.Heute()` und `KontributorenRepository` bleiben unverändert — es wird **keine** Uhr-Abstraktion eingeführt.
- [ ] Alle E2E-Suiten aus `R00001`–`R00019` bleiben **ohne Änderung** grün, insbesondere die Kartendetail-Suiten von `R00017`–`R00019` und die Identitätssuite von `R00013`.
- [ ] `GET /api/boards/{boardId}` und alle bestehenden Kartenrouten bleiben in Adresse, Verb und Antwortgestalt unverändert.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/014-kartenanhang.sql` — neue, idempotente Migration; Tabelle `Anhang` mit `AnhangId` als Primärschlüssel, `Karte` und `Kontributor` als Fremdschlüssel (nach der Projektregel benannt wie die referenzierte Tabelle), `Dateiname`, `Dateigroesse INTEGER NOT NULL`, `Zeitpunkt TEXT NOT NULL`, dazu ein eigener Index auf `Karte`. **Keine `Position`, keine BLOB-Spalte, keine Pfadspalte.**
- **Contracts:** `Source/KanbanC.Contracts/Karten/Anhang.cs` (neu), `Anhanginhalt.cs` (neu), `AnhangAnlegenAnfrage.cs` (neu), `Kartendetail.cs` (wächst um die Anhangliste).
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/AnhangValidator.cs`, `Anhangname.cs`, `Anhangpfad.cs`, `Anhangsgrenze.cs` (alle neu); `Source/KanbanC.BL/Operations/Fehler/Stillgelegt.cs` (Umbenennung + Schwester), `Nichtgefunden.cs` (Schwester `Anhang`).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Anhangleser.cs` (neu), `Anhangablage.cs` (neu — die **einzige** Stelle im Projekt, die außerhalb der Datenbank schreibt), `Kartenleser.cs` (`LiesKartendetail` führt die Anhänge mit, `:93`), `KartenRepository.cs` (`HaengeAnhangAn`, `LiesAnhang`, `EntferneAnhang`), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `HaengeAnhangAn`, `LiesAnhang`, `EntferneAnhang`, mit dem Urheberbefund neben dem bestehenden für den Kommentar.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — **drei** neue Routen als Unterressourcen der boardlosen Kartenadresse, neben `/etiketten` (`:28`), `/teilaufgaben` (`:33`) und `/kommentare` (`:38`); `Source/KanbanC.WebApi/Program.cs` (Grenze).
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`HaengeAnhangAn`, `EntferneAnhang` über `AlsKartendetail`, `:75` — **kein** Byte-Rückweg), `Source/KanbanC.Blazor/Services/Anhangadresse.cs` (neu), `Dateigroesseform.cs` (neu), `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` (Abschnitt hinter „Kommentare", `Kartendetail.razor:208-281`), `Source/KanbanC.Blazor/Program.cs` und `appsettings.json` (Grenze, öffentliche Basisadresse).
- **Unberührt:** `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor`, `Source/KanbanC.Contracts/Karten/Karte.cs`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor` — **auf der Bahn und in der Kopfzeile ändert sich nichts**.
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Karten/AnhangValidatorTests.cs`, `Operations/Karten/AnhangpfadTests.cs`, `Integrations/Karten/KartenServiceTests.cs`, `TestHelpers/TestKartenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenApiKlientTests.cs`, `Services/DateigroesseformTests.cs`, `Services/AnhangadresseTests.cs`), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/DateiwegProbeTests.cs` — die Probe, `Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/Karten/AnhangablageTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Infrastructure/Kartendetailvergleich.cs`), `Source/KanbanC.PlaywrightTests/` (`PageObjects/KartendetailSeite.cs`, `Infrastructure/WebApiKlient.cs`, `Tests/KartendetailOeffnenE2ETests.cs`, neue Testklasse `DateiAnKarteHaengenE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0004.dc.html`](../Dokumentation/Wireframes/D0004.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gilt daraus der **Abschnitt mit dem Vermerk `I0018`** (`:204-215`), die **linke** Hälfte der zweispaltigen Sektion, deren rechte Hälfte `I0019` trägt (`:216-226`): Überschrift, Zeilen aus Büroklammer, Dateiname, Größe und Download-Symbol, darunter die gestrichelte Ablegefläche. Dazu der **gemeinsame Leerzustand** der frischen Karte (`:425`: „Keine Anhänge, keine Verweise · hinzufügen"). Die Lesehilfe (`:536`) ordnet den Abschnitt `I0018` zu. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Ein `×` am Zeilenende.** Das Artboard zeichnet an der Anhangzeile kein Entfernen. Gebaut wird es trotzdem, weil die Route gebaut wird (siehe „Ablauf" und „Verworfene Alternativen") und eine Route ohne Bedienelement in der Oberfläche unerreichbar wäre. Beim Kommentar galt die umgekehrte Entscheidung; die Begründung dort („eine Route, die niemand ruft, wäre tote Flexibilität") trägt hier **nicht**: ein Anhang belegt dauerhaft Platz auf derselben Platte wie die Datenbank.
2. **Urheber und Zeitpunkt im `title` der Zeile.** Das Artboard zeichnet an der Zeile nur Name, Größe und Download-Symbol. Beide reisen trotzdem mit, weil die Vision sie verlangt und der Zeitpunkt zugleich die einzige Ordnung der Liste ist. In der Oberfläche stehen sie im `title` und **nicht** als zweite Textzeile — so bleibt die gezeichnete Form unangetastet.
3. **Die Leerzeile entsteht nur zur Hälfte.** Die gezeichnete Zeile nennt Anhänge und Verweise gemeinsam (`:425`); `I0019` gibt es noch nicht. Hier entsteht die Anhang-Hälfte, die gemeinsame Fassung mit `I0019`.

Alles andere am Abschnitt folgt der Skizze.

### Ablauf

1. **Datei anhängen** (`POST /api/karten/{karteId}/anhaenge`, multipart)
   - 1.1 `AnhangValidator.Pruefe(karteId, anfrage)` — leerer Name, Größe 0, Größe über `Anhangsgrenze`; die Kompensation nennt die Route samt Kartennummer und die Obergrenze in Bytes. **Der Urheber wird hier nicht geprüft**: seine beiden Regeln brauchen den Kontributorenbestand
   - 1.2 Bei Befunden: HTTP 400 mit Rumpf, **kein** Schreibzugriff, **keine** Datei
   - 1.3 `KartenService` prüft den Urheber — dieselben zwei Regeln wie beim Kommentar, der Urheber ist **Pflicht** und nicht `long?`
     - 1.3.1 Kontributor unbekannt → `Nichtgefunden.Kontributor(kontributorId)` → HTTP 404
     - 1.3.2 Kontributor stillgelegt → `Stillgelegt.Anhangurheber(kontributorId)` → HTTP 400
   - 1.4 `KartenRepository.HaengeAnhangAn(karteId, anfrage, strom)` in **einer** Transaktion
     - 1.4.1 Existiert die Karte nicht: `null` → HTTP 404 mit `Nichtgefunden.Karte(karteId)`
     - 1.4.2 `INSERT` mit `DateTimeOffset.UtcNow` als ISO-8601-Text — **er liefert die `AnhangId`, die der Dateiname auf der Platte ist**
     - 1.4.3 `Anhangablage.Lege` schreibt die Bytes **im Strom**; die geschriebene Länge geht als `Dateigroesse` zurück in die Zeile
     - 1.4.4 `Kartenleser.LiesKartendetail` in derselben Transaktion, dann `Commit`
     - 1.4.5 **Die Reihenfolge ist Absicht:** Zeile, dann Bytes, dann Commit. Jeder Abbruch hinterlässt damit höchstens eine **verwaiste Datei** — nie eine Zeile ohne Bytes, die einen Anhang zeigte, den niemand laden kann
   - 1.5 HTTP 200 mit dem ganzen `Kartendetail`
2. **Herunterladen** (`GET /api/karten/{karteId}/anhaenge/{anhangId}`)
   - 2.1 `KartenRepository.LiesAnhang` — die `WHERE`-Bedingung nennt **beide** Nummern, damit eine fremde `AnhangId` an dieser Karte nichts liefert
   - 2.2 `Anhangablage.Oeffne` gibt einen Lesestrom; fehlt die Datei, ist das ein **sichtbarer Fehler**, kein leerer Download
   - 2.3 `Results.File(strom, contentType, fileDownloadName)` — der Originalname aus der Spalte geht in `Content-Disposition`
3. **Entfernen** (`DELETE /api/karten/{karteId}/anhaenge/{anhangId}`)
   - 3.1 `KartenRepository.EntferneAnhang` — dieselbe Bedingung über beide Nummern
   - 3.2 **Zuerst die Zeile, Commit, dann die Datei** — spiegelbildlich zu 1.4.5 und aus demselben Grund: eine verwaiste Datei ist unsichtbar und behebbar, eine Zeile ohne Bytes zerbricht beim Klick
   - 3.3 HTTP 200 mit dem ganzen `Kartendetail`
4. **Lesen am Kartendetail** (`GET /api/karten/{karteId}`)
   - 4.1 `Anhangleser.LiesAnhaengeDerKarte` — `ORDER BY Zeitpunkt, AnhangId`
   - 4.2 Die beiden JOINs auf `Kontributor` und `Kontributorstilllegung` wie im `Kommentarleser`, damit der ganze Urheber mitreist
   - 4.3 Die Liste hängt am `Kartendetail`, nicht an `Karte`; **kein** Archivfilter, wie das ganze Kartendetail
5. **Oberfläche**
   - 5.1 Die Kartenseite liest die Wahl aus dem `Identitaetsspeicher` **beim Senden**, nicht beim Laden — Muster `R00019`
   - 5.2 Ohne gewählte Identität: Ablegefläche gesperrt mit Hinweis; kein Aufruf
   - 5.3 `InputFile` → `IBrowserFile.OpenReadStream(Anhangsgrenze)` → `MultipartFormDataContent` mit `StreamContent` — der Strom fließt durch, ohne im Blazor-Prozess vollständig zu landen
   - 5.4 `KartenApiKlient.HaengeAnhangAn` / `EntferneAnhang` über `AlsKartendetail` (`:75`) — 400 und 404 laufen denselben Weg
   - 5.5 **Das Herunterladen läuft am Blazor-Prozess vorbei**: `Anhangadresse.Fuer(karteId, anhangId)` rechnet aus `WebApi:OeffentlicheBasisAdresse` eine absolute URL in den `href` des Symbols
   - 5.6 `Dateigroesseform.AlsText(bytes)` formt die Größe
   - 5.7 `Kartendetail.razor` ersetzt nach jeder Antwort das ganze `_detail` — kein zweiter Abruf

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — drei neue Routen als Unterressourcen der boardlosen Kartenadresse, neben der Kommentarroute (`:38`). Die Adressen tragen kein Board, weil die Seite keins kennt.
- **`Kartenleser.LiesKartendetail`** (`:93`) — der eine Ort, an dem das Detail entsteht; hier reihen sich die Anhänge als fünfte Liste ein.
- **`Migrationslaeufer`** — die vierzehnte Migration reiht sich ein; kein Journal, also idempotent.
- **`Kartendetail.razor`** (`:208-281`, hinter dem Kommentarabschnitt) — der Abschnitt kommt als linke Hälfte der neuen zweispaltigen Sektion.
- **`SqliteVerbindungsfabrik` / `Datenhaltung:Verbindungszeichenfolge`** (`WebApi/appsettings.json:10`) — die Quelle, aus der der Ablageordner gerechnet wird. Sie ist **relativ** zum Arbeitsverzeichnis des API-Prozesses; der Ablageordner bleibt es damit auch, und die Testumgebung bekommt ihn ohne Zutun neben ihre temporäre Datei.
- **`Identitaetsspeicher`** (`Blazor/Program.cs:26`) — die Naht aus `R00013`, dieselbe wie in `R00019`.

**Klassen-Entwurf:**

- `Anhang` (DTO, immutable) — eine an der Karte liegende Datei. **Der Urheber reist als ganzer `Kontributor`**, damit Name, Kürzel und der Zusatz „stillgelegt" ohne zweiten Abruf entstehen — dieselbe Entscheidung wie bei `Kommentar`.
  - `record Anhang(long AnhangId, string Dateiname, long Dateigroesse, Kontributor Urheber, DateTimeOffset Zeitpunkt)`
- `Anhanginhalt` (DTO, immutable) — die **einzige** Antwort dieses Slices, die keine JSON ist: Name für den Kopf, Strom für den Rumpf.
  - `record Anhanginhalt(string Dateiname, Stream Inhalt)`
- `AnhangAnlegenAnfrage` (DTO, immutable) — gemeldeter Dateiname, gemeldete Größe und der Urheber; **`Kontributor` ist Pflicht** (`long`, nicht `long?`). Kein Feld für den Zeitpunkt und keines für den Ablagepfad.
  - `record AnhangAnlegenAnfrage(string Dateiname, long Dateigroesse, long Kontributor)`
- `Kartendetail` (DTO, immutable) — wächst um `IReadOnlyList<Anhang> Anhaenge`. **Kein Zählfeld daneben** und **keine Position**.
- `Anhangsgrenze` (Operation/Konstante) — die **eine** Zahl, auf die sich Validator, Route, Hub und Komponente beziehen.
- `Anhangname` (Operation, pure Logik) — kürzt den gemeldeten Namen auf seinen letzten Pfadbestandteil und schneidet Randleerzeichen.
  - `static string Normalisiert(string dateiname)`
- `AnhangValidator` (Operation, pure Logik) — Muster `KommentarValidator`. **Kein Dublettenbefund**, **keine Urheberprüfung**.
  - `static Pruefbefunde Pruefe(long karteId, AnhangAnlegenAnfrage anfrage)`
- `Anhangpfad` (Operation, pure Logik, **kein Dateizugriff**) — rechnet den Ablageort aus der Verbindungszeichenfolge.
  - `static string FuerKarte(string verbindungszeichenfolge, long karteId)`
  - `static string FuerAnhang(string verbindungszeichenfolge, long karteId, long anhangId)`
- `Anhangablage` (Provider/Ressourcenzugriff, wirft) — die **einzige** Stelle im Projekt, die außerhalb der Datenbank schreibt. Der Strom geht im Strom auf die Platte.
  - `static long Lege(string pfad, Stream inhalt)` · `static Stream Oeffne(string pfad)` · `static void Entferne(string pfad)`
- `Anhangleser` (Provider/Ressourcenzugriff) — liest die Anhänge einer Karte in Zeitpunkt-Reihenfolge, mit dem ganzen Urheber, in der laufenden Transaktion.
  - `static IReadOnlyList<Anhang> LiesAnhaengeDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)`
- `KartenRepository` (Provider, Integration nach Hausregel) — drei Wege, alle mit dem ganzen Detail bzw. dem Inhalt als Rückgabe, `null` bei fehlender Karte oder fremdem Anhang.
  - `Kartendetail? HaengeAnhangAn(long karteId, AnhangAnlegenAnfrage anfrage, Stream inhalt)`
  - `Anhanginhalt? LiesAnhang(long karteId, long anhangId)`
  - `Kartendetail? EntferneAnhang(long karteId, long anhangId)`
- `KartenService` (Integration, prüft/fängt) — dieselbe Antwortgestalt wie `SchreibeKommentar`.
  - `Ergebnis<Kartendetail> HaengeAnhangAn(long karteId, AnhangAnlegenAnfrage anfrage, Stream inhalt)`
  - `Ergebnis<Anhanginhalt> LiesAnhang(long karteId, long anhangId)`
  - `Ergebnis<Kartendetail> EntferneAnhang(long karteId, long anhangId)`
- `Nichtgefunden` (Operation) — eine Schwester mehr, mit **beiden** Nummern im Grund.
  - `static Fehlerbefund Anhang(long karteId, long anhangId)`
- `Stillgelegt` (Operation) — `Urheber` heißt künftig `Kommentarurheber`; `Anhangurheber` kommt daneben. Gleicher Code, eigene Meldung.
  - `static Fehlerbefund Kommentarurheber(long kontributorId)` · `static Fehlerbefund Anhangurheber(long kontributorId)`
- `Dateigroesseform` (Operation in der Oberflächenschicht, pure Logik) — Muster `Zeitpunktform` und `Teilaufgabenfortschritt`.
  - `static string AlsText(long dateigroesse)`
- `Anhangadresse` (Operation in der Oberflächenschicht, pure Logik) — die absolute URL, die in den Browser geht.
  - `static string Fuer(string oeffentlicheBasisAdresse, long karteId, long anhangId)`
- `KartenApiKlient` (Integration) — zwei Aufrufe mehr, beide über `AlsKartendetail` (`:75`). **Kein Byte-Rückweg.**
  - `Task<ApiErgebnis<Kartendetail>> HaengeAnhangAn(long karteId, long kontributorId, string dateiname, Stream inhalt)`
  - `Task<ApiErgebnis<Kartendetail>> EntferneAnhang(long karteId, long anhangId)`

### Änderungen an bestehenden Klassen

- `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs`) — ein Feld mehr, die fünfte Liste. **Änderung an grünem Bestand, aber mit kleiner Breite:** genau **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:93`, `KartenServiceTests.cs:470`) gegen 16 an `Karte`. Genau deshalb hängt die Liste hier und nicht an `Karte`.
- `Kartenleser` (`:93`) — `LiesKartendetail` führt die Anhänge mit, wie schon Etiketten, Teilaufgaben und Kommentare. Kein Archivfilter.
- `KartenRepository` — drei Wege dazu, Muster `SchreibeKommentar` (`:285-300`): Existenzprüfung, Schreiben und Rückgabe des ganzen Details in **einer** Transaktion. Der Zeitpunkt kommt aus `DateTimeOffset.UtcNow` an derselben Stelle wie beim Kommentar — **keine Uhr-Abstraktion**.
- `IKartenRepository` — drei Signaturen dazu; `TestKartenRepository` zieht mit.
- `KartenService` — ein Prüfweg für den Anhangurheber neben dem für den Kommentarurheber; die zwei Regeln sind dieselben.
- `Nichtgefunden` (`:47-58` `Teilaufgabe` als Nachbar) — `Anhang(karteId, anhangId)`; `AlleCodes` wächst.
- `Stillgelegt` (`:29`) — `Urheber` → `Kommentarurheber`, plus `Anhangurheber`. **Eine** Aufrufstelle in `KartenService` plus die Testzeile.
- `KartenEndpunkte` (`:38` Kommentarroute als Muster) — zwei Routenkonstanten und drei Registrierungen, dazu `MultipartBodyLengthLimit` an der POST-Route. **Die Vertragsfälle jeder Route gehören in denselben Arbeitsgang wie die Routen**: `FehlervertragTests.cs:41-58` liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot — hier kommen drei Routen auf einmal.
- `WebApi/Program.cs` — die Grenze; `Blazor/Program.cs` — `HubOptions.MaximumReceiveMessageSize` und das Lesen von `WebApi:OeffentlicheBasisAdresse` mit `WebApi:BasisAdresse` als Voreinstellung.
- `Blazor/appsettings.json` — ein Schlüssel mehr; der bestehende bleibt unverändert.
- `Kartendetail.razor` (`:208-281`) — eine zweispaltige Sektion mehr hinter „Kommentare", samt `InputFile`, Ablegefläche und Zurückweisungsmeldung.
- `Kartendetailvergleich` (`Source/KanbanC.WebApi.IntegrationTests/Infrastructure/Kartendetailvergleich.cs`) — vergleicht künftig **alle** Listen, siehe „Bestandsbefund".
- `KartendetailSeite` — Locator für Abschnitt, Zeilen, Dateiname, Größe, Download-Symbol, `×`, Ablegefläche, Leerzustand und Meldung.
- `WebApiKlient` (`Source/KanbanC.PlaywrightTests/Infrastructure/WebApiKlient.cs:154`) — `HaengeAnhangAn` für den Aufbau der E2E-Lage.
- `KartendetailOeffnenE2ETests` — die Leerzustandszeile wächst um den Anhang-Leerstand.

### Bestandsbefund (gefunden beim Schreiben dieser Anforderung)

`Source/KanbanC.WebApi.IntegrationTests/Infrastructure/Kartendetailvergleich.cs` vergleicht heute acht Glieder des `Kartendetail`, darunter aber **nur zwei der vier Listen**: `Etiketten` und `Etikettvorschlaege`. **`Teilaufgaben` und `Kommentare` werden nicht verglichen** — ein Test, der über diesen Helfer zwei Kartendetails gleich nennt, prüft sie stillschweigend nicht. Der Kommentarkopf der Datei sagt zudem noch „Kartendetail traegt zwei Listen"; es sind vier, mit dem Anhang fünf. Der Helfer wurde in `R00018`/`R00019` beim Wachsen des DTO nicht mitgezogen.

Die Behebung steht als **benannte Änderung 4** in den Akzeptanzkriterien und gehört in diesen Slice, weil sie sonst ein drittes Mal übersehen würde und die neue Anhangliste dasselbe Schicksal träfe. Sie ist **keine** Erweiterung des Umfangs: der Helfer ist Testinfrastruktur, kein Produktionscode, und die Änderung macht bestehende Tests strenger, nicht anders.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Probe vor der ersten produktiven Nutzung** (Skill `dependency-probe`, in `DateiwegProbeTests` auf einem im Test selbst gebauten Probe-Host, **ohne eine Zeile Produktionscode**): (1) eine Minimal-API-Route nimmt `IFormFile` samt zusätzlichem Formularfeld ohne eigenen Binder entgegen; (2) `Results.File(Stream, contentType, fileDownloadName)` setzt `Content-Disposition` so, dass der Klient den Originalnamen zurückliest; (3) **Fault-Injection**: eine Datei über `MultipartBodyLengthLimit` scheitert mit einer Ausnahme, die sich in einen Befund übersetzen lässt — sie wird **nicht** still abgeschnitten, sonst läge eine halbe Datei mit voller `Dateigroesse` in der Ablage. Geprüft mit einem 12-kB- und einem 12-MB-Strom. Fällt (2), holt der Klient den Namen aus dem `Kartendetail` statt aus dem Kopf; das ändert den Endpunkt, nicht die Anforderung.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Anhangname.Normalisiert` — `C:\Temp\a.md` und `ordner/a.md` ergeben beide `a.md`; Randleerzeichen fallen weg; ein Name ohne Pfad bleibt unverändert.
- `AnhangValidator.Pruefe` — leerer Name, Größe 0, Größe genau an der Grenze (kein Befund), Größe Grenze + 1 (Befund, der die Obergrenze in Bytes nennt); **zwei gleiche Namen ergeben keinen Befund**; die Kompensation nennt `POST /api/karten/{karteId}/anhaenge` samt Nummer.
- `Anhangpfad.FuerKarte` / `FuerAnhang` — `Data Source=kanbanc.db` + 14 + 7 ergibt `kanbanc.db-Files/14/7`; weitere Schlüssel (`Mode=`, `Cache=`) stören nicht; ein absoluter `Data Source` ergibt einen absoluten Ordner; **fehlendes `Data Source` scheitert sichtbar** statt still im Arbeitsverzeichnis zu landen.
- `Dateigroesseform.AlsText` (in `KanbanC.Blazor.Tests`) — 0 Bytes, genau 1000 Bytes, 41 000, 118 000, 1 200 000 und die 10-MB-Obergrenze; kB ohne Nachkommastelle, MB mit einer.
- `Anhangadresse.Fuer` (in `KanbanC.Blazor.Tests`) — absolute URL aus Basisadresse und beiden Nummern; Basisadresse mit und ohne abschließenden Schrägstrich ergibt dieselbe Adresse.
- `KartenService.HaengeAnhangAn` / `LiesAnhang` / `EntferneAnhang` gegen `TestKartenRepository` — Erfolg reicht das Detail bzw. den Inhalt durch; unbekannte Karte, unbekannter Anhang, unbekannter Kontributor und stillgelegter Kontributor liefern Befunde mit nichtleerem Code, Meldung und Kompensation; nach einer Zurückweisung wurde **nicht geschrieben**; der Befund zum stillgelegten Anhangurheber trägt eine **andere Meldung** als der zum Kommentarurheber und als der zum Verantwortlichen.
- `KartenApiKlient.HaengeAnhangAn` / `EntferneAnhang` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und **Rumpf** des abgesetzten Aufrufs werden mitgeprüft, insbesondere dass die `KontributorId` als **Formularfeld im multipart-Rumpf** und nicht in der Query steht. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `Anhangablage` gegen einen temporären Ordner — legen, lesen, entfernen; der Lesestrom liefert dieselben Bytes; **Zeile vorhanden, Datei fehlt → sichtbarer Fehler**. `KartenRepository.HaengeAnhangAn` / `LiesAnhang` / `EntferneAnhang` und `Anhangleser` gegen eine `TemporaereDatenbank` — schreiben und wieder lesen; die Datei liegt **wirklich** unter `<Ordner>/<KarteId>/<AnhangId>` (am Dateisystem geprüft, nicht an der Antwort); der gespeicherte Zeitpunkt liegt im **Zeitfenster** `t0 ≤ Zeitpunkt ≤ t1`; die `Dateigroesse` entspricht der **geschriebenen** Länge, auch wenn die gemeldete abweicht; zwei nachträglich verschieden datierte Zeilen kommen in Zeitordnung zurück; der Urheber kommt vollständig zurück, auch wenn er stillgelegt ist; eine fremde `AnhangId` liefert `null` und entfernt nichts; nach `EntferneAnhang` ist die **Datei weg**; ein Rollback lässt **keine Zeile ohne Datei** zurück (der Aufräumpfad gehört in denselben Test). `Kartenleser.LiesKartendetail` liefert die Liste auch für eine **archivierte** Karte. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — alle drei Routen mit 200, 400 (leerer Name; Größe 0; über der Grenze; stillgelegter Kontributor) und 404 (unbekannte Karte; unbekannter Anhang; fremder Anhang; unbekannter Kontributor) samt Rumpf; die Download-Route liefert Bytes und `Content-Disposition` mit dem Originalnamen; **10 MB + 1 Byte gegen die WebApi direkt** ergibt 400 mit Befund; `GET /api/boards/{boardId}` trägt danach **keine** Anhangliste an den Karten; `FehlervertragTests` ruft alle drei Routen ab. `WebApiNeustartTests` — Namen, Größen, Urheber, Zeitpunkte, Reihenfolge und Bytes überstehen den Neustart.

**E2E:** Eine Karte auf `/karten/{karteId}` ohne Anhang zeigt die Leerzeile. Mit gewählter Identität eine 41-kB-Datei über `SetInputFilesAsync` anhängen → die Zeile erscheint mit Name und „41 kB", Reload zeigt sie unverändert (US-1). Herunterladen über `Page.RunAndWaitForDownloadAsync` liefert **denselben Namen und dieselben Bytes** (US-2) — **hier fällt die SignalR-Annahme, und nur hier**. Eine Datei über der Obergrenze wird sichtbar zurückgewiesen, die Liste bleibt unverändert (US-4). Entfernen nimmt die Zeile **und die Datei** — die Datei wird am Ablageordner der Testdatenbank geprüft (`Testumgebung.Aktuelle.Datenbank`-Muster, derselbe Weg am Dienst vorbei wie `AeltereNachladenE2ETests.cs:202`), sonst wäre „entfernt" nur die verschwundene Zeile (US-3). In einem **frischen Browserkontext** ohne gewählte Identität ist die Ablegefläche gesperrt (US-5). Dazu laufen die E2E-Suiten aus `R00001`–`R00019` weiter — **ohne Änderung**; das ist die Gegenprobe des Slice.

Repositories, die `Anhangablage` und alles mit Datenbank- oder Dateisystem-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün). Das ist der einzige Knoten, den die WBS-Spalte `Braucht` von `I0018` nennt; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00017`** (`I0015`, grün — die Kartenseite, die Adresse `/karten/{karteId}`, das `Kartendetail` als Antwortgestalt, `KartenApiKlient.AlsKartendetail`, `KartendetailSeite`), **`R00018`** (`I0016`, grün — die zweite Aufrufstelle von `new Kartendetail(`) und **`R00019`** (`I0017`, grün — der Abschnitt darüber, der Urheber aus dem `Identitaetsspeicher`, `Stillgelegt.Urheber`, die Zeitform als ISO-8601-UTC-Text). Die Spalte `Braucht` von `I0018` nennt weder `I0015` noch `I0008`; das ist in der WBS als offene Frage vermerkt und gehört in `/planung aendern I0018`, wenn die Herkunft dokumentiert bleiben soll. An Front und Welle ändert es nichts, weil beide grün sind.
- Setzt ferner auf: **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css`), **`R00011`**/**`R00014`** (`Kontributor`, `Kontributorstilllegung`, `Kontributorartform`), **`R00013`** (`Identitaetsspeicher`), **`R00016`** (`LiesKartendetail` ohne Archivfilter).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0018` in seiner Spalte `Braucht` (geprüft am 2026-09-06 über `Dokumentation/Planung/kanbanc.md`). Fachlich benachbart ist `I0019` (Verweise), das die rechte Hälfte derselben Sektion füllt und die gemeinsame Leerzeile vollendet; `I0038` (Board exportieren) ist die Adresse für den Preis des nicht sprechenden Ablageordners.

## Umfang

```
Datei an Karte haengen (I0018) = 16 Bubbles: 12 Standard (9,6h), 4 unklar (3,2–8,5h).
Rest: 9,6h klar + 3,2–8,5h unklar · 9 von 16 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 16 Bubbles gruen (0 %) · 0 laufen · 16 offen
```

`I0018` ist vollständig bis zur Bubble geplant und trägt seine sechzehn Bubbles (`B0266`–`B0281`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: die Interaction hat einen prüfbaren Aspekt, nicht mehrere. Anhängen, Herunterladen und Entfernen teilen Tabelle, Ablage, Komponente und E2E-Weg; als getrennte Slices geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0016` und `I0017`. Dass das Herunterladen als einzige Handlung keine JSON zurückgibt, macht es nicht zu einem zweiten Aspekt: es ist die Rückseite desselben Verhaltens und ohne das Anhängen nicht einmal auslösbar. **Die Requirement-Klammer sitzt deshalb allein an `I0018`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0266` Probe: Datei durch multipart in die WebApi und zurück | Probe (`dependency-probe`) | 0,4–1,5h (**unklar**) |
| `B0267` Anhangtabelle anlegen | Provider (Migration) | 0,4h (belegt über `B0255`) |
| `B0268` Ablageort aus der Verbindungszeichenfolge rechnen | Operation | 0,4h (belegt über `B0251`) |
| `B0269` Bytes ablegen, lesen und entfernen | Provider | 0,4h (belegt über `B0252`) |
| `B0270` Anhangs-Anfrage prüfen | Operation | 0,4h (belegt über `B0257`) |
| `B0271` Anhänge am Kartendetail lesen | Contracts + Provider | 0,4h (belegt über `B0256`) |
| `B0272` Anhang anlegen | Provider | 0,4–1,5h (**unklar**) |
| `B0273` Anhang lesen und entfernen | Provider | 0,4h (belegt über `B0247`) |
| `B0274` Anhänge verdrahten | Integration | 0,4h (belegt über `B0259`) |
| `B0275` Obergrenze setzen — WebApi und Blazor-Kreislauf | Rahmen-Einstellungen | 0,4–1,5h (**unklar**) |
| `B0276` Endpunkte der Anhänge | Integration | 2h (Richtwert) |
| `B0277` API-Klient der Anhänge | Integration | 2h (Richtwert) |
| `B0278` Herunterladen als Verweis in den Browser | Operation (Oberfläche) | 0,4h (belegt über `B0251`) |
| `B0279` Dateigröße als Text | Operation (Oberfläche) | 0,4h (belegt über `B0262`) |
| `B0280` Anhangabschnitt der Kartenseite | UI | 2h (Richtwert) |
| `B0281` E2E Datei an Karte hängen | E2E | 2–4h (**unklar**) |

Mit 16 Bubbles ist das der **größte Slice bisher**, vier über `I0017`. Die zusätzlichen tragen den unbelegten Boden: die Probe (`B0266`), die Ablage im Dateisystem als eigene Schicht (`B0268`, `B0269`), die Rahmengrenzen (`B0275`) und der Weg des Browsers direkt zur WebApi (`B0278`). Vier unklare Bubbles sind ebenfalls das Maximum bisher — drei davon liegen an derselben Ursache: im ganzen Repository gibt es keinen Datei-Upload, also ist jede Annahme über `IFormFile`, `HubOptions` und `OpenReadStream` unbelegt. Derselbe Vermerk wie bei `I0005` bis `I0017`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`). Die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen — das verschöbe die Zählung des ganzen Baums. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Abweichung zur Notiz in der WBS:** die dortige Zählzeile zu `I0018` sagt „16 Bubbles, 13 Standardmuster, 3 unklar — `B0266`, `B0272` und `B0281`". Gezählt über die Aufwandsspalte sind es **12 Standard und 4 unklar**: `B0275` trägt ebenfalls eine Spanne (`0,4-1,5`) und einen ausdrücklichen `Unklar:`-Vermerk in seiner Notiz. Die Tabelle oben folgt der Aufwandsspalte. Korrektur der Notiz über `/planung aendern I0018` — diese Familie fasst Notizen nicht an.

## Offene Fragen

- **Ist 10 MB die richtige Obergrenze?** — **nicht entschieden, im stillen Lauf angenommen.** Gezeichnet sind 41 kB und 118 kB; die Vision nennt Anforderungs-, Planungs- und Architekturdateien sowie Auswertungsbilder. 10 MB lässt jedes davon zu und hält Videos draußen. Entscheidend ist nicht die Zahl, sondern dass sie **auch in der WebApi** steht. Vor `B0275` zu bestätigen; eine andere Zahl ändert genau eine Konstante.
- **Soll vor dem Entfernen eine Rückfrage stehen?** — **nicht entschieden.** Gebaut wird zunächst ohne, wie beim Abhaken einer Teilaufgabe. Ein versehentliches `×` kostet hier allerdings mehr als ein versehentlicher Haken: die Datei ist weg. Vor `B0280` zu bestätigen.
- **Hätte der Mensch Urheber und Zeitpunkt lieber sichtbar statt im `title`?** — **nicht entschieden.** Gebaut wird der `title`, damit die gezeichnete einzeilige Form bleibt. Wäre eine Metazeile gewünscht, ist das eine Zeile in `B0280` und **keine** Änderung am Datenmodell — beide Werte reisen ohnehin mit.
- **Bleibt `WebApi:OeffentlicheBasisAdresse` im LAN richtig gesetzt?** — **offen und durch keinen Test gedeckt.** Der E2E-Lauf arbeitet auf `127.0.0.1`, wo öffentliche und interne Adresse zusammenfallen; erst ein zweiter Rechner im LAN zeigt den Unterschied. **Restrisiko, benannt:** wer die Anwendung zum ersten Mal von einem fremden Browser bedient, prüft den Download von Hand. Voreinstellung ist `WebApi:BasisAdresse`, damit der Einzelrechnerbetrieb ohne Zutun funktioniert.
- **Kann Playwright die Ablegefläche per Drag-and-drop bedienen, oder nur den verborgenen Dateiwähler?** — **offen.** Betrifft `B0281`: der gezeichnete Ziehweg bliebe dann ungeprüft, das Anhängen selbst ist über `SetInputFilesAsync` in jedem Fall belegt.
- **Stückelt der Rahmen den Byte-Strom unterhalb der SignalR-Nachrichtengrenze?** — **offen.** Wenn ja, genügt ein kleinerer Wert für `MaximumReceiveMessageSize` als 10 MB. Betrifft `B0275` und verschiebt genau eine Zahl, nicht den Slice.
- ~~Liegen die Bytes als BLOB in SQLite oder im Dateisystem?~~ — **entschieden vom Menschen: im Dateisystem**, in einem Ordner neben der Datenbankdatei, je Karte ein Unterordner mit der `KarteId`. Begründung: die Vision-Leitplanke „lokale Datenhaltung, die Daten sind unmittelbar zugänglich".
- ~~Wie genau heißt der Ablageordner?~~ — **entschieden: `<voller Dateiname der Datenbank>-Files`**, also `kanbanc.db-Files`. Im stillen Lauf entschieden, Begründung unter „Angenommen im stillen Lauf".
- ~~Kommt das Entfernen mit?~~ — **entschieden: ja, drei Routen.** Begründung unter „Verworfene Alternativen".
- ~~Bekommt `KartenApiKlient` einen Byte-Rückweg?~~ — **entschieden: nein.** Der Browser lädt direkt von der WebApi. Begründung unter „Verworfene Alternativen".

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft bei jedem Start des `KanbanC.WebApi` mit; den Ablageordner legt die Anwendung beim ersten Anhängen selbst an.

## Manuelle Nachbereitungstätigkeiten

- **Beim ersten Betrieb über das LAN:** `WebApi:OeffentlicheBasisAdresse` in `Source/KanbanC.Blazor/appsettings.json` auf die im Netz erreichbare Adresse der WebApi setzen (z. B. `http://kanban-rechner:5280/`) und einen Download von einem **zweiten** Rechner aus prüfen. Kein Test deckt das ab.
- **Verwaiste Dateien:** bricht ein Anhängen zwischen dem Schreiben der Bytes und dem Commit ab, bleibt eine Datei ohne Zeile in der Ablage liegen. Sie ist unsichtbar und schadet nicht; wer sie loswerden will, löscht sie im Ordner. Ein Aufräumlauf ist bewusst nicht Teil dieses Slice.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Leerstelle, die jede der drei vorigen Kartenanforderungen offengelassen hat: die Karte kann inzwischen beschreiben, gliedern und besprechen, aber sie kann **nichts mitbringen**. Wer die WBS-Datei oder das Burndown-Bild zur Aufgabe stellen will, hat dafür nur einen Pfad im Fließtext, den niemand auf einem anderen Rechner öffnen kann — und ein KI-Agent, der ein Ergebnis erzeugt hat, kann es überhaupt nicht übergeben, sondern nur beschreiben. Das Zielbild ist eine Karte, die ihre Unterlagen trägt, und ein Bestand, der lokal und unmittelbar zugänglich bleibt. Die Kausalkette: **wenn** der Anhang eine Zeile mit Fremdschlüsseln auf Karte und Kontributor bekommt und seine Bytes als gewöhnliche Datei neben die Datenbank gelegt werden (X), **dann** entsteht der erste Weg, auf dem ein Arbeitsergebnis über dieselbe Route zwischen Mensch und Agent wandert, und zugleich der erste Ort, an dem die Daten der Anwendung auch ohne die Anwendung noch anfassbar sind (Y), **und dann** ist die Vision-Zusage „lokale Datenhaltung, unmittelbar zugänglich" nicht mehr nur eine Eigenschaft der Datenbankdatei, sondern gilt für alles, was das Board trägt (Z). Der Hebel liegt genau hier und nicht davor: ein allgemeiner Dateidienst ohne einen Ort, an dem Dateien wirklich gebraucht werden, wäre tote Flexibilität (C17), und kein ehrlicher Test hätte ihn grün bekommen. Und nicht danach: würde erst der Board-Export (`I0038`) Dateien anfassen, entstünde die Ablage in einem Slice, dessen Fertig-Kriterium vom Herausschreiben spricht und nicht vom Anhängen — die Karte ist der einfachste Träger, an dem sich der Weg Datei → Zeile → Datei ganz zeigt.

## Missing-Docs

- **Minimal-APIs und `IFormFile` mit zusätzlichen Formularfeldern:** Im Repository nirgends belegt — es gibt **keinen** Datei-Upload. Ob eine Minimal-API-Route `IFormFile` samt einem zweiten Formularfeld ohne eigenen Binder entgegennimmt, ist unbelegt. `B0266` belegt es; das Ergebnis gehört danach in `Dokumentation/Bibliotheken/`, falls es sich online nicht belegen lässt.
- **`Results.File` und `Content-Disposition`:** Ob der gesetzte `fileDownloadName` beim Klienten als Dateiname ankommt (und wie mit Umlauten im Namen umgegangen wird), ist unbelegt. Teil von `B0266`.
- **`MultipartBodyLengthLimit` und das Verhalten an der Grenze:** Ob eine zu große Datei mit einer fangbaren Ausnahme scheitert oder still abgeschnitten wird, entscheidet, ob eine halbe Datei mit voller `Dateigroesse` in der Ablage landen kann. Das ist die Fault-Injection von `B0266` und die wichtigste der drei Annahmen.
- **Blazor Server, `HubOptions.MaximumReceiveMessageSize` und `IBrowserFile.OpenReadStream`:** Beide Voreinstellungen (32 KB, 512 KB) sind im Repository nirgends gesetzt und nirgends dokumentiert; ob der Rahmen den Strom unterhalb der Nachrichtengrenze stückelt, ist offen und entscheidet über die nötige Größe. Fällt erst im E2E (`B0281`).
- **Dateisystemzugriff neben SQLite bei laufenden Transaktionen:** Ob `last_insert_rowid` und das Schreiben der Datei in derselben offenen Transaktion zusammengehen, ohne bei einem Rollback eine Datei stehen zu lassen, ist unbelegt (`B0272`).

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Bytes als BLOB in SQLite** | **Vom Menschen ausgeschlossen.** Die Vision verlangt lokale Datenhaltung, in der die Daten unmittelbar zugänglich sind; ein BLOB ist nur mit der Anwendung oder einem SQL-Werkzeug erreichbar. Nebeneffekt: die Datenbankdatei wüchse mit jedem Anhang, und jedes Kopieren zöge alle Bytes mit. |
| **Ein eigener Konfigurationsschlüssel für den Ablageordner** | Zwei Wahrheiten über denselben Ort. Wer die Datenbank umbenennt oder verschiebt, müsste an zwei Stellen ziehen, und beim ersten Vergessen zeigt der Ordner auf die Dateien einer anderen Datenbank. Gerechnet aus der Verbindungszeichenfolge folgt er automatisch. |
| **Ordnername ohne `.db` (`kanbanc-Files`)** | Anhängen ist eine Rechnung ohne Regel, Abschneiden braucht eine („welche Endung?") — und `kanbanc.db` und ein späteres `kanbanc.sqlite` fielen auf denselben Ordner. Mit `.db` im Namen sortieren beide nebeneinander in jeder Dateiliste; genau das war der Zweck. |
| **Eine Spalte für den Ablagepfad** | Zweite Wahrheit neben der Rechnung. Sie liefe beim ersten Verschieben der Datenbankdatei auseinander, und der Pfad ist aus zwei Zahlen ohnehin herstellbar. |
| **Die Datei auf der Platte nach dem Originalnamen benennen** | Nutzereingabe berührte dann die Platte: Pfadausbruch (`../`), reservierte Namen, Längengrenzen und Namenskollisionen wären lauter eigene Regeln, und eine Bereinigungsregel, die sich später ändert, verliert den Zugriff auf alte Dateien. **Preis der gewählten Form, benannt:** der Ordner ist ohne die Datenbank nicht sprechend — wer ihn öffnet, sieht `14/7` statt `wbs-export.md`. Die Adresse dafür ist `I0038 Board exportieren`, wo die Anhänge mit ihren Originalnamen herausfallen. |
| **Einen Dublettenbefund bei gleichem Dateinamen** | Zwei Anhänge sind zwei Dateien — dasselbe Argument wie bei zwei gleichlautenden Teilaufgaben (`R00018`) und Kommentaren (`R00019`); auf der Platte kollidieren sie ohnehin nicht, weil sie nach ihrer Nummer heißen. |
| **Nur Anhängen und Herunterladen, kein Entfernen** (Muster `I0017`) | Beim Kommentar galt: „eine Route, die niemand ruft, wäre tote Flexibilität". Hier trägt das nicht — ein Anhang, den niemand entfernen kann, belegt dauerhaft Platz auf derselben Platte wie die Datenbank, und eine versehentlich angehängte 10-MB-Datei wäre unwiderruflich. Die Route wird von der Oberfläche gerufen, ist also keine Vorratsflexibilität. |
| **Erst die Bytes, dann die Zeile** | Eine Datei ohne Zeile ist unsichtbar und behebbar; eine Zeile ohne Bytes ist ein Anhang, der beim Klick zerbricht. Deshalb beim Anhängen Zeile → Bytes → Commit und beim Entfernen Zeile → Commit → Datei. |
| **Die gemeldete Dateigröße speichern** | Eine gemeldete Zahl wäre eine zweite Wahrheit über dieselbe Datei und ließe sich vom Aufrufer beliebig setzen. Gespeichert wird, was tatsächlich geschrieben wurde. |
| **Die Größe als Text speichern** („41 kB") | Die Umrechnung ist Darstellung und gehört in die Oberflächenschicht (`Dateigroesseform`), nicht in die Spalte — sonst wäre keine Grenze mehr rechenbar. |
| **Ein Byte-Rückweg im `KartenApiKlient`** | Jede Methode dort endet in `ReadFromJsonAsync`, und `ApiAntwortleser`/`Zurueckweisungsleser` lesen ausschließlich JSON; ein zweiter Rückweg brächte eine zweite Leseform und einen Fehlerpfad ohne Befund-Rumpf. Über den Blazor-Prozess flössen die Bytes zudem zweimal über das Netz und zusätzlich durch den SignalR-Kreislauf, und der Browser bekäme keinen echten Download (Name, Fortschritt, Abbruch). |
| **Die Obergrenze nur in der Oberfläche setzen** | Ein Agent lädt an der Oberfläche vorbei. Eine Grenze, die nur in der Oberfläche steht, ist keine — und das widerspräche der Leitplanke, dass die API alles kann, was die Oberfläche kann. |
| **Eine eigene JSON-Adresse für die Metadaten eines Anhangs** | Die stehen im `Kartendetail`. `GET …/anhaenge/{anhangId}` liefert deshalb direkt die Bytes; eine zweite Adresse wäre eine zweite Wahrheit über dieselbe Zeile. |
| **HTTP 201 mit der geschriebenen Zeile** | Die Antwort trägt die Seite, die der Aufrufer betrachtet, nicht die geschriebene Zeile. Ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite (`R00018`, `R00019`). |
| **Der Urheber als Query-Parameter** | Der Rumpf ist der Ort, an dem dieses Projekt Kontributoren übergibt; hier ist es das Formularfeld im selben multipart-Rumpf. Zwei Wege für denselben Wert wären Synonym-Wildwuchs. |
| **Eine `Position` neben dem Zeitpunkt** | Der Zeitpunkt liefert die Ordnung, wie beim Kommentar. Eine Position daneben wäre eine zweite Wahrheit und liefe beim ersten Zurückdatieren auseinander. |
| **Den Begriff „Verweis" für den Anhang** | `Verweis` ist im Code als Hyperlink-Begriff vergeben (`board-verweis`, `Boardverweis`, `TitelverweisDerKarte`) und gehört fachlich `I0019`. Zwei Bedeutungen desselben Worts im selben Schirm wären genau der Synonym-Wildwuchs, den C06 verbietet. |
| **`Stillgelegt.Urheber` unverändert lassen** | Mit zwei Urhebersorten wäre `Urheber` nicht mehr kontexteindeutig, und die bestehende Meldung („kann keinen Kommentar mehr schreiben") wäre am Anhang eine Falschaussage. Die Umbenennung nach `Kommentarurheber` kostet **eine** Aufrufstelle plus eine Testzeile. |
| **Ein zweites Feature „Herunterladen"** | Es ist die Rückseite desselben Verhaltens und ohne das Anhängen nicht auslösbar; im selben E2E-Lauf belegt. Ein Feature, das nur die Interaction wiederholt, ist ein Fehler. |

### Bewusst out of scope

- **Vorschau eines Anhangs** (Bild im Browser anzeigen, Text im Blatt rendern). Im Artboard nicht gezeichnet, im Fertig-Kriterium nicht gefordert. Die Zeile zeigt Name und Größe; geöffnet wird die Datei vom Betriebssystem nach dem Download. **Lücke mit Adresse:** braucht die Karte eine Vorschau, ist das ein eigener Slice unter `D0004` samt Skizze über `/wireframe verfeinern D0004`.
- **Umbenennen eines Anhangs.** Im Artboard nicht gezeichnet. Der Name steht in der Spalte und wäre änderbar, aber niemand ruft es; das wäre tote Flexibilität (C17).
- **Ein Aufräumlauf für verwaiste Dateien.** Der gewählte Ablauf lässt bei einem Abbruch höchstens eine Datei ohne Zeile zurück — unsichtbar und ohne Wirkung auf die Anwendung. Ein Lauf, der die Ablage gegen die Tabelle abgleicht, gehört zu `I0031 Import wiederholen`/`I0038` und nicht hierher; das Muster liegt bereit (`.claude/app-architectures/Common/snippets/SollIstVergleich.md`).
- **Anhänge im Board-Export.** `I0038` schreibt sie mit ihren Originalnamen heraus; das ist zugleich die Adresse für den Preis des nicht sprechenden Ablageordners.
- **Mehrere Dateien in einem Aufruf.** Die Route nimmt **eine** Datei. Das Artboard zeichnet eine Ablegefläche, kein Mehrfachfeld; und eine Teilfehlschlag-Semantik („drei von fünf angekommen") wäre eine eigene Antwortgestalt.
- **Live-Aktualisierung der Anhänge** (ein zweiter Betrachter sieht den neuen Anhang ohne Reload). Gehört zu `D0007` und ist im Artboard nicht gezeichnet.
- **Zugriffsschutz auf die Download-Route.** Die Anwendung läuft im **Full-Trust-Modell ohne Authentifizierung** im LAN — das ist eine Leitplanke der Vision, kein Versäumnis. Wer die Adresse kennt, lädt die Datei; das gilt für jede andere Route dieses Projekts genauso.

### Angenommen im stillen Lauf

- **Der Ordner heißt `kanbanc.db-Files`** — nach dem **vollen** Dateinamen der Datenbank samt `.db`, nicht nach dem Stamm. Begründung unter „Verworfene Alternativen".
- **Der Abschnitt heißt „Anhänge"** und steht hinter „Kommentare" in der linken Spalte, als linke Hälfte der zweispaltigen Sektion — wie im Artboard.
- **Obergrenze 10 MB je Datei**, an drei Stellen gesetzt, mit einer Konstante als Quelle.
- **Der Urheber reist als ganzer `Kontributor`** im DTO, nicht als Nummer — dieselbe Entscheidung wie bei `Kommentar` und `Kartendetail.Verantwortlicher`.
- **Die Begriffe stehen fest (C06):** `Anhang` = die Zeile und der ganze Gegenstand · `Dateiname` = der Originalname in der Spalte · `Dateigroesse` = die Zahl in Bytes · `Anhangablage` = der Ort der Bytes, `Anhangpfad` rechnet ihn · `Herunterladen` = die Handlung am Symbol · `Urheber` = wer angehängt hat · `Zeitpunkt` = wann. **Nicht** `Verweis` (gehört `I0019`), **nicht** `Datei` allein (zu unbestimmt), **nicht** `AngehaengtAm` (die in `R00019` gesetzte Regel: `…Am` trägt im Stack nur reines Datum).
- **Die Fremdschlüsselspalte heißt `Kontributor`** nach der referenzierten Tabelle, `Karte` ebenso — Projektregel.
- **Der `Content-Type` beim Download** wird aus dem Dateinamen abgeleitet oder ist `application/octet-stream`; entscheidend für das Kriterium ist allein, dass der Originalname im `Content-Disposition` steht.
