using System.Collections.Generic;

namespace MyLab.PrometheusAgent.Model
{
    public class MetricReportItem
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public MetricReportItemValue[] Values { get; set; }
    }

    public class MetricReportItemValue
    {
        public double Value { get; set; }

        public long? TimeStamp { get; set; }

        public IReadOnlyDictionary<string, string> Labels { get; set; }

    }
}
