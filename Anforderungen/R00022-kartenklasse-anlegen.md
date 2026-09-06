---
id: R00022
status: Neu
datum: 2026-09-06
---

# R00022: Kartenklasse anlegen

## Beschreibung

Ein Board führt **Kartenklassen**: benannte Nummernkreise, aus denen Karten später ihre eigene, klassenspezifische Nummer bekommen — `WBS-32`, `BUG-08`, `BES-05`. Eine Kartenklasse entsteht mit genau zwei Angaben, **Name** und **Nummernkreis-Präfix**, und trägt von Anfang an einen **Zählerstand**, der bei 0 beginnt. Angelegt wird über `POST /api/boards/{boardId}/kartenklassen`, abgerufen über `GET /api/boards/{boardId}/kartenklassen`; in der Oberfläche steht der Bereich unter der Überschrift **„Klassen"** im **Layout-Modus** des Boards, unter der Zeile für die neue Spalte.

Zahlt ein auf: [Vision](R00000-vision.md) — „Karten mit der Ausdruckskraft von Kanbanflow — und darüber hinaus einer optionalen Klasse. Eine Klasse fasst zusammengehörige Karten (etwa alle aus der WBS) und vergibt eine eigene, klassenspezifische Nummerierung, so dass ein Agent über die API gezielt das richtige Set greift statt des ganzen Boards."

**Was dieser Slice noch nicht tut:** er vergibt keine Nummer. Das Zuordnen einer Karte zu einer Klasse ist `I0021`, das gezielte Abrufen ihrer Karten `I0022`. Hier entsteht der Nummernkreis, aus dem beide später schöpfen — samt der Zeile, die sagt, welche Nummer als nächste fällt.

**Dies ist der erste Slice von `D0005` „Karten-Klassen".** Mit ihm bekommt der vierte Dialog der Anwendung seinen ersten grünen Knoten.

## Geschäftlicher Nutzen

Die Vision nennt drei Quellen von Aufgaben nebeneinander — WBS-Import, Bugmeldungen, Beschaffung — und verlangt, dass ein Agent „gezielt das richtige Set greift statt des ganzen Boards". Beides steht und fällt mit einem Merkmal an der Karte, an dem sich die Sets unterscheiden lassen. Heute gibt es keins: eine Karte trägt Titel, Etiketten, Farbe und Verantwortlichen, aber nichts, was sie einer Herkunft zuordnet, und nichts, was ihr eine Nummer gäbe, die außerhalb der Anwendung tragfähig wäre.

