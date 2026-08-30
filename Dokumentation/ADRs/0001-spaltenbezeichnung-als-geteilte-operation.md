# ADR-0001: `Spaltenbezeichnung` wird von Validator und Repository gerufen

**Status**: Akzeptiert
**Datum**: 2026-08-30
**Entscheider**: Stefan Hoffmann
**Kontext-Anforderung**: [R00004](../../Anforderungen/R00004-layout-modus-spaltenpflege.md)

## Kontext

`R00004` macht die Spaltenbezeichnung je Board eindeutig, ohne Rücksicht auf Groß-/Kleinschreibung und umschließende Leerzeichen. Damit entsteht eine Frage, die vorher niemand stellen musste: **wann sind zwei Bezeichnungen dieselbe?** Die Antwort wird an drei Stellen gebraucht — beim Prüfen des Konflikts, beim Schreiben der Speicherform und beim Formulieren der Zurückweisungsmeldung.

Die Anforderung legt dafür die Operation `Spaltenbezeichnung` mit `Normalisiert` und `SindGleich` fest und nennt sie „die eine Stelle, die entscheidet, wann zwei Bezeichnungen dieselbe sind".

Damit gerät das Projekt in einen Konflikt zwischen zwei Regeln des Hausstils:

- **C02 (PoMO)** — *„Operation referenziert nie Operation (auch nicht als Konstruktor-/Feld-Dependency); nur Integrationen kennen die Einheiten, die sie verdrahten."*
- **C23 (DRY bei semantischer Äquivalenz)** — Die drei Stellen meinen dasselbe. Ändert sich die Vergleichsregel, müssen alle drei zwingend mitziehen; Kopien liefen bei der nächsten Änderung auseinander.

Heutige Aufrufer: `SpaltenValidator.cs:26,29` (Operation) und `SpaltenRepository.cs:37,56` (Provider).

## Optionen

### Option A: `Spaltenbezeichnung` bleibt eigene Operation, Validator und Repository rufen sie
- Vorteile: Die Vergleichsregel steht genau einmal. Sie ist ohne Testdouble unit-testbar (8 Tests). Der Validator formuliert seine Meldung mit derselben Normalform, die geschrieben wird — Meldung und Speicherstand können nicht auseinanderlaufen.
- Nachteile: Verstößt gegen den Wortlaut von C02. Der erste Selbsttest der Regel schlägt an: Entfernt man `Spaltenbezeichnung`, muss der Validator geändert werden.

### Option B: `SpaltenService` (Integration) normalisiert und übergibt die Normalform
- Vorteile: C02-konform — nur die Integration kennt beide Einheiten. Das Repository bekommt fertige Werte und trifft keine fachliche Entscheidung mehr.
- Nachteile: Die Regel „getrimmt speichern" verlässt den Provider und liegt beim Aufrufer. Jeder künftige Schreibpfad — Import (`I0030`), Board-Kopie, ein zweiter Dienst — muss daran denken; vergisst ihn einer, ist der Index umgehbar. Die Zusicherung wandert von einem Ort, der sie erzwingen kann, zu einem, der sie erinnern muss. Der Validator bräuchte weiterhin eine Vergleichsfunktion, also entweder dieselbe Referenz oder eine Kopie.

### Option C: Vergleich und Normalisierung an jeder Stelle ausschreiben
- Vorteile: Keine Referenz zwischen Operationen; jede Einheit steht allein.
- Nachteile: Drei Kopien derselben fachlichen Regel — genau der Fall, den C23 als „müssen sich zwingend mitändern" beschreibt. Die Umlaut-Einschränkung (siehe *Missing-Docs* in `R00004`) müsste an drei Stellen gleich dokumentiert und gleich geändert werden.

## Entscheidung

**Gewählt: Option A**

C23 schlägt hier C02, weil die Vergleichsregel eine einzige fachliche Aussage ist und ihre Verdopplung ein absehbarer Fehler wäre — sie wird sich ändern, sobald die Umlaut-Lücke geschlossen wird. Der Preis ist eine Referenz zwischen Funktionseinheiten, die der Hausstil sonst untersagt; sie ist eng begrenzt: `Spaltenbezeichnung` ist eine zustandslose Wertfunktion ohne Ressourcenzugriff, hat keine Abhängigkeiten und braucht in keinem Test ein Testdouble.

Bewusst in Kauf genommen: Der Wortlaut von C02 ist verletzt, und wer die Regel mechanisch prüft, findet hier einen Treffer. Deshalb dieses ADR.

## Konsequenzen

**Positiv:**
- Die Antwort auf „wann sind zwei Bezeichnungen dieselbe?" steht an einer Stelle und ist dort unit-getestet.
- Der Provider erzwingt die Speicherform selbst; kein künftiger Schreibpfad kann sie versehentlich umgehen.
- Zurückweisungsmeldung und gespeicherter Wert stammen aus derselben Funktion.

**Negativ / in Kauf zu nehmen:**
- Verstoß gegen den Wortlaut von C02 an zwei Aufrufstellen (`SpaltenValidator`, `SpaltenRepository`).
- Kommt eine weitere solche Wertfunktion hinzu, ist die Ausnahme kein Einzelfall mehr, sondern ein Muster — dann gehört sie als eigene Kategorie in den Stilkatalog statt als Ausnahme in ein ADR.

**Folgeentscheidungen / Aufgaben:**
- [ ] Bei der nächsten vergleichbaren Wertfunktion prüfen, ob der Stilkatalog eine Kategorie „zustandslose Wertfunktion" braucht, die Operationen rufen dürfen.
- [ ] Beim Schließen der Umlaut-Lücke (`COLLATE NOCASE` greift nur auf ASCII) bleibt `Spaltenbezeichnung` der einzige Ort, an dem die Vergleichsregel geändert wird.

## Quellen / Bezüge

- [R00004 — Layout-Modus für die Spaltenpflege](../../Anforderungen/R00004-layout-modus-spaltenpflege.md), Abschnitte „Grobentwurf" und „Missing-Docs"
- `~/.claude/skills/csharp-stil/SKILL.md` — C02 (PoMO), C03 (IODA-Typologie), C23 (DRY bei semantischer Äquivalenz), C26 (bewusste Abweichungen dokumentieren)
- `Source/KanbanC.BL/Operations/Boards/Spaltenbezeichnung.cs`, `SpaltenValidator.cs:26,29`, `Source/KanbanC.BL/Persistenz/Boards/SpaltenRepository.cs:37,56`
