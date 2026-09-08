---
id: R00049
status: Neu
datum: 2026-09-08
ursprung: Refactoring
ursprungslauf: R00024
---

# R00049: Kein Fehler auf dem Weg zur WebApi verschwindet lautlos

## Zusammenfassung

`WebApiAufruf.MitAusfallmeldung` ist das einzige Netz unter **48 Aufrufstellen** der Oberfläche und fängt genau **einen** Ausnahmetyp. Diese Anforderung ersetzt die Typprüfung durch eine benannte **Klassifikation** — Ausfall, fachlicher Befund, Programmfehler — so dass kein Fehler auf dem Weg zur WebApi mehr ohne Spur endet und keiner in einer Falschaussage endet.

Zahlt ein auf: [Vision](R00000-vision.md) — „An jeder Karte und jeder Zeit ist ablesbar, wer oder was gehandelt hat." Eine Handlung, die stumm scheitert, ist nicht ablesbar; und eine, die mit „Die WebApi ist nicht erreichbar" scheitert, obwohl die WebApi geantwortet hat, ist falsch ablesbar.

## Ausgangssituation

```csharp
// Source/KanbanC.Blazor/Services/WebApiAufruf.cs:5-16
public static async Task<string?> MitAusfallmeldung(Func<Task> aufruf)
{
    try
    {
        await aufruf();
        return null;
    }
    catch (HttpRequestException)
    {
        return WebApiAusfall.Meldung;
    }
}
```

**Fundstellen**

| Was | Wo |
|---|---|
| Der eine Fangpunkt | `Source/KanbanC.Blazor/Services/WebApiAufruf.cs:12` |
| Die eine Meldung | `Source/KanbanC.Blazor/Services/WebApiAusfall.cs:5` |
| Die drei Tests | `Source/KanbanC.Blazor.Tests/Services/WebApiAufrufTests.cs:8`, `:23`, `:31` |
| Der belegte Vorfall | [`R00024`](R00024-stiller-anhangverlust-beim-zweiten-ablegen.md), Root Cause 2 |
| Die Anmerkung des Laufs | `kanbanC-anmerkungen.md`, Nr. 377 |

**Der Befund hat einen Datenverlust mitgetragen.** Bei `R00024` (Anhang beim zweiten Ablegen verloren) riss der Dateistrom aus dem Blazor-Kreislauf ab. Die Ausnahme war **keine** `HttpRequestException` — sie fiel durch, `_ausfallmeldung` blieb `null`, und der Diagnoselauf protokollierte `Fehlermeldung=(keine) Zurueckweisung=(keine) Ausnahmeanzeige=False`. Der Datenverlust selbst ist behoben (Sperre der Ablegefläche, eigener Fangpunkt am Anhängen); **die verschluckende Stelle steht unverändert.** `R00024` hat sie ausdrücklich nicht angefasst — diese Anforderung ist die Einlösung dieser Zurückstellung.

### Die 48 Aufrufstellen und was sie mit dem Ergebnis tun

`await WebApiAufruf.MitAusfallmeldung(...)` steht **48-mal** im Produktivcode, verteilt auf **14 Razor-Dateien** (dazu vier Aufrufe in Tests und ein Verweis im Kommentar `ZeitenApiKlient.cs:77`):

| Zielvariable | Anzahl | Wirkung |
|---|---|---|
| `_ausfallmeldung` | 29 | wird als Meldung gerendert |
| `_fehlermeldung` | 6 | wird als Meldung gerendert |
| lokale `var ausfallmeldung` | 5 | wird geprüft und weitergereicht |
| `_zugFehlermeldung` | 4 | wird als Meldung gerendert |
| `_kartenzahlFehlermeldung` | 1 | wird als Meldung gerendert |
| `_importausfall` | 1 | wird als Meldung gerendert |
| **Rückgabe verworfen** | **2** | `Kopfzeile.razor:165`, `Kopfzeile.razor:273` |

Die dichteste Stelle ist `Kartendetail.razor` mit 21 Aufrufen, gefolgt von `Auswertungen.razor` (6) und `Board.razor` (5).

**Die zwei Stellen ohne Zuweisung sind ein zweiter, vom Fangbereich unabhängiger Verlustweg**: dort wird selbst die `HttpRequestException`, die heute korrekt gefangen wird, wortlos weggeworfen — die Kopfzeile lädt die laufenden Timer bzw. die Kontributoren nach und sagt nicht, wenn es misslingt.

### Was heute durchfällt — und wohin es geht

Kein Aufruf des Bestands endet in `MitAusfallmeldung`; er läuft weiter durch den Blazor-Kreislauf. Zwei Ausgänge, beide falsch:

