---
id: S00001
titel: Restaufwand bis die Application KanbanC vollstaendig ist
repo: stho32/kanbanC
status: Abgeschlossen
datum: 2026-08-29
anfrager: Stefan Hoffmann
bezug:
verwandt: []
---

# Restaufwand bis die Application KanbanC vollständig ist

Repository: `stho32/kanbanC` · Datei: `kanbanc-S00001-restaufwand-bis-alles-fertig.md`

## Anfrage

„Wie lange dauert es noch bis alles fertig ist." Gefragt ist der **Restaufwand** der Application
KanbanC — nicht der Aufwand eines einzelnen Features. „Alles fertig" heißt hier: alle Knoten der
WBS-Datei [Dokumentation/Planung/kanbanc.md](../Dokumentation/Planung/kanbanc.md) sind grün, also
alle 9 Dialogs mit ihren 39 Interactions. Zwei davon sind gebaut, 37 stehen offen.

Ein Anlass oder Termin wurde nicht genannt und ist beim Erstellen dieser Schätzung nicht bekannt
gewesen. Eine Budget- oder Terminschranke wurde ebenfalls nicht genannt.

**Für welche Besetzung gerechnet wurde:** Auf Nachfrage wurde **autonome Umsetzung ohne Dialog**
gewählt (`/implementierung autonom`, `/github schleife`). Siehe dazu den ersten Punkt unter
*Annahmen und offene Punkte* — für diese Arbeitsweise liegt in diesem Projekt kein Messwert vor.

## Umsetzungsoptionen

### Genau ein Umsetzungsziel — Begründung

Es gibt **eine** Option, keine Alternativen. Das ist hier kein Formfehler, sondern der Normalfall
bei geklärtem Auftrag: Das Zielbild
[Anforderungen/R00000-vision.md](../Anforderungen/R00000-vision.md) liegt vor, die Zerlegung in
9 Dialogs und 39 Interactions ist entschieden und in der WBS-Datei festgeschrieben, und die
Anfrage lautet ausdrücklich „bis **alles** fertig ist". Eine zweite Option müsste ein anderes Ziel
verfolgen — es gibt aber kein offenes Richtungs-Ziel mehr, über das der Auftraggeber zu entscheiden
hätte. Eine erfundene Alternative wäre eine schlechtere Auskunft, keine bessere.

Was es stattdessen gibt, sind **Umfangsstufen** (nur ein benutzbares Board statt aller neun Dialogs).
Das sind nach den Regeln dieser Familie keine Optionen, sondern Grenzen — sie stehen unter
*Nicht Gegenstand dieser Schätzung*.

### Das Umsetzungsziel im Wortlaut

Fertig-Kriterium des Knotens `A0001` (Application KanbanC) in der WBS-Datei:

> **alle Dialogs gruen**

Geliefert wird damit: die 37 noch offenen Interactions über beide Systemgrenzen — jede Funktion ist
über die Oberfläche **und** über die API erreichbar, wie es die Leitplanke des Zielbilds verlangt.
Konkret die Dialogs `D0001` Boards führen (Rest), `D0002` Kontributoren führen, `D0003` Board
bedienen, `D0004` Karteninhalt pflegen, `D0005` Karten-Klassen, `D0006` Zeiterfassung, `D0007`
Live-Aktualisierung, `D0008` WBS-Import und `D0009` Auswertungen. Jede Interaction wird erst grün,
wenn ein automatisierter Test es beweist (Skill `test-ehrlichkeit`); Unit-, Integrations- und
E2E-Tests sind Teil der Zahl, nicht ein Zusatz dazu.

### Knoten in der WBS und Ergebniszeile

Bezugsknoten: **`A0001`** in [Dokumentation/Planung/kanbanc.md](../Dokumentation/Planung/kanbanc.md).
Der Teilbaum ist dort bis zur Ebene **Interaction** geplant; 2 der 39 Interactions sind bis Bubble
verfeinert (`I0001`, `I0002` — beide grün), die 37 offenen nicht. Eine Zählung nach
`/planung zaehlen A0001` liefert für diese 37 deshalb **keine Zahl**, sondern den Hinweis, dass
verfeinert werden müsste. Die Zahl unten ist folglich eine **Hypothesen-Zählung** nach dem Skill
`work-breakdown-structure`; ihre gedachten Arbeitspakete stehen bewusst **nicht** in der WBS-Datei
und nicht im Dashboard.

