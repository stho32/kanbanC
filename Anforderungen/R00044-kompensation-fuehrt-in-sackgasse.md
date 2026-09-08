---
id: R00044
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: R00009, R00025
---

# R00044: Behebung der Kompensationsaktionen, die nicht ausführbar sind

## Beschreibung

Die Projektregel lautet: **eine Fehlerantwort nennt Grund mit Werten und die Kompensationsaktion, auch bei 404** — damit ein KI-Agent ohne Menschen weiterkommt. Sie steht in `CLAUDE.md`, wird in jeder Anforderung wiederholt und ist im Kontrakt festgeschrieben: „Kompensation als ausführbarer nächster Schritt mit Route“ (`Source/KanbanC.Contracts/Fehler/Fehlerbefund.cs:6`).

An mehreren Stellen ist die **Form** eingehalten und der **Inhalt** trägt trotzdem nicht: die Kompensation rät einen Aufruf, der im selben Zustand scheitert; sie nennt eine Adresse mit Platzhaltern statt mit Werten; sie verweist auf einen Planungsknoten statt auf eine Route; oder die Meldung spricht von einem Ding, das an der gerufenen Adresse gar nicht vorkommt. In allen vier Lagen liest ein Agent eine wohlgeformte Antwort und kommt nicht weiter — der teuerste Fall ist der, in dem er der Anweisung folgt und dabei in eine Sackgasse läuft.

**Schritte (Fall 1):** Board anlegen · in der ersten Spalte eine Karte anlegen · die Karte über `PUT /api/boards/1/karten/1/archivierung` mit `{"istArchiviert": true}` archivieren · `DELETE /api/boards/1/spalten/1` aufrufen.
**Erwartet:** 400 mit einem Befund, dessen Kompensation ausgeführt werden kann und danach zum Ziel führt.
**Tatsächlich:** 400 `spalte-traegt-karten` mit der Kompensation „Die 1 Karte mit `PUT /api/boards/{boardId}/karten/{karteId}/lage` in eine andere Spalte verschieben und das Entfernen wiederholen.“ — die Adresse trägt zwei ungefüllte Platzhalter, und der Aufruf antwortet für eine **archivierte** Karte mit **404 `karte-unbekannt`**. Aus dieser 404 führt keine der genannten Kompensationen zurück auf die archivierte Karte.

**Schritte (Fall 2):** zwei Boards anlegen · auf dem zweiten eine Kartenklasse anlegen · `GET /api/boards/1/kartenklassen/{klasseDesZweitenBoards}/karten` aufrufen.
**Erwartet:** eine Meldung, die nur von Dingen spricht, die an dieser Adresse vorkommen.
**Tatsächlich:** 404 `kartenklasse-fremd` mit „Die Kartenklasse 1 gehört zum Board 2, nicht zum Board 1 **dieser Karte**.“ — an dieser Route ist keine Karte im Spiel. Ein Agent, der die Meldung liest, sucht eine Karte, die es nicht gibt.

Zahlt ein auf: [Vision](R00000-vision.md) — „Was ein Mensch klicken kann, kann ein Agent aufrufen.“ Ein Mensch, der beim Entfernen einer Spalte scheitert, sieht seine archivierte Karte und holt sie zurück; der Agent sieht nur den Befund. Trägt die Kompensation nicht, ist die Gleichberechtigung an genau dieser Stelle aufgehoben.

**Beide Fälle sind dieselbe Sorte, deshalb eine Anforderung**: die Form stimmt, der Inhalt führt nicht weiter. Was fehlt, ist nicht eine weitere Formregel, sondern der Nachweis, dass die genannte Kompensation im Zustand, der sie ausgelöst hat, **wirklich geht**.

### Belege

| # | Befund | Beleg | Lage |
|---|---|---|---|
| 1 | `spalte-traegt-karten` | `Source/KanbanC.BL/Persistenz/Boards/SpaltenRepository.cs:174-181` | Kompensation rät `PUT …/lage`; für archivierte Karten 404 (Anmerkung **168**) |
| 2 | `kartenklasse-fremd` | `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs:146` | Meldung endet auf „dieser Karte“; an 4 von 5 Aufrufstellen ist keine Karte im Spiel (Anmerkung **392**) |

Die Sackgasse von Fall 1 ist belegt: `Kartenleser.LiesKartenNachPosition` filtert mit `AND a.Karte IS NULL` (`Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs:32`), also liegt eine archivierte Karte in keiner Spalte, die `SpaltenRepository.LadeAlle` liefert; `KartenService.VerschiebeKarte` weist deshalb mit `karte-unbekannt` zurück (`Source/KanbanC.BL/Integrations/Karten/KartenService.cs:76-79`). Zugleich zählt das Entfernen die archivierten Karten mit (`SpaltenRepository.cs:159-160`).