- **Der Kreislauf stirbt.** Eine durchgelassene Ausnahme aus einem Ereignisbehandler beendet den Circuit; sichtbar wird `#blazor-error-ui` (`MainLayout.razor:13`) — ein rotes Band ohne Grund, ohne Kompensationsaktion, und die Seite ist danach tot.
- **Es passiert gar nichts.** Wird die Ausnahme nicht am `await` geworfen, sondern auf einem Nebenpfad des Kreislaufs (Interop-Rückruf am Dateistrom, wie bei `R00024`), erreicht sie diesen `try` nie. Genau das war der stille Verlust.

Erreichbar an den 48 Stellen sind mindestens:

| Ausnahme | Woher | Heute |
|---|---|---|
| `HttpRequestException` | Verbindung weg, `EnsureSuccessStatusCode` bei 5xx/409/401 | **gefangen** |
| `TaskCanceledException` (innen `TimeoutException`) | Zeitüberschreitung des `HttpClient` | fällt durch |
| `IOException` | Abriss mitten im Antwortstrom | fällt durch |
| `JsonException` | unlesbarer Antwortrumpf, `ReadFromJsonAsync` | fällt durch |
| `NotSupportedException` | Antwort mit fremdem `Content-Type` | fällt durch |
| `InvalidOperationException` „Die WebApi hat keine verwertbare Antwort zurückgegeben." | `ApiAntwortleser.cs:58` | fällt durch |
| `JSException` / `JSDisconnectedException` | Browserkanal, Dateistrom | fällt durch |
| `InvalidOperationException` aus `ApiErgebnis.cs:34`/`:47` | Programmfehler: Wert eines zurückgewiesenen Ergebnisses gelesen | fällt durch — **richtig so** |
| `NullReferenceException`, `ArgumentException` | Programmfehler im Rumpf des Lambdas | fällt durch — **richtig so** |

### Das Haus kennt die Antwort schon — an vier Stellen

Ein aufgeweitetes `catch` mit **benannter Klassifikation** ist in diesem Projekt kein neues Muster, sondern das vorherrschende:

```csharp
// Source/KanbanC.Blazor/Services/Ereignisleitung.cs:64
catch (Exception abriss) when (abriss is HttpRequestException or IOException or JsonException or OperationCanceledException)

// Source/KanbanC.Blazor/Services/Pfadkopie.cs:38 und :58, Identitaetsspeicher.cs:43 und :79
catch (Exception fehler) when (IstBrowserausfall(fehler))
```

`WebApiAufruf` ist die **einzige** Stelle der Oberfläche, die noch nach nacktem Typ fängt. Die Frage „darf der Fangbereich wachsen?" ist im Bestand also längst mit Ja beantwortet — nur nicht hier.

### Was `WebApiAufrufTests.cs` wirklich zusichert

`R00024` hat `catch (Exception)` verworfen mit der Begründung, das breche `WebApiAufrufTests.cs:29`. Die Begründung ist zu prüfen, nicht zu wiederholen. Geprüft:

```csharp
// Source/KanbanC.Blazor.Tests/Services/WebApiAufrufTests.cs:30-36
[Test]
public void Wenn_eine_andere_Ausnahme_fliegt_dann_wird_sie_nicht_verschluckt()
{
    Assert.That(
        async () => await WebApiAufruf.MitAusfallmeldung(() => throw new InvalidOperationException("Fehler im Rumpf")),
        Throws.InstanceOf<InvalidOperationException>());
}
```

- **Der Name sagt die Zusage:** ein Programmfehler wird nicht verschluckt.
- **Die Zusicherung prüft den Mechanismus:** genau `InvalidOperationException` verlässt den Helfer.
- **Der Testname ist weiter als die Zusicherung.** „Eine andere Ausnahme" behauptet eine Aussage über alle Typen; belegt ist einer. Die Aussage, die dieser Test **nicht** trifft und die dem Haus fehlt: eine `JsonException` wird heute genauso durchgelassen — nur ist das kein Schutz, sondern der Fehler.
- **Die eigentliche Zusage ist eine Verbotszusage:** *Ein Programmfehler wird nicht in „Die WebApi ist nicht erreichbar" verwandelt.* Das ist die Zusage, die trägt — sie schützt vor der Falschaussage, nicht vor dem Fangen an sich.

**Die Zusicherung bleibt erhalten, wenn der Fangbereich wächst — und zwar wörtlich, ohne eine Zeile am Test zu ändern.** `InvalidOperationException` steht in keiner der beiden Klassen, die eine Meldung verdienen; es propagiert weiter, `Throws.InstanceOf<InvalidOperationException>()` bleibt grün. `R00024`s Begründung galt für die dort betrachtete Variante `catch (Exception)` **ohne** Klassifikation — für die stimmt sie. Für ein `when`-gefiltertes `catch` stimmt sie nicht.

