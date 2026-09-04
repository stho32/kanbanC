---
id: R00011
status: Neu
datum: 2026-09-04
---

# R00011: Kontributor anlegen

## Beschreibung

Ein Kontributor entsteht mit Name und Art — Mensch, Agent oder abgebildet — und steht danach in der Liste, aus der später gewählt wird, wer man ist. Angelegt wird über `POST /api/kontributoren` und über eine Anlegezeile am Ende der Liste auf `/kontributoren`; `GET /api/kontributoren` und dieselbe Liste zeigen ihn danach, alphabetisch einsortiert, und er überlebt einen Neustart. Eine Anlage ohne Namen wird zurückgewiesen — mit einem Befund für den Agenten, mit dem Satz „Ohne Namen entsteht kein Kontributor." für den Menschen.

Zahlt ein auf: [Vision](R00000-vision.md) — „ich selbst, gemeinsam mit KI-Agenten, die als eigene Kontributoren auf dem Board erscheinen"; und „Kontributoren werden in der Oberfläche angelegt — abgebildete Personen genauso wie alle anderen."

## Geschäftlicher Nutzen

Ohne Kontributoren ist das Board anonym: an keiner Karte und keiner Zeit ist ablesbar, wer gehandelt hat — die Zusage des Zielbilds („An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat") hat bis hierhin keinen Gegenstand. Dieser Slice legt ihn an. Er ist zugleich die Voraussetzung von vier weiteren Interactions: Kontributoren bearbeiten (`I0007`), Identität wählen (`I0008`), stilllegen (`I0009`) und, über die Identität, Kommentare (`I0017`) und Timer (`I0023`). Und die dritte Art zahlt auf einen eigenen Punkt der Vision ein: abgebildete Personen bekommen Karten und Zeiten zugeordnet, ohne die Anwendung je zu öffnen — wer im Team mitgeführt wird, muss nicht mitarbeiten können, um in der Auswertung vorzukommen.

## Funktionale Anforderungen

- Ein Kontributor entsteht mit einem Namen und einer der drei Arten Mensch, Agent, abgebildet.
- Angelegt wird über die API und über die Oberfläche; beide Wege erzeugen denselben Kontributor.
- Alle Kontributoren sind über einen Aufruf abrufbar, alphabetisch nach Name, Groß-/Kleinschreibung ohne Einfluss.
- Die Oberfläche zeigt die Liste mit Name und Art; die Art ist an ihrer Plakette erkennbar.
- Die Anlage sitzt als Zeile am Ende der Liste, nicht auf einem eigenen Schirm.
- Eine Anlage ohne Namen wird zurückgewiesen — mit einem Befund aus Grund, Werten und Kompensationsaktion, nicht als Serverfehler.
- Der Navigationspunkt „Kontributoren" der Kopfzeile führt auf die Seite, statt gesperrt zu sein.
- Kontributoren überleben einen Neustart der WebApi.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** Die Migration ist idempotent. Der `Migrationslaeufer` führt jedes eingebettete Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-24`) — also `CREATE TABLE IF NOT EXISTS`, kein `ALTER TABLE`.
- **Fehlervertrag:** Jede Fehlerantwort trägt einen Rumpf mit `Code`, `Meldung` und `Kompensation` (`R00007`, geprüft von `FehlervertragTests`). Das gilt für die beiden neuen Routen ab ihrem ersten Tag.
- **Kernregel des Projekts:** `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL`; die Kontributoren-Seite spricht ausschließlich über HTTP mit der WebApi (`CLAUDE.md`, „Die eine Regel, die den Aufbau trägt").
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"; geprüft von `GestaltungsfundamentTests`).
- **Benennung:** Primärschlüssel `KontributorId`, Domänensprache deutsch und kontexteindeutig (`Kontributor`, `Kontributorart`), Bezeichner ohne echte Umlaute, UI-Texte und Meldungen mit (C06, C07).

## Akzeptanzkriterien

### Anlegen und Abrufen über die API

- [ ] `POST /api/kontributoren` mit `{ "name": "Stefan", "art": "Mensch" }` antwortet mit HTTP 201, einem `Location`-Kopf auf die Wurzelressource und dem angelegten Kontributor samt vergebener `kontributorId`.
- [ ] Dieselbe Route nimmt `"Agent"` und `"Abgebildet"` entgegen und legt sie genauso an; andere Werte gibt es nicht.
- [ ] `GET /api/kontributoren` liefert HTTP 200 und **alle** Kontributoren mit Name und Art — auch die abgebildeten, ohne Filter und ohne Abfrageparameter.
- [ ] Die Liste ist alphabetisch nach Name sortiert, Groß-/Kleinschreibung ohne Einfluss, `KontributorId` als Zweitschlüssel. Rechenbeispiel: `stefan`, `Codex-Agent`, `Nina Barth` angelegt in dieser Reihenfolge → geliefert werden `Codex-Agent`, `Nina Barth`, `stefan`.
- [ ] Zwei Kontributoren dürfen denselben Namen tragen; ein zweiter „Stefan" wird **nicht** zurückgewiesen und bekommt eine eigene `KontributorId`.
- [ ] Nach einem Neustart der WebApi auf derselben Datei liefert `GET /api/kontributoren` dieselben Kontributoren; die nächste vergebene `KontributorId` schließt an die bestehenden an.
- [ ] Ein zweiter Lauf der Migration auf einer Datei mit Kontributoren lässt Schema **und** Daten unverändert.

### Kontributorenliste in der Oberfläche

- [ ] `/kontributoren` zeigt eine Liste mit den Spalten Name und Art; ohne angelegte Kontributoren ist sie leer und die Anlegezeile trotzdem bedienbar.
- [ ] Jede Zeile trägt eine Plakette, an der die Art zu erkennen ist; die drei Arten sind voneinander unterscheidbar dargestellt.
- [ ] Am Ende der Liste steht eine Anlegezeile mit Namensfeld, einer Wahl zwischen Mensch, Agent und abgebildet — Mensch vorgewählt — und dem Schalter „anlegen". Es gibt keinen zweiten Schirm und kein Dialogfenster.
- [ ] „anlegen" erzeugt den Kontributor; er erscheint danach an seiner alphabetischen Stelle in der Liste, ohne dass die Seite neu geladen wird.
- [ ] Nach einem Reload der Seite steht er weiterhin da.
- [ ] Was über die API angelegt wurde, steht in der danach geöffneten Liste der Oberfläche — und umgekehrt liefert `GET /api/kontributoren` den in der Oberfläche angelegten Kontributor.
- [ ] Ist die WebApi beim Anlegen nicht erreichbar, erscheint die Ausfallmeldung statt einer Ausnahmeseite; die Liste bleibt bedienbar.

### Zurückweisung ohne Namen

- [ ] `POST /api/kontributoren` mit leerem oder nur aus Leerzeichen bestehendem `name` antwortet mit HTTP 400 — nicht 500 — und einem Befund mit nichtleerem `Code`, einer `Meldung`, die den Grund nennt, und einer `Kompensation`, die genau diese Route mit einem nichtleeren `name` als nächsten Schritt nennt.
- [ ] Nach einer solchen Zurückweisung ist nichts geschrieben: `GET /api/kontributoren` liefert unverändert die Kontributoren von vorher.
- [ ] In der Oberfläche erscheint bei leerem Namen der Satz **„Ohne Namen entsteht kein Kontributor."**; die Anlegezeile bleibt bedienbar, der eingestellte Artwert bleibt stehen, und es entsteht kein Kontributor.
- [ ] `FehlervertragTests` nimmt die Fehlerantwort von `POST /api/kontributoren` in die Prüfung auf; keine Route der WebApi bleibt ungeprüft (der zweite Test der Klasse würde sonst rot).

### Navigationspunkt und Rahmen

- [ ] Der Punkt „Kontributoren" in der Kopfzeile ist ein Verweis auf `/kontributoren` und trägt kein `aria-disabled` mehr; ein Klick darauf öffnet die Seite.
- [ ] Auf der offenen Kontributoren-Seite ist der Punkt als aktiv erkennbar, so wie „Boards" auf der Board-Übersicht.
- [ ] Die Kopfzeile trägt auf dieser Seite den Titel „Kontributoren"; Titel, Navigation und Identitätsplatz stehen wie bisher.
- [ ] `RahmenE2ETests` zieht mit: die beiden Zusicherungen `aria-disabled=true` für „Kontributoren" und `NavigationsVerweise` = 1 (`Source/KanbanC.PlaywrightTests/Tests/RahmenE2ETests.cs:56-57`) beschreiben danach den neuen Stand — „Auswertungen" bleibt gesperrt, „Kontributoren" nicht mehr, und die Board-Übersicht trägt zwei Verweise statt einem.
- [ ] Die gesamte `R00005`-Suite (Rahmen, Gestaltungsfundament, Board-Übersicht) ist nach der Änderung grün; kein Test wird gelöscht, um das zu erreichen.

## Betroffene Verzeichnisstruktur

- **Contracts:** `Source/KanbanC.Contracts/Kontributoren/` — der Ordner existiert und ist leer; hier entstehen `Kontributor`, `KontributorAnlegenAnfrage` und die Aufzählung `Kontributorart`.
- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/006-kontributoren.sql` — neue, idempotente Migration. Die Einbettung deckt der bestehende Platzhalter `<EmbeddedResource Include="Persistenz\Migrationen\**\*.sql" />` ab (`Source/KanbanC.BL/KanbanC.BL.csproj:8`), ein Projekteintrag ist nicht nötig.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Kontributoren/KontributorenRepository.cs` (neu), `Source/KanbanC.BL/Interfaces/Kontributoren/IKontributorenRepository.cs` (neu).
- **Prüfung:** `Source/KanbanC.BL/Operations/Kontributoren/KontributorenValidator.cs` (neu).
- **Fachlogik:** `Source/KanbanC.BL/Integrations/Kontributoren/KontributorenService.cs` (neu).
- **API:** `Source/KanbanC.WebApi/Endpunkte/KontributorenEndpunkte.cs` (neu), Registrierung und DI in `Source/KanbanC.WebApi/Program.cs`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KontributorenApiKlient.cs` (neu), `Source/KanbanC.Blazor/Components/Pages/Kontributoren.razor` (+ `.razor.css`), `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor` (Navigationspunkt), DI in `Source/KanbanC.Blazor/Program.cs`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Kontributoren/`, `Source/KanbanC.BL.Tests/Integrations/Kontributoren/` und `TestHelpers/TestKontributorenRepository.cs`; `Source/KanbanC.Blazor.Tests/Services/KontributorenApiKlientTests.cs`; `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Kontributoren/KontributorenRepositoryTests.cs`, `Api/KontributorenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Persistenz/MigrationslaeuferTests.cs`; `Source/KanbanC.PlaywrightTests/PageObjects/KontributorenSeite.cs` (neu), `Tests/KontributorAnlegenE2ETests.cs` (neu), `Tests/RahmenE2ETests.cs` (geändert).
- **Unberührt:** `wwwroot/gestaltung.css` und `oberflaeche.css` — die Seite bringt ihre Gestaltung in `Kontributoren.razor.css` mit, mit Werten aus dem Token-Sheet; die Meldung nutzt die vorhandene `.meldung-abweisung` (`oberflaeche.css:44`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0002.dc.html`](../Dokumentation/Wireframes/D0002.dc.html) ist die Gestaltungsvorgabe; einschlägig sind **Zustand 1** (`/kontributoren`, ab Zeile 98) mit der Anlegezeile am Ende der Liste (Zeilen 241–257), der Rand-Fall der zurückgewiesenen Anlage (Zeilen 260–264) und die Farbregel der drei Arten (Zeile 392: Mensch Olive `accent-2`, Agent Terrakotta `accent`, abgebildet neutral mit gestricheltem Rand). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Was es an Umfang klärt: die Spalten „offen", „Zeit", „letzte Handlung" und „Pflege" zeigen die Zielform und gehören `I0007`, `I0009`, `I0026` und `D0007` — sie fehlen hier. Die Zählzeile „4 wählbar · 1 stillgelegt" im Seitenkopf setzt `I0009` voraus und entsteht dort. Das Artboard sagt selbst, dass diese Zuordnung offen ist (`D0002.dc.html:393`).

