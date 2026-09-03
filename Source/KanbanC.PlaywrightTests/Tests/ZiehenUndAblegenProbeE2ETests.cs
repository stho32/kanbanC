using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe nach dem Skill dependency-probe: R00007 setzt darauf, dass Playwright die nativen
// HTML5-Ziehereignisse auslöst und dass eine Ablagestelle getroffen werden kann, die erst
// nach dem Beginn des Zugs entsteht — unter Blazor Server nach einem Rundlauf über SignalR.
// Ohne diese drei Belege hätte I0012 keinen Weg, in der Oberfläche grün zu werden.
[TestFixture]
public class ZiehenUndAblegenProbeE2ETests : PageTest
{
    private const string SeiteMitFesterAblagestelle = """
        <style>body{margin:0}#quelle,#stelle,#leerraum{width:200px;height:60px}</style>
        <div id="quelle" draggable="true">Karte</div>
        <div id="stelle">hier ablegen</div>
        <div id="leerraum">nichts</div>
        <script>
          window.ereignisse = [];
          const quelle = document.getElementById('quelle');
          quelle.addEventListener('dragstart', e => {
            e.dataTransfer.setData('text/plain', 'karte');
            window.ereignisse.push('dragstart');
          });
          quelle.addEventListener('dragend', () => window.ereignisse.push('dragend'));
          const stelle = document.getElementById('stelle');
          stelle.addEventListener('dragover', e => { e.preventDefault(); window.ereignisse.push('dragover'); });
          stelle.addEventListener('drop', e => { e.preventDefault(); window.ereignisse.push('drop'); });
        </script>
        """;

    private const string SeiteMitNachgereichterAblagestelle = """
        <style>body{margin:0}#quelle,#huelle{width:200px;height:60px}</style>
        <div id="quelle" draggable="true">Karte</div>
        <div id="huelle"></div>
        <script>
          window.ereignisse = [];
          const quelle = document.getElementById('quelle');
          quelle.addEventListener('dragstart', e => {
            e.dataTransfer.setData('text/plain', 'karte');
            window.ereignisse.push('dragstart');
            setTimeout(() => {
              const stelle = document.createElement('div');
              stelle.id = 'stelle';
              stelle.textContent = 'hier ablegen';
              stelle.style.height = '60px';
              stelle.addEventListener('dragover', e => { e.preventDefault(); window.ereignisse.push('dragover'); });
              stelle.addEventListener('drop', e => { e.preventDefault(); window.ereignisse.push('drop'); });
              document.getElementById('huelle').appendChild(stelle);
            }, 300);
          });
          quelle.addEventListener('dragend', () => window.ereignisse.push('dragend'));
        </script>
        """;

    [Test]
    public async Task Wenn_DragToAsync_eine_Karte_auf_eine_Ablagestelle_zieht_dann_laufen_die_nativen_Ziehereignisse()
    {
        await Page.SetContentAsync(SeiteMitFesterAblagestelle);

        await Page.Locator("#quelle").DragToAsync(Page.Locator("#stelle"));

        var ereignisse = await Page.EvaluateAsync<string[]>("() => window.ereignisse");
        Assert.Multiple(() =>
        {
            Assert.That(ereignisse, Does.Contain("dragstart"));
            Assert.That(ereignisse, Does.Contain("dragover"));
            Assert.That(ereignisse, Does.Contain("drop"));
        });
    }

    [Test]
    public async Task Wenn_die_Ablagestelle_erst_nach_dem_Beginn_des_Zugs_entsteht_dann_laesst_sie_sich_trotzdem_treffen()
    {
        await Page.SetContentAsync(SeiteMitNachgereichterAblagestelle);
        var stelle = Page.Locator("#stelle");
        await Assertions.Expect(stelle).ToHaveCountAsync(0);

        await NimmAuf(Page.Locator("#quelle"));
        await Assertions.Expect(stelle).ToBeVisibleAsync();
        await LegeAbAuf(stelle);

        var ereignisse = await Page.EvaluateAsync<string[]>("() => window.ereignisse");
        Assert.That(ereignisse, Does.Contain("drop"));
    }

    [Test]
    public async Task Wenn_der_Zug_ausserhalb_jeder_Ablagestelle_endet_dann_kommt_dragend_ohne_drop()
    {
        await Page.SetContentAsync(SeiteMitFesterAblagestelle);

        await Page.Locator("#quelle").DragToAsync(Page.Locator("#leerraum"));

        var ereignisse = await Page.EvaluateAsync<string[]>("() => window.ereignisse");
        Assert.Multiple(() =>
        {
            Assert.That(ereignisse, Does.Contain("dragend"));
            Assert.That(ereignisse, Does.Not.Contain("drop"));
        });
    }

    // Die Mausfolge von Hand statt DragToAsync: der Zug bleibt offen, während die Oberfläche
    // die Ablagestellen nachreicht. Zwei Bewegungen sind nötig, damit der Browser den Zug
    // überhaupt beginnt.
    private async Task NimmAuf(ILocator karte)
    {
        var kasten = await karte.BoundingBoxAsync();
        await Page.Mouse.MoveAsync(kasten!.X + kasten.Width / 2, kasten.Y + kasten.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2 + 10, new MouseMoveOptions { Steps = 5 });
    }

    private async Task LegeAbAuf(ILocator stelle)
    {
        var kasten = await stelle.BoundingBoxAsync();
        await Page.Mouse.MoveAsync(kasten!.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
        await Page.Mouse.UpAsync();
    }
}
