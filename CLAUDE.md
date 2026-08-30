# KanbanC — Projektkonventionen

Ergänzt `~/.claude/CLAUDE.md`. Was dort steht, gilt auch hier; diese Datei trägt nur das Projektspezifische.

## Wozu

Zielbild: [Anforderungen/R00000-vision.md](Anforderungen/R00000-vision.md) — Kanban-Board, auf dem Menschen und KI-Agenten gleichberechtigt arbeiten. Die vier ausdrücklichen Nicht-Ziele dort sind Teil der Vorgabe, nicht bloß Ton.

Umfang: [Dokumentation/Planung/kanbanc.md](Dokumentation/Planung/kanbanc.md) — 9 Dialogs, 37 Interactions, geplant bis Interaction. **Die WBS ist die Fortschrittswahrheit**; weicht eine andere Liste ab, hat die WBS recht.

## Architektur-Vorlage

`.claude/app-architectures/dotnet-server-side-blazor/` — lokal kopiert, kanonisch für Projektstruktur, IOSP, Test-Pyramide und Deployment. Vor strukturellen Entscheidungen dort nachsehen, nicht raten.

Vier bewusste Abweichungen von der Vorlage:

1. **Zwei Startprojekte** statt einem: `KanbanC.Blazor` und `KanbanC.WebApi`.
2. **`KanbanC.Contracts`** kommt hinzu — DTOs, die beide Prozesse sprechen.
3. **Integrationstests hängen an der WebApi**, nicht an der Web-App.
4. **`KanbanC.Blazor.Tests`** kommt hinzu — Tests der Blazor-Dienste (`BoardApiKlient`, `ApiErgebnis`) unterhalb der E2E-Ebene. Die Vorlage kennt kein solches Projekt, weil ihre Oberfläche direkt auf die BL zugreift; hier liegt wegen der Kernregel ein fachnaher HTTP-Klient in der Oberflächenschicht, dessen Fehlerpfade über den Browser nicht auslösbar sind. Die Razor-Komponenten bleiben bei der E2E-Abdeckung.

## Die eine Regel, die den Aufbau trägt

`KanbanC.Blazor` hat **keine Projektreferenz auf `KanbanC.BL`** und bekommt auch keine. Die Oberfläche spricht ausschließlich über HTTP und den Live-Kanal mit der API.

Grund: Das Zielbild verlangt, dass die API alles kann, was die Oberfläche kann — für KI-Agenten als gleichberechtigte Akteure. Ein direkter Zugriff aus Blazor auf die Fachlogik würde diese Zusage still aushöhlen, weil eine UI-Funktion ohne Endpunkt baubar wäre. Wer diese Referenz hinzufügt, hebt das Kernmotiv des Projekts auf.

## Datenzugriff

**Dapper + Microsoft.Data.Sqlite**, kein EF Core. SQL wird geschrieben, nicht generiert — die wertvollen Abfragen (Burndown, Puffer-Verbrauch, Soll-Ist, Zeitsummen) sind analytisch.

- SQL nach Skill `sql-stil`: Fluss-Ausrichtung, explizite Spalten, Schlüsselwörter GROSS.
- Primärschlüssel heißen `<Tabelle>Id` (`BoardId`, `SpalteId`), Fremdschlüssel tragen den Namen der referenzierten Tabelle (`Board`), nie `<Tabelle>Nummer` — im ganzen Stack, auch in DTOs und Contracts (C06).
- Repositories sind IOSP-**Integrations**, nie Operations.
- Schema als versionierte, idempotente `.sql`-Dateien unter `Source/KanbanC.BL/Persistenz/Migrationen/` (`/erstelle-db-migration`).
- Für `I0031 Import wiederholen` liegt das Muster bereit: `.claude/app-architectures/Common/snippets/SollIstVergleich.md`.

## Code schreiben

Vor jeder C#-Datei den Skill `csharp-stil` laden — nicht erst im Review. Besonders im Blick:

- **C06** kontexteindeutige deutsche Domänensprache: `Kontributor`, `Kartenklasse`, `Zeiteintrag`, `Abschlussspalte` — ein Begriff, eine Schreibweise, auch in den SQL-Spaltennamen.
- **C07** Bezeichner ohne echte Umlaute (`ae/oe/ue/ss`); UI-Texte, Meldungen und Kommentare dagegen mit echten Umlauten.
- **C08** DTOs immutable — in `KanbanC.Contracts` ausnahmslos.

Die IOSP-Ordner (`Models`, `Operations`, `Integrations`, `Interfaces`, `Extensions`) tragen die englischen Namen der Vorlage; die Domänenordner darunter sind deutsch.

## Zieldesign der Oberfläche

`Dokumentation/Wireframes/` — **das ist das Design, das erreicht werden soll**, nicht eine Ideensammlung. Acht Schirme, aus WBS-Knoten abgeleitet; `kanbanc-wireframes.html` im Browser öffnen zeigt sie alle. `Dokumentation/Wireframes/README.md` sagt, welcher Schirm zu welcher Interaction gehört und welche Varianten noch zur Wahl stehen.

Verbindlich für jede Arbeit an der Oberfläche, ohne dass danach gefragt werden muss:

- **Gestaltungswerte kommen aus dem Token-Sheet**, nie als Literal in eine Komponenten-CSS-Datei. Das Sheet ist `Dokumentation/Wireframes/styles.css`; mit `R00005` liegt es als `Source/KanbanC.Blazor/wwwroot/gestaltung.css` in der Anwendung.
- **Vor einem neuen Schirm die Skizze ansehen.** Struktur, Bedienelemente und Beschriftungen folgen ihr; Abweichungen werden begründet, nicht stillschweigend gebaut.
- **Kein CSS-Framework.** Bootstrap steckt noch aus der Blazor-Vorlage in der Anwendung und geht mit `R00005` heraus; nichts Neues wird darauf gebaut.
- **Haltung „Kanbanflow-dicht".** Karten und Bahnen bekommen den Platz, Beiwerk tritt zurück.

Die Übernahme ist [R00005](Anforderungen/R00005-oberflaeche-nach-wireframes.md) — solange sie offen ist, gilt das Zieldesign trotzdem: neue Oberfläche entsteht danach, statt die Vorlage weiterzuschreiben. Die Wireframes selbst sind Dokumentation und werden nicht nachgeführt, wenn der Code sie einholt; weicht der Code ab, ist das eine Entscheidung, die in eine Anforderung gehört.

## Ports

| Dienst | Port |
|---|---|
| `KanbanC.WebApi` | 5280 |
| `KanbanC.Blazor` | 5180 |

Bewusst nicht die .NET-Standardports. Für Preview und E2E gilt zusätzlich der Skill `freier-port`: nie auf einem belegten oder produktiven Port starten, nach dem Start automatisiert verifizieren.

Beide lauschen auf `0.0.0.0` — die Anwendung ist im LAN erreichbar und läuft im **Full-Trust-Modell** ohne Authentifizierung. Das ist eine Leitplanke der Vision, kein Versäumnis.

## Reihenfolge der Arbeit

```
/planung status                        Stand lesen
/planung verfeinern <knoten> --bis Bubble
/anforderung aus-slice <knoten>        Slice → Anforderung
/implementierung im-pair <knoten>      umsetzen, Bubble für Bubble
/planung karte                         Dashboard neu rendern
/implementierung abschluss             Build, Tests, Coverage, Warnungen
```

`TreatWarningsAsErrors` ist aktiv. Grün wird ein Knoten erst, wenn ein Test es beweist (Skill `test-ehrlichkeit`) — nicht, wenn der Code geschrieben ist.
