using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace DockerAndK8sApi.Services;

public class SystemHealthCheck : IHealthCheck
{
    private static readonly Gauge CpuUsageGauge = Metrics.CreateGauge(
        "system_cpu_usage_percent",
        "Current CPU usage percentage of the system."
    );

    private static readonly Gauge MemoryUsageGauge = Metrics.CreateGauge(
        "system_memory_usage_percent",
        "Current memory usage percentage of the system."
    );

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cpuUsage = GetCpuUsagePercentage();
            var memoryUsage = GetMemoryUsagePercentage();

            CpuUsageGauge.Set(cpuUsage);
            MemoryUsageGauge.Set(memoryUsage);

            var data = new Dictionary<string, object>
            {
                { "cpu_usage_percent", cpuUsage },
                { "memory_usage_percent", memoryUsage }
            };

            if (cpuUsage > 90 || memoryUsage > 90)
                return Task.FromResult(HealthCheckResult.Degraded("High resource usage detected", data: data));

            return Task.FromResult(HealthCheckResult.Healthy("System OK", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to read system metrics", ex));
        }
    }

    private double GetCpuUsagePercentage()
    {
        if (OperatingSystem.IsLinux())
        {
            var cpuLine = File.ReadLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu "));
            if (cpuLine == null) return 0;

            var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Skip(1)
                               .Select(double.Parse)
                               .ToArray();

            var idle = parts[3];
            var total = parts.Sum();

            // Apenas um snapshot simples — idealmente medir entre dois intervalos
            return 100 * (1 - idle / total);
        }

        // Fallback genérico (em Windows, pode usar PerformanceCounter)
        return 0;
    }

    private double GetMemoryUsagePercentage()
    {
        if (OperatingSystem.IsLinux())
        {
            var memInfo = File.ReadAllLines("/proc/meminfo");
            var total = double.Parse(memInfo.First(l => l.StartsWith("MemTotal"))
                                          .Split(':')[1].Trim().Split(' ')[0]);
            var free = double.Parse(memInfo.First(l => l.StartsWith("MemAvailable"))
                                         .Split(':')[1].Trim().Split(' ')[0]);
            return 100 * (1 - free / total);
        }

        // Fallback genérico
        return 0;
    }
}