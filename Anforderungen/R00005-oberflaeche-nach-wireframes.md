---
id: R00005
status: Neu
datum: 2026-08-30
---

# R00005: Oberfläche auf das gezeichnete Design bringen

## Beschreibung

Die Weboberfläche verlässt die Blazor-Standardvorlage und übernimmt das Design, das unter [Dokumentation/Wireframes/](../Dokumentation/Wireframes/) gezeichnet ist: das Token-Sheet `styles.css` wird das Gestaltungsfundament der Anwendung, und der Rahmen — waagerechte Kopfzeile statt Seitenleiste — folgt den Skizzen. Die drei bereits gebauten Schirme (Board-Übersicht, Board mit Bahnen, Layout-Modus) werden auf die gezeichnete Struktur umgebaut. Für die fünf noch nicht gebauten Schirme wird das Wireframe-Verzeichnis als verbindliche Vorlage verankert, an der sich ihre spätere Umsetzung messen lassen muss.

Zahlt ein auf: [Vision](R00000-vision.md) — visuelle Haltung an Kanbanflow orientiert.

## Geschäftlicher Nutzen

Die Oberfläche sieht heute aus wie das, was `dotnet new blazor` erzeugt: Seitenleiste, Bootstrap-Blau, Tabellen ohne Haltung. Die Vision nennt die Gestaltung von Kanbanflow als Maßstab, und mit den Wireframes liegt dieser Maßstab erstmals gezeichnet vor. Solange er nicht im Code steht, entsteht jede neue Interaction in der Standardvorlage — und der Rückbau wird mit jedem Schirm teurer. Der zweite Nutzen ist Vorentscheidung: Ein festgelegtes Fundament nimmt jeder künftigen Oberflächen-Aufgabe die Gestaltungsfrage ab, statt sie neunmal einzeln zu beantworten.

## Funktionale Anforderungen

- Das Token-Sheet aus `Dokumentation/Wireframes/styles.css` ist das Gestaltungsfundament der Anwendung; Farben, Schriften, Abstände, Radien und Schatten der Oberfläche stammen aus seinen Variablen.
- Bootstrap wird aus der Anwendung entfernt; keine Seite verwendet weiterhin Bootstrap-Klassen.
- Der Rahmen ist eine waagerechte Kopfzeile mit Marke und Hauptnavigation; die Seitenleiste der Vorlage entfällt.
- Die Board-Übersicht zeigt die Boards in zwei Bändern nach Boardart, jedes Board als Kachel.
- Das Board zeigt seine Spalten als Bahnen in der gezeichneten Form: Kopfzeile je Bahn, Platz für die Kartenzahl, Abschlussspalte erkennbar.
- Das Anlegen eines Boards und der Layout-Modus folgen den Skizzen `Board anlegen & gestalten` A und B, soweit ihre fachlichen Voraussetzungen bereits bestehen.
- Die Schriftdateien liegen in der Anwendung; die Oberfläche sieht ohne Internetzugang aus wie mit.
- Für die noch nicht gebauten Schirme gilt das Wireframe-Verzeichnis als Vorlage: die zugehörige Interaction setzt sie um, diese Anforderung baut sie nicht vor.

## Nicht-funktionale Anforderungen

- **Benutzerfreundlichkeit:** Die Haltung der Wireframes ist „Kanbanflow-dicht" — Karten und Bahnen bekommen den Platz, Beiwerk tritt zurück. Messbar an den Schirmen: Auf der Board-Seite ist über dem ersten Bahnen-Kopf höchstens die Board-Kopfzeile mit Name, Art, Terminen und dem Layout-Bedienelement zu sehen.
- **Betrieb:** Keine Laufzeit-Abhängigkeit auf ein fremdes Netz. Web-Fonts werden mitgeliefert, nicht von Google geladen — die Anwendung läuft im LAN und darf offline nicht anders aussehen.
- **Sicherheit:** Unverändert Full-Trust im LAN ohne Authentifizierung. Der Identitätsplatz in der Kopfzeile ist in dieser Anforderung eine leere Stelle, keine Anmeldung.
- **Wartbarkeit:** Genau ein Ort trägt die Gestaltungswerte. Eine Farbe, ein Abstand oder ein Radius, der in einer Komponenten-CSS-Datei als Literal steht, ist ein Fehler — er gehört als Variable ins Token-Sheet.

