# dotnet-server-side-blazor Architecture Guide

Server-Side Blazor Webanwendung mit separater Business-Logic-Bibliothek (.BL), umfassender Test-Pyramide (Unit, Integration, Playwright E2E) und konsequenter Anwendung des IOSP (Integration Operation Segregation Principle).

## Contents

1. [Project Structure](./01-project-structure.md) — Solution-Layout und Verzeichnisorganisation
2. [Architecture Patterns](./02-architecture-patterns.md) — Schichten, Datenfluss und Design Patterns
3. [Blazor Server Guide](./03-blazor-server-guide.md) — Komponenten, Lifecycle, State Management, SignalR
4. [IOSP Guide](./04-iosp-guide.md) — Integration Operation Segregation Principle mit C#-Beispielen
5. [Testing Strategy](./05-testing-strategy.md) — Test-Pyramide: NUnit Unit, Integration, Playwright E2E
6. [Build & Deployment](./06-build-deployment.md) — CI/CD, Docker, Release-Workflow
7. [Developer Onboarding](./07-developer-onboarding.md) — Setup, Befehle, haeufige Probleme
8. [Anforderungen Workflow](./08-anforderungen-workflow.md) — Anforderungs-Workflow und Commands
9. [Project Scaffolding](./09-project-scaffolding.md) — Neues Projekt Schritt fuer Schritt aufsetzen

Templates: [templates/playwright-smoke/](./templates/playwright-smoke/README.md) — Smoke-Tests der Seitenerreichbarkeit (Kapitel 5), instanziiert per `/erstelle-blazor-playwright-tests`

## Quick Reference

### Official Documentation
- [ASP.NET Core 10.0 Release Notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)
- [Blazor Server Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-10.0)
- [Blazor Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0)
- [Blazor State Management](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/?view=aspnetcore-10.0)
- [Blazor Security](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0)
- [Blazor Performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-10.0)
- [NUnit Documentation](https://docs.nunit.org/)
- [Playwright .NET](https://playwright.dev/dotnet/docs/intro)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

### Key Resources
- [IOSP by Ralf Westphal](https://ralfwestphal.substack.com/p/integration-operation-segregation)
- [Flow Design / CCD Akademie](https://ccd-akademie.de/en/flow-design/)

## Technology Stack

| Technology | Version | Purpose |
|---|---|---|
| .NET | 10.0 | Runtime und SDK |
| ASP.NET Core | 10.0 | Web-Framework |
| Blazor Server | 10.0 | UI-Framework (Server-Side Rendering via SignalR) |
| C# | 13 | Programmiersprache |
| NUnit | 4.x | Test-Framework (Unit + Integration) |
| Microsoft.Playwright.NUnit | latest | E2E-Browser-Tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0 | WebApplicationFactory fuer Integration Tests |
| Entity Framework Core | 10.0 | ORM (optional) |

## When to Use

- Webanwendungen mit reichhaltiger Interaktivitaet (Dashboards, Formulare, Echtzeit-Updates)
- Projekte bei denen Business-Logik testbar und wiederverwendbar sein muss
- Teams die Wert auf klare Trennung von Logik und UI legen (IOSP)
- Anwendungen bei denen der Server die Kontrolle behaelt (kein WASM-Download noetig)

## When NOT to Use

- Offline-faehige Apps (→ Blazor WebAssembly oder PWA)
- Hochlast-Szenarien mit tausenden gleichzeitigen Nutzern (jeder Nutzer haelt eine SignalR-Verbindung)
- Reine REST-APIs ohne UI (→ dotnet-cli-tool oder Minimal API)
- Mobile Apps (→ .NET MAUI oder Flutter)

Last updated: 2026-04-09
