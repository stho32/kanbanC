---
id: R00048
status: Neu
datum: 2026-09-08
---

# R00048: Umgebungsabhängige Zusagen in ihrer Umgebung prüfen

## Beschreibung

Die gesamte Testumgebung von KanbanC läuft in `Development`; das ausgelieferte KanbanC läuft in `Production`. Jede Zusage, die an der Umgebung hängt, ist damit von keinem Test berührt — und ein Fehler dieser Klasse ist bereits unbemerkt bis in den Betrieb durchgerutscht. Diese Anforderung schließt die Blindstelle **als Klasse**: sie nimmt die umgebungsabhängigen Stellen beider Prozesse vollständig auf, gibt jeder einen von drei erlaubten Ausgängen, und stellt einen Wächter daneben, der schlägt, sobald eine neue Stelle hinzukommt, die keinen Ausgang hat.

Sie behebt **keinen** einzelnen Befund. Sie sorgt dafür, dass solche Befunde gefunden werden.

Zahlt ein auf: [Vision](R00000-vision.md) — „Was die Weboberfläche kann, kann auch die API"; eine Zusage, die nur in der Testumgebung gilt, ist für den Agenten im Betrieb keine.

### Der Befund

- **Testumgebung = `Development`, durchgehend.** `Source/KanbanC.PlaywrightTests/Infrastructure/Dienstprozess.cs:30` setzt `ASPNETCORE_ENVIRONMENT=Development` für **jeden** E2E-Prozess; `WebApplicationFactory` stellt es für die Integrationstests von sich aus ein (`Source/KanbanC.WebApi.IntegrationTests/Infrastructure/TestWebApi.cs:18` — Vorgabewert `Development`). Auch der in `README.md:47-50` dokumentierte Entwicklerstart `dotnet run --project …` fährt Development, weil beide `launchSettings.json` es setzen.
- **Ausgeliefert wird `Production`.** `.github/workflows/release.yml:113-128` publiziert beide Projekte, die beigelegte `START.md` startet sie als `./webapi/KanbanC.WebApi` und `./blazor/KanbanC.Blazor` — ohne `ASPNETCORE_ENVIRONMENT`, und `launchSettings.json` reist im Publish nicht mit. **Damit gibt es heute keinen einzigen Weg im Repository, der die Anwendung so fährt, wie sie ausgeliefert wird.**
- **Der bereits durchgerutschte Fehler.** `RouteHandlerOptions.ThrowOnBadRequest` ist vom Rahmen **nur in Development** auf `true` gesetzt. Die Anhangs-Obergrenze aus `R00020` hing daran: nur wenn die Formularbindung wirft, fängt der `Anhangsgrenzenwaechter` und übersetzt in `anhang-zu-gross` samt Byte-Angabe. Im Betrieb warf sie nicht — **400 mit leerem Rumpf, kein Befund**, während die gesamte Testsuite grün war. A/B gemessen: Production 400/0 Bytes, Development 400/262 Bytes (`kanbanC-anmerkungen.md`, Anmerkungen 296 und 299). Aufgefallen ist es erst, als jemand die Anwendung von Hand startete; behoben mit `Source/KanbanC.WebApi/Program.cs:51`.
- **Zwei weitere Stellen hängen heute am selben Zweig und sind von keinem Test berührt**: `UseExceptionHandler("/Error")` und `UseHsts()` in `Source/KanbanC.Blazor/Program.cs:81-85` (Anmerkung 300), dazu die Rahmenvorgaben aus Anmerkung 301.

## Geschäftlicher Nutzen

Der Fehler von `R00020` war nicht schwer zu finden — er war **unmöglich** zu finden, solange kein Test die Anwendung so fährt, wie sie läuft. Gefunden hat ihn ein Mensch, der die Anwendung zufällig von Hand startete. Das ist der teuerste denkbare Fundort und der unzuverlässigste.

Der Wert liegt nicht darin, drei bekannte Stellen abzudecken — den Anteil hätte man auch mit drei Zeilen Handarbeit. Er liegt darin, dass die **nächste** umgebungsabhängige Stelle, die noch niemand geschrieben hat, nicht mehr still an der Prüfung vorbeikommt: es gibt eine Liste, die Liste hat einen Wächter, und der Wächter ist ein Test. Das ist derselbe Mechanismus, mit dem der Routen-Vertragstest verhindert, dass ein neuer Endpunkt ungeprüft entsteht (`TestWebApi.cs:38-53`) — hier auf die Umgebungsweiche angewandt.

