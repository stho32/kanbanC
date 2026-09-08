---
id: R00046
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: R00032
---

# R00046: Behebung des springenden Standzeitpunkts im Trennungsdialog

## Beschreibung

Reißt der Kreislauf zwischen Browser und Anwendung ab, nennt der Trennungsdialog seit `R00032` den Zeitpunkt, seit dem der Schirm alt ist („Was du siehst, ist der Stand von 09:12."). Das ist die **einzige** Zeile, die über die englische Blazor-Vorlage hinaus gebaut wurde, und ihr einziger Zweck ist, dass ein Mensch weiß, wie veraltet das Bild vor ihm ist.

Genau diese Zeile lügt. `nenneStandDesSchirms(new Date())` läuft bei **jedem** Wechsel nach `state === "show"`. Geht der Dialog ein zweites Mal in `show`, ohne dass die Verbindung dazwischen wirklich stand, **springt der genannte Stand auf jetzt** — obwohl der Schirm weiterhin den Stand des ersten Abrisses zeigt. Der Dialog behauptet dann, das Bild sei aktuell, während er zugleich meldet, dass die Verbindung weg ist.

**Schritte:** Anwendung öffnen · den Kreislauf zum Browser um 09:12 abreißen lassen · den Dialog mit „Was du siehst, ist der Stand von 09:12." stehen lassen · einen zweiten Wechsel nach `show` auslösen, ohne dass die Verbindung dazwischen wieder stand (gescheitertes „Erneut verbinden", eine Rückkehr, die den Bildschirm nicht mehr erreicht, ein weiteres `show` der Laufzeit).
**Erwartet:** die Zeile nennt weiterhin **09:12** — den Zeitpunkt, ab dem der Schirm nicht mehr nachgeführt wird.
**Tatsächlich:** die Zeile nennt die **jetzige** Uhrzeit. Je länger die Trennung dauert und je öfter jemand „Erneut verbinden" drückt, desto jünger behauptet der Schirm zu sein.

Zahlt ein auf: [Vision](R00000-vision.md) — „**Live überall.** Bewegt ein Mensch oder die API eine Karte, sehen alle offenen Oberflächen die Änderung unverzüglich — ohne Reload, ohne Nachfragen." Der Trennungsdialog ist die Kehrseite dieser Zusage: er sagt, wann sie gerade **nicht** gilt und seit wann. Ein Zeitpunkt, der mitwandert, hebt genau diese Aussage auf — der Wert eines getrennten Schirms liegt nicht darin, dass er noch etwas kann, sondern darin, dass er über sein Alter nicht lügt ([R00032](R00032-nach-verbindungsabbruch-aufschliessen.md), „Geschäftlicher Nutzen").

**Die Regel ist im Projekt bereits entschieden und geschrieben — nur nicht im Browser.** `Ereignisverteiler.MeldeGetrennt` (`Source/KanbanC.Blazor/Services/Ereignisverteiler.cs:51-63`) hält für den **anderen** Abbruch (Blazor ↔ WebApi) genau diese Regel, samt Begründung im Kommentar: „Der genannte Zeitpunkt ist der des **ersten** Abrisses … jeder gescheiterte Versuch schöbe den Stand des Schirms sonst vor sich her, bis „Stand von" die jetzige Uhrzeit nennte." Der Wächter dort heißt `derAbrissIstSchonBekannt`. Im Skript des Trennungsdialogs fehlt er. Diese Anforderung trägt dieselbe Regel in die eine Datei, die sie nicht bekommen hat.

## Ursachenanalyse

### Root Cause

`Source/KanbanC.Blazor/Components/Layout/ReconnectModal.razor.js:13-24`

```js
function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        nenneStandDesSchirms(new Date());   // :15 — der Fehler
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
```

Drei Eigenschaften dieser vier Zeilen ergeben zusammen den Fehler:

1. **Der Zeitpunkt wird bei jedem `show` neu genommen** (`:15`). `new Date()` steht im Aufruf, nicht in einem Zustand, der zwischen zwei Ereignissen überlebt. Es gibt keinen Ort, an dem „der erste Abriss" stehen könnte, und deshalb auch keine Stelle, an der ein zweites `show` erkennen könnte, dass es das zweite ist.
2. **Es gibt kein Gegenstück zum Setzen.** Kein Zweig verwirft den Zeitpunkt. Dass die Zeile trotzdem nicht dauerhaft falsch stehen bleibt, liegt allein daran, dass sie beim nächsten `show` überschrieben wird — also am Fehler selbst.
3. **Die Fallunterscheidung ist unvollständig.** Behandelt werden vier der sieben Zustände, die die Blazor-Laufzeit meldet; `retrying`, `paused` und `resume-failed` fallen stumm durch, und es gibt kein abschließendes `else` (J15). Solange niemand die Liste vollständig hingeschrieben hat, ist auch nicht entscheidbar, bei welchem Zustand der Zeitpunkt zu setzen und bei welchem er zu verwerfen wäre — die Entscheidung wurde nie getroffen, sie ist ausgefallen.

