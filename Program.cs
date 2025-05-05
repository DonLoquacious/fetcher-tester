using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net.Http.Headers;
using System.Reflection.Emit;

var builder = WebApplication.CreateBuilder(args);

// Check the working directory and list its files
Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()))
{
    Console.WriteLine($"File: {file}");
}

var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx");
Console.WriteLine($"Loaded certificate: {cert.Subject}");

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
        listenOptions.UseHttps("certificate.pfx", null, httpsOptions =>
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

const string HostnameTestLabel = "hostname";
const string Port8080TestLabel = "port-8080";
const string Port8080IPTestLabel = "port-8080-ip";
const string Port8080SSLTestLabel = "port-8080-ssl";
const string Port8080IPSSLTestLabel = "port-8080-ip-ssl";
const string IPTestLabel = "ip";
const string SSLTestLabel = "ssl";
const string IPSSLTestLabel = "ip-ssl";
const string DelayedTestLabel = "delay";

Dictionary<string, (RequestDelegate, RequestDelegate)> testLookup = new()
{
    { HostnameTestLabel, new (HostnameTest, BasicEndpoint) },
    { Port8080TestLabel, new (Port8080Test, BasicEndpoint) },
    { Port8080IPTestLabel, new (Port8080IPTest, BasicEndpoint) },
    { Port8080SSLTestLabel, new (Port8080SSLTest, BasicEndpoint) },
    { Port8080IPSSLTestLabel, new (Port8080IPSSLTest, BasicEndpoint) },
    { IPTestLabel,  new (IPTest, BasicEndpoint) },
    { SSLTestLabel,  new (SSLTest, BasicEndpoint) },
    { IPSSLTestLabel,  new (IPSSLTest, BasicEndpoint) },
    { DelayedTestLabel,  new (DelayedTest, DelayedEndpoint) }
};

// map primary "run tests" endpoint, for the shell script to hit
app.Map("/run-tests", RunAllTests);

// map all individual tests, so they can be executed directly
foreach (var kvp in testLookup)
    app.Map($"/run-test/{kvp.Key.TestNameFromLabel()}", () => kvp.Value.Item1);

// map all fetch endpoints to host, for testing
foreach (var kvp in testLookup)
    app.Map($"/{kvp.Key.TestEndpointFromLabel()}", kvp.Value.Item2);

app.Run();

 async Task RunAllTests(HttpContext context)
{
    RequestContextLog(context);

    if (!string.IsNullOrEmpty(specific_test) && testLookup.TryGetValue(specific_test, out var specific_kvp))
    {
        if (specific_kvp.Item1 != null)
        {
            await specific_kvp.Item1.Invoke(context);
            Console.WriteLine($"All tests have completed successfully.");
        }
        else
            Console.WriteLine($"Tests have completed- some errors have occurred.");

        return;
    }

    // run all tests one by one, until a failure occurs
    foreach (var kvp in testLookup)
    {
        await kvp.Value.Item1.Invoke(context);
        if (!context.ValidateTestResults(kvp.Value.Item1.Method.Name))
            return;
    }
}

async Task HostnameTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(HostnameTestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {HostnameTestLabel.TestEndpointFromLabel()} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task Port8080Test(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080TestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 8080))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080TestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task Port8080IPTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080IPTestLabel.TestEndpointFromLabel(), useIP: true, ssl: false, portNumber: 8080))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task Port8080SSLTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080SSLTestLabel.TestEndpointFromLabel(), useIP: false, ssl: true, portNumber: 8080))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080SSLTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task Port8080IPSSLTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(Port8080IPSSLTestLabel.TestEndpointFromLabel(), useIP: true, ssl: true, portNumber: 8080))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {Port8080IPSSLTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task IPTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(IPTestLabel.TestEndpointFromLabel(), useIP: true, ssl: false, portNumber: 80))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task SSLTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(SSLTestLabel.TestEndpointFromLabel(), useIP: false, ssl: true, portNumber: 443))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {SSLTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task IPSSLTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(IPSSLTestLabel.TestEndpointFromLabel(), useIP: true, ssl: true, portNumber: 443))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {IPSSLTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

async Task DelayedTest(HttpContext context)
{
    context.Response.StatusCode = 200;

    if (!await RequestToRestAPI_CreateNewCallWithTestParameters(DelayedTestLabel.TestEndpointFromLabel(), useIP: false, ssl: false, portNumber: 80))
    {
        Console.WriteLine($"Error: Executing REST API to trigger fetch for {DelayedTestLabel} failed.");
        context.Response.StatusCode = 500;
    }
}

/// <summary>
/// The bost basic of HTTP endpoints.
/// Logs all context details, and then returns 200/OK right away.
/// </summary>
async Task BasicEndpoint(HttpContext context)
{
    RequestContextLog(context);
    await CreateOKResponse(context);

    return;
}

/// <summary>
/// A delayed HTTP endpoint.
/// Will wait a configurable amount of time before returning 200/OK.
/// </summary>
async Task DelayedEndpoint(HttpContext context)
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

/// <summary>
/// Creates an OK response in the context object.
/// Will include an XML response payload, which is configurable.
/// </summary>
async Task CreateOKResponse(HttpContext context)
{
    context.Response.ContentType = "application/xml";
    context.Response.StatusCode = 200;

    Console.WriteLine($"Remote IP Address and port: {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

    var defaultResponse = "<response>OK</response>";
    await context.Response.WriteAsync(string.IsNullOrWhiteSpace(testResponse) ? defaultResponse : testResponse);
}

public static class ExtensionMethods
{
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

    public static string TestNameFromLabel(this string label)
        => label + "-test";

    public static string TestEndpointFromLabel(this string label)
        => label + "-endpoint";
}
