using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Fehler;

// Eine andere Lage als „gibt es nicht": den Kontributor gibt es, er arbeitet nur nicht mehr mit.
// Deshalb ein eigener Code und eine eigene Stelle — und deshalb 400 statt 404: es fehlt kein
// Ding, es wurde eine Regel verletzt. Nichtgefunden.MeldetEinFehlendesDing kennt diesen Code
// bewusst nicht.
public static class Stillgelegt
{
    private const string KontributorStillgelegt = "kontributor-stillgelegt";
    private const string Kompensationsweg = "`GET /api/kontributoren` abrufen und den Aufruf mit einer KontributorId ohne „stillgelegtAm“ wiederholen — oder ihn über `PUT /api/kontributoren/{kontributorId}/stilllegung` mit „istStillgelegt“ = false zurückholen.";

    public static Fehlerbefund Kontributor(long kontributorId)
    {
        return new Fehlerbefund(
            KontributorStillgelegt,
            $"Der Kontributor mit der Nummer {kontributorId} ist stillgelegt und kann nicht verantwortlich sein.",
            Kompensationsweg);
    }

    // Die Schwester für den Urheber eines Kommentars, mit **demselben** Code: die Lage ist
    // dieselbe, und ein zweiter Code berührte Nichtgefunden.AlleCodes und die Statusabbildung,
    // ohne dass sich etwas unterschiede. Eigen ist nur die Meldung — die Schwester oben sagt
    // wörtlich „kann nicht verantwortlich sein", und das wäre am Kommentar eine Falschaussage:
    // hier wird niemand zuständig gemacht, hier sagt jemand etwas.
    public static Fehlerbefund Kommentarurheber(long kontributorId)
    {
        return new Fehlerbefund(
            KontributorStillgelegt,
            $"Der Kontributor mit der Nummer {kontributorId} ist stillgelegt und kann keinen Kommentar mehr schreiben.",
            Kompensationsweg);
    }

    // Die dritte Schwester, wieder mit demselben Code und wieder mit eigener Meldung: „kann nicht
    // verantwortlich sein" und „kann keinen Kommentar mehr schreiben" wären am Anhang beide eine
    // Falschaussage — hier legt jemand eine Datei zur Karte.
    public static Fehlerbefund Anhangurheber(long kontributorId)
    {
        return new Fehlerbefund(
            KontributorStillgelegt,
            $"Der Kontributor mit der Nummer {kontributorId} ist stillgelegt und kann keine Datei mehr anhängen.",
            Kompensationsweg);
    }
}
