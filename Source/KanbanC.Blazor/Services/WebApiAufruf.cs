namespace KanbanC.Blazor.Services;

public static class WebApiAufruf
{
    public static async Task<string?> MitAusfallmeldung(Func<Task> aufruf)
    {
        try
        {
            await aufruf();
            return null;
        }
        catch (HttpRequestException)
        {
            return WebApiAusfall.Meldung;
        }
    }
}