### Ablauf

1. **Seite öffnen**
   - 1.1 Klick auf „Kontributoren" in der Kopfzeile oder Direktaufruf von `/kontributoren`
   - 1.2 `Kontributoren.razor` ruft `KontributorenApiKlient.LadeAlle()` → `GET /api/kontributoren`
   - 1.3 `KontributorenService.LadeAlleKontributoren` → `KontributorenRepository.LadeAlle` mit `ORDER BY Name COLLATE NOCASE, KontributorId`
2. **Anlegen**
   - 2.1 Name eintragen, Art wählen (Mensch vorgewählt), „anlegen"
   - 2.2 `KontributorenApiKlient.LegeAn(new KontributorAnlegenAnfrage(name, art))` → `POST /api/kontributoren`, umschlossen von `WebApiAufruf.MitAusfallmeldung`
   - 2.3 `KontributorenService.LegeKontributorAn` prüft die Anfrage **vor** dem Schreiben
     - 2.3.1 Name leer → `Ergebnis<Kontributor>.Zurueckgewiesen` → HTTP 400 mit Befund
     - 2.3.2 Art unbekannt → derselbe Weg, eigener Befund
   - 2.4 gültig → `KontributorenRepository.LegeAn` schreibt in einer Transaktion und liest den Kontributor mit seiner `KontributorId` zurück
   - 2.5 HTTP 201 mit `Location` → die Seite lädt die Liste neu; der neue Kontributor steht an seiner alphabetischen Stelle
   - 2.6 Zurückweisung → die Seite zeigt „Ohne Namen entsteht kein Kontributor."; die Anlegezeile behält Namensfeld und Artwahl

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `KontributorenEndpunkte` — **eigene Wurzelressource** `/api/kontributoren`, kein Unterpfad eines Boards: ein Kontributor gehört der Anwendung, nicht einem Board. `I0007`, `I0008` und `I0009` hängen an derselben Adresse, und die Zeiterfassung (`D0006`) braucht Kontributoren board-übergreifend.
- `Kopfzeile.razor` — der `<span class="navigationspunkt navigationspunkt-gesperrt" id="navigation-kontributoren" aria-disabled="true">` wird zum `NavLink` mit `href="kontributoren"` und `ActiveClass="navigationspunkt-aktiv"`, wie der Punkt „Boards" darüber. Die Klasse `navigationspunkt-gesperrt` bleibt im Stylesheet, weil „Auswertungen" sie weiter braucht.
- `Program.cs` beider Prozesse — je eine Registrierung: WebApi `IKontributorenRepository`, `KontributorenService` und `KontributorenEndpunkte.Registriere(app)`; Blazor `KontributorenApiKlient` als Scoped.
- `Migrationslaeufer` — nimmt `006-kontributoren.sql` allein über den Dateinamen auf (Sortierung `StringComparer.Ordinal`), ohne Codeänderung.

