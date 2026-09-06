---
id: R00023
status: Neu
datum: 2026-09-06
---

# R00023: Karte einer Klasse zuordnen

## Beschreibung

Eine Karte bekommt eine **Kartenklasse** — und damit in derselben Handlung die **nächste Nummer** dieser Klasse: `WBS-32`, `BUG-08`. Gewählt wird das Feld „Klasse" im Eigenschaftenblock der Kartenseite oder über `PUT /api/karten/{karteId}/kartenklasse`; die vergebene Nummer steht danach auf der Kartenseite und als Plakette auf der Karte in der Bahn. Die Wahl „ohne Klasse" löst die Zuordnung wieder.

Zahlt ein auf: [Vision](R00000-vision.md) — „Karten mit der Ausdruckskraft von Kanbanflow — und darüber hinaus einer optionalen Klasse. Eine Klasse fasst zusammengehörige Karten (etwa alle aus der WBS) und vergibt eine eigene, klassenspezifische Nummerierung, so dass ein Agent über die API gezielt das richtige Set greift statt des ganzen Boards."

**Was `R00022` gebaut hat und dieser Slice benutzt:** den Nummernkreis samt geführtem Zählerstand. `R00022` legt ihn auf 0 und schreibt ihn nie fort — **hier wächst er.** Was dieser Slice noch nicht tut: das gezielte Abrufen der Karten einer Klasse ist `I0022`, der WBS-Import `I0030`.

**Dies ist der zweite Slice von `D0005` „Karten-Klassen".**

## Geschäftlicher Nutzen

`R00022` hat den Nummernkreis gebaut, aber noch keine Nummer vergeben — die Zusage der Vision, dass ein Agent „gezielt das richtige Set greift statt des ganzen Boards", ist damit halb eingelöst: der Träger existiert, die Karten hängen noch nicht daran. Dieser Slice schließt die Lücke und liefert zugleich das, was eine Kartennummer erst brauchbar macht: eine **Identität, die außerhalb der Anwendung trägt**. `WBS-32` kann in einen Zweignamen, eine Commit-Nachricht oder den Prompt eines Agenten wandern und dort noch dasselbe meinen — vorausgesetzt, die Nummer wiederholt sich nie. Genau diese Zusage ist der Kern dieses Slice, und sie ist der Grund, warum Erhöhen und Zuordnen in einer Transaktion stehen und ein eindeutiger Index sie zusätzlich im Schema festnagelt.

Ohne diesen Slice bleiben `I0022` (Karten einer Klasse abrufen), `I0030` (WBS-Datei importieren) und `I0038` (Board exportieren) unerreichbar — alle drei nennen `I0021` in ihrer Spalte `Braucht`.

## Funktionale Anforderungen

- Eine Karte wird über `PUT /api/karten/{karteId}/kartenklasse` einer Kartenklasse zugeordnet; die Antwort ist das **ganze Kartendetail** mit HTTP 200.
- Mit der Zuordnung vergibt die Kartenklasse ihre **nächste** Nummer und ihr **Zählerstand wächst um 1** — beides in **einer** Transaktion.
- Eine Karte trägt **höchstens eine** Kartenklasse; eine zweite Zuordnung ist ein **Wechsel**, keine zusätzliche Klasse.
- Ein Wechsel vergibt eine **neue** Nummer aus der neuen Kartenklasse; die alte Nummer **verfällt** und der Zählerstand der alten Kartenklasse **fällt nicht zurück**.
- **Dieselbe** Kartenklasse erneut zu wählen verbraucht **keine** Nummer: Zuordnung und Zählerstand bleiben, wie sie sind.
- Ein leeres Feld (`null`) **löst** die Zuordnung; die Nummer verfällt, der Zählerstand bleibt stehen. Eine Karte ohne Zuordnung zu lösen ist **kein** Fehler.
- Eine verfallene Nummer wird **nie wieder** vergeben — der Zählerstand wächst nur.
- Die vergebene Nummer reist als `Kartennummer` **an der Karte** mit und steht damit überall, wo eine Karte steht, auch in der Boardantwort.
- Das `Kartendetail` trägt die zugeordnete `Kartenklasse` als ganzes DTO; `null` heißt „ohne Klasse".
- Der Eigenschaftenblock der Kartenseite zeigt ein Auswahlfeld „Klasse" mit den Kartenklassen **dieses Boards** und dem Eintrag „ohne Klasse"; die Liste kommt über `GET /api/boards/{boardId}/kartenklassen`.
- Die Karte in der Bahn zeigt ihre Nummer als Plakette über dem Titel; eine Karte ohne Klasse zeigt nur ihren Titel.
- Eine **unbekannte Karte**, eine **unbekannte Kartenklasse** und eine Kartenklasse **eines fremden Boards** werden mit Befund zurückgewiesen.

## Nicht-funktionale Anforderungen

