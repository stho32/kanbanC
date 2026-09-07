---
application: kanbanc
wbs: Dokumentation/Planung/kanbanc.md
betrieb: lokal
canvas: https://claude.ai/code/artifact/b61e3007-056d-44e0-9cf5-7350c22f858a
seed: .claude/wireframes/kanbanc.html
art: mockup
richtung: aus dem Bestand — gestaltung.css (Caprasimo/Figtree, warmes Sandklima, runde Formen)
stand: 2026-09-07
zurueckgeholt: 2026-09-03
---

# Wireframes — KanbanC

Der Canvas trägt neun Artboards: `Main.dc.html`, den Screen-Flow über alle neun
Dialogs, sowie `D0001.dc.html` bis `D0008.dc.html`, die acht ausdetaillierten
Dialogs. Der
übrige Dialog bleibt ein Kasten im Flow; sein Detail-Artboard entsteht mit
`/wireframe verfeinern <dialog>`, wenn der Dialog dran ist (Rolling Wave). Woraus
ein solcher Lauf schöpft, sagt die
[Zuordnung Schirm → Dialog](#zuordnung-schirm--dialog).

Reife je Dialog wird aus dem Dateibestand gerechnet: `D0001` bis `D0008` stehen auf
`wireframe`, der übrige (`D0009`) auf `flow`.

**`D0007` ist der eine Dialog ohne eigenen Schirm** und trotzdem ein Artboard: sein
Gegenstand ist ein *Verhalten an vorhandenen Schirmen*. Gezeichnet sind deshalb
Zustände und Übergänge am Board, an der Kartenseite und in Zone 3 der Kopfzeile —
kein neuer Bildschirm.

| Datei | Was |
| --- | --- |
| `Main.dc.html` | Screen-Flow: neun Dialog-Kästen, dreizehn beschriftete Übergänge, Ampel je Dialog |
| `D0001.dc.html` | **Boards führen**, Rahmen 1440×3800 (gemessen 3625,3) — fünf Zustände untereinander: Board-Übersicht als echtes Fenster 1440×900, Anlegeformular, Layout-Modus, Kartenzahl-Schalter (I0004, der laufende Slice) und Board pflegen (I0005, I0038, I0039), dazu drei Ränder. Gebaut sind Übersicht, Anlegen, Layout-Modus und die Ränder; I0004, I0005, I0038 und I0039 sind Zielform und tragen an Ort und Stelle einen Vermerk |
| `D0002.dc.html` | **Kontributoren führen**, 1440×1560 — die Liste mit den drei Arten als Hauptzustand (I0006, I0007, I0009); darunter die Identitätswahl I0008 als **zwei nebeneinander gestellte Alternativen** B und C, damit die Entscheidung am Bild fällt |
| `D0003.dc.html` | **Board bedienen**, Fenster 1440×900, Rahmen 1100 (die Lesehilfe steht unter dem Fenster) — gefüllte Spaltenbahnen mit der Kartenform; die fünf Interactions I0010–I0014 als Zustände im selben Schirm, dazu drei Randfälle. **Am 2026-09-02 auf den gebauten Stand nachgezogen** |
| `D0004.dc.html` | **Karteninhalt pflegen**, Rahmen 1440×2960 (gemessen 2815,2) — die Karte als **eigene Seite** (`/karten/14`, Variante C): Hauptzustand als echtes Fenster 1440×900, dazu Verantwortlichenwahl, Etiketten und Farbe, die frisch angelegte Karte als Leerzustand, drei Ränder und der Einstieg vom Board. Alle fünf Interactions I0015–I0019 sind sichtbar; nichts davon ist gebaut, das ganze Artboard ist Zielform |
| `D0005.dc.html` | **Karten-Klassen**, Rahmen 1440×1980 (gemessen 1877,5) — der Klassenbereich sitzt **im Layout-Modus des Boards**, unter der Zeile für die neue Spalte: Hauptzustand mit Liste und Anlegezeile (I0020), Leerzustand, drei Ränder, die Zuordnung an der Karte (I0021) und der Abruf als API-Aufruf (I0022). Alle drei Interactions sind sichtbar; nichts davon ist gebaut, das ganze Artboard ist Zielform |
| `D0006.dc.html` | **Zeiterfassung**, Rahmen 1440×3480 (gemessen 3304,1) — die Zeiterfassung sitzt **auf der Kartenseite**, in dem Kasten, den `D0004.dc.html` dafür freihält: Hauptzustand als echtes Fenster 1440×900, der Zeitenblock in seinen übrigen Fassungen (mein Timer läuft, leer, Anatomie eines Eintrags), die Karte in der Bahn in drei Fassungen (I0023), die laufenden Timer in **Zone 3 der Kopfzeile** (I0027), Nachtragen und Ändern (I0025) und drei Ränder. Alle fünf Interactions I0023–I0027 sind sichtbar; nichts davon ist gebaut, das ganze Artboard ist Zielform |
| `D0007.dc.html` | **Live-Aktualisierung**, Rahmen 1440×4420 (gemessen 4200,7) — **kein eigener Schirm, sondern Zustände an vorhandenen**: die fremde Bewegung am Board als Fenster 1440×900 (I0028), die fremde Änderung an der offenen Kartenseite als zweites Fenster mit der Trennung „zieht nach“ / „wird angeboten“, die Anatomie der Einflugmarke in drei Fassungen, die Gleichstellung von Browser und API samt markierter Lücke im Rückweg, die drei Stufen des Verbindungsabbruchs (I0029) und das Aufschließen, dazu drei Ränder. Beide Interactions I0028 und I0029 sind sichtbar; nichts davon ist gebaut, das ganze Artboard ist Zielform |
| `D0008.dc.html` | **WBS-Import**, Rahmen 1440×5100 (gemessen 4844,3) — der Einstieg im **Layout-Modus des Boards** unter der Klassenpflege, dann drei Schritte auf eigener Adresse `/boards/2/import`: Datei (Fenster 1440×900, die gebaute Ablegefläche aus R00024), Vorschau (Fenster 1440×900, Baum links / Wirkung rechts, mit der **Schnittebene** als dem einen Regler), Bericht (I0032). Dazu die **Anatomie Baum → Board** Feld für Feld, der zweite Lauf mit vier Fächern und der Trennung „was die Datei nachzieht / was das Board behält" (I0031) und drei Ränder. Alle drei Interactions I0030–I0032 sind sichtbar; nichts davon ist gebaut, das ganze Artboard ist Zielform |
| `canvas.json` | Layout des Canvas: der Flow oben, D0003, D0002, D0001, D0004, D0005, D0006, D0007 und D0008 in der Reihe darunter, Start in der Canvas-Ansicht |
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

**Zur Betriebsart.** Der Wechsel von `canvas` nach `lokal` wurde mit dem Import am
2026-09-03 vollzogen — die Artboards liegen seither versioniert im Ordner, geändert
wird auf Ansage. Nur das Feld `betrieb:` blieb dabei liegen; es ist am 2026-09-04
auf `lokal` nachgetragen worden. Damit ist `zurueckholen` gegenstandslos und der
Canvas wird nicht mehr angefasst; die Adresse in `canvas:` bleibt als
Herkunftsnachweis stehen.

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
| 2026-09-04 | **D0001** — wo sitzt der Kartenzahl-Schalter (I0004)? | **Zone 3 der Navigationszeile**, links vom Layout-Schalter | Der ältere Satz zeichnet ihn im **Boardkopf** („Board A": Knopf „Kartenzahl anzeigen"); mit `R00005` ist der Boardkopf als eigene Zeile entfallen und seine Bedienelemente sind nach Zone 3 gewandert (`kopfzeile-bedienung`, heute der Layout-Schalter). Der Schalter folgt seinem Platz. Die zweite Fundstelle des alten Satzes — „Gestalten B", Zeile *Board*: „Kartenzahl im Spaltenkopf: an / aus" — versteckt eine Ansichtsfrage im Layout-Modus, den man zum Umschalten erst betreten müsste. Links vom Layout-Schalter, weil man erst ansieht und dann umbaut. |
| 2026-09-04 | Welche Form für den Schalter? | **`.kontrollfeld`** (Häkchenkasten mit Beschriftung „Kartenzahl"), nicht Knopf und nicht `.seg` | Die Kartenzahl ist eine **Einstellung, die stehen bleibt**, keine Handlung — ein Knopf sagt das Falsche. `.kontrollfeld` ist gebaut (`oberflaeche.css`, benutzt in `Spaltenpflege.razor` für „Abschlussspalte") und mit 16 px Höhe sprengt es die 35,2 px der Navigationszeile nicht; ein `.seg` mit „an / aus" misst 36,2 px und würde die gebaute Zeilenhöhe verändern. |
| 2026-09-04 | Wie kommen neun Interactions in ein Artboard? | **fünf Zustände untereinander plus eine Randzeile**, nur Zustand 1 als echtes Fenster 1440×900 | Der Kontrakt verbietet ein zweites Artboard für Zustände desselben Dialogs. Ein Fenster in voller Höhe je Zustand hätte den Rahmen auf über 5000 px getrieben, ohne mehr zu zeigen: Anlegen, Layout-Modus und Kartenzahl brauchen keinen leeren Seitenfuß. Der Hauptzustand bleibt maßstäblich, die übrigen sind Ausschnitte und sagen das in ihrer Beschriftung. |
| 2026-09-04 | Bedienelement für I0005 und I0038 auf der Übersicht | **⋯-Menü auf der Kachel** mit Umbenennen, Archivieren, Exportieren | Dieselbe Antwort wie in D0003 für I0014 — eine Kachel hat sonst keine Stelle für Kartenaktionen, und drei Knöpfe je Kachel erdrücken eine Übersicht, die auf fünf Spalten dicht steht. Das Umbenennen selbst passiert **in der Kachel**, wie D0002 die Kontributorzeile aufklappt, statt auf einem Schirm, den die WBS nicht kennt. |
| 2026-09-04 | I0038 / I0039 — Ablauf zeichnen oder Lücke markieren? | **Lücke markieren** | Der ältere Satz zeigt nur den Einstiegsknopf (Zuordnung, Spalte „Ohne Deckung"). Den Ablauf vom WBS-Import (D0008) abzuschreiben wäre falsch: dessen drei Schritte überführen Knoten in Karten, während I0039 ein Board als Ganzes wiederherstellt. Ein gestrichelter Kasten mit Grund ist ehrlicher als eine erfundene Vorschau, die die Umsetzung dann erbt. |
| 2026-09-04 | Termine im Artboard: `30.09.2026` wie in D0003 oder `2026-09-30`? | **ISO**, `2026-09-30` | `Terminformatierer.AlsText` schreibt `yyyy-MM-dd`, und `InputDate` liest es so. D0003 zeigt an dieser Stelle das deutsche Format — ein Befund, der in Offene Frage 12 steht; D0001 wiederholt ihn nicht. |
| 2026-09-04 | Standardspalten in der Vorschau des Anlegeformulars | **Zu erledigen · In Arbeit · Erledigt** | So stehen sie in `Boards.razor` (`Standardspalten`). Der ältere Satz nennt „Rückstand · In Arbeit · Fertig ✓" — das sind die Beispielspalten des Boards „KanbanC — Release 2", nicht die Vorlage, die der Endpunkt anlegt. |
| 2026-09-05 | **D0004** — Schublade (A), Modal (B) oder eigene Seite (C)? | **C — eigene Seite `/karten/14`** | Vier Gründe, keiner davon Geschmack. (1) Die API hat je Karte eine Adresse; C gibt der Oberfläche dieselbe Form — „Was ein Mensch klicken kann, kann ein Agent aufrufen". (2) Das Fertig-Kriterium von I0015 sagt „nach Reload da"; nur bei C landet ein Reload wieder auf derselben Karte, bei A und B auf dem Board. (3) D0004 trägt fünf Interactions und speist später D0005 und D0006 — genau der Platz, den **A** laut eigener Annotation nicht hat („wenig Platz für Beschreibung, Kommentare und Anhänge gleichzeitig"). (4) Die gebaute Kopfzeile führt Zone 1 als „die offene Seite" mit Rückpfeil; eine Seite füllt sie, eine Schublade nicht. **B** verworfen, weil es das Board verdeckt und Live-Änderungen dahinter unbemerkt bleiben — das steht gegen „Live überall" — und keine teilbare Adresse hat. Die Schwäche von C (Kontextwechsel weg vom Board) bleibt und ist gezeichnet: Brotkrumen plus der gebaute Rückpfeil. |
| 2026-09-05 | Was ist ein **Etikett**? Nirgends definiert — kein Knoten, keine Tabelle, kein Contract | **freie Textmarke an der Karte**, beim Tippen vervollständigt aus dem Bestand des Boards; kein verwalteter Etikettensatz | Ein verwalteter Satz bräuchte einen Pflegeschirm, und den kennt die WBS nicht — ein Artboard ohne Knoten wäre gegen den Kontrakt, und ein zusätzlicher Dialog ist Sache von `/planung`, nicht von `/wireframe`. Der praktische Nutzen (eine Schreibweise statt fünf) kommt aus der Vervollständigung. Das Artboard zeigt die Kehrseite mit: „Refactoring" und „Refaktorierung" stehen nebeneinander in der Liste. |
| 2026-09-05 | Etikett und Farbe — ein Ding oder zwei? | **zwei** | Das Fertig-Kriterium von I0015 nennt „Farbe **und** Etiketten" einzeln. Also trägt das Etikett Worte und keine Farbe, und die Farbe gehört der ganzen Karte. Fünf Werte, alle aus dem Token-Sheet: ohne, `--color-neutral-200`, `--color-accent-200`, `--color-accent-2-200`, `--color-neutral-300`. Eine sechste Farbe hätte das Sheet nicht — die gehört in `gestaltung.css` und damit in eine Anforderung (dieselbe Lage wie bei den Ampelfarben, Frage 3). |
| 2026-09-05 | Wer ist als **Verantwortlicher** wählbar? | Stillgelegte **nicht**, Abgebildete **doch**; „niemand" ist ein Eintrag der Liste | Die Regel „Stillgelegte nicht wählbar" ist dieselbe wie in der Identitätswahl und löst die zweite Hälfte des Fertig-Kriteriums von I0009 ein, die dort nicht prüfbar war („bleibt an alten Karten sichtbar") — der Rand zeigt genau das. Die Regel „Abgebildete nicht wählbar" gilt hier ausdrücklich **nicht**: sie können sich nicht selbst anmelden, aber jemand kann für sie eine Karte führen; genau dafür gibt es die Art. „Niemand" ist kein Zurücknehmen, sondern der Normalfall nach I0011. |
| 2026-09-05 | Adresse der Kartenseite | **`/karten/14`** über die `KarteId`, nicht `/karten/WBS-14` | Der ältere Satz schreibt `/karten/WBS-14`; die sprechende Nummer kommt aber erst mit der Klasse (I0021, D0005, rot). Die Route folgt dem gebauten Muster `/boards/{BoardId:long}`. Kommt D0005, kann die Adresse sprechend werden — das steht als Vermerk im gestrichelten Kasten „Klasse und Nummer". |
| 2026-09-05 | Wie kommt man in die Kartenseite? | **Titelklick auf der Karte** und ein zweiter Eintrag „Details öffnen" im ⋯-Menü | Die WBS-Notiz zu I0014 hat es zugesagt: „Kommt D0004, bekommt das Kartendetail denselben Eintrag zusätzlich; das Menü bleibt" (`kanbanc.md:383`). Gezeichnet ist nur der hinzukommende Eintrag; das Menü selbst bleibt, wie D0003 es zeigt. Umgekehrt trägt die Kartenseite „Archivieren" als benannte Fremdhandlung aus I0014. |
| 2026-09-05 | Titel: Überschrift der Seite oder Feld im Eigenschaftenblock? | **Überschrift**, 32 px, mit Stift daneben | Der Titel ist die Identität der Karte und steht dort, wo man ihn liest — geändert wird er an derselben Stelle. Die Navigationszeile trägt statt dessen den **Boardnamen**, weil ihr Rückpfeil dorthin führt; ein zweites Mal denselben Titel zu zeigen wäre Wiederholung ohne Nutzen. |
| 2026-09-05 | Rahmenhöhe und Platz von `D0004` auf dem Canvas | **1440×2960** bei `x` 4680, `y` 1300 | Gemessen, nicht geschätzt (Chromium, Google-Fonts-Fassung derselben Schriften): 2815,2 px. 2960 gibt 5,1 % Reserve, der Überschuss trägt die Grundfarbe `#ebddc5`. Der Platz setzt die Dialogreihe nach rechts fort, 120 px hinter `D0001` — Positionen der übrigen Artboards bleiben unverändert. |
| 2026-09-02 | Eingabefeld der Kartenanlage steht auf der Bahn in derselben Farbe wie die Bahn | **so gezeichnet**, nicht korrigiert | `.input` trägt `background: var(--color-surface)`, und die Bahn ist `--color-surface`: das Feld zeigt sich nur als umrandete Pille. `.karte` und `.meldung` sind für diesen Fall auf `--color-bg` gedreht worden, `.input` nicht. Ob das Absicht ist, entscheidet nicht das Artboard — es zeigt, was gebaut ist. Als Befund unter Offene Fragen 11. |
| 2026-09-06 | **D0005** — wo sitzt die Klassenpflege? | **im Layout-Modus des Boards**, als eigener Bereich unter der Zeile für die neue Spalte | Eine Kartenklasse ist an ihr Board gebunden — das Präfix ist nur dort eindeutig, und die Vision will „gezielt das richtige Set greifen **statt des ganzen Boards**". Ein eigener Schirm hätte deshalb keinen Gegenstand, und die WBS kennt zu D0005 auch keinen Dialog außerhalb des Boards. Der ältere Satz setzt die Klassen in „Gestalten unter dem Board" (`wireframes.js:230`); diese Fläche ist heute der gebaute Layout-Modus. **Der Befund, der aufzulösen war:** der gebaute Layout-Modus (`D0001.dc.html`, Zustand 3) hat keinen Klassenbereich — es gab einen Platz im alten Entwurf, aber keinen im gebauten Schirm. Er kommt jetzt hinzu, unter `#neue-spalte`, getrennt durch dieselbe Linie, die `.hr` zieht. |
| 2026-09-06 | Stört der neue Bereich die siebzehn Zählzusagen des Layout-Modus? | **nein — eigene Kennungen** (`#klassenpflege`, eigene Klassennamen) | `LayoutModusE2ETests` zählt ausschließlich unter `#spaltenbahnen` (`.spaltenbahn`, `.spaltenbahn-bearbeitung`, `.spaltenbahn-vermerk`) und auf `#neue-spalte` (`BoardSeite.cs:29,35,37,122`). Kein Element des Klassenbereichs fällt darunter, solange er keinen dieser Namen borgt. Das ist dieselbe Lage, in der bei I0019 ein Klassenname als Vertragsschutz diente — und der Grund, warum die Anlegezeile der Klasse **nicht** `.spaltenpflege-neu` heißt, obwohl sie so aussieht. |
| 2026-09-06 | Beschriftung: „Klassen" oder „Kartenklassen"? | **„Klassen" in der Oberfläche, `Kartenklasse` im Code** | Im Board steht das Wort neben „Spalten" und ist dort so eindeutig wie dieses; niemand liest im Kanban-Board eine Programmierklasse. Vision (`R00000-vision.md:52`), älterer Satz und WBS sagen ebenfalls „Klasse". Im Code dagegen ist „Klasse" belegt, deshalb heißen Typ, Tabelle, DTO und Vertrag `Kartenklasse` (C06 der Projekt-CLAUDE.md) — mit `Kartenklasse.Board` als Fremdschlüssel nach der Id-Regel. Die Beschriftung folgt der Lesart am Ort, der Bezeichner der Eindeutigkeit im Stack; beide Regeln gelten, sie gelten nur an verschiedenen Stellen. |
| 2026-09-06 | Tragen die Klassenzeilen ✎ und ✕, wie der ältere Satz sie zeichnet? | **nein — als Befund in die Fragen, nicht ins Bild** | Die WBS führt unter D0005 nur Anlegen (I0020), Zuordnen (I0021) und Abrufen (I0022). Ändern und Entfernen haben keinen Knoten; ein Bedienelement ohne Knoten wäre gegen den Kontrakt („Keine Dialogs erfinden"). Dieselbe Antwort wie beim Verlauf der Karte in D0004 (Frage 15) — und aus demselben Grund: das ist ein Befund für `/planung`, keine Lücke zum Auffüllen. Siehe Frage 17. |
| 2026-09-06 | Wie wird **I0022** sichtbar, wenn es keinen Schirm hat? | **der Aufruf selbst wird gezeichnet**, der Klassenfilter als markierte Lücke | Der Kontrakt verlangt zu jeder Interaction ein sichtbares Bedienelement. Für I0022 ist das der Aufruf: „Über die API liefert eine Klasse genau ihre Karten" — für einen Agenten, den gleichberechtigten Akteur der Vision, ist der Aufruf das Bedienelement. Einen Filter danebenzuzeichnen hieße, eine Oberfläche zu erfinden, die das Fertig-Kriterium nicht verlangt und die eigene Fragen aufwirft (was zeigt eine gefilterte Bahn als Kartenzahl, was heißt Ziehen in einer gefilterten Ansicht). Die Route folgt dem gebauten Muster `/api/boards/{boardId}/spalten/{spalteId}/karten` und ist Entwurf, keine Zusage. |
| 2026-09-06 | Feldbeschriftung der Anlegezeile: „Bezeichnung" wie bei der Spalte? | **„Name" und „Nummernkreis-Präfix"** | Das Fertig-Kriterium von I0020 sagt „mit Name und Nummernkreis-Präfix"; das Board heißt ebenfalls mit „Name". „Bezeichnung" trägt im Stack bisher nur die Spalte (`Spalte.Bezeichnung`). Das Artboard nimmt die Wörter des Kriteriums, statt eine dritte Schreibweise zu setzen. |
| 2026-09-06 | **D0006** — wo sitzt die Zeiterfassung? | **auf der Kartenseite**, im Kasten, den `D0004.dc.html` dafür freihält | Vier der fünf Interactions hängen an **einer Karte**: I0023 „Timer läuft **auf einer Karte**", I0024 der Eintrag dazu, I0025 dessen Korrektur, I0026 „**die Karte** zeigt ihre Zeiteinträge". `D0004.dc.html` trägt an dieser Stelle bereits einen gestrichelten Kasten „Zeiten und Timer · D0006 · I0023–I0026", und der ältere Satz zeichnet die Zeitentabelle in **allen drei** Kartendetail-Varianten in dieselbe Spalte (`wireframes.js`, `zeitenTab`). Ein eigener Schirm hätte für diese vier keinen Gegenstand — und die WBS kennt zu D0006 auch keinen. |
| 2026-09-06 | Wo sitzt **I0027**, die einzige Interaction ohne Karte? | **Zone 3 der Kopfzeile**, als Plakette neben dem Identitätsplatz, aufklappbar wie die Identitätswahl | Das Fertig-Kriterium sagt „**alle** gerade laufenden Timer … **auf einen Blick**" — das schließt jeden Platz aus, den man erst aufsuchen muss. Drei Alternativen sind geprüft und verworfen: (1) **eine Zone im Board** zeigte nur die Timer des offenen Boards, ein Timer hängt aber an einer Karte, nicht am gerade offenen Board; (2) **das Banner der Board-Übersicht** aus dem älteren Satz („Start B", `wireframes.js:60`) hat kein Zuhause mehr, weil für die Übersicht **Variante A gesetzt** ist, und die rechte Ereignisspur aus „Board A" gehört `D0007`, dessen Form erst `I0028` entscheidet — `I0027` braucht laut WBS aber nur `I0023`; (3) **ein eigener Schirm** wäre eine erfundene Interaction. Zone 3 dagegen ist gebaut, steht auf jeder Seite und trägt schon Identitätsplatz samt Popover (`Kopfzeile.razor`). Sie ist zudem das Gegenmittel gegen den benannten Preis von Variante C — laufende Zeit und der Name, für den sie läuft, stehen nebeneinander. Die Plakette gehört **neben** `identitaetsplatz`, nicht in `kopfzeile-bedienung`: das füllt die offene Seite. |
| 2026-09-06 | Woran sieht man, dass ein Timer für **mich** läuft und nicht für jemand anderen? | **Füllung und Handlung**, nie die Farbe allein: eigene Plakette gefüllt im Akzentton mit Stoppquadrat, fremde ruhig im Olivton mit den Initialen | Olive und Terrakotta sind in diesem Canvas vergeben — sie tragen die **Art** des Kontributors (D0002: Mensch olive, Agent terrakotta, abgebildet neutral). Wer „für mich" über die Farbe erzählte, sagte zugleich etwas Falsches über die Art. Die gefüllte Akzentbehandlung ist im ganzen System die der Hauptaktion (`.btn-haupt`) und sagt hier genau das: hier läuft meine Zeit, und ich kann sie von hier beenden. Die Artfarbe bleibt im runden Initialenkreis. Die Plakette in `D0003.dc.html` bleibt damit richtig: dessen Kopfzeile zeigt „nicht gewählt", also ist jeder laufende Timer dort ein fremder. |
| 2026-09-06 | Ein zweiter Timer, während einer läuft — was passiert? | **nicht entschieden; der Moment ist gezeichnet, beide Lesarten stehen nebeneinander** | Das Fertig-Kriterium von I0023 sagt „**ein** Timer läuft auf einer Karte" — es sagt nicht, ob ein Kontributor mehrere zugleich haben darf. Ein Mensch arbeitet an einer Sache; ein Agent kann sehr wohl an zweien arbeiten, und die Vision stellt beide gleich. Eine stille Setzung hier wäre eine Entscheidung über Fachlichkeit am Bild vorbei; das Artboard zeigt deshalb den Moment mit „Umschalten" und „Beide laufen lassen" und markiert die Stelle als offen. Entschieden wird beim Zerlegen von I0023 in Bubbles. Siehe Frage 20. |
| 2026-09-06 | Darf man einen **fremden** Timer stoppen? | **ja — die erlaubende Fassung ist gezeichnet, als Befund benannt** | I0024 sagt nicht, wer stoppen darf. Full Trust ohne Anmeldung ist eine Leitplanke der Vision, und ein Agenten-Timer, der über Nacht weiterläuft, muss von jemandem beendet werden können — eine Sperre wäre die stärkere Setzung und hätte keinen Beleg. Der Eintrag behält dabei den Kontributor, für den er läuft: gestoppt wird der Timer, nicht die Urheberschaft. Siehe Frage 21. |
| 2026-09-06 | Trägt das Änderungsformular „Eintrag löschen"? | **ja im Bild, als Befund benannt** | I0025 nennt „nachtragen und ändern", nicht löschen. Anders als bei den Klassen (Frage 17) ist hier aber ein Schaden absehbar, den kein Ändern behebt: ein Nachtrag auf der falschen Karte lässt sich nicht wegkorrigieren, weil das Formular die Karte nicht wechselt. Gezeichnet ist deshalb der Knopf, und die Frage steht im Index — anders als bei D0005 sitzt das Bedienelement nicht auf einem eigenen Knoten, sondern in einem, den es schon gibt. |
| 2026-09-06 | Ist der Zeitenschirm des älteren Satzes („Zeiten je Kontributor", A/B) die Vorlage? | **nein — keine der beiden Varianten wird ein Schirm** | Beide zeichnen eine Auswertung über Kontributoren hinweg: **A** die Kreuztabelle Karte × Kontributor mit Soll-Spalte, **B** den Stundenzettel je Person und Tag. Kein Fertig-Kriterium von I0023–I0027 verlangt das; der eigene Hinweistext des Schirms nennt als vierte Quelle `I0036` „Zeiten exportieren" — und der gehört **D0009**. Übernommen sind aus dem Schirm die **Bausteine**, nicht der Rahmen: der laufende Eintrag mit Beginn, Ende und Quelle, das Nachtragen als Zeile, das Banner der laufenden Timer. Die Kreuztabelle bleibt für `D0009` liegen, wo sie hingehört. |
| 2026-09-06 | Rahmenhöhe und Platz von `D0006` auf dem Canvas | **1440×3480** bei `x` 7800, `y` 1300 | Gemessen, nicht geschätzt (Chromium, Google-Fonts-Fassung derselben Schriften): 3304,1 px. 3480 gibt 5,3 % Reserve; der Überschuss trägt die Grundfarbe `#ebddc5` der Lesehilfe und ist deshalb unsichtbar. Der Platz setzt die Dialogreihe nach rechts fort, 120 px hinter `D0005`; Positionen der übrigen Artboards bleiben unverändert. |
| 2026-09-06 | Rahmenhöhe und Platz von `D0005` auf dem Canvas | **1440×1980** bei `x` 6240, `y` 1300 | Gemessen, nicht geschätzt (Chromium, Google-Fonts-Fassung derselben Schriften): 1877,5 px. 1980 gibt 5,5 % Reserve; der Überschuss trägt die Grundfarbe `#ebddc5` der Lesehilfe und ist deshalb unsichtbar. Der Platz setzt die Dialogreihe nach rechts fort, 120 px hinter `D0004`; Positionen der übrigen Artboards bleiben unverändert. |
| 2026-09-07 | **D0007** — wo sitzt die Live-Aktualisierung, wenn sie keinen eigenen Schirm hat? | **an den vorhandenen Schirmen**: Board (D0003), Kartenseite (D0004), Zone 3 der Kopfzeile — gezeichnet sind Zustände und Übergänge, kein neuer Bildschirm | Der Index hält seit 2026-08-30 fest, dass D0007 „kein eigener Schirm" ist, und der ältere Satz kennt Live nur als Merkmal von „Board" und „Start B". Ein eigener Bildschirm wäre eine erfundene Interaction (Kontrakt: keine Dialogs erfinden). Das Fertig-Kriterium von I0028 sagt „zeigt **jede andere offene Sicht** die Änderung" — der Gegenstand ist ein Verhalten aller Sichten, kein Ort. |
| 2026-09-07 | Woran sieht man, dass sich etwas bewegt hat, das man **nicht selbst** bewegt hat? | **Einflugmarke an der Sache selbst**: Akzentkante links plus eine Fußzeile „Wer · über welchen Weg · wann", die nach kurzer Zeit von selbst verschwindet | Eine Karte, die ohne Zutun springt, sieht wie ein Fehler aus — die Marke macht aus dem Sprung eine Nachricht. Sie steht **dort, wo die Änderung ist**, statt in einer Liste daneben, die man erst mit dem Board abgleichen müsste, und kostet keinen Platz, sobald nichts passiert (dieselbe Regel wie bei `Laufzaehler.Fuer`). Die eigene Handlung trägt **keine** Marke: sie hatte ihre Rückmeldung schon. |
| 2026-09-07 | Wird eine **Aktivitätsspur** (Board A) oder ein **Laufband** (Board B) gezeichnet? | **keine von beiden** — als markierter Befund in Rand C, nicht als Zutat | Der Index lässt die Wahl ausdrücklich I0028. Geprüft wurde zuerst, ob ein Fertig-Kriterium sie verlangt: I0028 sagt „zeigt … die Änderung ohne Zutun", I0029 spricht vom Nachholen des Stands — **keins von beiden verlangt eine Liste vergangener Ereignisse**. Eine solche Liste wäre ein *Verlauf*; der steht bei D0004 schon als Frage 13 ohne Knoten. Dieselbe Haltung wie bei den Klassenzeilen (Frage 17): ohne Knoten kommt es als Befund ins Bild, nicht als Bedienelement. |
| 2026-09-07 | Was passiert mit etwas, das ich gerade **in der Hand** habe? | **es wird nie ausgetauscht**: ein offenes Feld bekommt ein Angebot darüber („Nina hat die Beschreibung geändert · ansehen · übernehmen"), ein laufender Zug lässt die fremde Bewegung warten | Eine Änderung, die unter der Hand den Text austauscht, wäre schlimmer als gar keine Live-Aktualisierung. Das Angebot nennt Sachverhalt und Kompensationsaktion — dieselbe Haltung wie die Fehlerantworten der API. Beim Ziehen kennt der Bestand den Gedanken schon: die Ablegefläche ändert ihr Aussehen, „solange ein Zug läuft" (`Spaltenbahnen.razor`, `ablegeflaeche-laeuft`). |
| 2026-09-07 | Steht die Marke **„● live"** dauerhaft in der Kopfzeile, wie der ältere Satz sie zeichnet? | **nein** — sie erscheint erst, wenn die Verbindung weg ist, und nennt dann den Stand („nicht live · Stand von 09:12") | `wireframes.js` führt `liveMarke` in „Start B" und „Board A/B" dauerhaft. Übernommen wird sie nicht: I0027 hat für genau diese Stelle entschieden, dass ein Element, das nichts zu sagen hat, den Platz zurückgibt (`Laufzaehler.Fuer` liefert `null`, kein „0 laufen"). **Abwesenheit heißt: es steht.** Die Gegenrede — Abwesenheit könnte mehrdeutig sein — trägt nicht, weil der Abbruch selbst laut gezeigt wird (Zustand 5). |
| 2026-09-07 | Was ist während eines Abbruchs bedienbar? | **nichts** — und genau das wird gezeigt: die Fläche wird ruhiger statt unlesbar, die mitlaufenden Dauern halten an, die Marke nennt den Stand | Bei Blazor Server hängt jede Interaktion am Kreislauf; „bedienbar bleiben" ist keine Gestaltungswahl, sondern nicht vorhanden. Der Wert des getrennten Schirms ist, dass er **über sein Alter nicht lügt**. Eine Uhr, die ohne Verbindung weiterzählt, wäre die eine Zahl, die sicher falsch ist — dieselbe Begründung, mit der `B0326` und `B0335` die Dauer ohne Live-Kanal weggelassen haben. |
| 2026-09-07 | Aufschließen: **Ereignisse nachspielen** oder **frisch holen**? | **frisch holen**, mit einem Band „Wieder verbunden · 4 Änderungen nachgeholt" und Marken mit Uhrzeit statt „vor 3 Sek" | Das Fertig-Kriterium von I0029 sagt „holt den verpassten **Stand** nach" — den Stand, nicht die Ereignisse. Der Bestand hält es schon so: die Kopfzeile holt bei jedem Anlass frisch, statt in der Hand nachzuziehen (`Kopfzeile.razor`, „danach wird die Liste frisch geholt"). Ein Nachspielen bräuchte ein Ereignisjournal samt Reihenfolge, das nirgends geführt wird und dessen Lücken niemand bemerkte. Diese Marken bleiben als einzige stehen, bis das Band geschlossen wird: nach einer Trennung sind sie das Protokoll der Lücke. |
| 2026-09-07 | Löst der Live-Kanal die **Kopfzeilen-Plakette** aus ihrer Schuld? | **nur zur Hälfte**: die Zahl wächst jetzt ohne Ladeanlass, aber sie bleibt eine **Zahl**; die Dauer bekommt das Popover | R00030 nennt zwei Gründe für die fehlende Dauer. Der erste (ohne Live-Kanal ab der ersten Sekunde falsch) fällt hier weg. Der zweite überlebt: über alle Boards dürfen mehrere eigene Timer laufen (I0023, Entscheidung 1), und eine Dauer in der Plakette müsste einen davon auswählen — jede Auswahl wäre eine Zusage, die sie nicht halten kann. I0027 hat den Ausweg schon benannt: „Dauer und ,welcher denn‘ beantwortet einen Klick weiter das Popover, wo eine Zeile **ein** Eintrag ist". |
| 2026-09-07 | Wie kommt eine Änderung **der API** in die offenen Sichten? | **nicht entschieden — als markierte Lücke gezeichnet** (Zustand 4, gestricheltes Feld mit „?") | Der Weg vom WebApi-Prozess (5280) zurück in die Blazor-Sichten (5180) existiert nicht; `KanbanC.Contracts/Ereignisse/` liegt leer im Bestand und ist genau dafür freigehalten (belegt in `B0295`). **Wie** er gebaut wird, ist eine Architektur- und Bubble-Frage, keine Gestaltungsfrage — dasselbe Muster wie die markierten Lücken in `D0001` (I0038/I0039) und `D0005` (Klassenfilter). Gezeichnet ist deshalb nur, **was das Bild verspricht**: gleiche Kante, gleiche Zeile, derselbe Augenblick, egal wer gehandelt hat. |
| 2026-09-07 | Rahmenhöhe und Platz von `D0007` auf dem Canvas | **1440×4420** bei `x` 9360, `y` 1300 | Gemessen, nicht geschätzt (Chrome headless, Google-Fonts-Fassung derselben Schriften, `document.fonts.ready` abgewartet): 4200,7 px. 4420 trägt rund 5 % Reserve — dieselbe Rechnung wie bei `D0005` und `D0006`. Der Platz schließt die Reihe nach rechts an `D0006` an, mit 120 px Abstand. |
| 2026-09-07 | **D0008** — wo fängt ein Import an? | **im Layout-Modus des Boards**, unter der Klassenpflege; die Arbeit selbst auf eigener Adresse `/boards/{BoardId}/import` | Der ältere Satz zeichnet den Schirm, nicht den Weg dorthin. Drei Belege führen an dieselbe Stelle: (1) der Layout-Modus ist der Ort, an dem ein Board **eingerichtet** wird — Spalten (I0003/I0040) und Klassen (I0020) —, und ein Import richtet ein, er bedient nicht; (2) ein Import braucht eine **Kartenklasse**, sonst bekämen die Karten keine Nummer, und die wohnt genau dort (D0005, Zustand 1); (3) der Screen-Flow trägt die Kante **D0005 → D0008** bereits (`Main.dc.html:128`) — sie ist damit gezeichnet, und `Main.dc.html` bleibt unverändert. Die eigene Adresse folgt der Begründung von D0004 (Variante C, 2026-09-05): Mensch und Agent zeigen auf dieselbe Stelle, und drei Schritte brauchen die ganze Höhe. Ein eigener Dialog-Knoten „Import-Schirm" wäre eine erfundene Interaction. |
| 2026-09-07 | Welche Variante des älteren Satzes wird der Schirm? | **A als Gerüst, B als Inhalt von Schritt 2** | A allein (drei Schritte, Bilanz, Bericht) beantwortet die schwierigste Frage nicht — was aus einem Baum wird. B allein (Baum links, Wirkung rechts) hat keinen Bericht und damit kein `I0032`. Beide Interactions verlangen beides; der Zusammenbau erfindet nichts, er setzt B als Inhalt der Station „2 Vorschau" ein. |
| 2026-09-07 | **Welche Ebene der WBS wird eine Karte?** | **die Interaction**, sichtbar als Regler **Schnittebene** (Dialog · Interaction · Feature · Bubble), Vorgabe Interaction | Der ältere Satz bildet in **beiden** Varianten Bubbles auf Karten ab (`B0043`, `B0019`, `B0020`). Angewandt auf die WBS dieses Repositorys wären das **431 Karten** aus einer Datei — genau der unbesehene Massenimport, gegen den die Vorschau steht. Die Interaction ist im Bestand die Einheit der Arbeit: der Vertical Slice, auf dem `/implementierung im-pair` arbeitet, und die Ebene, die `/github` als **ein Issue** projiziert. 37 Karten sind ein Board. Die Vorgabe wird trotzdem **nicht versteckt**: der Regler steht im Bild und nennt die Zahl, die aus jeder Wahl folgt, bevor jemand sie erzeugt. |
| 2026-09-07 | **Was trägt die Eltern-Kind-Beziehung, wenn aus einem Baum ein Board wird?** | **drei Dinge, die es schon gibt** — Etikett (I0015) für den Dialog, Teilaufgaben (I0016) für Features und Bubbles, Dateiverweis (I0019) für die Herkunft; die Application wird das Zielboard und nie ein Knoten | Ein Board hat Spalten und Karten, kein Baum. Statt eine Hierarchie zu erfinden, gilt **eine Regel**: alles über der Schnittebene wird Ort und Etikett, die Schnittebene wird Karte, alles darunter wird Teilaufgabe. Jedes Ziel ist ein Feld, das die WBS als eigenen Knoten führt und das `R00017` bis `R00023` gebaut haben — `I0019` nennt „Verweise auf Pfade (Anforderungs-, **Planungs**-, Architekturdateien)" wörtlich, das ist die WBS-Datei selbst. Die Ebene **Feature** verliert dabei ihre Schachtelung: Features und Bubbles stehen flach nebeneinander in Dateireihenfolge, mit der ID vorn. Eine Teilaufgabenliste ist nach `I0016` flach; eine zweite Ebene wäre ein Knoten, den die WBS nicht hat. |
| 2026-09-07 | Titel der Karte: Name oder ID plus Name? | **`[I0001] Board anlegen`** | Die Hausform für die Projektion eines WBS-Knotens nach außen steht schon fest: `/github` gibt einem Issue den Titel `[I0003] Name`. Der Preis ist benannt — die Karte trägt dann **zwei** Nummern, die Kartennummer `WBS-32` der Klasse und die Knoten-ID der Datei. Sie sagen Verschiedenes: `WBS-32` gehört dem Board, `I0001` gehört der Datei, und beide werden in Gesprächen, Commits und Zweignamen benutzt. |
| 2026-09-07 | Woran erkennt der zweite Lauf eine Karte wieder? | **am Dateiverweis** `…/kanbanc.md#I0001`, mit benanntem Preis | Der Schlüssel darf nicht der Titel sein (änderbar) und nicht die Kartennummer (die vergibt die Klasse, nicht die Datei). Der Dateiverweis ist der einzige Ort im Bestand, der eine **Herkunft** trägt, und zugleich der Weg vom Board zurück in die Quelle. Der Preis: `I0019` erlaubt dem Menschen, einen Dateiverweis zu **entfernen** — dann entsteht beim nächsten Lauf eine zweite Karte für denselben Knoten. Ein eigenes Feld an der Karte bräuchte einen Knoten, den die WBS nicht hat. Siehe Frage 29. |
| 2026-09-07 | Was macht der zweite Lauf mit Karten, deren Knoten aus der Datei verschwunden ist? | **melden, nicht anfassen** — das Fach `ZuLoeschen` des Soll-Ist-Musters bleibt leer | `SollIstVergleich.md` (`.claude/app-architectures/Common/snippets/`) liefert vier Fächer; drei sind wörtlich übernommen, das vierte heißt hier „nicht mehr in der Datei". Eine Karte trägt, was die Datei nie hatte — Zeiteinträge, Kommentare, Anhänge, ihre Lage in der Bahn —, und genau daraus rechnet `D0009` Soll-Ist und Burndown. Die Vision führt außerdem als offene Richtungsfrage, dass der Import **in eine Richtung** geht; er darf nicht rückwärts löschen, was er nie geschrieben hat. Gemeldet wird trotzdem, mit der Kompensationsaktion daneben (archivieren, `I0014`) — keine stille Korrektur. |
| 2026-09-07 | Was zieht der zweite Lauf nach, und was bleibt? | **Datei**: Titel, Beschreibung, Etikett, Teilaufgaben samt Haken, Dateiverweise · **Board**: Spalte und Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Kartennummer | Die Trennung folgt zwei Sätzen, die beide im Bestand stehen. Projekt-CLAUDE.md: „**Die WBS ist die Fortschrittswahrheit**; weicht eine andere Liste ab, hat die WBS recht" — also gewinnt die Datei bei allem, was sie führt, bis hin zum Haken an der Teilaufgabe. Vision: „Vision, Anforderungen und WBS bleiben als Markdown-Dokumente die Wahrheit; **das Board führt den Arbeitsfluss**" — also gewinnt das Board bei der Spalte. Deshalb wird die Spalte **nur beim Anlegen** aus dem Status gesetzt: „Bereit" und „Prüfung" haben in der WBS kein Gegenstück, und eine Karte, die der zweite Lauf aus „Prüfung" zurückzöge, verlöre eine Aussage, die nur das Board kennt. Der Preis steht als Frage 30. |
| 2026-09-07 | Was sieht der Mensch, wenn ein **Agent** importiert? | **denselben Vorgang ohne Schirm** — `POST /api/boards/{id}/wbs-import` mit `trocken=true` für die Vorschau, danach der Live-Kanal aus D0007 | Die Zusage der Vision gilt auch für das Zeigen-vor-Schreiben: wäre die Vorschau nur ein Schirm, könnte ein Agent nicht prüfen, was er anrichtet. `trocken=true` gibt dieselbe Antwort, die Schritt 2 zeichnet, ohne dass etwas entsteht. Dass die entstehenden Karten in offenen Sichten erscheinen, ist keine Zutat, sondern eine **offene Schuld aus dem Bestand**: die WBS hält an `I0028`/`B0375` fest, die Meldung entstehe im Endpunkt, und „entsteht ein Weg an der WebApi vorbei (etwa der WBS-Import `I0030`), muss er die Meldung selbst tragen". Gezeichnet ist nur, dass es **ein** Vorgang ist und nicht 31 Bewegungen; wie ein Sammelereignis aussieht, gehört den Bubbles. |
| 2026-09-07 | Bekommt die Karte eine **Sollzeit** aus der Spalte `Aufwand`? | **nein — gestrichelter Kasten statt erfundener Ablage** | `I0026` hat es entschieden und begründet: „eine Sollzeit gibt es im Bestand nicht; sie gehört `I0033`". `I0033` wiederum braucht `I0030` — die Zählung kommt also über den Import ins System, aber **wo sie landet**, ist nicht entschieden. Ein Feld danebenzuzeichnen hieße, `I0033` seine Entscheidung vorwegzunehmen. Dieselbe Haltung wie bei den markierten Lücken in `D0001` (I0038/I0039), `D0005` (Klassenfilter) und `D0007` (Rückweg der API). Siehe Frage 23, die damit geschärft ist. |
| 2026-09-07 | Rahmenhöhe und Platz von `D0008` auf dem Canvas | **1440×5100** bei `x` 10920, `y` 1300 | Gemessen, nicht geschätzt (Chrome headless, Google-Fonts-Fassung derselben Schriften, `document.fonts.ready` abgewartet): 4844,3 px. 5100 trägt rund 5 % Reserve — dieselbe Rechnung wie bei `D0005` bis `D0007`. Der Platz schließt die Reihe nach rechts an `D0007` an, mit 120 px Abstand; Positionen der übrigen Artboards bleiben unverändert. |

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
| **D0001** Boards führen | „Start / Board-Übersicht" (ganz) · „Board anlegen & gestalten" (ganz) · „Board" (nur der Boardkopf: Kartenzahl-Schalter, Klassenfilter) | Start **A** · Anlegen **A**, Layout-Modus **B** | **I0038 / I0039** — Export und Import stehen nur als Knopf im Fuß von „Start A" und im Board-Abschnitt von „Gestalten B". Dateiwahl, Vorschau und Ergebnis sind nirgends gezeichnet. Seit 2026-09-04 als `D0001.dc.html` gezeichnet; die Lücke steht dort als **markierter Kasten**, nicht als erfundener Ablauf. |
| **D0002** Kontributoren führen | „Kontributoren & Identität" (ganz) | Liste **A** gesetzt · Identitätswahl **B oder C offen** (Frage 4) | — vollständig; seit 2026-08-31 als `D0002.dc.html` gezeichnet |
| **D0003** Board bedienen | „Board" (Bahnen, Karten, Abschlussspalte) | Bahnen **A/B** — dort deckungsgleich; **C** wäre eine spätere Zweitansicht | **I0014** Karte archivieren — der alte Satz kennt nur *Board* archivieren (I0005). Im Artboard als ⋯-Menü ergänzt, siehe Frage 6. |
| **D0004** Karteninhalt pflegen | „Kartendetail" (ganz) | **C** — eigene Seite, entschieden 2026-09-05 bei I0015 | **Verlauf** — alle drei Varianten des alten Satzes zeichnen eine Verlaufsspur („wer, wann, über welche Grenze"); die WBS kennt dazu keinen Knoten. Seit 2026-09-05 als `D0004.dc.html` gezeichnet; der Verlauf steht dort **nicht** im Bild, sondern als Frage 13. |
| **D0005** Karten-Klassen | „Board anlegen & gestalten" **B**, Abschnitt *Klassen* (I0020) · „Kartendetail" und die Kartenform in „Board" (I0021, Nummer auf der Karte) | Klassen-Teil aus **B** gesetzt | **I0022** — reine API-Zusage, absichtlich ohne Schirm; die Oberflächenentsprechung wäre der Klassenfilter, der mit R00005 aus dem Boardkopf nach Zone 3 gewandert ist. Seit 2026-09-06 als `D0005.dc.html` gezeichnet: der Aufruf steht im Bild, der Filter als markierte Lücke. **Ändern und Entfernen einer Klasse** hat keinen Knoten — Frage 17 |
| **D0006** Zeiterfassung | „Kartendetail" (Zeitentabelle und Timerzeile, in allen drei Varianten dieselbe Spalte) · „Zeiten je Kontributor" (nur die **Bausteine**: laufender Eintrag mit Beginn/Ende/Quelle, Nachtragezeile, Banner der laufenden Timer) · „Start B" Banner (I0027, als Inhalt — nicht als Ort) | **keine der beiden** Zeiten-Varianten wird ein Schirm, entschieden 2026-09-06 — A und B sind Auswertungen über Kontributoren hinweg und gehören zu **D0009** | — alle fünf gezeichnet; seit 2026-09-06 als `D0006.dc.html`. Ohne Vorlage im alten Satz waren der **erzwungene Identitätsschritt** vor dem ersten Timer (in D0002 nur als Preis von Variante C benannt) und der **zweite Timer während einer läuft** — beide stehen jetzt als Rand im Bild, der zweite ausdrücklich unentschieden (Frage 20) |
| **D0007** Live-Aktualisierung | **kein eigener Schirm** — nur als Merkmal *innerhalb* von „Board": Ereignisspur rechts (**A**) oder Laufband oben (**B**), dazu die Marke „● live" in der Kopfzeile und der Live-Punkt in „Start B" | **keine von beiden** — entschieden 2026-09-07: weder Spur noch Laufband, weil kein Fertig-Kriterium eine Liste vergangener Ereignisse verlangt; ebenso fällt die dauerhafte Marke „● live" | — beide Interactions gezeichnet; seit 2026-09-07 als `D0007.dc.html`. **I0029** hatte im älteren Satz keine Vorlage und ist entworfen, nicht abgeschrieben: drei Stufen des Abbruchs und das Aufschließen. Ohne Vorlage waren ebenso die **Einflugmarke** und die Regel **Angebot statt Austausch** für offene Felder |
| **D0008** WBS-Import | „WBS-Import" (ganz) | **A als Gerüst, B als Inhalt von Schritt 2** — entschieden 2026-09-07. A allein (drei Schritte, Bilanz, Bericht) sagt nicht, was aus einem Baum wird; B allein (Baum links, Wirkung rechts) hat keinen Bericht und damit kein `I0032`. Beide Interactions verlangen beides | — alle drei gezeichnet; seit 2026-09-07 als `D0008.dc.html`. Ohne Vorlage im alten Satz waren der **Einstieg** (wo ein Import anfängt — der alte Satz zeigt den Schirm, nicht den Weg dorthin), die **Anatomie Baum → Board** Feld für Feld, die **Schnittebene** und das vierte Fach des zweiten Laufs (*„nicht mehr in der Datei"*). Der alte Satz bildet in beiden Varianten **Bubbles** auf Karten ab (`B0043`, `B0019`); das ist hier zur **Interaction** verschoben und als Regler sichtbar gemacht — Frage 28 |
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

   **Nachtrag 2026-09-05, mit `D0004.dc.html` beantwortet:** es ist beides. Das
   Menü bleibt, wie D0003 es zeichnet, und bekommt „Details öffnen" als zweiten
   Eintrag; die Kartenseite trägt „Archivieren" als benannte Fremdhandlung aus
   I0014. Gezeichnet im Abschnitt *Einstieg vom Board*. Offen bleibt nur, ob das
   Menü darüber hinaus weitere Einträge bekommt.

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


12. **`D0003.dc.html` zeigt zwei Werte, die der Code anders schreibt.** Gefunden
    beim Zeichnen von D0001, belegt am Quelltext:
    `Source/KanbanC.Blazor/Services/Terminformatierer.cs` formatiert mit
    `yyyy-MM-dd`, das Artboard zeigt in der Kopfzeile aber
    „Start 01.08.2026 · Ziel 30.09.2026" (`D0003.dc.html:100`); und
    `Components/Pages/Board.razor` rendert `@_board.Art`, also den Enum-Namen
    **`Projekt`**, während das Artboard **„Projektboard"** zeigt (dieselbe Zeile).
    `D0001.dc.html` folgt dem Code. Nachgezogen wird D0003 hier **nicht**: der
    Beleg für eine Nachführung ist eine Anforderung, die die Abweichung trägt
    (Regel vom 2026-09-02), und die gibt es für diese zwei Werte nicht. Ob das
    ISO-Datum in der Oberfläche bleiben soll, ist ohnehin eine fachliche Frage
    und gehört in eine Anforderung, nicht ins Bild.

13. **Gilt der Kartenzahl-Schalter (I0004) für den Browser oder für das Board?**
    Das Fertig-Kriterium sagt „je Board einschaltbar" — das spricht für eine
    Eigenschaft des Boards, die alle sehen. Dagegen spricht nichts Zwingendes:
    eine Ansichtseinstellung im `localStorage` wäre billiger und träfe niemanden
    sonst. `D0001.dc.html` zeichnet den Schalter, ohne die Frage zu beantworten;
    entschieden wird sie in der Anforderung zu I0004.

14. **Das ⋯-Menü der Boardkachel liegt unter der Verweisfläche.**
    `Boardkachel.razor.css` legt mit `.board-verweis::after { inset: 0 }` den
    Verweis über die **ganze** Kachel — ein Bedienelement darauf wäre nicht
    anklickbar. Das ist kein Befund am Artboard, sondern die erste Frage, die
    beim Bauen von I0005 auftritt: entweder das Menü bekommt einen
    Stapelkontext über der Verweisfläche, oder die Verweisfläche endet vor dem
    Menü. Gehört in die Anforderung zu I0005, nicht ins Bild.

15. **Der Verlauf der Karte hat keinen Knoten.** Alle drei Varianten des älteren
    Satzes zeichnen ihn — „Verlauf" mit Kontributor, Zeitpunkt und Quelle
    (`wireframes.js:151`, `:176`, `:195`), in Variante C sogar als Zeitachse über
    die halbe Breite. In der WBS gibt es dazu **nichts**: weder unter D0004
    (I0015–I0019) noch anderswo steht eine Interaction, die Handlungen an einer
    Karte festhält. `D0004.dc.html` zeichnet ihn deshalb **nicht** — ein Artboard
    ohne Knoten wäre gegen den Kontrakt. Das ist ein Befund für `/planung`, nicht
    eine Lücke zum Auffüllen: entweder der Verlauf ist gewollt, dann braucht er
    eine Interaction, oder die Vision-Zusage „an jeder Karte ist ablesbar, wer
    oder was gehandelt hat" wird von Kommentar (I0017) und Zeiteintrag (I0024)
    allein getragen. Die Zeile „Quelle: Oberfläche / API" steht im Artboard
    deshalb nur an den Kommentaren, wo I0017 sie deckt.

16. **Etikett ist als freie Textmarke gesetzt, nicht abgestimmt.** Der Begriff
    steht im Fertig-Kriterium von I0015, ist aber nirgends definiert — der
    einzige Beleg im ganzen Repository ist ein einzelner Chip „Import"
    (`wireframes.js:157`). Entschieden ist: freie Marke je Karte mit
    Vervollständigung aus dem Bestand des Boards (siehe Entscheidungstabelle).
    Die Zerlegung hängt daran — eine Marke braucht eine Tabelle
    `Etikett (KarteId, Text)` und eine Abfrage über das Board, ein verwalteter
    Satz bräuchte darüber hinaus einen Pflegeschirm und damit einen WBS-Knoten,
    den es nicht gibt. Wer den verwalteten Satz will, geht über `/planung`, nicht
    über das Artboard.

17. **Ändern und Entfernen einer Klasse haben keinen Knoten.** Der ältere Satz
    zeichnet je Klassenzeile ✎ und ✕ (`wireframes.js:236`), die WBS führt unter
    D0005 aber nur `I0020` Anlegen, `I0021` Zuordnen und `I0022` Abrufen. Bei den
    Spalten ist beides gedeckt (`I0003` nennt „anlegen/umbenennen/umsortieren/
    entfernen"), bei den Klassen nicht. `D0005.dc.html` zeichnet die Bedienelemente
    deshalb **nicht** — dieselbe Antwort wie beim Verlauf der Karte (Frage 15).
    Das ist ein Befund für `/planung`, nicht eine Lücke zum Auffüllen: entweder
    Klassen sind unveränderlich, sobald Nummern vergeben sind (dann fehlt nichts),
    oder es braucht eine Interaction. Die zweite Frage hängt daran — was mit den
    vergebenen Nummern geschieht, wenn eine Klasse verschwindet, sagt bisher nichts.

18. **Was beim Wechsel der Klasse mit der Nummer geschieht, ist offen.** Das
    Fertig-Kriterium von `I0021` sagt „erhält eine Klasse und damit die *nächste*
    Nummer dieser Klasse" — für den Erstfall. Ob ein Wechsel eine zweite Nummer
    vergibt, die alte verfallen lässt oder beide ablesbar hält, steht nirgends;
    ebenso wenig, ob eine Karte mehr als eine Klasse tragen kann (die Vision sagt
    „mit optionaler Klasse", Einzahl — `R00000-vision.md:52`). `D0005.dc.html`
    zeigt den Erstfall und benennt die offene Stelle im Bild. Entschieden wird es
    in der Anforderung zu I0021, nicht am Artboard.

19. **Ob der Klassenfilter überhaupt gebaut wird, ist offen.** `I0022` ist eine
    API-Zusage; die Oberflächenentsprechung — ein Filter in Zone 3, wo mit R00005
    die Bedienelemente des Boardkopfs gelandet sind — hat kein Fertig-Kriterium.
    Wer ihn will, klärt zuerst zwei Folgefragen, die eine gefilterte Ansicht
    aufwirft: was die Kartenzahl im Bahnenkopf dann zählt (Frage zu I0004 hängt
    daran), und was Ziehen in einer gefilterten Bahn für die Position bedeutet
    (`I0012`). Das gehört in den Slice, nicht ins Bild.

20. **Darf ein Kontributor zwei Timer zugleich laufen lassen?** Das
    Fertig-Kriterium von `I0023` sagt „**ein** Timer läuft auf einer Karte für den
    gewählten Kontributor" — das beschreibt den Timer, nicht die Obergrenze je
    Kontributor. Für einen Menschen wäre „einer zur Zeit" die richtige Annahme;
    ein Agent kann sehr wohl an zwei Karten arbeiten, und die Vision stellt beide
    gleich. `D0006.dc.html` zeichnet den Moment des Konflikts (Rand B) mit beiden
    Lesarten — „Umschalten" und „Beide laufen lassen" — und entscheidet ihn
    **nicht**. Entschieden wird beim Zerlegen von `I0023` in Bubbles; die Wahl
    wirkt auf `I0027` (eine oder mehrere eigene Zeilen) und auf die Zusage der
    Plakette, dass es höchstens eine gefüllte je Bahnenbild gibt.

21. **Wer darf einen laufenden Timer stoppen, und darf er überlappen?** `I0024`
    sagt nur, was ein gestoppter Timer hinterlässt. Zwei Stellen bleiben offen:
    ob ein **fremder** Timer gestoppt werden darf — `D0006.dc.html` zeichnet die
    erlaubende Fassung, weil Full Trust ohne Anmeldung eine Leitplanke der Vision
    ist und ein über Nacht weiterlaufender Agenten-Timer sonst niemanden fände,
    der ihn beendet — und ob sich zwei Zeiteinträge desselben Kontributors
    **überlappen** dürfen. Gezeichnet ist nur die eine Zurückweisung, die aus
    „Beginn und Ende" zwingend folgt: das Ende liegt vor dem Beginn.

22. **„Eintrag löschen" hat keinen Knoten.** `I0025` nennt „nachtragen und
    ändern". Anders als bei den Klassen (Frage 17) steht der Knopf trotzdem im
    Bild, weil ein absehbarer Schaden ohne ihn bleibt: ein Nachtrag auf der
    falschen Karte lässt sich nicht wegkorrigieren, denn das Formular wechselt
    die Karte nicht. Er sitzt in einem Bedienelement, das es schon gibt, nicht in
    einem erfundenen — trotzdem ist er ein Befund für `/planung` und nicht Teil
    des Fertig-Kriteriums von `I0025`.

23. **Woher kommt das Soll je Karte?** `D0006.dc.html` zeigt neben dem Ist die
    Zeile „von 5:00 Soll" — sie steht so im älteren Satz (Kartendetail C, Board C)
    und ist die Grundlage der Schätz-Rückkopplung, die die Vision will. Einen
    Knoten hat sie nicht: `I0015` zählt Titel, Beschreibung, Verantwortlicher,
    Fälligkeit, Farbe und Etiketten auf, keine Sollzeit, und `D0009` setzt sie in
    `I0033` bereits voraus („der erfassten Zeit steht die WBS-Zählung gegenüber").
    Ob das Soll ein Feld der Karte ist oder aus dem WBS-Import (`D0008`) kommt,
    ist nicht entschieden — im Bild steht es als Zahl, im Index als Frage.
    **Geschärft am 2026-09-07 mit `D0008`.** Die Quelle ist gefunden: die WBS
    führt je Bubble eine Spalte `Aufwand`, und `I0033` braucht laut WBS
    ausdrücklich `I0030` — die Zählung kommt also über den Import ins System.
    Was fehlt, ist das **Ziel**: `I0026` hat festgehalten, dass es im Bestand
    keine Sollzeit gibt. `D0008.dc.html`, Zustand 4 zeichnet die Zeile deshalb
    als **gestrichelten Kasten** („Aufwand → kein Ort im Bestand"), statt ein
    Feld zu erfinden, das `I0033` seine Entscheidung vorwegnähme.

24. **Der Weg von der API zurück in die offenen Sichten fehlt — und ist der Kern
    von `I0028`.** Das Fertig-Kriterium sagt „bewegt ein Browser **oder die API**
    eine Karte". Ein Browser bewegt über den Blazor-Kreislauf, der schon steht; ein
    Agent bewegt über `KanbanC.WebApi` (Port 5280), einen **eigenen Prozess**, und
    von dort führt heute nichts zurück in die Blazor-Sitzungen (Port 5180).
    `KanbanC.Contracts/Ereignisse/` liegt seit dem 29.08. leer im Bestand und ist
    genau dafür freigehalten (`B0295`). `D0007.dc.html`, Zustand 4 zeichnet die
    Lücke als markiertes Feld statt eines erfundenen Wegs — **wie** sie geschlossen
    wird, gehört `/architektur` und den Bubbles, nicht dem Bild. Das Bild sagt nur,
    was zu halten ist: gleiche Kante, gleiche Zeile, derselbe Augenblick.

25. **`ReconnectModal.razor` ist gebaut und spricht Englisch.** Der Dialog aus der
    Blazor-Vorlage steht im Bestand, ist mit den Projekt-Tokens gestaltet
    (`--radius-lg`, `--color-accent`, `--shadow-lg`) und trägt „Rejoining the
    server…", „Retry", „The session has been paused by the server." — in einer
    Anwendung, deren Oberfläche sonst durchgehend deutsch ist. `D0007.dc.html`,
    Zustand 5 zeichnet die deutsche Zielform samt der einen Zeile, die die Vorlage
    nicht hat: **seit wann der Stand alt ist**. Ob die Umschrift zum
    Fertig-Kriterium von `I0029` gehört oder in eine eigene Anforderung, ist nicht
    entschieden — es ist ein Befund am Bestand, kein Entwurf.

26. **Wie lange die Einflugmarke steht, ist eine Größenordnung, kein Messwert.**
    „Etwa zehn Sekunden" steht im Bild, damit die Marke überhaupt eine Dauer hat;
    geprüft ist sie nicht. Dieselbe Unschärfe trifft die Frage, ab wie vielen
    nachgeholten Änderungen ein Aufschließen sie einzeln markiert, statt sie
    zusammenzufassen — gezeichnet sind vier, ein Board mit hundert Änderungen ist
    nicht gezeichnet.

27. **Die mitlaufende Dauer ist gezeichnet, ihr Takt nicht entschieden.**
    `B0326`, `B0328`, `B0335` und `B0362` haben die Dauer weggelassen, weil sie
    ohne Live-Kanal ab der ersten Sekunde falsch wäre — und weil „ein Sekundentakt
    je Karte über den Blazor-Kreislauf in jedem E2E-Lauf ein Wackelkandidat" wäre.
    Der erste Grund fällt mit `I0028` weg, **der zweite nicht**: ob die Dauer im
    Browser weiterzählt oder vom Server getaktet wird, entscheidet der Slice.
    Gezeichnet ist nur, dass sie dasteht und beim Abbruch anhält.

28. **Die Schnittebene ist gesetzt, nicht abgestimmt — und sie ist ein Regler,
    den kein Fertig-Kriterium verlangt.** `I0030` sagt nur „ihre Knoten stehen
    als Karten"; *welche* Knoten, sagt es nicht. Der ältere Satz bildet in beiden
    Varianten **Bubbles** auf Karten ab (`B0043`, `B0019`); angewandt auf
    `Dokumentation/Planung/kanbanc.md` wären das 431 Karten aus einer Datei.
    `D0008.dc.html`, Zustand 3 setzt deshalb die **Interaction** als Vorgabe —
    die Einheit, auf der `/implementierung im-pair` arbeitet und die `/github`
    als ein Issue projiziert — und macht die Wahl als Regler sichtbar, samt der
    Zahl, die aus ihr folgt. Zwei Dinge bleiben offen: ob der Regler überhaupt
    gebraucht wird oder ob „Interaction" für immer gilt, und was mit einer WBS
    geschieht, die **oberhalb** der Schnittebene keinen Dialog hat (eine
    Application mit nur einer Interaction hat nach `/planung anlegen` keinen
    Dialog-Knoten — dann fehlt das Etikett, und das ist gezeichnet nicht).

29. **Die Kupplung zwischen Karte und WBS-Knoten hängt an einem Feld, das der
    Mensch löschen darf.** `D0008.dc.html`, Zustand 4 trägt die Herkunft als
    **Dateiverweis** `Dokumentation/Planung/kanbanc.md#I0001` — der einzige Ort
    im Bestand, der eine Herkunft trägt, und zugleich der Weg vom Board zurück
    in die Quelle (`I0019` nennt Planungsdateien wörtlich). `I0019` erlaubt aber
    auch das **Entfernen** eines Dateiverweises; wer ihn löscht, bekommt beim
    nächsten Lauf eine zweite Karte für denselben Knoten. Geprüft und verworfen:
    die Wiedererkennung am Titelpräfix `[I0001]` — auch der Titel ist änderbar,
    und er führt nirgendwohin zurück. Ein **eigenes Feld** an der Karte wäre der
    saubere Träger, hat aber keinen Knoten; das ist ein Befund für `/planung`,
    keine Lücke zum Auffüllen — dieselbe Haltung wie bei den Fragen 15 und 17.

30. **Der zweite Lauf überschreibt Haken, die jemand am Board gesetzt hat.**
    `D0008.dc.html`, Zustand 6 trennt „was die Datei nachzieht" von „was das
    Board behält" und stellt die Teilaufgaben samt ihren Haken auf die Seite der
    Datei — mit Beleg: die Projekt-CLAUDE.md sagt „**Die WBS ist die
    Fortschrittswahrheit**; weicht eine andere Liste ab, hat die WBS recht". Der
    Preis ist real und nicht gezeichnet: hakt ein Mensch am Board eine
    Teilaufgabe ab und importiert danach dieselbe unveränderte Datei, ist der
    Haken wieder weg. Ob das gewollt ist (die Datei hat recht) oder ob der Import
    Haken nur **setzen** und nie zurücknehmen darf, entscheidet der Slice.

31. **Der Bericht ist flüchtig, und ob er das bleiben darf, ist offen.**
    `I0032` sagt „**nach dem Import** ist ablesbar, was angelegt, geändert und
    übersprungen wurde" — nicht „jederzeit nachlesbar". Gezeichnet ist deshalb
    Schritt 3 als Bericht des gerade gelaufenen Vorgangs, dazu die Zeile „zuletzt
    eingefahren · am 02.09. · 37 Karten" im Layout-Modus als einziger bleibender
    Rest. Ein **Archiv der Läufe** hätte keinen Knoten (dieselbe Lage wie der
    Verlauf der Karte, Frage 15). Wer die Seite verlässt, bevor er den Bericht
    gelesen hat, bekommt ihn nicht wieder — das ist eine Entscheidung, die der
    Slice ausdrücklich treffen sollte, statt sie zu erben.
