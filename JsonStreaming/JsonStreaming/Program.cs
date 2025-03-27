using System.Runtime.CompilerServices;

namespace JsonStreaming;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapGet("/stream", (CancellationToken ct) =>
            {
                return Results.Ok(Generate(ct));

                static async IAsyncEnumerable<StreamItem> Generate([EnumeratorCancellation] CancellationToken ct)
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1000), ct);

                        yield return new StreamItem(DateTimeOffset.UtcNow, i);
                    }
                }
            })
            .WithName("stream");

        app.Run();
    }
}

public readonly record struct StreamItem(DateTimeOffset Timestamp, int Value);
