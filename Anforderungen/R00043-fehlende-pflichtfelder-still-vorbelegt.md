---
id: R00043
status: Neu
datum: 2026-09-08
ursprung: Bug-Report
ursprungslauf: R00018, R00029
---

# R00043: Behebung der still vorbelegten Pflichtfelder in der WebApi

## Beschreibung

Fehlt in einem JSON-Rumpf ein **Pflichtfeld**, füllt `System.Text.Json` es stillschweigend mit `default` — `null` beim `string`, `0` bei der Zahl, `false` beim Wahrheitswert, `0001-01-01T00:00:00+00:00` beim Zeitpunkt, dem ersten Wert bei der Aufzählung. Die WebApi bemerkt das Fehlen nie; sie rechnet mit dem Vorgabewert weiter und antwortet dann entweder mit einem Serverfehler ohne Befund oder mit einem Befund, der auf den erfundenen Wert zeigt statt auf das fehlende Feld.

**Zwei Symptome, eine Ursache.** Beide sind belegt, beide verschwinden mit derselben Serialisierungseinstellung — deshalb ist es **eine** Anforderung und nicht zwei.

**Symptom 1 — der Serverfehler ohne Befund.**

- **Schritte:** `POST /api/karten/14/teilaufgaben` mit dem Rumpf `{}`.
- **Erwartet:** 400 mit Code, Meldung und Kompensationsaktion — „das Feld `text` fehlt, den Aufruf mit einem `text` wiederholen".
- **Tatsächlich:** `NullReferenceException` in `Teilaufgabentext.Normalisiert` und **HTTP 500 ohne jeden Rumpf**. Der Agent erfährt weder den Grund noch den nächsten Schritt.
- **Beleg:** Anmerkung 303 des Laufs (`kanbanC-anmerkungen.md`) — in **beiden** Läufen der A/B-Messung identisch, die Datei seit `R00018` unverändert; `Source/KanbanC.BL/Operations/Karten/Teilaufgabentext.cs:10` ruft `text.Trim()` ohne Null-Schutz, erreicht über `TeilaufgabenValidator.cs:25`.

**Symptom 2 — der Befund, der in die falsche Richtung zeigt.**

- **Schritte:** `POST /api/karten/14/zeiten` mit `{"kontributor":1,"beginn":"2026-09-05T12:00:00+00:00"}` — ohne `ende`.
- **Erwartet:** ein Befund, der sagt, dass ein Eintrag **ohne** Ende über `POST /api/karten/14/zeiten/laufend` entsteht.
- **Tatsächlich:** 400 mit Code `zeiteintrag-ende-vor-beginn` und der Meldung „Das Ende liegt vor dem Beginn: Beginn 2026-09-05T12:00:00Z, Ende **0001-01-01**T00:00:00Z." Die Kompensation lautet „den Aufruf mit einem `ende` nach dem `beginn` wiederholen" — sie schickt den Agenten auf dieselbe Route zurück, statt ihn auf die richtige zu weisen.
- **Beleg:** Anmerkung 453 des Laufs — „das Kriterium ist erfüllt (kein Eintrag entsteht), **die Meldung nur formal**"; `Source/KanbanC.BL/Operations/Zeiten/Zeitspanne.cs:37,47`.

Zahlt ein auf: [Vision](R00000-vision.md) — „Eine API auf Augenhöhe mit der Oberfläche … was ein Mensch klicken kann, kann ein Agent aufrufen." Ein Mensch am Formular kann kein Pflichtfeld weglassen; ein Agent an der API kann es jederzeit. Augenhöhe heißt, dass er in diesem Fall dieselbe brauchbare Auskunft bekommt wie der Mensch — und nicht einen Serverfehler oder einen Wegweiser in die falsche Richtung.

**Der Fehler stammt aus zwei Slices und liegt in keinem von beiden.** `I0016`/[`R00018`](R00018-karte-gliedern.md) hat den Teilaufgabentext gebaut, `I0025`/[`R00029`](R00029-zeiteintrag-nachtragen-und-aendern.md) den Nachtrag; beide haben ihre Zusagen gehalten. Was keiner der beiden Slices geregelt hat, ist die Frage **vor** der Fachlichkeit: was gilt, wenn der Rumpf das Feld gar nicht trägt. Diese Anforderung schließt genau diese Lücke — an einer Stelle, für die ganze WebApi.

## Ursachenanalyse

### Root Cause

**`Source/KanbanC.WebApi/Program.cs` setzt keine JSON-Optionen.** Es gibt im ganzen Startpfad kein `ConfigureHttpJsonOptions`; damit gilt die Vorgabe von `System.Text.Json`, und die lautet: **`RespectRequiredConstructorParameters` ist `false`.** Ein Konstruktorparameter ohne Vorgabewert wird auch dann gefüllt, wenn das JSON ihn nicht nennt — mit `default`.

Alle Anfrage-DTOs des Projekts sind positionale `record`-Typen (C08, immutable). Ihre Pflichtigkeit steht damit **ausschließlich im Konstruktor** — und genau die ignoriert die Deserialisierung heute. `Source/KanbanC.Contracts/Zeiten/ZeiteintragNachtragenAnfrage.cs:6-7` sagt es wörtlich:

```
// **Das Ende ist pflichtig.** Ein Eintrag ohne Ende ist kein Nachtrag, sondern ein Start, und den
// hat `POST /api/karten/{karteId}/zeiten/laufend`. Die Pflichtigkeit steht damit im Typ und
// braucht keinen Befund.
```

Das ist die Annahme, die der Fehler widerlegt: **die Pflichtigkeit steht im Typ und wirkt trotzdem nicht.** Dieselbe stille Annahme trägt jedes andere Anfrage-DTO.

### Warum die Einstellung allein nicht genügt — der zweite Teil der Ursache

`Program.cs:51` setzt `RouteHandlerOptions.ThrowOnBadRequest = true`, und `Anhangsgrenzenwaechter.cs:23` fängt davon **nur** `BadHttpRequestException when (fehler.InnerException is InvalidDataException)`. Eine gescheiterte JSON-Bindung trägt eine `JsonException` im Inneren und läuft durch — sie wird zu einer Antwort des Rahmens **ohne unseren Befund**.

Das ist im Bestand zweimal gemessen und als Regressionsschutz festgehalten:

