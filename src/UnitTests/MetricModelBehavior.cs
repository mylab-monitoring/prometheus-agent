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
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1")]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1")]
        public async Task ShouldRead(string metricString)
        {
            //Arrange
            var reader =  new StringReader(metricString);

            //Act
            var metric = await MetricModel.Read(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);
        }

        [Fact]
        public async Task ShouldWriteAsString()
        {
            //Arrange
            var metricLabels = new Dictionary<string, string>
            {
                {"bar_key", "bar_value"}
            };
            var originalMetric = new  MetricModel("foo", "gauge", 1.1d, metricLabels);
            
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
        }
    }
}
