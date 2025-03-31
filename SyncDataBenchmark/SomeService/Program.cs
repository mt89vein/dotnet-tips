using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SomeService;
using SomeService.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});

builder
    .Services
    .AddOpenTelemetry()
    .ConfigureResource(b => b.AddService(serviceName: "SomeService"))
    .WithMetrics(o =>
    {
        o.AddMeter("SomeService");
        o.AddAspNetCoreInstrumentation();
        o.AddPrometheusExporter();
        o.AddNpgsqlInstrumentation();
        o.AddRuntimeInstrumentation();
        o.AddHttpClientInstrumentation();
        o.AddProcessInstrumentation();
    });

// Add services to the container.
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new ConfigurationException("Connection string is missing");
}

var npgsqlDataSource =
    new NpgsqlDataSourceBuilder(connectionString)
        {
            Name = "SomeService",
            DefaultNameTranslator = new NpgsqlSnakeCaseNameTranslator()
        }
        .EnableDynamicJson()
        .BuildMultiHost()
        .WithTargetSession(TargetSessionAttributes.Primary);

builder.Services.AddSingleton(npgsqlDataSource);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ServiceContext>(o =>
{
    o.EnableDetailedErrors();
    o.EnableSensitiveDataLogging();
    o.UseSnakeCaseNamingConvention();
    o.UseNpgsql(npgsqlDataSource);
    o.UseLinqToDB();
});
builder.Services.AddScoped<Runner>();
builder.Services.AddScoped<Repository>();
builder.Services.AddSingleton<SyncDataSourceClient>();

LinqToDBForEFTools.Initialize();

builder.Services.AddHttpClient("SyncDataSource", client =>
{
    client.BaseAddress = new Uri("http://localhost:5080");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/swagger/{documentName}/swagger.json").CacheOutput();
    app.UseSwaggerUI(o =>
    {
        o.ExposeSwaggerDocumentUrlsRoute = true;
        o.EnableDeepLinking();
        o.EnableFilter();
        o.EnableSwaggerDocumentUrlsEndpoint();
    });
}

app.MapPrometheusScrapingEndpoint();

app.UseAuthorization();

app.MapGet("sync", (
    [FromQuery(Name = "i")] InsertApproachType insertApproachType,
    [FromQuery(Name = "f")] FetchApproachType fetchApproachType,
    [FromServices] IServiceScopeFactory scopeFactory
) =>
{
    // fire and forget
    _ = Task.Run(async () =>
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<Runner>();
        await runner.RunAsync(insertApproachType, fetchApproachType, CancellationToken.None);
    });
}).WithName("sync");

using var scope = app.Services.CreateScope();
using var ctx = scope.ServiceProvider.GetRequiredService<ServiceContext>();
ctx.Database.Migrate();

app.Run();
