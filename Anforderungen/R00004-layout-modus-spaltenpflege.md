---
id: R00004
status: In Arbeit
datum: 2026-08-30
---

# R00004: Layout-Modus für die Spaltenpflege

## Beschreibung

Die Bedienelemente zum Anlegen, Umbenennen, Umsortieren und Entfernen von Spalten verschwinden aus der Arbeitsfläche des Boards. Ein Bedienelement in der Board-Kopfzeile schaltet die Seite in einen **Layout-Modus**, in dem die Spaltenbahnen selbst bearbeitbar werden und ein „Fertig" zurück zur Arbeitsansicht führt. Zugleich wird die Bezeichnung einer Spalte innerhalb ihres Boards eindeutig — Groß- und Kleinschreibung sowie umschließende Leerzeichen entscheiden nicht mehr über die Unterscheidbarkeit.

Zahlt ein auf: [Vision](R00000-vision.md) — visuelle Haltung an Kanbanflow orientiert; eine API auf Augenhöhe mit der Oberfläche.

## Geschäftlicher Nutzen

In geschätzt 98 % der Zeit, in der jemand auf ein Board schaut, will er Karten bewegen und keine Spalten umbauen. Die heutige Pflegeliste kostet in dieser Zeit Platz und Aufmerksamkeit für etwas, das gerade niemand braucht. Zwei gleichnamige Spalten wiederum sind auf einem Board nicht auseinanderzuhalten: Wer eine Karte ablegt oder als Agent eine Spalte adressiert, muss raten, welche gemeint ist.

## Funktionale Anforderungen

- Die Arbeitsansicht eines Boards zeigt die Spalten als Bahnen, ohne Bedienelemente zur Spaltenpflege.
- Ein Bedienelement „Layout bearbeiten" in der Board-Kopfzeile schaltet in den Layout-Modus.
- Im Layout-Modus sind die Bahnen selbst bearbeitbar: anlegen, umbenennen, markieren, umsortieren, entfernen.
- „Fertig" verlässt den Layout-Modus und zeigt die Arbeitsansicht mit den vorgenommenen Änderungen.
- Die Bezeichnung einer Spalte ist innerhalb ihres Boards eindeutig, ohne Rücksicht auf Groß-/Kleinschreibung und umschließende Leerzeichen.
- Ein Bezeichnungskonflikt wird über API und Oberfläche als lesbare Zurückweisung gemeldet, nicht als Serverfehler.

## Nicht-funktionale Anforderungen

- **Benutzerfreundlichkeit:** Der Layout-Modus zeigt die Spalten in derselben räumlichen Anordnung wie die Arbeitsansicht — wer eine Spalte verschiebt, sieht das Ergebnis an Ort und Stelle statt in einer Liste (Maßstab Kanbanflow, Leitplanke der Vision).
- **Sicherheit:** Unverändert Full-Trust im LAN ohne Authentifizierung (Leitplanke der Vision). Die Eindeutigkeit ist eine fachliche Invariante, keine Zugriffsbeschränkung.

## Akzeptanzkriterien

### Arbeitsansicht ohne Pflege
- [x] Das geöffnete Board zeigt Kopfdaten und Spaltenbahnen; Anlegeformular, Pflegeliste und die Bedienelemente je Spalte sind nicht sichtbar.
- [x] Die Bahnen zeigen weiterhin Bezeichnung und den Abschlussvermerk mit Anzeigegrenze.

### Layout-Modus betreten und verlassen
- [x] Die Board-Kopfzeile trägt ein Bedienelement „Layout bearbeiten"; es ist auch dann vorhanden, wenn das Board keine Spalte hat.
- [x] Ein Klick darauf schaltet die Seite in den Layout-Modus: die Bahnen werden bearbeitbar, ein „Fertig" erscheint.
- [x] „Fertig" kehrt zur Arbeitsansicht zurück; zwischenzeitliche Änderungen sind dort sichtbar.
- [x] Ein Reload im Layout-Modus landet in der Arbeitsansicht — der Modus hat bewusst keine eigene Adresse.

