using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// HEALTH CHECKS DO PRÓPRIO DASHBOARD
// ====================================
var healthChecks = builder.Services.AddHealthChecks();

// Checa se o próprio dashboard está no ar
healthChecks.AddCheck("Dashboard", () => HealthCheckResult.Healthy("Dashboard operacional"), tags: new[] { "ui" });

// ====================================
// HEALTHCHECKS UI (sem EF Core)
// ====================================
builder.Services
    .AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(30); // a cada 30s
        options.MaximumHistoryEntriesPerEndpoint(50);

        // 👇 aqui está o pulo do gato
        options.AddHealthCheckEndpoint(
            name: "API Principal",
            uri: "http://localhost:8080/health" // ou o endereço público da sua API
        );
    })
    .AddSqliteStorage("Data Source=healthchecks.db");

// ====================================
// PIPELINE
// ====================================
var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/monitor";      // dashboard visual
    options.ApiPath = "/monitor-api"; // endpoint dos dados
});

app.Run();
