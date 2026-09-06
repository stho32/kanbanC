---
id: R00024
status: Neu
datum: 2026-09-06
ursprung: Bug-Report
ursprungslauf: R00020
---

# R00024: Behebung des stillen Anhangverlusts beim zweiten Ablegen

## Beschreibung

Wer auf `/karten/{karteId}` eine Datei anhängt und **während der Übertragung** eine zweite wählt, verliert die erste — vollständig und ohne jede Meldung. Die erste Datei bekommt **keine Zeile in der Datenbank** und **keine Datei in der Ablage**; die Oberfläche zeigt danach nur die zweite und sieht dabei völlig heil aus. Das ist stiller Datenverlust, keine Anzeigefrage.

**Schritte:** Karte mit gewählter Identität öffnen · eine 3-MB-Datei ablegen · sofort danach, ohne auf die Zeile zu warten, eine 2-kB-Datei ablegen.
**Erwartet:** beide Dateien hängen an der Karte, oder mindestens eine sichtbare Meldung sagt, welche nicht ankam.
**Tatsächlich:** nur die zweite hängt an der Karte. Die erste ist weg, ohne Meldung, ohne Zurückweisung, ohne Ausnahmeanzeige.

Zahlt ein auf: [Vision](R00000-vision.md) — „lokale Datenhaltung, … die Daten sind unmittelbar zugänglich". Eine Datei, die der Benutzer abgelegt hat und die nirgends ankommt, ist der Gegenbeweis dieser Zusage; und die Vision-Zusage „an jeder Karte ist ablesbar, wer oder was gehandelt hat" trägt nicht, wenn eine Handlung spurlos verschwindet.

**Der Fehler stammt aus `I0018`/[`R00020`](R00020-datei-an-karte-haengen.md) „Datei an Karte hängen" und ist bereits im Trunk.** `R00020` hat die Ablegefläche für **eine** Datei je Vorgang gezeichnet (Mehrfachauswahl steht dort ausdrücklich unter „Bewusst out of scope"), aber nirgends festgehalten, was gilt, während ein Vorgang läuft. Diese Anforderung schließt genau diese Lücke.

### Belege aus dem Diagnoselauf

35 Beobachtungen in drei Läufen, zwei davon unter CPU-Last:

- Im Fehlerfall `DOM=[burndown-r2.png] DB=[burndown-r2.png]` — die erste Datei hat **keine Zeile in der Datenbank**.
- DOM und Datenbank stimmten in **allen 35** Beobachtungen überein. Die Anzeige ist intakt; verloren geht der Upload selbst. **Ein Kriterium, das nur den DOM-Zustand betrachtet, hätte diesen Fehler nicht gefunden.**
- `Fehlermeldung=(keine) Zurueckweisung=(keine) Ausnahmeanzeige=False` — der Verlust ist für den Benutzer unsichtbar.
- Browserkonsole nur im Fehlerfall: `pageerror: Error: There was an exception invoking 'NotifyChange'`.
- **Reproduktion ohne Glück:** 3 MB zuerst, 2 kB danach kippt zuverlässig in der ersten Runde (2 von 2 Läufen). Mit den Originalgrößen (1000/2000 Bytes) blieb es isoliert grün — daher 13 von 13 isolierten Läufen grün und nur unter Last rot. **Die Dateigrößen sind Teil der Reproduktion**, nicht Beiwerk.

## Ursachenanalyse

### Root Cause

Drei Stellen, die zusammen den stillen Verlust ergeben. Keine davon genügt für sich, und keine lässt sich ohne die anderen sinnvoll beheben — deshalb ist das **eine** Anforderung.

