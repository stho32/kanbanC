# Common - Wiederverwendbare Architektur-Snippets

## Überblick

Diese Architektur-Sammlung enthält kleine, wiederverwendbare Architekturmuster (Snippets), die in verschiedenen Projekten immer wieder benötigt werden. Jedes Snippet ist eine in sich geschlossene Lösung für ein häufig auftretendes Problem.

## Verzeichnisstruktur

```
Common/
  ARCHITECTURE.md                      <- Diese Datei
  sast-zuordnung.md                    <- Sprache -> SAST-Werkzeug -> Aufruf (Review-Commands, /wartung)
  snippets/
    SollIstVergleich.md                <- Dokumentation und Beispiele für Soll-Ist-Vergleiche
    sql-idempotente-migrationen.md     <- Idempotenz-Muster SQL Server 2019 / MariaDB (/erstelle-db-migration)
    js-datum-ohne-moment.md            <- Datum/Zeit ohne Moment.js (/upgrade momentjs)
    js-templating-ohne-mustache.md     <- Templating ohne Mustache.js (/upgrade mustache)
    jquery-3-migration.md              <- Plugin-Tabelle, entfernte APIs, Verhaltensänderungen (/upgrade jquery)
    nunit4-testprojekt.csproj.md       <- Kompatibilitätsmatrix und csproj-Referenzblock (/upgrade nunit)
  templates/
    SollIstVergleich/                  <- Code-Vorlagen zum Copy-Paste
    Fallbeschreibung.html              <- HTML-Grundgerüst (/erstelle-fallbeschreibung)
```

## Verfügbare Snippets

| Snippet | Beschreibung |
|---------|--------------|
| [SollIstVergleich](snippets/SollIstVergleich.md) | Vergleich von Soll- und Istzustand mit Aktionsliste |
| [sql-idempotente-migrationen](snippets/sql-idempotente-migrationen.md) | Mehrfach ausführbare Migrationen nach `sql-stil` — SQL Server 2019 und MariaDB |
| [js-datum-ohne-moment](snippets/js-datum-ohne-moment.md) | Ersetzungstabelle und Hilfsfunktionen für den Abbau von Moment.js |
| [js-templating-ohne-mustache](snippets/js-templating-ohne-mustache.md) | `renderTemplate`/`escapeHtml` als Ersatz für Mustache.js |
| [jquery-3-migration](snippets/jquery-3-migration.md) | Plugin-Kompatibilität, entfernte APIs und Verhaltensänderungen jQuery 3.x |
| [nunit4-testprojekt.csproj](snippets/nunit4-testprojekt.csproj.md) | NUnit-4-Paketreferenzen je Zielframework |

## Weitere Referenzen

| Datei | Beschreibung |
|---|---|
| [sast-zuordnung](sast-zuordnung.md) | Welche statische Sicherheitsanalyse für welche Sprache — Maßstab der Review-Commands |
| [templates/Fallbeschreibung.html](templates/Fallbeschreibung.html) | HTML-Grundgerüst der Fallbeschreibung für fachliche Entscheider |

## Verwendung

1. Snippet-Dokumentation in `snippets/` lesen
2. Template aus `templates/` kopieren
3. An eigenen Anwendungsfall anpassen
4. Prefixes und Typnamen anpassen

## Beiträge

Neue Snippets sollten folgende Struktur haben:

1. **Dokumentation** in `snippets/<SnippetName>.md`:
   - Problemstellung
   - Lösungsansatz
   - Beispielcode
   - Anwendungsbeispiele

2. **Template** in `templates/<SnippetName>/`:
   - Kopierfähige Code-Dateien
   - Platzhalter für Anpassungen markiert
