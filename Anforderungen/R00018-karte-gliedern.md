---
id: R00018
status: Neu
datum: 2026-09-05
---

# R00018: Karte gliedern

## Beschreibung

Eine Karte trägt **Teilaufgaben**: kurze Texte, die in der Reihenfolge ihres Anlegens untereinander stehen und einzeln abgehakt werden können. Angelegt wird über `POST /api/karten/{karteId}/teilaufgaben`, abgehakt über `PUT /api/karten/{karteId}/teilaufgaben/{teilaufgabeId}` — beide antworten mit dem **ganzen `Kartendetail`**, derselben Antwortgestalt, die `R00017` für diese Seite festgelegt hat. In der Oberfläche steht der Abschnitt „Teilaufgaben" auf `/karten/{karteId}` hinter „Beschreibung", mit einem gerechneten Fortschritt („2 von 4") samt Balken, Zeilen mit Kästchen und einer Eingabezeile zum Hinzufügen.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

**Der Begriff heißt im ganzen Stack `Teilaufgabe`, nicht „Subtask" — auch in der UI-Beschriftung.** Das ist eine **bewusste Abweichung vom Artboard** (`D0004.dc.html:161` „Subtasks", `:423` „Keine Subtasks · anlegen", `:536` Lesehilfe): „Subtask" ist englisch und widerspricht C06 (deutsche, kontexteindeutige Domänensprache); der Bestand ist durchweg deutsch (`Karteneigenschaft`, `Kartenetikett`, `Etikettvorschlag`, `Kontributorstilllegung`), die gebaute Oberfläche ebenso („Beschreibung", „Verantwortlich", „Fällig", „Etiketten"), und das Artboard selbst schreibt „Kommentare", „Anhänge", „Verweise" — „Subtasks" ist dort der **einzige** englische Abschnittstitel. Zwei Namen für dasselbe Ding wären genau der Synonym-Wildwuchs, den C06 verbietet; `Teilaufgabe` trägt zudem keine Umlaute und erfüllt C07 ohne Ersatzschreibung. Die Wireframes werden nicht nachgeführt (`CLAUDE.md`, „Zieldesign der Oberfläche") — die Abweichung steht deshalb hier, nicht dort. Das Fertig-Kriterium von `I0016` bleibt in seinem Wortlaut stehen („Eine Karte trägt Subtasks, die einzeln abhakbar sind"); gemeint ist dasselbe Ding.

## Geschäftlicher Nutzen

Eine Karte kann seit `R00017` sagen, was zu tun ist — aber nur als ein Stück Fließtext. Eine Aufgabe, die aus fünf Schritten besteht, hat damit zwei schlechte Orte: den Titel oder die Beschreibung. In beiden lässt sich kein einzelner Schritt als erledigt markieren, und keiner der beiden sagt, wie weit die Karte ist, ohne dass jemand den Text liest. Genau das ist der Schmerzpunkt: eine Karte steht tagelang in „In Arbeit", ohne dass Fortschritt sichtbar wäre.

Mit den Teilaufgaben bekommt der **Zwischenstand einer Karte** einen Ort. Für den Menschen heißt das ein Kästchen statt einer Textänderung und ein Fortschritt, der sich ohne Lesen ablesen lässt. Für den KI-Agenten heißt es mehr: er kann seinen eigenen Plan an der Karte ablegen und Schritt für Schritt abhaken, während er arbeitet — die Karte wird das Protokoll seiner Arbeit, ohne dass ein Mensch dafür etwas eintragen muss. Das ist die Form von Gleichberechtigung, die die Vision meint: derselbe Mechanismus über beide Grenzen, mit demselben Ergebnis.

## Funktionale Anforderungen

- `POST /api/karten/{karteId}/teilaufgaben` legt **eine** Teilaufgabe an der Karte an und antwortet mit dem vollständigen `Kartendetail`.
- `PUT /api/karten/{karteId}/teilaufgaben/{teilaufgabeId}` setzt den Abhakstand **genau einer** Teilaufgabe und antwortet ebenso mit dem vollständigen `Kartendetail`.
- Das `Kartendetail` trägt die Teilaufgaben der Karte in stabiler Reihenfolge; eine neue Teilaufgabe wird **angehängt**.
- Eine Teilaufgabe hat eine eigene Nummer (`TeilaufgabeId`), die ihr Abhaken und ein späteres Umbenennen überlebt.
- Der Abhakstand ist ein **Ja/Nein** ohne Zeitpunkt; er lässt sich in beide Richtungen setzen.
- Ein leerer oder zu langer Teilaufgabentext wird mit Befund zurückgewiesen; gespeichert wird nichts.
- Zwei gleichlautende Teilaufgaben an derselben Karte sind **erlaubt** — zwei gleich benannte Arbeiten sind zwei Arbeiten, anders als zwei gleichlautende Etiketten.
- Eine unbekannte Karte und eine Teilaufgabennummer, die **nicht zu dieser Karte** gehört, werden mit HTTP 404 samt Rumpf beantwortet.
- Die Kartenseite zeigt den Abschnitt „Teilaufgaben" mit gerechnetem Fortschritt („2 von 4") und Balken, je Zeile ein Kästchen (abgehakte durchgestrichen) und eine Eingabezeile „Teilaufgabe hinzufügen".
- Hat die Karte keine Teilaufgabe, steht dort die **Handlung statt einer Null**: „Keine Teilaufgaben · anlegen", kein Fortschritt und kein Balken.
- Der Fortschritt wird **gerechnet, nicht gespeichert** und nicht mitgesendet.
- Alle Änderungen sind nach einem Reload und nach einem Neustart unverändert da.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** `012-kartenteilaufgabe.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Migrationslaeufer.cs:16-23`). Deshalb eine eigene Tabelle statt `ALTER TABLE Karte ADD COLUMN`, wie bei den Migrationen 004, 005, 007, 008, 009, 010 und 011.
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der beiden neuen Routen trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404. **Jede neue Route bringt ihren Vertragsfall im selben Arbeitsgang mit** (`FehlervertragTests.cs:41-58`; Lehre aus `B0152`/`B0159`): der Test liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot.
- **Antwortgestalt:** Beide Routen antworten mit **200 und dem ganzen `Kartendetail`** — auch das Anlegen, anders als `POST …/karten` (`KartenEndpunkte.cs:118`, HTTP 201 mit der angelegten Karte). Grund: die Antwort trägt hier nicht die angelegte Zeile, sondern die Seite, die der Aufrufer betrachtet; ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite (`B0224`, `B0238`).
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"). Symbole als Inline-SVG, wie das Kästchen im Artboard.
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier keine Projektreferenz auf `KanbanC.BL`; der Teilaufgabenabschnitt spricht ausschließlich über HTTP.
- **Nebenläufigkeit:** Das Abhaken schreibt **eine Zeile** und lässt die übrigen unberührt — zwei Betrachter, die gleichzeitig verschiedene Teilaufgaben derselben Karte abhaken, verlieren einander nicht.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün, mit den benannten Änderungen (siehe Akzeptanzkriterien).