**Der Kommentar über `nenneStandDesSchirms` (`:66-69`) behauptet bereits das Richtige** — „Der Zeitpunkt ist der des Abrisses und wächst nicht mit" — und beschreibt damit nicht den Code, sondern die Absicht. Die Funktion hält, was er sagt: sie rechnet nicht mit. Die Aufrufstelle hält es nicht.

### Welche Zustände Blazor meldet — nachgesehen, nicht angenommen

Gelesen in `~/.nuget/packages/microsoft.aspnetcore.app.internal.assets/10.0.0/_framework/blazor.web.js` (dieselbe Datei liegt im Veröffentlichungsstand unter `wwwroot/_framework/`). Der Dialog bekommt seine Ereignisse von `UserSpecifiedDisplay.dispatchReconnectStateChangedEvent`; es sind **sieben**, nicht vier:

| Zustand | Wann die Laufzeit ihn meldet (Beleg aus `blazor.web.js`) | Ist der Schirm danach frisch? |
|---|---|---|
| `show` | Konstruktor des Wiederverbindungsvorgangs, angelegt in `onConnectionDown` — auch bei einer vom Server angeordneten Pause (`{type:"pause"}`) | **nein** — der Abriss beginnt hier |
| `retrying` | `update({type:"reconnect",currentAttempt,secondsToNextAttempt})`, im Sekundentakt von `runTimer` während der automatischen Versuche | nein |
| `paused` | `update({type:"pause",remote})` — der Server hat den Kreislauf angehalten; folgt immer auf ein `show` | nein |
| `failed` | `failed()` nach erschöpften automatischen Versuchen, im Wiederverbinden-Modus | nein — der Schirm ist älter, nicht jünger |
| `resume-failed` | derselbe `failed()`-Zweig im Fortsetzen-Modus | nein |
| `hide` | `dispose()` des Wiederverbindungsvorgangs — **und `dispose()` wird ausschließlich aus `onConnectionUp()` gerufen** | **ja** — der Kreislauf steht wieder und der Server zeichnet |
| `rejected` | `rejected()`, der Kreislauf ist endgültig verworfen; das Skript lädt daraufhin die Seite neu | die Seite wird ersetzt |

**`hide` ist damit das einzige Ereignis, das eine echte Rückkehr belegt** — es ist dasselbe Signal, an dem der Dialog sich schließt. Das macht die Entscheidung eindeutig: gesetzt wird beim ersten `show`, verworfen wird bei `hide`.

**Zwei Rückkehrwege gehen am `hide` vorbei** und gehören deshalb ausdrücklich dazu: `retry()` schließt den Dialog nach einem erfolgreichen `Blazor.resumeCircuit()` selbst (`:41`), und `resume()` (`:52`) läuft über denselben Aufruf. Wo das Skript den Dialog selbst schließt, muss es den Zeitpunkt selbst verwerfen — sonst überlebt der alte Abriss eine gelungene Fortsetzung.

### Betroffene Komponenten

- `Source/KanbanC.Blazor/Components/Layout/ReconnectModal.razor.js` — `:13-24` (die Fallunterscheidung), `:26-50` (`retry`), `:52-60` (`resume`), `:70-73` (`nenneStandDesSchirms`). **Die einzige Datei mit einer Verhaltensänderung.**
- `Source/KanbanC.Blazor.Tests/Gestaltung/TrennungsdialogTests.cs` — zwei Tests zementieren die heutige Schreibweise und müssen mitgezogen werden (unten benannt).
- `Source/KanbanC.PlaywrightTests/Tests/TrennungsdialogE2ETests.cs` — hier entsteht der Reproduktionstest.
- **Nicht betroffen:** `ReconnectModal.razor` (kein Markup ändert sich, die Element-Ids bleiben unangetastet), `Ereignisverteiler`, `Verbindungsmarke`, `Verbindungsstand` — die halten die Regel für Fall 1 bereits; die gesamte `KanbanC.WebApi`, `KanbanC.BL`, `KanbanC.Contracts`, Schema und Migrationen.

### Der Test, der genau das prüfen sollte — und es nicht tut

`TrennungsdialogTests.cs` trägt den Test `Wenn_das_Skript_gelesen_wird_dann_nimmt_es_den_Zeitpunkt_genau_einmal` mit dem Kommentar „Der Zeitpunkt wird **einmal** beim Beginn der Trennung genommen und nicht bei jedem Zustandswechsel neu". Er prüft:

```csharp
Assert.That(Regex.Matches(skript, Regex.Escape("nenneStandDesSchirms(")), Has.Count.EqualTo(2), "Aufruf und Definition — mehr Aufrufstellen hieße mehrere Zeitpunkte.");
```

Er zählt **Vorkommen im Quelltext**, nicht Ausführungen. Eine Aufrufstelle, die bei jedem `show` erneut läuft, ist genau eine Aufrufstelle — der Test ist grün und war es immer. Er trägt den Namen der Zusage, ohne sie zu prüfen (Skill `test-ehrlichkeit`); er ist der Grund, aus dem der Fehler `R00032` überlebt hat. Er wird nicht gelöscht, sondern auf das umgestellt, was er tatsächlich belegen kann (Ablage: dass es ein Gegenstück zum Setzen gibt), und die Verhaltensaussage zieht auf die E2E-Ebene, wo ein Browser das Skript wirklich ausführt.

