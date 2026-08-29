using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class TerminformatiererTests
{
    [Test]
    public void Wenn_ein_Termin_gesetzt_ist_dann_erscheint_er_im_ISO_Format()
    {
        var text = Terminformatierer.AlsText(new DateOnly(2026, 9, 1));

        Assert.That(text, Is.EqualTo("2026-09-01"));
    }

    [Test]
    public void Wenn_kein_Termin_gesetzt_ist_dann_erscheint_ein_Gedankenstrich()
    {
        var text = Terminformatierer.AlsText(null);

        Assert.That(text, Is.EqualTo("—"));
    }
}
