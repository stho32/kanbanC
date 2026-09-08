---
id: R00029
status: In Arbeit
datum: 2026-09-06
---

# R00029: Zeiteintrag nachtragen und ändern

## Beschreibung

Ein Zeiteintrag entsteht auch **ohne Timer**: am Fuß der Einträgeliste trägt ein Formular Kontributor, Tag, „von" und „bis" ein und legt daraus einen abgeschlossenen Eintrag an. Dasselbe Formular öffnet sich — ohne den Tag — **in** einer bestehenden Zeile und korrigiert sie; von dort aus lässt sich der Eintrag auch löschen. Über die API tun das `POST /api/karten/{karteId}/zeiten`, `PUT …/zeiten/{zeiteintragId}` und `DELETE …/zeiten/{zeiteintragId}`.

Zahlt ein auf: [Vision](R00000-vision.md) — „Zeiterfassung, die zum Arbeiten passt" und „Auswertungen aus vollständigen Daten". `R00026` hat den Timer startbar, `R00027` stoppbar, `R00028` sichtbar gemacht; dieser Slice ist der erste, der eine erfasste Zeit **korrigierbar** macht — und der erste, der Zeit erfasst, die nie gemessen wurde.

**Dieser Slice löst drei ausdrücklich an ihn adressierte Schulden ein.** Sie stehen wörtlich in den Vorgängern und werden hier nicht neu erfunden:

1. **„Das Ende liegt vor dem Beginn" wird hier zurückgewiesen.** `R00027` hat es hierher verwiesen: „Die gezeichnete Zurückweisung […] gehört `I0025`: dort gibt der Mensch beide Zeitpunkte selbst ein und **kann** sie korrigieren. Dieselbe Invariante, zwei Antworten — weil nur dort eine Kompensation existiert."
2. **Überlappende Zeiteinträge sind erlaubt und werden nicht geprüft.** Die Frage steht seit `R00026` offen und wurde von `R00026` **und** `R00027` mit der Adresse `I0025` weitergereicht. Sie wird hier entschieden — siehe „Überlappungen".
3. **Löschen gehört hinein.** Das Artboard führt „Eintrag löschen" als „Bedienelement ohne Knoten, als Befund und nicht als Zutat" (`D0006.dc.html:698`) und verlangt die Entscheidung ausdrücklich in `I0025`.

**Zwei Vorbehalte werden mit diesem Slice fällig und hier ausdrücklich NICHT mitentschieden** — sie stehen unter „Offene Fragen" mit Adresse: die Idempotenz des zweiten Stopps aus `R00027` und die fehlende Überlappungswarnung aus `R00028` haben beide dieselbe Begründung getragen („keine Kompensationsaktion, solange `I0025` rot ist"), und genau diese Begründung trägt ab hier nicht mehr. `I0024` und `I0026` sind grün; beides gehört in eigene Anforderungen.

## Geschäftlicher Nutzen

