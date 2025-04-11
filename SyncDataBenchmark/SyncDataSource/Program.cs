// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.NameTranslation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SyncDataSource;

#pragma warning disable CA1852
#pragma warning disable CA1506

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
        o.AddAspNetCoreInstrumentation();
        o.AddPrometheusExporter();
        o.AddNpgsqlInstrumentation();
        o.AddRuntimeInstrumentation();
        o.AddHttpClientInstrumentation();
        o.AddProcessInstrumentation();
    });

// Add services to the container.
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new ConfigurationException("Connection string is missing");
}

var npgsqlDataSource =
    new NpgsqlDataSourceBuilder(connectionString)
        {
            Name = "SyncDataSource",
            DefaultNameTranslator = new NpgsqlSnakeCaseNameTranslator()
        }
        .EnableDynamicJson()
        .BuildMultiHost()
        .WithTargetSession(TargetSessionAttributes.Primary);

builder.Services.AddSingleton(npgsqlDataSource);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<UsersContext>(o =>
{
    o.EnableDetailedErrors();
    o.EnableSensitiveDataLogging();
    o.UseSnakeCaseNamingConvention();
    o.UseNpgsql(npgsqlDataSource);
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

// redirect to swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");

        await context.Response.StartAsync();
    }
    else
    {
        await next(context);
    }

});

app.MapPrometheusScrapingEndpoint();

app.UseAuthorization();

app.MapGet("/ef-core-stream", ([FromServices] UsersContext ctx, CancellationToken ct) =>
    {
        return Results.Ok(ctx
            .Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .AsAsyncEnumerable()
        );
    })
    .WithName("ef-core-stream");

app.MapGet("/npgsql-copy-to-stream", ([FromServices] NpgsqlDataSource dataSource, CancellationToken ct) =>
    {
        return Results.Ok(StreamUsers(dataSource, ct));

        static async IAsyncEnumerable<UserDto> StreamUsers(NpgsqlDataSource dataSource, [EnumeratorCancellation] CancellationToken ct)
        {
            await using var conn = await dataSource.OpenConnectionAsync(ct);
            await using var reader = conn.BeginBinaryExport("COPY users (id, surname, name, patronymic, created_at, is_active) TO STDOUT (FORMAT BINARY)");

            while(reader.StartRow() > 0)
            {
                yield return new UserDto
                {
                    Id = reader.Read<Guid>(),
                    Surname = reader.Read<string>(),
                    Name = reader.Read<string>(),
                    Patronymic = reader.Read<string>(),
                    CreatedAt = reader.Read<DateTimeOffset>(),
                    IsActive = reader.Read<bool>(),
                };
            }
        }
    })
    .WithName("npgsql-copy-to-stream");

app.MapGet("/offset-paging", async (
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] UsersContext ctx,
        CancellationToken ct
    ) =>
    {
        var skip = (page - 1) * pageSize;

        return Results.Ok(await ctx
            .Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToArrayAsync(ct)
        );
    })
    .WithName("offset-paging");

app.MapGet("/keyset-paging", async (
        [FromQuery] Guid? lastId,
        [FromQuery] int pageSize,
        [FromServices] UsersContext ctx,
        CancellationToken ct
    ) =>
    {
        return Results.Ok(await ctx
            .Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Where(x => (lastId == null || x.Id > lastId))
            .Take(pageSize)
            .ToArrayAsync(ct)
        );
    })
    .WithName("keyset-paging");

using var scope = app.Services.CreateScope();
using var ctx = scope.ServiceProvider.GetRequiredService<UsersContext>();
ctx.Database.Migrate();

app.Run();



internal readonly record struct UserDto
{
    public Guid Id { get; init; }

    public required string Surname { get; init; }

    public required string Name { get; init; }

    public string? Patronymic { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public required bool IsActive { get; init; }
}
