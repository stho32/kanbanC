---
application: KanbanC
wbs: Dokumentation/Planung/kanbanc.md
zuletzt: 2026-09-08
---

# Phasen — KanbanC

Welche Phasen ein Arbeitspaket (Blatt der Steuerungsebene der WBS) nach dem Bau durchläuft, und je Arbeitspaket die Durchläufe. Standardmodell nach Skill `work-breakdown-structure`, „Phasen je Arbeitspaket"; Datenmodell in `commands/planung.md`. Durchläufe schreibt `/anwendung phase <AP> <Phase> bestanden | befund "…" | entfaellt "<Grund>"`; weitere Phasen (Beta, Produktion je Kunde) sind Zeilen in `## Phasen` — Entscheidung des Auftraggebers.

## Phasen

| Nr | Phase | Abschließt | Nachweis | Eingebettet in | Gleich wie |
|---|---|---|---|---|---|
| 1 | Entwicklung | Subagent oder Entwickler | Bubbles grün, Gate bestanden | | |
| 2 | Entwicklertest | Entwickler | gegen die User Story | | |
| 3 | Abnahme | Auftraggeber | gegen die Akzeptanzkriterien | | |

## Durchläufe

| Arbeitspaket | Phase | Datum | Ergebnis | Wer | Beleg |
|---|---|---|---|---|---|
