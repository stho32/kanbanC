# Build & Deployment

## Build-Befehle

```bash
# Restore + Build
dotnet build

# Release-Build
dotnet build -c Release

# Publish (self-contained, Linux x64)
dotnet publish Source/MyApp.Web/ -c Release -o publish/ \
  --self-contained -r linux-x64

# Publish (framework-dependent, kleiner)
dotnet publish Source/MyApp.Web/ -c Release -o publish/
```

## Docker

### Dockerfile

```dockerfile
# docker/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Dependencies zuerst kopieren (Layer-Caching)
COPY MyApp.sln .
COPY Source/MyApp.Web/MyApp.Web.csproj Source/MyApp.Web/
COPY Source/MyApp.BL/MyApp.BL.csproj Source/MyApp.BL/
RUN dotnet restore

# Quellcode kopieren und builden
COPY Source/ Source/
RUN dotnet publish Source/MyApp.Web/ -c Release -o /app

# Runtime-Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MyApp.Web.dll"]
```

### docker-compose.yml

```yaml
# docker/docker-compose.yml
services:
  web:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=myapp;Username=postgres;Password=postgres
    depends_on:
      - db

  db:
    image: postgres:16
    environment:
      POSTGRES_DB: myapp
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

## CI/CD mit GitHub Actions

### ci.yml — Build + Test bei jedem Push/PR

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Unit Tests
        run: dotnet test Source/MyApp.BL.Tests/ --no-build -c Release --logger "trx;LogFileName=unit-tests.trx"

      - name: Integration Tests
        run: dotnet test Source/MyApp.BL.IntegrationTests/ --no-build -c Release --logger "trx;LogFileName=integration-tests.trx"

      - name: Install Playwright Browsers
        run: pwsh Source/MyApp.PlaywrightTests/bin/Release/net10.0/playwright.ps1 install --with-deps

      - name: E2E Tests
        run: dotnet test Source/MyApp.PlaywrightTests/ --no-build -c Release --logger "trx;LogFileName=e2e-tests.trx"

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/*.trx'

      - name: Check Vulnerable Packages
        run: dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerable.txt; ! grep -q "has the following vulnerable packages" vulnerable.txt
```

### release.yml — Release-Workflow

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Extract Version from Tag
        id: version
        run: echo "VERSION=${GITHUB_REF_NAME#v}" >> $GITHUB_OUTPUT

      - name: Build and Test
        run: |
          dotnet build -c Release /p:Version=${{ steps.version.outputs.VERSION }}
          dotnet test --no-build -c Release

      - name: Publish
        run: dotnet publish Source/MyApp.Web/ -c Release -o publish/ /p:Version=${{ steps.version.outputs.VERSION }}

      - name: Build Docker Image
        run: docker build -f docker/Dockerfile -t myapp:${{ github.ref_name }} .

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          generate_release_notes: true
```

### codeql.yml — SAST bei jedem Push/PR

```yaml
# .github/workflows/codeql.yml
name: CodeQL

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 6 * * 1'

jobs:
  analyze:
    runs-on: ubuntu-latest
    permissions:
      security-events: write

    steps:
      - uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Autobuild
        uses: github/codeql-action/autobuild@v3

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
```

## Umgebungskonfiguration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "DetailedErrors": true
}
```

### Umgebungsvariablen

| Variable | Beschreibung | Beispiel |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Runtime-Umgebung | `Development`, `Production` |
| `ASPNETCORE_URLS` | Bind-Adressen | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | DB-Verbindung (falls EF Core) | `Host=localhost;...` |

## Release-Prozess

1. **Feature-Branch** erstellen und entwickeln
2. **PR erstellen** — CI laeuft automatisch (Build + alle Tests)
3. **Code Review** und Merge in `main`
4. **Tag setzen** fuer Release: `git tag v1.0.0 && git push --tags`
5. **Release-Workflow** baut, testet, published und erstellt GitHub Release

## Offizielle Dokumentation

- [ASP.NET Core Docker](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/?view=aspnetcore-10.0)
- [GitHub Actions for .NET](https://learn.microsoft.com/en-us/dotnet/devops/github-actions-overview)
- [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
