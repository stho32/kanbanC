---
id: R00047
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: Projektvorlage
---

# R00047: Behebung der englischen Vorlagen-Fehlerseite im Betrieb

## Beschreibung

Tritt in der Oberfläche ein unbehandelter Fehler auf, sieht der Benutzer die **unveränderte englische Blazor-Projektvorlage**: „Error.", „An error occurred while processing your request.", „Development Mode", „Swapping to **Development** environment will display more detailed information about the error that occurred." Die Oberfläche ist sonst durchgehend deutsch — hier bricht sie mitten im schlechtesten Moment in eine fremde Sprache aus und gibt dem Benutzer einen Rat, den er nicht befolgen kann und nicht befolgen soll.

**Schritte:** Anwendung so starten, wie `CLAUDE.md` es dokumentiert (`dotnet KanbanC.Blazor.dll` — das läuft ohne gesetzte `ASPNETCORE_ENVIRONMENT` in **Production**) · eine Seite aufrufen, deren serverseitiges Rendern eine Ausnahme wirft, die nicht `HttpRequestException` ist.
**Erwartet:** eine deutsche Seite im Bild der Anwendung, die sagt, was der Benutzer jetzt tun kann.
**Tatsächlich:** die englische Vorlagenseite mit der Anleitung, den Server auf `Development` umzustellen.

Zahlt ein auf: [Vision](R00000-vision.md) — „Visuelle Haltung an Kanbanflow orientiert" und „Full-Trust-Modell, Weboberfläche im LAN": die Seite widerspricht der Gestaltungszusage und rät zu einer Umkonfiguration, die die Vision für den Betrieb ausdrücklich nicht vorsieht.

**Zwei Umstände machen den Befund schwerer, als der Text vermuten lässt:**

1. **Die Seite läuft ausschließlich außerhalb von Development.** `Source/KanbanC.Blazor/Program.cs:81-85` hängt `UseExceptionHandler("/Error")` an den Nicht-Development-Zweig. Ein Entwickler bekommt an dieser Stelle die Developer Exception Page und sieht die Vorlagenseite nie; **ein Benutzer sieht sie genau dann, wenn etwas schiefgeht — im Betrieb.**
2. **Kein einziger Test berührt sie**, weil die gesamte Testumgebung in Development fährt (`Source/KanbanC.PlaywrightTests/Infrastructure/Dienstprozess.cs:30` setzt `ASPNETCORE_ENVIRONMENT=Development` für jeden gestarteten Prozess; die Integrationstests setzen es über `WebApplicationFactory`). Belegt in Anmerkung **300** und **301** des Laufs, die Blindstelle selbst schon in Anmerkung **297**.

Derselbe blinde Fleck hat bereits einen echten Produktionsfehler durchgelassen: Anmerkung **296** — `RouteHandlerOptions.ThrowOnBadRequest` war nur in Development `true`, die Anhangsgrenze lieferte im Betrieb einen leeren Rumpf statt eines Befundes; grüne Tests die ganze Zeit. **Der eigentliche Inhalt dieser Anforderung ist deshalb nicht der Text, sondern der Weg, auf dem eine Zusage über den Betrieb prüfbar wird.**

## Ursachenanalyse

### Root Cause

Zwei Ursachen, die zusammen den Befund tragen:

