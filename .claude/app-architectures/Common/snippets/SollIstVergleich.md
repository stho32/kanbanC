# Soll-Ist-Vergleich Pattern

> Die Codebeispiele nutzen DataRow-Extension-Methods (`.AsString()`, `ToInstancesOf`) und `Wenn_…_dann_…`-Testnamen; Mapping und Testkonventionen folgen der App-Architektur-Vorlage des Projekts.

## Problemstellung

Bei der Synchronisation von Daten (z.B. Datenbanktabellen-Abschriften, Cache-Aktualisierungen, Import-Prozessen) muss ermittelt werden, welche Datensätze:
- **Unverändert** sind (keine Aktion erforderlich)
- **Zu aktualisieren** sind (Änderungen übernehmen)
- **Zu erstellen** sind (neue Datensätze anlegen)
- **Zu löschen** sind (veraltete Datensätze entfernen)

## Lösungsansatz

Der Soll-Ist-Vergleicher nimmt zwei Kollektionen entgegen:
1. **Sollzustand**: Wie die Daten aussehen sollen
2. **Istzustand**: Wie die Daten aktuell aussehen

**Wichtig**: Beide Datenquellen verwenden das **gleiche DTO**. Die Repositories sind dafür verantwortlich, ihre Daten in dieses gemeinsame DTO zu konvertieren.

Anhand eines definierten Schlüssels werden die Datensätze verglichen und kategorisiert.

## Architektur nach IOSP

```
SollIstVergleicher<T>      <- Operation (reine Logik, keine Abhängigkeiten)
SollIstVergleichErgebnis   <- Datenstruktur für das Ergebnis
SynchronisationsService    <- Integration (orchestriert Repository + Vergleicher)
```

## Komponenten

### 1. SollIstVergleichErgebnis

```csharp
public class SollIstVergleichErgebnis<T>
{
    public IReadOnlyList<T> ZuErstellen { get; }
    public IReadOnlyList<(T Soll, T Ist)> ZuAktualisieren { get; }
    public IReadOnlyList<T> ZuLoeschen { get; }
    public IReadOnlyList<(T Soll, T Ist)> Unveraendert { get; }

    public SollIstVergleichErgebnis(
        IReadOnlyList<T> zuErstellen,
        IReadOnlyList<(T Soll, T Ist)> zuAktualisieren,
        IReadOnlyList<T> zuLoeschen,
        IReadOnlyList<(T Soll, T Ist)> unveraendert)
    {
        ZuErstellen = zuErstellen;
        ZuAktualisieren = zuAktualisieren;
        ZuLoeschen = zuLoeschen;
        Unveraendert = unveraendert;
    }

    public bool HatAenderungen => ZuErstellen.Count > 0
                                || ZuAktualisieren.Count > 0
                                || ZuLoeschen.Count > 0;
}
```

### 2. SollIstVergleicher (Operation)

```csharp
public class SollIstVergleicher<T, TSchluessel>
{
    private readonly Func<T, TSchluessel> _schluesselSelektor;
    private readonly Func<T, T, bool> _sindGleich;

    public SollIstVergleicher(
        Func<T, TSchluessel> schluesselSelektor,
        Func<T, T, bool> sindGleich)
    {
        _schluesselSelektor = schluesselSelektor;
        _sindGleich = sindGleich;
    }

    public SollIstVergleichErgebnis<T> Vergleiche(
        IEnumerable<T> sollzustand,
        IEnumerable<T> istzustand)
    {
        var sollListe = sollzustand.ToList();
        var istListe = istzustand.ToList();

        var istNachSchluessel = istListe.ToDictionary(_schluesselSelektor);

        var zuErstellen = new List<T>();
        var zuAktualisieren = new List<(T Soll, T Ist)>();
        var unveraendert = new List<(T Soll, T Ist)>();

        foreach (var soll in sollListe)
        {
            var schluessel = _schluesselSelektor(soll);

            if (!istNachSchluessel.TryGetValue(schluessel, out var ist))
            {
                zuErstellen.Add(soll);
            }
            else if (_sindGleich(soll, ist))
            {
                unveraendert.Add((soll, ist));
            }
            else
            {
                zuAktualisieren.Add((soll, ist));
            }
        }

        var sollSchluessel = new HashSet<TSchluessel>(
            sollListe.Select(_schluesselSelektor));

        var zuLoeschen = istListe
            .Where(ist => !sollSchluessel.Contains(_schluesselSelektor(ist)))
            .ToList();

        return new SollIstVergleichErgebnis<T>(
            zuErstellen,
            zuAktualisieren,
            zuLoeschen,
            unveraendert);
    }
}
```