Für den Agenten als gleichberechtigten Akteur ist das keine Kür: er sieht ausschließlich den Betrieb. Eine Fehlerantwort, die nur in Development einen Grund nennt, ist für ihn eine wortlose Abweisung.

## Bestandsaufnahme — die umgebungsabhängigen Stellen

Erhoben am 2026-09-08 über beide `Program.cs`, die Test- und Startwege und die Rahmenvorgaben aus Anmerkung 301. **Diese Liste ist Anlage der Anforderung, nicht ihr Ergebnis** — das Ergebnis ist die geführte Liste im Code (Akzeptanzkriterien, Gruppe 1), die hiermit beginnt.

### Gruppe A — ausdrückliche Verzweigung im Produktionscode (im Quelltext sichtbar)

| # | Stelle | Zweig | Zusage, die daran hängt | Heute von einem Test berührt? |
|---|---|---|---|---|
| A1 | `Source/KanbanC.WebApi/Program.cs:94-97` — `app.MapOpenApi()` | nur `Development` | `/openapi/v1.json` beschreibt die API. `README.md:59` führt sie als „(nur Development)" | **nein** — im Betrieb fehlt die Route |
| A2 | `Source/KanbanC.Blazor/Program.cs:81,83` — `UseExceptionHandler("/Error", createScopeForErrors: true)` | nur **außerhalb** `Development` | Ein unbehandelter Fehler führt auf eine Seite statt auf einen Abbruch | **nein** (Anmerkung 300) |
| A3 | `Source/KanbanC.Blazor/Program.cs:81,85` — `UseHsts()` | nur **außerhalb** `Development` | HSTS-Kopf | **nein** (Anmerkung 300) |

### Gruppe B — Rahmenvorgaben, die an der Umgebung hängen, ohne dass jemand sie setzt

Die gefährlichere Hälfte: im Quelltext steht kein `IsDevelopment()`, es gibt nichts zu greppen, und die Stelle sieht umgebungsunabhängig aus.

| # | Vorgabe | Verhalten | Zusage, die daran hängt | Stand |
|---|---|---|---|---|
| B1 | `RouteHandlerOptions.ThrowOnBadRequest` | Rahmen setzt `true` nur in `Development` | 400 **mit** Befund `anhang-zu-gross` statt leerem Rumpf | **war blind — der durchgerutschte Fehler.** Heute ausdrücklich gesetzt (`WebApi/Program.cs:51`) und in Production geprüft (`AnhangEndpunkteTests.cs:354`) |
| B2 | Developer Exception Page (`WebApplication` fügt sie nur in `Development` ein) | in `Production` bleibt die WebApi **ohne jeden** Ausnahme-Handler — sie ruft `UseExceptionHandler` nirgends | Fehlervertrag aus `R00007`: eine Fehlerantwort nennt Grund und Kompensation | **blind.** Belegter Fall: `POST /api/karten/{id}/teilaufgaben` ohne `text` wirft `NullReferenceException` (Anmerkung 303) |
| B3 | `CircuitOptions.DetailedErrors` / `RazorComponentsServiceOptions.DetailedErrors` | nicht gesetzt; ob und wie sie an der Umgebung hängen, ist **nicht belegt** | Was der Benutzer bei einem Komponentenfehler zu sehen bekommt | **blind und unvermessen** (Anmerkung 301) |
| B4 | Auflösung der Static Web Assets | `dotnet <dll>` aus dem Projektverzeichnis liefert die fingerprinted Assets **nur** mit `ASPNETCORE_ENVIRONMENT=Development`, sonst HTTP 500 | Die Oberfläche lädt überhaupt | belegt in `R00001-board-anlegen.md:224` und Anmerkung 33. **Zugleich der Grund, warum sich die E2E-Ebene nicht einfach umstellen lässt** — ohne `dotnet publish` gibt es keinen Production-Lauf der Oberfläche |

### Gruppe C — Umgebungsweichen außerhalb des Produktionscodes