## Akzeptanzkriterien

### Die Karte trägt Teilaufgaben (API)

- [ ] `POST /api/karten/{karteId}/teilaufgaben` mit einem Text antwortet mit HTTP 200 und einem `Kartendetail`, dessen Teilaufgabenliste diesen Text als **letzten** Eintrag trägt.
- [ ] Rechenbeispiel Reihenfolge: an eine Karte ohne Teilaufgaben werden nacheinander `A`, `B`, `C` angelegt → die Liste lautet in jedem folgenden Abruf `A`, `B`, `C`, unabhängig davon, welche danach abgehakt werden.
- [ ] Jede Teilaufgabe trägt in der Antwort eine eigene, von den anderen verschiedene Nummer.
- [ ] `GET /api/karten/{karteId}` liefert danach dieselbe Liste in derselben Reihenfolge.
- [ ] Zwei Aufrufe mit **demselben** Text an derselben Karte werden beide angenommen und erzeugen zwei Einträge mit verschiedenen Nummern.
- [ ] Die Teilaufgaben hängen am `Kartendetail` und **nicht** an `Karte`: `GET /api/boards/{boardId}` liefert die Karten unverändert ohne Teilaufgabenliste.
- [ ] Ein Neustart der Anwendung lässt Texte, Reihenfolge und Abhakstand unverändert.

### Einzeln abhakbar (API)

- [ ] `PUT /api/karten/{karteId}/teilaufgaben/{teilaufgabeId}` mit „abgehakt" antwortet mit HTTP 200 und einem `Kartendetail`, in dem **genau diese** Teilaufgabe abgehakt ist und die übrigen unverändert stehen.
- [ ] Derselbe Aufruf mit „nicht abgehakt" nimmt das Abhaken zurück.
- [ ] Rechenbeispiel: Karte mit `A`, `B`, `C`; `B` abhaken → `A` und `C` bleiben nicht abgehakt, Nummern und Reihenfolge aller drei bleiben dieselben.
- [ ] Ein zweiter Aufruf mit demselben Stand ändert nichts und antwortet weiterhin mit HTTP 200.

### Zurückweisung und Fehlerantworten für Agenten

- [ ] Ein leerer Text (auch ein Text nur aus Leerzeichen) wird mit HTTP 400 **und Rumpf** zurückgewiesen; die Liste der Karte bleibt danach unverändert.
- [ ] Ein zu langer Text wird ebenso mit HTTP 400 und Rumpf zurückgewiesen; nichts wurde gespeichert.
- [ ] Randleerzeichen fallen weg, der Text im Übrigen nicht: `"  Kaffee  "` wird als `"Kaffee"` gespeichert, Groß- und Kleinschreibung bleibt.
- [ ] Eine **unbekannte** `karteId` beantworten **beide** Routen mit HTTP 404 und einem Befund, der Code, die aufgerufene Kartennummer und einen ausführbaren nächsten Aufruf nennt. Der Befund nennt **kein** Board — die Route kennt keins.
- [ ] Eine `teilaufgabeId`, die es gibt, **aber zu einer anderen Karte gehört**, wird mit HTTP 404 und Rumpf beantwortet; an beiden Karten hat sich nichts geändert. Der Befund nennt **beide** Nummern und die Kompensationsaktion.
- [ ] Keine der beiden Routen liefert eine Fehlerantwort mit leerem Rumpf.
- [ ] Der Vertragstest über alle registrierten Routen bleibt grün: beide neuen Routen werden von ihm abgerufen und stehen nicht als ungeprüft übrig (`FehlervertragTests.cs:41-58`).

