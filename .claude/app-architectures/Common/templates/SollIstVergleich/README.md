# Soll-Ist-Vergleich Templates

## Verwendung

1. Kopiere `SollIstVergleichErgebnis.cs` und `SollIstVergleicher.cs` in dein Projekt
2. Ersetze `{{NAMESPACE}}` durch deinen Namespace
3. Erstelle einen Vergleicher mit passenden Selektoren

## Wichtig: Ein DTO für Soll und Ist

Soll- und Ist-Datenquellen verwenden das **gleiche DTO**. Die Datenquellen (Repositories) sind dafür verantwortlich, ihre Daten in dieses gemeinsame DTO zu konvertieren.

## Beispiel

```csharp
// Gemeinsames DTO für beide Datenquellen
public class KundeDto
{
    public string KundenNr { get; }
    public string Name { get; }
    public string Email { get; }
    // ...
}

// Vergleicher erstellen - ein Typ für Soll und Ist
var vergleicher = new SollIstVergleicher<KundeDto, string>(
    schluesselSelektor: k => k.KundenNr,
    sindGleich: (soll, ist) => soll.Name == ist.Name && soll.Email == ist.Email
);

// Beide Datenquellen liefern KundeDto
var sollzustand = quellRepository.LadeAlleKunden();   // List<KundeDto>
var istzustand = zielRepository.LadeAlleKunden();     // List<KundeDto>

// Vergleich durchführen
var ergebnis = vergleicher.Vergleiche(sollzustand, istzustand);

// Ergebnis verarbeiten
foreach (var neu in ergebnis.ZuErstellen)
{
    zielRepository.Erstelle(neu);
}

foreach (var (soll, ist) in ergebnis.ZuAktualisieren)
{
    zielRepository.Aktualisiere(soll);
}

foreach (var alt in ergebnis.ZuLoeschen)
{
    zielRepository.Loesche(alt.KundenNr);
}
```
