using ConflictResolution.Centrifugo;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcurrentUpdates;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseDefaultServiceProvider(o =>
        {
            o.ValidateScopes = true;
            o.ValidateOnBuild = true;
        });

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ProductStore>();
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.IncludeFields = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });

        builder.Services.AddGrpcClient<CentrifugoApi.CentrifugoApiClient>(o =>
        {
            o.Address = new Uri("http://localhost:10000");
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddCors(o =>
        {
            o.AddPolicy("default", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
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

            app.MapOpenApi("/swagger/{documentName}/swagger.json").CacheOutput();
            app.UseSwaggerUI(o =>
            {
                o.ExposeSwaggerDocumentUrlsRoute = true;
                o.EnableDeepLinking();
                o.EnableFilter();
                o.EnableSwaggerDocumentUrlsEndpoint();
            });
        }

        app.UseCors("default");
        app.UseAuthorization();

        app.MapPost("/centrifugo/connect", () => Results.Ok(new { result = new { user = "777" } }));

        app.MapControllers();

        app.Run();
    }
}
