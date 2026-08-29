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
| A0001 | Application | — | KanbanC | gelb | alle Dialogs gruen |  |  |  |  |  | Leitplanken: C#/.NET, zwei Projekte KanbanC.Blazor und KanbanC.WebApi, SQLite, Betrieb im LAN, Full-Trust ohne Authentifizierung, Optik an Kanbanflow orientiert. Jede Interaction gilt über beide Systemgrenzen: was die Oberfläche kann, kann die API |
| D0001 | Dialog | A0001 | Boards führen | gelb | alle Interactions gruen |  |  |  |  |  | aus Vision, kein Requirement |
| I0001 | Interaction | D0001 | Board anlegen | gruen | Ein neues Board entsteht mit Name und Art (Linie oder Projekt) und erscheint in der Board-Liste |  |  |  |  | R00001 |  |
| F0001 | Feature | I0001 | Board anlegen und abrufen | gruen | AK „Board anlegen" und „Standardspalten" über API und Oberfläche (Liste, Formular); US-1, US-2, US-3 |  |  |  |  | R00001 |  |
| B0001 | Bubble | F0001 | Standardspalten erzeugen | gruen | Test gruen | — → StandardspaltenVorlage → 3 Spalten (mit Contracts-DTOs) | 2 | | | | Operation |
| B0015 | Bubble | F0001 | Datenbankverbindung öffnen | gruen | Test gruen | Verbindungszeichenfolge → SqliteVerbindungsfabrik.Oeffne → offene IDbConnection, Datei entsteht | 2 | | | | Provider; aus B0002 herausgeschnitten (Deckel) |
| B0002 | Bubble | F0001 | Schema anlegen | gruen | Test gruen | Verbindungszeichenfolge → Migrationslaeufer + Migration 001 → Tabellen in Datei | 2 | | | | Integration + Provider |
| B0003 | Bubble | F0001 | Board mit Spalten speichern | gruen | Test gruen | Anfrage + Spalten → BoardRepository.LegeAn → Board mit Nummer | 2 | | | | Provider, eine Transaktion |
| B0004 | Bubble | F0001 | Boards laden | gruen | Test gruen | — / Nummer → LadeAlle / Lade → Boards / Board? | 2 | | | | Provider |
| B0005 | Bubble | F0001 | Board-Anlage verdrahten | gruen | Test gruen | Anfrage → BoardService → Ergebnis<Board> | 2 | | | | Integration, Test-Repository |
| B0006 | Bubble | F0001 | Board-Endpunkte | gruen | Test gruen | HTTP → BoardEndpunkte + Start-Migration → 201 / 200 / 404 | 2 | | | | Integration |
| B0007 | Bubble | F0001 | API-Klient der Oberfläche | gruen | Test gruen | Anfrage → BoardApiKlient → Boards / ApiErgebnis | 2 | | | | Integration; Abdeckung über E2E |
| B0008 | Bubble | F0001 | Board-Seite | gruen | Test gruen | Liste + Formular → Boards.razor, NavMenu → Board angelegt | 2 | | | | UI; Abdeckung über E2E |
| B0009 | Bubble | F0001 | E2E Board anlegen | gruen | Test gruen | beide Prozesse auf freien Ports → Playwright → US-1, US-2, US-3 gruen | 2-4 | | | | unklar: Prozessstart-Infrastruktur |
| F0002 | Feature | I0001 | Ungültige Eingaben zurückweisen | gruen | AK „Zurückweisung ungültiger Eingaben" und lesbare Meldung in der Oberfläche; US-4, US-5 |  |  |  | F0001 | R00001 |  |
| B0010 | Bubble | F0002 | Anfrage prüfen | gruen | Test gruen | BoardAnlegenAnfrage → BoardAnlegenValidator → Pruefbefunde | 2 | | | | Operation |
| B0011 | Bubble | F0002 | Zurückweisung über die API | gruen | Test gruen | Pruefbefunde → BoardService / BoardEndpunkte → 400 Zurueckweisung | 2 | | | | Integration |
| B0012 | Bubble | F0002 | Zurückweisung in der Oberfläche | gruen | Test gruen | Zurueckweisung → Boards.razor → Meldung; E2E US-4, US-5 | 2 | | | | UI |
| F0003 | Feature | I0001 | Datenbestand überlebt Neustart | gruen | AK „Datenhaltung"; US-6 |  |  |  | F0001 | R00001 |  |
| B0013 | Bubble | F0003 | Migration idempotent | gruen | Test gruen | zweiter Lauf auf bestehender Datei → Migrationslaeufer → Schema und Daten unverändert | 2 | | | | Integration |
| B0014 | Bubble | F0003 | Neustart der WebApi | gruen | Test gruen | zweite Instanz auf derselben Datei → Boards bleiben, nächste Nummer 3 | 2 | | | | Integration; US-6 |
| I0002 | Interaction | D0001 | Boards auflisten und öffnen | gruen | Alle Boards sind mit Name und Art aufgelistet; das gewählte lässt sich öffnen | | | | I0001 | R00003 | |
| F0004 | Feature | I0002 | Boards in fester Reihenfolge auflisten | gruen | AK „Liste" ohne den Verweis-Punkt: API und Oberfläche liefern die Boards alphabetisch nach Name, Groß-/Kleinschreibung ohne Einfluss, BoardId als Zweitschlüssel; US-4 | | | | I0001 | R00003 | |
| B0016 | Bubble | F0004 | Sortierung in der Abfrage | gruen | Test gruen | Boards gemischter Schreibweise → BoardRepository.LadeAlle (ORDER BY Name COLLATE NOCASE, BoardId) → BoardUebersichten alphabetisch | 0,4 | | | | Provider; Aufwand belegt; US-4 |
| B0017 | Bubble | F0004 | Sortierung erreicht die API | gruen | Test gruen | HTTP GET /api/boards → BoardEndpunkte + BoardService → Liste in Repository-Reihenfolge | 0,4 | | | | Integration; nur Test, kein Produktionscode; Aufwand belegt |
| B0018 | Bubble | F0004 | E2E Liste alphabetisch | gruen | Test gruen | drei Boards gemischter Schreibweise → Playwright auf Boards.razor → Zeilen alphabetisch | 0,4 | | | | E2E; US-4; Aufwand belegt |
| F0005 | Feature | I0002 | Board als eigene Seite öffnen | gruen | AK „Board öffnen" plus der Verweis-Punkt aus „Liste": eigene Route, Kopfdaten, Spaltenbahnen, Reload-fest, Rückweg; US-1, US-2, US-3 | | | | I0001 | R00003 | |
| B0019 | Bubble | F0005 | Board-Seite mit Route | gruen | Test gruen | Route /boards/{BoardId:long} → Board.razor + BoardApiKlient.LadeBoard → Kopfzeile mit Name, Art, Terminen | 0,4 | | | | UI; US-2; Aufwand belegt |
| B0020 | Bubble | F0005 | Spaltenbahnen | gruen | Test gruen | Board.Spalten → Bahnen-Layout in Board.razor → Spalten nebeneinander, Abschlussspalte mit Anzeigegrenze markiert | 0,4 | | | | UI; Aufwand belegt |
| B0021 | Bubble | F0005 | Verweis aus der Liste | gruen | Test gruen | BoardUebersicht → NavLink in Boards.razor → /boards/{BoardId} | 0,4 | | | | UI; US-1; Aufwand belegt |
| B0022 | Bubble | F0005 | Detail-Panel abbauen, Seitenobjekte umziehen | gruen | Test gruen | Panel in Boards.razor + BoardsSeite → BoardSeite; die zwei R00001-E2E-Tests auf die neue Seite | 0,4-1,5 | | | | Umbau; unklar: Umfang des Testumzugs; R00001-Suite muss gruen bleiben |
| B0023 | Bubble | F0005 | E2E Board öffnen | gruen | Test gruen | Klick aus der Liste, Direktaufruf, Reload, Rückweg → Playwright → US-1, US-2, US-3 | 0,4 | | | | E2E; Aufwand belegt |
| F0006 | Feature | I0002 | Fehlerpfade beim Öffnen | gruen | AK „Unbekanntes Board": lesbare Meldung mit Nummer und Rückweg, kein Absturz; Meldung bei nicht erreichbarer WebApi; US-5, US-6 | | | | F0005 | R00003 | |
| B0024 | Bubble | F0006 | Unbekannte Board-Nummer | gruen | Test gruen | LadeBoard liefert null → Board.razor → Meldung mit der Nummer und Verweis zur Liste | 0,4 | | | | UI; US-5; Aufwand belegt |
| B0025 | Bubble | F0006 | WebApi nicht erreichbar | gruen | Test gruen | HttpRequestException → Board.razor → lesbare Meldung statt Ausnahmeseite | 0,4 | | | | UI; US-6; Aufwand belegt |
| B0026 | Bubble | F0006 | E2E Fehlerpfade | gruen | Test gruen | /boards/999 und angehaltene WebApi → Playwright → US-5, US-6 | 0,4 | | | | E2E; Aufwand belegt |
| I0003 | Interaction | D0001 | Spalten gestalten | gelb | Spalten lassen sich anlegen, umbenennen, umsortieren und entfernen; eine Spalte ist als Abschlussspalte mit Anzeigegrenze N markierbar | | | | I0001 | R00002 | |
| F0007 | Feature | I0003 | Spalten anlegen und aendern | gelb | AK „Spalte anlegen", „Spalte umbenennen und markieren", „Abschlussspalte und Anzeigegrenze" ueber API und Oberflaeche; US-1, US-2, US-6, US-7, US-8 |  |  |  |  | R00002 |  |
| B0027 | Bubble | F0007 | Spalten-Anfrage pruefen | gruen | Test gruen | SpalteAnlegenAnfrage / SpalteAendernAnfrage → SpaltenValidator.Pruefe → Pruefbefunde | 0,4 |  |  |  | Operation; die Contracts-DTOs entstehen hier, Pruefbefunde wandert nach Models/; belegt |
| B0028 | Bubble | F0007 | Spalte speichern und aendern | gruen | Test gruen | boardId + Anfrage → SpaltenRepository.LegeAn / Aendere → Spalte? | 0,4 |  |  |  | Provider; Position = hoechste + 1, eine Transaktion; belegt |
| B0029 | Bubble | F0007 | Spalten-Anlage verdrahten | gruen | Test gruen | boardId + Anfrage → SpaltenService → Ergebnis<Spalte>? | 0,4 |  |  |  | Integration, Test-Repository; belegt |
| B0030 | Bubble | F0007 | Spalten-Endpunkte anlegen und aendern | gruen | Test gruen | HTTP POST/PUT → SpaltenEndpunkte → 201 / 200 / 400 / 404 | 2 |  |  |  | Integration; kein Messwert fuer Endpunkt-Bubbles |
| B0031 | Bubble | F0007 | API-Klient der Spalten | gruen | Test gruen | Anfrage → SpaltenApiKlient → ApiErgebnis<Spalte> | 2 |  |  |  | Integration; Fehlerpfade in KanbanC.Blazor.Tests |
| B0032 | Bubble | F0007 | Spaltenpflege in der Oberflaeche | rot | Test gruen | Board-Detail → Boards.razor → Spalte angelegt und geaendert, Meldung bei Zurueckweisung | 2 |  |  |  | UI |
| B0033 | Bubble | F0007 | E2E Spalte anlegen und aendern | rot | Test gruen | beide Prozesse auf freien Ports → Playwright → US-1, US-2, US-6, US-7, US-8 | 2-4 |  |  |  | unklar: Erweiterung des Seitenobjekts BoardsSeite |
| B0041 | Bubble | F0007 | Ausfall der WebApi in der Spaltenpflege | rot | Test gruen | HttpRequestException → WebApiAusfall.BeimAufruf → Ausfallmeldung statt Absturz | 0,4 |  |  |  | Operation; Klammer aus Boards.razor hochgezogen, zweiter Nutzer (C23) |
| F0008 | Feature | I0003 | Spalten umsortieren | gelb | AK „Spalten umsortieren" ueber API und Oberflaeche; US-3, US-9 |  |  |  | F0007 | R00002 |  |
| B0034 | Bubble | F0008 | Reihenfolge pruefen | gruen | Test gruen | gewuenschte SpalteIds + vorhandene SpalteIds → SpaltenreihenfolgeValidator.Pruefe → Pruefbefunde | 0,4 |  |  |  | Operation; Contracts-DTO Spaltenreihenfolge entsteht hier; belegt |
| B0035 | Bubble | F0008 | Reihenfolge speichern | gruen | Test gruen | boardId + SpalteIds → SpaltenRepository.SetzeReihenfolge → Spalten mit Position 1..n | 0,4 |  |  |  | Provider, eine Transaktion; belegt |
| B0036 | Bubble | F0008 | Reihenfolge ueber die API | gruen | Test gruen | HTTP PUT /spalten/reihenfolge → SpaltenService + SpaltenEndpunkte → 200 / 400 / 404 | 2 |  |  |  | Integration |
| B0037 | Bubble | F0008 | Umsortieren in der Oberflaeche | rot | Test gruen | Hoch/Runter → Boards.razor + SpaltenApiKlient → neue Ordnung; E2E US-3 | 2 |  |  |  | UI; sendet die ganze Reihenfolge, nicht eine Einzelposition |
| B0042 | Bubble | F0008 | Reihenfolge in derselben Transaktion pruefen | gruen | Test gruen | boardId + SpalteIds → SpaltenRepository.SetzeReihenfolge → Ergebnis<Spalten> oder Zurueckweisung, Rollback | 0,4 |  |  |  | Provider; schliesst das Fenster zwischen Pruefung und Schreiben |
| F0009 | Feature | I0003 | Spalte entfernen | gelb | AK „Spalte entfernen" ueber API und Oberflaeche; US-4, US-5 |  |  |  | F0007 | R00002 |  |
| B0038 | Bubble | F0009 | Spalte loeschen und verdichten | gruen | Test gruen | boardId + spalteId → SpaltenRepository.Entferne → geloescht, verbleibende Positionen 1..n | 0,4 |  |  |  | Provider, eine Transaktion; belegt |
| B0039 | Bubble | F0009 | Entfernen ueber die API | gruen | Test gruen | HTTP DELETE → SpaltenService + SpaltenEndpunkte → 204 / 404 | 2 |  |  |  | Integration |
| B0040 | Bubble | F0009 | Entfernen in der Oberflaeche | rot | Test gruen | Entfernen → Boards.razor + SpaltenApiKlient → Spalte verschwindet; E2E US-4, US-5 | 2 |  |  |  | UI |
| I0004 | Interaction | D0001 | Kartenzahl je Spalte anzeigen | rot | Je Board einschaltbar, dass die Zahl der enthaltenen Karten in der Spaltenkopfzeile steht; sie folgt Änderungen ohne Reload | | | | I0003, I0011 | | |
| I0005 | Interaction | D0001 | Board umbenennen und archivieren | rot | Ein Board lässt sich umbenennen und archivieren; das archivierte ist aus der Standardliste verschwunden, bleibt aber abrufbar | | | | I0001 | | |
| I0038 | Interaction | D0001 | Board exportieren | rot | Ein einzelnes Board wird als eigenstaendige Datei herausgeschrieben; sie enthaelt Board, Spalten, Karten, Klassenzuordnungen und Zeiteintraege vollstaendig und ist ohne die Anwendung lesbar | | | | I0011, I0021, I0024 | | Ersetzt die Portabilitaet, die eine Datei je Board gebracht haette |
| I0039 | Interaction | D0001 | Board importieren | rot | Eine exportierte Board-Datei wird eingelesen; das Board erscheint mit seinem Inhalt in der Liste, ohne bestehende Boards zu veraendern | | | | I0038 | | |
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

