---
id: R00012
status: Neu
datum: 2026-09-04
---

# R00012: Kontributor bearbeiten

## Beschreibung

Ein bereits angelegter Kontributor bekommt einen neuen Namen und eine neue Art — über `PUT /api/kontributoren/{kontributorId}` und über das Stiftsymbol seiner Zeile auf `/kontributoren`. Die Zeile klappt an Ort und Stelle zu einer Bearbeitungszeile auf, die den heutigen Stand vorbelegt zeigt; „sichern" übernimmt ihn, „verwerfen" lässt ihn stehen. Eine Änderung ohne Namen wird mit 400 zurückgewiesen, eine unbekannte `KontributorId` mit 404 — beide mit einem Befund aus Grund, Werten und Kompensationsaktion, nicht mit leerem Rumpf.

Zahlt ein auf: [Vision](R00000-vision.md) — „Was ein Mensch klicken kann, kann ein Agent aufrufen"; und „Kontributoren werden in der Oberfläche angelegt — abgebildete Personen genauso wie alle anderen."

## Geschäftlicher Nutzen

Mit `R00011` entsteht ein Kontributor, aber er ist danach unveränderlich: ein Tippfehler im Namen bleibt für immer stehen, und wer einen Agenten versehentlich als Mensch eingetragen hat, kann das nicht mehr richtigstellen. Weil die WBS kein Löschen kennt — stillgelegt wird in `I0009`, gelöscht wird nicht — ist Bearbeiten die **einzige** Korrekturmöglichkeit, die es überhaupt geben wird. Der Name eines Kontributors erscheint später an Karten (`I0015`), Kommentaren (`I0017`) und in jeder Zeitauswertung (`D0007`); je länger er falsch steht, desto mehr Auswertungen tragen ihn falsch. Und die Art entscheidet ab `I0008` darüber, wer als Identität wählbar ist — sie muss korrigierbar sein, bevor sie eine Folge bekommt.

## Funktionale Anforderungen

- Name und Art eines Kontributors lassen sich ändern; alle drei Arten sind in beide Richtungen wählbar.
- Geändert wird über die API und über die Oberfläche; beide Wege führen zum selben Stand.
- Name und Art werden in **einem** Vorgang gesichert, nicht in zwei getrennten Aufrufen.
- In der Oberfläche wird in der Zeile bearbeitet, nicht auf einem zweiten Schirm; die Zeile zeigt den heutigen Stand vorbelegt.
- „verwerfen" schließt die Bearbeitung, ohne etwas zu ändern.
- Solange eine Zeile aufgeklappt ist, bleiben alle übrigen Kontributoren sichtbar.
- Der geänderte Stand überlebt einen Reload und einen Neustart der WebApi.
- Eine Änderung ohne Namen wird zurückgewiesen; eine unbekannte `KontributorId` wird als fehlendes Ding beantwortet, nicht als Regelverstoß.

## Nicht-funktionale Anforderungen

