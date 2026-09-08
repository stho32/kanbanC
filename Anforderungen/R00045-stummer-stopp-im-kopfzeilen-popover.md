---
id: R00045
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: R00030
---

# R00045: Behebung des stummen Stopps im Kopfzeilen-Popover

## Beschreibung

Wer in der Kopfzeile die Liste „Läuft gerade …" aufklappt und an einer Zeile das Stoppquadrat drückt, bekommt **keine Auskunft, wenn der Stopp nicht durchgeht**. Die Oberfläche wirft die Antwort der WebApi weg — sowohl eine fachliche Zurückweisung als auch die Ausfallmeldung — und lädt danach die Liste neu. Der Mensch sieht nur, dass die Zeile fort ist, und hält das für Erfolg.

Der schwerste Fall ist der Ausfall: fällt die WebApi zwischen Klick und Antwort aus, **verschwindet die Plakette ganz**. Genau das bedeutet nach [`R00030`](R00030-laufende-timer-sehen.md) „es läuft gerade nichts" — die Kopfzeile behauptet also das Gegenteil dessen, was gilt, und der Timer läuft weiter.

**Schritte (Ausfall):** zwei Timer starten · Plakette anklicken, die Liste steht offen · die WebApi anhalten · an einer Zeile das Stoppquadrat drücken.
**Erwartet:** eine lesbare Meldung, dass der Timer nicht beendet wurde, und eine Plakette, die weiter `2 laufen` sagt.
**Tatsächlich:** das Popover schließt sich, die Plakette ist weg, es steht nirgends etwas. Nicht zu unterscheiden von „beide Timer sind beendet".

**Schritte (Zurückweisung):** ein Timer läuft, die Liste steht offen · in einem zweiten Browser wird derselbe Zeiteintrag auf der Kartenseite **gelöscht** · das Stoppquadrat drücken.
**Erwartet:** eine Meldung, dass es diesen Zeiteintrag nicht mehr gibt, und was das für den Menschen bedeutet.
**Tatsächlich:** die Zeile fällt heraus, kein Wort. Der Befund der WebApi — Grund **und** Kompensationsaktion — wird verworfen.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt. Ein Timer, den man startet und stoppt, ohne Umstand." Ein Stopp, der schweigend nicht stattfindet, ist der größte denkbare Umstand: er kostet den Menschen die Auskunft **und** die Arbeitszeit, die weiterläuft.

**Der Fehler stammt aus `I0027`/[`R00030`](R00030-laufende-timer-sehen.md) „Laufende Timer sehen" und ist bereits im Trunk.** `R00030` hat das Stoppquadrat an jede Zeile gehängt (US-7) und dabei nur den Erfolgsweg gezeichnet; kein Akzeptanzkriterium und kein Szenario sagt, was bei einem gescheiterten Stopp geschieht. Diese Anforderung schließt genau diese Lücke.

### Was der Bugbericht nicht richtig traf

Anmerkung **466** nennt als typischen Fall „jemand anders hat den Eintrag inzwischen beendet". **Dieser Fall ist keine Zurückweisung.** Der zweite Stopp desselben Eintrags antwortet mit **200** und dem bereits beendeten Eintrag — das ist gewollt und belegt:

- `Source/KanbanC.WebApi/Endpunkte/ZeitenEndpunkte.cs:60-76` — „Auch der zweite Stopp antwortet mit 200 … ein Agent, dessen Antwort unterwegs verlorenging, wiederholt den Aufruf gefahrlos."
- `Source/KanbanC.BL/Persistenz/Zeiten/ZeitenRepository.cs:55-80` — der Wiederhol-Schutz sitzt im `UPDATE … AND Ende IS NULL`; die Zeile existiert weiter, gelesen und zurückgegeben wird der beendete Eintrag.

Der fachlich zurückgewiesene Fall ist ein anderer und ebenso real: **der Zeiteintrag existiert nicht mehr**, weil ihn jemand auf der Kartenseite gelöscht hat (`DELETE /api/karten/{karteId}/zeiten/{zeiteintragId}`, aus `R00029`). Dann antwortet die Route mit **404** und dem Befund `zeiteintrag-unbekannt`. Karten lassen sich nicht löschen — es gibt keine `MapDelete`-Route dafür —, also ist der gelöschte Zeiteintrag der einzige Weg in die Zurückweisung.

