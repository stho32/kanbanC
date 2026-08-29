# Templating ohne Mustache.js

> Ersatzcode und Ersatztabelle für den Abbau von Mustache.js zugunsten von Template Literals und einer Mini-Template-Funktion. Genutzt von `/upgrade mustache` (Weg B: Entfernen). Code nach Skill `javascript-stil`.

## Problemstellung

Mustache.js wird oft nur für einfache `{{key}}`-Ersetzungen, Listen und Bedingungen eingesetzt. Dafür ist eine Bibliothek nicht nötig — solange **jede** Ausgabe HTML-escaped wird. Ohne Escaping entsteht genau die XSS-Lücke neu, die CVE-2015-8862 in alten Mustache-Versionen hatte.

## Lösungsansatz

Alle Verwendungen listen, einfachste Ersetzungen zuerst, nach jeder Änderung testen; zum Schluss Bibliothek, Script-Tags/Imports und `package.json`-Eintrag entfernen. Partials, Custom Delimiters und tief verschachtelte Sections sind Aufwandstreiber — bei vielen davon ist ein Upgrade auf Mustache 4.x meist der bessere Weg.

## Ersatztabelle

| Mustache | Ersatz |
|---|---|
| `Mustache.render('<div>{{name}}</div>', { name })` | Template Literal `` `<div>${escapeHtml(data.name)}</div>` `` |
| einfache `{{key}}`-Ersetzung | `renderTemplate(template, data)` |
| Listen `{{#items}}<li>{{name}}</li>{{/items}}` | `items.map(item => '<li>' + escapeHtml(item.name) + '</li>').join('')` |
| Bedingung `{{#showName}}<span>{{name}}</span>{{/showName}}` | `if (data.showName) { … }` mit `escapeHtml(data.name)` |
| Partials `{{> name}}` | eigene Funktion je Partial, die einen String liefert |

## Ersatzcode

```javascript
"use strict";

// Mini-Template-Funktion für {{key}}-Ersetzungen — jede Ausgabe wird escaped
function renderTemplate(template, data) {
    return template.replace(/\{\{(\w+)\}\}/g, function (treffer, schluessel) {
        const wert = data[schluessel];
        if (wert === undefined) {
            return '';
        }
        return escapeHtml(String(wert));
    });
}

// HTML-Escaping — Pflicht bei jeder Ersetzung, sonst entsteht die XSS-Lücke neu
function escapeHtml(text) {
    const traegerDomElement = document.createElement('div');
    traegerDomElement.textContent = text;
    return traegerDomElement.innerHTML;
}
```

Anwendungsbeispiel mit Liste und Bedingung:

```javascript
function renderArtikelliste(artikel, zeigePreise) {
    const zeilen = artikel.map(function (eintrag) {
        let preisSpalte = '';
        if (zeigePreise) {
            preisSpalte = '<td>' + escapeHtml(eintrag.preis) + '</td>';
        }
        return '<tr><td>' + escapeHtml(eintrag.name) + '</td>' + preisSpalte + '</tr>';
    });
    return '<table>' + zeilen.join('') + '</table>';
}
```

## Checkliste

- [ ] Alle `Mustache.render`-/`to_html`-Stellen und Template-Dateien gelistet
- [ ] Jede Ausgabe läuft durch `escapeHtml` — auch in Template Literals
- [ ] Attribute in erzeugtem HTML immer quotiert (`class="…"`)
- [ ] Partials und Sections als Funktionen nachgebaut und getestet
- [ ] Bibliothek, Script-Tags/Imports, `package.json`-Eintrag entfernt

## Referenzen

- [MDN — Template Literals](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Template_literals)
- [CVE-2015-8862](https://nvd.nist.gov/vuln/detail/CVE-2015-8862) — XSS bei unquotierten Template-Attributen
