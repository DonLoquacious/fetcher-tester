using System.Net.Http.Headers;
using Xunit;

namespace fetcher_tester.endpoints;

/// <summary>
/// These tests exercise the "play" url for cXML scenarios.
/// </summary>
public class cXMLPlaybackTests
{
    public const string AviLabel = "avi";
    public const string Mp3Label = "mp3";
    public const string DelayedMp3Label = "delayed-mp3";
    public const string WavLabel = "wav";
    public const string OggLabel = "ogg";
    public const string Mp4Label = "mp4";
    public const string MovLabel = "mov";
    public const string PngLabel = "png";
    public const string TiffLabel = "tiff";
    public const string JpgLabel = "jpg";
    public const string PdfLabel = "pdf";

    public const string LabelPrefix = "cxml/playback";

    private readonly string? ProjectID;
    private readonly string? SpaceID;
    private readonly string? ApiToken;
    private readonly string? TestHostname;
    private readonly string? TestIp;
    private readonly string? TestToNumber;
    private readonly string? TestFromNumber;

    public cXMLPlaybackTests()
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
            { GenerateTestPath(AviLabel), AviTest },
            { GenerateTestPath(Mp3Label), Mp3Test },
            { GenerateTestPath(DelayedMp3Label), DelayedMp3TestEndpoint }
        };
    }

    public Dictionary<string, RequestDelegate> GetEndpoints()
    {
        return new() {
            { GenerateTestPath(AviLabel), AviTestResponseEndpoint },
            { GenerateTestPath(Mp3Label), Mp3TestResponseEndpoint },
        };
    }

    private static string GenerateTestPath(string label)
    {
        return $"{LabelPrefix}/{label}";
    }

    public bool ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(TestHostname))
        {
            Console.WriteLine("Error: Required test host configuration parameters not provided.");
            Console.WriteLine("Ensure that 'fetcher_tester_test_hostname' is set.");
            return false;
        }

        return true;
    }

    // The tests that should be executed via command line
    // `make test` will run all tests
    // `make test name=test_name` will run only a specific test
    //
    // test names are the label used + "-test"
    // so for instance "avi-test", "mp3-test", etc
    async Task AviTest(HttpContext context)
    {
        GeneralEndpoints.StatusCallbackReceivedEvent.Reset();

        context.RequestContextLog();
        await context.CreateOKcXMLResponse();

        var callbackStatusUrl = $"http://{TestHostname}/status_callback-endpoint";

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(AviLabel), useIP: false, ssl: false, portNumber: 80, callbackStatusUrl))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {AviLabel} failed.");
            await context.CreateServerErrorResponse();
            return;
        }

        if (!GeneralEndpoints.StatusCallbackReceivedEvent.WaitOne())
        {
            Console.WriteLine($"Error: Failed to receive status callback for test {Mp3Label}");
        }
    }

    async Task Mp3Test(HttpContext context)
    {
        Assert.NotNull(context);
        GeneralEndpoints.StatusCallbackReceivedEvent.Reset();

        context.RequestContextLog();
        await context.CreateOKcXMLResponse();

        var callbackStatusUrl = $"http://{TestHostname}/status_callback-endpoint";

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(GenerateTestPath(Mp3Label), useIP: false, ssl: false, portNumber: 80, callbackStatusUrl))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Mp3Label} failed.");
            await context.CreateServerErrorResponse();
            return;
        }

        if (!GeneralEndpoints.StatusCallbackReceivedEvent.WaitOne())
        {
            Console.WriteLine($"Error: Failed to receive status callback for test {Mp3Label}");
        }
    }

    async Task AviTestResponseEndpoint(HttpContext context)
    {
        Assert.NotNull(TestHostname);
        context.RequestContextLog();
        await context.CreatePlayMediaFilecXMLResponse(TestHostname, MediaEndpoints.AviLabel, null);
    }

    async Task DelayedMp3TestEndpoint(HttpContext context)
    {
        Assert.NotNull(TestHostname);
        context.RequestContextLog();
        await Task.Delay(TimeSpan.FromSeconds(4));
        await context.CreatePlayMediaFilecXMLResponse(TestHostname, MediaEndpoints.DelayedMp3Label, null);
    }

    async Task Mp3TestResponseEndpoint(HttpContext context)
    {
        Assert.NotNull(TestHostname);
        context.RequestContextLog();
        await context.CreatePlayMediaFilecXMLResponse(TestHostname, MediaEndpoints.Mp3Label, null);
    }

    async Task TwilioTestEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreateTwilioPlayMediaFilecXMLResponse();
    }

    // Create a new call with SignalWire, to begin a test
    async Task<bool> RequestToRestAPI_CreateNewCallWithTestParameters(string testName, bool useIP, bool ssl = false, int portNumber = 80, string? statusCallbackUrl = null)
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

        FormUrlEncodedContent? formContent = null;
        if (string.IsNullOrWhiteSpace(statusCallbackUrl)) {
            formContent = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string> ("Url", fetchUrl),
                new ("To", TestToNumber),
                new ("From", TestFromNumber)
            ]);
        }
        else
        {
            formContent = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string> ("Url", fetchUrl),
                new ("To", TestToNumber),
                new ("From", TestFromNumber),
                new ("StatusCallback", statusCallbackUrl),
                new ("StatusCallbackEvent", "answered")
            ]);
        }

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
