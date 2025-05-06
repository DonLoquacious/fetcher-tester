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

        foreach (var header in context.Request.Headers)
            Console.WriteLine($"Request Header: {header.Key}: {header.Value}");
    }

    public static async Task CreateOKResponse(this HttpContext context, string? customResponse = null)
    {
        context.Response.ContentType = "application/xml";
        context.Response.StatusCode = 200;

        Console.WriteLine($"Remote IP Address and port: {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

        var defaultResponse = "<response>OK</response>";
        await context.Response.WriteAsync(string.IsNullOrWhiteSpace(customResponse) ? defaultResponse : customResponse);
    }

    public static string TestNameFromLabel(this string label)
        => label + TestNameSuffix;

    public static string TestEndpointFromLabel(this string label)
        => label + TestEndpointSuffix;
}
