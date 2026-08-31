---
application: kanbanc
wbs: Dokumentation/Planung/kanbanc.md
canvas: https://claude.ai/code/artifact/b61e3007-056d-44e0-9cf5-7350c22f858a
seed: .claude/wireframes/kanbanc.html
art: mockup
richtung: aus dem Bestand — gestaltung.css (Caprasimo/Figtree, warmes Sandklima, runde Formen)
stand: 2026-08-30
zurueckgeholt: 2026-08-30
---

# Wireframes — KanbanC

Der Canvas trägt zwei Artboards: `Main.dc.html`, den Screen-Flow über alle neun
Dialogs, und `D0003.dc.html`, den ersten ausdetaillierten Bildschirm. Die übrigen
Dialogs bleiben Kästen im Flow; ihr Detail-Artboard entsteht mit
`/wireframe verfeinern <dialog>`, wenn der Dialog dran ist (Rolling Wave).

Reife je Dialog wird aus dem Dateibestand gerechnet: `D0003` steht auf `wireframe`,
die acht übrigen auf `flow`.

| Datei | Was |
| --- | --- |
| `Main.dc.html` | Screen-Flow: neun Dialog-Kästen, dreizehn beschriftete Übergänge, Ampel je Dialog |
| `D0003.dc.html` | **Board bedienen**, 1440×900 — gefüllte Spaltenbahnen mit der Kartenform; die fünf Interactions I0010–I0014 als Zustände im selben Schirm, dazu drei Randfälle |
| `canvas.json` | Layout des Canvas, zwei Rahmen untereinander, Start in der Canvas-Ansicht |
| `kanbanc-wireframes.html`, `wireframes.js`, `styles.css`, `README.md` | **älterer Satz, unangetastet** — acht gezeichnete Schirme mit Varianten, aus denen `verfeinern` schöpft; siehe Offene Fragen |

## Richtung

**Nicht entschieden, sondern vorgefunden.** Das Repository hat bereits ein
Design-System, das in der laufenden Anwendung gilt; ein Richtungsvergleich mit
`RichtungA/B/C` entfällt deshalb ersatzlos.

Fundstellen, aus denen die Werte **exakt** übernommen sind:

- `Source/KanbanC.Blazor/wwwroot/gestaltung.css` — der eine Ort der Gestaltungswerte.
  Mit `R00005` aus `Dokumentation/Wireframes/styles.css` übernommen (Bubble B0052).
- `Source/KanbanC.Blazor/wwwroot/oberflaeche.css` — die Formen, die mehr als eine
  Komponente teilt (`.seitenkopf`, `.meldung`, `.kontrollfeld`).

**Typografie.** Überschriften `Caprasimo` 400 (`--font-heading`), Zeilenhöhe 1.12,
Laufweite −0.015em; Fließtext `Figtree` 300–900 (`--font-body`), 15 px, Zeilenhöhe
1.55. Größen: h1 42, h2 32, h3 25, h4 20, h5 16, h6 13 px, h6 versal mit 0.08em.
Beide Schriften liegen in der Anwendung als lokale `woff2` unter `wwwroot/fonts/`
(SIL OFL) — im Canvas werden dieselben Familien über Google Fonts geladen, weil
die Artboard-Sandbox keine Repo-Dateien sieht; der Fallback-Stack ist in beiden
Fällen `system-ui, sans-serif`, wie in `gestaltung.css`.

**Farbklima.** Warmes Sand: Grund `--color-bg` `#f5ead8`, Flächen `--color-surface`
`#ebddc5`, Schrift `--color-text` `#201e1d`, Trennlinien
`color-mix(in srgb, #201e1d 16%, transparent)`. Erster Akzent `--color-accent`
`#c67139` (gebrannte Terrakotta), zweiter `--color-accent-2` `#7a8a5e` (Olive).
Drei Tonleitern à neun Stufen, in OKLCH auf einer gemeinsamen Helligkeitsskala
gerechnet: `--color-neutral-100…900` (`#f9f4ed` … `#2e2b25`),
`--color-accent-100…900` (`#fff2eb` … `#402310`),
`--color-accent-2-100…900` (`#f0fae1` … `#272e1b`).

