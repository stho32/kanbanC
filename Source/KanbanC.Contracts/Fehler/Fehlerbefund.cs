namespace KanbanC.Contracts.Fehler;

// Ein Grund, warum ein Aufruf nicht durchging — in der Form, die ein Agent allein benutzen kann:
// Code stabil und maschinenlesbar, Meldung mit den konkreten Werten des Vorgangs, Kompensation
// als ausführbarer nächster Schritt mit Route.
public record Fehlerbefund(string Code, string Meldung, string Kompensation);