> **Hypothese** (Annahme: die 37 offenen Interactions folgen dem Zuschnitt der beiden gebauten —
> Provider, Integration, Endpunkt, API-Klient, Oberfläche, E2E, dazu Fehlerpfade; 7 Bubbles bei
> reinem Anlegen/Ändern/Löschen auf bestehendem Muster, 11 bei mehreren Ansichten oder Aspekten,
> 15 dort, wo neue Technik hinzukommt — Live-Kanal, Dateiablage, Verschieben per Maus,
> Import-Abgleich, Auswertungsdiagramme):
> **ca. 375 Bubbles, gezählt 150–209h, Hypothesen-Bandbreite 75–418h.**

*Diese Zahl ist **hochgerechnet, nicht ausgezählt**: Maßstab sind die beiden fertigen Interactions,
übertragen auf die 37 offenen — so, wie man von zwei gestrichenen Zimmern auf die ganze Wohnung
schließt. Das kann gut hinkommen und kann deutlich danebenliegen. Wirklich genauer wird es erst,
wenn ein Ast vor dem Bauen bis zur Bubble durchgeplant ist
(`/planung verfeinern <knoten> --bis Bubble`); diese Planung kostet selbst Arbeitszeit. Gefragt war
eine grobe Zahl vor dem Bauen — genau die steht hier.*

Die 375 verteilen sich auf 16 kleine Interactions (je 7), 13 mittlere (je 11) und 8 große (je 15).
Als Standardmuster eingeordnet sind 322 Arbeitspakete, als unsicher 53 — die 53 liegen dort, wo
Technik hinzukommt, die es im Projekt noch nicht gibt.

## Nicht Gegenstand dieser Schätzung

Beidseitig sichtbare Grenze — was **nicht** in der Zahl steckt:

- **Kein Teilumfang.** Gerechnet ist der volle Umfang aller 9 Dialogs. Wer nur ein benutzbares Board
  will (etwa `D0001` Boards führen und `D0003` Board bedienen), bekommt eine deutlich kleinere Zahl —
  aber das ist eine eigene Frage mit eigener Zählung, keine Teilmenge dieser hier.
- **Die vier ausdrücklichen Nicht-Ziele des Zielbilds** und die drei geprüft-verworfenen Themen der
  WBS (WIP-Limits je Spalte, Swimlanes, wiederkehrende Karten). Kommt eines davon zurück, wächst der
  Umfang und die Zahl gilt nicht mehr.
- **Betrieb, Auslieferung, Datenmigration, Schulung.** Die Zahl endet, wenn die Tests grün sind.
- **Authentifizierung und Rechtesystem.** Das Full-Trust-Modell im LAN ist Leitplanke des Zielbilds;
  fällt diese Leitplanke, ist das ein neues Vorhaben.
- **Nachträgliche Richtungsentscheidungen innerhalb der Interactions.** Beispiele: welches
  Dateiformat der Board-Export bekommt (`I0038`), ob die Live-Aktualisierung über SignalR oder
  Polling läuft (`I0028`), wie die Auswertungen dargestellt werden (`I0034`, `I0035`). Genau dort
  entstehen später **mehrere Alternativen, die einzeln zu zählen sind** — dafür ist
  `/anforderung brainstorming` der Weg, nicht diese Schätzung.

## Einordnung der Zahl

| Zahl | Wert | Woher |
|---|---|---|
| **Basis** (Hypothese, ungepuffert) | **75–418h** | 375 gedachte Arbeitspakete, gezählt 150–209h, darauf der vorgeschriebene Hypothesen-Faktor 0,5–2 |
| **Puffer** | **209h** | 50 % der Basis-Obergrenze; deckt Wechselkosten, Abstimmung und Nacharbeit — alles, was kein Arbeitspaket ist |
| **Genannt** | **284–627h** | Basis + Puffer. Bei 8h Arbeitszeit am Tag: **36–78 Arbeitstage** |

Die Mindestzahl von 4h greift hier nicht — die Zählung liegt weit darüber.

**Wohin die Stunden gehen.** Von den gezählten 150–209h entfallen rund 129h auf die 322 als
Standardmuster eingeordneten Arbeitspakete (je 0,4h, die Untergrenze des Skills) und 21–80h auf die
53 als unsicher eingeordneten (je 0,4–1,5h). Die Untergrenze von 0,4h ist ein Störungspuffer, kein
Messwert — sie gilt auch dort, wo die tatsächliche Messung darunter lag.

**Gegenprobe am gemessenen Durchsatz.** Am 2026-08-29 entstanden zwischen 09:42 und 16:05 laut
Commit-Historie 26 Arbeitspakete einschließlich zweier vollständiger Anforderungen, zweier Reviews
und zweier Abschlussläufe — 0,25h je Arbeitspaket Wanduhr. Auf 375 Arbeitspakete übertragen wären
das rund **94h**, also deutlich unter der unteren genannten Grenze von 284h. Die genannte Zahl ist
damit nicht zu knapp gerechnet. Sie beruht allerdings auf zwei Interactions in einer noch sehr
kleinen Codebasis und auf der Pair-Arbeitsweise, nicht auf der gewählten autonomen — siehe unten.