### Der Abschnitt auf der Kartenseite

- [ ] Auf `/karten/{karteId}` steht hinter „Beschreibung" ein Abschnitt mit der Überschrift **„Teilaufgaben"** — nicht „Subtasks".
- [ ] Der Abschnitt zeigt den Fortschritt als Text und als Balken. Rechenbeispiel: 4 Teilaufgaben, 2 abgehakt → „2 von 4", Balken zu 50 %; alle 4 abgehakt → „4 von 4", Balken zu 100 %.
- [ ] Hat die Karte **keine** Teilaufgabe, steht dort „Keine Teilaufgaben · anlegen" — kein „0 von 0", kein Balken.
- [ ] Eine abgehakte Zeile ist durchgestrichen, eine offene nicht.
- [ ] Ein Klick auf ein Kästchen ändert den Stand, und der Fortschritt springt im selben Zug mit. Rechenbeispiel: 2 Teilaufgaben, keine abgehakt, eine abhaken → „1 von 2".
- [ ] Die Eingabezeile „Teilaufgabe hinzufügen" samt `+` legt an; danach steht der neue Eintrag als letzter in der Liste und das Feld ist wieder leer.
- [ ] Ein leerer Text bringt eine lesbare Meldung auf der Seite, wie die Zurückweisung des Kartenblatts (`B0227`), und legt nichts an.
- [ ] Nach einem Reload zeigt die Seite denselben Stand — Texte, Reihenfolge und Kästchen.
- [ ] Der Fortschritt kommt **nicht** aus der Antwort: es gibt kein Feld im `Kartendetail`, das ihn trägt.

### Der grüne Bestand bleibt grün — mit drei benannten Änderungen

