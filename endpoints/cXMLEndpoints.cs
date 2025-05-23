namespace fetcher_tester.endpoints;

/// <summary>
/// General cXML endpoints.
/// </summary>
public class cXMLEndpoints
{
    public static AutoResetEvent StatusCallbackReceivedEvent = new(false);

    public const string OkLabel = "ok";
    public const string DelayedLabel = "delayed";
    public const string CustomResponseLabel = "custom-response";
    public const string InnerStatusCallbackLabel = "inner-status-callback";
    public const string StatusCallbackLabel = "status-callback";

    public const string LabelPrefix = "cxml";

    private readonly string? TestResponse;
    private readonly int TestDelayMS;

    public cXMLEndpoints()
    {
        TestDelayMS = AppConfig.GetConfigValue("test_delay_ms", 4000);
        TestResponse = AppConfig.GetConfigValue("test_response", @"<response>OK</response>");
    }

    public Dictionary<string, RequestDelegate> GetEndpoints()
    {
        return new() {
            { GenerateTestPath(OkLabel), OkEndpoint },
            { GenerateTestPath(DelayedLabel), DelayedEndpoint },
            { GenerateTestPath(CustomResponseLabel), CustomEndpoint },
            { GenerateTestPath(InnerStatusCallbackLabel), StatusCallbackEndpoint },
            { GenerateTestPath(StatusCallbackLabel), StatusCallbackEndpoint }
        };
    }

    private static string GenerateTestPath(string label)
    {
        return $"{LabelPrefix}/{label}";
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    private async Task OkEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKcXMLResponse();

        return;
    }



    private async Task CustomEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKcXMLResponse(TestResponse);

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
        await context.CreateOKcXMLResponse();

        return;
    }

    // Status callback endpoint hit for all tests
    private async Task StatusCallbackEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        StatusCallbackReceivedEvent.Set();

        await context.CreateOKcXMLResponse();
        Console.WriteLine("Status callback received successfully!");

        var statusCallbackJSON = await context.Request.Body.ReadAsStringAsync();
        Console.WriteLine("Status callback content: " + statusCallbackJSON);
    }
}
