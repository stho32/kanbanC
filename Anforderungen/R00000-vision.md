---
id: R00000
titel: Vision — KanbanC
status: Lebend
angelegt: 2026-08-29
zuletzt-ergaenzt: 2026-08-29
---

# R00000: Vision — KanbanC

Kein Anforderungsdokument. Beschreibt das Gesamtziel der Anwendung.
Wird fortgeschrieben, nie abgehakt.

## Kurzfassung

KanbanC ist ein lokal betriebenes Kanban-Board, auf dem Menschen und KI-Agenten
gleichberechtigt arbeiten. Was die Weboberfläche kann, kann auch die API — Boards
anlegen, gestalten, Karten führen, Zeiten erfassen —, ohne Rate Limits und ohne
Kosten. Jede Änderung erscheint unverzüglich in allen offenen Sichten, so dass
Mensch und Agent nie auf verschiedene Stände blicken. Die erfassten Ist-Zeiten und
der vollständige Datenbestand sind unmittelbar für eigene Auswertungen zugänglich.

## Anlass

Das Kanban-Board wird heute größtenteils von Hand gepflegt. Die Arbeit selbst
entsteht zunehmend im Zusammenspiel mit KI-Agenten, aber der Agent kann den
Board-Zustand nicht führen — der Mensch überträgt nach, und das Board hinkt der
Realität hinterher.

Gleichzeitig ist es mühselig, an die eigenen Daten heranzukommen. Für eine eigene
Implementation von Burndown-Chart und Critical Chain werden sehr spezielle Daten
gebraucht, und zwar schnell; eine fremde Cloud-API mit Limits gibt sie nicht her.

Hinzu kommt der Preis: Kanbanflow kostet Geld, das für lokale Experimente nicht
angemessen ist.

Mit der Work Breakdown Structure liegt bereits ein mächtiges Planungswerkzeug vor.
Ob es sich dauerhaft und sinnvoll in ein solches System importieren und dort
verwalten lässt — KI-zugänglich —, ist das eigentliche Experiment.

## Zielbild

- **Ein Board, auf dem Mensch und Agent gleichberechtigt arbeiten.** Der Agent
  bewegt Karten, legt Aufgaben an und erfasst Zeiten über die API; die
  Weboberfläche ist der menschliche Blick auf denselben Datenbestand. An jeder
  Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat.

- **Zwei Sorten Board.** Linienboards begleiten dauerhafte Arbeit ohne Ende;
  Projektboards gehören zu einem bestimmten Vorhaben und laufen mit ihm aus.

- **Karten mit der Ausdruckskraft von Kanbanflow** — und darüber hinaus einer
  optionalen Klasse. Eine Klasse fasst zusammengehörige Karten (etwa alle aus der
  WBS) und vergibt eine eigene, klassenspezifische Nummerierung, so dass ein Agent
  über die API gezielt das richtige Set greift statt des ganzen Boards.

- **Aufgaben aus mehreren Quellen.** Der Import einer WBS ist die erste und
  wichtigste, aber nicht die einzige: Bugmeldungen, Beschaffungsaufgaben und
  Prozessarbeit ringsherum entstehen anders und finden trotzdem ihren Platz.

- **Zeiterfassung, die zum Arbeiten passt.** Ein Timer, den man startet und
  stoppt, ohne Umstand.

- **Auswertungen aus vollständigen Daten.** Soll-Ist-Vergleich gegen die
  WBS-Zählung, Burndown, Critical Chain mit Puffer-Verbrauch; dazu Zeiten je
  Aufgabe und Kontributor, exportierbar. Und dieselben Ist-Zeiten als Futter für
  die KI, die daraus künftige Aufgaben besser einschätzt.

- **Live überall.** Bewegt ein Mensch oder die API eine Karte, sehen alle offenen
  Oberflächen die Änderung unverzüglich — ohne Reload, ohne Nachfragen.

- **Eine API auf Augenhöhe mit der Oberfläche.** Nicht nur Karten lesen und
  schreiben, sondern Boards erzeugen und gestalten — Spalten, Klassen, Struktur.
  Was ein Mensch klicken kann, kann ein Agent aufrufen.

## Nutzer und Nutzen

