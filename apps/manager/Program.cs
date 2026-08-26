using System.Net.Http.Json;
using System.Globalization;
using Spectre.Console;

const string DefaultApiUrl = "http://127.0.0.1:5000";

if (args.Length == 0 || !string.Equals(args[0], "bootstrap-owner", StringComparison.OrdinalIgnoreCase))
{
    AnsiConsole.MarkupLine("[bold]NasForWindows Manager[/]");
    AnsiConsole.MarkupLine("Generate the first Owner token on the API host:");
    AnsiConsole.MarkupLine("  [blue]NasForWindows.Manager bootstrap-owner [--api-url URL][/]");
    return 0;
}

var apiUrl = ReadOption(args, "--api-url") ?? DefaultApiUrl;
if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseAddress)
    || (!string.Equals(baseAddress.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(baseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
{
    AnsiConsole.MarkupLine("[red]The API URL is invalid.[/]");
    return 1;
}

try
{
    using var client = new HttpClient { BaseAddress = baseAddress };
    using var response = await client.PostAsync("/api/bootstrap/token", content: null);
    if (!response.IsSuccessStatusCode)
    {
        AnsiConsole.MarkupLine(
            CultureInfo.InvariantCulture,
            "[red]The API rejected token generation ({0}). Bootstrap may already be complete.[/]",
            (int)response.StatusCode);
        return 1;
    }

    var result = await response.Content.ReadFromJsonAsync<BootstrapTokenResponse>();
    if (result?.Token is null || result.ExpiresAtUtc is null)
    {
        AnsiConsole.MarkupLine("[red]The API returned an invalid bootstrap response.[/]");
        return 1;
    }

    AnsiConsole.MarkupLine("[green]One-time Owner bootstrap token[/]");
    AnsiConsole.WriteLine(result.Token);
    AnsiConsole.MarkupLine(
        CultureInfo.InvariantCulture,
        "Expires at [yellow]{0}[/]. Enter it on the local Owner setup page. Do not share it.",
        Markup.Escape(result.ExpiresAtUtc.Value.ToLocalTime().ToString(
            "yyyy-MM-dd HH:mm:ss zzz",
            CultureInfo.InvariantCulture)));
    return 0;
}
catch (HttpRequestException exception)
{
    AnsiConsole.MarkupLine(
        CultureInfo.InvariantCulture,
        "[red]Unable to contact the local API:[/] {0}",
        Markup.Escape(exception.Message));
    return 1;
}

static string? ReadOption(string[] arguments, string optionName)
{
    for (var index = 0; index < arguments.Length - 1; index++)
    {
        if (string.Equals(arguments[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[index + 1];
        }
    }

    return null;
}

internal sealed record BootstrapTokenResponse(string? Token, DateTimeOffset? ExpiresAtUtc);
