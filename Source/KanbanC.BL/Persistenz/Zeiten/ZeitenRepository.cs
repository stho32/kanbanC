using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Persistenz.Zeiten;

public sealed class ZeitenRepository : IZeitenRepository
{
    private const string IsoZeitpunktformat = "O";
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public ZeitenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    // Suchen und Schreiben stehen in **einer** Transaktion, damit zwischen „läuft schon einer?" und
    // „dann schreibe ich einen" kein Fenster bleibt. Greift die Serialisierung nicht, schlägt der
    // partielle UNIQUE-Index aus 018 sichtbar an, statt still einen zweiten offenen Eintrag zu
    // legen.
    // Der zweite Start desselben Paares schreibt **nichts** und gibt denselben Eintrag zurück: das
    // Ziel des Aufrufs ist schon erreicht, und ein Befund wäre eine Meldung ohne
    // Kompensationsaktion.
    public Zeitmessungsstart? StarteZeitmessung(long karteId, long kontributorId, DateTimeOffset beginn)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var dieKarteGibtEsNicht = !GibtEsDieKarte(verbindung, transaktion, karteId);
        if (dieKarteGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst "diese Karte gibt es nicht" (404)
        }

        var laufender = LiesLaufendenEintrag(verbindung, transaktion, karteId, kontributorId);
        var fuerDiesesPaarLaeuftSchonEiner = laufender is not null;
        if (fuerDiesesPaarLaeuftSchonEiner)
        {
            transaktion.Commit();
            return new Zeitmessungsstart(laufender!, IstNeu: false);
        }

        var zeiteintragId = FuegeZeiteintragEin(verbindung, transaktion, karteId, kontributorId, beginn);
        var angelegter = Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId);
        transaktion.Commit();
        return new Zeitmessungsstart(angelegter, IstNeu: true);
    }

    // Lesen, Rechnen und Schreiben stehen in **einer** Transaktion, damit zwischen „läuft der
    // noch?" und „dann schließe ich ihn" kein Fenster bleibt.
    // Der Wiederhol-Schutz sitzt im Schreibweg selbst: `AND Ende IS NULL` im UPDATE. Ein zweiter
    // Stopp trifft damit keine Zeile, und das Ende bleibt stehen, wo es steht — der Aufrufer
    // verlöre sonst gemessene Zeit. Dasselbe Verhältnis wie ON CONFLICT DO NOTHING in
    // SchreibeStilllegung.
    // Das Schreibschloss fällt vor dem ersten Lesen (BEGIN IMMEDIATE, Muster OrdneZu): zwei
    // gleichzeitige Stopper lesen sonst beide und scheitern beide am Hochstufen. Anders als beim
    // Start fängt hier kein UNIQUE-Index den Verlierer auf — die Bedingung im UPDATE tut es.
    public Zeiteintrag? BeendeZeitmessung(long karteId, long zeiteintragId, DateTimeOffset uhrzeit)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var denEintragGibtEsAnDieserKarteNicht = !GibtEsDenZeiteintragAnDieserKarte(verbindung, transaktion, karteId, zeiteintragId);
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null; // stil-check: C25 null heisst "diesen Zeiteintrag gibt es an dieser Karte nicht" (404)
        }

        var vorherigerStand = Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId);
        SchreibeEndeSolangeErLaeuft(verbindung, transaktion, zeiteintragId, Zeitmessungsende.Fuer(vorherigerStand.Beginn, uhrzeit));
        var beendeter = Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId);
        transaktion.Commit();
        return beendeter;
    }

    // Ein Zeiteintrag, den es zwar gibt, der aber an einer anderen Karte liegt, ist an dieser
    // keiner — dieselbe Regel wie bei Anhang und Dateiverweis.
    private static bool GibtEsDenZeiteintragAnDieserKarte(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long zeiteintragId)
    {
        var parameter = new { ZeiteintragId = zeiteintragId, Karte = karteId };
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Zeiteintrag
             WHERE ZeiteintragId = @ZeiteintragId
               AND Karte = @Karte", parameter, transaktion);
        return anzahl > 0;
    }

    // Das Ende geht als ISO-Text in UTC durch dieselbe Spalte wie der Beginn: Dapper
    // materialisiert aus ihr keinen DateTimeOffset, und nur bei einheitlichem Versatz sortiert
    // Text lexikografisch wie chronologisch.
    private static void SchreibeEndeSolangeErLaeuft(IDbConnection verbindung, IDbTransaction transaktion, long zeiteintragId, DateTimeOffset ende)
    {
        var parameter = new
        {
            ZeiteintragId = zeiteintragId,
            Ende = ende.ToUniversalTime().ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
        };
        verbindung.Execute(@"
            UPDATE Zeiteintrag
               SET Ende = @Ende
             WHERE ZeiteintragId = @ZeiteintragId
               AND Ende IS NULL", parameter, transaktion);
    }

    private static bool GibtEsDieKarte(IDbConnection verbindung, IDbTransaction transaktion, long karteId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Karte
             WHERE KarteId = @KarteId", new { KarteId = karteId }, transaktion);
        return anzahl > 0;
    }

    // Ende IS NULL ist die ganze Bedingung: ein abgeschlossener Eintrag desselben Paares steht
    // einem neuen Start nicht im Weg.
    private static Zeiteintrag? LiesLaufendenEintrag(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long kontributorId)
    {
        var parameter = new { Karte = karteId, Kontributor = kontributorId };
        var zeiteintragId = verbindung.ExecuteScalar<long?>(@"
            SELECT ZeiteintragId
              FROM Zeiteintrag
             WHERE Karte = @Karte
               AND Kontributor = @Kontributor
               AND Ende IS NULL", parameter, transaktion);
        if (zeiteintragId is null)
        {
            return null; // stil-check: C25 null heisst „für dieses Paar läuft keiner"
        }

        return Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId.Value);
    }

    // **Ende bleibt beim Einfügen NULL** — ein laufender Timer ist genau der Eintrag ohne Ende;
    // gesetzt wird es erst beim Stopp.
    // Der Beginn geht als ISO-Text durch die Spalte: Microsoft.Data.Sqlite meldet für sie den Typ
    // String, und Dapper materialisiert daraus keinen DateTimeOffset (belegt in
    // SqliteEigenschaftenTests). Geschrieben wird derselbe Text, den der Zeitenleser umrechnet.
    private static long FuegeZeiteintragEin(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long kontributorId, DateTimeOffset beginn)
    {
        var parameter = new
        {
            Karte = karteId,
            Kontributor = kontributorId,
            Beginn = beginn.ToUniversalTime().ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, NULL);
            SELECT last_insert_rowid();", parameter, transaktion);
    }
}
