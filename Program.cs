using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80, listenOptions =>
    {
        Console.WriteLine($"Configured HTTP endpoint at {listenOptions.EndPoint}");
    });

    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        Console.WriteLine($"Configured HTTP endpoint at {listenOptions.EndPoint}");
    });

    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps(httpsOptions =>
        {
            httpsOptions.HandshakeTimeout = TimeSpan.FromSeconds(5);
            httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;
        });
        Console.WriteLine($"Configured HTTPS endpoint at {listenOptions.EndPoint}");
    });

});

builder.WebHost.UseUrls("http://*:80", "http://*:8080", "https://*:443");

DotNetEnv.Env.Load();
builder.Configuration
    .AddEnvironmentVariables(prefix: "fetcher_tester_")
    .AddCommandLine(args);

var app = builder.Build();

var projectID = builder.Configuration["test_project_id"];
var spaceID = builder.Configuration["test_space_id"];
var apiToken = builder.Configuration["test_api_token"];
var testHostname = builder.Configuration["test_hostname"];
var testIp = builder.Configuration["test_ip"];
var testResponse = builder.Configuration["test_response"];
var testDelayMS = builder.Configuration.GetValue("test_delay_ms", 4000);
var testToNumber = builder.Configuration["test_to_number"];
var testFromNumber = builder.Configuration["test_from_number"];
var specific_test = builder.Configuration["test_to_run"];

if (string.IsNullOrEmpty(testHostname) || string.IsNullOrEmpty(testIp))
{
    Console.WriteLine("Error: Required test host configuration parameters not provided.");
    Console.WriteLine("Ensure that 'fetcher_tester_test_hostname' and 'fetcher_tester_test_ip' are set.");
    return;
}

if (string.IsNullOrEmpty(projectID) || string.IsNullOrEmpty(spaceID) || string.IsNullOrEmpty(apiToken))
{
    Console.WriteLine("Error: Required test auth configuration parameters not provided.");
    Console.WriteLine("Ensure that 'fetcher_tester_test_project_id', 'fetcher_tester_test_space_id', 'fetcher_tester_test_api_token' are set.");
    return;
}

if (string.IsNullOrEmpty(testToNumber) || string.IsNullOrEmpty(testFromNumber))
{
    Console.WriteLine("Error: Required configuration 'test_to_number' or 'test_from_number' not provided (even though the docs say they aren't required...)");
    return;
}

const string fetchHostnameEndpointName = "fetch-hostname-test";
const string fetchPort8080EndpointName = "fetch-port-8080-test";
const string fetchPort8080SSLEndpointName = "fetch-port-8080-ssl-test";
const string fetchIPEndpointName = "fetch-ip-test";
const string fetchSSLEndpointName = "fetch-ssl-test";
const string fetchIPSSLEndpointName = "fetch-ip-ssl-test";
const string fetchDelayedEndpointName = "fetch-delay-test";

Dictionary<string, (Func<Task<bool>>, RequestDelegate)> testLookup = new()
{
    { fetchHostnameEndpointName, new (RunHostnameTest, FetchBasicTest) },
    { fetchPort8080EndpointName, new (RunPort8080Test, FetchBasicTest) },
    { fetchPort8080SSLEndpointName, new (RunPort8080SSLTest, FetchBasicTest) },
    { fetchIPEndpointName,  new (RunIPTest, FetchBasicTest) },
    { fetchSSLEndpointName,  new (RunSSLTest, FetchBasicTest) },
    { fetchIPSSLEndpointName,  new (RunIPSSLTest, FetchBasicTest) },
    { fetchDelayedEndpointName,  new (RunDelayedTest, FetchDelayedTest) }
};

