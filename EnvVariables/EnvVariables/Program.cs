using EnvVariables;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();

    var logger = new LoggerConfiguration()
        .MinimumLevel.Is(LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter())
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Logging.AddSerilog(logger);

    var envs = new List<EnvVariable>
    {
        EnvVariable.AsRequired(name: "REQUIRED_VARIABLE", populateTo: ["my:deep:section:value", "my:deep:anotherSection:value"]),
        EnvVariable.AsOptional(name: "NOT_REQUIRED_VARIABLE", populateTo: ["top_level_value"])
    };

    // we can dynamically add variables, that depends on some feature flags
    if (builder.Configuration.GetValue("SOME_FEATURE_ENABLED", true))
    {
        envs.Add(EnvVariable.AsRequired("FEATURE_VARIABLE", populateTo: ["some:feature:value"]));

        // we can add same variable multiple times and set different populateTo (they will be merged)
        envs.Add(EnvVariable.AsRequired("FEATURE_VARIABLE", populateTo: ["some:feature:value2"]));
    }

    var source = builder.Configuration.AddEnvSubstitution(
        LoggerFactory.Create(o => o.AddSerilog(logger)),
        envs
    );

    // also, we can add it later after registration
    source.Add(EnvVariable.AsRequired("ONE_MORE_VARIABLE", populateTo: ["one:more:section:value"]));

    var app = builder.Build();

    app.MapGet("/", ([FromServices] IConfiguration config) =>
    {
        var additionals = new[]
        {
            "my:deep:section:additionalPropertyAlsoWorks",
            "my:additionalPropertyAlsoWorks"
        };

        var result = new Dictionary<string, string?>();
        foreach (var envVariable in source.EnvVariables)
        {
            foreach (var p in envVariable.PopulateTo)
            {
                result[p] = config[p] + $" -> [from env variable {envVariable.Name}]";
            }
        }

        foreach (var additional in additionals)
        {
            result[additional] = config[additional];
        }

        return Results.Ok(result);
    });

    // easy to get configured variables
    app.MapGet("/envs", () => source.EnvVariables);

    if (args.Any(a => a.Equals("print-envs", StringComparison.OrdinalIgnoreCase)))
    {
        source.PrintEnvs();
        Environment.Exit(0);
    }

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
