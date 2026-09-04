---
id: R00014
status: Neu
datum: 2026-09-04
---

# R00014: Kontributor stilllegen

## Beschreibung

Ein Kontributor, der nicht mehr mitarbeitet, wird **stillgelegt** statt gelöscht: über `PUT /api/kontributoren/{kontributorId}/stilllegung` und über ein Pausensymbol in seiner Zeile auf `/kontributoren`. Er rutscht unter eine Gruppenzeile „stillgelegt · n" ans Ende der Liste, steht dort abgeblendet mit durchgestrichenem Namen und „stillgelegt seit &lt;Datum&gt;", und ein Textknopf „zurückholen" macht es rückgängig. Aus der Identitätswahl der Kopfzeile verschwindet er — weder über noch unter der Trennlinie; wer ihn zuvor gewählt hatte, sieht wieder „nicht gewählt".

Zahlt ein auf: [Vision](R00000-vision.md) — „Was ein Mensch klicken kann, kann ein Agent aufrufen"; und „Auswertungen aus vollständigen Daten … Zeiten je Aufgabe und Kontributor".

**Diese Anforderung ist bewusst kleiner als das Fertig-Kriterium ihrer Interaction — und das darf nicht still passieren.** `I0009` sagt: „Ein stillgelegter Kontributor verschwindet aus der Auswahl, **bleibt aber an alten Karten und Zeiten sichtbar**." Der erste Halbsatz wird hier gebaut und geprüft. Der zweite ist heute **nicht prüfbar**: `Karte` trägt keinen Kontributorbezug (`Source/KanbanC.Contracts/Karten/Karte.cs:3` — `record Karte(long KarteId, string Titel, int Position)`), und Zeiteinträge gibt es nicht (`D0006` ist vollständig rot). Kein ehrlicher Test könnte ihn grün machen (`~/.claude/skills/test-ehrlichkeit/SKILL.md`); ein Kriterium dafür wäre eine Absichtserklärung, kein Kriterium. Der zweite Halbsatz ist deshalb eine **Zusage an `I0015`, `I0017` und `D0006`** und steht unter „Notizen → Bewusst out of scope" mit Adresse — wer dort den Kontributorbezug anlegt, prüft dort auch, dass ein stillgelegter Kontributor an alten Karten und Zeiten sichtbar bleibt. Genau darum wird ein Kontributor hier **nie gelöscht**, sondern nur stillgelegt und zurückgeholt: das Löschen wäre die Handlung, die die spätere Zusage unmöglich machte (`Dokumentation/Planung/kanbanc.md`, Notiz zu `I0009`).

## Geschäftlicher Nutzen

Die Kontributorenliste wächst und schrumpft nie. Wer das Projekt verlässt, steht weiter in der Identitätswahl — und je länger die Anwendung läuft, desto mehr Namen stehen dort, unter denen niemand mehr arbeitet. Ein Löschen wäre die naheliegende Antwort und die falsche: `D0006` (Zeiterfassung) und `I0015`/`I0017` (Verantwortlicher, Kommentar) hängen daran, dass jede alte Zeit und jede alte Karte ihren Urheber behält; die Vision verlangt „Auswertungen aus vollständigen Daten … Zeiten je Aufgabe und Kontributor". Stilllegen ist der Weg, der beides bekommt: die Liste der Wählbaren bleibt kurz, der Datenbestand bleibt vollständig. Und weil `I0009` der letzte rote Slice von `D0002` ist, wird mit ihm der Dialog „Kontributoren führen" fertig — der Lebenszyklus eines Kontributors ist danach vollständig: anlegen (`R00011`), bearbeiten (`R00012`), wählen (`R00013`), stilllegen und zurückholen.

## Funktionale Anforderungen

- Ein Kontributor lässt sich stilllegen und wieder zurückholen — über `PUT /api/kontributoren/{kontributorId}/stilllegung` und über die Zeile auf `/kontributoren`.
- Der Stilllegungsstand steht mit dem **Datum** in jeder Antwortzeile von `GET /api/kontributoren`; `stillgelegtAm: null` heißt „aktiv".
- Stillgelegte stehen **am Ende** der Liste unter einer Gruppenzeile „stillgelegt · n"; die Reihenfolge kommt aus der Abfrage, nicht aus der Oberfläche.
- Der Seitenkopf von `/kontributoren` zählt „n aktiv · n stillgelegt".
- Ein stillgelegter Kontributor steht in der Identitätswahl **weder über noch unter der Trennlinie**.
- Wer einen inzwischen Stillgelegten gewählt hatte, sieht am Identitätsplatz wieder „nicht gewählt"; nach dem Zurückholen trägt der Platz wieder dessen Namen.
- Eine unbekannte `KontributorId` wird mit `404` und einem Befund zurückgewiesen, der Grund, Werte und Kompensationsaktion nennt.
- Der Stand überlebt einen Neustart der WebApi.
- **Kein Löschen.** Es gibt keinen Endpunkt und kein Bedienelement, das einen Kontributor entfernt.
- **Keine Sperre der Bearbeitung**: ein stillgelegter Kontributor bleibt über `PUT /api/kontributoren/{kontributorId}` änderbar; `I0007` wird nicht eingeschränkt.

## Nicht-funktionale Anforderungen

