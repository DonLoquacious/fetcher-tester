namespace fetcher_tester;

public static class ExtensionMethods
{
    const string TestNameSuffix = "-test";
    const string TestEndpointSuffix = "-endpoint";

    public static bool ValidateTestResults(this HttpContext context, string testName)
    {
        if (context.Response.StatusCode == 200)
        {
            Console.WriteLine($"Test {testName} has completed successfully.");
            return true;
        }
        else
        {
            Console.WriteLine($"Test {testName} completed, but some errors have occurred.");
            return false;
        }
    }

    public static void RequestContextLog(this HttpContext context)
    {
        Console.WriteLine($"Endpoint: {context.GetEndpoint()}");
        Console.WriteLine($"HTTP Method: {context.Request.Method}");
        Console.WriteLine($"Local: {context.Connection.LocalIpAddress}:{context.Connection.LocalPort}");
        Console.WriteLine($"Remote: {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

        foreach (var header in context.Request.Headers)
            Console.WriteLine($"Request Header: {header.Key}: {header.Value}");

        Console.WriteLine($"Payload: {context.Request.Body.ReadAsStringAsync().Result}");

        // HR
        Console.WriteLine("---");
    }

    public static async Task CreateOKResponse(this HttpContext context, string? customResponse = null)
    {
        context.Response.ContentType = "application/xml";
        context.Response.StatusCode = 200;

        var defaultResponse = @"<response>OK</response>";
        await context.Response.WriteAsync(string.IsNullOrWhiteSpace(customResponse) ? defaultResponse : customResponse);
    }

    public static async Task CreatePlayMediaFileResponse(this HttpContext context, string hostname, string filename, string? extension)
    {
        context.Response.ContentType = "application/xml";
        context.Response.StatusCode = 200;

        var responseXML = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Play loop=""5"">http://{hostname}/media/{filename}{(extension != null ? $".{extension}" : null)}</Play>
</Response>
";
        await context.Response.WriteAsync(responseXML);
    }

    public static async Task CreateServerErrorResponse(this HttpContext context, string? customResponse = null)
    {
        await Task.CompletedTask;

        context.Response.StatusCode = 500;
    }

    public static async Task<string> ReadAsStringAsync(this Stream requestBody, bool leaveOpen = false)
    {
        using StreamReader reader = new(requestBody, leaveOpen: leaveOpen);
        var bodyAsString = await reader.ReadToEndAsync();

        return bodyAsString;
    }

    public static string TestNameFromLabel(this string label)
        => label + TestNameSuffix;

    public static string TestEndpointFromLabel(this string label)
        => label + TestEndpointSuffix;
}
