using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.PrometheusAgent;
using MyLab.PrometheusAgent.Model;
using MyLab.PrometheusAgent.Services;
using MyLab.PrometheusAgent.Tools;
using MyLab.WebErrors;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class PrometheusAgentBehavior : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestApi<Startup, IMetricSourceService> _api;

        public PrometheusAgentBehavior(ITestOutputHelper output)
        {
            _output = output;
            _api = new TestApi<Startup, IMetricSourceService>
            {
                Output = output,
                ServiceOverrider = services => 
                    services
                        .Configure<ExceptionProcessingOptions>(o => o.HideError = false)
                        .AddLogging(c => c.AddXUnit(output).AddFilter(l =>true))
            };
        }

        [Fact]
        public async Task ShouldCombineMetrics()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PrometheusAgent__ScrapeConfig", "./scrape-config.yml");
            var agent = _api.StartWithProxy();
            var originTimeStamp = (long)(new DateTime(2021,6,28,8,19,18) - new DateTime(1970,1,1)).TotalMilliseconds;

            //Act
            var metricsString = await agent.GetMetrics();
            var metrics = await ReadMetrics(metricsString);

            //Assert
            Assert.Equal(6, metrics.Length);

            var job1Batch1Metric1 = metrics.FirstOrDefault(m =>
                m.Name == "foo_metric" && 
                m.Labels["instance"] == "localhost:10200" &&
                m.Labels["target_batch"] == "1" &&
                m.Labels["job"] == "job1");
            var job1Batch1Metric2 = metrics.FirstOrDefault(m =>
                m.Name == "bar_metric" && 
                m.Labels["instance"] == "localhost:10201" &&
                m.Labels["target_batch"] == "1" &&
                m.Labels["job"] == "job1");

            Assert.NotNull(job1Batch1Metric1);
            Assert.Equal("gauge", job1Batch1Metric1.Type);
            Assert.Equal(5, job1Batch1Metric1.Labels.Count);
            Assert.Equal("value1", job1Batch1Metric1.Labels["label1"]);
            Assert.Equal("value2", job1Batch1Metric1.Labels["label2"]);
            Assert.Null(job1Batch1Metric1.TimeStamp);

            Assert.NotNull(job1Batch1Metric2);
            Assert.Equal("counter", job1Batch1Metric2.Type);
            Assert.Equal(5, job1Batch1Metric2.Labels.Count);
            Assert.Equal("value3", job1Batch1Metric2.Labels["label3"]);
            Assert.Equal("value4", job1Batch1Metric2.Labels["label4"]);
            Assert.Equal(originTimeStamp, job1Batch1Metric2.TimeStamp);

            var job1Batch2Metric1 = metrics.FirstOrDefault(m =>
                m.Name == "foo_metric" &&
                m.Labels["instance"] == "localhost:10200" &&
                m.Labels["target_batch"] == "2" &&
                m.Labels["job"] == "job1");
            var job1Batch2Metric2 = metrics.FirstOrDefault(m =>
                m.Name == "bar_metric" &&
                m.Labels["instance"] == "localhost:10201" &&
                m.Labels["target_batch"] == "2" &&
                m.Labels["job"] == "job1");

            Assert.NotNull(job1Batch2Metric1);
            Assert.Equal("gauge", job1Batch2Metric1.Type);
            Assert.Equal("foo_metric", job1Batch2Metric1.Name);
            Assert.Equal(5, job1Batch2Metric1.Labels.Count);
            Assert.Equal("value1", job1Batch2Metric1.Labels["label1"]);
            Assert.Equal("value2", job1Batch2Metric1.Labels["label2"]);
            Assert.Null(job1Batch2Metric1.TimeStamp);

            Assert.NotNull(job1Batch2Metric2);
            Assert.Equal("counter", job1Batch2Metric2.Type);
            Assert.Equal("bar_metric", job1Batch2Metric2.Name);
            Assert.Equal(5, job1Batch2Metric2.Labels.Count);
            Assert.Equal("value3", job1Batch2Metric2.Labels["label3"]);
            Assert.Equal("value4", job1Batch2Metric2.Labels["label4"]);
            Assert.Equal(originTimeStamp, job1Batch2Metric2.TimeStamp);

            var job2Batch1Metric1 = metrics.FirstOrDefault(m =>
                m.Name == "foo_metric" &&
                m.Labels["instance"] == "localhost:10200" &&
                m.Labels["job"] == "job2");
            var job2Batch1Metric2 = metrics.FirstOrDefault(m =>
                m.Name == "bar_metric" &&
                m.Labels["instance"] == "localhost:10201" &&
                m.Labels["job"] == "job2");

            Assert.NotNull(job2Batch1Metric1);
            Assert.Equal("gauge", job2Batch1Metric1.Type);
            Assert.Equal(5, job2Batch1Metric1.Labels.Count);
            Assert.Equal("value1", job2Batch1Metric1.Labels["label1"]);
            Assert.Equal("value2", job2Batch1Metric1.Labels["label2"]);
            Assert.Equal("1", job2Batch1Metric1.Labels["target_batch"]);
            Assert.Null(job2Batch1Metric1.TimeStamp);

            Assert.NotNull(job2Batch1Metric2);
            Assert.Equal("counter", job2Batch1Metric2.Type);
            Assert.Equal(5, job2Batch1Metric2.Labels.Count);
            Assert.Equal("value3", job2Batch1Metric2.Labels["label3"]);
            Assert.Equal("value4", job2Batch1Metric2.Labels["label4"]);
            Assert.Equal("1", job2Batch1Metric2.Labels["target_batch"]);
            Assert.Equal(originTimeStamp, job2Batch1Metric2.TimeStamp);
        }

        [Fact]
        public async Task ShouldResistantToUnaccessibleTargets()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PrometheusAgent__ScrapeConfig", "./scrape-config-wrong-port.yml");
            var agent = _api.StartWithProxy();

            //Act
            var metricsString = await agent.GetMetrics();

            var metrics = await ReadMetrics(metricsString);
            var job1Batch1Metric1 = metrics.FirstOrDefault(m =>
                m.Name == "foo_metric" &&
                m.Labels["instance"] == "localhost:10200" &&
                m.Labels["job"] == "job1");

            //Assert
            Assert.NotNull(job1Batch1Metric1);
        }

        [Fact]
        public async Task ShouldResistantToUnresolvedHost()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PrometheusAgent__ScrapeConfig", "./scrape-config-wrong-host.yml");
            var agent = _api.StartWithProxy();

            //Act
            var metricsString = await agent.GetMetrics();

            var metrics = await ReadMetrics(metricsString);
            var job1Batch1Metric1 = metrics.FirstOrDefault(m =>
                m.Name == "foo_metric" &&
                m.Labels["instance"] == "localhost:10200" &&
                m.Labels["job"] == "job1");

            //Assert
            Assert.NotNull(job1Batch1Metric1);
        }

        [Fact]
        public async Task ShouldProvideConfiguration()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PrometheusAgent__ScrapeConfig", "./scrape-config-min.yml");
            var agent = _api.StartWithProxy();

            //Act
            var sources = await agent.GetConfig();

            //Assert
            Assert.NotNull(sources);
            Assert.Equal(2, sources.Length);
            Assert.Equal("http://localhost:10200/metrics-path", sources[0].ScrapeUrl.OriginalString);
            Assert.Equal("localhost:10200", sources[0].Labels["instance"]);
            Assert.Equal("job1", sources[0].Labels["job"]);
            Assert.Equal("1", sources[0].Labels["target_batch"]);
            Assert.Equal("http://localhost:10201/metrics-path", sources[1].ScrapeUrl.OriginalString);
            Assert.Equal("localhost:10201", sources[1].Labels["instance"]);
            Assert.Equal("job1", sources[1].Labels["job"]);
            Assert.Equal("1", sources[1].Labels["target_batch"]);
        }

        [Fact]
        public async Task ShouldProvideReport()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PrometheusAgent__ScrapeConfig", "./scrape-config-min.yml");
            var agent = _api.StartWithProxy();
            await agent.GetMetrics();

            //Act
            var report = await agent.GetReport();

            var report1 = report?.FirstOrDefault(r => r.Id == "http://localhost:10200/metrics-path");
            var report2 = report?.FirstOrDefault(r => r.Id == "http://localhost:10201/metrics-path");

            //Assert
            Assert.NotNull(report1);
            Assert.True(DateTime.Now.AddSeconds(-10) < report1.Dt);
            Assert.Null(report1.Error);
            Assert.True(70 < report1.ResponseVolume && report1.ResponseVolume < 80);
            Assert.Equal(1, report1.MetricsCount);
            Assert.True(report1.Duration < TimeSpan.FromSeconds(1));

            Assert.NotNull(report2);
            Assert.True(DateTime.Now.AddSeconds(-10) < report2.Dt);
            Assert.Null(report2.Error);
            Assert.True(80 < report2.ResponseVolume && report2.ResponseVolume < 90);
            Assert.Equal(1, report2.MetricsCount);
            Assert.True(report1.Duration < TimeSpan.FromSeconds(1));
        }

        async Task<MetricModel[]> ReadMetrics(string stringSource)
        {
            if (string.IsNullOrEmpty(stringSource))
                return Enumerable.Empty<MetricModel>().ToArray();

            var metrics = new List<MetricModel>();
            var reader = new StringReader(stringSource);


            while (reader.Peek() != -1)
            {
                metrics.Add(await MetricModel.ReadAsync(reader));
            }

            return metrics.ToArray();
        }

        public void Dispose()
        {
            _api?.Dispose();
        }
    }

    [Api]
    public interface IMetricSourceService
    {
        [Get("metrics")]
        Task<string> GetMetrics();

        [Get("config")]
        Task<ScrapeSourceDescription[]> GetConfig();

        [Get("report")]
        Task<TargetsReportItem[]> GetReport();
    }
}