- **Kernregel des Projekts:** `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL` (`CLAUDE.md`). Jede Funktion dieser Anforderung, die die Oberfläche hat, hat auch die API: das Stilllegen ist ein Endpunkt, den die Oberfläche aufruft. Die Filterung der Identitätswahl ist die einzige Regel, die es nur in der Oberfläche gibt — sie schränkt ein, statt zu erweitern, und die Kernregel wirkt in eine Richtung.
- **Fehlerantworten für Agenten:** die `404` trägt einen `Fehlerbefund` mit Code, Meldung samt Nummer und Kompensationsaktion — `Nichtgefunden.Kontributor` steht seit `R00012` und wird wiederverwendet, nicht nachgebaut.
- **Migration idempotent:** `007-kontributorstilllegung.sql` mit `CREATE TABLE IF NOT EXISTS`. Der `Migrationslaeufer` führt jedes Skript bei jedem Start aus und kennt kein Journal; ein `ALTER TABLE ADD COLUMN` scheitert beim zweiten Start (Muster `005-boardarchivierung.sql`, `006-kontributoren.sql`).
- **Gestaltung:** alle Werte aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"; geprüft von `GestaltungsfundamentTests`).
- **Benennung (C06/C07):** `Stilllegung`, `StillgelegtAm`, `Kontributorstilllegung`, `SetzeStilllegung` — ein Begriff, eine Schreibweise, in SQL-Spalte, DTO, Repository, Klient und Oberfläche. Bezeichner ohne echte Umlaute, UI-Texte und Meldungen mit.
- **Ein Feld, keine zweite Wahrheit (C17):** `Kontributor` wächst um `DateOnly? StillgelegtAm` und **nicht** zusätzlich um ein `IstStillgelegt` — der Wahrheitswert wäre daraus ableitbar.
- **Kein Abfrageparameter** an `GET /api/kontributoren`: ein Parameter, der nur ein geliefertes Feld noch einmal ausdrückt, ist tote Flexibilität (C17). Die Route bleibt damit in `RoutenOhneFehlerantwort` (`FehlervertragTests.cs:20`).

## Akzeptanzkriterien

### Stilllegen und Zurückholen über die API

- [ ] `PUT /api/kontributoren/{kontributorId}/stilllegung` mit `{"istStillgelegt": true}` antwortet `200` und liefert den Kontributor mit gesetztem `stillgelegtAm`.
- [ ] Derselbe Aufruf mit `{"istStillgelegt": false}` antwortet `200` und liefert den Kontributor mit `stillgelegtAm: null`.
- [ ] Beide Richtungen sind beliebig oft wiederholbar, ohne dass sich nach dem ersten Aufruf etwas ändert. Rechenbeispiel: zweimal `true`, dann zweimal `false`, dann einmal `true` → ein Kontributor, `stillgelegtAm` gesetzt, in der Tabelle genau eine Zeile.
- [ ] `GET /api/kontributoren` liefert `stillgelegtAm` in **jeder** Zeile — bei aktiven `null`, bei stillgelegten das Datum. Es gibt keinen Abfrageparameter, der die Liste filtert.
- [ ] `PUT /api/kontributoren/4711/stilllegung` auf eine nicht vergebene Nummer antwortet `404` mit einem Befund, der den Code `kontributor-unbekannt`, die Nummer `4711` in der Meldung und `GET /api/kontributoren` als Kompensationsaktion nennt.
- [ ] Nach einem Neustart der WebApi auf derselben Datei ist der Stilllegungsstand unverändert, inklusive Datum.
- [ ] Ein zweiter Lauf der Migration auf einer bestehenden Datei ändert weder Schema noch Daten.
- [ ] `PUT /api/kontributoren/{kontributorId}` (Name und Art ändern, `R00012`) funktioniert an einem stillgelegten Kontributor unverändert und lässt seinen Stilllegungsstand unangetastet.

### Die Liste auf `/kontributoren`

- [ ] Jede **aktive** Zeile trägt rechts neben dem Stift ein zweites Symbol „stilllegen"; ein Klick legt still und lädt die Liste neu.
- [ ] Stillgelegte stehen **am Ende** der Liste, unter einer Gruppenzeile „stillgelegt · n". Rechenbeispiel: `Anna`, `Bert`, `Cem` angelegt, `Anna` stillgelegt → Reihenfolge `Bert`, `Cem`, Gruppenzeile „stillgelegt · 1", `Anna`.
- [ ] Die Sortierung kommt aus `GET /api/kontributoren`; die Oberfläche sortiert nicht ein zweites Mal. Prüfbar an der API allein: aktive alphabetisch (Groß-/Kleinschreibung ohne Einfluss, `KontributorId` als Zweitschlüssel), danach stillgelegte nach derselben Regel.
- [ ] Die Gruppenzeile erscheint **nur**, wenn es mindestens einen Stillgelegten gibt — bei null Stillgelegten steht sie nicht da.
- [ ] Eine stillgelegte Zeile zeigt den Namen durchgestrichen, das Datum als „stillgelegt seit &lt;Datum&gt;" und einen Textknopf „zurückholen"; ein Klick darauf bringt sie zurück über die Gruppenzeile.
- [ ] Der Seitenkopf zeigt rechtsbündig „n aktiv · n stillgelegt". Rechenbeispiel: `Anna` (Mensch), `Bert` (Agent), `Cem` (abgebildet), `Dora` (Mensch) angelegt, `Dora` stillgelegt → „3 aktiv · 1 stillgelegt". **Nicht** „wählbar": das wären unter der Regel von `F0032` nur `Anna` — siehe „Offene Fragen".
- [ ] Ist die WebApi beim Stilllegen oder Zurückholen nicht erreichbar, erscheint die Ausfallmeldung über `WebApiAufruf.MitAusfallmeldung`; die Seite bleibt stehen.
- [ ] Der Stand überlebt einen Reload der Seite.

### Stillgelegte verschwinden aus der Identitätswahl

