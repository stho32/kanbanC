# Anforderungen Workflow

## Anforderungsstruktur

Anforderungen liegen im Root-Verzeichnis unter `Anforderungen/`:

```
MyApp/
├── Anforderungen/
│   ├── README.md
│   ├── R00001-benutzer-authentifizierung.md
│   ├── R00002-wetter-dashboard.md
│   └── R00003-benutzerprofil-verwaltung.md
└── Source/
    └── ...
```

## Nummernformat

- Format: `RXXXXX` (R + 5-stellige Zahl mit fuehrenden Nullen)
- Dateiname: `RXXXXX-kurzbeschreibung.md`
- Nummern sind fortlaufend und eindeutig
- Bereich: R00001 – R99999

## Frontmatter

```yaml
---
id: R00001
titel: Benutzer-Authentifizierung
typ: Feature
status: In Bearbeitung
erstellt: 2026-04-09
---
```

## Status-Werte

| Status | Bedeutung |
|---|---|
| `Entwurf` | Anforderung wird formuliert, noch nicht freigegeben |
| `Bereit` | Anforderung ist vollstaendig und kann implementiert werden |
| `In Bearbeitung` | Anforderung wird aktuell implementiert |
| `Review` | Implementation ist fertig, wird geprueft |
| `Abgeschlossen` | Implementation geprueft und abgenommen |
| `Abgelehnt` | Anforderung wird nicht umgesetzt |

## Verfuegbare Commands

| Command | Zweck |
|---|---|
| `/anforderung neu` | Neue Anforderung erstellen |
| `/anforderung abschliessen RXXXXX` | Offene Fragen in Anforderung beantworten |
| `/implementierung autonom RXXXXX` | Anforderung implementieren |
| `/implementierung pruefen RXXXXX` | Implementation auf Vollstaendigkeit pruefen |
| `/implementierung abschluss` | Quality-Gate: Build, Tests, Coverage, Warnungen |
| `/wartung` | Gesamtprojekt-Check inkl. Architektur-Compliance |

## Typischer Workflow

1. **Anforderung erstellen:**
   ```
   /anforderung neu
   ```
   Beschreibe das Feature oder den Bug. Claude erstellt eine strukturierte Anforderung mit Akzeptanzkriterien.

2. **Offene Fragen klaeren:**
   ```
   /anforderung abschliessen R00001
   ```
   Beantworte Rueckfragen die waehrend der Erstellung entstanden sind.

3. **Implementierung starten:**
   ```
   /implementierung autonom R00001
   ```
   Claude implementiert die Anforderung direkt auf `main` (Trunk-Based) mit Tests.

4. **Implementation pruefen:**
   ```
   /implementierung pruefen R00001
   ```
   Systematische Pruefung aller Akzeptanzkriterien.

5. **Quality-Gate:**
   ```
   /implementierung abschluss
   ```
   Build, alle Tests, Coverage, Warnungen, Vollstaendigkeit.

6. **Merge und Abschluss:**
   Anforderung auf `Abgeschlossen` setzen.

## Anforderungs-Template

```markdown
---
id: RXXXXX
titel: [Kurzer, praegnanter Titel]
typ: [Feature | Bugfix | Refactoring | Dokumentation]
status: Entwurf
erstellt: YYYY-MM-DD
---

# RXXXXX: [Titel]

## Beschreibung

[Was soll erreicht werden? Warum ist das wichtig?]

## Akzeptanzkriterien

- [ ] [Konkretes, pruefbares Kriterium 1]
- [ ] [Konkretes, pruefbares Kriterium 2]
- [ ] [Konkretes, pruefbares Kriterium 3]
- [ ] Unit Tests fuer alle Operations vorhanden
- [ ] Integration Tests fuer betroffene Endpunkte vorhanden
- [ ] IOSP eingehalten (keine Hybrid-Methoden)

## Technische Details

[Welche Dateien/Komponenten sind betroffen?]
[Welche neuen Operations/Integrations werden benoetigt?]

## Offene Fragen

- [Falls noch Klaerungsbedarf besteht]
```

## IOSP in Anforderungen

Jede Anforderung sollte bei den technischen Details explizit benennen:
- Welche **Operations** (Logik) werden neu erstellt oder geaendert?
- Welche **Integrations** (Orchestrierung) werden benoetigt?
- Welche **Interfaces** muessen definiert werden?

Dies erleichtert das Code Review und stellt sicher, dass IOSP eingehalten wird.
