---
id: R00050
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: R00021
---

# R00050: Behebung der angebotenen Handlungen, die ins Gesperrte führen

## Beschreibung

Auf der Kartenseite `/karten/{karteId}` bietet die leere Dateiverweis-Hälfte die Handlung „eintragen" an. Ein Klick darauf setzt den Fokus in das Feld `#dateiverweis-eingabe` — **auch dann, wenn keine Identität gewählt ist und das Feld deshalb `disabled` ist**. Ein `disabled`-Element ist nach HTML nicht fokussierbar; der Aufruf verpufft, der Fokus bleibt auf dem Knopf stehen, und wer der angebotenen Handlung folgt und danach tippt, tippt ins Leere. Es erscheint keine Meldung, es geschieht nichts.

**Schritte:** frischer Browserkontext ohne gewählte Identität · Karte mit Anhang, ohne Dateiverweis · `/karten/{karteId}` öffnen · in der Zeile „Keine Dateiverweise · eintragen" auf **eintragen** klicken · `Dokumentation/Planung/kanbanc.md` tippen.
**Erwartet:** die Handlung führt dorthin, wo eingetragen wird — oder sie nennt den Grund und die Kompensationsaktion.
**Tatsächlich:** nichts geschieht. Das Feld bleibt gesperrt und ohne Fokus, der getippte Pfad landet nirgends, und die einzige Auskunft ist der Hinweis, der ohnehin schon dastand.

Zahlt ein auf: [Vision](R00000-vision.md) — „Wer die Oberfläche öffnet, wählt aus, wer er ist" (Full-Trust-Modell) und die visuelle Haltung an Kanbanflow: eine angebotene Handlung, die nichts tut, ist Beiwerk, das nicht zurücktritt, sondern in die Irre führt.

**Der Fehler stammt aus `I0019`/[`R00021`](R00021-karte-auf-dateien-verweisen-lassen.md) „Karte auf Dateien verweisen lassen" und ist bereits im Trunk.** Beleg: **Anmerkung 322** des Laufs (`kanbanC-anmerkungen.md:329`) — „Offene Ecke, nicht gebaut: `#dateiverweis-eintragen` fokussiert bei fehlender Identitaet ein **gesperrtes** Feld. Das Kriterium ist ueber den E2E-Test *mit* Identitaet erfuellt; der Fall ohne Identitaet ist weder gebaut noch geprueft."

### Der Fall ist klein, aber er ist eine Familie

Der Bestand wurde daraufhin **durchgezählt**, nicht vermutet: die Kartenseite bietet genau **fünf** Handlungen der Bauart `blatthandlung` an — das sind alle Stellen, an denen ein Leerzustand statt einer Null eine Handlung anbietet. Zwei davon führen ins Gesperrte:

| Handlung | Ziel | Zustand ohne Identität | Befund |
|---|---|---|---|
| `#dateiverweis-eintragen` (`:356`) | `#dateiverweis-eingabe` (`:441`) | Ziel ist `disabled` | **Defekt — der gemeldete Fall** |
| `#anhang-hinzufuegen` (`:342`, `:349`) | `#anhang-datei` (`:395`) | Ziel ist `disabled` | **Defekt — derselbe Bauplan, per Label statt Fokus** |
| `#kommentar-schreiben` (`:276`) | `#kommentar-eingabe` (`:294`) | Ziel ist **nicht** gesperrt | in Ordnung |
| `#teilaufgabe-anlegen` (`:215`) | `#teilaufgabe-eingabe` (`:225`) | Teilaufgaben kennen keinen Urheber | in Ordnung |
| `#beschreibung-hinzufuegen` (`:143`) | öffnet das Beschreibungsfeld | nicht identitätsgebunden | in Ordnung |

Der zweite Defekt ist derselbe Fehler in anderer Mechanik: `#anhang-hinzufuegen` ist ein `<label for="anhang-datei">`, und ein Label, dessen Steuerelement `disabled` ist, hat nach HTML **kein Aktivierungsverhalten** — der Klick öffnet keinen Dateiwähler und tut sonst nichts. Er trifft zwei Sperrgründe, nicht einen: keine Identität gewählt (`R00020`) **und** ein laufendes Anhängen (`R00024`, `Ablegeflaechenstand.IstGesperrt`). Der zweite Grund ist im Leerzustand real, weil während des ersten Anhängens noch keine Anhangzeile steht und die Leerzeile mit ihrem „hinzufügen" deshalb weiter sichtbar ist.

