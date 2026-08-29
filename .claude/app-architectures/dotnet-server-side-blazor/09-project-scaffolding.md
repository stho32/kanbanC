# Project Scaffolding

Schritt-fuer-Schritt-Anleitung zum Aufsetzen eines neuen Projekts mit dieser Architektur.

## Voraussetzungen

- .NET SDK 10.0+ installiert (`dotnet --version`)
- PowerShell 7+ installiert (fuer Playwright)
- Git installiert

## Schritt 1: Solution und Verzeichnisse erstellen

```bash
# Projektname als Variable (anpassen!)
PROJECT_NAME="MyApp"

# Root-Verzeichnis erstellen
mkdir "$PROJECT_NAME" && cd "$PROJECT_NAME"

# Git initialisieren
git init

# Verzeichnisstruktur erstellen
mkdir -p Source Anforderungen Dokumentation .github/workflows docker

# Solution erstellen
dotnet new sln -n "$PROJECT_NAME"
```

## Schritt 2: Projekte erstellen

```bash
# Blazor Server Web-App
dotnet new blazor -n "$PROJECT_NAME.Web" -o "Source/$PROJECT_NAME.Web" --interactivity Server --no-https false

# Business Logic Bibliothek
dotnet new classlib -n "$PROJECT_NAME.BL" -o "Source/$PROJECT_NAME.BL"

# Unit Test Projekt
dotnet new nunit -n "$PROJECT_NAME.BL.Tests" -o "Source/$PROJECT_NAME.BL.Tests"

# Integration Test Projekt
dotnet new nunit -n "$PROJECT_NAME.BL.IntegrationTests" -o "Source/$PROJECT_NAME.BL.IntegrationTests"

# Playwright E2E Test Projekt
dotnet new nunit -n "$PROJECT_NAME.PlaywrightTests" -o "Source/$PROJECT_NAME.PlaywrightTests"
```

## Schritt 3: Projekte zur Solution hinzufuegen

```bash
dotnet sln add "Source/$PROJECT_NAME.Web/$PROJECT_NAME.Web.csproj"
dotnet sln add "Source/$PROJECT_NAME.BL/$PROJECT_NAME.BL.csproj"
dotnet sln add "Source/$PROJECT_NAME.BL.Tests/$PROJECT_NAME.BL.Tests.csproj"
dotnet sln add "Source/$PROJECT_NAME.BL.IntegrationTests/$PROJECT_NAME.BL.IntegrationTests.csproj"
dotnet sln add "Source/$PROJECT_NAME.PlaywrightTests/$PROJECT_NAME.PlaywrightTests.csproj"
```

## Schritt 4: Projekt-Referenzen setzen

```bash
# Web referenziert BL
dotnet add "Source/$PROJECT_NAME.Web/" reference "Source/$PROJECT_NAME.BL/"

# Unit Tests referenzieren BL
dotnet add "Source/$PROJECT_NAME.BL.Tests/" reference "Source/$PROJECT_NAME.BL/"

# Integration Tests referenzieren Web (und damit transitiv BL)
dotnet add "Source/$PROJECT_NAME.BL.IntegrationTests/" reference "Source/$PROJECT_NAME.Web/"

# Playwright Tests referenzieren Web
dotnet add "Source/$PROJECT_NAME.PlaywrightTests/" reference "Source/$PROJECT_NAME.Web/"
```

## Schritt 5: NuGet-Pakete installieren

```bash
# Unit Tests: NSubstitute fuer Mocking, Coverlet fuer Coverage
dotnet add "Source/$PROJECT_NAME.BL.Tests/" package NSubstitute
dotnet add "Source/$PROJECT_NAME.BL.Tests/" package coverlet.collector

# Integration Tests: WebApplicationFactory, Coverlet
dotnet add "Source/$PROJECT_NAME.BL.IntegrationTests/" package Microsoft.AspNetCore.Mvc.Testing
dotnet add "Source/$PROJECT_NAME.BL.IntegrationTests/" package coverlet.collector

# Playwright Tests
dotnet add "Source/$PROJECT_NAME.PlaywrightTests/" package Microsoft.Playwright.NUnit

# Integration Tests: SDK auf Web aendern
# WICHTIG: In Source/$PROJECT_NAME.BL.IntegrationTests/$PROJECT_NAME.BL.IntegrationTests.csproj
# das SDK von "Microsoft.NET.Sdk" auf "Microsoft.NET.Sdk.Web" aendern!
```

## Schritt 6: IntegrationTests SDK anpassen

Die Datei `Source/$PROJECT_NAME.BL.IntegrationTests/$PROJECT_NAME.BL.IntegrationTests.csproj` muss das Web-SDK verwenden:

