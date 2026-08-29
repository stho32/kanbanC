# Datum und Zeit ohne Moment.js

> Ersatzcode und Ersetzungstabelle für den Abbau von Moment.js zugunsten der nativen `Date`-API und `Intl.DateTimeFormat`. Genutzt von `/upgrade momentjs` (Weg B: Entfernen). Code nach Skill `javascript-stil` (`const`/`let`, explizite Verzweigungen, ein Gedanke je Funktion).

## Problemstellung

Moment.js ist im Maintenance-Modus, ~70 KB groß und in älteren Versionen mit ReDoS- und Path-Traversal-Lücken behaftet. Die meisten Verwendungen sind einfaches Formatieren, Parsen, Addieren/Subtrahieren, Vergleichen und relative Zeit — dafür reicht die native API, ergänzt um wenige Hilfsfunktionen.

## Lösungsansatz

Alle Aufrufe auflisten, nach Funktionstyp gruppieren, Hilfsfunktionen einführen, einfachste Fälle zuerst ersetzen und nach jeder Änderung testen; zum Schluss Bibliothek, Script-Tags/Imports, `package.json`-Eintrag und `moment-timezone` entfernen.

Alternativen mit fast identischer API, falls Hilfsfunktionen nicht genügen: Day.js (`dayjs().format('YYYY-MM-DD')`, `dayjs().add(1, 'day')`, ~2 KB), Luxon (vom Moment-Team), date-fns (funktional, tree-shaking-fähig).

## Ersetzungstabelle

| Moment.js | Native JavaScript / Hilfsfunktion |
|---|---|
| `moment()` | `new Date()` |
| `moment(string)` | `new Date(string)` |
| `moment(string, format)` | `parseDate(string, format)` (eigene Funktion je benötigtem Format) |
| `.format('YYYY-MM-DD')` | `.toISOString().split('T')[0]` |
| `.format('DD.MM.YYYY')` | `formatDate(date, 'DD.MM.YYYY')` |
| `.add(1, 'day')` / `.subtract(1, 'day')` | `date.setDate(date.getDate() ± 1)` |
| `.diff(other, 'days')` | `Math.floor((date1 - date2) / 86400000)` |
| `.isBefore(other)` / `.isAfter(other)` | `date1 < date2` / `date1 > date2` |
| `.isSame(other, 'day')` | `date1.toDateString() === date2.toDateString()` |
| `.startOf('day')` / `.endOf('day')` | `date.setHours(0, 0, 0, 0)` / `date.setHours(23, 59, 59, 999)` |
| `.isValid()` | `!isNaN(date.getTime())` |
| `.toISOString()` / `.valueOf()` / `.unix()` | `date.toISOString()` / `date.getTime()` / `Math.floor(date.getTime() / 1000)` |
| `.fromNow()` | `fromNow(date)` |
| `moment.duration(ms).humanize()` | `formatDuration(ms)` |
| `.locale('de').format('L')` | `formatDateLocalized(date, 'de-DE')` |

## Ersatzcode

```javascript
"use strict";

// Formatierung mit Platzhaltern YYYY, MM, DD
function formatDate(date, format) {
    const datum = new Date(date);
    const jahr = String(datum.getFullYear());
    const monat = String(datum.getMonth() + 1).padStart(2, '0');
    const tag = String(datum.getDate()).padStart(2, '0');
    return format.replace('YYYY', jahr).replace('MM', monat).replace('DD', tag);
}

// Relative Zeit („vor 3 Tagen") — Ersatz für .fromNow()
function fromNow(date) {
    const sekundenSeitdem = Math.floor((new Date() - new Date(date)) / 1000);
    const intervalle = [
        { einzahl: 'Jahr', mehrzahl: 'Jahren', sekunden: 31536000 },
        { einzahl: 'Monat', mehrzahl: 'Monaten', sekunden: 2592000 },
        { einzahl: 'Woche', mehrzahl: 'Wochen', sekunden: 604800 },
        { einzahl: 'Tag', mehrzahl: 'Tagen', sekunden: 86400 },
        { einzahl: 'Stunde', mehrzahl: 'Stunden', sekunden: 3600 },
        { einzahl: 'Minute', mehrzahl: 'Minuten', sekunden: 60 }
    ];
    for (const intervall of intervalle) {
        const anzahl = Math.floor(sekundenSeitdem / intervall.sekunden);
        if (anzahl >= 1) {
            let einheit = intervall.mehrzahl;
            if (anzahl === 1) {
                einheit = intervall.einzahl;
            }
            return 'vor ' + anzahl + ' ' + einheit;
        }
    }
    return 'gerade eben';
}

// Dauer in der größten passenden Einheit — Ersatz für moment.duration().humanize()
function formatDuration(milliseconds) {
    const sekunden = Math.floor(milliseconds / 1000);
    const minuten = Math.floor(sekunden / 60);
    const stunden = Math.floor(minuten / 60);
    const tage = Math.floor(stunden / 24);
    if (tage > 0) {
        return mitEinheit(tage, 'Tag', 'Tage');
    }
    if (stunden > 0) {
        return mitEinheit(stunden, 'Stunde', 'Stunden');
    }
    if (minuten > 0) {
        return mitEinheit(minuten, 'Minute', 'Minuten');
    }
    return mitEinheit(sekunden, 'Sekunde', 'Sekunden');
}

function mitEinheit(anzahl, einzahl, mehrzahl) {
    if (anzahl === 1) {
        return anzahl + ' ' + einzahl;
    }
    return anzahl + ' ' + mehrzahl;
}

// Locale-abhängige Formatierung über die Intl-API
// formatDateLocalized(new Date(), 'de-DE') -> "TT.MM.JJJJ", 'en-US' -> "MM/DD/YYYY"
function formatDateLocalized(date, locale, options) {
    const gewaehlteLocale = locale || 'de-DE';
    const gewaehlteOptionen = options || { day: '2-digit', month: '2-digit', year: 'numeric' };
    return new Intl.DateTimeFormat(gewaehlteLocale, gewaehlteOptionen).format(new Date(date));
}
```

## Checkliste

- [ ] Alle Moment-Aufrufe gelistet und nach Funktionstyp gruppiert
- [ ] Hilfsfunktionen implementiert und mit Unit-Tests belegt (Randfälle: Monatswechsel, Jahreswechsel, Sommerzeit, ungültiges Datum)
- [ ] `moment-timezone` ersetzt (Intl mit `timeZone`-Option) oder als Blocker dokumentiert
- [ ] Date-Picker und andere Bibliotheken, die Moment voraussetzen, geprüft
- [ ] Bibliothek, Script-Tags/Imports, `package.json`-Eintrag entfernt

## Referenzen

- [MDN — Date](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date)
- [MDN — Intl.DateTimeFormat](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat)
- [Day.js](https://day.js.org/) · [Luxon](https://moment.github.io/luxon/) · [date-fns](https://date-fns.org/)
