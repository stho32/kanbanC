namespace KanbanC.Contracts.Karten;

// Die **eine** Zahl, auf die sich Validator, Route, SignalR-Kreislauf und Dateiwähler beziehen.
// Sie liegt in den Contracts und nicht in der Fachlogik, weil sie an vier Stellen in **beiden**
// Prozessen gilt: die Oberfläche hat bewusst keine Projektreferenz auf KanbanC.BL, und eine
// zweite Zahl dort wäre genau die zweite Wahrheit, die das Kriterium ausschließt.
// 10 MB lässt jede Anforderungs-, Planungs- und Architekturdatei und jedes Auswertungsbild zu und
// hält Videos draußen.
public static class Anhangsgrenze
{
    public const long HoechsteDateigroesse = 10L * 1024 * 1024;
}