// map primary "run tests" endpoint, for the shell script to hit
app.Map("/run-tests", async (HttpContext context) =>
{
    var success = true;
    RequestContextLog(context);

    if (!string.IsNullOrEmpty(specific_test) && testLookup.TryGetValue(specific_test, out var kvp))
    {
        if (kvp.Item1 != null && await kvp.Item1.Invoke())
            Console.WriteLine($"All tests have completed successfully.");
        else
            Console.WriteLine($"Tests have completed- some errors have occurred.");

        return;
    }

    // run all tests one by one, until a failure occurs
    if (!await RunHostnameTest())
        success = false;
    else if (!await RunIPTest())
        success = false;
    else if (!await RunPort8080Test())
        success = false;
    else if (!await RunPort8080SSLTest())
        success = false;
    else if (!await RunSSLTest())
        success = false;
    else if (!await RunIPSSLTest())
        success = false;
    else if (!await RunDelayedTest())
        success = false;

    if (success)
        Console.WriteLine($"All tests have completed successfully.");
    else
        Console.WriteLine($"Tests have completed- some errors have occurred.");
});

// map all fetch endpoints to host, for testing
foreach (var kvp in testLookup)
    app.Map($"/{kvp.Key}", kvp.Value.Item2);

async Task<bool> RunHostnameTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchHostnameEndpointName, useIP: false, ssl: false, portNumber: 80))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchHostnameEndpointName} failed.");
    }

    return success;
}

async Task<bool> RunPort8080Test()
{
    var success = true;
    if (!await ExecuteFetch(fetchPort8080EndpointName, useIP: false, ssl: false, portNumber: 8080))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchPort8080EndpointName} failed.");
    }

    return success;
}

async Task<bool> RunPort8080SSLTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchPort8080SSLEndpointName, useIP: false, ssl: true, portNumber: 8080))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchPort8080SSLEndpointName} failed.");
    }

    return success;
}

async Task<bool> RunIPTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchIPEndpointName, useIP: true, ssl: false, portNumber: 80))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchIPEndpointName} failed.");
    }

    return success;
}

async Task<bool> RunSSLTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchSSLEndpointName, useIP: false, ssl: true, portNumber: 443))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchSSLEndpointName} failed.");
    }

    return success;
}

async Task<bool> RunIPSSLTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchIPSSLEndpointName, useIP: true, ssl: true, portNumber: 443))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchIPSSLEndpointName} failed.");
    }

    return success;
}

async Task<bool> RunDelayedTest()
{
    var success = true;
    if (!await ExecuteFetch(fetchDelayedEndpointName, useIP: false, ssl: false, portNumber: 80))
    {
        success = false;
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {fetchDelayedEndpointName} failed.");
    }

    return success;
}

async Task FetchBasicTest(HttpContext context)
{
    RequestContextLog(context);
    await CreateOKResponse(context);

    return;
}

async Task FetchDelayedTest(HttpContext context)
{
    RequestContextLog(context);

    await Task.Delay(TimeSpan.FromMilliseconds(testDelayMS));
    await CreateOKResponse(context);

    return;
}

void RequestContextLog(HttpContext context)
{
    Console.WriteLine($"Endpoint: {context.GetEndpoint()}");
    Console.WriteLine($"HTTP Method: {context.Request.Method}");

    foreach (var header in context.Request.Headers)
        Console.WriteLine($"Request Header: {header.Key}: {header.Value}");
}

async Task<bool> ExecuteFetch(string testName, bool useIP, bool ssl = false, int portNumber = 80)
{
    var fetchUrl = $"{(ssl ? "https" : "http")}://{(useIP ? testIp : testHostname)}:{portNumber}/{testName}";
    var requestUrl = $"https://{(string.IsNullOrEmpty(spaceID) ? "dev.swire.io" : spaceID)}/api/laml/2010-04-01/Accounts/{projectID}/Calls";

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
        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{projectID}:{apiToken}")));

    var formContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string> ("Url", fetchUrl),
        new ("To", testToNumber),
        new ("From", testFromNumber)
    });

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

async Task CreateOKResponse(HttpContext context)
{
    context.Response.ContentType = "application/xml";
    context.Response.StatusCode = 200;

    Console.WriteLine($"Remote IP Address and port: {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

    var defaultResponse = "<response>OK</response>";
    await context.Response.WriteAsync(string.IsNullOrWhiteSpace(testResponse) ? defaultResponse : testResponse);
}

app.Run();
