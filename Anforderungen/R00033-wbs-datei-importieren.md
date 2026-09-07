---
id: R00033
status: Neu
datum: 2026-09-07
---

# R00033: WBS-Datei importieren

## Beschreibung

Eine WBS-Datei wird hochgeladen, gelesen und als Karten auf ein bestehendes Board gelegt: jeder Knoten der gewählten **Schnittebene** wird eine Karte der gewählten Kartenklasse, mit Nummer, Titel in der `/github`-Hausform, Beschreibung, dem Dialog als Etikett, den Knoten darunter als abhakbare Teilaufgaben und einem Dateiverweis zurück auf die Zeile in der Quelldatei. Geschrieben wird erst im dritten Schritt — davor zeigt eine Vorschau, was entstehen würde, und **wie viele Karten** jede Wahl der Schnittebene ergibt.

Zahlt ein auf: [Vision](R00000-vision.md) — die Brücke zwischen Planung und Board. Die WBS ist in diesem Projekt die Fortschrittswahrheit; sie steht heute nur als Markdown im Repository. `R00033` bringt sie erstmals dorthin, wo gearbeitet wird, ohne sie zur zweiten Wahrheit zu machen.

**Die eine Regel dieses Slice ist die Abbildung Baum → Board.** Sie ist keine Beigabe des Entwurfs, sondern der Inhalt:

| Was im Baum | Was auf dem Board | Woher das Bauteil stammt |
|---|---|---|
| **Application** | das **gewählte Zielboard** — es wird **nie** angelegt | `I0001`, unverändert |
| **jeder Vorfahre oberhalb der Schnittebene** (außer der Application) | **Etikett** der Karte | `I0015` |
| **die Schnittebene selbst** (Vorgabe: **Interaction**) | die **Karte** | `I0011` / `I0021` |
| **alles darunter** (Feature, Bubble) | **Teilaufgaben**, flach, in Dateireihenfolge, ID vorn, Status `gruen` → Haken | `I0016` |
| **Herkunft** (`…/kanbanc.md#I0001`) | **Dateiverweis** | `I0019` |
| **Status** | **Zielspalte**: `gruen` → Abschlussspalte, alles andere → erste Spalte | `I0003` |
| **Aufwand**, `Eingabe → Ausgabe`, `Ausbaustufe` | **nichts** — Entwurfsangaben ohne Ort im Bestand | s. u., Preis 3 |

**Die Schnittebene ist der Grund, aus dem die Vorschau überhaupt existiert.** An der echten Datei dieses Projekts gemessen (`Dokumentation/Planung/kanbanc.md`, Stand 2026-09-07, 540 Knotenzeilen):

| Schnittebene | Karten | Teilaufgaben |
|---|---|---|
| Dialog | **9** | 530 |
| **Interaction** (Vorgabe) | **41** | 489 |
| Feature | **79** | 523 |
| Bubble | **445** | 5 |

Eine Datei, die 41 Karten ergeben soll, ergibt bei einem Klick daneben 445. **Deshalb steht die Zahl je Wahl in der Antwort, bevor jemand sie erzeugt** — für den Menschen als Regler in Schritt 2, für einen Agenten als `trocken`-Antwort desselben Aufrufs.

**Drei Schritte, geschrieben wird erst im dritten:** Datei wählen → Vorschau → schreiben. `POST /api/boards/{boardId}/wbs-import` trägt dafür das Feld `trocken`; mit `trocken=true` gibt es dieselbe Antwort ohne jede Schreibwirkung. Damit gilt „was die Oberfläche kann, kann die API" auch für das Zeigen-vor-Schreiben.

**`I0030` legt nur an.** Wiedererkennung, Soll-Ist-Vergleich und die vier Fächer sind `I0031`; der Bericht mit einer Zeile je Knoten ist `I0032`. Beide sind hier nicht enthalten und werden nicht vorweggenommen.

## Geschäftlicher Nutzen

Die Projekt-CLAUDE.md sagt: „**Die WBS ist die Fortschrittswahrheit**; weicht eine andere Liste ab, hat die WBS recht." Diese Wahrheit liegt heute in einer Markdown-Tabelle mit 540 Zeilen, die niemand bedient, sondern nur liest. Jeder Blick auf den Stand ist ein Griff in eine Datei; jede Zuteilung, jede Bahn, jede Zeitmessung findet daneben statt oder gar nicht.

Der Import macht aus der Planung **einen bedienbaren Bestand**, ohne die Datei zu entwerten: die Karte trägt einen Dateiverweis auf ihre Zeile, und die Datei bleibt die Quelle. Das ist der Unterschied zwischen „das Board zeigt den Plan" und „das Board **ist** ein zweiter Plan".