## Annahmen und offene Punkte

**Besetzung und Arbeitsweise.** Gerechnet wurde für **autonome Umsetzung ohne Dialog**
(`/implementierung autonom` bzw. die GitHub-Warteschlange `/github schleife`), so vom Anfragenden
gewählt. Eigenschaften der Ausführenden — Erfahrung, Tempo, Verfügbarkeit — sind nicht bewertet und
nicht in die Zahl eingerechnet; der Ersteller kennt sie nicht. **Bei anderer Besetzung oder anderer
Arbeitsweise gilt diese Zahl nicht, sondern muss spezifischer neu gerechnet werden.**

**Die Stundenwerte stammen aus der Pair-Arbeitsweise, nicht aus der gewählten autonomen.**
`Schaetzungen/_ist-zeiten.md` enthält 6 bestätigte Messungen (0,4h / 0,1h / 0,1h / 1,5h / 0,1h /
0,1h), alle vom 2026-08-29, alle Typ Standard, alle im Modus *moderat* — also im Dialog. Ob autonome
Läufe schneller oder langsamer sind als diese gemessenen, ist dem Ersteller **nicht bekannt**; es
liegt kein Messwert dieses Projekts dafür vor. Die Übertragung der Pair-Werte auf die autonome
Arbeitsweise ist eine **Vermutung** (nicht belegt). Sie ist der zweite Unsicherheitsfaktor über der
ohnehin schon hypothetischen Zerlegung — beide zusammen sind der Grund für die weite Bandbreite.

**Weniger als drei einschlägige Messwerte.** Für die als *unsicher* eingeordneten Arbeitspakete gibt
es genau **eine** Messung über der Untergrenze (`B0003 Board mit Spalten speichern`, 1,5h); sie
liefert die Obergrenze 1,5h. Eine einzelne Messung trägt eine Bandbreite nur schwach.

**Die Bubble-Zahl je Interaction ist gedacht, nicht geplant.** 375 ist die Summe der Hypothese aus
dem Abschnitt *Umsetzungsoptionen*. Belegt sind nur die beiden gebauten Interactions: `I0001` mit
15 Arbeitspaketen (einschließlich des einmaligen Fundaments — Datenbankverbindung, Schema,
Endpunkt-Muster, E2E-Infrastruktur) und `I0002` mit 11. Die Einordnung der 37 offenen in klein (7),
mittel (11) und groß (15) ist eine **Vermutung** anhand ihrer Fertig-Kriterien.

**Der Nenner kann wachsen.** Löst sich beim Bauen ein Arbeitspaket in drei auf, steigt die Zahl. Das
ist der Normalfall in Flow-Design und kein Rechenfehler — die WBS-Regel sagt ausdrücklich, dass der
Nenner wächst.

**Nicht gemessen, nicht eingerechnet.** Die Ist-Zeiten-Datei enthält nur 6 der 26 gebauten
Arbeitspakete. Für die übrigen 20 wurde nichts nachgetragen; ihre Dauer ist aus der Commit-Historie
abgeleitet und nur für die Gegenprobe verwendet, nicht für die Zahl.

### Woran die Bandbreite hängt

Weil die Zahl aus einer Hypothese stammt und nicht aus geplanten Arbeitspaketen, hängt die
Bandbreite an genau zwei Sätzen — nicht an einzelnen Paketen:

- **Die Annahme selbst:** dass die 37 offenen Interactions dem Zuschnitt der beiden gebauten folgen
  (7 / 11 / 15 Arbeitspakete je nach Größe). *Hochgerechnet, nicht ausgezählt — kein einziges dieser
  375 Arbeitspakete ist benannt. Das kann gut hinkommen und kann deutlich danebenliegen. Wirklich
  genauer wird es erst, wenn ein Ast vor dem Bauen bis zur Bubble durchgeplant ist
  (`/planung verfeinern <knoten> --bis Bubble`); diese Planung kostet selbst Arbeitszeit. Gefragt war
  eine grobe Zahl vor dem Bauen — genau die steht hier.*
- **Die Besetzung:** gerechnet für autonome Umsetzung ohne Dialog, mit Stundenwerten aus der
  Pair-Arbeitsweise. Eigenschaften der Ausführenden sind nicht bewertet; bei anderer Besetzung gilt
  die Zahl nicht, sondern muss spezifischer neu gerechnet werden.

**Wie die Spanne kleiner wird:** `/planung verfeinern <dialog> --bis Bubble` für den nächsten Dialog
ersetzt dort die Hypothese durch eine echte Zählung und macht den Faktor 0,5–2 überflüssig. Nach zwei
bis drei weiteren Interactions im gewählten autonomen Modus liegen außerdem Messwerte für **diese**
Arbeitsweise vor; dann entfällt die Vermutung der Übertragung.

