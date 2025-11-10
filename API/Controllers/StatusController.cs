using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.Devices;

namespace DockerAndK8sApi.Controller
{
    public static class SystemMetricsService
    {
        private static readonly DateTime _startTime = DateTime.UtcNow;
        private static TimeSpan? _lastCpuTotal = null;
        private static DateTime? _lastCpuCheck = null;

        // =============================
        // Uptime
        // =============================
        public static TimeSpan GetUptime()
        {
            return DateTime.UtcNow - _startTime;
        }

        // =============================
        // CPU Usage (Linux + Windows)
        // =============================
        public static double GetCpuUsage()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return GetCpuUsageLinux();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return GetCpuUsageWindows();
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private static double GetCpuUsageLinux()
        {
            string[] cpuStats1 = File.ReadAllText("/proc/stat").Split('\n')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            System.Threading.Thread.Sleep(100);
            string[] cpuStats2 = File.ReadAllText("/proc/stat").Split('\n')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double idle1 = double.Parse(cpuStats1[4]);
            double total1 = cpuStats1.Skip(1).Select(double.Parse).Sum();

            double idle2 = double.Parse(cpuStats2[4]);
            double total2 = cpuStats2.Skip(1).Select(double.Parse).Sum();

            double totalDiff = total2 - total1;
            double idleDiff = idle2 - idle1;

            return Math.Round((1.0 - idleDiff / totalDiff) * 100, 2);
        }

        private static double GetCpuUsageWindows()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(100);
            return Math.Round(cpuCounter.NextValue(), 2);
        }

        // =============================
        // Memory Usage
        // =============================
        public static double GetMemoryUsageMb()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows via PerformanceCounter ou WMI
                return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024.0 / 1024.0;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux via /proc/meminfo
                var memInfo = File.ReadAllLines("/proc/meminfo");
                var memTotalLine = memInfo.FirstOrDefault(l => l.StartsWith("MemTotal"));
                if (memTotalLine != null)
                {
                    var parts = memTotalLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (long.TryParse(parts[1], out var kb))
                        return kb / 1024.0;
                }
            }

            return 0;
        }

        // =============================
        // Disk Usage
        // =============================
        public static double GetDiskUsagePercent()
        {
            try
            {
                DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
                if (drive == null) return 0;

                double used = drive.TotalSize - drive.TotalFreeSpace;
                return Math.Round((used / drive.TotalSize) * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        // =============================
        // Thread Count
        // =============================
        public static int GetThreadCount()
        {
            try
            {
                return Process.GetCurrentProcess().Threads.Count;
            }
            catch
            {
                return 0;
            }
        }

        // =============================
        // Active Connections
        // =============================
        public static int GetActiveConnections(int port)
        {
            try
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipProperties.GetActiveTcpConnections();
                return tcpConnections.Count(c => c.LocalEndPoint.Port == port);
            }
            catch
            {
                return 0;
            }
        }

        // =============================
        // OS Information
        // =============================
        public static string GetSystemInfo()
        {
            try
            {
                return $"{RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})";
            }
            catch
            {
                return "Unknown OS";
            }
        }
    }
}