**Es bleibt eine echte Kollision, und sie ist der Kern der Arbeit:** `InvalidOperationException` bedeutet an zwei Stellen zweierlei.

| Herkunft | Bedeutung | Gehört zu |
|---|---|---|
| `ApiAntwortleser.cs:58` „Die WebApi hat keine verwertbare Antwort zurückgegeben." | Die API hat geantwortet, die Antwort trägt nichts | fachlicher Befund |
| `ApiErgebnis.cs:34`/`:47` „Ein zurückgewiesenes Ergebnis hat keinen Wert." | Der Aufrufer hat das Ergebnis falsch benutzt | Programmfehler |

Der CLR-Typ kann die beiden nicht trennen. Deshalb bekommt der Vertragsbruch der API einen **eigenen Typ**; ohne das wäre jede Klassifikation entweder zu eng (der Vertragsbruch fällt weiter durch) oder zu weit (der Programmfehler wird verschluckt und `WebApiAufrufTests.cs:31` bricht).

### Der zweite Weg: `ApiAntwortleser` — passen die Lesarten zusammen?

Geprüft, mit Zählung:

| Lesart | Nutzung | Verhalten bei 404 |
|---|---|---|
| `AlsErgebnis` (`ApiAntwortleser.cs:32`) | **13 Aufrufe** in 6 Klienten | ersetzt den Befund der API durch den Sammelsatz `BoardOderSpalteVerschwunden` (`:8-13`) |
| `AlsErgebnisMitGemeldetenBefunden` (`:18`) | **4 Aufrufe** in `AuswertungenApiKlient` | behält den Befund der API |

**Befund: Es wachsen zwei Muster nebeneinander, und der ältere ist derselbe Fehler eine Schicht höher.** Die WebApi liefert zu **jedem** 404 eine `Zurueckweisung` mit Grund und Kompensationsaktion (`Zurueckweisungen.AlsNichtgefunden`, `Nichtgefunden.Board`/`.Spalte`); `AlsErgebnis` wirft diese Auskunft weg und setzt einen Satz dagegen, der weder sagt, **welches** Ding fehlt, noch, wo es herkommt. `AlsErgebnisMitGemeldetenBefunden` ist aus `R00036` entstanden, weil genau das beim Soll-Ist-Vergleich nicht mehr trug — die Begründung im Kommentar (`:15-17`) ist aber nicht routen-, sondern vertragsspezifisch und gilt damit für jede Route.

**Konsequenz für diese Anforderung — dreifach:**

1. Der Fehlerpfad des Aufrufs (`WebApiAufruf`) muss dieselbe Form annehmen wie der Fehlerpfad der Antwort (`Zurueckweisung`): **Grund plus Kompensationsaktion**, nicht nur ein Satz. Sonst entsteht ein drittes Muster.
2. Der Zusammenschluss der beiden Lesarten von `ApiAntwortleser` gehört **nicht** hierher: er ändert das Verhalten von 13 Aufrufstellen in 6 Klienten und braucht seine eigene Anforderung (siehe `## Notizen`).
3. `AlsErgebnis` und `AlsErgebnisMitGemeldetenBefunden` bleiben in dieser Anforderung **unverändert** — bis auf den Ausnahmetyp aus `Wert<T>` (`:58`), der zur Klassifikation gehört.

## Ziel

Nach dieser Anforderung gilt:

- **`WebApiAufruf` klassifiziert, statt einen Typ zu prüfen.** Drei Klassen, benannt, jede in einer eigenen puren Operation entschieden und ohne Browser prüfbar:
  - **Ausfall** — die WebApi kam nicht zu Wort: `HttpRequestException`, `TaskCanceledException`/`TimeoutException`, `IOException`, `SocketException`. Ergebnis: `WebApiAusfall.Meldung`, unverändert.
  - **Fachlicher Befund** — die WebApi kam zu Wort, das Ergebnis ist trotzdem nicht verwertbar: `JsonException`, `NotSupportedException`, der Vertragsbruch aus `ApiAntwortleser`, ein abgerissener Browserkanal (`JSException`, `JSDisconnectedException`). Ergebnis: eine **eigene** Meldung, die den Grund nennt und die Kompensationsaktion — nie `WebApiAusfall.Meldung`, denn die wäre hier eine Falschaussage.
  - **Programmfehler** — alles übrige: propagiert unverändert. Kein `catch`, keine freundliche Meldung.