**Ausgeschlossen wurde:** Fall 1. `Ereignisverteiler.MeldeGetrennt` und `Verbindungsmarke.Fuer` verhalten sich richtig, der Wächter `derAbrissIstSchonBekannt` ist vorhanden und in `EreignisverteilerTests` belegt. Ausgeschlossen wurde ebenso das Markup: `ReconnectModal.razor` liefert die Zeile leer aus (`<p id="verbindungsalter"></p>`), was richtig ist und bleibt — der Server kann in diesem Fall nichts rendern.

## Lösungsvorschlag

### Langfristige Lösung

**Der Abrisszeitpunkt wird ein Zustand des Moduls, und die Fallunterscheidung nennt jeden Zustand, den die Laufzeit meldet.**

1. **Ein funktionsübergreifender Zustand im Modulkopf** (J02: der Modulkopf ist genau für funktionsübergreifenden Zustand da) — `let abrisszeitpunkt = null;` neben den bereits dort stehenden Element-Referenzen. `null` heißt „kein Abriss bekannt", dieselbe Bedeutung wie `Verbindungsstand.GetrenntSeit is null` in Fall 1; ein zweites Feld daneben ließe den Zustand zu, den es nicht geben darf.
2. **Beim ersten `show` setzen, bei jedem weiteren nicht.** Mit benannter Bedingung nach dem Vorbild der C#-Seite (`derAbrissIstSchonBekannt`), als `if`, nicht als `??=` (J11 verbietet `??` in neuem Code).
3. **Bei `hide` verwerfen** — die Verbindung stand wieder, der Server hat gezeichnet; der nächste Abriss ist ein neuer und bekommt einen neuen Zeitpunkt. Die Zeile wird dabei **geleert**, damit kein alter Text im geschlossenen Dialog auf sein Wiederauftauchen wartet.
4. **Bei den zwei Rückkehrwegen am `hide` vorbei ebenso verwerfen** — nach erfolgreichem `Blazor.resumeCircuit()` in `retry()` und in `resume()`.
5. **`retrying`, `paused`, `failed`, `resume-failed` lassen den Zeitpunkt unberührt** und stehen als benannte Zweige da, statt stumm durchzufallen; ein abschließendes `else` macht die Fallunterscheidung vollständig (J15).
6. **`rejected` lädt die Seite neu** wie bisher; der Zeitpunkt wird davor verworfen, damit kein Zustand die Seite überlebt, auf der er nicht mehr gilt.

Damit sagt die Zeile genau, was sie behauptet: seit wann der Schirm nicht mehr nachgeführt wird — gemessen vom ersten Abriss, zurückgesetzt erst durch eine belegte Rückkehr.

### Alternative Ansätze

| Option | Achsen | Warum verworfen |
|---|---|---|
| **Den Zeitpunkt aus der Anzeigezeile ablesen** statt ihn zu halten: nur setzen, wenn `verbindungsalter.textContent` leer ist | Komplexität niedrig · Testbarkeit mittel · Reversibilität hoch · Risiko mittel | Der Zustand wohnte dann im Anzeigetext, und die Regel hinge daran, dass niemand die Zeile anders befüllt. Der Text ist außerdem auf Minuten gerundet und nicht zurücklesbar — aus „09:12" entsteht kein Zeitpunkt mehr. Zustand gehört nicht in die Darstellung (J16). |
| **Den Zeitpunkt in `sessionStorage` halten**, damit er den Reload nach `rejected` überlebt | Komplexität mittel · Testbarkeit mittel · Reversibilität mittel · Risiko hoch | Nach einem Reload ist der Schirm **frisch geladen** und gerade nicht alt. Ein überlebender Zeitpunkt wäre dann die Lüge, die diese Anforderung beseitigt — die Option kehrt den Fehler um, statt ihn zu beheben. |
| **Den Zeitpunkt vom Server holen**, sobald die Verbindung wieder steht, und rückwirkend korrigieren | Komplexität hoch · Testbarkeit niedrig · Reversibilität niedrig · Risiko hoch | Der Server ist in diesem Fall **per Definition nicht erreichbar** — das ist der Trenner, an dem `R00032` die beiden Abbrüche unterscheidet. Steht er wieder, ist der Dialog längst zu und die Zeile gegenstandslos. |
| **Die Zeile entfernen** und den Dialog auf den Stand der Vorlage zurücknehmen | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko hoch | Löst den Fehler durch Rücknahme der Zusage. Die Zeile ist der einzige Inhalt, den `R00032` über die Vorlage hinaus gebaut hat, und US-12 verlangt sie ausdrücklich. Ein Dialog, der über sein Alter schweigt, ist ehrlicher als einer, der lügt — aber weniger wert als einer, der es sagt. |

## Test-Strategie

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

### Warum der Reproduktionstest ein E2E-Test ist