| # | Stelle | Fährt |
|---|---|---|
| C1 | `Source/KanbanC.PlaywrightTests/Infrastructure/Dienstprozess.cs:30` | `Development` — **jeder** E2E-Prozess |
| C2 | `Source/KanbanC.WebApi.IntegrationTests/Infrastructure/TestWebApi.cs:18` (Vorgabewert) | `Development` — jeder Integrationstest, der die Umgebung nicht ausdrücklich nennt |
| C3 | `Source/KanbanC.Blazor/Properties/launchSettings.json:10` und `Source/KanbanC.WebApi/Properties/launchSettings.json:10` | `Development` — der Entwicklerstart aus `README.md:47-50` |
| C4 | `.github/workflows/release.yml:113-128` + beigelegte `START.md` | **`Production`** — das ausgelieferte KanbanC, ohne gesetzte Variable |
| C5 | `appsettings.Development.json` in beiden Projekten | heute **inhaltsgleich** mit der Basisdatei (nur `Logging`) — keine Konfigurationsdrift, die Weiche ist trotzdem scharf: ein hier ergänzter Schlüssel gälte für jeden Testlauf und für nichts sonst |

### Negativbefunde — geprüft, hängen **nicht** an der Umgebung

- `app.UseStatusCodePagesWithReExecute("/not-found", …)` (`Blazor/Program.cs:87`) ist unbedingt registriert — die 404-Seite gilt in beiden Umgebungen.
- `Anhangsgrenzenwaechter.Registriere(app)` (`WebApi/Program.cs:92`) ist unbedingt registriert; umgebungsabhängig war **nicht** er, sondern seine Voraussetzung B1.
- `UseHttpsRedirection` gibt es in keinem der beiden Prozesse. Damit ist auch A3 in dieser Betriebsform folgenlos: `UseHsts` setzt den Kopf nur auf HTTPS-Antworten, und `CLAUDE.md` legt den Betrieb über `http://` im LAN fest.

**Zahl der Stellen: 3 + 4 + 5 = 12, davon 5 blind (A1, A2, A3, B2, B3), 1 vormals blind und heute geschlossen (B1).**

## Funktionale Anforderungen

- Eine geführte Liste der umgebungsabhängigen Stellen liegt im Code und trägt je Zeile genau einen von drei Ausgängen: **abgesichert** (ein Test in `Production` beweist die Zusage), **entschieden** (die Stelle gilt bewusst nur in einer Umgebung, ohne Zusage an die andere), **Befund** (die Zusage hält nicht — mit benannter Adresse: bestehende Anforderung oder `/anforderung aus-bug`).
- Ein Wächtertest hält die Liste gegen den Quelltext beider `Program.cs`: eine ausdrückliche Umgebungsverzweigung, die nicht in der Liste steht, macht ihn rot.
- Eine **Betriebsprobe** fährt beide Prozesse aus einem `dotnet publish`-Stand ohne gesetzte Umgebungsvariable — also in `Production`, so wie `START.md` es beschreibt — und prüft die Zusagen, die nur dort gelten.
- Zusagen der WebApi, die an der Umgebung hängen, werden auf der Integrationsebene in `Production` geprüft (`new TestWebApi(pfad, TestWebApi.Produktion)`), nicht in der Betriebsprobe — die Ebene ist billiger und existiert bereits.
- Die Betriebsprobe läuft im Gate `abschluss` als eigener, benennbarer Block; ihre gemessene Laufzeit wird beim ersten Lauf festgehalten.
- Kein Produktionscode wird geändert, um die Prüfbarkeit herzustellen — außer dort, wo eine Rahmenvorgabe aus Gruppe B ausdrücklich gesetzt wird, damit die Umgebung für sie keine Rolle mehr spielt.

## Nicht-funktionale Anforderungen

- **Laufzeit:** Die Betriebsprobe kostet im Gate höchstens 3 Minuten inklusive `dotnet publish` beider Projekte. Zum Vergleich: ein zweiter vollständiger E2E-Lauf kostet **13:53** (541 Tests, Anmerkung 687) und wäre damit die teuerste Einzelmaßnahme im Gate.
- **Ports:** Die Betriebsprobe startet nie auf 5280/5180, sondern auf ermittelten freien Ports (Skill `freier-port`, bestehendes Bauteil `FreierPort.Ermittle()`).
- **Rückstandsfreiheit:** Der publizierte Stand und die von ihm angelegte Datenbankdatei liegen unter einem temporären Ordner und werden nach dem Lauf gelöscht — wie `Testumgebung.LoescheDatenbank` es für den E2E-Lauf tut.

## Akzeptanzkriterien

### Gruppe 1 — Die Liste