- [ ] **Benannte Änderung 1:** `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs:15-23`) wächst um die Teilaufgaben. Das sind **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:90`, `KartenServiceTests`); beide werden angepasst, ihre Zusicherungen nicht.
- [ ] **Benannte Änderung 2:** `Nichtgefunden` (`Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs:35`) bekommt die Schwester `Teilaufgabe(karteId, teilaufgabeId)` neben `Karte(karteId)`; der neue Code steht in `AlleCodes`, damit `MeldetEinFehlendesDing` ihn zu 404 zählt. Wortlaut und Kompensation der bestehenden Befunde bleiben unverändert.
- [ ] **Benannte Änderung 3:** `KartendetailSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs`) wächst um die Locator des Abschnitts; die bestehenden Locator und die Tests, die sie nutzen, bleiben unverändert.
- [ ] Alle E2E-Suiten aus `R00001`–`R00017` bleiben **ohne Änderung** grün, insbesondere die Kartendetail-Suiten von `R00017` (Titel, Beschreibung, Fälligkeit, Farbe, Verantwortlicher, Etiketten) und die Board-Suiten `KarteVerschiebenE2ETests`, `EinfuegelinieE2ETests`, `AbschlussbahnAblageE2ETests`, `KartenzahlImBahnenkopfE2ETests`, `KartenmenueE2ETests`.
- [ ] `GET /api/boards/{boardId}` und die Kartenrouten unter dem Board bleiben in Adresse, Verb und Antwortgestalt unverändert.
- [ ] Der zweite Lauf des `Migrationslaeufer` auf einer bestehenden Datei lässt Schema und Daten unverändert.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/012-kartenteilaufgabe.sql` — neue, idempotente Migration; Tabelle `Teilaufgabe` mit `TeilaufgabeId` als Primärschlüssel, `Karte` als Fremdschlüssel, `Text`, `Position`, `Abgehakt`, dazu ein eigener Index auf `Karte`.
- **Contracts:** `Source/KanbanC.Contracts/Karten/Teilaufgabe.cs` (neu), `TeilaufgabeAnlegenAnfrage.cs` (neu), `Teilaufgabenstand.cs` (neu), `Kartendetail.cs` (wächst um die Teilaufgabenliste).
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/TeilaufgabenValidator.cs` und `Teilaufgabentext.cs` (neu); `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` (Schwester für die Teilaufgabe).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Teilaufgabenleser.cs` (neu), `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (`LiesKartendetail` führt die Teilaufgaben mit), `KartenRepository.cs` (`LegeTeilaufgabeAn`, `SetzeAbhakung`), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `LegeTeilaufgabeAn`, `SetzeAbhakung`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — zwei neue Routen als Unterressource der boardlosen Kartenadresse, neben `/etiketten` (`:28`).
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`LegeTeilaufgabeAn`, `SetzeAbhakung` über `AlsKartendetail`, `:47`), `Source/KanbanC.Blazor/Services/Teilaufgabenfortschritt.cs` (neu), `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` (Abschnitt hinter „Beschreibung", `Kartendetail.razor:92-108`).
- **Unberührt:** `Source/KanbanC.Contracts/Karten/Karte.cs`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor`, `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` — **auf der Bahn ändert sich nichts** (siehe „Bewusst out of scope").
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Karten/TeilaufgabenValidatorTests.cs`, `Integrations/Karten/KartenServiceTests.cs`, `TestHelpers/TestKartenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenApiKlientTests.cs`, `Services/TeilaufgabenfortschrittTests.cs`), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (`PageObjects/KartendetailSeite.cs`, neue Testklasse `KarteGliedernE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0004.dc.html`](../Dokumentation/Wireframes/D0004.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gilt daraus der **Abschnitt mit dem Vermerk `I0016`** (`:155-173`): Überschrift, Fortschrittstext, Balken, Zeilen mit Kästchen und durchgestrichenem Text bei abgehakten, Eingabezeile mit `+`. Dazu der **Leerzustand** der frischen Karte (`:423`) als Handlung statt Null. Die Lesehilfe (`:536`) ordnet den Abschnitt `I0016` zu. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Eine bewusste Abweichung, benannt statt stillschweigend:** Das Artboard beschriftet den Abschnitt „Subtasks" (`:161`), den Leerzustand „Keine Subtasks · anlegen" (`:423`) und die Eingabezeile „Subtask hinzufügen" (`:170`). Gebaut wird **„Teilaufgaben"**, **„Keine Teilaufgaben · anlegen"** und **„Teilaufgabe hinzufügen"** — mit der Begründung aus der Beschreibung. Alles andere am Abschnitt folgt der Skizze.

### Ablauf

1. **Teilaufgabe anlegen** (`POST /api/karten/{karteId}/teilaufgaben`)
   - 1.1 `TeilaufgabenValidator.Pruefe(karteId, anfrage)` — leerer Text, zu langer Text; die Kompensation nennt die Route des Aufrufers samt Kartennummer
   - 1.2 Bei Befunden: HTTP 400 mit Rumpf, **kein** Schreibzugriff
   - 1.3 `KartenRepository.LegeTeilaufgabeAn(karteId, anfrage)` in **einer** Transaktion
     - 1.3.1 Existiert die Karte nicht: `null` → HTTP 404 mit `Nichtgefunden.Karte(karteId)`
     - 1.3.2 Höchste `Position` der Karte lesen, `INSERT` mit `hoechste + 1`
     - 1.3.3 `Kartenleser.LiesKartendetail` in derselben Transaktion, dann `Commit`
   - 1.4 HTTP 200 mit dem ganzen `Kartendetail`
2. **Teilaufgabe abhaken** (`PUT /api/karten/{karteId}/teilaufgaben/{teilaufgabeId}`)
   - 2.1 `KartenRepository.SetzeAbhakung(karteId, teilaufgabeId, stand)` in einer Transaktion
     - 2.1.1 `UPDATE Teilaufgabe SET Abgehakt = @Abgehakt WHERE TeilaufgabeId = @TeilaufgabeId AND Karte = @KarteId` — **beide** Nummern in der Bedingung
     - 2.1.2 Keine Zeile getroffen: `null` → HTTP 404 mit `Nichtgefunden.Teilaufgabe(karteId, teilaufgabeId)`
     - 2.1.3 Sonst `LiesKartendetail`, `Commit`
   - 2.2 HTTP 200 mit dem ganzen `Kartendetail`
3. **Lesen** (`GET /api/karten/{karteId}`)
   - 3.1 `Teilaufgabenleser.LiesTeilaufgabenDerKarte` — `ORDER BY Position, TeilaufgabeId`
   - 3.2 Die Liste hängt am `Kartendetail`, nicht an `Karte`; **kein** Archivfilter, wie das ganze Kartendetail (`B0213`)
4. **Oberfläche**
   - 4.1 `KartenApiKlient.LegeTeilaufgabeAn` / `SetzeAbhakung` über `AlsKartendetail` (`KartenApiKlient.cs:47`) — 400 und 404 laufen denselben Weg
   - 4.2 `Teilaufgabenfortschritt` rechnet Text und Balkenanteil aus der Liste
   - 4.3 `Kartendetail.razor` ersetzt nach jeder Antwort das ganze `_detail` — kein zweiter Abruf

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — zwei neue Routen als Unterressource der boardlosen Kartenadresse, neben der Etikettenroute (`:28`). Die Adresse trägt kein Board, weil die Seite keins kennt.
- **`Kartenleser.LiesKartendetail`** — der eine Ort, an dem das Detail entsteht; hier reihen sich die Teilaufgaben ein.
- **`Migrationslaeufer`** — die zwölfte Migration reiht sich ein; kein Journal, also idempotent.
- **`Kartendetail.razor`** — der Abschnitt kommt hinter „Beschreibung" in die linke Spalte (`:92-108`).

**Klassen-Entwurf:**

- `Teilaufgabe` (DTO, immutable) — eine Zeile der Gliederung mit eigener Identität.
  - `record Teilaufgabe(long TeilaufgabeId, string Text, int Position, bool Abgehakt)`
- `TeilaufgabeAnlegenAnfrage` (DTO, immutable) — der Text, mehr braucht das Anlegen nicht; die Position bestimmt der Provider.
  - `record TeilaufgabeAnlegenAnfrage(string Text)`
- `Teilaufgabenstand` (DTO, immutable) — der Abhakstand als eigener Rumpf, damit die Route in beide Richtungen schaltet statt zu kippen.
  - `record Teilaufgabenstand(bool Abgehakt)`
- `Kartendetail` (DTO, immutable) — wächst um `IReadOnlyList<Teilaufgabe> Teilaufgaben`. **Kein Zählfeld daneben:** ein gespeicherter Fortschritt wäre eine zweite Wahrheit neben der Liste, die ihn trägt.
- `Teilaufgabentext` (Operation, pure Logik) — Muster `Etikettentext`: nur Randleerzeichen fallen weg, Groß- und Kleinschreibung bleibt.
  - `static string Normalisiert(string text)`
- `TeilaufgabenValidator` (Operation, pure Logik) — Muster `EtikettenValidator`, mit der Route des Aufrufers in der Kompensation. **Kein Dublettenbefund.**
  - `static Pruefbefunde Pruefe(long karteId, TeilaufgabeAnlegenAnfrage anfrage)`
- `Teilaufgabenleser` (Provider/Ressourcenzugriff) — liest die Teilaufgaben einer Karte in Position-Reihenfolge, in der laufenden Transaktion.
  - `static IReadOnlyList<Teilaufgabe> LiesTeilaufgabenDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)`
- `KartenRepository` (Provider, Integration nach Hausregel) — zwei Schreibwege, beide mit dem ganzen Detail als Rückgabe, beide `null` bei fehlendem Ding.
  - `Kartendetail? LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)`
  - `Kartendetail? SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)`
- `KartenService` (Integration, fängt/prüft) — dieselbe Antwortgestalt wie `AendereKarte` und `SetzeEtiketten`.
  - `Ergebnis<Kartendetail> LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)`
  - `Ergebnis<Kartendetail> SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)`
- `Nichtgefunden` (Operation) — eine Schwester mehr.
  - `static Fehlerbefund Teilaufgabe(long karteId, long teilaufgabeId)`
- `Teilaufgabenfortschritt` (Operation in der Oberflächenschicht, pure Logik) — Muster `Bahnenkopfzahl`.
  - `static string AlsText(IReadOnlyList<Teilaufgabe> teilaufgaben)`
  - `static int AlsProzent(IReadOnlyList<Teilaufgabe> teilaufgaben)`
- `KartenApiKlient` (Integration) — zwei Aufrufe mehr, beide über `AlsKartendetail`.
  - `Task<ApiErgebnis<Kartendetail>> LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)`
  - `Task<ApiErgebnis<Kartendetail>> SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)`

### Änderungen an bestehenden Klassen

- `Kartendetail` (`Source/KanbanC.Contracts/Karten/Kartendetail.cs:15-23`) — ein Feld mehr. **Änderung an grünem Bestand, aber mit kleiner Breite:** genau **zwei** positionale `new Kartendetail(`-Aufrufstellen (`Kartenleser.cs:90`, `KartenServiceTests`) gegen 16 an `Karte` (belegt in `B0221`). Genau deshalb hängt die Liste hier und nicht an `Karte`.
- `Kartenleser` (`:90`) — `LiesKartendetail` führt die Teilaufgaben mit, wie schon die Etiketten. Kein Archivfilter.
- `KartenRepository` — zwei Schreibwege dazu, Muster `SetzeEtiketten` (`:161-177`): Existenzprüfung, Schreiben und Rückgabe des ganzen Details in **einer** Transaktion. **`SchreibeOrdnung` (`:113`) wird nicht angefasst** — es gibt in diesem Slice weder Löschen noch Umsortieren, also entsteht keine Lücke, die zu verdichten wäre.
- `IKartenRepository` — zwei Signaturen dazu; `TestKartenRepository` zieht mit.
- `KartenEndpunkte` (`:28` Etikettenroute als Muster) — zwei Routenkonstanten und zwei Registrierungen. **Der Vertragsfall jeder Route gehört in denselben Arbeitsgang wie die Route**: `FehlervertragTests.cs:41-58` liest die registrierten Routen aus dem Testhost, und zwischen Route und Vertragsfall ist die Suite rot. Drei Fälle: 400 und 404 beim Anlegen, 404 beim Abhaken.
- `Nichtgefunden` (`:35` als Nachbar) — die Schwester `Teilaufgabe(karteId, teilaufgabeId)`; der neue Code kommt in `AlleCodes`, damit `MeldetEinFehlendesDing` ihn zu 404 zählt.
- `Kartendetail.razor` (`:92-108`) — ein Abschnitt mehr hinter „Beschreibung", samt Eingabefeld und Zurückweisungsmeldung (Muster `B0227`).
- `KartendetailSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs`) — Locator für Abschnitt, Fortschritt, Balken, Zeilen, Kästchen, Eingabefeld, Leerzustand und Meldung.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Teilaufgabentext.Normalisiert` — Randleerzeichen fallen weg, Groß-/Kleinschreibung und innere Leerzeichen bleiben.
- `TeilaufgabenValidator.Pruefe` — leerer Text, Text nur aus Leerzeichen, zu langer Text, gültiger Text ohne Befund; **zwei gleichlautende Texte an derselben Karte ergeben keinen Befund**; die Kompensation nennt `POST /api/karten/{karteId}/teilaufgaben` samt Nummer.
- `Teilaufgabenfortschritt` (in `KanbanC.Blazor.Tests`) — „2 von 4" bei 50 %, „4 von 4" bei 100 %, keine Teilaufgabe (Leerzustand statt „0 von 0"), keine abgehakte (0 %).
- `KartenService.LegeTeilaufgabeAn` / `SetzeAbhakung` gegen `TestKartenRepository` — Erfolg reicht das Detail durch; unbekannte Karte und fremde Teilaufgabe liefern Befunde mit nichtleerem Code, Meldung und Kompensation; nach einer Zurückweisung wurde **nicht geschrieben**.
- `KartenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und Rumpf des abgesetzten Aufrufs werden mitgeprüft. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository` und `Teilaufgabenleser` gegen eine `TemporaereDatenbank` — anlegen und wieder lesen; drei Anlagen ergeben die Positionen 1, 2, 3; Abhaken ändert **eine** Zeile und lässt die anderen unberührt; eine `teilaufgabeId` einer **fremden** Karte hakt nichts ab und liefert `null`; `null` bei unbekannter Karte; alles in einer Transaktion. `Kartenleser.LiesKartendetail` liefert die Liste in Position-Reihenfolge, auch für eine **archivierte** Karte. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — beide Routen mit 200, 400 und 404 samt Rumpf; `GET /api/boards/{boardId}` trägt danach **keine** Teilaufgabenliste an den Karten; `FehlervertragTests` ruft beide Routen ab. `WebApiNeustartTests` — Texte, Reihenfolge und Abhakstand überstehen den Neustart.

**E2E:** Eine Karte auf `/karten/{karteId}` ohne Teilaufgaben zeigt „Keine Teilaufgaben · anlegen". Zwei Teilaufgaben anlegen → beide stehen in Anlegereihenfolge, Fortschritt „0 von 2". Eine abhaken → durchgestrichen, Fortschritt „1 von 2". Reload → derselbe Stand (US-1, US-2). Leerer Text → lesbare Meldung, nichts angelegt (US-3). Dazu laufen die E2E-Suiten aus `R00001`–`R00017` weiter — **ohne Änderung**; das ist die Gegenprobe des Slice.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün). Das ist die einzige Vorbedingung, die die WBS-Spalte `Braucht` von `I0016` führt; sie ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00017`** (`I0015`, grün — die Kartenseite, die Adresse `/karten/{karteId}`, das `Kartendetail` als Antwortgestalt, `KartenApiKlient.AlsKartendetail`, `KartendetailSeite`). **Fachlich trägt `R00017` diesen Slice**, nicht `R00006`: der Abschnitt sitzt auf der Kartenseite, und ohne `Kartendetail` gäbe es keine Antwortgestalt. Die Spalte `Braucht` von `I0016` nennt `I0015` nicht; das ist in der WBS als offene Frage vermerkt (`kanbanc.md:457`) und gehört in `/planung aendern I0016`, wenn die Herkunft dokumentiert bleiben soll. An Front und Welle ändert es nichts, weil `I0015` grün ist.
- Setzt ferner auf: **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css`), **`R00016`** (`LiesKartendetail` ohne Archivfilter).
- Blockiert: **keinen** Knoten — kein Slice der WBS nennt `I0016` in seiner Spalte `Braucht` (geprüft am 2026-09-05 über `Dokumentation/Planung/kanbanc.md`). Fachlich hängt der Zähler auf der Bahn an dieser Anforderung (siehe „Bewusst out of scope").

## Umfang

```
Karte gliedern (I0016) = 11 Bubbles: 10 Standard (8,8h), 1 unklar (2–4h).
Rest: 8,8h klar + 2–4h unklar · 7 von 11 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 11 Bubbles gruen (0 %) · 0 laufen · 11 offen
```

`I0016` ist vollständig bis zur Bubble geplant und trägt seine elf Bubbles (`B0243`–`B0253`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung (`kanbanc.md:456`): die Interaction hat einen prüfbaren Aspekt, nicht mehrere. Anlegen und Abhaken teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; als zwei Features geführt wären es zwei Slices, die nur nacheinander gehen und dasselbe Verhalten teilen. **Die Requirement-Klammer sitzt deshalb allein an `I0016`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0243` Teilaufgabentabelle anlegen | Provider (Migration) | 0,4h (belegt über `B0234`) |
| `B0244` Teilaufgaben am Kartendetail lesen | Contracts + Provider | 0,4h (belegt über `B0235`) |
| `B0245` Teilaufgabentext prüfen | Operation | 0,4h (belegt über `B0236`) |
| `B0246` Teilaufgabe anlegen | Provider | 0,4h (belegt über `B0237`) |
| `B0247` Teilaufgabe abhaken | Provider | 0,4h (belegt über `B0237`) |
| `B0248` Teilaufgaben verdrahten | Integration | 0,4h (belegt über `B0238`) |
| `B0249` Endpunkte der Teilaufgaben | Integration | 2h (Richtwert) |
| `B0250` API-Klient der Teilaufgaben | Integration | 2h (Richtwert) |
| `B0251` Teilaufgabenfortschritt rechnen | Operation (Oberfläche) | 0,4h (belegt über `B0229`) |
| `B0252` Teilaufgabenabschnitt der Kartenseite | UI | 2h (Richtwert) |
| `B0253` E2E Karte gliedern | E2E | 2–4h (**unklar**) |

Mit 11 Bubbles ist das ein mittelgroßer Slice — nach den 30 von `I0015` wieder in der Größenordnung von `I0012` und `I0014`. Die einzige unklare Bubble ist die E2E-Bubble; derselbe Vermerk wie bei `I0005` bis `I0015`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`). Die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen — das verschöbe die Zählung des ganzen Baums. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- **Lässt sich das Kästchen auch über die Tastatur schalten?** — **nicht entschieden**, bewusst nicht geraten. Ein `<input type="checkbox">` in einem `<label>` kann es von selbst, eine gezeichnete SVG-Fläche mit `@onclick` nicht. Das Artboard zeichnet ein SVG-Kästchen (`D0004.dc.html:165-168`) und sagt zur Bedienung nichts. **Dieselbe offene Stelle wie bei `B0228`.** Vor `B0252` zu beantworten, weil sie die Bauform des Kästchens bestimmt; die E2E-Prüfung (`B0253`) hängt daran.
- **Soll die Eingabezeile nach dem Anlegen offen bleiben?** — **nicht entschieden.** Wer eine Gliederung schreibt, legt selten genau eine Zeile an; das Artboard zeigt einen Zustand, keinen Ablauf. Gebaut wird zunächst: Feld bleibt stehen und ist leer, Fokus bleibt darin. Vor `B0252` zu bestätigen.
- **Wann setzt der Mensch den Abhakstand ab — sofort oder gesammelt?** — **entschieden: sofort.** Ein Kästchen ist eine Einzelhandlung ohne Speichernknopf, und die Route schreibt genau eine Zeile. Steht hier, weil es bei `R00017` (`PUT` über alle vier Felder) die umgekehrte Antwort gab und der Unterschied kein Versehen ist.

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft bei jedem Start des `KanbanC.WebApi` mit.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass eine Karte seit `R00017` beschreibbar ist, aber keinen **Zwischenstand** kennt: eine Aufgabe aus fünf Schritten steht als ein Textblock da, und wer den dritten Schritt erledigt, kann das nirgends festhalten außer durch Umschreiben des Textes. Das Zielbild ist eine Karte, die ihren eigenen Fortschritt trägt — für den Menschen ablesbar ohne Lesen, für den Agenten schreibbar ohne Umformulieren. Die Kausalkette: **wenn** die Teilaufgabe eine eigene Zeile mit eigener Nummer bekommt (X), **dann** ist der Abhakstand ein einzeln adressierbarer Wert statt eines Textzustands (Y), **und dann** kann jeder der beiden Akteure genau einen Schritt schalten, ohne die Arbeit des anderen zu überschreiben, und die Oberfläche kann den Stand rechnen, statt ihn zu speichern (Z). Der Hebel liegt genau hier und nicht davor oder danach: davor — an `Karte` — würde dieselbe Beziehung 16 Aufrufstellen kosten und jeden Board-Abruf verteuern, ohne dass die Bahn davon etwas fordert; danach — bei einem gespeicherten Zählfeld — entstünde eine zweite Wahrheit neben der Liste, die schon beim ersten nebenläufigen Abhaken auseinanderliefe. Die eigene Nummer statt des Textes als Schlüssel ist derselbe Hebel eine Ebene tiefer: sie ist das Einzige, was den Abhakstand ein späteres Umbenennen überleben lässt.

## Missing-Docs

- **SQLite und `INTEGER PRIMARY KEY` mit `AUTOINCREMENT`:** Für `TeilaufgabeId` genügt ein `INTEGER PRIMARY KEY` (Rowid-Alias); ob eine wiederverwendete Nummer nach einem Löschen je zum Problem wird, ist erst relevant, wenn es ein Löschen gibt. Belegwürdig, sobald `I0016` um Entfernen erweitert wird.
- **Dapper und `bool` gegen SQLite `INTEGER`:** Der Bestand führt Ja/Nein-Werte bisher nur als Datum (`ErledigtAm`, `StillgelegtAm`, `ArchiviertAm`). `Abgehakt` ist der erste echte `bool` in einer Spalte; das Umsetzverhalten von Dapper ist im Repository nirgends belegt. Ein Probe-Test nach Skill `dependency-probe` gehört in `B0243` oder `B0246`, bevor darauf gebaut wird.

## Notizen

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Etikettenmuster: Karte und Text als Schlüssel, ganze Liste ersetzen** (`011-kartenetikett.sql`, `SetzeEtiketten`) | Eine Teilaufgabe hat eine Identität, die das Abhaken überlebt. Mit dem Text als Schlüsselbestandteil verlöre ein Umbenennen den Abhakstand; eine Listenersetzung schriebe bei jedem Klick alle Zeilen neu, vergäbe neue Nummern und verlöre den Zugang eines gleichzeitigen Aufrufers. |
| **`ALTER TABLE Karte ADD COLUMN`** | Eine Karte trägt n Teilaufgaben; eine Spalte trägt eine. Außerdem führt der `Migrationslaeufer` kein Journal — nur idempotente Skripte sind zulässig, und `CREATE TABLE IF NOT EXISTS` ist die gebaute Form (sechstes Mal nach `B0108`, `B0126`, `B0184`, `B0201`, `B0220`). |
| **`Abgehakt` als Zeitpunkt statt Ja/Nein** | Das Artboard zeigt kein Datum, und die Karte hat mit `ErledigtAm` schon einen Zeitpunkt für den einzigen Ort, an dem er gebraucht wird. Ein Zeitpunkt, den niemand liest, wäre tote Flexibilität (C14). |
| **Position mit Verdichtung (`SchreibeOrdnung`) und eine Reihenfolge-Route** | Beides wird erst gebraucht, wenn gelöscht oder umsortiert werden kann; in diesem Slice entsteht keine Lücke. `Position` bleibt trotzdem, weil sonst die Datenbank die Anzeige bestimmte und zwei Abrufe dieselbe Karte verschieden zeigten. |
| **Ein gespeicherter Fortschrittszähler am `Kartendetail`** | Zweite Wahrheit neben der Liste, die ihn trägt. Gerechnet wird in der Oberflächenschicht, Muster `Bahnenkopfzahl`. |
| **Zwei Features (Anlegen / Abhaken)** | Sie teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; getrennt geführt wären es zwei Slices, die nur nacheinander gehen und dasselbe Verhalten prüfen (`kanbanc.md:456`). |
| **HTTP 201 beim Anlegen** | Die Antwort trägt das ganze `Kartendetail`, nicht die angelegte Zeile. Ein Created-Rumpf wäre eine zweite Antwortgestalt für dieselbe Seite (`B0224`, `B0238`). |

### Bewusst out of scope

- **Der Zähler „2/4" auf der Kartenform der Bahn.** Das Artboard `D0003.dc.html:261` zeichnet ihn und ordnet ihn `I0016` zu (`D0003.dc.html:395`), aber das **Fertig-Kriterium von `I0016` nennt ihn nicht** („Eine Karte trägt Subtasks, die einzeln abhakbar sind" — kein Wort von der Bahn). Preis wäre `Source/KanbanC.Contracts/Karten/Karte.cs`: acht positionale Felder mit **16 Aufrufstellen in 9 Dateien** (belegt in `B0221`), gegen **zwei** an `Kartendetail`. Dieselbe Frage ist in `F0043` schon entschieden — eine n-Beziehung hängt am `Kartendetail`, nicht an `Karte` (`kanbanc.md:433`) —, und eine zweite Antwort hier wäre ein Widerspruch. **Lücke mit Adresse:** der Zähler gehört zu **derselben offenen Interaction unter `D0003`** wie Farbe und Verantwortlicher auf der Bahn (`kanbanc.md:433`); ein Artboard fehlt dafür **nicht** — `D0003.dc.html:261` zeichnet ihn bereits. Denselben Weg gingen `I0014` und `I0015` vor ihm.
- **Umsortieren, Umbenennen und Entfernen einer Teilaufgabe.** Im Artboard **nicht gezeichnet** (`D0004.dc.html:155-173` zeigt weder Ziehgriffe noch Hoch/Runter, keinen Stift und kein `✕`) und im Fertig-Kriterium nicht gefordert. **Lücke mit Adresse:** sie brauchen zuerst eine Skizze über `/wireframe verfeinern D0004` und danach eine eigene Anforderung. Erst mit ihnen entstünden Lücken in der Position, und erst dann wären `SchreibeOrdnung` und eine Reihenfolge-Route fällig.
- **Eine Route, die die ganze Gliederung am Stück setzt** (Import einer Liste in einem Aufruf). Nicht geprüft, ob der Mensch sie will; sie wäre eine **Ergänzung**, keine Änderung — die beiden Routen dieses Slice blieben unverändert.
- **Ein Verlauf, wer wann was abgehakt hat.** Die Lesehilfe von `D0004` vermerkt den fehlenden Verlaufsknoten als offene Frage des Wireframe-Index; in der WBS gibt es dafür keinen Knoten. Bleibt dort.

### Angenommen im stillen Lauf

- **Die UI-Beschriftung lautet „Teilaufgaben".** Begründet in der Beschreibung; die Wireframes werden dafür nicht nachgeführt.
- **Der Abschnitt steht hinter „Beschreibung" in der linken Spalte**, wie im Artboard, nicht im Eigenschaftenblatt rechts.
- **Höchstlänge des Teilaufgabentexts:** dieselbe Größenordnung wie beim Etikett (`EtikettenValidator`, 100 Zeichen) — eine Teilaufgabe ist eine Zeile, kein Absatz. Der genaue Wert wird in `B0245` festgelegt und dort begründet.
- **Der Abhakstand reist als eigener Rumpf `Teilaufgabenstand`**, nicht als Umschalten ohne Rumpf: ein `PUT`, das kippt, ist nicht wiederholbar, und ein Agent, der zweimal absetzt, käme sonst beim Ausgangszustand heraus.