Die **Nummer** ist dabei der eigentliche Gewinn, nicht die Gruppierung. Ein Etikett gruppiert schon heute. Was ein Etikett nicht kann, ist eine **Identität** liefern, die in einen Zweignamen, eine Commit-Nachricht, einen Kommentar oder den Prompt eines Agenten wandert und dort noch stimmt: `WBS-32` ist kurz, sprechbar, eindeutig und überlebt jeden Umzug der Karte über Spalten und Boards hinweg. Genau diese Zusage macht den geführten Zählerstand nötig (siehe „Warum löst diese Anforderung das Problem?").

Für den KI-Agenten heißt der Slice: er kann sich seinen eigenen Nummernkreis anlegen, bevor er Karten schreibt — über dieselbe Route, über die die Oberfläche es tut. Für den Menschen heißt er: der Nummernkreis wird dort gepflegt, wo auch die Spalten gepflegt werden, im Layout-Modus des Boards, und nicht in einem eigenen Schirm, den niemand findet.

## Funktionale Anforderungen

- `POST /api/boards/{boardId}/kartenklassen` nimmt **Name** und **Präfix** entgegen und antwortet mit HTTP 201 und der angelegten `Kartenklasse`.
- `GET /api/boards/{boardId}/kartenklassen` liefert die Kartenklassen des Boards in **Anlagereihenfolge**; ein Board ohne Kartenklasse liefert eine **leere** Liste, kein 404.
- Jede Kartenklasse trägt eine eigene Nummer (`KartenklasseId`), den Namen, das Präfix und ihren **Zählerstand**.
- Eine Kartenklasse gehört **einem Board**; ihr Präfix ist **nur innerhalb dieses Boards** eindeutig, ohne Rücksicht auf Groß-/Kleinschreibung.
- Der Zählerstand einer frisch angelegten Kartenklasse ist **0**; dieser Slice schreibt ihn nie fort.
- Das Präfix wird **genommen, wie es getippt wurde** — der Trenner ist Teil des Präfix, die Anwendung hängt nichts an und schreibt nichts um; nur die Ränder werden getrimmt.
- Ein **leerer Name**, ein **leeres Präfix**, ein Präfix mit **unerlaubten Zeichen** und ein Präfix **über der Höchstlänge** werden mit Befund zurückgewiesen; es entsteht keine Zeile.
- Ein Präfix, das **auf diesem Board** schon vergeben ist, wird mit Befund zurückgewiesen; der Befund nennt **den Namen der Kartenklasse, die es hält**.
- Dasselbe Präfix auf einem **zweiten Board** wird angenommen.
- Der **Name** wird **nicht** auf Eindeutigkeit geprüft.
- Ein **unbekanntes Board** wird bei beiden Routen mit HTTP 404 samt Rumpf beantwortet.
- Der Layout-Modus des Boards zeigt unter der Spaltenpflege den Bereich „Klassen": je Zeile Name, Präfix als Plakette und rechts „n vergeben · nächste `<Nummer>`".
- Die nächste Nummer entsteht aus Präfix und `Zählerstand + 1` und ist **mindestens zweistellig** — eine frische Kartenklasse zeigt `WBS-01`.
- Vor der ersten Kartenklasse steht ein Satz statt einer Liste: „Dieses Board hat keine Klasse."
- Unter der Liste steht die Anlegezeile mit den Feldern **Name** und **Nummernkreis-Präfix** und dem Knopf „Klasse anlegen".
- Eine Zurückweisung erscheint als lesbare Meldung; die Liste bleibt unverändert.
- Alle Kartenklassen sind nach einem Reload und nach einem Neustart unverändert da.
- **Ändern und Entfernen einer Kartenklasse gibt es nicht** — weder als Route noch als Bedienelement.

## Nicht-funktionale Anforderungen

- **Begriff (C06):** Der Gegenstand heißt im ganzen Stack **`Kartenklasse`** — Tabelle, Spaltennamen, Contracts, Route, Leser, Validator, Befundcodes. Die **Beschriftung in der Oberfläche** bleibt **„Klassen"**. Das ist kein Widerspruch, sondern die Trennung von Ort und Stack: im Board steht das Wort neben „Spalten" und ist dort so eindeutig wie dieses, im Code dagegen ist „Klasse" der Programmierbegriff. **Die Beschriftung folgt der Lesart am Ort, der Bezeichner der Eindeutigkeit im Stack.** Anders als bei `Teilaufgabe` (`R00018`) und `Dateiverweis` (`R00021`), wo beides zusammenfiel.
- **Der fachliche Zähler heißt `Zaehlerstand`, die formatierte Nummer `Kartennummer`** — nicht `Nummer`. Die Id-Konvention des Projekts reserviert das Wort für Schlüssel (Primärschlüssel `<Tabelle>Id`, Fremdschlüssel nach der referenzierten Tabelle); ein Feld `Nummer` an `Kartenklasse` läse sich wie ein Schlüssel und wäre keiner. C07: Bezeichner ohne echte Umlaute.
- **Der Zählerstand wird geführt, nicht gerechnet.** Er ist eine Spalte an der Kartenklasse, beginnt bei 0 und **wächst nur**. Eine aus den zugeordneten Karten gerechnete Zahl (`MAX`, `COUNT`) fiele zurück, sobald eine Karte die Klasse verlässt oder wechselt — die nächste Zuordnung bekäme eine Nummer, **die es schon gab**. Eine Kartennummer ist aber eine Identität (Kommentare, Zweignamen, WBS-Import `I0030`) und darf sich nicht wiederholen. Begründung ausführlich unter „Warum löst diese Anforderung das Problem?".
- **Datenhaltung:** `016-kartenklassen.sql` ist idempotent (`CREATE TABLE IF NOT EXISTS`) — der `Migrationslaeufer` führt jedes Skript bei **jedem** Start aus und kennt kein Journal. **Der Zählerstand steht in dieser Migration und nicht erst in der von `I0021`:** `ALTER TABLE ADD COLUMN` scheitert im zweiten Lauf, und eine bestehende `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich um eine Spalte.
- **Ein eindeutiger Index auf `(Board, Praefix COLLATE NOCASE)`** — die zweite Stelle nach `UX_Spalte_Board_Bezeichnung` (`002`), an der dieses Projekt eine Regel im Schema absichert. Er sichert sie gegen jeden Weg, der am Dienst vorbeischreibt; der lesbare Befund entsteht trotzdem davor im Validator, damit der Aufrufer **nie** auf eine nackte Datenbankmeldung trifft. Dazu ein eigener Index auf `Board`, aus demselben Grund wie in den Migrationen 012–015: der Primärschlüssel führt mit `KartenklasseId`, und jeder Leseweg dieser Tabelle fragt nach dem Board.
- **Geprüft wird die Form des Präfix, nicht seine Bedeutung.** Leere, Leerzeichen, Zeichenvorrat (`A-Z`, `a-z`, `0-9`, `-`, `_`) und Länge. Ausdrücklich **nicht** geprüft: ob das Präfix mit einem Trenner endet, ob es großgeschrieben ist, ob es aussieht wie die Beispiele. Wer `WBS_` will, bekommt es.
- **Die Kartenklasse reist nicht am `Board`-Contract mit** — eigene Leseroute statt eines Feldes an `Board`. Drei Gründe: **19 positionale `new Board(`-Aufrufstellen in 4 Dateien** (`BoardServiceTests.cs` 15, `BoardRepository.cs` 2, `TestBoardRepository.cs` 1, `ApiErgebnisTests.cs` 1) — dieselbe Größenordnung, aus der `R00006` die n-Beziehungen von `Karte` ferngehalten hat; `I0021` braucht die Klassenliste auf der **Kartenseite**, die `Kartendetail` lädt und nie ein `Board`; und `I0022` setzt eine Klassenressource unter dem Board ohnehin voraus. Ein Feld am `Board` wäre eine zweite Quelle für dieselbe Liste.
- **Route `…/kartenklassen`, Beschriftung „Klassen"** — begründete Abweichung vom Artboard, das `POST /api/boards/2/klassen` zeichnet (`D0005.dc.html:199`). Siehe „Gestaltungsvorgabe". `I0022` erbt die Route und muss sie nicht neu entscheiden.
- **Antwortgestalt:** **201 mit der angelegten `Kartenklasse`** und der `Location`-Kopfzeile, **200 mit der Liste** beim Abruf — das Muster von `SpaltenEndpunkte`, nicht das der Kartenunterressourcen (die antworten mit dem ganzen `Kartendetail`, weil sie eine Seite tragen; hier gibt es keine solche Seite).
- **Fehlerantworten für Agenten:** Jede Fehlerantwort der zwei neuen Routen trägt einen Rumpf mit Code, Meldung (mit den aufgerufenen Werten) und Kompensationsaktion — der Vertrag aus `R00007` gilt unverändert, **auch bei 404**. **Die Vertragsfälle beider Routen gehören in denselben Arbeitsgang wie die Routen** (`FehlervertragTests.cs:41-58`): der Test liest die registrierten Routen aus dem Testhost und ist zwischen Route und Vertragsfall rot. **Beide** neuen Routen liefern eine Fehlerantwort (unbekanntes Board); **keine** gehört auf `RoutenOhneFehlerantwort` (`FehlervertragTests.cs:18-23`).
- **Gestaltung:** Alle Gestaltungswerte kommen aus `wwwroot/gestaltung.css`; kein Literal in einer Komponenten-CSS-Datei, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").
- **Systemgrenzen:** `KanbanC.Blazor` bekommt auch hier **keine** Projektreferenz auf `KanbanC.BL`; alles läuft über einen eigenen `KartenklassenApiKlient` als JSON. Jede Zusage dieses Slice gilt über beide Systemgrenzen: was der Bereich im Layout-Modus kann, kann die API.
- **Rückwirkungsfreiheit:** Der grüne Bestand bleibt grün. **`LayoutModusE2ETests` muss unverändert grün bleiben** — seine 17 `ToHaveCountAsync`-Zusagen zählen ausschließlich unter `#spaltenbahnen` und `#neue-spalte`; der neue Bereich trägt **eigene Kennungen**. Zieht ein Test doch mit, ist das eine benannte Änderung und keine stille.

## Akzeptanzkriterien

### Die Kartenklasse entsteht mit Name und Präfix (API)

- [ ] `POST /api/boards/{boardId}/kartenklassen` mit Name und Präfix antwortet mit HTTP 201 und einer `Kartenklasse` mit `KartenklasseId`, Name, Präfix und `Zaehlerstand`.
- [ ] Die `Location`-Kopfzeile der Antwort zeigt auf die angelegte Kartenklasse.
- [ ] Der `Zaehlerstand` der angelegten Kartenklasse ist **0** — und zwar auch dann, wenn das Board schon Karten trägt.
- [ ] `GET /api/boards/{boardId}/kartenklassen` liefert danach HTTP 200 und diese Kartenklasse in der Liste.
- [ ] Name und Präfix kommen **zeichengleich bis auf die Ränder** zurück. Rechenbeispiel: `"  Dokumentation  "` / `"  DOK-  "` wird als `Dokumentation` / `DOK-` gespeichert; `WBS_` bleibt `WBS_`, `wbs-` bleibt kleingeschrieben `wbs-`.
- [ ] Ein Board **ohne** Kartenklasse liefert HTTP 200 und eine **leere Liste** — kein 404, keine Fehlermeldung.
- [ ] Rechenbeispiel Reihenfolge: werden nacheinander `WBS`, `Bugmeldungen`, `Beschaffung` angelegt, lautet die Liste in jedem folgenden Abruf `WBS`, `Bugmeldungen`, `Beschaffung` — **Anlagereihenfolge**, nicht alphabetisch.
- [ ] Die Kartenklassen hängen **nicht** am `Board`: `GET /api/boards/{boardId}` liefert das Board unverändert **ohne** Kartenklassenliste.
- [ ] Ein Neustart der Anwendung lässt Namen, Präfixe, Zählerstände und Reihenfolge unverändert.
- [ ] Der zweite Lauf des `Migrationslaeufer` auf einer bestehenden Datei lässt Schema und Daten unverändert.

### Was geprüft wird — und was ausdrücklich nicht

- [ ] Ein **leerer Name** wird mit HTTP 400 **und Rumpf** zurückgewiesen; es entsteht keine Zeile. Die Meldung lautet „Eine Klasse braucht einen Namen."
- [ ] Ein Name, der **nur aus Leerzeichen** besteht, gilt als leer und wird ebenso zurückgewiesen.
- [ ] Ein **leeres Präfix** wird mit HTTP 400 und Rumpf zurückgewiesen.
- [ ] Ein Präfix mit einem **Leerzeichen** darin wird zurückgewiesen — eine Kartennummer wandert in Zweignamen, Meldungen und Suchfelder.
- [ ] Ein Präfix mit einem Zeichen außerhalb von `A-Z`, `a-z`, `0-9`, `-`, `_` wird zurückgewiesen. Rechenbeispiel: `WBS-` und `WBS_2` gehen durch, `WBS/` und `WBS.` nicht.
- [ ] Ein Präfix **über der Höchstlänge** wird zurückgewiesen; der Befund **nennt die Höchstlänge**. Rechenbeispiel: bei einer Höchstlänge von 8 geht `ABCDEFGH` durch, `ABCDEFGHI` nicht.
- [ ] Ein Präfix, das **nicht** mit einem Trenner endet (`WBS`), wird **angenommen** — die Anwendung hängt nichts an.
- [ ] Ein Präfix in **Kleinbuchstaben** (`wbs-`) wird angenommen und **unverändert** abgelegt.
- [ ] Ein **doppelter Name** auf demselben Board wird **angenommen** — der Name wird nicht auf Eindeutigkeit geprüft.
- [ ] Die Zurückweisung nennt in der Kompensationsaktion die **Route samt Boardnummer**, wie bei Spalte, Etikett, Teilaufgabe und Kommentar.
- [ ] Nach jeder Zurückweisung wurde **nicht geschrieben**: die Liste des Boards ist unverändert.

### Das Präfix ist je Board eindeutig

- [ ] Ein zweiter `POST` mit **demselben** Präfix auf **dasselbe** Board antwortet mit HTTP 400 und Rumpf; die Liste bleibt bei **einem** Eintrag.
- [ ] Die Prüfung greift **ohne Rücksicht auf Groß-/Kleinschreibung**: `WBS-` und `wbs-` gelten auf einem Board als dasselbe Präfix.
- [ ] Die Prüfung greift auch, wenn sich die beiden Aufrufe nur an den **Rändern** unterscheiden: `"WBS-"` und `" WBS- "` sind dasselbe Präfix, weil getrimmt wird.
- [ ] Der Befund **nennt die Kartenklasse, die das Präfix hält**, mit ihrem Namen: „Das Präfix WBS- führt auf diesem Board schon die Klasse „WBS". Wähle ein anderes Präfix." — Grund **und** Kompensationsaktion in einem Satz.
- [ ] Dasselbe Präfix auf einem **zweiten Board** wird angenommen; beide Boards führen es danach nebeneinander.
- [ ] Das Schema sichert die Regel zusätzlich ab: ein direkter zweiter `INSERT` mit demselben `(Board, Praefix)` — auch in abweichender Schreibweise — scheitert an der Datenbank.
- [ ] Der Aufrufer trifft **nie** auf eine nackte Datenbankmeldung über einen verletzten Index.

### Der Zählerstand wird geführt, nicht gerechnet

- [ ] `Kartenklasse` trägt ein Feld `Zaehlerstand`; es ist **gespeichert** und wird nicht aus den Karten der Klasse gerechnet.
- [ ] Der Zählerstand einer frisch angelegten Kartenklasse ist 0 und bleibt es in diesem Slice — **keine** Route dieses Slice erhöht ihn.
- [ ] Die nächste Nummer entsteht aus Präfix und `Zaehlerstand + 1`, **mindestens zweistellig**. Rechenbeispiele: Stand 0 → `WBS-01`; Stand 7 → `BUG-08`; Stand 31 → `WBS-32`; Stand 100 → `WBS-101`.
- [ ] Die Auffüllung auf zwei Stellen ist **kein Abschneiden**: ein dreistelliger Stand wächst auf drei Stellen, ein vierstelliger auf vier.
- [ ] Das Präfix geht **unverändert** in die Nummer ein: `WBS_` mit Stand 0 ergibt `WBS_01`, nicht `WBS-01`.
- [ ] Die Bildung der Nummer ist **ohne Datenbank** prüfbar — sie ist reine Formatierung.

### Fehlerantworten für Agenten

- [ ] Ein **unbekanntes Board** liefert bei **beiden** Routen HTTP 404 **mit Rumpf** (Code, Meldung mit der aufgerufenen Nummer, Kompensationsaktion).
- [ ] Der 404-Befund entsteht über `Nichtgefunden.Board(boardId)` und ist damit derselbe wie bei den Spalten- und Kartenrouten — keine handgeschriebene Variante.
- [ ] `Nichtgefunden` wächst in diesem Slice **nicht** um eine Schwester: es gibt keine Adresse auf eine einzelne Kartenklasse.
- [ ] Jeder 400er-Befund trägt einen nichtleeren Code, eine Meldung mit den aufgerufenen Werten und eine nichtleere Kompensationsaktion.
- [ ] `FehlervertragTests` ruft **beide** neuen Routen ab und bleibt grün; **keine** der beiden steht auf `RoutenOhneFehlerantwort`.

### Der Klassenbereich im Layout-Modus

- [ ] Auf `/boards/{boardId}` erscheint der Bereich **nur im Layout-Modus**, unterhalb der Spaltenpflege, getrennt durch dieselbe Linie, die das Token-Sheet zieht.
- [ ] In der **Arbeitsansicht** (Layout-Modus aus) ist der Bereich **nicht** da.
- [ ] Der Bereich trägt die Überschrift **„Klassen"** — nicht „Kartenklassen".
- [ ] Jede Zeile zeigt den **Namen**, das **Präfix als Plakette** und rechts „n vergeben · nächste `<Nummer>`". Rechenbeispiel: eine frische Klasse „WBS" mit Präfix `WBS-` zeigt „0 vergeben · nächste WBS-01".
- [ ] Die Zeilen stehen in **Anlagereihenfolge**.
- [ ] Vor der ersten Kartenklasse steht der Satz „Dieses Board hat keine Klasse." — **ein Satz, kein Kasten**, in Wortlaut und Form wie der Leerzustand der Spalten; ein Board ohne Klasse ist kein Fehler.
- [ ] Unter der Liste steht die Anlegezeile mit den beschrifteten Feldern **„Name"** und **„Nummernkreis-Präfix"** und dem Knopf **„Klasse anlegen"**.
- [ ] Die Eingabefelder tragen **kein `value`-Attribut** (`@bind`, wie bei Spalte, Teilaufgabe und Kommentar).
- [ ] Nach dem Anlegen steht die neue Zeile als **letzte** in der Liste und die Felder sind leer.
- [ ] Ein leerer Name und ein doppeltes Präfix bringen eine **lesbare Meldung** auf der Seite („Die Klasse wurde nicht angelegt:" plus Befundliste); die Liste bleibt unverändert.
- [ ] Ist die WebApi nicht erreichbar, erscheint der übliche Ausfallsatz statt einer Ausnahmeseite.
- [ ] Nach einem Reload zeigt der Bereich dieselben Kartenklassen in derselben Reihenfolge.
- [ ] Alle Gestaltungswerte des Bereichs kommen aus `gestaltung.css`; die Komponenten-CSS-Datei enthält **kein** Farb-, Abstands- oder Radius-Literal.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] **Benannte Änderung 1:** `Source/KanbanC.Blazor/Components/Pages/Board.razor` (`:83-86`) — der Layout-Zweig bindet den neuen Bereich **zusätzlich** zu `<Spaltenpflege>` ein. Der Arbeitsansicht-Zweig bleibt unverändert.
- [ ] **Benannte Änderung 2:** `Source/KanbanC.Blazor/Program.cs` — der neue Klient wird neben `BoardApiKlient`, `SpaltenApiKlient`, `KartenApiKlient` und `KontributorenApiKlient` registriert.
- [ ] **Benannte Änderung 3:** `Source/KanbanC.PlaywrightTests/PageObjects/BoardSeite.cs` wächst um die Locator des neuen Bereichs. **Bestehende Locator werden nicht geändert.**
- [ ] **Benannte Änderung 4:** `FehlervertragTests` ruft die zwei neuen Routen ab. `RoutenOhneFehlerantwort` (`:18-23`) bleibt **unverändert** — beide neuen Routen liefern eine Fehlerantwort.
- [ ] **`LayoutModusE2ETests` bleibt unverändert grün.** Der neue Bereich trägt eigene Kennungen (`#klassenpflege` und Geschwister) und eigene Klassennamen; die Anlegezeile heißt bewusst **nicht** `.spaltenpflege-neu`, obwohl sie so aussieht. Zieht doch eine Zusicherung mit, ist das ein Befund und wird als benannte Änderung geführt — **nicht** durch Anpassen der Zahl erledigt.
- [ ] `Spaltenpflege.razor`, `Spaltenbahnen.razor` und `Spaltenleser` bleiben unverändert.
- [ ] `Board.cs`, `Spalte.cs` und `Karte.cs` (`KanbanC.Contracts`) bleiben unverändert — insbesondere wächst `Board` **nicht** um eine Kartenklassenliste.
- [ ] `Karte.razor` bleibt unverändert — **auf der Bahn steht in diesem Slice keine Plakette und keine Nummer** (das ist `I0021`).
- [ ] `Kartendetail.razor` bleibt unverändert — das Feld „Klasse" auf der Kartenseite ist `I0021`.
- [ ] `GET /api/boards/{boardId}` und alle bestehenden Routen bleiben in Adresse, Verb und Antwortgestalt unverändert.
- [ ] Alle E2E-Suiten aus `R00001`–`R00021` bleiben grün; geändert werden **nur** die vier oben benannten Stellen.

## Betroffene Verzeichnisstruktur

- **Schema:** `Source/KanbanC.BL/Persistenz/Migrationen/016-kartenklassen.sql` — neue, idempotente Migration; Tabelle `Kartenklasse` mit `KartenklasseId` als Primärschlüssel, `Board` als Fremdschlüssel (nach der referenzierten Tabelle benannt, Projektregel), `Name TEXT NOT NULL`, `Praefix TEXT NOT NULL`, `Zaehlerstand INTEGER NOT NULL DEFAULT 0`, ein Index auf `Board` und ein **eindeutiger** Index auf `(Board, Praefix COLLATE NOCASE)`.
- **Contracts:** `Source/KanbanC.Contracts/Klassen/Kartenklasse.cs` (neu), `KartenklasseAnlegenAnfrage.cs` (neu). Der Ordner `Klassen/` liegt leer im Bestand und war für `D0005` reserviert — wie `Ereignisse/` für `D0007` und `Zeiten/` für `D0006`.
- **Fachlogik (Operations):** `Source/KanbanC.BL/Operations/Klassen/KartenklassenValidator.cs` (neu), `Kartenklassenpraefix.cs` (neu — Trimmen und Vergleich, Muster `Spaltenbezeichnung`), `Kartennummer.cs` (neu — die reine Formatierung aus Präfix und Stand).
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Klassen/KartenklassenRepository.cs` (neu), `Kartenklassenleser.cs` (neu, `internal` — der SELECT, den `I0021` und `I0022` wiederverwenden), `Source/KanbanC.BL/Interfaces/Klassen/IKartenklassenRepository.cs` (neu).
- **Dienste:** `Source/KanbanC.BL/Integrations/Klassen/KartenklassenService.cs` (neu) — Muster `SpaltenService.LegeSpalteAn`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/KartenklassenEndpunkte.cs` (neu) — Board-Unterressource nach dem Muster `SpaltenEndpunkte`; Registrierung in `Source/KanbanC.WebApi/Program.cs`.
- **Oberfläche:** `Source/KanbanC.Blazor/Services/KartenklassenApiKlient.cs` (neu), `Source/KanbanC.Blazor/Components/Klassen/Klassenpflege.razor(.css)` (neu), `Source/KanbanC.Blazor/Components/Pages/Board.razor` (`:83-86`, der Layout-Zweig), `Source/KanbanC.Blazor/Program.cs` (Registrierung).
- **Unberührt:** `Source/KanbanC.Contracts/Boards/Board.cs`, `Source/KanbanC.Blazor/Components/Spalten/Spaltenpflege.razor`, `Source/KanbanC.Blazor/Components/Karten/Karte.razor`, `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` — **weder das Board-DTO noch die Bahn noch die Kartenseite ändern sich in diesem Slice.**
- **Tests:** `Source/KanbanC.BL.Tests/` (`Operations/Klassen/KartenklassenValidatorTests.cs`, `Operations/Klassen/KartennummerTests.cs`, `Operations/Klassen/KartenklassenpraefixTests.cs`, `Integrations/Klassen/KartenklassenServiceTests.cs`, `TestHelpers/TestKartenklassenRepository.cs`), `Source/KanbanC.Blazor.Tests/` (`Services/KartenklassenApiKlientTests.cs`, `Gestaltung/`-Prüfung des neuen Bereichs), `Source/KanbanC.WebApi.IntegrationTests/` (`Persistenz/Klassen/KartenklassenRepositoryTests.cs`, `Persistenz/MigrationslaeuferTests.cs`, `Api/KartenklassenEndpunkteTests.cs`, `Api/FehlervertragTests.cs`, `Api/WebApiNeustartTests.cs`), `Source/KanbanC.PlaywrightTests/` (`PageObjects/BoardSeite.cs`, `Infrastructure/WebApiKlient.cs`, neue Testklasse `KlasseAnlegenE2ETests`).

## Technische Überlegungen

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0005.dc.html`](../Dokumentation/Wireframes/D0005.dc.html) ist die Gestaltungsvorgabe. Für diesen Slice gelten daraus **Zustand 1** (der Klassenbereich im Layout-Modus, `:154-203`), **Zustand 2** (der Leerzustand, `:206-235`) und **Zustand 3** (die drei Ränder, `:238-286`) sowie die Lesehilfe (`:388-392`). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`) — die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

**Zustand 4 und 5 gehören nicht in diesen Slice:** das Feld „Klasse" auf der Kartenseite und die Plakette auf der Karte sind `I0021`, der Aufruf `…/karten` ist `I0022`. Beide zeigen den Zählerstand schon auf 31; hier steht jede frische Klasse auf „0 vergeben · nächste WBS-01".

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen keine Akzeptanzkriterien, so wie aus einer Bubble keine entstehen. Geprüft wird gegen die User Story.

**Drei bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die Route heißt `…/kartenklassen`, nicht `…/klassen`.** Das Artboard schreibt `POST /api/boards/2/klassen` (`:199`) und in Zustand 5 `GET /api/boards/2/klassen/1/karten` (`:363`). Gebaut wird `…/kartenklassen`, weil C06 **einen** Begriff in **einer** Schreibweise verlangt und die Route eine der Stellen ist, an denen der Bezeichner steht — nicht eine Beschriftung. Ein Artboard ist Entwurf, kein Vertrag. **Dieselbe Bewegung wie bei `R00018` („Subtasks" → „Teilaufgaben") und `R00021` („Verweise" → „Dateiverweise"), nur umgekehrt herum:** dort wich die Beschriftung vom Bild ab, hier weicht der **Bezeichner** von der Beschriftung ab und die Beschriftung bleibt, wie sie gezeichnet ist. `I0022` erbt die Route.
2. **Zwei Kennungssätze, die im Bild nicht zu sehen sind.** Der Bereich trägt `#klassenpflege` und Geschwister und eigene Klassennamen, damit die 17 Zählzusagen von `LayoutModusE2ETests` unberührt bleiben; die Anlegezeile heißt darum **nicht** `.spaltenpflege-neu`, obwohl das Artboard ausdrücklich „Form und Rhythmus von `.spaltenpflege-neu`" sagt (`:186-187`). Die **Form** wird übernommen, der **Name** nicht.
3. **Die zweistellige Auffüllung und die Anlagereihenfolge sind aus dem Bild abgeleitet, nicht ausgeschrieben.** Das Artboard zeigt `WBS-32`, `BUG-08`, `BES-05` (`:163-176`), `D0001` und `D0003` zeigen `WBS-09` — daraus folgt die Auffüllung. Und es führt WBS, Bugmeldungen, Beschaffung in einer Ordnung, die weder alphabetisch noch nach einer Position ist — daraus folgt die Anlagereihenfolge. Beides steht als Kriterium in dieser Anforderung, weil ein abgelesenes Bild keine Vereinbarung ist.

Alles andere am Bereich folgt der Skizze.

### Ablauf

1. **Kartenklasse anlegen** (`POST /api/boards/{boardId}/kartenklassen`)
   - 1.1 `KartenklassenService.LegeKartenklasseAn(boardId, anfrage)` liest zuerst den Bestand: `KartenklassenRepository.LadeAlle(boardId)`
     - 1.1.1 `null` → Board unbekannt → `Nichtgefunden.Board(boardId)` → HTTP 404 mit Rumpf
     - 1.1.2 `[]` → Board vorhanden, noch ohne Kartenklasse → weiter
   - 1.2 `KartenklassenValidator.Pruefe(anfrage, vergebenePraefixe)` — die vergebenen Präfixe kommen **mit ihren Klassennamen**, weil der Dublettenbefund den Namen nennt
     - 1.2.1 Name leer (nach dem Trimmen) → `kartenklasse-name-leer`
     - 1.2.2 Präfix leer → `kartenklasse-praefix-leer`
     - 1.2.3 Präfix mit Leerzeichen, unerlaubtem Zeichen oder über der Höchstlänge → `kartenklasse-praefix-ungueltig`
     - 1.2.4 Präfix auf diesem Board vergeben (Vergleich getrimmt und ohne Rücksicht auf Groß-/Kleinschreibung) → `kartenklasse-praefix-vergeben`, Meldung mit dem Namen der haltenden Klasse
   - 1.3 Bei Befunden: HTTP 400 mit Rumpf, **kein** Schreibzugriff
   - 1.4 `KartenklassenRepository.LegeAn(boardId, anfrage)` in **einer** Transaktion — `INSERT` mit `Zaehlerstand = 0`, danach die geschriebene Zeile zurücklesen
   - 1.5 HTTP 201 mit der `Kartenklasse` und der `Location`-Kopfzeile
2. **Kartenklassen abrufen** (`GET /api/boards/{boardId}/kartenklassen`)
   - 2.1 `KartenklassenRepository.LadeAlle(boardId)` — `SELECT … ORDER BY KartenklasseId`
   - 2.2 `null` → `Nichtgefunden.Board(boardId)` → HTTP 404 mit Rumpf
   - 2.3 Sonst HTTP 200 mit der Liste, auch wenn sie leer ist
3. **Die nächste Nummer** (reine Formatierung, ohne Datenbank)
   - 3.1 `Kartennummer.Aus(praefix, stand)` → `praefix` + `stand`, mindestens zweistellig aufgefüllt
   - 3.2 Die Zeile im Bereich zeigt `Zaehlerstand` als „n vergeben" und `Kartennummer.Aus(praefix, Zaehlerstand + 1)` als „nächste …"
4. **Der Bereich im Layout-Modus** (Oberfläche)
   - 4.1 `Board.razor` (`:83-86`) bindet im Layout-Zweig `<Klassenpflege BoardId="BoardId" />` unter `<Spaltenpflege …>` ein
   - 4.2 Die Komponente lädt ihre Liste selbst über `KartenklassenApiKlient.LadeKartenklassen(boardId)` — **nicht** über `_board`, weil die Kartenklassen nicht am `Board`-Contract hängen
   - 4.3 Nach dem Anlegen lädt sie neu; die Zurückweisung erscheint als Meldung über der Anlegezeile
   - 4.4 Der Ausfall der WebApi liefert den üblichen Ausfallsatz

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:**

- **`KartenklassenEndpunkte`** — eine neue Endpunktklasse als Board-Unterressource, registriert in `KanbanC.WebApi/Program.cs` neben `SpaltenEndpunkte`.
- **`Migrationslaeufer`** — die sechzehnte Migration reiht sich ein; kein Journal, also idempotent.
- **`Board.razor`** (`:83-86`) — der Layout-Zweig, in dem heute nur `<Spaltenpflege>` steht.
- **`Blazor/Program.cs`** — die Stelle, an der die vier bestehenden Klienten registriert sind.

**Klassen-Entwurf:**

- `Kartenklasse` (DTO, immutable) — ein Nummernkreis eines Boards. Der `Zaehlerstand` reist mit, weil die Zeile die nächste Nummer zeigt.
  - `record Kartenklasse(long KartenklasseId, string Name, string Praefix, int Zaehlerstand)`
- `KartenklasseAnlegenAnfrage` (DTO, immutable) — genau die zwei Angaben des Fertig-Kriteriums. **Kein Feld für den Zählerstand:** der Aufrufer setzt ihn nicht.
  - `record KartenklasseAnlegenAnfrage(string Name, string Praefix)`
- `Kartenklassenpraefix` (Operation, pure Logik) — Trimmen und der Vergleich ohne Rücksicht auf Groß-/Kleinschreibung. Muster `Spaltenbezeichnung`.
  - `static string Normalisiert(string praefix)`
  - `static bool SindGleich(string eines, string anderes)`
- `Kartennummer` (Operation, pure Logik, ohne Datenbank) — Präfix und Stand zu einer Nummer, mindestens zweistellig.
  - `static string Aus(string praefix, int stand)`
- `KartenklassenValidator` (Operation, pure Logik) — Muster `SpaltenValidator`, mit **einem** Unterschied: die vergebenen Präfixe kommen als **Paar aus Präfix und Klassenname**, weil der Dublettenbefund den Namen nennt.
  - `static Pruefbefunde Pruefe(KartenklasseAnlegenAnfrage anfrage, IReadOnlyList<Kartenklasse> vergebene)`
- `Kartenklassenleser` (Provider/Ressourcenzugriff, `internal`) — der SELECT über die Kartenklassen eines Boards in Anlagereihenfolge, in der laufenden Transaktion. Muster `Spaltenleser`; `I0021` und `I0022` benutzen ihn wieder.
  - `static IReadOnlyList<Kartenklasse> LiesKartenklassenDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)`
- `IKartenklassenRepository` (Interface) — die Schwester von `ISpaltenRepository` in `Interfaces/Klassen/`, damit der Dienst gegen ein Test-Repository prüfbar ist.
- `KartenklassenRepository` (Provider, Integration nach Hausregel) — `null` für ein unbekanntes Board, `[]` für ein Board ohne Kartenklasse. **Genau die Unterscheidung, aus der der Dienst sein 404 zieht.**
  - `IReadOnlyList<Kartenklasse>? LadeAlle(long boardId)`
  - `Ergebnis<Kartenklasse> LegeAn(long boardId, KartenklasseAnlegenAnfrage anfrage)`
- `KartenklassenService` (Integration, orchestriert) — erst `LadeAlle`, dann der Validator mit den vergebenen Präfixen, dann `LegeAn`. IOSP: geprüft wird im Validator, formatiert in `Kartennummer`.
  - `Ergebnis<Kartenklasse>? LegeKartenklasseAn(long boardId, KartenklasseAnlegenAnfrage anfrage)`
  - `IReadOnlyList<Kartenklasse>? LadeKartenklassen(long boardId)`
- `KartenklassenApiKlient` (Integration in der Oberflächenschicht) — Muster `SpaltenApiKlient`, JSON in beide Richtungen.
  - `Task<ApiErgebnis<IReadOnlyList<Kartenklasse>>> LadeKartenklassen(long boardId)`
  - `Task<ApiErgebnis<Kartenklasse>> LegeKartenklasseAn(long boardId, KartenklasseAnlegenAnfrage anfrage)`
- `Klassenpflege` (Razor-Komponente) — Liste, Leerzustand, Anlegezeile, Zurückweisung und Ausfallmeldung unter eigenen Kennungen. Lädt ihre Liste selbst.

### Änderungen an bestehenden Klassen

- `Board.razor` (`:83-86`) — der Layout-Zweig bindet `<Klassenpflege>` unter `<Spaltenpflege>` ein, getrennt durch die Linie des Token-Sheets. Der Arbeitsansicht-Zweig bleibt unverändert.
- `KanbanC.Blazor/Program.cs` — `KartenklassenApiKlient` als fünfter Klient registriert.
- `KanbanC.WebApi/Program.cs` — `KartenklassenEndpunkte.Registriere(app)` und der `KartenklassenService` in der Dienstsammlung.
- `FehlervertragTests` — zwei Fälle mehr (unbekanntes Board bei `POST` und bei `GET`); `RoutenOhneFehlerantwort` bleibt unverändert.
- `BoardSeite` (`Source/KanbanC.PlaywrightTests/PageObjects/`) — die Locator des neuen Bereichs kommen dazu, bestehende bleiben unangetastet.
- `WebApiKlient` (`Source/KanbanC.PlaywrightTests/Infrastructure/`) — `LegeKartenklasseAn` für den Aufbau der E2E-Lage.
- **`Board.cs` wird nicht angefasst** — das ist der Kern der Entscheidung „die Kartenklasse reist nicht am `Board`-Contract mit".

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Kartennummer.Aus` — Stand 0 → `WBS-01`; Stand 7 → `BUG-08`; Stand 31 → `WBS-32`; Stand 100 → `WBS-101`; Stand 9999 → `WBS-9999`; das Präfix geht **unverändert** ein (`WBS_` → `WBS_01`). Reine Formatierung, keine Datenbank.
- `Kartenklassenpraefix.Normalisiert` / `SindGleich` — Ränder fallen weg; `WBS-` und `wbs-` sind gleich; `WBS-` und `WBS_` sind verschieden; die Normalisierung schreibt die Schreibweise **nicht** um.
- `KartenklassenValidator.Pruefe` — leerer Name (Befund mit dem Wortlaut „Eine Klasse braucht einen Namen."), Name nur aus Leerzeichen (Befund), leeres Präfix (Befund), Präfix mit Leerzeichen (Befund), Präfix mit `/` und mit `.` (Befund), Präfix genau an der Höchstlänge (**kein** Befund), Höchstlänge + 1 (Befund, der die Höchstlänge nennt), Präfix ohne Trenner (**kein** Befund), vergebenes Präfix in abweichender Schreibweise (Befund, **der den Namen der haltenden Klasse nennt**), doppelter **Name** bei freiem Präfix (**kein** Befund); jede Kompensation nennt `POST /api/boards/{boardId}/kartenklassen` samt Nummer.
- `KartenklassenService.LegeKartenklasseAn` / `LadeKartenklassen` gegen `TestKartenklassenRepository` — unbekanntes Board liefert `null` (und daraus wird 404); ein Board ohne Kartenklasse liefert `[]` und **kein** `null`; Erfolg reicht die Klasse durch; nach einer Zurückweisung wurde **nicht geschrieben**; die vergebenen Präfixe kommen aus `LadeAlle` und nicht aus einer zweiten Quelle.
- `KartenklassenApiKlient` (in `KanbanC.Blazor.Tests`, gegen `TestKlientFabrik`) — 200 liefert die Liste, 201 die Klasse, 400 und 404 die Zurückweisung mit Befund; Methode, Adresse und Rumpf des abgesetzten Aufrufs werden mitgeprüft, insbesondere **dass die Adresse `kartenklassen` und nicht `klassen` lautet**. Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses Testprojekt gibt (`CLAUDE.md`, Abweichung 4).

**Integration:** `KartenklassenRepository.LegeAn` / `LadeAlle` und `Kartenklassenleser` gegen eine `TemporaereDatenbank` — schreiben und wieder lesen; Name und Präfix kommen **zeichengleich** zurück; der `Zaehlerstand` der geschriebenen Zeile ist **0**; drei Kartenklassen kommen in **Anlagereihenfolge** zurück, auch wenn ihre Namen alphabetisch anders lägen; ein unbekanntes Board liefert `null`, ein Board ohne Kartenklasse `[]`. **Der eindeutige Index selbst** wird geprüft: ein direkter zweiter `INSERT` mit `(Board, Praefix)` in abweichender Schreibweise scheitert an der Datenbank, derselbe Präfix auf einem **zweiten** Board geht durch. `Migrationslaeufer` — zweiter Lauf lässt Schema und Daten unverändert. `KartenklassenEndpunkte` über `TestWebApi` — beide Routen mit 200/201, 400 (leerer Name; leeres Präfix; ungültiges Präfix; zu langes Präfix; vergebenes Präfix) und 404 (unbekanntes Board, bei beiden Routen) samt Rumpf; `GET /api/boards/{boardId}` trägt danach **keine** Kartenklassenliste; `FehlervertragTests` ruft beide Routen ab. `WebApiNeustartTests` — Namen, Präfixe, Zählerstände und Reihenfolge überstehen den Neustart.

**E2E:** Ein Board ohne Kartenklasse zeigt im Layout-Modus den Satz „Dieses Board hat keine Klasse." und die Anlegezeile (US-3). Name und Präfix eintragen und „Klasse anlegen" drücken → die Zeile erscheint mit Namen, Plakette und „0 vergeben · nächste WBS-01"; ein Reload zeigt sie unverändert (US-1). Ein leerer Name und ein auf diesem Board vergebenes Präfix werden **sichtbar** zurückgewiesen, die Liste bleibt unverändert (US-2). **Dasselbe Präfix auf einem zweiten Board wird angenommen** — dieser Fall braucht **zwei** Boards im Aufbau und ist die Probe darauf, dass die Eindeutigkeit je Board und nicht global gilt (US-4). In der Arbeitsansicht ist der Bereich nicht da (US-3). Dazu laufen die E2E-Suiten aus `R00001`–`R00021` weiter; **`LayoutModusE2ETests` unverändert** ist die Gegenprobe auf die eigenen Kennungen.

Repositories, `Kartenklassenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00001`** (Board anlegen — `I0001`, grün). Das ist der einzige Knoten, den die WBS-Spalte `Braucht` von `I0020` nennt; er ist erfüllt, der Slice ist **frei**.
- Setzt außerdem auf: **`R00004`** (`I0040`, grün — der Layout-Modus, in dem der Bereich sitzt, und `Spaltenpflege` als Muster für Leerzustand, Anlegezeile und Zurückweisung) und **`R00002`** (`I0003`, grün — `SpaltenService`, `SpaltenValidator`, `SpaltenRepository`, `SpaltenApiKlient` als durchgängiges Muster über alle Schichten). Die Spalte `Braucht` von `I0020` nennt beide **nicht**; das ist in der WBS ausdrücklich vermerkt („`Braucht` führt Vorbedingungen, keine Bauplätze") und an Front und Welle ändert es nichts, weil beide grün sind.
- Setzt ferner auf: **`R00007`** (Fehlervertrag, `Nichtgefunden.Board`, `FehlervertragTests`), **`R00005`** (Token-Sheet `gestaltung.css` mit `.tag` und `.hr`).
- Blockiert: **`I0021`** („Karte einer Klasse zuordnen", noch ohne Anforderung) nennt `I0020` in seiner Spalte `Braucht`. Über `I0021` hängen mittelbar `I0022`, `I0030` (WBS-Import) und `I0038` (Board exportieren) daran. **Öffnet damit `D0005`:** `I0020` ist der erste Slice des vierten Dialogs.

## Umfang

```
Klasse anlegen (I0020) = 10 Bubbles: 9 Standard (8,4h), 1 unklar (2,0–4,0h).
Rest: 8,4h klar + 2,0–4,0h unklar · 6 von 10 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 10 Bubbles gruen (0 %) · 0 laufen · 10 offen
```

`I0020` ist vollständig bis zur Bubble geplant und trägt seine zehn Bubbles (`B0294`–`B0303`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: die Interaction hat einen prüfbaren Aspekt, nicht mehrere. Anlegen, Auflisten, Leerzustand und die drei Ränder teilen Tabelle, Antwortgestalt, Komponente und E2E-Weg; getrennt geführt wären es Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0016` bis `I0019`. `I0001`, `I0006` und `I0007` haben Anlegen und Zurückweisung getrennt; dort trug die Zurückweisung eine eigene Oberflächenlage (ein Formular auf einer eigenen Seite), hier ist sie eine Meldung im selben Bereich. **Die Requirement-Klammer sitzt deshalb allein an `I0020`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0294` Kartenklassentabelle anlegen | Provider (Migration) | 0,4h (belegt) |
| `B0295` Kartenklassen eines Boards lesen | Contracts + Provider | 0,4h (belegt) |
| `B0296` Kartenklassen-Anfrage prüfen | Operation | 0,4h (belegt) |
| `B0297` Kartennummer aus Präfix und Zählerstand | Operation | 0,4h (belegt) |
| `B0298` Kartenklasse speichern | Provider | 0,4h (belegt) |
| `B0299` Kartenklassen verdrahten | Integration | 0,4h (belegt) |
| `B0300` Endpunkte der Kartenklassen | Integration | 2h (Richtwert) |
| `B0301` API-Klient der Kartenklassen | Integration | 2h (Richtwert) |
| `B0302` Klassenbereich im Layout-Modus | UI | 2h (Richtwert) |
| `B0303` E2E Klasse anlegen | E2E | 2–4h (**unklar**) |

Mit 10 Bubbles ist das **der schlankste Slice seit `I0004`** und zwei weniger als `I0019`: es gibt keine Zeitform, keinen Urheber, kein Entfernen, keine zweite Route auf ein einzelnes Ding und keinen unbelegten Boden — jede Schicht hat ein gebautes Muster bei den Spalten. Die einzige unklare Bubble ist der E2E-Lauf, und zwar aus **einem** Grund: der Rand „Präfix auf diesem Board vergeben" braucht **zwei** Boards im Aufbau, weil die Eindeutigkeit je Board gilt — genau diese Zusage bräche ein global eindeutiger Index still. Derselbe Vermerk wie bei `I0005` bis `I0019`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den tatsächlich gemessenen Werten vergleichbarer Bubbles; die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0020` trägt — anders als die zu `I0018` und `I0019` — **keine** eigene Zählzeile; sie hält nur die Aufwandskonvention fest. Die Zahlen oben sind über die Aufwandsspalte der zehn Bubbles gezählt.

## Offene Fragen

- **Wie hoch ist die Höchstlänge des Präfix?** — **nicht entschieden, im stillen Lauf mit 8 angenommen.** Gezeichnet sind vier Zeichen (`WBS-`, `BUG-`, `BES-`, `DOK-`), die Plakette sitzt auf einer 256px-Bahn neben dem Namen. 8 trägt `RELEASE-` und bleibt kurz genug, dass die Nummer in einem Zweignamen nicht dominiert. Vor `B0296` zu bestätigen; eine andere Zahl ändert genau eine Konstante und ein Rechenbeispiel.
- **Wie hoch ist die Höchstlänge des Namens?** — **nicht entschieden, im stillen Lauf mit der Länge der Spaltenbezeichnung angenommen.** Der Name steht an derselben Art Stelle wie eine Spaltenbezeichnung und sollte nicht enger sein. Betrifft `B0296`.
- **Sollen zwei Kartenklassen mit demselben Namen und verschiedenen Präfixen ein Fehler sein?** — **entschieden: nein**, aber ungeprüft am Menschen. Das Artboard zeichnet genau einen Eindeutigkeitsrand, und der nennt das Präfix (`:264`). Die Identität einer Kartenklasse ist ihr Präfix. Fiele die Entscheidung anders, käme ein fünfter Befund in den Validator — sonst nichts.
- **Bleibt die zweistellige Auffüllung bei zweistellig, wenn ein Board vierstellige Nummern erreicht?** — **entschieden: ja, sie ist eine Untergrenze**, keine feste Breite: `WBS-101` und `WBS-1024` wachsen. Aus dem Bild abgeleitet; nicht am Menschen geprüft, ob er stattdessen eine feste Breite wollte. Eine feste Breite wäre eine Obergrenze für den Nummernkreis und damit eine Zusage, die irgendwann bricht.
- **Soll die Klassenliste später sortierbar sein?** — **nicht entschieden.** Gebaut wird Anlagereihenfolge (`ORDER BY KartenklasseId`). Eine Sortierung von Hand bräuchte eine Positionsspalte und wäre eine eigene Entscheidung; eine alphabetische Ordnung ordnete die Liste beim Anlegen unter der Hand um.
- **Soll die Zeile die Klassennummer (`KartenklasseId`) zeigen?** — **entschieden: nein.** Das Artboard zeigt sie nicht, und für `I0022` braucht der Agent sie über die API, nicht am Schirm. Sie reist trotzdem im DTO mit.
- ~~Ist eine Kartenklasse board-gebunden oder boardübergreifend?~~ — **entschieden: board-gebunden** (`Kartenklasse.Board`), das Präfix ist nur innerhalb eines Boards eindeutig. **Der scheinbare Widerspruch im Bestand ist aufgelöst, siehe „Notizen".**
- ~~Heißt der Gegenstand „Klasse" oder „Kartenklasse"?~~ — **entschieden: `Kartenklasse` im Stack, „Klassen" als Beschriftung.** Begründung in den Nicht-funktionalen Anforderungen.
- ~~Hängt die Anwendung einen Trenner an das Präfix an?~~ — **entschieden: nein.** Das Präfix wird genommen, wie es getippt wurde.
- ~~Wird der Zählerstand gespeichert oder gerechnet?~~ — **entschieden: gespeichert.** Begründung unter „Warum löst diese Anforderung das Problem?".
- ~~Kommen Ändern und Entfernen mit?~~ — **entschieden: nein.** Begründung und Adresse der Lücke unter „Notizen → Bewusst out of scope".

## Manuelle Vorbereitungstätigkeiten

- Keine. Die Migration läuft bei jedem Start des `KanbanC.WebApi` mit.

## Manuelle Nachbereitungstätigkeiten

- Keine.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Zusage der Vision, die heute an keiner Stelle des Codes eingelöst werden kann: „ein Agent greift über die API gezielt das richtige Set statt des ganzen Boards" — und dafür braucht eine Karte ein Merkmal, das sagt, aus welcher Quelle sie stammt, plus eine Nummer, die außerhalb der Anwendung trägt. Weder das eine noch das andere existiert; ein Etikett gruppiert zwar, kann aber keine fortlaufende Identität vergeben. Die Kausalkette: **wenn** ein Board benannte Nummernkreise mit einem je Board eindeutigen Präfix und einem **geführten** Zählerstand bekommt (X), **dann** existiert der Träger, aus dem `I0021` einer Karte eine dauerhafte, nicht wiederkehrende Nummer geben kann (Y), **und dann** kann `I0022` genau diese Karten liefern und `I0030` eine WBS-Datei so einlesen, dass jede Zeile ihre eigene, wiederfindbare Kartennummer trägt (Z). **Der Hebel liegt beim Zählerstand, nicht bei der Gruppierung**: eine aus den Karten gerechnete Zahl fiele zurück, sobald eine Karte die Klasse verlässt, und die nächste Zuordnung bekäme eine Nummer, die es schon gab — eine Identität, die sich wiederholt, ist keine, und `WBS-32` in einem Commit zeigte dann auf zwei verschiedene Aufgaben. Und der Hebel liegt genau hier und nicht später: würde der Nummernkreis erst mit dem Zuordnen entstehen (`I0021`), müsste dieselbe Interaktion Klasse anlegen, Präfix prüfen, Zähler führen und die Karte binden — vier Zusagen in einem Slice, von denen sich keine einzeln prüfen ließe.

## Missing-Docs

- **`AUTOINCREMENT` und wachsende Zähler in SQLite:** Das Projekt führt bisher keinen fachlichen Zähler in einer Spalte. Ob und wie eine Erhöhung `UPDATE … SET Zaehlerstand = Zaehlerstand + 1 RETURNING …` in einer Transaktion mit Dapper zuverlässig den neuen Stand liefert, ist im Repository unbelegt — **relevant erst für `I0021`**, hier nur benannt, weil die Spalte in diesem Slice entsteht.
- **Eindeutige Indizes in SQLite und die Fehlerform bei Verletzung:** Das Projekt hat bisher genau einen (`002-spalte-bezeichnung-eindeutig.sql`), und keiner der Tests prüft, wie die Verletzung als Ausnahme ankommt. Für diesen Slice reicht es zu wissen, dass sie ankommt (der Validator fängt vorher ab); für einen späteren Slice, der die Dublette **am Index** erkennen wollte, wäre die Form zu belegen.
- **`COLLATE NOCASE` und nicht-ASCII-Zeichen:** SQLite ebnet mit `NOCASE` nur ASCII ein. Für diesen Slice ohne Wirkung, weil der Zeichenvorrat des Präfix auf `A-Z a-z 0-9 - _` beschränkt ist — **das ist ein zweiter, nicht genannter Grund für die Zeichenvorratsprüfung** und gehört festgehalten, falls der Vorrat je erweitert wird.

## Notizen

### Der aufgelöste Widerspruch: board-gebunden gegen „Cross-Board-Abfrage"

Die WBS führt `I0022` in ihrer Entscheidungsliste unter den **Cross-Board-Abfragen** (`Dokumentation/Planung/kanbanc.md`, Zeile zu R00001: „Cross-Board-Abfragen (I0022, I0033, I0034, I0036, I0037) wären sonst ein Merge über N Dateien"). Das liest sich, als sei eine Kartenklasse boardübergreifend — und widerspräche der Bindung `Kartenklasse.Board`, die dieser Slice baut.

**Der Widerspruch ist keiner.** Der Satz steht in der Begründung für **eine** SQLite-Datei für alle Boards und ist eine Aussage über die **Ablage**, nicht über die **fachliche Bindung**: er sagt, dass eine Abfrage nicht über N Dateien gehen soll, nicht dass eine Klasse mehreren Boards gehört. Die Vision sagt die Bindung ausdrücklich: der Agent greift das richtige Set „**statt des ganzen Boards**" — der Bezugsrahmen ist das Board. Das Artboard sagt sie ebenso (`D0005.dc.html:389`: „eine Kartenklasse ist an ihr Board gebunden, ihr Präfix nur dort eindeutig"). Und ohne Bindung wäre der gezeichnete Rand „Präfix auf **diesem** Board schon vergeben" (`:262`) sinnlos.

**Nicht vom Menschen bestätigt** — im stillen Lauf entschieden und hier benannt, weil die Formulierung in der WBS beim Lesen stolpern lässt. Fiele die Entscheidung anders, fielen `Kartenklasse.Board`, der zusammengesetzte Index und die Route unter dem Board; das wäre kein Detail, sondern ein anderer Slice.

### Verworfene Alternativen

| Option | Warum verworfen |
|---|---|
| **Den Zählerstand aus den Karten der Klasse rechnen** (`MAX`, `COUNT`) | Fällt zurück, sobald eine Karte die Klasse verlässt oder wechselt; die nächste Zuordnung bekäme eine Nummer, die es schon gab. Eine Kartennummer ist eine Identität und darf sich nicht wiederholen. Dass Karten archiviert statt gelöscht werden, entkräftet das nicht — der Klassenwechsel allein genügt. |
| **Den Bezeichner `Klasse`** (wie die Beschriftung) | „Klasse" ist im Repository der Programmierbegriff; C06 verlangt einen kontexteindeutigen Namen. `Kartenklasse` ist im Code eindeutig, und die Projekt-`CLAUDE.md` nennt genau dieses Wort unter C06. |
| **Die Beschriftung „Kartenklassen"** (wie der Bezeichner) | Im Board steht das Wort neben „Spalten" und ist dort so eindeutig wie dieses. Die Vision und der ältere Wireframe-Satz sagen ebenfalls „Klassen". Eine Beschriftung, die den Stack-Zwang mitschleppt, macht die Oberfläche schwerfällig, ohne etwas zu klären. |
| **Ein Feld `Kartenklassen` am `Board`-Contract** | 19 positionale `new Board(`-Aufrufstellen in 4 Dateien; `I0021` braucht die Liste auf der Kartenseite, die nie ein `Board` lädt; `I0022` setzt die Ressource unter dem Board ohnehin voraus. Ein Feld am `Board` wäre eine zweite Quelle für dieselbe Liste. |
| **Die Route `…/klassen`** (wie gezeichnet) | Die Route ist eine Stelle des Bezeichners, nicht der Beschriftung. C06 verlangt eine Schreibweise; ein Artboard ist Entwurf, kein Vertrag. |
| **Ein global eindeutiges Präfix** | Nähme zwei Vorhaben die Freiheit, beide eine WBS-Klasse `WBS-` zu führen — und genau das ist der Normalfall bei mehreren Projektboards. Das Artboard sagt die Board-Eindeutigkeit ausdrücklich. |
| **Das Präfix beim Speichern großschreiben** | Wer `wbs-` tippt, bekäme etwas anderes zurück, als er eingetragen hat. Die Eindeutigkeit braucht den Vergleich ohne Rücksicht auf Schreibweise, nicht die Umschreibung des Werts — dasselbe Verhältnis wie bei `Spaltenbezeichnung`. |
| **Einen Trenner (`-`) automatisch anhängen** | Eine verborgene Regel, die niemand ändern kann. Das Artboard tippt `DOK-` ins Feld und zeigt `WBS-` als Plakette: dieselbe Zeichenkette. Wer `WBS_` will, bekommt es. |
| **Die Länge des Präfix in der Spalte begrenzen** (`TEXT(8)`) | Käme als Datenbankmeldung ohne Kompensationsaktion beim Aufrufer an. Die Grenze gehört in den Validator; das Schema sichert nur die Eindeutigkeit ab. |
| **Den Namen auf Eindeutigkeit prüfen** | Das Artboard zeichnet genau einen Eindeutigkeitsrand, und der nennt das Präfix. Die Identität einer Kartenklasse ist ihr Präfix; die Spalte hält es anders, dort gibt es aber kein zweites, tragendes Merkmal. |
| **Ein eigener Schirm „Klassen verwalten"** | Die WBS kennt keinen solchen Dialog, und eine Kartenklasse gehört zu ihrem Board. Der Layout-Modus ist die Fläche, auf der dieses Board schon gestaltet wird. |
| **Den Bereich in der Arbeitsansicht zeigen** | Die Arbeitsansicht zeigt die Bahnen, das hat `R00004` entschieden. Ein Pflegebereich dort nähme den Karten den Platz („Kanbanflow-dicht"). |
| **Die Anlegezeile `.spaltenpflege-neu` nennen** (wie das Artboard nahelegt) | Die 17 `ToHaveCountAsync`-Zusagen von `LayoutModusE2ETests` zählen unter `#spaltenbahnen` und `#neue-spalte`. Eine geteilte Kennung machte einen grünen Test rot, ohne dass sich an den Spalten etwas geändert hätte. Die **Form** wird übernommen, der **Name** nicht. |
| **Die Kartenklassen alphabetisch sortieren** (wie die Boardliste) | Die Liste ordnete sich beim Anlegen unter der Hand um. Das Artboard führt WBS, Bugmeldungen, Beschaffung — Anlagereihenfolge. |
| **Eine `Position`-Spalte wie bei den Spalten** | Dieser Slice kennt weder Umsortieren noch Entfernen; eine Ordnungszahl, die niemand ändert, wäre tote Flexibilität (C17). `KartenklasseId` ordnet. |
| **`Kartenklasse.Nummer` statt `Zaehlerstand`** | Die Id-Konvention reserviert „Nummer" für Schlüssel. Ein Feld `Nummer` an `Kartenklasse` läse sich wie ein Schlüssel und wäre keiner. |
| **Zwei Features („Klasse anlegen" und „Zurückweisung")** | Ein Fehlerpfad derselben Route, im selben E2E-Lauf belegt, in derselben Komponente sichtbar. Ein Feature, das nur die Interaction wiederholt, ist ein Fehler. |

### Bewusst out of scope

- **Eine Karte einer Klasse zuordnen** und damit eine Nummer vergeben. Das ist `I0021`; dieser Slice erhöht den Zählerstand nie.
- **Karten einer Klasse über die API abrufen.** Das ist `I0022`; die Route erbt das Präfix `…/kartenklassen`.
- **Die Plakette und die Nummer auf der Karte in der Bahn.** Gehört zu `I0021`; `Karte.razor` bleibt unberührt.
- **Ein Klassenfilter auf dem Board.** Im Artboard ausdrücklich nicht gezeichnet und ohne WBS-Knoten.
- **Ändern und Entfernen einer Kartenklasse.** Anders als bei `R00020` (Plattenplatz) und `R00021` (unbemerkter Verfall), wie bei `R00019`: beides ist nicht gezeichnet und hat keinen WBS-Knoten, der Grund dafür fehlt und der Grund dagegen ist stark. Sobald eine Klasse Nummern vergeben hat, hängen Karten daran: ein **Entfernen** nähme veröffentlichten Identitäten ihre Herkunft, ein **Präfixwechsel** ließe `WBS-31` und `DOK-32` in derselben Klasse stehen. Beides ist erst entscheidbar, wenn feststeht, was eine Zuordnung tut — und das ist Gegenstand von `I0021`, wo das Artboard den Klassenwechsel ebenfalls offen lässt (`D0005.dc.html:331`). **Lücke mit Adresse:** wird das Ändern oder Entfernen gewollt, entsteht es als **eigene Interaction unter `D0005`**, **nach** `I0021` und mit `Braucht: I0021` — nicht als Beigabe zu diesem Slice und nicht als Bedienelement ohne Knoten.
- **Kartenklassen im Board-Export.** Gehört zu `I0038`.
- **Live-Aktualisierung der Klassenliste** (ein zweiter Betrachter sieht die neue Klasse ohne Reload). Gehört zu `D0007`.

### Angenommen im stillen Lauf

- **Höchstlänge des Präfix: 8 Zeichen**, Zeichenvorrat `A-Z`, `a-z`, `0-9`, `-`, `_`. Vor `B0296` zu bestätigen.
- **Höchstlänge des Namens** wie bei der Spaltenbezeichnung — der Name steht an derselben Art Stelle.
- **Die Bindung ist board-gebunden**; der scheinbare Gegenbeleg in der WBS ist aufgelöst (siehe oben).
- **Die Beschriftung des Bereichs ist „Klassen"**, die Feldbeschriftungen sind „Name" und „Nummernkreis-Präfix", der Knopf heißt „Klasse anlegen" — Wortlaut nach Artboard.
- **Der Leerzustandssatz lautet „Dieses Board hat keine Klasse."** — Form und Rhythmus von `#keine-spalten`.
- **Die Zeile der Liste lautet „n vergeben · nächste `<Nummer>`"** — Wortlaut nach Artboard, mit „0 vergeben · nächste WBS-01" für eine frische Klasse.
- **Die Zurückweisung lautet „Die Klasse wurde nicht angelegt:"** plus Befundliste, in Form von `.meldung.meldung-abweisung`.
- **Die Befundcodes** lauten `kartenklasse-name-leer`, `kartenklasse-praefix-leer`, `kartenklasse-praefix-ungueltig` und `kartenklasse-praefix-vergeben`; keiner liegt in `Nichtgefunden.AlleCodes` — es sind 400er, keine fehlenden Dinge.
- **Die Begriffe stehen fest (C06):** `Kartenklasse` = der Nummernkreis und der ganze Gegenstand · `Praefix` = die Zeichenkette vor der Zahl · `Zaehlerstand` = wie viele Nummern vergeben sind · `Kartennummer` = die formatierte Nummer aus beidem. **Nicht** `Klasse` (im Code der Programmierbegriff), **nicht** `Nummer` (im Stack für Schlüssel reserviert), **nicht** `Nummernkreis` als Typname (er ist die Sache, nicht das Ding — im Feldnamen der Oberfläche steht er trotzdem, weil das Artboard ihn dort führt).
- **Die Fremdschlüsselspalte heißt `Board`** nach der referenzierten Tabelle — Projektregel.
- **Der Zählerstand liegt in der Migration dieses Slice**, nicht in der von `I0021` — `CREATE TABLE IF NOT EXISTS` wächst nicht nachträglich um eine Spalte.