**Dichte und Form.** Abstandsleiter `--space-1…8` = 4.4 / 8.8 / 13.2 / 17.6 / 26.4 /
35.2 px — keine 4er-Rasterung, die Werte werden nicht gerundet. Radien
`--radius-sm` 8, `--radius-md` 16, `--radius-lg` 28 px; darüber liegt eine
Schlussregel, die alles weicher macht: `.card` und `.dialog` auf
`calc(var(--radius-lg) * 1.15)` ≈ 32.2 px, `.btn`, `.tag`, `.seg` und `.input` auf
`999px` (Pille), `.input` mit `padding-inline: 14px`. Schatten
`--shadow-sm/md/lg` als tintengetönte `color-mix`-Werte über `#2e2b25` bei 14 / 16 / 22 %.

**Vokabular, das schon steht** — `verfeinern` benutzt diese Klassen, statt neue zu
erfinden: `.btn` mit `.btn-haupt/-neben/-schlicht/-symbol/-breit`, `.field`/`.input`,
`.radio`, `.seg`/`.seg-opt`, `.card` mit `.card-kicker/-title/-body/-meta`,
`.tag` mit `-accent/-accent-2/-neutral/-outline`, `.nav`/`.nav-brand`, `.table`,
`.dialog`/`.dialog-backdrop`, `.elev-sm/-md/-lg`, `.text-muted`, `.hr`,
sowie `.seitenkopf`, `.meldung` (`.meldung-abweisung`) und `.kontrollfeld`.

**Vorbild.** Kanbanflow-Dichte, wie die Vision und der ältere Wireframe-Satz es
festhalten: viel Information auf wenig Fläche, aber in warmem, rundem Gewand
statt im kühlen Werkzeugton.

## Entscheidungen

Der Lauf fand keinen Menschen vor. Alle Entscheidungen sind nach der Regel
„entscheiden statt raten, Annahme benennen" getroffen und stehen hier zur
Widerrede.