- [ ] Ein stillgelegter Kontributor steht im Popover „Ich bin …" **weder** als wählbare Zeile über der Trennlinie **noch** als gesperrte darunter. Rechenbeispiel: `Anna` (Mensch), `Bert` (Agent), `Cem` (abgebildet), `Dora` (Mensch) angelegt, `Dora` und `Bert` stillgelegt → eine wählbare Zeile (`Anna`), eine gesperrte (`Cem`).
- [ ] Wer `Dora` gewählt hatte und dann wird `Dora` stillgelegt: nach dem nächsten Laden steht am Identitätsplatz „nicht gewählt" — keine Fehlermeldung, keine Ausnahmeseite.
- [ ] Wird `Dora` zurückgeholt, trägt der Identitätsplatz **ohne erneute Wahl** wieder `Dora`: die gemerkte `KontributorId` wird nicht gelöscht.
- [ ] Nach dem Zurückholen steht `Dora` wieder als wählbare Zeile im Popover.

### Der grüne Bestand bleibt grün

- [ ] `Kontributor` bekommt genau ein Feld hinzu (`DateOnly? StillgelegtAm`). Die **52 positionalen `new Kontributor(…)` in 10 Dateien** ziehen mit; kein Test wird dabei gelöscht oder abgeschwächt. Betroffen sind u. a. `WebApiNeustartTests.cs:152-159`, `IdentitaetslisteTests.cs` (16 Vorkommen), `KontributorenEndpunkteTests.cs` (9), `KontributorenRepositoryTests.cs` (5).
- [ ] `IdentitaetslisteTests.cs` prüft heute in sechs Tests den Filter nach `Kontributorart` allein (`Identitaetsliste.cs:5-28`). Diese sechs bleiben gültig — sie legen niemanden still; die neue Regel kommt als zusätzliche Tests hinzu.
- [ ] `KontributorAendernE2ETests.cs:25` (`Stifte == 3`) und `KontributorenlisteE2ETests.cs:43-45` (drei Zeilen, alphabetisch, Plaketten) bleiben **unverändert** grün — dort wird niemand stillgelegt.
- [ ] `IdentitaetGesperrtE2ETests.cs:31-32` und `:128-139` (Zeilenzahlen im Popover) bleiben **unverändert** grün — auch dort wird niemand stillgelegt.
- [ ] Der Vertragstest `FehlervertragTests` nimmt die neue Route mit ihrem `404`-Fall auf — **in derselben Bubble, in der die Route entsteht** (`B0175`). Sonst ist die Suite rot, weil `Wenn_ein_Endpunkt_hinzukommt_…` jede ungeprüfte Route meldet (Lehre aus `B0152`/`B0159`).
- [ ] `GET /api/kontributoren` bleibt in `RoutenOhneFehlerantwort` (`FehlervertragTests.cs:20`) — es kommt kein Abfrageparameter hinzu, der zurückweisen könnte.
- [ ] Alle Tests aus `R00001`–`R00013` laufen weiter; `TreatWarningsAsErrors` bleibt erfüllt.

## Betroffene Verzeichnisstruktur

- **Contracts — neu:** `Source/KanbanC.Contracts/Kontributoren/Stilllegung.cs`. **Geändert:** `Kontributor.cs` (ein Feld).
- **BL — neu:** `Source/KanbanC.BL/Persistenz/Migrationen/007-kontributorstilllegung.sql`. **Geändert:** `Persistenz/Kontributoren/KontributorenRepository.cs`, `Interfaces/Kontributoren/IKontributorenRepository.cs`, `Integrations/Kontributoren/KontributorenService.cs`.
- **WebApi — geändert:** `Source/KanbanC.WebApi/Endpunkte/KontributorenEndpunkte.cs` (eine Route).
- **Oberfläche — geändert:** `Source/KanbanC.Blazor/Services/KontributorenApiKlient.cs`, `Services/Identitaetsliste.cs`, `Components/Pages/Kontributoren.razor` (+ `.razor.css`), `Components/Layout/Kopfzeile.razor`.
- **Tests:** `Source/KanbanC.BL.Tests/TestHelpers/TestKontributorenRepository.cs` und `Integrations/Kontributoren/KontributorenServiceTests.cs` (geändert); `Source/KanbanC.Blazor.Tests/Services/IdentitaetslisteTests.cs` und `KontributorenApiKlientTests.cs` (geändert); `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Kontributoren/KontributorenRepositoryTests.cs`, `Api/KontributorenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs` (geändert); `Source/KanbanC.PlaywrightTests/PageObjects/KontributorenSeite.cs`, `PageObjects/Rahmen.cs` (geändert) und zwei neue E2E-Dateien.
- **Unberührt:** `Source/KanbanC.Blazor/wwwroot/gestaltung.css` und `oberflaeche.css` — die neuen Regeln bringt `Kontributoren.razor.css` mit, mit Werten aus dem Token-Sheet. Karten, Spalten und Boards werden nicht angefasst.

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0002.dc.html`](../Dokumentation/Wireframes/D0002.dc.html) ist die Gestaltungsvorgabe; einschlägig sind der Seitenkopf mit der Zählzeile (`:107`), das Pausensymbol an jeder aktiven Zeile (`:138`, `:157`, `:196`, `:215`), die Gruppenzeile (`:220-222`) und die stillgelegte Zeile (`:223-239`) mit `opacity: 0.45`, durchgestrichenem Namen, „stillgelegt seit 12.08.2026" und dem Textknopf „zurückholen" mit Kreispfeil. `min-height: 36px` auf der Pflege-Zelle, damit die stillgelegte Zeile nicht flacher wird als die aktiven (`_wireframes.md:151`). Betriebsart des Canvas ist `lokal` (`_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story. Zwei begründete Abweichungen stehen unter „Offene Fragen": die Zählzeile („aktiv" statt „wählbar") und der fehlende Stift an der stillgelegten Zeile (Gestaltung, keine Regel).