Die Anforderung unterscheidet deshalb **drei** Ausgänge statt zweier und schreibt den ersten ausdrücklich fest, damit niemand eine Meldung erfindet, die die API nicht hergibt.

## Ursachenanalyse

### Root Cause

`Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor:271-285`:

```csharp
private async Task StoppeZeile(LaufendeZeitmessung messung)
{
    await WebApiAufruf.MitAusfallmeldung(() => BeendeZeitmessung(messung));
    await LadeLaufende();
    ...
}

private async Task BeendeZeitmessung(LaufendeZeitmessung messung)
{
    await ZeitenKlient.BeendeZeitmessung(messung.Zeiteintrag.Karte, messung.Zeiteintrag.ZeiteintragId);
}
```

Zwei Auskünfte fallen an derselben Stelle unter den Tisch:

1. **Das `ApiErgebnis<Zeiteintrag>`.** `ZeitenApiKlient.BeendeZeitmessung` liefert es (`Source/KanbanC.Blazor/Services/ZeitenApiKlient.cs:40-45`), aber `BeendeZeitmessung(messung)` ist als `async Task` deklariert und wirft den Rückgabewert weg. `WurdeZurueckgewiesen` wird nie gefragt, `Zurueckweisung` nie gelesen.
2. **Die Ausfallmeldung.** `WebApiAufruf.MitAusfallmeldung` gibt bei `HttpRequestException` den Text zurück (`Source/KanbanC.Blazor/Services/WebApiAufruf.cs`); hier wird er keiner Variablen zugewiesen.

**Verstärkend — und der Grund, warum der Ausfall wie ein Erfolg aussieht:** das anschließende `LadeLaufende()` (`Kopfzeile.razor:149-159`) fängt denselben Ausfall und setzt `_laufende = []`. Daraus folgt `Laufzaehler.Fuer([], …) == null` (`Source/KanbanC.Blazor/Services/Laufzaehler.cs:24-28`), die Plakette wird nicht gerendert, und `esLaeuftNichtsMehr` schließt zusätzlich das Popover. Ein Ausfall erzeugt also **exakt dieselbe Oberfläche** wie ein erfolgreicher Stopp des letzten Timers — die Lage, die `R00030` US-8 als „läuft nichts" definiert.

**Ausgeschlossen wurde:** die WebApi, die BL und der HTTP-Klient. `ZeitenApiKlient` liest den Befund korrekt aus dem 404 und legt ihn in das `ApiErgebnis` — belegt durch `Source/KanbanC.Blazor.Tests/Services/ZeitenApiKlientTests.cs:97-166`. Der Fehler liegt **vollständig in der Oberfläche**, in einer einzigen Komponente.

### Was im Popover sonst noch geprüft wurde

Der Auftrag verlangt, dieselbe Stelle im übrigen Popover zu suchen statt zu vermuten. Ergebnis:

| Stelle | Beleg | Befund |
|---|---|---|
| **Sprung zur Karte** | `Laufzeitpopover.razor` — die Zeile ist ein `<a href="karten/{KarteId}">` | **Kein Befund.** Kein API-Aufruf, kein Ergebnis, das verworfen werden könnte. Der Adresswechsel löst nur `AufAdresswechsel` aus. |
| **Aufklappen** | `Kopfzeile.razor:246-266` `SchalteLaufzeitliste` → `LadeLaufende()` | **Derselbe Befund.** Bei Ausfall wird `_laufende` geleert, `esLaeuftNichtsMehr` greift, das Popover **öffnet sich gar nicht** — ein Klick auf die Plakette tut sichtbar nichts. |
| **Nachladen** | `Kopfzeile.razor:149-159` `LadeLaufende`, aufgerufen aus `OnInitializedAsync`, `AufAdresswechsel`, `HoleLaufendeNach` | **Derselbe Befund.** Jeder Ausfall leert stillschweigend die Liste; die Plakette verschwindet oder zählt zu niedrig, ohne dass etwas dasteht. |
| **Stoppen** | `Kopfzeile.razor:271-285` | Der gemeldete Befund. |
| Identitätswahl aufklappen | `Kopfzeile.razor:165` `LadeKontributoren` | Derselbe Baufehler (Rückgabewert verworfen), aber **anderes Popover** — siehe „Bewusst nicht dabei". |

