using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace DockerAndK8sApi.Services
{
    public static class SystemMetricsService
    {
        private static DateTime _lastCpuCheckTime = DateTime.UtcNow;
        private static TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private static double _lastCpuUsage = 0.0;

        /// <summary>
        /// Retorna o uso de CPU do processo atual em percentual.
        /// </summary>
        public static double GetCpuUsage()
        {
            var process = Process.GetCurrentProcess();
            var currentTime = DateTime.UtcNow;
            var currentCpuTime = process.TotalProcessorTime;

            var timeDelta = (currentTime - _lastCpuCheckTime).TotalMilliseconds;
            var cpuDelta = (currentCpuTime - _lastTotalProcessorTime).TotalMilliseconds;

            if (timeDelta > 0)
            {
                _lastCpuUsage = (cpuDelta / (Environment.ProcessorCount * timeDelta)) * 100.0;
            }

            _lastCpuCheckTime = currentTime;
            _lastTotalProcessorTime = currentCpuTime;

            return Math.Round(_lastCpuUsage, 2);
        }

        /// <summary>
        /// Retorna o uso de memória do processo atual em MB.
        /// </summary>
        public static double GetMemoryUsageMb()
        {
            var process = Process.GetCurrentProcess();
            return Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
        }

        /// <summary>
        /// Retorna a quantidade de threads do processo atual.
        /// </summary>
        public static int GetThreadCount() => Process.GetCurrentProcess().Threads.Count;

        /// <summary>
        /// Retorna o número de conexões TCP ativas para a porta local especificada.
        /// </summary>
        public static int GetActiveConnections(int localPort)
        {
            try
            {
                return IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveTcpConnections()
                    .Count(c => c.LocalEndPoint.Port == localPort);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Retorna o tempo de uptime do processo atual.
        /// </summary>
        public static TimeSpan GetUptime()
        {
            return DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
    }
}