- `Source/KanbanC.WebApi.IntegrationTests/Api/KontributorartProbeTests.cs:37-48` — `PROBE_Wenn_die_Art_an_der_Route_einen_unbekannten_Text_traegt_dann_antwortet_ASP_NET_selbst_ohne_unseren_Befund`: 400, und `rumpf Does.Not.Contain("befunde")`.
- `Source/KanbanC.WebApi.IntegrationTests/Api/DateOnlyEingabeProbeTests.cs:54-63` — dieselbe Messung für `faelligAm: ""`, mit demselben Ergebnis.

**Daraus folgt: `RespectRequiredConstructorParameters` allein tauscht bei Symptom 1 die 500 gegen eine 400 ohne Rumpf** — der Vertrag aus `R00007` („keine Fehlerantwort mit leerem Rumpf") bliebe gebrochen, nur an anderer Stelle. Die Einstellung braucht einen Wächter neben sich, so wie die Anhangsgrenze einen hat. Das ist keine Zutat, sondern die zweite Hälfte derselben Ursache.

### Der dritte Teil: ein Feld auf `null` ist kein fehlendes Feld

`RespectRequiredConstructorParameters` greift nur, wenn das Feld **fehlt**. Steht es auf `null` (`{"text":null}`), ist es vorhanden, und `null` erreicht den Validator wie heute — die `NullReferenceException` bliebe für diesen Rumpf bestehen. Für einen Agenten sind „Feld weggelassen" und „Feld auf null" dieselbe Aussage; die Antwort muss dieselbe sein.

### Betroffene Komponenten

**Der Bestand, geprüft — nicht angenommen.** Alle 22 Anfrage-DTOs, die an einer Route im JSON-Rumpf gebunden werden, und was ein fehlendes Pflichtfeld heute auslöst:

| Route | Feld | Heute bei fehlendem Feld | Beleg |
|---|---|---|---|
| `POST /api/karten/{karteId}/teilaufgaben` | `text` | **500**, `NullReferenceException` | `Teilaufgabentext.cs:10` ← `TeilaufgabenValidator.cs:25` |
| `POST /api/karten/{karteId}/kommentare` | `text` | **500**, `NullReferenceException` | `Kommentartext.cs:10` ← `KommentarValidator.cs:24` |
| `POST /api/karten/{karteId}/dateiverweise` | `pfad` | **500**, `NullReferenceException` | `Dateiverweispfad.cs:14` ← `DateiverweisValidator.cs:31` |
| `POST /api/boards/{boardId}/kartenklassen` | `name`, `praefix` | **500**, `NullReferenceException` | `KartenklassenValidator.cs:34,48` |
| `PUT /api/karten/{karteId}/etiketten` | `etiketten` | **500**, `NullReferenceException` beim Durchlauf | `EtikettenValidator.cs:21` |
| `PUT /api/boards/{boardId}/spalten/reihenfolge` | `spalteIds` | **500**, `NullReferenceException` | `SpaltenreihenfolgeValidator.cs:15` |
| `POST /api/karten/{karteId}/zeiten` | `ende` | **400 mit falschem Befund** `zeiteintrag-ende-vor-beginn`, Meldung nennt `0001-01-01` | `Zeitspanne.cs:37,47` |
| `POST /api/karten/{karteId}/zeiten` · `PUT …/zeiten/{id}` | `beginn` | 400 `zeiteintrag-in-der-zukunft` **oder** stiller Eintrag auf `0001-01-01` | `Zeitspanne.cs:57` |
| `POST /api/kontributoren` · `PUT /api/kontributoren/{id}` | `art` | **201/200 — der Aufruf geht durch**: fehlt `art`, entsteht ein Kontributor der Art `Mensch` | `Kontributorart.cs:7` (erster Wert) |
| `PUT /api/karten/{karteId}` | `farbe` | **200 — der Aufruf geht durch**: die Karte bekommt `Ohne` | `Kartenfarbe.cs:11` |
| `PUT …/teilaufgaben/{id}/stand` | `abgehakt` | **200 — der Haken wird entfernt**, ohne dass jemand es verlangt hat | `Teilaufgabenstand.cs:6` |
| `PUT /api/boards/{id}/archivierung` · `…/kartenzahl` · `PUT /api/kontributoren/{id}/stilllegung` | `istArchiviert` · `zeigtKartenzahl` · `istStillgelegt` | **200 — es wird abgeschaltet**, weil `false` die Vorgabe ist | `Archivierung.cs:6`, `Kartenzahlanzeige.cs:5`, `Stilllegung.cs:6` |
| `POST …/zeiten/laufend` · `POST …/kommentare` · `POST …/dateiverweise` · `POST …/zeiten` | `kontributor` | 400/404 „Kontributor 0 ist unbekannt" — Grund und Kompensation zeigen auf eine erfundene Nummer | `Nichtgefunden.cs` |
| `PUT /api/boards/{boardId}/karten/{karteId}/lage` | `spalteId`, `position` | 400/404 auf Spalte `0` bzw. Position `0` | `Kartenlage.cs:8` |
| `POST /api/boards` · `PUT /api/boards/{id}` · `POST|PUT …/spalten` · `POST …/karten` · `PUT /api/karten/{id}` | `name` · `bezeichnung` · `titel` | **400 mit richtigem Befund** (`…-leer`) — diese Validatoren prüfen null-sicher mit `IsNullOrWhiteSpace` | `Boardname.cs:14`, `SpaltenValidator.cs:23`, `KartenValidator.cs:46`, `KontributorenValidator.cs:27` |

**Nicht betroffen — geprüft und begründet:**

- **Die multipart-Routen.** `POST /api/karten/{karteId}/anhaenge` (`KartenEndpunkte.cs:193`), `POST /api/boards/import` (`BoardimportEndpunkte.cs:33`) und der WBS-Import binden `IFormFile` und `[FromForm]`, nicht JSON. Sie laufen nicht durch `System.Text.Json`. `Importanfrage.cs:5-7` hat die Frage dort bereits richtig beantwortet: „**Trocken hat die Vorgabe true**: fehlt das Feld, wird nichts geschrieben … die teure Richtung gehört nie in die Vorgabe." Genau dieses Prinzip fehlt der JSON-Bindung.
- **Die Abfrageparameter.** `AbfrageparameterProbeTests.cs` misst die Bindung von `?archiviert=`; sie hat mit der Rumpf-Deserialisierung nichts zu tun und läuft in einer eigenen `Probeanwendung` ohne unsere Wächter.
- **`KanbanC.Blazor`.** Alle Klienten serialisieren typisierte `record`-Instanzen; ein Feld kann dort nicht fehlen. Die Oberfläche erzeugt keinen der beschriebenen Rümpfe und ändert sich nicht.
- **`Kartenlage.Kontributor`** trägt einen Vorgabewert (`long? Kontributor = null`) und bleibt auch nach der Einstellung optional — so ist es gemeint.

**Ausgeschlossen wurde:** der Datenzugriff und das Schema. Kein Symptom entsteht in einem Repository, keines berührt eine Tabelle; bei Symptom 1 bricht der Aufruf ab, bevor irgendetwas geschrieben wird, bei Symptom 2 weist der Dienst zurück. Es entsteht keine Migration.

### Der Test, der Symptom 2 verdeckt hat

`Source/KanbanC.WebApi.IntegrationTests/Api/ZeitenEndpunkteTests.cs:806-819` `Wenn_ein_Nachtrag_ohne_Ende_geschickt_wird_dann_entsteht_kein_Eintrag` schickt **genau** den Rumpf aus Symptom 2 und ist grün — er prüft `InRange(400, 499)` und „kein Eintrag". Beides stimmt. Sein Kommentar behauptet dabei etwas, was heute nicht gilt:

```
// Ein Nachtrag ohne „ende" ist kein Nachtrag, sondern ein Start — und der hat eine eigene
// Adresse. Die Gestalt der Anfrage sagt es, und die WebApi nimmt ihn nicht an.
```

Die Gestalt der Anfrage sagt es eben **nicht** — sie wird nicht gelesen. Der Test ist grün, weil er nur den Statusbereich zusichert; welchen Befund die Antwort trägt, prüft er nicht (Skill `test-ehrlichkeit`: ein Test, der weniger prüft, als sein Name behauptet). Derselbe Zuschnitt findet sich bei `:84-96` `Wenn_der_Rumpf_keinen_Kontributor_traegt_dann_wird_der_Aufruf_zurueckgewiesen_und_legt_nichts_an` (`{}` an `POST …/zeiten/laufend`): grün, weil der Dienst über den erfundenen Kontributor `0` stolpert, nicht weil das Feld fehlt.

**`FehlervertragTests` konnte beide Fälle nicht finden**, und das ist kein Versäumnis, sondern eine Eigenschaft seiner Bauweise: er schickt jede Anfrage als typisierten `record` über `PostAsJsonAsync`/`PutAsJsonAsync` (`:78-186`). Aus einem `record` **kann** kein Feld fehlen. Der Vertragstest deckt jede Route ab, aber nur mit vollständigen Rümpfen.

## Lösungsvorschlag

### Langfristige Lösung

**Ein fehlendes Pflichtfeld ist ein Befund wie jeder andere — an einer Stelle für die ganze WebApi.** Vier Teile, die zusammengehören:

1. **Die Einstellung.** `Program.cs` bekommt `builder.Services.ConfigureHttpJsonOptions(optionen => optionen.SerializerOptions.RespectRequiredConstructorParameters = true);` — neben den beiden schon vorhandenen `Configure`-Zeilen und mit demselben Anspruch begründet. Ein Pflichtfeld, das fehlt, wird ab da nicht mehr erfunden: die Deserialisierung scheitert.
2. **Der `Rumpfwaechter`.** Eine zweite Middleware neben `Anhangsgrenzenwaechter`, mit **disjunktem** Filter: `catch (BadHttpRequestException fehler) when (fehler.InnerException is JsonException)`. Sie antwortet 400 mit einer `Zurueckweisung`, deren Befund den Grund nennt, das fehlende Feld benennt und als Kompensation die Route des Aufrufers samt seiner Nummern nennt — dieselbe Form, die `Anhangsgrenzenwaechter` für den abgebrochenen Rumpf schon hat, und derselbe Grund: der Abbruch ist eine Eigenschaft des Wirts, nicht ein Zustand der Fachlichkeit.
3. **Der Zeiten-Sonderbefund.** `ZeiteintragNachtragenAnfrage.Ende` wird `DateTimeOffset?`, und `ZeitenService.TrageNach` weist einen Nachtrag ohne Ende mit einem **eigenen** Befund zurück, dessen Kompensation `POST /api/karten/{karteId}/zeiten/laufend` nennt. Nur so entsteht die Auskunft, die Anmerkung 453 vermisst; aus Teil 1 und 2 käme allein „das Feld `ende` fehlt", und der Agent wüsste immer noch nicht, dass es für seinen Fall eine andere Adresse gibt. Das ist dasselbe Vorgehen, das Anmerkung 26 für den Archivfilter gewählt hat: die Bindung lässt den Wert durch, damit die Fachlichkeit die Antwort geben kann.
4. **Null-sichere Validatoren.** Die sechs Stellen, die `Normalisiert(...)` auf einem Feld aufrufen, das `null` sein kann, prüfen vorher — in der Hausform, die `KartenValidator.cs:46`, `SpaltenValidator.cs:23`, `Boardname.cs:14` und `KontributorenValidator.cs:27` schon benutzen: `string.IsNullOrWhiteSpace` zuerst, dann der bestehende `…-leer`-Befund. Damit beantwortet `{"text":null}` dieselbe Frage wie `{}`, und die `Normalisiert`-Operationen behalten ihren ehrlichen Nicht-null-Kontrakt.

**Warum die Einstellung und nicht sechs Null-Prüfungen:** die Null-Prüfungen allein heilen nur die `string`-Fälle. Sie lassen `0001-01-01` beim Zeitpunkt stehen, den stillen `Mensch` bei der Kontributorart, das stille Abhaken beim Teilaufgabenstand und das stille Abschalten bei `archivierung`, `kartenzahl` und `stilllegung`. Und sie sind eine Regel, die bei jedem neuen Feld neu erinnert werden muss; die Einstellung gilt für jedes Feld, das je dazukommt — auch für die, an die beim Schreiben niemand denkt.

**Warum trotzdem die Null-Prüfungen dazu:** die Einstellung sieht ein vorhandenes `null` nicht. Ohne Teil 4 bliebe Symptom 1 für den Rumpf `{"text":null}` genau so bestehen, wie es heute ist.

### Alternative Ansätze

| Option | Achsen | Warum verworfen |
|---|---|---|
| **Nur `RespectRequiredConstructorParameters`** (die Einstellung allein) | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko **hoch** | Tauscht die 500 gegen eine 400 **ohne Rumpf** und bricht damit `R00007` weiter — belegt durch `KontributorartProbeTests:47` und `DateOnlyEingabeProbeTests:63`, die genau diese Antwort für andere JSON-Fehler schon messen. Ist Teil 1 der gewählten Lösung, nie für sich allein. |
| **Nur Null-Prüfungen in den sechs Validatoren** (lokaler Fix) | Komplexität niedrig · Testbarkeit hoch · Reversibilität hoch · Risiko mittel | Heilt Symptom 1, lässt Symptom 2 und jeden Zahl-, Wahrheits- und Aufzählungsfall unberührt. Eine Regel, die bei jedem neuen Pflichtfeld neu erinnert werden muss. |
| **Jedes Pflichtfeld nullable machen und jeden Fall fachlich beantworten** | Komplexität hoch · Testbarkeit hoch · Reversibilität niedrig · Risiko mittel | Hebt die Aussagekraft der Contracts auf: 22 DTOs verlören ihre Pflichtangabe im Typ, und jede Route bekäme einen neuen Befundzweig, den es fachlich gar nicht gibt. Für die **eine** Route, an der das Fehlen fachlich etwas anderes bedeutet (der Zeiten-Nachtrag), tut die gewählte Lösung genau das — dort trägt es. |
| **Ein Minimal-API-Endpunktfilter je Route** (`AddEndpointFilter` mit Pflichtfeldprüfung) | Komplexität hoch · Testbarkeit mittel · Reversibilität mittel · Risiko mittel | 50 Routen, 50 Filter, und die Pflichtangabe stünde ein zweites Mal neben dem Konstruktor — eine zweite Wahrheit, die beim ersten neuen Feld auseinanderläuft. |
| **Eine Validierungsbibliothek** (FluentValidation, `[Required]` samt Modellvalidierung) | Komplexität hoch · Testbarkeit mittel · Reversibilität niedrig · Risiko mittel | Neue Abhängigkeit für eine Frage, die eine Zeile Konfiguration beantwortet; und ihre Fehlerform ist nicht `Zurueckweisung`, müsste also ohnehin übersetzt werden. |
| **`RespectNullableAnnotations` zusätzlich einschalten**, statt die Validatoren null-sicher zu machen | Komplexität niedrig · Testbarkeit mittel · Reversibilität hoch · Risiko **hoch** | Die Einstellung wirkt in **beide** Richtungen und würde auch beim **Schreiben** jeder Antwort werfen, sobald ein nicht-nullbares Feld eines Antwort-DTOs zur Laufzeit `null` trägt. Das ist ein Risiko über den ganzen Antwortbestand für einen Gewinn, den vier Zeilen in den Validatoren sicher erbringen. |

## Test-Strategie

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`. Beide Reproduktionstests laufen auf der **Integrationsebene** (`KanbanC.WebApi.IntegrationTests`): die Ursache sitzt in der Serialisierung des Wirts und ist unterhalb der Route nicht auslösbar.

### Unit Test zur Bug-Reproduktion

**Reproduktionstest 1 — Symptom 1** (`TeilaufgabenEndpunkteTests` bzw. `KartenEndpunkteTests`, neu):
`Wenn_der_Rumpf_kein_text_traegt_dann_antwortet_die_Route_mit_400_und_einem_Befund_der_das_Feld_nennt`

- *Arrange:* Board, Spalte, Karte über den vorhandenen Aufbau.
- *Act:* `PostAsync($"/api/karten/{karteId}/teilaufgaben", JsonRumpf("{}"))`.
- *Assert:* Status `400`; `Fehlerrumpf.Lies` liefert genau einen Befund; sein `Code` ist stabil, seine `Meldung` nennt das Feld `text`, seine `Kompensation` nennt `POST /api/karten/{karteId}/teilaufgaben` mit der echten Kartennummer. Zusätzlich: die Karte trägt danach **keine** Teilaufgabe.
- *Warum ohne Fix rot:* die Antwort ist heute **500**, und `Fehlerrumpf.Lies` findet keinen Rumpf. Der Test scheitert an der ersten Zusicherung.

**Reproduktionstest 2 — Symptom 2** (`ZeitenEndpunkteTests`, neu):
`Wenn_ein_Nachtrag_kein_ende_traegt_dann_verweist_der_Befund_auf_die_Route_der_laufenden_Messung`

- *Act:* `PostAsync($"/api/karten/{karteId}/zeiten", JsonRumpf($$"""{"kontributor":{{stefanId}},"beginn":"2026-09-05T12:00:00+00:00"}"""))` — wörtlich der Rumpf aus Anmerkung 453.
- *Assert:* Status `400`; der Befund trägt **nicht** den Code `zeiteintrag-ende-vor-beginn`; seine `Meldung` enthält **nicht** die Zeichenfolge `0001-01-01`; seine `Kompensation` enthält `/zeiten/laufend`. Zusätzlich: es entsteht kein Eintrag.
- *Warum ohne Fix rot:* heute lautet der Code genau `zeiteintrag-ende-vor-beginn` und die Meldung enthält genau `0001-01-01` — beide Zusicherungen schlagen fehl. **Beide Enden sind rot**, nicht nur eines.

**Ergänzende Fälle auf derselben Ebene** (kein Ersatz für die beiden Reproduktionstests):

- `{"text":null}` an derselben Teilaufgabenroute — dieselbe Antwortform wie `{}`. Ohne Teil 4 der Lösung rot mit 500.
- `POST /api/kontributoren` mit `{"name":"Claude"}` — wird zurückgewiesen, statt einen Kontributor der Art `Mensch` anzulegen. Heute grün mit 201; **der Test ist nach dem Fix der Beleg, dass die Einstellung wirklich WebApi-weit gilt.**
- `PUT …/teilaufgaben/{id}/stand` mit `{}` — der Haken bleibt, wie er war.

**Unit Tests (pure Logik):**

- Der Befundtext des `Rumpfwaechter` (Code, Meldung, Kompensation) entsteht in einer Operation und wird dort ohne Wirt geprüft — Muster `AnhangValidator.RumpfUeberDerGrenze`.
- `ZeitenServiceTests` bzw. `ZeitspanneTests` um den neuen Befund „Nachtrag ohne Ende": genau ein Befund, Kompensation nennt die Route der laufenden Messung, und `beginn == ende` bleibt weiterhin erlaubt.

### Code-Extraktion für isoliertes Testing

Der Befund des `Rumpfwaechter` gehört **nicht** in die Middleware, sondern in eine Operation unter `Source/KanbanC.BL/Operations/Fehler/` oder neben `AnhangValidator.RumpfUeberDerGrenze` — dieselbe Trennung, die `Anhangsgrenzenwaechter.cs:32` schon vollzieht: die Middleware fängt und schreibt, die Operation formuliert. Nur so ist der Wortlaut ohne Wirt prüfbar.

### Regressionstests

- **`FehlervertragTests` wächst** um eine Fallgruppe „Pflichtfeld fehlt": je Route mit mindestens einem Pflichtfeld ein Aufruf mit **rohem JSON**, das genau dieses Feld auslässt. `PostAsJsonAsync` mit einem `record` kann das nicht — die neuen Fälle brauchen `StringContent`. Danach gilt die Zusage „jede Fehlerantwort trägt Code, Meldung und Kompensation" auch für unvollständige Rümpfe.
- **`KontributorartProbeTests.cs:47`** und **`DateOnlyEingabeProbeTests.cs:63`** werden **rot** und werden umgeschrieben (siehe Akzeptanzkriterien).
- `AbfrageparameterProbeTests` bleibt **unverändert** grün — Abfrageparameter, eigene `Probeanwendung`, kein Wächter.
- `ZeitenEndpunkteTests.cs:806` und `:84` bleiben grün; ihre Zusicherungen werden geschärft (siehe Akzeptanzkriterien).
- Alle E2E-Suiten bleiben **ohne Änderung** grün: die Oberfläche schickt typisierte `record`-Instanzen und kann keinen unvollständigen Rumpf erzeugen.
- `Anhangsgrenzenwaechter` und sein Test bleiben unverändert; ein Rumpf über der Grenze bekommt weiter **seinen** Befund und nicht den des `Rumpfwaechter`.

## Akzeptanzkriterien

### Ein fehlendes Pflichtfeld wird beantwortet, nicht erfunden

- [ ] `POST /api/karten/{karteId}/teilaufgaben` mit `{}` antwortet mit **400** und einem Befund, der Code, Meldung und Kompensation trägt. Kein Statuscode ≥ 500 an dieser Route, für keinen Rumpf.
- [ ] Die Meldung nennt das fehlende Feld beim Namen (`text`), die Kompensation die Route des Aufrufers **samt seiner Kartennummer** — dieselbe Form, die `TeilaufgabenValidator.cs:23` für den leeren Text schon benutzt.
- [ ] `{"text":null}` bekommt dieselbe Antwortform wie `{}`. Für einen Agenten sind beide dieselbe Aussage.
- [ ] Dasselbe gilt an **jeder** Route mit Pflichtfeld: Kommentar (`text`), Dateiverweis (`pfad`), Kartenklasse (`name`, `praefix`), Etiketten (`etiketten`), Spaltenreihenfolge (`spalteIds`), Zeiten (`kontributor`, `beginn`, `ende`), Kartenlage (`spalteId`, `position`), Kontributor (`art`), Kartenfarbe (`farbe`), Teilaufgabenstand (`abgehakt`), Archivierung, Kartenzahlanzeige, Stilllegung.
- [ ] **Kein Vorgabewert wird mehr zur Aussage.** Rechenbeispiel: `POST /api/kontributoren` mit `{"name":"Claude"}` legt **keinen** Kontributor mehr an — heute entsteht dabei einer der Art `Mensch`. Ebenso: `PUT …/teilaufgaben/{id}/stand` mit `{}` entfernt keinen Haken, und `PUT /api/boards/{id}/archivierung` mit `{}` archiviert nicht.
- [ ] Optionale Felder bleiben optional. Rechenbeispiel: `PUT /api/boards/{boardId}/karten/{karteId}/lage` mit `{"spalteId":7,"position":0}` geht weiterhin durch — `Kartenlage.Kontributor` trägt einen Vorgabewert und ist damit kein Pflichtfeld.

### Der Zeiten-Nachtrag weist auf die richtige Adresse

- [ ] `POST /api/karten/{karteId}/zeiten` ohne `ende` antwortet mit einem Befund, dessen **Kompensation `POST /api/karten/{karteId}/zeiten/laufend` nennt** — die Adresse, an der ein Eintrag ohne Ende tatsächlich entsteht.
- [ ] Dieser Befund trägt **nicht** den Code `zeiteintrag-ende-vor-beginn`, und seine Meldung enthält **nicht** die Zeichenfolge `0001-01-01`. Beides ist wörtlich zugesichert, weil beides der belegte Ist-Zustand ist.
- [ ] Der Befund `zeiteintrag-ende-vor-beginn` bleibt unverändert für den Fall, für den er gedacht ist: beide Zeitpunkte angegeben, das Ende früher. Rechenbeispiel: `beginn 2026-09-05T13:30:00Z`, `ende 2026-09-05T12:00:00Z` → weiterhin `zeiteintrag-ende-vor-beginn` mit beiden Werten in der Meldung (`ZeitenEndpunkteTests.cs:823` bleibt grün).
- [ ] `beginn == ende` bleibt erlaubt — eine Dauer von null ist eine wahre Aussage über eine sehr kurze Arbeit (`Zeitspanne.cs:12`).
- [ ] `PUT …/zeiten/{zeiteintragId}` behält sein **nullbares** `ende`: „Ende is null heißt läuft" ist die eine Regel über alle Interactions des Dialogs (`ZeiteintragAendernAnfrage.cs:5`), und der Änderungsaufruf führt keine zweite ein. Ein `ende`, das dort **fehlt**, ist danach trotzdem kein stiller `0001-01-01`, sondern schlicht kein Ende.

### Was die WebApi-weite Einstellung an bestehenden Zusagen ändert

- [ ] **Benannte Änderung 1 — `KontributorartProbeTests.cs:37-48`.** `PROBE_Wenn_die_Art_an_der_Route_einen_unbekannten_Text_traegt_dann_antwortet_ASP_NET_selbst_ohne_unseren_Befund` wird durch den `Rumpfwaechter` **rot**: die Route antwortet weiterhin 400, aber **mit** Befund. Name und Zusicherung werden umgeschrieben (`…_dann_antwortet_die_Route_mit_unserem_Befund`, `Does.Contain` statt `Does.Not.Contain`). Der Erkenntniswert der Probe bleibt: die Deserialisierung weist den unbekannten Text weiterhin **vor** dem Handler ab.
- [ ] **Benannte Änderung 2 — `DateOnlyEingabeProbeTests.cs:54-63`.** Wird aus demselben Grund rot und wird gleich umgeschrieben. Der Kommentar `:12-15` („an der Route wird daraus eine 400 von ASP.NET selbst, ohne unseren Befund") wird nachgeführt; der daraus abgeleitete Schluss — die Oberfläche schickt für ein geleertes Datumsfeld `null` und nicht den leeren Text — **bleibt gültig und wird nicht rückgängig gemacht**.
- [ ] **Benannte Änderung 3 — `FehlervertragTests`.** Die Fallgruppe „Pflichtfeld fehlt" kommt hinzu, mit rohem JSON statt typisierter `record`-Instanzen. Der zweite Test (`Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind`) und die Liste `RoutenOhneFehlerantwort` bleiben unverändert: es kommt keine Route hinzu.
- [ ] **Benannte Änderung 4 — `ZeitenEndpunkteTests.cs:806`.** `Wenn_ein_Nachtrag_ohne_Ende_geschickt_wird_dann_entsteht_kein_Eintrag` bleibt grün, wird aber von `InRange(400, 499)` auf den **Befund** verschärft; sein Kommentar („die Gestalt der Anfrage sagt es") stimmt erst nach dieser Anforderung und wird erst dann so stehen gelassen.
- [ ] **Benannte Änderung 5 — `ZeitenEndpunkteTests.cs:84`.** `Wenn_der_Rumpf_keinen_Kontributor_traegt_…` bleibt grün, ändert aber seinen Grund: die Zurückweisung kommt künftig aus dem fehlenden Pflichtfeld und nicht mehr aus dem unbekannten Kontributor `0`. Die Zusicherung wird auf den neuen Befund gezogen.
- [ ] **Benannte Änderung 6 — `ZeiteintragNachtragenAnfrage.cs`.** `Ende` wird `DateTimeOffset?`; der Kommentar „Die Pflichtigkeit steht damit im Typ und braucht keinen Befund" wird ersetzt — er war die Annahme, an der der Fehler hing. Die Pflichtigkeit steht danach im **Dienst**, mit Befund.
- [ ] **Unverändert — `AbfrageparameterProbeTests.cs:18-27`.** Bleibt grün und wird **nicht** angefasst: Abfrageparameter laufen nicht durch die Rumpf-Deserialisierung, und die Probe läuft in einer eigenen `Probeanwendung` ohne unsere Wächter. Wer sie „mit angleicht", hat eine fremde Messung zerstört.
- [ ] **Unverändert — `Anhangsgrenzenwaechter`.** Sein Filter (`InnerException is InvalidDataException`) und der des `Rumpfwaechter` (`InnerException is JsonException`) sind **disjunkt**; ein Rumpf über der Anhangsgrenze bekommt weiterhin `AnhangValidator.RumpfUeberDerGrenze` und nicht den neuen Befund. Belegt durch den bestehenden Grenztest, der unverändert grün bleibt.
- [ ] **Unverändert — `KartenValidator.cs:27-29`.** Die Feststellung „aus einem JSON-Rumpf ist dieser Befund nicht auslösbar: unbekannten Text weist die Deserialisierung vorher ab" bleibt wahr; nur die Antwort des Rahmens trägt danach einen Befund.
- [ ] **Unverändert — `KanbanC.Blazor` und alle E2E-Suiten.** Kein Klient, keine Komponente, kein Playwright-Test wird geändert; die Oberfläche kann keinen unvollständigen Rumpf erzeugen. `KanbanC.Blazor` bekommt **keine** Projektreferenz auf `KanbanC.BL` (`CLAUDE.md`, „Die eine Regel, die den Aufbau trägt").
- [ ] **Keine Änderung am Schema und keine Migration.** Der Migrationsläufer führt jedes Skript bei jedem Start aus und kennt kein Journal — diese Behebung braucht ihn nicht.

### Der Vertrag gilt vollständig

- [ ] Beide Reproduktionstests sind ohne Fix **rot** und mit Fix grün.
- [ ] Es gibt keine Route der WebApi, die auf einen unvollständigen JSON-Rumpf mit einem Statuscode ≥ 500 oder mit einer Fehlerantwort ohne Befund antwortet — geprüft in `FehlervertragTests` über **alle** Routen mit Pflichtfeld, nicht nur über die beiden aus den Symptomen.
- [ ] Jeder neue Befund trägt alle drei Teile: stabiler `Code`, `Meldung` mit den konkreten Werten des Vorgangs, `Kompensation` als ausführbarer nächster Schritt mit Route (`Fehlerbefund.cs:3-5`).
- [ ] Keine neuen Fehler: alle Tests aller Ebenen grün, Coverage nicht gefallen, `TreatWarningsAsErrors` erfüllt.

## Betroffene Verzeichnisstruktur

- **Wirt:** `Source/KanbanC.WebApi/Program.cs` (die JSON-Optionen, neben `FormOptions` und `RouteHandlerOptions`) und `Source/KanbanC.WebApi/Endpunkte/Rumpfwaechter.cs` (neu, neben `Anhangsgrenzenwaechter.cs`, registriert in `Program.cs` gleich daneben).
- **Fachlogik:** `Source/KanbanC.BL/Operations/Karten/` — Null-Prüfung in `TeilaufgabenValidator`, `KommentarValidator`, `DateiverweisValidator`, `EtikettenValidator`; `Source/KanbanC.BL/Operations/Klassen/KartenklassenValidator.cs`; `Source/KanbanC.BL/Operations/Boards/SpaltenreihenfolgeValidator.cs`. Der Befundtext des Wächters als Operation neben den übrigen Fehleroperationen.
- **Zeiten:** `Source/KanbanC.Contracts/Zeiten/ZeiteintragNachtragenAnfrage.cs` (`Ende` wird nullbar) und `Source/KanbanC.BL/Integrations/Zeiten/ZeitenService.cs` (`TrageNach` weist ohne Ende mit eigenem Befund zurück).
- **Unberührt:** die gesamte `KanbanC.Blazor`, alle Migrationen unter `Source/KanbanC.BL/Persistenz/Migrationen/`, alle Repositories, die multipart-Routen (Anhang, WBS-Import, Boardimport).
- **Tests:** `Source/KanbanC.WebApi.IntegrationTests/Api/FehlervertragTests.cs` (neue Fallgruppe), `ZeitenEndpunkteTests.cs` (ein neuer Test, zwei geschärfte), `KontributorartProbeTests.cs` und `DateOnlyEingabeProbeTests.cs` (je eine umgeschriebene Zusicherung), der Teilaufgaben-Reproduktionstest; `Source/KanbanC.BL.Tests/` für den Befundtext und den neuen Zeiten-Befund.

## Technische Überlegungen

### Ablauf

1. **Probe vor Bau** (Skill `dependency-probe`, Muster `KontributorartProbeTests`)
   - 1.1 `PROBE`: `JsonSerializer.Deserialize<TeilaufgabeAnlegenAnfrage>("{}", optionenMitRespect)` wirft `JsonException` — und die `Message` nennt den Parameternamen.
   - 1.2 `PROBE` an der Route: welche Ausnahme kommt in der Middleware an (`BadHttpRequestException`? welcher `InnerException`-Typ?) und welchen Status trägt sie.
   - 1.3 `PROBE`: ist der Feldname aus der Ausnahme **zuverlässig** herauszulesen — oder muss die Meldung ohne ihn auskommen. Davon hängt der Wortlaut ab, nicht die Struktur.
2. **Einstellung setzen** — `ConfigureHttpJsonOptions` in `Program.cs`, mit Begründung im Kommentar wie bei den beiden Nachbarzeilen.
3. **`Rumpfwaechter` bauen und registrieren** — neben `Anhangsgrenzenwaechter.Registriere(app)`, disjunkter Filter.
4. **Zeiten-Sonderbefund** — `Ende` nullbar, Befund im Dienst, Kompensation nennt `…/zeiten/laufend`.
5. **Validatoren null-sicher machen** — sechs Stellen, je in der Hausform `IsNullOrWhiteSpace` vor `Normalisiert`.
6. **Tests nachziehen** — Reproduktionstests, `FehlervertragTests`-Fallgruppe, die beiden Probe-Tests umschreiben, die beiden Zeiten-Tests schärfen.

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** `Program.cs` (JSON-Optionen und die Registrierung des Wächters) — sonst keine. Es kommt keine Route, kein Menüpunkt und keine Migration hinzu.

- `Rumpfwaechter` (Host-Adapter, Muster `Anhangsgrenzenwaechter`) — fängt die gescheiterte Rumpf-Bindung und schreibt eine `Zurueckweisung` mit 400.
  - `void Registriere(WebApplication anwendung)`
- `Rumpfbefund` (Operation, pure Logik, Muster `AnhangValidator.RumpfUeberDerGrenze`) — formuliert Code, Meldung und Kompensation für einen Rumpf, dem ein Pflichtfeld fehlt.
  - `Fehlerbefund PflichtfeldFehlt(string feld, string route)`
- `ZeiteintragNachtragenAnfrage` (DTO, immutable) — `Ende` wird `DateTimeOffset?`; die Pflichtigkeit wandert vom Typ in den Dienst.
- `ZeitenService` (Integration) — `TrageNach` weist einen Nachtrag ohne Ende mit eigenem Befund zurück, bevor `Zeitspanne.Pruefe` läuft.

### Änderungen an bestehenden Klassen

- `Program.cs` — eine `ConfigureHttpJsonOptions`-Zeile, eine `Rumpfwaechter.Registriere`-Zeile.
- `TeilaufgabenValidator`, `KommentarValidator`, `DateiverweisValidator`, `EtikettenValidator`, `KartenklassenValidator`, `SpaltenreihenfolgeValidator` — je eine Null-Prüfung vor der Normalisierung bzw. vor dem Durchlauf, die den vorhandenen `…-leer`-Befund liefert.
- `ZeitenService.TrageNach` — der neue Befundzweig.

## Tests

**Kandidaten für Unit Tests (pure Logik nach IOSP):** `Rumpfbefund` (Wortlaut, Code, Kompensation), die sechs Validatoren mit `null` als Eingabe, `Zeitspanne` unverändert.
**Integration:** beide Reproduktionstests, die Fallgruppe in `FehlervertragTests`, die drei Probe-Tests.
**E2E:** keine. Die Oberfläche erzeugt keinen unvollständigen Rumpf; ein E2E-Test dafür wäre nicht auslösbar.

## Abhängigkeiten

- Abhängig von: nichts. Die Behebung steht für sich und braucht keinen offenen Slice.
- Berührt (ohne Abhängigkeit): [`R00007`](R00007-karte-verschieben.md) — dort steht die Zusage „jede Fehlerantwort trägt Code, Meldung und Kompensation", die hier für unvollständige Rümpfe erstmals eingelöst wird; [`R00018`](R00018-karte-gliedern.md) und [`R00029`](R00029-zeiteintrag-nachtragen-und-aendern.md) als Ursprungsläufe.
- Blockiert: nichts. Je später sie kommt, desto mehr Routen tragen die Lücke.

## Offene Fragen

- **Nennt die Meldung das fehlende Feld beim Namen?** — **offen und entscheidend für den Wortlaut.** `JsonException` trägt den Parameternamen nur in der `Message`, nicht in einer eigenen Eigenschaft. Ist er nicht zuverlässig herauszulesen, nennt die Meldung stattdessen die Route und den Umstand („der Rumpf trägt nicht alle Pflichtfelder"), und die Kompensation verweist auf die Beschreibung der Route. Die **Struktur** des Befunds hängt nicht daran, der Text schon. Klärt Probe 1.3.
- **Ein Code für alle oder einer je Feld?** — **im stillen Lauf entschieden: ein Code** (`pflichtfeld-fehlt`), weil der Wächter die Fachlichkeit der Route nicht kennt und ein feldabhängiger Code eine Tabelle im Wirt bräuchte. Vor der Umsetzung zu bestätigen.
- **Soll `POST …/zeiten/laufend` einen `beginn` künftig ausdrücklich zurückweisen?** — **offen, hier nicht entschieden.** `ZeitmessungStartenAnfrage` hat bewusst kein Feld für den Beginn (`:5-7`); ein mitgeschickter `beginn` wird heute stillschweigend ignoriert. Das ist eine andere Frage als diese — sie betrifft **überzählige**, nicht fehlende Felder — und gehört in eine eigene Anforderung.
- **Gilt dieselbe Lücke für den Live-Kanal?** — **geprüft und verneint:** `GET /api/ereignisse` nimmt keinen Rumpf entgegen und steht in `RoutenOhneFehlerantwort`.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser sind zwei Antworten, die ein Agent nicht benutzen kann: ein nackter Serverfehler und ein Wegweiser, der in die falsche Richtung zeigt. Das Zielbild ist die Zusage der Vision, dass ein Agent die API so benutzen kann wie ein Mensch die Oberfläche — und die Oberfläche lässt kein Pflichtfeld weg, während die API es zulassen muss und deshalb dazu etwas sagen können muss. Die Kausalkette: **wenn** die Deserialisierung ein fehlendes Pflichtfeld nicht mehr mit `default` füllt, sondern scheitert (X), **dann** rechnet keine Fachlogik mehr mit einem Wert weiter, den niemand geschickt hat — es gibt kein `null` mehr im `Trim()` und kein `0001-01-01` mehr in einer Meldung (Y), **und dann** trägt jede Fehlerantwort wieder Grund, Werte und Kompensationsaktion, weil der Wächter den Abbruch in genau diese Form bringt (Z). Der Hebel liegt an der Serialisierung und nicht in den Validatoren: dahinter, im Validator, ließe sich das fehlende Feld nur noch **erraten** — der Vorgabewert ist zu diesem Zeitpunkt schon eingesetzt und von einem echt geschickten Wert nicht mehr unterscheidbar. Ein `abgehakt: false` und ein weggelassenes `abgehakt` sehen im Handler identisch aus; nur die Serialisierung kennt den Unterschied. Und nicht davor, im Klienten: ein Agent, der ein Feld vergisst, ist genau der Fall, den die API beantworten muss.

## Missing-Docs

- **`RespectRequiredConstructorParameters` bei positionalen `record`-Typen:** dass die Pflichtigkeit eines Konstruktorparameters ohne diese Einstellung wirkungslos ist, war im Repository nirgends festgehalten — im Gegenteil, `ZeiteintragNachtragenAnfrage.cs:7` behauptet das Gegenteil. Der Befund gehört nach `Dokumentation/Bibliotheken/`, falls er sich online nicht belegen lässt.
- **Welche Ausnahme eine gescheiterte Rumpf-Bindung bei `ThrowOnBadRequest = true` in der Middleware erzeugt** (Typ, `InnerException`, mitgeführter Statuscode) — unbelegt; entscheidet über den Filter des `Rumpfwaechter`.
- **Ob der Name des fehlenden Feldes strukturiert aus `JsonException` zu holen ist** — unbelegt; entscheidet über den Wortlaut der Meldung.
- **Ob die Antwort in der Middleware noch geschrieben werden kann**, nachdem die Rumpf-Bindung abgebrochen ist — für die Anhangsgrenze belegt (der Wächter tut es), für die JSON-Bindung angenommen, nicht gemessen.

## Notizen

- **Priorität: Hoch.** Kein Datenverlust, aber eine gebrochene Zusage an genau die Nutzergruppe, für die dieses Projekt gebaut wird. Symptom 1 ist ein 500 ohne Befund, Symptom 2 führt einen Agenten aktiv in die Irre — er wiederholt den Aufruf auf derselben Route, weil die Kompensation es ihm sagt, und scheitert wieder.
- **Betroffene Nutzer/Systeme:** ausschließlich Aufrufer der API — KI-Agenten und Skripte. **Menschen an der Oberfläche sind nicht betroffen**: die Blazor-Klienten serialisieren typisierte `record`-Instanzen, in denen kein Feld fehlen kann.
- **Workaround bis zur Behebung:** jeden Rumpf vollständig schicken, auch dort, wo ein Wert „ohnehin die Vorgabe" ist. Für den Zeiten-Nachtrag gilt zusätzlich: eine Meldung „Ende 0001-01-01" bedeutet **nicht**, dass ein Ende falsch war, sondern dass keines mitkam — der richtige nächste Aufruf ist `POST /api/karten/{karteId}/zeiten/laufend`.

### Verworfene Alternativen

Vollständig mit Achsen und Begründung unter „Lösungsvorschlag → Alternative Ansätze": die Einstellung allein · nur Null-Prüfungen · alle Pflichtfelder nullable · Endpunktfilter je Route · Validierungsbibliothek · `RespectNullableAnnotations`.

Dazu zwei Formen, die gar nicht erst in die Tabelle kamen:

| Option | Warum verworfen |
|---|---|
| **Nur die beiden belegten Routen reparieren** (Teilaufgabe und Zeiten-Nachtrag) | Die Ursache liegt in der Konfiguration des Wirts und wirkt auf jede der 22 Anfragegestalten. Zwei Routen zu heilen ließe zwölf gleichartige Löcher stehen — darunter die stillen, die heute mit **200** antworten. |
| **`ThrowOnBadRequest` wieder abschalten**, damit die Bindung still 400 liefert | Nimmt dem `Anhangsgrenzenwaechter` seine Grundlage und damit `R00020` seine Zusage; und eine 400 ohne Rumpf ist genau das, was `R00007` ausschließt. |

### Bewusst out of scope

- **Überzählige Felder im Rumpf** (`beginn` an `…/zeiten/laufend`) — andere Frage, eigene Anforderung; siehe „Offene Fragen".
- **Die Antwort auf unbekannte Aufzählungstexte und Formatfehler inhaltlich zu verbessern.** Der `Rumpfwaechter` gibt ihnen einen Befund, weil er dieselbe Ausnahme fängt; einen **je Fall zugeschnittenen** Wortlaut bekommen sie hier nicht.
- **Eine maschinenlesbare Beschreibung der Pflichtfelder je Route** (OpenAPI-Schema mit `required`). Wäre der nächste Schritt für Agenten, ist aber eine eigene Anforderung über die Dokumentation der API, nicht über ihr Fehlerverhalten.
- **Jede Änderung an der Oberfläche.** Sie ist an diesem Fehler unbeteiligt.

### Angenommen im stillen Lauf

- **Der `Rumpfwaechter` steht neben dem `Anhangsgrenzenwaechter` und ersetzt ihn nicht** — zwei Abbruchgründe, zwei Meldungen, zwei disjunkte Filter.
- **Ein Code für alle fehlenden Pflichtfelder** (`pflichtfeld-fehlt`) statt eines je Feld — siehe „Offene Fragen".
- **`ZeiteintragNachtragenAnfrage.Ende` wird nullbar**, damit die Fachlichkeit den Sonderbefund geben kann; alle anderen DTOs behalten ihre Pflichtangabe im Typ.
- **`RespectNullableAnnotations` wird nicht eingeschaltet**; der `null`-Fall wird in den Validatoren beantwortet — die Einstellung wirkte auch beim Schreiben der Antworten und wäre ein Risiko über den ganzen Antwortbestand.
- **`AbfrageparameterProbeTests` bleibt unangetastet** — die Messung gilt für Abfrageparameter und bleibt wahr.
