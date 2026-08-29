using System.Collections.Generic;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Ergebnis eines Soll-Ist-Vergleichs mit kategorisierten Datensätzen.
    /// Soll- und Ist-Datenquellen verwenden das gleiche DTO.
    /// </summary>
    /// <typeparam name="T">Typ der Datensätze (für Soll und Ist identisch)</typeparam>
    public class SollIstVergleichErgebnis<T>
    {
        /// <summary>
        /// Datensätze die im Sollzustand existieren, aber nicht im Istzustand.
        /// </summary>
        public IReadOnlyList<T> ZuErstellen { get; }

        /// <summary>
        /// Datensätze die in beiden Zuständen existieren, aber unterschiedliche Werte haben.
        /// Tuple: (Sollzustand, Istzustand)
        /// </summary>
        public IReadOnlyList<(T Soll, T Ist)> ZuAktualisieren { get; }

        /// <summary>
        /// Datensätze die im Istzustand existieren, aber nicht im Sollzustand.
        /// </summary>
        public IReadOnlyList<T> ZuLoeschen { get; }

        /// <summary>
        /// Datensätze die in beiden Zuständen identisch sind.
        /// </summary>
        public IReadOnlyList<(T Soll, T Ist)> Unveraendert { get; }

        public SollIstVergleichErgebnis(
            IReadOnlyList<T> zuErstellen,
            IReadOnlyList<(T Soll, T Ist)> zuAktualisieren,
            IReadOnlyList<T> zuLoeschen,
            IReadOnlyList<(T Soll, T Ist)> unveraendert)
        {
            ZuErstellen = zuErstellen;
            ZuAktualisieren = zuAktualisieren;
            ZuLoeschen = zuLoeschen;
            Unveraendert = unveraendert;
        }

        /// <summary>
        /// True wenn es mindestens eine Änderung gibt (Erstellen, Aktualisieren oder Löschen).
        /// </summary>
        public bool HatAenderungen => ZuErstellen.Count > 0
                                    || ZuAktualisieren.Count > 0
                                    || ZuLoeschen.Count > 0;

        /// <summary>
        /// Gesamtzahl aller Änderungen.
        /// </summary>
        public int AnzahlAenderungen => ZuErstellen.Count + ZuAktualisieren.Count + ZuLoeschen.Count;
    }
}
