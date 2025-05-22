using fetcher_tester;
using System.Security.Cryptography.X509Certificates;
using Xunit;

var builder = WebApplication.CreateBuilder(args);
Assert.NotNull(builder);

Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()))
{
    Console.WriteLine($"File: {file}");
}

var httpsEnabled = false;
if (File.Exists("certificate.pfx"))
{
    httpsEnabled = true;
    var cert = X509CertificateLoader.LoadPkcs12FromFile("certificate.pfx", null, X509KeyStorageFlags.DefaultKeySet);
    Console.WriteLine($"Loaded certificate: {cert.Subject}");
}

var listenUrls = new List<string>();
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80, listenOptions =>
    {
        Console.WriteLine($"Configured HTTP endpoint at {listenOptions.EndPoint}");
    });
    listenUrls.Add("http://*:80");

    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        Console.WriteLine($"Configured HTTP endpoint at {listenOptions.EndPoint}");
    });
    listenUrls.Add("http://*:8080");

    if (httpsEnabled)
    {
        serverOptions.ListenAnyIP(443, listenOptions =>
        {
            listenOptions.UseHttps("certificate.pfx", null, httpsOptions =>
            {
                httpsOptions.HandshakeTimeout = TimeSpan.FromSeconds(5);
                httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;
            });
            Console.WriteLine($"Configured HTTPS endpoint at {listenOptions.EndPoint}");
        });
        listenUrls.Add("https://*:443");
    }
});

builder.WebHost.UseUrls([.. listenUrls]);

DotNetEnv.Env.Load();
builder.Configuration
    .AddEnvironmentVariables(prefix: "fetcher_tester_")
    .AddCommandLine(args);

builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();
Assert.NotNull(app);

AppConfig.SetConfiguration(builder.Configuration);

string? specific_test = builder.Configuration["test_to_run"];
string? relayContext = builder.Configuration["relay_context"];

Dictionary<string, (RequestDelegate?, RequestDelegate)>? testLookup = null;
MapEndpoints(app);

Assert.NotNull(testLookup);
CancellationTokenSource endingToken = new CancellationTokenSource();

if (!string.IsNullOrWhiteSpace(relayContext))
{
    Console.WriteLine($"Creating new relay consumer for context '{relayContext}'.");
    var consumer = new RelayConsumer();

    if (consumer != null)
    {
        Console.WriteLine($"Consumer created successfully- running.");
        await Task.Run(consumer.Run, endingToken.Token);
    }
    else
        Console.WriteLine($"Error: Relay consumer creation for context '{relayContext}' has failed.");
}

Console.WriteLine($"Running webhost and hosting application.");
app.Run();

endingToken.Cancel();

void MapEndpoints(WebApplication app)
{
    app.Map("/run-tests", RunAllTests);

    var actionTests = new ActionTests();
    actionTests.ValidationConfiguration();

    var playbackTests = new PlayTests();
    playbackTests.ValidationConfiguration();

    testLookup = new Dictionary<string, (RequestDelegate?, RequestDelegate)>() { { "basic", new(null, BasicEndpoint) } };
    testLookup = testLookup.Concat(actionTests.GenerateTestLookup()).ToDictionary();
    testLookup = testLookup.Concat(playbackTests.GenerateTestLookup()).ToDictionary();

    foreach (var kvp in testLookup)
    {
        if (kvp.Value.Item1 != null)
        {
            Console.WriteLine($"Adding test endpoint for: {kvp.Key.TestNameFromLabel()}");
            app.Map($"/run-test/{kvp.Key.TestNameFromLabel()}", kvp.Value.Item1);
        }
    }

    foreach (var kvp in testLookup)
    {
        Console.WriteLine($"Adding endpoint for: {kvp.Key.TestEndpointFromLabel()}");
        app.Map($"/{kvp.Key.TestEndpointFromLabel()}", kvp.Value.Item2);
    }
}

async Task RunAllTests(HttpContext context)
{
    Assert.NotNull(context);
    context.RequestContextLog();

    if (testLookup == null)
    {
        Console.WriteLine("Error: Could not locate any tests to run.");
        return;
    }

    if (!string.IsNullOrEmpty(specific_test))
    {
        if (!testLookup.TryGetValue(specific_test, out var specific_kvp) || specific_kvp.Item1 == null)
        {
            Console.WriteLine($"Error: Could not locate specific test {specific_test} to run.");
            return;
        }

        await specific_kvp.Item1.Invoke(context);
        Console.WriteLine($"Test {specific_test} has completed successfully.");
    }

    foreach (var kvp in testLookup)
    {
        if (kvp.Value.Item1 == null)
            continue;

        await kvp.Value.Item1.Invoke(context);
        if (!context.ValidateTestResults(kvp.Value.Item1.Method.Name))
            return;
    }
}

async Task BasicEndpoint(HttpContext context)
{
    Assert.NotNull(context);
    await Task.CompletedTask;

    context.RequestContextLog();
    context.Response.StatusCode = 200;
}
