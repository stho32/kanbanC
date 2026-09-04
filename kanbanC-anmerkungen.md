# Anmerkungen — kanbanC

Gesammelt waehrend der Laeufe von `/anwendung`. Jede Anmerkung traegt einen Beleg.
Abgearbeitet wird gebuendelt in Station `Z1` (`/anwendung anmerkungen`).

| # | Slice | Station | Anmerkung | Beleg | Status |
|---|---|---|---|---|---|
| 1 | — | A4 | Feld `stand:` wurde beim Nachtrag von `betrieb: lokal` nicht mitgezogen — es steht auf 2026-09-03, die Datei wurde am 2026-09-04 geaendert | Dokumentation/Wireframes/_wireframes.md:9 | offen |
| 2 | I0004 | S3 | Ob der Kartenzahl-Schalter eine Browser- oder eine Board-Eigenschaft ist, entscheidet das Fertig-Kriterium nicht eindeutig; das Artboard beantwortet es nicht | Dokumentation/Planung/kanbanc.md (I0004, Fertig-Kriterium) | offen |
| 3 | I0004 | S3 | `D0003.dc.html` zeigt Termine als `01.08.2026` und den Tag `Projektboard`; der Code formatiert `yyyy-MM-dd` und rendert den Enum-Namen `Projekt`. D0001 folgt dem Code, D0003 wurde nicht nachgezogen — es fehlt die Anforderung, die die Abweichung traegt | Dokumentation/Wireframes/D0003.dc.html:100 · Source/KanbanC.Contracts/Boards/BoardArt.cs:8 | offen |
| 4 | I0004 | S3 | `.board-verweis::after { inset: 0 }` legt den Verweis ueber die ganze Kachel — ein Kontextmenue darauf waere nicht anklickbar; gehoert in die Anforderung zu I0005 | Source/KanbanC.Blazor/Components/Boards/Boardkachel.razor.css:21 | offen |
| 5 | I0004 | S4 | Entscheidung im stillen Lauf: der Schalter ist **Board-Eigenschaft** (persistiert, gilt fuer alle Betrachter), nicht Browser-Zustand. Grund: Fertig-Kriterium „je Board einschaltbar"; ein localStorage-Zustand haette keinen Endpunkt und hoehlte die Kernregel aus. Beantwortet Anmerkung 2 | Anforderungen/R00000-vision.md:71-73 · Dokumentation/Wireframes/D0001.dc.html:566 | offen |
| 6 | I0004 | S4 | Die Form `20+` der Abschlussspalte ist in diesem Slice nicht baubar — sie setzt die Kuerzung der Bahn aus I0013 voraus (noch rot). Solange die Bahn alle Karten haelt, ist die genaue Zahl richtig | Dokumentation/Wireframes/D0001.dc.html:610 · Dokumentation/Planung/kanbanc.md (I0013) | offen |
| 7 | I0004 | S4 | Widerspruch im Repo: `_ist-zeiten.md` fuehrt fuer Endpunkt-, Klient-, UI- und E2E-Bubbles Messwerte von 0,0-0,1 h als `bestaetigt`, die WBS setzt fuer dieselben Typen 2 h und vermerkt „kein Messwert". Eine der beiden Aussagen ist falsch | Schaetzungen/_ist-zeiten.md:20-31 · Dokumentation/Planung/kanbanc.md (B0030) | offen |
| 8 | I0004 | S4 | Dashboard nicht nachgezogen — `.claude/planung/kanbanc.html` zeigt den Stand vor der Verfeinerung; Rendern lag ausserhalb der Schreibgrenze der Station. Nachzuholen mit `/planung karte` | .claude/planung/kanbanc.html | offen |
| 9 | I0004 | S5 | `ON CONFLICT … DO UPDATE` ist im Bestand nirgends benutzt (kein Treffer unter `Source/`); das Verhalten innerhalb der Transaktion ist unbelegt — Probe-Test vor B0110 (Skill `dependency-probe`) | Anforderungen/R00009-kartenzahl-je-spalte.md (Missing Docs) | offen |
| 10 | I0004 | S5 | Ein zweiter, unabhaengiger Browser-Kontext ist in der Playwright-Testumgebung nicht vorgesehen; davon haengt B0115 ab — der Test, der die Board-Bindung beweist | Source/KanbanC.PlaywrightTests/Testumgebung.cs · Infrastructure/Dienstprozess.cs | offen |
| 11 | I0004 | S5 | `SqliteVerbindungsfabrik` setzt kein `PRAGMA foreign_keys`; die Existenzpruefung des Boards muss die Abfrage selbst leisten | Source/KanbanC.BL/Persistenz/SqliteVerbindungsfabrik.cs | offen |