## Bedingungen, unter denen diese Schätzung gilt

> **Eine Schätzung ist keine Garantie** und keine Zusage. Die tatsächliche Arbeitszeit kann
> **geringer** ausfallen und sie kann **höher** ausfallen. Wer aus einer Bandbreite einen einzelnen
> Wert herausnimmt, hat keine Schätzung mehr, sondern eine Behauptung.

Verweisnummern in den Katalog `~/.claude/commands/projekt/_bedingungen.md`:

| Bedingung | Nr. | Kern |
|---|---|---|
| Keine parallele Arbeit | #200, #204, #301 | Die Stunden gelten für Arbeit an einer Sache; Wechselkosten sind nicht eingerechnet |
| Benannter Ansprechpartner mit Mandat | #103, #107 | Eine Person, namentlich, darf über Umfang und Abweichung entscheiden |
| Rückmeldungen zügig | #309, #303 | Wartezeit auf Antworten ist nicht enthalten und schlägt 1:1 durch |
| Full Kit vor Beginn | #308, #300 | Umgebung, Zugänge, Daten, Bau- und Startweg vollständig vorhanden |
| Umfang fest, Änderung nur per Verfahren | #101, #108 | Alles unter *Nicht Gegenstand* ist ausdrücklich nicht enthalten |
| Fertig-Kriterium mit Ja/Nein prüfbar | #102, #505 | Je Arbeitspaket eine zuständige Person und ein prüfbares Ergebnis |
| Zahlen ehrgeizig, Puffer sichtbar | #201, #202, #208 | Die Arbeitspakete sind ehrgeizig gerechnet; der Puffer steht als eigene Zeile daneben |
| Ungestörte Arbeitszeit | #407, #302 | Zerstückelte Zeit vervielfacht dieselben Arbeitspakete |
| Rangfolge im Konfliktfall geklärt | #109 | Vorher klären, was zuerst nachgibt: Umfang, Termin oder Qualität |
| Von den Ausführenden getragen | #400, #405 | Gilt erst, wenn die ausführende Person sie für erreichbar hält |
| Besetzung wie angenommen | #400, #405 | Gerechnet für autonome Umsetzung ohne Dialog; bei anderer Besetzung gilt die Zahl nicht |

**Projektspezifisch kommt hinzu:**

| Bedingung | Kern |
|---|---|
| Die Kernregel bleibt bestehen | `KanbanC.Blazor` bekommt keine Projektreferenz auf `KanbanC.BL`. Fällt diese Regel, ändert sich der Zuschnitt jeder Interaction — jede Funktion braucht dann keinen Endpunkt mehr, und die Zahl gilt nicht |
| `TreatWarningsAsErrors` bleibt aktiv | Die Zahl enthält den Aufwand, warnungsfrei zu bauen |
| Coverage-Ziel ~90 % über alle Testebenen | Die Zahl enthält Unit-, Integrations- und E2E-Tests. Ein niedrigeres Ziel macht sie kleiner, ein höheres größer |

**Schlusssatz.** Läuft die Arbeit nicht geschützt und kontrolliert ab — parallele Aufgaben, kein
erreichbarer Ansprechpartner, fehlende Voraussetzungen, unterwegs wachsender Umfang, zerstückelte
Arbeitszeit — dann ist diese Schätzung nicht ungenauer, sondern **unbrauchbar**. Der ehrliche Umgang
damit ist dann nicht, die Zahl zu verteidigen oder zu strecken, sondern **neu zu rechnen**.

## Empfehlung

*(leer — es wurde nicht um eine Einschätzung gebeten)*

## Notizen / Quellen

- Stand der WBS: `Dokumentation/Planung/kanbanc.md`, Frontmatter `zuletzt: 2026-08-29`. 43 Vertical
  Slices, davon 6 grün; 39 Interactions, davon 2 grün; 26 Arbeitspakete, alle grün.
- Messwerte: `Schaetzungen/_ist-zeiten.md` — 6 bestätigte Einträge vom 2026-08-29, Modus *moderat*.
- Durchsatz-Gegenprobe: `git log` vom 2026-08-29, 09:21–16:05 (48 Commits).
- Regeln der Zählung: Skill `work-breakdown-structure`, Abschnitte *Schätzen durch Zählen*,
  *Hypothesen-Zählung*, *Fortschritt und Rest*.
- Erstellt am 2026-08-29. Die Besetzung „autonom, ohne Dialog" wurde im Interview gewählt; der
  Hinweis, dass dafür kein Messwert vorliegt, wurde vor der Wahl gegeben.
