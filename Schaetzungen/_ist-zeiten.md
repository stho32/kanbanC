# Ist-Zeiten

Gemessene Dauer abgeschlossener Bubbles. Grundlage für künftige Zählungen
(`/schaetzung`, Skill `work-breakdown-structure`) statt geliehener Konventionswerte.

`brutto` = Wanduhr der Bubble (`date` Start bis Abschluss). Gilt als Arbeitszeit.
`bestaetigt` = aus dem Commit-Fenster des Kalendertags abgeleitet (s.u.), automatisch.
Nie vom User erfragen.
Zeilen mit `brutto` `—` sind aus der Commit-Historie nachgetragen: `bestaetigt` gerechnet, keine Wanduhr.

| Datum | Aufgabe | Bubble | Typ | geschaetzt | brutto | bestaetigt | Modus |
|---|---|---|---|---|---|---|---|
| 2026-08-29 | R00001 Board anlegen | B0001 Standardspalten erzeugen | Standard | 2h | 0,3h | 0,4h | moderat |
| 2026-08-29 | R00001 Board anlegen | B0015 Datenbankverbindung öffnen | Standard | 2h | 0,0h | 0,1h | moderat |
| 2026-08-29 | R00001 Board anlegen | B0002 Schema anlegen | Standard | 2h | 0,1h | 0,1h | moderat |
| 2026-08-29 | R00001 Board anlegen | B0003 Board mit Spalten speichern | Standard | 2h | 1,5h | 1,5h | moderat |
| 2026-08-29 | R00001 Board anlegen | B0004 Boards laden | Standard | 2h | 0,1h | 0,1h | moderat |
| 2026-08-29 | R00001 Board anlegen | B0005 Board-Anlage verdrahten | Standard | 2h | 0,1h | 0,1h | moderat |
| 2026-08-29 | R00002 Spalten gestalten | B0027 Spalten-Anfrage prüfen | Standard | 0,4h | 0,1h | 0,1h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0028 Spalte speichern und ändern | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0029 Spalten-Anlage verdrahten | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0030 Spalten-Endpunkte anlegen und ändern | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0031 API-Klient der Spalten | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0032 Spaltenpflege in der Oberfläche | Standard | 2h | 0,1h | 0,1h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0033 E2E Spalte anlegen und ändern | Standard | 2-4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0034 Reihenfolge prüfen | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0035 Reihenfolge speichern | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0036 Reihenfolge über die API | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0037 Umsortieren in der Oberfläche | Standard | 2h | 0,1h | 0,1h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0038 Spalte löschen und verdichten | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0039 Entfernen über die API | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0040 Entfernen in der Oberfläche | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0041 Ausfall der WebApi in der Spaltenpflege | Standard | 0,4h | 0,3h | 0,3h | autonom |
| 2026-08-29 | R00002 Spalten gestalten | B0042 Reihenfolge in derselben Transaktion pruefen | Standard | 0,4h | 0,3h | 0,3h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0016 Sortierung in der Abfrage | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0017 Sortierung erreicht die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0018 E2E Liste alphabetisch | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0019 Board-Seite mit Route | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0020 Spaltenbahnen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0021 Verweis aus der Liste | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0022 Detail-Panel abbauen, Seitenobjekte umziehen | unklar | 0,4-1,5h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0023 E2E Board öffnen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0024 Unbekannte Board-Nummer | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0025 WebApi nicht erreichbar | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | B0026 E2E Fehlerpfade | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0043 Bahnen als eigene Komponente | Standard | 2h | — | 0,0h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0044 Layout-Modus schalten | Standard | 2h | — | 0,0h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0045 Bahnen bearbeitbar machen | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0046 E2E Layout-Modus | unklar | 2-4h | — | 0,0h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0047 Bezeichnung normalisieren | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0048 Namenskonflikt pruefen | Standard | 0,4h | — | 0,1h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0049 Eindeutigen Index anlegen | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0050 Konflikt ueber die API | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | B0051 Konflikt in der Oberflaeche | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0066 Zieldesign zentral vermerkt | Standard | 0,4h | — | 0,2h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0052 Token-Sheet einziehen | Standard | 0,4h | — | 0,2h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0053 Schriften mitliefern | unklar | 2-4h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0054 Bootstrap ausbauen | unklar | 2-4h | — | 0,1h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0055 E2E Fundament | Standard | 2h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0056 Kopfzeile | Standard | 2h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0057 Seitenleiste abbauen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0058 E2E Rahmen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0059 Boards nach Art aufteilen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0060 Baender und Kacheln | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0061 Anlegen als Patch | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0062 E2E Uebersicht und Anlegen | Standard | 2h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0063 Bahnenform | Standard | 2h | — | 0,1h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0064 Waagerechtes Scrollen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-30 | R00005 Oberfläche auf das gezeichnete Design bringen | B0065 E2E Bahnen | Standard | 2h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0067 Kartentabelle anlegen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0068 Karten einer Spalte lesen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0069 Karten reisen mit dem Board | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0070 Karten über die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0080 Kartenanfrage prüfen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0074 Karte speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0075 Kartenanlage verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0076 Karten-Endpunkte | Standard | 2h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0081 Zurückweisung über die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0083 Spalte mit Karten zurueckweisen | Standard | 0,4h | — | 0,1h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0077 API-Klient der Karten | Standard | 2h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0071 Kartenform in der Bahn | Standard | 2h | — | 0,1h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0072 Leere Bahn | Standard | 0,4h | — | 0,1h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0078 Anlegen im Bahnenfuß | unklar | 2-4h | — | 0,1h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0073 E2E Karten am Board | unklar | 2-4h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0079 E2E Karte anlegen | Standard | 2h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0082 Zurückweisung in der Oberfläche | Standard | 2h | — | 0,0h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | B0084 Zurueckweisung im Layout-Modus | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0096 Fehlerbefund neben der Meldung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0097 Die 16 Befunde bekommen Code und Kompensation | unklar | 2-4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0100 Oberfläche liest die Meldung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0101 Alte Form abbauen | unklar | 2-4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0098 Antwort auf „gibt es nicht“ | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0099 Die sechs leeren 404 füllen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0102 Vertragstest über alle Fehlerantworten | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0085 Zug in der Datenbank ausführen | Standard | 2h | — | 0,1h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0086 Zug verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0087 Lage-Endpunkt | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0088 API-Klient des Zugs | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0089 Karte wird ziehbar | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0090 Ablagestellen in der Bahn | unklar | 2-4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0091 Ablegen löst den Zug aus | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0092 E2E Karte verschieben | unklar | 2-4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0093 Ziellage prüfen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0094 Zurückweisung über die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-03 | R00007 Karte verschieben | B0095 Zurückweisung und Ausfall in der Oberfläche | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | B0103 Ziellage aus Karte und Hälfte | Standard | 0,4h | — | 0,1h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | B0104 Karte nimmt an | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | B0105 Einfügelinie zeigen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | B0106 Restfläche und leere Bahn nehmen an | Standard | 2h | — | 0,0h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | B0107 E2E Einfügelinie und Ablagefläche | unklar | 2-4h | — | 0,1h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0108 Einstellung des Boards ablegen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0109 Board trägt seine Kartenzahlanzeige | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0110 Anzeige umschalten speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0111 Umschalten verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0112 Endpunkt der Kartenzahlanzeige | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0113 API-Klient der Kartenzahlanzeige | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0114 Schalter in Zone 3 | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0115 E2E Kartenzahl schalten | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0116 Zahl in der Kopfzeile | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | B0117 E2E Zahl folgt Änderungen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0118 Namen prüfen | Standard | 0,4h | — | 1,7h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0119 Namen speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0120 Umbenennen verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0121 Endpunkt zum Umbenennen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0122 API-Klient des Umbenennens | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0123 ⋯-Menü der Kachel | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0124 Umbenennen in der Kachel | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0125 E2E Board umbenennen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0126 Archivstand ablegen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0127 Board trägt seinen Archivstand | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0128 Liste nach Archivstand | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0129 Archivieren und Zurückholen speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0130 Archivierung verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0131 Endpunkt der Archivierung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0132 Archivierte über die API abrufen | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0133 API-Klient der Archivierung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0134 Archivieren im ⋯-Menü | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0135 Filter aktive und archivierte | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0136 Zurückholen in der archivierten Ansicht | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | B0137 E2E archivieren und zurückholen | unklar | 2-4h | — | 0,1h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0138 Kontributoren-Tabelle anlegen | Standard | 0,4h | — | 0,1h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0139 Kontributor speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0140 Kontributoren laden | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0141 Kontributor-Anlage verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0142 Kontributoren-Endpunkte | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0143 API-Klient der Kontributoren | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0144 Kontributoren-Seite | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0145 Anlegezeile am Ende der Liste | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0146 E2E Kontributor anlegen | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0147 Kontributor-Anfrage prüfen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0148 Zurückweisung über die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | B0149 Zurückweisung in der Oberfläche | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0150 Kontributor speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0157 Änderungsanfrage prüfen | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0158 Antwort auf „diesen Kontributor gibt es nicht“ | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0151 Änderung verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0152 Endpunkt zum Ändern | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0159 Zurückweisung über die API | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0153 API-Klient der Änderung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0154 Pflege-Spalte mit Stiftsymbol | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0155 Bearbeiten in der aufgeklappten Zeile | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0160 Zurückweisung in der Bearbeitungszeile | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | B0156 E2E Kontributor ändern | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0161 Probe: Browser-Speicher aus Blazor | unklar | 2-4h | — | 0,2h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0162 Identitätsspeicher | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0163 Identitätsplatz wird Bedienelement | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0164 Popover mit den wählbaren Menschen | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0165 Wahl überlebt den Reload | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0166 Ausfall der WebApi im Identitätsplatz | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0167 E2E Identität wählen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0168 Gesperrter Teil der Liste | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00013 Identität wählen | B0169 E2E gesperrte Einträge | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0170 Stilllegungsstand ablegen | Standard | 0,4h | — | 0,1h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0171 Kontributor trägt seinen Stilllegungsstand | unklar | 0,4-1,5h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0172 Stillgelegte ans Ende der Liste | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0173 Stilllegen und Zurückholen speichern | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0174 Stilllegung verdrahten | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0175 Endpunkt der Stilllegung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0176 API-Klient der Stilllegung | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0177 Pausensymbol in der Pflege-Zelle | Standard | 2h | — | 0,1h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0178 Gruppenzeile und stillgelegte Zeile | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0179 Zählzeile im Seitenkopf | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0180 E2E stilllegen und zurückholen | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0181 Identitätsliste bezieht den Stilllegungsstand ein | Standard | 0,4h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0182 Gemerkte Identität eines Stillgelegten | Standard | 2h | — | 0,0h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | B0183 E2E aus der Auswahl verschwunden | Standard | 2h | — | 0,0h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0184 Erledigungstabelle anlegen | Standard | 0,4h | 0,0h | 0,1h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0185 Erledigungsregel | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0186 Erledigung schreiben und löschen | Standard | 2h | 0,0h | 0,0h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0187 Erledigungsdatum reist mit der Karte | Standard | 0,4-1,5h | 0,0h | 0,0h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0188 Erledigung über die API | Standard | 0,4h | 0,0h | 0,0h | autonom |
| 2026-08-29 | R00003 Boards auflisten und öffnen | Lauf R00003 · S7 ausstehend | Nacharbeit | — | — | 0,1h | autonom |
| 2026-08-30 | R00004 Layout-Modus für die Spaltenpflege | Lauf R00004 · S7 ausstehend | Nacharbeit | — | — | 0,4h | autonom |
| 2026-09-02 | R00005 Oberfläche auf das gezeichnete Design bringen | Lauf R00005 · S7 ausstehend | Nacharbeit | — | — | 4,6h | autonom |
| 2026-08-31 | R00006 Karten anlegen und am Board sehen | Lauf R00006 · S7 ausstehend | Nacharbeit | — | — | 0,9h | autonom |
| 2026-09-03 | R00007 Karte verschieben | Lauf R00007 · S7 ausstehend | Nacharbeit | — | — | 1,2h | autonom |
| 2026-09-03 | R00008 Einfügelinie statt Ablagekästen | Lauf R00008 · S7 ausstehend | Nacharbeit | — | — | 1,6h | autonom |
| 2026-09-04 | R00009 Kartenzahl je Spalte anzeigen | Lauf R00009 · S7 ausstehend | Nacharbeit | — | — | 0,2h | autonom |
| 2026-09-04 | R00010 Board umbenennen und archivieren | Lauf R00010 · S7 ausstehend | Nacharbeit | — | — | 0,3h | autonom |
| 2026-09-04 | R00011 Kontributor anlegen | Lauf R00011 · S7 ausstehend | Nacharbeit | — | — | 0,2h | autonom |
| 2026-09-04 | R00012 Kontributor bearbeiten | Lauf R00012 · S7 ausstehend | Nacharbeit | — | — | 0,4h | autonom |
| 2026-09-04 | R00013 Identität wählen | Lauf R00013 · S7 ausstehend | Nacharbeit | — | — | 0,6h | autonom |
| 2026-09-04 | R00014 Kontributor stilllegen | Lauf R00014 · S7 ausstehend | Nacharbeit | — | — | 0,2h | autonom |
| 2026-09-05 | R00015 Erledigte Karten gebündelt sehen | B0189 Erledigungsordnung und Kürzung | Standard | 0,4h | 0,0h | 0,0h | autonom |