**Die Gegenprobe steht schon im Bestand und ist richtig gebaut:** `#timer-starten` (`:539`) ist die einzige identitätsgebundene Handlung der Seite, die den Fall behandelt — sie öffnet ohne gewählte Identität die Identitätswahl und startet nach der Wahl **ohne zweiten Klick** ([`R00026`](R00026-timer-starten.md), Kriterium `:96`). Diese Anforderung zieht die beiden anderen Stellen auf denselben Stand, statt einen zweiten Umgang mit demselben Zustand zu erfinden.

Ausserhalb der Kartenseite wurde ebenfalls nachgesehen: `Import.razor` und `Boards.razor` führen dieselbe Ablegefläche, bieten aber **keine** zweite Handlung daneben an — dort ist das gesperrte Element selbst das angeklickte, und der Grund steht als Hinweis darunter. Kein Befund.

## Ursachenanalyse

### Root Cause

**Die Handlung prüft den Zustand nicht, in dem sie handelt.**

`Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor:1830`

```csharp
private async Task FokussiereDateiverweisfeld()
{
    await _dateiverweisfeld.FocusAsync();
}
```

Der Aufruf ist bedingungslos. Das Ziel ist es nicht — `:440-445`:

```razor
<input class="input dateiverweisfeld"
       id="dateiverweis-eingabe"
       @ref="_dateiverweisfeld"
       placeholder="Pfad im Repository eintragen"
       disabled="@(_urheber is null)"
       …
```

`ElementReference.FocusAsync` ruft browserseitig `element.focus()`. Ein `disabled`-Formularelement ist nach HTML nicht fokussierbar, `focus()` ist darauf ein No-Op und wirft nicht. **Der Aufruf scheitert also lautlos** — es gibt weder eine Ausnahme noch eine Meldung noch eine Zustandsänderung. Genau darum ist der Fehler seit `I0019` unentdeckt geblieben.

Dieselbe Wurzel an der zweiten Stelle, `:342`/`:349` gegen `:395`:

```razor
<label class="blatthandlung" id="anhang-hinzufuegen" for="anhang-datei">hinzufügen</label>
…
<InputFile class="ablegefeld" id="anhang-datei" OnChange="NimmDatei" disabled="@AblegeflaecheIstGesperrt" />
```

Hier gibt es nicht einmal C#-Code, der prüfen könnte: die Kupplung ist das `for`-Attribut, und sie verliert ihre Wirkung still, sobald das Ziel gesperrt ist.

**Ausgeschlossen wurde:** die Sperre selbst. Sie ist richtig und wird nicht angetastet — `R00020` und `R00021` haben ausdrücklich entschieden, dass ohne Urheber kein Anhang und kein Dateiverweis entsteht, und `R00024` hat den zweiten Sperrgrund am Anhängen begründet. Ausgeschlossen wurden ebenso WebApi, `KanbanC.BL`, Contracts, Schema und Migrationen: es wird nichts geschrieben, nichts gelesen, keine Route berührt. **Der Fehler liegt vollständig in der Oberfläche.**

### Betroffene Komponenten

- `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` — `:342`, `:349`, `:356` (die drei angebotenen Handlungen), `:395`, `:440-445` (die zwei gesperrten Ziele), `:556-566` (das heutige Identitätspopover im Zeitenblock), `:1242` (`_identitaetswahlIstOffen`), `:1324-1362` (`StarteTimer`, `OeffneIdentitaetswahl`, `UebernimmIdentitaetUndStarte` — das Vorbild), `:1546-1560` (Escape und Schließen), `:1830` (`FokussiereDateiverweisfeld`).
- `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor.css` — die Popover-Regeln, die heute nur unter dem Zeitenblock gelten.
- `Source/KanbanC.Blazor/Components/Layout/Identitaetswahl.razor` — wird **unverändert** wiederverwendet.
- `Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs` — wächst um Locator; die bestehenden bleiben unverändert.
- `Source/KanbanC.PlaywrightTests/Tests/DateiverweisAnKarteE2ETests.cs`, `DateiAnKarteHaengenE2ETests.cs` — je ein Reproduktionstest kommt hinzu.
- **Nicht betroffen:** WebApi, `KanbanC.BL`, `KanbanC.Contracts`, Schema, Migrationen, `Components/Layout/Kopfzeile.razor`. Es gibt keine Datenbankänderung und keine Vertragsänderung.

### Der Test, der den Fehler heute verdeckt

`DateiverweisAnKarteE2ETests.cs:446-454` prüft genau diese Handlung — aber über den Aufbau `FrischeKarte(…)`, der in `:472` **die Identität wählt**:

```csharp
public async Task Wenn_die_Handlung_der_halben_Zeile_geklickt_wird_dann_steht_der_Cursor_im_Eingabefeld()
{
    var seite = await FrischeKarte(mitAnhang: true, mitDateiverweis: false);
    await seite.DateiverweisLeerstand.Locator("#dateiverweis-eintragen").ClickAsync();
    await Expect(seite.Dateiverweisfeld).ToBeFocusedAsync();
}
```

Der Test ist richtig und bleibt unverändert. Er deckt den Fehler nur deshalb nicht auf, weil sein Aufbau den einen Zustand nicht herstellt, in dem er auftritt — dieselbe Lehre wie bei den Zwischenzusicherungen aus [`R00024`](R00024-stiller-anhangverlust-beim-zweiten-ablegen.md): ein Test, dessen Aufbau die Vorbedingung des Fehlers ausschließt, prüft nicht, was sein Name verspricht.

## Lösungsvorschlag

### Langfristige Lösung

**Eine angebotene Handlung führt aus — oder sie nennt den Grund und die Kompensationsaktion. Sie tut nie nichts.**

Das ist dieselbe Zusage, die dieses Projekt seinen API-Fehlerantworten macht (Grund mit Werten plus Kompensationsaktion, auch bei 404), hier für die Oberfläche. Drei Teile:

1. **Die Regel wird prüfbare Logik, nicht ein Vorsatz.** Der Zustand „was tut diese Handlung gerade?" wird als pure Operation `Handlungsantwort` nach `Source/KanbanC.Blazor/Services/` gezogen — Muster `Ablegeflaechenstand` und `Anhangausfall` aus `R00024`, und der Grund, aus dem `KanbanC.Blazor.Tests` existiert (`CLAUDE.md`, Abweichung 4). Sie beantwortet aus dem Zustand (`Identität gewählt?`, `läuft ein Anhängen?`) genau eine von drei Antworten: **ausführen**, **Identität erfragen**, **auf den laufenden Vorgang verweisen**. Ohne Browser prüfbar, in allen Kombinationen.

2. **Fehlende Identität wird erfragt, wie beim Timer.** Ein Klick auf „eintragen" oder „hinzufügen" ohne gewählte Identität öffnet die **Identitätswahl** an der Stelle, an der geklickt wurde; nach der Wahl geschieht **unmittelbar** das, was die Handlung versprochen hat — der Cursor steht im Dateiverweisfeld, beziehungsweise die Ablegefläche ist frei —, ohne zweiten Klick. Das ist wörtlich die Entscheidung aus [`R00026`](R00026-timer-starten.md) (`:33`, `:96`): „nicht gewählt" ist eine fehlende Angabe, keine Regelverletzung, und die Wahl ist die Kompensationsaktion.

3. **Popover, Auffangfläche und Escape-Behandlung entstehen einmal, nicht dreimal.** Sie werden aus dem Zeitenblock in eine Komponente `Identitaetsfrage.razor` gezogen, die an jeder der drei Stellen sitzt und sich öffnet, wenn das offene Vorhaben ihres ist. Das ist die Bedingung dafür, dass Teil 2 keine dritte und vierte Kopie desselben Markups erzeugt — Anmerkung 413 hält die schon bestehende Verdopplung fest, und der Zeitenblock hört mit diesem Slice auf, ein Sonderfall zu sein.

**Zwei Dinge bleiben ausdrücklich, wie sie sind:**

- **Die Sperren.** Kein Feld wird geöffnet, keine Bedingung gelockert. Der Fehler ist nicht die Sperre, sondern die Handlung, die sie ignoriert.
- **Der Wortlaut der Leerzeilen.** „Keine Anhänge, keine Dateiverweise · hinzufügen", „Keine Anhänge · hinzufügen", „Keine Dateiverweise · eintragen" bleiben zeichengleich; sie sind an sechs Stellen im E2E-Bestand hart zugesichert, und die Zeile ist kein Sperranzeiger. Was sich ändert, ist allein, was der Klick tut.

**Der laufende Anhängevorgang bekommt eine andere Antwort als die fehlende Identität**, weil er eine andere Kompensationsaktion hat: warten. Ein Klick auf „hinzufügen", während `wbs-export.md` läuft, öffnet deshalb **keine** Identitätswahl, sondern lässt den Grund sichtbar stehen, der bereits eine Zeile tiefer steht („‚wbs-export.md‘ wird angehängt …", `Ablegeflaechenstand.Text`). Geprüft wird, dass er in diesem Moment **sichtbar** ist — nicht bloß, dass nichts passiert.

### Alternative Ansätze