- **Die Nummer darf sich nie wiederholen — auch nicht unter zwei gleichzeitigen Schreibern.** Erhöhen des Zählerstands und Schreiben der Zuordnung stehen in **einer** Transaktion; `UNIQUE(Kartenklasse, Zaehlerstand)` sichert die Zusage im Schema gegen jeden Weg, der am Dienst vorbeischreibt. Dasselbe Verhältnis wie bei `UX_Kartenklasse_Board_Praefix` in `016`: der lesbare Befund entsteht davor im Dienst, der Index ist das Netz darunter.
- **Paralleler SQLite-Schreibzugriff ist im ganzen Repository noch nie geprüft worden.** Der nebenläufige Fall bekommt deshalb einen **eigenen** Test (`B0307`), keinen Nebensatz in einem anderen. Fällt er, ist die Antwort nicht „schätzen", sondern der neue Zuschnitt der Vergabe — Haltung nach Skill `dependency-probe`.
- **Begriff (C06):** Der Gegenstand heißt im ganzen Stack **`Kartenklasse`**, die vergebene Nummer **`Kartennummer`**, der Zähler **`Zaehlerstand`**. Die **Beschriftung in der Oberfläche** bleibt **„Klasse"** — dieselbe Trennung wie in `R00022`: die Beschriftung folgt der Lesart am Ort, der Bezeichner der Eindeutigkeit im Stack.
- **Die Id-Konvention verbietet `Nummer` als Feldnamen** (Primärschlüssel `<Tabelle>Id`, Fremdschlüssel nach der referenzierten Tabelle). Der fachliche Nummernbegriff heißt deshalb `Kartennummer` und nicht `Kartenummer`/`Nummer` — er ist keine Schlüsselangabe, sondern eine formatierte Zeichenkette. C07: Bezeichner ohne echte Umlaute, UI-Texte und Kommentare mit.
- **C08:** `KartenklasseZuordnenAnfrage` ist ein immutable Record in `KanbanC.Contracts`.
- **Datenhaltung:** `017-kartenklassenzuordnung.sql` ist eine **eigene Tabelle** mit `CREATE TABLE IF NOT EXISTS` — **kein `ALTER TABLE Karte ADD COLUMN`**. Der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal; ein `ALTER TABLE` scheiterte im zweiten Lauf, und die bestehende `CREATE TABLE IF NOT EXISTS` aus `003` wächst nicht nachträglich um eine Spalte. Neuntes Mal nach `B0108`, `B0126`, `B0184`, `B0201`, `B0220`, `B0255`, `B0267`, `B0283`.
- **Abgelegt wird der Zählerstand, nicht die fertige Nummer.** Gebildet wird sie überall aus `Kartennummer.Aus(Praefix, Zaehlerstand)` — eine zweite abgelegte Schreibweise derselben Regel liefe beim ersten Sonderfall auseinander.
- **Fehlerantworten für Agenten:** jeder Befund nennt Grund **mit Werten** und die **Kompensationsaktion**, auch der 404 (Projektregel, `Nichtgefunden`).
- **Die Kernregel bleibt:** `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL`; die Oberfläche spricht ausschließlich HTTP.

## Akzeptanzkriterien

### Die Zuordnung vergibt die nächste Nummer

- [ ] `PUT /api/karten/{karteId}/kartenklasse` mit `{ "kartenklasse": <KartenklasseId> }` ordnet die Karte zu und antwortet mit HTTP 200 und dem **ganzen Kartendetail**.
- [ ] Die vergebene Nummer ist `Praefix` + `Zaehlerstand + 1`, mindestens zweistellig: Kartenklasse `WBS-` auf Stand **31** ergibt **`WBS-32`**; eine frische Kartenklasse auf Stand **0** ergibt **`WBS-01`**.
- [ ] Der `Zaehlerstand` der Kartenklasse steht danach auf dem vergebenen Wert — nach `WBS-32` auf **32**; `GET /api/boards/{boardId}/kartenklassen` zeigt „32 vergeben · nächste WBS-33".
- [ ] Erhöhen und Zuordnen geschehen in **einer** Transaktion: schlägt eines fehl, ist weder der Zählerstand gewachsen noch eine Zuordnung geschrieben.
- [ ] Die Zuordnung überlebt Reload und Neustart der WebApi.

### Wechsel, erneute Wahl und Lösen

- [ ] **Wechsel:** Karte trägt `WBS-32` (Kartenklasse `WBS-` auf Stand 32), Zuordnung zu `BUG-` (Stand 7) → die Karte trägt **`BUG-08`**, `BUG-` steht auf **8**, `WBS-` bleibt auf **32**. Es gibt genau **eine** Zuordnungszeile.
- [ ] **`WBS-32` wird nie wieder vergeben:** die nächste Zuordnung zu `WBS-` ergibt `WBS-33`.
- [ ] **Dieselbe Kartenklasse erneut:** Karte trägt `WBS-32`, erneute Zuordnung zu derselben Kartenklasse → die Nummer bleibt **`WBS-32`**, der Zählerstand bleibt **32**. Es wird **keine** Nummer verbraucht.
- [ ] **Lösen:** `{ "kartenklasse": null }` entfernt die Zuordnung; `Karte.Kartennummer` ist danach `null`, `Kartendetail.Kartenklasse` ist `null`, der Zählerstand der Kartenklasse bleibt **unverändert**.
- [ ] Eine Karte **ohne** Zuordnung zu lösen ist **kein** Fehler: HTTP 200, das Ziel ist erreicht.
- [ ] Die Folge **zuordnen → lösen → erneut zuordnen** am selben Board ergibt `WBS-32` → keine Nummer → **`WBS-33`**: der Zählerstand fällt nicht zurück.

### Die Nummer wiederholt sich nie — auch unter zwei Schreibern

- [ ] Zwei **nebenläufige** Zuordnungen auf dieselbe Kartenklasse (Stand 31) ergeben **zwei verschiedene** Zählerstände (32 und 33); keine Nummer entsteht zweimal.
- [ ] Greift die Serialisierung nicht, schlägt `UNIQUE(Kartenklasse, Zaehlerstand)` **sichtbar** an, statt still eine zweite `WBS-32` danebenzuschreiben.
- [ ] Der eindeutige Index selbst wird geprüft: ein direkter zweiter `INSERT` mit demselben Paar `(Kartenklasse, Zaehlerstand)` scheitert an der Datenbank.

### Höchstens eine Kartenklasse je Karte

- [ ] Eine Karte hat nie zwei Zuordnungen; `UNIQUE(Karte)` trägt die Regel ins Schema.
- [ ] Ein direkter zweiter `INSERT` auf dieselbe `Karte` scheitert an der Datenbank.

### Die Nummer ist sichtbar