### Ablauf

1. **Stilllegen über die API**
   - 1.1 `PUT /api/kontributoren/{kontributorId}/stilllegung` mit `Stilllegung(bool IstStillgelegt)` im Rumpf
   - 1.2 `KontributorenService.SetzeStilllegung(kontributorId, stilllegung)`
     - 1.2.1 Repository liest erst, schreibt dann — eine unbekannte Nummer darf nicht wie ein Erfolg aussehen (Muster `Aendere`)
     - 1.2.2 unbekannt → `Ergebnis<Kontributor>.Zurueckgewiesen(Nichtgefunden.Kontributor(kontributorId))` → `404`
   - 1.3 `IstStillgelegt == true` → `INSERT … ON CONFLICT DO NOTHING` mit dem heutigen Datum; `false` → `DELETE`
   - 1.4 gelesen wird die geschriebene Zeile zurück, in derselben Transaktion
2. **Liste laden**
   - 2.1 `KontributorenRepository.LadeAlle` mit `LEFT JOIN Kontributorstilllegung`; fehlende Zeile heißt aktiv
   - 2.2 `ORDER BY` erst nach Stilllegung, dann `Name COLLATE NOCASE, KontributorId`
3. **Oberfläche `/kontributoren`**
   - 3.1 Zeilen mit `StillgelegtAm is null` bekommen Stift und Pausensymbol
   - 3.2 vor der ersten stillgelegten Zeile die Gruppenzeile „stillgelegt · n" über `colspan`
   - 3.3 stillgelegte Zeile: abgeblendet, Name durchgestrichen, Datum, Textknopf „zurückholen"
   - 3.4 nach jedem Schalten wird die **ganze Liste neu geholt** — die Reihenfolge gehört der Abfrage (Muster `SichereBearbeitung`)
   - 3.5 Seitenkopf zählt aus der geladenen Liste: aktiv = `StillgelegtAm is null`
4. **Identitätswahl**
   - 4.1 `Identitaetsliste.Waehlbare` = aktiv **und** `Kontributorart.Mensch`; `Gesperrte` = aktiv **und** nicht Mensch
   - 4.2 stillgelegte stehen in keiner der beiden Listen
   - 4.3 `Kopfzeile.Identitaetsbeschriftung` löst die gemerkte Id künftig über `Waehlbare` auf statt über die volle Liste; kein Treffer → „nicht gewählt"
   - 4.4 der `sessionStorage` wird **nicht** angefasst — die Id bleibt liegen und trägt wieder, sobald zurückgeholt wird

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `KontributorenEndpunkte.Registriere` — eine Route mehr, Unterressource wie `/api/boards/{boardId}/archivierung`.
- `Kontributoren.razor:76-85` — die Pflege-Zelle trägt heute genau einen Schalter und bekommt einen zweiten; `.spalte-pflege` ist mit 15 % für einen gerechnet (`Kontributoren.razor.css:88-91`).
- `Identitaetsliste.cs:5-28` — die eine Stelle, an der „wählbar" definiert ist; sie bekommt die zweite Bedingung.
- `Kopfzeile.razor:143-152` — löst die gemerkte Id heute über die volle Liste auf.
- `007-kontributorstilllegung.sql` — der `Migrationslaeufer` liest die Skripte in Namensreihenfolge; der Platzhalter-Eintrag in `KanbanC.BL.csproj` deckt die Einbettung ab (Beleg: `B0138`).

**Klassen-Entwurf:**

- `Stilllegung` (DTO, immutable, `KanbanC.Contracts/Kontributoren/`) — der gewünschte Stand als Rumpf des Aufrufs; ein benanntes Feld statt eines nackten `bool`, damit ein Agent im JSON sieht, was er setzt, und dieselbe Route zurückholt. Muster `Archivierung`.
  - `record Stilllegung(bool IstStillgelegt)`
- `Kontributor` (DTO, immutable, **geändert**) — wächst um `DateOnly? StillgelegtAm`; `null` heißt aktiv. Kein zweites Feld `IstStillgelegt` (C17).
- `IKontributorenRepository` (Interface, **geändert**)
  - `Kontributor? SetzeStilllegung(long kontributorId, Stilllegung stilllegung)` — `null` heißt: diese `KontributorId` gibt es nicht.
- `KontributorenRepository` (Provider/Ressourcenzugriff, **geändert**) — schreibt und liest den Stilllegungsstand; die Zeile in `Kontributorstilllegung` ist die Aussage, das Datum ihre Ergänzung. `LadeAlle` bekommt den `LEFT JOIN` und die erweiterte `ORDER BY`-Klausel.
- `KontributorenService` (Integration, **geändert**)
  - `Ergebnis<Kontributor> SetzeStilllegung(long kontributorId, Stilllegung stilllegung)` — kein Validator: ein Wahrheitswert hat keinen ungültigen Fall, geprüft wird nur, ob es den Kontributor gibt (Muster `BoardService.SchalteKartenzahl`, `B0111`).
- `KontributorenApiKlient` (Integration, Blazor, **geändert**)
  - `Task<ApiErgebnis<Kontributor>> SetzeStilllegung(long kontributorId, Stilllegung stilllegung)` — `400` und `404` laufen denselben Weg wie in `Aendere` (`KontributorenApiKlient.cs:43-57`).