- **`WebApiAufrufTests.cs` bleibt Zeile für Zeile unverändert und grün** — alle drei Tests, insbesondere `:31`.
- **Die 48 Aufrufstellen bleiben unverändert.** Signatur und Rückgabetyp `Task<string?>` bleiben; was sich ändert, ist ausschließlich, **wie oft** ein Text statt `null` zurückkommt und **welcher**. Ausnahme sind die zwei Stellen, die die Rückgabe wegwerfen (Schritt 8).
- **Der Fehlerpfad steht neben der abgesicherten Operation** (C24) und die Klassifikation trägt keinen ungenutzten Zweig (C16).

## Mikro-Refactoring-Schritte

IDE-Befehle für Rider (`Ctrl+T, Ctrl+M` = Extract Method, `Shift+F6` = Rename, `F12`/`Alt+F7` = Find Usages); die ReSharper/Visual-Studio-Entsprechungen stehen in Klammern. Nach **jedem** Schritt: `dotnet build Source/KanbanC.sln` und `dotnet test Source/KanbanC.Blazor.Tests`.

Ein Commit je Schritt nach Skill `trunk-commits`, Betreff `[R00049] <Schrittbeschreibung>`.

---

### Schritt 1: Den heutigen Fangbereich als benanntes Prädikat ausdrücken

**Tastenkombination**: Cursor auf `HttpRequestException` in `WebApiAufruf.cs:12` → `Ctrl+T, Ctrl+M` (ReSharper: `Ctrl+R, M`)
**Aktion**: Extract Method — reines Umschreiben ohne Verhaltensänderung.
**Details**: `catch (HttpRequestException)` wird zu `catch (Exception fehler) when (IstWebApiAusfall(fehler))` mit `private static bool IstWebApiAusfall(Exception fehler) => fehler is HttpRequestException;` — wörtlich dieselbe Menge, ausgedrückt in der Form, die `Pfadkopie.cs:46` und `Ereignisleitung.cs:64` schon verwenden. Kein Ternary, keine `=>`-Kurzform im Rumpf (C19): explizites `return`.
**Verifizierung**: Build grün; `dotnet test Source/KanbanC.Blazor.Tests --filter WebApiAufrufTests` — alle drei Tests grün, keiner geändert. Das ist der Beweis, dass Form und Verhalten hier unabhängig sind.

### Schritt 2: Den Vertragsbruch der API von einem Programmfehler unterscheidbar machen

**Tastenkombination**: `Shift+Alt+C` (neue Klasse) in `Source/KanbanC.Blazor/Services/`
**Aktion**: Manueller Mikro-Schritt — eigener Ausnahmetyp.
**Details**: `WebApiAntwortException : Exception` anlegen. In `ApiAntwortleser.cs:58` das `throw new InvalidOperationException("Die WebApi hat keine verwertbare Antwort zurückgegeben.")` auf den neuen Typ umstellen. **Nur diese eine Stelle** — `ApiErgebnis.cs:34` und `:47` bleiben `InvalidOperationException`, denn sie *sind* Programmfehler.
**Verifizierung**: `Alt+F7` (ReSharper: `Shift+F12`) auf `InvalidOperationException` in `ApiAntwortleser.cs` zeigt keinen Treffer mehr; `dotnet test Source/KanbanC.Blazor.Tests` grün. Verhalten unverändert: der neue Typ fällt weiterhin durch.

### Schritt 3: Den roten Test schreiben

**Tastenkombination**: `Alt+Einfg` in `WebApiAufrufTests.cs` (Generate)
**Aktion**: Manueller Mikro-Schritt — der Test, der vor der Behebung rot ist. **Dieser Schritt lässt die Suite bewusst rot**; er ist der einzige.
**Details**: Neue Datei `Source/KanbanC.Blazor.Tests/Services/WebApiAufrufKlassifikationTests.cs`, damit `WebApiAufrufTests.cs` unangetastet bleibt. Drei Tests:
- `Wenn_die_Antwort_der_WebApi_unlesbar_ist_dann_kommt_eine_eigene_Meldung` — Lambda wirft `JsonException`, erwartet: nicht `null`, **und** nicht `WebApiAusfall.Meldung`.
- `Wenn_die_WebApi_keine_verwertbare_Antwort_gibt_dann_kommt_eine_eigene_Meldung` — Lambda wirft `WebApiAntwortException`, gleiche Erwartung.
- `Wenn_die_Uhr_ablaeuft_dann_kommt_die_Ausfallmeldung` — Lambda wirft `TaskCanceledException` mit innerer `TimeoutException`, erwartet: `WebApiAusfall.Meldung`.
**Verifizierung**: `dotnet test Source/KanbanC.Blazor.Tests --filter WebApiAufrufKlassifikationTests` — **alle drei rot**, und zwar mit der Ursache „Ausnahme nicht gefangen", nicht mit einer falschen Meldung. Wer hier nur zwei rote Tests sieht, hat einen davon zu schwach formuliert (Skill `test-ehrlichkeit`).