- [ ] `Karte` trägt genau **ein** neues Feld `string? Kartennummer`; es steht überall, wo eine Karte steht — auch in der Boardantwort, so dass ein Agent die Nummer **ohne zweiten Aufruf** sieht.
- [ ] `Kartendetail` trägt die zugeordnete `Kartenklasse` als ganzes DTO (Name, Präfix, Zählerstand); `null` heißt „ohne Klasse".
- [ ] Die Karte in der Bahn zeigt die Nummer als Plakette **über** dem Titel; eine Karte ohne Klasse zeigt nur ihren Titel, und die Stelle kostet keine Zeile.
- [ ] Der Eigenschaftenblock der Kartenseite zeigt das Feld „Klasse" mit den Kartenklassen dieses Boards, dem Eintrag „ohne Klasse" und der Hinweiszeile „Vergibt beim Speichern `<nächste Nummer>`".
- [ ] Ein Board **ohne** Kartenklasse zeigt an dieser Stelle einen Satz statt eines leeren Auswahlfeldes — Wortlaut und Form wie `#keine-klassen` in `Klassenpflege.razor:16`.
- [ ] Die Klassenliste des Feldes kommt über `GET /api/boards/{boardId}/kartenklassen` mit dem `Board` aus dem Kartendetail — **das Kartendetail wächst nicht um eine siebte Liste.**

### Fehlerantworten für Agenten