| Option | Achsen | Warum verworfen |
|---|---|---|
| **Die Handlung im gesperrten Zustand ausblenden oder ausgrauen** | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko mittel | Bricht den zeichengleichen Wortlaut der Leerzeilen (sechs harte Zusicherungen) und nimmt dem Menschen den einzigen Weg, der ihn aus dem Zustand herausführt: er sieht dann, dass er nicht darf, aber nicht, wie er dürfte. `R00026` hat für denselben Zustand ausdrücklich anders entschieden. |
| **Den Fokus auf die Identitätswahl in der Kopfzeile setzen** | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko mittel | Führt den Blick quer über den Schirm weg von der Stelle, an der gearbeitet wird, und lässt den Menschen die Wahl selbst öffnen — ein zweiter und ein dritter Klick für dieselbe Absicht. Der Bestand kann es an einer Stelle schon besser. |
| **Nur `FokussiereDateiverweisfeld` um ein `if (_urheber is null) return;` ergänzen** (lokaler Fix) | Komplexität niedrig · Testbarkeit mittel · Reversibilität hoch · Risiko hoch | Macht das Nichtstun **beabsichtigt**, ohne es zu beheben, und lässt die zweite Stelle stehen. Der gemeldete Fehler ist nicht, dass zufällig nichts geschieht, sondern dass eine angebotene Handlung nichts tut. |
| **Das bestehende Popover im Zeitenblock wiederverwenden** (ein Flag, eine Stelle, drei Auslöser) | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko mittel | Der Klick geschieht in der linken Spalte unten, die Wahl öffnete sich in der rechten Eigenschaftenspalte oben — eine Antwort, die woanders erscheint als die Frage. Bleibt der Rückfallweg, falls die Komponentenzerlegung in Teil 3 den grünen Timerpfad gefährdet. |
| **Die Sperre am Dateiverweisfeld aufheben und erst beim Abschicken fragen** (wie beim Kommentarfeld, das tippbar ist und dessen Senden-Knopf sperrt) | Komplexität mittel · Testbarkeit hoch · Reversibilität niedrig · Risiko hoch | Kehrt eine Entscheidung von `R00021` um (US-5: „In einem frischen Browserkontext ohne gewählte Identität ist die Eingabezeile gesperrt") und macht aus einer Fehlerbehebung eine Bedienkonzeptänderung. Die Ungleichheit zwischen Kommentarfeld und Dateiverweisfeld bleibt bestehen und ist unter „Notizen" als eigene Frage vermerkt. |

## Test-Strategie

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

**Was als Reproduktionstest nicht zählt:** eine Zusicherung „das Feld ist gesperrt" (`ToBeDisabledAsync`) oder „das Feld hat keinen Fokus" (`Not.ToBeFocusedAsync`) ist **heute schon grün** — sie beschreibt den Fehlerzustand, statt ihn aufzudecken. Der Fehler ist der **Fokus**, nicht die Sperre; rot wird ein Test nur an der Zusage, die die Handlung heute nicht einlöst.

### Unit Test zur Bug-Reproduktion

Der Fehler wohnt in der Kupplung zwischen Klick und Browserfokus und ist auf der Unit-Ebene nicht reproduzierbar; die Reproduktionstests sind E2E. Die Unit-Ebene sichert die herausgezogene Entscheidung:

**Reproduktionstest 1 — der gemeldete Fall** (`DateiverweisAnKarteE2ETests`, neu):
`Wenn_ohne_Identitaet_auf_eintragen_geklickt_wird_dann_wird_die_Identitaet_erfragt_und_der_Cursor_steht_danach_im_Feld`

- *Arrange:* frischer Browserkontext ohne gewählte Identität, Karte mit Anhang und ohne Dateiverweis, damit die halbe Zeile „Keine Dateiverweise · eintragen" steht (bestehender Aufbau `KarteMitDateiverweisOhneIdentitaet` als Vorbild, ohne `WaehleIdentitaet`).
- *Act:* Klick auf `#dateiverweis-eintragen` → Klick auf `Stefan` in der erscheinenden Wahl.
- *Assert:* die Identitätswahl war nach dem ersten Klick sichtbar; danach ist `#dateiverweis-eingabe` **nicht mehr gesperrt und fokussiert**; ein sofort getippter Pfad steht im Feld und lässt sich mit der Eingabetaste eintragen; die Zeile erscheint mit Urheber „Stefan" im `title`.
- *Warum ohne Fix rot:* die erste Zusicherung schlägt fehl, weil heute keine Wahl aufgeht; und ohne sie schlüge die zweite fehl, weil das Feld gesperrt bleibt. **Beide Enden sind rot.**

**Reproduktionstest 2 — dieselbe Familie an der zweiten Stelle** (`DateiAnKarteHaengenE2ETests`, neu):
`Wenn_ohne_Identitaet_auf_hinzufuegen_geklickt_wird_dann_wird_die_Identitaet_erfragt_und_die_Ablegeflaeche_ist_danach_frei`

- *Arrange:* frischer Browserkontext ohne gewählte Identität, Karte ohne Anhang und ohne Dateiverweis → die gemeinsame Zeile `#leerstand-beide-haelften` steht.
- *Act:* Klick auf `#anhang-hinzufuegen` → Wahl von `Stefan`.
- *Assert:* die Wahl war offen; danach ist `#anhang-datei` frei (`ToBeEnabledAsync`), `#anhang-hinweis` ist verschwunden, und eine unmittelbar abgelegte Datei hängt an der Karte — **gegen die API gelesen**, nicht gegen das DOM.
- *Warum ohne Fix rot:* der Klick auf das Label bleibt heute vollständig folgenlos; keine der drei Zusicherungen tritt ein.

**Reproduktionstest 3 — der zweite Sperrgrund bekommt die andere Antwort** (`DateiAnKarteHaengenE2ETests`, neu):
`Wenn_waehrend_eines_laufenden_Anhaengens_auf_hinzufuegen_geklickt_wird_dann_nennt_die_Flaeche_die_laufende_Datei_und_keine_Identitaetswahl_geht_auf`

- *Arrange:* Identität gewählt, Karte ohne Anhang; eine 3-MB-Datei ablegen und **nicht** auf die Zeile warten (die Ablegefläche ist dann gesperrt — die Zusage aus `R00024`).
- *Act:* Klick auf `#anhang-hinzufuegen`, solange `#anhang-datei` gesperrt ist.
- *Assert:* keine Identitätswahl ist offen; die Ablegefläche nennt sichtbar `wbs-export.md`; nach Ende des Vorgangs ist die Fläche wieder frei und die Datei hängt an der Karte.
- *Warum ohne Fix rot:* heute ist die Zusicherung „die Identitätswahl bleibt zu" zwar grün, die Zusicherung „der Grund ist in diesem Moment sichtbar" aber nicht gebaut und über den Klickweg nie geprüft — der Test hält die Unterscheidung fest, die diese Anforderung erst einführt. **Falls er ohne Fix grün ist, ist er als Regressionstest zu führen und nicht als Reproduktionstest zu zählen** (Skill `test-ehrlichkeit`).

**Unit Tests (pure Logik, `KanbanC.Blazor.Tests/Services/HandlungsantwortTests.cs`):**

- Identität gewählt, kein Vorgang → **ausführen**.
- Keine Identität → **Identität erfragen**, für beide Handlungen dieselbe Antwort.
- Identität gewählt, Anhängen läuft → **auf den laufenden Vorgang verweisen**, nie Identität erfragen.
- Keine Identität **und** Anhängen läuft → **Identität erfragen**: das ist der Grund, den der Mensch auflösen kann, in derselben Rangfolge, die `Ablegeflaechenstand` für den Text schon führt.
- Der Dateiverweis kennt den zweiten Grund nicht: ohne Identität erfragen, sonst ausführen.

### Code-Extraktion für isoliertes Testing

Die Entscheidung steckt heute nirgends — sie fehlt. Sie entsteht als Operation `Handlungsantwort` unter `Source/KanbanC.Blazor/Services/` (pure Logik, keine Abhängigkeit auf Komponenten, Muster `Ablegeflaechenstand`, `Anhangausfall`, `WebApiAusfall`), damit die Regel ohne Browser vollständig prüfbar ist und nicht als Bedingung in einer Razor-Datei liegt. Die Komponente bleibt Integration (IOSP): sie liest den Zustand, fragt die Operation und führt aus. `Identitaetsfrage.razor` ist reine Darstellung und trägt keine Regel.

### Regressionstests

- `DateiverweisAnKarteE2ETests` bleibt **vollständig grün ohne Änderung an den bestehenden Zusicherungen** — insbesondere `:173` (die Eingabezeile ist ohne Identität gesperrt), `:186` (sie wird frei ohne Reload) und `:446` (mit Identität steht der Cursor im Feld).
- `DateiAnKarteHaengenE2ETests` und die zwei Reproduktionstests aus `R00024` bleiben unverändert grün; die Sperre der Ablegefläche wird nicht angetastet.
- `TimerStartenE2ETests` bleibt unverändert grün — insbesondere der Weg „Klick ohne Identität öffnet die Wahl, danach läuft der Timer ohne zweiten Klick". Er ist die Probe darauf, dass die Zerlegung aus Teil 3 den grünen Pfad nicht beschädigt.
- `KartendetailOeffnenE2ETests:98` und die fünf weiteren Stellen, die den Wortlaut der Leerzeilen hart zusichern, bleiben unverändert.
- `AblegeflaechenstandTests`, `AnhangausfallTests` bleiben unverändert; `DateiverweisabschnittTests` und `AnhangabschnittTests` (Gestaltung) bleiben grün oder werden benannt angepasst.
- Die Suiten aus `R00001`–`R00042` bleiben ohne Änderung grün.

## Akzeptanzkriterien

### Keine angebotene Handlung führt ins Gesperrte

- [ ] **Die Regel, als Zählung prüfbar:** von den fünf Handlungen der Bauart `blatthandlung` auf der Kartenseite führt keine in ein Element, das im Augenblick des Klicks `disabled` ist. Rechenbeispiel: ohne gewählte Identität sind zwei der fünf Ziele gesperrt (`#dateiverweis-eingabe`, `#anhang-datei`) — beide Klicks öffnen die Identitätswahl, null Klicks bleiben folgenlos.
- [ ] Kein Aufruf von `FocusAsync` auf der Kartenseite geschieht, ohne dass geprüft wurde, ob das Ziel im selben Zustand fokussierbar ist.
- [ ] **Nach dem Klick auf `#dateiverweis-eintragen` ohne gewählte Identität ist die Identitätswahl sichtbar.** Diese Zusicherung ist der Reproduktionstest: sie ist ohne Behebung rot.
- [ ] **Nach dem Klick auf `#anhang-hinzufuegen` ohne gewählte Identität ist die Identitätswahl sichtbar** — derselbe Anspruch, obwohl die Kupplung dort ein `for`-Attribut ist und kein Handler.
- [ ] Eine Zusicherung, die nur „das Feld ist gesperrt" oder „das Feld hat keinen Fokus" prüft, zählt **nicht** als Reproduktionstest: beide sind heute grün. Der Nachweis wird an der Zusage genommen, die die Handlung einlöst.

### Nach der Wahl geschieht, was die Handlung versprochen hat

- [ ] Wird in der so geöffneten Wahl eine Identität gewählt, steht der Cursor **unmittelbar** in `#dateiverweis-eingabe`, das Feld ist frei, und ein sofort getippter Pfad landet darin — **ohne zweiten Klick**, wörtlich wie in `R00026:96`.
- [ ] Für „hinzufügen" gilt dasselbe: nach der Wahl ist `#anhang-datei` frei, `#anhang-hinweis` ist verschwunden, und die als Nächstes abgelegte Datei hängt an der Karte — geprüft **gegen die API**, nicht gegen das DOM.
- [ ] Die so gewählte Identität gilt anschließend auch in der Kopfzeile und im Zeitenblock: es entsteht kein zweiter Identitätsbegriff, geschrieben wird über denselben `Identitaetsspeicher` (`R00013`, `sessionStorage`, ein Browserzustand je Tab und kein Login).
- [ ] Der eingetragene Dateiverweis trägt den **gerade gewählten** Urheber im `title` — die Wahl wird vor dem Absenden ein letztes Mal gelesen, wie an allen drei Schreibwegen der Seite.
- [ ] Wird die Wahl mit Escape oder einem Klick auf die Auffangfläche geschlossen, ohne dass gewählt wurde, geschieht **nichts weiter**: kein Fokus in ein gesperrtes Feld, keine Meldung, kein geänderter Zustand.

### Ein laufender Vorgang bekommt seine eigene Antwort

- [ ] Ein Klick auf `#anhang-hinzufuegen`, während ein Anhängen läuft und eine Identität gewählt ist, öffnet **keine** Identitätswahl — die fehlende Angabe ist nicht der Grund, und eine Wahl wäre eine Falschauskunft.
- [ ] In diesem Moment ist der Grund **sichtbar**: die Ablegefläche nennt die laufende Datei. Rechenbeispiel: während `wbs-export.md` läuft, steht dort „‚wbs-export.md‘ wird angehängt …" und nicht der Hinweis auf die Kopfzeile — dieselbe Rangfolge, die `Ablegeflaechenstand.Text` schon führt.
- [ ] Sind **beide** Gründe zugleich gegeben (keine Identität, Anhängen läuft), wird die Identität erfragt: sie ist der Grund, den der Mensch auflösen kann.
- [ ] Nach dem Ende des Vorgangs führt derselbe Klick wieder aus — ohne Reload, bei Erfolg, Zurückweisung und Ausfall gleichermaßen.

### Die Entscheidung ist ohne Browser prüfbar

- [ ] Welche der drei Antworten eine Handlung gibt, entsteht in einer Operation unter `Source/KanbanC.Blazor/Services/` und nicht als Bedingung in `Kartendetail.razor`.
- [ ] `KanbanC.Blazor.Tests` belegt alle Kombinationen aus (Identität gewählt ja/nein) × (Anhängen läuft ja/nein) × (Handlung: Dateiverweis / Anhang) — acht Fälle, jeder mit genau einer erwarteten Antwort.
- [ ] Die Operation kennt weder `ElementReference` noch `IJSRuntime` noch eine Razor-Komponente; sie ist eine reine Abbildung von Zustand auf Antwort.

### Die Sperren und der gezeichnete Zustand bleiben unangetastet

- [ ] `#dateiverweis-eingabe` bleibt ohne gewählte Identität `disabled`, `#anhang-datei` bleibt es aus beiden Gründen; kein Kriterium dieser Anforderung lockert eine Sperre.
- [ ] Der Wortlaut der drei Leerzeilen bleibt **zeichengleich**: „Keine Anhänge, keine Dateiverweise · hinzufügen", „Keine Anhänge · hinzufügen", „Keine Dateiverweise · eintragen". Die Zeile ist kein Sperranzeiger; sechs bestehende Zusicherungen im E2E-Bestand hängen daran.
- [ ] Die bestehenden Hinweiszeilen `#dateiverweis-hinweis` und `#anhang-hinweis` bleiben, wo und wie sie sind — sie erklären den Zustand, die Wahl löst ihn auf.
- [ ] Die Gestaltung des Popovers an den neuen Stellen nutzt ausschließlich Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in `Kartendetail.razor.css`, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] **Benannte Änderung 1:** `Kartendetail.razor` — `:356` und `:1830` fragen vor dem Fokussieren die Antwort ab; `:342`/`:349` bekommen einen eigenen `@onclick` **zusätzlich zum `for`-Attribut**, der nur im gesperrten Zustand handelt. Das `for` bleibt: ein per Skript ausgelöster Klick auf ein Dateifeld verlöre die Benutzergeste, mit der der Browser den Dateiwähler erlaubt.
- [ ] **Benannte Änderung 2:** `Source/KanbanC.Blazor/Services/Handlungsantwort.cs` (neu) — pure Operation, keine Abhängigkeit auf Komponenten.
- [ ] **Benannte Änderung 3:** `Source/KanbanC.Blazor/Components/Karten/Identitaetsfrage.razor(.css)` (neu) — Popover, Auffangfläche und Escape-Behandlung einmal statt dreimal; `Identitaetswahl.razor` wird darin **unverändert** eingesetzt. Der Zeitenblock nutzt ab jetzt dieselbe Komponente, und `TimerStartenE2ETests` bleibt ohne Änderung grün.
- [ ] **Benannte Änderung 4:** `KartendetailSeite.cs` wächst um die Locator der Wahl an den zwei neuen Stellen; die bestehenden Locator und ihre Ids bleiben unverändert.
- [ ] **Benannte Änderung 5:** `DateiverweisAnKarteE2ETests` und `DateiAnKarteHaengenE2ETests` wachsen um die drei Reproduktionstests; **keine bestehende Zusicherung wird geändert oder entfernt.**
- [ ] `Kopfzeile.razor` bleibt unverändert — die Verdopplung des Popovers mit der Kopfzeile (Anmerkung 413) bleibt bestehen und ist nicht Gegenstand dieser Anforderung.
- [ ] Build ohne Warnungen (`TreatWarningsAsErrors`), alle Testebenen grün, Coverage nicht gefallen.

