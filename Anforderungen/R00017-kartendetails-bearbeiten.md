---
id: R00017
status: Neu
datum: 2026-09-05
---

# R00017: Kartendetails bearbeiten

## Beschreibung

Eine Karte bekommt eine eigene Seite unter `/karten/{karteId}` und eine eigene Adresse `GET /api/karten/{karteId}`. Dort stehen die sechs Angaben, die eine Karte über ihren Titel hinaus trägt: **Kartenbeschreibung**, **Verantwortlicher**, **Fälligkeit**, **Kartenfarbe** und **Etiketten** — jede änderbar über die Seite wie über `PUT /api/karten/{karteId}` bzw. `PUT /api/karten/{karteId}/etiketten`, und jede nach einem Reload unverändert da. Vom Board führen zwei Wege hin: der Kartentitel und ein zweiter Eintrag „Details öffnen" im `⋯`-Menü der Karte.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche. […] Was ein Mensch klicken kann, kann ein Agent aufrufen."

**Diese Anforderung löst zwei Zusagen früherer Anforderungen ein — beides ausdrücklich, nicht nebenbei:**

1. **Die zweite Hälfte des Fertig-Kriteriums von `I0009`.** `R00014` hat sie mit Adresse hierher weitergegeben: „`I0015` (Verantwortlicher an der Karte) […] legt den Kontributorbezug an — **wer ihn dort anlegt, prüft dort auch, dass ein stillgelegter Kontributor an alten Karten und Zeiten sichtbar bleibt**" (`R00014`, „Bewusst out of scope"). Mit `F0042` gibt es diesen Bezug zum ersten Mal, und damit ist der Satz prüfbar: eine Karte mit stillgelegtem Verantwortlichen **zeigt ihn weiter** (gedämpft, mit dem Zusatz „stillgelegt"), während er in der Auswahl nicht mehr steht. Das steht als eigenes Akzeptanzkriterium und als eigenes E2E-Szenario (US-5), nicht als Randnotiz.
2. **Die Zusage von `I0014`, dass eine archivierte Karte „über API und Archiv auffindbar" bleibt.** `LiesKartendetail` filtert **nicht** nach dem Archivstand — umgekehrt zu `LiesKartenNachPosition` (`R00016`). Eine archivierte Karte ist kein Bestand, aber sie behält ihre Seite und ihre Adresse. Ohne das widerspräche `/karten/{karteId}` genau dem, was `R00016` zugesagt hat.

**Und sie ist an einer Stelle bewusst kleiner als das Fertig-Kriterium naheliegt — auch das nicht still:** **die Bahn wird in diesem Slice nicht eingefärbt.** Das Fertig-Kriterium von `I0015` verlangt „lassen sich ändern und sind nach Reload da", nicht „sind auf der Bahn sichtbar". Farbe und Verantwortlicher **reisen** zwar an `Karte` mit und stehen damit auch in `GET /api/boards/{boardId}` — ein Agent sieht sie also sofort —, aber die **Kartenform auf der Bahn gehört `D0003`**, und eine Änderung dort wäre ein Schirm ohne Skizze (`CLAUDE.md`, „Zieldesign der Oberfläche": vor einem neuen Schirm die Skizze ansehen). **Die Lücke geht mit Adresse weiter:** Farbe und Verantwortlicher auf der Kartenform sind eine **eigene Interaction unter `D0003`** — zuerst `/wireframe verfeinern D0003`, dann `/planung verfeinern D0003`, dann eine eigene Anforderung. Dieselbe Behandlung wie in `R00016` für die Archivsicht und in `R00014` für die zweite Hälfte von `I0009`. Diese Anforderung schafft dafür die Voraussetzung, die nicht nachträglich herstellbar wäre: die Werte liegen an `Karte` und nicht an einem zweiten Record, die Bahn muss dafür später nichts nachladen.

## Geschäftlicher Nutzen

Eine Karte kann heute genau eine Sache sagen: ihren Titel. Alles, was eine Aufgabe im Alltag ausmacht — was zu tun ist, bis wann, wer, und woran man sie in einer vollen Bahn wiedererkennt —, hat keinen Ort. Für den Menschen heißt das, dass der Titel alles tragen muss und dabei unlesbar wird; für den Agenten heißt es, dass er eine Aufgabe anlegen, aber nicht beschreiben kann, und dass er niemandem etwas zuweisen kann, obwohl es Kontributoren seit `R00011` gibt. Genau diese Lücke ist die Ursache dafür, dass die Kontributorenverwaltung bisher folgenlos blieb: sie führt Menschen und Agenten, aber nichts im System zeigt auf sie. Mit dem Verantwortlichen bekommt sie ihren ersten Verbraucher.

Der zweite Nutzen ist die **Adresse**. `/karten/14` ist teilbar, sie überlebt einen Reload, und sie ist die Form, in der die API die Karte ohnehin schon führt — dieselbe Gestalt für Mensch und Agent, statt einer Schublade, die nur im Browser existiert. Sie ist zugleich der Träger, auf dem `D0004` weiterbaut: Subtasks (`I0016`), Kommentare (`I0017`), Anhänge (`I0018`) und Verweise (`I0019`) hängen alle an ihr, und `D0005`/`D0006` speisen dieselbe Seite. Ohne sie hätte keines dieser fünf Folgethemen einen Ort.

## Funktionale Anforderungen

- `GET /api/karten/{karteId}` liefert das **Kartendetail**: die Karte, ihr Board mit Namen, ihre Spalte mit Bezeichnung, den Verantwortlichen mit Name und Art, die Etiketten der Karte und die Etikettvorschläge ihres Boards.
- Die Route `/karten/{karteId}` in der Oberfläche zeigt dasselbe: Rückweg zum Board, Brotkrumen, Titel als Überschrift und das Eigenschaftenblatt.
- `PUT /api/karten/{karteId}` ändert Titel, Kartenbeschreibung, Fälligkeit, Kartenfarbe und Verantwortlichen in einem Aufruf und antwortet mit dem vollständigen Kartendetail.
- `PUT /api/karten/{karteId}/etiketten` setzt die **ganze** Etikettenliste der Karte und antwortet ebenso mit dem Kartendetail.
- Vom Board führen zwei Wege auf die Kartenseite: der Kartentitel als Verweis und der Eintrag „Details öffnen" im `⋯`-Menü der Karte.
- Ein geleerter Titel wird mit dem Wortlaut des `KartenValidator` zurückgewiesen; gespeichert wird nichts.
- Wählbar als Verantwortlicher sind alle **nicht stillgelegten** Kontributoren jeder Art — **auch abgebildete** —, dazu der Eintrag „niemand"; ein bereits gesetzter, inzwischen stillgelegter Verantwortlicher bleibt an der Karte sichtbar.
- Ein Etikett ist eine freie Textmarke an der Karte; beim Tippen wird aus dem Etikettenbestand des Boards vervollständigt, mit der Zahl der Karten je Text.
- Ein Etikett, das keine Karte mehr trägt, verschwindet aus dem Bestand des Boards, ohne dass jemand es aufräumt.
- Eine unbekannte Kartennummer beantwortet die API mit Befund und die Oberfläche mit einer lesbaren Meldung samt Rückweg — kein Absturz.
- Eine **archivierte** Karte behält ihre Seite und ihre Adresse.

## Nicht-funktionale Anforderungen

- **Datenhaltung:** Die Migrationen `010-karteneigenschaft.sql` und `011-kartenetikett.sql` sind idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal (`Source/KanbanC.BL/Persistenz/Migrationen/Migrationslaeufer.cs:16-23`). Deshalb eigene Tabellen statt `ALTER TABLE Karte ADD COLUMN`, wie bei den Migrationen 004, 005, 007, 008 und 009. `003-karten.sql` bleibt unangetastet und sagt genau das voraus (`003-karten.sql:1-3`).
- **Datumsablage:** `FaelligAm` steht als ISO-Text (`yyyy-MM-dd`) in der Spalte und wird in C# umgerechnet — Dapper nimmt `DateOnly` nicht als Parameterwert (belegt durch `SqliteEigenschaftenTests.cs:89-131`). Dasselbe Verfahren wie bei `ErledigtAm` (`Kartenleser.cs:58-66`).
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der drei neuen Routen trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, auch bei 404. **Jede neue Route bringt ihren Vertragsfall im selben Arbeitsgang mit** (`FehlervertragTests.cs:53-56`; Lehre aus `B0152`/`B0159`).
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche"). Die fünf Kartenfarben sind Token-Werte, keine erfundenen Farben. Symbole als Inline-SVG.
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier keine Projektreferenz auf `KanbanC.BL`; die Kartenseite spricht ausschließlich über HTTP.
- **Datumsdarstellung:** Die Fälligkeit wird über den bestehenden `Terminformatierer` angezeigt (ISO-Text, `—` wenn keiner) — kein zweites Datumsformat in der Anwendung.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün, mit **einer benannten Ausnahme** — den drei E2E-Zusicherungen auf genau einen Menüeintrag (siehe Akzeptanzkriterien).

## Akzeptanzkriterien

### Die Kartenseite und ihre Adresse (F0040)

- [ ] `GET /api/karten/{karteId}` antwortet mit HTTP 200 und einem Kartendetail, das Karte, Board mit Namen, Spalte mit Bezeichnung, Etiketten und Etikettvorschläge trägt — **ohne** `boardId` in der Adresse.
- [ ] Die bestehenden Kartenrouten unter `/api/boards/{boardId}/…` bleiben unverändert: Adresse, Verb und Antwortgestalt von Anlegen, Zug, Archivierung und Kartenliste ändern sich nicht.
- [ ] Eine **unbekannte** `karteId` beantwortet die Route mit HTTP 404 **und einem Rumpf**: ein Befund mit nichtleerem `code`, einer `meldung`, welche die aufgerufene Nummer nennt, und einer `kompensation` mit einem ausführbaren nächsten Aufruf. Der Befund nennt **kein** Board — die Route kennt keins, und eine erfundene Nummer wäre eine Falschaussage.
- [ ] Eine **archivierte** Karte liefert unter derselben Adresse dasselbe Kartendetail wie zuvor. Rechenbeispiel: Karte `B` archivieren → `GET /api/boards/{boardId}` zeigt sie nicht, `GET /api/karten/{karteIdVonB}` antwortet weiterhin mit 200.
- [ ] `/karten/{karteId}` in der Oberfläche zeigt Rückpfeil, Boardnamen, die Plakette „Karte n", Brotkrumen und den Titel als Überschrift.
- [ ] Der Rückpfeil führt auf `/boards/{boardId}` des Boards, zu dem die Karte gehört.
- [ ] Ein Direktaufruf von `/karten/{karteId}` und ein Reload zeigen dieselbe Karte — die Seite lädt aus der Adresse, nicht aus einem übergebenen Zustand.
- [ ] `/karten/9999` zeigt eine lesbare Meldung mit der Nummer und einen Rückweg, keine Ausnahmeseite. Wortlaut analog `Board.razor` („Eine Karte mit der Nummer 9999 gibt es nicht.").
- [ ] Ist die WebApi nicht erreichbar, erscheint die Ausfallmeldung statt einer Ausnahmeseite (`WebApiAufruf.MitAusfallmeldung`).
- [ ] Auf dem Board ist der Kartentitel ein Verweis auf `/karten/{karteId}`; ein Klick darauf **löst keinen Ziehvorgang aus**.
- [ ] Das `⋯`-Menü der Karte trägt danach **zwei** Einträge: „Details öffnen" und „Archivieren". Beide führen dieselbe Wirkung aus wie zuvor bzw. wie der Titelverweis.
- [ ] Die Karte bleibt ziehbar wie zuvor: Kartenhälften, Einfügelinie und Zielposition verhalten sich unverändert (`R00008`).

### Titel, Beschreibung, Fälligkeit und Farbe (F0041)

- [ ] `PUT /api/karten/{karteId}` mit Titel, Beschreibung, Fälligkeit und Farbe antwortet mit HTTP 200 und dem **vollständigen Kartendetail** — nicht nur mit der Karte; dieselbe Überlegung wie bei `PUT …/lage`, das die Spalten zurückgibt (`KartenService.cs:101-103`).
- [ ] Nach einem erneuten `GET /api/karten/{karteId}` stehen alle vier Werte unverändert da; nach einem Neustart der WebApi auf derselben Datei ebenso.
- [ ] Beschreibung, Fälligkeit und Farbe reisen an `Karte` mit und stehen damit auch in `GET /api/boards/{boardId}`, in `GET …/spalten/{spalteId}/karten` und in der Antwort von `PUT …/lage`.
- [ ] Ein **geleerter Titel** wird mit HTTP 400 und dem Befund `kartentitel-leer` zurückgewiesen; die Meldung lautet wörtlich „Der Titel darf nicht leer sein." — derselbe Satz wie beim Anlegen. **Gespeichert wird nichts:** nach der Zurückweisung sind alle vier Werte unverändert.
- [ ] Die **Kompensationsaktion nennt die Route, die der Aufrufer wirklich gerufen hat**: `POST /api/boards/{boardId}/spalten/{spalteId}/karten` beim Anlegen, `PUT /api/karten/{karteId}` beim Ändern. Beide Wortlaute stammen aus **einer** Quelle, dem `KartenValidator`.
- [ ] Ein zu langer Titel (> 1000 Zeichen) wird mit `kartentitel-zu-lang` zurückgewiesen, ebenfalls mit der Route des Aufrufers in der Kompensation.
- [ ] Die Kartenfarbe kennt genau fünf Werte; ein anderer Wert wird mit Befund zurückgewiesen. Voreinstellung einer Karte ohne gesetzte Farbe ist „ohne".
- [ ] Eine **frisch angelegte Karte** liefert `beschreibung: null`, `faelligAm: null` und `farbe: "Ohne"` — ohne dass jemand vorher eine Eigenschaftszeile angelegt hat.
- [ ] Auf der Kartenseite zeigen leere Felder eine **Handlung statt einer Null**: „Beschreibung hinzufügen" statt eines leeren Kastens, `—` bei Fällig.
- [ ] Titel, Beschreibung, Fälligkeit und Farbe lassen sich auf der Kartenseite ändern; nach einem Reload stehen alle vier so da, wie sie gesetzt wurden.
- [ ] Wird der Titel auf der Seite geleert, erscheint die Zurückweisung als Meldung und **die vorige Fassung bleibt stehen**.
- [ ] Ein zweiter Lauf der Migration `010` auf einer bestehenden Datei lässt Schema **und** Daten unverändert; gesetzte Eigenschaften bleiben stehen.

### Der Verantwortliche (F0042)

- [ ] `PUT /api/karten/{karteId}` setzt den Verantwortlichen über den Fremdschlüssel `kontributor`; `null` bedeutet „niemand" und ist ein gültiger Wert, kein Fehler.
- [ ] Das Kartendetail trägt den Verantwortlichen mit **Name und Art**, nicht nur mit seiner Nummer; `Karte` selbst trägt die Nummer.
- [ ] Nach einem Reload und nach einem Neustart der WebApi steht derselbe Verantwortliche da.
- [ ] Eine **unbekannte** Kontributornummer wird mit Befund zurückgewiesen (Grund, Nummer, Kompensationsaktion); nichts wird gespeichert.
- [ ] Eine **stillgelegte** Kontributornummer wird ebenso mit Befund zurückgewiesen — ein eigener Code, weil es eine andere Lage ist als „gibt es nicht"; nichts wird gespeichert.
- [ ] **Abgebildete Kontributoren sind wählbar.** Rechenbeispiel: Bestand mit 1 Mensch (aktiv), 1 Agent (aktiv), 1 Abgebildetem (aktiv), 1 Menschen (stillgelegt) → die Auswahl zeigt „niemand" plus 3 Einträge, nicht 1 und nicht 4.
- [ ] **Ein stillgelegter Verantwortlicher bleibt an der Karte sichtbar** — gedämpft und mit dem Zusatz „stillgelegt" — und ist gleichzeitig **nicht mehr wählbar**. Rechenbeispiel: Karte mit Verantwortlichem `Jan R.`, `Jan R.` wird stillgelegt → die Karte zeigt weiter `Jan R.`, die Auswahlliste enthält ihn nicht mehr. **Das ist die Einlösung der zweiten Hälfte des Fertig-Kriteriums von `I0009`** und wird durch einen E2E-Test belegt, nicht behauptet.
- [ ] Die Auswahl auf der Kartenseite trägt ein Suchfeld, je Eintrag eine Art-Plakette und den Eintrag „niemand".
- [ ] Wird „niemand" gewählt, hat die Karte danach keinen Verantwortlichen mehr, und das übersteht einen Reload.

### Etiketten (F0043)

- [ ] `PUT /api/karten/{karteId}/etiketten` setzt die **ganze** Liste: die übergebene Liste ist danach exakt die Liste der Karte, gleichgültig wie sie vorher aussah. Rechenbeispiel: Karte mit `Import`, `Doku` → `PUT` mit `["Doku", "Refactoring"]` → die Karte trägt `Doku` und `Refactoring`, nicht drei Etiketten.
- [ ] Eine leere Liste ist gültig und nimmt der Karte alle Etiketten.
- [ ] Die Etiketten stehen nach einem Reload und nach einem Neustart der WebApi unverändert da.
- [ ] Die Etiketten reisen **im Kartendetail**, nicht an `Karte`: `GET /api/boards/{boardId}` bleibt unverändert und bekommt keine Etikettenliste je Karte.
- [ ] Das Kartendetail trägt die **Etikettvorschläge des Boards** mit der Zahl der Karten je Text. Rechenbeispiel: auf dem Board tragen 7 Karten das Etikett `Refactoring` und 1 Karte `Refaktorierung` → die Vorschläge nennen `Refactoring · 7` und `Refaktorierung · 1`.
- [ ] Vorschläge stammen **nur** aus dem Board der Karte; Etiketten anderer Boards erscheinen nicht.
- [ ] **Ein Etikett, das keine Karte mehr trägt, ist aus dem Bestand fort.** Rechenbeispiel: zwei Karten desselben Boards, nur Karte `A` trägt `Import` → `Import` von `A` entfernen → die Vorschläge an Karte `B` enthalten `Import` nicht mehr. Kein Aufräumschritt und kein Pflegeschirm.
- [ ] Ein **leerer** Etikettentext, ein **zu langer** und ein **doppelter** Text (nach Normalisierung der Randleerzeichen) werden mit Befund zurückgewiesen; nichts wird gespeichert.
- [ ] Zwei Texte, die sich nur in ihrer Schreibweise unterscheiden (`Refactoring`, `Refaktorierung`), sind **zwei** Etiketten und kein Befund — die Vervollständigung macht abweichende Schreibweisen sichtbar, sie verhindert sie nicht.
- [ ] Auf der Kartenseite lässt sich ein Etikett tippen und anlegen, eines aus der Vorschlagsliste übernehmen und eines über `✕` entfernen; die Vorschlagsliste zeigt die Kartenzahl je Text und den Eintrag „… neu anlegen".
- [ ] Ein zweiter Lauf der Migration `011` auf einer bestehenden Datei lässt Schema **und** Daten unverändert.

### Fehlerantworten für Agenten (F0040–F0043)

- [ ] Alle drei neuen Routen antworten bei unbekannter `karteId` mit HTTP 404 **und Rumpf**; keine liefert einen leeren Körper.
- [ ] `PUT /api/karten/{karteId}` und `PUT …/etiketten` antworten bei ungültiger Eingabe mit HTTP 400 **und Rumpf**.
- [ ] Der Vertragstest über alle registrierten Routen bleibt grün: alle drei neuen Routen werden von ihm abgerufen und sind nicht als ungeprüft übrig (`FehlervertragTests.cs:53-56`).
- [ ] Jede Kompensation nennt einen Aufruf, den ein Agent ohne weitere Auskunft absetzen kann — bei der boardlosen Kartenroute also einen Weg, der bei `GET /api/boards` beginnt.

### Der grüne Bestand bleibt grün — mit einer benannten Ausnahme

- [ ] **Benannte Ausnahme:** Drei grüne Zusicherungen nageln das `⋯`-Menü auf **genau einen** Eintrag fest und ziehen mit dem zweiten Eintrag mit — `KartenmenueE2ETests.cs:30`, `KarteArchivierenE2ETests.cs:60` und `:104`. Sie erwarten danach **zwei** Einträge in der gezeichneten Reihenfolge („Details öffnen", „Archivieren"). Das ist eine Änderung an grünem Bestand, keine Nebensache; sie steht hier, damit sie nicht als Beifang durchgeht.
- [ ] `KartenmenueE2ETests` bleibt im Übrigen unverändert grün: Öffnen und Schließen des Menüs, kein Ziehvorgang beim Öffnen, das Menü über den Kartenhälften.
- [ ] `KarteVerschiebenE2ETests`, `EinfuegelinieE2ETests`, `AbschlussbahnAblageE2ETests` und `KartenzahlImBahnenkopfE2ETests` bleiben **ohne Änderung** grün: sie zählen Karten, Kartenhälften und ziehbare Karten, und weder der Titelverweis noch der zweite Menüeintrag dürfen eine dieser Zählungen verschieben.
- [ ] `KarteAnlegenE2ETests` bleibt ohne Änderung grün — insbesondere die Zurückweisung „Der Titel darf nicht leer sein." beim Anlegen.
- [ ] `GekuerzteAbschlussspalteTests`, `BahnenkopfzahlTests`, `AbschlussbahnTests` und `DatumsgruppenTests` bleiben grün, obwohl `Karte` um Felder wächst; die 16 positionalen `new Karte(…)` werden angepasst, ihre Zusicherungen nicht.
- [ ] Der Fehlervertrag der bestehenden Routen bleibt unverändert: `Nichtgefunden.Karte(boardId, karteId)` behält Wortlaut und Kompensation für die Routen unter dem Board.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/010-karteneigenschaft.sql` und `011-kartenetikett.sql` — neue, idempotente Migrationen.
- **Contracts:** `Source/KanbanC.Contracts/Karten/Karte.cs` (wächst um Beschreibung, Fälligkeit, Farbe und Verantwortlichen), neu `Kartendetail.cs`, `KarteAendernAnfrage.cs`, `Kartenetiketten.cs`, `Etikettvorschlag.cs`, `Kartenfarbe.cs`.
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Karten/KartenValidator.cs` (zweite Überladung, Route als Parameter), neu `Source/KanbanC.BL/Operations/Karten/EtikettenValidator.cs` und `Source/KanbanC.BL/Operations/Kontributoren/Verantwortlichenliste.cs`; `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` (Schwester ohne Board, Stilllegungsbefund).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs` (beide bestehenden `SELECT`s wachsen um die Eigenschaften, `LiesKartendetail` kommt hinzu — **ohne** Archivfilter), neu `Source/KanbanC.BL/Persistenz/Karten/Etikettenleser.cs`, `Source/KanbanC.BL/Persistenz/Karten/KartenRepository.cs` (`Aendere`, `SetzeEtiketten`), `Source/KanbanC.BL/Interfaces/Karten/IKartenRepository.cs`.
- **Dienste:** `Source/KanbanC.BL/Integrations/Karten/KartenService.cs` — `LadeKartendetail`, `AendereKarte`, `SetzeEtiketten`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenEndpunkte.cs` — drei neue Routen ohne Board in der Adresse.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenApiKlient.cs` (`LadeKartendetail`, `AendereKarte`, `SetzeEtiketten`), neu `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor` (Titelverweis, zweiter Menüeintrag).
- **Unberührt:** `Source/KanbanC.BL/Operations/Karten/Abschlussbahn.cs`, `KartenlageValidator.cs`, `Erledigungsstand.cs`, `Source/KanbanC.BL/Operations/Boards/Archivfilter.cs`, `Source/KanbanC.Blazor/Components/Spalten/Spaltenbahnen.razor` (bis auf das Durchreichen, falls die Karte einen neuen Parameter braucht) — die Bahn wird in diesem Slice **nicht** eingefärbt.
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Karten/`, `Operations/Kontributoren/`, `Integrations/Karten/`, `TestHelpers/TestKartenRepository.cs`, `TestHelpers/TestSpaltenRepository.cs`), `Source/KanbanC.Blazor.Tests/Services/KartenApiKlientTests.cs`, `Source/KanbanC.WebApi.IntegrationTests/` (`Api/KartenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Persistenz/Karten/KartenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (neues Seitenobjekt `KartendetailSeite`, `PageObjects/BoardSeite.cs`, vier neue Testklassen).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0004.dc.html`](../Dokumentation/Wireframes/D0004.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gelten daraus: **Zustand 1** (die Kartenseite, Zone 1 der Kopfzeile, Brotkrumen, Titel als Überschrift, Eigenschaftenblatt rechts), **Zustand 2** (Verantwortlichenwahl als Popover mit Suchfeld und Art-Plaketten), **Zustand 3** (Etikettenfeld mit Vervollständigung und die fünf Farbpunkte), **Zustand 4** (Leerzustand der frisch angelegten Karte), die drei **Ränder** (unbekannte Karte, geleerter Titel, stillgelegter Verantwortlicher) und der Ausschnitt **Einstieg vom Board**. Die gestrichelten Kästen („Klasse und Nummer", „Zeiten und Timer") gehören `D0005` und `D0006` und bleiben leer. Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

Zwei Stellen des Artboards gehören ausdrücklich **nicht** in diesen Slice: der **Archivierungsknopf** in den Brotkrumen trägt selbst den Vermerk `I0014 · D0003` (`D0004.dc.html:130-131`), und `I0014` ist grün — er käme über `/planung aendern I0014` oder eine eigene Anforderung; die **Adresse `/karten/WBS-14`** des älteren Satzes setzt die Klasse aus `D0005` voraus und ist hier `/karten/14`.

### Ablauf

1. **Kartendetail lesen** (`GET /api/karten/{karteId}`)
   - 1.1 `KartenEndpunkte` ruft `KartenService.LadeKartendetail(karteId)`
   - 1.2 `Kartenleser.LiesKartendetail` liest über `Karte → Spalte → Board` (zwei JOINs) plus `LEFT JOIN Karteneigenschaft` und `LEFT JOIN Kontributor`; **kein Archivfilter**
     - 1.2.1 `null` → `Nichtgefunden` ohne Board → HTTP 404 mit Rumpf
   - 1.3 `Etikettenleser` liefert die Etiketten der Karte und die Etikettvorschläge des Boards (`Etikett JOIN Karte JOIN Spalte`, gruppiert nach Text mit `COUNT`)
   - 1.4 Zusammensetzung zum `Kartendetail` → HTTP 200
2. **Karte ändern** (`PUT /api/karten/{karteId}`)
   - 2.1 `KartenValidator.Pruefe(karteId, anfrage)` → leerer Titel, zu langer Titel, unbekannte Farbe → HTTP 400 mit Rumpf
   - 2.2 `KartenService.AendereKarte` prüft den Verantwortlichen gegen den Kontributorenbestand — die Prüfung braucht den Bestand und sitzt deshalb im Dienst, nicht im Validator (dieselbe Trennung wie beim Zug)
     - 2.2.1 unbekannte Nummer → `Nichtgefunden.Kontributor`; stillgelegte Nummer → eigener Befund
   - 2.3 `KartenRepository.Aendere` in **einer** Transaktion: `UPDATE Karte SET Titel` und `INSERT INTO Karteneigenschaft … ON CONFLICT DO UPDATE` (Muster `SchreibeErledigung`, `KartenRepository.cs:172-206`); `null` bei unbekannter Karte
   - 2.4 Rücklesen als `Kartendetail` → HTTP 200; die Seite behält damit **eine** Quelle und lädt nicht nach
3. **Etiketten setzen** (`PUT /api/karten/{karteId}/etiketten`)
   - 3.1 `EtikettenValidator.Pruefe` → leerer Text, zu langer Text, doppelter Text nach Normalisierung → HTTP 400 mit Rumpf
   - 3.2 `KartenRepository.SetzeEtiketten`: `DELETE` aller Zeilen der Karte, `INSERT` der ganzen Liste, eine Transaktion — dasselbe Muster wie `SpaltenRepository.SetzeReihenfolge`. Ein Text ohne Karte verschwindet damit von selbst aus dem Bestand
   - 3.3 Rücklesen als `Kartendetail` → HTTP 200
4. **Die Kartenseite**
   - 4.1 `Kartendetail.razor` mit `@page "/karten/{KarteId:long}"` lädt beim `OnParametersSetAsync` über `KartenApiKlient`, umschlossen von `WebApiAufruf.MitAusfallmeldung` — Muster `Board.razor`
   - 4.2 `null` → Meldung mit Nummer und Rückweg; `HttpRequestException` → Ausfallmeldung
   - 4.3 Änderung eines Feldes → `AendereKarte` → das gelieferte Kartendetail ist die neue Quelle der Anzeige
   - 4.4 Zurückweisung → Meldung, die vorige Fassung bleibt stehen
5. **Einstieg vom Board**
   - 5.1 `Karte.razor`: der Titel wird ein `<a href="/karten/@Kartendaten.KarteId">`; er darf `@ondragstart` des `<article>` **nicht** auslösen
   - 5.2 das `⋯`-Menü bekommt „Details öffnen" **vor** „Archivieren", mit derselben Adresse

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenEndpunkte`** — die drei neuen Routen sitzen erstmals unter `/api/karten/{karteId}` **ohne** Board. Der Grund ist die Oberfläche: ein Browser, der `/karten/14` öffnet, kennt das Board noch nicht — es steht erst in der Antwort. `KartenRepository.BoardDerKarte` (`KartenRepository.cs:121`) belegt, dass die Kartennummer dafür genügt.
- **`Kartenleser`** — der eine Ort, an dem eine Karte zur Karte wird; hier entscheidet sich, welche Felder überall mitreisen und welche nur am Detail hängen.
- **`Migrationslaeufer`** — die zehnte und elfte Migration reihen sich ein; kein Journal, also idempotent.
- **`Karte.razor`** — die Karte auf der Bahn bekommt ihren zweiten Weg nach außen.

**Klassen-Entwurf:**

- `Karte` (DTO, immutable) — wächst um vier Werte; **kein zweiter Record neben ihr**, weil zwei Records für dieselbe Sache zwei Wahrheiten wären (C06).
  - `record Karte(long KarteId, string Titel, int Position, DateOnly? ErledigtAm, string? Beschreibung, DateOnly? FaelligAm, Kartenfarbe Farbe, long? Kontributor)`
- `Kartendetail` (DTO, immutable) — **Zusammensetzung, nicht Verdopplung**: die Karte plus ihren Ort und das, was nur hier gebraucht wird.
  - `record Kartendetail(Karte Karte, long Board, string Boardname, long Spalte, string Spaltenbezeichnung, Kontributor? Verantwortlicher, IReadOnlyList<string> Etiketten, IReadOnlyList<Etikettvorschlag> Etikettvorschlaege)`
  - Der Verantwortliche reist als **`Kontributor`**, nicht als eigener Record: derselbe Begriff, dieselbe Schreibweise (C06), und `StillgelegtAm` liefert der Oberfläche den Zusatz „stillgelegt" ohne ein zweites Feld.
- `Etikettvorschlag` (DTO, immutable) — ein Text des Boards mit der Zahl der Karten, die ihn tragen.
  - `record Etikettvorschlag(string Text, int Kartenzahl)`
- `Kartenfarbe` (Aufzählung) — `Ohne`, `Sand`, `Terrakotta`, `Olive`, `Nebel`; als Text in der Spalte, wie `BoardArt` und `Kontributorart`.
- `KarteAendernAnfrage` (DTO, immutable) — die vier Skalarfelder und der Verantwortliche in **einer** Anfrage, weil sie in einem Blatt geändert werden.
  - `record KarteAendernAnfrage(string Titel, string? Beschreibung, DateOnly? FaelligAm, Kartenfarbe Farbe, long? Kontributor)`
- `Kartenetiketten` (DTO, immutable) — die ganze Liste, nicht ein Zugang.
  - `record Kartenetiketten(IReadOnlyList<string> Etiketten)`
- `KartenValidator` (Operation, pure Logik) — bekommt eine zweite Überladung und **die Route als Parameter**, damit die Kompensation den Aufrufer dort abholt, wo er steht. Vorbilder: `KontributorenValidator` (`:11-21`) und `Boardname` (`:22`).
  - `static Pruefbefunde Pruefe(KarteAnlegenAnfrage anfrage)`
  - `static Pruefbefunde Pruefe(long karteId, KarteAendernAnfrage anfrage)`
- `EtikettenValidator` (Operation, pure Logik) — leerer Text, zu langer Text, doppelter Text nach Normalisierung der Randleerzeichen.
  - `static Pruefbefunde Pruefe(Kartenetiketten etiketten)`
- `Verantwortlichenliste` (Operation, pure Logik) — die Wählbaren einer Karte: alle nicht Stillgelegten jeder Art, „niemand" als Eintrag, ein stillgelegter Träger getrennt ausgewiesen. **Schwester** von `Identitaetsliste.Waehlbare` — dieselbe Stilllegungsregel, ein anderer Zweck; deshalb eine eigene Operation statt einer zweiten Bedeutung an derselben.
- `Etikettenleser` (Provider, Ressourcenzugriff) — Etiketten einer Karte und Vorschläge eines Boards; die Kartenzahl ist eine `COUNT`-Spalte, kein gezählter Client.
- `IKartenRepository` / `KartenRepository` (Provider) — `null` heißt „diese Karte gibt es nicht".
  - `Kartendetail? LiesKartendetail(long karteId)`
  - `Kartendetail? Aendere(long karteId, KarteAendernAnfrage anfrage)`
  - `Kartendetail? SetzeEtiketten(long karteId, Kartenetiketten etiketten)`
- `KartenService` (Integration) — verdrahtet; prüft den Verantwortlichen gegen den Kontributorenbestand.
  - `Ergebnis<Kartendetail> LadeKartendetail(long karteId)`
  - `Ergebnis<Kartendetail> AendereKarte(long karteId, KarteAendernAnfrage anfrage)`
  - `Ergebnis<Kartendetail> SetzeEtiketten(long karteId, Kartenetiketten etiketten)`
- `KartenApiKlient` (Integration, Blazor) — drei HTTP-Wege; Muster `LadeKartenDerSpalte` (`KartenApiKlient.cs:25`) und `SpaltenApiKlient.Aendere`.
  - `Task<ApiErgebnis<Kartendetail>> LadeKartendetail(long karteId)`
  - `Task<ApiErgebnis<Kartendetail>> AendereKarte(long karteId, KarteAendernAnfrage anfrage)`
  - `Task<ApiErgebnis<Kartendetail>> SetzeEtiketten(long karteId, Kartenetiketten etiketten)`
- `Kartendetail` (Blazor-Seite) — `@page "/karten/{KarteId:long}"`; Kopfzeile über `SectionContent` wie `Board.razor`, Meldung samt Rückweg wortgleich.
- **Migration** `010-karteneigenschaft.sql` (Skript, idempotent) — **eine** Tabelle für vier Werte, Muster `Boardeinstellung` (`004`), nicht Muster `Karteerledigung` (eine Tatsache, eine Tabelle): die vier werden immer im selben Formular geändert und immer zusammen gelesen.
  ```sql
  CREATE TABLE IF NOT EXISTS Karteneigenschaft
  (
      Karte        INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
      Beschreibung TEXT    NULL,
      Kontributor  INTEGER NULL REFERENCES Kontributor (KontributorId),
      FaelligAm    TEXT    NULL,
      Farbe        TEXT    NOT NULL
  );
  ```
  Die Spalte `Kontributor` entsteht hier mit und wird erst in `F0042` gefüllt — **totes Schema für die Dauer eines Features**; das ist der ausdrückliche Preis der einen Tabelle und steht unter „Offene Fragen".
- **Migration** `011-kartenetikett.sql` (Skript, idempotent) — der zusammengesetzte Schlüssel verhindert dasselbe Etikett zweimal an einer Karte, ohne dass jemand prüft.
  ```sql
  CREATE TABLE IF NOT EXISTS Etikett
  (
      Karte INTEGER NOT NULL REFERENCES Karte (KarteId),
      Text  TEXT    NOT NULL,
      PRIMARY KEY (Karte, Text)
  );
  ```

### Änderungen an bestehenden Klassen

- `Karte` (`Source/KanbanC.Contracts/Karten/Karte.cs:6`) — vier neue Werte. **Änderung an grünem Bestand mit Breitenwirkung:** **16 positionale `new Karte(…)` in 9 Dateien** ziehen mit (`TestKartenRepository.cs`, `TestSpaltenRepository.cs`, `AbschlussbahnTests.cs`, `BoardServiceTests.cs`, `KartenServiceTests.cs`, `DatumsgruppenTests.cs`, `BahnenkopfzahlTests.cs`, `Kartenleser.cs`, `KartenRepository.cs`). Die neuen Werte werden **benannt** eingetragen (Muster `StillgelegtAm: null` aus `R00014`), nicht als nacktes `null` — sonst ist beim nächsten Lesen nicht erkennbar, welches `null` welches Feld ist.
- `Kartenleser` (`:15-51`) — beide bestehenden `SELECT`s bekommen `LEFT JOIN Karteneigenschaft`, `AlsKarte` die vier Werte; `LiesKartendetail` kommt hinzu und filtert **nicht** nach dem Archivstand. **Änderung an grünem Bestand.**
- `KartenValidator` (`:10` Konstante, `:24` Signatur) — die Route wird Parameter, die zweite Überladung kommt hinzu, Befundtexte bleiben. **Änderung an grünem Bestand**, sichtbar in `KartenValidatorTests`. Kein grüner Test nagelt heute den Kompensationstext dieser beiden Befunde fest (geprüft am 2026-09-05: `Befundpruefung.ErwarteVollstaendigenBefund` prüft nur auf nichtleer).
- `Nichtgefunden` (`:25`) — `Karte(long boardId, long karteId)` bekommt eine **Schwester ohne Board**, weil die boardlose Route keins kennt und ein erfundener Wert eine Falschaussage wäre. Dazu ein Befundcode für den **stillgelegten** Kontributor neben dem bestehenden `Nichtgefunden.Kontributor` (`:57`). Beide Codes gehören in `AlleCodes`, damit `MeldetEinFehlendesDing` weiterhin über 404 und 400 entscheidet — der Stilllegungsbefund ist allerdings **kein fehlendes Ding** und gehört deshalb zu 400, nicht zu 404.
- `KartenEndpunkte` — drei neue Routen und eine zweite Routenkonstante ohne Board. **Der Vertragsfall jeder Route gehört in denselben Arbeitsgang wie die Route**: `FehlervertragTests.cs:53-56` liest die registrierten Routen aus dem Testhost, und zwischen Route und Vertragsfall ist die Suite rot.
- `Karte.razor` (`:14-21` Menü, `:24` Titel) — der Titel wird ein Verweis, das Menü bekommt einen zweiten Eintrag. **Zwei Fallstricke, beide schon einmal aufgetreten:** der Verweis darf den `@ondragstart` des `<article>` nicht auslösen (derselbe Konflikt wie bei `B0207`, dazu die Probe `KindZiehbarkeitProbeE2ETests`), und ein zweiter Eintrag samt zweiter Erläuterungszeile macht aus `MenuehinweisDerKarte` (`BoardSeite.cs:274-277`) einen Locator mit **zwei** Treffern — `ToHaveTextAsync(string)` schlägt dort um in `ToHaveTextAsync(string[])`.
- `BoardSeite` (`:254-287`) — die Kartenlocator wachsen um den Titelverweis und den zweiten Menüpunkt; das neue Seitenobjekt `KartendetailSeite` tritt daneben.
- `TestKartenRepository`, `TestSpaltenRepository` — je um das Nötige erweitert.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `KartenValidator.Pruefe(karteId, anfrage)` — leerer Titel, zu langer Titel, unbekannte Farbe; **die Kompensation nennt `PUT /api/karten/{karteId}`**, und die Anlegeüberladung nennt unverändert die `POST`-Route.
- `EtikettenValidator.Pruefe` — leere Liste (gültig), nur Leerzeichen, zu langer Text, zwei Texte, die sich nur in Randleerzeichen unterscheiden (Dublette), zwei Schreibweisen desselben Wortes (**keine** Dublette).
- `Verantwortlichenliste` — Stillgelegte fehlen, Abgebildete sind enthalten, „niemand" steht als Eintrag, ein stillgelegter Träger wird getrennt ausgewiesen.
- `KartenService.LadeKartendetail` / `AendereKarte` / `SetzeEtiketten` gegen `TestKartenRepository` — Erfolg reicht das Detail durch; unbekannte Karte, unbekannter Kontributor und stillgelegter Kontributor liefern Befunde mit nichtleerem Code, Meldung und Kompensation; nach einer Zurückweisung wurde **nicht geschrieben**.
- `KartenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert das Detail, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und Rumpf des abgesetzten Aufrufs werden mitgeprüft. Diese Fehlerpfade sind über den Browser nicht auslösbar.

**Integration:** `KartenRepository` und `Etikettenleser` gegen eine `TemporaereDatenbank` — Eigenschaften schreiben und wieder lesen, zweites Schreiben überschreibt (`ON CONFLICT DO UPDATE`), `FaelligAm` als ISO-Text und wieder als `DateOnly`, `null` bei unbekannter Karte, alles in einer Transaktion; Etiketten setzen, ersetzen, leeren; Vorschläge mit Kartenzahl je Text und nur aus dem eigenen Board; das letzte entfernte Etikett fehlt danach im Bestand. `Kartenleser.LiesKartendetail` liefert eine **archivierte** Karte. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenEndpunkte` über `TestWebApi` — die drei Routen mit 200, 400 und 404 samt Rumpf; `GET /api/boards/{boardId}` trägt danach die neuen Kartenfelder; `FehlervertragTests` ruft alle drei Routen ab. `WebApiNeustartTests` — Eigenschaften, Verantwortlicher und Etiketten überstehen den Neustart. `GekuerzteAbschlussspalteTests`, `KartenAmBoardTests` und `BahnenkopfzahlTests` laufen mit.

**E2E:** Ein Board mit Karten in zwei Bahnen und ein Kontributorenbestand mit allen drei Arten. Titelklick und Menüeintrag führen auf `/karten/{n}`, Direktaufruf und Reload zeigen dieselbe Karte, der Rückpfeil führt zum Board, `/karten/9999` zeigt die Meldung (US-1). Titel, Beschreibung, Fälligkeit und Farbe ändern, Reload zeigt alle vier, ein geleerter Titel bringt „Der Titel darf nicht leer sein." und ändert nichts (US-2). Verantwortlichen setzen, Reload zeigt ihn, „niemand" nimmt ihn zurück; ein Abgebildeter ist wählbar (US-4). Ein Kontributor wird zwischen zwei Schritten stillgelegt: danach ist er nicht mehr wählbar, steht aber weiter an der Karte (US-5 — die Einlösung von `I0009`). Etikett anlegen, zweites aus der Vervollständigung übernehmen, eines entfernen, Reload zeigt den Stand; das letzte entfernte Etikett fehlt in der Vorschlagsliste einer anderen Karte desselben Boards (US-6). Dazu laufen `KarteVerschiebenE2ETests`, `EinfuegelinieE2ETests`, `AbschlussbahnAblageE2ETests`, `KartenzahlImBahnenkopfE2ETests` und alle übrigen E2E-Suiten aus `R00001`–`R00016` weiter — **ohne Änderung bis auf die drei benannten Menü-Zusicherungen**; das ist die eigentliche Gegenprobe des Slice.

Repositories und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00006`** (Karte anlegen — `I0011`, grün) und **`R00011`** (Kontributor anlegen — `I0006`, grün). Das sind die beiden Vorbedingungen, die die WBS-Spalte `Braucht` von `I0015` führt; beide sind erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00016`** (`I0014`, grün — `B0218` ändert dessen `⋯`-Menü und dessen drei E2E-Auflagen; `LiesKartendetail` löst dessen Zusage „über API und Archiv auffindbar" ein), **`R00014`** (`I0009`, grün — die Stilllegungsregel und die hierher weitergegebene Zusage), **`R00013`** (`Identitaetsliste` als Vorbild der `Verantwortlichenliste`), **`R00007`** (Fehlervertrag, `Nichtgefunden`), **`R00008`** (Einfügelinie — der Titelverweis darf die Kartenhälften nicht verschieben), **`R00003`** (`Board.razor` als Muster für Route, Meldung und Rückweg), **`R00005`** (Token-Sheet und Kopfzeile). Die Spalte `Braucht` von `I0015` nennt `I0014` nicht; das ist in der WBS als offene Frage vermerkt und gehört in `/planung aendern I0015`, wenn die Herkunft dokumentiert bleiben soll.
- Blockiert: **keinen** Knoten außerhalb dieses Slice — kein anderer Slice der WBS nennt `I0015`, `F0040`, `F0041`, `F0042` oder `F0043` in seiner Spalte `Braucht` (geprüft am 2026-09-05 über `Dokumentation/Planung/kanbanc.md`). Fachlich hängen die vier übrigen Interactions von `D0004` (`I0016`–`I0019`) an der Seite, die `F0040` baut.
- Reihenfolge innerhalb der Anforderung: `F0040` → `F0041` → `F0042` und `F0043`; so führt es die Spalte `Braucht`. `F0042` und `F0043` hängen beide nur an `F0041` und sind untereinander unabhängig.

## Umfang

```
Kartendetails bearbeiten (I0015) = 30 Bubbles: 25 Standard (29,2h), 5 unklar (8,4–17,5h).
Rest: 29,2h klar + 8,4–17,5h unklar · 13 von 30 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 30 Bubbles gruen (0 %) · 0 laufen · 30 offen
```

`I0015` ist vollständig bis zur Bubble geplant, in **vier** Slices — die Reihenfolge ist die der Spalte `Braucht`:

| Slice | Bubbles | Umfang | Braucht |
|---|---|---|---|
| `F0040` Kartendetail als eigene Seite | B0213–B0219 (7) | 8,8h klar + 2–4h unklar | `I0011` |
| `F0041` Titel, Beschreibung, Fälligkeit und Farbe ändern | B0220–B0228 (9) | 9,2h klar + 2,4–5,5h unklar | `F0040` |
| `F0042` Verantwortlichen setzen | B0229–B0233 (5) | 3,2h klar + 2–4h unklar | `F0041`, `I0006` |
| `F0043` Etiketten an der Karte | B0234–B0242 (9) | 8,0h klar + 2–4h unklar | `F0041` |

**Mit 30 Bubbles ist das der größte Slice des Projekts** (die sechzehn abgeschlossenen lagen bei 3 bis 14), und **vier der fünf unklaren Bubbles sind E2E-Bubbles** (`B0219`, `B0228`, `B0233`, `B0242`, je 2–4h) — das größte Unsicherheitsbündel bisher. Die fünfte ist `B0221` (0,4–1,5h) mit den 16 Aufrufstellen. Belegt sind 13 Werte über die Vergleichsbubbles `B0027`, `B0028`, `B0029`, `B0184`, `B0196`; die übrigen tragen Richtwerte. Derselbe Vermerk wie bei `I0005` bis `I0014`, damit er nicht als Beifang durchgeht: die 2h-Richtwerte liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles (`B0030`–`B0033` in `_ist-zeiten.md`, alle bei 0,0–0,1h). Die Konvention wurde auch hier nicht geändert, weil das die Zählung des ganzen Baums verschöbe. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

## Offene Fragen

- **Sollen die vier Skalarfelder einzeln oder gesammelt gesichert werden?** — **nicht entschieden**, bewusst nicht geraten. Gebaut wird zunächst **gesammelt**: ein `PUT` trägt alle vier plus den Verantwortlichen, wie die Anfrage geschnitten ist. Ob die Oberfläche jedes Feld einzeln absetzt (fünf Aufrufe beim Durchtippen) oder einmal beim Verlassen des Blattes, ist eine Bedienfrage, die das Artboard nicht beantwortet: es zeichnet einen Zustand, keinen Speicherzeitpunkt. **Vor `B0227` zu beantworten.**
- **Soll die Kartenseite den Archivstand anzeigen?** — **nicht entschieden.** Eine archivierte Karte behält ihre Seite (siehe Beschreibung), aber das Artboard zeichnet dafür keine Stelle. Ohne Anzeige sieht ein Mensch auf `/karten/14` nicht, dass die Karte vom Board fort ist. **Nicht geraten**, weil ein erfundener Vermerk eine Gestaltung ohne Skizze wäre; **vor `B0217` zu beantworten**, falls die Antwort „ja" lautet.
- ~~Welche Adresse bekommt die Kartenseite?~~ — entschieden am 2026-09-05: **eine eigene Seite `/karten/{karteId}`** (Variante C des Artboards). Vier Gründe: die API hat je Karte ohnehin eine Adresse; „nach Reload da" landet nur so wieder auf derselben Karte; `D0004` trägt fünf Interactions und braucht den Platz; die gebaute Kopfzeile führt Zone 1 als „die offene Seite" samt Rückpfeil. Verworfen: Modal (verdeckt das Board, keine teilbare Adresse) und Schublade (zu wenig Platz für fünf Interactions).
- ~~Kommt das Board in die Adresse?~~ — entschieden am 2026-09-05: **nein**, `GET`/`PUT /api/karten/{karteId}` ohne Board. Ein Browser, der `/karten/14` öffnet, kennt das Board noch nicht; ein Board in der Adresse wäre eine Angabe, die der Aufrufer nicht haben kann, ohne vorher zu fragen. `KartenRepository.BoardDerKarte` (`:121`) belegt, dass die Kartennummer genügt. Die bestehenden Routen unter dem Board bleiben unverändert. **Nicht geprüft**, ob der Mensch die alten Routen später unter die neue Adresse ziehen will.
- ~~Eine Tabelle oder vier?~~ — entschieden am 2026-09-05: **eine Tabelle `Karteneigenschaft`**, Muster `Boardeinstellung` (`004`). Die vier Werte werden immer im selben Formular geändert und immer zusammen gelesen; vier Tabellen brächten vier `LEFT JOIN` in einen Leser, der schon zwei trägt. **Der Preis wird ausdrücklich benannt: die Spalte `Kontributor` entsteht in `B0220` und bleibt bis `F0042` ungenutzt — totes Schema für die Dauer eines Features.** Die Alternative wäre, sie erst in `F0042` per zweiter Migration anzuhängen; das ginge in SQLite nur über eine neue Tabelle, weil eine bestehende `CREATE TABLE IF NOT EXISTS` nicht nachträglich um eine Spalte wächst — also genau die vier Tabellen, die hier verworfen wurden. **Nicht geprüft**, ob der Mensch stattdessen eine eigene Tabelle `Karteverantwortung` will.
- ~~Wächst `Karte` oder kommt ein zweiter Record?~~ — entschieden am 2026-09-05: **`Karte` wächst, `Kartendetail` kommt als Zusammensetzung hinzu.** Zwei Records für dieselbe Sache wären zwei Wahrheiten (C06, dasselbe Argument, mit dem `R00016` das `Archivierung`-DTO wiederverwendet hat). Preis ist `B0221` mit 16 Aufrufstellen. **Etiketten reisen nicht an `Karte`**: sie sind eine n-Beziehung, das Artboard zeichnet sie auf der Bahn nicht, und an `Karte` verteuerten sie jeden Board-Abruf um eine zweite Abfrage.
- ~~Was ist ein Etikett?~~ — entschieden am 2026-09-05: **eine freie Textmarke** mit Vervollständigung aus dem Bestand des Boards, **kein verwalteter Etikettensatz**. Ein verwalteter Satz bräuchte einen Pflegeschirm, den die WBS nicht kennt — ein Artboard ohne Knoten wäre erfunden. Der praktische Nutzen (eine Schreibweise statt fünf) kommt aus der Vervollständigung, nicht aus einer Verwaltung. Ein Etikett trägt Worte, keine Farbe. **Nicht geprüft**, ob der Mensch später doch einen Etikettensatz je Board pflegen will.
- ~~Brauchen die Etikettvorschläge eine eigene Route?~~ — entschieden am 2026-09-05: **nein**, sie reisen im `Kartendetail`. Die Vervollständigung ist eine Sache dieses einen Schirms, und der Schirm hat genau eine Adresse. Ein Agent kommt über dieselbe Adresse an den Bestand; die Zusage „was die Oberfläche kann, kann ein Agent aufrufen" bleibt eingelöst. **Nicht geprüft**, ob eine Board-Ressource für Etiketten später gebraucht wird — dann ist sie ein eigener Slice.
- ~~Wer ist als Verantwortlicher wählbar?~~ — entschieden am 2026-09-05: **Stillgelegte nicht, Abgebildete sehr wohl**, „niemand" ist ein Listeneintrag und kein zweiter Knopf. Zwei Regeln, die nicht dieselbe sind: ein Abgebildeter kann sich nicht selbst anmelden, aber jemand kann für ihn eine Karte führen — genau dafür gibt es die Art. Damit ist die Auswahl **nicht** dieselbe wie die der Identitätswahl (`Identitaetsliste.Waehlbare` lässt nur aktive Menschen zu), und `Verantwortlichenliste` ist eine eigene Operation statt einer zweiten Bedeutung an derselben.
- ~~Was wird aus einem stillgelegten Verantwortlichen?~~ — entschieden am 2026-09-05: **die Karte zeigt ihn weiter**, gedämpft und mit dem Zusatz „stillgelegt"; wählbar ist er nicht mehr, ein Wechsel geht nur nach vorn. Das ist die Einlösung der zweiten Hälfte von `I0009` und steht als Kriterium und als E2E-Szenario, nicht als Notiz.
- ~~Nennt die Kompensation des `KartenValidator` eine oder zwei Routen?~~ — entschieden am 2026-09-05: **die Route des Aufrufers**, je Überladung eine. Der WBS-Vermerk zu `B0222` („nennt jetzt zwei Routen, POST und PUT") ist so gelesen, dass **beide** Routen im Wortlaut vorkommen — jede dort, wo sie gerufen wurde. Belege für dieses Muster im grünen Bestand: `KontributorenValidator` (`:11-21`, eine Überladung je Route), `Boardname` (`:22`, Route als Parameter) und `Archivfilter` nach `R00016`. Eine Kompensation, die beide Routen in **einem** Satz nennt, schickte den Aufrufer zur Hälfte an eine Adresse, an der er nicht steht.
- ~~Wie wird eine unbekannte Kartenfarbe zurückgewiesen?~~ — entschieden am 2026-09-05: **wie die unbekannte Kontributorart.** Aus einem JSON-Rumpf ist der Befund nicht auslösbar — unbekannten Text weist die Deserialisierung vorher ab, und ASP.NET antwortet selbst mit 400 ohne unseren Rumpf (belegt durch `KontributorartProbeTests.cs:38-52`). Der Befund greift für Aufrufer, die die Aufzählung selbst füllen; der `KartenValidator` trägt ihn mit demselben erklärenden Kommentar wie `KontributorenValidator.cs:38-40`. **Das Akzeptanzkriterium „ein anderer Wert wird mit Befund zurückgewiesen" ist deshalb auf der Unit-Ebene zu prüfen, nicht über HTTP.**
- ~~Bleibt eine archivierte Karte über ihre Seite erreichbar?~~ — entschieden am 2026-09-05: **ja**, `LiesKartendetail` filtert nicht nach dem Archivstand — umgekehrt zu `LiesKartenNachPosition`. Eine archivierte Karte ist kein Bestand, aber sie ist auffindbar; das hat `I0014` zugesagt.
- **Anmerkung mit Beleg — Index auf `Etikett (Karte)`:** Die Bubble `B0234` nennt einen Index auf `Karte` **zusätzlich** zum zusammengesetzten Primärschlüssel `(Karte, Text)`. In SQLite legt ein solcher Primärschlüssel bereits einen Index an, dessen führende Spalte `Karte` ist; ein zweiter Index auf derselben Spalte wäre Dublette. Er wird deshalb **nicht** angelegt, solange kein Abfrageplan ihn verlangt. Das ist eine Abweichung von der Bubble-Notiz, keine von einem Kriterium — und sie steht hier, statt still zu geschehen.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Keine. Beide Migrationen laufen beim Start der WebApi mit. Bestehende Karten bekommen weder eine Zeile in `Karteneigenschaft` noch in `Etikett` und lesen sich damit als „ohne Beschreibung, ohne Fälligkeit, Farbe ohne, niemand verantwortlich, keine Etiketten" — der sichtbare Zustand vor der Anforderung bleibt der sichtbare Zustand danach.

## Warum löst diese Anforderung das Problem? (Pflicht)

Auslöser ist, dass eine Karte heute nur ihren Titel sagen kann: alles, was eine Aufgabe im Alltag ausmacht, muss in eine Zeile Text oder gar nicht — und die seit `R00011` gepflegten Kontributoren zeigen auf nichts, weil kein Datensatz auf sie verweist. Wenn die Karte eine eigene Adresse bekommt und an dieser Adresse ihre Eigenschaften liegen, dann hat jedes weitere Feld von da an einen Ort, und „nach Reload da" wird überhaupt erst prüfbar — ein Reload landet nur auf einer Adresse wieder bei derselben Karte, in einem Modal oder einer Schublade nirgends. Genau darin sitzt der Hebel: die Adresse ist die Voraussetzung für alles, was `D0004` noch bringt (Subtasks, Kommentare, Anhänge, Verweise) und für `D0005` und `D0006` obendrein, während die einzelnen Felder ohne sie jeweils eine eigene Ablage bräuchten. Vorgelagert geht es nicht — ohne Träger für die Eigenschaften ist kein Feld prüfbar; nachgelagert auch nicht — würden die Felder zuerst am Board eingebaut und erst später auf eine Seite umgezogen, wäre die Kartenform zweimal zu bauen und die Bahn zweimal zu ändern. Dass die vier Skalarfelder in **einer** Tabelle und **einer** Anfrage liegen, ist derselbe Hebel eine Ebene tiefer: sie werden in einem Blatt geändert, also werden sie in einem Zug fertig, und eine Farbe ohne Fälligkeit auszuliefern wäre eine zerschnittene Bubble, keine Iteration.

## Missing-Docs

- **`INSERT … ON CONFLICT DO UPDATE` auf einer Tabelle mit Fremdschlüssel-Primärschlüssel und mehreren Nutzspalten.** `SqliteUpsertProbeTests` und die Muster aus `R00009`/`R00015` decken den Fall mit **einer** Nutzspalte ab. Ob die `excluded.`-Schreibweise bei vier Spalten unverändert trägt und ob dabei nicht gesetzte Spalten überschrieben werden, ist nicht belegt. **Vor `B0223` mit einem Probe-Test klären** (`~/.claude/skills/dependency-probe/SKILL.md`), falls die vorhandenen Tests die Frage nicht bereits beantworten.
- **Ein `<a>` innerhalb eines `draggable="true"`-Elements in Blazor.** `KindZiehbarkeitProbeE2ETests` hat die Frage für einen `<button>` beantwortet; ein Verweis bringt zusätzlich das **native Ziehen des Links** mit, das der Browser von sich aus anbietet. Ob `draggable="false"` am `<a>` genügt oder ob `@ondragstart:stopPropagation` nötig ist, steht nirgends. Betrifft `B0218`.
- **Ansprechbarkeit einer Vorschlagsliste beim Tippen in Playwright.** Wie stabil sich eine Liste ansprechen lässt, die sich mit jedem Tastendruck neu aufbaut, ist im Repository nicht erprobt — die bestehenden Auswahllisten (`Identitaetswahl`) haben kein Suchfeld mit Filterung. Betrifft `B0242`.
- **`DateOnly` in einem Aufzählungs-nahen DTO über System.Text.Json.** `Kontributor.StillgelegtAm` belegt, dass es **im Lesen** trägt; ob ein `DateOnly?` auch als **Eingabefeld** einer `PUT`-Anfrage mit leerem Wert (`null` vs. `""`) sauber ankommt, ist nicht belegt. Betrifft `B0225`.

## Notizen

### Verworfene Alternativen

- **Modal über dem Board** (Variante B des älteren Wireframe-Satzes). Verworfen: es verdeckt das Board und lässt Live-Änderungen dahinter unbemerkt — das steht gegen „Live überall" —, und es hat keine teilbare Adresse; „nach Reload da" wäre nicht prüfbar.
- **Schublade neben dem Board** (Variante A). Verworfen laut eigener Annotation des älteren Satzes: „wenig Platz für Beschreibung, Kommentare und Anhänge gleichzeitig" — und `D0004` trägt fünf Interactions.
- **`GET /api/boards/{boardId}/karten/{karteId}` mit Board in der Adresse.** Symmetrisch zu den bestehenden Kartenrouten. Verworfen: ein Browser, der eine geteilte Adresse öffnet, kennt das Board nicht; es steht erst in der Antwort.
- **Vier Tabellen `Kartenbeschreibung`, `Karteverantwortung`, `Kartefaelligkeit`, `Kartenfarbe`.** Muster `Karteerledigung`/`Kartenarchivierung` (eine Tatsache, eine Tabelle). Verworfen: die vier Werte werden immer im selben Formular geändert und immer zusammen gelesen; vier `LEFT JOIN` in einem Leser, der schon zwei trägt, sind der höhere Preis als eine ungenutzte Spalte für die Dauer eines Features.
- **`ALTER TABLE Karte ADD COLUMN`.** Ein JOIN weniger. Verworfen: in SQLite nicht idempotent, und der `Migrationslaeufer` führt jedes Skript bei jedem Start aus — dieselbe Begründung wie bei `004`, `005`, `007`, `008` und `009`.
- **Ein zweiter Record `Karteneigenschaften` neben `Karte`.** Verworfen: zwei Records für dieselbe Sache sind zwei Wahrheiten (C06), und die Farbe gehört laut Artboard der Karte auf der Bahn — sie muss also mitreisen.
- **Etiketten an `Karte`.** Verworfen: eine n-Beziehung an einem Skalar-DTO verteuerte jeden Board-Abruf um eine zweite Abfrage und jede der 16 Aufrufstellen um eine Liste; auf der Bahn sind Etiketten nicht gezeichnet. Gegenprobe: die Farbe reist mit, weil sie ein Skalar ist und auf die Bahn gehört.
- **Verwalteter Etikettensatz je Board** (anlegen, umbenennen, Farbe geben). Verworfen: er bräuchte einen Pflegeschirm, den die WBS nicht kennt; ein Artboard ohne Knoten wäre erfunden. Der Nutzen kommt aus der Vervollständigung.
- **Eigene Route `GET /api/boards/{boardId}/etiketten` für die Vorschläge.** Verworfen: die Vervollständigung ist eine Sache dieses einen Schirms, und der Schirm hat eine Adresse; die zweite Route bestünde nur für den Auswähler.
- **Einzelne Zugänge und Abgänge an den Etiketten** (`POST`/`DELETE` je Etikett). Verworfen: die ganze Liste zu ersetzen ist dasselbe Muster wie `SpaltenRepository.SetzeReihenfolge`, und ein Text ohne Karte verschwindet damit von selbst aus dem Bestand, ohne eigenen Aufräumschritt.
- **Abgebildete Kontributoren von der Wahl ausschließen**, wie in der Identitätswahl. Verworfen: sie können sich nicht selbst anmelden, aber jemand kann für sie eine Karte führen — genau dafür gibt es die Art. Die Regel der Identitätswahl ist eine andere Regel, nicht dieselbe.
- **„Niemand" als eigener Knopf „Verantwortlichen entfernen".** Verworfen: eine Karte ohne Verantwortlichen ist der Normalfall nach dem Anlegen, kein Fehler, den man zurücknimmt.
- **Eine sechste Kartenfarbe.** Verworfen: das Token-Sheet führt zwei Akzenttonleitern und eine neutrale; wer eine sechste will, ergänzt `gestaltung.css`, und das ist eine eigene Anforderung.
- **Den Verantwortlichen als eigenen Record im `Kartendetail`.** Verworfen: `Kontributor` sagt dasselbe, trägt `StillgelegtAm` für den Zusatz „stillgelegt" und ist bereits die eine Schreibweise für diesen Begriff (C06).

### Bewusst out of scope

- **Farbe und Verantwortlicher auf der Kartenform der Bahn.** Die Werte reisen an `Karte` mit und stehen einem Agenten sofort zur Verfügung; **gezeichnet** wird die Bahn hier nicht. **Adresse:** eigene Interaction unter `D0003`; zuerst `/wireframe verfeinern D0003`, dann `/planung verfeinern D0003`, dann eine eigene Anforderung. Diese Anforderung baut die Voraussetzung vollständig.
- **Der Archivierungsknopf auf der Kartenseite.** Das Artboard führt ihn ausdrücklich als `I0014 · D0003` (`D0004.dc.html:130-131`), und `I0014` ist grün. Er käme über `/planung aendern I0014` oder eine eigene Anforderung.
- **Ein Vermerk „archiviert" auf der Kartenseite.** Siehe „Offene Fragen" — nicht entschieden, nicht geraten.
- **Subtasks (`I0016`), Kommentare (`I0017`), Anhänge (`I0018`), Verweise (`I0019`).** Sie gehören derselben Seite und eigenen Interactions; die Seite ist hier der Träger, nicht der Inhalt.
- **Klasse und sprechende Kartennummer (`D0005`, `I0021`).** Erst mit der Klasse könnte die Adresse `/karten/WBS-14` heißen statt `/karten/14`.
- **Zeiten und Timer (`D0006`, `I0023`–`I0026`).** Gestrichelter Kasten am Platz, gezeichnet bei `D0006`.
- **Ein Verlauf an der Karte** („wer, wann, über welche Grenze"). Der ältere Wireframe-Satz zeichnet ihn in allen drei Varianten; in der WBS gibt es dazu **keinen Knoten**. Das Artboard hat ihn deshalb nicht gezeichnet, sondern als offene Frage im Index vermerkt — hier wird er ebenso wenig erfunden.
- **Urheberschaft der Änderung** („wer hat den Verantwortlichen gesetzt"). Der `Identitaetsspeicher` steht seit `R00013` bereit, aber nirgends im Schema liegt eine Spalte, die den Handelnden aufnähme; ein Parameter, den niemand auswertet, wäre tote Flexibilität (C17). Sie kommt mit `I0017` und `D0006`, wo sie zum ersten Mal abgelegt und gelesen wird.
- **Live-Übertragung an andere offene Sichten.** Wer ändert, sieht es sofort; ein zweiter Betrachter erst beim nächsten Laden. Das ist `I0028`, nicht dieser Slice.

### Angenommen im stillen Lauf

Diese Anforderung ist ohne Rückfrage entstanden. Die abgehakten Punkte unter „Offene Fragen" sind Annahmen mit Beleg aus Planung, Artboard und Code, keine bestätigten Vorgaben; die WBS führt den größeren Teil davon als Anmerkungen (`Dokumentation/Planung/kanbanc.md`, Offene Fragen zu `I0015`). **Hier** neu getroffen und dort noch nicht vermerkt sind vier Annahmen: dass die Kompensation des `KartenValidator` je Überladung die Route des Aufrufers nennt statt beider in einem Satz; dass der Befund zur unbekannten Kartenfarbe wie bei der Kontributorart nur unterhalb von HTTP prüfbar ist; dass der Verantwortliche im `Kartendetail` als `Kontributor` reist statt als eigener Record; und dass der Index auf `Etikett (Karte)` entfällt, weil der zusammengesetzte Primärschlüssel ihn bereits stellt. Die beiden Fragen, die im stillen Lauf **nicht** entschieden wurden, sind der Speicherzeitpunkt des Eigenschaftenblatts und die Anzeige des Archivstands auf der Kartenseite.