## Akzeptanzkriterien

### Gestaltungsfundament
- [ ] Das Token-Sheet liegt als `Source/KanbanC.Blazor/wwwroot/gestaltung.css` in der Anwendung und wird von `App.razor` geladen.
- [ ] Es trägt dieselben Variablen wie `Dokumentation/Wireframes/styles.css`: `--color-bg`, `--color-surface`, `--color-text`, `--color-accent`, `--color-accent-2`, die drei Rampen zu je neun Stufen, `--font-heading`, `--font-body`, `--space-1` bis `--space-8`, `--radius-sm|md|lg`, `--shadow-sm|md|lg`.
- [ ] Der Hintergrund der geladenen Seite ist der Wert von `--color-bg` (`#f5ead8`), nicht Weiß.
- [ ] Überschriften erscheinen in `Caprasimo`, Fließtext in `Figtree`.
- [ ] Die Schriftdateien liegen unter `Source/KanbanC.Blazor/wwwroot/fonts/`; das Token-Sheet bindet sie über `@font-face` ein und enthält keinen `@import` auf `fonts.googleapis.com`.
- [ ] Bei abgeschaltetem Netzzugang erscheint dieselbe Schrift wie mit — der Test blockiert Anfragen an fremde Hosts und vergleicht die Schriftfamilie des `h1`.

### Bootstrap ist fort
- [ ] `Source/KanbanC.Blazor/wwwroot/lib/bootstrap/` ist gelöscht, `App.razor` lädt kein Bootstrap mehr.
- [ ] Keine `.razor`-Datei enthält noch eine Bootstrap-Klasse; geprüft wird auf `btn-primary`, `btn-outline-secondary`, `form-control`, `form-select`, `form-label`, `alert`, `row`, `col-md-`, `mb-`, `px-`, `d-flex`, `navbar`.
- [ ] Alle bestehenden Tests laufen unverändert grün — der Umbau ändert die Optik, nicht das Verhalten.

### Rahmen und Navigation
- [ ] Der Rahmen zeigt eine waagerechte Kopfzeile mit der Marke `KanbanC` links und den Navigationspunkten `Boards`, `Auswertungen`, `Kontributoren`.
- [ ] Der Punkt der gerade offenen Seite ist als aktiv erkennbar; `Auswertungen` und `Kontributoren` sind sichtbar, aber als noch nicht verfügbar gekennzeichnet und führen ins Leere — sie stehen für `D0009` und `D0002`.
- [ ] Rechts in der Kopfzeile steht der Platz für die Identität mit dem Text `nicht gewählt`; er ist noch nicht bedienbar (`I0008`).
- [ ] Die Seitenleiste (`.sidebar`, `NavMenu`) existiert nicht mehr.

### Schirm „Start — Board-Übersicht"
- [ ] Die Boards stehen unter zwei Bandüberschriften: `Linienboards — laufen ohne Ende` und `Projektboards — laufen mit dem Vorhaben aus`.
- [ ] Innerhalb eines Bandes bleibt die alphabetische Sortierung aus `R00003` erhalten: `beschaffung`, `Betrieb`, `Zulauf` erscheinen in dieser Reihenfolge.
- [ ] Jedes Board erscheint als Kachel mit seinem Namen; bei einem Projektboard nennt der Kachelfuß den Zieltermin, bei einem Linienboard bleibt die Stelle leer.
- [ ] Ein Band ohne Boards zeigt seine Überschrift und darunter einen Hinweis, keine leere Fläche.
- [ ] Ein Klick auf die Kachel öffnet `/boards/{BoardId}` — der Verweis aus `R00003` bleibt, er sitzt jetzt auf der Kachel.
- [ ] Das Bedienelement `+ Board anlegen` sitzt rechts in der Zeile mit der Überschrift `Boards`.