## Implementierungshinweise

**Die Antwort der Operation.** Drei Fälle, benannt statt als Wahrheitswert-Paar: ausführen, Identität erfragen, auf den laufenden Vorgang verweisen. Die Rangfolge bei zwei Gründen ist dieselbe wie in `Ablegeflaechenstand.Text` — der auflösbare Grund gewinnt.

**Das offene Vorhaben.** `_identitaetswahlIstOffen` (`:1242`) ist heute ein Wahrheitswert und trägt implizit „danach den Timer starten". Es wird zu einem benannten Vorhaben (Timer starten / Dateiverweis eintragen / Datei anhängen), damit `Identitaetsfrage` weiß, wo sie aufgeht, und `UebernimmIdentitaet…` weiß, was danach geschieht. `UebernimmIdentitaetUndStarte` (`:1353`) ist das Vorbild, das dabei erhalten bleibt: merken, Urheber neu lesen, ausführen.

**Das Label bleibt ein Label.** Ein `<button>`, der per JS-Interop `#anhang-datei` klickt, verlöre nach einem `await` die Benutzergeste, und der Browser öffnete den Dateiwähler nicht mehr. Der zusätzliche `@onclick` auf dem Label ist der Weg: er feuert unabhängig davon, ob das Ziel gesperrt ist, und tut im freien Zustand nichts.