**Zur Herkunft:** Anmerkung 168 ist unter Slice `I0014` protokolliert. Nach `git log -S` stammt der Kompensationstext aus `R00007` (Commit `52abc1a` „Der Fehlervertrag traegt Code, Meldung und Kompensation“), und der Zweig, der ihn falsch macht, aus `R00016` (Commit `d1cc8d7` „Archivierte Karten einer Spalte lesen“). Das Frontmatter führt `R00009, R00025` nach Auftrag; siehe „Offene Fragen“.

## Ursachenanalyse

### Root Cause

Eine gemeinsame Wurzel, vier Ausprägungen. Die Wurzel ist, dass **niemand die Kompensation ausführt**. `FehlervertragTests` prüft, dass jede Fehlerantwort jedes Endpunkts einen Befund mit nichtleerem Code, nichtleerer Meldung und nichtleerer Kompensation trägt (`Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs:34-50` über `Infrastructure/Fehlerrumpf.cs:11-27`). Geprüft wird die **Gestalt** des Satzes, nie seine **Wirkung**. Solange „nichtleer“ das Kriterium ist, kann jede der vier Ausprägungen entstehen, ohne dass ein Test rot wird.

**Ausprägung A — die Kompensation rät einen Aufruf, der im selben Zustand scheitert.**
`Source/KanbanC.BL/Persistenz/Boards/SpaltenRepository.cs:174-181`. Der Befund zählt aktive **und** archivierte Karten (`:159-160`) und schickt für beide auf die Lage-Route. Für eine archivierte Karte ist das eine 404. Zusätzlich ist die genannte **Zahl auf keiner genannten Route nachprüfbar**: `GET /api/boards/{boardId}` zeigt nur die aktiven Karten; wer „3 Karten“ liest und dort eine findet, hat keinen Weg zu den anderen zwei. Der einzige Abruf, der archivierte Karten zeigt, ist `GET /api/boards/{boardId}/karten` (`Source/KanbanC.BL/Persistenz/Karten/Kartenleser.cs:49-67`) — die Kompensation nennt ihn nicht.

**Ausprägung B — die Adresse trägt Platzhalter statt Werten.** Acht Befundstellen in zwei Dateien, alle im Spalten-Bereich:

| Befundcode | Beleg | Adresse in der Kompensation |
|---|---|---|
| `spalte-traegt-karten` | `SpaltenRepository.cs:180` | `PUT /api/boards/{boardId}/karten/{karteId}/lage` |
| `spaltenbestand-geaendert` | `SpaltenRepository.cs:21-24` | `GET /api/boards/{boardId}`, `PUT /api/boards/{boardId}/spalten/reihenfolge` |
| `spalte-bezeichnung-vergeben` (Repository) | `SpaltenRepository.cs:27-30` | `GET /api/boards/{boardId}` |
| `spalte-bezeichnung-vergeben` (Validator) | `SpaltenValidator.cs:35-38` | `GET /api/boards/{boardId}` und `Spaltenroute` |
| `spalte-bezeichnung-leer` | `SpaltenValidator.cs:25-28` | `Spaltenroute` |
| `abschlussspalte-ohne-anzeigegrenze` | `SpaltenValidator.cs:51-54` | `Spaltenroute` |
| `anzeigegrenze-nicht-positiv` | `SpaltenValidator.cs:60-63` | `Spaltenroute` |
| `anzeigegrenze-ohne-abschlussspalte` | `SpaltenValidator.cs:69-72` | `Spaltenroute` |

`Spaltenroute` ist `"POST oder PUT auf /api/boards/{boardId}/spalten"` (`SpaltenValidator.cs:8`) — Platzhalter **und** zwei Methoden in einer Adresse. Kein Aufruf lässt sich daraus bilden. **Der ganze übrige Bestand macht es anders**: `AnhangValidator`, `DateiverweisValidator`, `EtikettenValidator`, `TeilaufgabenValidator`, `KommentarValidator`, `KartenValidator`, `KartenklassenValidator`, `KontributorenValidator`, `Archivfilter` und `Zeitraumfilter` bekommen die gerufene Adresse mit den Nummern des Aufrufers als Parameter herein — die Regel ist ausdrücklich niedergeschrieben in `Source/KanbanC.WebApi/Endpunkte/AuswertungsEndpunkte.cs:125-126` („Die Kompensation nennt die Adresse, die der Aufrufer wirklich gerufen hat — mit seinen Nummern, nicht mit den Platzhaltern der Routenvorlage.“). Der Spalten-Bereich ist die einzige Ausnahme, und zwar die älteste Stelle des Bestandes.