### Schirm „Board"
- [ ] Die Bahnen tragen eine eigene Kopfzeile mit der Bezeichnung; rechts darin ist der Platz für die Kartenzahl vorgesehen und bleibt leer, solange `I0004` nicht umgesetzt ist.
- [ ] Die Abschlussspalte ist an ihrer Bezeichnung mit Häkchen erkennbar und nennt ihre Anzeigegrenze.
- [ ] Die Bahnen liegen nebeneinander und scrollen waagerecht, sobald sie breiter sind als das Fenster; die Seite selbst scrollt dabei nicht waagerecht.
- [ ] Fußzeile je Bahn: die Stelle für `+ Karte` ist vorgesehen und bleibt leer, solange `I0011` nicht umgesetzt ist.
- [ ] Der Layout-Modus aus `R00004` zeigt dieselbe räumliche Anordnung wie die Arbeitsansicht; alle acht Kriterien der Gruppe „Spaltenpflege im Layout-Modus" aus `R00004` gelten unverändert weiter.

### Schirm „Board anlegen & gestalten"
- [ ] Das Anlegeformular erscheint erst nach Klick auf `+ Board anlegen` und schließt sich nach dem Anlegen wieder; es steht nicht dauerhaft über der Liste.
- [ ] Die Art wird über zwei Auswahlknöpfe gewählt (`Linienboard — ohne Ende`, `Projektboard — mit Auslauf`), nicht über ein Auswahlfeld.
- [ ] Die Terminfelder erscheinen nur bei gewählter Art `Projektboard`.
- [ ] Unter den Feldern zeigt eine Vorschau die drei Standardspalten, die mit dem Board entstehen (`B0001`).
- [ ] Eine Zurückweisung erscheint als Meldung am Formular, gestaltet mit den Tokens; die Befunde aus `R00001` bleiben wörtlich erhalten.

### Vorlage für die noch nicht gebauten Schirme
- [ ] `Dokumentation/Wireframes/README.md` weist das Verzeichnis als verbindliche Gestaltungsvorlage aus und nennt diese Anforderung.
- [ ] `CLAUDE.md` trägt einen Abschnitt, der die Wireframes als Zieldesign benennt, so dass jede weitere Arbeit an der Oberfläche sie ohne Nachfrage berücksichtigt.
- [ ] Die fünf Schirme ohne Code — Kartendetail, WBS-Import, Auswertungen, Zeiten je Kontributor, Kontributoren & Identität — bleiben in dieser Anforderung ungebaut; ihre Umsetzung gehört zu `D0004`, `D0008`, `D0009`, `D0006` und `D0002`.
- [ ] Für jeden dieser fünf Schirme steht im Wireframe-README, welche gezeichneten Varianten zur Wahl stehen und dass die Wahl bei der zugehörigen Interaction fällt.

### Bestandsschutz der Tests
- [ ] Die E2E-Anker der bestehenden Tests bleiben erhalten oder werden im Seitenobjekt nachgeführt: `#board-liste`, `.board-verweis`, `#keine-boards`, `#board-kopf`, `#board-name`, `#board-art`, `#board-starttermin`, `#board-zieltermin`, `#zur-board-liste`, `#board-unbekannt`, `#fehlermeldung`, `#zurueckweisung`, `#layout-bearbeiten`, `#layout-fertig`.
- [ ] Verschwindet ein Anker, weil sein Element eine andere Form bekommt, trägt die neue Form denselben Bezeichner — die Tests aus `R00001` bis `R00004` werden nicht umgeschrieben, um grün zu bleiben.

## Betroffene Verzeichnisstruktur

