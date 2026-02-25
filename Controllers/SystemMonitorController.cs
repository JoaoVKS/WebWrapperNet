using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using WebWrap.Models;

namespace WebWrap.Controllers
{
    public static class SystemMonitorController
    {
        public static string GetFullMetricsJson()
        {
            var ramMetrics = GetRamMetrics();
            var ramTotal = ramMetrics.Total;
            var ramUsed = ramMetrics.Used;
            var metrics = new SystemMetrics
            {
                // 1. CPU (%)
                CpuUsagePercent = GetCpuUsage(),

                // 2. RAM (GB)
                RamTotalGB = ramTotal,
                RamUsedGB = ramUsed,
                RamFreeGB = Math.Round(ramTotal - ramUsed, 2),

                // 3. Storage (disk)
                Storage = GetStorageMetrics(),

                // 4. Temperatures
                Temperatures = GetTemperatures()
            };

            return JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        }

        private static List<StorageMetric> GetStorageMetrics()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady);

            var storageList = new List<StorageMetric>();

            foreach (var drive in drives)
            {
                double totalGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                double freeGB = drive.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0;
                double usedGB = totalGB - freeGB;

                storageList.Add(new StorageMetric
                {
                    DriveName = drive.Name,
                    Label = drive.VolumeLabel,
                    TotalGB = Math.Round(totalGB, 2),
                    UsedGB = Math.Round(usedGB, 2),
                    FreeGB = Math.Round(freeGB, 2),
                    UsagePercent = Math.Round((usedGB / totalGB) * 100, 1)
                });
            }
            return storageList;
        }

        private static double GetCpuUsage()
        {
            using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
            {
                cpuCounter.NextValue(); // First call usually returns 0
                System.Threading.Thread.Sleep(100); // Small delay for a real sample
                return Math.Round(cpuCounter.NextValue(), 2);
            }
        }

        private static dynamic GetRamMetrics()
        {
            var gcStatus = GC.GetGCMemoryInfo();
            double totalMemory = (double)gcStatus.TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
            double usedMemory = (double)gcStatus.MemoryLoadBytes / 1024 / 1024 / 1024;

            return new
            {
                Total = Math.Round(totalMemory, 2),
                Used = Math.Round(usedMemory, 2),
            };
        }

        private static List<TemperatureMetric> GetTemperatures()
        {
            var temps = new List<TemperatureMetric>();
            try
            {
                // Note: Requires running as Administrator for many sensors
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // The temperature in WMI is in Kelvin * 10
                        double tempKelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                        double tempCelsius = (tempKelvin / 10.0) - 273.15;
                        temps.Add(new TemperatureMetric
                        {
                            Name = obj["InstanceName"].ToString() ?? string.Empty,
                            Celsius = Math.Round(tempCelsius, 1)
                        });
                    }
                }
            }
            catch
            {
                // Acesso negado ou sensores não suportados via WMI
                return new List<TemperatureMetric>
                {
                    new TemperatureMetric { Name = "N/A", Celsius = 0 }
                };
            }
            return temps;
        }
    }
}