### Spaltenpflege im Layout-Modus
- [x] Im Modus legt ein Formular eine weitere Spalte an; sie erscheint als letzte Bahn.
- [x] Je Bahn lassen sich Bezeichnung, Abschlussmarkierung und Anzeigegrenze ändern und speichern.
- [x] Je Bahn verschieben zwei Bedienelemente sie um eine Position nach vorn bzw. hinten; die Bahn wandert sichtbar mit: aus `[A, B, C]` wird nach einem Vorwärtsschritt von `B` die Anordnung `[B, A, C]`.
- [x] Je Bahn entfernt ein Bedienelement sie; die Bahn verschwindet.
- [x] Eine Zurückweisung der API erscheint als lesbare Meldung, ohne dass die Seite abstürzt — die Seite nimmt danach eine weitere Bedienung an und führt sie aus.
- [x] Ist die WebApi im Layout-Modus nicht erreichbar, erscheint eine lesbare Meldung statt einer Ausnahmeseite.

### Eindeutigkeit über die API
- [x] `POST /api/boards/{boardId}/spalten` mit einer Bezeichnung, die es auf dem Board schon gibt, liefert HTTP 400 mit einem Befund, der den Konflikt benennt; es entsteht keine Spalte.
- [x] Der Vergleich ignoriert Groß-/Kleinschreibung und umschließende Leerzeichen: `Erledigt`, `erledigt` und `Erledigt ` gelten als dieselbe Bezeichnung.
- [x] `PUT /api/boards/{boardId}/spalten/{spalteId}` auf eine von einer anderen Spalte belegte Bezeichnung liefert HTTP 400 und ändert nichts.
- [x] Eine Spalte lässt sich auf ihre eigene Bezeichnung speichern — das ist kein Konflikt; sonst wäre die Anzeigegrenze ohne Umbenennen nicht änderbar.
- [x] Dieselbe Bezeichnung auf zwei verschiedenen Boards ist zulässig.
- [x] Wird eine Spalte entfernt, lässt sich danach eine neue Spalte mit ihrer Bezeichnung anlegen.
- [x] Zwei gleichzeitige Anfragen mit derselben neuen Bezeichnung führen zu genau einer Spalte; die zweite erhält HTTP 400 mit `Zurueckweisung`, keinen Serverfehler.

### Eindeutigkeit in der Oberfläche
- [ ] Der Konflikt erscheint im Layout-Modus als lesbare Meldung; die bestehende Spalte bleibt unverändert.

### Datenhaltung
- [x] Eine Bezeichnung wird ohne umschließende Leerzeichen gespeichert; der Abruf liefert sie getrimmt zurück.
- [x] Das Schema trägt einen Index, der die Bezeichnung je Board eindeutig macht, unabhängig von Groß-/Kleinschreibung.
- [x] Die Migration ist idempotent und läuft auf einem Bestand mit gleichnamigen Spalten durch, ohne Daten zu verlieren: vorhandene Duplikate werden deterministisch umbenannt, bevor der Index entsteht.

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Pages/Board.razor` (Kopfzeile, Moduswechsel), `Source/KanbanC.Blazor/Components/Spalten/` (bestehende `Spaltenpflege.razor`, neue `Spaltenbahnen.razor`), Bahnen-Optik in `Board.razor.css`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/SpaltenEndpunkte.cs` — der Konflikt reist über den bestehenden Zurückweisungspfad; kein neuer Endpunkt.
- **Fachlogik:** `Source/KanbanC.BL/Operations/Boards/` (neue Operation `Spaltenbezeichnung`, erweiterter `SpaltenValidator`), `Integrations/Boards/SpaltenService.cs`, `Interfaces/Boards/ISpaltenRepository.cs`.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Boards/SpaltenRepository.cs`; Schema unter `Source/KanbanC.BL/Persistenz/Migrationen/` als `002-spalte-bezeichnung-eindeutig.sql`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Boards/` und `Integrations/Boards/`, `Source/KanbanC.WebApi.IntegrationTests/Api/` und `Persistenz/Boards/`, `Source/KanbanC.PlaywrightTests/Tests/` mit Erweiterung des Seitenobjekts `BoardSeite`.

## Technische Überlegungen

