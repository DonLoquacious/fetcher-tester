using System.Net.Http.Headers;
using Xunit;

namespace fetcher_tester;

/// <summary>
/// These tests exercise the "action" callback url for cXML scenarios.
/// For instance, on a new call creation using the REST API, you can specify the action url
/// that SignalWire should use to fetch the cXML script to run for the caller, once connected.
/// 
/// Using a dummy SignalWire-associate "to" number (just set up to wait for a minute, then hang up),
/// we can bypass needing an actual client to be connected and receiving these new calls.
/// </summary>
public class cXMLFetchTests
{
    public const string HostnameLabel = "hostname";
    public const string Port8080Label = "port-8080";
    public const string Port8080IPLabel = "port-8080-ip";
    public const string Port8080SSLLabel = "port-8080-ssl";
    public const string Port8080IPSSLLabel = "port-8080-ip-ssl";
    public const string IPLabel = "ip";
    public const string SSLLabel = "ssl";
    public const string IPSSLLabel = "ip-ssl";
    public const string DelayedLabel = "delay";

    public const string LabelPrefix = "cxml-fetch";

    private readonly string? ProjectID;
    private readonly string? SpaceID;
    private readonly string? ApiToken;
    private readonly string? TestHostname;
    private readonly string? TestIp;
    private readonly string? TestToNumber;
    private readonly string? TestFromNumber;

    public cXMLFetchTests()
    {
        TestToNumber = AppConfig.GetConfigValue("test_to_number");
        TestFromNumber = AppConfig.GetConfigValue("test_from_number");
        ProjectID = AppConfig.GetConfigValue("test_project_id");
        SpaceID = AppConfig.GetConfigValue("test_space_id");
        ApiToken = AppConfig.GetConfigValue("test_api_token");
        TestHostname = AppConfig.GetConfigValue("test_hostname");
        TestIp = AppConfig.GetConfigValue("test_ip");

        Assert.NotNull(ProjectID);
        Assert.NotNull(SpaceID);
        Assert.NotNull(ApiToken);
    }

    public Dictionary<string, RequestDelegate> GetTests()
    {
        return new() {
            { GenerateTestPath(HostnameLabel), HostnameTest },
            { GenerateTestPath(Port8080Label), Port8080Test },
            { GenerateTestPath(Port8080IPLabel), Port8080IPTest },
            { GenerateTestPath(Port8080SSLLabel), Port8080SSLTest },
            { GenerateTestPath(Port8080IPSSLLabel), Port8080IPSSLTest },
            { GenerateTestPath(IPLabel), IPTest },
            { GenerateTestPath(SSLLabel), SSLTest },
            { GenerateTestPath(IPSSLLabel), IPSSLTest },
            { GenerateTestPath(DelayedLabel), DelayedTest }
        };
    }

    private static string GenerateTestPath(string label)
    {
        return $"{LabelPrefix}/{label}";
    }

    public Dictionary<string, RequestDelegate> GetEndpoints()
    {
        return [];
    }

    public bool ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(TestHostname) || string.IsNullOrEmpty(TestIp))
        {
            Console.WriteLine("Error: Required test host configuration parameters not provided.");
            Console.WriteLine("Ensure that 'fetcher_tester_test_hostname' and 'fetcher_tester_test_ip' are set.");
            return false;
        }

        if (string.IsNullOrEmpty(ProjectID) || string.IsNullOrEmpty(SpaceID) || string.IsNullOrEmpty(ApiToken))
        {
            Console.WriteLine("Error: Required test auth configuration parameters not provided.");
            Console.WriteLine("Ensure that 'fetcher_tester_test_project_id', 'fetcher_tester_test_space_id', 'fetcher_tester_test_api_token' are set.");
            return false;
        }

        if (string.IsNullOrEmpty(TestToNumber) || string.IsNullOrEmpty(TestFromNumber))
        {
            Console.WriteLine("Error: Required configuration 'test_to_number' or 'test_from_number' not provided (even though the docs say they aren't required...)");
            return false;
        }

        return true;
    }

    async Task HostnameTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(HostnameLabel), useIP: false, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {HostnameLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080Test(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(Port8080Label), useIP: false, ssl: false, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080Label} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080IPTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(Port8080IPLabel), useIP: true, ssl: false, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080SSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(Port8080SSLLabel), useIP: false, ssl: true, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080SSLLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080IPSSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(Port8080IPSSLLabel), useIP: true, ssl: true, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPSSLLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task IPTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(IPLabel), useIP: true, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task SSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(SSLLabel), useIP: false, ssl: true, portNumber: 443))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {SSLLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task IPSSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(IPSSLLabel), useIP: true, ssl: true, portNumber: 443))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPSSLLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task DelayedTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(DelayedLabel), useIP: false, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {DelayedLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }



    /// <summary>
    /// Makes a request to a SignalWire REST API to create a new call.
    /// The new call auth/numbers are provided by configuration (required).
    /// All tests use the same numbers for simplicity.
    /// If the number provided in "to" is a SignalWire number, that number will be "executed" as part of this process.
    /// This can be ignored / just don't point the number to anything important. It isn't what is being tested, but is required.
    /// 
    /// Returning false indicates the request itself failed- this isn't a part of the test, and should be debugged and fixed.
    /// </summary>
    async Task<bool> RequestToRestAPI_CreateNewCallWithTestParameters(string testName, bool useIP, bool ssl = false, int portNumber = 80)
    {
        var fetchUrl = $"{(ssl ? "https" : "http")}://{(useIP ? TestIp : TestHostname)}:{portNumber}/tests/{testName}";
        var requestUrl = $"https://{(string.IsNullOrEmpty(SpaceID) ? "dev.swire.io" : SpaceID)}/api/laml/2010-04-01/Accounts/{ProjectID}/Calls";

        var clientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, sslPolicyErrors) =>
            {
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch ||
                       sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        };

        using var httpClient = new HttpClient(clientHandler);
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{ProjectID}:{ApiToken}")));

        var formContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string> ("Url", fetchUrl),
            new ("To", TestToNumber),
            new ("From", TestFromNumber)
        ]);

        request.Content = formContent;
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        try
        {
            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("HTTP Request succeeded.");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                return true;
            }
            else
            {
                Console.WriteLine($"HTTP Request failed. Status Code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");

            if (ex.InnerException != null && ex.InnerException != ex)
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }

        return false;
    }
}
