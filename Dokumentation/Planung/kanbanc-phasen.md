---
application: KanbanC
wbs: Dokumentation/Planung/kanbanc.md
zuletzt: 2026-09-08
---

# Phasen — KanbanC

Welche Phasen ein Arbeitspaket (eine Interaction der WBS) nach dem Bau durchläuft, und je Arbeitspaket die Durchläufe. Standardmodell nach Skill `work-breakdown-structure`, „Phasen je Arbeitspaket"; Datenmodell in `commands/planung.md`. Gefahren wird eine Phase mit `/testen <phase> [<AP>]`, Durchläufe schreibt `/testen eintragen <AP> <Phase> bestanden | befund "…" | entfaellt "<Grund>"`; weitere Phasen (Beta, Produktion je Kunde) legt `/planung phase anlegen` an — Entscheidung des Auftraggebers.

## Phasen

| Nr | Phase | Abschließt | Nachweis | Eingebettet in | Gleich wie | Status |
|---|---|---|---|---|---|---|
| 1 | Entwicklung | Subagent oder Entwickler | — | | | |
| 2 | Entwicklertest | Entwickler | user-story | | | |
| 3 | Abnahme | Auftraggeber | akzeptanzkriterien | | | |

## Durchläufe

| Arbeitspaket | Phase | Datum | Ergebnis | Wer | Beleg |
|---|---|---|---|---|---|