- **Fehlervertrag:** Jede Fehlerantwort trägt einen Rumpf mit `Code`, `Meldung` und `Kompensation` (`R00007`, geprüft von `FehlervertragTests`). Das gilt für die neue Route ab ihrem ersten Tag — für 400 **und** für 404.
- **Eine Quelle für „das Ding gibt es nicht":** Der 404-Befund entsteht in `Nichtgefunden`, nicht als handgeschriebener Rumpf im Endpunkt (`Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs`, angelegt in `B0098` genau zu diesem Zweck).
- **Kernregel des Projekts:** `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL`; die Kontributoren-Seite spricht ausschließlich über HTTP mit der WebApi (`CLAUDE.md`, „Die eine Regel, die den Aufbau trägt").
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in `Kontributoren.razor.css`, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"; geprüft von `GestaltungsfundamentTests`).
- **Keine Migration:** Die Tabelle `Kontributor` steht seit `006-kontributoren.sql`; diese Anforderung schreibt kein Schema. Sie berührt die Persistenz nur mit einem `UPDATE`.
- **Benennung:** Primärschlüssel `KontributorId`, Domänensprache deutsch und kontexteindeutig (`Kontributor`, `Kontributorart`), Bezeichner ohne echte Umlaute, UI-Texte und Meldungen mit (C06, C07).

## Akzeptanzkriterien

### Ändern über die API

- [ ] `PUT /api/kontributoren/{kontributorId}` mit `{ "name": "Codex-Agent", "art": "Agent" }` antwortet mit HTTP 200 und dem geänderten Kontributor — dieselbe `kontributorId`, der neue Name, die neue Art.
- [ ] Name und Art ändern sich in **einem** Aufruf; es gibt keine Unterressource `/name` und keine `/art`.
- [ ] Alle drei Arten sind Ziel und Ausgangspunkt: Mensch → Agent, Agent → abgebildet und abgebildet → Mensch werden gleichermaßen übernommen.
- [ ] Nur der genannte Kontributor ändert sich. Rechenbeispiel: `Anna` (Mensch), `Bert` (Agent), `Cara` (abgebildet) angelegt; `Bert` wird zu `Zora` (Mensch) geändert → `GET /api/kontributoren` liefert `Anna`, `Cara`, `Zora` — drei Einträge, `Anna` und `Cara` unverändert, `Zora` an der neuen alphabetischen Stelle.
- [ ] Nach einem Neustart der WebApi auf derselben Datei liefert `GET /api/kontributoren` den geänderten Stand, nicht den alten.
- [ ] Der `Location`-Kopf von `POST /api/kontributoren` zeigt danach auf `/api/kontributoren/{kontributorId}` statt auf die Wurzelressource; `KontributorenEndpunkteTests.cs:25` zieht mit. Damit ist die Hälfte eines `R00011`-Kriteriums abgelöst — bewusst, siehe „Änderungen an bestehenden Klassen".

### Bearbeiten in der Zeile

- [ ] Die Liste auf `/kontributoren` trägt eine Kopfzelle „Pflege" und je Zeile ein Stiftsymbol; die Spalten „offen", „Zeit" und „letzte Handlung" der gezeichneten Zielform entstehen hier **nicht**.
- [ ] Ein Klick auf den Stift öffnet **genau eine** Bearbeitungszeile; ein Klick auf einen zweiten Stift schließt die erste.
- [ ] Die Bearbeitungszeile zeigt Name und Art des Kontributors vorbelegt — nicht leer und nicht auf „Mensch" zurückgesetzt.
- [ ] „sichern" übernimmt den Stand; die Liste zeigt ihn danach an seiner alphabetischen Stelle, ohne dass die Seite neu geladen wird, und nach einem Reload weiterhin.
- [ ] „verwerfen" schließt die Zeile; der Kontributor steht unverändert da, und `GET /api/kontributoren` bestätigt das.
- [ ] Solange eine Zeile aufgeklappt ist, sind **alle** übrigen Kontributoren mit Name und Art weiterhin in der Liste zu sehen — die aufgeklappte Zeile verdrängt keine andere.
- [ ] Was über die API geändert wurde, steht in der danach geöffneten Liste der Oberfläche — und umgekehrt liefert `GET /api/kontributoren` den in der Oberfläche geänderten Stand.
- [ ] Ist die WebApi beim Sichern nicht erreichbar, erscheint die Ausfallmeldung statt einer Ausnahmeseite; die Seite bleibt bedienbar.
- [ ] Die drei bestehenden Tests aus `KontributorenlisteE2ETests` sind nach der Änderung unverändert grün; kein Test wird gelöscht oder abgeschwächt, um das zu erreichen.

### Zurückweisung einer ungültigen Änderung

- [ ] `PUT /api/kontributoren/{kontributorId}` mit leerem oder nur aus Leerzeichen bestehendem `name` antwortet mit HTTP 400 — nicht 500 — und einem Befund mit nichtleerem `Code`, einer `Meldung`, die den Grund nennt, und einer `Kompensation`, die **diese** Route (`PUT /api/kontributoren/{kontributorId}`) mit einem nichtleeren `name` als nächsten Schritt nennt, nicht die Anlegeroute.
- [ ] Nach einer solchen Zurückweisung ist nichts geschrieben: `GET /api/kontributoren` liefert den Kontributor unverändert.
- [ ] Der Befund des leeren Namens beim **Anlegen** nennt weiterhin `POST /api/kontributoren`; `KontributorenValidatorTests.cs:59-66` bleibt in der Sache gültig und prüft danach beide Routen an ihrem jeweiligen Fall.
- [ ] In der Oberfläche erscheint bei leerem Namen der Satz **„Ohne Namen bleibt der Kontributor, wie er war."**; die Bearbeitungszeile bleibt offen und bedienbar, der eingestellte Artwert bleibt stehen, und der Kontributor bleibt unverändert.
- [ ] Der Satz der Anlegezeile — „Ohne Namen entsteht kein Kontributor." — bleibt unverändert; die beiden Zeilen sagen nicht dasselbe.

### Zurückweisung einer unbekannten KontributorId

- [ ] `PUT /api/kontributoren/999` auf eine `KontributorId`, die es nicht gibt, antwortet mit HTTP 404 — nicht 400 und nicht mit leerem Rumpf — und einem Befund, dessen `Meldung` die angefragte Nummer nennt und dessen `Kompensation` `GET /api/kontributoren` abzurufen und den Aufruf mit einer gelieferten `KontributorId` zu wiederholen verlangt.
- [ ] Der Befund entsteht in `Nichtgefunden`; `Nichtgefunden.MeldetEinFehlendesDing` kennt seinen Code, so dass `Zurueckweisungen.AlsFehlerantwort` ihn ohne Sonderweg im Endpunkt auf 404 statt 400 abbildet.
- [ ] Ein Aufruf mit unbekannter `KontributorId` **und** leerem Namen liefert eine Antwort mit Befunden, nicht einen Serverfehler; welcher der beiden Statuscodes gilt, ist im Ablauf festgelegt (Prüfung vor Datenzugriff → 400).
- [ ] `FehlervertragTests` nimmt beide Fehlerantworten von `PUT /api/kontributoren/{kontributorId}` in die Prüfung auf (leerer Name, unbekannte `KontributorId`); die Prüfung „keine Route ungeprüft" (`FehlervertragTests.cs:53-56`) ist danach grün.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Kontributoren/KontributorAendernAnfrage.cs` (neu) neben den drei bestehenden Dateien des Ordners.
- **Prüfung:** `Source/KanbanC.BL/Operations/Kontributoren/KontributorenValidator.cs` (geändert — zweite Route), `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` (geändert — `Kontributor`).
- **Datenzugriff:** `Source/KanbanC.BL/Interfaces/Kontributoren/IKontributorenRepository.cs` und `Source/KanbanC.BL/Persistenz/Kontributoren/KontributorenRepository.cs` (beide geändert — `Aendere`).
- **Fachlogik:** `Source/KanbanC.BL/Integrations/Kontributoren/KontributorenService.cs` (geändert — dritte Lage „unbekannter Kontributor").
- **API:** `Source/KanbanC.WebApi/Endpunkte/KontributorenEndpunkte.cs` (geändert — `MapPut`, `Location`-Kopf, Kommentar).
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KontributorenApiKlient.cs` (geändert — `Aendere`), `Source/KanbanC.Blazor/Services/Kontributorenmeldung.cs` (geändert — Wortlaut je Zeile), `Source/KanbanC.Blazor/Components/Pages/Kontributoren.razor` (+ `.razor.css`).
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Kontributoren/KontributorenValidatorTests.cs`, `Source/KanbanC.BL.Tests/Operations/Fehler/` (Nichtgefunden), `Source/KanbanC.BL.Tests/Integrations/Kontributoren/`, `Source/KanbanC.BL.Tests/TestHelpers/TestKontributorenRepository.cs`; `Source/KanbanC.Blazor.Tests/Services/KontributorenApiKlientTests.cs` und `KontributorenmeldungTests`; `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Kontributoren/KontributorenRepositoryTests.cs`, `Api/KontributorenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`; `Source/KanbanC.PlaywrightTests/PageObjects/KontributorenSeite.cs` (geändert), `Tests/KontributorAendernE2ETests.cs` (neu).
- **Unberührt:** `Source/KanbanC.BL/Persistenz/Migrationen/` — kein Schema wächst; `wwwroot/gestaltung.css` und `oberflaeche.css` — die Pflege-Spalte und die Bearbeitungszeile bringen ihre Gestaltung in `Kontributoren.razor.css` mit, mit Werten aus dem Token-Sheet, und nutzen die vorhandene `.meldung-abweisung` (`oberflaeche.css:44`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0002.dc.html`](../Dokumentation/Wireframes/D0002.dc.html) ist die Gestaltungsvorgabe; einschlägig sind die **Spalte „Pflege" mit dem Stiftsymbol je Zeile** (Zeilen 137, 156, 195, 214) und die **aufgeklappte Bearbeitungszeile an „Codex-Agent"** (Zeilen 162–180) mit Namensfeld, Dreier-Segmentwahl und den Schaltern „sichern" und „verwerfen". Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Was es an Umfang klärt: von der gezeichneten Zielform gehört `I0007` **nur die Spalte „Pflege"** — „offen", „Zeit" und „letzte Handlung" gehören `I0026` (`D0006`) und `D0007`, und die Metazeile der aufgeklappten Zeile („1 offen · 5:32 · vor 41 Min · API", Zeile 174) zeigt dieselben Zahlen und bleibt aus demselben Grund leer. Das zweite Symbol der Pflege-Spalte („stilllegen") und die stillgelegte Zeile ohne Stift gehören `I0009`. Das Artboard sagt selbst, dass die Zuordnung der drei Zahlenspalten offen ist (`D0002.dc.html:391, 399`).

### Ablauf

1. **Bearbeitung öffnen**
   - 1.1 Klick auf den Stift in der Spalte „Pflege" der Zeile
   - 1.2 `Kontributoren.razor` merkt sich die offene `KontributorId` und legt ein Bearbeitungsformular mit Name und Art des Kontributors an
   - 1.3 die Zeile wird als `td colspan` über die Tabellenbreite gezeichnet; alle übrigen Zeilen bleiben stehen
2. **Sichern**
   - 2.1 Name ändern, Art wählen, „sichern"
   - 2.2 `KontributorenApiKlient.Aendere(kontributorId, new KontributorAendernAnfrage(name, art))` → `PUT /api/kontributoren/{kontributorId}`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 2.3 `KontributorenService.AendereKontributor` prüft **vor** dem Schreiben
     - 2.3.1 Name leer oder Art unbekannt → `Ergebnis<Kontributor>.Zurueckgewiesen` → HTTP 400 mit Befund
     - 2.3.2 gültig → `KontributorenRepository.Aendere`; liefert es `null`, gibt es diesen Kontributor nicht → `Ergebnis<Kontributor>.Zurueckgewiesen` mit `Nichtgefunden.Kontributor(kontributorId)` → HTTP 404, weil `Zurueckweisungen.AlsFehlerantwort` den Code als fehlendes Ding erkennt
   - 2.4 HTTP 200 → die Zeile schließt, die Seite lädt die ganze Liste neu; der Kontributor steht an seiner neuen alphabetischen Stelle
   - 2.5 Zurückweisung → die Zeile bleibt offen und zeigt „Ohne Namen bleibt der Kontributor, wie er war."
3. **Verwerfen**
   - 3.1 „verwerfen" vergisst das Formular und die offene `KontributorId`; kein Aufruf, keine Änderung

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `KontributorenEndpunkte` — die **erste Adresse des einzelnen Kontributors**, `PUT /api/kontributoren/{kontributorId:long}`. Muster: `PUT /api/boards/{boardId:long}` (`BoardEndpunkte.cs:17`). Name und Art werden zusammen gesichert, weil das Artboard einen Schalter „sichern" zeigt; zwei Unterressourcen wären zwei Aufrufe für einen Vorgang.
- `Kontributoren.razor` — die Liste bekommt eine Spalte und einen Zeilenzustand; die bestehende Anlegezeile bleibt, wie sie ist.
- `Nichtgefunden` — die eine Stelle, an der aus „das Ding gibt es nicht" ein Befund wird; sie wächst um `Kontributor`, statt dass der Endpunkt einen eigenen 404-Rumpf schreibt.

**Klassen-Entwurf:**

- `KontributorAendernAnfrage` (Contract, DTO, immutable) — der Rumpf des Änderns. Gleiche Felder wie `KontributorAnlegenAnfrage`, aber ein eigener Typ: die beiden Vorgänge dürfen sich getrennt entwickeln, und ein gemeinsamer Typ würde beim ersten zusätzlichen Feld auseinandergerissen.
  - `public record KontributorAendernAnfrage(string Name, Kontributorart Art)`
- `KontributorenValidator` (Operation, pure Logik, **geändert**) — prüft weiterhin Name und Art; die Kompensationsaktion nennt je Fall die Route, an der der Aufrufer steht.
  - `Pruefbefunde Pruefe(KontributorAnlegenAnfrage anfrage)` (bestehend)
  - `Pruefbefunde Pruefe(long kontributorId, KontributorAendernAnfrage anfrage)`
- `Nichtgefunden` (Operation, **geändert**) — ein Befund mehr, ein Code mehr in `AlleCodes`.
  - `Fehlerbefund Kontributor(long kontributorId)`
- `IKontributorenRepository` / `KontributorenRepository` (Provider, Ressourcenzugriff, **geändert**) — `UPDATE Kontributor SET Name, Kontributorart` in einer Transaktion, danach dasselbe Zurücklesen wie bei `LegeAn`. `null` heißt: diese `KontributorId` gibt es nicht — wie `BoardRepository.BenenneUm`.
  - `Kontributor? Aendere(long kontributorId, KontributorAendernAnfrage anfrage)`
- `KontributorenService` (Integration, **geändert**) — drei Lagen statt zwei: ungültig, unbekannt, Erfolg.
  - `Ergebnis<Kontributor> AendereKontributor(long kontributorId, KontributorAendernAnfrage anfrage)`
- `KontributorenEndpunkte` (Integration, statisch, **geändert**) — eine Route mehr; 400 und 404 gehen beide durch `Zurueckweisungen.AlsFehlerantwort`, das am Code des Befunds trennt.
  - `routen.MapPut(Basisroute + "/{kontributorId:long}", AendereKontributor).WithName("KontributorAendern")` → 200 / 400 / 404
- `KontributorenApiKlient` (Integration, Blazor, **geändert**) — der HTTP-Weg der Oberfläche; 400 und 404 tragen beide eine `Zurueckweisung` mit Befunden und laufen denselben Weg.
  - `public Task<ApiErgebnis<Kontributor>> Aendere(long kontributorId, KontributorAendernAnfrage anfrage)`
- `Kontributorenmeldung` (Operation, Blazor, **geändert**) — der Wortlaut hängt daran, welche Zeile fragt: die Anlegezeile sagt „Ohne Namen entsteht kein Kontributor.", die Bearbeitungszeile „Ohne Namen bleibt der Kontributor, wie er war."
  - `string AusAnlage(Zurueckweisung zurueckweisung)`
  - `string AusAenderung(Zurueckweisung zurueckweisung)`
- `Kontributoren.razor` (UI, **geändert**) — hält zusätzlich die offene `KontributorId` und ein Bearbeitungsformular; die Zeile wird entweder als Anzeigezeile oder als aufgeklappte Bearbeitungszeile gezeichnet.

### Änderungen an bestehenden Klassen

- `IKontributorenRepository` (`:7`) — bekommt `Aendere`. **`TestKontributorenRepository.cs:11` muss mitziehen, sonst baut `KanbanC.BL.Tests` nicht.** Das Test-Repository bekommt dabei denselben Beobachter wie beim Anlegen (`ErhalteneAnfrage`), damit „bei Zurückweisung wird nicht geschrieben" prüfbar bleibt.
- `KontributorenValidator` (`:9, :21, :33`) — kennt heute nur die Konstante `Anlegeroute = "POST /api/kontributoren"` und verdrahtet sie in beiden Befunden. Mit einer zweiten Route nennt die Kompensationsaktion die jeweilige. Muster: `B0118`. `KontributorenValidatorTests.cs:59-66` prüft den Wortlaut wörtlich und zieht mit — der Anlegefall bleibt bestehen, der Änderungsfall kommt dazu.
- `Nichtgefunden` — trägt heute `Board`, `Karte`, `FremdeKarte`, `Spalte`, `FremdeSpalte`; `AlleCodes` wächst um `kontributor-unbekannt`. `MeldetEinFehlendesDing` braucht dafür keine Änderung, nur den neuen Code in der Liste.
- `KontributorenService` (`:19-30`) — kennt bisher nur „ungültig gegen Erfolg" und braucht die dritte Lage „unbekannter Kontributor".
- `KontributorenEndpunkte` — der Kommentar `:18-19` („eine Adresse des einzelnen Kontributors gibt es noch nicht") wird falsch und geht heraus; der `Location`-Kopf von `LegeKontributorAn` (`:29`) zeigt danach auf `/api/kontributoren/{kontributorId}`. **Das löst die Hälfte eines grünen `R00011`-Kriteriums ab** („`Location`-Kopf auf die Wurzelressource", `R00011`, Abschnitt „Anlegen und Abrufen über die API"): sobald es die Adresse gibt, ist der Verweis auf die Wurzel die schlechtere Antwort. `KontributorenEndpunkteTests.cs:25` zieht mit.
- `FehlervertragTests` — `PUT /api/kontributoren/{kontributorId:long}` bekommt **zwei** Fälle in `AlleFehlerantworten` (leerer Name → 400, unbekannte `KontributorId` → 404). Ohne sie schlägt `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` (`:53-56`) fehl, sobald die Route registriert ist — die Route und ihr Vertragsfall gehören zusammen.
- `KontributorenSeite` (PageObject, `:18-25`) — hängt an der heutigen Zeilenstruktur und bekommt Locator für Stift, Bearbeitungszeile, „sichern" und „verwerfen".
- `Kontributorenmeldung` (`:10`) — bildet heute `kontributor-name-leer` auf einen einzigen Satz ab; die Bearbeitungszeile braucht ihren eigenen.
- `WebApiNeustartTests` — ein Fall mehr: der geänderte Stand übersteht den Neustart.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KontributorenValidator` — leerer Name und Name aus Leerzeichen beim Ändern; unbekannte Art; gültige Anfrage ohne Befund; und der Nachweis, dass die Kompensation des Änderungsfalls `PUT /api/kontributoren/{kontributorId}` nennt, die des Anlegefalls weiterhin `POST /api/kontributoren`.
- `Nichtgefunden.Kontributor` — der Befund nennt die angefragte Nummer und `GET /api/kontributoren` als Kompensation; `MeldetEinFehlendesDing` erkennt seinen Code.
- `KontributorenService` gegen `TestKontributorenRepository` — gültige Anfrage schreibt und liefert den Kontributor; leerer Name liefert eine Zurückweisung **ohne** Schreibzugriff (Beobachterflag); ein `null` vom Repository wird zur Zurückweisung mit dem Nichtgefunden-Befund.
- `Kontributorenmeldung` — die Änderung liefert „Ohne Namen bleibt der Kontributor, wie er war.", die Anlage weiterhin „Ohne Namen entsteht kein Kontributor."; ein unbekannter Code fällt auf die Meldungen der WebApi zurück.
- `KontributorenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert den geänderten Kontributor; 400 und 404 liefern die Zurückweisung **mit den Befunden der WebApi**; geprüft wird zusätzlich, dass Methode (`PUT`) und Adresse (`api/kontributoren/{id}`) des abgesetzten Aufrufs stimmen. Diese Pfade sind über den Browser nicht auslösbar.

**Integration:**
- `KontributorenRepository` gegen eine `TemporaereDatenbank` — Ändern schreibt Name und Art und liest die geänderte Zeile zurück; ein zweiter Kontributor bleibt unberührt; eine unbekannte `KontributorId` liefert `null` und ändert nichts; alle drei Arten sind Ziel.
- `KontributorenEndpunkte` über `TestWebApi` — 200 mit dem geänderten Kontributor, 400 mit Befund bei leerem Namen, 404 mit Befund bei unbekannter `KontributorId`, und je die Zusicherung, dass danach nichts geändert ist; der `Location`-Kopf des Anlegens zeigt auf die neue Adresse.
- `FehlervertragTests` — die beiden neuen Fehlerantworten werden aufgenommen; die Prüfung „keine Route ungeprüft" bleibt grün.
- `WebApiNeustartTests` — der geänderte Stand übersteht den Neustart.

**E2E:** Stift klicken, Name und Art ändern, sichern; die Liste zeigt den neuen Stand an neuer alphabetischer Stelle und nach einem Reload weiterhin (US-1). „verwerfen" lässt den alten Stand stehen (US-2). Solange eine Zeile offen ist, sind die übrigen Kontributoren sichtbar (US-3). Leerer Name zeigt „Ohne Namen bleibt der Kontributor, wie er war.", die Zeile bleibt offen und bedienbar (US-5). Ein Agent ändert über die API, der Mensch sieht den neuen Stand (US-4). Neue Testklasse `KontributorAendernE2ETests`, erweitertes Seitenobjekt `KontributorenSeite`. Dazu laufen alle E2E-Tests aus `R00001`–`R00011` weiter, `KontributorenlisteE2ETests` und `KontributorAnlegenE2ETests` unverändert.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: **`R00011`** (Kontributor anlegen). Die WBS-Spalte `Braucht` von `I0007` nennt `I0006`; der Knoten ist `gruen` (`Dokumentation/Planung/kanbanc.md:144, 159`). Ohne die Tabelle `Kontributor`, `KontributorenRepository`, `KontributorenService`, `KontributorenEndpunkte`, `KontributorenApiKlient` und die Seite `/kontributoren` gibt es nichts zu ändern.
- Setzt auf vorhandene Bausteine auf: `R00001` (`Ergebnis<T>`, `Pruefbefunde`), `R00005` (Token-Sheet, `oberflaeche.css`), `R00007` (Fehlervertrag: `Fehlerbefund`, `Zurueckweisung`, `Zurueckweisungen.AlsFehlerantwort`, `Nichtgefunden`), `R00006` (`WebApiAufruf.MitAusfallmeldung`), `R00010` (Muster `PUT` mit 400 und 404 an `BoardEndpunkte`/`BoardService`).
- Ändert bestehende grüne Tests: `KontributorenValidatorTests`, `KontributorenEndpunkteTests`, `FehlervertragTests`, `TestKontributorenRepository`, `KontributorenSeite` (siehe „Änderungen an bestehenden Klassen").
- Blockiert: nichts unmittelbar. Kein Knoten der WBS nennt `I0007` in seiner Spalte `Braucht`; `I0008` und `I0009` hängen an `I0006`, nicht an diesem Slice. Geprüft am 2026-09-04 über `Dokumentation/Planung/kanbanc.md`.
- Reihenfolge innerhalb der Anforderung: `F0029` vor `F0030` — `F0030` nennt `F0029` in `Braucht`. Und innerhalb von `F0029`/`F0030` gehören `B0152` und `B0159` zusammen: zwischen der registrierten Route und ihrem Vertragsfall ist `FehlervertragTests` rot.

## Umfang

```
Kontributoren bearbeiten (I0007) = 11 Bubbles: 11 Standard (14,0h), 0 unklar.
Rest: 14,0h klar · 5 von 11 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0007` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0029` Kontributor ändern | B0150–B0156 (7) | 10,8h klar | `I0006` |
| `F0030` Ungültige Änderung und unbekannten Kontributor zurückweisen | B0157–B0160 (4) | 3,2h klar | `F0029` |

Belegt sind die fünf Prüf-, Datenzugriffs- und Verdrahtungs-Bubbles (`B0150`, `B0151`, `B0157`–`B0159`; Vergleichswerte `B0027`, `B0028`, `B0029` in `Schaetzungen/_ist-zeiten.md`); die Endpunkt-, Klienten-, UI- und E2E-Bubbles tragen den Richtwert 2h ohne Messung. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Derselbe Vermerk wie bei `I0005` und `I0006`, damit er nicht als Beifang durchgeht: die 2h-Richtwerte liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde auch hier nicht geändert, weil das die Zählung des ganzen Baums verschöbe; die Frage gehört einmal entschieden, nicht je Slice (`Dokumentation/Planung/kanbanc.md:290`).

## Offene Fragen

- ~~Eine Adresse oder zwei Unterressourcen (`/name`, `/art`)?~~ — entschieden: **eine Adresse, `PUT /api/kontributoren/{kontributorId}`**. Das Artboard hat einen Schalter „sichern"; zwei Unterressourcen wären zwei Aufrufe für einen Vorgang und zwei Gelegenheiten, auf halbem Weg stehen zu bleiben. Muster: `PUT /api/boards/{boardId}` (`Dokumentation/Planung/kanbanc.md:286`).
- ~~Eigener 404-Rumpf im Endpunkt oder `Nichtgefunden`?~~ — entschieden: **`Nichtgefunden` wächst um `Kontributor`**. `Nichtgefunden.Board/Karte/Spalte` entstand in `B0098` ausdrücklich als die eine Stelle, an der aus „das Ding gibt es nicht" ein Befund wird; ein zweiter, handgeschriebener Rumpf wäre die zweite Quelle, die `B0098` gerade abgeschafft hat (`Dokumentation/Planung/kanbanc.md:287`).
- ~~Was gehört von der gezeichneten Zielform der Liste zu diesem Slice?~~ — entschieden: **nur die Spalte „Pflege"**. „offen", „Zeit" und „letzte Handlung" gehören `I0026` (`D0006`) und `D0007`; die Metazeile der aufgeklappten Zeile bleibt aus demselben Grund leer (`Dokumentation/Planung/kanbanc.md:285`).
- ~~Ist der erste Halbsatz des Fertig-Kriteriums („Alle Kontributoren sind sichtbar") schon mit `I0006` erledigt?~~ — entschieden: **er bleibt Kriterium, als Regressionsschutz**. Als eigener Aspekt wäre er eine Wiederholung von `B0144`; sobald aber eine Zeile als `td colspan` aufklappt, wird „alle sind sichtbar" wieder eine echte Aussage. Geprüft von `B0156`, das zusätzlich die drei bestehenden Tests aus `KontributorenlisteE2ETests` grün halten muss (`Dokumentation/Planung/kanbanc.md:283`).
- **Offen geblieben, weil nicht Gegenstand dieses Slice:** ob der Mensch lieber **einen** Satz für beide Zeilen hätte, statt zweier. Das Artboard schreibt für diesen Rand keinen Satz vor (`Dokumentation/Planung/kanbanc.md:288`); hier ist der zweite Satz gewählt, weil beim Ändern nichts entsteht — siehe „Angenommen im stillen Lauf".
- **Offen geblieben, weil nicht Gegenstand dieses Slice:** ob `GET /api/kontributoren/{kontributorId}` gebraucht wird. Die Adresse trägt nach dieser Anforderung nur `PUT`; ein `GET` darauf antwortet mit 405. Die WBS kennt keinen Knoten dafür, und die Liste liefert alles, was ein Agent braucht — siehe „Missing-Docs".

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Es entsteht keine Migration; bestehende Datenbanken bleiben unverändert.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist eine Einbahnstraße, die `R00011` hinterlassen hat: ein Kontributor entsteht, aber nichts an ihm lässt sich danach je wieder korrigieren — und weil die WBS bewusst kein Löschen kennt, wäre jeder Tippfehler und jede falsch gewählte Art dauerhaft. Wenn Name und Art über eine einzige Adresse änderbar werden, bekommt die Korrektur denselben Weg wie die Anlage: der Mensch über die Zeile, der Agent über den Aufruf — und die Zusage der Vision, dass ein Agent alles kann, was ein Mensch klicken kann, bleibt auch für den Korrekturfall wahr. Der Hebel sitzt hier und nicht später, weil der Name eines Kontributors ab `I0015`, `I0017` und `D0007` in jeder Karte, jedem Kommentar und jeder Auswertung mitläuft: eine Korrektur an einer Stelle ist billig, eine Korrektur nach zwanzig Verweisen ist eine Datenbereinigung. Und dass die Zurückweisung hier zwei Lagen bekommt statt einer — ungültige Eingabe und unbekannter Kontributor —, ist keine Zutat, sondern der Unterschied zum Anlegen: eine Änderung kann sich auf etwas beziehen, das es gar nicht gibt, und ein Agent, der darauf einen leeren 404 bekäme, wüsste nicht, dass er die Liste neu lesen muss.

## Missing-Docs

- **Verhalten einer Route, die nur ein Verb trägt.** Ob ASP.NET Core auf `GET /api/kontributoren/{id}` mit 405 und leerem Rumpf antwortet — und ob `FehlervertragTests` das als ungeprüfte Fehlerantwort zählt oder gar nicht sieht, weil `webApi.Routen` nur registrierte Verben liefert — ist im Bestand nirgends belegt. Betrifft die Frage, ob der `Location`-Kopf auf eine Adresse zeigen darf, die kein `GET` beantwortet. Vor dem Bauen von `B0152` mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`); Vorbilder sind `AbfrageparameterProbeTests` und `KontributorartProbeTests`.
- **Wiedereintritt eines `@bind`-Formulars in einer aufklappenden Tabellenzeile.** Ob Blazor die Bindung eines Formulars sauber verwirft, wenn dieselbe `<tr>` für einen anderen Kontributor neu aufgeklappt wird, oder ob dafür ein `@key` gesetzt sein muss, ist im Bestand nicht belegt — die Anlegezeile existiert genau einmal und stellt die Frage nicht.

## Notizen

### Verworfene Alternativen

- **Zwei Unterressourcen `PUT …/name` und `PUT …/art`.** Feiner geschnitten und für einen Agenten, der nur die Art korrigieren will, sparsamer. Verworfen: das Artboard hat einen Schalter „sichern"; zwei Aufrufe für einen Vorgang können auf halbem Weg scheitern und hinterlassen dann einen Stand, den niemand wollte.
- **`PATCH` mit optionalen Feldern.** Verworfen: der Bestand kennt kein `PATCH`, und `null` als „nicht geändert" macht aus jedem DTO-Feld eine Fallunterscheidung. `KontributorAendernAnfrage` trägt beide Werte, weil die Bearbeitungszeile beide zeigt.
- **`KontributorAnlegenAnfrage` für beide Vorgänge wiederverwenden.** Spart einen Typ. Verworfen (C24, DRY nur bei semantischer Äquivalenz): Anlegen und Ändern sind zwei Vorgänge mit zwei Kompensationsaktionen; der geteilte Typ würde beim ersten zusätzlichen Feld auseinandergerissen. Der Bestand trennt genauso (`SpalteAnlegenAnfrage` / `SpalteAendernAnfrage`).
- **Bearbeiten auf einem eigenen Schirm oder in einem Dialogfenster.** Verworfen: das Artboard klappt die Zeile an Ort und Stelle auf, wie `D0001` es für das Umbenennen in der Kachel tut. Ein zweiter Schirm für ein Textfeld und drei Segmente ist ein Weg zu viel — und der erste Halbsatz des Fertig-Kriteriums („alle sind sichtbar") verlöre seinen Sinn.
- **Mehrere Zeilen gleichzeitig aufklappen lassen.** Verworfen: `B0154` sagt „Klick öffnet genau eine Zeile", und mehrere offene Formulare mit je eigenem ungespeicherten Stand sind ein Zustand, den niemand angefordert hat.
- **Den 404 in der Oberfläche über `ApiAntwortleser.AlsErgebnis` laufen lassen.** Verworfen: dessen `BoardOderSpalteVerschwunden` ersetzt den Befund der WebApi durch „Das Board oder die Spalte gibt es nicht mehr." (`ApiAntwortleser.cs:8-13, :26-29`) — auf der Kontributorenseite wäre das eine falsche Meldung. Siehe „Angenommen im stillen Lauf".
- **`ApiAntwortleser` verallgemeinern, so dass er den 404-Rumpf liest.** Wäre die sauberere Stelle, seit jeder 404 einen Befund trägt. Verworfen für diesen Slice: das ändert das Verhalten von `BoardApiKlient` und `SpaltenApiKlient` und damit grüne Tests aus `R00002`, `R00003` und `R00010` — ein eigener Vorgang, kein Beifang. Gehört als `/anforderung refactoring` gestellt.

### Bewusst out of scope

- **Stilllegen und Zurückholen** samt zweitem Symbol in der Pflege-Spalte, Gruppenzeile „stillgelegt" und Zählzeile „x wählbar · y stillgelegt" (`I0009`) — im Artboard gezeichnet, hier nicht gebaut. Die stillgelegte Zeile trägt kein Stiftsymbol; diese Regel entsteht dort, nicht hier.
- **Identitätswahl** (`I0008`). Dass ein abgebildeter Kontributor nicht als Identität wählbar ist, gehört dorthin; hier sind alle drei Arten in beide Richtungen wählbar, auch von und zu „abgebildet".
- **Die Spalten „offen", „Zeit" und „letzte Handlung"** und die Metazeile der aufgeklappten Zeile (`I0026`, `D0006`, `D0007`).
- **Löschen eines Kontributors.** Die WBS kennt dafür keinen Knoten.
- **Live-Übertragung an andere offene Sichten** (`I0028`): ändert ein Agent, sieht ein offener Browser es erst beim nächsten Laden.
- **Eine Historie der Änderungen.** Wer wann welchen Namen geändert hat, wird nicht festgehalten; die Tabelle trägt den aktuellen Stand.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Neben den Entscheidungen unter „Offene Fragen" stehen sieben Annahmen mit Beleg:

1. **Die Bearbeitungszeile bekommt einen eigenen Wortlaut: „Ohne Namen bleibt der Kontributor, wie er war."** `Kontributorenmeldung.cs:10` kennt heute nur „Ohne Namen entsteht kein Kontributor." — beim Ändern entsteht nichts, der Satz wäre falsch. Das Artboard schreibt für diesen Rand keinen Satz vor (`Dokumentation/Planung/kanbanc.md:288`); nicht geprüft, ob ein Satz für beide Zeilen lieber wäre.
2. **`KontributorenApiKlient.Aendere` liest den Befund aus dem 404-Rumpf selbst**, statt `ApiAntwortleser.AlsErgebnis` zu nutzen. Dessen `BoardOderSpalteVerschwunden` ersetzt jeden 404-Befund durch eine Board-Meldung (`Source/KanbanC.Blazor/Services/ApiAntwortleser.cs:8-13, :26-29`), was hier eine falsche Aussage wäre. 400 und 404 laufen dadurch denselben Weg, wie `B0153` es verlangt.
3. **Die aufgeklappte Zeile ersetzt die Anzeigezeile ihres Kontributors**, statt zusätzlich zu ihr zu erscheinen. So zeichnet es das Artboard (`D0002.dc.html:162-180`: die Zeile „Codex-Agent" steht nur einmal, als `td colspan`). Genau deshalb ist „alle übrigen bleiben sichtbar" eine prüfbare Aussage.
4. **Der Name wird geprüft, aber nicht umgeschrieben.** „Leer" heißt leer nach dem Trimmen (`string.IsNullOrWhiteSpace`), gespeichert wird der Name, wie er ankommt — dieselbe Regel wie beim Anlegen (`R00011`, Annahme 1).
5. **Prüfung vor Datenzugriff:** ein Aufruf mit unbekannter `KontributorId` **und** leerem Namen antwortet mit 400, nicht mit 404. Dieselbe Reihenfolge wie `BoardService.BenenneBoardUm` (`Source/KanbanC.BL/Integrations/Boards/BoardService.cs:45-61`); eine Anfrage, die schon als Anfrage ungültig ist, muss nicht erst nachschlagen.
6. **Die Seite lädt nach dem Sichern die ganze Liste neu**, statt den geänderten Kontributor lokal umzusortieren. Die Reihenfolge gehört der Abfrage (`KontributorenRepository.LadeAlle`), und eine zweite Sortierung in der Oberfläche wäre eine zweite Wahrheit — dieselbe Begründung wie in `B0145`.
7. **Das Namensfeld der Bearbeitungszeile ist 300 px breit wie das der Anlegezeile** und nutzt dieselbe Klasse `.feld-name` (`Kontributoren.razor.css`), statt einen zweiten Wert einzuführen. Das Artboard zeichnet beide gleich breit.

Wer eine dieser Annahmen anders will, ändert sie vor dem Bauen — nach `B0152` kostet die Adressfrage einen zweiten Umbau an `FehlervertragTests` und am `Location`-Kopf.