Die drei Befunde der Laufzeitliste haben **eine** Ursache und werden zusammen behoben: die Kopfzeile hat keine Stelle, an der sie etwas sagen kann, und behandelt deshalb „ich weiß es nicht" wie „es ist nichts".

### Betroffene Komponenten

- `Source/KanbanC.Blazor/Components/Layout/Kopfzeile.razor` — `StoppeZeile`, `BeendeZeitmessung`, `LadeLaufende`, `SchalteLaufzeitliste`
- `Source/KanbanC.Blazor/Components/Layout/Laufzeitpopover.razor` — trägt die Meldung über der Liste
- `Source/KanbanC.Blazor/Services/` — neue Operation für den Wortlaut
- **Nicht betroffen:** `KanbanC.WebApi`, `KanbanC.BL`, `KanbanC.Contracts`, das Schema. Keine Migration, keine Änderung an Route, Verb, Statuscode oder Antwortgestalt.

## Lösungsvorschlag

### Langfristige Lösung

**Option 1 — der gescheiterte Stopp bekommt eine Stelle im Popover, und die Liste wird bei Ausfall nicht mehr geleert.**

Drei Ausgänge, drei Verhaltensweisen:

**A — Erfolg (200).** Die Zeile fällt heraus, die Plakette zählt herunter, **keine Meldung**. Das schließt den Fall ein, dass ein anderer den Eintrag vorher beendet hat: die API antwortet idempotent mit 200, der Mensch bekommt, was er wollte, und eine Meldung wäre eine erfundene Störung. **Diese Entscheidung steht hier, damit sie niemand für ein Versehen hält.**

**B — Zurückweisung (404, `zeiteintrag-unbekannt`).** Über der Liste steht ein Satz, der den Grund nennt und sagt, was gilt: der Zeiteintrag ist fort, gelöscht hat ihn jemand anders, die Liste ist bereits neu geholt. Der Wortlaut entsteht aus dem Befund der WebApi in einer eigenen, testbaren Operation — Muster `Kontributorenmeldung` (`Source/KanbanC.Blazor/Services/Kontributorenmeldung.cs`): bekannter Code → eigener, menschentauglicher Satz; jeder andere Code → `befund.Meldung`, so wie die WebApi ihn meldet.

**C — Ausfall (`HttpRequestException`).** Über der Liste steht ein **anderer** Satz: der Timer wurde nicht beendet, weil die WebApi nicht erreichbar ist, und was der Mensch tun kann. Muster `Anhangausfall` (`Source/KanbanC.Blazor/Services/Anhangausfall.cs`) — eigener Text statt des nackten `WebApiAusfall.Meldung`, weil dieser nur sagt, was kaputt ist, und nicht, was **nicht geschehen** ist.