**Klassen-Entwurf:**

- `Kontributorart` (Contract, Aufzählung) — genau drei Werte, als Text im JSON wie `BoardArt`.
  - `[JsonConverter(typeof(JsonStringEnumConverter<Kontributorart>))] public enum Kontributorart { Mensch, Agent, Abgebildet }`
- `Kontributor` (Contract, DTO, immutable) — was die Liste zeigt und was die API liefert. Kein zweites Übersichts-DTO: die Liste zeigt Name und Art, also den ganzen Kontributor.
  - `public record Kontributor(long KontributorId, string Name, Kontributorart Art)`
- `KontributorAnlegenAnfrage` (Contract, DTO, immutable) — der Rumpf des Anlegens.
  - `public record KontributorAnlegenAnfrage(string Name, Kontributorart Art)`
- `KontributorenValidator` (Operation, pure Logik) — prüft Name und Art; jeder Befund nennt Grund mit Werten und die Kompensationsaktion mit dieser Route.
  - `Pruefbefunde Pruefe(KontributorAnlegenAnfrage anfrage)`
- `IKontributorenRepository` / `KontributorenRepository` (Provider, Ressourcenzugriff) — schreibt in einer Transaktion, liest zurück; die Sortierung sitzt in der Abfrage, nicht im Service.
  - `Kontributor LegeAn(KontributorAnlegenAnfrage anfrage)`
  - `IReadOnlyList<Kontributor> LadeAlle()`
