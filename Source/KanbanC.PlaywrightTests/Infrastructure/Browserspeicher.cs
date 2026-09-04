namespace KanbanC.PlaywrightTests.Infrastructure;

// Der Browserzustand, an dem die Identitätswahl hängt: ihr Schlüssel im sessionStorage und die
// Sperre, mit der ein Browser mit abgeschalteten Website-Daten jeden Zugriff darauf abweist.
// Gesperrt werden nur die Schlüssel der Anwendung, damit die Prüfung die Anwendung trifft und
// nicht das Gerüst von Blazor.
public static class Browserspeicher
{
    public const string Identitaetsschluessel = "kanbanc.identitaet";

    public const string Probeschluessel = "kanbanc.probe";

    public const string Sperre = """
        const echterLeser = Storage.prototype.getItem;
        Storage.prototype.getItem = function (schluessel) {
          if (schluessel.startsWith('kanbanc.')) {
            throw new DOMException('Der Browser-Speicher ist gesperrt.', 'SecurityError');
          }
          return echterLeser.call(this, schluessel);
        };
        const echterSchreiber = Storage.prototype.setItem;
        Storage.prototype.setItem = function (schluessel, wert) {
          if (schluessel.startsWith('kanbanc.')) {
            throw new DOMException('Der Browser-Speicher ist gesperrt.', 'SecurityError');
          }
          echterSchreiber.call(this, schluessel, wert);
        };
        """;
}
