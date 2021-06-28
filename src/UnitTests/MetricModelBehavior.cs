using System;
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
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1", null)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1", null)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1 1624868358000", "28.06.2021 08:19:18")]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1 1624868358000", "28.06.2021 08:19:18")]
        public async Task ShouldRead(string metricString, string dateTime)
        {
            //Arrange
            var reader =  new StringReader(metricString);
            DateTime? expectedDateTime = null;

            if(dateTime != null)
                expectedDateTime = DateTime.Parse(dateTime);

            //Act
            var metric = await MetricModel.Read(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);

            if (expectedDateTime.HasValue)
            {
                var timeStampFromEpoch = expectedDateTime.Value - new DateTime(1970, 1, 1);
                Assert.Equal(timeStampFromEpoch.TotalMilliseconds, metric.TimeStamp.Value);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("01.01.2021 21:00")]
        public async Task ShouldWriteAsString(string dateTimeStr)
        {
            //Arrange
            var metricLabels = new Dictionary<string, string>
            {
                {"bar_key", "bar_value"}
            };

            DateTime? dateTime = dateTimeStr != null
                ? DateTime.Parse(dateTimeStr)
                : (DateTime?) null;

            var originalMetric = new  MetricModel(
                "foo", 
                "gauge", 
                1.1d, 
                dateTime,
                metricLabels);
            
            var metricStringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(metricStringBuilder);

            //Act
            await originalMetric.Write(stringWriter);

            var metricString = metricStringBuilder.ToString();
            _output.WriteLine(metricString);

            var actualMetric = await MetricModel.Read(metricString);

            //Assert
            Assert.Equal(originalMetric.Name, actualMetric.Name);
            Assert.Equal(originalMetric.Type, actualMetric.Type);
            Assert.Equal(originalMetric.Value, actualMetric.Value);
            Assert.Equal(originalMetric.Labels, actualMetric.Labels);
            Assert.Equal(originalMetric.TimeStamp, actualMetric.TimeStamp);

            if(dateTime.HasValue)
                Assert.Equal(dateTime, new DateTime(1970, 1, 1).AddMilliseconds(actualMetric.TimeStamp.Value));
        }
    }
}
