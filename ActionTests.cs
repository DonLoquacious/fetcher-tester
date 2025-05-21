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
public class ActionTests
{
    const string HostnameTestLabel = "hostname";
    const string Port8080TestLabel = "port-8080";
    const string Port8080IPTestLabel = "port-8080-ip";
    const string Port8080SSLTestLabel = "port-8080-ssl";
    const string Port8080IPSSLTestLabel = "port-8080-ip-ssl";
    const string IPTestLabel = "ip";
    const string SSLTestLabel = "ssl";
    const string IPSSLTestLabel = "ip-ssl";
    const string DelayedTestLabel = "delay";

    private readonly string? ProjectID;
    private readonly string? SpaceID;
    private readonly string? ApiToken;
    private readonly string? TestHostname;
    private readonly string? TestIp;
    private readonly string? TestResponse;
    private readonly int TestDelayMS;
    private readonly string? TestToNumber;
    private readonly string? TestFromNumber;

    public ActionTests()
    {
        TestDelayMS = AppConfig.GetConfigValue("test_delay_ms", 4000);
        TestToNumber = AppConfig.GetConfigValue("test_to_number");
        TestFromNumber = AppConfig.GetConfigValue("test_from_number");
        ProjectID = AppConfig.GetConfigValue("test_project_id");
        SpaceID = AppConfig.GetConfigValue("test_space_id");
        ApiToken = AppConfig.GetConfigValue("test_api_token");
        TestHostname = AppConfig.GetConfigValue("test_hostname");
        TestIp = AppConfig.GetConfigValue("test_ip");
        TestResponse = AppConfig.GetConfigValue("test_response");

        Assert.NotNull(ProjectID);
        Assert.NotNull(SpaceID);
        Assert.NotNull(ApiToken);
    }

    public Dictionary<string, (RequestDelegate?, RequestDelegate)> GenerateTestLookup()
    {
        return new() {
            { HostnameTestLabel, new(HostnameTest, BasicEndpoint) },
            { Port8080TestLabel, new(Port8080Test, BasicEndpoint) },
            { Port8080IPTestLabel, new(Port8080IPTest, BasicEndpoint) },
            { Port8080SSLTestLabel, new(Port8080SSLTest, BasicEndpoint) },
            { Port8080IPSSLTestLabel, new(Port8080IPSSLTest, BasicEndpoint) },
            { IPTestLabel,  new(IPTest, BasicEndpoint) },
            { SSLTestLabel,  new(SSLTest, BasicEndpoint) },
            { IPSSLTestLabel,  new(IPSSLTest, BasicEndpoint) },
            { DelayedTestLabel,  new(DelayedTest, DelayedEndpoint) }
        };
    }

    public bool ValidationConfiguration()
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

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(HostnameTestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {HostnameTestLabel.TestEndpointFromLabel()} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080Test(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080TestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080TestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080IPTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080IPTestLabel.TestEndpointFromLabel(), useIP: true, ssl: false, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080SSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080SSLTestLabel.TestEndpointFromLabel(), useIP: false, ssl: true, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080SSLTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task Port8080IPSSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080IPSSLTestLabel.TestEndpointFromLabel(), useIP: true, ssl: true, portNumber: 8080))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPSSLTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task IPTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(IPTestLabel.TestEndpointFromLabel(), useIP: true, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task SSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(SSLTestLabel.TestEndpointFromLabel(), useIP: false, ssl: true, portNumber: 443))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {SSLTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task IPSSLTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(IPSSLTestLabel.TestEndpointFromLabel(), useIP: true, ssl: true, portNumber: 443))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPSSLTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    async Task DelayedTest(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse();

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(DelayedTestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {DelayedTestLabel} failed.");
            await context.CreateServerErrorResponse();
        }
    }

    /// <summary>
    /// The most basic of HTTP endpoints.
    /// Logs all context details, and then returns 200/OK right away.
    /// </summary>
    async Task BasicEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateOKResponse(TestResponse);

        return;
    }

    /// <summary>
    /// A delayed HTTP endpoint.
    /// Will wait a configurable amount of time before returning 200/OK.
    /// </summary>
    async Task DelayedEndpoint(HttpContext context)
    {
        context.RequestContextLog();

        await Task.Delay(TimeSpan.FromMilliseconds(TestDelayMS));
        await context.CreateOKResponse(TestResponse);

        return;
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
        var fetchUrl = $"{(ssl ? "https" : "http")}://{(useIP ? TestIp : TestHostname)}:{portNumber}/{testName}";
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