**Ausprägung C — die Kompensation nennt einen Planungsknoten statt einer Route.**
`Source/KanbanC.BL/Operations/Import/Importbefunde.cs:91-95` (`import-verweis-doppelt`): „Den Verweis an einer der beiden Karten entfernen (`I0019`) oder eine der beiden archivieren (`I0014`)“. `I0019` und `I0014` sind WBS-Knoten; ein Agent kennt sie nicht und findet an der API nichts unter diesem Namen. Dieselbe Stelle in `:113-117` (`import-schnittebene-abweichend`) nennt neben dem Knoten `I0020` immerhin die Route — dort ist der Knoten überflüssiges Beiwerk, hier ist er der **einzige** Hinweis auf den ersten Schritt.

**Ausprägung D — die Meldung spricht von einem Ding, das an der Adresse nicht vorkommt.**
`Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs:146`. `FremdeKartenklasse` hat fünf Aufrufstellen; an **vier** ist keine Karte im Spiel:

| Aufrufstelle | Route(n) | Karte im Spiel? |
|---|---|---|
| `Integrations/Klassen/KartenklassenService.cs:85` | `GET /api/boards/{b}/kartenklassen/{k}/karten` | nein |
| `Integrations/Auswertungen/AuswertungsService.cs:215` | `…/soll-ist`, `…/burndown`, `…/puffer`, `…/zeitexport`, `…/zeitexport.csv` | nein |
| `Operations/Import/Kartenklassenpruefung.cs:35` | `POST /api/boards/{b}/wbs-import` | nein |
| `Integrations/Karten/KartenService.cs:265` | `PUT /api/karten/{karteId}/kartenklasse` | ja |

Der Fall wurde beim Bau bewusst übernommen, weil `R00025` ausdrücklich verlangte, dass `Nichtgefunden` nicht wächst (`Anforderungen/R00025-karten-einer-klasse-abrufen.md:43`, WBS-Notiz zu `B0316`). Die Entscheidung ist dokumentiert und war für ihren Slice richtig; das Ergebnis bleibt falsch. Sie ist auch nicht der Grund, warum es so bleiben muss: die zwei Wörter „dieser Karte“ tragen keine Auskunft, die der Rest des Satzes nicht schon trägt.

### Betroffene Komponenten

- `Source/KanbanC.BL/Persistenz/Boards/SpaltenRepository.cs` — Ausprägungen A und B
- `Source/KanbanC.BL/Operations/Boards/SpaltenValidator.cs` — Ausprägung B
- `Source/KanbanC.BL/Operations/Import/Importbefunde.cs` — Ausprägung C
- `Source/KanbanC.BL/Operations/Fehler/Nichtgefunden.cs` — Ausprägung D
- `Source/KanbanC.WebApi/Endpunkte/SpaltenEndpunkte.cs` — reicht die gerufene Adresse durch, wie die übrigen Endpunkt-Klassen es tun
- `Source/KanbanC.BL/Interfaces/Boards/ISpaltenRepository.cs` und `Source/KanbanC.BL/Integrations/Boards/SpaltenService.cs` — Durchreichen der Adresse bzw. der Board-Nummer
- `Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs` — die Zusage wächst von der Form zur Wirkung
- `Source/KanbanC.BL.Tests/TestHelpers/TestSpaltenRepository.cs`, `Source/KanbanC.Blazor.Tests/Services/SpaltenApiKlientTests.cs` — führen den Befundtext als Testdaten und ziehen mit

**Unberührt:** `Source/KanbanC.BL/Persistenz/Migrationen/` (keine Migration), das Datenmodell, `Source/KanbanC.Blazor/` außer den mitziehenden Testdaten, `Source/KanbanC.PlaywrightTests/`. Kein Endpunkt kommt hinzu, keiner fällt weg, kein Statuscode ändert sich.

## Lösungsvorschlag

### Langfristige Lösung

**Gewählt: Option 2 — die vier Stellen sachlich richtigstellen und die Zusage „ausführbar“ im Vertragstest verankern.**

**1. Die Definition.** Eine Kompensation heißt **ausführbar**, wenn ein Aufrufer, der nichts hat als die Fehlerantwort, allein mit dem, was in ihr steht, zu einem erfolgreichen Aufruf kommt. Drei prüfbare Teilzusagen (siehe Akzeptanzkriterien):
- **A1 Adresse:** Jede Adresse in der Kompensation ist eine beim Host registrierte Route und trägt an jeder Stelle einen Wert, keinen Platzhalter.
- **A2 Erreichbarkeit:** Jeder in der Kompensation als erster Schritt genannte lesende Aufruf antwortet **im Zustand, der den Fehler erzeugt hat**, mit 2xx.
- **A3 Wirkung:** Wer die Kompensation ausführt und den ursprünglichen Aufruf wiederholt, bekommt eine erfolgreiche Antwort — oder einen **anderen** Befund, der eine Stelle weiter führt, nie denselben.