- `KontributorenService` (Integration) — verdrahtet Prüfung und Datenzugriff.
  - `Ergebnis<Kontributor> LegeKontributorAn(KontributorAnlegenAnfrage anfrage)`
  - `IReadOnlyList<Kontributor> LadeAlleKontributoren()`
- `KontributorenEndpunkte` (Integration, statisch) — zwei Routen.
  - `routen.MapPost(Basisroute, LegeKontributorAn).WithName("KontributorAnlegen")` → 201 / 400 über `Zurueckweisungen.AlsFehlerantwort`
  - `routen.MapGet(Basisroute, LadeAlleKontributoren).WithName("KontributorenAuflisten")` → 200
- `KontributorenApiKlient` (Integration, Blazor) — der HTTP-Weg der Oberfläche; die Fehlerpfade sind über den Browser nicht auslösbar und werden in `KanbanC.Blazor.Tests` geprüft.
  - `public Task<ApiErgebnis<Kontributor>> LegeAn(KontributorAnlegenAnfrage anfrage)`
  - `public Task<IReadOnlyList<Kontributor>> LadeAlle()`
- `Kontributoren.razor` (UI, Route `/kontributoren`) — Liste mit Artplakette und Anlegezeile am Ende; hält die geladene Liste und die Meldung.
- **Migration** `006-kontributoren.sql` (Skript, idempotent) — Art als Text wie `Board.Art`, Spaltenname nach der Vorgabe aus `B0138`.
  ```sql
  CREATE TABLE IF NOT EXISTS Kontributor
  (
      KontributorId  INTEGER PRIMARY KEY AUTOINCREMENT,
      Name           TEXT    NOT NULL,
      Kontributorart TEXT    NOT NULL
  );
  ```
  Kein `UNIQUE` auf `Name`: zwei Menschen dürfen gleich heißen, und die Anwendung unterscheidet über die `KontributorId`.

