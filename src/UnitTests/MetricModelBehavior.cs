﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MyLab.PrometheusAgent;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class MetricModelBehavior
    {
        private readonly ITestOutputHelper _output;

        public MetricModelBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("foo_metric{label1=\"value1\",label2=\"value2\"} 1.1", null, false)]
        [InlineData("foo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1", null, false)]
        [InlineData("foo_metric{label1=\"value1\",label2=\"value2\"} 1.1 1624868358000", "28.06.2021 08:19:18", false)]
        [InlineData("foo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1 1624868358000", "28.06.2021 08:19:18", false)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1", null, true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1", null, true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1 1624868358000", "28.06.2021 08:19:18", true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1 1624868358000", "28.06.2021 08:19:18", true)]
        public async Task ShouldRead(string metricString, string dateTime, bool hasType)
        {
            //Arrange
            var reader =  new StringReader(metricString);
            DateTime? expectedDateTime = null;

            if(dateTime != null)
                expectedDateTime = DateTime.Parse(dateTime);

            //Act
            var metric = await MetricModel.ReadAsync(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);

            if (expectedDateTime.HasValue)
            {
                var timeStampFromEpoch = expectedDateTime.Value - new DateTime(1970, 1, 1);
                Assert.Equal(timeStampFromEpoch.TotalMilliseconds, metric.TimeStamp.Value);
            }

            if(hasType)
                Assert.Equal("gauge", metric.Type);
            else
            {
                Assert.Null(metric.Type);
            }
        }

        [Fact]
        public async Task ShouldReadEscaped()
        {
            //Arrange
            var reader = new StringReader("foo_metric{label1=\"value1\",label2=\"value\\\"2\"} 1.1");
            
            //Act
            var metric = await MetricModel.ReadAsync(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value\"2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);
        }

        [Fact]
        public async Task ShouldReadMetricsWithoutBody()
        {
            //Arrange
            var reader=  new StringReader("# TYPE foo_metric gauge");

            //Act
            var metric = await MetricModel.ReadAsync(reader);

            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Null(metric.TimeStamp);
            Assert.Null(metric.Labels);
            Assert.Equal(0, metric.Value);
        }

        [Fact]
        public async Task ShouldReadSimpleMetrics()
        {
            //Arrange
            var reader = new StringReader("dotnet_total_memory_bytes 6308464");
            
            //Act
            var metric = await MetricModel.ReadAsync(reader);
            
            //Assert
            Assert.Equal("dotnet_total_memory_bytes", metric.Name);
            Assert.Null(metric.Labels);
            Assert.Equal(6308464d, metric.Value);
            Assert.Null(metric.Type);
            Assert.Null(metric.TimeStamp);
        }

        [Fact]
        public async Task ShouldParseRealMetrics()
        {
            //Arrange
            var realMetricsString = await File.ReadAllTextAsync("docker-peeker-metrics.txt");
            var rdr = new StringReader(realMetricsString);

            //Act
            while (rdr.Peek() != -1)
            {
                await MetricModel.ReadAsync(rdr);
            }

            //Assert

        }

        [Fact]
        public async Task ShouldParseCrazyMetrics()
        {
            //Arrange
            var realMetricsString = await File.ReadAllTextAsync("crazy-metrics.txt");
            var rdr = new StringReader(realMetricsString);

            //Act
            while (rdr.Peek() != -1)
            {
                await MetricModel.ReadAsync(rdr);
            }

            //Assert

        }
    }
}
