namespace KanbanC.Blazor.Services;

// Das gerechnete Bild der Fieberkurve: die drei Zonenflächen als `points`-Attribute, die Lage des
// Punktes „heute" auf der Zeichenfläche und die Zone, in der er liegt.
// `DerVerbrauchLiegtUeberDemRand` sagt, dass der Punkt am oberen Rand sitzt, weil mehr verbraucht
// wurde, als die Kette an Puffer hat — der Zahlenwert daneben nennt, wie viel.
public record Fieberkurvenlage(
    string Gruenflaeche,
    string Gelbflaeche,
    string Rotflaeche,
    double PunktX,
    double PunktY,
    Verbrauchszone Zone,
    bool DerVerbrauchLiegtUeberDemRand); // stil-check: C09 Komposition benannter Werte
