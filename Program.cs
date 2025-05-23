using fetcher_tester;
using fetcher_tester.endpoints;
using fetcher_tester.relay;
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

Dictionary<string, RequestDelegate>? testLookup = null;
MapEndpoints(app);

Assert.NotNull(testLookup);
CancellationTokenSource endingToken = new();

if (!string.IsNullOrWhiteSpace(relayContext))
{
    Console.WriteLine($"Creating new relay consumer for context '{relayContext}'.");
    var consumer = new RelayConsumer();

    if (consumer != null)
    {
        Console.WriteLine($"Consumer created successfully- running.");
        _ = Task.Run(consumer.Run, endingToken.Token);
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

    Dictionary<string, RequestDelegate>? endpointLookup = null;

    var fetchTests = new cXMLFetchTests();
    fetchTests.ValidateConfiguration();
    testLookup = fetchTests.GetTests();
    endpointLookup = fetchTests.GetEndpoints();

    var playbackTests = new cXMLPlaybackTests();
    playbackTests.ValidateConfiguration();
    testLookup = testLookup.Concat(playbackTests.GetTests()).ToDictionary();
    endpointLookup = endpointLookup.Concat(playbackTests.GetEndpoints()).ToDictionary();

    foreach (var kvp in testLookup)
    {
        Console.WriteLine($"Adding test for: {kvp.Key}");
        app.Map($"/tests/{kvp.Key}", kvp.Value);
    }

    var generalEndpoints = new GeneralEndpoints();
    endpointLookup = endpointLookup.Concat(generalEndpoints.GetEndpoints()).ToDictionary();

    var cxmlEndpoints = new cXMLEndpoints();
    endpointLookup = endpointLookup.Concat(cxmlEndpoints.GetEndpoints()).ToDictionary();

    var mediaEndpoints = new MediaEndpoints();
    endpointLookup = endpointLookup.Concat(mediaEndpoints.GetEndpoints()).ToDictionary();

    foreach (var kvp in endpointLookup)
    {
        Console.WriteLine($"Adding endpoint for: {kvp.Key}");
        app.Map($"/endpoints/{kvp.Key}", kvp.Value);
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
        if (!testLookup.TryGetValue(specific_test, out var specific_kvp))
        {
            Console.WriteLine($"Error: Could not locate specific test {specific_test} to run.");
            return;
        }

        await specific_kvp.Invoke(context);
        Console.WriteLine($"Test {specific_test} has completed successfully.");
    }

    foreach (var kvp in testLookup)
    {
        await kvp.Value.Invoke(context);
        if (!context.ValidateTestResults(kvp.Value.Method.Name))
            return;
    }
}