| Datum | Frage | Entscheidung | Grund |
|---|---|---|---|
| 2026-08-30 | Mockup oder Prototyp? | `art: mockup` | Der Screen-Flow ist eine Landkarte, keine bedienbare Oberfläche; ob einzelne Schirme klickbar gebraucht werden, entscheidet sich erst beim jeweiligen Dialog. |
| 2026-08-30 | Gestaltungsrichtung | aus dem Bestand, `gestaltung.css` | Das Design-System gilt bereits in der laufenden Anwendung (R00005, I0041 grün). Eine neue Richtung zu erfinden würde eine gebaute und getestete Oberfläche entwerten. |
| 2026-08-30 | Woher die Ampelfarben? | rot `#b8482f`, gelb `#d9a03c` neu abgeleitet, grün `#7a8a5e` = `--color-accent-2` | `gestaltung.css` kennt keine Statusfarben. Beide neuen Werte sind in OKLCH auf die Sandpalette gerechnet (Chroma in der Ordnung von `--color-accent`), damit die Ampel lesbar bleibt, ohne aus dem Klima zu fallen. Sie gehören dem Wireframe, nicht der Anwendung. |
| 2026-08-30 | Wo ist der Einstieg? | D0001 „Boards führen", markiert als `Einstieg · /boards` | Die gebaute Kopfzeile (B0056) führt Boards, Auswertungen, Kontributoren; die Anwendung landet auf der Board-Übersicht. D0002 und D0009 sind über die Kopfzeile erreichbar, aber kein Startpunkt. |
| 2026-08-30 | D0007 hat keinen eigenen Schirm — trotzdem ein Kasten? | ja, mit einer Zeile „Kein eigener Schirm" im Kasten | Der Kontrakt verlangt einen Kasten je Dialog, und die WBS führt D0007 als Dialog. Ihn wegzulassen würde den Flow von der WBS abweichen lassen. |
| 2026-08-30 | Welche Übergänge werden gezeichnet? | 13 Kanten, **jede durch die Spalte `Braucht` gedeckt**; D0003 → D0009 gestrichelt | Eine vierzehnte Kante D0004 → D0006 („Zeiten der Karte") war gezeichnet und wurde gestrichen: I0026 braucht I0024, nicht I0015 — sie hätte eine Abhängigkeit behauptet, die die WBS nicht kennt. Gestrichelt ist D0003 → D0009, weil I0037 „Rohdaten über die API abrufen" kein Klickweg ist, sondern der Zugang der Agenten — die Kernzusage der Vision. |
| 2026-08-30 | Zwei Werte außerhalb der Typoskala | Kanten-Beschriftungen 10 px, innerer Kastenabstand `--space-1` (4.4 px) | Beide Werte stehen in `gestaltung.css` (10 px als `.card-kicker`, 4.4 px als `--space-1`), aber in anderer Rolle. Ein Flussdiagramm ist dichter als eine Kachelseite; die Leiter selbst wird nicht verlassen. |
| 2026-08-30 | Wie viele Artboards? | eines (`Main.dc.html`) | `entwerfen` macht Flow und Richtung; die Richtung stand schon fest, also entfallen auch die Richtungs-Artboards. Dialog-Artboards sind Sache von `verfeinern`. |
| 2026-08-30 | **D0003** — Kartenform trotz fremder Inhalte vollständig zeichnen? | ja, ganze Anatomie aus `wkarte()`, die fremden Teile in der Lesehilfe am Fuß benannt | Das Artboard ist die Vorlage für `R00006`. Zeichnete man nur, was I0010/I0011 selbst füllen, bliebe ein Titel übrig — und die Umsetzung erfände die Kartenform. Benannt sind: Klassennummer I0021 (D0005), Subtask-Zähler I0016 und Etikett I0015 (D0004), Soll-/Ist-Zeit und laufender Timer I0023/I0024 (D0006), Kontributor-Avatar I0008 (D0002). |
| 2026-08-30 | Welche Bahnenvariante des alten Satzes? | A/B ohne jedes Live-Element | In den Bahnen sind A und B deckungsgleich; sie unterscheiden sich nur im Ort der Live-Ereignisse. Die gehören D0007 (rot), und `README.md` des alten Satzes setzt die Wahl Spur/Laufband ausdrücklich erst mit I0028. Also weder Spur noch Laufband. |
| 2026-08-30 | Karten und Meldung auf welchem Grund? | Karte und Meldung `--color-bg` `#f5ead8`, Bahn `--color-surface` `#ebddc5` | `.card` und `.meldung` tragen im Token-Sheet selbst `--color-surface` — auf einer Bahn derselben Farbe wären sie unsichtbar. Die Umkehrung ist die kleinste Abweichung, die die Schichtung rettet; alle übrigen Werte (Radius 16 px, `--shadow-sm`, Polsterung) bleiben unverändert. |
| 2026-08-30 | I0014 hat im alten Satz kein Bedienelement — welches? | `⋯`-Menü auf der Karte, ein einziger Eintrag „Archivieren" samt Erläuterung | Der Kontrakt verlangt zu jeder Interaction ein sichtbares Bedienelement. D0003 besitzt genau eine Kartenaktion; ein Menü mit einem Eintrag ist ehrlicher, als Einträge aus D0004 dazuzuerfinden. |
| 2026-08-30 | I0011 offen oder geschlossen zeigen? | beides — geschlossen (`+ Karte`) in drei Bahnen, offen in „Bereit" | Der gebaute Fuß reserviert genau eine Stelle. Ein Artboard, das nur den Ruhezustand zeigt, verschiebt die Frage nach Eingabefeld und Knöpfen in die Umsetzung. |
| 2026-08-30 | Welche Ränder? | leere Bahn („Prüfung", Zahl 0), zu langer Titel (BES-04, drei Zeilen), zurückgewiesene Anlage („Ohne Titel entsteht keine Karte") | Die drei Ränder, die beim Bauen von I0010/I0011 unweigerlich auftreten. Die Zurückweisung nimmt die Form von `.meldung-abweisung` aus `oberflaeche.css`. |
| 2026-08-30 | Wie unterscheiden sich Mensch und KI auf der Karte? | Avatar-Kreis 20 px; Mensch Olive (`--color-accent-2-200/-800`), KI Terrakotta (`--color-accent-200/-800`) | Der alte Satz trennt sie über Klassen ohne Farbfestlegung. Zwei Akzente gleicher Chroma und Helligkeit sagen genau das, was die Vision verlangt: gleichberechtigt, nicht gleich. |
| 2026-08-30 | Zeichen oder SVG für Symbole? | `✓` der Abschlussspalte bleibt Zeichen, alles Neue ist Inline-SVG | `Spaltenbahnen.razor` rendert `✓` als Zeichen — das Artboard bildet die gebaute Anatomie ab, statt still einen Diff zu erzeugen. Für Plus, Menü, Häkchenkasten, Uhr, Archivkiste und Chevron gilt die Regel des `design`-Skills: keine Dingbats. |
| 2026-08-30 | Beispieldaten | aus dem alten Satz übernommen; neu nur der Starttermin 01.08.2026 | WBS-31, BES-04, WBS-28, BUG-07, WBS-14, WBS-21, BUG-05, WBS-11, WBS-09, WBS-06, BES-01 stehen so in `wireframes.js`. Der gebaute Kopf zeigt Start **und** Ziel, der alte Satz nur ein Datum — der Starttermin ist damit die einzige gesetzte Zahl. |
| 2026-08-30 | Fünf Bahnen à 256 px passen nicht mittig in 1440 px | links bündig ab 26,4 px, Rest ist Überlauf | 5 × 16rem + 4 × `--space-3` = 1332,8 px. `.spaltenbahnen` trägt `overflow-x: auto`; ein zentriertes Board würde eine Ausrichtung behaupten, die der gebaute Code nicht hat. |
| 2026-08-30 | Titel des Artifacts | bleibt „KanbanC" | Der veröffentlichte Canvas heißt so seit `entwerfen`; ein Titelwechsel beim Republish würde ihn in Listen und Freigaben zu einem anderen Ding machen. |

## Offene Fragen

1. **Der ältere Wireframe-Satz ist quer zu den Dialogs geschnitten.**
   `kanbanc-wireframes.html` / `wireframes.js` zeichnen **acht Schirme** mit
   Varianten; der Schirm „Board" mischt Knoten aus D0001 (I0004), D0003 (I0010–I0013)
   und D0007. Wenn `verfeinern` diese Zeichnungen aufgreift, sind sie auf die
   **neun Dialogs** umzuschneiden — der Schirm ist nicht der Dialog. Der Satz bleibt
   bis dahin unangetastet; er ist über `R00005` verbindlich und inhaltlich wertvoll.
   Zuordnung, so wie sie heute steht:

   | Schirm (alt) | Dialogs (WBS) |
   | --- | --- |
   | Start / Board-Übersicht | D0001 |
   | Board anlegen & gestalten | D0001, D0005 (Klassen-Teil) |
   | Board | D0001 (I0004), D0003, D0007 |
   | Kartendetail | D0004, D0005 (Nummer), D0006 (Zeiten) |
   | WBS-Import | D0008 |
   | Auswertungen | D0009 |
   | Zeiten je Kontributor | D0006 |
   | Kontributoren & Identität | D0002 |

2. **`art: mockup` ist gesetzt, nicht abgestimmt.** Für D0003 (Karte verschieben)
   und D0001 (Layout-Modus) könnte ein klickbarer Prototyp die Bedienbarkeit
   klären, die eine statische Ansicht offen lässt.

3. **Die zwei neuen Ampelfarben sind gesetzt, nicht abgestimmt.** Sie leben nur im
   Wireframe; soll die Anwendung selbst je Statusfarben brauchen, gehören sie in
   `gestaltung.css` und damit in eine Anforderung.

4. **Ist D0001 wirklich der Einstieg?** I0008 „Identität wählen" sagt „Beim Öffnen
   der Oberfläche wählt man, wer man ist" — das könnte ein vorgelagerter Schirm
   sein oder ein Element der Kopfzeile. Entscheidet sich bei D0002.

5. **Das WBS-Frontmatter kennt den Canvas noch nicht.** `/wireframe` schreibt nicht
   in die WBS. `/planung aendern A0001` muss `wireframes: Dokumentation/Wireframes/`
   setzen — erst damit finden `/anforderung`, `/implementierung` und `/github` den Canvas.

6. **Das Kartenmenü ist gesetzt, nicht abgestimmt.** `⋯` auf der Karte mit
   „Archivieren" — die Alternative wäre, das Archivieren erst im Kartendetail (D0004)
   anzubieten. Dann hätte I0014 auf dem Board kein Bedienelement und wäre ohne D0004
   nicht bedienbar. Entscheidet sich mit `R00006` oder spätestens bei D0004.

7. **Die Ablagestelle beim Ziehen trägt eine Positionsangabe** („hier ablegen ·
   Position 1"). Ob die Position beziffert wird oder die Lücke allein genügt, sagt
   das Fertig-Kriterium von I0012 nicht. Gezeichnet ist die ausführlichere Form.

8. **Vier Elemente der Kartenform gehören späteren Dialogs.** Baut `R00006` nur
   I0010 und I0011, stellt sich dieselbe Frage wie bei der Kartenzahl und `+ Karte`:
   reservierte leere Stelle im Markup (wie `B0063` es für den Bahnenkopf gemacht hat)
   oder gar nicht anlegen. Das Artboard zeigt die Zielform; welchen Weg die
   Umsetzung nimmt, gehört in die Anforderung.