- [ ] Die geführte Liste liegt im Code und enthält beim Abschluss **mindestens die 12 Stellen** aus der Bestandsaufnahme (A1–A3, B1–B4, C1–C5), je mit Datei und Zeile.
- [ ] Jede Zeile trägt genau einen Ausgang aus `abgesichert` / `entschieden` / `Befund`. Eine Zeile ohne Ausgang gibt es nicht; ein `Befund` ohne benannte Adresse (Anforderungsnummer oder ausdrücklich angelegte Bug-Anforderung) zählt als Zeile ohne Ausgang.
- [ ] Die Liste nennt für jede Zeile mit Ausgang `abgesichert` den Test, der sie deckt — Datei und Testname.

### Gruppe 2 — Der Wächter (das Kriterium, das die Klasse schließt)

- [ ] Ein Test liest `Source/KanbanC.WebApi/Program.cs` und `Source/KanbanC.Blazor/Program.cs` und findet jede ausdrückliche Umgebungsverzweigung (`IsDevelopment`, `IsProduction`, `IsStaging`, `IsEnvironment`, `EnvironmentName`). Steht eine gefundene Stelle nicht in der Liste, ist der Test **rot**.
- [ ] Fault Injection belegt den Wächter: eine testweise eingefügte vierte Verzweigung macht ihn rot, ihre Entfernung wieder grün. Ohne diesen Nachweis gilt der Wächter als nicht vorhanden (Skill `test-ehrlichkeit`).
- [ ] Der Wächter deckt Gruppe B **nicht** ab und behauptet es auch nicht — er trägt einen Kommentar, der sagt, warum: eine Rahmenvorgabe steht nicht im Quelltext. Für Gruppe B ist die Liste selbst der Träger, und ihre Pflege gehört in die Bestandsaufnahme jedes Slices, der eine neue Rahmenvorgabe berührt.

### Gruppe 3 — Prüfung in der Umgebung, in der die Zusage gilt

