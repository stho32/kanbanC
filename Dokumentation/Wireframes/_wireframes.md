---
application: kanbanc
wbs: Dokumentation/Planung/kanbanc.md
canvas: https://claude.ai/code/artifact/b61e3007-056d-44e0-9cf5-7350c22f858a
seed: .claude/wireframes/kanbanc.html
art: mockup
richtung: aus dem Bestand — gestaltung.css (Caprasimo/Figtree, warmes Sandklima, runde Formen)
stand: 2026-09-03
zurueckgeholt: 2026-09-03
---

# Wireframes — KanbanC

Der Canvas trägt drei Artboards: `Main.dc.html`, den Screen-Flow über alle neun
Dialogs, sowie `D0002.dc.html` und `D0003.dc.html`, die beiden ausdetaillierten
Bildschirme. Die übrigen Dialogs bleiben Kästen im Flow; ihr Detail-Artboard
entsteht mit `/wireframe verfeinern <dialog>`, wenn der Dialog dran ist
(Rolling Wave). Woraus ein solcher Lauf schöpft, sagt die
[Zuordnung Schirm → Dialog](#zuordnung-schirm--dialog).

Reife je Dialog wird aus dem Dateibestand gerechnet: `D0002` und `D0003` stehen auf
`wireframe`, die sieben übrigen auf `flow`.

| Datei | Was |
| --- | --- |
| `Main.dc.html` | Screen-Flow: neun Dialog-Kästen, dreizehn beschriftete Übergänge, Ampel je Dialog |
| `D0002.dc.html` | **Kontributoren führen**, 1440×1560 — die Liste mit den drei Arten als Hauptzustand (I0006, I0007, I0009); darunter die Identitätswahl I0008 als **zwei nebeneinander gestellte Alternativen** B und C, damit die Entscheidung am Bild fällt |
| `D0003.dc.html` | **Board bedienen**, Fenster 1440×900, Rahmen 1100 (die Lesehilfe steht unter dem Fenster) — gefüllte Spaltenbahnen mit der Kartenform; die fünf Interactions I0010–I0014 als Zustände im selben Schirm, dazu drei Randfälle. **Am 2026-09-02 auf den gebauten Stand nachgezogen** |
| `canvas.json` | Layout des Canvas: der Flow oben, D0003 und D0002 in der Reihe darunter, Start in der Canvas-Ansicht |
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

**Zur Richtung des Nachführens.** Für den älteren Wireframe-Satz gilt die Regel aus
seiner [README.md](README.md): *„Diese Dateien werden nicht nachgefuehrt, wenn der
Code sie einholt. Weicht der Code ab, gehoert das in eine Anforderung."* Sie gilt
weiter und ist der Normalfall — der alte Satz bleibt unangetastet.

**Das Nachziehen von `D0003.dc.html` am 2026-09-02 ist die Ausnahme, und zwar eine
mit Beleg.** Die Abweichung ist keine stille Drift, sondern in
[`R00005`](../../Anforderungen/R00005-oberflaeche-nach-wireframes.md) unter
„Notizen" als *„Zwei abgelöste Zusagen aus der Gestaltungsarbeit nach der
Umsetzung"* dokumentiert: die entfallene Wortmarke samt ausgeblendetem
Navigationspunkt `Boards` und der gekürzte Vermerk „Grenze 20". Weil die
Anforderung die Änderung bereits trägt, bleibt für das Artboard nur die Wahl,
falsch zu bleiben oder nachzuziehen. Die `.dc.html`-Artboards sind außerdem, anders
als der alte Satz, **Vorlage für noch nicht gebaute Interactions** (hier I0010–I0014
für `R00006`) — eine Vorlage, die den gebauten Rahmen falsch zeigt, erzeugt beim
Bauen genau den Diff, den sie verhindern soll. **Daraus wird keine Regel:** ein
Artboard wird nur nachgezogen, wenn eine Anforderung die Abweichung bereits
dokumentiert. Fehlt dieser Beleg, gilt weiter der Normalfall — die Abweichung
gehört in eine Anforderung, nicht ins Bild.

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
| 2026-08-30 | Karten und Meldung auf welchem Grund? | **auf einer Bahn** Karte und Meldung `--color-bg` `#f5ead8`, Bahn `--color-surface` `#ebddc5`; auf dem Seitengrund bleibt es beim Token-Default `--color-surface` (so in D0002) | `.card` und `.meldung` tragen im Token-Sheet selbst `--color-surface` — auf einer Bahn derselben Farbe wären sie unsichtbar. Die Umkehrung ist die kleinste Abweichung, die die Schichtung rettet; alle übrigen Werte (Radius 16 px, `--shadow-sm`, Polsterung) bleiben unverändert. |
| 2026-08-30 | I0014 hat im alten Satz kein Bedienelement — welches? | `⋯`-Menü auf der Karte, ein einziger Eintrag „Archivieren" samt Erläuterung | Der Kontrakt verlangt zu jeder Interaction ein sichtbares Bedienelement. D0003 besitzt genau eine Kartenaktion; ein Menü mit einem Eintrag ist ehrlicher, als Einträge aus D0004 dazuzuerfinden. |
| 2026-08-30 | I0011 offen oder geschlossen zeigen? | beides — geschlossen (`+ Karte`) in drei Bahnen, offen in „Bereit" | Der gebaute Fuß reserviert genau eine Stelle. Ein Artboard, das nur den Ruhezustand zeigt, verschiebt die Frage nach Eingabefeld und Knöpfen in die Umsetzung. |
| 2026-08-30 | Welche Ränder? | leere Bahn („Prüfung", Zahl 0), zu langer Titel (BES-04, drei Zeilen), zurückgewiesene Anlage („Ohne Titel entsteht keine Karte") | Die drei Ränder, die beim Bauen von I0010/I0011 unweigerlich auftreten. Die Zurückweisung nimmt die Form von `.meldung-abweisung` aus `oberflaeche.css`. |
| 2026-08-30 | Wie unterscheiden sich Mensch und KI auf der Karte? | Avatar-Kreis 20 px; Mensch Olive (`--color-accent-2-200/-800`), KI Terrakotta (`--color-accent-200/-800`) | Der alte Satz trennt sie über Klassen ohne Farbfestlegung. Zwei Akzente gleicher Chroma und Helligkeit sagen genau das, was die Vision verlangt: gleichberechtigt, nicht gleich. |
| 2026-08-30 | Zeichen oder SVG für Symbole? | `✓` der Abschlussspalte bleibt Zeichen, alles Neue ist Inline-SVG | `Spaltenbahnen.razor` rendert `✓` als Zeichen — das Artboard bildet die gebaute Anatomie ab, statt still einen Diff zu erzeugen. Für Plus, Menü, Häkchenkasten, Uhr, Archivkiste und Chevron gilt die Regel des `design`-Skills: keine Dingbats. |
| 2026-08-30 | Beispieldaten | aus dem alten Satz übernommen; neu nur der Starttermin 01.08.2026 | WBS-31, BES-04, WBS-28, BUG-07, WBS-14, WBS-21, BUG-05, WBS-11, WBS-09, WBS-06, BES-01 stehen so in `wireframes.js`. Der gebaute Kopf zeigt Start **und** Ziel, der alte Satz nur ein Datum — der Starttermin ist damit die einzige gesetzte Zahl. |
| 2026-08-30 | Fünf Bahnen à 256 px passen nicht mittig in 1440 px | links bündig ab 26,4 px, Rest ist Überlauf | 5 × 16rem + 4 × `--space-3` = 1332,8 px. `.spaltenbahnen` trägt `overflow-x: auto`; ein zentriertes Board würde eine Ausrichtung behaupten, die der gebaute Code nicht hat. |
| 2026-08-30 | Titel des Artifacts | bleibt „KanbanC" | Der veröffentlichte Canvas heißt so seit `entwerfen`; ein Titelwechsel beim Republish würde ihn in Listen und Freigaben zu einem anderen Ding machen. |
| 2026-08-31 | **D0002** — wie kommen vier Interactions in ein Artboard? | Hauptzustand nach Variante A; I0008 als **zwei Alternativen nebeneinander** im selben Rahmen | Der Kontrakt verbietet ein zweites Artboard für Zustände desselben Dialogs. A ist die Liste und trägt I0006, I0007, I0009; I0008 ist keine Verfeinerung von A, sondern eine unentschiedene Alternative — nebeneinander gestellt lässt sie sich am Bild entscheiden statt aus einer Beschreibung. |
| 2026-08-31 | Wird B oder C gewählt? | **nicht gewählt** — beide gezeichnet, Vorschlag C in Frage 4 | Die Wahl hängt an der Einstiegsfrage des Screen-Flows und ist damit größer als D0002. Ein Lauf ohne Menschen zeichnet sie auf, statt sie nebenbei zu treffen. |
| 2026-08-31 | Dritte Farbe für die Art „abgebildet" | Neutral (`--color-neutral-100/-800`), Avatar mit gestricheltem Rand | Mensch (Olive) und Agent (Terrakotta) sind die zwei Akteure aus der Vision und tragen je einen Akzent. „Abgebildet" ist kein Akteur — eine dritte Akzentfarbe würde Gleichrangigkeit behaupten, die die Vision ausdrücklich verneint („niemand wählt deren Identität"). |
| 2026-08-31 | Beispieldaten: ein wählbarer Mensch reicht nicht | **Nina Barth** (Mensch, aktiv) gegenüber `wireframes.js` ergänzt | Von den vier Kontributoren des alten Satzes ist unter den Regeln von I0008/I0009 genau einer wählbar: Stefan. Agenten arbeiten über die API, Maria Lenz ist abgebildet, Jan R. stillgelegt. Eine Identitätswahl mit einer einzigen Kachel zeigt keine Wahl. Der alte Satz zeichnet Jan R. in Variante B noch als Kachel — das widerspricht I0009 und ist hier korrigiert. |
| 2026-08-31 | Wie wird I0007 sichtbar? | Stiftsymbol je Zeile **und** eine aufgeklappte Bearbeitungszeile an „Codex-Agent" | Ein Symbol allein zeigt, dass es geht, nicht wie. Die aufgeklappte Zeile beantwortet, was beim Bearbeiten änderbar ist (Name und Art) und wo gesichert wird — ohne einen zweiten Schirm zu erfinden, den die WBS nicht kennt. |
| 2026-08-31 | Welcher Rand für D0002? | zurückgewiesene Anlage: „Ohne Namen entsteht kein Kontributor" | Der eine Rand, der beim Bauen von I0006 unweigerlich auftritt. Form und Wortlaut folgen `.meldung-abweisung` und dem in B0050 gesetzten Muster: Zurückweisung, nicht Serverfehler. |
| 2026-08-31 | Wort für das Ende der Stilllegung | „zurückholen" | I0009 nennt nur das Stilllegen. „Reaktivieren" (so das `↺` des alten Satzes) ist Fremdwort und Systemsprache; C06 verlangt kontexteindeutige deutsche Domänensprache. |
| 2026-08-31 | Alte Schirme jetzt auf die Dialogs umschneiden? | nein — nur die **Zuordnung** wird verbindlich festgeschrieben | Rolling Wave: ein Dialog wird gezeichnet, wenn er dran ist. Sieben Dialogs vorab auszudetaillieren hätte dasselbe Problem wie eine WBS mit geratenen Bubbles. Die Zuordnung kostet nichts und verhindert genau den Fehler, den Frage 1 benannt hat: dass ein späterer Lauf den Schirm für den Dialog hält. |
| 2026-08-31 | Wo liegt `D0002.dc.html` auf dem Canvas? | zweite Reihe, rechts neben D0003 (`x` 1560, `y` 1300) | 120 px Abstand zu D0003, 140 px zur Flow-Reihe darüber — beides über dem Mindestabstand. Die Dialog-Artboards bilden damit eine eigene Reihe unter der Landkarte; weitere wachsen nach rechts. |
| 2026-08-31 | Typoskala in den Dialog-Artboards | h1 **28 px** statt 42 px; sonst die Leiter unverändert (h2 32, h3 25, h4 20) | Die Skala ist für eine Textseite gerechnet; eine Seitenüberschrift über einer dichten Arbeitsfläche in 42 px erdrückt sie. 28 px ist in `D0003.dc.html` bereits gesetzt (Boardname) — D0002 folgt derselben Wahl, statt eine zweite zu treffen. Gilt nur für Dialog-Artboards; `Main.dc.html` bleibt bei seinem eigenen Eintrag. |
| 2026-08-31 | Kachel-Avatare der Identitätswahl | **48 px** statt der gesetzten 20 px | 20 px ist der Durchmesser in Zeile und Karte. Variante B zeigt drei Kacheln über die halbe Schirmbreite; derselbe Kreis wäre dort kein Bedienelement mehr, sondern ein Punkt. Alle übrigen Avatare im Artboard bleiben bei 20 px. |
| 2026-08-31 | Nachgezogen nach dem Gegenlesen | Radius 12 → 16 px · Platzhalterhöhe 34 → 35.2 px (`--space-8`) · Symbolfläche 28 → 36 px (`.btn-symbol`) · Deckkraft 0.62/0.55 → 0.45 (`.btn:disabled`) · Strichstärke 2.4 → 2.2 · Rahmenhöhe 1520 → 1560 px | Sieben freie Zahlen, die weder ein Token trafen noch in `D0003.dc.html` präzedenziert waren. Der Sinn dieses Artboards ist, exakte Werte vorzugeben — eine gerundete Zahl darin wäre eine stille Abweichung, die die Umsetzung erbt. Die 36-px-Flächen strecken fünf Tabellenzeilen, daher der höhere Rahmen. |
| 2026-09-03 | **D0003** — wie werden die Ablageorte beim Ziehen gezeigt? | **Einfügelinie statt Ablagekästen**: 2 px im Akzentton zwischen zwei Karten, ohne Beschriftung; die Restfläche unter der letzten Karte nimmt ganzflächig an, eine leere Bahn ebenso | Die gezeichneten Kästen (84 px, gestrichelt, „hier ablegen · Position N“) schoben beim Zug alle Karten auseinander — das Board sprang, statt ruhig zu bleiben. Eine Einfügelinie sagt dasselbe auf 2 px. Die Positionsnummer entfällt ersatzlos: eine Linie trägt keinen Text, und die Stelle ist am Bild ablesbar. Vom Menschen beauftragt, nicht abgeleitet. |
| 2026-09-03 | Wohin fällt die Karte, wenn man über einer Karte loslässt? | obere Hälfte → davor, untere Hälfte → dahinter | Die verbreitete Erwartung (Trello, Kanbanflow). Die Alternative „immer davor“ macht das Anhängen ans Ende einer Bahn unerreichbar, ohne die Restfläche zu treffen. |
| 2026-08-31 | Stillgelegte Zeile ist 13 px flacher als die aktiven | `min-height: 36px` auf der Pflege-Zelle | Die aktiven Zeilen bekommen ihre Höhe aus den 36-px-Symbolflächen; die stillgelegte trägt statt dessen den Textknopf „zurückholen". Eine Tabelle lebt vom gleichmäßigen Zeilenrhythmus — der Bruch wäre als Absicht lesbar gewesen, ist aber keine. |
| 2026-09-02 | **D0003** — Artboard nachziehen oder Abweichung melden? | **nachziehen** | Die sechs Änderungen sind über `R00005` „Notizen" belegt; das Artboard ist zugleich die Vorlage für `R00006`. Siehe den Absatz „Zur Richtung des Nachführens" über dieser Tabelle. |
| 2026-09-02 | Wohin mit der Lesehilfe, wenn das Board die volle Fensterhöhe füllt? | **unter das Fenster**, außerhalb der 1440×900 | Der gebaute Schirm hat genau zwei Elemente übereinander — Navigationszeile und Board. Läge die Lesehilfe weiter im Fenster, zeigte das Artboard eine dritte Zeile, die es nicht gibt. Sie gehört ohnehin dem Wireframe, nicht dem Schirm. |
| 2026-09-02 | Rahmenhöhe in `canvas.json` | 900 → **1100** | Gemessen, nicht geschätzt (Chromium, Google-Fonts-Fassung derselben Schriften): Fenster 900 px, Lesehilfe 140,6 px, zusammen 1040,6 px. 1100 gibt 5,7 % Reserve; der Überschuss trägt die Grundfarbe des Lesehilfe-Bandes `#ebddc5` und ist deshalb unsichtbar. Das Artboard wird durch den entfallenen Boardkopf **nicht** flacher, weil die Lesehilfe aus dem Fenster herausrückt — die Boardfläche selbst gewinnt rund 100 px. |
| 2026-09-02 | Kartenzahl im Bahnenkopf: gefüllt lassen, obwohl der Code die Stelle leer reserviert? | **gefüllt** | `R00005` stellt I0004 ausdrücklich out of scope („die Bahnen bekommen nur die Stellen"); das Artboard zeigt aber die Zielform, wie schon bei den vier Kartenelementen fremder Dialogs. Die Lesehilfe benennt es jetzt ausdrücklich als später gebaut. Eine leere Stelle zu zeichnen hieße, die Zielform zu verschweigen. |
| 2026-09-02 | Fuß der Abschlussspalte: `+ Karte` oder „Ältere nachladen"? | **beides** — `+ Karte` im Fuß, „Ältere nachladen" ans Ende der Bahnenfläche | Der gebaute Fuß trägt in **jeder** Bahn die Kartenanlage; ihn in der Abschlussspalte durch etwas anderes zu ersetzen wäre eine erfundene Ausnahme. Das Bedienelement für I0013 bleibt trotzdem sichtbar, wie der Kontrakt es verlangt — es sitzt unter dem Nachlade-Hinweis, wo es hingehört. |
| 2026-09-02 | Radius des `⋯`-Menüs, das der Code nicht kennt | `--radius-xs` 4 px | Karten sind jetzt fast eckig. Ein 16-px-Menü unmittelbar auf einer 4-px-Karte läse sich als zweites Formensystem. Die Zurückweisung dagegen behält 16 px, weil `.meldung` in `oberflaeche.css` gebaut ist — dort wird nichts angeglichen, was der Code festlegt. |
| 2026-09-02 | Eingabefeld der Kartenanlage steht auf der Bahn in derselben Farbe wie die Bahn | **so gezeichnet**, nicht korrigiert | `.input` trägt `background: var(--color-surface)`, und die Bahn ist `--color-surface`: das Feld zeigt sich nur als umrandete Pille. `.karte` und `.meldung` sind für diesen Fall auf `--color-bg` gedreht worden, `.input` nicht. Ob das Absicht ist, entscheidet nicht das Artboard — es zeigt, was gebaut ist. Als Befund unter Offene Fragen 11. |

## Zuordnung Schirm → Dialog

**Verbindlich für jeden künftigen `/wireframe verfeinern`-Lauf.** Der ältere Satz
(`kanbanc-wireframes.html`, `wireframes.js`, `README.md`) zeichnet **acht Schirme**,
die quer zu den **neun Dialogs** geschnitten sind. Diese Tabelle sagt je Dialog,
woraus er schöpft — sie ändert am alten Satz nichts; dessen `README.md` verweist
nur hierher.

Lesart: **Speisende Schirme** nennt den Schirm und, wo es nötig ist, den Teil
davon. **Variante** ist die Wahl aus dem `README.md` des alten Satzes — `offen`
heißt, sie fällt bei der Interaction, die den Schirm baut. **Ohne Deckung** nennt
Interactions, die kein Schirm zeichnet; das ist ein **Befund**, keine Lücke zum
Auffüllen.

| Dialog | Speisende Schirme (alt) | Variante | Ohne Deckung |
| --- | --- | --- | --- |
| **D0001** Boards führen | „Start / Board-Übersicht" (ganz) · „Board anlegen & gestalten" (ganz) · „Board" (nur der Boardkopf: Kartenzahl-Schalter, Klassenfilter) | Start **A** · Anlegen **A**, Layout-Modus **B** | **I0038 / I0039** — Export und Import stehen nur als Knopf im Fuß von „Start A" und im Board-Abschnitt von „Gestalten B". Dateiwahl, Vorschau und Ergebnis sind nirgends gezeichnet. |
| **D0002** Kontributoren führen | „Kontributoren & Identität" (ganz) | Liste **A** gesetzt · Identitätswahl **B oder C offen** (Frage 4) | — vollständig; seit 2026-08-31 als `D0002.dc.html` gezeichnet |
| **D0003** Board bedienen | „Board" (Bahnen, Karten, Abschlussspalte) | Bahnen **A/B** — dort deckungsgleich; **C** wäre eine spätere Zweitansicht | **I0014** Karte archivieren — der alte Satz kennt nur *Board* archivieren (I0005). Im Artboard als ⋯-Menü ergänzt, siehe Frage 6. |
| **D0004** Karteninhalt pflegen | „Kartendetail" (ganz) | **offen** — A, B oder C, Wahl bei I0015–I0019 | — |
| **D0005** Karten-Klassen | „Board anlegen & gestalten" **B**, Abschnitt *Klassen* (I0020) · „Kartendetail" und die Kartenform in „Board" (I0021, Nummer auf der Karte) | Klassen-Teil aus **B** gesetzt | **I0022** — reine API-Zusage, absichtlich ohne Schirm; die Oberflächenentsprechung ist der Klassenfilter im Boardkopf |
| **D0006** Zeiterfassung | „Zeiten je Kontributor" (ganz) · „Kartendetail" (I0026, Zeiten der Karte) · „Board A" rechte Spur und „Start B" Banner (I0027, laufende Timer) | **offen** — A oder B, Wahl bei I0023–I0027 | — |
| **D0007** Live-Aktualisierung | **kein eigener Schirm** — nur als Merkmal *innerhalb* von „Board": Ereignisspur rechts (**A**) oder Laufband oben (**B**), dazu die Marke „● live" in der Kopfzeile und der Live-Punkt in „Start B" | Spur oder Laufband entscheidet **I0028**, nicht vorher | **I0029** Aufschließen nach Verbindungsabbruch — weder Zustand noch Meldung gezeichnet |
| **D0008** WBS-Import | „WBS-Import" (ganz) | **offen** — A oder B, Wahl bei I0030–I0032 | — |
| **D0009** Auswertungen | „Auswertungen" (ganz) | **offen** — A oder B, Wahl bei I0033–I0037 | **I0037** — reine API-Zusage, absichtlich ohne Schirm |

**Ein Dialog ist von keinem Schirm gedeckt: D0007.** Live-Aktualisierung ist im
alten Satz kein Bildschirm, sondern eine Eigenschaft zweier anderer. Wer D0007
verfeinert, findet dort die *Darstellung* laufender Ereignisse vor (I0028), aber
nichts zum Verbindungsabbruch (I0029) — dieser Zustand ist zu entwerfen, nicht
abzuschreiben. Der Kontrakt bleibt trotzdem gewahrt: D0007 steht als Kasten im
Screen-Flow, weil die WBS ihn als Dialog führt.

**Drei weitere Stellen ohne Vorlage** — I0014 (Karte archivieren), I0029
(Aufschließen), I0038/I0039 (Board exportieren und importieren, nur Einstiegsknopf).
Sie werden hier nicht nachgezeichnet: der alte Satz bleibt, wie er ist, und der
Entwurf gehört in den `verfeinern`-Lauf des jeweiligen Dialogs. **I0022 und I0037**
sind absichtlich ohne Schirm — sie sind die API-Zusage der Vision und haben keine
Oberfläche.

Die Gegenrichtung, Schirm → Dialogs, in Kurzform:

| Schirm (alt) | Dialogs (WBS) |
| --- | --- |
| Start / Board-Übersicht | D0001 · D0006 (I0027 in Variante B) · D0007 (Live-Punkt in B) |
| Board anlegen & gestalten | D0001 · D0005 (Klassen-Teil in B) |
| Board | D0001 (I0004, Klassenfilter) · D0003 · D0007 |
| Kartendetail | D0004 · D0005 (Nummer) · D0006 (Zeiten) |
| WBS-Import | D0008 |
| Auswertungen | D0009 |
| Zeiten je Kontributor | D0006 |
| Kontributoren & Identität | D0002 |

## Offene Fragen

1. ~~**Der ältere Wireframe-Satz ist quer zu den Dialogs geschnitten.**~~
   **Erledigt am 2026-08-31.** Der Satz bleibt unangetastet und ist über `R00005`
   weiter verbindlich; die Zuordnung steht jetzt als
   [Zuordnung Schirm → Dialog](#zuordnung-schirm--dialog) — je Dialog, welcher
   Schirm ihn speist, welche Variante gesetzt ist und was ohne Deckung bleibt.
   Umgeschnitten wird nicht auf Vorrat, sondern je Dialog beim
   `verfeinern`-Lauf. Was aus der Prüfung als **Befund** übrig bleibt: **D0007**
   hat als einziger Dialog keinen eigenen Schirm, und I0014, I0029 sowie
   I0038/I0039 haben keine Vorlage.

2. **`art: mockup` ist gesetzt, nicht abgestimmt.** Für D0003 (Karte verschieben)
   und D0001 (Layout-Modus) könnte ein klickbarer Prototyp die Bedienbarkeit
   klären, die eine statische Ansicht offen lässt.

3. **Die zwei neuen Ampelfarben sind gesetzt, nicht abgestimmt.** Sie leben nur im
   Wireframe; soll die Anwendung selbst je Statusfarben brauchen, gehören sie in
   `gestaltung.css` und damit in eine Anforderung.

4. **Wo wird die Identität gewählt — Variante B oder C? Und ist D0001 damit noch
   der Einstieg?** (Geschärft am 2026-08-31; beide Varianten stehen nebeneinander
   in `D0002.dc.html`.) Beide erfüllen das Fertig-Kriterium von I0008 — „beim
   Öffnen der Oberfläche wählt man, wer man ist; die Wahl überlebt einen Reload"
   (localStorage). Sie unterscheiden sich in genau einem Punkt, und der
   beantwortet zugleich die Einstiegsfrage des Screen-Flows:

   - **B — ganzflächiger Vorschirm.** Vor dem ersten Board steht „Wer bist du?".
     Dann ist **I0008 der Einstieg**, D0001 rückt dahinter, und `Main.dc.html`
     braucht eine Kante I0008 → D0001; die Markierung `Einstieg · /boards` wandert.
   - **C — Popover an der Kopfzeile.** Kein Vorschirm; **D0001 bleibt der
     Einstieg**, der Identitätsplatz der Kopfzeile trägt die Wahl. Der Screen-Flow
     bleibt, wie er ist.

   **Vorschlag: C** — mit dem Zwang aus B als Ergänzung, nicht als eigener Schirm.

   1. *Der Platz ist gebaut.* `Components/Layout/Kopfzeile.razor` trägt
      `kopfzeile-identitaet` mit dem Text „nicht gewählt" (B0056). C füllt eine
      vorhandene Stelle; B fügt einen Bildschirm hinzu, den die WBS nicht kennt —
      und ein Artboard ohne Knoten wäre gegen den Kontrakt.
   2. *Ein Rechner, mehrere Menschen.* Die Vision nennt „gelegentlich weitere
      Menschen im lokalen Netz" und Full-Trust ohne Anmeldung. Eine einmalige
      Wahl je Browser ist genau dann falsch, wenn sich zwei Menschen einen
      Rechner teilen; bei C ist das Umschalten ein Klick dort, wo man steht.
   3. *Der Preis trifft den häufigen Fall.* Den Vorschirm bezahlt auch, wer nur
      nachsehen will, wie das Board steht — der mit Abstand häufigste Aufruf.
      Das widerspricht der Haltung „Kanbanflow-dicht".
   4. *Der Wortlaut der Vision ist mit C erfüllbar*, wenn „nicht gewählt"
      sichtbar stehen bleibt und spätestens der Timer (I0023) die Wahl erzwingt,
      bevor er läuft. Das ist der Zwang aus B ohne dessen Preis — und der
      einzige Punkt, an dem C nachgeschärft werden muss.

   **Gegenrede, die ernst zu nehmen ist:** C lässt zu, versehentlich als „nicht
   gewählt" oder unter fremdem Namen zu arbeiten — in einem Board, dessen Zweck
   die verlässliche Zuordnung von Karten und Zeiten ist, ist das kein kleiner
   Fehler. Wer das ausschließen will, nimmt B; dann aber als bewusste Antwort auf
   die Einstiegsfrage, mit der Kante im Screen-Flow, nicht nebenbei.

   Entschieden wird bei I0008.

5. ~~**Das WBS-Frontmatter kennt den Canvas noch nicht.**~~
   **Erledigt.** `wireframes: Dokumentation/Wireframes/` steht im Frontmatter der
   WBS-Datei; `/anforderung`, `/implementierung` und `/github` finden den Canvas.
   Gesetzt hat es `/planung aendern` — `/wireframe` schreibt nie in die WBS.

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

9. **Der Kopf der Abschlussspalte ist einen Pixel höher als die vier anderen.**
   Gemessen am gebauten Stand: 47 px gegenüber 46 px, die Trennlinie sitzt dort auf
   96,38 statt 95,38 px. Ursache ist der Vermerk „Grenze 20" (11 px) neben der
   Bezeichnung (15 px) in `.spaltenbahn-anzeige`, das mit `align-items: baseline`
   ausgerichtet ist — nicht das Häkchen und nicht die Kartenzahl (durch Wegnehmen
   je einzeln nachgewiesen). Die feste `line-height: var(--space-6)` auf dem Kopf
   fängt den äußeren Flex-Container ab, den inneren nicht. Das Artboard zeichnet
   den Rest, statt ihn wegzuglätten. **Kein Auftrag am Code** — wer die letzte
   Pixelzeile will, gibt `.spaltenbahn-anzeige` eine feste Höhe oder richtet es
   auf `center` statt `baseline` aus; das gehört in eine Anforderung.

10. **Zwei Regeln beschreiben denselben Kopfzeilen-Inhalt, und die aus der
    Kopfzeile gewinnt.** `Board.razor.css` setzt `.board-name` auf 18 px und
    `.board-zurueck` auf 16 px in `--color-accent`; `Kopfzeile.razor.css` setzt
    dieselben Elemente über `::deep` auf 17 px und den Rückweg auf 15 px in
    `--color-text` bei 60 %. Die `::deep`-Regel ist spezifischer
    (`.kopfzeile[b-…] .board-name` gegen `.board-name[b-…]`), sie gewinnt in
    beiden Fällen. Das Artboard zeichnet den Gewinner — also einen gedämpften
    Rückweg, keinen terrakottafarbenen. Die unterlegenen Werte in
    `Board.razor.css` sind tot; das aufzuräumen ist Sache einer Anforderung.

11. **Das Eingabefeld der Kartenanlage ist auf der Bahn fast unsichtbar.** Siehe
    die Entscheidung vom 2026-09-02: `.input` bleibt `--color-surface` auf einer
    Bahn derselben Farbe, während `.karte` und `.meldung` für genau diesen Fall
    auf `--color-bg` gedreht wurden. Fällt beim Bauen von `R00006` auf oder nie.