Dazu die Korrektur, ohne die C nur halb wirkt: **`LadeLaufende` leert `_laufende` bei einem Ausfall nicht mehr.** Was zuletzt bekannt war, bleibt stehen — dieselbe Regel wie auf der Boardliste (`WebApiAusfallE2ETests`: „eine lesbare Meldung, und die Liste bleibt stehen"). Damit bleibt die Plakette stehen, das Popover bleibt offen, und die Meldung erklärt den Rest. Ein Nachladen, das gar nichts erfahren hat, darf keine Aussage über die Welt treffen.

Warum diese Option: sie sitzt genau dort, wo der Fehler entstand, ändert keinen Vertrag, braucht keine neue Gestaltung (die Klassen `meldung` und `meldung-abweisung` stehen in `wwwroot/oberflaeche.css` und ziehen ihre Werte bereits aus den Tokens von `gestaltung.css`) und ist über E2E vollständig prüfbar.

### Alternative Ansätze

- **Option 2 — eine gemeinsame Meldungsstelle für die ganze Kopfzeile**, die auch die Identitätswahl und das Laden der Kontributoren abdeckt: richtig gedacht, aber sie zieht ein zweites Popover und einen zweiten Slice in einen Bugfix hinein und verschiebt die Behebung des gemeldeten Fehlers.
- **Option 3 — `WebApiAufruf` um ein `MitErgebnis<T>` erweitern**, das Ausfall und `ApiErgebnis` in einem Rückgabewert bündelt, sodass eine verworfene Antwort gar nicht mehr kompiliert: der beste Hebel gegen die Wiederholung, aber er fasst über zwanzig Aufrufstellen in `Kartendetail`, `Board`, `Boards` und `Import` an — ein Refactoring, das eine eigene Anforderung verdient und nicht am Fehlerpfad hängen darf.
- **Option 4 — die WebApi den zweiten Stopp mit 409 beantworten lassen**, damit „war schon beendet" ein eigener Fall wird: verworfen, weil es die in `R00027` ausdrücklich zugesagte gefahrlose Wiederholbarkeit für Agenten aufhebt — ein Fehler in der Oberfläche wird nicht in der API repariert.

| Option | Komplexität | Testbarkeit | Reversibilität | Risiko |
|---|---|---|---|---|
| 1 Lokal in der Kopfzeile | gering | hoch (E2E + Unit auf den Wortlaut) | hoch | gering — eine Komponente |
| 2 Meldungsstelle der Kopfzeile | mittel | hoch | mittel | Umfang wächst über den Bug hinaus |
| 3 `MitErgebnis<T>` überall | hoch | hoch | gering | trifft alle Seiten zugleich |
| 4 409 in der WebApi | mittel | hoch | gering | bricht eine zugesagte Eigenschaft der API |

## Test-Strategie

### Reproduktionstests (ohne Fix rot)

Der Fehler sitzt in einer `.razor`-Komponente; `KanbanC.Blazor.Tests` kennt kein bUnit, und der Fehlerpfad ist genau der, den ein Test durch die Oberfläche auslösen muss. Die Reproduktion gehört deshalb nach `KanbanC.PlaywrightTests`, neue Datei `Tests/LaufendeTimerFehlerpfadeE2ETests.cs`:

1. `Wenn_der_Stopp_aus_dem_Popover_bei_ausgefallener_WebApi_scheitert_dann_sagt_das_Popover_es_und_die_Plakette_bleibt_stehen`
   **Arrange:** zwei Timer auf zwei Boards, Identität gewählt, Popover offen (`Rahmen.Laufzeitpopover`), dann `Testumgebung.Aktuelle.HalteWebApiAn()`.
   **Act:** `Rahmen.LaufzeitStoppquadrat(zeiteintragId)` drücken.
   **Assert:** die Meldung im Popover ist sichtbar und nennt, dass der Timer **nicht** beendet wurde; `Rahmen.Laufzeitzaehler` trägt weiterhin `2 laufen`; das Popover steht offen; die Ausnahmeanzeige bleibt verborgen.
   **Heute rot:** heute schließt sich das Popover, die Plakette verschwindet, und es steht nichts da.

2. `Wenn_der_Zeiteintrag_inzwischen_geloescht_wurde_dann_nennt_das_Popover_den_Grund`
   **Arrange:** ein Timer läuft, Popover offen; über `Infrastructure/WebApiKlient` `DELETE /api/karten/{karteId}/zeiten/{zeiteintragId}` absetzen.
   **Act:** Stoppquadrat drücken.
   **Assert:** die Meldung nennt, dass es diesen Zeiteintrag nicht mehr gibt, und **nicht** „Die WebApi ist nicht erreichbar"; die Liste ist neu geholt.
   **Heute rot:** heute fällt die Zeile wortlos heraus.

3. `Wenn_das_Aufklappen_bei_ausgefallener_WebApi_scheitert_dann_sagt_die_Kopfzeile_es_statt_nichts_zu_tun`
   **Arrange:** ein Timer läuft, Seite geladen, danach `HalteWebApiAn()`.
   **Act:** Plakette anklicken.
   **Assert:** das Popover öffnet sich mit dem zuletzt bekannten Stand und der Ausfallmeldung; die Plakette verschwindet nicht.
   **Heute rot:** heute geschieht auf den Klick sichtbar gar nichts.

### Unit Tests (Wortlaut)

`Source/KanbanC.Blazor.Tests/Services/StoppmeldungTests.cs` — pure Logik ohne Seiteneffekte, Muster `KontributorenmeldungTests`:

- Eine `Zurueckweisung` mit dem Code `zeiteintrag-unbekannt` ergibt einen Satz, der den Grund nennt und sagt, was gilt.
- Ein unbekannter Code fällt auf `befund.Meldung` zurück, so wie die WebApi ihn meldet.
- Der Ausfallsatz unterscheidet sich vom Zurückweisungssatz und ist nicht wortgleich mit `WebApiAusfall.Meldung`.

Diese Tests sind **keine** Reproduktion — sie prüfen einen neuen Baustein und wären auch dann grün, wenn `StoppeZeile` ihn nie aufriefe. Die Reproduktion leisten allein die drei E2E-Tests oben (Skill `test-ehrlichkeit`).

### Code-Extraktion für isoliertes Testing

Nur der Wortlaut wird herausgezogen (`Stoppmeldung` in `Services/`) — dieselbe Trennung, die `Kontributorenmeldung`, `Anhangausfall` und `Laufzaehler` schon tragen. Der Zustandsübergang selbst bleibt in der Komponente und wird über E2E geprüft; ihn nach C# zu heben, ohne bUnit einzuführen, hieße die Komponente umzubauen, um einen Fehlerpfad prüfbar zu machen, den die Oberfläche ohnehin auslöst.

### Regressionstests

- `LaufendeTimerE2ETests` bleibt vollständig grün — besonders `Wenn_auch_die_letzte_Zeile_gestoppt_wird_dann_verschwinden_Popover_und_Plakette_und_bleiben_nach_dem_Reload_fort` (der Erfolgsweg schließt das Popover weiterhin) und `Wenn_aus_dem_Popover_gestoppt_wird_dann_zeigt_die_Kartenseite_denselben_Zustand`.
- Neu und ausdrücklich grün-vor-und-nach dem Fix: `Wenn_ein_anderer_den_Eintrag_schon_beendet_hat_dann_faellt_die_Zeile_ohne_Meldung_heraus` — die Wache gegen eine erfundene Meldung für Ausgang A.
- `WebApiAusfallE2ETests`, `ZurueckweisungE2ETests`, `TimerStoppenE2ETests`, `ZeitenSehenE2ETests` unverändert.
- `KanbanC.WebApi.IntegrationTests` und `KanbanC.BL.Tests` unverändert — es ändert sich nichts hinter der HTTP-Grenze.

## Akzeptanzkriterien

### Der gescheiterte Stopp ist sichtbar
- [ ] Der Reproduktionstest 1 ist ohne Fix rot und mit Fix grün
- [ ] Scheitert der Stopp aus dem Popover, steht im Popover eine lesbare Meldung, bevor der Mensch irgendetwas weiter tut
- [ ] Die Meldung sagt, dass der Timer **nicht** beendet wurde — nicht nur, dass etwas schiefging
- [ ] Die Meldung verschwindet, sobald der Mensch die nächste Handlung im Popover auslöst

### Die drei Ausgänge sind unterschieden
- [ ] Bei einer Zurückweisung (404 `zeiteintrag-unbekannt`) steht ein Satz, der den Grund nennt und sagt, was der Mensch stattdessen tun kann
- [ ] Bei einem Ausfall (`HttpRequestException`) steht ein **anderer**, wörtlich verschiedener Satz
- [ ] Kein Ausfallsatz erscheint bei einer Zurückweisung und umgekehrt — geprüft über beide E2E-Tests
- [ ] Ein unbekannter Befundcode wird als `befund.Meldung` der WebApi angezeigt, nicht verschluckt und nicht umgeschrieben
- [ ] Beendet ein anderer denselben Eintrag vor mir, fällt die Zeile **ohne** Meldung heraus — die API antwortet mit 200, und es gibt nichts zu melden

### Die Kopfzeile behauptet nichts, was sie nicht weiß
- [ ] Fällt die WebApi beim Stoppen aus, bleibt die Plakette stehen und zählt weiter, was zuletzt bekannt war: laufen zwei Timer und der Stopp scheitert, steht dort weiterhin `2 laufen` und nicht `1 läuft` und nicht nichts
- [ ] Fällt die WebApi beim Aufklappen aus, öffnet sich das Popover mit dem zuletzt bekannten Stand und der Ausfallmeldung, statt sichtbar nichts zu tun
- [ ] Fällt die WebApi beim Nachladen aus (Seitenwechsel, eigener Start/Stopp auf der Kartenseite), verschwindet die Plakette nicht
- [ ] Kehrt die WebApi zurück, zieht der nächste Anlass den echten Stand nach, und die Meldung ist fort

### Nichts anderes ändert sich
- [ ] Der erfolgreiche Stopp verhält sich unverändert: Zeile fort, Plakette zählt herunter, beim letzten Timer schließen Popover und Plakette
- [ ] `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}/ende` bleibt unverändert — Adresse, Verb, Statuscodes, Antwortgestalt und Fehlervertrag; der zweite Stopp antwortet weiterhin mit 200
- [ ] Kein Schemawechsel, keine Migration
- [ ] `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL`
- [ ] Alle Tests aller Ebenen grün, `TreatWarningsAsErrors` erfüllt

### Gestaltung
- [ ] Die Meldung nutzt die bestehenden Klassen `meldung` und `meldung-abweisung` aus `wwwroot/oberflaeche.css`
- [ ] Neue CSS-Werte kommen ausschließlich aus den Tokens in `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Farb-, Abstands- oder Schriftliteral in einer Komponenten-CSS-Datei
- [ ] Die Meldung trägt `role="alert"` und eine feste Elementkennung, damit E2E sie ansprechen kann

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Layout/` — `Kopfzeile.razor` hält den Meldungszustand, `Laufzeitpopover.razor` rendert ihn über der Liste.
- **Logik der Oberfläche:** `Source/KanbanC.Blazor/Services/` — `Stoppmeldung.cs` neben `Kontributorenmeldung.cs`, `Anhangausfall.cs` und `WebApiAusfall.cs`; die Ordner sind hier fachlich, nicht nach Typ geschnitten.
- **Tests:** `Source/KanbanC.Blazor.Tests/Services/StoppmeldungTests.cs` spiegelt den Ordner des Bausteins; `Source/KanbanC.PlaywrightTests/Tests/LaufendeTimerFehlerpfadeE2ETests.cs` neben `LaufendeTimerE2ETests.cs`; `PageObjects/Rahmen.cs` bekommt den Locator der Meldung.
- **Unberührt:** `Source/KanbanC.WebApi/`, `Source/KanbanC.BL/`, `Source/KanbanC.Contracts/` und deren Testprojekte.

## Technische Überlegungen

### Ablauf

1. **Klick auf das Stoppquadrat** — `Laufzeitpopover` löst `Gestoppt` aus, `Kopfzeile.StoppeZeile(messung)` läuft an
   - 1.1 Der Meldungszustand wird zurückgesetzt: eine neue Handlung erbt keine alte Meldung
2. **Aufruf und Auswertung** statt Verwerfen
   - 2.1 `var ausfall = await WebApiAufruf.MitAusfallmeldung(() => SendeZeitmessungsende(messung));`
   - 2.2 In `SendeZeitmessungsende`: `var ergebnis = await ZeitenKlient.BeendeZeitmessung(…);`
     - 2.2.1 Wenn `ergebnis.WurdeZurueckgewiesen`: `_stoppmeldung = Stoppmeldung.AusZurueckweisung(ergebnis.Zurueckweisung)`
   - 2.3 Wenn `ausfall is not null`: `_stoppmeldung = Stoppmeldung.Ausfall`
3. **Liste nachziehen** — `await LadeLaufende()`
   - 3.1 Bei Ausfall: `_laufende` **bleibt stehen**, `_laufzaehler` wird nicht auf `null` gesetzt; die Ausfallmeldung wird gesetzt, falls Schritt 2 noch keine gesetzt hat
   - 3.2 Bei Erfolg: `_laufende` und `_laufzaehler` wie bisher
4. **Popover-Entscheidung**
   - 4.1 Steht eine Meldung an, bleibt das Popover offen — sonst hätte niemand sie gelesen
   - 4.2 Sonst wie bisher: ist die Liste leer, schließt es sich
5. **Aufräumen** — `SchliessePopover` und jede neue Handlung im Popover setzen die Meldung zurück

### Grobentwurf

**Wichtige Einstiegsstellen:** das Stoppquadrat in `Laufzeitpopover.razor` (Ereignis `Gestoppt`), die Plakette in `Kopfzeile.razor` (Ereignis `SchalteLaufzeitliste`) und die vier Aufrufer von `LadeLaufende`.

- `Stoppmeldung` (Operation, pure Logik) — macht aus einer `Zurueckweisung` oder einem Ausfall den Satz, den ein Mensch im Popover liest. Muster `Kontributorenmeldung`: bekannter Code → eigener Satz, sonst `befund.Meldung`.
  - `static string AusZurueckweisung(Zurueckweisung zurueckweisung)`
  - `static string Ausfall { get; }`
- `Laufzeitpopover` (Komponente) — bekommt einen Parameter mehr und zeigt ihn über der Liste, dort, wo der Blick gerade ist.
  - `[Parameter] public string? Meldung { get; set; }`
- `Kopfzeile` (Komponente) — hält `_stoppmeldung` als einzigen neuen Zustand; `BeendeZeitmessung` wird zu `SendeZeitmessungsende` und wertet das `ApiErgebnis` aus, wie es `Kartendetail.SendeZeitmessungsende` seit `R00027` tut.

**Ein** Meldungsfeld, nicht zwei: Ausfall und Zurückweisung schließen einander aus, und zwei Felder ließen den Zustand zu, den es nicht geben darf — dieselbe Begründung, mit der `_offenesPopover` ein Enum statt zweier Wahrheitswerte ist.

### Änderungen an bestehenden Klassen

- `Kopfzeile.razor`: `StoppeZeile` wertet aus statt zu verwerfen; `BeendeZeitmessung` → `SendeZeitmessungsende` mit `ApiErgebnis`-Auswertung; `LadeLaufende` leert bei Ausfall nicht mehr und meldet; `SchalteLaufzeitliste` öffnet auch bei Ausfall; `SchliessePopover` setzt die Meldung zurück.
- `Laufzeitpopover.razor`: neuer Parameter `Meldung`, Rendern über der Liste mit `role="alert"` und fester Kennung.
- `PageObjects/Rahmen.cs`: Locator `LaufzeitMeldung`.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md`.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Stoppmeldung` — nimmt eine `Zurueckweisung`, gibt einen String; kein Zugriff, keine Zeit, kein Zufall.

**Integration:** keine. Hinter der HTTP-Grenze ändert sich nichts; die 404-Antwort der Route ist in `KanbanC.WebApi.IntegrationTests` seit `R00027` abgedeckt, das Lesen des Befunds im Klienten in `ZeitenApiKlientTests`.

**E2E:** die drei Reproduktionstests und die Wache für Ausgang A, alle in `LaufendeTimerFehlerpfadeE2ETests`.

## Abhängigkeiten

- Abhängig von: [R00030](R00030-laufende-timer-sehen.md) (der Slice, der den Fehler einbrachte), [R00027](R00027-timer-stoppen.md) (Stopp-Endpunkt und Idempotenz), [R00029](R00029-zeiteintrag-nachtragen-und-aendern.md) (das Löschen, das die Zurückweisung erzeugt)
- Blockiert: nichts

## Offene Fragen

- **Wo genau steht die Meldung — im Popover oder in der Kopfzeile?** Vorschlag und Annahme dieser Anforderung: **im Popover, über der Liste** — dieselbe Begründung, mit der `Zeiteintragsformular` seine Zurückweisung über die Eingaben und nicht an den Seitenkopf setzt: sie gehört in den Blick, in dem gerade gehandelt wird. Widerspruch würde nur den Ort ändern, keine Kriterien.
- **Verschwindet die Plakette bei einem Ausfall wirklich nicht mehr?** Annahme: nein, sie bleibt mit dem zuletzt bekannten Stand stehen. Die Alternative — Plakette weg, aber mit Ausfallmeldung — hielte die Lüge „es läuft nichts" aufrecht und wird deshalb nicht vorgeschlagen.
- **Bekommt der stehengebliebene Stand eine sichtbare Alterskennzeichnung?** Annahme: nein, nicht in diesem Bugfix. Die Kopfzeile trägt mit `_verbindungsmarke` bereits eine Marke für die abgerissene Ereignisleitung (`R00032`); eine zweite Altersanzeige daneben wäre eine Gestaltungsfrage und keine Fehlerbehebung.

## Warum löst diese Anforderung das Problem?

Der Auslöser ist eine Handlung, die nicht stattfindet und trotzdem wie ein Erfolg aussieht: der Mensch drückt Stopp, die Zeile verschwindet, und der Timer läuft weiter. Wird das `ApiErgebnis` ausgewertet statt verworfen und die Liste bei einem Ausfall nicht mehr geleert, dann hat die Kopfzeile zum ersten Mal einen Zustand für „ich weiß es nicht" — und muss ihn nicht länger als „es ist nichts" ausgeben. Daraus folgt beobachtbar: bei einer Zurückweisung steht der Grund da, bei einem Ausfall ein anderer Satz und eine Plakette, die weiter zählt; in beiden Fällen weiß der Mensch, dass er es noch einmal versuchen muss. Genau **diese** Änderung ist der Hebel, weil der Fehlerpfad an einer einzigen Stelle abgeschnitten wird — die WebApi liefert Grund und Kompensation längst, der Klient liest sie längst korrekt aus, und erst die letzte Zeile in `Kopfzeile.StoppeZeile` wirft sie weg. Eine vorgelagerte Änderung an API oder Klient reparierte etwas, das nicht kaputt ist; eine nachgelagerte an der Gestaltung könnte nur anzeigen, was in der Komponente gar nicht ankommt.

## Missing-Docs

Keine. Der Fehler und seine Behebung liegen vollständig im eigenen Bestand; jede Aussage dieser Anforderung ist mit `Datei:Zeile` belegt.

## Notizen

- **Priorität: Hoch.** Kein Datenverlust — der Zeiteintrag bleibt korrekt —, aber eine falsche Aussage über gemessene Arbeitszeit an der auffälligsten Stelle der Anwendung, und laufende Zeit, die weiterläuft, während der Mensch sie für beendet hält.
- **Betroffene Nutzer/Systeme:** jeder Mensch am Browser. KI-Agenten sind nicht betroffen — sie sprechen die API direkt und bekommen Befund und Kompensation seit jeher.
- **Workaround:** die Seite neu laden; danach zeigt die Plakette wieder den echten Stand.
- **Bewusst nicht dabei:** die Identitätswahl. `LadeKontributoren` (`Kopfzeile.razor:165`) verwirft seine Ausfallmeldung auf dieselbe Weise, und das Popover öffnet sich dann mit leerer Liste. Das ist ein eigener Befund an einem anderen Popover aus einem anderen Slice; er gehört in eine eigene Anforderung, nicht als Beifang in diesen Fix.
- **Bewusst nicht dabei:** `MitErgebnis<T>` als bauartbedingte Sperre gegen verworfene Antworten (Option 3). Der Hebel ist gut, der Umfang ist ein Refactoring über alle Seiten — Kandidat für `/anforderung refactoring`.
- **Verworfene Alternativen:** Optionen 2, 3 und 4 mit den Begründungen unter „Alternative Ansätze".
