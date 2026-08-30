# Schriften der Oberfläche

Beide Schriften liegen bewusst im Repository und werden nicht zur Laufzeit von
Google geladen: KanbanC läuft im LAN und darf ohne Netzzugang nicht anders
aussehen (R00005, US-2). Eingebunden werden sie über `@font-face` in
[`../gestaltung.css`](../gestaltung.css).

| Datei | Schrift | Schnitt | Verwendung |
| --- | --- | --- | --- |
| `caprasimo-latin.woff2`, `caprasimo-latin-ext.woff2` | Caprasimo | statisch, 400 | `--font-heading` — Überschriften |
| `figtree-latin.woff2`, `figtree-latin-ext.woff2` | Figtree | variabel, wght 300–900 | `--font-body` — Fließtext |

Bezogen am 2026-08-30 von `fonts.gstatic.com` über das Stylesheet
`https://fonts.googleapis.com/css2?family=Caprasimo&family=Figtree:wght@400;600;700&display=swap`
(mit Chrome-Kennung abgerufen, damit die `woff2`-Fassungen kommen). Die Aufteilung in
`latin` und `latin-ext` samt `unicode-range` ist von dort übernommen.

`Caveat` wird nicht mitgeliefert. Die Schrift steht im Wireframe-Rahmen, aber in keiner
Variablen des Token-Sheets — sie kommt erst dazu, wenn ein Schirm sie wirklich braucht.

## Lizenz

Beide stehen unter der SIL Open Font License 1.1; die vollständigen Texte liegen als
`OFL-Caprasimo.txt` und `OFL-Figtree.txt` daneben.

- Caprasimo: Copyright 2023 The Caprasimo Project Authors (https://github.com/docrepair-fonts/caprasimo-fonts)
- Figtree: Copyright 2022 The Figtree Project Authors (https://github.com/erikdkennedy/figtree)
