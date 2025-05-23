namespace fetcher_tester.endpoints;

/// <summary>
/// General HTTP/S endpoints.
/// </summary>
public class GeneralEndpoints
{
    public static AutoResetEvent StatusCallbackReceivedEvent = new(false);

    public const string OkLabel = "ok";
    public const string NotFoundLabel = "not-found";
    public const string BadRequestLabel = "bad-request";
    public const string ServerErrorLabel = "server-error";
    public const string DelayedLabel = "delayed";
    public const string CustomResponseLabel = "custom-response";
    public const string InnerStatusCallbackLabel = "inner-status-callback";
    public const string StatusCallbackLabel = "status-callback";

    private readonly string? TestResponse;
    private readonly int TestDelayMS;

    public GeneralEndpoints()
    {
        TestDelayMS = AppConfig.GetConfigValue("test_delay_ms", 4000);
        TestResponse = AppConfig.GetConfigValue("test_response", @"<response>OK</response>");
    }

    public Dictionary<string, RequestDelegate> GetEndpoints()
    {
        return new() {
            { GenerateTestPath(OkLabel), OkEndpoint },
            { GenerateTestPath(NotFoundLabel), NotFoundEndpoint },
            { GenerateTestPath(BadRequestLabel), BadRequestEndpoint },
            { GenerateTestPath(ServerErrorLabel), ServerErrorEndpoint },
            { GenerateTestPath(DelayedLabel), DelayedEndpoint },
            { GenerateTestPath(CustomResponseLabel), CustomEndpoint },
            { GenerateTestPath(InnerStatusCallbackLabel), StatusCallbackEndpoint },
            { GenerateTestPath(StatusCallbackLabel), StatusCallbackEndpoint }
        };
    }

    private static string GenerateTestPath(string label)
    {
        return label;
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task OkEndpoint(HttpContext context)
    {
        await Task.CompletedTask;

        context.RequestContextLog();
        context.Response.StatusCode = 200;

        return;
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task BadRequestEndpoint(HttpContext context)
    {
        await Task.CompletedTask;

        context.RequestContextLog();
        context.Response.StatusCode = 400;

        return;
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task NotFoundEndpoint(HttpContext context)
    {
        await Task.CompletedTask;

        context.RequestContextLog();
        context.Response.StatusCode = 404;

        return;
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task ServerErrorEndpoint(HttpContext context)
    {
        await Task.CompletedTask;

        context.RequestContextLog();
        context.Response.StatusCode = 500;

        return;
    }

    private async Task CustomEndpoint(HttpContext context)
    {
        context.RequestContextLog();

        context.Response.StatusCode = 200;
        await context.Response.WriteAsJsonAsync(TestResponse);

        return;
    }

    /// <summary>
    /// A delayed HTTP endpoint.
    /// Will wait a configurable amount of time before returning 200/OK.
    /// </summary>
    private async Task DelayedEndpoint(HttpContext context)
    {
        context.RequestContextLog();

        await Task.Delay(TimeSpan.FromMilliseconds(TestDelayMS));
        context.Response.StatusCode = 200;

        return;
    }

    // Status callback endpoint hit for all tests
    private async Task StatusCallbackEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        StatusCallbackReceivedEvent.Set();

        context.Response.StatusCode = 200;
        Console.WriteLine("Status callback received successfully!");

        var statusCallbackJSON = await context.Request.Body.ReadAsStringAsync();
        Console.WriteLine("Status callback content: " + statusCallbackJSON);
    }
}
