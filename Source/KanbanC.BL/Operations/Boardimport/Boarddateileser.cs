using System.Text.Json;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Export;
using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Operations.Boardimport;

// Aus einem Dateistrom wird eine Boarddatei. **Der Leser braucht kein Board und keine
// Datenbank** — er ist der Teil dieses Slice, der allein an der Antwort prüfbar ist.
// Gelesen wird der Vertrag Boardexport mit **denselben** Serialisierungsoptionen, mit denen
// Exportdatei schreibt: ein zweiter Dateityp oder ein zweiter Optionssatz liefe bei der nächsten
// Ergänzung auseinander, und die Anwendung läse ihre eigene Datei nicht mehr.
// Eine von Hand bearbeitete Datei kann Angaben weglassen, die eine ausgeleitete immer trägt;
// deshalb wird jede Pflichtangabe genannt, die fehlt, statt später an ihr zu scheitern.
public static class Boarddateileser
{
    public static Ergebnis<Boardexport> Lies(Stream inhalt, string dateiname)
    {
        Boardexport? gelesene;
        try
        {
            gelesene = JsonSerializer.Deserialize<Boardexport>(inhalt, Exportdatei.Dateiform);
        }
        catch (JsonException ausnahme)
        {
            return Zurueckgewiesen(dateiname, ausnahme.Message);
        }

        var dieDateiTraegtKeinObjekt = gelesene is null;
        if (dieDateiTraegtKeinObjekt)
        {
            return Zurueckgewiesen(dateiname, "sie trägt kein JSON-Objekt.");
        }

        var fehlendeAngaben = FehlendeAngaben(gelesene!);
        var derDateiFehltEineAngabe = fehlendeAngaben.Count > 0;
        if (derDateiFehltEineAngabe)
        {
            return Zurueckgewiesen(dateiname, "es fehlt " + string.Join(", ", fehlendeAngaben) + ".");
        }

        var fehlendeListenzeilen = FehlendeListenzeilen(gelesene!);
        var einerListenzeileFehltEineAngabe = fehlendeListenzeilen.Count > 0;
        if (einerListenzeileFehltEineAngabe)
        {
            return Zurueckgewiesen(dateiname, "es fehlt " + string.Join(", ", fehlendeListenzeilen) + ".");
        }

        var fehlendeZeilenangaben = FehlendeZeilenangaben(gelesene!);
        var einerZeileFehltEineAngabe = fehlendeZeilenangaben.Count > 0;
        if (einerZeileFehltEineAngabe)
        {
            return Zurueckgewiesen(dateiname, "es fehlt " + string.Join(", ", fehlendeZeilenangaben) + ".");
        }

        return Ergebnis<Boardexport>.Erfolg(gelesene!);
    }

    // Eine fremde JSON-Datei ist kein Board: sie parst, trägt aber keinen Exportkopf. Genannt wird
    // **jede** fehlende Angabe und nicht die erste — wer die falsche Datei erwischt hat, soll
    // nicht Runde um Runde eine weitere Lücke erfahren.
    private static IReadOnlyList<string> FehlendeAngaben(Boardexport datei)
    {
        var fehlende = new List<string>();
        Sammle(fehlende, datei.Kopf is null, "der Exportkopf „kopf“");
        Sammle(fehlende, datei.Board is null, "das Board „board“");
        Sammle(fehlende, datei.Board is not null && datei.Board.Name is null, "am Board der Name „name“");
        Sammle(fehlende, datei.Spalten is null, "die Spaltenliste „spalten“");
        Sammle(fehlende, datei.Kartenklassen is null, "die Kartenklassenliste „kartenklassen“");
        Sammle(fehlende, datei.Kontributoren is null, "die Kontributorenliste „kontributoren“");
        Sammle(fehlende, datei.Karten is null, "die Kartenliste „karten“");
        Sammle(fehlende, datei.Zeiteintraege is null, "die Zeitenliste „zeiteintraege“");
        return fehlende;
    }

