using System.Text.Json;
using System.Text.Json.Serialization;
using ATMegaPestaV1.Api.Bench;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Enums go over the wire as camelCase text ("ok", "warning", "proceedWithBackup"), the
// same as the properties: the front end reads names and does not end up tied to the order
// the members were declared in.
builder.Services.Configure<JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddSignalR();

// Singleton because there is only one bench. The service serialises hardware access
// internally — see BenchService.
builder.Services.AddSingleton<BenchService>();

// Vite's dev server serves from a different origin. In production the React build is
// served by this same API (wwwroot), where there is no CORS to solve; the policy exists
// for development and for React Native pointing at the bench over the LAN.
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
              ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Required for SignalR over WebSockets from a different origin.
    .AllowCredentials()));

var app = builder.Build();

app.UseCors();

// The React build is copied into wwwroot; without it the API serves only the endpoints.
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Bench ───────────────────────────────────────────────────────────────────

app.MapGet("/api/bench/config", (BenchService bench) => bench.Config);

app.MapPost("/api/bench/detect", (BenchService bench, CancellationToken ct) =>
    bench.DetectAsync(ct));

app.MapPost("/api/bench/reset", async (BenchService bench, CancellationToken ct) =>
{
    await bench.ResetAsync(ct);
    return Results.NoContent();
});

// ── Target chip ─────────────────────────────────────────────────────────────

app.MapPost("/api/target/read-settings", (BenchService bench, CancellationToken ct) =>
    bench.ReadSettingsAsync(ct));

app.MapPost("/api/target/backup", (BenchService bench, CancellationToken ct) =>
    bench.SaveBackupAsync(ct));

app.MapGet("/api/target/backups/{timestamp}/{name}", (
    string timestamp, string name, BenchService bench) =>
{
    var file = bench.LocateBackupFile(timestamp, name);

    return file is null
        ? Results.NotFound()
        : Results.File(file.FullName, "application/octet-stream", file.Name);
});

// ── Integrity check ─────────────────────────────────────────────────────────

app.MapGet("/api/integrity/catalog", (BenchService bench) => bench.Catalog);

app.MapPost("/api/integrity/run", (
    IntegrityRequest request, BenchService bench, CancellationToken ct) =>
    bench.RunIntegrityAsync(request, ct));

app.MapHub<BenchHub>("/hub/bench");

// The SPA handles its own routes: a refresh on /verification is not a 404 from the API.
app.MapFallbackToFile("index.html");

app.Run();
