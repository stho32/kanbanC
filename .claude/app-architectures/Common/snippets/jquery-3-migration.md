# jQuery 3 — Migrationsreferenz

> Plugin-Kompatibilitätstabelle, entfernte APIs und Verhaltensänderungen beim Wechsel von jQuery 1.x/2.x auf 3.x. Genutzt von `/upgrade jquery`; Ablauf, Zielversion, Sicherheitslage und Migrate-Strategie stehen dort.

## Abhängige Bibliotheken

| Bibliothek | Typische Dateinamen | Hinweis |
|---|---|---|
| jQuery UI | `jquery-ui*.js`, `jquery.ui*.js` | Version 1.13+ für jQuery 3.x |
| jQuery Validation | `jquery.validate*.js` | Version 1.19+ für jQuery 3.x |
| DataTables | `jquery.dataTables*.js`, `dataTables*.js` | Version 1.10.18+ für jQuery 3.x |
| Select2 | `select2*.js` | Version 4.0+ für jQuery 3.x |
| Chosen | `chosen*.js` | Version 1.8+ für jQuery 3.x |
| Bootstrap (JS) | `bootstrap*.js` | Bootstrap 3.4+ oder 4/5 |
| jQuery Migrate | `jquery-migrate*.js` | Version 3.6.0 für jQuery 3.x |
| jQuery Form | `jquery.form*.js` | AJAX-Formulare |
| jQuery File Upload | `jquery.fileupload*.js` | Datei-Upload |
| jQuery BlockUI | `jquery.blockUI*.js` | UI-Blockierung |
| jQuery Masked Input | `jquery.maskedinput*.js`, `jquery.mask*.js` | Eingabemasken |
| jQuery Autocomplete | `jquery.autocomplete*.js` | Autovervollständigung (nicht jQuery UI) |
| jQuery Cookie | `jquery.cookie*.js` | veraltet, besser js-cookie |
| jQuery Easing | `jquery.easing*.js` | Easing-Funktionen |
| Fancybox | `fancybox*.js`, `jquery.fancybox*.js` | Version 3+ für jQuery 3.x |
| Colorbox | `jquery.colorbox*.js` | Lightbox |
| Magnific Popup | `jquery.magnific-popup*.js` | Lightbox/Modal |
| Slick Carousel | `slick*.js` | Carousel/Slider |
| Owl Carousel | `owl.carousel*.js` | Version 2.3+ für jQuery 3.x |
| bxSlider | `jquery.bxslider*.js` | Slider |
| Tablesorter | `jquery.tablesorter*.js` | Tabellen-Sortierung |
| qTip2 | `jquery.qtip*.js` | Tooltips |
| Toastr | `toastr*.js` | Toast-Benachrichtigungen |
| jGrowl | `jquery.jgrowl*.js` | Benachrichtigungen |
| jQuery Timepicker | `jquery.timepicker*.js`, `jquery-ui-timepicker*.js` | Zeit-Auswahl |
| jQuery Lazy Load | `jquery.lazyload*.js`, `lazy*.js` | Lazy Loading |
| jQuery Context Menu | `jquery.contextMenu*.js` | Kontextmenüs |
| jQuery Uniform | `jquery.uniform*.js` | Form-Styling |
| jQuery Tags Input | `jquery.tagsinput*.js` | Tag-Eingabe |
| Tokeninput | `jquery.tokeninput*.js` | Token-Eingabe |
| jQuery Multiselect | `jquery.multiselect*.js` | Multi-Select |
| FullCalendar | `fullcalendar*.js` | Version 4+ benötigt kein jQuery mehr |
| jQuery Sparkline | `jquery.sparkline*.js` | Mini-Charts |
| jQuery Knob | `jquery.knob*.js` | Drehregler |
| jQuery Steps | `jquery.steps*.js` | Wizard |
| jQuery SmartWizard | `jquery.smartWizard*.js` | Wizard |
| jQuery Countdown | `jquery.countdown*.js` | Countdown |
| jQuery ScrollTo | `jquery.scrollTo*.js` | Scroll-Animationen |
| jQuery Waypoints | `waypoints*.js`, `jquery.waypoints*.js` | Scroll-Events |
| jQuery Sticky | `jquery.sticky*.js` | Sticky-Elemente |
| jQuery Nice Scroll | `jquery.nicescroll*.js` | Custom Scrollbars |
| jQuery Perfect Scrollbar | `perfect-scrollbar*.js` | Custom Scrollbars |

## Entfernte und veraltete APIs (1.x/2.x → 3.x)