- `Identitaetsliste` (Operation, statisch, **geändert**) — `Waehlbare` und `Gesperrte` beziehen den Stilllegungsstand ein; ein stillgelegter Kontributor steht in keiner der beiden Listen.
- `Kontributoren` (UI-Seite, **geändert**) — Pausensymbol, Gruppenzeile, stillgelegte Zeile, Zählzeile im Seitenkopf.
- `Kopfzeile` (UI, **geändert**) — löst die gemerkte Id über `Identitaetsliste.Waehlbare` auf.
- `KontributorenSeite` und `Rahmen` (PageObjects, **geändert**) — Locator für Pausensymbol, Gruppenzeile, stillgelegte Zeilen, „zurückholen" und Zählzeile.

### Änderungen an bestehenden Klassen

- `Kontributor.cs` — ein Feld; **52 positionale Aufrufe in 10 Dateien** ziehen mit (Liste unter „Akzeptanzkriterien → Der grüne Bestand bleibt grün").
- `KontributorenRepository.cs` — `LadeAlle` (`:49-58`) bekommt `LEFT JOIN` und `ORDER BY`; `LiesKontributor` ebenso; `SetzeStilllegung` und ein privates `SchreibeStilllegung` kommen hinzu (Muster `BoardRepository.SchreibeArchivierung`, `:121-138`).
- `IKontributorenRepository.cs` und `TestKontributorenRepository.cs` — eine Methode mehr.
- `KontributorenService.cs` — eine Methode mehr.
- `KontributorenEndpunkte.cs` — eine Route mehr.
- `KontributorenApiKlient.cs` — eine Methode mehr.
- `Identitaetsliste.cs` — die zwei Filter bekommen eine zweite Bedingung; der Kommentar `:5-8` wird nachgeführt.
- `Kopfzeile.razor` — `Identitaetsbeschriftung` löst über `Waehlbare` auf.
- `Kontributoren.razor(.css)` — zweiter Schalter in der Pflege-Zelle, Breite von `.spalte-pflege` neu bemessen, Gruppenzeile und stillgelegte Zeile, Zählzeile.
- `FehlervertragTests.cs` — ein Fehlerfall mehr (in derselben Bubble wie die Route).

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Identitaetsliste` — `Waehlbare` und `Gesperrte` mit stillgelegten Menschen, stillgelegten Agenten und aktiven aller drei Arten. Pure Logik ohne Seiteneffekte, prüfbar ohne Browser (`KanbanC.Blazor.Tests`).
- `KontributorenService.SetzeStilllegung` gegen `TestKontributorenRepository` — Erfolg und die eine Zurückweisung (unbekannte Nummer) mit dem Befundcode.
- `KontributorenApiKlient.SetzeStilllegung` — die Fehlerpfade `400` und `404` sind über den Browser nicht auslösbar; genau dafür gibt es `KanbanC.Blazor.Tests` (`CLAUDE.md`).

**Integration:** `KontributorenRepositoryTests` (Migration idempotent, Stilllegen/Zurückholen wiederholbar, `LEFT JOIN`, Sortierung mit gemischter Schreibweise, unbekannte Nummer liefert `null`); `KontributorenEndpunkteTests` (`200`/`404`, `stillgelegtAm` in der Liste); `FehlervertragTests` (der `404`-Fall der neuen Route); `WebApiNeustartTests` (Stand samt Datum überlebt den Neustart). Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten.

**E2E:**
- `KontributorStilllegenE2ETests` (neu) — Pausensymbol klicken, Zeile rutscht unter die Gruppenzeile, Zählzeile ändert sich, Reload hält den Stand, „zurückholen" bringt die Zeile zurück nach oben (US-1, US-2, US-4). Das Arrange legt die Kontributoren über die API an (Muster `KontributorenlisteE2ETests`).
- `IdentitaetStillgelegtE2ETests` (neu) — der Stillgelegte fehlt im Popover über und unter der Trennlinie, der Identitätsplatz des ihn Gewählten steht auf „nicht gewählt", nach dem Zurückholen steht er wieder zur Wahl (US-3).
- Bestand: `KontributorAendernE2ETests`, `KontributorAnlegenE2ETests`, `KontributorenlisteE2ETests`, `IdentitaetWaehlenE2ETests` und `IdentitaetGesperrtE2ETests` laufen **unverändert** mit.

## Abhängigkeiten

- Abhängig von: **`R00011`** (Kontributor anlegen). Die WBS-Spalte `Braucht` von `I0009` nennt `I0006`; der Knoten ist `gruen` (`Dokumentation/Planung/kanbanc.md:144`).
- Abhängig von: **`R00013`** (Identität wählen) für den Slice `F0034` — `Braucht` nennt `F0033, I0008`; `I0008` ist `gruen`. `F0034` löst die dort ausdrücklich offen gelassene Auflage ein (`R00013`, „Offen geblieben, weil `I0009` noch rot ist").
- Setzt auf vorhandene Bausteine auf: `Nichtgefunden.Kontributor` (`R00012`), `WebApiAufruf.MitAusfallmeldung` (`R00006`), `Migrationslaeufer` und das Muster `Boardarchivierung` (`R00010`), `Identitaetsspeicher` und `Identitaetsliste` (`R00013`).
- Ändert bestehende Klassen mit grünen Tests: `Kontributor`, `Identitaetsliste`, `Kopfzeile`, `Kontributoren.razor(.css)`, `KontributorenSeite`, `Rahmen` — siehe Akzeptanzkriterien.
- Blockiert: nichts unmittelbar über `Braucht`. Mittelbar aber die zweite Hälfte des Fertig-Kriteriums, die als Zusage an **`I0015`**, **`I0017`** und **`D0006`** weitergegeben wird (siehe „Notizen → Bewusst out of scope").
- **`I0009` ist der letzte rote Slice von `D0002`** — mit ihm wird der Dialog „Kontributoren führen" grün.
- Reihenfolge innerhalb der Anforderung: `F0033` vor `F0034`; `F0034` nennt `F0033` in `Braucht`. Innerhalb `F0033` steht `B0171` (das DTO wächst) früh, weil 52 Aufrufstellen daran hängen — ein späterer Zeitpunkt hieße, sie zweimal anzufassen.

## Umfang

```
Kontributor stilllegen (I0009) = 14 Bubbles: 13 Standard (18,0h), 1 unklar (0,4-1,5h).
Rest: 18,0h klar + 0,4-1,5h unklar · 5 von 14 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 14 Bubbles gruen (0 %) · 0 laufen · 14 offen
```

`I0009` ist bis zur Bubble geplant, in **zwei** Slices:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0033` Kontributor stilllegen und zurückholen | B0170–B0180 (11) | 13,6h klar + 0,4-1,5h unklar | `I0006` |
| `F0034` Stillgelegte verschwinden aus der Identitätswahl | B0181–B0183 (3) | 4,4h klar | `F0033`, `I0008` |

Belegt sind fünf Werte über Vergleichswerte in `Schaetzungen/_ist-zeiten.md`: `B0170` (0,4h, wie `B0002`), `B0172` (0,4h, wie `B0016`), `B0173` (0,4h, wie `B0028`), `B0174` (0,4h, wie `B0029`) und `B0181` (0,4h, wie `B0027`). Die acht Endpunkt-, Klienten-, UI- und E2E-Bubbles tragen den Richtwert 2h ohne Messung. Unklar ist allein `B0171` (0,4-1,5h), weil 52 positionale `new Kontributor(…)` in 10 Dateien mitziehen; die Bandbreite bleibt sichtbar und wird nicht in eine Summe gerechnet. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

Derselbe Vermerk wie bei `I0005` bis `I0008`, damit er nicht als Beifang durchgeht: die 2h-Richtwerte liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde auch hier nicht geändert, weil das die Zählung des ganzen Baums verschöbe; die Frage gehört einmal entschieden, nicht je Slice (`Dokumentation/Planung/kanbanc.md`, Offene Fragen zum Aufwand `I0009`).

## Offene Fragen

- ~~Bekommt `GET /api/kontributoren` jetzt einen Abfrageparameter, wo es einen echten Serverzustand gibt?~~ — entschieden: **nein.** Der Stand steht stattdessen als `stillgelegtAm` in jeder Antwortzeile. Das beantwortet die von `R00013` ausdrücklich hierher weitergereichte Frage zum zweiten Mal mit Nein, jetzt aus einem anderen Grund als dort. Der Unterschied zum Muster `archiviert` an `GET /api/boards` (`R00010`) ist der Schirm: Boards zeigen **entweder** aktive **oder** archivierte über einen Umschalter, die Kontributorenliste zeigt **beide gleichzeitig** unter einer Gruppenzeile (`D0002.dc.html:220-239`) — ein Umschalter ist dort gar nicht gezeichnet. Ein Filter mit Voreinstellung „aktiv" müsste also umgangen oder zweimal aufgerufen werden. Die Kernregel bleibt gewahrt: ein Agent, der wissen will, wer aktiv ist, ruft dieselbe eine Adresse auf und liest dieselbe Auskunft wie die Oberfläche. **Nicht geprüft**, ob eine Liste mit sehr vielen Stillgelegten später doch einen Filter braucht.
- ~~Ist ein stillgelegter Kontributor noch bearbeitbar?~~ — entschieden: **ja, keine Sperre.** Das Artboard zeichnet an der stillgelegten Zeile keinen Stift (`D0002.dc.html:223-239`); das ist **Gestaltung der Zeile**, keine fachliche Regel. Drei Gründe: eine Sperre stünde in keinem Fertig-Kriterium; sie würde einen Tippfehler dauerhaft festschreiben, obwohl derselbe Name laut Interaction „an alten Karten und Zeiten sichtbar" bleiben soll; und die Kernregel wirkt in eine Richtung — die Oberfläche darf weniger können als die API, nicht mehr. `I0007` wird nicht eingeschränkt. **Nicht geprüft**, ob der Mensch den Stift auch an der stillgelegten Zeile sehen möchte.
- ~~Was passiert mit einer gemerkten Identität, die stillgelegt wird?~~ — entschieden: **sie gilt wie „unbekannt"** — der Identitätsplatz fällt auf „nicht gewählt" zurück. Sonst arbeitete jemand weiter unter einer Identität, die gerade aus der Auswahl verschwunden ist, und das Fertig-Kriterium wäre in dem einen Tab, in dem es zählt, nicht erfüllt. Der `sessionStorage` wird **nicht** geleert: ein Serverzustand darf keinen Browserzustand fremder Tabs anfassen; die Id bleibt liegen und trägt wieder, sobald zurückgeholt wird. **Nicht geprüft**, ob der Mensch stattdessen eine Meldung erwartet („deine Identität wurde stillgelegt").
- ~~Kommt das Datum mit?~~ — entschieden: **ja**, `StillgelegtAm` in der Tabelle. Anders als bei `Boardarchivierung`, wo ein `ArchiviertAm` bewusst wegblieb, „solange niemand danach fragt" (`R00010`): hier fragt das Artboard danach — die stillgelegte Zeile trägt „stillgelegt seit 12.08.2026" (`D0002.dc.html:233`). Ein Datum, das nur die Oberfläche erfände, wäre keins. Gespeichert wird **ein** Feld, nicht zwei: ein `IstStillgelegt` wäre daraus ableitbar und damit eine zweite Wahrheit (C17). **Nicht geprüft**, ob die Ausdrücklichkeit eines eigenen Wahrheitsfeldes einem Agenten mehr wert wäre als die Sparsamkeit.
- ~~Wie heißt die Zählzeile im Seitenkopf?~~ — entschieden: **„n aktiv · n stillgelegt"**, nicht „n wählbar · n stillgelegt" wie im Artboard (`D0002.dc.html:107`) — **begründete Abweichung vom Artboard**. Das Artboard widerspricht dort seiner eigenen Zählung: „wählbar" ist laut Wireframe-Index und laut `F0032` **Mensch und nicht stillgelegt**, das wären in der gezeichneten Liste 2 (Stefan, Nina Barth) und nicht 4. Die gezeichnete 4 ist die Zahl der **aktiven** Kontributoren aller drei Arten. Ein Wort mit zwei Bedeutungen verstößt gegen C06; „wählbar" bleibt der Identitätswahl vorbehalten, die Kontributorenseite zählt „aktiv". Die Wireframes werden dafür **nicht** nachgeführt — sie sind Dokumentation, und eine Abweichung gehört in eine Anforderung (`CLAUDE.md`).
- **Offen geblieben, weil nicht Gegenstand dieses Slice:** ob die Spalten „offen", „Zeit" und „letzte Handlung" des Artboards zu `D0002` gehören oder erst mit `D0006`/`D0007` kommen. Das Artboard lässt es ausdrücklich offen (`D0002.dc.html:399`); die gebaute Liste hat drei Spalten (Name, Art, Pflege), die Gruppenzeile bekommt deshalb `colspan="3"` und nicht `colspan="6"`.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Die Migration läuft beim Start; bestehende Datenbanken bekommen die leere Tabelle `Kontributorstilllegung` und damit lauter aktive Kontributoren.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist eine Liste, die nur wachsen kann: seit `R00011` entstehen Kontributoren, seit `R00013` wählt man einen davon als seine Identität — aber niemand geht je wieder heraus, und jeder, der einmal angelegt wurde, steht für immer in der Wahl. Die naheliegende Antwort wäre ein Löschen, und genau die ist falsch: `D0006` (Zeiterfassung), `I0015` (Verantwortlicher) und `I0017` (Kommentar) hängen alle daran, dass ein Urheber nicht verschwindet — ein gelöschter Kontributor macht jede spätere Auswertung „Zeiten je Kontributor" unvollständig, und die Vision verlangt sie vollständig. Wenn stattdessen ein Stilllegungsstand in einer eigenen Tabelle liegt und in jeder Antwortzeile mitgeliefert wird, dann verschwindet der Kontributor aus der Identitätswahl und rutscht in der Pflegeliste unter eine Gruppenzeile — die Wahl bleibt kurz, der Datensatz bleibt da, und der Vorgang ist in beide Richtungen wiederholbar. Der Hebel sitzt genau hier und nicht später, weil jeder Slice, der ab jetzt einen Kontributorbezug anlegt, sonst gegen einen Bestand baut, aus dem Zeilen verschwinden können; und er sitzt nicht früher, weil erst `R00013` die Auswahl geschaffen hat, aus der etwas verschwinden *kann*. Dass der Stand mit einem Datum kommt und in derselben Antwort steht wie Name und Art, ist der Unterschied zwischen „ein Agent kann nachsehen, wer noch mitarbeitet" und „ein Agent muss raten oder eine zweite Route lernen".

## Missing-Docs

- **`DateOnly` über die JSON-Grenze und in SQLite.** Der Bestand kennt bislang kein Datumsfeld in einem Contracts-DTO — `Board` trägt Termine, aber der Umgang mit `DateOnly` in `System.Text.Json` (Serialisierungsform, Rundreise durch `GetFromJsonAsync`) und in Dapper gegen eine SQLite-`TEXT`-Spalte ist im Repository nirgends belegt. Falls die Rundreise nicht auf Anhieb trägt, ist das ein Fall für einen Probe-Test nach `~/.claude/skills/dependency-probe/SKILL.md`, nicht für einen Umbau des DTOs.
- **Anzeigeform des Datums in der Oberfläche.** Das Artboard schreibt „stillgelegt seit 12.08.2026". Wo die Anwendung ihre Kulturformatierung festlegt (`Program.cs`, `CultureInfo`), ist nicht dokumentiert; ein `ToString("d")` unter fremder Kultur ergäbe eine andere Schreibweise als das Artboard.

## Notizen

### Verworfene Alternativen

| Option | Vorteil | Warum verworfen |
|---|---|---|
| **Löschen statt Stilllegen** (`DELETE /api/kontributoren/{id}`) | Die einfachste Lösung: eine Zeile weg, kein zweites Konzept, keine Sortierregel, keine Gruppenzeile. | Sie macht die zweite Hälfte des Fertig-Kriteriums für immer unmöglich und nimmt `D0006`, `I0015` und `I0017` die Grundlage. Ein gelöschter Urheber ist eine Lücke in jeder Auswertung. |
| **Spalte `StillgelegtAm` an `Kontributor` per `ALTER TABLE`** | Ein Join weniger, ein Schema statt zweier Tabellen. | Der `Migrationslaeufer` führt jedes Skript bei jedem Start aus und kennt kein Journal; `ADD COLUMN` scheitert beim zweiten Start. Muster `Boardarchivierung` (`B0126`) und `Boardeinstellung` (`B0108`). |
| **Zwei Felder am DTO** (`bool IstStillgelegt` **und** `DateOnly? StillgelegtAm`) | Für einen Agenten ausdrücklicher: er liest ein Wahrheitsfeld, statt auf `null` zu prüfen. | Zwei Wahrheiten über dieselbe Tatsache, die auseinanderlaufen können (C17). `stillgelegtAm: null` ist die Aussage „aktiv". |
| **Abfrageparameter `stillgelegt` an `GET /api/kontributoren`** | Muster `archiviert` an `GET /api/boards` (`R00010`), symmetrisch zum Bestand. | Die Kontributorenliste zeigt beide Gruppen gleichzeitig; ein Filter müsste umgangen oder zweimal aufgerufen werden. Siehe „Offene Fragen". |
| **Sortierung in der Oberfläche** statt im `ORDER BY` | Kein Eingriff in die bestehende Abfrage. | Eine zweite Wahrheit über die Reihenfolge; der Kommentar `KontributorenRepository.cs:49-50` verbietet sie ausdrücklich, und ein Agent bekäme eine andere Reihenfolge als der Mensch. |
| **Eigener Schirm „Archiv der Kontributoren"** | Die aktive Liste bliebe ganz frei von Stillgelegten. | Das Artboard zeichnet ihn nicht; die WBS kennt keinen zweiten Dialogzustand dafür. Ein erfundener Schirm wäre Umfang, den `/planung` nicht geplant hat. |
| **`sessionStorage` leeren, wenn der Gewählte stillgelegt wird** | Der Browser trüge dann keine tote Id mehr. | Ein Serverzustand darf keinen Browserzustand fremder Tabs anfassen, und nach dem Zurückholen müsste der Mensch erneut wählen. Siehe „Offene Fragen". |

### Bewusst out of scope

- **Die zweite Hälfte des Fertig-Kriteriums von `I0009`: „bleibt aber an alten Karten und Zeiten sichtbar".** Heute nicht prüfbar — `Karte` trägt keinen Kontributorbezug (`Source/KanbanC.Contracts/Karten/Karte.cs:3`), Zeiteinträge gibt es nicht (`D0006` vollständig rot). **Die Zusage geht mit Adresse weiter:** `I0015` (Verantwortlicher an der Karte), `I0017` (Kommentar mit Kontributor und Zeitpunkt) und `D0006` (Zeiterfassung) legen den Kontributorbezug an — **wer ihn dort anlegt, prüft dort auch, dass ein stillgelegter Kontributor an alten Karten und Zeiten sichtbar bleibt.** Diese Anforderung schafft dafür die Voraussetzung, die nicht nachträglich herstellbar wäre: es gibt kein Löschen, also gibt es keinen verwaisten Bezug.
- **Ein Abfrageparameter an `GET /api/kontributoren`** — siehe „Offene Fragen".
- **Eine Sperre der Bearbeitung Stillgelegter** — siehe „Offene Fragen"; `I0007` bleibt unverändert.
- **Die Spalten „offen", „Zeit" und „letzte Handlung"** des Artboards (`D0002.dc.html:399`) — sie gehören `D0006`/`D0007`, nicht diesem Slice. Die Gruppenzeile spannt deshalb über drei Spalten, nicht über sechs.
- **Eine Meldung an den, dessen Identität gerade stillgelegt wurde.** Der Identitätsplatz fällt still auf „nicht gewählt" zurück; eine Benachrichtigung wäre ein Live-Thema (`I0028`).
- **Ein Grund oder eine Notiz zur Stilllegung.** Weder Fertig-Kriterium noch Artboard fragen danach.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Neben den fünf Entscheidungen unter „Offene Fragen" stehen sechs Annahmen mit Beleg:

1. **Die Tabelle heißt `Kontributorstilllegung`, ihr Schlüssel `Kontributor`.** Primärschlüssel `<Tabelle>Id`, Fremdschlüssel tragen den Namen der referenzierten Tabelle (`CLAUDE.md`, „Datenzugriff"); Muster `Boardarchivierung (Board INTEGER PRIMARY KEY REFERENCES Board (BoardId))`.
2. **Die Route ist `PUT /api/kontributoren/{kontributorId}/stilllegung`** — eine Unterressource wie `/archivierung`, `/lage`, `/reihenfolge` und `/kartenzahl`, damit der Board-artige `PUT` auf die Wurzelressource (Name und Art ändern, `R00012`) frei bleibt. Dieselbe Route holt zurück; die Richtung steht im Rumpf.
3. **Das Datum ist ein `DateOnly`, nicht ein Zeitstempel.** Das Artboard zeigt „stillgelegt seit 12.08.2026" ohne Uhrzeit; eine Uhrzeit, die niemand anzeigt, wäre Genauigkeit ohne Leser. Gesetzt wird das Datum beim Stilllegen, nicht beim Zurückholen.
4. **Ein zweites Stilllegen ändert das Datum nicht.** `INSERT … ON CONFLICT DO NOTHING` — das Muster von `SchreibeArchivierung`. „Stillgelegt seit" bezeichnet den Beginn, und ein versehentlicher zweiter Klick darf ihn nicht verschieben. Wer zurückholt und erneut stilllegt, bekommt das neue Datum, weil die Zeile dazwischen verschwunden ist.
5. **Die Zählzeile zählt die geladene Liste, nicht eine eigene Abfrage.** Die Oberfläche hat die vollständige Liste ohnehin; ein zweiter Aufruf nur für zwei Zahlen wäre eine zweite Quelle, die auseinanderlaufen kann.
6. **Das Pausensymbol steht an jeder aktiven Zeile, unabhängig von der Art.** Das Artboard zeichnet es an Mensch, Agent und abgebildeter Person (`D0002.dc.html:138`, `:157`, `:196`, `:215`). Auch ein Agent hört auf mitzuarbeiten; eine Regel „nur Menschen lassen sich stilllegen" steht in keinem Fertig-Kriterium.

Wer eine dieser Annahmen anders will, ändert sie vor dem Bauen — nach `B0171` kostet die Frage nach der Form des Feldes einen zweiten Durchgang durch 52 Aufrufstellen.
