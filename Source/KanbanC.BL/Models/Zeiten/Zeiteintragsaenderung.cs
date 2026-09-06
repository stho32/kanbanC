using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Models.Zeiten;

// Was aus einer Änderung wurde — festgestellt unter **demselben** Schreibschloss, unter dem
// geschrieben wird. Die Auskunft muss von dort kommen: läge sie in einem eigenen Lesezugriff
// davor, bliebe zwischen „für dieses Paar läuft kein anderer" und dem UPDATE ein Fenster, in dem
// der partielle Index UX_Zeiteintrag_Karte_Kontributor_Laufend statt eines Befunds zuschlägt.
// Bei `WurdeGeaendert` steht in `Eintrag` der geänderte Eintrag; sonst steht dort der **andere**,
// der für dasselbe Paar schon läuft — der Dienst macht daraus den lesbaren Befund mit seiner
// Nummer.
// Dieselbe Gestalt wie Zeitmessungsstart: der Eintrag plus die eine Auskunft, die ihm nicht
// anzusehen ist.
public sealed record Zeiteintragsaenderung(Zeiteintrag Eintrag, bool WurdeGeaendert);