Der zweite Nutzen ist die Rechnung, die dieser Slice erst möglich macht: `I0033` („Soll-Ist-Vergleich abrufen") führt `I0030` ausdrücklich in seiner Spalte `Braucht`. Ohne die Knoten auf dem Board gibt es nichts, dem die erfasste Zeit gegenüberstünde.

Der dritte ist die Zusage an Agenten. Ein Mensch lädt eine Datei über die Ablegefläche; ein Agent schickt **dieselbe Datei an dieselbe Route** und bekommt **dieselbe Antwort**. Deshalb wohnt der Leser in `KanbanC.BL/Operations/Import/` und nicht in der Oberfläche — ein Parser in `KanbanC.Blazor` wäre ein Weg, den die API nicht hätte, und genau das verbietet die Kernregel des Projekts.

## Funktionale Anforderungen

- Eine hochgeladene Markdown-Datei mit Frontmatter und Knotentabelle wird zu einem Knotenbaum in **Dateireihenfolge**.
- Eine Datei ohne `application:` im Frontmatter oder ohne Knotentabelle wird **zurückgewiesen** — mit Grund, Werten und Kompensationsaktion, ohne dass ein Board berührt wird.
- Eine Zeile mit abweichender Zellenzahl, unbekannter Ebene, unbekanntem Status, leerem Namen oder unbekanntem Eltern wird **übersprungen und gemeldet**, nicht verschwiegen.
- Ein maskiertes Trennzeichen (`\|`) in einer Zelle bleibt ein **Zeichen** und trennt keine Zelle.
- Die **Schnittebene** ist ein Feld der Anfrage mit der Vorgabe `Interaction`; wählbar sind `Dialog`, `Interaction`, `Feature` und `Bubble`.
- Die Antwort nennt die **Kartenzahl je Wahl** der Schnittebene, nicht nur für die gewählte.
- Mit `trocken=true` liefert der Aufruf Bilanz und Zeilen und schreibt **nichts**. **`trocken=true` ist die Vorgabe.**
- Das **Zielboard wird gewählt und nie angelegt**; die Application der Datei bestimmt es nicht.
- Die **Kartenklasse muss vorher existieren** und dem Board gehören; ein Board ohne Klasse wird mit Grund und Kompensationsaktion zurückgewiesen.
- Die Anfrage nennt den **Urheber** (`kontributor`); ohne ihn entsteht keine Karte.
- Ohne `trocken` schreibt derselbe Aufruf **in einer Transaktion**: Karten mit Nummer, Etiketten, Teilaufgaben samt Haken, Dateiverweise — alles oder nichts.
- Jede Karte trägt den **Dateiverweis auf ihre Zeile** in der Form `<pfad>#<ID>`; der Pfad kommt als Feld der Anfrage, fehlt er, gilt der Dateiname.
- Karten, die in der **Abschlussspalte** entstehen, bekommen den **Erledigungszeitpunkt** des Laufs.
- Der Schirm `/boards/{BoardId}/import` führt in drei Schritten von der Datei über die Vorschau zum Schreiben; der Einstieg sitzt im **Layout-Modus** des Boards.
- Nach dem Schreiben nennt der Schirm **eine Zeile** — die Zahl der angelegten Karten — und führt zum Board.
- Ein erfolgreicher Import meldet auf `GET /api/ereignisse` **genau ein** Ereignis; jede offene Sicht dieses Boards lädt danach **einmal** neu.
- Ein zurückgewiesener Import meldet **nichts**.

## Nicht-funktionale Anforderungen

- **Kernregel:** der Leser wohnt in `KanbanC.BL/Operations/Import/`. `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL` und keinen eigenen Parser.
- **Kein Markdown-Paket** in keinem Projekt. Begründung mit Messung unter „Technische Überlegungen".
- **Kein neues Schema, keine Migration.** Nachgesehen: `Karte` (003), `Kartenetikett` (011), `Kartenteilaufgabe` (012), `Kartendateiverweis` (015) und `Kartenklassenzuordnung` (017) stehen alle; der Migrationslauf bleibt bei 18 Skripten.
- **Die Datei wird nicht gespeichert** — weder als Anhang noch sonst. Preis benannt (s. u.).
- **Eine Transaktion über den ganzen Lauf.** Auf der echten Datei sind das bei Interaction-Schnitt **684 Schreibvorgänge** (41 Karten + 41 Zuordnungen + 41 Etiketten + 489 Teilaufgaben + 41 Dateiverweise + 31 Kartennummernstände).
- **`UNIQUE(Kartenklasse, Zaehlerstand)`** aus 017 hält auch für 41 Vergaben in einem Zug.
- **Gestaltung:** alle Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei. Der Schirm folgt `D0008.dc.html`.
- **Fehlerform:** jede Zurückweisung ist ein `Fehlerbefund` mit Code, Meldung mit den konkreten Werten und ausführbarer Kompensation — auch bei 404. Nie „ungültiges Format".
- **Umlaute:** C07 gilt für Bezeichner; Anzeigetexte, Meldungen und Kommentare tragen echte Umlaute.
- **Größe:** die Anwendung muss eine Zeile von **8.162 Zeichen** und eine Zelle von **7.990 Zeichen** unbeschadet durchreichen (gemessen, s. u.).

## Akzeptanzkriterien

### Die Datei wird gelesen (`F0051`)

- [ ] Aus Frontmatter und Knotentabelle entsteht ein Knotenbaum mit **ID, Ebene, Eltern, Name, Status und den Textspalten**, in **Dateireihenfolge**.
- [ ] Das Frontmatter liefert `application`, `sprache` und `zuletzt`; **ohne Block oder ohne `application:` ist es keine WBS**.
- [ ] Eine Datei ohne `application:` **oder** ohne Knotentabelle wird zurückgewiesen — mit Dateiname, den fehlenden Angaben und dem Weg über `/planung anlegen`; **kein Board wird berührt**.
- [ ] Eine Knotenzeile zerfällt in **zwölf Zellen**. Rechenbeispiel: die 540 Knotenzeilen von `kanbanc.md` liefern beim Zerlegen jeweils genau **14 Teile** (zwölf Zellen zwischen zwei leeren Rändern).
- [ ] Ein **maskiertes Trennzeichen** `\|` in einer Zelle bleibt Zeichen und erhöht die Zellenzahl nicht.
- [ ] Eine Zeile mit **abweichender Zellenzahl** wird als übersprungen gemeldet, mit Zeilennummer und gefundener Zahl.
- [ ] Eine Zeile mit **unbekannter Ebene, unbekanntem Status oder leerem Namen** wird übersprungen — mit dem gefundenen Wert im Grund.
- [ ] Ein Knoten mit **unbekanntem Eltern** wird übersprungen; sein Teilbaum verschwindet nicht still, sondern wird mitgemeldet.
- [ ] Der Leser braucht **kein Board**: er ist ohne eines vollständig prüfbar.
- [ ] Die **echte Datei** läuft durch: `kanbanc.md` (Stand 2026-09-07) ergibt **540 Knoten** — 1 Application, 9 Dialogs, 41 Interactions, 54 Features, 435 Bubbles — und **null** übersprungene Zeilen.
- [ ] Eine **Zelle mit 8.000 Zeichen** und eine **Zeile mit 8.200 Zeichen** laufen unbeschadet durch.

### Zeigen vor Schreiben (`F0052`)

- [ ] `POST /api/boards/{boardId}/wbs-import` mit `trocken=true` liefert **200** mit Bilanz und Zeilen und **schreibt nichts** — nach dem Aufruf steht keine Karte mehr auf dem Board als davor.
- [ ] **Fehlt `trocken`, wird nichts geschrieben**: die Vorgabe ist `true`.
- [ ] Die **Schnittebene** ist ein Feld der Anfrage mit der Vorgabe `Interaction`; sie reist als **Wort** im JSON (`"Interaction"`), wie `Ereignisweg` und `Kontributorart`.
- [ ] Die Antwort nennt die **Kartenzahl je Wahl**. Rechenbeispiel für `kanbanc.md`: Dialog **9**, Interaction **41**, Feature **79**, Bubble **445**.
- [ ] **Über der Schnittebene wird Etikett, die Schnittebene wird Karte, darunter wird Teilaufgabe** — flach, in Dateireihenfolge.
- [ ] **Kein Knoten geht still verloren:** ein Knoten oberhalb der Schnittebene, der keinen Nachfahren auf der Schnittebene hat, wird selbst zur Karte. Rechenbeispiel: bei Feature-Schnitt sind es **79** Karten — 54 Features plus 25 Interactions ohne Feature.
- [ ] Die **Application wird das gewählte Zielboard** und wird **nie** angelegt; ein Import auf ein nicht vorhandenes Board ergibt **404** mit der Boardnummer und dem Weg zur Board-Liste.
- [ ] Der **Titel** lautet `[I0001] Board anlegen` — ID in eckigen Klammern, dann der Name.
- [ ] Die **Beschreibung** entsteht aus Fertig-Kriterium, Notiz und `Braucht`; **Aufwand, `Eingabe → Ausgabe` und `Ausbaustufe` werden nicht übernommen**. Verantwortlicher, Fälligkeit und Farbe bleiben leer.
- [ ] Die **Zielspalte** folgt dem Status: `gruen` → Abschlussspalte, **alles andere** → erste Spalte nach Position. Rechenbeispiel für `kanbanc.md` bei Interaction-Schnitt: **31** Karten in die Abschlussspalte, **10** in die erste.
- [ ] `bestehend` zählt wie `gruen` (Abschlussspalte, Haken). Beleg: der Skill `work-breakdown-structure` rechnet „alle zählenden Kinder `gruen` (oder `bestehend`) → `gruen`". In `kanbanc.md` trägt genau **eine** Zeile diesen Status (`B0380`).
- [ ] `verworfen`, `option` und `ausbau` werden **übersprungen mit Grund** — sie zählen auch in der WBS nicht.
- [ ] Hat das Board **keine Kartenklasse** oder gehört die genannte einem anderen Board, wird zurückgewiesen — mit Boardname, Klassennummer und dem Weg über den Layout-Modus. **Der Import legt nie eine Klasse an.**
- [ ] Fehlt der **Urheber** (`kontributor`), wird zurückgewiesen — mit dem Weg zur Kontributorenliste.
- [ ] Der **Importbericht** trägt `angelegt`, `geaendert`, `unveraendert`, `uebersprungen` und eine Zeile je Knoten; `geaendert` und `unveraendert` sind in diesem Slice **immer 0** (sie füllt `I0031`).
- [ ] Der Endpunkt nimmt **multipart** an (`datei`, `klasse`, `schnittebene`, `pfad`, `trocken`, `kontributor`) und ist wie der Anhang von der Antiforgery-Prüfung ausgenommen.

### Die Knoten werden Karten (`F0053`)

- [ ] Derselbe Aufruf **ohne `trocken`** antwortet **201** mit demselben Bericht, und die Karten stehen auf dem Board.
- [ ] Geschrieben wird in **einer Transaktion**: bricht ein Schritt ab, steht danach **keine** Karte des Laufs.
- [ ] Jede Karte trägt eine **Kartennummer** der gewählten Klasse; der **Zählerstand wächst je Karte**. Rechenbeispiel: Klasse `WBS` mit Stand 0, 41 Karten → Nummern `WBS01` bis `WBS41`, Stand danach **41**.
- [ ] `UNIQUE(Kartenklasse, Zaehlerstand)` wird durch den Lauf **nicht** verletzt.
- [ ] Jede Karte trägt das **Etikett** ihres Dialogs (Beispiel: `Boards führen`); bei tieferer Schnittebene zusätzlich die Namen der Vorfahren bis zum Dialog.
- [ ] Jede Karte trägt ihre Nachfahren als **Teilaufgaben**, flach, in Dateireihenfolge, mit der **ID vorn** (`B0405 Probe: die echte WBS-Datei durch den Leser`).
- [ ] Eine Teilaufgabe mit Status `gruen` (oder `bestehend`) ist **abgehakt**. Rechenbeispiel für `kanbanc.md` bei Interaction-Schnitt: **489** Teilaufgaben, davon **454** abgehakt.
- [ ] Jede Karte trägt den **Dateiverweis** `<pfad>#<ID>` — Beispiel `Dokumentation/Planung/kanbanc.md#I0001`.
- [ ] Fehlt das Feld `pfad`, gilt der **Dateiname** der hochgeladenen Datei.
- [ ] Eine Karte, die in der **Abschlussspalte** entsteht, bekommt `ErledigtAm` auf den **Tag des Laufs** — sonst stünden alle grünen Karten in der Datumsgruppierung aus `I0013` in einer Gruppe **ohne Datum**.
- [ ] Der **Einstieg** sitzt im Layout-Modus des Boards unter der Klassenpflege; **ohne Kartenklasse** steht dort der Weg dorthin statt des Knopfes.
- [ ] **Schritt 1** nutzt die vorhandene Ablegefläche aus `R00024` unverändert; **beide Sperrgründe gelten** (keine Identität gewählt, ein Vorgang läuft).
- [ ] **Schritt 1** zeigt den gleichwertigen API-Aufruf daneben.
- [ ] **Schritt 2** zeigt den Baum links lesend, die Wirkung rechts und die Schnittebene als Regler **mit der Kartenzahl je Wahl**.
- [ ] **Schritt 3** ist **eine Zeile** („41 Karten angelegt") plus der Weg zum Board.
- [ ] Fällt die WebApi während des Imports aus, erscheint die bestehende **Ausfallmeldung** (`WebApiAufruf.MitAusfallmeldung`), keine Ausnahmeseite.

### Ein Vorgang, eine Meldung (`F0054`)

- [ ] Ein erfolgreicher Import meldet auf `GET /api/ereignisse` **genau ein** Ereignis mit Board, Urheber, Weg, Zeitpunkt und der **Zahl der Karten**.
- [ ] Es ist **ein** Ereignis, nicht eines je Karte. Rechenbeispiel: 41 angelegte Karten ergeben **1** Ereignis, 445 ebenfalls **1**.
- [ ] Das Ereignis trägt eine **eigene Ereignisart** neben `kartenereignis`; beide laufen über denselben Strom, und ein Abonnent unterscheidet sie **am Artnamen**, ohne den Rumpf zu lesen.
- [ ] Die bestehende `EreignisstromProbeTests`-Suite bleibt grün; `Kartenereignis` behält Gestalt und Artnamen.
- [ ] Jede offene Sicht **dieses** Boards lädt danach **einmal** neu; eine Sicht auf ein anderes Board lädt **nicht**.
- [ ] Es entsteht **keine Einflugmarke je Karte** — 41 Marken wären Rauschen.
- [ ] Ein **zurückgewiesener** Import meldet **nichts**.
- [ ] Ein Import mit `trocken=true` meldet **nichts**.
- [ ] Die Meldung entsteht im **Endpunkt**, nicht im Dienst — wie `B0375` es festgelegt hat.

### Was dieser Slice ausdrücklich nicht tut

- [ ] **Keine Wiedererkennung.** Läuft dieselbe Datei ein zweites Mal, entstehen die Karten **erneut**. Das ist `I0031`; Schritt 2 sagt es in einer Zeile, statt es zu verschweigen.
- [ ] **Kein Bericht mit einer Zeile je Knoten im Schirm** — das ist `I0032`. Der Bericht reist in der Antwort, der Schirm nennt die Zahl.
- [ ] **Keine Sollzeit an der Karte.** Die Spalte `Aufwand` kommt mit diesem Slice **nicht** ins System; die Lücke hat die Adresse `I0033`.
- [ ] **Kein Speichern der Importdatei**, weder als Anhang noch daneben.
- [ ] **Kein Anlegen eines Boards** und **keiner Kartenklasse**.
- [ ] **Kein Häkchen je Teilbaum** in der Vorschau — die Schnittebene beantwortet die Frage schon vor dem Schreiben (C24).
- [ ] **Keine Zeile „zuletzt eingefahren"** im Einstieg — sie bräuchte eine Tabelle `Boardimport`, und ein Archiv der Läufe hat keinen Knoten.
- [ ] **Kein Rückfluss ins Markdown** — die Vision führt ihn als offene Richtungsfrage.
- [ ] **Kein eigenes Herkunftsfeld an der Karte** — es bräuchte einen Knoten unter `D0004`.
- [ ] **Keine neue Migration, kein neues Paket, kein neues Schema.**

### Der grüne Bestand bleibt grün

- [ ] Die **Ablegefläche** aus `R00024` verhält sich am Anhang unverändert; Sperre und Text sind wiederverwendet, nicht kopiert.
- [ ] Die **Kartennummern** aus `R00023` bleiben unverändert; der Import nutzt denselben Weg zur nächsten Nummer.
- [ ] Die **Live-Suite** aus `R00031` und `R00032` bleibt grün: Kartenereignis, Einflugmarke, Warteregeln, Aufschließen und die Wiederaufnahme der Leitung verhalten sich unverändert.
- [ ] Die **Datumsgruppierung** aus `R00015` zeigt die importierten grünen Karten unter dem Tag des Laufs — nicht in einer Gruppe ohne Datum.
- [ ] Alle bestehenden E2E-Tests bleiben grün.

## Betroffene Verzeichnisstruktur

- **`Source/KanbanC.BL/Operations/Import/`** (neu) — der Leser und die Abbildung: Frontmatterleser, Zeilenzerleger, Knotenleser, Baumbildner, Kartenentwurfsbildner, Zielspaltenwahl, Kartenfelder, Herkunftsverweis, Kartenklassenprüfung. Alles pure Logik, alles ohne Board prüfbar.
- **`Source/KanbanC.BL/Integrations/Import/`** (neu) — `WbsImportService` als Integration, die liest, prüft, entwirft und (ohne `trocken`) schreiben lässt.
- **`Source/KanbanC.BL/Persistenz/Import/`** (neu) — `WbsImportRepository`, eine Transaktion über den ganzen Lauf. Muster: `KartenRepository` und `KartenklassenRepository`.
- **`Source/KanbanC.Contracts/Import/`** (neu) — `Schnittebene`, `Importanfrage`, `Importbericht`, `Importzeile`, `Kartenzahlen`. Immutable (C08), Wörter statt Zahlen im JSON.
- **`Source/KanbanC.Contracts/Ereignisse/`** — `Importereignis` tritt neben `Kartenereignis`.
- **`Source/KanbanC.WebApi/Endpunkte/`** — `WbsImportEndpunkte` (multipart, `DisableAntiforgery` wie beim Anhang); `EreignisEndpunkte` und `Ereignisdrehscheibe` tragen die zweite Art.
- **`Source/KanbanC.Blazor/Services/`** — `ImportApiKlient` (multipart wie `KartenApiKlient.HaengeAnhangAn`); `Ereignisleitung` und `Ereignisverteiler` melden die zweite Art weiter.
- **`Source/KanbanC.Blazor/Components/Pages/`** — `Import.razor(.css)` als neuer Schirm auf `/boards/{BoardId}/import`; `Board.razor` bekommt die Kachel im Layout-Modus und lädt auf das Importereignis einmal neu.
- **`Source/KanbanC.BL/Persistenz/Migrationen/`** — **unberührt.** Es entsteht kein Skript 019.
- **Tests** — `KanbanC.BL.Tests/Operations/Import/` und `Integrations/Import/` (der Löwenanteil), `KanbanC.WebApi.IntegrationTests/Api/` (Endpunkt, Transaktion, Ereignis), `KanbanC.Blazor.Tests/Services/` (Fehlerpfade des Klienten, zweite Ereignisart), `KanbanC.PlaywrightTests/` (ein E2E-Lauf plus Seitenobjekt).

## Technische Überlegungen

### Kein Markdown-Paket — gemessen, nicht vermutet

Der naheliegende Reflex bei „Markdown-Tabelle parsen" ist ein Paket. Die Messung an der echten Datei (`kanbanc.md`, Stand 2026-09-07) sagt etwas anderes:

| Was befürchtet wurde | Was gemessen wurde |
|---|---|
| maskierte Trennzeichen `\|` in Zellen | **0** von 540 Zeilen |
| ungerade Backtick-Zahl (offene Codespans) | **0** |
| Markdown-Klammerlinks in Zellen | **0** |
| abweichende Zellenzahl | **0** — alle 540 Zeilen liefern genau 14 Teile |

**Der gefürchtete Fallstrick ist heute nicht scharf.** Die Maskierungsregel ist damit eine Zeile Code und ein Test, kein Paket. Ein Markdown-Paket brächte einen vollständigen Dialekt-Parser für eine Tabelle mit zwölf Spalten und eine Abhängigkeit, die bei jedem Upgrade mitgeprüft werden müsste.

**Scharf ist stattdessen die Größe:** die längste Zeile misst **8.162 Zeichen**, die längste Zelle **7.990**. Das ist keine Parserfrage, sondern eine Durchreichfrage — durch multipart, durch JSON, durch SQLite, durch die Kartenanzeige. **Nachgesehen:** `KartenValidator` prüft den Titel (max. 1.000) und **nicht die Beschreibung**; eine Beschreibung von 8.000 Zeichen läuft also durch. Die längste Beschreibung, die aus `kanbanc.md` entstünde, misst **8.076 Zeichen** (`I0027`).

Deshalb bleibt `B0405` als `dependency-probe` **vorn**: die Fault-Injection (maskiertes Trennzeichen, zu wenige Zellen, 8.000-Zeichen-Notiz) ist noch nicht gelaufen, und was nicht gelaufen ist, ist eine Annahme und kein Messwert.

### Die Probedatei muss eine eingefrorene Kopie sein

`B0405` nennt Zahlen (505 Knotenzeilen, 404 Bubbles). Diese Zahlen stammen aus dem Lauf **vor** der Verfeinerung von `I0030` selbst; heute steht dieselbe Datei bei **540** Knotenzeilen und **435** Bubbles. Der Grund ist keine Schlamperei, sondern die Natur der Sache: **die Datei beschreibt das Projekt, das sie einliest.** Jedes `/planung verfeinern` ändert sie.

**Folge für die Umsetzung:** die eingebettete Testressource ist eine **Kopie mit Datum im Namen**, nicht ein Link auf die lebende Datei. Sonst rotten die Zusicherungen bei der nächsten Verfeinerung, und ein roter Test meldet dann nichts über den Leser.

### Die Längen des Bestands — nachgesehen statt vermutet

| Feld | Grenze im Bestand | größter Wert aus `kanbanc.md` | passt |
|---|---|---|---|
| Kartentitel | 1.000 (`KartenValidator`) | 78 (`[B0405] …`) | ja |
| Teilaufgabentext | 200 (`TeilaufgabenValidator`) | 78 | ja |
| Etikett | 100 (`EtikettenValidator`) | 46 | ja |
| Dateiverweispfad | 500 (`DateiverweisValidator`) | 45 | ja |
| Kartenklassenpräfix | 8 (`KartenklassenValidator`) | — | ja |
| Beschreibung | **keine Grenze** | 8.076 | ja, mit Preis |

**Die einzige Grenze, die eng werden kann, ist der Teilaufgabentext mit 200 Zeichen** — bei einem Bubble-Namen wäre er heute nicht ausgereizt, aber er ist die Stelle, an der eine fremde WBS anschlägt. Eine zu lange Teilaufgabe wird **übersprungen und gemeldet**, nicht gekürzt: eine gekürzte Teilaufgabe ist eine stille Falschaussage.

### Die drei benannten Preise

**Preis 1 — die Datei wird gelesen und nicht gespeichert.** Die Anhangablage hängt unter `<Name>.db-Files/<KarteId>/` an einer **Karte**; eine Importdatei gehört keiner. Eine Kopie wäre außerdem eine zweite Wahrheit über den Umfang neben der Datei im Repository. **Der Preis:** nach dem Lauf ist **nicht feststellbar, welche Fassung** diese Karten erzeugt hat. Zurück führt allein der Herkunftsverweis auf die lebende Datei — und die hat sich seither vielleicht bewegt.

**Preis 2 — gelbe Knoten wandern einmal von Hand.** Es gibt **zwei** Zielspalten und nicht drei: `gruen` → Abschlussspalte, alles andere → erste Spalte. `IstAbschlussspalte` ist die **einzige markierte Spalteneigenschaft** im Schema; eine dritte Zuordnung müsste auf das Wort „In Arbeit" treffen und bräche auf jedem anders benannten Board. **Der Preis:** ein Knoten mit Status `gelb` landet in der ersten Spalte und wird einmal von Hand gezogen. In `kanbanc.md` betrifft das bei Interaction-Schnitt **0 von 41** Karten, bei Dialog-Schnitt **1 von 9**.

**Preis 3 — die Aufwände kommen mit diesem Slice nicht ins System.** Die WBS führt je Bubble eine Spalte `Aufwand`; im Bestand gibt es **keine Sollzeit an der Karte** (`I0026` hat nachgesehen), und `I0033` setzt sie voraus. Ein Feld hier nähme jenem Slice die Entscheidung vorweg und wäre bis dahin tote Flexibilität (C24). **Der Preis:** `I0033` braucht erst ein Feld und **danach einen erneuten Import**. `D0008.dc.html`, Zustand 4 zeichnet die Zeile deshalb als gestrichelten Kasten.

### Die Kupplung ist der Dateiverweis — mit benannter Folge

Die Karte findet über `Dokumentation/Planung/kanbanc.md#I0001` zurück in die Datei. Das ist der **einzige Ort im Bestand, der eine Herkunft trägt**, und `I0019` nennt Planungsdateien wörtlich.

**Die Folge, ausdrücklich benannt:** `I0019` lässt den Menschen einen Dateiverweis auch **entfernen**. Wer ihn löscht, bekommt beim nächsten Lauf eine **zweite Karte** für denselben Knoten. Der Schaden trifft `I0031`, nicht diesen Slice — `I0030` legt ohnehin nur an. Geprüft und verworfen: die Wiedererkennung am Titelpräfix `[I0001]`; auch der Titel ist änderbar, und er führt nirgendwohin zurück. Ein eigenes Feld an der Karte wäre der saubere Träger, hat aber keinen Knoten unter `D0004`.

**Der Pfad ist ein Feld der Anfrage.** Ein Browser liefert bei einem Datei-Upload nur `kanbanc.md`; das Artboard zeichnet aber den Repository-Pfad. Fehlt `pfad`, gilt der Dateiname — dann ist der Verweis kürzer, aber nicht falsch.

### Der Anforderungsverweis entfällt — und warum

`B0416` sieht neben der Herkunft einen zweiten Dateiverweis auf die Anforderung vor. **Nachgesehen: er ist aus der Datei nicht ableitbar.** Die Spalte `Requirement` führt eine **ID** (`R00023`); die Datei heißt `Anforderungen/R00023-karte-einer-klasse-zuordnen.md`, und den kebab-case-Zusatz kennt nur das Dateisystem — der Import liest genau eine hochgeladene Datei und sieht kein Repository.

Ein Verweis `Anforderungen/R00023` ginge durch den `DateiverweisValidator` (er prüft Länge und Ränder, nicht Existenz) und stünde als **toter Pfad an 31 von 41 Karten**. Das wäre eine unehrliche Schnittstelle (C25).

**Entschieden:** die Anforderungsnummer steht als Zeile in der **Beschreibung** („Anforderung: R00023"); der Import legt **einen** Dateiverweis je Karte an. Die Umkehrung — ein zweites Anfragefeld `anforderungsordner` — ist tote Flexibilität, solange sie niemand bestellt; sie hat mit `I0031` eine Adresse.

### Der Urheber ist Pflicht, nicht Zierde

`Kartendateiverweis` (015) trägt `Kontributor INTEGER NOT NULL`. **Ohne Urheber entsteht kein Dateiverweis und damit keine vollständige Karte.** Der Urheber reist deshalb im **Rumpf** der multipart-Anfrage (`kontributor`), genau wie bei `HaengeAnhangAn` — und nicht als Query. Für den Menschen fällt das nicht auf: die Ablegefläche aus `R00024` ist ohne gewählte Identität ohnehin gesperrt und sagt warum. Für einen Agenten ist `kontributor` ein Pflichtfeld mit lesbarer Zurückweisung.

### Zwei Ereignisarten statt 41 Kartenereignissen

Die `Ereignisdrehscheibe` hält je Abonnent **64 Plätze mit `DropOldest`** (nachgelesen, `Ereignisdrehscheibe.PlatzJeAbonnent`). Ein Bubble-Schnitt mit 445 Karten verlöre bei Kartenereignissen je Karte **stillschweigend** Ereignisse — und ein `Kartenereignis` mit Platzhalterwerten für einen Import wäre eine unehrliche Schnittstelle (C25).

Deshalb: **ein Lauf meldet einen Vorgang, die offene Sicht lädt einmal neu.** Das löst zugleich die in `B0375` wörtlich festgehaltene Schuld ein: „entsteht ein Weg an der WebApi vorbei — etwa der WBS-Import `I0030` —, muss er die Meldung selbst tragen."

**Der offene Punkt der Technik:** `TypedResults.ServerSentEvents` trägt heute genau **eine** Art (`EreignisEndpunkte.Ereignisart = "kartenereignis"`, ein einziger Typparameter). Ob zwei Arten auf demselben Strom über diesen Rahmen gehen oder ob der Strom auf einen gemeinsamen Umschlag umgestellt werden muss, entscheidet `B0433` — mit einer Erweiterung von `EreignisstromProbeTests`, nicht mit einer Vermutung.

### Die Schnittebene und die Vollständigkeit

Die Schnittebene ist eine **Untergrenze, keine Auswahl**: ein Knoten oberhalb, der keinen Nachfahren auf der Schnittebene hat, wird selbst zur Karte. Sonst verschwände sein Teilbaum still — und still verschwinden ist in diesem Projekt die eine Sache, die nirgends erlaubt ist.

**Bei der Vorgabe kostet die Regel nichts:** auf `kanbanc.md` ergibt Interaction-Schnitt mit und ohne sie dieselben **41** Karten. Sichtbar wird sie erst bei Feature-Schnitt (79 statt 54) und Bubble-Schnitt (445 statt 435).

### Eine Application ohne Dialog-Knoten

Eine Application mit nur einer Interaction hat nach `/planung anlegen` **keinen Dialog-Knoten**. Dann gibt es oberhalb der Schnittebene nichts außer der Application — und die ist das Board. **Entschieden: die Karte entsteht ohne Etikett.** Kein Ersatzetikett aus dem Application-Namen: das Board trägt den Namen bereits, und ein Etikett, das auf jeder Karte gleich lautet, ist keine Auskunft.

### Ablauf

1. **Einstieg**
   - 1.1 Layout-Modus des Boards → Kachel „WBS-Import" → `/boards/{BoardId}/import`
   - 1.2 Ohne Kartenklasse: statt des Knopfes der Weg zur Klassenpflege
2. **Schritt 1 — Datei wählen**
   - 2.1 Ablegefläche aus `R00024` (`InputFile`, `Ablegeflaechenstand`), beide Sperrgründe
   - 2.2 Zielklasse wählen, `pfad` vorbelegen, den gleichwertigen API-Aufruf daneben zeigen
3. **Schritt 2 — Vorschau** (`trocken=true`)
   - 3.1 `POST /api/boards/{boardId}/wbs-import` multipart
   - 3.2 `Wbsleser.Lies` → `Wbsbaum` + Übersprungene
   - 3.3 `Kartenentwuerfe` je Schnittebene rechnen → Kartenzahl je Wahl
   - 3.4 Baum links lesend, Wirkung rechts, Regler mit der Zahl
   - 3.5 **Ende hier** — es wurde nichts geschrieben
4. **Schritt 3 — schreiben** (`trocken=false`)
   - 4.1 Kartenklasse prüfen (gehört sie dem Board?) → sonst `400` mit Kompensation
   - 4.2 **eine Transaktion**: je Knoten Karte + Kartenklassenzuordnung (Zählerstand +1) + Etiketten + Teilaufgaben + Dateiverweis
   - 4.3 Karte in der Abschlussspalte → `ErledigtAm` = Tag des Laufs
   - 4.4 Commit; bei Fehler Rollback — **alles oder nichts**
   - 4.5 Endpunkt meldet **ein** `Importereignis` (Weg aus dem `Wegkopf`)
   - 4.6 Schirm: eine Zeile („41 Karten angelegt") und der Weg zum Board
5. **Die offene Sicht**
   - 5.1 `Ereignisleitung` liest das `Importereignis` → `Ereignisverteiler`
   - 5.2 `Board.razor` des betroffenen Boards lädt **einmal** neu; keine Einflugmarke je Karte

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** die neue Route `POST /api/boards/{boardId}/wbs-import`; der neue Schirm `/boards/{BoardId}/import`; die Kachel im Layout-Modus von `Board.razor`; `Ereignisdrehscheibe` und `EreignisEndpunkte` für die zweite Art. **Keine Migration, kein neues Paket, kein neues Startprojekt.**

**In `KanbanC.Contracts/Import` (neu):**
- `Schnittebene` (Enum mit `JsonStringEnumConverter`, Muster `Ereignisweg`) — `Dialog`, `Interaction`, `Feature`, `Bubble`; Vorgabe `Interaction`.
- `Importbericht` (DTO, immutable) — `Angelegt`, `Geaendert`, `Unveraendert`, `Uebersprungen`, `Kartenzahlen`, `Zeilen`.
- `Importzeile` (DTO, immutable) — Knoten-ID, Ebene, was daraus wurde, Grund bei Übersprungenem.
- `Kartenzahlen` (DTO, immutable) — die Zahl je Schnittebene, damit der Regler sie ohne zweiten Aufruf hat.

**In `KanbanC.Contracts/Ereignisse`:**
- `Importereignis` (DTO, immutable) — `Board`, `Urheber`, `Kartenzahl`, `Weg`, `Zeitpunkt`. Signal statt Nutzlast, wie `Kartenereignis`.

**In `KanbanC.BL/Operations/Import` (neu, alles pure Logik):**
- `Frontmatterleser` — `application`, `sprache`, `zuletzt`; ohne Block oder ohne `application:` kein Befund, sondern eine Zurückweisung.
- `Zeilenzerleger` — eine Tabellenzeile → zwölf Zellen; `\|` bleibt Zeichen.
- `Knotenleser` — zwölf Zellen → `Wbsknoten` oder `Uebersprungen` mit Grund.
- `Wbsbaumbildner` — `Wbsknoten`-Liste → `Wbsbaum` in Dateireihenfolge; unbekannter Eltern → übersprungen.
- `Kartenentwurfsbildner` — `Wbsbaum` + `Schnittebene` → `Kartenentwuerfe`; trägt die Regel Baum → Board.
- `Zielspaltenwahl` — Status + Spalten → Abschlussspalte oder erste Spalte; `verworfen`/`option`/`ausbau` übersprungen.
- `Kartenfelder` — Titel `[ID] Name`, Beschreibung aus Fertig-Kriterium, Notiz und `Braucht`.
- `Herkunftsverweis` — `pfad` + ID → `…/kanbanc.md#I0001`; ohne `pfad` der Dateiname.
- `Kartenklassenpruefung` — gehört die Klasse dem Board? sonst `Fehlerbefund` mit Werten und Kompensation.

**In `KanbanC.BL/Integrations/Import` (neu):**
- `WbsImportService` (Integration, fängt/loggt)
  - `Ergebnis<Importbericht> Importiere(long boardId, Importanfrage anfrage, Stream datei)`

**In `KanbanC.BL/Persistenz/Import` (neu):**
- `WbsImportRepository` (Provider, wirft) — schreibt Karten, Zuordnungen, Etiketten, Teilaufgaben und Dateiverweise in **einer** Transaktion.
  - `IReadOnlyList<Karte> Schreibe(long boardId, Kartenentwuerfe entwuerfe, long kartenklasseId, long kontributorId)`

**In `KanbanC.WebApi`:**
- `WbsImportEndpunkte` (Integration) — multipart, `DisableAntiforgery`, `200` / `201` / `400` / `404`; meldet das `Importereignis`.
- `Ereignisdrehscheibe` und `EreignisEndpunkte` — zweite Art auf demselben Strom.

**In `KanbanC.Blazor`:**
- `ImportApiKlient` (Integration) — multipart wie `KartenApiKlient.HaengeAnhangAn`; `ApiErgebnis<Importbericht>`.
- `Import.razor` (Seite, drei Schritte) · `Board.razor` (Kachel, Nachladen auf das Importereignis).

**Kein Interface** für die neuen Bauteile: je Aufgabe genau eine Implementation (C25).

### Änderungen an bestehenden Klassen

| Klasse | Änderung |
|---|---|
| `WebApi/Ereignisdrehscheibe` | trägt zwei Ereignisarten statt einer |
| `WebApi/Endpunkte/EreignisEndpunkte` | meldet beide Arten mit je eigenem Artnamen |
| `Blazor/Services/Ereignisleitung` | liest beide Arten vom Strom |
| `Blazor/Services/Ereignisverteiler` | meldet das Importereignis an seine Hörer weiter |
| `Blazor/Components/Pages/Board.razor` | Kachel „WBS-Import" im Layout-Modus; lädt auf das Importereignis einmal neu |
| `Blazor/Program.cs` | registriert `ImportApiKlient` |
| `WebApi/Program.cs` | registriert `WbsImportService` und die Route |
| `PlaywrightTests/PageObjects/BoardSeite` | Locator der Kachel |

**Nicht geändert:** `Migrationen/` in Gänze, `KartenRepository`, `KartenklassenRepository`, `Ablegeflaechenstand`, `Karte.razor`, `Kartendetail.razor`.

## Tests

Nach Skill `test-pyramide`, jeder Test nach Skill `test-ehrlichkeit`.

**Kandidaten für Unit Tests (pure Logik nach IOSP, `KanbanC.BL.Tests`):** der Löwenanteil dieses Slice ist prüfbar, **ohne dass ein Board existiert** — dieselbe Lage wie `F0044` bei `I0028`.
- `Frontmatterleser` — mit Block, ohne Block, ohne `application:`.
- `Zeilenzerleger` — zwölf Zellen; `\|` bleibt Zeichen; zu wenige und zu viele Zellen werden gemeldet.
- `Knotenleser` — unbekannte Ebene, unbekannter Status, leerer Name; `bestehend` gilt wie `gruen`.
- `Wbsbaumbildner` — Dateireihenfolge bleibt; unbekannter Eltern wird übersprungen.
- `Kartenentwurfsbildner` — die vier Schnittebenen; Vorfahren werden Etiketten; Nachfahren werden flache Teilaufgaben; ein Knoten ohne Nachfahren auf der Schnittebene wird selbst Karte.
- `Zielspaltenwahl` — `gruen` → Abschlussspalte, `rot`/`gelb` → erste Spalte, `verworfen`/`option`/`ausbau` → übersprungen mit Grund.
- `Kartenfelder` — Titel `[I0001] Board anlegen`; Aufwand und Fluss kommen **nicht** vor.
- `Herkunftsverweis` — mit `pfad`, ohne `pfad`.
- `Kartenklassenpruefung` — fremde Klasse, keine Klasse; Befund trägt Werte und Kompensation.
- **`B0405` als `dependency-probe`** — die eingefrorene Kopie der echten Datei durch den ganzen Leser, plus Fault-Injection: maskiertes Trennzeichen, zu wenige Zellen, 8.000-Zeichen-Notiz.

**Integration (`KanbanC.WebApi.IntegrationTests`, echte SQLite-Datei):**
- `trocken=true` schreibt nichts — Kartenzahl vor und nach dem Aufruf gleich.
- `trocken` fehlt → es wird nichts geschrieben.
- der Schreiblauf legt Karten, Nummern, Etiketten, Teilaufgaben und Dateiverweise an; die Bilanz stimmt.
- **die Transaktion hält**: ein erzwungener Fehler mittendrin lässt **keine** Karte zurück.
- `UNIQUE(Kartenklasse, Zaehlerstand)` bleibt über 41 Vergaben unverletzt.
- Board ohne Kartenklasse, fremde Klasse, unbekanntes Board, fehlender `kontributor` — je eine lesbare Zurückweisung mit Kompensation.
- `ErledigtAm` steht an Karten der Abschlussspalte und ist an den übrigen `null`.
- **ein** Importereignis je Lauf; keines bei `trocken` und keines bei Zurückweisung.
- `EreignisstromProbeTests` erweitert: **zwei Arten auf einem Strom** — das ist der offene Punkt aus `B0433` und wird gemessen, nicht vermutet.

**`KanbanC.Blazor.Tests`:** `ImportApiKlient` — 400 und 404 werden zu `ApiErgebnis` mit Befunden; ein Ausfall der WebApi wird zur Ausfallmeldung. Diese Pfade sind über den Browser nicht auslösbar — genau der Grund, aus dem dieses Projekt existiert. Dazu: der `Ereignisverteiler` meldet beide Arten, ohne dass sie einander stören.

**E2E (`KanbanC.PlaywrightTests`, beide Prozesse auf freien Ports nach Skill `freier-port`):** **ein** Lauf — Datei ablegen, Vorschau sehen, Schnittebene wechseln und die Zahl sich ändern sehen, schreiben, Karten auf dem Board. **Die Testdatei ist eine kleine WBS**, nicht `kanbanc.md`; die große läuft in `B0405`.

Repositories, DAL-Klassen und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten — `WbsImportRepository` wird über die Integrationsebene geprüft.

## Abhängigkeiten

- Abhängig von: **`R00023`** (Karte einer Klasse zuordnen — `I0021`, **grün**). Das ist der eine Knoten der WBS-Spalte `Braucht`; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf (alle grün, die Spalte `Braucht` führt Vorbedingungen, keine Bauplätze): **`R00001`/`R00002`** (Board und Spalten samt `IstAbschlussspalte`), **`R00006`** (Karten), **`R00015`** (Datumsgruppierung der Abschlussspalte), **`R00017`** (Etiketten, `I0015`), **`R00018`** (Teilaufgaben, `I0016`), **`R00021`** (Dateiverweise, `I0019`), **`R00022`** (Kartenklassen, `I0020`), **`R00024`** (Ablegefläche mit Sperre), **`R00031`** (Live-Kanal und Drehscheibe), **`R00005`** (`gestaltung.css`).
- Blockiert: **`I0031`** („Import wiederholen") und **`I0032`** („Import-Ergebnis sehen") führen `I0030` in ihrer Spalte `Braucht`; **`I0033`** („Soll-Ist-Vergleich abrufen") ebenfalls.
- **`D0008` wird mit diesem Slice nicht grün** — `I0031` und `I0032` bleiben offen.

## Umfang

```
WBS-Datei importieren (I0030) = 31 Bubbles: 28 Standard (32,0h), 3 unklar (6,0–12,0h).
Rest: 32,0h klar + 6,0–12,0h unklar · 0 von 31 Werten belegt, alle Richtwerte (ungemessen).

Fortschritt: 0 von 31 Bubbles gruen (0 %) · 0 laufen · 31 offen
```

`I0030` ist vollständig bis zur Bubble geplant und trägt seine Bubbles in **vier Features**:

| Feature | Bubbles | Standard | unklar | Braucht |
|---|---|---|---|---|
| `F0051` Die WBS-Datei wird gelesen | `B0405`–`B0411` (7) | 6 (7,2h) | 1 (2,0–4,0h) | `I0021` |
| `F0052` Zeigen vor Schreiben | `B0412`–`B0420` (9) | 9 (8,4h) | 0 | `F0051` |
| `F0053` Die Knoten werden Karten | `B0421`–`B0431` (11) | 10 (13,6h) | 1 (2,0–4,0h) | `F0052` |
| `F0054` Ein Vorgang, eine Meldung | `B0432`–`B0435` (4) | 3 (2,8h) | 1 (2,0–4,0h) | `F0053` |

**Warum vier Features:** weil vier Aspekte **getrennt fertig** werden. `F0051` hat kein Board und ist ohne eines vollständig prüfbar — dieselbe Lage wie `F0044` bei `I0028`. `F0052` ist ohne jede Schreibwirkung prüfbar. `F0053` schreibt. `F0054` ist die Meldung, die auch dann noch fehlen könnte, wenn alles andere steht. Anders als bei `I0016` bis `I0027` teilen sie weder Datenquelle noch Komponente noch Prüfweg.

**Mit 31 Bubbles ist das der größte Slice des Projekts** — vor `I0015` (30), `I0028` (20) und `I0029` (15). **Nur drei der einunddreißig tragen eine Bandbreite**, und das ist kein Zufall: der Löwenanteil ist pure Logik ohne Infrastruktur, und die Infrastruktur, die gebraucht wird (multipart, Transaktion, Ereignisstrom, Ablegefläche), ist im Bestand **vorgemacht**. Ungeklärt sind genau drei Stellen: die Probe am echten Dateiformat (`B0405`), der E2E-Lauf über drei Schritte (`B0431`) und die Frage, ob zwei Ereignisarten über `TypedResults.ServerSentEvents` gehen (`B0433`).

Derselbe Vermerk wie bei allen Slices seit `I0005`: die 2h-Richtwerte für UI- und Endpunkt-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. **Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen** — die Bubbles sind Vorplanung, keine Vereinbarung.

**Die Requirement-Klammer sitzt an `I0030` und an allen vier Features** — dieselbe Form wie bei `R00017`/`I0015`, `R00031`/`I0028` und `R00032`/`I0029`: die Features sind die Blätter der Steuerungsebene und damit die Slices, aber sie gehören zu **einem** Fertig-Kriterium und werden gemeinsam vereinbart.

## Offene Fragen

- **Wo wohnt der Parser?** — **entschieden: in `KanbanC.BL/Operations/Import/`.** Die Kernregel verlangt, dass ein Agent dieselbe Datei an dieselbe Route schickt; ein Parser in der Oberfläche wäre ein Weg, den die API nicht hätte. **Nicht am Menschen geprüft.**
- **Braucht der Leser ein Markdown-Paket?** — **entschieden: nein.** Gemessen an der echten Datei: 540 von 540 Zeilen liefern genau 14 Teile, null maskierte Trennzeichen, null ungerade Backtick-Zahlen, null Klammerlinks. Die Maskierungsregel ist eine Zeile Code und ein Test. **Scharf ist die Größe** (8.162 Zeichen je Zeile, 7.990 je Zelle), und die ist keine Parserfrage. `B0405` probt beides, bevor gebaut wird. **Nicht am Menschen geprüft.**
- **Wird die Importdatei gespeichert?** — **entschieden: nein.** Die Anhangablage hängt an einer Karte, eine Importdatei gehört keiner, und eine Kopie wäre eine zweite Wahrheit über den Umfang. **Preis: welche Fassung diese Karten erzeugt hat, ist danach nicht feststellbar.** Die Umkehrung wäre ein Anhang an einer Karte, die es nicht gibt. **Nicht am Menschen geprüft.**
- **Legt der Import eine Kartenklasse an, wenn keine da ist?** — **entschieden: nein.** `I0020` legt Klassen an; ein zweiter Weg müsste Name und Präfix raten. Board ohne Klasse → Zurückweisung mit Grund **und** Kompensationsaktion. **Nicht am Menschen geprüft.**
- **Zwei Zielspalten oder drei?** — **entschieden: zwei.** `IstAbschlussspalte` ist die einzige markierte Spalteneigenschaft; ein Treffer auf das Wort „In Arbeit" bräche auf anders benannten Boards. **Preis: gelbe Knoten wandern einmal von Hand** (auf `kanbanc.md` bei Interaction-Schnitt: 0 von 41). **Nicht am Menschen geprüft.**
- **Was ist die Vorgabe von `trocken`?** — **entschieden: `true`.** Fehlt das Feld, wird nichts geschrieben. Das ist die einzige Vorgabe, die einen Fehler billig macht: eine Datei mit 540 Zeilen erzeugt sonst unbesehen 445 Karten. Die Umkehrung (`false`) wäre bequemer für den Schirm, der das Feld ohnehin setzt. **Nicht am Menschen geprüft.**
- **Wie zählt `bestehend`?** — **entschieden: wie `gruen`** (Abschlussspalte, Haken). Beleg: der Skill `work-breakdown-structure` rechnet „alle zählenden Kinder `gruen` (oder `bestehend`) → `gruen`". **Befund:** `B0415` nennt diesen Status nicht, und ohne die Regel landete `B0380` als einzige Zeile der echten Datei in der ersten Spalte. **Nicht am Menschen geprüft.**
- **Was geschieht mit einem Teilbaum, der die Schnittebene nicht erreicht?** — **entschieden: der oberste Knoten ohne Nachfahren auf der Schnittebene wird selbst zur Karte.** Sonst verschwände er still. Bei der Vorgabe kostet die Regel nichts (41 = 41); sichtbar wird sie bei Feature-Schnitt (79 statt 54). **Nicht am Menschen geprüft.**
- **Bekommt die Karte einen Dateiverweis auf ihre Anforderung?** — **entschieden: nein, die Nummer steht in der Beschreibung.** Die Spalte führt eine ID, die Datei trägt einen kebab-case-Zusatz, den nur das Dateisystem kennt; ein erfundener Pfad stünde als toter Verweis an 31 von 41 Karten. **Weicht von `B0416` ab** — eine Bubble ist Entwurf, kein Vertrag. Die Umkehrung wäre ein zweites Anfragefeld `anforderungsordner`. **Nicht am Menschen geprüft.**
- **Was wird das Etikett, wenn es keinen Dialog-Knoten gibt?** — **entschieden: kein Etikett.** Der Application-Name steht schon am Board; ein Etikett, das auf jeder Karte gleich lautet, ist keine Auskunft. **Nicht am Menschen geprüft.**
- **Sagt der Schirm, dass ein zweiter Lauf erneut anlegt?** — **entschieden: ja, in einer Zeile in Schritt 2.** `I0030` erkennt nichts wieder; das zu verschweigen wäre dieselbe Sorte stiller Schaden, die `R00024` behoben hat. **Nicht am Menschen geprüft.**
- **Trägt `TypedResults.ServerSentEvents` zwei Ereignisarten?** — **offen** (`B0433`). Heute steht dort genau ein Typparameter und ein fester Artname. `EreignisstromProbeTests` wird erweitert, statt zu vermuten. Geht es nicht, ist ein gemeinsamer Umschlag mit Artfeld der Ausweg — und dann ändert sich die Gestalt des bestehenden `Kartenereignis` auf der Leitung.
- **Was geschieht mit einer Teilaufgabe über 200 Zeichen?** — **entschieden: übersprungen und gemeldet**, nicht gekürzt. In `kanbanc.md` tritt der Fall nicht auf (längster Wert 78), eine fremde WBS kann ihn auslösen. **Nicht am Menschen geprüft.**
- **Wie kommt die Bilanz je Schnittebene zustande, ohne viermal zu lesen?** — **offen** (`B0413`/`B0417`). Der Baum wird einmal gelesen und viermal ausgewertet, oder die Zahlen fallen beim Durchlauf nebenbei an. Eine Frage der Umsetzung, keine der Fachlichkeit.
- **Wie prüft der E2E-Lauf drei Schritte auf einem Schirm ohne feste Pausen?** — **offen** (`B0431`). Gewartet wird auf Zustände, nie auf Zeit; welche Zustände das sind, entscheidet der Bau.

## Manuelle Vorbereitungstätigkeiten

- **Eine Kartenklasse auf dem Zielboard anlegen** (`I0020`, Layout-Modus). Ohne sie weist der Import zurück — mit dem Weg dorthin. Für dieses Projekt ist der naheliegende Name `WBS` mit Präfix `WBS-`.
- **Eine eingefrorene Kopie von `kanbanc.md`** als Testressource ablegen, mit Datum im Namen. Ein Link auf die lebende Datei rottet mit der nächsten Verfeinerung.

## Manuelle Nachbereitungstätigkeiten

- **Gelbe Knoten einmal von Hand ziehen** — sie landen in der ersten Spalte (Preis 2). In `kanbanc.md` betrifft das bei Interaction-Schnitt niemanden.
- **Kein zweiter Lauf derselben Datei**, solange `I0031` offen ist: er legt die Karten erneut an.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Trennung, die dieses Projekt selbst gezogen hat: die WBS ist die Fortschrittswahrheit, aber sie liegt in einer Datei, die niemand bedient — 540 Zeilen Markdown neben einem Board, das leer ist. Jede Auswertung, die die Vision verspricht (Soll-Ist, Puffer, Burndown), setzt voraus, dass die geplanten Einheiten überhaupt als Karten existieren; `I0033` führt `I0030` deshalb wörtlich in seiner Spalte `Braucht`. Die Kausalkette: **wenn** eine WBS-Datei über eine Route eingelesen wird, die Mensch und Agent gleichermaßen benutzen, und **wenn** die entstehenden Karten ihre Herkunft als Dateiverweis mitführen (X), **dann** steht der Plan dort, wo gearbeitet wird, ohne die Datei zu entwerten oder zu duplizieren (Y), **und dann** kann alles, was auf Karten rechnet — Zeiterfassung, Bündelung, Auswertung —, erstmals über den geplanten Umfang rechnen (Z). **Der Hebel liegt beim Einlesen und nicht beim Nachziehen:** `I0031` (Wiederholen) wäre die nachgelagerte Änderung, und sie ist ohne einen ersten Lauf gegenstandslos — man kann nichts abgleichen, was nie angelegt wurde. **Und er liegt nicht bei einem Rückfluss ins Markdown**, der die vorgelagerte Änderung wäre: der machte aus zwei Wahrheiten sofort zwei schreibende Wahrheiten, und die Vision führt ihn ausdrücklich als offene Richtungsfrage. **Die Vorschau ist dabei kein Komfort, sondern der Grund, warum der Hebel überhaupt sicher zu bedienen ist:** dieselbe Datei ergibt je nach Schnittebene 9 oder 445 Karten, und eine Zahl, die man erst nach dem Schreiben sieht, ist keine Auskunft mehr, sondern ein Befund.

## Missing-Docs

- **`TypedResults.ServerSentEvents` mit mehreren Ereignisarten:** ob der Rahmen je Strom genau eine Art trägt oder ob mehrere Typen auf einer Leitung möglich sind, steht in keiner Projektdokumentation und entscheidet `B0433`.
- **Grenzen von multipart in Minimal-APIs:** ab welcher Dateigröße `IFormFile` gepuffert oder auf Platte geschrieben wird und welche Vorgabe für die maximale Formulargröße gilt, ist im Repository nirgends belegt — bei einer 500-KB-WBS ist das eine Randfrage, bei einer 50-MB-Datei nicht.
- **Transaktionsgröße in SQLite:** 684 Schreibvorgänge in einer Transaktion sind im Projekt bisher nicht vorgekommen (bislang höchstens ein Board mit drei Spalten). Ob und ab wann die Journalgröße oder die Sperrdauer eine Rolle spielt, ist unbelegt.
- **Die Statuswerte der WBS als Vertrag:** `rot`, `gelb`, `gruen`, `bestehend`, `option`, `ausbau`, `verworfen` stehen im Skill `work-breakdown-structure`, nicht im Repository. Ein Leser, der sie hart kennt, hängt an einer Datei außerhalb des Projekts.
- **Dapper und `DateTimeOffset` aus SQLite:** dass Dapper diesen Typ nicht selbst materialisiert, ist im Projekt bekannt und in den bestehenden Lesern umgangen, aber nirgends als Regel festgehalten — der neue Leser wiederholt sie sonst als Zufall.

## Notizen

### Verworfene Alternativen

- **Ein Markdown-Paket für die Tabelle** — gemessen unnötig: 540 von 540 Zeilen zerfallen sauber, es gibt kein maskiertes Trennzeichen. Ein Dialekt-Parser für zwölf Spalten wäre eine Abhängigkeit ohne Gegenwert.
- **Der Parser in `KanbanC.Blazor`** — ein Weg, den die API nicht hätte; hebt die Kernregel des Projekts auf.
- **Die Importdatei als Anhang speichern** — sie gehört keiner Karte, und eine Kopie wäre eine zweite Wahrheit über den Umfang.
- **Das Zielboard aus der Application anlegen** — der Import bekäme eine Fähigkeit, die `I0001` schon hat, und ein Tippfehler im Frontmatter erzeugte ein Board.
- **Die Kartenklasse beim Import anlegen** — Name und Präfix wären geraten; `I0020` ist die Stelle, und die Zurückweisung führt dorthin.
- **Eine dritte Zielspalte für `gelb`** — sie müsste auf das Wort „In Arbeit" treffen; `IstAbschlussspalte` ist die einzige markierte Spalteneigenschaft im Schema.
- **`trocken=false` als Vorgabe** — ein vergessenes Feld erzeugte hunderte Karten; die teure Richtung gehört nie in die Vorgabe.
- **Bubbles als Karten (der ältere Wireframe-Satz)** — 445 Karten aus einer Datei; die Interaction ist die Einheit, auf der `/implementierung im-pair` arbeitet und die `/github` als **ein** Issue projiziert.
- **Die Schnittebene ohne Vorschau** — die Zahl gehört ins Bild, bevor jemand sie erzeugt.
- **Ein Häkchen je Teilbaum in der Vorschau** — die Schnittebene beantwortet die Frage, vor der es schützen soll, schon vorher (C24).
- **Wiedererkennung am Titelpräfix `[I0001]`** — der Titel ist änderbar und führt nirgendwohin zurück; der Dateiverweis führt zurück.
- **Ein eigenes Herkunftsfeld an der Karte** — der sauberere Träger, aber ohne Knoten unter `D0004`; ein Feld zu erfinden nähme diesem Slice die Ehrlichkeit über seine Kupplung.
- **Ein Dateiverweis auf die Anforderungsdatei** — der Pfad ist aus der WBS nicht ableitbar; er stünde tot an 31 von 41 Karten.
- **Ein Feld für die Spalte `Aufwand`** — nähme `I0033` die Entscheidung vorweg und wäre bis dahin tote Flexibilität (C24).
- **Ein Kartenereignis je angelegter Karte** — der Kanal hält 64 Plätze mit `DropOldest`; 445 Ereignisse gingen still verloren, und ein `Kartenereignis` mit Platzhalterwerten wäre eine unehrliche Schnittstelle (C25).
- **Eine Einflugmarke je importierter Karte** — einundvierzig Marken sind Rauschen, keine Auskunft.
- **Die Meldung im Dienst statt im Endpunkt** — `B0375` hat den Ort festgelegt; die Fachlogik weiß nicht, dass jemand zuhört.
- **Eine Zeile je Knoten im Schirm nach dem Lauf** — das ist `I0032`; hier sagt der Schirm die Zahl und führt zum Board.
- **Die Zeile „zuletzt eingefahren" im Einstieg** — sie bräuchte eine Tabelle `Boardimport`; ein `ALTER TABLE` ist mit dem journallosen Läufer ohnehin nicht idempotent.
- **Eine zu lange Teilaufgabe kürzen** — eine gekürzte Teilaufgabe ist eine stille Falschaussage; sie wird übersprungen und gemeldet.
- **Die lebende `kanbanc.md` als Testressource verlinken** — die Zusicherungen rotteten bei der nächsten Verfeinerung.

### Bewusst out of scope

- **`I0031` Import wiederholen** — Wiedererkennung am Herkunftsverweis, die Fächer `geaendert`, `unveraendert` und „nicht mehr in der Datei", und die Trennung „was die Datei nachzieht / was das Board behält". Das Fach `ZuLoeschen` bleibt dort **leer**: verwaiste Karten werden gemeldet, nicht gelöscht.
- **`I0032` Import-Ergebnis sehen** — der Bericht mit einer Zeile je Knoten, der Kartennummer daneben und dem Grund jeder übersprungenen Zeile.
- **`I0033` Soll-Ist-Vergleich** — braucht erst ein Feld für die Sollzeit und danach einen erneuten Import.
- **Ein Rückfluss ins Markdown** — die Vision führt ihn als offene Richtungsfrage.
- **Ein Archiv der Läufe** — kein Knoten, keine Tabelle.
- **Ein Klassenfilter auf dem Board** — eigener Slice, sobald ihn jemand bestellt (`I0022` hat das schon so entschieden).

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede steht oben unter „Offene Fragen" mit ihrer Umkehrung.

1. **Der Parser wohnt in `KanbanC.BL/Operations/Import/`** — die Kernregel, nicht der bequemste Ort.
2. **Kein Markdown-Paket**; die Maskierung ist eine Regel plus Test. Gemessen an 540 Zeilen.
3. **Die Datei wird gelesen und nicht gespeichert** — Preis 1 benannt.
4. **Die Kartenklasse muss vorher existieren**; der Import legt nie eine an.
5. **Zwei Zielspalten, nicht drei** — Preis 2 benannt.
6. **`trocken=true` ist die Vorgabe.**
7. **`bestehend` zählt wie `gruen`** — Beleg aus dem Skill; `B0415` nennt den Status nicht.
8. **Ein Knoten ohne Nachfahren auf der Schnittebene wird selbst zur Karte** — kein Teilbaum verschwindet still.
9. **Kein Dateiverweis auf die Anforderung**; die Nummer steht in der Beschreibung. Abweichung von `B0416`, begründet.
10. **Kein Etikett, wenn kein Dialog-Knoten existiert.**
11. **Der Schirm sagt in Schritt 2, dass ein zweiter Lauf erneut anlegt.**
12. **Eine zu lange Teilaufgabe wird übersprungen und gemeldet**, nicht gekürzt.
13. **Die Aufwände kommen mit diesem Slice nicht ins System** — Preis 3, Adresse `I0033`.
14. **Der Urheber (`kontributor`) ist ein Pflichtfeld der Anfrage** — `Kartendateiverweis` (015) lässt nichts anderes zu.
15. **Eine zweite Ereignisart statt 41 Kartenereignissen**; ein Lauf, eine Meldung, ein Neuladen.