Ein Timer, den jemand zu spät startet, zu spät stoppt oder ganz vergisst, erzeugt bis heute **unkorrigierbare** Zahlen. Genau diese Zahlen sind das Futter für Soll-Ist (`I0033`), Burndown und die Schätz-Rückkopplung der Vision („dieselben Ist-Zeiten als Futter für die KI, die daraus künftige Aufgaben besser einschätzt") — eine Auswertung über Daten, die niemand geraderücken kann, ist eine Auswertung, der niemand traut.

Dazu die Lage, die die Vision selbst erzeugt: ein Agent, der ohne Bildschirm arbeitet, hat keinen Timer gestartet und trägt seine Zeit hinterher nach. Ohne `POST …/zeiten` bleibt ihm nur, einen Timer zu starten und sofort zu stoppen — und die gemessene Dauer wäre erfunden.

## Funktionale Anforderungen

- Ein Zeiteintrag mit Kontributor, Beginn und Ende lässt sich ohne vorherige Messung anlegen (`POST /api/karten/{karteId}/zeiten`, 201).
- Ein bestehender Zeiteintrag lässt sich in Kontributor, Beginn und Ende ändern (`PUT /api/karten/{karteId}/zeiten/{zeiteintragId}`, 200).
- Ein bestehender Zeiteintrag lässt sich löschen (`DELETE /api/karten/{karteId}/zeiten/{zeiteintragId}`, 200 mit dem `Kartendetail`).
- Ein Ende vor dem Beginn wird mit einem Befund zurückgewiesen, der **beide** Werte nennt; eine Dauer von null bleibt erlaubt.
- Ein Zeitpunkt in der Zukunft wird zurückgewiesen; Toleranz eine Minute.
- Ein laufender Eintrag ist änderbar und darf dabei beendet oder wieder laufend gemacht werden.
- Für einen stillgelegten Kontributor wird nicht nachgetragen; beim Ändern greift die Stilllegung nur bei Kontributorwechsel.
- Überlappende Zeiteinträge bleiben erlaubt und werden nicht geprüft.
- Die Kartenseite trägt das Nachtragsformular am Fuß der Einträgeliste und dasselbe Formular in der Zeile, die geändert wird.

## Nicht-funktionale Anforderungen

- **Fehlerantworten für Agenten:** jeder Befund nennt den Grund **mit den beteiligten Werten** und eine ausführbare Kompensationsaktion — auch bei 404 (Projektregel, Memory `api-fehler-fuer-agenten`).
- **Kernregel:** `KanbanC.Blazor` bekommt keine Referenz auf `KanbanC.BL`. Jede Fähigkeit dieses Slice existiert zuerst als Endpunkt; die Oberfläche ruft ihn.
- **Sicherheit:** unverändert Full Trust im LAN ohne Anmeldung (Leitplanke der Vision). Niemand ist an den eigenen Kontributor gebunden.
- **Keine Schemaänderung:** Tabelle `Zeiteintrag` und der partielle Index stehen seit Migration `018`; dieser Slice schreibt in bestehende Spalten.
- **Benutzerfreundlichkeit:** kein Dialogfenster — die Einträgeliste bleibt beim Nachtragen und Ändern sichtbar, damit man sieht, wogegen man korrigiert.
- **Zeitzonen:** die API nimmt und liefert volle Zeitstempel (`DateTimeOffset`, ISO-8601); die Umrechnung Ortszeit → UTC geschieht in der Oberflächenschicht an **einer** Stelle.

## Akzeptanzkriterien

### Ein Zeiteintrag lässt sich von Hand erfassen

- [x] `POST /api/karten/{karteId}/zeiten` mit `{ kontributor, beginn, ende }` antwortet **201** und liefert den angelegten `Zeiteintrag` mit `zeiteintragId`, `karte`, ganzem `kontributor`, `beginn` und **gesetztem** `ende`.
- [x] Der Eintrag erscheint danach in `GET /api/karten/{karteId}` unter `zeiteintraege` und überlebt einen Neustart der WebApi.
- [x] `ende` ist beim Nachtragen **pflichtig**: eine Anfrage ohne `ende` ist kein Nachtrag, sondern ein Start (`POST …/zeiten/laufend`, `R00026`) und wird nicht als Nachtrag angenommen.
- [x] Rechenbeispiel: `beginn` 2026-09-05T12:00:00Z, `ende` 2026-09-05T13:30:00Z ergibt einen Eintrag, dessen Dauer in der Oberfläche als `1:30` erscheint.
- [x] Ein Nachtrag lässt den partiellen Index nie anschlagen — er trägt immer ein Ende. Ein für dasselbe Paar (Karte, Kontributor) **laufender** Eintrag steht einem Nachtrag deshalb nicht im Weg und bleibt unverändert laufen.

### Ein Zeiteintrag lässt sich korrigieren

- [x] `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}` mit `{ kontributor, beginn, ende }` antwortet **200** und liefert den geänderten Eintrag; `zeiteintragId` und `karte` bleiben, was sie waren.
- [x] Alle drei Felder sind änderbar: `kontributor`, `beginn` und `ende`. Die **Karte** ist nicht änderbar.
- [x] Rechenbeispiel: ein Eintrag 17:40 – 18:25 (`0:45`) wird auf 17:40 – 18:40 geändert und liest sich danach mit `1:00`; die Summe seines Kontributors wächst um genau `0:15`.
- [x] `ende` ist beim Ändern **nullbar**: `ende: null` macht den Eintrag wieder laufend.

### Ein laufender Eintrag ist änderbar

- [x] Ein Eintrag ohne `ende` lässt sich ändern; ein mitgegebenes `ende` beendet ihn — dasselbe Ergebnis wie ein Stopp, nur mit selbstgewähltem Zeitpunkt.
- [x] Setzt eine Änderung `ende` auf `null` zurück, während für dasselbe Paar (Karte, Kontributor) **schon ein anderer** Eintrag läuft, antwortet der Aufruf mit **400** und einem lesbaren Befund, der die `zeiteintragId` des anderen nennt; Kompensation: „diesen stoppen oder ändern".
- [x] In diesem Fall kommt **nie** eine `SqliteException` des partiellen Index `UX_Zeiteintrag_Karte_Kontributor_Laufend` durch — geprüft wird vor dem `UPDATE`.
- [x] Läuft für dasselbe Paar **kein** anderer, gelingt der Rückfall auf „laufend" und `GET /api/karten/{karteId}` zeigt den Eintrag danach ohne `ende`.

### Ein Zeiteintrag lässt sich löschen

- [x] `DELETE /api/karten/{karteId}/zeiten/{zeiteintragId}` antwortet **200** und liefert das ganze `Kartendetail` **ohne** die gelöschte Zeile — Hausform `EntferneAnhang` (`KartenService.cs:413`).
- [x] Der Eintrag ist danach auch nach einem Reload fort; die Summe seines Kontributors ist um genau seine Dauer kleiner.
- [x] Ein laufender Eintrag ist ebenso löschbar wie ein abgeschlossener; das Paar (Karte, Kontributor) ist danach wieder frei.

### „Das Ende liegt vor dem Beginn" wird zurückgewiesen

- [x] Liegt `ende` vor `beginn`, antwortet der Aufruf mit **400** und dem Code `zeiteintrag-ende-vor-beginn`; die Meldung nennt **beide** Werte, die Kompensation lautet sinngemäß „den Aufruf mit einem `ende` nach dem `beginn` wiederholen".
- [x] Der Code steht **nicht** in `Nichtgefunden.AlleCodes` (`Nichtgefunden.cs:22`) und bildet deshalb auf 400 ab, wie `kontributor-stillgelegt` und `dateiverweis-doppelt`.
- [x] Die Regel gilt beim Nachtragen **und** beim Ändern.
- [x] Rechenbeispiel: `beginn` 15:30, `ende` 14:00 wird zurückgewiesen; `beginn` 14:00, `ende` 14:00 (**Dauer null**) wird **angenommen** — dieselbe Entscheidung wie in `R00027`.
- [x] Nichts wird geschrieben: nach einer Zurückweisung steht kein neuer Eintrag in `zeiteintraege`, und ein bestehender ist unverändert.

### Ein Zeitpunkt in der Zukunft wird zurückgewiesen

- [x] Liegt `beginn` oder `ende` mehr als **eine Minute** nach der Serveruhr, antwortet der Aufruf mit **400** und dem Code `zeiteintrag-in-der-zukunft`; die Meldung nennt den beanstandeten Wert **und** die Toleranz, die Kompensation lautet sinngemäß „den Zeitpunkt in die Vergangenheit legen".
- [x] Rechenbeispiel: bei Serveruhr 14:00:30 wird `ende` 14:01:00 **angenommen** (30 s voraus, innerhalb der Toleranz) und `ende` 14:02:00 **zurückgewiesen** (90 s voraus).
- [x] Die Uhr wird in die Prüfung **hereingereicht** und nicht in ihr gelesen — Muster `Zeitmessungsende.Fuer`; beide Ränder sind ohne Zeitmanipulation prüfbar.
- [x] Ein `ende: null` (laufend) wird von dieser Regel nicht beanstandet; geprüft wird nur, was gesetzt ist.

### Überlappungen bleiben erlaubt und ungeprüft

- [x] Zwei Zeiteinträge desselben Kontributors dürfen sich zeitlich überlappen — auf derselben Karte und auf verschiedenen. Es entsteht **keine** Zurückweisung und **keine** Warnung.
- [x] Rechenbeispiel: drei nachgetragene Einträge desselben Tages für denselben Kontributor (08:00–18:00, 09:00–19:00, 10:00–20:00) werden alle drei angenommen; seine Summe auf der Karte lautet `30:00`, ungekappt und nicht als „1 Tag 6:00".
- [x] Die **einzige** Schranke bleibt der partielle Index: zwei **laufende** Einträge desselben Paares (Karte, Kontributor) gibt es weiterhin nicht.

### Wer darf — Kontributor und Stilllegung

- [x] Der Kontributor reist beim Nachtragen **und** beim Ändern im Rumpf mit; er wird nie erraten.
- [x] Niemand ist an den eigenen Kontributor gebunden: für einen fremden Kontributor darf nachgetragen und geändert werden.
- [x] Für einen **stillgelegten** Kontributor wird **nicht** nachgetragen: 400 mit dem Code `kontributor-stillgelegt`, Meldung und Kompensationsweg wie beim Start (`Stillgelegt.Zeitmesser`, `Stillgelegt.cs:62`).
- [x] Beim **Ändern** greift die Stilllegung **nur bei Kontributorwechsel**: ein Eintrag eines später Stillgelegten bleibt in Beginn und Ende korrigierbar, solange sein Kontributor derselbe bleibt.
- [x] Rechenbeispiel: Eintrag `#6` gehört dem stillgelegten Stefan. `PUT` mit `kontributor: Stefan` und geändertem `ende` **gelingt**; `PUT` mit `kontributor: Claude → Stefan` (Wechsel **auf** den Stillgelegten) wird **zurückgewiesen**.

### Fehlerantworten für Agenten

- [x] Eine unbekannte `karteId` antwortet **404** mit `karte-unbekannt` und dem Weg zurück über `GET /api/boards` (`Nichtgefunden.Karte(karteId)`).
- [x] Eine `zeiteintragId`, die es **an dieser Karte** nicht gibt, antwortet **404** mit `zeiteintrag-unbekannt` und dem Weg zurück über `GET /api/karten/{karteId}` (`Nichtgefunden.Zeiteintrag`, `Nichtgefunden.cs:99`) — auch dann, wenn es die Nummer an einer anderen Karte gibt.
- [x] Gibt es schon die **Karte** nicht, meldet der Aufruf die Karte und nicht den Zeiteintrag: eine Kompensation, die auf eine 404-Adresse zeigt, wäre nicht ausführbar (Muster `BefundZumFehlendenZeiteintrag`, `ZeitenService.cs:67`).
- [x] Ein unbekannter `kontributor` antwortet **404** mit `kontributor-unbekannt`.
- [x] Jeder Befund dieses Slice nennt Grund **mit Werten** und Kompensationsaktion; kein Befund verweist auf eine Adresse, die es nicht gibt.

### In der Oberfläche

- [x] Am Fuß der Einträgeliste steht eine aufklappbare Zeile mit Kontributor, Tag, „von", „bis" und der gerechneten Dauer („ergibt 1:30"); „Nachtragen" legt an, „Abbrechen" klappt zu.
- [x] Der Kontributor ist mit der **gewählten Identität** vorbelegt und bleibt änderbar.
- [x] Eine bestehende Zeile öffnet sich **an Ort und Stelle** mit demselben Formular — **ohne** den Tag, gefüllt — und trägt „Sichern", „Verwerfen" und „Eintrag löschen".
- [x] Es ist immer **höchstens eine** Zeile zugleich offen; die Liste bleibt dabei sichtbar (kein Dialogfenster).
- [x] Eine Zurückweisung erscheint als **Gründeliste**, die Eingaben bleiben stehen, und „ergibt" zeigt `—`.
- [x] Nach einem gelungenen Nachtrag, einer Änderung oder einer Löschung ziehen Liste, Summen je Kontributor und Ist-Summe (`R00028`) nach; jeder Stand überlebt den Reload.
- [ ] Eine Buchung über Mitternacht wird über **zwei** Einträge erfasst; das Formular trägt dafür keinen zweiten Tag.

### Was dieser Slice ausdrücklich nicht tut

- [x] **Keine Migration** und keine Schemaänderung.
- [x] **Keine Überlappungswarnung** und keine stille Korrektur überlappender Zeiten.
- [x] **Keine Änderung am zweiten Stopp** (`PUT …/zeiten/{id}/ende` bleibt, wie `R00027` es gebaut hat) — die fällig gewordene Frage steht unter „Offene Fragen".
- [x] **Keine Kartenänderung am Zeiteintrag**: ein Eintrag wandert nicht auf eine andere Karte, er wird gelöscht und neu angelegt.
- [ ] **Keine Kopfzeilen-Übersicht** (`I0027`), **keine Live-Nachführung** (`I0028`), **kein Soll-Ist** (`I0033`), **kein Export** (`I0036`).

### Der grüne Bestand bleibt grün

- [x] `POST …/zeiten/laufend` und `PUT …/zeiten/{id}/ende` verhalten sich unverändert (`R00026`, `R00027`); der Routentabellen-Test wächst um die drei neuen Routen, ohne die bestehenden zu verändern.
- [x] Kein Konflikt zwischen `…/zeiten/laufend` und `…/zeiten/{zeiteintragId:long}`: der `long`-Constraint trennt die Nummer vom Wort.
- [x] Zeitenblock, Bilanz, Ist-Summe, Leerzustand und Stoppquadrat aus `R00028` bleiben in Kennung, Wortlaut und Verhalten; die `R00028`-Suite bleibt grün.
- [x] Der Fehlervertrag bleibt geschlossen: die beiden neuen Codes sind in `FehlervertragTests` erfasst und bilden auf 400 ab.

## Betroffene Verzeichnisstruktur

- **Schema:** **unberührt.** Keine neue Datei unter `Source/KanbanC.BL/Persistenz/Migrationen/`; `018-zeiteintrag.sql` trägt Tabelle und Index bereits.
- **Contracts:** `Source/KanbanC.Contracts/Zeiten/ZeiteintragNachtragenAnfrage.cs` (**neu**), `ZeiteintragAendernAnfrage.cs` (**neu**) — beide immutable Records (C08). `Zeiteintrag.cs` **unberührt**: eine Antwortgestalt für alle fünf Interactions.
- **Fachlogik:** `Source/KanbanC.BL/Operations/Zeiten/Zeitspanne.cs` (**neu**, neben `Zeitmessungsende`); `Source/KanbanC.BL/Operations/Fehler/Doppelt.cs` wächst um den Befund zum schon laufenden Eintrag.
- **Datenzugriff:** `Source/KanbanC.BL/Persistenz/Zeiten/ZeitenRepository.cs` und `Source/KanbanC.BL/Interfaces/Zeiten/IZeitenRepository.cs` wachsen um `TrageNach`, `Aendere`, `Loesche`. `Zeitenleser.cs` **unberührt**.
- **Dienst:** `Source/KanbanC.BL/Integrations/Zeiten/ZeitenService.cs` wächst um drei Glieder — IOSP-Integration, `Ergebnis<Zeiteintrag>` bzw. `Ergebnis<Kartendetail>`.
- **API:** `Source/KanbanC.WebApi/Endpunkte/ZeitenEndpunkte.cs` wächst um drei Routen; `Program.cs` **unberührt** (`ZeitenService` ist registriert).
- **Oberfläche — Rechnung:** `Source/KanbanC.Blazor/Services/Zeitpunktform.cs` wächst um die **Gegenrichtung** (Tag + Uhrzeit in Ortszeit → `DateTimeOffset` in UTC) und bekommt **keine** Schwesterklasse.
- **Oberfläche — Klient:** `Source/KanbanC.Blazor/Services/ZeitenApiKlient.cs` wächst um drei Glieder und um eine zweite Lesehilfe für die Antwortgestalt `Kartendetail`.
- **Oberfläche — Ansicht:** `Source/KanbanC.Blazor/Components/Pages/Kartendetail.razor(.css)` — der bestehende `#zeitenabschnitt` bekommt Nachtragsformular, Zeilenformular und die Zurückweisungsanzeige. Die Datei trägt heute **1871 Zeilen** (gemessen); siehe „Der Zeitenblock und seine Größe".
- **Unberührt:** `Karte.razor`, `Spaltenbahnen.razor`, `Board.razor`, `Kopfzeile.razor`, `Zeitbilanz.cs`, `Dauerform.cs`, `wwwroot/gestaltung.css`.
- **Tests:** `Source/KanbanC.BL.Tests/Operations/Zeiten/ZeitspanneTests.cs` (**neu**), `Integrations/Zeiten/ZeitenServiceTests.cs` wächst, `TestHelpers/TestZeitenRepository.cs` wächst; `Source/KanbanC.WebApi.IntegrationTests/Persistenz/Zeiten/ZeitenRepositoryTests.cs` und `Api/ZeitenEndpunkteTests.cs` wachsen; `Source/KanbanC.Blazor.Tests/Services/ZeitenApiKlientTests.cs` und `ZeitpunktformTests.cs` wachsen; `Source/KanbanC.PlaywrightTests/Tests/ZeitenNachtragenE2ETests.cs` (**neu**) mit Locatoren in `PageObjects/KartendetailSeite.cs`.

## Technische Überlegungen

### Dieselbe Invariante, zwei Antworten — und warum das kein Widerspruch ist

`I0024` klemmt ein Ende vor dem Beginn **still** auf den Beginn (`Zeitmessungsende.Fuer`); hier wird derselbe Sachverhalt **zurückgewiesen**. Das ist keine Inkonsistenz, sondern die Anwendung derselben Regel des Projekts auf zwei verschiedene Lagen: **ein Befund braucht eine ausführbare Kompensationsaktion.**

- Beim Stopp erzeugt die **Serveruhr** den Wert. Die Kompensation wäre „stelle die Serveruhr", der Timer liefe bis dahin weiter — also wird geklemmt.
- Hier gibt der Aufrufer **beide** Zeitpunkte selbst ein. Die Kompensation ist „den Aufruf mit einem `ende` nach dem `beginn` wiederholen" — ausführbar, in einem Schritt, ohne Bestandskenntnis.

Eine Klemmung wäre hier die schlechtere Antwort: sie machte aus „von 15:30 bis 14:00" eine Nullmessung und verschwiege dem Aufrufer seinen Tippfehler.

**Dauer null bleibt erlaubt** — dieselbe Entscheidung wie in `R00027`, Kriterium 4: `beginn == ende` ist eine wahre Aussage über eine sehr kurze Arbeit, und eine Zurückweisung hätte als Kompensation nur „nimm eine andere Zahl".

### Die Zukunftsprüfung und ihre Toleranz

Ein Zeiteintrag ist eine Aussage über **geleistete** Arbeit. Ein Nachtrag in der Zukunft vergiftete Summe (`I0026`), Soll-Ist und Burndown (`I0033`) mit Zeit, die niemand gearbeitet hat — und die Kompensation ist ausführbar („den Zeitpunkt in die Vergangenheit legen").

**Die Toleranz von einer Minute ist eine gesetzte Zahl, nicht eine gemessene** — das gehört benannt. Sie ist aus der **Minutengenauigkeit des Formulars** abgeleitet: „von 14:00 bis 15:30" trägt keine Sekunden, also läge ein Nachtrag „bis jetzt" um bis zu 59 Sekunden voraus, sobald die Sekunde im Formularwert auf null steht. Eine Toleranz von null machte den häufigsten legitimen Fall zum Fehler. Eine größere Toleranz (fünf Minuten, eine Stunde) hätte keinen ableitbaren Grund; wer die Zahl später ändern will, findet sie an **einer** Stelle in `Zeitspanne`.

Es ist keine Uhrensynchronisation zwischen Browser und Server unterstellt: gerechnet wird gegen die **Server**uhr, weil die Prüfung im Dienst sitzt und der Wert dort ankommt. Läuft ein Browser im LAN merklich vor, sieht der Mensch eine Zurückweisung mit beiden Werten und kann sie lesen — das ist der Sinn der Meldung.

### Überlappungen — die seit `R00026` offene Frage, hier entschieden

**Entschieden: überlappende Zeiteinträge sind erlaubt und werden nicht geprüft.**

`I0023` erlaubt einem Kontributor mehrere laufende Timer, weil ein Agent an mehreren Karten zugleich rechnet. Zwei solche Timer erzeugen beim Stoppen **zwangsläufig** überlappende abgeschlossene Einträge. Eine Sperre hier erklärte also Bestand für ungültig, den `I0023` und `I0024` legal erzeugen — und sie hätte keine ausführbare Kompensation: „welchen der beiden soll ich kürzen?" müsste der Aufrufer raten.

Die einzige Lage, die zwei Antworten auf **eine** Frage wäre (gleiche Karte, gleicher Kontributor, beide laufend), fängt weiterhin der partielle Index `UX_Zeiteintrag_Karte_Kontributor_Laufend`.

**Der Preis, ausdrücklich benannt:** eine Summe je Kontributor (`I0026`) und der Soll-Ist-Vergleich (`I0033`) können mehr als 24 Stunden je Tag ausweisen. Für einen Agenten ist diese Zahl wahr; für einen Menschen ist sie ein Erfassungsfehler — er bleibt sichtbar, weil jede Zeile ihren Zeitraum einzeln nennt und `Dauerform.AlsText` die Stunden über 24 hinauslaufen lässt (`30:00`). **Keine stille Korrektur.**

### Der Rückfall auf „laufend" braucht einen lesbaren Befund

`ende: null` beim Ändern ist erlaubt — „`Ende is null` heißt läuft" ist die **eine** Regel über fünf Interactions, und ein Änderungsaufruf mit Pflicht-Ende führte eine zweite ein („beim Ändern gibt es kein Laufen").

Der Randfall entsteht dabei zwangsläufig: läuft für dasselbe Paar (Karte, Kontributor) schon ein **anderer** Eintrag, schlägt der partielle Index zu. Ohne vorgelagerte Prüfung käme eine `SqliteException` heraus — eine nackte Datenbankmeldung über einen verletzten Index, genau das, was `Doppelt` (`Doppelt.cs:16-18`) für den Dateiverweis vermeidet. Deshalb wird der laufende Eintrag **vor** dem `UPDATE` gelesen (Muster `LiesLaufendenEintrag`, `ZeitenRepository.cs:122`), und der Befund nennt seine `zeiteintragId` samt der Kompensation „diesen stoppen oder ändern".

Das Schreibschloss fällt wie bei `BeendeZeitmessung` **vor** dem ersten Lesen (`BeginneSchreibtransaktion`): zwei gleichzeitige Änderungen läsen sonst beide und scheiterten beide am Hochstufen.

### Wer darf — und wo die Stilllegung greift

Der Kontributor reist **im Aufruf** mit, beim Nachtragen und beim Ändern. Anders als beim Stopp (`R00027`, der nichts erzeugt und deshalb keinen trägt) **erzeugt** ein Nachtrag einen Eintrag und muss sagen, für wen — wie `ZeitmessungStartenAnfrage`. Beim Ändern trägt er ihn ebenfalls, weil er änderbar ist: wer für einen Agenten nachträgt, tut genau das (Artboard, Zustand 5).

**Die Stilllegung greift asymmetrisch, und das ist Absicht:**

- **Nachtragen:** für einen stillgelegten Kontributor wird nicht nachgetragen — dieselbe Regel und derselbe Befund wie beim Start (`Stillgelegt.Zeitmesser`). Ein Eintrag für jemanden, der nicht mehr mitarbeitet, ist eine neue Behauptung über neue Arbeit.
- **Ändern:** die Prüfung greift **nur bei Kontributorwechsel**. Ein bestehender Eintrag eines später Stillgelegten muss korrigierbar bleiben — sonst friert die Stilllegung falsche Zeiten dauerhaft ein, und die Auswertung erbt sie. Der Dienst muss den **bisherigen** Kontributor dafür kennen, liest ihn also vor der Prüfung.

### Löschen — keine zweite Fähigkeit, sondern der Korrekturweg für ein Feld

Das Fertig-Kriterium sagt „von Hand erfassen und **korrigieren**". Die **Karte** ist das einzige Feld, das das Formular nicht ändert: ein Zeiteintrag hängt an einer Karte, und ihn umzuhängen wäre eine andere Fähigkeit mit eigenen Fragen (welche Karte? auf welchem Board?). Ohne Löschen bliebe ein Nachtrag auf der falschen Karte **dauerhaft** stehen, und „korrigieren" wäre eine halbe Zusage — dieser Slice ist der erste, der Einträge erzeugt, die **nie gemessen** wurden und deshalb komplett falsch sein können.

Die Antwortgestalt ist das ganze `Kartendetail`, Hausform `EntferneAnhang` und `EntferneDateiverweis` (`KartenService.cs:413`, `KartenApiKlient.cs:113`): dieselbe Seite verbraucht es, ein zweiter Abruf wäre ein Fenster, in dem Liste und Summe auseinanderlaufen.

### Zwei Anfragegestalten, ein Validator

`ZeiteintragNachtragenAnfrage(long Kontributor, DateTimeOffset Beginn, DateTimeOffset Ende)` — **pflichtiges** Ende: ein Nachtrag ohne Ende ist kein Nachtrag, sondern ein Start, und den hat `I0023` an einer eigenen Adresse.

`ZeiteintragAendernAnfrage(long Kontributor, DateTimeOffset Beginn, DateTimeOffset? Ende)` — **nullbares** Ende, siehe oben.

Geprüft wird beides von **einer** Operation `Zeitspanne.Pruefe(beginn, ende?, uhr)`: die Invarianten (Ende nach Beginn, nichts in der Zukunft) sind dieselben, und zwei Validatoren wären zwei Orte für eine Regel. Die Pflichtigkeit des Endes ist eine Frage der **Gestalt**, nicht der Prüfung — sie steht im Typ und braucht keinen Befund.

### Die Uhr ist ein Parameter

`Zeitspanne.Pruefe` bekommt „jetzt" hereingereicht und liest keine Uhr im Inneren — Muster `Zeitmessungsende.Fuer` (`Zeitmessungsende.cs:10`) und `Zeitpunktform` (`Zeitpunktform.cs:8-10`). Nur so sind beide Ränder der Toleranz (59 s voraus, 61 s voraus) ohne Zeitmanipulation prüfbar.

### Die Umrechnung wohnt in `Zeitpunktform`

Das Formular trägt „Tag, von, bis" (Nachtrag) und „von, bis" (Änderung) in **Ortszeit**; die API nimmt volle Zeitstempel. Die Umrechnung Ortszeit → UTC ist die **Gegenrichtung** dessen, was `Zeitpunktform` heute tut (UTC → Ortszeit), und gehört daneben — nicht in die Komponente und nicht in eine zweite Klasse. Sonst stünde dieselbe Regel an zwei Stellen und liefe auseinander.

In Blazor Server ist „Ortszeit" die Zeitzone des **Servers**, nicht die des Browsers; das steht schon im Kopfkommentar von `Zeitpunktform` und wird hier zum dritten Mal getragen.

### Der Zeitenblock und seine Größe — eine Frage der Umsetzung

`R00028` hat die Frage ausdrücklich offen gelassen und hierher gezeigt: „`Kartendetail.razor` ist 1726 Zeilen lang, `I0025` fasst denselben Block noch einmal an." Die Datei trägt heute **1871 Zeilen** (gemessen), und dieser Slice bringt dem Zeitenblock erstmals **eigenen Zustand** — offene Zeile, Formularwerte, eigene Zurückweisung.

Damit ist der Moment da, die Frage zu stellen: **zieht der Zeitenblock in eine eigene Komponente?** Sie wird hier **benannt und nicht entschieden** — es ist eine Frage der Lesbarkeit, nicht der Fachlichkeit, sie ändert kein Verhalten und keine Zusage, und sie gehört dorthin, wo der Code entsteht. **Entscheiden darf sie die Umsetzung.**

### Wo die Zurückweisung steht — offen bis zum Code

`B0347` führt es als unklar, und das bleibt es hier: die Kartenseite führt ihre Zurückweisung heute **je Seite** an genau einer Stelle ganz oben (`#kartenblatt-zurueckweisung`, `Kartendetail.razor:65`), das Artboard zeichnet sie **je Formular** über den Eingaben (Rand C). Beides ist vertretbar; welches der beiden sich in der bestehenden Seite ohne Verrenkung baut, zeigt sich am Code. Das Akzeptanzkriterium fordert deshalb nur, **dass** eine lesbare Gründeliste erscheint, die Eingaben stehen bleiben und „ergibt" `—` zeigt — nicht, an welcher Stelle der Seite sie steht.

### Gestaltungsvorgabe

Das Artboard [`Dokumentation/Wireframes/D0006.dc.html`](../Dokumentation/Wireframes/D0006.dc.html) ist die Gestaltungsvorgabe des Dialogs; für diesen Slice gelten daraus **Zustand 5** (`:500`, Nachtragsformular am Fuß und Änderungsformular in der Zeile) und **Rand C** (`:655`, der zurückgewiesene Nachtrag). Betriebsart des Canvas ist `lokal` (`Dokumentation/Wireframes/_wireframes.md:4`); die Dateien im Repository sind der einzige Stand, ein `zurueckholen` entfällt.

Das Artboard ist **Vorgabe für die Gestaltung, keine Vereinbarung**: aus ihm entstehen **keine** Akzeptanzkriterien. Geprüft wird gegen die User Story.

**Bewusste Abweichungen, benannt statt stillschweigend:**

1. **Die gezeichnete laufende Zeile trägt im Bild „2:14"** — im Bestand trägt sie „läuft" ohne Dauer (`R00028`, Entscheidung „der laufende Eintrag zählt nicht"). Diese Anforderung ändert daran nichts.
2. **Die Route der Änderung heißt im Bild `PUT /api/karten/14/zeiten/{zeiteintragId}`** und wird genau so gebaut; `GET /api/zeiten/laufend` aus demselben Kasten gehört `I0027` und entsteht hier nicht.
3. **Der Ort der Zurückweisung** bleibt offen (siehe oben) — das Bild zeigt sie je Formular.

### Ablauf

1. **Nachtragen** (`POST /api/karten/{karteId}/zeiten`)
   - 1.1 `ZeitenEndpunkte.TrageZeiteintragNach(karteId, anfrage)` → `ZeitenService.TrageNach`
   - 1.2 `Zeitspanne.Pruefe(anfrage.Beginn, anfrage.Ende, Jetzt())` → `Pruefbefunde`
     - 1.2.1 Ende vor Beginn → `zeiteintrag-ende-vor-beginn` mit beiden Werten
     - 1.2.2 Beginn oder Ende mehr als eine Minute voraus → `zeiteintrag-in-der-zukunft` mit Wert und Toleranz
   - 1.3 `BefundZumZeitmesser(anfrage.Kontributor)` — unbekannt (404) oder stillgelegt (400), unverändert aus dem Bestand
   - 1.4 `ZeitenRepository.TrageNach(karteId, anfrage)` in **einer** Transaktion → `Zeiteintrag`; `null` heißt „diese Karte gibt es nicht"
   - 1.5 `null` → `Nichtgefunden.Karte(karteId)`; sonst **201** mit dem Eintrag
2. **Ändern** (`PUT /api/karten/{karteId}/zeiten/{zeiteintragId}`)
   - 2.1 `Zeitspanne.Pruefe(anfrage.Beginn, anfrage.Ende, Jetzt())` — dieselbe Operation
   - 2.2 Bisherigen Eintrag lesen; gibt es ihn an dieser Karte nicht → `BefundZumFehlendenZeiteintrag` (Karte vor Zeiteintrag)
   - 2.3 **Nur bei Kontributorwechsel:** `BefundZumZeitmesser(anfrage.Kontributor)`
   - 2.4 **Nur bei `Ende is null`:** läuft für dasselbe Paar schon ein anderer → 400 mit dessen `zeiteintragId` und der Kompensation „diesen stoppen oder ändern"
   - 2.5 `ZeitenRepository.Aendere(...)` unter Schreibschloss → geänderter `Zeiteintrag`, **200**
3. **Löschen** (`DELETE /api/karten/{karteId}/zeiten/{zeiteintragId}`)
   - 3.1 `ZeitenRepository.Loesche(karteId, zeiteintragId)` → `Kartendetail` ohne die Zeile; `null` → 404 wie 2.2
   - 3.2 **200** mit dem `Kartendetail`
4. **Oberfläche — nachtragen**
   - 4.1 Zeile am Fuß aufklappen, Kontributor mit `_urheber` vorbelegen
   - 4.2 „ergibt" laufend rechnen: `Dauerform.AlsText(ende - beginn)`, bei ungültiger Spanne `—`
   - 4.3 „Nachtragen" → `Zeitpunktform` rechnet Tag + Uhrzeit in UTC → `ZeitenApiKlient.TrageNach`
   - 4.4 Erfolg → `_detail` mit dem neuen Eintrag; Zurückweisung → Gründeliste, Eingaben bleiben stehen
5. **Oberfläche — ändern und löschen**
   - 5.1 Klick auf eine Zeile klappt sie auf; eine zuvor offene schließt sich
   - 5.2 „Sichern" → `ZeitenApiKlient.Aendere`, der zurückkommende Eintrag ersetzt den alten (Muster `MitEintrag`, `Kartendetail.razor:1315`)
   - 5.3 „Eintrag löschen" → `ZeitenApiKlient.Loesche`, das zurückkommende `Kartendetail` ersetzt `_detail`
   - 5.4 „Verwerfen" schließt ohne Aufruf

### Grobentwurf (Klassen-Entwurf ohne Implementierungen)

**Wichtige Einstiegsstellen:** drei neue Routen in `ZeitenEndpunkte.Registriere`; das Nachtragsformular am Fuß des bestehenden `#zeitenabschnitt` in `Kartendetail.razor`; das Zeilenformular in der Einträgeliste aus `R00028`. Keine Migration, kein neuer Dienst, keine Registrierung in `Program.cs`.

- `ZeiteintragNachtragenAnfrage` (DTO, immutable) — Kontributor, Beginn und **pflichtiges** Ende eines von Hand erfassten Eintrags.
- `ZeiteintragAendernAnfrage` (DTO, immutable) — Kontributor, Beginn und **nullbares** Ende eines bestehenden Eintrags; `null` heißt „läuft wieder".
- `Zeitspanne` (Operation, pure Logik) — prüft eine Spanne gegen ihre beiden Invarianten. Die Uhr ist ein Parameter.
  - `Pruefbefunde Pruefe(DateTimeOffset beginn, DateTimeOffset? ende, DateTimeOffset jetzt)`
- `Doppelt` (Operation, bestehend) — wächst um den Befund zum schon laufenden Eintrag desselben Paares.
  - `Fehlerbefund LaufenderZeiteintrag(long karteId, long kontributorId, long laufendeZeiteintragId)`
- `IZeitenRepository` / `ZeitenRepository` (Ressourcenzugriff) — wächst um drei Glieder; `null` heißt jeweils „gibt es an dieser Karte nicht".
  - `Zeiteintrag? TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)`
  - `Zeiteintrag? Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)`
  - `Kartendetail? Loesche(long karteId, long zeiteintragId)`
  - `Zeiteintrag? LiesLaufendenEintragAusser(long karteId, long kontributorId, long zeiteintragId)` — für 2.4
- `ZeitenService` (Integration, fängt und meldet) — wächst um drei Glieder in der Reihenfolge „erst die Befunde, deren Kompensation ohne Bestandskenntnis ausführbar ist".
  - `Ergebnis<Zeiteintrag> TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)`
  - `Ergebnis<Zeiteintrag> Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)`
  - `Ergebnis<Kartendetail> Loesche(long karteId, long zeiteintragId)`
- `ZeitenEndpunkte` (Integration) — drei Routen, 201 / 200 / 200; Statuscode aus dem Befundcode über `Zurueckweisungen.AlsFehlerantwort`.
- `Zeitpunktform` (Operation, Oberflächenschicht, bestehend) — wächst um die Gegenrichtung.
  - `DateTimeOffset AusOrtszeit(DateOnly tag, TimeOnly uhrzeit)`
  - `DateTimeOffset? AusOrtszeit(DateOnly tag, TimeOnly? uhrzeit)`
- `ZeitenApiKlient` (Integration, Oberflächenschicht) — wächst um drei Glieder und eine zweite Lesehilfe `AlsKartendetail`.
  - `Task<ApiErgebnis<Zeiteintrag>> TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)`
  - `Task<ApiErgebnis<Zeiteintrag>> Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)`
  - `Task<ApiErgebnis<Kartendetail>> Loesche(long karteId, long zeiteintragId)`

### Änderungen an bestehenden Klassen

- `ZeitenEndpunkte` — drei Routenkonstanten und drei Handler; die bestehenden zwei bleiben unverändert. Der Routentabellen-Test wächst mit.
- `ZeitenService` — drei Glieder; `BefundZumZeitmesser` und `BefundZumFehlendenZeiteintrag` werden wiederverwendet, nicht kopiert.
- `ZeitenRepository` / `IZeitenRepository` — drei Glieder; `LiesLaufendenEintrag` bekommt eine Schwester, die den zu ändernden Eintrag ausnimmt.
- `Doppelt` — ein Befund mehr, mit eigenem Code.
- `Zeitpunktform` — die Gegenrichtung; die bestehenden drei Methoden bleiben unverändert.
- `ZeitenApiKlient` — drei Glieder, zweite Lesehilfe.
- `Kartendetail.razor(.css)` — Nachtragsformular, Zeilenformular, Zurückweisungsanzeige, Zustand „welche Zeile ist offen". `SendeZeitmessungsende` und `StoppeTimer` bleiben unverändert.
- `TestZeitenRepository` — die drei neuen Glieder als Testdoppel.

## Tests

Nach `~/.claude/skills/test-pyramide/SKILL.md` und `~/.claude/skills/test-ehrlichkeit/SKILL.md`; jedes Szenario der User Story wird ein Test.

**Kandidaten für Unit Tests (pure Logik nach IOSP):**
- `Zeitspanne.Pruefe` (`KanbanC.BL.Tests`) — Ende nach Beginn ist befundfrei; Ende **vor** Beginn ergibt `zeiteintrag-ende-vor-beginn`, und die Meldung **enthält beide Werte** (nicht nur „ist ungültig"); `beginn == ende` ist befundfrei (**Dauer null**); 59 s voraus ist befundfrei, 61 s voraus ergibt `zeiteintrag-in-der-zukunft`; `ende: null` beanstandet nur den Beginn; ein Beginn in der Zukunft **und** ein Ende vor dem Beginn ergeben **zwei** Befunde, nicht einen. Die Uhr geht als Parameter herein.
- `Doppelt.LaufenderZeiteintrag` — der Befund nennt die fremde `zeiteintragId` und trägt eine Kompensation, die auf eine existierende Adresse zeigt.
- `Zeitpunktform.AusOrtszeit` (`KanbanC.Blazor.Tests`) — Tag + Uhrzeit ergeben denselben Zeitpunkt, den `AlsZeitraum` wieder als diese Uhrzeit liest (**Hin- und Rückweg an einem Beispiel**); leeres „bis" ergibt `null`; die Umrechnung geht über die Serverzeitzone.
- Der Beweis ist der Wert, nicht der Aufruf: jeder Test vergleicht Befundcodes, Meldungstexte und Zeitpunkte.

**Integration** (`KanbanC.WebApi.IntegrationTests`, gegen echte SQLite-Datei):
- `ZeitenRepositoryTests` — `TrageNach` schreibt Beginn **und** Ende als ISO-Text in UTC und liest sie zurück; ein Nachtrag neben einem **laufenden** Eintrag desselben Paares gelingt (der Index schlägt nicht an); `Aendere` ändert alle drei Felder; `Aendere` mit `ende: null` macht einen abgeschlossenen Eintrag wieder laufend und ist danach über `LiesLaufendenEintrag` auffindbar; `Loesche` entfernt die Zeile und liefert ein `Kartendetail` ohne sie; jedes Glied liefert `null` für einen Eintrag an einer **fremden** Karte.
- `ZeitenEndpunkteTests` — 201 / 200 / 200 auf den drei Routen; 400 mit `zeiteintrag-ende-vor-beginn` samt beider Werte; 400 mit `zeiteintrag-in-der-zukunft`; 400 mit `kontributor-stillgelegt` beim Nachtrag; **200** beim Ändern eines Eintrags eines Stillgelegten ohne Kontributorwechsel; 400 beim Wechsel **auf** einen Stillgelegten; 404 mit `karte-unbekannt` und mit `zeiteintrag-unbekannt`; die Routentabelle enthält die drei neuen Namen; `…/zeiten/laufend` bleibt erreichbar (kein Konflikt mit dem `long`-Constraint).
- **Der Randfall des Rückfalls auf „laufend"** bekommt einen eigenen Integrationstest, weil nur er beweist, dass **kein** `SqliteException`-Text durchkommt: zwei Einträge desselben Paares, einer laufend, der andere abgeschlossen; `PUT` mit `ende: null` auf den abgeschlossenen antwortet **400** mit einem Befund, der die andere `zeiteintragId` nennt.

**Blazor-Tests (unterhalb E2E, `KanbanC.Blazor.Tests`):** `ZeitenApiKlientTests` um die drei Glieder — Erfolgs- und Fehlerpfad je Glied, insbesondere die **zweite Antwortgestalt** (`Kartendetail` beim Löschen) und der Löschaufruf **ohne Rumpf**. Diese Pfade sind über den Browser nicht auslösbar (Projektkonvention, CLAUDE.md).

**E2E** (`ZeitenNachtragenE2ETests`, beide Prozesse auf freien Ports nach Skill `freier-port`): auf einer Karte einen Eintrag nachtragen und ihn mit Zeitraum und Dauer in der Liste sehen; ihn in seiner Zeile ändern und die Summe nachziehen sehen; „von 15:30 bis 14:00" eingeben und **„Das Ende liegt vor dem Beginn"** lesen, während die Eingaben stehen bleiben; den Eintrag löschen und die Zeile verschwinden sehen; jeder Stand überlebt den Reload.

Repositories, `Zeitenleser` und alles mit Datenbank-Abhängigkeit sind **keine** Unit-Test-Kandidaten. Während der Implementierung jede Klasse nochmal prüfen.

## Abhängigkeiten

- Abhängig von: **`R00027`** (Timer stoppen — `I0024`, **grün**) und **`R00028`** (Zeiten einer Karte sehen — `I0026`, **grün**). Das sind genau die zwei Knoten der WBS-Spalte `Braucht` von `I0025`; beide sind erfüllt, der Slice ist **frei**. `I0026` steht dort, weil das Änderungsformular in einer **Zeile der Einträgeliste** aufklappt — und die Liste ist wörtlich das Fertig-Kriterium von `I0026`.
- Setzt außerdem auf: **`R00026`** (`I0023` — Tabelle `Zeiteintrag`, partieller Index, `Zeiteintrag`-DTO, `ZeitenService`, `ZeitenApiKlient`, Zeitenblock), **`R00017`** (Kartendetailseite), **`R00013`** (Identität wählen — die Vorbelegung des Kontributors), **`R00011`**/**`R00014`** (Kontributor mit Stilllegung), **`R00020`**/**`R00021`** (Hausform `EntferneAnhang`/`EntferneDateiverweis` für die Löschantwort), **`R00007`** (Fehlervertrag, `Nichtgefunden`, `FehlervertragTests`), **`R00005`** (`gestaltung.css`). Die Spalte `Braucht` nennt sie nicht — sie führt Vorbedingungen, keine Bauplätze; alle sind grün.
- Blockiert: **`I0031`** („Import wiederholen") berührt Zeiteinträge nicht; unmittelbar hängt an diesem Slice **kein** Knoten über die Spalte `Braucht`. Mittelbar profitieren `I0033` (Soll-Ist) und `I0036` (Zeiten exportieren) davon, dass die Daten korrigierbar sind.
- **Löst ein:** die drei an `I0025` adressierten Schulden aus `R00026`, `R00027` und `R00028` (siehe Beschreibung).
- **Macht fällig:** zwei Vorbehalte aus `R00027` und `R00028` — siehe „Offene Fragen". Sie werden hier **nicht** mitentschieden.

## Umfang

```
Zeiteintrag nachtragen und ändern (I0025) = 12 Bubbles: 8 Standard (9,6h), 4 unklar (3,2–8,5h).
Rest: 9,6h klar + 3,2–8,5h unklar · 3 von 12 Werten belegt, Rest Richtwerte (ungemessen).

Fortschritt: 0 von 12 Bubbles gruen (0 %) · 0 laufen · 12 offen
```

`I0025` ist vollständig bis zur Bubble geplant und trägt seine zwölf Bubbles (`B0337`–`B0348`) **direkt** — **kein Feature dazwischen**. Begründung aus der Zerlegung: Nachtragen und Ändern sind **dasselbe Formular an zwei Orten** und teilen Tabelle, Antwortgestalt, Validator, Komponente und E2E-Weg; getrennt geführt wären es zwei Slices, die nur nacheinander gehen und dasselbe Verhalten teilen — dieselbe Lage wie bei `I0020` bis `I0024`. **Die Requirement-Klammer sitzt deshalb allein an `I0025`.**

| Bubble | Art | Aufwand |
|---|---|---|
| `B0337` Zeitspanne pruefen | Operation + Contracts | 0,4h (belegt) |
| `B0338` Zeiteintrag nachtragen | Provider | 0,4h (belegt) |
| `B0339` Zeiteintrag aendern | Provider | 0,4–1,5h (**unklar**) |
| `B0340` Zeiteintrag loeschen | Provider | 0,4h (belegt) |
| `B0341` Nachtrag, Aenderung und Loeschung verdrahten | Integration | 0,4–1,5h (**unklar**) |
| `B0342` Endpunkte des Nachtragens, Aenderns und Loeschens | Integration | 2h (Richtwert) |
| `B0343` Nachtrag, Aenderung und Loeschung im API-Klienten | Integration | 2h (Richtwert) |
| `B0344` Zeitpunkt aus Tag und Uhrzeit | Operation (Oberfläche) | 0,4h (Richtwert) |
| `B0345` Nachtragsformular am Fuss der Liste | UI | 2h (Richtwert) |
| `B0346` Aenderungsformular in der Zeile | UI | 2h (Richtwert) |
| `B0347` Zurueckweisung im Formular | UI | 0,4–1,5h (**unklar**) |
| `B0348` E2E Zeiteintrag nachtragen, aendern und loeschen | E2E | 2–4h (**unklar**) |

Mit zwölf Bubbles ist das der bislang größte Slice des Dialogs — mehr als `I0023` (elf), `I0026` (neun) und `I0024` (sieben). Der Grund ist die Zahl der **Fähigkeiten**: drei Endpunkte statt einem, zwei Anfragegestalten, zwei Formulare und ein Löschweg. Die vier unklaren Bubbles haben verschiedene Ursachen: `B0339` muss den Rückfall auf „laufend" **vor** dem `UPDATE` gegen den partiellen Index absichern; `B0341` trägt fünf Befundlagen in einer Reihenfolge, in der jede Kompensation ausführbar bleibt, und braucht für die asymmetrische Stilllegungsprüfung den bisherigen Kontributor; `B0347` klärt, ob die Meldung je Formular oder je Seite steht (siehe oben); `B0348` braucht Nachtragen, Ändern, eine Zurückweisung, Löschen und Reload in einem Lauf. Derselbe Vermerk wie bei `I0005` bis `I0026`: die 2h-Richtwerte für Endpunkt-, Klienten- und UI-Bubbles liegen über den gemessenen Werten vergleichbarer Bubbles (`Schaetzungen/_ist-zeiten.md`: 0,0–0,6h); die Konvention wurde nicht abgesenkt, solange niemand entschieden hat, ob die Messungen den Typ tragen. Welche Bubbles es am Ende wirklich werden, entscheidet der Entwickler beim Bauen.

**Übereinstimmung mit der Notiz in der WBS:** die Notiz zu `I0025` trägt keine eigene Zählzeile; sie hält die acht Entscheidungen des Slice fest. Die Zahlen oben sind über die Aufwandsspalte der zwölf Bubbles gezählt.

## Offene Fragen

**Zwei Fragen sind mit diesem Slice fällig geworden. Sie werden hier ausdrücklich NICHT mitentschieden — beide gehören in eine eigene Anforderung an einem grünen Knoten:**

- **Bleibt der zweite Stopp aus `I0024` idempotent, jetzt wo `I0025` steht?** — **fällig, hier nicht entschieden.** `R00027` hat die Idempotenz unter ausdrücklichen Vorbehalt gestellt: „die Begründung […] trägt nur, **solange `I0025` rot ist**. […] Sobald `I0025` steht, gäbe es eine ausführbare Kompensation — und die Frage ist **neu zu stellen**." Mit diesem Slice existiert die Kompensation („das Ende über `PUT /api/karten/{karteId}/zeiten/{zeiteintragId}` setzen"), also **trägt die alte Begründung nicht mehr.** Die Entscheidung selbst bleibt vermutlich richtig — der Wiederholungsfall entsteht durch eine verlorene Antwort, nicht durch einen Irrtum, und 200 mit dem Eintrag sagt schon alles —, aber sie ist ab hier **anders zu begründen**. **`I0024` ist grün**; eine Änderung dort wäre eine Änderung an fertigem Verhalten mit eigenen Tests. **Adresse:** eigene Anforderung über `I0024` (`/anforderung aus-code R00027` zum Nachziehen der Begründung, oder `/anforderung neu`, wenn das Verhalten fallen soll). **Nicht am Menschen geprüft.**
- **Bekommen überlappende Zeiten jetzt eine Warnung?** — **fällig, hier nicht entschieden.** `R00028` hat es genauso vermerkt: eine Warnung „hätte hier **keine Kompensationsaktion**, weil korrigieren erst `I0025` kann. […] **Neu zu stellen, sobald `I0025` steht.**" Mit diesem Slice ist Korrigieren möglich, die Begründung fällt weg. Was dieser Slice entscheidet, ist nur, dass er selbst **nicht sperrt** (siehe „Überlappungen") — das ist eine Aussage über die **Zurückweisung**, nicht über einen **Hinweis** in der Anzeige. Ein Hinweis wäre eine neue Fähigkeit der Oberfläche mit eigenen Fragen (an welcher Zeile? mit welchem Wortlaut? auch für Agenten, deren Überlappung wahr ist?). **`I0026` ist grün.** **Adresse:** eigene Anforderung über `I0026` bzw. `I0033`. **Nicht am Menschen geprüft.**

**Im stillen Lauf entschieden:**

- **Dürfen sich zwei Zeiteinträge desselben Kontributors zeitlich überlappen?** — **entschieden: ja, erlaubt und ungeprüft.** Die seit `R00026` offene und von `R00026` **und** `R00027` hierher adressierte Frage. Begründung unter „Überlappungen": eine Sperre erklärte Bestand für ungültig, den `I0023`/`I0024` legal erzeugen, und hätte keine ausführbare Kompensation. **Die Umkehrung** wäre eine Zurückweisung, bei der der Aufrufer raten muss, welchen der beiden Einträge er kürzen soll. **Der Preis ist benannt:** Summen über 24 Stunden je Tag. **Nicht am Menschen geprüft.**
- **Wird ein Ende vor dem Beginn hier zurückgewiesen statt geklemmt?** — **entschieden: zurückgewiesen**, 400 mit `zeiteintrag-ende-vor-beginn` und beiden Werten. Die von `R00027` hierher verwiesene Schuld. **Dauer null bleibt erlaubt.** **Nicht am Menschen geprüft.**
- **Ist eine Minute die richtige Zukunftstoleranz?** — **entschieden: eine Minute** — **gesetzte Zahl, nicht gemessen.** Abgeleitet aus der Minutengenauigkeit des Formulars; jede andere Zahl wäre gegriffen. **Die Umkehrung** (Toleranz null) machte den häufigsten legitimen Fall — „bis jetzt" — zum Fehler. **Nicht am Menschen geprüft.**
- **Gehört Löschen in diesen Slice?** — **entschieden: ja.** Das Artboard führt den Knopf als Befund ohne Knoten und verlangt die Entscheidung hier. Begründung unter „Löschen": die Karte ist das einzige Feld, das das Formular nicht ändert. **Das Fertig-Kriterium bleibt wörtlich stehen** — Löschen ist der Korrekturweg für dieses eine Feld, keine zweite Fähigkeit. **Nicht am Menschen geprüft.**
- **Ist ein laufender Eintrag änderbar, und darf er wieder laufend werden?** — **entschieden: ja, beides.** „`Ende is null` heißt läuft" bleibt die eine Regel über fünf Interactions. **Die Umkehrung** (Pflicht-Ende beim Ändern) führte eine zweite Regel ein. **Nicht am Menschen geprüft.**
- **Reist der Kontributor beim Ändern mit, und ist jemand an den eigenen gebunden?** — **entschieden: er reist mit, niemand ist gebunden.** Full Trust ohne Anmeldung ist eine Leitplanke der Vision; „wer für einen Agenten nachträgt, tut genau das" (Artboard, Zustand 5). **Nicht am Menschen geprüft.**
- **Greift die Stilllegung beim Ändern?** — **entschieden: nur bei Kontributorwechsel.** Sonst fröre die Stilllegung falsche Zeiten ein. **Die Umkehrung** hieße, dass ein Tippfehler in der Zeit eines ausgeschiedenen Mitarbeiters für immer in der Auswertung steht. **Nicht am Menschen geprüft.**

**Offen und bewusst der Umsetzung überlassen:**

- **Zieht der Zeitenblock aus `Kartendetail.razor` in eine eigene Komponente?** — **offen, hier bewusst nicht entschieden.** Die Wiedervorlage aus `R00028`; die Datei trägt 1871 Zeilen, und dieser Slice bringt dem Block erstmals eigenen Zustand. Es ist eine Frage der Lesbarkeit, keine der Fachlichkeit — sie ändert kein Verhalten und keine Zusage. **Entscheiden darf sie die Umsetzung.**
- **Steht die Zurückweisung je Formular oder je Seite?** — **offen** (`B0347`). Die Kartenseite führt ihre Meldungen heute an einer Stelle ganz oben (`Kartendetail.razor:65`), das Artboard zeichnet sie über dem Formular. Das Akzeptanzkriterium fordert die lesbare Gründeliste, nicht ihren Ort. **Entscheidet sich am Code.**

## Manuelle Vorbereitungstätigkeiten

- Keine. Es entsteht keine Migration; Tabelle und Index stehen seit `018`.

## Manuelle Nachbereitungstätigkeiten

- Keine. Bestehende Zeiteinträge sind nach dem Deployment ohne Zutun korrigierbar — genau dafür ist der Slice da.

## Warum löst diese Anforderung das Problem? (Pflicht)

Der Auslöser ist eine Erfassung ohne Radiergummi: seit `R00026` bis `R00028` lässt sich Zeit messen, stoppen und ansehen — aber **eine falsche Zahl bleibt für immer falsch**, und wer den Timer vergessen hat, kann seine Arbeit gar nicht erst eintragen. Die Kausalkette: **wenn** drei Adressen einen Zeiteintrag anlegen, ändern und löschen, und eine einzige Operation dabei prüft, dass das Ende nach dem Beginn liegt und nichts in der Zukunft steht (X), **dann** entstehen zum ersten Mal Einträge ohne Messung und werden bestehende korrigierbar — und der Aufrufer bekommt bei jedem Fehlversuch eine Meldung mit beiden Werten und einem ausführbaren Weg zurück (Y), **und dann** trägt die Zeitreihe, auf der Soll-Ist (`I0033`), Burndown, Export (`I0036`) und die Schätz-Rückkopplung der Vision aufsetzen — statt einer Reihe, in der jeder Tippfehler zementiert ist (Z). **Der Hebel liegt beim Korrigieren und nicht bei einer strengeren Messung:** ein Timer misst genau, was er misst — falsch wird es durch den Menschen, der ihn zu spät startet, und dagegen hilft keine Regel im Schreibweg, sondern nur ein zweiter Weg zum selben Datensatz. Und er liegt **hinter** `I0026`: korrigieren kann man nur, was man vorher sieht — deshalb klappt das Formular in einer Zeile **dieser** Liste auf, statt eine eigene zu bauen.

## Missing-Docs

- **Zwei Anfragegestalten auf denselben Datensatz mit unterschiedlicher Pflichtigkeit eines Feldes:** dass `Ende` einmal `DateTimeOffset` und einmal `DateTimeOffset?` ist, während beide auf dieselbe Spalte schreiben, hat im Repository kein Vorbild — die bisherigen Anfrage-DTOs sind je Aufruf vollständig gleichgestaltet.
- **`UPDATE` einer Spalte auf `NULL` gegen einen partiellen `UNIQUE`-Index:** dass Microsoft.Data.Sqlite den Index beim Setzen von `Ende = NULL` **innerhalb** derselben Transaktion prüft (und nicht erst beim Commit), ist im Repository unbelegt. `R00027` hat die Gegenrichtung (Freigabe nach dem Setzen) offen gelassen; das ist die Hinrichtung.
- **Rundung und Zeitzone bei `DateOnly` + `TimeOnly` → `DateTimeOffset` in Blazor Server:** die Umrechnung Ortszeit → UTC über `TimeZoneInfo.Local` an der Sommerzeitgrenze (eine Uhrzeit, die es an einem Tag zweimal oder gar nicht gibt) ist im Projekt nirgends vorgemacht — bisher lief die Umrechnung nur in die andere Richtung.

## Notizen

### Warum der Nachtrag nicht an `POST …/zeiten/laufend` andockt

Ein Feld `ende` an `ZeitmessungStartenAnfrage` machte aus einer Adresse zwei Fähigkeiten und aus der Idempotenz des zweiten Starts (`R00026`) eine Fallunterscheidung: der Start gibt einen laufenden Eintrag unverändert zurück, ein Nachtrag müsste dagegen jedes Mal einen neuen anlegen. `ZeitenEndpunkte` hält `POST …/zeiten` seit `B0324` ausdrücklich dafür frei (`ZeitenEndpunkte.cs:11-12`): „Start und Nachtrag sind zwei Fragen und bekommen zwei Adressen."

### Verworfene Alternativen

- **Ende beim Ändern pflichtig machen** — einfacher zu prüfen, aber es führte eine zweite Regel neben „`Ende is null` heißt läuft" ein und machte einen laufenden Eintrag unkorrigierbar, ohne ihn vorher zu stoppen.
- **Ein Ende vor dem Beginn hier ebenfalls klemmen** (wie `I0024`) — machte aus einem Tippfehler eine Nullmessung und verschwiege ihn; die Kompensation ist hier ausführbar, also gehört ein Befund hin.
- **Überlappungen zurückweisen** — erklärte Bestand für ungültig, den `I0023`/`I0024` legal erzeugen; die Kompensation wäre nicht ausführbar.
- **Überlappungen still kürzen** — machte aus einem sichtbaren Erfassungsfehler eine unsichtbare Lüge und wäre für Agenten schlicht falsch.
- **Zukunft erlauben** — vergiftete Summe, Soll-Ist und Burndown mit Zeit, die niemand gearbeitet hat; die Kompensation ist trivial ausführbar.
- **Zukunft ohne Toleranz zurückweisen** — machte „bis jetzt" bei minutengenauer Eingabe bis zu 59 s lang zum Fehler.
- **Löschen weglassen** (`I0025` nennt es nicht) — ein Nachtrag auf der falschen Karte bliebe dauerhaft stehen; „korrigieren" im Fertig-Kriterium wäre eine halbe Zusage.
- **Die Karte am Zeiteintrag änderbar machen statt zu löschen** — eine andere Fähigkeit mit eigenen Fragen (Board? Berechtigung? Summenwanderung), und der partielle Index bekäme einen Fall mehr.
- **Löschen mit 204 ohne Rumpf** — die Seite bräuchte danach einen zweiten Abruf; Hausform ist `EntferneAnhang` mit dem ganzen `Kartendetail`.
- **Ein eigener Dienst `ZeiteintragskorrekturService`** — Zeiten sind **ein** Unterthema; `ZeitenService` trägt heute zwei Glieder und wächst auf fünf.
- **Zwei Validatoren für die zwei Anfragegestalten** — dieselbe Regel an zwei Stellen; die Pflichtigkeit des Endes steht im Typ und braucht keinen Befund.
- **Die Umrechnung Ortszeit → UTC in der Komponente** — eine zweite Stelle für dieselbe Regel; sie gehört neben ihre Gegenrichtung in `Zeitpunktform`.
- **Ein Formular als Dialogfenster** — die Liste verschwände, und man korrigierte gegen etwas, das man nicht mehr sieht.
- **Beim Ändern gar nicht auf Stilllegung prüfen** — dann trüge ein Nachtrag über den Umweg „anlegen für mich, dann umhängen" die Regel aus.
- **Die Idempotenz des zweiten Stopps gleich hier mitentscheiden** — `I0024` ist grün, die Frage betrifft fremdes fertiges Verhalten mit eigenen Tests, und eine Anforderung, die zwei Slices ändert, ist nicht mehr gegen einen prüfbar.

### Bewusst out of scope

- **Die Zurückweisung des zweiten Stopps** — `I0024`, eigene Anforderung (siehe „Offene Fragen").
- **Eine Warnung bei überlappenden Zeiten** — `I0026`/`I0033`, eigene Anforderung (siehe „Offene Fragen").
- **Die Karte eines Zeiteintrags ändern** — kein Knoten; der Weg ist löschen und neu anlegen.
- **Laufende Timer in der Kopfzeile** — `I0027`.
- **Live-Nachführung und mitlaufende Dauer** — `I0028`/`D0007`.
- **Das „von 5:00 Soll" aus dem Bild** — `I0033`, ohne Knoten unter `D0006`.
- **Zeiten exportieren** — `I0036`.
- **Zugriffsschutz** — Full Trust im LAN, Leitplanke der Vision.

### Angenommen im stillen Lauf

Dieser Slice ist im Modus „still" geschrieben; die folgenden Annahmen sind entschieden, aber **nicht am Menschen geprüft**. Jede ist oben unter „Offene Fragen" mit ihrer Umkehrung vermerkt.

1. **Drei Adressen:** `POST …/zeiten` (201), `PUT …/zeiten/{zeiteintragId}` (200), `DELETE …/zeiten/{zeiteintragId}` (200 mit `Kartendetail`).
2. **Zwei Anfragegestalten, ein Validator** — Ende pflichtig beim Nachtragen, nullbar beim Ändern.
3. **Ende vor Beginn wird zurückgewiesen** (400, `zeiteintrag-ende-vor-beginn`, beide Werte); **Dauer null bleibt erlaubt**.
4. **Zukunft wird zurückgewiesen, Toleranz eine Minute** — gesetzte Zahl, aus der Minutengenauigkeit des Formulars abgeleitet, **nicht gemessen**.
5. **Überlappungen sind erlaubt und werden nicht geprüft** — mit dem benannten Preis von Summen über 24 h.
6. **Ein laufender Eintrag ist änderbar** und darf wieder laufend werden; der Rückfall bekommt einen lesbaren Befund statt einer `SqliteException`.
7. **Der Kontributor reist beim Nachtragen und beim Ändern mit; niemand ist an den eigenen gebunden.**
8. **Stilllegung sperrt den Nachtrag; beim Ändern greift sie nur bei Kontributorwechsel.**
9. **Löschen gehört in diesen Slice**, als Korrekturweg für das eine Feld, das das Formular nicht trägt.
10. **Die Oberfläche rechnet Tag und Uhrzeit in einen Zeitpunkt; die API nimmt volle Zeitstempel.** Mitternachtsbuchungen werden über zwei Einträge erfasst.