- **Gestaltungsfundament:** `Source/KanbanC.Blazor/wwwroot/gestaltung.css` (neu, aus dem Token-Sheet), `Source/KanbanC.Blazor/wwwroot/fonts/` (neu), `wwwroot/app.css` (schrumpft auf das, was Blazor selbst braucht: Fehlerleiste, Validierungsmarken), `wwwroot/lib/bootstrap/` (entfällt).
- **Rahmen:** `Source/KanbanC.Blazor/Components/App.razor` (Stylesheet-Einbindung), `Components/Layout/MainLayout.razor` und `.razor.css` (Kopfzeile statt Seitenleiste), `Components/Layout/NavMenu.razor` (wird zur waagerechten Navigation oder entfällt zugunsten einer neuen Komponente).
- **Schirme:** `Components/Pages/Boards.razor` (Bänder, Kacheln, Anlegeformular als Patch), `Components/Pages/Board.razor` (Kopfzeile), `Components/Spalten/Spaltenbahnen.razor` und `.razor.css` (Bahnenform).
- **Tests:** `Source/KanbanC.PlaywrightTests/Seiten/` (Seitenobjekte `BoardsSeite`, `BoardSeite` folgen den neuen Formen), `Source/KanbanC.PlaywrightTests/Tests/` (neue Datei für die Gestaltungskriterien).
- **Unberührt:** `KanbanC.BL`, `KanbanC.WebApi`, `KanbanC.Contracts` — diese Anforderung fasst keine Fachlogik und keinen Endpunkt an.

## Technische Überlegungen

### Ablauf

1. **Fundament einziehen**
   - 1.1 `styles.css` nach `wwwroot/gestaltung.css` übernehmen
   - 1.2 Die drei Schriftfamilien als `woff2` unter `wwwroot/fonts/` ablegen, `@import` durch `@font-face` ersetzen
   - 1.3 `App.razor`: `gestaltung.css` laden, Bootstrap-Zeile entfernen
   - 1.4 `app.css` auf das reduzieren, was Blazor selbst stellt (`#blazor-error-ui`, `.validation-message`, `.invalid`)
2. **Rahmen umbauen**
   - 2.1 `MainLayout` verliert `.sidebar`, bekommt die Kopfzeile
   - 2.2 Navigationspunkte `Boards`, `Auswertungen`, `Kontributoren`; die letzten zwei als noch nicht verfügbar gekennzeichnet
   - 2.3 Identitätsplatz rechts, Text `nicht gewählt`
3. **Schirme umbauen, einer nach dem anderen, Tests dazwischen grün**
   - 3.1 `Boards.razor`: Bänder nach Boardart, Kacheln, Anlegeformular als Patch
     - 3.1.1 Die Sortierung bleibt, wo sie ist — sie kommt aus dem Repository (`B0016`); die Gruppierung nach Art geschieht in der Oberfläche
   - 3.2 `Board.razor`: Kopfzeile nach Skizze
   - 3.3 `Spaltenbahnen.razor`: Bahnenkopf mit Bezeichnung und leerer Zahlenstelle, Bahnenfuß mit leerer Kartenstelle
4. **Vorlage verankern**
   - 4.1 Wireframe-README um Status und Variantenwahl ergänzen
   - 4.2 `CLAUDE.md` um den Abschnitt zum Zieldesign ergänzen

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- `App.razor` — die eine Stelle, an der die Stylesheets der Anwendung hängen; hier entscheidet sich, ob Bootstrap noch geladen wird.
- `MainLayout.razor` — der Rahmen jeder Seite; hier wird aus der Seitenleiste die Kopfzeile.
- `gestaltung.css` — der eine Ort der Gestaltungswerte. Jede Komponenten-CSS-Datei greift auf seine Variablen zu und definiert keine eigenen Werte.
- `Spaltenbahnen.razor` — die Bahnenform, die alle künftigen Kartenschirme trägt.

**Klassen-Entwurf:**

