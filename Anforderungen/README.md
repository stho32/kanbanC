# Anforderungen

Anforderungsdokumente im Format `RXXXXX-kebab-case-zusammenfassung.md`, fortlaufend ab `R00001`.

`R00000-vision.md` ist reserviert: die **Vision** der Anwendung — keine Anforderung, nicht abhakbar. Sie wird ergänzt, nie ersetzt; jede Ergänzung bekommt eine Zeile in der Ergänzungshistorie.

## Commands

| Command | Zweck |
|---|---|
| `/vision stand` | Zielbild gegen den Stand der WBS lesen |
| `/anforderung aus-slice <knoten>` | aus einem Slice der WBS eine Anforderung schreiben |
| `/anforderung neu` | Anforderung aus einer Featurebeschreibung |
| `/implementierung im-pair <knoten>` | Umsetzung im Dialog, Bubble für Bubble |
| `/implementierung pruefen RXXXXX` | Implementation gegen die Akzeptanzkriterien |

Der Umfang steht nicht hier, sondern in der WBS: `Dokumentation/Planung/kanbanc.md`.