- [ ] Für jede Zeile mit Ausgang `abgesichert` läuft der deckende Test in **derselben** Umgebung, in der die Zusage gilt — nicht in `Development` mit einer Annahme darüber. Prüfbar: der Test nennt die Umgebung ausdrücklich (`TestWebApi.Produktion` bzw. der Betriebsstand), keine Zeile verlässt sich auf einen Vorgabewert.
- [ ] `A1` ist entschieden: entweder liefert `/openapi/v1.json` auch in `Production` eine Beschreibung — dann belegt es ein Test in Production — oder das Fehlen ist als bewusste Grenze festgehalten, **mit einem Satz zur Vision** („Agenten als gleichberechtigte Akteure"), weil `README.md:59` es heute nur als Klammerbemerkung führt.
- [ ] `A2` ist geprüft, soweit ohne Testhaken im Produktionscode möglich: die Betriebsprobe belegt in `Production`, dass `/Error` erreichbar ist und die Seite ausliefert, die die Anwendung dort zeigen will. **Der Inhalt der Seite gehört zu `R00047`, nicht hierher** — dieses Kriterium prüft, dass die Seite im Betrieb überhaupt steht.
- [ ] `A3` ist entschieden statt grün gemacht: die Betriebsprobe belegt, dass über `http://` **kein** HSTS-Kopf kommt, und die Liste hält fest, dass `UseHsts()` in der festgelegten Betriebsform (`CLAUDE.md`, LAN über `http://`, kein `UseHttpsRedirection`) folgenlos ist. Ein Kriterium, das den Kopf fordert, wäre in dieser Betriebsform nicht erfüllbar.
- [ ] `B1` bleibt bewiesen: der bestehende Production-Test zur Anhangs-Obergrenze (`AnhangEndpunkteTests.cs:354`) steht unverändert grün und ist in der Liste als Deckung von B1 eingetragen.
- [ ] `B2` ist vermessen: die Antwort der WebApi auf eine unbehandelte Ausnahme ist in `Production` gemessen (Statuscode und Rumpfgröße in Bytes, nach dem Muster der A/B-Messung aus Anmerkung 302) und in der Liste als `Befund` mit dieser Messung eingetragen. **Die Behebung ist nicht Gegenstand dieser Anforderung** — der Befund bekommt eine Adresse.
- [ ] `B3` ist vermessen statt vermutet: ob `DetailedErrors` an der Umgebung hängt, wird an der laufenden Anwendung festgestellt und mit dem Messergebnis in die Liste geschrieben — auch das Ergebnis „hängt nicht daran" ist ein zulässiger Ausgang (`entschieden`).
- [ ] `B4` ist in der Liste als Voraussetzung der Betriebsprobe eingetragen: dass die Oberfläche ihre fingerprinted Assets liefert, ist der erste Prüfpunkt der Probe — schlägt er fehl, steht der publizierte Stand falsch, nicht die Anwendung.

### Gruppe 4 — Die Betriebsprobe

- [ ] Die Probe publiziert beide Projekte in einen temporären Ordner und startet sie als `dotnet <ordner>/KanbanC.WebApi.dll` bzw. `…/KanbanC.Blazor.dll` **ohne** gesetztes `ASPNETCORE_ENVIRONMENT`; ein Test belegt, dass der laufende Prozess tatsächlich `Production` meldet — sonst prüfte die Probe still wieder Development.
- [ ] Beide Prozesse laufen auf über `FreierPort.Ermittle()` ermittelten Ports; 5280 und 5180 kommen im Testcode nicht vor.
- [ ] Nach dem Lauf sind temporärer Ordner, Datenbankdatei und Anhang-Ablageordner gelöscht; ein zweiter Lauf hintereinander ist grün (kein Rückstand, der den nächsten Lauf trägt oder stört).
- [ ] Die Probe hängt **nicht** an der `[SetUpFixture] Testumgebung` der E2E-Ebene: sie startet keine zweiten Development-Prozesse. Prüfbar daran, dass ihr Namensraum nicht unterhalb von `KanbanC.PlaywrightTests` liegt.

### Gruppe 5 — Preis und Gate

- [ ] Die Laufzeit der Betriebsprobe ist **gemessen** und im Dokumentationsteil festgehalten — eine Zahl aus einem Lauf, keine Schätzung.
- [ ] Die gemessene Laufzeit liegt unter 3 Minuten. Liegt sie darüber, ist das ein Befund mit Zahl, kein stilles Hinnehmen: dann wird entschieden, ob die Probe im Gate bleibt oder auf die WebApi-Hälfte schrumpft.
- [ ] Der Gesamtlauf des Gates bleibt in derselben Größenordnung: die Zeit für Unit, Integration und E2E ändert sich nicht messbar, weil die Probe eine eigene, zusätzliche Einheit ist und keine bestehende umbaut.
- [ ] Alle heute grünen Tests bleiben grün — insbesondere die 541 E2E-Tests, die weiterhin in `Development` laufen. Diese Anforderung stellt die E2E-Ebene **nicht** um.

## Betroffene Verzeichnisstruktur

- **Betriebsprobe und Wächter** liegen in `Source/KanbanC.PlaywrightTests/`, aber im Namensraum `KanbanC.Betriebsprobe` — ein Geschwister, kein Kind von `KanbanC.PlaywrightTests`. Grund: NUnits `[SetUpFixture]` wirkt auf den eigenen Namensraum **und alle darunter**; ein Kind bekäme `Testumgebung` mitsamt ihren beiden Development-Prozessen aufgezwungen. Als Geschwister im selben Assembly nutzt die Probe `Infrastructure/FreierPort.cs` und `Infrastructure/Dienstprozess.cs` direkt weiter, ohne neues Projekt und ohne kopierte Datei.
- **Production-Prüfungen der WebApi** liegen bei ihrem Gegenstand in `Source/KanbanC.WebApi.IntegrationTests/Api/` — dort, wo `AnhangEndpunkteTests.cs:354` das Muster schon vorgibt.
- **Die geführte Liste** liegt als Testdatei im Namensraum der Betriebsprobe, nicht als Markdown: eine Liste, die kein Test liest, veraltet unbemerkt — genau die Krankheit, gegen die diese Anforderung geschrieben ist.
- **Kein Produktionscode** wird angefasst, außer dort, wo Gruppe 3 eine Rahmenvorgabe ausdrücklich setzen lässt (`Source/KanbanC.WebApi/Program.cs`, `Source/KanbanC.Blazor/Program.cs`).

## Technische Überlegungen

### Ablauf

1. **Bestandsaufnahme in Code gießen**
   - 1.1 Die 12 Zeilen aus der Bestandsaufnahme als Datenstruktur anlegen, je mit Datei, Zeile, Zusage und Ausgang.
   - 1.2 Zeilen ohne belegten Ausgang zunächst als `Befund` führen — die Liste ist von der ersten Minute an vollständig, auch wenn noch nichts abgesichert ist.
2. **Wächter bauen und mit Fault Injection belegen**
   - 2.1 Beide `Program.cs` einlesen, Umgebungsverzweigungen finden.
   - 2.2 Gefundene Menge gegen die Liste halten; Differenz in beide Richtungen melden (neue Stelle **und** verwaiste Listenzeile).
   - 2.3 Vierte Verzweigung testweise einfügen → rot; entfernen → grün. Ergebnis festhalten.
3. **Betriebsstand herstellen**
   - 3.1 `dotnet publish` beider Projekte in einen temporären Ordner, in der Konfiguration des laufenden Testlaufs.
   - 3.2 Zwei freie Ports ermitteln, Verbindungszeichenfolge und `WebApi:BasisAdresse` über `__`-Variablen setzen — wie `Testumgebung` es tut.
   - 3.3 `Dienstprozess.Starte` aufrufen und `ASPNETCORE_ENVIRONMENT` im Umgebungswörterbuch auf `Production` setzen. Die Zeile 30 setzt Development als Vorgabe, die Schleife in Zeile 37-40 überschreibt sie — **der Weg existiert schon, er wurde nur nie gegangen.**
   - 3.4 Ersten Prüfpunkt fahren: liefert die Oberfläche ihre Assets? (B4)
   - 3.5 Belegen, dass die Prozesse `Production` melden — sonst prüft die Probe wieder Development.
4. **Die Zusagen abklopfen** — je Zeile der Liste ein Prüfpunkt oder eine ausdrückliche Entscheidung: A1 (`/openapi/v1.json`), A2 (`/Error` erreichbar), A3 (kein HSTS-Kopf über `http://`), B2 (Ausnahme-Antwort messen), B3 (`DetailedErrors` vermessen).
5. **Aufräumen** — Prozesse beenden, temporären Ordner samt Datenbankdatei und `-Files`-Ordner löschen.
6. **Laufzeit messen und eintragen**; Gate-Block benennen.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** das Gate `abschluss` (eigener, gefilterter Testblock); `Dienstprozess.Starte` als bereits vorhandener Prozessstarter mit überschreibbarer Umgebung; `TestWebApi.Produktion` als bereits vorhandener Weg auf der Integrationsebene.

- `Umgebungsabhaengigestelle` (DTO, immutable) — eine Zeile der geführten Liste: Kennung, Datei, Zeile, Zusage, Ausgang, deckender Test.
- `Umgebungsabhaengigestellen` (benannte Collection) — beantwortet: welche Stellen sind ohne Ausgang, welche Kennungen deckt kein Test, welche Dateien sind betroffen.
  - `Umgebungsabhaengigestellen OhneAusgang()`
  - `IReadOnlyList<string> Kennungen()`
- `Umgebungsweichenleser` (Operation) — liest eine `Program.cs` und gibt die gefundenen Umgebungsverzweigungen als Zeilennummern zurück. Reine Textarbeit, keine Seiteneffekte außer dem Lesen.
  - `IReadOnlyList<int> Finde(string quelltext)`
- `UmgebungsweichenWaechterTests` (Testfixture) — hält die gefundenen Weichen gegen die Liste, in beide Richtungen.
- `Betriebsstand` (Integration, `IDisposable`) — publiziert beide Projekte in einen temporären Ordner, startet sie in `Production` auf freien Ports, räumt am Ende auf. Nutzt `Dienstprozess` und `FreierPort` unverändert.
  - `static Task<Betriebsstand> Starte()`
  - `string BlazorAdresse { get; }` · `string WebApiAdresse { get; }`
- `BetriebsprobeTests` (Testfixture, `[OneTimeSetUp]` auf `Betriebsstand`) — die Prüfpunkte aus Schritt 4.

### Änderungen an bestehenden Klassen

- `Source/KanbanC.PlaywrightTests/Infrastructure/Dienstprozess.cs` — Zeile 30 bleibt inhaltlich, bekommt aber den Kommentar, dass sie eine **Vorgabe** ist, die der Aufrufer über das Umgebungswörterbuch überschreibt. Heute liest sich die Zeile wie eine Festlegung; genau diese Lesart hat die Blindstelle so lange gehalten.
- `Source/KanbanC.WebApi/Program.cs` und `Source/KanbanC.Blazor/Program.cs` — nur, soweit Gruppe 3 eine Rahmenvorgabe aus Gruppe B ausdrücklich setzen lässt. Jede solche Zeile wird damit vom unsichtbaren B-Fall zum sichtbaren A-Fall und fällt unter den Wächter.

## Tests

Nach Skill `test-pyramide`. Diese Anforderung erzeugt fast nur Testcode — die Ebenenverteilung ist trotzdem zu benennen, weil sie hier der Gegenstand ist.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Umgebungsweichenleser` — reine Textarbeit auf einem übergebenen Quelltext, ohne Datei- oder Prozesszugriff. Die Fälle: Verzweigung gefunden, negierte Verzweigung gefunden, `IsEnvironment("…")` gefunden, Vorkommen im Kommentar **nicht** als Weiche gezählt.
- `Umgebungsabhaengigestellen` — Zeilen ohne Ausgang, verwaiste Kennungen.

**Integration:** die Production-Varianten der umgebungsabhängigen WebApi-Zusagen über `new TestWebApi(pfad, TestWebApi.Produktion)` — dieselbe Bauform wie `AnhangEndpunkteTests.cs:354`. Hierher gehört auch die Messung zu B2.

**E2E / Betriebsprobe:** die Prüfpunkte gegen den publizierten Stand in `Production` (A1, A2, A3, B3, B4). Sie sind formal E2E, laufen aber als eigener Block und ohne Browser, wo eine HTTP-Anfrage reicht.

**Wächter:** `UmgebungsweichenWaechterTests` liest den Quelltext des Repositories — kein Unit-Test im engeren Sinn, sondern ein Vertragstest wie der Routen-Test in `TestWebApi.cs:38-53`. Er wird nur dann als vorhanden verbucht, wenn die Fault Injection aus Kriterium Gruppe 2 gefahren wurde.

## Abhängigkeiten

- Abhängig von: nichts. Die Anforderung braucht keinen anderen Slice und blockiert keinen.
- Grenzt ab gegen [R00047](R00047-englische-fehlerseite-im-betrieb.md) (deutsche Fehlerseite): `R00047` macht die Seite richtig, `R00048` sorgt dafür, dass ihre Umgebung überhaupt geprüft wird. Fällt `R00047` weg, bleibt `R00048` vollständig sinnvoll — und umgekehrt.
- Erzeugt voraussichtlich eine Bug-Anforderung für B2 (`NullReferenceException` in `Teilaufgabentext`, Anmerkung 303) — als Adresse eines Befunds, nicht als Teil des Umfangs.
- Diese Anforderung bekommt **keinen WBS-Knoten**: sie ist keine Interaction der Application, sondern eine Prüfzusage über den Bestand.

## Offene Fragen

- Keine offen. Zwei Fragen wurden beim Schreiben durch Nachsehen im Bestand beantwortet statt gestellt: (a) ob die E2E-Prozesse überhaupt in `Production` startbar sind — ja, `Dienstprozess.cs:37-40` überschreibt die Vorgabe, aber der publizierte Stand ist wegen B4 Voraussetzung; (b) ob es schon einen Production-Test gibt — ja, `AnhangEndpunkteTests.cs:354`, und `TestWebApi.Produktion` ist bereits der dafür gebaute Weg.

## Manuelle Vorbereitungstätigkeiten

- Keine.

## Manuelle Nachbereitungstätigkeiten

- Die gemessene Laufzeit der Betriebsprobe in den Gate-Ablauf des Projekts übernehmen, damit sie beim nächsten `abschluss` nicht neu geschätzt wird.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt war konkret: eine Zusage aus `R00020` — die Anhangs-Obergrenze antwortet mit einem Befund statt wortlos — galt in `Development` und brach in `Production`, und **die gesamte Testsuite war dabei grün** (Anmerkung 296). Der Auslöser war nicht ein übersehener Fall, sondern eine strukturelle Lücke: kein Weg im Repository fährt die Anwendung so, wie sie ausgeliefert wird (C1–C4).

Die Wirkungskette: Wenn eine geführte Liste die umgebungsabhängigen Stellen benennt **und** ein Wächtertest sie gegen den Quelltext hält (X), dann kann eine neue Verzweigung nicht mehr entstehen, ohne dass jemand ihr einen Ausgang geben muss (Y) — und weil einer der drei Ausgänge „in `Production` geprüft" heißt und die Betriebsprobe diesen Ausgang billig macht, ist der bequemste Weg für den Nächsten der richtige (Z). Ohne den Wächter deckt jede Prüfung nur die heute bekannten Stellen ab und die Klasse bleibt offen; ohne die Betriebsprobe hätte der Wächter keinen bezahlbaren Ausgang und würde umgangen.

Der Hebel sitzt genau hier und nicht früher oder später: **früher** wäre „keine Umgebungsverzweigungen mehr schreiben" — das geht für `ThrowOnBadRequest` (und ist geschehen), nicht aber für die Developer Exception Page, deren Umgebungsabhängigkeit gewollt ist. **Später** wäre „im Betrieb beobachten" — genau das ist passiert, und es hat einen Menschen gebraucht, der die Anwendung zufällig von Hand startete.

## Missing-Docs

- **Welche ASP.NET-Core-Vorgaben an `IHostEnvironment` hängen, ohne im Anwendungscode aufzutauchen**, ist nirgends als Liste dokumentiert — weder in der Architektur-Vorlage unter `.claude/app-architectures/dotnet-server-side-blazor/` noch in einer greifbaren Übersicht des Rahmens. Gruppe B dieser Anforderung ist deshalb aus Vorfällen zusammengetragen (Anmerkungen 296, 301, 33), nicht aus einer Quelle abgelesen. Sie ist mit Sicherheit unvollständig, und der Wächter kann sie nicht schließen. Eine belegte Liste gehörte nach `Dokumentation/Bibliotheken/missing-docs.md`.
- **`CircuitOptions.DetailedErrors` und `RazorComponentsServiceOptions.DetailedErrors`**: ob und wie ihre Vorgabewerte an der Umgebung hängen, konnte im Bestand nicht belegt werden — deshalb steht B3 als „unvermessen" und nicht als Behauptung in der Liste.

## Notizen

### Verworfene Alternativen

- **Zweiter vollständiger E2E-Lauf in `Production`** — der gründlichste Weg, aber der Preis ist belegt: **13:53 für 541 Tests** (Anmerkung 687), plus `dotnet publish` beider Projekte, und in schlechteren Läufen zerfiel dieselbe Suite in vier Blöcke von 2:43 bis 7:56 (Anmerkung 644). Ein Gate, das dadurch von rund 14 auf rund 28 Minuten wächst, wird umgangen — und er deckte kein Prozent mehr ab als die Betriebsprobe: 528 der 541 Tests prüfen Zusagen, die von der Umgebung überhaupt nicht abhängen.
- **Nur eine Liste umgebungsabhängiger Stellen, bei jeder Änderung von Hand geprüft** — kostenlos und wertlos. Genau diese Prüfung ist bei `R00020` nicht passiert, obwohl der Anlass bekannt war; und Grün ohne Test widerspricht der Projektregel („Grün wird ein Knoten erst, wenn ein Test es beweist"). Als Teil der Lösung bleibt die Liste erhalten — aber mit einem Test daneben, der sie gegen den Code hält.
- **Umgebungsabhängigkeit ganz abschaffen** (jede Vorgabe ausdrücklich setzen, kein `IsDevelopment()` mehr) — trägt für `ThrowOnBadRequest` und ist dort bereits geschehen, trägt aber nicht durch: die Developer Exception Page und die detaillierten Kreislauf-Fehler sollen in Development anders sein, und `UseHsts` gehört nicht in einen Entwicklerlauf. Übernommen wird der brauchbare Teil: wo eine Rahmenvorgabe **still** an der Umgebung hängt (Gruppe B), wird sie ausdrücklich gesetzt — dann sieht der Wächter sie.
- **Eigenes Testprojekt `Source/KanbanC.Betriebsprobe`** — sauberer im Zuschnitt, aber es wäre die fünfte bewusste Abweichung von der Architektur-Vorlage und müsste `FreierPort` und `Dienstprozess` entweder kopieren oder über eine Projektreferenz auf ein Testprojekt holen. Der Geschwister-Namensraum im bestehenden E2E-Projekt kostet nichts und erreicht dasselbe.

### Bewusst außerhalb des Umfangs

- Die **deutsche Fehlerseite** — [R00047](R00047-englische-fehlerseite-im-betrieb.md). Diese Anforderung prüft, dass `/Error` im Betrieb steht; was darauf steht, entscheidet `R00047`.
- Die **Behebung** der Befunde B2 und B3. Sie bekommen eine Adresse, keinen Fix.
- Die **Umstellung der E2E-Ebene**. Sie bleibt in `Development`, und das ist richtig: dort wird die Fachlichkeit geprüft, nicht die Betriebsform.