## Anwendungsbeispiel: Datenbanktabellen-Abschrift

### Szenario

Eine lokale Tabelle `KundenAbschrift` soll mit den aktuellen Kundendaten aus dem Quellsystem synchronisiert werden.

### Gemeinsames DTO

```csharp
// Ein DTO für beide Datenquellen
public class KundeDto
{
    public string KundenNr { get; }
    public string Name { get; }
    public string Email { get; }
    public DateTime LetzteAenderung { get; }

    public KundeDto(string kundenNr, string name, string email, DateTime letzteAenderung)
    {
        KundenNr = kundenNr;
        Name = name;
        Email = email;
        LetzteAenderung = letzteAenderung;
    }
}
```

### Repositories (beide liefern KundeDto)

```csharp
// Quellsystem-Repository konvertiert in KundeDto
public class QuellsystemKundenRepository
{
    public List<KundeDto> LadeAlleKunden()
    {
        var dataTable = _databaseAccessor.GetDataTable("SELECT * FROM Kunden");
        return dataTable.ToInstancesOf(CreateKundeDto);
    }

    private KundeDto CreateKundeDto<T>(DataRow row)
    {
        return new KundeDto(
            row["KundenNr"].AsString(),
            row["Name"].AsString(),
            row["Email"].AsString(),
            row["LetzteAenderung"].ToDateTime());
    }
}

// Abschrift-Repository konvertiert ebenfalls in KundeDto
public class KundenAbschriftRepository
{
    public List<KundeDto> LadeAlleAbschriften()
    {
        var dataTable = _databaseAccessor.GetDataTable("SELECT * FROM KundenAbschrift");
        return dataTable.ToInstancesOf(CreateKundeDto);
    }

    private KundeDto CreateKundeDto<T>(DataRow row)
    {
        return new KundeDto(
            row["KundenNr"].AsString(),
            row["Name"].AsString(),
            row["Email"].AsString(),
            row["LetzteAenderung"].ToDateTime());
    }

    public void Erstelle(KundeDto kunde) { /* ... */ }
    public void Aktualisiere(KundeDto kunde) { /* ... */ }
    public void Loesche(string kundenNr) { /* ... */ }
}
```

### Vergleicher erstellen

```csharp
var kundenVergleicher = new SollIstVergleicher<KundeDto, string>(
    schluesselSelektor: k => k.KundenNr,
    sindGleich: (soll, ist) =>
        soll.Name == ist.Name
        && soll.Email == ist.Email
        && soll.LetzteAenderung == ist.LetzteAenderung
);
```

### Synchronisations-Service (Integration)

```csharp
public class KundenAbschriftSynchronisationsService
{
    private readonly QuellsystemKundenRepository _quellsystem;
    private readonly KundenAbschriftRepository _abschrift;
    private readonly SollIstVergleicher<KundeDto, string> _vergleicher;

    public KundenAbschriftSynchronisationsService()
    {
        _quellsystem = new QuellsystemKundenRepository();
        _abschrift = new KundenAbschriftRepository();
        _vergleicher = new SollIstVergleicher<KundeDto, string>(
            schluesselSelektor: k => k.KundenNr,
            sindGleich: (soll, ist) =>
                soll.Name == ist.Name
                && soll.Email == ist.Email
                && soll.LetzteAenderung == ist.LetzteAenderung);
    }

    public SynchronisationsResultat Synchronisiere()
    {
        // 1. Sollzustand bestimmen
        var sollzustand = _quellsystem.LadeAlleKunden();

        // 2. Istzustand bestimmen
        var istzustand = _abschrift.LadeAlleAbschriften();

        // 3. Soll-Ist-Vergleich ausführen
        var ergebnis = _vergleicher.Vergleiche(sollzustand, istzustand);

        // 4. Istzustand aktualisieren
        foreach (var zuErstellen in ergebnis.ZuErstellen)
        {
            _abschrift.Erstelle(zuErstellen);
        }

        foreach (var (soll, ist) in ergebnis.ZuAktualisieren)
        {
            _abschrift.Aktualisiere(soll);
        }

        foreach (var zuLoeschen in ergebnis.ZuLoeschen)
        {
            _abschrift.Loesche(zuLoeschen.KundenNr);
        }

        return new SynchronisationsResultat(
            ergebnis.ZuErstellen.Count,
            ergebnis.ZuAktualisieren.Count,
            ergebnis.ZuLoeschen.Count);
    }
}
```

## Unit Tests