**2. Fall 1 — `spalte-traegt-karten` sagt die Wahrheit und nennt beide Wege.** Der Befund kennt bereits beide Zahlen (`karten.Count` und `archivierte.Count`); er nennt sie künftig getrennt und gibt je Sorte den Weg an, mit den Nummern des Aufrufers:

> Die Spalte „Zu erledigen“ enthält noch 3 Karten (2 aktive, 1 archivierte) und lässt sich deshalb nicht entfernen.
> Kompensation: `GET /api/boards/7/karten` abrufen und die KarteIds dieser Spalte ablesen — die aktiven mit `PUT /api/boards/7/karten/{KarteId}/lage` in eine andere Spalte verschieben; die archivierten zuerst über `PUT /api/boards/7/karten/{KarteId}/archivierung` mit „istArchiviert“ = false zurückholen und dann ebenso verschieben. Danach `DELETE /api/boards/7/spalten/4` wiederholen.

Die verbleibenden `{KarteId}`-Stellen sind kein Verstoß gegen A1: sie stehen für einen Wert, den der genannte vorherige Aufruf liefert, und sind als Feldname geschrieben, nicht als Routenplatzhalter. **A1 gilt für die Adresse, nicht für den Bezeichner eines abzulesenden Wertes**; die Prüfung greift auf `{boardId}`/`{spalteId}`-Stellen, die nirgends beschafft werden. `GET /api/boards/{boardId}/karten` ist der einzige Abruf, der archivierte Karten zeigt, und er macht die genannte Zahl nachprüfbar.

**3. Fall 2 — `kartenklasse-fremd` spricht von keiner Karte.** Zwei Wörter entfallen:

> Die Kartenklasse 5 gehört zum Board 9, nicht zum Board 2.

Der Satz ist an allen fünf Aufrufstellen richtig, auch an der Kartenroute — dort steht die Karte in der Adresse und wird nicht vermisst. **`Nichtgefunden` wächst dabei nicht**: keine neue Methode, kein neuer Code, kein Parameter mehr. Die Auflage aus `R00025` bleibt eingehalten.

**4. Ausprägung B — der Spalten-Bereich bekommt die gerufene Adresse herein**, wie es der übrige Bestand tut. `SpaltenValidator.Pruefe` und die drei `Pruefbefunde` des `SpaltenRepository` nehmen die Adresse bzw. `boardId` als Eingang; `Spaltenroute` wird durch die konkrete Adresse des Aufrufers ersetzt und verliert das „POST oder PUT“.

**5. Ausprägung C — `Importbefunde.VerweisDoppelt` nennt Routen statt Knoten.** `DELETE /api/karten/{karteId}/dateiverweise/{dateiverweisId}` bzw. `PUT /api/boards/{boardId}/karten/{karteId}/archivierung`, mit den Nummern der beiden gefundenen Karten. Die Knoten-IDs entfallen auch in `:113-117`.

**6. Die Zusage wird geprüft, nicht zugesagt.** `FehlervertragTests` wächst um zwei bestandsweite Tests (A1 statisch, A2 dynamisch) über dieselben Fälle, die es ohnehin einsammelt, plus je einen Kettentest für Fall 1 und Fall 2. Details unter „Test-Strategie“.

### Alternative Ansätze

- **Option 1 — nur die zwei gemeldeten Stellen ändern, kein Test über den Bestand.** Verworfen: dann bleibt die Wurzel stehen (niemand führt die Kompensation aus), die acht Platzhalter-Befundstellen bleiben unentdeckt, und der nächste Slice baut den nächsten Fall.
- **Option 3 — die Kompensation strukturieren: `Fehlerbefund` bekommt statt eines Satzes eine Liste maschinenlesbarer Schritte (Methode, Adresse, Rumpf).** Reizvoll, weil A1–A3 dann ohne Textanalyse prüfbar wären und ein Agent nicht mehr parsen müsste. Verworfen für diese Anforderung: es ändert den Kontrakt jeder Fehlerantwort, bricht `KanbanC.Contracts` und jeden Klienten, und ein Bugfix ist der falsche Anlass für eine Vertragsänderung. Gehört als eigene Anforderung auf den Tisch, wenn die Textprüfung sich als zu grob erweist — Notiz unten.
- **Option 4 — `PUT …/lage` archivierte Karten annehmen lassen.** Verworfen: eine archivierte Karte ist ausdrücklich kein Bestand (`Kartenleser.cs:13-14`), und ein Zug auf ihr wäre eine stille Entarchivierung. Der Fehler liegt im Satz, nicht in der Route.
- **Option 5 — das Entfernen einer Spalte nimmt ihre archivierten Karten mit.** Verworfen: das ist stiller Datenverlust und eine fachliche Entscheidung, keine Fehlerbehebung.

