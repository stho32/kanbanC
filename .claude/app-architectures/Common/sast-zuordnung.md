# SAST-Zuordnung je Sprache

> Welche statische Sicherheitsanalyse für welche Sprache — die eine Quelle für `/review-projektarchitekturcompliance` (Phase 4), `/review-architekturvorlage` (Phase 4) und `/wartung`. Änderungen an der Zuordnung gehören hierher, nicht in die Commands.

## Zuordnung

| Sprache | Werkzeug | Aufruf / Konfiguration | Hinweis |
|---|---|---|---|
| C# / .NET | CodeQL | `.github/workflows/codeql.yml`, Query-Suite `security-extended` | — |
| JavaScript / TypeScript | CodeQL | `.github/workflows/codeql.yml`, Query-Suite `security-extended` | — |
| Python | CodeQL | `.github/workflows/codeql.yml`, Query-Suite `security-extended` | — |
| PHP | PHPStan + Larastan | `phpstan.neon` (Level ≥ 6) und `.github/workflows/sast.yml` mit `vendor/bin/phpstan analyse` | **NICHT CodeQL** — unterstützt kein PHP |
| Dart / Flutter | `dart analyze` strict (+ `custom_lint` für Vorlagen) | `analysis_options.yaml` mit `implicit-casts: false`, `implicit-dynamic: false`; Aufruf `dart analyze --fatal-infos` im CI | **NICHT CodeQL** — unterstützt kein Dart |
| PowerShell | PSScriptAnalyzer | `Invoke-ScriptAnalyzer -Path . -Recurse -Severity Error,Warning` im CI | — |
| SQL (Migrationen, Scripts) | kein eigenes SAST | Review nach Skill `sql-stil` (Mandantenskopus, Parametrisierung); Schema-Prüfung `/audit schema` | — |

## Workflow-Anforderungen (alle Sprachen)

- Läuft auf `push`, `pull_request` **und** `schedule` (wöchentlich).
- Ergebnis blockiert den Merge bei Findings der Stufe *error* / *high*.
- Bei CodeQL: richtige Query-Suite; Stufenplan Built-in → Alerts auswerten → Custom Rules.
- Status prüfen: `gh run list --workflow=codeql.yml` bzw. `--workflow=sast.yml`; offene Findings: `gh api repos/{owner}/{repo}/code-scanning/alerts --jq 'length'`.

## Fehlkonfigurationen

Ein CodeQL-Workflow in einem PHP- oder Dart-Projekt ist **FEHLT**, kein Compliance-Erfolg — er läuft leer und erzeugt ein falsches Sicherheitsgefühl. Aktiv darauf prüfen.