### Änderungen an bestehenden Klassen

- `Kopfzeile.razor` — ein `<span>` wird zum `NavLink`; `id="navigation-kontributoren"` bleibt, damit das Seitenobjekt `Rahmen` unverändert findet.
- `Program.cs` (WebApi) — drei Zeilen: Repository, Service, `KontributorenEndpunkte.Registriere(app)`.
- `Program.cs` (Blazor) — eine Zeile: `builder.Services.AddScoped<KontributorenApiKlient>()`.
- `FehlervertragTests` — `POST /api/kontributoren` bekommt einen Fall in `AlleFehlerantworten` (leerer Name). `GET /api/kontributoren` hat keine Fehlerantwort und gehört deshalb in `RoutenOhneFehlerantwort` (`Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs:14-19`) — sonst schlägt der zweite Test der Klasse fehl, sobald die Route existiert.
- `RahmenE2ETests` — die Zusicherungen in `Wenn_eine_Seite_offen_ist_dann_stehen_Auswertungen_und_Kontributoren_sichtbar_aber_ohne_Weg_da` (Zeilen 54–57) beschreiben den alten Stand: „Kontributoren" trägt danach kein `aria-disabled` mehr, und die Board-Übersicht zeigt zwei Navigationsverweise statt einem. Der Test wird umgeschrieben, nicht gelöscht: „Auswertungen" bleibt gesperrt und muss weiter beweisbar gesperrt sein.
- `WebApiNeustartTests` — ein Fall mehr: angelegte Kontributoren überstehen den Neustart.
- `MigrationslaeuferTests` — zweiter Lauf auf einer Datei mit Kontributoren.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KontributorenValidator` — leerer Name, nur Leerzeichen, gültiger Name, unbekannte Art; und der Nachweis, dass die Kompensation `POST /api/kontributoren` nennt.
- `KontributorenService` gegen `TestKontributorenRepository` — gültige Anfrage schreibt und liefert den Kontributor; leerer Name liefert eine Zurückweisung **ohne** Schreibzugriff (Beobachterflag im Test-Repository); `LadeAlleKontributoren` reicht die Reihenfolge des Repositories unverändert durch.
- `KontributorenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 201 liefert den Kontributor, 400 liefert die Zurückweisung mit ihren Befunden; geprüft wird zusätzlich, dass Methode und Adresse des abgesetzten Aufrufs stimmen. Diese Pfade sind über den Browser nicht auslösbar.

**Integration:**
- `KontributorenRepository` gegen eine `TemporaereDatenbank` — Anlegen vergibt eine `KontributorId`; `LadeAlle` liefert gemischt geschriebene Namen alphabetisch ohne Rücksicht auf Groß-/Kleinschreibung; alle drei Arten kommen unverändert zurück; zwei gleiche Namen sind erlaubt.
- `Migrationslaeufer` — zweiter Lauf auf einer Datei mit Kontributoren: Schema und Daten unverändert.
- `KontributorenEndpunkte` über `TestWebApi` — 201 mit `Location` und Rumpf, 200 mit der sortierten Liste, 400 mit Befund bei leerem Namen und die Zusicherung, dass danach nichts gespeichert ist.
- `FehlervertragTests` — die neue Fehlerantwort wird aufgenommen, die neue Leseroute als fehlerfrei geführt; die Prüfung „keine Route ungeprüft" bleibt grün.
- `WebApiNeustartTests` — Kontributoren überstehen den Neustart, die nächste `KontributorId` schließt an.

