namespace WebWrap.Models
{
    public class SystemMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double RamTotalGB { get; set; }
        public double RamUsedGB { get; set; }
        public double RamFreeGB { get; set; }
        public List<StorageMetric> Storage { get; set; } = new();
        public List<TemperatureMetric> Temperatures { get; set; } = new();
    }

    public class StorageMetric
    {
        public string DriveName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double TotalGB { get; set; }
        public double UsedGB { get; set; }
        public double FreeGB { get; set; }
        public double UsagePercent { get; set; }
    }

    public class TemperatureMetric
    {
        public string Name { get; set; } = string.Empty;
        public double Celsius { get; set; }
    }
}
