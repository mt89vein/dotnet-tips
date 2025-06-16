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

    // we can dynamically add variables, that depends on some static feature flags
    if (string.Equals(Environment.GetEnvironmentVariable("SOME_STATIC_FEATURE_ENABLED"), bool.TrueString, StringComparison.OrdinalIgnoreCase))
    {
        envs.Add(EnvVariable.AsRequired("STATIC_FEATURE_VARIABLE", populateTo: ["some:static_feature:value"]));

        // we can add same variable multiple times and set different populateTo (they will be merged)
        envs.Add(EnvVariable.AsRequired("STATIC_FEATURE_VARIABLE", populateTo: ["some:static_feature:value2"]));
    }

    var source = builder.Configuration.AddEnvSubstitution(
        LoggerFactory.Create(o => o.AddSerilog(logger)),
        envs,
        dynamicVariables: () =>
        {
            var dynamicEnvs = new List<EnvVariable>();

            // imagine here your dynamic feature flag check
            if (string.Equals(Environment.GetEnvironmentVariable("SOME_DYNAMIC_FEATURE_ENABLED"),
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase))
            {
                dynamicEnvs.Add(EnvVariable.AsRequired("DYNAMIC_FEATURE_VARIABLE",
                    populateTo: ["some:dynamic_feature:value"]));
            }

            return dynamicEnvs;
        });

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
        foreach (var envVariable in source.GetAllVariables())
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
    app.MapGet("/envs", () => source.GetAllVariables());

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