**Das Popover in der linken Spalte.** Es öffnet an der Stelle des Klicks. Der Zeitenblock zeigt die Bauform (`:556-566`): Auffangfläche darunter, `tabindex="-1"` am Popover, damit Escape nach einem Klick darin nicht ins Leere läuft.

**Nach der Wahl fokussieren, nicht davor.** Das Feld ist erst frei, wenn `_urheber` steht und die Renderrunde durch ist; ein `FocusAsync` im selben Zug träfe wieder ein `disabled`-Element — derselbe Fehler mit besserer Absicht.

## Offene Fragen

- **Bleibt die Ungleichheit zwischen Kommentarfeld und Dateiverweisfeld?** Das Kommentarfeld ist ohne Identität tippbar und nur der Senden-Knopf gesperrt; das Dateiverweisfeld ist selbst gesperrt. Beide Wege sind für sich stimmig, nebeneinander sind es zwei Antworten auf dieselbe Frage. **Nicht Gegenstand dieser Anforderung** — sie behebt den Fehler, ohne eine Entscheidung von `R00021` umzukehren. Gehört als eigene Anforderung entschieden, oder gar nicht.
- **Soll die Regel über die Kartenseite hinaus gelten?** Geprüft wurde der ganze Bestand; ausserhalb der Kartenseite gibt es heute keinen Fall. Ob die Zählung als Prüfung im Bau verankert wird (etwa als Gestaltungstest über den Quelltext, Muster `DateiverweisabschnittTests`) oder als Regel im Stilkatalog, ist offen.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Schmerzpunkt ist konkret: wer ohne gewählte Identität auf „eintragen" klickt, bekommt vom Programm die Auskunft, dass es die Handlung anbietet — und dann geschieht nichts. Ändern wir die Handlung so, dass sie ihren Zustand kennt, dann hat sie in jedem Zustand eine Antwort: sie führt aus, oder sie erfragt genau die Angabe, die fehlt, und führt danach aus. Damit wird aus einer toten Stelle ein Weg, der zum Ziel führt — und der Mensch braucht nicht zu wissen, dass es die Kopfzeile ist, die ihm im Weg stand. Der Hebel liegt genau hier und nicht an der Sperre: die Sperre ist richtig und in `R00020`, `R00021` und `R00024` je begründet; falsch ist nur, dass daneben eine Einladung steht, die sie nicht kennt. Und er liegt nicht erst beim Abschicken, denn dort wäre der Pfad längst getippt und die Enttäuschung schon eingetreten. Weil dieselbe Wurzel an zwei Stellen liegt und an einer dritten (`#timer-starten`) bereits richtig gelöst ist, wird nicht zweimal geflickt, sondern die Antwort einmal als prüfbare Operation hingelegt — dann kostet die nächste angebotene Handlung keinen neuen Fehler.

