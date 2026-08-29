# Eingebettete Ressourcen in .NET 10 — Namensbildung für `.sql`-Migrationen

Belegt durch Probe-Test am 2026-08-29 (.NET SDK 10.0.100) und Microsoft Learn. Genutzt von R00001 (`EingebetteteMigrationsQuelle`, `Migrationslaeufer`).

## Einbinden

```xml
<ItemGroup>
  <EmbeddedResource Include="Persistenz\Migrationen\**\*.sql" />
</ItemGroup>
```

`.sql`-Dateien liegen nicht in den Default-Globs des SDK — der explizite Eintrag ist nötig und erzeugt keinen Doppel-Include-Fehler.

## Ergebnisname

Für Nicht-`.resx`-Dateien gilt: **`<RootNamespace>.<Ordnerpfad mit Punkten>.<Dateiname>`** — der Dateiname bleibt wörtlich erhalten, inklusive Ziffern, Bindestrichen und Erweiterung.

Probe mit Projekt `Probe`, Datei `Migrationen/001-boards-und-spalten.sql`:

```
Probe.Migrationen.001-boards-und-spalten.sql
Probe.Migrationen.002-zweite.sql
```

Für KanbanC also: `KanbanC.BL.Persistenz.Migrationen.001-boards-und-spalten.sql`.

## Regeln für die Dateinamen

- **Genau ein Punkt** im Dateinamen (der vor der Erweiterung). Microsoft Learn: eine `EmbeddedResource` mit zwei Punkten im Namen taucht ohne `LogicalName` **nicht** in `GetManifestResourceNames()` auf. `001-boards-und-spalten.sql` ist in Ordnung; `001.boards.sql` wäre unsichtbar.
- Bindestriche und führende Ziffern sind unproblematisch — der Name ist kein C#-Bezeichner.
- Die Reihenfolge der Migrationen ergibt sich aus der **lexikalischen Sortierung** der Ressourcennamen; dreistellige Nummern mit führenden Nullen halten sie bis 999 stabil.

## Lesen

```csharp
var assembly = typeof(Migrationslaeufer).Assembly;
var namen = assembly.GetManifestResourceNames()
    .Where(n => n.EndsWith(".sql", StringComparison.Ordinal))
    .OrderBy(n => n, StringComparer.Ordinal);
foreach (var name in namen)
{
    using var strom = assembly.GetManifestResourceStream(name)!;
    using var leser = new StreamReader(strom);
    var sql = leser.ReadToEnd();
    verbindung.Execute(sql);
}
```

`StringComparer.Ordinal` statt der Kultur-Sortierung, damit `InvariantGlobalization` und Betriebssystem keinen Einfluss haben.

## Explizite Namen (nicht verwendet)

`LogicalName` überschreibt die Konvention vollständig (`<EmbeddedResource Include="X.sql" LogicalName="Migration.001" />`). Für KanbanC nicht nötig — die Konvention ist eindeutig und die Datei wird ohnehin per Suffix und Sortierung gefunden, nicht per festem Namen.

## Quellen

- [Microsoft Learn — How MSBuild generates manifest file names](https://learn.microsoft.com/en-us/dotnet/core/resources/manifest-file-names) (Regel `RootNamespace.RelativePathWithDotsForSlashes`, Zwei-Punkte-Einschränkung, `LogicalName`)
- Probe: Scratchpad der Sitzung vom 2026-08-29, Ausgabe von `GetManifestResourceNames()`
