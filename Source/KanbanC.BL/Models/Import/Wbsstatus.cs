namespace KanbanC.BL.Models.Import;

// Der Ampelstand eines Knotens. Sieben Werte, wie der Skill work-breakdown-structure sie führt —
// und nicht fünf: bestehend, option und ausbau kommen in echten Dateien vor, und ein Leser, der
// sie nicht kennt, überspränge Zeilen, die in Ordnung sind.
// Bestehend zählt überall wie gruen: der Skill rechnet „alle zählenden Kinder gruen (oder
// bestehend), also gruen“. Option, ausbau und verworfen zählen nicht zum Umfang und werden übersprungen.
public enum Wbsstatus
{
    Rot,
    Gelb,
    Gruen,
    Bestehend,
    Option,
    Ausbau,
    Verworfen,
}