Das Skript ist eine ES-Modul-Datei ohne Testläufer: im ganzen Repository gibt es keine `package.json` und keinen zweiten JavaScript-Baustein. Auf der Unit-Ebene existiert nur `TrennungsdialogTests`, das den **Quelltext als Text** liest — und genau diese Bauform hat den Fehler durchgelassen. Der Reproduktionstest gehört deshalb dorthin, wo ein Browser das Skript wirklich ausführt.

**Der Abriss wird nach dem gemessenen Rezept des Bestands hergestellt, und die Reihenfolge ist Teil davon** (`TrennungsdialogProbeE2ETests`, `TrennungsdialogE2ETests.TrenneDenBrowserVonDerAnwendung`): ein Init-Skript ersetzt `WebSocket` durch eine sammelnde Ableitung, der Test schließt die gesammelten Verbindungen, und **erst danach** kommt `Context.SetOfflineAsync(true)` dazu. `SetOfflineAsync` allein reißt eine schon offene Verbindung **nicht** — das ist in `TrennungsdialogProbeE2ETests` gemessen und steht dort als Beleg. In der umgekehrten Reihenfolge bleibt der Sockel in `CLOSING` hängen, meldet nie ein `close`, und Blazor erfährt vom Abriss nichts. Der neue Test benutzt dasselbe Vorgehen und erfindet kein zweites.

### Reproduktionstest 1 — der Zeitpunkt bleibt der des ersten Abrisses

`TrennungsdialogE2ETests`, neu: `Wenn_der_Dialog_erneut_in_show_geht_dann_nennt_er_weiterhin_den_ersten_Abriss`

- *Arrange:* die Browseruhr wird beherrschbar gemacht, **bevor** die Seite geladen wird (Weg unten), und steht auf 09:12. Danach der Abriss nach dem Rezept des Bestands; der Dialog steht und nennt „Stand von 09:12".
- *Act:* die Uhr wird auf 10:47 gestellt. Dann geht der Dialog ein zweites Mal in `show`, **ohne** dass dazwischen ein `hide` lag.
- *Assert:* `#verbindungsalter` nennt weiterhin **09:12**. Zusätzlich: kein `pageerror` in der Browserkonsole, und `#blazor-error-ui` bleibt unsichtbar.
- *Warum ohne Fix rot:* `nenneStandDesSchirms(new Date())` läuft erneut und schreibt 10:47.

**Die beherrschbare Uhr ist Pflicht, nicht Bequemlichkeit.** Die Zeile ist auf Minuten gerundet; ein zweites `show` wenige Sekunden nach dem ersten nennt ohnehin dieselbe Uhrzeit, und ein Test ohne Zeitsprung wäre **auch ohne Fix grün**. Er prüfte dann die Rundung, nicht die Regel.

- **Erster Weg (Hausmuster):** `Date` im Init-Skript um einen steuerbaren Versatz erweitern — dieselbe Bauform, mit der `SammleVerbindungen` schon `WebSocket` ersetzt, und ohne neue Werkzeug-Abhängigkeit.
- **Zweiter Weg:** `Page.Clock.SetFixedTimeAsync` (Playwright 1.62 kann es). **Vor dem Einsatz zu belegen** (Skill `dependency-probe`): dass es die Zeit auch auf einer bereits geladenen Seite ändert und dabei **keine** Zeitgeber verstellt — die automatischen Wiederverbindungsversuche und der SignalR-Herzschlag hängen an `setTimeout`, und eine gefälschte Zeitgeberachse nähme dem Test den Abriss, den er misst. Ergibt die Probe das nicht, gilt der erste Weg.

**Wie das zweite `show` entsteht:** durch dasselbe Ereignis, das die Laufzeit selbst schickt —

```js
document.getElementById("components-reconnect-modal")
        .dispatchEvent(new CustomEvent("components-reconnect-state-changed", { detail: { state: "show" } }));
```

Das ist kein Ersatz des Prüfgegenstands: der Dialog steht aus einem **echten** Abriss, das echte Skript läuft im echten Browser, und geprüft wird der echte Anzeigetext. Ersetzt wird allein der Auslöser, und zwar durch den Wortlaut, den `dispatchReconnectStateChangedEvent({state:"show"})` in `blazor.web.js` erzeugt. Der Grund ist Bestimmbarkeit: ein zweites `show` aus dem Betrieb hängt an einer Verbindung, die im richtigen Zehntelsekundenfenster kommt und wieder geht — daraus wird kein Test, sondern ein Würfel.

### Reproduktionstest 2 — nach einer echten Rückkehr beginnt die Alterung neu

`TrennungsdialogE2ETests`, neu: `Wenn_die_Verbindung_wirklich_zurueckkam_dann_nennt_der_naechste_Abriss_seinen_eigenen_Zeitpunkt`