1. **`Source/KanbanC.Blazor/Components/Pages/Error.razor:1-34` ist die unveränderte Projektvorlage.** Sie wurde bei `A6` (Technikwahl, `/erstelle-app`) mitgeliefert und seither nicht angefasst — kein Slice hatte sie im Umfang, weil kein Fertig-Kriterium der WBS auf sie zeigt. Ihr Text ist an eine Entwicklerin gerichtet („setze `ASPNETCORE_ENVIRONMENT` auf `Development` und starte die App neu"), nicht an einen Benutzer.
2. **`Source/KanbanC.PlaywrightTests/Infrastructure/Dienstprozess.cs:30` schließt den Prüfweg.** Jeder E2E-Prozess startet in Development; dort ist der Zweig aus `Program.cs:81-85` gar nicht aktiv. Die Seite ist damit nicht „ungetestet, weil vergessen", sondern **strukturell unerreichbar** für die vorhandene Testumgebung — dieselbe Struktur, die Anmerkung 296 durchgelassen hat.

**Ausgeschlossen wurde:** eine fehlende Lokalisierung (die Anwendung führt keine Ressourcendateien, alle Texte stehen deutsch im Markup — die Vorlagenseite ist die einzige Ausnahme); ein Renderfehler (die Seite rendert korrekt, sie sagt nur das Falsche); ein Routingfehler (`@page "/Error"` ist in jeder Umgebung routbar, siehe *Test-Strategie*).

**Prüfweg, der schon heute offen steht:** `Dienstprozess.Starte` setzt Development in Zeile 30, überschreibt aber in Zeile 37-39 aus dem übergebenen `umgebung`-Wörterbuch — **die Reihenfolge steht so im Code**. Ein Aufrufer kann `["ASPNETCORE_ENVIRONMENT"] = "Production"` mitgeben, ohne die gemeinsame Infrastruktur zu ändern.

### Betroffene Komponenten

- `Source/KanbanC.Blazor/Components/Pages/Error.razor` — der Prüfgegenstand.
- `Source/KanbanC.Blazor/Program.cs:81-85` — der Zweig, der die Seite überhaupt erst erreichbar macht (bleibt unverändert).
- `Source/KanbanC.Blazor/Components/Layout/MainLayout.razor` — die Seite rendert über `Routes.razor` in diesem Layout (kein `@layout` in `Error.razor`, `DefaultLayout="typeof(Layout.MainLayout)"`). Das Layout injiziert `Ereignisverteiler` (Singleton) und trägt die Kopfzeile; es muss auf dem Fehlerweg tragen, sonst wirft die Fehlerseite selbst.
- `Source/KanbanC.PlaywrightTests/Testumgebung.cs`, `Infrastructure/Dienstprozess.cs` — die Testumgebung, die den Betriebszweig bisher nie betritt.
- `Source/KanbanC.Blazor.Tests/Gestaltung/` — der Ort der vorhandenen Quelltextprüfungen.
- `Source/KanbanC.Blazor/wwwroot/gestaltung.css` — Herkunft aller Gestaltungswerte der neuen Seite.

## Lösungsvorschlag

### Langfristige Lösung

**Gewählt: Option B — die Seite neu schreiben und den Prüfweg über einen zweiten Blazor-Prozess in Production öffnen.**

1. **`Error.razor` wird eine Seite dieser Anwendung.** Deutsch, im Bild der Oberfläche, mit den Handlungen, die hier wirklich offenstehen — zurück zur Boardliste, Seite neu laden, und wenn es bleibt: im Protokoll des Prozesses nachsehen. Kein Wort über `ASPNETCORE_ENVIRONMENT`, keine Anleitung zur Umkonfiguration des Servers.
2. **Die `RequestId` bleibt — als „Fehlerkennung", eine Stufe leiser.** Begründung unter *Implementierungshinweise*.
3. **Ein eigener E2E-Prüfstand in Production**: eine Suite startet einen **zweiten** Blazor-Prozess auf einem freien Port mit `ASPNETCORE_ENVIRONMENT=Production`, provoziert eine echte unbehandelte Ausnahme und prüft, dass die deutsche Fehlerseite mit Status 500 erscheint. Der bestehende Lauf bleibt in Development unverändert.
4. **Zusätzlich eine Quelltextprüfung** in `KanbanC.Blazor.Tests/Gestaltung/` nach dem Muster der übrigen Gestaltungsprüfungen: die Abwesenheit des Vorlagentexts lässt sich billig und dauerhaft festnageln, damit kein späterer Vorlagen-Upgrade ihn zurückbringt.

### Alternative Ansätze

- **Option A — nur den Text übersetzen, keine Prüfung.** Verworfen: das behebt den Wortlaut und lässt die Ursache stehen — die Seite bliebe von keinem Test berührt, und der nächste Eingriff könnte sie unbemerkt wieder brechen; genau der Weg, auf dem Anmerkung 296 entstanden ist.
- **Option C — die gesamte Testumgebung auf Production umstellen** (`Dienstprozess.cs:30`). Verworfen: dann läge `UseExceptionHandler` über **jedem** E2E-Test, und ein echter Fehler in irgendeiner Seite erschiene als aufgeräumte deutsche Fehlerseite statt als Stacktrace — die Diagnosefähigkeit der ganzen Suite fiele weg. Dazu verlöre die WebApi `MapOpenApi()` (Anmerkung 301) und die Detailfehler des Kreislaufs. Ein Prüfstand für den Betriebszweig ist richtig, ein Betriebsmodus für alle Tests ist es nicht.
- **Option D — `UseExceptionHandler` in allen Umgebungen aktivieren** und die Seite so in Development prüfbar machen. Verworfen: das nimmt der Entwicklung die Developer Exception Page und kehrt die Voreinstellung des Rahmens um, um einen Testzugang zu gewinnen — Produktionsverhalten wird an den Test angepasst statt umgekehrt.
- **Option E — ein Testendpunkt in `KanbanC.Blazor`, der auf Anfrage wirft.** Verworfen: Produktionscode, den nur der Test braucht, und eine Route, die im Betrieb mitläuft. Die Testumgebung hat mit `Testumgebung.cs` bereits den Weg am Dienst vorbei etabliert (siehe Kommentar zu `Datenbank` dort); dieselbe Haltung gilt hier.

## Test-Strategie

### Unit Test zur Bug-Reproduktion

**`FehlerseiteTests` in `Source/KanbanC.Blazor.Tests/Gestaltung/`** — nach dem Muster von `AnhangabschnittTests` (liest die Datei über `Quelltextbaum.BlazorDatei("Components", "Pages", "Error.razor")`, weil die Ablage der Prüfgegenstand ist).

- *Arrange*: `Error.razor` einlesen.
- *Act/Assert*:
  - `Does.Not.Contain("Development Mode")`, `Does.Not.Contain("ASPNETCORE_ENVIRONMENT")`, `Does.Not.Contain("An error occurred while processing your request")`, `Does.Not.Contain(">Error.<")` — der Vorlagentext ist weg.
  - `Does.Contain("Etwas ist schiefgegangen")` (o. ä. Wortlaut aus den Akzeptanzkriterien) und die deutsche `<PageTitle>` — der neue Text steht da.
  - kein Farb-, Abstands- oder Radius-Literal in der zugehörigen `Error.razor.css` (dieselbe Prüfung wie in `AuswertungsflaecheTests`).

**Dieser Test ist gegen den heutigen Stand rot** — `Error.razor` trägt „Development Mode" wörtlich.

### Code-Extraktion für isoliertes Testing

Nicht nötig. Der Prüfgegenstand ist eine Razor-Seite ohne Fachlogik; ihr `@code`-Block liest nur `Activity.Current?.Id ?? HttpContext?.TraceIdentifier`. Wird die Kennung nicht bloß gezeigt, sondern geformt (etwa gekürzt), gehört diese Formung als kleine Klasse nach `Services/` und bekommt einen eigenen Unit-Test — wie `Dateigroesseform` und `Zeitpunktform` es vormachen.

### Regressionstests

**E2E in Production — der Kern dieser Anforderung.** Eine eigene Suite (`FehlerseiteImBetriebE2ETests`), die nicht am Blazor-Prozess der `Testumgebung` hängt:

1. **Zweiter Prozess**: `Dienstprozess.Starte(...)` mit `["ASPNETCORE_ENVIRONMENT"] = "Production"` im `umgebung`-Wörterbuch. Das genügt bereits — `Dienstprozess.cs:37-39` überschreibt die Vorgabe aus Zeile 30; die gemeinsame Infrastruktur bleibt unverändert. Bereitschaftspfad `/` (`Home.razor` ist statisch und braucht die WebApi nicht). Freier Port über `FreierPort.Ermittle()`.
2. **Auslöser ohne Testhaken im Produktionscode**: `WebApi__BasisAdresse` zeigt auf einen winzigen `HttpListener` der Testklasse, der jede Anfrage mit `200` und `Content-Type: text/html` beantwortet. `BoardApiKlient.LadeAlleBoards` ruft `GetFromJsonAsync` (`BoardApiKlient.cs:21`); bei fremdem Content-Type wirft das **keine** `HttpRequestException` — und `WebApiAufruf.MitAusfallmeldung` fängt ausschließlich `HttpRequestException` (`Services/WebApiAufruf.cs`). Die Ausnahme entkommt beim serverseitigen Rendern von `/boards`, der Rahmen führt `/Error` erneut aus. **Das ist der reale Fall „die Gegenstelle antwortet unerwartet", kein konstruierter.**
3. **Assertions**: Statuscode 500; im Rumpf der deutsche Wortlaut; **nicht** „Development"; die Kopfzeile der Anwendung ist da (das Layout trägt auf dem Fehlerweg).
4. **Gegenprobe (Fault Injection, Skill `test-ehrlichkeit`)**: dieselbe Suite gegen einen Prozess mit `ASPNETCORE_ENVIRONMENT=Development` muss **anders** ausgehen — dort erscheint die Developer Exception Page. Damit ist belegt, dass der Test wirklich den Betriebszweig misst und nicht durchgereicht wird.

**Bestandsschutz**: alle vorhandenen E2E-Suiten laufen unverändert in Development weiter; `Testumgebung` behält ihren einen Blazor-Prozess. `[assembly: Parallelizable(ParallelScope.None)]` gilt bereits, der zweite Prozess kollidiert also mit keinem laufenden Test.

## Akzeptanzkriterien

### Der Text

- [ ] Der Reproduktionstest (`FehlerseiteTests`) ist gegen den heutigen Stand rot und nach der Änderung grün
- [ ] `Error.razor` enthält keinen der Vorlagensätze mehr: kein „Error.", kein „An error occurred while processing your request", kein „Development Mode", kein „Swapping to Development environment", kein „ASPNETCORE_ENVIRONMENT"
- [ ] Die Seite ist vollständig deutsch, mit echten Umlauten (C07), und nennt in einem Satz, was geschehen ist — ohne Fachjargon und ohne Stacktrace
- [ ] Die Seite nennt mindestens zwei Handlungen, die der Benutzer **an diesem Rechner** ausführen kann: zurück zur Boardliste (`<a href="/boards">`) und die Seite neu laden. Kein Hinweis, der eine Umkonfiguration des Servers verlangt
- [ ] `<PageTitle>` ist deutsch
- [ ] Die Fehlerkennung erscheint als deutsch beschriftete Nebenzeile mit einem Satz, wozu sie gut ist (siehe *Implementierungshinweise*); sie steht nicht vor dem, was der Benutzer tun kann

### Die Gestaltung

- [ ] Kein Farb-, Abstands-, Radius- oder Schriftliteral in der Seite oder ihrer `.razor.css` — alle Werte kommen aus `gestaltung.css` (geprüft wie in `AuswertungsflaecheTests`)
- [ ] Keine Bootstrap-Klasse und kein neues CSS-Framework
- [ ] Die Seite rendert im `MainLayout` mit der Kopfzeile der Anwendung

### Die Prüfbarkeit — der Kern

- [ ] Es gibt eine E2E-Suite, die einen Blazor-Prozess mit `ASPNETCORE_ENVIRONMENT=Production` startet und darin eine **echte unbehandelte Ausnahme** auslöst
- [ ] Diese Suite belegt: Statuscode **500** und die deutsche Fehlerseite im Rumpf; das Wort „Development" kommt im Rumpf nicht vor
- [ ] Die Fault Injection ist gefahren und dokumentiert: derselbe Ablauf gegen einen Development-Prozess liefert **nicht** die Fehlerseite, sondern die Developer Exception Page — der Test misst also den Betriebszweig
- [ ] Der Auslöser braucht **keinen** Testhaken in `KanbanC.Blazor`: kein neuer Endpunkt, keine neue Route, kein Schalter, der nur für den Test existiert
- [ ] `Dienstprozess.cs:30` bleibt unverändert; die Umgebung wird über das `umgebung`-Wörterbuch übergeben
- [ ] Alle bestehenden E2E-Suiten laufen weiter in Development und bleiben ohne Änderung grün

### Keine Regression

- [ ] Alle Tests aller Ebenen sind grün, `TreatWarningsAsErrors` bleibt erfüllt
- [ ] `KanbanC.Blazor` hat weiterhin keine Projektreferenz auf `KanbanC.BL`
- [ ] Der Zweig in `Program.cs:81-85` bleibt inhaltlich unverändert — insbesondere wird `UseExceptionHandler` **nicht** nach Development gezogen

## Implementierungshinweise

### Entscheidung zur `RequestId`: bleibt, als „Fehlerkennung", eine Stufe leiser

**Behalten.** Drei Gründe, die in diesem System zusammenfallen:

1. **Benutzer und Betreiber sind dieselbe Person.** Die Vision setzt lokalen Betrieb im LAN, Full-Trust, keine Accounts — es gibt keinen Support-Desk, an den man eine Kennung meldet, aber es gibt das Konsolenprotokoll des Prozesses, den derselbe Mensch selbst gestartet hat. Genau dort steht die Ausnahme, und die Kennung ist die einzige Brücke vom Bild zur Protokollzeile.
2. **Sie ist die einzige handlungsfähige Information auf der Seite.** Wenn der Fehler nach dem Neuladen bleibt, ist „schau im Protokoll nach dieser Kennung" die einzige verbleibende Handlung — und diese Anforderung will gerade sagen, was ein Mensch hier tun kann.
3. **Die Vision nennt Agenten als gleichberechtigte Akteure**, und die Hausregel für Fehlerantworten lautet: Grund nennen **und** die Kompensationsaktion. Eine Kennung, unter der sich das Ereignis wiederfinden lässt, ist die Kompensation, die auf einer Fehlerseite überhaupt möglich ist.

**Ballast ist nicht die Kennung, sondern ihre Prominenz.** In der Vorlage steht sie über allem anderen. In der neuen Seite steht sie **nach** den Handlungen, deutsch beschriftet (`Fehlerkennung`), in `--font-mono`, mit einem Satz, wozu sie gut ist. Die Bedingung `ShowRequestId` bleibt: ist keine Kennung da, entfällt der Block ganz — eine leere Zeile mit Beschriftung wäre schlechter als keine.

### Der Fehlerweg muss selbst tragen

Die Seite wird nach einer Ausnahme im Rahmen erneut ausgeführt (`createScopeForErrors: true`). Alles, was darauf rendert, muss ohne die WebApi und ohne intakten Kreislauf funktionieren:

- **Keine interaktive Komponente auf der Seite.** Der Rückweg ist ein einfaches `<a href="/boards">`, kein `NavLink` mit Ereignisbindung und kein `@onclick` — eine zweite Ausnahme auf der Fehlerseite führte zu einer nackten 500 ohne jeden Text.
- **Kein API-Aufruf** in `OnInitialized`. Die Seite weiß nichts und soll nichts wissen.
- **`MainLayout` bleibt das Layout** (über `Routes.razor`), muss dabei aber ohne Fehler durchlaufen: es injiziert den `Ereignisverteiler` (Singleton, existiert auch im Fehler-Scope) und rendert die Kopfzeile. Das ist im E2E-Test in Production mitzuprüfen — die Kopfzeile muss auf der Fehlerseite erscheinen.
- **Zwei Fehlerflächen nicht verwechseln.** `MainLayout` trägt bereits `#blazor-error-ui` („Ein unerwarteter Fehler ist aufgetreten." / „Neu laden") für den Ausfall des interaktiven Kreislaufs. `/Error` ist die Fläche für Ausnahmen beim serverseitigen Rendern. Die Wortwahl der neuen Seite sollte zur bestehenden Leiste passen, ohne sie zu doppeln.

### Der Prüfstand

- Der zweite Prozess braucht ein eigenes Assembly-Pfad-Stück; `Testumgebung.Assembly(...)` ist heute privat. Entweder die neue Suite baut den Pfad selbst (wie `Testumgebung` es tut), oder `Testumgebung` bekommt eine öffentliche Methode dafür. Beides ist Testinfrastruktur, kein Produktionscode.
- `appsettings.Development.json` der Blazor-App unterscheidet sich inhaltlich nicht von `appsettings.json` (nur Logging, identisch) — der Wechsel nach Production kostet konfigurationsseitig nichts.
- **HSTS ist im Production-Prozess kein Hindernis**: die Middleware setzt den Header nur auf HTTPS-Anfragen; der Prüfstand fährt wie die ganze Suite über `http://127.0.0.1`.
- **Kosten des Prüfstands**: ein zusätzlicher `dotnet`-Prozessstart je Lauf (Bereitschaftsabfrage alle 200 ms, Frist 60 s — in der Praxis wenige Sekunden), ein zusätzlicher freier Port, ein `HttpListener` in der Testklasse. Kein Eingriff in `Dienstprozess`, keine Änderung an bestehenden Tests.

### Befund, bewusst nicht im Umfang: HSTS

`app.UseHsts()` (`Program.cs:85`) hängt am selben Nicht-Development-Zweig und ist ebenso von keinem Test berührt — der Befund ist echt und gehört genannt (Anmerkung 300). **Er gehört aber nicht in diese Anforderung:**

- HSTS wirkt ausschließlich über HTTPS; KanbanC ist als HTTP-Dienst auf 5180/5280 dokumentiert und lauscht auf `0.0.0.0` im LAN. Im dokumentierten Betriebsmodus hat der Aufruf **keine beobachtbare Wirkung** — es gibt nichts, was ein Test hier festhalten könnte, außer der Abwesenheit eines Headers.
- Ob KanbanC je über HTTPS läuft, ist eine offene Frage an die Vision („Bleibt es lokal?"), keine Frage der Fehlerseite. Sie hier mitzuentscheiden hieße, eine Transportentscheidung in eine Text- und Bedienbarkeitskorrektur zu schmuggeln.

Verwandt und ebenfalls außerhalb: `CircuitOptions.DetailedErrors`, die Developer Exception Page und `MapOpenApi()` nur in Development (Anmerkung 301). Der letzte Punkt ist ausdrücklich **keine** Blindstelle, sondern eine offene Entscheidung — die Vision sieht Agenten als gleichberechtigte Akteure, und `/openapi/v1.json` fehlt im Betrieb.

Ebenfalls nicht im Umfang, aber beim Lesen aufgefallen: **`Source/KanbanC.Blazor/Components/Pages/NotFound.razor` trägt denselben Vorlagenzustand** („Not Found" / „Sorry, the content you are looking for does not exist."). Anders als `/Error` läuft diese Seite in **allen** Umgebungen (`Program.cs:87` steht außerhalb des Zweigs) und ist damit schon heute ohne neuen Prüfstand testbar — eine andere Fehlerklasse mit einer anderen Lösung. Gehört in eine eigene Anforderung.

## Offene Fragen

- **Wortlaut der Überschrift.** Vorgeschlagen: „Etwas ist schiefgegangen." als `<h1>`, darunter ein Satz „Die Seite konnte nicht angezeigt werden. Der Vorgang wurde abgebrochen; an Ihren Daten hat sich nichts geändert." — der zweite Halbsatz ist eine Zusage, die belastbar sein muss (der Fehler tritt beim Rendern auf, nachdem ein etwaiger Schreibvorgang schon durch war). *Angenommen, bis widersprochen: der Zusatz über die Daten bleibt weg — was der Benutzer sieht, deckt nicht, was die WebApi bereits geschrieben hat.*
- **Soll die Seite den Weg ins Protokoll benennen** (Konsolenausgabe des Blazor-Prozesses) oder nur die Kennung zeigen? *Angenommen: einen Satz dazu, weil der Benutzer hier der Betreiber ist.*
- **Bekommt die Fehlerseite ein Artboard in `Dokumentation/Wireframes/`?** Die acht Schirme decken sie nicht ab, und sie gehört zu keinem WBS-Dialog. *Angenommen: nein — sie folgt den Tokens und der Haltung, ohne eigene Skizze; ist das falsch, gehört ein Artboard vor die Umsetzung.*

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist nicht die falsche Sprache, sondern **die Kombination aus einer Seite, die nur im Betrieb erscheint, und einer Testumgebung, die den Betrieb nie betritt** — dieselbe Kombination, die Anmerkung 296 einen echten Produktionsfehler durchgehen ließ. Übersetzt man nur den Text, bleibt die Kausalkette intakt: die nächste umgebungsabhängige Zusage fällt genauso durch. Baut man dagegen den Prüfstand — einen zweiten Blazor-Prozess in Production, angestoßen über das `umgebung`-Wörterbuch, das `Dienstprozess.cs:37-39` ohnehin schon durchreicht —, dann wird der Betriebszweig zum ersten Mal von einem Test berührt; und weil der Auslöser eine echte, im Klientcode belegte Ausnahmelücke ist (`WebApiAufruf` fängt nur `HttpRequestException`), misst der Test den wirklichen Weg und nicht eine gestellte Kulisse. Daraus folgt: die deutsche Seite ist ab dem Fix nicht bloß geschrieben, sondern **bewacht** — ein Vorlagen-Upgrade, das den englischen Text zurückbrächte, wird rot, und zwar auf beiden Ebenen (Quelltextprüfung und E2E in Production). Der Hebel liegt genau hier und nicht vorgelagert (die Vorlage lässt sich nicht abstellen) und nicht nachgelagert (ein Review fängt eine Datei nicht, die seit `A6` niemand ansieht).

## Missing-Docs

- **Verhalten von `UseExceptionHandler` beim serverseitigen Rendern einer Blazor-Seite mit `InteractiveServer`-Rendermodus**: Dass eine Ausnahme im Prerender die Middleware erreicht (und nicht die Kreislauf-Fehlerleiste), ist hier aus dem Aufbau geschlossen, nicht aus einer Quelle belegt. Der erste Testlauf des Prüfstands ist zugleich die Probe darauf (Skill `dependency-probe`) — geht sie anders aus, braucht der Auslöser eine andere Form.
- **Welche Ausnahmen `GetFromJsonAsync` bei fremdem Content-Type genau wirft** (`NotSupportedException` gegen `JsonException`): für den Auslöser genügt „nicht `HttpRequestException`", der genaue Typ ist unbelegt und wird im Probe-Schritt festgestellt.

## Notizen

- **Priorität: Hoch.** Der Text ist kosmetisch, die Blindstelle ist es nicht — sie hat nachweislich schon einen Produktionsfehler getragen (Anmerkung 296).
- **Betroffene Nutzer/Systeme:** jeder Benutzer der Oberfläche im dokumentierten Betrieb (`dotnet KanbanC.Blazor.dll` ohne gesetzte Umgebungsvariable). Entwicklung ist nicht betroffen und war deshalb blind.
- **Workaround:** keiner. Die Seite lässt sich im Betrieb nicht umgehen; wer die Vorlagenanleitung befolgt und auf `Development` umstellt, öffnet die Developer Exception Page im LAN — den Rat gibt die Vorlage, und er widerspricht dem Betriebsmodell.
- **Kein WBS-Knoten.** Bug-Anforderung; der Fortschritt der WBS bleibt unberührt.
- **Herkunft:** Blazor-Projektvorlage, unverändert seit `A6`. Belege: Anmerkungen 296, 297, 300, 301 in `kanbanC-anmerkungen.md`.
