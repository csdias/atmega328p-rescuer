using System.Text.Json;
using System.Text.Json.Serialization;
using ATMegaPestaV1.Api.Bancada;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Os enums vão como texto em camelCase ("ok", "aviso", "prosseguirComCopia"), a par das
// propriedades: o front end lê nomes e não fica agarrado à ordem em que os membros foram
// declarados.
builder.Services.Configure<JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddSignalR();

// Singleton porque a bancada é uma só. O serviço serializa internamente os acessos ao
// hardware — ver BancadaService.
builder.Services.AddSingleton<BancadaService>();

// O dev server do Vite serve noutra origem. Em produção o React é servido por esta mesma
// API (wwwroot), onde não há CORS para resolver; a política existe para o desenvolvimento
// e para o React Native a apontar para a bancada pela LAN.
var origens = builder.Configuration.GetSection("OrigensPermitidas").Get<string[]>()
              ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origens)
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Necessário para o SignalR sobre WebSockets com origem distinta.
    .AllowCredentials()));

var app = builder.Build();

app.UseCors();

// O build do React é copiado para wwwroot; sem ele a API serve só os endpoints.
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Bancada ─────────────────────────────────────────────────────────────────

app.MapGet("/api/bancada/config", (BancadaService bancada) => bancada.Config);

app.MapPost("/api/bancada/detectar", (BancadaService bancada, CancellationToken ct) =>
    bancada.DetectarAsync(ct));

app.MapPost("/api/bancada/reiniciar", async (BancadaService bancada, CancellationToken ct) =>
{
    await bancada.ReiniciarAsync(ct);
    return Results.NoContent();
});

// ── Chip-alvo ───────────────────────────────────────────────────────────────

app.MapPost("/api/alvo/ler-configuracoes", (BancadaService bancada, CancellationToken ct) =>
    bancada.LerConfiguracoesAsync(ct));

app.MapPost("/api/alvo/copia", (BancadaService bancada, CancellationToken ct) =>
    bancada.GuardarCopiaAsync(ct));

app.MapGet("/api/alvo/copias/{carimbo}/{nome}", (
    string carimbo, string nome, BancadaService bancada) =>
{
    var ficheiro = bancada.LocalizarFicheiroCopia(carimbo, nome);

    return ficheiro is null
        ? Results.NotFound()
        : Results.File(ficheiro.FullName, "application/octet-stream", ficheiro.Name);
});

// ── Verificação de integridade ──────────────────────────────────────────────

app.MapGet("/api/integridade/catalogo", (BancadaService bancada) => bancada.Catalogo);

app.MapPost("/api/integridade/executar", (
    IntegridadeRequest pedido, BancadaService bancada, CancellationToken ct) =>
    bancada.ExecutarIntegridadeAsync(pedido, ct));

app.MapHub<BancadaHub>("/hub/bancada");

// O SPA trata as suas próprias rotas: um refresh em /verificacao não é um 404 da API.
app.MapFallbackToFile("index.html");

app.Run();