### Ablauf

1. **Board öffnen** — unverändert
   - 1.1 `BoardApiKlient.LadeBoard(boardId)` → Kopfdaten und Spalten
   - 1.2 `Spaltenbahnen` rendert mit `IstBearbeitbar = false`
2. **Layout-Modus betreten**
   - 2.1 Klick auf „Layout bearbeiten" setzt den Modus
   - 2.2 `Spaltenbahnen` rendert mit `IstBearbeitbar = true`; `Spaltenpflege` blendet Anlegeformular und Meldungen ein
3. **Spalte anlegen oder ändern**
   - 3.1 `SpaltenService` lädt die vorhandenen Spalten des Boards
   - 3.2 `SpaltenValidator.Pruefe(...)` mit den vergebenen Bezeichnungen
     - 3.2.1 Konflikt: `Zurueckweisung` zurück, HTTP 400, kein Schreibzugriff
   - 3.3 `SpaltenRepository.LegeAn` bzw. `Aendere` schreibt die getrimmte Bezeichnung
     - 3.3.1 Verletzt ein gleichzeitiger Schreibvorgang den Index: Constraint-Fehler abfangen und als `Zurueckweisung` zurückgeben
   - 3.4 Die Oberfläche lädt das Board neu; die Bahnen zeigen den neuen Stand
4. **Layout-Modus verlassen**
   - 4.1 „Fertig" setzt den Modus zurück; `Spaltenbahnen` rendert wieder lesend

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `Board.razor` — die Kopfzeile bekommt das Bedienelement „Layout bearbeiten"; die Seite hält den Modus als Zustand und wählt danach die Darstellung der Bahnen.
- `Spaltenbahnen.razor` — der eine Ort, an dem Spalten als Bahnen entstehen, in beiden Zuständen.
- `SpaltenService` — die Integration, die vorhandene Bezeichnungen lädt und in die Prüfung gibt.
- `002-spalte-bezeichnung-eindeutig.sql` — die Migration, die den Index anlegt; `Migrationslaeufer` findet sie über die Namenssortierung.

**Klassen-Entwurf:**

- `Spaltenbahnen` (Blazor-Komponente) — rendert die Spalten eines Boards als nebeneinanderliegende Bahnen. Im bearbeitbaren Zustand tragen die Bahnen zusätzlich Eingabefelder und Bedienelemente. Eine Komponente für beide Zustände, weil „das Board ansehen" und „das Layout ändern" dieselbe Fläche zeigen (C23).
  - `[Parameter] IReadOnlyList<Spalte> Spalten`
  - `[Parameter] bool IstBearbeitbar`
  - `[Parameter] EventCallback<long> SpalteWeiterVorn`
  - `[Parameter] EventCallback<long> SpalteWeiterHinten`
  - `[Parameter] EventCallback<long> SpalteEntfernt`
  - `[Parameter] EventCallback<SpalteGespeichert> SpalteGeaendert`
- `SpalteGespeichert` (DTO, immutable) — die vom Formular übernommenen Werte einer Bahn: `SpalteId`, `Bezeichnung`, `IstAbschlussspalte`, `Anzeigegrenze`.
- `Spaltenbezeichnung` (Operation) — die eine Stelle, die entscheidet, wann zwei Bezeichnungen dieselbe sind, und die die Speicherform herstellt.
  - `public static string Normalisiert(string bezeichnung)`
  - `public static bool SindGleich(string eine, string andere)`
- `SpaltenValidator` (Operation, erweitert) — prüft zusätzlich gegen die bereits vergebenen Bezeichnungen des Boards.
  - `public static Pruefbefunde Pruefe(string bezeichnung, bool istAbschlussspalte, int? anzeigegrenze, IReadOnlyList<string> vergebeneBezeichnungen)`
- `ISpaltenRepository` (Provider-Vertrag, geändert) — `LegeAn` und `Aendere` brauchen einen dritten Ausgang, weil neben „Erfolg" und „unbekannt" nun „Konflikt" existiert. Dasselbe Muster, das `SetzeReihenfolge` seit `B0042` verwendet.
  - `Ergebnis<Spalte>? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)`
  - `Ergebnis<Spalte>? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)`

