using MyLab.PrometheusAgent.Services;

namespace MyLab.PrometheusAgent.Model
{
    class MetricsScrapingResult
    {
        public MetricModel[] Metrics { get; set; }

        public TargetsReportItem Report { get; set; }
    }
}