## Test-Strategie

### Unit Test zur Bug-Reproduktion

Beide Fälle bekommen einen Test, **beide sind vor der Behebung rot**. Die Ebene ist Integration (WebApi), nicht Unit: die Zusage lautet „ein Aufrufer kommt weiter“, und wer weiterkommt, ruft die API.

**T1 — Fall 1, die Kette (`FehlervertragTests`, neu).**
`Wenn_eine_Spalte_nur_archivierte_Karten_traegt_dann_fuehrt_ihre_Kompensation_zum_erfolgreichen_Entfernen`
- *Arrange:* Board anlegen; in Spalte 1 eine Karte anlegen; die Karte über `PUT /api/boards/1/karten/1/archivierung` mit `istArchiviert = true` archivieren.
- *Act:* `DELETE /api/boards/1/spalten/{spalte1}` → 400 `spalte-traegt-karten`. Aus dem Kompensationstext die genannten Adressen ziehen und **der Reihe nach ausführen**; danach das `DELETE` wiederholen.
- *Assert:* kein Zwischenschritt antwortet 4xx; das wiederholte `DELETE` antwortet 2xx.
- *Heute rot:* der Zwischenschritt `PUT /api/boards/{boardId}/karten/{karteId}/lage` ist als Adresse gar nicht bildbar, und mit eingesetzten Nummern antwortet er 404 `karte-unbekannt`. Der Test scheitert also aus **zwei** Gründen, die beide zur Sackgasse gehören.

**T2 — Fall 2, die Meldung (`FehlervertragTests`, neu).**
`Wenn_die_Kartenklasse_eines_anderen_Boards_abgerufen_wird_dann_spricht_die_Meldung_von_keiner_Karte`
- *Arrange:* zwei Boards; auf dem zweiten eine Kartenklasse (der Aufbau steht schon: `FehlervertragTests.LegeAufbauAn` legt `FremdeKartenklasse` an).
- *Act:* `GET /api/boards/{erstes}/kartenklassen/{fremde}/karten`.
- *Assert:* 404, Code `kartenklasse-fremd`, und die Meldung enthält das Wort „Karte“ nicht; sie lautet wörtlich „Die Kartenklasse {k} gehört zum Board {b2}, nicht zum Board {b1}.“
- *Heute rot:* die Meldung endet auf „dieser Karte“.
- *Gegenprobe im selben Test oder daneben:* an `PUT /api/karten/{karteId}/kartenklasse` bleibt derselbe Code und dieselbe Meldung — die Änderung nimmt der Kartenroute nichts.

### Code-Extraktion für isoliertes Testing

Die Prüfung von A1 braucht eine reine Funktion, sonst steckt sie im Testkörper. Neu in den Integrationstests (nicht in der BL — sie ist Prüflogik, kein Fachwissen):

- `Kompensationsadressen.Aus(string kompensation)` → `IReadOnlyList<Kompensationsadresse>`; `Kompensationsadresse(string? Methode, string Pfad)`. Zieht die Adressen aus dem Satz (Backtick-Abschnitte und `/api/…`-Vorkommen).
- `Kompensationsadresse.IstAufgeloest` — kein `{…}` im Pfad außer den ausdrücklich als Ablesewert markierten Feldnamen.
- `Routenabgleich.Passt(Kompensationsadresse, IReadOnlyList<string> registrierteRouten)` — ersetzt Zahlensegmente durch die Platzhalter der Routenvorlage und vergleicht.

Alle drei sind pure Logik und bekommen eigene Unit-Tests mit Beispielsätzen aus dem Bestand (ein tragender, ein platzhaltriger, einer ohne Adresse).

### Regressionstests

**Unit** (`KanbanC.BL.Tests`):
- `NichtgefundenTests` — der Text von `FremdeKartenklasse` wird auf den neuen Wortlaut festgelegt; `MeldetEinFehlendesDing` bleibt `true` (der Code ändert sich nicht).
- `SpaltenValidatorTests` — jeder der fünf Befunde nennt die übergebene Adresse mit Werten; kein `{`, kein „POST oder PUT“.
- `ImportbefundeTests` — `VerweisDoppelt` nennt zwei Routen und keine Knoten-ID.
- Die drei neuen Prüffunktionen (siehe oben).

