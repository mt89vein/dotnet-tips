using System.Text.Json;
using EnvVariables;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;
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
        .MinimumLevel
        .Is(LogEventLevel.Information)
        .MinimumLevel
        .Override("Microsoft", LogEventLevel.Information)
        .Enrich
        .FromLogContext()
        .WriteTo
        .Console(new JsonFormatter())
        .ReadFrom
        .Configuration(builder.Configuration)
        .CreateLogger();

    builder.Logging.AddSerilog(logger);

    var envs = new List<EnvVariable>
    {
        EnvVariableBuilder
            .Required("REQUIRED_VARIABLE")
            .WithDescription("Example of required variable")
            .WithPopulateTo("my:deep:section:value", "my:deep:anotherSection:value"),
        EnvVariableBuilder
            .Optional("NOT_REQUIRED_VARIABLE")
            .WithDescription("Example of optional variable")
            .WithPopulateTo("top_level_value"),
    };

    // we can dynamically add variables, that depends on some static feature flags
    if (string.Equals(Environment.GetEnvironmentVariable("SOME_STATIC_FEATURE_ENABLED"), bool.TrueString,
            StringComparison.OrdinalIgnoreCase))
    {
        envs.Add(EnvVariableBuilder
            .Required("STATIC_FEATURE_VARIABLE")
            .WithDescription("static env variable")
            .WithPopulateTo("some:static_feature:value"));

        // we can add same variable multiple times and set different populateTo (they will be merged)
        envs.Add(EnvVariableBuilder
            .Required("STATIC_FEATURE_VARIABLE")
            .WithDescription("static env variable")
            .WithPopulateTo("some:static_feature:value2"));
    }

    var envVariablesProvider = builder.Configuration.AddEnvVariables(
        envs,
        dynamicVariables: () =>
        {
            var dynamicEnvs = new List<EnvVariable>();

            // imagine here your dynamic feature flag check
            if (string.Equals(Environment.GetEnvironmentVariable("SOME_DYNAMIC_FEATURE_ENABLED"),
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase))
            {

                dynamicEnvs.Add(EnvVariableBuilder
                    .Optional("DYNAMIC_FEATURE_VARIABLE")
                    .WithDescription("Dynamic feature variable")
                    .WithPopulateTo("some:dynamic_feature:value")
                );
            }

            return dynamicEnvs;
        });

    // also, we can add it later after registration
    envVariablesProvider.Add([
        EnvVariableBuilder
            .Required("ONE_MORE_VARIABLE")
            .WithDescription("one more!")
            .WithPopulateTo("one:more:section:value")
    ]);

    var app = builder.Build();

    app.MapGet("/", ([FromServices] IConfiguration config) =>
    {
        var additionals = new[]
        {
            "my:deep:section:additionalPropertyAlsoWorks",
            "my:additionalPropertyAlsoWorks"
        };

        var result = new Dictionary<string, string?>();
        foreach (var envVariable in envVariablesProvider.GetAll())
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
    app.MapGet("/envs", () => envVariablesProvider.GetAll());

    // for print envs to logs via dotnet run -- print-envs
    if (args.Any(a => a.Equals("print-envs", StringComparison.OrdinalIgnoreCase)))
    {
        foreach (var envVariable in envVariablesProvider.GetAll())
        {
            app.Logger.LogInformation("@{EnvVariable}", envVariable);
        }

        return 0;
    }

    // for save envs to file via dotnet run -- save-envs
    if (args.Any(a => a.Equals("save-envs", StringComparison.OrdinalIgnoreCase)))
    {
        var opt = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllBytesAsync(
            "./envs.json",
            JsonSerializer.SerializeToUtf8Bytes(
                envVariablesProvider.GetAll().OrderBy(x => x.Name),
                options: opt
            )
        );

        return 0;
    }

    app.Run();

    return 0;
}
catch (InvalidEnvVariablesException e)
{
    Log.Fatal("Failed to start app. {Message} [{Errors}]", e.Message, e.Errors);

    return 1;
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");

    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
