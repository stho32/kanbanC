using System.Globalization;
using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

public static class AnhangValidator
{
    private const string ZuGross = "anhang-zu-gross";

    // Wie beim Kommentar- und beim TeilaufgabenValidator nennt die Kompensation die Route des
    // Aufrufers samt seiner Nummer. **Kein Dublettenbefund:** zwei Dateien gleichen Namens an
    // derselben Karte sind zwei Dateien — auf der Platte kollidieren sie ohnehin nicht, weil sie
    // nach ihrer Nummer heißen. **Kein Urheberbefund:** seine beiden Regeln brauchen den
    // Kontributorenbestand und gehören damit in den Dienst.
    public static Pruefbefunde Pruefe(long karteId, AnhangAnlegenAnfrage anfrage)
    {
        var anhangroute = $"POST /api/karten/{karteId}/anhaenge";
        var befunde = new List<Fehlerbefund>();
        var dateiname = Anhangname.Normalisiert(anfrage.Dateiname);

        var derDateinameIstLeer = dateiname.Length == 0;
        if (derDateinameIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "anhang-name-leer",
                "Ein Anhang braucht einen Dateinamen.",
                $"`{anhangroute}` als multipart mit einer Datei wiederholen, deren „filename“ nichtleer ist."));
        }

        var dieDateiIstLeer = anfrage.Dateigroesse <= 0;
        if (dieDateiIstLeer)
        {
            befunde.Add(new Fehlerbefund(
                "anhang-leer",
                "Eine Datei ohne Inhalt lässt sich nicht anhängen.",
                $"`{anhangroute}` mit einer Datei wiederholen, die mindestens ein Byte trägt."));
        }

        var dieDateiIstZuGross = anfrage.Dateigroesse > Anhangsgrenze.HoechsteDateigroesse;
        if (dieDateiIstZuGross)
        {
            var grenze = Anhangsgrenze.HoechsteDateigroesse.ToString(CultureInfo.InvariantCulture);
            befunde.Add(new Fehlerbefund(
                ZuGross,
                $"Ein Anhang darf höchstens {grenze} Bytes groß sein; diese Datei meldet {anfrage.Dateigroesse.ToString(CultureInfo.InvariantCulture)} Bytes.",
                $"`{anhangroute}` mit einer Datei von höchstens {grenze} Bytes wiederholen."));
        }

        return new Pruefbefunde(befunde);
    }

    // Derselbe Befund für den Fall, in dem der Rahmen den Rumpf schon abgebrochen hat, bevor die
    // Anwendung ihn sah: die gemeldete Größe ist dann unbekannt, die Obergrenze ist es nicht.
    // Ohne diesen Weg käme an der Route eine Fehlerantwort ohne Befund heraus — und genau das
    // schließt der Vertrag aus R00007 aus.
    public static Fehlerbefund RumpfUeberDerGrenze(long karteId)
    {
        var grenze = Anhangsgrenze.HoechsteDateigroesse.ToString(CultureInfo.InvariantCulture);
        return new Fehlerbefund(
            ZuGross,
            $"Der Rumpf des Aufrufs überschreitet die Obergrenze von {grenze} Bytes je Anhang und wurde abgebrochen.",
            $"`POST /api/karten/{karteId}/anhaenge` mit einer Datei von höchstens {grenze} Bytes wiederholen.");
    }
}
