# NUnit-4-Testprojekt — Paketreferenzen

> Referenzblock für `.csproj`-Dateien von Testprojekten nach der Migration auf NUnit 4. Genutzt von `/upgrade nunit`; Ablauf, Analyse und Migrationsstrategie stehen dort. Versionen Stand Dezember 2025 — bei Ausführung auf NuGet.org prüfen.

## Kompatibilitätsmatrix

Drei Pakete bilden eine Einheit: NUnit + Adapter + Test.Sdk. Fehlt eines oder passen die Versionen nicht, gibt es keine Test-Discovery. Der Adapter ist TFM-abhängig.

| .NET Version | NUnit | NUnit3TestAdapter | Microsoft.NET.Test.Sdk | Anmerkung |
|---|---|---|---|---|
| net462 / net48 | 4.4.0 | 5.2.0 | 18.0.1 | Adapter 6.0 nicht kompatibel |
| net6.0 / net7.0 | 4.4.0 | 5.2.0 | 18.0.1 | Adapter 6.0 nicht kompatibel |
| net8.0 / net9.0 | 4.4.0 | 6.0.0 | 18.0.1 | neuester Adapter |
| net10.0 | 4.4.0 | 6.0.0 | 18.0.1 | Freigabe des Adapters für net10.0 bei Ausführung prüfen |

## Referenzblock (.NET 8.0+)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
  <PackageReference Include="NUnit" Version="4.4.0" />
  <PackageReference Include="NUnit3TestAdapter" Version="6.0.0" />
  <PackageReference Include="NUnit.Analyzers" Version="4.7.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Ältere TFMs: `NUnit3TestAdapter` auf `5.2.0`. Bei Multi-Targeting mit unterschiedlichen Adapter-Anforderungen: bedingte `PackageReference` (`Condition="'$(TargetFramework)' == 'net48'"`) oder einheitliches TFM.

Mit Central Package Management (`Directory.Packages.props`) stehen die `Version`-Attribute dort als `PackageVersion`; im `.csproj` bleiben nur die `Include`-Einträge.

## Hinweise

- `NUnit.Analyzers` **vor** dem NUnit-Upgrade heben — der Analyzer arbeitet nur auf kompilierendem Code und zeigt dann die Migrations-Warnungen.
- `ClassicAssert` (`using NUnit.Framework.Legacy;`) ist eine gültige Migrationsstrategie; die Constraint-Syntax kann schrittweise folgen.