**Integration** (`KanbanC.WebApi.IntegrationTests`) — die zwei bestandsweiten Tests, die aus der Formzusage eine Wirkungszusage machen:
- `Jede_Kompensation_nennt_nur_registrierte_Routen_mit_konkreten_Werten` (A1): über alle Fälle aus `AlleFehlerantworten`; je Adresse `IstAufgeloest` und `Routenabgleich.Passt(…, webApi.Routen)`. Heute rot durch die Ausprägungen A, B und C.
- `Jeder_lesende_erste_Schritt_einer_Kompensation_antwortet_im_Fehlerzustand_mit_2xx` (A2): die erste `GET`-Adresse jeder Kompensation im selben Zustand ausführen. Deckt „zeigt ins Leere“ allgemein ab, auch für Fälle, die niemand gemeldet hat.
- `AlleFehlerantworten` wird um die Fälle ergänzt, die heute fehlen, damit **jede** der acht Befundstellen aus Ausprägung B mindestens einmal durch die Prüfung läuft — sonst ist die Zusage nur so breit wie die Fallliste. Der bestehende Test `Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind` deckt Routen ab, nicht Befunde; die Lücke bleibt sonst offen.
- `SpaltenRepositoryTests:395,456` (`spalte-traegt-karten`) werden um die Zahlenaufteilung und den Archivfall erweitert.

**E2E** (`KanbanC.PlaywrightTests`): keiner. Kein Schirm ändert sich, und der Fehler ist ein Fehler des API-Vertrags. `SpaltenApiKlientTests:156` führt den Befundtext als Testdatum und zieht mit.

## Akzeptanzkriterien

### Der Reproduktionsnachweis
- [ ] `T1` und `T2` sind **vor** der Behebung rot und **nach** ihr grün; beide scheitern vorher an der Sache, nicht an einer Zusicherung über Formulierungen.
- [ ] Der ursprüngliche Fehler tritt in den Szenarien der User Story nicht mehr auf.
- [ ] Keine neuen Fehler: alle Tests aller Ebenen grün, Coverage nicht gefallen, `TreatWarningsAsErrors` hält.

### „Ausführbar“ — die drei Teilzusagen
- [ ] **A1 Adresse:** Für jede Fehlerantwort jeder Route gilt: jede Adresse in `kompensation` ist eine beim Host registrierte Route (Abgleich gegen `TestWebApi.Routen`) und enthält keine Stelle der Form `{name}`, außer sie benennt einen Wert, den ein in derselben Kompensation **vorher** genannter Abruf liefert (`{KarteId}` nach `GET …/karten`). Rechenbeispiel: `PUT /api/boards/{boardId}/karten/{karteId}/lage` fällt durch (zwei unbeschaffte Stellen); `PUT /api/boards/7/karten/{KarteId}/lage` nach vorangehendem `GET /api/boards/7/karten` besteht.
- [ ] **A2 Erreichbarkeit:** Ist der erste Schritt einer Kompensation ein lesender Aufruf, antwortet er im Zustand, der den Fehler erzeugt hat, mit 2xx.
- [ ] **A3 Wirkung:** Für die beiden gemeldeten Fälle gilt: nach Ausführung der Kompensation antwortet der ursprüngliche Aufruf mit 2xx — nicht erneut mit demselben Befund.
- [ ] Die drei Teilzusagen stehen als Tests in `FehlervertragTests`, nicht als Satz in einem Kommentar.

### Fall 1 — die Spalte mit archivierten Karten
- [ ] `DELETE /api/boards/{b}/spalten/{s}` auf eine Spalte mit **nur** archivierten Karten liefert 400 `spalte-traegt-karten`, dessen Kompensation den Weg über `PUT /api/boards/{b}/karten/{k}/archivierung` mit `istArchiviert = false` nennt.
- [ ] Die Meldung nennt die Zahl aufgeteilt: Rechenbeispiel — 2 aktive und 1 archivierte Karte ergeben „3 Karten (2 aktive, 1 archivierte)“, nicht „3 Karten“.
- [ ] Die Kompensation nennt `GET /api/boards/{b}/karten` als den Abruf, in dem die genannte Zahl nachprüfbar ist; jede genannte Adresse trägt die Board- und Spaltennummer des Aufrufers.
- [ ] Die Kette aus T1 endet mit einem erfolgreichen `DELETE`.

### Fall 2 — die fremde Kartenklasse
- [ ] Die Meldung von `kartenklasse-fremd` lautet an **allen fünf** Aufrufstellen „Die Kartenklasse {k} gehört zum Board {b2}, nicht zum Board {b1}.“ und enthält das Wort „Karte“ nicht mehr.
- [ ] Der Code `kartenklasse-fremd` und der Statuscode 404 bleiben unverändert; `Nichtgefunden.MeldetEinFehlendesDing` kennt ihn weiterhin.
- [ ] **`Nichtgefunden` wächst nicht**: keine zusätzliche Methode, keine zusätzliche Überladung, kein zusätzlicher Parameter — die Auflage aus `R00025` bleibt eingehalten.