### Schritt 4: Die Meldung des fachlichen Befunds als Operation anlegen

**Tastenkombination**: `Shift+Alt+C` in `Source/KanbanC.Blazor/Services/`
**Aktion**: Manueller Mikro-Schritt — Text neben `WebApiAusfall`, Muster `WebApiAusfall.cs`/`Anhangausfall`.
**Details**: `WebApiAntwortstoerung` mit `public const string Meldung = "…";`. Der Text nennt **Grund** („Die WebApi hat geantwortet, die Antwort war nicht verwertbar.") und **Kompensationsaktion** („Bitte die Seite neu laden und den Vorgang wiederholen.") — dieselbe Form, die `Fehlerbefund` für die API vorschreibt. Er unterscheidet sich hörbar von `WebApiAusfall.Meldung`.
**Verifizierung**: Build grün, Suite unverändert (Schritt 3 weiter rot). Der Text wird noch von niemandem benutzt — das ist in diesem einen Zwischenschritt in Ordnung und in Schritt 5 aufgelöst.

### Schritt 5: Die Klassifikation einziehen

**Tastenkombination**: `Ctrl+T, Ctrl+M` in `WebApiAufruf.cs`
**Aktion**: Der **eine Schritt, der Verhalten ändert** — er ist der Zweck dieser Anforderung, siehe `## Warum löst diese Anforderung das Problem?`.
**Details**: Neben `IstWebApiAusfall` (Schritt 1) entsteht `IstAntwortstoerung`. Der Rumpf bekommt einen zweiten `catch`-Block, direkt unter dem ersten (C24: Fehlerpfad neben der Operation):
- `IstWebApiAusfall`: `fehler is HttpRequestException or IOException or SocketException` **plus** `TaskCanceledException`/`OperationCanceledException`, deren `InnerException` eine `TimeoutException` ist. Eine Abbruchausnahme **ohne** diese innere Ursache ist ein regulärer Abbruch und wird **nicht** gefangen — sonst würde `Board.razor:576`/`Kartendetail.razor:2176` untergraben.
- `IstAntwortstoerung`: `fehler is JsonException or NotSupportedException or WebApiAntwortException or JSException or JSDisconnectedException`.
- Beide Prädikate mit benannter Bedingungsvariable statt zusammengesetztem Ausdruck im `return` (C13).
- **Kein dritter Zweig** für Typen ohne belegte Herkunft (C16).
**Verifizierung**: `dotnet test Source/KanbanC.Blazor.Tests` — Schritt 3 wird **grün**, `WebApiAufrufTests.cs` bleibt grün, **ohne dass eine seiner Zeilen angefasst wurde**. Genau das ist der Beleg, dass die Zusage von `:31` haltbar war. Anschließend `dotnet run ~/.claude/tools/csharp-stil-check.cs Source/KanbanC.Blazor/Services/WebApiAufruf.cs` — keine FEHLER-Befunde.

### Schritt 6: `InvalidOperationException` gegen Rückfall absichern

**Tastenkombination**: `Alt+Einfg` in `WebApiAufrufKlassifikationTests.cs`
**Aktion**: Manueller Mikro-Schritt — Regressionsnetz.
**Details**: `Wenn_ein_Ergebnis_falsch_benutzt_wird_dann_wird_der_Programmfehler_nicht_verschluckt` — das Lambda liest `ApiErgebnis<string>.Zurueckgewiesen(...).Wert` und erwartet `Throws.InstanceOf<InvalidOperationException>()`. Damit hängt die Zusage nicht mehr an einem künstlich geworfenen Typ, sondern am echten Weg aus `ApiErgebnis.cs:34`.
**Verifizierung**: grün. Wer später `InvalidOperationException` in `IstAntwortstoerung` aufnimmt, sieht **zwei** rote Tests.

### Schritt 7: Den Kommentar an die Klassifikation binden

**Tastenkombination**: —
**Aktion**: Manueller Mikro-Schritt.
**Details**: Über `IstAntwortstoerung` ein **fachliches WHY** in zwei Zeilen (C19): warum eine geantwortete, aber unverwertbare Antwort nicht „nicht erreichbar" heißen darf. Kein WHAT-Kommentar, keine Entstehungsgeschichte — die gehört in die Commit-Message (C26).
**Verifizierung**: `/review csharp` auf `WebApiAufruf.cs` meldet zu C19 nichts.

### Schritt 8: Die zwei Stellen schließen, die die Meldung wegwerfen

