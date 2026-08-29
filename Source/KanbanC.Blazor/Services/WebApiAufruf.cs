namespace KanbanC.Blazor.Services;

public static class WebApiAufruf
{
    private const string NichtErreichbar = "Die WebApi ist nicht erreichbar. Bitte später erneut versuchen.";

    public static async Task<string?> MitAusfallmeldung(Func<Task> aufruf)
    {
        try
        {
            await aufruf();
            return null;
        }
        catch (HttpRequestException)
        {
            return NichtErreichbar;
        }
    }
}
