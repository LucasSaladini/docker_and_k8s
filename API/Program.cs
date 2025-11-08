using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ===== 1️⃣ Logging =====
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console()
      .Enrich.FromLogContext();
});

// ===== 2️⃣ Services =====
builder.Services.AddHttpClient("ExternalAPI", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ===== 3️⃣ HealthChecks =====
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(
        new Uri("https://jsonplaceholder.typicode.com/posts/1"),
        name: "external-api",
        timeout: TimeSpan.FromSeconds(5)
    );

// ===== 4️⃣ Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== 5️⃣ Middleware =====
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

// ===== 6️⃣ Endpoints =====

// Ping
app.MapGet("/ping", () => Results.Ok("pong"))
   .WithName("Ping")
   .WithOpenApi();

// External API Call
app.MapGet("/external", async ([FromServices] IHttpClientFactory httpClientFactory,
                               [FromServices] ILogger<Program> logger) =>
{
    var client = httpClientFactory.CreateClient("ExternalAPI");
    try
    {
        var response = await client.GetAsync("posts/1");
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("External API returned non-success status: {StatusCode}", response.StatusCode);
            return Results.Problem($"External API error: {response.StatusCode}");
        }

        var data = await response.Content.ReadFromJsonAsync<object>();
        logger.LogInformation("External API call successful");
        return Results.Ok(data);
    }
    catch (TaskCanceledException)
    {
        logger.LogError("External API call timed out");
        return Results.Problem("External API call timed out");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error calling external API");
        return Results.Problem("Unexpected error calling external API");
    }
})
.WithName("ExternalCall")
.WithOpenApi();

// Health Checks (liveness/readiness)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Metrics Prometheus
app.UseMetricServer(); // endpoint /metrics
app.UseHttpMetrics();  // coleta métricas de HTTP

app.Run();