- `Kopfzeile` (Blazor-Komponente) — der waagerechte Rahmenkopf: Marke, Hauptnavigation mit aktivem Punkt, Identitätsplatz. Eine Komponente, weil sie auf jeder Seite dieselbe ist und der Identitätsplatz später (`I0008`) genau eine Stelle zum Anfassen braucht.
  - `[Parameter] string Identitaetstext`
- `Boardband` (Blazor-Komponente) — ein Band der Board-Übersicht: Überschrift der Boardart und darunter die Kacheln oder der Hinweis auf ein leeres Band.
  - `[Parameter] string Ueberschrift`
  - `[Parameter] IReadOnlyList<BoardUebersicht> Boards`
- `Boardkachel` (Blazor-Komponente) — ein Board als Kachel mit Name, Fußzeile und Verweis auf seine Seite.
  - `[Parameter] BoardUebersicht Board`
- `Boardbaender` (Operation) — teilt die Boards einer Übersicht nach Boardart auf, ohne ihre Reihenfolge zu verändern. Pure Logik, deshalb ohne Komponente prüfbar.
  - `public static Boardbaender Aus(IReadOnlyList<BoardUebersicht> boards)`
  - `public IReadOnlyList<BoardUebersicht> Linienboards { get; }`
  - `public IReadOnlyList<BoardUebersicht> Projektboards { get; }`

### Änderungen an bestehenden Klassen

- `App.razor` — Bootstrap-Verweis entfällt, `gestaltung.css` kommt hinzu.
- `MainLayout.razor` (+ `.razor.css`) — Seitenleiste entfällt, `Kopfzeile` kommt hinein.
- `NavMenu.razor` (+ `.razor.css`) — geht in `Kopfzeile` auf und entfällt.
- `Boards.razor` — Tabelle weicht den Bändern und Kacheln; das Anlegeformular bekommt einen Sichtbarkeitszustand, die Terminfelder hängen an der gewählten Art; `BoardFormular` bleibt, wie es ist.
- `Board.razor` — Kopfzeile nach Skizze; die Zustandsführung des Layout-Modus bleibt unverändert.
- `Spaltenbahnen.razor` (+ `.razor.css`) — Bahnenkopf und -fuß nach Skizze; `IstBearbeitbar` und die Ereignisse bleiben, wie sie sind.
- `BoardsSeite`, `BoardSeite` (Seitenobjekte der E2E-Tests) — Locator folgen den neuen Formen, wo ein Anker seine Form wechselt.
- `app.css` — schrumpft auf das, was Blazor selbst stellt.

## Tests

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Boardbaender` — die Aufteilung nach Art ohne Seiteneffekte: gemischte Liste bleibt in ihrer Reihenfolge, leere Bänder entstehen als leere Listen.

**Integration:** Keine. Diese Anforderung fasst weder Datenzugriff noch Endpunkte an; die bestehenden Integrationstests dienen als Regressionsnetz und laufen unverändert.

**E2E:** Die Gestaltungskriterien sind über den Browser prüfbar und gehören dorthin — Hintergrundfarbe des `body` gegen `#f5ead8`, Schriftfamilie des `h1` gegen `Caprasimo`, Ausbleiben jeder Anfrage auf `fonts.googleapis.com` beim Seitenaufruf, Ausbleiben von `bootstrap` in den geladenen Stylesheets, Marke und drei Navigationspunkte in der Kopfzeile, Fehlen der Seitenleiste, zwei Bandüberschriften mit den erwarteten Kacheln, alphabetische Reihenfolge innerhalb eines Bandes, Terminfelder erst bei gewählter Art `Projektboard`, Vorschau der Standardspalten, waagerechtes Scrollen der Bahnen ohne waagerechtes Scrollen der Seite. Dazu laufen alle E2E-Tests aus `R00001` bis `R00004` weiter.

Repositories und alles mit Datenbank-Abhängigkeit sind keine Unit-Test-Kandidaten.

## Abhängigkeiten