**Tastenkombination**: `Ctrl+Alt+V` (Introduce Variable; ReSharper: `Ctrl+R, V`) auf `Kopfzeile.razor:165` und `:273`
**Aktion**: Introduce Variable, dann Zuweisung an das vorhandene Meldungsfeld der Kopfzeile.
**Details**: Beide Aufrufe verwerfen heute die Rückgabe. `:151` in derselben Datei zeigt das richtige Muster (`var ausfallmeldung = await …`). Die Kopfzeile bekommt **kein neues** Bedienelement: die Meldung geht dorthin, wo `:151` sie schon hinlegt.
**Verifizierung**: `dotnet build`; `grep -rn "^\s*await WebApiAufruf.MitAusfallmeldung" Source/KanbanC.Blazor` liefert **null Treffer** — die maschinelle Fassung des Kriteriums „kein Aufruf wirft seine Meldung weg".

### Schritt 9: Aufräumen und Gegenprobe

**Tastenkombination**: `Ctrl+Alt+O` (Optimize Usings), `Alt+Entf` (Safe Delete) für Übriggebliebenes
**Aktion**: Safe Delete, Optimize Usings.
**Details**: Ungenutzte `using`, tote Hilfsmethoden entfernen (C16). Dann `/review csharp` über die geänderten Dateien.
**Verifizierung**: `/implementierung abschluss R00049` — Build ohne Warnung (`TreatWarningsAsErrors`), alle Testebenen grün, Coverage von `KanbanC.Blazor.Tests` über `Services/` nicht gesunken.

## Verifikation

- [ ] `dotnet build Source/KanbanC.sln` ohne Warnung nach **jedem** Schritt (`TreatWarningsAsErrors` ist aktiv).
- [ ] `dotnet test Source/KanbanC.Blazor.Tests` grün nach jedem Schritt außer Schritt 3 (dort drei rote Tests, gewollt und benannt).
- [ ] `git diff Source/KanbanC.Blazor.Tests/Services/WebApiAufrufTests.cs` ist nach Schritt 9 **leer**.
- [ ] `grep -c "await WebApiAufruf.MitAusfallmeldung" Source/KanbanC.Blazor -r` liefert weiterhin **48**; keine Aufrufstelle ist entstanden oder verschwunden.
- [ ] `grep -rn "^\s*await WebApiAufruf.MitAusfallmeldung" Source/KanbanC.Blazor` liefert **0** Treffer (vorher 2).
- [ ] `dotnet run ~/.claude/tools/csharp-stil-check.cs Source/KanbanC.Blazor/Services/WebApiAufruf.cs` ohne FEHLER-Befund.
- [ ] `dotnet test Source/KanbanC.PlaywrightTests` unverändert grün — insbesondere `IdentitaetWaehlenE2ETests.cs:296/:323/:421/:427`, die `#blazor-error-ui` als unsichtbar zusichern.
- [ ] `Source/KanbanC.Blazor` hat weiterhin **keine** Projektreferenz auf `KanbanC.BL` (`grep -c "KanbanC.BL" Source/KanbanC.Blazor/KanbanC.Blazor.csproj` = 0).

## Rollback-Strategie

Vor Schritt 1: `git tag r00049-start`. Je Schritt ein Commit, also je Schritt ein sauberer Rückweg.

| Schritt | Zurück |
|---|---|
| 1, 4, 7, 9 | `git revert <commit>` — keine Abhängigkeit nach hinten |
| 2 | `git revert` — dann fällt der Vertragsbruch wieder als `InvalidOperationException` durch; Schritt 5 muss mit zurück |
| 3 | `git revert` oder die Testdatei löschen; nichts hängt daran |
| 5 | **der Verhaltensschritt** — `git revert <commit>` stellt den alten Fangbereich her; Schritt 3 wird dadurch wieder rot und muss mit zurück |
| 6 | `git revert` |
| 8 | `git checkout r00049-start -- Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor` |
| alles | `git reset --hard r00049-start` |

Bei Zweifel in einem einzelnen IDE-Refactoring: `Ctrl+Z` vor dem Commit; die Schritte sind so geschnitten, dass keiner mehr als eine Datei plus deren Tests berührt (Ausnahme Schritt 5: `WebApiAufruf.cs` allein).

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist belegt und nicht hypothetisch: bei `R00024` ging eine 3-MB-Datei verloren, und der einzige Grund, warum der Benutzer es nicht erfuhr, war dieser eine `catch`-Block — die Ausnahme war keine `HttpRequestException`, also gab es keine Meldung. Wenn wir die **Typprüfung durch eine Klassifikation ersetzen**, dann hat jede Ausnahme auf dem Weg zur WebApi genau eine von drei Adressen (Ausfallmeldung, eigene Befundmeldung, Weiterreichen), was dazu führt, dass es keinen vierten Ausgang mehr gibt — und „lautlos verschwinden" war genau dieser vierte Ausgang. Der Hebel liegt hier und nicht an den 48 Aufrufstellen, weil alle 48 dieselbe eine Stelle benutzen: eine Änderung an einer Datei deckt jede Handlung der Oberfläche ab, während 48 einzelne Fangpunkte 48 Gelegenheiten wären, einen zu vergessen — die Lage, aus der `R00024` kam. Er liegt auch nicht weiter vorn bei den Klienten, denn die kennen den Bedienvorgang nicht, dem eine Meldung gilt. Und er löst das Problem, ohne die Zusage aufzugeben, die `R00024` zu Recht verteidigt hat: ein Programmfehler wird weiterhin nicht in eine freundliche Falschaussage verwandelt — das beweist `WebApiAufrufTests.cs:31`, unverändert und grün.

