using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Karten;
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
    private static bool GibtEsDenZeiteintragAnDieserKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId, long zeiteintragId)
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
            Ende = AlsIsoText(ende),
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
            Beginn = AlsIsoText(beginn),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, NULL);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    // Ein Eintrag mit Beginn **und** Ende in einem Zug — der partielle Index kann hier nie
    // anschlagen, weil ein Nachtrag nie ohne Ende entsteht. Ein schon laufender Eintrag desselben
    // Paares bleibt deshalb unberührt laufen.
    public Zeiteintrag? TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var dieKarteGibtEsNicht = !GibtEsDieKarte(verbindung, transaktion, karteId);
        if (dieKarteGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst "diese Karte gibt es nicht" (404)
        }

        var zeiteintragId = FuegeAbgeschlossenenZeiteintragEin(verbindung, transaktion, karteId, anfrage);
        var nachgetragener = Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId);
        transaktion.Commit();
        return nachgetragener;
    }

    private static long FuegeAbgeschlossenenZeiteintragEin(IDbConnection verbindung, IDbTransaction transaktion, long karteId, ZeiteintragNachtragenAnfrage anfrage)
    {
        var parameter = new
        {
            Karte = karteId,
            Kontributor = anfrage.Kontributor,
            Beginn = AlsIsoText(anfrage.Beginn),
            Ende = AlsIsoText(anfrage.Ende),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Zeiteintrag (Karte, Kontributor, Beginn, Ende)
            VALUES (@Karte, @Kontributor, @Beginn, @Ende);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    // Der Stand vor einer Änderung: der Dienst braucht den bisherigen Kontributor, um zu wissen,
    // ob überhaupt gewechselt wird.
    public Zeiteintrag? Lies(long karteId, long zeiteintragId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var denEintragGibtEsAnDieserKarteNicht = !GibtEsDenZeiteintragAnDieserKarte(verbindung, transaktion: null, karteId, zeiteintragId);
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null; // stil-check: C25 null heisst "diesen Zeiteintrag gibt es an dieser Karte nicht" (404)
        }

        return Zeitenleser.LiesZeiteintrag(verbindung, transaktion: null, zeiteintragId);
    }

    // Das Schreibschloss fällt wie bei BeendeZeitmessung **vor** dem ersten Lesen: zwei
    // gleichzeitige Änderungen läsen sonst beide und scheiterten beide am Hochstufen.
    // Der Rückfall auf „läuft" wird deshalb **hier drin** geprüft und nicht davor im Dienst: nur
    // unter demselben Schloss bleibt zwischen „für dieses Paar läuft kein anderer" und dem UPDATE
    // kein Fenster, in dem der partielle Index statt eines Befunds zuschlägt.
    public Zeiteintragsaenderung? Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var denEintragGibtEsAnDieserKarteNicht = !GibtEsDenZeiteintragAnDieserKarte(verbindung, transaktion, karteId, zeiteintragId);
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null; // stil-check: C25 null heisst "diesen Zeiteintrag gibt es an dieser Karte nicht" (404)
        }

        var derEintragSollWiederLaufen = anfrage.Ende is null;
        var schonLaufender = LiesLaufendenEintragAusser(verbindung, transaktion, karteId, anfrage.Kontributor, zeiteintragId);
        var einAndererStehtDemRueckfallImWeg = derEintragSollWiederLaufen && schonLaufender is not null;
        if (einAndererStehtDemRueckfallImWeg)
        {
            transaktion.Commit();
            return new Zeiteintragsaenderung(schonLaufender!, WurdeGeaendert: false);
        }

        SchreibeAenderung(verbindung, transaktion, zeiteintragId, anfrage);
        var geaenderter = Zeitenleser.LiesZeiteintrag(verbindung, transaktion, zeiteintragId);
        transaktion.Commit();
        return new Zeiteintragsaenderung(geaenderter, WurdeGeaendert: true);
    }

    // Die Schwester von LiesLaufendenEintrag, die den gerade geänderten Eintrag ausnimmt: er
    // selbst steht seinem eigenen Rückfall auf „läuft" nicht im Weg.
    private static Zeiteintrag? LiesLaufendenEintragAusser(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long kontributorId, long zeiteintragId)
    {
        var parameter = new { Karte = karteId, Kontributor = kontributorId, ZeiteintragId = zeiteintragId };
        var laufendeZeiteintragId = verbindung.ExecuteScalar<long?>(@"
            SELECT ZeiteintragId
              FROM Zeiteintrag
             WHERE Karte = @Karte
               AND Kontributor = @Kontributor
               AND Ende IS NULL
               AND ZeiteintragId <> @ZeiteintragId", parameter, transaktion);
        if (laufendeZeiteintragId is null)
        {
            return null; // stil-check: C25 null heisst „für dieses Paar läuft kein anderer"
        }

        return Zeitenleser.LiesZeiteintrag(verbindung, transaktion, laufendeZeiteintragId.Value);
    }

    // Alle drei Felder in **einem** UPDATE, die Karte in keinem: ein Zeiteintrag wandert nicht auf
    // eine andere Karte.
    private static void SchreibeAenderung(IDbConnection verbindung, IDbTransaction transaktion, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)
    {
        var parameter = new
        {
            ZeiteintragId = zeiteintragId,
            Kontributor = anfrage.Kontributor,
            Beginn = AlsIsoText(anfrage.Beginn),
            Ende = AlsIsoTextOderNichts(anfrage.Ende),
        };
        verbindung.Execute(@"
            UPDATE Zeiteintrag
               SET Kontributor = @Kontributor,
                   Beginn = @Beginn,
                   Ende = @Ende
             WHERE ZeiteintragId = @ZeiteintragId", parameter, transaktion);
    }

    // Zurück kommt das ganze Kartendetail ohne die gelöschte Zeile — Hausform EntferneAnhang
    // und EntferneDateiverweis: dieselbe Seite verbraucht es, ein zweiter Abruf waere ein Fenster,
    // in dem Liste und Summe auseinanderlaufen.
    public Kartendetail? Loesche(long karteId, long zeiteintragId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = _verbindungsfabrik.BeginneSchreibtransaktion(verbindung);

        var derZeiteintragGehoertNichtZuDieserKarte = !LoescheZeiteintragszeile(verbindung, transaktion, karteId, zeiteintragId);
        if (derZeiteintragGehoertNichtZuDieserKarte)
        {
            return null; // stil-check: C25 null heisst "diesen Zeiteintrag gibt es an dieser Karte nicht" (404)
        }

        var detail = Kartenleser.LiesKartendetail(verbindung, transaktion, karteId);
        transaktion.Commit();
        return detail;
    }

    // Die Zahl der gelöschten Zeilen ist zugleich die Auskunft, ob der Eintrag zu dieser Karte
    // gehört — wie bei LoescheAnhangzeile. **Beide** Nummern stehen in der Bedingung.
    private static bool LoescheZeiteintragszeile(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long zeiteintragId)
    {
        var parameter = new { ZeiteintragId = zeiteintragId, Karte = karteId };
        var geloeschteZeilen = verbindung.Execute(@"
            DELETE
              FROM Zeiteintrag
             WHERE ZeiteintragId = @ZeiteintragId
               AND Karte = @Karte", parameter, transaktion);
        return geloeschteZeilen > 0;
    }

    // Ohne Transaktion und ohne Nummer: eine reine Auskunft über den Bestand, wie die lesenden
    // Glieder des BoardService.
    public IReadOnlyList<LaufendeZeitmessung> LiesLaufende()
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        return Zeitenleser.LiesAlleLaufenden(verbindung, transaktion: null);
    }

    // Derselbe ISO-Text in UTC wie beim Start und beim Stopp: Microsoft.Data.Sqlite meldet für
    // die Spalte den Typ String, und nur bei einheitlichem Versatz sortiert Text lexikografisch
    // wie chronologisch.
    private static string AlsIsoText(DateTimeOffset zeitpunkt)
    {
        return zeitpunkt.ToUniversalTime().ToString(IsoZeitpunktformat, CultureInfo.InvariantCulture);
    }

    private static string? AlsIsoTextOderNichts(DateTimeOffset? zeitpunkt)
    {
        if (zeitpunkt is null)
        {
            return null;
        }

        return AlsIsoText(zeitpunkt.Value);
    }
}
