using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING ==========
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console()
      .MinimumLevel.Debug()
      .Enrich.FromLogContext()
      .Enrich.WithProperty("Application", "DockerAndK8sApi");
});

// ========== SERVICES ==========
builder.Services.AddHttpClient("ExternalAPI", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========== HEALTH CHECKS ==========
builder.Services.AddHealthChecks()
    .AddCheck("Banco de Dados", () =>
        HealthCheckResult.Unhealthy("Banco de dados ainda não configurado"))
    .AddCheck("Cache Redis", () =>
    {
        var delay = new Random().Next(100, 800);
        return delay < 500
            ? HealthCheckResult.Healthy($"Tempo de resposta: {delay}ms")
            : HealthCheckResult.Degraded($"Tempo de resposta alto: {delay}ms");
    })
    .AddCheck("Serviço Externo - Pagamentos", () =>
    {
        var available = new Random().Next(0, 3);
        return available switch
        {
            0 => HealthCheckResult.Healthy("Serviço OK"),
            1 => HealthCheckResult.Degraded("Serviço lento"),
            _ => HealthCheckResult.Unhealthy("Serviço indisponível"),
        };
    })
    .AddCheck("CPU Utilização", () =>
    {
        var cpuUsage = new Random().Next(10, 95);
        return cpuUsage < 70
            ? HealthCheckResult.Healthy($"CPU: {cpuUsage}%")
            : HealthCheckResult.Degraded($"CPU Alta: {cpuUsage}%");
    })
    .AddUrlGroup(
        new Uri("https://jsonplaceholder.typicode.com/posts/1"),
        name: "external-api",
        timeout: TimeSpan.FromSeconds(5)
    );

var app = builder.Build();

// ========== MIDDLEWARE ==========
app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMetricServer();
app.UseHttpMetrics();

// ========== ENDPOINTS ==========
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self",
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("/ping", () => Results.Ok("pong")).WithName("Ping").WithOpenApi();

app.MapGet("/", () => "API principal rodando — HealthChecks disponíveis em /health");

app.Run();