**1. Uploads werden nicht serialisiert.** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor:366`

```razor
<InputFile class="ablegefeld" id="anhang-datei" OnChange="NimmDatei" disabled="@(_urheber is null)" />
```

Das Feld nimmt weitere `change`-Ereignisse an, während `NimmDatei` (`:1109`) über `UebernimmAnhang` (`:1137`) noch liest:

```csharp
await using var inhalt = datei.OpenReadStream(Anhangsgrenze.HoechsteDateigroesse);
var ergebnis = await KartenKlient.HaengeAnhangAn(KarteId, kontributorId, datei.Name, inhalt);
```

Der Strom wird **stückweise über SignalR** aus dem Browser nachgezogen, während der HTTP-Aufruf an die WebApi schon läuft. Das zweite `change`-Ereignis ersetzt browserseitig die Zuordnung Datei-Nummer → Datei; die laufende Übertragung findet ihre Referenz nicht mehr und stirbt. Blazor wirft dabei in `NotifyChange` — das ist der `pageerror`, der ausschließlich im Fehlerfall auftaucht. Belegt wird das nicht-serialisierte Verhalten zusätzlich durch die **vertauschte Ablagereihenfolge** in der Datenbank: die zweite Datei kommt vor der ersten an.

`disabled="@(_urheber is null)"` sperrt die Fläche also **nur** aus dem Identitätsgrund aus `R00020`; ein laufender Vorgang ist kein Grund.

**2. Der Abbruch hat keinen Fangpunkt.** `Source/KanbanC.Blazor/Services/WebApiAufruf.cs`

```csharp
catch (HttpRequestException)
{
    return WebApiAusfall.Meldung;
}
```

`MitAusfallmeldung` ist das einzige Netz unter `UebernimmAnhang` (`Kartendetail.razor:1131`), und es fängt ausschließlich `HttpRequestException`. Ein abgerissener Dateistrom ist keine — der Abbruch verschwindet lautlos, und `_ausfallmeldung` bleibt `null`.

**Der Fangpunkt gehört trotzdem nicht in diesen Helfer.** `WebApiAufrufTests.cs:29` (`Wenn_eine_andere_Ausnahme_fliegt_dann_wird_sie_nicht_verschluckt`) hält fest, dass er andere Ausnahmen bewusst durchlässt; ein aufgeweitetes `catch (Exception)` würde jeden Programmierfehler **jeder** Handlung der Kartenseite in ein „Die WebApi ist nicht erreichbar" verwandeln. Der Fangpunkt entsteht **neben** ihm, am Anhängen, mit eigener Meldung.

**3. Die Meldung des einen Vorgangs wird vom nächsten gelöscht.** `Kartendetail.razor:1111` setzt eingangs `_zurueckweisung = null`, `:1131` setzt am Ende `_ausfallmeldung`. Überlappen zwei Vorgänge, löscht der zweite beim Eintreten den Befund des ersten und überschreibt beim Austreten dessen Meldung. Selbst wenn der erste Vorgang etwas zu sagen hätte, käme es nicht an.

**Ausgeschlossen wurde:** die WebApi und der Datenzugriff. DOM und Datenbank stimmen in allen 35 Beobachtungen überein, und die verlorene Datei erreicht die API nie — es gibt weder eine Zeile noch eine halbe Datei in der Ablage. `KartenEndpunkte`, `KartenService`, `Anhangablage` und `Anhangpfad` sind nicht beteiligt; **der Fehler liegt vollständig in der Oberfläche.** Ausgeschlossen wurde ebenso die Obergrenze aus `Anhangsgrenze`: beide Dateien liegen weit darunter, und der Größenwächter (`:1123`) schlägt nicht an.

### Betroffene Komponenten

- `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` — `:366` (das Feld), `:1093-1109` (`Ablegesperre`, `NimmDatei`), `:1137` (`UebernimmAnhang`).
- `Source/KanbanC.Blazor/Services/WebApiAufruf.cs` — bleibt unverändert, ist aber die Stelle, an der das Netz heute endet.
- `Source/KanbanC.PlaywrightTests/Tests/DateiAnKarteHaengenE2ETests.cs` — der einzige Ort, an dem der Fehler heute überhaupt auslösbar ist.
- **Nicht betroffen:** WebApi, `KanbanC.BL`, Schema, Migrationen, Contracts. Es gibt keine Datenbankänderung; der Migrationsläufer wird nicht angefasst.

### Der Test, der den Fehler heute aufdeckt — und die drei, die ihn verdecken

`DateiAnKarteHaengenE2ETests.cs:121` `Wenn_das_Kreuz_geklickt_wird_dann_verschwinden_Zeile_und_Datei_und_die_zweite_bleibt` legt in `:125-126` zwei Dateien **ohne Zwischenzusicherung** ab und ist damit der einzige Test des Bestands, der das Fenster trifft.

Die drei Nachbartests legen ebenfalls zwei Dateien nacheinander ab, setzen aber je eine Zusicherung über das **Ergebnis des ersten** dazwischen (`:47`, `:89`, `:208` — jeweils `Expect(Anhaenge).ToHaveCountAsync(1)`). Diese Zusicherung ist fachlich richtig (US-1 verlangt, dass die Zeile erscheint), wirkt aber zugleich als Synchronisation: der zweite Vorgang startet erst, wenn der erste durch ist, und das Fenster kann nicht mehr getroffen werden.

**Diese Zwischenzusicherungen dürfen nicht als Vorbild dienen.** Sie sind der Grund, warum der Verlust seit `R00020` unentdeckt blieb (Skill `test-ehrlichkeit`: ein Test, der sein Grün einer Wartewirkung verdankt, prüft nicht, was sein Name sagt). Sie bleiben unverändert stehen — geändert wird nicht der Bestand, sondern das, was neu dazukommt.

## Lösungsvorschlag

### Langfristige Lösung

**Die Ablegefläche ist gesperrt, solange ein Anhängen läuft** — und ein Anhängen, das trotzdem scheitert, wird sichtbar.

Drei Teile, die zusammengehören:

1. **Sperre statt Warteschlange.** Die Ablegefläche kennt künftig **zwei** Gründe für denselben gesperrten Zustand: keine Identität gewählt (wie seit `R00020`) und ein laufendes Anhängen. Der Zustand wird als **pure Logik** aus der Komponente herausgezogen (`Ablegeflaechenstand` in `KanbanC.Blazor/Services/`, Muster `Dateigroesseform`/`Anhangadresse`), damit er ohne Browser prüfbar ist; die Komponente hält nur noch die laufende Datei. Während des Vorgangs sagt die Fläche, **welche** Datei gerade angehängt wird — die Sperre ist begründet sichtbar, nicht bloß grau.
2. **Ein eigener Fangpunkt am Anhängen.** Scheitert die Übertragung der Bytes, entsteht eine lesbare Meldung, die den Dateinamen nennt, sagt, dass die Datei **nicht angekommen** ist, und die Kompensationsaktion nennt („bitte erneut ablegen"). `WebApiAufruf` bleibt unverändert. Der Text kommt aus einer eigenen Operation (`Anhangausfall`, Muster `WebApiAusfall`), damit er ohne Browser prüfbar ist.
3. **Keine Meldung überschreibt eine fremde.** Das Zurücksetzen von `_zurueckweisung`/`_ausfallmeldung` gehört zum Vorgang, den der Benutzer **auslöst** — nie zu einem, der schon läuft. Mit der Sperre aus (1) kann die Überlappung im Bedienweg gar nicht mehr entstehen; (3) ist die Zusicherung, dass der Fall auch dann kein Datum verschluckt, wenn er auf anderem Weg doch eintritt.

**Warum Sperre und nicht Warteschlange** (die Frage aus dem Auftrag, ausdrücklich entschieden):

- **Die Warteschlange beseitigt die Ursache nicht.** Was verloren geht, ist die **Dateireferenz im Browser**, und die zieht das zweite `change`-Ereignis weg, bevor irgendein C#-Code in der Warteschlange davon erfährt. Eine Warteschlange, die `IBrowserFile`-Referenzen aufbewahrt, bewahrt Referenzen auf, die schon tot sind. Damit sie trüge, müsste sie die Bytes **vorher vollständig lesen** — bis zu 10 MB je Eintrag durch den SignalR-Kreislauf in den Arbeitsspeicher, bei mehreren Einträgen ein Vielfaches. Sie tauscht den Verlust gegen Speicherdruck und eine neue Fehlerquelle.
- **Das Zielbild gibt den Ausschlag.** „Kanbanflow-dicht, Beiwerk tritt zurück" (`CLAUDE.md`, „Zieldesign der Oberfläche"): eine Warteschlange braucht eigene Oberfläche — Positionen, Stand je Eintrag, Abbrechen, Fehler je Eintrag. Das ist genau das Beiwerk, das zurücktreten soll. Die Sperre braucht **einen Zustand an einem schon gezeichneten Element**.
- **Die Erwartung des Benutzers, der zwei Dateien nacheinander anklickt**, ist: eine wählen, ankommen sehen, die nächste wählen. `R00020` hat die Fläche bewusst **ohne** Mehrfachauswahl gezeichnet; sie hat nie versprochen, mehrere gleichzeitig zu nehmen — sie hält ihr Versprechen nur nicht. Eine Warteschlange verspräche Gleichzeitigkeit und löste sie trotzdem nacheinander ein: bei einem 10-MB-Anhang wartet der Benutzer genauso, sieht aber keinen Grund.
- **Die Sperre ist ehrlicher.** Sie sagt, was gilt, im Moment, in dem es gilt. Eine Warteschlange verbirgt den Engpass und macht aus einem sichtbaren Warten ein unsichtbares.
- **Der Mechanismus existiert schon.** `disabled` am Feld, `ablegeflaeche-gesperrt` an der Fläche und die Hinweiszeile darunter sind seit `R00020` da; es kommt ein zweiter Grund für denselben Zustand hinzu, kein zweites Bedienkonzept.
- **Reversibilität.** Wird später Mehrfachauswahl gewollt, gehört die Warteschlange in den Slice, der die Fläche dafür zeichnet — nicht in eine Fehlerbehebung.

### Alternative Ansätze

| Option | Achsen | Warum verworfen |
|---|---|---|
| **Uploads in einer Warteschlange serialisieren** | Komplexität hoch · Testbarkeit mittel · Reversibilität mittel · Risiko hoch | Kann die Ursache nicht beseitigen, ohne die Bytes vorher in den Speicher zu lesen (bis 10 MB je Eintrag); braucht eigene Oberfläche und verspricht eine Gleichzeitigkeit, die `R00020` gar nicht gezeichnet hat. Begründung oben in voller Länge. |
| **Nur den Fehlerpfad reparieren** (Abbruch fangen und melden, lokaler Fix) | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko hoch | Macht den Verlust **sichtbar**, verhindert ihn nicht. Der Benutzer erführe, dass seine 3-MB-Datei weg ist — mehr nicht. Teil 2 der gewählten Lösung, aber nie für sich allein. |
| **`WebApiAufruf.MitAusfallmeldung` auf `catch (Exception)` aufweiten** | Komplexität niedrig · Testbarkeit niedrig · Reversibilität hoch · Risiko hoch | Bricht die ausdrückliche Zusage von `WebApiAufrufTests.cs:29` und verwandelt jeden Programmierfehler jeder Handlung der Kartenseite in „Die WebApi ist nicht erreichbar" — eine Falschaussage als Voreinstellung. |
| **Direkt-Upload vom Browser an die WebApi** (Architekturänderung: der Browser postet das multipart-Formular selbst, wie er beim Herunterladen schon direkt lädt) | Komplexität hoch · Testbarkeit mittel · Reversibilität niedrig · Risiko mittel | Nimmt den SignalR-Kreislauf ganz aus dem Weg und löst die Ursache an der Wurzel — aber es ist ein Umbau des Anhängewegs samt neuer Antwortverarbeitung im Browser, nicht die Behebung eines Fehlers. Bleibt als Richtung notiert, falls der Kreislauf ein zweites Mal auffällt. |
| **Die zweite Datei stumm verwerfen** (Feld bleibt offen, der Handler ignoriert das zweite Ereignis) | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko hoch | Tauscht den stillen Verlust der ersten gegen den stillen Verlust der zweiten Datei. Der Fehler dieser Anforderung ist nicht, welche Datei verloren geht, sondern **dass es stumm geschieht**. |

## Test-Strategie

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`.