```xml
<!-- AENDERN von: -->
<Project Sdk="Microsoft.NET.Sdk">

<!-- ZU: -->
<Project Sdk="Microsoft.NET.Sdk.Web">
```

## Schritt 7: BL-Verzeichnisstruktur erstellen

```bash
# IOSP-Verzeichnisse in BL
mkdir -p "Source/$PROJECT_NAME.BL/Models"
mkdir -p "Source/$PROJECT_NAME.BL/Operations"
mkdir -p "Source/$PROJECT_NAME.BL/Integrations"
mkdir -p "Source/$PROJECT_NAME.BL/Interfaces"
mkdir -p "Source/$PROJECT_NAME.BL/Extensions"

# Placeholder-Klasse loeschen
rm -f "Source/$PROJECT_NAME.BL/Class1.cs"
```

## Schritt 8: Test-Verzeichnisstruktur erstellen

```bash
# Unit Tests spiegeln BL-Struktur
mkdir -p "Source/$PROJECT_NAME.BL.Tests/Operations"
mkdir -p "Source/$PROJECT_NAME.BL.Tests/Integrations"
mkdir -p "Source/$PROJECT_NAME.BL.Tests/TestHelpers"

# Integration Tests
mkdir -p "Source/$PROJECT_NAME.BL.IntegrationTests/Infrastructure"
mkdir -p "Source/$PROJECT_NAME.BL.IntegrationTests/Pages"
mkdir -p "Source/$PROJECT_NAME.BL.IntegrationTests/Api"

# Playwright Tests
mkdir -p "Source/$PROJECT_NAME.PlaywrightTests/Infrastructure"
mkdir -p "Source/$PROJECT_NAME.PlaywrightTests/PageObjects"
mkdir -p "Source/$PROJECT_NAME.PlaywrightTests/Tests"
```

## Schritt 9: Playwright-Browser installieren

```bash
dotnet build "Source/$PROJECT_NAME.PlaywrightTests/"
pwsh "Source/$PROJECT_NAME.PlaywrightTests/bin/Debug/net10.0/playwright.ps1" install
```

## Schritt 10: .editorconfig erstellen

```bash
cat > .editorconfig << 'EOF'
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{cs,razor}]
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion

[*.{json,yml,yaml,xml,csproj}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
EOF
```

## Schritt 11: Directory.Build.props erstellen

Zentrale Build-Eigenschaften fuer alle Projekte:

```bash
cat > "Source/Directory.Build.props" << 'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="10.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
EOF
```

## Schritt 12: .gitignore erstellen

```bash
cat > .gitignore << 'EOF'
## .NET
bin/
obj/
publish/
*.user
*.suo

## IDE
.vs/
.idea/
.vscode/

## Test Results
TestResults/
coveragereport/

## OS
.DS_Store
Thumbs.db

## Environment
*.env
appsettings.*.local.json
EOF
```

## Schritt 13: Anforderungen-README erstellen

```bash
cat > Anforderungen/README.md << 'EOF'
# Anforderungen

Dieses Verzeichnis enthaelt alle Anforderungsdokumente im Format `RXXXXX-beschreibung.md`.

## Commands

- `/anforderung neu` — Neue Anforderung erstellen
- `/implementierung autonom RXXXXX` — Anforderung implementieren
- `/implementierung pruefen RXXXXX` — Implementation pruefen
EOF
```

## Schritt 14: Dokumentation-README erstellen

```bash
cat > Dokumentation/README.md << 'EOF'
# Dokumentation

Projektdokumentation, Architektur-Entscheidungen und Guides.
EOF
```

## Schritt 15: Build und Test verifizieren

```bash
# Alles bauen
dotnet build

# Alle Tests ausfuehren (sollte gruene Tests zeigen)
dotnet test
```

## Schritt 16: Erster Commit

```bash
git add -A
git commit -m "Initial project setup: Blazor Server + BL + Tests (IOSP architecture)"
```

## Ergebnis

Nach diesen Schritten hast du ein lauffaehiges Projekt mit:

- Blazor Server Web-App in `Source/MyApp.Web/`
- Business Logic Bibliothek in `Source/MyApp.BL/` mit IOSP-Verzeichnissen
- Unit Tests in `Source/MyApp.BL.Tests/`
- Integration Tests in `Source/MyApp.BL.IntegrationTests/` mit WebApplicationFactory
- Playwright E2E Tests in `Source/MyApp.PlaywrightTests/`
- Anforderungsverzeichnis bereit fuer `/anforderung neu`
- Dokumentationsverzeichnis
- CI-faehige Projektstruktur
