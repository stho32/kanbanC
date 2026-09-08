using KanbanC.BL.Operations.Boardimport;
using KanbanC.BL.Operations.Export;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.TestHelpers;

// Das durchgehende Rechenbeispiel aus R00041 als Datei: Board „KanbanC — Release 2" mit der
// **BoardId 1**, 3 Spalten, 1 Kartenklasse mit Präfix WBS- und Zählerstand 40, die
// Kontributoren Stefan (7, aktiv) und Alt-Kollege (11, stillgelegt), 24 Karten — darunter eine
// archivierte, eine ohne Klasse und ohne Sollband und K24 mit WBS-32 und je einem Eintrag in
// allen fuenf Listen — und 2 Zeiteinträge, davon einer laufend.
internal static class Boarddateibeispiel
{
    internal const string Boardname = "KanbanC — Release 2";
    internal const long BoardId = 1;
    internal const long BereitId = 10;
    internal const long ArbeitId = 11;
    internal const long ErledigtId = 12;
    internal const long KartenklasseId = 3;
    internal const long StefanId = 7;
    internal const long AltKollegeId = 11;
    internal const long ArchivierteKarteId = 122;
    internal const long KlassenloseKarteId = 123;
    internal const long VollstaendigeKarteId = 124;
    internal const int ZaehlerstandDerKlasse = 40;
    internal const int ZaehlerstandK24 = 32;
    internal static readonly DateTimeOffset BeginnZ1 = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset EndeZ1 = new(2026, 9, 6, 10, 30, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset BeginnZ2 = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);
    internal static readonly DateOnly Erledigungstag = new(2026, 9, 4);
    internal static readonly DateOnly Stilllegungstag = new(2026, 9, 1);
    internal static readonly DateOnly Zieltermin = new(2026, 9, 30);

    internal static readonly Kontributor Stefan = new(StefanId, "Stefan", Kontributorart.Mensch, null);
    internal static readonly Kontributor AltKollege = new(AltKollegeId, "Alt-Kollege", Kontributorart.Mensch, Stilllegungstag);

    internal static Boardexport Datei()
    {
        return new Boardexport(
            new Exportkopf(Fassungspruefung.ErwarteteAnwendung, Fassungspruefung.ErwarteteFassung, BeginnZ1, "Die Bytes der Anhänge reisen nicht mit."),
            new Exportboard(BoardId, Boardname, BoardArt.Projekt, null, Zieltermin, ZeigtKartenzahl: true, IstArchiviert: false),
            Spalten(),
            [new Kartenklasse(KartenklasseId, "WBS", "WBS-", ZaehlerstandDerKlasse)],
            [Stefan, AltKollege],
            Karten(),
            Zeiteintraege());
    }

    internal static Stream AlsStrom(Boardexport datei)
    {
        return new MemoryStream(Exportdatei.AlsJson(datei));
    }

    internal static Stream AlsStrom(string text)
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
    }

    private static IReadOnlyList<Exportspalte> Spalten()
    {
        return
        [
            new Exportspalte(BereitId, "Zu erledigen", 1, IstAbschlussspalte: false, null),
            new Exportspalte(ArbeitId, "In Arbeit", 2, IstAbschlussspalte: false, null),
            new Exportspalte(ErledigtId, "Erledigt", 3, IstAbschlussspalte: true, 20),
        ];
    }

    // 21 erledigte Karten, die archivierte K22, die klassenlose K23 und die vollständige K24:
    // zusammen 24.
    private static IReadOnlyList<Exportkarte> Karten()
    {
        var karten = new List<Exportkarte>();
        foreach (var nummer in Enumerable.Range(1, 21))
        {
            karten.Add(Schlichtekarte(100 + nummer, $"K{nummer}", ErledigtId, nummer, Erledigungstag, istArchiviert: false));
        }

        karten.Add(Schlichtekarte(ArchivierteKarteId, "K22", ErledigtId, 22, Erledigungstag, istArchiviert: true));
        karten.Add(Klassenlosekarte());
        karten.Add(Vollstaendigekarte());
        return karten;
    }

    private static Exportkarte Schlichtekarte(long karteId, string titel, long spalteId, int zaehlerstand, DateOnly? erledigtAm, bool istArchiviert)
    {
        var karte = new Karte(karteId, titel, zaehlerstand, erledigtAm, null, null, Kartenfarbe.Ohne, null, $"WBS-{zaehlerstand:00}");
        var rohdatenkarte = new Rohdatenkarte(
            karte,
            spalteId,
            "Erledigt",
            new Archivierung(istArchiviert),
            new Kartenklasse(KartenklasseId, "WBS", "WBS-", ZaehlerstandDerKlasse),
            [],
            [],
            [],
            [],
            []);
        return new Exportkarte(rohdatenkarte, null, null, zaehlerstand);
    }

    // Ohne Klasse und ohne Sollband: sie darf danach kein Ersatzband und keine Ersatzzuordnung
    // bekommen.
    private static Exportkarte Klassenlosekarte()
    {
        var karte = new Karte(KlassenloseKarteId, "K23", 1, null, null, null, Kartenfarbe.Ohne, null, null);
        var rohdatenkarte = new Rohdatenkarte(karte, BereitId, "Zu erledigen", new Archivierung(false), null, [], [], [], [], []);
        return new Exportkarte(rohdatenkarte, null, null, null);
    }

    // K24 traegt WBS-32, das Sollband 2,0–4,0 h, Stefan als Verantwortlichen und je einen Eintrag
    // in allen fuenf Listen.
    private static Exportkarte Vollstaendigekarte()
    {
        var karte = new Karte(VollstaendigeKarteId, "K24", 2, null, "Vollständig oder sichtbar gescheitert.", Zieltermin, Kartenfarbe.Sand, StefanId, $"WBS-{ZaehlerstandK24}");
        var rohdatenkarte = new Rohdatenkarte(
            karte,
            BereitId,
            "Zu erledigen",
            new Archivierung(false),
            new Kartenklasse(KartenklasseId, "WBS", "WBS-", ZaehlerstandDerKlasse),
            ["Rohdaten"],
            [new Teilaufgabe(51, "Zwei Routen bauen", 1, Abgehakt: false)],
            [new Kommentar(61, "Vollständig oder sichtbar gescheitert.", Stefan, BeginnZ1)],
            [new Anhang(71, "bericht.pdf", 2048, Stefan, BeginnZ1)],
            [new Dateiverweis(81, "/ablage/bericht.pdf", Stefan, BeginnZ1)]);
        return new Exportkarte(rohdatenkarte, Stefan, new Zeitband(2.0m, 4.0m), ZaehlerstandK24);
    }

    // Z1 ist abgeschlossen an K24, Z2 läuft an der archivierten K22 — und der Laufende gehört
    // dem stillgelegten Alt-Kollegen.
    private static IReadOnlyList<Zeiteintrag> Zeiteintraege()
    {
        return
        [
            new Zeiteintrag(91, VollstaendigeKarteId, Stefan, BeginnZ1, EndeZ1),
            new Zeiteintrag(92, ArchivierteKarteId, AltKollege, BeginnZ2, null),
        ];
    }
}
