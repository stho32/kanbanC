using Dapper;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

// **Der abgezogene Bestand, Zeile für Zeile über siebzehn Tabellen** — die Grundlage des
// Beweises, dass ein Import bestehende Boards nicht anfasst. Verglichen wird nicht ein Beispiel,
// sondern jede Zeile mit jeder Spalte; ein Abzug vor dem Lauf gegen einen danach.
// Kontributor und Kontributorstilllegung stehen bewusst **nicht** darin: sie gehoeren keinem
// Board, und dass die Personenliste waechst, ist die eine benannte Ausnahme.
internal static class Bestandsabzug
{
    private static readonly (string Tabelle, string Abfrage)[] Abzuege =
    [
        ("Board", @"
            SELECT BoardId, Name, Art, Starttermin, Zieltermin
              FROM Board
             WHERE BoardId IN @Boards
             ORDER BY BoardId"),
        ("Boardeinstellung", @"
            SELECT Board, ZeigtKartenzahl
              FROM Boardeinstellung
             WHERE Board IN @Boards
             ORDER BY Board"),
        ("Boardarchivierung", @"
            SELECT Board
              FROM Boardarchivierung
             WHERE Board IN @Boards
             ORDER BY Board"),
        ("Spalte", @"
            SELECT SpalteId, Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze
              FROM Spalte
             WHERE Board IN @Boards
             ORDER BY SpalteId"),
        ("Kartenklasse", @"
            SELECT KartenklasseId, Board, Name, Praefix, Zaehlerstand
              FROM Kartenklasse
             WHERE Board IN @Boards
             ORDER BY KartenklasseId"),
        ("Karte", @"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY k.KarteId"),
        ("Karteneigenschaft", @"
            SELECT e.Karte, e.Beschreibung, e.Kontributor, e.FaelligAm, e.Farbe
              FROM Karteneigenschaft e
              JOIN Karte k ON k.KarteId = e.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY e.Karte"),
        ("Karteerledigung", @"
            SELECT r.Karte, r.ErledigtAm
              FROM Karteerledigung r
              JOIN Karte k ON k.KarteId = r.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY r.Karte"),
        ("Kartenarchivierung", @"
            SELECT a.Karte
              FROM Kartenarchivierung a
              JOIN Karte k ON k.KarteId = a.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY a.Karte"),
        ("Kartenklassenzuordnung", @"
            SELECT z.KartenklassenzuordnungId, z.Karte, z.Kartenklasse, z.Zaehlerstand
              FROM Kartenklassenzuordnung z
              JOIN Karte k ON k.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY z.KartenklassenzuordnungId"),
        ("Kartensollzeit", @"
            SELECT t.Karte, t.SollzeitVonStunden, t.SollzeitBisStunden
              FROM Kartensollzeit t
              JOIN Karte k ON k.KarteId = t.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY t.Karte"),
        ("Etikett", @"
            SELECT e.Karte, e.Text
              FROM Etikett e
              JOIN Karte k ON k.KarteId = e.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY e.Karte, e.Text"),
        ("Teilaufgabe", @"
            SELECT t.TeilaufgabeId, t.Karte, t.Text, t.Position, t.Abgehakt
              FROM Teilaufgabe t
              JOIN Karte k ON k.KarteId = t.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY t.TeilaufgabeId"),
        ("Kommentar", @"
            SELECT m.KommentarId, m.Karte, m.Kontributor, m.Text, m.Zeitpunkt
              FROM Kommentar m
              JOIN Karte k ON k.KarteId = m.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY m.KommentarId"),
        ("Anhang", @"
            SELECT h.AnhangId, h.Karte, h.Kontributor, h.Dateiname, h.Dateigroesse, h.Zeitpunkt
              FROM Anhang h
              JOIN Karte k ON k.KarteId = h.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY h.AnhangId"),
        ("Dateiverweis", @"
            SELECT v.DateiverweisId, v.Karte, v.Kontributor, v.Pfad, v.Zeitpunkt
              FROM Dateiverweis v
              JOIN Karte k ON k.KarteId = v.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY v.DateiverweisId"),
        ("Zeiteintrag", @"
            SELECT z.ZeiteintragId, z.Karte, z.Kontributor, z.Beginn, z.Ende
              FROM Zeiteintrag z
              JOIN Karte k ON k.KarteId = z.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board IN @Boards
             ORDER BY z.ZeiteintragId"),
    ];

    internal static int Tabellenzahl => Abzuege.Length;

    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> Zieh(TemporaereDatenbank datenbank, IReadOnlyList<long> boardIds)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var jeTabelle = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var abzug in Abzuege)
        {
            var zeilen = verbindung.Query(abzug.Abfrage, new { Boards = boardIds });
            jeTabelle[abzug.Tabelle] = zeilen.Select(AlsText).ToList();
        }

        return jeTabelle;
    }

    // Die Personenliste steht daneben, weil sie die eine Tabelle ist, die waechst — und weil
    // gerade deshalb zu zeigen ist, dass keine ihrer vorhandenen Zeilen angefasst wurde.
    internal static IReadOnlyList<string> ZiehPersonenliste(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query(@"
            SELECT k.KontributorId, k.Name, k.Kontributorart, s.StillgelegtAm
              FROM Kontributor k
                   LEFT JOIN Kontributorstilllegung s
                          ON s.Kontributor = k.KontributorId
             ORDER BY k.KontributorId");
        return zeilen.Select(AlsText).ToList();
    }

    private static string AlsText(dynamic zeile)
    {
        var felder = (IDictionary<string, object>)zeile;
        return string.Join(" | ", felder.Select(feld => $"{feld.Key}={feld.Value}"));
    }
}