```csharp
[TestFixture]
public class SollIstVergleicherTests
{
    [Test]
    public void Wenn_Soll_leer_und_Ist_hat_Eintraege_dann_alle_zu_loeschen()
    {
        var vergleicher = ErstelleStringVergleicher();
        var soll = new List<TestDto>();
        var ist = new List<TestDto> { new TestDto("A", "Wert1") };

        var ergebnis = vergleicher.Vergleiche(soll, ist);

        Assert.AreEqual(0, ergebnis.ZuErstellen.Count);
        Assert.AreEqual(0, ergebnis.ZuAktualisieren.Count);
        Assert.AreEqual(1, ergebnis.ZuLoeschen.Count);
        Assert.AreEqual("A", ergebnis.ZuLoeschen[0].Schluessel);
    }

    [Test]
    public void Wenn_Ist_leer_und_Soll_hat_Eintraege_dann_alle_zu_erstellen()
    {
        var vergleicher = ErstelleStringVergleicher();
        var soll = new List<TestDto> { new TestDto("A", "Wert1") };
        var ist = new List<TestDto>();

        var ergebnis = vergleicher.Vergleiche(soll, ist);

        Assert.AreEqual(1, ergebnis.ZuErstellen.Count);
        Assert.AreEqual(0, ergebnis.ZuAktualisieren.Count);
        Assert.AreEqual(0, ergebnis.ZuLoeschen.Count);
    }

    [Test]
    public void Wenn_gleicher_Schluessel_aber_unterschiedlicher_Wert_dann_zu_aktualisieren()
    {
        var vergleicher = ErstelleStringVergleicher();
        var soll = new List<TestDto> { new TestDto("A", "NeuerWert") };
        var ist = new List<TestDto> { new TestDto("A", "AlterWert") };

        var ergebnis = vergleicher.Vergleiche(soll, ist);

        Assert.AreEqual(0, ergebnis.ZuErstellen.Count);
        Assert.AreEqual(1, ergebnis.ZuAktualisieren.Count);
        Assert.AreEqual(0, ergebnis.ZuLoeschen.Count);
        Assert.AreEqual("NeuerWert", ergebnis.ZuAktualisieren[0].Soll.Wert);
        Assert.AreEqual("AlterWert", ergebnis.ZuAktualisieren[0].Ist.Wert);
    }

    [Test]
    public void Wenn_identische_Eintraege_dann_unveraendert()
    {
        var vergleicher = ErstelleStringVergleicher();
        var soll = new List<TestDto> { new TestDto("A", "Wert1") };
        var ist = new List<TestDto> { new TestDto("A", "Wert1") };

        var ergebnis = vergleicher.Vergleiche(soll, ist);

        Assert.AreEqual(0, ergebnis.ZuErstellen.Count);
        Assert.AreEqual(0, ergebnis.ZuAktualisieren.Count);
        Assert.AreEqual(0, ergebnis.ZuLoeschen.Count);
        Assert.AreEqual(1, ergebnis.Unveraendert.Count);
    }

    private SollIstVergleicher<TestDto, string> ErstelleStringVergleicher()
    {
        return new SollIstVergleicher<TestDto, string>(
            schluesselSelektor: dto => dto.Schluessel,
            sindGleich: (a, b) => a.Wert == b.Wert);
    }

    private class TestDto
    {
        public string Schluessel { get; }
        public string Wert { get; }

        public TestDto(string schluessel, string wert)
        {
            Schluessel = schluessel;
            Wert = wert;
        }
    }
}
```

## Varianten

### Mit Batch-Verarbeitung

Für große Datenmengen kann das Repository Batch-Operationen unterstützen:

```csharp
_abschrift.ErstelleViele(ergebnis.ZuErstellen);
_abschrift.AktualisiereViele(ergebnis.ZuAktualisieren.Select(x => x.Soll));
_abschrift.LoescheViele(ergebnis.ZuLoeschen.Select(x => x.KundenNr));
```

### Mit Transaktionen

```csharp
using (var transaktion = _databaseAccessor.BeginTransaction())
{
    try
    {
        // Änderungen durchführen
        transaktion.Commit();
    }
    catch
    {
        transaktion.Rollback();
        throw;
    }
}
```

## Checkliste

- [ ] Gemeinsames DTO für beide Datenquellen definiert
- [ ] Schlüssel-Selektor definiert (eindeutige Identifikation)
- [ ] Gleichheits-Vergleich implementiert (welche Felder sind relevant?)
- [ ] Beide Repositories liefern das gleiche DTO
- [ ] Repository-Methoden für Create/Update/Delete vorhanden
- [ ] Fehlerbehandlung im Service implementiert
- [ ] Bei großen Datenmengen: Batch-Verarbeitung erwägen
- [ ] Bei kritischen Daten: Transaktionen verwenden
