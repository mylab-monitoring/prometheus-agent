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
                        .AddLogging(c => c.AddXUnit(output))
            };
        }

        [Fact]
        public async Task ShouldCombineMetrics()
        {
            //Arrange
            Environment.SetEnvironmentVariable("PROMETHEUS_AGENT__CONFIG", "./scrape-config.yml");
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
            Environment.SetEnvironmentVariable("PROMETHEUS_AGENT__CONFIG", "./scrape-config-wrong-port.yml");
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
            Environment.SetEnvironmentVariable("PROMETHEUS_AGENT__CONFIG", "./scrape-config-wrong-host.yml");
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
    }
}
