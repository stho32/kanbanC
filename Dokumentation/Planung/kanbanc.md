---
application: KanbanC
system: KanbanC
vision: Anforderungen/R00000-vision.md
sprache: de
zuletzt: 2026-08-29
---

# WBS — KanbanC

## Knoten

| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A0001 | Application | — | KanbanC | rot | alle Dialogs gruen | | | | | | Leitplanken: C#/.NET, zwei Projekte KanbanC.Blazor und KanbanC.WebApi, SQLite, Betrieb im LAN, Full-Trust ohne Authentifizierung, Optik an Kanbanflow orientiert. Jede Interaction gilt über beide Systemgrenzen: was die Oberfläche kann, kann die API |
| D0001 | Dialog | A0001 | Boards führen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0001 | Interaction | D0001 | Board anlegen | rot | Ein neues Board entsteht mit Name und Art (Linie oder Projekt) und erscheint in der Board-Liste | | | | | R00001 | |
| I0002 | Interaction | D0001 | Boards auflisten und öffnen | rot | Alle Boards sind mit Name und Art aufgelistet; das gewählte lässt sich öffnen | | | | I0001 | | |
| I0003 | Interaction | D0001 | Spalten gestalten | rot | Spalten lassen sich anlegen, umbenennen, umsortieren und entfernen; eine Spalte ist als Abschlussspalte mit Anzeigegrenze N markierbar | | | | I0001 | | |
| I0004 | Interaction | D0001 | Kartenzahl je Spalte anzeigen | rot | Je Board einschaltbar, dass die Zahl der enthaltenen Karten in der Spaltenkopfzeile steht; sie folgt Änderungen ohne Reload | | | | I0003, I0011 | | |
| I0005 | Interaction | D0001 | Board umbenennen und archivieren | rot | Ein Board lässt sich umbenennen und archivieren; das archivierte ist aus der Standardliste verschwunden, bleibt aber abrufbar | | | | I0001 | | |
| D0002 | Dialog | A0001 | Kontributoren führen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0006 | Interaction | D0002 | Kontributor anlegen | rot | Ein Kontributor entsteht mit Name und Art (Mensch, Agent, abgebildet) und steht zur Auswahl bereit | | | | | | |
| I0007 | Interaction | D0002 | Kontributoren bearbeiten | rot | Alle Kontributoren sind sichtbar; Name und Art lassen sich ändern | | | | I0006 | | |
| I0008 | Interaction | D0002 | Identität wählen | rot | Beim Öffnen der Oberfläche wählt man, wer man ist; die Wahl überlebt einen Reload | | | | I0006 | | localStorage |
| I0009 | Interaction | D0002 | Kontributor stilllegen | rot | Ein stillgelegter Kontributor verschwindet aus der Auswahl, bleibt aber an alten Karten und Zeiten sichtbar | | | | I0006 | | |
| D0003 | Dialog | A0001 | Board bedienen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0010 | Interaction | D0003 | Board ansehen | rot | Das Board zeigt seine Spalten mit den enthaltenen Karten in ihrer Reihenfolge | | | | I0003 | | Walking Skeleton |
| I0011 | Interaction | D0003 | Karte anlegen | rot | Eine neue Karte entsteht in einer Spalte, über die Oberfläche wie über die API | | | | I0010 | | |
| I0012 | Interaction | D0003 | Karte verschieben | rot | Eine Karte wechselt Spalte und Position; die neue Lage bleibt nach Reload erhalten | | | | I0011 | | |
| I0013 | Interaction | D0003 | Erledigte Karten gebündelt sehen | rot | Die Abschlussspalte gruppiert ihre Karten nach Erledigungsdatum und zeigt nur die N neuesten; ältere sind über die API vollständig und in der Oberfläche über Nachladen erreichbar | | | | I0012, I0003 | | Vorbild Kanbanflow: Done gruppiert nach Datum, Standard 20 neueste |
| I0014 | Interaction | D0003 | Karte archivieren | rot | Eine archivierte Karte verschwindet vom Board, bleibt aber über API und Archiv auffindbar | | | | I0011 | | |
| D0004 | Dialog | A0001 | Karteninhalt pflegen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0015 | Interaction | D0004 | Kartendetails bearbeiten | rot | Titel, Beschreibung, Verantwortlicher, Fälligkeit, Farbe und Etiketten lassen sich ändern und sind nach Reload da | | | | I0011, I0006 | | |
| I0016 | Interaction | D0004 | Karte gliedern | rot | Eine Karte trägt Subtasks, die einzeln abhakbar sind | | | | I0011 | | |
| I0017 | Interaction | D0004 | Karte kommentieren | rot | Ein Kommentar wird mit Kontributor und Zeitpunkt an der Karte festgehalten | | | | I0011, I0008 | | |
| I0018 | Interaction | D0004 | Datei an Karte hängen | rot | Eine Datei lässt sich an eine Karte hängen und von dort wieder herunterladen | | | | I0011 | | |
| I0019 | Interaction | D0004 | Karte auf Dateien verweisen lassen | rot | Eine Karte trägt Verweise auf Pfade (Anforderungs-, Planungs-, Architekturdateien); der Verweis ist als solcher erkennbar und über die API abrufbar | | | | I0011 | | |
| D0005 | Dialog | A0001 | Karten-Klassen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0020 | Interaction | D0005 | Klasse anlegen | rot | Eine Klasse entsteht mit Name und Nummernkreis-Präfix | | | | I0001 | | |
| I0021 | Interaction | D0005 | Karte einer Klasse zuordnen | rot | Eine Karte erhält eine Klasse und damit die nächste Nummer dieser Klasse; die Nummer ist auf der Karte sichtbar | | | | I0020, I0011 | | |
| I0022 | Interaction | D0005 | Karten einer Klasse abrufen | rot | Über die API liefert eine Klasse genau ihre Karten, ohne die übrigen | | | | I0021 | | Motiv: gezielter KI-Abgleich statt ganzes Board |
| D0006 | Dialog | A0001 | Zeiterfassung | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0023 | Interaction | D0006 | Timer starten | rot | Ein Timer läuft auf einer Karte für den gewählten Kontributor und ist als laufend erkennbar | | | | I0011, I0008 | | |
| I0024 | Interaction | D0006 | Timer stoppen | rot | Der gestoppte Timer hinterlässt einen Zeiteintrag mit Beginn, Ende und Kontributor | | | | I0023 | | |
| I0025 | Interaction | D0006 | Zeiteintrag nachtragen und ändern | rot | Ein Zeiteintrag lässt sich von Hand erfassen und korrigieren | | | | I0024 | | |
| I0026 | Interaction | D0006 | Zeiten einer Karte sehen | rot | Die Karte zeigt ihre Zeiteinträge und deren Summe je Kontributor | | | | I0024 | | |
| I0027 | Interaction | D0006 | Laufende Timer sehen | rot | Alle gerade laufenden Timer sind mit Karte und Kontributor auf einen Blick sichtbar | | | | I0023 | | |
| D0007 | Dialog | A0001 | Live-Aktualisierung | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0028 | Interaction | D0007 | Änderung ohne Reload sehen | rot | Bewegt ein Browser oder die API eine Karte, zeigt jede andere offene Sicht die Änderung ohne Zutun | | | | I0012 | | |
| I0029 | Interaction | D0007 | Nach Verbindungsabbruch aufschließen | rot | Eine unterbrochene Sicht verbindet sich neu und holt den verpassten Stand nach | | | | I0028 | | |
| D0008 | Dialog | A0001 | WBS-Import | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0030 | Interaction | D0008 | WBS-Datei importieren | rot | Eine WBS-Datei wird eingelesen; ihre Knoten stehen als Karten der Klasse WBS auf einem Board | | | | I0021 | | |
| I0031 | Interaction | D0008 | Import wiederholen | rot | Ein erneuter Import derselben Datei aktualisiert die vorhandenen Karten, statt Dubletten zu erzeugen | | | | I0030 | | |
| I0032 | Interaction | D0008 | Import-Ergebnis sehen | rot | Nach dem Import ist ablesbar, was angelegt, geändert und übersprungen wurde | | | | I0030 | | |
| D0009 | Dialog | A0001 | Auswertungen | rot | alle Interactions gruen | | | | | | aus Vision, kein Requirement |
| I0033 | Interaction | D0009 | Soll-Ist-Vergleich abrufen | rot | Für einen Kartenbestand steht die erfasste Zeit der WBS-Zählung gegenüber | | | | I0030, I0026 | | |
| I0034 | Interaction | D0009 | Burndown sehen | rot | Der Restumfang über die Zeit ist als Verlauf dargestellt | | | | I0030 | | |
| I0035 | Interaction | D0009 | Puffer-Verbrauch sehen | rot | Für eine Kette ist der verbrauchte Puffer gegen den Fortschritt ablesbar | | | | I0034 | | Critical Chain |
| I0036 | Interaction | D0009 | Zeiten exportieren | rot | Zeiten je Aufgabe und Kontributor lassen sich als Datei herausziehen | | | | I0026 | | |
| I0037 | Interaction | D0009 | Rohdaten über die API abrufen | rot | Karten, Zeiten und Verläufe sind über die API vollständig und ohne Limit abrufbar | | | | I0011 | | Motiv: eigene Auswertungen ohne fremde Grenzen |

## Offene Fragen

Keine.

## Notizen / Quellen

- `Anforderungen/R00000-vision.md` — alle Knoten sind aus den acht Zielbild-Punkten abgeleitet; Requirements existieren noch keine, deshalb trägt kein Knoten eine `RXXXXX`-Klammer. Über `/anforderung aus-slice <knoten>` entstehen sie.
- Entschieden beim Anlegen: **eine** Application statt zweier — `KanbanC.Blazor` und `KanbanC.WebApi` sind Container derselben Anwendung; zwei WBS-Dateien würden den Umfang nach Schichten schneiden.
- Entschieden beim Anlegen: Oberfläche und API sind **zwei Systemgrenzen einer Interaction**, kein eigener Ast. Dass die API alles kann, ist Fertig-Kriterium an jedem Slice.
- Ausdrücklich nicht aufgenommen (geprüft und verworfen): WIP-Limits je Spalte, Swimlanes, wiederkehrende Karten.
- Recherche zur Abschlussspalte (I0013): Kanbanflow gruppiert erledigte Aufgaben nach Fertigstellungsdatum und zeigt standardmäßig nur die 20 neuesten; manuelles Archivieren entfällt dadurch. Quelle: https://kanbanflow.com/features