### Unit Test zur Bug-Reproduktion

Der Fehler ist eine Wettlaufsituation zwischen Browser und Blazor-Kreislauf; er ist auf der Unit-Ebene **nicht** reproduzierbar. Der Reproduktionstest ist deshalb ein E2E-Test, und die Unit-Ebene sichert die herausgezogene Logik:

**Reproduktionstest 1 — der Bedienweg** (`DateiAnKarteHaengenE2ETests`, neu):
`Wenn_zwei_Dateien_so_schnell_abgelegt_werden_wie_die_Flaeche_es_zulaesst_dann_stehen_beide_in_der_Datenbank`

- *Arrange:* Karte ohne Anhang, Identität gewählt (bestehender Aufbau `KarteOhneAnhang`).
- *Act:* `HaengeDateiAn("wbs-export.md", Bytes(3 * 1024 * 1024))` · `Expect(Anhangdateifeld).ToBeDisabledAsync()` · `Expect(Anhangdateifeld).ToBeEnabledAsync()` · `HaengeDateiAn("burndown-r2.png", Bytes(2048))`.
- *Assert:* **gegen die API**, nicht gegen das DOM — `WebApiKlient.LadeKartendetail(karteId)` trägt zwei Anhänge mit den beiden Namen und `Dateigroesse` `3145728` und `2048`; zu **beiden** liegt die Datei unter `Testdatenbank.Ablageordner` (`LiegtAnhangdatei`), und ihre Länge stimmt mit der Zeile überein.
- *Warum ohne Fix rot:* die Zusicherung `ToBeDisabledAsync` schlägt fehl, weil die Fläche nie sperrt — und selbst wenn man sie wegließe, fehlte danach die erste Zeile in der Datenbank. **Beide** Enden sind rot.
- *Warum die Wartepunkte keine Zwischenzusicherung im verbotenen Sinn sind:* sie prüfen die **neue Zusage** (die Fläche sperrt und gibt wieder frei), nicht das **Ergebnis des ersten Vorgangs**. Ohne Fix passiert `ToBeEnabledAsync` sofort, weil das Feld nie gesperrt war — der Test serialisiert also nichts, was er nicht selbst prüft.

