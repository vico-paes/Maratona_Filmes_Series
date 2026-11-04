using System.Text.Json;
using System.Text.Json.Serialization;
using Maratona.Api.Auth;
using Maratona.Api.Awards;
using Maratona.Api.Data;
using Maratona.Api.Feed;
using Maratona.Api.Infrastructure;
using Maratona.Api.Jobs;
using Maratona.Api.Library;
using Maratona.Api.Lists;
using Maratona.Api.Omdb;
using Maratona.Api.Profiles;
using Maratona.Api.Reviews;
using Maratona.Api.Tmdb;
using Maratona.Api.Titles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddMemoryCache();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var frontendUrls = (builder.Configuration["FRONTEND_URL"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(frontendUrls).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((o, config) =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = JwtTokens.Issuer,
            ValidAudience = JwtTokens.Issuer,
            IssuerSigningKey = JwtTokens.Key(config)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(RateLimits.Configure);
builder.Services.AddSingleton<JwtTokens>();

builder.Services.AddHttpClient<TmdbClient>(c =>
{
    c.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    c.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<OmdbClient>(c =>
{
    c.BaseAddress = new Uri("https://www.omdbapi.com/");
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<WikidataClient>(c =>
{
    c.BaseAddress = new Uri("https://query.wikidata.org/");
    c.Timeout = TimeSpan.FromSeconds(60);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MaratonaFilmesSeries/1.0 (https://github.com/vico-paes/Maratona_Filmes_Series)");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/sparql-results+json");
});
builder.Services.AddHttpClient("datasets", c => c.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddScoped<TitleService>();
builder.Services.AddSingleton<TitleWarmupQueue>();
builder.Services.AddHostedService<TitleWarmupService>();
builder.Services.AddScoped<OmdbService>();
builder.Services.AddHostedService<TmdbSyncJob>();
builder.Services.AddHostedService<OmdbJob>();
builder.Services.AddHostedService<OscarImportJob>();
builder.Services.AddHostedService<EmmyJob>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapTitleEndpoints();
app.MapLibraryEndpoints();
app.MapReviewEndpoints();
app.MapListEndpoints();
app.MapProfileEndpoints();
app.MapFeedEndpoints();

app.Run();