- Abhängig von: `R00001` (Board anlegen), `R00002` (Spaltenpflege), `R00003` (Board-Seite), `R00004` (Layout-Modus) — alle vier bereits erledigt; ihre Schirme sind die Fläche, die hier umgebaut wird.
- Blockiert: keine Anforderung im Sinne einer Sperre. Jede künftige Oberflächen-Arbeit baut aber auf dem Fundament auf und wird ohne diese Anforderung in der Standardvorlage gebaut.

## Offene Fragen

- **Lizenz und Bezug der Schriften.** `Caprasimo`, `Figtree` und `Caveat` stehen unter der SIL Open Font License und dürfen mitgeliefert werden; woher die `woff2`-Dateien konkret kommen (Download aus dem Google-Fonts-Archiv, Ablage im Repository oder Bezug beim Bauen), ist nicht entschieden. Vorschlag für die Umsetzung: die Dateien liegen im Repository unter `wwwroot/fonts/`, weil das Repository ohne weitere Schritte lauffähig bleiben soll. `Caveat` wird nur geladen, wenn ein Schirm sie tatsächlich braucht — im Token-Sheet ist sie im `@import` genannt, aber in keiner Variablen verwendet.
- **Wohin mit `Auswertungen` und `Kontributoren` in der Kopfzeile?** Sie stehen in jedem gezeichneten Schirm, führen aber auf nichts. Vorschlag: sichtbar und erkennbar deaktiviert, weil ein leerer Navigationspunkt ehrlicher ist als eine Kopfzeile, die sich später umbaut.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist, dass die Vision Kanbanflow als visuellen Maßstab nennt, dieser Maßstab aber bisher nur als Satz existierte und der Code deshalb bei der Blazor-Standardvorlage blieb — `R00004` hat die Gestaltungsfrage ausdrücklich vertagt („die Optik wird später und dann für das ganze Board angefasst"). Mit den Wireframes liegt der Maßstab erstmals gezeichnet vor; wenn er jetzt als Token-Sheet in die Anwendung wandert, dann hat jede weitere Oberflächen-Aufgabe eine Vorlage statt einer Ermessensfrage, und die Gestaltung wird einmal entschieden statt neunmal. Gerade jetzt ist das der Hebel und nicht später: Drei Schirme sind gebaut, fünf sind es nicht — der Umbau kostet heute drei Seiten, nach `D0003`, `D0004` und `D0006` kostet derselbe Umbau jede Karte, jedes Formular und jede Zeittabelle mit. Der Umbau ist der richtige Eingriffspunkt und nicht das bloße Umfärben von Bootstrap, weil die Wireframes nicht nur andere Farben zeigen, sondern eine andere Struktur — Kopfzeile statt Seitenleiste, Kacheln statt Tabelle, Bahnen mit Kopf und Fuß statt nackter Listen; ein Theme über Bootstrap würde die Farben treffen und die Struktur verfehlen.

## Missing-Docs

- **Einbindung eigener Web-Fonts in Blazor Server.** Wie `@font-face` mit dem `Assets`-Mechanismus und dem Fingerprinting von .NET 10 zusammenspielt, ist im Projekt nirgends beschrieben; die Architektur-Vorlage `dotnet-server-side-blazor` behandelt nur Bootstrap.
- **Umgang mit `.razor.css` und Token-Variablen.** Ob CSS-Isolation die Variablen des globalen Sheets sieht (sie tut es, weil Variablen vererbt werden und die Isolation nur Selektoren umschreibt) ist im Projekt nicht festgehalten und wird beim nächsten Mal wieder nachgeschlagen.
- **Prüfbarkeit von Gestaltung.** Welche Aussagen über Optik als ehrlicher Test taugen und welche nur Pixel zementieren, ist keine im Projekt beantwortete Frage. Hier wird auf berechnete Werte einzelner Eigenschaften gesetzt, nicht auf Screenshot-Vergleiche.

## Notizen

**Herkunft der Wireframes.** Die Skizzen liegen als `Dokumentation/KanbanC Wireframes Deutsch.zip` im Repository an und wurden am 2026-08-30 nach `Dokumentation/Wireframes/` entpackt. Sie zeichnen acht Schirme mit zusammen fünfzehn Varianten und leiten jeden Schirm aus WBS-Knoten ab.

**Übernommen wird das Token-Sheet, nicht die Skizze.** `styles.css` ist als Gestaltungsfundament geschrieben („source of truth for the system's look") und wandert in die Anwendung. Die Klassen mit `w`-Präfix aus `wireframes.js` und `kanbanc-wireframes.html` sind Low-Fi-Formen zum Zeichnen; sie bleiben in der Dokumentation.

**Variantenwahl.** Für die drei gebauten Schirme legt diese Anforderung fest, was baubar ist: Bei `Start` ist das Variante A, weil Variante B auf laufenden Timern (`I0027`) und dem Live-Kanal (`D0007`) aufsetzt, die beide noch nicht existieren. Bei `Board` sind die Varianten A und B in den Bahnen deckungsgleich und unterscheiden sich nur im Ort der Live-Ereignisse; gebaut werden die Bahnen, die Wahl zwischen Aktivitätsspur rechts und Laufband oben fällt mit `I0028`. Bei `Board anlegen & gestalten` gilt A für das Anlegen und B für den Layout-Modus, ohne den Klassen-Teil (`I0020`). Für die übrigen fünf Schirme bleiben alle gezeichneten Varianten stehen; ihre Wahl gehört zur jeweiligen Interaction und nicht hierher.

### Verworfene Alternativen

- **Token-Sheet als Bootstrap-Theme darüberlegen** — die billigste Variante: Bootstrap bleibt, seine Variablen werden auf die Tokens gesetzt, keine Seite muss angefasst werden. Verworfen, weil die Wireframes vor allem eine andere Struktur zeigen (Kopfzeile, Kacheln, Bahnen mit Kopf und Fuß) und ein Theme genau diese nicht liefert; zurück bliebe eine getönte Standardvorlage, und die Kriterien dieser Anforderung wären nicht erfüllbar.
- **Nur die Farbvariablen übernehmen, Struktur später** — verworfen, weil das die Entscheidung nicht trifft, sondern verschiebt: Die nächste Interaction stünde wieder vor derselben Frage, und der Rückbau wäre um die dann gebauten Schirme teurer.
- **Alle acht Schirme jetzt bauen** — erwogen, weil das Design dann vollständig im Code stünde. Verworfen, weil fünf der Schirme Karten, Zeiteinträge, Kontributoren und Auswertungen zeigen, die es weder in der Fachlogik noch in der API gibt; es entstünde Oberfläche ohne Inhalt, die bei der Umsetzung der zugehörigen Interaction erneut angefasst würde. Stattdessen sind diese Schirme hier als verbindliche Vorlage verankert.
- **Eigenes Design statt der Wireframes** — nicht ernsthaft erwogen, hier nur der Vollständigkeit halber: Die Vision nennt Kanbanflow als Maßstab, und die Wireframes sind dessen Ausformung für dieses Projekt.

### Bewusst out of scope

- Die fünf Schirme ohne Code: Kartendetail, WBS-Import, Auswertungen, Zeiten je Kontributor, Kontributoren & Identität.
- Die Identitätswahl selbst (`I0008`) — die Kopfzeile bekommt nur den Platz.
- Kartenzahl im Spaltenkopf (`I0004`) und `+ Karte` im Bahnenfuß (`I0011`) — die Bahnen bekommen nur die Stellen.
- Live-Elemente jeder Art: Aktivitätsspur, Laufband, Live-Punkt (`D0007`).
- Dunkles Farbschema. Das Token-Sheet kommentiert es, definiert es aber nicht; die Anwendung bleibt hell.
- Mobile- und Tablet-Ansichten. Die Vision schließt Mobile-Apps aus; die Wireframes zeichnen Desktop-Breiten.
- Umsortieren der Bahnen per Drag & Drop; es bleibt bei den Bedienelementen aus `R00004`.
