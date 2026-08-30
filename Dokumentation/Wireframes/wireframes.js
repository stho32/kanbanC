/* KanbanC — Wireframes (Haltung: Kanbanflow-dicht). Low-fi Strukturskizzen, aus README, R00000-vision.md und Dokumentation/Planung/kanbanc.md abgeleitet. */

/* ── Bausteine ───────────────────────────────────────────── */
const bar = (w, h = 7) => `<span class="bar" style="width:${w}%;height:${h}px"></span>`;
const bars = (...w) => `<span class="bars">${w.map((x) => bar(x)).join('')}</span>`;
const chip = (t, k = '') => `<span class="chip ${k}">${t}</span>`;
const knopf = (t, k = '') => `<span class="wbtn ${k}">${t}</span>`;
const feld = (t) => `<span class="wfeld">${t}</span>`;
const av = (t, k = 'mensch') => `<span class="wav wav-${k}">${t}</span>`;
const bot = av('KI', 'agent');

const wkarte = (o = {}) => `<div class="wkarte${o.laeuft ? ' wkarte-laeuft' : ''}${o.klein ? ' wkarte-klein' : ''}">
  <div class="wkarte-kopf">${chip(o.nr || 'WBS-14', 'chip-nr')}${o.laeuft ? chip('⏱ 2:14 läuft', 'chip-live') : (o.zeit ? `<span class="mini">${o.zeit}</span>` : '')}</div>
  ${o.titel ? `<p class="wtitel">${o.titel}</p>` : bars(96, 62)}
  <div class="wkarte-fuss">${o.agent ? bot : av('ST')}${o.tasks ? `<span class="mini">☑ ${o.tasks}</span>` : ''}${o.etikett ? chip(o.etikett) : ''}<span class="mini rechts">${o.soll || '3:00 Soll'}</span></div>
</div>`;

const spalte = (name, zahl, karten, extra = '') => `<section class="wspalte">
  <header class="wspalte-kopf"><b>${name}</b><span class="wzahl">${zahl}</span></header>
  <div class="wspalte-karten">${karten}</div>
  <span class="wneu">+ Karte</span>${extra}</section>`;

const rahmen = (o) => `<figure class="frame${o.wide ? ' wide' : ''}">
  <figcaption><span class="vk">${o.vk}</span><b>${o.titel}</b><span class="frame-sub">${o.sub}</span></figcaption>
  <div class="box">${o.inhalt}</div>
  <ul class="anno">${o.anno.map((a) => `<li>${a}</li>`).join('')}</ul>
</figure>`;

const kopfzeile = (aktiv, zusatz = '') => `<div class="wnav"><span class="wmarke">KanbanC</span>
  <span class="wnav-links">${['Boards', 'Auswertungen', 'Kontributoren'].map((t) => `<span class="${t === aktiv ? 'an' : ''}">${t}</span>`).join('')}</span>
  ${zusatz}<span class="wich">${av('ST')} Stefan ▾</span></div>`;

const liveMarke = '<span class="wlive">● live</span>';

/* ── Schirme ─────────────────────────────────────────────── */
const S = {};