In erster Linie ich selbst, gemeinsam mit KI-Agenten, die als eigene Kontributoren
auf dem Board erscheinen. Gelegentlich weitere Menschen im lokalen Netz, die ihre
Browserfenster auf dieselbe Instanz richten.

Kontributoren gibt es dabei in zwei Ausprägungen: solche, die die Anwendung
benutzen — Mensch am Browser, Agent an der API —, und solche, die nur abgebildet
werden. Letzteren werden Karten und Zeiten zugeordnet, ohne dass sie die Anwendung
je öffnen.

Der Nutzen: ein Board, das der Wirklichkeit nicht hinterherhinkt, weil die
Agenten es selbst führen — und ein Datenbestand, aus dem sich jede Auswertung
sofort und ohne fremde Grenzen ziehen lässt.

## Leitplanken

- **C# / .NET**, aufgeteilt in zwei Projekte: `KanbanC.Blazor` als Weboberfläche
  und `KanbanC.WebApi` als API. Der bestehende Hausstil und Werkzeugbestand
  (`csharp-stil`, NUnit, Blazor-Playwright-Tests, Migrationen) ist darauf
  ausgerichtet.
- **Weboberfläche, netzwerkfähig im LAN** — kein Desktop-Programm, aber auch kein
  Betrieb im Internet.
- **Lokale Datenhaltung**, zunächst SQLite; die Daten liegen auf der eigenen
  Maschine und sind von dort unmittelbar zugänglich.
- **Full-Trust-Modell** — keine Authentifizierung, keine Rechteprüfung. Wer die
  Oberfläche öffnet, wählt aus, wer er ist; die Wahl merkt sich der Browser
  (localStorage).
- **Kontributoren werden in der Oberfläche angelegt** — abgebildete Personen
  genauso wie alle anderen. Der Unterschied entsteht erst in der zweiten Stufe:
  niemand wählt deren Identität, um in ihrem Namen zu arbeiten.
- **Visuelle Haltung an Kanbanflow orientiert** — dessen Gestaltung ist der
  Maßstab, an dem sich die Oberfläche messen lässt.

## Ausdrücklich nicht Ziel

- **Kein Cloud-Dienst.** Keine Accounts, keine Registrierung, keine
  Mandantentrennung, kein Betrieb im Internet.
- **Kein Funktionsgleichstand mit Kanbanflow.** Keine Mobile-Apps, keine
  Fremd-Integrationen (Kalender, Chat, Automatisierungsdienste), keine
  Team-Kollaborationsfeatures wie Benachrichtigungen oder @-Mentions.
- **Kein klassisches Projektmanagement-Werkzeug.** Kein Gantt, keine
  Ressourcenauslastung, keine Termin- und Kapazitätsplanung. Burndown und Critical
  Chain rechnen aus den Ist-Daten; sie planen nicht.
- **Kein Ersatz für die Anforderungs- und Planungs-Commands.** Vision,
  Anforderungen und WBS bleiben als Markdown-Dokumente die Wahrheit; das Board
  führt den Arbeitsfluss.

## Offene Richtungsfragen

- **Bleibt es lokal?** SQLite und Single-User-Datenhaltung sind für die erste
  Phase gesetzt. Ob je eine zweite Phase mit Serverbetrieb und echter
  Mehrbenutzer-Datenhaltung folgt, ist offen — zurzeit ist das Ganze ein
  Experiment.
- **Bleibt Full-Trust?** Ein späteres Identitäts- oder Rechtemodell ist nicht
  ausgeschlossen, aber auch nicht vorgesehen. Hängt an der ersten Frage.
- **Fließt etwas zur WBS zurück?** Der Import geht in eine Richtung. Über die
  Schnittstellen wäre ein Rückfluss in die Markdown-Datei möglich; ob er kommt,
  ist ungeklärt.
- **Welche weiteren Aufgabenquellen?** E-Mail und Formulare wurden als
  Möglichkeiten genannt, nicht als Vorhaben.

## Ergänzungshistorie

| Datum | Ergänzung |
|---|---|
| 2026-08-29 | Vision angelegt (Kanban für Mensch und KI, WBS-Import, Zeiterfassung, Live-Updates, offene API) |
