using Dapper;
using KanbanC.BL.Interfaces.Persistenz;

namespace KanbanC.BL.Persistenz.Migrationen;

public sealed class Migrationslaeufer
{
    private const string SkriptEndung = ".sql";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public Migrationslaeufer(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public void FuehreAus()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        foreach (var skriptName in EingebetteteSkriptNamenInReihenfolge())
        {
            var sql = LeseSkript(skriptName);
            verbindung.Execute(sql);
        }
    }

    private static IEnumerable<string> EingebetteteSkriptNamenInReihenfolge()
    {
        var alleRessourcen = typeof(Migrationslaeufer).Assembly.GetManifestResourceNames();
        var skripte = alleRessourcen.Where(name => name.EndsWith(SkriptEndung, StringComparison.Ordinal));
        return skripte.OrderBy(name => name, StringComparer.Ordinal);
    }

    private static string LeseSkript(string skriptName)
    {
        using var strom = typeof(Migrationslaeufer).Assembly.GetManifestResourceStream(skriptName);
        var skriptIstNichtLesbar = strom is null;
        if (skriptIstNichtLesbar)
        {
            throw new InvalidOperationException($"Migrationsskript {skriptName} ist nicht lesbar.");
        }

        using var leser = new StreamReader(strom!);
        return leser.ReadToEnd();
    }
}
