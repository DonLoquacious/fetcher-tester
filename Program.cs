using fetcher_tester;

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
var specific_test = builder.Configuration["test_to_run"];

Dictionary<string, (RequestDelegate, RequestDelegate)>? testLookup = null;

// map primary "run tests" endpoint, for the shell script to hit
app.Map("/run-tests", RunAllTests);

var actionTests = new ActionTests(builder.Configuration);
actionTests.ValidationConfiguration();

testLookup = actionTests.GenerateTestLookup().ToDictionary();
//testLookup = testLookup.Concat(otherTests.GenerateTestLookup()).ToDictionary();

// map all individual tests, so they can be executed directly
foreach (var kvp in testLookup)
    app.Map($"/run-test/{kvp.Key.TestNameFromLabel()}", kvp.Value.Item1);

// map all fetch endpoints to host, for testing
foreach (var kvp in testLookup)
    app.Map($"/{kvp.Key.TestEndpointFromLabel()}", kvp.Value.Item2);

app.Run();

 async Task RunAllTests(HttpContext context)
{
    context.RequestContextLog();

    if (testLookup == null)
    {
        Console.WriteLine("Error: Could not locate any tests to run.");
        return;
    }

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
