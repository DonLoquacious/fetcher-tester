using System.Net.Http.Headers;

namespace fetcher_tester;

/// <summary>
/// These tests exercise the "play" url for cXML scenarios.
/// </summary>
public class PlayTests
{
    const string AviTestFilename = "file_example_AVI_480_750kB.avi";
    const string Mp3TestFilename = "file_example_MP3_1MG.mp3";
    const string WavTestFilename = "file_example_WAV_1MG.wav";
    const string OggTestFilename = "file_example_OOG_1MG.ogg";
    const string Mp4TestFilename = "file_example_MP4_480_1_5MG.mp4";
    const string MovTestFilename = "file_example_MOV_480_700kB.mov";
    const string PngTestFilename = "file_example_PNG_1MB.png";
    const string TiffTestFilename = "file_example_TIFF_1MB.tiff";
    const string JpgTestFilename = "file_example_JPG_1MB.jpg";
    const string PdfTestFilename = "file_example_PDF_1MB.pdf";

    const string AviTestLabel = "avi";
    const string Mp3TestLabel = "mp3";
    const string WavTestLabel = "wav";
    const string OggTestLabel = "ogg";
    const string Mp4TestLabel = "mp4";
    const string MovTestLabel = "mov";
    const string PngTestLabel = "png";
    const string TiffTestLabel = "tiff";
    const string JpgTestLabel = "jpg";
    const string PdfTestLabel = "pdf";

    AutoResetEvent StatusCallbackReceivedEvent = new AutoResetEvent(false);

    private readonly string? ProjectID;
    private readonly string? SpaceID;
    private readonly string? ApiToken;
    private readonly string? TestHostname;
    private readonly string? TestIp;
    private readonly string? TestToNumber;
    private readonly string? TestFromNumber;

    public PlayTests(ConfigurationManager configuration)
    {
        TestToNumber = configuration["test_to_number"];
        TestFromNumber = configuration["test_from_number"];
        ProjectID = configuration["test_project_id"];
        SpaceID = configuration["test_space_id"];
        ApiToken = configuration["test_api_token"];
        TestHostname = configuration["test_hostname"];
        TestIp = configuration["test_ip"];
    }

    public Dictionary<string, (RequestDelegate?, RequestDelegate)> GenerateTestLookup()
    {
        return new() {
            { AviTestLabel, new(AviTest, AviTestEndpoint) },
            { Mp3TestLabel, new(Mp3Test, Mp3TestEndpoint) },
            { "delayed-mp3", new(null, DelayedMp3TestEndpoint) },
            { "status_callback", new (null, StatusCallbackEndpoint) },
            { $"media/{AviTestLabel}", new (null, AviFilehost) },
            { $"media/{PdfTestLabel}", new (null, PdfFilehost) },
            { $"media/{TiffTestLabel}", new (null, TiffFilehost) },
            { $"media/{Mp3TestLabel}", new (null, Mp3Filehost) },
            { $"media/{WavTestLabel}", new (null, WavFilehost) },
            { $"media/{Mp4TestLabel}", new (null, Mp4Filehost) },
            { $"media/{JpgTestLabel}", new (null, JpgFilehost) },
            { $"media/{PngTestLabel}", new (null, PngFilehost) },
            { $"media/{OggTestLabel}", new (null, OggFilehost) },
            { $"media/{MovTestLabel}", new (null, MovFilehost) },
            { $"media/delayed-mp3", new (null, DelayedMp3Filehost)}
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

    // The tests that should be executed via command line
    // `make test` will run all tests
    // `make test name=test_name` will run only a specific test
    //
    // test names are the label used + "-test"
    // so for instance "avi-test", "mp3-test", etc
    async Task AviTest(HttpContext context)
    {
        StatusCallbackReceivedEvent.Reset();

        context.RequestContextLog();
        await context.CreateOKResponse();

        var callbackStatusUrl = $"http://{TestHostname}/status_callback-endpoint";

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(AviTestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80, callbackStatusUrl))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {AviTestLabel.TestEndpointFromLabel()} failed.");
            await context.CreateServerErrorResponse();
            return;
        }

        if (!StatusCallbackReceivedEvent.WaitOne())
        {
            Console.WriteLine($"Error: Failed to receive status callback for test {Mp3TestLabel.TestEndpointFromLabel()}");
        }
    }

    async Task Mp3Test(HttpContext context)
    {
        StatusCallbackReceivedEvent.Reset();

        context.RequestContextLog();
        await context.CreateOKResponse();

        var callbackStatusUrl = $"http://{TestHostname}/status_callback-endpoint";

        if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Mp3TestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80, callbackStatusUrl))
        {
            Console.WriteLine($"Error: Executing REST API to trigger fetch for {Mp3TestLabel.TestEndpointFromLabel()} failed.");
            await context.CreateServerErrorResponse();
            return;
        }

        if (!StatusCallbackReceivedEvent.WaitOne())
        {
            Console.WriteLine($"Error: Failed to receive status callback for test {Mp3TestLabel.TestEndpointFromLabel()}");
        }
    }

    async Task AviTestEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreatePlayMediaFileResponse(TestHostname, AviTestLabel.TestEndpointFromLabel(), null);
    }

    async Task DelayedMp3TestEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await Task.Delay(TimeSpan.FromSeconds(5));
        await context.CreatePlayMediaFileResponse(TestHostname, Mp3TestLabel.TestEndpointFromLabel(), null);
    }

    async Task Mp3TestEndpoint(HttpContext context)
    {
        context.RequestContextLog();
        await context.CreatePlayMediaFileResponse(TestHostname, Mp3TestLabel.TestEndpointFromLabel(), null);
    }

    async Task DelayedMp3Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await Task.Delay(TimeSpan.FromSeconds(5));
        await context.Response.SendFileAsync("media/" + Mp3TestFilename);
    }

    async Task Mp3Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await context.Response.SendFileAsync("media/" + Mp3TestFilename);
    }

    async Task WavFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/wav";

        await context.Response.SendFileAsync("media/" + WavTestFilename);
    }

    async Task AviFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/avi";

        await context.Response.SendFileAsync("media/" + AviTestFilename);
    }

    async Task Mp4Filehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/mp4";

        await context.Response.SendFileAsync("media/" + Mp4TestFilename);
    }

    async Task MovFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "video/quicktime";

        await context.Response.SendFileAsync("media/" + MovTestFilename);
    }

    async Task OggFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/ogg";

        await context.Response.SendFileAsync("media/" + OggTestFilename);
    }

    async Task PdfFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "audio/mpeg3";

        await context.Response.SendFileAsync("media/" + PdfTestFilename);
    }

    async Task PngFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/png";

        await context.Response.SendFileAsync("media/" + PngTestFilename);
    }

    async Task JpgFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/jpg";

        await context.Response.SendFileAsync("media/" + JpgTestFilename);
    }

    async Task TiffFilehost(HttpContext context)
    {
        context.RequestContextLog();
        context.Response.StatusCode = 200;
        context.Response.ContentType = "image/tiff";

        await context.Response.SendFileAsync("media/" + TiffTestFilename);
    }

    // Status callback endpoint hit for all tests
    async Task StatusCallbackEndpoint(HttpContext context)
    {
        StatusCallbackReceivedEvent.Set();

        await context.CreateOKResponse();
        Console.WriteLine("Status callback received successfully!");

        var statusCallbackJSON = await context.Request.Body.ReadAsStringAsync();
        Console.WriteLine("Status callback content: " + statusCallbackJSON);
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