- *Act:* Uhr auf 09:12, erster Abriss, Dialog nennt 09:12 · `SetOfflineAsync(false)`, der Dialog schließt (belegt in `TrennungsdialogProbeE2ETests`) · Uhr auf 10:47 · zweiter Abriss nach demselben Rezept.
- *Assert:* der Dialog nennt jetzt **10:47**.
- *Warum er gebraucht wird:* er verhindert die billigste Falschlösung — „den Zeitpunkt nach dem ersten Mal nie wieder anfassen". Ohne ihn wäre ein Fix grün, der den Dialog beim zweiten Abriss ein Alter von Stunden erfinden lässt. Er ist **ohne Fix grün** und deshalb kein Reproduktionstest, sondern die Zusicherung der Gegenrichtung; er darf nicht als Nachweis der Behebung gezählt werden.

### Unit-Ebene: was die Ablage belegen kann und was nicht

`TrennungsdialogTests` bleibt die Ablageprüfung (Sprache, Kennungen, leere Zeile im Markup) und bekommt **keine** neue Verhaltensaussage — Textzählungen können keine belegen. Zwei bestehende Tests ziehen mit:

- `Wenn_das_Skript_gelesen_wird_dann_schreibt_es_den_Stand_beim_Beginn_der_Trennung` erwartet heute wörtlich `nenneStandDesSchirms(new Date())`. Diese Schreibweise verschwindet mit dem Fix; die Zusicherung wird auf das umgestellt, was bleibt (die Zeile „Was du siehst, ist der Stand von" und `verbindungsalter.textContent`).
- `Wenn_das_Skript_gelesen_wird_dann_nimmt_es_den_Zeitpunkt_genau_einmal` wird umbenannt und auf das umgestellt, was ein Textvergleich tragen kann: dass es zum Setzen ein Verwerfen gibt und `new Date()` nicht mehr im Aufruf steht. Sein bisheriger Name bleibt nicht stehen — er versprach eine Verhaltensaussage, die jetzt E2E belegt ist.

### Regressionstests

- Die vier bestehenden Tests in `TrennungsdialogE2ETests` bleiben **unverändert** grün, insbesondere der Abgleich `^Was du siehst, ist der Stand von \d\d:\d\d\.$` und das Schließen des Dialogs bei Rückkehr.
- `TrennungsdialogProbeE2ETests` bleibt **unverändert** — es ist der Beleg für die Reihenfolge und wird nicht angefasst.
- Die Suiten aus `R00001`–`R00045` bleiben ohne Änderung grün, insbesondere die von `R00032`: `AufschliessenE2ETests`, `VerbindungsmarkeTests`, `EreignisverteilerTests`, `VerbindungsstandGestaltungTests`.

## Akzeptanzkriterien

### Der genannte Zeitpunkt ist der des ersten Abrisses

- [ ] Geht der Dialog ein zweites Mal in `show`, ohne dass dazwischen ein `hide` lag, nennt `#verbindungsalter` **weiterhin den Zeitpunkt des ersten Abrisses**. Rechenbeispiel: Abriss 09:12, zweites `show` um 10:47 → die Zeile sagt „Was du siehst, ist der Stand von 09:12." und nicht 10:47.
- [ ] Das gilt unabhängig davon, wie oft `show` erneut eintrifft und wie viel Zeit dazwischen liegt: nach drei weiteren `show` um 10:47, 11:03 und 12:30 steht dort unverändert 09:12.
- [ ] Der Abrisszeitpunkt wird **gehalten**, nicht aus dem Anzeigetext zurückgelesen: es gibt einen Modulzustand, dessen `null`-Wert „kein Abriss bekannt" heißt.
- [ ] `retrying`, `paused`, `failed` und `resume-failed` lassen den Zeitpunkt **unberührt**. Rechenbeispiel: Abriss 09:12, danach fünf `retrying` und ein `failed` → die Zeile sagt weiterhin 09:12.

### Verworfen wird er nur bei einer belegten Rückkehr

- [ ] Bei `hide` wird der Zeitpunkt verworfen **und die Zeile geleert** — `hide` wird ausschließlich aus `onConnectionUp()` gemeldet und ist damit das einzige Ereignis, das eine echte Rückkehr belegt.
- [ ] Kam die Verbindung wirklich zurück, nennt der **nächste** Abriss seinen eigenen Zeitpunkt. Rechenbeispiel: Abriss 09:12 → Rückkehr → Abriss 10:47 → die Zeile sagt 10:47.
- [ ] Schließt das Skript den Dialog **selbst** nach einem erfolgreichen `Blazor.resumeCircuit()` — in `retry()` und in `resume()` —, verwirft es den Zeitpunkt an derselben Stelle. Ein alter Abriss überlebt keine gelungene Fortsetzung.
- [ ] Bei `rejected` wird der Zeitpunkt verworfen, bevor die Seite neu geladen wird; er überlebt den Reload nicht. Begründung im Kriterium: nach einem Reload ist der Schirm frisch geladen und gerade nicht alt.

### Die Fallunterscheidung ist vollständig

- [ ] Der Zustandszweig nennt **alle sieben** Zustände, die die Laufzeit meldet — `show`, `retrying`, `paused`, `failed`, `resume-failed`, `hide`, `rejected` — und schließt mit einem ausdrücklichen `else` (J15). Kein Zustand fällt stumm durch.
- [ ] Ein wiederholtes `show` erzeugt **keinen** `pageerror` in der Browserkonsole, und `#blazor-error-ui` bleibt unsichtbar.
- [ ] Was die Vorlage tut, tut sie weiter: `Blazor.reconnect()`, `Blazor.resumeCircuit()`, das Wiederaufnehmen über `visibilitychange` bei `failed` und der Reload bei `rejected` bleiben in Wirkung und Reihenfolge unverändert.

### Der Reproduktionstest ist ehrlich

- [ ] Der Reproduktionstest ist ohne Fix **rot** und mit Fix grün.
- [ ] Er arbeitet mit einer **beherrschbaren Browseruhr** und einem Zeitsprung über eine Minutengrenze. Begründung im Kriterium: die Zeile ist auf Minuten gerundet, ein Test ohne Zeitsprung wäre auch ohne Fix grün und prüfte die Rundung statt der Regel.
- [ ] Er stellt den Abriss nach dem gemessenen Rezept des Bestands her: Verbindungen im Init-Skript sammeln, im Browser schließen, **danach** `SetOfflineAsync(true)`. Die Reihenfolge wird nicht getauscht — in der umgekehrten bleibt der Sockel in `CLOSING` und meldet nie ein `close`.
- [ ] Der Test der Gegenrichtung (nach echter Rückkehr ein neuer Zeitpunkt) existiert und ist als **ohne Fix grün** gekennzeichnet; er wird nicht als Nachweis der Behebung gezählt.
- [ ] `TrennungsdialogProbeE2ETests` bleibt **unverändert** — die Probe ist der Beleg für die Reihenfolge und kein Prüfgegenstand dieser Anforderung.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] **Benannte Änderung 1:** `Source/KanbanC.Blazor/Components/Layout/ReconnectModal.razor.js` — Modulzustand, vollständige Fallunterscheidung, Verwerfen an den drei Stellen. Die **einzige** Datei mit einer Verhaltensänderung.
- [ ] **Benannte Änderung 2:** `TrennungsdialogTests.cs` — `Wenn_das_Skript_gelesen_wird_dann_schreibt_es_den_Stand_beim_Beginn_der_Trennung` verliert die Erwartung `nenneStandDesSchirms(new Date())`; `Wenn_das_Skript_gelesen_wird_dann_nimmt_es_den_Zeitpunkt_genau_einmal` wird umbenannt und auf eine Aussage umgestellt, die ein Textvergleich tragen kann. Die vier Tests über Sprache, Umlaute, Kennungen und leeres Markup bleiben **unverändert**.
- [ ] **Benannte Änderung 3:** `TrennungsdialogE2ETests.cs` — zwei neue Tests; die vier bestehenden bleiben unverändert grün.
- [ ] **`ReconnectModal.razor` wird nicht angefasst.** Die Element-Ids (`components-reconnect-modal`, `components-reconnect-button`, `components-resume-button`, `components-seconds-to-next-attempt`, `verbindungsalter`) und die `components-*-visible`-Klassen bleiben unverändert — das Blazor-Laufzeitteil findet den Dialog über sie.
- [ ] Es entsteht **kein Gestaltungswert** in einer Komponenten-CSS-Datei; diese Behebung ändert nichts am Aussehen. Träte doch ein Wert hinzu, käme er aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`.
- [ ] `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL` (`CLAUDE.md`, „Die eine Regel, die den Aufbau trägt"). Die Behebung ist reines Browserskript und braucht keine.
- [ ] **Keine Änderung** an `KanbanC.WebApi`, `KanbanC.BL`, `KanbanC.Contracts`, am Schema, an Migrationen oder an einem Paket.
- [ ] Fall 1 bleibt unberührt: `Ereignisverteiler`, `Verbindungsstand` und `Verbindungsmarke` werden nicht geändert; ihre Tests bleiben grün.
- [ ] Keine neuen Fehler: alle Tests aller Ebenen grün, Coverage nicht gefallen, `TreatWarningsAsErrors` erfüllt.

## Implementierungshinweise

- **Vor dem Schreiben `~/.claude/skills/javascript-stil/SKILL.md` laden.** Einschlägig sind J02 (funktionsübergreifender Zustand gehört in den Modulkopf, alles andere nicht), J07 (benannte Aussage statt nackter Bedingung), J11 (`let` nur bei echter Neuzuweisung, **kein `??`** und kein Ternary in neuem Code — also `if`, nicht `abrisszeitpunkt ??= new Date()`), J15 (sichtbarer Fehlerpfad und vollständige Fallunterscheidung mit `else`), J16 (Zustand nicht in der Darstellung halten).
- **Die Benennung folgt der C#-Seite.** `Ereignisverteiler.MeldeGetrennt` nennt seinen Wächter `derAbrissIstSchonBekannt`; dasselbe Wort im Skript macht sichtbar, dass es dieselbe Regel ist. `abrisszeitpunkt` für den Zustand — C07 gilt auch hier (Bezeichner ohne echte Umlaute, Anzeigetexte mit).
- **Der Kommentar bei `nenneStandDesSchirms` bleibt inhaltlich richtig** und beschreibt ab jetzt auch die Aufrufstelle. Er wird nicht um eine Änderungsgeschichte ergänzt (J11: Kommentare sagen Fachliches, nicht Entwurfsgeschichte).
- **Verwerfen heißt Zustand *und* Zeile.** Ein zurückgesetzter Zustand bei stehen gebliebenem Text erzeugt einen Dialog, der beim nächsten Öffnen kurz den alten Zeitpunkt zeigt, bevor der neue kommt.
- **Die Fallunterscheidung wächst von vier auf sieben Zweige.** Wird sie dadurch unleserlich, ist die Ablösung durch eine Zuordnung Zustand → Handlung der Weg (J08/J12), nicht eine tiefere `if`-Kette. Die vier Zustände ohne Wirkung auf den Zeitpunkt dürfen sich einen Zweig teilen, solange alle vier namentlich dastehen.
- **`showModal()` auf einem schon offenen Dialog** ist beim zweiten `show` der Fall. Ob der Browser das still hinnimmt oder wirft, ist unbelegt (siehe Missing-Docs) — der erste Testlauf ist das Orakel. Wirft er, bekommt der Aufruf einen Wächter auf `reconnectModal.open`; nimmt er es hin, bleibt der Aufruf, wie er ist. Das Kriterium „kein `pageerror`" gilt in beiden Fällen.
- **Nicht mit aufräumen:** die englischen Kommentare aus der Vorlage (`:1`, `:31-34`, `:44`) und die englischen Funktionsnamen (`handleReconnectStateChanged`, `retry`, `resume`) bleiben stehen. Sie sind kein Anzeigetext, `R00032` hat sie bewusst gelassen, und ein Umbenennen machte den Diff dieser Behebung unlesbar (J17: ein Commit, ein Thema).

## Offene Fragen

- **Welchen Weg nimmt die beherrschbare Uhr im Test?** — **nicht entschieden, im stillen Lauf mit dem Hausmuster gebaut:** `Date` im Init-Skript, wie `SammleVerbindungen` schon `WebSocket` ersetzt. `Page.Clock` bleibt die Alternative, sobald die Probe belegt, dass es die Zeitgeber der automatischen Wiederverbindung nicht verstellt. Vor der Umsetzung zu bestätigen.
- **Soll die Zeile sekundengenau werden**, damit ein Sprung auch innerhalb einer Minute sichtbar wäre? — **nein, im stillen Lauf so gelassen.** `R00032` hat die Minutenform gewählt, sie steht in US-12 und im bestehenden Abgleich `\d\d:\d\d`; eine Sekundenangabe an einem Schirm, der stundenlang stehen kann, wäre eine erfundene Genauigkeit. Die Testbarkeit wird über die Uhr hergestellt, nicht über das Format.
- **Soll der Dialog zusätzlich sagen, dass ein Wiederverbinden bereits gescheitert ist?** — **nein, out of scope.** Die Zeile „Erneut verbinden ist gescheitert … nächster Versuch in N Sekunden." leistet das bereits über die `components-reconnect-repeated-attempt-visible`-Klasse der Vorlage.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist ein Dialog, der genau in der einen Zusage falsch liegt, für die er über die Vorlage hinaus gebaut wurde: ein Mensch soll wissen, wie alt das Bild vor ihm ist, und bekommt stattdessen die jetzige Uhrzeit auf einem Schirm, der seit einer Stunde nicht mehr nachgeführt wird. Das Zielbild ist die Kehrseite von „Live überall": wo die Oberfläche gerade nicht live ist, sagt sie es — und sagt richtig, seit wann. Die Kausalkette: **wenn** der Abrisszeitpunkt zu einem Zustand des Moduls wird, der beim ersten `show` entsteht (X), **dann** kann kein weiteres `show` ihn mehr vorschieben, weil es den bereits bekannten Abriss vorfindet statt einer leeren Aufrufstelle (Y), **und dann** nennt der Dialog durchgehend den Zeitpunkt, ab dem der Schirm stehen geblieben ist — die Angabe wird mit der Dauer der Trennung genauer statt falscher (Z). Der Hebel liegt an der Aufrufstelle und nicht in `nenneStandDesSchirms`: die Funktion rechnet bereits nichts mit und ist unschuldig; falsch ist allein, **wann** sie gerufen wird. Und nicht in der Anzeige: eine Zeile, die den Text nur seltener überschriebe, wäre dieselbe Lüge mit anderer Frequenz. Dass die Fallunterscheidung dabei vollständig wird, ist keine Zugabe, sondern die Bedingung der Behebung — solange drei der sieben Zustände nirgends genannt sind, ist „bei welchem Zustand gilt was" nicht beantwortet, sondern offen.

## Missing-Docs

- **Die sieben Zustände von `components-reconnect-state-changed`** (`show`, `retrying`, `paused`, `failed`, `resume-failed`, `hide`, `rejected`), ihre Auslöser und die Zusicherung, dass `hide` ausschließlich aus `onConnectionUp` stammt, sind im Repository nirgends festgehalten und mussten aus `blazor.web.js` gelesen werden. Der Befund gehört nach `Dokumentation/Bibliotheken/`, falls er sich in der Microsoft-Dokumentation nicht belegen lässt.
- **Verhalten von `HTMLDialogElement.showModal()` auf einem bereits modal geöffneten Dialog** — still oder `InvalidStateError` — ist unbelegt und entscheidet darüber, ob der Aufruf einen Wächter braucht.
- **`Page.Clock` in Playwright .NET 1.62:** ob `SetFixedTimeAsync` auf einer bereits geladenen Seite wirkt und ob es Zeitgeber unangetastet lässt, ist unbelegt und entscheidet über den Weg der beherrschbaren Uhr im Test.

## Notizen

- **Priorität: Hoch.** Kein Datenverlust, aber eine Falschaussage genau dort, wo die Anwendung um Vertrauen bittet — und sie wird mit der Dauer der Störung schlimmer, also genau dann, wenn sie am meisten zählt.
- **Betroffene Nutzer/Systeme:** jeder Mensch am Browser, dem der Kreislauf abreißt. **KI-Agenten sind nicht betroffen** — sie sprechen die WebApi über HTTP an, ohne SignalR und ohne diesen Dialog.
- **Workaround bis zur Behebung:** die Seite neu laden. Danach ist der Schirm tatsächlich frisch, und die Frage nach seinem Alter stellt sich nicht mehr.
- **Der Fehler stammt aus `I0029`/[`R00032`](R00032-nach-verbindungsabbruch-aufschliessen.md)** und ist bereits im Trunk. Er ist keine übersehene Anforderung: `R00032` hat die Regel für Fall 1 ausdrücklich formuliert und gebaut („Der Zeitpunkt ist der des **Abrisses**"), sie aber im Browserskript nicht wiederholt.
- **Kein WBS-Knoten.** Bug-Anforderungen tragen keine Requirement-Klammer in `Dokumentation/Planung/kanbanc.md`; `D0007` bleibt, wie es ist.

### Verworfene Alternativen

Vollständig mit Achsen und Begründung unter „Lösungsvorschlag → Alternative Ansätze": aus der Anzeigezeile ablesen · in `sessionStorage` halten · vom Server holen · die Zeile entfernen.

Dazu zwei Formen, die gar nicht erst in die Tabelle kamen:

| Option | Warum verworfen |
|---|---|
| **Den Zeitpunkt nach dem ersten Setzen nie wieder anfassen** | Wäre für Reproduktionstest 1 grün und für den Betrieb falsch: nach einer echten Rückkehr und einem zweiten Abriss erfände der Dialog ein Alter von Stunden. Genau dagegen steht Reproduktionstest 2. |
| **Die Verhaltenszusage weiter über einen Textvergleich in `TrennungsdialogTests` prüfen** | Diese Bauform hat den Fehler durchgelassen: `Has.Count.EqualTo(2)` zählt Vorkommen im Quelltext, nicht Ausführungen. Ein Test, der eine Verhaltensaussage im Namen trägt, gehört dorthin, wo das Verhalten stattfindet. |

### Bewusst out of scope

- **Fall 1** (Blazor ↔ WebApi) — `Ereignisverteiler`, `Verbindungsmarke` und `Verbindungsstand` halten die Regel bereits und werden nicht angefasst.
- **Die englischen Kommentare und Funktionsnamen der Vorlage** im Skript — siehe Implementierungshinweise.
- **Ein Aufschließen nach Fall 2.** `R00032` hat belegt, dass der Kreislauf serverseitig weiterlebt und beim Wiederanschluss den aktuellen Stand zeigt; daran ändert diese Behebung nichts.
- **Eine sekundengenaue oder mitlaufende Altersangabe** — siehe Offene Fragen.

### Angenommen im stillen Lauf

- **`hide` gilt als Beleg einer echten Rückkehr**, weil die Laufzeit es ausschließlich aus `onConnectionUp()` meldet und der Dialog sich an demselben Signal schließt. Es ist das beste verfügbare Signal; ein zweites gibt es nicht.
- **Die vier Zustände `retrying`, `paused`, `failed` und `resume-failed` lassen den Zeitpunkt unberührt** — keiner von ihnen belegt, dass der Server wieder gezeichnet hat.
- **`rejected` verwirft den Zeitpunkt**, obwohl der folgende Reload ihn ohnehin abräumt: die Regel steht dann an allen Stellen gleich da, statt an einer über den Reload zu argumentieren.
- **Der Test erzeugt das zweite `show` durch dasselbe Ereignis, das die Laufzeit schickt.** Der Auslöser wird ersetzt, der Prüfgegenstand nicht — ein zweites `show` aus dem Betrieb wäre ein Würfel statt eines Tests.
- **Die beherrschbare Uhr entsteht im Init-Skript**, nicht über `Page.Clock` — Hausmuster vor neuer Werkzeug-Abhängigkeit, solange die Probe fehlt.
