using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.PrometheusAgent;
using MyLab.PrometheusAgent.Services;
using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class MetricReportBuilderBehavior
    {
        [Fact]
        public async Task ShouldCombineLabels()
        {
            //Arrange
            var scrapeConfigProvider = new TestScrapeConfigService();
            var targetsMetricProvider = new TestTargetMetricsProvider();
            var reportBuilder = new MetricReportBuilder(targetsMetricProvider, scrapeConfigProvider);

            //Act
            var report = (await reportBuilder.Build()).ToArray();
            var reportItem = report.FirstOrDefault();

            //Assert
            Assert.Single(report);
            Assert.NotNull(reportItem);
            Assert.Equal("foo_metric", reportItem.Name);
            Assert.Equal("gauge", reportItem.Type);
            Assert.Equal(1.1d, reportItem.Value);
            Assert.Equal(5, reportItem.Labels.Count);
            Assert.Equal("value1", reportItem.Labels["label1"]);
            Assert.Equal("value2", reportItem.Labels["label2"]);
            Assert.Equal("foo_job", reportItem.Labels["job"]);
            Assert.Equal("service_label_value", reportItem.Labels["service_label"]);
            Assert.Equal("localhost:123", reportItem.Labels["instance"]);
        }

        class TestScrapeConfigService : IScrapeConfigService
        {
            public Task<ScrapeConfig> Provide()
            {
                return Task.FromResult(new ScrapeConfig
                {
                    Jobs = new []
                    {
                        new ScrapeConfigJob
                        {
                            JobName = "foo_job",
                            StaticConfigs = new []
                            {
                                new ScrapeStaticConfig
                                {
                                    Targets = new []{ "localhost:123" },
                                    Labels = new Dictionary<string, string>
                                    {
                                        {"service_label", "service_label_value"}
                                    }
                                }
                            }
                        }, 
                    }
                });
            }
        }

        class TestTargetMetricsProvider : ITargetsMetricProvider
        {
            public async Task<TargetMetrics[]> Provide()
            {
                return new[]
                {
                    new TargetMetrics
                    {
                        Id = "localhost:123",
                        Metrics = new []
                        {
                            await MetricModel.ReadAsync("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1")
                        }
                    }, 
                };
            }
        }
    }
}