## Erwartete Verbesserungen

- **Kein vierter Ausgang.** Vorher: 1 gefangener Typ, mindestens 7 durchfallende, davon 2 zu Recht. Nachher: 2 klassifizierte Klassen, 1 bewusst durchgelassene, 0 unklassifizierte.
- **Keine Falschaussage.** „Die WebApi ist nicht erreichbar" steht künftig nur noch da, wo sie es nicht war.
- **Eine Stelle statt vieler.** Der Fangbereich wächst zentral; keine der 48 Aufrufstellen muss davon wissen.
- **Zwei Verlustwege weniger** (Schritt 5 und Schritt 8), beide belegt, beide maschinell nachprüfbar.
- **Ein Muster statt zweier.** `WebApiAufruf` fängt danach so, wie `Ereignisleitung`, `Pfadkopie` und `Identitaetsspeicher` es längst tun.

## Offene Fragen

- **Bekommt der fachliche Befund eine eigene Meldung oder soll er in `Zurueckweisung`-Form gerendert werden?** *Angenommen: eigene Meldung als `string`* — die 48 Aufrufstellen rendern heute alle einen `string`; eine `Zurueckweisung` als Rückgabe würde alle 48 anfassen und den Refactoring-Charakter sprengen. Der Text trägt Grund und Kompensationsaktion, damit die Form der `Fehlerbefund`-Zusage entspricht, ohne den Typ zu ändern.
- **Gehört `OperationCanceledException` ohne innere `TimeoutException` in die Ausfallklasse?** *Angenommen: nein* — `Board.razor:576` und `Kartendetail.razor:2176` fangen sie bereits als regulären Abbruch; sie hier zusätzlich als Ausfall zu melden, erzeugte eine Meldung für einen Vorgang, den der Benutzer selbst beendet hat.
- **Sollen `AlsErgebnis` und `AlsErgebnisMitGemeldetenBefunden` in dieser Anforderung zusammengelegt werden?** *Angenommen: nein* — 13 Aufrufe in 6 Klienten ändern ihr Verhalten bei 404; das ist eine eigene Anforderung (siehe `## Notizen`).

## Notizen

### Der eine Schritt, der Verhalten ändert

Ein Refactoring erhält Verhalten; Schritt 5 tut das nicht. Er ist trotzdem hier richtig aufgehoben und wird nicht ausgelagert, weil die Verhaltensänderung **der Zweck** dieser Anforderung ist und ohne die Schritte 1, 2 und 4 nicht sicher machbar wäre. Alle anderen acht Schritte sind verhaltenserhaltend; Schritt 5 ist einzeln commitet, einzeln rückholbar und durch die drei Tests aus Schritt 3 eingerahmt. Die Verhaltensänderung betrifft ausschließlich den **Fehlerpfad**: wo heute nichts oder ein toter Circuit stand, steht danach ein Satz.

### Verworfene Alternativen