- ~~`B0019`: Verhalten des Blazor-Routers bei `/boards/abc`~~ — beantwortet in R00003: der `:long`-Constraint greift nicht, die Anfrage endet über `UseStatusCodePagesWithReExecute` auf der `NotFound`-Seite. Kein Absturz, kein Standardwert. Belegt durch `BoardFehlerpfadeE2ETests`.
- ~~`B0022`: Umfang des Testumzugs~~ — gemessen: zwei E2E-Tests aus `R00001` und vier Locator in `BoardsSeite`; der Umzug lag am unteren Ende der Bandbreite.

## Notizen / Quellen

- `Anforderungen/R00000-vision.md` — alle Knoten sind aus den acht Zielbild-Punkten abgeleitet; Requirements existieren noch keine, deshalb trägt kein Knoten eine `RXXXXX`-Klammer. Über `/anforderung aus-slice <knoten>` entstehen sie.
- Entschieden beim Anlegen: **eine** Application statt zweier — `KanbanC.Blazor` und `KanbanC.WebApi` sind Container derselben Anwendung; zwei WBS-Dateien würden den Umfang nach Schichten schneiden.
- Entschieden beim Anlegen: Oberfläche und API sind **zwei Systemgrenzen einer Interaction**, kein eigener Ast. Dass die API alles kann, ist Fertig-Kriterium an jedem Slice.
- Ausdrücklich nicht aufgenommen (geprüft und verworfen): WIP-Limits je Spalte, Swimlanes, wiederkehrende Karten.
- Entschieden bei R00001: **eine** SQLite-Datei fuer alle Boards, jede Zeile mit ihrer Board-Nummer — keine Datei je Board. Cross-Board-Abfragen (I0022, I0033, I0034, I0036, I0037) waeren sonst ein Merge ueber N Dateien. Die Portabilitaet kommt stattdessen ueber I0038/I0039.
- Recherche zur Abschlussspalte (I0013): Kanbanflow gruppiert erledigte Aufgaben nach Fertigstellungsdatum und zeigt standardmäßig nur die 20 neuesten; manuelles Archivieren entfällt dadurch. Quelle: https://kanbanflow.com/features
