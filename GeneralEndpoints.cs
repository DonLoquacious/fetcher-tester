namespace fetcher_tester;

/// <summary>
/// General HTTP/S endpoints.
/// </summary>
public class GeneralEndpoints
{
    public static AutoResetEvent StatusCallbackReceivedEvent = new(false);

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
            { "basic", BasicEndpoint },
            { "custom", CustomEndpoint },
            { "delayed", DelayedEndpoint },
            { "inner_status_callback", StatusCallbackEndpoint },
            { "status_callback", StatusCallbackEndpoint }
        };
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task BasicEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        return;
    }

    private async Task CustomEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse(TestResponse);

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
        await context.CreateOKResponse();

        return;
    }

    // Status callback endpoint hit for all tests
    private async Task StatusCallbackEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        StatusCallbackReceivedEvent.Set();

        await context.CreateOKResponse();
        Console.WriteLine("Status callback received successfully!");

        var statusCallbackJSON = await context.Request.Body.ReadAsStringAsync();
        Console.WriteLine("Status callback content: " + statusCallbackJSON);
    }
}
