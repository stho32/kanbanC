using System.Globalization;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Was an einer fremd bewegten Karte steht: „Nina Barth · vor 3 Sek" oder „Claude-Agent · über die
// API · gerade eben". Gerechnet in der Oberflächenschicht wie Laufplakette, Laufzaehler, Dauerform
// und Zeitpunktform — nicht gespeichert und nicht mitgesendet.
// **null heißt „keine Marke":** die eigene Handlung bekommt keine. Wer selbst zieht, weiß es
// schon; die Rückmeldung war die Bewegung unter der Maus. Der Vermerk darüber hängt am Kreislauf
// und nicht an der Identität — sonst schwiege ein zweiter Browser derselben Person mit, und genau
// der soll die Änderung sehen.
// **Der Name kommt aus der Kontributorenliste und nicht aus dem Ereignis**, damit ein Umbenennen
// von selbst nachzieht; ein unbekannter oder fehlender Urheber ergibt eine Marke ohne Namen.
// **Der Weg steht nur bei Api:** die Oberfläche ist der Normalfall und braucht keine Nennung. Die
// Unterscheidung läuft über Anwesenheit und Wortlaut, nie über die Farbe — Olive und Terrakotta
// tragen in diesem Canvas die Art des Kontributors.
// „jetzt" ist ein Parameter und keine Uhr im Inneren, Muster Zeitmessungsende.Fuer: nur so ist
// „vor 3 Sek" ohne Zeitmanipulation prüfbar.
public static class Einflugmarke
{
    private const string Trenner = " · ";
    private const string UeberDieApi = "über die API";
    private const string GeradeEben = "gerade eben";

    public static string? Fuer(Kartenereignis ereignis, IReadOnlyList<Kontributor> kontributoren, IReadOnlyCollection<long> eigeneZuege, DateTimeOffset jetzt)
    {
        var dieBewegungWarDieEigene = eigeneZuege.Contains(ereignis.Karte);
        if (dieBewegungWarDieEigene)
        {
            return null;
        }

        return string.Join(Trenner, Bestandteile(ereignis, kontributoren, jetzt));
    }

    // Drei Fragen in einer Zeile, und jede darf ausfallen außer der letzten: wer, über welchen Weg,
    // wann.
    private static IEnumerable<string> Bestandteile(Kartenereignis ereignis, IReadOnlyList<Kontributor> kontributoren, DateTimeOffset jetzt)
    {
        var name = NameDesUrhebers(ereignis.Urheber, kontributoren);
        if (name is not null)
        {
            yield return name;
        }

        var dieBewegungKamUeberDieApi = ereignis.Weg == Ereignisweg.Api;
        if (dieBewegungKamUeberDieApi)
        {
            yield return UeberDieApi;
        }

        yield return AlsZeitangabe(ereignis.Zeitpunkt, jetzt);
    }

    private static string? NameDesUrhebers(long? urheber, IReadOnlyList<Kontributor> kontributoren)
    {
        if (urheber is null)
        {
            return null;
        }

        var genannter = kontributoren.FirstOrDefault(kontributor => kontributor.KontributorId == urheber.Value);
        if (genannter is null)
        {
            return null;
        }

        return genannter.Name;
    }

    // Sekundengenau statt minutengenau wie in der Metazeile eines Kommentars: eine Marke steht nur
    // etwa zehn Sekunden, und „vor 0 Min" sagte darin nichts. Ein Zeitpunkt, der wegen abweichender
    // Uhren in der Zukunft liegt, wird zu „gerade eben" statt zu einer negativen Zahl.
    private static string AlsZeitangabe(DateTimeOffset zeitpunkt, DateTimeOffset jetzt)
    {
        var abstand = jetzt - zeitpunkt;
        var dieBewegungIstEbenErstGeschehen = abstand < TimeSpan.FromSeconds(1);
        if (dieBewegungIstEbenErstGeschehen)
        {
            return GeradeEben;
        }

        var sekunden = (int)abstand.TotalSeconds;
        return $"vor {sekunden.ToString(CultureInfo.InvariantCulture)} Sek";
    }
}
