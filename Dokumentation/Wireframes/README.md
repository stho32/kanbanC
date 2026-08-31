# Wireframes — KanbanC

> **Verbindlich.** Das ist das Design, das die Anwendung erreichen soll — siehe
> [CLAUDE.md](../../CLAUDE.md) und die Anforderung
> [R00005](../../Anforderungen/R00005-oberflaeche-nach-wireframes.md), die es uebernimmt.
> Wer an der Oberflaeche arbeitet, sieht hier zuerst nach.

Low-fi Strukturskizzen der Oberflaeche, Haltung **Kanbanflow-dicht**.
Oeffnen: `kanbanc-wireframes.html` im Browser — keine Abhaengigkeiten ausser den
Web-Fonts (Caprasimo, Figtree, Caveat) von Google Fonts.

| Datei | Zweck |
| --- | --- |
| `kanbanc-wireframes.html` | Rahmen, Reiter je Schirm, Wireframe-CSS |
| `wireframes.js` | die Schirme und ihre Varianten als Daten |
| `styles.css` | Token-Sheet (Farben, Schrift, Abstaende, Radien) |

## Schirme

| Schirm | Varianten | Abgeleitet aus |
| --- | --- | --- |
| Start / Board-Uebersicht | 2 | I0002, F0004, I0005 |
| Board | 3 | F0005, I0003, I0004, I0010–I0013, D0007 |
| Kartendetail | 3 | I0015–I0019, I0021, I0026 |
| Board anlegen & gestalten | 2 | I0001, F0007–F0009, I0020 |
| WBS-Import | 2 | I0030–I0032 |
| Auswertungen | 2 | I0033–I0037 |
| Zeiten je Kontributor | 2 | I0023–I0027 |
| Kontributoren & Identitaet | 3 | I0006–I0009 |

> **Zuordnung zu den Dialogs der WBS.** Diese acht Schirme sind quer zu den neun
> Dialogs geschnitten — der Schirm ist nicht der Dialog. Welcher Schirm welchen
> Dialog speist, welche Variante gesetzt ist und welche Interactions von keinem
> Schirm gedeckt sind, steht verbindlich in [`_wireframes.md`](_wireframes.md),
> Abschnitt „Zuordnung Schirm → Dialog". Diese Datei wird dafuer nicht umgebaut.

## Bewusst nicht gezeichnet

WIP-Limits je Spalte, Swimlanes und wiederkehrende Karten — in
`Dokumentation/Planung/kanbanc.md` geprueft und verworfen. Stattdessen:
Abschlussspalte mit Anzeigegrenze N und Gruppierung nach Erledigungsdatum
(I0013), Kartenzahl je Spalte einschaltbar (I0004).

## Stand je Schirm und Variantenwahl

Wo mehrere Varianten gezeichnet sind, faellt die Wahl bei der Interaction, die den
Schirm baut — nicht vorher und nicht hier.

| Schirm | Stand | Variantenwahl |
| --- | --- | --- |
| Start / Board-Uebersicht | gebaut (I0001–I0003), Umbau in R00005 | **A** gesetzt. B setzt auf laufende Timer (I0027) und den Live-Kanal (D0007) auf und ist heute nicht baubar |
| Board | gebaut (I0003, I0040), Umbau in R00005 | Bahnen aus **A/B** gesetzt — dort deckungsgleich. Ob die Live-Ereignisse als Spur rechts (A) oder als Laufband oben (B) erscheinen, entscheidet I0028. **C** bliebe eine spaetere Zweitansicht |
| Board anlegen & gestalten | gebaut (I0001, I0003, I0040), Umbau in R00005 | **A** fuer das Anlegen, **B** fuer den Layout-Modus. Der Klassen-Teil aus B gehoert zu I0020 |
| Kartendetail | offen | A, B oder C — Wahl bei D0004 (I0015–I0019) |
| WBS-Import | offen | A oder B — Wahl bei D0008 (I0030–I0032) |
| Auswertungen | offen | A oder B — Wahl bei D0009 (I0033–I0037) |
| Zeiten je Kontributor | offen | A oder B — Wahl bei D0006 (I0023–I0027) |
| Kontributoren & Identitaet | offen | A fuehrt die Liste (I0006, I0007, I0009); fuer die Identitaetswahl B oder C — Wahl bei I0008 |

## Was uebernommen wird

`styles.css` ist das Gestaltungsfundament und wandert als `gestaltung.css` in
`Source/KanbanC.Blazor/wwwroot/`. Die Klassen mit `w`-Praefix aus `wireframes.js`
und `kanbanc-wireframes.html` sind Low-Fi-Formen zum Zeichnen und bleiben hier.

Diese Dateien werden nicht nachgefuehrt, wenn der Code sie einholt. Weicht der Code
ab, gehoert das in eine Anforderung.