**Reproduktionstest 2 — die Bilanz unter Zwang** (`DateiAnKarteHaengenE2ETests`, neu):
`Wenn_die_Ueberlappung_erzwungen_wird_dann_verschwindet_kein_Anhang_stumm`

- *Act:* dieselben zwei Dateien, aber ohne jeden Wartepunkt — Playwright setzt die zweite Datei auch auf ein gesperrtes Feld und erzwingt damit eine Überlappung, die ein Mensch nicht auslösen kann.
- *Assert (die Bilanz):* für **jeden** der zwei Vorgänge gilt danach: entweder er steht als Zeile in der Datenbank (über die API gelesen), oder auf der Seite steht eine sichtbare Meldung, die **seinen Dateinamen** nennt. Kein Vorgang ist beides nicht. Zusätzlich: `#blazor-error-ui` bleibt unsichtbar, und die Konsole zeigt keinen `pageerror`.
- *Warum ohne Fix rot:* heute steht die erste Datei weder in der Datenbank noch in einer Meldung — genau der belegte Zustand `Fehlermeldung=(keine) Zurueckweisung=(keine)`.
- Dieser Test hält die Zusage „nie stumm" auch für den Weg, den die Sperre nicht abdeckt.

**Unit Tests (pure Logik, `KanbanC.Blazor.Tests/Services/`):**

- `AblegeflaechenstandTests` — ohne Identität: gesperrt, Hinweis auf die Kopfzeile · mit Identität, kein Vorgang: frei · mit Identität, laufender Vorgang: gesperrt, Text nennt die laufende Datei · **beide** Gründe zugleich: gesperrt, und der Text nennt den Identitätsgrund (er ist der, den der Benutzer auflösen muss).
- `AnhangausfallTests` — die Meldung nennt den Dateinamen, sagt, dass die Datei nicht angehängt wurde, und nennt die Kompensationsaktion; sie unterscheidet sich von `WebApiAusfall.Meldung`.

### Code-Extraktion für isoliertes Testing