- [ ] **Unbekannte Karte** → HTTP 404 mit `karte-unbekannt`, Grund mit der Nummer und Kompensationsaktion.
- [ ] **Unbekannte Kartenklasse** → HTTP 404 mit `kartenklasse-unbekannt`, Grund mit der Nummer und der Kompensationsaktion `GET /api/boards/{boardId}/kartenklassen`.
- [ ] **Kartenklasse eines fremden Boards** → eigener Befund `kartenklasse-fremd` nach dem Muster `karte-fremd`/`spalte-fremd`: eine Kartenklasse gehört **einem** Board, und die eines anderen ist an dieser Karte keine.
- [ ] Nach einer Zurückweisung wurde **nicht geschrieben**: kein Zählerstand gewachsen, keine Zuordnung angelegt oder verändert.
- [ ] `FehlervertragTests` deckt die neue Route ab; sie steht **nicht** auf `RoutenOhneFehlerantwort`.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] `Kartendetailvergleich` wächst um das Glied `Kartenklasse` — sonst prüfte er zwei Details still als gleich, die es in der neuen Angabe nicht sind.
- [ ] `LayoutModusE2ETests` mit seinen 17 `ToHaveCountAsync`-Zusagen ist nach dem Lauf **unverändert grün**.
- [ ] `KindZiehbarkeitProbeE2ETests` und `VerweisInZiehbarerKarteProbeE2ETests` bleiben grün: die Plakette steht **über** dem Titelverweis, nicht in ihm.
- [ ] Die E2E-Suiten aus `R00001`–`R00022` laufen unverändert weiter.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/017-kartenklassenzuordnung.sql` — neue, idempotente Migration; Tabelle `Kartenklassenzuordnung` mit `KartenklassenzuordnungId` als Primärschlüssel, `Karte` und `Kartenklasse` als Fremdschlüssel (nach den referenzierten Tabellen benannt, Projektregel), `Zaehlerstand INTEGER NOT NULL`, ein Index auf `Karte`, ein **eindeutiger** Index auf `Karte` und ein **eindeutiger** Index auf `(Kartenklasse, Zaehlerstand)`.
- **Contracts:** `Source/KanbanC.Contracts/Klassen/KartenklasseZuordnenAnfrage.cs` (neu); `Source/KanbanC.Contracts/Karten/Karte.cs` und `Kartendetail.cs` wachsen um je **ein** Glied.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Klassen/KartenklassenRepository.cs` (`OrdneZu`, `LoeseZuordnung`), `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (`LEFT JOIN` auf `Kartenklassenzuordnung` und `Kartenklasse`), `Source/KanbanC.BL/Interfaces/Klassen/IKartenklassenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `OrdneKartenklasseZu`; `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` — zwei Codes mehr.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — eine Route mehr; `Source/KanbanC.WebApi/Program.cs` (Dienstsammlung, falls der `KartenService` eine Abhängigkeit dazubekommt).
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`OrdneKartenklasseZu`), `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` (`:429`, das Eigenschaftenblatt), `Source/KanbanC.Blazor/Components/Karten/Karte.razor(.css)` (die Plakette).
- **Unberührt:** `Source/KanbanC.Blazor/Components/Klassen/Klassenpflege.razor`, `Source/KanbanC.WebApi/Endpunkte/KartenklassenEndpunkte.cs`, `Source/KanbanC.Contracts/Klassen/Kartenklasse.cs`, `Source/KanbanC.Contracts/Klassen/Kartennummer.cs` — **Route und Klient der Kartenklassen aus `R00022` werden hier nur benutzt, nicht erweitert.**
- **Tests:** `Source/KanbanC.BL.Tests/` (`Integrations/Karten/KartenServiceTests.cs`, `TestHelpers/TestKartenklassenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenApiKlientTests.cs`), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/Klassen/KartenklassenRepositoryTests.cs`, `Persistenz/Klassen/KartenklassenzuordnungNebenlaeufigTests.cs` (neu), `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`, `Infrastructure/Kartendetailvergleich.cs`), `Source/KanbanC.PlaywrightTests/` (`PageObjects/KartendetailSeite.cs`, `PageObjects/BoardSeite.cs`, `Infrastructure/WebApiKlient.cs`, neue Testklasse `KarteEinerKlasseZuordnenE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0005.dc.html`](../Dokumentation/Wireframes/D0005.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gilt daraus **Zustand 4** (`:281-345`): das Feld „Klasse" im Eigenschaftenblock der Kartenseite und die Plakette auf der Karte in der Bahn, jeweils als Ausschnitt — die beiden Schirme selbst bleiben ihren Artboards `D0004` und `D0003`. Der Platz auf der Kartenseite steht dort seit dem 2026-09-05 als gestrichelter Kasten „Klasse und Nummer" reserviert. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Zwei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Route heißt `…/kartenklasse`, nicht `…/klasse`.** Das Artboard schreibt `PUT /api/karten/14/klasse` (`:340`). Gebaut wird `…/kartenklasse` — **dieselbe Entscheidung wie bei `…/kartenklassen` in `R00022`/`B0300`**: C06 verlangt **einen** Begriff in **einer** Schreibweise, und die Route ist eine der Stellen, an denen der Bezeichner steht, nicht eine Beschriftung. Die **Beschriftung im Schirm bleibt „Klasse"**, wie gezeichnet. Ein Artboard ist Entwurf, kein Vertrag.
2. **Ein `PUT` statt `PUT` + `DELETE`.** Das leere Feld (`null`) löst die Zuordnung; es gibt **kein eigenes `DELETE`**. Begründung: es ist dieselbe Handlung an derselben Stelle — „welche Klasse trägt diese Karte" —, und ein Agent müßte sonst zwei Adressen für **eine** Frage kennen. Das Artboard zeichnet den Erstfall und entscheidet den Wechsel nicht (`:337`, „was I0021 offen lässt"); die Entscheidung fällt hier.

Alles andere am Ausschnitt folgt der Skizze: die Plakette ist dieselbe `.tag`-Form, die `D0001` und `D0003` schon zeigen (`WBS-31`, `WBS-28`, `WBS-09`) und die `Klassenpflege.razor:25` für das Präfix verwendet; Gestaltungswerte kommen aus `gestaltung.css`, kein Literal in der Komponenten-CSS.

### Ablauf

1. **Karte einer Kartenklasse zuordnen** (`PUT /api/karten/{karteId}/kartenklasse`, Rumpf `{ "kartenklasse": 5 }`)
   - 1.1 `KartenService.OrdneKartenklasseZu(karteId, anfrage)` liest zuerst das Kartendetail — es liefert `Board`
     - 1.1.1 Karte unbekannt → `Nichtgefunden.Karte(karteId)` → HTTP 404 mit Rumpf
   - 1.2 `anfrage.Kartenklasse is null` → weiter bei Schritt 2 (lösen)
   - 1.3 Kartenklasse prüfen — **im Dienst, nicht in einem Validator**: beide Regeln brauchen den Bestand
     - 1.3.1 `Kartenklassenleser.LiesKartenklasseDesBoards(…, board, kartenklasseId)` liefert `null` → gibt es die Kartenklasse überhaupt? → `kartenklasse-unbekannt` bzw. `kartenklasse-fremd`
     - 1.3.2 Bei Befund: HTTP 404 mit Rumpf, **kein** Schreibzugriff
   - 1.4 `KartenklassenRepository.OrdneZu(karteId, kartenklasseId)` in **einer** Transaktion
     - 1.4.1 Trägt die Karte **dieselbe** Kartenklasse schon → **nichts** ändern, bestehenden Zählerstand zurückgeben
     - 1.4.2 Sonst: `UPDATE Kartenklasse SET Zaehlerstand = Zaehlerstand + 1 WHERE KartenklasseId = … RETURNING Zaehlerstand`
     - 1.4.3 Zuordnungszeile schreiben bzw. die bestehende ersetzen (`UNIQUE(Karte)` erzwingt die Einzahl); der Zählerstand der **alten** Kartenklasse bleibt stehen
   - 1.5 Kartendetail neu lesen, HTTP 200
2. **Zuordnung lösen** (`{ "kartenklasse": null }`)
   - 2.1 `KartenklassenRepository.LoeseZuordnung(karteId)` — Zeile weg, Zählerstand **unverändert**
   - 2.2 Karte ohne Zuordnung → kein Fehler, das Ziel ist erreicht
   - 2.3 Kartendetail neu lesen, HTTP 200
3. **Die Nummer lesen** (`Kartenleser`)
   - 3.1 `LEFT JOIN Kartenklassenzuordnung` auf `Karte`, `LEFT JOIN Kartenklasse` darauf
   - 3.2 `Karte.Kartennummer` = `Kartennummer.Aus(Praefix, Zaehlerstand)`, `null` ohne Zuordnung
   - 3.3 `Kartendetail.Kartenklasse` = das ganze `Kartenklasse`-DTO, `null` ohne Zuordnung
4. **Das Feld im Eigenschaftenblock** (Oberfläche)
   - 4.1 `Kartendetail.razor` lädt beim Öffnen zusätzlich `KartenklassenApiKlient.LadeKartenklassen(_detail.Board)`
   - 4.2 Auswahlfeld mit „ohne Klasse" + den Kartenklassen des Boards; Präfixplakette und Hinweiszeile „Vergibt beim Speichern `<Nummer>`"
   - 4.3 Board ohne Kartenklasse → ein Satz statt eines leeren Auswahlfeldes
   - 4.4 Zweiter Aufruf heißt zweiter Fehlerpfad: Ausfall über `WebApiAufruf.MitAusfallmeldung`
5. **Die Plakette in der Bahn** (`Karte.razor`)
   - 5.1 `Kartendaten.Kartennummer` ist gesetzt → `.tag` **über** dem Titelverweis
   - 5.2 `null` → nichts; die Stelle kostet keine Zeile

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`Migrationslaeufer`** — die siebzehnte Migration reiht sich ein; kein Journal, also idempotent, und **eine eigene Tabelle**.
- **`KartenEndpunkte`** (`:60-79`) — eine Route mehr, boardlose Unterressource wie Teilaufgaben, Kommentare, Anhänge und Dateiverweise.
- **`Kartenleser`** (`:93`) — die eine Stelle, an der ein `Kartendetail` entsteht.
- **`Kartendetail.razor`** (`:429`) — das `eigenschaftenblatt`, in dem Verantwortlicher, Fälligkeit und Etiketten stehen.
- **`Karte.razor`** (`:33`) — der Titelverweis, über den die Plakette kommt.

**Klassen-Entwurf:**

- `KartenklasseZuordnenAnfrage` (DTO, immutable) — genau eine Angabe. `null` heißt „ohne Klasse".
  - `record KartenklasseZuordnenAnfrage(long? Kartenklasse)`
- `Kartenklassenzuordnung` (DTO, immutable, `KanbanC.BL/Models`) — die geschriebene Zeile mit dem vergebenen Zählerstand. Reist **nicht** in die Contracts: nach außen sichtbar ist die fertige Nummer, nicht der Stand.
  - `record Kartenklassenzuordnung(long KartenklassenzuordnungId, long Karte, long Kartenklasse, int Zaehlerstand)`
- `IKartenklassenRepository` (Interface, wächst) — zwei Methoden mehr, damit der Dienst gegen `TestKartenklassenRepository` prüfbar bleibt.
  - `Kartenklassenzuordnung? OrdneZu(long karteId, long kartenklasseId)`
  - `bool LoeseZuordnung(long karteId)`
- `KartenklassenRepository` (Provider, Integration nach Hausregel) — **hier wächst der Zählerstand**. Erhöhen und Zuordnen in **einer** Transaktion; die drei Fälle (erstmalig, Wechsel, dieselbe Klasse erneut) stehen unter demselben Schloss. `null` bei unbekannter Karte oder unbekannter Kartenklasse.
- `KartenService.OrdneKartenklasseZu` (Integration, orchestriert) — liest das Detail, prüft die Kartenklasse gegen das Board der Karte, verzweigt auf `OrdneZu` oder `LoeseZuordnung`, liest neu. IOSP: geprüft und geschrieben wird darunter.
  - `Ergebnis<Kartendetail> OrdneKartenklasseZu(long karteId, KartenklasseZuordnenAnfrage anfrage)`
- **Kein Validator** — es gibt nichts syntaktisch zu prüfen; beide Regeln brauchen den Bestand und sitzen deshalb im Dienst, wie der Urheberbefund in `B0287` und aus demselben Grund, aus dem `B0111` keinen bekam.
- `Nichtgefunden` (Operation, wächst) — zwei Fabrikmethoden mehr, beide mit Grund **und** Kompensationsaktion.
  - `static Fehlerbefund Kartenklasse(long kartenklasseId)` — Code `kartenklasse-unbekannt`
  - `static Fehlerbefund KartenklasseFremd(long boardId, long kartenklasseId)` — Code `kartenklasse-fremd`, Muster `karte-fremd`/`spalte-fremd`
- `KartenApiKlient.OrdneKartenklasseZu` (Integration in der Oberflächenschicht) — Muster der übrigen `PUT`-Aufrufe, JSON in beide Richtungen.
  - `Task<ApiErgebnis<Kartendetail>> OrdneKartenklasseZu(long karteId, KartenklasseZuordnenAnfrage anfrage)`

### Änderungen an bestehenden Klassen

- `Karte` (Contracts) — **ein** Feld mehr: `string? Kartennummer`. Es reist an der Karte mit und steht damit überall, wo eine Karte steht, auch in der Boardantwort — derselbe Grund, aus dem Beschreibung, Fälligkeit und Farbe dort stehen; ein Agent sieht die Nummer ohne zweiten Aufruf, und die Bahn zeichnet sie. **Keine `KartenklasseId` an `Karte`:** das Präfix in der Nummer sagt lesbar, welche Klasse trägt, den gezielten Zugriff auf das Set liefert `I0022`, und ein zweites Feld für dieselbe Auskunft wäre **52 positionale `new Karte(…)` in 9 Dateien** teuer.
- `Kartendetail` (Contracts) — **ein** Feld mehr: `Kartenklasse? Kartenklasse`, das ganze DTO, wie `Verantwortlicher` den ganzen `Kontributor` trägt. Damit zeigt das Feld im Eigenschaftenblock Name, Präfix und nächste Nummer ohne zweite Abfrage. **Keine siebte Liste** — die Klassenliste des Boards kommt über die Leseroute aus `R00022`.
- `Kartenleser` (`:93`) — der `SELECT` bekommt zwei `LEFT JOIN`; die eine Stelle, an der `new Kartendetail(` steht, wächst mit.
- `KartenEndpunkte` — `MapPut(Kartenklassenroute, OrdneKartenklasseZu)`; `FehlervertragTests` zieht **in derselben Bubble** mit, weil der Test rot wird, sobald eine Route ohne Vertragsfall registriert ist (`FehlervertragTests.cs:53-56`).
- `Kartendetailvergleich` (`:30`) — ein Glied mehr; sonst prüft der Helfer die neue Angabe stillschweigend nicht.
- `KartendetailSeite`, `BoardSeite`, `WebApiKlient` (PlaywrightTests) — Locator und Aufbauhilfen kommen dazu, bestehende bleiben unangetastet.
- **`Kartenklasse.cs` und `Kartennummer.cs` werden nicht angefasst** — beide stehen seit `R00022` und werden hier nur benutzt.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KartenService.OrdneKartenklasseZu` gegen `TestKartenklassenRepository` — unbekannte Karte liefert den Befund und **schreibt nicht**; unbekannte Kartenklasse liefert `kartenklasse-unbekannt`; eine Kartenklasse eines **anderen** Boards liefert `kartenklasse-fremd` und **nicht** `kartenklasse-unbekannt` (die Unterscheidung ist die ganze Pointe des zweiten Codes); `null` im Feld führt auf `LoeseZuordnung` und nicht auf `OrdneZu`; nach jeder Zurückweisung wurde **nicht geschrieben**; der Erfolg liefert das **neu gelesene** Detail.
- `Nichtgefunden.Kartenklasse` / `KartenklasseFremd` — Grund nennt die Nummern, Kompensation nennt `GET /api/boards/{boardId}/kartenklassen`; beide Codes stehen in `AlleCodes`.
- `KartenApiKlient.OrdneKartenklasseZu` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 404 die Zurückweisung mit Befund; Methode, Adresse und Rumpf werden mitgeprüft, insbesondere **dass die Adresse `kartenklasse` und nicht `klasse` lautet** und dass `null` als `null` im Rumpf steht (nicht als fehlendes Feld). Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses Testprojekt gibt (`CLAUDE.md`, Abweichung 4).

**Integration:** `KartenklassenRepository.OrdneZu` / `LoeseZuordnung` und der erweiterte `Kartenleser` gegen eine `TemporaereDatenbank` — erstmalige Zuordnung vergibt `Zaehlerstand + 1` und erhöht die Klasse; **Wechsel** ersetzt die Zeile und lässt den alten Zählerstand stehen; **dieselbe Klasse erneut** ändert nichts und verbraucht **keine** Nummer; **Lösen** entfernt die Zeile und lässt den Zählerstand stehen; **zuordnen → lösen → erneut zuordnen** ergibt `WBS-32`, dann `WBS-33`; unbekannte Karte und unbekannte Kartenklasse liefern `null`. **Die Indizes selbst** werden geprüft: ein direkter zweiter `INSERT` mit demselben `(Kartenklasse, Zaehlerstand)` scheitert, ebenso ein zweiter auf dieselbe `Karte`. **Der nebenläufige Fall bekommt einen eigenen Test** (`B0307`): zwei gleichzeitige `OrdneZu` auf dieselbe Kartenklasse ergeben zwei verschiedene Zählerstände — paralleler SQLite-Schreibzugriff ist im ganzen Repository noch nie geprüft worden, und diese eine Zusage zeigt kein Einzeltest. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — 200 (zuordnen, wechseln, erneut dieselbe, lösen, lösen ohne Zuordnung) und 404 (unbekannte Karte, unbekannte Kartenklasse, fremde Kartenklasse) samt Rumpf; `GET /api/boards/{boardId}` trägt die `Kartennummer` an jeder Karte; `FehlervertragTests` ruft die neue Route ab. `WebApiNeustartTests` — Zuordnungen, Nummern und Zählerstände überstehen den Neustart.

**E2E:** Auf der Kartenseite die Klasse „WBS" wählen → die Nummer erscheint, die Karte in der Bahn trägt die Plakette, ein Reload zeigt beides unverändert, und die Klassenzeile im Layout-Modus zählt eins hoch (US-1). Auf „Bugmeldungen" wechseln → neue Nummer aus der neuen Klasse, der Zählerstand von „WBS" bleibt stehen (US-2). „ohne Klasse" wählen → die Plakette verschwindet; erneut „WBS" wählen → **die nächste**, nicht die alte Nummer; der Zählerstand fällt nicht zurück (US-3) — drei Schritte am selben Board, die sichtbare Probe auf die Identitätszusage. Ein Board ohne Kartenklasse zeigt an der Stelle den Satz statt eines leeren Auswahlfeldes (US-4). Dazu laufen die E2E-Suiten aus `R00001`–`R00022` weiter; **`LayoutModusE2ETests` unverändert** und die beiden Ziehproben unverändert sind die Gegenproben.

Repositories, `Kartenleser`, `Kartenklassenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00022`** (Kartenklasse anlegen — `I0020`, grün) und **`R00006`** (Karte anlegen — `I0011`, grün). Das sind genau die beiden Knoten der WBS-Spalte `Braucht` von `I0021`; beide sind erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00017`** (`I0016`, grün — die Kartenseite mit dem Eigenschaftenblock, in dem das Feld sitzt), **`R00012`** (`I0012`, grün — die Karte in der Bahn, auf der die Plakette sitzt), **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css` mit `.tag`). Die Spalte `Braucht` nennt diese nicht — sie führt Vorbedingungen, keine Bauplätze; an Front und Welle ändert es nichts, weil alle grün sind.
- Blockiert: **`I0022`** („Karten einer Klasse abrufen"), **`I0030`** („WBS-Datei importieren") und **`I0038`** („Board exportieren") — alle drei nennen `I0021` in ihrer Spalte `Braucht`.

## Umfang

```
Karte einer Klasse zuordnen (I0021) = 11 Bubbles: 7 Standard (9,2h), 4 unklar (3,2–8,5h).
Rest: 9,2h klar + 3,2–8,5h unklar · 3 von 11 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0021` ist vollständig bis zur Bubble geplant und trägt seine elf Bubbles (`B0304`–`B0314`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: Zuordnen, Wechseln und Lösen teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; getrennt geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0016` bis `I0020`. **Die Requirement-Klammer sitzt deshalb allein an `I0021`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0304` Zuordnungstabelle anlegen | Provider (Migration) | 0,4h (belegt) |
| `B0305` Nächste Nummer vergeben | Provider | 0,4–1,5h (**unklar**) |
| `B0306` Zuordnung lösen | Provider | 0,4h (belegt) |
| `B0307` Zwei gleichzeitige Zuordnungen | Provider (nur Test) | 0,4–1,5h (**unklar**) |
| `B0308` Karte und Kartendetail tragen ihre Nummer | Contracts + Provider | 0,4–1,5h (**unklar**) |
| `B0309` Zuordnung verdrahten | Integration | 0,4h (belegt) |
| `B0310` Endpunkt der Kartenklasse an der Karte | Integration | 2h (Richtwert) |
| `B0311` API-Klient der Zuordnung | Integration | 2h (Richtwert) |
| `B0312` Klassenfeld im Eigenschaftenblock | UI | 2h (Richtwert) |
| `B0313` Nummernplakette auf der Karte in der Bahn | UI | 2h (Richtwert) |
| `B0314` E2E Karte einer Klasse zuordnen | E2E | 2–4h (**unklar**) |

Vier unklare Bubbles sind mehr als bei `I0020` (eine), und die Gründe sind benannt: `B0305` hält drei Fälle unter **einem** Schloss; `B0307` betritt mit parallelem SQLite-Schreibzugriff Neuland, das im Repository nie geprüft wurde; `B0308` zieht **52 positionale `new Karte(…)` in 9 Dateien** und zwei `new Kartendetail(` mit (dieselbe Lage wie `B0171`); `B0314` braucht drei Schritte am selben Board für den Rand „der Zählerstand fällt nicht zurück". Derselbe Vermerk wie bei `I0005` bis `I0020`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles; die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0021` trägt keine eigene Zählzeile; sie hält die vier Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der elf Bubbles gezählt.

## Offene Fragen

- **Was geschieht mit den vergebenen Nummern, wenn ein Präfix je änderbar wird?** — **nicht hier zu entscheiden, aber benannt.** Abgelegt wird der Zählerstand, nicht die fertige Nummer; wäre ein Präfix änderbar, änderten sich **rückwirkend alle** Nummern der Klasse — `WBS-31` hieße plötzlich `PRJ-31`, obwohl `WBS-31` in einem Zweignamen steht. Die Frage gehört einer Interaction „Kartenklasse ändern", die es heute in der WBS **nicht gibt** (`R00022` hat Ändern und Entfernen ausdrücklich out of scope gestellt). **Adresse der Lücke:** `/planung verfeinern D0005`, bevor eine solche Interaction entsteht; dann ist zwischen „Präfix unveränderlich" und „abgelegte Nummer statt Zählerstand" zu wählen. Dieser Slice legt die Entscheidung nicht fest — er macht sie nur fällig.
- **Soll die Kartenseite eine Zuordnung sofort speichern oder erst mit „Speichern"?** — **entschieden: sofort**, wie Verantwortlicher und Fälligkeit im selben Eigenschaftenblock. Die Hinweiszeile des Artboards sagt „Vergibt beim Speichern WBS-32" (`:311`), meint aber die Wahl selbst; ein eigener Speicherknopf für **ein** Feld wäre ein Bruch mit den Nachbarfeldern. Nicht am Menschen geprüft.
- **Soll ein Wechsel gewarnt werden, weil die alte Nummer verfällt?** — **entschieden: nein**, keine Rückfrage. Der Verfall ist die Regel, nicht der Unfall, und eine Bestätigung an einem Auswahlfeld, das direkt neben Fälligkeit und Verantwortlichem sitzt, wäre der einzige solche Fall auf dem Schirm. Fiele die Entscheidung anders, käme sie in der Oberfläche dazu und ließe API und Fachlogik unberührt.
- **Wie verhält sich SQLite bei zwei gleichzeitigen Schreibern auf derselben Datei?** — **unbelegt, deshalb `B0307` als eigener Test.** Ob die Serialisierung über `BEGIN IMMEDIATE`, über einen Wiederholungsversuch oder über den eindeutigen Index als Netz kommt, entscheidet der Bau. Fällt der Test, ist die Antwort der neue Zuschnitt von `B0305`, nicht eine Schätzung.
- **Soll die Boardantwort auch das Präfix je Karte tragen?** — **entschieden: nein.** `Karte.Kartennummer` trägt es bereits sichtbar in sich (`WBS-32`); ein zweites Feld wäre dieselbe Auskunft zweimal.
- ~~Kann eine Karte mehr als eine Kartenklasse tragen?~~ — **entschieden: nein, höchstens eine** (`UNIQUE(Karte)`). Vision `R00000-vision.md:52` spricht von der Karte „mit optionaler Klasse" in der **Einzahl**, das Fertig-Kriterium von „**der** Nummer" in der Einzahl. Mehrere Klassen machten die sichtbare Nummer mehrdeutig; das Querschneiden leisten die Etiketten aus `I0015`.
- ~~Was geschieht beim Wechsel mit der alten Nummer?~~ — **entschieden: sie verfällt und wird nie wieder vergeben.** Der Zählerstand wächst nur; Migration `016` sagt das wörtlich voraus.
- ~~Verbraucht die erneute Wahl derselben Klasse eine Nummer?~~ — **entschieden: nein.** Sonst frisst jedes versehentliche Speichern einen Nummernkreis.
- ~~Braucht das Lösen ein eigenes `DELETE`?~~ — **entschieden: nein**, das leere Feld im `PUT` genügt. Begründung unter „Gestaltungsvorgabe".
- ~~Woher kommt die Klassenliste der Kartenseite?~~ — **entschieden: über `GET /api/boards/{boardId}/kartenklassen`** mit dem `Board` aus dem Kartendetail. Route und Klient stehen seit `R00022`.

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration `017` läuft bei jedem Start des `KanbanC.WebApi` mit.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine halb eingelöste Zusage: `R00022` hat den Nummernkreis gebaut, aber keine Karte hängt daran — ein Agent kann heute weder „gib mir alle WBS-Karten" fragen noch `WBS-32` in einen Zweignamen schreiben, weil es keine `WBS-32` gibt. Die Kausalkette: **wenn** das Zuordnen einer Karte den Zählerstand ihrer Kartenklasse in derselben Transaktion erhöht und die vergebene Zahl als Zuordnung festschreibt (X), **dann** trägt jede zugeordnete Karte eine Nummer, die es genau einmal gab und nie wieder geben wird (Y), **und dann** kann `I0022` genau dieses Set liefern, `I0030` eine WBS-Datei mit wiederfindbaren Nummern einlesen und ein Mensch `WBS-32` in einer Commit-Nachricht verwenden, ohne dass sie später auf eine andere Aufgabe zeigt (Z). **Der Hebel liegt bei der Transaktion und dem eindeutigen Index, nicht beim Auswahlfeld**: die sichtbare Wahl ist der einfache Teil, aber zwischen „nächste Nummer lesen" und „Nummer vergeben" ein Fenster zu lassen, hieße zwei Karten dieselbe Identität zu geben — und eine Identität, die zweimal vorkommt, ist keine. Und der Hebel liegt genau hier und nicht später: würde die Nummer erst beim Abrufen (`I0022`) oder beim Export (`I0038`) gerechnet, wäre sie eine Ansicht statt einer Zusage und änderte sich mit jedem Umsortieren.

## Missing-Docs

- **Paralleler Schreibzugriff auf eine SQLite-Datei aus zwei Verbindungen:** Im Repository nirgends belegt. Ob `BEGIN IMMEDIATE`, ein `busy_timeout` oder WAL gebraucht wird, damit zwei gleichzeitige `OrdneZu` sauber serialisieren, ist offen — deshalb `B0307` als eigener Test statt einer Annahme. Der Befund gehört nach dem Bau festgehalten, weil jeder spätere schreibende Slice davon erbt.
- **`UPDATE … SET Zaehlerstand = Zaehlerstand + 1 RETURNING …` mit Dapper in einer Transaktion:** Schon in `R00022` als „relevant erst für `I0021`" benannt. Ob `RETURNING` den **neuen** Stand zuverlässig liefert und wie Dapper ihn abholt (`ExecuteScalar` gegen `QuerySingle`), ist unbelegt.
- **Die Fehlerform einer Verletzung eines eindeutigen Index in SQLite:** `R00022` hat sie als offen vermerkt und brauchte sie nicht, weil der Validator vorher abfing. Hier wird sie **gebraucht**: `B0307` verlangt, dass die Verletzung *sichtbar* anschlägt statt still danebenzuschreiben, und dafür muss die Form belegt sein.

## Notizen

### Warum die Nummer der Zuordnung gehört und nicht der Karte

Eine Karte kann ihre Klasse wechseln; eine Nummer kann das nicht. Hinge die Nummer an der Karte, müsste beim Wechsel entschieden werden, ob sie mitwandert (dann stünde `WBS-32` in der Klasse „Bugmeldungen") oder umgeschrieben wird (dann zeigte `WBS-32` in einer alten Commit-Nachricht ins Leere). Hängt sie an der **Zuordnung**, ist der Fall trivial: die Zuordnung endet, die Nummer endet mit ihr, und weil der Zählerstand nur wächst, wird sie nie an etwas anderes vergeben. Die Karte behält lesbar, was sie **jetzt** ist; was sie einmal war, ist bewusst keine Frage dieses Slice (siehe „Bewusst out of scope").

### Verworfene Alternativen

- **`ALTER TABLE Karte ADD COLUMN Kartenklasse` statt eigener Tabelle** — scheitert am `Migrationslaeufer`, der jedes Skript bei jedem Start ausführt und kein Journal kennt; im zweiten Lauf bricht der Start. Neuntes Mal dieselbe Entscheidung.
- **`PUT` + eigenes `DELETE` für das Lösen** — zwei Adressen für eine Frage. Ein Agent müßte beide kennen und wüßte nicht, welche die Wahrheit trägt.
- **Die Nummer fertig ablegen statt des Zählerstands** — eine zweite Schreibweise derselben Regel neben `Kartennummer.Aus`; beim ersten Sonderfall (Auffüllbreite, Trenner) liefen sie auseinander. Die Kehrseite — ein änderbares Präfix schriebe alle Nummern rückwirkend um — ist als offene Frage mit Adresse benannt statt hier entschieden.
- **Den Zählerstand aus den zugeordneten Karten rechnen (`MAX`, `COUNT`)** — schon in `R00022` verworfen und hier der eigentliche Prüfstein: er fiele beim ersten Lösen zurück, und die nächste Zuordnung bekäme eine Nummer, die es schon gab.
- **Mehrere Kartenklassen je Karte** — machte die sichtbare Nummer mehrdeutig und widerspräche der Einzahl in Vision und Fertig-Kriterium. Querschneiden leisten die Etiketten.
- **`KartenklasseId` als zweites Feld an `Karte`** — dieselbe Auskunft zweimal, zu 52 positionalen Aufrufstellen in 9 Dateien.
- **Die Klassenliste als siebte Liste am `Kartendetail`** — hätte den Board-Abruf nicht verteuert (das Detail ist eine Einzeladresse), aber eine Liste dupliziert, für die es seit `R00022` eine eigene Route gibt. Anders als bei den `Etikettvorschlaegen` (`B0223`), die keine eigene Adresse hatten.
- **Den nebenläufigen Fall im E2E-Test mitprüfen** — über den Browser ist die Lage nicht herstellbar. Derselbe Grund, aus dem es `KanbanC.Blazor.Tests` gibt.

### Bewusst out of scope

- **Historie verfallener Nummern.** Welche Nummern eine Karte einmal trug, beantwortet dieser Slice nicht. Das wäre eine eigene Tabelle, ein eigener Leseweg und eine eigene Ansicht — ein eigener Slice, wenn sich zeigt, dass er gebraucht wird.
- **Kartenklasse ändern und entfernen.** Gibt es in der WBS nicht; `R00022` hat beides mit Grund out of scope gestellt. Mit diesem Slice wird die Frage **entscheidbar** (jetzt steht fest, was eine Zuordnung tut) und zugleich **fällig** (siehe die erste offene Frage).
- **Klassenfilter in der Oberfläche.** Das gezielte Abrufen ist `I0022` und dort ausdrücklich als reine API-Zusage geführt.
- **Nummern beim Import vergeben.** `I0030` (WBS-Datei importieren) benutzt diesen Slice, baut ihn nicht mit.
- **Zugriffsschutz.** Die Anwendung läuft im LAN ohne Anmeldung — Leitplanke der Vision.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber nicht am Menschen geprüft. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Die Zuordnung speichert sofort**, wie die Nachbarfelder im Eigenschaftenblock — kein eigener Speicherknopf.
2. **Ein Wechsel wird nicht gewarnt**, obwohl die alte Nummer verfällt.
3. **Die Route heißt `…/kartenklasse`** und weicht damit vom Artboard ab — dieselbe Entscheidung wie in `R00022`.
4. **Ein `PUT` mit leerem Feld ersetzt ein `DELETE`.**
5. **Der Befund einer fremden Kartenklasse ist ein eigener Code** (`kartenklasse-fremd`) und kein `kartenklasse-unbekannt` — nach dem Muster `karte-fremd`/`spalte-fremd`, weil die Kompensation eine andere ist.
