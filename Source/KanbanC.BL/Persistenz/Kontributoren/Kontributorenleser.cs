using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Persistenz.Kontributoren;

// Die **referenzierten** Kontributoren eines Boards, fünf Herkünfte in einer Abfrage vereinigt:
// der Verantwortliche der Karte und die Urheber von Kommentar, Anhang, Dateiverweis und
// Zeiteintrag. Nicht die Personenliste der Installation — ein Board exportiert seinen Inhalt,
// nicht das Adressbuch daneben.
// Stillgelegte fallen nicht heraus: ihre Arbeit steht in der Datei, dieselbe Regel, die der
// Zeitenleser schon führt.
internal static class Kontributorenleser
{
    private const string IsoDatumsformat = "yyyy-MM-dd";

    public static IReadOnlyList<Kontributor> LiesKontributorenDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Kontributorzeile>(@"
            SELECT k.KontributorId, k.Name, k.Kontributorart, t.StillgelegtAm
              FROM Kontributor k
              LEFT JOIN Kontributorstilllegung t ON t.Kontributor = k.KontributorId
             WHERE k.KontributorId IN (SELECT e.Kontributor
                                         FROM Karteneigenschaft e
                                         JOIN Karte a ON a.KarteId = e.Karte
                                         JOIN Spalte s ON s.SpalteId = a.Spalte
                                        WHERE s.Board = @BoardId
                                          AND e.Kontributor IS NOT NULL
                                        UNION
                                       SELECT m.Kontributor
                                         FROM Kommentar m
                                         JOIN Karte a ON a.KarteId = m.Karte
                                         JOIN Spalte s ON s.SpalteId = a.Spalte
                                        WHERE s.Board = @BoardId
                                        UNION
                                       SELECT h.Kontributor
                                         FROM Anhang h
                                         JOIN Karte a ON a.KarteId = h.Karte
                                         JOIN Spalte s ON s.SpalteId = a.Spalte
                                        WHERE s.Board = @BoardId
                                        UNION
                                       SELECT v.Kontributor
                                         FROM Dateiverweis v
                                         JOIN Karte a ON a.KarteId = v.Karte
                                         JOIN Spalte s ON s.SpalteId = a.Spalte
                                        WHERE s.Board = @BoardId
                                        UNION
                                       SELECT z.Kontributor
                                         FROM Zeiteintrag z
                                         JOIN Karte a ON a.KarteId = z.Karte
                                         JOIN Spalte s ON s.SpalteId = a.Spalte
                                        WHERE s.Board = @BoardId)
             ORDER BY k.Name COLLATE NOCASE, k.KontributorId", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsKontributor).ToList();
    }

    // Eine fehlende Zeile in Kontributorstilllegung heißt aktiv.
    private static Kontributor AlsKontributor(Kontributorzeile zeile)
    {
        return new Kontributor(zeile.KontributorId, zeile.Name, Enum.Parse<Kontributorart>(zeile.Kontributorart), AlsDatum(zeile.StillgelegtAm));
    }

    private static DateOnly? AlsDatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record Kontributorzeile(long KontributorId, string Name, string Kontributorart, string? StillgelegtAm);
}