### Der Bestand
- [ ] Die acht Befundstellen der Ausprägung B nennen ihre Adresse mit den Nummern des Aufrufers; `SpaltenValidator.Spaltenroute` in der Form „POST oder PUT auf …“ existiert nicht mehr.
- [ ] `Importbefunde.VerweisDoppelt` und `Importbefunde.SchnittebeneAbweichend` nennen keine WBS-Knoten-ID mehr, sondern für jeden Schritt eine Route.
- [ ] Jeder Befund, der durch diese Anforderung geändert wird, kommt mindestens einmal in `AlleFehlerantworten` vor — sonst prüft A1 ihn nicht.

## Implementierungshinweise

- **Reihenfolge:** zuerst T1/T2 rot stellen, dann die bestandsweiten Tests A1/A2 (die auf einen Schlag mehrere Stellen rot färben), dann die vier Ausprägungen der Reihe nach grün machen. Wer umgekehrt anfängt, hat keinen Beweis, dass die Tests je rot waren (Skill `test-ehrlichkeit`).
- **Adresse durchreichen, nicht bauen:** `SpaltenEndpunkte` gibt die Adresse mit den Werten des Aufrufers hinein, genau wie `KartenklassenEndpunkte.Kartenlisteroute` und `AuswertungsEndpunkte.Adresse` es tun. Nicht in der BL zusammensetzen — dort steht die Routenvorlage nicht.
- **`SpaltenRepository` bekommt `boardId` bereits** (`LoescheSpalte(verbindung, transaktion, boardId, spalteId)`), die Spaltennummer ebenso; für `SpalteTraegtNochKarten` genügt, sie durchzureichen. Die drei statischen `Pruefbefunde`-Felder am Klassenkopf werden zu Methoden mit Parametern — sie können nicht mehr `static readonly` bleiben.
- **IOSP:** `SpaltenRepository` ist Integration (Projektkonvention); die Befundbildung bleibt eine reine Operation und wandert bei Bedarf zu `SpaltenValidator` bzw. in eine eigene Operation, statt im Repository zu wachsen.
- **C06/C07:** neue Bezeichner deutsch und ohne echte Umlaute (`Kompensationsadresse`, `Routenabgleich`, `IstAufgeloest`); die Meldungs- und Kompensationstexte tragen echte Umlaute.
- **C08:** `Kompensationsadresse` ist ein immutables `record`.
- **Die Kettentests brauchen keinen Parser für Schreibaufrufe.** T1 liest die Adressen aus dem Text und führt sie aus; welchen Rumpf ein `PUT` braucht, steht nicht im Satz und wird im Test gesetzt. Das ist bewusst so: A1/A2 sind maschinell über den ganzen Bestand prüfbar, A3 ist es je Befund von Hand. Ein Anspruch, A3 bestandsweit automatisch zu prüfen, verlangte die strukturierte Kompensation aus Option 3.
- **`Fehlerrumpf.Lies` bleibt, wie es ist** — die Formprüfung ist weiterhin richtig, sie ist nur nicht mehr die ganze Zusage.

## Offene Fragen