### Änderungen an bestehenden Klassen

- `Board.razor` — Zustandsfeld für den Layout-Modus; Kopfzeile mit „Layout bearbeiten" und „Fertig"; die Spaltenschleife weicht der Komponente `Spaltenbahnen`.
- `Spaltenpflege.razor` — verliert die Liste `#spalten-liste` an `Spaltenbahnen`; behält Anlegeformular, Zurückweisungs- und Ausfallmeldung und reicht die Bahnen-Ereignisse an die API weiter.
- `SpaltenValidator` — zusätzlicher Parameter und zusätzliche Prüfung samt Befundtext.
- `SpaltenService` — lädt die vorhandenen Spalten vor jeder Anlage und Änderung und reicht ihre Bezeichnungen in die Prüfung.
- `SpaltenRepository` — schreibt die getrimmte Bezeichnung; fängt die Constraint-Verletzung ab und gibt sie als `Zurueckweisung` zurück.
- `SpaltenApiKlient` — folgt den geänderten Rückgaben; der Konfliktbefund erreicht die Oberfläche über den bestehenden `ApiErgebnis`-Pfad.
- `BoardSeite` (Seitenobjekt der E2E-Tests) — Bedienelemente des Layout-Modus, Umschalten und „Fertig".

## Tests

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Spaltenbezeichnung` — Normalisierung und Gleichheit ohne Seiteneffekte.
- `SpaltenValidator` — Konflikterkennung gegen eine übergebene Liste, ohne Datenbank.
- `Spaltenordnung` — unverändert, bleibt abgedeckt.

**Integration:** `SpaltenRepository` gegen eine echte SQLite-Datei (getrimmtes Schreiben, Constraint-Verletzung als Zurückweisung, Migration auf einem Bestand mit Duplikaten); `SpaltenEndpunkte` über `WebApplicationFactory` (400 bei Konflikt, 200 beim Speichern auf die eigene Bezeichnung, Freiwerden nach dem Entfernen).

**E2E:** Die sechs User-Story-Szenarien durch die Oberfläche — Arbeitsansicht ohne Bedienelemente, Moduswechsel und Rückkehr, sichtbares Wandern einer Bahn, Konfliktmeldung, Weiterbedienbarkeit nach einer Zurückweisung, Ausfallmeldung im Modus.

Repositories und alles mit Datenbank-Abhängigkeit sind keine Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `R00002` (Spaltenpflege über API und Oberfläche), `R00003` (Board-Seite als Ort der Bahnen)
- Blockiert: —

## Offene Fragen

- Nach welchem Muster benennt die Migration vorhandene Duplikate um? Vorschlag für die Umsetzung: Suffix mit laufender Zahl in der Reihenfolge der `SpalteId`, so dass der Lauf wiederholbar dasselbe Ergebnis liefert.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass die Spaltenpflege heute dauerhaft unter den Bahnen steht, obwohl sie fast nie gebraucht wird: Sie kostet Platz und Aufmerksamkeit in genau der Zeit, in der jemand Karten bewegen will. Wenn die Bedienelemente hinter einen Moduswechsel wandern, dann ist die Arbeitsfläche im Regelfall frei, und die Konfiguration ist trotzdem einen Klick entfernt — das Zielbild ist ein Board in der Haltung von Kanbanflow, wo das Layout über `Edit board layout` erreichbar ist und nicht auf der Arbeitsfläche liegt. Gerade jetzt ist diese Änderung der Hebel und nicht später: Karten existieren noch nicht (`I0011`), die Bahnen sind leer, und der Umbau kostet heute eine Komponente mit zwei Zuständen; sobald Karten, Kartenzahl (`I0004`) und Live-Aktualisierung (`I0028`) auf denselben Bahnen liegen, ist derselbe Umbau ein Eingriff quer durch die Oberfläche. Die Eindeutigkeit der Bezeichnung gehört in denselben Schritt, weil sie dieselben Bedienpfade und dieselben Tests berührt: Wenn zwei Spalten gleich heißen, muss beim Ablegen einer Karte und beim Zugriff eines Agenten geraten werden — der Praxisbeleg dafür steht auf dem realen Kanbanflow-Board, wo die Eindeutigkeit von Hand hergestellt wird (`Queue 1`, `Queue 2`, `Queue 2b`), obwohl das Werkzeug sie nicht erzwingt.

## Missing-Docs

- **Reichweite von `COLLATE NOCASE` in SQLite.** Die Kollation vergleicht nur ASCII-Buchstaben ohne Rücksicht auf die Schreibweise; `Ärger` und `ärger` gelten ihr als verschieden. Der Validator in C# fängt diesen Fall über die Normalisierung ab, der Index nicht — bei zwei gleichzeitigen Anfragen mit umlautgleichen Bezeichnungen bleibt also eine Lücke. Bewusst hingenommen, wie schon die Sortierfrage in `R00003`; belastbare Doku zur Erweiterung (ICU-Erweiterung, eigene Kollation) fehlt im Projekt.
- **Zustandsführung von Blazor-Komponenten über einen Moduswechsel.** Ob `OnParametersSet` beim Umschalten die Formulare neu aufbaut und wie Meldungen den Wechsel überleben sollen, ist im Projekt bisher nirgends beschrieben; `Spaltenpflege.razor` trägt dazu heute nur eine Einzelfall-Lösung.

## Notizen

**Ablösung eines Kriteriums aus `R00002`.** `R00002` fordert unter „Spalte anlegen": *„Zwei Spalten mit derselben Bezeichnung sind erlaubt und erhalten verschiedene `SpalteId`."* Dieses Kriterium gilt mit der Umsetzung von `R00004` nicht mehr; der zugehörige Test `SpaltenEndpunkteTests.Wenn_zwei_Spalten_derselben_Bezeichnung_angelegt_werden_dann_tragen_sie_verschiedene_SpalteIds` wird durch seinen Gegentest ersetzt. Da Anforderungsdateien nach ihrer Erstellung nicht mehr geändert werden, hält diese Anforderung die Ablösung fest.

**Vorbild.** Kanbanflow löst die Spaltenkonfiguration nicht als Dialog, sondern als Modus: Die Kopfzeile wird ersetzt durch `Done | Layout: <Board> | Add column | Add swimlane`, darunter stehen die Spalten in ihrer Board-Anordnung ohne Karten, jede mit klickbarem Namen, Kartenzahl bzw. WIP-Limit und einem eigenen Bedienelement.

### Verworfene Alternativen

- **Modal-Dialog hinter einem Zahnrad** — die billigste Variante, weil die bestehende Pflegeliste unverändert in ein Overlay wandern könnte; verworfen, weil das Umsortieren sein Ergebnis nicht zeigt: Man müsste den Dialog schließen, um zu sehen, was man getan hat.
- **Eigene Route `/boards/{boardId}/spalten`** — verlinkbar und reload-fest, was zum Motiv von `R00003` passt; verworfen wegen des größten Kontextwechsels: Man verlässt das Board, um seine Spalten zu ändern.
- **Eindeutigkeit nur im Validator** — verworfen, weil das Fenster zwischen Prüfen und Schreiben offen bliebe; bei Menschen und Agenten, die gleichzeitig schreiben, ist das kein theoretischer Fall.
- **Prüfung in derselben Transaktion statt eindeutigem Index** — wäre konsistent mit `B0042` und käme ohne Migration aus; verworfen, weil die Invariante dann nur im Code ruht und nicht dort verankert ist, wo sie niemand umgehen kann.

### Bewusst out of scope

- Gestaltung der Pflege-Oberfläche — die Optik wird später und dann für das ganze Board angefasst.
- Swimlanes und WIP-Limits, die Kanbanflow im Layout-Modus zusätzlich anbietet; die Kartenzahl je Spalte ist `I0004`.
- Umsortieren per Drag & Drop; es bleibt bei „weiter vorn" und „weiter hinten".
- `UNIQUE (Board, Position)` — im Review zu `R00002` angemerkt, gehört in einen eigenen Slice.
- Eindeutigkeit von Board-Namen.
- Eine eigene Adresse für den Layout-Modus.
