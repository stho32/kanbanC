using System;
using System.Collections.Generic;
using System.Linq;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Vergleicht Soll- und Istzustand anhand eines Schlüssels und kategorisiert die Datensätze.
    /// Dies ist eine Operation nach IOSP - reine Logik ohne Abhängigkeiten.
    /// Soll- und Ist-Datenquellen verwenden das gleiche DTO - die Datenquellen sind für die Gleichheit verantwortlich.
    /// </summary>
    /// <typeparam name="T">Typ der Datensätze (für Soll und Ist identisch)</typeparam>
    /// <typeparam name="TSchluessel">Typ des Vergleichschlüssels</typeparam>
    public class SollIstVergleicher<T, TSchluessel>
    {
        private readonly Func<T, TSchluessel> _schluesselSelektor;
        private readonly Func<T, T, bool> _sindGleich;

        /// <summary>
        /// Erstellt einen neuen Soll-Ist-Vergleicher.
        /// </summary>
        /// <param name="schluesselSelektor">Funktion zur Extraktion des Schlüssels aus Datensätzen</param>
        /// <param name="sindGleich">Funktion zum Vergleich ob Soll und Ist inhaltlich gleich sind</param>
        public SollIstVergleicher(
            Func<T, TSchluessel> schluesselSelektor,
            Func<T, T, bool> sindGleich)
        {
            _schluesselSelektor = schluesselSelektor ?? throw new ArgumentNullException(nameof(schluesselSelektor));
            _sindGleich = sindGleich ?? throw new ArgumentNullException(nameof(sindGleich));
        }

        /// <summary>
        /// Vergleicht Soll- und Istzustand und kategorisiert alle Datensätze.
        /// </summary>
        /// <param name="sollzustand">Die gewünschten Datensätze (Soll)</param>
        /// <param name="istzustand">Die aktuellen Datensätze (Ist)</param>
        /// <returns>Kategorisiertes Vergleichsergebnis</returns>
        public SollIstVergleichErgebnis<T> Vergleiche(
            IEnumerable<T> sollzustand,
            IEnumerable<T> istzustand)
        {
            var sollListe = sollzustand?.ToList() ?? new List<T>();
            var istListe = istzustand?.ToList() ?? new List<T>();

            var istNachSchluessel = istListe.ToDictionary(_schluesselSelektor);

            var zuErstellen = new List<T>();
            var zuAktualisieren = new List<(T Soll, T Ist)>();
            var unveraendert = new List<(T Soll, T Ist)>();

            foreach (var soll in sollListe)
            {
                var schluessel = _schluesselSelektor(soll);

                if (!istNachSchluessel.TryGetValue(schluessel, out var ist))
                {
                    zuErstellen.Add(soll);
                }
                else if (_sindGleich(soll, ist))
                {
                    unveraendert.Add((soll, ist));
                }
                else
                {
                    zuAktualisieren.Add((soll, ist));
                }
            }

            var sollSchluessel = new HashSet<TSchluessel>(
                sollListe.Select(_schluesselSelektor));

            var zuLoeschen = istListe
                .Where(ist => !sollSchluessel.Contains(_schluesselSelektor(ist)))
                .ToList();

            return new SollIstVergleichErgebnis<T>(
                zuErstellen,
                zuAktualisieren,
                zuLoeschen,
                unveraendert);
        }
    }
}