- **Das Frontmatter führt `ursprungslauf: R00009, R00025` nach Auftrag; belegt ist etwas anderes.** `git log -S` weist den Kompensationstext von Fall 1 `R00007` zu (`52abc1a`) und den Zweig, der ihn falsch macht, `R00016` (`d1cc8d7`); Anmerkung 168 ist unter Slice `I0014` protokolliert, nicht unter `I0004`. Fall 2 ist mit `R00025` korrekt zugeordnet (Anmerkung 392, Slice `I0022`), stammt aber als Methode aus `R00023`. Soll das Feld auf `R00016, R00025` gehen, damit die Ist-Zeit als Nacharbeit des Laufs verbucht wird, der den Fehler eingebracht hat? — **Antwort abwarten, das Feld sonst unverändert lassen.**
- **Wie weit soll die Textprüfung gehen?** A1 und A2 lesen Prosa mit einem Regex. Das ist heute genug, wird aber spröde, sobald jemand eine Adresse ohne Backticks schreibt. Alternative: Option 3 (strukturierte Kompensation) als eigene Anforderung. Soll sie jetzt angelegt werden, oder erst, wenn die Textprüfung das erste Mal falsch anschlägt?
- **Sieben Bug-Anforderungen entstehen parallel (`R00043`, `R00045`–`R00050`).** Deckt eine davon die Ausprägung B (Platzhalter im Spalten-Bereich) oder C (Knoten-IDs im Import) bereits ab? Wenn ja, fallen sie hier heraus und die Kriterien unter „Der Bestand“ werden gestrichen; die Definition von „ausführbar“ und die bestandsweiten Tests bleiben in jedem Fall hier.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist konkret: ein Agent, der eine Spalte entfernen will, bekommt eine wohlgeformte Fehlerantwort, folgt ihr, bekommt eine 404 und hat keinen Weg mehr — und ein zweiter, der eine fremde Kartenklasse abruft, sucht eine Karte, die es in seinem Aufruf nie gab. Das Zielbild ist, dass die Fehlerantwort dasselbe leistet wie der Blick eines Menschen auf den Schirm: sie sagt, was als Nächstes geht. Die Kausalkette: **wenn die Kompensation im Test wirklich ausgeführt wird (X)**, dann fällt jede Anweisung durch, die ins Leere zeigt, einen Platzhalter trägt oder auf einen Planungsknoten verweist **(Y)**, und damit ist jede Fehlerantwort ein Weg statt einer Sackgasse **(Z)**. Der Hebel liegt genau hier und nicht vorgelagert: Regel, Kontrakt und Formprüfung sind alle vorhanden und alle eingehalten — es fehlt allein die Prüfung der Wirkung, und ohne sie entsteht der nächste Fall im nächsten Slice, weil „nichtleer“ jede Ausrede durchlässt. Nachgelagert — etwa im Agenten, der die Antwort liest — ließe sich nichts reparieren: er hat nur, was im Satz steht.

## Notizen

- Priorität: **Hoch**. Kein Datenverlust, aber der Ausfall trifft genau die Nutzergruppe, für die die Vision gebaut ist, und er trifft sie unsichtbar: die Antwort sieht heil aus.
- Betroffene Nutzer/Systeme: KI-Agenten an der API. Menschen am Browser merken den Fehler nicht — sie sehen die archivierte Karte und den Rückholschalter.
- Workaround: Der Agent ruft `GET /api/boards/{boardId}/karten` von sich aus ab und errät den Archivfall. Genau das soll er nicht müssen.
- **Verworfene Alternativen** — je ein Satz unter „Alternative Ansätze“.
- **Bewusst out of scope, geprüft und beurteilt:**
  - `position-ausserhalb` (`Operations/Karten/KartenlageValidator.cs:29`) und `bestand-geaendert` (`Persistenz/Karten/KartenRepository.cs:690`) raten „die Karten der Zielspalte zählen“ auf `GET /api/boards/{boardId}`; an einer **Abschlussspalte** ist die Kartenliste dort auf die `Anzeigegrenze` gekürzt (`Operations/Karten/Abschlussbahn.cs:35-43`), die richtige Zahl steht nur im Feld `kartenzahl`. Wer zählt, zählt zu wenig. Keine Sackgasse — die Meldung nennt die gültige Spanne selbst und die Adresse ist tragend —, aber dieselbe Sorte Ungenauigkeit; Kandidat für `/anforderung aus-bug`, wenn nicht schon eine der parallelen Anforderungen ihn führt.
  - `zeitspanne-ende-vor-beginn` und `zeitpunkt-in-der-zukunft` (`Operations/Zeiten/Zeitspanne.cs:48,84`) nennen **keine** Adresse („Den Aufruf … wiederholen“). Ausführbar, weil der Aufrufer seinen eigenen Aufruf kennt; A1 greift nicht, weil keine Adresse dasteht. Sie weichen aber vom Kontrakt „Kompensation als ausführbarer nächster Schritt **mit Route**“ (`Fehlerbefund.cs:6`) ab. Bewusst gelassen: eine Adresse zu ergänzen ist eine Formfrage, kein Fehler in der Wirkung.
- **Nicht geändert wird der Fehlervertrag selbst**: Code, Statusabbildung und Antwortgestalt bleiben, wie sie sind. Diese Anforderung macht wahr, was er schon zusagt.

## Missing-Docs

- Es gibt keine eine Stelle, an der die Regel „Kompensation nennt die Adresse des Aufrufers mit seinen Nummern“ niedergeschrieben ist; sie steht als Kommentar an drei Stellen (`AuswertungsEndpunkte.cs:125-126`, `Archivfilter.cs:8-11`, `Zeitraumfilter.cs:9-11`) und wurde deshalb im Spalten-Bereich nie befolgt. Nach dieser Anforderung ist der Test die Stelle — ein Satz in `CLAUDE.md` unter „Datenzugriff“/„Code schreiben“ wäre die zweite; das entscheidet `/lerne-aus-vorgang`, nicht diese Anforderung.
