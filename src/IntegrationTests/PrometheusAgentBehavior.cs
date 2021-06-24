using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.PrometheusAgent;
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
                Output = output
            };

            Environment.SetEnvironmentVariable("PAGENT_SCRAPE_CONFIG_APTH", "./scrape-config.yml");
        }

        [Fact]
        public async Task ShouldCombineMetrics()
        {
            //Arrange
            var agent = _api.StartWithProxy();

            //Act
            var metricsString = await agent.GetMetrics();
            var metrics = await ReadMetrics(metricsString);

            //Assert
            Assert.Equal(6, metrics.Length);

            var job1Batch1Metric1 = metrics[0];
            var job1Batch1Metric2 = metrics[1];

            Assert.Equal("gauge", job1Batch1Metric1.Type);
            Assert.Equal("foo_metric", job1Batch1Metric1.Name);
            Assert.Equal(5, job1Batch1Metric1.Labels.Count);
            Assert.Equal("value1", job1Batch1Metric1.Labels["label1"]);
            Assert.Equal("value2", job1Batch1Metric1.Labels["label2"]);
            Assert.Equal("1", job1Batch1Metric1.Labels["target_batch"]);
            Assert.Equal("job1", job1Batch1Metric1.Labels["job"]);
            Assert.Equal("localhost:10200", job1Batch1Metric1.Labels["instance"]);

            Assert.Equal("counter", job1Batch1Metric2.Type);
            Assert.Equal("bar_metric", job1Batch1Metric2.Name);
            Assert.Equal(5, job1Batch1Metric2.Labels.Count);
            Assert.Equal("value3", job1Batch1Metric2.Labels["label3"]);
            Assert.Equal("value4", job1Batch1Metric2.Labels["label4"]);
            Assert.Equal("1", job1Batch1Metric2.Labels["target_batch"]);
            Assert.Equal("job1", job1Batch1Metric2.Labels["job"]);
            Assert.Equal("localhost:10201", job1Batch1Metric2.Labels["instance"]);

            var job1Batch2Metric1 = metrics[2];
            var job1Batch2Metric2 = metrics[3];

            Assert.Equal("gauge", job1Batch2Metric1.Type);
            Assert.Equal("foo_metric", job1Batch2Metric1.Name);
            Assert.Equal(3, job1Batch2Metric1.Labels.Count);
            Assert.Equal("value1", job1Batch2Metric1.Labels["label1"]);
            Assert.Equal("value2", job1Batch2Metric1.Labels["label2"]);
            Assert.Equal("1", job1Batch2Metric1.Labels["target_batch"]);
            Assert.Equal("job1", job1Batch2Metric1.Labels["job"]);
            Assert.Equal("localhost:10200", job1Batch2Metric1.Labels["instance"]);

            Assert.Equal("counter", job1Batch2Metric2.Type);
            Assert.Equal("bar_metric", job1Batch2Metric2.Name);
            Assert.Equal(5, job1Batch2Metric2.Labels.Count);
            Assert.Equal("value3", job1Batch2Metric2.Labels["label3"]);
            Assert.Equal("value4", job1Batch2Metric2.Labels["label4"]);
            Assert.Equal("2", job1Batch2Metric2.Labels["target_batch"]);
            Assert.Equal("job1", job1Batch2Metric2.Labels["job"]);
            Assert.Equal("localhost:10201", job1Batch2Metric2.Labels["instance"]);

            var job2Batch1Metric1 = metrics[4];
            var job2Batch1Metric2 = metrics[5];

            Assert.Equal("gauge", job2Batch1Metric1.Type);
            Assert.Equal("foo_metric", job2Batch1Metric1.Name);
            Assert.Equal(3, job2Batch1Metric1.Labels.Count);
            Assert.Equal("value1", job2Batch1Metric1.Labels["label1"]);
            Assert.Equal("value2", job2Batch1Metric1.Labels["label2"]);
            Assert.Equal("1", job2Batch1Metric1.Labels["target_batch"]);
            Assert.Equal("job2", job2Batch1Metric1.Labels["job"]);
            Assert.Equal("localhost:10200", job2Batch1Metric1.Labels["instance"]);

            Assert.Equal("counter", job2Batch1Metric2.Type);
            Assert.Equal("bar_metric", job2Batch1Metric2.Name);
            Assert.Equal(5, job2Batch1Metric2.Labels.Count);
            Assert.Equal("value3", job2Batch1Metric2.Labels["label3"]);
            Assert.Equal("value4", job2Batch1Metric2.Labels["label4"]);
            Assert.Equal("1", job2Batch1Metric2.Labels["target_batch"]);
            Assert.Equal("job2", job2Batch1Metric2.Labels["job"]);
            Assert.Equal("localhost:10201", job2Batch1Metric2.Labels["instance"]);
        }

        async Task<MetricModel[]> ReadMetrics(string stringSource)
        {
            var metrics = new List<MetricModel>();
            var reader = new StringReader(stringSource);


            while (reader.Peek() != -1)
            {
                metrics.Add(await MetricModel.Read(reader));
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