Der Sperrzustand steckt heute als `Ablegesperre`-Getter (`Kartendetail.razor:1093`) und als Inline-Ausdruck `disabled="@(_urheber is null)"` (`:366`) in der Komponente und ist damit nur über den Browser prüfbar. Er wird als **Operation** nach `Source/KanbanC.Blazor/Services/` herausgezogen (`Ablegeflaechenstand`), zusammen mit dem Meldungstext (`Anhangausfall`) — dasselbe Vorgehen, das `R00020` für `Dateigroesseform` und `Anhangadresse` gewählt hat, und der Grund, aus dem `KanbanC.Blazor.Tests` existiert (`CLAUDE.md`, Abweichung 4). Die Komponente behält nur den Zustand („welche Datei läuft gerade") und bleibt Integration (IOSP).

### Regressionstests

- **Die vorhandene Suite `DateiAnKarteHaengenE2ETests` bleibt vollständig grün, ohne Änderung an den bestehenden Zusicherungen** — insbesondere `:47`, `:89`, `:121` und `:208`. Die neue Sperre darf keinen dieser Tests hängen lassen: sie gibt nach jedem Ausgang wieder frei.
- Die Suiten aus `R00001`–`R00023` bleiben ohne Änderung grün.
- `WebApiAufrufTests` bleibt **unverändert** — alle drei Tests, insbesondere `:29`.
- Der Fehlervertrag der WebApi wird nicht berührt (`FehlervertragTests`); es kommt keine Route hinzu.

## Akzeptanzkriterien

### Kein Anhang geht verloren — geprüft bis in die Datenbank

- [ ] Werden zwei Dateien so schnell nacheinander abgelegt, wie die Ablegefläche es zulässt, stehen danach **beide** Anhänge in der Datenbank. Rechenbeispiel: 3 145 728 Bytes zuerst, 2 048 Bytes danach → `GET /api/karten/{karteId}` trägt zwei Einträge mit `Dateigroesse` `3145728` und `2048`.
- [ ] Zu **jedem** Eintrag liegt die Datei unter `<Ablageordner>/<KarteId>/<AnhangId>`, und ihre Länge stimmt mit der `Dateigroesse` der Zeile überein — geprüft am Dateisystem, nicht an der Antwort.
- [ ] Die Zusicherung wird **nicht am DOM** genommen: sie liest die API und den Ablageordner. Begründung im Kriterium selbst: DOM und Datenbank stimmten in allen 35 Beobachtungen des Diagnoselaufs überein, ein DOM-Kriterium wäre grün gewesen.
- [ ] **Bilanz:** Jeder vom Benutzer ausgelöste Ablegevorgang endet entweder als Zeile in der Datenbank **oder** als sichtbare Meldung, die seinen Dateinamen nennt — nie als keines von beidem. Rechenbeispiel: zwei Ablegevorgänge, einer davon zurückgewiesen → eine Zeile in der Datenbank plus eine sichtbare Meldung, Summe zwei; kein Vorgang ohne Spur.
- [ ] Die Bilanz gilt auch, wenn die Überlappung **erzwungen** wird (ein Weg, den die Oberfläche nicht anbietet): dann darf ein Vorgang scheitern, aber nicht stumm.
- [ ] Es entsteht nie eine Zeile ohne Datei und nie eine Datei, deren Länge von der `Dateigroesse` ihrer Zeile abweicht.
- [ ] Der Reproduktionstest arbeitet mit **3 MB zuerst und 2 kB danach**. Mit 1 000 und 2 000 Bytes blieb der Fehler in 13 von 13 isolierten Läufen unsichtbar; die Größen sind Teil der Reproduktion und werden nicht „aufgeräumt".

### Ein laufendes Anhängen ist unantastbar und sichtbar

- [ ] Solange ein Anhängen läuft, ist die Ablegefläche gesperrt: das Dateifeld ist `disabled`, ein Klick öffnet keinen Dateiwähler, und eine auf die Fläche gezogene Datei löst kein zweites `change`-Ereignis aus.
- [ ] Die Sperre ist **begründet** sichtbar: die Fläche sagt, dass gerade angehängt wird, und nennt die Datei. Rechenbeispiel: während `wbs-export.md` läuft, steht dort der Dateiname `wbs-export.md` und **nicht** der Hinweis auf die Identitätswahl.
- [ ] Nach dem Ende des Vorgangs ist die Fläche wieder frei — **ohne Reload**, und zwar bei Erfolg, bei Zurückweisung und bei Ausfall gleichermaßen. Kein Ausgang lässt die Fläche gesperrt zurück.
- [ ] Ohne gewählte Identität bleibt die Fläche gesperrt wie seit `R00020`; die beiden Gründe ergeben zusammen genau **einen** gesperrten Zustand, und der angezeigte Text nennt den Grund, den der Benutzer auflösen kann.
- [ ] Der Sperrzustand ist **ohne Browser prüfbar**: er entsteht in einer Operation unter `Source/KanbanC.Blazor/Services/` und wird in `KanbanC.Blazor.Tests` mit allen vier Kombinationen (Identität ja/nein × Vorgang läuft ja/nein) belegt.

### Ein gescheitertes Anhängen wird sichtbar

- [ ] Bricht die Übertragung der Bytes ab, erscheint eine lesbare Meldung auf der Kartenseite, die den **Dateinamen** nennt, sagt, dass die Datei **nicht angehängt** wurde, und die Kompensationsaktion nennt („erneut ablegen") — derselbe Anspruch, den `R00020` an die Fehlerantworten der API stellt, hier für die Oberfläche.
- [ ] Die Anhangliste bleibt unverändert; die abgebrochene Datei steht **weder** in der Liste **noch** in der Datenbank **noch** in der Ablage. Es bleibt keine halbe Datei zurück.
- [ ] Die Meldung unterscheidet sich von `WebApiAusfall.Meldung` — „Die WebApi ist nicht erreichbar" wäre hier eine Falschaussage: sie war erreichbar, die Datei war es nicht.
- [ ] Im regulären Bedienweg erzeugt das Anhängen **keinen** `pageerror` in der Browserkonsole. Rechenbeispiel: der Ablauf „zwei Dateien nacheinander" erzeugt null Konsolenfehler; im Fehlerfall stand dort `Error: There was an exception invoking 'NotifyChange'`.
- [ ] Die Blazor-Ausnahmeanzeige (`#blazor-error-ui`) bleibt in beiden Reproduktionstests unsichtbar — der Kreislauf bricht nicht ab.

### Keine Meldung überschreibt eine fremde

- [ ] Die Meldung und der Befund eines Anhängens werden nur von einem Vorgang zurückgesetzt, den der Benutzer **selbst ausgelöst** hat — nie von einem, der beim Auslösen schon lief.
- [ ] Nach einer Zurückweisung bleibt deren Befund stehen, bis der Benutzer die nächste Handlung auslöst. Rechenbeispiel: eine Datei mit 10 MB + 1 Byte wird beanstandet, danach wird eine 2-kB-Datei angehängt → zuerst steht die Größenmeldung mit „10,5 MB", danach steht die neue Zeile und keine alte Meldung; zu keinem Zeitpunkt steht eine Meldung, die zu keiner der beiden Dateien gehört.
- [ ] Es gibt keinen Zustand, in dem zwei Anhängevorgänge derselben Karte gleichzeitig laufen — belegt daran, dass die Fläche während eines Vorgangs kein zweites Ereignis annimmt.

### Der grüne Bestand bleibt grün — mit benannten Änderungen

- [ ] **Benannte Änderung 1:** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` — `:366` liest den Sperrzustand statt `_urheber is null`; `NimmDatei` (`:1109`) und `UebernimmAnhang` (`:1137`) setzen und räumen den Vorgangszustand und bekommen den eigenen Fangpunkt.
- [ ] **Benannte Änderung 2:** `Source/KanbanC.Blazor/Services/Ablegeflaechenstand.cs` (neu) und `Anhangausfall.cs` (neu) — pure Operationen, keine Abhängigkeit auf Komponenten, Muster `Dateigroesseform`/`WebApiAusfall`.
- [ ] **Benannte Änderung 3:** `Source/KanbanC.Blazor/Services/WebApiAufruf.cs` bleibt **unverändert**, und `WebApiAufrufTests.cs` ebenso — insbesondere `:29`. Wer den gemeinsamen Helfer aufweitet, hat diese Anforderung nicht erfüllt, sondern eine zweite gebrochen.
- [ ] **Benannte Änderung 4:** `Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs` wächst um die Locator des laufenden Vorgangs; `DateiAnKarteHaengenE2ETests` um die zwei Reproduktionstests. Die bestehenden Locator bleiben unverändert.
- [ ] **Benannte Änderung 5:** die Gestaltung des laufenden Zustands nutzt ausschließlich Werte aus `Source/KanbanC.Blazor/wwwroot/gestaltung.css`; kein Literal in `Kartendetail.razor.css`, kein CSS-Framework (`CLAUDE.md`, „Zieldesign der Oberfläche").
- [ ] Die vier bestehenden Tests, die zwei Dateien nacheinander ablegen (`:47`, `:89`, `:121`, `:208`), bleiben **ohne Änderung** grün. Ihre Zwischenzusicherungen werden weder entfernt noch nachgeahmt: kein neuer Test setzt zwischen zwei Ablegevorgänge eine Zusicherung über das **Ergebnis** des ersten.
- [ ] Alle E2E-Suiten aus `R00001`–`R00023` bleiben ohne Änderung grün.
- [ ] **Keine Änderung an der WebApi:** Routen, Verben, Antwortgestalten und Fehlerverträge bleiben, wie sie sind; `FehlervertragTests` bleibt unverändert grün.
- [ ] **Keine Änderung am Schema und keine Migration.** Der Migrationsläufer führt jedes Skript bei jedem Start aus und kennt kein Journal — diese Behebung braucht ihn nicht.
- [ ] `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL` (`CLAUDE.md`, „Die eine Regel, die den Aufbau trägt"). Die neuen Operationen kommen ohne aus; `Anhangsgrenze` liegt bereits in `KanbanC.Contracts`.
- [ ] Der Reproduktionstest ist ohne Fix rot und mit Fix grün — beide, Bedienweg und Bilanz.
- [ ] Keine neuen Fehler: alle Tests aller Ebenen grün, Coverage nicht gefallen, `TreatWarningsAsErrors` erfüllt.

## Betroffene Verzeichnisstruktur

- **Oberfläche:** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor` (Feld, `NimmDatei`, `UebernimmAnhang`) und `Kartendetail.razor.css` (der laufende Zustand der Fläche, Werte aus `gestaltung.css`).
- **Operationen der Oberfläche:** `Source/KanbanC.Blazor/Services/Ablegeflaechenstand.cs` (neu), `Source/KanbanC.Blazor/Services/Anhangausfall.cs` (neu).
- **Unberührt:** `Source/KanbanC.Blazor/Services/WebApiAufruf.cs`, `KartenApiKlient.cs`, die gesamte `KanbanC.WebApi`, `KanbanC.BL`, `KanbanC.Contracts` und alle Migrationen.
- **Tests:** `Source/KanbanC.Blazor.Tests/Services/AblegeflaechenstandTests.cs` (neu), `AnhangausfallTests.cs` (neu); `Source/KanbanC.PlaywrightTests/PageObjects/KartendetailSeite.cs` (Locator), `Source/KanbanC.PlaywrightTests/Tests/DateiAnKarteHaengenE2ETests.cs` (zwei neue Tests).

## Implementierungshinweise

- **Der Zustand gehört in die Komponente, die Regel nicht.** `Kartendetail.razor` hält das Feld „welche Datei läuft gerade" (`string?`); ob die Fläche daraufhin gesperrt ist und was sie sagt, rechnet `Ablegeflaechenstand`. Damit bleibt die Komponente Integration und die Regel prüfbar (IOSP, C01).
- **Freigeben gehört in ein `finally`.** Der Vorgangszustand muss auf jedem Ausgang zurückgesetzt werden — Erfolg, Zurückweisung, Größenwächter (`:1123`), Ausfall und Ausnahme. Eine Fläche, die nach einem Fehler gesperrt bleibt, tauscht den Datenverlust gegen eine unbedienbare Seite.
- **Nach dem `await` neu zeichnen.** Der Sperrzustand entsteht **vor** dem ersten `await` in `NimmDatei` und muss den Browser erreichen, bevor die Übertragung beginnt — sonst ist das Feld im entscheidenden Moment noch offen. Ein `StateHasChanged()` an dieser Stelle ist kein Beiwerk, sondern die Bedingung, unter der die Sperre wirkt.
- **`await using var inhalt = datei.OpenReadStream(...)` kann beim Verwerfen selbst werfen**, wenn die Referenz schon tot ist. Der Fangpunkt muss das `await using` **einschließen**, nicht darin liegen.
- **Welche Ausnahme genau fliegt, ist unbelegt.** Der erzwungene Überlappungstest ist das Orakel: gefangen wird der Typ, den er tatsächlich erzeugt, **benannt** — nie `catch (Exception)`. Ergibt der Lauf einen Typ, den auch Programmierfehler tragen, ist die Bilanz über den Vorgangszustand (`finally` sieht: nichts angekommen) das Mittel, nicht ein weiteres `catch`.
- **Benennung (C06/C07):** `Ablegeflaechenstand`, `Anhangausfall`, `laufendeDatei` — Bezeichner ohne echte Umlaute, Oberflächentexte und Kommentare **mit**. Die Begriffe aus `R00020` gelten unverändert: `Anhang`, `Anhangablage`, `Dateiname`, `Dateigroesse`, `Herunterladen`.
- **Kein `multiple` am Dateifeld.** `R00020` hat die Mehrfachauswahl ausdrücklich ausgeschlossen; diese Anforderung führt sie nicht durch die Hintertür ein.

## Offene Fragen

- **Soll die gesperrte Fläche einen Fortschritt zeigen** (Balken oder Prozent) statt nur „wird angehängt"? — **nicht entschieden, im stillen Lauf ohne gebaut.** Bei 3 MB dauert der Vorgang unter einer Sekunde, und ein Balken wäre Beiwerk, das die Haltung „Kanbanflow-dicht" gerade zurückdrängen will. Ein Fortschritt ließe sich nachrüsten, ohne die Sperre anzufassen. Vor der Umsetzung zu bestätigen.
- **Soll ein laufendes Anhängen abbrechbar sein?** — **nicht entschieden, im stillen Lauf ohne gebaut.** Ein Abbruch bräuchte einen zweiten Weg, auf dem ein Vorgang endet, und der ist genau die Klasse Weg, aus der dieser Fehler entstanden ist. Bei einer Obergrenze von 10 MB ist die Wartezeit kurz.
- **Betrifft dieselbe Lücke auch andere Ablegewege?** — **geprüft und verneint:** `<InputFile>` kommt im ganzen `Source/`-Baum genau einmal vor (`Kartendetail.razor:366`). Es gibt keinen zweiten Datei-Upload, der mitgezogen werden müsste. Sollte `I0019` oder ein späterer Slice einen zweiten anlegen, gilt diese Anforderung dort sinngemäß.
- **Respektiert Playwright `disabled` bei `SetInputFilesAsync`?** — **offen und entscheidend für den Zuschnitt der beiden Reproduktionstests.** Setzt Playwright Dateien auch auf ein gesperrtes Feld, ist Reproduktionstest 2 genau der erzwungene Weg, den die Bilanz absichert; verweigert es die Aktion, wird aus Test 2 eine Zusicherung, dass die Aktion verweigert wird. In beiden Fällen bleibt die Zusage „nie stumm" prüfbar, aber die Testform hängt daran. Klärt der erste Lauf.
- **Wie lange bleibt die Fläche nach dem Ablegen gesperrt, wenn die WebApi hängt?** — **offen.** Heute gibt es keine Zeitgrenze am HTTP-Aufruf; hängt die WebApi, hängt die Fläche mit. Das ist kein neuer Zustand (dieselbe Lage wie bei jeder anderen Handlung der Kartenseite), wird durch die Sperre aber erstmals **sichtbar**. Eine Zeitgrenze wäre eine eigene Anforderung über alle Handlungen, nicht nur über den Anhang.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist der schlimmste Fehler, den eine Anwendung mit lokaler Datenhaltung haben kann: der Benutzer legt eine Datei ab, die Oberfläche sieht heil aus, und die Datei existiert nirgends. Das Zielbild ist eine Karte, die alles trägt, was der Benutzer ihr gegeben hat — und, wo sie es einmal nicht kann, wenigstens sagt, dass sie es nicht konnte. Die Kausalkette: **wenn** die Ablegefläche während eines laufenden Anhängens kein zweites Ereignis mehr annimmt (X), **dann** kann kein zweiter Vorgang der laufenden Übertragung die Dateireferenz wegziehen, und die Wettlaufsituation, die den Verlust erzeugt, existiert im Bedienweg nicht mehr (Y), **und dann** endet jeder Ablegevorgang beobachtbar — als Zeile in der Datenbank oder als Meldung auf der Seite —, womit die Vision-Zusage „lokale Datenhaltung, unmittelbar zugänglich" wieder für alles gilt, was der Benutzer abgelegt hat (Z). Der Hebel liegt genau an der Fläche und nicht dahinter: hinter ihr, im Fangpunkt, ließe sich der Verlust nur noch **melden**, nicht mehr verhindern — die Datei ist zu diesem Zeitpunkt schon tot. Und nicht davor, in der WebApi: die hat die Datei nie gesehen, sie ist an diesem Fehler unbeteiligt. Dass die Behebung zugleich einen eigenen Fangpunkt und die Meldungstrennung mitbringt, ist keine Zugabe, sondern die zweite Hälfte derselben Zusage: eine Sperre, die den Regelfall dicht macht, und eine Meldung, die den Rest sichtbar macht — zusammen ergeben sie „nie stumm", einzeln nicht.

## Missing-Docs

- **Blazor Server, `InputFile` und überlappende `change`-Ereignisse:** Dass ein zweites `change`-Ereignis die Dateireferenzen des ersten browserseitig ersetzt und eine laufende `OpenReadStream`-Übertragung damit stirbt, ist im Repository nirgends dokumentiert und war die Ursache dieses Fehlers. Der Befund gehört nach `Dokumentation/Bibliotheken/`, falls er sich online nicht belegen lässt.
- **Welche Ausnahme ein abgerissener `IBrowserFile`-Strom in Blazor Server erzeugt** (Typ, Zeitpunkt, ob beim Lesen oder beim Verwerfen), ist unbelegt und entscheidet über die Form des Fangpunkts.
- **Actionability von `SetInputFilesAsync` bei `disabled`:** ob Playwright die Aktion verweigert oder ausführt, ist unbelegt und entscheidet über die Form von Reproduktionstest 2.
- **Zeitpunkt des Neuzeichnens vor einem langen `await` in einem Blazor-Ereignishandler:** dass der Sperrzustand den Browser erreichen muss, bevor die Übertragung beginnt, ist im Repository nirgends festgehalten; es ist die Bedingung, unter der jede solche Sperre wirkt.

## Notizen

- **Priorität: Kritisch.** Stiller Datenverlust ohne Workaround an der Oberfläche; der Benutzer erfährt nicht, dass er etwas verloren hat.
- **Betroffene Nutzer/Systeme:** jeder Mensch an der Kartenseite. **KI-Agenten sind nicht betroffen** — sie rufen `POST /api/karten/{karteId}/anhaenge` direkt auf, ohne SignalR und ohne `InputFile`; parallele Aufrufe der API sind unabhängig voneinander.
- **Workaround bis zur Behebung:** nach dem Ablegen warten, bis die Zeile in der Liste steht, erst dann die nächste Datei wählen. Die Liste ist dabei verlässlich — sie stimmte in allen 35 Beobachtungen mit der Datenbank überein.

### Verworfene Alternativen

Vollständig mit Achsen und Begründung unter „Lösungsvorschlag → Alternative Ansätze": Warteschlange · nur den Fehlerpfad reparieren · `MitAusfallmeldung` aufweiten · Direkt-Upload an die WebApi · die zweite Datei stumm verwerfen.

Dazu drei Formen, die gar nicht erst in die Tabelle kamen:

| Option | Warum verworfen |
|---|---|
| **Die Zwischenzusicherungen der drei Nachbartests in den neuen Test übernehmen** | Sie sind der Grund, warum der Fehler seit `R00020` unentdeckt blieb: als Synchronisation wirkend, als Zusicherung geschrieben. Ein Reproduktionstest, der so gebaut ist, wäre auch ohne Fix grün. |
| **Den Fehler nur am DOM prüfen** | DOM und Datenbank stimmten in allen 35 Beobachtungen überein. Ein DOM-Kriterium wäre grün gewesen, während die Datei verloren ging — es hätte den Fehler nicht gefunden und würde ihn auch beim nächsten Mal nicht finden. |
| **Die bestehenden vier Tests umbauen, damit sie den Fehler mit abdecken** | Sie prüfen, was ihr Name sagt (US-1, US-3, US-5), und sind grün, weil das Verhalten stimmt. Ein Reproduktionstest gehört daneben, nicht hinein; ein Test, der zwei Dinge zugleich prüft, sagt bei Rot nicht mehr, welches gebrochen ist. |

### Bewusst out of scope

- **Mehrere Dateien in einem Ablegevorgang.** `R00020` hat das ausdrücklich ausgeschlossen; diese Anforderung führt es nicht ein. Wenn es kommt, kommt es mit einer Fläche, die es zeichnet, und dann ist die Warteschlange dort die richtige Antwort.
- **Fortschrittsanzeige und Abbrechen** — siehe „Offene Fragen".
- **Eine Zeitgrenze am HTTP-Aufruf der Oberfläche.** Betrifft alle Handlungen der Kartenseite gleichermaßen und wäre eine eigene Anforderung.
- **Ein Aufräumlauf für verwaiste Dateien.** Unverändert die Lage aus `R00020`: bei diesem Fehler entsteht keine verwaiste Datei, weil die Bytes die WebApi nie erreichen.
- **Live-Aktualisierung der Anhangliste.** Gehört zu `D0007`.

### Angenommen im stillen Lauf

- **Sperre statt Warteschlange** — die Entscheidung, die der Auftrag verlangt hat; Begründung in voller Länge unter „Langfristige Lösung".
- **Der Text der gesperrten Fläche nennt die laufende Datei** („„wbs-export.md" wird angehängt …"), damit die Sperre begründet ist und nicht bloß grau.
- **Sind beide Sperrgründe zugleich gegeben**, nennt die Fläche den Identitätsgrund — er ist der, den der Benutzer auflösen kann; der laufende Vorgang löst sich von selbst.
- **Kein Fortschrittsbalken, kein Abbrechen** — siehe „Offene Fragen".
- **Der Fangpunkt liegt am Anhängen, nicht im gemeinsamen Helfer.** `WebApiAufruf` bleibt unverändert, weil `WebApiAufrufTests.cs:29` das ausdrücklich zusichert.