| Option | Achsen | Warum verworfen |
|---|---|---|
| **`catch (Exception)` ohne Filter** | Komplexität niedrig · Testbarkeit niedrig · Reversibilität hoch · Risiko hoch | Verwandelt jeden Programmfehler in „Die WebApi ist nicht erreichbar" und bricht `WebApiAufrufTests.cs:31` — die Verwerfung aus `R00024` gilt für **diese** Variante unverändert. |
| **Rückgabetyp auf `Zurueckweisung`/`Aufrufbefund` heben** | Komplexität hoch · Testbarkeit hoch · Reversibilität niedrig · Risiko hoch | Der fachlich richtige Zielzustand — aber er ändert 48 Aufrufstellen in 14 Dateien und ist damit kein Refactoring mehr, sondern ein Umbau des Fehlerkanals der Oberfläche. Bleibt als Richtung notiert. |
| **Zweite Überladung `MitBefund` neben `MitAusfallmeldung`** | Komplexität mittel · Testbarkeit hoch · Reversibilität hoch · Risiko hoch | Erzeugt genau das, was diese Anforderung an `ApiAntwortleser` als Befund notiert: zwei Wege für dieselbe Frage, und die Wahl zwischen ihnen an 48 Aufrufstellen. |
| **Je Aufrufstelle einen eigenen Fangpunkt** (das Vorgehen aus `R00024`, verallgemeinert) | Komplexität hoch · Testbarkeit mittel · Reversibilität mittel · Risiko hoch | 48 Gelegenheiten, einen zu vergessen. `R00024` hat das für **eine** Stelle mit besonderem Bedarf getan (der Dateiname gehört in die Meldung) — als Regel wäre es die Lage, aus der der Befund stammt. |
| **Nichts tun, `MitAusfallmeldung` als bewusst enges Netz belassen** | Komplexität null · Risiko hoch | Wäre vertretbar, wenn irgendwo stünde, wohin das Durchgelassene geht. Es steht nirgends, und einmal ist es in einen Datenverlust gelaufen. |

### Bewusst out of scope

- **Zusammenlegen der beiden `ApiAntwortleser`-Lesarten.** Der Befund ist oben belegt und gehört in eine eigene Anforderung: `AlsErgebnis` wirft den Befund weg, den die WebApi zu jedem 404 mitliefert (`Zurueckweisungen.AlsNichtgefunden`), und `R00036`s Begründung für `AlsErgebnisMitGemeldetenBefunden` ist nicht routenspezifisch. Betroffen wären 13 Aufrufe in `BoardApiKlient`, `BoardimportApiKlient`, `ImportApiKlient`, `KartenApiKlient`, `KartenklassenApiKlient` und `SpaltenApiKlient`.
- **Die zwei doppelten Fangpunkte** `Board.razor:236` und `Kartendetail.razor:832`, die `HttpRequestException` erneut fangen, um `_webApiIstNichtErreichbar` zu setzen. Sie sind korrekt, aber ein drittes Vorkommen desselben Themas; ihr Zusammenschluss mit `WebApiAufruf` ändert die Ladewege beider Seiten und gehört nicht in diese Anforderung.
- **Protokollierung.** Ob die klassifizierten Ausnahmen zusätzlich in ein Protokoll gehören (wie `Ereignisleitung.cs:66` es tut), ist eine eigene Frage; diese Anforderung macht sie **sichtbar**, nicht **nachlesbar**.

### Regelbezug

- **C24 (sichtbarer Fehlerpfad)** — die `catch`-Blöcke stehen unmittelbar unter dem abgesicherten `await`, nicht am Klassenende; kein Ausnahmetyp fällt stillschweigend durch, so wie kein Enum-Wert stillschweigend durchfallen darf.
- **C16 (keine tote Flexibilität)** — in die Klassifikation kommt nur, wofür oben eine Herkunft belegt ist; kein Typ „für später".
- **C13** — jedes Prädikat mit benannter Bedingungsvariable, keine zusammengesetzte Bedingung im `return`.
- **C18 / Skill `test-ehrlichkeit`** — der Test aus Schritt 3 ist **vor** der Behebung rot; kein bestehender Test wird angepasst, um grün zu werden.
- **C06/C07** — `IstWebApiAusfall`, `IstAntwortstoerung`, `WebApiAntwortstoerung`, `WebApiAntwortException`: deutsche Domänensprache, Bezeichner ohne echte Umlaute, Meldungstexte mit echten Umlauten.
- **Kernregel des Projekts** — es entsteht keine Projektreferenz von `KanbanC.Blazor` auf `KanbanC.BL`; alle neuen Typen liegen in `Source/KanbanC.Blazor/Services/`.

## Missing-Docs

- **Ausnahmeverhalten von `HttpClient` bei Zeitüberschreitung über .NET-Versionen.** Ob die Zeitüberschreitung als `TaskCanceledException` mit innerer `TimeoutException` oder als `TimeoutException` ankommt, entscheidet über die Prädikatsform in Schritt 5. Vor der Umsetzung mit einem Probe-Test gegen die im Projekt verwendete .NET-Version belegen (Skill `dependency-probe`) statt aus allgemeinem Wissen anzunehmen.
- **Welche Ausnahme ein abgerissener `IBrowserFile`-Strom in Blazor Server tatsächlich wirft** und ob sie am `await` des Aufrufers ankommt. `R00024` belegt einen `pageerror` im Browser, aber keinen gefangenen C#-Typ. Bleibt sie ein Nebenpfad, deckt Schritt 5 sie nicht ab — der Fangpunkt aus `R00024` bleibt dann dauerhaft nötig, und das gehört an `Anhangausfall` notiert.
