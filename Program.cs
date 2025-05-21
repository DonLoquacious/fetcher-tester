using fetcher_tester;
using System.Security.Cryptography.X509Certificates;
using Xunit;

var builder = WebApplication.CreateBuilder(args);

// Check the working directory and list its files
Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()))
{
    Console.WriteLine($"File: {file}");
}

var cert = X509CertificateLoader.LoadPkcs12FromFile("certificate.pfx", null, X509KeyStorageFlags.DefaultKeySet);
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

builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();
Assert.NotNull(app);

// set config provider for all to use- easy for unit tests
AppConfig.SetConfiguration(builder.Configuration);

var specific_test = builder.Configuration["test_to_run"];
var relayContext = builder.Configuration["relay_context"];

Dictionary<string, (RequestDelegate?, RequestDelegate)>? testLookup = null;
MapEndpoints(app);

Assert.NotNull(testLookup);

// create a relay listener, if a context has been provided
if (!string.IsNullOrWhiteSpace(relayContext))
{
    Console.WriteLine($"Creating new relay consumer for context {relayContext}.");
    var consumer = new RelayConsumer();

    if (consumer != null)
        consumer.Run();
    else
        Console.WriteLine($"Error: Relay consumer creation for context {relayContext} has failed.");
}

app.Run();

void MapEndpoints(WebApplication app)
{
    // map primary "run tests" endpoint, for the shell script to hit
    app.Map("/run-tests", RunAllTests);

    var actionTests = new ActionTests();
    actionTests.ValidationConfiguration();

    var playbackTests = new PlayTests();
    playbackTests.ValidationConfiguration();

    testLookup = new Dictionary<string, (RequestDelegate?, RequestDelegate)>() { { "basic", new(null, BasicEndpoint) } };
    testLookup = testLookup.Concat(actionTests.GenerateTestLookup()).ToDictionary();
    testLookup = testLookup.Concat(playbackTests.GenerateTestLookup()).ToDictionary();

    // map all individual tests, so they can be executed directly
    foreach (var kvp in testLookup)
    {
        if (kvp.Value.Item1 != null)
        {
            Console.WriteLine($"Adding test endpoint for: {kvp.Key.TestNameFromLabel()}");
            app.Map($"/run-test/{kvp.Key.TestNameFromLabel()}", kvp.Value.Item1);
        }
    }

    // map all endpoints needed for testing
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

    // run all tests one by one, until a failure occurs
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