S.start = {
  titel: 'Start — Board-Übersicht',
  hinweis: 'Alphabetisch sortiert (B0016), Linien- und Projektboards unterschieden, direkter Weg ins Board (/boards/{BoardId}).',
  frames: [
    rahmen({
      vk: 'A', titel: 'Zwei Bänder: Linie / Projekt', sub: 'Art ist die Hauptordnung',
      anno: ['Boardart als Bandüberschrift — Linienboards ohne Ende, Projektboards mit Auslaufdatum', 'Kachel zeigt offene/gesamte Karten und beteiligte Kontributoren, Agenten mit KI-Marke', 'Anlegen sitzt oben rechts, das Formular kommt als Patch in derselben Seite', 'Archivierte Boards nur über den Filter erreichbar (I0005)'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wseite">
          <div class="wzeile"><h4>Boards</h4><span class="rechts">${feld('suchen')} ${knopf('+ Board anlegen', 'prim')}</span></div>
          <p class="wgruppe">Linienboards — laufen ohne Ende</p>
          <div class="wgitter">${['Betrieb & Wartung', 'Beschaffung'].map((n) => `<div class="wkachel"><b>${n}</b>${bars(70)}<div class="wkachel-fuss">${av('ST')}${bot}<span class="mini rechts">5 / 12 offen</span></div></div>`).join('')}</div>
          <p class="wgruppe">Projektboards — laufen mit dem Vorhaben aus</p>
          <div class="wgitter">${['KanbanC — Release 2', 'WBS-Experiment'].map((n, i) => `<div class="wkachel${i === 0 ? ' wkachel-an' : ''}"><b>${n}</b>${bars(84, 55)}<div class="wkachel-fuss">${av('ST')}${bot}<span class="mini rechts">bis 30.09.</span></div></div>`).join('')}</div>
          <p class="wfuss mini">Filter: ${chip('aktive')} ${chip('archivierte')} · ${knopf('Board importieren')} ${knopf('Board exportieren')}</p>
        </div>`
    }),
    rahmen({
      vk: 'B', titel: 'Eine Liste, Zustand vorn', sub: 'Was gerade läuft, steht oben',
      anno: ['Kopf zeigt laufende Timer über alle Boards (I0027) — der Einstieg für Mensch und Agent', 'Eine Tabelle statt Kacheln: Name, Art, offene Karten, letzte Bewegung, wer sie ausgelöst hat', 'Live-Punkt in der Zeile blinkt, wenn ein Agent gerade dort arbeitet', 'Dichter — trägt 20+ Boards ohne Scrollen'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wseite">
          <div class="wbanner">⏱ 2 Timer laufen — ${bot} Claude auf <b>WBS-14</b> · ${av('ST')} Stefan auf <b>WBS-21</b> ${liveMarke}</div>
          <div class="wzeile"><h4>Boards</h4><span class="rechts">${knopf('+ Board anlegen', 'prim')}</span></div>
          <table class="wtab"><thead><tr><th>Board</th><th>Art</th><th>offen</th><th>letzte Bewegung</th></tr></thead><tbody>
            ${[['Beschaffung', 'Linie', '3', 'vor 3 Std · Stefan'], ['Betrieb & Wartung', 'Linie', '5', 'vor 1 Std · KI'], ['KanbanC — Release 2', 'Projekt', '18', 'vor 6 Min · KI'], ['WBS-Experiment', 'Projekt', '41', 'gestern · Stefan']].map((r) => `<tr><td><b>${r[0]}</b></td><td>${chip(r[1])}</td><td>${r[2]}</td><td class="mini">${r[3]}</td></tr>`).join('')}
          </tbody></table>
        </div>`
    })
  ]
};

const kartenA = wkarte({ nr: 'WBS-14', titel: 'WBS-Import: Markdown-Baum in Karten überführen', laeuft: true, agent: true, tasks: '2/4', soll: '5:00 Soll' })
  + wkarte({ nr: 'WBS-21', titel: 'Timer starten und stoppen', tasks: '1/2', zeit: '1:36' })
  + wkarte({ nr: 'BUG-05', titel: 'SignalR bricht nach Standby', agent: true, etikett: 'Bug', zeit: '0:51' });

S.board = {
  titel: 'Board',
  hinweis: 'Spaltenbahnen (B0020), Kartenzahl je Spalte einschaltbar (I0004), Abschlussspalte nach Datum gruppiert mit Anzeigegrenze N (I0013). Keine WIP-Limits, keine Swimlanes — in der Planung ausdrücklich verworfen.',
  frames: [
    rahmen({
      wide: true, vk: 'A', titel: 'Bahnen + Aktivitätsspur rechts', sub: 'Der Agent ist im Blick, ohne die Karten zu verdrängen',
      anno: ['Rechte Spur führt Live-Ereignisse und laufende Timer — je Eintrag steht, ob Oberfläche oder API gehandelt hat', 'Karten mit laufendem Agenten-Timer pulsieren und tragen die Zeit oben rechts', 'Abschlussspalte gruppiert nach Erledigungsdatum, Fuß zeigt „20 neueste · älteres nachladen“', 'Spaltenpflege liegt eingeklappt unter den Bahnen (B0032), nicht in einem Panel'],
      inhalt: `${kopfzeile('Boards', liveMarke)}
        <div class="wboard">
          <div class="wboard-kopf"><b>KanbanC — Release 2</b>${chip('Projektboard')}<span class="mini">bis 30.09.2026</span>
            <span class="rechts">Klasse: ${chip('alle', 'chip-an')}${chip('WBS')}${chip('Bugs')}${chip('Beschaffung')} ${knopf('Kartenzahl anzeigen')}</span></div>
          <div class="wboard-koerper">
            <div class="wbahnen">
              ${spalte('Rückstand', '11', wkarte({ nr: 'WBS-31', titel: 'Klassen-Nummerierung über die API', tasks: '0/2', soll: '3:00' }) + wkarte({ nr: 'BES-04', titel: 'Playwright-Lizenz klären', soll: '0:30' }))}
              ${spalte('Bereit', '6', wkarte({ nr: 'WBS-28', titel: 'Anzeigegrenze im Spaltenkopf', tasks: '0/2', soll: '2:00' }) + wkarte({ nr: 'BUG-07', titel: 'Timer läuft nach Reload doppelt', etikett: 'Bug', soll: '0:45' }))}
              ${spalte('In Arbeit', '3', kartenA)}
              ${spalte('Prüfung', '1', wkarte({ nr: 'WBS-11', titel: 'Kontributoren anlegen', tasks: '2/2', zeit: '2:18' }))}
              ${spalte('Fertig ✓', '20+', `<p class="wdatum">Heute</p>${wkarte({ nr: 'WBS-09', titel: 'Boards anlegen', klein: true })}<p class="wdatum">Gestern</p>${wkarte({ nr: 'WBS-06', titel: 'Migrationen idempotent', klein: true })}`, '<span class="wnachladen mini">20 neueste · älteres nachladen</span>')}
            </div>
            <aside class="wspur"><b class="mini">Aktivität ${liveMarke}</b>
              ${[[bot, 'bewegt <b>WBS-14</b> nach In Arbeit', 'vor 6 Min · API'], [bot, 'erfasst Zeit auf <b>WBS-14</b>', 'läuft · API'], [av('ST'), 'gibt <b>WBS-11</b> zur Prüfung', 'vor 24 Min · Oberfläche'], [bot, 'legt <b>BUG-05</b> an', 'vor 41 Min · API']].map((e) => `<div class="wspur-e">${e[0]}<div><p class="mini">${e[1]}</p><span class="mini leise">${e[2]}</span></div></div>`).join('')}
              <div class="wspur-timer mini"><b>Laufende Timer</b><p>${bot} WBS-14 · 2:14</p><p>${av('ST')} WBS-21 · 0:12</p></div></aside>
          </div>
          <div class="wpflege mini">▸ Spalten gestalten — anlegen, umbenennen, ↑↓, entfernen, Abschlussspalte + Anzeigegrenze</div>
        </div>`
    }),
    rahmen({
      wide: true, vk: 'B', titel: 'Volle Breite, Live-Leiste oben', sub: 'Maximale Bahnenbreite, Ereignisse als Laufband',
      anno: ['Kein rechtes Panel — die Bahnen bekommen die ganze Breite, gut für sechs und mehr Spalten', 'Live-Ereignisse laufen als schmale Leiste unter dem Kopf; Klick klappt die volle Spur auf', 'Karte trägt Etiketten und Fälligkeit als kleine Marken, Agenten-Zeile bleibt gepulst', 'Spaltenpflege offen unter den Bahnen — Gestalten ist Alltag, nicht Sonderfall'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wboard">
          <div class="wboard-kopf"><b>KanbanC — Release 2</b>${chip('Projektboard')}<span class="rechts">${feld('Karte suchen')} ${knopf('+ Karte', 'prim')}</span></div>
          <div class="wticker mini">${liveMarke} ${bot} <b>WBS-14</b> → In Arbeit · vor 6 Min &nbsp;|&nbsp; ${av('ST')} <b>WBS-11</b> → Prüfung &nbsp;|&nbsp; ${bot} Zeit läuft auf <b>WBS-14</b> <span class="rechts">▾ ganze Spur</span></div>
          <div class="wbahnen wbahnen-breit">
            ${spalte('Rückstand', '11', wkarte({ nr: 'WBS-31', titel: 'Klassen-Nummerierung über die API', soll: '3:00' }) + wkarte({ nr: 'WBS-32', titel: 'Export der Ist-Zeiten als CSV', soll: '1:30' }) + wkarte({ nr: 'BES-04', titel: 'Playwright-Lizenz klären', soll: '0:30' }))}
            ${spalte('Bereit', '6', wkarte({ nr: 'WBS-28', titel: 'Anzeigegrenze im Spaltenkopf', soll: '2:00' }) + wkarte({ nr: 'BUG-07', titel: 'Timer doppelt nach Reload', etikett: 'Bug', soll: '0:45' }))}
            ${spalte('In Arbeit', '3', kartenA)}
            ${spalte('Prüfung', '1', wkarte({ nr: 'WBS-11', titel: 'Kontributoren anlegen', tasks: '2/2', zeit: '2:18' }))}
            ${spalte('Fertig ✓', '20+', `<p class="wdatum">Heute</p>${wkarte({ nr: 'WBS-09', titel: 'Boards anlegen', klein: true })}${wkarte({ nr: 'BES-01', titel: 'SQLite-Ablage', klein: true })}`, '<span class="wnachladen mini">Anzeigegrenze 20 · nachladen</span>')}
          </div>
          <div class="wpflege wpflege-auf">
            <b class="mini">Spalten gestalten</b>
            <div class="wpflege-zeilen mini">${['Rückstand', 'Bereit', 'In Arbeit', 'Prüfung', 'Fertig ✓ · Abschluss, Grenze 20'].map((n) => `<span class="wpflege-zeile">☰ ${n} <span class="rechts">↑ ↓ ✎ ✕</span></span>`).join('')}
              <span class="wpflege-zeile">${feld('neue Spalte')} ${knopf('anlegen', 'prim')}</span></div></div>
        </div>`
    }),
    rahmen({
      wide: true, vk: 'C', titel: 'Zeilen statt Bahnen', sub: 'Sehr dicht — für 40+ Karten und den Blick auf Zeiten',
      anno: ['Spalte wird zur Gruppe, Karte zur Zeile: Nummer, Titel, Klasse, Verantwortlicher, Soll/Ist, Fälligkeit', 'Alles auf einen Blick, Sortieren und Filtern nach Klasse gehört hierher — nahe an der API-Sicht', 'Bewegen per Menü in der Zeile statt per Zug — barrierefreier, aber weniger Kanban-Gefühl', 'Bahnen bleiben als Umschalter erreichbar: ▤ Zeilen / ▥ Bahnen'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wboard">
          <div class="wboard-kopf"><b>KanbanC — Release 2</b><span class="rechts">${chip('▤ Zeilen', 'chip-an')}${chip('▥ Bahnen')} Klasse: ${chip('WBS', 'chip-an')}${chip('Bugs')} ${knopf('CSV')}</span></div>
          <div class="wbanner mini">⏱ ${bot} WBS-14 · 2:14 &nbsp; ${av('ST')} WBS-21 · 0:12 ${liveMarke}</div>
          <table class="wtab wtab-dicht"><thead><tr><th>Nr</th><th>Karte</th><th>Klasse</th><th>wer</th><th>Ist / Soll</th><th>fällig</th><th></th></tr></thead><tbody>
            <tr class="wgruppen-zeile"><td colspan="7">In Arbeit · 3</td></tr>
            ${[['WBS-14', 'WBS-Import: Markdown-Baum in Karten überführen', 'WBS', bot, '3:34 / 5:00', '02.09.', 1], ['WBS-21', 'Timer starten und stoppen', 'WBS', av('ST'), '1:36 / 2:30', '03.09.', 0], ['BUG-05', 'SignalR bricht nach Standby', 'Bugs', bot, '0:51 / 1:00', '—', 0]].map((r) => `<tr class="${r[6] ? 'wzeile-laeuft' : ''}"><td><b>${r[0]}</b></td><td>${r[1]}${r[6] ? chip('⏱ läuft', 'chip-live') : ''}</td><td>${chip(r[2])}</td><td>${r[3]}</td><td>${r[4]}</td><td class="mini">${r[5]}</td><td class="mini">⋯</td></tr>`).join('')}
            <tr class="wgruppen-zeile"><td colspan="7">Bereit · 6</td></tr>
            ${[['WBS-28', 'Anzeigegrenze im Spaltenkopf', 'WBS', av('ST'), '0:00 / 2:00', '05.09.'], ['BUG-07', 'Timer läuft nach Reload doppelt', 'Bugs', av('–', 'leer'), '0:00 / 0:45', '—']].map((r) => `<tr><td><b>${r[0]}</b></td><td>${r[1]}</td><td>${chip(r[2])}</td><td>${r[3]}</td><td>${r[4]}</td><td class="mini">${r[5]}</td><td class="mini">⋯</td></tr>`).join('')}
            <tr class="wgruppen-zeile"><td colspan="7">Fertig ✓ · 20 neueste, nach Datum</td></tr>
          </tbody></table>
        </div>`
    })
  ]
};

const zeitenTab = `<table class="wtab mini"><tbody>
  <tr><td>${bot} Claude</td><td>heute 09:12</td><td class="rechts">2:12</td></tr>
  <tr><td>${av('ST')} Stefan</td><td>gestern 17:40</td><td class="rechts">0:45</td></tr>
  <tr><td>${bot} Claude</td><td>gestern 11:05</td><td class="rechts">0:37</td></tr>
  <tr><td colspan="2">${feld('Zeit nachtragen')}</td><td class="rechts">${knopf('+')}</td></tr></tbody></table>`;

S.karte = {
  titel: 'Kartendetail',
  hinweis: 'Titel, Beschreibung, Verantwortlicher, Fälligkeit, Farbe, Etiketten (I0015), Subtasks (I0016), Kommentare (I0017), Anhänge und Dateiverweise (I0018/I0019), Klasse mit Nummer (I0021), Zeiten je Kontributor (I0026).',
  frames: [
    rahmen({
      vk: 'A', titel: 'Schublade über dem Board', sub: 'Board bleibt sichtbar, kurzer Weg zurück',
      anno: ['Von rechts, ~480px, Board bleibt links stehen und aktualisiert weiter', 'Timer als erste Handlung ganz oben — starten/stoppen in einem Klick', 'Verlauf am Fuß nennt zu jedem Schritt Kontributor und Quelle (Oberfläche / API)', 'Schwäche: wenig Platz für Beschreibung, Kommentare und Anhänge gleichzeitig'],
      inhalt: `<div class="wsplit"><div class="wsplit-links">${bars(80, 90, 60, 84, 70, 55, 88)}</div>
        <div class="wdrawer">
          <div class="wzeile">${chip('WBS-14', 'chip-nr')}<span class="mini">Klasse WBS</span><span class="rechts mini">✕</span></div>
          <h4>WBS-Import: Markdown-Baum in Karten überführen</h4>
          <div class="wtimer">${knopf('⏹ Timer stoppen', 'prim')}<b>3:34</b><span class="mini">läuft für ${bot} Claude</span></div>
          <div class="wpaar mini"><span>Verantwortlich ${bot} Claude</span><span>fällig 02.09.</span><span>Etiketten ${chip('Import')}</span><span>Farbe ▮</span></div>
          <p class="mini leise">Beschreibung</p>${bars(100, 92, 70)}
          <p class="mini leise">Subtasks 2/4</p>
          <div class="wtasks mini">${[['☑', 'Parser für die Gliederung'], ['☑', 'Idempotenter Abgleich'], ['☐', 'Klassenspezifische Nummerierung'], ['☐', 'Integrationstest']].map((t) => `<span>${t[0]} ${t[1]}</span>`).join('')}</div>
          <p class="mini leise">Zeiten je Kontributor · Summe 3:34</p>${zeitenTab}
          <p class="mini leise">Verlauf</p>
          <div class="wverlauf mini"><span>${bot} nach In Arbeit bewegt · vor 6 Min · API</span><span>${bot} Subtask erledigt · vor 22 Min · API</span><span>${av('ST')} Soll auf 5:00 gesetzt · gestern · Oberfläche</span></div>
        </div></div>`
    }),
    rahmen({
      vk: 'B', titel: 'Modal, zwei Spalten', sub: 'Inhalt links, Zeit und Verlauf rechts',
      anno: ['Breiter: Beschreibung, Subtasks, Kommentare und Anhänge stehen ungedrängt links', 'Rechte Spalte bündelt Zeiterfassung, Zeiteinträge, Verlauf — die Agenten-Seite der Karte', 'Kopfzeile trägt Nummer, Klasse und Spaltenwechsler; Bewegen ohne Zurück zum Board', 'Schwäche: verdeckt das Board, Live-Änderungen dahinter bleiben unbemerkt'],
      inhalt: `<div class="wmodal-um"><div class="wmodal">
        <div class="wzeile">${chip('WBS-14', 'chip-nr')}${chip('WBS')}<span class="mini">Spalte: ${feld('In Arbeit ▾')}</span><span class="rechts mini">✕</span></div>
        <h4>WBS-Import: Markdown-Baum in Karten überführen</h4>
        <div class="wmodal-koerper">
          <div>
            <p class="mini leise">Beschreibung</p>${bars(100, 94, 88, 60)}
            <p class="mini leise">Subtasks 2/4</p><div class="wtasks mini">${['☑ Parser', '☑ Abgleich idempotent', '☐ Nummerierung', '☐ Integrationstest'].map((t) => `<span>${t}</span>`).join('')}</div>
            <p class="mini leise">Kommentare</p><div class="wverlauf mini"><span>${bot} „Parser liest jetzt auch Ebene 4.“</span><span>${feld('Kommentar schreiben')}</span></div>
            <p class="mini leise">Dateien & Verweise</p><div class="wtasks mini"><span>📎 wbs-export.md</span><span>↗ Dokumentation/Planung/kanbanc.md</span></div>
          </div>
          <div class="wmodal-rechts">
            <div class="wtimer">${knopf('▶ Timer', 'prim')}<b>3:34</b></div>
            <p class="mini leise">Zeiten</p>${zeitenTab}
            <p class="mini leise">Verlauf</p><div class="wverlauf mini"><span>${bot} → In Arbeit · API</span><span>${av('ST')} Soll 5:00 · Oberfläche</span><span>${av('ST')} aus WBS importiert</span></div>
          </div>
        </div></div></div>`
    }),
    rahmen({
      vk: 'C', titel: 'Eigene Seite /karten/WBS-14', sub: 'Verlinkbar — Mensch und Agent zeigen auf dieselbe Adresse',
      anno: ['Eigene Route, reload- und teilbar; ein Agent kann im Kommentar auf die Karte verweisen', 'Verlaufsspur läuft links als Zeitachse durch — wer, wann, über welche Grenze', 'Platz für alles: Beschreibung, Subtasks, Kommentare, Anhänge, Zeittabelle', 'Schwäche: Kontextwechsel weg vom Board, Rückweg über Brotkrumen'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wseite">
          <p class="mini leise">Boards / KanbanC — Release 2 / ${chip('WBS-14', 'chip-nr')}</p>
          <div class="wsplit-2">
            <div class="wachse">
              <p class="mini leise">Verlauf</p>
              ${[[bot, 'nach In Arbeit', 'vor 6 Min · API'], [bot, 'Subtask erledigt', 'vor 22 Min · API'], [av('ST'), 'Soll 5:00', 'gestern · Oberfläche'], [av('ST'), 'aus WBS importiert', '29.08. · Oberfläche']].map((e) => `<div class="wachse-e">${e[0]}<div><p class="mini">${e[1]}</p><span class="mini leise">${e[2]}</span></div></div>`).join('')}
            </div>
            <div>
              <h4>WBS-Import: Markdown-Baum in Karten überführen</h4>
              <div class="wtimer">${knopf('⏹ stoppen', 'prim')}<b>3:34 / 5:00</b><span class="mini">${bot} Claude</span></div>
              <div class="wpaar mini"><span>Klasse WBS · Nr 14</span><span>fällig 02.09.</span><span>${chip('Import')}</span></div>
              ${bars(100, 92, 78)}
              <p class="mini leise">Subtasks</p><div class="wtasks mini">${['☑ Parser', '☑ Abgleich', '☐ Nummerierung', '☐ Test'].map((t) => `<span>${t}</span>`).join('')}</div>
              <p class="mini leise">Zeiten je Kontributor</p>${zeitenTab}
            </div>
          </div>
        </div>`
    })
  ]
};

S.gestalten = {
  titel: 'Board anlegen & gestalten',
  hinweis: 'Board mit Name und Art (I0001), Standardspalten (B0001), Spalten anlegen/umbenennen/umsortieren/entfernen, Abschlussspalte mit Anzeigegrenze (I0003), Klassen mit Nummernkreis-Präfix (I0020).',
  frames: [
    rahmen({
      vk: 'A', titel: 'Anlegen als Patch in der Liste', sub: 'Zwei Felder, Vorschau der Standardspalten',
      anno: ['Formular öffnet in der Liste, kein eigener Schirm — Name, Art, fertig', 'Vorschau zeigt die drei Standardspalten, die mit angelegt werden', 'Zurückweisung erscheint direkt am Feld, lesbar formuliert (F0002)', 'Projektboard klappt zusätzlich Start- und Enddatum auf'],
      inhalt: `<div class="wseite">
        <div class="wzeile"><h4>Board anlegen</h4><span class="rechts mini">✕</span></div>
        <p class="mini leise">Name</p>${feld('KanbanC — Release 3')}
        <p class="wfehler mini">⚠ Ein Board mit diesem Namen gibt es schon.</p>
        <p class="mini leise">Art</p><div class="wradio mini"><span>◉ Linienboard — ohne Ende</span><span>○ Projektboard — mit Auslauf</span></div>
        <p class="mini leise">Diese Spalten entstehen mit</p>
        <div class="wvorschau">${['Rückstand', 'In Arbeit', 'Fertig ✓'].map((n) => `<span class="wvor-sp">${n}</span>`).join('')}</div>
        <div class="wzeile">${knopf('Anlegen', 'prim')}${knopf('Abbrechen')}<span class="rechts mini">gleiche Wirkung: POST /api/boards</span></div>
      </div>`
    }),
    rahmen({
      vk: 'B', titel: 'Gestalten unter dem Board', sub: 'Spalten und Klassen an einer Stelle',
      anno: ['Sitzt unter den Bahnen auf /boards/{BoardId} — gestalten, ohne das Board zu verlassen', 'Spaltenzeile: ziehen oder ↑↓, umbenennen, entfernen; genau eine ist Abschlussspalte mit Grenze N', 'Klassen tragen Name und Nummernkreis-Präfix — daraus entsteht WBS-14, BUG-07', 'Jede Zeile nennt den gleichwertigen API-Aufruf; das hält die Vollständigkeit sichtbar'],
      inhalt: `<div class="wseite">
        <div class="wzeile"><h4>Board gestalten</h4><span class="rechts mini">KanbanC — Release 2</span></div>
        <p class="mini leise">Spalten</p>
        <div class="wpflege-zeilen mini">${[['Rückstand', ''], ['Bereit', ''], ['In Arbeit', ''], ['Prüfung', ''], ['Fertig ✓', 'Abschlussspalte · Anzeigegrenze ' + feld('20')]].map((r) => `<span class="wpflege-zeile">☰ ${r[0]} <span class="rechts">${r[1]} ↑ ↓ ✎ ✕</span></span>`).join('')}
          <span class="wpflege-zeile">${feld('neue Spalte')} ${knopf('anlegen', 'prim')}</span></div>
        <p class="mini leise">Klassen — Name und Nummernkreis</p>
        <div class="wpflege-zeilen mini">${[['WBS', 'WBS-', '31 vergeben'], ['Bugmeldungen', 'BUG-', '7 vergeben'], ['Beschaffung', 'BES-', '4 vergeben']].map((r) => `<span class="wpflege-zeile">${r[0]} <span class="rechts">${chip(r[1])} ${r[2]} ✎ ✕</span></span>`).join('')}
          <span class="wpflege-zeile">${feld('Klasse')} ${feld('Präfix')} ${knopf('anlegen', 'prim')}</span></div>
        <p class="mini leise">Board</p><div class="wzeile mini">${knopf('umbenennen')}${knopf('archivieren')}${knopf('exportieren')}<span class="rechts">Kartenzahl im Spaltenkopf: ${chip('an', 'chip-an')}${chip('aus')}</span></div>
      </div>`
    })
  ]
};

S.import = {
  titel: 'WBS-Import',
  hinweis: 'Datei einlesen, Knoten als Karten der Klasse WBS (I0030); erneuter Import aktualisiert statt zu duplizieren (I0031); ablesbar, was angelegt, geändert und übersprungen wurde (I0032).',
  frames: [
    rahmen({
      vk: 'A', titel: 'Drei Schritte mit Vorschau', sub: 'Erst zeigen, dann schreiben',
      anno: ['Schritt 1 Datei, Schritt 2 Vorschau, Schritt 3 Bericht — nichts wird ohne Vorschau geschrieben', 'Vorschau zählt getrennt: angelegt / geändert / übersprungen, jede Zeile aufklappbar', 'Zielboard und Klasse werden oben gewählt, Nummern kommen aus dem Nummernkreis der Klasse', 'Zweiter Import derselben Datei zeigt fast nur „unverändert“ — der Idempotenz-Beweis für das Auge'],
      inhalt: `<div class="wseite">
        <div class="wschritte mini"><span class="an">1 Datei</span><span class="an">2 Vorschau</span><span>3 Bericht</span></div>
        <div class="wzeile mini">Ziel: ${feld('KanbanC — Release 2 ▾')} Klasse: ${feld('WBS ▾')} <span class="rechts">📄 kanbanc.md · 128 Zeilen</span></div>
        <div class="wbilanz">${[['12', 'angelegt'], ['5', 'geändert'], ['29', 'unverändert'], ['1', 'übersprungen']].map((b) => `<span class="wbilanz-e"><b>${b[0]}</b>${b[1]}</span>`).join('')}</div>
        <table class="wtab wtab-dicht mini"><tbody>
          ${[['+', 'B0043', 'Puffer-Verbrauch rechnen', 'angelegt → WBS-32'], ['~', 'B0020', 'Spaltenbahnen', 'Soll 0,4 → 0,8'], ['=', 'B0019', 'Board-Seite mit Route', 'unverändert'], ['!', 'B0044', '— ohne Fertig-Kriterium', 'übersprungen']].map((r) => `<tr><td><b>${r[0]}</b></td><td>${r[1]}</td><td>${r[2]}</td><td class="mini">${r[3]}</td></tr>`).join('')}
        </tbody></table>
        <div class="wzeile">${knopf('Import ausführen', 'prim')}${knopf('Abbrechen')}<span class="rechts mini">gleiche Wirkung: POST /api/import/wbs</span></div>
      </div>`
    }),
    rahmen({
      vk: 'B', titel: 'Baum links, Wirkung rechts', sub: 'Auswahl im Original, Folge daneben',
      anno: ['Links die WBS in ihrer Gliederung — Dialog, Interaction, Feature, Bubble, wie in der Datei', 'Häkchen je Teilbaum: nur der WBS-Ast, der wirklich aufs Board soll', 'Rechts wandert die Wirkung mit: welche Karte entsteht, welche wird berührt', 'Teurer zu bauen, aber der einzige Weg, Teilimporte zu steuern'],
      inhalt: `<div class="wseite">
        <div class="wzeile"><h4>WBS importieren</h4><span class="rechts mini">📄 kanbanc.md</span></div>
        <div class="wsplit-2">
          <div class="wbaum mini">${[[0, '☑ A0001 KanbanC'], [1, '☑ D0001 Boards führen'], [2, '☑ I0001 Board anlegen'], [3, '☑ F0001 Board anlegen und abrufen'], [4, '☑ B0001 Standardspalten erzeugen'], [4, '☑ B0002 Schema anlegen'], [1, '☐ D0006 Zeiterfassung'], [2, '☐ I0023 Timer starten']].map((z) => `<span style="padding-left:${z[0] * 14}px">${z[1]}</span>`).join('')}</div>
          <div>
            <p class="mini leise">Wirkung der Auswahl</p>
            <div class="wbilanz">${[['12', 'angelegt'], ['5', 'geändert'], ['1', 'übersprungen']].map((b) => `<span class="wbilanz-e"><b>${b[0]}</b>${b[1]}</span>`).join('')}</div>
            <div class="wtasks mini">${['+ WBS-32 Standardspalten erzeugen', '+ WBS-33 Schema anlegen', '~ WBS-14 Soll 0,4 → 0,8', '! B0044 ohne Fertig-Kriterium'].map((t) => `<span>${t}</span>`).join('')}</div>
            <p class="mini leise">Zielspalte für neue Karten</p>${feld('Rückstand ▾')}
            ${knopf('Auswahl importieren', 'prim')}
          </div>
        </div>
      </div>`
    })
  ]
};

const burndown = `<svg viewBox="0 0 300 120" class="wchart"><polyline points="10,20 60,32 110,50 160,58 210,74 260,96" class="soll"/><polyline points="10,22 60,40 110,44 160,70 210,66 260,52" class="ist"/><line x1="10" y1="110" x2="290" y2="110"/><line x1="10" y1="10" x2="10" y2="110"/></svg>`;
const fieber = `<svg viewBox="0 0 300 120" class="wchart"><rect x="10" y="10" width="280" height="100" class="zone"/><polyline points="10,100 70,86 130,70 190,44 250,30" class="ist"/><line x1="10" y1="110" x2="290" y2="110"/></svg>`;

S.auswertung = {
  titel: 'Auswertungen',
  hinweis: 'Soll-Ist gegen die WBS-Zählung (I0033), Burndown (I0034), Puffer-Verbrauch / Critical Chain (I0035), Export (I0036), Rohdaten über die API (I0037).',
  frames: [
    rahmen({
      wide: true, vk: 'A', titel: 'Ein Blatt, vier Felder', sub: 'Alles nebeneinander, ohne Umschalten',
      anno: ['Burndown groß, Puffer-Fieberkurve daneben, Soll-Ist und Kontributor-Summen darunter', 'Filterzeile gilt für alle Felder: Board, Klasse, Zeitraum', 'Jedes Feld nennt seinen API-Pfad — die Auswertung ist auch ohne Oberfläche zu holen', 'Gefahr: vier Diagramme wollen erklärt werden; Untertitel je Feld ist Pflicht'],
      inhalt: `${kopfzeile('Auswertungen')}
        <div class="wseite">
          <div class="wzeile mini">Board ${feld('Release 2 ▾')} Klasse ${feld('WBS ▾')} Zeitraum ${feld('letzte 30 Tage ▾')}<span class="rechts">${knopf('CSV')}${knopf('API')}</span></div>
          <div class="wgitter-2">
            <div class="wfeldbox"><b class="mini">Burndown — Restumfang</b>${burndown}<span class="mini leise">Soll gestrichelt, Ist voll · GET /api/auswertungen/burndown</span></div>
            <div class="wfeldbox"><b class="mini">Puffer-Verbrauch</b>${fieber}<span class="mini leise">Fortschritt × verbrauchter Puffer · Critical Chain</span></div>
            <div class="wfeldbox"><b class="mini">Soll-Ist gegen die WBS-Zählung</b>
              <div class="wbalken">${[['B0019', 70, 96], ['B0020', 40, 55], ['WBS-14', 100, 72]].map((r) => `<span class="wbalken-z"><span class="mini">${r[0]}</span><span class="wb"><i style="width:${r[1]}%"></i></span><span class="wb wb-ist"><i style="width:${r[2]}%"></i></span></span>`).join('')}</div></div>
            <div class="wfeldbox"><b class="mini">Zeit je Kontributor</b>
              <table class="wtab mini"><tbody>${[[bot + ' Claude', '18:24', '46 %'], [av('ST') + ' Stefan', '16:10', '40 %'], [av('CX', 'agent') + ' Codex', '5:32', '14 %']].map((r) => `<tr><td>${r[0]}</td><td class="rechts">${r[1]}</td><td class="mini">${r[2]}</td></tr>`).join('')}</tbody></table></div>
          </div>
        </div>`
    }),
    rahmen({
      wide: true, vk: 'B', titel: 'Eine Auswertung, groß', sub: 'Umschalter links, viel Fläche fürs Diagramm',
      anno: ['Liste der Auswertungen links, gewählte füllt die Fläche — Details statt Übersicht', 'Unter dem Diagramm die Zahlen, aus denen es entsteht; direkt exportierbar', 'Der Umschalter wächst mit: eigene Auswertungen kommen als weiterer Eintrag dazu', 'Braucht zwei Klicks für den Vergleich zweier Auswertungen'],
      inhalt: `${kopfzeile('Auswertungen')}
        <div class="wsplit-3">
          <nav class="wseiten-nav mini">${['Burndown', 'Puffer-Verbrauch', 'Soll-Ist gegen WBS', 'Zeit je Kontributor', 'Rohdaten / API'].map((t, i) => `<span class="${i === 0 ? 'an' : ''}">${t}</span>`).join('')}</nav>
          <div class="wseite">
            <div class="wzeile"><h4>Burndown — Release 2</h4><span class="rechts mini">${feld('Klasse WBS ▾')} ${feld('30 Tage ▾')} ${knopf('CSV')}</span></div>
            <div class="wchart-gross">${burndown}</div>
            <div class="wzeile mini"><span>Rest 41 von 79 Knoten</span><span>Ist 40:06 h</span><span>Soll-Zählung 52,4</span><span class="rechts">Puffer 38 % verbraucht</span></div>
            <table class="wtab wtab-dicht mini"><thead><tr><th>Tag</th><th>Rest Soll</th><th>Rest Ist</th><th>erledigt</th><th>wer</th></tr></thead><tbody>
              ${[['28.08.', '52,4', '52,4', '—', '—'], ['29.08.', '48,0', '49,2', '3', av('ST') + ' ' + bot], ['30.08.', '43,6', '41,8', '5', bot]].map((r) => `<tr><td>${r[0]}</td><td>${r[1]}</td><td>${r[2]}</td><td>${r[3]}</td><td>${r[4]}</td></tr>`).join('')}
            </tbody></table>
          </div>
        </div>`
    })
  ]
};

S.zeiten = {
  titel: 'Zeiten je Kontributor',
  hinweis: 'Zeiteintrag nachtragen und ändern (I0025), Zeiten einer Karte und Summe je Kontributor (I0026), laufende Timer auf einen Blick (I0027), Export (I0036).',
  frames: [
    rahmen({
      vk: 'A', titel: 'Kreuztabelle Kontributor × Karte', sub: 'Summen in beide Richtungen',
      anno: ['Zeilen Karten, Spalten Kontributoren, Summen am Rand — die Grundlage der Schätz-Rückkopplung', 'Zelle anklickbar: darunter die einzelnen Einträge mit Beginn, Ende, Quelle', 'Kopf zeigt laufende Timer, sie zählen in der Tabelle sichtbar mit', 'Wird bei vielen Kontributoren breit — Spalten müssen zusammenklappen'],
      inhalt: `<div class="wseite">
        <div class="wbanner mini">⏱ läuft: ${bot} WBS-14 · 2:14 ${liveMarke}<span class="rechts">${knopf('CSV')}</span></div>
        <table class="wtab wtab-dicht mini"><thead><tr><th>Karte</th><th>${av('ST')}</th><th>${bot}</th><th>${av('CX', 'agent')}</th><th>Summe</th><th>Soll</th></tr></thead><tbody>
          ${[['WBS-14 Import', '0:45', '3:29', '—', '4:14', '5:00'], ['WBS-21 Timer', '1:36', '—', '—', '1:36', '2:30'], ['BUG-05 SignalR', '—', '—', '0:51', '0:51', '1:00'], ['WBS-11 Kontributoren', '2:18', '—', '—', '2:18', '2:00']].map((r) => `<tr><td>${r[0]}</td><td>${r[1]}</td><td>${r[2]}</td><td>${r[3]}</td><td><b>${r[4]}</b></td><td class="mini">${r[5]}</td></tr>`).join('')}
          <tr class="wgruppen-zeile"><td>Summe</td><td>4:39</td><td>3:29</td><td>0:51</td><td><b>8:59</b></td><td>10:30</td></tr>
        </tbody></table>
        <p class="mini leise">Einträge zu WBS-14 · aufgeklappt</p>
        <div class="wtasks mini">${['09:12 – 11:24 · ' + bot + ' · API', '17:40 – 18:25 · ' + av('ST') + ' · Oberfläche', feld('nachtragen: Karte, Kontributor, von, bis') + knopf('+')].map((t) => `<span>${t}</span>`).join('')}</div>
      </div>`
    }),
    rahmen({
      vk: 'B', titel: 'Kontributor wählen, Tage lesen', sub: 'Wie ein Stundenzettel — pro Person, pro Tag',
      anno: ['Links die Kontributoren mit Tagessumme, rechts deren Einträge nach Tag gruppiert', 'Nachtragen und Korrigieren am Ort — die Zeile wird zum Formular', 'Agenten stehen gleichberechtigt in derselben Liste, Quelle steht am Eintrag', 'Weniger Vergleich zwischen Personen, dafür saubere Korrektur-Arbeit'],
      inhalt: `<div class="wsplit-3">
        <nav class="wseiten-nav mini">${[[av('ST') + ' Stefan', '4:39'], [bot + ' Claude', '3:29'], [av('CX', 'agent') + ' Codex', '0:51'], [av('ML') + ' Maria', '0:00']].map((r, i) => `<span class="${i === 1 ? 'an' : ''}">${r[0]}<b class="rechts">${r[1]}</b></span>`).join('')}</nav>
        <div class="wseite">
          <div class="wzeile"><h4>${bot} Claude-Agent</h4><span class="rechts mini">${feld('30.08. ▾')} ${knopf('CSV')}</span></div>
          <div class="wbanner mini">⏱ läuft auf <b>WBS-14</b> seit 09:12 · 2:14 ${knopf('stoppen')}</div>
          <p class="mini leise">Heute</p>
          <div class="wtasks mini">${['09:12 – 11:24 · WBS-14 Import · API · 2:12 ✎', '08:40 – 09:10 · BUG-05 SignalR · API · 0:30 ✎'].map((t) => `<span>${t}</span>`).join('')}</div>
          <p class="mini leise">Gestern</p>
          <div class="wtasks mini">${['11:05 – 11:42 · WBS-14 Import · API · 0:37 ✎', feld('von') + feld('bis') + feld('Karte') + knopf('nachtragen', 'prim')].map((t) => `<span>${t}</span>`).join('')}</div>
        </div>
      </div>`
    })
  ]
};

S.kontributoren = {
  titel: 'Kontributoren & Identität',
  hinweis: 'Anlegen mit Art Mensch / Agent / abgebildet (I0006), bearbeiten (I0007), stilllegen (I0009), Identität wählen und in localStorage merken (I0008). Full-Trust: keine Anmeldung.',
  frames: [
    rahmen({
      vk: 'A', titel: 'Kontributoren führen', sub: 'Eine Liste, drei Arten',
      anno: ['Art als Marke: Mensch, Agent, abgebildet — nur die ersten zwei sind wählbare Identitäten', 'Stillgelegte rutschen nach unten, bleiben an alten Karten und Zeiten sichtbar', 'Zeile zeigt, wo der Kontributor gerade steht: offene Karten, erfasste Zeit, letzte Handlung', 'Anlegen als Zeile am Ende — kein eigener Schirm'],
      inhalt: `<div class="wseite">
        <div class="wzeile"><h4>Kontributoren</h4><span class="rechts mini">Full-Trust · keine Anmeldung</span></div>
        <table class="wtab mini"><thead><tr><th>Name</th><th>Art</th><th>offen</th><th>Zeit</th><th>letzte Handlung</th><th></th></tr></thead><tbody>
          ${[[av('ST') + ' Stefan', 'Mensch', '6', '16:10', 'vor 24 Min · Oberfläche'], [bot + ' Claude-Agent', 'Agent', '3', '18:24', 'vor 6 Min · API'], [av('CX', 'agent') + ' Codex-Agent', 'Agent', '1', '5:32', 'vor 41 Min · API'], [av('ML') + ' Maria Lenz', 'abgebildet', '2', '0:00', '—']].map((r) => `<tr><td>${r[0]}</td><td>${chip(r[1])}</td><td>${r[2]}</td><td>${r[3]}</td><td class="mini">${r[4]}</td><td class="mini">✎ ⏸</td></tr>`).join('')}
          <tr class="wgruppen-zeile"><td colspan="6">stillgelegt · 1</td></tr>
          <tr><td>${av('JR')} Jan R.</td><td>${chip('Mensch')}</td><td>0</td><td>2:10</td><td class="mini">seit 12.08.</td><td class="mini">↺</td></tr>
          <tr><td colspan="3">${feld('Name')} ${feld('Art ▾')}</td><td colspan="3">${knopf('anlegen', 'prim')}</td></tr>
        </tbody></table>
      </div>`
    }),
    rahmen({
      vk: 'B', titel: 'Identitätswahl beim Öffnen', sub: 'Ganzflächig, einmalig, groß',
      anno: ['Erster Aufruf im Browser: „Wer bist du?“ — große Kacheln, ein Klick, Wahl liegt in localStorage', 'Abgebildete Kontributoren sind hier nicht wählbar und deshalb nicht abgebildet', 'Agenten erscheinen zwar auf dem Board, aber nicht als Wahl — sie arbeiten über die API', 'Klarer Start, kostet aber einen Schirm vor dem eigentlichen Board'],
      inhalt: `<div class="wwahl">
        <h4>Wer bist du?</h4>
        <p class="mini leise">Full-Trust im LAN — die Wahl merkt sich dieser Browser und lässt sich jederzeit ändern.</p>
        <div class="wwahl-gitter">${[[av('ST'), 'Stefan'], [av('ML'), 'Maria Lenz'], [av('JR'), 'Jan R.'], ['+', 'anlegen']].map((k) => `<span class="wwahl-k">${k[0]}<b>${k[1]}</b></span>`).join('')}</div>
        <p class="mini leise">Agenten wählen sich nicht — sie arbeiten über die API und erscheinen trotzdem am Board.</p>
      </div>`
    }),
    rahmen({
      vk: 'C', titel: 'Identität in der Kopfzeile', sub: 'Kein Vorschirm — umschalten, wo man steht',
      anno: ['Kein eigener Schirm: die Kopfzeile trägt die Wahl, das Board kommt sofort', 'Ohne Wahl steht dort „nicht gewählt“ mit Hinweis — erst der Timer erzwingt eine Identität', 'Umschalten mitten in der Arbeit ist ein Klick — realistisch für einen Rechner, viele Menschen', 'Weniger deutlich: man arbeitet leichter versehentlich als jemand anderes'],
      inhalt: `${kopfzeile('Boards')}
        <div class="wpopover">
          <p class="mini leise">Ich bin …</p>
          ${[[av('ST'), 'Stefan', 'gewählt'], [av('ML'), 'Maria Lenz', ''], [av('JR'), 'Jan R.', ''], [bot, 'Claude-Agent', 'nur API'], [av('ML'), 'abgebildet: Team Einkauf', 'nicht wählbar']].map((r) => `<span class="wpop-z ${r[2] === 'gewählt' ? 'an' : ''} ${r[2] === 'nicht wählbar' || r[2] === 'nur API' ? 'aus' : ''}">${r[0]} ${r[1]} <span class="rechts mini">${r[2]}</span></span>`).join('')}
          <span class="wpop-z mini">${knopf('Kontributor anlegen')}</span>
        </div>
        <div class="wseite wseite-blass">${bars(70, 92, 60, 84)}<div class="wbahnen wbahnen-blass">${['Rückstand', 'Bereit', 'In Arbeit'].map((n) => spalte(n, '', wkarte({ klein: true }) + wkarte({ klein: true }))).join('')}</div></div>`
    })
  ]
};

/* ── Aufbau ──────────────────────────────────────────────── */
const ordnung = ['start', 'board', 'karte', 'gestalten', 'import', 'auswertung', 'zeiten', 'kontributoren'];

document.addEventListener('DOMContentLoaded', () => {
  const reiter = document.getElementById('reiter');
  const buehne = document.getElementById('buehne');
  reiter.innerHTML = ordnung.map((id, i) => `<button data-id="${id}" class="${i === 0 ? 'an' : ''}">${S[id].titel}</button>`).join('');
  buehne.innerHTML = ordnung.map((id, i) => `<section class="schirm${i === 0 ? ' on' : ''}" id="s-${id}">
    <header class="schirm-kopf"><h2>${S[id].titel}</h2><p>${S[id].hinweis}</p></header>
    <div class="frames">${S[id].frames.join('')}</div></section>`).join('');
  reiter.querySelectorAll('button').forEach((b) => b.addEventListener('click', () => {
    reiter.querySelectorAll('button').forEach((x) => x.classList.toggle('an', x === b));
    buehne.querySelectorAll('.schirm').forEach((s) => s.classList.toggle('on', s.id === 's-' + b.dataset.id));
    window.scrollTo(0, 0);
  }));
});
