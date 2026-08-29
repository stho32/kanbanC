using System.Text.RegularExpressions;

namespace {{NAMESPACE}}.PlaywrightTests.Helpers;

public static class AspNetErrorParser
{
    public static AspNetErrorInfo Parse(string htmlContent)
    {
        var result = new AspNetErrorInfo();

        // Blazor Server-Side Errors
        if (htmlContent.Contains("blazor-error-boundary") ||
            htmlContent.Contains("An error has occurred"))
        {
            result.HasError = true;
            result.ErrorType = "BlazorError";
            result.Message = ExtractBlazorError(htmlContent);
            return result;
        }

        // ASP.NET Developer Exception Page
        if (htmlContent.Contains("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"))
        {
            result.HasError = true;
            result.ErrorType = ExtractExceptionType(htmlContent);
            result.Message = ExtractExceptionMessage(htmlContent);
            result.StackTrace = ExtractStackTrace(htmlContent);
            return result;
        }

        // Generic ASP.NET Error
        if (htmlContent.Contains("An unhandled exception occurred") ||
            htmlContent.Contains("Server Error in") ||
            htmlContent.Contains("Runtime Error"))
        {
            result.HasError = true;
            result.ErrorType = "AspNetError";
            result.Message = ExtractGenericError(htmlContent);
            return result;
        }

        // HTTP Error Codes
        if (htmlContent.Contains("<title>404") ||
            htmlContent.Contains("<title>500") ||
            htmlContent.Contains("<title>403"))
        {
            result.HasError = true;
            result.ErrorType = "HttpError";
            result.Message = ExtractHttpError(htmlContent);
            return result;
        }

        return result;
    }

    private static string ExtractExceptionType(string html)
    {
        // Suche nach Exception-Typ im Developer Exception Page Format
        var match = Regex.Match(html, @"<span class=""exception-type"">([^<]+)</span>");
        return match.Success ? match.Groups[1].Value.Trim() : "UnknownException";
    }

    private static string ExtractExceptionMessage(string html)
    {
        var match = Regex.Match(html, @"<span class=""exception-message"">([^<]+)</span>");
        return match.Success ? match.Groups[1].Value.Trim() : "Keine Fehlermeldung extrahierbar";
    }

    private static string ExtractStackTrace(string html)
    {
        var match = Regex.Match(html, @"<pre class=""rawExceptionStackTrace""[^>]*>([^<]+)</pre>",
            RegexOptions.Singleline);
        if (match.Success)
        {
            var trace = match.Groups[1].Value;
            // Nur erste 5 Zeilen für KI-Ausgabe
            var lines = trace.Split('\n').Take(5);
            return string.Join("\n", lines);
        }
        return "";
    }

    private static string ExtractBlazorError(string html)
    {
        var match = Regex.Match(html, @"<div[^>]*blazor-error[^>]*>([^<]+)</div>");
        return match.Success ? match.Groups[1].Value.Trim() : "Blazor-Fehler ohne Details";
    }

    private static string ExtractGenericError(string html)
    {
        // Title-Tag als Fallback
        var match = Regex.Match(html, @"<title>([^<]+)</title>");
        return match.Success ? match.Groups[1].Value.Trim() : "Server-Fehler ohne Details";
    }

    private static string ExtractHttpError(string html)
    {
        var match = Regex.Match(html, @"<title>(\d{3}[^<]*)</title>");
        return match.Success ? $"HTTP {match.Groups[1].Value.Trim()}" : "HTTP-Fehler";
    }
}

public class AspNetErrorInfo
{
    public bool HasError { get; set; }
    public string ErrorType { get; set; } = "";
    public string Message { get; set; } = "";
    public string StackTrace { get; set; } = "";

    public override string ToString()
    {
        var result = $"{ErrorType}: {Message}";
        if (!string.IsNullOrEmpty(StackTrace))
            result += $"\nStackTrace (gekürzt):\n{StackTrace}";
        return result;
    }
}