    // Eine von Hand bearbeitete Datei kann eine Zeile durch `null` ersetzen oder ihr die
    // Pflichtangaben nehmen. Ohne diese Pruefung schlueg der Lauf spaeter mit einer Ausnahme fehl
    // statt mit einem Befund — und die Zusage „jede Zurueckweisung nennt Grund, Werte und
    // Kompensationsaktion" waere gebrochen.
    private static IReadOnlyList<string> FehlendeListenzeilen(Boardexport datei)
    {
        var fehlende = new List<string>();
        SammleFehlendeSpalten(fehlende, datei.Spalten);
        SammleFehlendeKartenklassen(fehlende, datei.Kartenklassen);
        SammleFehlendeKontributoren(fehlende, datei.Kontributoren);
        SammleFehlendeKartenzeilen(fehlende, datei.Karten);
        SammleFehlendeZeiteintragszeilen(fehlende, datei.Zeiteintraege);
        return fehlende.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void SammleFehlendeSpalten(List<string> fehlende, IReadOnlyList<Exportspalte> spalten)
    {
        foreach (var spalte in spalten)
        {
            Sammle(fehlende, spalte is null, "eine Zeile der Spaltenliste");
            Sammle(fehlende, spalte is not null && spalte.Bezeichnung is null, "an einer Spalte die Bezeichnung „bezeichnung“");
        }
    }

    private static void SammleFehlendeKartenklassen(List<string> fehlende, IReadOnlyList<Kartenklasse> kartenklassen)
    {
        foreach (var kartenklasse in kartenklassen)
        {
            Sammle(fehlende, kartenklasse is null, "eine Zeile der Kartenklassenliste");
            Sammle(fehlende, kartenklasse is not null && kartenklasse.Name is null, "an einer Kartenklasse der Name „name“");
            Sammle(fehlende, kartenklasse is not null && kartenklasse.Praefix is null, "an einer Kartenklasse das Präfix „praefix“");
        }
    }

    private static void SammleFehlendeKontributoren(List<string> fehlende, IReadOnlyList<Kontributor> kontributoren)
    {
        foreach (var kontributor in kontributoren)
        {
            Sammle(fehlende, kontributor is null, "eine Zeile der Kontributorenliste");
            Sammle(fehlende, kontributor is not null && kontributor.Name is null, "an einem Kontributor der Name „name“");
        }
    }

    private static void SammleFehlendeKartenzeilen(List<string> fehlende, IReadOnlyList<Exportkarte> karten)
    {
        foreach (var exportkarte in karten)
        {
            Sammle(fehlende, exportkarte is null, "eine Zeile der Kartenliste");
        }
    }

    private static void SammleFehlendeZeiteintragszeilen(List<string> fehlende, IReadOnlyList<Zeiteintrag> zeiteintraege)
    {
        foreach (var zeiteintrag in zeiteintraege)
        {
            Sammle(fehlende, zeiteintrag is null, "eine Zeile der Zeitenliste");
        }
    }

    // Dieselbe Strenge eine Ebene tiefer. Genannt wird die **Zeilenart** und nicht die Stelle in
    // der Liste: an einer von Hand bearbeiteten Datei fehlt eine Angabe selten nur einmal.
    private static IReadOnlyList<string> FehlendeZeilenangaben(Boardexport datei)
    {
        var fehlende = new List<string>();
        foreach (var exportkarte in datei.Karten)
        {
            SammleFehlendeAngabenDerKarte(fehlende, exportkarte);
        }

        foreach (var zeiteintrag in datei.Zeiteintraege)
        {
            Sammle(fehlende, zeiteintrag.Kontributor is null, "an einem Zeiteintrag der Kontributor „kontributor“");
        }

        return fehlende.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void SammleFehlendeAngabenDerKarte(List<string> fehlende, Exportkarte exportkarte)
    {
        var derKarteFehltDieRohdatenkarte = exportkarte.Karte is null;
        if (derKarteFehltDieRohdatenkarte)
        {
            fehlende.Add("an einer Karte die Kartenzeile „karte“");
            return;
        }

        var karte = exportkarte.Karte!;
        Sammle(fehlende, karte.Karte is null, "an einer Karte die Kartenangaben „karte.karte“");
        Sammle(fehlende, karte.Karte is not null && karte.Karte.Titel is null, "an einer Karte der Titel „titel“");
        Sammle(fehlende, karte.Etiketten is null, "an einer Karte die Etikettenliste „etiketten“");
        Sammle(fehlende, karte.Teilaufgaben is null, "an einer Karte die Teilaufgabenliste „teilaufgaben“");
        Sammle(fehlende, karte.Kommentare is null, "an einer Karte die Kommentarliste „kommentare“");
        Sammle(fehlende, karte.Anhaenge is null, "an einer Karte die Anhangliste „anhaenge“");
        Sammle(fehlende, karte.Dateiverweise is null, "an einer Karte die Dateiverweisliste „dateiverweise“");
        SammleFehlendeUrheber(fehlende, karte);
    }

    private static void SammleFehlendeUrheber(List<string> fehlende, Rohdatenkarte karte)
    {
        SammleFehlendeKommentarurheber(fehlende, karte.Kommentare);
        SammleFehlendeAnhangurheber(fehlende, karte.Anhaenge);
        SammleFehlendeDateiverweisurheber(fehlende, karte.Dateiverweise);
    }

    private static void SammleFehlendeKommentarurheber(List<string> fehlende, IReadOnlyList<Kommentar> kommentare)
    {
        var dieListeFehlt = kommentare is null;
        if (dieListeFehlt)
        {
            return;
        }

        foreach (var kommentar in kommentare!)
        {
            Sammle(fehlende, kommentar.Urheber is null, "an einem Kommentar der Urheber „urheber“");
        }
    }

    private static void SammleFehlendeAnhangurheber(List<string> fehlende, IReadOnlyList<Anhang> anhaenge)
    {
        var dieListeFehlt = anhaenge is null;
        if (dieListeFehlt)
        {
            return;
        }

        foreach (var anhang in anhaenge!)
        {
            Sammle(fehlende, anhang.Urheber is null, "an einem Anhang der Urheber „urheber“");
        }
    }

    private static void SammleFehlendeDateiverweisurheber(List<string> fehlende, IReadOnlyList<Dateiverweis> dateiverweise)
    {
        var dieListeFehlt = dateiverweise is null;
        if (dieListeFehlt)
        {
            return;
        }

        foreach (var dateiverweis in dateiverweise!)
        {
            Sammle(fehlende, dateiverweis.Urheber is null, "an einem Dateiverweis der Urheber „urheber“");
        }
    }

    private static void Sammle(List<string> fehlende, bool dieAngabeFehlt, string angabe)
    {
        if (dieAngabeFehlt)
        {
            fehlende.Add(angabe);
        }
    }

    private static Ergebnis<Boardexport> Zurueckgewiesen(string dateiname, string grund)
    {
        return Ergebnis<Boardexport>.Zurueckgewiesen(new Pruefbefunde([Unlesbar.Boarddatei(dateiname, grund)]));
    }
}
