using System.Text.Json;
using System.Text.Json.Serialization;
using Maratona.Api.Data;
using Maratona.Api.Infrastructure;
using Maratona.Api.Jobs;
using Maratona.Api.Tmdb;
using Maratona.Api.Titles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings__Default não configurada.");

builder.Services.AddDbContext<AppDbContext>(o => o
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<TmdbClient>(c =>
{
    c.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    c.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<TitleService>();
builder.Services.AddHostedService<TmdbSyncJob>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapTitleEndpoints();

app.Run();