**E2E:** Über den Navigationspunkt auf die Seite gehen (US-4); Name eintragen, Art wählen, anlegen, der Kontributor steht in der Liste und nach einem Reload weiterhin (US-1, US-3); ein Agent legt über die API an, der Mensch sieht ihn (US-2); leerer Name zeigt „Ohne Namen entsteht kein Kontributor.", die Zeile bleibt bedienbar (US-5). Neues Seitenobjekt `KontributorenSeite`. Dazu laufen alle E2E-Tests aus `R00001`–`R00010` weiter, `RahmenE2ETests` in angepasster Form.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: **keiner Anforderung**. Die WBS-Spalte `Braucht` von `I0006` ist leer — der Slice ist frei (`Dokumentation/Planung/kanbanc.md:144`).
- Setzt auf vorhandene Bausteine auf, ohne sie zu ändern: `R00001` (`Ergebnis<T>`, `Pruefbefunde`, `Migrationslaeufer`, `SqliteVerbindungsfabrik`), `R00005` (Kopfzeile, Token-Sheet, `oberflaeche.css`), `R00007` (Fehlervertrag: `Fehlerbefund`, `Zurueckweisung`, `Zurueckweisungen.AlsFehlerantwort`), `R00006` (`WebApiAufruf.MitAusfallmeldung`).
- Ändert einen bestehenden grünen Test: `RahmenE2ETests` aus `R00005` (siehe „Änderungen an bestehenden Klassen").
- Blockiert: `I0007` (Kontributoren bearbeiten), `I0008` (Identität wählen), `I0009` (Kontributor stilllegen) und `I0015` (Kartendetails bearbeiten) — alle vier nennen `I0006` in ihrer Spalte `Braucht`; über `I0008` mittelbar auch `I0017` (Karte kommentieren) und `I0023` (Timer starten). Geprüft am 2026-09-04 über `Dokumentation/Planung/kanbanc.md`.
- Reihenfolge innerhalb der Anforderung: `F0027` vor `F0028` — `F0028` nennt `F0027` in `Braucht`, weil die zurückgewiesene Anlage die Anlegezeile voraussetzt.

## Umfang

```
Kontributor anlegen (I0006) = 12 Bubbles: 12 Standard (14,4h), 0 unklar.
Rest: 14,4h klar · 6 von 12 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0006` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0027` Kontributor anlegen und abrufen | B0138–B0146 (9) | 11,6h klar | — |
| `F0028` Anlage ohne Namen zurückweisen | B0147–B0149 (3) | 2,8h klar | `F0027` |

Belegt sind die sechs Prüf-, Datenzugriffs- und Verdrahtungs-Bubbles (`B0138`–`B0141`, `B0147`, `B0148`; Vergleichswerte `B0002`, `B0004`, `B0027`, `B0028`, `B0029` in `Schaetzungen/_ist-zeiten.md`); die Endpunkt-, Klienten-, UI- und E2E-Bubbles tragen den Richtwert 2h ohne Messung. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Derselbe Vermerk wie bei `I0005`, damit er nicht als Beifang durchgeht: die 2h-Richtwerte liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde auch hier nicht geändert, weil das die Zählung des ganzen Baums verschöbe; die Frage gehört einmal entschieden, nicht je Slice (`Dokumentation/Planung/kanbanc.md:268`).

## Offene Fragen

- ~~Eigene Wurzelressource oder Unterressource eines Boards?~~ — entschieden: **`/api/kontributoren` als eigene Wurzelressource**. Ein Kontributor gehört der Anwendung, nicht einem Board; `I0007`, `I0008` und `I0009` hängen an derselben Adresse, und die Zeiterfassung braucht Kontributoren board-übergreifend (`Dokumentation/Planung/kanbanc.md:262`). Nicht geprüft, ob Kontributoren später je Board eingeschränkt werden sollen.
- ~~Liefert `GET /api/kontributoren` die abgebildeten mit?~~ — entschieden: **ja, alle drei Arten ohne Filter**. Die Regel „ein abgebildeter Kontributor ist kein Akteur" hat in diesem Slice nur eine sichtbare Folge: die neutrale Plakette. Die Nicht-Wählbarkeit gehört `I0008`; ein Abfrageparameter `waehlbar` wäre eine Vorwegnahme der Identitätswahl (`Dokumentation/Planung/kanbanc.md:265`).
- ~~Braucht die Liste ein eigenes Übersichts-DTO wie `BoardUebersicht`?~~ — entschieden: **nein**. Die Liste zeigt Name und Art, also genau die Felder des `Kontributor`. Wächst sie später um „offen", „Zeit" und „letzte Handlung", entsteht das Übersichts-DTO dort, wo diese Zahlen herkommen (`I0026`, `D0007`), nicht auf Vorrat (`Dokumentation/Planung/kanbanc.md:263`).
- ~~Eigene Tabelle oder eine Spalte an etwas Bestehendem?~~ — entschieden: **neue Tabelle `Kontributor`** über `006-kontributoren.sql` mit `CREATE TABLE IF NOT EXISTS`. `ALTER TABLE … ADD COLUMN` ist in SQLite nicht idempotent und scheitert beim zweiten Lauf des `Migrationslaeufer`; eine bestehende `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich um eine Spalte.
- ~~Was heißt „steht zur Auswahl bereit" im Fertig-Kriterium?~~ — entschieden: **er steht in der Liste, aus der `I0008` später wählen lässt** — sichtbar in der Oberfläche und abrufbar über `GET /api/kontributoren`. Die Identitätswahl selbst wird hier **nicht** gebaut; das Artboard zeichnet dafür zwei Alternativen, zwischen denen noch nicht entschieden ist (`D0002.dc.html:393`, Frage 4 des Wireframe-Index).
- **Offen geblieben, weil nicht Gegenstand dieses Slice:** ob bei einer unlesbaren `art` im JSON (`"art": "Chef"`) eine Fehlerantwort mit unserem Befund entsteht. Der Bestand verhält sich bei `BoardArt` genauso, und das Fertig-Kriterium nennt nur den leeren Namen — siehe „Missing-Docs".

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start der WebApi mit; bestehende Datenbanken bekommen die leere Tabelle dazu.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist eine Lücke, die durch alle bisherigen Anforderungen läuft: Boards, Spalten und Karten stehen, aber niemand steht dahinter — es gibt in der Anwendung keinen Begriff für „wer hat das getan". Wenn Kontributoren als eigene Ressource entstehen, bekommt jede spätere Zuschreibung ihren Anknüpfungspunkt: erst die Identitätswahl (`I0008`), dann Verantwortliche an Karten (`I0015`), Kommentare (`I0017`) und Zeiteinträge (`I0023`) — und am Ende die Auswertung „Zeiten je Aufgabe und Kontributor", die die Vision als Nutzen nennt. Der Hebel sitzt genau hier und nicht später, weil vier Interactions `I0006` in ihrer Spalte `Braucht` führen: ohne diesen Slice ist der ganze Dialog `D0002` und mit ihm die Zeiterfassung blockiert. Und dass die drei Arten schon beim Anlegen unterschieden werden — statt „Mensch oder Agent" jetzt und „abgebildet" später —, verhindert genau die Migration, die man sonst nachschieben müsste: die Unterscheidung steht in der Vision, sie ist kein Zusatzwunsch, und eine Aufzählung nachträglich um einen Wert zu erweitern zieht Validator, Oberfläche und Testdaten ein zweites Mal an.

## Missing-Docs

- **Fehlerverhalten von `JsonStringEnumConverter` bei unbekanntem Text.** Ob `"art": "Chef"` vor dem Handler mit einer Fehlerantwort ohne unseren Befund abgewiesen wird oder den Handler erreicht, ist im Bestand nirgends belegt — `BoardAnlegenValidator` prüft `Enum.IsDefined`, was nur bei numerischen Werten außerhalb des Bereichs greifen kann. Vor dem Bauen des Validators mit einem Probe-Test klären (`~/.claude/skills/dependency-probe/SKILL.md`); ein solcher Test existiert für Abfrageparameter bereits als Vorbild (`Source/KanbanC.WebApi.IntegrationTests/Api/AbfrageparameterProbeTests.cs`).
- **`ActiveClass` von `NavLink` bei mehreren aktiven Kandidaten.** Der Punkt „Boards" verschwindet heute, sobald ein Board offen ist; mit einem zweiten Verweis in derselben Navigation ist nicht belegt, wie sich `NavLinkMatch` auf `/kontributoren` gegenüber `/boards` verhält. Betrifft nur die Hervorhebung, nicht den Weg.

## Notizen

### Verworfene Alternativen

- **`/api/boards/{boardId}/kontributoren` als Unterressource.** Näher am Muster der bestehenden Routen. Verworfen: ein Kontributor gehört keinem Board, und `D0006` braucht ihn board-übergreifend — die Adresse müsste beim ersten Timer wieder umziehen.
- **Ein drittes Feature „steht zur Auswahl bereit".** Verworfen bereits in der Planung: das Abrufen ist dieselbe Prüfung wie das Anlegen — die Liste zeigt den neuen Kontributor — und wäre eine Wiederholung der Interaction (`Dokumentation/Planung/kanbanc.md:261`).
- **Anlegen auf einem eigenen Schirm oder in einem Dialogfenster.** Verworfen: das Artboard antwortet mit einer Zeile am Ende der Liste, wie `D0001` es für das Umbenennen in der Kachel tut; ein zweiter Schirm für ein Textfeld und drei Segmente ist ein Weg zu viel.
- **Abfrageparameter `waehlbar` an `GET /api/kontributoren`.** Verworfen: eine Vorwegnahme von `I0008`. Solange niemand wählt, filtert auch niemand.
- **`UNIQUE` auf `Name`.** Verworfen: der Bestand kennt keine Namenseindeutigkeit — auch zwei Boards dürfen gleich heißen (`001-boards-und-spalten.sql`); nur Spaltenbezeichnungen sind je Board eindeutig (`002-spalte-bezeichnung-eindeutig.sql`). Eine Verschärfung ausgerechnet bei Personennamen wäre eine eigene Entscheidung.
- **Der Navigationspunkt bleibt gesperrt, die Seite ist nur direkt erreichbar.** Verworfen: dann wäre der Slice über die Oberfläche nicht bedienbar, und der geänderte `RahmenE2ETests` bliebe nur aufgeschoben.

### Bewusst out of scope

- **Bearbeiten** von Name und Art (`I0007`), **Stilllegen** samt Zählzeile „x wählbar · y stillgelegt" (`I0009`), **Identitätswahl** (`I0008`) — im Artboard gezeichnet, hier nicht gebaut.
- **Die Spalten „offen", „Zeit", „letzte Handlung" und „Pflege"** der gezeichneten Liste (`I0026`, `D0006`, `D0007`).
- **Avatar-Kürzel als Bild oder Farbe je Person** — das Artboard zeigt Initialen in der Farbe der Art; mehr braucht dieser Slice nicht.
- **Live-Übertragung an andere offene Sichten** (`I0028`): legt ein Agent an, sieht ein offener Browser es erst beim nächsten Laden.
- **Löschen eines Kontributors.** Die WBS kennt dafür keinen Knoten; stillgelegt wird in `I0009`, gelöscht wird nicht.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Neben den Entscheidungen unter „Offene Fragen" stehen fünf Annahmen mit Beleg:

1. **Der Name wird geprüft, aber nicht umgeschrieben.** „Leer" heißt leer nach dem Trimmen (`string.IsNullOrWhiteSpace`), gespeichert wird der Name so, wie er ankommt — genau wie beim Boardnamen (`Source/KanbanC.BL/Operations/Boards/Boardname.cs:12-15`).
2. **Kein Feldname `Kontributorart` im DTO.** Die Spalte heißt `Kontributorart`, wie `B0138` sie festlegt; das DTO-Feld heißt `Art`, weil der Typ den Kontext schon trägt — dieselbe Aufteilung wie `Board.Art` zu `BoardArt` (`Source/KanbanC.Contracts/Boards/Board.cs:6`).
3. **`POST /api/kontributoren` antwortet mit 201 und `Location`**, wie `POST /api/boards` (`Source/KanbanC.WebApi/Endpunkte/BoardEndpunkte.cs:32`) — die Route ist die Wurzelressource, nicht die des einzelnen Kontributors, weil es `GET /api/kontributoren/{id}` in diesem Slice noch nicht gibt.
4. **Die Seite lädt nach dem Anlegen die ganze Liste neu**, statt den neuen Kontributor lokal einzusortieren. Die Reihenfolge gehört der Abfrage (`B0140`), und eine zweite Sortierung in der Oberfläche wäre eine zweite Wahrheit.
5. **Der Punkt „Auswertungen" bleibt gesperrt.** Nur „Kontributoren" wird entsperrt; der geänderte `RahmenE2ETests` muss weiter beweisen, dass es einen gesperrten Punkt gibt (`Source/KanbanC.PlaywrightTests/Tests/RahmenE2ETests.cs:55`).

Wer eine dieser Annahmen anders will, ändert sie vor dem Bauen — nach `B0139` kostet die Feldfrage eine zweite Migration.
