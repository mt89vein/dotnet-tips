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
        new RequiredVariable(name: "REQUIRED_VARIABLE", populateTo: ["my:deep:section:value", "my:deep:anotherSection:value"]),
        new OptionalVariable(name: "NOT_REQUIRED_VARIABLE", populateTo: ["top_level_value"])
    };

    if (builder.Configuration.GetValue("SOME_FEATURE_ENABLED", true))
    {
        envs.Add(new RequiredVariable("FEATURE_VARIABLE", populateTo: ["some:feature:value"]));
    }

    builder.Configuration.AddEnvSubstitution(
        LoggerFactory.Create(o => o.AddSerilog(logger)),
        envs
    );

    var app = builder.Build();

    app.MapGet("/", ([FromServices] IConfiguration config) =>
    {
        return Results.Ok(new[]
        {
            config["my:deep:section:value"],
            config["my:deep:anotherSection:value"],
            config["top_level_value"],
            config["my:deep:section:additionalPropertyAlsoWorks"],
            config["my:additionalPropertyAlsoWorks"],
            config["some:feature:value"]
        });
    });

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