## Notizen

- **Priorität:** Mittel. Kein Datenverlust und keine falsche Auskunft — aber eine Sackgasse an einer Stelle, an der die Oberfläche selbst zum Weitergehen einlädt, und die einzige Handlung der Seite, die einen Zustand ignoriert, den drei Nachbarn kennen.
- **Betroffene Nutzer/Systeme:** Menschen an der Kartenseite in einem frischen Browserkontext oder einem neuen Tab — die Identität ist ein Zustand je Tab (`I0008`), der Fall tritt also bei jedem neuen Tab auf, nicht nur beim ersten Besuch. Agenten sind nicht betroffen: sie schreiben über HTTP und kommen an dieser Oberfläche nicht vorbei.
- **Workaround:** In der Kopfzeile die Identität wählen, dann in das Feld klicken statt auf „eintragen".
- **Verworfene Alternativen:** siehe „Alternative Ansätze" — Ausgrauen der Handlung, Fokus auf die Kopfzeile, lokales `return`, Wiederverwendung des Zeiten-Popovers, Aufheben der Sperre.
- **Bewusst out of scope:** die Verdopplung von Popover und Popover-CSS zwischen Kartenseite und `Kopfzeile.razor` (Anmerkung 413) — diese Anforderung räumt sie **innerhalb** der Kartenseite auf und rührt die Kopfzeile nicht an, weil dort eine grüne Komponente ohne Befund steht.
- **Kein WBS-Knoten:** eine Bug-Anforderung bekommt keinen; der Fortschritt der WBS bleibt unberührt.