| Alte API | Ersatz |
|---|---|
| `.live()`, `.die()` | `$(document).on('click', '.selector', handler)` / `.off()` |
| `.bind()`, `.unbind()`, `.delegate()`, `.undelegate()` | `.on()`, `.off()` |
| `.toggle(fn1, fn2)` (Event-Handler) | eigene Implementierung |
| `jQuery.browser`, `jQuery.sub()`, `.selector`, `.context` | entfernt (Feature Detection) |
| `.andSelf()` | `.addBack()` |
| `.size()` | `.length` |
| `$.isArray()` | `Array.isArray()` |
| `$.parseJSON()` | `JSON.parse()` |
| `$.unique()` | `$.uniqueSort()` |
| `.load(fn)`, `.unload(fn)`, `.error(fn)` | `.on("load", fn)`, `.on("unload", fn)`, `.on("error", fn)` |
| `.success()`, `.error()`, `.complete()` (Ajax) | `.done()`, `.fail()`, `.always()` |
| `$(document).on("ready", fn)` | `$(fn)` oder `$.ready.then(fn)` |
| `jQuery.expr[":"]`, `jQuery.expr.filters` | `jQuery.expr.pseudos` |
| `jQuery.fx.interval` | deprecated (`requestAnimationFrame`) |
| `jQuery.event.props`, `jQuery.event.fixHooks` | entfernt |
| `:first`, `:last`, `:eq()`, `:even`, `:odd` | deprecated — `.first()`, `.last()`, `.eq()`, `.filter()` |

## Verhaltensänderungen in 3.x

| Änderung | Aktion |
|---|---|
| `.attr()` nur für HTML-Attribute, `.prop()` für DOM-Properties (`checked`, `disabled`, `selected`) | `$('#cb').prop('checked', true)` statt `.attr('checked', true)`; `.prop('checked', false)` statt `.removeAttr('checked')` |
| HTML-Strings müssen mit `<` beginnen | Selektor- und HTML-Strings trennen |
| Ajax-Events müssen auf `document` registriert werden | `$(document).ajaxError(...)` |
| `hover` nicht mehr Shorthand für `mouseenter mouseleave` | beide Events explizit |
| `.ready()`-Handler laufen immer asynchron | Code, der synchrones `.ready()` erwartet, anpassen |
| `$.ajax()` liefert Promises/A+-kompatible Promises; `.then()` erhält nur das erste Argument | `.then(function(data){})`; `.done(function(data, textStatus, jqXHR){})` behält alle Argumente |
| `$.Deferred` Promises/A+-konform | Callback-Ketten prüfen |
| `.width()`/`.height()` liefern Dezimalwerte (`getBoundingClientRect`); auf leeren Collections `undefined` statt `null` | Vergleiche anpassen |
| `.outerWidth()`/`.outerHeight()` auf `window` inkl. Scrollbar (`window.innerWidth`) | Layout-Berechnungen prüfen |
| `"use strict"` in jQuery 3.0 | eigene Erweiterungen strict-fähig |
| `.val()` auf `<select multiple>` ohne Auswahl liefert `[]` statt `null` | `if (val.length === 0)` statt `if (val === null)` |
| `:hidden`/`:visible`: sichtbar, sobald eine Layout-Box existiert (auch bei 0×0) | Sichtbarkeitsprüfungen anpassen |
| `$("#")` wirft Syntax Error statt leerer Collection | leere Selektoren abfangen |
| `.data("click-count")` wird intern als `clickCount` gespeichert | Schlüssel konsistent |
| Cross-Domain-Scripts benötigen `dataType: "script"` | `$.ajax`-Optionen ergänzen |
| `.wrapAll(function)` ruft die Funktion nur einmal auf | Rückgabewert prüfen |
| Animationen nutzen `requestAnimationFrame` (pausieren im Hintergrund-Tab) | zeitkritische Animationen prüfen |
| Delegierte Events mit ungültigem Selektor werfen sofort | Selektoren validieren |
| `$.ajax()` entfernt den Hash (#) nicht mehr aus der URL | ggf. manuell entfernen |

## Sicherheitsänderung 3.5 (`htmlPrefilter`, behebt CVE-2020-11022/-11023)

Self-closing Tags (XHTML-Stil) werden nicht mehr automatisch konvertiert:

```javascript
// Alt: $("<div/><span/>") wurde zu <div></div><span></span>
// Ab 3.5: bleibt <div/><span/> — der Browser interpretiert es als <div><span>
$("<div></div><span></span>");   // explizite Closing-Tags
$("<div class='test' />");       // einzelne Tags mit Attributen funktionieren weiterhin
```

## Referenzen

- [jQuery 3.0 Upgrade Guide](https://jquery.com/upgrade-guide/3.0/) · [jQuery 3.5 Upgrade Guide](https://jquery.com/upgrade-guide/3.5/)
- [jQuery Migrate Warnings](https://github.com/jquery/jquery-migrate/blob/main/warnings.md)
